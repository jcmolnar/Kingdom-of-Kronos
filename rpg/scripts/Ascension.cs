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
$AscensionTalent[Telekinesis, Desc] = "Auto-pickup loot within 10 unit radius";

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

// Robe unlock talents (dynamic cost based on owned count: 25/10/5)
$AscensionTalent[JudgementRobeTalent, Name] = "Judgement Robe";
$AscensionTalent[JudgementRobeTalent, Cost] = 25;
$AscensionTalent[JudgementRobeTalent, CostType] = "remort";
$AscensionTalent[JudgementRobeTalent, MinRemort] = 125;
$AscensionTalent[JudgementRobeTalent, Desc] = "Unlock Judgement Robe (25% reflect 10% damage) for purchase at Giovanni's shop";
$AscensionTalent[JudgementRobeTalent, DynamicCost] = "robe";

$AscensionTalent[StormRobeTalent, Name] = "Storm Robe";
$AscensionTalent[StormRobeTalent, Cost] = 25;
$AscensionTalent[StormRobeTalent, CostType] = "remort";
$AscensionTalent[StormRobeTalent, MinRemort] = 110;
$AscensionTalent[StormRobeTalent, Desc] = "Unlock Storm Robe (25% zap for 500 damage) for purchase at Giovanni's shop";
$AscensionTalent[StormRobeTalent, DynamicCost] = "robe";

$AscensionTalent[VoidRobeTalent, Name] = "Void Robe";
$AscensionTalent[VoidRobeTalent, Cost] = 25;
$AscensionTalent[VoidRobeTalent, CostType] = "remort";
$AscensionTalent[VoidRobeTalent, MinRemort] = 105;
$AscensionTalent[VoidRobeTalent, Desc] = "Unlock Void Robe (5% phase shift dodge) for purchase at Giovanni's shop";
$AscensionTalent[VoidRobeTalent, DynamicCost] = "robe";

// Talent list for iteration
$AscensionTalentList = "DualWielding ExpAffinity1 ExpAffinity2 ExpAffinity3 GoldDigger BerserkerRage SpellEcho Telekinesis IronSkin DodgeMastery JudgementRobeTalent StormRobeTalent VoidRobeTalent";

//=============================================================================
// CORE FUNCTIONS
//=============================================================================

function Ascension::CanonicalTalentId(%talentName)
{
	// Backward compatibility alias:
	// Old code uses "DualWield", canonical talent id is "DualWielding".
	if(String::ICompare(%talentName, "DualWield") == 0)
		return "DualWielding";
	
	return %talentName;
}

function Ascension::HasTalent(%clientId, %talentName)
{
	// SAFEGUARD: Bots are never allowed to have Ascension talents
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		return false;
		
	// Check if player has unlocked a specific talent
	%talents = fetchData(%clientId, "AscensionTalents");
	if(%talents == "" || %talents == -1 || %talents == "0")
		return false;
	
	%canonicalTarget = Ascension::CanonicalTalentId(%talentName);
	%hasTalent = false;
	
	// Exact token scan to avoid substring collisions (e.g., Name vs Name2).
	for(%i = 0; (%ownedTalent = GetWord(%talents, %i)) != -1; %i++)
	{
		if(%ownedTalent == "" || %ownedTalent == "0")
			continue;
		
		%canonicalOwned = Ascension::CanonicalTalentId(%ownedTalent);
		if(String::ICompare(%canonicalOwned, %canonicalTarget) == 0)
		{
			%hasTalent = true;
			break;
		}
	}
	
	if(%hasTalent)
	{
		// Lazy bootstrap: if Telekinesis is owned but scan loop is not running,
		// schedule it immediately. This recovers from missed startup scheduling.
		if(%canonicalTarget == "Telekinesis")
		{
			if($TelekinesisScanToken == "" || $TelekinesisScanToken == -1)
				Ascension::ScheduleTelekinesisScan(1);
		}
		return true;
	}
	
	return false;
}

