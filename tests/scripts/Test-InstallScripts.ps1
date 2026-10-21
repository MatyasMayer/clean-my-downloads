# Script-level test of install.ps1 / uninstall.ps1 in a scratch location (no Pester needed).
# Run: powershell -File tests/scripts/Test-InstallScripts.ps1
# Never registers the real scheduled task or touches the real Start menu.
$ErrorActionPreference = 'Stop'
$install = Join-Path $PSScriptRoot '..\..\scripts\install.ps1'
$uninstall = Join-Path $PSScriptRoot '..\..\scripts\uninstall.ps1'
$root = Join-Path ([IO.Path]::GetTempPath()) ("cmd-test-" + [guid]::NewGuid().ToString('N'))
$dir = Join-Path $root 'app'
$menu = Join-Path $root 'menu'
$failures = 0
function Check($ok, $what) {
    if ($ok) { Write-Host "PASS  $what" } else { Write-Host "FAIL  $what"; $script:failures++ }
}
function Read-Lnk($name) { (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $menu "$name.lnk")) }

try {
    & $install -InstallDir $dir -StartMenuDir $menu -SkipTask -SkipSettingsInit | Out-Null
    Check (Test-Path (Join-Path $dir 'CleanMyDownloads.Sorter.exe')) 'install publishes the sorter exe'
    $names = (Get-ChildItem $menu -Filter *.lnk).BaseName | Sort-Object
    $want = 'Edit settings', 'Preview sort', 'Show sort log', 'Sort now', 'Undo last run'
    Check (($names -join '|') -eq (($want | Sort-Object) -join '|')) 'five shortcuts created'
    Check ((Read-Lnk 'Sort now').Arguments -eq '--sort') 'Sort now -> --sort'
    Check ((Read-Lnk 'Preview sort').Arguments -eq '--preview') 'Preview sort -> --preview'
    Check ((Read-Lnk 'Undo last run').Arguments -eq '--undo') 'Undo last run -> --undo'
    $l = Read-Lnk 'Show sort log'
    Check ($l.TargetPath -match 'notepad' -and $l.Arguments -match 'move\.log') 'Show sort log opens move.log in Notepad'
    $s = Read-Lnk 'Edit settings'
    Check ($s.TargetPath -match 'notepad' -and $s.Arguments -match 'settings\.json') 'Edit settings opens settings.json in Notepad'

    Set-Content (Join-Path $dir 'settings.json') 'MINE'
    Set-Content (Join-Path $dir 'move.log') 'LOG'
    & $install -InstallDir $dir -StartMenuDir $menu -SkipTask -SkipSettingsInit | Out-Null
    Check ((Get-Content (Join-Path $dir 'settings.json')) -eq 'MINE') 'reinstall keeps settings'
    Check ((Get-Content (Join-Path $dir 'move.log')) -eq 'LOG') 'reinstall keeps log'
    Check ((Get-ChildItem $menu -Filter *.lnk).Count -eq 5) 'reinstall leaves five shortcuts'

    & $uninstall -InstallDir $dir -StartMenuDir $menu -SkipTask | Out-Null
    Check (-not (Test-Path $menu)) 'uninstall removes shortcuts'
    Check (-not (Test-Path (Join-Path $dir 'CleanMyDownloads.Sorter.exe'))) 'uninstall removes program'
    Check ((Test-Path (Join-Path $dir 'settings.json')) -and (Test-Path (Join-Path $dir 'move.log'))) 'uninstall keeps settings and log'
} finally {
    Remove-Item $root -Recurse -Force -ErrorAction SilentlyContinue
}
if ($failures) { Write-Host "$failures check(s) failed"; exit 1 }
Write-Host 'All checks passed'
