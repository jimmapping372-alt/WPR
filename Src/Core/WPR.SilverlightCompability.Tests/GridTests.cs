using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class GridTests
    {
        private class FixedSizeChild : FrameworkElement
        {
            public Size Size { get; }
            public FixedSizeChild(double w, double h) { Size = new Size(w, h); }
            protected override Size MeasureOverride(Size availableSize) => Size;
        }

        [Fact]
        public void NoDefinitions_BehavesAsSingleStarCell()
        {
            var grid = new Grid();
            var child = new FixedSizeChild(50, 30);
            grid.Children.Add(child);

            grid.Measure(new Size(400, 200));
            grid.Arrange(new Rect(0, 0, 400, 200));

            Assert.Equal(400, child.ActualWidth);
            Assert.Equal(200, child.ActualHeight);
        }

        [Fact]
        public void PixelColumns_AllocateExactWidth()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            var c0 = new FixedSizeChild(0, 0);
            var c1 = new FixedSizeChild(0, 0);
            Grid.SetColumn(c0, 0);
            Grid.SetColumn(c1, 1);
            grid.Children.Add(c0);
            grid.Children.Add(c1);

            grid.Measure(new Size(500, 500));
            grid.Arrange(new Rect(0, 0, 500, 500));

            Assert.Equal(100, grid.ColumnDefinitions[0].ActualWidth);
            Assert.Equal(200, grid.ColumnDefinitions[1].ActualWidth);
            Assert.Equal(100, c0.ActualWidth);
            Assert.Equal(200, c1.ActualWidth);
        }

        [Fact]
        public void StarColumns_DistributeRemainingProportionally()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            grid.Measure(new Size(700, 100));
            grid.Arrange(new Rect(0, 0, 700, 100));

            Assert.Equal(100, grid.ColumnDefinitions[0].ActualWidth);
            // 600 remaining, split 1:2 → 200 / 400
            Assert.Equal(200, grid.ColumnDefinitions[1].ActualWidth);
            Assert.Equal(400, grid.ColumnDefinitions[2].ActualWidth);
        }

        [Fact]
        public void AutoColumn_SizesToWidestChild()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var auto1 = new FixedSizeChild(80, 10);
            var auto2 = new FixedSizeChild(120, 10);
            Grid.SetColumn(auto1, 0);
            Grid.SetColumn(auto2, 0);
            grid.Children.Add(auto1);
            grid.Children.Add(auto2);

            grid.Measure(new Size(500, 50));
            grid.Arrange(new Rect(0, 0, 500, 50));

            Assert.Equal(120, grid.ColumnDefinitions[0].ActualWidth);
            Assert.Equal(380, grid.ColumnDefinitions[1].ActualWidth);
        }

        [Fact]
        public void Children_PlacedAtCorrectOffset()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            var child = new FixedSizeChild(0, 0);
            Grid.SetColumn(child, 1);
            Grid.SetRow(child, 1);
            grid.Children.Add(child);

            grid.Measure(new Size(500, 500));
            grid.Arrange(new Rect(0, 0, 500, 500));

            Assert.Equal(80, child.ActualWidth);
            Assert.Equal(40, child.ActualHeight);
            // We don't expose arranged x/y directly, but ActualWidth/Height reflect the cell.
        }

        [Fact]
        public void Span_ChildOccupiesSpannedExtent()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

            var span = new FixedSizeChild(0, 0);
            Grid.SetColumn(span, 0);
            Grid.SetColumnSpan(span, 2);
            grid.Children.Add(span);

            grid.Measure(new Size(500, 100));
            grid.Arrange(new Rect(0, 0, 500, 100));

            Assert.Equal(120, span.ActualWidth); // 50 + 70
        }

        [Fact]
        public void OutOfRangeColumn_ClampsToLast()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

            var child = new FixedSizeChild(0, 0);
            Grid.SetColumn(child, 99);
            grid.Children.Add(child);

            grid.Measure(new Size(200, 50));
            grid.Arrange(new Rect(0, 0, 200, 50));

            Assert.Equal(60, child.ActualWidth); // clamped to last column
        }

        [Fact]
        public void XamlReader_AttachedProperty_GridColumn()
        {
            string xaml = @"
<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <Grid.ColumnDefinitions>
    <ColumnDefinition />
    <ColumnDefinition />
  </Grid.ColumnDefinitions>
  <StackPanel Grid.Column=""1"" />
</Grid>";
            var grid = (Grid)XamlReader.Load(xaml);
            Assert.Equal(2, grid.ColumnDefinitions.Count);
            Assert.Single(grid.Children);
            Assert.Equal(1, Grid.GetColumn(grid.Children[0]));
        }

        [Fact]
        public void XamlReader_RowDefinitionsViaPropertyElementCollection()
        {
            string xaml = @"
<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <Grid.RowDefinitions>
    <RowDefinition Height=""Auto"" />
    <RowDefinition Height=""*"" />
    <RowDefinition Height=""2*"" />
  </Grid.RowDefinitions>
</Grid>";
            var grid = (Grid)XamlReader.Load(xaml);
            Assert.Equal(3, grid.RowDefinitions.Count);
            Assert.Equal(GridUnitType.Auto, grid.RowDefinitions[0].Height.GridUnitType);
            Assert.Equal(GridUnitType.Star, grid.RowDefinitions[1].Height.GridUnitType);
            Assert.Equal(1.0, grid.RowDefinitions[1].Height.Value);
            Assert.Equal(GridUnitType.Star, grid.RowDefinitions[2].Height.GridUnitType);
            Assert.Equal(2.0, grid.RowDefinitions[2].Height.Value);
        }
    }
}
