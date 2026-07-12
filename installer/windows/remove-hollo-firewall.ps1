#requires -RunAsAdministrator
Get-NetFirewallRule -DisplayName "Hollo Server (LAN)" -ErrorAction SilentlyContinue |
    Remove-NetFirewallRule

Write-Host "Regra de firewall do Hollo removida."
