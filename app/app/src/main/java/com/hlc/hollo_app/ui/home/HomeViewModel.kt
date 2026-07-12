package com.hlc.hollo_app.ui.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import android.net.Uri
import com.hlc.hollo_app.data.FilesException
import com.hlc.hollo_app.data.FilesRepository
import com.hlc.hollo_app.data.remote.StoredFolder
import com.hlc.hollo_app.data.remote.StoredFile
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

enum class DriveSection(val title: String) {
    DRIVE("Meu Drive"),
    TRASH("Lixeira"),
    STARRED("Com estrela"),
}

data class HomeUiState(
    val section: DriveSection = DriveSection.DRIVE,
    val folders: List<StoredFolder> = emptyList(),
    val files: List<StoredFile> = emptyList(),
    val breadcrumbs: List<StoredFolder> = emptyList(),
    val fileCount: Int = 0,
    val isLoading: Boolean = true,
    val errorMessage: String? = null,
    val isUploading: Boolean = false,
    val uploadProgress: Int = 0,
    val uploadMessage: String? = null,
    val isCreatingFolder: Boolean = false,
    val isMovingToTrash: Boolean = false,
)

class HomeViewModel(private val repository: FilesRepository) : ViewModel() {
    private val _state = MutableStateFlow(HomeUiState())
    val state: StateFlow<HomeUiState> = _state.asStateFlow()

    init { load() }

    fun selectSection(section: DriveSection) {
        if (_state.value.section == section && !_state.value.isLoading) {
            if (section == DriveSection.DRIVE && _state.value.breadcrumbs.isNotEmpty()) {
                load(folderId = null)
            }
            return
        }
        _state.update { it.copy(section = section, breadcrumbs = emptyList()) }
        load()
    }

    fun openFolder(folder: StoredFolder) {
        if (_state.value.section != DriveSection.DRIVE) return
        load(folder.id)
    }

    fun openBreadcrumb(folder: StoredFolder?) {
        if (_state.value.section == DriveSection.DRIVE) load(folder?.id)
    }

    fun retry() = load(currentFolderId())

    fun upload(uri: Uri) {
        if (_state.value.isUploading) return
        viewModelScope.launch {
            _state.update { it.copy(isUploading = true, uploadProgress = 0, uploadMessage = null) }
            try {
                repository.upload(uri, currentFolderId()) { progress ->
                    _state.update { it.copy(uploadProgress = progress) }
                }
                _state.update { it.copy(isUploading = false, uploadMessage = "Arquivo enviado com sucesso.") }
                load(currentFolderId())
            } catch (error: FilesException) {
                _state.update {
                    it.copy(
                        isUploading = false,
                        uploadMessage = if (error.isNetworkError) "Falha de conexão durante o envio." else "Não foi possível enviar o arquivo.",
                    )
                }
            }
        }
    }

    fun consumeUploadMessage() = _state.update { it.copy(uploadMessage = null) }

    fun createFolder(name: String) {
        val normalizedName = name.trim()
        if (normalizedName.isEmpty() || _state.value.isCreatingFolder) return

        viewModelScope.launch {
            _state.update { it.copy(isCreatingFolder = true, uploadMessage = null) }
            try {
                repository.createFolder(normalizedName, currentFolderId())
                _state.update {
                    it.copy(isCreatingFolder = false, uploadMessage = "Pasta criada com sucesso.")
                }
                load(currentFolderId())
            } catch (error: FilesException) {
                _state.update {
                    it.copy(
                        isCreatingFolder = false,
                        uploadMessage = if (error.isNetworkError) {
                            "Falha de conexão ao criar a pasta."
                        } else {
                            "Não foi possível criar a pasta."
                        },
                    )
                }
            }
        }
    }

    fun moveFileToTrash(file: StoredFile) = moveToTrash(
        successMessage = "Arquivo movido para a lixeira.",
        action = { repository.moveFileToTrash(file.id) },
    )

    fun moveFolderToTrash(folder: StoredFolder) = moveToTrash(
        successMessage = "Pasta movida para a lixeira.",
        action = { repository.moveFolderToTrash(folder.id) },
    )

    private fun moveToTrash(successMessage: String, action: suspend () -> Unit) {
        if (_state.value.isMovingToTrash) return
        viewModelScope.launch {
            _state.update { it.copy(isMovingToTrash = true, uploadMessage = null) }
            try {
                action()
                _state.update { it.copy(isMovingToTrash = false, uploadMessage = successMessage) }
                load(if (_state.value.section == DriveSection.DRIVE) currentFolderId() else null)
            } catch (error: FilesException) {
                _state.update {
                    it.copy(
                        isMovingToTrash = false,
                        uploadMessage = if (error.isNetworkError) {
                            "Falha de conexão ao mover para a lixeira."
                        } else {
                            "Não foi possível mover o item para a lixeira."
                        },
                    )
                }
            }
        }
    }

    private fun currentFolderId() = _state.value.breadcrumbs.lastOrNull()?.id

    private fun load(folderId: String? = null) {
        viewModelScope.launch {
            _state.update { it.copy(isLoading = true, errorMessage = null) }
            try {
                when (_state.value.section) {
                    DriveSection.DRIVE -> repository.browse(folderId).let { listing ->
                        _state.update {
                            it.copy(
                                folders = listing.folders,
                                files = listing.files,
                                breadcrumbs = listing.breadcrumbs,
                                fileCount = listing.files.size,
                                isLoading = false,
                            )
                        }
                    }
                    DriveSection.STARRED -> repository.starred().let { listing ->
                        _state.update { it.copy(folders = listing.folders, files = listing.files, fileCount = listing.files.size, isLoading = false) }
                    }
                    DriveSection.TRASH -> repository.trash().let { listing ->
                        _state.update { it.copy(folders = listing.folders, files = listing.files, fileCount = listing.files.size, isLoading = false) }
                    }
                }
            } catch (error: FilesException) {
                _state.update {
                    it.copy(
                        isLoading = false,
                        errorMessage = if (error.isNetworkError) {
                            "Não foi possível conectar ao Hollo."
                        } else "Não foi possível carregar suas pastas.",
                    )
                }
            }
        }
    }
}
