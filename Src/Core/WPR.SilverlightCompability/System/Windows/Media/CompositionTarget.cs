using System;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
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
