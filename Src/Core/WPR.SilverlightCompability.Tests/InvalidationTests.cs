using System;
using System.Collections.Generic;
using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class InvalidationTests
    {
        private class Leaf : FrameworkElement
        {
            protected override Size MeasureOverride(Size availableSize) => new Size(10, 10);
        }

        [Fact]
        public void Children_Add_SetsParent_AndClearsOnRemove()
        {
            var sp = new StackPanel();
            var child = new Leaf();

            sp.Children.Add(child);
            Assert.Same(sp, child.Parent);

            sp.Children.Remove(child);
            Assert.Null(child.Parent);
        }

        [Fact]
        public void DoubleAttach_ToDifferentParents_Throws()
        {
            var a = new StackPanel();
            var b = new StackPanel();
            var child = new Leaf();
            a.Children.Add(child);

            Assert.Throws<InvalidOperationException>(() => b.Children.Add(child));
        }

        [Fact]
        public void InvalidateMeasure_OnLeaf_PropagatesUpToRoot()
        {
            var leaf = new Leaf();
            var inner = new StackPanel();
            var outer = new StackPanel();
            inner.Children.Add(leaf);
            outer.Children.Add(inner);

            outer.Measure(new Size(100, 100));
            inner.Measure(new Size(100, 100));
            leaf.Measure(new Size(100, 100));
            Assert.True(outer.IsMeasureValid);
            Assert.True(inner.IsMeasureValid);

            leaf.InvalidateMeasure();

            Assert.False(leaf.IsMeasureValid);
            Assert.False(inner.IsMeasureValid);
            Assert.False(outer.IsMeasureValid);
        }

        [Fact]
        public void RootInvalidation_FiresMeasureInvalidatedEvent()
        {
            var root = new StackPanel();
            var leaf = new Leaf();
            root.Children.Add(leaf);

            root.Measure(new Size(100, 100));
            leaf.Measure(new Size(100, 100));

            int eventCount = 0;
            root.MeasureInvalidatedEvent += (s, e) => eventCount++;

            leaf.InvalidateMeasure();

            Assert.True(eventCount >= 1);
        }

        [Fact]
        public void ContentControl_PresenterReparented_OnContentChange()
        {
            var cc = new ContentControl();
            var first = new StackPanel();
            cc.Content = first;
            Assert.Same(cc, first.Parent);

            var second = new StackPanel();
            cc.Content = second;
            Assert.Null(first.Parent);
            Assert.Same(cc, second.Parent);
        }
    }
}
