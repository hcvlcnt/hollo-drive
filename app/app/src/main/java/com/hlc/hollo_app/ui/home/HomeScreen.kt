package com.hlc.hollo_app.ui.home

import android.content.Intent
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.GridItemSpan
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.InsertDriveFile
import androidx.compose.material.icons.automirrored.rounded.Logout
import androidx.compose.material.icons.rounded.DeleteOutline
import androidx.compose.material.icons.rounded.Folder
import androidx.compose.material.icons.rounded.FolderOpen
import androidx.compose.material.icons.rounded.AudioFile
import androidx.compose.material.icons.rounded.Code
import androidx.compose.material.icons.rounded.Description
import androidx.compose.material.icons.rounded.Image
import androidx.compose.material.icons.rounded.VideoFile
import androidx.compose.material.icons.rounded.Home
import androidx.compose.material.icons.rounded.Menu
import androidx.compose.material.icons.rounded.MoreVert
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.CreateNewFolder
import androidx.compose.material.icons.rounded.UploadFile
import androidx.compose.material.icons.rounded.PersonOutline
import androidx.compose.material.icons.rounded.Schedule
import androidx.compose.material.icons.rounded.Star
import androidx.compose.material.icons.rounded.StarOutline
import androidx.compose.material3.Button
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Divider
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalDrawerSheet
import androidx.compose.material3.ModalNavigationDrawer
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationDrawerItem
import androidx.compose.material3.NavigationDrawerItemDefaults
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.material3.rememberDrawerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.hlc.hollo_app.data.remote.StoredFolder
import com.hlc.hollo_app.data.remote.StoredFile
import com.hlc.hollo_app.data.remote.User
import com.hlc.hollo_app.ui.components.HolloLogo
import kotlinx.coroutines.launch
import org.koin.androidx.compose.koinViewModel

private val FolderAmber = Color(0xFFD97706)
private val FolderBackground = Color(0xFFFFF7E6)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    user: User,
    onLogout: () -> Unit,
    viewModel: HomeViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val drawerState = rememberDrawerState(DrawerValue.Closed)
    val scope = rememberCoroutineScope()
    val context = LocalContext.current
    val snackbarHostState = remember { SnackbarHostState() }
    var addMenuExpanded by remember { mutableStateOf(false) }
    var createFolderDialogOpen by remember { mutableStateOf(false) }
    val filePicker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) {
            runCatching {
                context.contentResolver.takePersistableUriPermission(uri, Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            viewModel.upload(uri)
        }
    }

    LaunchedEffect(state.uploadMessage) {
        state.uploadMessage?.let {
            snackbarHostState.showSnackbar(it)
            viewModel.consumeUploadMessage()
        }
    }

    ModalNavigationDrawer(
        drawerState = drawerState,
        drawerContent = {
            HolloDrawer(
                user = user,
                activeSection = state.section,
                onSectionSelected = {
                    viewModel.selectSection(it)
                    scope.launch { drawerState.close() }
                },
                onLogout = onLogout,
            )
        },
    ) {
        Scaffold(
            containerColor = MaterialTheme.colorScheme.background,
            topBar = {
                TopAppBar(
                    title = {
                        Column {
                            Text(state.section.title, fontWeight = FontWeight.SemiBold)
                            if (state.section == DriveSection.DRIVE && state.breadcrumbs.isNotEmpty()) {
                                Text(
                                    state.breadcrumbs.last().name,
                                    style = MaterialTheme.typography.labelMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                )
                            }
                        }
                    },
                    navigationIcon = {
                        IconButton(onClick = { scope.launch { drawerState.open() } }) {
                            Icon(Icons.Rounded.Menu, contentDescription = "Abrir menu")
                        }
                    },
                    actions = {
                        IconButton(onClick = { scope.launch { drawerState.open() } }) {
                            Icon(Icons.Rounded.MoreVert, contentDescription = "Mais opções")
                        }
                    },
                    colors = TopAppBarDefaults.topAppBarColors(
                        containerColor = MaterialTheme.colorScheme.background,
                    ),
                )
            },
            bottomBar = {
                MainNavigationBar(
                    selected = state.section,
                    onSelect = viewModel::selectSection,
                )
            },
            snackbarHost = { SnackbarHost(snackbarHostState) },
            floatingActionButton = {
                if (state.section == DriveSection.DRIVE) {
                    Box {
                        ExtendedFloatingActionButton(
                            onClick = { if (!state.isUploading) addMenuExpanded = true },
                            icon = {
                                if (state.isUploading) {
                                    CircularProgressIndicator(modifier = Modifier.size(20.dp), strokeWidth = 2.dp)
                                } else Icon(Icons.Rounded.Add, contentDescription = null)
                            },
                            text = {
                                Text(if (state.isUploading) "Enviando ${state.uploadProgress}%" else "Adicionar")
                            },
                        )
                        DropdownMenu(
                            expanded = addMenuExpanded,
                            onDismissRequest = { addMenuExpanded = false },
                        ) {
                            DropdownMenuItem(
                                text = { Text("Enviar arquivo") },
                                leadingIcon = { Icon(Icons.Rounded.UploadFile, contentDescription = null) },
                                onClick = {
                                    addMenuExpanded = false
                                    filePicker.launch(arrayOf("*/*"))
                                },
                            )
                            DropdownMenuItem(
                                text = { Text("Nova pasta") },
                                leadingIcon = { Icon(Icons.Rounded.CreateNewFolder, contentDescription = null) },
                                onClick = {
                                    addMenuExpanded = false
                                    createFolderDialogOpen = true
                                },
                            )
                        }
                    }
                }
            },
        ) { padding ->
            HomeContent(
                state = state,
                modifier = Modifier.padding(padding),
                onFolderOpen = viewModel::openFolder,
                onBreadcrumbOpen = viewModel::openBreadcrumb,
                onFolderTrash = viewModel::moveFolderToTrash,
                onFileTrash = viewModel::moveFileToTrash,
                onRetry = viewModel::retry,
            )
        }
    }

    if (createFolderDialogOpen) {
        CreateFolderDialog(
            isCreating = state.isCreatingFolder,
            onDismiss = { if (!state.isCreatingFolder) createFolderDialogOpen = false },
            onCreate = { name ->
                viewModel.createFolder(name)
                createFolderDialogOpen = false
            },
        )
    }
}

