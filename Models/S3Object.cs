namespace AwsHelper.Models
{
    public class S3Object
    {
        public string Key { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public string StorageClass { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;
        
        public string SizeFormatted => FormatBytes(Size);
        public string FileName => Path.GetFileName(Key);
        public string Directory => Path.GetDirectoryName(Key) ?? string.Empty;
        
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
    
    public class S3Bucket
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
    }
    
    public class S3Folder
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        
        public string SizeFormatted => FormatBytes(TotalSize);
        
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
    
    public class S3BrowserItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public S3Object? File { get; set; }
        public S3Folder? Folder { get; set; }
    }
}
