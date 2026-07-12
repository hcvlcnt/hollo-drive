using backend.Repositories;
using Microsoft.Extensions.Options;

namespace backend.Services
{
    public class StorageUsageService : IStorageUsageService
    {
        private readonly IStoredFileRepository _storedFileRepository;
        private readonly IUserRepository _userRepository;
        private readonly StorageQuotaOptions _options;

        public StorageUsageService(
            IStoredFileRepository storedFileRepository,
            IUserRepository userRepository,
            IOptions<StorageQuotaOptions> options)
        {
            _storedFileRepository = storedFileRepository;
            _userRepository = userRepository;
            _options = options.Value;
        }

        public async Task<StorageUsageResponse> GetUsageAsync(
            Guid currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var userQuota = GetDefaultUserQuota();
            var currentUserUsed = await _storedFileRepository.SumSizeInBytesAsync(
                currentUserId,
                cancellationToken: cancellationToken);
            var response = new StorageUsageResponse
            {
                IsAdmin = isAdmin,
                CurrentUser = CreateScope(currentUserUsed, userQuota)
            };

            if (isAdmin)
            {
                var activeUserCount = await _userRepository.CountActiveAsync(cancellationToken);
                var systemUsed = await _storedFileRepository.SumSizeInBytesAsync(
                    cancellationToken: cancellationToken);
                var systemQuota = Math.Max(activeUserCount, 1) * userQuota;

                response.System = CreateScope(systemUsed, systemQuota);
            }

            return response;
        }

        public async Task EnsureUserCanStoreAsync(
            Guid currentUserId,
            long additionalSizeInBytes,
            CancellationToken cancellationToken = default)
        {
            if (additionalSizeInBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(additionalSizeInBytes), "File size cannot be negative.");
            }

            var userQuota = GetDefaultUserQuota();
            var currentUserUsed = await _storedFileRepository.SumSizeInBytesAsync(
                currentUserId,
                cancellationToken: cancellationToken);

            if (currentUserUsed + additionalSizeInBytes > userQuota)
            {
                throw new InvalidOperationException("Storage quota exceeded.");
            }
        }

        private long GetDefaultUserQuota()
        {
            return _options.DefaultUserQuotaInBytes > 0
                ? _options.DefaultUserQuotaInBytes
                : 53_687_091_200;
        }

        private static StorageUsageScopeResponse CreateScope(long usedInBytes, long quotaInBytes)
        {
            return new StorageUsageScopeResponse
            {
                UsedInBytes = usedInBytes,
                QuotaInBytes = quotaInBytes,
                UsedPercentage = quotaInBytes > 0
                    ? Math.Clamp(usedInBytes / (double)quotaInBytes * 100, 0, 100)
                    : 0
            };
        }
    }
}
