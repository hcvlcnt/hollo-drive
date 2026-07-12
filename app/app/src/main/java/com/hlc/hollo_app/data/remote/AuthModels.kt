package com.hlc.hollo_app.data.remote

import kotlinx.serialization.Serializable

@Serializable
data class LoginRequest(val email: String, val password: String)

@Serializable
data class AuthSession(val expiresAt: String, val user: User)

@Serializable
data class User(
    val id: String,
    val name: String,
    val email: String,
    val role: String,
    val isActive: Boolean,
    val createdAt: String,
    val updatedAt: String? = null,
    val deletedAt: String? = null,
)

@Serializable
data class ApiError(val error: String)
