using backend.Services;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly IStoredFileService _storedFileService;
        private readonly IStoredFolderService _storedFolderService;
        private readonly IStorageUsageService _storageUsageService;

        public FilesController(
            IBlobStorageService blobStorageService,
            IStoredFileService storedFileService,
            IStoredFolderService storedFolderService,
            IStorageUsageService storageUsageService)
        {
            _blobStorageService = blobStorageService;
            _storedFileService = storedFileService;
            _storedFolderService = storedFolderService;
            _storageUsageService = storageUsageService;
        }

        [HttpPost("upload-url")]
        public async Task<ActionResult<CreateUploadSasResponse>> CreateUploadUrl(
            [FromBody] CreateUploadSasRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                await _storageUsageService.EnsureUserCanStoreAsync(
                    currentUserId.Value,
                    request.SizeInBytes,
                    cancellationToken);

                var response = await _blobStorageService.CreateUploadSasAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (InvalidOperationException exception) when (exception.Message == "Storage quota exceeded.")
            {
                return BadRequest(new { error = "Limite de armazenamento excedido." });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<StoredFileResponse>> CreateMetadata(
            [FromBody] CreateStoredFileMetadataRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Unauthorized();
            }

            try
            {
                request.UserId = userId.Value;
                var storedFile = await _storedFileService.CreateMetadataAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = storedFile.Id }, StoredFileResponse.FromModel(storedFile));
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<StoredFileResponse>> Upload(
            [FromQuery] string name,
            [FromQuery] Guid? folderId,
            [FromQuery] string? virtualPath,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            if (Request.ContentLength is not > 0)
            {
                return BadRequest(new { error = "O tamanho do arquivo é obrigatório." });
            }

            BlobUploadResult? uploaded = null;
            var metadataCreated = false;
            try
            {
                await _storageUsageService.EnsureUserCanStoreAsync(
                    userId.Value,
                    Request.ContentLength.Value,
                    cancellationToken);

                var contentType = string.IsNullOrWhiteSpace(Request.ContentType)
                    ? "application/octet-stream"
                    : Request.ContentType;
                uploaded = await _blobStorageService.UploadAsync(
                    Request.Body,
                    name,
                    contentType,
                    virtualPath,
                    cancellationToken);

                var storedFile = await _storedFileService.CreateMetadataAsync(
                    new CreateStoredFileMetadataRequest
                    {
                        UserId = userId.Value,
                        FolderId = folderId,
                        Name = name,
                        OriginalName = name,
                        Extension = Path.GetExtension(name),
                        ContentType = contentType,
                        SizeInBytes = Request.ContentLength.Value,
                        ContainerName = uploaded.ContainerName,
                        BlobName = uploaded.BlobName,
                        VirtualPath = virtualPath,
                        ETag = uploaded.ETag
                    },
                    cancellationToken);
                metadataCreated = true;

                return CreatedAtAction(nameof(GetById), new { id = storedFile.Id }, StoredFileResponse.FromModel(storedFile));
            }
            catch (InvalidOperationException exception) when (exception.Message == "Storage quota exceeded.")
            {
                return BadRequest(new { error = "Limite de armazenamento excedido." });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
            finally
            {
                if (uploaded is not null && !metadataCreated)
                {
                    await _blobStorageService.DeleteBlobIfExistsAsync(
                        uploaded.ContainerName,
                        uploaded.BlobName,
                        CancellationToken.None);
                }
            }
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<StoredFileResponse>>> Search(
            [FromQuery] string? searchTerm,
            [FromQuery] string? virtualPath,
            [FromQuery] Guid? folderId,
            [FromQuery] Guid? userId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFiles = await _storedFileService.SearchAsync(
                searchTerm: searchTerm,
                virtualPath: virtualPath,
                folderId: folderId,
                userId: currentUserId.Value,
                isAdmin: false,
                skip: skip,
                take: take,
                cancellationToken: cancellationToken);

            return Ok(storedFiles.Select(StoredFileResponse.FromModel).ToList());
        }

        [HttpGet("browser")]
        public async Task<ActionResult<DirectoryListingResponse>> Browse(
            [FromQuery] Guid? folderId,
            [FromQuery] Guid? userId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                var folders = await _storedFolderService.ListChildrenAsync(
                    folderId,
                    currentUserId.Value,
                    false,
                    cancellationToken);
                var files = await _storedFileService.SearchAsync(
                    folderId: folderId,
                    userId: currentUserId.Value,
                    isAdmin: false,
                    skip: skip,
                    take: take,
                    cancellationToken: cancellationToken);
                var breadcrumbs = await _storedFolderService.GetBreadcrumbsAsync(
                    folderId,
                    currentUserId.Value,
                    false,
                    cancellationToken);

                return Ok(new DirectoryListingResponse
                {
                    CurrentFolder = breadcrumbs.LastOrDefault() is { } currentFolder
                        ? StoredFolderResponse.FromModel(currentFolder)
                        : null,
                    Breadcrumbs = breadcrumbs.Select(StoredFolderResponse.FromModel).ToList(),
                    Folders = folders.Select(StoredFolderResponse.FromModel).ToList(),
                    Files = files.Select(StoredFileResponse.FromModel).ToList()
                });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpGet("trash")]
        public async Task<ActionResult<TrashListingResponse>> ListTrash(
            [FromQuery] Guid? userId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                var folders = await _storedFolderService.ListTrashAsync(
                    currentUserId.Value,
                    false,
                    skip,
                    take,
                    cancellationToken);
                var files = await _storedFileService.ListTrashAsync(
                    currentUserId.Value,
                    false,
                    skip,
                    take,
                    cancellationToken);

                return Ok(new TrashListingResponse
                {
                    Folders = folders.Select(StoredFolderResponse.FromModel).ToList(),
                    Files = files.Select(StoredFileResponse.FromModel).ToList()
                });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpGet("starred")]
        public async Task<ActionResult<DirectoryListingResponse>> ListStarred(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                var folders = await _storedFolderService.ListStarredAsync(
                    currentUserId.Value,
                    false,
                    skip,
                    take,
                    cancellationToken);
                var files = await _storedFileService.ListStarredAsync(
                    currentUserId.Value,
                    false,
                    skip,
                    take,
                    cancellationToken);

                return Ok(new DirectoryListingResponse
                {
                    Folders = folders.Select(StoredFolderResponse.FromModel).ToList(),
                    Files = files.Select(StoredFileResponse.FromModel).ToList()
                });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpGet("storage-usage")]
        public async Task<ActionResult<StorageUsageResponse>> GetStorageUsage(CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            return Ok(await _storageUsageService.GetUsageAsync(
                currentUserId.Value,
                User.IsInRole(ApplicationRole.Admin),
                cancellationToken));
        }

        [HttpGet("category-stats")]
        public async Task<ActionResult<FileCategoryStatsResponse>> GetCategoryStats(
            [FromQuery] Guid? userId,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                return Ok(await _storedFileService.GetCategoryStatsAsync(
                    currentUserId.Value,
                    false,
                    cancellationToken));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpGet("folders")]
        public async Task<ActionResult<IReadOnlyList<StoredFolderResponse>>> ListFolders(
            [FromQuery] Guid? parentFolderId,
            [FromQuery] Guid? userId,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                var folders = await _storedFolderService.ListChildrenAsync(
                    parentFolderId,
                    currentUserId.Value,
                    false,
                    cancellationToken);

                return Ok(folders.Select(StoredFolderResponse.FromModel).ToList());
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost("folders")]
        public async Task<ActionResult<StoredFolderResponse>> CreateFolder(
            [FromBody] CreateStoredFolderRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                request.UserId = currentUserId.Value;
                var storedFolder = await _storedFolderService.CreateAsync(request, cancellationToken);

                return CreatedAtAction(nameof(Browse), new { folderId = storedFolder.Id }, StoredFolderResponse.FromModel(storedFolder));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPatch("folders/{id:guid}/name")]
        public async Task<ActionResult<StoredFolderResponse>> RenameFolder(
            Guid id,
            [FromBody] RenameStoredFolderRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                var storedFolder = await _storedFolderService.RenameAsync(
                    id,
                    currentUserId.Value,
                    false,
                    request.Name,
                    cancellationToken);

                return storedFolder is null ? NotFound() : Ok(StoredFolderResponse.FromModel(storedFolder));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPatch("folders/{id:guid}/trash")]
        public async Task<ActionResult<StoredFolderResponse>> MoveFolderToTrash(
            Guid id,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFolder = await _storedFolderService.MoveToTrashAsync(
                id,
                currentUserId.Value,
                false,
                cancellationToken);

            return storedFolder is null ? NotFound() : Ok(StoredFolderResponse.FromModel(storedFolder));
        }

        [HttpPatch("folders/{id:guid}/restore")]
        public async Task<ActionResult<StoredFolderResponse>> RestoreFolder(
            Guid id,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFolder = await _storedFolderService.RestoreAsync(
                id,
                currentUserId.Value,
                false,
                cancellationToken);

            return storedFolder is null ? NotFound() : Ok(StoredFolderResponse.FromModel(storedFolder));
        }

        [HttpPatch("folders/{id:guid}/starred")]
        public async Task<ActionResult<StoredFolderResponse>> SetFolderStarred(
            Guid id,
            [FromBody] SetStarredRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFolder = await _storedFolderService.SetStarredAsync(
                id,
                currentUserId.Value,
                false,
                request.IsStarred,
                cancellationToken);

            return storedFolder is null ? NotFound() : Ok(StoredFolderResponse.FromModel(storedFolder));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<StoredFileResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFile = await _storedFileService.GetByIdForUserAsync(
                id,
                currentUserId.Value,
                false,
                cancellationToken);

            return storedFile is null ? NotFound() : Ok(StoredFileResponse.FromModel(storedFile));
        }

        [HttpGet("{id:guid}/download-url")]
        public async Task<ActionResult<DownloadBlobSasResponse>> CreateDownloadUrl(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFile = await _storedFileService.GetByIdForUserAsync(
                id,
                currentUserId.Value,
                false,
                cancellationToken);

            if (storedFile is null)
            {
                return NotFound();
            }

            try
            {
                return Ok(_blobStorageService.CreateDownloadSas(
                    storedFile.BlobName,
                    storedFile.Name,
                    storedFile.ContentType));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpGet("{id:guid}/content")]
        public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFile = await _storedFileService.GetByIdForUserAsync(
                id,
                currentUserId.Value,
                false,
                cancellationToken);
            if (storedFile is null)
            {
                return NotFound();
            }

            var stream = await _blobStorageService.OpenReadAsync(
                storedFile.ContainerName,
                storedFile.BlobName,
                cancellationToken);
            return File(stream, storedFile.ContentType, storedFile.Name, enableRangeProcessing: true);
        }

        [HttpPatch("{id:guid}/name")]
        public async Task<ActionResult<StoredFileResponse>> Rename(
            Guid id,
            [FromBody] RenameStoredFileRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            try
            {
                var storedFile = await _storedFileService.RenameAsync(
                    id,
                    currentUserId.Value,
                    false,
                    request.Name,
                    cancellationToken);

                return storedFile is null ? NotFound() : Ok(StoredFileResponse.FromModel(storedFile));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPatch("{id:guid}/trash")]
        public async Task<ActionResult<StoredFileResponse>> MoveToTrash(
            Guid id,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFile = await _storedFileService.MoveToTrashAsync(
                id,
                currentUserId.Value,
                false,
                cancellationToken);

            return storedFile is null ? NotFound() : Ok(StoredFileResponse.FromModel(storedFile));
        }

        [HttpPatch("{id:guid}/restore")]
        public async Task<ActionResult<StoredFileResponse>> Restore(
            Guid id,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFile = await _storedFileService.RestoreAsync(
                id,
                currentUserId.Value,
                false,
                cancellationToken);

            return storedFile is null ? NotFound() : Ok(StoredFileResponse.FromModel(storedFile));
        }

        [HttpPatch("{id:guid}/starred")]
        public async Task<ActionResult<StoredFileResponse>> SetFileStarred(
            Guid id,
            [FromBody] SetStarredRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            var storedFile = await _storedFileService.SetStarredAsync(
                id,
                currentUserId.Value,
                false,
                request.IsStarred,
                cancellationToken);

            return storedFile is null ? NotFound() : Ok(StoredFileResponse.FromModel(storedFile));
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public class RenameStoredFileRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class RenameStoredFolderRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class SetStarredRequest
    {
        public bool IsStarred { get; set; }
    }

    public class DirectoryListingResponse
    {
        public StoredFolderResponse? CurrentFolder { get; set; }
        public IReadOnlyList<StoredFolderResponse> Breadcrumbs { get; set; } = [];
        public IReadOnlyList<StoredFolderResponse> Folders { get; set; } = [];
        public IReadOnlyList<StoredFileResponse> Files { get; set; } = [];
    }

    public class TrashListingResponse
    {
        public IReadOnlyList<StoredFolderResponse> Folders { get; set; } = [];
        public IReadOnlyList<StoredFileResponse> Files { get; set; } = [];
    }

    public class StoredFolderResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? ParentFolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsStarred { get; set; }

        public static StoredFolderResponse FromModel(StoredFolder storedFolder)
        {
            return new StoredFolderResponse
            {
                Id = storedFolder.Id,
                UserId = storedFolder.UserId,
                ParentFolderId = storedFolder.ParentFolderId,
                Name = storedFolder.Name,
                CreatedAt = storedFolder.CreatedAt,
                UpdatedAt = storedFolder.UpdatedAt,
                DeletedAt = storedFolder.DeletedAt,
                IsStarred = storedFolder.IsStarred
            };
        }
    }

    public class StoredFileResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public string? Extension { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public string ContainerName { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string? VirtualPath { get; set; }
        public string? ETag { get; set; }
        public string? ChecksumSha256 { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsStarred { get; set; }

        public static StoredFileResponse FromModel(StoredFile storedFile)
        {
            return new StoredFileResponse
            {
                Id = storedFile.Id,
                UserId = storedFile.UserId,
                FolderId = storedFile.FolderId,
                Name = storedFile.Name,
                OriginalName = storedFile.OriginalName,
                Extension = storedFile.Extension,
                ContentType = storedFile.ContentType,
                SizeInBytes = storedFile.SizeInBytes,
                ContainerName = storedFile.ContainerName,
                BlobName = storedFile.BlobName,
                VirtualPath = storedFile.VirtualPath,
                ETag = storedFile.ETag,
                ChecksumSha256 = storedFile.ChecksumSha256,
                CreatedAt = storedFile.CreatedAt,
                UpdatedAt = storedFile.UpdatedAt,
                DeletedAt = storedFile.DeletedAt,
                IsStarred = storedFile.IsStarred
            };
        }
    }
}
