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
//AntiMagic Belts
$HardcodedItemCost[AntiMagicBelt] = 200000;
$HardcodedItemCost[MajorAntiMagicBelt] = 1500000;
$HardcodedItemCost[ExtremeAntiMagicBelt] = 12000000;
$HardcodedItemCost[GodlyAntiMagicBelt] = 75000000;

$HardcodedItemCost[Tent] = 4000;
$HardcodedItemCost[ScoutVehicle] = 500000;
$HardcodedItemCost[OrbOfLuminance] = 25;
$HardcodedItemCost[OrbOfBreath] = 1000;
//boots
$HardcodedItemCost[CheetaursPaws] = 1500;
$HardcodedItemCost[BootsOfGliding] = 8000;
$HardcodedItemCost[WindWalkers] = 45000;
$HardcodedItemCost[WindPaws] = 1000000;
//Power Rings
$HardcodedItemCost[MinorPowerRing] = 800;
$HardcodedItemCost[PowerRing] = 2500;
$HardcodedItemCost[MajorPowerRing] = 20000;
$HardcodedItemCost[ExtremePowerRing] = 300000;
$HardcodedItemCost[GodlyPowerRing] = 2000000;
//Regen Necklaces
$HardcodedItemCost[MinorRegenerationNecklace] = 3000;
$HardcodedItemCost[RegenerationNecklace] = 40000;
$HardcodedItemCost[MajorRegenerationNecklace] = 10000000;
$HardcodedItemCost[ExtremeRegenerationNecklace] = 50000000;
$HardcodedItemCost[GodlyRegenerationNecklace] = 120000000;

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
}

//=====================
// ACCESSORY FUNCTIONS
//=====================

