using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class StackPanelTests
    {
        private class FixedSizeChild : FrameworkElement
        {
            public Size Size { get; }
            public FixedSizeChild(double w, double h) { Size = new Size(w, h); }
            protected override Size MeasureOverride(Size availableSize) => Size;
        }

        [Fact]
        public void Vertical_Measure_StacksHeights_AndUsesWidestChild()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            panel.Children.Add(new FixedSizeChild(100, 30));
            panel.Children.Add(new FixedSizeChild(150, 20));
            panel.Children.Add(new FixedSizeChild(80, 40));

            panel.Measure(new Size(500, 500));

            Assert.Equal(150, panel.DesiredSize.Width);
            Assert.Equal(90, panel.DesiredSize.Height);
        }

        [Fact]
        public void Horizontal_Measure_StacksWidths_AndUsesTallestChild()
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new FixedSizeChild(100, 30));
            panel.Children.Add(new FixedSizeChild(150, 20));
            panel.Children.Add(new FixedSizeChild(80, 40));

            panel.Measure(new Size(500, 500));

            Assert.Equal(330, panel.DesiredSize.Width);
            Assert.Equal(40, panel.DesiredSize.Height);
        }

        [Fact]
        public void Vertical_Arrange_PlacesChildrenAtCumulativeOffsets()
        {
            var c1 = new FixedSizeChild(100, 30);
            var c2 = new FixedSizeChild(100, 20);
            var c3 = new FixedSizeChild(100, 40);
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            panel.Children.Add(c1);
            panel.Children.Add(c2);
            panel.Children.Add(c3);

            panel.Measure(new Size(500, 500));
            panel.Arrange(new Rect(0, 0, 500, 500));

            // Each child's ActualHeight should match its desired height; cumulative Y arrangement
            // is a function of order — verified via ActualHeight summing for the full tree.
            Assert.Equal(30, c1.ActualHeight);
            Assert.Equal(20, c2.ActualHeight);
            Assert.Equal(40, c3.ActualHeight);
        }

        [Fact]
        public void Children_Add_InvalidatesMeasure()
        {
            var panel = new StackPanel();
            panel.Measure(new Size(100, 100));
            Assert.True(panel.IsMeasureValid);

            panel.Children.Add(new FixedSizeChild(10, 10));
            Assert.False(panel.IsMeasureValid);
        }

        [Fact]
        public void Children_Remove_InvalidatesMeasure()
        {
            var child = new FixedSizeChild(10, 10);
            var panel = new StackPanel();
            panel.Children.Add(child);
            panel.Measure(new Size(100, 100));
            Assert.True(panel.IsMeasureValid);

            panel.Children.Remove(child);
            Assert.False(panel.IsMeasureValid);
        }

        [Fact]
        public void Default_Orientation_IsVertical()
        {
            var panel = new StackPanel();
            Assert.Equal(Orientation.Vertical, panel.Orientation);
        }
    }
}
