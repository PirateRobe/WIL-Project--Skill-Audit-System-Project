// Models/StorageDocument.cs
using System;

namespace WebApplication2.Models
{
    public class StorageDocument
    {
        public string Id { get; set; }
        public string Type { get; set; } // "Training Certificate", "Qualification Certificate", "Transcript"
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
        public string StoragePath { get; set; }
        public DateTime UploadDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long FileSize { get; set; }
        public string FileExtension => Path.GetExtension(FileName)?.ToLower() ?? "";

        public bool IsPdf => FileExtension == ".pdf";
        public bool IsImage => FileExtension == ".jpg" || FileExtension == ".jpeg" || FileExtension == ".png";

        public string DisplaySize
        {
            get
            {
                if (FileSize == 0) return "Unknown";
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:0.0} KB";
                return $"{FileSize / (1024.0 * 1024.0):0.0} MB";
            }
        }
    }
}