function Ascension::UnlockTalent(%clientId, %talentName)
{
	// Unlock a talent for a player after payment
	
	// SAFEGUARD: Bots cannot unlock talents
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		return;
	
	%talentName = Ascension::CanonicalTalentId(%talentName);
		
	%talents = fetchData(%clientId, "AscensionTalents");
	if(%talents == "" || %talents == -1 || %talents == "0")
		%talents = "";
	
	// Add talent to list if not already there
	if(!Ascension::HasTalent(%clientId, %talentName))
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

function Ascension::GetRobeTalentCount(%clientId)
{
	// Count how many robe talents this player has unlocked
	%count = 0;
	if(Ascension::HasTalent(%clientId, "JudgementRobeTalent")) %count++;
	if(Ascension::HasTalent(%clientId, "StormRobeTalent")) %count++;
	if(Ascension::HasTalent(%clientId, "VoidRobeTalent")) %count++;
	return %count;
}

function Ascension::GetRobeCost(%clientId)
{
	// Get dynamic cost for next robe talent based on how many already owned
	%count = Ascension::GetRobeTalentCount(%clientId);
	if(%count == 0) return 25;  // First robe: 25 RL
	if(%count == 1) return 10;  // Second robe: 10 RL
	return 5;                    // Third robe: 5 RL
}

function Ascension::GetRobeMinRemort(%clientId)
{
	// Get minimum remort required to buy a robe talent
	// After spending RL, player must still have 100 RL to equip
	%count = Ascension::GetRobeTalentCount(%clientId);
	if(%count == 0) return 125;  // 125 - 25 = 100
	if(%count == 1) return 110;  // 110 - 10 = 100
	return 105;                   // 105 - 5 = 100
}

function Ascension::CanAfford(%clientId, %talentName)
{
	// Check if player can afford the talent
	%talentName = Ascension::CanonicalTalentId(%talentName);
	
	%cost = $AscensionTalent[%talentName, Cost];
	%costType = $AscensionTalent[%talentName, CostType];
	%minRemort = $AscensionTalent[%talentName, MinRemort];
	
	// Handle dynamic robe talent costs
	if($AscensionTalent[%talentName, DynamicCost] == "robe")
	{
		%cost = Ascension::GetRobeCost(%clientId);
		%minRemort = Ascension::GetRobeMinRemort(%clientId);
	}
	
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
	%talentName = Ascension::CanonicalTalentId(%talentName);
	
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
		
		// Handle dynamic robe costs in error message
		if($AscensionTalent[%talentName, DynamicCost] == "robe")
			%cost = Ascension::GetRobeCost(%clientId);
		
		if(%costType == "remort")
			Client::sendMessage(%clientId, 0, "You need " @ %cost @ " remorts to unlock this!");
		else
			Client::sendMessage(%clientId, 0, "You need " @ %cost @ " SP to unlock this!");
		
		return false;
	}
	
	// Deduct cost
	%cost = $AscensionTalent[%talentName, Cost];
	%costType = $AscensionTalent[%talentName, CostType];
	
	// Handle dynamic robe costs
	if($AscensionTalent[%talentName, DynamicCost] == "robe")
		%cost = Ascension::GetRobeCost(%clientId);
	
	if(%costType == "remort")
	{
		%currentRemort = fetchData(%clientId, "RemortStep");
		%newRemort = %currentRemort - %cost;
		storeData(%clientId, "RemortStep", %newRemort);

		// DESIGN DECISION (intentional, do not "fix"): the player's current LVL is
		// deliberately NOT clamped to the new remort's max level (125 + remort*8).
		// They sacrificed a large amount of progression time for this talent - let
		// them keep the level; they'll remort again soon anyway. Being over-level
		// for the remort is an accepted transient state.
		Client::sendMessage(%clientId, 0, "You sacrificed " @ %cost @ " remorts! (Now Remort " @ %newRemort @ ")");
	}
	else if(%costType == "sp")
	{
		storeData(%clientId, "SPcredits", %cost, "dec");
		Client::sendMessage(%clientId, 0, "You spent " @ %cost @ " SP!");
	}
	
	// Unlock talent
	Ascension::UnlockTalent(%clientId, %talentName);
	
	// Wake Telekinesis scan immediately after unlock (safe: single scheduler handle).
	if(%talentName == "Telekinesis")
		Ascension::ScheduleTelekinesisScan(1);
	
	// NOTE: Caller (processMenuConfirmAscension) shows the success message

	
	// Special instructions for specific talents
	if(Ascension::CanonicalTalentId(%talentName) == "DualWielding")
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
// Periodically scans for lootbags and auto-picks them up
// Uses existing loot pickup logic to prevent duplication
// Respects lootbag permissions (namelist)
// Does NOT interfere with lootbag aggregation

$TelekinesisRadius = 10;          // 10 game units - close-range magic auto-loot
$TelekinesisScanInterval = 10;    // 10 seconds between scans (aggregation uses 30s)
$TelekinesisNoBagInterval = 20;   // Slow down polling when no lootbags are found
$TelekinesisMaxPerPass = 5;       // Max bags to process per scan (prevents freeze)
$TelekinesisLootScanBudget = 120; // Max LootbagGroup objects inspected per scan
$TelekinesisIdleInterval = 60;    // Check less frequently when no active Telekinesis players
$TelekinesisMissionSyncInterval = 60; // Fallback MissionCleanup sync cadence
$TelekinesisMissionScanBudget = 180;  // Max MissionCleanup objects inspected per sync

function Ascension::StartTelekinesisLoop()
{
	// Start the periodic scan for all players
	Ascension::ScheduleTelekinesisScan($TelekinesisScanInterval);
}

function Ascension::ScheduleTelekinesisScan(%delay)
{
	// Tribes 1 has no cancel(). Use a generation token so only the latest
	// scheduled callback is allowed to run.
	if(%delay == "" || %delay <= 0)
		%delay = 1;
	
	if($TelekinesisScanToken == "" || $TelekinesisScanToken == -1)
		$TelekinesisScanToken = 0;
	
	$TelekinesisScanToken++;
	%token = $TelekinesisScanToken;
	schedule("Ascension::TelekinesisScanAll(" @ %token @ ");", %delay);
}

function Ascension::EnsureTelekinesisLoopForClient(%clientId)
{
	// Start/wake Telekinesis scan loop when a real player with the talent joins/spawns.
	if(%clientId == "" || %clientId == -1)
		return false;
	
	// Never treat bots as Telekinesis owners.
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		return false;
	
	if(!Ascension::HasTalent(%clientId, "Telekinesis"))
		return false;
	
	// Always wake quickly on join/spawn. Token guard keeps this safe.
	Ascension::ScheduleTelekinesisScan(1);
	return true;
}

function Ascension::TelekinesisScanAll(%token)
{
	// Ignore stale callbacks from older schedules.
	if(%token != $TelekinesisScanToken)
		return;
	
	// Build active Telekinesis player list first.
	// If no connected/active players have the talent, skip loot scanning work.
	%activeTeleList = "";
	%activeTeleCount = 0;
	for(%id = Client::getFirst(); %id != -1; %id = Client::getNext(%id))
	{
		if(!Ascension::HasTalent(%id, "Telekinesis") || IsDead(%id))
			continue;
		
		%playerObj = Client::getOwnedObject(%id);
		if(%playerObj == -1 || %playerObj == "")
			continue;
		
		%activeTeleList = %activeTeleList @ %id @ " ";
		%activeTeleCount++;
	}
	
	if(%activeTeleCount <= 0)
	{
		Ascension::ScheduleTelekinesisScan($TelekinesisIdleInterval);
		return;
	}
	
	// Scan LootbagGroup first. MissionCleanup fallback is throttled to avoid
	// expensive full-group sweeps while still fixing initial registration misses.
	%hasLootbagGroup = isObject("LootbagGroup");
	%hasMissionCleanup = isObject("MissionCleanup");
	if(!%hasLootbagGroup && !%hasMissionCleanup)
	{
		Ascension::ScheduleTelekinesisScan($TelekinesisNoBagInterval);
		return;
	}
	
	%bagList = "";
	%bagCount = 0;
	%seenBags = " ";
	
	// Primary source: LootbagGroup (chunked with a rolling cursor)
	if(%hasLootbagGroup)
	{
		%count = Group::objectCount(LootbagGroup);
		if(%count > 0)
		{
			%scanBudget = $TelekinesisLootScanBudget;
			if(%scanBudget == "" || %scanBudget <= 0 || %scanBudget > %count)
				%scanBudget = %count;
			
			%scanStart = $TelekinesisLootScanCursor;
			if(%scanStart == "" || %scanStart == -1 || %scanStart < 0 || %scanStart >= %count)
				%scanStart = 0;
			
			for(%n = 0; %n < %scanBudget; %n++)
			{
				%idx = %scanStart + %n;
				if(%idx >= %count)
					%idx -= %count;
				
				%bag = Group::getObject(LootbagGroup, %idx);
				
				if(!isObject(%bag))
					continue;
				
				// CRITICAL SAFEGUARD: Skip Player objects
				if(getObjectType(%bag) == "Player")
					continue;
				
				%objType = getObjectType(%bag);
				%mapName = GameBase::getMapName(%bag);
				%lootData = $loot[%bag];
				
				// Only process valid lootbags
				if(%objType != "Item" || (%mapName != "Backpack" && %lootData == ""))
					continue;
				
				if(%lootData == "" || %lootData == -1)
					continue;
				
				// Skip duplicates
				if(String::findSubStr(%seenBags, " " @ %bag @ " ") != -1)
					continue;
				
				%seenBags = %seenBags @ %bag @ " ";
				%bagList = %bagList @ %bag @ " ";
				%bagCount++;
			}
			
			%nextScan = %scanStart + %scanBudget;
			if(%nextScan >= %count)
				%nextScan -= %count;
			$TelekinesisLootScanCursor = %nextScan;
		}
		else
		{
			$TelekinesisLootScanCursor = 0;
		}
	}
	
	// Fallback source: MissionCleanup (for bags not yet in LootbagGroup)
	// Run when LootbagGroup is missing/empty OR periodically by throttle.
	%doMissionSync = false;
	if(%hasMissionCleanup)
	{
		if(!%hasLootbagGroup)
			%doMissionSync = true;
		else if(Group::objectCount(LootbagGroup) <= 0)
			%doMissionSync = true;
		else
		{
			%now = getSimTime();
			%lastSync = $TelekinesisLastMissionSync;
			if(%lastSync == "" || %lastSync == -1 || (%now - %lastSync) >= $TelekinesisMissionSyncInterval)
				%doMissionSync = true;
		}
	}
	
	if(%doMissionSync)
	{
		%missionGroup = nameToID("MissionCleanup");
		%missionCount = Group::objectCount(%missionGroup);
		if(%missionCount > 0)
		{
			%missionScanBudget = $TelekinesisMissionScanBudget;
			if(%missionScanBudget == "" || %missionScanBudget <= 0 || %missionScanBudget > %missionCount)
				%missionScanBudget = %missionCount;
			
			%missionScanStart = $TelekinesisMissionScanCursor;
			if(%missionScanStart == "" || %missionScanStart == -1 || %missionScanStart < 0 || %missionScanStart >= %missionCount)
				%missionScanStart = 0;
			
			for(%n = 0; %n < %missionScanBudget; %n++)
			{
				%idx = %missionScanStart + %n;
				if(%idx >= %missionCount)
					%idx -= %missionCount;
				
				%bag = Group::getObject(%missionGroup, %idx);
				
				if(!isObject(%bag))
					continue;
				
				// CRITICAL SAFEGUARD: Skip Player objects
				if(getObjectType(%bag) == "Player")
					continue;
				
				// Skip duplicates already seen from LootbagGroup
				if(String::findSubStr(%seenBags, " " @ %bag @ " ") != -1)
					continue;
				
				%objType = getObjectType(%bag);
				%mapName = GameBase::getMapName(%bag);
				%lootData = $loot[%bag];
				
				// Only process valid lootbags
				if(%objType != "Item" || (%mapName != "Backpack" && %lootData == ""))
					continue;
				
				if(%lootData == "" || %lootData == -1)
					continue;
				
				// Add to LootbagGroup for future scans if available
				if(%hasLootbagGroup)
					addToSet(LootbagGroup, %bag);
				
				%seenBags = %seenBags @ %bag @ " ";
				%bagList = %bagList @ %bag @ " ";
				%bagCount++;
			}
			
			%missionNextScan = %missionScanStart + %missionScanBudget;
			if(%missionNextScan >= %missionCount)
				%missionNextScan -= %missionCount;
			$TelekinesisMissionScanCursor = %missionNextScan;
		}
		else
		{
			$TelekinesisMissionScanCursor = 0;
		}
		
		$TelekinesisLastMissionSync = getSimTime();
	}
	
	%processed = 0;
	
	for(%i = 0; %i < %bagCount && %processed < $TelekinesisMaxPerPass; %i++)
	{
		%bag = GetWord(%bagList, %i);
		
		if(%bag == "" || %bag == -1 || !isObject(%bag))
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
		
		for(%p = 0; %p < %activeTeleCount; %p++)
		{
			%id = GetWord(%activeTeleList, %p);
			
			%playerObj = Client::getOwnedObject(%id);
			if(%playerObj == -1 || %playerObj == "")
				continue;
			
			%playerPos = GameBase::getPosition(%playerObj);
			%dist = Vector::getDistance(%playerPos, %bagPos);
			
			if(%dist > $TelekinesisRadius)
				continue;
			
			// Check eligibility using Item::onCollision permission rules
			%playerName = Client::getName(%id);
			%eligible = false;
			
			// Reuse Item::onCollision loot permission logic:
			// IsInCommaList(namelist, playerName) OR namelist == "*"
			if(IsInCommaList(%namelist, %playerName) || %namelist == "*")
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
	
	// Schedule next scan (slow down when no loot candidates were found).
	%nextInterval = $TelekinesisScanInterval;
	if(%bagCount <= 0)
		%nextInterval = $TelekinesisNoBagInterval;
	
	Ascension::ScheduleTelekinesisScan(%nextInterval);
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
			Client::sendMessage(%clientId, 0, "Telekinesis: Collected loot.~loot");
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
		
		// Handle dynamic robe costs for display
		if($AscensionTalent[%talent, DynamicCost] == "robe")
			%cost = Ascension::GetRobeCost(%clientId);
			
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
	
	// Handle dynamic robe costs for display
	if($AscensionTalent[%talent, DynamicCost] == "robe")
		%cost = Ascension::GetRobeCost(%clientId);
		
	%costType = $AscensionTalent[%talent, CostType];
	%desc = $AscensionTalent[%talent, Desc];
	%requires = $AscensionTalent[%talent, Requires];
	%minRemort = $AscensionTalent[%talent, MinRemort];

	// DISPLAY FIX: robe talents gate on the DYNAMIC count-based minimum (first robe
	// 125, second 110, third 105 - regardless of which robe), not the per-talent
	// static value. CanAfford already used the dynamic value; the info screen didn't,
	// so e.g. buying Void Robe first displayed "Requires Remort 105+" while the
	// actual gate was 125.
	if($AscensionTalent[%talent, DynamicCost] == "robe")
		%minRemort = Ascension::GetRobeMinRemort(%clientId);
	
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

