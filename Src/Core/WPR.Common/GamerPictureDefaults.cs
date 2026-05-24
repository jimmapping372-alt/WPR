using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WPR.Common
{
    /// <summary>
    /// Bundled stock gamer-picture avatars, embedded as resources under
    /// <c>WPR.Common.GamerPictures.&lt;id&gt;.png</c>. The host's settings UI exposes them
    /// as default choices; <see cref="Configuration.GamerPicturePath"/> stores
    /// <c>"default:&lt;id&gt;"</c> when a default is picked (vs. a raw absolute path for
    /// custom Browse picks). <c>GamerProfile.GetGamerPicture</c> consumes the same scheme
    /// to resolve the active avatar at game-launch time.
    /// </summary>
    public static class GamerPictureDefaults
    {
        public const string ConfigPrefix = "default:";
        private const string ResourcePrefix = "WPR.Common.GamerPictures.";
        private const string ResourceSuffix = ".png";

        private static readonly Assembly _Assembly = typeof(GamerPictureDefaults).Assembly;

        private static readonly Lazy<IReadOnlyList<string>> _Ids = new Lazy<IReadOnlyList<string>>(() =>
            _Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                         && n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                .Select(n => n.Substring(ResourcePrefix.Length, n.Length - ResourcePrefix.Length - ResourceSuffix.Length))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray());

        /// <summary>
        /// IDs of all bundled defaults (sorted, ordinal). Stable across runs.
        /// </summary>
        public static IReadOnlyList<string> Ids => _Ids.Value;

        /// <summary>
        /// Open a fresh read stream for the default with the given ID, or null if no
        /// such default is bundled. Caller owns the stream.
        /// </summary>
        public static Stream? Open(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _Assembly.GetManifestResourceStream(ResourcePrefix + id + ResourceSuffix);
        }

        /// <summary>
        /// True if the configuration value points to a bundled default
        /// (uses the <c>default:&lt;id&gt;</c> scheme).
        /// </summary>
        public static bool IsDefault(string? configValue) =>
            configValue != null && configValue.StartsWith(ConfigPrefix, StringComparison.Ordinal);

        /// <summary>
        /// Extract the ID portion of a <c>default:&lt;id&gt;</c> config value, or null
        /// if the value is not in that scheme.
        /// </summary>
        public static string? ExtractId(string? configValue) =>
            IsDefault(configValue) ? configValue!.Substring(ConfigPrefix.Length) : null;

        /// <summary>
        /// Build the config string for a given default ID.
        /// </summary>
        public static string ToConfigValue(string id) => ConfigPrefix + id;
    }
}