function GetAccessoryVar(%item, %type)
{
	dbecho($dbechoMode, "GetAccessoryVar(" @ %item @ ", " @ %type @ ")");

	%nitem = getCroppedItem(%item);

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

	%list = "";
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%count = Player::getItemCount(%clientId, %i);

		if(%count)
		{
			%item = getItemData(%i);

			%flag = "";
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
				if(%item.className == "Equipped" || %item.className == "Weapon" || %item.className == "Backpack")
				{
					if(%item.className == "Weapon")
					{
						if(Player::getMountedItem(%clientId, $WeaponSlot) == %item)
							%flag = True;
					}
					else if(%item.className == "Backpack")
					{
						if(Player::getMountedItem(%clientId, $BackpackSlot) == %item)
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
				if(%filter != -1)
				{
					%flag2 = "";
					%av = GetAccessoryVar(%item, $SpecialVar);
					for(%j = 0; GetWord(%av, %j) != -1; %j+=2)
					{
						%w = GetWord(%av, %j);
						if(String::findSubStr(%filter, %w) != -1)
							%flag2 = True;
					}
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

	%add = 0;
	%list = GetAccessoryList(%clientId, 4, %char);
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%w = GetWord(%list, %i);

		%slot = "";
		if(%w.className == Weapon)
			%slot = $WeaponSlot;
		else if(%w.className == Backpack)
			%slot = $BackpackSlot;

		if(%slot != "")
		{
			if(Player::getMountedItem(%clientId, %slot) == %w)
				%count = 1;
			else
				%count = 0;
		}
		else
			%count = Player::getItemCount(%clientId, %w);

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
			%amnt = Backpack::HasThisStuff(%clientid,%item);
			if(%amnt > 0)
			{
				%item = $backpackitem[%item, "Item"];
				%name = $backpackitem[%item, "Name"];
				%newmsg = nsprintf(%msg, %name);
				Client::sendMessage(%clientId, %msgcolor, %newmsg);
				Backpack::TakeThisStuff(%clientid,%item,%amnt);
			}
		}
		if(Player::getItemCount(%clientId, %item))
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

	//the $ArmorList is present only for this function so far, in order to speed things up and not have to cycle thru
	//each and every item in the game
	for(%i = 1; $ArmorList[%i] != ""; %i++)
	{
		if(Player::getItemCount(%clientId, $ArmorList[%i] @ "0"))
			return $ArmorList[%i];
	}
	return "";
}

//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
//   POTIONS
//=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

$AccessoryVar[BluePotion, $Weight] = 4;
$AccessoryVar[BluePotion, $MiscInfo] = "A blue potion that heals 15 HP";
ItemData BluePotion
{
	description = "Blue Potion";
	shapeFile = "armorKit";
	heading = "eMiscellany";
	className = "Accessory";
	shadowDetailMask = 4;
	price = 0;
};
function BluePotion::onUse(%player,%item)
{
	%clientId = Player::getClient(%player);

	Player::decItemCount(%player,%item);
	%hp = fetchData(%clientId, "HP");
	refreshHP(%clientId, -0.15);
	refreshAll(%clientId);

	if(fetchData(%clientId, "HP") != %hp)
		UseSkill(%clientId, $SkillHealing, True, True);
}

$AccessoryVar[CrystalBluePotion, $Weight] = 10;
$AccessoryVar[CrystalBluePotion, $MiscInfo] = "A crystal blue potion that heals 60 HP";
ItemData CrystalBluePotion
{
	description = "Crystal Blue Potion";
	shapeFile = "armorKit";
	heading = "eMiscellany";
	className = "Accessory";
	shadowDetailMask = 4;
	price = 0;
};
function CrystalBluePotion::onUse(%player,%item)
{
	%clientId = Player::getClient(%player);

	Player::decItemCount(%player,%item);
	%hp = fetchData(%clientId, "HP");
	refreshHP(%clientId, -0.6);
	refreshAll(%clientId);

	if(fetchData(%clientId, "HP") != %hp)
		UseSkill(%clientId, $SkillHealing, True, True);
}

$AccessoryVar[EnergyVial, $Weight] = 2;
$AccessoryVar[EnergyVial, $MiscInfo] = "An energy vial that provides 16 MP";
ItemData EnergyVial
{
	description = "Energy Vial";
	shapeFile = "armorKit";
	heading = "eMiscellany";
	className = "Accessory";
	shadowDetailMask = 4;
	price = 0;
};
function EnergyVial::onUse(%player,%item)
{
	%clientId = Player::getClient(%player);

	Player::decItemCount(%player,%item);
	refreshMANA(%clientId, -16);
	refreshAll(%clientId);
}

$AccessoryVar[CrystalEnergyVial, $Weight] = 5;
$AccessoryVar[CrystalEnergyVial, $MiscInfo] = "A crystal energy vial that provides 50 MP";
ItemData CrystalEnergyVial
{
	description = "Crystal Energy Vial";
	shapeFile = "armorKit";
	heading = "eMiscellany";
	className = "Accessory";
	shadowDetailMask = 4;
	price = 0;
};
function CrystalEnergyVial::onUse(%player,%item)
{
	%clientId = Player::getClient(%player);

	Player::decItemCount(%player,%item);
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

ItemData DuelCard
{
	description = "Duel Card";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};


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

ItemData OgreTooth
{
	description = "Ogre Tooth";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[AncientScroll, $Weight] = 15;
$AccessoryVar[AncientScroll, $MiscInfo] = "A very old look scroll";

ItemData AncientScroll
{
	description = "Ancient Scroll";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[Crown, $Weight] = 20;
$AccessoryVar[Crown, $MiscInfo] = "A very valuable crown.";

ItemData Crown
{
	description = "Crown";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[KronoStone, $Weight] = 20;
$AccessoryVar[KronoStone, $MiscInfo] = "A mystical stone sizzling with power";

ItemData KronoStone
{
	description = "Krono Stone";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[MinotaurHorn, $Weight] = 30;
$AccessoryVar[MinotaurHorn, $MiscInfo] = "A horn from a Minotaur's Head";

ItemData MinotaurHorn
{
	description = "Minotaur Horn";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[AlienSpine, $Weight] = 30;
$AccessoryVar[AlienSpine, $MiscInfo] = "A long spine from an alien";

ItemData AlienSpine
{
	description = "Alien Spine";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[AlienEye, $Weight] = 60;
$AccessoryVar[AlienEye, $MiscInfo] = "A gooey eye from the Alien Queen";

ItemData AlienEye
{
	description = "Alien Eye";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[DemonBreath, $Weight] = 40;
$AccessoryVar[DemonBreath, $MiscInfo] = "The very rare breath of a demon";

ItemData DemonBreath
{
	description = "Demon Breath";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};
$AccessoryVar[AngelsTear, $Weight] = 50;
$AccessoryVar[AngelsTear, $MiscInfo] = "A tear shed from an angel when it dies.";

ItemData AngelsTear
{
	description = "Angels Tear";
	className = "Accessory";
	shapeFile = "Diamond";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[BlackStatue, $Weight] = 1;
$AccessoryVar[BlackStatue, $MiscInfo] = "A black statue";

ItemData BlackStatue
{
	description = "Black Statue";
	className = "Accessory";
	shapeFile = "mineammo";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[SkeletonBone, $Weight] = 10;
$AccessoryVar[SkeletonBone, $MiscInfo] = "A skeleton bone";

ItemData SkeletonBone
{
	description = "Skeleton Bone";
	className = "Accessory";
	shapeFile = "grenade";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

$AccessoryVar[EnchantedStone, $Weight] = 5;
$AccessoryVar[EnchantedStone, $MiscInfo] = "An enchanted stone";

ItemData EnchantedStone
{
	description = "Enchanted Stone";
	className = "Accessory";
	shapeFile = "granite";
	heading = "eMiscellany";
	shadowDetailMask = 4;
	price = 0;
};

//===================
//  LORE ITEMS
//===================

//========= ORBS ===================================================

//i suggest putting orbs that protect from water at the top of the list.
//$ItemList[Orb, 2] = "OrbOfBreath";
$ItemList[Orb, 1] = "OrbOfLuminance";

//Orb of Luminance
$AccessoryVar[OrbOfLuminance, $AccessoryType] = $ShieldAccessoryType;
$AccessoryVar[OrbOfLuminance, $Weight] = 1.0;
$AccessoryVar[OrbOfLuminance, $MiscInfo] = "The Orb Of Luminance provides you with temporary illumination.";
$OverrideMountPoint[OrbOfLuminance] = 2;
$BurnOut[OrbOfLuminance] = 150;
$BurnOutInRain[OrbOfLuminance] = 5;
$ProtectFromWater[OrbOfLuminance] = "";

ItemImageData OrbOfLuminanceImage
{
	shapeFile = "orb";
	mountPoint = $OverrideMountPoint[OrbOfLuminance];
	mountOffset = {0.0, 0.0, 1.8};
	mountRotation = {5, 3, 3};

	lightType = 2;
	lightRadius = 13;
	lightTime = 9999;
	lightColor = { 0.95, 0.85, 0.55 };
};
ItemData OrbOfLuminance
{
	description = "Orb Of Luminance";
	className = "Accessory";
	shapeFile = "orb";
	imageType = OrbOfLuminanceImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData OrbOfLuminance0
{
	description = "Lit Orb Of Luminance";
	className = "Equipped";
	shapeFile = "orb";
	imageType = OrbOfLuminanceImage;

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
