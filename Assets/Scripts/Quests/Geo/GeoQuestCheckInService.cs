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
        public string RemoteStatus { get; }

        internal GeoCheckInResult(GeoCheckInStatus status, int visitCount = 0,
            GeoVisitResult? visitResult = null, GeoLocationError? locationError = null, string remoteStatus = null)
        {
            Status = status;
            VisitCount = visitCount;
            VisitResult = visitResult;
            LocationError = locationError;
            RemoteStatus = remoteStatus;
        }
    }

    /// <summary>Call on the Unity main thread. Counted requires a local save or server confirmation.</summary>
    public sealed class GeoQuestCheckInService
    {
        private readonly IGeoLocationSource locations;
        private readonly IGeoQuestProgressStore store;
        private readonly IGeoQuestRemoteClient remote;
        private readonly Func<DateTimeOffset> now;
        private readonly TimeSpan maximumAge;
        private bool checking;

        public GeoQuestCheckInService(IGeoLocationSource locations, IGeoQuestProgressStore store,
            Func<DateTimeOffset> now = null, TimeSpan? maximumAge = null, IGeoQuestRemoteClient remote = null)
        {
            this.locations = locations ?? throw new ArgumentNullException(nameof(locations));
            if (store == null && remote == null) throw new ArgumentNullException(nameof(store));
            this.store = store;
            this.remote = remote;
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
                GeoQuestTracker tracker = remote == null ? store.Load(childId, definition) : null;
                var server = remote == null ? null : await remote.LoadGeoProgressAsync(childId, definition);
                int count = server?.VisitCount ?? tracker.VisitCount;
                if (server != null && !server.CanCheckIn)
                    return new GeoCheckInResult(GeoCheckInStatus.VisitChecked, count, remoteStatus: server.Status);
                if (tracker != null && tracker.IsCompleted)
                    return new GeoCheckInResult(GeoCheckInStatus.VisitChecked, tracker.VisitCount, GeoVisitResult.Completed);

                var completion = new TaskCompletionSource<(GeoLocationReading Reading, GeoLocationError? Error)>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                locations.RequestLocation(
                    reading => completion.TrySetResult((reading, null)),
                    error => completion.TrySetResult((null, error)));
                var observation = await completion.Task;
                if (observation.Error.HasValue)
                    return new GeoCheckInResult(GeoCheckInStatus.LocationFailed, count,
                        locationError: observation.Error, remoteStatus: server?.Status);
                var location = observation.Reading;
                if (location == null || !location.IsUsableAt(now(), maximumAge))
                    return new GeoCheckInResult(GeoCheckInStatus.InvalidReading, count, remoteStatus: server?.Status);

                if (remote != null)
                {
                    var confirmed = await remote.CheckInGeoQuestAsync(childId, definition, location);
                    return new GeoCheckInResult(GeoCheckInStatus.VisitChecked, confirmed.VisitCount,
                        confirmed.AlreadyVisitedToday ? GeoVisitResult.AlreadyVisitedToday : GeoVisitResult.Counted,
                        remoteStatus: confirmed.Status);
                }

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
