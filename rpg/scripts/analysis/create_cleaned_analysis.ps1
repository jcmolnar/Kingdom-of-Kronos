# Create cleaned analysis with deduplicated variables and improved extraction

$scriptDir = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts"
$outputFile = Join-Path $scriptDir "CLEANED_ANALYSIS.txt"
$duplicateFunctionsFile = Join-Path $scriptDir "DUPLICATE_FUNCTIONS_LIST.txt"

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

$allFunctions = @{}  # function signature -> array of scripts
$functionsByScript = @{}  # script -> array of function signatures
$allVariables = @{}  # variable name -> array of scripts (deduplicated per script)
$variablesByScript = @{}  # script -> array of variable names
$duplicateFunctions = @{}  # signature -> scripts array

Write-Host "Creating cleaned analysis with improved extraction..."

foreach ($script in $scripts) {
    $filePath = Join-Path $scriptDir $script
    if (-not (Test-Path $filePath)) {
        Write-Host "NOT FOUND: $script"
        continue
    }
    
    Write-Host "Processing: $script"
    $content = Get-Content $filePath -Raw
    
    # Initialize script-specific collections
    $scriptFuncList = New-Object System.Collections.ArrayList
    $scriptVarList = New-Object System.Collections.ArrayList
    
    # Extract functions - improved pattern to handle multi-line and namespaces
    $functionPattern = '(?m)^\s*function\s+([a-zA-Z0-9_:]+)\s*\(([^)]*)\)'
    $functions = [regex]::Matches($content, $functionPattern)
    
    $scriptFunctions = @{}
    foreach ($match in $functions) {
        $funcName = $match.Groups[1].Value
        $params = $match.Groups[2].Value
        $fullSignature = "$funcName($params)"
        
        # Only count once per script
        if (-not $scriptFunctions.ContainsKey($fullSignature)) {
            $scriptFunctions[$fullSignature] = $true
            [void]$scriptFuncList.Add($fullSignature)
            
            if (-not $allFunctions.ContainsKey($fullSignature)) {
                $allFunctions[$fullSignature] = @()
            }
            if ($script -notin $allFunctions[$fullSignature]) {
                $allFunctions[$fullSignature] += $script
            }
            
            # Track duplicates
            if ($allFunctions[$fullSignature].Count -gt 1) {
                $duplicateFunctions[$fullSignature] = $allFunctions[$fullSignature]
            }
        }
    }
    
    # Improved variable extraction - catch more patterns
    $scriptVars = @{}
    $lines = $content -split "`r?`n"
    
    foreach ($line in $lines) {
        $trimmedLine = $line.Trim()
        
        # Skip comments and function definitions
        if ($trimmedLine -match '^\s*//' -or $trimmedLine -match '^\s*function\s+') {
            continue
        }
        
        # Match $variable = (assignment) - improved pattern
        if ($trimmedLine -match '\$([a-zA-Z0-9_:]+)\s*=') {
            $varName = $matches[1]
            # Skip function parameters
            if (-not ($trimmedLine -match 'function.*\$' + $varName)) {
                if (-not $scriptVars.ContainsKey($varName)) {
                    $scriptVars[$varName] = $true
                    [void]$scriptVarList.Add($varName)
                    
                    if (-not $allVariables.ContainsKey($varName)) {
                        $allVariables[$varName] = @()
                    }
                    if ($script -notin $allVariables[$varName]) {
                        $allVariables[$varName] += $script
                    }
                }
            }
        }
        
        # Match $variable[index] = (array assignment) - improved pattern
        if ($trimmedLine -match '\$([a-zA-Z0-9_:]+)\[') {
            $varName = $matches[1]
            if (-not $scriptVars.ContainsKey($varName)) {
                $scriptVars[$varName] = $true
                [void]$scriptVarList.Add($varName)
                
                if (-not $allVariables.ContainsKey($varName)) {
                    $allVariables[$varName] = @()
                }
                if ($script -notin $allVariables[$varName]) {
                    $allVariables[$varName] += $script
                }
            }
        }
        
        # Match $Namespace::Variable - improved pattern
        if ($trimmedLine -match '\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)') {
            $varName = $matches[1]
            if (-not $scriptVars.ContainsKey($varName)) {
                $scriptVars[$varName] = $true
                [void]$scriptVarList.Add($varName)
                
                if (-not $allVariables.ContainsKey($varName)) {
                    $allVariables[$varName] = @()
                }
                if ($script -notin $allVariables[$varName]) {
                    $allVariables[$varName] += $script
                }
            }
        }
        
        # Match $variable::subvariable (nested namespaces)
        if ($trimmedLine -match '\$([a-zA-Z0-9_]+::[a-zA-Z0-9_:]+)') {
            $varName = $matches[1]
            if (-not $scriptVars.ContainsKey($varName)) {
                $scriptVars[$varName] = $true
                [void]$scriptVarList.Add($varName)
                
                if (-not $allVariables.ContainsKey($varName)) {
                    $allVariables[$varName] = @()
                }
                if ($script -notin $allVariables[$varName]) {
                    $allVariables[$varName] += $script
                }
            }
        }
    }
    
    # Store the collected functions and variables for this script
    if ($scriptFuncList.Count -gt 0) {
        $functionsByScript[$script] = $scriptFuncList.ToArray()
    } else {
        $functionsByScript[$script] = @()
    }
    if ($scriptVarList.Count -gt 0) {
        $variablesByScript[$script] = $scriptVarList.ToArray()
    } else {
        $variablesByScript[$script] = @()
    }
}

