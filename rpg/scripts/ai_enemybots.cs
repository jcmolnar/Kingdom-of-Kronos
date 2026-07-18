//============================================================================
// ai_enemybots.cs — split from Ai.cs (scatter split, ai.cs peel)
// Extracted 2026-07-18 at commit 320698f. Mechanical text move, no behavior change.
// Hostile-bot combat AI: targeting/attack loops, weapon select, movement, skills, LOS handlers, enemy team enforcement. Serves ALL hostile bots (SpawnPoint enemies, TempSpawn event bots, turned-evil pets) — never townbots.
// exec'd by Ai.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====

//------------------------------
// AI::setWeapons()
//------------------------------
function AI::setWeapons(%aiName, %loadout)
{
	Watchdog_Enter("AI::setWeapons");
	dbecho($dbechoMode, "AI::setWeapons(" @ %aiName @ ")");
	%currentTime = getSimTime();
	if($AI_PERIODIC_DEBUG)
		echo("[INERT DEBUG] AI::setWeapons: ENTRY @ " @ %currentTime @ " - aiName=" @ %aiName @ ", loadout=" @ %loadout);

	// CRITICAL: Use getClientIdFromName() instead of getId() to minimize error spam
	// getClientIdFromName() checks BotInfoAiName first (for Player objects) before calling getId()
	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate that we got a valid ID
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
	{
		// Bot doesn't exist - likely died before scheduled call executed
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG)
			echo("[INERT DEBUG] AI::setWeapons: getClientIdFromName returned invalid ID (-1/empty/False) for " @ %aiName @ " - returning early");
		return;
	}
	if($AI_PERIODIC_DEBUG)
		echo("[INERT DEBUG] AI::setWeapons: getClientIdFromName returned " @ %aiId @ " for " @ %aiName);
	
	// CRITICAL: Validate that player object still exists before proceeding
	%playerObj = Client::getOwnedObject(%aiId);
	if($AI_PERIODIC_DEBUG)
		echo("[INERT DEBUG] AI::setWeapons: Client::getOwnedObject(" @ %aiId @ ") returned: " @ %playerObj);
	
	// CRITICAL FIX: Fallback if engine hasn't updated getOwnedObject yet
	if(%playerObj == -1 || %playerObj == "")
	{
		if($AI_PERIODIC_DEBUG)
			echo("[INERT DEBUG] AI::setWeapons: Player object missing from Client::getOwnedObject, trying FindPlayerInBotGroup() fallback");
		%playerObj = FindPlayerInBotGroup(%aiId);
		if($AI_PERIODIC_DEBUG)
			echo("[INERT DEBUG] AI::setWeapons: FindPlayerInBotGroup(" @ %aiId @ ") returned: " @ %playerObj);
		
		// Check BotGroup status for debugging
		if(!isObject("BotGroup"))
		{
			if($AI_PERIODIC_DEBUG)
				echo("[INERT DEBUG] AI::setWeapons: WARNING - BotGroup does not exist when fallback is needed!");
		}
		else
		{
			%botGroupCount = Group::objectCount(BotGroup);
			if($AI_PERIODIC_DEBUG)
				echo("[INERT DEBUG] AI::setWeapons: BotGroup exists with " @ %botGroupCount @ " objects");
		}
	}
		
	if(%playerObj == -1 || %playerObj == "")
	{
		if($AI_PERIODIC_DEBUG)
			echo("[INERT DEBUG] AI::setWeapons: Player object still missing after fallback - retrying in 1.0s");
		if($AI_DEBUG_ENABLED)
			echo("[AI WARNING] AI::setWeapons: Player object missing for " @ %aiName @ " (id=" @ %aiId @ "). Retrying in 1.0s...");
		schedule("AI::setWeapons(\"" @ %aiName @ "\", \"" @ %loadout @ "\");", 1.0);
		return;
	}
	if($AI_PERIODIC_DEBUG)
		echo("[INERT DEBUG] AI::setWeapons: Player object validated (playerObj=" @ %playerObj @ "), proceeding");
	if($AI_DEBUG_ENABLED)
		echo("[AI DEBUG] AI::setWeapons: Applying weapons to " @ %aiName @ " (playerObj=" @ %playerObj @ ")");

	// CRITICAL: Re-apply armor to ensure hitbox properties are correctly set
	// AI::spawn() sets the armor, but the hitbox may not be properly initialized for Player objects
	// This explicit call to Player::setArmor() ensures the armor datablock's hitbox properties are applied
	%currentArmor = Player::getArmor(%aiId);
	if(%currentArmor != "" && %currentArmor != -1)
	{
		Player::setArmor(%aiId, %currentArmor);
		
		// Randomized Appearance Skin Enforcement
		%guardtype = StripTrailingDigits(%aiName);
		if($BotRandomizeAppearance[%guardtype])
		{
			%skin = $ArmorToSkin[%currentArmor];
			if(%skin != "")
			{
				Safe_SetSkin(%aiId, %skin, "AI::setWeapons rotating skin setup");
			}
		}
	}


	// CRITICAL: Ensure team is set BEFORE GiveThisStuff() runs
	// GiveThisStuff() calls RefreshAllEnemyBot() for enemy bots (which does NOT touch team)
	// GameBase::setTeam() might not be synchronous, so set it right before GiveThisStuff()
	// CRITICAL: Use the stored botTeam value (set in SpawnAIGetClientId from $BotInfo[botName, TEAM])
	// Different bot types have different teams - do NOT hardcode to 11!
	%storedBotTeam = fetchData(%aiId, "botTeam");
	// NOTE: team 0 (Citizen) is a VALID stored team - TempSpawn bots can be friendly.
	// Unset is "" (storeData never writes a stray 0 into botTeam), so only ""/-1 fall through.
	if(%storedBotTeam != "" && %storedBotTeam != -1)
	{
		%currentTeam = GameBase::getTeam(%aiId);
		if(%currentTeam != %storedBotTeam)
		{
			// Team is wrong - set it before GiveThisStuff() calls RefreshAllEnemyBot()
			GameBase::setTeam(%aiId, %storedBotTeam);
			%playerObjForTeam = Client::getOwnedObject(%aiId);
			if(%playerObjForTeam != -1 && %playerObjForTeam != "")
				GameBase::setTeam(%playerObjForTeam, %storedBotTeam);
			if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] AI::setWeapons - Set team to " @ %storedBotTeam @ " for " @ %aiName @ " (clientId=" @ %aiId @ ", was " @ %currentTeam @ ") before GiveThisStuff()");
		}
	}
	else
	{
		// botTeam not stored - try to get from BotInfo (should have been set in SpawnAIGetClientId)
		%botInfoTeam = $BotInfo[%aiName, TEAM];
		// CRITICAL FIX: Allow team 0 (Town) to be valid! Removed checks for 0.
		if(%botInfoTeam != "" && %botInfoTeam != -1)
		{
			// Store it and set the team
			storeData(%aiId, "botTeam", %botInfoTeam);
			%currentTeam = GameBase::getTeam(%aiId);
			if(%currentTeam != %botInfoTeam)
			{
				GameBase::setTeam(%aiId, %botInfoTeam);
				%playerObjForTeam = Client::getOwnedObject(%aiId);
				if(%playerObjForTeam != -1 && %playerObjForTeam != "")
					GameBase::setTeam(%playerObjForTeam, %botInfoTeam);
				if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] AI::setWeapons - Set team to " @ %botInfoTeam @ " for " @ %aiName @ " (clientId=" @ %aiId @ ", was " @ %currentTeam @ ") from BotInfo before GiveThisStuff()");
			}
		}
		else
		{
			%teamToSet = 1; // Default to enemy
			
			// CRITICAL FIX: Check if it's a town bot before defaulting to team 1
			if(String::findSubStr(%aiName, "TownBot_") == 0)
			{
				echo("WARNING: AI::setWeapons - No team info for town bot " @ %aiName @ ". Defaulting to team 0.");
				%teamToSet = 0;
			}
			else
			{
				// No team info available - default to team 1 (enemy) but log warning
				echo("WARNING: AI::setWeapons - No team info for bot " @ %aiName @ " (clientId=" @ %aiId @ "). Defaulting to team 1.");
			}
			
			storeData(%aiId, "botTeam", %teamToSet);
			GameBase::setTeam(%aiId, %teamToSet);
			%playerObjForTeam = Client::getOwnedObject(%aiId);
			if(%playerObjForTeam != -1 && %playerObjForTeam != "")
				GameBase::setTeam(%playerObjForTeam, %teamToSet);
		}
	}
	
	if(%loadout == -1 || %loadout == "" || String::ICompare(%loadout, "default") == 0)
	{
		%items = $BotInfo[%aiName, ITEMS];
		//echo("[SPAWN DEBUG] AI::setWeapons(): BotInfo[" @ %aiName @ ", ITEMS]=" @ %items);
		if(%items == "")
		{
			// Extract guardtype by removing trailing digits using global function
			%guardtype = StripTrailingDigits(%aiName);
			%equipString = $BotEquipment[%guardtype];
			//echo("[SPAWN DEBUG] AI::setWeapons(): guardtype=" @ %guardtype @ ", BotEquipment[" @ %guardtype @ "]=" @ %equipString);
			if(%equipString != "")
			{
				// CRITICAL: Re-validate player object exists before calling GiveThisStuff
				// Bot may have been deleted between validation check and this point
				%playerObjCheck = Client::getOwnedObject(%aiId);
				if(%playerObjCheck == -1 || %playerObjCheck == "")
				{
					// Bot was deleted - silently return
					return;
				}
				
				// Store original equipment string with percentage chances for lootbag generation
				storeData(%aiId, "OriginalLootString", %equipString);
				if($AI_PERIODIC_DEBUG)
					echo("[INERT DEBUG] AI::setWeapons: Calling GiveThisStuff with equipString (guardtype=" @ %guardtype @ ")");
				
				// CRITICAL FIX: Ensure skin is set before GiveThisStuff for TOWN BOTS only
				// Enemy bots typically have empty %currentArmor or use different skin logic
				if(%currentArmor != "" && %currentArmor != -1 && String::findSubStr(%aiName, "TownBot_") == 0)
					Safe_SetSkin(%aiId, $Server::teamSkin[0], "AI::setWeapons town bot guardtype");
					
				//echo("[SPAWN DEBUG] AI::setWeapons(): Calling GiveThisStuff with equipString=" @ %equipString);
				GiveThisStuff(%aiId, %equipString, False);
			}
			else
			{
				echo("WARNING: AI::setWeapons: Bot " @ %aiName @ " (guardtype: " @ %guardtype @ ") - No equipment string found in $BotEquipment[" @ %guardtype @ "]");
			}
		}
		else
		{
			// CRITICAL: Re-validate player object exists before calling GiveThisStuff
			// Bot may have been deleted between validation check and this point
			%playerObjCheck = Client::getOwnedObject(%aiId);
			if(%playerObjCheck == -1 || %playerObjCheck == "")
			{
				// Bot was deleted - silently return
				return;
			}
			
			// Store original items string with percentage chances for lootbag generation
			storeData(%aiId, "OriginalLootString", %items);
			if($AI_PERIODIC_DEBUG)
				echo("[INERT DEBUG] AI::setWeapons: Calling GiveThisStuff with items from BotInfo");
			//echo("[SPAWN DEBUG] AI::setWeapons(): Calling GiveThisStuff with items=" @ %items);
			
			// CRITICAL FIX: Ensure skin is set before GiveThisStuff for TOWN BOTS only
			if(%currentArmor != "" && %currentArmor != -1 && String::findSubStr(%aiName, "TownBot_") == 0)
				Safe_SetSkin(%aiId, $Server::teamSkin[0], "AI::setWeapons town bot items");
				
			GiveThisStuff(%aiId, %items, False);
		}
	}
	else
	{
		%loadoutString = $LoadOut[%loadout];
		//echo("[SPAWN DEBUG] AI::setWeapons(): LoadOut[" @ %loadout @ "]=" @ %loadoutString);
		// Store original loadout string with percentage chances for lootbag generation
		if(%loadoutString != "")
		{
			// CRITICAL: Re-validate player object exists before calling GiveThisStuff
			// Bot may have been deleted between validation check and this point
			%playerObjCheck = Client::getOwnedObject(%aiId);
			if(%playerObjCheck == -1 || %playerObjCheck == "")
			{
				// Bot was deleted - silently return
				return;
			}
			
			storeData(%aiId, "OriginalLootString", %loadoutString);
			if($AI_PERIODIC_DEBUG)
				echo("[INERT DEBUG] AI::setWeapons: Calling GiveThisStuff with loadoutString (loadout=" @ %loadout @ ")");
			//echo("[SPAWN DEBUG] AI::setWeapons(): Calling GiveThisStuff with loadoutString=" @ %loadoutString);
		
		// CRITICAL FIX: Ensure skin is set before GiveThisStuff for TOWN BOTS only
		if(%currentArmor != "" && %currentArmor != -1 && String::findSubStr(%aiName, "TownBot_") == 0)
			Safe_SetSkin(%aiId, $Server::teamSkin[0], "AI::setWeapons town bot loadout");
			
		GiveThisStuff(%aiId, %loadoutString, False);
		}
		else
		{
			//echo("[SPAWN DEBUG] AI::setWeapons(): WARNING - LoadOut[" @ %loadout @ "] is empty");
		}
	}

	HardcodeAIskills(%aiId);

	Game::refreshClientScore(%aiId);
  
	if($AI_PERIODIC_DEBUG)
		echo("[INERT DEBUG] AI::setWeapons: Setting AI engine variables - triggerPct=1.0, iq=100, attackMode=" @ $AIattackMode);
	AI::SetVar(%aiName, triggerPct, 1.0 );
	AI::setVar(%aiName, iq, 100 );
	AI::setVar(%aiName, attackMode, $AIattackMode);
	AI::setAutomaticTargets( %aiName );

	if($AI_PERIODIC_DEBUG)
		echo("[INERT DEBUG] AI::setWeapons: Scheduling callbackPeriodic for " @ %aiName @ " (5s interval)");
	ai::callbackPeriodic(%aiName, 5, AI::Periodic);

	AI::SelectBestWeapon(%aiId);	//this way the bot spawns and has a weapon in hand
	
	// Schedule SpotDist update after a delay to ensure weapon is mounted
	// This is a backup in case AI::SelectBestWeapon() didn't set it correctly
	schedule("AI::SetSpotDist(" @ %aiId @ ");", 0.3);
}


