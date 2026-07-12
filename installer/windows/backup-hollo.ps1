[CmdletBinding()]
param(
    [string]$Destination = (Join-Path $HOME "Hollo Backups")
)

$ErrorActionPreference = "Stop"
$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = Join-Path $Destination "hollo-$timestamp"
$storagePath = Join-Path $backupPath "storage"
New-Item -ItemType Directory -Force $storagePath | Out-Null

Push-Location $projectRoot
try {
    docker compose exec -T postgres pg_dump -U postgres -d hollo |
        Set-Content -LiteralPath (Join-Path $backupPath "database.sql") -Encoding utf8
    docker compose cp "azurite:/data/." $storagePath
    if (Test-Path ".env") {
        Copy-Item -LiteralPath ".env" -Destination (Join-Path $backupPath "hollo.env")
    }
}
finally {
    Pop-Location
}

Write-Host "Backup criado em $backupPath" -ForegroundColor Green
