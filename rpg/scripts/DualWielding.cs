//============================================================================
// DUAL WIELDING SYSTEM - Kingdom of Kronos - Made by Jobo
//============================================================================
// This module implements dual-wielding functionality.
// 
// HOW IT WORKS:
//   - Primary weapon uses $WeaponSlot (slot 0 - standard behavior)
//   - Off-hand weapon name is stored in player data, visual mounted in slot 6
//   - Weapons STAY in inventory while equipped (no removal/duplication)
//   - When primary weapon fires, off-hand attack triggers after delay ($DualWield::OffHandDelayOffset)
//   - Off-hand damage uses its own weapon stats (configurable multiplier available)
//   - Equipping a new weapon auto-cycles: old off-hand unequips, main becomes off-hand, new becomes main
//
// TOGGLE MODE:
//   - Players can enter "dual wield mode" with #dualwield (no args)
//   - While in toggle mode, equipping weapons automatically sets up dual wielding
//   - Equipping a weapon pushes the current main weapon to off-hand
//   - Toggle mode persists across death/respawn (disabled on remort)
//
// SKILL REQUIREMENT:
//   - Players must have Ascension talent "DualWielding" to dual wield
//  - Can be changed to skill requirement if needed 
// LIMITATIONS:
//   - Off-hand weapon uses slot 6 for visual display
//   - AI bots dual wielding is disabled by default ($DualWield::AllowForBots)
//   - Weapons still require their individual skill/remort requirements
//
// USAGE:
//   DualWield::EquipOffHand(%clientId, %weaponItem)   - Equip off-hand weapon
//   DualWield::UnequipOffHand(%clientId)              - Unequip off-hand weapon
//   DualWield::GetOffHandWeapon(%clientId)            - Get current off-hand weapon
//   DualWield::IsEnabled(%clientId)                   - Check if dual wielding is active
//   DualWield::CanDualWield(%clientId)                - Check if player meets skill requirement
//   DualWield::GetToggleMode(%clientId)               - Get toggle mode state
//   DualWield::SetToggleMode(%clientId, %enabled)     - Set toggle mode state
//   DualWield::ToggleMode(%clientId)                  - Flip toggle mode on/off
//   DualWield::HandleEquipInToggleMode(%clientId, %item) - Handle equip in toggle mode
//
// COMMANDS:
//   #dualwield             - Toggle dual wield mode on/off
//   #dualwield <weaponName>  - Equip specified weapon in off-hand
//   #dualwield off         - Unequip off-hand weapon
//   #dualwield status      - Show current off-hand, mode state, and requirements
//   #dualwield help        - Show command help
//
//
// INTEGRATION REQUIRED (other script modifications):
//   
//   1. comchat.cs - Command handling for #dualwield command:
//      Add: if(GetWord(%msg, 0) == "#dualwield") { DualWield::Command(%clientId, GetWords(%msg, 1, 99)); return; }
//   
//   2. weaponHandling.cs - Weapon fire logic (DealDamage or similar):
//      Add: DualWield::OnPrimaryFire(%clientId, %weapon);
//   
//   3. weaponHandling.cs - remoteNextWeapon() function (near end):
//      Add: DualWield::RefreshOffHandVisual(%clientId);
//      Purpose: Re-mount off-hand visual after scroll wheel weapon switch
//   
//   4. weaponHandling.cs - remotePrevWeapon() function (near end):
//      Add: DualWield::RefreshOffHandVisual(%clientId);
//      Purpose: Re-mount off-hand visual after scroll wheel weapon switch
//   
//   5. weaponHandling.cs - Weapon::onUse() function (line ~149):
//      Add: if(DualWield::HandleEquipInToggleMode(%clientId, %item)) return;
//      Purpose: Allows GUI equipping to automatically set up dual wielding when toggle mode is active
//
//   6. rpgstats.cs - DoRemort() function (line ~1015):
//      Add: DualWield::UnequipOffHand(%clientId);
//      Add: DualWield::SetToggleMode(%clientId, false);
//      Purpose: Unequip off-hand and disable toggle mode when player remorts
//
//   7. playerspawn.cs - Game::playerSpawned() function (near end):
//      Add: schedule("DualWield::RestoreOffHandVisual(" @ %clientId @ ");", 0.5);
//      Purpose: Restore off-hand weapon visual after player spawn/reconnect
//
//   8. rpgfunk.cs - SaveCharacter() function:
//      Add: Save DualWield_OffHandWeapon to $funk::var[%name, 0, 53]
//      Purpose: Persist equipped off-hand weapon across sessions
//
//   9. rpgfunk.cs - LoadCharacter() function:
//      Add: Load $funk::var[%name, 0, 53] to DualWield_OffHandWeapon
//      Purpose: Restore off-hand weapon name on reconnect
//
// INVENTORY DESIGN (Option C):
//   - Off-hand weapons STAY IN INVENTORY while equipped
//   - No decrement/increment on equip/unequip
//   - This prevents item loss on reconnect since save file only stores weapon name
//   - Death: Weapons drop in pack since they're still in inventory
//   - Remort: UnequipOffHand is called which clears the stored weapon name
//
// GLOBAL VARIABLES USED FROM OTHER SCRIPTS:
//   $PlayerSkill[%clientId, %skillType] - Player skill levels
//   $SkillRestriction[%weapon] - Weapon skill/remort requirements
//   $SkillName[%skillType] - Skill names for messages
//   $WeaponSlot - Primary weapon slot (usually 0)
//   $MsgRed, $MsgYellow, $MsgBeige, $MsgGreen - Message colors
//   $MinLevel, $MinRemort - Restriction type constants
//
// DATA FIELDS STORED:
//   DualWield_OffHandWeapon - Current off-hand weapon name
//   DualWield_ToggleMode - Toggle mode state (1 = on, empty = off)
//
// FUNCTIONS CALLED FROM OTHER SCRIPTS:
//   GetRemort(%clientId) - Get player remort count
//   RPGlevel(%clientId) - Get player RPG level
//   SaveCharacter(%clientId) - Persist character data
//   Client::getOwnedObject(%clientId) - Get player object
//   Player::getMountedItem(%player, %slot) - Get equipped weapon
//   Player::getItemCount(%player, %item) - Get inventory count
//   Player::setItemCount(%player, %item, %count) - Set inventory count
//   Player::mountItem(%player, %item, %slot) - Mount item visual
//   Player::unMountItem(%player, %slot) - Unmount item visual
//   fetchData(%clientId, %key) / storeData(%clientId, %key, %value) - Data storage
//============================================================================

