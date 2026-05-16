// Minimal shims for the System.Windows.Media.Animation namespace.
//
// The renderer does not actually run timelines yet — these types exist so user
// IL (and the XAML loader that materializes Storyboards declared in pages /
// control templates) JITs cleanly. Behaviour: properties remember their value;
// Begin/Stop are no-ops; Completed fires once on Begin so user code that waits
// for a one-shot timer (UserRank, PowerupsControl) makes forward progress.

using System;
using System.Collections;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Shim for <c>System.Windows.Media.Animation.Timeline</c>. Base for storyboards
    /// and per-property animations. We surface the handful of properties WP7 user
    /// code touches (<see cref="Duration"/>, <see cref="RepeatBehavior"/>,
    /// <see cref="AutoReverse"/>, <see cref="BeginTime"/>, <see cref="FillBehavior"/>,
    /// <see cref="Name"/>, <see cref="SpeedRatio"/>) and the
    /// <see cref="Completed"/> event. Nothing animates — but everything compiles
    /// and a Storyboard.Begin call fires Completed synchronously so logic gated
    /// on "the timer finished" keeps progressing.
    /// </summary>
    public class Timeline : DependencyObject
    {
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(nameof(Duration), typeof(Duration), typeof(Timeline),
                new PropertyMetadata(Duration.Automatic));

        public static readonly DependencyProperty RepeatBehaviorProperty =
            DependencyProperty.Register(nameof(RepeatBehavior), typeof(RepeatBehavior), typeof(Timeline),
                new PropertyMetadata(new RepeatBehavior(1)));

        public static readonly DependencyProperty AutoReverseProperty =
            DependencyProperty.Register(nameof(AutoReverse), typeof(bool), typeof(Timeline),
                new PropertyMetadata((object)false));

        public static readonly DependencyProperty BeginTimeProperty =
            DependencyProperty.Register(nameof(BeginTime), typeof(TimeSpan?), typeof(Timeline),
                new PropertyMetadata((object?)TimeSpan.Zero));

        public static readonly DependencyProperty FillBehaviorProperty =
            DependencyProperty.Register(nameof(FillBehavior), typeof(FillBehavior), typeof(Timeline),
                new PropertyMetadata(FillBehavior.HoldEnd));

        public static readonly DependencyProperty SpeedRatioProperty =
            DependencyProperty.Register(nameof(SpeedRatio), typeof(double), typeof(Timeline),
                new PropertyMetadata((object)1.0));

        public Duration Duration
        {
            get => (Duration)GetValue(DurationProperty)!;
            set => SetValue(DurationProperty, value);
        }

        public RepeatBehavior RepeatBehavior
        {
            get => (RepeatBehavior)GetValue(RepeatBehaviorProperty)!;
            set => SetValue(RepeatBehaviorProperty, value);
        }

        public bool AutoReverse
        {
            get => (bool)GetValue(AutoReverseProperty)!;
            set => SetValue(AutoReverseProperty, value);
        }

        public TimeSpan? BeginTime
        {
            get => (TimeSpan?)GetValue(BeginTimeProperty);
            set => SetValue(BeginTimeProperty, value);
        }

        public FillBehavior FillBehavior
        {
            get => (FillBehavior)GetValue(FillBehaviorProperty)!;
            set => SetValue(FillBehaviorProperty, value);
        }

        public double SpeedRatio
        {
            get => (double)GetValue(SpeedRatioProperty)!;
            set => SetValue(SpeedRatioProperty, value);
        }

        public string? Name { get; set; }

        public event EventHandler? Completed;

        /// <summary>Raised by <see cref="Storyboard.Begin"/> so user code that waits
        /// on a one-shot timer can complete. Animation-typed subclasses override to
        /// short-circuit nothing meaningful.</summary>
        protected internal void RaiseCompleted() => Completed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shim for <c>System.Windows.Media.Animation.Storyboard</c>. In SL a Storyboard
    /// is a Timeline that drives its <see cref="Children"/>. Begin/Stop here are
    /// near-no-ops — Begin fires <c>Completed</c> synchronously so user logic gated
    /// on "the timer ticked" doesn't deadlock.
    /// </summary>
    [ContentProperty(nameof(Children))]
    public class Storyboard : Timeline
    {
        public TimelineCollection Children { get; } = new TimelineCollection();

        // Begin/Stop/etc are deliberate no-ops. In SL, Begin starts the timeline
        // and fires Completed after Duration elapses. We don't run timelines, so
        // we can't honor that timing. Firing Completed synchronously here would
        // be catastrophic: WP7 code routinely restarts a timer from its own
        // Completed handler (PowerupsControl._tokenTimer is exactly this), and a
        // sync raise would recurse infinitely → StackOverflowException. Leaving
        // Completed unraised means logic gated on "the animation finished" never
        // ticks — accept that until a real timeline engine ships.
        public void Begin() { }
        public void Stop() { }
        public void Pause() { }
        public void Resume() { }
        public void Seek(TimeSpan offset) { }
        public void SeekAlignedToLastTick(TimeSpan offset) { }
        public void SkipToFill() { }
        public TimeSpan GetCurrentTime() => TimeSpan.Zero;
        public ClockState GetCurrentState() => ClockState.Stopped;

        // Attached properties. XAML and user code use these to bind a child animation
        // to a target FrameworkElement / property path. Real SL signatures take
        // (Timeline, …) — user IL emits exactly that, so the strong-typed first
        // parameter is critical for MissingMethodException avoidance.
        public static readonly DependencyProperty TargetNameProperty =
            DependencyProperty.RegisterAttached("TargetName", typeof(string), typeof(Storyboard),
                new PropertyMetadata((object?)null));

        public static readonly DependencyProperty TargetPropertyProperty =
            DependencyProperty.RegisterAttached("TargetProperty", typeof(PropertyPath), typeof(Storyboard),
                new PropertyMetadata((object?)null));

        public static void SetTargetName(Timeline element, string name)
            => element?.SetValue(TargetNameProperty, name);
        public static string? GetTargetName(Timeline element)
            => (string?)element?.GetValue(TargetNameProperty);

        public static void SetTarget(Timeline element, DependencyObject target) { /* no-op */ }

        public static void SetTargetProperty(Timeline element, PropertyPath path)
            => element?.SetValue(TargetPropertyProperty, path);
        public static PropertyPath? GetTargetProperty(Timeline element)
            => (PropertyPath?)element?.GetValue(TargetPropertyProperty);
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.TimelineCollection</c>.</summary>
    public class TimelineCollection : List<Timeline> { }

    /// <summary>Shim for <c>System.Windows.Media.Animation.DoubleAnimation</c>.</summary>
    public class DoubleAnimation : Timeline
    {
        public double? From { get; set; }
        public double? To { get; set; }
        public double? By { get; set; }
        public IEasingFunction? EasingFunction { get; set; }
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames</c>.</summary>
    [ContentProperty(nameof(KeyFrames))]
    public class DoubleAnimationUsingKeyFrames : Timeline
    {
        public DoubleKeyFrameCollection KeyFrames { get; } = new DoubleKeyFrameCollection();
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.DoubleKeyFrame</c>.</summary>
    public abstract class DoubleKeyFrame : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(DoubleKeyFrame),
                new PropertyMetadata((object)0.0));

        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(nameof(KeyTime), typeof(KeyTime), typeof(DoubleKeyFrame),
                new PropertyMetadata(KeyTime.Uniform));

        public double Value
        {
            get => (double)GetValue(ValueProperty)!;
            set => SetValue(ValueProperty, value);
        }

        public KeyTime KeyTime
        {
            get => (KeyTime)GetValue(KeyTimeProperty)!;
            set => SetValue(KeyTimeProperty, value);
        }
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.EasingDoubleKeyFrame</c>.</summary>
    public class EasingDoubleKeyFrame : DoubleKeyFrame
    {
        public IEasingFunction? EasingFunction { get; set; }
    }

    /// <summary>Concrete linear keyframe — XAML often spells it as <c>LinearDoubleKeyFrame</c>.</summary>
    public class LinearDoubleKeyFrame : DoubleKeyFrame { }

    /// <summary>Shim for <c>System.Windows.Media.Animation.DoubleKeyFrameCollection</c>.</summary>
    public class DoubleKeyFrameCollection : List<DoubleKeyFrame> { }

    /// <summary>Marker for easing functions referenced in XAML (BackEase, CubicEase, etc.).
    /// We don't model them — they're stored and ignored.</summary>
    public interface IEasingFunction { }

    /// <summary>
    /// Shim for <c>System.Windows.Media.Animation.KeyTime</c>. Tri-state value: a
    /// concrete <see cref="TimeSpan"/>, <see cref="Uniform"/>, or <see cref="Paced"/>.
    /// </summary>
    public struct KeyTime : IEquatable<KeyTime>
    {
        // 0 = uninitialized (treated as Uniform), 1 = TimeSpan, 2 = Uniform, 3 = Paced, 4 = Percent.
        private readonly int _kind;
        private readonly TimeSpan _timeSpan;
        private readonly double _percent;

        private KeyTime(int kind, TimeSpan ts, double pct)
        {
            _kind = kind;
            _timeSpan = ts;
            _percent = pct;
        }

        public static KeyTime FromTimeSpan(TimeSpan ts) => new KeyTime(1, ts, 0);
        public static KeyTime FromPercent(double percent) => new KeyTime(4, default, percent);
        public static KeyTime Uniform => new KeyTime(2, default, 0);
        public static KeyTime Paced => new KeyTime(3, default, 0);

        public TimeSpan TimeSpan => _kind == 1 ? _timeSpan : default;
        public double Percent => _kind == 4 ? _percent : 0;
        public KeyTimeType Type => _kind switch
        {
            1 => KeyTimeType.TimeSpan,
            2 => KeyTimeType.Uniform,
            3 => KeyTimeType.Paced,
            4 => KeyTimeType.Percent,
            _ => KeyTimeType.Uniform,
        };

        public static implicit operator KeyTime(TimeSpan ts) => FromTimeSpan(ts);

        public bool Equals(KeyTime other) =>
            _kind == other._kind && _timeSpan == other._timeSpan && _percent == other._percent;
        public override bool Equals(object? obj) => obj is KeyTime k && Equals(k);
        public override int GetHashCode() => HashCode.Combine(_kind, _timeSpan, _percent);
        public static bool operator ==(KeyTime a, KeyTime b) => a.Equals(b);
        public static bool operator !=(KeyTime a, KeyTime b) => !a.Equals(b);
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.KeyTimeType</c>.</summary>
    public enum KeyTimeType
    {
        Uniform,
        Percent,
        TimeSpan,
        Paced,
    }

    /// <summary>
    /// Shim for <c>System.Windows.Media.Animation.RepeatBehavior</c>. Either a finite
    /// iteration count (<c>new RepeatBehavior(3)</c>), a duration, or
    /// <see cref="Forever"/>.
    /// </summary>
    public struct RepeatBehavior : IEquatable<RepeatBehavior>
    {
        // 0 = count (default 1), 1 = duration, 2 = forever.
        private readonly int _kind;
        private readonly double _count;
        private readonly TimeSpan _duration;

        public RepeatBehavior(double count)
        {
            _kind = 0;
            _count = count;
            _duration = default;
        }

        public RepeatBehavior(TimeSpan duration)
        {
            _kind = 1;
            _count = 0;
            _duration = duration;
        }

        private RepeatBehavior(int kind)
        {
            _kind = kind;
            _count = 0;
            _duration = default;
        }

        public static RepeatBehavior Forever => new RepeatBehavior(2);

        public bool HasCount => _kind == 0;
        public bool HasDuration => _kind == 1;
        public double Count => _kind == 0 ? _count : 0;
        public TimeSpan Duration => _kind == 1 ? _duration : default;

        public bool Equals(RepeatBehavior other) =>
            _kind == other._kind && _count == other._count && _duration == other._duration;
        public override bool Equals(object? obj) => obj is RepeatBehavior r && Equals(r);
        public override int GetHashCode() => HashCode.Combine(_kind, _count, _duration);
        public static bool operator ==(RepeatBehavior a, RepeatBehavior b) => a.Equals(b);
        public static bool operator !=(RepeatBehavior a, RepeatBehavior b) => !a.Equals(b);
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.ClockState</c>.</summary>
    public enum ClockState
    {
        Active,
        Filling,
        Stopped,
    }

    /// <summary>Shim for <c>System.Windows.Media.Animation.FillBehavior</c>.</summary>
    public enum FillBehavior
    {
        HoldEnd,
        Stop,
    }
}
