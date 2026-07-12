using backend.Models;

namespace backend.Services
{
    public interface IStoredFileService
    {
        Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<StoredFile?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);
        Task<StoredFile?> GetByBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFile>> SearchAsync(
            string? searchTerm = null,
            string? virtualPath = null,
            Guid? folderId = null,
            Guid? userId = null,
            bool isAdmin = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<StoredFile> CreateMetadataAsync(CreateStoredFileMetadataRequest request, CancellationToken cancellationToken = default);
        Task<StoredFile?> RenameAsync(Guid id, Guid userId, bool isAdmin, string name, CancellationToken cancellationToken = default);
        Task<StoredFile?> MoveToTrashAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<StoredFile?> RestoreAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFile>> ListTrashAsync(Guid? userId, bool isAdmin, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFile>> ListStarredAsync(Guid? userId, bool isAdmin, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
        Task<StoredFile?> SetStarredAsync(Guid id, Guid userId, bool isAdmin, bool isStarred, CancellationToken cancellationToken = default);
        Task<FileCategoryStatsResponse> GetCategoryStatsAsync(Guid? userId, bool isAdmin, CancellationToken cancellationToken = default);
    }

    public class FileCategoryStatsResponse
    {
        public FileCategoryStatResponse Images { get; set; } = new();
        public FileCategoryStatResponse Videos { get; set; } = new();
        public FileCategoryStatResponse Documents { get; set; } = new();
        public FileCategoryStatResponse Audio { get; set; } = new();
        public FileCategoryStatResponse Others { get; set; } = new();
    }

    public class FileCategoryStatResponse
    {
        public long Count { get; set; }
        public long SizeInBytes { get; set; }
    }
}
