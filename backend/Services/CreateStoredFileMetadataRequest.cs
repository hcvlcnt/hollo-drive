namespace backend.Services
{
    public class CreateStoredFileMetadataRequest
    {
        public Guid UserId { get; set; }
        public Guid? FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public string? Extension { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeInBytes { get; set; }
        public string ContainerName { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string? VirtualPath { get; set; }
        public string? ETag { get; set; }
        public string? ChecksumSha256 { get; set; }
    }
}
