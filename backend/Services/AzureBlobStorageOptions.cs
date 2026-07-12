namespace backend.Services
{
    public class AzureBlobStorageOptions
    {
        public const string SectionName = "AzureBlobStorage";

        public string AccountName { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string? BlobServiceUri { get; set; }
        public string? PublicBlobServiceUri { get; set; }
        public int UploadSasExpirationMinutes { get; set; }
        public long MaxUploadSizeInBytes { get; set; }
        public string[] CorsAllowedOrigins { get; set; } = [];
    }
}