// Continuous attack loop for enemy bots
// This function triggers attacks at the correct weapon speed
function AI::ContinuousAttack(%aiName, %targetId)
{
	dbecho($dbechoMode, "AI::ContinuousAttack(" @ %aiName @ ", " @ %targetId @ ")");

	if($MeleeDebug)
		echo("[MELEE DEBUG] ContinuousAttack ENTRY " @ %aiName @ " -> target " @ %targetId);

	// Get bot info
	%aiId = AI::getClientIdFromName(%aiName);
	if(%aiId == -1 || %aiId == "")
		return; // Bot doesn't exist
	
	// If bot is still spawn-invulnerable / not loaded, do not attack
	if(fetchData(%aiId, "SpawnInvuln") || !fetchData(%aiId, "HasLoadedAndSpawned"))
	{
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " EXIT: SpawnInvuln=" @ fetchData(%aiId, "SpawnInvuln") @ " HasLoadedAndSpawned=" @ fetchData(%aiId, "HasLoadedAndSpawned"));
		storeData(%aiId, "BotAttackLoopActive", "");
		return;
	}
	
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == -1 || %playerObj == "")
	{
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " EXIT: no player object (aiId " @ %aiId @ ")");
		return; // No player object
	}
	
	// Check if target is still valid
	// TRIGGER MODEL: melee weapons here are repurposed gun images (reloadTime=0, infinite
	// ammo, swing pacing baked into the fire animation), so like a player, the bot HOLDS
	// the trigger while engaged and the engine chains swings at the animation rate.
	// Every disengage path below must RELEASE the trigger, otherwise the bot keeps
	// swinging at air after the target dies/escapes (the old code released after 100s).
	%targetPlayerObj = Client::getOwnedObject(%targetId);
	if(%targetPlayerObj == -1 || %targetPlayerObj == "")
	{
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " EXIT: target " @ %targetId @ " has no player object");
		storeData(%aiId, "AITarget", "");
		storeData(%aiId, "BotAttackLoopActive", "");
		Player::trigger(%playerObj, $WeaponSlot, false);
		return; // Target gone
	}

	// Check if still targeting this enemy
	%currentTarget = fetchData(%aiId, "AITarget");
	if(%currentTarget != %targetId)
	{
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " EXIT: changed target (AITarget=" @ %currentTarget @ " loop target=" @ %targetId @ ")");
		storeData(%aiId, "BotAttackLoopActive", "");
		Player::trigger(%playerObj, $WeaponSlot, false);
		return; // Changed target
	}

	// Get positions
	%aiPos = GameBase::getPosition(%playerObj);
	%targetPos = GameBase::getPosition(%targetPlayerObj);
	if(%aiPos == "" || %targetPos == "")
	{
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " EXIT: empty position (ai='" @ %aiPos @ "' target='" @ %targetPos @ "')");
		storeData(%aiId, "BotAttackLoopActive", "");
		Player::trigger(%playerObj, $WeaponSlot, false);
		return;
	}

	// Drop target if zones differ or distance is excessively large (prevent cross-zone chasing)
	// Zone values are $Zone::FolderID object ids (zone indexes are 1-based), so 0 is
	// NOT a real zone - it means the zone was never stamped (seen on live: bots carried
	// zone 0 vs players' folder ids, so this guard dropped EVERY target and the attack
	// loop died instantly; bots only swung via the engine drone's short-lived auto-fire).
	// Unknown zone on either side = skip the drop; the dist>150 cap below still bounds chasing.
	%aiZone = fetchData(%aiId, "zone");
	%targetZone = fetchData(%targetId, "zone");
	if(%aiZone != "" && %aiZone != -1 && %aiZone != 0 && %targetZone != "" && %targetZone != -1 && %targetZone != 0 && %aiZone != %targetZone)
	{
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " EXIT: zone mismatch (bot zone " @ %aiZone @ " vs target zone " @ %targetZone @ ")");
		storeData(%aiId, "AITarget", "");
		storeData(%aiId, "BotAttackLoopActive", "");
		Player::trigger(%playerObj, $WeaponSlot, false);
		AI::newDirectiveRemove(%aiName, 99);
		return;
	}

	// Calculate distance
	%dist = Vector::getDistance(%aiPos, %targetPos);

	// Hard cap: if target is extremely far, drop it to avoid global chasing
	if(%dist > 150)
	{
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " EXIT: dist > 150 (" @ %dist @ ")");
		storeData(%aiId, "AITarget", "");
		storeData(%aiId, "BotAttackLoopActive", "");
		storeData(%aiId, "LastLoggedDist", "");
		Player::trigger(%playerObj, $WeaponSlot, false);
		AI::newDirectiveRemove(%aiName, 99);
		return;
	}
	
	// Get weapon and attack range - USE WEAPON RANGE, not $AIspotDist
	%equippedWeapon = Player::getMountedItem(%playerObj, $WeaponSlot);
	%attackRange = 6; // Default melee range (2 + 4)
	if(%equippedWeapon != -1 && %equippedWeapon != "")
	{
		%weaponRange = GetRange(%equippedWeapon);
		if(%weaponRange != "" && %weaponRange != -1 && %weaponRange > 0)
			%attackRange = %weaponRange;
	}
	// Pull bots in very close (hug player) for attack loop
	%attackRangeTight = %attackRange - 4;
	if(%attackRangeTight < 1.0) %attackRangeTight = 1.0;
	%approachRange = %attackRangeTight - 0.25;
	if(%approachRange < 0.5) %approachRange = 0.5;
	
	// DEBUG: Only log on significant state changes (not every tick)
	%lastLoggedDist = fetchData(%aiId, "LastLoggedDist");
	%distDiff = %dist - %lastLoggedDist;
	if(%distDiff < 0) %distDiff = %distDiff * -1; // Absolute value
	// review #61: gate on $MeleeDebug. This per-tick range echo was the ONLY diagnostic in
	// AI::ContinuousAttack not gated on the debug flag - a chasing/kiting bot moves >2
	// units almost every 0.5s tick, so it wrote ~2 console lines/sec per engaged bot.
	if($MeleeDebug && (%lastLoggedDist == "" || %distDiff > 2)) // Only log if distance changed by 2+ units
	{
		if(%dist <= %attackRange)
			%rangeStatus = "IN RANGE";
		else
			%rangeStatus = "OUT OF RANGE";
		echo("[BOT RANGE DEBUG] Bot " @ %aiName @ " - Range: " @ %attackRange @ ", Dist: " @ %dist @ " (" @ %rangeStatus @ ")");
		storeData(%aiId, "LastLoggedDist", %dist);
	}
	
	// Check if in range
		if(%dist <= %attackRangeTight)
	{
		// Face target - calculate direction vector (from bot to target)
		%dirVec = Vector::sub(%targetPos, %aiPos);
		// Use only horizontal components for rotation (ignore height difference)
		%dirVec = GetWord(%dirVec, 0) @ " " @ GetWord(%dirVec, 1) @ " 0";
		%norm = Vector::normalize(%dirVec);
		%targetRot = Vector::getRotation(%norm);
		// DO NOT add 180 degrees - Vector::getRotation gives the correct facing direction
		// Adding 180 makes them face AWAY (which is what the old guard behavior did)
		%finalRot = "0 0 " @ GetWord(%targetRot, 2);
		GameBase::setRotation(%playerObj, %finalRot);
		
		// Play idle/root animation (not running) when attacking
		GameBase::playSequence(%playerObj, 0, "root");
		
		// Trigger attack: fresh RELEASE+PRESS edge every tick, not a bare hold.
		// The engine drone also writes the trigger from its own moves (aiObj
		// fireAtPlayerTarget -> serverUpdateMove edge logic), and its true->false
		// edge can release a script-held trigger; a bare re-press while the image
		// state machine is parked in Ready is a no-op, so the bot stopped swinging
		// until a range change forced an edge. Mid-swing (Fire/Reload) both calls
		// are no-ops, so swing pacing is still the weapon's fire animation.
		Player::trigger(%playerObj, $WeaponSlot, false);
		Player::trigger(%playerObj, $WeaponSlot, true);
		if($MeleeDebug)
			echo("[MELEE DEBUG] " @ %aiName @ " loop tick: IN RANGE (dist " @ %dist @ "), trigger re-pulled, energy=" @ GameBase::getEnergy(%playerObj));

		// Schedule next loop tick (re-validates target; re-press while held is a no-op)
		%weaponDelay = GetDelay(%equippedWeapon);
		if(%weaponDelay == "" || %weaponDelay == 0 || %weaponDelay < 0.3)
			%weaponDelay = 0.5; // Minimum 0.5 second delay

		// Schedule next attack
		schedule("AI::ContinuousAttack(\"" @ %aiName @ "\", " @ %targetId @ ");", %weaponDelay);
	}
		else
		{
			// Out of range - stop swinging and move directly toward the target's current position
			if($MeleeDebug)
				echo("[MELEE DEBUG] " @ %aiName @ " loop tick: OUT OF RANGE (dist " @ %dist @ " > tight " @ %attackRangeTight @ "), chasing");
			Player::trigger(%playerObj, $WeaponSlot, false);
			AI::directiveWaypoint(%aiName, %targetPos, 99);
			
			// Keep the attack loop running to check distance again
			schedule("AI::ContinuousAttack(\"" @ %aiName @ "\", " @ %targetId @ ");", 0.5);
		}
}

