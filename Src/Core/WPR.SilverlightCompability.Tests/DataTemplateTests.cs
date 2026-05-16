using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class DataTemplateTests
    {
        private class Item
        {
            public string Title { get; set; } = "";
        }

        [Fact]
        public void DataTemplate_LoadContent_InstantiatesNewVisualEachCall()
        {
            string xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <TextBlock Text=""static"" />
</DataTemplate>";
            var template = (DataTemplate)XamlReader.Load(xaml);
            var a = template.LoadContent();
            var b = template.LoadContent();
            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.NotSame(a, b);
            Assert.Equal("static", ((TextBlock)a!).Text);
        }

        [Fact]
        public void DataTemplate_WithBinding_ResolvesAgainstDataContext()
        {
            string xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <TextBlock Text=""{Binding Title}"" />
</DataTemplate>";
            var template = (DataTemplate)XamlReader.Load(xaml);

            var tb = (TextBlock)template.LoadContent()!;
            tb.DataContext = new Item { Title = "Hello" };
            Assert.Equal("Hello", tb.Text);
        }

        [Fact]
        public void ItemsControl_WithItemTemplate_GeneratesPerItemContainer()
        {
            string xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <TextBlock Text=""{Binding Title}"" />
</DataTemplate>";
            var template = (DataTemplate)XamlReader.Load(xaml);

            var items = new[]
            {
                new Item { Title = "alpha" },
                new Item { Title = "beta" },
            };

            var ic = new ItemsControl { ItemTemplate = template, ItemsSource = items };

            Assert.Equal(2, ic.Children.Count);
            Assert.Equal("alpha", ((TextBlock)ic.Children[0]).Text);
            Assert.Equal("beta", ((TextBlock)ic.Children[1]).Text);
        }

        [Fact]
        public void ItemsControl_NoItemTemplate_FallsBackToToString()
        {
            var ic = new ItemsControl { ItemsSource = new[] { "x", "y" } };
            Assert.Equal("x", ((TextBlock)ic.Children[0]).Text);
        }
    }
}
