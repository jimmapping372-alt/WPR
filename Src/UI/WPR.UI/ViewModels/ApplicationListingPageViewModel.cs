using ReactiveUI;
using System.Threading.Tasks;
using WPR;
using WPR.Models;
using WPR.Common;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Reactive;
using Avalonia.Threading;
using System;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

namespace WPR.UI.ViewModels
{
    public class ApplicationListingPageViewModel : ViewModelBase
    {
        private string _SearchText;
        private ObservableCollection<ApplicationItemViewModel> _Applications;
        private ApplicationItemViewModel? _ChoosenApp;
        private InstallingAppViewModel? _Installing;
        private bool _IsViewingInstall;

        private readonly LibraryScanner _LibraryScanner;
        private readonly List<DiscoveredApplication> _Discovered = new List<DiscoveredApplication>();

        public ReactiveCommand<string, Unit> AppSearchCommand;


        public ReactiveCommand<ApplicationItemViewModel?, Unit> RunAppCommand;

        public Interaction<Application, bool> DeleteExistingAppInteraction;

        public ReactiveCommand<ApplicationItemViewModel?, Unit> DeleteAppCommand;

        public ReactiveCommand<Unit, Unit> ShowInstallProgressCommand;

        public event EventHandler<ApplicationItemViewModel>? InstallRequested;


        public string SearchText
        {
            get { return _SearchText; }
            set { this.RaiseAndSetIfChanged(ref _SearchText, value); }
        }

        public ApplicationItemViewModel? ChoosenApp
        {
            get { return _ChoosenApp; }
            set
            {
                this.RaiseAndSetIfChanged(ref _ChoosenApp, value);
                if (value != null && value.IsInstalling)
                {
                    // Selecting the in-flight install item navigates to the
                    // install detail pane (showing the progress bar) instead
                    // of treating it like an ordinary app detail.
                    IsViewingInstall = true;
                }
                else if (value != null && _IsViewingInstall)
                {
                    // Selecting any other app implicitly leaves the install
                    // progress view.
                    IsViewingInstall = false;
                }
                this.RaisePropertyChanged(nameof(IsDetailEmpty));
                this.RaisePropertyChanged(nameof(IsDetailApp));
            }
        }

        public CancellationTokenSource? CancelSource { get; set; }

        public InstallingAppViewModel? Installing
        {
            get { return _Installing; }
            set
            {
                this.RaiseAndSetIfChanged(ref _Installing, value);
                // Focus the install pane on start; release it on completion.
                IsViewingInstall = value != null;
                this.RaisePropertyChanged(nameof(IsDetailEmpty));
                this.RaisePropertyChanged(nameof(IsDetailApp));
                this.RaisePropertyChanged(nameof(IsDetailInstalling));
                // Rebuild the library list so the discovered "available" entry
                // for this product is replaced by an "installing" entry — and
                // restored to "available" once the install completes / cancels.
                UpdateApplications();
            }
        }

        public bool IsViewingInstall
        {
            get { return _IsViewingInstall; }
            private set
            {
                this.RaiseAndSetIfChanged(ref _IsViewingInstall, value);
                this.RaisePropertyChanged(nameof(IsDetailEmpty));
                this.RaisePropertyChanged(nameof(IsDetailApp));
                this.RaisePropertyChanged(nameof(IsDetailInstalling));
            }
        }

        public bool IsDetailEmpty => _ChoosenApp == null && !_IsViewingInstall;
        public bool IsDetailApp => _ChoosenApp != null && !_IsViewingInstall;
        public bool IsDetailInstalling => _IsViewingInstall && _Installing != null;

        public ObservableCollection<ApplicationItemViewModel> Applications {
            get { return _Applications; }
            set { this.RaiseAndSetIfChanged(ref _Applications, value); }
        }

