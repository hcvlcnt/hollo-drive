import {
  Clock,
  Download,
  File,
  FileCode,
  FileIcon,
  FileText,
  Film,
  FolderClosed,
  Image,
  MoreHorizontal,
  Music,
  Pencil,
  RotateCcw,
  Star,
  Trash2,
} from "lucide-react"

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "~/components/ui/tooltip"
import { cn } from "~/lib/utils"
import { Empty, EmptyHeader, EmptyMedia, EmptyTitle } from "../ui/empty"

const TRASH_RETENTION_DAYS = 30

export type FileItem = {
  id: string
  name: string
  type: "folder" | "image" | "video" | "audio" | "document" | "code" | "file"
  size?: string
  modified: string
  starred?: boolean
  color?: string
  deletedAt?: string | null
}

const FOLDER_COLORS: Record<string, { bg: string; icon: string }> = {
  amber: { bg: "bg-amber-50 border-amber-100", icon: "text-amber-600" },
  blue: { bg: "bg-blue-50 border-blue-100", icon: "text-blue-600" },
  violet: { bg: "bg-violet-50 border-violet-100", icon: "text-violet-600" },
  emerald: {
    bg: "bg-emerald-50 border-emerald-100",
    icon: "text-emerald-600",
  },
}

const FILE_TYPE_CONFIG = {
  document: {
    icon: FileText,
    bg: "bg-red-50 border-red-100",
    text: "text-red-600",
  },
  image: {
    icon: Image,
    bg: "bg-sky-50 border-sky-100",
    text: "text-sky-600",
  },
  video: {
    icon: Film,
    bg: "bg-pink-50 border-pink-100",
    text: "text-pink-600",
  },
  audio: {
    icon: Music,
    bg: "bg-lime-50 border-lime-100",
    text: "text-lime-700",
  },
  code: {
    icon: FileCode,
    bg: "bg-cyan-50 border-cyan-100",
    text: "text-cyan-600",
  },
  file: {
    icon: File,
    bg: "bg-stone-50 border-stone-200",
    text: "text-stone-500",
  },
  folder: {
    icon: FolderClosed,
    bg: "bg-amber-50 border-amber-100",
    text: "text-amber-600",
  },
}

interface FileGridProps {
  viewMode: "grid" | "list"
  items: FileItem[]
  isLoading?: boolean
  isTrashView?: boolean
  onFolderOpen?: (folderId: string) => void
  onFileDownload?: (file: FileItem) => void
  onToggleStarred?: (item: FileItem) => void
  onMoveToTrash?: (item: FileItem) => void
  onRestore?: (item: FileItem) => void
}

