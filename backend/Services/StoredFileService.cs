using backend.Models;
using backend.Repositories;

namespace backend.Services
{
    public class StoredFileService : IStoredFileService
    {
        private readonly IStoredFileRepository _storedFileRepository;
        private readonly IStoredFolderRepository _storedFolderRepository;

        public StoredFileService(
            IStoredFileRepository storedFileRepository,
            IStoredFolderRepository storedFolderRepository)
        {
            _storedFileRepository = storedFileRepository;
            _storedFolderRepository = storedFolderRepository;
        }

        public Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _storedFileRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
        }

        public Task<StoredFile?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            return isAdmin
                ? _storedFileRepository.GetByIdAsync(id, cancellationToken: cancellationToken)
                : _storedFileRepository.GetByIdForUserAsync(id, userId, cancellationToken: cancellationToken);
        }

        public Task<StoredFile?> GetByBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            return _storedFileRepository.GetByBlobAsync(
                NormalizeRequired(containerName, nameof(containerName)),
                NormalizeRequired(blobName, nameof(blobName)),
                cancellationToken: cancellationToken);
        }

        public Task<IReadOnlyList<StoredFile>> SearchAsync(
            string? searchTerm = null,
            string? virtualPath = null,
            Guid? folderId = null,
            Guid? userId = null,
            bool isAdmin = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            return _storedFileRepository.SearchAsync(
                searchTerm,
                NormalizeOptional(virtualPath),
                folderId,
                isAdmin ? userId : userId ?? throw new ArgumentException("User id is required.", nameof(userId)),
                skip: skip,
                take: take,
                cancellationToken: cancellationToken);
        }

        public async Task<StoredFile> CreateMetadataAsync(CreateStoredFileMetadataRequest request, CancellationToken cancellationToken = default)
        {
            if (request.SizeInBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.SizeInBytes), "File size cannot be negative.");
            }

            if (request.FolderId.HasValue)
            {
                var folder = await _storedFolderRepository.GetByIdForUserAsync(
                    request.FolderId.Value,
                    request.UserId,
                    cancellationToken: cancellationToken);

                if (folder is null)
                {
                    throw new ArgumentException("Folder was not found.", nameof(request.FolderId));
                }
            }

            var storedFile = new StoredFile
            {
                UserId = request.UserId,
                FolderId = request.FolderId,
                Name = NormalizeRequired(request.Name, nameof(request.Name)),
                OriginalName = NormalizeRequired(request.OriginalName, nameof(request.OriginalName)),
                Extension = NormalizeExtension(request.Extension, request.OriginalName),
                ContentType = NormalizeOptional(request.ContentType) ?? "application/octet-stream",
                SizeInBytes = request.SizeInBytes,
                ContainerName = NormalizeRequired(request.ContainerName, nameof(request.ContainerName)),
                BlobName = NormalizeRequired(request.BlobName, nameof(request.BlobName)),
                VirtualPath = NormalizeOptional(request.VirtualPath),
                ETag = NormalizeOptional(request.ETag),
                ChecksumSha256 = NormalizeOptional(request.ChecksumSha256),
                CreatedAt = DateTime.UtcNow
            };

            return await _storedFileRepository.AddAsync(storedFile, cancellationToken);
        }

        public async Task<StoredFile?> RenameAsync(Guid id, Guid userId, bool isAdmin, string name, CancellationToken cancellationToken = default)
        {
            var storedFile = await GetByIdForUserAsync(id, userId, isAdmin, cancellationToken);

            if (storedFile is null)
            {
                return null;
            }

            storedFile.Name = NormalizeRequired(name, nameof(name));
            storedFile.UpdatedAt = DateTime.UtcNow;

            return await _storedFileRepository.UpdateAsync(storedFile, cancellationToken);
        }

        public async Task<StoredFile?> MoveToTrashAsync(
            Guid id,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var storedFile = await GetByIdForUserAsync(id, userId, isAdmin, cancellationToken);

            if (storedFile is null)
            {
                return null;
            }

            storedFile.DeletedAt = DateTime.UtcNow;
            storedFile.UpdatedAt = DateTime.UtcNow;

            return await _storedFileRepository.UpdateAsync(storedFile, cancellationToken);
        }

        public async Task<StoredFile?> RestoreAsync(
            Guid id,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var storedFile = isAdmin
                ? await _storedFileRepository.GetByIdAsync(id, includeDeleted: true, cancellationToken)
                : await _storedFileRepository.GetByIdForUserAsync(id, userId, includeDeleted: true, cancellationToken);

            if (storedFile is null)
            {
                return null;
            }

            storedFile.DeletedAt = null;
            storedFile.UpdatedAt = DateTime.UtcNow;

            return await _storedFileRepository.UpdateAsync(storedFile, cancellationToken);
        }

        public Task<IReadOnlyList<StoredFile>> ListTrashAsync(
            Guid? userId,
            bool isAdmin,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            return _storedFileRepository.ListDeletedRootsAsync(
                isAdmin ? userId : userId ?? throw new ArgumentException("User id is required.", nameof(userId)),
                skip,
                take,
                cancellationToken);
        }

        public Task<IReadOnlyList<StoredFile>> ListStarredAsync(
            Guid? userId,
            bool isAdmin,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            return _storedFileRepository.ListStarredAsync(
                isAdmin ? userId : userId ?? throw new ArgumentException("User id is required.", nameof(userId)),
                skip,
                take,
                cancellationToken);
        }

        public async Task<StoredFile?> SetStarredAsync(
            Guid id,
            Guid userId,
            bool isAdmin,
            bool isStarred,
            CancellationToken cancellationToken = default)
        {
            var storedFile = await GetByIdForUserAsync(id, userId, isAdmin, cancellationToken);

            if (storedFile is null)
            {
                return null;
            }

            storedFile.IsStarred = isStarred;
            storedFile.UpdatedAt = DateTime.UtcNow;

            return await _storedFileRepository.UpdateAsync(storedFile, cancellationToken);
        }

        public async Task<FileCategoryStatsResponse> GetCategoryStatsAsync(
            Guid? userId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var stats = await _storedFileRepository.GetCategoryStatsAsync(
                isAdmin ? userId : userId ?? throw new ArgumentException("User id is required.", nameof(userId)),
                cancellationToken: cancellationToken);

            return new FileCategoryStatsResponse
            {
                Images = CreateStatResponse(stats.Images),
                Videos = CreateStatResponse(stats.Videos),
                Documents = CreateStatResponse(stats.Documents),
                Audio = CreateStatResponse(stats.Audio),
                Others = CreateStatResponse(stats.Others)
            };
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", parameterName);
            }

            return value.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeExtension(string? extension, string originalName)
        {
            var normalizedExtension = NormalizeOptional(extension);

            if (normalizedExtension is not null)
            {
                return normalizedExtension.StartsWith('.') ? normalizedExtension : $".{normalizedExtension}";
            }

            var originalNameExtension = Path.GetExtension(originalName);
            return string.IsNullOrWhiteSpace(originalNameExtension) ? null : originalNameExtension;
        }

        private static FileCategoryStatResponse CreateStatResponse(FileCategoryStat stat)
        {
            return new FileCategoryStatResponse
            {
                Count = stat.Count,
                SizeInBytes = stat.SizeInBytes
            };
        }
    }
}
