import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { adminApi } from "./api"

import type { UpdateAdminUserRequest } from "./types"

export const adminKeys = {
  all: ["admin"] as const,
  users: () => [...adminKeys.all, "users"] as const,
}

export function useAdminUsersQuery(enabled = true) {
  return useQuery({
    queryKey: adminKeys.users(),
    queryFn: adminApi.listUsers,
    enabled,
    retry: false,
  })
}

export function useUpdateAdminUserMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      userId,
      payload,
    }: {
      userId: string
      payload: UpdateAdminUserRequest
    }) => adminApi.updateUser(userId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.users() })
    },
  })
}

export function useDeleteAdminUserMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: adminApi.deleteUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.users() })
    },
  })
}
