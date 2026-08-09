//============================================================================
// rpgfunk_player.cs — split from rpgfunk.cs (scatter split, Phase 2)
// Extracted 2026-07-18 at commit 5118b1b. Mechanical text move, no behavior change.
// RefreshAll/GiveThisStuff/UpdateAppearance + team/race + requirements (HOT PATHS)
// Source line ranges listed in the rpgfunk.cs shell tombstone.
// exec'd by rpgfunk.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
                     // NOTE: the join-time INVENTORY SUMMARY block is deliberately
                     // ALWAYS ON - it's the audit log of what a player had on join
                     // (used to restore inventory after loss bugs). Do not gate it.

// Safe skin setter - validates skin before applying and logs potential invisibility issues
function Safe_SetSkin(%clientId, %skin, %callerContext)
{
	if(%skin == "" || %skin == -1)
	{
		%clientName = Client::getName(%clientId);
		if(%clientName == "" || %clientName == -1) %clientName = "Unknown";
		echo("[INVISIBILITY BUG] Safe_SetSkin: EMPTY SKIN detected for " @ %clientName @ " (clientId=" @ %clientId @ "). Context: " @ %callerContext @ ". NOT setting skin to prevent invisibility!");
		return;
	}
	
	if($INVISIBILITY_DEBUG)
	{
		%clientName = Client::getName(%clientId);
		if(%clientName == "" || %clientName == -1) %clientName = "Bot";
		echo("[INVISIBILITY DEBUG] Safe_SetSkin: Setting skin '" @ %skin @ "' for " @ %clientName @ " (clientId=" @ %clientId @ "). Context: " @ %callerContext);
	}
	
	// OPTIMIZATION: Only set skin if it's actually different to prevent engine/network spam
	if(Client::getSkinBase(%clientId) != %skin)
	{
		Client::setSkin(%clientId, %skin);
	}
}

function viewGroupList(%clientId)
{
	dbecho($dbechoMode, "viewGroupList(" @ %clientId @ ")");

	bottomprint(%clientId, fetchData(%clientId, "grouplist"), 8);
}
function isInSpellList(%name, %sname)
{
	dbecho($dbechoMode, "isInSpellList(" @ %name @ ", " @ %sname @ ")");

	%sname = %sname @ $sepchar;

	//check if %sname (includes delimiter) is in %name's SpellList
	if(String::findSubStr($SpellList[%name], %sname) != -1)
		return 1;
	else
		return 0;
}
function getSpellAtPos(%name, %pos)
{
	dbecho($dbechoMode, "getSpellAtPos(" @ %name @ ", " @ %pos @ ")");

	%s = $SpellList[%name];
	%n = 0;
	%oldpos = 0;

	for(%i=0; %i<=String::len(%s); %i++)
	{
		%a = String::getSubStr(%s, %i, 1);
		if(%a == ",")
		{
			%n++;
			if(%n == %pos && (%i+1) <= String::len(%s))
			{
				return String::getSubStr(%s, %oldpos, (%i-1)-%oldpos+1);
			}
			%oldpos = %i+1;
		}
	}
	return -1;
}
function countNumSpells(%clientId)
{
	dbecho($dbechoMode, "countNumSpells(" @ %clientId @ ")");

	%name = Client::getName(%clientId);
	%s = $SpellList[%name];
	%n = 0;

	for(%i=0; %i<=String::len(%s); %i++)
	{
		%a = String::getSubStr(%s, %i, 1);
		if(%a == ",") %n++;
	}

	return %n;
}

// VOID CONVERSION 2026-07-14: "what armor is this player wearing?" - belt-first
// (EquippedBeltArmor, the converted system), falling back to the engine-armor
// cache (WornEngineArmor, written by UpdateAppearance from mounted X0 items -
// only un-migrated players still have those). Use this instead of reading the
// "Armor" funkvar, which now holds the belt CARRIED-armor list, not a name.
function GetWornArmor(%clientId)
{
	%belt = fetchData(%clientId, "EquippedBeltArmor");
	if(%belt != "" && %belt != "0" && %belt != -1)
		return %belt;
	return fetchData(%clientId, "WornEngineArmor");
}

