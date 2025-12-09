# Remove duplicate functions from ANALYSIS_RESULTS.txt using restored file

$inputFile = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS_restored.txt"
$outputFile = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"

if (-not (Test-Path $inputFile)) {
    Write-Host "ERROR: Restored file not found!"
    exit 1
}

# Read file line by line
$lines = Get-Content $inputFile

$output = New-Object System.Collections.ArrayList
$currentScript = ""
$inFunctionsSection = $false
$seenFunctions = @{}
$duplicateCount = 0

foreach ($line in $lines) {
    # Check if we're entering a new script section
    if ($line -match '^========================================$') {
        # Reset for new script section
        $currentScript = ""
        $inFunctionsSection = $false
        $seenFunctions = @{}
        [void]$output.Add($line)
        continue
    }
    
    # Check if this is a script name line (after the separator)
    if ($line -match '^([a-zA-Z0-9_]+\.cs)$') {
        $currentScript = $matches[1]
        $inFunctionsSection = $false
        $seenFunctions = @{}
        [void]$output.Add($line)
        continue
    }
    
    # Check if we're entering the FUNCTIONS section
    if ($line -match '^FUNCTIONS:$') {
        $inFunctionsSection = $true
        $seenFunctions = @{}
        [void]$output.Add($line)
        continue
    }
    
    # Check if we're leaving the FUNCTIONS section
    if ($inFunctionsSection -and ($line -match '^VARIABLES' -or $line -match '^CROSS-REFERENCE')) {
        $inFunctionsSection = $false
        [void]$output.Add($line)
        continue
    }
    
    # If we're in the FUNCTIONS section, check for duplicates
    if ($inFunctionsSection) {
        $trimmedLine = $line.Trim()
        if ($trimmedLine -ne "" -and $trimmedLine -match '^[a-zA-Z0-9_:]+\([^)]*\)$') {
            # This looks like a function signature
            $functionKey = "$currentScript|$trimmedLine"
            if (-not $seenFunctions.ContainsKey($functionKey)) {
                $seenFunctions[$functionKey] = $true
                [void]$output.Add($line)
            } else {
                $duplicateCount++
                # Skip duplicate
            }
        } else {
            # Not a function line or empty, keep it
            [void]$output.Add($line)
        }
    } else {
        # Not in functions section, just add the line
        [void]$output.Add($line)
    }
}

# Write output
$output | Out-File $outputFile -Encoding UTF8

Write-Host "Removed duplicate functions from ANALYSIS_RESULTS.txt"
Write-Host "Original lines: $($lines.Count)"
Write-Host "Output lines: $($output.Count)"
Write-Host "Removed: $duplicateCount duplicate function entries"