# Generate cleaned analysis report using StringBuilder approach
$report = New-Object System.Text.StringBuilder

[void]$report.AppendLine("CLEANED ANALYSIS REPORT")
[void]$report.AppendLine("Generated: $(Get-Date)")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("")
[void]$report.AppendLine("SUMMARY")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("Total Unique Functions: $($allFunctions.Count)")
[void]$report.AppendLine("Total Unique Variables: $($allVariables.Count)")
[void]$report.AppendLine("Duplicate Function Signatures: $($duplicateFunctions.Count)")
[void]$report.AppendLine("")
[void]$report.AppendLine("NOTE: Variables are now deduplicated - each variable is only counted once per script.")
[void]$report.AppendLine("")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("DUPLICATE FUNCTION SIGNATURES")
[void]$report.AppendLine("(These need to be investigated - same function defined in multiple files)")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("")

if ($duplicateFunctions.Count -eq 0) {
    [void]$report.AppendLine("No duplicate function signatures found.")
    [void]$report.AppendLine("")
} else {
    foreach ($sig in ($duplicateFunctions.Keys | Sort-Object)) {
        $scripts = $duplicateFunctions[$sig]
        [void]$report.AppendLine("Function: $sig")
        [void]$report.AppendLine("  Found in: $($scripts -join ', ')")
        [void]$report.AppendLine("  WARNING: This function is defined in multiple files!")
        [void]$report.AppendLine("")
    }
}

[void]$report.AppendLine("")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("FUNCTIONS BY SCRIPT")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("")

foreach ($script in $scripts) {
    $filePath = Join-Path $scriptDir $script
    if (-not (Test-Path $filePath)) {
        continue
    }
    
    # Get functions for this script from $allFunctions
    $scriptFuncs = @()
    foreach ($funcSig in $allFunctions.Keys) {
        if ($script -in $allFunctions[$funcSig]) {
            $scriptFuncs += $funcSig
        }
    }
    if ($scriptFuncs.Count -gt 0) {
        [void]$report.AppendLine("")
        [void]$report.AppendLine("$script ($($scriptFuncs.Count) functions)")
        foreach ($func in ($scriptFuncs | Sort-Object)) {
            [void]$report.AppendLine("  - $func")
        }
    }
}

[void]$report.AppendLine("")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("UNIQUE VARIABLES BY SCRIPT")
[void]$report.AppendLine("(Deduplicated - each variable listed once per script)")
[void]$report.AppendLine("========================================")
[void]$report.AppendLine("")

foreach ($script in $scripts) {
    $filePath = Join-Path $scriptDir $script
    if (-not (Test-Path $filePath)) {
        continue
    }
    
    # Get variables for this script from $allVariables
    $scriptVars = @()
    foreach ($varName in $allVariables.Keys) {
        if ($script -in $allVariables[$varName]) {
            $scriptVars += $varName
        }
    }
    if ($scriptVars.Count -gt 0) {
        [void]$report.AppendLine("")
        [void]$report.AppendLine("$script ($($scriptVars.Count) unique variables)")
        foreach ($var in ($scriptVars | Sort-Object)) {
            [void]$report.AppendLine("  - `$$var")
        }
    }
}

# Write report to file
$report.ToString() | Out-File $outputFile -Encoding UTF8

# Create separate duplicate functions list
$dupReport = New-Object System.Text.StringBuilder

[void]$dupReport.AppendLine("DUPLICATE FUNCTIONS - REQUIRES INVESTIGATION")
[void]$dupReport.AppendLine("Generated: $(Get-Date)")
[void]$dupReport.AppendLine("========================================")
[void]$dupReport.AppendLine("")
[void]$dupReport.AppendLine("Total Duplicate Function Signatures: $($duplicateFunctions.Count)")
[void]$dupReport.AppendLine("")
[void]$dupReport.AppendLine("These functions have the EXACT same signature in multiple files.")
[void]$dupReport.AppendLine("This may indicate:")
[void]$dupReport.AppendLine("1. Functions that were accidentally duplicated")
[void]$dupReport.AppendLine("2. Functions that should be in a shared/common file")
[void]$dupReport.AppendLine("3. Functions that need to be renamed to avoid conflicts")
[void]$dupReport.AppendLine("")

foreach ($sig in ($duplicateFunctions.Keys | Sort-Object)) {
    $scripts = $duplicateFunctions[$sig]
    [void]$dupReport.AppendLine("")
    [void]$dupReport.AppendLine("========================================")
    [void]$dupReport.AppendLine("Function: $sig")
    [void]$dupReport.AppendLine("Found in: $($scripts -join ', ')")
    [void]$dupReport.AppendLine("")
    [void]$dupReport.AppendLine("ACTION REQUIRED: Review these files and:")
    [void]$dupReport.AppendLine("  - Remove duplicate if it's accidental")
    [void]$dupReport.AppendLine("  - Move to common file if it's shared code")
    [void]$dupReport.AppendLine("  - Rename if they serve different purposes")
}

$dupReport.ToString() | Out-File $duplicateFunctionsFile -Encoding UTF8 -NoNewline

Write-Host "`nCleaned analysis complete!"
Write-Host "Results saved to: $outputFile"
Write-Host "Duplicate functions list saved to: $duplicateFunctionsFile"
Write-Host "Total unique functions: $($allFunctions.Count)"
Write-Host "Total unique variables: $($allVariables.Count)"
Write-Host "Duplicate function signatures: $($duplicateFunctions.Count)"
