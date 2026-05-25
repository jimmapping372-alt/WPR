using Avalonia.Controls;
using Avalonia.Input;
using WPR.UI.ViewModels;

namespace WPR.UI.Views
{
    public partial class ApplicationView : UserControl
    {
        public ApplicationView()
        {
            InitializeComponent();
#if __ANDROID__
            // Touch UX: a single tap on a row launches the app. On Desktop this
            // would conflict with click-to-select + double-click-to-launch (see
            // ApplicationListingPage.axaml.cs), so the handler is Android-only.
            AddHandler(PointerReleasedEvent, OnPointerReleased, Avalonia.Interactivity.RoutingStrategies.Bubble);
#endif
        }

#if __ANDROID__
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton != MouseButton.Left)
            {
                return;
            }

            if (e.Source is Button)
            {
                return;
            }

            if (DataContext is ApplicationItemViewModel vm)
            {
                WPR.UI.ApplicationLaunchRequest.Ask(vm.Model);
            }
        }
#endif
    }
}
