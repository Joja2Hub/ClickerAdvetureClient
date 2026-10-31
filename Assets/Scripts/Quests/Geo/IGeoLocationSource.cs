using System;

namespace ClickerAdventure
{
    public enum GeoLocationError
    {
        Unavailable,
        Busy,
        DisabledOrDenied,
        Failed,
        Timeout,
        Cancelled
    }

    public interface IGeoLocationSource
    {
        void RequestLocation(Action<GeoLocationReading> success, Action<GeoLocationError> failure);
    }
}
