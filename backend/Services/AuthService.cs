using backend.Auth;
using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private const int MinimumPasswordLength = 8;

        private readonly IUserRepository _userRepository;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ITokenService _tokenService;
        private readonly AdminUserOptions _adminUserOptions;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHashService passwordHashService,
            ITokenService tokenService,
            IOptions<AdminUserOptions> adminUserOptions)
        {
            _userRepository = userRepository;
            _passwordHashService = passwordHashService;
            _tokenService = tokenService;
            _adminUserOptions = adminUserOptions.Value;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var name = NormalizeRequired(request.Name, nameof(request.Name));
            var email = NormalizeEmail(request.Email);
            ValidatePassword(request.Password);

            var existingUser = await _userRepository.GetByEmailAsync(email, includeDeleted: true, cancellationToken);
            if (existingUser is not null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var user = new ApplicationUser
            {
                Name = name,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                PasswordHash = _passwordHashService.HashPassword(request.Password),
                Role = ApplicationRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);
            return _tokenService.CreateAccessToken(user);
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(request.Email);
            var user = await _userRepository.GetByEmailAsync(email, includeDeleted: false, cancellationToken);

            if (user is null ||
                !user.IsActive ||
                !_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return null;
            }

            return _tokenService.CreateAccessToken(user);
        }

        public async Task<UserResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken: cancellationToken);
            return user is null || !user.IsActive ? null : UserMapper.ToResponse(user);
        }

        public async Task EnsureDefaultAdminAsync(CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(_adminUserOptions.Email);
            var existingUser = await _userRepository.GetByEmailAsync(email, includeDeleted: true, cancellationToken);

            if (existingUser is not null)
            {
                if (existingUser.Role != ApplicationRole.Admin || !existingUser.IsActive || existingUser.DeletedAt is not null)
                {
                    existingUser.Role = ApplicationRole.Admin;
                    existingUser.IsActive = true;
                    existingUser.DeletedAt = null;
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(existingUser, cancellationToken);
                }

                return;
            }

            ValidatePassword(_adminUserOptions.Password);

            var admin = new ApplicationUser
            {
                Name = NormalizeRequired(_adminUserOptions.Name, nameof(_adminUserOptions.Name)),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                PasswordHash = _passwordHashService.HashPassword(_adminUserOptions.Password),
                Role = ApplicationRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(admin, cancellationToken);
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", parameterName);
            }

            return value.Trim();
        }

        private static string NormalizeEmail(string email)
        {
            var normalizedEmail = NormalizeRequired(email, nameof(email));

            try
            {
                return new MailAddress(normalizedEmail).Address;
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("Invalid email.", nameof(email), exception);
            }
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
            {
                throw new ArgumentException($"Password must have at least {MinimumPasswordLength} characters.", nameof(password));
            }
        }
    }
}
