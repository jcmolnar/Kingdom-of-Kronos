function SetupShop(%clientId, %id)
{
	dbecho($dbechoMode, "SetupShop(" @ %clientId @ ", " @ %id @ ")");

	ClearCurrentShopVars(%clientId);
	%clientId.currentShop = %id;

	%clientId.bulkNum = "";

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	Client::setGuiMode(%clientId, 4);

	%txt = "<f1><jc>COINS: " @ fetchData(%clientId, "COINS");
	Client::setInventoryText(%clientId, %txt);

	%info = $BotInfo[%id.name, SHOP];	

	for(%i = 0; GetWord(%info, %i) != -1; %i++)
	{
		%a = GetWord(%info, %i);

		%max = getNumItems();		
		for(%z = 0; %z < %max; %z++)
		{
			%item = getItemData(%z);

			if($AccessoryVar[%item, $ShopIndex] == %a)
			{
				Client::setItemShopping(%clientId, %item);
				Client::setItemBuying(%clientId, %item);
			}
		}
	}
}

function SetupBank(%clientId, %id)
{
	dbecho($dbechoMode, "SetupBank(" @ %clientId @ ", " @ %id @ ")");

	ClearCurrentShopVars(%clientId);
	%clientId.currentBank = %id;

	%clientId.bulkNum = "";

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	if(Client::getGuiMode(%clientId) != 4)
		Client::setGuiMode(%clientId, 4);

	%txt = "<f1><jc>COINS: " @ fetchData(%clientId, "COINS");
	Client::setInventoryText(%clientId, %txt);

	%info = fetchData(%clientId, "BankStorage");

	for(%i = 0; GetWord(%info, %i) != -1; %i+=2)
	{
		%item = GetWord(%info, %i);

		Client::setItemShopping(%clientId, %item);
		Client::setItemBuying(%clientId, %item);
	}
}

function SetupBlacksmith(%clientId, %id)
{
	dbecho($dbechoMode, "SetupBlacksmith(" @ %clientId @ ", " @ %id @ ")");

	%clientId.currentSmith = %id;

	%clientId.bulkNum = "";

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	if(Client::getGuiMode(%clientId) != 4)
		Client::setGuiMode(%clientId, 4);

	%info = fetchData(%clientId, "TempSmith");
	for(%i = 0; GetWord(%info, %i) != -1; %i+=2)
	{
		%item = GetWord(%info, %i);

		Client::setItemShopping(%clientId, %item);
		Client::setItemBuying(%clientId, %item);
	}

	%txt = "<f1><jc>COINS: " @ fetchData(%clientId, "COINS");
	Client::setInventoryText(%clientId, %txt);
}

function SetupInvSteal(%clientId, %id)
{
	dbecho($dbechoMode, "SetupInvSteal(" @ %clientId @ ", " @ %id @ ")");

	ClearCurrentShopVars(%clientId);
	%clientId.currentInvSteal = %id;

	%clientId.bulkNum = "";

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	if(Client::getGuiMode(%clientId) != 4)
		Client::setGuiMode(%clientId, 4);

	%txt = "<f1><jc>" @ Client::getName(%id) @ "'s inventory";
	Client::setInventoryText(%clientId, %txt);

	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%item = getItemData(%i);
		%itemcount = Player::getItemCount(%id, %item);

		if(%itemcount > 0)
		{
			Client::setItemShopping(%clientId, %item);
			Client::setItemBuying(%clientId, %item);
		}
	}
}

function SetupCreatePack(%clientId)
{
	dbecho($dbechoMode, "SetupCreatePack(" @ %clientId @ ")");

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	if(Client::getGuiMode(%clientId) != 4)
		Client::setGuiMode(%clientId, 4);

	%info = fetchData(%clientId, "TempPack");
	for(%i = 0; GetWord(%info, %i) != -1; %i+=2)
	{
		%item = GetWord(%info, %i);

		Client::setItemShopping(%clientId, %item);
		Client::setItemBuying(%clientId, %item);
	}
}

function ClearCurrentShopVars(%clientId)
{
	dbecho($dbechoMode, "ClearCurrentShopVars(" @ %clientId @ ")");

      %clientId.currentShop = "";
      %clientId.currentBank = "";
      %clientId.currentSmith = "";
	%clientId.currentInvSteal = "";

	storeData(%clientId, "TempPack", "");
	storeData(%clientId, "TempSmith", "");
}

