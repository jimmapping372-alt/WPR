using System;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Stub for <c>System.Windows.Controls.TextBox</c>. Editable text input. WPR currently
    /// renders this as a TextBlock-like read-only field; full editing requires routing keyboard
    /// input through Avalonia's input pipeline, which we don't yet do for Silverlight pages.
    /// </summary>
    public class TextBox : Control
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(TextBox),
                new PropertyMetadata(14.0));

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(nameof(FontFamily), typeof(string), typeof(TextBox),
                new PropertyMetadata("Segoe UI"));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(TextBox),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(TextBox),
                new PropertyMetadata(TextAlignment.Left));

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(TextBox),
                new PropertyMetadata(TextWrapping.NoWrap));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(TextBox),
                new PropertyMetadata(0));

        public static readonly DependencyProperty AcceptsReturnProperty =
            DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(TextBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TextBox),
                new PropertyMetadata(false));

        public string Text
        {
            get => (string)GetValue(TextProperty)!;
            set
            {
                string old = Text;
                SetValue(TextProperty, value ?? string.Empty);
                if (!string.Equals(old, value, StringComparison.Ordinal))
                    TextChanged?.Invoke(this, new TextChangedEventArgs());
            }
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty)!;
            set => SetValue(FontSizeProperty, value);
        }

        public string FontFamily
        {
            get => (string)GetValue(FontFamilyProperty)!;
            set => SetValue(FontFamilyProperty, value);
        }

        public Brush? Foreground
        {
            get => (Brush?)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty)!;
            set => SetValue(TextAlignmentProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty)!;
            set => SetValue(TextWrappingProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty)!;
            set => SetValue(MaxLengthProperty, value);
        }

        public bool AcceptsReturn
        {
            get => (bool)GetValue(AcceptsReturnProperty)!;
            set => SetValue(AcceptsReturnProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty)!;
            set => SetValue(IsReadOnlyProperty, value);
        }

        public int SelectionStart { get; set; }
        public int SelectionLength { get; set; }
        public string SelectedText { get; set; } = string.Empty;

        public event EventHandler<TextChangedEventArgs>? TextChanged;

        public void Select(int start, int length)
        {
            SelectionStart = start;
            SelectionLength = length;
        }

        public void SelectAll()
        {
            SelectionStart = 0;
            SelectionLength = Text?.Length ?? 0;
        }
    }

    /// <summary>Minimal stand-in for a Control base — the WP TextBox derives from Control, but
    /// our framework collapses Control to FrameworkElement since most of Control's chrome
    /// (template, focus visual) isn't relevant on WPR.</summary>
    public class Control : FrameworkElement
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(Control),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(Control),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(Control),
                new PropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(Control),
                new PropertyMetadata(new Thickness(0)));

        public Brush? Background
        {
            get => (Brush?)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public Brush? BorderBrush
        {
            get => (Brush?)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty)!;
            set => SetValue(BorderThicknessProperty, value);
        }

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty)!;
            set => SetValue(PaddingProperty, value);
        }

        // Silverlight's templated-control plumbing — every WP toolkit control sets
        // DefaultStyleKey = typeof(self) in its ctor so a generic.xaml-style lookup
        // can find the matching ControlTemplate. We don't apply templates.
        //
        // The setter is intentionally a NO-OP that never touches `this` — the
        // patched WP Toolkit IL emits `call Control::set_DefaultStyleKey` with
        // `this` of types (Panorama, Pivot, …) whose post-patch base chain bypasses
        // Control (TemplatedItemsControl<T> → ItemsControl → StackPanel → Panel).
        // Storing to a backing field on this object would corrupt memory in that
        // case; throwing away the value is safe.
        public object? DefaultStyleKey
        {
            set { /* no-op on purpose, see comment above */ }
        }
    }

    public class TextChangedEventArgs : EventArgs { }
}
