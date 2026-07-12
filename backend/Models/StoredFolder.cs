using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class StoredFolder
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public Guid? ParentFolderId { get; set; }
        public StoredFolder? ParentFolder { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsStarred { get; set; }

        public ICollection<StoredFolder> ChildFolders { get; set; } = new List<StoredFolder>();
        public ICollection<StoredFile> Files { get; set; } = new List<StoredFile>();
    }
}
