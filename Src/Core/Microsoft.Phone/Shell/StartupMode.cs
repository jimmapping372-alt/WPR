using System;

namespace Microsoft.Phone.Shell
{
    /// <summary>
    /// Integer values match the real WP7 SDK — games compare against literal
    /// integers in IL (see <see cref="WPR.SilverlightCompability.StartupMode"/>
    /// for the full diagnosis from Asphalt 5).
    /// </summary>
    public enum StartupMode
    {
        Launch = 1,
        Activate = 2,
    }
}
