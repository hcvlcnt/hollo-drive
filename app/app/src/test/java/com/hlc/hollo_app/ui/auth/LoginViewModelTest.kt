package com.hlc.hollo_app.ui.auth

import com.hlc.hollo_app.data.AuthException
import com.hlc.hollo_app.data.AuthFailure
import com.hlc.hollo_app.data.AuthRepository
import com.hlc.hollo_app.data.remote.User
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.async
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class LoginViewModelTest {
    private val dispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() = Dispatchers.setMain(dispatcher)

    @After
    fun tearDown() = Dispatchers.resetMain()

    @Test
    fun `campos vazios exibem validacao sem chamar repositorio`() {
        val repository = FakeAuthRepository()
        val viewModel = LoginViewModel(repository)

        viewModel.submit()

        assertEquals("Informe seu e-mail.", viewModel.state.value.errorMessage)
        assertEquals(0, repository.loginCalls)
    }

    @Test
    fun `email invalido exibe mensagem local`() {
        val viewModel = LoginViewModel(FakeAuthRepository())
        viewModel.onEmailChange("email-invalido")
        viewModel.onPasswordChange("senha")

        viewModel.submit()

        assertEquals("Informe um e-mail válido.", viewModel.state.value.errorMessage)
    }

    @Test
    fun `login valido emite usuario e encerra carregamento`() = runTest {
        val repository = FakeAuthRepository()
        val viewModel = LoginViewModel(repository)
        viewModel.onEmailChange("admin@hollo.local")
        viewModel.onPasswordChange("Admin@123456")
        val event = async { viewModel.events.first() }

        viewModel.submit()

        assertEquals(repository.user, (event.await() as LoginEvent.Success).user)
        assertFalse(viewModel.state.value.isLoading)
        assertEquals(1, repository.loginCalls)
    }

    @Test
    fun `credenciais rejeitadas usam mensagem em portugues`() {
        val repository = FakeAuthRepository(failure = AuthFailure.InvalidCredentials)
        val viewModel = LoginViewModel(repository)
        viewModel.onEmailChange("admin@hollo.local")
        viewModel.onPasswordChange("errada")

        viewModel.submit()

        assertEquals("E-mail ou senha inválidos.", viewModel.state.value.errorMessage)
        assertFalse(viewModel.state.value.isLoading)
    }

    @Test
    fun `visibilidade da senha alterna`() {
        val viewModel = LoginViewModel(FakeAuthRepository())
        assertFalse(viewModel.state.value.passwordVisible)
        viewModel.togglePasswordVisibility()
        assertTrue(viewModel.state.value.passwordVisible)
    }
}

private class FakeAuthRepository(
    private val failure: AuthFailure? = null,
) : AuthRepository {
    var loginCalls = 0
    val user = User(
        id = "1",
        name = "Admin",
        email = "admin@hollo.local",
        role = "admin",
        isActive = true,
        createdAt = "2026-07-10T00:00:00Z",
    )

    override fun hasStoredSession() = false

    override suspend fun login(email: String, password: String): User {
        loginCalls++
        failure?.let { throw AuthException(it) }
        return user
    }

    override suspend fun currentUser() = user
    override suspend fun logout() = Unit
}
