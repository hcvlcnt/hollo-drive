import { apiRequest } from "~/lib/api/client"

import type { AuthSession, LoginRequest, RegisterRequest, User } from "./types"

export const authApi = {
  login(payload: LoginRequest) {
    return apiRequest<AuthSession>("/auth/login", {
      method: "POST",
      body: payload,
    })
  },
  register(payload: RegisterRequest) {
    return apiRequest<AuthSession>("/auth/register", {
      method: "POST",
      body: payload,
    })
  },
  me() {
    return apiRequest<User>("/auth/me")
  },
  logout() {
    return apiRequest<void>("/auth/logout", {
      method: "POST",
    })
  },
}
