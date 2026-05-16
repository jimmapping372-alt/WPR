using Avalonia.Controls;
using Avalonia.ReactiveUI;
using WPR.UI.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Reactive.Linq;
using WPR.Models;
using WPR.Common;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;

namespace WPR.UI.Pages
{
    public partial class ApplicationListingPage : ReactiveUserControl<ApplicationListingPageViewModel>
    {
        private List<FilePickerFileType> AppInstallFileFilters;

        public ApplicationListingPage()
        {
            InitializeComponent();
            ApplicationListingPageViewModel vm = new ApplicationListingPageViewModel();
            DataContext = vm;

            AppInstallFileFilters = new List<FilePickerFileType>
            {
                new FilePickerFileType("XAP file")
                {
                    Patterns = new List<string> { "*.xap" }
                },
                new FilePickerFileType("All files")
                {
                    Patterns = new List<string> { "*.*" }
                }
            };

            // Persistent handler so both the "+" button and the per-item
            // Install command (for library-discovered XAPs) share the same prompt.
            vm.DeleteExistingAppInteraction.RegisterHandler(context =>
                Dispatcher.UIThread.InvokeAsync(async () =>
            {
                Application app = context.Input;
                var msgResult = await MessageBoxUtils.GetMessageDialogResult(
                    title: Properties.Resources.ApplicationAlreadyInstalled,
                    text: String.Format(Properties.Resources.ApplicationAlreadyInstalledDescription, app.Name),
                    icon: MessageBox.Avalonia.Enums.Icon.Question,
                    buttons: MessageBox.Avalonia.Enums.ButtonEnum.YesNo);

                context.SetOutput(msgResult == MessageBox.Avalonia.Enums.ButtonResult.Yes);
            }));

            vm.InstallRequested += OnDiscoveredAppInstallRequested;

            this.Get<Button>("addNewAppButton").Click += AddNewAppButton_Click;
        }

        private async void AddNewAppButton_Click(object? sender, RoutedEventArgs e)
        {
            var result = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Choose XAP file",
                FileTypeFilter = AppInstallFileFilters
            });

            if (result == null || result.Count < 1) return;
            var file = result[0];

            ApplicationPreview? preview;
            using (var previewStream = await file.OpenReadAsync())
            {
                preview = ApplicationInstaller.ReadPreview(previewStream);
            }

            if (preview == null)
            {
                await MessageBoxUtils.GetMessageDialogResult(
                    title: Properties.Resources.InstallationFailed,
                    text: LocaleUtils.GetDisplayName(ApplicationInstallError.InvalidManifestFiles),
                    icon: MessageBox.Avalonia.Enums.Icon.Error);
                return;
            }

            await RunInstallAsync(async () => await file.OpenReadAsync(), preview);
        }

        private async void OnDiscoveredAppInstallRequested(object? sender, ApplicationItemViewModel appItem)
        {
            string? xapPath = appItem.XapFilePath;
            if (string.IsNullOrEmpty(xapPath) || !File.Exists(xapPath))
            {
                await MessageBoxUtils.GetMessageDialogResult(
                    title: Properties.Resources.InstallationFailed,
                    text: LocaleUtils.GetDisplayName(ApplicationInstallError.MissingManifestFiles),
                    icon: MessageBox.Avalonia.Enums.Icon.Error);
                return;
            }

            ApplicationPreview? preview = appItem.Preview;
            if (preview == null)
            {
                using FileStream previewStream = new FileStream(xapPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                preview = ApplicationInstaller.ReadPreview(previewStream);
            }

            if (preview == null)
            {
                await MessageBoxUtils.GetMessageDialogResult(
                    title: Properties.Resources.InstallationFailed,
                    text: LocaleUtils.GetDisplayName(ApplicationInstallError.InvalidManifestFiles),
                    icon: MessageBox.Avalonia.Enums.Icon.Error);
                return;
            }

            await RunInstallAsync(
                () => Task.FromResult<Stream>(new FileStream(xapPath, FileMode.Open, FileAccess.Read, FileShare.Read)),
                preview);
        }

        private async Task RunInstallAsync(Func<Task<Stream>> openStream, ApplicationPreview preview)
        {
            ApplicationInstallError err = await ViewModel!.InstallAsync(openStream, preview);

            if (err != ApplicationInstallError.None && err != ApplicationInstallError.Canceled)
            {
                await MessageBoxUtils.GetMessageDialogResult(
                    title: Properties.Resources.InstallationFailed,
                    text: LocaleUtils.GetDisplayName(err),
                    icon: MessageBox.Avalonia.Enums.Icon.Error);
            }

            ViewModel!.UpdateApplicationList(ViewModel!.SearchText);
        }

        Window GetWindow() => VisualRoot as Window ?? throw new NullReferenceException("Invalid Owner");
        TopLevel GetTopLevel() => VisualRoot as TopLevel ?? throw new NullReferenceException("Invalid Owner");
        IStorageProvider GetStorageProvider() => GetTopLevel().StorageProvider;
    }
}
