import { apiRequest } from "~/lib/api/client"

import type {
  CreateFileMetadataRequest,
  CreateFolderRequest,
  DownloadUrlResponse,
  CreateUploadUrlRequest,
  DirectoryListing,
  FileCategoryStats,
  SetStarredRequest,
  StarredListing,
  StorageUsage,
  StoredFile,
  StoredFolder,
  TrashListing,
  UploadFileRequest,
  UploadProgress,
  UploadUrlResponse,
} from "./types"

const SIMPLE_UPLOAD_LIMIT_IN_BYTES = 100 * 1024 * 1024
const BLOCK_UPLOAD_CHUNK_SIZE_IN_BYTES = 8 * 1024 * 1024
const BLOCK_UPLOAD_MAX_RETRIES = 3

export const filesApi = {
  browse(folderId?: string | null) {
    const searchParams = new URLSearchParams()

    if (folderId) {
      searchParams.set("folderId", folderId)
    }

    const queryString = searchParams.toString()
    return apiRequest<DirectoryListing>(
      `/files/browser${queryString ? `?${queryString}` : ""}`
    )
  },
  createFolder(payload: CreateFolderRequest) {
    return apiRequest<StoredFolder>("/files/folders", {
      method: "POST",
      body: payload,
    })
  },
  listTrash() {
    return apiRequest<TrashListing>("/files/trash")
  },
  listStarred() {
    return apiRequest<StarredListing>("/files/starred")
  },
  moveFileToTrash(fileId: string) {
    return apiRequest<StoredFile>(`/files/${encodeURIComponent(fileId)}/trash`, {
      method: "PATCH",
    })
  },
  restoreFile(fileId: string) {
    return apiRequest<StoredFile>(
      `/files/${encodeURIComponent(fileId)}/restore`,
      {
        method: "PATCH",
      }
    )
  },
  setFileStarred(fileId: string, payload: SetStarredRequest) {
    return apiRequest<StoredFile>(
      `/files/${encodeURIComponent(fileId)}/starred`,
      {
        method: "PATCH",
        body: payload,
      }
    )
  },
  moveFolderToTrash(folderId: string) {
    return apiRequest<StoredFolder>(
      `/files/folders/${encodeURIComponent(folderId)}/trash`,
      {
        method: "PATCH",
      }
    )
  },
  restoreFolder(folderId: string) {
    return apiRequest<StoredFolder>(
      `/files/folders/${encodeURIComponent(folderId)}/restore`,
      {
        method: "PATCH",
      }
    )
  },
  setFolderStarred(folderId: string, payload: SetStarredRequest) {
    return apiRequest<StoredFolder>(
      `/files/folders/${encodeURIComponent(folderId)}/starred`,
      {
        method: "PATCH",
        body: payload,
      }
    )
  },
  createUploadUrl(payload: CreateUploadUrlRequest) {
    return apiRequest<UploadUrlResponse>("/files/upload-url", {
      method: "POST",
      body: payload,
    })
  },
  createMetadata(payload: CreateFileMetadataRequest) {
    return apiRequest<StoredFile>("/files", {
      method: "POST",
      body: payload,
    })
  },
  createDownloadUrl(fileId: string) {
    return Promise.resolve<DownloadUrlResponse>({
      downloadUrl: `/api/files/${encodeURIComponent(fileId)}/content`,
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    })
  },
  getStorageUsage() {
    return apiRequest<StorageUsage>("/files/storage-usage")
  },
  getCategoryStats() {
    return apiRequest<FileCategoryStats>("/files/category-stats")
  },
  async uploadFile({
    file,
    folderId,
    virtualPath,
    onProgress,
  }: UploadFileRequest) {
    emitUploadProgress(onProgress, file, 0)
    return uploadFileThroughApi(file, folderId, virtualPath, onProgress)
  },
}

function uploadFileThroughApi(
  file: File,
  folderId?: string | null,
  virtualPath?: string | null,
  onProgress?: (progress: UploadProgress) => void
): Promise<StoredFile> {
  return new Promise((resolve, reject) => {
    const params = new URLSearchParams({ name: file.name })
    if (folderId) params.set("folderId", folderId)
    if (virtualPath) params.set("virtualPath", virtualPath)

    const request = new XMLHttpRequest()
    request.open("POST", `/api/files/upload?${params}`)
    request.withCredentials = true
    request.setRequestHeader("Content-Type", file.type || "application/octet-stream")
    request.upload.onprogress = (event) => {
      if (event.lengthComputable) emitUploadProgress(onProgress, file, event.loaded)
    }
    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        emitUploadProgress(onProgress, file, file.size)
        resolve(JSON.parse(request.responseText) as StoredFile)
        return
      }
      reject(new Error(request.responseText || request.statusText || "Falha ao enviar arquivo."))
    }
    request.onerror = () => reject(new Error("Falha ao enviar arquivo."))
    request.send(file)
  })
}

type BlobUploadResult = {
  eTag: string | null
}

async function uploadFileWithSinglePut(
  upload: UploadUrlResponse,
  file: File,
  onProgress?: (progress: UploadProgress) => void
): Promise<BlobUploadResult> {
  return uploadBlobWithXmlHttpRequest(upload, file, onProgress)
}

