using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WPR;
using WPR.Models;

namespace XapToWPR;

public sealed class XapToWprConverter
{
    private sealed class WprManifest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public ApplicationType ApplicationType { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string EntryPointAssembly { get; set; } = string.Empty;
        public string EntryPointType { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int PatchedVersion { get; set; }
        public DateTime ConvertedTime { get; set; }
        public string ConverterVersion { get; set; } = "1.0";
    }

    public async Task ConvertAsync(
        string xapPath,
        string wprPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(xapPath))
        {
            throw new ArgumentException("Input XAP path is empty", nameof(xapPath));
        }

        if (string.IsNullOrWhiteSpace(wprPath))
        {
            throw new ArgumentException("Output WPR path is empty", nameof(wprPath));
        }

        xapPath = Path.GetFullPath(xapPath);
        wprPath = Path.GetFullPath(wprPath);

        if (!File.Exists(xapPath))
        {
            throw new FileNotFoundException("Input XAP file not found", xapPath);
        }

        string tempRoot = Path.GetDirectoryName(wprPath) ?? Path.GetTempPath();
        string tempFolder = Path.Combine(tempRoot, "XapToWPR_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempFolder);

        try
        {
            Debug.WriteLine($"[XapToWPR] Starting conversion. XAP: {xapPath}, WPR: {wprPath}");
            progress?.Report(1);

            try
            {
                using FileStream fs = File.OpenRead(xapPath);
                using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

            // --- Разбор WMAppManifest.xml ---
            ZipArchiveEntry? wmManifestEntry = archive.GetEntry("WMAppManifest.xml");
            if (wmManifestEntry == null)
            {
                throw new InvalidDataException("WMAppManifest.xml not found in XAP");
            }

            XmlDocument wmManifestDoc = new XmlDocument();
            using (Stream wmStream = wmManifestEntry.Open())
            {
                wmManifestDoc.Load(wmStream);
            }

            XmlElement? root = wmManifestDoc.DocumentElement;
            XmlNodeList? appNodeList = root?.SelectNodes("//App");
            if (appNodeList == null || appNodeList.Count == 0)
            {
                throw new InvalidDataException("Invalid WMAppManifest.xml: <App> node not found");
            }

            XmlNode appNode = appNodeList[0]!;

            XmlAttribute? titleAttrib = appNode.Attributes?["Title"];
            XmlAttribute? runtimeTypeAttrb = appNode.Attributes?["RuntimeType"];
            XmlAttribute? versionAttrib = appNode.Attributes?["Version"];
            XmlAttribute? productAttrib = appNode.Attributes?["ProductID"];

            if (titleAttrib == null || runtimeTypeAttrb == null || versionAttrib == null || productAttrib == null)
            {
                throw new InvalidDataException("Invalid WMAppManifest.xml: required attributes missing");
            }

            string productTrimmed = productAttrib.Value.Trim('{', '}');

            if (!Enum.TryParse(runtimeTypeAttrb.Value, true, out ApplicationType runtimeTypeParsed))
            {
                throw new NotSupportedException($"Unsupported application type: {runtimeTypeAttrb.Value}");
            }

            XmlAttribute? authorAttrib = appNode.Attributes?["Author"];
            XmlAttribute? publisherAttrib = appNode.Attributes?["Publisher"];
            XmlAttribute? descriptionAttrib = appNode.Attributes?["Description"];

            string? iconPath = null;
            XmlNodeList? iconPathNodes = appNode.SelectNodes("//IconPath");
            if (iconPathNodes != null && iconPathNodes.Count > 0)
            {
                iconPath = iconPathNodes[0]!.InnerText;
            }

            // --- Разбор AppManifest.xaml ---
            ZipArchiveEntry? appManifestEntry = archive.GetEntry("AppManifest.xaml");
            if (appManifestEntry == null)
            {
                throw new InvalidDataException("AppManifest.xaml not found in XAP");
            }

                XmlDocument appManifestDoc = new XmlDocument();
                using (Stream appStream = appManifestEntry.Open())
                {
                    appManifestDoc.Load(appStream);
                }

                XmlNode? deploymentNode = appManifestDoc.DocumentElement;
                XmlAttribute? entryPointAsmAttrib = deploymentNode?.Attributes?["EntryPointAssembly"];
                XmlAttribute? entryPointTypeAttrib = deploymentNode?.Attributes?["EntryPointType"];

                if (entryPointAsmAttrib == null || entryPointTypeAttrib == null)
                {
                    throw new InvalidDataException("Invalid AppManifest.xaml: EntryPointAssembly/EntryPointType missing");
                }

                var nsmgr = new XmlNamespaceManager(appManifestDoc.NameTable);
                nsmgr.AddNamespace("a", "http://schemas.microsoft.com/client/2007/deployment");

                XmlNodeList? assemblies = deploymentNode?.SelectNodes("//a:Deployment.Parts//a:AssemblyPart", nsmgr);
                if (assemblies == null)
                {
                    throw new InvalidDataException("Invalid AppManifest.xaml: assemblies list missing");
                }

                XmlAttribute? entryPointAsmFileNameAttrib = null;

                foreach (XmlNode assembly in assemblies)
                {
                    XmlAttribute? attrib = assembly.Attributes?["x:Name"] ?? assembly.Attributes?["Name"];
                    if (attrib == null || attrib.Value != entryPointAsmAttrib.Value)
                    {
                        continue;
                    }

                    entryPointAsmFileNameAttrib = assembly.Attributes?["Source"];
                }

                if (entryPointAsmFileNameAttrib == null)
                {
                    throw new InvalidDataException("Invalid AppManifest.xaml: entry point assembly source not found");
                }

                // Имя entry DLL после патчинга может быть изменено (см. ApplicationPatcher/AssemblyNameStandardization),
                // поэтому сохраняем в манифест уже стандартизованное имя файла.
                string entryPointAssemblyFile = entryPointAsmFileNameAttrib.Value;
                string standardizedEntryAssemblyFile = StandardizeAssemblyFileName(entryPointAssemblyFile);

                progress?.Report(5);

            // --- Распаковка XAP во временную директорию ---
                int count = 0;
                foreach (ZipArchiveEntry entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string destinationPath = Path.Combine(tempFolder, entry.FullName);

                if (destinationPath.EndsWith(Path.DirectorySeparatorChar) || destinationPath.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                count++;
                int percent = 5 + (int)((double)count / archive.Entries.Count * 35.0);
                progress?.Report(percent);
            }

            // --- Патчинг DLL через ApplicationPatcher ---
            progress?.Report(40);

            Debug.WriteLine($"[XapToWPR] Extracted XAP to temp folder: {tempFolder}. Starting patching...");

            var patcher = new ApplicationPatcher();
            await Task.Run(() =>
            {
                patcher.Patch(tempFolder, p =>
                {
                    int mapped = 40 + (int)(p * 0.4); // 40-80%
                    progress?.Report(mapped);
                }, cancellationToken);
            }, cancellationToken);

            // --- Формирование manifest.json ---
            var manifest = new WprManifest
            {
                Name = titleAttrib.Value,
                Description = descriptionAttrib?.Value ?? string.Empty,
                IconPath = iconPath ?? string.Empty,
                ApplicationType = runtimeTypeParsed,
                ProductId = productTrimmed,
                Author = authorAttrib?.Value ?? "Unknown",
                Publisher = publisherAttrib?.Value ?? "Unknown",
                EntryPointAssembly = standardizedEntryAssemblyFile,
                EntryPointType = entryPointTypeAttrib.Value,
                Version = versionAttrib.Value,
                PatchedVersion = ApplicationPatcher.Version,
                ConvertedTime = DateTime.Now,
                ConverterVersion = "1.0"
            };

            Debug.WriteLine("[XapToWPR] Manifest prepared: " +
                $"Name='{manifest.Name}', ProductId='{manifest.ProductId}', " +
                $"Version='{manifest.Version}', Type='{manifest.ApplicationType}'");

            progress?.Report(85);

            // --- Создание .wpr (zip-архив) ---
            if (File.Exists(wprPath))
            {
                File.Delete(wprPath);
            }

            Debug.WriteLine("[XapToWPR] Creating WPR archive...");

            using (FileStream wprStream = File.Create(wprPath))
            using (ZipArchive wprArchive = new ZipArchive(wprStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                // manifest.json в корне
                ZipArchiveEntry manifestEntry = wprArchive.CreateEntry("manifest.json");
                await using (Stream manifestStream = manifestEntry.Open())
                {
                    await JsonSerializer.SerializeAsync(manifestStream, manifest, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }, cancellationToken);
                }

                // содержимое приложения под папкой app/
                string[] files = Directory.GetFiles(tempFolder, "*", SearchOption.AllDirectories);
                int current = 0;

                foreach (string file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string relativePath = Path.GetRelativePath(tempFolder, file)
                        .Replace('\\', '/');

                    string entryName = "app/" + relativePath;
                    ZipArchiveEntry fileEntry = wprArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                    await using Stream src = File.OpenRead(file);
                    await using Stream dst = fileEntry.Open();
                    await src.CopyToAsync(dst, cancellationToken);

                    current++;
                    int percent = 85 + (int)((double)current / files.Length * 15.0); // 85-100%
                    progress?.Report(percent);
                }
            }

            progress?.Report(100);
            Debug.WriteLine("[XapToWPR] Conversion completed successfully.");
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException("Input XAP is not a valid ZIP archive or is corrupted.", ex);
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
            }
            catch
            {
                // ignore cleanup errors
                Debug.WriteLine("[XapToWPR] Failed to cleanup temporary folder: " + tempFolder);
            }
        }
    }

    private static string StandardizeAssemblyFileName(string original)
    {
        string fileName = Path.GetFileNameWithoutExtension(original);
        string extension = Path.GetExtension(original);

        string sanitizedName = Regex.Replace(fileName, "[*'\",_&#^@!]", "_");
        return sanitizedName + extension;
    }
}
