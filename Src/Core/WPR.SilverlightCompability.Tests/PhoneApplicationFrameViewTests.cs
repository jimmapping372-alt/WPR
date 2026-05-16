using System;
using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class PhoneApplicationFrameViewTests
    {
        private static PhoneApplicationFrame NewFrame()
        {
            var frame = new PhoneApplicationFrame();
            frame.RegisterPage("Red", typeof(RedPage));
            frame.RegisterPage("Blue", typeof(BluePage));
            return frame;
        }

        private class RedPage : PhoneApplicationPage
        {
            public RedPage()
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00));
            }
        }

        private class BluePage : PhoneApplicationPage
        {
            public BluePage()
            {
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF));
            }
        }

        [Fact]
        public void View_TracksFrameContent_OnNavigate()
        {
            var frame = NewFrame();
            var view = new PhoneApplicationFrameView(frame);

            Assert.Null(view.CurrentPage);

            frame.Navigate(new Uri("/Red.xaml", UriKind.Relative));
            Assert.IsType<RedPage>(view.CurrentPage);

            frame.Navigate(new Uri("/Blue.xaml", UriKind.Relative));
            Assert.IsType<BluePage>(view.CurrentPage);
        }

        [Fact]
        public void View_TracksGoBack()
        {
            var frame = NewFrame();
            var view = new PhoneApplicationFrameView(frame);
            frame.Navigate(new Uri("/Red.xaml", UriKind.Relative));
            frame.Navigate(new Uri("/Blue.xaml", UriKind.Relative));

            frame.GoBack();

            Assert.IsType<RedPage>(view.CurrentPage);
        }

        [Fact]
        public void View_ConstructedWithFrameAlreadyAtPage_PicksUpExistingContent()
        {
            var frame = NewFrame();
            frame.Navigate(new Uri("/Red.xaml", UriKind.Relative));

            var view = new PhoneApplicationFrameView(frame);

            Assert.IsType<RedPage>(view.CurrentPage);
        }

        [Fact]
        public void ConvertBrush_SolidColorBrush_PreservesArgb()
        {
            var slBrush = new SolidColorBrush(Color.FromArgb(0x80, 0x12, 0x34, 0x56));
            var avBrush = PhoneApplicationFrameView.ConvertBrush(slBrush);

            var solid = Assert.IsType<global::Avalonia.Media.SolidColorBrush>(avBrush);
            Assert.Equal(0x80, solid.Color.A);
            Assert.Equal(0x12, solid.Color.R);
            Assert.Equal(0x34, solid.Color.G);
            Assert.Equal(0x56, solid.Color.B);
        }

        [Fact]
        public void ConvertBrush_NullBrush_ReturnsNull()
        {
            Assert.Null(PhoneApplicationFrameView.ConvertBrush(null));
        }

        [Fact]
        public void ConvertBrush_UnknownBrushType_ReturnsNull()
        {
            // No GradientBrush etc. yet — should bail out gracefully.
            var unsupported = new UnknownBrush();
            Assert.Null(PhoneApplicationFrameView.ConvertBrush(unsupported));
        }

        private class UnknownBrush : Brush { }
    }
}
