using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using WPR.Common;

namespace WPR
{
    /// <summary>
    /// One curated achievement from a game's hardcoded catalogue. <see cref="IconRelativePath"/>
    /// is relative to <see cref="Configuration.DataStorePath"/> (empty when no icon ships), so it
    /// round-trips through <see cref="Configuration.DataPath"/> exactly like the rest of the DB's
    /// <c>_IconPath</c> values.
    /// </summary>
    public sealed record HardcodedAchievement(
        string Key,
        string Name,
        string Description,
        int GamerScore,
        string IconRelativePath);

    /// <summary>
    /// Reads the committed, hardcoded achievement catalogue for a product — the authoritative
    /// source for curated games (no network, no IL scan). Each game ships under
    /// <c>Database/Achievements/&lt;productId&gt;/</c> as an <c>achievements.json</c> manifest plus
    /// one PNG per achievement; that tree is deployed to the runtime data dir at startup, so the
    /// paths here resolve against <see cref="Configuration.DataStorePath"/>.
    /// </summary>
    public static class HardcodedAchievementCatalogue
    {
        public const string CatalogueRoot = "Database/Achievements";
        private const string ManifestName = "achievements.json";

        private static readonly JsonSerializerOptions JsonOptions =
            new() { PropertyNameCaseInsensitive = true };

        private static string ProductDir(string productId) =>
            Configuration.Current!.DataPath(Path.Combine(CatalogueRoot, productId));

        public static bool HasCatalogue(string productId) =>
            !string.IsNullOrEmpty(productId)
            && File.Exists(Path.Combine(ProductDir(productId), ManifestName));

        /// <summary>Product IDs (folder names) that ship a catalogue manifest.</summary>
        public static IReadOnlyList<string> ProductIds()
        {
            string root = Configuration.Current!.DataPath(CatalogueRoot);
            if (!Directory.Exists(root)) return Array.Empty<string>();

            var ids = new List<string>();
            foreach (string dir in Directory.GetDirectories(root))
            {
                if (File.Exists(Path.Combine(dir, ManifestName)))
                    ids.Add(Path.GetFileName(dir));
            }
            return ids;
        }

        public static List<HardcodedAchievement> Load(string productId)
        {
            var result = new List<HardcodedAchievement>();
            if (string.IsNullOrEmpty(productId)) return result;

            string dir = ProductDir(productId);
            string manifest = Path.Combine(dir, ManifestName);
            if (!File.Exists(manifest)) return result;

            ManifestDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<ManifestDto>(File.ReadAllText(manifest), JsonOptions);
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppInstall,
                    $"HardcodedAchievementCatalogue: failed to parse {manifest}: {ex.Message}");
                return result;
            }

            if (dto?.Achievements == null) return result;

            foreach (EntryDto a in dto.Achievements)
            {
                if (string.IsNullOrEmpty(a.Key)) continue;

                string icon = string.Empty;
                if (!string.IsNullOrEmpty(a.Icon) && File.Exists(Path.Combine(dir, a.Icon)))
                {
                    icon = $"{CatalogueRoot}/{productId}/{a.Icon}";
                }

                result.Add(new HardcodedAchievement(
                    Key: a.Key,
                    Name: string.IsNullOrEmpty(a.Name) ? a.Key : a.Name,
                    Description: a.Description ?? string.Empty,
                    GamerScore: a.GamerScore,
                    IconRelativePath: icon));
            }

            return result;
        }

        private sealed class ManifestDto
        {
            public string? Name { get; set; }
            public List<EntryDto>? Achievements { get; set; }
        }

        private sealed class EntryDto
        {
            public string Key { get; set; } = "";
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int GamerScore { get; set; }
            public string? Icon { get; set; }
        }
    }
}
