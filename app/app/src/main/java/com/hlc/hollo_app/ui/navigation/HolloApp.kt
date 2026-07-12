package com.hlc.hollo_app.ui.navigation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.hlc.hollo_app.ui.auth.LoginRoute
import com.hlc.hollo_app.ui.components.ServerStatusBar
import com.hlc.hollo_app.ui.home.HomeScreen
import kotlinx.serialization.Serializable
import org.koin.androidx.compose.koinViewModel

@Serializable
private data object LoginDestination

@Serializable
private data object HomeDestination

@Composable
fun HolloApp(sessionViewModel: SessionViewModel = koinViewModel()) {
    val status by sessionViewModel.status.collectAsStateWithLifecycle()
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MaterialTheme.colorScheme.background),
    ) {
        ServerStatusBar()
        Box(modifier = Modifier.weight(1f)) {
            HolloAppContent(status, sessionViewModel)
        }
    }
}

@Composable
private fun HolloAppContent(status: SessionStatus, sessionViewModel: SessionViewModel) {
    if (status is SessionStatus.Loading) {
        LoadingScreen()
        return
    }
    val navController = rememberNavController()
    val signedIn = status as? SessionStatus.SignedIn

    LaunchedEffect(signedIn?.user?.id) {
        val target = if (signedIn != null) HomeDestination else LoginDestination
        navController.navigate(target) {
            popUpTo(navController.graph.findStartDestination().id) { inclusive = true }
            launchSingleTop = true
        }
    }

    NavHost(navController = navController, startDestination = if (signedIn != null) HomeDestination else LoginDestination) {
        composable<LoginDestination> {
            val signedOut = status as? SessionStatus.SignedOut
            LoginRoute(
                sessionMessage = signedOut?.message,
                onLogin = sessionViewModel::onLogin,
                onRetrySession = sessionViewModel::retry,
            )
        }
        composable<HomeDestination> {
            val user = (status as? SessionStatus.SignedIn)?.user
            if (user != null) HomeScreen(user = user, onLogout = sessionViewModel::logout)
        }
    }
}

@Composable
private fun LoadingScreen() {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        CircularProgressIndicator()
        Text(
            text = "Validando sessão…",
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}