$AccessoryVar[BluePotion, $ShopIndex] = 1;
$AccessoryVar[CrystalBluePotion, $ShopIndex] = 2;
$AccessoryVar[EnergyVial, $ShopIndex] = 3;
$AccessoryVar[CrystalEnergyVial, $ShopIndex] = 4;
$AccessoryVar[ScoutVehicle, $ShopIndex] = 5;

$AccessoryVar[CheetaursPaws, $ShopIndex] = 33;
$AccessoryVar[BootsOfGliding, $ShopIndex] = 34;
$AccessoryVar[WindWalkers, $ShopIndex] = 35;

$AccessoryVar[Hatchet, $ShopIndex] = 39;
$AccessoryVar[Club, $ShopIndex] = 47;
$AccessoryVar[Knife, $ShopIndex] = 55;
$AccessoryVar[PickAxe, $ShopIndex] = 63;
$AccessoryVar[Tent, $ShopIndex] = 98;
$AccessoryVar[OrbOfLuminance, $ShopIndex] = 99;
$AccessoryVar[OrbOfBreath, $ShopIndex] = 103;

$AccessoryVar[BlackStatue, $ShopIndex] = 100;
$AccessoryVar[SkeletonBone, $ShopIndex] = 101;
$AccessoryVar[EnchantedStone, $ShopIndex] = 102;

