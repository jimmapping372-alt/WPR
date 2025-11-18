using System;

namespace WPR.Models
{
    public class WprApplication
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public ApplicationType ApplicationType { get; set; } = ApplicationType.Unknown;
        public string ProductId { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string Assembly { get; set; } = string.Empty; // entry DLL
        public string EntryPoint { get; set; } = string.Empty; // full type name
        public string Version { get; set; } = string.Empty;
        public int PatchedVersion { get; set; }
        public DateTime InstalledTime { get; set; }
    }
}
