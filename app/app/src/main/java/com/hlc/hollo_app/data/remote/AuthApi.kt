package com.hlc.hollo_app.data.remote

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST

interface AuthApi {
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthSession>

    @GET("auth/me")
    suspend fun me(): Response<User>

    @POST("auth/logout")
    suspend fun logout(): Response<Unit>
}
