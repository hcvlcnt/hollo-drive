package com.hlc.hollo_app.di

import com.hlc.hollo_app.BuildConfig
import com.hlc.hollo_app.data.AuthRepository
import com.hlc.hollo_app.data.DefaultAuthRepository
import com.hlc.hollo_app.data.DefaultFilesRepository
import com.hlc.hollo_app.data.FilesRepository
import com.hlc.hollo_app.data.remote.AuthApi
import com.hlc.hollo_app.data.remote.FilesApi
import com.hlc.hollo_app.data.session.EncryptedSessionCookieJar
import com.hlc.hollo_app.data.server.ServerConfigStore
import com.hlc.hollo_app.data.server.ServerUrlInterceptor
import com.hlc.hollo_app.ui.auth.LoginViewModel
import com.hlc.hollo_app.ui.home.HomeViewModel
import com.hlc.hollo_app.ui.navigation.SessionViewModel
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import org.koin.android.ext.koin.androidContext
import org.koin.core.module.dsl.singleOf
import org.koin.core.module.dsl.viewModelOf
import org.koin.dsl.bind
import org.koin.dsl.module
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory

val appModule = module {
    single {
        Json {
            ignoreUnknownKeys = true
            explicitNulls = false
        }
    }
    single { EncryptedSessionCookieJar(androidContext()) }
    single { ServerConfigStore(androidContext()) }
    single { ServerUrlInterceptor(get()) }
    single {
        OkHttpClient.Builder()
            .cookieJar(get<EncryptedSessionCookieJar>())
            .addInterceptor(get<ServerUrlInterceptor>())
            .apply {
                if (BuildConfig.DEBUG) {
                    addInterceptor(HttpLoggingInterceptor().apply {
                        level = HttpLoggingInterceptor.Level.BASIC
                    })
                }
            }
            .build()
    }
    single {
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(get())
            .addConverterFactory(get<Json>().asConverterFactory("application/json".toMediaType()))
            .build()
    }
    single { get<Retrofit>().create(AuthApi::class.java) }
    single { get<Retrofit>().create(FilesApi::class.java) }
    singleOf(::DefaultAuthRepository) bind AuthRepository::class
    singleOf(::DefaultFilesRepository) bind FilesRepository::class
    viewModelOf(::LoginViewModel)
    viewModelOf(::HomeViewModel)
    viewModelOf(::SessionViewModel)
}
