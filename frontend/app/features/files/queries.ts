import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { filesApi } from "./api"

import type {
  CreateFolderRequest,
  SetStarredRequest,
  UploadFileRequest,
} from "./types"

export const fileKeys = {
  all: ["files"] as const,
  browser: (folderId?: string | null) =>
    [...fileKeys.all, "browser", folderId ?? "root"] as const,
  trash: () => [...fileKeys.all, "trash"] as const,
  starred: () => [...fileKeys.all, "starred"] as const,
  storageUsage: () => [...fileKeys.all, "storage-usage"] as const,
  categoryStats: () => [...fileKeys.all, "category-stats"] as const,
}

export function useDirectoryListingQuery(
  folderId?: string | null,
  enabled = true
) {
  return useQuery({
    queryKey: fileKeys.browser(folderId),
    queryFn: () => filesApi.browse(folderId),
    enabled,
  })
}

export function useCreateFolderMutation(folderId?: string | null) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateFolderRequest) =>
      filesApi.createFolder(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.browser(folderId) })
    },
  })
}

export function useTrashListingQuery(enabled = true) {
  return useQuery({
    queryKey: fileKeys.trash(),
    queryFn: filesApi.listTrash,
    enabled,
  })
}

export function useStarredListingQuery(enabled = true) {
  return useQuery({
    queryKey: fileKeys.starred(),
    queryFn: filesApi.listStarred,
    enabled,
  })
}

export function useMoveFileToTrashMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (fileId: string) => filesApi.moveFileToTrash(fileId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all })
    },
  })
}

export function useRestoreFileMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (fileId: string) => filesApi.restoreFile(fileId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all })
    },
  })
}

export function useSetFileStarredMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      fileId,
      payload,
    }: {
      fileId: string
      payload: SetStarredRequest
    }) => filesApi.setFileStarred(fileId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all })
    },
  })
}

export function useMoveFolderToTrashMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (folderId: string) => filesApi.moveFolderToTrash(folderId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all })
    },
  })
}

export function useRestoreFolderMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (folderId: string) => filesApi.restoreFolder(folderId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all })
    },
  })
}

export function useSetFolderStarredMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      folderId,
      payload,
    }: {
      folderId: string
      payload: SetStarredRequest
    }) => filesApi.setFolderStarred(folderId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all })
    },
  })
}

export function useUploadFileMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: UploadFileRequest | UploadFileRequest[]) => {
      const uploads = Array.isArray(payload) ? payload : [payload]
      return Promise.all(uploads.map((upload) => filesApi.uploadFile(upload)))
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all })
    },
  })
}

export function useDownloadFileMutation() {
  return useMutation({
    mutationFn: (fileId: string) => filesApi.createDownloadUrl(fileId),
  })
}

export function useStorageUsageQuery(enabled = true) {
  return useQuery({
    queryKey: fileKeys.storageUsage(),
    queryFn: filesApi.getStorageUsage,
    enabled,
  })
}

export function useFileCategoryStatsQuery(enabled = true) {
  return useQuery({
    queryKey: fileKeys.categoryStats(),
    queryFn: filesApi.getCategoryStats,
    enabled,
  })
}
