using System.Runtime.CompilerServices;

// ResourceDictionary was moved into WPR.SilverlightCompability so that
// WPR.SilverlightCompability.FrameworkElement can expose a Resources property
// that returns this type without a circular project reference. We keep the
// forwarder here so any IL (including any pre-existing patched user assemblies
// that still reference WC's old version) finds the type at runtime.
[assembly: TypeForwardedTo(typeof(WPR.WindowsCompability.ResourceDictionary))]
