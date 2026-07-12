package com.hlc.hollo_app.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.hlc.hollo_app.data.AuthException
import com.hlc.hollo_app.data.AuthFailure
import com.hlc.hollo_app.data.AuthRepository
import com.hlc.hollo_app.data.remote.User
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class LoginUiState(
    val email: String = "",
    val password: String = "",
    val passwordVisible: Boolean = false,
    val isLoading: Boolean = false,
    val errorMessage: String? = null,
)

sealed interface LoginEvent {
    data class Success(val user: User) : LoginEvent
}

class LoginViewModel(private val repository: AuthRepository) : ViewModel() {
    private val _state = MutableStateFlow(LoginUiState())
    val state: StateFlow<LoginUiState> = _state.asStateFlow()

    private val _events = Channel<LoginEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    fun onEmailChange(value: String) = _state.update {
        it.copy(email = value, errorMessage = null)
    }

    fun onPasswordChange(value: String) = _state.update {
        it.copy(password = value, errorMessage = null)
    }

    fun togglePasswordVisibility() = _state.update {
        it.copy(passwordVisible = !it.passwordVisible)
    }

    fun submit() {
        val current = _state.value
        if (current.isLoading) return
        val validationError = validate(current.email, current.password)
        if (validationError != null) {
            _state.update { it.copy(errorMessage = validationError) }
            return
        }

        viewModelScope.launch {
            _state.update { it.copy(isLoading = true, errorMessage = null) }
            try {
                val user = repository.login(current.email.trim(), current.password)
                _events.send(LoginEvent.Success(user))
            } catch (error: AuthException) {
                _state.update { it.copy(errorMessage = error.failure.toMessage()) }
            } finally {
                _state.update { it.copy(isLoading = false) }
            }
        }
    }

    private fun validate(email: String, password: String): String? = when {
        email.isBlank() -> "Informe seu e-mail."
        !EMAIL_PATTERN.matches(email.trim()) -> "Informe um e-mail válido."
        password.isBlank() -> "Informe sua senha."
        else -> null
    }
}

private val EMAIL_PATTERN = Regex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")

private fun AuthFailure.toMessage(): String = when (this) {
    AuthFailure.InvalidCredentials -> "E-mail ou senha inválidos."
    AuthFailure.Network -> "Não foi possível conectar ao Hollo. Verifique sua rede e tente novamente."
    is AuthFailure.Server -> message ?: "Ocorreu um erro inesperado. Tente novamente."
}
