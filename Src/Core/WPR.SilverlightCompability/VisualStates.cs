// Visual-state machine shims. WP Toolkit controls (PerformanceProgressBar,
// LongListSelector, Pivot, etc.) call VisualStateManager.GoToState in property-
// changed handlers to drive their templated visuals. Our renderer doesn't apply
// templates; the API surface here is enough for the IL to JIT + execute as
// no-ops (GoToState returns false to mean "no transition happened").

using System;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    /// <summary>Shim for <c>System.Windows.VisualState</c>.</summary>
    public class VisualState : DependencyObject
    {
        public string? Name { get; set; }
        public Storyboard? Storyboard { get; set; }
    }

    /// <summary>Shim for <c>System.Windows.VisualStateGroup</c>. Holds named visual states.</summary>
    public class VisualStateGroup : DependencyObject
    {
        public string? Name { get; set; }
        public IList<VisualState> States { get; } = new List<VisualState>();
        public IList<VisualTransition> Transitions { get; } = new List<VisualTransition>();
        public VisualState? CurrentState { get; internal set; }

#pragma warning disable CS0067
        public event EventHandler<VisualStateChangedEventArgs>? CurrentStateChanging;
        public event EventHandler<VisualStateChangedEventArgs>? CurrentStateChanged;
#pragma warning restore CS0067
    }

    /// <summary>Shim for <c>System.Windows.VisualTransition</c>. Storyboard between two named states.</summary>
    public class VisualTransition : DependencyObject
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public Duration GeneratedDuration { get; set; } = new Duration(TimeSpan.Zero);
        public Storyboard? Storyboard { get; set; }
    }

    /// <summary>Shim for <c>System.Windows.VisualStateChangedEventArgs</c>.</summary>
    public class VisualStateChangedEventArgs : EventArgs
    {
        public VisualState? OldState { get; set; }
        public VisualState? NewState { get; set; }
        public Control? Control { get; set; }
    }

    /// <summary>
    /// Shim for <c>System.Windows.VisualStateManager</c>. The single touched API is
    /// the static <see cref="GoToState"/>; our implementation returns <c>false</c>
    /// to signal "no transition occurred", which user code typically treats as
    /// "OK, we'll skip the animation" without erroring.
    /// </summary>
    public class VisualStateManager : DependencyObject
    {
        public static readonly DependencyProperty VisualStateGroupsProperty =
            DependencyProperty.RegisterAttached("VisualStateGroups", typeof(IList<VisualStateGroup>),
                typeof(VisualStateManager), new PropertyMetadata((object?)null));

        public static IList<VisualStateGroup>? GetVisualStateGroups(DependencyObject obj)
            => (IList<VisualStateGroup>?)obj?.GetValue(VisualStateGroupsProperty);

        public static void SetVisualStateGroups(DependencyObject obj, IList<VisualStateGroup> value)
            => obj?.SetValue(VisualStateGroupsProperty, value);

        public static bool GoToState(Control control, string stateName, bool useTransitions)
            => false;
    }

    /// <summary>Shim for <c>System.Windows.SizeChangedEventHandler</c>.</summary>
    public delegate void SizeChangedEventHandler(object sender, SizeChangedEventArgs e);

    /// <summary>Shim for <c>System.Windows.SizeChangedEventArgs</c>.</summary>
    public class SizeChangedEventArgs : RoutedEventArgs
    {
        public Size PreviousSize { get; set; }
        public Size NewSize { get; set; }
    }

    /// <summary>
    /// Shim for <c>System.Windows.Media.CompositionTarget</c>. The only WP7 use we
    /// see is subscribing to <see cref="Rendering"/> for per-frame callbacks
    /// (Panorama's entrance animation in design-tool mode). We never raise it.
    /// </summary>
    public static class CompositionTarget
    {
#pragma warning disable CS0067
        public static event EventHandler? Rendering;
#pragma warning restore CS0067
    }
}
