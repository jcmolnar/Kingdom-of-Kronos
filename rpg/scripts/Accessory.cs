//When adding a new accessory, follow these steps:
//-(if it has a new accessory type, fill in the stuff here)
//-add the actual itemdata here

//current item method involves having two ItemData's for each item, where one differs from the
//other by category.  One is Accessory, the other is Equipped.

//=========================
//  $SpecialVar list:
//=========================
//1:
//2:
//3: MDEF
//4: HP
//5: Mana
//6: ATK
//7: DEF
//8: Internal armor switching variable
//9:
//10: HP regen
//11: Mana regen	

$SpecialVarDesc[1] = "";
$SpecialVarDesc[2] = "";
$SpecialVarDesc[3] = "MDEF (Magical)";
$SpecialVarDesc[4] = "HP";
$SpecialVarDesc[5] = "Mana";
$SpecialVarDesc[6] = "ATK";
$SpecialVarDesc[7] = "DEF";
$SpecialVarDesc[8] = "[Internal]";
$SpecialVarDesc[9] = "";
$SpecialVarDesc[10] = "HP regen";
$SpecialVarDesc[11] = "Mana regen";

$RingAccessoryType = 1;
$BodyAccessoryType = 2;
$BootsAccessoryType = 3;
$BackAccessoryType = 4;
$ShieldAccessoryType = 5;
$TalismanAccessoryType = 6;
$SwordAccessoryType = 7;
$AxeAccessoryType = 8;
$PolearmAccessoryType = 9;
$BludgeonAccessoryType = 10;
$RangedAccessoryType = 11;
$ProjectileAccessoryType = 12;
$HeadAccessoryType = 13;
$BeltAccessoryType = 14;

$LocationDesc[$RingAccessoryType] = "Ring";
$LocationDesc[$BodyAccessoryType] = "Body";
$LocationDesc[$BootsAccessoryType] = "Feet";
$LocationDesc[$BackAccessoryType] = "Back";
$LocationDesc[$ShieldAccessoryType] = "Shield";
$LocationDesc[$TalismanAccessoryType] = "Talisman";
$LocationDesc[$SwordAccessoryType] = "Sword";
$LocationDesc[$AxeAccessoryType] = "Axe";
$LocationDesc[$PolearmAccessoryType] = "Polearm";
$LocationDesc[$BludgeonAccessoryType] = "Bludgeon";
$LocationDesc[$RangedAccessoryType] = "Ranged";
$LocationDesc[$ProjectileAccessoryType] = "Projectile";
$LocationDesc[$HeadAccessoryType] = "Head";
$LocationDesc[$BeltAccessoryType] = "Belt";

$maxAccessory[$RingAccessoryType] = 2;
$maxAccessory[$BodyAccessoryType] = 1;
$maxAccessory[$BootsAccessoryType] = 1;
$maxAccessory[$BackAccessoryType] = 1;
$maxAccessory[$ShieldAccessoryType] = 1;
$maxAccessory[$TalismanAccessoryType] = 1;
$maxAccessory[$HeadAccessoryType] = 1;
$maxAccessory[$BeltAccessoryType] = 1;

//these are used for $AccessoryVar
$AccessoryType = 1;			//(used in item.cs)
$SpecialVar = 2;				//(used in player.cs)
$Weight = 3;				//(used in rpgfunk.cs)
$ShopIndex = 4;
$MiscInfo = 5;

$HardcodedItemCost[BluePotion] = 15;
$HardcodedItemCost[CrystalBluePotion] = 100;
$HardcodedItemCost[EnergyVial] = 15;
$HardcodedItemCost[CrystalEnergyVial] = 100;
//Helmets
$HardcodedItemCost[IronHelmet] = 2000;
$HardcodedItemCost[GoldenHelmet] = 16000;
$HardcodedItemCost[CrystalHelmet] = 200000;
$HardcodedItemCost[DiamondHelmet] = 1300000;
$HardcodedItemCost[BlackDiamondHelmet] = 10000000;
$HardcodedItemCost[RedDiamondHelmet] = 135000000;
$HardcodedItemCost[WhiteDiamondHelmet] = 150000000;
//AntiMagic Belts
$HardcodedItemCost[AntiMagicBelt] = 200000;
$HardcodedItemCost[MajorAntiMagicBelt] = 1500000;
$HardcodedItemCost[ExtremeAntiMagicBelt] = 12000000;
$HardcodedItemCost[GodlyAntiMagicBelt] = 75000000;
$HardcodedItemCost[HeavenlyAntiMagicBelt] = 200000000;

$HardcodedItemCost[Tent] = 4000;
$HardcodedItemCost[ScoutVehicle] = 500000;
$HardcodedItemCost[AdminOrb] = 99999999999999;
$HardcodedItemCost[OrbOfBreath] = 1000;
//boots
$HardcodedItemCost[CheetaursPaws] = 1500;
$HardcodedItemCost[BootsOfGliding] = 8000;
$HardcodedItemCost[WindWalkers] = 45000;
$HardcodedItemCost[WindPaws] = 1000000;
// AdminBoots has no value - it's a rare item
//Power Rings
$HardcodedItemCost[MinorPowerRing] = 800;
$HardcodedItemCost[PowerRing] = 2500;
$HardcodedItemCost[MajorPowerRing] = 20000;
$HardcodedItemCost[ExtremePowerRing] = 300000;
$HardcodedItemCost[GodlyPowerRing] = 2000000;
$HardcodedItemCost[HeavenlyPowerRing] = 10000000;
//Regen Necklaces
$HardcodedItemCost[MinorRegenerationNecklace] = 3000;
$HardcodedItemCost[RegenerationNecklace] = 40000;
$HardcodedItemCost[MajorRegenerationNecklace] = 10000000;
$HardcodedItemCost[ExtremeRegenerationNecklace] = 50000000;
$HardcodedItemCost[GodlyRegenerationNecklace] = 120000000;
$HardcodedItemCost[HeavenlyRegenerationNecklace] = 300000000;

$HardcodedItemCost[DuelCard] = 60000;
$HardcodedItemCost[RubyNecklace] = 300;
$HardcodedItemCost[OgreTooth] = 6000;
$HardcodedItemCost[AncientScroll] = 40000;
$HardcodedItemCost[Crown] = 45000;
$HardcodedItemCost[KronoStone] = 80000;
$HardcodedItemCost[MinotaurHorn] = 100000;
$HardcodedItemCost[AlienSpine] = 150000;
$HardcodedItemCost[AlienEye] = 500000;
$HardcodedItemCost[DemonBreath] = 700000;
$HardcodedItemCost[EnchantedStone] = 2450;
$HardcodedItemCost[SkeletonBone] = 5860;
$HardcodedItemCost[AngelsTear] = 900000;
$HardcodedItemCost[Bible] = 800000;
$HardcodedItemCost[BadgeOfFriendship] = 1;
$HardcodedItemCost[BadgeOfLoyalty] = 1;
$HardcodedItemCost[BadgeOfHonor] = 1;
$HardcodedItemCost[BadgeOfReverence] = 1;

