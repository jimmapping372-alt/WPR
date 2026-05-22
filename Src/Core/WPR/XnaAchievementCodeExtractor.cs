using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using Mono.Cecil;
using Mono.Cecil.Cil;

using WPR.Common;

namespace WPR
{
    /// <summary>
    /// One achievement extracted from a game's install, with the metadata
    /// the seeder needs to populate the SQLite catalogue.
    /// </summary>
    public sealed record XnaAchievementEntry(
        string Key,
        string Name,
        string Description,
        string? IconFilenameStem);

    /// <summary>
    /// Install-time read-only scan that pulls achievements out of the game's
    /// install folder. Four sources, tried in priority order; the FIRST one
    /// that yields results wins (the lower-priority sources are skipped):
    ///
    /// <para><b>Source D (override) — known-product hardcoded catalogue.</b>
    /// Some titles store their achievement keys in an inline static array
    /// rather than the XNA content pipeline catalogue, with no
    /// <c>AwardAchievement(literal)</c> callsite and no <c>Content/Achievements</c>
    /// folder — sources A/B/C all return empty for them and the game NREs at
    /// runtime when it iterates an empty list. For known-bad ProductIDs the
    /// keys recovered via decompilation are kept in
    /// <see cref="KnownProductCatalogues"/> and short-circuit the rest.</para>
    ///
    /// <para><b>Source A — XNA XML content catalogue.</b> Games shipped with
    /// the XNA content pipeline routinely compile their achievement metadata
    /// into <c>Content/xml/socialnetworks.xml.xnb</c> (player-visible name,
    /// description, secret flag) and <c>Content/xml/achievementlist.xml.xnb</c>
    /// (runtime mapping: social-id → texture / score / mode). The "XNB" wrapper
    /// for XML content is just an XNA header followed by the raw XML, so we
    /// scan past the header for <c>&lt;?xml</c> and parse the rest. This
    /// gives us the canonical achievement list with descriptions and
    /// icon filenames, no IL guessing.</para>
    ///
    /// <para>When the XML source matches, results are filtered to the
    /// <c>"HD"</c> product token (as opposed to Lite versions). Non-HD-only
    /// achievements stay out of the user's library.</para>
    ///
    /// <para><b>Source B (fallback) — IL literal at <c>AwardAchievement*</c>
    /// callsite.</b> For every <c>call</c> / <c>callvirt</c> targeting a
    /// method whose name contains "AwardAchievement" and takes a
    /// <c>System.String</c> parameter, walk back for the nearest <c>ldstr</c>.
    /// Catches direct unlock calls in games that don't ship the XML
    /// catalogue. Description/icon are empty — the IL has only the key.</para>
    ///
    /// <para><b>Source C (fallback) — <c>Content/Achievements/*.xnb</c>
    /// filenames.</b> XNA's content pipeline stores per-achievement icons
    /// under that path; the filename stem IS the achievement key for most
    /// titles. Filters out non-achievement assets (<c>item_*</c>,
    /// <c>lock*</c>, <c>Background*</c>).</para>
    ///
    /// Memory cost: at most ~40 KB of XML decoded per file at install time.
    /// Trivial.
    /// </summary>
    public static class XnaAchievementCodeExtractor
    {
        private const int LookbackWindow = 16;
        private const string AwardAchievementSubstring = "AwardAchievement";
        private const string SystemStringFullName = "System.String";

        private static readonly string[] AssetPrefixDenyList = new[]
        {
            "item_",
            "lock",
            "Background",
            "background",
        };

        /// <summary>
        /// Achievement product flavour we keep. The catalogue groups entries
        /// under <c>&lt;specificAchievements product="SD,HD,freeSD,freeHD"&gt;</c>
        /// blocks; we accept any block whose comma-separated product list
        /// contains "HD" as an exact token AND no token containing "Lite".
        /// That gives the full retail-HD set without the cut-down Lite list.
        /// </summary>
        private const string KeepProductToken = "HD";

