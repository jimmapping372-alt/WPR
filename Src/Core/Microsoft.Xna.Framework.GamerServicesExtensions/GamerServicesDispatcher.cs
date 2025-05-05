using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.GamerServices
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
                    throw new InvalidOperationException("WindowHandle can only be set once.");
                }
            }
        }
    }
}
