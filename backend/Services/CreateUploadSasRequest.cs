namespace backend.Services
{
    public class CreateUploadSasRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long SizeInBytes { get; set; }
        public string? VirtualPath { get; set; }
    }
}
