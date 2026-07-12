import { useEffect, useState } from "react"
import { useNavigate } from "react-router"
import {
  Loader2,
  Menu,
  Power,
  ShieldCheck,
  Trash2,
  UserRound,
  X,
} from "lucide-react"
import { toast } from "sonner"

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "~/components/ui/alert-dialog"
import { ADMIN_LABEL, Sidebar } from "~/components/file-manager/sidebar"
import { Button } from "~/components/ui/button"
import { Progress } from "~/components/ui/progress"
import { useCurrentUserQuery } from "~/features/auth/queries"
import {
  useAdminUsersQuery,
  useDeleteAdminUserMutation,
  useUpdateAdminUserMutation,
} from "~/features/admin/queries"
import { ApiError } from "~/lib/api/client"
import { cn } from "~/lib/utils"

import type { AdminUser } from "~/features/admin/types"

export default function AdminPage() {
  const navigate = useNavigate()
  const currentUserQuery = useCurrentUserQuery()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const adminUsersQuery = useAdminUsersQuery(Boolean(currentUserQuery.data))
  const updateUserMutation = useUpdateAdminUserMutation()
  const deleteUserMutation = useDeleteAdminUserMutation()
  const user = currentUserQuery.data
  const users = adminUsersQuery.data ?? []
  const adminError = adminUsersQuery.error
  const isAccessDenied =
    adminError instanceof ApiError && adminError.status === 403

  useEffect(() => {
    if (currentUserQuery.isError) {
      navigate("/login", { replace: true })
    }
  }, [currentUserQuery.isError, navigate])

  useEffect(() => {
    if (adminError instanceof ApiError && adminError.status === 401) {
      navigate("/login", { replace: true })
    }
  }, [adminError, navigate])

  if (currentUserQuery.isLoading) {
    return (
      <div className="flex h-screen items-center justify-center bg-background text-muted-foreground">
        <Loader2 className="size-5 animate-spin" />
      </div>
    )
  }

  if (!user) {
    return null
  }

  function handleOpenDrive() {
    navigate("/")
  }

  function handleToggleAccess(targetUser: AdminUser) {
    const nextIsActive = !targetUser.isActive

    updateUserMutation.mutate(
      {
        userId: targetUser.id,
        payload: { isActive: nextIsActive },
      },
      {
        onSuccess: () => {
          toast.success(
            nextIsActive
              ? "Acesso do usuário reativado."
              : "Acesso do usuário desativado."
          )
        },
        onError: (error: unknown) => {
          toast.error(getAdminActionError(error))
        },
      }
    )
  }

  function handleDeleteAccess(targetUser: AdminUser) {
    deleteUserMutation.mutate(targetUser.id, {
      onSuccess: () => {
        toast.success("Acesso apagado com sucesso.")
      },
      onError: (error: unknown) => {
        toast.error(getAdminActionError(error))
      },
    })
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
          activeItem={ADMIN_LABEL}
          showAdministration={user.role === "Admin"}
          onDriveOpen={handleOpenDrive}
          onStarredOpen={handleOpenDrive}
          onTrashOpen={handleOpenDrive}
          onAdminOpen={() => setSidebarOpen(false)}
        />
      </div>

      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <button
          className="fixed top-3.5 left-3.5 z-50 flex size-8 items-center justify-center rounded-lg border border-border bg-background shadow-sm lg:hidden"
          onClick={() => setSidebarOpen(!sidebarOpen)}
          aria-label="Menu"
        >
          {sidebarOpen ? <X className="size-4" /> : <Menu className="size-4" />}
        </button>

        <main className="flex-1 overflow-y-auto">
          <div className="mx-auto flex max-w-7xl flex-col gap-5 px-5 py-5">
            <div className="flex flex-col gap-3 border-b border-border pb-5 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="mb-2 flex items-center gap-2 text-sm font-medium text-muted-foreground">
                  <ShieldCheck className="size-4" />
                  Administração
                </div>
                <h1 className="text-xl font-semibold tracking-tight text-foreground">
                  Usuarios do sistema
                </h1>
                <p className="mt-0.5 text-sm text-muted-foreground">
                  Gerencie acesso sem visualizar senhas ou credenciais.
                </p>
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <UserRound className="size-4" />
                {user.name}
              </div>
            </div>

            {isAccessDenied ? (
              <AccessDeniedState />
            ) : adminUsersQuery.isLoading ? (
              <LoadingState />
            ) : adminUsersQuery.isError ? (
              <ErrorState />
            ) : (
              <UsersTable
                users={users}
                currentUserId={user.id}
                isMutating={
                  updateUserMutation.isPending || deleteUserMutation.isPending
                }
                onToggleAccess={handleToggleAccess}
                onDeleteAccess={handleDeleteAccess}
              />
            )}
          </div>
        </main>
      </div>
    </div>
  )
}

