using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using WPR.Installation;
using WPR.Models;
using WPR.Runtime;
using WPR.Storage;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace WPR
{
    public partial class MainPage : ContentPage
    {
        private readonly ApplicationsRepository _repository;
        private readonly WprInstaller _installer;
        private readonly GameLauncher _launcher;
        private readonly ObservableCollection<WprApplication> _apps;

        public MainPage()
        {
            InitializeComponent();

            var baseFolder = FileSystem.AppDataDirectory;
            _repository = new ApplicationsRepository(baseFolder);
            _installer = new WprInstaller(baseFolder);
            _launcher = new GameLauncher(baseFolder);
            _apps = new ObservableCollection<WprApplication>(_repository.Load());

            GamesList.ItemsSource = _apps;

            InstallButton.Clicked += InstallButton_Clicked;
            RunButton.Clicked += RunButton_Clicked;
        }

        private async void InstallButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var pickResult = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Choose WPR package",
                    FileTypes = new FilePickerFileType(new System.Collections.Generic.Dictionary<DevicePlatform, System.Collections.Generic.IEnumerable<string>>
                    {
                        { DevicePlatform.UWP, new [] { ".wpr" } },
                        { DevicePlatform.Android, new [] { "application/zip", "application/octet-stream" } },
                        { DevicePlatform.iOS, new [] { "public.data" } }
                    })
                });

                if (pickResult == null)
                {
                    return;
                }

                using var stream = await pickResult.OpenReadAsync();

                InstallProgress.Progress = 0;
                StatusLabel.Text = "Installing...";

                var progress = new Progress<int>(value =>
                {
                    InstallProgress.Progress = value / 100.0;
                });

                var cts = new CancellationTokenSource();

                var (error, app) = await _installer.InstallAsync(
                    stream,
                    progress,
                    confirmReplace: async existing =>
                    {
                        var answer = await DisplayAlert(
                            "Game already installed",
                            $"{existing.Name} is already installed. Reinstall?",
                            "Yes",
                            "No");
                        return answer;
                    },
                    cancellationToken: cts.Token);

                InstallProgress.Progress = 0;

                if (error == WprInstallError.None && app != null)
                {
                    // обновить список
                    var existingIndex = -1;
                    for (int i = 0; i < _apps.Count; i++)
                    {
                        if (_apps[i].ProductId == app.ProductId)
                        {
                            existingIndex = i;
                            break;
                        }
                    }

                    if (existingIndex >= 0)
                    {
                        _apps[existingIndex] = app;
                    }
                    else
                    {
                        _apps.Add(app);
                    }

                    StatusLabel.Text = $"Installed: {app.Name}";
                    await DisplayAlert("Installation", $"Game '{app.Name}' installed successfully.", "OK");
                }
                else
                {
                    StatusLabel.Text = "Installation failed";
                    await DisplayAlert("Installation failed", error.ToString(), "OK");
                }
            }
            catch (Exception ex)
            {
                InstallProgress.Progress = 0;
                StatusLabel.Text = "Installation error";
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void RunButton_Clicked(object sender, EventArgs e)
        {
            if (GamesList.SelectedItem is not WprApplication app)
            {
                await DisplayAlert("Run", "Please select a game to run.", "OK");
                return;
            }

            try
            {
                StatusLabel.Text = $"Launching: {app.Name}";
                _launcher.Launch(app);
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Launch error";
                await DisplayAlert("Launch error", ex.Message, "OK");
            }
        }
    }
}
