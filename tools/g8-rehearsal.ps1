# g8-rehearsal.ps1 - swap the DEV server's Temp\ between your own dev saves and
# a COPY of the live server's saves, for the G8 migration rehearsal.
#
#   .\tools\g8-rehearsal.ps1 -Mode live      # put the live-save COPY in place
#   .\tools\g8-rehearsal.ps1 -Mode dev       # put your own dev saves back
#   .\tools\g8-rehearsal.ps1 -Mode status    # show which set is active
#
# STOP THE DEV SERVER FIRST. The script refuses to run if Temp\ was written in
# the last 2 minutes, because swapping saves under a running server corrupts them.
#
# Nothing here ever writes to the live server. The staged copy was taken
# read-only; the live tree is never a destination. See LIVE_MIGRATION_PLAN.md G8.

param([ValidateSet('live','dev','status')][string]$Mode = 'status')

$ErrorActionPreference = 'Stop'
$root    = Split-Path -Parent $PSScriptRoot
$temp    = Join-Path $root 'Temp'
$devBak  = Join-Path $root 'Temp.devown-bak-20260808'
$staged  = Join-Path $root '_rehearsal_live-saves-2026-08-08'
$marker  = Join-Path $temp '_REHEARSAL_LIVE_SAVES_ACTIVE.txt'

function Get-ActiveSet {
    if (Test-Path $marker) { 'live' } else { 'dev' }
}

function Assert-ServerDown {
    if (-not (Test-Path $temp)) { return }
    $recent = Get-ChildItem $temp -File -ErrorAction SilentlyContinue |
              Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-2) }
    if ($recent) {
        throw "Temp\ was written within the last 2 minutes - the dev server looks like it is RUNNING. Stop it, then re-run. (newest: $($recent[0].Name))"
    }
}

switch ($Mode) {
  'status' {
    Write-Host "active save set : $(Get-ActiveSet)"
    if (Test-Path $temp)   { Write-Host "Temp\           : $((Get-ChildItem $temp -Filter *.cs -File).Count) .cs" }
    if (Test-Path $devBak) { Write-Host "dev backup      : $((Get-ChildItem $devBak -Filter *.cs -File).Count) .cs  ($devBak)" }
    if (Test-Path $staged) { Write-Host "staged live copy: $((Get-ChildItem $staged -Filter *.cs -File).Count) .cs  ($staged)" }
    Write-Host ""
    Write-Host "G8 pass criteria: nothing lost, nothing duplicated, worn armor re-equips,"
    Write-Host "log out + back in is identical (idempotency), and ZERO [VOID AUDIT] lines."
  }

  'live' {
    Assert-ServerDown
    if (-not (Test-Path $staged)) { throw "staged live-save copy not found: $staged" }
    if ((Get-ActiveSet) -eq 'live') { Write-Host "already on the live-save copy - nothing to do."; break }

    # refresh the dev backup so the CURRENT dev saves are what we restore later
    if (Test-Path $devBak) { Remove-Item $devBak -Recurse -Force }
    Copy-Item $temp $devBak -Recurse
    Write-Host "dev saves backed up -> $devBak"

    Get-ChildItem $temp -Filter *.cs -File | Remove-Item -Force
    Copy-Item (Join-Path $staged '*.cs') $temp
    $n = (Get-ChildItem $temp -Filter *.cs -File).Count
    Set-Content $marker "Live-save COPY active for the G8 rehearsal ($n .cs). Restore with: .\tools\g8-rehearsal.ps1 -Mode dev"
    Write-Host "live-save copy in place: $n .cs"
    Write-Host "Boot dev and log in as several REAL characters (heavily geared, remort/"
    Write-Host "ascended, full bank, legacy BankStorage, low level, mid-session)."
  }

  'dev' {
    Assert-ServerDown
    if (-not (Test-Path $devBak)) { throw "dev save backup not found: $devBak" }
    Get-ChildItem $temp -Filter *.cs -File | Remove-Item -Force
    Copy-Item (Join-Path $devBak '*.cs') $temp
    Remove-Item $marker -Force -ErrorAction SilentlyContinue
    Write-Host "dev saves restored: $((Get-ChildItem $temp -Filter *.cs -File).Count) .cs"
  }
}