function GenerateAllShieldCosts()
{
	dbecho($dbechoMode, "GenerateAllShieldCosts()");

	$ItemCost[SteelKnightShield] = GenerateItemCost(SteelKnightShield);
	$ItemCost[CrystalKnightShield] = GenerateItemCost(CrystalKnightShield);
	$ItemCost[DiamondKnightShield] = GenerateItemCost(DiamondKnightShield) * 10;
	$ItemCost[BlackDiamondKnightShield] = GenerateItemCost(BlackDiamondKnightShield) * 2;
	$ItemCost[RedDiamondKingShield] = 1350000000;
	$ItemCost[WhiteDiamondKingShield] = 1500000000;
}

//=====================
// ACCESSORY FUNCTIONS
//=====================

function GetAccessoryVar(%item, %type)
{
	dbecho($dbechoMode, "GetAccessoryVar(" @ %item @ ", " @ %type @ ")");

	%nitem = getCroppedItem(%item);
	
	// CRITICAL: Check if this is a seal battle bot's weapon with scaled damage
	// If $SpecialVar is requested and we have a scaled damage value for this weapon, return it
	if(%type == $SpecialVar)
	{
		// Use reverse lookup to quickly find which seal battle bot owns this weapon
		%sealBotId = $SealBattleWeaponOwner[%nitem];
		if(%sealBotId != "" && %sealBotId != -1)
		{
			// Verify this bot still exists and is a seal battle bot
			%isSealBattleBot = fetchData(%sealBotId, "SealBattleBot");
			if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
			{
				// Verify this bot still has the weapon equipped
				%mountedWeapon = Player::getMountedItem(%sealBotId, $WeaponSlot);
				if(%mountedWeapon != -1 && %mountedWeapon != "")
				{
					%mountedWeaponName = getCroppedItem(%mountedWeapon);
					if(%mountedWeaponName == %nitem)
					{
						// This weapon belongs to a seal battle bot - return scaled damage
						%scaledDamage = $SealBattleWeaponDamage[%sealBotId, %nitem];
						if(%scaledDamage != "" && %scaledDamage != -1)
						{
							// Return the scaled damage for this seal battle bot's weapon
							return %scaledDamage;
						}
					}
					else
					{
						// Bot no longer has this weapon - clear the lookup
						$SealBattleWeaponOwner[%nitem] = "";
					}
				}
				else
				{
					// Bot no longer has a weapon - clear the lookup
					$SealBattleWeaponOwner[%nitem] = "";
				}
			}
			else
			{
				// Bot is no longer a seal battle bot - clear the lookup
				$SealBattleWeaponOwner[%nitem] = "";
			}
		}
	}

	return $AccessoryVar[%nitem, %type];
}

function getCroppedItem(%item)
{
	dbecho($dbechoMode, "getCroppedItem(" @ %item @ ")");

	%zitem = %item @ "xx";
	%p = String::findSubStr(%zitem, "0xx");
	if(%p != -1)
		%nitem = String::getSubStr(%item, 0, %p);
	else
		%nitem = %item;

	return %nitem;
}

function GetAccessoryList(%clientId, %type, %filter)
{
	dbecho($dbechoMode, "GetAccessoryList(" @ %clientId @ ", " @ %type @ ", " @ %filter @ ")");

	if(IsDead(%clientId) || !fetchData(%clientId, "HasLoadedAndSpawned") || %clientId.IsInvalid || %clientId.choosingGroup || %clientId.choosingClass)
		return "";

	// CRITICAL: Validate player object exists before calling Player::getItemCount()
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return "";

	%list = "";
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		// Re-validate player object in loop in case it gets despawned mid-execution
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == "" || %playerObj == -1)
			break; // Exit loop if player object no longer exists
		
		%count = SafeGetItemCount(%clientId, %i, "GetAccessoryList");

		if(%count)
		{
			%item = getItemData(%i);

			%flag = False;
			if(%type == 1)
			{
				if(%item.className == "Accessory")
					%flag = True;
			}
			else if(%type == 2)
			{
				if(%item.className == "Equipped")
					%flag = True;
			}
			else if(%type == 3)
			{
				if(%item.className == "Accessory" || %item.className == "Equipped")
					%flag = True;
			}
			else if(%type == 4)
			{
				if(%item.className == "Equipped" || %item.className == "Weapon" || %item.className == "Belt")
				{
					if(%item.className == "Weapon")
					{
						if(Player::getMountedItem(%clientId, $WeaponSlot) == %item)
							%flag = True;
					}
					else if(%item.className == "Belt")
					{
						if(Player::getMountedItem(%clientId, $BeltSlot) == %item)
							%flag = True;
					}
					else
						%flag = True;
				}
			}
			else if(%type == 5)
			{
				if($AccessoryVar[%item, $AccessoryType] == $SwordAccessoryType)
					%flag = True;
			}
			else if(%type == 6)
			{
				if($AccessoryVar[%item, $AccessoryType] == $AxeAccessoryType)
					%flag = True;
			}
			else if(%type == 7)
			{
				if($AccessoryVar[%item, $AccessoryType] == $PolearmAccessoryType)
					%flag = True;
			}
			else if(%type == 8)
			{
				if($AccessoryVar[%item, $AccessoryType] == $BludgeonAccessoryType)
					%flag = True;
			}
			else if(%type == 9)
			{
				if($AccessoryVar[%item, $AccessoryType] == $RangedAccessoryType)
					%flag = True;
			}
			else if(%type == 10)
			{
				if($AccessoryVar[%item, $AccessoryType] == $ProjectileAccessoryType)
					%flag = True;
			}
			else if(%type == -1)
				%flag = True;

			if(%flag)
			{
				%flag2 = False;
				if(%filter != -1)
				{
					%av = GetAccessoryVar(%item, $SpecialVar);
					for(%j = 0; GetWord(%av, %j) != -1; %j+=2)
					{
						%w = GetWord(%av, %j);
						if(String::findSubStr(%filter, %w) != -1)
							%flag2 = True;
					}
				}
				else
				{
					// If filter is -1, flag2 should be True (no filter means all match)
					%flag2 = True;
				}
				if(%filter == -1 || %flag2)
					%list = %list @ %item @ " ";
			}
		}
	}
	return %list;
}

