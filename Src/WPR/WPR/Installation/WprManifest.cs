using System;
using WPR.Models;

namespace WPR.Installation
{
    internal class WprManifest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public ApplicationType ApplicationType { get; set; } = ApplicationType.Unknown;
        public string ProductId { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string EntryPointAssembly { get; set; } = string.Empty;
        public string EntryPointType { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int PatchedVersion { get; set; }
        public DateTime ConvertedTime { get; set; }
        public string ConverterVersion { get; set; } = string.Empty;
    }
}
