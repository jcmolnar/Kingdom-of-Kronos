//============================================================================
// ai_tempspawn.cs — split from Ai.cs (scatter split, ai.cs peel)
// Extracted 2026-07-18 at commit 320698f. Mechanical text move, no behavior change.
// Command-spawn front door (DetermineBotTeam / AI::helper / SpawnAI). NOTE: 'TempSpawn' is a commandIssuer flavor, not a separate pipeline — these functions ALSO serve enemy SpawnPoint spawns; arena/seal/daily/weekly event bots enter here with commandIssuer 'TempSpawn'.
// exec'd by Ai.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====

//------------------------------------------------------------------
// Helper Functions for Bot Team Determination
// NOTE: isTownBot() and isEnemyBot() are defined earlier in this file (the isTownBot/isEnemyBot helper functions)
// to preserve comprehensive safeguards including save file checks and registry lookups
//------------------------------------------------------------------

// Centralized function to determine bot team from various sources
// Returns the team number, or -1 if unable to determine
function DetermineBotTeam(%botName, %displayName, %commandIssuer, %clientId)
{
	// Priority 1: TempSpawn - team is specified in command string (Word 4)
	if(GetWord(%commandIssuer, 0) == "TempSpawn")
	{
		%botTeam = GetWord(%commandIssuer, 4);
		if(%botTeam != "" && %botTeam != -1)
			return %botTeam;
		return 1; // Default to team 1 if not specified
	}
	
	// Priority 2: MarkerSpawn - team from map name
	if(GetWord(%commandIssuer, 0) == "MarkerSpawn")
	{
		%botTeam = GameBase::getMapName(GetWord(%commandIssuer, 1));
		if(%botTeam != "" && %botTeam != -1)
			return %botTeam;
		return 1; // Default to team 1 (enemy)
	}
	
	// Priority 3: Direct lookup via $NameForRace -> $TeamForRace (most reliable for enemy bots)
	// This is the standard way teams are defined in enemyarmors.cs
	%guardtype = StripTrailingDigits(%botName);
	if(%guardtype != "" && %guardtype != -1)
	{
		%botRace = $NameForRace[%guardtype];
		if(%botRace != "" && %botRace != -1)
		{
			%teamFromRace = $TeamForRace[%botRace];
			if(%teamFromRace != "" && %teamFromRace != -1 && %teamFromRace != "0" && %teamFromRace != 0)
			{
				if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] DetermineBotTeam: Found team " @ %teamFromRace @ " via $NameForRace->$TeamForRace (guardtype=" @ %guardtype @ ", race=" @ %botRace @ ")");
				return %teamFromRace;
			}
		}
	}
	
	// Priority 4: Fall back to $BotInfo TEAM setting (for custom bot definitions)
	%botTeam = $BotInfo[%botName, TEAM];
	if(%botTeam != "" && %botTeam != -1 && %botTeam != "0" && %botTeam != 0)
		return %botTeam;
	
	// Priority 5: Determine from race stored on client (if clientId provided)
	%botRace = "";
	if(%clientId != -1 && %clientId != "")
	{
		%botRace = fetchData(%clientId, "RACE");
		if(%botRace == "" || %botRace == -1)
		{
			// Try to get race from armor type
			%armor = Player::getArmor(%clientId);
			if(%armor != "" && %armor != -1 && $ArmorTypeToRace[%armor] != "")
				%botRace = $ArmorTypeToRace[%armor];
		}
	}
	
	// Determine team from race
	if(%botRace != "" && %botRace != -1 && $TeamForRace[%botRace] != "" && $TeamForRace[%botRace] != "0" && $TeamForRace[%botRace] != 0)
		return $TeamForRace[%botRace];
	
	// Priority 5: Fallback to display name pattern matching
	// CRITICAL: Use != -1 instead of == 0 to match pattern ANYWHERE in name
	// This allows names like "Giant Demon Lord" to match "Demon"
	if(%displayName != "" && %displayName != -1)
	{
		if(String::findSubStr(%displayName, "Admin") != -1)
			return 11; // Admin bots (team 11)
		else if(String::findSubStr(%displayName, "Ogre") != -1)
			return 2; // Ogres
		else if(String::findSubStr(%displayName, "Pigman") != -1)
			return 3; // Pigmen
		else if(String::findSubStr(%displayName, "Zombie") != -1 || String::findSubStr(%displayName, "Undead") != -1)
			return 4; // Undead
		else if(String::findSubStr(%displayName, "Demon") != -1)
			return 5; // Demons
		else if(String::findSubStr(%displayName, "God") != -1)
			return 9; // Gods
		else if(String::findSubStr(%displayName, "Minotaur") != -1)
			return 6; // Minotaur
		else if(String::findSubStr(%displayName, "Alien") != -1)
			return 7; // Aliens
		else if(String::findSubStr(%displayName, "Seal") != -1)
			return 8; // Seals
		else if(String::findSubStr(%displayName, "Angel") != -1)
			return 10; // Angels
		else if(String::findSubStr(%displayName, "Void") != -1)
			return 12; // Void enemies
	}
	
	// Default: team 1 (Enemy)
	return 1;
}
function AI::helper(%aiName, %displayName, %commandIssuer, %loadout, %spawnPointId)
{
	// Initialize %loadout to empty string if not provided
	if(%loadout == "")
		%loadout = "";
	
	dbecho($dbechoMode, "AI::helper(" @ %aiName @ ", " @ %displayName @ ", " @ %commandIssuer @ ", " @ %loadout @ ", " @ %spawnPointId @ ")");
	//echo("[SPAWN DEBUG] AI::helper(): aiName=" @ %aiName @ ", commandIssuer=" @ %commandIssuer);

	if(GetWord(%commandIssuer, 0) == "TempSpawn")
	{
		//the %commandIssuer is a data string
		%spawnPos = GetWord(%commandIssuer, 1) @ " " @ GetWord(%commandIssuer, 2) @ " " @ GetWord(%commandIssuer, 3);
	}
	else if(GetWord(%commandIssuer, 0) == "MarkerSpawn")
	{
		//the %commandIssuer is a marker
		%spawnPos = GameBase::getPosition(GetWord(%commandIssuer, 1));
	}
	else if(GetWord(%commandIssuer, 0) == "SpawnPoint")
	{
		//the %commandIssuer is a Spawn Point
		//we must now figure out a position around this Spawn Point

		%spawnpoint = GetWord(%commandIssuer, 1);
		//echo("[SPAWN DEBUG] AI::helper(): SpawnPoint type, spawnpoint=" @ %spawnpoint);

		%info = Object::getName(%spawnpoint);

		%minrad = GetWord(%info, 1);
		%maxrad = GetWord(%info, 2);

		%spawnPointPos = GameBase::getPosition(%spawnpoint);
		
		%tempPos = RandomPositionXY(%minrad, %maxrad);
		%xPos = GetWord(%tempPos, 0) + GetWord(%spawnPointPos, 0);
		%yPos = GetWord(%tempPos, 1) + GetWord(%spawnPointPos, 1);
		%zPos = GetWord(%spawnPointPos, 2);

		%spawnPos = %xPos @ " " @ %yPos @ " " @ %zPos;
		
		// CRITICAL FIX #2: Counter is now managed by transaction system (ReserveSpawnSlot in SpawnLoop)
		// Do NOT increment here - ReserveSpawnSlot already did it atomically
	}

%n = getAInumber();
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] AI::helper(): got AI number=" @ %n);

	// Bail if the AI number pool is exhausted (before $numAI++/telemetry, so no
	// counters need unwinding - just release the reserved spawn slot)
	if(%n == "" || %n == -1)
	{
		echo("ERROR: AI::helper - no AI number available, aborting spawn of " @ %aiName);
		if(%spawnPointId != "" && %spawnPointId != -1)
			RollbackSpawnSlot(%spawnPointId);
		return -1;
	}

