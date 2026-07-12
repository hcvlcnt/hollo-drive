using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace backend.Services
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private static readonly BlobClientOptions ClientOptions = new(
            BlobClientOptions.ServiceVersion.V2023_11_03);

        private readonly AzureBlobStorageOptions _options;

        public AzureBlobStorageService(IOptions<AzureBlobStorageOptions> options)
        {
            _options = options.Value;
        }

        public async Task<CreateUploadSasResponse> CreateUploadSasAsync(
            CreateUploadSasRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateConfiguration();
            ValidateUploadRequest(request);

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.UploadSasExpirationMinutes);
            var contentType = NormalizeOptional(request.ContentType) ?? "application/octet-stream";
            var blobName = GenerateBlobName(request.Name, request.VirtualPath);
            var credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
            var internalBlobServiceClient = new BlobServiceClient(
                GetBlobServiceUri(),
                credential,
                ClientOptions);
            var containerClient = internalBlobServiceClient.GetBlobContainerClient(_options.ContainerName);
            var publicBlobServiceClient = new BlobServiceClient(
                GetPublicBlobServiceUri(),
                credential,
                ClientOptions);
            var blobClient = publicBlobServiceClient
                .GetBlobContainerClient(_options.ContainerName)
                .GetBlobClient(blobName);

            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            await EnsureCorsRulesAsync(internalBlobServiceClient, cancellationToken);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = expiresAt
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            var response = new CreateUploadSasResponse
            {
                UploadUrl = blobClient.GenerateSasUri(sasBuilder).ToString(),
                ContainerName = _options.ContainerName,
                BlobName = blobName,
                ContentType = contentType,
                SizeInBytes = request.SizeInBytes,
                ExpiresAt = expiresAt,
                Headers = new Dictionary<string, string>
                {
                    ["x-ms-blob-type"] = "BlockBlob",
                    ["Content-Type"] = contentType
                }
            };

            return response;
        }

        public DownloadBlobSasResponse CreateDownloadSas(
            string blobName,
            string fileName,
            string? contentType = null)
        {
            ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Blob name is required.", nameof(blobName));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.UploadSasExpirationMinutes);
            var credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
            var publicBlobServiceClient = new BlobServiceClient(
                GetPublicBlobServiceUri(),
                credential,
                ClientOptions);
            var blobClient = publicBlobServiceClient
                .GetBlobContainerClient(_options.ContainerName)
                .GetBlobClient(blobName.Trim());

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = blobName.Trim(),
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = expiresAt,
                ContentDisposition = CreateAttachmentContentDisposition(fileName),
                ContentType = NormalizeOptional(contentType) ?? "application/octet-stream"
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return new DownloadBlobSasResponse
            {
                DownloadUrl = blobClient.GenerateSasUri(sasBuilder).ToString(),
                ExpiresAt = expiresAt
            };
        }

        public async Task<BlobUploadResult> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            string? virtualPath = null,
            CancellationToken cancellationToken = default)
        {
            ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            var credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
            var serviceClient = new BlobServiceClient(GetBlobServiceUri(), credential, ClientOptions);
            var containerClient = serviceClient.GetBlobContainerClient(_options.ContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobName = GenerateBlobName(fileName, virtualPath);
            var blobClient = containerClient.GetBlobClient(blobName);
            var response = await blobClient.UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = NormalizeOptional(contentType) ?? "application/octet-stream"
                    }
                },
                cancellationToken);

            return new BlobUploadResult(_options.ContainerName, blobName, response.Value.ETag.ToString());
        }

        public async Task<Stream> OpenReadAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            ValidateConfiguration();
            var credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
            var serviceClient = new BlobServiceClient(GetBlobServiceUri(), credential, ClientOptions);
            var blobClient = serviceClient
                .GetBlobContainerClient(containerName.Trim())
                .GetBlobClient(blobName.Trim());

            return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
        }

        public async Task DeleteBlobIfExistsAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Container name is required.", nameof(containerName));
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Blob name is required.", nameof(blobName));
            }

            var credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
            var blobServiceClient = new BlobServiceClient(
                GetBlobServiceUri(),
                credential,
                ClientOptions);
            var blobClient = blobServiceClient
                .GetBlobContainerClient(containerName.Trim())
                .GetBlobClient(blobName.Trim());

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        private async Task EnsureCorsRulesAsync(
            BlobServiceClient blobServiceClient,
            CancellationToken cancellationToken)
        {
            var allowedOrigins = _options.CorsAllowedOrigins
                .Select(NormalizeOptional)
                .Where(origin => origin is not null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (allowedOrigins.Length == 0)
            {
                return;
            }

            var properties = await blobServiceClient.GetPropertiesAsync(cancellationToken);
            var allowedOriginsValue = string.Join(",", allowedOrigins);
            var existingRule = properties.Value.Cors.FirstOrDefault(rule =>
                string.Equals(rule.AllowedOrigins, allowedOriginsValue, StringComparison.OrdinalIgnoreCase));

            if (existingRule is not null &&
                existingRule.AllowedMethods.Contains("PUT", StringComparison.OrdinalIgnoreCase) &&
                existingRule.AllowedHeaders == "*")
            {
                return;
            }

            properties.Value.Cors.Clear();
            properties.Value.Cors.Add(new BlobCorsRule
            {
                AllowedOrigins = allowedOriginsValue,
                AllowedMethods = "PUT,GET,HEAD,OPTIONS",
                AllowedHeaders = "*",
                ExposedHeaders = "ETag,x-ms-request-id,x-ms-version",
                MaxAgeInSeconds = 3600
            });

            await blobServiceClient.SetPropertiesAsync(properties.Value, cancellationToken);
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.AccountName))
            {
                throw new InvalidOperationException("Azure blob storage account name is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.AccountKey))
            {
                throw new InvalidOperationException("Azure blob storage account key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.ContainerName))
            {
                throw new InvalidOperationException("Azure blob storage container name is not configured.");
            }

            if (_options.UploadSasExpirationMinutes <= 0)
            {
                throw new InvalidOperationException("Azure blob storage SAS expiration must be greater than zero.");
            }

            if (_options.MaxUploadSizeInBytes <= 0)
            {
                throw new InvalidOperationException("Azure blob storage max upload size must be greater than zero.");
            }
        }

        private Uri GetBlobServiceUri()
        {
            if (!string.IsNullOrWhiteSpace(_options.BlobServiceUri))
            {
                return new Uri(_options.BlobServiceUri);
            }

            return new Uri($"https://{_options.AccountName}.blob.core.windows.net");
        }

        private Uri GetPublicBlobServiceUri()
        {
            if (!string.IsNullOrWhiteSpace(_options.PublicBlobServiceUri))
            {
                return new Uri(_options.PublicBlobServiceUri);
            }

            return GetBlobServiceUri();
        }

        private void ValidateUploadRequest(CreateUploadSasRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("File name is required.", nameof(request.Name));
            }

            if (request.SizeInBytes <= 0)
            {
                throw new ArgumentException("O arquivo precisa ter mais de 0 B.");
            }

            if (request.SizeInBytes > _options.MaxUploadSizeInBytes)
            {
                throw new ArgumentException(
                    $"O arquivo excede o limite de upload de {FormatFileSize(_options.MaxUploadSizeInBytes)}.");
            }
        }

        private static string FormatFileSize(long sizeInBytes)
        {
            if (sizeInBytes < 1024)
            {
                return $"{sizeInBytes} B";
            }

            var units = new[] { "KB", "MB", "GB", "TB" };
            var size = sizeInBytes / 1024d;
            var unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex += 1;
            }

            return $"{size:0.#} {units[unitIndex]}";
        }

        private static string GenerateBlobName(string fileName, string? virtualPath)
        {
            var extension = Path.GetExtension(fileName);
            var prefix = NormalizeVirtualPath(virtualPath);
            var generatedName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";

            return string.IsNullOrWhiteSpace(prefix) ? generatedName : $"{prefix}/{generatedName}";
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeVirtualPath(string? virtualPath)
        {
            var normalizedVirtualPath = NormalizeOptional(virtualPath);

            if (normalizedVirtualPath is null)
            {
                return null;
            }

            return string.Join(
                '/',
                normalizedVirtualPath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(SanitizePathSegment)
                    .Where(segment => segment.Length > 0));
        }

        private static string SanitizePathSegment(string segment)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(segment.Where(character => !invalidChars.Contains(character)));
        }

        private static string CreateAttachmentContentDisposition(string fileName)
        {
            var sanitizedFileName = SanitizeDownloadFileName(fileName);
            var escapedFileName = sanitizedFileName.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var encodedFileName = Uri.EscapeDataString(sanitizedFileName);

            return $"attachment; filename=\"{escapedFileName}\"; filename*=UTF-8''{encodedFileName}";
        }

        private static string SanitizeDownloadFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitizedFileName = string.Concat(
                fileName.Trim().Select(character => invalidChars.Contains(character) ? '_' : character));

            return string.IsNullOrWhiteSpace(sanitizedFileName) ? "download" : sanitizedFileName;
        }
    }
}