function AI::Periodic(%aiName)
{
	dbecho($dbechoMode, "AI::Periodic(" @ %aiName @ ")");

	// review #28: reject corpse objects BEFORE the expensive getClientIdFromName scan.
	// A dead bot is renamed "Corpse<N> <name>", which is never in the $BotNameToClient/
	// $TownBotSpawned fast path, so getClientIdFromName would fall through to a full
	// BaseRep pool walk - pure waste for every lingering corpse tick, discarded by this
	// same substring check moments later. Moved above the lookup.
	if(String::findSubStr(%aiName, "Corpse") != -1)
	{
		// This is a corpse, not a bot - ignore it (throttle logging to once/30s per name)
		%currentTime = getSimTime();
		%lastCorpseLog = $AI_Periodic_CorpseLog[%aiName];
		if(%lastCorpseLog == "" || %lastCorpseLog == -1 || (%currentTime - %lastCorpseLog) >= 30)
		{
			if($AI_PERIODIC_DEBUG)
				echo("[INERT DEBUG] AI::Periodic: Ignoring corpse object " @ %aiName @ " - callbackPeriodic still running for dead bot");
			$AI_Periodic_CorpseLog[%aiName] = %currentTime;
		}
		return;
	}

	// CRITICAL FIX: Early exit for bots in Graveyard (dead but not yet cleaned up)
	// This prevents log spam and resource waste from dead bots continuing to run periodic logic
	// We check this BEFORE any other processing to minimize overhead
	%checkClientId = AI::getClientIdFromName(%aiName);
	if(%checkClientId != -1 && %checkClientId != "")
	{
		// Check if in graveyard
		if(IsInGraveyard(%checkClientId))
			return; // Dead bot - don't reschedule, let it die quietly
		
		// Check if player object exists
		%checkPlayerObj = Client::getOwnedObject(%checkClientId);
		if(%checkPlayerObj == -1 || %checkPlayerObj == "" || !isObject(%checkPlayerObj))
			return; // No player object - bot is effectively dead
	}

	// review #28: corpse-object check moved ABOVE the getClientIdFromName call (top of
	// this function) so a lingering corpse tick no longer pays for a full BaseRep scan.

	// Throttle debug logging to once per bot per 10 seconds
	%currentTime = getSimTime();
	%lastLogTime = $AI_Periodic_LastLog[%aiName];
	if(%lastLogTime == "" || %lastLogTime == -1 || (%currentTime - %lastLogTime) >= 10)
	{
		if($AI_PERIODIC_DEBUG)
			echo("[INERT DEBUG] AI::Periodic: ENTRY @ " @ %currentTime @ " - aiName=" @ %aiName);
		$AI_Periodic_LastLog[%aiName] = %currentTime;
	}

	// Use the clientId we already got from the early check (avoid duplicate call)
	// Note: %checkClientId was set at the top of this function
	%aiId = %checkClientId;
	
	// Validate bot still exists (redundant with early check but kept for safety)
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
	{
		return; // Bot doesn't exist anymore, skip
	}

	if(fetchData(%aiId, "dumbAIflag") || fetchData(%aiId, "frozen"))
	{
		// DEBUG: Log when bot is frozen/dumb
		if(String::findSubStr(%aiName, "God") != -1 || String::findSubStr(%aiName, "Insurrector") != -1)
		{
			if($AI_DEBUG_ENABLED)
				echo("[AI DEBUG] AI::Periodic - Bot " @ %aiName @ " is frozen or dumbAI, skipping");
		}
		if($AI_PERIODIC_DEBUG)
			echo("[INERT DEBUG] AI::Periodic: Bot " @ %aiName @ " (aiId=" @ %aiId @ ") is frozen or dumbAI - returning early");
		return;
	}

	// PHASE 4 FIX: Skip ALL AI processing if bot is frozen (set by shove spells/commands)
	// This completely halts AI behavior during shove to allow physics to move the bot
	if($BotFrozen[%aiId] == "true")
	{
		if($AI_PERIODIC_DEBUG)
			echo("[INERT DEBUG] AI::Periodic: Bot " @ %aiName @ " (aiId=" @ %aiId @ ") is frozen via $BotFrozen - returning early");
		return;
	}

	// Skip adding new directives if bot was recently shoved (prevents AI from overriding shove impulse)
	// CRITICAL FIX: Check if ShovedByPlayer timestamp is recent (within 1.5 seconds), not just if it exists
	// This prevents false positives from stale timestamps when client IDs are reused
	%shovedTime = fetchData(%aiId, "ShovedByPlayer");
	if(%shovedTime != "" && %shovedTime != -1 && %shovedTime != "0")
	{
		%currentTime = getSimTime();
		%timeSinceShove = %currentTime - %shovedTime;
		// Only consider it "recently shoved" if it was within the last 1.5 seconds
		if(%timeSinceShove < 1.5)
		{
			if($AI_PERIODIC_DEBUG)
				echo("[INERT DEBUG] AI::Periodic: Bot " @ %aiName @ " (aiId=" @ %aiId @ ") was recently shoved " @ %timeSinceShove @ "s ago - returning early");
			return;
		}
		else
		{
			// Stale timestamp - clear it to prevent false positives
			storeData(%aiId, "ShovedByPlayer", "");
			$BotFrozen[%aiId] = "";
		}
	}

	// Reuse player object from early check (avoid duplicate call)
	%playerObj = %checkPlayerObj;
	if(%playerObj == -1 || %playerObj == "")
	{
		return; // Bot doesn't have a valid player object (already checked early but kept for safety)
	}
	
	%aiTeam = GameBase::getTeam(%playerObj);
	%aiPos = GameBase::getPosition(%playerObj);
	
	// Fallback: If team is still -1, use stored botTeam
	if(%aiTeam == -1)
	{
		%aiTeam = fetchData(%aiId, "botTeam");
		if(%aiTeam == "" || %aiTeam == -1)
			%aiTeam = 1; // Default to team 1 (Enemy) if all else fails
	}
	
	// DEBUG: Log targeting info for God bots (throttled to once per 30 seconds to reduce spam)
	%spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
	if(String::findSubStr(%aiName, "God") != -1 || String::findSubStr(%aiName, "Insurrector") != -1)
	{
		%currentTime = getIntegerTime(true);
		%lastDebugLogTime = $AIPeriodicLastDebugLog[%aiName];
		if(%lastDebugLogTime == "" || %lastDebugLogTime == "0" || %lastDebugLogTime == -1)
			%lastDebugLogTime = 0;
		
		%timeSinceLastLog = %currentTime - %lastDebugLogTime;
		if(%timeSinceLastLog >= 30000) // 30 seconds (in milliseconds)
		{
			if($AI_DEBUG_ENABLED)
				echo("[AI DEBUG] AI::Periodic - Bot " @ %aiName @ " (clientId=" @ %aiId @ ", playerObj=" @ %playerObj @ "): team=" @ %aiTeam @ ", SpawnBotInfo='" @ %spawnBotInfo @ "'");
			$AIPeriodicLastDebugLog[%aiName] = %currentTime;
		}
	}

	//=================================================================
	//Everytime this function is called, the bot looks at all clients
	//(including bots) and sets a waypoint to the nearest one that is
	//NOT on the same team, and that it can see in its FOV (by
	//comparing Rotations).  These loops will undoubtedly cause alot
	//of CPU drain.
	//=================================================================

	if(%spawnBotInfo != "")
	{
		if($AIsmartFOVbots)
		{
			//find all clients that COULD be in the bot's FOV and that are not on the same team.
			// CRITICAL: Use Player object for position/rotation, not client ID
			%aiRot = GetWord(GameBase::getRotation(%playerObj), 2);

			// BUGFIX: consumption below checks %idList[1] and iterates from index 1,
			// so filling from index 0 silently dropped the FIRST candidate found
			// (and skipped the whole FOV branch when only one enemy was visible)
			%c = 1; // Initialize counter for idList array (1-based to match consumer)

			// review #9: gather FOV candidates from a SPATIALLY-BOUNDED containerBoxFillSet
			// query (only objects within the bot's detection box), NOT GetEveryoneIdList(),
			// which walked the ENTIRE BaseRep pool (every player + every bot) every 5s per
			// bot - O(bots x everyone), plus an isFile disk probe per candidate via
			// fetchData(...,"invisible"). The else/sniff branch below already used this exact
			// bounded query; the FOV branch now does too, then applies the SAME FOV angle
			// filter. Behaviour is unchanged: the box (%botMaxRange*2 half-extent) covers the
			// full %botMaxRange distance the consumer requires, so no in-range target drops.
			%botMaxRange = fetchData(%aiId, "AImaxRangeOverride");
			if(%botMaxRange == "" || %botMaxRange == -1 || %botMaxRange == "0")
				%botMaxRange = $AImaxRange;
			%b = %botMaxRange * 2;
			%fovSet = newObject("set", SimSet);
			containerBoxFillSet(%fovSet, $SimPlayerObjectType, %aiPos, %b, %b, %b, 0);
			for(%i = 0; %i < Group::objectCount(%fovSet); %i++)
			{
				%targetPlayerObj = Group::getObject(%fovSet, %i);
				if(%targetPlayerObj == -1 || %targetPlayerObj == "")
					continue;
				// Use helper to get client ID from Player object (handles enemy bots)
				%id = GetClientIdFromPlayerObject(%targetPlayerObj);
				if(%id == -1 || %id == "")
					%id = %targetPlayerObj;
				// CRITICAL: Use Player object for team check, not client ID
				%targetTeam = GameBase::getTeam(%targetPlayerObj);

				// Validate id is valid and not on same team (self is same-team -> filtered)
				if(%id != -1 && %id != "" && %targetTeam != %aiTeam && !fetchData(%id, "invisible"))
				{
					%targetPos = GameBase::getPosition(%targetPlayerObj);
					%vec = Vector::sub(%targetPos, %aiPos);
					%vecRot = GetWord(Vector::getRotation(%vec), 2);

					if(%vecRot >= %aiRot - $AIFOVPan && %vecRot <= %aiRot + $AIFOVPan)
					{
						%idList[%c++] = %id;
					}
				}
			}
			deleteObject(%fovSet);
		}

		if(%idList[1] != "" && $AIsmartFOVbots)
		{
			%closest = 500000;
			for(%i = 1; %idList[%i] != ""; %i++)
			{
				//echo(%aiId @ ": AI spotted " @ %idList[%i]);
				%dist = Vector::getDistance(%aiPos, GameBase::getPosition(%idList[%i]));
				if(%dist < %closest)
				{
					%closest = %dist;
					%closestId = %idList[%i];
				}
			}
	
			// Use per-bot detection range override if set, otherwise use global $AImaxRange
			%botMaxRange = fetchData(%aiId, "AImaxRangeOverride");
			if(%botMaxRange == "" || %botMaxRange == -1 || %botMaxRange == "0")
				%botMaxRange = $AImaxRange;
			if(%closest <= %botMaxRange && %closestId != "")
			{
				//echo(%aiId @ ": targeting (moving towards) " @ %closestId @ ", " @ %closest @ " meters away");
				// Validate closestId still exists before using it
				%targetPlayer = Client::getOwnedObject(%closestId);
				if(%targetPlayer != -1 && %targetPlayer != "")
				{
					%targetPos = GameBase::getPosition(%targetPlayer);
					
					// Get the weapon range for attack distance - use actual weapon range, NOT $AIspotDist
					%equippedWeapon = Player::getMountedItem(%playerObj, $WeaponSlot);
					%attackRange = 6; // Default melee range (minRange 2 + RangeTable 4)
					if(%equippedWeapon != -1 && %equippedWeapon != "")
					{
						%weaponRange = GetRange(%equippedWeapon);
						if(%weaponRange != "" && %weaponRange != -1 && %weaponRange > 0)
							%attackRange = %weaponRange;
					}
					// Pull bots in very close (hug player)
					%attackRangeTight = %attackRange - 4;
					if(%attackRangeTight < 1.0) %attackRangeTight = 1.0;
					%approachRange = %attackRangeTight - 0.25;
					if(%approachRange < 0.5) %approachRange = 0.5;
					
					if(%closest <= %attackRangeTight)
					{
						// Within attack range - check if already attacking this target
						%currentTarget = fetchData(%aiId, "AITarget");
						%isNewTarget = (%currentTarget != %closestId);
						
						if(%isNewTarget)
						{
							// New target - stop movement and set up attack
							AI::newDirectiveRemove(%aiName, 99);
							GameBase::playSequence(%playerObj, 0, "root");
							storeData(%aiId, "AITarget", %closestId);
						}
						
						// Start continuous attack loop if not already running
						%attackLoopActive = fetchData(%aiId, "BotAttackLoopActive");
						if(%attackLoopActive != "true" || %isNewTarget)
						{
							storeData(%aiId, "BotAttackLoopActive", "true");
							// Start the attack loop immediately
							AI::ContinuousAttack(%aiName, %closestId);
						}
					}
					else if(%closest <= %approachRange)
					{
						AI::newDirectiveWaypoint(%aiName, %targetPos, 99);
					}
					else
					{
						// Close directly to the target position (no buffer)
						AI::newDirectiveWaypoint(%aiName, %targetPos, 99);
					}
				}
			}
		}
		else
		{
			//==============================================================
			// I'm making it so bots can "smell" their target. Basically,
			// if you're close enough to them, they will lock onto you.
			// I doubt this loop will cause much lag... *fingers crossed*
			//==============================================================

			if(fetchData(%aiId, "SpellCastStep") != 1 && !fetchData(%aiId, "noBotSniff"))
			{
				%closest = 500000;

				%flag = False;
				// Use per-bot detection range override if set, otherwise use global $AImaxRange
				%botMaxRange = fetchData(%aiId, "AImaxRangeOverride");
				if(%botMaxRange == "" || %botMaxRange == -1 || %botMaxRange == "0")
					%botMaxRange = $AImaxRange;
				%b = %botMaxRange * 2;
				%set = newObject("set", SimSet);
				%n = containerBoxFillSet(%set, $SimPlayerObjectType, %aiPos, %b, %b, %b, 0);
				for(%i = 0; %i < Group::objectCount(%set); %i++)
				{
					%targetPlayerObj = Group::getObject(%set, %i);
					// Validate object is valid before getting client
					if(%targetPlayerObj != -1 && %targetPlayerObj != "")
					{
						// Use helper function to get client ID from Player object (handles enemy bots)
						%id = GetClientIdFromPlayerObject(%targetPlayerObj);
						if(%id == -1 || %id == "")
							%id = %targetPlayerObj;
						
						// CRITICAL: Use Player object for team check, not client ID
						%targetTeam = GameBase::getTeam(%targetPlayerObj);
						
						if(%id != -1 && %id != "" && %targetTeam != %aiTeam && !fetchData(%id, "invisible"))
						{
							%targetPos = GameBase::getPosition(%targetPlayerObj);
							%dist = Vector::getDistance(%aiPos, %targetPos);
							if(%dist < %closest)
							{
								%closest = %dist;
								%closestId = %id;
								%closestPlayerObj = %targetPlayerObj;
							}
						}
					}
				}
				deleteObject(%set);

				// Use per-bot detection range override if set, otherwise use global $AImaxRange
				%botMaxRange = fetchData(%aiId, "AImaxRangeOverride");
				if(%botMaxRange == "" || %botMaxRange == -1 || %botMaxRange == "0")
					%botMaxRange = $AImaxRange;
				if(%closest <= %botMaxRange && %closestId != "")
				{
					// Validate closestId still exists before using it
					%targetPlayer = Client::getOwnedObject(%closestId);
					if(%targetPlayer != -1 && %targetPlayer != "")
					{
						// Use the Player object for position (more reliable than client ID)
						%targetPos = GameBase::getPosition(%targetPlayer);
						if(%targetPos == "" || %targetPos == -1)
							%targetPos = GameBase::getPosition(%closestId); // Fallback
						
						// Get the weapon range for attack distance
						// This accounts for different weapon types (swords, polearms, etc.)
						%equippedWeapon = Player::getMountedItem(%playerObj, $WeaponSlot);
						%attackRange = 6; // Default melee range (minRange 2 + RangeTable 4)
						if(%equippedWeapon != -1 && %equippedWeapon != "")
						{
							%weaponRange = GetRange(%equippedWeapon);
							if(%weaponRange != "" && %weaponRange != -1 && %weaponRange > 0)
								%attackRange = %weaponRange;
						}
						// Pull bots in very close (hug player)
						%attackRangeTight = %attackRange - 4;
						if(%attackRangeTight < 1.0) %attackRangeTight = 1.0;
						%approachRange = %attackRangeTight - 0.25;
						if(%approachRange < 0.5) %approachRange = 0.5;
						
						// CRITICAL FIX: For Player objects, we need custom attack logic
						// The AI engine directives don't work properly for Player objects
						if(%closest <= %attackRangeTight)
						{
							// Within attack range - check if already attacking this target
							%currentTarget = fetchData(%aiId, "AITarget");
							%isNewTarget = (%currentTarget != %closestId);
							
							if(%isNewTarget)
							{
								// New target - stop movement and set up attack
								AI::newDirectiveRemove(%aiName, 99);
								GameBase::playSequence(%playerObj, 0, "root");
								storeData(%aiId, "AITarget", %closestId);
							}
							
							// Start continuous attack loop if not already running
							%attackLoopActive = fetchData(%aiId, "BotAttackLoopActive");
							if(%attackLoopActive != "true" || %isNewTarget)
							{
								storeData(%aiId, "BotAttackLoopActive", "true");
								// Start the attack loop immediately
								AI::ContinuousAttack(%aiName, %closestId);
							}
						}
						else if(%closest <= %approachRange)
						{
							// Close to attack range - move to exact position
							AI::newDirectiveWaypoint(%aiName, %targetPos, 99);
						}
						else
						{
							// Far from target - move directly toward their current position
							AI::newDirectiveWaypoint(%aiName, %targetPos, 99);
						}

						PlaySound(RandomRaceSound(fetchData(%aiId, "RACE"), Acquired), %aiPos);
					}
				}
				else
				{
					%flag = True;
					// Not targeting anyone - clear target and stop attack loop
					storeData(%aiId, "AITarget", "");
					storeData(%aiId, "BotAttackLoopActive", "");
				}
			}
			
			if(%flag || fetchData(%aiId, "noBotSniff"))
			{
				AI::SelectMovement(%aiName);
			}
		}
	}

	//=================================================================
	// Event stuff
	//=================================================================
	%i = GetEventCommandIndex(%aiId, "onPosCloseEnough");
	if(%i != -1)
	{
		%x = GetWord($EventCommand[%aiId, %i], 2);
		%y = GetWord($EventCommand[%aiId, %i], 3);
		%z = GetWord($EventCommand[%aiId, %i], 4);
		%dpos = %x @ " " @ %y @ " " @ %z;

		if(Vector::getDistance(%dpos, GameBase::getPosition(%aiId)) <= 5)
		{
			%name = GetWord($EventCommand[%aiId, %i], 0);
			%type = GetWord($EventCommand[%aiId, %i], 1);
			%cl = NEWgetClientByName(%name);
			if(%cl == -1)
				%cl = 2048;

			%cmd = String::NEWgetSubStr($EventCommand[%aiId, %i], String::findSubStr($EventCommand[%aiId, %i], ">")+1, 99999);
			%pcmd = ParseBlockData(%cmd, %aiId, "");
			$EventCommand[%aiId, %i] = "";
			schedule("remoteSay(" @ %cl @ ", 0, \"" @ %pcmd @ "\", \"" @ %name @ "\");", 1);
		}
	}
	%i = GetEventCommandIndex(%aiId, "onIdCloseEnough");
	if(%i != -1)
	{
		%id = GetWord($EventCommand[%aiId, %i], 2);
		// Validate target id still exists before using it
		if(%id != -1 && %id != "")
		{
			%targetPlayer = Client::getOwnedObject(%id);
			if(%targetPlayer != -1 && %targetPlayer != "")
			{
				%dpos = GameBase::getPosition(%id);

				if(Vector::getDistance(%dpos, %aiPos) <= 10)
				{
					%name = GetWord($EventCommand[%aiId, %i], 0);
					%type = GetWord($EventCommand[%aiId, %i], 1);
					%cl = NEWgetClientByName(%name);
					if(%cl == -1)
						%cl = 2048;

					%cmd = String::NEWgetSubStr($EventCommand[%aiId, %i], String::findSubStr($EventCommand[%aiId, %i], ">")+1, 99999);
					%pcmd = ParseBlockData(%cmd, %aiId, "");
					$EventCommand[%aiId, %i] = "";
					schedule("remoteSay(" @ %cl @ ", 0, \"" @ %pcmd @ "\", \"" @ %name @ "\");", 1);
				}
			}
			else
			{
				// Target no longer exists, clear the event command
				$EventCommand[%aiId, %i] = "";
			}
		}
	}

	//=================================================================
	//1 thru 4 = animation 10, 5 thru 10 = animation 11, else do nothing
	//=================================================================
	if(Item::getVelocity(%aiId) == "0 0 0")
	{
		%r = floor(getRandom() * 200)+1;
		if(%r >= 1 && %r <= 4)
			RemotePlayAnim(%aiId, 10);
		else if(%r >= 5 && %r <= 10)
			RemotePlayAnim(%aiId, 11);

		if(GameBase::getTeam(%aiId) > 1)
		{
			if(OddsAre(5))
				RemotePlayAnim(%aiId, 2);
		}
	}
	
	//=============================================
	//do other stuff...
	//=============================================
	//%curTarget = ai::getTarget( %aiName );

	if(OddsAre(4))
		AI::SelectBestWeapon(%aiId);
}
function AI::NextWeapon(%aiId)
{
	dbecho($dbechoMode, "AI::NextWeapon(" @ %aiId @ ")");

	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == "" || %playerObj == -1)
		return;

	%item = Player::getMountedItem(%aiId, $WeaponSlot);

	if(%item == -1 || $NextWeapon[%item] == "")
		selectValidWeapon(%aiId);
	else
	{
		%loopCount = 0;
		%maxLoops = 100; // Prevent infinite loops
		for(%weapon = $NextWeapon[%item]; %weapon != %item && %loopCount < %maxLoops; %weapon = $NextWeapon[%weapon])
		{
			%loopCount++;
			if(isSelectableWeapon(%aiId, %weapon))
			{
				%x = "";
				if(GetAccessoryVar(%weapon, $AccessoryType) == $RangedAccessoryType)
				{
					%x = GetBestRangedProj(%aiId, %weapon);
					if(%x != -1)
						storeData(%aiId, "LoadedProjectile " @ %weapon, %x);
				}

				if(%x != -1)
				{
					Player::useItem(%aiId, %weapon);
					if(Player::getMountedItem(%aiId, $WeaponSlot) == %weapon || Player::getNextMountedItem(%aiId, $WeaponSlot) == %weapon)
						break;
				}
			}
		}
		if(%loopCount >= %maxLoops)
		{
			echo("ERROR: AI::NextWeapon - Infinite loop detected for bot " @ %aiId @ ". Breaking.");
		}
	}

	AI::SetSpotDist(%aiId);
}

