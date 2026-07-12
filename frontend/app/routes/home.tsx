import { useEffect, useRef, useState } from "react"
import { useNavigate } from "react-router"
import { Loader2, Menu, Star, Trash2, X } from "lucide-react"
import { toast } from "sonner"

import { FileGrid } from "~/components/file-manager/file-grid"
import { Header } from "~/components/file-manager/header"
import { QuickStats } from "~/components/file-manager/quick-stats"
import { Sidebar } from "~/components/file-manager/sidebar"
import { Toolbar } from "~/components/file-manager/toolbar"
import { Progress } from "~/components/ui/progress"
import { useCurrentUserQuery } from "~/features/auth/queries"
import {
  useCreateFolderMutation,
  useDirectoryListingQuery,
  useDownloadFileMutation,
  useFileCategoryStatsQuery,
  useMoveFileToTrashMutation,
  useMoveFolderToTrashMutation,
  useRestoreFileMutation,
  useRestoreFolderMutation,
  useSetFileStarredMutation,
  useSetFolderStarredMutation,
  useStarredListingQuery,
  useTrashListingQuery,
  useUploadFileMutation,
} from "~/features/files/queries"
import { cn } from "~/lib/utils"

import type { FileItem } from "~/components/file-manager/file-grid"
import type {
  StoredFile,
  StoredFolder,
  UploadProgress,
} from "~/features/files/types"

type UploadProgressItem = UploadProgress & {
  id: string
}

