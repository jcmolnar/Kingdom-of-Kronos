function SetupShop(%clientId, %id)
{
	dbecho($dbechoMode, "SetupShop(" @ %clientId @ ", " @ %id @ ")");

	ClearCurrentShopVars(%clientId);
	%clientId.currentShop = %id;

	%clientId.bulkNum = "";

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	// HUD clients get the Kronos shop screen (opened at the end of this
	// function, after the item list below is built); vanilla clients
	// get the stock CmdInventory gui mode - unchanged.
	%clientId.kshopCount = 0;
	%clientId.kshopBeltCnt = 0;
	if(!%clientId.hasKronosHUD)
		Client::setGuiMode(%clientId, 4);

	%txt = "<f1><jc>COINS: " @ Number::Beautify(fetchData(%clientId, "COINS"), -3);
	Client::setInventoryText(%clientId, %txt);

	// Get bot name from stored data (town bots are now Player objects, not Item objects)
	// %id is now a clientId, not an Item object, so we can't use %id.name
	%botName = fetchData(%id, "BotInfoAiName");
	
	// CRITICAL: If BotInfoAiName starts with "TownBot_", extract the actual bot name
	// BotInfoAiName format is "TownBot_merchant1", but we need "merchant1" for $BotInfo lookup
	if(String::findSubStr(%botName, "TownBot_") == 0)
	{
		%botName = String::getSubStr(%botName, 8, 999);  // Remove "TownBot_" prefix (8 characters)
	}
	
	// Fallback to Client::getName if BotInfoAiName is invalid
	if(%botName == "" || %botName == -1 || %botName == "0")
	{
		%botName = Client::getName(%id);
		// If Client::getName also fails, try player object name property
		if(%botName == "" || %botName == -1 || %botName == "0")
		{
			%playerObj = Client::getOwnedObject(%id);
			if(%playerObj != "" && %playerObj != -1)
				%botName = %playerObj.name;
		}
	}
	
	%info = $BotInfo[%botName, SHOP];	

	for(%i = 0; GetWord(%info, %i) != -1; %i++)
	{
		%a = GetWord(%info, %i);

		// VOID 2026-07-15: shop indexes are positive numbers (they start at 1).
		// A stray token coerces to 0 under this engine's ==, and items with NO
		// ShopIndex ("") compare equal to it - which listed every index-less
		// belt item (all 23 converted armors) at this merchant. Reject junk.
		if((%a * 1) < 1)
			continue;

		// First, check all regular ItemData items
		%max = getNumItems();
		for(%z = 0; %z < %max; %z++)
		{
			%item = getItemData(%z);

			if($AccessoryVar[%item, $ShopIndex] != "" && $AccessoryVar[%item, $ShopIndex] == %a)
			{
				Client::setItemShopping(%clientId, %item);
				Client::setItemBuying(%clientId, %item);

				// record for the Kronos shop push (HUD clients)
				%clientId.kshopIdx[%clientId.kshopCount] = %z;
				%clientId.kshopCount++;
			}
		}
		
		// Then, check all belt items (items without ItemData but registered in $BeltItem)
		// Skip Deployables category since those still use ItemData (like DepBasePack)
		// Iterate through belt categories: QuestItems, KeyItems, Consumables, Accessories
		for(%catIndex = 1; %catIndex <= 7; %catIndex++)
		{
			%category = $Belt::Categories[%catIndex];
			// Skip Deployables (category index 3) since those still use ItemData
			if(%category == "" || %category == -1 || %category == "Deployables")
				continue;
				
			%itemCount = $Belt::Count[%category];
			
			// Iterate through all registered items in this category
			for(%itemIndex = 0; %itemIndex < %itemCount; %itemIndex++)
			{
				%item = $BeltItem[%itemIndex, "Num", %category];
				
				// Check if this belt item has a matching ShopIndex
				// (VOID 2026-07-15: empty ShopIndex must never match - see the %a guard above)
				if(%item != "" && %item != -1 && $AccessoryVar[%item, $ShopIndex] != "" && $AccessoryVar[%item, $ShopIndex] == %a)
				{
					// Check if this item already has ItemData by searching through all ItemData indices
					// If it has ItemData, it was already added in the loop above, so skip it
					%hasItemData = False;
					for(%checkZ = 0; %checkZ < %max; %checkZ++)
					{
						%checkItem = getItemData(%checkZ);
						if(%checkItem == %item)
						{
							%hasItemData = True;
							break;
						}
					}
					
					// Only add belt items that don't have ItemData (to avoid duplicates)
					if(!%hasItemData)
					{
						Client::setItemShopping(%clientId, %item);
						Client::setItemBuying(%clientId, %item);

						// record for the Kronos shop push (HUD clients)
						%clientId.kshopBeltItem[%clientId.kshopBeltCnt] = %item;
						%clientId.kshopBeltCnt++;
					}
				}
			}
		}
	}

	// HUD clients: open the Kronos shop screen now that the stock
	// list is captured (vanilla clients are already in gui mode 4)
	if(%clientId.hasKronosHUD)
		KronosShop_Open(%clientId, "shop", Client::getName(%id));
}

