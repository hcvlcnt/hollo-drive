package com.hlc.hollo_app.data.server

import android.content.Context
import com.hlc.hollo_app.BuildConfig
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.Serializable
import okhttp3.HttpUrl.Companion.toHttpUrl

@Serializable
data class ServerPairingPayload(
    val version: Int,
    val serverId: String,
    val name: String,
    val apiUrl: String,
)

class ServerConfigStore(context: Context) {
    private val preferences = context.getSharedPreferences("hollo_server", Context.MODE_PRIVATE)
    private val _baseUrl = MutableStateFlow(preferences.getString(KEY_API_URL, null) ?: BuildConfig.API_BASE_URL)
    val baseUrl: StateFlow<String> = _baseUrl.asStateFlow()

    val serverName: String?
        get() = preferences.getString(KEY_SERVER_NAME, null)

    fun save(payload: ServerPairingPayload) {
        require(payload.version == 1) { "Versão de pareamento incompatível." }
        require(payload.serverId.isNotBlank()) { "Servidor sem identificação." }

        val parsed = payload.apiUrl.toHttpUrl()
        require(parsed.scheme == "http" || parsed.scheme == "https") { "Endereço inválido." }
        val normalized = parsed.newBuilder()
            .encodedPath(parsed.encodedPath.trimEnd('/') + "/")
            .query(null)
            .fragment(null)
            .build()
            .toString()

        preferences.edit()
            .putString(KEY_API_URL, normalized)
            .putString(KEY_SERVER_ID, payload.serverId)
            .putString(KEY_SERVER_NAME, payload.name)
            .apply()
        _baseUrl.value = normalized
    }

    private companion object {
        const val KEY_API_URL = "api_url"
        const val KEY_SERVER_ID = "server_id"
        const val KEY_SERVER_NAME = "server_name"
    }
}
