using backend.Models;
using backend.Repositories;

namespace backend.Services
{
    public class StoredFolderService : IStoredFolderService
    {
        private readonly IStoredFolderRepository _storedFolderRepository;
        private readonly IStoredFileRepository _storedFileRepository;

        public StoredFolderService(
            IStoredFolderRepository storedFolderRepository,
            IStoredFileRepository storedFileRepository)
        {
            _storedFolderRepository = storedFolderRepository;
            _storedFileRepository = storedFileRepository;
        }

        public Task<StoredFolder?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            return isAdmin
                ? _storedFolderRepository.GetByIdAsync(id, cancellationToken: cancellationToken)
                : _storedFolderRepository.GetByIdForUserAsync(id, userId, cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFolder>> ListChildrenAsync(
            Guid? parentFolderId,
            Guid userId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            if (parentFolderId.HasValue)
            {
                var parentFolder = await GetByIdForUserAsync(parentFolderId.Value, userId, isAdmin, cancellationToken);

                if (parentFolder is null)
                {
                    throw new ArgumentException("Parent folder was not found.", nameof(parentFolderId));
                }
            }

            return await _storedFolderRepository.ListChildrenAsync(
                parentFolderId,
                isAdmin ? null : userId,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFolder>> GetBreadcrumbsAsync(
            Guid? folderId,
            Guid userId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            var breadcrumbs = new List<StoredFolder>();
            var nextFolderId = folderId;

            while (nextFolderId.HasValue)
            {
                var folder = await GetByIdForUserAsync(nextFolderId.Value, userId, isAdmin, cancellationToken);

                if (folder is null)
                {
                    throw new ArgumentException("Folder was not found.", nameof(folderId));
                }

                breadcrumbs.Add(folder);
                nextFolderId = folder.ParentFolderId;
            }

            breadcrumbs.Reverse();
            return breadcrumbs;
        }

        public async Task<StoredFolder> CreateAsync(CreateStoredFolderRequest request, CancellationToken cancellationToken = default)
        {
            var name = NormalizeRequired(request.Name, nameof(request.Name));

            if (request.ParentFolderId.HasValue)
            {
                var parentFolder = await _storedFolderRepository.GetByIdForUserAsync(
                    request.ParentFolderId.Value,
                    request.UserId,
                    cancellationToken: cancellationToken);

                if (parentFolder is null)
                {
                    throw new ArgumentException("Parent folder was not found.", nameof(request.ParentFolderId));
                }
            }

            var duplicateFolder = await _storedFolderRepository.GetChildByNameAsync(
                request.UserId,
                request.ParentFolderId,
                name,
                cancellationToken: cancellationToken);

            if (duplicateFolder is not null)
            {
                throw new ArgumentException("A folder with this name already exists in this location.", nameof(request.Name));
            }

            var storedFolder = new StoredFolder
            {
                UserId = request.UserId,
                ParentFolderId = request.ParentFolderId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            return await _storedFolderRepository.AddAsync(storedFolder, cancellationToken);
        }

        public async Task<StoredFolder?> RenameAsync(
            Guid id,
            Guid userId,
            bool isAdmin,
            string name,
            CancellationToken cancellationToken = default)
        {
            var storedFolder = await GetByIdForUserAsync(id, userId, isAdmin, cancellationToken);

            if (storedFolder is null)
            {
                return null;
            }

            var normalizedName = NormalizeRequired(name, nameof(name));
            var duplicateFolder = await _storedFolderRepository.GetChildByNameAsync(
                storedFolder.UserId,
                storedFolder.ParentFolderId,
                normalizedName,
                cancellationToken: cancellationToken);

            if (duplicateFolder is not null && duplicateFolder.Id != storedFolder.Id)
            {
                throw new ArgumentException("A folder with this name already exists in this location.", nameof(name));
            }

            storedFolder.Name = normalizedName;
            storedFolder.UpdatedAt = DateTime.UtcNow;

            return await _storedFolderRepository.UpdateAsync(storedFolder, cancellationToken);
        }

        public async Task<StoredFolder?> MoveToTrashAsync(
            Guid id,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var storedFolder = await GetByIdForUserAsync(id, userId, isAdmin, cancellationToken);

            if (storedFolder is null)
            {
                return null;
            }

            var deletedAt = DateTime.UtcNow;
            await MoveFolderTreeToTrashAsync(storedFolder, deletedAt, cancellationToken);

            return storedFolder;
        }

        public async Task<StoredFolder?> RestoreAsync(
            Guid id,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var storedFolder = isAdmin
                ? await _storedFolderRepository.GetByIdAsync(id, includeDeleted: true, cancellationToken)
                : await _storedFolderRepository.GetByIdForUserAsync(id, userId, includeDeleted: true, cancellationToken);

            if (storedFolder is null)
            {
                return null;
            }

            var restoredAt = DateTime.UtcNow;
            await RestoreFolderTreeAsync(storedFolder, restoredAt, cancellationToken);

            return storedFolder;
        }

        public Task<IReadOnlyList<StoredFolder>> ListTrashAsync(
            Guid? userId,
            bool isAdmin,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            return _storedFolderRepository.ListDeletedRootsAsync(
                isAdmin ? userId : userId ?? throw new ArgumentException("User id is required.", nameof(userId)),
                skip,
                take,
                cancellationToken);
        }

        public Task<IReadOnlyList<StoredFolder>> ListStarredAsync(
            Guid? userId,
            bool isAdmin,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            return _storedFolderRepository.ListStarredAsync(
                isAdmin ? userId : userId ?? throw new ArgumentException("User id is required.", nameof(userId)),
                skip,
                take,
                cancellationToken);
        }

        public async Task<StoredFolder?> SetStarredAsync(
            Guid id,
            Guid userId,
            bool isAdmin,
            bool isStarred,
            CancellationToken cancellationToken = default)
        {
            var storedFolder = await GetByIdForUserAsync(id, userId, isAdmin, cancellationToken);

            if (storedFolder is null)
            {
                return null;
            }

            storedFolder.IsStarred = isStarred;
            storedFolder.UpdatedAt = DateTime.UtcNow;

            return await _storedFolderRepository.UpdateAsync(storedFolder, cancellationToken);
        }

        private async Task MoveFolderTreeToTrashAsync(
            StoredFolder storedFolder,
            DateTime deletedAt,
            CancellationToken cancellationToken)
        {
            var childFolders = await _storedFolderRepository.ListChildrenAsync(
                storedFolder.Id,
                storedFolder.UserId,
                cancellationToken: cancellationToken);
            foreach (var childFolder in childFolders)
            {
                await MoveFolderTreeToTrashAsync(childFolder, deletedAt, cancellationToken);
            }

            IReadOnlyList<StoredFile> childFiles;
            do
            {
                childFiles = await _storedFileRepository.SearchAsync(
                    folderId: storedFolder.Id,
                    userId: storedFolder.UserId,
                    skip: 0,
                    take: 100,
                    cancellationToken: cancellationToken);

                foreach (var childFile in childFiles)
                {
                    childFile.DeletedAt = deletedAt;
                    childFile.UpdatedAt = deletedAt;
                    await _storedFileRepository.UpdateAsync(childFile, cancellationToken);
                }
            }
            while (childFiles.Count > 0);

            storedFolder.DeletedAt = deletedAt;
            storedFolder.UpdatedAt = deletedAt;
            await _storedFolderRepository.UpdateAsync(storedFolder, cancellationToken);
        }

        private async Task RestoreFolderTreeAsync(
            StoredFolder storedFolder,
            DateTime restoredAt,
            CancellationToken cancellationToken)
        {
            var childFolders = await _storedFolderRepository.ListChildrenAsync(
                storedFolder.Id,
                storedFolder.UserId,
                includeDeleted: true,
                cancellationToken: cancellationToken);
            foreach (var childFolder in childFolders)
            {
                await RestoreFolderTreeAsync(childFolder, restoredAt, cancellationToken);
            }

            IReadOnlyList<StoredFile> childFiles;
            var skip = 0;
            do
            {
                childFiles = await _storedFileRepository.SearchAsync(
                    folderId: storedFolder.Id,
                    userId: storedFolder.UserId,
                    includeDeleted: true,
                    skip: skip,
                    take: 100,
                    cancellationToken: cancellationToken);

                foreach (var childFile in childFiles.Where(file => file.DeletedAt is not null))
                {
                    childFile.DeletedAt = null;
                    childFile.UpdatedAt = restoredAt;
                    await _storedFileRepository.UpdateAsync(childFile, cancellationToken);
                }

                skip += childFiles.Count;
            }
            while (childFiles.Count > 0);

            storedFolder.DeletedAt = null;
            storedFolder.UpdatedAt = restoredAt;
            await _storedFolderRepository.UpdateAsync(storedFolder, cancellationToken);
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", parameterName);
            }

            return value.Trim();
        }
    }
}