function AI::SelectBestWeapon(%aiId)
{
	dbecho($dbechoMode, "AI::SelectBestWeapon(" @ %aiId @ ")");

	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == "" || %playerObj == -1)
		return -1;

	%weapon = GetBestWeapon(%aiId);
	if(%weapon != -1)
	{
		%x = "";
		%isRanged = (GetAccessoryVar(%weapon, $AccessoryType) == $RangedAccessoryType);
		
		if(%isRanged)
		{
			%x = GetBestRangedProj(%aiId, %weapon);
			if(%x != -1)
				storeData(%aiId, "LoadedProjectile " @ %weapon, %x);
		}

		// For ranged weapons, only use if we have a projectile
		// For melee weapons, always use (no projectile needed)
		if((%isRanged && %x != -1) || !%isRanged)
		{
			Player::useItem(%aiId, %weapon);
			// Schedule SpotDist update with a small delay to ensure weapon is mounted
			// This ensures the weapon is actually mounted before we try to get its range
			schedule("AI::SetSpotDist(" @ %aiId @ ");", 0.1);
		}
	}

	return -1;
}

function AI::SetSpotDist(%aiId, %retryCount)
{
	dbecho($dbechoMode, "AI::SetSpotDist(" @ %aiId @ ")");

	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == "" || %playerObj == -1)
		return;

	if(fetchData(%aiId, "frozen") || fetchData(%aiId, "dumbAIflag"))
		return;

	// Limit retries to prevent infinite loops
	if(%retryCount == "")
		%retryCount = 0;
	
	if(%retryCount >= 10)
	{
		// Max retries reached - bot may not have a weapon, use default range
		%botName = fetchData(%aiId, "BotInfoAiName");
		if(%botName == "" || %botName == -1 || %botName == "0")
			%botName = Client::getName(%aiId);
		if(%botName != "" && %botName != -1 && %botName != "0")
		{
			AI::setVar(%botName, SpotDist, $AIspotDist);
			AI::setVar(%botName, triggerPct, 1.0);
		}
		return;
	}

	%item = Player::getMountedItem(%aiId, $WeaponSlot);
	%botName = fetchData(%aiId, "BotInfoAiName");
	
	// Fallback to Client::getName if BotInfoAiName is invalid
	if(%botName == "" || %botName == -1 || %botName == "0")
		%botName = Client::getName(%aiId);
	
	// Validate bot name exists and weapon is mounted before calling AI::setVar
	if(%botName != "" && %botName != -1 && %botName != "0" && %item != "" && %item != -1)
	{
		%weaponRange = GetRange(%item);
		AI::setVar(%botName, SpotDist, %weaponRange);
		AI::setVar(%botName, triggerPct, 1.0);
		// Only log on first successful attempt to reduce spam
		//if(%retryCount == 0)
		//	echo("[SPAWN DEBUG] AI::SetSpotDist(): Set SpotDist to " @ %weaponRange @ " for bot " @ %botName @ " (weapon: " @ %item @ ")");
	}
	else
	{
		// Weapon not mounted yet - schedule a retry (only log first retry)
		if(%item == "" || %item == -1)
		{
			//if(%retryCount == 0)
			//	echo("[SPAWN DEBUG] AI::SetSpotDist(): Weapon not mounted yet for bot " @ %botName @ ", scheduling retry...");
			schedule("AI::SetSpotDist(" @ %aiId @ ", " @ (%retryCount + 1) @ ");", 0.2);
		}
	}
}

