using System;
using Microsoft.Phone.Controls;

namespace WPR.SilverlightCompability
{
    public class PhoneApplicationFrame : Frame
    {
        public new PhoneApplicationPage? Content => _currentPage;

        /// <summary>Fired when the frame becomes obscured (e.g. lock screen). No-op shim — never raised.</summary>
        public event EventHandler<ObscuredEventArgs>? Obscured;

        /// <summary>
        /// Fired when the frame becomes unobscured. The real WP API is asymmetric — Obscured
        /// passes an <see cref="ObscuredEventArgs"/>, but Unobscured is plain <see cref="EventHandler"/>
        /// because there's nothing to report when the frame regains visibility. We mirror that
        /// signature so user IL <c>add_Unobscured(EventHandler)</c> resolves correctly.
        /// </summary>
        public event EventHandler? Unobscured;

        // Keep these "used" to silence CS0067 — they're part of the public contract for IL binding.
        private void _SuppressUnusedWarnings()
        {
            Obscured?.Invoke(this, new ObscuredEventArgs(false));
            Unobscured?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dispatch a hardware Back-key press. Order matches real WP7:
        ///   1. Current page's <c>BackKeyPress</c> handler runs; if it cancels,
        ///      the press is consumed and nothing else happens (return true).
        ///   2. Otherwise, if <c>CanGoBack</c>, navigate back and return true.
        ///   3. Otherwise return false — at the root of the back-stack, real
        ///      WP7 exits the app; the host should close its window.
        /// </summary>
        public bool HandleBackKey()
        {
            if (_currentPage != null && _currentPage.RaiseBackKeyPress())
                return true;
            if (CanGoBack) { GoBack(); return true; }
            return false;
        }
    }
}