// Configuration
$DualWield::Enabled = true;                    // Master toggle for dual wielding
// NOTE: Off-hand weapon is displayed in slot 6 (hardcoded)
$DualWield::OffHandDelayOffset = 0.1;          // Slight delay between primary and off-hand fire (seconds)
$DualWield::AllowForBots = false;              // Whether bots can dual wield

// OLD SKILL REQUIREMENT (replaced by Ascension talent system)
// $DualWield::RequiredSkillLevel = 300;        // Required Slashing skill to dual wield
// $DualWield::RequiredSkillType = 1;           // Skill type: 1 = Slashing ($SkillSlashing)
// NOTE: Dual wielding requires the "DualWielding" Ascension talent (legacy alias "DualWield" is supported)

// Damage multipliers (0.7 = 70% damage, 1.0 = full damage)
// Per-weapon override: $DualWield::DamageMultiplier[DiamondClaymore] = 0.8;
$DualWield::DefaultDamageMultiplier = 1.0;

// Speed multipliers (1.0 = no penalty, 0.8 = 20% slower)  
// Per-weapon override: $DualWield::SpeedMultiplier[DiamondClaymore] = 0.9;
$DualWield::DefaultSpeedMultiplier = 1.0;

// Spell casting restriction
$DualWield::AllowSpellcasting = true;         // If false, cannot cast spells while dual wielding

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
$WeaponShape[FinalVerdict] = "BattleAxe";
$WeaponShape[Test] = "Test";
$WeaponShape[StormCaller] = "trident";
$WeaponShape[WorldSplitter] = "katana";
$WeaponShape[SoulReaver] = "hammer";
$WeaponShape[SkyRender] = "trident";
$WeaponShape[EchoFang] = "katana";

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
	mountRotation = { 0, 1.60, 0 }; // first is forward/backward rotation -- second is left/right rotation -- idk what third is i didnt need it
	mountOffSet = { -0.65, 0, -0.16 };  // Move left (-X) to opposite hand -- first one is left/right, second is forwards/backward, third is vertical up/down
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
	mountRotation = { 0, 1.50, 0 };
	mountOffSet = { -0.80, 0, 0 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
	mountOffSet = { -0.85, 0, -0.16 };
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
    
    // CRITICAL: Verify player has the Ascension talent
    // This prevents double attacks if off-hand weapon data exists but player lacks the talent
    if(!Ascension::HasTalent(%clientId, "DualWielding"))
        return false;
    
    %offHandWeapon = DualWield::GetOffHandWeapon(%clientId);
    return (%offHandWeapon != "" && %offHandWeapon != -1 && %offHandWeapon != "0");
}


