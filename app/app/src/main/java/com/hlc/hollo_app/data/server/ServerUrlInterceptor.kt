package com.hlc.hollo_app.data.server

import com.hlc.hollo_app.BuildConfig
import okhttp3.Interceptor
import okhttp3.Response
import okhttp3.HttpUrl.Companion.toHttpUrl

class ServerUrlInterceptor(private val store: ServerConfigStore) : Interceptor {
    private val buildBaseUrl = BuildConfig.API_BASE_URL.toHttpUrl()

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val configured = store.baseUrl.value.toHttpUrl()
        val relativePath = request.url.encodedPath.removePrefix(buildBaseUrl.encodedPath)
        val configuredPath = configured.encodedPath.trimEnd('/')
        val url = request.url.newBuilder()
            .scheme(configured.scheme)
            .host(configured.host)
            .port(configured.port)
            .encodedPath("$configuredPath/$relativePath".replace("//", "/"))
            .build()

        return chain.proceed(request.newBuilder().url(url).build())
    }
}
