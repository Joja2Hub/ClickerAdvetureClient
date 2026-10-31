using System;
using System.Threading.Tasks;

namespace ClickerAdventure
{
    public enum GeoCheckInStatus { VisitChecked, LocationFailed, InvalidReading, Busy }

    public sealed class GeoCheckInResult
    {
        public GeoCheckInStatus Status { get; }
        public GeoVisitResult? VisitResult { get; }
        public GeoLocationError? LocationError { get; }
        public int VisitCount { get; }

        internal GeoCheckInResult(GeoCheckInStatus status, int visitCount = 0,
            GeoVisitResult? visitResult = null, GeoLocationError? locationError = null)
        {
            Status = status;
            VisitCount = visitCount;
            VisitResult = visitResult;
            LocationError = locationError;
        }
    }

    /// <summary>Call on the Unity main thread. A Counted result is returned only after saving.</summary>
    public sealed class GeoQuestCheckInService
    {
        private readonly IGeoLocationSource locations;
        private readonly IGeoQuestProgressStore store;
        private readonly Func<DateTimeOffset> now;
        private readonly TimeSpan maximumAge;
        private bool checking;

        public GeoQuestCheckInService(IGeoLocationSource locations, IGeoQuestProgressStore store,
            Func<DateTimeOffset> now = null, TimeSpan? maximumAge = null)
        {
            this.locations = locations ?? throw new ArgumentNullException(nameof(locations));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.now = now ?? (() => DateTimeOffset.UtcNow);
            this.maximumAge = maximumAge ?? TimeSpan.FromSeconds(30);
            if (this.maximumAge < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumAge));
        }

        public async Task<GeoCheckInResult> CheckInAsync(string childId, GeoQuestDefinition definition)
        {
            if (checking) return new GeoCheckInResult(GeoCheckInStatus.Busy);
            checking = true;
            try
            {
                // Always reload: a failed save must not leave an unsaved visit in this service.
                GeoQuestTracker tracker = store.Load(childId, definition);
                if (tracker.IsCompleted)
                    return new GeoCheckInResult(GeoCheckInStatus.VisitChecked, tracker.VisitCount, GeoVisitResult.Completed);

                var completion = new TaskCompletionSource<(GeoLocationReading Reading, GeoLocationError? Error)>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                locations.RequestLocation(
                    reading => completion.TrySetResult((reading, null)),
                    error => completion.TrySetResult((null, error)));
                var observation = await completion.Task;
                if (observation.Error.HasValue)
                    return new GeoCheckInResult(GeoCheckInStatus.LocationFailed, tracker.VisitCount,
                        locationError: observation.Error);
                var location = observation.Reading;
                if (location == null || !location.IsUsableAt(now(), maximumAge))
                    return new GeoCheckInResult(GeoCheckInStatus.InvalidReading, tracker.VisitCount);

                GeoVisitResult result = tracker.RegisterVisit(location.Latitude, location.Longitude,
                    location.AccuracyMeters, location.ObservedAt);
                if (result == GeoVisitResult.Counted)
                    store.Save(childId, tracker);
                return new GeoCheckInResult(GeoCheckInStatus.VisitChecked, tracker.VisitCount, result);
            }
            finally { checking = false; }
        }
    }
}
