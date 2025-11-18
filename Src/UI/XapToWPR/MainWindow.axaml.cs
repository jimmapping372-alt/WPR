using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace XapToWPR;

public partial class MainWindow : Window
{
    private TextBox? _inputPathTextBox;
    private TextBox? _outputPathTextBox;
    private TextBlock? _statusTextBlock;
    private readonly XapToWprConverter _converter = new();

    public MainWindow()
    {
        InitializeComponent();

        _inputPathTextBox = this.FindControl<TextBox>("InputPathTextBox");
        _outputPathTextBox = this.FindControl<TextBox>("OutputPathTextBox");
        _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");

        this.FindControl<Button>("BrowseInputButton").AddHandler(Button.ClickEvent, BrowseInputClicked);
        this.FindControl<Button>("BrowseOutputButton").AddHandler(Button.ClickEvent, BrowseOutputClicked);
        this.FindControl<Button>("ConvertButton").AddHandler(Button.ClickEvent, ConvertClicked);
    }

    private async void BrowseInputClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose XAP file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("XAP file") { Patterns = new[] { "*.xap" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files != null && files.Count > 0 && _inputPathTextBox != null)
        {
            _inputPathTextBox.Text = files[0].Path.LocalPath;
        }
    }

    private async void BrowseOutputClicked(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Choose output WPR file",
            DefaultExtension = "wpr",
            ShowOverwritePrompt = true
        });

        if (file != null && _outputPathTextBox != null)
        {
            _outputPathTextBox.Text = file.Path.LocalPath;
        }
    }

    private async void ConvertClicked(object? sender, RoutedEventArgs e)
    {
        if (_inputPathTextBox == null || _outputPathTextBox == null || _statusTextBlock == null)
        {
            return;
        }

        var input = _inputPathTextBox.Text;
        var output = _outputPathTextBox.Text;

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            _statusTextBlock.Text = "Please select both input XAP and output WPR paths.";
            return;
        }

        _statusTextBlock.Text = "Converting...";

        bool succeeded = false;
        string? errorMessage = null;

        try
        {
            var progress = new Progress<int>(p =>
            {
                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Text = $"Converting... {p}%";
                }
            });

            await _converter.ConvertAsync(input, output, progress);
            succeeded = true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        if (_statusTextBlock == null)
        {
            return;
        }

        if (!succeeded)
        {
            _statusTextBlock.Text = errorMessage != null
                ? $"Conversion failed: {errorMessage}"
                : "Conversion failed.";
            return;
        }

        try
        {
            using var wprStream = File.OpenRead(output);
            using var wprArchive = new ZipArchive(wprStream, ZipArchiveMode.Read, leaveOpen: false);

            var manifestEntry = wprArchive.GetEntry("manifest.json");
            if (manifestEntry != null)
            {
                using var manifestStream = manifestEntry.Open();
                var manifest = JsonSerializer.Deserialize<WprManifestView>(manifestStream);
                if (manifest != null)
                {
                    _statusTextBlock.Text = $"Conversion completed: {manifest.Name} (ProductId={manifest.ProductId})";
                    return;
                }
            }

            _statusTextBlock.Text = "Conversion completed successfully (manifest.json not found or invalid).";
        }
        catch
        {
            _statusTextBlock.Text = "Conversion completed successfully (failed to read manifest.json).";
        }
    }

    private sealed class WprManifestView
    {
        public string Name { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
    }
}
