//============================================================================
// DUAL WIELDING SYSTEM - Kingdom of Kronos
//============================================================================
// This module implements experimental dual-wielding functionality.
// 
// HOW IT WORKS:
//   - Primary weapon uses engine slot 0 (standard behavior)
//   - Off-hand weapon uses engine slot 3 (unused slot)
//   - When primary weapon fires, off-hand weapon also fires (if equipped)
//   - Off-hand weapon deals reduced damage (configurable via $DualWield::OffHandDamageMultiplier)
//
// SKILL REQUIREMENT:
//   - Players must have Slashing skill >= $DualWield::RequiredSkillLevel to dual wield
//   - Default requirement is 300 Slashing skill
//   - This simulates the training needed to fight with two weapons
//
// LIMITATIONS:
//   - Player animations only support one weapon (visual may look odd)
//   - Both weapons share the same fire animation timing
//   - AI bots do not use dual wielding (slot 0 only)
//   - Polearms cannot be dual wielded (too large)
//   - Shields cannot be equipped while dual wielding
//
// USAGE:
//   DualWield::EquipOffHand(%clientId, %weaponItem)   - Equip off-hand weapon
//   DualWield::UnequipOffHand(%clientId)              - Unequip off-hand weapon
//   DualWield::GetOffHandWeapon(%clientId)            - Get current off-hand weapon
//   DualWield::IsEnabled(%clientId)                   - Check if dual wielding is active
//   DualWield::CanDualWield(%clientId)                - Check if player meets skill requirement
//
// COMMANDS (for testing):
//   #dualwield <weaponName>  - Equip specified weapon in off-hand
//   #dualwield off           - Unequip off-hand weapon
//   #dualwield status        - Show current off-hand and requirements
//
// INTEGRATION REQUIRED:
//   1. Add to comchat.cs command handling: 
//      if(GetWord(%msg, 0) == "#dualwield") { DualWield::Command(%clientId, GetWords(%msg, 1, 99)); return; }
//   
//   2. Add to weapon fire logic (where damage is dealt):
//      DualWield::OnPrimaryFire(%clientId, %weapon);
//   
//   3. Optionally modify damage calculation to apply multiplier:
//      %damageMultiplier = DualWield::GetDamageMultiplier(%clientId, %slot);
//============================================================================

// Configuration
$DualWield::Enabled = true;                    // Master toggle for dual wielding
$DualWield::OffHandSlot = 3;                   // Engine slot for off-hand weapon
$DualWield::OffHandDelayOffset = 0.1;          // Slight delay between primary and off-hand fire (seconds)
$DualWield::AllowForBots = false;              // Whether bots can dual wield
$DualWield::RequiredSkillLevel = 300;          // Required Slashing skill to dual wield
$DualWield::RequiredSkillType = 1;             // Skill type: 1 = Slashing ($SkillSlashing)

// Damage multipliers (0.7 = 70% damage, 1.0 = full damage)
// Per-weapon override: $DualWield::DamageMultiplier[DiamondClaymore] = 0.8;
$DualWield::DefaultDamageMultiplier = 0.7;

// Speed multipliers (1.0 = no penalty, 0.8 = 20% slower)  
// Per-weapon override: $DualWield::SpeedMultiplier[DiamondClaymore] = 0.9;
$DualWield::DefaultSpeedMultiplier = 1.0;

// Spell casting restriction
$DualWield::AllowSpellcasting = false;         // If false, cannot cast spells while dual wielding

// Valid weapon types for dual wielding (melee weapons)
// Uses $AccessoryType values: Sword=7, Axe=8, Polearm=9, Bludgeon=10
$DualWield::AllowedTypes = "7 8 9 10";  // All melee weapon types

//============================================================================
// WEAPON SHAPE MAPPING
// Maps each weapon to its DTS shape file (from weapons.cs)
//============================================================================
// Swords - short_sword shape
$WeaponShape[RustyIronBlade] = "short_sword";
$WeaponShape[SharpIronBlade] = "short_sword";

// Swords - sword shape
$WeaponShape[IronBroadSword] = "sword";
$WeaponShape[SteelBroadSword] = "sword";

// Swords - long_sword shape
$WeaponShape[SteelLongSword] = "long_sword";
$WeaponShape[GoldenLongSword] = "long_sword";

// Swords - katana shape
$WeaponShape[GoldenBastardSword] = "katana";
$WeaponShape[CrystalBastardSword] = "katana";
$WeaponShape[TemperedCrystalBastardSword] = "katana";
$WeaponShape[CrystalClaymore] = "katana";
$WeaponShape[DiamondClaymore] = "katana";

