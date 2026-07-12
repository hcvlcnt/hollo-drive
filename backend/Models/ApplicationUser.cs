using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class ApplicationUser
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(320)]
        public string NormalizedEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(512)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string Role { get; set; } = ApplicationRole.User;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();
        public ICollection<StoredFolder> StoredFolders { get; set; } = new List<StoredFolder>();
    }
}