function uploadBlobWithXmlHttpRequest(
  upload: UploadUrlResponse,
  file: File,
  onProgress?: (progress: UploadProgress) => void
): Promise<BlobUploadResult> {
  return new Promise((resolve, reject) => {
    const request = new XMLHttpRequest()

    request.open(upload.method, upload.uploadUrl)

    Object.entries(upload.headers).forEach(([header, value]) => {
      request.setRequestHeader(header, value)
    })

    request.upload.onprogress = (event) => {
      if (!event.lengthComputable) {
        return
      }

      emitUploadProgress(onProgress, file, event.loaded)
    }

    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        emitUploadProgress(onProgress, file, file.size)
        resolve({ eTag: request.getResponseHeader("ETag") })
        return
      }

      reject(new Error(getBlobErrorMessage(request.responseText, request.statusText)))
    }

    request.onerror = () => {
      reject(new Error("Falha ao enviar arquivo."))
    }

    request.send(file)
  })
}

async function uploadFileInBlocks(
  upload: UploadUrlResponse,
  file: File,
  onProgress?: (progress: UploadProgress) => void
): Promise<BlobUploadResult> {
  const blockIds = createBlockIds(file.size)
  let uploadedBytes = 0

  for (let index = 0; index < blockIds.length; index += 1) {
    const start = index * BLOCK_UPLOAD_CHUNK_SIZE_IN_BYTES
    const end = Math.min(start + BLOCK_UPLOAD_CHUNK_SIZE_IN_BYTES, file.size)
    const chunk = file.slice(start, end)

    await uploadBlockWithRetry(upload.uploadUrl, blockIds[index], chunk)
    uploadedBytes += chunk.size
    emitUploadProgress(onProgress, file, uploadedBytes)
  }

  const commitResponse = await fetch(createCommitBlockListUrl(upload.uploadUrl), {
    method: "PUT",
    headers: {
      "Content-Type": "application/xml",
      "x-ms-blob-content-type": upload.contentType || "application/octet-stream",
    },
    body: createBlockListXml(blockIds),
  })

  await ensureBlobResponseOk(commitResponse)
  emitUploadProgress(onProgress, file, file.size)

  return {
    eTag: commitResponse.headers.get("ETag"),
  }
}

async function uploadBlockWithRetry(
  uploadUrl: string,
  blockId: string,
  chunk: Blob
) {
  for (let attempt = 1; attempt <= BLOCK_UPLOAD_MAX_RETRIES; attempt += 1) {
    const response = await fetch(createPutBlockUrl(uploadUrl, blockId), {
      method: "PUT",
      headers: {
        "Content-Type": "application/octet-stream",
      },
      body: chunk,
    })

    if (response.ok) {
      return
    }

    if (attempt === BLOCK_UPLOAD_MAX_RETRIES) {
      await ensureBlobResponseOk(response)
    }

    await delay(500 * attempt)
  }
}

function createBlockIds(fileSize: number) {
  const blockCount = Math.ceil(fileSize / BLOCK_UPLOAD_CHUNK_SIZE_IN_BYTES)

  return Array.from({ length: blockCount }, (_, index) =>
    createBlockId(index)
  )
}

function createBlockId(index: number) {
  return btoa(`block-${index.toString().padStart(8, "0")}`)
}

function createPutBlockUrl(uploadUrl: string, blockId: string) {
  return `${uploadUrl}&comp=block&blockid=${encodeURIComponent(blockId)}`
}

function createCommitBlockListUrl(uploadUrl: string) {
  return `${uploadUrl}&comp=blocklist`
}

function createBlockListXml(blockIds: string[]) {
  return `<?xml version="1.0" encoding="utf-8"?><BlockList>${blockIds
    .map((blockId) => `<Latest>${blockId}</Latest>`)
    .join("")}</BlockList>`
}

async function ensureBlobResponseOk(response: Response) {
  if (response.ok) {
    return
  }

  const responseText = await response.text().catch(() => "")
  throw new Error(getBlobErrorMessage(responseText, response.statusText))
}

function emitUploadProgress(
  onProgress: ((progress: UploadProgress) => void) | undefined,
  file: File,
  uploadedBytes: number
) {
  onProgress?.({
    fileName: file.name,
    uploadedBytes: Math.min(uploadedBytes, file.size),
    totalBytes: file.size,
    percentage:
      file.size > 0 ? Math.min((uploadedBytes / file.size) * 100, 100) : 0,
  })
}

function getBlobErrorMessage(responseText: string, statusText: string) {
  return (
    responseText.match(/<Message>([\s\S]*?)<\/Message>/)?.[1]?.trim() ||
    statusText ||
    "Falha ao enviar arquivo."
  )
}

function delay(milliseconds: number) {
  return new Promise((resolve) => window.setTimeout(resolve, milliseconds))
}

function getFileExtension(fileName: string) {
  const extensionStart = fileName.lastIndexOf(".")

  if (extensionStart <= 0 || extensionStart === fileName.length - 1) {
    return null
  }

  return fileName.slice(extensionStart)
}