// (AI::activelyFollow and AI::moveToAttackMarker removed - no callers anywhere
// in the codebase; superseded by newDirectiveFollow/moveSomewhere/moveToFurthest)

function AI::moveSomewhere(%aiName)
{
	dbecho($dbechoMode, "AI::moveSomewhere(" @ %aiName @ ")");

	// Use getClientIdFromName() instead of getId() to minimize error spam
	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
		return;

	if(fetchData(%aiId, "dumbAIflag"))
		return;

	//echo(%aiId @ " ======> Ok, i'm considering giving the bot a new place to go.");
	%attackMarker = fetchData(%aiId, "AIattackMarker");
	if(%attackMarker != "")
	{
		// Validate marker object still exists
		%markerObj = %attackMarker;
		if(%markerObj != -1 && %markerObj != "")
		{
			%dist = Vector::getDistance(GameBase::getPosition(%markerObj), GameBase::getPosition(%aiId));
			//echo(%aiId @ " ======> It seems this bot has an attackMarker, and it's this far: " @ %dist);
			if(%dist <= $AIcloseEnoughMarkerDist)
			{
				//echo(%aiId @ " ======> The distance " @ %dist @ " seems to be close enough to cancel the attackMarker!");
				storeData(%aiId, "AIattackMarker", "");
			}
		}
		else
		{
			// Marker object no longer exists, clear it
			storeData(%aiId, "AIattackMarker", "");
		}
	}
	if(fetchData(%aiId, "AIattackMarker") == "")
	{
		//echo(%aiId @ " ======> The bot has no attack marker, so I'm attempting to give bot somewhere to go!");
		%aiPos = GameBase::getPosition(%aiId);
		%minrad = $AIminrad;
		%maxrad = $AImaxrad;

		%tempPos = RandomPositionXY(%minrad, %maxrad);

		%xPos = GetWord(%tempPos, 0) + GetWord(%aiPos, 0);
		%yPos = GetWord(%tempPos, 1) + GetWord(%aiPos, 1);
		%zPos = GetWord(%aiPos, 2); //doesn't matter; the bot can't go thru terrain
		
		%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

		storeData(%aiId, "AIattackMarker", "");
		AI::newDirectiveWaypoint(%aiName, %newPos, 99);

		//echo(%aiName @ " IS WANDERING TO LOCATION " @ %newPos);
	}
}

//experimental function, which makes bot look around himself at preset angles and, whichever is the furthest, go there.
//i'm hoping this will simulate a smarter bot.

//test results: seems to work great!  hopefully it doesn't cause too much lag, but up to now... looks fine
function AI::moveToFurthest(%aiName)
{
	dbecho($dbechoMode, "AI::moveToFurthest(" @ %aiName @ ")");

	// Use getClientIdFromName() instead of getId() to minimize error spam
	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
		return;

	if(fetchData(%aiId, "dumbAIflag"))
		return;

	%attackMarker = fetchData(%aiId, "AIattackMarker");
	if(%attackMarker != "")
	{
		// Validate marker object still exists
		%markerObj = %attackMarker;
		if(%markerObj != -1 && %markerObj != "")
		{
			%dist = Vector::getDistance(GameBase::getPosition(%markerObj), GameBase::getPosition(%aiId));
			if(%dist <= $AIcloseEnoughMarkerDist)
			{
				storeData(%aiId, "AIattackMarker", "");
			}
		}
		else
		{
			// Marker object no longer exists, clear it
			storeData(%aiId, "AIattackMarker", "");
		}
	}
	if(fetchData(%aiId, "AIattackMarker") == "")
	{
		%aiPos = GameBase::getPosition(%aiId);
		%aiPlayer = Client::getOwnedObject(%aiId);
		
		// Validate player object exists before LOS calls
		if(%aiPlayer == -1 || %aiPlayer == "")
			return;

		%furthest = -1;
		for(%i = 0; %i <= 6.283; %i+= 0.52)
		{
			GameBase::getLOSinfo(%aiPlayer, 1000, "0 0 " @ %i);
			%dist = Vector::getDistance(%aiPos, $los::position);
			if(%dist > %furthest && $los::position != "0 0 0" && $los::position != "")
			{
				%furthest = %dist;
				%chosenPos = $los::position;
			}
		}
		if(%furthest == -1)
		{
			//it seems the bot only sees sky, so revert to AI::moveSomewhere
			AI::moveSomewhere(%aiName);
			return;
		}

		%finalPos = %chosenPos;

		AI::newDirectiveWaypoint(%aiName, %finalPos, 99);

		//echo(%aiName @ " FOUND THE FURTHEST POINT AT LOCATION " @ %chosenPos);
	}
}


//These will only be invoked if the target is REALLY close to the bot (since the SpotDist is only the range of the
//weapon).  This means that if the bot ever gets close enough to engage in battle, he will try his best to continue
//the fight by following the target.  Once the target is lost or dies, directive 99 will be cancelled and directive
//99 will take over (regular walking, formations etc)
function AI::onTargetLOSAcquired(%aiName, %idNum)
{
	dbecho($dbechoMode, "AI::onTargetLOSAcquired(" @ %aiName @ ", " @ %idNum @ ")");

	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "")
		return; // Bot doesn't exist anymore, skip

	if(fetchData(%aiId, "SpawnBotInfo") != "" && !fetchData(%aiId, "dumbAIflag"))
		AI::newDirectiveFollow(%aiName, %idNum, 0, 99);
}

function AI::onTargetLOSLost(%aiName, %idNum)
{
	dbecho($dbechoMode, "AI::onTargetLOSLost(" @ %aiName @ ", " @ %idNum @ ")");

	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "")
		return; // Bot doesn't exist anymore, skip

	if(fetchData(%aiId, "SpawnBotInfo") != "" && !fetchData(%aiId, "dumbAIflag"))
		AI::newDirectiveRemove(%aiName, 99);
}

function AI::onTargetLOSRegained(%aiName, %idNum)
{
	dbecho($dbechoMode, "AI::onTargetLOSRegained(" @ %aiName @ ", " @ %idNum @ ")");

	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "")
		return; // Bot doesn't exist anymore, skip

	// CRITICAL FIX: Fallback if engine hasn't updated getOwnedObject yet
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == -1 || %playerObj == "")
		%playerObj = FindPlayerInBotGroup(%aiId);
		
	if(%playerObj == -1 || %playerObj == "")
		return;

	if(fetchData(%aiId, "SpawnBotInfo") != "" && !fetchData(%aiId, "dumbAIflag"))
		AI::newDirectiveFollow(%aiName, %idNum, 0, 99);
}

function AI::onTargetDied(%aiName, %idNum)
{
	dbecho($dbechoMode, "AI::onTargetDied(" @ %aiName @ ", " @ %idNum @ ")");

	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists before trying to remove directive
	if(%aiId == -1 || %aiId == "")
		return; // Bot doesn't exist anymore, skip

	if(fetchData(%aiId, "SpawnBotInfo") != "" && !fetchData(%aiId, "dumbAIflag"))
		AI::newDirectiveRemove(%aiName, 99);
}                                 

//=================================================
function AI::SelectMovement(%aiName)
{
	dbecho($dbechoMode, "AI::SelectMovement(" @ %aiName @ ")");

	// Use getClientIdFromName() instead of getId() to minimize error spam
	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
		return;

	// Skip random movement if enemy bot has an active target
	// But allow the AI directive system to handle movement towards target
	if(IsEnemyBot(%aiId))
	{
		%hasTarget = fetchData(%aiId, "AITarget");
		if(%hasTarget != "" && %hasTarget != -1)
			return; // Don't do random movement while targeting - AI::ContinuousAttack handles movement
	}

	if(fetchData(%aiId, "botAttackMode") == 1)
	{
		//Regular walk

		if(IsInArenaDueler(%aiId) || IsInRoster(%aiId))
			%r = OddsAre(1);
		else
		{
			// Check for per-bot AImoveChance first (for seal/Colloseum bots), then fall back to global
			%botMoveChance = fetchData(%aiId, "AImoveChance");
			if(%botMoveChance != "")
				%r = OddsAre(%botMoveChance);
			else
				%r = OddsAre($AImoveChance);
		}
		
		if(%r && !fetchData(%aiId, "frozen"))
		{
			%s = RandomRaceSound(fetchData(%aiId, "RACE"), RandomWait);
			if(%s == "NoSound")
				PlaySound(SoundGrunt1, GameBase::getPosition(%aiId));
			else
				PlaySound(%s, GameBase::getPosition(%aiId));
			AI::moveToFurthest(%aiName);
		}
	}
	else if(fetchData(%aiId, "botAttackMode") == 2)
	{
		//Follow a specific player target

		%followId = fetchData(%aiId, "tmpbotdata");
		if(%followId != "" && %followId != -1 && %followId != %aiId)
		{
			// Validate target still exists before following
			%targetPlayer = Client::getOwnedObject(%followId);
			if(%targetPlayer != -1 && %targetPlayer != "")
				AI::newDirectiveFollow(%aiName, %followId, 0, 99);
		}
	}
	else if(fetchData(%aiId, "botAttackMode") == 3)
	{
		//Attack at certain position
		AI::newDirectiveWaypoint(%aiName, fetchData(%aiId, "tmpbotdata"), 99);
	}
	else if(fetchData(%aiId, "botAttackMode") == 4)
	{
		//BotGroup formation

		%a = AI::IsInWhichBotGroup(%aiId);
		if(%a != -1)
		{
			%g = $tmpBotGroup[%a];
			
			if(%g != "")
			{
				//NOTE: can't make the bots follow a random bot in the group because at one point or another,
				//the bots will pick a follow combination which will NOT involve the team leader, leaving the
				//team leader alone.
				//This new method involves a North East oriented line.

				for(%i = 1; (%b = GetWord(%g, %i)) != -1; %i++)
				{
					if(%b == %aiId)
						%n = %i-1;
				}

				if(%n != "")
				{
					%followId = GetWord(%g, %n);

					if(!fetchData(%aiId, "frozen"))
					{
						if(%followId != "" && %followId != -1 && %followId != %aiId)
						{
							// Validate target bot still exists before following
							%targetPlayer = Client::getOwnedObject(%followId);
							if(%targetPlayer != -1 && %targetPlayer != "")
								AI::newDirectiveFollow(%aiName, %followId, 0, 99);
							else
								AI::moveToFurthest(%aiName); // Target doesn't exist, just move
						}
						else
							AI::moveToFurthest(%aiName);					//team leader gets to move.
					}
				}
			}
		}

	}
}

