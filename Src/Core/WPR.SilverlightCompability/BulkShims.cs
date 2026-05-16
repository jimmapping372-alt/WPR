// Bulk shim file: minimal stubs for the long-tail of System.Windows.* types
// referenced by WP Toolkit controls (Microsoft.Phone.Controls.dll, .Toolkit.dll)
// and user code. The goal is "type token resolves", "JIT succeeds", "no-op
// behaviour at runtime" — not faithful semantics. Each type carries just enough
// API surface that the patched IL JIT'ing against it doesn't MissingMethod /
// MissingField at runtime.
//
// One file keeps the unrelated-but-related grunt work in one place rather than
// spraying tiny per-type files across the project.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace WPR.SilverlightCompability
{
    // ---- Input ---------------------------------------------------------------

    /// <summary>Shim for <c>System.Windows.Input.ManipulationDelta</c>.</summary>
    public struct ManipulationDelta
    {
        public Point Translation;
        public double Rotation;
        public Point Scale;
        public Point Expansion;
    }

    /// <summary>Shim for <c>System.Windows.Input.ManipulationVelocities</c>.</summary>
    public struct ManipulationVelocities
    {
        public Point LinearVelocity;
        public double AngularVelocity;
        public Point ExpansionVelocity;
    }

    /// <summary>Shim for <c>System.Windows.Input.ManipulationStartedEventArgs</c>.</summary>
    public class ManipulationStartedEventArgs : RoutedEventArgs
    {
        public Point ManipulationOrigin { get; set; }
        public UIElement? ManipulationContainer { get; set; }
        public void Complete() { }
    }

    /// <summary>Shim for <c>System.Windows.Input.ManipulationDeltaEventArgs</c>.</summary>
    public class ManipulationDeltaEventArgs : RoutedEventArgs
    {
        public Point ManipulationOrigin { get; set; }
        public UIElement? ManipulationContainer { get; set; }
        public ManipulationDelta DeltaManipulation { get; set; }
        public ManipulationDelta CumulativeManipulation { get; set; }
        public ManipulationVelocities Velocities { get; set; }
        public void Complete() { }
    }

    /// <summary>Shim for <c>System.Windows.Input.ManipulationCompletedEventArgs</c>.</summary>
    public class ManipulationCompletedEventArgs : RoutedEventArgs
    {
        public Point ManipulationOrigin { get; set; }
        public UIElement? ManipulationContainer { get; set; }
        public ManipulationDelta TotalManipulation { get; set; }
        public ManipulationVelocities FinalVelocities { get; set; }
    }

    /// <summary>Shim for <c>System.Windows.Input.MouseButtonEventArgs</c>.</summary>
    public class MouseButtonEventArgs : RoutedEventArgs
    {
        public Point GetPosition(UIElement? relativeTo) => default;
    }

    /// <summary>Shim for <c>System.Windows.Input.MouseEventArgs</c>.</summary>
    public class MouseEventArgs : RoutedEventArgs
    {
        public Point GetPosition(UIElement? relativeTo) => default;
    }

    /// <summary>Shim for <c>System.Windows.Input.MouseButtonEventHandler</c>.</summary>
    public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);

    /// <summary>Shim for <c>System.Windows.Input.MouseEventHandler</c>.</summary>
    public delegate void MouseEventHandler(object sender, MouseEventArgs e);

    /// <summary>Shim for <c>System.Windows.Input.KeyEventArgs</c>.</summary>
    public class KeyEventArgs : RoutedEventArgs
    {
        public Key Key { get; set; }
        public int PlatformKeyCode { get; set; }
    }

    /// <summary>Shim for <c>System.Windows.Input.KeyEventHandler</c>.</summary>
    public delegate void KeyEventHandler(object sender, KeyEventArgs e);

    /// <summary>Shim for <c>System.Windows.Input.Key</c>.</summary>
    public enum Key
    {
        None = 0, Back = 1, Enter = 6, Escape = 27, Space = 32, Tab = 3,
        Left = 37, Up = 38, Right = 39, Down = 40,
    }

    // ---- Media: transforms ---------------------------------------------------

    /// <summary>Shim for <c>System.Windows.Media.GeneralTransform</c>.</summary>
    public abstract class GeneralTransform : DependencyObject
    {
        public virtual Point Transform(Point point) => point;
        public virtual GeneralTransform? Inverse => null;
    }

    /// <summary>Shim for <c>System.Windows.Media.Transform</c>. Abstract DO base.</summary>
    public abstract class Transform : GeneralTransform { }

    /// <summary>Shim for <c>System.Windows.Media.TransformCollection</c>.</summary>
    public class TransformCollection : List<Transform> { }

    /// <summary>Shim for <c>System.Windows.Media.TransformGroup</c>.</summary>
    public class TransformGroup : Transform
    {
        public TransformCollection Children { get; } = new TransformCollection();
    }

    /// <summary>Shim for <c>System.Windows.Media.TranslateTransform</c>.</summary>
    public class TranslateTransform : Transform
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>Shim for <c>System.Windows.Media.CompositeTransform</c>. Combined
    /// translate/rotate/scale/skew used everywhere by WP page transitions.</summary>
    public class CompositeTransform : Transform
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public double Rotation { get; set; }
        public double SkewX { get; set; }
        public double SkewY { get; set; }
        public double TranslateX { get; set; }
        public double TranslateY { get; set; }
    }

    /// <summary>Shim for <c>System.Windows.Media.Projection</c>. Abstract.</summary>
    public abstract class Projection : DependencyObject { }

    /// <summary>Shim for <c>System.Windows.Media.PlaneProjection</c>. 3-D rotation
    /// used by WP page transitions (turnstile, etc.).</summary>
    public class PlaneProjection : Projection
    {
        public double CenterOfRotationX { get; set; } = 0.5;
        public double CenterOfRotationY { get; set; } = 0.5;
        public double CenterOfRotationZ { get; set; }
        public double GlobalOffsetX { get; set; }
        public double GlobalOffsetY { get; set; }
        public double GlobalOffsetZ { get; set; }
        public double LocalOffsetX { get; set; }
        public double LocalOffsetY { get; set; }
        public double LocalOffsetZ { get; set; }
        public double RotationX { get; set; }
        public double RotationY { get; set; }
        public double RotationZ { get; set; }
    }

    // ---- Media: misc ---------------------------------------------------------

    /// <summary>Shim for <c>System.Windows.Media.Geometry</c>.</summary>
    public abstract class Geometry : DependencyObject
    {
        public virtual Rect Bounds => default;
    }

    /// <summary>Shim for <c>System.Windows.Media.RectangleGeometry</c>.</summary>
    public class RectangleGeometry : Geometry
    {
        public Rect Rect { get; set; }
        public override Rect Bounds => Rect;
    }

    /// <summary>Shim for <c>System.Windows.Media.GradientBrush</c>.</summary>
    public abstract class GradientBrush : Brush { }

    /// <summary>Shim for <c>System.Windows.Media.TileBrush</c>. Base for ImageBrush.</summary>
    public abstract class TileBrush : Brush
    {
        public AlignmentX AlignmentX { get; set; } = AlignmentX.Center;
        public AlignmentY AlignmentY { get; set; } = AlignmentY.Center;
        public Stretch Stretch { get; set; } = Stretch.Fill;
    }

    /// <summary>Shim for <c>System.Windows.Media.AlignmentX</c>.</summary>
    public enum AlignmentX { Left, Center, Right }

    /// <summary>Shim for <c>System.Windows.Media.AlignmentY</c>.</summary>
    public enum AlignmentY { Top, Center, Bottom }

    /// <summary>Shim for <c>System.Windows.Media.CacheMode</c>. Abstract.</summary>
    public abstract class CacheMode : DependencyObject { }

    /// <summary>Shim for <c>System.Windows.Media.BitmapCache</c>.</summary>
    public class BitmapCache : CacheMode
    {
        public double RenderAtScale { get; set; } = 1.0;
    }

    /// <summary>Shim for <c>System.Windows.Media.VisualTreeHelper</c>. The few
    /// methods user code touches are static enumerators we satisfy with
    /// no-children/null returns.</summary>
    public static class VisualTreeHelper
    {
        public static int GetChildrenCount(DependencyObject reference)
        {
            if (reference is Panel p) return p.Children.Count;
            if (reference is ContentControl cc) return cc.Content is UIElement ? 1 : 0;
            return 0;
        }

        public static DependencyObject? GetChild(DependencyObject reference, int childIndex)
        {
            if (reference is Panel p) return childIndex >= 0 && childIndex < p.Children.Count ? p.Children[childIndex] : null;
            if (reference is ContentControl cc && childIndex == 0) return cc.Content as DependencyObject;
            return null;
        }

        public static DependencyObject? GetParent(DependencyObject reference)
            => (reference as UIElement)?.Parent;

        public static IEnumerable<UIElement> FindElementsInHostCoordinates(Point intersectingPoint, UIElement subtree)
            => Array.Empty<UIElement>();

        public static IEnumerable<UIElement> FindElementsInHostCoordinates(Rect intersectingRect, UIElement subtree)
            => Array.Empty<UIElement>();
    }

    /// <summary>Shim for <c>System.Windows.Media.Imaging.BitmapCreateOptions</c>.</summary>
    [Flags]
    public enum BitmapCreateOptions
    {
        None             = 0,
        DelayCreation    = 2,
        IgnoreImageCache = 8,
        BackgroundCreation = 0x10,
    }

    // ---- Animation easing functions ------------------------------------------

    /// <summary>Shim for <c>System.Windows.Media.Animation.ExponentialEase</c>.</summary>
    public class ExponentialEase : IEasingFunction
    {
        public double Exponent { get; set; } = 2.0;
        public EasingMode EasingMode { get; set; } = EasingMode.EaseOut;
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.QuarticEase</c>.</summary>
    public class QuarticEase : IEasingFunction
    {
        public EasingMode EasingMode { get; set; } = EasingMode.EaseOut;
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.EasingMode</c>.</summary>
    public enum EasingMode { EaseIn, EaseOut, EaseInOut }

    // ---- Controls -----------------------------------------------------------

    /// <summary>Shim for <c>System.Windows.Controls.CheckBox</c>.</summary>
    public class CheckBox : ContentControl
    {
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(CheckBox),
                new PropertyMetadata((object?)false));

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

#pragma warning disable CS0067
        public event RoutedEventHandler? Checked;
        public event RoutedEventHandler? Unchecked;
        public event RoutedEventHandler? Indeterminate;
#pragma warning restore CS0067
    }

    /// <summary>Shim for <c>System.Windows.Controls.Primitives.ToggleButton</c>.</summary>
    public class ToggleButton : ContentControl
    {
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(ToggleButton),
                new PropertyMetadata((object?)false));

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

#pragma warning disable CS0067
        public event RoutedEventHandler? Checked;
        public event RoutedEventHandler? Unchecked;
#pragma warning restore CS0067
    }

    /// <summary>Shim for <c>System.Windows.Controls.Primitives.Selector</c>.
    /// We collapsed this onto our flat ListBox earlier; expose as a separate
    /// type-only alias for IL refs that name <c>Selector</c> directly.</summary>
    public class Selector : ItemsControl
    {
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(Selector),
                new PropertyMetadata((object)(-1)));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(Selector),
                new PropertyMetadata((object?)null));

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