function UpdateAppearance(%clientId)
{
	// Recursion Guard: Prevent infinite loops
	if($InUpdateAppearance[%clientId]) return;
	$InUpdateAppearance[%clientId] = true;
	
	%clientName = Client::getName(%clientId);
	dbecho($dbechoMode, "UpdateAppearance(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before proceeding
	// (merge-scar duplicated statements/conditions in this function cleaned up)
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		// Player object doesn't exist (player/bot was deleted)
		$InUpdateAppearance[%clientId] = false;
		return;
	}
	
	// CRITICAL FIX: Check explicit flag on player object (most robust)
	if(%player.isTownBot == "true" || %player.isTownBot == true)
	{
		// This is definitely a town bot - skip
		dbecho($dbechoMode, "UpdateAppearance skipped for bot " @ %clientId @ " (isTownBot flag set)");
		$InUpdateAppearance[%clientId] = false;
		return;
	}

	// Skip all bots - their appearance is managed during spawn and should not be changed
	// Town bots have BotInfoAiName set but no SpawnBotInfo
	// Enemy bots have SpawnBotInfo set
	// Both types already have their armor/skin set correctly during spawn, so we shouldn't overwrite it
	if(isRPGAI(%clientId))
	{
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		
		// If it's a town bot (BotInfoAiName but no SpawnBotInfo) - skip
		if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0" && (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
		{
			// This is a town bot - skip UpdateAppearance to prevent skin reset
			$InUpdateAppearance[%clientId] = false;
			return;
		}
		
		// If it's an enemy bot (has SpawnBotInfo) - skip
		// Enemy bots have their armor/skin set from $BotInfo[botName, RACE] and equipment string in SpawnAI()
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Enemy bot - don't change their appearance (already set from $BotInfo and equipment string)
			$InUpdateAppearance[%clientId] = false;
			return;
		}
	}

	//Determine armor from shields
	%armor = -1;
	%shield = -1;
	
	// CRITICAL: Re-validate player object before calling GetAccessoryList (which calls Player::getItemCount)
	%playerCheck = Client::getOwnedObject(%clientId);
	if(%playerCheck == -1 || %playerCheck == "")
	{
		// Player object was deleted between validation and this call
		$InUpdateAppearance[%clientId] = false;
		return;
	}
	
	%list = GetAccessoryList(%clientId, 2, "3 7");
	for(%i = 0; (%w = getCroppedItem(GetWord(%list, %i))) != -1; %i++)
	{
		if($AccessoryVar[%w, $AccessoryType] == $BodyAccessoryType)
			%armor = %w;
		else if($AccessoryVar[%w, $AccessoryType] == $ShieldAccessoryType)
			%shield = %w;
	}

	// Belt shields (BeltWeapons.cs): shield-type belt accessories carry their
	// visual ItemData name in $BeltAccessoryVisual. Feed it into the same
	// slot-2 mount logic engine shields use below (the visual is a phantom
	// mount - never in inventory). An explicitly equipped belt shield wins
	// over an engine shield.
	%beltAccList = fetchData(%clientId, "EquippedBeltAccessories");
	if(%beltAccList != "" && %beltAccList != "0")
	{
		for(%i = 0; (%w = GetWord(%beltAccList, %i)) != -1; %i++)
		{
			if($AccessoryVar[%w, $AccessoryType] == $ShieldAccessoryType && $BeltAccessoryVisual[%w] != "")
				%shield = $BeltAccessoryVisual[%w];
		}
	}

	// Store armor name to player data so armor effects can be looked up (used by playerdamage.cs)
	// This allows armor special effects (RETRIBUTION, STATIC_DISCHARGE, PHASE_SHIFT) to work
	// VOID CONVERSION 2026-07-14: re-keyed "Armor" -> "WornEngineArmor". The "Armor" key now
	// belongs EXCLUSIVELY to the belt's carried-armor list ("name count " pairs, field 48);
	// writing "" here on every appearance refresh was wiping that list for any player with
	// no engine body-armor mounted (i.e. every migrated player). Readers that want "what
	// armor is this player wearing" use GetWornArmor() (belt-first, engine fallback).
	if(%armor != -1 && %armor != "")
		storeData(%clientId, "WornEngineArmor", %armor);
	else
		storeData(%clientId, "WornEngineArmor", "");
	
	// CRITICAL: Re-validate player object before using it
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		// Player object was deleted during GetAccessoryList
		$InUpdateAppearance[%clientId] = false;
		return;
	}
	%race = fetchData(%clientId, "RACE");
	%model = Player::getArmor(%clientId);
	%cw = String::getSubStr(%model, String::findSubStr(%model, "Armor"), 99999);
	// HARDEN EXTRACTION: If %cw is just "Armor" (extracted from a monster like OrcArmor), 
	// it leads to "MaleHumanArmor" (invalid/slow) when switching back to human via Transmog.
	// Humans MUST have a numeric suffix (0-11). Default to Armor7 (balanced) if missing.
	%lastChar = String::getSubStr(%cw, String::len(%cw)-1, 1);
	if(%lastChar < "0" || %lastChar > "9") %cw = "Armor7";
	%skinbase = Client::getSkinBase(%clientId);
	
	// Initialize %apm, only access $ArmorPlayerModel if %armor is valid
	%apm = "";
	if(%armor != -1 && %armor != "")
	{
		if($ArmorPlayerModel[%armor] != "")
			%apm = $ArmorPlayerModel[%armor];
	}

	//=================================
	// Update skin & Race (Transmog Overrides)
	//=================================
	%personalSkin = fetchData(%clientId, "PersonalSkin");
	%tmRace = fetchData(%clientId, "TransmogRace");
	
	if(%personalSkin != "" && %personalSkin != "0")
	{
		%skinbase = %personalSkin;
		
		// 1. Race Override (TransmogRace)
		if(%tmRace != "" && %tmRace != "0")
		{
		    %race = %tmRace;
		}
		else
		{
		    // Legacy fallback for skins without TransmogRace flag
		    if(%personalSkin == "rpgorc") %race = "Orc";
		    else if(%personalSkin == "rpggnoll") %race = "Pigman";
		    else if(%personalSkin == "min") %race = "Minotaur";
		    else if(%personalSkin == "undead") %race = "Undead";
		    else if(%personalSkin == "zombie") %race = "Zombie";
		    else if(%personalSkin == "chewbacca") %race = "Ogre"; 
		    else if(%personalSkin == "storm_daemon") %race = "Demon";
		    else if(%personalSkin == "redgodeye") %race = "Ogre";
		}

		// 2. Human-only Gender Overrides
		if(%race == "MaleHuman" || %race == "FemaleHuman")
		{
			if(String::findSubStr(%personalSkin, "female") != -1) %race = "FemaleHuman";
			else if(String::findSubStr(%personalSkin, "male") != -1) %race = "MaleHuman";
		}
		
		// 3. Universal Suffix Stripping
		%suffixPos = String::findSubStr(%skinbase, ".male");
		if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
		%suffixPos = String::findSubStr(%skinbase, ".female");
		if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
	}
	else if(%race == "MaleHuman" || %race == "FemaleHuman")
	{
		%skinbase = "rpgbase";
	}

	//=================================
	// Update Visuals (Armor/Model)
	//=================================
	if(%race == "MaleHuman" || %race == "FemaleHuman")
	{
		if(%armor != -1)
		{
			// Regular gear visual
			%skinbase = $ArmorSkin[%armor];
		}

		// Belt armor visual (BeltWeapons plan): belt armors are datablock-less;
		// their skins register in $ArmorSkin under the belt name. Equipped belt
		// armor overrides the engine-armor skin - and must apply even with NO
		// engine armor equipped (%armor == -1), where the skin would otherwise
		// stay rpgbase. Transmog still wins below.
		%beltArmor = fetchData(%clientId, "EquippedBeltArmor");
		if(%beltArmor != "" && %beltArmor != "0" && $ArmorSkin[%beltArmor] != "")
		{
			%skinbase = $ArmorSkin[%beltArmor];
			// VOID CONVERSION 2026-07-14: belt armor also carries the player-model
			// override (robes -> "Robed") - %apm was only derived from the mounted
			// ENGINE armor above, so a belt-equipped robe would keep the plate model.
			// %apm is consumed by the player-model selection further down.
			if($ArmorPlayerModel[%beltArmor] != "")
				%apm = $ArmorPlayerModel[%beltArmor];
		}

		// Transmog visual override (re-applied here since the armor/belt skins
		// above overwrite the earlier transmog pass; harmless no-op otherwise)
		if(%personalSkin != "" && %personalSkin != "0")
		{
			%skinbase = %personalSkin;
			%suffixPos = String::findSubStr(%skinbase, ".male");
			if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
			%suffixPos = String::findSubStr(%skinbase, ".female");
			if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
		}
	}
	else if(%race == "DeathKnight")
	{
		%skinbase = "cphoenix";
		%apm = "";
		%cw = "Armor22";
		%armor = 0;
	}
	else
	{
		%p = $RaceToArmorType[%race];
		%armor = -1;
	}

	//=================================
	// Update player model (Armor)
	//=================================
	%adminBoots = (Player::getItemCount(%clientId, "AdminBoots0") > 0);
	if(%adminBoots)
	{
		// ADMIN BOOTS OVERRIDE: Select specialized variant for flying
		if(%race == "Orc" || %race == "Pigman") %p = "AdminBootsMediumArmor";
		else if(%race == "Ogre") %p = "AdminBootsHeavyArmor";
		else if(%race == "Zombie") %p = "AdminBootsZombieArmor";
		else if(%race == "Undead") %p = "AdminBootsSkelArmor";
		else if(%race == "Minotaur") %p = "AdminBootsMinotaurArmor";
		else if(%race == "Angel") %p = "AdminBootsFemaleRobedArmor";
		else if(%race == "Admin") %p = "AdminBootsRobedArmor";
		else if(%race == "Demon" || %race == "Alien" || %race == "Seals" || %race == "God" || %race == "Uber") %p = "AdminBootsMonsterArmor";
		// Check for: actual robe equipped (apm=="Robed"), Transmog robe skin, or robed race
		else if(%apm == "Robed" && %race == "MaleHuman") %p = "AdminBootsRobedArmor";
		else if(%apm == "Robed" && %race == "FemaleHuman") %p = "AdminBootsFemaleRobedArmor";
		else if(%race == "MaleHumanRobed" || (String::findSubStr(%personalSkin, "robe") != -1 && %race == "MaleHuman")) %p = "AdminBootsRobedArmor";
		else if(%race == "FemaleHumanRobed" || (String::findSubStr(%personalSkin, "robe") != -1 && %race == "FemaleHuman")) %p = "AdminBootsFemaleRobedArmor";
		else %p = "AdminBootsArmor";
	}
	// VOID CONVERSION 2026-07-14: also take this branch when %apm is set with NO
	// engine armor mounted - a belt-equipped robe has %armor == -1 (nothing
	// mounted) but must still get the Robed model (%race @ "Robed" @ %cw), else
	// the naked path renders the plate-body "tights" model under the robe skin.
	// For belt PLATE armor (%apm == "") the naked path below builds the identical
	// string, so behavior there is unchanged.
	else if(%armor != -1 || %apm != "")
	{
		%p = %race @ %apm @ %cw;
	}
	else
	{
		// Default Armor handling (Naked/No Body Accessory)
		// For humans: Use %race @ %cw to preserve the current speed tier (set by RefreshWeight)
		// %cw was already extracted from current armor earlier in this function and defaults to "Armor7" if invalid
		// This avoids both: the invisibility bug (Armor0 -> Armor7 double-switch) AND
		// breaking speed boots (which set Armor8-11 via RefreshWeight)
		if(%race == "MaleHuman" || %race == "FemaleHuman")
			%p = %race @ %cw; 
		else
			%p = $RaceToArmorType[%race];
	}

	%ae = GameBase::getEnergy(%player);

	//=================================
	// Set Armor (Check logic)
	//=================================
	// We only set armor if it's different. This prevents the "armor ping-pong"
	// that triggers repeated attack animations even when no change is needed.
	if(Player::getArmor(%clientId) != %p && %p != "")
	{
		Player::setArmor(%clientId, %p);
		GameBase::setEnergy(%player, %ae);
	}
	
	//=================================
	// Update skin (After Armor)
	//=================================
	// CRITICAL FIX: Validate skin before setting - empty skin causes invisibility!
	if(%skinbase == "" || %skinbase == -1)
	{
		echo("[INVISIBILITY BUG] UpdateAppearance: EMPTY SKINBASE detected for " @ %clientName @ " (clientId=" @ %clientId @ "). Race=" @ %race @ ", Armor=" @ %armor @ ", ArmorSkin[armor]=" @ $ArmorSkin[%armor] @ ". Falling back to rpgbase.");
		%skinbase = "rpgbase";
	}
	
	// CRITICAL FIX: Set skin AFTER armor change to prevent reversion to default/enemy skin
	if(Client::getSkinBase(%clientId) != %skinbase)
	{
		// Log skin changes for debugging invisibility issues
		if($SKIN_DEBUG) echo("[SKIN DEBUG] UpdateAppearance: Setting skin for " @ %clientName @ " from '" @ Client::getSkinBase(%clientId) @ "' to '" @ %skinbase @ "'");
		Client::setSkin(%clientId, %skinbase);
	}

	//=================================
	// Update shields and Orb
	//=================================
	if(%shield != -1)
	{
		if(Player::getMountedItem(%clientId, 2) != %shield)
		{
			Player::unmountItem(%clientId, 2);
			Player::mountItem(%clientId, %shield, 2);
		}
	}
	else
	{
		%player = Client::getOwnedObject(%clientId);
		if(%player != -1)
		{
			if(Player::getMountedItem(%clientId, 2) != -1)
				Player::unmountItem(%clientId, 2);

			for(%i = 1; $ItemList[Orb, %i] != ""; %i++)
			{
				// Re-validate player object in loop in case it gets despawned mid-execution
				%playerCheck = Client::getOwnedObject(%clientId);
				if(%playerCheck == "" || %playerCheck == -1)
					break; // Exit loop if player object no longer exists
				
				if(SafeGetItemCount(%clientId, $ItemList[Orb, %i] @ "0", "UpdateAppearance"))
					Player::mountItem(%clientId, $ItemList[Orb, %i] @ "0", 2);
			}
		}
	}
	
	// Release Recursion Guard
	$InUpdateAppearance[%clientId] = false;
}

