namespace backend.Services
{
    public class StorageQuotaOptions
    {
        public const string SectionName = "StorageQuota";

        public long DefaultUserQuotaInBytes { get; set; } = 53_687_091_200;
    }
}
