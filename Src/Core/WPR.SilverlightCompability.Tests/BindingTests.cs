using System.ComponentModel;
using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class BindingTests
    {
        private class PlainVm
        {
            public string Name { get; set; } = "Initial";
            public Nested Nested { get; set; } = new();
        }

        private class Nested
        {
            public string Inner { get; set; } = "deep";
        }

        private class InpcVm : INotifyPropertyChanged
        {
            private string _name = "first";
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Name
            {
                get => _name;
                set
                {
                    if (_name == value) return;
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        [Fact]
        public void MarkupExtensionParser_PositionalAndNamed()
        {
            var p = MarkupExtensionParser.Parse("{Binding Foo}");
            Assert.Equal("Binding", p.TypeName);
            Assert.Equal("Foo", Assert.Single(p.PositionalArgs));

            var q = MarkupExtensionParser.Parse("{Binding Path=Foo, Mode=TwoWay}");
            Assert.Equal("Binding", q.TypeName);
            Assert.Equal("Foo", q.NamedArgs["Path"]);
            Assert.Equal("TwoWay", q.NamedArgs["Mode"]);
        }

        [Fact]
        public void Binding_FromDataContext_PlainObject_AppliesValue()
        {
            var tb = new TextBlock();
            tb.DataContext = new PlainVm { Name = "Hello" };
            tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            Assert.Equal("Hello", tb.Text);
        }

        [Fact]
        public void Binding_DottedPath_Resolves()
        {
            var tb = new TextBlock();
            tb.DataContext = new PlainVm();
            tb.SetBinding(TextBlock.TextProperty, new Binding("Nested.Inner"));
            Assert.Equal("deep", tb.Text);
        }

        [Fact]
        public void Binding_INPC_PropertyChanges_PropagateToTarget()
        {
            var vm = new InpcVm();
            var tb = new TextBlock { DataContext = vm };
            tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            Assert.Equal("first", tb.Text);

            vm.Name = "second";
            Assert.Equal("second", tb.Text);
        }

        [Fact]
        public void Binding_DataContextChange_ReevaluatesBindings()
        {
            var tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            // DataContext is null; nothing to bind to → text stays default (empty).
            Assert.Equal(string.Empty, tb.Text);

            tb.DataContext = new PlainVm { Name = "post" };
            Assert.Equal("post", tb.Text);
        }

        [Fact]
        public void Binding_InheritedFromAncestor_DataContext()
        {
            var page = new ContentControl { DataContext = new PlainVm { Name = "ancestor" } };
            var sp = new StackPanel();
            var tb = new TextBlock();
            sp.Children.Add(tb);
            page.Content = sp;

            tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            Assert.Equal("ancestor", tb.Text);
        }

        [Fact]
        public void XamlLoad_BindingMarkupExtension_AppliesViaDataContext()
        {
            string xaml = @"
<TextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
           Text=""{Binding Path=Name}"" />";
            var tb = (TextBlock)XamlReader.Load(xaml);
            tb.DataContext = new PlainVm { Name = "from-xaml" };
            Assert.Equal("from-xaml", tb.Text);
        }

        [Fact]
        public void Binding_ExplicitSource_OverridesDataContext()
        {
            var explicitSrc = new PlainVm { Name = "explicit" };
            var tb = new TextBlock { DataContext = new PlainVm { Name = "implicit" } };
            tb.SetBinding(TextBlock.TextProperty,
                new Binding("Name") { Source = explicitSrc });
            Assert.Equal("explicit", tb.Text);
        }
    }
}
