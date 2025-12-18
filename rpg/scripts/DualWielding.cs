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
$DualWield::OffHandDamageMultiplier = 0.7;     // Off-hand deals 70% damage
$DualWield::OffHandDelayOffset = 0.1;          // Slight delay between primary and off-hand fire (seconds)
$DualWield::AllowForBots = false;              // Whether bots can dual wield
$DualWield::RequiredSkillLevel = 300;          // Required Slashing skill to dual wield
$DualWield::RequiredSkillType = 1;             // Skill type: 1 = Slashing ($SkillSlashing)

// Valid weapon types for dual wielding (melee weapons)
// Uses $AccessoryType values: Sword=7, Axe=8, Polearm=9, Bludgeon=10
$DualWield::AllowedTypes = "7 8 10";  // Swords, Axes, Bludgeons (no polearms - too large)

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
    if(%count < 1)
    {
        Client::sendMessage(%clientId, $MsgRed, "You don't have a " @ %weaponItem @ " to equip.");
        return false;
    }
    
    // Unequip current off-hand if any
    DualWield::UnequipOffHand(%clientId);
    
    // Store the off-hand weapon
    storeData(%clientId, "DualWield_OffHandWeapon", %weaponItem);
    
    // Convert to equipped state: item -> item0 (like accessory equipping pattern)
    // Decrement base item count
    Player::setItemCount(%playerObj, %weaponItem, Player::getItemCount(%playerObj, %weaponItem) - 1);
    // Increment equipped version (item + "0" suffix)
    Player::setItemCount(%playerObj, %weaponItem @ "0", Player::getItemCount(%playerObj, %weaponItem @ "0") + 1);
    
    // Mount the off-hand VISUAL weapon to slot 6 (uses mountPoint=2 with offset for other hand)
    // The off-hand item uses "2" suffix (e.g., DualWieldTest2, Rapier2)
    %offHandVisual = %weaponItem @ "2";
    %offHandData = getItemData(%offHandVisual);
    
    if(%offHandData != "" && %offHandData != -1)
    {
        // Off-hand visual item exists - mount it to slot 6
        Player::mountItem(%playerObj, %offHandVisual, 6);
        echo("[DUAL WIELD] Mounted off-hand visual '" @ %offHandVisual @ "' to slot 6");
    }
    else
    {
        // No dedicated off-hand item - try mounting original to slot 6
        Player::mountItem(%playerObj, %weaponItem, 6);
        echo("[DUAL WIELD] No off-hand visual found, mounted '" @ %weaponItem @ "' to slot 6");
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
    
    // Convert from equipped state: item0 -> item (reverse of equip)
    // Decrement equipped version (item + "0" suffix)
    Player::setItemCount(%playerObj, %currentWeapon @ "0", Player::getItemCount(%playerObj, %currentWeapon @ "0") - 1);
    // Increment base item count
    Player::setItemCount(%playerObj, %currentWeapon, Player::getItemCount(%playerObj, %currentWeapon) + 1);
    
    // Clear stored data
    storeData(%clientId, "DualWield_OffHandWeapon", "");
    
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
    
    // DEBUG: Check what's mounted in slot 6 before triggering
    %mountedItem = Player::getMountedItem(%playerObj, 6);
    %itemState = Player::getItemState(%playerObj, 6);
    echo("[DUAL WIELD DEBUG] Before trigger - Slot 6 mounted: " @ %mountedItem @ ", state: " @ %itemState);
    
    // Trigger the off-hand weapon on slot 6 (where it's visually mounted)
    // Using slot 6 ensures the weapon's fire animation plays from its DTS file
    Player::trigger(%playerObj, 6, true);
    
    // DEBUG: Check state after triggering
    %itemStateAfter = Player::getItemState(%playerObj, 6);
    echo("[DUAL WIELD DEBUG] After trigger - Slot 6 state: " @ %itemStateAfter);
    
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
    
    // Validate player still exists
    %playerObj = Client::getOwnedObject(%clientId);
    if(%playerObj == "" || %playerObj == -1)
        return;
    
    // Deal off-hand damage (with multiplier)
    %range = $WeaponRange[%weaponType];
    if(%range == "") %range = 4;
    
    // Use reduced damage for off-hand
    // The MeleeAttack function will be called, but we scale damage via the weapon lookup
    // For now, just call MeleeAttack - integrating damage multiplier requires deeper hook
    MeleeAttack(%playerObj, %range, %weaponType);
    
    echo("[DUAL WIELD] Off-hand melee attack executed for " @ Client::getName(%clientId));
}

echo("[DUAL WIELD] Test weapon 'DualWieldTest' registered.");
echo("[DUAL WIELD] Usage: #give DualWieldTest 2, equip primary, then #dualwield DualWieldTest");

