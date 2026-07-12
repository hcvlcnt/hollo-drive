package com.hlc.hollo_app.data

import android.content.Context
import android.net.Uri
import android.provider.OpenableColumns
import com.hlc.hollo_app.data.remote.DirectoryListing
import com.hlc.hollo_app.data.remote.CreateFolderRequest
import com.hlc.hollo_app.data.remote.FilesApi
import com.hlc.hollo_app.data.remote.FolderListing
import com.hlc.hollo_app.data.remote.StoredFile
import com.hlc.hollo_app.data.remote.StoredFolder
import java.io.IOException
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody
import okio.BufferedSink
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class FilesException(val isNetworkError: Boolean) : Exception()

interface FilesRepository {
    suspend fun browse(folderId: String?): DirectoryListing
    suspend fun starred(): FolderListing
    suspend fun trash(): FolderListing
    suspend fun createFolder(name: String, parentFolderId: String?): StoredFolder
    suspend fun moveFileToTrash(id: String): StoredFile
    suspend fun moveFolderToTrash(id: String): StoredFolder
    suspend fun upload(uri: Uri, folderId: String?, onProgress: (Int) -> Unit): StoredFile
}

class DefaultFilesRepository(
    private val api: FilesApi,
    private val context: Context,
) : FilesRepository {
    override suspend fun browse(folderId: String?) = request { api.browse(folderId) }
    override suspend fun starred() = request { api.starred() }
    override suspend fun trash() = request { api.trash() }
    override suspend fun createFolder(name: String, parentFolderId: String?) =
        request { api.createFolder(CreateFolderRequest(name.trim(), parentFolderId)) }
    override suspend fun moveFileToTrash(id: String) = request { api.moveFileToTrash(id) }
    override suspend fun moveFolderToTrash(id: String) = request { api.moveFolderToTrash(id) }

    override suspend fun upload(uri: Uri, folderId: String?, onProgress: (Int) -> Unit): StoredFile = withContext(Dispatchers.IO) {
        val source = readSource(uri)
        val body = object : RequestBody() {
            override fun contentType() = source.contentType.toMediaType()
            override fun contentLength() = source.size

            override fun writeTo(sink: BufferedSink) {
                context.contentResolver.openInputStream(uri)?.use { input ->
                    val buffer = ByteArray(DEFAULT_BUFFER_SIZE)
                    var uploaded = 0L
                    while (true) {
                        val count = input.read(buffer)
                        if (count < 0) break
                        sink.write(buffer, 0, count)
                        uploaded += count
                        onProgress(((uploaded * 100) / source.size.coerceAtLeast(1)).toInt().coerceIn(0, 100))
                    }
                } ?: throw IOException("Unable to open selected file")
            }
        }
        val storedFile = request { api.upload(source.name, folderId, body) }
        onProgress(100)
        storedFile
    }

    private suspend fun <T> request(call: suspend () -> retrofit2.Response<T>): T = try {
        val response = call()
        if (!response.isSuccessful) throw FilesException(false)
        response.body() ?: throw FilesException(false)
    } catch (error: FilesException) {
        throw error
    } catch (_: IOException) {
        throw FilesException(true)
    } catch (_: Exception) {
        throw FilesException(false)
    }

    private fun readSource(uri: Uri): UploadSource {
        var name: String? = null
        var size: Long? = null
        context.contentResolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE), null, null, null)?.use { cursor ->
            if (cursor.moveToFirst()) {
                name = cursor.getString(cursor.getColumnIndexOrThrow(OpenableColumns.DISPLAY_NAME))
                val sizeIndex = cursor.getColumnIndex(OpenableColumns.SIZE)
                if (sizeIndex >= 0 && !cursor.isNull(sizeIndex)) size = cursor.getLong(sizeIndex)
            }
        }
        val resolvedSize = size ?: context.contentResolver.openAssetFileDescriptor(uri, "r")?.use { it.length }
        if (resolvedSize == null || resolvedSize < 0) throw FilesException(false)
        return UploadSource(
            name = name?.takeIf { it.isNotBlank() } ?: "arquivo",
            size = resolvedSize,
            contentType = context.contentResolver.getType(uri) ?: "application/octet-stream",
        )
    }

    private data class UploadSource(val name: String, val size: Long, val contentType: String)
}
