namespace backend.Services
{
    public interface IUserManagementService
    {
        Task<UserResponse?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserResponse>> SearchAsync(
            string? searchTerm = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);
        Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
