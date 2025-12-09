# PowerShell script to update town bot inventories based on zone merchants
# Removes Dagger and adds appropriate weapons, armor, and shields based on zone

$missionFile = "C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\MISSIONS\KingdomKronos.mis"

# Zone to items mapping
$zoneItems = @{
    "Yuliple" = @{
        weapons = @("RustyIronBlade", "ButterKnife", "CrackedStick", "SharpIronBlade", "LongKnife", "IronStick", "IronBroadSword", "IronSpear", "IronMace")
        armor = @("RatSkinShirt")
        shields = @()
    }
    "Sanctuary" = @{
        weapons = @("SteelBroadSword", "SteelSpear", "SteelMace", "SteelLongSword", "SteelPike", "SteelHammer", "GoldenLongSword", "GoldenPike", "SteelWarHammer")
        armor = @("StuddedLeatherSuit", "ToughHideSuit", "IronScaleMail")
        shields = @()
    }
    "Curama" = @{
        weapons = @("GoldenBastardSword", "CrystalPike", "GoldenWarHammer", "CrystalBastardSword", "CrystalTrident", "GoldenDivineMace")
        armor = @("SteelScaleMail", "SteelBrigandineMail", "GoldenBrigandineMail")
        shields = @()
    }
    "Arbal" = @{
        weapons = @("TemperedCrystalBastardSword", "TemperedCrystalTrident", "CrystalDivineMace", "CrystalClaymore", "DiamondTrident", "DiamondDivineMace")
        armor = @("GoldenChainMail", "CrystalChainMail", "CrystalRingMail")
        shields = @()
    }
    "Kronos" = @{
        weapons = @("DiamondClaymore", "DiamondDeathSpear", "DiamondBrainSpiller")
        armor = @("CrystalBandedMail", "CrystalSplintMail", "TungstenSplintMail")
        shields = @()
    }
    "Market" = @{
        weapons = @("DiamondLegendSword", "DiamondLegendSpear", "DiamondLegendMace")
        armor = @("TungstenPlateMail", "DiamondPlateMail", "DiamondFieldPlate")
        shields = @("SteelKnightShield")
    }
    "Lamisor" = @{
        weapons = @("BlackDiamondDreamSword", "BlackDiamondDreamSpear", "BlackDiamondDreamMace")
        armor = @("DiamondFullPlate")
        shields = @("CrystalKnightShield")
    }
    "Asna" = @{
        weapons = @("Hatchet", "Club", "Knife")
        armor = @()
        shields = @()
    }
    "Porellis" = @{
        weapons = @("BlackDiamondAtomSplitter", "BlackDiamondAtomPiercer", "BlackDiamondAtomSmasher")
        armor = @("BlackDiamondFullPlate")
        shields = @("DiamondKnightShield")
    }
    "Gian" = @{
        weapons = @("TerminusEst", "AecoSeorei", "MorningStar")
        armor = @("RedDiamondPlate", "WhiteDiamondPlate")
        shields = @("BlackDiamondKnightShield", "RedDiamondKingShield", "WhiteDiamondKingShield")
    }
}

function Get-Zone {
    param([string]$markerName)
    
    $markerLower = $markerName.ToLower()
    
    if ($markerLower -like "*yuliple*") { return "Yuliple" }
    if ($markerLower -like "*sanctuary*") { return "Sanctuary" }
    if ($markerLower -like "*curama*") { return "Curama" }
    if ($markerLower -like "*arbal*") { return "Arbal" }
    if ($markerLower -like "*kronos*") { return "Kronos" }
    if ($markerLower -like "*market*") { return "Market" }
    if ($markerLower -like "*lamisor*") { return "Lamisor" }
    if ($markerLower -like "*asna*") { return "Asna" }
    if ($markerLower -like "*porellis*") { return "Porellis" }
    if ($markerLower -like "*gian*") { return "Gian" }
    
    return $null
}

function Remove-Dagger {
    param([string]$itemsLine)
    
    # Remove "Dagger 1" or "Dagger" pattern
    $itemsLine = $itemsLine -replace '\s+Dagger\s+\d+\s*', ' '
    $itemsLine = $itemsLine -replace '\s+Dagger\s*', ' '
    return $itemsLine.Trim()
}

function Add-ZoneItems {
    param([string]$itemsLine, [string]$zone)
    
    if (-not $zoneItems.ContainsKey($zone)) {
        return $itemsLine
    }
    
    $items = $zoneItems[$zone]
    $random = Get-Random
    
    # Add weapon
    if ($items.weapons.Count -gt 0) {
        $weapon = $items.weapons | Get-Random
        $itemsLine += " $weapon 1"
    }
    
    # Add armor
    if ($items.armor.Count -gt 0) {
        $armor = $items.armor | Get-Random
        $itemsLine += " $armor 1"
    }
    
    # Add shield (50% chance)
    if ($items.shields.Count -gt 0 -and (Get-Random -Maximum 2) -eq 0) {
        $shield = $items.shields | Get-Random
        $itemsLine += " $shield 1"
    }
    
    return $itemsLine
}

Write-Host "Reading mission file..."
$content = Get-Content $missionFile -Raw

Write-Host "Processing bots..."

# Pattern to match bot groups
$pattern = '(?s)(instant SimGroup "(?:merchant|banker|porter|quest|hunt|tournyquest|guildmaster|sealnpc|assassin|duelmanager|manager|hometele)\d+"\s*\{[^}]*?instant Marker "([^"]+)"[^}]*?instant SimGroup "ITEMS ([^"]+)"[^}]*?\})'

$matches = [regex]::Matches($content, $pattern)
$changesMade = 0

foreach ($match in $matches) {
    $fullMatch = $match.Groups[0].Value
    $markerName = $match.Groups[1].Value
    $itemsLine = $match.Groups[2].Value
    
    $zone = Get-Zone $markerName
    
    if ($zone) {
        $newItems = Remove-Dagger $itemsLine
        $newItems = Add-ZoneItems $newItems $zone
        Write-Host "Updated bot in $zone : $markerName - Items: $newItems"
    } else {
        $newItems = Remove-Dagger $itemsLine
        if ($newItems -ne $itemsLine) {
            Write-Host "Removed Dagger from: $markerName"
        }
    }
    
    $newMatch = $fullMatch -replace '"ITEMS ' + [regex]::Escape($itemsLine) + '"', '"ITEMS ' + $newItems + '"'
    $content = $content -replace [regex]::Escape($fullMatch), $newMatch
    $changesMade++
}

Write-Host "Writing updated file..."
Set-Content -Path $missionFile -Value $content -NoNewline

Write-Host "Done! Changes made: $changesMade"

