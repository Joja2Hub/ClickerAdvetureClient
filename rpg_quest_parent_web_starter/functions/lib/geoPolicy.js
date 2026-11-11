"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GeoPolicyError = void 0;
exports.assertCompletedGeoQuest = assertCompletedGeoQuest;
exports.evaluateGeoVisit = evaluateGeoVisit;
const node_crypto_1 = require("node:crypto");
class GeoPolicyError extends Error {
    constructor(code, message) {
        super(message);
        this.code = code;
    }
}
exports.GeoPolicyError = GeoPolicyError;
function object(value) {
    if (!value || typeof value !== "object" || Array.isArray(value))
        throw new GeoPolicyError("invalid-argument", "Некорректные геоданные.");
    return value;
}
function number(data, field, min, max, integer = false) {
    const value = data[field];
    if (typeof value !== "number" || !Number.isFinite(value) || value < min || value > max
        || (integer && !Number.isInteger(value)))
        throw new GeoPolicyError("invalid-argument", `Некорректное поле: ${field}.`);
    return value;
}
function readConditions(conditions) {
    const geo = object(conditions);
    if (geo.version !== 1)
        throw new GeoPolicyError("failed-precondition", "Неизвестная версия геоквеста.");
    const latitude = number(geo, "latitude", -90, 90);
    const longitude = number(geo, "longitude", -180, 180);
    const radius = number(geo, "radiusMeters", Number.MIN_VALUE, Number.MAX_VALUE);
    const goal = number(geo, "requiredVisits", 1, 2147483647, true);
    const offset = number(geo, "utcOffsetMinutes", -840, 840, true);
    const start = number(geo, "startsAtUnixSeconds", -62135596800, 253402300799, true);
    const end = number(geo, "endsAtUnixSeconds", -62135596800, 253402300799, true);
    if (end <= start)
        throw new GeoPolicyError("failed-precondition", "Некорректный срок задания.");
    const definitionHash = (0, node_crypto_1.createHash)("sha256").update(JSON.stringify([
        1, latitude, longitude, radius, goal, start, end, offset,
    ])).digest("hex");
    return { latitude, longitude, radius, goal, offset, start, end, definitionHash };
}
function assertCompletedGeoQuest(conditions, progress) {
    const { goal, definitionHash } = readConditions(conditions);
    const stored = object(progress);
    if (stored.visitCount !== goal || stored.definitionHash !== definitionHash) {
        throw new GeoPolicyError("failed-precondition", "Посещения по текущим условиям ещё не подтверждены.");
    }
}
function evaluateGeoVisit(conditions, observation, serverNow) {
    const { latitude, longitude, radius, goal, offset, start, end, definitionHash } = readConditions(conditions);
    const reading = object(observation);
    const lat = number(reading, "latitude", -90, 90);
    const lon = number(reading, "longitude", -180, 180);
    const accuracy = number(reading, "accuracyMeters", 0, Number.MAX_VALUE);
    const observedAt = number(reading, "observedAtUnixSeconds", -62135596800, 253402300799, true);
    if (observedAt > serverNow || serverNow - observedAt > 30)
        throw new GeoPolicyError("failed-precondition", "Нужны свежие координаты. Повторите отметку.");
    if (serverNow < start || serverNow >= end || observedAt < start || observedAt >= end)
        throw new GeoPolicyError("failed-precondition", "Задание вне срока выполнения.");
    const radians = Math.PI / 180;
    const a = Math.sin((lat - latitude) * radians / 2) ** 2
        + Math.cos(latitude * radians) * Math.cos(lat * radians) * Math.sin((lon - longitude) * radians / 2) ** 2;
    const distance = 6371000 * 2 * Math.asin(Math.sqrt(Math.max(0, Math.min(1, a))));
    if (accuracy > radius || distance + accuracy > radius)
        throw new GeoPolicyError("failed-precondition", "Точка с учётом погрешности находится вне радиуса задания.");
    const localTime = observedAt + offset * 60;
    if (localTime < -62135596800 || localTime > 253402300799)
        throw new GeoPolicyError("invalid-argument", "Некорректное местное время.");
    const day = new Date(localTime * 1000).toISOString().slice(0, 10);
    return { day, definitionHash, goal, distance, observedAt };
}
//# sourceMappingURL=geoPolicy.js.map