# check-boot.ps1 - assert a server boot log against the deploy gates.
# Implements LIVE_MIGRATION_PLAN.md G1 (dev) and Phase 4 (live).
#
#   .\tools\check-boot.ps1                          # this tree's console.log
#   .\tools\check-boot.ps1 -Log "C:\...\console.log" # live's console.log
#   .\tools\check-boot.ps1 -Tail 4000                # widen the window
#
# console.log APPENDS across boots, so by default only the tail is scanned -
# otherwise you match errors from previous runs. Exit code 1 = an abort string
# fired or a required line is missing.

param(
  [string]$Log  = (Join-Path (Split-Path -Parent $PSScriptRoot) 'console.log'),
  [int]   $Tail = 2000
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Log)) { Write-Host "log not found: $Log" -ForegroundColor Red; exit 1 }

$lines = Get-Content $Log -Tail $Tail
Write-Host "scanning last $($lines.Count) lines of $Log`n"

# --- ABORT STRINGS: any hit = stop the deploy -------------------------------
$aborts = [ordered]@{
  'Unknown command getRealMillis' = 'WRONG kronos_datetime.dll (live has the old build) - R3'
  'Unknown command vslot'         = 'kronos_virtualitems.dll missing - R4'
  'disabling Virtual Slots'       = 'VSlot DLL probe failed. NOT a warning: carry caps stay enforced with no window - R4'
  'exec: file not found'          = 'a script or config file is missing from the package'
  'Unable to find function'       = 'broken call across the split modules'
  '[VOID AUDIT]'                  = 'belt item minted an ENGINE count - corruption tripwire - R1/R2'
  'AI SAFEGUARDS DISABLED'        = 'ai_safeguards_off.cs shipped AND got exec"d - R11'
}
# --- REQUIRED LINES: all must appear ----------------------------------------
$required = [ordered]@{
  '[DEPLOY] profile ='            = 'deploy profile took effect (should read "live" on the live server)'
  '[SLOT AUDIT] ItemData types'   = 'item registration audit - N must MATCH dev exactly'
  'Number::Beautify sanity'       = 'MathPlugin loaded (else coin/exp render blank)'
}
# --- KNOWN-BASELINE syntax errors -------------------------------------------
# These 5 files use a bare `default` as an AI::helper argument; `default` is a
# reserved word here, so the parser rejects that STATEMENT at exec time. This is
# PRE-EXISTING and identical on the live server (verified 2026-08-08), so it is
# NOT a deploy blocker - but a syntax error in any OTHER file would be. Tracked
# separately; the fix is quoting it ("default").
$syntaxBaseline = @('DailyQuest.cs','WeeklyBoss.cs','comchat.cs','remortseal.cs','rpgarena.cs')

$fail = $false

Write-Host "MUST BE ABSENT" -ForegroundColor Cyan
foreach ($k in $aborts.Keys) {
  $hits = $lines | Select-String -SimpleMatch $k
  if ($hits) {
    $fail = $true
    Write-Host ("  ABORT  {0}  ({1} hit(s))" -f $k, $hits.Count) -ForegroundColor Red
    Write-Host ("         -> {0}" -f $aborts[$k]) -ForegroundColor Red
    $hits | Select-Object -First 2 | ForEach-Object { Write-Host ("         | " + $_.Line.Trim()) -ForegroundColor DarkGray }
  } else {
    Write-Host ("  ok     {0}" -f $k) -ForegroundColor DarkGray
  }
}

# syntax errors: only NEW ones (outside the known baseline) are fatal
Write-Host "`nSYNTAX ERRORS (baseline-compared)" -ForegroundColor Cyan
$syn = $lines | Select-String -SimpleMatch 'Syntax error' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
if (-not $syn) { Write-Host "  none" -ForegroundColor Green }
foreach ($s in $syn) {
  $known = $false
  foreach ($b in $syntaxBaseline) { if ($s -like "*$b*") { $known = $true } }
  if ($known) { Write-Host ("  known  {0}" -f $s) -ForegroundColor DarkGray }
  else        { $fail = $true; Write-Host ("  NEW    {0}   <- not in baseline, investigate" -f $s) -ForegroundColor Red }
}

Write-Host "`nMUST BE PRESENT" -ForegroundColor Cyan
foreach ($k in $required.Keys) {
  $hits = $lines | Select-String -SimpleMatch $k
  if ($hits) {
    Write-Host ("  ok     {0}" -f $hits[-1].Line.Trim()) -ForegroundColor Green
  } else {
    $fail = $true
    Write-Host ("  MISSING {0}" -f $k) -ForegroundColor Red
    Write-Host ("         -> {0}" -f $required[$k]) -ForegroundColor Red
  }
}

# value assertions - the LINE existing is not enough
Write-Host "`nVALUE ASSERTIONS" -ForegroundColor Cyan
$b = $lines | Select-String -SimpleMatch 'Number::Beautify sanity' | Select-Object -Last 1
if ($b -and $b.Line -match '123,456') {
  Write-Host "  ok     MathPlugin formatting works (123,456)" -ForegroundColor Green
} elseif ($b) {
  $fail = $true
  Write-Host ("  FAIL   {0}" -f $b.Line.Trim()) -ForegroundColor Red
  Write-Host "         -> Number::Beautify did NOT return 123,456 ('False' = command missing)." -ForegroundColor Red
  Write-Host "            MathPlugin is not loaded: every coin/exp number renders BLANK." -ForegroundColor Red
}
$v = $lines | Select-String -SimpleMatch '[VSLOT]' | Select-Object -Last 1
if ($v -and ($v.Line -match 'windows ready' -or $v.Line -match 'dbm=')) {
  Write-Host ("  ok     {0}" -f $v.Line.Trim()) -ForegroundColor Green
} elseif ($v) {
  $fail = $true
  Write-Host ("  FAIL   {0}" -f $v.Line.Trim()) -ForegroundColor Red
  Write-Host "         -> VSlots did NOT come up. Carry caps stay enforced with no window - R4." -ForegroundColor Red
} else {
  $fail = $true
  Write-Host "  MISSING [VSLOT] line entirely" -ForegroundColor Red
}

# record the SLOT AUDIT number so dev and live can be compared
$slot = $lines | Select-String -SimpleMatch '[SLOT AUDIT] ItemData types' | Select-Object -Last 1
if ($slot) { Write-Host "`nrecord this and compare dev vs live:`n  $($slot.Line.Trim())" -ForegroundColor Yellow }

Write-Host ""
if ($fail) { Write-Host "RESULT: FAIL - do not proceed" -ForegroundColor Red; exit 1 }
Write-Host "RESULT: PASS" -ForegroundColor Green
