//==============================================================================
// BeltWeapons.cs - datablock-less weapons (belt/backpack storage + shell mount)
//==============================================================================
// WHY: the engine caps ItemData at 256 types (Player::MaxItemTypes, and the
// stock client both stores that table fixed-size AND speaks 8-bit item ids on
// the wire - see TribesSource FearPlayerPSC.cpp ItemTypeBits). New weapons as
// datablocks would overflow VANILLA clients at connect. Belt weapons instead
// live entirely server-side in Belt storage (script string lists, no datablock)
// and borrow an existing weapon datablock as their mounted "SHELL":
//
//   - the shell provides the in-hand MODEL and SWING ANIMATION/SPEED
//   - all combat identity (damage roll, skill type, effects, name) comes from
//     the same name-keyed script tables real weapons use ($AccessoryVar,
//     $SkillType, $WeaponEffect...), registered here for the belt name
//   - MeleeAttack (weapons.cs) swaps the shell for the equipped belt weapon
//     before the damage call, so Player::onDamage sees the belt weapon name
//
// Vanilla clients need no download: they see the shell's model, and all
// names/stats reach them as server-composed text.
//
// PHANTOM MOUNT RULE: equipping mounts the shell WITHOUT giving an inventory
// count - the engine mounts by type with no count requirement (TribesSource
// playerInventory.cpp mountItem), while sell/drop/bank/smith/death-drop all
// require count > 0. So the shell in hand is untransferable by construction:
// nothing to farm, sell, or dupe. Unequip/death just unmount + clear state
// (see BeltWeapon::OnDeath call in Player::onKilled).
//==============================================================================

//------------------------------------------------------------------------------
// Registration. %shell = existing weapon datablock whose model/swing this
// weapon borrows (same weapon class: sword shell for a sword, etc).
// %damage = flat damage value (same scale as the shells' $SpecialVar word 1).
//------------------------------------------------------------------------------
function BeltWeapon::Register(%item, %displayName, %shell, %damage, %skillReq, %weight, %cost, %miscInfo)
{
	if($BeltWeapon[%item, "Shell"] != "")
	{
		echo("WARNING: BeltWeapon::Register - '" @ %item @ "' registered twice, ignoring second registration");
		return;
	}
	if($AccessoryVar[%shell, $AccessoryType] == "")
	{
		echo("ERROR: BeltWeapon::Register - shell '" @ %shell @ "' for '" @ %item @ "' is not a registered weapon");
		return;
	}

	$BeltWeapon[%item, "Shell"] = %shell;
	$BeltWeapon[%item, "SkillReq"] = %skillReq;

	// Belt storage (this also sets $AccessoryVar[$Weight] and $HardcodedItemCost)
	BeltItem::Add(%displayName, %item, "Weapons", %weight, %cost);

	// Combat identity - the exact tables the damage path reads for real weapons
	$AccessoryVar[%item, $AccessoryType] = $AccessoryVar[%shell, $AccessoryType];
	$AccessoryVar[%item, $SpecialVar] = "6 " @ %damage;
	$AccessoryVar[%item, $MiscInfo] = %miscInfo;
	$SkillType[%item] = $SkillType[%shell];
	$SkillRestriction[%item] = $SkillType[%shell] @ " " @ %skillReq;
	$WeaponDelay[%item] = GetDelay(%shell);		// swing speed is baked into the shell's fire anim
	$WeaponRange[%item] = GetRange(%shell) - 2.0;	// GetRange() re-adds the 2.0 base
	if($WeaponShape[%shell] != "")
		$WeaponShape[%item] = $WeaponShape[%shell];	// dual-wield visual table
	$ItemCost[%item] = %cost;
}

