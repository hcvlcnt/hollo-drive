[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker não foi encontrado. Inicie ou instale o Docker Desktop."
}

Push-Location $projectRoot
try {
    if ($NoBuild) {
        docker compose up -d
    }
    else {
        docker compose up --build -d
    }
}
finally {
    Pop-Location
}

Write-Host "Containers do Hollo iniciados sem privilégios administrativos." -ForegroundColor Green