        public void UpdateApplicationList(string text)
        {
            try
            {
                string filter = (text ?? "").ToLower();

                List<ApplicationItemViewModel> installed = ApplicationContext.Current.Applications!
                    .Where(app => app.Name.ToLower().Contains(filter))
                    .OrderBy(app => app.Name.ToLower())
                    .Select(app => new ApplicationItemViewModel(app))
                    .ToList();

                HashSet<string> installedProductIds = new HashSet<string>(
                    installed.Where(i => !string.IsNullOrEmpty(i.ProductId)).Select(i => i.ProductId!),
                    StringComparer.OrdinalIgnoreCase);

                // Suppress the discovered entry for whatever's installing right
                // now — we'll insert an installing-mode entry in its place below
                // so the library list shows one row per product rather than
                // duplicating "available" + "installing" for the same app.
                string? installingProductId = _Installing?.ProductId;

                List<ApplicationItemViewModel> discovered = _Discovered
                    .Where(d => !installedProductIds.Contains(d.Preview.ProductId))
                    .Where(d => installingProductId == null
                        || !string.Equals(d.Preview.ProductId, installingProductId, StringComparison.OrdinalIgnoreCase))
                    .Where(d => (d.Preview.Name ?? "").ToLower().Contains(filter))
                    .OrderBy(d => (d.Preview.Name ?? "").ToLower())
                    .Select(d => new ApplicationItemViewModel(d.XapFilePath, d.Preview))
                    .ToList();

                IEnumerable<ApplicationItemViewModel> combined = installed.Concat(discovered);

                // Splice in an installing item. It's not in either source list
                // (installed = DB rows, discovered = un-installing scanner) so
                // it lives alongside both. Put it at the top of the available
                // section by ordering it after installed items.
                if (_Installing != null
                    && (_Installing.Name ?? "").ToLower().Contains(filter))
                {
                    var installingItem = new ApplicationItemViewModel(_Installing);
                    combined = installed.Concat(new[] { installingItem }).Concat(discovered);
                }

                _ChoosenApp = null;

                Applications =
                    new ObservableCollection<ApplicationItemViewModel>(combined);

                foreach (ApplicationItemViewModel appItem in Applications)
                {
                    appItem.UninstallRequested += OnAppUninstallRequested;
                    appItem.InstallRequested += OnAppInstallRequested;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine( $"[ex] WPR.UI - ApplicationListingViewModel: " +
                    $"Unable to query application database with exception:\n {ex}");
                Log.Error(LogCategory.AppList,
                    $"Unable to query application database with exception:\n {ex}");
                Applications = new ObservableCollection<ApplicationItemViewModel>();
            }
        }

        private void OnAppUninstallRequested(object? sender, ApplicationItemViewModel appItem)
        {
            _ = DeleteApplicationAsync(appItem);
        }

        private void OnAppInstallRequested(object? sender, ApplicationItemViewModel appItem)
        {
            // Surface to the page, which owns the dialogs (delete-existing prompt,
            // result message box) and the file/stream lifecycle for the install flow.
            InstallRequested?.Invoke(this, appItem);
        }

        public void UpdateApplications()
        {
            UpdateApplicationList(SearchText);
        }

        public ApplicationListingPageViewModel()
        {
            _SearchText = "";
            Applications = new ObservableCollection<ApplicationItemViewModel>();
            DeleteExistingAppInteraction = new Interaction<Application, bool>();

            // In ApplicationListingPageViewModel constructor
            var deleteEnabled = this.WhenAnyValue(x => x.ChoosenApp).Select(x => x != null && x.IsInstalled);
            DeleteAppCommand = ReactiveCommand.CreateFromTask<ApplicationItemViewModel>(DeleteApplicationAsync, deleteEnabled);
            RunAppCommand = ReactiveCommand.CreateFromTask<ApplicationItemViewModel>(RunApplicationAsync, deleteEnabled);

            ShowInstallProgressCommand = ReactiveCommand.Create(() =>
            {
                if (_Installing != null) IsViewingInstall = true;
            });

            this.WhenAnyValue(v => v.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(text => UpdateApplicationList(text));

            _LibraryScanner = new LibraryScanner();
            _LibraryScanner.Discovered += OnLibraryDiscovered;
            _LibraryScanner.Removed += OnLibraryRemoved;
            _LibraryScanner.Path = Configuration.Current?.GameLibraryPath;

            // Initial scan: synchronous so the first paint shows discovered items.
            // The folder is local and small in practice; fine to do on the UI thread.
            foreach (DiscoveredApplication entry in _LibraryScanner.ScanOnce())
            {
                _Discovered.Add(entry);
            }

            Configuration.GameLibraryPathChanged += OnConfigurationLibraryPathChanged;

            UpdateApplicationList(SearchText);
        }

        private void OnConfigurationLibraryPathChanged(object? sender, string? newPath)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _Discovered.Clear();
                _LibraryScanner.Path = newPath;
                foreach (DiscoveredApplication entry in _LibraryScanner.ScanOnce())
                {
                    _Discovered.Add(entry);
                }
                UpdateApplications();
            });
        }

        private void OnLibraryDiscovered(object? sender, DiscoveredApplication entry)
        {
            Dispatcher.UIThread.Post(() =>
            {
                int idx = _Discovered.FindIndex(d =>
                    string.Equals(d.XapFilePath, entry.XapFilePath, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) _Discovered[idx] = entry;
                else _Discovered.Add(entry);
                UpdateApplications();
            });
        }

        private void OnLibraryRemoved(object? sender, string path)
        {
            Dispatcher.UIThread.Post(() =>
            {
                int removed = _Discovered.RemoveAll(d =>
                    string.Equals(d.XapFilePath, path, StringComparison.OrdinalIgnoreCase));
                if (removed > 0) UpdateApplications();
            });
        }

            public async Task<ApplicationInstallError> InstallAsync(
                Func<Task<Stream>> openStream,
                ApplicationPreview preview)
            {
                CancelSource = new CancellationTokenSource();
                Installing = new InstallingAppViewModel(preview, () => CancelSource?.Cancel());

                try
                {
                    using Stream stream = await openStream();
                    return await ApplicationInstaller.Install(
                        stream,
                        progress => Dispatcher.UIThread.Post(() =>
                        {
                            if (Installing != null) Installing.Progress = progress;
                        }),
                        app => DeleteExistingAppInteraction.Handle(app),
                        CancelSource.Token);
                }
                finally
                {
                    Installing = null;
                }
            }

            private async Task RunApplicationAsync(ApplicationItemViewModel app) {
                if (app?.Model != null) ApplicationLaunchRequest.Ask(app.Model);
            }

            private async Task DeleteApplicationAsync(ApplicationItemViewModel app) {
                if (app?.Model == null) return;
                ApplicationContext.Current.Applications.Remove(app.Model);
                await ApplicationContext.Current.SaveChangesAsync(); // Persist the uninstall
                Applications.Remove(app);
                UpdateApplications();
            }
    }
}
