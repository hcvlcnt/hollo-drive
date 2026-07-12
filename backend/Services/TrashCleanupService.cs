using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services
{
    public class TrashCleanupService : ITrashCleanupService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IOptions<TrashCleanupOptions> _options;

        public TrashCleanupService(
            ApplicationDbContext dbContext,
            IBlobStorageService blobStorageService,
            IOptions<TrashCleanupOptions> options)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
            _options = options;
        }

        public async Task<TrashCleanupResult> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
        {
            var retentionDays = Math.Max(_options.Value.RetentionDays, 1);
            var batchSize = Math.Clamp(_options.Value.BatchSize, 1, 500);
            var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);
            var result = new TrashCleanupResult
            {
                CutoffUtc = cutoffUtc
            };

            result.DeletedFiles = await DeleteExpiredFilesAsync(cutoffUtc, batchSize, cancellationToken);
            result.DeletedFolders = await DeleteExpiredFoldersAsync(cutoffUtc, batchSize, cancellationToken);

            return result;
        }

        private async Task<int> DeleteExpiredFilesAsync(
            DateTime cutoffUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            var deletedFiles = 0;

            while (true)
            {
                var expiredFiles = await _dbContext.StoredFiles
                    .Where(storedFile => storedFile.DeletedAt != null && storedFile.DeletedAt <= cutoffUtc)
                    .OrderBy(storedFile => storedFile.DeletedAt)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (expiredFiles.Count == 0)
                {
                    return deletedFiles;
                }

                foreach (var expiredFile in expiredFiles)
                {
                    await _blobStorageService.DeleteBlobIfExistsAsync(
                        expiredFile.ContainerName,
                        expiredFile.BlobName,
                        cancellationToken);
                }

                _dbContext.StoredFiles.RemoveRange(expiredFiles);
                await _dbContext.SaveChangesAsync(cancellationToken);

                deletedFiles += expiredFiles.Count;
            }
        }

        private async Task<int> DeleteExpiredFoldersAsync(
            DateTime cutoffUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            var deletedFolders = 0;

            while (true)
            {
                var expiredLeafFolders = await _dbContext.StoredFolders
                    .Where(storedFolder => storedFolder.DeletedAt != null && storedFolder.DeletedAt <= cutoffUtc)
                    .Where(storedFolder =>
                        !_dbContext.StoredFiles.Any(storedFile => storedFile.FolderId == storedFolder.Id) &&
                        !_dbContext.StoredFolders.Any(childFolder => childFolder.ParentFolderId == storedFolder.Id))
                    .OrderBy(storedFolder => storedFolder.DeletedAt)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (expiredLeafFolders.Count == 0)
                {
                    return deletedFolders;
                }

                _dbContext.StoredFolders.RemoveRange(expiredLeafFolders);
                await _dbContext.SaveChangesAsync(cancellationToken);

                deletedFolders += expiredLeafFolders.Count;
            }
        }
    }
}