function UsersTable({
  users,
  currentUserId,
  isMutating,
  onToggleAccess,
  onDeleteAccess,
}: {
  users: AdminUser[]
  currentUserId: string
  isMutating: boolean
  onToggleAccess: (user: AdminUser) => void
  onDeleteAccess: (user: AdminUser) => void
}) {
  if (users.length === 0) {
    return (
      <div className="flex min-h-48 items-center justify-center rounded-xl border border-dashed border-border bg-card text-sm text-muted-foreground">
        Nenhum usuário encontrado.
      </div>
    )
  }

  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card">
      <div className="grid grid-cols-[minmax(220px,2fr)_130px_minmax(180px,1fr)_220px] gap-4 border-b border-border bg-muted/50 px-4 py-2.5 max-lg:hidden">
        <span className="text-xs font-semibold text-muted-foreground">
          Usuario
        </span>
        <span className="text-xs font-semibold text-muted-foreground">
          Acesso
        </span>
        <span className="text-xs font-semibold text-muted-foreground">
          Consumo
        </span>
        <span className="text-xs font-semibold text-muted-foreground">
          Ações
        </span>
      </div>

      {users.map((targetUser) => {
        const isCurrentUser = targetUser.id === currentUserId
        const usage = targetUser.storageUsage
        const usedPercentage = Math.round(usage?.usedPercentage ?? 0)

        return (
          <div
            key={targetUser.id}
            className="grid grid-cols-1 gap-3 border-b border-border px-4 py-4 last:border-b-0 lg:grid-cols-[minmax(220px,2fr)_130px_minmax(180px,1fr)_220px] lg:items-center lg:gap-4"
          >
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <p className="truncate text-sm font-medium text-foreground">
                  {targetUser.name}
                </p>
                {isCurrentUser && (
                  <span className="rounded-full bg-secondary px-2 py-0.5 text-[11px] font-medium text-secondary-foreground">
                    Você
                  </span>
                )}
              </div>
              <p className="truncate text-xs text-muted-foreground">
                {targetUser.email}
              </p>
            </div>

            <div>
              <span
                className={cn(
                  "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                  targetUser.isActive
                    ? "bg-emerald-50 text-emerald-700"
                    : "bg-muted text-muted-foreground"
                )}
              >
                {targetUser.isActive ? "Ativo" : "Inativo"}
              </span>
            </div>

            <div className="min-w-0">
              <div className="mb-1.5 flex items-center justify-between gap-3 text-xs">
                <span className="truncate text-muted-foreground">
                  {formatStorageSize(usage?.usedInBytes ?? 0)}
                </span>
                <span className="shrink-0 text-muted-foreground">
                  {usedPercentage}%
                </span>
              </div>
              <Progress value={usedPercentage} />
            </div>

            <div className="flex flex-wrap items-center gap-2 lg:justify-end">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={isCurrentUser || isMutating}
                onClick={() => onToggleAccess(targetUser)}
              >
                <Power className="size-3.5" />
                {targetUser.isActive ? "Desativar" : "Reativar"}
              </Button>
              <DeleteAccessDialog
                user={targetUser}
                disabled={isCurrentUser || isMutating}
                onConfirm={() => onDeleteAccess(targetUser)}
              />
            </div>
          </div>
        )
      })}
    </div>
  )
}

function DeleteAccessDialog({
  user,
  disabled,
  onConfirm,
}: {
  user: AdminUser
  disabled: boolean
  onConfirm: () => void
}) {
  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button
          type="button"
          variant="destructive"
          size="sm"
          disabled={disabled}
        >
          <Trash2 className="size-3.5" />
          Apagar
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Apagar acesso?</AlertDialogTitle>
          <AlertDialogDescription>
            O usuário {user.email} não poderá mais acessar o sistema. Esta ação
            não exibe nem altera senhas.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancelar</AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onConfirm}>
            Apagar acesso
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function LoadingState() {
  return (
    <div className="flex min-h-48 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground">
      <Loader2 className="size-5 animate-spin" />
    </div>
  )
}

function AccessDeniedState() {
  return (
    <div className="flex min-h-48 flex-col items-center justify-center rounded-xl border border-border bg-card px-6 text-center">
      <ShieldCheck className="mb-3 size-7 text-muted-foreground" />
      <h2 className="text-sm font-semibold text-foreground">Acesso negado</h2>
      <p className="mt-1 max-w-sm text-sm text-muted-foreground">
        O backend recusou a consulta administrativa para este usuário.
      </p>
    </div>
  )
}

function ErrorState() {
  return (
    <div className="flex min-h-48 items-center justify-center rounded-xl border border-border bg-card text-sm text-destructive">
      Não foi possível carregar os usuários.
    </div>
  )
}

function getAdminActionError(error: unknown) {
  return error instanceof Error ? error.message : "Falha ao atualizar usuário."
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
