import type { StorageUsageScope } from "~/features/files/types"

export type AdminUser = {
  id: string
  name: string
  email: string
  role: string
  isActive: boolean
  storageUsage: StorageUsageScope | null
  createdAt: string
  updatedAt: string | null
  deletedAt: string | null
}

export type UpdateAdminUserRequest = {
  isActive?: boolean
}