//------------------------------------------------------------------------------
// Belt ARMOR registration - zero datablocks INCLUDING visuals. Armor in this
// engine is a pure skin swap (UpdateAppearance -> $ArmorSkin -> Client::setSkin,
// rpgfunk.cs), so a belt armor is just: belt storage entry + skin name + stats.
// Equip/unequip/stat handling is the existing Belt::EquipArmor path (stats
// applied via Belt::ApplyAccessoryStats from $SpecialVar), and UpdateAppearance
// overrides the engine-armor skin with $ArmorSkin[<equipped belt armor>].
//
// %skin  = skin base name (e.g. "rpgscalemail") - must exist in the skin VOLs
// %stats = raw $SpecialVar stat string: "statIndex value ..." pairs, indexes:
//          3=MDEF 4=HP 5=Mana 6=ATK 7=DEF 10=HPregen 11=Manaregen
//          (e.g. "7 25 4 100" = +25 DEF, +100 HP)
//------------------------------------------------------------------------------
function BeltArmor::Register(%item, %displayName, %skin, %stats, %weight, %cost, %miscInfo)
{
	if($BeltItem[%item, "Item"] != "")
	{
		echo("WARNING: BeltArmor::Register - '" @ %item @ "' registered twice, ignoring second registration");
		return;
	}

	BeltItem::Add(%displayName, %item, "Armor", %weight, %cost);
	$ArmorSkin[%item] = %skin;
	$AccessoryVar[%item, $SpecialVar] = %stats;
	$AccessoryVar[%item, $MiscInfo] = %miscInfo;
	$ItemCost[%item] = %cost;
}

//------------------------------------------------------------------------------
// Belt ACCESSORY registration - one line per accessory, zero or one datablocks.
// Equip/unequip/stat/slot-limit handling is the existing Belt::EquipAccessory
// path (stats via Belt::ApplyAccessoryStats, max slots via $maxAccessory).
//
// %accessoryType = $ShieldAccessoryType / $RingAccessoryType / ... constant
// %stats         = raw $SpecialVar stat string (see BeltArmor::Register)
// %visualItem    = "" for stat-only accessories (rings etc), or the name of a
//                  visual ItemData (with imageType) for accessories that show
//                  on the player. Shield-type visuals are phantom-mounted on
//                  image slot 2 by UpdateAppearance, exactly like engine
//                  shields - the visual ItemData is never placed in anyone's
//                  inventory, so it costs its 1 slot but can never leak.
//------------------------------------------------------------------------------
function BeltAccessory::Register(%item, %displayName, %accessoryType, %stats, %weight, %cost, %miscInfo, %visualItem)
{
	if($BeltItem[%item, "Item"] != "")
	{
		echo("WARNING: BeltAccessory::Register - '" @ %item @ "' registered twice, ignoring second registration");
		return;
	}

	BeltItem::Add(%displayName, %item, "Accessories", %weight, %cost);
	$AccessoryVar[%item, $AccessoryType] = %accessoryType;
	$AccessoryVar[%item, $SpecialVar] = %stats;
	$AccessoryVar[%item, $MiscInfo] = %miscInfo;
	$ItemCost[%item] = %cost;
	if(%visualItem != "")
		$BeltAccessoryVisual[%item] = %visualItem;
}

