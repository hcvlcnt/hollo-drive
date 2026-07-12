using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace backend.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStoredFileRepository _storedFileRepository;
        private readonly StorageQuotaOptions _storageQuotaOptions;

        public UserManagementService(
            IUserRepository userRepository,
            IStoredFileRepository storedFileRepository,
            IOptions<StorageQuotaOptions> storageQuotaOptions)
        {
            _userRepository = userRepository;
            _storedFileRepository = storedFileRepository;
            _storageQuotaOptions = storageQuotaOptions.Value;
        }

        public async Task<UserResponse?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(id, includeDeleted, cancellationToken);

            if (user is null)
            {
                return null;
            }

            var usedInBytes = await _storedFileRepository.SumSizeInBytesAsync(
                user.Id,
                cancellationToken: cancellationToken);

            return UserMapper.ToResponse(user, CreateStorageUsage(usedInBytes));
        }

        public async Task<IReadOnlyList<UserResponse>> SearchAsync(
            string? searchTerm = null,
            bool includeDeleted = false,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.SearchAsync(searchTerm, includeDeleted, skip, take, cancellationToken);
            var usageByUserId = await _storedFileRepository.SumSizeInBytesByUserAsync(
                users.Select(user => user.Id),
                cancellationToken: cancellationToken);

            return users
                .Select(user => UserMapper.ToResponse(
                    user,
                    CreateStorageUsage(usageByUserId.GetValueOrDefault(user.Id))))
                .ToList();
        }

        public async Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(id, includeDeleted: true, cancellationToken);

            if (user is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var normalizedEmail = NormalizeEmail(request.Email);
                var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, includeDeleted: true, cancellationToken);

                if (existingUser is not null && existingUser.Id != id)
                {
                    throw new InvalidOperationException("A user with this email already exists.");
                }

                user.Email = normalizedEmail;
                user.NormalizedEmail = normalizedEmail.ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                user.Role = ApplicationRole.Normalize(request.Role);
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (user.IsActive && user.DeletedAt is not null)
            {
                user.DeletedAt = null;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return UserMapper.ToResponse(user);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(id, includeDeleted: false, cancellationToken);

            if (user is null)
            {
                return false;
            }

            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }

        private static string NormalizeEmail(string email)
        {
            var normalizedEmail = email.Trim();

            try
            {
                return new MailAddress(normalizedEmail).Address;
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("Invalid email.", nameof(email), exception);
            }
        }

        private StorageUsageScopeResponse CreateStorageUsage(long usedInBytes)
        {
            var quotaInBytes = GetDefaultUserQuota();

            return new StorageUsageScopeResponse
            {
                UsedInBytes = usedInBytes,
                QuotaInBytes = quotaInBytes,
                UsedPercentage = quotaInBytes > 0
                    ? Math.Clamp(usedInBytes / (double)quotaInBytes * 100, 0, 100)
                    : 0
            };
        }

        private long GetDefaultUserQuota()
        {
            return _storageQuotaOptions.DefaultUserQuotaInBytes > 0
                ? _storageQuotaOptions.DefaultUserQuotaInBytes
                : 53_687_091_200;
        }
    }
}
