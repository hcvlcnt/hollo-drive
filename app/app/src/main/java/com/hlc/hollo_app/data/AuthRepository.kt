package com.hlc.hollo_app.data

import com.hlc.hollo_app.data.remote.ApiError
import com.hlc.hollo_app.data.remote.AuthApi
import com.hlc.hollo_app.data.remote.LoginRequest
import com.hlc.hollo_app.data.remote.User
import com.hlc.hollo_app.data.session.EncryptedSessionCookieJar
import kotlinx.serialization.json.Json
import java.io.IOException

sealed interface AuthFailure {
    data object InvalidCredentials : AuthFailure
    data object Network : AuthFailure
    data class Server(val message: String?) : AuthFailure
}

class AuthException(val failure: AuthFailure) : Exception()

interface AuthRepository {
    fun hasStoredSession(): Boolean
    suspend fun login(email: String, password: String): User
    suspend fun currentUser(): User
    suspend fun logout()
}

class DefaultAuthRepository(
    private val api: AuthApi,
    private val cookieJar: EncryptedSessionCookieJar,
    private val json: Json,
) : AuthRepository {
    override fun hasStoredSession() = cookieJar.hasSession()

    override suspend fun login(email: String, password: String): User = execute {
        val response = api.login(LoginRequest(email, password))
        if (response.isSuccessful) {
            response.body()?.user ?: throw AuthException(AuthFailure.Server(null))
        } else {
            if (response.code() == 401) throw AuthException(AuthFailure.InvalidCredentials)
            throw AuthException(AuthFailure.Server(errorMessage(response.errorBody()?.string())))
        }
    }

    override suspend fun currentUser(): User = execute {
        val response = api.me()
        if (response.isSuccessful) {
            response.body() ?: throw AuthException(AuthFailure.Server(null))
        } else {
            if (response.code() == 401) cookieJar.clear()
            throw AuthException(AuthFailure.Server(errorMessage(response.errorBody()?.string())))
        }
    }

    override suspend fun logout() {
        try {
            api.logout()
        } catch (_: Exception) {
            // Logout local deve sempre funcionar, mesmo offline.
        } finally {
            cookieJar.clear()
        }
    }

    private suspend fun <T> execute(block: suspend () -> T): T = try {
        block()
    } catch (error: AuthException) {
        throw error
    } catch (_: IOException) {
        throw AuthException(AuthFailure.Network)
    } catch (_: Exception) {
        throw AuthException(AuthFailure.Server(null))
    }

    private fun errorMessage(body: String?): String? = runCatching {
        body?.let { json.decodeFromString<ApiError>(it).error }
    }.getOrNull()
}
