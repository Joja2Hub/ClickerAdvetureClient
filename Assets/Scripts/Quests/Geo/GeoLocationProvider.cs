using System;
using System.Collections;
using UnityEngine;

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

    /// <summary>One foreground location request at a time; never starts GPS on scene load.</summary>
    public sealed class GeoLocationProvider : MonoBehaviour
    {
    #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        [SerializeField, Min(1)] private float timeoutSeconds = 20;
    #endif
        [SerializeField, Min(1)] private float maximumAgeSeconds = 30;
    #if UNITY_EDITOR
        [Header("Editor simulation (never used in a device build)")]
        [SerializeField] private double editorLatitude = 55.75;
        [SerializeField] private double editorLongitude = 37.62;
        [SerializeField, Min(0)] private float editorAccuracyMeters = 5;
    #endif
        private bool requesting;
        private bool ownsLocationService;
        private Action<GeoLocationReading> onSuccess;
        private Action<GeoLocationError> onFailure;

        public void RequestLocation(Action<GeoLocationReading> success, Action<GeoLocationError> failure)
        {
            if (requesting)
            {
                failure?.Invoke(GeoLocationError.Busy);
                return;
            }
            if (!isActiveAndEnabled)
            {
                failure?.Invoke(GeoLocationError.Unavailable);
                return;
            }
            requesting = true;
            onSuccess = success;
            onFailure = failure;
            StartCoroutine(ReadLocation());
        }

        private IEnumerator ReadLocation()
        {
    #if UNITY_EDITOR
            yield return null;
            var reading = new GeoLocationReading(editorLatitude, editorLongitude,
                editorAccuracyMeters, DateTimeOffset.UtcNow);
            if (reading.IsUsableAt(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(maximumAgeSeconds)))
                Finish(reading);
            else
                Fail(GeoLocationError.Failed);
    #elif UNITY_ANDROID || UNITY_IOS
            // Unity requests the platform location permission when starting this service.
            // Do not take ownership of another component's active location session.
            if (Input.location.status != LocationServiceStatus.Stopped)
            {
                Fail(GeoLocationError.Busy);
                yield break;
            }
            ownsLocationService = true;
            Input.location.Start(10, 0);
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (Input.location.status == LocationServiceStatus.Failed)
                {
                    Fail(Input.location.isEnabledByUser
                        ? GeoLocationError.Failed : GeoLocationError.DisabledOrDenied);
                    yield break;
                }
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    LocationInfo info = Input.location.lastData;
                    if (!double.IsNaN(info.timestamp) && !double.IsInfinity(info.timestamp)
                        && info.timestamp > 0 && info.timestamp < 253402300799)
                    {
                        var reading = new GeoLocationReading(info.latitude, info.longitude,
                            info.horizontalAccuracy, DateTimeOffset.FromUnixTimeSeconds(0).AddSeconds(info.timestamp));
                        if (reading.IsUsableAt(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(maximumAgeSeconds)))
                        {
                            Finish(reading);
                            yield break;
                        }
                    }
                }
                yield return null;
            }
            Fail(Input.location.isEnabledByUser
                ? GeoLocationError.Timeout : GeoLocationError.DisabledOrDenied);
    #else
            yield return null;
            Fail(GeoLocationError.Unavailable);
    #endif
        }

        private void Finish(GeoLocationReading reading)
        {
            Action<GeoLocationReading> callback = onSuccess;
            Cleanup();
            callback?.Invoke(reading);
        }

        private void Fail(GeoLocationError error)
        {
            Action<GeoLocationError> callback = onFailure;
            Cleanup();
            callback?.Invoke(error);
        }

        private void Cleanup()
        {
            if (ownsLocationService)
                Input.location.Stop();
            ownsLocationService = false;
            requesting = false;
            onSuccess = null;
            onFailure = null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (requesting)
                Fail(GeoLocationError.Cancelled);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && requesting)
            {
                StopAllCoroutines();
                Fail(GeoLocationError.Cancelled);
            }
        }
    }
}

