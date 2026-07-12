export type StoredFolder = {
  id: string
  userId: string
  parentFolderId: string | null
  name: string
  createdAt: string
  updatedAt: string | null
  deletedAt: string | null
  isStarred: boolean
}

export type StoredFile = {
  id: string
  userId: string
  folderId: string | null
  name: string
  originalName: string
  extension: string | null
  contentType: string
  sizeInBytes: number
  containerName: string
  blobName: string
  virtualPath: string | null
  eTag: string | null
  checksumSha256: string | null
  createdAt: string
  updatedAt: string | null
  deletedAt: string | null
  isStarred: boolean
}

export type DirectoryListing = {
  currentFolder: StoredFolder | null
  breadcrumbs: StoredFolder[]
  folders: StoredFolder[]
  files: StoredFile[]
}

export type TrashListing = {
  folders: StoredFolder[]
  files: StoredFile[]
}

export type StarredListing = {
  folders: StoredFolder[]
  files: StoredFile[]
}

export type CreateFolderRequest = {
  name: string
  parentFolderId?: string | null
}

export type CreateUploadUrlRequest = {
  name: string
  contentType?: string | null
  sizeInBytes: number
  virtualPath?: string | null
}

export type UploadUrlResponse = {
  uploadUrl: string
  method: "PUT"
  headers: Record<string, string>
  containerName: string
  blobName: string
  contentType: string
  sizeInBytes: number
  expiresAt: string
}

export type DownloadUrlResponse = {
  downloadUrl: string
  expiresAt: string
}

export type StorageUsageScope = {
  usedInBytes: number
  quotaInBytes: number
  usedPercentage: number
}

export type StorageUsage = {
  currentUser: StorageUsageScope
  system: StorageUsageScope | null
  isAdmin: boolean
}

export type FileCategoryStat = {
  count: number
  sizeInBytes: number
}

export type FileCategoryStats = {
  images: FileCategoryStat
  videos: FileCategoryStat
  documents: FileCategoryStat
  audio: FileCategoryStat
  others: FileCategoryStat
}

export type CreateFileMetadataRequest = {
  folderId?: string | null
  name: string
  originalName: string
  extension?: string | null
  contentType: string
  sizeInBytes: number
  containerName: string
  blobName: string
  virtualPath?: string | null
  eTag?: string | null
  checksumSha256?: string | null
}

export type UploadFileRequest = {
  file: File
  folderId?: string | null
  virtualPath?: string | null
  onProgress?: (progress: UploadProgress) => void
}

export type UploadProgress = {
  fileName: string
  uploadedBytes: number
  totalBytes: number
  percentage: number
}

export type SetStarredRequest = {
  isStarred: boolean
}
