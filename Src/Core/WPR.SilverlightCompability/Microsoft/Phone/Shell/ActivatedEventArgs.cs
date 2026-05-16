using System;
using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    public class ActivatedEventArgs : EventArgs
    {
        public bool IsApplicationInstancePreserved { get; internal set; }
    }
}
