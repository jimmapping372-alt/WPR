using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.GamerServices.TrueAchievements;

using WPR.Common;

namespace WPR
{
    /// <summary>
    /// Install-time pass that pre-populates the local achievements DB. Pulls
    /// rich entries from <see cref="XnaAchievementCodeExtractor.ExtractRich"/>
    /// (which prefers the XNA XML catalogue under <c>Content/xml/</c> when
    /// available, with IL literals / asset filenames as fallbacks), then
    /// optionally enriches with TrueAchievements scraper metadata for any
    /// entry whose description / icon is still empty.
    ///
    /// Every row is persisted with <c>IsEarned = false</c> so the WPR UI can
    /// show the full locked list immediately after install. At runtime,
    /// <see cref="SignedInGamer.BeginAwardAchievement"/> flips the row keyed
    /// by <c>OwnProductId + Key</c> when the game unlocks.
    /// </summary>
    public static class XnaAchievementSeeder
    {
        public static async Task SeedAsync(string productId, string appName, string installFolder)
        {
            if (string.IsNullOrEmpty(productId)) return;

            try
            {
                bool alreadySeeded = await AchievementContext.Current.Achievements!
                    .AnyAsync(a => a.OwnProductId == productId);
                if (alreadySeeded)
                {
                    Log.Info(LogCategory.AppInstall,
                        $"XnaAchievementSeeder: '{appName}' ({productId}) already has achievements rows; skipping.");
                    return;
                }

                List<XnaAchievementEntry> entries;
                try { entries = XnaAchievementCodeExtractor.ExtractRich(installFolder); }
                catch (Exception ex)
                {
                    Log.Warn(LogCategory.AppInstall,
                        $"XnaAchievementSeeder: extractor threw for '{appName}': {ex.Message}");
                    entries = new List<XnaAchievementEntry>();
                }

                Log.Info(LogCategory.AppInstall,
                    $"XnaAchievementSeeder: extracted {entries.Count} achievement(s) from " +
                    $"'{appName}' ({productId})'s install folder.");

                // Optional: TrueAchievements scrape, used only as a metadata
                // backfill for entries whose description / icon is still empty
                // (i.e. came from the IL or folder fallbacks rather than the
                // XML catalogue). When the XML catalogue source already gave
                // us descriptions, this is harmless redundancy.
                AchievementCollection scraped;
                try { scraped = await Scraper.QueryAchievements(productId); }
                catch (Exception ex)
                {
                    Log.Warn(LogCategory.AppInstall,
                        $"XnaAchievementSeeder: scraper threw for '{appName}': {ex.Message}");
                    scraped = new AchievementCollection();
                }

                var scrapedByKey = scraped
                    .Where(s => !string.IsNullOrEmpty(s.Key))
                    .GroupBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var rows = new List<Achievement>();
                foreach (var entry in entries)
                {
                    string name = entry.Name;
                    string desc = entry.Description;
                    string icon = entry.IconFilenameStem != null
                        ? $"Content/Achievements/{entry.IconFilenameStem}.png"
                        : string.Empty;
                    int score = 0;

                    // Backfill from scraper if the XML didn't give us those
                    // fields. Match by Key — scraper-side keys may be display
                    // names while ours are canonical ids, so this is best-
                    // effort and often a no-match.
                    if (scrapedByKey.TryGetValue(entry.Key, out var s))
                    {
                        if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(s.Name)) name = s.Name;
                        if (string.IsNullOrEmpty(desc) && !string.IsNullOrEmpty(s.Description)) desc = s.Description;
                        if (string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(s._IconPath)) icon = s._IconPath;
                        if (score == 0 && s.GamerScore > 0) score = s.GamerScore;
                    }

                    rows.Add(new Achievement
                    {
                        OwnProductId = productId,
                        Key = entry.Key,
                        Name = string.IsNullOrEmpty(name) ? entry.Key : name,
                        Description = desc ?? string.Empty,
                        HowToEarn = desc ?? string.Empty,
                        _IconPath = icon,
                        GamerScore = score,
                        DisplayBeforeEarned = true,
                        IsEarned = false,
                        EarnedOnline = false,
                        EarnedDateTime = DateTime.MinValue,
                    });
                }

                if (rows.Count == 0)
                {
                    Log.Info(LogCategory.AppInstall,
                        $"XnaAchievementSeeder: nothing to seed for '{appName}' ({productId}).");
                    return;
                }

                await AchievementContext.Current.Achievements!.AddRangeAsync(rows);
                await AchievementContext.Current.SaveChangesAsync();

                Log.Info(LogCategory.AppInstall,
                    $"XnaAchievementSeeder: seeded {rows.Count} achievement(s) for '{appName}' ({productId}).");
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppInstall,
                    $"XnaAchievementSeeder: unexpected failure for '{appName}' ({productId}): {ex}");
            }
        }
    }
}
