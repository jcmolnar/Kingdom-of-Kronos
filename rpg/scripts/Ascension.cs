// Ascension.cs - Talent system for sacrificing remorts/SP for permanent abilities
// Players can unlock powerful talents by spending remort levels or skill points

//=============================================================================
// TALENT DEFINITIONS
//=============================================================================

// Remort-cost talents
$AscensionTalent[DualWielding, Name] = "Dual Wielding";
$AscensionTalent[DualWielding, Cost] = 100;
$AscensionTalent[DualWielding, CostType] = "remort";
$AscensionTalent[DualWielding, MinRemort] = 0;
$AscensionTalent[DualWielding, Desc] = "Equip weapons in both hands with 100% effectiveness";

$AscensionTalent[ExpAffinity1, Name] = "Experience Affinity I";
$AscensionTalent[ExpAffinity1, Cost] = 25;
$AscensionTalent[ExpAffinity1, CostType] = "remort";
$AscensionTalent[ExpAffinity1, MinRemort] = 0;
$AscensionTalent[ExpAffinity1, Desc] = "+25% experience from kills (multiplicative)";

$AscensionTalent[ExpAffinity2, Name] = "Experience Affinity II";
$AscensionTalent[ExpAffinity2, Cost] = 50;
$AscensionTalent[ExpAffinity2, CostType] = "remort";
$AscensionTalent[ExpAffinity2, MinRemort] = 0;
$AscensionTalent[ExpAffinity2, Desc] = "+50% experience from kills (stacks with I for +87.5% total)";
$AscensionTalent[ExpAffinity2, Requires] = "ExpAffinity1";

$AscensionTalent[ExpAffinity3, Name] = "Experience Affinity III";
$AscensionTalent[ExpAffinity3, Cost] = 100;
$AscensionTalent[ExpAffinity3, CostType] = "remort";
$AscensionTalent[ExpAffinity3, MinRemort] = 0;
$AscensionTalent[ExpAffinity3, Desc] = "+100% experience from kills (stacks for +275% total)";
$AscensionTalent[ExpAffinity3, Requires] = "ExpAffinity2";

$AscensionTalent[GoldDigger, Name] = "Gold Digger";
$AscensionTalent[GoldDigger, Cost] = 25;
$AscensionTalent[GoldDigger, CostType] = "remort";
$AscensionTalent[GoldDigger, MinRemort] = 0;
$AscensionTalent[GoldDigger, Desc] = "+25% coin drops from enemies";

$AscensionTalent[BerserkerRage, Name] = "Berserker's Rage";
$AscensionTalent[BerserkerRage, Cost] = 25;
$AscensionTalent[BerserkerRage, CostType] = "remort";
$AscensionTalent[BerserkerRage, MinRemort] = 0;
$AscensionTalent[BerserkerRage, Desc] = "+30% damage when HP is below 25%";

$AscensionTalent[SpellEcho, Name] = "Spell Echo";
$AscensionTalent[SpellEcho, Cost] = 50;
$AscensionTalent[SpellEcho, CostType] = "remort";
$AscensionTalent[SpellEcho, MinRemort] = 0;
$AscensionTalent[SpellEcho, Desc] = "15% chance for spells to cast twice";

$AscensionTalent[Telekinesis, Name] = "Telekinesis";
$AscensionTalent[Telekinesis, Cost] = 30;
$AscensionTalent[Telekinesis, CostType] = "remort";
$AscensionTalent[Telekinesis, MinRemort] = 0;
$AscensionTalent[Telekinesis, Desc] = "Auto-pickup loot within 5 unit radius";

// SP-cost talents
$AscensionTalent[IronSkin, Name] = "Iron Skin";
$AscensionTalent[IronSkin, Cost] = 250000;
$AscensionTalent[IronSkin, CostType] = "sp";
$AscensionTalent[IronSkin, MinRemort] = 25;
$AscensionTalent[IronSkin, Desc] = "+15% damage reduction from all sources";

$AscensionTalent[DodgeMastery, Name] = "Dodge Mastery";
$AscensionTalent[DodgeMastery, Cost] = 250000;
$AscensionTalent[DodgeMastery, CostType] = "sp";
$AscensionTalent[DodgeMastery, MinRemort] = 25;
$AscensionTalent[DodgeMastery, Desc] = "Permanent 10% chance to dodge all damage";

