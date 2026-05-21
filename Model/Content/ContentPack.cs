using System;

namespace ToastFish.Model.Content
{
    public class ContentPack
    {
        public string packId { get; set; }
        public string version { get; set; }
        public string jlptLevel { get; set; }
        public string contentKind { get; set; }
        public string displayName { get; set; }
        public string description { get; set; }
        public string sourceId { get; set; }
        public string licenseName { get; set; }
        public string licenseUrl { get; set; }
        public string contentHash { get; set; }
        public DateTime installedAt { get; set; }
    }
}
