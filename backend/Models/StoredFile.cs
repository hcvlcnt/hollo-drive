using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class StoredFile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public Guid? FolderId { get; set; }
        public StoredFolder? Folder { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OriginalName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Extension { get; set; }

        [Required]
        [MaxLength(128)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long SizeInBytes { get; set; }

        [Required]
        [MaxLength(63)]
        public string ContainerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1024)]
        public string BlobName { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? VirtualPath { get; set; }

        [MaxLength(256)]
        public string? ETag { get; set; }

        [MaxLength(64)]
        public string? ChecksumSha256 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsStarred { get; set; }
    }
}