//------------------------------------------------------------------------------
// Equip: validate ownership + skill, loan/mount the shell, bind the identity.
//------------------------------------------------------------------------------
function BeltWeapon::Equip(%clientId, %item)
{
	// accept display-name alias
	if($BeltItem[%item, "Item"] != "")
		%item = $BeltItem[%item, "Item"];

	if($BeltWeapon[%item, "Shell"] == "")
	{
		Client::sendMessage(%clientId, $MsgRed, "That is not an equippable backpack weapon.");
		return;
	}
	if(!Belt::HasThisStuff(%clientId, %item))
	{
		Client::sendMessage(%clientId, $MsgRed, "You don't have a " @ $BeltItem[%item, "Name"] @ " in your backpack.");
		return;
	}
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		Client::sendMessage(%clientId, $MsgRed, "You must be in-game to equip weapons.");
		return;
	}

	// same gate real weapons enforce via $SkillRestriction
	%req = $BeltWeapon[%item, "SkillReq"];
	%skillType = $SkillType[%item];
	if(%req > 0 && $PlayerSkill[%clientId, %skillType] < %req)
	{
		Client::sendMessage(%clientId, $MsgRed, "You need " @ %req @ " " @ $SkillDesc[%skillType] @ " skill to wield " @ $BeltItem[%item, "Name"] @ ".");
		return;
	}

	// swap out any previous belt weapon
	if(fetchData(%clientId, "EquippedBeltWeapon") != "")
		BeltWeapon::Unequip(%clientId, true);

	// Mount the shell as a PHANTOM: the engine mounts by type WITHOUT
	// requiring inventory count (TribesSource playerInventory.cpp mountItem),
	// while every transfer path (sell/drop/bank/smith/death-drop) requires
	// count > 0. So a count-0 mount can never be sold, dropped, banked, or
	// lootbagged - no loan, no reclaim, no dupe surface. If the player DOES
	// own real copies of the shell, the mount is an ordinary one and
	// transferring those is legitimately their business (GuardShellTransfer
	// keeps the belt equip state in sync when the last one goes).
	%shell = $BeltWeapon[%item, "Shell"];
	storeData(%clientId, "BeltWeaponShellLoaned", "");	// legacy loan flag, no longer used
	storeData(%clientId, "EquippedBeltWeapon", %item);
	Player::mountItem(%playerObj, %shell, $WeaponSlot);

	Client::sendMessage(%clientId, $MsgGreen, "You equipped " @ $BeltItem[%item, "Name"] @ ".");
	echo("[BELT WEAPON] " @ Client::getName(%clientId) @ " equipped " @ %item @ " (shell " @ %shell @ ")");

	VSlot::Sync(%clientId);	// Phase D native inventory (no-op unless $pref::VSlotsEnabled)
}

//------------------------------------------------------------------------------
// Unequip: unbind identity and unmount the phantom shell. If the player owns
// REAL copies of the shell (count > 0) the mount is left alone - it's an
// ordinary weapon in that case and unmounting it would be wrong.
//------------------------------------------------------------------------------
function BeltWeapon::Unequip(%clientId, %silent)
{
	%item = fetchData(%clientId, "EquippedBeltWeapon");
	if(%item == "")
	{
		if(!%silent)
			Client::sendMessage(%clientId, $MsgRed, "You have no backpack weapon equipped.");
		return;
	}

	storeData(%clientId, "EquippedBeltWeapon", "");
	storeData(%clientId, "BeltWeaponShellLoaned", "");	// legacy loan flag, no longer used

	%shell = $BeltWeapon[%item, "Shell"];
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != -1 && %playerObj != "")
	{
		if(Player::getMountedItem(%clientId, $WeaponSlot) == %shell
		&& SafeGetItemCount(%clientId, %shell, "BeltWeaponUnequip") <= 0)
			Player::unMountItem(%clientId, $WeaponSlot);
	}

	if(!%silent)
		Client::sendMessage(%clientId, $MsgYellow, "You unequipped " @ $BeltItem[%item, "Name"] @ ".");
	echo("[BELT WEAPON] " @ Client::getName(%clientId) @ " unequipped " @ %item);
}

//------------------------------------------------------------------------------
// Death hook (called EARLY in Player::onKilled, before drop assembly): clear
// the equip binding. The phantom shell has count 0 so the drop assembly never
// sees it. No-op for bots and players with nothing equipped.
//------------------------------------------------------------------------------
function BeltWeapon::OnDeath(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return;
	if(fetchData(%clientId, "EquippedBeltWeapon") == "")
		return;
	BeltWeapon::Unequip(%clientId, true);
	Client::sendMessage(%clientId, $MsgYellow, "Your backpack weapon returns to your backpack.");
}

//------------------------------------------------------------------------------
// Shell-transfer sync. The phantom mount itself can't be transferred (count 0
// blocks every path), so this is NOT an exploit guard anymore - it keeps the
// belt equip STATE honest when the player transfers real copies of the same
// ItemData their equipped belt weapon uses as its shell: selling/dropping the
// last real copy triggers the engine's auto-unmount (setItemCount(0) unmounts,
// playerInventory.cpp:206), which would leave EquippedBeltWeapon pointing at
// an unmounted shell. Called before the transfer at sellItem entry,
// remoteDropItem, bank deposit, and smith combos. Returns true if it unequipped.
//------------------------------------------------------------------------------
function BeltWeapon::GuardShellTransfer(%clientId, %item)
{
	if(%clientId == "" || %clientId == -1)
		return false;
	%equipped = fetchData(%clientId, "EquippedBeltWeapon");
	if(%equipped == "" || %equipped == "0")
		return false;
	if($BeltWeapon[%equipped, "Shell"] != %item)
		return false;
	BeltWeapon::Unequip(%clientId, true);
	Client::sendMessage(%clientId, $MsgYellow, "Your backpack weapon returns to your backpack.");
	return true;
}

