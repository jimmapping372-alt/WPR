// Minimal shims for the System.Windows.Shapes namespace.
//
// Real Silverlight defines a Shape base with rich path / geometry support; here
// we only need the type tokens to resolve (so user IL referencing
// System.Windows.Shapes.Rectangle / Shape JITs) and the Fill / Stroke / etc.
// setters to round-trip. The renderer doesn't currently paint primitive shapes.

using System;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Shim for <c>System.Windows.Shapes.Shape</c>. Common base for primitive
    /// vector shapes (<see cref="Rectangle"/>, etc.). Surface area limited to the
    /// brushes WP7 code routinely sets — <see cref="Fill"/>, <see cref="Stroke"/>,
    /// <see cref="StrokeThickness"/> — plus a no-op <c>Stretch</c> that XAML often
    /// specifies.
    /// </summary>
    public abstract class Shape : FrameworkElement
    {
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(Shape),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Shape),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(Shape),
                new PropertyMetadata((object)1.0));

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(Shape),
                new PropertyMetadata(Stretch.None));

        public Brush? Fill
        {
            get => (Brush?)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public Brush? Stroke
        {
            get => (Brush?)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty)!;
            set => SetValue(StrokeThicknessProperty, value);
        }

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty)!;
            set => SetValue(StretchProperty, value);
        }
    }

    /// <summary>
    /// Shim for <c>System.Windows.Shapes.Rectangle</c>. UserRank uses it as a
    /// colored badge — code paints the shape via <see cref="Shape.Fill"/> /
    /// <see cref="UIElement.OpacityMask"/>. Renderer doesn't paint shapes yet;
    /// the type exists so JIT'd code that references it loads cleanly.
    /// </summary>
    public class Rectangle : Shape
    {
        public static readonly DependencyProperty RadiusXProperty =
            DependencyProperty.Register(nameof(RadiusX), typeof(double), typeof(Rectangle),
                new PropertyMetadata((object)0.0));

        public static readonly DependencyProperty RadiusYProperty =
            DependencyProperty.Register(nameof(RadiusY), typeof(double), typeof(Rectangle),
                new PropertyMetadata((object)0.0));

        public double RadiusX
        {
            get => (double)GetValue(RadiusXProperty)!;
            set => SetValue(RadiusXProperty, value);
        }

        public double RadiusY
        {
            get => (double)GetValue(RadiusYProperty)!;
            set => SetValue(RadiusYProperty, value);
        }
    }

    /// <summary>
    /// Shim for <c>System.Windows.Controls.Border</c>. Single-child decoration
    /// with background / border-brush / thickness / corner-radius. Mirrors the
    /// SL semantics enough for XAML <c>&lt;Border&gt;</c> wrappers to load.
    /// </summary>
    [ContentProperty(nameof(Child))]
    public class Border : FrameworkElement
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(Border),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(Border),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(Border),
                new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(Border),
                new PropertyMetadata(new CornerRadius()));

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(Border),
                new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty ChildProperty =
            DependencyProperty.Register(nameof(Child), typeof(UIElement), typeof(Border),
                new PropertyMetadata((object?)null, OnChildChanged));

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

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty)!;
            set => SetValue(CornerRadiusProperty, value);
        }

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty)!;
            set => SetValue(PaddingProperty, value);
        }

        public UIElement? Child
        {
            get => (UIElement?)GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Border b)
            {
                if (e.OldValue is UIElement old) old.SetParent(null);
                if (e.NewValue is UIElement now) now.SetParent(b);
                b.InvalidateMeasure();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Child == null) return Size.Empty;
            // Subtract border + padding from the available slot.
            double padW = BorderThickness.Left + BorderThickness.Right + Padding.Left + Padding.Right;
            double padH = BorderThickness.Top + BorderThickness.Bottom + Padding.Top + Padding.Bottom;
            var inner = new Size(
                Math.Max(0, availableSize.Width - padW),
                Math.Max(0, availableSize.Height - padH));
            Child.Measure(inner);
            return new Size(Child.DesiredSize.Width + padW, Child.DesiredSize.Height + padH);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child == null) return finalSize;
            double left = BorderThickness.Left + Padding.Left;
            double top = BorderThickness.Top + Padding.Top;
            double padW = BorderThickness.Left + BorderThickness.Right + Padding.Left + Padding.Right;
            double padH = BorderThickness.Top + BorderThickness.Bottom + Padding.Top + Padding.Bottom;
            Child.Arrange(new Rect(left, top,
                Math.Max(0, finalSize.Width - padW),
                Math.Max(0, finalSize.Height - padH)));
            return finalSize;
        }
    }
}
