package com.hlc.hollo_app.ui.components

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBars
import androidx.compose.foundation.layout.windowInsetsPadding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.CloudOff
import androidx.compose.material.icons.rounded.QrCodeScanner
import androidx.compose.material.icons.rounded.Sync
import androidx.compose.material3.AssistChip
import androidx.compose.material3.AssistChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.hlc.hollo_app.data.server.ServerConfigStore
import com.hlc.hollo_app.data.server.ServerPairingPayload
import com.journeyapps.barcodescanner.ScanContract
import com.journeyapps.barcodescanner.ScanOptions
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import okhttp3.HttpUrl.Companion.toHttpUrl
import okhttp3.OkHttpClient
import okhttp3.Request
import org.koin.compose.koinInject

private sealed interface ServerState {
    data object Checking : ServerState
    data object Available : ServerState
    data object Unavailable : ServerState
}

@Composable
fun ServerStatusBar(
    client: OkHttpClient = koinInject(),
    store: ServerConfigStore = koinInject(),
    json: Json = koinInject(),
) {
    val configuredUrl by store.baseUrl.collectAsStateWithLifecycle()
    val baseUrl = remember(configuredUrl) { configuredUrl.toHttpUrl() }
    val pairingUrl = remember(baseUrl) {
        baseUrl.newBuilder()
            .encodedPath(baseUrl.encodedPath.trimEnd('/') + "/server/pairing")
            .query(null)
            .build()
    }
    var state: ServerState by remember { mutableStateOf(ServerState.Checking) }
    var retry by remember { mutableIntStateOf(0) }
    var scannerError by remember { mutableStateOf<String?>(null) }
    val scanner = rememberLauncherForActivityResult(ScanContract()) { result ->
        val contents = result.contents ?: return@rememberLauncherForActivityResult
        runCatching {
            store.save(json.decodeFromString<ServerPairingPayload>(contents))
        }.onSuccess {
            scannerError = null
            retry++
        }.onFailure {
            scannerError = "QR Code do Hollo inválido"
        }
    }

    LaunchedEffect(pairingUrl, retry) {
        var hasConnected = false
        var consecutiveFailures = 0
        while (true) {
            if (!hasConnected) state = ServerState.Checking
            val isAvailable = withContext(Dispatchers.IO) {
                runCatching {
                    client.newCall(Request.Builder().url(pairingUrl).get().build()).execute().use {
                        it.isSuccessful
                    }
                }.getOrDefault(false)
            }

            if (isAvailable) {
                hasConnected = true
                consecutiveFailures = 0
                state = ServerState.Available
                delay(10_000)
            } else {
                consecutiveFailures++
                if (!hasConnected || consecutiveFailures >= 2) {
                    state = ServerState.Unavailable
                }
                delay(if (hasConnected && consecutiveFailures < 2) 2_000 else 10_000)
            }
        }
    }

    val available = state is ServerState.Available
    val unavailable = state is ServerState.Unavailable
    val color = if (unavailable) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurfaceVariant
    val containerColor = if (unavailable) MaterialTheme.colorScheme.errorContainer else MaterialTheme.colorScheme.surfaceVariant

    if (available && scannerError == null) return

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .background(MaterialTheme.colorScheme.background)
            .windowInsetsPadding(WindowInsets.statusBars)
            .padding(horizontal = 12.dp, vertical = 4.dp),
        contentAlignment = Alignment.Center,
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            if (!available || scannerError != null) {
                AssistChip(
                    onClick = { retry++ },
                    label = {
                        Text(
                            scannerError ?: if (state is ServerState.Checking) {
                                "Procurando servidor Hollo..."
                            } else {
                                "Servidor Hollo não encontrado"
                            },
                        )
                    },
                    leadingIcon = {
                        Icon(
                            imageVector = if (state is ServerState.Checking) Icons.Rounded.Sync else Icons.Rounded.CloudOff,
                            contentDescription = null,
                        )
                    },
                    colors = AssistChipDefaults.assistChipColors(
                        containerColor = containerColor,
                        labelColor = color,
                        leadingIconContentColor = color,
                    ),
                    border = AssistChipDefaults.assistChipBorder(
                        enabled = true,
                        borderColor = color.copy(alpha = 0.35f),
                    ),
                )
            }
            IconButton(
                onClick = {
                    scanner.launch(
                        ScanOptions()
                            .setDesiredBarcodeFormats(ScanOptions.QR_CODE)
                            .setPrompt("Aponte para o QR Code do servidor Hollo")
                            .setBeepEnabled(false)
                            .setOrientationLocked(false),
                    )
                },
            ) {
                Icon(Icons.Rounded.QrCodeScanner, contentDescription = "Conectar servidor por QR Code")
            }
        }
    }
}