function HardcodeAIskills(%aiId)
{
	Watchdog_Enter("HardcodeAIskills");
	dbecho($dbechoMode, "HardcodeAIskills(" @ %aiId @ ")");

	// CRITICAL FIX #4: Check DontResetSkills flag at the very top
	// This prevents HardcodeAIskills from overwriting scaled stats (e.g., seal battle bots)
	if(fetchData(%aiId, "DontResetSkills") == "true" || fetchData(%aiId, "DontResetSkills") == "True" || fetchData(%aiId, "DontResetSkills") == "1")
	{
		echo("[HARDCODEAI] HardcodeAIskills(): Skipping skill reset for clientId " @ %aiId @ " (DontResetSkills flag set)");
		return;
	}

	// CRITICAL: Ensure enemy bots have their correct team set at the start of HardcodeAIskills
	// This prevents any timing issues where the team might be -1 during initialization
	// CRITICAL: Use the stored botTeam value (set in SpawnAIGetClientId from $BotInfo[botName, TEAM])
	// Different bot types have different teams - do NOT hardcode to 11!
	%storedBotTeam = fetchData(%aiId, "botTeam");
	// NOTE: team 0 (Citizen) is a VALID stored team - TempSpawn bots can be friendly
	if(%storedBotTeam != "" && %storedBotTeam != -1)
	{
		%currentTeam = GameBase::getTeam(%aiId);
		if(%currentTeam != %storedBotTeam)
		{
			GameBase::setTeam(%aiId, %storedBotTeam);
			%playerObjForTeam = Client::getOwnedObject(%aiId);
			if(%playerObjForTeam != -1 && %playerObjForTeam != "")
				GameBase::setTeam(%playerObjForTeam, %storedBotTeam);
			if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] HardcodeAIskills - Set team to " @ %storedBotTeam @ " for clientId " @ %aiId @ " (was " @ %currentTeam @ ") before RefreshAllEnemyBot()");
		}
	}
	else
	{
		// botTeam not stored - try to get from BotInfoAiName
		%botInfoAiName = fetchData(%aiId, "BotInfoAiName");
		if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
		{
			%botInfoTeam = $BotInfo[%botInfoAiName, TEAM];
			if(%botInfoTeam != "" && %botInfoTeam != -1 && %botInfoTeam != "0" && %botInfoTeam != 0)
			{
				// Store it and set the team
				storeData(%aiId, "botTeam", %botInfoTeam);
				%currentTeam = GameBase::getTeam(%aiId);
				if(%currentTeam != %botInfoTeam)
				{
					GameBase::setTeam(%aiId, %botInfoTeam);
					%playerObjForTeam = Client::getOwnedObject(%aiId);
					if(%playerObjForTeam != -1 && %playerObjForTeam != "")
						GameBase::setTeam(%playerObjForTeam, %botInfoTeam);
					if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] HardcodeAIskills - Set team to " @ %botInfoTeam @ " for clientId " @ %aiId @ " (was " @ %currentTeam @ ") from BotInfo before RefreshAllEnemyBot()");
				}
			}
		}
	}

	// CRITICAL: Check if this is a seal battle bot that has already been scaled
	// If SetupBot() has already scaled the skills, we must NOT reset them
	%isSealBattleBot = fetchData(%aiId, "SealBattleBot");
	%sealBattleScaledRound = fetchData(%aiId, "SealBattleScaledRound");
	%hasScaledSkills = (%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1") && (%sealBattleScaledRound != "" && %sealBattleScaledRound != -1 && %sealBattleScaledRound != "0" && %sealBattleScaledRound != 0);
	
	// CRITICAL: For seal battle bots, restore original LVL and skills BEFORE HardcodeAIskills() runs
	// This ensures that if a client ID is reused from a previous round, we restore the true originals
	// BEFORE recalculating skills, so the new skills are based on the base LVL, not scaled LVL
	if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
	{
		// CRITICAL: Restore original LVL first, so HardcodeAIskills() calculates from base LVL
		%storedOriginalLVL = fetchData(%aiId, "SealBattleOriginalLVL");
		if(%storedOriginalLVL != "" && %storedOriginalLVL != -1 && %storedOriginalLVL != 0)
		{
			storeData(%aiId, "LVL", %storedOriginalLVL);
			echo("[SEAL BATTLE] HardcodeAIskills(): Restored original LVL: " @ %storedOriginalLVL @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating skills");
		}
		
		// CRITICAL: Restore original skills BEFORE HardcodeAIskills() recalculates them
		// This ensures that if skills were scaled in a previous round, we restore the true originals
		%storedOriginalEndurance = fetchData(%aiId, "SealBattleOriginalEndurance");
		if(%storedOriginalEndurance != "" && %storedOriginalEndurance != -1)
		{
			$PlayerSkill[%aiId, $SkillEndurance] = %storedOriginalEndurance;
			echo("[SEAL BATTLE] HardcodeAIskills(): Restored original Endurance: " @ %storedOriginalEndurance @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating");
		}
		
		%storedOriginalEnergy = fetchData(%aiId, "SealBattleOriginalEnergy");
		if(%storedOriginalEnergy != "" && %storedOriginalEnergy != -1)
		{
			$PlayerSkill[%aiId, $SkillEnergy] = %storedOriginalEnergy;
			echo("[SEAL BATTLE] HardcodeAIskills(): Restored original Energy: " @ %storedOriginalEnergy @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating");
		}
		
		%storedOriginalWeightCapacity = fetchData(%aiId, "SealBattleOriginalWeightCapacity");
		if(%storedOriginalWeightCapacity != "" && %storedOriginalWeightCapacity != -1)
		{
			$PlayerSkill[%aiId, $SkillWeightCapacity] = %storedOriginalWeightCapacity;
			echo("[SEAL BATTLE] HardcodeAIskills(): Restored original WeightCapacity: " @ %storedOriginalWeightCapacity @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating");
		}
	}
	
	if(!%hasScaledSkills)
	{
		// Not a seal battle bot, or seal battle bot hasn't been scaled yet - proceed with normal skill initialization
		SetAllSkills(%aiId, 0);

		%ns = getNumSkills();
		%a = $autoStartupSP + round($initSPcredits / %ns) + round(((fetchData(%aiId, "LVL")-1) * $SPgainedPerLevel) / %ns);
		for(%i = 1; %i <= %ns; %i++)
			AddSkillPoint(%aiId, %i, %a);

		//==== HARDCODED SKILLS TO ENSURE CHALLENGING BOTS ============
		$PlayerSkill[%aiId, $SkillSlashing] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")+1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillPiercing] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")+1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillBludgeoning] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")+1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillDodging] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-2) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillVehicleCombat] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillOffensiveCasting] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillDefensiveCasting] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillNeutralCasting] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillEnergy] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillSpeech] = $SkillCap;
		$PlayerSkill[%aiId, $SkillWeightCapacity] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillEndurance] = ( (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel) ) / 2;
		
		// CRITICAL: For seal battle bots, restore original LVL and skills BEFORE HardcodeAIskills() runs
		// This ensures that if a client ID is reused from a previous round, we restore the true originals
		// BEFORE recalculating skills, so the new skills are based on the base LVL, not scaled LVL
		if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
		{
			// CRITICAL: Restore original LVL first, so HardcodeAIskills() calculates from base LVL
			%storedOriginalLVL = fetchData(%aiId, "SealBattleOriginalLVL");
			if(%storedOriginalLVL != "" && %storedOriginalLVL != -1 && %storedOriginalLVL != 0)
			{
				storeData(%aiId, "LVL", %storedOriginalLVL);
				echo("[SEAL BATTLE] HardcodeAIskills(): Restored original LVL: " @ %storedOriginalLVL @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating skills");
			}
			
			// CRITICAL: Restore original skills BEFORE HardcodeAIskills() recalculates them
			// This ensures that if skills were scaled in a previous round, we restore the true originals
			%storedOriginalEndurance = fetchData(%aiId, "SealBattleOriginalEndurance");
			if(%storedOriginalEndurance != "" && %storedOriginalEndurance != -1)
			{
				$PlayerSkill[%aiId, $SkillEndurance] = %storedOriginalEndurance;
				echo("[SEAL BATTLE] HardcodeAIskills(): Restored original Endurance: " @ %storedOriginalEndurance @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating");
			}
			
			%storedOriginalEnergy = fetchData(%aiId, "SealBattleOriginalEnergy");
			if(%storedOriginalEnergy != "" && %storedOriginalEnergy != -1)
			{
				$PlayerSkill[%aiId, $SkillEnergy] = %storedOriginalEnergy;
				echo("[SEAL BATTLE] HardcodeAIskills(): Restored original Energy: " @ %storedOriginalEnergy @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating");
			}
			
			%storedOriginalWeightCapacity = fetchData(%aiId, "SealBattleOriginalWeightCapacity");
			if(%storedOriginalWeightCapacity != "" && %storedOriginalWeightCapacity != -1)
			{
				$PlayerSkill[%aiId, $SkillWeightCapacity] = %storedOriginalWeightCapacity;
				echo("[SEAL BATTLE] HardcodeAIskills(): Restored original WeightCapacity: " @ %storedOriginalWeightCapacity @ " for seal battle bot (clientId=" @ %aiId @ ") before recalculating");
			}
			
			// CRITICAL: Now store the NEWLY CALCULATED skills as originals (only if they weren't stored before)
			// This happens AFTER HardcodeAIskills() has recalculated them from the restored base LVL
			%storedOriginalEndurance = fetchData(%aiId, "SealBattleOriginalEndurance");
			if(%storedOriginalEndurance == "" || %storedOriginalEndurance == -1)
			{
				// Store original Endurance immediately after HardcodeAIskills() sets it
				%originalEndurance = $PlayerSkill[%aiId, $SkillEndurance];
				storeData(%aiId, "SealBattleOriginalEndurance", %originalEndurance);
				$EnemyBotData[%aiId, "SealBattleOriginalEndurance"] = %originalEndurance;
				$ClientData[%aiId, "SealBattleOriginalEndurance"] = %originalEndurance;
				echo("[SEAL BATTLE] HardcodeAIskills(): Stored original Endurance: " @ %originalEndurance @ " for seal battle bot (clientId=" @ %aiId @ ")");
			}
			
			%storedOriginalEnergy = fetchData(%aiId, "SealBattleOriginalEnergy");
			if(%storedOriginalEnergy == "" || %storedOriginalEnergy == -1)
			{
				// Store original Energy immediately after HardcodeAIskills() sets it
				%originalEnergy = $PlayerSkill[%aiId, $SkillEnergy];
				storeData(%aiId, "SealBattleOriginalEnergy", %originalEnergy);
				$EnemyBotData[%aiId, "SealBattleOriginalEnergy"] = %originalEnergy;
				$ClientData[%aiId, "SealBattleOriginalEnergy"] = %originalEnergy;
				echo("[SEAL BATTLE] HardcodeAIskills(): Stored original Energy: " @ %originalEnergy @ " for seal battle bot (clientId=" @ %aiId @ ")");
			}
			
			%storedOriginalWeightCapacity = fetchData(%aiId, "SealBattleOriginalWeightCapacity");
			if(%storedOriginalWeightCapacity == "" || %storedOriginalWeightCapacity == -1)
			{
				// Store original WeightCapacity immediately after HardcodeAIskills() sets it
				%originalWeightCapacity = $PlayerSkill[%aiId, $SkillWeightCapacity];
				storeData(%aiId, "SealBattleOriginalWeightCapacity", %originalWeightCapacity);
				$EnemyBotData[%aiId, "SealBattleOriginalWeightCapacity"] = %originalWeightCapacity;
				$ClientData[%aiId, "SealBattleOriginalWeightCapacity"] = %originalWeightCapacity;
				echo("[SEAL BATTLE] HardcodeAIskills(): Stored original WeightCapacity: " @ %originalWeightCapacity @ " for seal battle bot (clientId=" @ %aiId @ ")");
			}
		}
		$PlayerSkill[%aiId, $SkillSenseHeading] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillHealing] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel) / 5;

		%a = (  (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel)  ) / 2;
		%sr = round(%a * GetSkillMultiplier(%aiId, $SkillOffensiveCasting));
		$PlayerSkill[%aiId, $SkillSpellResistance] = %sr;
		//=============================================================
	}
	else
	{
		// Seal battle bot with already scaled skills - DO NOT reset them
		// SetupBot() has already scaled Endurance, Energy, and WeightCapacity
		// We only need to ensure other skills are set (but not Endurance/Energy/WeightCapacity)
		echo("[SEAL BATTLE] HardcodeAIskills(): Seal battle bot (clientId=" @ %aiId @ ", round=" @ %sealBattleScaledRound @ ") already has scaled skills. Skipping Endurance/Energy/WeightCapacity reset to preserve scaled values.");
		
		// Only set skills that weren't scaled by SetupBot()
		%ns = getNumSkills();
		%a = $autoStartupSP + round($initSPcredits / %ns) + round(((fetchData(%aiId, "LVL")-1) * $SPgainedPerLevel) / %ns);
		for(%i = 1; %i <= %ns; %i++)
		{
			// Skip Endurance, Energy, and WeightCapacity - these are scaled by SetupBot()
			if(%i != $SkillEndurance && %i != $SkillEnergy && %i != $SkillWeightCapacity)
				AddSkillPoint(%aiId, %i, %a);
		}

		//==== HARDCODED SKILLS (excluding Endurance, Energy, WeightCapacity) ============
		$PlayerSkill[%aiId, $SkillSlashing] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")+1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillPiercing] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")+1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillBludgeoning] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")+1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillDodging] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-2) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillVehicleCombat] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		// SKIP: $PlayerSkill[%aiId, $SkillOffensiveCasting] - scaled by SetupBot() for spell damage
		$PlayerSkill[%aiId, $SkillDefensiveCasting] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillNeutralCasting] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		// SKIP: $PlayerSkill[%aiId, $SkillEnergy] - scaled by SetupBot()
		$PlayerSkill[%aiId, $SkillSpeech] = $SkillCap;
		// SKIP: $PlayerSkill[%aiId, $SkillWeightCapacity] - scaled by SetupBot()
		// SKIP: $PlayerSkill[%aiId, $SkillEndurance] - scaled by SetupBot()
		$PlayerSkill[%aiId, $SkillSenseHeading] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel);
		$PlayerSkill[%aiId, $SkillHealing] = (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel) / 5;

		%a = (  (getRandom() * $SkillRangePerLevel) + ((fetchData(%aiId, "LVL")-1) * $SkillRangePerLevel)  ) / 2;
		%sr = round(%a * GetSkillMultiplier(%aiId, $SkillOffensiveCasting));
		$PlayerSkill[%aiId, $SkillSpellResistance] = %sr;
		//=============================================================
	}
	
	// DEBUG: Check player object status after skills initialization
	// NOTE: This is called from AI::setWeapons, not directly from SpawnAI
	%playerObj = Client::getOwnedObject(%aiId);
	%playerName = Client::getName(%aiId);
	%botName = fetchData(%aiId, "BotInfoAiName");
	if(%botName == "" || %botName == "0") %botName = "Unknown";
	
	if(%playerObj == -1 || %playerObj == "")
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] HardcodeAIskills(): WARNING - Bot " @ %botName @ " (clientId=" @ %aiId @ ") has NO player object after skills initialization! Display name: '" @ %playerName @ "'. This may become a shell bot.");
	}
	else
	{
		// Only log if this is an enemy bot (to reduce spam)
		%spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] HardcodeAIskills(): Bot " @ %botName @ " (clientId=" @ %aiId @ ") skills initialized successfully. Display name: '" @ %playerName @ "', Player object: VALID");
		}
	}
	
	// CRITICAL: Validate player object exists and is valid before calling RefreshAll
	// Bot might have been deleted between spawn and this call
	// RefreshAll calls GetWeight() which calls Player::getItemCount for all items, so we need strict validation
	%playerObjCheck = Client::getOwnedObject(%aiId);
	if(%playerObjCheck != -1 && %playerObjCheck != "" && isObject(%playerObjCheck))
	{
		// Re-validate right before RefreshAll call (player object might be deleted during skill initialization)
		%playerObjFinal = Client::getOwnedObject(%aiId);
		if(%playerObjFinal != -1 && %playerObjFinal != "" && isObject(%playerObjFinal))
		{
			// CRITICAL: Check if this is a seal battle bot before clearing SpawnInvuln
			// Seal battle bots need to keep SpawnInvuln until SetupBot sets it properly
			%isSealBattleBot = fetchData(%aiId, "SealBattleBot");
			if(%isSealBattleBot != "true" && %isSealBattleBot != "True" && %isSealBattleBot != "1")
			{
				// Clear any remaining spawn invulnerability now that skills are set (only for non-seal battle bots)
				storeData(%aiId, "SpawnInvuln", "");
				%dispNameClear = Client::getName(%aiId);
				if(%dispNameClear != "" && %dispNameClear != -1)
					$SpawnInvulnByName[%dispNameClear] = "";
			}
			else
			{
				// Seal battle bot - SetupBot will handle SpawnInvuln, but ensure freeze is set immediately
				// This ensures bot is frozen as soon as it's initialized, not waiting for SetupBot retries
				storeData(%aiId, "frozen", "true");
				$BotFrozen[%aiId] = "true";
				$EnemyBotData[%aiId, "frozen"] = "true";
				
				// CRITICAL: For Player objects, use AI::setVar to actually freeze the bot
				%botAiName = fetchData(%aiId, "BotInfoAiName");
				if(%botAiName == "" || %botAiName == -1 || %botAiName == "0")
					%botAiName = $BotInfoAiName[%aiId];
				if(%botAiName != "" && %botAiName != -1 && %botAiName != "0")
				{
					AI::setVar(%botAiName, seekOff, 1); // Disable AI seeking
					AI::setVar(%botAiName, SpotDist, 0); // Disable targeting
					AI::newDirectiveRemove(%botAiName, 99); // Remove movement directives
				}
				
				echo("[SEAL BATTLE] HardcodeAIskills(): Set freeze flag and AI vars immediately for seal battle bot (clientId=" @ %aiId @ ", aiName=" @ %botAiName @ ")");
			}
			
			// CRITICAL: ALWAYS set team RIGHT BEFORE RefreshAllEnemyBot() runs to ensure it's set
			// GameBase::setTeam() is NOT synchronous - it takes time for the engine to process
			// We must set it and then wait a moment before calling RefreshAllEnemyBot()
			%storedBotTeam = fetchData(%aiId, "botTeam");
			
			// CRITICAL: If botTeam is not stored or is invalid, determine it from BotInfo
			// DO NOT hardcode to 11 - different bot types have different teams!
			// NOTE: stored team 0 is VALID (friendly TempSpawn bots) - only ""/-1 = unset
			if(%storedBotTeam == "" || %storedBotTeam == -1)
			{
				// Try to get from BotInfoAiName
				%botInfoAiName = fetchData(%aiId, "BotInfoAiName");
				if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
				{
					%botInfoTeam = $BotInfo[%botInfoAiName, TEAM];
					if(%botInfoTeam != "" && %botInfoTeam != -1 && %botInfoTeam != "0" && %botInfoTeam != 0)
					{
						%storedBotTeam = %botInfoTeam;
						storeData(%aiId, "botTeam", %botInfoTeam);
						if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] HardcodeAIskills - Retrieved botTeam from BotInfo: " @ %botInfoTeam @ " for " @ %botInfoAiName);
					}
				}
				
				// If still not found, default to team 1 (enemy) but log warning
				if(%storedBotTeam == "" || %storedBotTeam == -1)
				{
					echo("WARNING: HardcodeAIskills - No team info for bot " @ %botInfoAiName @ " (clientId=" @ %aiId @ "). Defaulting to team 1.");
					%storedBotTeam = 1;
					storeData(%aiId, "botTeam", 1);
				}
			}
			
			// If botTeam is stored, ALWAYS set it right before RefreshAll() (don't just check)
			// This ensures the team is set even if GameBase::setTeam() wasn't synchronous earlier
			if(%storedBotTeam != "" && %storedBotTeam != -1)
			{
				// CRITICAL: Set team on client ID FIRST (same as RefreshAll checks)
				// Set it multiple times to ensure it takes effect
				GameBase::setTeam(%aiId, %storedBotTeam);
				GameBase::setTeam(%playerObjFinal, %storedBotTeam);
				GameBase::setTeam(%aiId, %storedBotTeam); // Set again on client ID
				
				// NOTE: GameBase::getTeam() is NOT synchronous - it may return stale values
				// We set the team above, so we trust it's set and proceed with RefreshAllEnemyBot()
				// The team is enforced later by EnforceEnemyBotTeam (via ScheduleTeamEnforcement) if needed
				// Don't check immediately after setting - it's unreliable due to timing
				if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] HardcodeAIskills - Set team to " @ %storedBotTeam @ " for " @ %botName @ " (clientId=" @ %aiId @ ") before RefreshAllEnemyBot()");
				// Call RefreshAllEnemyBot() instead of RefreshAll() - it does NOT touch team
				RefreshAllEnemyBot(%aiId);
				
				// CRITICAL: For seal battle bots, re-apply scaled stats after RefreshAllEnemyBot()
				// RefreshAllEnemyBot() recalculates DEF, MDEF, ATK, DMG from base values, overwriting scaled stats
				%sealBattleRound = fetchData(%aiId, "SealBattleScaledRound");
				if(%sealBattleRound != "" && %sealBattleRound != -1 && %sealBattleRound != "0" && %sealBattleRound != 0)
				{
					// This is a seal battle bot with scaled stats - re-apply them immediately
					// Check if we have stored scaled stats before calling
					%checkStoredDEF = $SealBattleScaledStats[%aiId, "DEF"];
					if(%checkStoredDEF != "" && %checkStoredDEF != -1)
					{
						echo("[SEAL BATTLE] HardcodeAIskills(): Re-applying scaled stats for seal battle bot (clientId=" @ %aiId @ ", round=" @ %sealBattleRound @ ") after RefreshAllEnemyBot()");
						SealBattle::ReapplyScaledStats(%aiId, %sealBattleRound);
					}
					else
					{
						echo("[SEAL BATTLE] HardcodeAIskills(): WARNING - Seal battle bot (clientId=" @ %aiId @ ", round=" @ %sealBattleRound @ ") has no stored scaled stats yet. SetupBot may not have run yet.");
					}
				}
			}
			else
			{
				if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] WARNING - HardcodeAIskills - No stored botTeam for " @ %botName @ " (clientId=" @ %aiId @ ")");
				// No stored team - call RefreshAllEnemyBot() anyway (it does NOT touch team)
				RefreshAllEnemyBot(%aiId);
				
				// CRITICAL: For seal battle bots, re-apply scaled stats after RefreshAllEnemyBot()
				// RefreshAllEnemyBot() recalculates DEF, MDEF, ATK, DMG from base values, overwriting scaled stats
				%sealBattleRound = fetchData(%aiId, "SealBattleScaledRound");
				if(%sealBattleRound != "" && %sealBattleRound != -1 && %sealBattleRound != "0" && %sealBattleRound != 0)
				{
					// This is a seal battle bot with scaled stats - re-apply them immediately
					// Check if we have stored scaled stats before calling
					%checkStoredDEF = $SealBattleScaledStats[%aiId, "DEF"];
					if(%checkStoredDEF != "" && %checkStoredDEF != -1)
					{
						echo("[SEAL BATTLE] HardcodeAIskills(): Re-applying scaled stats for seal battle bot (clientId=" @ %aiId @ ", round=" @ %sealBattleRound @ ") after RefreshAllEnemyBot()");
						SealBattle::ReapplyScaledStats(%aiId, %sealBattleRound);
					}
					else
					{
						echo("[SEAL BATTLE] HardcodeAIskills(): WARNING - Seal battle bot (clientId=" @ %aiId @ ", round=" @ %sealBattleRound @ ") has no stored scaled stats yet. SetupBot may not have run yet.");
					}
				}
			}
		}
		else
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] HardcodeAIskills(): Bot " @ %botName @ " (clientId=" @ %aiId @ ") player object became invalid right before RefreshAll call. Skipping RefreshAll to prevent errors.");
		}
	}
		else
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] HardcodeAIskills(): Bot " @ %botName @ " (clientId=" @ %aiId @ ") has NO player object before RefreshAll call. Skipping RefreshAll to prevent errors.");
		}
	
	// DEFENSIVE FIX: Final startFadeIn call to ensure enemy bot visibility
	// Fixes intermittent invisibility that can occur during spawn
	%finalPlayerObj = Client::getOwnedObject(%aiId);
	if(%finalPlayerObj != "" && %finalPlayerObj != -1 && isObject(%finalPlayerObj))
	{
		GameBase::startFadeIn(%finalPlayerObj);
	}
}

