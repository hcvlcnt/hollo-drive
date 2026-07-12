namespace backend.Services
{
    public class CreateStoredFolderRequest
    {
        public Guid UserId { get; set; }
        public Guid? ParentFolderId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