// Talent list for iteration
$AscensionTalentList = "DualWielding ExpAffinity1 ExpAffinity2 ExpAffinity3 GoldDigger BerserkerRage SpellEcho Telekinesis IronSkin DodgeMastery";

//=============================================================================
// CORE FUNCTIONS
//=============================================================================

function Ascension::HasTalent(%clientId, %talentName)
{
	// SAFEGUARD: Bots are never allowed to have Ascension talents
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		return false;
		
	// Check if player has unlocked a specific talent
	%talents = fetchData(%clientId, "AscensionTalents");
	if(%talents == "" || %talents == -1 || %talents == "0")
		return false;
	
	// Search for talent in space-separated list
	if(String::findSubStr(%talents, %talentName) >= 0)
		return true;
	
	return false;
}

function Ascension::UnlockTalent(%clientId, %talentName)
{
	// Unlock a talent for a player after payment
	
	// SAFEGUARD: Bots cannot unlock talents
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		return;
		
	%talents = fetchData(%clientId, "AscensionTalents");
	if(%talents == "" || %talents == -1 || %talents == "0")
		%talents = "";
	
	// Add talent to list
	if(String::findSubStr(%talents, %talentName) == -1)
	{
		if(%talents == "")
			%talents = %talentName;
		else
			%talents = %talents @ " " @ %talentName;
		
		storeData(%clientId, "AscensionTalents", %talents);
		return true;
	}
	
	return false;
}

function Ascension::CanAfford(%clientId, %talentName)
{
	// Check if player can afford the talent
	%cost = $AscensionTalent[%talentName, Cost];
	%costType = $AscensionTalent[%talentName, CostType];
	%minRemort = $AscensionTalent[%talentName, MinRemort];
	
	// Check minimum remort requirement
	%currentRemort = fetchData(%clientId, "RemortStep");
	if(%currentRemort < %minRemort)
		return false;
	
	// Check cost
	if(%costType == "remort")
	{
		if(%currentRemort >= %cost)
			return true;
	}
	else if(%costType == "sp")
	{
		%currentSP = fetchData(%clientId, "SPcredits");
		if(%currentSP >= %cost)
			return true;
	}
	
	return false;
}

function Ascension::Purchase(%clientId, %talentName)
{
	// Purchase a talent (deduct cost and unlock)
	
	// Check if already owned
	if(Ascension::HasTalent(%clientId, %talentName))
	{
		Client::sendMessage(%clientId, 0, "You already have this talent!");
		return false;
	}
	
	// Check prerequisites
	%requires = $AscensionTalent[%talentName, Requires];
	if(%requires != "" && %requires != -1)
	{
		if(!Ascension::HasTalent(%clientId, %requires))
		{
			%reqName = $AscensionTalent[%requires, Name];
			Client::sendMessage(%clientId, 0, "You must unlock " @ %reqName @ " first!");
			return false;
		}
	}
	
	// Check if can afford
	if(!Ascension::CanAfford(%clientId, %talentName))
	{
		%costType = $AscensionTalent[%talentName, CostType];
		%cost = $AscensionTalent[%talentName, Cost];
		
		if(%costType == "remort")
			Client::sendMessage(%clientId, 0, "You need " @ %cost @ " remorts to unlock this!");
		else
			Client::sendMessage(%clientId, 0, "You need " @ %cost @ " SP to unlock this!");
		
		return false;
	}
	
	// Deduct cost
	%cost = $AscensionTalent[%talentName, Cost];
	%costType = $AscensionTalent[%talentName, CostType];
	
	if(%costType == "remort")
	{
		%currentRemort = fetchData(%clientId, "RemortStep");
		%newRemort = %currentRemort - %cost;
		storeData(%clientId, "RemortStep", %newRemort);
		
		// Update max level based on new remort
		%maxLevel = 125 + (%newRemort * 8);
		Client::sendMessage(%clientId, 0, "You sacrificed " @ %cost @ " remorts! (Now Remort " @ %newRemort @ ")");
	}
	else if(%costType == "sp")
	{
		storeData(%clientId, "SPcredits", %cost, "dec");
		Client::sendMessage(%clientId, 0, "You spent " @ %cost @ " SP!");
	}
	
	// Unlock talent
	Ascension::UnlockTalent(%clientId, %talentName);
	
	// NOTE: Caller (processMenuConfirmAscension) shows the success message

	
	// Special instructions for specific talents
	if(%talentName == "DualWield")
	{
		Client::sendMessage(%clientId, 0, "Use #dualwield to toggle dual wielding mode.");
		Client::sendMessage(%clientId, 0, "Equip a weapon normally, then equip another to hold it in your off-hand.");
	}
	
	// Refresh player stats
	RefreshAll(%clientId, "true");
	
	return true;
}

