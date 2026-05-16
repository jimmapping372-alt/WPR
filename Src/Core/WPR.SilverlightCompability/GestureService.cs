// Microsoft.Phone.Controls.Toolkit's GestureService attached property + the
// GestureListener it bridges to. Real WP7 routes touch events through Direct3D
// surface handlers up to GestureListener instances attached via:
//
//   <toolkit:GestureService.GestureListener>
//     <toolkit:GestureListener Tap="OnTap" Flick="OnFlick"/>
//   </toolkit:GestureService.GestureListener>
//
// We re-route Avalonia pointer events through the WPR.SilverlightCompability
// shim layer to invoke whichever handlers the user wired.

using System;
using WPR.SilverlightCompability;

namespace Microsoft.Phone.Controls
{
    /// <summary>
    /// Attached-property host for <see cref="GestureListener"/>. Real WP defines
    /// this in <c>Microsoft.Phone.Controls.Toolkit.dll</c>; user XAML's
    /// <c>&lt;toolkit:GestureService.GestureListener&gt;</c> attached property
    /// sets the listener and our pointer pipeline can find it via
    /// <see cref="GetGestureListener"/>.
    /// </summary>
    public static class GestureService
    {
        public static readonly DependencyProperty GestureListenerProperty =
            DependencyProperty.RegisterAttached(
                "GestureListener", typeof(GestureListener), typeof(GestureService),
                new PropertyMetadata((object?)null));

        public static GestureListener? GetGestureListener(DependencyObject obj)
            => (GestureListener?)obj?.GetValue(GestureListenerProperty);

        public static void SetGestureListener(DependencyObject obj, GestureListener value)
            => obj?.SetValue(GestureListenerProperty, value);
    }

    /// <summary>
    /// Per-element gesture event source. User XAML wires the events:
    ///   <c>&lt;toolkit:GestureListener Tap="OnTap" Hold="..." Flick="..." DragStarted="..." DragDelta="..." DragCompleted="..."/&gt;</c>.
    /// We invoke them from the FrameView's pointer pipeline.
    /// </summary>
    public class GestureListener : DependencyObject
    {
        // Suppress CS0067 — these events are referenced from XAML at parse time
        // but not all are raised by every code path; that's fine for ABI.
#pragma warning disable CS0067
        public event EventHandler<GestureEventArgs>? Tap;
        public event EventHandler<GestureEventArgs>? DoubleTap;
        public event EventHandler<GestureEventArgs>? Hold;
        public event EventHandler<FlickGestureEventArgs>? Flick;
        public event EventHandler<DragStartedGestureEventArgs>? DragStarted;
        public event EventHandler<DragDeltaGestureEventArgs>? DragDelta;
        public event EventHandler<DragCompletedGestureEventArgs>? DragCompleted;
        public event EventHandler<GestureEventArgs>? GestureBegin;
        public event EventHandler<GestureEventArgs>? GestureCompleted;
        public event EventHandler<MouseEventArgs>? MouseMove;
        public event EventHandler<MouseEventArgs>? MouseEnter;
        public event EventHandler<MouseEventArgs>? MouseLeftButtonDown;
        public event EventHandler<MouseEventArgs>? MouseLeftButtonUp;
        public event EventHandler<MouseEventArgs>? MouseLeave;
#pragma warning restore CS0067

        // CRITICAL: the gesture events fire with `sender` = the element the
        // GestureListener is attached to (the ListBox, StackPanel, etc.), NOT
        // the GestureListener itself. Real WP7 toolkit user code routinely
        // does `(ListBox)sender` inside a Tap handler — passing `this` here
        // would surface an <see cref="InvalidCastException"/> in their first
        // line. The FrameView's tap pipeline supplies the attached element
        // via the <paramref name="attachedTo"/> argument.

        internal void RaiseTap(Point screenPos, UIElement attachedTo, UIElement origin)
            => Tap?.Invoke(attachedTo, new GestureEventArgs(screenPos, origin));

        internal void RaiseFlick(Point screenPos, UIElement attachedTo, UIElement origin, double horizontal, double vertical)
            => Flick?.Invoke(attachedTo, new FlickGestureEventArgs(screenPos, origin, horizontal, vertical));

        internal void RaiseHold(Point screenPos, UIElement attachedTo, UIElement origin)
            => Hold?.Invoke(attachedTo, new GestureEventArgs(screenPos, origin));
    }

    /// <summary>Shim for the WP toolkit gesture event-args base.</summary>
    public class GestureEventArgs : EventArgs
    {
        public Point Position { get; }
        public UIElement OriginalSource { get; }
        public bool Handled { get; set; }
        public GestureEventArgs(Point position, UIElement originalSource)
        {
            Position = position;
            OriginalSource = originalSource;
        }
        public Point GetPosition(UIElement? relativeTo) => Position;
    }

    /// <summary>Shim for FlickGestureEventArgs — supplies HorizontalVelocity / VerticalVelocity.</summary>
    public class FlickGestureEventArgs : GestureEventArgs
    {
        public double HorizontalVelocity { get; }
        public double VerticalVelocity { get; }
        /// <summary>Best-effort direction (Horizontal when |HV|>|VV|).</summary>
        public Orientation Direction =>
            Math.Abs(HorizontalVelocity) >= Math.Abs(VerticalVelocity)
                ? Orientation.Horizontal
                : Orientation.Vertical;

        public FlickGestureEventArgs(Point pos, UIElement origin, double hv, double vv)
            : base(pos, origin)
        {
            HorizontalVelocity = hv;
            VerticalVelocity = vv;
        }
    }

    public class DragStartedGestureEventArgs : GestureEventArgs
    {
        public Orientation Direction { get; }
        public DragStartedGestureEventArgs(Point pos, UIElement origin, Orientation direction)
            : base(pos, origin) { Direction = direction; }
    }

    public class DragDeltaGestureEventArgs : GestureEventArgs
    {
        public double HorizontalChange { get; }
        public double VerticalChange { get; }
        public Orientation Direction { get; }
        public DragDeltaGestureEventArgs(Point pos, UIElement origin, double hc, double vc, Orientation direction)
            : base(pos, origin)
        {
            HorizontalChange = hc;
            VerticalChange = vc;
            Direction = direction;
        }
    }

    public class DragCompletedGestureEventArgs : GestureEventArgs
    {
        public double HorizontalChange { get; }
        public double VerticalChange { get; }
        public Orientation Direction { get; }
        public DragCompletedGestureEventArgs(Point pos, UIElement origin, double hc, double vc, Orientation direction)
            : base(pos, origin)
        {
            HorizontalChange = hc;
            VerticalChange = vc;
            Direction = direction;
        }
    }
}
