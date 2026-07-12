package com.hlc.hollo_app.ui.navigation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.hlc.hollo_app.data.AuthException
import com.hlc.hollo_app.data.AuthFailure
import com.hlc.hollo_app.data.AuthRepository
import com.hlc.hollo_app.data.remote.User
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

sealed interface SessionStatus {
    data object Loading : SessionStatus
    data class SignedOut(val message: String? = null) : SessionStatus
    data class SignedIn(val user: User) : SessionStatus
}

class SessionViewModel(private val repository: AuthRepository) : ViewModel() {
    private val _status = MutableStateFlow<SessionStatus>(SessionStatus.Loading)
    val status: StateFlow<SessionStatus> = _status.asStateFlow()

    init {
        restoreSession()
    }

    fun onLogin(user: User) {
        _status.value = SessionStatus.SignedIn(user)
    }

    fun retry() {
        _status.value = SessionStatus.Loading
        restoreSession()
    }

    fun logout() {
        viewModelScope.launch {
            repository.logout()
            _status.value = SessionStatus.SignedOut()
        }
    }

    private fun restoreSession() {
        if (!repository.hasStoredSession()) {
            _status.value = SessionStatus.SignedOut()
            return
        }
        viewModelScope.launch {
            try {
                _status.value = SessionStatus.SignedIn(repository.currentUser())
            } catch (error: AuthException) {
                val message = if (error.failure == AuthFailure.Network) {
                    "Não foi possível validar sua sessão. Tente novamente."
                } else null
                _status.update { SessionStatus.SignedOut(message) }
            }
        }
    }
}
