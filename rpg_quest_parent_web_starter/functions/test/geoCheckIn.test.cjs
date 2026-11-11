const { test } = require('node:test');
const assert = require('node:assert/strict');
const { registerGeoVisit } = require('../lib/geoCheckIn.js');
const { assertCompletedGeoQuest, evaluateGeoVisit } = require('../lib/geoPolicy.js');
const start = Date.parse('2026-11-09T00:00:00+03:00') / 1000;
const geo = () => ({ version: 1, latitude: 55.75, longitude: 37.62, radiusMeters: 100,
  requiredVisits: 2, startsAtUnixSeconds: start, endsAtUnixSeconds: start + 7 * 86400, utcOffsetMinutes: 180 });
const reading = (time = start + 3600) => ({ childId: 'child', questId: 'school', latitude: 55.75,
  longitude: 37.62, accuracyMeters: 5, observedAtUnixSeconds: time });
const questPath = 'children/child/quests/school';

// Serial transaction double: applies all writes together and rejects reads after writes.
// Real Firestore contention/retry and access rules still require emulator verification.
class MemoryDb {
  constructor() {
    this.docs = new Map([
      ['users/uid', { role: 'child', childId: 'child', familyId: 'family' }],
      ['children/child', { familyId: 'family' }],
      [questPath, { status: 'assigned', geo: geo() }],
    ]);
    this.tail = Promise.resolve();
  }
  doc(path) { return { path, collection: name => ({ doc: id => this.doc(`${path}/${name}/${id}`) }) }; }
  runTransaction(work) {
    const result = this.tail.then(async () => {
      const writes = [];
      const value = await work({
        get: async ref => {
          assert.equal(writes.length, 0, 'all reads must precede writes');
          const data = this.docs.get(ref.path);
          return { exists: data !== undefined, data: () => data };
        },
        create: (ref, data) => { assert.equal(this.docs.has(ref.path), false); writes.push([ref.path, data]); },
        update: (ref, data) => writes.push([ref.path, { ...this.docs.get(ref.path), ...data }]),
      });
      for (const [path, data] of writes) this.docs.set(path, data);
      return value;
    });
    this.tail = result.catch(() => {});
    return result;
  }
}
const rejectCode = code => error => { assert.equal(error.code, code); return true; };

test('first visit is persisted; same-day retries leave count unchanged', async () => {
  const db = new MemoryDb();
  const first = await registerGeoVisit(db, 'uid', reading(), start + 3600);
  assert.equal(first.visitCount, 1);
  const repeat = await registerGeoVisit(db, 'uid', reading(start + 7200), start + 7200);
  assert.equal(repeat.alreadyVisitedToday, true);
  assert.equal(repeat.visitCount, 1);
  assert.equal(db.docs.size, 4);
});
test('parallel requests count a day once', async () => {
  const db = new MemoryDb();
  const results = await Promise.all(Array.from({ length: 5 }, () => registerGeoVisit(db, 'uid', reading(), start + 3600)));
  assert.equal(results.filter(r => !r.alreadyVisitedToday).length, 1);
  assert.equal(db.docs.get(questPath).geoProgress.visitCount, 1);
});
test('final visit submits to the parent without crediting rewards', async () => {
  const db = new MemoryDb();
  await registerGeoVisit(db, 'uid', reading(), start + 3600);
  const next = start + 86400 + 3600;
  const final = await registerGeoVisit(db, 'uid', reading(next), next);
  assert.equal(final.status, 'submitted');
  assert.equal(final.visitCount, 2);
  assert.ok(db.docs.get(questPath).submittedAt);
  const progress = db.docs.get(questPath).geoProgress;
  assert.doesNotThrow(() => assertCompletedGeoQuest(geo(), progress));
  assert.throws(() => assertCompletedGeoQuest(geo(), { ...progress, visitCount: 1 }));
  assert.throws(() => assertCompletedGeoQuest({ ...geo(), radiusMeters: 200 }, progress));
  assert.throws(() => assertCompletedGeoQuest(geo(), undefined));
  const repeat = await registerGeoVisit(db, 'uid', reading(next), next);
  assert.equal(repeat.alreadyVisitedToday, true);
  assert.equal(db.docs.get('children/child').gold, undefined);
  assert.equal([...db.docs.keys()].some(key => key.includes('rewardLedger')), false);
});
for (const [label, change, code] of [
  ['missing login', db => {}, 'unauthenticated'],
  ['other child', db => db.docs.set('users/uid', { role: 'child', childId: 'other', familyId: 'family' }), 'permission-denied'],
  ['parent account', db => db.docs.set('users/uid', { role: 'parent', childId: 'child', familyId: 'family' }), 'permission-denied'],
  ['other family', db => db.docs.set('children/child', { familyId: 'other' }), 'permission-denied'],
  ['missing quest', db => db.docs.delete(questPath), 'not-found'],
  ['cancelled quest', db => db.docs.get(questPath).status = 'cancelled', 'failed-precondition'],
  ['bad progress', db => db.docs.get(questPath).geoProgress = { visitCount: -1 }, 'failed-precondition'],
]) test(`rejects ${label} without writes`, async () => {
  const db = new MemoryDb(); change(db);
  const before = [...db.docs.entries()];
  await assert.rejects(registerGeoVisit(db, label === 'missing login' ? undefined : 'uid', reading(), start + 3600), rejectCode(code));
  assert.deepEqual([...db.docs.entries()], before);
});
test('changed conditions do not mix old and new progress', async () => {
  const db = new MemoryDb();
  await registerGeoVisit(db, 'uid', reading(), start + 3600);
  db.docs.get(questPath).geo.radiusMeters = 200;
  await assert.rejects(registerGeoVisit(db, 'uid', reading(), start + 3600), rejectCode('failed-precondition'));
  assert.equal(db.docs.get(questPath).geoProgress.visitCount, 1);
});
test('path segments cannot redirect the request', async () => {
  await assert.rejects(registerGeoVisit(new MemoryDb(), 'uid', { ...reading(), questId: '../other' }), rejectCode('invalid-argument'));
});
for (const [label, patch, now] of [
  ['stale', { observedAtUnixSeconds: start + 3569 }, start + 3600],
  ['future', { observedAtUnixSeconds: start + 3601 }, start + 3600],
  ['outside', { latitude: 56 }, start + 3600],
  ['inaccurate', { accuracyMeters: 101 }, start + 3600],
  ['invalid coordinates', { longitude: NaN }, start + 3600],
  ['exclusive end', { observedAtUnixSeconds: start + 7 * 86400 }, start + 7 * 86400],
]) test(`rejects ${label} readings without writing progress`, async () => {
  const db = new MemoryDb();
  await assert.rejects(registerGeoVisit(db, 'uid', { ...reading(), ...patch }, now));
  assert.equal(db.docs.get(questPath).geoProgress, undefined);
  assert.equal(db.docs.size, 3);
});
test('day is derived from observation time and the quest UTC offset', () => {
  const now = Date.parse('2026-11-09T21:00:00Z') / 1000;
  assert.equal(evaluateGeoVisit(geo(), reading(now), now).day, '2026-11-10');
  assert.equal(evaluateGeoVisit(geo(), reading(now - 1), now).day, '2026-11-09');
});