function AddPoints(%clientId, %char)
{
	dbecho($dbechoMode, "AddPoints(" @ %clientId @ ", " @ %char @ ")");

	// CRITICAL: Validate player object exists before proceeding
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		// Player object doesn't exist (player/bot was deleted)
		return 0;
	}

	%add = 0;
	
	// Add Belt system stat bonuses based on stat type
	// Stat indices: 3=MDEF, 4=HP, 5=Mana, 6=ATK, 7=DEF, 10=HP regen, 11=Mana regen
	if(String::findSubStr(%char, "3") != -1)
	{
		%beltBonus = fetchData(%clientId, "BeltMDEFBonus");
		if(%beltBonus != "" && %beltBonus != "0" && %beltBonus != -1)
			%add += %beltBonus;
	}
	if(String::findSubStr(%char, "4") != -1)
	{
		%beltBonus = fetchData(%clientId, "BeltHPBonus");
		if(%beltBonus != "" && %beltBonus != "0" && %beltBonus != -1)
			%add += %beltBonus;
	}
	if(String::findSubStr(%char, "5") != -1)
	{
		%beltBonus = fetchData(%clientId, "BeltManaBonus");
		if(%beltBonus != "" && %beltBonus != "0" && %beltBonus != -1)
			%add += %beltBonus;
	}
	if(String::findSubStr(%char, "6") != -1)
	{
		%beltBonus = fetchData(%clientId, "BeltATKBonus");
		if(%beltBonus != "" && %beltBonus != "0" && %beltBonus != -1)
			%add += %beltBonus;
	}
	if(String::findSubStr(%char, "7") != -1)
	{
		%beltBonus = fetchData(%clientId, "BeltDEFBonus");
		if(%beltBonus != "" && %beltBonus != "0" && %beltBonus != -1)
			%add += %beltBonus;
	}
	if(String::findSubStr(%char, "10") != -1)
	{
		%beltBonus = fetchData(%clientId, "BeltHPRegenBonus");
		if(%beltBonus != "" && %beltBonus != "0" && %beltBonus != -1)
			%add += %beltBonus;
	}
	if(String::findSubStr(%char, "11") != -1)
	{
		%beltBonus = fetchData(%clientId, "BeltManaRegenBonus");
		if(%beltBonus != "" && %beltBonus != "0" && %beltBonus != -1)
			%add += %beltBonus;
	}
	
	// Initialize %list to empty string before calling GetAccessoryList
	%list = "";
	%list = GetAccessoryList(%clientId, 4, %char);
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		// CRITICAL: Re-validate player object before each iteration
		%playerCheck = Client::getOwnedObject(%clientId);
		if(%playerCheck == -1 || %playerCheck == "")
		{
			// Player object was deleted during loop
			break;
		}
		
		%w = GetWord(%list, %i);
		
		// CRITICAL: GetAccessoryList returns item objects converted to strings (item names)
		// We need to check if this is a Weapon or Belt to verify it's mounted
		// For accessories, we can use the string name directly
		
		// Try to find the ItemData object for className check (only needed for weapons/belts)
		%itemObj = -1;
		%maxItems = getNumItems();
		for(%j = 0; %j < %maxItems; %j++)
		{
			%checkItem = getItemData(%j);
			// Compare item names - convert ItemData object to string for comparison
			%checkItemName = %checkItem @ "";
			if(%checkItemName == %w)
			{
				%itemObj = %checkItem;
				break;
			}
		}

		%slot = "";
		%count = 0;
		
		// Only check className if we found the item object (for weapons/belts)
		if(%itemObj != -1 && %itemObj != "")
		{
			if(%itemObj.className == Weapon)
				%slot = $WeaponSlot;
			else if(%itemObj.className == Belt)
				%slot = $BeltSlot;
		}

		if(%slot != "")
		{
			// For weapons/belts, check if mounted
			if(%itemObj != -1 && %itemObj != "")
			{
				if(Player::getMountedItem(%clientId, %slot) == %itemObj)
					%count = 1;
				else
					%count = 0;
			}
		}
		else
		{
			// For accessories, use item count directly (string name works for getItemCount)
			// CRITICAL: Re-validate player object before calling Player::getItemCount
			%playerCheck2 = Client::getOwnedObject(%clientId);
			if(%playerCheck2 == -1 || %playerCheck2 == "")
			{
				// Player object was deleted
				break;
			}
			// Use the item name string for getItemCount (it accepts both object and name)
			%count = Player::getItemCount(%clientId, %w);
		}

		%tmp = GetAccessoryVar(%w, $SpecialVar);

		for(%j = 0; GetWord(%tmp, %j) != -1; %j+=2)
		{
			%e = GetWord(%tmp, %j);
			if(String::findSubStr(%char, %e) != -1)
				%add += GetWord(%tmp, %j+1) * %count;
		}
	}

	return %add;
}

function AddItemSpecificPoints(%item, %char)
{
	dbecho($dbechoMode, "AddItemSpecificPoints(" @ %item @ ", " @ %char @ ")");

	%tmp = GetAccessoryVar(%item, $SpecialVar);

	for(%j = 0; GetWord(%tmp, %j) != -1; %j+=2)
	{
		%e = GetWord(%tmp, %j);
		if(%e == %char)
		{
			%info = GetWord(%tmp, %j+1);
			break;
		}
	}

	return %info;
}

function WhatSpecialVars(%thing)
{
	dbecho($dbechoMode, "WhatSpecialVars(" @ %thing @ ")");

	%tmp = GetAccessoryVar(%thing, $SpecialVar);

	%t = "";
	for(%i = 0; GetWord(%tmp, %i) != -1; %i+=2)
	{
		%s = GetWord(%tmp, %i);
		%n = GetWord(%tmp, %i+1);

		%t = %t @ $SpecialVarDesc[%s] @ ": " @ %n @ ", ";
	}
	if(%t == "")
		%t = "None";
	else
		%t = String::getSubStr(%t, 0, String::len(%t)-2);
	
	return %t;
}

function NullItemList(%clientId, %type, %msgcolor, %msg)
{
	dbecho($dbechoMode, "NullItemList(" @ %clientId @ ", " @ %type @ ", " @ %msgcolor @ ", " @ %msg @ ")");

	for(%z = 1; $ItemList[%type, %z] != ""; %z++)
	{
		%item = $ItemList[%type, %z];

		if(isBackpackItem(%item))
		{
			%amnt = Belt::HasThisStuff(%clientid,%item);
			if(%amnt > 0)
			{
				%item = $BeltItem[%item, "Item"];
				if(%item == "") %item = %item; // If no registered name, use original
				%name = $BeltItem[%item, "Name"];
				if(%name == "") %name = $AccessoryVar[%item, $Name];
				%newmsg = nsprintf(%msg, %name);
				Client::sendMessage(%clientId, %msgcolor, %newmsg);
				Belt::TakeThisStuff(%clientid,%item,%amnt);
			}
		}
		if(SafeGetItemCount(%clientId, %item, "Accessory::RemoveAccessory"))
		{
			Player::setItemCount(%clientId, %item, 0);

			%newmsg = nsprintf(%msg, %item.description);
			Client::sendMessage(%clientId, %msgcolor, %newmsg);
		}
	}
}

function GetCurrentlyWearingArmor(%clientId)
{
	dbecho($dbechoMode, "GetCurrentlyWearingArmor(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before proceeding
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return "";

	//the $ArmorList is present only for this function so far, in order to speed things up and not have to cycle thru
	//each and every item in the game
	for(%i = 1; $ArmorList[%i] != ""; %i++)
	{
		// Re-validate player object in loop in case it gets despawned mid-execution
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == "" || %playerObj == -1)
			break; // Exit loop if player object no longer exists
		
		if(SafeGetItemCount(%clientId, $ArmorList[%i] @ "0", "GetCurrentlyWearingArmor"))
			return $ArmorList[%i];
	}
	return "";
}

//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
//   POTIONS
//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

$AccessoryVar[BluePotion, $Weight] = 4;
$AccessoryVar[BluePotion, $MiscInfo] = "A blue potion that heals 15 HP";
// ItemData BluePotion - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Blue Potion";
//	shapeFile = "armorKit";
//	heading = "eMiscellany";
//	className = "Accessory";
//	shadowDetailMask = 4;
//	price = 0;
//};
function BluePotion::onUse(%player,%item)
{
	%clientId = GetClientIdFromPlayerObject(%player);
	if(%clientId == -1 || %clientId == "")
		return; // Invalid player object

	// Check if this is a belt item - use Belt::TakeThisStuff instead of Player::decItemCount
	if(isBeltItem(%item))
	{
		Belt::TakeThisStuff(%clientId, %item, 1);
	}
	else
	{
		// Legacy: remove from standard inventory
		Player::decItemCount(%player,%item);
	}
	
	%hp = fetchData(%clientId, "HP");
	refreshHP(%clientId, -0.15);
	refreshAll(%clientId);

	if(fetchData(%clientId, "HP") != %hp)
		UseSkill(%clientId, $SkillHealing, True, True);
}

