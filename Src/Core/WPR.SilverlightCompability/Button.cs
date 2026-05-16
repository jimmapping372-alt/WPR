using System;

namespace WPR.SilverlightCompability
{
    public class Button : ContentControl
    {
        public event RoutedEventHandler? Click;

        /// <summary>Invoked by the host when this button has been hit-tested as the target of a press.</summary>
        internal void RaiseClick()
        {
            Click?.Invoke(this, new RoutedEventArgs { OriginalSource = this });
        }
    }
}
