using System;

namespace WPR.SilverlightCompability
{
    public class NavigationEventArgs : EventArgs
    {
        public object? Content { get; }
        public Uri? Uri { get; }
        public NavigationMode NavigationMode { get; }

        public NavigationEventArgs(object? content, Uri? uri, NavigationMode mode)
        {
            Content = content;
            Uri = uri;
            NavigationMode = mode;
        }
    }

    public delegate void NavigatedEventHandler(object sender, NavigationEventArgs e);
    public delegate void NavigatingCancelEventHandler(object sender, NavigatingCancelEventArgs e);
    public delegate void NavigationFailedEventHandler(object sender, NavigationFailedEventArgs e);
    public delegate void NavigationStoppedEventHandler(object sender, NavigationEventArgs e);
}
