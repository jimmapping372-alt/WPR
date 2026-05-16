using System;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    public class DeactivatedEventArgs : EventArgs
    {
        public DeactivationReason Reason { get; internal set; }
    }
}