function Ascension::GetExpMultiplier(%clientId)
{
	// Calculate total EXP multiplier from all Affinity talents
	%multiplier = 1.0;
	
	if(Ascension::HasTalent(%clientId, "ExpAffinity1"))
		%multiplier *= 1.25;
	
	if(Ascension::HasTalent(%clientId, "ExpAffinity2"))
		%multiplier *= 1.50;
	
	if(Ascension::HasTalent(%clientId, "ExpAffinity3"))
		%multiplier *= 2.00;
	
	return %multiplier;
}

//=============================================================================
// TELEKINESIS AUTO-LOOT SYSTEM
//=============================================================================
// Periodically scans for lootbags within 50m and auto-picks them up
// Uses existing loot pickup logic to prevent duplication
// Respects lootbag permissions (namelist)
// Does NOT interfere with lootbag aggregation

$TelekinesisRadius = 5;           // 5 game units - close-range magic auto-loot
$TelekinesisScanInterval = 10;    // 10 seconds between scans (aggregation uses 30s)
$TelekinesisMaxPerPass = 5;       // Max bags to process per scan (prevents freeze)

function Ascension::StartTelekinesisLoop()
{
	// Start the periodic scan for all players
	schedule("Ascension::TelekinesisScanAll();", $TelekinesisScanInterval);
}

function Ascension::TelekinesisScanAll()
{
	// Scan LootbagGroup for bags, find eligible players, use round-robin for fairness
	if(!isObject("LootbagGroup"))
	{
		schedule("Ascension::TelekinesisScanAll();", $TelekinesisScanInterval);
		return;
	}
	
	%count = Group::objectCount(LootbagGroup);
	%processed = 0;
	
	for(%i = 0; %i < %count && %processed < $TelekinesisMaxPerPass; %i++)
	{
		%bag = Group::getObject(LootbagGroup, %i);
		
		if(!isObject(%bag))
			continue;
		
		// CRITICAL SAFEGUARD: Skip Player objects (same as aggregation)
		if(getObjectType(%bag) == "Player")
			continue;
		
		// Skip if bag is being processed
		if($TelekinesisProcessing[%bag])
			continue;
		
		%lootData = $loot[%bag];
		if(%lootData == "" || %lootData == -1)
			continue;
		
		%bagPos = GameBase::getPosition(%bag);
		%ownerName = GetWord(%lootData, 0);
		%namelist = GetWord(%lootData, 1);
		
		// Collect all eligible players in range
		%eligibleList = "";
		%eligibleCount = 0;
		
		for(%id = Client::getFirst(); %id != -1; %id = Client::getNext(%id))
		{
			if(!Ascension::HasTalent(%id, "Telekinesis") || IsDead(%id))
				continue;
			
			%playerObj = Client::getOwnedObject(%id);
			if(%playerObj == -1 || %playerObj == "")
				continue;
			
			%playerPos = GameBase::getPosition(%playerObj);
			%dist = Vector::getDistance(%playerPos, %bagPos);
			
			if(%dist > $TelekinesisRadius)
				continue;
			
			// Check eligibility: own bag, public loot, or bot-killed loot on namelist
			%playerName = Client::getName(%id);
			%eligible = false;
			
			if(String::ICompare(%ownerName, %playerName) == 0)
				%eligible = true;
			else if(%ownerName == "*")
				%eligible = true;
			else if(IsLootOwnerBot(%ownerName) && IsInCommaList(%namelist, %playerName))
				%eligible = true;
			
			if(%eligible)
			{
				%eligibleList = %eligibleList @ %id @ " ";
				%eligibleCount++;
			}
		}
		
		// Round-robin: pick someone who wasn't the last recipient if possible
		%recipient = -1;
		
		if(%eligibleCount == 1)
		{
			%recipient = GetWord(%eligibleList, 0);
		}
		else if(%eligibleCount > 1)
		{
			// First try to find someone who wasn't last recipient
			for(%j = 0; %j < %eligibleCount; %j++)
			{
				%candidate = GetWord(%eligibleList, %j);
				if(%candidate != $TelekinesisLastRecipient)
				{
					%recipient = %candidate;
					break;
				}
			}
			// If all were last recipient (shouldn't happen), just pick first
			if(%recipient == -1)
				%recipient = GetWord(%eligibleList, 0);
		}
		
		// Give loot to recipient
		if(%recipient != -1)
		{
			$TelekinesisLastRecipient = %recipient;
			Ascension::TelekinesisPickup(%recipient, %bag);
			%processed++;
		}
	}
	
	// Schedule next scan
	schedule("Ascension::TelekinesisScanAll();", $TelekinesisScanInterval);
}