        /// <summary>
        /// ProductIDs whose achievement keys can't be recovered by sources A/B/C
        /// because the game stores them in an inline static array (e.g. PvZ's
        /// <c>Sexy.Achievements.ACHIEVEMENT_KEYS</c>) and never reaches an
        /// <c>AwardAchievement(literal)</c> callsite during static scanning.
        /// Keys are the verbatim strings the game compares against at runtime —
        /// they're also the display names, since this title family doesn't
        /// distinguish ids from names.
        /// </summary>
        private static readonly Dictionary<string, string[]> KnownProductCatalogues =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Plants vs. Zombies — keys recovered from Sexy.Achievements.ACHIEVEMENT_KEYS
                // in LAWN.dll (the 18-entry static array constructed in the cctor).
                ["706f822a-a47e-e011-986b-78e7d1fa76f8"] = new[]
                {
                    "Home Lawn Security",
                    "Master of Mosticulture",
                    "Better Off Dead",
                    "China Shop",
                    "Beyond the Grave",
                    "Crash of the Titan",
                    "Soil Your Plants",
                    "Explodonator",
                    "Close Shave",
                    "Shopaholic",
                    "Nom Nom Nom",
                    "No Fungus Among Us",
                    "Dont Pea in the Pool",
                    "Grounded",
                    "Good Morning",
                    "Popcorn Party",
                    "Roll Some Heads",
                    "Disco is Undead",
                },
            };

        public static List<XnaAchievementEntry> ExtractRich(string installFolder, string? productId = null)
        {
            // Source D: known-product override. Authoritative for titles whose
            // keys aren't visible to the other sources.
            var fromKnown = TryExtractFromKnownProduct(productId);
            if (fromKnown.Count > 0) return fromKnown;

            // Source A: XML catalogue. Returns full rich entries.
            var fromXml = TryExtractFromXmlCatalogue(installFolder);
            if (fromXml.Count > 0) return fromXml;

            // Source B+C: union of IL literals and Content/Achievements files.
            var keys = new HashSet<string>(StringComparer.Ordinal);
            ExtractFromIl(installFolder, keys);
            ExtractFromContentAchievementsFolder(installFolder, keys);

            return keys.Select(k => new XnaAchievementEntry(
                Key: k, Name: k, Description: string.Empty, IconFilenameStem: null)
            ).ToList();
        }

