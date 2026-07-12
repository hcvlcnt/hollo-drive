using backend.Models;

namespace backend.Services
{
    public static class UserMapper
    {
        public static UserResponse ToResponse(
            ApplicationUser user,
            StorageUsageScopeResponse? storageUsage = null)
        {
            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                StorageUsage = storageUsage,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DeletedAt = user.DeletedAt
            };
        }
    }
}
