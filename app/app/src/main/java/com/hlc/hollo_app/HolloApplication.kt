package com.hlc.hollo_app

import android.app.Application
import com.hlc.hollo_app.di.appModule
import org.koin.android.ext.koin.androidContext
import org.koin.core.context.startKoin

class HolloApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        startKoin {
            androidContext(this@HolloApplication)
            modules(appModule)
        }
    }
}
