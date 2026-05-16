using System.Collections.Generic;
using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class ContentControlButtonItemsTests
    {
        [Fact]
        public void ContentControl_UIElementContent_PresenterIsTheElement()
        {
            var sp = new StackPanel();
            var cc = new ContentControl { Content = sp };
            Assert.Same(sp, cc.Presenter);
        }

        [Fact]
        public void ContentControl_StringContent_WrappedInTextBlock()
        {
            var cc = new ContentControl { Content = "hello" };
            var tb = Assert.IsType<TextBlock>(cc.Presenter);
            Assert.Equal("hello", tb.Text);
        }

        [Fact]
        public void ContentControl_ContentChanged_RebuildsPresenter()
        {
            var cc = new ContentControl { Content = "first" };
            var first = cc.Presenter;
            cc.Content = "second";
            Assert.NotSame(first, cc.Presenter);
            Assert.Equal("second", ((TextBlock)cc.Presenter!).Text);
        }

        [Fact]
        public void ContentControl_NullContent_PresenterIsNull()
        {
            var cc = new ContentControl();
            Assert.Null(cc.Presenter);
            cc.Measure(new Size(100, 100));
            Assert.True(cc.DesiredSize.IsEmpty);
        }

        [Fact]
        public void Button_Click_FiresWithSenderAndOriginalSource()
        {
            var btn = new Button { Content = "Press" };
            int count = 0;
            object? sender = null;
            object? source = null;
            btn.Click += (s, e) => { count++; sender = s; source = e.OriginalSource; };

            btn.RaiseClick();

            Assert.Equal(1, count);
            Assert.Same(btn, sender);
            Assert.Same(btn, source);
        }

        [Fact]
        public void HitTester_PointInButton_ReturnsButtonInChain()
        {
            var btn = new Button { Content = "x" };
            var page = new PhoneApplicationPage { Content = btn };

            // Layout
            page.Measure(new Size(200, 100));
            page.Arrange(new Rect(0, 0, 200, 100));

            var chain = HitTester.HitTest(page, 10, 10);
            Assert.Contains(btn, chain);
        }

        [Fact]
        public void HitTester_PointOutsideBounds_EmptyChain()
        {
            var btn = new Button { Content = "x" };
            var page = new PhoneApplicationPage { Content = btn };
            page.Measure(new Size(200, 100));
            page.Arrange(new Rect(0, 0, 200, 100));

            var chain = HitTester.HitTest(page, 999, 999);
            Assert.Empty(chain);
        }

        [Fact]
        public void HitTester_RespectsIsHitTestVisible()
        {
            var btn = new Button { Content = "x", IsHitTestVisible = false };
            var page = new PhoneApplicationPage { Content = btn };
            page.Measure(new Size(200, 100));
            page.Arrange(new Rect(0, 0, 200, 100));

            var chain = HitTester.HitTest(page, 10, 10);
            Assert.DoesNotContain(btn, chain);
        }

        [Fact]
        public void ItemsControl_ItemsSource_PopulatesChildren()
        {
            var ic = new ItemsControl { ItemsSource = new[] { "alpha", "beta", "gamma" } };
            Assert.Equal(3, ic.Children.Count);
            Assert.Equal("alpha", ((TextBlock)ic.Children[0]).Text);
            Assert.Equal("gamma", ((TextBlock)ic.Children[2]).Text);
        }

        [Fact]
        public void ItemsControl_UIElementItems_AddedDirectly()
        {
            var sp1 = new StackPanel();
            var sp2 = new StackPanel();
            var ic = new ItemsControl { ItemsSource = new UIElement[] { sp1, sp2 } };
            Assert.Same(sp1, ic.Children[0]);
            Assert.Same(sp2, ic.Children[1]);
        }

        [Fact]
        public void ItemsControl_NewItemsSource_RebuildsChildren()
        {
            var ic = new ItemsControl { ItemsSource = new[] { "a", "b" } };
            Assert.Equal(2, ic.Children.Count);

            ic.ItemsSource = new[] { "x", "y", "z" };
            Assert.Equal(3, ic.Children.Count);
            Assert.Equal("x", ((TextBlock)ic.Children[0]).Text);
        }

        [Fact]
        public void XamlLoad_ButtonWithStringContent()
        {
            string xaml = @"
<Button xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Content=""OK"" />";
            var btn = (Button)XamlReader.Load(xaml);
            Assert.Equal("OK", btn.Content);
        }

        [Fact]
        public void Page_StillBehavesAsContentControl_AfterRefactor()
        {
            // Sanity: PhoneApplicationPage now extends ContentControl, so its Content
            // can still be set to a UIElement via XAML and Background still applies.
            string xaml = @"
<PhoneApplicationPage xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                      Background=""Black"">
  <StackPanel />
</PhoneApplicationPage>";
            var page = (PhoneApplicationPage)XamlReader.Load(xaml);
            Assert.IsType<StackPanel>(page.Content);
            Assert.IsType<SolidColorBrush>(page.Background);
        }
    }
}