@Composable
private fun CreateFolderDialog(
    isCreating: Boolean,
    onDismiss: () -> Unit,
    onCreate: (String) -> Unit,
) {
    var name by remember { mutableStateOf("") }
    val normalizedName = name.trim()

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Nova pasta") },
        text = {
            OutlinedTextField(
                value = name,
                onValueChange = { name = it },
                label = { Text("Nome da pasta") },
                singleLine = true,
                enabled = !isCreating,
                modifier = Modifier.fillMaxWidth(),
            )
        },
        confirmButton = {
            Button(
                onClick = { onCreate(normalizedName) },
                enabled = normalizedName.isNotEmpty() && !isCreating,
            ) {
                if (isCreating) {
                    CircularProgressIndicator(modifier = Modifier.size(18.dp), strokeWidth = 2.dp)
                } else {
                    Text("Criar")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isCreating) { Text("Cancelar") }
        },
    )
}

@Composable
private fun HomeContent(
    state: HomeUiState,
    modifier: Modifier,
    onFolderOpen: (StoredFolder) -> Unit,
    onBreadcrumbOpen: (StoredFolder?) -> Unit,
    onFolderTrash: (StoredFolder) -> Unit,
    onFileTrash: (StoredFile) -> Unit,
    onRetry: () -> Unit,
) {
    Column(modifier.fillMaxSize()) {
        if (state.section == DriveSection.DRIVE) {
            Breadcrumbs(state.breadcrumbs, onBreadcrumbOpen)
        }
        when {
            state.isLoading -> LoadingFolders()
            state.errorMessage != null -> ErrorFolders(state.errorMessage, onRetry)
            state.folders.isEmpty() && state.files.isEmpty() -> EmptyFolders(state.section)
            else -> LazyVerticalGrid(
                columns = GridCells.Fixed(2),
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, bottom = 24.dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                if (state.folders.isNotEmpty()) {
                    item(span = { GridItemSpan(maxLineSpan) }) {
                        SectionTitle("PASTAS")
                    }
                }
                items(state.folders, key = { it.id }) { folder ->
                    FolderCard(
                        folder = folder,
                        section = state.section,
                        onClick = { onFolderOpen(folder) },
                        onMoveToTrash = { onFolderTrash(folder) },
                    )
                }
                if (state.files.isNotEmpty()) {
                    item(span = { GridItemSpan(maxLineSpan) }) {
                        SectionTitle("ARQUIVOS", topPadding = if (state.folders.isEmpty()) 0.dp else 14.dp)
                    }
                }
                items(state.files, key = { it.id }) { file ->
                    FileCard(file, state.section, onMoveToTrash = { onFileTrash(file) })
                }
            }
        }
    }
}

