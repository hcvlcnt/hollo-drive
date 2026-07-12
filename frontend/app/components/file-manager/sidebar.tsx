import { useState, type ElementType } from "react"
import {
  ChevronRight,
  Clock,
  FolderClosed,
  Home,
  Plus,
  ShieldCheck,
  Star,
  Trash2,
} from "lucide-react"

import { BrandLogo } from "~/components/brand-logo"
import { Button } from "~/components/ui/button"
import { Separator } from "~/components/ui/separator"
import { useStorageUsageQuery } from "~/features/files/queries"
import { cn } from "~/lib/utils"

interface NavItem {
  icon: ElementType
  label: string
  href: string
  active?: boolean
  badge?: string
}

export const ADMIN_LABEL = "Administração"

const baseMainNav: NavItem[] = [
  { icon: Home, label: "Início", href: "/", active: true },
  { icon: Clock, label: "Recentes", href: "#" },
  { icon: Star, label: "Com estrela", href: "#" },
]

const storageNav: NavItem[] = [
  { icon: FolderClosed, label: "Meu Drive", href: "#" },
  { icon: Trash2, label: "Lixeira", href: "#" },
]

interface SidebarProps {
  className?: string
  activeItem?: string
  showAdministration?: boolean
  onDriveOpen?: () => void
  onStarredOpen?: () => void
  onTrashOpen?: () => void
  onAdminOpen?: () => void
}

export function Sidebar({
  className,
  activeItem: controlledActiveItem,
  showAdministration = false,
  onDriveOpen,
  onStarredOpen,
  onTrashOpen,
  onAdminOpen,
}: SidebarProps) {
  const [activeItem, setActiveItem] = useState("Início")
  const storageUsageQuery = useStorageUsageQuery()
  const storageUsage = storageUsageQuery.data
  const currentActiveItem = controlledActiveItem ?? activeItem
  const mainNav = showAdministration
    ? [
        ...baseMainNav,
        { icon: ShieldCheck, label: ADMIN_LABEL, href: "/admin" },
      ]
    : baseMainNav
  const primaryStorage = storageUsage?.isAdmin
    ? (storageUsage.system ?? storageUsage.currentUser)
    : storageUsage?.currentUser
  const currentUserStorage = storageUsage?.currentUser
  const usagePercentage = Math.round(primaryStorage?.usedPercentage ?? 0)
  const usageLabel = primaryStorage
    ? `${formatStorageSize(primaryStorage.usedInBytes)} / ${formatStorageSize(
        primaryStorage.quotaInBytes
      )}`
    : storageUsageQuery.isLoading
      ? "Carregando..."
      : "Indisponível"
  const currentUserUsageLabel =
    storageUsage?.isAdmin && currentUserStorage
      ? `Seu uso: ${formatStorageSize(
          currentUserStorage.usedInBytes
        )} / ${formatStorageSize(currentUserStorage.quotaInBytes)}`
      : null

  return (
    <aside
      className={cn(
        "flex h-full w-60 shrink-0 flex-col border-r border-sidebar-border bg-sidebar",
        className
      )}
    >
      <div className="border-b border-sidebar-border px-5 py-4">
        <BrandLogo markClassName="size-7" textClassName="text-sm" />
      </div>

      <div className="px-4 pt-4 pb-2">
        <Button
          size="sm"
          className="h-9 w-full gap-1.5 bg-primary text-sm font-medium text-primary-foreground hover:bg-primary/90"
        >
          <Plus className="size-4" />
          Novo
        </Button>
      </div>

      <nav className="flex flex-1 flex-col gap-0.5 overflow-y-auto px-3 py-1">
        {mainNav.map((item) => (
          <NavLink
            key={item.label}
            item={item}
            isActive={currentActiveItem === item.label}
            onClick={() => {
              setActiveItem(item.label)

              if (item.href === "/") {
                onDriveOpen?.()
              }

              if (item.label === "Com estrela") {
                onStarredOpen?.()
              }

              if (item.label === ADMIN_LABEL) {
                onAdminOpen?.()
              }
            }}
          />
        ))}

        <Separator className="my-2 bg-sidebar-border" />

        <p className="px-2 py-1 text-[11px] font-semibold tracking-wider text-muted-foreground/70 uppercase">
          Armazenamento
        </p>

        {storageNav.map((item) => (
          <NavLink
            key={item.label}
            item={item}
            isActive={currentActiveItem === item.label}
            onClick={() => {
              setActiveItem(item.label)

              if (item.label === "Meu Drive") {
                onDriveOpen?.()
              }

              if (item.label === "Lixeira") {
                onTrashOpen?.()
              }
            }}
          />
        ))}
      </nav>

      <div className="border-t border-sidebar-border p-4">
        <div className="mb-1.5 flex items-center justify-between gap-2">
          <span className="text-xs font-medium text-foreground">
            {storageUsage?.isAdmin ? "Armazenamento total" : "Armazenamento"}
          </span>
          <span className="shrink-0 text-xs text-muted-foreground">
            {usageLabel}
          </span>
        </div>
        <div className="h-1.5 overflow-hidden rounded-full bg-border">
          <div
            className="h-full rounded-full bg-foreground/70 transition-all"
            style={{ width: `${usagePercentage}%` }}
          />
        </div>
        {currentUserUsageLabel && (
          <p className="mt-2 text-xs text-muted-foreground">
            {currentUserUsageLabel}
          </p>
        )}
        <button className="mt-2.5 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground">
          {storageUsage?.isAdmin
            ? "Gerenciar armazenamento"
            : "Adquirir mais espaço"}{" "}
          <ChevronRight className="inline size-3" />
        </button>
      </div>
    </aside>
  )
}

function formatStorageSize(sizeInBytes: number) {
  if (sizeInBytes < 1024) {
    return `${sizeInBytes} B`
  }

  const units = ["KB", "MB", "GB", "TB", "PB"]
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

function NavLink({
  item,
  isActive,
  onClick,
}: {
  item: NavItem
  isActive: boolean
  onClick: () => void
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "flex w-full items-center gap-2.5 rounded-lg px-2 py-1.5 text-left text-sm transition-colors",
        isActive
          ? "bg-sidebar-accent font-medium text-sidebar-accent-foreground"
          : "text-sidebar-foreground/80 hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground"
      )}
    >
      <item.icon className="size-4 shrink-0" />
      <span className="flex-1 truncate">{item.label}</span>
      {item.badge && (
        <span className="rounded-full bg-foreground/10 px-1.5 py-0.5 text-[10px] font-semibold text-foreground/60">
          {item.badge}
        </span>
      )}
    </button>
  )
}
