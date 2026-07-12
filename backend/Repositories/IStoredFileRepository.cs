using backend.Models;

namespace backend.Repositories
{
    public interface IStoredFileRepository
    {
        Task<StoredFile?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<StoredFile?> GetByIdForUserAsync(Guid id, Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<StoredFile?> GetByBlobAsync(string containerName, string blobName, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFile>> SearchAsync(
            string? searchTerm = null,
            string? virtualPath = null,
            Guid? folderId = null,
            Guid? userId = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFile>> ListDeletedRootsAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFile>> ListStarredAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<FileCategoryStats> GetCategoryStatsAsync(
            Guid? userId = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);
        Task<long> SumSizeInBytesAsync(
            Guid? userId = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<Guid, long>> SumSizeInBytesByUserAsync(
            IEnumerable<Guid> userIds,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);
        Task<StoredFile> AddAsync(StoredFile storedFile, CancellationToken cancellationToken = default);
        Task<StoredFile> UpdateAsync(StoredFile storedFile, CancellationToken cancellationToken = default);
    }

    public class FileCategoryStats
    {
        public FileCategoryStat Images { get; set; } = new();
        public FileCategoryStat Videos { get; set; } = new();
        public FileCategoryStat Documents { get; set; } = new();
        public FileCategoryStat Audio { get; set; } = new();
        public FileCategoryStat Others { get; set; } = new();
    }

    public class FileCategoryStat
    {
        public long Count { get; set; }
        public long SizeInBytes { get; set; }
    }
}