// Swords - elfinblade shape
$WeaponShape[DiamondLegendSword] = "elfinblade";
$WeaponShape[SealFighterBlade] = "elfinblade";
$WeaponShape[SealGuardianBlade] = "elfinblade";
$WeaponShape[BlackDiamondDreamSword] = "elfinblade";
$WeaponShape[BlackDiamondAtomSplitter] = "elfinblade";

// Daggers - dagger shape
$WeaponShape[ButterKnife] = "dagger";
$WeaponShape[LongKnife] = "dagger";
$WeaponShape[Knife] = "dagger";
$WeaponShape[Dagger] = "dagger";
$WeaponShape[CastingBlade] = "dagger";

// Polearms - spear shape
$WeaponShape[IronSpear] = "spear";
$WeaponShape[SteelSpear] = "spear";
$WeaponShape[SteelPike] = "spear";
$WeaponShape[GoldenPike] = "spear";
$WeaponShape[CrystalPike] = "spear";
$WeaponShape[DiamondDeathSpear] = "spear";
$WeaponShape[DiamondLegendSpear] = "spear";
$WeaponShape[BlackDiamondDreamSpear] = "spear";
$WeaponShape[BlackDiamondAtomPiercer] = "spear";

// Polearms - trident shape
$WeaponShape[CrystalTrident] = "trident";
$WeaponShape[TemperedCrystalTrident] = "trident";
$WeaponShape[DiamondTrident] = "trident";

// Bludgeons - mace shape
$WeaponShape[CrackedStick] = "mace";
$WeaponShape[IronStick] = "mace";
$WeaponShape[IronMace] = "mace";
$WeaponShape[SteelMace] = "mace";
$WeaponShape[GoldenDivineMace] = "mace";
$WeaponShape[CrystalDivineMace] = "mace";
$WeaponShape[DiamondDivineMace] = "mace";
$WeaponShape[DiamondBrainSpiller] = "mace";
$WeaponShape[DiamondLegendMace] = "mace";
$WeaponShape[BlackDiamondDreamMace] = "mace";
$WeaponShape[BlackDiamondAtomSmasher] = "mace";
$WeaponShape[Club] = "mace";

// Bludgeons - hammer shape
$WeaponShape[SteelHammer] = "hammer";
$WeaponShape[SteelWarHammer] = "hammer";
$WeaponShape[GoldenWarHammer] = "hammer";

// Axes - hatchet shape
$WeaponShape[Hatchet] = "hatchet";

// Tools - Pick shape
$WeaponShape[PickAxe] = "Pick";

// New weapons (from newstuff.cs)
$WeaponShape[TerminusEst] = "elfinblade";
$WeaponShape[AecoSeorei] = "spear";
$WeaponShape[MorningStar] = "mace";
$WeaponShape[WhiteDiamondVoidCutter] = "elfinblade";
$WeaponShape[WhiteDiamondVoidCrusher] = "hammer";
$WeaponShape[WhiteDiamondVoidImpaler] = "trident";

//============================================================================
// OFF-HAND WEAPON DEFINITIONS (Generic per shape)
// These provide left-hand visuals for any weapon using the same DTS shape
//============================================================================

// Standard off-hand offset (moves weapon to left hand)
// X: -0.65 moves left, Y: 0 no forward/back, Z: -0.16 slight down adjustment

//--- KATANA (needs special rotation) ---
ItemImageData OffHand_KatanaImage
{
	shapeFile  = "katana";
	mountPoint = 0;
	mountRotation = { 0, 1.60, 0 };  // Katana needs extra rotation
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.9;
	accuFire = true;
	sfxFire = SoundSwing3;
};
ItemData OffHand_Katana { heading = "bWeapons"; description = "Off-Hand Katana"; className = "Weapon"; shapeFile = "katana"; imageType = OffHand_KatanaImage; };
function OffHand_KatanaImage::onFire(%player, %slot) { }

//--- SHORT SWORD ---
ItemImageData OffHand_ShortSwordImage
{
	shapeFile  = "short_sword";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.9;
	accuFire = true;
	sfxFire = SoundSwing1;
};
ItemData OffHand_ShortSword { heading = "bWeapons"; description = "Off-Hand Short Sword"; className = "Weapon"; shapeFile = "short_sword"; imageType = OffHand_ShortSwordImage; };
function OffHand_ShortSwordImage::onFire(%player, %slot) { }