$AccessoryVar[CrystalBluePotion, $Weight] = 10;
$AccessoryVar[CrystalBluePotion, $MiscInfo] = "A crystal blue potion that heals 60 HP";
// ItemData CrystalBluePotion - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Crystal Blue Potion";
//	shapeFile = "armorKit";
//	heading = "eMiscellany";
//	className = "Accessory";
//	shadowDetailMask = 4;
//	price = 0;
//};
function CrystalBluePotion::onUse(%player,%item)
{
	%clientId = GetClientIdFromPlayerObject(%player);
	if(%clientId == -1 || %clientId == "")
		return; // Invalid player object

	// Check if this is a belt item - use Belt::TakeThisStuff instead of Player::decItemCount
	if(isBeltItem(%item))
	{
		Belt::TakeThisStuff(%clientId, %item, 1);
	}
	else
	{
		// Legacy: remove from standard inventory
		Player::decItemCount(%player,%item);
	}
	
	%hp = fetchData(%clientId, "HP");
	refreshHP(%clientId, -0.6);
	refreshAll(%clientId);

	if(fetchData(%clientId, "HP") != %hp)
		UseSkill(%clientId, $SkillHealing, True, True);
}

$AccessoryVar[EnergyVial, $Weight] = 2;
$AccessoryVar[EnergyVial, $MiscInfo] = "An energy vial that provides 16 MP";
// ItemData EnergyVial - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Energy Vial";
//	shapeFile = "armorKit";
//	heading = "eMiscellany";
//	className = "Accessory";
//	shadowDetailMask = 4;
//	price = 0;
//};
function EnergyVial::onUse(%player,%item)
{
	%clientId = GetClientIdFromPlayerObject(%player);
	if(%clientId == -1 || %clientId == "")
		return; // Invalid player object

	// Check if this is a belt item - use Belt::TakeThisStuff instead of Player::decItemCount
	if(isBeltItem(%item))
	{
		Belt::TakeThisStuff(%clientId, %item, 1);
	}
	else
	{
		// Legacy: remove from standard inventory
		Player::decItemCount(%player,%item);
	}
	
	refreshMANA(%clientId, -16);
	refreshAll(%clientId);
}

$AccessoryVar[CrystalEnergyVial, $Weight] = 5;
$AccessoryVar[CrystalEnergyVial, $MiscInfo] = "A crystal energy vial that provides 50 MP";
// ItemData CrystalEnergyVial - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Crystal Energy Vial";
//	shapeFile = "armorKit";
//	heading = "eMiscellany";
//	className = "Accessory";
//	shadowDetailMask = 4;
//	price = 0;
//};
function CrystalEnergyVial::onUse(%player,%item)
{
	%clientId = GetClientIdFromPlayerObject(%player);
	if(%clientId == -1 || %clientId == "")
		return; // Invalid player object

	// Check if this is a belt item - use Belt::TakeThisStuff instead of Player::decItemCount
	if(isBeltItem(%item))
	{
		Belt::TakeThisStuff(%clientId, %item, 1);
	}
	else
	{
		// Legacy: remove from standard inventory
		Player::decItemCount(%player,%item);
	}
	
	refreshMANA(%clientId, -50);
	refreshAll(%clientId);
}

//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
//   RINGS
//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

$AccessoryVar[MinorPowerRing, $AccessoryType] = $RingAccessoryType;
$AccessoryVar[MinorPowerRing, $SpecialVar] = "4 7 7 30";
$AccessoryVar[MinorPowerRing, $Weight] = 2;
$AccessoryVar[MinorPowerRing, $MiscInfo] = "This ring slightly increases hp and defense";

