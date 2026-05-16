using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class XamlChildrenTests
    {
        private const string PresentationXmlns =
            "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"";
        private const string XXmlns =
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

        [Fact]
        public void Load_StackPanel_WithDirectChildren_AppendsToChildrenCollection()
        {
            string xaml = $@"
<StackPanel {PresentationXmlns} Orientation=""Horizontal"">
  <StackPanel />
  <StackPanel />
</StackPanel>";
            var sp = (StackPanel)XamlReader.Load(xaml);

            Assert.Equal(Orientation.Horizontal, sp.Orientation);
            Assert.Equal(2, sp.Children.Count);
            Assert.IsType<StackPanel>(sp.Children[0]);
        }

        [Fact]
        public void Load_StackPanel_NestedXName_FindNameTraversesChildren()
        {
            string xaml = $@"
<StackPanel {PresentationXmlns} {XXmlns}>
  <StackPanel x:Name=""inner"" />
</StackPanel>";
            var outer = (StackPanel)XamlReader.Load(xaml);

            object? inner = outer.FindName("inner");
            Assert.NotNull(inner);
            Assert.Same(outer.Children[0], inner);
        }

        [Fact]
        public void Load_DirectChild_OnTypeWithoutContentProperty_Throws()
        {
            // ColumnDefinition has no [ContentProperty]; direct children should fail loudly.
            string xaml = $@"
<ColumnDefinition {PresentationXmlns}>
  <StackPanel />
</ColumnDefinition>";
            Assert.Throws<XamlParseException>(() => XamlReader.Load(xaml));
        }

        [Fact]
        public void Load_StackPanel_WithBackgroundAndChildren_BothApplied()
        {
            string xaml = $@"
<StackPanel {PresentationXmlns} Background=""Red"">
  <StackPanel />
</StackPanel>";
            var sp = (StackPanel)XamlReader.Load(xaml);

            var brush = Assert.IsType<SolidColorBrush>(sp.Background);
            Assert.Equal(Color.FromRgb(0xFF, 0, 0), brush.Color);
            Assert.Single(sp.Children);
        }
    }
}
