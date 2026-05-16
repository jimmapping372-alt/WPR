using Avalonia.Controls;
using WPR.UI.ViewModels;

namespace WPR.UI.Pages
{
    public partial class AchievementsPage : UserControl
    {
        public AchievementsPage()
        {
            InitializeComponent();
            DataContext = new AchievementsPageViewModel();
        }
    }
}