//--- SWORD ---
ItemImageData OffHand_SwordImage
{
	shapeFile  = "sword";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.9;
	accuFire = true;
	sfxFire = SoundSwing1;
};
ItemData OffHand_Sword { heading = "bWeapons"; description = "Off-Hand Sword"; className = "Weapon"; shapeFile = "sword"; imageType = OffHand_SwordImage; };
function OffHand_SwordImage::onFire(%player, %slot) { }

//--- LONG SWORD ---
ItemImageData OffHand_LongSwordImage
{
	shapeFile  = "long_sword";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.9;
	accuFire = true;
	sfxFire = SoundSwing2;
};
ItemData OffHand_LongSword { heading = "bWeapons"; description = "Off-Hand Long Sword"; className = "Weapon"; shapeFile = "long_sword"; imageType = OffHand_LongSwordImage; };
function OffHand_LongSwordImage::onFire(%player, %slot) { }

//--- ELFINBLADE ---
ItemImageData OffHand_ElfinbladeImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.9;
	accuFire = true;
	sfxFire = SoundSwing2;
};
ItemData OffHand_Elfinblade { heading = "bWeapons"; description = "Off-Hand Elfinblade"; className = "Weapon"; shapeFile = "elfinblade"; imageType = OffHand_ElfinbladeImage; };
function OffHand_ElfinbladeImage::onFire(%player, %slot) { }

//--- DAGGER ---
ItemImageData OffHand_DaggerImage
{
	shapeFile  = "dagger";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.7;
	accuFire = true;
	sfxFire = SoundSwing1;
};
ItemData OffHand_Dagger { heading = "bWeapons"; description = "Off-Hand Dagger"; className = "Weapon"; shapeFile = "dagger"; imageType = OffHand_DaggerImage; };
function OffHand_DaggerImage::onFire(%player, %slot) { }

//--- SPEAR ---
ItemImageData OffHand_SpearImage
{
	shapeFile  = "spear";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 1.2;
	accuFire = true;
	sfxFire = SoundSwing3;
};
ItemData OffHand_Spear { heading = "bWeapons"; description = "Off-Hand Spear"; className = "Weapon"; shapeFile = "spear"; imageType = OffHand_SpearImage; };
function OffHand_SpearImage::onFire(%player, %slot) { }

//--- TRIDENT ---
ItemImageData OffHand_TridentImage
{
	shapeFile  = "trident";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 1.2;
	accuFire = true;
	sfxFire = SoundSwing3;
};
ItemData OffHand_Trident { heading = "bWeapons"; description = "Off-Hand Trident"; className = "Weapon"; shapeFile = "trident"; imageType = OffHand_TridentImage; };
function OffHand_TridentImage::onFire(%player, %slot) { }

//--- MACE ---
ItemImageData OffHand_MaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 1.0;
	accuFire = true;
	sfxFire = SoundSwing5;
};
ItemData OffHand_Mace { heading = "bWeapons"; description = "Off-Hand Mace"; className = "Weapon"; shapeFile = "mace"; imageType = OffHand_MaceImage; };
function OffHand_MaceImage::onFire(%player, %slot) { }

//--- HAMMER ---
ItemImageData OffHand_HammerImage
{
	shapeFile  = "hammer";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 1.2;
	accuFire = true;
	sfxFire = SoundSwing6;
};
ItemData OffHand_Hammer { heading = "bWeapons"; description = "Off-Hand Hammer"; className = "Weapon"; shapeFile = "hammer"; imageType = OffHand_HammerImage; };
function OffHand_HammerImage::onFire(%player, %slot) { }

//--- CLUB (uses mace shape in-game) ---
ItemImageData OffHand_ClubImage
{
	shapeFile  = "mace";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.9;
	accuFire = true;
	sfxFire = SoundSwing4;
};
ItemData OffHand_Club { heading = "bWeapons"; description = "Off-Hand Club"; className = "Weapon"; shapeFile = "mace"; imageType = OffHand_ClubImage; };
function OffHand_ClubImage::onFire(%player, %slot) { }

//--- PICK (for PickAxe) ---
ItemImageData OffHand_PickImage
{
	shapeFile  = "Pick";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 1.0;
	accuFire = true;
	sfxFire = SoundSwing1;
};
ItemData OffHand_Pick { heading = "bWeapons"; description = "Off-Hand Pick"; className = "Weapon"; shapeFile = "Pick"; imageType = OffHand_PickImage; };
function OffHand_PickImage::onFire(%player, %slot) { }


