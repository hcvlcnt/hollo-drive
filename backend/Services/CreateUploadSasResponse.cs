namespace backend.Services
{
    public class CreateUploadSasResponse
    {
        public string UploadUrl { get; set; } = string.Empty;
        public string Method { get; set; } = "PUT";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string ContainerName { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
