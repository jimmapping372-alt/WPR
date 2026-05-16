using System.IO;
using System.Resources;
using System.Text;
using Xunit;

namespace WPR.SilverlightCompability.Tests
{
    public class GResourcesLookupTests
    {
        private static MemoryStream MakeBundle(params (string key, byte[] value)[] entries)
        {
            var ms = new MemoryStream();
            var writer = new ResourceWriter(ms);
            foreach (var e in entries)
                writer.AddResource(e.key, e.value);
            writer.Generate();
            byte[] bytes = ms.ToArray();
            writer.Dispose(); // disposes ms too, but we've already snapshotted the bytes
            return new MemoryStream(bytes, writable: false);
        }

        [Fact]
        public void FindEntry_ExactKey_ReturnsValueStream()
        {
            byte[] payload = Encoding.UTF8.GetBytes("<Page />");
            using var bundle = MakeBundle(("mainpage.xaml", payload));

            using Stream? result = ResourceBundleReader.FindEntry(bundle, "mainpage.xaml");

            Assert.NotNull(result);
            using var reader = new StreamReader(result!);
            Assert.Equal("<Page />", reader.ReadToEnd());
        }

        [Fact]
        public void FindEntry_CaseInsensitive()
        {
            byte[] payload = Encoding.UTF8.GetBytes("<x />");
            using var bundle = MakeBundle(("MainPage.xaml", payload));

            using Stream? result = ResourceBundleReader.FindEntry(bundle, "mainpage.xaml");
            Assert.NotNull(result);
        }

        [Fact]
        public void FindEntry_KeyMismatch_ReturnsNull()
        {
            byte[] payload = Encoding.UTF8.GetBytes("<x />");
            using var bundle = MakeBundle(("app.xaml", payload));

            using Stream? result = ResourceBundleReader.FindEntry(bundle, "mainpage.xaml");
            Assert.Null(result);
        }

        [Fact]
        public void FindEntry_PathSegmentSuffix()
        {
            byte[] payload = Encoding.UTF8.GetBytes("<x />");
            // Real WP resources sometimes include a folder prefix.
            using var bundle = MakeBundle(("views/mainpage.xaml", payload));

            using Stream? result = ResourceBundleReader.FindEntry(bundle, "mainpage.xaml");
            Assert.NotNull(result);
        }
    }
}
