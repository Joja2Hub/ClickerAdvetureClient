const { test } = require('node:test');
const assert = require('node:assert/strict');
const { buildGeoQuest, newGeoQuestDraft, validateGeoQuest } = require('../../Temp/ParentGeoChecks/geoQuest.js');

const draft = () => ({ latitude: '55.75', longitude: '37.62', radiusMeters: '100', requiredVisits: '5',
  startDate: '2026-11-02', lastDate: '2026-11-08', utcOffsetMinutes: '180' });

test('matches the Unity version 1 contract and includes the entire last day', () => {
  assert.deepEqual(buildGeoQuest(draft()), { version: 1, latitude: 55.75, longitude: 37.62,
    radiusMeters: 100, requiredVisits: 5, startsAtUnixSeconds: 1793566800,
    endsAtUnixSeconds: 1794171600, utcOffsetMinutes: 180 });
});
test('comma decimals are accepted; source draft remains unchanged', () => {
  const input = { ...draft(), latitude: '55,75', longitude: '37,62' };
  assert.equal(buildGeoQuest(input).latitude, 55.75);
  assert.equal(input.latitude, '55,75');
});
test('default dates use Moscow calendar days even when UTC date differs', () => {
  const input = newGeoQuestDraft(new Date('2026-11-01T22:00:00Z'));
  assert.equal(input.startDate, '2026-11-02');
  assert.equal(input.lastDate, '2026-11-08');
  assert.equal(input.latitude, '');
});
test('one visit on one inclusive day is possible in a negative UTC offset', () => {
  const geo = buildGeoQuest({ ...draft(), startDate: '2026-11-02', lastDate: '2026-11-02', requiredVisits: '1', utcOffsetMinutes: '-300' });
  assert.equal(new Date(geo.startsAtUnixSeconds * 1000).toISOString(), '2026-11-02T05:00:00.000Z');
  assert.equal(geo.endsAtUnixSeconds - geo.startsAtUnixSeconds, 86400);
});
for (const [field, value] of [
  ['latitude', ''], ['latitude', '91'], ['longitude', '-181'], ['latitude', 'NaN'],
  ['radiusMeters', '0'], ['radiusMeters', 'Infinity'], ['requiredVisits', '2.5'],
  ['requiredVisits', '-1'], ['requiredVisits', '8'], ['utcOffsetMinutes', '841'],
  ['utcOffsetMinutes', '180.5'], ['startDate', '2026-02-30'], ['lastDate', '2026-11-01'],
  ['lastDate', ''], ['startDate', 'not-a-date'],
]) test(`rejects invalid ${field}: ${JSON.stringify(value)}`, () => {
  assert.throws(() => buildGeoQuest({ ...draft(), [field]: value }));
});
test('service validation rejects unknown versions and fractional timestamps', () => {
  assert.throws(() => validateGeoQuest({ ...buildGeoQuest(draft()), version: 2 }));
  assert.throws(() => validateGeoQuest({ ...buildGeoQuest(draft()), startsAtUnixSeconds: 1793566800.5 }));
});
test('exclusive midnight does not add an extra calendar day', () => {
  const geo = buildGeoQuest({ ...draft(), requiredVisits: '7' });
  assert.doesNotThrow(() => validateGeoQuest(geo));
  assert.throws(() => validateGeoQuest({ ...geo, requiredVisits: 8 }));
});