//--- BATTLEAXE ---
ItemImageData OffHand_BattleAxeImage
{
	shapeFile  = "BattleAxe";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 1.0;
	accuFire = true;
	sfxFire = AxeSlash2;
};
ItemData OffHand_BattleAxe { heading = "bWeapons"; description = "Off-Hand Battle Axe"; className = "Weapon"; shapeFile = "BattleAxe"; imageType = OffHand_BattleAxeImage; };
function OffHand_BattleAxeImage::onFire(%player, %slot) { }

//--- HATCHET ---
ItemImageData OffHand_HatchetImage
{
	shapeFile  = "hatchet";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.8;
	accuFire = true;
	sfxFire = AxeSlash2;
};
ItemData OffHand_Hatchet { heading = "bWeapons"; description = "Off-Hand Hatchet"; className = "Weapon"; shapeFile = "hatchet"; imageType = OffHand_HatchetImage; };
function OffHand_HatchetImage::onFire(%player, %slot) { }

//--- AXE (generic) ---
ItemImageData OffHand_AxeImage
{
	shapeFile  = "axe";
	mountPoint = 0;
	mountRotation = { 0, 0, 0 };
	mountOffSet = { -0.65, 0, -0.16 };
	weaponType = 0;
	fireTime = 0.9;
	accuFire = true;
	sfxFire = AxeSlash2;
};
ItemData OffHand_Axe { heading = "bWeapons"; description = "Off-Hand Axe"; className = "Weapon"; shapeFile = "axe"; imageType = OffHand_AxeImage; };
function OffHand_AxeImage::onFire(%player, %slot) { }

//============================================================================
// OFF-HAND SHAPE MAPPING
// Maps weapon shapeFile names to their off-hand ItemData
//============================================================================
$DualWield::OffHandItem["katana"] = OffHand_Katana;
$DualWield::OffHandItem["short_sword"] = OffHand_ShortSword;
$DualWield::OffHandItem["sword"] = OffHand_Sword;
$DualWield::OffHandItem["long_sword"] = OffHand_LongSword;
$DualWield::OffHandItem["elfinblade"] = OffHand_Elfinblade;
$DualWield::OffHandItem["dagger"] = OffHand_Dagger;
$DualWield::OffHandItem["spear"] = OffHand_Spear;
$DualWield::OffHandItem["trident"] = OffHand_Trident;
$DualWield::OffHandItem["mace"] = OffHand_Mace;
$DualWield::OffHandItem["hammer"] = OffHand_Hammer;
$DualWield::OffHandItem["BattleAxe"] = OffHand_BattleAxe;
$DualWield::OffHandItem["hatchet"] = OffHand_Hatchet;
$DualWield::OffHandItem["axe"] = OffHand_Axe;
$DualWield::OffHandItem["Pick"] = OffHand_Pick;

echo("[DUAL WIELD] Registered 14 off-hand weapon shapes.");

//============================================================================
// CORE FUNCTIONS
//============================================================================

// Check if a player has dual wielding enabled
function DualWield::IsEnabled(%clientId)
{
    if(!$DualWield::Enabled)
        return false;
    
    // Bots can't dual wield unless explicitly allowed
    if(!$DualWield::AllowForBots && isRPGAI(%clientId))
        return false;
    
    %offHandWeapon = DualWield::GetOffHandWeapon(%clientId);
    return (%offHandWeapon != "" && %offHandWeapon != -1);
}

// Get the currently equipped off-hand weapon
function DualWield::GetOffHandWeapon(%clientId)
{
    return fetchData(%clientId, "DualWield_OffHandWeapon");
}

// Check if a weapon type is valid for dual wielding
function DualWield::IsValidWeaponType(%item)
{
    %accessoryType = $AccessoryVar[%item, $AccessoryType];
    
    // Check if this type is in the allowed list
    for(%i = 0; GetWord($DualWield::AllowedTypes, %i) != -1; %i++)
    {
        if(%accessoryType == GetWord($DualWield::AllowedTypes, %i))
            return true;
    }
    
    return false;
}

// Check if a player meets the skill requirement to dual wield
function DualWield::CanDualWield(%clientId)
{
    if(!$DualWield::Enabled)
        return false;
    
    // Bots can't dual wield unless explicitly allowed
    if(!$DualWield::AllowForBots && isRPGAI(%clientId))
        return false;
    
    // Check skill requirement
    // $PlayerSkill is the global array that stores skill levels
    // $SkillSlashing = 1 (defined elsewhere)
    %skillLevel = $PlayerSkill[%clientId, $DualWield::RequiredSkillType];
    if(%skillLevel == "") %skillLevel = 0;
    
    if(%skillLevel < $DualWield::RequiredSkillLevel)
        return false;
    
    return true;
}

