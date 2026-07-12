using System;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("backend.Models.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(320)
                        .HasColumnType("character varying(320)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("character varying(120)");

                    b.Property<string>("NormalizedEmail")
                        .IsRequired()
                        .HasMaxLength(320)
                        .HasColumnType("character varying(320)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasDefaultValue("User");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("backend.Models.StoredFile", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("BlobName")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<string>("ChecksumSha256")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("ContainerName")
                        .IsRequired()
                        .HasMaxLength(63)
                        .HasColumnType("character varying(63)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ETag")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Extension")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<Guid?>("FolderId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsStarred")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("OriginalName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<long>("SizeInBytes")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("VirtualPath")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.HasKey("Id");

                    b.HasIndex("ContainerName", "BlobName")
                        .IsUnique();

                    b.HasIndex("FolderId");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "IsStarred");

                    b.ToTable("StoredFiles");
                });

            modelBuilder.Entity("backend.Models.StoredFolder", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsStarred")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<Guid?>("ParentFolderId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ParentFolderId");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "ParentFolderId", "Name");

                    b.HasIndex("UserId", "IsStarred");

                    b.ToTable("StoredFolders");
                });

            modelBuilder.Entity("backend.Models.StoredFile", b =>
                {
                    b.HasOne("backend.Models.StoredFolder", "Folder")
                        .WithMany("Files")
                        .HasForeignKey("FolderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("backend.Models.ApplicationUser", "User")
                        .WithMany("StoredFiles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Folder");

                    b.Navigation("User");
                });

            modelBuilder.Entity("backend.Models.StoredFolder", b =>
                {
                    b.HasOne("backend.Models.StoredFolder", "ParentFolder")
                        .WithMany("ChildFolders")
                        .HasForeignKey("ParentFolderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("backend.Models.ApplicationUser", "User")
                        .WithMany("StoredFolders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ParentFolder");

                    b.Navigation("User");
                });

            modelBuilder.Entity("backend.Models.ApplicationUser", b =>
                {
                    b.Navigation("StoredFiles");

                    b.Navigation("StoredFolders");
                });

            modelBuilder.Entity("backend.Models.StoredFolder", b =>
                {
                    b.Navigation("ChildFolders");

                    b.Navigation("Files");
                });
#pragma warning restore 612, 618
        }
    }
}
