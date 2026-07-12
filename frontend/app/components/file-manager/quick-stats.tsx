import { FileQuestion, FileText, Film, Image, Music } from "lucide-react"

import { cn } from "~/lib/utils"

import type { FileCategoryStats } from "~/features/files/types"

const STATS = [
  {
    key: "images",
    label: "Imagens",
    icon: Image,
    bg: "bg-sky-50 border-sky-100",
    text: "text-sky-600",
  },
  {
    key: "videos",
    label: "Videos",
    icon: Film,
    bg: "bg-pink-50 border-pink-100",
    text: "text-pink-600",
  },
  {
    key: "documents",
    label: "Documentos",
    icon: FileText,
    bg: "bg-red-50 border-red-100",
    text: "text-red-600",
  },
  {
    key: "audio",
    label: "Audio",
    icon: Music,
    bg: "bg-lime-50 border-lime-100",
    text: "text-lime-700",
  },
  {
    key: "others",
    label: "Outros",
    icon: FileQuestion,
    bg: "bg-zinc-50 border-zinc-100",
    text: "text-zinc-600",
  },
] satisfies Array<{
  key: keyof FileCategoryStats
  label: string
  icon: typeof Image
  bg: string
  text: string
}>

const EMPTY_STATS: FileCategoryStats = {
  images: { count: 0, sizeInBytes: 0 },
  videos: { count: 0, sizeInBytes: 0 },
  documents: { count: 0, sizeInBytes: 0 },
  audio: { count: 0, sizeInBytes: 0 },
  others: { count: 0, sizeInBytes: 0 },
}

type QuickStatsProps = {
  stats?: FileCategoryStats
  isLoading?: boolean
}

export function QuickStats({ stats = EMPTY_STATS, isLoading }: QuickStatsProps) {
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-5">
      {STATS.map((stat) => {
        const categoryStats = stats[stat.key]
        const fileCount = categoryStats.count

        return (
          <div
            key={stat.key}
            className="flex cursor-pointer items-center gap-3 rounded-xl border border-border bg-card p-4 transition-all hover:border-foreground/20 hover:shadow-sm"
          >
            <div
              className={cn(
                "flex size-9 shrink-0 items-center justify-center rounded-xl border",
                stat.bg
              )}
            >
              <stat.icon className={cn("size-4", stat.text)} />
            </div>
            <div className="min-w-0">
              <p className="text-xs text-muted-foreground">{stat.label}</p>
              <p className="text-sm leading-tight font-semibold text-foreground">
                {isLoading ? "..." : formatNumber(fileCount)}{" "}
                <span className="text-xs font-normal text-muted-foreground">
                  {fileCount === 1 ? "arquivo" : "arquivos"}
                </span>
              </p>
              <p className="text-xs text-muted-foreground">
                {isLoading ? "..." : formatFileSize(categoryStats.sizeInBytes)}
              </p>
            </div>
          </div>
        )
      })}
    </div>
  )
}

function formatNumber(value: number) {
  return value.toLocaleString("pt-BR")
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
