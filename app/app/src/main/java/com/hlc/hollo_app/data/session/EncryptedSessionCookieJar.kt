package com.hlc.hollo_app.data.session

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import okhttp3.Cookie
import okhttp3.CookieJar
import okhttp3.HttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrl
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

class EncryptedSessionCookieJar(context: Context) : CookieJar {
    private val preferences = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
    private var cachedCookie: Cookie? = readCookie()

    @Synchronized
    override fun saveFromResponse(url: HttpUrl, cookies: List<Cookie>) {
        val session = cookies.lastOrNull { it.name == COOKIE_NAME } ?: return
        if (session.expiresAt <= System.currentTimeMillis()) {
            clear()
            return
        }
        cachedCookie = session
        persist(session.toString())
    }

    @Synchronized
    override fun loadForRequest(url: HttpUrl): List<Cookie> {
        val cookie = cachedCookie ?: return emptyList()
        if (cookie.expiresAt <= System.currentTimeMillis()) {
            clear()
            return emptyList()
        }
        return if (cookie.matches(url)) listOf(cookie) else emptyList()
    }

    @Synchronized
    fun hasSession(): Boolean {
        val cookie = cachedCookie ?: return false
        if (cookie.expiresAt <= System.currentTimeMillis()) {
            clear()
            return false
        }
        return true
    }

    @Synchronized
    fun clear() {
        cachedCookie = null
        preferences.edit().clear().apply()
    }

    private fun persist(value: String) {
        runCatching {
            val cipher = Cipher.getInstance(TRANSFORMATION)
            cipher.init(Cipher.ENCRYPT_MODE, getOrCreateKey())
            val encrypted = cipher.doFinal(value.toByteArray(Charsets.UTF_8))
            preferences.edit()
                .putString(KEY_IV, Base64.encodeToString(cipher.iv, Base64.NO_WRAP))
                .putString(KEY_DATA, Base64.encodeToString(encrypted, Base64.NO_WRAP))
                .apply()
        }.onFailure { clear() }
    }

    private fun readCookie(): Cookie? = runCatching {
        val iv = preferences.getString(KEY_IV, null) ?: return null
        val data = preferences.getString(KEY_DATA, null) ?: return null
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(
            Cipher.DECRYPT_MODE,
            getOrCreateKey(),
            GCMParameterSpec(128, Base64.decode(iv, Base64.NO_WRAP)),
        )
        val plainText = cipher.doFinal(Base64.decode(data, Base64.NO_WRAP)).toString(Charsets.UTF_8)
        Cookie.parse(BuildConfigUrl.apiBaseUrl.toHttpUrl(), plainText)
    }.getOrElse {
        preferences.edit().clear().apply()
        null
    }

    private fun getOrCreateKey(): SecretKey {
        val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE).apply { load(null) }
        (keyStore.getKey(KEY_ALIAS, null) as? SecretKey)?.let { return it }
        return KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEYSTORE).run {
            init(
                KeyGenParameterSpec.Builder(
                    KEY_ALIAS,
                    KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT,
                )
                    .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                    .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                    .build(),
            )
            generateKey()
        }
    }

    private object BuildConfigUrl {
        val apiBaseUrl: String get() = com.hlc.hollo_app.BuildConfig.API_BASE_URL
    }

    private companion object {
        const val PREFS = "hollo_secure_session"
        const val COOKIE_NAME = "hollo_access_token"
        const val KEY_ALIAS = "hollo_session_key"
        const val KEY_IV = "iv"
        const val KEY_DATA = "data"
        const val ANDROID_KEYSTORE = "AndroidKeyStore"
        const val TRANSFORMATION = "AES/GCM/NoPadding"
    }
}