ItemData MinorPowerRing
{
	description = "Minor Power Ring";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData MinorPowerRing0
{
	description = "Minor Power Ring";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[PowerRing, $AccessoryType] = $RingAccessoryType;
$AccessoryVar[PowerRing, $SpecialVar] = "4 17 7 55";
$AccessoryVar[PowerRing, $Weight] = 5;
$AccessoryVar[PowerRing, $MiscInfo] = "This ring increases hp and defense";

ItemData PowerRing
{
	description = "Power Ring";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData PowerRing0
{
	description = "Power Ring";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[MajorPowerRing, $AccessoryType] = $RingAccessoryType;
$AccessoryVar[MajorPowerRing, $SpecialVar] = "4 40 7 95";
$AccessoryVar[MajorPowerRing, $Weight] = 15;
$AccessoryVar[MajorPowerRing, $MiscInfo] = "This ring majorly increases hp and defense";

ItemData MajorPowerRing
{
	description = "Major Power Ring";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData MajorPowerRing0
{
	description = "Major Power Ring";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[ExtremePowerRing, $AccessoryType] = $RingAccessoryType;
$AccessoryVar[ExtremePowerRing, $SpecialVar] = "4 70 7 150";
$AccessoryVar[ExtremePowerRing, $Weight] = 25;
$AccessoryVar[ExtremePowerRing, $MiscInfo] = "This ring extremely increases hp and defense";

ItemData ExtremePowerRing
{
	description = "Extreme Power Ring";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData ExtremePowerRing0
{
	description = "Extreme Power Ring";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[GodlyPowerRing, $AccessoryType] = $RingAccessoryType;
$AccessoryVar[GodlyPowerRing, $SpecialVar] = "4 140 7 250";
$AccessoryVar[GodlyPowerRing, $Weight] = 60;
$AccessoryVar[GodlyPowerRing, $MiscInfo] = "This ring extremely increases hp and defense";

ItemData GodlyPowerRing
{
	description = "Godly Power Ring";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData GodlyPowerRing0
{
	description = "Godly Power Ring";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[HeavenlyPowerRing, $AccessoryType] = $RingAccessoryType;
$AccessoryVar[HeavenlyPowerRing, $SpecialVar] = "4 210 7 375";
$AccessoryVar[HeavenlyPowerRing, $Weight] = 90;
$AccessoryVar[HeavenlyPowerRing, $MiscInfo] = "This ring provides heavenly increases to hp and defense";

ItemData HeavenlyPowerRing
{
	description = "Heavenly Power Ring";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData HeavenlyPowerRing0
{
	description = "Heavenly Power Ring";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
//   Regen Necklaces
//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

$AccessoryVar[MinorRegenerationNecklace, $AccessoryType] = $TalismanAccessoryType;
$AccessoryVar[MinorRegenerationNecklace, $SpecialVar] = "10 0.7 11 1";
$AccessoryVar[MinorRegenerationNecklace, $Weight] = 20;
$AccessoryVar[MinorRegenerationNecklace, $MiscInfo] = "This necklace increases hp and mana regeneration";

ItemData MinorRegenerationNecklace
{
	description = "Minor Regeneration Necklace";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData MinorRegenerationNecklace0
{
	description = "Minor Regeneration Necklace";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[RegenerationNecklace, $AccessoryType] = $TalismanAccessoryType;
$AccessoryVar[RegenerationNecklace, $SpecialVar] = "10 2 11 2.5";
$AccessoryVar[RegenerationNecklace, $Weight] = 50;
$AccessoryVar[RegenerationNecklace, $MiscInfo] = "This necklace increases hp and mana regeneration";

ItemData RegenerationNecklace
{
	description = "Regeneration Necklace";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData RegenerationNecklace0
{
	description = "Regeneration Necklace";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[MajorRegenerationNecklace, $AccessoryType] = $TalismanAccessoryType;
$AccessoryVar[MajorRegenerationNecklace, $SpecialVar] = "10 5 11 6";
$AccessoryVar[MajorRegenerationNecklace, $Weight] = 100;
$AccessoryVar[MajorRegenerationNecklace, $MiscInfo] = "This necklace greatly increases hp and mana regeneration";

ItemData MajorRegenerationNecklace
{
	description = "Major Regeneration Necklace";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData MajorRegenerationNecklace0
{
	description = "Major Regeneration Necklace";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[ExtremeRegenerationNecklace, $AccessoryType] = $TalismanAccessoryType;
$AccessoryVar[ExtremeRegenerationNecklace, $SpecialVar] = "10 10 11 12";
$AccessoryVar[ExtremeRegenerationNecklace, $Weight] = 250;
$AccessoryVar[ExtremeRegenerationNecklace, $MiscInfo] = "This necklace extremely increases your hp and mana regeneration";

ItemData ExtremeRegenerationNecklace
{
	description = "Extreme Regeneration Necklace";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData ExtremeRegenerationNecklace0
{
	description = "Extreme Regeneration Necklace";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[GodlyRegenerationNecklace, $AccessoryType] = $TalismanAccessoryType;
$AccessoryVar[GodlyRegenerationNecklace, $SpecialVar] = "10 24 11 31";
$AccessoryVar[GodlyRegenerationNecklace, $Weight] = 750;
$AccessoryVar[GodlyRegenerationNecklace, $MiscInfo] = "This necklace gives you godly hp and mana regeneration";

ItemData GodlyRegenerationNecklace
{
	description = "Godly Regeneration Necklace";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData GodlyRegenerationNecklace0
{
	description = "Godly Regeneration Necklace";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[HeavenlyRegenerationNecklace, $AccessoryType] = $TalismanAccessoryType;
$AccessoryVar[HeavenlyRegenerationNecklace, $SpecialVar] = "10 36 11 47";
$AccessoryVar[HeavenlyRegenerationNecklace, $Weight] = 1125;
$AccessoryVar[HeavenlyRegenerationNecklace, $MiscInfo] = "This necklace provides heavenly hp and mana regeneration";

ItemData HeavenlyRegenerationNecklace
{
	description = "Heavenly Regeneration Necklace";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData HeavenlyRegenerationNecklace0
{
	description = "Heavenly Regeneration Necklace";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};


//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
//   HeadGear
//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

$AccessoryVar[IronHelmet, $AccessoryType] = $HeadAccessoryType;
$AccessoryVar[IronHelmet, $SpecialVar] = "7 25 3 35";
$AccessoryVar[IronHelmet, $Weight] = 10;
$AccessoryVar[IronHelmet, $MiscInfo] = "A iron helmet, providing defense from both physical and magic attacks!";

ItemData IronHelmet
{
	description = "Iron Helmet";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData IronHelmet0
{
	description = "Iron Helmet";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[GoldenHelmet, $AccessoryType] = $HeadAccessoryType;
$AccessoryVar[GoldenHelmet, $SpecialVar] = "7 60 3 100";
$AccessoryVar[GoldenHelmet, $Weight] = 25;
$AccessoryVar[GoldenHelmet, $MiscInfo] = "A golden helmet, providing defense from both physical and magic attacks!";

ItemData GoldenHelmet
{
	description = "Golden Helmet";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData GoldenHelmet0
{
	description = "Golden Helmet";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[CrystalHelmet, $AccessoryType] = $HeadAccessoryType;
$AccessoryVar[CrystalHelmet, $SpecialVar] = "7 110 3 180";
$AccessoryVar[CrystalHelmet, $Weight] = 45;
$AccessoryVar[CrystalHelmet, $MiscInfo] = "A crystal helmet, providing defense from both physical and magic attacks!";

ItemData CrystalHelmet
{
	description = "Crystal Helmet";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData CrystalHelmet0
{
	description = "Crystal Helmet";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[DiamondHelmet, $AccessoryType] = $HeadAccessoryType;
$AccessoryVar[DiamondHelmet, $SpecialVar] = "7 180 3 280";
$AccessoryVar[DiamondHelmet, $Weight] = 70;
$AccessoryVar[DiamondHelmet, $MiscInfo] = "A diamond helmet, providing defense from both physical and magic attacks!";

ItemData DiamondHelmet
{
	description = "Diamond Helmet";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData DiamondHelmet0
{
	description = "Diamond Helmet";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[BlackDiamondHelmet, $AccessoryType] = $HeadAccessoryType;
$AccessoryVar[BlackDiamondHelmet, $SpecialVar] = "7 300 3 500";
$AccessoryVar[BlackDiamondHelmet, $Weight] = 210;
$AccessoryVar[BlackDiamondHelmet, $MiscInfo] = "A black diamond helmet, providing defense from both physical and magic attacks!";

ItemData BlackDiamondHelmet
{
	description = "Black Diamond Helmet";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData BlackDiamondHelmet0
{
	description = "Black Diamond Helmet";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$HardcodedItemCost[RedDiamondHelmet] = 135000000;
$AccessoryVar[RedDiamondHelmet, $AccessoryType] = $HeadAccessoryType;
$AccessoryVar[RedDiamondHelmet, $SpecialVar] = "7 450 3 750";
$AccessoryVar[RedDiamondHelmet, $Weight] = 315;
$AccessoryVar[RedDiamondHelmet, $MiscInfo] = "A helmet forged from rare red diamond, fit for a king!";

ItemData RedDiamondHelmet
{
	description = "Red Diamond Helmet";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData RedDiamondHelmet0
{
	description = "Red Diamond Helmet";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$HardcodedItemCost[WhiteDiamondHelmet] = 150000000;
$AccessoryVar[WhiteDiamondHelmet, $AccessoryType] = $HeadAccessoryType;
$AccessoryVar[WhiteDiamondHelmet, $SpecialVar] = "7 600 3 1000";
$AccessoryVar[WhiteDiamondHelmet, $Weight] = 420;
$AccessoryVar[WhiteDiamondHelmet, $MiscInfo] = "The rarest of white diamonds forged into a helmet, fit for the King of Kronos!";

ItemData WhiteDiamondHelmet
{
	description = "White Diamond Helmet";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData WhiteDiamondHelmet0
{
	description = "White Diamond Helmet";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
//   Belts
//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

$AccessoryVar[AntiMagicBelt, $AccessoryType] = $BeltAccessoryType;
$AccessoryVar[AntiMagicBelt, $SpecialVar] = "3 250";
$AccessoryVar[AntiMagicBelt, $Weight] = 20;
$AccessoryVar[AntiMagicBelt, $MiscInfo] = "A magical belt with the power to shield you from magic!";

ItemData AntiMagicBelt
{
	description = "Antimagic Belt";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData AntiMagicBelt0
{
	description = "Antimagic Belt";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[MajorAntiMagicBelt, $AccessoryType] = $BeltAccessoryType;
$AccessoryVar[MajorAntiMagicBelt, $SpecialVar] = "3 550";
$AccessoryVar[MajorAntiMagicBelt, $Weight] = 40;
$AccessoryVar[MajorAntiMagicBelt, $MiscInfo] = "A strong magical belt with the power to shield you from magic!";

ItemData MajorAntiMagicBelt
{
	description = "Major Antimagic Belt";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData MajorAntiMagicBelt0
{
	description = "Major Antimagic Belt";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[ExtremeAntiMagicBelt, $AccessoryType] = $BeltAccessoryType;
$AccessoryVar[ExtremeAntiMagicBelt, $SpecialVar] = "3 900";
$AccessoryVar[ExtremeAntiMagicBelt, $Weight] = 70;
$AccessoryVar[ExtremeAntiMagicBelt, $MiscInfo] = "A strong magical belt with the power to shield you from magic!";

ItemData ExtremeAntiMagicBelt
{
	description = "Extreme AntiMagic Belt";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData ExtremeAntiMagicBelt0
{
	description = "Extreme AntiMagic Belt";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[GodlyAntiMagicBelt, $AccessoryType] = $BeltAccessoryType;
$AccessoryVar[GodlyAntiMagicBelt, $SpecialVar] = "3 2500";
$AccessoryVar[GodlyAntiMagicBelt, $Weight] = 200;
$AccessoryVar[GodlyAntiMagicBelt, $MiscInfo] = "A strong magical belt which gives you godly protection from magic.";

ItemData GodlyAntiMagicBelt
{
	description = "Godly AntiMagic Belt";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData GodlyAntiMagicBelt0
{
	description = "Godly AntiMagic Belt";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[HeavenlyAntiMagicBelt, $AccessoryType] = $BeltAccessoryType;
$AccessoryVar[HeavenlyAntiMagicBelt, $SpecialVar] = "3 3750";
$AccessoryVar[HeavenlyAntiMagicBelt, $Weight] = 300;
$AccessoryVar[HeavenlyAntiMagicBelt, $MiscInfo] = "A heavenly magical belt which provides divine protection from magic.";

ItemData HeavenlyAntiMagicBelt
{
	description = "Heavenly AntiMagic Belt";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData HeavenlyAntiMagicBelt0
{
	description = "Heavenly AntiMagic Belt";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
//   ARMOR MODIFYING ACCESSORIES
//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

$AccessoryVar[CheetaursPaws, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[CheetaursPaws, $SpecialVar] = "8 1";
$AccessoryVar[CheetaursPaws, $Weight] = 3;
$AccessoryVar[CheetaursPaws, $MiscInfo] = "Cheetaur's Paws increase speed and jump power";

ItemData CheetaursPaws
{
	description = "Cheetaur's Paws";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData CheetaursPaws0
{
	description = "Cheetaur's Paws";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[BootsOfGliding, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[BootsOfGliding, $SpecialVar] = "8 2";
$AccessoryVar[BootsOfGliding, $Weight] = 3;
$AccessoryVar[BootsOfGliding, $MiscInfo] = "Boots Of Gliding let you glide";

ItemData BootsOfGliding
{
	description = "Boots Of Gliding";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData BootsOfGliding0
{
	description = "Boots Of Gliding";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[WindWalkers, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[WindWalkers, $SpecialVar] = "8 3";
$AccessoryVar[WindWalkers, $Weight] = 3;
$AccessoryVar[WindWalkers, $MiscInfo] = "Wind Walkers let you fly!";

ItemData WindWalkers
{
	description = "Wind Walkers";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData WindWalkers0
{
	description = "Wind Walkers";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[WindPaws, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[WindPaws, $SpecialVar] = "8 4";
$AccessoryVar[WindPaws, $Weight] = 3;
$AccessoryVar[WindPaws, $MiscInfo] = "Wind Paws let you fly and run fast!";

ItemData WindPaws
{
	description = "Wind Paws";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData WindPaws0
{
	description = "Wind Paws";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[AdminBoots, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[AdminBoots, $Weight] = 1;
$AccessoryVar[AdminBoots, $SpecialVar] = "8 5";
$AccessoryVar[AdminBoots, $MiscInfo] = "<f2>FAST AS FUCK BOI! NO MANA COST!? CAN'T FUCKING BE TRUE! THEY'RE REAL?";
// AdminBoots has no value - it's a rare item (GenerateItemCost returns 0 for it)
// Also define for equipped version to ensure lookup works
$AccessoryVar[AdminBoots0, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[AdminBoots0, $Weight] = 1;

ItemData AdminBoots
{
	description = "Admin Boots";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData AdminBoots0
{
	description = "Admin Boots";
	className = "Equipped";
	shapeFile = "discammo";
    maxJetSideForceFactor = 0.8;
    maxJetForwardVelocity = 120;
    minJetEnergy = 1;
    jetForce = 400;
    jetEnergyDrain = 0.0;
	jumpImpulse = 75;
	maxForwardSpeed = $spdgm-50;
	maxBackwardSpeed = $spdgm-51;
	maxSideSpeed = $spdgm-51;

	heading = "aArmor";
};

// AdminBoots onMount/onUnmount - OnMount does #dk then #human (quick toggle), OnUnmount just unequips
function AdminBoots::onMount(%player, %item, %slot)
{
	%clientId = GetClientIdFromPlayerObject(%player);
	if(%clientId == -1 || %clientId == "")
		return;
	
	// Store original armor to restore later
	%originalArmor = Player::getArmor(%clientId);
	storeData(%clientId, "AdminBootsOriginalArmor", %originalArmor);
}

// Helper function to start the race change sequence also a change in rpgfunk.cs
function AdminBoots::startRaceChangeSequence(%clientId)
{
	if(%clientId == -1 || %clientId == "")
		return;
	
	// CRITICAL: Preserve current mana before race change (ChangeRace sets mana to full)
	%currentMana = fetchData(%clientId, "MANA");
	
	// CRITICAL: Set a flag to prevent RefreshAll from resetting mana during this sequence
	storeData(%clientId, "AdminBootsPreserveMana", %currentMana);
	
	// Set armor to AdminBootsArmor for free flight (jetEnergyDrain = 0.0) with reduced speed
	// This is what actually enables the flight properties and applies the speed reduction
	Player::setArmor(%clientId, "AdminBootsArmor");
	
	// Refresh character to ensure DeathKnight values apply
	RefreshAll(%clientId);
	
	// Switch back to Human (this will set mana to full, but we'll restore it)
	ChangeRace(%clientId, "Human");
	// Immediately restore mana after ChangeRace (before RefreshAll calls)
	if(%currentMana != "" && %currentMana != -1 && %currentMana != "0")
	{
		%maxMana = fetchData(%clientId, "MaxMANA");
		if(%currentMana < %maxMana)
			setMANA(%clientId, %currentMana);
	}
	
	// Refresh again to ensure all values are properly applied
	RefreshAll(%clientId);
	
	// Update appearance to ensure armor/visual updates
	UpdateAppearance(%clientId);
	
	RefreshAll(%clientId);
	
	// Ensure armor is set to AdminBootsArmor (in case UpdateAppearance changed it)
	Player::setArmor(%clientId, "AdminBootsArmor");
	
	// Final mana restoration after all RefreshAll calls
	if(%currentMana != "" && %currentMana != -1 && %currentMana != "0")
	{
		%maxMana = fetchData(%clientId, "MaxMANA");
		if(%currentMana < %maxMana)
			setMANA(%clientId, %currentMana);
	}
	
	// Clear the preserve mana flag
	storeData(%clientId, "AdminBootsPreserveMana", "");
}

function AdminBoots::onUnmount(%player, %item, %slot)
{
	%clientId = GetClientIdFromPlayerObject(%player);
	if(%clientId == -1 || %clientId == "")
		return;
	
	// Clear the trigger flag so it can be triggered again if re-equipped
	storeData(%clientId, "AdminBootsRaceChangeTriggered", "");
	
	// Restore original armor
	%originalArmor = fetchData(%clientId, "AdminBootsOriginalArmor");
	if(%originalArmor != "" && %originalArmor != -1)
	{
		Player::setArmor(%clientId, %originalArmor);
		storeData(%clientId, "AdminBootsOriginalArmor", "");
	}
	RefreshAll(%clientId);
	UpdateAppearance(%clientId);
}

function AdminBoots0::onMount(%player, %item, %slot)
{
	// Call the same function as AdminBoots
	AdminBoots::onMount(%player, %item, %slot);
}

function AdminBoots0::onUnmount(%player, %item, %slot)
{
	// Just unequip - no race changes
	// The item properties (jetEnergyDrain = 0.0, etc.) are handled by the ItemData itself
}

//============================================================================

$AccessoryVar[SteelKnightShield, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[SteelKnightShield, $SpecialVar] = "7 90 4 30";
$AccessoryVar[SteelKnightShield, $Weight] = 20;
$AccessoryVar[SteelKnightShield, $MiscInfo] = "The Steel Knight Shield is a unique item that provides great defense.";

ItemImageData SteelKnightShieldImage
{
	shapeFile = "shield3";
	mountPoint = 2;
	mountOffset = {0.10, -0.1, -0.3};
	mountRotation = {0, 0, 0.785};
};
ItemData SteelKnightShield
{
	description = "Steel Knight Shield";
	className = "Accessory";
	shapeFile = "shield3";
	imageType = SteelKnightShieldImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData SteelKnightShield0
{
	description = "Steel Knight Shield";
	className = "Equipped";
	shapeFile = "shield3";

	heading = "aArmor";
};

$AccessoryVar[CrystalKnightShield, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[CrystalKnightShield, $SpecialVar] = "7 180 4 90";
$AccessoryVar[CrystalKnightShield, $Weight] = 40;
$AccessoryVar[CrystalKnightShield, $MiscInfo] = "The Crystal Knight Shield is a unique item that provides great defense.";

ItemImageData CrystalKnightShieldImage
{
	shapeFile = "shield3";
	mountPoint = 2;
	mountOffset = {0.10, -0.1, -0.3};
	mountRotation = {0, 0, 0.785};
};
ItemData CrystalKnightShield
{
	description = "Crystal Knight Shield";
	className = "Accessory";
	shapeFile = "shield3";
	imageType = CrystalKnightShieldImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData CrystalKnightShield0
{
	description = "Crystal Knight Shield";
	className = "Equipped";
	shapeFile = "shield3";

	heading = "aArmor";
};

$AccessoryVar[DiamondKnightShield, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[DiamondKnightShield, $SpecialVar] = "7 320 4 170";
$AccessoryVar[DiamondKnightShield, $Weight] = 80;
$AccessoryVar[DiamondKnightShield, $MiscInfo] = "The Diamond Knight Shield is a unique item that provides great defense.";

ItemImageData DiamondKnightShieldImage
{
	shapeFile = "shield3";
	mountPoint = 2;
	mountOffset = {0.10, -0.1, -0.3};
	mountRotation = {0, 0, 0.785};
};
ItemData DiamondKnightShield
{
	description = "Diamond Knight Shield";
	className = "Accessory";
	shapeFile = "shield3";
	imageType = DiamondKnightShieldImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData DiamondKnightShield0
{
	description = "Diamond Knight Shield";
	className = "Equipped";
	shapeFile = "shield3";

	heading = "aArmor";
};

$AccessoryVar[BlackDiamondKnightShield, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[BlackDiamondKnightShield, $SpecialVar] = "7 1000 4 450";
$AccessoryVar[BlackDiamondKnightShield, $Weight] = 200;
$AccessoryVar[BlackDiamondKnightShield, $MiscInfo] = "Defend yourself with a shield made from Black Diamond!";

ItemImageData BlackDiamondKnightShieldImage
{
	shapeFile = "shield3";
	mountPoint = 2;
	mountOffset = {0.10, -0.1, -0.3};
	mountRotation = {0, 0, 0.785};
};
ItemData BlackDiamondKnightShield
{
	description = "Black Diamond Knight Shield";
	className = "Accessory";
	shapeFile = "shield3";
	imageType = BlackDiamondKnightShieldImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData BlackDiamondKnightShield0
{
	description = "Black Diamond Knight Shield";
	className = "Equipped";
	shapeFile = "shield3";

	heading = "aArmor";
};

$AccessoryVar[RedDiamondKingShield, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[RedDiamondKingShield, $SpecialVar] = "7 1500 4 600";
$AccessoryVar[RedDiamondKingShield, $Weight] = 250;
$AccessoryVar[RedDiamondKingShield, $MiscInfo] = "A shield forged from rare red diamond, ripped from the hands of the fallen King of Curama!";

ItemImageData RedDiamondKingShieldImage
{
	shapeFile = "shield3";
	mountPoint = 2;
	mountOffset = {0.10, -0.1, -0.3};
	mountRotation = {0, 0, 0.785};
};
ItemData RedDiamondKingShield
{
	description = "Red Diamond King Shield";
	className = "Accessory";
	shapeFile = "shield3";
	imageType = RedDiamondKingShieldImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData RedDiamondKingShield0
{
	description = "Red Diamond King Shield";
	className = "Equipped";
	shapeFile = "shield3";

	heading = "aArmor";
};

$AccessoryVar[WhiteDiamondKingShield, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[WhiteDiamondKingShield, $SpecialVar] = "7 2000 4 900";
$AccessoryVar[WhiteDiamondKingShield, $Weight] = 400;
$AccessoryVar[WhiteDiamondKingShield, $MiscInfo] = "A shield forged from the rarest white diamonds, hand crafted for the King of Kronos! How did you get this?";

ItemImageData WhiteDiamondKingShieldImage
{
	shapeFile = "shield3";
	mountPoint = 2;
	mountOffset = {0.10, -0.1, -0.3};
	mountRotation = {0, 0, 0.785};
};
ItemData WhiteDiamondKingShield
{
	description = "White Diamond King Shield";
	className = "Accessory";
	shapeFile = "shield3";
	imageType = WhiteDiamondKingShieldImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData WhiteDiamondKingShield0
{
	description = "White Diamond King Shield";
	className = "Equipped";
	shapeFile = "shield3";

	heading = "aArmor";
};


//============================================================================

ItemData MaleHumanTownBot
{
	description = "Male Town Bot";
	className = "TownBot";
	shapeFile = "rpgmalehuman";

	visibleToSensor = true;	//thanks Adger!!
	mapFilter = 1;		//thanks Adger!!
};
ItemData FemaleHumanTownBot
{
	description = "Female Town Bot";
	className = "TownBot";
	shapeFile = "lfemalehuman";

	visibleToSensor = true;	//thanks Adger!!
	mapFilter = 1;		//thanks Adger!!
};

//------------------------
$AccessoryVar[Tent, $Weight] = 40;
$AccessoryVar[Tent, $MiscInfo] = "A tent. Use #camp to set it up, and #uncamp to disassemble it.";

ItemData Tent
{
	description = "Tent";
	shapeFile = "armorKit";
	heading = "eMiscellany";
	className = "Accessory";
	shadowDetailMask = 4;
	price = 0;
};
//------------------------
$AccessoryVar[ScoutVehicle, $Weight] = 80;
$AccessoryVar[ScoutVehicle, $MiscInfo] = "A scout vehicle. Use #deploy to set it up, and #undeploy to disassemble it.";

ItemData ScoutVehicle
{
	description = "Scout Vehicle";
	shapeFile = "armorKit";
	heading = "eMiscellany";
	className = "Accessory";
	shadowDetailMask = 4;
	price = 0;
};
//------------------------

//===== MISC STUFF ===============================================================

ItemData Lootbag
{
	description = "Backpack";
	className = "Lootbag";
	shapeFile = "ammo2";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

//===================
//  Mining stuff
//===================
//$AccessoryVar[Quartz, $Weight] = 0.2;

//$AccessoryVar[Quartz, $MiscInfo] = "Quartz";

//$HardcodedItemCost[SmallRock] = 13;

%f = 43;
//$ItemList[Mining, 1] = "SmallRock " @ round($HardcodedItemCost[SmallRock] / %f)+2;

//ItemData Quartz
//{
//	description = "Quartz";
//	className = "Accessory";
//	shapeFile = "quartz";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};


$AccessoryVar[DuelCard, $Weight] = 2;
$AccessoryVar[DuelCard, $MiscInfo] = "Card you can use to challenge someone. Read [#duelrules]";

// ItemData DuelCard - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Duel Card";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};


//$AccessoryVar[RubyNecklace, $Weight] = 3;
//$AccessoryVar[RubyNecklace, $MiscInfo] = "A ruby necklace";

//ItemData RubyNecklace
//{
//	description = "Ruby Necklace";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[OgreTooth, $Weight] = 8;
$AccessoryVar[OgreTooth, $MiscInfo] = "An ogre tooth";

// ItemData OgreTooth - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Ogre Tooth";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[AncientScroll, $Weight] = 15;
$AccessoryVar[AncientScroll, $MiscInfo] = "A very old look scroll";

// ItemData AncientScroll - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Ancient Scroll";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[Crown, $Weight] = 20;
$AccessoryVar[Crown, $MiscInfo] = "A very valuable crown.";

// ItemData Crown - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Crown";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[KronoStone, $Weight] = 20;
$AccessoryVar[KronoStone, $MiscInfo] = "A mystical stone sizzling with power";

// ItemData KronoStone - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Krono Stone";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[MinotaurHorn, $Weight] = 30;
$AccessoryVar[MinotaurHorn, $MiscInfo] = "A horn from a Minotaur's Head";

// ItemData MinotaurHorn - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Minotaur Horn";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[AlienSpine, $Weight] = 30;
$AccessoryVar[AlienSpine, $MiscInfo] = "A long spine from an alien";

// ItemData AlienSpine - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Alien Spine";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[AlienEye, $Weight] = 60;
$AccessoryVar[AlienEye, $MiscInfo] = "A gooey eye from the Alien Queen";

// ItemData AlienEye - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Alien Eye";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[DemonBreath, $Weight] = 40;
$AccessoryVar[DemonBreath, $MiscInfo] = "The very rare breath of a demon";

// ItemData DemonBreath - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Demon Breath";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};
$AccessoryVar[AngelsTear, $Weight] = 50;
$AccessoryVar[AngelsTear, $MiscInfo] = "A tear shed from an angel when it dies.";

// ItemData AngelsTear - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Angels Tear";
//	className = "Accessory";
//	shapeFile = "Diamond";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[BlackStatue, $Weight] = 1;
$AccessoryVar[BlackStatue, $MiscInfo] = "A black statue";

// ItemData BlackStatue - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Black Statue";
//	className = "Accessory";
//	shapeFile = "mineammo";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[SkeletonBone, $Weight] = 10;
$AccessoryVar[SkeletonBone, $MiscInfo] = "A skeleton bone";

// ItemData SkeletonBone - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Skeleton Bone";
//	className = "Accessory";
//	shapeFile = "grenade";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

$AccessoryVar[EnchantedStone, $Weight] = 5;
$AccessoryVar[EnchantedStone, $MiscInfo] = "An enchanted stone";

$AccessoryVar[Bible, $Weight] = 5;
$AccessoryVar[Bible, $MiscInfo] = "Do you have time to talk about your lord and savior?";

// ItemData EnchantedStone - MIGRATED TO BELT SYSTEM (no longer uses ItemData)
//{
//	description = "Enchanted Stone";
//	className = "Accessory";
//	shapeFile = "granite";
//	heading = "eMiscellany";
//	shadowDetailMask = 4;
//	price = 0;
//};

//===================
//  LORE ITEMS
//===================

//========= ORBS ===================================================

//i suggest putting orbs that protect from water at the top of the list.
//$ItemList[Orb, 2] = "OrbOfBreath";
$ItemList[Orb, 1] = "AdminOrb";

//Admin Orb
$AccessoryVar[AdminOrb, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[AdminOrb, $Weight] = 1.0;
$AccessoryVar[AdminOrb, $MiscInfo] = "The Admin Orb provides you with powerful illumination.";
$OverrideMountPoint[AdminOrb] = 2;
$BurnOut[AdminOrb] = 999999999;
$BurnOutInRain[AdminOrb] = 999999999;
$ProtectFromWater[AdminOrb] = "";

ItemImageData AdminOrbImage
{
	shapeFile = "orb";
	mountPoint = $OverrideMountPoint[AdminOrb];
	mountOffset = {0.0, 0.0, 1.8};
	mountRotation = {5, 3, 3};

	lightType = 2;
	lightRadius = 60;
	lightTime = 9999;
	lightColor = { 1.0, 1.0, 0.8 };
};
ItemData AdminOrb
{
	description = "Admin Orb";
	className = "Accessory";
	shapeFile = "orb";
	imageType = AdminOrbImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData AdminOrb0
{
	description = "Lit Admin Orb";
	className = "Equipped";
	shapeFile = "orb";
	imageType = AdminOrbImage;

	heading = "aArmor";
};

//Orb of Breath
$AccessoryVar[OrbOfBreath, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[OrbOfBreath, $Weight] = 0.8;
$AccessoryVar[OrbOfBreath, $MiscInfo] = "The Orb Of Breath provides you with a temporary ability to breathe underwater.";
$OverrideMountPoint[OrbOfBreath] = 2;
$BurnOut[OrbOfBreath] = 300;
$BurnOutInRain[OrbOfBreath] = 0;
$ProtectFromWater[OrbOfBreath] = True;