function UpdateTeam(%clientId)
{
	dbecho($dbechoMode, "UpdateTeam(" @ %clientId @ ")");

	// CRITICAL: Check display name patterns FIRST to identify bots before SpawnBotInfo is set
	// This prevents UpdateTeam() from running for bots when called from Game::playerSpawn()
	// during AI::spawn() before SpawnBotInfo and BotInfoAiName are set
	%playerName = Client::getName(%clientId);
	%isBotByName = false;
	if(%playerName != "" && %playerName != -1)
	{
		// Check for enemy bot name patterns (anywhere in name, not just start)
		// CRITICAL: Use != -1 to match pattern anywhere in name (allows "Giant Demon Lord" to match "Demon")
		if(String::findSubStr(%playerName, "Alien") != -1 || 
		   String::findSubStr(%playerName, "Admin") != -1 ||
		   String::findSubStr(%playerName, "Angel") != -1 ||
		   String::findSubStr(%playerName, "Demon") != -1 ||
		   String::findSubStr(%playerName, "Zombie") != -1 ||
		   String::findSubStr(%playerName, "Ogre") != -1 ||
		   String::findSubStr(%playerName, "Orc") != -1 ||
		   String::findSubStr(%playerName, "Pigman") != -1 ||
		   String::findSubStr(%playerName, "Pigmen") != -1 ||
		   String::findSubStr(%playerName, "Undead") != -1 ||
		   String::findSubStr(%playerName, "Minotaur") != -1 ||
		   String::findSubStr(%playerName, "Seal") != -1 ||
		   String::findSubStr(%playerName, "God") != -1 ||
		   String::findSubStr(%playerName, "Void") != -1 ||
		   String::findSubStr(%playerName, "Enemy") != -1)
		{
			%isBotByName = true;
		}
		
		// Check for town bot name patterns (quest NPCs, merchants, etc.)
		// Town bots typically have names like "Troy", "Rufus", "Merchant", etc.
		// But we can't reliably identify them by name alone, so we rely on BotInfoAiName check below
	}

	// CRITICAL: Skip all bots - they have their team set from $BotInfo[botName, TEAM] in SpawnAI()
	// Town bots have BotInfoAiName but no SpawnBotInfo
	// Enemy bots have SpawnBotInfo set
	// Both types already have their team set correctly during spawn, so we shouldn't overwrite it
	
	// FIRST SAFEGUARD: Check Player::isAiControlled - most reliable check for AI bots
	if(Player::isAiControlled(%clientId))
	{
		// This is an AI-controlled entity - do not modify team
		return;
	}
	
	// PHASE 2 FIX: Also check the centralized bot registry for registered bots
	// This provides another layer of protection against team modification
	if(IsBotRegistered(%clientId))
	{
		// Bot is in the registry - absolutely do not modify team
		return;
	}
	
	if(isRPGAI(%clientId) || %isBotByName)
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		
		// If it's a town bot (BotInfoAiName but no SpawnBotInfo) - skip
		if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1 && (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
		{
			// Town bot - don't change their team (should already be set to 0)
			return;
		}
		
		// If it's an enemy bot (has SpawnBotInfo OR matches enemy bot name pattern) - skip
		// Enemy bots have their team set from $BotInfo[botName, TEAM] in SpawnAI()
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Enemy bot - don't change their team (already set from $BotInfo[botName, TEAM])
			return;
		}
		
		// If it matches enemy bot name pattern but SpawnBotInfo isn't set yet (during spawn)
		// This is a newly spawned enemy bot - don't set team, let SpawnAIGetClientId() handle it
		if(%isBotByName)
		{
			// This is an enemy bot being spawned - don't set team here
			// SpawnAIGetClientId() will set the correct team shortly
			return;
		}
	}

	%race = fetchData(%clientId, "RACE");
	%t = $TeamForRace[%race];
	
	// CRITICAL: For players, default to team 0 (friendly) if race is invalid or not set
	// For bots, this should never happen as they have their team set elsewhere
	// NOTE: Team 0 is VALID (Citizen/friendly) - don't treat it as invalid!
	// Only treat empty string or -1 as invalid
	if(%t == "" || %t == -1)
	{
		// Check if this is a bot or player
		%isBot = isRPGAI(%clientId) || Player::isAiControlled(%clientId);
		if(%isBot)
		{
			// Bot with invalid race - default to team 1 (enemy)
			echo("WARNING: UpdateTeam - Invalid team for bot race '" @ %race @ "', defaulting to team 1");
			%t = 1;
		}
		else
		{
			// Player with invalid/missing race - default to team 0 (friendly)
			// Only log if race is actually missing, not when team 0 is correct
			if(%race == "" || %race == -1)
			{
				echo("WARNING: UpdateTeam - Missing race for player, defaulting to team 0");
			}
			%t = 0;
		}
	}
	
	// CRITICAL: Validate team is not -1 before setting
	// -1 means team hasn't been set yet, so we should never set it to -1
	// NOTE: 0 is a VALID team (friendly), so don't treat it as invalid!
	if(%t == -1 || %t == "-1")
	{
		// Check if this is a bot or player
		%isBot = isRPGAI(%clientId) || Player::isAiControlled(%clientId);
		if(%isBot)
		{
			echo("ERROR: UpdateTeam - Team is -1 after validation for bot! Defaulting to team 1 for clientId " @ %clientId);
			%t = 1;
		}
		else
		{
			echo("ERROR: UpdateTeam - Team is -1 after validation for player! Defaulting to team 0 for clientId " @ %clientId);
			%t = 0;
		}
	}
	
	GameBase::setTeam(%clientId, %t);
	
	// CRITICAL: For enemy bots, immediately restore team from botTeam if UpdateTeam() changed it
	// This prevents UpdateTeam() from overwriting the correct team set during spawn
	if(isRPGAI(%clientId))
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Enemy bot - restore team from botTeam if stored
			%storedBotTeam = fetchData(%clientId, "botTeam");
			
			// If botTeam is not stored, try to get from BotInfoAiName
			if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
			{
				%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
				if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
				{
					%storedBotTeam = $BotInfo[%botInfoAiName, TEAM];
					if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
					{
						%storedBotTeam = 1; // Default to team 1 if not specified
					}
					// Store it for future use
					storeData(%clientId, "botTeam", %storedBotTeam);
				}
				else
				{
					// Can't determine team - default to 1
					%storedBotTeam = 1;
					storeData(%clientId, "botTeam", 1);
				}
			}
			
			// Validate storedBotTeam is not -1
			if(%storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
			{
				echo("ERROR: UpdateTeam - botTeam is -1 for enemy bot (clientId=" @ %clientId @ "). Defaulting to team 1.");
				%storedBotTeam = 1;
				storeData(%clientId, "botTeam", 1);
			}
			
			// CRITICAL: Check Player object first (team is more reliably set there)
			// Then fall back to client ID if Player object doesn't exist
			%playerObjForTeam = Client::getOwnedObject(%clientId);
			%currentTeam = -1;
			if(%playerObjForTeam != -1 && %playerObjForTeam != "")
			{
				%currentTeam = GameBase::getTeam(%playerObjForTeam);
			}
			// If still -1, check client ID
			if(%currentTeam == -1)
			{
				%currentTeam = GameBase::getTeam(%clientId);
			}
			
			// CRITICAL: If current team is -1, ALWAYS restore it
			// If current team doesn't match stored team, restore it
			if(%currentTeam == -1 || %currentTeam != %storedBotTeam)
			{
				// Team was changed by UpdateTeam() or is -1 - restore it immediately
				GameBase::setTeam(%clientId, %storedBotTeam);
				%playerObjForTeam = Client::getOwnedObject(%clientId);
				if(%playerObjForTeam != -1 && %playerObjForTeam != "")
					GameBase::setTeam(%playerObjForTeam, %storedBotTeam);
				if(%currentTeam == -1)
				{
					if($BOT_TEAM_DEBUG) echo("[BOT TEAM DEBUG] UpdateTeam - Restored enemy bot team from -1 to " @ %storedBotTeam @ " for clientId " @ %clientId);
				}
				else
				{
					echo("WARNING: UpdateTeam - Restored enemy bot team from " @ %currentTeam @ " to " @ %storedBotTeam @ " for clientId " @ %clientId);
				}
			}
		}
	}
}
function ChangeRace(%clientId, %race)
{
	dbecho($dbechoMode, "ChangeRace(" @ %clientId @ ", " @ %race @ ")");

	if(%race == "DeathKnight")
		storeData(%clientId, "RACE", "DeathKnight");
	else if(%race == "Human")
		storeData(%clientId, "RACE", Client::getGender(%clientId) @ "Human");
	else if(%race == "MaleHuman")
		storeData(%clientId, "RACE", "MaleHuman");
	else if(%race == "FemaleHuman")
		storeData(%clientId, "RACE", "FemaleHuman");

	setHP(%clientId, fetchData(%clientId, "MaxHP"));
	
	// CRITICAL: Check if AdminBoots wants to preserve mana (prevents mana refresh on stance changes)
	%preserveMana = fetchData(%clientId, "AdminBootsPreserveMana");
	if(%preserveMana != "" && %preserveMana != -1 && %preserveMana != "0")
	{
		// AdminBoots wants to preserve mana - restore it instead of setting to full
		%maxMana = fetchData(%clientId, "MaxMANA");
		if(%preserveMana < %maxMana)
			setMANA(%clientId, %preserveMana);
		else
			setMANA(%clientId, fetchData(%clientId, "MaxMANA"));
	}
	else
	{
		// Normal behavior - set mana to full
		setMANA(%clientId, fetchData(%clientId, "MaxMANA"));
	}

	RefreshAll(%clientId);
	
}

function RefreshAll(%clientId, %fromSkillUpgrade)
{
	// WATCHDOG: Track this function for freeze detection
	Watchdog_Enter("RefreshAll");
	%vperfTE = getRealMillis();	// $VoidPerf probe: function entry
	
	if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] RefreshAll: ENTRY - clientId=" @ %clientId @ ", fromSkillUpgrade=" @ %fromSkillUpgrade);
	dbecho($dbechoMode, "RefreshAll(" @ %clientId @ ", " @ %fromSkillUpgrade @ ")");

	// DEBUG: Log when RefreshAll is called from a skill upgrade to track frequency and identify spam
	// CRITICAL: Validate player object exists before logging debug info to prevent errors
	if(($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) && (%fromSkillUpgrade == "true" || %fromSkillUpgrade == "1" || %fromSkillUpgrade == 1))
	{
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck != -1 && %playerObjCheck != "")
		{
			%playerName = Client::getName(%clientId);
			if(%playerName == "" || %playerName == -1)
				%playerName = "Unknown";
			%isBot = isRPGAI(%clientId);
			%botInfo = "";
			if(%isBot)
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				if(%botName == "" || %botName == "0")
					%botName = fetchData(%clientId, "SpawnBotInfo");
				if(%botName != "" && %botName != "0")
					%botInfo = " (Bot: " @ %botName @ ")";
			}
			echo("[DEBUG RefreshAll] Called from skill upgrade - clientId: " @ %clientId @ ", player: '" @ %playerName @ "'" @ %botInfo);
		}
	}

//	echo("===== DEBUG RefreshAll: START =====");
//echo("DEBUG RefreshAll: clientId = " @ %clientId);

	// CRITICAL: Validate client ID and player object exist before proceeding
	if(%clientId == -1 || %clientId == "" || %clientId == "0")
	{
		// Clear scheduled flag for invalid client ID
		$RefreshAllScheduled[%clientId] = "";
		return;  // Invalid client ID
	}
	
	// CRITICAL: Early check for disconnected clients - if Client::getName() returns empty, client has disconnected
	// This prevents RefreshAll from running on disconnected clients (which causes spam from scheduled calls)
	%clientName = Client::getName(%clientId);
	if(%clientName == "" || %clientName == -1)
	{
		// Client has disconnected - clear scheduled flag and silently return to prevent spam from scheduled RefreshAll calls
		$RefreshAllScheduled[%clientId] = "";
		return;
	}
	
	// CRITICAL: Check player object exists early (before any other processing)
	// This prevents errors when RefreshAll is called on disconnected clients or deleted bots
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Player object doesn't exist - clear scheduled flag and silently return
		// This can happen for bots that are being deleted or clients that have disconnected
		$RefreshAllScheduled[%clientId] = "";
		return;
	}
	
	// CRITICAL: Prevent multiple scheduled RefreshAll calls from stacking up
	// If a RefreshAll is already scheduled, don't schedule another one (prevents spam from rapid skill upgrades)
	// Check if RefreshAll is already scheduled for this client
	if($RefreshAllScheduled[%clientId] == "true" || $RefreshAllScheduled[%clientId] == "1")
	{
		// RefreshAll is already scheduled - don't run another one
		return;
	}
	
	// Clear scheduled flag if we're proceeding with RefreshAll immediately
	$RefreshAllScheduled[%clientId] = "";
	
	// Player object was already validated above, so we can use it directly here
	%playerName = Client::getName(%clientId);
	
	// CRITICAL: For enemy bots, skip most RefreshAll functionality since they use simplified AI system
	// Only do minimal updates (team restoration) and skip player-specific logic
	// EXCEPTION: Seal battle bots need full stat calculations (MaxHP, MaxMANA, etc.) so they are NOT treated as regular enemy bots
	%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
	%isEnemyBot = false;
	if(isRPGAI(%clientId))
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Only treat as enemy bot if it's NOT a seal battle bot
			// Seal battle bots need full RefreshAll() calculations for scaled stats
			if(%isSealBattleBot != "true" && %isSealBattleBot != "True" && %isSealBattleBot != "1")
			{
				%isEnemyBot = true;
			}
		}
	}
	
	// For enemy bots (but NOT seal battle bots), use dedicated RefreshAllEnemyBot function
	// This function does NOT touch the team at all - it only does minimal updates
	if(%isEnemyBot)
	{
		// Call dedicated enemy bot refresh function that does NOT touch team
		RefreshAllEnemyBot(%clientId);
		// Skip all other RefreshAll functionality for enemy bots
		return;
	}


	// CRITICAL: Skip AdminBoots handling for town bots - they shouldn't have AdminBoots and don't need race changes
	%isTownBot = false;
	if(isRPGAI(%clientId))
	{
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		// Town bots have BotInfoAiName but no SpawnBotInfo
		if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0" && (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
		{
			%isTownBot = true;
		}
	}
	
	// Check if AdminBoots are equipped and trigger race change sequence if needed (skip for town bots)
	if(!%isTownBot)
	{
		// CRITICAL: Re-validate player object before calling Player::getItemCount
		%playerCheck = Client::getOwnedObject(%clientId);
		if(%playerCheck == -1 || %playerCheck == "")
		{
			// Player object was deleted between initial check and this call
			echo("[DEBUG getItemCount] RefreshAll - Player object deleted before AdminBoots check, clientId: " @ %clientId);
			return;
		}
		
		%adminBootsCount = 0;
		// CRITICAL: Final validation right before Player::getItemCount call
		%playerCheckFinal = Client::getOwnedObject(%clientId);
		if(%playerCheckFinal != -1 && %playerCheckFinal != "")
		{
			%adminBootsCount = Player::getItemCount(%clientId, "AdminBoots0");
		}
		else
		{
			echo("[DEBUG getItemCount] RefreshAll - Player object invalid right before AdminBoots0 check, clientId: " @ %clientId);
		}
		if(%adminBootsCount > 0)
		{
			%hasTriggered = fetchData(%clientId, "AdminBootsRaceChangeTriggered");
			if(%hasTriggered != "true")
			{
				storeData(%clientId, "AdminBootsRaceChangeTriggered", "true");
				// Store original armor
				%originalArmor = Player::getArmor(%clientId);
				storeData(%clientId, "AdminBootsOriginalArmor", %originalArmor);
				// Start the race change sequence immediately
				AdminBoots::startRaceChangeSequence(%clientId);
			}
		}
		else
		{
			// AdminBoots not equipped - clear the trigger flag if it exists
			%hasTriggered = fetchData(%clientId, "AdminBootsRaceChangeTriggered");
			if(%hasTriggered == "true")
			{
				storeData(%clientId, "AdminBootsRaceChangeTriggered", "");
			}
		}
	}

	%race = fetchData(%clientId, "RACE");
	//echo("DEBUG RefreshAll: RACE = '" @ %race @ "'");
	%vperfT0 = getRealMillis();	// $VoidPerf probe (equip lag hunt 2026-07-15); wall clock, sim clocks freeze per frame
	if(String::findSubStr(%race, "Human") != -1)
	{
//		echo("DEBUG RefreshAll: Calling RefreshWeight...");
		// CRITICAL: GetWeight() must be called before RefreshWeight() to set $GetWeight::ArmorMod
		// This ensures accessories with SpecialVar type 8 (like Wind Paws) properly apply their modifiers
		GetWeight(%clientId);
		RefreshWeight(%clientId);
//		echo("DEBUG RefreshAll: RefreshWeight completed");
	}
	%vperfT1 = getRealMillis();	// $VoidPerf probe: weight done

