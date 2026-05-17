using DesktopNotifications;
using DesktopNotifications.Windows;

#if __ANDROID__
using DesktopNotifications.Android;
#endif

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WPR.Common
{
    public static class NativeUI
    {
        // Branding shown by Windows in the toast notification chrome. Also doubles as
        // the AppUserModelID and as the Start Menu shortcut filename, so it has to
        // be a string that's valid in all three contexts.
        private const string WindowsAppDisplayName = "Windows Phone Runner";

        public static INotificationManager NotificationManager { get; set; }

        public static void Initialize(object hostControl = null)
        {
//#if __ANDROID__
//            NotificationManager = new AndroidNotificationManager((hostControl as Android.Content.Context)!);
//#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // FreeDesktopNotificationManager is not available in this build; leave null
                NotificationManager = null;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Write a 1×1 transparent .ico so the toast chrome's icon slot renders
                // empty rather than the .exe's blank/default icon placeholder. Windows
                // toast XML doesn't expose a "hide app icon" knob — the only way to
                // suppress the visual is to make the underlying icon transparent.
                string? iconPath = TryWriteBlankIcon();

                var ctx = WindowsApplicationContext.FromCurrentProcess(
                    customName: WindowsAppDisplayName,
                    iconPath: iconPath);
                NotificationManager = new WindowsNotificationManager(ctx);

                // Remove the stale "WPR.UI.Desktop.lnk" from older builds that used the
                // .exe filename as the app name. Without this, the Start Menu / search
                // still shows the old entry alongside the new one.
                TryRemoveLegacyShortcut("WPR.UI.Desktop");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // AppleNotificationManager is not available in this build; leave null
                NotificationManager = null;
            }
            else
            {
                // Unknown platform
                NotificationManager = null;
            }
//#endif
            //NotificationManager.Initialize();
        }

        /// <summary>
        /// Write a minimal 1×1 fully-transparent ICO to <c>%LOCALAPPDATA%\WPR\blank.ico</c>
        /// and return its path. Used as the Start Menu shortcut's <c>IconLocation</c> so the
        /// toast notification's chrome glyph renders empty rather than showing the
        /// default-Avalonia placeholder bundled into the .exe.
        /// 70 bytes total: 6-byte ICONDIR + 16-byte ICONDIRENTRY + 40-byte BITMAPINFOHEADER
        /// + 4-byte transparent BGRA pixel + 4-byte AND mask.
        /// Returns null on any failure (caller falls back to no icon override).
        /// </summary>
        private static string? TryWriteBlankIcon()
        {
            try
            {
                string icoPath = Configuration.Current?.DataPath("blank.ico")
                                 ?? Path.Combine(Path.GetTempPath(), "wpr-blank.ico");
                Directory.CreateDirectory(Path.GetDirectoryName(icoPath)!);
                if (File.Exists(icoPath) && new FileInfo(icoPath).Length == 70)
                    return icoPath;

                byte[] ico = new byte[]
                {
                    // ICONDIR
                    0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
                    // ICONDIRENTRY: w=1, h=1, colors=0, _, planes=1, bpp=32, bytes=48, offset=22
                    0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x20, 0x00,
                    0x30, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00,
                    // BITMAPINFOHEADER (size=40, w=1, h=2 (image+mask doubled), planes=1,
                    // bpp=32, rest zeros)
                    0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                    0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x20, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // XOR pixel: BGRA 0,0,0,0 (fully transparent)
                    0x00, 0x00, 0x00, 0x00,
                    // AND mask: 1 row, 4 bytes (4-byte aligned), all zero (show alpha)
                    0x00, 0x00, 0x00, 0x00,
                };

                File.WriteAllBytes(icoPath, ico);
                return icoPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Best-effort removal of an obsolete Start Menu shortcut written by an earlier
        /// build under a different name. Swallow all errors — the shortcut may not
        /// exist, or the user may have moved it.
        /// </summary>
        private static void TryRemoveLegacyShortcut(string oldAppName)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string lnk = Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs", $"{oldAppName}.lnk");
                if (File.Exists(lnk)) File.Delete(lnk);
            }
            catch { /* best-effort */ }
        }
    }
}
