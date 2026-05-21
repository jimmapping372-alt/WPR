using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Phone.Shell
{
    public class PhoneApplicationService
    {
        private bool _AppActivated = false;

        static PhoneApplicationService()
        {
            Current = new PhoneApplicationService();
        }

        public PhoneApplicationService()
        {
            UserIdleDetectionMode = IdleDetectionMode.Disabled;
            ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;

            State = new Dictionary<string, object>();
        }

        public void HandleApplicationStart(bool anew)
        {
#if DEBUG
            Trace.WriteLine($"[wpr-trace] PhoneApplicationService.HandleApplicationStart(anew={anew}) firing Launching+Activated. " +
                $"Subscribers: Launching={CountInvocations(_Launching)} Activated={CountInvocations(_Activated)}");
#endif

            // WP7 fires Launching on a fresh launch (anew=true) and Activated on resume
            // (anew=false, IsApplicationInstancePreserved=true). We previously fired only
            // Activated which left games that initialize in Application_Launching wedged on
            // their splash forever (e.g. MonstaFish drawing Clear(Color.Black) and nothing
            // else because the scene system never got built).
            if (anew)
            {
                try { _Launching?.Invoke(this, new LaunchingEventArgs()); }
                catch (Exception ex)
                {
#if DEBUG
                    Trace.WriteLine("[wpr-ex] PhoneApplicationService.Launching handler threw: " + ex);
#else
                    _ = ex;
#endif
                }
            }

            try { _Activated?.Invoke(this, new ActivatedEventArgs(!anew)); }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine("[wpr-ex] PhoneApplicationService.Activated handler threw: " + ex);
#else
                _ = ex;
#endif
            }

            _AppActivated = true;
        }

        public void HandleApplicationExit()
        {
            Deactivated?.Invoke(this, new DeactivatedEventArgs());
            Closing?.Invoke(this, new ClosingEventArgs());

            // Recycle
            Current = new PhoneApplicationService();
        }

        private static int CountInvocations(Delegate? d) => d?.GetInvocationList().Length ?? 0;

        private event EventHandler<ActivatedEventArgs>? _Activated;

        public event EventHandler<ActivatedEventArgs>? Activated
        {
            add
            {
#if DEBUG
                Trace.WriteLine($"[wpr-trace] PhoneApplicationService.Activated += handler (_AppActivated={_AppActivated})");
#endif
                if (_AppActivated)
                {
                    value?.Invoke(this, new ActivatedEventArgs(false));
                } else
                {
                    _Activated += value;
                }
            }

            remove
            {
                _Activated -= value;
            }
        }
        public event EventHandler<DeactivatedEventArgs>? Deactivated;

        private event EventHandler<LaunchingEventArgs>? _Launching;
        public event EventHandler<LaunchingEventArgs>? Launching
        {
            add
            {
#if DEBUG
                Trace.WriteLine($"[wpr-trace] PhoneApplicationService.Launching += handler");
#endif
                _Launching += value;
            }
            remove { _Launching -= value; }
        }
        public event EventHandler<ClosingEventArgs>? Closing;

        /// <summary>
        /// WP8 fast-app-resume signal. Some apps wire <c>App.xaml</c> handlers like
        /// <c>RunningInBackground="OnRunningInBackground"</c>; the type must exist for the
        /// XAML-driven Delegate.CreateDelegate parameter resolution to succeed. Never raised
        /// by WPR — desktop has no equivalent lifecycle phase.
        /// </summary>
        public event EventHandler<RunningInBackgroundEventArgs>? RunningInBackground;

        public StartupMode StartupMode { get => StartupMode.Launch; }

        public static PhoneApplicationService? Current { get; private set; }

        public IDictionary<string, object> State { get; private set; }

        public IdleDetectionMode UserIdleDetectionMode { get; set; }
        public IdleDetectionMode ApplicationIdleDetectionMode { get; set; }
    }
}
