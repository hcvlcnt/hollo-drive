package com.hlc.hollo_app.data.remote

import kotlinx.serialization.Serializable
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Body
import retrofit2.http.POST
import retrofit2.http.PATCH
import retrofit2.http.Path
import retrofit2.http.Query
import okhttp3.RequestBody

@Serializable
data class StoredFolder(
    val id: String,
    val userId: String,
    val parentFolderId: String? = null,
    val name: String,
    val createdAt: String,
    val updatedAt: String? = null,
    val deletedAt: String? = null,
    val isStarred: Boolean = false,
)

@Serializable
data class StoredFile(
    val id: String,
    val name: String,
    val extension: String? = null,
    val contentType: String,
    val sizeInBytes: Long,
    val isStarred: Boolean = false,
)

@Serializable
data class DirectoryListing(
    val currentFolder: StoredFolder? = null,
    val breadcrumbs: List<StoredFolder> = emptyList(),
    val folders: List<StoredFolder> = emptyList(),
    val files: List<StoredFile> = emptyList(),
)

@Serializable
data class FolderListing(
    val folders: List<StoredFolder> = emptyList(),
    val files: List<StoredFile> = emptyList(),
)

@Serializable
data class CreateFolderRequest(
    val name: String,
    val parentFolderId: String? = null,
)

@Serializable
data class CreateUploadUrlRequest(
    val name: String,
    val contentType: String,
    val sizeInBytes: Long,
)

@Serializable
data class UploadUrlResponse(
    val uploadUrl: String,
    val method: String,
    val headers: Map<String, String>,
    val containerName: String,
    val blobName: String,
    val contentType: String,
    val sizeInBytes: Long,
    val expiresAt: String,
)

@Serializable
data class CreateFileMetadataRequest(
    val folderId: String? = null,
    val name: String,
    val originalName: String,
    val extension: String? = null,
    val contentType: String,
    val sizeInBytes: Long,
    val containerName: String,
    val blobName: String,
    val eTag: String? = null,
)

interface FilesApi {
    @PATCH("files/{id}/trash")
    suspend fun moveFileToTrash(@Path("id") id: String): Response<StoredFile>

    @PATCH("files/folders/{id}/trash")
    suspend fun moveFolderToTrash(@Path("id") id: String): Response<StoredFolder>

    @POST("files/folders")
    suspend fun createFolder(@Body request: CreateFolderRequest): Response<StoredFolder>

    @POST("files/upload")
    suspend fun upload(
        @Query("name") name: String,
        @Query("folderId") folderId: String? = null,
        @Body body: RequestBody,
    ): Response<StoredFile>

    @POST("files/upload-url")
    suspend fun createUploadUrl(@Body request: CreateUploadUrlRequest): Response<UploadUrlResponse>

    @POST("files")
    suspend fun createMetadata(@Body request: CreateFileMetadataRequest): Response<StoredFile>

    @GET("files/browser")
    suspend fun browse(@Query("folderId") folderId: String? = null): Response<DirectoryListing>

    @GET("files/starred")
    suspend fun starred(): Response<FolderListing>

    @GET("files/trash")
    suspend fun trash(): Response<FolderListing>
}