// Periodic check to fix enemy bots that are on team -1
// This catches cases where team wasn't set correctly during spawn
function PeriodicBotTeamCheck()
{
	%botList = GetBotIdList();
	%fixedCount = 0;
	
	for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
	{
		// Only check enemy bots (those with SpawnBotInfo)
		%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
		if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
			continue; // Not an enemy bot, skip
		
		// CRITICAL: Check Player object first (team is more reliably set there)
		// Then fall back to client ID if Player object doesn't exist
		%playerObj = Client::getOwnedObject(%botId);
		%currentTeam = -1;
		%teamFromPlayerObj = -1;
		%teamFromClientId = -1;
		if(%playerObj != -1 && %playerObj != "")
		{
			%teamFromPlayerObj = GameBase::getTeam(%playerObj);
			%currentTeam = %teamFromPlayerObj;
		}
		// If still -1, check client ID
		if(%currentTeam == -1)
		{
			%teamFromClientId = GameBase::getTeam(%botId);
			%currentTeam = %teamFromClientId;
		}
		
		if(%currentTeam == -1)
		{
			// Bot is on team -1 - try to fix it
			%storedBotTeam = fetchData(%botId, "botTeam");
			%botName = fetchData(%botId, "BotInfoAiName");
			
			// DEBUG: Log detailed info about why team is -1
			echo("WARNING: PeriodicBotTeamCheck - Enemy bot " @ %botName @ " (clientId=" @ %botId @ ") has team -1. PlayerObj team: " @ %teamFromPlayerObj @ ", ClientId team: " @ %teamFromClientId @ ", storedBotTeam: " @ %storedBotTeam);
			
			// NOTE: stored team 0 is VALID (friendly TempSpawn bots) - only ""/-1 = unset
			if(%storedBotTeam != "" && %storedBotTeam != -1)
			{
				// Use stored team
				// CRITICAL: Set team on Player object FIRST (more reliable), then client ID
				%playerObj = Client::getOwnedObject(%botId);
				if(%playerObj != -1 && %playerObj != "")
				{
					GameBase::setTeam(%playerObj, %storedBotTeam);
					// Also set on client ID as backup
					GameBase::setTeam(%botId, %storedBotTeam);
					
					// Verify team was actually set
					%verifyTeam = GameBase::getTeam(%playerObj);
					if(%verifyTeam == -1)
					{
						// Still -1 - retry with client ID
						GameBase::setTeam(%botId, %storedBotTeam);
						GameBase::setTeam(%playerObj, %storedBotTeam);
						%verifyTeam2 = GameBase::getTeam(%playerObj);
						if(%verifyTeam2 == -1)
						{
							echo("ERROR: PeriodicBotTeamCheck - Failed to set team for " @ %botName @ " (clientId=" @ %botId @ ") - team still -1 after retry!");
						}
					}
				}
				else
				{
					// No Player object - set on client ID only
					GameBase::setTeam(%botId, %storedBotTeam);
				}
				echo("WARNING: PeriodicBotTeamCheck - Fixed team -1 for enemy bot " @ %botName @ " (clientId=" @ %botId @ ") to team " @ %storedBotTeam);
				%fixedCount++;
			}
			else if(%botName != "" && %botName != -1 && %botName != "0")
			{
				// Try to get team from BotInfo
				%botInfoTeam = $BotInfo[%botName, TEAM];
				if(%botInfoTeam != "" && %botInfoTeam != -1 && %botInfoTeam != "0" && %botInfoTeam != 0)
				{
					// Store it and set the team
					storeData(%botId, "botTeam", %botInfoTeam);
					// CRITICAL: Set team on Player object FIRST (more reliable), then client ID
					%playerObj = Client::getOwnedObject(%botId);
					if(%playerObj != -1 && %playerObj != "")
					{
						GameBase::setTeam(%playerObj, %botInfoTeam);
						// Also set on client ID as backup
						GameBase::setTeam(%botId, %botInfoTeam);
					}
					else
					{
						// No Player object - set on client ID only
						GameBase::setTeam(%botId, %botInfoTeam);
					}
					echo("WARNING: PeriodicBotTeamCheck - Fixed team -1 for enemy bot " @ %botName @ " (clientId=" @ %botId @ ") to team " @ %botInfoTeam @ " from BotInfo");
					%fixedCount++;
				}
				else
				{
					// Default to team 1 (enemy)
					storeData(%botId, "botTeam", 1);
					// CRITICAL: Set team on Player object FIRST (more reliable), then client ID
					%playerObj = Client::getOwnedObject(%botId);
					if(%playerObj != -1 && %playerObj != "")
					{
						GameBase::setTeam(%playerObj, 1);
						// Also set on client ID as backup
						GameBase::setTeam(%botId, 1);
					}
					else
					{
						// No Player object - set on client ID only
						GameBase::setTeam(%botId, 1);
					}
					echo("WARNING: PeriodicBotTeamCheck - Fixed team -1 for enemy bot " @ %botName @ " (clientId=" @ %botId @ ") to team 1 (default)");
					%fixedCount++;
				}
			}
		}
	}
	
	if(%fixedCount > 0)
		echo("PeriodicBotTeamCheck - Fixed " @ %fixedCount @ " enemy bot(s) that were on team -1");
	
	// Schedule next check in 30 seconds (was 15s; team-1 fixes are rare enough
	// that the full GetBotIdList scan twice as often wasn't buying anything -
	// the unconditional WARNING echoes will show if fixes start firing again)
	schedule("PeriodicBotTeamCheck();", 30);
}