export default function HomePage() {
  const navigate = useNavigate()
  const currentUserQuery = useCurrentUserQuery()
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid")
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [activeView, setActiveView] = useState<"drive" | "starred" | "trash">(
    "drive"
  )
  const [currentFolderId, setCurrentFolderId] = useState<string | null>(null)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [uploadProgressItems, setUploadProgressItems] = useState<
    UploadProgressItem[]
  >([])
  const fileInputRef = useRef<HTMLInputElement>(null)
  const directoryQuery = useDirectoryListingQuery(
    currentFolderId,
    Boolean(currentUserQuery.data) && activeView === "drive"
  )
  const fileCategoryStatsQuery = useFileCategoryStatsQuery(
    Boolean(currentUserQuery.data)
  )
  const trashQuery = useTrashListingQuery(
    Boolean(currentUserQuery.data) && activeView === "trash"
  )
  const starredQuery = useStarredListingQuery(
    Boolean(currentUserQuery.data) && activeView === "starred"
  )
  const createFolderMutation = useCreateFolderMutation(currentFolderId)
  const uploadFileMutation = useUploadFileMutation()
  const downloadFileMutation = useDownloadFileMutation()
  const moveFileToTrashMutation = useMoveFileToTrashMutation()
  const moveFolderToTrashMutation = useMoveFolderToTrashMutation()
  const restoreFileMutation = useRestoreFileMutation()
  const restoreFolderMutation = useRestoreFolderMutation()
  const setFileStarredMutation = useSetFileStarredMutation()
  const setFolderStarredMutation = useSetFolderStarredMutation()

  useEffect(() => {
    if (currentUserQuery.isError) {
      navigate("/login", { replace: true })
    }
  }, [currentUserQuery.isError, navigate])

  if (currentUserQuery.isLoading) {
    return (
      <div className="flex h-screen items-center justify-center bg-background text-muted-foreground">
        <Loader2 className="size-5 animate-spin" />
      </div>
    )
  }

  if (!currentUserQuery.data) {
    return null
  }

  const user = currentUserQuery.data
  const directory = activeView === "drive" ? directoryQuery.data : null
  const trash = activeView === "trash" ? trashQuery.data : null
  const starred = activeView === "starred" ? starredQuery.data : null
  const items = directory
    ? mapDirectoryToItems(directory.folders, directory.files)
    : trash
      ? mapDirectoryToItems(trash.folders, trash.files, true)
      : starred
        ? mapDirectoryToItems(starred.folders, starred.files)
        : []
  const breadcrumb = [
    { id: null, name: "Meu Drive" },
    ...(directory?.breadcrumbs.map((folder) => ({
      id: folder.id,
      name: folder.name,
    })) ?? []),
  ]

  function handleCreateFolder() {
    const name = window.prompt("Nome da pasta")

    if (!name?.trim()) {
      return
    }

    createFolderMutation.mutate({
      name: name.trim(),
      parentFolderId: currentFolderId,
    })
  }

  function handleOpenDrive() {
    setActiveView("drive")
    setSidebarOpen(false)
  }

  function handleOpenTrash() {
    setActiveView("trash")
    setCurrentFolderId(null)
    setSidebarOpen(false)
  }

  function handleOpenStarred() {
    setActiveView("starred")
    setCurrentFolderId(null)
    setSidebarOpen(false)
  }

  function handleOpenAdmin() {
    navigate("/admin")
  }

  function handleUploadClick() {
    setUploadError(null)
    fileInputRef.current?.click()
  }

  function handleFileSelection(event: React.ChangeEvent<HTMLInputElement>) {
    const selectedFiles = Array.from(event.currentTarget.files ?? [])
    event.currentTarget.value = ""

    if (selectedFiles.length === 0) {
      return
    }

    setUploadProgressItems(
      selectedFiles.map((file, index) => ({
        id: getUploadProgressId(file, index),
        fileName: file.name,
        uploadedBytes: 0,
        totalBytes: file.size,
        percentage: 0,
      }))
    )

    uploadFileMutation.mutate(
      selectedFiles.map((file, index) => ({
        file,
        folderId: currentFolderId,
        virtualPath: getCurrentVirtualPath(directory?.breadcrumbs ?? []),
        onProgress: (progress) => {
          const id = getUploadProgressId(file, index)

          setUploadProgressItems((currentItems) =>
            currentItems.map((item) =>
              item.id === id ? { ...progress, id } : item
            )
          )
        },
      })),
      {
        onSuccess: () => {
          setUploadError(null)
          setUploadProgressItems([])
          toast.success(getUploadSuccessMessage(selectedFiles.length))
        },
        onError: (error) => {
          setUploadProgressItems([])
          setUploadError(
            error instanceof Error ? error.message : "Falha ao enviar arquivo."
          )
        },
      }
    )
  }

  function handleFileDownload(file: FileItem) {
    downloadFileMutation.mutate(file.id, {
      onSuccess: ({ downloadUrl }) => {
        const anchor = document.createElement("a")
        anchor.href = downloadUrl
        anchor.download = file.name
        anchor.rel = "noopener"
        document.body.appendChild(anchor)
        anchor.click()
        anchor.remove()
      },
      onError: (error: unknown) => {
        toast.error(
          error instanceof Error ? error.message : "Falha ao baixar arquivo."
        )
      },
    })
  }

  function handleMoveToTrash(item: FileItem) {
    const options = {
      onSuccess: () => {
        toast.success(
          item.type === "folder"
            ? "Pasta movida para a lixeira."
            : "Arquivo movido para a lixeira."
        )
      },
      onError: (error: unknown) => {
        toast.error(
          error instanceof Error
            ? error.message
            : "Falha ao mover item para a lixeira."
        )
      },
    }

    if (item.type === "folder") {
      moveFolderToTrashMutation.mutate(item.id, options)
      return
    }

    moveFileToTrashMutation.mutate(item.id, options)
  }

  function handleRestore(item: FileItem) {
    const options = {
      onSuccess: () => {
        toast.success(
          item.type === "folder"
            ? "Pasta restaurada com sucesso."
            : "Arquivo restaurado com sucesso."
        )
      },
      onError: (error: unknown) => {
        toast.error(
          error instanceof Error ? error.message : "Falha ao restaurar item."
        )
      },
    }

    if (item.type === "folder") {
      restoreFolderMutation.mutate(item.id, options)
      return
    }

    restoreFileMutation.mutate(item.id, options)
  }

  function handleToggleStarred(item: FileItem) {
    const nextIsStarred = !item.starred
    const options = {
      onSuccess: () => {
        toast.success(
          nextIsStarred
            ? "Item adicionado aos favoritos."
            : "Item removido dos favoritos."
        )
      },
      onError: (error: unknown) => {
        toast.error(
          error instanceof Error
            ? error.message
            : "Falha ao atualizar favorito."
        )
      },
    }

    if (item.type === "folder") {
      setFolderStarredMutation.mutate(
        {
          folderId: item.id,
          payload: { isStarred: nextIsStarred },
        },
        options
      )
      return
    }

    setFileStarredMutation.mutate(
      {
        fileId: item.id,
        payload: { isStarred: nextIsStarred },
      },
      options
    )
  }

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-30 bg-foreground/20 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <div
        className={cn(
          "fixed inset-y-0 left-0 z-40 transition-transform duration-300 lg:relative lg:translate-x-0",
          sidebarOpen ? "translate-x-0" : "-translate-x-full"
        )}
      >
        <Sidebar
          activeItem={getSidebarActiveItem(activeView)}
          showAdministration={user.role === "Admin"}
          onDriveOpen={handleOpenDrive}
          onStarredOpen={handleOpenStarred}
          onTrashOpen={handleOpenTrash}
          onAdminOpen={handleOpenAdmin}
        />
      </div>

      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <Header user={user} viewMode={viewMode} onViewChange={setViewMode} />

        <button
          className="fixed top-3.5 left-3.5 z-50 flex size-8 items-center justify-center rounded-lg border border-border bg-background shadow-sm lg:hidden"
          onClick={() => setSidebarOpen(!sidebarOpen)}
          aria-label="Menu"
        >
          {sidebarOpen ? <X className="size-4" /> : <Menu className="size-4" />}
        </button>

        <main className="flex-1 overflow-y-auto">
          <div className="mx-auto flex max-w-7xl flex-col gap-5 px-5 py-5">
            <div>
              <h1 className="text-xl font-semibold tracking-tight text-foreground">
                Bom dia, {user.name}
              </h1>
              {activeView === "trash" ? (
                <p className="mt-0.5 text-sm text-muted-foreground">
                  Itens removidos ficam listados aqui para revisão.
                </p>
              ) : (
                <p className="mt-0.5 text-sm text-muted-foreground">
                  Seus arquivos estão organizados e prontos para acesso.
                </p>
              )}
            </div>

            <QuickStats
              stats={fileCategoryStatsQuery.data}
              isLoading={fileCategoryStatsQuery.isLoading}
            />
            {activeView === "drive" ? (
              <Toolbar
                breadcrumb={breadcrumb}
                onBreadcrumbClick={setCurrentFolderId}
                onNewFolder={handleCreateFolder}
                onUpload={handleUploadClick}
                isUploading={uploadFileMutation.isPending}
              />
            ) : activeView === "starred" ? (
              <div className="flex items-center gap-2 py-2 text-sm font-semibold text-foreground">
                <Star className="size-4 fill-amber-500 text-amber-500" />
                Com estrela
              </div>
            ) : (
              <div className="flex items-center gap-2 py-2 text-sm font-semibold text-foreground">
                <Trash2 className="size-4 text-muted-foreground" />
                Lixeira
              </div>
            )}
            <input
              ref={fileInputRef}
              type="file"
              className="hidden"
              multiple
              onChange={handleFileSelection}
            />
            {uploadError && (
              <p className="-mt-3 text-sm text-destructive">{uploadError}</p>
            )}
            {uploadProgressItems.length > 0 && (
              <UploadProgressList items={uploadProgressItems} />
            )}
            <FileGrid
              viewMode={viewMode}
              items={items}
              isLoading={
                activeView === "drive"
                  ? directoryQuery.isLoading
                  : activeView === "starred"
                    ? starredQuery.isLoading
                    : trashQuery.isLoading
              }
              isTrashView={activeView === "trash"}
              onFolderOpen={
                activeView === "drive" ? setCurrentFolderId : undefined
              }
              onFileDownload={handleFileDownload}
              onToggleStarred={handleToggleStarred}
              onMoveToTrash={handleMoveToTrash}
              onRestore={handleRestore}
            />
          </div>
        </main>
      </div>
    </div>
  )
}

