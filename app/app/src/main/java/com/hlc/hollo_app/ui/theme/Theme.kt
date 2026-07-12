package com.hlc.hollo_app.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightColors = lightColorScheme(
    primary = Ink,
    onPrimary = WarmWhite,
    background = WarmWhite,
    onBackground = Ink,
    surface = WarmWhite,
    surfaceVariant = WarmSurface,
    onSurface = Ink,
    onSurfaceVariant = MutedInk,
    error = HolloError,
)

private val DarkColors = darkColorScheme(
    primary = DarkOnSurface,
    onPrimary = Ink,
    background = DarkBackground,
    onBackground = DarkOnSurface,
    surface = DarkSurface,
    onSurface = DarkOnSurface,
    onSurfaceVariant = ColorTokens.darkMuted,
    error = ColorTokens.darkError,
)

private object ColorTokens {
    val darkMuted = androidx.compose.ui.graphics.Color(0xFFC9C5BA)
    val darkError = androidx.compose.ui.graphics.Color(0xFFFFB4AB)
}

@Composable
fun HolloTheme(
    darkTheme: Boolean = true,
    content: @Composable () -> Unit,
) {
    MaterialTheme(
        colorScheme = if (darkTheme) DarkColors else LightColors,
        typography = Typography,
        content = content,
    )
}