        /// <summary>
        /// Convenience: just the key set. Kept for callers that don't need
        /// the metadata.
        /// </summary>
        public static HashSet<string> Extract(string installFolder, string? productId = null)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in ExtractRich(installFolder, productId))
            {
                keys.Add(entry.Key);
            }
            return keys;
        }

        // ------------------------------------------------------------------
        // Source D: known-product hardcoded catalogue.
        // ------------------------------------------------------------------

        private static List<XnaAchievementEntry> TryExtractFromKnownProduct(string? productId)
        {
            if (string.IsNullOrEmpty(productId)) return new List<XnaAchievementEntry>();
            if (!KnownProductCatalogues.TryGetValue(productId, out var keys))
                return new List<XnaAchievementEntry>();

            Log.Info(LogCategory.AppInstall,
                $"XnaAchievementCodeExtractor: {keys.Length} hardcoded achievement(s) " +
                $"applied from known-product catalogue for {productId}.");

            return keys.Select(k => new XnaAchievementEntry(
                Key: k, Name: k, Description: string.Empty, IconFilenameStem: null)
            ).ToList();
        }

        // ------------------------------------------------------------------
        // Source A: XNA XML content catalogue.
        // ------------------------------------------------------------------

        private static List<XnaAchievementEntry> TryExtractFromXmlCatalogue(string installFolder)
        {
            var result = new List<XnaAchievementEntry>();

            // The xml/ folder is the HD/desktop assets; xmlwp7/ is the
            // phone-specific cut. User explicitly wants HD only.
            string socialPath = Path.Combine(installFolder, "Content", "xml", "socialnetworks.xml.xnb");
            string listPath = Path.Combine(installFolder, "Content", "xml", "achievementlist.xml.xnb");
            if (!File.Exists(socialPath)) return result;

            // achievementlist.xml.xnb is optional — when missing, achievements
            // get seeded without icon paths but display name / description /
            // unlock all still work.
            Dictionary<string, string> socialIdToTexture = new(StringComparer.Ordinal);
            if (File.Exists(listPath))
            {
                socialIdToTexture = ParseAchievementListTextures(listPath);
            }

            string? socialXml = ReadXmlFromXnb(socialPath);
            if (string.IsNullOrEmpty(socialXml)) return result;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(socialXml);

                foreach (XmlNode block in doc.GetElementsByTagName("specificAchievements"))
                {
                    string product = block.Attributes?["product"]?.Value ?? "";
                    if (!ProductMatchesHd(product)) continue;

                    foreach (XmlNode ach in block.ChildNodes)
                    {
                        if (ach.NodeType != XmlNodeType.Element) continue;
                        if (ach.LocalName != "achievement") continue;

                        string id = ach.Attributes?["id"]?.Value ?? "";
                        if (string.IsNullOrEmpty(id)) continue;

                        string name = ach.Attributes?["name"]?.Value ?? id;
                        string desc = ach.Attributes?["description"]?.Value ?? string.Empty;
                        socialIdToTexture.TryGetValue(id, out string? texture);

                        result.Add(new XnaAchievementEntry(
                            Key: id,
                            Name: name,
                            Description: desc,
                            IconFilenameStem: string.IsNullOrEmpty(texture) ? null : texture));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppInstall,
                    $"XnaAchievementCodeExtractor: socialnetworks.xml parse failed: {ex.Message}");
                return new List<XnaAchievementEntry>();
            }

            // Dedupe by Key — the XML can list the same achievement under
            // multiple product blocks (e.g. shared SD+HD then HD-only iOS).
            result = result
                .GroupBy(e => e.Key, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToList();

            Log.Info(LogCategory.AppInstall,
                $"XnaAchievementCodeExtractor: {result.Count} achievement(s) from socialnetworks.xml.xnb " +
                $"(filtered to product token '{KeepProductToken}', non-Lite).");
            return result;
        }

        private static Dictionary<string, string> ParseAchievementListTextures(string listPath)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            string? xml = ReadXmlFromXnb(listPath);
            if (string.IsNullOrEmpty(xml)) return map;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                foreach (XmlNode ach in doc.GetElementsByTagName("achievement"))
                {
                    string socialId = ach.Attributes?["social_id"]?.Value ?? "";
                    string texture = ach.Attributes?["texture"]?.Value ?? "";
                    if (string.IsNullOrEmpty(socialId) || string.IsNullOrEmpty(texture)) continue;
                    map[socialId] = texture;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppInstall,
                    $"XnaAchievementCodeExtractor: achievementlist.xml parse failed: {ex.Message}");
            }
            return map;
        }

        /// <summary>
        /// Match policy: product list (comma-separated tokens) contains "HD"
        /// EXACTLY as a token AND no token contains "Lite" (excluding
        /// SDLite/HDLite groups).
        /// </summary>
        private static bool ProductMatchesHd(string product)
        {
            bool hasHd = false;
            foreach (string raw in product.Split(','))
            {
                string token = raw.Trim();
                if (token.IndexOf("Lite", StringComparison.OrdinalIgnoreCase) >= 0) return false;
                if (token.Equals(KeepProductToken, StringComparison.Ordinal)) hasHd = true;
            }
            return hasHd;
        }

        /// <summary>
        /// Strip the XNA Content (.xnb) header off an <c>*.xml.xnb</c> file
        /// and return the inner XML as UTF-8 text. The XNB header before the
        /// payload is variable-length (depends on type-reader list), but for
        /// String-reader XML content we can find <c>&lt;?xml</c> by simple
        /// byte scan. Trailing bytes after the XML's last <c>&gt;</c> can be
        /// padding — clipped off.
        /// </summary>
        private static string? ReadXmlFromXnb(string xnbPath)
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(xnbPath); }
            catch { return null; }

            int start = IndexOf(bytes, "<?xml"u8.ToArray());
            if (start < 0) return null;

            string text = Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
            int lastClose = text.LastIndexOf('>');
            if (lastClose < 0) return null;
            return text.Substring(0, lastClose + 1);
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { match = false; break; }
                }
                if (match) return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------
        // Source B: IL literal at AwardAchievement* callsite (fallback).
        // ------------------------------------------------------------------

        private static void ExtractFromIl(string installFolder, HashSet<string> keys)
        {
            string[] dlls;
            try { dlls = Directory.GetFiles(installFolder, "*.dll"); }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppInstall,
                    $"XnaAchievementCodeExtractor: enumerate failed: {ex.Message}");
                return;
            }

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(installFolder);
            var rp = new ReaderParameters { AssemblyResolver = resolver };

            foreach (var dllPath in dlls)
            {
                AssemblyDefinition? asm = null;
                try { asm = AssemblyDefinition.ReadAssembly(dllPath, rp); }
                catch { continue; }

                using (asm)
                {
                    foreach (var module in asm.Modules)
                    {
                        foreach (var type in module.GetTypes())
                        {
                            foreach (var method in type.Methods)
                            {
                                if (!method.HasBody) continue;
                                ScanMethodForLiteralAtCallsite(method, keys);
                            }
                        }
                    }
                }
            }
        }

        private static void ScanMethodForLiteralAtCallsite(MethodDefinition method, HashSet<string> keys)
        {
            var instructions = method.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                var ins = instructions[i];
                if (ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) continue;
                if (ins.Operand is not MethodReference target) continue;
                if (!IsAwardAchievementCandidate(target)) continue;

                for (int j = i - 1, walked = 0; j >= 0 && walked < LookbackWindow; j--, walked++)
                {
                    var prev = instructions[j];
                    if (prev.OpCode == OpCodes.Ldstr && prev.Operand is string s)
                    {
                        if (!string.IsNullOrEmpty(s)) keys.Add(s);
                        break;
                    }
                }
            }
        }

        private static bool IsAwardAchievementCandidate(MethodReference target)
        {
            if (target.Name == null) return false;
            if (target.Name.IndexOf(AwardAchievementSubstring, StringComparison.Ordinal) < 0)
                return false;
            foreach (var p in target.Parameters)
            {
                if (p.ParameterType?.FullName == SystemStringFullName) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Source C: Content/Achievements/*.xnb filenames (fallback).
        // ------------------------------------------------------------------

        private static void ExtractFromContentAchievementsFolder(string installFolder, HashSet<string> keys)
        {
            string[] candidates =
            {
                Path.Combine(installFolder, "Content", "Achievements"),
                Path.Combine(installFolder, "Content", "achievements"),
            };

            string? folder = candidates.FirstOrDefault(Directory.Exists);
            if (folder == null) return;

            string[] xnbs;
            try { xnbs = Directory.GetFiles(folder, "*.xnb"); }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppInstall,
                    $"XnaAchievementCodeExtractor: failed to enumerate {folder}: {ex.Message}");
                return;
            }

            foreach (var path in xnbs)
            {
                string stem = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(stem)) continue;
                if (StartsWithAny(stem, AssetPrefixDenyList)) continue;
                keys.Add(stem);
            }
        }

        private static bool StartsWithAny(string s, IReadOnlyList<string> prefixes)
        {
            for (int i = 0; i < prefixes.Count; i++)
            {
                if (s.StartsWith(prefixes[i], StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