export function FileGrid({
  viewMode,
  items,
  isLoading = false,
  isTrashView = false,
  onFolderOpen,
  onFileDownload,
  onToggleStarred,
  onMoveToTrash,
  onRestore,
}: FileGridProps) {
  const folders = items.filter((file) => file.type === "folder")
  const files = items.filter((file) => file.type !== "folder")

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5">
        {Array.from({ length: 8 }).map((_, index) => (
          <div
            key={index}
            className="h-28 animate-pulse rounded-xl border border-border bg-muted/50"
          />
        ))}
      </div>
    )
  }

  if (items.length === 0) {
    return (
      <div className="flex min-h-48 items-center justify-center rounded-xl border border-dashed border-border bg-card text-sm text-muted-foreground">
        <Empty>
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <FileIcon />
            </EmptyMedia>
            <EmptyTitle>Nenhum arquivo ou pasta encontrado.</EmptyTitle>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      {folders.length > 0 && (
        <section>
          <h2 className="mb-3 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
            Pastas
          </h2>
          <div
            className={cn(
              viewMode === "grid"
                ? "grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5"
                : "flex flex-col gap-1"
            )}
          >
            {folders.map((item) =>
              viewMode === "grid" ? (
                <FolderCard
                  key={item.id}
                  item={item}
                  onOpen={() => onFolderOpen?.(item.id)}
                  isTrashView={isTrashView}
                  onToggleStarred={onToggleStarred}
                  onMoveToTrash={onMoveToTrash}
                  onRestore={onRestore}
                />
              ) : (
                <FileListRow
                  key={item.id}
                  item={item}
                  onFolderOpen={onFolderOpen}
                  onToggleStarred={onToggleStarred}
                  isTrashView={isTrashView}
                  onMoveToTrash={onMoveToTrash}
                  onRestore={onRestore}
                />
              )
            )}
          </div>
        </section>
      )}

      {files.length > 0 && (
        <section>
          <h2 className="mb-3 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
            Arquivos
          </h2>
          {viewMode === "grid" ? (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5">
              {files.map((item) => (
                <FileCard
                  key={item.id}
                  item={item}
                  onDownload={onFileDownload}
                  onToggleStarred={onToggleStarred}
                  isTrashView={isTrashView}
                  onMoveToTrash={onMoveToTrash}
                  onRestore={onRestore}
                />
              ))}
            </div>
          ) : (
            <div className="overflow-hidden rounded-xl border border-border">
              <div className="grid grid-cols-[2fr_1fr_1fr_40px] gap-3 border-b border-border bg-muted/50 px-4 py-2">
                <span className="text-xs font-semibold text-muted-foreground">
                  Nome
                </span>
                <span className="text-xs font-semibold text-muted-foreground">
                  Tamanho
                </span>
                <span className="text-xs font-semibold text-muted-foreground">
                  Modificado
                </span>
                <span />
              </div>
              {files.map((item, index) => (
                <FileListRow
                  key={item.id}
                  item={item}
                  last={index === files.length - 1}
                  onFileDownload={onFileDownload}
                  onToggleStarred={onToggleStarred}
                  isTrashView={isTrashView}
                  onMoveToTrash={onMoveToTrash}
                  onRestore={onRestore}
                />
              ))}
            </div>
          )}
        </section>
      )}
    </div>
  )
}

function FolderCard({
  item,
  onOpen,
  isTrashView = false,
  onToggleStarred,
  onMoveToTrash,
  onRestore,
}: {
  item: FileItem
  onOpen?: () => void
  isTrashView?: boolean
  onToggleStarred?: (item: FileItem) => void
  onMoveToTrash?: (item: FileItem) => void
  onRestore?: (item: FileItem) => void
}) {
  const colorConfig = FOLDER_COLORS[item.color ?? "amber"]

  return (
    <div
      className="group relative cursor-pointer rounded-xl border border-border bg-card p-3.5 transition-all select-none hover:border-foreground/20 hover:shadow-sm"
      onClick={onOpen}
    >
      <div className="mb-3 flex items-start justify-between">
        <div
          className={cn(
            "flex size-10 items-center justify-center rounded-xl border",
            colorConfig.bg
          )}
        >
          <FolderClosed className={cn("size-5", colorConfig.icon)} />
        </div>
        <div className="flex items-center gap-1">
          {isTrashView && <TrashExpirationTooltip deletedAt={item.deletedAt} />}
          <FileActions
            item={item}
            isTrashView={isTrashView}
            onToggleStarred={onToggleStarred}
            onMoveToTrash={onMoveToTrash}
            onRestore={onRestore}
          />
        </div>
      </div>
      <p className="mb-0.5 truncate text-sm font-medium text-foreground">
        {item.name}
      </p>
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted-foreground">{item.modified}</p>
        <div className="flex items-center gap-1.5">
          {item.starred && (
            <Star className="size-3 fill-amber-500 text-amber-500" />
          )}
        </div>
      </div>
    </div>
  )
}

