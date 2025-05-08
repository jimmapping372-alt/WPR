using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WPR.MonoGameCompability.GamerServices
{
    public static class GamerServicesDispatcher
    {

        public static event EventHandler<EventArgs> InstallingTitleUpdate;


        public static void Initialize(IServiceProvider serviceProvider)
        {
        }

        public static void Update()
        {
        }

        public static bool IsInitialized => true;

        /*public static IntPtr WindowHandle
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }*/
        private static IntPtr _WindowsHandle;
        public static IntPtr WindowHandle
        {
            get
            {
                return _WindowsHandle;
            }
            set
            {
                if (_WindowsHandle == null)
                {
                    _WindowsHandle = value;
                }
                else
                {
                    Debug.WriteLine("[i] gamerServices-GamerServicesDispatcher: WindowHandle can only be set once.");
                }
            }
        }
    }
}
