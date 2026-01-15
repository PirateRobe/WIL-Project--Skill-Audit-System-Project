namespace WebApplication2.Models
{
    public class StorageFileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime Updated { get; set; }
        public string ContentType { get; set; }
        public string DownloadUrl { get; set; }
        public string Url { get; set; }
        public string DisplaySize
        {
            get
            {
                if (Size < 1024) return $"{Size} B";
                if (Size < 1024 * 1024) return $"{Size / 1024.0:0.0} KB";
                return $"{Size / (1024.0 * 1024.0):0.0} MB";
            }
        }

        public string FileType
        {
            get
            {
                var extension = System.IO.Path.GetExtension(Name)?.ToLower();
                return extension switch
                {
                    ".pdf" => "PDF Document",
                    ".jpg" or ".jpeg" => "JPEG Image",
                    ".png" => "PNG Image",
                    ".doc" or ".docx" => "Word Document",
                    _ => "File"
                };
            }
        }
    }
}
