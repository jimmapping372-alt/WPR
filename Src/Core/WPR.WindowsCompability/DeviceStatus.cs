using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using WPR.Common;

namespace WPR.WindowsCompability
{
    // projection: Microsoft.Phone.Info.DeviceStatus
    public class DeviceStatus
    {
        /*private static IsolatedStorageSettings2 _ApplicationSettings;
        private const string LocalSettingsName = "__LocalSettings";

        private IsolatedStorageFile _Holder;
        private static Dictionary<string, object> _Settings;*/

        public DeviceStatus()
        {
        }

        public static string DeviceName
        {
            get
            {
                return "WPRunner 2025";
            }
        }

        public static string DeviceManufacturer
        {
            get
            {
                return "Microsoft"; //"WPRunner"
            }
        }



        ~DeviceStatus()
        {
            //Save();
        }

        public void Save()
        {
            /*using (IsolatedStorageFileStream storage = _Holder.CreateFile(LocalSettingsName))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>));
                serializer.WriteObject(storage, _Settings);
            }*/
        }


      
        //public object this[string key]
        //{
        //    get => _DeviceStatus[key];
        //    set => _DeviceStatus[key] = value;
        //}

        /*public object? this[object key]
        {
            get
            {
                string? keyString = key as string;
                if (keyString == null)
                {
                    return null;
                }
                //if (!_DeviceStatus.ContainsKey(keyString))
                //{
                //    return null;
                //}

                return _DeviceStatus[keyString];
            }
            set
            {
                string? keyString = key as string;
                if (keyString == null)
                {
                    return;
                }
                if (!_Settings.ContainsKey(keyString))
                {
                    return;
                }

                _Settings[keyString] = value!;
            }
        }*/

     
        /*public static bool TryGetValue(string key, out object value)
        {
            //value = true;
            return _DeviceStatus.TryGetValue(key, out value);
        }*/

    }
}