%newName = %aiName @ %n;
if(%aiName == %displayName)
{
	// CRITICAL: Skip race prefix for Colloseum bots (they use dedicated bot types)
	// Colloseum bots have internal names like "DemiGodRoundOne", "TitanRoundTwo", etc.
	%isColloseumBot = (String::findSubStr(%aiName, "RoundOne") != -1 || 
	                   String::findSubStr(%aiName, "RoundTwo") != -1 || 
	                   String::findSubStr(%aiName, "RoundThree") != -1);
	
	if(%isColloseumBot)
	{
		// Colloseum bot - use newName directly as display name (no race prefix)
		%displayName = %newName;
	}
	else
	{
		// Regular bot - apply race prefix as normal
		%displayName = $NameForRace[%aiName] @ %newName;
	}
}
$numAI++;
$Telemetry_NumAI_Inc++;  // Track $numAI increments
Telemetry_RecordSpawnAttempt();  // Track spawn attempt
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] AI::helper(): newName=" @ %newName @ ", displayName=" @ %displayName @ ", calling SpawnAI()");
	// CRITICAL: Delay moved to SpawnAI() itself to prevent multiple scheduled spawns when called from loops
	// CRITICAL FIX #2: Pass spawnPointId to SpawnAI so it can commit/rollback the transaction
	%spawnResult = SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout, %spawnPointId);
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] AI::helper(): SpawnAI() returned=" @ %spawnResult);
	
	// CRITICAL FIX: If spawn failed, record failure and optionally rollback slot
	if(%spawnResult == -1 || %spawnResult == "")
	{
		// Rollback spawn slot if this was a spawn point spawn
		if(%spawnPointId != "" && %spawnPointId != -1)
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] AI::helper(): Spawn FAILED - rolling back reserved slot for spawnPoint " @ %spawnPointId);
			RollbackSpawnSlot(%spawnPointId);
		}
		// CRITICAL FIX: ALWAYS record failure to decrement $numAI (balances the earlier $numAI increment for this spawn attempt)
		// This was previously only called for spawnpoint spawns, causing $numAI leaks for TempSpawn/Town bots
		Telemetry_RecordSpawnFailed("other");
		return -1;
	}

	setAInumber(%newName, %n);

	return %newName;
}
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout, %spawnPointId)
{
	// CRITICAL: Check server capacity before attempting spawn
	// This prevents the engine from rejecting spawns or crashing when full
	// NOTE: We only use this for capacity check, NOT for prediction (prediction was always wrong)
	%capacityCheck = PlayerManager::getFreeId();
	if(%capacityCheck == -1)
	{
		echo("CRITICAL: SpawnAI - Server is FULL! Aborting spawn for " @ %newName @ " (displayName: " @ %displayName @ ")");
		// review #5: do NOT roll back or record failure here. This returns -1 to the
		// sole caller AI::helper, which unconditionally does RollbackSpawnSlot +
		// Telemetry_RecordSpawnFailed for EVERY -1 (using its own %spawnPointId, which
		// owns the reservation). Doing it here too double-decremented
		// $numAIperSpawnPoint (over-spawn) and $numAI. (Matches the already-correct
		// "already scheduled" path below, which deliberately defers to AI::helper.)
		return -1;
	}
	// Initialize %loadout to empty string if not provided
	if(%loadout == "")
		%loadout = "";
	
	dbecho($dbechoMode, "SpawnAI(" @ %newName @ ", " @ %displayName @ ", " @ %aiSpawnPos @ ", " @ %commandIssuer @ ", " @ %loadout @ ", " @ %spawnPointId @ ")");
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAI(): ENTRY - newName=" @ %newName @ ", displayName=" @ %displayName @ ", commandIssuer=" @ %commandIssuer @ ", spawnPointId=" @ %spawnPointId);

	// CRITICAL: Check if this bot is already spawned or scheduled to spawn
	// This prevents multiple spawns when called from loops
	// Use AI::getClientIdFromName() first (no error messages) instead of AI::getId() which logs errors
	%existingId = AI::getClientIdFromName(%newName);
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAI(): Check existing bot - getClientIdFromName(" @ %newName @ ") returned: " @ %existingId);
	if(%existingId != -1 && %existingId != "" && %existingId != "False" && %existingId != "false")
	{
		// Bot name exists - check if player object is valid
		%existingPlayerObj = Client::getOwnedObject(%existingId);
		if(%existingPlayerObj != -1 && %existingPlayerObj != "")
		{
			// Bot already exists with valid player object - don't spawn again
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): WARNING - Bot " @ %newName @ " already exists with valid player object (clientId=" @ %existingId @ "). Skipping duplicate spawn.");
			return -1;
		}
		else
		{
			// Bot name exists but player object is invalid - this is a shell bot
			
			// SAFEGUARD: Verify it's not a real player before cleaning up
			if(!IsSafeToModify(%existingId, "SpawnAI Shell Cleanup"))
			{
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
					echo("[SPAWN FLOW] SpawnAI(): STARTUP ABORTED - Existing client " @ %existingId @ " (" @ %newName @ ") is protected.");
				return -1; 
			}

			// Clean it up before spawning a new one
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): WARNING - Bot " @ %newName @ " exists but player object is invalid (shell bot, clientId=" @ %existingId @ "). Cleaning up before respawn...");
			
			// CRITICAL: Run full CleanupBot so AI number, counters, and registry entries are released
			// (PreSpawnCleanup alone does not free AI numbers, which caused names to climb to 100+)
			CleanupBot(%existingId, %newName);

			%escapedName = String::replace(%newName, "\"", "\\\"");
			AI::delete(%escapedName);
			// NOTE: no "wait" is possible here (schedule("") is a no-op) - if the engine
			// hasn't finished the delete, createAI's "already exists" handler defers a retry
		}
	}
	
	// Check if spawn is already scheduled for this bot
	%isScheduled = $SpawnAIScheduled[%newName];
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAI(): Check scheduled flag - $SpawnAIScheduled[" @ %newName @ "] = '" @ %isScheduled @ "'");
	if(%isScheduled == "true")
	{
		// Spawn already scheduled - don't schedule again
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
			echo("[SPAWN FLOW] SpawnAI(): WARNING - Spawn already scheduled for bot " @ %newName @ ". Skipping duplicate schedule.");
		
		// CRITICAL FIX: Return -1 so AI::helper rolls back the slot and decrements $numAI
		// We don't need to manually rollback here because AI::helper will do it when we return -1
		return -1;
	}
	
	// CRITICAL FIX #2: Extract spawnPointId from parameter or commandIssuer
	%isSpawnPoint = false;
	if(%spawnPointId == "" || %spawnPointId == -1)
	{
		if(GetWord(%commandIssuer, 0) == "SpawnPoint")
		{
			%isSpawnPoint = true;
			%spawnPointId = GetWord(%commandIssuer, 1);
		}
	}
	else
	{
		%isSpawnPoint = true;
	}
	// CRITICAL FIX #2: Counter is now managed by transaction system (ReserveSpawnSlot in SpawnLoop)
	// Do NOT increment/decrement here - use CommitSpawnSlot/RollbackSpawnSlot
	
	// CRITICAL FIX: With transactional spawn system, we spawn immediately while holding the reservation
	// No artificial delays needed - the slot is already reserved atomically in SpawnLoop
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAI(): Proceeding immediately with spawn (slot already reserved)");
	$SpawnAIScheduled[%newName] = ""; // Clear scheduled flag
	$EnemyBotSpawnRetry[%newName] = ""; // Clear retry flag
	
	// Determine if we should bypass the race condition check
	// Critical systems like Seal Battle and Arena use "TempSpawn" or "MarkerSpawn" and rely on tight timing
	// They cannot handle the 3-second rejection delay without breaking their logic chains
	%bypassRaceCheck = false;
	%issuerType = GetWord(%commandIssuer, 0);
	if(%issuerType == "TempSpawn" || %issuerType == "MarkerSpawn")
	{
		%bypassRaceCheck = true;
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
			echo("[SPAWN FLOW] SpawnAI(): Bypassing race condition check for critical system issuer: " @ %issuerType);
	}
	
	// Proceed with spawn immediately
	// Pass skipPostSpawn=true for SpawnPoint bots (they're handled by SpawnAIGetClientId())
	
	// CRITICAL: Store commandIssuer BEFORE calling createAI() so that if the spawn is deferred,
	// the deferred retry can pass it to SpawnAIGetClientId for proper SpawnBotInfo tracking
	$DeferredSpawnCommandIssuer[%newName] = %commandIssuer;
	
	%retval = createAI(%newName, %aiSpawnPos, %displayName, true, %bypassRaceCheck);
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAI(): createAI() returned: " @ %retval);
	
	// Clear deferred context if spawn wasn't deferred (normal path doesn't need it)
	if(%retval != "deferred")
		$DeferredSpawnCommandIssuer[%newName] = "";

	if(%retval != -1)
	{
		// CRITICAL: Try to get client ID immediately to set team before UpdateTeam() runs
		// UpdateTeam() may be called by Game::playerSpawned() before SpawnAIGetClientId() runs
		// If we can get the client ID now, set SpawnBotInfo and team immediately to prevent UpdateTeam() from setting it to team 1
		%immediateClientId = NEWgetClientByName(%displayName);
		if(%immediateClientId != -1 && %immediateClientId != "")
		{
			// Got client ID immediately - set SpawnBotInfo and team right away
			// Use centralized team determination function
			%botTeam = DetermineBotTeam(%newName, %displayName, %commandIssuer, %immediateClientId);
			
			// Set SpawnBotInfo immediately so UpdateTeam() recognizes it as an enemy bot
			if(%isSpawnPoint)
			{
				%spawnBotInfo = "SpawnPoint " @ %spawnPointId;
			}
			else
			{
				%spawnBotInfo = %commandIssuer;
			}
			
			// CRITICAL: Set $BotType BEFORE any storeData() calls
			// This ensures data routes to $EnemyBotData, not $ClientData
			$BotType[%immediateClientId] = "enemy";
			
			storeData(%immediateClientId, "SpawnBotInfo", %spawnBotInfo);
			$EnemyBotData[%immediateClientId, "SpawnBotInfo"] = %spawnBotInfo;
			
			// CRITICAL FIX: Clear ExpDistributed flag for new enemy bots
			// This prevents the bot from inheriting "EXP already distributed" status from a previous bot/client
			storeData(%immediateClientId, "ExpDistributed", "");
			
			// Set team immediately to prevent UpdateTeam() from setting it to team 1
			// Store botTeam first so RefreshAll() can restore it if needed
			storeData(%immediateClientId, "botTeam", %botTeam);
			
			// Set team using GameBase::setTeam (Player::setTeam doesn't exist)
			%playerObj = Client::getOwnedObject(%immediateClientId);
			if(%playerObj != -1 && %playerObj != "")
				GameBase::setTeam(%playerObj, %botTeam);
			else
				GameBase::setTeam(%immediateClientId, %botTeam);
			
			// Schedule aggressive team enforcement with multiple retries
			ScheduleTeamEnforcement(%immediateClientId, %botTeam);
			
			// OPTIMIZED: Removed redundant VerifyEnemyBotTeam schedule - ScheduleTeamEnforcement handles this
			
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): Set team immediately to " @ %botTeam @ " for " @ %newName @ " (clientId=" @ %immediateClientId @ ") to prevent UpdateTeam() override");
		}
		
		// CRITICAL: AI::spawn() creates a Player object, but it takes time to register in the client list
		// We need to wait a moment before trying to get the client ID
		// Schedule the client ID lookup with a short delay (0.5s; engine creates objects almost instantly)
		// CRITICAL: Verify AI::spawn() actually succeeded before scheduling lookup
		// createAI() returns the AI name on success, -1 on failure, "deferred" if delayed due to player connecting
		if(%retval == "deferred")
		{
			// Spawn was deferred due to player actively connecting
			// Don't schedule SpawnAIGetClientId - the deferred spawn will handle everything when it runs
			// Rollback the reserved slot since we didn't actually spawn
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): Spawn deferred for " @ %newName @ " - rolling back slot and returning (deferred spawn will retry)");
			if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
			{
				RollbackSpawnSlot(%spawnPointId);
			}
			return "deferred";
		}
		else if(%newName != "" && %newName != -1)
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): Scheduling client ID lookup in 0.5s (engine creates objects almost instantly)");
			// Pass spawnPointId to SpawnAIGetClientId
			%spawnPointIdForGetId = "";
			if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				%spawnPointIdForGetId = %spawnPointId;
			// NOTE: predictedId removed - was always returning wrong ID (town bot instead of new bot)
			// AI::getId() inside SpawnAIGetClientId works correctly and finds the bot immediately
			schedule("SpawnAIGetClientId(\"" @ %newName @ "\", \"" @ %displayName @ "\", \"" @ %aiSpawnPos @ "\", \"" @ %commandIssuer @ "\", \"" @ %loadout @ "\", \"" @ %spawnPointIdForGetId @ "\", \"\");", 0.5);
			return %newName; // Return immediately, client ID lookup happens in scheduled call
		}
		else
		{
			// createAI() failed - don't schedule lookup
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): WARNING - createAI() failed for " @ %newName @ ", not scheduling client ID lookup");
			// review #5: no rollback here - AI::helper rolls back this -1 (see the
			// server-full note above). Double rollback over-decremented the counter.
			return -1;
		}
	}
	else
	{
		// createAI() failed
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAI(): WARNING - createAI() failed for " @ %newName);
		// review #5: no rollback here - AI::helper rolls back this -1 (see above).
		return -1;
	}
}
