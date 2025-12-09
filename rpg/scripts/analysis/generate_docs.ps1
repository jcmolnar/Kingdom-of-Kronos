# Generate comprehensive documentation from analysis results

$scriptDir = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts"
$analysisFile = Join-Path $scriptDir "ANALYSIS_RESULTS.txt"
$outputFile = Join-Path $scriptDir "MASTER_FUNCTION_VARIABLE_DOCUMENTATION.md"

$content = Get-Content $analysisFile -Raw

# Extract script sections
$scriptPattern = '========================================\r?\n([^\r\n]+)\r?\n========================================\r?\nFunctions: (\d+)\r?\nVariables: (\d+)'
$matches = [regex]::Matches($content, $scriptPattern)

$doc = @"
# Master Function and Variable Documentation
## Kingdom of Kronos RPG Mod

**Generated:** $(Get-Date -Format "MMMM dd, yyyy HH:mm:ss")  
**Total Scripts Analyzed:** $($matches.Count)  
**Total Functions Found:** 925  
**Total Variables Found:** 2,038  

---

## Table of Contents

1. [Scripts Loaded by Server.cs](#scripts-loaded-by-servercs)
2. [Functions by Script](#functions-by-script)
3. [Variables by Script](#variables-by-script)
4. [Function Cross-Reference](#function-cross-reference)
5. [Variable Cross-Reference](#variable-cross-reference)

---

## Scripts Loaded by Server.cs

The following scripts are executed in order by `Server.cs` (lines 161-227):

"@

$scriptNum = 1
foreach ($match in $matches) {
    $scriptName = $match.Groups[1].Value
    $funcCount = $match.Groups[2].Value
    $varCount = $match.Groups[3].Value
    $doc += "$scriptNum. **$scriptName** - Functions: $funcCount, Variables: $varCount`n"
    $scriptNum++
}

$doc += @"

---

## Functions by Script

> **Note:** This section contains all functions extracted from each script. Function signatures include parameter names where available. Descriptions can be added incrementally.

"@

# Extract function lists for each script
$scriptSections = $content -split '========================================'
foreach ($section in $scriptSections) {
    if ($section -match '([^\r\n]+)\r?\n========================================\r?\nFunctions: (\d+)\r?\nVariables: (\d+)\r?\n\r?\nFUNCTIONS:\r?\n(.*?)(?:\r?\nVARIABLES|$)') {
        $scriptName = $matches[1]
        $funcCount = $matches[2]
        $funcList = $matches[4].Trim()
        
        if ($funcList -ne "") {
            $doc += @"

### $scriptName
**Functions:** $funcCount

"@
            $functions = $funcList -split "`r?`n" | Where-Object { $_.Trim() -ne "" }
            $funcNum = 1
            foreach ($func in $functions) {
                if ($func -match '^([a-zA-Z0-9_:]+)\s*\(([^)]*)\)') {
                    $funcName = $matches[1]
                    $params = $matches[2]
                    $doc += "$funcNum. **$funcName($params)**`n"
                    $doc += "   - *Description: [To be added]*`n"
                    $doc += "   - *Used in: [To be added]*`n`n"
                    $funcNum++
                }
            }
        }
    }
}

$doc += @"

---

## Variables by Script

> **Note:** This section contains variables extracted from each script. Variable purposes and scopes can be documented incrementally.

"@

# Extract variable lists
foreach ($section in $scriptSections) {
    if ($section -match '([^\r\n]+)\r?\n========================================\r?\nFunctions: (\d+)\r?\nVariables: (\d+)\r?\n.*?VARIABLES \(first 50\):\r?\n(.*?)(?:\r?\n========================================|$)') {
        $scriptName = $matches[1]
        $varCount = $matches[2]
        $varList = $matches[4].Trim()
        
        if ($varList -ne "" -and $varList -notmatch '^$') {
            $doc += @"

### $scriptName
**Variables:** $varCount

"@
            $variables = $varList -split "`r?`n" | Where-Object { $_.Trim() -ne "" -and $_ -notmatch '^\s*$' } | Select-Object -First 50
            foreach ($var in $variables) {
                $var = $var.Trim()
                if ($var -ne "") {
                    $doc += "- `$$var` - *[Description to be added]*`n"
                }
            }
            $doc += "`n"
        }
    }
}

$doc += @"

---

## Function Cross-Reference

*[Function cross-reference data from analysis - showing where each function is used across scripts]*

## Variable Cross-Reference

*[Variable cross-reference data from analysis - showing where each variable is used across scripts]*

---

## Notes

This documentation is generated from automated analysis of all scripts loaded by `Server.cs`. 

**To add descriptions:**
1. Functions are listed with their signatures
2. Add descriptions under each function explaining what it does
3. Add "Used in:" notes to indicate where functions are called
4. Variables can be documented with their purpose and scope

**Analysis Results File:** `ANALYSIS_RESULTS.txt` contains the complete raw extraction of all functions and variables.

**Next Steps:**
- Review each script section
- Add detailed descriptions for functions
- Document variable purposes
- Add usage examples where helpful
- Cross-reference related functions

---

**End of Master Documentation**
"@

$doc | Out-File $outputFile -Encoding UTF8
Write-Host "Documentation generated: $outputFile"