// Equip a weapon in the off-hand slot
function DualWield::EquipOffHand(%clientId, %weaponItem)
{
    if(!$DualWield::Enabled)
    {
        Client::sendMessage(%clientId, $MsgRed, "Dual wielding is currently disabled.");
        return false;
    }
    
    // Check skill requirement FIRST
    if(!DualWield::CanDualWield(%clientId))
    {
        %currentSkill = $PlayerSkill[%clientId, $DualWield::RequiredSkillType];
        if(%currentSkill == "") %currentSkill = 0;
        Client::sendMessage(%clientId, $MsgRed, "You need " @ $DualWield::RequiredSkillLevel @ " Slashing skill to dual wield. (Current: " @ %currentSkill @ ")");
        return false;
    }
    
    // Validate the weapon exists
    %itemData = getItemData(%weaponItem);
    if(%itemData == "" || %itemData == -1)
    {
        Client::sendMessage(%clientId, $MsgRed, "Invalid weapon: " @ %weaponItem);
        return false;
    }
    
    // Check if it's a valid dual-wield weapon type
    if(!DualWield::IsValidWeaponType(%weaponItem))
    {
        Client::sendMessage(%clientId, $MsgRed, "This weapon type cannot be dual-wielded. Only swords, axes, and bludgeons are allowed.");
        return false;
    }
    
    // Check if player has the weapon in inventory
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
    {
        return false;
    }
    
    %count = Player::getItemCount(%playerObj, %weaponItem);
    
    // Check if trying to dual wield same weapon as primary
    %primaryWeapon = Player::getMountedItem(%playerObj, $WeaponSlot);
    %requiredCount = 1;
    if(%weaponItem == %primaryWeapon)
    {
        // Need 2 of the same weapon (one in each hand)
        %requiredCount = 2;
    }
    
    if(%count < %requiredCount)
    {
        if(%requiredCount == 2)
            Client::sendMessage(%clientId, $MsgRed, "You need 2 " @ %weaponItem @ " to dual wield the same weapon.");
        else
            Client::sendMessage(%clientId, $MsgRed, "You don't have a " @ %weaponItem @ " to equip.");
        return false;
    }
    
    // Unequip current off-hand if any
    DualWield::UnequipOffHand(%clientId);
    
    // INVENTORY TRANSFER: Decrement weapon count (move to off-hand)
    Player::setItemCount(%playerObj, %weaponItem, %count - 1);
    
    // Store the off-hand weapon name
    storeData(%clientId, "DualWield_OffHandWeapon", %weaponItem);
    
    // DUPE PREVENTION: Save immediately after inventory change
    SaveCharacter(%clientId);
    
    // Get the weapon's shape file using the $WeaponShape global (set where weapons are defined)
    // Fallback: try common shape names based on weapon type
    %shapeFile = $WeaponShape[%weaponItem];
    
    // If no shape defined, try to determine from weapon type
    if(%shapeFile == "" || %shapeFile == -1)
    {
        %weaponType = $AccessoryVar[%weaponItem, $AccessoryType];
        // Default shapes by weapon type (can be overridden per-weapon)
        if(%weaponType == $SwordAccessoryType) %shapeFile = "katana";
        else if(%weaponType == $AxeAccessoryType) %shapeFile = "BattleAxe";
        else if(%weaponType == $PolearmAccessoryType) %shapeFile = "spear";
        else if(%weaponType == $BludgeonAccessoryType) %shapeFile = "mace";
        else %shapeFile = "katana";  // Default fallback
    }
    
    // Look up the generic off-hand item for this shape
    %offHandVisual = $DualWield::OffHandItem[%shapeFile];
    
    if(%offHandVisual != "" && %offHandVisual != -1)
    {
        // Generic off-hand item exists for this shape - mount it to slot 6
        Player::mountItem(%playerObj, %offHandVisual, 6);
        echo("[DUAL WIELD] Mounted generic off-hand '" @ %offHandVisual @ "' for shape '" @ %shapeFile @ "' to slot 6");
    }
    else
    {
        // No off-hand defined for this shape - try weapon-specific "2" suffix as fallback
        %offHandVisual = %weaponItem @ "2";
        %offHandData = getItemData(%offHandVisual);
        
        if(%offHandData != "" && %offHandData != -1)
        {
            Player::mountItem(%playerObj, %offHandVisual, 6);
            echo("[DUAL WIELD] Mounted weapon-specific off-hand '" @ %offHandVisual @ "' to slot 6");
        }
        else
        {
            // Last resort: mount original weapon to slot 6 (won't animate properly)
            Player::mountItem(%playerObj, %weaponItem, 6);
            echo("[DUAL WIELD] WARNING: No off-hand visual found for shape '" @ %shapeFile @ "', mounted original weapon");
        }
    }
    
    %weaponName = %itemData.description;
    if(%weaponName == "") %weaponName = %weaponItem;
    
    Client::sendMessage(%clientId, $MsgBeige, "Off-hand equipped: " @ %weaponName);
    echo("[DUAL WIELD] " @ Client::getName(%clientId) @ " equipped off-hand weapon: " @ %weaponItem);
    
    return true;
}

