using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class StoredFileRepository : IStoredFileRepository
    {
        private static readonly string[] DocumentExtensions =
        [
            ".csv",
            ".doc",
            ".docx",
            ".ods",
            ".odt",
            ".pdf",
            ".ppt",
            ".pptx",
            ".rtf",
            ".txt",
            ".xls",
            ".xlsx"
        ];

        private readonly ApplicationDbContext _context;

        public StoredFileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<StoredFile?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            return ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted)
                .FirstOrDefaultAsync(storedFile => storedFile.Id == id, cancellationToken);
        }

        public Task<StoredFile?> GetByIdForUserAsync(Guid id, Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            return ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted)
                .FirstOrDefaultAsync(storedFile => storedFile.Id == id && storedFile.UserId == userId, cancellationToken);
        }

        public Task<StoredFile?> GetByBlobAsync(string containerName, string blobName, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            return ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted)
                .FirstOrDefaultAsync(
                    storedFile => storedFile.ContainerName == containerName && storedFile.BlobName == blobName,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFile>> SearchAsync(
            string? searchTerm = null,
            string? virtualPath = null,
            Guid? folderId = null,
            Guid? userId = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted);

            if (userId.HasValue)
            {
                query = query.Where(storedFile => storedFile.UserId == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearchTerm = searchTerm.Trim();
                query = query.Where(storedFile =>
                    storedFile.Name.Contains(normalizedSearchTerm) ||
                    storedFile.OriginalName.Contains(normalizedSearchTerm) ||
                    storedFile.BlobName.Contains(normalizedSearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(virtualPath))
            {
                var normalizedVirtualPath = virtualPath.Trim();
                query = query.Where(storedFile => storedFile.VirtualPath == normalizedVirtualPath);
            }

            query = folderId.HasValue
                ? query.Where(storedFile => storedFile.FolderId == folderId.Value)
                : query.Where(storedFile => storedFile.FolderId == null);

            return await query
                .OrderBy(storedFile => storedFile.Name)
                .Skip(Math.Max(skip, 0))
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFile>> ListDeletedRootsAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var query = _context.StoredFiles.AsNoTracking()
                .Where(storedFile => storedFile.DeletedAt != null)
                .Where(storedFile =>
                    storedFile.FolderId == null ||
                    !_context.StoredFolders.Any(storedFolder =>
                        storedFolder.Id == storedFile.FolderId &&
                        storedFolder.DeletedAt != null));

            if (userId.HasValue)
            {
                query = query.Where(storedFile => storedFile.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(storedFile => storedFile.DeletedAt)
                .ThenBy(storedFile => storedFile.Name)
                .Skip(Math.Max(skip, 0))
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFile>> ListStarredAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted: false)
                .Where(storedFile => storedFile.IsStarred);

            if (userId.HasValue)
            {
                query = query.Where(storedFile => storedFile.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(storedFile => storedFile.UpdatedAt ?? storedFile.CreatedAt)
                .ThenBy(storedFile => storedFile.Name)
                .Skip(Math.Max(skip, 0))
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(cancellationToken);
        }

        public Task<long> SumSizeInBytesAsync(
            Guid? userId = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted);

            if (userId.HasValue)
            {
                query = query.Where(storedFile => storedFile.UserId == userId.Value);
            }

            return query.SumAsync(storedFile => storedFile.SizeInBytes, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, long>> SumSizeInBytesByUserAsync(
            IEnumerable<Guid> userIds,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var normalizedUserIds = userIds.Distinct().ToArray();

            if (normalizedUserIds.Length == 0)
            {
                return new Dictionary<Guid, long>();
            }

            return await ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted)
                .Where(storedFile => normalizedUserIds.Contains(storedFile.UserId))
                .GroupBy(storedFile => storedFile.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    UsedInBytes = group.Sum(storedFile => storedFile.SizeInBytes)
                })
                .ToDictionaryAsync(
                    item => item.UserId,
                    item => item.UsedInBytes,
                    cancellationToken);
        }

        public async Task<FileCategoryStats> GetCategoryStatsAsync(
            Guid? userId = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.StoredFiles.AsNoTracking(), includeDeleted);

            if (userId.HasValue)
            {
                query = query.Where(storedFile => storedFile.UserId == userId.Value);
            }

            return new FileCategoryStats
            {
                Images = await CreateStatAsync(ApplyImagesFilter(query), cancellationToken),
                Videos = await CreateStatAsync(ApplyVideosFilter(query), cancellationToken),
                Documents = await CreateStatAsync(ApplyDocumentsFilter(query), cancellationToken),
                Audio = await CreateStatAsync(ApplyAudioFilter(query), cancellationToken),
                Others = await CreateStatAsync(ApplyOthersFilter(query), cancellationToken)
            };
        }

        public async Task<StoredFile> AddAsync(StoredFile storedFile, CancellationToken cancellationToken = default)
        {
            _context.StoredFiles.Add(storedFile);
            await _context.SaveChangesAsync(cancellationToken);
            return storedFile;
        }

        public async Task<StoredFile> UpdateAsync(StoredFile storedFile, CancellationToken cancellationToken = default)
        {
            _context.StoredFiles.Update(storedFile);
            await _context.SaveChangesAsync(cancellationToken);
            return storedFile;
        }

        private static IQueryable<StoredFile> ApplyDeletedFilter(IQueryable<StoredFile> query, bool includeDeleted)
        {
            return includeDeleted ? query : query.Where(storedFile => storedFile.DeletedAt == null);
        }

        private static IQueryable<StoredFile> ApplyImagesFilter(IQueryable<StoredFile> query)
        {
            return query.Where(storedFile => storedFile.ContentType.StartsWith("image/"));
        }

        private static IQueryable<StoredFile> ApplyVideosFilter(IQueryable<StoredFile> query)
        {
            return query.Where(storedFile => storedFile.ContentType.StartsWith("video/"));
        }

        private static IQueryable<StoredFile> ApplyAudioFilter(IQueryable<StoredFile> query)
        {
            return query.Where(storedFile => storedFile.ContentType.StartsWith("audio/"));
        }

        private static IQueryable<StoredFile> ApplyDocumentsFilter(IQueryable<StoredFile> query)
        {
            return query.Where(storedFile =>
                storedFile.ContentType.Contains("document") ||
                storedFile.ContentType.Contains("msword") ||
                storedFile.ContentType.Contains("pdf") ||
                storedFile.ContentType.Contains("presentation") ||
                storedFile.ContentType.Contains("spreadsheet") ||
                storedFile.ContentType.Contains("text/") ||
                (storedFile.Extension != null && DocumentExtensions.Contains(storedFile.Extension.ToLower())));
        }

        private static IQueryable<StoredFile> ApplyOthersFilter(IQueryable<StoredFile> query)
        {
            return query.Where(storedFile =>
                !storedFile.ContentType.StartsWith("image/") &&
                !storedFile.ContentType.StartsWith("video/") &&
                !storedFile.ContentType.StartsWith("audio/") &&
                !storedFile.ContentType.Contains("document") &&
                !storedFile.ContentType.Contains("msword") &&
                !storedFile.ContentType.Contains("pdf") &&
                !storedFile.ContentType.Contains("presentation") &&
                !storedFile.ContentType.Contains("spreadsheet") &&
                !storedFile.ContentType.Contains("text/") &&
                (storedFile.Extension == null || !DocumentExtensions.Contains(storedFile.Extension.ToLower())));
        }

        private static async Task<FileCategoryStat> CreateStatAsync(
            IQueryable<StoredFile> query,
            CancellationToken cancellationToken)
        {
            return new FileCategoryStat
            {
                Count = await query.LongCountAsync(cancellationToken),
                SizeInBytes = await query.SumAsync(storedFile => (long?)storedFile.SizeInBytes, cancellationToken) ?? 0
            };
        }
    }
}
