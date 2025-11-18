using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WPR.Models;
using WPR.Storage;

namespace WPR.Installation
{
    public enum WprInstallError
    {
        None,
        MissingManifest,
        InvalidManifest,
        Canceled,
        UnexpectedError
    }

    public sealed class WprInstaller
    {
        private readonly string _appsRoot;
        private readonly ApplicationsRepository _repository;

        public WprInstaller(string baseFolder)
        {
            _appsRoot = Path.Combine(baseFolder, "apps");
            Directory.CreateDirectory(_appsRoot);
            _repository = new ApplicationsRepository(baseFolder);
        }

        public async Task<(WprInstallError error, WprApplication? app)> InstallAsync(
            Stream wprStream,
            IProgress<int>? progress,
            Func<WprApplication, Task<bool>> confirmReplace,
            CancellationToken cancellationToken)
        {
            try
            {
                progress?.Report(1);

                using var archive = new ZipArchive(wprStream, ZipArchiveMode.Read, leaveOpen: false);

                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null)
                {
                    return (WprInstallError.MissingManifest, null);
                }

                WprManifest? manifest;
                using (var manifestStream = manifestEntry.Open())
                using (var reader = new StreamReader(manifestStream))
                {
                    var json = await reader.ReadToEndAsync();
                    manifest = JsonConvert.DeserializeObject<WprManifest>(json);
                }

                if (manifest == null ||
                    string.IsNullOrWhiteSpace(manifest.Name) ||
                    string.IsNullOrWhiteSpace(manifest.ProductId) ||
                    string.IsNullOrWhiteSpace(manifest.EntryPointAssembly) ||
                    string.IsNullOrWhiteSpace(manifest.EntryPointType) ||
                    string.IsNullOrWhiteSpace(manifest.Version))
                {
                    return (WprInstallError.InvalidManifest, null);
                }

                var productTrimmed = manifest.ProductId.Trim('{', '}');
                var appFolder = Path.Combine(_appsRoot, productTrimmed);

                // Проверка существующей установки
                var existingApps = _repository.Load();
                var existing = existingApps.FirstOrDefault(a => a.ProductId == productTrimmed);
                if (existing != null)
                {
                    if (!await confirmReplace(existing))
                    {
                        return (WprInstallError.Canceled, null);
                    }

                    if (Directory.Exists(appFolder))
                    {
                        Directory.Delete(appFolder, recursive: true);
                    }

                    _repository.Remove(productTrimmed);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return (WprInstallError.Canceled, null);
                }

                Directory.CreateDirectory(appFolder);

                int total = archive.Entries.Count(e => e.FullName.StartsWith("app/", StringComparison.OrdinalIgnoreCase));
                int extracted = 0;

                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.StartsWith("app/", StringComparison.OrdinalIgnoreCase))
                        continue;

                    cancellationToken.ThrowIfCancellationRequested();

                    var relativePath = entry.FullName.Substring("app/".Length)
                        .Replace('/', Path.DirectorySeparatorChar);
                    var destinationPath = Path.Combine(appFolder, relativePath);

                    if (string.IsNullOrEmpty(relativePath))
                        continue;

                    if (destinationPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }

                    extracted++;
                    if (total > 0)
                    {
                        var p = 5 + (int)((double)extracted / total * 90.0);
                        progress?.Report(p);
                    }
                }

                var app = new WprApplication
                {
                    Name = manifest.Name,
                    Description = manifest.Description ?? string.Empty,
                    IconPath = string.IsNullOrEmpty(manifest.IconPath)
                        ? string.Empty
                        : Path.Combine(productTrimmed, manifest.IconPath.Replace('/', Path.DirectorySeparatorChar)),
                    ApplicationType = manifest.ApplicationType,
                    ProductId = productTrimmed,
                    Author = string.IsNullOrEmpty(manifest.Author) ? "Unknown" : manifest.Author,
                    Publisher = string.IsNullOrEmpty(manifest.Publisher) ? "Unknown" : manifest.Publisher,
                    Assembly = manifest.EntryPointAssembly,
                    EntryPoint = manifest.EntryPointType,
                    Version = manifest.Version,
                    PatchedVersion = manifest.PatchedVersion,
                    InstalledTime = DateTime.Now
                };

                _repository.AddOrReplace(app);

                progress?.Report(100);
                return (WprInstallError.None, app);
            }
            catch (OperationCanceledException)
            {
                return (WprInstallError.Canceled, null);
            }
            catch
            {
                return (WprInstallError.UnexpectedError, null);
            }
        }
    }
}
