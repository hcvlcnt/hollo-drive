import { apiRequest } from "~/lib/api/client"

import type { AdminUser, UpdateAdminUserRequest } from "./types"

export const adminApi = {
  listUsers() {
    return apiRequest<AdminUser[]>("/admin/users")
  },
  updateUser(userId: string, payload: UpdateAdminUserRequest) {
    return apiRequest<AdminUser>(`/admin/users/${encodeURIComponent(userId)}`, {
      method: "PATCH",
      body: payload,
    })
  },
  deleteUser(userId: string) {
    return apiRequest<void>(`/admin/users/${encodeURIComponent(userId)}`, {
      method: "DELETE",
    })
  },
}
