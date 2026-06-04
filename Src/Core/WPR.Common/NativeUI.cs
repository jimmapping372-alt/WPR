using DesktopNotifications;

namespace WPR.Common
{
    /// <summary>
    /// Cross-platform holder for the active notification manager. The concrete
    /// implementation is constructed and assigned by each UI head (Windows toast in
    /// WPR.UI.Desktop, Android channel notifications in WPR.UI.Android) so the
    /// platform-specific code lives with the platform it targets. Core consumers
    /// (e.g. GamerServices) only ever see <see cref="INotificationManager"/>.
    /// </summary>
    public static class NativeUI
    {
        public static INotificationManager? NotificationManager { get; set; }
    }
}
