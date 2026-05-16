using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class PageContentTests
    {
        [Fact]
        public void Page_Measure_DelegatesToContent()
        {
            var page = new PhoneApplicationPage();
            var stack = new StackPanel();
            // Add a fixed-size leaf so the stack reports a deterministic size.
            stack.Children.Add(new SizedLeaf(50, 30));
            stack.Children.Add(new SizedLeaf(80, 20));
            page.Content = stack;

            page.Measure(new Size(1000, 1000));

            // StackPanel default is Vertical: total height 50, max width 80.
            Assert.Equal(80, page.DesiredSize.Width);
            Assert.Equal(50, page.DesiredSize.Height);
        }

        [Fact]
        public void Page_Arrange_PlacesContent_AtArrangedRect()
        {
            var page = new PhoneApplicationPage();
            var stack = new StackPanel();
            page.Content = stack;

            page.Measure(new Size(400, 300));
            page.Arrange(new Rect(0, 0, 400, 300));

            Assert.Equal(0, stack.ArrangedRect.X);
            Assert.Equal(0, stack.ArrangedRect.Y);
            Assert.Equal(400, stack.ArrangedRect.Width);
            Assert.Equal(300, stack.ArrangedRect.Height);
        }

        [Fact]
        public void Page_FindName_TraversesIntoContent()
        {
            string xaml = @"
<PhoneApplicationPage xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                      xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <StackPanel x:Name=""root"">
    <TextBlock x:Name=""greeting"" Text=""Hi"" />
  </StackPanel>
</PhoneApplicationPage>";
            var page = (PhoneApplicationPage)XamlReader.Load(xaml);

            Assert.IsType<TextBlock>(page.FindName("greeting"));
            Assert.IsType<StackPanel>(page.FindName("root"));
        }

        [Fact]
        public void Page_DirectXamlChild_BecomesContent()
        {
            string xaml = @"
<PhoneApplicationPage xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                      Background=""Black"">
  <StackPanel />
</PhoneApplicationPage>";
            var page = (PhoneApplicationPage)XamlReader.Load(xaml);
            Assert.IsType<StackPanel>(page.Content);
        }

        [Fact]
        public void ArrangedRect_OnLeafChild_ReflectsParentLayout()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            var child = new SizedLeaf(0, 0);
            Grid.SetColumn(child, 1);
            grid.Children.Add(child);

            grid.Measure(new Size(500, 100));
            grid.Arrange(new Rect(0, 0, 500, 100));

            Assert.Equal(50, child.ArrangedRect.X); // offset of column 1 = width of column 0
            Assert.Equal(0, child.ArrangedRect.Y);
            Assert.Equal(80, child.ArrangedRect.Width);
            Assert.Equal(40, child.ArrangedRect.Height);
        }

        private class SizedLeaf : FrameworkElement
        {
            private readonly Size _size;
            public SizedLeaf(double w, double h) { _size = new Size(w, h); }
            protected override Size MeasureOverride(Size availableSize) => _size;
        }
    }
}
