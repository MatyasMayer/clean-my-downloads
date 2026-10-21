<#
.SYNOPSIS
  Installs (or upgrades in place) clean-my-downloads for the current user.
.DESCRIPTION
  Publishes the sorter (framework-dependent) into InstallDir, writes default settings if none exist,
  registers the hourly scheduled task and creates five Start-menu shortcuts.
  Safe to run again: program files are replaced, settings.json and move.log are never touched.
.PARAMETER InstallDir
  Defaults to %LOCALAPPDATA%\clean-my-downloads (also holds settings.json and move.log).
.PARAMETER StartMenuDir
  Folder for the shortcuts. Defaults to a "Clean My Downloads" folder under the user's Start-menu Programs.
.PARAMETER SkipTask
  Do not register the scheduled task (for tests).
.PARAMETER SkipSettingsInit
  Do not run the sorter once to write the default settings (for tests).
#>
[CmdletBinding()]
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'clean-my-downloads'),
    [string]$StartMenuDir = (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Clean My Downloads'),
    [string]$TaskName = 'clean-my-downloads',
    [switch]$SkipTask,
    [switch]$SkipSettingsInit
)
$ErrorActionPreference = 'Stop'

$exeName = 'CleanMyDownloads.Sorter.exe'
$project = Join-Path $PSScriptRoot '..\src\CleanMyDownloads.Sorter\CleanMyDownloads.Sorter.csproj'
$exe = Join-Path $InstallDir $exeName
$settings = Join-Path $InstallDir 'settings.json'
$log = Join-Path $InstallDir 'move.log'

# 1. Publish into a staging folder, then copy over the install dir (settings and log are not in the output).
$staging = Join-Path ([IO.Path]::GetTempPath()) ("cmd-publish-" + [guid]::NewGuid().ToString('N'))
try {
    & dotnet publish $project -c Release --no-self-contained -o $staging --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }
    New-Item -ItemType Directory -Force $InstallDir | Out-Null
    Copy-Item (Join-Path $staging '*') $InstallDir -Recurse -Force
} finally {
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
}

# 2. Default settings: the sorter writes them itself when missing; --init-settings does only that (no move.log entries).
if (-not $SkipSettingsInit -and -not (Test-Path $settings)) {
    Start-Process $exe -ArgumentList '--init-settings' -Wait -WindowStyle Hidden
}

# 3. Hourly task: catch up missed runs, run on battery, no window.
if (-not $SkipTask) {
    $action = New-ScheduledTaskAction -Execute $exe -Argument '--sort'
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Hours 1)
    $taskSettings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries -Hidden -ExecutionTimeLimit (New-TimeSpan -Hours 1)
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $taskSettings `
        -Description 'Sorts the Downloads folder (clean-my-downloads)' -Force | Out-Null
}

# 4. Start-menu shortcuts (recreated each time).
New-Item -ItemType Directory -Force $StartMenuDir | Out-Null
$shell = New-Object -ComObject WScript.Shell
$shortcuts = [ordered]@{
    'Sort now'      = @($exe, '--sort')
    'Preview sort'  = @($exe, '--preview')
    'Undo last run' = @($exe, '--undo')
    'Show sort log' = @('notepad.exe', "`"$log`"")
    'Edit settings' = @('notepad.exe', "`"$settings`"")
}
foreach ($name in $shortcuts.Keys) {
    $lnk = $shell.CreateShortcut((Join-Path $StartMenuDir "$name.lnk"))
    $lnk.TargetPath = $shortcuts[$name][0]
    $lnk.Arguments = $shortcuts[$name][1]
    $lnk.WorkingDirectory = $InstallDir
    $lnk.Save()
}
Write-Host "Installed to $InstallDir"
