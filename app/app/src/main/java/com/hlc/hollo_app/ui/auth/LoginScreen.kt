package com.hlc.hollo_app.ui.auth

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBars
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBars
import androidx.compose.foundation.layout.windowInsetsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.autofill.ContentType
import androidx.compose.ui.focus.FocusDirection
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.semantics.contentType
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.hlc.hollo_app.data.remote.User
import com.hlc.hollo_app.ui.components.HolloLogo
import org.koin.androidx.compose.koinViewModel

@Composable
fun LoginRoute(
    sessionMessage: String?,
    onLogin: (User) -> Unit,
    onRetrySession: () -> Unit,
    viewModel: LoginViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    LaunchedEffect(viewModel) {
        viewModel.events.collect { event ->
            if (event is LoginEvent.Success) onLogin(event.user)
        }
    }
    LoginScreen(
        state = state,
        sessionMessage = sessionMessage,
        onEmailChange = viewModel::onEmailChange,
        onPasswordChange = viewModel::onPasswordChange,
        onTogglePassword = viewModel::togglePasswordVisibility,
        onSubmit = viewModel::submit,
        onRetrySession = onRetrySession,
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LoginScreen(
    state: LoginUiState,
    sessionMessage: String?,
    onEmailChange: (String) -> Unit,
    onPasswordChange: (String) -> Unit,
    onTogglePassword: () -> Unit,
    onSubmit: () -> Unit,
    onRetrySession: () -> Unit,
) {
    val focusManager = LocalFocusManager.current
    Scaffold(containerColor = MaterialTheme.colorScheme.background) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .windowInsetsPadding(WindowInsets.statusBars)
                .windowInsetsPadding(WindowInsets.navigationBars)
                .imePadding()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 28.dp, vertical = 32.dp),
            verticalArrangement = Arrangement.Center,
        ) {
            Column(modifier = Modifier.fillMaxWidth()) {
                HolloLogo()
                Spacer(Modifier.padding(top = 30.dp))
                Text(
                    text = "Bem-vindo de volta",
                    style = MaterialTheme.typography.headlineMedium,
                    color = MaterialTheme.colorScheme.onBackground,
                )
                Text(
                    text = "Acesse sua conta para continuar.",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(top = 8.dp, bottom = 28.dp),
                )
                OutlinedTextField(
                    value = state.email,
                    onValueChange = onEmailChange,
                    modifier = Modifier
                        .fillMaxWidth()
                        .semantics { contentType = ContentType.EmailAddress },
                    label = { Text("E-mail") },
                    placeholder = { Text("seu@email.com") },
                    singleLine = true,
                    enabled = !state.isLoading,
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Email,
                        imeAction = ImeAction.Next,
                    ),
                    keyboardActions = KeyboardActions(
                        onNext = { focusManager.moveFocus(FocusDirection.Down) },
                    ),
                )
                OutlinedTextField(
                    value = state.password,
                    onValueChange = onPasswordChange,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 16.dp)
                        .semantics { contentType = ContentType.Password },
                    label = { Text("Senha") },
                    singleLine = true,
                    enabled = !state.isLoading,
                    visualTransformation = if (state.passwordVisible) {
                        VisualTransformation.None
                    } else PasswordVisualTransformation(),
                    trailingIcon = {
                        TextButton(onClick = onTogglePassword, enabled = !state.isLoading) {
                            Text(if (state.passwordVisible) "Ocultar" else "Mostrar")
                        }
                    },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Password,
                        imeAction = ImeAction.Done,
                    ),
                    keyboardActions = KeyboardActions(onDone = {
                        focusManager.clearFocus()
                        onSubmit()
                    }),
                )
                val message = state.errorMessage ?: sessionMessage
                if (message != null) {
                    Text(
                        text = message,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium,
                        modifier = Modifier.padding(top = 16.dp),
                    )
                    if (sessionMessage != null && state.errorMessage == null) {
                        TextButton(onClick = onRetrySession, contentPadding = PaddingValues(0.dp)) {
                            Text("Tentar validar novamente")
                        }
                    }
                }
                Button(
                    onClick = onSubmit,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 24.dp),
                    enabled = !state.isLoading,
                    contentPadding = PaddingValues(vertical = 14.dp),
                ) {
                    if (state.isLoading) {
                        CircularProgressIndicator(
                            color = Color.White,
                            strokeWidth = 2.dp,
                            modifier = Modifier.padding(end = 10.dp),
                        )
                        Text("Entrando…")
                    } else Text("Entrar")
                }
            }
        }
    }
}
