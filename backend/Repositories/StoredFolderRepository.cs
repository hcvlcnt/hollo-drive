using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class StoredFolderRepository : IStoredFolderRepository
    {
        private readonly ApplicationDbContext _context;

        public StoredFolderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<StoredFolder?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            return ApplyDeletedFilter(_context.StoredFolders.AsNoTracking(), includeDeleted)
                .FirstOrDefaultAsync(storedFolder => storedFolder.Id == id, cancellationToken);
        }

        public Task<StoredFolder?> GetByIdForUserAsync(Guid id, Guid userId, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            return ApplyDeletedFilter(_context.StoredFolders.AsNoTracking(), includeDeleted)
                .FirstOrDefaultAsync(
                    storedFolder => storedFolder.Id == id && storedFolder.UserId == userId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFolder>> ListChildrenAsync(
            Guid? parentFolderId = null,
            Guid? userId = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.StoredFolders.AsNoTracking(), includeDeleted);

            if (userId.HasValue)
            {
                query = query.Where(storedFolder => storedFolder.UserId == userId.Value);
            }

            query = parentFolderId.HasValue
                ? query.Where(storedFolder => storedFolder.ParentFolderId == parentFolderId.Value)
                : query.Where(storedFolder => storedFolder.ParentFolderId == null);

            return await query
                .OrderBy(storedFolder => storedFolder.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFolder>> ListDeletedRootsAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var query = _context.StoredFolders.AsNoTracking()
                .Where(storedFolder => storedFolder.DeletedAt != null)
                .Where(storedFolder =>
                    storedFolder.ParentFolderId == null ||
                    !_context.StoredFolders.Any(parentFolder =>
                        parentFolder.Id == storedFolder.ParentFolderId &&
                        parentFolder.DeletedAt != null));

            if (userId.HasValue)
            {
                query = query.Where(storedFolder => storedFolder.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(storedFolder => storedFolder.DeletedAt)
                .ThenBy(storedFolder => storedFolder.Name)
                .Skip(Math.Max(skip, 0))
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StoredFolder>> ListStarredAsync(
            Guid? userId = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.StoredFolders.AsNoTracking(), includeDeleted: false)
                .Where(storedFolder => storedFolder.IsStarred);

            if (userId.HasValue)
            {
                query = query.Where(storedFolder => storedFolder.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(storedFolder => storedFolder.UpdatedAt ?? storedFolder.CreatedAt)
                .ThenBy(storedFolder => storedFolder.Name)
                .Skip(Math.Max(skip, 0))
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(cancellationToken);
        }

        public Task<StoredFolder?> GetChildByNameAsync(
            Guid userId,
            Guid? parentFolderId,
            string name,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.StoredFolders.AsNoTracking(), includeDeleted)
                .Where(storedFolder => storedFolder.UserId == userId && storedFolder.Name == name);

            query = parentFolderId.HasValue
                ? query.Where(storedFolder => storedFolder.ParentFolderId == parentFolderId.Value)
                : query.Where(storedFolder => storedFolder.ParentFolderId == null);

            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<StoredFolder> AddAsync(StoredFolder storedFolder, CancellationToken cancellationToken = default)
        {
            _context.StoredFolders.Add(storedFolder);
            await _context.SaveChangesAsync(cancellationToken);
            return storedFolder;
        }

        public async Task<StoredFolder> UpdateAsync(StoredFolder storedFolder, CancellationToken cancellationToken = default)
        {
            _context.StoredFolders.Update(storedFolder);
            await _context.SaveChangesAsync(cancellationToken);
            return storedFolder;
        }

        private static IQueryable<StoredFolder> ApplyDeletedFilter(IQueryable<StoredFolder> query, bool includeDeleted)
        {
            return includeDeleted ? query : query.Where(storedFolder => storedFolder.DeletedAt == null);
        }
    }
}
