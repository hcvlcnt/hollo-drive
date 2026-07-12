package com.hlc.hollo_app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.hlc.hollo_app.ui.navigation.HolloApp
import com.hlc.hollo_app.ui.theme.HolloTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            HolloTheme {
                HolloApp()
            }
        }
    }
}
