[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker não foi encontrado. Inicie ou instale o Docker Desktop."
}

Push-Location $projectRoot
try {
    docker compose pull
    docker compose up --build -d
}
finally {
    Pop-Location
}

Write-Host "Hollo atualizado." -ForegroundColor Green