function Ascension::TelekinesisPickup(%clientId, %bag)
{
	// Pickup a specific lootbag for a specific player
	if(!isObject(%bag))
		return;
	
	// Mark bag as being processed
	$TelekinesisProcessing[%bag] = true;
	
	%lootData = $loot[%bag];
	if(%lootData == "" || %lootData == -1)
	{
		$TelekinesisProcessing[%bag] = "";
		return;
	}
	
	%ownerName = GetWord(%lootData, 0);
	%namelist = GetWord(%lootData, 1);
	
	// Extract items from loot string (skip owner and namelist)
	%newloot = String::getSubStr(%lootData, String::len(%ownerName)+String::len(%namelist)+2, 99999);
	
	// Give items to player
	if(%newloot != "")
	{
		if(%ownerName == "*" || IsLootOwnerBot(%ownerName))
			Client::sendMessage(%clientId, 0, "Telekinesis: Collected loot.");
		else
			Client::sendMessage(%clientId, 0, "Telekinesis: Recovered your backpack.");
		
		GiveThisStuff(%clientId, %newloot, true);
		
		// Clean up loot data
		$loot[%bag] = "";
		
		// Update owner's lootbag list (only if player-owned)
		if(%ownerName != "*" && !IsLootOwnerBot(%ownerName))
		{
			%ownerId = NEWgetClientByName(%ownerName);
			if(%ownerId != -1)
				storeData(%ownerId, "lootbaglist", RemoveFromCommaList(fetchData(%ownerId, "lootbaglist"), %bag));
		}
		
		// Delete the bag safely
		if(isObject(%bag))
			deleteObject(%bag);
	}
	
	// Clear processing flag
	$TelekinesisProcessing[%bag] = "";
}

//=============================================================================
// ASCENSION SHOP MENU SYSTEM
//=============================================================================

// Items per page (leave room for Next/Back options)
$AscensionShopPerPage = 6;

