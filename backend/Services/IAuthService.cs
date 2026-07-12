namespace backend.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task EnsureDefaultAdminAsync(CancellationToken cancellationToken = default);
    }
}
