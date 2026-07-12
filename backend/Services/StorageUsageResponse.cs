namespace backend.Services
{
    public class StorageUsageResponse
    {
        public StorageUsageScopeResponse CurrentUser { get; set; } = new();
        public StorageUsageScopeResponse? System { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class StorageUsageScopeResponse
    {
        public long UsedInBytes { get; set; }
        public long QuotaInBytes { get; set; }
        public double UsedPercentage { get; set; }
    }
}