function SetupAscensionShop(%clientId, %botId, %page)
{
	// Open the Ascension talent shop menu
	dbecho($dbechoMode, "SetupAscensionShop(" @ %clientId @ ", " @ %botId @ ", " @ %page @ ")");
	
	// SAFEGUARD: Bots cannot use Ascension shop
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		return;
	
	// Default to page 0
	if(%page == "" || %page == -1)
		%page = 0;
	
	// Store state for pagination and selection
	%clientId.currentAscensionTrainer = %botId;
	%clientId.ascensionShopPage = %page;
	
	// Get player resources
	%currentRemort = fetchData(%clientId, "RemortStep");
	if(%currentRemort == "" || %currentRemort == -1)
		%currentRemort = 0;
	%currentSP = fetchData(%clientId, "SPcredits");
	if(%currentSP == "" || %currentSP == -1)
		%currentSP = 0;
	
	// First pass: build list of unowned talents
	%talentCount = GetWordCount($AscensionTalentList);
	%unownedList = "";
	%unownedCount = 0;
	
	for(%i = 0; %i < %talentCount; %i++)
	{
		%talent = GetWord($AscensionTalentList, %i);
		
		// Skip if already owned
		if(Ascension::HasTalent(%clientId, %talent))
			continue;
		
		%unownedList = %unownedList @ %talent @ " ";
		%unownedCount++;
	}
	
	// Calculate pagination
	%totalPages = floor((%unownedCount - 1) / $AscensionShopPerPage) + 1;
	if(%unownedCount == 0)
		%totalPages = 1;
	if(%page >= %totalPages)
		%page = %totalPages - 1;
	if(%page < 0)
		%page = 0;
	
	%startIndex = %page * $AscensionShopPerPage;
	%endIndex = %startIndex + $AscensionShopPerPage;
	
	// Build the menu header
	%header = "Talents P" @ (%page + 1) @ "/" @ %totalPages @ " (R:" @ %currentRemort @ " SP:" @ %currentSP @ ")";
	Client::buildMenu(%clientId, %header, "SelectAscension", true);
	
	// Add talents for this page
	%displayNum = 1;
	for(%i = %startIndex; %i < %endIndex && %i < %unownedCount; %i++)
	{
		%talent = GetWord(%unownedList, %i);
		%name = $AscensionTalent[%talent, Name];
		%cost = $AscensionTalent[%talent, Cost];
		%costType = $AscensionTalent[%talent, CostType];
		%requires = $AscensionTalent[%talent, Requires];
		
		// Check prerequisites
		%hasPrereq = true;
		if(%requires != "" && %requires != -1)
		{
			if(!Ascension::HasTalent(%clientId, %requires))
				%hasPrereq = false;
		}
		
		// Build cost string
		if(%costType == "remort")
			%costStr = %cost @ "R";
		else
			%costStr = %cost @ "SP";
		
		// Check affordability
		%canAfford = Ascension::CanAfford(%clientId, %talent);
		
		// Build menu item text with number prefix for clarity
		if(!%hasPrereq)
			%menuText = %displayNum @ ". [LOCK] " @ %name @ " (" @ %costStr @ ")";
		else if(!%canAfford)
			%menuText = %displayNum @ ". [---] " @ %name @ " (" @ %costStr @ ")";
		else
			%menuText = %displayNum @ ". " @ %name @ " (" @ %costStr @ ")";
		
		// Store talent mapping for this slot (use numeric code)
		$AscensionMenuSlot[%clientId, %displayNum] = %talent;
		
		Client::addMenuItem(%clientId, %menuText, %displayNum);
		%displayNum++;
	}
	
	if(%unownedCount == 0)
	{
		Client::addMenuItem(%clientId, "All talents owned!", "0");
	}
	
	// Add navigation options
	if(%page > 0)
		Client::addMenuItem(%clientId, "<< Back", "prev");
	if(%page < %totalPages - 1)
		Client::addMenuItem(%clientId, "Next >>", "next");
}

