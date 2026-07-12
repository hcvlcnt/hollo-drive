namespace backend.Services
{
    public class TrashCleanupResult
    {
        public DateTime CutoffUtc { get; set; }
        public int DeletedFiles { get; set; }
        public int DeletedFolders { get; set; }
    }
}
