//============================================================================
// ai_safeguards.cs — split from Ai.cs (scatter split, ai.cs peel)
// Extracted 2026-07-18 at commit 320698f. Mechanical text move, no behavior change.
// Safeguards + shared machinery: clientId pool guards (2048 server slot / 2049-2175 shared pool), registry, graveyard, allocators (SpawnAIGetClientId/createAI), reconciliation, ghost cleanup, onDroneKilled, watchdog, telemetry, directives, bot groups. LOAD-BEARING: guards exist against real player-hijack incidents.
// exec'd by Ai.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
//
// AI support functions.
//

//
// This function creates an AI player using the supplied group of markers 
//    for locations.  The first marker in the group gives the starting location 
//    of the the AI, and the remaining markers specify the path to follow.  
//
// Example call:  
// 
//    createAI( guardNumberOne, "MissionGroup\\Teams\\team0\\guardPath", larmor );
//

$Stance::Array[1] = "Normal";
$Stance::Array[2] = "Offensive";
$Stance::Array[3] = "Defensive";
$Stance::Array[4] = "Glass Cannon";
$Stance::Array[5] = "MageBane";
$Stance::Array[6] = "BladeBane";

//globals
//--------
// path type
// 0 = circular
// 1 = oneWay
// 2 = twoWay
$AI::defaultPathType = 2; //run twoWay paths

//armor types
//light = larmor
//medium = marmor
//heavy = harmor

$AIattackMode = 1;

// Debug output control flags
// Set to 1 to enable debug output, 0 to disable (reduces server load and log spam)
$AI_DEBUG_ENABLED = 0;        // Controls [INERT DEBUG], [SPAWN FLOW], [AI DEBUG] messages
$AI_SPAWN_DEBUG = 0;          // Controls [SPAWN FLOW] messages specifically
$AI_PERIODIC_DEBUG = 0;       // Controls [INERT DEBUG] AI::Periodic messages
$LOOTBAG_DEBUG = 0;           // Controls [LOOTBAG AGGREGATE], [LOOT DEBUG] messages
$ONKILLED_DEBUG = 0;          // Controls [ONKILLED DEBUG] messages in Player::onKilled
$Debug::SafeGuards = 0;       // Controls [SAFEGUARD] player protection logging

// Granular Debug Flags (Turn off to reduce spam)
$TOWNBOT_RACE_DEBUG = 0;      // Controls [TOWNBOT RACE DEBUG] messages
$TOWNBOT_SKIN_DEBUG = 0;      // Controls [FINAL FIX] messages
$MISSION_CLEANUP_DEBUG = 0;   // Controls [DEBUG] addToSetMissionCleanup messages
$GETBOTID_DEBUG = 0;          // Controls [GETBOTIDLIST DEBUG] messages
$SPAWNLOOP_DEBUG = 0;         // Controls [SPAWN DEBUG] messages
$BOT_TEAM_DEBUG = 0;          // Controls [BOT TEAM DEBUG] messages
$TEAM_ENFORCE_DEBUG = 0;      // Controls [TEAM ENFORCE] messages
$BOT_TRACK_DEBUG = 0;         // Controls [BOT TRACK] messages
$BOT_REGISTRY_DEBUG = 0;      // Controls [BOT REGISTRY] messages
$SPAWN_TRANSACTION_DEBUG = 0; // Controls [SPAWN TRANSACTION] messages
$BOT_SHELL_DEBUG = 0;         // Controls [BOT SHELL DEBUG] messages
$SPAWN_COUNTER_DEBUG = 0;     // Controls [SPAWN COUNTER] messages
$BOT_CLEANUP_DEBUG = 0;       // Controls [BOT CLEANUP] messages
$TOWNBOT_ARMOR_DEBUG = 0;     // Controls [TOWNBOT ARMOR DEBUG] messages
$RECONCILE_DEBUG = 0;         // Controls [RECONCILE] messages

// Engine BaseRep ID range (Tribes source: PlayerManager::findBaseRep uses 128 slots).
// Real players and AI share this same pool.
$BaseRepClientIdMin = 2048;
$BaseRepClientIdMax = 2175;

// Empty-zone check tuning (performance/safety balance)
// $ZoneEmptyCheckInterval: how often PeriodicEmptyZoneCheck runs
// $ZoneEmptyAuditInterval: how often stable empty zones are force-audited
// $ZoneEmptyDespawnRetryInterval: minimum delay before retrying despawn on stable empty zones
$ZoneEmptyCheckInterval = 30;
$ZoneEmptyAuditInterval = 180;
$ZoneEmptyDespawnRetryInterval = 120;


// Bot tracking counters
$TotalActiveBots = 0;      // Total count of all active bots (enemy + town)
$ActiveEnemyBots = 0;         // Count of active enemy bots (Player objects with SpawnBotInfo)
$ActiveTownBots = 0;          // Count of active town bots (Drones with BotInfoAiName, no SpawnBotInfo)

// ============================================================================
// SPAWN TELEMETRY COUNTERS
// Track spawn attempts, successes, and failures for diagnostics
// ============================================================================
$Telemetry_SpawnAttempts = 0;        // Total spawn attempts
$Telemetry_SpawnSuccess = 0;         // Successful spawns
$Telemetry_SpawnFailed = 0;          // Failed spawns
$Telemetry_SpawnFailedTownBot = 0;   // Failed due to town bot conflict
$Telemetry_SpawnFailedClientId = 0;  // Failed due to client ID issues
$Telemetry_SpawnFailedZoneEmpty = 0; // Failed due to zone becoming empty
$Telemetry_SpawnFailedOther = 0;     // Failed for other reasons
$Telemetry_DeathsProcessed = 0;      // Bot deaths processed
$Telemetry_AINumbersFreed = 0;       // AI numbers successfully freed
$Telemetry_AINumberOrphans = 0;      // AI numbers that couldn't be freed
$Telemetry_NumAI_Inc = 0;            // $numAI increment count
$Telemetry_NumAI_Dec = 0;            // $numAI decrement count

function Telemetry_RecordSpawnAttempt() { $Telemetry_SpawnAttempts++; }
function Telemetry_RecordSpawnSuccess() { $Telemetry_SpawnSuccess++; }
function Telemetry_RecordSpawnFailed(%reason)
{
	$Telemetry_SpawnFailed++;
	if(%reason == "townbot")
		$Telemetry_SpawnFailedTownBot++;
	else if(%reason == "clientid")
		$Telemetry_SpawnFailedClientId++;
	else if(%reason == "zoneempty")
		$Telemetry_SpawnFailedZoneEmpty++;
	else
		$Telemetry_SpawnFailedOther++;
	
	// CRITICAL: Successfully recording a failure means the spawn attempt (which incremented $numAI) 
	// did not result in a live bot. We MUST decrement $numAI to balance the count.
	if($numAI > 0)
	{
		$numAI--;
		$Telemetry_NumAI_Dec++;
	}
}
function Telemetry_RecordDeath() { $Telemetry_DeathsProcessed++; }
function Telemetry_RecordAINumberFreed() { $Telemetry_AINumbersFreed++; }
function Telemetry_Reset()
{
	$Telemetry_SpawnAttempts = 0;
	$Telemetry_SpawnSuccess = 0;
	$Telemetry_SpawnFailed = 0;
	$Telemetry_SpawnFailedTownBot = 0;
	$Telemetry_SpawnFailedClientId = 0;
	$Telemetry_SpawnFailedOther = 0;
	$Telemetry_DeathsProcessed = 0;
	$Telemetry_AINumbersFreed = 0;
	$Telemetry_AINumberOrphans = 0;
}

// ============================================================================
// WATCHDOG SYSTEM - Detects infinite loops and server freezes
// Logs current function to file every 5 seconds. On freeze, check config/watchdog.log
// ============================================================================
$Watchdog_Enabled = true;            // Master switch for external watchdog heartbeat
$Watchdog_CurrentFunction = "";     // Currently executing function
$Watchdog_LoopCounter = 0;          // Current loop iteration
$Watchdog_MaxIterations = 500;      // Max iterations before breaking (safety limit)
$Watchdog_HeartbeatTick = 0;        // Incremented every heartbeat so file contents change too

// Heartbeat - writes to file every 5 seconds
// If server freezes, watchdog.log shows what was running
function Watchdog_Heartbeat()
{
	if(!$Watchdog_Enabled) return;
	
	%status = getSimTime() @ " | Func: " @ $Watchdog_CurrentFunction @ " | Loop: " @ $Watchdog_LoopCounter;
	%status = %status @ " | Bots: " @ $ActiveEnemyBots @ "/" @ $numAI;
	$Watchdog_HeartbeatTick++;
	$Watchdog_LastHeartbeat = getSimTime();
	
	// Write to both console and file
	echo("[WATCHDOG] " @ %status);
	
	// Write to dedicated file in temp directory (same location as other game exports)
	// Overwrites each time - last state before freeze is captured
	export("$Watchdog_*", "temp\\watchdog_state.cs", false);
	
	schedule("Watchdog_Heartbeat();", 5);
}

// Call at start of high-risk functions
function Watchdog_Enter(%funcName)
{
	$Watchdog_CurrentFunction = %funcName;
	$Watchdog_LoopCounter = 0;
	$Watchdog_EntryTime = getSimTime();
}

// Call at end of high-risk functions
function Watchdog_Exit()
{
	$Watchdog_CurrentFunction = "";
	$Watchdog_LoopCounter = 0;
}

// Call inside loops - returns true if should break (exceeded limit)
function Watchdog_LoopCheck(%context)
{
	$Watchdog_LoopCounter++;
	if($Watchdog_LoopCounter > $Watchdog_MaxIterations)
	{
		echo("WATCHDOG ALERT: Loop exceeded " @ $Watchdog_MaxIterations @ " iterations in " @ $Watchdog_CurrentFunction @ " (" @ %context @ ")! Breaking to prevent freeze.");
		return true;
	}
	return false;
}

// CRITICAL: Watchdog is started by calling StartWatchdog() from Server.cs
// This ensures reliable initialization after all scripts are loaded
function StartWatchdog()
{
	if(!$Watchdog_Enabled) return;
	
	if($Watchdog_Started != "true")
	{
		$Watchdog_Started = "true";
		schedule("Watchdog_Heartbeat();", 10);
		echo("[WATCHDOG] Started watchdog heartbeat (every 5 seconds, output to temp\\watchdog_state.cs)");
	}
}

// ============================================================================
// PERIODIC AI NUMBER RECONCILIATION
// Runs every 60 seconds (1 minute) to clean up orphaned AI numbers from $aiNumTable
// ============================================================================
$AINumberReconciliationEnabled = true;  // Set to false to disable

function StartAINumberReconciliation()
{
	if($AINumberReconciliationEnabled)
	{
		schedule("PeriodicAINumberReconciliation();", 60);  // 1 minute
		echo("[AI RECONCILIATION] Started periodic AI number reconciliation (every 60 seconds)");
	}
}

function PeriodicAINumberReconciliation()
{
	if(!$AINumberReconciliationEnabled)
		return;
	
	%freedCount = 0;
	%checkedCount = 0;
	
	// Get list of live bot names
	%liveBotNames = "";
	%botList = GetBotIdList();
	if(%botList != "")
	{
		for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
		{
			%botName = fetchData(%botId, "BotInfoAiName");
			if(%botName != "" && %botName != -1 && %botName != "0")
				%liveBotNames = %liveBotNames @ %botName @ " ";
		}
	}
	
	// Scan $aiNumTable for reserved numbers
	// review #8: scan the FULL 0..5000 range getAInumber allocates over (was 0..500,
	// so any leaked number above 500 was never reclaimed).
	for(%n = 0; %n <= 5000; %n++)
	{
		if($aiNumTable[%n] != "" && $aiNumTable[%n] != -1)
		{
			%checkedCount++;

			// review #8: GRACE WINDOW. Skip numbers reserved within the last 10s - a
			// bot reserves its number (setAInumber) up to ~3.5s before its
			// BotInfoAiName is stored, during which it contributes an empty name to
			// %liveBotNames and would be wrongly judged orphaned and freed (then
			// re-issued to the next spawn -> name collision). getSimTime is SECONDS.
			%reservedAt = $aiNumReservedTime[%n];
			if(%reservedAt != "" && %reservedAt != -1 && (getSimTime() - %reservedAt) < 10)
				continue;

			// Check if any live bot uses this number
			%isOrphaned = true;
			for(%k = 0; (%liveName = GetWord(%liveBotNames, %k)) != -1; %k++)
			{
				if($tmpbotn[%liveName] == %n)
				{
					%isOrphaned = false;
					break;
				}
			}

			if(%isOrphaned)
			{
				$aiNumTable[%n] = "";
				$aiNumReservedTime[%n] = "";
				%freedCount++;
			}
		}
	}
	
	if(%freedCount > 0)
		echo("[AI RECONCILIATION] Freed " @ %freedCount @ " orphaned AI numbers (checked " @ %checkedCount @ ")");
	
	// Schedule next run
	schedule("PeriodicAINumberReconciliation();", 60);  // 1 minute
}

// ============================================================================
// GHOST BOT DETECTION AND CLEANUP
// Runs every 30 seconds to detect and clean up ghost bots (bots with missing BotInfoAiName)
// Uses double-scan approach: first scan flags suspects, second scan cleans up confirmed ghosts
// ============================================================================
$GhostBotCleanupEnabled = true;  // Set to false to disable

function StartGhostBotCleanup()
{
	if($GhostBotCleanupEnabled)
	{
		schedule("PeriodicGhostBotScan();", 30);  // 30 seconds
		echo("[GHOST BOT] Started periodic ghost bot detection (every 30 seconds)");
	}
}

function PeriodicGhostBotScan()
{
	if(!$GhostBotCleanupEnabled)
		return;
	
	%ghostCount = 0;
	%cleanedCount = 0;
	%newSuspects = 0;
	
	// Get list of all bot objects from BotGroup
	if(!isObject("BotGroup"))
	{
		schedule("PeriodicGhostBotScan();", 30);
		return;
	}
	
	%group = nameToID("BotGroup");
	%count = Group::objectCount(%group);
	
	for(%i = %count - 1; %i >= 0; %i--)
	{
		%obj = Group::getObject(%group, %i);
		if(!isObject(%obj)) continue;
		if(getObjectType(%obj) != "Player") continue;
		
		%clientId = Player::getClient(%obj);
		if(%clientId == "" || %clientId == -1) continue;
		
		// Check if this bot has valid BotInfoAiName
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		%displayName = Client::getName(%clientId);
		
		// Skip if it has valid identifying data
		if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			continue;
		
		// Skip town bots (they don't use BotInfoAiName the same way)
		if(isTownBot(%clientId))
			continue;
		
		// This is a potential ghost bot (enemy bot without BotInfoAiName)
		%ghostCount++;
		
		// Check if it was already flagged in a previous scan
		// review #27: bind the suspect flag to the specific Player OBJECT, not just the
		// clientId. The id pool (2049-2175) is recycled, so a flagged ghost's id can be
		// reused by a brand-new bot still inside its ~1s registration window (no
		// BotInfoAiName yet). Keyed only by clientId, the next scan would read the OLD
		// flag's >25s timestamp and DELETE the legitimately-spawning new bot.
		if($GhostBotSuspect[%clientId] != "" && $GhostBotSuspectObj[%clientId] == %obj)
		{
			%timeFlagged = $GhostBotSuspect[%clientId];
			%currentTime = getSimTime();
			%elapsed = %currentTime - %timeFlagged;

			// If flagged for more than 25 seconds (allows for normal 3s spawn delay + buffer)
			if(%elapsed > 25)
			{
				// CONFIRMED GHOST - clean it up
				echo("[GHOST BOT] Cleaning up confirmed ghost bot: clientId=" @ %clientId @ ", displayName='" @ %displayName @ "', SpawnBotInfo='" @ %spawnBotInfo @ "', flagged " @ %elapsed @ "s ago");

				// Mark as no-drop, no-exp to prevent side effects
				storeData(%clientId, "noDropLootbagFlag", True);
				storeData(%clientId, "noExperienceFlag", True);

				// Clean up data
				ClearAllBotData(%clientId, false);

				// Try to delete the player object
				if(isObject(%obj))
					deleteObject(%obj);

				// Clear the suspect flag
				$GhostBotSuspect[%clientId] = "";
				$GhostBotSuspectObj[%clientId] = "";
				%cleanedCount++;
			}
		}
		else
		{
			// NEW SUSPECT (or the id was recycled onto a DIFFERENT object since we last
			// flagged it) - (re)flag with the current time and remember WHICH object.
			$GhostBotSuspect[%clientId] = getSimTime();
			$GhostBotSuspectObj[%clientId] = %obj;
			%newSuspects++;

			if($AI_DEBUG_ENABLED)
				echo("[GHOST BOT] Flagged new suspect: clientId=" @ %clientId @ ", displayName='" @ %displayName @ "'");
		}
	}
	
	// Clear flags for bots that are no longer suspects (they got proper data or were cleaned up elsewhere)
	// This prevents stale flags from accumulating
	for(%checkId = $BaseRepClientIdMin; %checkId <= $BaseRepClientIdMax; %checkId++)
	{
		if($GhostBotSuspect[%checkId] != "")
		{
			// Check if this bot still exists and is still a ghost
			%stillExists = false;
			%playerObj = Client::getOwnedObject(%checkId);
			if(%playerObj != "" && %playerObj != -1 && isObject(%playerObj))
			{
				%botInfoAiName = fetchData(%checkId, "BotInfoAiName");
				if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
				{
					// Skip town bots
					if(!isTownBot(%checkId))
						%stillExists = true;
				}
			}
			
			if(!%stillExists)
			{
				// Bot either got proper data or was cleaned up - clear the flag
				$GhostBotSuspect[%checkId] = "";
				$GhostBotSuspectObj[%checkId] = "";	// review #27: clear the companion object binding too
			}
		}
	}
	
	if(%cleanedCount > 0 || (%newSuspects > 0 && $AI_DEBUG_ENABLED))
		echo("[GHOST BOT] Scan complete: " @ %ghostCount @ " ghosts found, " @ %newSuspects @ " newly flagged, " @ %cleanedCount @ " cleaned up");
	
	// Schedule next scan
	schedule("PeriodicGhostBotScan();", 30);
}

// ============================================================================
// CENTRALIZED BOT REGISTRY SYSTEM
// This is the authoritative source for bot tracking and spawn counter management
// ============================================================================
// $BotRegistry[clientId] = spawnPointId     // Maps bot clientId to its spawn point
// $BotRegistry[clientId, "team"] = team     // Stores expected team for enforcement
// $BotRegistry[clientId, "name"] = aiName   // Stores bot AI name for lookup
// $BotRegistryList = ""                     // Space-separated list of all enemy bot clientIds
$BotRegistryList = "";

// ============================================================================
// CRITICAL FIX #1: GRAVEYARD SYSTEM
// ============================================================================
// Prevents race conditions in death flow by atomically marking bots as dead
// When a bot dies, it's immediately renamed to "DEAD_timestamp" and moved to GraveyardSet
// Only deleted after delay, ensuring SpawnAIGetClientId never reuses a dying bot's ID
// ============================================================================
$GraveyardSet = "";  // SimSet name for dead bots (created on first use)
$GraveyardInitialized = false;

// InitializeGraveyard: Creates the GraveyardSet SimSet if it doesn't exist
function InitializeGraveyard()
{
	if($GraveyardInitialized == true)
		return;
	
	// Create SimSet for graveyard if it doesn't exist
	%graveyardId = nameToID("GraveyardSet");
	if(%graveyardId == -1)
	{
		%graveyardId = newObject("GraveyardSet", SimSet, true);
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD] Created GraveyardSet SimSet");
	}
	$GraveyardSet = "GraveyardSet";
	$GraveyardInitialized = true;
}

// IsInGraveyard: Checks if a bot name or clientId is in the graveyard
function IsInGraveyard(%botNameOrId)
{
	if(%botNameOrId == "" || %botNameOrId == -1)
		return false;
	
	// Check if name starts with "DEAD_"
	if(String::findSubStr(%botNameOrId, "DEAD_") == 0)
		return true;
	
	// Check if clientId has a graveyard marker
	%graveyardMarker = $GraveyardClientId[%botNameOrId];
	if(%graveyardMarker == "true" || %graveyardMarker == "1")
		return true;
	
	return false;
}

// AddToGraveyard: Atomically marks a bot as dead and moves it to graveyard
function AddToGraveyard(%aiName, %clientId)
{
	if(%aiName == "" || %aiName == -1 || %clientId == "" || %clientId == -1)
		return;
	
	InitializeGraveyard();
	if($SpellCastToken[%clientId] == "" || $SpellCastToken[%clientId] == -1)
		$SpellCastToken[%clientId] = 0;
	$SpellCastToken[%clientId]++;
	$PowerCloudCacheToken[%clientId] = "";
	
	// Create dead name with timestamp
	%deadName = "DEAD_" @ getSimTime() @ "_" @ %aiName;
	%timestamp = getSimTime();
	
	// Store mapping from original name to dead name
	$GraveyardDeadName[%aiName] = %deadName;
	$GraveyardOriginalName[%deadName] = %aiName;
	$GraveyardClientId[%clientId] = "true";
	$GraveyardClientId[%aiName] = %clientId;  // Also map by name for lookup
	// CRITICAL: Store timestamp for automatic cleanup
	$GraveyardTimestamp[%clientId] = %timestamp;
	$GraveyardTimestamp[%aiName] = %timestamp;
	
	// Try to rename the AI in the engine (may fail for Player objects, but we still mark it)
	%escapedOriginalName = String::replace(%aiName, "\"", "\\\"");
	%escapedDeadName = String::replace(%deadName, "\"", "\\\"");
	
	// Note: AI::rename may not work for Player objects, but marking in our system is sufficient
	// The important part is that SpawnAIGetClientId will check IsInGraveyard() before reusing IDs
	
	if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD] AddToGraveyard: Marked bot " @ %aiName @ " (clientId=" @ %clientId @ ") as dead -> " @ %deadName @ " @ " @ %timestamp);
	
	// Schedule periodic cleanup check (runs every 30 seconds)
	// Note: CleanupOldGraveyardEntries() now always reschedules itself, so we only need to start it once
	if($GraveyardCleanupScheduled == "")
	{
		$GraveyardCleanupScheduled = "true";
		schedule("CleanupOldGraveyardEntries();", 30);
	}
}

// RemoveFromGraveyard: Cleans up graveyard entry after deletion
function RemoveFromGraveyard(%aiName, %clientId)
{
	if(%aiName == "" || %aiName == -1)
		return;
	
	// Calculate age before clearing timestamp
	%age = -1;
	if(%clientId != "" && %clientId != -1)
	{
		%timestamp = $GraveyardTimestamp[%clientId];
		if(%timestamp != "" && %timestamp != -1)
		{
			%age = getSimTime() - %timestamp;
		}
	}
	
	%deadName = $GraveyardDeadName[%aiName];
	if(%deadName != "" && %deadName != -1)
	{
		$GraveyardDeadName[%aiName] = "";
		$GraveyardOriginalName[%deadName] = "";
	}
	
	if(%clientId != "" && %clientId != -1)
	{
		$GraveyardClientId[%clientId] = "";
		$GraveyardClientId[%aiName] = "";
		$GraveyardTimestamp[%clientId] = "";
		$GraveyardTimestamp[%aiName] = "";
	}
	
	// Log removal with age if available
	// BUGFIX: braces added - the else previously bound to the inner debug-flag if
	// (dangling else), so the no-age message could never print
	if(%age >= 0)
	{
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD] RemoveFromGraveyard: Removed bot " @ %aiName @ " (clientId=" @ %clientId @ ") after " @ %age @ "s");
	}
	else
	{
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD] RemoveFromGraveyard: Removed bot " @ %aiName @ " (clientId=" @ %clientId @ ")");
	}
}

// CleanupOldGraveyardEntries: Automatically removes graveyard entries older than 10 seconds
function CleanupOldGraveyardEntries()
{
	%currentTime = getSimTime();
	%cleanedCount = 0;
	%maxAge = 10; // 10 seconds
	%totalEntries = 0;
	%oldestAge = 0;
	
	// Iterate through all client IDs that might be in graveyard
	// Range: 2048 is the SERVER's reserved slot (PlayerManager::reset never puts index 0
	// on the free list), so allocatable ids are Min+1..Max; the old hardcoded 2200 upper
	// bound wasted 25 impossible ids per pass
	for(%id = $BaseRepClientIdMin + 1; %id <= $BaseRepClientIdMax; %id++)
	{
		if($GraveyardClientId[%id] == "true" || $GraveyardClientId[%id] == "1")
		{
			%totalEntries++;
			%timestamp = $GraveyardTimestamp[%id];
			if(%timestamp != "" && %timestamp != -1)
			{
				%age = %currentTime - %timestamp;
				if(%age > %oldestAge)
					%oldestAge = %age;
				
				if(%age > %maxAge)
				{
					// Auto-cleanup old entry - need to find the AI name
					%aiName = "";
					
					// METHOD 1: Try to get name from BotInfoAiName (most reliable)
					%aiName = fetchData(%id, "BotInfoAiName");
					if(%aiName == "" || %aiName == -1 || %aiName == "0")
						%aiName = "";
					
					// METHOD 2: Try to get name from BotRegistry
					if(%aiName == "")
					{
						%aiName = $BotRegistry[%id, "name"];
						if(%aiName == "" || %aiName == -1)
							%aiName = "";
					}
					
					// METHOD 3: Iterate through BotRegistryList to find this clientId
					if(%aiName == "")
					{
						for(%i = 0; (%checkId = GetWord($BotRegistryList, %i)) != -1; %i++)
						{
							if(%checkId == %id)
							{
								%aiName = $BotRegistry[%checkId, "name"];
								if(%aiName != "" && %aiName != -1)
									break;
							}
						}
					}
					
					// METHOD 4: Iterate through all clients AND bots to check reverse mapping
					// This handles cases where bot isn't in registry but graveyard entry exists
					// CRITICAL: Use BaseRep::getFirst/getNext which includes AI bots
					// (Client::getFirst only returns real players, not AI bots)
					if(%aiName == "")
					{
						for(%checkId = BaseRep::getFirst(); %checkId != -1; %checkId = BaseRep::getNext(%checkId))
						{
							// Check if this clientId has a name that maps to our target ID
							%checkName = Client::getName(%checkId);
							if(%checkName != "" && %checkName != -1)
							{
								if($GraveyardClientId[%checkName] == %id)
								{
									%aiName = %checkName;
									break;
								}
							}
							
							// Also check BotInfoAiName for this client
							%checkBotName = fetchData(%checkId, "BotInfoAiName");
							if(%checkBotName != "" && %checkBotName != -1)
							{
								if($GraveyardClientId[%checkBotName] == %id)
								{
									%aiName = %checkBotName;
									break;
								}
							}
						}
					}
					
					// If we found the name, remove it properly
					if(%aiName != "")
					{
						RemoveFromGraveyard(%aiName, %id);
						%cleanedCount++;
						if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD CLEANUP] Auto-cleaned old entry: clientId=" @ %id @ ", name=" @ %aiName @ ", age=" @ %age @ "s");
					}
					else
					{
						// No name found - clean up all possible graveyard entries for this clientId
						// This prevents memory leaks from orphaned entries
						$GraveyardClientId[%id] = "";
						$GraveyardTimestamp[%id] = "";
						
						// Also try to find and clear any dead name entries that might reference this clientId
						// Iterate through possible dead names (format: "DEAD_timestamp_name")
						// This is a best-effort cleanup since we don't have the name
						%cleanedCount++;
						if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD CLEANUP] Auto-cleaned old entry (no name found): clientId=" @ %id @ ", age=" @ %age @ "s");
					}
				}
			}
			else
			{
				// Entry has no timestamp - clean it up as orphaned
				// Try to find the name first (same methods as above)
				%aiName = "";
				%aiName = fetchData(%id, "BotInfoAiName");
				if(%aiName == "" || %aiName == -1 || %aiName == "0")
				{
					%aiName = $BotRegistry[%id, "name"];
					if(%aiName == "" || %aiName == -1)
						%aiName = "";
				}
				
				// If name found, use proper cleanup
				if(%aiName != "")
				{
					RemoveFromGraveyard(%aiName, %id);
					%cleanedCount++;
					if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD CLEANUP] Auto-cleaned orphaned entry (no timestamp): clientId=" @ %id @ ", name=" @ %aiName);
				}
				else
				{
					// No name found - clean up what we can
					$GraveyardClientId[%id] = "";
					$GraveyardTimestamp[%id] = "";
					%cleanedCount++;
					if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD CLEANUP] Auto-cleaned orphaned entry (no timestamp, no name): clientId=" @ %id);
				}
			}
		}
	}
	
	if(%cleanedCount > 0)
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD CLEANUP] Cleaned " @ %cleanedCount @ " old entries (total was " @ %totalEntries @ ", oldest age was " @ %oldestAge @ "s)");
	
	// Warn if graveyard has too many entries
	if(%totalEntries > 50)
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD WARNING] Graveyard has " @ %totalEntries @ " entries (threshold: 50) - possible accumulation issue");
	
	// Always schedule next cleanup (runs every 30 seconds regardless of entry count)
	// This ensures periodic cleanup even if entries are added between cleanup cycles
	schedule("CleanupOldGraveyardEntries();", 30);
}

// ============================================================================
// BOT REGISTRY FUNCTIONS
// ============================================================================

// Register a bot in the centralized registry
function RegisterBot(%clientId, %spawnPointId, %team, %aiName)
{
	if(%clientId == "" || %clientId == -1)
		return;
	
	// Store spawn point mapping (authoritative source for counter management)
	$BotRegistry[%clientId] = %spawnPointId;
	$BotRegistry[%clientId, "team"] = %team;
	$BotRegistry[%clientId, "name"] = %aiName;

	// O(1) name->clientId map: fast path for AI::getClientIdFromName(),
	// which otherwise scans every BaseRep per call (hot in AI::Periodic)
	if(%aiName != "" && %aiName != -1)
		$BotNameToClient[%aiName] = %clientId;
	
	// Add to registry list if not already present
	%found = false;
	for(%i = 0; GetWord($BotRegistryList, %i) != -1; %i++)
	{
		if(GetWord($BotRegistryList, %i) == %clientId)
		{
			%found = true;
			break;
		}
	}
	
	if(!%found)
	{
		if($BotRegistryList == "")
			$BotRegistryList = %clientId;
		else
			$BotRegistryList = $BotRegistryList @ " " @ %clientId;
	}
	
	// PRIORITY 2: Set $BotType cache for O(1) bot type detection
	// This eliminates 300+ array lookups per spawn in GetClientDataType()
	$BotType[%clientId] = "enemy";
	
	if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Registered bot: clientId=" @ %clientId @ ", spawnPoint=" @ %spawnPointId @ ", team=" @ %team @ ", name=" @ %aiName);
}

// Unregister a bot from the centralized registry
// %excludeObject: Optional player object ID to skip when cleaning orphans (e.g., the dying bot in Player::onKilled)
function UnregisterBot(%clientId, %excludeObject)
{
	if(%clientId == "" || %clientId == -1)
		return;
	
	// NOTE: $BotType is cleared by ClearAllBotData() at END of function
	
	%spawnPointId = $BotRegistry[%clientId];
	%aiName = $BotRegistry[%clientId, "name"];
	
	// Also try to get name from storeData if not in registry
	if(%aiName == "" || %aiName == -1)
		%aiName = fetchData(%clientId, "BotInfoAiName");
	
	if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY DEBUG] UnregisterBot called: clientId=" @ %clientId @ ", spawnPoint=" @ %spawnPointId @ ", name=" @ %aiName);
	
	// Clear the O(1) name->clientId map entry, but only if it still points at
	// this clientId - a newer bot may have already reused the name
	if(%aiName != "" && %aiName != -1 && $BotNameToClient[%aiName] == %clientId)
		$BotNameToClient[%aiName] = "";

	// Clear registry entries
	$BotRegistry[%clientId] = "";
	$BotRegistry[%clientId, "team"] = "";
	$BotRegistry[%clientId, "name"] = "";
	
	// PRIORITY 1: Use unified ClearAllBotData() for all data clearing
	ClearAllBotData(%clientId, false);
	
	// Remove from registry list
	%newList = "";
	%foundInList = false;
	for(%i = 0; GetWord($BotRegistryList, %i) != -1; %i++)
	{
		%id = GetWord($BotRegistryList, %i);
		if(%id != %clientId)
		{
			if(%newList == "")
				%newList = %id;
			else
				%newList = %newList @ " " @ %id;
		}
		else
		{
			%foundInList = true;
		}
	}
	$BotRegistryList = %newList;
	
	// CRITICAL: Also delete any orphaned player objects for this client ID
	// This handles the case where Player::getClient(obj) returns clientId but Client::getOwnedObject(clientId) returns -1
	// Scan BotGroup
	if(isObject("BotGroup"))
	{
		// Death-path optimization: defer BotGroup scan out of Player::onKilled callback
		// to avoid synchronous full-group scans during bot-death bursts.
		if(%excludeObject != "" && %excludeObject != -1)
		{
			ScheduleDeferredGroupOrphanScan(%clientId, %excludeObject, "BotGroup");
		}
		else
		{
			%group = nameToID("BotGroup");
			%count = Group::objectCount(%group);
			for(%i = %count - 1; %i >= 0; %i--)
			{
				%obj = Group::getObject(%group, %i);
				if(!isObject(%obj)) continue;
				if(getObjectType(%obj) != "Player") continue;

				%objClientId = Player::getClient(%obj);
				if(%objClientId == %clientId)
				{
					// CRITICAL: Skip if this is the dying object (passed via %excludeObject)
					// This prevents use-after-free crash when called from Player::onKilled
					if(%obj == %excludeObject)
					{
						if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Skipping dying object " @ %obj @ " in BotGroup for clientId=" @ %clientId @ " (excluded)");
						continue;
					}

					if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Found orphaned object " @ %obj @ " in BotGroup for clientId=" @ %clientId @ " - scheduling deletion");
					// Schedule deletion to prevent crash during death processing
					schedule("if(isObject(" @ %obj @ ")) deleteObject(" @ %obj @ ");", 0.5);
				}
			}
		}
	}
	
	// CRITICAL: Also scan MissionCleanup since orphaned objects end up there
	if(isObject("MissionCleanup"))
	{
		// Death-path optimization: defer MissionCleanup scan out of Player::onKilled callback
		// to avoid synchronous full-group scans during bot-death bursts.
		if(%excludeObject != "" && %excludeObject != -1)
		{
			ScheduleDeferredGroupOrphanScan(%clientId, %excludeObject, "MissionCleanup");
		}
		else
		{
			%group = nameToID("MissionCleanup");
			%count = Group::objectCount(%group);
			for(%i = %count - 1; %i >= 0; %i--)
			{
				%obj = Group::getObject(%group, %i);
				if(!isObject(%obj)) continue;
				if(getObjectType(%obj) != "Player") continue;
				
				%objClientId = Player::getClient(%obj);
				if(%objClientId == %clientId)
				{
					// CRITICAL: Skip if this is the dying object (passed via %excludeObject)
					// This prevents use-after-free crash when called from Player::onKilled
					if(%obj == %excludeObject)
					{
						if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Skipping dying object " @ %obj @ " in MissionCleanup for clientId=" @ %clientId @ " (excluded)");
						continue;
					}
					
					if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Found orphaned object " @ %obj @ " in MissionCleanup for clientId=" @ %clientId @ " - scheduling deletion");
					// Schedule deletion to prevent crash during death processing
					schedule("if(isObject(" @ %obj @ ")) deleteObject(" @ %obj @ ");", 0.5);
				}
			}
		}
	}
	
	if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Unregistered bot: clientId=" @ %clientId @ ", spawnPoint=" @ %spawnPointId @ ", name=" @ %aiName @ ", wasInList=" @ %foundInList);
}

// Schedule deferred orphan scans (BotGroup or MissionCleanup) for death-path unregisters.
// This keeps Player::onKilled callback lean while preserving orphan cleanup behavior.
// CONSOLIDATED: was two copy-paste pairs (ScheduleDeferredBotGroupOrphanScan /
// ScheduleDeferredMissionCleanupOrphanScan) differing only in the group name -
// merged so future fixes can't land in one copy and miss the other.
function ScheduleDeferredGroupOrphanScan(%clientId, %excludeObject, %groupName)
{
	if(%clientId == "" || %clientId == -1)
		return;
	if(%groupName == "" || %groupName == -1)
		return;

	// Avoid stacking scans for the same client ID + group in the same short window.
	if($DeferredGroupScanScheduled[%groupName, %clientId] != "")
		return;

	$DeferredGroupScanScheduled[%groupName, %clientId] = true;
	// Token guards against clientId reuse between schedule and execution.
	%scanToken = %clientId @ "_" @ getSimTime() @ "_" @ floor(getRandom() * 1000000);
	$DeferredGroupScanToken[%groupName, %clientId] = %scanToken;

	%excludeObjectArg = %excludeObject;
	if(%excludeObjectArg == "" || %excludeObjectArg == -1)
		%excludeObjectArg = -1;

	schedule("DeferredGroupOrphanScan(" @ %clientId @ ", " @ %excludeObjectArg @ ", \"" @ %scanToken @ "\", \"" @ %groupName @ "\");", 0.25);
}

function DeferredGroupOrphanScan(%clientId, %excludeObject, %scanToken, %groupName)
{
	// Abort stale scheduled scans if the client ID has been reused/retokenized.
	if($DeferredGroupScanToken[%groupName, %clientId] != %scanToken)
		return;

	$DeferredGroupScanScheduled[%groupName, %clientId] = "";
	$DeferredGroupScanToken[%groupName, %clientId] = "";

	if(%clientId == "" || %clientId == -1)
		return;

	if(!isObject(%groupName))
		return;

	%ownedObj = Client::getOwnedObject(%clientId);
	// Hard guard: never run orphan deletion when this client ID currently owns a live player object.
	if(%ownedObj != "" && %ownedObj != -1 && isObject(%ownedObj))
	{
		if(!Player::isAiControlled(%ownedObj))
		{
			%ownedName = Client::getName(%clientId);
			if(%ownedName != "" && %ownedName != -1 && isFile("temp\\" @ %ownedName @ ".cs"))
			{
				if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Deferred " @ %groupName @ " cleanup cancelled: clientId " @ %clientId @ " now belongs to player '" @ %ownedName @ "'");
				return;
			}

			// Even without a save-file match, avoid touching active non-AI owned objects.
			if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Deferred " @ %groupName @ " cleanup cancelled: clientId " @ %clientId @ " has active non-AI owned object " @ %ownedObj);
			return;
		}
	}

	%group = nameToID(%groupName);
	%count = Group::objectCount(%group);
	for(%i = %count - 1; %i >= 0; %i--)
	{
		%obj = Group::getObject(%group, %i);
		if(!isObject(%obj)) continue;
		if(getObjectType(%obj) != "Player") continue;
		if(%obj == %excludeObject) continue;
		if(%ownedObj != "" && %ownedObj != -1 && %obj == %ownedObj) continue;

		%objClientId = Player::getClient(%obj);
		if(%objClientId != %clientId)
			continue;

		// Additional collision safety: don't touch non-AI player objects with character saves.
		if(!Player::isAiControlled(%obj))
		{
			%objName = Client::getName(%objClientId);
			if(%objName != "" && %objName != -1 && isFile("temp\\" @ %objName @ ".cs"))
			{
				if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Deferred " @ %groupName @ " cleanup skipped player object " @ %obj @ " for clientId=" @ %clientId @ " (" @ %objName @ ")");
				continue;
			}
		}

		if(!IsSafeToDeletePlayerObject(%obj, %clientId, "DeferredGroupOrphanScan"))
			continue;

		if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY] Deferred orphan cleanup: object " @ %obj @ " in " @ %groupName @ " for clientId=" @ %clientId);
		schedule("if(isObject(" @ %obj @ ")) deleteObject(" @ %obj @ ");", 0.5);
	}
}

// (ScheduleDeferredMissionCleanupOrphanScan / DeferredMissionCleanupOrphanScan
// removed - consolidated into the parameterized ScheduleDeferredGroupOrphanScan /
// DeferredGroupOrphanScan above, called with "MissionCleanup")

// =============================================================================
// PRIORITY 1: Unified bot data clearing function
// Consolidates cleanup from: UnregisterBot, ClearVariables, connectivity.cs, #deletebot
// Call this function to clear ALL bot data for a client ID
// =============================================================================
function ClearAllBotData(%clientId, %preserveBotInfoAiName)
{
	if(%clientId == "" || %clientId == -1)
		return;

	%clientDataType = GetClientDataType(%clientId);

	// CRITICAL SAFETY CHECK: Never clear data if this client is an active player
	// who has already loaded and spawned. This is a "Defense in Depth" measure.
	if(%clientDataType == "player" && fetchData(%clientId, "HasLoadedAndSpawned") == "true")
	{
		echo("WARNING: ClearAllBotData - Attempted to clear data for ACTIVE PLAYER " @ Client::getName(%clientId) @ " (ID: " @ %clientId @ "). ABORTING.");
		return;
	}

	if($SpellCastToken[%clientId] == "" || $SpellCastToken[%clientId] == -1)
		$SpellCastToken[%clientId] = 0;
	$SpellCastToken[%clientId]++;
	$PowerCloudCacheToken[%clientId] = "";

	// Performance: avoid repeated GetClientDataType/isFile resolution during bulk storeData clears.
	// Only enable override when this client is already resolved as a bot type.
	%useStoreTypeOverride = false;
	if(%clientDataType == "enemybot" || %clientDataType == "townbot")
	{
		BeginStoreDataClientTypeOverride(%clientId, %clientDataType);
		%useStoreTypeOverride = true;
	}

	// -------------------------------------------------------------------------
	// 2. Clear storeData fields (routes to appropriate array based on type)
	// -------------------------------------------------------------------------
	// Core bot identity
	if(!%preserveBotInfoAiName)
		storeData(%clientId, "BotInfoAiName", "");
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", "");
	storeData(%clientId, "HasLoadedAndSpawned", "");
	storeData(%clientId, "DeathProcessed", "");
	storeData(%clientId, "ExpDistributed", "");
	
	// Location and team
	storeData(%clientId, "zone", "");
	storeData(%clientId, "tmpzone", "");
	storeData(%clientId, "botTeam", "");
	
	// Belt items & Inventory
	storeData(%clientId, "QuestItems", "");
	storeData(%clientId, "KeyItems", "");
	storeData(%clientId, "Consumables", "");
	storeData(%clientId, "Armor", "");
	storeData(%clientId, "Accessories", "");
	storeData(%clientId, "Other", "");
	storeData(%clientId, "spawnStuff", "");
	storeData(%clientId, "BankStorage", "");
	storeData(%clientId, "StoredQuestItems", "");
	storeData(%clientId, "StoredKeyItems", "");
	storeData(%clientId, "StoredConsumables", "");
	storeData(%clientId, "StoredArmor", "");
	storeData(%clientId, "StoredAccessories", "");
	storeData(%clientId, "StoredOther", "");
	storeData(%clientId, "EquippedBeltArmor", "");
	storeData(%clientId, "EquippedBeltAccessories", "");
	storeData(%clientId, "DualWield_OffHandWeapon", "");
	
	// RPG Stats & Identity
	storeData(%clientId, "RemortStep", "");
	storeData(%clientId, "LVL", "");
	storeData(%clientId, "EXP", "");
	storeData(%clientId, "SPcredits", "");
	storeData(%clientId, "MyHouse", "");
	storeData(%clientId, "RankPoints", "");
	storeData(%clientId, "TournyRank", "");
	storeData(%clientId, "GROUP", "");
	storeData(%clientId, "CLASS", "");
	storeData(%clientId, "LCK", "");
	storeData(%clientId, "bounty", "");
	storeData(%clientId, "inArena", "");
	
	// Feature variables
	storeData(%clientId, "AscensionTalents", "");
	storeData(%clientId, "AutoSkill_Priority", "");
	storeData(%clientId, "AutoParty_Enabled", "");
	
	// Legacy / System variables
	storeData(%clientId, "defaultTalk", "");
	storeData(%clientId, "password", "");
	storeData(%clientId, "PlayerInfo", "");
	storeData(%clientId, "deathmsg", "");
	storeData(%clientId, "campRot", "");
	storeData(%clientId, "tmphp", "");
	storeData(%clientId, "tmpmana", "");
	storeData(%clientId, "tmpLastSaveVer", "");
	storeData(%clientId, "savedMountedWeapon", "");
	
	// Flags
	storeData(%clientId, "noExperienceFlag", "");
	storeData(%clientId, "noDropLootbagFlag", "");
	storeData(%clientId, "dumbAIflag", "");
	storeData(%clientId, "frozen", "");
	storeData(%clientId, "noBotSniff", "");
	
	// AI behavior
	storeData(%clientId, "SpellCastStep", "");
	storeData(%clientId, "LCKconsequence", "");
	storeData(%clientId, "AIattackMarker", "");
	storeData(%clientId, "AITarget", "");
	storeData(%clientId, "AILastDestination", "");
	storeData(%clientId, "AILastLoggedDist", "");
	storeData(%clientId, "AIMovementLoopRunning", "");
	storeData(%clientId, "botAttackMode", "");
	storeData(%clientId, "tmpbotdata", "");
	
	// Seal battle
	storeData(%clientId, "SealBattleBot", "");
	storeData(%clientId, "SealBattleTargetMaxHP", "");
	storeData(%clientId, "SealBattleTargetMaxMANA", "");
	
	// Misc
	storeData(%clientId, "ShovedByPlayer", "");
	storeData(%clientId, "OriginalLootString", "");
	storeData(%clientId, "Stance", "");
	
	// CRITICAL: Player visibility flags (from #hide command) - can make bots invisible if inherited
	storeData(%clientId, "invisible", "");
	storeData(%clientId, "blockHide", "");
	storeData(%clientId, "lastPos", "");
	
	// CRITICAL: Admin privileges - bots should NEVER have admin access
	// Guard: bots have no engine object at their clientId (only zombie NetConnections do),
	// so an unguarded dotted-field write here errored on every normal bot cleanup
	if(isObject(%clientId))
		%clientId.adminLevel = "";
	
	// Additional player-only flags that could cause issues if inherited
	storeData(%clientId, "ignoreGlobal", "");
	storeData(%clientId, "campPos", "");
	storeData(%clientId, "BANK", "");
	storeData(%clientId, "BANK_CHUNKS", "");
	storeData(%clientId, "BANK_FORMAT", "");
	storeData(%clientId, "BANK_LEGACY_BACKUP", "");
	storeData(%clientId, "BANK_NEEDS_FILE_BACKUP", "");
	storeData(%clientId, "isMimic", "");
	storeData(%clientId, "grouplist", "");
	storeData(%clientId, "partyOwned", "");
	storeData(%clientId, "partylist", "");
	storeData(%clientId, "BeltStorage", "");
	storeData(%clientId, "COINS", "");
	storeData(%clientId, "tmpname", "");
	
	// -------------------------------------------------------------------------
	// 3. Clear $EnemyBotData arrays directly (belt items + flags)
	// -------------------------------------------------------------------------
	if(!%preserveBotInfoAiName)
		$EnemyBotData[%clientId, "BotInfoAiName"] = "";
	$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
	$EnemyBotData[%clientId, "SpawnTime"] = "";
	$EnemyBotData[%clientId, "zone"] = "";
	$EnemyBotData[%clientId, "tmpzone"] = "";
	$EnemyBotData[%clientId, "SpawnOriginZoneID"] = "";
	$EnemyBotData[%clientId, "botTeam"] = "";
	$EnemyBotData[%clientId, "RemortStep"] = "";
	$EnemyBotData[%clientId, "QuestItems"] = "";
	$EnemyBotData[%clientId, "KeyItems"] = "";
	$EnemyBotData[%clientId, "Consumables"] = "";
	$EnemyBotData[%clientId, "Armor"] = "";
	$EnemyBotData[%clientId, "Accessories"] = "";
	$EnemyBotData[%clientId, "Other"] = "";
	$EnemyBotData[%clientId, "noExperienceFlag"] = "";
	$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
	$EnemyBotData[%clientId, "dumbAIflag"] = "";
	$EnemyBotData[%clientId, "frozen"] = "";
	$EnemyBotData[%clientId, "noBotSniff"] = "";
	$EnemyBotData[%clientId, "SpellCastStep"] = "";
	$EnemyBotData[%clientId, "LCKconsequence"] = "";
	$EnemyBotData[%clientId, "AIattackMarker"] = "";
	
	// -------------------------------------------------------------------------
	// 4. Clear $TownBotData arrays directly
	// -------------------------------------------------------------------------
	if(!%preserveBotInfoAiName)
		$TownBotData[%clientId, "BotInfoAiName"] = "";
	$TownBotData[%clientId, "SpawnBotInfo"] = "";
	$TownBotData[%clientId, "SpawnTime"] = "";
	$TownBotData[%clientId, "zone"] = "";
	$TownBotData[%clientId, "QuestItems"] = "";
	$TownBotData[%clientId, "KeyItems"] = "";
	$TownBotData[%clientId, "Consumables"] = "";
	$TownBotData[%clientId, "Armor"] = "";
	$TownBotData[%clientId, "Accessories"] = "";
	$TownBotData[%clientId, "Other"] = "";
	
	// -------------------------------------------------------------------------
	// 5. Clear $ClientData arrays directly (backwards compatibility)
	// -------------------------------------------------------------------------
	if(!%preserveBotInfoAiName)
		$ClientData[%clientId, "BotInfoAiName"] = "";
	// 5. CRITICAL: Explicitly clear legacy $ClientData array entries
	// -------------------------------------------------------------------------
	// This prevents GetDataFromArray() from falling back to player data
	// from a previous player who used this same clientId.
	// -------------------------------------------------------------------------
	$ClientData[%clientId, "BotInfoAiName"] = "";
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "SpawnTime"] = "";
	$ClientData[%clientId, "zone"] = "";
	$ClientData[%clientId, "AscensionTalents"] = "";
	$ClientData[%clientId, "AutoSkill_Priority"] = "";
	$ClientData[%clientId, "AutoParty_Enabled"] = "";
	$ClientData[%clientId, "MyHouse"] = "";
	$ClientData[%clientId, "RemortStep"] = "";
	$ClientData[%clientId, "LVL"] = "";
	$ClientData[%clientId, "EXP"] = "";
	$ClientData[%clientId, "SPcredits"] = "";
	$ClientData[%clientId, "RankPoints"] = "";
	$ClientData[%clientId, "TournyRank"] = "";
	$ClientData[%clientId, "GROUP"] = "";
	$ClientData[%clientId, "CLASS"] = "";
	$ClientData[%clientId, "LCK"] = "";
	$ClientData[%clientId, "bounty"] = "";
	$ClientData[%clientId, "inArena"] = "";
	$ClientData[%clientId, "QuestItems"] = "";
	$ClientData[%clientId, "KeyItems"] = "";
	$ClientData[%clientId, "Consumables"] = "";
	$ClientData[%clientId, "Armor"] = "";
	$ClientData[%clientId, "Accessories"] = "";
	$ClientData[%clientId, "Other"] = "";
	$ClientData[%clientId, "spawnStuff"] = "";
	$ClientData[%clientId, "BankStorage"] = "";
	$ClientData[%clientId, "StoredQuestItems"] = "";
	$ClientData[%clientId, "StoredKeyItems"] = "";
	$ClientData[%clientId, "StoredConsumables"] = "";
	$ClientData[%clientId, "StoredArmor"] = "";
	$ClientData[%clientId, "StoredAccessories"] = "";
	$ClientData[%clientId, "StoredOther"] = "";
	$ClientData[%clientId, "EquippedBeltArmor"] = "";
	$ClientData[%clientId, "EquippedBeltAccessories"] = "";
	$ClientData[%clientId, "DualWield_OffHandWeapon"] = "";
	$ClientData[%clientId, "defaultTalk"] = "";
	$ClientData[%clientId, "password"] = "";
	$ClientData[%clientId, "PlayerInfo"] = "";
	$ClientData[%clientId, "deathmsg"] = "";
	$ClientData[%clientId, "campRot"] = "";
	$ClientData[%clientId, "tmphp"] = "";
	$ClientData[%clientId, "tmpmana"] = "";
	$ClientData[%clientId, "tmpLastSaveVer"] = "";
	$ClientData[%clientId, "savedMountedWeapon"] = "";
	// CRITICAL: Player visibility and system flags - prevents inheritance issues
	$ClientData[%clientId, "invisible"] = "";
	$ClientData[%clientId, "blockHide"] = "";
	$ClientData[%clientId, "lastPos"] = "";
	$ClientData[%clientId, "ignoreGlobal"] = "";
	$ClientData[%clientId, "campPos"] = "";
	$ClientData[%clientId, "BANK"] = "";
	$ClientData[%clientId, "BANK_CHUNKS"] = "";
	$ClientData[%clientId, "BANK_FORMAT"] = "";
	$ClientData[%clientId, "BANK_LEGACY_BACKUP"] = "";
	$ClientData[%clientId, "BANK_NEEDS_FILE_BACKUP"] = "";
	$ClientData[%clientId, "isMimic"] = "";
	$ClientData[%clientId, "grouplist"] = "";
	$ClientData[%clientId, "partyOwned"] = "";
	$ClientData[%clientId, "partylist"] = "";
	$ClientData[%clientId, "BeltStorage"] = "";
	$ClientData[%clientId, "COINS"] = "";
	$ClientData[%clientId, "tmpname"] = "";
	
	// 6. Clear fast lookup arrays
	// -------------------------------------------------------------------------
	if(!%preserveBotInfoAiName)
		$BotInfoAiName[%clientId] = "";
	
	// -------------------------------------------------------------------------
	// 7. Clear $Belt::CachedList (prevents memory leaks)
	// -------------------------------------------------------------------------
	$Belt::CachedList[%clientId, "QuestItems"] = "";
	$Belt::CachedList[%clientId, "KeyItems"] = "";
	$Belt::CachedList[%clientId, "Consumables"] = "";
	$Belt::CachedList[%clientId, "Armor"] = "";
	$Belt::CachedList[%clientId, "Accessories"] = "";
	$Belt::CachedList[%clientId, "Other"] = "";
	
	// -------------------------------------------------------------------------
	// 8. Clear directive table entries (0-99)
	// -------------------------------------------------------------------------
	for(%d = 0; %d <= 99; %d++)
		$aidirectiveTable[%clientId, %d] = "";
	
	// -------------------------------------------------------------------------
	// 9. Clear EventCommand entries (0-99)
	// -------------------------------------------------------------------------
	for(%i = 0; %i <= 99; %i++)
		$EventCommand[%clientId, %i] = "";
	
	// -------------------------------------------------------------------------
	// 10. Clear LoadedProjectile for common weapons
	// -------------------------------------------------------------------------
	%commonWeapons = "Crossbow Bow Rifle Pistol Shotgun";
	for(%i = 0; (%weapon = GetWord(%commonWeapons, %i)) != -1; %i++)
		storeData(%clientId, "LoadedProjectile " @ %weapon, "");

	if(%useStoreTypeOverride)
		EndStoreDataClientTypeOverride(%clientId);

	// -------------------------------------------------------------------------
	// 11. Clear $BotType cache LAST (after all storeData calls complete)
	// -------------------------------------------------------------------------
	$BotType[%clientId] = "";
}


// Check if a bot is registered
function IsBotRegistered(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return false;
	if($BotRegistry[%clientId] != "" && $BotRegistry[%clientId] != -1)
		return true;
	return false;
}

function GetRegisteredBotCount(%spawnPointId)
{
	%count = 0;
	for(%i = 0; GetWord($BotRegistryList, %i) != -1; %i++)
	{
		%clientId = GetWord($BotRegistryList, %i);
		if($BotRegistry[%clientId] == %spawnPointId)
			%count++;
	}
	return %count;
}

// Cleanup a specific orphaned client ID - called via schedule after timing issue is detected
// This checks if the timing issue persisted (indicating a true orphan) and deletes the object if so
function CleanupOrphanedClientId(%clientId, %originalPlayerObj)
{
	// Clear the scheduled flag
	$OrphanCleanupScheduled[%clientId] = "";
	
	// First, verify Client::getOwnedObject still returns -1 (timing issue persisted)
	%currentOwnedObj = Client::getOwnedObject(%clientId);
	if(%currentOwnedObj != -1 && %currentOwnedObj != "")
	{
		// Timing issue resolved - client now owns an object, no cleanup needed
		return;
	}
	
	// Timing issue persisted - this is truly an orphaned object
	// Check if the original player object still exists and still claims this client ID
	if(isObject(%originalPlayerObj))
	{
		%objClientId = Player::getClient(%originalPlayerObj);
		if(%objClientId == %clientId)
		{
			// Verify this is not a real player before deleting
			if(!IsRealPlayer(%clientId))
			{
				%dataName = GameBase::getDataName(%originalPlayerObj);
				
				// TOWN BOT RESPAWN: Check if this is an orphaned town bot
				// Town bots have BotInfoAiName starting with "TownBot_"
				%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
				%isTownBot = (String::findSubStr(%botInfoAiName, "TownBot_") == 0);
				
				if(%isTownBot)
				{
					// Extract bot name from "TownBot_botname"
					%botName = String::getSubStr(%botInfoAiName, 8, 100);
					%zoneIndex = $TownBotZone[%botName];
					
					echo("[ORPHAN CLEANUP] Town bot orphaned: " @ %botInfoAiName @ " (clientId=" @ %clientId @ ", zone=" @ %zoneIndex @ ") - triggering respawn");
					
					// Clear spawn tracking so the bot can respawn
					$TownBotSpawned[%botName] = "";
					
					// Remove from TownBotList
					%newList = "";
					for(%j = 0; (%id = GetWord($TownBotList, %j)) != -1; %j++)
					{
						if(%id != %clientId)
							%newList = %newList @ %id @ " ";
					}
					$TownBotList = %newList;
					
					// Clear bot type cache
					$BotType[%clientId] = "";
					
					// Clear TownBotData
					$TownBotData[%clientId, "BotInfoAiName"] = "";
					$TownBotData[%clientId, "SpawnBotInfo"] = "";
					
					// Delete the orphaned object
					deleteObject(%originalPlayerObj);
					
					// Schedule respawn if players are still in the zone
					if(%zoneIndex != "" && %zoneIndex != -1 && $ZonePlayerCount[%zoneIndex] > 0)
					{
						echo("[ORPHAN CLEANUP] Scheduling respawn for town bot " @ %botName @ " in zone " @ %zoneIndex);
						schedule("SpawnSingleZoneBot(\"" @ %botName @ "\", " @ %zoneIndex @ ");", 2);
					}
				}
				else
				{
					// Regular enemy bot orphan - just delete
					echo("[ORPHAN CLEANUP] Delayed cleanup: Object " @ %originalPlayerObj @ " (data=" @ %dataName @ ") is still orphaned for clientId=" @ %clientId @ " after 10s - deleting");
					deleteObject(%originalPlayerObj);
				}
			}
			else
			{
				echo("[ORPHAN CLEANUP] Delayed cleanup: Object " @ %originalPlayerObj @ " for clientId=" @ %clientId @ " - SKIPPED (IsRealPlayer=true)");
			}
		}
	}
	
	// Also scan for any other orphaned objects with this client ID (there might be multiple)
	if(isObject("MissionCleanup"))
	{
		%group = nameToID("MissionCleanup");
		%count = Group::objectCount(%group);
		for(%i = %count - 1; %i >= 0; %i--)
		{
			%obj = Group::getObject(%group, %i);
			if(!isObject(%obj)) continue;
			if(getObjectType(%obj) != "Player") continue;
			if(%obj == %originalPlayerObj) continue; // Already handled above
			
			%objClientId = Player::getClient(%obj);
			if(%objClientId == %clientId)
			{
				if(!IsRealPlayer(%clientId))
				{
					%dataName = GameBase::getDataName(%obj);
					
					// Check if this is a town bot
					%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
					%isTownBot = (String::findSubStr(%botInfoAiName, "TownBot_") == 0);
					
					if(%isTownBot)
					{
						echo("[ORPHAN CLEANUP] Delayed cleanup: Additional orphaned town bot object " @ %obj @ " (data=" @ %dataName @ ") found for clientId=" @ %clientId @ " - deleting (respawn handled by primary cleanup)");
					}
					else
					{
						echo("[ORPHAN CLEANUP] Delayed cleanup: Additional orphaned object " @ %obj @ " (data=" @ %dataName @ ") found for clientId=" @ %clientId @ " - deleting");
					}
					deleteObject(%obj);
				}
			}
		}
	}
}

// Debug function to dump the entire bot registry state
// Call with: DumpBotRegistry();
function DumpBotRegistry()
{
	echo("==================================================");
	echo(" BOT REGISTRY DUMP");
	echo("==================================================");
	echo(" $BotRegistryList: '" @ $BotRegistryList @ "'");
	echo(" Total entries: " @ GetWordCount($BotRegistryList));
	echo("--------------------------------------------------");
	
	for(%i = 0; GetWord($BotRegistryList, %i) != -1; %i++)
	{
		%clientId = GetWord($BotRegistryList, %i);
		%spawnPoint = $BotRegistry[%clientId];
		%team = $BotRegistry[%clientId, "team"];
		%name = $BotRegistry[%clientId, "name"];
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		%playerObj = Client::getOwnedObject(%clientId);
		%clientName = Client::getName(%clientId);
		
		echo(" [" @ %i @ "] clientId=" @ %clientId);
		echo("     Registry: spawnPoint='" @ %spawnPoint @ "', team='" @ %team @ "', name='" @ %name @ "'");
		echo("     StoreData: BotInfoAiName='" @ %botInfoAiName @ "', SpawnBotInfo='" @ %spawnBotInfo @ "'");
		echo("     Engine: playerObj=" @ %playerObj @ ", clientName='" @ %clientName @ "'");
		
		if(%playerObj == -1 || %playerObj == "")
			echo("     STATUS: >>> SHELL BOT (no player object) <<<");
		else if(%spawnPoint == "" && %botInfoAiName == "")
			echo("     STATUS: >>> ZOMBIE (in list but no data) <<<");
	}
	echo("==================================================");
}

// Clean up orphaned bot objects that exist in the world but have broken client associations
// This happens when: Player::getClient(obj) returns a clientId, but Client::getOwnedObject(clientId) returns -1
// Call with: CleanupOrphanedBotObjects();
function CleanupOrphanedBotObjects()
{
	echo("==================================================");
	echo(" ORPHANED BOT CLEANUP");
	echo("==================================================");
	
	%cleanedCount = 0;
	%checkedCount = 0;
	
	// Scan BotGroup first
	if(isObject("BotGroup"))
	{
		%group = nameToID("BotGroup");
		%count = Group::objectCount(%group);
		echo(" Scanning BotGroup (" @ %count @ " objects)...");
		
		// Iterate backwards to safely delete
		for(%i = %count - 1; %i >= 0; %i--)
		{
			%obj = Group::getObject(%group, %i);
			if(!isObject(%obj)) continue;
			if(getObjectType(%obj) != "Player") continue;
			
			%checkedCount++;
			%clientId = Player::getClient(%obj);
			
			if(%clientId != -1 && %clientId != "")
			{
				%ownedObj = Client::getOwnedObject(%clientId);
				
				// ORPHAN DETECTED: Object thinks it belongs to client, but client doesn't own it
				if(%ownedObj == -1 || %ownedObj == "")
				{
					%dataName = GameBase::getDataName(%obj);
					%pos = GameBase::getPosition(%obj);
					echo(" [ORPHAN] Object " @ %obj @ " (data=" @ %dataName @ ") claims clientId=" @ %clientId @ " but Client::getOwnedObject returns -1");
					echo("          Position: " @ %pos);
					
					// SAFEGUARD: Verify this is not a real player
					if(!IsRealPlayer(%clientId))
					{
						echo("          ACTION: Deleting orphaned bot object...");
						deleteObject(%obj);
						%cleanedCount++;
					}
					else
					{
						echo("          ACTION: SKIPPED - IsRealPlayer returned true (protected)");
					}
				}
			}
		}
	}
	
	// Also scan MissionCleanup for any missed orphans
	if(isObject("MissionCleanup"))
	{
		%group = nameToID("MissionCleanup");
		%count = Group::objectCount(%group);
		echo(" Scanning MissionCleanup (" @ %count @ " objects)...");
		
		for(%i = %count - 1; %i >= 0; %i--)
		{
			%obj = Group::getObject(%group, %i);
			if(!isObject(%obj)) continue;
			if(getObjectType(%obj) != "Player") continue;
			
			%checkedCount++;
			%clientId = Player::getClient(%obj);
			
			if(%clientId != -1 && %clientId != "" && %clientId >= $BaseRepClientIdMin)
			{
				%ownedObj = Client::getOwnedObject(%clientId);
				
				if(%ownedObj == -1 || %ownedObj == "")
				{
					%dataName = GameBase::getDataName(%obj);
					%pos = GameBase::getPosition(%obj);
					echo(" [ORPHAN] Object " @ %obj @ " (data=" @ %dataName @ ") claims clientId=" @ %clientId @ " but Client::getOwnedObject returns -1");
					echo("          Position: " @ %pos);
					
					if(!IsRealPlayer(%clientId))
					{
						echo("          ACTION: Deleting orphaned bot object...");
						deleteObject(%obj);
						%cleanedCount++;
					}
					else
					{
						echo("          ACTION: SKIPPED - IsRealPlayer returned true (protected)");
					}
				}
			}
		}
	}
	
	echo("--------------------------------------------------");
	echo(" Checked: " @ %checkedCount @ " player objects");
	echo(" Cleaned: " @ %cleanedCount @ " orphaned objects");
	echo("==================================================");
	
	return %cleanedCount;
}

// ============================================================================
// UNIFIED PLAYER SAFEGUARD FUNCTIONS
// ============================================================================
// These functions consolidate all player protection checks into a single, 
// reliable API. They should be used instead of scattered individual checks.
// 
// Check ordering (fastest to slowest):
// 1. Player::isAiControlled() - Engine-level, very fast
// 2. Client ID range check - Simple comparison  
// 3. isFile("temp\\name.cs") - Filesystem access, slowest but most reliable
//
// Debug output controlled by: $Debug::SafeGuards
// ============================================================================

// $Debug::SafeGuards is defined at the top of the file with other debug flags

// IsRealPlayer: Quick check to determine if a client ID belongs to a real player
// Returns: true if real player, false if bot
// Updated to handle "Ghost Bots" (disconnected bots that lose isAiControlled status but have lingering data)
function IsRealPlayer(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return false;
		
	// Priority 1: Save file check (Ultimate Truth for RPG Mod)
	// We check this FIRST because Ghost Bots are !isAiControlled (like players) but !isFile (unlike players)
	%playerName = Client::getName(%clientId);
	if(%playerName != "" && %playerName != -1)
	{
		// Check cache first (set at connection time)
		if($PlayerHasSaveFile[%clientId] == true || $PlayerHasSaveFile[%clientId] == "1")
		{
			if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " has cached save file = REAL PLAYER");
			return true;
		}
		
		// Fallback to filesystem check
		if(isFile("temp\\" @ %playerName @ ".cs"))
		{
			// Update cache for future checks
			$PlayerHasSaveFile[%clientId] = true;
			PlayerManager::reserveId(%clientId);   // native DLL: bots can't inherit this real player's id
			if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " (" @ %playerName @ ") has save file = REAL PLAYER");
			return true;
		}
	}
	
	// Priority 2: Engine check
	// If the engine knows it's AI, it's definitely not a real player
	if(Player::isAiControlled(%clientId))
	{
		return false;
	}
	
	// Priority 3: Ghost Bot Check
	// If execution reaches here, it is NOT AI-controlled (could be Real Player OR Ghost Bot)
	// We distinguish them by checking for lingering bot data
	%botInfo = fetchData(%clientId, "BotInfoAiName");
	%spawnInfo = fetchData(%clientId, "SpawnBotInfo");
	if((%botInfo != "" && %botInfo != -1 && %botInfo != "0") || (%spawnInfo != "" && %spawnInfo != -1 && %spawnInfo != "0"))
	{
		// It has bot data but no save file -> It is a Ghost Bot
		if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " has data '" @ %botInfo @ "'/'" @ %spawnInfo @ "' but !isAiControlled = GHOST BOT (Not Real)");
		return false;
	}
	
	// Priority 4: Non-BaseRep IDs are never AI/player client IDs in this system.
	// Treat as player-safe fallback.
	if(%clientId < $BaseRepClientIdMin)
	{
		if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " is below BaseRep range (< " @ $BaseRepClientIdMin @ ") = REAL PLAYER (safe fallback)");
		return true;
	}
	
	// Priority 5: Town Bot Client check (O(1) lookup via $TownBotClient)
	// This is faster than string search and more reliable
	if(IsTownBotClientId(%clientId))
	{
		if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " is town bot (via IsTownBotClientId) = NOT REAL");
		return false;
	}
	
	// Priority 6: Bot Registry check
	// Enemy bots spawned from SpawnPoints are tracked in $BotRegistry
	// BUGFIX: RegisterBot stores the spawn point at $BotRegistry[%clientId] (no subkey);
	// the old $BotRegistry[%clientId, "spawnPoint"] key is never written, so this tier never fired
	if($BotRegistry[%clientId] != "" && $BotRegistry[%clientId] != -1)
	{
		if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " found in $BotRegistry = ENEMY BOT (Not Real)");
		return false;
	}
	
	// Fallback: If we can't prove it's a bot, assume it's a player for safety
	if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " fallback safety check = REAL PLAYER");
	return true;
}

// SafeAIDelete: Safely delete a bot after verifying it's still AI-controlled
// This prevents ghost shell bots when a player connects on the same clientId during the delete delay
// Called from scheduled deletion in Player::onKilled()
function SafeAIDelete(%botName, %originalClientId)
{
	// First check: Is this clientId still AI-controlled?
	%playerObj = Client::getOwnedObject(%originalClientId);
	if(%playerObj != "" && %playerObj != -1 && isObject(%playerObj))
	{
		if(!Player::isAiControlled(%playerObj))
		{
			// A real player has connected on this clientId - DO NOT DELETE
			echo("[SAFEGUARD] SafeAIDelete: ClientId " @ %originalClientId @ " is now a REAL PLAYER - skipping AI::delete for " @ %botName);
			return;
		}
	}
	
	// Second check: Verify this bot name still maps to the expected clientId
	// Use AI::getClientIdFromName() (silent) instead of AI::getId() - the engine renames
	// dead AIs to "Corpse<N> <name>" (aiObj.cpp doPostLoopComputations), so AI::getId() on
	// the original name ALWAYS fails after death and spams "Could not find drone" every kill
	%currentClientId = AI::getClientIdFromName(%botName);
	if(%currentClientId != %originalClientId && %currentClientId != "" && %currentClientId != -1 && %currentClientId != "False")
	{
		// Bot name now refers to a different clientId - a new bot spawned with same name
		echo("[SAFEGUARD] SafeAIDelete: Bot " @ %botName @ " now on different clientId (" @ %currentClientId @ " vs original " @ %originalClientId @ ") - skipping delete");
		return;
	}
	
	// Safe to delete
	if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] SafeAIDelete: Verified safe, calling AI::delete(" @ %botName @ ")");
	AI::delete(%botName);
}

// IsSafeToModify: Master safeguard function - checks if safe to modify a client ID
// Returns: true if safe to modify (is a bot), false if real player detected (UNSAFE)
// Use this before ANY operation that could affect a player's game state
function IsSafeToModify(%clientId, %operation)
{
	if(%clientId == "" || %clientId == -1)
	{
		if($Debug::SafeGuards)
			echo("[SAFEGUARD] IsSafeToModify [" @ %operation @ "]: Invalid clientId - returning false (safe, no target)");
		return false; // Nothing to modify
	}
	
	// PRIORITY CHECK: Save file cache - checked FIRST to catch bot-player collisions
	// If a bot has overwritten a player's client ID, the engine sees it as AI-controlled
	// but the save file cache still correctly identifies it as a real player's ID
	if($PlayerHasSaveFile[%clientId] == true || $PlayerHasSaveFile[%clientId] == "1")
	{
		echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: Client " @ %clientId @ " has CACHED save file (possible bot-player collision!) - REAL PLAYER PROTECTED");
		return false;
	}
	
	// ZOMBIE CLIENT CHECK: Detect desync between PlayerManager and SimManager
	// Real players have a NetConnection/PacketStream object at their client ID
	// Bots do NOT have an engine object at their client ID - only a PlayerManager entry
	// If isObject() returns true but it's not a Player, it's a zombie NetConnection!
	if(isObject(%clientId))
	{
		%objType = getObjectType(%clientId);
		// "Player" type means it's a valid bot/player object - that's OK
		// Any OTHER type (NetConnection, PacketStream, etc.) means zombie client!
		if(%objType != "Player" && %objType != "" && %objType != "False")
		{
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: Client " @ %clientId @ " has zombie object type '" @ %objType @ "' - ZOMBIE CLIENT PROTECTED");
			return false;
		}
	}
	
	// Layer 1: Engine check (fastest for normal bots)
	if(Player::isAiControlled(%clientId))
	{
		// Is AI -> Safe to modify (unless cache check above caught a collision)
	}
	else
	{
		// Not AI controlled - could be Real Player OR Ghost Bot
		// Use IsRealPlayer() which now handles Ghost Bot detection
		if(IsRealPlayer(%clientId))
		{
			echo("[BLOCKED] SAFEGUARD [" @ %operation @ "]: Client " @ %clientId @ " detected as REAL PLAYER - PROTECTED");
			return false;
		}
		// If IsRealPlayer returns false here, it's a Ghost Bot -> Safe to modify (cleanup)
	}
	
	// Layer 2: Filesystem save file check (fallback - catches cases where cache wasn't populated)
	%playerName = Client::getName(%clientId);
	if(%playerName != "" && %playerName != -1)
	{
		// Filesystem check only (cache already checked at top of function)
		if(isFile("temp\\" @ %playerName @ ".cs"))
		{
			// Update cache
			$PlayerHasSaveFile[%clientId] = true;
			PlayerManager::reserveId(%clientId);   // native DLL: bots can't inherit this real player's id
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: Client " @ %clientId @ " (" @ %playerName @ ") has save file - REAL PLAYER PROTECTED");
			return false;
		}
	}
	
	// All checks passed - safe to modify (is a bot)
	if($Debug::SafeGuards)
		echo("[SAFEGUARD] IsSafeToModify [" @ %operation @ "]: Client " @ %clientId @ " is a BOT - OK to modify");
	return true;
}

// IsSafeToModifyForEnemyBot: Extended safeguard for enemy bot operations
// This protects BOTH real players AND town bots from being modified/deleted by enemy bot logic
// Use this in enemy bot spawn/cleanup paths to prevent town bot conflicts
// Returns: true if safe to modify (is an enemy bot or stale data), false if PROTECTED
function IsSafeToModifyForEnemyBot(%clientId, %operation)
{
	if(%clientId == "" || %clientId == -1)
	{
		if($Debug::SafeGuards)
			echo("[SAFEGUARD] IsSafeToModifyForEnemyBot [" @ %operation @ "]: Invalid clientId - returning false");
		return false;
	}
	
	// Layer 1: Check if it's a real player (existing protection)
	if(!IsSafeToModify(%clientId, %operation))
	{
		// Already blocked by real player protection
		return false;
	}
	
	// Layer 2: CRITICAL - Check if this is an active town bot
	// Town bots pass IsSafeToModify() because they're AI-controlled, but we don't want to despawn them
	if(isTownBot(%clientId))
	{
		// Verify the town bot is actually alive (has valid player object)
		%townBotPlayerObj = Client::getOwnedObject(%clientId);
		if(%townBotPlayerObj != "" && %townBotPlayerObj != -1 && isObject(%townBotPlayerObj))
		{
			// Active town bot - PROTECT IT
			echo("[BLOCKED] SAFEGUARD [" @ %operation @ "]: Client " @ %clientId @ " is an ACTIVE TOWN BOT - PROTECTED from enemy bot operations");
			return false;
		}
		// Town bot is dead/invalid - safe to clean up its data
		if($Debug::SafeGuards)
			echo("[SAFEGUARD] IsSafeToModifyForEnemyBot [" @ %operation @ "]: Client " @ %clientId @ " is a dead/stale town bot - OK to cleanup");
	}
	
	// Layer 3: Check $TownBotSpawned registry directly (belt and suspenders)
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
	{
		if($TownBotSpawned[%botName] == %clientId)
		{
			// This client ID is registered to a town bot
			%townBotPlayerObj = Client::getOwnedObject(%clientId);
			if(%townBotPlayerObj != "" && %townBotPlayerObj != -1 && isObject(%townBotPlayerObj))
			{
				echo("[BLOCKED] SAFEGUARD [" @ %operation @ "]: Client " @ %clientId @ " is registered to ACTIVE town bot '" @ %botName @ "' - PROTECTED");
				return false;
			}
		}
	}
	
	// All checks passed - safe to modify for enemy bot purposes
	if($Debug::SafeGuards)
		echo("[SAFEGUARD] IsSafeToModifyForEnemyBot [" @ %operation @ "]: Client " @ %clientId @ " is NOT a protected entity - OK to modify");
	return true;
}

// IsSafeToDeletePlayerObject: Specific safeguard for player object deletion
// This is the most critical safeguard - used before deleteObject() on player objects
// Returns: true if safe to delete (object belongs to a bot), false if UNSAFE
function IsSafeToDeletePlayerObject(%playerObj, %clientId, %operation)
{
	// Validate object exists
	if(%playerObj == -1 || %playerObj == "" || !isObject(%playerObj))
	{
		if($Debug::SafeGuards)
			echo("[SAFEGUARD] IsSafeToDeletePlayerObject [" @ %operation @ "]: Object doesn't exist - returning false");
		return false; // Nothing to delete
	}
	
	// Layer 1: Engine check on player object directly (most reliable)
	if(!Player::isAiControlled(%playerObj))
	{
		// Exception: Ghost Bots
		// If the provided %clientId is confirmed to be a Bot (via IsRealPlayer, logic includes ghost check),
		// we allow proceeding to subsequent layers (which will double-check save files).
		%isGhostException = false;
		if(%clientId != "" && %clientId != -1)
		{
			if(!IsRealPlayer(%clientId))
				%isGhostException = true;
		}
		
		if(!%isGhostException)
		{
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: PlayerObj " @ %playerObj @ " is NOT AI-controlled - REAL PLAYER OBJECT PROTECTED");
			return false;
		}
	}
	
	// Layer 2: If clientId provided, verify it's in BaseRep range.
	if(%clientId != "" && %clientId != -1)
	{
		if(%clientId < $BaseRepClientIdMin)
		{
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: ClientId " @ %clientId @ " is below BaseRep range (< " @ $BaseRepClientIdMin @ ") - REAL PLAYER PROTECTED");
			return false;
		}
		
		// Double-check: client should also be AI-controlled
		if(!Player::isAiControlled(%clientId))
		{
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: ClientId " @ %clientId @ " is NOT AI-controlled - REAL PLAYER PROTECTED");
			return false;
		}
	}
	
	// Layer 3: Save file check - try multiple ways to get player name
	%playerName = "";
	
	// Method 1: Get name from client ID
	if(%clientId != "" && %clientId != -1)
		%playerName = Client::getName(%clientId);
	
	// Method 2: Get control client from object
	if(%playerName == "" || %playerName == -1)
	{
		%controlClient = GameBase::getControlClient(%playerObj);
		if(%controlClient != "" && %controlClient != -1)
			%playerName = Client::getName(%controlClient);
	}
	
	// Method 3: Get from stored data
	if(%playerName == "" || %playerName == -1)
	{
		if(%clientId != "" && %clientId != -1)
			%playerName = fetchData(%clientId, "name");
	}
	
	// Check save file if we have a name
	if(%playerName != "" && %playerName != -1)
	{
		// Check cache first
		if($PlayerHasSaveFile[%clientId] == true || $PlayerHasSaveFile[%clientId] == "1")
		{
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: Player " @ %playerName @ " has CACHED save file - REAL PLAYER OBJECT PROTECTED");
			return false;
		}
		
		// Fallback to filesystem
		if(isFile("temp\\" @ %playerName @ ".cs"))
		{
			// BUGFIX: braces were missing, so reserveId() ran even with an empty/-1 clientId
			if(%clientId != "" && %clientId != -1)
			{
				$PlayerHasSaveFile[%clientId] = true;
				PlayerManager::reserveId(%clientId);   // native DLL: bots can't inherit this real player's id
			}
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: Player " @ %playerName @ " has save file - REAL PLAYER OBJECT PROTECTED");
			return false;
		}
	}
	
	// All checks passed - safe to delete (belongs to a bot)
	if($Debug::SafeGuards)
		echo("[SAFEGUARD] IsSafeToDeletePlayerObject [" @ %operation @ "]: PlayerObj " @ %playerObj @ " (clientId=" @ %clientId @ ") is a BOT OBJECT - OK to delete");
	return true;
}

// ClearPlayerSaveFileCache: Clears the save file cache for a client
// Call this when a player disconnects to clean up cache
function ClearPlayerSaveFileCache(%clientId)
{
	if(%clientId != "" && %clientId != -1)
	{
		$PlayerHasSaveFile[%clientId] = "";
		schedule("PlayerManager::releaseId(" @ %clientId @ ");", 30.0);   // native DLL: release 30s later, after the zombie object is torn down
		if($Debug::SafeGuards)
			echo("[SAFEGUARD] Cleared save file cache for clientId " @ %clientId);
	}
}

// ============================================================================
// CENTRALIZED SPAWN COUNTER MANAGEMENT
// This is the ONLY place spawn counters should be decremented
// ============================================================================

// Decrement spawn counter for a bot - uses multiple fallback methods
// %excludeObject: Optional player object ID to exclude from orphan cleanup (passed to UnregisterBot)
// Returns true if counter was decremented, false if no spawn point found
function DecrementSpawnCounter(%clientId, %excludeObject)
{
	if(%clientId == "" || %clientId == -1)
	{
		if($SPAWN_COUNTER_DEBUG) echo("[SPAWN COUNTER] DecrementSpawnCounter: Invalid clientId");
		return false;
	}
	
	%spawnPointId = "";
	%source = "";
	
	// Priority 1: Check $BotRegistry (authoritative source)
	%spawnPointId = $BotRegistry[%clientId];
	if(%spawnPointId != "" && %spawnPointId != -1 && %spawnPointId != "0")
	{
		%source = "BotRegistry";
	}
	
	// Priority 2: Check fetchData SpawnBotInfo
	if(%spawnPointId == "" || %spawnPointId == -1 || %spawnPointId == "0")
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
		{
			%spawnPointId = GetWord(%spawnBotInfo, 1);
			%source = "fetchData";
		}
	}
	
	// Priority 3: Check $EnemyBotData array
	if(%spawnPointId == "" || %spawnPointId == -1 || %spawnPointId == "0")
	{
		%spawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
		if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
		{
			%spawnPointId = GetWord(%spawnBotInfo, 1);
			%source = "EnemyBotData";
		}
	}
	
	// Priority 4: Check $ClientData array
	if(%spawnPointId == "" || %spawnPointId == -1 || %spawnPointId == "0")
	{
		%spawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
		if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
		{
			%spawnPointId = GetWord(%spawnBotInfo, 1);
			%source = "ClientData";
		}
	}
	
	// If we found a spawn point, decrement the counter
	if(%spawnPointId != "" && %spawnPointId != -1 && %spawnPointId != "0")
	{
		%oldCounter = $numAIperSpawnPoint[%spawnPointId];
		if(%oldCounter == "")
			%oldCounter = 0;
		
		if(%oldCounter > 0)
		{
			$numAIperSpawnPoint[%spawnPointId]--;
			if($SPAWN_COUNTER_DEBUG) echo("[SPAWN COUNTER] Decremented counter for SpawnPoint " @ %spawnPointId @ " (was: " @ %oldCounter @ ", now: " @ $numAIperSpawnPoint[%spawnPointId] @ ") - source: " @ %source @ ", clientId: " @ %clientId);
		}
		else
		{
			if($SPAWN_COUNTER_DEBUG) echo("[SPAWN COUNTER] WARNING: Counter already 0 for SpawnPoint " @ %spawnPointId @ " - cannot decrement (source: " @ %source @ ", clientId: " @ %clientId @ ")");
		}
		
		// Ensure counter never goes below 0
		if($numAIperSpawnPoint[%spawnPointId] < 0)
			$numAIperSpawnPoint[%spawnPointId] = 0;
		
		// Unregister from bot registry (pass excludeObject to prevent deleting dying player)
		UnregisterBot(%clientId, %excludeObject);
		
		return true;
	}
	
	// Check if this is a TempSpawn bot (Colloseum/Seal Battle bots don't use spawn point counters)
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%isTempSpawn = false;
	if(%spawnBotInfo != "" && String::findSubStr(%spawnBotInfo, "TempSpawn") == 0)
		%isTempSpawn = true;
	if(!%isTempSpawn)
	{
		%spawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
		if(%spawnBotInfo != "" && String::findSubStr(%spawnBotInfo, "TempSpawn") == 0)
			%isTempSpawn = true;
	}
	if(!%isTempSpawn)
	{
		%spawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
		if(%spawnBotInfo != "" && String::findSubStr(%spawnBotInfo, "TempSpawn") == 0)
			%isTempSpawn = true;
	}
	
	// Only show warning if it's NOT a TempSpawn bot (TempSpawn bots don't use spawn point counters)
	if(!%isTempSpawn)
		if($SPAWN_COUNTER_DEBUG) echo("[SPAWN COUNTER] WARNING: Could not find spawn point for clientId " @ %clientId @ " - counter NOT decremented");
	
	// Still unregister even if we couldn't find spawn point
	// Pass excludeObject to avoid touching the actively dying object in callback context
	UnregisterBot(%clientId, %excludeObject);
	
	return false;
}


// ============================================================================
// SPAWN COUNTER RECONCILIATION
// Periodically checks actual bot counts against counters and fixes discrepancies
// ============================================================================

function ReconcileSpawnCounters()
{
	if($RECONCILE_DEBUG) echo("[RECONCILE] Starting spawn counter reconciliation...");
	
	%totalFixed = 0;
	%totalChecked = 0;
	
	// Get all spawn points from the MissionGroup
	%group = nameToID("MissionGroup\\SpawnPoints");
	
	if(%group != -1)
	{
		for(%i = 0; %i <= Group::objectCount(%group) - 1; %i++)
		{
			%spawnPoint = Group::getObject(%group, %i);
			if(%spawnPoint == -1 || %spawnPoint == "")
				continue;
			
			%info = Object::getName(%spawnPoint);
			if(%info == "")
				continue;
			
			%totalChecked++;
			
			// Count actual bots registered to this spawn point
			%actualCount = GetRegisteredBotCount(%spawnPoint);
			
			// Get the current counter value
			%counterValue = $numAIperSpawnPoint[%spawnPoint];
			if(%counterValue == "")
				%counterValue = 0;
			
			// Check if a spawn is currently in progress for this spawnpoint
			%inProgress = ($SpawnPointInProgress[%spawnPoint] == "true");
			
			// If a spawn is in progress, skip correcting this point entirely
			// This prevents reconciliation from zeroing the counter while a bot is mid-spawn
			if(%inProgress)
				continue;
			
			// Check for discrepancy
			if(%actualCount != %counterValue)
			{
				if($RECONCILE_DEBUG) echo("[RECONCILE] DISCREPANCY FOUND: SpawnPoint " @ %spawnPoint @ " - Counter=" @ %counterValue @ ", Actual=" @ %actualCount @ " - FIXING");
				$numAIperSpawnPoint[%spawnPoint] = %actualCount;
				%totalFixed++;
			}
		}
	}
	
	// Also check spawn points in the common ID range (8650-8700)
	for(%sp = 8650; %sp <= 8700; %sp++)
	{
		%counterValue = $numAIperSpawnPoint[%sp];
		if(%counterValue != "" && %counterValue > 0)
		{
			%inProgress = ($SpawnPointInProgress[%sp] == "true");
			if(%inProgress)
				continue;
			
			%actualCount = GetRegisteredBotCount(%sp);
			
			if(%actualCount != %counterValue)
			{
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] DISCREPANCY FOUND: SpawnPoint " @ %sp @ " - Counter=" @ %counterValue @ ", Actual=" @ %actualCount @ " - FIXING");
				$numAIperSpawnPoint[%sp] = %actualCount;
				%totalFixed++;
			}
			%totalChecked++;
		}
	}
	
	// Verify registry list integrity - remove any invalid entries
	%newList = "";
	%removedCount = 0;
	// SAFETY: Add max iteration limit to prevent infinite loop if registry is corrupted
	%maxIterations = 500;
	for(%i = 0; GetWord($BotRegistryList, %i) != -1 && %i < %maxIterations; %i++)
	{
		%clientId = GetWord($BotRegistryList, %i);
		
		// Check if this client ID still has a valid player object
		%playerObj = Client::getOwnedObject(%clientId);
		%now = getSimTime();
		%lastSeen = $BotRegistryLastSeen[%clientId];
		%hasLoaded = fetchData(%clientId, "HasLoadedAndSpawned");
		
		if(%playerObj == -1 || %playerObj == "")
		{
			// Grace period / safety: if bot was marked loaded or recently seen, keep it
			if(%hasLoaded)
			{
				$BotRegistryLastSeen[%clientId] = %now;
				
				if(%newList == "")
					%newList = %clientId;
				else
					%newList = %newList @ " " @ %clientId;
				continue;
			}
			
			if(%lastSeen == "" || %lastSeen == -1)
			{
				// First time missing: record timestamp and keep
				$BotRegistryLastSeen[%clientId] = %now;
				
				if(%newList == "")
					%newList = %clientId;
				else
					%newList = %newList @ " " @ %clientId;
				continue;
			}
			
			%age = %now - %lastSeen;
			if(%age < 10) // <10 seconds missing: keep
			{
				if(%newList == "")
					%newList = %clientId;
				else
					%newList = %newList @ " " @ %clientId;
				continue;
			}
			
			// Bot missing beyond grace: clean up registry
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] Removing dead bot from registry: clientId=" @ %clientId @ " (missing " @ %age @ "s)");
			
			// Proactively clean up the engine-side AI entry to prevent shells
			// BUGFIX: this block was guarded by (%playerObj != -1 && ...) inside the branch
			// where %playerObj is ALWAYS -1/"", so it never ran. There is no owned object to
			// delete here, but the engine may still know the AI by name - run the safeguards
			// and the name-based AI::delete.
			%reconcileNameCheck = Client::getName(%clientId);
			if(%reconcileNameCheck != "" && %reconcileNameCheck != -1)
			{
				%reconcileCharacterFile = "temp\\" @ %reconcileNameCheck @ ".cs";
				if(isFile(%reconcileCharacterFile))
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] CRITICAL SAFEGUARD - Real player " @ %reconcileNameCheck @ " (clientId=" @ %clientId @ ") detected. Skipping deletion to prevent data loss.");
					continue; // Skip this client ID
				}
			}

			// Additional check: Verify this is not a connected real player
			%isReconcileConnected = false;
			// SAFETY: Add max iteration limit to prevent infinite loop
			%clientCheckCount = 0;
			for(%cl = Client::getFirst(); %cl != -1 && %clientCheckCount < 200; %cl = Client::getNext(%cl))
			{
				%clientCheckCount++;
				if(%cl == %clientId)
				{
					%isReconcileConnected = true;
					break;
				}
			}

			// If connected but no save file yet, check if it's a real player by checking if it's NOT AI-controlled
			if(%isReconcileConnected && !Player::isAiControlled(%clientId))
			{
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] CRITICAL SAFEGUARD - Connected client " @ %clientId @ " is NOT AI-controlled. This is a real player. Skipping deletion.");
				continue; // Skip this client ID
			}

			// All checks passed - clean up via AI::delete (proper engine cleanup)
			%aiName = $BotInfoAiName[%clientId];
			if(%aiName == "") %aiName = $EnemyBotData[%clientId, "BotInfoAiName"];
			if(%aiName == "") %aiName = $TownBotData[%clientId, "BotInfoAiName"];
			if(%aiName == "") %aiName = fetchData(%clientId, "BotInfoAiName");

			if(%aiName != "" && %aiName != -1 && %aiName != "0")
			{
				// SAFETY: only delete by name if the name still maps to THIS clientId
				// (or is unknown to the engine) - a respawned bot may have reused the name
				%reconcileActualId = AI::getClientIdFromName(%aiName);
				if(%reconcileActualId == %clientId || %reconcileActualId == -1 || %reconcileActualId == "")
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] Deleting lingering bot via AI::delete: " @ %aiName @ " (clientId=" @ %clientId @ ")");
					AI::delete(%aiName);
				}
				else
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] Bot name " @ %aiName @ " now alive on clientId " @ %reconcileActualId @ " - skipping AI::delete, clearing registry only");
				}
			}

			// Decrement counter for this bot's spawn point
			%spawnPointId = $BotRegistry[%clientId];
			if(%spawnPointId != "" && %spawnPointId != -1)
			{
				if($numAIperSpawnPoint[%spawnPointId] > 0)
					$numAIperSpawnPoint[%spawnPointId]--;
			}
			
			// Clear registry entries
			$BotRegistry[%clientId] = "";
			$BotRegistry[%clientId, "team"] = "";
			$BotRegistry[%clientId, "name"] = "";
			$BotRegistryLastSeen[%clientId] = "";
			%removedCount++;
		}
		else
		{
			// Bot still valid, keep in list and refresh last-seen
			$BotRegistryLastSeen[%clientId] = %now;
			if(%newList == "")
				%newList = %clientId;
			else
				%newList = %newList @ " " @ %clientId;
		}
	}
	$BotRegistryList = %newList;
	
	if(%totalFixed > 0 || %removedCount > 0)
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] Completed: Checked " @ %totalChecked @ " spawn points, Fixed " @ %totalFixed @ " discrepancies, Removed " @ %removedCount @ " dead bots");
	}
	
	// NEW: Also reconcile town bots
	ReconcileTownBots();
	
	// Schedule next reconciliation in 30 seconds
	schedule("ReconcileSpawnCounters();", 30);
}

//=============================================================================
// Town Bot Reconciliation
// Checks for stale $TownBotClient entries and cleans them up
//=============================================================================
function ReconcileTownBots()
{
	%fixed = 0;
	%checked = 0;
	
	// Iterate through all registered town bot names
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1 && %i < 200; %i++)
	{
		if(%botName == "" || %botName == "0" || %botName == -1)
			continue;
			
		%clientId = $TownBotSpawned[%botName];
		if(%clientId == "" || %clientId == -1)
			continue;
			
		%checked++;
		
		// Check if bot is still alive (has valid player object)
		%playerObj = Client::getOwnedObject(%clientId);
		
		if(%playerObj == -1 || %playerObj == "")
		{
			// Bot died but wasn't cleaned up properly
			echo("[RECONCILE TOWN] Town bot " @ %botName @ " (clientId=" @ %clientId @ ") is dead - cleaning up stale entry");
			
			// Clear all tracking
			UnregisterTownBotClient(%clientId);
			$TownBotSpawned[%botName] = "";
			$TownBotList = RemoveFromCommaList($TownBotList, %clientId);
			$BotType[%clientId] = "";
			
			%fixed++;
		}
		else
		{
			// Bot is alive - verify $TownBotClient is in sync
			if(!IsTownBotClientId(%clientId))
			{
				// $TownBotClient not set but $TownBotSpawned says it exists - fix it
				%zone = $TownBotZone[%botName];
				if(%zone == "" || %zone == -1)
					%zone = 0;
				RegisterTownBotClient(%clientId, %botName, %zone);
				echo("[RECONCILE TOWN] Fixed missing $TownBotClient entry for " @ %botName @ " (clientId=" @ %clientId @ ")");
				%fixed++;
			}
		}
	}
	
	if(%fixed > 0)
		echo("[RECONCILE TOWN] Completed: Checked " @ %checked @ " town bots, Fixed " @ %fixed @ " stale entries");
}

// Start the reconciliation loop (call this from server init)
function StartSpawnCounterReconciliation()
{
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] Starting spawn counter reconciliation loop (every 30 seconds)");
	schedule("ReconcileSpawnCounters();", 30);
}

// ============================================================================
// PRE-SPAWN CLEANUP
// Comprehensive cleanup of stale data before spawning a new bot
// ============================================================================

function PreSpawnCleanup(%clientId)
{
	// UNIFIED SAFEGUARD: Verify this is a bot (handles Real Players vs Ghost Bots)
	if(!IsSafeToModify(%clientId, "PreSpawnCleanup"))
		return;
	
	if(%clientId == "" || %clientId == -1)
		return;

	// TOWN BOT GUARD: Never clear a LIVE town bot from the enemy spawn path.
	// Stale town bot data (no $TownBotClient entry) still gets cleaned below.
	if(IsTownBotClientId(%clientId))
	{
		echo("[PRE-SPAWN CLEANUP] WARNING: clientId " @ %clientId @ " is a live town bot - skipping cleanup");
		return;
	}

	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[PRE-SPAWN CLEANUP] Cleaning up stale data for clientId " @ %clientId);

	// BUGFIX: %pobj was never assigned, so this entire stale-object cleanup
	// block was dead code - lingering player objects were leaked (ghost/shell bots)
	%pobj = Client::getOwnedObject(%clientId);
	if(%pobj != -1 && %pobj != "" && isObject(%pobj))
	{
		// (Redundant safeguards removed - handled by IsSafeToModify at top of function)
		
		// Get the AI name from stale data
		%aiName = $BotInfoAiName[%clientId];
		if(%aiName == "") %aiName = $EnemyBotData[%clientId, "BotInfoAiName"];
		if(%aiName == "") %aiName = $TownBotData[%clientId, "BotInfoAiName"];
		if(%aiName == "") %aiName = fetchData(%clientId, "BotInfoAiName");
		
		// Check for Enemy Bot status (SpawnBotInfo) to handle $numAI decrement
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		%isEnemyBot = (%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0");
		
		if(%aiName != "" && %aiName != -1 && %aiName != "0")
		{
			// CRITICAL FIX: Check if this bot name is actually on THIS clientId or a different one
			// If it returns a DIFFERENT clientId, the bot is ALIVE elsewhere - do NOT call
			// AI::delete() or it will kill the live bot!
			// Use AI::getClientIdFromName() (silent) instead of AI::getId() - dead AIs are
			// renamed to "Corpse<N> <name>" by the engine, so AI::getId() on a stale name
			// spams "Could not find drone" to the console
			%actualClientId = AI::getClientIdFromName(%aiName);
			
			if(%actualClientId == %clientId)
			{
				// Bot is on this clientId - safe to delete by name
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[PRE-SPAWN CLEANUP] Deleting bot via AI::delete: " @ %aiName @ " (clientId=" @ %clientId @ ")");
				
				// CRITICAL: Decrement $numAI if this is an Enemy Bot
				if(%isEnemyBot && $numAI > 0)
				{
					$numAI--;
					$Telemetry_NumAI_Dec++;
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %aiName);
				}
				
				AI::delete(%aiName);

				// AI::delete is a no-op if the engine no longer knows this name
				// (e.g. dead AI renamed to Corpse*) - ensure the stale object is gone
				if(isObject(%pobj))
				{
					deleteObject(%pobj);
					Client::setOwnedObject(%clientId, -1);
				}
			}
			else if(%actualClientId != -1 && %actualClientId != "" && %actualClientId != "False" && %actualClientId != "false")
			{
				// Bot is ALIVE on a different clientId - DON'T delete by name!
				// Just delete the player object on THIS clientId and clear the stale data
				echo("[PRE-SPAWN CLEANUP] WARNING: Bot " @ %aiName @ " is alive on clientId " @ %actualClientId @ ", NOT deleting by name. Clearing stale data from clientId " @ %clientId);
				
				// CRITICAL: Decrement $numAI if this is an Enemy Bot
				if(%isEnemyBot && $numAI > 0)
				{
					$numAI--;
					$Telemetry_NumAI_Dec++;
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %aiName);
				}
				
				deleteObject(%pobj);
				Client::setOwnedObject(%clientId, -1);
			}
			else
			{
				// Bot name not found in AI engine - delete player object directly
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[PRE-SPAWN CLEANUP] Bot " @ %aiName @ " not found in AI engine, deleting player object for clientId " @ %clientId);
				
				// CRITICAL: Decrement $numAI if this is an Enemy Bot
				if(%isEnemyBot && $numAI > 0)
				{
					$numAI--;
					$Telemetry_NumAI_Dec++;
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %aiName);
				}
				
				deleteObject(%pobj);
				Client::setOwnedObject(%clientId, -1);
			}
		}
		else
		{
			// Fallback: No AI name found, delete player object directly
			echo("[PRE-SPAWN CLEANUP] WARNING: No AI name found, using deleteObject fallback for clientId " @ %clientId);
			
			// CRITICAL: Decrement $numAI if this is an Enemy Bot
			if(%isEnemyBot && $numAI > 0)
			{
				$numAI--;
				$Telemetry_NumAI_Dec++;
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %clientId);
			}
			
			deleteObject(%pobj);
			Client::setOwnedObject(%clientId, -1);
		}
	}
	
	// Clear bot registry entry if exists
	if($BotRegistry[%clientId] != "" || $BotRegistry[%clientId, "team"] != "" || $BotRegistry[%clientId, "name"] != "")
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[PRE-SPAWN CLEANUP] Removing from bot registry");
		$BotRegistry[%clientId] = "";
		$BotRegistry[%clientId, "team"] = "";
		$BotRegistry[%clientId, "name"] = "";
	}
	
	// Remove from registry list
	%newList = "";
	for(%i = 0; GetWord($BotRegistryList, %i) != -1; %i++)
	{
		%id = GetWord($BotRegistryList, %i);
		if(%id != %clientId)
		{
			if(%newList == "")
				%newList = %id;
			else
				%newList = %newList @ " " @ %id;
		}
	}
	$BotRegistryList = %newList;
	
	// Clear all storeData entries
	storeData(%clientId, "BotInfoAiName", "");
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", "");
	storeData(%clientId, "botTeam", "");
	storeData(%clientId, "HasLoadedAndSpawned", "");
	storeData(%clientId, "DeathProcessed", "");
	storeData(%clientId, "ExpDistributed", "");
	storeData(%clientId, "RemortStep", "");
	storeData(%clientId, "QuestItems", "");
	storeData(%clientId, "KeyItems", "");
	storeData(%clientId, "Consumables", "");
	storeData(%clientId, "Armor", "");
	storeData(%clientId, "Accessories", "");
	storeData(%clientId, "Other", "");
	storeData(%clientId, "noExperienceFlag", "");
	storeData(%clientId, "noDropLootbagFlag", "");
	storeData(%clientId, "dumbAIflag", "");
	storeData(%clientId, "frozen", "");
	storeData(%clientId, "noBotSniff", "");
	storeData(%clientId, "SpellCastStep", "");
	storeData(%clientId, "LCKconsequence", "");
	storeData(%clientId, "AIattackMarker", "");
	storeData(%clientId, "AITarget", "");
	storeData(%clientId, "ShovedByPlayer", "");
	storeData(%clientId, "botAttackMode", "");
	storeData(%clientId, "zone", "");
	storeData(%clientId, "tmpzone", "");
	storeData(%clientId, "SpawnOriginZoneID", "");
	
	// PHASE 4: Clear frozen state
	$BotFrozen[%clientId] = "";
	
	// Clear data arrays
	$EnemyBotData[%clientId, "BotInfoAiName"] = "";
	$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
	$EnemyBotData[%clientId, "SpawnTime"] = "";
	$EnemyBotData[%clientId, "zone"] = "";
	$EnemyBotData[%clientId, "tmpzone"] = "";
	$EnemyBotData[%clientId, "SpawnOriginZoneID"] = "";
	$ClientData[%clientId, "BotInfoAiName"] = "";
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$TownBotData[%clientId, "BotInfoAiName"] = "";
	$TownBotData[%clientId, "SpawnBotInfo"] = "";
	
	// Clear direct array storage
	$BotInfoAiName[%clientId] = "";
	
	// Clear directive table
	$aidirectiveTable[%clientId, 99] = "";
	
	// Clear belt cache
	$Belt::CachedList[%clientId, "QuestItems"] = "";
	$Belt::CachedList[%clientId, "KeyItems"] = "";
	$Belt::CachedList[%clientId, "Consumables"] = "";
	$Belt::CachedList[%clientId, "Armor"] = "";
	$Belt::CachedList[%clientId, "Accessories"] = "";
	$Belt::CachedList[%clientId, "Other"] = "";
	
	// Clear spawn retry counters
	%aiName = fetchData(%clientId, "BotInfoAiName");
	if(%aiName != "" && %aiName != -1)
	{
		$EnemyBotSpawnRetry[%aiName] = "";
		$EnemyBotClientIdRetry[%aiName] = "";
		$SpawnAIScheduled[%aiName] = "";
		$Directive99RemovalAttempted[%aiName] = "";
	}
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[PRE-SPAWN CLEANUP] Cleanup complete for clientId " @ %clientId);
}

// ============================================================================
// BOT TYPE HELPER FUNCTIONS
// Consolidated functions to determine bot type
// ============================================================================

// HasEnemyBotNamePrefix: Check if a name starts with a known enemy bot prefix
// Centralized check based on NameForRace values in EnemyArmors.cs
// Returns: true if name starts with an enemy bot prefix, false otherwise
function HasEnemyBotNamePrefix(%name)
{
	if(%name == "" || %name == -1)
		return false;
	
	// Enemy bot prefixes from EnemyArmors.cs NameForRace values
	// Covers: Alien, Admin, Angel, Demon, God, Minotaur, Ogre, Orc, Pigman, Undead, Zombie, Seal, Enemy, Void
	if(String::findSubStr(%name, "Alien") == 0 || 
	   String::findSubStr(%name, "Admin") == 0 ||
	   String::findSubStr(%name, "Angel") == 0 ||
	   String::findSubStr(%name, "Demon") == 0 ||
	   String::findSubStr(%name, "God") == 0 ||
	   String::findSubStr(%name, "Minotaur") == 0 ||
	   String::findSubStr(%name, "Ogre") == 0 ||
	   String::findSubStr(%name, "Orc") == 0 ||
	   String::findSubStr(%name, "Pigman") == 0 ||
	   String::findSubStr(%name, "Undead") == 0 ||
	   String::findSubStr(%name, "Zombie") == 0 ||
	   String::findSubStr(%name, "Seal") == 0 ||
	   String::findSubStr(%name, "Enemy") == 0 ||
	   String::findSubStr(%name, "Void") == 0)
	{
		return true;
	}
	return false;
}

// Check if a client ID belongs to a town bot
function isTownBot(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return false;
	
	// CRITICAL SAFEGUARD: If player has a character save file, they are NOT a bot
	%playerNameCheck = Client::getName(%clientId);
	if(%playerNameCheck != "" && %playerNameCheck != -1)
	{
		%characterFile = "temp\\" @ %playerNameCheck @ ".cs";
		if(isFile(%characterFile))
			return false; // Real player with save file
	}
	
	// MOST RELIABLE: Check $TownBotSpawned registry
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
	{
		if($TownBotSpawned[%botName] == %clientId)
			return true;
	}
	
	// Check multiple data sources for BotInfoAiName
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
		%botInfoAiName = $TownBotData[%clientId, "BotInfoAiName"];
	if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
		%botInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
	
	// Check for TownBot_ prefix in name
	if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
	{
		if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
			return true;
	}
	
	// Check SpawnBotInfo - town bots should NOT have SpawnBotInfo
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	if(%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0")
		%spawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	
	// If has valid BotInfoAiName but no SpawnBotInfo, it's a town bot
	if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
	{
		if(%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0")
			return true;
	}
	
	return false;
}

// Check if a client ID belongs to an enemy bot
function isEnemyBot(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return false;
	
	// CRITICAL SAFEGUARD: If player has a character save file, they are NOT a bot
	%playerNameCheck = Client::getName(%clientId);
	if(%playerNameCheck != "" && %playerNameCheck != -1)
	{
		%characterFile = "temp\\" @ %playerNameCheck @ ".cs";
		if(isFile(%characterFile))
			return false; // Real player with save file
	}
	
	// Check if registered in bot registry (most reliable for enemy bots)
	if(IsBotRegistered(%clientId))
		return true;
	
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	
	// Enemy bots have SpawnBotInfo set (SpawnPoint, TempSpawn, or MarkerSpawn)
	if(%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0")
	{
		%spawnType = GetWord(%spawnBotInfo, 0);
		if(%spawnType == "SpawnPoint" || %spawnType == "TempSpawn" || %spawnType == "MarkerSpawn")
		{
			return true;
		}
	}
	
	// Fallback: Check display name patterns
	%playerName = Client::getName(%clientId);
	if(%playerName != "" && %playerName != -1)
	{
		// Check for enemy bot name patterns (at start of name)
		if(HasEnemyBotNamePrefix(%playerName))
		{
			return true;
		}
	}
	
	return false;
}

// Get the spawn point ID for a bot (returns "" if not found or not a spawn point bot)
function getBotSpawnPoint(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return "";
	
	// Priority 1: Check bot registry
	%spawnPoint = $BotRegistry[%clientId];
	if(%spawnPoint != "" && %spawnPoint != -1)
		return %spawnPoint;
	
	// Priority 2: Check fetchData
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
		return GetWord(%spawnBotInfo, 1);
	
	// Priority 3: Check $EnemyBotData
	%spawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
		return GetWord(%spawnBotInfo, 1);
	
	return "";
}

// ============================================================================
// CONSOLIDATED BOT CLEANUP
// Single function that handles ALL bot cleanup to ensure consistency
// ============================================================================

// function CleanupBot(%clientId, %aiName)
function CleanupBot(%clientId, %aiName)
{
	// Unified Safeguard: Check if safe to modify (handles Real Players vs Ghost Bots)
	if(!IsSafeToModify(%clientId, "CleanupBot"))
		return;

	
	echo("[BOT CLEANUP] Starting cleanup for clientId=" @ %clientId @ ", aiName=" @ %aiName);

	// review #7: capture the bot's classification AND its AI-name BEFORE Step 1 wipes
	// the markers. DecrementSpawnCounter -> UnregisterBot -> ClearAllBotData clears
	// SpawnBotInfo/BotInfoAiName/$BotType/$BotRegistry, after which isEnemyBot/isTownBot
	// fall back to a race-name-prefix check that FAILS for non-prefixed bots (Colloseum
	// "Round1", TempSpawn) - silently skipping the $numAI/$ActiveEnemyBots/
	// Telemetry_RecordDeath decrements (counter leak) and the BotInfoAiName fallback
	// used below to free the AI number.
	%isEnemy = isEnemyBot(%clientId);
	%isTown = isTownBot(%clientId);
	if(%aiName == "" || %aiName == -1)
		%aiName = fetchData(%clientId, "BotInfoAiName");

	// Step 1: Decrement spawn counter (uses centralized function with fallbacks)
	DecrementSpawnCounter(%clientId);

	// Step 2: Decrement tracking counters (classification captured above, pre-wipe)
	if(%isEnemy)
	{
		$ActiveEnemyBots--;
		$TotalActiveBots--;
		if($ActiveEnemyBots < 0) $ActiveEnemyBots = 0;
		if($TotalActiveBots < 0) $TotalActiveBots = 0;
		if($numAI > 0)
		{
			$numAI--;
			$Telemetry_NumAI_Dec++;  // Track $numAI decrements
		}
		Telemetry_RecordDeath();  // Track bot death processed
	}
	else if(%isTown)
	{
		$ActiveTownBots--;
		$TotalActiveBots--;
		if($ActiveTownBots < 0) $ActiveTownBots = 0;
		if($TotalActiveBots < 0) $TotalActiveBots = 0;
	}
	
	// Step 3: Free AI number
	if(%aiName == "" || %aiName == -1)
		%aiName = fetchData(%clientId, "BotInfoAiName");
	
	if(%aiName != "" && %aiName != -1 && %aiName != "0")
	{
		%aiNumber = getAInumberFromName(%aiName);
		if(%aiNumber != "" && %aiNumber != -1)
		{
			$aiNumTable[%aiNumber] = "";
			$tmpbotn[%aiName] = "";
		}
		
		// Clear spawn retry flags
		$EnemyBotSpawnRetry[%aiName] = "";
		$EnemyBotClientIdRetry[%aiName] = "";
		$SpawnAIScheduled[%aiName] = "";
		$Directive99RemovalAttempted[%aiName] = "";
	}
	
	// Step 4: Unregister from bot registry
	// PERF: DecrementSpawnCounter (Step 1) already calls UnregisterBot in BOTH of its
	// branches (spawn point found or not), which runs the full ClearAllBotData pass.
	// Calling it again here was a complete duplicate (~150 redundant storeData clears
	// per bot death). Removed.

	// Step 5: Clear all data storage
	// Use PreSpawnCleanup which also handles lingering player-object deletion
	PreSpawnCleanup(%clientId);
	
	// Step 5: Mark client ID as recently freed
	// Use validation token to prevent scheduled clear from executing on a real player who got the same clientId
	%validationToken = %aiName @ "_" @ getSimTime();
	if(%validationToken == "" || %validationToken == -1)
		%validationToken = "CleanupBot_" @ %clientId @ "_" @ getSimTime();
	$ClientIdRecentlyFreedToken[%clientId] = %validationToken;
	$ClientIdRecentlyFreed[%clientId] = getSimTime();
	schedule("if($ClientIdRecentlyFreedToken[" @ %clientId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %clientId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %clientId @ "] = \"\"; }", 30.0);
	
	echo("[BOT CLEANUP] Cleanup complete for clientId=" @ %clientId);
}

// Helper function to extract AI number from name
function getAInumberFromName(%aiName)
{
	if(%aiName == "" || %aiName == -1)
		return -1;
	
	// Extract trailing number from name (e.g., "Liquifier20" -> 20)
	%len = String::len(%aiName);
	%numStr = "";
	%digitString = "0123456789";
	
	for(%i = %len - 1; %i >= 0; %i--)
	{
		%char = String::getSubStr(%aiName, %i, 1);
		// Check if character is a digit (0-9 only) - use String::findSubStr to avoid TorqueScript == comparison bug ("E" == "0" evaluates to true)
		if(String::findSubStr(%digitString, %char) != -1)
		{
			%numStr = %char @ %numStr;
		}
		else
		{
			break;
		}
	}
	
	if(%numStr == "")
		return -1;
	
	return %numStr + 0;  // Convert to number
}

// RMRPG PATTERN: Direct array storage for AI names (fast lookup)
// $BotInfoAiName[%clientId] = %aiName;  // Stores AI name indexed by clientId
// This is set alongside storeData() for backward compatibility

//---------------------------------
// AI::detectSimRebase()
// The kronosfix_server time-fix DLL periodically rebases the sim clock BACKWARDS to keep it inside the
// high-precision float range. getSimTime() then jumps down, so any $ClientIdRecentlyFreed[] recorded
// before the rebase reads as "in the future" -> (getSimTime() - freed) goes NEGATIVE -> every ID-reuse
// guard thinks the id was "just freed" and aborts the spawn. Fix: when the clock jumps backwards, shift
// every stored freed-timestamp down by the same amount so the real "freed N seconds ago" deltas stay
// correct across the rebase (protection preserved, spawns continue). Call at each spawn entry point.
//---------------------------------
function AI::detectSimRebase()
{
	%now = getSimTime();
	if($AI_LastSimTime != "" && %now < ($AI_LastSimTime - 1))   // clock jumped back >1s = a DLL rebase
	{
		%shift = $AI_LastSimTime - %now;                         // how far the clock jumped backwards
		for(%i = 2048; %i <= 2175; %i++)                         // engine client id range = 0x800 + 0..127
			if($ClientIdRecentlyFreed[%i] != "")
				$ClientIdRecentlyFreed[%i] = $ClientIdRecentlyFreed[%i] - %shift;
		echo("[SIM REBASE] Sim clock rebased by " @ %shift @ "s; adjusted ID-reuse timers (spawns continue).");
	}
	$AI_LastSimTime = %now;
}

//---------------------------------
//createAI()
//---------------------------------
function createAI(%aiName, %markerGroup, %name, %skipPostSpawn, %bypassRaceCheck)
{
	AI::detectSimRebase();   // keep ID-reuse guards correct across the time-fix DLL's clock rebase
	// CRITICAL INTEGRATION: Server capacity check at lowest level
	// This provides defense-in-depth - even if higher-level callers skip the check
	%predictedId = PlayerManager::getFreeId();
	if(%predictedId == -1)
	{
		echo("CRITICAL: createAI - Server is FULL! Aborting spawn for " @ %aiName);
		return -1;
	}
	
	dbecho($dbechoMode, "createAI(" @ %aiName @ ", " @ %markerGroup @ ", " @ %name @ ", " @ %skipPostSpawn @ ")");
	//echo("[SPAWN DEBUG] createAI(): aiName=" @ %aiName @ ", markerGroup=" @ %markerGroup @ ", name=" @ %name);
	
	// Initialize %skipPostSpawn to false if not provided
	if(%skipPostSpawn == "")
		%skipPostSpawn = false;

	%group = nameToID( %markerGroup );
   
	if( %group == -1 || Group::objectCount(%group) == 0 )
	{
	      %spawnPos = %markerGroup;
	      %spawnRot = "0 0 0";
	      //echo("[SPAWN DEBUG] createAI(): Using markerGroup as position (no group found)");
	}
	else
	{
		for(%i = 0; %i < Group::objectCount(%group); %i++)
		{
			%obj = Group::getObject(%group, %i);
			if(getObjectType(%obj) != "SimGroup")
				break;
		}
	      %spawnMarker = Group::getObject(%group, %i);
	      %spawnPos = GameBase::getPosition(%spawnMarker);
	      %spawnRot = GameBase::getRotation(%spawnMarker);
	      //echo("[SPAWN DEBUG] createAI(): Using marker position from group");
	}

	// Parse guard type using helper
	%guardtype = Bot_ParseGuardType(%aiName);
	//echo("[SPAWN DEBUG] createAI(): guardtype=" @ %guardtype);

	if($BotInfo[%aiName, RACE] != "")
		%armor = $RaceToArmorType[$BotInfo[%aiName, RACE]];		//bots in map will get this call
	else
		%armor = $RaceToArmorType[$NameForRace[%guardtype]];		//spawn bots will get this call

	// Randomized Appearance Override
	if($BotRandomizeAppearance[%guardtype])
	{
		%list = $BotRandomArmorList[%guardtype];
		if(%list == "")
			%list = "OgreArmor PigmanArmor OrcArmor UndeadArmor TravellerArmor MaleElfArmor FemaleElfArmor MinotaurArmor AlienArmor ZombieArmor DemonArmor GodArmor AngelArmor VoidArmor";
		
		%count = 0;
		while(GetWord(%list, %count) != -1)
			%count++;
			
		if(%count > 0)
		{
			%idx = floor(getRandom() * %count);
			%armor = GetWord(%list, %idx);
			//echo("[SPAWN DEBUG] createAI(): Randomized appearance for " @ %aiName @ " (" @ %guardtype @ ") resolved to armor: " @ %armor);
		}
	}


	// Validate armor type was found
	if(%armor == "" || %armor == -1)
	{
		echo("ERROR: createAI - Could not find armor type for bot " @ %aiName @ " (guardtype: " @ %guardtype @ ")");
		echo("  $NameForRace[" @ %guardtype @ "] = '" @ $NameForRace[%guardtype] @ "'");
		echo("  $RaceToArmorType[" @ $NameForRace[%guardtype] @ "] = '" @ %armor @ "'");
		return -1;
	}

	// CRITICAL: Check if AI name already exists before spawning
	// If it exists but the player object is invalid (shell bot), clean it up first
	%existingId = AI::getClientIdFromName(%aiName);
	if(%existingId != -1 && %existingId != "" && %existingId != "False" && %existingId != "false")
	{
		%existingPlayerObj = Client::getOwnedObject(%existingId);
		if(%existingPlayerObj == -1 || %existingPlayerObj == "")
		{
			// AI name exists but player object is invalid - this is a shell bot
			// Clean it up before spawning a new one
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): WARNING - AI name '" @ %aiName @ "' already exists but player object is invalid (shell bot). Cleaning up before respawn...");
			%escapedName = String::replace(%aiName, "\"", "\\\"");
			AI::delete(%escapedName);
			// Run PreSpawnCleanup to ensure all stale data is cleared
			// NOTE: no "wait" is possible here (schedule("") is a no-op) - if the engine
			// hasn't finished the delete, the AI::spawn below fails and the "already
			// exists" handler defers a retry
			PreSpawnCleanup(%existingId);
		}
		else
		{
			// Bot already exists with valid player object - don't spawn again
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): WARNING - Bot " @ %aiName @ " already exists with valid player object (clientId=" @ %existingId @ "). Skipping duplicate spawn.");
			return -1;
		}
	}
	
	// CRITICAL: Before spawning, check if any client IDs have stale player objects
	// This prevents shell bots from forming when a bot dies and a new one spawns quickly
	// Uses unified helper function Bot_CleanupStaleIds
	%cleanupPerformed = Bot_CleanupStaleIds();
	
	// CRITICAL PRE-FLIGHT CHECK: Delay spawn if ANY player is actively connecting
	// This prevents race conditions where AI::spawn might assign a client ID that's about to be used by a connecting player
	%hasActiveConnection = false;
	for(%checkId = Client::getFirst(); %checkId != -1; %checkId = Client::getNext(%checkId))
	{
		%connectTime = $ClientIdPlayerConnecting[%checkId];
		if(%connectTime != "" && %connectTime != "0" && %connectTime != -1)
		{
			%timeSinceConnect = getSimTime() - %connectTime;
			if(%timeSinceConnect < 10)  // Connection within last 10 seconds
			{
				%hasActiveConnection = true;
				echo("CRITICAL SAFEGUARD: createAI - Player actively connecting (clientId=" @ %checkId @ ", " @ %timeSinceConnect @ "s ago). Delaying bot spawn for " @ %aiName);
				break;
			}
			else
			{
				// Stale connection flag - clear it
				$ClientIdPlayerConnecting[%checkId] = "";
			}
		}
	}
	
	if(%hasActiveConnection)
	{
		// Delay this spawn by 2 seconds to let player connection complete
		// CRITICAL FIX: Pass original arguments matching createAI signature:
		// createAI(%aiName, %markerGroup, %name, %skipPostSpawn, %bypassRaceCheck)
		// Note: %markerGroup was originally passed in, can be position string or marker group name
		// We pass %spawnPos since we've already resolved it from %markerGroup
		
		// CRITICAL FIX: If this is a SpawnPoint bot (skipPostSpawn=true), store context so
		// the deferred retry can call SpawnAIGetClientId after spawn succeeds
		// This prevents ghost bots that have no BotInfoAiName or tracking
		if(%skipPostSpawn == true || %skipPostSpawn == "true" || %skipPostSpawn == "1")
		{
			$DeferredSpawnIsSpawnPoint[%aiName] = true;
			$DeferredSpawnDisplayName[%aiName] = %name;
			$DeferredSpawnPos[%aiName] = %spawnPos;
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): Deferred spawn for SpawnPoint bot " @ %aiName @ " - stored context for SpawnAIGetClientId");
		}
		
		schedule("createAI(\"" @ %aiName @ "\", \"" @ %spawnPos @ "\", \"" @ %name @ "\", " @ %skipPostSpawn @ ", " @ %bypassRaceCheck @ ");", 2.0);
		return "deferred";
	}
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] createAI(): Calling AI::spawn(" @ %aiName @ ", " @ %armor @ ", " @ %spawnPos @ ", " @ %spawnRot @ ", " @ %name @ ")");
	
	// =========================================================================================================
	// CRITICAL PRE-SPAWN PROTECTION: Cache all connected player client IDs and names BEFORE AI::spawn()
	// After spawn, we check if the bot got a client ID that was in this list with a save file
	// This catches collisions that IsRealPlayer() can't detect (because engine already overwrote the player)
	// =========================================================================================================
	%preSpawnPlayerList = "";
	for(%pCl = Client::getFirst(); %pCl != -1; %pCl = Client::getNext(%pCl))
	{
		%pName = Client::getName(%pCl);
		if(%pName != "" && %pName != -1)
		{
			// Check if this client has a save file (indicating a real player)
			if($PlayerHasSaveFile[%pCl] == true || $PlayerHasSaveFile[%pCl] == "1" || isFile("temp\\" @ %pName @ ".cs"))
			{
				// Store the client ID and name for post-spawn verification
				$PreSpawnPlayerName[%pCl] = %pName;
				%preSpawnPlayerList = %preSpawnPlayerList @ %pCl @ " ";
			}
		}
	}
	
	// =========================================================================================================
	// CRITICAL PRE-SPAWN PROTECTION: Block spawn if predicted ID belongs to a real player
	// This prevents the engine from assigning a zombie player's client ID to a bot, which causes black screens
	// The post-spawn check was too late - by then the engine had already corrupted the player's session
	// =========================================================================================================
	%predictedSpawnId = PlayerManager::getFreeId();
	if(%predictedSpawnId != -1)
	{
		// Check if this ID has a save file flag (real player, possibly zombie/disconnecting)
		if($PlayerHasSaveFile[%predictedSpawnId] == true || $PlayerHasSaveFile[%predictedSpawnId] == "1")
		{
			// Track defer count to prevent infinite loops
			$DeferredSpawnRetryCount[%aiName]++;
			if($DeferredSpawnRetryCount[%aiName] > 5)
			{
				echo("[SPAWN ABORT] Max defer retries (5) exceeded for " @ %aiName @ " - predicted ID " @ %predictedSpawnId @ " still has PlayerHasSaveFile. Aborting spawn.");
				// Clear the stale save file flag since it's clearly orphaned
				$PlayerHasSaveFile[%predictedSpawnId] = "";
				$DeferredSpawnRetryCount[%aiName] = "";
				return "-1_MAX_RETRIES";
			}
			
			echo("[PRE-SPAWN BLOCKED] Predicted ID " @ %predictedSpawnId @ " has PlayerHasSaveFile set (zombie player detected). Deferring spawn of " @ %aiName @ " (retry " @ $DeferredSpawnRetryCount[%aiName] @ "/5)");
			schedule("createAI(\"" @ %aiName @ "\", \"" @ %spawnPos @ "\", \"" @ %name @ "\", " @ %skipPostSpawn @ ", " @ %bypassRaceCheck @ ");", 2.0);
			return "deferred_player_zombie";
		}
		else
		{
			// Predicted ID is safe - clear any stale retry count
			$DeferredSpawnRetryCount[%aiName] = "";
		}
	}
	
	// =========================================================================================================
	// ZOMBIE CLIENT DETECTION (WARNING ONLY - does not block spawns)
	// The full-range scan was too aggressive - blocking ALL spawns if ANY zombie existed
	// Now we just log if a zombie is detected, but rely on post-spawn IsSafeToModify() for protection
	// =========================================================================================================
	// Note: Zombie detection is handled in IsSafeToModify() which protects against modifying zombie IDs
	// after spawn. Pre-spawn blocking caused infinite defer loops when zombies persisted.
	
	%spawnResult = AI::spawn( %aiName, %armor, %spawnPos, %spawnRot, %name, "male2" );
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] createAI(): AI::spawn() returned: " @ %spawnResult);
	
	// CRITICAL: Handle "already exists" error from AI::spawn()
	// This can happen if the cleanup above didn't complete in time
	if( %spawnResult == "false" )
	{
		// Check if the error was "already exists"
		%existingId2 = AI::getClientIdFromName(%aiName);
		if(%existingId2 != -1 && %existingId2 != "" && %existingId2 != "False" && %existingId2 != "false")
		{
			// BUGFIX: schedule("", 0.2) does NOT pause execution (TorqueScript has no sleep),
			// so the old synchronous retry ran before the engine processed AI::delete and
			// failed the same way. Defer a full createAI retry instead, reusing the same
			// deferred-spawn context the player-connecting path uses so SpawnPoint bots
			// still get SpawnAIGetClientId registration on the retry.
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): AI::spawn() failed - AI name exists. Deleting and deferring retry 0.5s...");
			%escapedName = String::replace(%aiName, "\"", "\\\"");
			AI::delete(%escapedName);

			if(%skipPostSpawn == true || %skipPostSpawn == "true" || %skipPostSpawn == "1")
			{
				$DeferredSpawnIsSpawnPoint[%aiName] = true;
				$DeferredSpawnDisplayName[%aiName] = %name;
				$DeferredSpawnPos[%aiName] = %spawnPos;
			}

			schedule("createAI(\"" @ %aiName @ "\", \"" @ %spawnPos @ "\", \"" @ %name @ "\", " @ %skipPostSpawn @ ", " @ %bypassRaceCheck @ ");", 0.5);
			return "deferred";
		}
	}
	
	if( %spawnResult != "false" )
	{
		// Attempt to immediately tag invulnerability via display name if clientId is already resolvable
		%preClient = NEWgetClientByName(%name);
		
		// CRITICAL: Set $BotType IMMEDIATELY to prevent GetClientDataType() confusion
		// This closes the race window between spawn and RegisterBot()
		if(%preClient != -1 && %preClient != "" && %preClient != "False")
		{
			$BotType[%preClient] = "enemy";
		}
		
		// =========================================================================================================
		// RACE CONDITION FIX
		// prevent immediate reuse of Client IDs that were just freed (unless bypassed by critical systems)
		// =========================================================================================================
		if(!%bypassRaceCheck && %preClient != -1 && $ClientIdRecentlyFreed[%preClient] != "" && (getSimTime() - $ClientIdRecentlyFreed[%preClient] < 3))
		{
			 echo("[SPAWN DANGER] ID REUSE DETECTED! Client " @ %preClient @ " was freed recently. Aborting spawn of " @ %aiName @ " to prevent collision.");
			 %escapedName = String::replace(%aiName, "\"", "\\\"");
			 AI::delete(%escapedName);
			 return "-1_RACE_CONDITION"; // Return specific error code to trigger rollback
		}
		// =========================================================================================================
		
		// =========================================================================================================
		// CRITICAL: PROTECT ALREADY-CONNECTED PLAYERS
		// If AI::spawn assigned a client ID that belongs to a real player, abort immediately
		// This prevents black screens caused by player objects being overwritten by bot spawn
		// 
		// We use BOTH the pre-spawn cache AND IsRealPlayer() for maximum protection:
		// 1. Pre-spawn cache catches cases where engine overwrote the player (IsRealPlayer fails after)
		// 2. IsRealPlayer catches edge cases not covered by the cache
		// =========================================================================================================
		if(%preClient != -1 && %preClient != "" && %preClient != "False" && %preClient != "false")
		{
			// CHECK 1: Pre-spawn cache - most reliable for detecting collision AFTER engine overwrote player
			%cachedPlayerName = $PreSpawnPlayerName[%preClient];
			if(%cachedPlayerName != "" && %cachedPlayerName != %name && %cachedPlayerName != %aiName)
			{
				// This client ID had a REAL PLAYER with save file before spawn, but now has the bot's name
				echo("[CRITICAL BLOCKED] AI::spawn assigned client ID " @ %preClient @ " which HAD real player '" @ %cachedPlayerName @ "' before spawn! Aborting spawn of " @ %aiName @ " to prevent black screen.");
				%escapedName = String::replace(%aiName, "\"", "\\\"");
				AI::delete(%escapedName);
				// Clear the cache entry
				$PreSpawnPlayerName[%preClient] = "";
				return "-1_PLAYER_COLLISION";
			}
			// Clear the cache entry (no longer needed)
			$PreSpawnPlayerName[%preClient] = "";
			
			// CHECK 2: IsRealPlayer - backup check for edge cases
			if(IsRealPlayer(%preClient))
			{
				echo("[CRITICAL BLOCKED] AI::spawn assigned client ID " @ %preClient @ " which belongs to a REAL PLAYER! Aborting spawn of " @ %aiName @ " to prevent black screen.");
				%escapedName = String::replace(%aiName, "\"", "\\\"");
				AI::delete(%escapedName);
				return "-1_PLAYER_COLLISION";
			}
		}
		// =========================================================================================================

		if(%preClient != -1 && %preClient != "" && %preClient != "False" && %preClient != "false")
		{
			storeData(%preClient, "SpawnInvuln", True);
			$SpawnInvulnByName[%name] = getSimTime();
		}
		else
		{
			// Mark by name so damage guard can use it before clientId is registered
			$SpawnInvulnByName[%name] = getSimTime();
		}
		
		// Only schedule createAIPostSpawn() for pre-placed bots (not SpawnPoint bots)
		// SpawnPoint bots are handled entirely by SpawnAI() and SpawnAIGetClientId()
		if(!%skipPostSpawn)
		{
			// Add a delay to ensure bot is fully initialized before getting ID
			// Increased from 0.1s to 0.5s to give engine more time to initialize Player object
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): SUCCESS - scheduling createAIPostSpawn() in 0.5s");
			schedule("createAIPostSpawn(\"" @ %aiName @ "\", \"" @ %armor @ "\", " @ %group @ ");", 0.5);
		}
		else
		{
			// SpawnPoint bot - normally handled by SpawnAI() flow
			// But if this was a deferred spawn retry, we need to call SpawnAIGetClientId ourselves
			if($DeferredSpawnIsSpawnPoint[%aiName] == true)
			{
				// This was a deferred SpawnPoint spawn - call SpawnAIGetClientId to properly register it
				%deferredDisplayName = $DeferredSpawnDisplayName[%aiName];
				%deferredSpawnPos = $DeferredSpawnPos[%aiName];
				%deferredCommandIssuer = $DeferredSpawnCommandIssuer[%aiName];
				
				// Clear the deferred context
				$DeferredSpawnIsSpawnPoint[%aiName] = "";
				$DeferredSpawnDisplayName[%aiName] = "";
				$DeferredSpawnPos[%aiName] = "";
				$DeferredSpawnCommandIssuer[%aiName] = "";
				
				// Extract spawnPointId from commandIssuer if present
				%deferredSpawnPointId = "";
				if(GetWord(%deferredCommandIssuer, 0) == "SpawnPoint")
					%deferredSpawnPointId = GetWord(%deferredCommandIssuer, 1);
				
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
					echo("[SPAWN FLOW] createAI(): Deferred SpawnPoint spawn SUCCESS - scheduling SpawnAIGetClientId for " @ %aiName @ " (commandIssuer=" @ %deferredCommandIssuer @ ", spawnPointId=" @ %deferredSpawnPointId @ ")");
				
				// Schedule SpawnAIGetClientId with the stored context including commandIssuer and spawnPointId
				schedule("SpawnAIGetClientId(\"" @ %aiName @ "\", \"" @ %deferredDisplayName @ "\", \"" @ %deferredSpawnPos @ "\", \"" @ %deferredCommandIssuer @ "\", \"\", \"" @ %deferredSpawnPointId @ "\", \"\");", 0.5);
			}
			else
			{
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
					echo("[SPAWN FLOW] createAI(): SUCCESS - skipping createAIPostSpawn() (SpawnPoint bot)");
			}
		}
		return %aiName;  // Return the AI name on success
	}
	else
	{
      	//echo("[SPAWN DEBUG] createAI(): FAILED - AI::spawn() returned false");
      	dbecho(1, "Failure spawning bot:");
		dbecho(1, "%aiName: " @ %aiName);
		dbecho(1, "%armor: " @ %armor);
		dbecho(1, "%guardtype: " @ %guardtype);
		dbecho(1, "%spawnPos: " @ %spawnPos);
		dbecho(1, "%spawnRot: " @ %spawnRot);
		dbecho(1, "%name: " @ %name);
		return -1;
      }
}

//---------------------------------
//createAIPostSpawn()
//---------------------------------
// Post-spawn initialization function to ensure bot is fully initialized
// NOTE: This is ONLY for pre-placed bots in the map, NOT for SpawnPoint bots
function createAIPostSpawn(%aiName, %armor, %group)
{
	//echo("[SPAWN DEBUG] createAIPostSpawn(): Called for " @ %aiName @ " (armor=" @ %armor @ ")");
	// CRITICAL: AI::spawn() creates AIObj and registers it in AIManager immediately
	// So AI::getId() SHOULD work, but try getClientIdFromName first (no errors)
	// Try AI::getClientIdFromName() first (works for both Drones and Player objects, no error messages)
	%AiId = AI::getClientIdFromName(%aiName);
	
	// If getClientIdFromName failed, bot may not exist (died before scheduled call)
	// Don't call AI::getId() as it generates "Could not find drone" errors for non-existent bots
	// If getClientIdFromName returns -1/empty/False, the bot likely doesn't exist anymore
	if(%AiId == -1 || %AiId == "" || %AiId == "False" || %AiId == "false")
	{
		// Bot doesn't exist - likely died before scheduled call executed
		// Silently return without error messages
		return;
	}
	
	// Validate that we got a valid ID
	if(%AiId == -1 || %AiId == "" || %AiId == "False" || %AiId == "false")
	{
		// Bot may have died between the check and now - silently return
		return;
	}
	
	// CRITICAL: Verify this client ID does not belong to a real player
	// This catches any delayed collisions where AI::spawn assigned a player's ID
	if(IsRealPlayer(%AiId))
	{
		echo("[CRITICAL BLOCKED] createAIPostSpawn: Client ID " @ %AiId @ " belongs to a REAL PLAYER! Aborting post-spawn for " @ %aiName @ " to prevent black screen.");
		// Clean up the bot if it exists
		%escapedName = String::replace(%aiName, "\"", "\\\"");
		AI::delete(%escapedName);
		return;
	}
	
	// VALIDATE: Check if player object actually exists
	%playerObj = Client::getOwnedObject(%AiId);
	%playerName = Client::getName(%AiId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// DEBUG: Log when createAIPostSpawn is called for a bot with no player object
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] createAIPostSpawn(): WARNING - Bot " @ %aiName @ " (clientId=" @ %AiId @ ") has NO player object! Display name: '" @ %playerName @ "'. This may become a shell bot.");
		// Bot may have died - silently return without cleanup attempts
		return;
	}
	else
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] createAIPostSpawn(): Bot " @ %aiName @ " (clientId=" @ %AiId @ ") has valid player object. Display name: '" @ %playerName @ "'");
	}
	
	// CRITICAL: Check for ghost client ID (empty name Player object)
	// This happens when the engine creates a Player object but the name isn't set yet
	// BUT: We need to verify this is actually a ghost and not just a bot whose name hasn't been set yet
	%botName = Client::getName(%AiId);
	if(%botName == "" || %botName == -1)
	{
		// Name is empty - check if this AI name is already in use by another active bot
		// If another bot with this AI name exists and has a valid name, then this is a ghost
		%otherBotId = AI::getClientIdFromName(%aiName);
		if(%otherBotId != -1 && %otherBotId != "" && %otherBotId != %AiId)
		{
			%otherBotName = Client::getName(%otherBotId);
			if(%otherBotName != "" && %otherBotName != -1)
			{
				// Another bot with this AI name exists and has a valid name - this is a ghost
				echo("WARNING: createAIPostSpawn - Detected ghost client ID " @ %AiId @ " (empty name) for AI " @ %aiName @ ". Another bot with this AI name exists (clientId=" @ %otherBotId @ ", name='" @ %otherBotName @ "'). Cleaning up ghost...");
				CleanupGhostClientId(%AiId, %aiName);
				return;
			}
		}
		
		// Name is empty but no other bot with this AI name exists
		// This might be a bot whose name hasn't been set yet - wait a bit and check again
		// Schedule a delayed check instead of immediately cleaning up
		schedule("CheckGhostClientIdDelayed(" @ %AiId @ ", \"" @ %aiName @ "\");", 0.5);
		// Continue processing - if it's truly a ghost, the delayed check will clean it up
	}
	
	// CRITICAL: Skip this function if bot has SpawnBotInfo (it's a SpawnPoint bot)
	// SpawnPoint bots are handled entirely by SpawnAI() and should not be modified here
	%spawnBotInfo = fetchData(%AiId, "SpawnBotInfo");
	//echo("[SPAWN DEBUG] createAIPostSpawn(): bot " @ %aiName @ " has SpawnBotInfo='" @ %spawnBotInfo @ "'");
	// Check for both empty string and "0" string (some functions may set it to "0" instead of clearing it)
	if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
	{
		//echo("[SPAWN DEBUG] createAIPostSpawn(): SKIPPED - bot " @ %aiName @ " is a SpawnPoint bot (SpawnBotInfo='" @ %spawnBotInfo @ "')");
		return;
	}
	
	// CRITICAL: Verify this is actually a town bot before processing
	// Use centralized IsEnemyBot() helper function
	if(IsEnemyBot(%AiId))
	{
		%botInfoExists = "no";
		if($BotInfo[%aiName, ITEMS] != "")
			%botInfoExists = "yes";
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] createAIPostSpawn(): SKIPPED - bot " @ %aiName @ " is an enemy bot (displayName='" @ %displayName @ "', team=" @ %botTeam @ ", BotInfo exists=" @ %botInfoExists @ ")");
		return;
	}
	
	//echo("[SPAWN DEBUG] createAIPostSpawn(): Processing town bot " @ %aiName);
	ClearVariables(%AiId);

	storeData(%AiId, "BotInfoAiName", %aiName);
	$BotInfoAiName[%AiId] = %aiName;  // Also store in direct array for fast lookup
	
	// CRITICAL: Clear SpawnBotInfo to prevent town bots from being mistaken for enemy bots
	// Town bots should NOT have SpawnBotInfo set (only enemy bots have it)
	// This prevents zone change detection from killing town bots
	storeData(%AiId, "SpawnBotInfo", "");
	storeData(%AiId, "SpawnTime", ""); // Also clear SpawnTime to be safe
	
	// CRITICAL: Increment town bot tracking counters
	$ActiveTownBots++;
	$TotalActiveBots++;
	
	// Add to TownBotList for tracking
	if($TownBotList == "")
		$TownBotList = %AiId;
	else
		$TownBotList = $TownBotList @ " " @ %AiId;
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT TRACK] Town bot spawned: " @ %aiName @ " (clientId=" @ %AiId @ ") | Total Town: " @ $ActiveTownBots @ " | Total All: " @ $TotalActiveBots);
	
	storeData(%AiId, "RACE", $ArmorTypeToRace[%armor]);
	storeData(%AiId, "LCKconsequence", "miss");
	storeData(%AiId, "RemortStep", 0);
	storeData(%AiId, "HasLoadedAndSpawned", True);
	storeData(%AiId, "botAttackMode", 1);
	storeData(%AiId, "tmpbotdata", "");

	storeData(%AiId, "HP", fetchData(%AiId, "MaxHP"));
	storeData(%AiId, "MANA", 1000);

	refreshHPREGEN(%AiId);
	refreshMANAREGEN(%AiId);

	// Set LCK from BotInfo, or default to 0 if not set
	%luck = $BotInfo[%aiName, LCK];
	if(%luck == "" || %luck == -1)
		%luck = 0;
	storeData(%AiId, "LCK", %luck);
	
	// Set LCKconsequence to "miss" for enemy bots so LCK protection works
	// This allows bots to lose LCK instead of dying when they would be killed
	storeData(%AiId, "LCKconsequence", "miss");

	// AI mobs always use "Normal" stance
	storeData(%AiId, "Stance", "Normal");

	if(%group != -1)
	{
		// The order number is used for sorting waypoints, and other directives.  
		%orderNumber = 200;
     
		for(%i = 0; %i < Group::objectCount(%group); %i++)
		{
			%spawnMarker = Group::getObject(%group, %i);
			if(getObjectType(%spawnMarker) != "SimGroup")
			{
				%spawnPos = GameBase::getPosition(%spawnMarker);
       
				AI::DirectiveWaypoint( %aiName, %spawnPos, %orderNumber );
       
				%orderNumber++;
			}
		}

		AI::setAutomaticTargets(%aiName);
	}

	GameBase::startFadeIn(%AiId);
	%spawnPos = GameBase::getPosition(%AiId);
	PlaySound(SoundSpawn2, %spawnPos);
	
	// DEFENSIVE FIX: Schedule a second startFadeIn to ensure visibility
	// Fixes intermittent invisibility that can occur during spawn
	schedule("if(isObject(" @ %AiId @ ")) GameBase::startFadeIn(" @ %AiId @ ");", 0.5);
}

//----------------------------------
// AI::setupAI()
//
// Called from Mission::init() which is defined in Objectives.cs (or Dm.cs for
//    deathmatch missions).  
//----------------------------------   
function AI::setupAI(%key, %team)
{
	dbecho($dbechoMode, "AI::setupAI(" @ %key @ ", " @ %team @ ")");

	//if there is no key then they don't exist yet
	if(%key == "")
	{
		%aiFound = 0;
		for( %T = 0; %T < 13; %T++ )
		{
			%groupId = nameToID("MissionGroup\\Teams\\team" @ %T @ "\\AI" );
			if( %groupId != -1 )
			{
				%teamItemCount = Group::objectCount(%groupId);
				if( %teamItemCount > 0 )
				{
					AI::initDrones(%T, %teamItemCount);
					%aiFound += %teamItemCount;
				}
			}
		}
		if( %aiFound == 0 )
			dbecho(1, "No drones exist...");
		else
			dbecho(1, %aiFound @ " drones installed..." );

		$numAi = %aiFound;
		
		// NOTE: Periodic cleanup systems (StartAINumberReconciliation, StartGhostBotCleanup)
		// are started from Server.cs in createServer() alongside other periodic systems
	}
	else     //respawning dead AI with original name and path
	{
		%group = nameToID("MissionGroup\\Teams\\team" @ %team @ "\\AI\\" @ %key);
		%num = Group::objectCount(%group);
		createAI(%key, %group, $BotInfo[%key, NAME]);
		%aiId = AI::getId(%key);
		
		// Validate that we got a valid ID
		if(%aiId == -1 || %aiId == "")
		{
			echo("ERROR: AI::setupAI - AI::getId failed for " @ %key @ ". Bot may be broken.");
			return;
		}

		GameBase::setTeam(%aiId, %team);
		AI::setVar(%key, pathType, $AI::defaultPathType);
		AI::setWeapons(%key);

		//**RPG (added because AI::onDroneKilled doesn't conserve the AI's team)
		storeData(%aiId, "botTeam", %team);
		//**
	}		
}


//-----------------------------------
// AI::initDrones()
//-----------------------------------
function AI::initDrones(%team, %numAi)
{
	dbecho($dbechoMode, "AI::initDrones(" @ %team @ ", " @ %numAi @ ")");

	dbecho(1, "spawning team " @ %team @ " ai...");
	for(%guard = 0; %guard < %numAi; %guard++)
	{
		//check for internal data
		%tempSet = nameToID("MissionGroup\\Teams\\team" @ %team @ "\\AI");
		%tempItem = Group::getObject(%tempSet, %guard);
		%aiName = Object::getName(%tempItem);

		%set = nameToID("MissionGroup\\Teams\\team" @ %team @ "\\AI\\" @ %aiName);
		%numPts = Group::objectCount(%set);

		if(%numPts > 0)
		{
			GatherBotInfo(%set);

			createAI(%aiName, %set, $BotInfo[%aiName, NAME]);
			%aiId = AI::getId( %aiName );
			
			// Validate that we got a valid ID
			if(%aiId == -1 || %aiId == "")
			{
				echo("ERROR: AI::initDrones - AI::getId failed for " @ %aiName @ ". Bot may be broken.");
				continue;
			}
			
			AI::setVar( %aiName, iq,  100 );
			AI::setVar( %aiName, attackMode, $AIattackMode);
			AI::setVar( %aiName, pathType, $AI::defaultPathType);
			AI::setWeapons(%aiName);

			UpdateTeam(%aiId);

			//**RPG (added because AI::onDroneKilled doesn't conserve the AI's team)
			storeData(%aiId, "botTeam", %team);
			//**
		}
		else
			dbecho(1, "no info to spawn ai...");
	}
}


//------------------------------------------------------------------
//functions to test and move AI players.
//
//------------------------------------------------------------------

$numAI = 0;

// Helper function to get client ID after a delay (allows Player object to register)
// Helper to find a player object for a client ID by searching BotGroup
// This circumvents the engine bug where Client::getOwnedObject() returns -1
function FindPlayerInBotGroup(%clientId)
{
	if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] FindPlayerInBotGroup: Called for clientId=" @ %clientId);
	
	// CRITICAL: Check if this is a shell bot (bot with no player object) before searching
	// This prevents unnecessary searches and reduces debug spam
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%hasBotData = ((%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0") || 
	               (%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0"));
	
	// If this is a bot (has bot data) but Client::getOwnedObject() returns -1, it's likely a shell bot
	%playerObj = Client::getOwnedObject(%clientId);
	if(%hasBotData && (%playerObj == -1 || %playerObj == ""))
	{
		// Shell bot detected - return -1 immediately to prevent unnecessary BotGroup search
		// THROTTLE: Only log once per 30 seconds per clientId to prevent log spam
		%currentTime = getSimTime();
		%lastShellLog = $FindPlayerInBotGroup_ShellLog[%clientId];
		if(%lastShellLog == "" || %lastShellLog == -1 || (%currentTime - %lastShellLog) >= 30)
		{
			if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) 
				echo("[INERT DEBUG] FindPlayerInBotGroup: Shell bot detected (clientId=" @ %clientId @ ", BotInfoAiName='" @ %botInfoAiName @ "', no player object) - returning -1");
			$FindPlayerInBotGroup_ShellLog[%clientId] = %currentTime;
		}
		return -1;
	}
	
	if(!isObject(BotGroup))
	{
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] FindPlayerInBotGroup: BotGroup does not exist - returning -1");
		return -1;
	}
	
	%count = Group::objectCount(BotGroup);
	if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] FindPlayerInBotGroup: BotGroup exists with " @ %count @ " objects");
	
	for(%i = 0; %i < %count; %i++)
	{
		%obj = Group::getObject(BotGroup, %i);
		%objClientId = Player::getClient(%obj);
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] FindPlayerInBotGroup: Checking object " @ %i @ " (obj=" @ %obj @ ", clientId=" @ %objClientId @ ")");
		if(%objClientId == %clientId)
		{
			if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] FindPlayerInBotGroup: Found match! Returning obj=" @ %obj);
			return %obj;
		}
	}
	if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] FindPlayerInBotGroup: No match found for clientId=" @ %clientId @ " - returning -1");
	return -1;
}

// ============================================================================
// SPAWN AI HELPER FUNCTIONS
// Consolidated helpers to reduce duplication in SpawnAIGetClientId
// ============================================================================

// Helper function to extract guard type from AI name by removing trailing digits
// Replaces logic previously inline in createAI
function Bot_ParseGuardType(%aiName)
{
	// CONSOLIDATED: delegates to StripTrailingDigits (rpgfunk.cs) - the previous
	// private loop was character-identical to it
	%guardtype = StripTrailingDigits(%aiName);

	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] Bot_ParseGuardType(): Extracted guardtype='" @ %guardtype @ "' from name='" @ %aiName @ "'");

	return %guardtype;
}

// Helper function to clean up stale client IDs before spawning a new bot
// Replaces the massive "Stale ID Cleanup" block in createAI
function Bot_CleanupStaleIds()
{
	%cleanupPerformed = false;
	
	// Tier 1: Use Client::getFirst()/getNext() (most reliable method)
	for(%checkId = Client::getFirst(); %checkId != -1; %checkId = Client::getNext(%checkId))
	{
		%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
		if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
		{
			// UNIFIED SAFEGUARD: Verify this is a bot (handles Real Players vs Ghost Bots)
			if(IsSafeToModify(%checkId, "createAI Stale Cleanup"))
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				
				// If freed less than 2 seconds ago, check if player object still exists
				if(%timeSinceFreed < 2 && %timeSinceFreed >= 0)
				{
					%stalePlayerObj = Client::getOwnedObject(%checkId);
					if(%stalePlayerObj != -1 && %stalePlayerObj != "" && isObject(%stalePlayerObj))
					{
						// Use consolidated cleanup helper
						%staleAiName = fetchData(%checkId, "BotInfoAiName");
						if(Spawn_CleanupStaleClientId(%checkId, %staleAiName))
							%cleanupPerformed = true;
					}
				}
			}
		}
	}
	
	// Tier 2: Range loop fallback (allocatable BaseRep range; 2048 = server slot, skipped)
	// - only if Tier 1 didn't find any recently freed IDs
	if(!%cleanupPerformed)
	{
		for(%checkId = $BaseRepClientIdMin + 1; %checkId <= $BaseRepClientIdMax; %checkId++)
		{
			%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				// SAFEGUARD: Verify this is actually a bot, not a real player
				%checkName = Client::getName(%checkId);
				if(%checkName != "" && %checkName != -1)
				{
					%characterFile = "temp\\" @ %checkName @ ".cs";
					if(isFile(%characterFile))
						continue; // Real player with save file - skip
				}
				
				// Additional safeguard: Check if it's marked as a bot
				%botInfoAiName = fetchData(%checkId, "BotInfoAiName");
				%spawnBotInfo = fetchData(%checkId, "SpawnBotInfo");
				%isLikelyBot = false;
				if(%botInfoAiName != "" && %botInfoAiName != -1)
					%isLikelyBot = true;
				else if(%spawnBotInfo != "" && %spawnBotInfo != -1)
					%isLikelyBot = true;
				else if($BotRegistry[%checkId] != "" && $BotRegistry[%checkId] != -1)
					%isLikelyBot = true;
				
				if(%isLikelyBot)
				{
					%currentTime = getSimTime();
					%timeSinceFreed = %currentTime - %recentlyFreed;
					
					// If freed less than 2 seconds ago, check if player object still exists
					if(%timeSinceFreed < 2 && %timeSinceFreed >= 0)
					{
						%stalePlayerObj = Client::getOwnedObject(%checkId);
						if(%stalePlayerObj != -1 && %stalePlayerObj != "" && isObject(%stalePlayerObj))
						{
							// CRITICAL: Final verification immediately before deletion
							// BUGFIX: isFunction() is not a console function in Tribes (C++-internal
							// only), so the old check always failed with an unknown-command error and
							// ran an inline fallback. Bot_IsRealPlayer is defined in this file - call it.
							%isRealPlayer = Bot_IsRealPlayer(%checkId);

							if(%isRealPlayer)
							{
								if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
									echo("[SPAWN FLOW] Bot_CleanupStaleIds(): CRITICAL SAFEGUARD - Real player detected at clientId=" @ %checkId @ ". Aborting deletion.");
								continue;
							}
							
							// All checks passed - use consolidated cleanup helper
							Spawn_CleanupStaleClientId(%checkId, %botInfoAiName);
						}
					}
				}
			}
		}
	}
	
	return %cleanupPerformed;
}

// Store zone data for a bot using both storeData and direct array access
// This ensures DespawnZoneBots can find the bot regardless of which data source it checks
function Bot_StoreZoneData(%aiId, %spawnPointId)
{
	if(%aiId == "" || %aiId == -1) return;
	if(%spawnPointId == "" || %spawnPointId == -1) return;
	
	%markerZone = $MarkerZone[%spawnPointId];
	if(%markerZone == "" || %markerZone == -1) return;
	
	storeData(%aiId, "zone", %markerZone);
	storeData(%aiId, "tmpzone", %markerZone);
	storeData(%aiId, "SpawnOriginZoneID", %markerZone);
	
	// CRITICAL: Explicitly write to EnemyBotData as well, since storeData() might default to ClientData
	// because SpawnBotInfo isn't set yet (so isRPGAI returns false)
	$EnemyBotData[%aiId, "zone"] = %markerZone;
	$EnemyBotData[%aiId, "SpawnOriginZoneID"] = %markerZone;
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
		echo("[SPAWN FLOW] Bot_StoreZoneData(): Stored zone " @ %markerZone @ " for clientId=" @ %aiId);
}

// Get validated player object with FindPlayerInBotGroup fallback
// Returns "" if player object is invalid or doesn't exist
function Bot_GetValidatedPlayerObject(%clientId)
{
	if(%clientId == "" || %clientId == -1) return "";
	
	%playerObj = Client::getOwnedObject(%clientId);
	
	// Fallback if engine hasn't updated getOwnedObject yet
	if(%playerObj == -1 || %playerObj == "")
	{
		%playerObj = FindPlayerInBotGroup(%clientId);
		if(%playerObj != -1 && %playerObj != "" && ($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG))
			echo("[SPAWN FLOW] Bot_GetValidatedPlayerObject(): Found player object " @ %playerObj @ " via FindPlayerInBotGroup fallback for clientId=" @ %clientId);
	}
	
	// Validate object actually exists
	if(%playerObj == -1 || %playerObj == "" || !isObject(%playerObj))
		return "";
	
	return %playerObj;
}

// Check if a client ID belongs to a real player (not a bot)
// Returns true if this is a real player that should NOT be touched
// CONSOLIDATED: delegates to IsRealPlayer - the previous private implementation
// disagreed with it on ghost bots (not-AI-controlled + lingering bot data):
// this said "real player", IsRealPlayer correctly says "ghost bot, cleanable".
// IsRealPlayer is also stricter on protection (save-file cache, below-BaseRep-range fallback).
function Bot_IsRealPlayer(%clientId)
{
	return IsRealPlayer(%clientId);
}

// (Bot_MatchesEnemyPattern removed - no callers; HasEnemyBotNamePrefix plus the
// inline Round/Colloseum checks in SpawnAIGetClientId are what's actually used)

// ============================================================================
// SPAWN HELPER: Abort spawn when zone becomes empty during spawn delay
// Called when SpawnAIGetClientId detects zone has 0 players
// Cleans up ghost bot, rolls back spawn slot, tracks telemetry
// Returns: nothing (void function, caller should return after calling)
// ============================================================================
function Spawn_AbortEmptyZone(%newName, %spawnPointId, %zoneIndex)
{
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
		echo("[SPAWN FLOW] Spawn_AbortEmptyZone(): Zone " @ %zoneIndex @ " is empty, aborting spawn for " @ %newName);
	
	// CRITICAL: Delete the already-spawned bot to prevent ghost shell
	// AI::spawn() already created the bot, we MUST delete it
	
	// Try to find the bot using multiple methods
	// Silent lookups first - AI::getId() spams "Could not find drone" to the console
	// when the bot is already gone, so only use it as the last resort
	%ghostBotId = AI::getClientIdFromName(%newName);
	if(%ghostBotId == "" || %ghostBotId == -1)
		%ghostBotId = NEWgetClientByName(%newName);
	if(%ghostBotId == "" || %ghostBotId == -1)
		%ghostBotId = AI::getId(%newName);  // Engine lookup - spams console if not found
	
	// Handle Seal Battle bots specially
	// BUGFIX: match "Round*" anywhere in the name (== 0 prefix check missed rank-prefixed
	// bots like "ImmortalRoundOne0"), consistent with the checks in SpawnAIGetClientId
	if(String::findSubStr(%newName, "RoundOne") != -1 || String::findSubStr(%newName, "RoundTwo") != -1 || String::findSubStr(%newName, "RoundThree") != -1)
	{
		if(%ghostBotId != -1 && %ghostBotId != "")
		{
			storeData(%ghostBotId, "SealBattleBot", true);
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
				echo("[SPAWN FLOW] Spawn_AbortEmptyZone(): Flagged ghost bot " @ %newName @ " as SealBattleBot");
		}
	}
	
	// Clear spawn flags
	$SpawnAIScheduled[%newName] = "";
	
	// Clear bot data if found
	if(%ghostBotId != -1 && %ghostBotId != "")
	{
		storeData(%ghostBotId, "SpawnBotInfo", "");
		storeData(%ghostBotId, "BotInfoAiName", "");
		$BotType[%ghostBotId] = "";
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN AI] Deleting ghost bot via AI::delete: " @ %newName @ " (clientId=" @ %ghostBotId @ ")");
	}
	else
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN AI] Couldn't find clientId for " @ %newName @ ", trying AI::delete anyway");
	}
	
	// ALWAYS try AI::delete by name - the bot was created by AI::spawn
	AI::delete(%newName);
	
	// CRITICAL: Rollback spawn slot to prevent spawn point from being marked "busy" forever
	RollbackSpawnSlot(%spawnPointId);
	
	Telemetry_RecordSpawnFailed("zoneempty");
}

// ============================================================================
// SPAWN HELPER: Full cleanup for stale/orphaned client ID before reuse
// Consolidates: player object deletion, counter decrements, AI number freeing, data clearing
// Returns: true if cleanup performed, false if skipped (real player or invalid)
// ============================================================================
function Spawn_CleanupStaleClientId(%clientId, %oldBotInfoAiName)
{
	if(%clientId == "" || %clientId == -1) return false;
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
		echo("[SPAWN FLOW] Spawn_CleanupStaleClientId(): Cleaning up stale client ID " @ %clientId @ " (old BotInfoAiName='" @ %oldBotInfoAiName @ "')");
	
	// Get existing BotInfoAiName if not provided
	if(%oldBotInfoAiName == "" || %oldBotInfoAiName == -1)
		%oldBotInfoAiName = fetchData(%clientId, "BotInfoAiName");
	
	// Delete any existing player object to prevent shell bots
	%oldPlayerObj = Client::getOwnedObject(%clientId);
	if(%oldPlayerObj != -1 && %oldPlayerObj != "" && isObject(%oldPlayerObj))
	{
		if(IsSafeToDeletePlayerObject(%oldPlayerObj, %clientId, "Spawn_CleanupStaleClientId"))
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
				echo("[SPAWN FLOW] Spawn_CleanupStaleClientId(): Deleting old bot (AI name='" @ %oldBotInfoAiName @ "')");
			
			// CRITICAL: Use AI::delete for named bots to properly unregister from engine
			// Without this, the AI drone remains registered in the Torque AI engine
			if(%oldBotInfoAiName != "" && %oldBotInfoAiName != -1 && %oldBotInfoAiName != "0")
			{
				AI::delete(%oldBotInfoAiName);
			}
			else
			{
				// Fallback: No AI name, use direct deleteObject
				// Decrement $numAI ONLY if this is an Enemy Bot that hasn't been processed by Player::onKilled
				// (Player::onKilled clears SpawnBotInfo, so if it's still here, we need to decrement)
				%checkSpawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
				if(%checkSpawnBotInfo != "" && %checkSpawnBotInfo != -1 && %checkSpawnBotInfo != "0")
				{
					if($numAI > 0)
					{
						$numAI--;
						$Telemetry_NumAI_Dec++;
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN COUNTER] Spawn_CleanupStaleClientId: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %clientId);
					}
				}
				Client::setOwnedObject(%clientId, -1);
				deleteObject(%oldPlayerObj);
			}
		}
	}
	
	// Decrement spawn counter BEFORE clearing SpawnBotInfo
	DecrementSpawnCounter(%clientId);
	
	// Free AI number BEFORE clearing BotInfoAiName
	if(%oldBotInfoAiName != "" && %oldBotInfoAiName != -1 && %oldBotInfoAiName != "0")
	{
		%aiNumber = $tmpbotn[%oldBotInfoAiName];
		if(%aiNumber != "" && %aiNumber != -1)
		{
			$aiNumTable[%aiNumber] = "";
			$tmpbotn[%oldBotInfoAiName] = "";
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
				echo("[SPAWN FLOW] Spawn_CleanupStaleClientId(): Freed AI number " @ %aiNumber @ " for bot " @ %oldBotInfoAiName);
		}
	}
	
	// Clear all bot data
	Bot_ClearStaleData(%clientId);
	
	return true;
}

// Clear all stale bot data from a client ID
// Used when reusing a client ID or cleaning up shell bots
function Bot_ClearStaleData(%clientId)
{
	if(%clientId == "" || %clientId == -1) return;
	
	storeData(%clientId, "BotInfoAiName", "");
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "HasLoadedAndSpawned", "");
	
	$EnemyBotData[%clientId, "BotInfoAiName"] = "";
	$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "BotInfoAiName"] = "";
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$BotInfoAiName[%clientId] = "";
	
	UnregisterBot(%clientId);
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
		echo("[SPAWN FLOW] Bot_ClearStaleData(): Cleared stale data for clientId=" @ %clientId);
}

function SpawnAIGetClientId(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout, %spawnPointId, %predictedId)
{
	// WATCHDOG: Track this function for freeze detection
	Watchdog_Enter("SpawnAIGetClientId");
	AI::detectSimRebase();   // keep ID-reuse guards correct across the time-fix DLL's clock rebase
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN DEBUG] SpawnAIGetClientId: ENTRY spawnPointId='" @ %spawnPointId @ "' for " @ %newName @ ", predictedId=" @ %predictedId);
	
	// CRITICAL INTEGRATION: Check predicted ID first (O(1) lookup)
	// If the predicted ID matches our bot name, we found it immediately!
	if(%predictedId != "" && %predictedId != -1)
	{
		%checkName = Client::getName(%predictedId);
		if(%checkName == %newName)
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
				echo("[SPAWN FLOW] SpawnAIGetClientId(): Prediction SUCCESS! Found bot " @ %newName @ " at ID " @ %predictedId);
			
			// Inject this ID into AI::getId's typical result path to skip other searches
			// Just ensure we use it below
		}
		else
		{
			// Prediction failed (race condition or mismatch) - log and fall back to standard search
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
				echo("[SPAWN FLOW] SpawnAIGetClientId(): Prediction mismatch (ID " @ %predictedId @ " has name '" @ %checkName @ "', expected '" @ %newName @ "'). Falling back to standard search.");
			%predictedId = ""; // Clear so we don't use it
		}
	}
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAIGetClientId(): ENTRY - newName=" @ %newName @ ", displayName=" @ %displayName @ ", spawnPointId=" @ %spawnPointId @ " @ " @ getSimTime());
	
	// Extract spawnPointId from commandIssuer if not provided
	if(%spawnPointId == "" || %spawnPointId == -1)
	{
		if(GetWord(%commandIssuer, 0) == "SpawnPoint")
			%spawnPointId = GetWord(%commandIssuer, 1);
	}
	
	// CRITICAL: Check if zone is still occupied before proceeding
	// If zone became empty during 3.0s delay, abort spawn to prevent orphaned bots
	if(%spawnPointId != "" && %spawnPointId != -1)
	{
		%zoneFolderID = $MarkerZone[%spawnPointId];
		if(%zoneFolderID != "" && %zoneFolderID != -1)
		{
			%zoneIndex = Zone::getIndex(%zoneFolderID);
			if(%zoneIndex > 0)
			{
				%playerCount = $ZonePlayerCount[%zoneIndex];
				if(%playerCount == "")
					%playerCount = 0;
				
				if(%playerCount <= 0)
				{
					// Use consolidated helper for zone empty abort (replaces ~50 lines)
					Spawn_AbortEmptyZone(%newName, %spawnPointId, %zoneIndex);
					return; // Abort spawn
				}
			}
		}
	}
	
	// CRITICAL: AI::spawn() creates a Player object for enemy bots
	// BotInfoAiName is NOT set yet (set later), so getClientIdFromName() will fail
	// Try AI::getId() FIRST (most reliable for newly spawned bots), then NEWgetClientByName(), then brute-force
	%aiId = "";
	
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAIGetClientId(): Attempting to get client ID for " @ %newName @ " (displayName: " @ %displayName @ ")");
	
	// Priority 0: Use Predicted ID if validated
	if(%predictedId != "" && %predictedId != -1)
	{
		%aiIdFromGetId = %predictedId;
	}
	else
	{
		%aiIdFromGetId = AI::getId(%newName);
	}
	
	// Priority 1: Try AI::getId() first (most reliable for newly spawned bots, works immediately)
	// This is the fastest and most reliable way to find a bot that was just spawned
	if(%aiIdFromGetId != -1 && %aiIdFromGetId != "" && %aiIdFromGetId != "False" && %aiIdFromGetId != "false")
	{
		// CRITICAL SAFEGUARD: Check if a player is actively connecting to this client ID
		// If so, abort this spawn to prevent collision (causes black screen for player)
		%playerConnecting = $ClientIdPlayerConnecting[%aiIdFromGetId];
		if(%playerConnecting != "" && %playerConnecting != "0" && %playerConnecting != -1)
		{
			%timeSinceConnect = getSimTime() - %playerConnecting;
			// Only abort if connection is recent (within 30 seconds)
			if(%timeSinceConnect < 30)
			{
				echo("CRITICAL SAFEGUARD: SpawnAIGetClientId - Client ID " @ %aiIdFromGetId @ " has active player connection (" @ %timeSinceConnect @ "s ago). ABORTING spawn for bot " @ %newName @ " to prevent collision.");
				
				// CRITICAL: Delete the orphaned bot that was already created by AI::spawn()
				// Without this, the bot exists as a "shell" with no proper registration
				%escapedName = String::replace(%newName, "\"", "\\\"");
				echo("[SHELL CLEANUP] Deleting orphaned bot " @ %newName @ " (clientId=" @ %aiIdFromGetId @ ") to prevent shell bot");
				
				// SAFETY: Only clear bot data if this client ID is still AI-controlled
				// If the player has already taken over the ID, we must NOT clear their data
				if(Player::isAiControlled(%aiIdFromGetId))
				{
					ClearAllBotData(%aiIdFromGetId, false);
				}
				else
				{
					echo("[SHELL CLEANUP] WARNING: Client ID " @ %aiIdFromGetId @ " is no longer AI-controlled, skipping ClearAllBotData to protect player");
				}
				
				// AI::delete works by name, not client ID, so it's safe regardless
				AI::delete(%escapedName);
				
				// Rollback spawn slot
				if(%spawnPointId != "" && %spawnPointId != -1)
					RollbackSpawnSlot(%spawnPointId);
				Telemetry_RecordSpawnFailed("clientid");
				return;
			}
			else
			{
				// Connection is stale (>30s) - clear it and proceed
				$ClientIdPlayerConnecting[%aiIdFromGetId] = "";
			}
		}
		
		// Validate the client ID has a valid player object
		%playerObj = Bot_GetValidatedPlayerObject(%aiIdFromGetId);
			
		if(%playerObj != "")
		{
			// Verify it's AI-controlled (safety check)
			if(Player::isAiControlled(%aiIdFromGetId))
			{
				%aiId = %aiIdFromGetId;
				
				// CRITICAL FIX: Even in fast-path/Priority 1, we MUST check for stale data!
				// If this ID was recently freed (recycled by engine), it might still have old bot data (stats, flags)
				%recentlyFreed = $ClientIdRecentlyFreed[%aiId];
				if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
				{
					// CRITICAL: Check if a new valid player object exists at this clientId
					// If so, the new bot has already spawned - do NOT clear its data!
					%playerObj = Client::getOwnedObject(%aiId);
					if(%playerObj == -1 || !isObject(%playerObj))
					{
						// No valid player object - safe to clear stale data
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
							echo("[SPAWN FLOW] SpawnAIGetClientId(): Fast-path ID " @ %aiId @ " was recently freed! FORCING stale data cleanup.");
						
						// Force clean the ID to prevent stat inheritance (God Mode exploit)
						Bot_ClearStaleData(%aiId);
					}
					else
					{
						// Valid player object exists - new bot already spawned, skip cleanup
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
							echo("[SPAWN FLOW] SpawnAIGetClientId(): Fast-path ID " @ %aiId @ " was recently freed BUT new player object exists - skipping cleanup to preserve new bot.");
					}
					
					// Clear the flag so we don't check it again
					$ClientIdRecentlyFreed[%aiId] = "";
				}
				
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via AI::getId() (fastest method)");
				
				// Store zone data so DespawnZoneBots can find this bot
				Bot_StoreZoneData(%aiId, %spawnPointId);
				
				// CRITICAL FIX: Set SealBattleBot flag IMMEDIATELY if this is a seal battle bot
				// This ensures that when HardcodeAIskills runs (before SetupBot), RefreshAll() knows to calculate stats
				if(String::findSubStr(%newName, "RoundOne") == 0 || String::findSubStr(%newName, "RoundTwo") == 0 || String::findSubStr(%newName, "RoundThree") == 0)
				{
					storeData(%aiId, "SealBattleBot", true);
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Flagged new bot " @ %newName @ " (clientId=" @ %aiId @ ") as SealBattleBot");
				}
			}
		}
	}
	
	// Priority 2: Try NEWgetClientByName() (fast lookup by display name)
	if((%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false") && %displayName != "" && %displayName != -1)
	{
		%aiId = NEWgetClientByName(%displayName);
		if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
		{
			// Found via NEWgetClientByName() - validate it's safe to use
			%playerObj = Bot_GetValidatedPlayerObject(%aiId);
				
			if(%playerObj != "")
			{
				// Check if it was recently freed
				%recentlyFreed = $ClientIdRecentlyFreed[%aiId];
				if(%recentlyFreed == "" || %recentlyFreed == "0" || %recentlyFreed == -1)
				{
					// Not recently freed - check if it's already assigned to a bot
					%existingBotInfoAiName = fetchData(%aiId, "BotInfoAiName");
					if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0" || %existingBotInfoAiName == %newName)
					{
							// Safe to use - this is our newly-spawned bot
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via NEWgetClientByName()");
						
						// Store zone data so DespawnZoneBots can find this bot
						Bot_StoreZoneData(%aiId, %spawnPointId);
						
						// CRITICAL FIX: Set SealBattleBot flag IMMEDIATELY if this is a seal battle bot
						// This ensures that when HardcodeAIskills runs (before SetupBot), RefreshAll() knows to calculate stats
						if(String::findSubStr(%newName, "RoundOne") == 0 || String::findSubStr(%newName, "RoundTwo") == 0 || String::findSubStr(%newName, "RoundThree") == 0)
						{
							storeData(%aiId, "SealBattleBot", true);
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Flagged new bot " @ %newName @ " (clientId=" @ %aiId @ ") as SealBattleBot");
						}
					}
					else
					{
						// Already assigned to different bot - clear and continue to brute-force
						%aiId = "";
					}
				}
				else
				{
					// Recently freed - check if enough time has passed (10 seconds to prevent shell bots)
					%currentTime = getSimTime();
					%timeSinceFreed = %currentTime - %recentlyFreed;
					if(%timeSinceFreed >= 10)
					{
						// Enough time has passed - clear flag and use it
						$ClientIdRecentlyFreed[%aiId] = "";
						
						// CRITICAL FIX: Clear ALL old bot data before reusing this client ID
						// This prevents shell bot detection from triggering due to stale BotInfoAiName
						// If the old BotInfoAiName doesn't match the new bot name, we must clear it
						if(%existingBotInfoAiName != "" && %existingBotInfoAiName != -1 && %existingBotInfoAiName != "0" && %existingBotInfoAiName != %newName)
						{
							// Use consolidated cleanup helper (replaces 35 lines of inline code)
							Spawn_CleanupStaleClientId(%aiId, %existingBotInfoAiName);
						}
						
						// Now check if we can use this client ID (should be clean now)
						%existingBotInfoAiName = fetchData(%aiId, "BotInfoAiName");
						if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0" || %existingBotInfoAiName == %newName)
						{
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via NEWgetClientByName() (was recently freed but enough time passed, old data cleared)");

							// Store zone data so DespawnZoneBots can find this bot
							Bot_StoreZoneData(%aiId, %spawnPointId);
						}
						else
						{
							// Still has conflicting bot data after cleanup attempt - skip this client ID
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - Client ID " @ %aiId @ " still has conflicting BotInfoAiName='" @ %existingBotInfoAiName @ "' after cleanup attempt. Skipping.");
							%aiId = "";
						}
					}
					else
					{
						// Not enough time - skip and continue to brute-force
						%aiId = "";
					}
				}
			}
			else
			{
				// Player object invalid - clear aiId so Priority 3 brute-force can be tried
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): NEWgetClientByName() returned " @ %aiId @ " but Player object is missing - falling through to brute-force search.");
				%aiId = ""; // Clear so Priority 3 runs
			}
		}
	}
	
	// Priority 3: If NEWgetClientByName() failed, use Client::getFirst/getNext (CRITICAL FIX #3)
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): NEWgetClientByName() failed, using Client::getFirst/getNext iteration");
		// CRITICAL FIX #3: Replace brute-force loop with Client::getFirst/getNext
		// Hard Rule: Never touch a ClientID unless Player::isAiControlled(%id) returns true
		if(%displayName != "" && %displayName != -1)
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Iterating through connected clients for displayName '" @ %displayName @ "'");
			%foundCount = 0;
			for(%checkId = Client::getFirst(); %checkId != -1; %checkId = Client::getNext(%checkId))
			{
				// CRITICAL FIX #3: Hard Rule - Only touch AI-controlled clients
				if(!Player::isAiControlled(%checkId))
				{
					// This is a real player - skip immediately (don't even check name)
					continue;
				}
				
				// CRITICAL FIX #1: Check if this clientId is in the graveyard
				if(IsInGraveyard(%checkId))
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): SKIPPING client ID " @ %checkId @ " - In graveyard (dying bot)");
					continue;
				}
				%checkName = Client::getName(%checkId);
				%playerObj = Bot_GetValidatedPlayerObject(%checkId);
				
				// DEBUG: Log first few valid client IDs to see what we're finding
				if(%foundCount < 5 && %playerObj != -1 && %playerObj != "" && isObject(%playerObj))
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): DEBUG - clientId " @ %checkId @ " has name='" @ %checkName @ "', playerObj=" @ %playerObj);
					%foundCount++;
				}
				
				// CRITICAL: Check if Player object exists and is valid BEFORE checking name match
				if(%playerObj == -1 || %playerObj == "")
				{
					// Player object is gone - check for shell bots
					%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
					%existingSpawnInfo = fetchData(%checkId, "SpawnBotInfo");
					%registrySpawn = $BotRegistry[%checkId];
					%isLikelyBot = false;
					if(%existingBotInfoAiName != "" && %existingBotInfoAiName != -1)
						%isLikelyBot = true;
					else if(%existingSpawnInfo != "" && %existingSpawnInfo != -1)
						%isLikelyBot = true;
					else if(%registrySpawn != "" && %registrySpawn != -1)
						%isLikelyBot = true;
					
					if(%isLikelyBot)
					{
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Player object missing for client ID " @ %checkId @ " but bot markers exist. Cleaning up shell.");
						CleanupBot(%checkId, %existingBotInfoAiName);
					}
					else
					{
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): SKIPPING client ID " @ %checkId @ " - Player object is missing (no bot markers).");
					}
					continue;
				}
				
				// Player object exists - check if it's actually valid
				if(!isObject(%playerObj))
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): SKIPPING client ID " @ %checkId @ " - Player object exists but is invalid (shell bot cleanup in progress)");
					
					// If this ID is tracked as a bot, clean it aggressively to avoid lingering shells
					%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
					%existingSpawnInfo = fetchData(%checkId, "SpawnBotInfo");
					%registrySpawn = $BotRegistry[%checkId];
					%isLikelyBot = false;
					if(%existingBotInfoAiName != "" && %existingBotInfoAiName != -1)
						%isLikelyBot = true;
					else if(%existingSpawnInfo != "" && %existingSpawnInfo != -1)
						%isLikelyBot = true;
					else if(%registrySpawn != "" && %registrySpawn != -1)
						%isLikelyBot = true;
					
					if(%isLikelyBot)
					{
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Cleaning shell for client ID " @ %checkId @ " (invalid playerObj, bot markers present).");
						CleanupBot(%checkId, %existingBotInfoAiName);
					}
					continue; // Skip this client ID, it's a shell bot being cleaned up
				}
				
				%isDead = %playerObj.dead;
				if(%isDead == "1" || %isDead == 1)
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): SKIPPING client ID " @ %checkId @ " - Player object still exists and is marked as dead (cleanup in progress)");
					continue; // Skip this client ID, it's still being cleaned up
				}
				
				// NEW LOGIC: Trust nameless fresh bots (Name Replication Lag Fix)
				// When AI::spawn creates a bot, Client::getName() returns empty for a few seconds until name replicates
				// We must accept these nameless clients if they meet specific criteria to prevent spawn rollbacks
				// This check happens AFTER player object validation but BEFORE name comparison to catch fresh bots immediately
				if(%checkName == "" && %displayName != "")
				{
					// Name is empty but we have an expected display name - check if this is our fresh bot
					// Player object was already validated above, so we know it's safe to use
					if(Player::isAiControlled(%checkId))
					{
						// This is an AI-controlled client with a valid player object - check if it's NOT in the graveyard (dying)
						if(!IsInGraveyard(%checkId))
						{
							// Verify it's not already assigned to another bot
							%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
							if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
							{
								// No existing bot assigned - this is our fresh bot whose name hasn't replicated yet
								%aiId = %checkId;
								if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found nameless AI client " @ %checkId @ " - Assuming it is " @ %displayName @ " (name lag)");
								
								// Store zone data so DespawnZoneBots can find this bot
								Bot_StoreZoneData(%aiId, %spawnPointId);
								break;
							}
						}
					}
				}
				
				// CRITICAL: Check if the name matches FIRST - if it does, this IS our newly-spawned bot
				// In this case, we should clear the "recently freed" flag and use it
				%isOurNewBot = false;
				if(%checkName != "" && %checkName != -1 && String::ICompare(%displayName, %checkName) == 0)
				{
					// Name matches - this is our newly-spawned bot!
					// Clear the "recently freed" flag since this IS our bot
					$ClientIdRecentlyFreed[%checkId] = "";
					%isOurNewBot = true;
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found newly-spawned bot " @ %newName @ " at clientId " @ %checkId @ " - cleared recently freed flag");
					
					// Store zone data so DespawnZoneBots can find this bot
					Bot_StoreZoneData(%checkId, %spawnPointId);
				}
				
				// If this is NOT our newly-spawned bot, check if it was recently freed
				// This prevents reusing client IDs that are still being cleaned up
				if(!%isOurNewBot)
				{
					%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
					if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
					{
						%currentTime = getSimTime();
						%timeSinceFreed = %currentTime - %recentlyFreed;
						if(%timeSinceFreed < 5) // 5 seconds for cleanup
						{
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): SKIPPING client ID " @ %checkId @ " - was recently freed " @ %timeSinceFreed @ "s ago (freed at " @ %recentlyFreed @ ", current time " @ %currentTime @ ", need 5s delay for cleanup)");
							continue; // Skip this client ID, cleanup still in progress
						}
						else
						{
							// Enough time has passed, clear the flag
							$ClientIdRecentlyFreed[%checkId] = "";
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Cleared recently freed flag for client ID " @ %checkId @ " (freed " @ %timeSinceFreed @ "s ago)");
						}
					}
				}
				
				// Now check if the name matches (only if client ID passed all validation checks above)
				if(%checkName != "" && %checkName != -1)
				{
					if(String::ICompare(%displayName, %checkName) == 0)
					{
						// Found it! Check if it's already assigned to a bot
						%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
						
						if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
						{
							%aiId = %checkId;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via brute-force search (no existing BotInfoAiName)");
							
							// Store zone data so DespawnZoneBots can find this bot
							Bot_StoreZoneData(%aiId, %spawnPointId);
							break;
						}
						else if(%existingBotInfoAiName == %newName)
						{
							%aiId = %checkId;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via brute-force search (matches BotInfoAiName)");
							break;
						}
						else
						{
							// Display name matches but BotInfoAiName doesn't - this is our newly-spawned bot with stale BotInfoAiName
							// Clear the stale data and use this client ID
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %checkId @ " via brute-force search (display name matches but BotInfoAiName='" @ %existingBotInfoAiName @ "' is stale, clearing)");
							
							// Clear stale bot data
							Bot_ClearStaleData(%checkId);
							
							%aiId = %checkId;
							
							// Store zone data so DespawnZoneBots can find this bot
							Bot_StoreZoneData(%aiId, %spawnPointId);
							break;
						}
					}
				}
				
				// FALLBACK #1: If Client::getName() is empty but Player object exists and is AI-controlled, trust it
				// The Torque engine takes a few frames to populate Client::getName() for newly spawned bots
				// If we have a valid Player object that is AI-controlled, this is likely our newly spawned bot
				if((%checkName == "" || %checkName == -1) && %playerObj != -1 && %playerObj != "" && isObject(%playerObj))
				{
					// Player object exists and is valid, but name isn't ready yet
					// Check if this is AI-controlled (strong indicator it's a bot)
					if(Player::isAiControlled(%checkId))
					{
						// This is an AI-controlled client with a valid Player object but no name yet
						// Check if it's not already assigned to another bot
						%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
						if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
						{
							// No existing bot assigned - this is likely our newly spawned bot
							%aiId = %checkId;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - Found unnamed AI client ID " @ %checkId @ " - assuming it is " @ %newName @ " (Player object exists, AI-controlled, no BotInfoAiName)");
							break;
						}
					}
				}
				
				// FALLBACK #2: Check Player object's .name field (sometimes the object has the name before the client does)
				if((%checkName == "" || %checkName == -1) && %playerObj != -1 && %playerObj != "" && isObject(%playerObj))
				{
					%playerObjName = %playerObj.name;
					if(%playerObjName != "" && %playerObjName != -1 && String::ICompare(%displayName, %playerObjName) == 0)
					{
						// Player object's name matches! This is our bot
						%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
						if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
						{
							%aiId = %checkId;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via Player object name fallback (playerObj.name='" @ %playerObjName @ "', Client::getName not ready yet)");
							break;
						}
						else if(%existingBotInfoAiName == %newName)
						{
							%aiId = %checkId;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via Player object name fallback (matches BotInfoAiName)");
							break;
						}
					}
				}
			}
			if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
			{
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Brute-force search failed");
				// DEBUG: Try one more time with a small delay to see if names are ready now
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): DEBUG - Attempting final check with GetEveryoneIdList()");
				%idList = GetEveryoneIdList();
				if(%idList != "")
				{
					for(%i = 0; (%checkId = GetWord(%idList, %i)) != -1; %i++)
					{
						%checkName = Client::getName(%checkId);
						if(%checkName != "" && %checkName != -1 && String::ICompare(%displayName, %checkName) == 0)
						{
							%aiId = %checkId;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found client ID " @ %aiId @ " via GetEveryoneIdList() fallback");
							break;
						}
					}
				}
			}
		}
	}
	else
	{
		// Only warn if we ALSO don't have a valid aiId - otherwise this is expected
		// (we found the bot via AI::getId but displayName was empty, which is fine)
		if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - displayName is empty for bot " @ %newName @ " (aiId=" @ %aiId @ ", spawnPointId=" @ %spawnPointId @ "). Cannot search by displayName.");
		}
	}
	
	// CRITICAL FIX #2: Use spawnPointId from parameter if provided, otherwise extract from commandIssuer
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
	
	// Validate player object if we found a client ID
	if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
	{
		// CRITICAL: Before reusing a client ID, ensure any old player object is deleted
		// This prevents shell bots from forming when a new bot spawns before the old one's player object is deleted
		// SAFEGUARD: First check if this is a real player - never touch real players
		%playerNameCheck = Client::getName(%aiId);
		if(%playerNameCheck != "" && %playerNameCheck != -1)
		{
			%characterFile = "temp\\" @ %playerNameCheck @ ".cs";
			if(isFile(%characterFile))
			{
				// This is a real player with a save file - abort immediately
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): CRITICAL SAFEGUARD - clientId " @ %aiId @ " is a real player (" @ %playerNameCheck @ ") with save file. Aborting to prevent data loss.");
				%aiId = -1; // Reset to force retry with different client ID
			}
		}
		
		// Only proceed if we confirmed it's not a real player
		if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
		{
			%existingPlayerObj = Client::getOwnedObject(%aiId);
			if(%existingPlayerObj != -1 && %existingPlayerObj != "" && isObject(%existingPlayerObj))
			{
				// Check if this is actually our newly-spawned bot or an old one
				%existingBotInfoAiName = fetchData(%aiId, "BotInfoAiName");
				%existingName = Client::getName(%aiId);
				
				// SAFEGUARD: Double-check this is not a real player
				if(%existingName != "" && %existingName != -1)
				{
					%characterFileCheck = "temp\\" @ %existingName @ ".cs";
					if(isFile(%characterFileCheck))
					{
						// This is a real player - abort immediately
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): CRITICAL SAFEGUARD - Found real player " @ %existingName @ " (clientId=" @ %aiId @ ") with save file. Aborting to prevent data loss.");
						%aiId = -1; // Reset to force retry
					}
				}
				
				// Only proceed with deletion if we confirmed it's not a real player
				if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
				{
					// CRITICAL FIX: Check if the current display name matches our new bot
					// If it does, this IS our newly spawned bot - stale BotInfoAiName is from a previous bot
					// Just clear the stale data instead of deleting the player object!
					if(String::ICompare(%existingName, %displayName) == 0)
					{
						// This is our new bot! Don't delete it - just clear stale BotInfoAiName if present
						if(%existingBotInfoAiName != "" && %existingBotInfoAiName != -1 && %existingBotInfoAiName != "0" && %existingBotInfoAiName != %newName)
						{
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found OUR bot " @ %displayName @ " with stale BotInfoAiName='" @ %existingBotInfoAiName @ "'. Clearing stale data, NOT deleting.");
							storeData(%aiId, "BotInfoAiName", "");
							$BotInfoAiName[%aiId] = "";
							$EnemyBotData[%aiId, "BotInfoAiName"] = "";
							$TownBotData[%aiId, "BotInfoAiName"] = "";
						}
						// Skip the deletion block - this is our bot
					}
					// If the name doesn't match OR BotInfoAiName is set to a different bot, this is an old player object
					else if((%existingName != "" && %existingName != -1 && String::ICompare(%displayName, %existingName) != 0) || 
					   (%existingBotInfoAiName != "" && %existingBotInfoAiName != -1 && %existingBotInfoAiName != "0" && %existingBotInfoAiName != %newName))
					{
						// CRITICAL: Final verification immediately before deletion (prevents race condition)
						// Re-check save file one more time to ensure a real player didn't connect between checks
						%finalNameCheck = Client::getName(%aiId);
						if(%finalNameCheck != "" && %finalNameCheck != -1)
						{
							%finalCharacterFile = "temp\\" @ %finalNameCheck @ ".cs";
							if(isFile(%finalCharacterFile))
							{
								if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): FINAL SAFEGUARD - Real player " @ %finalNameCheck @ " (clientId=" @ %aiId @ ") detected immediately before deletion. Aborting to prevent data loss.");
								%aiId = -1; // Reset to force retry
								Telemetry_RecordSpawnFailed("clientid");
								return -1; // Exit immediately
							}
							
							// Additional check: Verify name doesn't match player pattern (not bot pattern)
							if(!HasEnemyBotNamePrefix(%finalNameCheck))
							{
								// Name doesn't match bot patterns - could be a real player
								// Double-check with save file one more time
								%doubleCheckFile = "temp\\" @ %finalNameCheck @ ".cs";
								if(isFile(%doubleCheckFile))
								{
									if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): FINAL SAFEGUARD - Name '" @ %finalNameCheck @ "' has save file. This is a REAL PLAYER. Aborting deletion.");
									%aiId = -1;
									Telemetry_RecordSpawnFailed("clientid");
									return -1;
								}
							}
						}
						
						// Additional safeguard: Verify this is not a connected real player
						%isConnected = false;
						for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
						{
							if(%cl == %aiId)
							{
								%isConnected = true;
								break;
							}
						}
						
						// If connected but no save file yet, check if it's a real player by checking if it's NOT AI-controlled
						if(%isConnected && !Player::isAiControlled(%aiId))
						{
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): FINAL SAFEGUARD - Connected client " @ %aiId @ " is NOT AI-controlled. This is a real player. Aborting deletion.");
							%aiId = -1;
							Telemetry_RecordSpawnFailed("clientid");
							return -1;
						}
						
						// All safeguards passed - safe to delete old bot using AI::delete
						if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
						{
							// Get AI name for proper deletion
							%oldAiName = %existingBotInfoAiName;
							if(%oldAiName == "" || %oldAiName == -1) %oldAiName = $BotInfoAiName[%aiId];
							if(%oldAiName == "" || %oldAiName == -1) %oldAiName = $EnemyBotData[%aiId, "BotInfoAiName"];
							if(%oldAiName == "" || %oldAiName == -1) %oldAiName = $TownBotData[%aiId, "BotInfoAiName"];
							
							if(%oldAiName != "" && %oldAiName != -1 && %oldAiName != "0")
							{
								// CRITICAL FIX: Check if this bot name is actually on THIS clientId or a different one
								// If it returns a DIFFERENT clientId, the bot is ALIVE elsewhere - do NOT call
								// AI::delete() or it will kill the live bot!
								// Use AI::getClientIdFromName() (silent) instead of AI::getId() - dead AIs are
								// renamed to "Corpse<N> <name>" by the engine, so AI::getId() on a stale name
								// spams "Could not find drone" to the console
								%actualClientId = AI::getClientIdFromName(%oldAiName);
								
								if(%actualClientId == %aiId)
								{
									// Bot is on this clientId - safe to delete by name
									if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Deleting old bot via AI::delete: " @ %oldAiName);
									AI::delete(%oldAiName);
								}
								else if(%actualClientId != -1 && %actualClientId != "" && %actualClientId != "False" && %actualClientId != "false")
								{
									// Bot is ALIVE on a different clientId - DON'T delete by name!
									if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - Bot " @ %oldAiName @ " is alive on clientId " @ %actualClientId @ ", NOT deleting by name. Just clearing stale data.");
									deleteObject(%existingPlayerObj);
									Client::setOwnedObject(%aiId, -1);
								}
								else
								{
									// Bot name not found in AI engine - delete player object directly
									if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Bot " @ %oldAiName @ " not found in AI engine, deleting player object");
									deleteObject(%existingPlayerObj);
									Client::setOwnedObject(%aiId, -1);
								}
							}
							else
							{
								if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - No AI name for old bot, using deleteObject fallback");
								deleteObject(%existingPlayerObj);
								Client::setOwnedObject(%aiId, -1);
							}
							// Run cleanup to clear any stale data
							PreSpawnCleanup(%aiId);
						}
					}
				}
			}
		}
		
		%playerObj = Client::getOwnedObject(%aiId);
		
		// CRITICAL: Check if this is our newly-spawned bot BEFORE checking "recently freed" flag
		// If the player object exists, is valid, and the name matches, this IS our bot
		// In this case, we should clear the "recently freed" flag and use it
		%isValidNewBot = false;
		if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
		{
			%actualName = Client::getName(%aiId);
			if(%actualName != "" && %actualName != -1 && String::ICompare(%actualName, %displayName) == 0)
			{
				// This is our newly-spawned bot - clear the "recently freed" flag
				$ClientIdRecentlyFreed[%aiId] = "";
				%isValidNewBot = true;
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found newly-spawned bot " @ %newName @ " at clientId " @ %aiId @ " - cleared recently freed flag");
			}
		}
		
		// CRITICAL: Double-check "recently freed" flag AFTER validating it's our bot
		// This catches cases where the bot died between the loop check and here
		// BUT: Skip this check if we confirmed this is our newly-spawned bot
		if(!%isValidNewBot)
		{
			%recentlyFreed = $ClientIdRecentlyFreed[%aiId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				if(%timeSinceFreed < 5) // 5 seconds for cleanup
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): ERROR - Client ID " @ %aiId @ " was recently freed " @ %timeSinceFreed @ "s ago (freed at " @ %recentlyFreed @ ", current time " @ %currentTime @ "). Bot " @ %newName @ " died during spawn! Skipping this client ID.");
					%aiId = -1; // Reset to force retry with different client ID
				}
			}
			
			// CRITICAL: Check for shell bots - Player object exists but bot is dead/invalid
			// Only check if we haven't confirmed this is our newly-spawned bot
			if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
			{
				if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
				{
					// Player object exists - check if this is a shell bot (dead bot with lingering Player object)
					%hasLoadedAndSpawned = fetchData(%aiId, "HasLoadedAndSpawned");
					%botInfoAiName = fetchData(%aiId, "BotInfoAiName");
					%spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
					%isDead = %playerObj.dead;
					
					// Shell bot detection: Player object exists but:
					// 1. HasLoadedAndSpawned is not set (bot never fully initialized)
					// 2. OR bot is marked as dead
					// 3. OR bot data indicates it's a bot but name doesn't match our new bot
					%isShellBot = false;
					if((%hasLoadedAndSpawned == "" || %hasLoadedAndSpawned == "0" || %hasLoadedAndSpawned == -1) && 
					   (%botInfoAiName != "" || %spawnBotInfo != ""))
					{
						// Bot data exists but HasLoadedAndSpawned is not set - likely a shell bot
						%isShellBot = true;
					}
					else if(%isDead == "1" || %isDead == 1)
					{
						// Player object is marked as dead - this is a shell bot
						%isShellBot = true;
					}
					else if(%botInfoAiName != "" && %botInfoAiName != %newName)
					{
						// BotInfoAiName is set to a different bot - this is a shell bot
						%isShellBot = true;
					}
					
					if(%isShellBot)
					{
						// CRITICAL SAFEGUARD: Verify this is actually a bot, not a player
						%shellBotName = Client::getName(%aiId);
						%isRealPlayer = false;
						if(%shellBotName != "" && %shellBotName != -1)
						{
							%characterFile = "temp\\" @ %shellBotName @ ".cs";
							if(isFile(%characterFile))
							{
								%isRealPlayer = true;
							}
						}
						
						if(!%isRealPlayer)
						{
							// This is a shell bot - clean it up
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Detected shell bot at clientId " @ %aiId @ " (Player object exists but bot is dead/invalid). Cleaning up...");
							
							// CRITICAL: Get AI name BEFORE clearing data for proper engine deletion
							%shellAiName = $BotInfoAiName[%aiId];
							if(%shellAiName == "") %shellAiName = $EnemyBotData[%aiId, "BotInfoAiName"];
							if(%shellAiName == "") %shellAiName = $TownBotData[%aiId, "BotInfoAiName"];
							if(%shellAiName == "") %shellAiName = fetchData(%aiId, "BotInfoAiName");
							
							// Clear bot data
							storeData(%aiId, "BotInfoAiName", "");
							storeData(%aiId, "SpawnBotInfo", "");
							storeData(%aiId, "HasLoadedAndSpawned", "");
							$EnemyBotData[%aiId, "BotInfoAiName"] = "";
							$EnemyBotData[%aiId, "SpawnBotInfo"] = "";
							$ClientData[%aiId, "BotInfoAiName"] = "";
							$ClientData[%aiId, "SpawnBotInfo"] = "";
							$BotInfoAiName[%aiId] = "";
							
							// Unregister from bot registry
							UnregisterBot(%aiId);
							
							// Delete using AI::delete if we have the name
							if(%shellAiName != "" && %shellAiName != -1 && %shellAiName != "0")
							{
								if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Deleting shell bot via AI::delete: " @ %shellAiName);
								AI::delete(%shellAiName);
							}
							else
							{
								if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - No AI name for shell bot, using deleteObject fallback");
								deleteObject(%playerObj);
								Client::setOwnedObject(%aiId, -1);
							}
							
							// Mark as recently freed to prevent immediate reuse
							$ClientIdRecentlyFreed[%aiId] = getSimTime();
							
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Cleaned up shell bot at clientId " @ %aiId);
							// After cleanup, reset aiId to force retry with different client ID
							%aiId = -1;
						}
					}
				}
			}
		}
		
		// Re-check player object after potential shell bot cleanup
		%playerObj = Client::getOwnedObject(%aiId);
		if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false" && (%playerObj == -1 || %playerObj == ""))
		{
			// Found client ID but player object is invalid - check if it was recently freed (bot died)
			%recentlyFreed = $ClientIdRecentlyFreed[%aiId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%timeSinceFreed = getSimTime() - %recentlyFreed;
				if(%timeSinceFreed < 5)
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): ERROR - Bot " @ %newName @ " spawned but player object is invalid! clientId=" @ %aiId @ " was recently freed " @ %timeSinceFreed @ "s ago. Bot died during spawn! Skipping.");
					// CRITICAL: Rollback spawn slot if this was a spawn point call
					if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
					{
						RollbackSpawnSlot(%spawnPointId);
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Rolled back spawn slot for SpawnPoint " @ %spawnPointId @ " (bot died during spawn)");
					}
					%aiId = -1; // Reset to force retry
				}
				else
				{
					// Enough time has passed, try to clean up shell bot
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): ERROR - Bot " @ %newName @ " spawned but player object is invalid! clientId=" @ %aiId @ ", Display name: '" @ %displayName @ "'. This bot will become a SHELL! Cleaning up...");
					%escapedName = String::replace(%newName, "\"", "\\\"");
					AI::delete(%escapedName);
					// NOTE: Don't call Telemetry_RecordSpawnFailed here - retries will be scheduled
					// Telemetry will be recorded when max retries is reached (the Telemetry_RecordSpawnFailed("other") "Max retries reached" call below)
					// Reset aiId so we retry
				}
			}
			else
			{
				// Not recently freed, but player object is invalid - shell bot
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): ERROR - Bot " @ %newName @ " spawned but player object is invalid! clientId=" @ %aiId @ ", Display name: '" @ %displayName @ "'. This bot will become a SHELL! Cleaning up...");
				%escapedName = String::replace(%newName, "\"", "\\\"");
				AI::delete(%escapedName);
				// NOTE: Don't call Telemetry_RecordSpawnFailed here - retries will be scheduled
				// Telemetry will be recorded when max retries is reached (the Telemetry_RecordSpawnFailed("other") "Max retries reached" call below)
				// Reset aiId so we retry
				%aiId = "";
				// CRITICAL: Rollback spawn slot if this was a spawn point call
				if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				{
					RollbackSpawnSlot(%spawnPointId);
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Rolled back spawn slot for SpawnPoint " @ %spawnPointId @ " (invalid player object)");
				}
			}
		}
	}
	
	// If still failed, retry after delay
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
	{
		// CRITICAL: Use separate retry counter for client ID lookup (different from SpawnAI retry counter)
		// This prevents conflicts with $EnemyBotSpawnRetry which is used for SpawnAI retries and "internal" flag
		%retryCount = $EnemyBotClientIdRetry[%newName];
		if(%retryCount == "")
			%retryCount = 0;
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): ERROR - Could not get client ID for bot " @ %newName @ " (displayName: " @ %displayName @ "). Retry count: " @ %retryCount @ "/3");
		if(%retryCount < 3)
		{
			$EnemyBotClientIdRetry[%newName] = %retryCount + 1;
			// Progressive retry delays: 0.5s, 1.0s, 1.5s (increasing delay for later retries)
			%retryDelay = 0.5 + (%retryCount * 0.5);
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Scheduling retry #" @ (%retryCount + 1) @ " in " @ %retryDelay @ "s");
			// CRITICAL FIX #2: Pass spawnPointId to retry call for proper transaction handling
			%spawnPointIdForRetry = "";
			if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				%spawnPointIdForRetry = %spawnPointId;
			schedule("SpawnAIGetClientId(\"" @ %newName @ "\", \"" @ %displayName @ "\", \"" @ %aiSpawnPos @ "\", \"" @ %commandIssuer @ "\", \"" @ %loadout @ "\", \"" @ %spawnPointIdForRetry @ "\");", %retryDelay);
			// CRITICAL FIX #2: Do NOT decrement counter on retry - slot is still reserved
			// Rollback will only happen if max retries are reached
			return -1;
		}
		else
		{
			// Max retries reached - give up
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Max retries reached, giving up");
			
			// CRITICAL: Rollback spawn slot if this was a spawn point call
			if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
			{
				RollbackSpawnSlot(%spawnPointId);
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Rolled back spawn slot for SpawnPoint " @ %spawnPointId @ " (max retries reached)");
			}
			
			// CRITICAL: Before cleaning up as "ghost bot", try AI::getId() one more time
			// This is the most reliable method and should work even if brute-force search failed
			%ghostBotId = "";
			%ghostFound = false;
			%useBotInsteadOfCleanup = false;
			
			// Try AI::getId() first (most reliable for newly spawned bots)
			%ghostBotId2 = AI::getId(%newName);
			if(%ghostBotId2 != -1 && %ghostBotId2 != "" && %ghostBotId2 != "False" && %ghostBotId2 != "false")
			{
				// Validate the client ID has a valid player object
				%playerObj = Client::getOwnedObject(%ghostBotId2);
				if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
				{
					// Verify it's AI-controlled (safety check)
					if(Player::isAiControlled(%ghostBotId2))
					{
						// This is a valid bot - use it instead of deleting it!
						%aiId = %ghostBotId2;
						%useBotInsteadOfCleanup = true;
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Found bot " @ %newName @ " via AI::getId() (id=" @ %aiId @ ") - using it instead of cleaning up");
					}
				}
			}
			
			// Only proceed with ghost bot cleanup if AI::getId() didn't find a valid bot
			if(!%useBotInsteadOfCleanup)
			{
				// Try multiple methods to find the ghost bot
				%ghostBotId = AI::getClientIdFromName(%newName);
				if(%ghostBotId != -1 && %ghostBotId != "" && %ghostBotId != "False" && %ghostBotId != "false")
				{
					%ghostFound = true;
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - Bot " @ %newName @ " exists via getClientIdFromName() (clientId=" @ %ghostBotId @ ") but couldn't be found during lookup. Cleaning up ghost bot...");
				}
				else
				{
					// Try display name lookup as last resort
					%checkId = NEWgetClientByName(%displayName);
					if(%checkId != -1 && %checkId != "" && %checkId != "False" && %checkId != "false")
					{
						%ghostBotId = %checkId;
						%ghostFound = true;
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - Bot " @ %newName @ " exists via display name lookup (clientId=" @ %ghostBotId @ ") but couldn't be found during lookup. Cleaning up ghost bot...");
					}
				}
			}
			
			// If we found a valid bot via AI::getId(), skip cleanup and continue with normal flow
			if(%useBotInsteadOfCleanup)
			{
				// We found a valid bot via AI::getId() - skip cleanup and continue with normal registration
				// %aiId is already set above, so we can proceed with the rest of the function
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Using bot found via AI::getId(), skipping cleanup");
			}
			// Clean up ghost bot if found (and we didn't find a valid bot to use)
			else if(%ghostFound)
			{
				// SAFETY CHECK: Verify this is actually a bot, not a player
				%isActuallyBot = false;
				%isActuallyPlayer = false;
				
				if(%ghostBotId != "" && %ghostBotId != -1)
				{
					// Check 1: Character save file (most reliable indicator of real player)
					%ghostName = Client::getName(%ghostBotId);
					if(%ghostName != "" && %ghostName != -1)
					{
						%characterFile = "temp\\" @ %ghostName @ ".cs";
						if(isFile(%characterFile))
						{
							%isActuallyPlayer = true;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): CRITICAL SAFEGUARD - Client ID " @ %ghostBotId @ " has character save file (" @ %ghostName @ ") - this is a REAL PLAYER! Skipping cleanup to prevent player deletion.");
						}
					}
					
					// Check 2: HasLoadedAndSpawned flag (only players have this)
					if(!%isActuallyPlayer)
					{
						%hasLoadedAndSpawned = fetchData(%ghostBotId, "HasLoadedAndSpawned");
						if(%hasLoadedAndSpawned == "True" || %hasLoadedAndSpawned == "true" || %hasLoadedAndSpawned == "1")
						{
							%isActuallyPlayer = true;
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): SAFETY CHECK - Client ID " @ %ghostBotId @ " has HasLoadedAndSpawned flag - this is a PLAYER, not a bot! Skipping cleanup to prevent player deletion.");
						}
					}
					
					// Check 3: SpawnBotInfo or BotInfoAiName (only bots have these)
					if(!%isActuallyPlayer)
					{
						%spawnBotInfo = fetchData(%ghostBotId, "SpawnBotInfo");
						%botInfoAiName = fetchData(%ghostBotId, "BotInfoAiName");
						if((%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1) || 
						   (%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1))
						{
							%isActuallyBot = true;
						}
					}
					
					// Check 4: Use isRPGAI() function if available
					if(!%isActuallyPlayer && !%isActuallyBot)
					{
						%checkIsRPGAI = isRPGAI(%ghostBotId);
						if(%checkIsRPGAI == "true" || %checkIsRPGAI == "1" || %checkIsRPGAI == true)
						{
							%isActuallyBot = true;
						}
					}
					
					// Check 5: Verify name matches bot pattern (additional safety)
					if(!%isActuallyPlayer && !%isActuallyBot)
					{
						if(%ghostName == "" || %ghostName == -1)
							%ghostName = Client::getName(%ghostBotId);
						if(%ghostName != "" && %ghostName != -1)
						{
							// Check if name matches bot patterns
							if(HasEnemyBotNamePrefix(%ghostName))
							{
								%isActuallyBot = true;
							}
						}
					}
				}
				
				// Only proceed with cleanup if we confirmed it's a bot and NOT a player
				if(%isActuallyPlayer)
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): ERROR - Attempted to clean up client ID " @ %ghostBotId @ " but it's a PLAYER! Aborting cleanup to prevent player deletion.");
					%ghostFound = false; // Prevent cleanup
				}
				else if(!%isActuallyBot)
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - Could not confirm client ID " @ %ghostBotId @ " is a bot. Skipping cleanup to be safe.");
					%ghostFound = false; // Prevent cleanup
				}
				
				if(%ghostFound && %isActuallyBot)
				{
					%escapedName = String::replace(%newName, "\"", "\\\"");
					AI::delete(%escapedName);
					
					// Also try to clean up via client ID if Player object exists
					if(%ghostBotId != "" && %ghostBotId != -1)
					{
						%ghostPlayerObj = Client::getOwnedObject(%ghostBotId);
						if(%ghostPlayerObj != -1 && %ghostPlayerObj != "" && isObject(%ghostPlayerObj))
						{
							// CRITICAL: Final safety check immediately before deleting Player object
							// Check 1: Character save file (most reliable)
							%finalNameCheck = Client::getName(%ghostBotId);
							if(%finalNameCheck != "" && %finalNameCheck != -1)
							{
								%finalCharacterFile = "temp\\" @ %finalNameCheck @ ".cs";
								if(isFile(%finalCharacterFile))
								{
									if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): FINAL SAFEGUARD - Client ID " @ %ghostBotId @ " has character save file (" @ %finalNameCheck @ ") - this is a REAL PLAYER! Aborting Player object deletion.");
									Telemetry_RecordSpawnFailed("clientid");
									return -1; // Exit immediately
								}
							}
							
							// Check 2: HasLoadedAndSpawned flag
							%finalCheck = fetchData(%ghostBotId, "HasLoadedAndSpawned");
							if(%finalCheck == "True" || %finalCheck == "true" || %finalCheck == "1")
							{
								if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): FINAL SAFEGUARD - Client ID " @ %ghostBotId @ " has HasLoadedAndSpawned flag - this is a PLAYER! Aborting Player object deletion.");
								Telemetry_RecordSpawnFailed("clientid");
								return -1; // Exit immediately
							}
							
							// All checks passed - safe to delete ghost bot
							// CRITICAL: Mark this client ID as recently freed to prevent immediate reuse
							// This prevents shell bots from forming when the client ID is immediately reused
							// Use validation token to prevent scheduled clear from affecting real players
							%validationToken = %newName @ "_" @ getSimTime();
							$ClientIdRecentlyFreedToken[%ghostBotId] = %validationToken;
							$ClientIdRecentlyFreed[%ghostBotId] = getSimTime();
							schedule("if($ClientIdRecentlyFreedToken[" @ %ghostBotId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %ghostBotId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %ghostBotId @ "] = \"\"; }", 30.0);
							deleteObject(%ghostPlayerObj);
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Deleted ghost Player object for " @ %newName @ " (clientId=" @ %ghostBotId @ ")");
						}
						
						// Clean up any bot data arrays (safe even if it's a player - just clears empty data)
						$EnemyBotData[%ghostBotId, "SpawnBotInfo"] = "";
						$EnemyBotData[%ghostBotId, "BotInfoAiName"] = "";
						storeData(%ghostBotId, "SpawnBotInfo", "");
						storeData(%ghostBotId, "BotInfoAiName", "");
						storeData(%ghostBotId, "botTeam", "");
						
						// Clear recently freed flag if set
						$ClientIdRecentlyFreed[%ghostBotId] = "";
					}
					
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Cleaned up ghost bot " @ %newName @ " (clientId=" @ %ghostBotId @ ")");
				}
			}
			else
			{
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Bot " @ %newName @ " was never created - no cleanup needed");
			}
			// Clear both retry counters on failure
			$EnemyBotClientIdRetry[%newName] = "";
			$EnemyBotSpawnRetry[%newName] = "";
			
			// CRITICAL FIX: Free the AI number when spawn fails completely
			// When AI::spawn() fails silently, the AI number is leaked because we never get a clientId
			// We must extract the number from the bot name and free it directly
			%aiNumber = $tmpbotn[%newName];
			if(%aiNumber != "" && %aiNumber != -1 && %aiNumber != "0")
			{
				$aiNumTable[%aiNumber] = "";
				$tmpbotn[%newName] = "";
				Telemetry_RecordAINumberFreed();
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): CRITICAL - Freed leaked AI number " @ %aiNumber @ " for failed bot " @ %newName);
			}
			else
			{
				// Try to extract number from name as fallback (e.g., "Banisher5" -> 5)
				// This handles cases where $tmpbotn wasn't set properly
				%nameLen = String::len(%newName);
				for(%i = %nameLen - 1; %i >= 0; %i--)
				{
					%char = String::getSubStr(%newName, %i, 1);
					if(%char >= "0" && %char <= "9")
					{
						// Found a digit, extract the number
						%numStart = %i;
						while(%numStart > 0)
						{
							%prevChar = String::getSubStr(%newName, %numStart - 1, 1);
							if(%prevChar >= "0" && %prevChar <= "9")
								%numStart--;
							else
								break;
						}
						%extractedNum = String::getSubStr(%newName, %numStart, %nameLen - %numStart);
						if(%extractedNum != "" && $aiNumTable[%extractedNum] != "")
						{
							$aiNumTable[%extractedNum] = "";
							$tmpbotn[%newName] = "";
							Telemetry_RecordAINumberFreed();
							if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): CRITICAL - Freed leaked AI number " @ %extractedNum @ " for failed bot " @ %newName @ " (via name extraction)");
						}
						break;
					}
				}
			}
			
			// review #6: rollback removed here - the SAME reserved slot is already
			// rolled back at the TOP of this max-retries branch ("Rolled back spawn
			// slot ... max retries reached" above), which fires on every path into
			// this branch (including the ghost-cleanup early returns). Rolling back a
			// second time here double-decremented $numAIperSpawnPoint, letting the
			// spawn loop over-spawn past the point's max until the 30s reconcile.
			Telemetry_RecordSpawnFailed("other"); // Max retries reached
			return -1;
		}
	}
		
		// VALIDATE: Check if player object actually exists
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Got client ID " @ %aiId @ ", validating player object...");
		%playerObj = Client::getOwnedObject(%aiId);
	%playerName = Client::getName(%aiId);
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Client::getOwnedObject(" @ %aiId @ ") returned: " @ %playerObj);
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Client::getName(" @ %aiId @ ") returned: '" @ %playerName @ "'");

	// SAFETY: If this clientId is registered as a town bot, check if it's ACTUALLY alive
	for(%tbIdx = 0; (%tbName = GetWord($TownBotRegistry, %tbIdx)) != -1; %tbIdx++)
	{
		%tbClient = $TownBotSpawned[%tbName];
		if(%tbClient != "" && %tbClient != -1 && %tbClient == %aiId)
		{
			// Found matching clientId - but is the town bot ACTUALLY alive?
			%tbPlayerObj = Client::getOwnedObject(%tbClient);
			%tbPlayerName = Client::getName(%tbClient);
			
			// Verify town bot is alive: has valid player object AND name matches town bot pattern
			%tbIsAlive = false;
			if(%tbPlayerObj != -1 && %tbPlayerObj != "" && isObject(%tbPlayerObj))
			{
				// Player object exists - check if the name is still the town bot's name
				// Town bots have specific display names, enemy bots have different patterns
				if(%tbPlayerName != "" && %tbPlayerName != -1)
				{
					// Check if the current player name matches what we expect for a town bot
					// NOT the enemy bot display name we're trying to spawn
					if(%tbPlayerName != %displayName && %tbPlayerName != %playerName)
					{
						// The player object at this clientId doesn't match our new enemy bot
						// It might still be the town bot - check $TownBotData
						%tbStoredName = $TownBotData[%tbClient, "BotInfoAiName"];
						if(%tbStoredName == %tbName || String::findSubStr(%tbPlayerName, %tbName) != -1)
						{
							%tbIsAlive = true;
						}
					}
				}
			}
			
			if(%tbIsAlive)
			{
				// Town bot IS alive - abort enemy spawn
				echo("ERROR: SpawnAI - ClientId " @ %aiId @ " belongs to active town bot " @ %tbName @ ". Aborting enemy spawn for " @ %newName @ ".");
		
				// CRITICAL: Delete the already-spawned enemy bot to prevent shell bot
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Deleting aborted enemy bot " @ %newName @ " due to town bot conflict");
				AI::delete(%newName);
				
				// Rollback reserved slot
				if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				{
					RollbackSpawnSlot(%spawnPointId);
				}
				// Clear enemy registry entries
				$BotRegistry[%aiId] = "";
				$BotRegistry[%aiId, "team"] = "";
				$BotRegistry[%aiId, "name"] = "";
				$BotRegistryLastSeen[%aiId] = "";
				Telemetry_RecordSpawnFailed("townbot");
				return;
			}
			else
			{
				// Town bot is DEAD/STALE - clear the stale data and allow enemy spawn
				echo("[TOWN BOT CLEANUP] Town bot " @ %tbName @ " has stale data on clientId " @ %tbClient @ " but is not alive. Clearing stale data.");
				$TownBotSpawned[%tbName] = "";
				$TownBotData[%tbClient, "BotInfoAiName"] = "";
				$TownBotData[%tbClient, "SpawnTime"] = "";
				$BotType[%tbClient] = "";
				// Continue with enemy spawn (don't return/abort)
			}
		}
	}

		if(%playerObj == -1 || %playerObj == "")
		{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): ERROR - Bot " @ %newName @ " spawned but player object is invalid! clientId=" @ %aiId @ ", Display name: '" @ %playerName @ "'. This bot will become a SHELL! Cleaning up...");
			// Clean up the broken bot - use escaped name to prevent syntax errors
		%escapedName = String::replace(%newName, "\"", "\\\"");
		AI::delete(%escapedName);
		// CRITICAL FIX #2: Rollback reserved slot if player object is invalid
		if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
		{
			RollbackSpawnSlot(%spawnPointId);
		}
			Telemetry_RecordSpawnFailed("clientid");
			return -1;
	}
	else
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Bot " @ %newName @ " spawned successfully. clientId=" @ %aiId @ ", Display name: '" @ %playerName @ "', Player object: VALID");
	}
	
	// CRITICAL: Set team IMMEDIATELY after validating player object to prevent RefreshAll() from seeing team -1
	// This must happen BEFORE any operations that might trigger RefreshAll() (like HardcodeAIskills, AI::setWeapons, etc.)
	// Determine team using centralized function
	%botTeam = fetchData(%aiId, "botTeam");
	if(%botTeam == "" || %botTeam == -1 || %botTeam == "0" || %botTeam == 0)
	{
		// Team not set yet - determine from commandIssuer or bot name
		%botTeam = DetermineBotTeam(%newName, %displayName, %commandIssuer, %aiId);
	}
	
	// Store botTeam first so RefreshAll() can restore it if needed
	storeData(%aiId, "botTeam", %botTeam);
	
	// Set team using GameBase::setTeam (Player::setTeam doesn't exist)
	// CRITICAL: Set team multiple times to ensure it takes effect before RefreshAll() runs
	if(%playerObj != -1 && %playerObj != "")
	{
		// Player object exists - set team directly on it using GameBase
		GameBase::setTeam(%playerObj, %botTeam);
		// Also set using client ID as backup
		GameBase::setTeam(%aiId, %botTeam);
		if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] SpawnAIGetClientId - Set team IMMEDIATELY to " @ %botTeam @ " for " @ %newName @ " (clientId=" @ %aiId @ ")");
		
		// CRITICAL: Verify team was actually set - if still -1, retry
		%verifyTeam = GameBase::getTeam(%playerObj);
		if(%verifyTeam == -1)
		{
			// Team is still -1 - try setting again with client ID
			GameBase::setTeam(%aiId, %botTeam);
			%verifyTeam2 = GameBase::getTeam(%aiId);
			if(%verifyTeam2 == -1)
			{
				if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] WARNING - Team still -1 after setting for " @ %newName @ " (clientId=" @ %aiId @ "). Scheduling retry...");
				// Schedule a retry to set team again after a short delay
				schedule("GameBase::setTeam(" @ %aiId @ ", " @ %botTeam @ "); GameBase::setTeam(" @ %playerObj @ ", " @ %botTeam @ ");", 0.05);
			}
		}
	}
	else
	{
		// Player object not ready yet - use GameBase::setTeam with client ID
		GameBase::setTeam(%aiId, %botTeam);
		if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] SpawnAIGetClientId - Set team IMMEDIATELY to " @ %botTeam @ " for " @ %newName @ " (clientId=" @ %aiId @ ", using client ID)");
	}
		
		// CRITICAL: Check if this client ID is already in use by a REAL PLAYER (not a bot)
		// If a real player is using this client ID, we cannot use it for a bot
		// NOTE: We check by display name patterns because isRPGAI() may not work yet (bot data not set)
		%playerName = Client::getName(%aiId);
		if(%playerName != "" && %playerName != -1)
		{
			// PRIORITY 1: Check if SpawnBotInfo is already set (from earlier in SpawnAI or previous spawn attempt)
			// This is the most reliable indicator that this is a bot, especially for Colloseum bots with non-standard display names
			%isBotName = false;
			%checkSpawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
			if(%checkSpawnBotInfo != "" && %checkSpawnBotInfo != -1 && %checkSpawnBotInfo != "0")
			{
				%isBotName = true;  // SpawnBotInfo is set, so it's definitely a bot
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Identified bot by SpawnBotInfo='" @ %checkSpawnBotInfo @ "' for clientId=" @ %aiId);
			}
			
			// PRIORITY 2: Check if internal name (%newName) matches the bot we're currently spawning
			// This catches cases where we're reusing a client ID for the same bot
			if(!%isBotName)
			{
				%checkBotInfoAiName = fetchData(%aiId, "BotInfoAiName");
				if(%checkBotInfoAiName != "" && %checkBotInfoAiName != -1 && %checkBotInfoAiName != "0" && %checkBotInfoAiName == %newName)
				{
					%isBotName = true;  // BotInfoAiName matches the bot we're spawning, so it's our bot
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Identified bot by BotInfoAiName match (expected=" @ %newName @ ", found=" @ %checkBotInfoAiName @ ") for clientId=" @ %aiId);
				}
			}
			
			// PRIORITY 3: Check if internal name (%newName) matches known bot patterns
			// This catches Colloseum bots and other bots with non-standard display names
			if(!%isBotName)
			{
				// Check for Colloseum-specific bot internal name patterns
				if(String::findSubStr(%newName, "BattleOx") == 0 ||
				   String::findSubStr(%newName, "Invader") == 0 ||
				   String::findSubStr(%newName, "MoonBreaker") == 0 ||
				   // Check for RoundOne/Two/Three anywhere in name (rank-based bots like "ImmortalRoundOne0")
				   String::findSubStr(%newName, "RoundOne") != -1 ||
				   String::findSubStr(%newName, "RoundTwo") != -1 ||
				   String::findSubStr(%newName, "RoundThree") != -1 ||
				   String::findSubStr(%newName, "roundOne") != -1 ||
				   String::findSubStr(%newName, "roundTwo") != -1 ||
				   String::findSubStr(%newName, "roundThree") != -1 ||
				   String::findSubStr(%newName, "Round") != -1 ||
				   // Use consolidated helper for enemy prefixes
				   HasEnemyBotNamePrefix(%newName))
				{
					%isBotName = true;  // Internal name matches bot patterns
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Identified bot by internal name pattern (newName=" @ %newName @ ") for clientId=" @ %aiId);
				}
			}
			
			// PRIORITY 4: Check if the display name matches bot patterns (enemy bots have prefixes like "Alien", "Admin", "Demon", "Zombie", etc.)
			// Based on NameForRace values in EnemyArmors.cs: Alien, Admin, Angel, Demon, God, Minotaur, Ogre, Orc, Pigman, Seal, Undead, Zombie
			// Also explicitly check for Colloseum bot display names: Round1, Round2, Round3, round1, round2, round3
			// And seal battle bot internal names: RoundOne, RoundTwo, RoundThree
			if(!%isBotName)
			{
				// Explicit checks for Colloseum bot display names (case-insensitive)
				if(String::ICompare(%playerName, "Round1") == 0 ||
				   String::ICompare(%playerName, "Round2") == 0 ||
				   String::ICompare(%playerName, "Round3") == 0 ||
				   String::ICompare(%playerName, "round1") == 0 ||
				   String::ICompare(%playerName, "round2") == 0 ||
				   String::ICompare(%playerName, "round3") == 0 ||
				   String::ICompare(%playerName, "RoundOne") == 0 ||
				   String::ICompare(%playerName, "RoundTwo") == 0 ||
				   String::ICompare(%playerName, "RoundThree") == 0 ||
				   String::ICompare(%playerName, "roundOne") == 0 ||
				   String::ICompare(%playerName, "roundTwo") == 0 ||
				   String::ICompare(%playerName, "roundThree") == 0 ||
				   // Check for rank-based Colloseum bot names (e.g., "ImmortalRoundOne0", "TitanRoundTwo1")
				   String::findSubStr(%playerName, "RoundOne") != -1 ||
				   String::findSubStr(%playerName, "RoundTwo") != -1 ||
				   String::findSubStr(%playerName, "RoundThree") != -1 ||
				   String::findSubStr(%playerName, "roundOne") != -1 ||
				   String::findSubStr(%playerName, "roundTwo") != -1 ||
				   String::findSubStr(%playerName, "roundThree") != -1)
				{
					%isBotName = true;
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Identified bot by explicit Colloseum/Seal Battle display name (playerName=" @ %playerName @ ") for clientId=" @ %aiId);
				}
				// Standard bot name patterns
				else if(HasEnemyBotNamePrefix(%playerName) ||
				   String::findSubStr(%playerName, "Round") != -1)  // Check for "Round" anywhere
				{
					%isBotName = true;
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Identified bot by display name pattern (playerName=" @ %playerName @ ") for clientId=" @ %aiId);
				}
			}
			
			// PRIORITY 5: Check if BotInfoAiName is already set (we just set it above, so this should be true for newly spawned bots)
			if(!%isBotName)
			{
				%checkBotInfoAiName = fetchData(%aiId, "BotInfoAiName");
				if(%checkBotInfoAiName != "" && %checkBotInfoAiName != -1 && %checkBotInfoAiName != "0")
				{
					%isBotName = true;  // BotInfoAiName is set, so it's a bot
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Identified bot by BotInfoAiName (BotInfoAiName=" @ %checkBotInfoAiName @ ") for clientId=" @ %aiId);
				}
			}
			
			// PRIORITY 6: Check if isRPGAI() returns true (bot data is set)
			if(!%isBotName && isRPGAI(%aiId))
			{
				%isBotName = true;  // Bot data exists, so it's a bot
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Identified bot by isRPGAI() for clientId=" @ %aiId);
			}
			
			// If it's not a bot name pattern and not a bot, it's a real player
			if(!%isBotName)
			{
				// This is a REAL PLAYER using this client ID - cannot spawn bot here
				echo("ERROR: SpawnAI - Client ID " @ %aiId @ " is already in use by real player '" @ %playerName @ "'. Cannot spawn enemy bot " @ %newName @ ". Aborting spawn...");
				// CRITICAL FIX #2: Rollback reserved slot if spawn failed
				if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				{
					RollbackSpawnSlot(%spawnPointId);
				}
				// Clean up any bot data that might have been set
				storeData(%aiId, "SpawnBotInfo", "");
				storeData(%aiId, "BotInfoAiName", "");
				$EnemyBotData[%aiId, "SpawnBotInfo"] = "";
				$EnemyBotData[%aiId, "BotInfoAiName"] = "";
				// Clean up the broken bot
				// Use getClientIdFromName() instead of getId() to avoid error spam
				%checkId = AI::getClientIdFromName(%newName);
				if(%checkId != -1 && %checkId != "" && %checkId != "False" && %checkId != "false")
					AI::delete(%newName);
				Telemetry_RecordSpawnFailed("clientid");
				return -1;  // Abort spawn
			}
		}
		
		// CRITICAL: Check for townbot conflicts BEFORE setting AI vars
		// This prevents enemy bots from inheriting townbot data
		%staleTownBotInfo = $TownBotData[%aiId, "BotInfoAiName"];
		%currentBotInfoAiName = $ClientData[%aiId, "BotInfoAiName"];
		%currentSpawnBotInfo = $ClientData[%aiId, "SpawnBotInfo"];
		
		// CRITICAL: First check if the current bot at this client ID is actually the enemy bot we just spawned
		// Check multiple indicators to determine if this is our enemy bot
		%isOurBot = false;
		
		// Check 1: BotInfoAiName matches our bot name
		if(%currentBotInfoAiName == %newName)
		{
			%isOurBot = true;  // This client ID is already assigned to our enemy bot
		}
		
		// Check 2: SpawnBotInfo is set (enemy bots have SpawnBotInfo, town bots don't)
		%hasSpawnBotInfo = (%currentSpawnBotInfo != "" && %currentSpawnBotInfo != -1 && %currentSpawnBotInfo != "0");
		if(%hasSpawnBotInfo)
		{
			%isOurBot = true;  // Has SpawnBotInfo, so it's an enemy bot, not a town bot
		}
		
		// Check 3: Display name matches enemy bot pattern (we just spawned this bot with this display name)
		%currentDisplayName = Client::getName(%aiId);
		if(%currentDisplayName == %displayName)
		{
			%isOurBot = true;  // Display name matches, so this is the bot we just spawned
		}
		
		// Check 4: Display name matches enemy bot patterns (God, Admin, Alien, etc.)
		if(!%isOurBot && %currentDisplayName != "" && %currentDisplayName != -1)
		{
			if(HasEnemyBotNamePrefix(%currentDisplayName))
			{
				%isOurBot = true;  // Display name matches enemy bot pattern, so it's an enemy bot
			}
		}
		
		// Check if there's an active town bot using this client ID
		// CRITICAL: Only check if this is NOT our bot
		%isTownBotClientId = false;
		if(!%isOurBot)
		{
			// Check $TownBotSpawned to see if this client ID is actually assigned to a town bot
			for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
			{
				if($TownBotSpawned[%botName] == %aiId)
				{
					%isTownBotClientId = true;
					break;
				}
			}
		}
		
		// Also verify the town bot is actually still alive by checking its player object
		%townBotPlayerObj = Client::getOwnedObject(%aiId);
		%townBotIsAlive = (%townBotPlayerObj != "" && %townBotPlayerObj != -1);
		
		// Only treat as active town bot if:
		// 1. It's NOT our bot (we just spawned it)
		// 2. It's in $TownBotSpawned
		// 3. It has a valid player object
		%hasActiveTownBot = (!%isOurBot && %isTownBotClientId && %townBotIsAlive);
		
		if(%hasActiveTownBot)
		{
			// CRITICAL FIX: Active town bot conflict detected!
			// The client ID %aiId is being used by an ACTIVE town bot.
			// We MUST NOT delete %playerObj because that IS the town bot's player object!
			// Instead, we abort this spawn attempt and let the retry logic try again
			// (which may eventually get a different client ID or the town bot may despawn naturally)
			
			%retryCount = $EnemyBotSpawnRetry[%newName];
			if(%retryCount == "")
				%retryCount = 0;
			
			if(%retryCount < 3)
			{
				echo("WARNING: SpawnAIGetClientId - Client ID " @ %aiId @ " is in use by an ACTIVE town bot. Aborting spawn for enemy bot " @ %newName @ " (attempt " @ (%retryCount + 1) @ "/3). Town bot is PROTECTED.");
				
				// DO NOT delete %playerObj - that's the TOWN BOT!
				// Just abort and retry - the engine might assign a different client ID next time
				// or the town bot may despawn naturally if players leave the zone
				
				// Increment retry counter and retry by calling SpawnAI again with all parameters
				$EnemyBotSpawnRetry[%newName] = %retryCount + 1;
				// Retry spawning - schedule a retry call to SpawnAI with all original parameters
				// Use a longer delay to give the engine time to assign a different client ID
				schedule("SpawnAI(\"" @ %newName @ "\", \"" @ %displayName @ "\", \"" @ %aiSpawnPos @ "\", \"" @ %commandIssuer @ "\");", 1.0);
				return -1; // Return -1 to indicate failure, but retry is scheduled
			}
			else
			{
				// Max retries reached - give up but DON'T delete the town bot
				echo("ERROR: SpawnAIGetClientId - Failed to spawn enemy bot " @ %newName @ " after 3 retries. Client ID " @ %aiId @ " is still in use by an ACTIVE town bot. Town bot is PROTECTED - NOT deleting.");
				// CRITICAL FIX #2: Rollback reserved slot if spawn failed
				if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				{
					RollbackSpawnSlot(%spawnPointId);
				}
				// DO NOT delete %playerObj - that's the TOWN BOT!
				// The town bot must remain alive and functional
				$EnemyBotSpawnRetry[%newName] = "";
				return -1;
			}
		}
		
		// Reset retry counter on successful spawn
		$EnemyBotSpawnRetry[%newName] = "";
		
		// CRITICAL: Double-check that there's NO active townbot before clearing any townbot data
		// This is a safety check to ensure we never clear data from an active townbot
		// IMPORTANT: Also verify the town bot is actually still alive (player object exists)
		%doubleCheckIsTownBot = false;
		for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
		{
			if($TownBotSpawned[%botName] == %aiId)
			{
				// Found a town bot entry - verify it's actually still alive
				%townBotPlayerObj = Client::getOwnedObject(%aiId);
				if(%townBotPlayerObj != "" && %townBotPlayerObj != -1 && isObject(%townBotPlayerObj))
				{
					// Town bot is still alive - this is a conflict
					%doubleCheckIsTownBot = true;
					break;
				}
				else
				{
					// Town bot entry exists but bot is dead - clear the stale entry
					echo("INFO: SpawnAI - Found stale $TownBotSpawned entry for " @ %botName @ " (clientId=" @ %aiId @ "). Bot is dead, clearing entry.");
					$TownBotSpawned[%botName] = "";
				}
			}
		}
		
		// Only clear townbot data if:
		// 1. There's no active townbot using this client ID (double-check)
		// 2. AND the player object we have is the enemy bot we just spawned (not a townbot)
		%doubleCheckPlayerObj = Client::getOwnedObject(%aiId);
		%doubleCheckIsEnemyBot = (%doubleCheckPlayerObj == %playerObj);
		
		if(!%doubleCheckIsTownBot && %doubleCheckIsEnemyBot)
		{
			// SAFE TO CLEAR: No active townbot and player object matches the enemy bot we spawned
			// Clear ALL townbot data fields to prevent enemy bots from inheriting townbot IDs
			// Clear from both $TownBotData and $ClientData for complete cleanup
			if(%staleTownBotInfo != "" && %staleTownBotInfo != "0" && %staleTownBotInfo != -1)
			{
				echo("INFO: SpawnAI - Client ID " @ %aiId @ " was previously used by a dead town bot. Clearing all stale town bot data for enemy bot " @ %newName @ ".");
			}
			
			$TownBotData[%aiId, "BotInfoAiName"] = "";
			$TownBotData[%aiId, "SpawnBotInfo"] = "";
			$TownBotData[%aiId, "SpawnTime"] = "";
			$TownBotData[%aiId, "QuestItems"] = "";
			$TownBotData[%aiId, "KeyItems"] = "";
			$TownBotData[%aiId, "Consumables"] = "";
			$TownBotData[%aiId, "Armor"] = "";
			$TownBotData[%aiId, "Accessories"] = "";
			$TownBotData[%aiId, "Other"] = "";
			// Also clear from $ClientData for backwards compatibility
			$ClientData[%aiId, "BotInfoAiName"] = "";
			$ClientData[%aiId, "SpawnBotInfo"] = "";
		}
		else
		{
			// SAFETY: Don't clear townbot data if there's any chance it's still active
			// This should never happen if the earlier checks worked, but better safe than sorry
			if(%doubleCheckIsTownBot)
			{
				echo("ERROR: SpawnAI - Attempted to clear townbot data but townbot is still active! Client ID " @ %aiId @ " is in $TownBotSpawned. Aborting enemy bot spawn.");
				// CRITICAL FIX #2: Rollback reserved slot if spawn failed
				if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				{
					RollbackSpawnSlot(%spawnPointId);
				}
				// Delete the enemy bot we just spawned
				if(%playerObj != -1 && %playerObj != "")
				{
					// CRITICAL: Mark this client ID as recently freed to prevent immediate reuse
					// This prevents shell bots from forming when the client ID is immediately reused
					// Use validation token to prevent scheduled clear from affecting real players
					%validationToken = %newName @ "_" @ getSimTime();
					$ClientIdRecentlyFreedToken[%aiId] = %validationToken;
					$ClientIdRecentlyFreed[%aiId] = getSimTime();
					schedule("if($ClientIdRecentlyFreedToken[" @ %aiId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %aiId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %aiId @ "] = \"\"; }", 30.0);
					deleteObject(%playerObj);
				}
				return -1;
			}
			else if(!%doubleCheckIsEnemyBot)
			{
				echo("ERROR: SpawnAI - Player object mismatch! Expected enemy bot but got different object. Aborting enemy bot spawn.");
				// CRITICAL FIX #2: Rollback reserved slot if spawn failed
				if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				{
					RollbackSpawnSlot(%spawnPointId);
				}
				// Delete the enemy bot we just spawned
				if(%playerObj != -1 && %playerObj != "")
				{
					// CRITICAL: Mark this client ID as recently freed to prevent immediate reuse
					// This prevents shell bots from forming when the client ID is immediately reused
					// Use validation token to prevent scheduled clear from affecting real players
					%validationToken = %newName @ "_" @ getSimTime();
					$ClientIdRecentlyFreedToken[%aiId] = %validationToken;
					$ClientIdRecentlyFreed[%aiId] = getSimTime();
					schedule("if($ClientIdRecentlyFreedToken[" @ %aiId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %aiId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %aiId @ "] = \"\"; }", 30.0);
					deleteObject(%playerObj);
				}
				return -1;
			}
		}
		
		// CRITICAL: Set $BotType FIRST before any storeData() calls
		// This ensures all data routes to $EnemyBotData, not $ClientData
		// Without this, SpawnBotInfo gets stored in wrong array and EXP distribution breaks
		$BotType[%aiId] = "enemy";
		
		// CRITICAL: Clear any stale SpawnBotInfo from previous bot using same clientId
		// This ensures enemy bots don't inherit old spawn info from previous bots
		storeData(%aiId, "SpawnBotInfo", "");
		storeData(%aiId, "SpawnTime", "");
		
		// Now set AI vars after clearing all conflicting data
		AI::setVar( %newName,  iq,  100 );
		AI::setVar( %newName,  attackMode, $AIattackMode);
		AI::setVar( %newName,  pathType, $AI::defaultPathType);
		//AI::SetVar( %newName,  seekOff, 1);
		AI::setAutomaticTargets( %newName );

	if(GetWord(%commandIssuer, 0) == "TempSpawn")
	{
		storeData(%aiId, "SpawnBotInfo", %commandIssuer);
		%team = GetWord(%commandIssuer, 4);
		GameBase::setTeam(%aiId, %team);
		// CRITICAL: Store botTeam so it can be restored if UpdateTeam() or RefreshAll() changes it
		storeData(%aiId, "botTeam", %team);
		AI::SetVar(%newName, spotDist, $AIspotDist);
		
		// Set RACE before UpdateTeam() to prevent warnings (same logic as SpawnPoint bots)
		// CONSOLIDATED: was an inline copy of the trailing-digit loop
		%guardtype = StripTrailingDigits(%newName);

		if($BotInfo[%newName, RACE] != "")
			%armor = $RaceToArmorType[$BotInfo[%newName, RACE]];
		else
			%armor = $RaceToArmorType[$NameForRace[%guardtype]];
		
		// Set RACE from armor type if we have it
		if(%armor != "" && %armor != -1 && $ArmorTypeToRace[%armor] != "")
		{
			storeData(%aiId, "RACE", $ArmorTypeToRace[%armor]);
		}
		
		// Set LCKconsequence to "miss" for enemy bots so LCK protection works
		storeData(%aiId, "LCKconsequence", "miss");
		// CRITICAL: Clear stance for enemy bots - they should not have stances enabled
		storeData(%aiId, "Stance", "");
		
		// CRITICAL: Clear stale zone data from reused clientId to prevent false "changing zones" logs
		// The bot will get its correct zone set by UpdateZone when it detects the bot's position
		storeData(%aiId, "zone", "");
		storeData(%aiId, "tmpzone", "");
		
		// Clear RemortStep to 0 to prevent stale remort from previous bot using same clientId
		storeData(%aiId, "RemortStep", 0);
		// Clear belt items to prevent stale items from previous bot using same clientId
		storeData(%aiId, "QuestItems", "");
		storeData(%aiId, "KeyItems", "");
		storeData(%aiId, "Consumables", "");
		storeData(%aiId, "Armor", "");
		storeData(%aiId, "Accessories", "");
		storeData(%aiId, "Other", "");
		// Clear flags to prevent stale flags from previous bot using same clientId
		storeData(%aiId, "noExperienceFlag", "");
		storeData(%aiId, "noDropLootbagFlag", "");
		// Clear AI behavior flags to prevent stale data from affecting AI::Periodic() targeting
		storeData(%aiId, "dumbAIflag", "");
		storeData(%aiId, "frozen", "");
		storeData(%aiId, "noBotSniff", "");
		storeData(%aiId, "SpellCastStep", "");
			//echo("[SPAWN DEBUG] SpawnAI(): TempSpawn type, team=" @ %team);
	}
	else if(GetWord(%commandIssuer, 0) == "MarkerSpawn")
	{
		storeData(%aiId, "SpawnBotInfo", %commandIssuer);
		%team = GameBase::getMapName(GetWord(%commandIssuer, 1));
		if(%team == "" || %team == -1) %team = 1; // Default to team 1 (enemy) for MarkerSpawn bots
		GameBase::setTeam(%aiId, %team);
		// CRITICAL: Store botTeam so it can be restored if UpdateTeam() or RefreshAll() changes it
		storeData(%aiId, "botTeam", %team);
		AI::SetVar(%newName, spotDist, $AIspotDist);
		// Set LCKconsequence to "miss" for enemy bots so LCK protection works
		storeData(%aiId, "LCKconsequence", "miss");
		// CRITICAL: Clear stance for enemy bots - they should not have stances enabled
		storeData(%aiId, "Stance", "");
		// Clear RemortStep to 0 to prevent stale remort from previous bot using same clientId
		storeData(%aiId, "RemortStep", 0);
		// Clear belt items to prevent stale items from previous bot using same clientId
		storeData(%aiId, "QuestItems", "");
		storeData(%aiId, "KeyItems", "");
		storeData(%aiId, "Consumables", "");
		storeData(%aiId, "Armor", "");
		storeData(%aiId, "Accessories", "");
		storeData(%aiId, "Other", "");
		// Clear flags to prevent stale flags from previous bot using same clientId
		storeData(%aiId, "noExperienceFlag", "");
		storeData(%aiId, "noDropLootbagFlag", "");
		// Clear AI behavior flags to prevent stale data from affecting AI::Periodic() targeting
		storeData(%aiId, "dumbAIflag", "");
		storeData(%aiId, "frozen", "");
		storeData(%aiId, "noBotSniff", "");
		storeData(%aiId, "SpellCastStep", "");
			//echo("[SPAWN DEBUG] SpawnAI(): MarkerSpawn type, team=" @ %team);
	}
	else if(GetWord(%commandIssuer, 0) == "SpawnPoint")
	{
			//the %commandIssuer is a spawn crystal
		%spawnPointId = GetWord(%commandIssuer, 1);
			storeData(%aiId, "SpawnBotInfo", %commandIssuer);
			
			// Verify SpawnBotInfo was stored correctly
			%verifySpawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
			//echo("[SPAWN DEBUG] SpawnAI(): Stored SpawnBotInfo='" @ %commandIssuer @ "' for aiId=" @ %aiId);
			//echo("[SPAWN DEBUG] SpawnAI(): Verified SpawnBotInfo='" @ %verifySpawnBotInfo @ "'");
			
			// CRITICAL: Counter was already incremented at the start of SpawnAI() to prevent race condition
			// Don't increment again here - just log the current counter value
			// Counter was already incremented in AI::helper() - just log current value
			%counterAfter = $numAIperSpawnPoint[%spawnPointId];
			if(%counterAfter == "")
				%counterAfter = 0;
			//echo("[SPAWN DEBUG] SpawnAI(): SpawnPoint type, spawnPointId=" @ %spawnPointId @ ", counter: " @ %counterAfter @ " (incremented in AI::helper())");
			
			// Set RACE before UpdateTeam() to prevent warnings
			// Use same logic as createAI() to derive race from bot name
			// CONSOLIDATED: was an inline copy of the trailing-digit loop
			%guardtype = StripTrailingDigits(%newName);

			if($BotInfo[%newName, RACE] != "")
				%armor = $RaceToArmorType[$BotInfo[%newName, RACE]];
			else
				%armor = $RaceToArmorType[$NameForRace[%guardtype]];
			
			// Set RACE from armor type if we have it
			if(%armor != "" && %armor != -1 && $ArmorTypeToRace[%armor] != "")
			{
				storeData(%aiId, "RACE", $ArmorTypeToRace[%armor]);
				//echo("[SPAWN DEBUG] SpawnAI(): Set RACE=" @ $ArmorTypeToRace[%armor] @ " from armor=" @ %armor);
			}
			else
			{
				//echo("[SPAWN DEBUG] SpawnAI(): WARNING - Could not set RACE (armor=" @ %armor @ ", guardtype=" @ %guardtype @ ")");
			}
			
			// CRITICAL: Set LCKconsequence to "miss" for enemy bots so LCK protection works
			// This allows bots to lose LCK instead of dying when they would be killed
			storeData(%aiId, "LCKconsequence", "miss");
			// CRITICAL: Clear stance for enemy bots - they should not have stances enabled
			storeData(%aiId, "Stance", "");
			//echo("[SPAWN DEBUG] SpawnAI(): Set LCKconsequence='miss' for enemy bot " @ %newName);
			
			// Store spawn time for zone change detection (enemy bots that change zones after 10 seconds are killed)
			storeData(%aiId, "SpawnTime", GetPersistentTime());
			
			// CRITICAL: Clear stale zone data from reused clientId to prevent false "changing zones" logs
			// The bot will get its correct zone set by UpdateZone when it detects the bot's position
			storeData(%aiId, "zone", "");
			storeData(%aiId, "tmpzone", "");
			//echo("[SPAWN DEBUG] SpawnAI(): Stored SpawnTime=" @ GetPersistentTime() @ " for enemy bot " @ %newName);
			
			// Clear RemortStep to 0 to prevent stale remort from previous bot using same clientId
			// This ensures bots without REMORT in their equipment string start at remort 0
			storeData(%aiId, "RemortStep", 0);
			//echo("[SPAWN DEBUG] SpawnAI(): Cleared RemortStep to 0 for enemy bot " @ %newName);
			
			// Clear belt items to prevent stale items from previous bot using same clientId
			// This ensures bots only have items from their equipment string, not leftover items
			storeData(%aiId, "QuestItems", "");
			storeData(%aiId, "KeyItems", "");
			storeData(%aiId, "Consumables", "");
			storeData(%aiId, "Armor", "");
			storeData(%aiId, "Accessories", "");
			storeData(%aiId, "Other", "");
			// Clear flags to prevent stale flags from previous bot using same clientId
			// This ensures bots give experience and drop lootbags unless explicitly set otherwise
			storeData(%aiId, "noExperienceFlag", "");
			storeData(%aiId, "noDropLootbagFlag", "");
			// Clear AI behavior flags to prevent stale data from affecting AI::Periodic() targeting
			// These flags can prevent bots from targeting players if they persist from previous bots
			storeData(%aiId, "dumbAIflag", "");
			storeData(%aiId, "frozen", "");
			storeData(%aiId, "noBotSniff", "");
			storeData(%aiId, "SpellCastStep", "");
			//echo("[SPAWN DEBUG] SpawnAI(): Cleared belt items for enemy bot " @ %newName);
			
			// Clear belt cache for this bot to prevent stale data from previous bot using same clientId
			$Belt::CachedList[%aiId, "QuestItems"] = "";
			$Belt::CachedList[%aiId, "KeyItems"] = "";
			$Belt::CachedList[%aiId, "Consumables"] = "";
			
		// CRITICAL: Team was already set earlier (immediately after player object validation)
		// For SpawnPoint bots, RACE is now set - update team from RACE if available (more accurate than display name pattern)
		// TempSpawn and MarkerSpawn bots already have correct team from commandIssuer, so skip update for them
		if(GetWord(%commandIssuer, 0) == "SpawnPoint")
		{
			%botTeam = fetchData(%aiId, "botTeam");
			%botRace = fetchData(%aiId, "RACE");
			if(%botRace != "" && %botRace != -1 && $TeamForRace[%botRace] != "" && $TeamForRace[%botRace] != "0" && $TeamForRace[%botRace] != 0)
			{
				// RACE is set - use team from race (more accurate than display name pattern)
				%botTeamFromRace = $TeamForRace[%botRace];
				if(%botTeamFromRace != %botTeam)
				{
					%botTeam = %botTeamFromRace;
					storeData(%aiId, "botTeam", %botTeam);
					%playerObj = Client::getOwnedObject(%aiId);
					if(%playerObj != -1 && %playerObj != "")
						GameBase::setTeam(%playerObj, %botTeam);
					else
						GameBase::setTeam(%aiId, %botTeam);
					if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] SpawnAIGetClientId - Updated team from RACE to " @ %botTeam @ " for " @ %newName @ " (clientId=" @ %aiId @ ")");
				}
			}
		}
		
		// Schedule aggressive team enforcement with multiple retries
		ScheduleTeamEnforcement(%aiId, %botTeam);
		
		// OPTIMIZED: Removed redundant VerifyEnemyBotTeam schedule - ScheduleTeamEnforcement handles this
			
			// CRITICAL: Don't call UpdateTeam() for enemy bots - it may overwrite the team we just set
			// UpdateTeam() is designed for players, not bots. Enemy bots have their team set explicitly above.
			// UpdateTeam(%aiId);
		}
		
		// CRITICAL: Store BotInfoAiName for ALL enemy bots (TempSpawn, MarkerSpawn, and SpawnPoint)
		// This must be outside the SpawnPoint-specific block so it runs for all enemy bot types
		// Store the AI name (%newName) so we can identify this bot later
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Setting BotInfoAiName for clientId " @ %aiId @ " to '" @ %newName @ "'");
		storeData(%aiId, "BotInfoAiName", %newName);
		$BotInfoAiName[%aiId] = %newName;  // Also store in direct array for fast lookup
		
		// CRITICAL FIX: Clear ExpDistributed flag for new enemy bots
		// This prevents the bot from inheriting "EXP already distributed" status from a previous bot/client
		storeData(%aiId, "ExpDistributed", "");
		
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): BotInfoAiName set successfully");
		
		AI::SetVar(%newName, spotDist, $AIspotDist);
		
		// CRITICAL: Increment enemy bot tracking counters
		$ActiveEnemyBots++;
		$TotalActiveBots++;
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Incremented counters - Total Enemy: " @ $ActiveEnemyBots @ " | Total All: " @ $TotalActiveBots);
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT TRACK] Enemy bot spawned: " @ %newName @ " (clientId=" @ %aiId @ ") | Total Enemy: " @ $ActiveEnemyBots @ " | Total All: " @ $TotalActiveBots);
		
		// CRITICAL: Register bot in centralized registry for reliable spawn counter management
		// Get spawn point and team from earlier in the function
		%registrySpawnPoint = "";
		%registryTeam = "";
		if(GetWord(%commandIssuer, 0) == "SpawnPoint")
		{
			%registrySpawnPoint = GetWord(%commandIssuer, 1);
			%registryTeam = fetchData(%aiId, "botTeam");
		}
		else if(GetWord(%commandIssuer, 0) == "TempSpawn")
		{
			%registryTeam = GetWord(%commandIssuer, 4);
		}
		else if(GetWord(%commandIssuer, 0) == "MarkerSpawn")
		{
			%registryTeam = GameBase::getMapName(GetWord(%commandIssuer, 1));
		}
		RegisterBot(%aiId, %registrySpawnPoint, %registryTeam, %newName);
		
		// CRITICAL FIX #2: Commit the reserved spawn slot (spawn was successful)
		if(%registrySpawnPoint != "" && %registrySpawnPoint != -1)
		{
			CommitSpawnSlot(%registrySpawnPoint);
		}
		
		// CRITICAL: Set HasLoadedAndSpawned flag for enemy bots
		// This allows ClearVariables() and other cleanup functions to identify fully initialized bots
		// This must be set after the bot is fully spawned and initialized
		storeData(%aiId, "HasLoadedAndSpawned", True);
		// Clear any lingering no-drop flags now that spawn succeeded
		storeData(%aiId, "noDropLootbagFlag", "");
		$EnemyBotData[%aiId, "noDropLootbagFlag"] = "";
		$ClientData[%aiId, "noDropLootbagFlag"] = "";
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Set HasLoadedAndSpawned flag for enemy bot " @ %newName @ " (clientId=" @ %aiId @ ")");
		
		// Schedule AI::setWeapons() with a small delay to ensure bot is fully initialized
		// This ensures the player object exists when GiveThisStuff() is called
		// CRITICAL: Increased delay from 0.15s to 0.2s to give engine time to process team change
		// This prevents RefreshAll() (called from HardcodeAIskills) from seeing team -1
		%currentTime = getSimTime();
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] SpawnAIGetClientId: Scheduling AI::setWeapons(" @ %newName @ ") in 0.2s @ " @ %currentTime);
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Scheduling AI::setWeapons() in 0.2s");
		schedule("AI::setWeapons(\"" @ %newName @ "\", \"" @ %loadout @ "\");", 0.2);
		
		// Clear scheduled flag on successful spawn
		$SpawnAIScheduled[%newName] = "";
		%completionTime = getSimTime();
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] SpawnAIGetClientId: COMPLETED @ " @ %completionTime @ " - returning " @ %newName);
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Spawn completed successfully, returning " @ %newName);
		
		Telemetry_RecordSpawnSuccess();  // Track successful spawn
		
		// CRITICAL: Clear client ID retry counter on successful spawn
		$EnemyBotClientIdRetry[%newName] = "";
		
		// CRITICAL: Clear spawn-in-progress flag for this spawn point when spawn completes
		if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
		{
			$SpawnPointInProgress[%spawnPointId] = "";
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): Cleared $SpawnPointInProgress flag for SpawnPoint " @ %spawnPointId);
		}
		
		return %newName;
}


// (AI::moveAhead removed - no callers anywhere in the codebase;
// old commander-order helper from stock Tribes)

//
// OK, this is the complete command callback - issued for any command sent
//    to an AI. 
//
function AI::onCommand ( %name, %commander, %command, %waypoint, %targetId, %cmdText, %cmdStatus, %cmdSequence)
{
	dbecho($dbechoMode, "AI::onCommand(" @ %name @ ", " @ %commander @ ", " @ %command @ ", " @ %waypoint @ ", " @ %targetId @ ", " @ %cmdText @ ", " @ %cmdStatus @ ", " @ %cmdSequence @ ")");

	// Use getClientIdFromName() instead of getId() to minimize error spam
	%aiId = AI::getClientIdFromName(%name);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
		return;
		
	%T = GameBase::getTeam( %aiId );
	%groupId = nameToID("MissionGroup\\Teams\\team" @ %T @ "\\AI\\" @ %name );
	
	// Validate group exists
	if(%groupId == -1)
		return;
		
	%nodeCount = Group::objectCount( %groupId );
	dbecho(2, "checking drone information...." @ " number of nodes: " @ %nodeCount);
	dbecho(2, "AI id: " @ %aiId @ " groupId: " @ %groupId);
   
	if($SinglePlayer) // || %nodeCount == 1
	{
		if( %command == 2 || %command == 1 )
		{
			// must convert waypoint location into world location.  waypoint location
			//    is given in range [0-1023, 0-1023].  
			%worldLoc = WaypointToWorld ( %waypoint );
			AI::newDirectiveWaypoint( %name, %worldLoc, 99 );
			dbecho ( 2, %name @ " IS PROCEEDING TO LOCATION " @ %worldLoc );
		}
		dbecho( 2, "AI::OnCommand() issued to  " @ %name @ "  with parameters: " );
		dbecho( 3, "Cmdr:        " @ %commander );
		dbecho( 3, "Command:     " @ %command );
		dbecho( 3, "Waypoint:    " @ %waypoint );
		dbecho( 3, "TargetId:    " @ %targetId );
		dbecho( 3, "cmdText:     " @ %cmdText );
		dbecho( 3, "cmdStatus:   " @ %cmdStatus );
		dbecho( 3, "cmdSequence: " @ %cmdSequence );
	}
	else
		return;   
}

// Play the given wave file FROM %source to %DEST.  The wave name is JUST the basic wave
// name without voice base info (which it will grab for you from the source clientId).
// Basically does some string fiddling for you.
//
// Example:
//    Ai::soundHelper( 2051, 2049, cheer3 );
//
function Ai::soundHelper( %sourceId, %destId, %waveFileName )
{
	dbecho($dbechoMode, "Ai::soundHelper(" @ %sourceId @ ", " @ %destId @ ", " @ %waveFileName @ ")");

	// Validate parameters - prevent bots from sending invalid messages
	if(%sourceId == "" || %sourceId == -1 || %destId == "" || %destId == -1)
		return;
	
	if(%waveFileName == "" || %waveFileName == -1)
		return;

	%voiceBase = Client::getVoiceBase( %sourceId );
	if(%voiceBase == "" || %voiceBase == -1 || %voiceBase == "1")
	{
		// Invalid voice base - don't send message to prevent "1" spam
		dbecho( 2, "WARNING: Ai::soundHelper - Invalid voice base '" @ %voiceBase @ "' for sourceId " @ %sourceId @ ", skipping message");
		return;
	}

	%wName = strcat( "~w", %voiceBase );
	%wName = strcat( %wName, ".w" );
	%wName = strcat( %wName, %waveFileName );
	%wName = strcat( %wName, ".wav" );

	// Validate final message - if it's just "1" or invalid, don't send
	if(%wName == "" || %wName == -1 || %wName == "1" || String::len(%wName) < 5)
	{
		dbecho( 2, "WARNING: Ai::soundHelper - Invalid message '" @ %wName @ "' for sourceId " @ %sourceId @ ", skipping");
		return;
	}

	dbecho( 2, "Trying to play " @ %wName );

	Client::sendMessage( %destId, 0, %wName );
}


//=============================================================================
// BOT CLEANUP HELPER FUNCTIONS
// These functions consolidate repetitive cleanup code from AI::onDroneKilled
//=============================================================================

// Helper: Find client ID from AI name using multi-tier fallback
// Returns: Client ID or -1 if not found
function Bot_GetClientIdFromAiName(%aiName)
{
	// Tier 1: AI::getClientIdFromName (works for both Drones and Player objects)
	%aiId = AI::getClientIdFromName(%aiName);
	if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
		return %aiId;

	// Tier 2: Search BotInfoAiName via GetEveryoneIdList
	%list = GetEveryoneIdList();
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%checkId = GetWord(%list, %i);
		if(fetchData(%checkId, "BotInfoAiName") == %aiName)
			return %checkId;
	}

	// (Former Tier 3 removed: it iterated Client::getFirst/getNext, which only returns
	// real player connections - never AI bots - so it could not find what this function
	// looks for. The range loop below covers the whole BaseRep id space.)

	// Tier 4: Fallback range loop (allocatable BaseRep range; 2048 = server slot, skipped)
	for(%checkId = $BaseRepClientIdMin + 1; %checkId <= $BaseRepClientIdMax; %checkId++)
	{
		if($EnemyBotData[%checkId, "BotInfoAiName"] == %aiName)
			return %checkId;
		if($ClientData[%checkId, "BotInfoAiName"] == %aiName)
			return %checkId;
	}

	return -1;
}

// Helper: Determine bot type from available data
// Returns: "enemy", "town", or "unknown"
function Bot_DetermineType(%aiId, %spawnBotInfo, %botInfoAiName)
{
	// Enemy bot: Has SpawnBotInfo (primary indicator)
	if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		return "enemy";

	// Town bot: BotInfoAiName starts with "TownBot_"
	if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
	{
		if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
			return "town";
	}

	// Infer from display name (fallback)
	%displayName = Client::getName(%aiId);
	if(%displayName != "" && %displayName != -1)
	{
		if(HasEnemyBotNamePrefix(%displayName))
		{
			return "enemy";
		}
	}

	return "unknown";
}

// Helper: Clear all storeData fields for a bot
function Bot_ClearStoreData(%aiId, %botType)
{
	if($SpellCastToken[%aiId] == "" || $SpellCastToken[%aiId] == -1)
		$SpellCastToken[%aiId] = 0;
	$SpellCastToken[%aiId]++;
	$PowerCloudCacheToken[%aiId] = "";

	// Common fields (all bot types)
	storeData(%aiId, "SpawnBotInfo", "");
	storeData(%aiId, "SpawnTime", "");
	storeData(%aiId, "BotInfoAiName", "");
	storeData(%aiId, "RemortStep", "");
	storeData(%aiId, "QuestItems", "");
	storeData(%aiId, "KeyItems", "");
	storeData(%aiId, "Consumables", "");
	storeData(%aiId, "Armor", "");
	storeData(%aiId, "Accessories", "");
	storeData(%aiId, "Other", "");
	storeData(%aiId, "zone", "");
	storeData(%aiId, "tmpzone", "");
	storeData(%aiId, "SpawnOriginZoneID", "");
	storeData(%aiId, "botTeam", "");
	storeData(%aiId, "AITarget", "");
	storeData(%aiId, "AILastDestination", "");
	storeData(%aiId, "AILastLoggedDist", "");
	storeData(%aiId, "AIMovementLoopRunning", "");
	storeData(%aiId, "dumbAIflag", "");
	storeData(%aiId, "frozen", "");
	storeData(%aiId, "RACE", "");
	storeData(%aiId, "SpawnInvuln", "");

	if(%botType == "enemy")
	{
		// Enemy-specific fields
		storeData(%aiId, "noExperienceFlag", "");
		storeData(%aiId, "noDropLootbagFlag", "");
		storeData(%aiId, "noBotSniff", "");
		storeData(%aiId, "SpellCastStep", "");
		storeData(%aiId, "LCKconsequence", "");
		storeData(%aiId, "AIattackMarker", "");
		storeData(%aiId, "BotAttackLoopActive", "");
		storeData(%aiId, "SealBattleBot", "");
		storeData(%aiId, "SealBattleScaledRound", "");
		storeData(%aiId, "DailyEliteOwner", "");	// daily elite tag (DailyQuest.cs) - stale value on a reused slot would credit the old owner for a random bot kill
		storeData(%aiId, "WeeklyBossTag", "");	// weekly boss tag (WeeklyBoss.cs) - same stale-slot protection
		storeData(%aiId, "SealBattleOriginalLVL", "");
		storeData(%aiId, "SealBattleOriginalRemortStep", "");
		storeData(%aiId, "SealBattleOriginalEndurance", "");
		storeData(%aiId, "SealBattleOriginalEnergy", "");
		storeData(%aiId, "SealBattleOriginalWeightCapacity", "");
		storeData(%aiId, "AImoveChance", "");
		
		// CRITICAL: Clear $SealBattleScaledStats array directly (storeData doesn't access this)
		// This prevents stale seal battle stats from contaminating regular enemy bots
		$SealBattleScaledStats[%aiId, "DEF"] = "";
		$SealBattleScaledStats[%aiId, "MDEF"] = "";
		$SealBattleScaledStats[%aiId, "ATK"] = "";
		$SealBattleScaledStats[%aiId, "DMG"] = "";
		$SealBattleScaledStats[%aiId, "MaxHP"] = "";
		$SealBattleScaledStats[%aiId, "MaxMANA"] = "";
		$SealBattleScaledStats[%aiId, "LCK"] = "";
		$SealBattleScaledStats[%aiId, "round"] = "";
	}
	else if(%botType == "town")
	{
		// Town-specific fields
		storeData(%aiId, "NoDropLoot", "");
		storeData(%aiId, "MountWeaponOnSpawn", "");
		storeData(%aiId, "MountWeaponOnTalk", "");
		storeData(%aiId, "ShowIdleMessage", "");
		storeData(%aiId, "LastInteractionTime", "");
	}
}

// Helper: Clear all array data for a bot
function Bot_ClearArrayData(%aiId, %botType)
{
	// Clear direct array
	$BotInfoAiName[%aiId] = "";
	$BotType[%aiId] = "";
	$BotFrozen[%aiId] = "";

	// Clear directives (0-99)
	for(%d = 0; %d <= 99; %d++)
		$aidirectiveTable[%aiId, %d] = "";

	// Clear belt cache
	$Belt::CachedList[%aiId, "QuestItems"] = "";
	$Belt::CachedList[%aiId, "KeyItems"] = "";
	$Belt::CachedList[%aiId, "Consumables"] = "";
	$Belt::CachedList[%aiId, "Armor"] = "";
	$Belt::CachedList[%aiId, "Accessories"] = "";
	$Belt::CachedList[%aiId, "Other"] = "";

	if(%botType == "enemy")
	{
		// Clear $EnemyBotData
		$EnemyBotData[%aiId, "SpawnBotInfo"] = "";
		$EnemyBotData[%aiId, "SpawnTime"] = "";
		$EnemyBotData[%aiId, "BotInfoAiName"] = "";
		$EnemyBotData[%aiId, "zone"] = "";
		$EnemyBotData[%aiId, "tmpzone"] = "";
		$EnemyBotData[%aiId, "SpawnOriginZoneID"] = "";
		$EnemyBotData[%aiId, "RemortStep"] = "";
		$EnemyBotData[%aiId, "QuestItems"] = "";
		$EnemyBotData[%aiId, "KeyItems"] = "";
		$EnemyBotData[%aiId, "Consumables"] = "";
		$EnemyBotData[%aiId, "Armor"] = "";
		$EnemyBotData[%aiId, "Accessories"] = "";
		$EnemyBotData[%aiId, "Other"] = "";
		$EnemyBotData[%aiId, "noExperienceFlag"] = "";
		$EnemyBotData[%aiId, "noDropLootbagFlag"] = "";
		$EnemyBotData[%aiId, "dumbAIflag"] = "";
		$EnemyBotData[%aiId, "frozen"] = "";
		$EnemyBotData[%aiId, "noBotSniff"] = "";
		$EnemyBotData[%aiId, "SpellCastStep"] = "";
		$EnemyBotData[%aiId, "LCKconsequence"] = "";
		$EnemyBotData[%aiId, "AIattackMarker"] = "";
		$EnemyBotData[%aiId, "SealBattleBot"] = "";
		$EnemyBotData[%aiId, "SealBattleScaledRound"] = "";
		$EnemyBotData[%aiId, "SealBattleOriginalLVL"] = "";
		$EnemyBotData[%aiId, "SealBattleOriginalRemortStep"] = "";
		$EnemyBotData[%aiId, "SealBattleOriginalEndurance"] = "";
		$EnemyBotData[%aiId, "SealBattleOriginalEnergy"] = "";
		$EnemyBotData[%aiId, "SealBattleOriginalWeightCapacity"] = "";
		$EnemyBotData[%aiId, "AImoveChance"] = "";
		$EnemyBotData[%aiId, "RACE"] = "";
		$EnemyBotData[%aiId, "SpawnInvuln"] = "";
		$EnemyBotData[%aiId, "DEF"] = "";
		$EnemyBotData[%aiId, "MDEF"] = "";
		$EnemyBotData[%aiId, "ATK"] = "";
		$EnemyBotData[%aiId, "DMG"] = "";
		
		// CRITICAL: Clear $SealBattleScaledStats array (separate from $EnemyBotData)
		// This prevents stale seal battle stats from contaminating regular enemy bots
		$SealBattleScaledStats[%aiId, "DEF"] = "";
		$SealBattleScaledStats[%aiId, "MDEF"] = "";
		$SealBattleScaledStats[%aiId, "ATK"] = "";
		$SealBattleScaledStats[%aiId, "DMG"] = "";
		$SealBattleScaledStats[%aiId, "MaxHP"] = "";
		$SealBattleScaledStats[%aiId, "MaxMANA"] = "";
		$SealBattleScaledStats[%aiId, "LCK"] = "";
		$SealBattleScaledStats[%aiId, "round"] = "";
	}
	else if(%botType == "town")
	{
		// Clear $TownBotData
		$TownBotData[%aiId, "BotInfoAiName"] = "";
		$TownBotData[%aiId, "SpawnBotInfo"] = "";
		$TownBotData[%aiId, "SpawnTime"] = "";
		$TownBotData[%aiId, "QuestItems"] = "";
		$TownBotData[%aiId, "KeyItems"] = "";
		$TownBotData[%aiId, "Consumables"] = "";
		$TownBotData[%aiId, "Armor"] = "";
		$TownBotData[%aiId, "Accessories"] = "";
		$TownBotData[%aiId, "Other"] = "";
	}

	// Always clear $ClientData for backwards compatibility
	$ClientData[%aiId, "SpawnBotInfo"] = "";
	$ClientData[%aiId, "SpawnTime"] = "";
	$ClientData[%aiId, "BotInfoAiName"] = "";
	$ClientData[%aiId, "SealBattleBot"] = "";
	$ClientData[%aiId, "SealBattleScaledRound"] = "";
	$ClientData[%aiId, "SealBattleOriginalLVL"] = "";
	$ClientData[%aiId, "SealBattleOriginalRemortStep"] = "";
	$ClientData[%aiId, "SealBattleOriginalEndurance"] = "";
	$ClientData[%aiId, "SealBattleOriginalEnergy"] = "";
	$ClientData[%aiId, "SealBattleOriginalWeightCapacity"] = "";
	$ClientData[%aiId, "AImoveChance"] = "";
	$ClientData[%aiId, "RACE"] = "";
	$ClientData[%aiId, "SpawnInvuln"] = "";
	$ClientData[%aiId, "DEF"] = "";
	$ClientData[%aiId, "MDEF"] = "";
	$ClientData[%aiId, "ATK"] = "";
	$ClientData[%aiId, "DMG"] = "";
}


function AI::onDroneKilled(%aiName)
{
	dbecho($dbechoMode, "AI::onDroneKilled(" @ %aiName @ ")");

	if(!$SinglePlayer)
	{
		// STEP 1: Find client ID using helper function (consolidates 4-tier fallback)
		%aiId = Bot_GetClientIdFromAiName(%aiName);
		
		if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
		{
			// Bot already cleaned up (normal for zone despawns)
			if($AI_DEBUG_ENABLED) echo("[BOT CLEANUP] AI::onDroneKilled(): Bot " @ %aiName @ " already cleaned up (normal for zone despawns)");
			return;
		}
		
		// STEP 2: Verify this is a bot (not a real player)
		if(!IsSafeToModify(%aiId, "AI::onDroneKilled"))
			return;
		
		// STEP 3: Check if already processed (prevent double-counting)
		%deathProcessed = fetchData(%aiId, "DeathProcessed");
		if(%deathProcessed != "" && %deathProcessed != "0" && %deathProcessed != -1)
			return;
		
		// Mark as processing IMMEDIATELY
		storeData(%aiId, "DeathProcessed", "processing");
		
		// STEP 4: Get player object and name for cleanup
		%playerObj = Client::getOwnedObject(%aiId);
		%playerName = Client::getName(%aiId);
		
		// STEP 5: Clear directive #99 immediately
		if($Directive99RemovalAttempted[%aiName] != "true" && $Directive99RemovalAttempted[%aiName] != "1")
			AI::newDirectiveRemove(%aiName, 99);
		
		// STEP 6: Extract bot data BEFORE any cleanup
		%spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
		if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
		{
			%spawnBotInfo = $EnemyBotData[%aiId, "SpawnBotInfo"];
			if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
				%spawnBotInfo = $ClientData[%aiId, "SpawnBotInfo"];
		}
		
		%botInfoAiName = fetchData(%aiId, "BotInfoAiName");
		if(%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1)
		{
			%botInfoAiName = $EnemyBotData[%aiId, "BotInfoAiName"];
			if(%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1)
				%botInfoAiName = $ClientData[%aiId, "BotInfoAiName"];
		}
		
		// Clear spawn scheduled flag
		if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
			$SpawnAIScheduled[%botInfoAiName] = "";
		
		// Re-check botInfoAiName from additional sources
		if(%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1)
		{
			%botInfoAiName = $BotInfoAiName[%aiId];
			if(%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1)
			{
				%botInfoAiName = $TownBotData[%aiId, "BotInfoAiName"];
				if(%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1)
					%botInfoAiName = $EnemyBotData[%aiId, "BotInfoAiName"];
			}
		}
		
		%team = fetchData(%aiId, "botTeam");
		%aiNumber = $tmpbotn[%aiName];
		storeData(%aiId, "botTeam", "");
		
		// STEP 7: Determine bot type using helper function
		%botType = Bot_DetermineType(%aiId, %spawnBotInfo, %botInfoAiName);
		
		// STEP 8: Process based on bot type
		if(%botType == "enemy")
		{
			// === ENEMY BOT CLEANUP ===
			
			// Free AI number
			if(%aiNumber != "" && %aiNumber != -1 && %aiNumber != "0")
			{
				$aiNumTable[%aiNumber] = "";
				$tmpbotn[%aiName] = "";
			}
			
			// Unregister from bot registry
			// review #29: pass the dying bot's %playerObj (fetched above) as excludeObject
			// so UnregisterBot routes the BotGroup + MissionCleanup orphan scans through
			// the DEFERRED path instead of running them synchronously here. Without it,
			// an AoE/zone despawn killing several bots in one tick ran multiple full
			// MissionCleanup scans (hundreds-thousands of objects) back-to-back inside the
			// death callbacks - a visible hitch, the exact burst the deferred path exists for.
			UnregisterBot(%aiId, %playerObj);

			// Mark as processed
			storeData(%aiId, "DeathProcessed", "enemy");
			
			// Clear all bot data using helper functions
			Bot_ClearStoreData(%aiId, "enemy");
			Bot_ClearArrayData(%aiId, "enemy");
			
			// Clear seal battle spawn invuln by name
			%displayName = Client::getName(%aiId);
			if(%displayName != "" && %displayName != -1)
			{
				if(String::findSubStr(%displayName, "SealFighter") == 0 || 
				   String::findSubStr(%displayName, "SealMage") == 0 || 
				   String::findSubStr(%displayName, "SealGuardian") == 0)
				{
					$SpawnInvulnByName[%displayName] = "";
				}
			}
			
			// Schedule client ID reuse
			%validationToken = %aiName @ "_" @ getSimTime();
			$ClientIdRecentlyFreedToken[%aiId] = %validationToken;
			schedule("if($ClientIdRecentlyFreedToken[" @ %aiId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %aiId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %aiId @ "] = \"\"; }", 1.5);
			
			// Pet cleanup
			// BUGFIX: only run owner-side cleanup for actual pets - this used to write to a
			// ""-keyed data row and message an invalid client on EVERY enemy bot death
			$PetList = RemoveFromCommaList($PetList, %aiId);
			%petowner = fetchData(%aiId, "petowner");
			if(%petowner != "" && %petowner != -1 && %petowner != "0")
			{
				storeData(%petowner, "PersonalPetList", RemoveFromCommaList(fetchData(%petowner, "PersonalPetList"), %aiId));
				Client::sendMessage(%petowner, $MsgRed, Client::getName(%aiId) @ " was slain!");
				storeData(%aiId, "petowner", "");
			}
			
			// Bot group cleanup
			%b = AI::IsInWhichBotGroup(%aiId);
			if(%b != -1)
				AI::RemoveBotFromBotGroup(%aiId, %b);
			
			// Add to graveyard and schedule deletion
			AddToGraveyard(%aiName, %aiId);
			if(%aiName != "" && %aiName != -1 && %aiName != "0")
			{
				%escapedAiName = String::replace(%aiName, "\"", "\\\"");
				schedule("AI::delete(\"" @ %escapedAiName @ "\"); RemoveFromGraveyard(\"" @ %escapedAiName @ "\", " @ %aiId @ ");", 1.0);
				$Directive99RemovalAttempted[%aiName] = "";
			}
			
			// Delete player object
			if(%playerObj != -1 && %playerObj != "")
				schedule("if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ ");", 1.0);
		}
		else if(%botType == "town")
		{
			// === TOWN BOT CLEANUP ===
			
			// Mark as processed
			storeData(%aiId, "DeathProcessed", "town");
			
			// CRITICAL: Store the display name for respawn BEFORE any cleanup
			// The original display name comes from the mission marker (e.g., "Yuliple banker")
			// and may differ from $BotInfo[NAME] (which is just "banker")
			%displayNameForRespawn = Client::getName(%aiId);
			
			// Extract bot name from BotInfoAiName
			%botName = "";
			if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
			{
				%botName = %botInfoAiName;
				if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
					%botName = String::getSubStr(%botInfoAiName, 8, 999);
			}
			else if(%aiName != "" && %aiName != "0" && %aiName != -1)
			{
				if(String::findSubStr(%aiName, "TownBot_") == 0)
					%botName = String::getSubStr(%aiName, 8, 999);
				else
					%botName = %aiName;
			}
			
			// Fallback: Search $TownBotSpawned by clientId
			if(%botName == "" || %botName == "0" || %botName == -1)
			{
				for(%i = 0; (%regBotName = GetWord($TownBotRegistry, %i)) != -1; %i++)
				{
					if($TownBotSpawned[%regBotName] == %aiId)
					{
						%botName = %regBotName;
						break;
					}
				}
			}
			
			// Clear all bot data using helper functions
			Bot_ClearStoreData(%aiId, "town");
			Bot_ClearArrayData(%aiId, "town");
			
			// Remove from TownBotList
			$TownBotList = RemoveFromCommaList($TownBotList, %aiId);
			
			// Clear spawn tracking
			if(%botName != "" && %botName != -1 && %botName != "0")
			{
				$TownBotSpawned[%botName] = "";
				// Store display name for respawn (preserves zone prefix like "Yuliple banker")
				if(%displayNameForRespawn != "" && %displayNameForRespawn != -1)
					$TownBotDisplayName[%botName] = %displayNameForRespawn;
				echo("[TOWN BOT CLEANUP] AI::onDroneKilled - Cleared $TownBotSpawned[" @ %botName @ "] for clientId " @ %aiId @ " (displayName=" @ %displayNameForRespawn @ ")");
			}
			else
			{
				// Fallback: Search and clear by clientId
				for(%i = 0; (%regBotName = GetWord($TownBotRegistry, %i)) != -1; %i++)
				{
					if($TownBotSpawned[%regBotName] == %aiId)
					{
						$TownBotSpawned[%regBotName] = "";
						break;
					}
				}
			}
			
			// Bot group cleanup
			%b = AI::IsInWhichBotGroup(%aiId);
			if(%b != -1)
				AI::RemoveBotFromBotGroup(%aiId, %b);
			
			// Free AI number
			if(%aiNumber != "" && %aiNumber != -1 && %aiNumber != "0")
			{
				$aiNumTable[%aiNumber] = "";
				$tmpbotn[%aiName] = "";
			}
			else
			{
				%fallbackNumber = $tmpbotn[%aiName];
				if(%fallbackNumber != "" && %fallbackNumber != -1 && %fallbackNumber != "0")
				{
					$aiNumTable[%fallbackNumber] = "";
					$tmpbotn[%aiName] = "";
				}
			}
			
			// Schedule AI deletion
			if(%aiName != "" && %aiName != -1 && %aiName != "0")
			{
				%escapedAiName = String::replace(%aiName, "\"", "\\\"");
				schedule("AI::delete(\"" @ %escapedAiName @ "\");", 1.0);
				$Directive99RemovalAttempted[%aiName] = "";
			}
			
			// Mark client ID as recently freed
			$ClientIdRecentlyFreed[%aiId] = getSimTime();
			
			// Clear retry counters
			if(%botName != "" && %botName != -1 && %botName != "0")
			{
				$TownBotRetryGetAIIdCount[%botName] = "";
				$TownBotSpawnRetry[%botName] = "";
			}
			
			// Delete player object (NO respawn schedule - town bots respawn via SpawnZoneBots() when players enter zone)
			if(%playerObj != -1 && %playerObj != "")
				schedule("if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ ");", 1.0);
			
			// NOTE: Town bots do NOT use AI::setupAI() for respawning!
			// Instead, we schedule SpawnSingleZoneBot() if players are still in the zone.
			// This allows town bots to respawn while players remain in the zone.
			%zoneIndex = "";
			if(%botName != "" && %botName != "0" && %botName != -1)
				%zoneIndex = $TownBotZone[%botName];
			
			if(%zoneIndex != "" && %zoneIndex != -1 && %zoneIndex != "0")
			{
				// Check if players are still in this zone
				%zonePlayers = $ZonePlayerCount[%zoneIndex];
				if(%zonePlayers > 0)
				{
					// Schedule respawn after 60 seconds if players are still in zone
					echo("[TOWN BOT RESPAWN] Scheduling respawn for " @ %botName @ " in zone " @ %zoneIndex @ " (60s delay, " @ %zonePlayers @ " players in zone)");
					schedule("if($ZonePlayerCount[" @ %zoneIndex @ "] > 0) SpawnSingleZoneBot(\"" @ %botName @ "\", " @ %zoneIndex @ ");", 60);
				}
				else
				{
					echo("[TOWN BOT RESPAWN] No players in zone " @ %zoneIndex @ " - " @ %botName @ " will respawn when players re-enter");
				}
			}
		}
		else
		{
			// === UNKNOWN BOT TYPE ===
			storeData(%aiId, "DeathProcessed", "unknown");
			
			if(%botType != "enemy")
				echo("WARNING: AI::onDroneKilled - Bot " @ %aiName @ " (clientId=" @ %aiId @ ") is unknown type. SpawnBotInfo='" @ %spawnBotInfo @ "', BotInfoAiName='" @ %botInfoAiName @ "'");
			
			// Minimal cleanup
			storeData(%aiId, "SpawnBotInfo", "");
			storeData(%aiId, "BotInfoAiName", "");
			storeData(%aiId, "AIattackMarker", "");
			$BotInfoAiName[%aiId] = "";
			
			if(%aiName != "" && %aiName != -1 && %aiName != "0")
			{
				%escapedAiName = String::replace(%aiName, "\"", "\\\"");
				schedule("AI::delete(\"" @ %escapedAiName @ "\");", 1.0);
				$Directive99RemovalAttempted[%aiName] = "";
			}
			
			schedule("AI::setupAI(" @ %aiName @ ", " @ %team @ ");", 60);
		}
		
		// Clear DeathProcessed flag after delay
		schedule("storeData(" @ %aiId @ ", \"DeathProcessed\", \"\");", 5.0);
	}
	else
	{
		dbecho(2, "Non training callback called from Training");
	}
}

function getAInumber()
{
	dbecho($dbechoMode, "getAInumber()");

	%currentTime = getSimTime();
	
	for(%i = 0; %i <= 5000; %i++)
	{
		if($aiNumTable[%i] == "")
		{
			// COOLDOWN CHECK: Skip numbers that were freed recently (within 3 seconds)
			// This prevents "An AI named X already exists!" errors when the engine
			// hasn't finished cleaning up the old AI before we try to spawn a new one
			%cooldownTime = $AINumberCooldown[%i];
			if(%cooldownTime != "" && %cooldownTime != -1)
			{
				%timeSinceFreed = %currentTime - %cooldownTime;
				if(%timeSinceFreed < 3)
				{
					// Still on cooldown - skip this number
					continue;
				}
				else
				{
					// Cooldown expired - clear the flag
					$AINumberCooldown[%i] = "";
				}
			}
			return %i;
		}
	}

	// All 5000 slots taken or cooling down - explicit failure instead of falling
	// off the end (which returned "" and produced a bare-guardtype name collision)
	echo("ERROR: getAInumber - no free AI numbers available (all in use or on cooldown)");
	return -1;
}
function setAInumber(%aiName, %n)
{
	dbecho($dbechoMode, "setAInumber(" @ %aiName @ ", " @ %n @ ")");

	$aiNumTable[%n] = True;
	$tmpbotn[%aiName] = %n;
	// review #8: stamp the reservation time (getSimTime is SECONDS) so
	// PeriodicAINumberReconciliation won't free a number that was JUST reserved but
	// whose BotInfoAiName hasn't been stored yet (the ~0.5-3.5s spawn-registration
	// window) - freeing it there let getAInumber hand the same number to the next
	// spawn, producing duplicate names / delete of the live in-flight bot.
	$aiNumReservedTime[%n] = getSimTime();
}


//------ BotGroup stuff ---------------------------------

function AI::IsInWhichBotGroup(%aiId)
{
	dbecho($dbechoMode, "AI::IsInWhichBotGroup(" @ %aiId @ ")");

	for(%i = 0; (%a = GetWord($BotGroups, %i)) != -1; %i++)
	{
		for(%j = 0; (%b = GetWord($tmpBotGroup[%a], %j)) != -1; %j++)
		{
			if(%b == %aiId)
				return %a;
		}
	}
	return -1;
}

function AI::CreateBotGroup(%group)
{
	dbecho($dbechoMode, "AI::CreateBotGroup(" @ %group @ ")");

	$BotGroups = $BotGroups @ %group @ " ";
	$tmpBotGroup[%group] = "";
}

function AI::DiscardBotGroup(%group)
{
	dbecho($dbechoMode, "AI::DiscardBotGroup(" @ %group @ ")");

	for(%i = 0; (%a = GetWord($tmpBotGroup[%group], %i)) != -1; %i++)
		storeData(%a, "botAttackMode", 1);

	$BotGroups = FixStuffString($BotGroups);
	$BotGroups = String::replace($BotGroups, " " @ %group @ " ", " ");
	$tmpBotGroup[%group] = "";
}

function AI::CountBotGroupMembers(%group)
{
	dbecho($dbechoMode, "AI::CountBotGroupMembers(" @ %group @ ")");

	for(%i = 0; (%a = GetWord($tmpBotGroup[%group], %i)) != -1; %i++){}
	return %i;
}

function AI::IsInBotGroup(%aiId, %group)
{
	dbecho($dbechoMode, "AI::IsInBotGroup(" @ %aiId @ ", " @ %group @ ")");

	for(%i = 0; (%a = GetWord($tmpBotGroup[%group], %i)) != -1; %i++)
	{
		if(%aiId == %a)
			return True;
	}
	return False;
}

function AI::BotGroupExists(%group)
{
	dbecho($dbechoMode, "AI::BotGroupExists(" @ %group @ ")");

	for(%i = 0; (%a = GetWord($BotGroups, %i)) != -1; %i++)
	{
		if(%a == %group)
			return True;
	}
	return False;
}

function AI::RemoveBotFromBotGroup(%aiId, %group)
{
	dbecho($dbechoMode, "AI::RemoveBotFromGroup(" @ %aiId @ ", " @ %group @ ")");

	// CRITICAL SAFEGUARD: Prevent removing players from bot groups
	if(!isRPGAI(%aiId) && !Player::isAiControlled(%aiId))
	{
		echo("WARNING: AI::RemoveBotFromBotGroup - Attempted to remove player clientId " @ %aiId @ " from bot group. Skipping to prevent player data modification.");
		return;
	}
	
	// ADDITIONAL SAFEGUARD: Only remove bots that have fully loaded and spawned
	if(!fetchData(%aiId, "HasLoadedAndSpawned"))
	{
		echo("WARNING: AI::RemoveBotFromBotGroup - Bot clientId " @ %aiId @ " has not loaded and spawned yet. Skipping to prevent data corruption.");
		return;
	}

	$tmpBotGroup[%group] = String::Replace($tmpBotGroup[%group], %aiId @ " ", "");
	storeData(%aiId, "botAttackMode", 1);
}

function AI::AddBotToBotGroup(%aiId, %group)
{
	dbecho($dbechoMode, "AI::AddBotToBotGroup(" @ %aiId @ ", " @ %group @ ")");

	// CRITICAL SAFEGUARD: Prevent adding players to bot groups
	if(!isRPGAI(%aiId) && !Player::isAiControlled(%aiId))
	{
		echo("WARNING: AI::AddBotToBotGroup - Attempted to add player clientId " @ %aiId @ " to bot group. Skipping to prevent player data modification.");
		return;
	}
	
	// ADDITIONAL SAFEGUARD: Only add bots that have fully loaded and spawned
	if(!fetchData(%aiId, "HasLoadedAndSpawned"))
	{
		echo("WARNING: AI::AddBotToBotGroup - Bot clientId " @ %aiId @ " has not loaded and spawned yet. Skipping to prevent data corruption.");
		return;
	}

	$tmpBotGroup[%group] = $tmpBotGroup[%group] @ %aiId @ " ";
	storeData(%aiId, "botAttackMode", 4);
}

//------ remastered directives ------------------------------

// Helper function to get client ID from AI name or bot name
// Works for Player objects (enemy bots) via multiple lookup methods
// Uses SILENT methods first to avoid console error spam from AI::getId()
function AI::getClientIdFromName(%aiName)
{
	// FAST PATH: O(1) registry maps before the full BaseRep scan below.
	// $BotNameToClient is maintained by RegisterBot/UnregisterBot (enemy bots),
	// $TownBotSpawned by the town bot spawn flow. The mapping is verified
	// against the bot data arrays before trusting it (clientId reuse safety).
	%fastId = $BotNameToClient[%aiName];
	if(%fastId == "" || %fastId == -1)
		%fastId = $TownBotSpawned[%aiName];
	if(%fastId != "" && %fastId != -1)
	{
		%fastObj = Client::getOwnedObject(%fastId);
		if(%fastObj != -1 && %fastObj != "" && isObject(%fastObj))
		{
			%fastName = $EnemyBotData[%fastId, "BotInfoAiName"];
			if(%fastName == "") %fastName = $TownBotData[%fastId, "BotInfoAiName"];
			if(%fastName == "") %fastName = $BotInfoAiName[%fastId];
			if(%fastName == "") %fastName = $ClientData[%fastId, "BotInfoAiName"];
			if(%fastName == %aiName)
				return %fastId;
		}
	}

	// SILENT METHOD 1: Search by BotInfoAiName using BaseRep iteration
	// This is checked FIRST because AI::getId() prints error messages when AI not found
	for(%id = BaseRep::getFirst(); %id != -1; %id = BaseRep::getNext(%id))
	{
		// Check BotInfoAiName in all arrays
		%botInfoAiName = $EnemyBotData[%id, "BotInfoAiName"];
		if(%botInfoAiName == "") %botInfoAiName = $TownBotData[%id, "BotInfoAiName"];
		if(%botInfoAiName == "") %botInfoAiName = $BotInfoAiName[%id];
		if(%botInfoAiName == "") %botInfoAiName = $ClientData[%id, "BotInfoAiName"];
		
		if(%botInfoAiName == %aiName)
		{
			// Validate player object exists
			%playerObj = Client::getOwnedObject(%id);
			if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
			{
				return %id;
			}
		}
		
		// Also check display name
		%displayName = Client::getName(%id);
		if(%displayName != "" && %displayName != -1 && String::ICompare(%displayName, %aiName) == 0)
		{
			if(Player::isAiControlled(%id))
			{
				return %id;
			}
		}
	}
	
	// Bot doesn't exist - return -1
	return -1;
}

function AI::newDirectiveFollow(%aiName, %idNum, %rad, %directive)
{
	dbecho($dbechoMode, "AI::newDirectiveFollow(" @ %aiName @ ", " @ %idNum @ ", " @ %rad @ ", " @ %directive @ ")");

	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "")
		return; // Bot doesn't exist anymore, skip
	
	// Validate target player object still exists
	%targetPlayer = Client::getOwnedObject(%idNum);
	if(%targetPlayer == -1 || %targetPlayer == "")
		return; // Target doesn't exist anymore, skip
	
	AI::newDirectiveRemove(%aiName, %directive);

	$aidirectiveTable[%aiId, %directive] = "follow";
	
	// Use AI::directiveFollow for all bots - the circle running issue was fixed
	AI::directiveFollow(%aiName, %idNum, %rad, %directive);
}

function AI::newDirectiveWaypoint(%aiName, %pos, %directive)
{
	dbecho($dbechoMode, "AI::newDirectiveWaypoint(" @ %aiName @ ", " @ %pos @ ", " @ %directive @ ")");

	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "")
		return;
		
	AI::newDirectiveRemove(%aiName, %directive);

	$aidirectiveTable[%aiId, %directive] = "waypoint";
	
	// Use AI::directiveWaypoint for all bots
	AI::directiveWaypoint(%aiName, %pos, %directive);
}

function AI::newDirectiveRemove(%aiName, %directive)
{
	dbecho($dbechoMode, "AI::newDirectiveRemove(" @ %aiName @ ", " @ %directive @ ")");

	%aiId = AI::getClientIdFromName(%aiName);
	
	// Validate bot still exists before trying to remove directive
	if(%aiId == -1 || %aiId == "")
	{
		// Bot doesn't exist or couldn't be found, silently return
		return;
	}
	
	// CRITICAL: Validate player object still exists (bot might be in process of being deleted)
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Bot is being deleted or doesn't exist, silently return to avoid crashes
		return;
	}
	
	// CRITICAL: Track attempts to remove directive 99 to prevent repeated errors
	// If we've already tried to remove this directive for this bot and it failed, don't try again
	if(%directive == 99)
	{
		%removalAttempted = $Directive99RemovalAttempted[%aiName];
		if(%removalAttempted == "true" || %removalAttempted == "1")
		{
			// Already attempted removal for this bot - silently return to prevent repeated errors
			return;
		}
		// Mark that we're attempting removal
		$Directive99RemovalAttempted[%aiName] = "true";
	}
	
	// CRITICAL: Check if this is an enemy bot (Player object, not Drone)
	// AI::directiveRemove() only works for Drones, not Player objects
	// For enemy bots, we just clear our tracking table and skip the engine call
	%spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
	%isEnemyBot = (%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1);
	
	// CRITICAL: Check if directive exists in our tracking table
	// If it's in our table, it was set by us and we should remove it
	%directiveValue = $aidirectiveTable[%aiId, %directive];
	if(%directiveValue != "" && %directiveValue != "0" && %directiveValue != -1)
	{
		// Directive exists in our tracking table
		// For enemy bots (Player objects), just clear our tracking table - don't call engine
		// For Drones, try to remove it from the engine
		if(!%isEnemyBot)
		{
			// This is a Drone - try to remove it from the engine
			// If it fails, the engine will log an error but we'll continue
			AI::directiveRemove(%aiName, %directive);
		}
		// Clear our tracking table for both enemy bots and Drones
		$aidirectiveTable[%aiId, %directive] = "";
		// Clear removal attempt flag on success (if directive 99)
		if(%directive == 99)
			$Directive99RemovalAttempted[%aiName] = "";
		return;
	}
	
	// CRITICAL: If directive is not in our table, it might have been set by the engine directly
	// (e.g., directive 99 is set automatically when bots follow targets)
	// Only attempt removal if we have a valid AI name and the bot still exists
	// CRITICAL: Skip engine call for enemy bots (Player objects) - AI::directiveRemove() doesn't work for them
	if(!%isEnemyBot && %aiName != "" && %aiName != -1 && %aiName != "0")
	{
		// Re-validate player object before attempting removal (bot might have been deleted)
		%playerObjCheck = Client::getOwnedObject(%aiId);
		if(%playerObjCheck != -1 && %playerObjCheck != "")
		{
			// Try to remove directive even if not in our table (might have been set by engine)
			// This prevents directive 99 from persisting when bots are stopped/followed
			// The engine will log an error if the directive doesn't exist, but we track attempts to prevent spam
			AI::directiveRemove(%aiName, %directive);
			// Note: We don't clear the flag here because if the directive doesn't exist,
			// the engine will log an error, and we want to prevent repeated attempts
		}
		else
		{
			// Bot was deleted - clear removal attempt flag
			if(%directive == 99)
				$Directive99RemovalAttempted[%aiName] = "";
		}
	}
	else if(%isEnemyBot)
	{
		// Enemy bot - just clear the removal attempt flag since we can't use engine functions
		if(%directive == 99)
			$Directive99RemovalAttempted[%aiName] = "";
	}
	else
	{
		// Invalid AI name - clear removal attempt flag
		if(%directive == 99)
			$Directive99RemovalAttempted[%aiName] = "";
	}
	// Silently return regardless of success/failure to avoid cluttering logs
}

//------- Ghost Client ID Cleanup ----------------------------------------

// CheckGhostClientIdDelayed: Delayed check for ghost client ID (allows time for name to be set)
function CheckGhostClientIdDelayed(%clientId, %aiName)
{
	if(%clientId == -1 || %clientId == "" || %clientId == "0")
		return;
	
	// Check if player object still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
		return;  // Already deleted
	
	// Check if name has been set
	%name = Client::getName(%clientId);
	if(%name != "" && %name != -1)
	{
		// Name was set - check if it's a real player before returning
		%characterFile = "temp\\" @ %name @ ".cs";
		if(isFile(%characterFile))
		{
			// Real player with save file - not a ghost, abort cleanup
			return;
		}
		return;  // Name was set, not a ghost
	}
	
	// CRITICAL: Check if this is a real player loading (connected but name not set yet)
	// Real players can have empty names during initial connection
	%isConnected = false;
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(%cl == %clientId)
		{
			%isConnected = true;
			break;
		}
	}
	
	// If connected but name is empty, this might be a real player loading - check for save file
	if(%isConnected)
	{
		// Check if it's a real player by checking if it's NOT AI-controlled
		if(!Player::isAiControlled(%clientId))
		{
			// Connected and not AI-controlled - this is a real player loading, skip cleanup
			return;
		}
	}
	
	// Name is still empty - check if this AI name is in use by another bot
	%otherBotId = AI::getClientIdFromName(%aiName);
	if(%otherBotId != -1 && %otherBotId != "" && %otherBotId != %clientId)
	{
		%otherBotName = Client::getName(%otherBotId);
		if(%otherBotName != "" && %otherBotName != -1)
		{
			// Another bot with this AI name exists - this is definitely a ghost
			// CRITICAL: Final check for save file before cleanup
			%finalNameCheck = Client::getName(%clientId);
			if(%finalNameCheck != "" && %finalNameCheck != -1)
			{
				%finalCharacterFile = "temp\\" @ %finalNameCheck @ ".cs";
				if(isFile(%finalCharacterFile))
				{
					// Real player with save file - abort cleanup
					return;
				}
			}
			echo("WARNING: CheckGhostClientIdDelayed - Confirmed ghost client ID " @ %clientId @ " (empty name) for AI " @ %aiName @ ". Another bot exists (clientId=" @ %otherBotId @ "). Cleaning up...");
			CleanupGhostClientId(%clientId, %aiName);
		}
	}
	else
	{
		// No other bot with this AI name - might still be initializing, check once more
		schedule("CheckGhostClientIdDelayed(" @ %clientId @ ", \"" @ %aiName @ "\");", 1.0);
	}
}

// CleanupGhostClientId: Removes all data associated with a ghost client ID (empty name Player object)
function CleanupGhostClientId(%clientId, %aiName)
{
	if(%clientId == -1 || %clientId == "" || %clientId == "0")
		return;
	
	// CRITICAL SAFEGUARD: Check for character save file before cleanup
	%ghostName = Client::getName(%clientId);
	if(%ghostName != "" && %ghostName != -1)
	{
		%characterFile = "temp\\" @ %ghostName @ ".cs";
		if(isFile(%characterFile))
		{
			echo("WARNING: CleanupGhostClientId - Client ID " @ %clientId @ " has character save file (" @ %ghostName @ ") - this is a REAL PLAYER! Aborting cleanup to prevent data loss.");
			return; // Abort immediately
		}
	}
	
	// Additional safeguard: Check if it's a connected real player
	%isConnected = false;
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(%cl == %clientId)
		{
			%isConnected = true;
			break;
		}
	}
	
	// If connected but no save file yet, check if it's a real player by checking if it's NOT AI-controlled
	if(%isConnected && !Player::isAiControlled(%clientId))
	{
		echo("WARNING: CleanupGhostClientId - Connected client " @ %clientId @ " is NOT AI-controlled. This is a real player. Aborting cleanup.");
		return; // Abort immediately
	}
	
	// CRITICAL: Clear all storeData fields
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", "");
	storeData(%clientId, "BotInfoAiName", "");
	storeData(%clientId, "RemortStep", "");
	storeData(%clientId, "QuestItems", "");
	storeData(%clientId, "KeyItems", "");
	storeData(%clientId, "Consumables", "");
	storeData(%clientId, "Armor", "");
	storeData(%clientId, "Accessories", "");
	storeData(%clientId, "Other", "");
	storeData(%clientId, "noExperienceFlag", "");
	storeData(%clientId, "noDropLootbagFlag", "");
	storeData(%clientId, "dumbAIflag", "");
	storeData(%clientId, "frozen", "");
	storeData(%clientId, "noBotSniff", "");
	storeData(%clientId, "SpellCastStep", "");
	storeData(%clientId, "LCKconsequence", "");
	storeData(%clientId, "AIattackMarker", "");
	storeData(%clientId, "botTeam", "");
	storeData(%clientId, "AITarget", "");
	storeData(%clientId, "AILastDestination", "");
	storeData(%clientId, "AILastLoggedDist", "");
	storeData(%clientId, "AIMovementLoopRunning", "");
	storeData(%clientId, "zone", "");
	storeData(%clientId, "tmpzone", "");
	storeData(%clientId, "petowner", "");
	storeData(%clientId, "ExpDistributed", "");
	
	// CRITICAL: Clear all array data
	$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
	$EnemyBotData[%clientId, "SpawnTime"] = "";
	$EnemyBotData[%clientId, "BotInfoAiName"] = "";
	$TownBotData[%clientId, "SpawnBotInfo"] = "";
	$TownBotData[%clientId, "SpawnTime"] = "";
	$TownBotData[%clientId, "BotInfoAiName"] = "";
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "SpawnTime"] = "";
	$ClientData[%clientId, "BotInfoAiName"] = "";
	$BotInfoAiName[%clientId] = "";
	
	// CRITICAL: Clear all directive table entries
	for(%d = 0; %d <= 99; %d++)
	{
		$aidirectiveTable[%clientId, %d] = "";
	}
	
	// CRITICAL: Clear belt cached lists
	$Belt::CachedList[%clientId, "QuestItems"] = "";
	$Belt::CachedList[%clientId, "KeyItems"] = "";
	$Belt::CachedList[%clientId, "Consumables"] = "";
	$Belt::CachedList[%clientId, "Armor"] = "";
	$Belt::CachedList[%clientId, "Accessories"] = "";
	$Belt::CachedList[%clientId, "Other"] = "";
	
	// CRITICAL: Remove from pet lists
	$PetList = RemoveFromCommaList($PetList, %clientId);
	%petowner = fetchData(%clientId, "petowner");
	if(%petowner != "" && %petowner != -1 && %petowner != "0")
	{
		storeData(%petowner, "PersonalPetList", RemoveFromCommaList(fetchData(%petowner, "PersonalPetList"), %clientId));
	}
	
	// CRITICAL: Remove from bot groups
	%b = AI::IsInWhichBotGroup(%clientId);
	if(%b != -1)
		AI::RemoveBotFromBotGroup(%clientId, %b);
	
	// CRITICAL: Remove from TownBotList
	$TownBotList = RemoveFromCommaList($TownBotList, %clientId);
	
	// CRITICAL: Clear aiNumTable entry if exists
	if($tmpbotn[%aiName] != "" && $tmpbotn[%aiName] != -1)
	{
		$aiNumTable[$tmpbotn[%aiName]] = "";
		$tmpbotn[%aiName] = "";
	}
	
	// CRITICAL: Clear directive 99 removal attempt flag
	if(%aiName != "" && %aiName != -1 && %aiName != "0")
		$Directive99RemovalAttempted[%aiName] = "";
	
	// CRITICAL: Try to delete the Player object if it exists
	// CRITICAL: Add 0.5 second delay to deletion to help code load properly and functions/variables be called more effectively
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != -1 && %playerObj != "")
	{
		schedule("if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ ");", 1.0);
	}
	
	// CRITICAL: Try to delete AI name from engine registry
	if(%aiName != "" && %aiName != -1 && %aiName != "0")
	{
		// CRITICAL: Decrement $numAI if this was an Enemy Bot (had SpawnBotInfo)
		// We explicitly check SpawnBotInfo to distinguish from Town Bots (which don't track $numAI)
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0")
		{
			if($numAI > 0)
			{
				$numAI--;
				$Telemetry_NumAI_Dec++;
				echo("[SPAWN COUNTER] CleanupGhostClientId: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %aiName);
			}
		}
		
		%escapedAiName = String::replace(%aiName, "\"", "\\\"");
		// CRITICAL: Add 1 second delay to deletion to help code load properly and functions/variables be called more effectively
		schedule("AI::delete(\"" @ %escapedAiName @ "\");", 1.0);
	}
	
	echo("CleanupGhostClientId: Cleaned up ghost client ID " @ %clientId @ " (AI: " @ %aiName @ ")");
}

// PeriodicShellBotCheck: Scans for and detects shell bots (bots with no player object)
function PeriodicShellBotCheck()
{
	// Detect-only diagnostic: it scans both bot registries but takes no action,
	// so skip the scans entirely unless debugging. Loop stays alive so the
	// flags can be flipped at runtime without restarting the scheduler.
	if(!$AI_DEBUG_ENABLED && !$AI_SPAWN_DEBUG)
	{
		schedule("PeriodicShellBotCheck();", 30);
		return;
	}

	Watchdog_Enter("PeriodicShellBotCheck");
	%shellCount = 0;
	// CRITICAL FIX: Client::getFirst()/getNext() only returns REAL player clients, NOT AI bots!
	// Use $BotRegistryList to iterate enemy bots and $TownBotSpawned for town bots
	
	// Check enemy bots from registry
	if($BotRegistryList != "")
	{
		for(%i = 0; (%checkId = GetWord($BotRegistryList, %i)) != -1; %i++)
		{
			if(Watchdog_LoopCheck("BotRegistryList")) break;
			if(%checkId != "" && %checkId != "0")
			{
				%name = Client::getName(%checkId);
				%playerObj = Client::getOwnedObject(%checkId);
				%botInfoAiName = fetchData(%checkId, "BotInfoAiName");
				%spawnBotInfo = fetchData(%checkId, "SpawnBotInfo");
				
				// Check if it's a shell bot (has bot data but no player object)
				if((%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0") || 
				   (%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0"))
				{
					if(%playerObj == -1 || %playerObj == "" || !isObject(%playerObj))
					{
						%shellCount++;
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] PeriodicShellBotCheck: SHELL BOT DETECTED - clientId=" @ %checkId @ ", Display name: '" @ %name @ "', BotInfoAiName='" @ %botInfoAiName @ "', SpawnBotInfo='" @ %spawnBotInfo @ "'. Bot has no player object!");
					}
				}
			}
		}
	}
	
	// Check town bots from registry
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
	{
		if(Watchdog_LoopCheck("TownBotRegistry")) break;
		if(%botName != "" && %botName != "0")
		{
			%checkId = $TownBotSpawned[%botName];
			if(%checkId != "" && %checkId != -1 && %checkId != "0")
			{
				%name = Client::getName(%checkId);
				%playerObj = Client::getOwnedObject(%checkId);
				%botInfoAiName = fetchData(%checkId, "BotInfoAiName");
				
				// Check if it's a shell bot (has bot data but no player object)
				if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
				{
					if(%playerObj == -1 || %playerObj == "" || !isObject(%playerObj))
					{
						%shellCount++;
						if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] PeriodicShellBotCheck: SHELL TOWN BOT DETECTED - clientId=" @ %checkId @ ", Name: '" @ %botName @ "', BotInfoAiName='" @ %botInfoAiName @ "'. Bot has no player object!");
					}
				}
			}
		}
	}
	
	if(%shellCount > 0)
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[BOT SHELL DEBUG] PeriodicShellBotCheck: Found " @ %shellCount @ " shell bot(s) (bots with no player object)");
	}
	
	// Schedule next check in 30 seconds
	Watchdog_Exit();
	schedule("PeriodicShellBotCheck();", 30);
}

// review #26/#63: was a top-level EXEC-TIME schedule, which (a) is FLUSHED by the first
// Server::loadMission (so the loop never actually started on a live server), and (b)
// DUPLICATED a permanent scan chain on every live re-exec of ai.cs. Wrapped in a guarded
// Start* called from Server.cs AFTER loadMission, matching StartWatchdog / StartGhostBotCleanup.
function StartShellBotCheck()
{
	if($ShellBotCheckStarted)
		return;
	$ShellBotCheckStarted = true;
	schedule("PeriodicShellBotCheck();", 60);
}

// PeriodicGhostClientIdCleanup: Scans for and cleans up ghost client IDs periodically
function PeriodicGhostClientIdCleanup()
{
	Watchdog_Enter("PeriodicGhostClientIdCleanup");
	// Scan a range of client IDs for ghost objects (empty name Player objects that are bots)
	// review #62: bound the scan to the ONLY possible BaseRep client-id range (2049-2175).
	// The old 2000-2300 span wasted 173 of 301 iterations per 30s pass on ids that can
	// never hold a client - the same stale hardcoded bound already fixed elsewhere in this file.
	%startId = $BaseRepClientIdMin + 1;
	%endId = $BaseRepClientIdMax;
	
	%ghostCount = 0;
	for(%checkId = %startId; %checkId <= %endId; %checkId++)
	{
		if(Watchdog_LoopCheck("ClientIdRange")) break;
		%playerObj = Client::getOwnedObject(%checkId);
		if(%playerObj != -1 && %playerObj != "")
		{
			// Player object exists - check if it's a ghost (empty name)
			%name = Client::getName(%checkId);
			
			// CRITICAL: Check if this is a real player loading (connected but name not set yet)
			// Real players can have empty names during initial connection
			%isConnected = false;
			for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			{
				if(%cl == %checkId)
				{
					%isConnected = true;
					break;
				}
			}
			
			// If connected but name is empty, this might be a real player loading - skip it
			if(%isConnected && (%name == "" || %name == -1))
			{
				// Check if it's a real player by checking if it's NOT AI-controlled
				if(!Player::isAiControlled(%checkId))
				{
					// Connected and not AI-controlled - this is a real player loading, skip
					continue;
				}
				
				// Check if save file exists (player might have name set but getName() not ready)
				// If no save file and no bot data, skip (could be loading player)
				%botInfoAiName = fetchData(%checkId, "BotInfoAiName");
				%spawnBotInfo = fetchData(%checkId, "SpawnBotInfo");
				if((%botInfoAiName == "" || %botInfoAiName == -1) && (%spawnBotInfo == "" || %spawnBotInfo == -1))
				{
					// No bot data and connected - likely a real player loading, skip
					continue;
				}
			}
			
			// CRITICAL: Check for save file before cleanup
			if(%name != "" && %name != -1)
			{
				%characterFile = "temp\\" @ %name @ ".cs";
				if(isFile(%characterFile))
				{
					// Real player with save file - skip
					continue;
				}
			}
			
			if(%name == "" || %name == -1)
			{
				// This is a ghost client ID - check if it's a bot (not a real player)
				// Found a ghost!
				// Check if it has bot data
				%aiName = fetchData(%checkId, "BotInfoAiName");
				if(%aiName != "" && %aiName != -1 && %aiName != "0")
				{
					// CRITICAL: Decrement $numAI if this was an Enemy Bot (had SpawnBotInfo)
					%spawnBotInfo = fetchData(%checkId, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0")
					{
						if($numAI > 0)
						{
							$numAI--;
							$Telemetry_NumAI_Dec++;
							echo("[SPAWN COUNTER] PeriodicGhostClientIdCleanup: Decremented $numAI (now " @ $numAI @ ") for ghost enemy bot " @ %aiName);
						}
					}
					
					echo("PeriodicGhostClientIdCleanup: Found ghost client ID " @ %checkId @ " (AI: " @ %aiName @ ") - cleaning up");
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] PeriodicGhostClientIdCleanup: Found ghost client ID " @ %checkId @ " (AI: " @ %aiName @ ") - cleaning up");
					CleanupGhostClientId(%checkId, %aiName);
					%ghostCount++;
				}
			}
	}
	}
	
	if(%ghostCount > 0)
		echo("PeriodicGhostClientIdCleanup: Cleaned up " @ %ghostCount @ " ghost client ID(s)");
	
	// Schedule next cleanup in 30 seconds
	Watchdog_Exit();
	schedule("PeriodicGhostClientIdCleanup();", 30);
}

// review #26/#63: was a top-level EXEC-TIME schedule (flushed by the first
// Server::loadMission so it never ran on a live server; duplicated on every live
// re-exec of ai.cs). Wrapped in a guarded Start* called from Server.cs after loadMission.
function StartGhostClientIdCleanup()
{
	if($GhostClientIdCleanupStarted)
		return;
	$GhostClientIdCleanupStarted = true;
	schedule("PeriodicGhostClientIdCleanup();", 60);  // first run after 60s to let the server fully initialize
}

//------- TownBot stuff ----------------------------------------

// Global variables for dynamic bot loading
$TownBotRegistry = "";  // List of registered bot names (not spawned yet)
$TownBotZone[0] = "";  // Maps bot name to zone index (0 = no zone/unloaded area)
$TownBotSpawned[0] = "";  // Maps bot name to clientId if spawned, "" if not spawned
$ZonePlayerCount[0] = 0;  // Tracks number of players in each zone
$ZoneBotDespawnSchedule[0] = "";  // Tracks scheduled despawn for each zone


function GatherBotInfo(%group)
{
	dbecho($dbechoMode, "GatherBotInfo(" @ %group @ ")");

	%biggestn = 0;
	%aiName = Object::getName(%group);

	%count = Group::objectCount(%group);
	for(%i = 0; %i <= %count-1; %i++)
	{
		%object = Group::getObject(%group, %i);
		if(getObjectType(%object) == "SimGroup")
		{
			%system = Object::getName(%object);
			%type = GetWord(%system, 0);
			%info = String::getSubStr(%system, String::len(%type)+1, 9999);

			// Extract type name by removing trailing digits using global function
			%type2 = StripTrailingDigits(%type);
			if(%type2 != %type)
			{
				// Extract the number that was removed
				%len = String::len(%type);
				%type2Len = String::len(%type2);
				%numStr = String::getSubStr(%type, %type2Len, %len - %type2Len);
				%n = floor(%numStr);
			}
			else
			{
				%n = 0;  // No trailing digits found
			}

			if(%type == "NAME")
				$BotInfo[%aiName, NAME] = %info;
			else if(%type == "LVL" || %type == "LEVEL")
				$BotInfo[%aiName, LVL] = %info;
			else if(%type == "RACE")
				$BotInfo[%aiName, RACE] = %info;
			else if(%type == "NEED")
				$BotInfo[%aiName, NEED] = %info;
			else if(%type == "TAKE")
				$BotInfo[%aiName, TAKE] = %info;
			else if(%type == "GIVE")
				$BotInfo[%aiName, GIVE] = %info;
			else if(%type == "SHOP")
				$BotInfo[%aiName, SHOP] = %info;
			else if(%type == "ITEMS")
				$BotInfo[%aiName, ITEMS] = %info;
			else if(%type == "CSAY")
				$BotInfo[%aiName, CSAY] = %info;
			else if(%type == "LSAY")
				$BotInfo[%aiName, LSAY] = %info;
			else if(%type == "BOT")
				$BotInfo[%aiName, BOT] = %info;
			else if(%type == "POS")
				$BotInfo[%aiName, POS] = %info;
			else if(%type == "LCK")
				$BotInfo[%aiName, LCK] = %info;
			else if(%type == "SIMGROUP")
				$BotInfo[%aiName, SIMGROUP] = %info;

			// Store indexed types (SAY, CUE, NSAY, NCUE) using extracted type and number
			if(%type2 == "SAY")
				$BotInfo[%aiName, SAY, %n] = %info;
			else if(%type2 == "CUE")
				$BotInfo[%aiName, CUE, %n] = %info;
			else if(%type2 == "NSAY")
				$BotInfo[%aiName, NSAY, %n] = %info;
			else if(%type2 == "NCUE")
				$BotInfo[%aiName, NCUE, %n] = %info;

			if(%n > %biggestn)
				%biggestn = %n;
		}
		else
			%marker = %object;
	}
	$BotInfo[%aiName, SAY, %biggestn+1] = "";
	$BotInfo[%aiName, NSAY, %biggestn+1] = "";
	$BotInfo[%aiName, CUE, %biggestn+1] = "";
	$BotInfo[%aiName, NCUE, %biggestn+1] = "";

	//==============================================
	//The following is generally BotMaker-only code
	//==============================================
	if($BotInfo[%aiName, SIMGROUP] != "")
	{
		%g = nameToId("MissionGroup\\" @ $BotInfo[%aiName, SIMGROUP]);

		%count = Group::objectCount(%g);
		for(%i = 0; %i <= %count-1; %i++)
		{
			%object = Group::getObject(%g, %i);
			if(getObjectType(%object) == "SimGroup")
			{
				%system = Object::getName(%object);
				%type = GetWord(%system, 0);
				%info = String::getSubStr(%system, String::len(%type)+1, 9999);

				if(%type == "NAMES")
					$BotInfo[%aiName, NAMES] = %info;
				else if(%type == "DEFAULTS")
				{
					%class = GetWord(%info, 0);
					%stuff = String::getSubStr(%info, String::len(%class)+1, 9999);

					$BotInfo[%aiName, DEFAULTS, %class] = %stuff;
				}
			}
			else if(getObjectType(%object) == "Marker")
			{
				$BotInfo[%aiName, DESTSPAWN] = %object;
			}
		}
	}
	//==============================================

	return %marker;
}

function isRPGAI(%clientId)
{
	dbecho($dbechoMode, "isRPGAI(" @ %clientId @ ")");

	// CRITICAL SAFEGUARD: Check if player has a character save file FIRST
	// If a character save file exists, this is DEFINITELY a real player, NOT a bot
	// This prevents false positives from stale bot data in arrays
	%playerNameCheck = Client::getName(%clientId);
	if(%playerNameCheck != "" && %playerNameCheck != -1)
	{
		%characterFile = "temp\\" @ %playerNameCheck @ ".cs";
		if(isFile(%characterFile))
		{
			// This is a real player with a save file - NOT a bot
			return False;
		}
	}

	// Check multiple data sources since town bots and enemy bots store data differently
	// CRITICAL FIX: Direct array access to avoid infinite recursion with fetchData->GetDataFromArray->isRPGAI
	// Check multiple data sources since town bots and enemy bots store data differently
	%botInfoAiName = $EnemyBotData[%clientId, "BotInfoAiName"];
	if(%botInfoAiName == "")
		%botInfoAiName = $TownBotData[%clientId, "BotInfoAiName"];
	if(%botInfoAiName == "")
		%botInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
		
	%spawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	if(%spawnBotInfo == "")
		%spawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
	
	// Also check $TownBotData (where town bots store their data)
	if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
		%botInfoAiName = $TownBotData[%clientId, "BotInfoAiName"];
	
	// Also check $EnemyBotData (where enemy bots store their data)
	if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
		%botInfoAiName = $EnemyBotData[%clientId, "BotInfoAiName"];
	
	// Also check $ClientData (legacy storage)
	if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
		%botInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
	
	// Also check $BotInfoAiName direct array
	if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
		%botInfoAiName = $BotInfoAiName[%clientId];
	
	// Check SpawnBotInfo in multiple places too
	if(%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0")
		%spawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	if(%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0")
		%spawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
	
	// Also check if this clientId is in $TownBotSpawned (for town bots)
	%isTownBotByRegistry = false;
	// Also check if this clientId is in $TownBotSpawned (for town bots)
	%isTownBotByRegistry = false;
	for(%i = 0; %i < $TownBotRegistryCount; %i++)
	{
		%botName = $TownBotRegistry[%i];
		if(%botName != "" && $TownBotSpawned[%botName] == %clientId)
		{
			%isTownBotByRegistry = true;
			break;
		}
	}
	
	// Also check bot registry (for enemy bots)
	%isEnemyBotByRegistry = IsBotRegistered(%clientId);
	
	// Check if BotInfoAiName is valid (not empty, -1, or "0")
	%hasValidBotName = (%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0");
	if(%hasValidBotName || %spawnBotInfo != "" || %isTownBotByRegistry || %isEnemyBotByRegistry)
		return True;
	else
		return False;
}

// (VerifyEnemyBotTeam removed - its only remaining caller was its own retry
// reschedule; the real call sites were replaced by ScheduleTeamEnforcement /
// EnforceEnemyBotTeam, as the "OPTIMIZED: Removed redundant VerifyEnemyBotTeam
// schedule" comments at those sites record)

// ---------------------------------------------------------------------------------------------------------
// REINFORCED DISCONNECT HANDLER
// ---------------------------------------------------------------------------------------------------------
// This function is called by the engine when ANY client drops (disconnects or is deleted).
// It acts as the final safety net for:
// 1. Ghost Bot Prevention: Decrementing spawn counters if they weren't already.
// 2. Race Condition Prevention: Marking the ID as recently freed so it isn't reused immediately.
function onClientDrop(%clientId)
{
	// Skip all logic during server shutdown to prevent freeze
	if($ServerShuttingDown)
		return;
	
	echo("[CLIENT DROP] onClientDrop(" @ %clientId @ ") called @ " @ getSimTime());
	
	// 1. Mark ID as recently freed (Critical for Race Condition Fix)
	// This ensures we don't reuse this ID for 3 seconds, even if it was a player
	$ClientIdRecentlyFreed[%clientId] = getSimTime();
	
	// 2. Check if it was an Enemy Bot
	// We check multiple flags because some might be cleared during cleanup
	%botName = fetchData(%clientId, "BotInfoAiName");
	%spawnInfo = fetchData(%clientId, "SpawnBotInfo");
	%registryInfo = $BotRegistry[%clientId];
	
	%isEnemyBot = false;
	if(%botName != "" && %botName != -1 && %botName != "0")
		%isEnemyBot = true;
	else if(%spawnInfo != "" && %spawnInfo != -1)
		%isEnemyBot = true;
	else if(%registryInfo != "" && %registryInfo != -1)
		%isEnemyBot = true;
	
	if(%isEnemyBot)
	{
		echo("[CLIENT DROP] Detected Enemy Bot drop: " @ %clientId @ " (Name=" @ %botName @ ", Spawn=" @ %spawnInfo @ ")");
		
		// 3. Decrement Spawn Counter (Critical for Ghost Bot Fix)
		// Usually Player::onKilled handles this, but if the bot was deleted/kicked/disconnected
		// without dying, we MUST decrement here to free the slot.
		// DecrementSpawnCounter has built-in checks to prevent double-decrementing.
		
		if(%spawnInfo != "" && %spawnInfo != -1)
		{
			%spawnPointId = GetWord(%spawnInfo, 1);
			if(GetWord(%spawnInfo, 0) == "SpawnPoint" && %spawnPointId != "")
			{
				echo("[CLIENT DROP] Attempting fail-safe counter decrement for SpawnPoint " @ %spawnPointId);
				// DecrementSpawnCounter expects clientId as first argument
				DecrementSpawnCounter(%clientId);
			}
		}
		
		// 4. Cleanup Bot Registry
		// BUGFIX: this previously decremented $EnemyBotCount/$TotalBotCount (variables that
		// don't exist anywhere else - the real counters are $ActiveEnemyBots/$TotalActiveBots)
		// and only cleared the base registry key, leaving "team"/"name" subkeys and the
		// $BotRegistryList entry behind. Use UnregisterBot for full cleanup.
		if($BotRegistry[%clientId] != "")
		{
			UnregisterBot(%clientId);
			$ActiveEnemyBots--;
			$TotalActiveBots--;
			if($ActiveEnemyBots < 0) $ActiveEnemyBots = 0;
			if($TotalActiveBots < 0) $TotalActiveBots = 0;
			echo("[CLIENT DROP] Cleaned up registry for client " @ %clientId);
		}
	}
}

// ============================================================================
// DEBUG TOOL: InspectBot
// ============================================================================
function InspectBot(%clientId)
{
echo("==================================================");
echo(" INSPECT BOT: " @ %clientId);
echo("==================================================");

// 1. Basic Client Info
%name = Client::getName(%clientId);
%playerObj = Client::getOwnedObject(%clientId);
%controlObj = Client::getControlObject(%clientId);
%team = Client::getTeam(%clientId);

echo(" [CLIENT]");
echo(" Name:              '" @ %name @ "'");
echo(" Team:              " @ %team);
echo(" Player Object:     " @ %playerObj);
echo(" Control Object:    " @ %controlObj);

// 2. Engine Checks
%isAi = Player::isAiControlled(%clientId);
%playerObjIsAi = "";
if(%playerObj != -1 && %playerObj != "")
%playerObjIsAi = Player::isAiControlled(%playerObj);

echo(" [ENGINE]");
echo(" Player::isAiControlled(" @ %clientId @ "): " @ %isAi);
if(%playerObj != "")
{
echo(" Player::isAiControlled(" @ %playerObj @ "): " @ %playerObjIsAi);
echo(" Object Name:           '" @ Object::getName(%playerObj) @ "'");
echo(" Object DataBlock:      " @ %playerObj.dataBlock);
echo(" Object Position:       " @ GameBase::getPosition(%playerObj));
echo(" Object Rotation:       " @ GameBase::getRotation(%playerObj));
echo(" Object State:          " @ %playerObj.state @ " (Dead: " @ %playerObj.dead @ ")");
}

// Reverse lookup check
if(%playerObj != -1 && %playerObj != "")
{
%clientFromObj = Player::getClient(%playerObj);
echo(" Player::getClient(" @ %playerObj @ "):    " @ %clientFromObj);
if(%clientFromObj != %clientId)
echo(" [WARNING] Client ID mismatch! Owned=" @ %clientId @ ", Reverse=" @ %clientFromObj);
}

// 3. Mod Data Checks
%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
%hasLoaded = fetchData(%clientId, "HasLoadedAndSpawned");
%zone = fetchData(%clientId, "zone");

echo(" [MOD DATA]");
echo(" BotInfoAiName:     '" @ %botInfoAiName @ "'");
echo(" SpawnBotInfo:      '" @ %spawnBotInfo @ "'");
echo(" HasLoaded:         " @ %hasLoaded);
echo(" Zone:              " @ %zone);

// 4. EnemyBotData (Global Array)
%ebName = $EnemyBotData[%clientId, "BotInfoAiName"];
%ebSpawn = $EnemyBotData[%clientId, "SpawnBotInfo"];
%ebZone = $EnemyBotData[%clientId, "zone"];

echo(" [ENEMY BOT DATA]");
echo(" $EnemyBotData Name:  '" @ %ebName @ "'");
echo(" $EnemyBotData Spawn: '" @ %ebSpawn @ "'");
echo(" $EnemyBotData Zone:  " @ %ebZone);

// 5. Registry & Graveyard
%inRegistry = $BotRegistry[%clientId];
%inGraveyard = IsInGraveyard(%clientId);
%recentlyFreed = $ClientIdRecentlyFreed[%clientId];

echo(" [REGISTRY]");
echo(" $BotRegistry:      " @ %inRegistry);
echo(" In Graveyard:      " @ %inGraveyard);
echo(" Recently Freed:    " @ %recentlyFreed);

// 6. Safeguards
%isSafeToModify = IsSafeToModify(%clientId, "InspectBot");
%isRealPlayer = IsRealPlayer(%clientId);

// Check for save file manually to report path
%saveFile = "temp\\" @ %name @ ".cs";
%hasSaveFile = isFile(%saveFile);

echo(" [SAFEGUARDS]");
echo(" IsRealPlayer:      " @ %isRealPlayer);
echo(" IsSafeToModify:    " @ %isSafeToModify);
echo(" Has Save File:     " @ %hasSaveFile @ " (" @ %saveFile @ ")");
	
echo("--------------------------------------------------");
echo(" REAL PLAYER LIST (via IsRealPlayer check):");
%realCount = 0;
%aiCount = 0;
for(%id = Client::getFirst(); %id != -1; %id = Client::getNext(%id))
{
	if(IsRealPlayer(%id))
	{
		%realCount++;
		%rName = Client::getName(%id);
		%rIsAi = Player::isAiControlled(%id);
		%rFile = isFile("temp\\" @ %rName @ ".cs");
		echo(" > " @ %id @ ": '" @ %rName @ "' (isAI=" @ %rIsAi @ ", hasFile=" @ %rFile @ ")");
	}
	else
	{
		%aiCount++;
	}
}
echo(" Total Real: " @ %realCount @ " | Total AI: " @ %aiCount);

echo("==================================================");
}