//	echo("DEBUG RefreshAll: Calling UpdateAppearance...");
	// CRITICAL: Skip UpdateAppearance for bots - they have their armor set during spawn
	// UpdateAppearance was a legacy workaround for non-engine spawning that caused
	// town bots to have their armor changed to AdminArmor immediately after spawn
	%isBot = isRPGAI(%clientId);
	%clientName = Client::getName(%clientId);
	
	// Recursion Guard: Prevent infinite loops if callbacks trigger RefreshAll again
	if($InRefreshAll[%clientId]) return;
	$InRefreshAll[%clientId] = true;

	if($TOWNBOT_ARMOR_DEBUG) echo("[TOWNBOT ARMOR DEBUG] RefreshAll: clientId=" @ %clientId @ " name='" @ %clientName @ "' isRPGAI=" @ %isBot);
	if(!%isBot)
	{
		if($TOWNBOT_ARMOR_DEBUG) echo("[TOWNBOT ARMOR DEBUG] RefreshAll: Calling UpdateAppearance for " @ %clientId);
		UpdateAppearance(%clientId);
	}
	else
	{
		if($TOWNBOT_ARMOR_DEBUG) echo("[TOWNBOT ARMOR DEBUG] RefreshAll: SKIPPING UpdateAppearance for bot " @ %clientId);
	}
	%vperfT2 = getRealMillis();	// $VoidPerf probe: appearance done
//	echo("DEBUG RefreshAll: UpdateAppearance completed");

//	echo("DEBUG RefreshAll: Calling refreshHPREGEN...");
	refreshHPREGEN(%clientId);
//	echo("DEBUG RefreshAll: refreshHPREGEN completed");

//	echo("DEBUG RefreshAll: Calling refreshMANAREGEN...");
	refreshMANAREGEN(%clientId);
//	echo("DEBUG RefreshAll: refreshMANAREGEN completed");
	%vperfT3 = getRealMillis();	// $VoidPerf probe: regen done

//	echo("DEBUG RefreshAll: Calling Game::refreshClientScore...");
	Game::refreshClientScore(%clientId);
//	echo("DEBUG RefreshAll: Game::refreshClientScore completed");
	%vperfT4 = getRealMillis();	// $VoidPerf probe: score done

	// NOTE: Enemy bot team restoration is now handled at the beginning of RefreshAll()
	// This code is kept for town bots (if any) but enemy bots return early

	// Ensure AdminBootsArmor is set if AdminBoots are equipped (skip for town bots)
	if(!%isTownBot)
	{
		// The armor itself has the speed values baked in, so we just need to ensure it's applied
		// Re-check player object exists (it might have been deleted during RefreshAll)
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == -1 || %playerObj == "")
		{
			// Player object was deleted during RefreshAll - silently return
			$InRefreshAll[%clientId] = false;
			return;
		}
		
		// CRITICAL: Re-validate player object before calling Player::getItemCount
		%playerCheck2 = Client::getOwnedObject(%clientId);
		if(%playerCheck2 == -1 || %playerCheck2 == "")
		{
			// Player object was deleted between check and this call
			$InRefreshAll[%clientId] = false;
			return;
		}
		
		// CRITICAL: Final validation right before Player::getItemCount call
		%playerCheckFinal2 = Client::getOwnedObject(%clientId);
		%adminBootsCount = 0;
		if(%playerCheckFinal2 != -1 && %playerCheckFinal2 != "")
		{
			%adminBootsCount = Player::getItemCount(%clientId, "AdminBoots0");
		}
		else
		{
			echo("[DEBUG getItemCount] RefreshAll (AdminBootsArmor check) - Player object invalid right before AdminBoots0 check, clientId: " @ %clientId);
		}
		if(%adminBootsCount > 0)
		{
			// Make sure AdminBootsArmor is set (in case UpdateAppearance changed it)
			// BUG FIX: Recognize all specialized AdminBoots variants to prevent redundant overrides
			if(String::findSubStr(Player::getArmor(%clientId), "AdminBoots") == -1)
			{
				// Check if player is wearing a Robe
				%isRobed = false;
				
				// CRITICAL FIX: Use GetAccessoryList to find the armor, just like UpdateAppearance does.
				// Player::getMountedItem does NOT work for RPG accessories/armors.
				%list = GetAccessoryList(%clientId, 2, "3 7");
				for(%i = 0; (%w = getCroppedItem(GetWord(%list, %i))) != -1; %i++)
				{
					if($AccessoryVar[%w, $AccessoryType] == $BodyAccessoryType)
					{
						// Found body armor, check if it's a robe
						if($ArmorPlayerModel[%w] == "Robed")
						{
							%isRobed = true;
							break;
						}
					}
				}

				// VOID CONVERSION 2026-07-14: belt-equipped robes aren't mounted, so
				// the scan above can't see them - check EquippedBeltArmor too.
				if(!%isRobed && $ArmorPlayerModel[fetchData(%clientId, "EquippedBeltArmor")] == "Robed")
					%isRobed = true;

				if(%isRobed)
					Player::setArmor(%clientId, "AdminBootsRobedArmor");
				else
					Player::setArmor(%clientId, "AdminBootsArmor");
			}
		}
	}

	// NOTE: Enemy bot team restoration is now handled at the beginning of RefreshAll()
	// Enemy bots return early, so this code only runs for players and town bots

//	echo("===== DEBUG RefreshAll: COMPLETE =====");
	%vperfT5 = getRealMillis();	// $VoidPerf probe: adminboots done

	// VOIDPERF 2026-07-15: KronosHUD_Push was called here AND at the tail of
	// Game::refreshClientScore (rpgstats.cs, called above) - a fully redundant
	// second push. Each push costs 2 full-inventory AddPoints scans (fetchData
	// MaxHP/MaxMANA are COMPUTED stats), measured ~190ms - so the duplicate
	// alone was a third of the equip lag spike. refreshClientScore's push
	// stays; if that tail call is ever removed, restore one here.
	// KronosHUD_Push(%clientId);
	// $VoidPerf probe (equip lag hunt 2026-07-15): first run showed ~480ms
	// UNaccounted between the coarse weight/appearance brackets - this maps
	// the whole function. entry = the validation/AdminBoots-check preamble.
	if($VoidPerf)
		echo("[VOIDPERF] RefreshAll(" @ %clientId @ "): entry=" @ (%vperfT0 - %vperfTE) @ "ms weight=" @ (%vperfT1 - %vperfT0) @ "ms appearance=" @ (%vperfT2 - %vperfT1) @ "ms regen=" @ (%vperfT3 - %vperfT2) @ "ms score=" @ (%vperfT4 - %vperfT3) @ "ms adminboots=" @ (%vperfT5 - %vperfT4) @ "ms hudpush=" @ (getRealMillis() - %vperfT5) @ "ms");
	// VOID 2026-07-15 (HUD audit #3): re-push an OPEN HUD panel too - RefreshAll
	// only ever pushed vitals, so belt changes not caused by a panel button
	// (loot pickups, telekinesis, migration, admin gives) left an open panel
	// stale. Debounced: an armor swap runs RefreshAll twice back-to-back and
	// each panel push is ~50 remoteEvals, so inline pushes doubled into a
	// visible server hitch. KShop_QueuedSync self-gates on hasKronosHUD +
	// kshopOpen, so this is a no-op for vanilla clients and closed panels.
	if(%clientId.hasKronosHUD && %clientId.kshopOpen != "" && %clientId.kshopSyncQueued == "")
	{
		%clientId.kshopSyncQueued = true;
		schedule("KShop_QueuedSync(" @ %clientId @ ");", 0.2);
	}

	// WATCHDOG: Clear tracking for this function
	Watchdog_Exit();

	// CRITICAL: Clear the skill upgrade refresh flag ONLY when the top-level 
	// call from UseSkill completes (indicated by %fromSkillUpgrade).
	// This prevents nested RefreshAll calls (like those in the AdminBoots sequence)
	// from dropping the guard while the refresh process is still busy.
	if(%fromSkillUpgrade == "true" || %fromSkillUpgrade == "1" || %fromSkillUpgrade == 1)
	{
		$SkillUpgradeRefreshScheduled[%clientId] = "";
	}
	
	// Release Recursion Guard
	$InRefreshAll[%clientId] = false;
}

