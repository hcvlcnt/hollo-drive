using backend.Models;

namespace backend.Services
{
    public interface IStoredFolderService
    {
        Task<StoredFolder?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFolder>> ListChildrenAsync(Guid? parentFolderId, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFolder>> GetBreadcrumbsAsync(Guid? folderId, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);
        Task<StoredFolder> CreateAsync(CreateStoredFolderRequest request, CancellationToken cancellationToken = default);
        Task<StoredFolder?> RenameAsync(Guid id, Guid userId, bool isAdmin, string name, CancellationToken cancellationToken = default);
        Task<StoredFolder?> MoveToTrashAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<StoredFolder?> RestoreAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFolder>> ListTrashAsync(Guid? userId, bool isAdmin, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoredFolder>> ListStarredAsync(Guid? userId, bool isAdmin, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
        Task<StoredFolder?> SetStarredAsync(Guid id, Guid userId, bool isAdmin, bool isStarred, CancellationToken cancellationToken = default);
    }
}
