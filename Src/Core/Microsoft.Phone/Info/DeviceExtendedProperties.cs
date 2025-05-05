using System;

namespace Microsoft.Phone.Info
{
    public class DeviceExtendedProperties
    {
        public DeviceExtendedProperties()
        {
        }

        public static bool TryGetValue(string propertyName, out Object propertyValue)
        {
            propertyValue = GetValue(propertyName);
            return true;
        }

        public static string DeviceManufacturer
        {
            get
            {
                return "Microsoft";
            }
        }

        //public string get_DeviceManufacturer()
        //{
        //    string ManufacturerName = "WPRunner";//"Microsoft";
        //    return ManufacturerName;
        //}

        public static Object? GetValue(string property)
        {
            switch (property)
            {
                case "DeviceManufacturer":
                    return "WPRunner";

                case "DeviceName":
                    return "WPRunner 2023";

                case "DeviceFirmwareVersion":
                case "DeviceHardwareVersion":
                    return "8.0.0";

                case "DeviceTotalMemory":

                    // Return 2GB RAM
                    //return 2048L * 1024 * 1024;
                    return 4096L * 1024 * 1024;

                case "ApplicationMemoryLimit":

                    // Return 512MB RAM //2GB RAM
                    //return 512L * 1024 * 1024; 
                    return 2048L * 1024 * 1024;

                case "ApplicationWorkingSetLimit":
                    // Return 1GB RAM
                    //return 1024L * 1024 * 1024; 
                    return 2048L * 1024 * 1024;

                case "DeviceStatus":
                    return "ok";//default;

                case "DeviceUniqueId": // A byte array. 20 bytes in length.
                    return "12345678901234567890";

                default:
                    return null;
            }
        }
    }
}