function getSidebarActiveItem(activeView: "drive" | "starred" | "trash") {
  if (activeView === "starred") {
    return "Com estrela"
  }

  if (activeView === "trash") {
    return "Lixeira"
  }

  return "Início"
}

function UploadProgressList({ items }: { items: UploadProgressItem[] }) {
  return (
    <div className="-mt-2 flex flex-col gap-3 rounded-lg border border-border bg-card p-3">
      {items.map((item) => (
        <div key={item.id} className="flex flex-col gap-1.5">
          <div className="flex items-center justify-between gap-3 text-xs">
            <span className="truncate font-medium text-foreground">
              {item.fileName}
            </span>
            <span className="shrink-0 text-muted-foreground">
              {Math.round(item.percentage)}%
            </span>
          </div>
          <Progress value={item.percentage} />
          <p className="text-xs text-muted-foreground">
            {formatFileSize(item.uploadedBytes)} de{" "}
            {formatFileSize(item.totalBytes)}
          </p>
        </div>
      ))}
    </div>
  )
}

function getUploadProgressId(file: File, index: number) {
  return `${file.name}-${file.size}-${file.lastModified}-${index}`
}

function getUploadSuccessMessage(fileCount: number) {
  if (fileCount === 1) {
    return "Arquivo enviado com sucesso."
  }

  return `${fileCount} arquivos enviados com sucesso.`
}

