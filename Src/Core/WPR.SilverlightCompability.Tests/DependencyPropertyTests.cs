using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class DependencyPropertyTests
    {
        private class Probe : DependencyObject
        {
            public static readonly DependencyProperty FooProperty =
                DependencyProperty.Register("Foo", typeof(int), typeof(Probe),
                    new PropertyMetadata(42));

            public static readonly DependencyProperty BarProperty =
                DependencyProperty.Register("Bar", typeof(string), typeof(Probe),
                    new PropertyMetadata((object?)null));
        }

        [Fact]
        public void GetValue_NotSet_ReturnsMetadataDefault()
        {
            var p = new Probe();
            Assert.Equal(42, p.GetValue(Probe.FooProperty));
        }

        [Fact]
        public void SetValue_FiresChangeCallbackOnce()
        {
            int callCount = 0;
            DependencyProperty dp = DependencyProperty.Register("X", typeof(int), typeof(Probe),
                new PropertyMetadata(0, (d, e) => callCount++));

            var p = new Probe();
            p.SetValue(dp, 7);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void SetValue_SameValue_DoesNotRefireCallback()
        {
            int callCount = 0;
            DependencyProperty dp = DependencyProperty.Register("Y", typeof(int), typeof(Probe),
                new PropertyMetadata(0, (d, e) => callCount++));

            var p = new Probe();
            p.SetValue(dp, 5);
            p.SetValue(dp, 5);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void ClearValue_RestoresDefault()
        {
            var p = new Probe();
            p.SetValue(Probe.FooProperty, 100);
            Assert.Equal(100, p.GetValue(Probe.FooProperty));

            p.ClearValue(Probe.FooProperty);
            Assert.Equal(42, p.GetValue(Probe.FooProperty));
        }

        [Fact]
        public void ClearValue_FiresCallbackWithDefaultAsNewValue()
        {
            int callCount = 0;
            object? observedNew = null;
            DependencyProperty dp = DependencyProperty.Register("Z", typeof(int), typeof(Probe),
                new PropertyMetadata(11, (d, e) =>
                {
                    callCount++;
                    observedNew = e.NewValue;
                }));

            var p = new Probe();
            p.SetValue(dp, 99);
            p.ClearValue(dp);

            Assert.Equal(2, callCount);
            Assert.Equal(11, observedNew);
        }

        [Fact]
        public void GetValue_NullableReferenceDefault_ReturnsNull()
        {
            var p = new Probe();
            Assert.Null(p.GetValue(Probe.BarProperty));
        }
    }
}
