namespace backend.Services
{
    public interface IStorageUsageService
    {
        Task<StorageUsageResponse> GetUsageAsync(
            Guid currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default);

        Task EnsureUserCanStoreAsync(
            Guid currentUserId,
            long additionalSizeInBytes,
            CancellationToken cancellationToken = default);
    }
}