// CRITICAL: New function specifically for enemy bots - does NOT touch team at all
// This is an exact replica of the enemy bot handling in RefreshAll, but without any team restoration
function RefreshAllEnemyBot(%clientId)
{
	dbecho($dbechoMode, "RefreshAllEnemyBot(" @ %clientId @ ")");

	// CRITICAL: Validate client ID and player object exist before proceeding
	if(%clientId == -1 || %clientId == "" || %clientId == "0")
	{
		return;  // Invalid client ID
	}
	
	// CRITICAL: Early check for disconnected clients
	%clientName = Client::getName(%clientId);
	if(%clientName == "" || %clientName == -1)
	{
		return;
	}
	
	// CRITICAL SAFEGUARD: Check if this is a real player with a save file FIRST
	// If a character save file exists, this is DEFINITELY a real player, NOT a bot
	// This prevents RefreshAllEnemyBot from running on players, which could cause client crashes
	if(%clientName != "" && %clientName != -1)
	{
		%characterFile = "temp\\" @ %clientName @ ".cs";
		if(isFile(%characterFile))
		{
			// This is a real player with a save file - abort immediately to prevent client crash
			echo("[CRITICAL] RefreshAllEnemyBot(): SAFEGUARD - Detected real player " @ %clientName @ " (clientId=" @ %clientId @ ") with save file. Aborting to prevent client crash.");
			return;
		}
	}
	
	// CRITICAL: Additional safeguard - verify this is actually a bot
	// Check for bot markers (BotInfoAiName, SpawnBotInfo, or in bot registry)
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%inRegistry = ($BotRegistry[%clientId] != "" && $BotRegistry[%clientId] != -1);
	
	// If none of these bot markers exist, this is likely a player - abort
	if((%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0") && 
	   (%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0") && 
	   !%inRegistry)
	{
		// No bot markers found - this is likely a player, abort to prevent client crash
		echo("[CRITICAL] RefreshAllEnemyBot(): SAFEGUARD - No bot markers found for clientId " @ %clientId @ " (" @ %clientName @ "). Aborting to prevent client crash.");
		return;
	}
	
	// CRITICAL: Check player object exists early
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		return;
	}
	
	// CRITICAL: Clear stance for enemy bots - they should not have stances enabled
	// This ensures stance is always cleared even if something else tries to set it
	storeData(%clientId, "Stance", "");
	
	// DO NOT TOUCH TEAM - This function is specifically designed to NOT modify team
	// The team should already be set correctly by SpawnAIGetClientId() or AI::setWeapons()
	// Any team modifications here could cause the team to reset to -1
	
	// Skip all other RefreshAll functionality for enemy bots
	return;
}

function HasThisStuff(%clientId, %list, %multiplier)
{
	dbecho($dbechoMode, "HasThisStuff(" @ %clientId @ ", " @ %list @ ")");

	if(%list == "")
		return True;

	if(%multiplier == "" || %multiplier <= 0)
		%multiplier = 1;

	%name = Client::getName(%clientId);

	//--------
	// PASS 1
	//--------
	%flag = False;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "LVLG")
		{
			if(fetchData(%clientId, "LVL") > %w2)
				%flag = True;
			else
				%flag = 667;
		}
		else if(%w == "LVLS")
		{
			if(fetchData(%clientId, "LVL") < %w2)
				%flag = True;
			else
				%flag = 667;
		}
		else if(%w == "LVLE")
		{
			if(fetchData(%clientId, "LVL") == %w2)
				%flag = True;
			else
				%flag = 667;
		}
	}

	if(%flag == 667)
		return %flag;


	//--------
	// PASS 2
	//--------
	%cntindex = 0;
	%flag = False;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "CNT")
		{
			%cntindex++;
			%tmpcnt[%cntindex] = %w2;
		}
		else if(%w == "CNTAFFECTS")
		{
			%tmpcntaffects[%cntindex] = %w2;
		}
	}

	//Process the counter data, if any
	for(%i = 1; %tmpcnt[%i] != ""; %i++)
	{
		if(%tmpcnt[%i] != "" && %tmpcntaffects[%i] != "")
		{
			%firstchar = String::getSubStr(%tmpcnt[%i], 0, 1);
			%n = floor(String::getSubStr(%tmpcnt[%i], 1, 9999));
			if(%firstchar == "<")
			{
				if($QuestCounter[%name, %tmpcntaffects[%i]] < %n)
					%flag = True;
				else
					%flag = 666;
			}
			else if(%firstchar == ">")
			{
				if($QuestCounter[%name, %tmpcntaffects[%i]] > %n)
					%flag = True;
				else
					%flag = 666;
			}
			else if(%firstchar == "=")
			{
				if($QuestCounter[%name, %tmpcntaffects[%i]] == %n)
					%flag = True;
				else
					%flag = 666;
			}
		}
		if(%flag == 666)
			return %flag;
	}


	//--------
	// PASS 3
	//--------
	%flag = True;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "COINS")
		{
			if(Bank::CanPay(%clientId, %w2))
				%flag = True;
			else
				return False;
		}
		else if(%w == "REMORT")
		{
			if(fetchData(%clientId, "RemortStep") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "TOURNYRANK")
		{
			if(fetchData(%clientId, "TournyRank") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "RankPoints")
		{
			if(fetchData(%clientId, "RankPoints") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "AI")
		{
			%isAI = Player::isAIcontrolled(%clientId);
			if(%isAI == %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "EXP")
		{
			if(fetchData(%clientId, "EXP") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(isBeltItem(%w))
		{
			%amnt = Belt::HasThisStuff(%clientid,%w);
			if(%amnt >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w != "COINS" && %w != "REMORT" && %w != "LVLG" && %w != "LVLS" && %w != "LVLE" && %w != "CNT" && %w != "CNTAFFECTS" && %w != "RankPoints" && %w != "AI" && %w != "EXP" && %w != "TOURNYRANK")
		{
			if(Player::getItemCount(%clientId, %w) >= %w2)
				%flag = True;
			else
				return False;
		}
	}

	return %flag;
}

function TakeThisStuff(%clientId, %list, %multiplier)
{
	dbecho($dbechoMode, "TakeThisStuff(" @ %clientId @ ", " @ %list @ ")");

	if(%multiplier == "" || %multiplier <= 0)
		%multiplier = 1;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "COINS")
		{
			if(Bank::CanPay(%clientId, %w2))
				Bank::Pay(%clientId, %w2);
			else
				return False;
		}
		else if(%w == "EXP")
		{
			if(fetchData(%clientId, "EXP") >= %w2)
				storeData(%clientId, "EXP", %w2, "dec");
			else
				return False;
		}
		else if(%w == "CNT" || %w == "CNTAFFECTS" || %w == "LVLG" || %w == "LVLS" || %w == "LVLE")
		{
			//ignore
		}
		else if(isBeltItem(%w))
		{
			%amount = Belt::HasThisStuff(%clientid,%w);
			if(%amount >= %w2)
				Belt::TakeThisStuff(%clientid,%w,%w2);
			else
				return False;
		}
		else
		{
			%amount = Player::getItemCount(%clientId, %w);
			if(%amount >= %w2)
				Player::setItemCount(%clientId, %w, %amount-%w2);
			else
				return False;
		}
	}

	return True;
}

// VOID MIGRATION 2026-07-14: dedup the SPAWN RESTORE against the belt. Belt
// saves (field 48) fire mid-session (every equip calls SaveCharacter) but
// spawnStuff (field 15) only rewrites on a clean logout - so a server killed
// mid-session leaves BOTH a belt save AND the stale pre-conversion armor
// tokens in field 15, and the next spawn double-gives every migrated armor.
// Rule: a spawnStuff token (base or worn-"0" twin) that resolves to a belt-
// registered item the belt ALREADY holds is stale - field 48 is authoritative.
// ONLY for the spawn restore: normal gives (lootbag pickups etc) must still
// stack, and belt buys go through Belt::GiveThisStuff directly anyway.
// VOID CARRY CAP 2026-07-15: returns the first belt item in a loot list that
// would exceed its carry window for this client, or "". Pickup paths use it
// to refuse the WHOLE pickup (bag stays on the ground - nothing is lost, the
// player makes room and grabs it again). Bots are exempt at the call sites.
function Void::LootCapBlocker(%clientId, %list)
{
	for(%i = 0; GetWord(%list, %i) != -1; %i += 2)
	{
		%w = GetWord(%list, %i);
		if(isBeltItem(%w) && Void::AtCarryCap(%clientId, %w))
			return %w;
	}
	return "";
}

function VoidMigrate::FilterSpawnStuff(%clientId, %list)
{
	%out = "";
	for(%i = 0; GetWord(%list, %i) != -1; %i += 2)
	{
		%w  = GetWord(%list, %i);
		%w2 = GetWord(%list, %i + 1);
		%chk = %w;
		if(!isBeltItem(%chk) && String::len(%chk) > 1 && String::getSubStr(%chk, String::len(%chk)-1, 1) == "0" && isBeltItem(String::getSubStr(%chk, 0, String::len(%chk)-1)))
			%chk = String::getSubStr(%chk, 0, String::len(%chk)-1);	// worn twin -> base
		if(isBeltItem(%chk) && Belt::HasThisStuff(%clientId, %chk))
		{
			echo("[VOID MIGRATE] " @ Client::getName(%clientId) @ ": stale spawnStuff token '" @ %w @ " " @ %w2 @ "' dropped - belt already holds '" @ %chk @ "'.");
			continue;
		}
		%out = %out @ %w @ " " @ %w2 @ " ";
	}
	return %out;
}

function GiveThisStuff(%clientId, %list, %echo, %multiplier)
{
	// Initialize %list to empty string if not provided
	if(%list == "")
		%list = "";
	
	// CRITICAL: Validate clientId before proceeding
	// If clientId is False, -1, empty, or invalid, return early to prevent errors
	if(%clientId == "False" || %clientId == "false" || %clientId == -1 || %clientId == "" || %clientId == "0")
	{
		echo("ERROR: GiveThisStuff - Invalid clientId: " @ %clientId @ ". Cannot give items.");
		return;
	}
	
	dbecho($dbechoMode, "GiveThisStuff(" @ %clientId @ ", " @ %list @ ", " @ %echo @ ")");

	%name = Client::getName(%clientId);
	
	// CRITICAL: Validate that we got a valid name (bot/player still exists)
	if(%name == "" || %name == -1)
	{
		echo("ERROR: GiveThisStuff - could not find player object for clientId " @ %clientId @ ". Bot/player may have been deleted.");
		return;
	}

	if(%multiplier == "" || %multiplier <= 0)
		%multiplier = 1;

	%cntindex = 0;

	// VOID 2026-07-15: defer VSlot row pushes for the duration of the give
	// loop - each belt give would otherwise re-push all 28 rows via the DLL
	// (a multi-item lootbag/telekinesis sweep visibly lagged the server).
	// Flushed once after the loop, before the RefreshAll section.
	%clientId.vslotDeferSync = true;

	// CRITICAL: Track if this is spawn equipment for enemy bots (to store in OriginalLootString with percentage format)
	// The roll will happen on death, not on spawn
	%isProcessingSpawnEquipment = false;
	if(isRPGAI(%clientId))
	{
		%isLootbagPickup = (fetchData(%clientId, "IsLootbagPickup") == "true");
		%isProcessingSpawnEquipment = (!%isLootbagPickup && (fetchData(%clientId, "SpawnBotInfo") != ""));
	}

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		
		// Debug logging for LCK processing
		if(isRPGAI(%clientId) && %w == "LCK")
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = "Unknown";
			//echo("[SPAWN DEBUG] GiveThisStuff(): Found LCK in loop - w='" @ %w @ "', w2='" @ %w2 @ "' for bot " @ %botName);
		}

		//if there is a / in %w2, then what trails after the / is the minimum random number between 0 and 100 which
		//is applied as a percentage to the starting number of %w2
		// For AI bots on SPAWN, skip the percentage roll - it will be rolled when the bot dies
		// For AI bots picking up LOOTBAGS, give the items directly (lootbag contents already passed drop chance)
		// For players, roll the percentage immediately
		%spos = String::findSubStr(%w2, "/");
		if(%spos > 0)
		{
			%original = String::getSubStr(%w2, 0, %spos);
			%perc = String::getSubStr(%w2, %spos+1, 99999);

			if(isRPGAI(%clientId))
			{
				// CRITICAL: Check if this is a lootbag pickup using the flag set in Item::onCollision
				// This is more reliable than checking SpawnBotInfo, since bots still have SpawnBotInfo when picking up lootbags
				%isLootbagPickup = (fetchData(%clientId, "IsLootbagPickup") == "true");
				%isSpawnEquipment = (!%isLootbagPickup && (fetchData(%clientId, "SpawnBotInfo") != ""));
				
				// DEBUG: Log when bots receive items via GiveThisStuff
				%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					%botName = fetchData(%clientId, "BotInfoAiName");
					if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
					// Only log quest items, weapons, consumables, and accessories (not LCK, EXP, COINS, etc.)
					if(String::findSubStr(%w, "Spine") != -1 || String::findSubStr(%w, "Fragment") != -1 || 
					   String::findSubStr(%w, "Breath") != -1 || String::findSubStr(%w, "Sword") != -1 ||
					   String::findSubStr(%w, "Potion") != -1 || String::findSubStr(%w, "Vial") != -1 ||
					   String::findSubStr(%w, "AdminBoots") != -1 || String::findSubStr(%w, "Boots") != -1)
					{
					}
				}
				
				if(%isSpawnEquipment)
				{
					// CRITICAL: For AI bots on spawn, give ALL items (no roll on spawn)
					// The roll will happen on death in Player::onKilled
					// Store items in OriginalLootString with percentage format (e.g., "item count/percentage")
					// This allows the death handler to roll once per item
					%w2 = %original;
				}
				else
				{
					// For AI bots picking up lootbags, give the items directly (no percentage roll)
					// Lootbag contents already passed the drop chance when the lootbag was created
					// These items will always drop when the bot dies (handled in playerdamage.cs)
					%w2 = %original;
				}
			}
			else
			{
				// For players, roll the percentage immediately
				// Check if percentage is a range (e.g., "10-15" or "5-10")
				%rangePos = String::findSubStr(%perc, "-");
				%firstChar = String::getSubStr(%perc, 0, 1);
				if(%rangePos > 0 && %firstChar != "-")
				{
					// Variable drop rate range (e.g., 10-15% means roll between 10% and 15%)
					%minPerc = String::getSubStr(%perc, 0, %rangePos);
					%maxPerc = String::getSubStr(%perc, %rangePos + 1, 99999);
					
					// Randomly choose a drop chance within the range
					%chance = %minPerc + floor(getRandom() * (%maxPerc - %minPerc + 1));
					if(%chance > %maxPerc) %chance = %maxPerc;
					
					%roll = floor(getRandom() * 100) + 1;  // Roll 1-100
					
					if(%roll <= %chance)
						%w2 = %original;
					else
						%w2 = 0;
				}
				else if(%perc < 0)
				{
					// Negative percentages indicate rare drops (low chance)
					%absPerc = -%perc;  // Convert -100 to 100
					%roll = floor(getRandom() * 100) + 1;  // Roll 1-100
					
					// For -100, need roll of 100 (1% chance)
					// For -50, need roll >= 51 (50% chance)
					// Formula: need roll >= (100 - absPerc + 1), but for absPerc >= 100, need exact 100
					%minRoll = 100 - %absPerc + 1;
					if(%absPerc >= 100) %minRoll = 100;  // -100 or more rare -> need exactly 100 (1% chance)
					
					if(%roll >= %minRoll)
						%w2 = %original;
					else
						%w2 = 0;
				}
				else
				{
					// Single fixed percentage or "1 in X" format
					// Convert string to number for proper comparison
					%percNum = %perc * 1;
					
					// If number >= 1000, treat as "1 in X" format (e.g., 1/100000 = 1 in 100,000 chance)
					// Otherwise treat as percentage (e.g., 0.001 = 0.001%, 5 = 5%)
					if(%percNum >= 1000)
					{
						// "1 in X" format - roll 1 to X, need exactly 1
						%roll = floor(getRandom() * %percNum) + 1;  // Roll 1 to %percNum
						
						if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %w @ " | Format: 1 in " @ %percNum @ " | Roll: " @ %roll @ " (need roll == 1)");
						
						if(%roll == 1)
						{
							%w2 = %original;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %w @ " will drop (roll " @ %roll @ " == 1)");
						}
						else
						{
							%w2 = 0;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %w @ " will not drop (roll " @ %roll @ " != 1)");
						}
					}
					else if(%percNum < 1)
					{
						// For decimal percentages (e.g., 0.001%), roll 1-100000
						// 0.001% = 1 in 100,000 chance
						%roll = floor(getRandom() * 100000) + 1;  // Roll 1-100000
						%target = %percNum * 1000;  // Convert 0.001 to 1 (for 0.001% = 1 in 100000)
						
						if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %w @ " | Percentage: " @ %percNum @ "% | Roll: " @ %roll @ " | Target: " @ %target @ " (need roll <= " @ %target @ ")");
						
						if(%roll <= %target)
						{
							%w2 = %original;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %w @ " will drop (roll " @ %roll @ " <= " @ %target @ ")");
						}
						else
						{
							%w2 = 0;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %w @ " will not drop (roll " @ %roll @ " > " @ %target @ ")");
						}
					}
					else
					{
						// For normal percentages (>= 1%), roll 1-100
						%roll = floor(getRandom() * 100) + 1;  // Roll 1-100
						
						if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %w @ " | Percentage: " @ %percNum @ "% | Roll: " @ %roll @ " (need roll <= " @ %percNum @ ")");
						
						if(%roll <= %percNum)
						{
							%w2 = %original;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %w @ " will drop (roll " @ %roll @ " <= " @ %percNum @ ")");
						}
						else
						{
							%w2 = 0;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %w @ " will not drop (roll " @ %roll @ " > " @ %percNum @ ")");
						}
					}
				}
				
				if(%w2 < 0) %w2 = 0;
			}
		}

		//if there is a d in %w2 AND it has a number on either side, then it's a dice roll
		%dpos = String::findSubStr(%w2, "d");
		%l1 = String::getSubStr(%w2, %dpos-1, 1);
		%l2 = floor(%l1);
		%r1 = String::getSubStr(%w2, %dpos+1, 1);
		%r2 = floor(%r1);
		if(%dpos > 0 && String::ICompare(%l1, %l2) == 0 && String::ICompare(%r1, %r2) == 0)
		{
			%w2 = GetRoll(%w2);
			if(%w2 < 1) %w2 = 1;
		}

		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "COINS")
		{
			// ASCENSION: Gold Digger - +25% coin drops
			if(Ascension::HasTalent(%clientId, "GoldDigger"))
				%w2 = floor(%w2 * 1.25);

			%w2 = floor(%w2);  // clean integer coins (%multiplier drop scaling can be fractional)
			storeData(%clientId, "COINS", %w2, "inc");
			if(%echo) Client::sendMessage(%clientId, 0, "You received " @ Number::Beautify(%w2, -3) @ " coins.~loot");
		}
		else if(%w == "EXP")
		{
			// Check if player is already at max level for their remort
			%currentLevel = fetchData(%clientId, "LVL");
			%remortStep = fetchData(%clientId, "RemortStep");
			%maxLevel = 125 + (%remortStep * 8);
			
			if(%currentLevel >= %maxLevel)
			{
				// Player is already at max level - don't grant EXP
				if(%echo) 
				{
					%msg = "<jc>You can no longer gain experience. Your body has reached it's peak limit!";
					centerprint(%clientId, %msg, floor(String::len(%msg) / 20));
				}
			}
			else
			{
				// Player is below max level - grant EXP
				%w2 = floor(%w2);  // clean integer exp (%multiplier drop scaling can be fractional)
				storeData(%clientId, "EXP", %w2, "inc");
				if(%echo) Client::sendMessage(%clientId, 0, "You received " @ Number::Beautify(%w2, -3) @ " experience.");
			}
		}
		else if(%w == "LCK")
		{
			// CRITICAL: For seal battle bots, DO NOT overwrite LCK from equipment string
			// Seal battle bots have their LCK set by SealBattle::SetupBot() based on player count
			// This must take precedence over the base LCK from equipment string
			%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
			if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
			{
				// Seal battle bot - skip LCK setting from equipment string
				// SetupBot() will set the correct scaled LCK based on player count
				if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " LCK (ignored for seal battle bot).");
				// Continue to next item without setting LCK
			}
			else
			{
				// For enemy bots on spawn, ALWAYS SET LCK from equipment string (don't increment)
				// This is because clientIds are reused, so LCK from previous bots persists
				// The equipment string represents the bot's base stats, so we should always set it
				%currentLCK = fetchData(%clientId, "LCK");
				%isSpawnPointBot = (fetchData(%clientId, "SpawnBotInfo") != "");
				
				// Debug logging for bots
				if(isRPGAI(%clientId))
				{
					%botName = fetchData(%clientId, "BotInfoAiName");
					if(%botName == "" || %botName == "0") %botName = "Unknown";
					//echo("[SPAWN DEBUG] GiveThisStuff(): Processing LCK for bot " @ %botName @ " (clientId=" @ %clientId @ ")");
					//echo("[SPAWN DEBUG] GiveThisStuff(): currentLCK='" @ %currentLCK @ "', newLCK=" @ %w2 @ ", isSpawnPointBot=" @ %isSpawnPointBot);
				}
				
				// For enemy bots (SpawnPoint bots), always SET LCK from equipment string
				// This ensures bots get their correct base LCK even when clientIds are reused
				if(isRPGAI(%clientId) && %isSpawnPointBot)
				{
					// Enemy bot spawn - always set LCK directly from equipment string
					storeData(%clientId, "LCK", %w2);
					if(isRPGAI(%clientId))
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = "Unknown";
						//echo("[SPAWN DEBUG] GiveThisStuff(): SET LCK to " @ %w2 @ " for enemy bot " @ %botName @ " (was '" @ %currentLCK @ "')");
						%verifyLCK = fetchData(%clientId, "LCK");
						//echo("[SPAWN DEBUG] GiveThisStuff(): Verified LCK='" @ %verifyLCK @ "'");
					}
				}
				else
				{
					// Player or town bot receiving additional LCK - increment
					storeData(%clientId, "LCK", %w2, "inc");
					if(isRPGAI(%clientId))
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = "Unknown";
						//echo("[SPAWN DEBUG] GiveThisStuff(): INCREMENTED LCK by " @ %w2 @ " for bot " @ %botName @ " (current was '" @ %currentLCK @ "')");
						%verifyLCK = fetchData(%clientId, "LCK");
						//echo("[SPAWN DEBUG] GiveThisStuff(): Verified LCK='" @ %verifyLCK @ "'");
					}
				}
				if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " LCK.");
			}
		}
		else if(%w == "SP")
		{
			storeData(%clientId, "SPcredits", %w2, "inc");
			if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " Skill Points.");
		}
		else if(%w == "CLASS")
		{
			storeData(%clientId, "CLASS", %w2);
			storeData(%clientId, "GROUP", $ClassGroup[fetchData(%clientId, "CLASS")]);
		}
		else if(%w == "LVL")
		{
			//note: the class MUST be specified in %stuff prior to this call
			storeData(%clientId, "EXP", GetExp(%w2, %clientId) + 100);
		}
		else if(%w == "REMORT")
		{
			//note: the class MUST be specified in %stuff prior to this call
			storeData(%clientId, "RemortStep", %w2);
		}
		else if(%w == "TOURNYRANK")
		{
			//note: the class MUST be specified in %stuff prior to this call
			storeData(%clientId, "TournyRank", %w2);
		}
		else if(%w == "TEAM")
		{
			GameBase::setTeam(%clientId, %w2);
			if(%echo) Client::sendMessage(%clientId, 0, "Team set to " @ %w2 @ ".");
		}
		else if(%w == "RankPoints")
		{
			storeData(%clientId, "RankPoints", %w2, "inc");
			if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " Rank Points.");
		}
		else if(%w == "CNT")
		{
			%cntindex++;
			%tmpcnt[%cntindex] = %w2;
		}
		else if(%w == "CNTAFFECTS")
		{
			%tmpcntaffects[%cntindex] = %w2;
		}
		else if(String::len(%w) > 1 && !isBeltItem(%w) && String::getSubStr(%w, String::len(%w)-1, 1) == "0" && isBeltItem(String::getSubStr(%w, 0, String::len(%w)-1)))
		{
			// VOID MIGRATION 2026-07-14: a saved WORN-armor token ("Xxx0 1" - the
			// Equipped-class twin, itemevents.cs equip swap) whose BASE name is now
			// belt-registered. Give the base to the belt and remember it for a
			// re-equip after the loop (equipping inline would SaveCharacter with a
			// half-restored inventory). Idempotent: post-migration saves never
			// contain X0 tokens again, and the equip only fires onto an empty slot.
			%vmBase = String::getSubStr(%w, 0, String::len(%w)-1);
			if(%w2 != "" && (%w2 * 1) > 0)
			{
				Belt::GiveThisStuff(%clientId, %vmBase, %w2, false);
				echo("[VOID MIGRATE] " @ %name @ ": worn '" @ %w @ "' x" @ %w2 @ " -> belt '" @ %vmBase @ "'");
				if($BeltItem[%vmBase, "Type"] == "Armor" && %voidEquipArmor == "")
					%voidEquipArmor = %vmBase;
				// VOID ACCESSORIES 2026-07-15: worn accessories re-equip too - a
				// player wears several at once (helmet+boots+shield), so collect
				// a list; Belt::EquipAccessory enforces $maxAccessory per type.
				else if($BeltItem[%vmBase, "Type"] == "Accessories")
					%voidEquipAccs = %voidEquipAccs @ %vmBase @ " ";
			}
		}
		else if(isBackpackItem(%w))
		{
			// Validate item and count before giving to belt
			// Skip invalid entries: empty item, item "0", empty count, count -1, count <= 0
			if(%w != "" && %w != -1 && %w != "0" && %w2 != "" && %w2 != -1 && %w2 != "-1" && (%w2 * 1) > 0)
			{
				// DEBUG: Log when bots receive belt items via GiveThisStuff (isBackpackItem path)
				if(isRPGAI(%clientId))
				{
					%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
						%beltType = $BeltItem[%w, "Type"];
					}
				}
				Belt::GiveThisStuff(%clientId, %w, %w2, %echo);
			}
		}
		else
		{
			// Get player object - Item::giveItem expects a player object, not a clientId
			%player = Client::getOwnedObject(%clientId);
			
			// CRITICAL: Validate player object exists before calling Player::getItemCount or Item::giveItem
			if(%player == -1 || %player == "" || !isObject(%player))
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				if(%botName == "")
					%botName = fetchData(%clientId, "SpawnBotInfo");
				if(%botName == "")
					%botName = Client::getName(%clientId);
				echo("[DEBUG getItemCount] GiveThisStuff - Player object not found, clientId: " @ %clientId @ ", bot: " @ %botName @ ", item: " @ %w @ ", count: " @ %w2);
				// Skip giving item if player object doesn't exist
				continue;
			}
			
			// DEBUG: Log when bots receive weapons via GiveThisStuff
			if(isRPGAI(%clientId))
			{
				%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					%botName = fetchData(%clientId, "BotInfoAiName");
					if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
					%itemData = getItemData(%w);
					if(%itemData.className == "Weapon" || String::findSubStr(%w, "Sword") != -1 || String::findSubStr(%w, "Blade") != -1)
					{
						// CRITICAL: Re-validate player object right before Player::getItemCount call
						%playerCheck = Client::getOwnedObject(%clientId);
						if(%playerCheck != -1 && %playerCheck != "" && isObject(%playerCheck))
						{
							%currentWeaponCount = Player::getItemCount(%clientId, %w);
						}
					}
				}
			}
			//echo("[SPAWN DEBUG] GiveThisStuff(): clientId=" @ %clientId @ ", player=" @ %player @ ", item=" @ %w @ ", count=" @ %w2);
			// VOID 2026-07-14: discard VSlot placeholder tokens from poisoned saves
			// (saved before the updateSpawnStuff exclusion existed). Giving a
			// placeholder count outside VSlot::Sync corrupts the row display and
			// crashed the server on login.
			if(VSlot::IsSlotItem(%w))
			{
				echo("GiveThisStuff: skipping VSlot placeholder token '" @ %w @ "' x" @ %w2 @ " for " @ %name @ " (display row, not an item)");
				continue;
			}
			// SLOT PURGE 2026-07-13: defensive skip for names that no longer resolve to a
			// registered ItemData (codebase convention: .description reads False for
			// non-datablocks, see WhatIs/Belt::WhatIs). Protects old saves that still hold a
			// since-removed item (e.g. Grenade/RepairPatch) - degrades to a console line
			// instead of handing a dead name to Item::giveItem. Covers all future removals.
			if(%w.description == False)
			{
				echo("GiveThisStuff: skipping unknown item '" @ %w @ "' x" @ %w2 @ " for " @ %name @ " (datablock no longer registered)");
				continue;
			}
			if(%player != "" && %player != -1)
			{
				%result = Item::giveItem(%player, %w, %w2, %echo);
				
				// Weapons are given through Item::giveItem
				// OriginalLootString is already set in Ai.cs with percentage format, so no need to track here
				//echo("[SPAWN DEBUG] GiveThisStuff(): Item::giveItem() returned=" @ %result);
			}
			else
			{
				echo("ERROR: GiveThisStuff - could not find player object for clientId " @ %clientId);
			}
		}
	}

	// VOID 2026-07-15: flush the deferred VSlot sync - one row push for the
	// whole give instead of one per item (see the defer set before the loop).
	%clientId.vslotDeferSync = "";
	if(%clientId.vslotSyncPending)
	{
		%clientId.vslotSyncPending = "";
		VSlot::Sync(%clientId);
	}

	// VOID MIGRATION 2026-07-14: re-equip the migrated worn armor now that the whole
	// inventory is restored (deferred from the X0 branch above). Only fires when
	// nothing is already equipped, so it can never override a player's later choice.
	if(%voidEquipArmor != "")
	{
		%vmCur = fetchData(%clientId, "EquippedBeltArmor");
		if(%vmCur == "" || %vmCur == "0" || %vmCur == -1)
			Belt::EquipArmor(%clientId, %voidEquipArmor);
	}
	// VOID ACCESSORIES 2026-07-15: re-equip migrated worn accessories (deferred
	// list). Belt::EquipAccessory itself enforces the per-type $maxAccessory
	// slots and refuses politely, so this can't over-equip.
	if(%voidEquipAccs != "")
	{
		for(%vmI = 0; (%vmA = GetWord(%voidEquipAccs, %vmI)) != -1; %vmI++)
			Belt::EquipAccessory(%clientId, %vmA);
	}

	// CRITICAL: For enemy bots, use RefreshAllEnemyBot() which does NOT touch team
	// For players and other bots (town bots, seal battle bots), use RefreshAll()
	%isEnemyBot = false;
	if(isRPGAI(%clientId))
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
			// Only treat as enemy bot if it's NOT a seal battle bot
			// Seal battle bots need full RefreshAll() calculations for scaled stats
			if(%isSealBattleBot != "true" && %isSealBattleBot != "True" && %isSealBattleBot != "1")
			{
				%isEnemyBot = true;
			}
		}
	}
	
	if(%isEnemyBot)
	{
		// Enemy bot - use RefreshAllEnemyBot() which does NOT touch team
		RefreshAllEnemyBot(%clientId);
	}
	else
	{
		// Player or other bot type - use normal RefreshAll()
		RefreshAll(%clientId);
	}
	
	// CRITICAL: OriginalLootString is already set in Ai.cs with percentage format (e.g., "item count/percentage")
	// The roll will happen on death in Player::onKilled, not on spawn
	// Items not in OriginalLootString but in belt are from lootbags and will always drop 100%

	//Process the counter data, if any
	for(%i = 1; %tmpcnt[%i] != ""; %i++)
	{
		if(%tmpcnt[%i] != "" && %tmpcntaffects[%i] != "")
		{
			%first = String::getSubStr(%tmpcnt[%i], 0, 1);
			if(%first == "+" || %first == "-")
				$QuestCounter[%name, %tmpcntaffects[%i]] += floor(%tmpcnt[%i]);
			else
				$QuestCounter[%name, %tmpcntaffects[%i]] = floor(%tmpcnt[%i]);
		}
	}
}
	