// Unequip the off-hand weapon
function DualWield::UnequipOffHand(%clientId)
{
    %currentWeapon = DualWield::GetOffHandWeapon(%clientId);
    
    if(%currentWeapon == "" || %currentWeapon == -1)
        return false;  // Nothing to unequip
    
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return false;
    
    // Unmount off-hand visual from slot 6
    Player::unMountItem(%playerObj, 6);
    
    // INVENTORY TRANSFER: Return weapon to inventory (increment count)
    %currentCount = Player::getItemCount(%playerObj, %currentWeapon);
    Player::setItemCount(%playerObj, %currentWeapon, %currentCount + 1);
    
    // Clear stored data
    storeData(%clientId, "DualWield_OffHandWeapon", "");
    
    // DUPE PREVENTION: Save immediately after inventory change
    SaveCharacter(%clientId);
    
    %itemData = getItemData(%currentWeapon);
    %weaponName = %itemData.description;
    if(%weaponName == "") %weaponName = %currentWeapon;
    
    Client::sendMessage(%clientId, $MsgBeige, "Off-hand unequipped: " @ %weaponName);
    echo("[DUAL WIELD] " @ Client::getName(%clientId) @ " unequipped off-hand weapon");
    
    return true;
}

//============================================================================
// FIRE TRIGGER HOOK
//============================================================================
// This function should be called from the primary weapon's fire logic
// to also trigger the off-hand weapon.

function DualWield::OnPrimaryFire(%clientId, %primaryWeapon)
{
    if(!DualWield::IsEnabled(%clientId))
        return;
    
    %offHandWeapon = DualWield::GetOffHandWeapon(%clientId);
    if(%offHandWeapon == "" || %offHandWeapon == -1)
        return;
    
    // Schedule off-hand fire with slight delay for visual effect
    if($DualWield::OffHandDelayOffset > 0)
    {
        schedule("DualWield::FireOffHand(" @ %clientId @ ", \"" @ %offHandWeapon @ "\");", $DualWield::OffHandDelayOffset);
    }
    else
    {
        DualWield::FireOffHand(%clientId, %offHandWeapon);
    }
}

// Fire the off-hand weapon (called after primary fire)
function DualWield::FireOffHand(%clientId, %offHandWeapon)
{
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return;
    
    // Verify off-hand is still equipped
    if(DualWield::GetOffHandWeapon(%clientId) != %offHandWeapon)
        return;
    
    // Trigger the off-hand weapon on slot 6 (where it's visually mounted)
    // Using slot 6 ensures the weapon's fire animation plays from its DTS file
    Player::trigger(%playerObj, 6, true);
    
    // Schedule trigger release
    schedule("DualWield::ReleaseOffHandTrigger(" @ %clientId @ ");", 0.1);
}

function DualWield::ReleaseOffHandTrigger(%clientId)
{
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj != "" && %playerObj != -1)
    {
        Player::trigger(%playerObj, 6, false);  // Release on slot 6 to match fire trigger
    }
}

//============================================================================
// DAMAGE MODIFIER
//============================================================================
// This should be called from the damage calculation to apply off-hand penalty

function DualWield::GetDamageMultiplier(%clientId, %slot)
{
    if(%slot == $DualWield::OffHandSlot && DualWield::IsEnabled(%clientId))
    {
        return $DualWield::OffHandDamageMultiplier;
    }
    return 1.0;
}

//============================================================================
// CHAT COMMAND
//============================================================================

