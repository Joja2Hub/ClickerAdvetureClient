const { test } = require('node:test');
const assert = require('node:assert/strict');
const { Timestamp } = require('firebase-admin/firestore');
const { claimReward } = require('../lib/claimQuestReward.js');

const input = { childId: 'child', questId: 'quest' };
const questPath = 'children/child/quests/quest';
const rewardPath = 'children/child/rewardLedger/quest_quest';

function setup(questPatch = {}, reward) {
  const docs = new Map([
    ['users/uid', { role: 'child', childId: 'child', familyId: 'family' }],
    ['children/child', { familyId: 'family' }],
    [questPath, { status: 'approved', approvedAt: Timestamp.fromMillis(1000), rewardXp: 25, rewardGold: 10, ...questPatch }],
  ]);
  if (reward !== undefined) docs.set(rewardPath, reward);
  const commits = [];
  let queue = Promise.resolve();
  const db = {
    doc(path) {
      return { path, collection: name => ({ doc: id => db.doc(`${path}/${name}/${id}`) }) };
    },
    runTransaction(work) {
      const result = queue.then(async () => {
        const writes = [];
        const response = await work({
          get: async ref => {
            assert.equal(writes.length, 0);
            return { data: () => docs.get(ref.path) };
          },
          create: (ref, data) => {
            assert.equal(docs.has(ref.path), false);
            writes.push([ref.path, data]);
          },
          update: (ref, data) => writes.push([ref.path, data]),
          set: (ref, data) => writes.push([ref.path, data]),
        });
        for (const [path, data] of writes) docs.set(path, { ...docs.get(path), ...data });
        commits.push(writes);
        return response;
      });
      queue = result.catch(() => {});
      return result;
    },
  };
  return { db, docs, commits };
}

test('legacy approval creates an already-claimed ledger and credits in the same transaction', async () => {
  const { db, docs, commits } = setup();
  assert.deepEqual(await claimReward(db, 'uid', input), { ok: true });
  assert.equal(docs.get(questPath).status, 'claimed');
  assert.equal(docs.get(rewardPath).claimed, true);
  assert.equal(docs.get(rewardPath).xp, 25);
  assert.equal(docs.get(rewardPath).gold, 10);
  const paths = commits[0].map(([path]) => path);
  assert.ok(paths.includes('children/child/gameState/main'));
  assert.ok(paths.includes(rewardPath));
  assert.ok(paths.includes(questPath));
});

test('a second claim in the same queue cannot create another credit', async () => {
  const { db, commits } = setup();
  const results = await Promise.allSettled([claimReward(db, 'uid', input), claimReward(db, 'uid', input)]);
  assert.equal(results[0].status, 'fulfilled');
  assert.equal(results[1].reason.code, 'already-exists');
  assert.equal(commits.length, 1);
});

test('existing ledger remains authoritative for a geolocation quest', async () => {
  const { db, docs } = setup({ geo: {} }, { claimed: false, xp: 7, gold: 3 });
  await claimReward(db, 'uid', input);
  assert.equal(docs.get(rewardPath).xp, 7);
  assert.equal(docs.get(rewardPath).claimed, true);
});

for (const [name, patch, code] of [
  ['geo quest without ledger', { geo: {} }, 'failed-precondition'],
  ['missing approval date', { approvedAt: null }, 'failed-precondition'],
  ['claimed legacy quest', { status: 'claimed' }, 'already-exists'],
  ['old claim marker', { claimedAt: Timestamp.fromMillis(2000) }, 'already-exists'],
  ['unapproved quest', { status: 'submitted' }, 'failed-precondition'],
  ['invalid amount', { rewardXp: -1 }, 'failed-precondition'],
]) {
  test(`rejects ${name} without crediting`, async () => {
    const { db, commits } = setup(patch);
    await assert.rejects(claimReward(db, 'uid', input), { code });
    assert.deepEqual(commits, []);
  });
}

test('rejects a profile belonging to a different family', async () => {
  const { db, docs, commits } = setup();
  docs.set('children/child', { familyId: 'other-family' });
  await assert.rejects(claimReward(db, 'uid', input), { code: 'permission-denied' });
  assert.deepEqual(commits, []);
});