function FileCard({
  item,
  onDownload,
  isTrashView = false,
  onToggleStarred,
  onMoveToTrash,
  onRestore,
}: {
  item: FileItem
  onDownload?: (file: FileItem) => void
  isTrashView?: boolean
  onToggleStarred?: (item: FileItem) => void
  onMoveToTrash?: (item: FileItem) => void
  onRestore?: (item: FileItem) => void
}) {
  const config = FILE_TYPE_CONFIG[item.type] ?? FILE_TYPE_CONFIG.file
  const Icon = config.icon

  return (
    <div className="group relative cursor-pointer rounded-xl border border-border bg-card p-3.5 transition-all select-none hover:border-foreground/20 hover:shadow-sm">
      <div className="mb-3 flex items-start justify-between">
        <div
          className={cn(
            "flex size-10 items-center justify-center rounded-xl border",
            config.bg
          )}
        >
          <Icon className={cn("size-5", config.text)} />
        </div>
        <div className="flex items-center gap-1">
          {isTrashView && <TrashExpirationTooltip deletedAt={item.deletedAt} />}
          <FileActions
            item={item}
            onDownload={onDownload}
            isTrashView={isTrashView}
            onToggleStarred={onToggleStarred}
            onMoveToTrash={onMoveToTrash}
            onRestore={onRestore}
          />
        </div>
      </div>
      <p className="mb-0.5 truncate text-sm font-medium text-foreground">
        {item.name}
      </p>
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted-foreground">{item.size}</p>
        <div className="flex items-center gap-1.5">
          {item.starred && (
            <Star className="size-3 fill-amber-500 text-amber-500" />
          )}
        </div>
      </div>
    </div>
  )
}

function FileListRow({
  item,
  last,
  onFolderOpen,
  onFileDownload,
  isTrashView = false,
  onToggleStarred,
  onMoveToTrash,
  onRestore,
}: {
  item: FileItem
  last?: boolean
  onFolderOpen?: (folderId: string) => void
  onFileDownload?: (file: FileItem) => void
  isTrashView?: boolean
  onToggleStarred?: (item: FileItem) => void
  onMoveToTrash?: (item: FileItem) => void
  onRestore?: (item: FileItem) => void
}) {
  const isFolder = item.type === "folder"
  const config = FILE_TYPE_CONFIG[item.type] ?? FILE_TYPE_CONFIG.file
  const Icon = isFolder ? FolderClosed : config.icon
  const colorConfig = isFolder ? FOLDER_COLORS[item.color ?? "amber"] : null
  const iconClass = isFolder ? colorConfig!.icon : config.text
  const bgClass = isFolder ? colorConfig!.bg : config.bg

  return (
    <div
      className={cn(
        "group flex cursor-pointer items-center gap-3 px-4 py-2.5 transition-colors hover:bg-muted/40",
        !last && "border-b border-border",
        isFolder &&
          "mb-1 rounded-xl border border-border hover:border-foreground/20"
      )}
      onClick={() => {
        if (isFolder) {
          onFolderOpen?.(item.id)
        }
      }}
    >
      <div
        className={cn(
          "flex size-8 shrink-0 items-center justify-center rounded-lg border",
          bgClass
        )}
      >
        <Icon className={cn("size-4", iconClass)} />
      </div>
      <div className="grid min-w-0 flex-1 grid-cols-[2fr_1fr_1fr_40px] items-center gap-3">
        <div className="flex min-w-0 items-center gap-2">
          <span className="truncate text-sm font-medium text-foreground">
            {item.name}
          </span>
          {item.starred && (
            <Star className="size-3 shrink-0 fill-amber-500 text-amber-500" />
          )}
        </div>
        <span className="text-xs text-muted-foreground">
          {item.size ?? "-"}
        </span>
        <span className="text-xs text-muted-foreground">{item.modified}</span>
        <div className="flex items-center justify-end gap-1">
          {isTrashView && (
            <TrashExpirationTooltip deletedAt={item.deletedAt} compact />
          )}
          <FileActions
            item={item}
            onDownload={onFileDownload}
            isTrashView={isTrashView}
            onToggleStarred={onToggleStarred}
            onMoveToTrash={onMoveToTrash}
            onRestore={onRestore}
          />
        </div>
      </div>
    </div>
  )
}

