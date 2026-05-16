using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class CanvasTests
    {
        private class SizedLeaf : FrameworkElement
        {
            private readonly Size _size;
            public SizedLeaf(double w, double h) { _size = new Size(w, h); }
            protected override Size MeasureOverride(Size availableSize) => _size;
        }

        [Fact]
        public void ChildrenPlacedAtAttachedLeftTop()
        {
            var canvas = new Canvas();
            var c1 = new SizedLeaf(20, 30);
            var c2 = new SizedLeaf(40, 10);
            Canvas.SetLeft(c1, 100);
            Canvas.SetTop(c1, 50);
            Canvas.SetLeft(c2, 200);
            Canvas.SetTop(c2, 80);
            canvas.Children.Add(c1);
            canvas.Children.Add(c2);

            canvas.Measure(new Size(500, 500));
            canvas.Arrange(new Rect(0, 0, 500, 500));

            Assert.Equal(100, c1.ArrangedRect.X);
            Assert.Equal(50, c1.ArrangedRect.Y);
            Assert.Equal(20, c1.ArrangedRect.Width);
            Assert.Equal(30, c1.ArrangedRect.Height);

            Assert.Equal(200, c2.ArrangedRect.X);
            Assert.Equal(80, c2.ArrangedRect.Y);
        }

        [Fact]
        public void ChildWithoutAttachedProps_PlacedAtOrigin()
        {
            var canvas = new Canvas();
            var child = new SizedLeaf(50, 50);
            canvas.Children.Add(child);

            canvas.Measure(new Size(500, 500));
            canvas.Arrange(new Rect(0, 0, 500, 500));

            Assert.Equal(0, child.ArrangedRect.X);
            Assert.Equal(0, child.ArrangedRect.Y);
        }

        [Fact]
        public void XamlAttachedProperties_Resolved()
        {
            string xaml = @"
<Canvas xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <StackPanel Canvas.Left=""25"" Canvas.Top=""75"" />
</Canvas>";
            var canvas = (Canvas)XamlReader.Load(xaml);
            Assert.Single(canvas.Children);
            Assert.Equal(25, Canvas.GetLeft(canvas.Children[0]));
            Assert.Equal(75, Canvas.GetTop(canvas.Children[0]));
        }
    }
}
