# Analyze duplicates and improve variable/function extraction
# This script will:
# 1. Find duplicate variables (same variable name appearing multiple times)
# 2. Find duplicate functions (same function signature appearing in multiple files)
# 3. Improve variable extraction to be more accurate

$scriptDir = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts"
$outputFile = Join-Path $scriptDir "DUPLICATE_ANALYSIS.txt"

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
    "remortseal.cs", "DebugInit.cs"
)

$allFunctions = @{}  # function name -> array of {script, signature}
$allVariables = @{}  # variable name -> array of scripts
$functionSignatures = @{}  # full signature -> array of scripts
$duplicateFunctions = @{}
$duplicateVariables = @{}

Write-Host "Analyzing scripts for duplicates and improving extraction..."

foreach ($script in $scripts) {
    $filePath = Join-Path $scriptDir $script
    if (-not (Test-Path $filePath)) {
        Write-Host "NOT FOUND: $script"
        continue
    }
    
    Write-Host "Processing: $script"
    $content = Get-Content $filePath -Raw
    
    # Extract functions with better pattern matching
    # Match: function NameSpace::FunctionName(params) or function FunctionName(params)
    $functionPattern = 'function\s+([a-zA-Z0-9_:]+)\s*\(([^)]*)\)'
    $functions = [regex]::Matches($content, $functionPattern)
    
    foreach ($match in $functions) {
        $funcName = $match.Groups[1].Value
        $params = $match.Groups[2].Value
        $fullSignature = "$funcName($params)"
        
        # Track by function name
        if (-not $allFunctions.ContainsKey($funcName)) {
            $allFunctions[$funcName] = @()
        }
        $allFunctions[$funcName] += @{
            Script = $script
            Signature = $fullSignature
        }
        
        # Track by full signature (to find exact duplicates)
        if (-not $functionSignatures.ContainsKey($fullSignature)) {
            $functionSignatures[$fullSignature] = @()
        }
        $functionSignatures[$fullSignature] += $script
        
        # Check for duplicate signatures
        if ($functionSignatures[$fullSignature].Count -gt 1) {
            if (-not $duplicateFunctions.ContainsKey($fullSignature)) {
                $duplicateFunctions[$fullSignature] = $functionSignatures[$fullSignature]
            }
        }
    }
    
    # Improved variable extraction
    # Match: $VariableName = or $VariableName[ or $VariableName::
    # But exclude function parameters and local variables in function bodies
    $lines = $content -split "`r?`n"
    $inFunction = $false
    $functionDepth = 0
    
    foreach ($line in $lines) {
        $trimmedLine = $line.Trim()
        
        # Track function boundaries (simple detection)
        if ($trimmedLine -match '^\s*function\s+') {
            $inFunction = $true
            $functionDepth++
        }
        if ($trimmedLine -match '^\s*}\s*$' -and $inFunction) {
            $functionDepth--
            if ($functionDepth -eq 0) {
                $inFunction = $false
            }
        }
        
        # Extract global variables (not in function bodies, not in parameter lists)
        # Match: $VarName = or $VarName[ or $VarName::
        if (-not $inFunction -or $trimmedLine -match '^\s*\$') {
            # Match $variable = (assignment)
            if ($trimmedLine -match '\$([a-zA-Z0-9_:]+)\s*=') {
                $varName = $matches[1]
                # Skip if it's in a function parameter list
                if (-not ($trimmedLine -match 'function.*\$' + $varName)) {
                    if (-not $allVariables.ContainsKey($varName)) {
                        $allVariables[$varName] = @()
                    }
                    if ($script -notin $allVariables[$varName]) {
                        $allVariables[$varName] += $script
                    }
                }
            }
            
            # Match $variable[index] = (array assignment)
            if ($trimmedLine -match '\$([a-zA-Z0-9_]+)\[') {
                $varName = $matches[1]
                if (-not $allVariables.ContainsKey($varName)) {
                    $allVariables[$varName] = @()
                }
                if ($script -notin $allVariables[$varName]) {
                    $allVariables[$varName] += $script
                }
            }
            
            # Match $Namespace::Variable
            if ($trimmedLine -match '\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)') {
                $varName = $matches[1]
                if (-not $allVariables.ContainsKey($varName)) {
                    $allVariables[$varName] = @()
                }
                if ($script -notin $allVariables[$varName]) {
                    $allVariables[$varName] += $script
                }
            }
        }
    }
}

# Find variables that appear in multiple scripts
foreach ($var in $allVariables.Keys) {
    if ($allVariables[$var].Count -gt 1) {
        $duplicateVariables[$var] = $allVariables[$var]
    }
}

# Generate report
$report = @"
DUPLICATE ANALYSIS REPORT
Generated: $(Get-Date)
========================================

SUMMARY
========================================
Total Unique Functions: $($allFunctions.Count)
Total Unique Function Signatures: $($functionSignatures.Count)
Total Unique Variables: $($allVariables.Count)
Duplicate Function Signatures: $($duplicateFunctions.Count)
Variables Used in Multiple Scripts: $($duplicateVariables.Count)

========================================
DUPLICATE FUNCTION SIGNATURES
(Exact same function signature in multiple files)
========================================

"@

if ($duplicateFunctions.Count -eq 0) {
    $report += "No duplicate function signatures found.`n`n"
} else {
    foreach ($sig in ($duplicateFunctions.Keys | Sort-Object)) {
        $scripts = $duplicateFunctions[$sig]
        $report += "Function: $sig`n"
        $report += "  Found in: $($scripts -join ', ')`n`n"
    }
}

$report += @"

========================================
FUNCTIONS WITH SAME NAME BUT DIFFERENT SIGNATURES
(Overloaded functions or functions with same name in different namespaces)
========================================

"@

$overloaded = $allFunctions.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 } | Sort-Object Name
foreach ($func in $overloaded) {
    $funcName = $func.Key
    $instances = $func.Value
    $uniqueSigs = @()
    foreach ($inst in $instances) {
        if ($inst.Signature -notin $uniqueSigs) {
            $uniqueSigs += $inst.Signature
        }
    }
    
    if ($uniqueSigs.Count -gt 1) {
        $report += "Function Name: $funcName`n"
        foreach ($inst in $instances) {
            $report += "  - $($inst.Signature) in $($inst.Script)`n"
        }
        $report += "`n"
    }
}

$report += @"

========================================
VARIABLES USED IN MULTIPLE SCRIPTS
(These are likely global variables shared across scripts)
========================================

"@

$sortedVars = $duplicateVariables.GetEnumerator() | Sort-Object Name
foreach ($var in $sortedVars) {
    $varName = $var.Key
    $scripts = $var.Value
    $report += "`$$varName`n"
    $report += "  Used in: $($scripts -join ', ')`n`n"
}

$report += @"

========================================
UNIQUE VARIABLES BY SCRIPT
(First 20 variables per script, deduplicated)
========================================

"@

foreach ($script in $scripts) {
    $filePath = Join-Path $scriptDir $script
    if (-not (Test-Path $filePath)) {
        continue
    }
    
    $scriptVars = $allVariables.GetEnumerator() | Where-Object { $_.Value -contains $script } | Select-Object -First 20
    if ($scriptVars.Count -gt 0) {
        $report += "`n$script`n"
        foreach ($var in $scriptVars) {
            $report += "  - `$$($var.Key)`n"
        }
    }
}

$report | Out-File $outputFile -Encoding UTF8
Write-Host "`nAnalysis complete! Results saved to: $outputFile"
Write-Host "Duplicate function signatures: $($duplicateFunctions.Count)"
Write-Host "Variables used in multiple scripts: $($duplicateVariables.Count)"

