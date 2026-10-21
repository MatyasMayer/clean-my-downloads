<#
.SYNOPSIS
  Removes the scheduled task, the Start-menu shortcuts and the program files.
  Never removes the Downloads folders, settings.json or move.log.
#>
[CmdletBinding()]
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'clean-my-downloads'),
    [string]$StartMenuDir = (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Clean My Downloads'),
    [string]$TaskName = 'clean-my-downloads',
    [switch]$SkipTask
)
$ErrorActionPreference = 'Stop'

if (-not $SkipTask -and (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue)) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}
if (Test-Path $StartMenuDir) { Remove-Item $StartMenuDir -Recurse -Force }

$keep = 'settings.json', 'move.log'
if (Test-Path $InstallDir) {
    Get-ChildItem $InstallDir -Force | Where-Object { $keep -notcontains $_.Name } |
        Remove-Item -Recurse -Force
    if (-not (Get-ChildItem $InstallDir -Force)) { Remove-Item $InstallDir -Force }
}
Write-Host 'Uninstalled (settings.json and move.log were kept).'