function getSpawnIndex(%aiName)
{
	dbecho($dbechoMode, "getSpawnIndex(" @ %aiName @ ")");

	// review #41: scan a FIXED range, not "until empty". $spawnIndex is SPARSE (gaps at
	// 7, 9, 28, 114-119, 129...), so the old "while $spawnIndex[%i] != ''" loop stopped
	// dead at the first gap (index 7) and could never find any of the ~115 real enemy
	// types at index 8+. (Currently has no callers - this removes the latent landmine.)
	for(%i = 1; %i <= 200; %i++)
	{
		if($spawnIndex[%i] != "" && $spawnIndex[%i] == %aiName)
			return %i;
	}
	return -1;
}

function FellOffMap(%id)
{
	dbecho($dbechoMode, "FellOffMap(" @ %id @ ")");

	RefreshAll(%id);

	if(Player::isAiControlled(%id))
	{
		storeData(%id, "noDropLootbagFlag", True);
		playNextAnim(%id);
		Player::Kill(%id);
	}
	else
	{
		CheckAndBootFromArena(%id);
		Item::setVelocity(%id, "0 0 0");
		TeleportToMarker(%id, "TheArena\\TeleportExitMarkers", 0, 0);

		Client::sendMessage(%id, $MsgRed, "You were restored to the arena exit marker.");
	}
}

