namespace backend.Services
{
    public interface ITrashCleanupService
    {
        Task<TrashCleanupResult> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default);
    }
}
