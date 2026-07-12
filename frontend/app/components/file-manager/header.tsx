import { useState } from "react"
import { useNavigate } from "react-router"
import {
  Bell,
  ChevronDown,
  LayoutGrid,
  List,
  LogOut,
  Search,
  Settings,
} from "lucide-react"

import { Avatar, AvatarFallback } from "~/components/ui/avatar"
import { Button } from "~/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu"
import { Input } from "~/components/ui/input"
import { useLogoutMutation } from "~/features/auth/queries"
import type { User } from "~/features/auth/types"
import { cn } from "~/lib/utils"

interface HeaderProps {
  user: User
  viewMode: "grid" | "list"
  onViewChange: (mode: "grid" | "list") => void
}

export function Header({ user, viewMode, onViewChange }: HeaderProps) {
  const navigate = useNavigate()
  const logoutMutation = useLogoutMutation()
  const [searchFocused, setSearchFocused] = useState(false)

  const handleLogout = async () => {
    try {
      await logoutMutation.mutateAsync()
    } finally {
      navigate("/login", { replace: true })
    }
  }

  return (
    <header className="flex h-14 shrink-0 items-center gap-4 border-b border-border bg-background px-5 pl-14 lg:pl-5">
      <div
        className={cn(
          "relative flex-1 transition-all duration-200",
          searchFocused ? "max-w-lg" : "max-w-md"
        )}
      >
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Pesquisar arquivos..."
          className="h-8 border-border/50 bg-secondary/60 pl-9 text-sm text-foreground placeholder:text-muted-foreground/60 focus-visible:ring-ring/40"
          onFocus={() => setSearchFocused(true)}
          onBlur={() => setSearchFocused(false)}
        />
      </div>

      <div className="ml-auto flex items-center gap-1.5">
        <div className="flex items-center rounded-lg border border-border/50 bg-secondary/60 p-0.5">
          <button
            onClick={() => onViewChange("grid")}
            className={cn(
              "flex size-7 items-center justify-center rounded-md transition-all",
              viewMode === "grid"
                ? "bg-background text-foreground shadow-sm"
                : "text-muted-foreground hover:text-foreground"
            )}
            aria-label="Visualizacao em grade"
          >
            <LayoutGrid className="size-3.5" />
          </button>
          <button
            onClick={() => onViewChange("list")}
            className={cn(
              "flex size-7 items-center justify-center rounded-md transition-all",
              viewMode === "list"
                ? "bg-background text-foreground shadow-sm"
                : "text-muted-foreground hover:text-foreground"
            )}
            aria-label="Visualizacao em lista"
          >
            <List className="size-3.5" />
          </button>
        </div>

        <Button
          variant="ghost"
          size="icon"
          className="size-8 text-muted-foreground hover:bg-secondary/80 hover:text-foreground"
        >
          <Bell className="size-4" />
          <span className="sr-only">Notificacoes</span>
        </Button>

        <Button
          variant="ghost"
          size="icon"
          className="size-8 text-muted-foreground hover:bg-secondary/80 hover:text-foreground"
        >
          <Settings className="size-4" />
          <span className="sr-only">Configuracoes</span>
        </Button>

        <DropdownMenu>
          <DropdownMenuTrigger className="ml-1 flex items-center gap-2 rounded-lg py-1 pr-2 pl-1 transition-colors hover:bg-secondary/80">
            <Avatar className="size-7">
              <AvatarFallback className="bg-foreground/15 text-xs font-semibold text-foreground">
                {getInitials(user.name)}
              </AvatarFallback>
            </Avatar>
            <span className="hidden max-w-28 truncate text-sm font-medium text-foreground sm:block">
              {getShortName(user.name)}
            </span>
            <ChevronDown className="hidden size-3.5 text-muted-foreground sm:block" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuLabel className="font-normal">
              <div className="flex flex-col gap-0.5">
                <span className="truncate text-sm font-medium text-foreground">
                  {user.name}
                </span>
                <span className="truncate text-xs text-muted-foreground">
                  {user.email}
                </span>
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem>Meu perfil</DropdownMenuItem>
            <DropdownMenuItem>Configuracoes</DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive focus:text-destructive"
              disabled={logoutMutation.isPending}
              onClick={handleLogout}
            >
              <LogOut className="size-4" />
              Sair
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}

function getInitials(name: string) {
  return name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase()
}

function getShortName(name: string) {
  const [firstName, secondName] = name.trim().split(/\s+/)

  return secondName ? `${firstName} ${secondName[0]}.` : firstName
}
