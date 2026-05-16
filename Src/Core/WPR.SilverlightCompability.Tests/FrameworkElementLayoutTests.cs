using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class FrameworkElementLayoutTests
    {
        private class CapturingElement : FrameworkElement
        {
            public Size LastMeasureAvailable { get; private set; }
            public Size LastArrangeFinal { get; private set; }
            public Size MeasureReturnValue { get; set; } = Size.Empty;

            protected override Size MeasureOverride(Size availableSize)
            {
                LastMeasureAvailable = availableSize;
                return MeasureReturnValue;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                LastArrangeFinal = finalSize;
                return finalSize;
            }
        }

        [Fact]
        public void Measure_WithExplicitWidthHeight_ProducesMatchingDesiredSize()
        {
            var fe = new CapturingElement { Width = 200, Height = 150 };

            fe.Measure(new Size(1000, 1000));

            Assert.Equal(200, fe.DesiredSize.Width);
            Assert.Equal(150, fe.DesiredSize.Height);
            Assert.True(fe.IsMeasureValid);
        }

        [Fact]
        public void Measure_WithMargin_PassesShrunkAvailableSizeToOverride()
        {
            var fe = new CapturingElement
            {
                Margin = new Thickness(10, 20, 30, 40),
            };

            fe.Measure(new Size(1000, 1000));

            Assert.Equal(960, fe.LastMeasureAvailable.Width);
            Assert.Equal(940, fe.LastMeasureAvailable.Height);
        }

        [Fact]
        public void Measure_DesiredSizeAddsMarginBackOntoMeasureOverrideResult()
        {
            var fe = new CapturingElement
            {
                Margin = new Thickness(5),
                MeasureReturnValue = new Size(100, 80),
            };

            fe.Measure(new Size(1000, 1000));

            Assert.Equal(110, fe.DesiredSize.Width);
            Assert.Equal(90, fe.DesiredSize.Height);
        }

        [Fact]
        public void Arrange_StretchAlignment_FillsFinalRect()
        {
            var fe = new CapturingElement();
            fe.Measure(new Size(0, 0));

            fe.Arrange(new Rect(0, 0, 500, 300));

            Assert.Equal(500, fe.ActualWidth);
            Assert.Equal(300, fe.ActualHeight);
            Assert.True(fe.IsArrangeValid);
        }

        [Fact]
        public void Arrange_NonStretchAlignment_ShrinksToDesiredSize()
        {
            var fe = new CapturingElement
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                MeasureReturnValue = new Size(120, 60),
            };
            fe.Measure(new Size(1000, 1000));

            fe.Arrange(new Rect(0, 0, 500, 300));

            Assert.Equal(120, fe.ActualWidth);
            Assert.Equal(60, fe.ActualHeight);
        }

        [Fact]
        public void Arrange_RespectsMargin_ShrinksAvailableSlotForArrangeOverride()
        {
            var fe = new CapturingElement
            {
                Margin = new Thickness(10, 20, 30, 40),
            };
            fe.Measure(new Size(1000, 1000));

            fe.Arrange(new Rect(0, 0, 500, 300));

            // 500 - (10+30) = 460 wide; 300 - (20+40) = 240 tall, with stretch alignment.
            Assert.Equal(460, fe.LastArrangeFinal.Width);
            Assert.Equal(240, fe.LastArrangeFinal.Height);
        }

        [Fact]
        public void Measure_ClampsToMinMaxBounds()
        {
            var fe = new CapturingElement
            {
                MinWidth = 50,
                MaxWidth = 100,
                MeasureReturnValue = new Size(0, 0),
            };

            fe.Measure(new Size(1000, 1000));

            // MeasureOverride received clamped available width.
            Assert.Equal(100, fe.LastMeasureAvailable.Width);
            // DesiredSize at least MinWidth.
            Assert.Equal(50, fe.DesiredSize.Width);
        }
    }
}
