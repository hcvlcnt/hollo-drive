using backend.Models;

namespace backend.Repositories
{
    public interface IStoredFolderRepository
    {
        Task<StoredFolder?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<StoredFolder?> GetByIdForUserAsync(Guid id, Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFolder>> ListChildrenAsync(
            Guid? parentFolderId = null,
            Guid? userId = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFolder>> ListDeletedRootsAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFolder>> ListStarredAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<StoredFolder?> GetChildByNameAsync(
            Guid userId,
            Guid? parentFolderId,
            string name,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);
        Task<StoredFolder> AddAsync(StoredFolder storedFolder, CancellationToken cancellationToken = default);
        Task<StoredFolder> UpdateAsync(StoredFolder storedFolder, CancellationToken cancellationToken = default);
    }
}
