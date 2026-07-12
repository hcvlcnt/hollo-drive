using backend.Models;

namespace backend.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetByEmailAsync(string email, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ApplicationUser>> SearchAsync(
            string? searchTerm = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);
        Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
        Task<ApplicationUser> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    }
}