function getCurrentVirtualPath(breadcrumbs: StoredFolder[]) {
  if (breadcrumbs.length === 0) {
    return null
  }

  return breadcrumbs.map((folder) => folder.name).join("/")
}

function mapDirectoryToItems(
  folders: StoredFolder[],
  files: StoredFile[],
  useDeletedAt = false
): FileItem[] {
  return [
    ...folders.map((folder): FileItem => ({
      id: folder.id,
      name: folder.name,
      type: "folder",
      modified: formatDateLabel(
        (useDeletedAt ? folder.deletedAt : null) ??
          folder.updatedAt ??
          folder.createdAt
      ),
      deletedAt: folder.deletedAt,
      starred: folder.isStarred,
      color: getFolderColor(folder.id),
    })),
    ...files.map((file): FileItem => ({
      id: file.id,
      name: file.name,
      type: getFileType(file),
      size: formatFileSize(file.sizeInBytes),
      modified: formatDateLabel(
        (useDeletedAt ? file.deletedAt : null) ??
          file.updatedAt ??
          file.createdAt
      ),
      deletedAt: file.deletedAt,
      starred: file.isStarred,
    })),
  ]
}

function getFileType(file: StoredFile): FileItem["type"] {
  const extension = file.extension?.toLowerCase()
  const contentType = file.contentType.toLowerCase()

  if (contentType.startsWith("image/")) {
    return "image"
  }

  if (contentType.startsWith("video/")) {
    return "video"
  }

  if (contentType.startsWith("audio/")) {
    return "audio"
  }

  if (
    contentType.includes("pdf") ||
    contentType.includes("document") ||
    contentType.includes("msword") ||
    contentType.includes("presentation") ||
    contentType.includes("spreadsheet") ||
    contentType.startsWith("text/") ||
    [
      ".csv",
      ".doc",
      ".docx",
      ".ods",
      ".odt",
      ".pdf",
      ".ppt",
      ".pptx",
      ".rtf",
      ".txt",
      ".xls",
      ".xlsx",
    ].includes(extension ?? "")
  ) {
    return "document"
  }

  if (
    [
      ".cs",
      ".css",
      ".html",
      ".js",
      ".json",
      ".jsx",
      ".ts",
      ".tsx",
      ".xml",
      ".yml",
      ".yaml",
    ].includes(extension ?? "")
  ) {
    return "code"
  }

  return "file"
}

function formatFileSize(sizeInBytes: number) {
  if (sizeInBytes < 1024) {
    return `${sizeInBytes} B`
  }

  const units = ["KB", "MB", "GB", "TB"]
  let size = sizeInBytes / 1024
  let unitIndex = 0

  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024
    unitIndex += 1
  }

  return `${size.toLocaleString("pt-BR", {
    maximumFractionDigits: size >= 10 ? 0 : 1,
  })} ${units[unitIndex]}`
}

function formatDateLabel(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return "-"
  }

  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(date)
}

function getFolderColor(id: string) {
  const colors = ["amber", "blue", "violet", "emerald"]
  const index = id.charCodeAt(0) % colors.length

  return colors[index]
}
