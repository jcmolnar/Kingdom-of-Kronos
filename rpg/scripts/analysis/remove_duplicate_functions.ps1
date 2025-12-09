# Remove duplicate functions from ANALYSIS_RESULTS.txt

$inputFile = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
$outputFile = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"

# Read file line by line to preserve formatting
$lines = Get-Content $inputFile

$output = New-Object System.Collections.ArrayList
$currentScript = ""
$inFunctionsSection = $false
$seenFunctions = @{}

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
    
    # Check if this is a script name line
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
    
    # Check if we're leaving the FUNCTIONS section (empty line or VARIABLES section)
    if ($inFunctionsSection -and ($line -match '^VARIABLES' -or ($line.Trim() -eq "" -and $seenFunctions.Count -gt 0))) {
        $inFunctionsSection = $false
        [void]$output.Add($line)
        continue
    }
    
    # If we're in the FUNCTIONS section, check for duplicates
    if ($inFunctionsSection) {
        $trimmedLine = $line.Trim()
        if ($trimmedLine -ne "") {
            # Create a unique key for this function in this script
            $functionKey = "$currentScript|$trimmedLine"
            if (-not $seenFunctions.ContainsKey($functionKey)) {
                $seenFunctions[$functionKey] = $true
                [void]$output.Add($line)
            }
            # Skip duplicate
        } else {
            [void]$output.Add($line)
        }
    } else {
        # Not in functions section, just add the line
        [void]$output.Add($line)
    }
}

# Write output with proper line breaks
$output | Out-File $outputFile -Encoding UTF8

Write-Host "Removed duplicate functions from ANALYSIS_RESULTS.txt"
Write-Host "Original lines: $($lines.Count)"
Write-Host "Output lines: $($output.Count)"
Write-Host "Removed: $($lines.Count - $output.Count) duplicate function entries"
