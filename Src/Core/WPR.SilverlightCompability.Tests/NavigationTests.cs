using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class NavigationTests
    {
        private static PhoneApplicationFrame NewFrame()
        {
            var frame = new PhoneApplicationFrame();
            frame.RegisterPage("MainPage", typeof(MainPage));
            frame.RegisterPage("SettingsPage", typeof(SettingsPage));
            frame.RegisterPage("DetailsPage", typeof(DetailsPage));
            return frame;
        }

        private class MainPage : PhoneApplicationPage
        {
            public List<string> Lifecycle { get; } = new();
            protected internal override void OnNavigatedTo(NavigationEventArgs e) => Lifecycle.Add("To");
            protected internal override void OnNavigatedFrom(NavigationEventArgs e) => Lifecycle.Add("From");
            protected internal override void OnNavigatingFrom(NavigatingCancelEventArgs e) => Lifecycle.Add("Navigating");
        }

        private class SettingsPage : PhoneApplicationPage { }
        private class DetailsPage : PhoneApplicationPage { }

        [Fact]
        public void Navigate_ResolvesPageFromUriAndSetsContent()
        {
            var frame = NewFrame();

            bool ok = frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));

            Assert.True(ok);
            Assert.IsType<MainPage>(frame.Content);
            Assert.Equal(new Uri("/MainPage.xaml", UriKind.Relative), frame.CurrentSource);
        }

        [Fact]
        public void Navigate_PushesCurrentToBackStack_AndClearsForwardStack()
        {
            var frame = NewFrame();
            frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            frame.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));

            Assert.True(frame.CanGoBack);
            Assert.Single(frame.BackStack);
            Assert.False(frame.CanGoForward);

            frame.GoBack();
            Assert.True(frame.CanGoForward);

            frame.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            Assert.False(frame.CanGoForward); // forward stack cleared
        }

        [Fact]
        public void GoBack_ReturnsToPreviousUri()
        {
            var frame = NewFrame();
            frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            frame.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));

            frame.GoBack();

            Assert.IsType<MainPage>(frame.Content);
            Assert.Equal(new Uri("/MainPage.xaml", UriKind.Relative), frame.CurrentSource);
            Assert.False(frame.CanGoBack);
            Assert.True(frame.CanGoForward);
        }

        [Fact]
        public void GoBack_OnEmptyStack_Throws()
        {
            var frame = NewFrame();
            Assert.Throws<InvalidOperationException>(() => frame.GoBack());
        }

        [Fact]
        public void NavigatingCancelled_BlocksNavigation()
        {
            var frame = NewFrame();
            frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));

            frame.Navigating += (s, e) => e.Cancel = true;
            bool ok = frame.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));

            Assert.False(ok);
            Assert.IsType<MainPage>(frame.Content);
            Assert.Empty(frame.BackStack);
        }

        [Fact]
        public void Navigate_ToUnregisteredPage_RaisesNavigationFailed()
        {
            var frame = NewFrame();
            NavigationFailedEventArgs? capturedArgs = null;
            frame.NavigationFailed += (s, e) => { capturedArgs = e; e.Handled = true; };

            bool ok = frame.Navigate(new Uri("/DoesNotExist.xaml", UriKind.Relative));

            Assert.False(ok);
            Assert.NotNull(capturedArgs);
            Assert.NotNull(capturedArgs!.Exception);
            Assert.Null(frame.Content);
        }

        [Fact]
        public void PageLifecycle_FiresInOrder_OnNavigateAway()
        {
            var frame = NewFrame();
            frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            var main = (MainPage)frame.Content!;
            Assert.Equal(new[] { "To" }, main.Lifecycle);

            frame.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));

            Assert.Equal(new[] { "To", "Navigating", "From" }, main.Lifecycle);
        }

        [Fact]
        public void Page_NavigationServiceSetWhileCurrent_AndClearedAfterLeaving()
        {
            var frame = NewFrame();
            frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            var main = (MainPage)frame.Content!;

            Assert.Same(frame.NavigationService, main.NavigationService);

            frame.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));

            Assert.Null(main.NavigationService);
        }

        [Fact]
        public void GoForward_RestoresUri_AndPopsForwardStack()
        {
            var frame = NewFrame();
            frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            frame.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
            frame.GoBack();

            frame.GoForward();

            Assert.Equal(new Uri("/SettingsPage.xaml", UriKind.Relative), frame.CurrentSource);
            Assert.True(frame.CanGoBack);
            Assert.False(frame.CanGoForward);
        }
    }
}
