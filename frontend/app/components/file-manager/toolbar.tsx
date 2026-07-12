import { ChevronRight, FolderPlus, Home, Loader2, Upload } from "lucide-react"

import { Button } from "~/components/ui/button"

export type BreadcrumbItem = {
  id: string | null
  name: string
}

interface ToolbarProps {
  breadcrumb?: BreadcrumbItem[]
  onUpload?: () => void
  onNewFolder?: () => void
  onBreadcrumbClick?: (folderId: string | null) => void
  isUploading?: boolean
}

export function Toolbar({
  breadcrumb = [{ id: null, name: "Meu Drive" }],
  onUpload,
  onNewFolder,
  onBreadcrumbClick,
  isUploading = false,
}: ToolbarProps) {
  return (
    <div className="flex items-center justify-between gap-3 py-2">
      <nav
        aria-label="Localização atual"
        className="flex min-w-0 items-center gap-1.5"
      >
        <button
          className="flex items-center gap-1 text-muted-foreground transition-colors hover:text-foreground"
          onClick={() => onBreadcrumbClick?.(null)}
        >
          <Home className="size-3.5" />
        </button>
        {breadcrumb.map((crumb, i) => (
          <span key={crumb.id ?? "root"} className="flex min-w-0 items-center gap-1.5">
            <ChevronRight className="size-3.5 shrink-0 text-muted-foreground/50" />
            <button
              className={
                i === breadcrumb.length - 1
                  ? "truncate text-sm font-semibold text-foreground"
                  : "truncate text-sm text-muted-foreground transition-colors hover:text-foreground"
              }
              onClick={() => onBreadcrumbClick?.(crumb.id)}
            >
              {crumb.name}
            </button>
          </span>
        ))}
      </nav>

      <div className="flex shrink-0 items-center gap-2">
        <Button
          variant="outline"
          size="sm"
          className="h-8 gap-1.5 border-border text-sm font-medium text-foreground hover:bg-secondary/80"
          onClick={onNewFolder}
        >
          <FolderPlus className="size-3.5" />
          Nova pasta
        </Button>
        <Button
          size="sm"
          className="h-8 gap-1.5 bg-primary text-sm font-medium text-primary-foreground hover:bg-primary/90"
          onClick={onUpload}
          disabled={isUploading}
        >
          {isUploading ? (
            <Loader2 className="size-3.5 animate-spin" />
          ) : (
            <Upload className="size-3.5" />
          )}
          {isUploading ? "Enviando..." : "Enviar arquivo"}
        </Button>
      </div>
    </div>
  )
}