#pragma warning disable CS0067
        public event SelectionChangedEventHandler? SelectionChanged;
#pragma warning restore CS0067
    }

    /// <summary>
    /// Shim for <c>System.Windows.Controls.Page</c>. Navigation host. In SL the chain is
    /// <c>Page → UserControl → Control</c> and <c>PhoneApplicationPage</c> derives from
    /// <see cref="Page"/>. The page-lifecycle hooks (<see cref="OnNavigatedTo"/>,
    /// <see cref="OnNavigatedFrom"/>, <see cref="OnNavigatingFrom"/>) are declared
    /// here — overriding subclasses (Minesweeper's MainPage) emit
    /// <c>call base.OnNavigatedTo</c> against <c>System.Windows.Controls.Page</c>, which
    /// the patcher rewrites to land here. Without these methods on this exact type,
    /// user IL <see cref="MissingMethodException"/>s on the base call.
    /// </summary>
    public class Page : UserControl
    {
        public string? Title { get; set; }
        public NavigationService? NavigationService { get; internal set; }

        protected internal virtual void OnNavigatedTo(NavigationEventArgs e) { }
        protected internal virtual void OnNavigatedFrom(NavigationEventArgs e) { }
        protected internal virtual void OnNavigatingFrom(NavigatingCancelEventArgs e) { }
    }

    /// <summary>Shim for <c>System.Windows.Controls.ContentPresenter</c>.</summary>
    public class ContentPresenter : ContentControl { }

    /// <summary>Shim for <c>System.Windows.Controls.ItemsPresenter</c>.</summary>
    public class ItemsPresenter : FrameworkElement { }

    /// <summary>Shim for <c>System.Windows.Controls.ItemCollection</c>.
    /// In SL this is the runtime collection backing ItemsControl.Items.
    /// Inherits from <see cref="PresentationFrameworkCollection{T}"/> because
    /// user / toolkit IL emits <c>callvirt PresentationFrameworkCollection&lt;object&gt;::IndexOf</c>
    /// against it — if we used <c>List&lt;object&gt;</c> as the base, that virtual call
    /// would land in the wrong slot (or fail outright on a missing override).
    /// </summary>
    public class ItemCollection : PresentationFrameworkCollection<object> { }

    /// <summary>Shim for <c>System.Windows.Controls.ItemContainerGenerator</c>.
    /// User code rarely touches this directly; needs to exist for typerefs.</summary>
    public class ItemContainerGenerator
    {
        public DependencyObject? ContainerFromIndex(int index) => null;
        public DependencyObject? ContainerFromItem(object item) => null;
        public int IndexFromContainer(DependencyObject container) => -1;
        public object? ItemFromContainer(DependencyObject container) => null;
    }

    // ---- Data binding helpers ------------------------------------------------

    /// <summary>Shim for <c>System.Windows.Data.IValueConverter</c>.</summary>
    public interface IValueConverter
    {
        object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture);
        object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture);
    }

    /// <summary>Shim for <c>System.Windows.Data.BindingExpressionBase</c>.</summary>
    public class BindingExpressionBase { public void UpdateSource() { } }

    /// <summary>Shim for <c>System.Windows.Data.RelativeSource</c>.</summary>
    public class RelativeSource
    {
        public RelativeSourceMode Mode { get; set; } = RelativeSourceMode.TemplatedParent;
        public RelativeSource() { }
        public RelativeSource(RelativeSourceMode mode) { Mode = mode; }
    }

    /// <summary>Shim for <c>System.Windows.Data.RelativeSourceMode</c>.</summary>
    public enum RelativeSourceMode { TemplatedParent, Self, FindAncestor, PreviousData }

    // ---- Fonts ---------------------------------------------------------------

    /// <summary>Shim for <c>System.Windows.FontWeight</c>.</summary>
    public struct FontWeight : IEquatable<FontWeight>
    {
        public int Weight { get; }
        public FontWeight(int weight) { Weight = weight; }
        public bool Equals(FontWeight other) => Weight == other.Weight;
        public override bool Equals(object? obj) => obj is FontWeight fw && Equals(fw);
        public override int GetHashCode() => Weight;
        public static bool operator ==(FontWeight a, FontWeight b) => a.Equals(b);
        public static bool operator !=(FontWeight a, FontWeight b) => !a.Equals(b);
        public override string ToString() => Weight.ToString();
    }

    /// <summary>Shim for <c>System.Windows.FontWeights</c>. Standard named weights.</summary>
    public static class FontWeights
    {
        public static FontWeight Thin       => new FontWeight(100);
        public static FontWeight ExtraLight => new FontWeight(200);
        public static FontWeight Light      => new FontWeight(300);
        public static FontWeight Normal     => new FontWeight(400);
        public static FontWeight Medium     => new FontWeight(500);
        public static FontWeight SemiBold   => new FontWeight(600);
        public static FontWeight Bold       => new FontWeight(700);
        public static FontWeight ExtraBold  => new FontWeight(800);
        public static FontWeight Black      => new FontWeight(900);
    }

    // ---- Misc top-level ------------------------------------------------------

    /// <summary>Shim for <c>System.Windows.Style</c>. Carries setters keyed by
    /// property name (or DependencyProperty). <see cref="Apply"/> applies the
    /// collected setters to a target FrameworkElement — called when an element's
    /// Style property changes (FrameworkElement.OnStyleChanged).</summary>
    [ContentProperty(nameof(Setters))]
    public class Style
    {
        public Type? TargetType { get; set; }
        public Style? BasedOn { get; set; }
        public List<Setter> Setters { get; } = new List<Setter>();

        /// <summary>
        /// Apply this style (and the chain of <see cref="BasedOn"/> styles) to
        /// <paramref name="target"/>. BasedOn applies first so the closest style
        /// wins on conflicts. Property strings are resolved against the runtime
        /// type's DPs first, falling back to CLR properties.
        /// </summary>
        public void Apply(FrameworkElement target)
        {
            if (target == null) return;
            BasedOn?.Apply(target);

            Type t = target.GetType();
            foreach (Setter s in Setters)
            {
                if (s.Property is not string propName || string.IsNullOrEmpty(propName)) continue;
                try { ApplyOne(target, t, propName, s.Value); }
                catch { /* one bad setter shouldn't take down the whole apply */ }
            }
        }

        private static void ApplyOne(FrameworkElement target, Type t, string propName, object? value)
        {
            // Strip "Class.Property" qualifier when present (e.g. "TextBlock.FontSize").
            int dot = propName.IndexOf('.');
            if (dot >= 0) propName = propName.Substring(dot + 1);

            // DependencyProperty first.
            FieldInfo? dpField = null;
            for (Type? cur = t; cur != null && dpField == null; cur = cur.BaseType)
            {
                dpField = cur.GetField(propName + "Property",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            }
            if (dpField?.GetValue(null) is DependencyProperty dp)
            {
                object? converted = ConvertForTarget(value, dp.PropertyType);
                target.SetValue(dp, converted);
                return;
            }

            // CLR property fallback.
            PropertyInfo? prop = t.GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (prop != null && prop.CanWrite)
            {
                object? converted = ConvertForTarget(value, prop.PropertyType);
                prop.SetValue(target, converted);
            }
        }

        private static object? ConvertForTarget(object? value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;
            if (value is string str) return XamlTypeConverter.Convert(str, targetType);
            return value;
        }
    }

    /// <summary>Shim for <c>System.Windows.Setter</c>. Property is the string
    /// name of a DependencyProperty/CLR property on the target type; Value is
    /// the value to assign (raw string from XAML or pre-converted object).</summary>
    public class Setter
    {
        public object? Property { get; set; }
        public object? Value { get; set; }
        public Setter() { }
        public Setter(object property, object? value) { Property = property; Value = value; }
    }

    /// <summary>Shim for <c>System.Windows.PropertyPath</c>.
    /// Stores the dotted path; we don't resolve it (Bindings carry their own Path string).</summary>
    public class PropertyPath
    {
        public string Path { get; }
        public PropertyPath(string path) { Path = path; }
        public PropertyPath(object _) { Path = string.Empty; }
        public override string ToString() => Path;
    }

    /// <summary>Shim for <c>System.Windows.Deployment</c>. Singleton describing the
    /// installed XAP. <c>Current</c> is exposed so user code can access entry-point
    /// info; we leave Parts empty.</summary>
    public class Deployment
    {
        public static Deployment Current { get; } = new Deployment();
        public DeploymentPartCollection Parts { get; } = new DeploymentPartCollection();
        public string? EntryPointAssembly { get; set; }
        public string? EntryPointType { get; set; }
    }

    public class DeploymentPart
    {
        public Uri? Source { get; set; }
    }

    public class DeploymentPartCollection : List<DeploymentPart> { }

    /// <summary>Shim for <c>System.Windows.PresentationFrameworkCollection&lt;T&gt;</c>.
    /// SL's typed observable-ish list used as the public collection type for many
    /// XAML collection-valued properties (RowDefinitions, ColumnDefinitions, etc.).
    /// Mutators are <c>virtual</c> so derived collections (RowDefinitionCollection,
    /// ColumnDefinitionCollection) can hook layout-invalidation callbacks — List&lt;T&gt;'s
    /// own methods are non-virtual, so we cannot use it as a base for that.
    /// Also: user IL emits <c>callvirt PresentationFrameworkCollection&lt;T&gt;::set_Item</c>
    /// on values returned by <c>Grid.RowDefinitions</c> etc., so the concrete
    /// collections MUST actually inherit from this type for the dispatch to land
    /// in the right slot.</summary>
    public class PresentationFrameworkCollection<T> : IList<T>, IList
    {
        private readonly List<T> _items = new List<T>();

        public virtual T this[int index]
        {
            get => _items[index];
            set { _items[index] = value; OnItemsChanged(); }
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public virtual void Add(T item) { _items.Add(item); OnItemsChanged(); }
        public virtual void Clear() { _items.Clear(); OnItemsChanged(); }
        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(T item) => _items.IndexOf(item);
        public virtual void Insert(int index, T item) { _items.Insert(index, item); OnItemsChanged(); }
        public virtual bool Remove(T item)
        {
            bool r = _items.Remove(item);
            if (r) OnItemsChanged();
            return r;
        }
        public virtual void RemoveAt(int index) { _items.RemoveAt(index); OnItemsChanged(); }
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        // Non-generic IList facade — XAML/XamlReader's TryAddToCollection path
        // grabs the IList interface for collection-valued properties; expose so
        // both work without ambiguity.
        bool IList.IsFixedSize => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        object? IList.this[int index]
        {
            get => _items[index];
            set { _items[index] = (T)value!; OnItemsChanged(); }
        }
        int IList.Add(object? value) { Add((T)value!); return _items.Count - 1; }
        bool IList.Contains(object? value) => value is T t && _items.Contains(t);
        int IList.IndexOf(object? value) => value is T t ? _items.IndexOf(t) : -1;
        void IList.Insert(int index, object? value) => Insert(index, (T)value!);
        void IList.Remove(object? value) { if (value is T t) Remove(t); }
        void ICollection.CopyTo(Array array, int index)
            => ((ICollection)_items).CopyTo(array, index);

        /// <summary>Override to react to list mutations — RowDefinitionCollection and
        /// ColumnDefinitionCollection use this to call Grid.InvalidateMeasure.</summary>
        protected virtual void OnItemsChanged() { }
    }
}

// ---- Threading -----------------------------------------------------------

namespace WPR.SilverlightCompability.Threading
{
    /// <summary>Shim for <c>System.Windows.Threading.Dispatcher</c>. Marshals work
    /// back onto the UI thread. Our shim just runs everything inline — fine because
    /// the renderer hasn't taken ownership of a UI thread yet.</summary>
    public class Dispatcher
    {
        internal static readonly Dispatcher Shared = new Dispatcher();

        public bool CheckAccess() => true;

        public DispatcherOperation BeginInvoke(Delegate d, params object?[] args)
        {
            try { d.DynamicInvoke(args); } catch { /* swallow per SL semantics */ }
            return new DispatcherOperation();
        }

        public DispatcherOperation BeginInvoke(Action a)
        {
            try { a(); } catch { }
            return new DispatcherOperation();
        }

        public DispatcherOperation BeginInvoke<T>(Action<T> a, T arg)
        {
            try { a(arg); } catch { }
            return new DispatcherOperation();
        }
    }

    /// <summary>Shim for <c>System.Windows.Threading.DispatcherOperation</c>.</summary>
    public class DispatcherOperation
    {
        public Task Task { get; } = System.Threading.Tasks.Task.CompletedTask;
        public object? Wait() => null;
    }

    /// <summary>Shim for <c>System.Windows.Threading.DispatcherTimer</c>. Wraps a
    /// system timer so user code that schedules periodic callbacks doesn't break.</summary>
    public class DispatcherTimer
    {
        private System.Timers.Timer? _timer;

        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
        public bool IsEnabled => _timer?.Enabled ?? false;

        public event EventHandler? Tick;

        public void Start()
        {
            Stop();
            _timer = new System.Timers.Timer(Math.Max(1, Interval.TotalMilliseconds));
            _timer.AutoReset = true;
            _timer.Elapsed += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