function SetOnGround(%clientId, %extraZ)
{
	dbecho($dbechoMode, "SetOnGround(" @ %clientId @ ", " @ %extra2 @ ")");

	%maxdist = 5000;

	%origpos = GameBase::getPosition(%clientId);

	%x = GetWord(%origpos, 0);
	%y = GetWord(%origpos, 1);
	%z = GetWord(%origpos, 2);

	%finalpos = %x @ " " @ %y @ " " @ %z + %extraZ;

	GameBase::setPosition(%clientId, %finalpos);

	%index = 0;
	//for(%i = 0; %i >= -3.15; %i -= 1.57)
	for(%i = 0; %i >= -4.725; %i -= 0.785)
	{
		if(GameBase::getLOSinfo(Client::getOwnedObject(%clientId), %maxdist, %i @ " 0 0"))
		{
			%index++;
			%pos[%index] = $los::position;
		}
	}

	%closest = %maxdist+1;
	for(%j = 1; %j <= %index; %j++)
	{
		%dist = Vector::getDistance(%pos[%j], %finalpos);
		if(%dist < %closest)
		{
			%closest = %dist;
			%closestIndex = %j;
		}
	}

	if(%pos[%closestIndex] != "")
		GameBase::setPosition(%clientId, %pos[%closestIndex]);
	else
		GameBase::setPosition(%clientId, %origpos);

	return %pos[%closestIndex];
}

