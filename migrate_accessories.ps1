# Accessory-Only Migration Script (PowerShell)
# Migrates specified Accessories from Legacy Inventory (15) AND Bank (16, 60-63)
$saveDir = Join-Path $PSScriptRoot "Temp"

# Strict list of Accessories as registered in Belt.cs
$accessories = @(
    "MinorPowerRing", "PowerRing", "MajorPowerRing", "ExtremePowerRing", "GodlyPowerRing", "HeavenlyPowerRing", 
    "MinorRegenerationNecklace", "RegenerationNecklace", "MajorRegenerationNecklace", "ExtremeRegenerationNecklace", 
    "GodlyRegenerationNecklace", "HeavenlyRegenerationNecklace", "AntiMagicBelt", "MajorAntiMagicBelt", 
    "ExtremeAntiMagicBelt", "GodlyAntiMagicBelt", "HeavenlyAntiMagicBelt"
)

# Convert to a hash set for fast lookup
$accessoryLookup = @{}
foreach ($item in $accessories) {
    $accessoryLookup[$item.ToLower()] = $item
}

function ConvertFrom-StorageString($str) {
    if (!$str -or $str -eq "0") { return @{} }
    $items = @{}
    $cleanStr = $str -replace '^0\s+', ''
    $cleanStr = $cleanStr.Trim()
    if ($cleanStr -eq "") { return @{} }
    
    $parts = $cleanStr.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
    for ($i = 0; $i -lt $parts.Length; $i += 2) {
        if ($i + 1 -lt $parts.Length) {
            $name = $parts[$i]
            $count = 0
            if ([int]::TryParse($parts[$i + 1], [ref]$count)) {
                if ($items.ContainsKey($name)) { $items[$name] = [int]($items[$name] + $count) }
                else { $items[$name] = [int]$count }
            }
        }
    }
    return $items
}

function ConvertTo-StorageString($itemMap) {
    if (!$itemMap -or $itemMap.Count -eq 0) { return "0" }
    $parts = @()
    $keys = $itemMap.Keys | Sort-Object
    foreach ($name in $keys) {
        if ($itemMap[$name] -gt 0) { $parts += "$name $($itemMap[$name])" }
    }
    if ($parts.Count -gt 0) { return ($parts -join " ") + " " }
    return "0"
}

$files = Get-ChildItem -Path $saveDir -Filter *.cs
foreach ($file in $files) {
    Write-Host "Processing $($file.Name)..."
    $lines = Get-Content $file.FullName
    $slots = @{}
    $playerName = ""
    
    # regex extract all funkvar slots
    $regex = '^\$funk::var\["(.+?)", 0, (\d+)\] = "(.*)";$'
    foreach ($line in $lines) {
        if ($line -match $regex) {
            $playerName = $matches[1]
            $slots[[int]$matches[2]] = $matches[3]
        }
    }
    
    if (!$playerName) { continue }

    # === 1. INVENTORY MIGRATION (Slot 15 -> Slot 49/52) ===
    $invItems = ConvertFrom-StorageString $slots[15]
    $beltAccessories = ConvertFrom-StorageString $slots[49]
    $equippedStateList = if ($slots[52] -and $slots[52] -ne "0") { $slots[52].Trim().Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries) } else { @() }
    
    $migratedInvCount = 0
    $invKeys = $invItems.Keys | ForEach-Object { $_ }
    foreach ($itemKey in $invKeys) {
        $isEquipped = $itemKey.EndsWith("0")
        $baseName = if ($isEquipped) { $itemKey.Substring(0, $itemKey.Length - 1) } else { $itemKey }
        
        if ($accessoryLookup.ContainsKey($baseName.ToLower())) {
            $realName = $accessoryLookup[$baseName.ToLower()]
            $count = $invItems[$itemKey]
            
            if ($beltAccessories.ContainsKey($realName)) { $beltAccessories[$realName] = [int]($beltAccessories[$realName] + $count) }
            else { $beltAccessories[$realName] = [int]$count }
            
            if ($isEquipped) {
                # Add to Slot 52 equipped list
                for ($i = 0; $i -lt $count; $i++) { $equippedStateList += $realName }
            }
            
            $invItems.Remove($itemKey)
            $migratedInvCount++
            Write-Host "  [INV] Migrated $count $itemKey to Belt System"
        }
    }

    # === 2. BANK MIGRATION (Slot 16, 60-63 -> Slot 45) ===
    # Merge existing bank slots
    $fullBankString = ($slots[16], $slots[60], $slots[61], $slots[62], $slots[63] | Where-Object { $_ -and $_ -ne "0" }) -join " "
    $bankItems = ConvertFrom-StorageString $fullBankString
    $beltBank = ConvertFrom-StorageString $slots[45]
    
    $migratedBankCount = 0
    $bankKeys = $bankItems.Keys | ForEach-Object { $_ }
    foreach ($itemKey in $bankKeys) {
        $baseName = if ($itemKey.EndsWith("0")) { $itemKey.Substring(0, $itemKey.Length - 1) } else { $itemKey }
        
        if ($accessoryLookup.ContainsKey($baseName.ToLower())) {
            $realName = $accessoryLookup[$baseName.ToLower()]
            $count = $bankItems[$itemKey]
            
            if ($beltBank.ContainsKey($realName)) { $beltBank[$realName] = [int]($beltBank[$realName] + $count) }
            else { $beltBank[$realName] = [int]$count }
            
            $bankItems.Remove($itemKey)
            $migratedBankCount++
            Write-Host "  [BANK] Migrated $count $itemKey to StoredAccessories"
        }
    }

    if ($migratedInvCount -gt 0 -or $migratedBankCount -gt 0) {
        # Format the final strings
        $newInvStr = ConvertTo-StorageString $invItems
        $newBankStr = ConvertTo-StorageString $bankItems
        $newBeltAccStr = ConvertTo-StorageString $beltAccessories
        $newBeltBankStr = ConvertTo-StorageString $beltBank
        $finalEquippedStr = if ($equippedStateList.Count -gt 0) { ($equippedStateList -join " ") + " " } else { "0" }

        $newLines = @()
        foreach ($line in $lines) {
            if ($line -match $regex) {
                $s = [int]$matches[2]
                if ($s -eq 15) { $newLines += "`$funk::var[`"$playerName`", 0, 15] = `"$newInvStr`";" }
                elseif ($s -eq 16) { $newLines += "`$funk::var[`"$playerName`", 0, 16] = `"$newBankStr`";" }
                elseif ($s -ge 60 -and $s -le 63) { $newLines += "`$funk::var[`"$playerName`", 0, $s] = `"0`";" }
                elseif ($s -eq 49) { $newLines += "`$funk::var[`"$playerName`", 0, 49] = `"$newBeltAccStr`";" }
                elseif ($s -eq 52) { $newLines += "`$funk::var[`"$playerName`", 0, 52] = `"$finalEquippedStr`";" }
                elseif ($s -eq 45) { $newLines += "`$funk::var[`"$playerName`", 0, 45] = `"$newBeltBankStr`";" }
                else { $newLines += $line }
            }
            else { $newLines += $line }
        }
        # Re-save with proper encoding
        [System.IO.File]::WriteAllLines($file.FullName, $newLines)
        Write-Host "  Done: $playerName ($($migratedInvCount + $migratedBankCount) accessories moved)"
    }
}
Write-Host "`nAccessory Migration Complete!"