function DualWield::Command(%clientId, %args)
{
    %arg1 = GetWord(%args, 0);
    
    if(%arg1 == "" || %arg1 == "help")
    {
        Client::sendMessage(%clientId, $MsgBeige, "Dual Wield Commands:");
        Client::sendMessage(%clientId, $MsgBeige, "  #dualwield <weapon>  - Equip weapon in off-hand");
        Client::sendMessage(%clientId, $MsgBeige, "  #dualwield off       - Unequip off-hand");
        Client::sendMessage(%clientId, $MsgBeige, "  #dualwield status    - Show current off-hand");
        return;
    }
    
    if(%arg1 == "off" || %arg1 == "none" || %arg1 == "unequip")
    {
        DualWield::UnequipOffHand(%clientId);
        return;
    }
    
    if(%arg1 == "status")
    {
        %weapon = DualWield::GetOffHandWeapon(%clientId);
        if(%weapon == "" || %weapon == -1)
            Client::sendMessage(%clientId, $MsgBeige, "Off-hand: None");
        else
            Client::sendMessage(%clientId, $MsgBeige, "Off-hand: " @ %weapon);
        return;
    }
    
    // Try to equip the specified weapon
    DualWield::EquipOffHand(%clientId, %arg1);
}

//============================================================================
// CLEANUP (on disconnect/death)
//============================================================================

function DualWield::OnPlayerDisconnect(%clientId)
{
    // Ensure off-hand weapon is returned to inventory before disconnect
    // (inventory saving should handle this, but just in case)
    DualWield::UnequipOffHand(%clientId);
}

function DualWield::OnPlayerDeath(%clientId)
{
    // Drop off-hand weapon on death (or return to inventory based on game rules)
    // For now, just unequip it
    DualWield::UnequipOffHand(%clientId);
}

//============================================================================
// INITIALIZATION
//============================================================================

if($DualWield::Enabled)
    echo("[DUAL WIELD] Dual Wielding system loaded. Master toggle: ENABLED");
else
    echo("[DUAL WIELD] Dual Wielding system loaded. Master toggle: DISABLED");

echo("[DUAL WIELD] Off-hand slot: " @ $DualWield::OffHandSlot @ ", Damage multiplier: " @ $DualWield::OffHandDamageMultiplier);
echo("[DUAL WIELD] Skill requirement: " @ $DualWield::RequiredSkillLevel @ " Slashing");

//============================================================================
// TEST WEAPON: DualWieldTest
//============================================================================
// A test weapon for dual wielding using the DevilsClaw mount pattern.
// Uses mountPoint = 2 with offset to position in the off-hand.
//
// USAGE:
//   #give DualWieldTest 2
//   Equip primary normally (press key to use weapon)
//   Then: #dualwield DualWieldTest
//
// The off-hand weapon visually appears in the other hand via slot 6.

// Weapon stats
$WeaponRange[DualWieldTest] = 4;
$WeaponDelay[DualWieldTest] = 0.9;
$AccessoryVar[DualWieldTest, $AccessoryType] = $SwordAccessoryType;  // Type 7 = Sword
$AccessoryVar[DualWieldTest, $SpecialVar] = "6 500";  // 50 ATK
$AccessoryVar[DualWieldTest, $Weight] = 5;
$AccessoryVar[DualWieldTest, $MiscInfo] = "A test sword for dual wielding.";
$SkillType[DualWieldTest] = $SkillSlashing;
$ItemCost[DualWieldTest] = 1;

//--------------------------------------------
// PRIMARY WEAPON (main hand, slot 0)
// Copied from Rapier pattern
//--------------------------------------------
ItemImageData DualWieldTestImage
{
	shapeFile  = "katana";
	mountPoint = 0;
	weaponType = 0;
	reloadTime = 0;
	fireTime = $WeaponDelay[DualWieldTest];
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};

ItemData DualWieldTest
{
	heading = "bWeapons";
	description = "Dual Wield Test Sword";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = DualWieldTestImage;
	price = 0;
	showWeaponBar = true;
};

//--------------------------------------------
// OFF-HAND WEAPON (other hand, slot 6)
// mountPoint 2 = BackpackMount, using offset to move to left hand
//--------------------------------------------
ItemImageData DualWieldTestImage2
{
	shapeFile  = "katana";
	// Use mountPoint 0 (same as primary weapons) to allow proper fire animation
	// Use larger offset to position in left hand instead of right
	mountPoint = 0;
	mountRotation = { 0, 1.60, 0 }; // first is forward/backward rotation -- second is left/right rotation -- idk what third is i didnt need it
	mountOffSet = { -0.65, 0, -0.16 };  // Move left (-X) to opposite hand -- first one is left/right, second is forwards/backward, third is vertical up/down
	weaponType = 0;
	reloadTime = 0;
	fireTime = $WeaponDelay[DualWieldTest];
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;
	
	// CRITICAL: Need sfxFire to trigger animation properly
	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};