function WalkSlowInvisLoop(%clientId, %delay, %grace)
{
	dbecho($dbechoMode, "WalkSlowInvisLoop(" @ %clientId @ ", " @ %delay @ ", " @ %grace @ ")");

	%pos = GameBase::getPosition(%clientId);
	if(fetchData(%clientId, "lastPos") == "")
		storeData(%clientId, "lastPos", %pos);

	if(Vector::getDistance(%pos, fetchData(%clientId, "lastPos")) <= %grace && fetchData(%clientId, "invisible"))
	{
		storeData(%clientId, "lastPos", GameBase::getPosition(%clientId));
		schedule("WalkSlowInvisLoop(" @ %clientId @ ", " @ %delay @ ", " @ %grace @ ");", %delay, %clientId);
	}
	else
	{
		if(fetchData(%clientId, "invisible"))
			UnHide(%clientId);

		Client::sendMessage(%clientId, $MsgRed, "You are no longer Hiding In Shadows.");

	}
}
function UnHide(%clientId)
{
	dbecho($dbechoMode, "UnHide(" @ %clientId @ ")");

	if(fetchData(%clientId, "invisible"))
	{
		GameBase::startFadeIn(%clientId);
		storeData(%clientId, "invisible", "");
	}

	storeData(%clientId, "lastPos", "");
	storeData(%clientId, "blockHide", True);
	schedule("storeData(" @ %clientId @ ", \"blockHide\", \"\");", 10);
}

function AddToTargetList(%clientId, %cl)
{
	dbecho($dbechoMode, "AddToTargetList(" @ %clientId @ ", " @ %cl @ ")");

	%name = Client::getName(%cl);
	if(!IsInCommaList(fetchData(%clientId, "targetlist"), %name))
	{
		storeData(%clientId, "targetlist", AddToCommaList(fetchData(%clientId, "targetlist"), %name));

		Client::sendMessage(%cl, $MsgRed, Client::getName(%clientId) @ " wants you dead!  Travel carefully!");
		Client::sendMessage(%clientId, $MsgRed, %name @ " has been notified of your intentions.");

		schedule("RemoveFromTargetList(" @ %clientId @ ", " @ %cl @ ");", 10 * 60);
	}
}
function RemoveFromTargetList(%clientId, %cl)
{
	dbecho($dbechoMode, "RemoveFromTargetList(" @ %clientId @ ", " @ %cl @ ")");

	%name = Client::getName(%cl);
	if(IsInCommaList(fetchData(%clientId, "targetlist"), %name))
	{
		storeData(%clientId, "targetlist", RemoveFromCommaList(fetchData(%clientId, "targetlist"), %name));

		Client::sendMessage(%cl, $MsgBeige, Client::getName(%clientId) @ " was forced to declare a truce.");
		Client::sendMessage(%clientId, $MsgBeige, %name @ " has expired on your target-list.");
	}
}

function AddBounty(%clientId, %amt)
{
	dbecho($dbechoMode, "AddBounty(" @ %clientId @ ", " @ %amt @ ")");

	%b = fetchData(%clientId, "bounty") + %amt;
	storeData(%clientId, "bounty", Cap(%b, 0, 100000000));

	return fetchData(%clientId, "bounty");
}

// TEMPORARILY DISABLED - PostSteal function
//function PostSteal(%clientId, %success, %type, %targetId)
//{
//	dbecho($dbechoMode, "PostSteal(" @ %clientId @ ", " @ %success @ ", " @ %type @ ")");
//
//	if(%type == 0)
//	{
//		//regular steal
//		if(%success)
//			AddBounty(%clientId, (5.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//		else
//			AddBounty(%clientId, (5.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//	}
//	else if(%type == 1)
//	{
//		//pickpocket
//		if(%success)
//			AddBounty(%clientId, (10 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//		else
//			AddBounty(%clientId, (10.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//	}
//	else if(%type == 2)
//	{
//		//mug
//		if(%success)
//			AddBounty(%clientId, (15.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//		else
//			AddBounty(%clientId, (15.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//	}
//
//	if(%success)
//		UpdateBonusState(%clientId, "Theft 1", 20 / 2);
//	else
//		UpdateBonusState(%clientId, "Theft 1", 120 / 2);
//}

function GetTypicalTossStrength(%clientId)
{
	dbecho($dbechoMode, "GetTypicalTossStrength(" @ %clientId @ ")");

	if(fetchData(%clientId, "RACE") == "DeathKnight")
	{
		%toss = 10;
	}
	else
	{
		%a = Player::getArmor(%clientId);
		%b = String::getSubStr(%a, String::len(%a)-1, 1);
		%toss = Cap($speed[fetchData(%clientId, "RACE"), %b]-2, 3, 10);
	}

	return %toss;
}

// TEMPORARILY DISABLED - AllowedToSteal function
//function AllowedToSteal(%clientId)
//{
//	dbecho($dbechoMode, "AllowedToSteal(" @ %clientId @ ")");
//
//	if(fetchData(%clientId, "InSleepZone") != "")
//		return "You can't steal inside a sleeping area.";
//	//else if(Zone::getType(fetchData(%clientId, "zone")) == "PROTECTED")
//	//	return "You can't steal from someone in protected territory.";
//
//	return "True";
//}

// TEMPORARILY DISABLED - PerhapsPlayStealSound function
//function PerhapsPlayStealSound(%clientId, %type)
//{
//	dbecho($dbechoMode, "PerhapsPlayStealSound(" @ %clientId @ ", " @ %type @ ")");
//
//	if(%type == 0)
//		%snd = SoundMoney1;
//	else if(%type == 1)
//		%snd = SoundPickupItem;
//	else if(%type == 2)
//		%snd = SoundPickupItem;
//
//	%r = getRandom() * 1000;
//	%n = 1000 - $PlayerSkill[%clientId, $SkillStealing];
//	if(%r <= %n)
//	{
//		playSound(%snd, GameBase::getPosition(%clientId));
//		return True;
//	}
//	else
//		return False;
//}

function GetLCKcost(%clientId)
{
	dbecho($dbechoMode, "GetLCKcost(" @ %clientId @ ")");

	%a = floor( pow(2, Cap(fetchData(%clientId, "LCK"), 0, 26)) * 15 ) + 100;

	return Cap(%a, 0, "inf");
}
function Getrpcost(%clientId)
{
	dbecho($dbechoMode, "Getrpcost(" @ %clientId @ ")");

	%a = floor( pow(2, Cap(fetchData(%clientId, "RankPoints"), 0, 26)) * 7 ) + 100;

	return Cap(%a, 0, "inf");
}

function UnequipMountedStuff(%clientId)
{
	dbecho($dbechoMode, "UnequipMountedStuff(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before calling Player::getItemCount
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return; // Player object doesn't exist

	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		// Re-validate player object in case it gets despawned during the loop
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == "" || %playerObj == -1)
			break; // Exit loop if player object no longer exists
		
		%a = getItemData(%i);
		%itemcount = Player::getItemCount(%clientId, %a);

		if(%itemcount)
		{
			if(%a.className == "Equipped")
			{
				%b = String::getSubStr(%a, 0, String::len(%a)-1);
				// Remove ALL equipped items, not just one
				Player::decItemCount(%clientId, %a, %itemcount);
				Player::incItemCount(%clientId, %b, %itemcount);
			}
			else if(Player::getMountedItem(%clientId, $WeaponSlot) == %a)
			{
				Player::unMountItem(%clientId, $WeaponSlot);
			}
		}
	}
}

function TownBot_PlayFarewell(%playerClientId, %botClientId)
{
	if(%playerClientId == "" || %botClientId == "")
		return;

	// Check if a session voice was already assigned during greeting
	%voice = %botClientId.sessionVoice;
	
	// ROBUSTNESS CHECK: Verify voice is valid (must contain "male" or "female")
	// This handles cases where sessionVoice might be empty, "0", or undefined
	if(String::findSubStr(%voice, "male") == -1 && String::findSubStr(%voice, "female") == -1)
	{
		// Fallback: Randomize voice if no valid session voice exists
		%botArmor = Player::getArmor(%botClientId);
		%rand = floor(getRandom() * 5) + 1; // Random 1-5
		
		%voice = "male" @ %rand; // Default to male 1-5
		if(String::findSubStr(%botArmor, "female") != -1)
			%voice = "female" @ %rand; // Female 1-5
	}
	
	// Randomly choose between "No Problem" (wnoprob) and "Bye" (wbye)
	%suffix = "wbye";
	if(floor(getRandom() * 2) == 1) // 50% chance
		%suffix = "wnoprob";
	
	Client::sendMessage(%playerClientId, 0, "~w" @ %voice @ "." @ %suffix @ ".wav");
}