function TrashExpirationTooltip({
  deletedAt,
  compact = false,
}: {
  deletedAt?: string | null
  compact?: boolean
}) {
  const expiration = getTrashExpirationInfo(deletedAt)

  if (!expiration) {
    return null
  }

  return (
    <TooltipProvider delayDuration={150}>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            className={cn(
              "flex shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground",
              compact ? "size-5" : "size-6"
            )}
            onClick={(event) => event.stopPropagation()}
            aria-label={expiration.ariaLabel}
          >
            <Clock className={compact ? "size-3.5" : "size-4"} />
          </button>
        </TooltipTrigger>
        <TooltipContent
          side="top"
          className="flex flex-col items-start gap-0.5"
        >
          <span>{expiration.deleteLabel}</span>
          <span className="text-background/80">
            {expiration.remainingLabel}
          </span>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

function getTrashExpirationInfo(deletedAt?: string | null) {
  if (!deletedAt) {
    return null
  }

  const deletedDate = new Date(deletedAt)

  if (Number.isNaN(deletedDate.getTime())) {
    return null
  }

  const deleteDate = new Date(deletedDate)
  deleteDate.setDate(deleteDate.getDate() + TRASH_RETENTION_DAYS)

  const today = startOfDay(new Date())
  const deleteDay = startOfDay(deleteDate)
  const daysRemaining = Math.ceil(
    (deleteDay.getTime() - today.getTime()) / (1000 * 60 * 60 * 24)
  )
  const formattedDate = formatTrashDateLabel(deleteDate)
  const remainingLabel =
    daysRemaining > 1
      ? `Faltam ${daysRemaining} dias`
      : daysRemaining === 1
        ? "Falta 1 dia"
        : daysRemaining === 0
          ? "Será excluído hoje"
          : "Exclusão pendente"

  return {
    deleteLabel: `Será excluído em ${formattedDate}`,
    remainingLabel,
    ariaLabel: `Será excluído em ${formattedDate}. ${remainingLabel}.`,
  }
}

function startOfDay(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

function formatTrashDateLabel(date: Date) {
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(date)
}

function FileActions({
  item,
  onDownload,
  isTrashView = false,
  onToggleStarred,
  onMoveToTrash,
  onRestore,
}: {
  item: FileItem
  onDownload?: (file: FileItem) => void
  isTrashView?: boolean
  onToggleStarred?: (item: FileItem) => void
  onMoveToTrash?: (item: FileItem) => void
  onRestore?: (item: FileItem) => void
}) {
  const isFolder = item.type === "folder"

  if (isTrashView) {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger
          className="flex size-6 items-center justify-center rounded-md text-muted-foreground opacity-0 transition-all group-hover:opacity-100 hover:bg-secondary hover:text-foreground data-[state=open]:opacity-100"
          onClick={(event) => event.stopPropagation()}
          aria-label="Mais opções"
        >
          <MoreHorizontal className="size-3.5" />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-40">
          <DropdownMenuItem
            className="gap-2"
            onClick={(event) => {
              event.stopPropagation()
              onRestore?.(item)
            }}
          >
            <RotateCcw className="size-3.5" />
            Restaurar
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    )
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className="flex size-6 items-center justify-center rounded-md text-muted-foreground opacity-0 transition-all group-hover:opacity-100 hover:bg-secondary hover:text-foreground"
        onClick={(event) => event.stopPropagation()}
        aria-label="Mais opções"
      >
        <MoreHorizontal className="size-3.5" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44">
        {!isTrashView && (
          <>
            <DropdownMenuItem className="gap-2">
              <Pencil className="size-3.5" />
              Renomear
            </DropdownMenuItem>
            <DropdownMenuItem
              className="gap-2"
              onClick={(event) => {
                event.stopPropagation()
                onToggleStarred?.(item)
              }}
            >
              <Star className="size-3.5" />
              {item.starred ? "Remover estrela" : "Adicionar estrela"}
            </DropdownMenuItem>
          </>
        )}
        {!isTrashView && !isFolder && (
          <DropdownMenuItem
            className="gap-2"
            onClick={(event) => {
              event.stopPropagation()
              onDownload?.(item)
            }}
          >
            <Download className="size-3.5" />
            Baixar
          </DropdownMenuItem>
        )}
        {!isTrashView && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="gap-2 text-destructive focus:text-destructive"
              onClick={(event) => {
                event.stopPropagation()
                onMoveToTrash?.(item)
              }}
            >
              <Trash2 className="size-3.5" />
              Mover para lixeira
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
