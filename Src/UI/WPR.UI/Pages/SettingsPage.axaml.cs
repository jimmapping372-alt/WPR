using Avalonia.Controls;
using WPR.Common;

using ReactiveUI;
using System;
using System.Linq;

using Microsoft.Xna.Framework.GamerServices;
using MessageBox.Avalonia;

namespace WPR.UI.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();

            TextBox gamerTagTextBox = this.Get<TextBox>("gamerTagTextBox");
            if (Configuration.Current.GamerTag != null)
            {
                gamerTagTextBox.Text = Configuration.Current.GamerTag;
            }

            gamerTagTextBox.WhenAnyValue(x => x.Text).Subscribe(text =>
            {
                if (Gamer.SignedInGamers.Count != 0)
                {
                    Gamer.SignedInGamers[0].Gamertag = text;
                }

                Configuration.Current.GamerTag = text;
                Configuration.Current.Save();
            });

            WireHighlightColorPicker();

            TextBox pathTextBox = this.Get<TextBox>("dataStoragePathText");
            pathTextBox.Text = Configuration.Current.DataStorePath;

            Button pathChangeBtn = this.Get<Button>("dataStoragePathBrowse");
            pathChangeBtn.Click += async (obj, args) =>
            {
                string? resultFolder = await new OpenFolderDialog()
                {
                    Directory = Configuration.Current.DataStorePath
                }.ShowAsync(GetWindow());

                if (resultFolder != null)
                {
                    pathTextBox.Text = resultFolder;
                    Configuration.Current.DataStorePath = resultFolder;
                    Configuration.Current.Save();

                    var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(
                        title: Properties.Resources.SuccessfullyChanged,
                        text: Properties.Resources.SuccessfullyChangedDataPathMsg,
                        icon: MessageBox.Avalonia.Enums.Icon.Success,
                        windowStartupLocation: WindowStartupLocation.CenterScreen);

                    await msgBox.ShowDialog(GetWindow());
                }
            };

            this.Get<Button>("restoreDefaultStoragePathBtn").Click += async (obj, args) =>
            {
                Configuration.Current.RestoreDefaultDataStoragePath();
                pathTextBox.Text = Configuration.Current.DataStorePath;

                Configuration.Current.Save();

                var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(
                    title: Properties.Resources.SuccessfullyChanged,
                    text: Properties.Resources.SuccessfullyChangedDataPathMsg,
                    icon: MessageBox.Avalonia.Enums.Icon.Success,
                    windowStartupLocation: WindowStartupLocation.CenterScreen);

                await msgBox.ShowDialog(GetWindow());
            };

            TextBox libraryPathTextBox = this.Get<TextBox>("gameLibraryPathText");
            libraryPathTextBox.Text = Configuration.Current.GameLibraryPath ?? "";

            Button libraryPathBrowseBtn = this.Get<Button>("gameLibraryPathBrowse");
            libraryPathBrowseBtn.Click += async (obj, args) =>
            {
                string? resultFolder = await new OpenFolderDialog()
                {
                    Directory = Configuration.Current.GameLibraryPath ?? Configuration.Current.DataStorePath
                }.ShowAsync(GetWindow());

                if (resultFolder != null)
                {
                    libraryPathTextBox.Text = resultFolder;
                    Configuration.Current.GameLibraryPath = resultFolder;
                    Configuration.Current.Save();
                }
            };

            this.Get<Button>("clearGameLibraryPathBtn").Click += (obj, args) =>
            {
                libraryPathTextBox.Text = "";
                Configuration.Current.GameLibraryPath = null;
                Configuration.Current.Save();
            };
        }

        /// <summary>
        /// Populate the highlight-color combo with the WP7 accent palette,
        /// seed selection from <see cref="Configuration.AccentColor"/>, and on
        /// change persist the chosen hex back to configuration plus update the
        /// preview swatch. The actual phone theme reads this on next game launch
        /// (in <c>WPR.WindowsCompability.Application</c>'s ctor); we don't apply
        /// it live to running games because <c>PhoneTheme</c>-keyed brushes get
        /// captured into individual element <c>Style</c>s during XAML load.
        /// </summary>
        private void WireHighlightColorPicker()
        {
            var combo = this.Get<ComboBox>("highlightColorBox");
            var swatch = this.Get<Border>("highlightColorSwatch");
            combo.ItemsSource = WP7AccentColors.Presets;

            string? saved = Configuration.Current.AccentColor;
            WP7AccentColor initial = WP7AccentColors.Presets
                .FirstOrDefault(c => string.Equals(c.Hex, saved, StringComparison.OrdinalIgnoreCase))
                ?? WP7AccentColors.Default;
            combo.SelectedItem = initial;
            swatch.Background = initial.Brush;

            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem is not WP7AccentColor pick) return;
                Configuration.Current.AccentColor = pick.Hex;
                Configuration.Current.Save();
                swatch.Background = pick.Brush;
            };
        }

        Window GetWindow() => VisualRoot as Window ?? throw new NullReferenceException("Invalid Owner");
    }
}
