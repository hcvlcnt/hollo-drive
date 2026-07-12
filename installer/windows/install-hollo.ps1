#requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$ServerName = "Hollo Casa",
    [int]$ApiPort = 8080,
    [int]$WebPort = 5173,
    [switch]$KeepPublicNetwork
)

$ErrorActionPreference = "Stop"
$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

$defaultRoute = Get-NetRoute -DestinationPrefix "0.0.0.0/0" |
    Sort-Object RouteMetric, InterfaceMetric |
    Select-Object -First 1
if (-not $defaultRoute) {
    throw "Nenhuma rota de rede IPv4 ativa foi encontrada."
}

$address = Get-NetIPAddress -InterfaceIndex $defaultRoute.InterfaceIndex -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -notlike "169.254.*" } |
    Select-Object -First 1
if (-not $address) {
    throw "Nenhum endereço IPv4 válido foi encontrado na interface ativa."
}

$networkProfile = Get-NetConnectionProfile -InterfaceIndex $defaultRoute.InterfaceIndex
if ($networkProfile.NetworkCategory -eq "Public" -and -not $KeepPublicNetwork) {
    try {
        Set-NetConnectionProfile `
            -InterfaceIndex $defaultRoute.InterfaceIndex `
            -NetworkCategory Private
        $networkProfile = Get-NetConnectionProfile -InterfaceIndex $defaultRoute.InterfaceIndex
    }
    catch {
        throw "Não foi possível definir a rede como Privada. O Docker Desktop bloqueia conexões de entrada no perfil Público. Altere o perfil em Configurações > Rede e Internet > Ethernet e execute o instalador novamente."
    }
}

if ($networkProfile.NetworkCategory -eq "Public") {
    Write-Warning "A rede continua Pública. Regras explícitas do Docker Desktop podem bloquear a API mesmo com a porta 8080 permitida."
}

$publicUrl = "http://$($address.IPAddress):$ApiPort/api/"
$envPath = Join-Path $projectRoot ".env"
$existingServerId = if (Test-Path $envPath) {
    (Get-Content -LiteralPath $envPath |
        Where-Object { $_ -like "HOLLO_SERVER_ID=*" } |
        Select-Object -First 1) -replace "^HOLLO_SERVER_ID=", ""
}
$serverId = if ($existingServerId) {
    $existingServerId
}
else {
    "hollo-" + ([guid]::NewGuid().ToString("N"))
}
@(
    "HOLLO_SERVER_ID=$serverId"
    "HOLLO_SERVER_NAME=$ServerName"
    "HOLLO_PUBLIC_URL=$publicUrl"
) | Set-Content -LiteralPath $envPath -Encoding utf8

$ruleName = "Hollo Server (LAN)"
$existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if ($existingRule) {
    $existingRule | Remove-NetFirewallRule
}
New-NetFirewallRule `
    -DisplayName $ruleName `
    -Description "Permite acesso à API Hollo somente a partir da sub-rede local." `
    -Direction Inbound `
    -Action Allow `
    -Protocol TCP `
    -LocalPort $ApiPort,$WebPort `
    -RemoteAddress LocalSubnet `
    -Profile Any | Out-Null

Write-Host "Hollo Server configurado." -ForegroundColor Green
Write-Host "Nome: $ServerName"
Write-Host "API:  $publicUrl"
Write-Host "Firewall: TCP $ApiPort e $WebPort, origem LocalSubnet"
Write-Host "Execute .\installer\windows\start-hollo.ps1 em um PowerShell comum para iniciar os containers."
