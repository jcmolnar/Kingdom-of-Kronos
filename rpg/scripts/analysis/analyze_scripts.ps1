# Script Analysis Tool for Kingdom of Kronos
# Extracts functions and variables from all .cs files

$scriptDir = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts"
$outputFile = "$scriptDir\ANALYSIS_RESULTS.txt"

$scripts = @(
    "globals.cs", "Ai.cs", "rpgfunk.cs", "skills.cs", "house.cs", "rpgarena.cs", 
    "sleep.cs", "game.cs", "Admin.cs", "marker.cs", "trigger.cs", "zone.cs", 
    "spells.cs", "classes.cs", "party.cs", "jail.cs", "nsound.cs", "Help.cs", 
    "baseExpData.cs", "baseDebrisData.cs", "baseProjData.cs", "armordata.cs", 
    "mission.cs", "item.cs", "Accessory.cs", "weapons.cs", "armors.cs", 
    "Crystal.cs", "Spawn.cs", "connectivity.cs", "gameevents.cs", "shopping.cs", 
    "weight.cs", "mana.cs", "hp.cs", "rpgstats.cs", "rpghud.cs", "playerdamage.cs", 
    "playerspawn.cs", "itemevents.cs", "Belt.cs", "economy.cs", "remote.cs", 
    "weaponHandling.cs", "BonusState.cs", "ferry.cs", "Player.cs", "Vehicle.cs", 
    "Turret.cs", "beacon.cs", "staticshape.cs", "Station.cs", "moveable.cs", 
    "sensor.cs", "Mine.cs", "interiorLight.cs", "comchat.cs", "plugs.cs", 
    "version.cs", "hackfix.cs", "newstuff.cs", "advertisements.cs", 
    "TaurikAdmins.cs", "remortseal.cs", "DebugInit.cs"
)

$results = @{}
$allFunctions = @{}
$allVariables = @{}

foreach ($script in $scripts) {
    $filePath = Join-Path $scriptDir $script
    if (Test-Path $filePath) {
        Write-Host "Analyzing: $script"
        $content = Get-Content $filePath -Raw
        
        # Extract functions
        $functionPattern = 'function\s+([a-zA-Z0-9_:]+)\s*\(([^)]*)\)'
        $functions = [regex]::Matches($content, $functionPattern)
        
        $scriptFunctions = @()
        foreach ($match in $functions) {
            $funcName = $match.Groups[1].Value
            $params = $match.Groups[2].Value
            $scriptFunctions += "$funcName($params)"
            
            if (-not $allFunctions.ContainsKey($funcName)) {
                $allFunctions[$funcName] = @()
            }
            $allFunctions[$funcName] += $script
        }
        
        # Extract global variables ($variable = or $variable[)
        $varPattern = '\$([a-zA-Z0-9_:\[\],\s]+)\s*='
        $variables = [regex]::Matches($content, $varPattern)
        
        $scriptVars = @()
        foreach ($match in $variables) {
            $varName = $match.Groups[1].Value.Trim()
            if ($varName -notmatch '^\d+$' -and $varName.Length -lt 200) {
                $scriptVars += $varName
                
                if (-not $allVariables.ContainsKey($varName)) {
                    $allVariables[$varName] = @()
                }
                if ($script -notin $allVariables[$varName]) {
                    $allVariables[$varName] += $script
                }
            }
        }
        
        $results[$script] = @{
            Functions = $scriptFunctions
            Variables = $scriptVars
            FunctionCount = $scriptFunctions.Count
            VariableCount = $scriptVars.Count
        }
    } else {
        Write-Host "NOT FOUND: $script"
    }
}

# Generate report
$report = @"
SCRIPT ANALYSIS REPORT
Generated: $(Get-Date)
Total Scripts Analyzed: $($results.Count)

"@

foreach ($script in $scripts) {
    if ($results.ContainsKey($script)) {
        $data = $results[$script]
        $report += @"

========================================
$script
========================================
Functions: $($data.FunctionCount)
Variables: $($data.VariableCount)

FUNCTIONS:
$($data.Functions -join "`n")

VARIABLES (first 50):
$(($data.Variables | Select-Object -First 50) -join "`n")

"@
    }
}

$report += @"

========================================
CROSS-REFERENCE: FUNCTIONS
========================================
"@

foreach ($func in ($allFunctions.Keys | Sort-Object)) {
    $report += "$func`n"
    $report += "  Used in: $($allFunctions[$func] -join ', ')`n`n"
}

$report += @"

========================================
CROSS-REFERENCE: VARIABLES (Top 100)
========================================
"@

$sortedVars = $allVariables.GetEnumerator() | Sort-Object Name | Select-Object -First 100
foreach ($var in $sortedVars) {
    $report += "`$$($var.Key)`n"
    $report += "  Used in: $($var.Value -join ', ')`n`n"
}

$report | Out-File $outputFile -Encoding UTF8
Write-Host "`nAnalysis complete! Results saved to: $outputFile"
Write-Host "Total Functions Found: $($allFunctions.Count)"
Write-Host "Total Variables Found: $($allVariables.Count)"

