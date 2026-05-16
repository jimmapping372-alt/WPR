using System;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Shim for <c>System.Windows.Controls.ListBox</c>. In Silverlight the chain is
    /// <c>ItemsControl &lt;- Selector &lt;- ListBox</c>; we collapse <c>Selector</c>
    /// onto <c>ListBox</c> since the renderer doesn't currently need a separate
    /// selector base. Visual is the inherited <see cref="ItemsControl"/> StackPanel
    /// over <see cref="ItemsControl.ItemsSource"/>; click-to-select isn't wired up
    /// yet, but the API surface (<see cref="SelectedIndex"/>, <see cref="SelectedItem"/>,
    /// <see cref="SelectionChanged"/>) exists so user IL and XAML JIT cleanly.
    /// </summary>
    public class ListBox : ItemsControl
    {
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(ListBox),
                new PropertyMetadata((object)(-1), OnSelectedIndexChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(ListBox),
                new PropertyMetadata((object?)null, OnSelectedItemChanged));

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(nameof(SelectionMode), typeof(SelectionMode), typeof(ListBox),
                new PropertyMetadata(SelectionMode.Single));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty)!;
            set => SetValue(SelectedIndexProperty, value);
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public SelectionMode SelectionMode
        {
            get => (SelectionMode)GetValue(SelectionModeProperty)!;
            set => SetValue(SelectionModeProperty, value);
        }

        // Suppress CS0067 — we declare these for source compatibility but our
        // renderer doesn't currently raise selection events.
#pragma warning disable CS0067
        public event SelectionChangedEventHandler? SelectionChanged;
#pragma warning restore CS0067

        // Prevent re-entrancy when one of SelectedIndex / SelectedItem is set in
        // response to the other changing.
        private bool _syncing;

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox lb || lb._syncing) return;
            lb._syncing = true;
            try
            {
                int idx = e.NewValue is int i ? i : -1;
                if (idx < 0)
                {
                    lb.SelectedItem = null;
                    return;
                }
                int j = 0;
                if (lb.ItemsSource != null)
                {
                    foreach (object? item in lb.ItemsSource)
                    {
                        if (j == idx) { lb.SelectedItem = item; return; }
                        j++;
                    }
                }
                lb.SelectedItem = null;
            }
            finally { lb._syncing = false; }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox lb || lb._syncing) return;
            lb._syncing = true;
            try
            {
                object? target = e.NewValue;
                if (target == null) { lb.SelectedIndex = -1; return; }
                int j = 0;
                if (lb.ItemsSource != null)
                {
                    foreach (object? item in lb.ItemsSource)
                    {
                        if (Equals(item, target)) { lb.SelectedIndex = j; return; }
                        j++;
                    }
                }
                lb.SelectedIndex = -1;
            }
            finally { lb._syncing = false; }
        }
    }

    /// <summary>Shim for <c>System.Windows.Controls.SelectionMode</c>.</summary>
    public enum SelectionMode
    {
        Single,
        Multiple,
        Extended,
    }

    /// <summary>Shim for <c>System.Windows.Controls.SelectionChangedEventHandler</c>.</summary>
    public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

    /// <summary>
    /// Shim for <c>System.Windows.Controls.SelectionChangedEventArgs</c>.
    /// <see cref="AddedItems"/> / <see cref="RemovedItems"/> mirror SL semantics;
    /// the lists are populated when our renderer eventually raises the event.
    /// </summary>
    public class SelectionChangedEventArgs : RoutedEventArgs
    {
        public System.Collections.IList AddedItems { get; }
        public System.Collections.IList RemovedItems { get; }

        public SelectionChangedEventArgs(System.Collections.IList removedItems, System.Collections.IList addedItems)
        {
            RemovedItems = removedItems;
            AddedItems = addedItems;
        }

        public SelectionChangedEventArgs()
            : this(Array.Empty<object>(), Array.Empty<object>())
        {
        }
    }
}
