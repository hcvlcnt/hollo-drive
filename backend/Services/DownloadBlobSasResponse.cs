namespace backend.Services
{
    public class DownloadBlobSasResponse
    {
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
