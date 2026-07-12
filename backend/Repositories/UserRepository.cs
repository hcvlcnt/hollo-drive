using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<ApplicationUser?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            return ApplyDeletedFilter(_context.Users.AsNoTracking(), includeDeleted)
                .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        }

        public Task<ApplicationUser?> GetByEmailAsync(string email, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);

            return ApplyDeletedFilter(_context.Users.AsNoTracking(), includeDeleted)
                .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationUser>> SearchAsync(
            string? searchTerm = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyDeletedFilter(_context.Users.AsNoTracking(), includeDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearchTerm = searchTerm.Trim();
                query = query.Where(user =>
                    user.Name.Contains(normalizedSearchTerm) ||
                    user.Email.Contains(normalizedSearchTerm));
            }

            return await query
                .OrderBy(user => user.Name)
                .ThenBy(user => user.Email)
                .Skip(Math.Max(skip, 0))
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
        {
            return _context.Users
                .AsNoTracking()
                .CountAsync(user => user.DeletedAt == null && user.IsActive, cancellationToken);
        }

        public async Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            user.NormalizedEmail = NormalizeEmail(user.Email);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task<ApplicationUser> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            user.NormalizedEmail = NormalizeEmail(user.Email);
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        private static IQueryable<ApplicationUser> ApplyDeletedFilter(IQueryable<ApplicationUser> query, bool includeDeleted)
        {
            return includeDeleted ? query : query.Where(user => user.DeletedAt == null);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }
    }
}
