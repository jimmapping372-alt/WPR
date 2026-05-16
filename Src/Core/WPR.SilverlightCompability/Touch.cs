using System;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Stub for <c>System.Windows.Input.Touch</c>. WP/Silverlight apps subscribe to
    /// <see cref="FrameReported"/> to receive raw multi-touch frames. WPR currently routes
    /// touch through Avalonia/FNA's pointer pipeline rather than this static, so the event
    /// is never fired — but the type must exist so the user's hookup IL JITs and runs.
    /// </summary>
    public static class Touch
    {
        public static event TouchFrameEventHandler? FrameReported;

        // Suppress "never used" warning while the event isn't raised yet — we want the
        // event member around so user code that subscribes to it works.
        internal static void RaiseFrameReportedForFutureUse(object? sender, TouchFrameEventArgs e)
        {
            FrameReported?.Invoke(sender, e);
        }
    }

    /// <summary>Delegate signature for <see cref="Touch.FrameReported"/>.</summary>
    public delegate void TouchFrameEventHandler(object? sender, TouchFrameEventArgs e);

    /// <summary>
    /// Stub event args for a touch frame report. Carries an empty <see cref="TouchPointCollection"/>
    /// so user code that iterates touches sees no points (and skips its loop).
    /// </summary>
    public class TouchFrameEventArgs : EventArgs
    {
        public int Timestamp { get; }

        public TouchFrameEventArgs() { }
        public TouchFrameEventArgs(int timestamp) { Timestamp = timestamp; }

        public TouchPointCollection GetTouchPoints(UIElement? relativeTo) => new TouchPointCollection();
        public TouchPoint? GetPrimaryTouchPoint(UIElement? relativeTo) => null;

        public void SuspendMousePromotionUntilTouchUp() { /* no-op */ }
    }

    public class TouchPointCollection : List<TouchPoint> { }

    public class TouchPoint
    {
        public TouchAction Action { get; set; }
        public TouchDevice? TouchDevice { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
    }

    public class TouchDevice
    {
        public int Id { get; set; }
        public UIElement? DirectlyOver { get; set; }
    }

    public enum TouchAction
    {
        Down = 0,
        Move = 1,
        Up = 2,
    }
}
