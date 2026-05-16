using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class ImageTests
    {
        [Fact]
        public void Defaults_StretchUniform_SourceNull()
        {
            var img = new Image();
            Assert.Null(img.Source);
            Assert.Equal(Stretch.Uniform, img.Stretch);
        }

        [Fact]
        public void Source_NullOrUnloadable_MeasuresEmpty()
        {
            var img = new Image { Source = "/no/such/path/image.png" };
            img.Measure(new Size(200, 200));
            Assert.True(img.DesiredSize.IsEmpty);
        }

        [Fact]
        public void XamlLoad_AppliesSourceAndStretch()
        {
            string xaml = @"
<Image xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
       Source=""logo.png"" Stretch=""Fill"" />";
            var img = (Image)XamlReader.Load(xaml);
            Assert.Equal("logo.png", img.Source);
            Assert.Equal(Stretch.Fill, img.Stretch);
        }
    }
}