//------------------------------------------------------------------------------
// #weapon chat command (vanilla-client path; KronosHUD can call the same
// functions directly). Dispatched from comchat.cs.
//------------------------------------------------------------------------------
function BeltWeapon::Command(%clientId, %sub, %rest)
{
	if(%sub == "equip" && %rest != "")
		BeltWeapon::Equip(%clientId, %rest);
	else if(%sub == "remove" || %sub == "unequip")
		BeltWeapon::Unequip(%clientId, false);
	else if(%sub == "list")
	{
		%n = 0;
		%nf = Belt::GetNS(%clientId, "Weapons");
		%ns = GetWord(%nf, 0);
		for(%i = 1; %i <= %ns; %i++)
		{
			%it = GetWord(%nf, %i);
			%amnt = Belt::HasThisStuff(%clientId, %it);
			%eq = "";
			if(fetchData(%clientId, "EquippedBeltWeapon") == %it)
				%eq = " [EQUIPPED]";
			Client::sendMessage(%clientId, $MsgWhite, %amnt @ "x " @ $BeltItem[%it, "Name"] @ " - ATK " @ GetWord($AccessoryVar[%it, $SpecialVar], 1) @ %eq);
			%n++;
		}
		if(%n == 0)
			Client::sendMessage(%clientId, $MsgWhite, "No weapons in your backpack.");
	}
	else
		Client::sendMessage(%clientId, $MsgWhite, "Usage: #weapon list | #weapon equip <name> | #weapon remove");
}

//==============================================================================
// Registrations
//==============================================================================
// BeltWeapon::Register(name, "Display Name", ShellWeapon, damage, skillReq, weight, cost, "description");
// BeltArmor::Register(name, "Display Name", "skinbase", "statPairs", weight, cost, "description");
// BeltAccessory::Register(name, "Display Name", type, "statPairs", weight, cost, "description", VisualItem);
// TEST ITEMS - remove once the system is verified in-game:
BeltWeapon::Register(TestBeltBlade, "Test Belt Blade", SteelBroadSword, 40, 0, 5.0, 1000, "A conjured blade that lives in your backpack");
BeltArmor::Register(TestBeltMail, "Test Belt Mail", "rpgscalemail", "7 25 4 100", 10.0, 1000, "Conjured scale mail that lives in your backpack");

// KOKBATCH shields (converted from Accessory.cs ItemData pairs - the X0
// 'Equipped' tokens were dropped, freeing 9 of the 256 ItemData slots; each
// X ItemData remains ONLY as the slot-2 mounted visual).
BeltAccessory::Register(CathedralAegis, "Cathedral Aegis",   $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Cathedral Aegis",   CathedralAegis);
BeltAccessory::Register(WardenBulwark,  "Warden's Bulwark",  $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Warden's Bulwark",  WardenBulwark);
BeltAccessory::Register(CrimsonRose,    "Crimson Rose",      $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Crimson Rose",      CrimsonRose);
BeltAccessory::Register(MarauderGuard,  "Marauder's Guard",  $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Marauder's Guard",  MarauderGuard);
BeltAccessory::Register(TowerAegis,     "Tower Aegis",       $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Tower Aegis",       TowerAegis);
BeltAccessory::Register(KrakenEye,      "Kraken Eye",        $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Kraken Eye",        KrakenEye);
BeltAccessory::Register(DragonEye,      "Dragon Eye",        $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Dragon Eye",        DragonEye);
BeltAccessory::Register(EyeOfHatred,    "Eye of Hatred",     $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Eye of Hatred",     EyeOfHatred);
BeltAccessory::Register(StarlightEye,   "Starlight Eye",     $ShieldAccessoryType, "7 320 4 170", 80, 50000000, "Starlight Eye",     StarlightEye);