// Get the currently equipped off-hand weapon
function DualWield::GetOffHandWeapon(%clientId)
{
    return fetchData(%clientId, "DualWield_OffHandWeapon");
}

// Restore off-hand weapon visual after player spawn/reconnect
// Called from Game::playerSpawned with a short delay to ensure main weapon is mounted first
function DualWield::RestoreOffHandVisual(%clientId)
{
    %offHandWeapon = fetchData(%clientId, "DualWield_OffHandWeapon");
    
    // No off-hand weapon saved
    if(%offHandWeapon == "" || %offHandWeapon == -1 || %offHandWeapon == "0")
        return;
    
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return;
    
    // Verify player still has the weapon in inventory (Option C: weapons stay in inventory)
    %count = Player::getItemCount(%playerObj, %offHandWeapon);
    if(%count < 1)
    {
        // Weapon no longer in inventory - clear the saved state
        storeData(%clientId, "DualWield_OffHandWeapon", "");
        return;
    }

    // EXPLOIT FIX: same-item-in-both-hands requires two copies (mirrors EquipOffHand)
    %mountedMain = Player::getMountedItem(%playerObj, $WeaponSlot);
    if(%mountedMain == %offHandWeapon && %count < 2)
    {
        storeData(%clientId, "DualWield_OffHandWeapon", "");
        return;
    }
    
    // Check if player still meets dual wield requirements
    if(!DualWield::CanDualWield(%clientId))
    {
        // No longer meets requirements - unequip and clear
        storeData(%clientId, "DualWield_OffHandWeapon", "");
        DualWield::SetToggleMode(%clientId, false);
        return;
    }
    
    // Get the visual item to mount
    %shapeFile = $WeaponShape[%offHandWeapon];
    if(%shapeFile == "" || %shapeFile == -1)
    {
        %weaponType = $AccessoryVar[%offHandWeapon, $AccessoryType];
        if(%weaponType == $SwordAccessoryType) %shapeFile = "katana";
        else if(%weaponType == $AxeAccessoryType) %shapeFile = "BattleAxe";
        else if(%weaponType == $PolearmAccessoryType) %shapeFile = "spear";
        else if(%weaponType == $BludgeonAccessoryType) %shapeFile = "mace";
        else %shapeFile = "katana";
    }
    
    %offHandVisual = $DualWield::OffHandItem[%shapeFile];
    if(%offHandVisual != "" && %offHandVisual != -1)
    {
        Player::mountItem(%playerObj, %offHandVisual, 6);
    }
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

// Check if a player meets the requirement to dual wield (Ascension talent)
function DualWield::CanDualWield(%clientId)
{
    if(!$DualWield::Enabled)
        return false;
    
    // Bots can't dual wield unless explicitly allowed
    if(!$DualWield::AllowForBots && isRPGAI(%clientId))
        return false;
    
    // Check Ascension talent - this is the ONLY way to unlock dual wielding
    if(!Ascension::HasTalent(%clientId, "DualWielding"))
        return false;
    
    return true;
}

//============================================================================
// TOGGLE MODE STATE
//============================================================================
// Toggle mode allows players to equip weapons through the normal GUI
// and have them automatically set up dual wielding.

// Get the current toggle mode state for a player
function DualWield::GetToggleMode(%clientId)
{
    %state = fetchData(%clientId, "DualWield_ToggleMode");
    if(%state == "" || %state == "0" || %state == -1)
        return false;
    return true;
}

// Set the toggle mode state for a player
function DualWield::SetToggleMode(%clientId, %enabled)
{
    if(%enabled)
        storeData(%clientId, "DualWield_ToggleMode", "1");
    else
        storeData(%clientId, "DualWield_ToggleMode", "");
}

// Toggle the dual wield mode on/off
function DualWield::ToggleMode(%clientId)
{
    if(!DualWield::CanDualWield(%clientId))
    {
        Client::sendMessage(%clientId, $MsgRed, "You need the Ascension 'Dual Wielding' talent to dual wield.");
        return;
    }
    
    %currentState = DualWield::GetToggleMode(%clientId);
    
    if(%currentState)
    {
        // Turn off
        DualWield::SetToggleMode(%clientId, false);
        Client::sendMessage(%clientId, $MsgYellow, "Dual wield mode: OFF - Equipping weapons normally");
    }
    else
    {
        // Turn on
        DualWield::SetToggleMode(%clientId, true);
        Client::sendMessage(%clientId, $MsgGreen, "Dual wield mode: ON - Equipping weapons will set up dual wielding");
    }
}

// Handle weapon equip when toggle mode is active
// Returns true if handled (don't do normal equip), false to continue normal equip
function DualWield::HandleEquipInToggleMode(%clientId, %weaponItem)
{
    // Double-check mode is active
    if(!DualWield::GetToggleMode(%clientId))
        return false;
    
    // Check if player can dual wield
    if(!DualWield::CanDualWield(%clientId))
        return false;
    
    // Check if this is a valid weapon type for dual wielding
    if(!DualWield::IsValidWeaponType(%weaponItem))
        return false;  // Not a dual-wieldable weapon, equip normally
    
    // Check weapon skill/remort requirements
    if(!DualWield::CheckWeaponRestriction(%clientId, %weaponItem))
        return false;  // Message already sent, let normal equip fail too
    
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return false;
    
    // Get current main weapon
    %currentMain = Player::getMountedItem(%playerObj, $WeaponSlot);
    
    // Case 1: No main weapon - just equip normally
    if(%currentMain == "" || %currentMain == -1)
        return false;  // Let normal equip handle it
    
    // Case 2: Trying to equip same weapon as main
    if(%currentMain == %weaponItem)
    {
        // Check if player has 2 of this weapon
        %count = Player::getItemCount(%playerObj, %weaponItem);
        if(%count < 2)
        {
            // Only have 1 - just let normal equip handle it (does nothing since already equipped)
            return false;
        }
        // They have 2 - equip to off-hand
        DualWield::EquipOffHand(%clientId, %weaponItem);
        return true;  // We handled it
    }
    
    // Case 3: Different weapon - push current main to off-hand
    // First, unequip any existing off-hand (returns to inventory)
    %currentOffHand = DualWield::GetOffHandWeapon(%clientId);
    if(%currentOffHand != "" && %currentOffHand != -1)
    {
        DualWield::UnequipOffHand(%clientId);
    }
    
    // Move current main weapon to off-hand
    // The current main weapon is already in inventory (mounted items are still in inventory)
    // We need to equip it as off-hand
    DualWield::EquipOffHand(%clientId, %currentMain);
    
    // Let normal equip proceed to mount the new weapon as main
    return false;
}

// Check if player meets the $SkillRestriction requirements for a weapon
// Returns true if player can use the weapon, false if not
// Sets %reasonOut to a message explaining why if check fails
function DualWield::CheckWeaponRestriction(%clientId, %weaponItem)
{
    %restriction = $SkillRestriction[%weaponItem];
    
    // No restriction defined - weapon can be equipped
    if(%restriction == "" || %restriction == -1)
        return true;
    
    // Parse restriction format: "SkillType RequiredAmount [MinRemort RemortAmount]"
    // Example: "$SkillSlashing 2500 $MinRemort 75"
    %skillType = GetWord(%restriction, 0);
    %skillRequired = GetWord(%restriction, 1);
    %remortType = GetWord(%restriction, 2);
    %remortRequired = GetWord(%restriction, 3);
    
    // Check skill requirement
    if(%skillType != "" && %skillType != -1 && %skillRequired != "" && %skillRequired != -1)
    {
        // $MinLevel check
        if(%skillType == $MinLevel)
        {
            %playerLevel = RPGlevel(%clientId);
            if(%playerLevel < %skillRequired)
            {
                Client::sendMessage(%clientId, $MsgRed, "You need level " @ %skillRequired @ " to use this weapon. (Current: " @ %playerLevel @ ")");
                return false;
            }
        }
        // $MinRemort check (if only remort, no skill)
        else if(%skillType == $MinRemort)
        {
            %playerRemort = GetRemort(%clientId);
            if(%playerRemort < %skillRequired)
            {
                Client::sendMessage(%clientId, $MsgRed, "You need " @ %skillRequired @ " remorts to use this weapon. (Current: " @ %playerRemort @ ")");
                return false;
            }
        }
        // Regular skill check
        else
        {
            %playerSkill = $PlayerSkill[%clientId, %skillType];
            if(%playerSkill == "") %playerSkill = 0;
            
            if(%playerSkill < %skillRequired)
            {
                %skillName = $SkillName[%skillType];
                if(%skillName == "") %skillName = "Skill #" @ %skillType;
                Client::sendMessage(%clientId, $MsgRed, "You need " @ %skillRequired @ " " @ %skillName @ " skill to use this weapon. (Current: " @ %playerSkill @ ")");
                return false;
            }
        }
    }
    
    // Check additional remort requirement (word 2 and 3)
    if(%remortType == $MinRemort && %remortRequired != "" && %remortRequired != -1)
    {
        %playerRemort = GetRemort(%clientId);
        if(%playerRemort < %remortRequired)
        {
            Client::sendMessage(%clientId, $MsgRed, "You need " @ %remortRequired @ " remorts to use this weapon. (Current: " @ %playerRemort @ ")");
            return false;
        }
    }
    
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
    
    // Check Ascension talent requirement FIRST
    if(!DualWield::CanDualWield(%clientId))
    {
        Client::sendMessage(%clientId, $MsgRed, "You need the Ascension 'Dual Wielding' talent to dual wield.");
        return false;
    }
    
    // Auto-unequip existing off-hand before equipping new one
    %currentOffHand = fetchData(%clientId, "DualWield_OffHandWeapon");
    if(%currentOffHand != "" && %currentOffHand != -1 && %currentOffHand != "0")
    {
        DualWield::UnequipOffHand(%clientId);
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
    
    // Check if player meets the weapon's skill/remort requirements
    if(!DualWield::CheckWeaponRestriction(%clientId, %weaponItem))
    {
        // Message already sent by CheckWeaponRestriction
        return false;
    }
    
    // Check if player has the weapon in inventory
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
    {
        return false;
    }
    
    %count = Player::getItemCount(%playerObj, %weaponItem);

    // Option C: Weapons stay in inventory while equipped. Player just needs to own at least 1.
    if(%count < 1)
    {
        Client::sendMessage(%clientId, $MsgRed, "You don't have a " @ %weaponItem @ " to equip.");
        return false;
    }

    // EXPLOIT FIX: dual-wielding the SAME item as the mounted main weapon requires
    // owning TWO of it - one physical item can't be in both hands. The toggle-mode
    // path already enforced this (count >= 2); the direct #dualwield <weapon> path
    // didn't, allowing double attacks from a single item.
    %mountedMain = Player::getMountedItem(%playerObj, $WeaponSlot);
    if(%mountedMain == %weaponItem && %count < 2)
    {
        Client::sendMessage(%clientId, $MsgRed, "You need two of that weapon to wield one in each hand.");
        return false;
    }
    
    
    // NOTE: Previous off-hand was already unequipped above (lines 795-800)
    
    // NOTE (Option C): Weapon stays in inventory while equipped as off-hand.
    // We just track it via DualWield_OffHandWeapon. This prevents item loss on reconnect
    // since the save file only stores the weapon name, not a separate inventory count.
    
    // Store the off-hand weapon name
    storeData(%clientId, "DualWield_OffHandWeapon", %weaponItem);
    
    // Save to persist the equipped off-hand weapon
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
    
    // Get weapon display name - prefer description, fall back to item name
    %weaponName = %itemData.description;
    if(%weaponName == "" || %weaponName == "Tool" || %weaponName == "Weapon")
        %weaponName = %weaponItem;
    
    Client::sendMessage(%clientId, $MsgBeige, "Off-hand equipped: " @ %weaponName);
    echo("[DUAL WIELD] " @ Client::getName(%clientId) @ " equipped off-hand weapon: " @ %weaponItem);
    
    return true;
}

// Refresh/re-mount the off-hand weapon visual (called after switching primary weapon)
// This fixes the visual disappearing when using scroll wheel to switch weapons
// BUGFIX: this used to read $DualWield::OffHandImage[%weapon] - an array nothing
// ever writes (the real map is $DualWield::OffHandItem[shape]) - so the refresh
// was a no-op and the off-hand visual vanished on every scroll-wheel weapon
// switch while the damage kept firing invisibly. RestoreOffHandVisual does the
// correct shape lookup (plus inventory/talent validation), so delegate to it.
function DualWield::RefreshOffHandVisual(%clientId)
{
    DualWield::RestoreOffHandVisual(%clientId);
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
    
    // NOTE (Option C): Weapon stays in inventory - no transfer needed.
    // We just clear the tracking variable.
    
    // Clear stored data
    storeData(%clientId, "DualWield_OffHandWeapon", "");
    
    // Save to persist the unequipped state
    SaveCharacter(%clientId);
    
    %itemData = getItemData(%currentWeapon);
    // Use $AccessoryVar for proper weapon name, fallback to description or item name
    %weaponName = $AccessoryVar[%currentWeapon, $MiscInfo];
    if(%weaponName == "" || %weaponName == -1)
    {
        %weaponName = %itemData.description;
        if(%weaponName == "" || %weaponName == "Weapon" || %weaponName == "Tool")
            %weaponName = %currentWeapon;
    }
    else
    {
        // MiscInfo is description, use the item name for display
        %weaponName = %currentWeapon;
    }
    
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
    
    // CRITICAL: Block off-hand attacks during skill upgrade RefreshAll to prevent attack spam exploit
    if($SkillUpgradeRefreshScheduled[%clientId] == "true")
        return;
    
    %offHandWeapon = DualWield::GetOffHandWeapon(%clientId);
    if(%offHandWeapon == "" || %offHandWeapon == -1 || %offHandWeapon == "0")
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
    // Safety check: Verify dual wielding is still enabled (could have been disabled between schedule and fire)
    if(!DualWield::IsEnabled(%clientId))
        return;
    
    // CRITICAL: Block off-hand attacks during skill upgrade RefreshAll to prevent attack spam exploit
    if($SkillUpgradeRefreshScheduled[%clientId] == "true")
        return;
    
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return;
    
    // Verify off-hand is still equipped (same weapon)
    %currentOffHand = DualWield::GetOffHandWeapon(%clientId);
    if(%currentOffHand != %offHandWeapon || %currentOffHand == "" || %currentOffHand == -1 || %currentOffHand == "0")
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
    
    // No args = toggle mode
    if(%arg1 == "" || %arg1 == -1)
    {
        DualWield::ToggleMode(%clientId);
        return;
    }
    
    if(%arg1 == "help")
    {
        Client::sendMessage(%clientId, $MsgBeige, "Dual Wield Commands:");
        Client::sendMessage(%clientId, $MsgBeige, "  #dualwield           - Toggle dual wield mode on/off");
        Client::sendMessage(%clientId, $MsgBeige, "  #dualwield <weapon>  - Equip weapon in off-hand");
        Client::sendMessage(%clientId, $MsgBeige, "  #dualwield off       - Unequip off-hand");
        Client::sendMessage(%clientId, $MsgBeige, "  #dualwield status    - Show current off-hand and mode");
        return;
    }
    
    if(%arg1 == "off" || %arg1 == "none" || %arg1 == "unequip" || %arg1 == "disable")
    {
        DualWield::UnequipOffHand(%clientId);
        DualWield::SetToggleMode(%clientId, false);
        Client::sendMessage(%clientId, $MsgYellow, "Dual wield mode: OFF");
        return;
    }
    
    if(%arg1 == "status")
    {
        %weapon = DualWield::GetOffHandWeapon(%clientId);
        %modeState = DualWield::GetToggleMode(%clientId);
        
        if(%modeState)
            Client::sendMessage(%clientId, $MsgGreen, "Dual wield mode: ON");
        else
            Client::sendMessage(%clientId, $MsgYellow, "Dual wield mode: OFF");
        
        if(%weapon == "" || %weapon == -1)
            Client::sendMessage(%clientId, $MsgBeige, "Off-hand: None");
        else
            Client::sendMessage(%clientId, $MsgBeige, "Off-hand: " @ %weapon);
        return;
    }
    
    if(%arg1 == "mode" || %arg1 == "toggle" || %arg1 == "on")
    {
        DualWield::ToggleMode(%clientId);
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
// OFF-HAND MELEE ATTACK HELPER
//============================================================================
// Called via schedule from primary weapon fire callbacks in weapons.cs
// This deals off-hand damage after the animation delay

function DualWield::FireOffHandMelee(%clientId, %player, %weaponType)
{
    if(!DualWield::IsEnabled(%clientId))
        return;
    
    // CRITICAL: Block off-hand attacks during skill upgrade RefreshAll to prevent attack spam exploit
    // This function bypasses MeleeAttack's anti-spam timer, so we need our own check here
    if($SkillUpgradeRefreshScheduled[%clientId] == "true")
        return;
    
    // Get fresh player object from clientId (the passed %player may be stale from schedule string)
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return;
    
    // Get the off-hand weapon being used
    %offHandWeapon = DualWield::GetOffHandWeapon(%clientId);
    if(%offHandWeapon == "" || %offHandWeapon == -1)
        %offHandWeapon = %weaponType;  // Fallback to passed weapon

    // BALANCE FIX: pace off-hand swings by the OFF-HAND weapon's own delay.
    // Previously the off-hand fired once per PRIMARY swing with no own cooldown,
    // so fast-main + heavy-offhand swung the heavy weapon at the fast weapon's
    // rate - the optimal build was always fastest main + hardest off-hand.
    %offDelay = GetDelay(%offHandWeapon);
    if(%offDelay == "" || %offDelay <= 0)
        %offDelay = 0.5;
    %now = getSimTime();
    %lastOff = $DualWield::LastOffHandFire[%clientId];
    if(%lastOff != "" && %lastOff != -1 && (%now - %lastOff) < %offDelay)
        return;
    $DualWield::LastOffHandFire[%clientId] = %now;

    // Deal off-hand damage DIRECTLY (bypass MeleeAttack's anti-spam timer)
    // BUGFIX: use GetRange() (minRange 2 + $WeaponRange) like every other melee
    // path - raw $WeaponRange made the off-hand reach 2 units shorter than the
    // same weapon in the main hand
    %range = GetRange(%offHandWeapon);
    if(%range == "" || %range == -1 || %range <= 0) %range = 4;
    
    // Direct LOS check and damage (same logic as MeleeAttack but without anti-spam)
    $los::object = "";
    if(GameBase::getLOSinfo(%playerObj, %range))
    {
        %obj = getObjectType($los::object);
        if(%obj == "Player")
        {
            // Deal damage with the off-hand weapon
            GameBase::virtual($los::object, "onDamage", $BulletDamageType, 1.0, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, %offHandWeapon);
        }
    }
    
    // Still call PostAttack for any post-attack effects
    PostAttack(%clientId, %offHandWeapon);
}

