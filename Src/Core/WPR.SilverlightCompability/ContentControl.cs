namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Single-content host. <see cref="Content"/> is typed as <c>object?</c> — a UIElement
    /// is hosted directly; any other value is wrapped in a TextBlock at render time.
    /// </summary>
    [ContentProperty(nameof(Content))]
    public class ContentControl : FrameworkElement
    {
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(ContentControl),
                new PropertyMetadata((object?)null, OnContentChanged));

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(ContentControl),
                new PropertyMetadata((object?)null));

        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public Brush? Background
        {
            get => (Brush?)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        private UIElement? _presenter;

        /// <summary>The UIElement actually placed in the visual tree for the current Content.</summary>
        internal UIElement? Presenter => _presenter ??= BuildPresenter(Content);

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContentControl cc)
            {
                cc._presenter?.SetParent(null);
                cc._presenter = cc.BuildPresenter(e.NewValue);
                cc._presenter?.SetParent(cc);
                cc.InvalidateMeasure();
            }
        }

        private UIElement? BuildPresenter(object? content)
        {
            if (content == null) return null;
            if (content is UIElement el) return el;
            return new TextBlock { Text = content.ToString() ?? string.Empty };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UIElement? p = Presenter;
            if (p == null) return Size.Empty;
            p.Measure(availableSize);
            return p.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Presenter?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return finalSize;
        }

        public override object? FindName(string name)
        {
            if (Name == name) return this;
            if (Presenter is FrameworkElement fe) return fe.FindName(name);
            return null;
        }
    }
}
