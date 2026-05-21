using System;
using System.Diagnostics;

namespace Microsoft.Phone.Info
{
    public class DeviceExtendedProperties
    {
        public static bool TryGetValue(string propertyName, out Object propertyValue)
        {
            propertyValue = GetValue(propertyName);
            return propertyValue != null;
        }

        public static Object? GetValue(string property)
        {
            switch (property)
            {
                case "DeviceManufacturer":
                    return "WPRunner";

                case "DeviceName":
                    return "WPRunner 2022";

                case "DeviceFirmwareVersion":
                case "DeviceHardwareVersion":
                    return "8.0.0";

                case "DeviceTotalMemory":
                    return 2048L * 1024 * 1024;

                // Per-app memory counters — WP7 games commonly read these for crash
                // telemetry (e.g. PressPlay.Tentacles.MetricsSender.CreateTearDownExtendedKeys
                // calls .ToString() on the value, so returning null here NREs the host on
                // game exit). Long, in bytes, matching the WP7 SDK shape.
                case "ApplicationCurrentMemoryUsage":
                    try { return Process.GetCurrentProcess().WorkingSet64; }
                    catch { return GC.GetTotalMemory(false); }

                case "ApplicationPeakMemoryUsage":
                    try { return Process.GetCurrentProcess().PeakWorkingSet64; }
                    catch { return GC.GetTotalMemory(false); }

                case "ApplicationMemoryUsageLimit":
                    // WP7 hard cap was 90 MB on lowmem devices, 180 MB on full-RAM ones.
                    // Report the higher value so games that gate features on the limit
                    // light them up.
                    return 180L * 1024 * 1024;

                default:
                    return null;
            }
        }
    }
}
