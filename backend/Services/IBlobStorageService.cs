namespace backend.Services
{
    public interface IBlobStorageService
    {
        Task<CreateUploadSasResponse> CreateUploadSasAsync(
            CreateUploadSasRequest request,
            CancellationToken cancellationToken = default);

        DownloadBlobSasResponse CreateDownloadSas(
            string blobName,
            string fileName,
            string? contentType = null);

        Task<BlobUploadResult> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            string? virtualPath = null,
            CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default);

        Task DeleteBlobIfExistsAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default);
    }

    public record BlobUploadResult(string ContainerName, string BlobName, string? ETag);
}
