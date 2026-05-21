using System;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Shim for <c>Microsoft.Phone.Shell.StartupMode</c>. The integer values must
    /// match the real WP7 SDK — games compare against the literal <c>1</c> in IL
    /// (Asphalt 5: <c>if ((int)PhoneApplicationService.Current.StartupMode == 1)</c>
    /// to detect a fresh launch and push their splash state). Using the C# default
    /// ordering of <c>Launch=0, Activate=1</c> caused those checks to silently take
    /// the wrong branch — Asphalt 5 ended up in an empty placeholder state and sat
    /// on a blank screen forever.
    /// </summary>
    public enum StartupMode
    {
        Launch = 1,
        Activate = 2,
    }
}