//Custom shop variables
$AccessoryVar[RustyIronBlade, $ShopIndex] = 134;
$AccessoryVar[ButterKnife, $ShopIndex] = 135;
$AccessoryVar[CrackedStick, $ShopIndex] = 136;
$AccessoryVar[RatSkinShirt, $ShopIndex] = 137;
$AccessoryVar[SharpIronBlade, $ShopIndex] = 139;
$AccessoryVar[LongKnife, $ShopIndex] = 140;
$AccessoryVar[IronStick, $ShopIndex] = 141;
$AccessoryVar[IronBroadSword, $ShopIndex] = 143;
$AccessoryVar[IronSpear, $ShopIndex] = 144;
$AccessoryVar[IronMace, $ShopIndex] = 145;
$AccessoryVar[MinorPowerRing, $ShopIndex] = 146;
$AccessoryVar[StuddedLeatherSuit, $ShopIndex] = 147;
$AccessoryVar[SteelBroadSword, $ShopIndex] = 148;
$AccessoryVar[SteelSpear, $ShopIndex] = 149;
$AccessoryVar[SteelMace, $ShopIndex] = 150;
$AccessoryVar[ToughHideSuit, $ShopIndex] = 152;
$AccessoryVar[SteelLongSword, $ShopIndex] = 153;
$AccessoryVar[SteelPike, $ShopIndex] = 154;
$AccessoryVar[SteelHammer, $ShopIndex] = 155;
$AccessoryVar[PowerRing, $ShopIndex] = 156;
$AccessoryVar[IronScaleMail, $ShopIndex] = 157;
$AccessoryVar[GoldenLongSword, $ShopIndex] = 158;
$AccessoryVar[GoldenPike, $ShopIndex] = 159;
$AccessoryVar[SteelWarHammer, $ShopIndex] = 160;
$AccessoryVar[MinorRegenerationNecklace, $ShopIndex] = 161;
$AccessoryVar[SteelScaleMail, $ShopIndex] = 162;
$AccessoryVar[GoldenBastardSword, $ShopIndex] = 163;
$AccessoryVar[CrystalPike, $ShopIndex] = 164;
$AccessoryVar[GoldenWarHammer, $ShopIndex] = 165;
$AccessoryVar[SteelBrigandineMail, $ShopIndex] = 166;
$AccessoryVar[CrystalBastardSword, $ShopIndex] = 167;
$AccessoryVar[CrystalTrident, $ShopIndex] = 168;
$AccessoryVar[GoldenDivineMace, $ShopIndex] = 169;
$AccessoryVar[GoldenBrigandineMail, $ShopIndex] = 170;
$AccessoryVar[TemperedCrystalBastardSword, $ShopIndex] = 171;
$AccessoryVar[TemperedCrystalTrident, $ShopIndex] = 172;
$AccessoryVar[CrystalDivineMace, $ShopIndex] = 173;
$AccessoryVar[GoldenChainMail, $ShopIndex] = 174;
$AccessoryVar[CrystalClaymore, $ShopIndex] = 175;
$AccessoryVar[DiamondTrident, $ShopIndex] = 176;
$AccessoryVar[DiamondDivineMace, $ShopIndex] = 177;
$AccessoryVar[CrystalChainMail, $ShopIndex] = 178;
$AccessoryVar[MajorPowerRing, $ShopIndex] = 179;
$AccessoryVar[CrystalRingMail, $ShopIndex] = 180;
$AccessoryVar[DiamondClaymore, $ShopIndex] = 181;
$AccessoryVar[DiamondDeathSpear, $ShopIndex] = 182;
$AccessoryVar[DiamondBrainSpiller, $ShopIndex] = 183;
$AccessoryVar[CrystalBandedMail, $ShopIndex] = 184;
$AccessoryVar[CrystalSplintMail, $ShopIndex] = 185;
$AccessoryVar[TungstenSplintMail, $ShopIndex] = 186;
$AccessoryVar[DiamondLegendSword, $ShopIndex] = 187;
$AccessoryVar[DiamondLegendSpear, $ShopIndex] = 188;
$AccessoryVar[DiamondLegendMace, $ShopIndex] = 189;
$AccessoryVar[TungstenPlateMail, $ShopIndex] = 190;
$AccessoryVar[DiamondPlateMail, $ShopIndex] = 191;
$AccessoryVar[DiamondFieldPlate, $ShopIndex] = 192;
$AccessoryVar[ExtremePowerRing, $ShopIndex] = 193;
$AccessoryVar[SteelKnightShield, $ShopIndex] = 194;
$AccessoryVar[RegenerationNecklace, $ShopIndex] = 195;
$AccessoryVar[BlackDiamondDreamSword, $ShopIndex] = 196;
$AccessoryVar[BlackDiamondDreamSpear, $ShopIndex] = 197;
$AccessoryVar[BlackDiamondDreamMace, $ShopIndex] = 198;
$AccessoryVar[DiamondFullPlate, $ShopIndex] = 199;
$AccessoryVar[GodlyPowerRing, $ShopIndex] = 200;
$AccessoryVar[CrystalKnightShield, $ShopIndex] = 201;
$AccessoryVar[MajorRegenerationNecklace, $ShopIndex] = 202;
$AccessoryVar[WindPaws, $ShopIndex] = 203;
$AccessoryVar[IronHelmet, $ShopIndex] = 204;
$AccessoryVar[GoldenHelmet, $ShopIndex] = 205;
$AccessoryVar[CrystalHelmet, $ShopIndex] = 206;
$AccessoryVar[DiamondHelmet, $ShopIndex] = 207;
$AccessoryVar[AntiMagicBelt, $ShopIndex] = 208;
$AccessoryVar[MajorAntiMagicBelt, $ShopIndex] = 209;
$AccessoryVar[BlackDiamondAtomSplitter, $ShopIndex] = 210;
$AccessoryVar[BlackDiamondAtomPiercer, $ShopIndex] = 211;
$AccessoryVar[BlackDiamondAtomSmasher, $ShopIndex] = 212;
$AccessoryVar[ExtremeAntiMagicBelt, $ShopIndex] = 213;
$AccessoryVar[BlackDiamondHelmet, $ShopIndex] = 214;
$AccessoryVar[DiamondKnightShield, $ShopIndex] = 215;
$AccessoryVar[BlackDiamondFullPlate, $ShopIndex] = 216;
$AccessoryVar[RedDiamondPlate, $ShopIndex] = 217;
$AccessoryVar[GodlyAntiMagicBelt, $ShopIndex] = 218;
$AccessoryVar[BlackDiamondKnightShield, $ShopIndex] = 219;
$AccessoryVar[ExtremeRegenerationNecklace, $ShopIndex] = 220;
$AccessoryVar[GodlyRegenerationNecklace, $ShopIndex] = 221;
$AccessoryVar[TerminusEst, $ShopIndex] = 222;
$AccessoryVar[WhiteDiamondPlate, $ShopIndex] = 223;