ItemData DualWieldTest2
{
	heading = "bWeapons";
	description = "Dual Wield Test Sword";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = DualWieldTestImage2;
	price = 0;
	showWeaponBar = true;
};

// CRITICAL: onFire callback for off-hand weapon - called when setImageTrigger fires on slot 6
function DualWieldTestImage2::onFire(%player, %slot)
{
	%clientId = Player::getClient(%player);
	echo("[DUAL WIELD TEST] Off-hand fire from slot " @ %slot);
	
	// The actual damage is handled separately via FireOffHandMelee
	// This callback processes the trigger event - animation should play automatically
}

//--------------------------------------------
// MOUNT/UNMOUNT CALLBACKS
// For dual wielding: mount off-hand when primary is mounted
// (opposite of normal Rapier which shows sheathed weapon)
//--------------------------------------------

function DualWieldTest::onMount(%player, %imageSlot)
{
	%client = GameBase::getOwnerClient(%player);
	
	// Clear any previously mounted off-hand items
	Player::unmountItem(%player, 4);
	Player::unmountItem(%player, 5);
	Player::unmountItem(%player, 6);
	
	// If dual wielding is enabled, mount the off-hand weapon
	if(DualWield::IsEnabled(%client))
	{
		Player::mountItem(%player, DualWieldTest2, 6);
		echo("[DUAL WIELD TEST] Mounted off-hand to slot 6");
	}
}

function DualWieldTest::onUnmount(%player, %imageSlot)
{
	// Unmount off-hand when primary is unmounted
	Player::unmountItem(%player, 6);
	echo("[DUAL WIELD TEST] Unmounted off-hand from slot 6");
}

//--------------------------------------------
// FIRE CALLBACKS
//--------------------------------------------

function DualWieldTestImage::onFire(%player, %slot)
{
	%clientId = Player::getClient(%player);
	
	// Deal primary damage
	MeleeAttack(%player, $WeaponRange[DualWieldTest], DualWieldTest);
	
	echo("[DUAL WIELD TEST] Primary fire from slot " @ %slot);
	
	// Trigger off-hand attack if dual wielding is active
	if(DualWield::IsEnabled(%clientId))
	{
		// Call OnPrimaryFire which triggers the animation via setImageTrigger
		DualWield::OnPrimaryFire(%clientId, DualWieldTest);
		
		// Schedule off-hand DAMAGE with slight delay (after animation starts)
		schedule("DualWield::FireOffHandMelee(" @ %clientId @ ", " @ %player @ ", DualWieldTest);", $DualWield::OffHandDelayOffset);
	}
}

//--------------------------------------------
// OFF-HAND MELEE ATTACK HELPER
//--------------------------------------------
// Called via schedule from primary fire

function DualWield::FireOffHandMelee(%clientId, %player, %weaponType)
{
    if(!DualWield::IsEnabled(%clientId))
        return;
    
    // Get fresh player object from clientId (the passed %player may be stale from schedule string)
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return;
    
    // Get the off-hand weapon being used
    %offHandWeapon = DualWield::GetOffHandWeapon(%clientId);
    if(%offHandWeapon == "" || %offHandWeapon == -1)
        %offHandWeapon = %weaponType;  // Fallback to passed weapon
    
    // Deal off-hand damage DIRECTLY (bypass MeleeAttack's anti-spam timer)
    %range = $WeaponRange[%offHandWeapon];
    if(%range == "" || %range == -1) %range = 4;
    
    // Direct LOS check and damage (same logic as MeleeAttack but without anti-spam)
    $los::object = "";
    if(GameBase::getLOSinfo(%playerObj, %range))
    {
        %obj = getObjectType($los::object);
        if(%obj == "Player")
        {
            // Deal damage with the off-hand weapon
            GameBase::virtual($los::object, "onDamage", $BulletDamageType, 1.0, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, %offHandWeapon);
            echo("[DUAL WIELD] Off-hand HIT on " @ $los::object @ " with " @ %offHandWeapon);
        }
    }
    
    // Still call PostAttack for any post-attack effects
    PostAttack(%clientId, %offHandWeapon);
    
    echo("[DUAL WIELD] Off-hand attack executed for " @ Client::getName(%clientId) @ " with " @ %offHandWeapon);
}

echo("[DUAL WIELD] Test weapon 'DualWieldTest' registered.");
echo("[DUAL WIELD] Usage: #give DualWieldTest 2, equip primary, then #dualwield DualWieldTest");