function SetupBank(%clientId, %id)
{
	dbecho($dbechoMode, "SetupBank(" @ %clientId @ ", " @ %id @ ")");

	ClearCurrentShopVars(%clientId);
	%clientId.currentBank = %id;
	
	%msg = "<jc><f2>To Deposit/Withdraw 'bulk' Items, please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, 10);

	%clientId.bulkNum = "";

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	if(Client::getGuiMode(%clientId) != 4)
		Client::setGuiMode(%clientId, 4);

	%txt = "<f1><jc>COINS: " @ Number::Beautify(fetchData(%clientId, "COINS"), -3);
	Client::setInventoryText(%clientId, %txt);

	%info = fetchData(%clientId, "BankStorage");

	for(%i = 0; GetWord(%info, %i) != -1; %i+=2)
	{
		%item = GetWord(%info, %i);

		// VOID BANK 2026-07-14: skip zero/negative-count leftovers - they drew
		// ghost rows that answered every click with "You only have 0".
		if((GetWord(%info, %i + 1) * 1) <= 0)
			continue;

		Client::setItemShopping(%clientId, %item);
		Client::setItemBuying(%clientId, %item);
	}

	// VOID BANK 2026-07-14: belt storage rows (VSlotBank*) join the same screen -
	// "buying" one withdraws the mapped belt item (economy.cs dispatch ->
	// Belt::BankWithdraw). No-op unless $pref::VSlotsEnabled.
	VSlot::SyncBank(%clientId);
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

	%txt = "<f1><jc>COINS: " @ Number::Beautify(fetchData(%clientId, "COINS"), -3);
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

	// Stop the coin display when closing shop
	StopCoinsDisplay(%clientId);

	// Play Farewell when closing Shop/Bank/Smith GUIs
	if(%clientId.currentShop != "") TownBot_PlayFarewell(%clientId, %clientId.currentShop);
	if(%clientId.currentBank != "") TownBot_PlayFarewell(%clientId, %clientId.currentBank);
	if(%clientId.currentSmith != "") TownBot_PlayFarewell(%clientId, %clientId.currentSmith);

      %clientId.currentShop = "";
      %clientId.currentBank = "";
      %clientId.currentSmith = "";
	%clientId.currentInvSteal = "";
	%clientId.currentBeltBank = "";

	storeData(%clientId, "TempPack", "");
	storeData(%clientId, "TempSmith", "");
}

$AccessoryVar[BluePotion, $ShopIndex] = 1;
$AccessoryVar[CrystalBluePotion, $ShopIndex] = 2;
$AccessoryVar[EnergyVial, $ShopIndex] = 3;
$AccessoryVar[CrystalEnergyVial, $ShopIndex] = 4;
$AccessoryVar[ScoutVehicle, $ShopIndex] = 5;
$AccessoryVar[HeavenlyAntiMagicBelt, $ShopIndex] = 226;

$AccessoryVar[CheetaursPaws, $ShopIndex] = 33;
$AccessoryVar[BootsOfGliding, $ShopIndex] = 34;
$AccessoryVar[WindWalkers, $ShopIndex] = 35;

$AccessoryVar[Hatchet, $ShopIndex] = 39;
$AccessoryVar[Club, $ShopIndex] = 47;
$AccessoryVar[Knife, $ShopIndex] = 55;
$AccessoryVar[PickAxe, $ShopIndex] = 63;
$AccessoryVar[Tent, $ShopIndex] = 98;
$AccessoryVar[AdminOrb, $ShopIndex] = 99;
$AccessoryVar[OrbOfLight, $ShopIndex] = 103;

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
$AccessoryVar[HeavenlyPowerRing, $ShopIndex] = 224;
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
$AccessoryVar[RedDiamondKingShield, $ShopIndex] = 227;
$AccessoryVar[WhiteDiamondKingShield, $ShopIndex] = 228;
$AccessoryVar[RedDiamondHelmet, $ShopIndex] = 229;
$AccessoryVar[WhiteDiamondHelmet, $ShopIndex] = 230;
$AccessoryVar[ExtremeRegenerationNecklace, $ShopIndex] = 220;
$AccessoryVar[GodlyRegenerationNecklace, $ShopIndex] = 221;
$AccessoryVar[HeavenlyRegenerationNecklace, $ShopIndex] = 225;
$AccessoryVar[TerminusEst, $ShopIndex] = 222;
$AccessoryVar[WhiteDiamondPlate, $ShopIndex] = 223;
$AccessoryVar[AecoSeorei, $ShopIndex] = 231;
$AccessoryVar[MorningStar, $ShopIndex] = 232;
$AccessoryVar[WhiteDiamondVoidCutter, $ShopIndex] = 233;
$AccessoryVar[WhiteDiamondVoidCrusher, $ShopIndex] = 234;
$AccessoryVar[WhiteDiamondVoidImpaler, $ShopIndex] = 235;
$AccessoryVar[FinalVerdict, $ShopIndex] = 236;
$AccessoryVar[StormCaller, $ShopIndex] = 237;
$AccessoryVar[WorldSplitter, $ShopIndex] = 238;
$AccessoryVar[JudgementRobe, $ShopIndex] = 239;
$AccessoryVar[StormRobe, $ShopIndex] = 240;
$AccessoryVar[VoidRobe, $ShopIndex] = 241;
$AccessoryVar[SoulReaver, $ShopIndex] = 242;
$AccessoryVar[SkyRender, $ShopIndex] = 243;
$AccessoryVar[EchoFang, $ShopIndex] = 244;
$AccessoryVar[Test, $ShopIndex] = 245;


// ============================================
// Merchant Definitions
// ============================================
// NOTE: All merchant shop lists are now defined in the map file (KingdomKronos.mis)
// Merchants are defined in the "TownBots" SimGroup with their SHOP field containing shop indices
// This prevents conflicts and keeps all merchant data in one place

// ===== KOKBATCH REMOVED 2026-07-14 ===== (shop entries for the stripped weapons; see newstuff.cs)