@Composable
private fun SectionTitle(title: String, topPadding: androidx.compose.ui.unit.Dp = 0.dp) {
    Text(
        text = title,
        style = MaterialTheme.typography.labelMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
        modifier = Modifier.padding(start = 4.dp, top = topPadding, bottom = 10.dp),
    )
}

@Composable
private fun Breadcrumbs(items: List<StoredFolder>, onOpen: (StoredFolder?) -> Unit) {
    LazyRow(
        contentPadding = PaddingValues(horizontal = 16.dp),
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.fillMaxWidth(),
    ) {
        item {
            Text(
                text = "Meu Drive",
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = if (items.isEmpty()) FontWeight.SemiBold else FontWeight.Normal,
                color = if (items.isEmpty()) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.primary,
                modifier = Modifier
                    .clip(RoundedCornerShape(8.dp))
                    .clickable { onOpen(null) }
                    .padding(horizontal = 5.dp, vertical = 8.dp),
            )
        }
        items(items.size) { index ->
            Text("/", color = MaterialTheme.colorScheme.outline, modifier = Modifier.padding(horizontal = 3.dp))
            val folder = items[index]
            Text(
                text = folder.name,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                fontWeight = if (index == items.lastIndex) FontWeight.SemiBold else FontWeight.Normal,
                color = if (index == items.lastIndex) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.primary,
                modifier = Modifier
                    .clip(RoundedCornerShape(8.dp))
                    .clickable { onOpen(folder) }
                    .padding(horizontal = 5.dp, vertical = 8.dp),
            )
        }
    }
}

@Composable
private fun FolderCard(
    folder: StoredFolder,
    section: DriveSection,
    onClick: () -> Unit,
    onMoveToTrash: () -> Unit,
) {
    var menuExpanded by remember { mutableStateOf(false) }
    var confirmTrash by remember { mutableStateOf(false) }
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .height(132.dp)
            .clickable(enabled = section == DriveSection.DRIVE, onClick = onClick),
        shape = RoundedCornerShape(18.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp),
        border = androidx.compose.foundation.BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant),
    ) {
        Column(Modifier.fillMaxSize().padding(14.dp)) {
            Row(verticalAlignment = Alignment.Top) {
                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .clip(RoundedCornerShape(13.dp))
                        .background(FolderBackground),
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(Icons.Rounded.Folder, contentDescription = null, tint = FolderAmber)
                }
                Spacer(Modifier.weight(1f))
                if (folder.isStarred) {
                    Icon(Icons.Rounded.Star, contentDescription = "Com estrela", tint = FolderAmber, modifier = Modifier.size(18.dp))
                }
                if (section != DriveSection.TRASH) {
                    Box {
                        IconButton(onClick = { menuExpanded = true }, modifier = Modifier.size(28.dp)) {
                            Icon(Icons.Rounded.MoreVert, contentDescription = "Opções da pasta", modifier = Modifier.size(18.dp))
                        }
                        DropdownMenu(expanded = menuExpanded, onDismissRequest = { menuExpanded = false }) {
                            DropdownMenuItem(
                                text = { Text("Mover para a lixeira") },
                                leadingIcon = { Icon(Icons.Rounded.DeleteOutline, contentDescription = null) },
                                onClick = {
                                    menuExpanded = false
                                    confirmTrash = true
                                },
                            )
                        }
                    }
                }
            }
            Spacer(Modifier.weight(1f))
            Text(
                folder.name,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.SemiBold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
            Text(
                if (section == DriveSection.TRASH) "Na lixeira" else "Pasta",
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 2.dp),
            )
        }
    }
    if (confirmTrash) {
        TrashConfirmationDialog(
            itemName = folder.name,
            itemType = "pasta",
            onDismiss = { confirmTrash = false },
            onConfirm = {
                confirmTrash = false
                onMoveToTrash()
            },
        )
    }
}

