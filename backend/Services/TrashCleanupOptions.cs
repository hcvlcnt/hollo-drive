namespace backend.Services
{
    public class TrashCleanupOptions
    {
        public const string SectionName = "TrashCleanup";

        public bool Enabled { get; set; } = true;
        public int RetentionDays { get; set; } = 30;
        public int IntervalHours { get; set; } = 24;
        public int BatchSize { get; set; } = 100;
        public bool RunOnStartup { get; set; } = true;
    }
}
