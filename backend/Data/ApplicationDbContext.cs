using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<StoredFile> StoredFiles { get; set; }
        public DbSet<StoredFolder> StoredFolders { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(user => user.NormalizedEmail).IsUnique();

                entity.Property(user => user.Role)
                    .HasMaxLength(32)
                    .HasDefaultValue(ApplicationRole.User);

                entity.Property(user => user.IsActive)
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<StoredFile>(entity =>
            {
                entity.HasIndex(storedFile => storedFile.UserId);
                entity.HasIndex(storedFile => storedFile.FolderId);
                entity.HasIndex(storedFile => new { storedFile.UserId, storedFile.IsStarred });
                entity.HasIndex(storedFile => new { storedFile.ContainerName, storedFile.BlobName }).IsUnique();

                entity.Property(storedFile => storedFile.IsStarred)
                    .HasDefaultValue(false);

                entity.HasOne(storedFile => storedFile.User)
                    .WithMany(user => user.StoredFiles)
                    .HasForeignKey(storedFile => storedFile.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(storedFile => storedFile.Folder)
                    .WithMany(folder => folder.Files)
                    .HasForeignKey(storedFile => storedFile.FolderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StoredFolder>(entity =>
            {
                entity.HasIndex(storedFolder => storedFolder.UserId);
                entity.HasIndex(storedFolder => storedFolder.ParentFolderId);
                entity.HasIndex(storedFolder => new { storedFolder.UserId, storedFolder.ParentFolderId, storedFolder.Name });
                entity.HasIndex(storedFolder => new { storedFolder.UserId, storedFolder.IsStarred });

                entity.Property(storedFolder => storedFolder.IsStarred)
                    .HasDefaultValue(false);

                entity.HasOne(storedFolder => storedFolder.User)
                    .WithMany(user => user.StoredFolders)
                    .HasForeignKey(storedFolder => storedFolder.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(storedFolder => storedFolder.ParentFolder)
                    .WithMany(folder => folder.ChildFolders)
                    .HasForeignKey(storedFolder => storedFolder.ParentFolderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