@Composable
private fun FileCard(file: StoredFile, section: DriveSection, onMoveToTrash: () -> Unit) {
    val visual = fileVisual(file)
    var menuExpanded by remember { mutableStateOf(false) }
    var confirmTrash by remember { mutableStateOf(false) }
    Card(
        modifier = Modifier.fillMaxWidth().height(132.dp),
        shape = RoundedCornerShape(18.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp),
        border = androidx.compose.foundation.BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant),
    ) {
        Column(Modifier.fillMaxSize().padding(14.dp)) {
            Row(verticalAlignment = Alignment.Top) {
                Box(
                    modifier = Modifier.size(44.dp).clip(RoundedCornerShape(13.dp)).background(visual.background),
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(visual.icon, contentDescription = null, tint = visual.tint)
                }
                Spacer(Modifier.weight(1f))
                if (file.isStarred) {
                    Icon(Icons.Rounded.Star, contentDescription = "Com estrela", tint = FolderAmber, modifier = Modifier.size(18.dp))
                }
                if (section != DriveSection.TRASH) {
                    Box {
                        IconButton(onClick = { menuExpanded = true }, modifier = Modifier.size(28.dp)) {
                            Icon(Icons.Rounded.MoreVert, contentDescription = "Opções do arquivo", modifier = Modifier.size(18.dp))
                        }
                        DropdownMenu(expanded = menuExpanded, onDismissRequest = { menuExpanded = false }) {
                            DropdownMenuItem(
                                text = { Text("Mover para a lixeira") },
                                leadingIcon = { Icon(Icons.Rounded.DeleteOutline, contentDescription = null) },
                                onClick = {
                                    menuExpanded = false
                                    confirmTrash = true
                                },
                            )
                        }
                    }
                }
            }
            Spacer(Modifier.weight(1f))
            Text(
                file.name,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.SemiBold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
            Text(
                formatFileSize(file.sizeInBytes),
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 2.dp),
            )
        }
    }
    if (confirmTrash) {
        TrashConfirmationDialog(
            itemName = file.name,
            itemType = "arquivo",
            onDismiss = { confirmTrash = false },
            onConfirm = {
                confirmTrash = false
                onMoveToTrash()
            },
        )
    }
}

@Composable
private fun TrashConfirmationDialog(
    itemName: String,
    itemType: String,
    onDismiss: () -> Unit,
    onConfirm: () -> Unit,
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Mover para a lixeira?") },
        text = { Text("O $itemType “$itemName” poderá ser restaurado posteriormente.") },
        confirmButton = {
            Button(onClick = onConfirm) { Text("Mover") }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Cancelar") }
        },
    )
}

private data class FileVisual(val icon: ImageVector, val tint: Color, val background: Color)

private fun fileVisual(file: StoredFile): FileVisual = when {
    file.contentType.startsWith("image/") -> FileVisual(Icons.Rounded.Image, Color(0xFF0284C7), Color(0xFFEAF7FE))
    file.contentType.startsWith("video/") -> FileVisual(Icons.Rounded.VideoFile, Color(0xFFDB2777), Color(0xFFFCEEF5))
    file.contentType.startsWith("audio/") -> FileVisual(Icons.Rounded.AudioFile, Color(0xFF4D7C0F), Color(0xFFF3F9E8))
    file.extension?.lowercase() in setOf("kt", "java", "js", "ts", "tsx", "jsx", "cs", "py", "html", "css", "json", "xml") ->
        FileVisual(Icons.Rounded.Code, Color(0xFF0891B2), Color(0xFFEAF9FC))
    file.contentType.startsWith("text/") || file.extension?.lowercase() in setOf("pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx") ->
        FileVisual(Icons.Rounded.Description, Color(0xFFDC2626), Color(0xFFFEF0F0))
    else -> FileVisual(Icons.AutoMirrored.Rounded.InsertDriveFile, Color(0xFF78716C), Color(0xFFF5F5F4))
}

private fun formatFileSize(bytes: Long): String {
    if (bytes < 1024) return "$bytes B"
    val units = listOf("KB", "MB", "GB", "TB")
    var value = bytes / 1024.0
    var unit = 0
    while (value >= 1024 && unit < units.lastIndex) {
        value /= 1024
        unit++
    }
    return if (value >= 10) "%.0f %s".format(value, units[unit]) else "%.1f %s".format(value, units[unit])
}

