using System;
using System.IO;

namespace WPR.XnaCompability.Media
{
    /// <summary>
    /// Shim for WP7's <c>Microsoft.Xna.Framework.Media.Picture</c>. WP7's MediaLibrary
    /// exposed the user's photo gallery via this type; desktop XNA / FNA never had it.
    /// We provide an empty-but-typed stub so games that <i>reference</i> the type at
    /// load time don't fail (e.g. ZombiesWP7's <c>CustomMgr.Init</c> hits this purely
    /// because the type appears in a method signature it loads).
    ///
    /// Behaviour: every property returns a sensible default; <c>GetImage</c> and
    /// <c>GetThumbnail</c> return empty streams. Game code that actually iterates the
    /// gallery will see an empty collection (via <see cref="MediaLibrary.Pictures"/>).
    /// That matches a phone with zero photos, which is the safest fake for desktop.
    /// </summary>
    public class Picture
    {
        internal Picture(string name = "Untitled", int width = 0, int height = 0)
        {
            Name = name;
            Width = width;
            Height = height;
            Date = DateTime.MinValue;
        }

        public string Name { get; }
        public DateTime Date { get; }
        public int Width { get; }
        public int Height { get; }
        public string Album => string.Empty;

        /// <summary>Returns an empty stream — desktop has no phone photo gallery.</summary>
        public Stream GetImage() => new MemoryStream(Array.Empty<byte>(), writable: false);

        /// <summary>Returns an empty stream — desktop has no phone photo gallery.</summary>
        public Stream GetThumbnail() => new MemoryStream(Array.Empty<byte>(), writable: false);
    }
}
