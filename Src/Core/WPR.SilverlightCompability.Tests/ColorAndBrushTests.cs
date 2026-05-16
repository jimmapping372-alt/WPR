using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class ColorAndBrushTests
    {
        [Fact]
        public void Color_FromArgb_StoresAllChannels()
        {
            var c = Color.FromArgb(0x80, 0x10, 0x20, 0x30);
            Assert.Equal(0x80, c.A);
            Assert.Equal(0x10, c.R);
            Assert.Equal(0x20, c.G);
            Assert.Equal(0x30, c.B);
        }

        [Fact]
        public void Color_FromRgb_DefaultsAlphaToFull()
        {
            var c = Color.FromRgb(0x10, 0x20, 0x30);
            Assert.Equal(0xFF, c.A);
        }

        [Fact]
        public void SolidColorBrush_DefaultColor_IsTransparentDefault()
        {
            var b = new SolidColorBrush();
            Assert.Equal(default(Color), b.Color);
        }

        [Fact]
        public void SolidColorBrush_ConstructorAssignsColor()
        {
            var c = Color.FromArgb(0xFF, 0xAB, 0xCD, 0xEF);
            var b = new SolidColorBrush(c);
            Assert.Equal(c, b.Color);
        }

        [Fact]
        public void Brush_OpacityDefault_IsOne()
        {
            var b = new SolidColorBrush();
            Assert.Equal(1.0, b.Opacity);
        }
    }
}
