using System;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Stub for <c>Microsoft.Phone.Shell.PhoneApplicationService</c>. Apps register one inside
    /// <c>&lt;Application.ApplicationLifetimeObjects&gt;</c> and wire its lifecycle events
    /// (<see cref="Launching"/>, <see cref="Closing"/>, <see cref="Activated"/>, <see cref="Deactivated"/>)
    /// to handlers on App.xaml.cs. Most members return safe defaults; the events are present so
    /// XAML hookup succeeds and so WPR can fire <see cref="Launching"/> at boot if it chooses.
    /// </summary>
    public sealed class PhoneApplicationService
    {
        private static PhoneApplicationService? _Current;
        public static PhoneApplicationService? Current => _Current;

        static PhoneApplicationService()
        {
            _Current = new PhoneApplicationService();
        }

        public PhoneApplicationService()
        {
            _Current = this;
        }

        public IDictionary<string, object> State { get; } = new Dictionary<string, object>();

        public TimeSpan ApplicationIdleTimeout { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan UserIdleTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public IdleDetectionMode UserIdleDetectionMode { get; set; } = IdleDetectionMode.Enabled;
        public IdleDetectionMode ApplicationIdleDetectionMode { get; set; } = IdleDetectionMode.Enabled;

        public StartupMode StartupMode { get; internal set; } = StartupMode.Launch;

        public Version ContractVersion { get; } = new Version(8, 0);

        public event EventHandler<LaunchingEventArgs>? Launching;
        public event EventHandler<ClosingEventArgs>? Closing;
        public event EventHandler<ActivatedEventArgs>? Activated;
        public event EventHandler<DeactivatedEventArgs>? Deactivated;
        public event EventHandler<RunningInBackgroundEventArgs>? RunningInBackground;

        internal void RaiseLaunching() => Launching?.Invoke(this, new LaunchingEventArgs());
        internal void RaiseClosing() => Closing?.Invoke(this, new ClosingEventArgs());
        internal void RaiseActivated(bool preserved) =>
            Activated?.Invoke(this, new ActivatedEventArgs { IsApplicationInstancePreserved = preserved });
        internal void RaiseDeactivated(DeactivationReason reason) =>
            Deactivated?.Invoke(this, new DeactivatedEventArgs { Reason = reason });
        internal void RaiseRunningInBackground() =>
            RunningInBackground?.Invoke(this, new RunningInBackgroundEventArgs());
    }

    public enum IdleDetectionMode { Enabled, Disabled }

    public enum StartupMode { Launch, Activate }

    public enum DeactivationReason { ApplicationAction, PowerSavingMode, UserAction }

    public class LaunchingEventArgs : EventArgs { }

    public class ClosingEventArgs : EventArgs { }

    public class ActivatedEventArgs : EventArgs
    {
        public bool IsApplicationInstancePreserved { get; internal set; }
    }

    public class DeactivatedEventArgs : EventArgs
    {
        public DeactivationReason Reason { get; internal set; }
    }

    public class RunningInBackgroundEventArgs : EventArgs { }
}