@Composable
private fun MainNavigationBar(selected: DriveSection, onSelect: (DriveSection) -> Unit) {
    NavigationBar(containerColor = MaterialTheme.colorScheme.surface) {
        NavigationBarItem(
            selected = selected == DriveSection.DRIVE,
            onClick = { onSelect(DriveSection.DRIVE) },
            icon = { Icon(Icons.Rounded.FolderOpen, contentDescription = null) },
            label = { Text("Meu Drive") },
        )
        NavigationBarItem(
            selected = selected == DriveSection.TRASH,
            onClick = { onSelect(DriveSection.TRASH) },
            icon = { Icon(Icons.Rounded.DeleteOutline, contentDescription = null) },
            label = { Text("Lixeira") },
        )
        NavigationBarItem(
            selected = selected == DriveSection.STARRED,
            onClick = { onSelect(DriveSection.STARRED) },
            icon = { Icon(Icons.Rounded.StarOutline, contentDescription = null) },
            label = { Text("Com estrela") },
        )
    }
}

@Composable
private fun HolloDrawer(
    user: User,
    activeSection: DriveSection,
    onSectionSelected: (DriveSection) -> Unit,
    onLogout: () -> Unit,
) {
    ModalDrawerSheet(modifier = Modifier.fillMaxHeight().width(304.dp)) {
        HolloLogo(modifier = Modifier.padding(24.dp), markSize = 36.dp)
        HorizontalDivider()
        Text("NAVEGAÇÃO", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.padding(24.dp, 20.dp, 24.dp, 8.dp))
        DrawerItem("Início", Icons.Rounded.Home, activeSection == DriveSection.DRIVE) { onSectionSelected(DriveSection.DRIVE) }
        DrawerItem("Recentes", Icons.Rounded.Schedule, false) { }
        DrawerItem("Com estrela", Icons.Rounded.StarOutline, activeSection == DriveSection.STARRED) { onSectionSelected(DriveSection.STARRED) }
        DrawerItem("Meu Drive", Icons.Rounded.FolderOpen, activeSection == DriveSection.DRIVE) { onSectionSelected(DriveSection.DRIVE) }
        DrawerItem("Lixeira", Icons.Rounded.DeleteOutline, activeSection == DriveSection.TRASH) { onSectionSelected(DriveSection.TRASH) }
        Spacer(Modifier.weight(1f))
        HorizontalDivider()
        Row(Modifier.padding(20.dp), verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Rounded.PersonOutline, contentDescription = null)
            Column(Modifier.padding(start = 12.dp).weight(1f)) {
                Text(user.name, fontWeight = FontWeight.SemiBold, maxLines = 1)
                Text(user.email, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant, maxLines = 1, overflow = TextOverflow.Ellipsis)
            }
        }
        NavigationDrawerItem(
            label = { Text("Sair") },
            selected = false,
            onClick = onLogout,
            icon = { Icon(Icons.AutoMirrored.Rounded.Logout, contentDescription = null) },
            modifier = Modifier.padding(NavigationDrawerItemDefaults.ItemPadding).padding(bottom = 16.dp),
        )
    }
}

@Composable
private fun DrawerItem(label: String, icon: ImageVector, selected: Boolean, onClick: () -> Unit) {
    NavigationDrawerItem(
        label = { Text(label) },
        selected = selected,
        onClick = onClick,
        icon = { Icon(icon, contentDescription = null) },
        modifier = Modifier.padding(NavigationDrawerItemDefaults.ItemPadding),
    )
}

@Composable
private fun LoadingFolders() {
    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { CircularProgressIndicator() }
}

@Composable
private fun ErrorFolders(message: String, onRetry: () -> Unit) {
    Column(Modifier.fillMaxSize().padding(32.dp), horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.Center) {
        Text(message, color = MaterialTheme.colorScheme.error)
        Button(onClick = onRetry, modifier = Modifier.padding(top = 16.dp)) { Text("Tentar novamente") }
    }
}

@Composable
private fun EmptyFolders(section: DriveSection) {
    Column(Modifier.fillMaxSize().padding(32.dp), horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.Center) {
        Icon(Icons.Rounded.FolderOpen, contentDescription = null, tint = MaterialTheme.colorScheme.outline, modifier = Modifier.size(52.dp))
        Text(
            when (section) {
                DriveSection.DRIVE -> "Nenhuma pasta por aqui"
                DriveSection.TRASH -> "A lixeira está vazia"
                DriveSection.STARRED -> "Nenhuma pasta com estrela"
            },
            style = MaterialTheme.typography.titleMedium,
            modifier = Modifier.padding(top = 14.dp),
        )
    }
}