function processMenuSelectAscension(%clientId, %code)
{
	// Handle talent selection from the Ascension shop menu
	dbecho($dbechoMode, "processMenuSelectAscension(" @ %clientId @ ", " @ %code @ ")");
	
	// Handle pagination navigation
	if(%code == "next")
	{
		%page = %clientId.ascensionShopPage;
		if(%page == "" || %page == -1)
			%page = 0;
		SetupAscensionShop(%clientId, %clientId.currentAscensionTrainer, %page + 1);
		return;
	}
	else if(%code == "prev")
	{
		%page = %clientId.ascensionShopPage;
		if(%page == "" || %page == -1)
			%page = 0;
		SetupAscensionShop(%clientId, %clientId.currentAscensionTrainer, %page - 1);
		return;
	}
	
	// Handle "all owned" or cancel
	if(%code == "0" || %code == "none" || %code == "" || %code == -1)
	{
		// Clear trainer reference
		%clientId.currentAscensionTrainer = "";
		%clientId.ascensionShopPage = "";
		return;
	}
	
	// Numeric code - look up talent from mapping
	%talent = $AscensionMenuSlot[%clientId, %code];
	if(%talent == "" || %talent == -1)
	{
		Client::sendMessage(%clientId, $MsgRed, "Invalid selection.");
		%clientId.currentAscensionTrainer = "";
		return;
	}
	
	%name = $AscensionTalent[%talent, Name];
	if(%name == "" || %name == -1)
	{
		Client::sendMessage(%clientId, $MsgRed, "Invalid talent.");
		%clientId.currentAscensionTrainer = "";
		return;
	}
	
	// Store selected talent for confirmation
	%clientId.selectedAscensionTalent = %talent;

	
	// Show talent info
	%cost = $AscensionTalent[%talent, Cost];
	%costType = $AscensionTalent[%talent, CostType];
	%desc = $AscensionTalent[%talent, Desc];
	%requires = $AscensionTalent[%talent, Requires];
	%minRemort = $AscensionTalent[%talent, MinRemort];
	
	// Build cost string
	if(%costType == "remort")
		%costStr = %cost @ " Remorts";
	else
		%costStr = %cost @ " SP";
	
	// Check prerequisites
	%hasPrereq = true;
	%prereqName = "";
	if(%requires != "" && %requires != -1)
	{
		if(!Ascension::HasTalent(%clientId, %requires))
		{
			%hasPrereq = false;
			%prereqName = $AscensionTalent[%requires, Name];
		}
	}
	
	// Check affordability
	%canAfford = Ascension::CanAfford(%clientId, %talent);
	
	// Already owned?
	if(Ascension::HasTalent(%clientId, %talent))
	{
		Client::sendMessage(%clientId, $MsgYellow, "You already have " @ %name @ "!");
		%clientId.currentAscensionTrainer = "";
		return;
	}
	
	// Store selected talent for confirmation
	%clientId.selectedAscensionTalent = %talent;
	
	// Always show talent info
	Client::sendMessage(%clientId, $MsgYellow, "=== " @ %name @ " ===");
	Client::sendMessage(%clientId, $MsgWhite, "Cost: " @ %costStr);
	Client::sendMessage(%clientId, $MsgBeige, %desc);
	if(%minRemort > 0)
		Client::sendMessage(%clientId, $MsgWhite, "Requires Remort " @ %minRemort @ "+");
	
	// Build menu based on whether can purchase
	if(!%hasPrereq)
	{
		// Missing prerequisite - show info only
		Client::sendMessage(%clientId, $MsgRed, "LOCKED - Requires: " @ %prereqName);
		Client::buildMenu(%clientId, %name @ " [LOCKED]", "ConfirmAscension", true);
		Client::addMenuItem(%clientId, "<< Back to list", "back");
	}
	else if(!%canAfford)
	{
		// Can't afford - show info only
		Client::sendMessage(%clientId, $MsgRed, "Not enough " @ %costType @ "!");
		Client::buildMenu(%clientId, %name @ " [Cannot Afford]", "ConfirmAscension", true);
		Client::addMenuItem(%clientId, "<< Back to list", "back");
	}
	else
	{
		// Can purchase - show confirmation
		Client::buildMenu(%clientId, "Purchase " @ %name @ "?", "ConfirmAscension", true);
		Client::addMenuItem(%clientId, "YES - Spend " @ %costStr, "confirm");
		Client::addMenuItem(%clientId, "NO - Cancel", "cancel");
		Client::addMenuItem(%clientId, "<< Back to list", "back");
	}
}

function processMenuConfirmAscension(%clientId, %code)
{
	// Handle confirmation of talent purchase
	dbecho($dbechoMode, "processMenuConfirmAscension(" @ %clientId @ ", " @ %code @ ")");
	
	%talent = %clientId.selectedAscensionTalent;
	
	// Handle Back - return to talent list
	if(%code == "back")
	{
		%clientId.selectedAscensionTalent = "";
		%page = %clientId.ascensionShopPage;
		if(%page == "" || %page == -1)
			%page = 0;
		SetupAscensionShop(%clientId, %clientId.currentAscensionTrainer, %page);
		return;
	}
	
	if(%code == "confirm" && %talent != "" && %talent != -1)
	{
		// Attempt purchase
		%success = Ascension::Purchase(%clientId, %talent);
		
		if(%success)
		{
			%name = $AscensionTalent[%talent, Name];
			Client::sendMessage(%clientId, $MsgGreen, "You have unlocked " @ %name @ "!");
			playSound(SoundSpawn2, GameBase::getPosition(Client::getOwnedObject(%clientId)));
		}
	}
	else
	{
		Client::sendMessage(%clientId, $MsgWhite, "Purchase cancelled.");
	}
	
	// Clear stored data
	%clientId.selectedAscensionTalent = "";
	%clientId.currentAscensionTrainer = "";
}

// Short alias for #ascend shop command
function Ascension::ShowShop(%clientId)
{
	SetupAscensionShop(%clientId, "", 0);
}

echo("[ASCENSION] Ascension system loaded - " @ GetWordCount($AscensionTalentList) @ " talents available");

// Start Telekinesis loop after server initialization
schedule("Ascension::StartTelekinesisLoop();", 30);