// ============================================================================
// AGGRESSIVE TEAM ENFORCEMENT FOR ENEMY BOTS
// This function is called multiple times after spawn to ensure team sticks
// ============================================================================

function EnforceEnemyBotTeam(%clientId, %expectedTeam, %attempts)
{
	Watchdog_Enter("EnforceEnemyBotTeam");
	// Validate inputs
	if(%clientId == -1 || %clientId == "" || %expectedTeam == "")
		return;
	
	// Initialize attempts counter
	if(%attempts == "" || %attempts == -1)
		%attempts = 0;
	
	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return;  // Bot doesn't exist anymore
	
	// NOTE: Don't check IsBotRegistered here because RegisterBot is called AFTER ScheduleTeamEnforcement
	// Just verify the player object exists and enforce the team
	
	// Get current team from Player object (more reliable than client ID)
	%currentTeam = GameBase::getTeam(%playerObj);
	
	if(%currentTeam != %expectedTeam)
	{
		// Team is wrong - enforce the correct team
		if($AI_DEBUG_ENABLED) echo("[TEAM ENFORCE] EnforceEnemyBotTeam: clientId=" @ %clientId @ ", attempt=" @ %attempts @ ", current=" @ %currentTeam @ ", expected=" @ %expectedTeam @ " - FIXING");
		
		// Set team on BOTH Player object AND client ID
		GameBase::setTeam(%playerObj, %expectedTeam);
		GameBase::setTeam(%clientId, %expectedTeam);
		
		// Also update stored team value
		storeData(%clientId, "botTeam", %expectedTeam);
		$BotRegistry[%clientId, "team"] = %expectedTeam;
		
		// Schedule another check with exponential backoff (up to 5 attempts)
		if(%attempts < 5)
		{
			%nextDelay = 0.2 * (%attempts + 1);  // 0.2s, 0.4s, 0.6s, 0.8s, 1.0s
			schedule("EnforceEnemyBotTeam(" @ %clientId @ ", " @ %expectedTeam @ ", " @ (%attempts + 1) @ ");", %nextDelay);
		}
		else
		{
			if($TEAM_ENFORCE_DEBUG) echo("[TEAM ENFORCE] EnforceEnemyBotTeam: Max attempts reached for clientId=" @ %clientId @ ". Final team=" @ GameBase::getTeam(%playerObj));
		}
	}
}

// Schedule aggressive team enforcement for a new bot
function ScheduleTeamEnforcement(%clientId, %expectedTeam)
{
	// Reject only ""/-1 as invalid. Team 0 (Citizen) IS valid here: TempSpawn issuers
	// can spawn FRIENDLY bots (e.g. invasion/arena "General" on team 0) - the old
	// %expectedTeam == 0 rejection coerced those to team 1 and made them hostile.
	if(%clientId == -1 || %clientId == "" || %expectedTeam == "" || %expectedTeam == -1)
	{
		if($TEAM_ENFORCE_DEBUG) echo("[TEAM ENFORCE] ScheduleTeamEnforcement: Invalid parameters - clientId=" @ %clientId @ ", expectedTeam=" @ %expectedTeam @ " - Using default team 1");
		%expectedTeam = 1; // Default to team 1 (enemy) if invalid
	}
	
	if($TEAM_ENFORCE_DEBUG) echo("[TEAM ENFORCE] ScheduleTeamEnforcement: Scheduling enforcement for clientId=" @ %clientId @ " with expectedTeam=" @ %expectedTeam);
	
	// OPTIMIZED: Single initial call at 0.3s - EnforceEnemyBotTeam has built-in retry logic
	// with exponential backoff (up to 5 retries at 0.2s, 0.4s, 0.6s, 0.8s, 1.0s)
	// Previous code scheduled 5 calls which could cascade to 30 schedules per bot!
	schedule("EnforceEnemyBotTeam(" @ %clientId @ ", " @ %expectedTeam @ ", 0);", 0.3);
}
