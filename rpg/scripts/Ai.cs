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
$AI_DEBUG_ENABLED = 1;        // Controls [INERT DEBUG], [SPAWN FLOW], [AI DEBUG] messages
$AI_SPAWN_DEBUG = 1;          // Controls [SPAWN FLOW] messages specifically
$AI_PERIODIC_DEBUG = 1;       // Controls [INERT DEBUG] AI::Periodic messages
$LOOTBAG_DEBUG = 1;           // Controls [LOOTBAG AGGREGATE], [LOOT DEBUG] messages
$Debug::SafeGuards = 1;       // Controls [SAFEGUARD] player protection logging

// Granular Debug Flags (Turn off to reduce spam)
$TOWNBOT_RACE_DEBUG = 0;      // Controls [TOWNBOT RACE DEBUG] messages
$TOWNBOT_SKIN_DEBUG = 0;      // Controls [FINAL FIX] messages
$MISSION_CLEANUP_DEBUG = 1;   // Controls [DEBUG] addToSetMissionCleanup messages
$GETBOTID_DEBUG = 1;          // Controls [GETBOTIDLIST DEBUG] messages
$SPAWNLOOP_DEBUG = 1;         // Controls [SPAWN DEBUG] messages
$BOT_TEAM_DEBUG = 1;          // Controls [BOT TEAM DEBUG] messages
$TEAM_ENFORCE_DEBUG = 1;      // Controls [TEAM ENFORCE] messages
$BOT_TRACK_DEBUG = 1;         // Controls [BOT TRACK] messages
$BOT_REGISTRY_DEBUG = 1;      // Controls [BOT REGISTRY] messages
$SPAWN_TRANSACTION_DEBUG = 1; // Controls [SPAWN TRANSACTION] messages
$BOT_SHELL_DEBUG = 1;         // Controls [BOT SHELL DEBUG] messages
$SPAWN_COUNTER_DEBUG = 1;     // Controls [SPAWN COUNTER] messages
$BOT_CLEANUP_DEBUG = 1;       // Controls [BOT CLEANUP] messages
$TOWNBOT_ARMOR_DEBUG = 1;     // Controls [TOWNBOT ARMOR DEBUG] messages
$RECONCILE_DEBUG = 1;         // Controls [RECONCILE] messages


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
function Telemetry_RecordAINumberOrphan() { $Telemetry_AINumberOrphans++; }
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
$Watchdog_Enabled = true;           // Master switch for watchdog
$Watchdog_CurrentFunction = "";     // Currently executing function
$Watchdog_LoopCounter = 0;          // Current loop iteration
$Watchdog_MaxIterations = 500;      // Max iterations before breaking (safety limit)

// Heartbeat - writes to file every 5 seconds
// If server freezes, watchdog.log shows what was running
function Watchdog_Heartbeat()
{
	if(!$Watchdog_Enabled) return;
	
	%status = getSimTime() @ " | Func: " @ $Watchdog_CurrentFunction @ " | Loop: " @ $Watchdog_LoopCounter;
	%status = %status @ " | Bots: " @ $ActiveEnemyBots @ "/" @ $numAI;
	
	// Write to both console and file
	echo("[WATCHDOG] " @ %status);
	
	// Write to dedicated file (overwrites each time - last state before freeze)
	export("$Watchdog_*", "config/watchdog_state.cs", false);
	
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

// Start watchdog on server load
schedule("Watchdog_Heartbeat();", 10);

// ============================================================================
// PERIODIC AI NUMBER RECONCILIATION
// Runs every 5 minutes to clean up orphaned AI numbers from $aiNumTable
// ============================================================================
$AINumberReconciliationEnabled = true;  // Set to false to disable

function StartAINumberReconciliation()
{
	if($AINumberReconciliationEnabled)
	{
		schedule("PeriodicAINumberReconciliation();", 300);  // 5 minutes
		echo("[AI RECONCILIATION] Started periodic AI number reconciliation (every 5 minutes)");
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
	for(%n = 0; %n <= 500; %n++)
	{
		if($aiNumTable[%n] != "" && $aiNumTable[%n] != -1)
		{
			%checkedCount++;
			
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
				%freedCount++;
			}
		}
	}
	
	if(%freedCount > 0)
		echo("[AI RECONCILIATION] Freed " @ %freedCount @ " orphaned AI numbers (checked " @ %checkedCount @ ")");
	
	// Schedule next run
	schedule("PeriodicAINumberReconciliation();", 300);  // 5 minutes
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
		if($GhostBotSuspect[%clientId] != "")
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
				%cleanedCount++;
			}
		}
		else
		{
			// NEW SUSPECT - flag it with current time
			$GhostBotSuspect[%clientId] = getSimTime();
			%newSuspects++;
			
			if($AI_DEBUG_ENABLED)
				echo("[GHOST BOT] Flagged new suspect: clientId=" @ %clientId @ ", displayName='" @ %displayName @ "'");
		}
	}
	
	// Clear flags for bots that are no longer suspects (they got proper data or were cleaned up elsewhere)
	// This prevents stale flags from accumulating
	for(%checkId = 2048; %checkId < 2128; %checkId++)
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
			}
		}
	}
	
	if(%cleanedCount > 0 || (%newSuspects > 0 && $AI_DEBUG_ENABLED))
		echo("[GHOST BOT] Scan complete: " @ %ghostCount @ " ghosts found, " @ %newSuspects @ " newly flagged, " @ %cleanedCount @ " cleaned up");
	
	// Schedule next scan
	schedule("PeriodicGhostBotScan();", 30);
}

// ============================================================================
// PRE-INDEXED BOT LOOKUP TABLES
// Provides O(1) access to bot data by clientId and by aiName
// ============================================================================
// $BotIndex_ClientToName[clientId] = aiName
// $BotIndex_NameToClient[aiName] = clientId
// $BotIndex_SpawnPointBots[spawnPointId] = "clientId1 clientId2 ..."

function BotIndex_Add(%clientId, %aiName, %spawnPointId)
{
	if(%clientId == "" || %clientId == -1)
		return;
	
	$BotIndex_ClientToName[%clientId] = %aiName;
	if(%aiName != "" && %aiName != -1)
		$BotIndex_NameToClient[%aiName] = %clientId;
	
	if(%spawnPointId != "" && %spawnPointId != -1)
	{
		%existing = $BotIndex_SpawnPointBots[%spawnPointId];
		if(%existing == "")
			$BotIndex_SpawnPointBots[%spawnPointId] = %clientId;
		else
			$BotIndex_SpawnPointBots[%spawnPointId] = %existing @ " " @ %clientId;
	}
}

function BotIndex_Remove(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return;
	
	%aiName = $BotIndex_ClientToName[%clientId];
	$BotIndex_ClientToName[%clientId] = "";
	if(%aiName != "" && %aiName != -1)
		$BotIndex_NameToClient[%aiName] = "";
	
	%spawnPointId = $BotRegistry[%clientId];
	if(%spawnPointId != "" && %spawnPointId != -1)
	{
		%existing = $BotIndex_SpawnPointBots[%spawnPointId];
		%newList = "";
		for(%i = 0; (%id = GetWord(%existing, %i)) != -1; %i++)
		{
			if(%id != %clientId)
			{
				if(%newList == "")
					%newList = %id;
				else
					%newList = %newList @ " " @ %id;
			}
		}
		$BotIndex_SpawnPointBots[%spawnPointId] = %newList;
	}
}

function BotIndex_GetNameByClient(%clientId) { return $BotIndex_ClientToName[%clientId]; }
function BotIndex_GetClientByName(%aiName) { return $BotIndex_NameToClient[%aiName]; }
function BotIndex_GetBotsBySpawnPoint(%spawnPointId) { return $BotIndex_SpawnPointBots[%spawnPointId]; }

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
	if(%age >= 0)
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD] RemoveFromGraveyard: Removed bot " @ %aiName @ " (clientId=" @ %clientId @ ") after " @ %age @ "s");
	else
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[GRAVEYARD] RemoveFromGraveyard: Removed bot " @ %aiName @ " (clientId=" @ %clientId @ ")");
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
	for(%id = 2049; %id <= 2200; %id++)
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
	
	// Add to O(1) lookup index
	BotIndex_Add(%clientId, %aiName, %spawnPointId);
	
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
	
	// Remove from O(1) lookup index (call before clearing registry entries)
	BotIndex_Remove(%clientId);
	
	%spawnPointId = $BotRegistry[%clientId];
	%aiName = $BotRegistry[%clientId, "name"];
	
	// Also try to get name from storeData if not in registry
	if(%aiName == "" || %aiName == -1)
		%aiName = fetchData(%clientId, "BotInfoAiName");
	
	if($BOT_REGISTRY_DEBUG) echo("[BOT REGISTRY DEBUG] UnregisterBot called: clientId=" @ %clientId @ ", spawnPoint=" @ %spawnPointId @ ", name=" @ %aiName);
	
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
				
				echo("[BOT REGISTRY] Found orphaned object " @ %obj @ " in BotGroup for clientId=" @ %clientId @ " - scheduling deletion");
				// Schedule deletion to prevent crash during death processing
				schedule("if(isObject(" @ %obj @ ")) deleteObject(" @ %obj @ ");", 0.5);
			}
		}
	}
	
	// CRITICAL: Also scan MissionCleanup since orphaned objects end up there
	if(isObject("MissionCleanup"))
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
				
				echo("[BOT REGISTRY] Found orphaned object " @ %obj @ " in MissionCleanup for clientId=" @ %clientId @ " - scheduling deletion");
				// Schedule deletion to prevent crash during death processing
				schedule("if(isObject(" @ %obj @ ")) deleteObject(" @ %obj @ ");", 0.5);
			}
		}
	}
	
	echo("[BOT REGISTRY] Unregistered bot: clientId=" @ %clientId @ ", spawnPoint=" @ %spawnPointId @ ", name=" @ %aiName @ ", wasInList=" @ %foundInList);
}

// =============================================================================
// PRIORITY 1: Unified bot data clearing function
// Consolidates cleanup from: UnregisterBot, ClearVariables, connectivity.cs, #deletebot
// Call this function to clear ALL bot data for a client ID
// =============================================================================
function ClearAllBotData(%clientId, %preserveBotInfoAiName)
{
	if(%clientId == "" || %clientId == -1)
		return;
	
	// NOTE: $BotType is cleared at END of function (after all storeData calls)
	// Otherwise GetClientDataType() warns 100+ times during cleanup
	
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
	
	// Belt items
	storeData(%clientId, "QuestItems", "");
	storeData(%clientId, "KeyItems", "");
	storeData(%clientId, "Consumables", "");
	storeData(%clientId, "Armor", "");
	storeData(%clientId, "Accessories", "");
	storeData(%clientId, "Other", "");
	storeData(%clientId, "RemortStep", "");
	
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
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "SpawnTime"] = "";
	$ClientData[%clientId, "zone"] = "";
	
	// -------------------------------------------------------------------------
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
	
	// -------------------------------------------------------------------------
	// 11. Clear $BotType cache LAST (after all storeData calls complete)
	// -------------------------------------------------------------------------
	$BotType[%clientId] = "";
}

// Get spawn point for a bot from registry
function GetBotSpawnPointFromRegistry(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return "";
	return $BotRegistry[%clientId];
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
				echo("[ORPHAN CLEANUP] Delayed cleanup: Object " @ %originalPlayerObj @ " (data=" @ %dataName @ ") is still orphaned for clientId=" @ %clientId @ " after 10s - deleting");
				deleteObject(%originalPlayerObj);
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
					echo("[ORPHAN CLEANUP] Delayed cleanup: Additional orphaned object " @ %obj @ " (data=" @ %dataName @ ") found for clientId=" @ %clientId @ " - deleting");
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
			
			if(%clientId != -1 && %clientId != "" && %clientId > 2048)  // Only bot client IDs
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
	
	// Priority 4: Client ID in player range (2048 and below)
	// If it has no save file and no bot data, but is in player range -> Assume Real Player (Safety)
	if(%clientId <= 2048)
	{
		if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " is in player range (<= 2048) = REAL PLAYER");
		return true;
	}
	
	// Fallback: If we can't prove it's a bot, assume it's a player for safety
	if($Debug::SafeGuards) echo("[SAFEGUARD] IsRealPlayer: Client " @ %clientId @ " fallback safety check = REAL PLAYER");
	return true;
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
	
	// Layer 2: Client ID range (bot range is 2049+)
	if(%clientId <= 2048)
	{
		echo("[BLOCKED] SAFEGUARD [" @ %operation @ "]: Client " @ %clientId @ " is in player range (<= 2048) - REAL PLAYER PROTECTED");
		return false;
	}
	
	// Layer 3: Filesystem save file check (fallback - catches cases where cache wasn't populated)
	%playerName = Client::getName(%clientId);
	if(%playerName != "" && %playerName != -1)
	{
		// Filesystem check only (cache already checked at top of function)
		if(isFile("temp\\" @ %playerName @ ".cs"))
		{
			// Update cache
			$PlayerHasSaveFile[%clientId] = true;
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
	
	// Layer 2: If clientId provided, verify it's in bot range
	if(%clientId != "" && %clientId != -1)
	{
		if(%clientId <= 2048)
		{
			echo("[CRITICAL] SAFEGUARD [" @ %operation @ "]: ClientId " @ %clientId @ " is in player range - REAL PLAYER PROTECTED");
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
			if(%clientId != "" && %clientId != -1)
				$PlayerHasSaveFile[%clientId] = true;
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
	UnregisterBot(%clientId);
	
	return false;
}

// Increment spawn counter - centralized for consistency
function IncrementSpawnCounter(%spawnPointId)
{
	if(%spawnPointId == "" || %spawnPointId == -1)
		return;
	
	%oldCounter = $numAIperSpawnPoint[%spawnPointId];
	if(%oldCounter == "")
		%oldCounter = 0;
	
	$numAIperSpawnPoint[%spawnPointId]++;
	if($SPAWN_COUNTER_DEBUG) echo("[SPAWN COUNTER] Incremented counter for SpawnPoint " @ %spawnPointId @ " (was: " @ %oldCounter @ ", now: " @ $numAIperSpawnPoint[%spawnPointId] @ ")");
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
			if(%age < 10000) // <10 seconds missing: keep
			{
				if(%newList == "")
					%newList = %clientId;
				else
					%newList = %newList @ " " @ %clientId;
				continue;
			}
			
			// Bot missing beyond grace: clean up registry
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] Removing dead bot from registry: clientId=" @ %clientId @ " (missing " @ %age @ " ms)");
			
			// Proactively delete any lingering player object to prevent shells
			// CRITICAL: Check for save file before deletion to prevent deleting real players
			if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
			{
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
				
				// All checks passed - safe to delete using AI::delete (proper engine cleanup)
				%aiName = $BotInfoAiName[%clientId];
				if(%aiName == "") %aiName = $EnemyBotData[%clientId, "BotInfoAiName"];
				if(%aiName == "") %aiName = $TownBotData[%clientId, "BotInfoAiName"];
				if(%aiName == "") %aiName = fetchData(%clientId, "BotInfoAiName");
				
				if(%aiName != "" && %aiName != -1 && %aiName != "0")
				{
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] Deleting lingering bot via AI::delete: " @ %aiName @ " (clientId=" @ %clientId @ ")");
					AI::delete(%aiName);
				}
				else
				{
					// Fallback: No AI name found, delete player object directly (may create shell)
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[RECONCILE] WARNING: No AI name found for clientId " @ %clientId @ ", using deleteObject fallback");
					deleteObject(%playerObj);
					Client::setOwnedObject(%clientId, -1);
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
	
	// Schedule next reconciliation in 30 seconds
	schedule("ReconcileSpawnCounters();", 30);
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
	
	echo("[PRE-SPAWN CLEANUP] Cleaning up stale data for clientId " @ %clientId);

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
			// AI::getId returns the clientId for a bot name. If it returns a DIFFERENT clientId,
			// the bot is ALIVE elsewhere - do NOT call AI::delete() or it will kill the live bot!
			%actualClientId = AI::getId(%aiName);
			
			if(%actualClientId == %clientId)
			{
				// Bot is on this clientId - safe to delete by name
				echo("[PRE-SPAWN CLEANUP] Deleting bot via AI::delete: " @ %aiName @ " (clientId=" @ %clientId @ ")");
				
				// CRITICAL: Decrement $numAI if this is an Enemy Bot
				if(%isEnemyBot && $numAI > 0)
				{
					$numAI--;
					$Telemetry_NumAI_Dec++;
					echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %aiName);
				}
				
				AI::delete(%aiName);
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
					echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %aiName);
				}
				
				deleteObject(%pobj);
				Client::setOwnedObject(%clientId, -1);
			}
			else
			{
				// Bot name not found in AI engine - delete player object directly
				echo("[PRE-SPAWN CLEANUP] Bot " @ %aiName @ " not found in AI engine, deleting player object for clientId " @ %clientId);
				
				// CRITICAL: Decrement $numAI if this is an Enemy Bot
				if(%isEnemyBot && $numAI > 0)
				{
					$numAI--;
					$Telemetry_NumAI_Dec++;
					echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %aiName);
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
				echo("[SPAWN COUNTER] PreSpawnCleanup: Decremented $numAI (now " @ $numAI @ ") for enemy bot " @ %clientId);
			}
			
			deleteObject(%pobj);
			Client::setOwnedObject(%clientId, -1);
		}
	}
	
	// Clear bot registry entry if exists
	if($BotRegistry[%clientId] != "" || $BotRegistry[%clientId, "team"] != "" || $BotRegistry[%clientId, "name"] != "")
	{
		echo("[PRE-SPAWN CLEANUP] Removing from bot registry");
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
	
	echo("[PRE-SPAWN CLEANUP] Cleanup complete for clientId " @ %clientId);
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
	
	// Step 1: Decrement spawn counter (uses centralized function with fallbacks)
	DecrementSpawnCounter(%clientId);
	
	// Step 2: Decrement tracking counters
	%isEnemy = isEnemyBot(%clientId);
	%isTown = isTownBot(%clientId);
	
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
	
	// Step 4: Unregister from bot registry (CRITICAL: removes from $BotRegistryList)
	// This MUST happen to prevent shell bot spam - without this, dead bots stay in iteration lists
	UnregisterBot(%clientId);
	
	// Step 5: Clear all data storage
	// Use PreSpawnCleanup which already does comprehensive clearing
	PreSpawnCleanup(%clientId);
	
	// Step 5: Mark client ID as recently freed
	// Use validation token to prevent scheduled clear from executing on a real player who got the same clientId
	%validationToken = %aiName @ "_" @ getSimTime();
	if(%validationToken == "" || %validationToken == -1)
		%validationToken = "CleanupBot_" @ %clientId @ "_" @ getSimTime();
	$ClientIdRecentlyFreedToken[%clientId] = %validationToken;
	$ClientIdRecentlyFreed[%clientId] = getSimTime();
	schedule("if($ClientIdRecentlyFreedToken[" @ %clientId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %clientId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %clientId @ "] = \"\"; }", 10.0);
	
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
//createAI()
//---------------------------------
function createAI(%aiName, %markerGroup, %name, %skipPostSpawn, %bypassRaceCheck)
{
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

	//echo("[SPAWN DEBUG] createAI(): armor=" @ %armor);

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
			PreSpawnCleanup(%existingId);
			// Wait a moment for cleanup to complete
			schedule("", 0.1);
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
		// Return false to indicate spawn was deferred
		schedule("createAI(\"" @ %aiName @ "\", \"" @ %armor @ "\", \"" @ %spawnPos @ "\", \"" @ %spawnRot @ "\", \"" @ %name @ "\");", 2.0);
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
		// Try to clean up and retry once
		%existingId2 = AI::getClientIdFromName(%aiName);
		if(%existingId2 != -1 && %existingId2 != "" && %existingId2 != "False" && %existingId2 != "false")
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): AI::spawn() failed - AI name exists. Attempting cleanup and retry...");
			%escapedName = String::replace(%aiName, "\"", "\\\"");
			AI::delete(%escapedName);
			// Retry after cleanup
			schedule("", 0.2);
			%spawnResult = AI::spawn( %aiName, %armor, %spawnPos, %spawnRot, %name, "male2" );
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): Retry AI::spawn() returned: " @ %spawnResult);
		}
	}
	
	if( %spawnResult != "false" )
	{
		// Attempt to immediately tag invulnerability via display name if clientId is already resolvable
		%preClient = NEWgetClientByName(%name);
		
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
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] createAI(): SUCCESS - skipping createAIPostSpawn() (SpawnPoint bot)");
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

//------------------------------
// AI::setWeapons()
//------------------------------
function AI::setWeapons(%aiName, %loadout)
{
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
	}

	// CRITICAL: Ensure team is set BEFORE GiveThisStuff() runs
	// GiveThisStuff() calls RefreshAllEnemyBot() for enemy bots (which does NOT touch team)
	// GameBase::setTeam() might not be synchronous, so set it right before GiveThisStuff()
	// CRITICAL: Use the stored botTeam value (set in SpawnAIGetClientId from $BotInfo[botName, TEAM])
	// Different bot types have different teams - do NOT hardcode to 11!
	%storedBotTeam = fetchData(%aiId, "botTeam");
	if(%storedBotTeam != "" && %storedBotTeam != -1 && %storedBotTeam != "0" && %storedBotTeam != 0)
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
					Client::setSkin(%aiId, $Server::teamSkin[0]);
					
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
				Client::setSkin(%aiId, $Server::teamSkin[0]);
				
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
			Client::setSkin(%aiId, $Server::teamSkin[0]);
			
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

// Get armor movement speed for enemy bots
// Returns the maxForwardSpeed value for the given armor type
function GetArmorMoveSpeed(%armorName)
{
	// Speed values from armordata.cs: $spdlow=6, $spdlowmed=7, $spdmed=8, $spdfast=16
	// Map armor types to their defined speeds
	if(%armorName == "ZombieArmor") return 8;      // $spdmed
	if(%armorName == "OrcArmor") return 8;         // $spdmed
	if(%armorName == "OgreArmor") return 8;        // $spdmed
	if(%armorName == "UndeadArmor") return 6;      // $spdlow
	if(%armorName == "DemonArmor") return 8;       // $spdmed
	if(%armorName == "PigmanArmor") return 8;      // $spdmed
	if(%armorName == "MinotaurArmor") return 7;    // $spdlowmed
	if(%armorName == "AlienArmor") return 16;      // $spdfast
	if(%armorName == "SealArmor") return 16;       // $spdfast
	if(%armorName == "GodArmor") return 24;        // $spdfast * 1.5
	if(%armorName == "AngelArmor") return 8;       // $spdmed
	if(%armorName == "AdminArmor") return 8;       // $spdmed
	return 8; // Default to $spdmed
}

// Continuous attack loop for enemy bots
// This function triggers attacks at the correct weapon speed
function AI::ContinuousAttack(%aiName, %targetId)
{
	dbecho($dbechoMode, "AI::ContinuousAttack(" @ %aiName @ ", " @ %targetId @ ")");
	
	// Get bot info
	%aiId = AI::getClientIdFromName(%aiName);
	if(%aiId == -1 || %aiId == "")
		return; // Bot doesn't exist
	
	// If bot is still spawn-invulnerable / not loaded, do not attack
	if(fetchData(%aiId, "SpawnInvuln") || !fetchData(%aiId, "HasLoadedAndSpawned"))
	{
		storeData(%aiId, "BotAttackLoopActive", "");
		return;
	}
	
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == -1 || %playerObj == "")
		return; // No player object
	
	// Check if target is still valid
	%targetPlayerObj = Client::getOwnedObject(%targetId);
	if(%targetPlayerObj == -1 || %targetPlayerObj == "")
	{
		storeData(%aiId, "AITarget", "");
		storeData(%aiId, "BotAttackLoopActive", "");
		return; // Target gone
	}
	
	// Check if still targeting this enemy
	%currentTarget = fetchData(%aiId, "AITarget");
	if(%currentTarget != %targetId)
	{
		storeData(%aiId, "BotAttackLoopActive", "");
		return; // Changed target
	}
	
	// Get positions
	%aiPos = GameBase::getPosition(%playerObj);
	%targetPos = GameBase::getPosition(%targetPlayerObj);
	if(%aiPos == "" || %targetPos == "")
	{
		storeData(%aiId, "BotAttackLoopActive", "");
		return;
	}
	
	// Drop target if zones differ or distance is excessively large (prevent cross-zone chasing)
	%aiZone = fetchData(%aiId, "zone");
	%targetZone = fetchData(%targetId, "zone");
	if(%aiZone != "" && %aiZone != -1 && %targetZone != "" && %targetZone != -1 && %aiZone != %targetZone)
	{
		storeData(%aiId, "AITarget", "");
		storeData(%aiId, "BotAttackLoopActive", "");
		AI::newDirectiveRemove(%aiName, 99);
		return;
	}
	
	// Calculate distance
	%dist = Vector::getDistance(%aiPos, %targetPos);
	
	// Hard cap: if target is extremely far, drop it to avoid global chasing
	if(%dist > 150)
	{
		storeData(%aiId, "AITarget", "");
		storeData(%aiId, "BotAttackLoopActive", "");
		storeData(%aiId, "LastLoggedDist", "");
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
	if(%lastLoggedDist == "" || %distDiff > 2) // Only log if distance changed by 2+ units
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
		
		// Trigger attack
		Player::trigger(%playerObj, $WeaponSlot, true);
		schedule("Player::trigger(" @ %playerObj @ ", " @ $WeaponSlot @ ", false);", 100);
		
		// Get weapon delay and schedule next attack
		%weaponDelay = GetDelay(%equippedWeapon);
		if(%weaponDelay == "" || %weaponDelay == 0 || %weaponDelay < 0.3)
			%weaponDelay = 0.5; // Minimum 0.5 second delay
		
		// Schedule next attack
		schedule("AI::ContinuousAttack(\"" @ %aiName @ "\", " @ %targetId @ ");", %weaponDelay);
	}
		else
		{
			// Out of range - move directly toward the target's current position (no follow buffer)
			AI::directiveWaypoint(%aiName, %targetPos, 99);
			
			// Keep the attack loop running to check distance again
			schedule("AI::ContinuousAttack(\"" @ %aiName @ "\", " @ %targetId @ ");", 0.5);
		}
}

function AI::Periodic(%aiName)
{
	dbecho($dbechoMode, "AI::Periodic(" @ %aiName @ ")");

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

	// CRITICAL: Check if this is a corpse object - corpses can trigger callbackPeriodic
	// after bot death. Corpse names typically start with "Corpse" followed by a number and bot name
	// Example: "Corpse5 Liquifier6" - we need to reject these immediately to prevent "shellbot" errors
	if(String::findSubStr(%aiName, "Corpse") != -1)
	{
		// This is a corpse, not a bot - ignore it
		// Throttle logging to once per 30 seconds per corpse name to avoid spam
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

			%c = 0; // Initialize counter for idList array
			%list = GetEveryoneIdList();
			// Ensure %list is initialized (should always be from GetEveryoneIdList, but be safe)
			if(%list == "")
				%list = "";
			for(%i = 0; GetWord(%list, %i) != -1; %i++)
			{
				%id = GetWord(%list, %i);
				// CRITICAL: Get Player object for target to check team correctly
				%targetPlayerObj = Client::getOwnedObject(%id);
				%targetTeam = -1;
				if(%targetPlayerObj != -1 && %targetPlayerObj != "")
					%targetTeam = GameBase::getTeam(%targetPlayerObj);
				else
					%targetTeam = GameBase::getTeam(%id); // Fallback to client ID
				
				// Validate id is valid and not on same team
				if(%id != -1 && %id != "" && %targetTeam != %aiTeam && !fetchData(%id, "invisible"))
				{
					%targetPos = GameBase::getPosition(%targetPlayerObj);
					if(%targetPos == "" || %targetPos == -1)
						%targetPos = GameBase::getPosition(%id); // Fallback
					%vec = Vector::sub(%targetPos, %aiPos);
					%vecRot = GetWord(Vector::getRotation(%vec), 2);
		
					if(%vecRot >= %aiRot - $AIFOVPan && %vecRot <= %aiRot + $AIFOVPan)
					{
						%idList[%c++] = %id;
					}
				}
			}
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

function AI::activelyFollow(%aiName, %curTarget, %bypass)
{
	dbecho($dbechoMode, "AI::activelyFollow(" @ %aiName @ ", " @ %curTarget @ ", " @ %bypass @ ")");

	%aiId = ai::getId(%aiName);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "")
		return;
	
	// Validate target still exists
	%targetPlayer = Client::getOwnedObject(%curTarget);
	if(%targetPlayer == -1 || %targetPlayer == "")
		return;

	if(GameBase::getTeam(%aiId) != GameBase::getTeam(%curTarget) || %bypass)
	{
		//echo("Sending " @ %aiName @ " to actively follow and attack " @ %curTarget);
		AI::newDirectiveFollow(%aiName, %curTarget, 0, 99);
	}
}

function AI::moveToAttackMarker(%name, %method)
{
	dbecho($dbechoMode, "AI::moveToAttackMarker(" @ %name @ ", " @ %method @ ")");

	// Use getClientIdFromName() instead of getId() to minimize error spam
	%aiId = AI::getClientIdFromName(%name);
	
	// Validate bot still exists
	if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
		return False;

	if(fetchData(%aiId, "dumbAIflag"))
		return False;

      %tempSet = nameToID("MissionGroup\\Teams\\team1\\AIattackMarkers");

	if(%tempSet != -1)
	{
		%num = Group::objectCount(%tempSet);
		
		// Validate group has objects before processing
		if(%num <= 0)
			return False;

		if(%method == 0)
		{
			//pick a random marker
			%r = floor(getRandom() * %num);
		}
		else if(%method == 1)
		{
			//pick nearest marker
			%dist = 1000000;
			for(%i=0; %i<=%num-1; %i++)
			{
				%m = Group::getObject(%tempSet, %i);
				if(%m != -1 && %m != "")
				{
					%testdist = Vector::getDistance(GameBase::getPosition(%aiId), GameBase::getPosition(%m));
					if(%testdist < %dist)
					{
						%dist = %testdist;
						%r = %i;
					}
				}
			}
		}
	      %marker = Group::getObject(%tempSet, %r);
	      
	      // Validate marker is valid
	      if(%marker == -1 || %marker == "")
	      	return False;
	
		%worldLoc = GameBase::getPosition(%marker);

		AI::newDirectiveWaypoint(%name, %worldLoc, 99);
		storeData(%aiId, "AIattackMarker", %marker);

		//echo(%name @ " IS PROCEEDING TO LOCATION " @ %worldLoc);

		return True;
	}
	return False;
}

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

//------------------------------------------------------------------
// Helper Functions for Bot Identification and Team Determination
//------------------------------------------------------------------

// Determine if a client ID is an enemy bot
function IsEnemyBot(%clientId)
{
	if(%clientId == -1 || %clientId == "")
		return false;
	
	// Check SpawnBotInfo (enemy bots have this, town bots don't)
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		return true;
	
	// Check display name pattern (fallback for newly spawned bots)
	%playerName = Client::getName(%clientId);
	if(%playerName != "" && %playerName != -1)
	{
		// Check for enemy bot name patterns (must be at start of name)
		if(HasEnemyBotNamePrefix(%playerName))
		{
			return true;
		}
	}
	
	return false;
}

// Determine if a client ID is a town bot
function IsTownBot(%clientId)
{
	if(%clientId == -1 || %clientId == "")
		return false;
	
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	
	// Town bots have BotInfoAiName but no SpawnBotInfo
	if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1 && 
	   (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
	{
		return true;
	}
	
	return false;
}

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
		// CRITICAL FIX: ALWAYS record failure to decrement $numAI (balances increment at line 4392)
		// This was previously only called for spawnpoint spawns, causing $numAI leaks for TempSpawn/Town bots
		Telemetry_RecordSpawnFailed("other");
		return -1;
	}

	setAInumber(%newName, %n);

	return %newName;
}
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout, %spawnPointId)
{
	// CRITICAL INTEGRATION: Check server capacity before attempting spawn
	// This prevents the engine from rejecting spawns or crashing when full
	%predictedId = PlayerManager::getFreeId();
	if(%predictedId == -1)
	{
		echo("CRITICAL: SpawnAI - Server is FULL! Aborting spawn for " @ %newName @ " (displayName: " @ %displayName @ ")");
		// Rollback spawn slot if this was a spawn point spawn
		if(%spawnPointId != "" && %spawnPointId != -1)
		{
			RollbackSpawnSlot(%spawnPointId);
		}
		Telemetry_RecordSpawnFailed("serverfull");
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
			// Wait a moment for cleanup to complete
			schedule("", 0.1);
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
	%retval = createAI(%newName, %aiSpawnPos, %displayName, true, %bypassRaceCheck);
	if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
		echo("[SPAWN FLOW] SpawnAI(): createAI() returned: " @ %retval);

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
			
			// Also schedule the standard verification as backup
			schedule("VerifyEnemyBotTeam(" @ %immediateClientId @ ", \"" @ %newName @ "\", " @ %botTeam @ ");", 0.5);
			
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): Set team immediately to " @ %botTeam @ " for " @ %newName @ " (clientId=" @ %immediateClientId @ ") to prevent UpdateTeam() override");
		}
		
		// CRITICAL: AI::spawn() creates a Player object, but it takes time to register in the client list
		// We need to wait a moment before trying to get the client ID
		// Schedule the client ID lookup with a short delay (0.5s; engine creates objects almost instantly)
		// CRITICAL: Verify AI::spawn() actually succeeded before scheduling lookup
		// createAI() returns the AI name on success, -1 on failure
		if(%newName != "" && %newName != -1)
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): Scheduling client ID lookup in 3.0s (waiting for Player object to register and name to replicate)");
			// CRITICAL FIX #2: Pass spawnPointId to SpawnAIGetClientId
			%spawnPointIdForGetId = "";
			if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
				%spawnPointIdForGetId = %spawnPointId;
			// CRITICAL INTEGRATION: Pass predicted ID to avoid expensive lookups
			schedule("SpawnAIGetClientId(\"" @ %newName @ "\", \"" @ %displayName @ "\", \"" @ %aiSpawnPos @ "\", \"" @ %commandIssuer @ "\", \"" @ %loadout @ "\", \"" @ %spawnPointIdForGetId @ "\", \"" @ %predictedId @ "\");", 3.0);
			return %newName; // Return immediately, client ID lookup happens in scheduled call
		}
		else
		{
			// createAI() failed - don't schedule lookup
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG)
				echo("[SPAWN FLOW] SpawnAI(): WARNING - createAI() failed for " @ %newName @ ", not scheduling client ID lookup");
			// CRITICAL FIX #2: Rollback reserved slot if spawn failed
			if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
			{
				RollbackSpawnSlot(%spawnPointId);
			}
			return -1;
		}
	}
	else
	{
		// createAI() failed - rollback reserved slot
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAI(): WARNING - createAI() failed for " @ %newName @ ", rolling back reserved slot");
		// CRITICAL FIX #2: Rollback reserved slot if spawn failed
		if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
		{
			RollbackSpawnSlot(%spawnPointId);
		}
		return -1;
	}
}

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
	%guardtype = %aiName;
	%len = String::len(%aiName);
	%numStr = "";
	%digitString = "0123456789";
	
	// Find trailing digits (working backwards)
	for(%i = %len - 1; %i >= 0; %i--)
	{
		%char = String::getSubStr(%aiName, %i, 1);
		// Check if character is a digit (0-9 only)
		if(String::findSubStr(%digitString, %char) != -1)
		{
			%numStr = %char @ %numStr;
		}
		else
		{
			break;
		}
	}
	
	// If we found trailing digits, remove them
	if(%numStr != "")
	{
		%guardtype = String::getSubStr(%aiName, 0, %len - String::len(%numStr));
	}
	
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
	
	// Tier 2: Range loop fallback (2049-2200) - only if Tier 1 didn't find any recently freed IDs
	if(!%cleanupPerformed)
	{
		for(%checkId = 2049; %checkId <= 2200; %checkId++)
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
							// Use Bot_IsRealPlayer helper if available (or duplicate logic if not defined yet)
							%isRealPlayer = false;
							if(isFunction("Bot_IsRealPlayer")) {
								%isRealPlayer = Bot_IsRealPlayer(%checkId);
							} else {
								// Inline fallback if helper not yet defined (safety)
								%finalStaleNameCheck = Client::getName(%checkId);
								if(%finalStaleNameCheck != "" && %finalStaleNameCheck != -1) {
									if(isFile("temp\\" @ %finalStaleNameCheck @ ".cs")) %isRealPlayer = true;
								}
								if(!%isRealPlayer) {
									%isStaleConnected = false;
									for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl)) {
										if(%cl == %checkId) { %isStaleConnected = true; break; }
									}
									if(%isStaleConnected && !Player::isAiControlled(%checkId)) %isRealPlayer = true;
								}
							}

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
function Bot_IsRealPlayer(%clientId)
{
	if(%clientId == "" || %clientId == -1) return false;
	
	// Check 1: Character save file (most reliable indicator)
	%name = Client::getName(%clientId);
	if(%name != "" && %name != -1)
	{
		%characterFile = "temp\\" @ %name @ ".cs";
		if(isFile(%characterFile))
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
				echo("[SPAWN FLOW] Bot_IsRealPlayer(): Client " @ %clientId @ " (" @ %name @ ") is a REAL PLAYER (save file exists)");
			return true;
		}
	}
	
	// Check 2: Not AI-controlled
	if(!Player::isAiControlled(%clientId))
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) 
			echo("[SPAWN FLOW] Bot_IsRealPlayer(): Client " @ %clientId @ " is a REAL PLAYER (not AI controlled)");
		return true;
	}
	
	return false;
}

// Check if a name matches known enemy bot patterns
// Returns true if the name looks like an enemy bot
function Bot_MatchesEnemyPattern(%name)
{
	if(%name == "" || %name == -1) return false;
	
	// Standard enemy bot prefixes - use consolidated helper
	if(HasEnemyBotNamePrefix(%name))
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] Bot_MatchesEnemyPattern(): Name '" @ %name @ "' matches enemy pattern");
		return true;
	}
	
	// Colloseum/Seal Battle bot patterns (can appear anywhere in name)
	if(String::findSubStr(%name, "Round") != -1 ||
	   String::findSubStr(%name, "BattleOx") == 0 ||
	   String::findSubStr(%name, "Invader") == 0 ||
	   String::findSubStr(%name, "MoonBreaker") == 0)
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] Bot_MatchesEnemyPattern(): Name '" @ %name @ "' matches enemy pattern");
		return true;
	}
	
	return false;
}

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
	%ghostBotId = AI::getId(%newName);  // Most reliable - engine lookup
	if(%ghostBotId == "" || %ghostBotId == -1)
		%ghostBotId = AI::getClientIdFromName(%newName);
	if(%ghostBotId == "" || %ghostBotId == -1)
		%ghostBotId = NEWgetClientByName(%newName);
	
	// Handle Seal Battle bots specially
	if(String::findSubStr(%newName, "RoundOne") == 0 || String::findSubStr(%newName, "RoundTwo") == 0 || String::findSubStr(%newName, "RoundThree") == 0)
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
		echo("[SPAWN AI] Deleting ghost bot via AI::delete: " @ %newName @ " (clientId=" @ %ghostBotId @ ")");
	}
	else
	{
		echo("[SPAWN AI] Couldn't find clientId for " @ %newName @ ", trying AI::delete anyway");
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
	// %aiIdFromGetId = AI::getId(%newName); // REPLACED BY ABOVE BLOCK
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
				// Player object invalid - skip and continue to brute-force
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnAIGetClientId(): SKIPPING client ID " @ %checkId @ " - Player object is missing (likely freed or not ready yet, no bot markers).");
				continue; // Skip this client ID, player object not ready
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
								// AI::getId returns the clientId for a bot name. If it returns a DIFFERENT clientId,
								// the bot is ALIVE elsewhere - do NOT call AI::delete() or it will kill the live bot!
								%actualClientId = AI::getId(%oldAiName);
								
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
					// Telemetry will be recorded when max retries is reached (line ~5971)
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
				// Telemetry will be recorded when max retries is reached (line ~5971)
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
							schedule("if($ClientIdRecentlyFreedToken[" @ %ghostBotId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %ghostBotId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %ghostBotId @ "] = \"\"; }", 10.0);
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
			
			// CRITICAL FIX #2: Rollback reserved slot when max retries reached
			if(%isSpawnPoint && %spawnPointId != "" && %spawnPointId != -1)
			{
				RollbackSpawnSlot(%spawnPointId);
			}
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
					schedule("if($ClientIdRecentlyFreedToken[" @ %aiId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %aiId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %aiId @ "] = \"\"; }", 10.0);
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
					schedule("if($ClientIdRecentlyFreedToken[" @ %aiId @ "] == \"" @ %validationToken @ "\") { $ClientIdRecentlyFreed[" @ %aiId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %aiId @ "] = \"\"; }", 10.0);
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
		// Extract guardtype by removing trailing digits (more reliable than clipTrailingNumbers)
		// This works backwards from the end to find and remove only trailing digits
		%guardtype = %newName;
		%len = String::len(%newName);
		%numStr = "";
		%digitString = "0123456789";
		
		// Find trailing digits (working backwards)
		for(%i = %len - 1; %i >= 0; %i--)
		{
			%char = String::getSubStr(%newName, %i, 1);
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
		
		// If we found trailing digits, remove them
		if(%numStr != "")
		{
			%guardtype = String::getSubStr(%newName, 0, %len - String::len(%numStr));
		}
		
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
			// Extract guardtype by removing trailing digits (more reliable than clipTrailingNumbers)
			// This works backwards from the end to find and remove only trailing digits
			%guardtype = %newName;
			%len = String::len(%newName);
			%numStr = "";
			%digitString = "0123456789";
			
		// Find trailing digits (working backwards)
		for(%i = %len - 1; %i >= 0; %i--)
		{
			%char = String::getSubStr(%newName, %i, 1);
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
		
		// If we found trailing digits, remove them
		if(%numStr != "")
		{
			%guardtype = String::getSubStr(%newName, 0, %len - String::len(%numStr));
		}
			
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
		
		// Also schedule the standard verification as backup
		schedule("VerifyEnemyBotTeam(" @ %aiId @ ", \"" @ %newName @ "\", " @ %botTeam @ ");", 0.5);
			
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


//
//This function will move an AI player to the position of an object
//that the players LOS is hitting(terrain included). Must be within 50 units.
//
//
function AI::moveToLOS(%aiName, %commandIssuer) 
{
	dbecho($dbechoMode, "AI::moveToLos(" @ %aiName @ ", " @ %commandIssuer @ ")");

	// Validate command issuer still exists
	%playerObj = Client::getOwnedObject(%commandIssuer);
	if(%playerObj == "" || %playerObj == -1)
	{
		dbecho(2, "AI::moveToLOS - Command issuer no longer exists.");
		return;
	}

	%issuerRot = GameBase::getRotation(%commandIssuer);
	%playerPos = GameBase::getPosition(%commandIssuer);
      
	//check within max dist
	if(GameBase::getLOSInfo(%playerObj, 100, %issuerRot))
	{ 
		%newIssuedVec = $LOS::position;
		if(%newIssuedVec != "" && %newIssuedVec != "0 0 0")
		{
			%distance = Vector::getDistance(%playerPos, %newIssuedVec);
			dbecho(2, "Command accepted, AI player(s) moving....");
			dbecho(2, "distance to LOS: " @ %distance);
			AI::newDirectiveWaypoint( %aiName, %newIssuedVec, 99 );
		}
	}
	else
		dbecho(2, "Distance too far.");

	dbecho(2, "LOS point: " @ $LOS::position);
}

//This function will move an AI player to a position directly in front of
//the player passed, at a distance that is specified.
function AI::moveAhead(%aiName, %commandIssuer, %distance) 
{
	dbecho($dbechoMode, "AI::moveAhead(" @ %aiName @ ", " @ %commandIssuer @ ", " @ %distance @ ")");

	// Validate command issuer still exists
	%playerObj = Client::getOwnedObject(%commandIssuer);
	if(%playerObj == "" || %playerObj == -1)
	{
		dbecho(2, "AI::moveAhead - Command issuer no longer exists.");
		return;
	}

	%issuerRot = GameBase::getRotation(%commandIssuer);
	%commPos  = GameBase::getPosition(%commandIssuer);
	dbecho(2, "Commanders Position: " @ %commPos);

	//get commanders x and y positions
	%comm_x = getWord(%commPos, 0);
	%comm_y = getWord(%commPos, 1);

	//get offset x and y positions
	%offSetPos = Vector::getFromRot(%issuerRot, %distance);
	%off_x = getWord(%offSetPos, 0);
	%off_y = getWord(%offSetPos, 1);

	//calc new position
	%new_x = %comm_x + %off_x;
	%new_y = %comm_y + %off_y;
	%newPos = %new_x  @ " " @ %new_y @ " 0";

	//move AI player
	dbecho(2, "AI moving to " @ %newPos);
	AI::newDirectiveWaypoint(%aiName, %newPos, 99);
}  

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

	// Tier 3: Search arrays via Client iteration
	for(%checkId = Client::getFirst(); %checkId != -1; %checkId = Client::getNext(%checkId))
	{
		if($EnemyBotData[%checkId, "BotInfoAiName"] == %aiName)
			return %checkId;
		if($ClientData[%checkId, "BotInfoAiName"] == %aiName)
			return %checkId;
	}

	// Tier 4: Fallback range loop (2049-2200)
	for(%checkId = 2049; %checkId <= 2200; %checkId++)
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
		storeData(%aiId, "SealBattleOriginalLVL", "");
		storeData(%aiId, "SealBattleOriginalRemortStep", "");
		storeData(%aiId, "SealBattleOriginalEndurance", "");
		storeData(%aiId, "SealBattleOriginalEnergy", "");
		storeData(%aiId, "SealBattleOriginalWeightCapacity", "");
		storeData(%aiId, "AImoveChance", "");
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
			UnregisterBot(%aiId);
			
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
			$PetList = RemoveFromCommaList($PetList, %aiId);
			%petowner = fetchData(%aiId, "petowner");
			storeData(%petowner, "PersonalPetList", RemoveFromCommaList(fetchData(%petowner, "PersonalPetList"), %aiId));
			Client::sendMessage(%petowner, $MsgRed, Client::getName(%aiId) @ " was slain!");
			storeData(%aiId, "petowner", "");
			
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
				echo("[TOWN BOT CLEANUP] AI::onDroneKilled - Cleared $TownBotSpawned[" @ %botName @ "] for clientId " @ %aiId);
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
			
			// Delete player object and schedule respawn
			if(%playerObj != -1 && %playerObj != "")
				schedule("if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ ");", 1.0);
			
			schedule("AI::setupAI(" @ %aiName @ ", " @ %team @ ");", 60);
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

function AI::sayLater(%clientId, %guardId, %message, %look)
{
	dbecho($dbechoMode, "AI::sayLater(" @ %clientId @ ", " @ %guardId @ ", " @ %message @ ", " @ %look @ ")");

	// Validate client ID exists
	if(%clientId == "" || %clientId == -1)
		return;

	// Get guard name (try BotInfo first, then Client::getName)
	%guardName = "";
	if(%guardId.name != "")
		%guardName = $BotInfo[%guardId.name, NAME];
	if(%guardName == "")
		%guardName = Client::getName(%guardId);
	if(%guardName == "")
		%guardName = "Banker"; // Fallback

	Client::sendMessage(%clientId, $MsgBeige, %guardName @ " tells you, \"" @ %message @ "\"");

	if(%look)
		AI::lookAtPlayer(%clientId, %guardId);
}
function AI::lookAtPlayer(%clientId, %guardId)
{
	dbecho($dbechoMode, "AI::lookAtPlayer(" @ %clientId @ ", " @ %guardId @ ")");

	// Validate both client and guard still exist
	%clientPlayer = Client::getOwnedObject(%clientId);
	// CRITICAL FIX: Fallback if engine hasn't updated getOwnedObject yet
	if(%clientPlayer == -1 || %clientPlayer == "")
		%clientPlayer = FindPlayerInBotGroup(%clientId);

	%guardPlayer = Client::getOwnedObject(%guardId);
	// CRITICAL FIX: Fallback for guard too
	if(%guardPlayer == -1 || %guardPlayer == "")
		%guardPlayer = FindPlayerInBotGroup(%guardId);

	if(%clientPlayer == "" || %clientPlayer == -1 || %guardPlayer == "" || %guardPlayer == -1)
		return;

	// Use player objects for position calculations
	%clpos = GameBase::getPosition(%clientPlayer);
	%gupos = GameBase::getPosition(%guardPlayer);

	// Calculate direction vector from bot to player
	%v1 = Vector::sub(%clpos, %gupos);

	// For horizontal rotation, use only X and Y components (ignore Z/height)
	%dx = GetWord(%v1, 0);
	%dy = GetWord(%v1, 1);
	%horizontalVec = %dx @ " " @ %dy @ " 0";
	
	%norm = Vector::normalize(%horizontalVec);
	%rot = Vector::getRotation(%norm);

	%gurot = GameBase::getRotation(%guardPlayer);
	%temp = Vector::sub(%rot, %gurot);
	%temp2 = GetWord(%temp, 2);

	// For town bots (Player objects), we want them to face TOWARDS the player, not away
	// The original code added 180 degrees which made them face away - remove that for town bots
	// CRITICAL: Use isTownBot() helper function which properly distinguishes town bots from enemy bots
	// Town bots have BotInfoAiName but NO SpawnBotInfo
	// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
	%isTownBot = isTownBot(%guardId);
	
	if(%isTownBot)
	{
		// Town bot - face towards player (don't add 180 degrees)
		// Preserve current pitch and roll, only update yaw (horizontal rotation)
		%currentPitch = GetWord(%gurot, 0);
		%currentRoll = GetWord(%gurot, 1);
		%newYaw = GetWord(%rot, 2);
		%rot = %currentPitch @ " " @ %currentRoll @ " " @ %newYaw;
	}
	else
	{
		// Enemy bot: face directly towards player; do not add 180
		%currentPitch = GetWord(%gurot, 0);
		%currentRoll = GetWord(%gurot, 1);
		%newYaw = GetWord(%rot, 2);
		%rot = %currentPitch @ " " @ %currentRoll @ " " @ %newYaw;
	}

	// Pass the guard clientId to RotateTownBot (town bots are now Player objects, need clientId)
	// %guardId is already a clientId, so use it directly
	RotateTownBot(%guardId, %rot);
}

function getAInumber()
{
	dbecho($dbechoMode, "getAInumber()");

	for(%i = 0; %i <= 5000; %i++)
	{
		if($aiNumTable[%i] == "")
		{
			return %i;
		}
	}
}
function setAInumber(%aiName, %n)
{
	dbecho($dbechoMode, "setAInumber(" @ %aiName @ ", " @ %n @ ")");

	$aiNumTable[%n] = True;
	$tmpbotn[%aiName] = %n;
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
	if(%storedBotTeam != "" && %storedBotTeam != -1 && %storedBotTeam != "0" && %storedBotTeam != 0)
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
			if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
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
				if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
				{
					echo("WARNING: HardcodeAIskills - No team info for bot " @ %botInfoAiName @ " (clientId=" @ %aiId @ "). Defaulting to team 1.");
					%storedBotTeam = 1;
					storeData(%aiId, "botTeam", 1);
				}
			}
			
			// If botTeam is stored, ALWAYS set it right before RefreshAll() (don't just check)
			// This ensures the team is set even if GameBase::setTeam() wasn't synchronous earlier
			if(%storedBotTeam != "" && %storedBotTeam != -1 && %storedBotTeam != "0" && %storedBotTeam != 0)
			{
				// CRITICAL: Set team on client ID FIRST (same as RefreshAll checks)
				// Set it multiple times to ensure it takes effect
				GameBase::setTeam(%aiId, %storedBotTeam);
				GameBase::setTeam(%playerObjFinal, %storedBotTeam);
				GameBase::setTeam(%aiId, %storedBotTeam); // Set again on client ID
				
				// NOTE: GameBase::getTeam() is NOT synchronous - it may return stale values
				// We set the team above, so we trust it's set and proceed with RefreshAllEnemyBot()
				// The team will be verified later by VerifyEnemyBotTeam if needed
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
}

// Helper function to verify team was set after HardcodeAIskills restoration
function VerifyTeamAfterHardcodeAIskills(%clientId, %expectedTeam, %botName)
{
	%currentTeam = GameBase::getTeam(%clientId);
	if(%currentTeam != %expectedTeam)
	{
		// Team still not set - try one more time
		GameBase::setTeam(%clientId, %expectedTeam);
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj != -1 && %playerObj != "")
			GameBase::setTeam(%playerObj, %expectedTeam);
		if($AI_DEBUG_ENABLED) echo("[BOT TEAM DEBUG] VerifyTeamAfterHardcodeAIskills - Team still " @ %currentTeam @ " for " @ %botName @ " (clientId=" @ %clientId @ "), retried setting to " @ %expectedTeam);
	}
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

// Start periodic shell bot check after 60 seconds
schedule("PeriodicShellBotCheck();", 60);

// PeriodicGhostClientIdCleanup: Scans for and cleans up ghost client IDs periodically
function PeriodicGhostClientIdCleanup()
{
	Watchdog_Enter("PeriodicGhostClientIdCleanup");
	// Scan a range of client IDs for ghost objects (empty name Player objects that are bots)
	%startId = 2000;  // Adjust based on your server's client ID range
	%endId = %startId + 300;  // Check up to 300 IDs ahead
	
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

// Start periodic ghost client ID cleanup on server initialization
// This will run every 30 seconds to catch any ghost client IDs that slip through
schedule("PeriodicGhostClientIdCleanup();", 60);  // Start after 60 seconds to let server fully initialize

//------- TownBot stuff ----------------------------------------

// Global variables for dynamic bot loading
$TownBotRegistry = "";  // List of registered bot names (not spawned yet)
$TownBotZone[0] = "";  // Maps bot name to zone index (0 = no zone/unloaded area)
$TownBotSpawned[0] = "";  // Maps bot name to clientId if spawned, "" if not spawned
$ZonePlayerCount[0] = 0;  // Tracks number of players in each zone
$ZoneBotDespawnSchedule[0] = "";  // Tracks scheduled despawn for each zone

function InitTownBots()
{
	dbecho($dbechoMode, "InitTownBots() - Registering bots for dynamic loading");
	echo("===== Calling InitTownBots() - Registering bots for dynamic loading =====");

	$TownBotList = "";  // Clear the list (now stores clientIds, not Item object IDs)
	$TownBotRegistry = "";  // Clear bot registry

	%group = nameToId("MissionGroup/TownBots");

	if(%group != -1)
	{
		%cnt = Group::objectCount(%group);
		for(%i = 0; %i <= %cnt - 1; %i++)
		{
			%object = Group::getObject(%group, %i);
			if(%object == -1 || %object == "")
				continue;
				
			%name = Object::getName(%object);
			%marker = "";  // Initialize marker
			
			if(getObjectType(%object) == "SimGroup")
			{
				// Call GatherBotInfo to parse bot info and get marker
				// This function parses the SimGroup structure and returns the marker object
				%marker = GatherBotInfo(%object);
				
				// Debug logging for specific merchants
				if(%name == "merchant14" || %name == "merchant19")
				{
					echo("DEBUG: GatherBotInfo for " @ %name @ " returned marker=" @ %marker);
				}
				
				// If GatherBotInfo failed or returned invalid marker, skip this bot
				if(%marker == "" || %marker == -1)
				{
					echo("WARNING: InitTownBots - GatherBotInfo returned invalid marker for " @ %name);
					if(%name == "merchant14" || %name == "merchant19")
					{
						echo("ERROR: " @ %name @ " will NOT spawn - marker is invalid!");
					}
					continue;
				}
			}
			else
			{
				// If it's not a SimGroup, it might be a Marker directly
				if(getObjectType(%object) == "Marker")
					%marker = %object;
			}

			// Skip if we don't have a valid marker
			if(%marker == "" || %marker == -1)
			{
				echo("WARNING: InitTownBots - No valid marker found for " @ %name);
				continue;
			}

			// Register bot for dynamic loading (don't spawn yet)
			%spawnPos = GameBase::getPosition(%marker);
			%spawnRot = GameBase::getRotation(%marker);
			
			// Store bot position and rotation for later spawning
			$BotInfo[%name, SPAWN_POS] = %spawnPos;
			$BotInfo[%name, SPAWN_ROT] = %spawnRot;
			$BotInfo[%name, SPAWN_MARKER] = %marker;
			
			// Determine which zone this bot belongs to
			%botZone = GetBotZone(%spawnPos);
			
			// FALLBACK: If zone detection failed (zone 0), try to manually assign based on bot name/position
			if(%botZone == 0)
			{
				// Try to find zone by description for known merchants
				%posX = GetWord(%spawnPos, 0);
				%posY = GetWord(%spawnPos, 1);
				
				// Debug: List all zones to help identify the correct one
				if(%name == "merchant14" || %name == "merchant19")
				{
					echo("DEBUG: Fallback triggered for " @ %name @ " at position " @ %spawnPos);
					echo("DEBUG: Searching through " @ $numZones @ " zones for matching description...");
				}
				
				// Black Market merchant (merchant14) - position around -2583 -1133 191
				if(%name == "merchant14" || (%posX >= -2600 && %posX <= -2560 && %posY >= -1160 && %posY <= -1100))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Black Market") == 0 || 
						   String::findSubStr(%zoneDesc, "Black Market") >= 0 ||
						   String::findSubStr(%zoneDesc, "black market") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Black Market zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Black Market zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Porellis merchant (merchant19) - position around -4479 1845 84
				else if(%name == "merchant19" || (%posX >= -4500 && %posX <= -4450 && %posY >= 1830 && %posY <= 1860))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Porellis") == 0 || 
						   String::findSubStr(%zoneDesc, "Porellis") >= 0 ||
						   String::findSubStr(%zoneDesc, "porellis") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Porellis zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Porellis zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Sanctuary merchants (merchant4, merchant5, assassin1) - position around -2844 -1323 694
				else if(%name == "merchant4" || %name == "merchant5" || %name == "assassin1" || (%posX >= -2860 && %posX <= -2830 && %posY >= -1330 && %posY <= -1315))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Sanctuary") == 0 || 
						   String::findSubStr(%zoneDesc, "Sanctuary") >= 0 ||
						   String::findSubStr(%zoneDesc, "sanctuary") >= 0 ||
						   String::findSubStr(%zoneDesc, "Empress Sanctuary") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Sanctuary zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Sanctuary zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Arbal Research Center (merchant9-11, banker4, hunt3, quest16) - position around -2034 -2870 302
				else if(%name == "merchant9" || %name == "merchant10" || %name == "merchant11" || %name == "banker4" || %name == "hunt3" || %name == "quest16" || (%posX >= -2050 && %posX <= -1970 && %posY >= -2900 && %posY <= -2840))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Arbal Research Center") == 0 || 
						   String::findSubStr(%zoneDesc, "Arbal") >= 0 ||
						   String::findSubStr(%zoneDesc, "arbal") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Arbal Research Center zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Arbal Research Center zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Kingdom of Kronos (merchant12-13, guildmaster1) - position around -3397 1712 1551
				else if(%name == "merchant12" || %name == "merchant13" || %name == "guildmaster1" || (%posX >= -3420 && %posX <= -3390 && %posY >= 1690 && %posY <= 1730))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Kingdom of Kronos") == 0 || 
						   String::findSubStr(%zoneDesc, "Kingdom of Kronos") >= 0 ||
						   String::findSubStr(%zoneDesc, "Kronos") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Kingdom of Kronos zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Kingdom of Kronos zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Yuliple City (manager1) - position around -1198.5 1503.25 72.776
				else if(%name == "manager1" || (%posX >= -1200 && %posX <= -1195 && %posY >= 1500 && %posY <= 1510))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Yuliple City") == 0 || 
						   String::findSubStr(%zoneDesc, "Yuliple") >= 0 ||
						   String::findSubStr(%zoneDesc, "yuliple") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Yuliple City zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Yuliple City zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
			}
			
			$TownBotZone[%name] = %botZone;
			$TownBotSpawned[%name] = "";  // Not spawned yet
			
			// Add to registry
			$TownBotRegistry = $TownBotRegistry @ %name @ " ";
			
			// Debug logging for specific merchants that aren't spawning
			if(%name == "merchant4" || %name == "merchant5" || %name == "merchant14" || %name == "merchant19" || %name == "banker1")
			{
				echo("DEBUG: Registered bot: " @ %name @ " at position " @ %spawnPos @ " for zone: " @ %botZone @ " (" @ $Zone::Desc[%botZone] @ ")");
				if(%botZone == 0)
				{
					echo("ERROR: " @ %name @ " was assigned to zone 0 (no zone) - bot will NOT spawn!");
				}
				// Verify it was actually added to registry
				if(%name == "banker1")
				{
					%regPos = String::findSubStr($TownBotRegistry, "banker1");
					if(%regPos >= 0)
						echo("DEBUG: banker1 confirmed in registry at position " @ %regPos);
					else
						echo("ERROR: banker1 NOT found in registry after adding! Registry: '" @ $TownBotRegistry @ "'");
				}
			}
			else
			{
				echo("Registered bot: " @ %name @ " for zone: " @ %botZone @ " (" @ $Zone::Desc[%botZone] @ ")");
			}
		}
	}
	
	echo("===== InitTownBots() completed - Registered " @ GetWordCount($TownBotRegistry) @ " bots for dynamic loading =====");
	
	// Start periodic empty zone check to catch zones that should be empty but aren't being despawned
	// This runs every 30 seconds to verify zones are actually empty and despawn bots if needed
	schedule("PeriodicEmptyZoneCheck();", 60);  // Start after 60 seconds to let server fully initialize
	
	// Start periodic bot team check to fix bots that are on team -1
	// This runs every 15 seconds to verify and fix bot teams (increased frequency for faster detection)
	schedule("PeriodicBotTeamCheck();", 30);  // Start after 30 seconds to let server fully initialize
}

// Determine which zone a bot position belongs to (returns zone index, 0 if no zone)
function GetBotZone(%position)
{
	%posX = GetWord(%position, 0);
	%posY = GetWord(%position, 1);
	%posZ = GetWord(%position, 2);
	
	// Debug: Check if zones are initialized
	if($numZones == "" || $numZones == 0)
	{
		echo("ERROR: GetBotZone - $numZones is not set! Zones may not be initialized. Position: " @ %position);
		return 0;
	}
	
	// Debug logging for first few calls to see what's happening
	%debugThis = false;
	// Check if this is a known problematic position
	if((%posX >= -2860 && %posX <= -2830 && %posY >= -1330 && %posY <= -1315) ||  // Sanctuary merchants (merchant4/5)
	   (%posX >= -1200 && %posX <= -1195 && %posY >= 1500 && %posY <= 1510) ||  // manager1
	   (%posX >= -2590 && %posX <= -2570 && %posY >= -1160 && %posY <= -1120) ||  // Black Market merchant14
	   (%posX >= -4490 && %posX <= -4470 && %posY >= 1840 && %posY <= 1850))  // Porellis merchant19
	{
		%debugThis = true;
		echo("DEBUG: GetBotZone checking position " @ %position @ " against " @ $numZones @ " zones");
	}
	
	for(%z = 1; %z <= $numZones; %z++)
	{
		%zonePos = $Zone::Marker[%z];
		%zoneX = GetWord(%zonePos, 0);
		%zoneY = GetWord(%zonePos, 1);
		%zoneZ = GetWord(%zonePos, 2);
		
		%length = $Zone::Length[%z];
		%width = $Zone::Width[%z];
		%height = $Zone::Height[%z];
		%sheight = $Zone::SHeight[%z];
		
		// Check if zone data is valid
		if(%zonePos == "" || %length == "" || %width == "")
		{
			if(%debugThis)
				echo("DEBUG: GetBotZone - Zone " @ %z @ " has invalid data (pos='" @ %zonePos @ "', length='" @ %length @ "', width='" @ %width @ "')");
			continue;
		}
		
		// Check if position is within zone bounds
		%halfLength = %length / 2;
		%halfWidth = %width / 2;
		
		%minX = %zoneX - %halfLength;
		%maxX = %zoneX + %halfLength;
		%minY = %zoneY - %halfWidth;
		%maxY = %zoneY + %halfWidth;
		%minZ = %zoneZ - %sheight;
		%maxZ = %zoneZ + %height;
		
		// RELAXED Z BOUNDS: Allow some tolerance for Z coordinate (bots may be slightly above/below zone floor)
		// Add 10 units of tolerance below the zone minimum to account for bots on slightly lower ground
		%minZ = %minZ - 10;
		
		if(%debugThis && %z <= 5)  // Debug first 5 zones
		{
			echo("DEBUG: Zone " @ %z @ " (" @ $Zone::Desc[%z] @ "): center=(" @ %zoneX @ " " @ %zoneY @ " " @ %zoneZ @ "), bounds X[" @ %minX @ " to " @ %maxX @ "], Y[" @ %minY @ " to " @ %maxY @ "], Z[" @ %minZ @ " to " @ %maxZ @ "]");
			echo("DEBUG: Position (" @ %posX @ " " @ %posY @ " " @ %posZ @ ") - X in bounds: " @ (%posX >= %minX && %posX <= %maxX) @ ", Y in bounds: " @ (%posY >= %minY && %posY <= %maxY) @ ", Z in bounds: " @ (%posZ >= %minZ && %posZ <= %maxZ));
		}
		
		if(%posX >= %minX && %posX <= %maxX &&
		   %posY >= %minY && %posY <= %maxY &&
		   %posZ >= %minZ && %posZ <= %maxZ)
		{
			if(%debugThis)
				echo("DEBUG: GetBotZone found zone " @ %z @ " (" @ $Zone::Desc[%z] @ ") for position " @ %position);
			return %z;
		}
	}
	
	// Debug logging if no zone found for known positions
	if(%debugThis)
	{
		echo("ERROR: GetBotZone - No zone found for position " @ %position);
		echo("  Checked " @ $numZones @ " zones but position is outside all zone bounds");
		echo("  Position: X=" @ %posX @ ", Y=" @ %posY @ ", Z=" @ %posZ);
	}
	
	return 0;  // No zone found
}

// Post-spawn initialization function for town bots
// Called after a 2-second delay to allow Player object to register in client list
function SpawnZoneBotPostSpawn(%aiName, %botName, %displayName, %zoneIndex)
{
	// For town bots, AI::spawn() creates a Player object, not a Drone
	// AI::getClientIdFromName() calls AI::getId() internally which fails for Player objects
	// So we skip it and search through all clients by display name directly
	// CRITICAL: Set expectedAiName BEFORE searching so it's available for validation
	%expectedAiName = "TownBot_" @ %botName;
	%clientId = "";
	
	// Try NEWgetClientByName first (faster if it works)
	%clientId = NEWgetClientByName(%displayName);
	
	// If that fails, search through all clients by display name
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%list = GetEveryoneIdList();
		for(%clientIndex = 0; GetWord(%list, %clientIndex) != -1; %clientIndex++)
		{
			%id = GetWord(%list, %clientIndex);
			%clientDisplayName = Client::getName(%id);
			if(String::ICompare(%displayName, %clientDisplayName) == 0)
			{
				// Found a client with matching display name
				// Check if it's already assigned to a different bot
				%existingBotInfoAiName = fetchData(%id, "BotInfoAiName");
				if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
				{
					// This client doesn't have a BotInfoAiName set - it's the newly spawned bot!
					%clientId = %id;
					break;
				}
				else if(%existingBotInfoAiName == %expectedAiName)
				{
					// Already assigned to this bot (shouldn't happen, but handle it)
					%clientId = %id;
					break;
				}
				else
				{
					// This client is already assigned to a different bot - skip it
				}
			}
		}
	}
	
	// If still not found, try brute-force search through recent client IDs (like RetryGetAIId does)
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		// Try checking a range of recent client IDs (bots typically get high IDs)
		// Start from 2049 (where player IDs begin) and check up to 2200
		%startId = 2049;  // Player IDs start at 2049
		%endId = 2200;
		
		for(%checkId = %startId; %checkId <= %endId; %checkId++)
		{
			// CRITICAL: Check if this client ID was recently freed BEFORE checking name match
			// This prevents reusing client IDs that are still being cleaned up, even if the name matches
			%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				if(%timeSinceFreed < 5) // 5 seconds for cleanup
				{
					continue; // Skip this client ID, cleanup still in progress
				}
				else
				{
					// Enough time has passed, clear the flag
					$ClientIdRecentlyFreed[%checkId] = "";
				}
			}
			
			// CRITICAL: Check if Player object exists and is valid BEFORE checking name match
			%playerObj = Client::getOwnedObject(%checkId);
			if(%playerObj == -1 || %playerObj == "")
			{
				continue; // Skip this client ID, player object not ready
			}
			
			// Player object exists - check if it's actually valid
			if(!isObject(%playerObj))
			{
				continue; // Skip this client ID, it's a shell bot being cleaned up
			}
			
			%checkName = Client::getName(%checkId);
			if(%checkName != "" && %checkName != -1)
			{
				if(String::ICompare(%displayName, %checkName) == 0)
				{
					// Found it! Check if it's already assigned to a bot
					%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
					if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
					{
						%clientId = %checkId;
						break;
					}
					else if(%existingBotInfoAiName == %expectedAiName)
					{
						%clientId = %checkId;
						break;
					}
				}
			}
		}
	}
	
	// If that fails, retry after a short delay (timing issue - Player object may not be registered yet)
	if(%clientId == -1 || %clientId == "")
	{
		echo("WARNING: SpawnZoneBotPostSpawn - Could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ "). Retrying...");
		// Use longer delay - bots need time to register in client list (1.0s additional delay after the initial 1.0s)
		schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 1.0);
		return;
	}
	
	// VALIDATE: Check if player object actually exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		echo("ERROR: SpawnZoneBotPostSpawn - Bot " @ %botName @ " spawned but player object is invalid! clientId=" @ %clientId);
		// Clean up the broken bot - use deleteObject on the player object if we have the clientId
		// Note: We don't use AI::delete() for town bots since they're Player objects, not Drones
		if(%clientId != -1 && %clientId != "")
		{
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != -1 && %playerObj != "")
				deleteObject(%playerObj);
		}
		return;
	}
	
	// CRITICAL: Check if BotInfoAiName is already set BEFORE we set it
	// This prevents false positives from our own assignment
	%existingBotInfoAiName = fetchData(%clientId, "BotInfoAiName");
	
	// CRITICAL: Double-check that this clientId isn't already assigned to a different town bot
	// Only check if it was already set (not by us) and doesn't match expected
	if(%existingBotInfoAiName != "" && %existingBotInfoAiName != -1 && %existingBotInfoAiName != "0" && %existingBotInfoAiName != %expectedAiName)
	{
		echo("ERROR: SpawnZoneBotPostSpawn - Found clientId " @ %clientId @ " is already assigned to different bot: " @ %existingBotInfoAiName @ " (expected: " @ %expectedAiName @ "). This should not happen!");
		// This is a critical error - we found a clientId that's already assigned to a different bot
		// This means our search logic failed. Delete the newly spawned bot and retry.
		if(%playerObj != -1 && %playerObj != "")
			deleteObject(%playerObj);
		schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 0.1);
		return;
	}
	
	// CRITICAL: Set BotInfoAiName IMMEDIATELY to prevent UpdateZone from processing this bot
	// This must happen before any zone detection runs, so IsTownBot() works correctly
	%aiNameFull = "TownBot_" @ %botName;
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	
	// CRITICAL: Set team IMMEDIATELY after spawn to prevent default team -1
	// AI::spawn() may default Player objects to team -1, so we must set it right away
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// CRITICAL: First, clear any stale enemy bot data from this client ID
	// This prevents stale data from making the town bot look like an enemy bot
	%staleSpawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
	if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
	{
		// Clear stale enemy bot data - the player object at this client ID is the new town bot, not the old enemy bot
		echo("WARNING: SpawnZoneBotPostSpawn - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
		// Clear all enemy bot data fields that could cause conflicts from new array
		$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
		$EnemyBotData[%clientId, "SpawnTime"] = "";
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
		// NOTE: Don't clear BotInfoAiName here - we just set it for the town bot above!
		// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
		$ClientData[%clientId, "SpawnBotInfo"] = "";
		$ClientData[%clientId, "SpawnTime"] = "";
	}
	
	// Reset retry counter on successful spawn
	$TownBotSpawned[%botName] = %clientId;
	
	// PRIORITY 2: Set $BotType cache for O(1) bot type detection
	$BotType[%clientId] = "town";
	
	// Add to TownBotList immediately so bots can be found for interaction
	$TownBotList = $TownBotList @ %clientId @ " ";
	
	// CRITICAL: Clear SpawnBotInfo to prevent town bots from being mistaken for enemy bots
	// Town bots should NOT have SpawnBotInfo set (only enemy bots have it)
	// This prevents zone change detection from killing town bots
	$TownBotData[%clientId, "SpawnBotInfo"] = "";
	$TownBotData[%clientId, "SpawnTime"] = "";
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", "");
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "SpawnTime"] = "";
	
	// CRITICAL: Set HasLoadedAndSpawned flag to indicate bot is fully initialized
	// This allows DespawnZoneBots() to identify fully loaded bots
	storeData(%clientId, "HasLoadedAndSpawned", True);
	
	// CRITICAL: Schedule a team verification after a short delay to catch UpdateTeam() overriding it
	schedule("VerifyTownBotTeam(" @ %clientId @ ", \"" @ %botName @ "\", " @ %botTeam @ ");", 0.2);
	
	// Initialize bot post-spawn (must store clientId in $TownBotSpawned first so InitTownBotPostSpawn can find it)
	// InitTownBotPostSpawn expects to find the clientId in $TownBotSpawned[%botName]
	schedule("InitTownBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\");", 0.1);
	// Schedule item mounting after post-spawn completes (allow time for initialization)
	schedule("InitTownBotItemsForBot(" @ %clientId @ ", \"" @ %botName @ "\");", 1.5);
	echo("Spawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ")");
}

// Retry function for getting client ID when timing issues occur
// This handles cases where AI::spawn() succeeds but we can't get the client ID immediately
function RetryGetAIId(%aiName, %botName, %displayName, %zoneIndex)
{
	// For town bots, they are Player objects, not Drones
	// AI::getClientIdFromName() calls AI::getId() internally which fails for Player objects
	// So we skip it and use NEWgetClientByName() directly
	%clientId = NEWgetClientByName(%displayName);
	
	// If still not found, try searching through all client IDs directly (brute force approach)
	// This handles cases where GetEveryoneIdList() hasn't updated yet
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		// Try checking a range of recent client IDs (bots typically get high IDs)
		// Start from 2049 (where player IDs begin) and check up to 2200
		%startId = 2049;  // Player IDs start at 2049
		%endId = 2200;
		
		for(%checkId = %startId; %checkId <= %endId; %checkId++)
		{
			// CRITICAL: Check if this client ID was recently freed BEFORE checking name match
			// This prevents reusing client IDs that are still being cleaned up, even if the name matches
			%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				if(%timeSinceFreed < 5) // 5 seconds for cleanup
				{
					continue; // Skip this client ID, cleanup still in progress
				}
				else
				{
					// Enough time has passed, clear the flag
					$ClientIdRecentlyFreed[%checkId] = "";
				}
			}
			
			// CRITICAL: Check if Player object exists and is valid BEFORE checking name match
			%playerObj = Client::getOwnedObject(%checkId);
			if(%playerObj == -1 || %playerObj == "")
			{
				continue; // Skip this client ID, player object not ready
			}
			
			// Player object exists - check if it's actually valid
			if(!isObject(%playerObj))
			{
				continue; // Skip this client ID, it's a shell bot being cleaned up
			}
			
			%checkName = Client::getName(%checkId);
			if(%checkName != "" && %checkName != -1)
			{
				if(String::ICompare(%displayName, %checkName) == 0)
				{
					// Found it! Check if it's already assigned to a bot
					%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
					if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
					{
						%clientId = %checkId;
						break;
					}
				}
			}
		}
	}
	
	// If still not found, try one more time with GetEveryoneIdList() (might have updated)
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%clientId = NEWgetClientByName(%displayName);
	}
	
	// If still not found after all attempts, try one more brute-force pass over everyone (names may include prefixes)
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%listAll = GetEveryoneIdList();
		%expectedDisplayName = $BotInfo[%botName, NAME];
		for(%iBF = 0; (%cidBF = GetWord(%listAll, %iBF)) != -1; %iBF++)
		{
			if(%cidBF == "" || %cidBF == -1)
				continue;
			%pObjBF = Client::getOwnedObject(%cidBF);
			if(%pObjBF == -1 || %pObjBF == "")
				continue;
			%nmBF = Client::getName(%cidBF);
			if(%nmBF == "" || %nmBF == -1)
				continue;
			// Skip real players with save files
			if(isFile("temp\\" @ %nmBF @ ".cs"))
				continue;
			// Match exact displayName, or expected town bot name, or case-insensitive substring match
			if(String::ICompare(%nmBF, %displayName) == 0 ||
			   (%expectedDisplayName != "" && String::ICompare(%nmBF, %expectedDisplayName) == 0) ||
			   (%expectedDisplayName != "" && String::findSubStr(%nmBF, %expectedDisplayName) != -1))
			{
				%existingBotInfoAiName = fetchData(%cidBF, "BotInfoAiName");
				if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
				{
					%clientId = %cidBF;
					break;
				}
			}
		}
	}

	// If still not found after all attempts, check retry count
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%retryCount = $TownBotRetryGetAIIdCount[%botName];
		if(%retryCount == "")
			%retryCount = 0;
		
		if(%retryCount < 10)
		{
			// Try again with exponential backoff
			$TownBotRetryGetAIIdCount[%botName] = %retryCount + 1;
			%delay = 0.5 * (%retryCount + 1);  // up to 5s by attempt 10
			echo("WARNING: RetryGetAIId - Could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ ", aiName: " @ %aiName @ "). Retrying (attempt " @ (%retryCount + 1) @ "/10) in " @ %delay @ "s...");
			schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", %delay);
			return;
		}
		else
		{
			echo("ERROR: RetryGetAIId - Still could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ ", aiName: " @ %aiName @ ") after 10 retries.");
			echo("  AI::spawn() may have succeeded but the Player object is not yet registered in the client list.");
			$TownBotRetryGetAIIdCount[%botName] = "";
			return;
		}
	}
	
	// Reset retry counter on success
	$TownBotRetryGetAIIdCount[%botName] = "";
	
	// Bot was found on retry - continue with initialization
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		echo("ERROR: RetryGetAIId - Bot " @ %botName @ " found but player object is invalid! clientId=" @ %clientId);
		return;
	}
	
	// Mark invulnerability during init to avoid early death before full setup
	storeData(%clientId, "SpawnInvuln", True);
	
	// CRITICAL: Check if this client ID is already in use by a REAL PLAYER (not a bot)
	// If a real player is using this client ID, we cannot use it for a bot
	// NOTE: For town bots, we check if the name matches known town bot patterns or if it's a real player name
	%actualName = Client::getName(%clientId);
	if(%actualName != "" && %actualName != -1)
	{
		// Check if the name matches enemy bot patterns (enemy bots have prefixes like "Alien", "Admin", "Demon", etc.)
		%isEnemyBotName = HasEnemyBotNamePrefix(%actualName);
		
		// Check if isRPGAI() returns true (bot data is set)
		%hasBotData = isRPGAI(%clientId);
		
		// If it's not an enemy bot name pattern and doesn't have bot data, it might be a real player
		// BUT: Town bots don't have enemy bot name patterns, so we need to check differently
		// For town bots, we check if the name matches the expected display name for this town bot
		%expectedDisplayName = $BotInfo[%botName, NAME];
		%isExpectedTownBot = false;
		if(%expectedDisplayName != "" && %expectedDisplayName != -1)
		{
			// Check if the actual name matches the expected town bot display name (with or without zone prefix)
			if(String::ICompare(%actualName, %expectedDisplayName) == 0 ||
			   String::findSubStr(%actualName, %expectedDisplayName) >= 0)
			{
				%isExpectedTownBot = true;
			}
		}
		
		// If it's not an enemy bot, doesn't have bot data, and doesn't match expected town bot name, it's likely a real player
		if(!%isEnemyBotName && !%hasBotData && !%isExpectedTownBot)
		{
			// This is likely a REAL PLAYER using this client ID - cannot spawn bot here
			echo("ERROR: RetryGetAIId - Client ID " @ %clientId @ " is already in use by real player '" @ %actualName @ "'. Cannot spawn town bot " @ %botName @ " (expected: '" @ %expectedDisplayName @ "'). Aborting...");
			// Clean up any bot data that might have been set
			storeData(%clientId, "SpawnBotInfo", "");
			storeData(%clientId, "BotInfoAiName", "");
			$TownBotData[%clientId, "SpawnBotInfo"] = "";
			$TownBotData[%clientId, "BotInfoAiName"] = "";
			$TownBotSpawned[%botName] = "";
			return;  // Abort spawn
		}
	}
	
	// CRITICAL: Check for ghost client ID (empty name Player object)
	// BUT: We need to verify this is actually a ghost and not just a bot whose name hasn't been set yet
	if(%actualName == "" || %actualName == -1)
	{
		// Name is empty - check if this AI name is already in use by another active bot
		%otherBotId = AI::getClientIdFromName(%aiName);
		if(%otherBotId != -1 && %otherBotId != "" && %otherBotId != %clientId)
		{
			%otherBotName = Client::getName(%otherBotId);
			if(%otherBotName != "" && %otherBotName != -1)
			{
				// Another bot with this AI name exists - this is a ghost
				echo("WARNING: RetryGetAIId - Detected ghost client ID " @ %clientId @ " (empty name) for bot " @ %botName @ ". Another bot exists (clientId=" @ %otherBotId @ "). Cleaning up ghost...");
				CleanupGhostClientId(%clientId, %aiName);
				return;
			}
		}
		
		// Name is empty but no other bot with this AI name exists
		// This might be a bot whose name hasn't been set yet - wait a bit and check again
		schedule("CheckGhostClientIdDelayed(" @ %clientId @ ", \"" @ %aiName @ "\");", 0.5);
		// Continue processing - if it's truly a ghost, the delayed check will clean it up
	}
	
	// CRITICAL: Set BotInfoAiName IMMEDIATELY to prevent UpdateTeam() from changing team
	// This must happen BEFORE setting team so UpdateTeam() recognizes it as a town bot
	%aiNameFull = "TownBot_" @ %botName;
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	
	// CRITICAL: Set team IMMEDIATELY to prevent default team -1
	// AI::spawn() may default Player objects to team -1, so we must set it right away
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// Verify team was set correctly (debug check)
	%actualTeam = GameBase::getTeam(%clientId);
	if(%actualTeam != %botTeam)
	{
		echo("WARNING: RetryGetAIId - Team mismatch for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam @ ". Re-setting...");
		GameBase::setTeam(%clientId, %botTeam);
	}
	
	// CRITICAL: Check if this client ID was previously used by an enemy bot
	// Only clear if the enemy bot is actually dead/stale (not still alive)
	%staleSpawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
	if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
	{
		// Verify the enemy bot is actually dead by checking if it's still an active enemy bot
		%currentSpawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
		%currentBotInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
		
		// If the current bot is an active enemy bot (has SpawnBotInfo but different BotInfoAiName),
		// then this is a conflict - don't clear data, log error instead
		if(%currentSpawnBotInfo != "" && %currentSpawnBotInfo != "0" && %currentSpawnBotInfo != -1 && %currentBotInfoAiName != "")
		{
			echo("ERROR: RetryGetAIId - Client ID " @ %clientId @ " is still in use by an active enemy bot! Cannot spawn town bot " @ %botName @ ". This should not happen - AI::spawn() should not reuse active client IDs.");
			// Don't clear data - the enemy bot is still alive
			// Delete the town bot that was just spawned
			if(%playerObj != -1 && %playerObj != "")
				deleteObject(%playerObj);
			return;
		}
		
		// Enemy bot is dead/stale - safe to clear
		echo("WARNING: RetryGetAIId - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
		// Clear all enemy bot data fields that could cause conflicts from new array
		$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
		$EnemyBotData[%clientId, "SpawnTime"] = "";
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
		$EnemyBotData[%clientId, "BotInfoAiName"] = "";
		// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
		$ClientData[%clientId, "SpawnBotInfo"] = "";
		$ClientData[%clientId, "SpawnTime"] = "";
	}
	
	// Reset retry counter on successful spawn
	$TownBotSpawnRetry[%botName] = "";
	
	$TownBotSpawned[%botName] = %clientId;
	$BotType[%clientId] = "town";  // PRIORITY 2: Set $BotType cache
	$TownBotList = $TownBotList @ %clientId @ " ";
	// CRITICAL: Set BotInfoAiName in $TownBotData FIRST so GetClientDataType identifies it as a town bot
	// This must happen before clearing SpawnBotInfo to ensure proper type detection
	// CRITICAL: Must use "TownBot_" prefix to match SpawnZoneBots format
	%aiNameFull = "TownBot_" @ %botName;
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	$TownBotData[%clientId, "SpawnBotInfo"] = "";
	$TownBotData[%clientId, "SpawnTime"] = "";
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", "");
	// Also directly clear from $ClientData to ensure it's gone (migration mode)
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "SpawnTime"] = "";
	
	// CRITICAL: Set team IMMEDIATELY after setting BotInfoAiName to prevent UpdateTeam() from changing it
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// Verify team was set correctly (debug check)
	%actualTeam = GameBase::getTeam(%clientId);
	if(%actualTeam != %botTeam)
	{
		echo("WARNING: RetryGetAIId - Team mismatch for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam @ ". Re-setting...");
		GameBase::setTeam(%clientId, %botTeam);
		// Verify again after re-setting
		%actualTeam2 = GameBase::getTeam(%clientId);
		if(%actualTeam2 != %botTeam)
		{
			echo("ERROR: RetryGetAIId - Team still incorrect after re-set for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam2 @ ".");
		}
	}
	
	// CRITICAL: Set HasLoadedAndSpawned flag to indicate bot is fully initialized
	// This allows DespawnZoneBots() to identify fully loaded bots
	storeData(%clientId, "HasLoadedAndSpawned", True);
	// Clear spawn invulnerability now that init is complete
	storeData(%clientId, "SpawnInvuln", "");
	// Clear name-based invuln guard
	$SpawnInvulnByName[%displayName] = "";
	// Clear any lingering no-drop flags now that spawn succeeded
	storeData(%clientId, "noDropLootbagFlag", "");
	$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
	$ClientData[%clientId, "noDropLootbagFlag"] = "";
	// Clear any lingering no-drop flags now that spawn succeeded
	storeData(%clientId, "noDropLootbagFlag", "");
	$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
	$ClientData[%clientId, "noDropLootbagFlag"] = "";
	// Clear name-based invuln guard
	$SpawnInvulnByName[%displayName] = "";
	
	// CRITICAL: Schedule a team verification after a short delay to catch UpdateTeam() overriding it
	schedule("VerifyTownBotTeam(" @ %clientId @ ", \"" @ %botName @ "\", " @ %botTeam @ ");", 0.2);
	
	schedule("InitTownBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\");", 0.1);
	schedule("InitTownBotItemsForBot(" @ %clientId @ ", \"" @ %botName @ "\");", 1.5);
	echo("Spawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ") - found on retry");
}

// Spawn a single bot (helper function for retry after cleanup)
function SpawnSingleZoneBot(%botName, %zoneIndex)
{
	// Skip invalid bot names (like "0" which can appear in lists)
	if(%botName == "" || %botName == "0" || %botName == -1)
		return;
	
	if($TownBotZone[%botName] != %zoneIndex || $TownBotSpawned[%botName] != "")
		return;  // Bot not for this zone or already spawned
	
		// Spawn this bot
		%aiName = "TownBot_" @ %botName;
		
		// Get race and validate it exists
		%botRace = $BotInfo[%botName, RACE];
		if(%botRace == "" || %botRace == -1)
		{
			echo("ERROR: SpawnSingleZoneBot - Bot " @ %botName @ " has no RACE defined, defaulting to Human");
			%botRace = "Human";
		}
		
		%armor = $RaceToArmorType[%botRace];
		if(%armor == "" || %armor == -1)
		{
			echo("ERROR: SpawnSingleZoneBot - Could not find armor for race '" @ %botRace @ "' for bot " @ %botName @ ", defaulting to MaleHumanArmor");
			%armor = "MaleHumanArmor";
		}
		
		%spawnPos = $BotInfo[%botName, SPAWN_POS];
		%spawnRot = $BotInfo[%botName, SPAWN_ROT];
		%displayName = $BotInfo[%botName, NAME];
		
		// CRITICAL INTEGRATION: Check server capacity before spawning
		%predictedId = PlayerManager::getFreeId();
		if(%predictedId == -1)
		{
			echo("CRITICAL: SpawnSingleZoneBot - Server is FULL! Aborting spawn for " @ %botName);
			return;
		}
		
		if(AI::spawn(%aiName, %armor, %spawnPos, %spawnRot, %displayName, "male2") != "false")
	{
		// For town bots, AI::spawn() creates a Player object, not a Drone
		// AI::getClientIdFromName() calls AI::getId() internally which fails for Player objects
		// So we skip it and use NEWgetClientByName() directly
		%clientId = NEWgetClientByName(%displayName);
		
		// If that fails, retry after a short delay (timing issue - Player object may not be registered yet)
		if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
		{
			echo("WARNING: SpawnSingleZoneBot - Could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ ", aiName: " @ %aiName @ "). Retrying...");
			schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 0.1);
			return;
		}
		
		// VALIDATE: Check if player object actually exists
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == -1 || %playerObj == "")
		{
			echo("ERROR: SpawnSingleZoneBot - Bot " @ %botName @ " spawned but player object is invalid! clientId=" @ %clientId);
			// Clean up the broken bot - use deleteObject on the player object if we have the clientId
			// Note: We don't use AI::delete() for town bots since they're Player objects, not Drones
			if(%clientId != -1 && %clientId != "")
			{
				%playerObj = Client::getOwnedObject(%clientId);
				if(%playerObj != -1 && %playerObj != "")
					deleteObject(%playerObj);
			}
			return;
		}
		
		// CRITICAL: Set BotInfoAiName IMMEDIATELY to prevent UpdateTeam() from setting team to 1
		// This must happen before UpdateTeam() runs (called by Game::playerSpawned())
		%aiNameFull = "TownBot_" @ %botName;
		storeData(%clientId, "BotInfoAiName", %aiNameFull);
		$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
		
		// CRITICAL: Set team IMMEDIATELY after spawn to prevent default team -1
		// AI::spawn() may default Player objects to team -1, so we must set it right away
		// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
		%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
		GameBase::setTeam(%clientId, %botTeam);
		
		// CRITICAL: First, clear any stale enemy bot data from this client ID
		// This prevents stale data from making the town bot look like an enemy bot
		// Note: The player object at %clientId is the NEW town bot, so we can safely clear stale enemy bot data
		%staleSpawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
		%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
		if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
		{
			// Clear stale enemy bot data - the player object at this client ID is the new town bot, not the old enemy bot
			echo("WARNING: SpawnSingleZoneBot - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
			// Clear all enemy bot data fields that could cause conflicts from new array
			$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
			$EnemyBotData[%clientId, "SpawnTime"] = "";
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
			$EnemyBotData[%clientId, "BotInfoAiName"] = "";
			// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
			$ClientData[%clientId, "SpawnBotInfo"] = "";
			$ClientData[%clientId, "SpawnTime"] = "";
		}
		
		// NOW check if there's an active enemy bot using this client ID (after clearing stale data)
		%currentSpawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
		%currentBotInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
		
		// Check if there's an active enemy bot using this client ID
		// Verify by checking if the player object at this client ID is actually an enemy bot
		%checkPlayerObj = Client::getOwnedObject(%clientId);
		%checkIsAlive = (%checkPlayerObj != "" && %checkPlayerObj != -1);
		%hasActiveEnemyBot = (%checkIsAlive && %currentSpawnBotInfo != "" && %currentSpawnBotInfo != "0" && %currentSpawnBotInfo != -1 && %currentBotInfoAiName != "" && %currentBotInfoAiName != %botName);
		
		if(%hasActiveEnemyBot)
		{
			// Active enemy bot conflict - delete and retry (up to 3 attempts)
			%retryCount = $TownBotSpawnRetry[%botName];
			if(%retryCount == "")
				%retryCount = 0;
			
			if(%retryCount < 3)
			{
				echo("WARNING: SpawnSingleZoneBot - Client ID " @ %clientId @ " is in use by an active enemy bot. Deleting and retrying spawn for town bot " @ %botName @ " (attempt " @ (%retryCount + 1) @ "/3).");
				// Delete the bot that was just spawned
				if(%playerObj != -1 && %playerObj != "")
					deleteObject(%playerObj);
				// Increment retry counter and retry
				$TownBotSpawnRetry[%botName] = %retryCount + 1;
				schedule("SpawnSingleZoneBot(\"" @ %botName @ "\", " @ %zoneIndex @ ");", 0.2);
				return;
			}
			else
			{
				// Max retries reached - give up
				echo("ERROR: SpawnSingleZoneBot - Failed to spawn town bot " @ %botName @ " after 3 retries. Client ID " @ %clientId @ " is still in use by an active enemy bot.");
				if(%playerObj != -1 && %playerObj != "")
					deleteObject(%playerObj);
				$TownBotSpawnRetry[%botName] = "";
				return;
			}
		}
		
		// Reset retry counter on successful spawn
		$TownBotSpawnRetry[%botName] = "";
		
		// Check if there's stale enemy bot data (dead/stale bot)
		// Also check $ClientData (old array) since we're in migration mode
		%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
		if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
		{
			// Enemy bot is dead/stale - safe to clear
			echo("WARNING: SpawnSingleZoneBot - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
			// Clear all enemy bot data fields that could cause conflicts from new array
			$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
			$EnemyBotData[%clientId, "SpawnTime"] = "";
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
			$EnemyBotData[%clientId, "BotInfoAiName"] = "";
			// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
			$ClientData[%clientId, "SpawnBotInfo"] = "";
			$ClientData[%clientId, "SpawnTime"] = "";
		}
		
		// Reset retry counter on successful spawn
		$TownBotSpawnRetry[%botName] = "";
		
		$TownBotSpawned[%botName] = %clientId;
		$BotType[%clientId] = "town";  // PRIORITY 2: Set $BotType cache
		
		// Add to TownBotList immediately so bots can be found for interaction
		$TownBotList = $TownBotList @ %clientId @ " ";
		
		// CRITICAL: Set BotInfoAiName in $TownBotData FIRST so GetClientDataType identifies it as a town bot
		// This must happen before clearing SpawnBotInfo to ensure proper type detection
		// CRITICAL: Must use "TownBot_" prefix to match SpawnZoneBots format
		%aiNameFull = "TownBot_" @ %botName;
		$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
		storeData(%clientId, "BotInfoAiName", %aiNameFull);
		
		// CRITICAL: Clear SpawnBotInfo to prevent town bots from being mistaken for enemy bots
		// Town bots should NOT have SpawnBotInfo set (only enemy bots have it)
		// This prevents zone change detection from killing town bots
		// Clear from both new array and old array to ensure no stale data
		$TownBotData[%clientId, "SpawnBotInfo"] = "";
		$TownBotData[%clientId, "SpawnTime"] = "";
		storeData(%clientId, "SpawnBotInfo", "");
		storeData(%clientId, "SpawnTime", ""); // Also clear SpawnTime to be safe
		// Also directly clear from $ClientData to ensure it's gone (migration mode)
		$ClientData[%clientId, "SpawnBotInfo"] = "";
		$ClientData[%clientId, "SpawnTime"] = "";
		
		// CRITICAL: Set team IMMEDIATELY after setting BotInfoAiName to prevent UpdateTeam() from changing it
		// Team was already set earlier, but set it again here to ensure it's correct after all data clearing
		// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
		%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
		GameBase::setTeam(%clientId, %botTeam);
		
		// Verify team was set correctly (debug check)
		%actualTeam = GameBase::getTeam(%clientId);
		if(%actualTeam != %botTeam)
		{
			echo("WARNING: SpawnSingleZoneBot - Team mismatch for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ ", got " @ %actualTeam @ ". Re-setting...");
			GameBase::setTeam(%clientId, %botTeam);
		}
		
		// CRITICAL: Set HasLoadedAndSpawned flag to indicate bot is fully initialized
		// This allows DespawnZoneBots() to identify fully loaded bots
		storeData(%clientId, "HasLoadedAndSpawned", True);
		
		schedule("InitTownBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\");", 0.1);
		schedule("InitTownBotItemsForBot(" @ %clientId @ ", \"" @ %botName @ "\");", 1.5);
		echo("Spawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ")");
	}
}

// Helper function to extract a short zone name for bot display names
// Examples: "Yuliple City" -> "Yuliple", "Kingdom of Kronos" -> "Kronos", "Empress Sanctuary" -> "Empress"
function GetZoneShortName(%zoneDesc)
{
	if(%zoneDesc == "" || %zoneDesc == -1)
		return "";
	
	// Common patterns - use GetWord for reliable parsing:
	// "Kingdom of Kronos" -> "Kronos" (take 3rd word)
	%word0 = GetWord(%zoneDesc, 0);
	%word1 = GetWord(%zoneDesc, 1);
	if(%word0 == "Kingdom" && %word1 == "of")
	{
		%word2 = GetWord(%zoneDesc, 2);
		if(%word2 != "" && %word2 != -1)
			return %word2; // Return "Kronos"
	}
	
	// "X City" -> "X" (take first word)
	if(String::findSubStr(%zoneDesc, " City") != -1)
		return %word0;
	
	// "X Sanctuary" -> "X" (take first word)
	if(String::findSubStr(%zoneDesc, " Sanctuary") != -1)
		return %word0;
	
	// Default: return first word (usually the city/zone name)
	return %word0;
}

// ============================================================================
// DELAYED ZONE SPAWN VERIFICATION
// Prevents spawning bots when players quickly pass through a zone
// ============================================================================
$ZoneSpawnDelay = 10;      // Seconds to wait before spawning ENEMY ZONES (set to 0 to disable delay)
$TownSpawnDelay = 1;       // Seconds to wait before spawning TOWN ZONES (shorter delay for town bots)

// Called when first player enters zone - schedules spawn verification
function ScheduleZoneSpawn(%zoneIndex)
{
	if(%zoneIndex == 0 || %zoneIndex == "")
		return;
	
	// EXEMPTION: Zone 24 (Colloseum) is managed by the Seal Battle system in remortseal.cs
	// Do not use normal spawn verification - let the seal battle handle its own bots
	if(%zoneIndex == 24)
		return;
	
	// Mark that we have a pending spawn for this zone
	$ZoneSpawnPending[%zoneIndex] = getSimTime();
	
	// Determine if this is a town zone (has town bots) or enemy zone
	%isTownZone = false;
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
	{
		if($TownBotZone[%botName] == %zoneIndex)
		{
			%isTownZone = true;
			break;
		}
	}
	
	// Get appropriate delay based on zone type
	%delay = $ZoneSpawnDelay;  // Default to enemy zone delay (10s)
	if(%isTownZone)
	{
		%delay = $TownSpawnDelay;  // Town zones use shorter delay (1s)
		if(%delay == "" || %delay < 0)
			%delay = 1;
	}
	else
	{
		if(%delay == "" || %delay < 0)
			%delay = 10;  // Default to 10 seconds for enemy zones
	}
	
	if(%delay == 0)
	{
		// No delay - spawn immediately (legacy behavior)
		SpawnZoneBots(%zoneIndex);
		$ZoneSpawnPending[%zoneIndex] = "";
	}
	else
	{
		// Schedule verification after delay
		%zoneDesc = $Zone::Desc[%zoneIndex];
		%zoneType = "enemy";
		if(%isTownZone)
			%zoneType = "town";
		echo("[ZONE SPAWN] Scheduling " @ %zoneType @ " spawn verification for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") in " @ %delay @ " seconds");
		schedule("VerifyAndSpawnZoneBots(" @ %zoneIndex @ ");", %delay);
	}
}

// Called after delay - verifies players are still in zone before spawning
function VerifyAndSpawnZoneBots(%zoneIndex)
{
	if(%zoneIndex == 0 || %zoneIndex == "")
		return;
	
	%zoneDesc = $Zone::Desc[%zoneIndex];
	
	// Check if spawn was cancelled (player left before delay expired)
	if($ZoneSpawnPending[%zoneIndex] == "")
	{
		echo("[ZONE SPAWN] Spawn cancelled for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - players left before delay expired");
		return;
	}
	
	// Verify players are still in zone
	%playerCount = $ZonePlayerCount[%zoneIndex];
	if(%playerCount <= 0)
	{
		echo("[ZONE SPAWN] Aborting spawn for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - zone is now empty");
		$ZoneSpawnPending[%zoneIndex] = "";
		return;
	}
	
	// Players still in zone - proceed with spawn
	echo("[ZONE SPAWN] Verified players in zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - spawning bots now");
	$ZoneSpawnPending[%zoneIndex] = "";
	SpawnZoneBots(%zoneIndex);
}

// Cancel pending spawn when zone becomes empty
function CancelPendingZoneSpawn(%zoneIndex)
{
	if($ZoneSpawnPending[%zoneIndex] != "")
	{
		%zoneDesc = $Zone::Desc[%zoneIndex];
		echo("[ZONE SPAWN] Cancelling pending spawn for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - zone emptied");
		$ZoneSpawnPending[%zoneIndex] = "";
	}
}

// Spawn all bots for a specific zone
function SpawnZoneBots(%zoneIndex)
{
	%zoneDesc = $Zone::Desc[%zoneIndex];
	%zoneShortName = GetZoneShortName(%zoneDesc);
	
	if(%zoneIndex == 0 || %zoneIndex == "")
		return;
	
	// Clear any pending despawn for this zone (Tribes doesn't have cancel() function)
	// Set flag to empty to prevent scheduled despawn from executing
	$ZoneBotDespawnSchedule[%zoneIndex] = "";
	
	%wordCount = GetWordCount($TownBotRegistry);
	
	for(%i = 0; %i < %wordCount; %i++)
	{
		%botName = GetWord($TownBotRegistry, %i);
		
		// Skip invalid bot names (like "0" which can appear in lists)
		if(%botName == "" || %botName == "0" || %botName == -1)
			continue;
		
		if($TownBotZone[%botName] == %zoneIndex && $TownBotSpawned[%botName] == "")
		{
			// Spawn this bot
			%aiName = "TownBot_" @ %botName;
			
			// Validate aiName is not empty or "0" (shouldn't happen, but safety check)
			if(%aiName == "" || %aiName == "0" || %aiName == "TownBot_0" || %aiName == -1)
			{
				echo("ERROR: SpawnZoneBots - Invalid aiName constructed from botName='" @ %botName @ "', skipping");
				continue;
			}
			
			// Get race and validate it exists
			%botRace = $BotInfo[%botName, RACE];
			if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Bot=" @ %botName @ " | $BotInfo[RACE]='" @ %botRace @ "'");
			if(%botRace == "" || %botRace == -1)
			{
				echo("ERROR: SpawnZoneBots - Bot " @ %botName @ " has no RACE defined, defaulting to MaleHuman");
				%botRace = "MaleHuman";
			}
			
			%armor = $RaceToArmorType[%botRace];
			if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Bot=" @ %botName @ " | Race='" @ %botRace @ "' | $RaceToArmorType='" @ %armor @ "'");
			if(%armor == "" || %armor == -1)
			{
				echo("ERROR: SpawnZoneBots - Could not find armor for race '" @ %botRace @ "' for bot " @ %botName @ ", defaulting to MaleHumanArmor7");
				%armor = "MaleHumanArmor7";
			}
			
			%spawnPos = $BotInfo[%botName, SPAWN_POS];
			%spawnRot = $BotInfo[%botName, SPAWN_ROT];
			%baseDisplayName = $BotInfo[%botName, NAME];
			
			// Only prepend zone short name to merchants and bankers to make them unique
			// This avoids issues like "Empress Empress Theodora" for quest NPCs
			%displayName = %baseDisplayName;
			%isMerchant = (String::findSubStr(%botName, "merchant") == 0);
			%isBanker = (String::findSubStr(%botName, "banker") == 0);
			
			if((%isMerchant || %isBanker) && %zoneShortName != "" && %zoneShortName != -1)
			{
				%displayName = %zoneShortName @ " " @ %baseDisplayName;
			}
			
			// Check if we already have a stored ID for this bot
			%storedId = $TownBotSpawned[%botName];
			if(%storedId != "" && %storedId != -1)
			{
				// We have a stored ID, verify the bot still exists
				%playerObj = Client::getOwnedObject(%storedId);
				if(%playerObj != "" && %playerObj != -1)
				{
					// CRITICAL: Check for ghost client ID (empty name Player object)
					%storedName = Client::getName(%storedId);
					if(%storedName == "" || %storedName == -1)
					{
						// This is a ghost client ID - clean it up
						echo("WARNING: SpawnZoneBots - Found ghost client ID " @ %storedId @ " for bot " @ %botName @ " (empty name). Cleaning up...");
						%aiNameFull = "TownBot_" @ %botName;
						CleanupGhostClientId(%storedId, %aiNameFull);
						$TownBotSpawned[%botName] = "";
						// Continue to spawn a new bot
					}
					else
				{
					// Bot already exists and is valid - skip spawning
					continue;
					}
				}
				else
				{
					// Stored ID is stale (bot was deleted), clear it
					$TownBotSpawned[%botName] = "";
				}
			}
			
			// Check if an AI with this name already exists (could be an orphaned AI or enemy bot with corrupted name)
			// For town bots, they are Player objects, not Drones, so AI::getId() will always fail
			// Only check by display name for town bots
			%existingId = "";
			if(%displayName != "" && %displayName != -1)
			{
				%existingId = NEWgetClientByName(%displayName);
			}
			// Note: We don't use AI::getId() for town bots since they're Player objects, not Drones
			// AI::getId() only works for Drones and will generate "Could not find drone" errors for Player objects
			%useModifiedAiName = false;
			if(%existingId != -1 && %existingId != "")
			{
				// CRITICAL: Check for ghost client ID (empty name Player object)
				%existingName = Client::getName(%existingId);
				if(%existingName == "" || %existingName == -1)
				{
					// This is a ghost client ID - clean it up
					echo("WARNING: SpawnZoneBots - Found ghost client ID " @ %existingId @ " with empty name for bot " @ %botName @ ". Cleaning up...");
					%aiNameFull = "TownBot_" @ %botName;
					CleanupGhostClientId(%existingId, %aiNameFull);
					%existingId = "";  // Clear so we can spawn a new bot
				}
			}
			
			if(%existingId != -1 && %existingId != "")
			{
				// CRITICAL: Check if this client ID is already in use by a REAL PLAYER (not a bot)
				// If a real player is using this client ID, we cannot spawn a bot with it
				// NOTE: We check by display name patterns because isRPGAI() may not work yet (bot data not set)
				%existingPlayerName = Client::getName(%existingId);
				if(%existingPlayerName != "" && %existingPlayerName != -1)
				{
					// Check if the name matches bot patterns (enemy bots have prefixes, town bots match expected names)
					%isBotName = false;
					
					// Check enemy bot patterns
					%isBotName = HasEnemyBotNamePrefix(%existingPlayerName);
					
					// Check if it matches expected town bot display name
					%expectedDisplayName = $BotInfo[%botName, NAME];
					if(!%isBotName && %expectedDisplayName != "" && %expectedDisplayName != -1)
					{
						if(String::ICompare(%existingPlayerName, %expectedDisplayName) == 0 ||
						   String::findSubStr(%existingPlayerName, %expectedDisplayName) >= 0)
						{
							%isBotName = true;  // Matches expected town bot name
						}
					}
					
					// Also check if isRPGAI() returns true (bot data is set)
					if(!%isBotName && isRPGAI(%existingId))
					{
						%isBotName = true;  // Bot data exists, so it's a bot
					}
					
					// If it's not a bot name pattern and not a bot, it's a real player
					if(!%isBotName)
					{
						// This is a REAL PLAYER using this client ID - cannot spawn bot here
						echo("WARNING: SpawnZoneBots - Client ID " @ %existingId @ " is already in use by real player '" @ %existingPlayerName @ "'. Cannot spawn bot " @ %botName @ ". Skipping...");
						continue;  // Skip spawning this bot
					}
				}
				
				// An AI with this name already exists - check if it's actually a town bot
				// CRITICAL: Check $TownBotData and $TownBotSpawned FIRST to avoid stale data from $ClientData
				%existingBotInfoAiNameTown = $TownBotData[%existingId, "BotInfoAiName"];
				%existingSpawnBotInfoTown = $TownBotData[%existingId, "SpawnBotInfo"];
				
				// Check if this client ID is registered as a town bot
				%isRegisteredTownBot = false;
				%registeredTownBotName = "";
				for(%regIndex = 0; %regIndex < $TownBotRegistryCount; %regIndex++)
				{
					%regBotName = $TownBotRegistry[%regIndex];
					if(%regBotName != "" && %regBotName != "0" && %regBotName != -1)
					{
						if($TownBotSpawned[%regBotName] == %existingId)
						{
							%isRegisteredTownBot = true;
							%registeredTownBotName = %regBotName;
							break;
						}
					}
				}
				
				// If it's a registered town bot with the correct name, skip spawning
				if(%isRegisteredTownBot && %registeredTownBotName == %botName)
				{
					// Bot already exists and is valid - update stored ID and skip spawning
					$TownBotSpawned[%botName] = %existingId;
					continue;
				}
				
				// If it's a registered town bot with a different name, this is a conflict (same display name, different bot)
				// This shouldn't happen, but handle it gracefully
				if(%isRegisteredTownBot && %registeredTownBotName != %botName)
				{
					echo("WARNING: SpawnZoneBots - Town bot " @ %botName @ " has same display name as existing town bot " @ %registeredTownBotName @ " (clientId=" @ %existingId @ "). This is a naming conflict.");
					// Continue to spawn - AI::spawn() will handle the conflict
				}
				
				// Check if it's an enemy bot by checking $EnemyBotData directly (avoid stale $ClientData)
				%existingSpawnBotInfoEnemy = $EnemyBotData[%existingId, "SpawnBotInfo"];
				%existingBotInfoAiNameEnemy = $EnemyBotData[%existingId, "BotInfoAiName"];
				
				// Also check $ClientData as fallback (but only if not found in new arrays)
				if(%existingSpawnBotInfoEnemy == "" || %existingSpawnBotInfoEnemy == "0" || %existingSpawnBotInfoEnemy == -1)
				{
					%existingSpawnBotInfo = $ClientData[%existingId, "SpawnBotInfo"];
					%existingBotInfoAiName = $ClientData[%existingId, "BotInfoAiName"];
				}
				else
				{
					%existingSpawnBotInfo = %existingSpawnBotInfoEnemy;
					%existingBotInfoAiName = %existingBotInfoAiNameEnemy;
				}
				
				// If it's an enemy bot (has SpawnBotInfo but wrong BotInfoAiName)
				// Since town bots and enemy bots spawn in different zones, this should only happen if the enemy bot is dead/stale
				if(%existingSpawnBotInfo != "" && %existingSpawnBotInfo != "0" && %existingSpawnBotInfo != -1 && %existingBotInfoAiName != %botName)
				{
					// Verify the enemy bot is actually still alive
					%enemyBotPlayerObj = Client::getOwnedObject(%existingId);
					%enemyBotIsAlive = (%enemyBotPlayerObj != "" && %enemyBotPlayerObj != -1);
					
					// If enemy bot is still alive, this is unexpected - retry spawning to get a different client ID
					// (Town bots and enemy bots should never be in the same zones)
					if(%enemyBotIsAlive)
					{
						%retryCount = $TownBotSpawnRetry[%botName];
						if(%retryCount == "")
							%retryCount = 0;
						
						if(%retryCount < 3)
						{
							echo("WARNING: SpawnZoneBots - Cannot spawn town bot " @ %botName @ ". Active enemy bot (clientId=" @ %existingId @ ") with same display name found. Retrying spawn (attempt " @ (%retryCount + 1) @ "/3).");
							$TownBotSpawnRetry[%botName] = %retryCount + 1;
							// Retry spawning ONLY this specific bot after a delay (not the entire zone)
							schedule("SpawnSingleZoneBot(\"" @ %botName @ "\", " @ %zoneIndex @ ");", 0.5);
							continue;
						}
						else
						{
							// Max retries reached - give up
							echo("ERROR: SpawnZoneBots - Failed to spawn town bot " @ %botName @ " after 3 retries. Active enemy bot (clientId=" @ %existingId @ ") still has same display name.");
							$TownBotSpawnRetry[%botName] = "";
							continue;
						}
					}
					
					// Enemy bot is dead/stale - allow spawn, stale data will be cleared after spawn
					echo("INFO: SpawnZoneBots - Enemy bot (clientId=" @ %existingId @ ") with same display name is dead/stale. Allowing town bot spawn - stale data will be cleared.");
					// Continue to spawn - stale data will be cleared after spawn
				}
				
				// Otherwise, it's an orphaned AI or unknown state - AI::spawn() will clean it up
			}
			
			// No stored ID means this is a fresh spawn - just proceed to spawn
			// AI::spawn() will handle any orphaned AIs with this name
			
			if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: CALLING AI::spawn() for " @ %botName @ " with armor='" @ %armor @ "'");
			
			// CRITICAL INTEGRATION: Check server capacity before spawning
			%predictedId = PlayerManager::getFreeId();
			if(%predictedId == -1)
			{
				echo("CRITICAL: SpawnZoneBots - Server is FULL! Aborting spawn for " @ %botName);
				// Clean up stored pending state?
				// Just continue, maybe next slot frees up?
				continue;
			}
			
			if(AI::spawn(%aiName, %armor, %spawnPos, %spawnRot, %displayName, "male2") != "false")
			{
				if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: AI::spawn() SUCCEEDED for " @ %botName);
				// DEBUG: Commented out to reduce server lag
	//echo("[TOWN BOT DEBUG] AI::spawn() returned SUCCESS for " @ %botName);
				// CRITICAL: Try to get client ID immediately to set team before UpdateTeam() runs
				// UpdateTeam() may be called by Game::playerSpawned() before SpawnZoneBotPostSpawn() runs
				// If we can get the client ID now, set team immediately to prevent UpdateTeam() from setting it to team 1
				
				// CRITICAL INTEGRATION: Check predicted ID first
				%immediateClientId = "";
				if(%predictedId != "" && %predictedId != -1 && Client::getName(%predictedId) == %displayName)
				{
					%immediateClientId = %predictedId;
					if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Prediction SUCCESS for " @ %botName @ " (ID: " @ %immediateClientId @ ")");
				}
				else
				{
					%immediateClientId = NEWgetClientByName(%displayName);
				}
				if(%immediateClientId != -1 && %immediateClientId != "")
			{
					// DEBUG: Check what armor the bot actually got
					%actualArmor = Player::getArmor(%immediateClientId);
					%actualTeam = GameBase::getTeam(%immediateClientId);
					%playerObj = Client::getOwnedObject(%immediateClientId);
					%dataName = "";
					if(%playerObj != -1 && %playerObj != "")
					{
						%dataName = GameBase::getDataName(%playerObj);
						// CRITICAL FIX: Mark object as town bot explicitly to prevent UpdateAppearance from messing with it
						%playerObj.isTownBot = true;
					}
					
					if($TOWNBOT_RACE_DEBUG)
					{
						echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Bot=" @ %botName @ " spawned | clientId=" @ %immediateClientId @ " | playerObj=" @ %playerObj);
						echo("[TOWNBOT RACE DEBUG]   Expected armor='" @ %armor @ "' | Player::getArmor='" @ %actualArmor @ "' | GameBase::getDataName='" @ %dataName @ "' | Team=" @ %actualTeam);
					}
					
					// CRITICAL DEBUG: Check for orphaned objects with this client ID in MissionCleanup
					if(isObject("MissionCleanup"))
					{
						%group = nameToID("MissionCleanup");
						%count = Group::objectCount(%group);
						%orphanCount = 0;
						for(%j = %count - 1; %j >= 0; %j--)
						{
							%obj = Group::getObject(%group, %j);
							if(!isObject(%obj)) continue;
							if(getObjectType(%obj) != "Player") continue;
							
							%objClientId = Player::getClient(%obj);
							if(%objClientId == %immediateClientId && %obj != %playerObj)
							{
								%orphanData = GameBase::getDataName(%obj);
								echo("[TOWNBOT RACE DEBUG]   WARNING: Found orphaned object " @ %obj @ " (data=" @ %orphanData @ ") also claiming clientId=" @ %immediateClientId @ "!");
								%orphanCount++;
								// Delete the orphan immediately
								echo("[TOWNBOT RACE DEBUG]   Deleting orphan " @ %obj);
								deleteObject(%obj);
							}
						}
						if(%orphanCount > 0)
							if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG]   Cleaned " @ %orphanCount @ " orphaned objects for clientId=" @ %immediateClientId);
					}
					
					// Got client ID immediately - set BotInfoAiName and team right away
				%aiNameFull = "TownBot_" @ %botName;
					storeData(%immediateClientId, "BotInfoAiName", %aiNameFull);
					$TownBotData[%immediateClientId, "BotInfoAiName"] = %aiNameFull;
					// Set team to 0 immediately to prevent UpdateTeam() from setting it to team 1
					GameBase::setTeam(%immediateClientId, 0);
					
					// CRITICAL FIX: Clear ExpDistributed flag for new town bots
					// This prevents the bot from inheriting "EXP already distributed" status from a previous bot/client
					storeData(%immediateClientId, "ExpDistributed", "");
					
					// CRITICAL FIX: Always refresh armor visual after setting team
					// When team was -1 (engine default), the visual skin is wrong even if armor model is correct
					// We must re-apply armor after team change to fix the visual appearance
					if(%armor != "" && %armor != -1)
					{
						if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Refreshing armor visual for " @ %botName @ " after team set (armor='" @ %armor @ "')");
						Player::setArmor(%playerObj, %armor);
						Client::setSkin(%immediateClientId, $Server::teamSkin[0]);
						
						// FINAL WORD: Schedule one more skin enforcement at T+2.0s
						// This runs after ALL other scheduled scripts (especially any ~1.0s culprits)
						schedule("ForceTownBotSkin(" @ %immediateClientId @ ", \"" @ %botName @ "\");", 2.0);
					}
				}
				
				// CRITICAL: Add 1 second delay before looking up client ID to allow Player object to register
				// This prevents "Could not get client ID" warnings on fresh server restarts
				// Schedule the client ID lookup and initialization with a delay
				schedule("SpawnZoneBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 1.0);
				continue; // Continue to next bot - this one will be initialized in scheduled call
			}
		}
	}
}

// Despawn all bots for a specific zone
function DespawnZoneBots(%zoneIndex)
{
	// CRITICAL: Reject invalid zone indices (0, empty, or -1)
	// Zone -1 is "Unknown" zone and should never trigger despawn
	if(%zoneIndex == 0 || %zoneIndex == "" || %zoneIndex == -1)
		return;
	
	// EXEMPTION: Zone 23 (Colosseum) is managed by the Seal Battle system in remortseal.cs
	// Do not despawn bots here - the seal battle handles its own bot lifecycle
	if(%zoneIndex == 23)
		return;
	
	// CRITICAL: Verify zone is actually empty before despawning (double-check against $ZonePlayerCount)
	// This prevents despawning bots when players are still in the zone (if $ZonePlayerCount got out of sync)
	%zoneFolderID = $Zone::FolderID[%zoneIndex];
	if(%zoneFolderID == "" || %zoneFolderID == -1)
		return; // Invalid zone index
	%playerList = Zone::getPlayerList(%zoneFolderID, 2); // Type 2 = real players only (not bots)
	%hasPlayers = (%playerList != "" && %playerList != -1);
	
	if(%hasPlayers)
	{
		// Zone has players - don't despawn, but fix the count
		%actualPlayerCount = GetWordCount(%playerList);
		$ZonePlayerCount[%zoneIndex] = %actualPlayerCount;
		echo("WARNING: DespawnZoneBots - Zone " @ %zoneIndex @ " has " @ %actualPlayerCount @ " player(s) but was scheduled for despawn. Fixed player count.");
		return;
	}
	
	// Clear the despawn schedule flag
	$ZoneBotDespawnSchedule[%zoneIndex] = "";
	
	// Get zone description to match bots in this zone
	%zoneDesc = $Zone::Desc[%zoneIndex];
	if(%zoneDesc == "")
		return;
	
	// First, collect all town bot clientIds to remove from TownBotList (optimization: rebuild list once at end)
	%clientIdsToRemove = "";
	%botCount = 0;
	
	// First, despawn all town bots in this zone
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
	{
		// Skip invalid bot names (like "0" which can appear in lists)
		if(%botName == "" || %botName == "0" || %botName == -1)
			continue;
		
		if($TownBotZone[%botName] == %zoneIndex && $TownBotSpawned[%botName] != "")
		{
			%clientId = $TownBotSpawned[%botName];
			%aiName = "TownBot_" @ %botName;
			
			// UNIFIED SAFEGUARD: Verify this is a bot (handles Real Players vs Ghost Bots)
			if(!IsSafeToModify(%clientId, "DespawnZoneBots (Town)"))
				continue;
			
			// ADDITIONAL SAFEGUARD: Only despawn bots that have fully loaded and spawned
			// This prevents despawning bots that are still initializing or in an invalid state
			if(!fetchData(%clientId, "HasLoadedAndSpawned"))
			{
				echo("WARNING: DespawnZoneBots - Bot " @ %botName @ " (clientId " @ %clientId @ ") has not loaded and spawned yet. Skipping despawn to prevent data corruption.");
				continue;
			}
			
			// Get the player object (same as clientId for AI bots)
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
			{
				// CRITICAL: Clear all bot identification data BEFORE deletion to prevent conflicts
				// This prevents old bots from being detected when searching for new bots with same display name
				storeData(%clientId, "BotInfoAiName", "");
				$TownBotData[%clientId, "BotInfoAiName"] = "";
				$ClientData[%clientId, "BotInfoAiName"] = "";
				$BotInfoAiName[%clientId] = "";  // Clear direct array for fast lookup
				
				// CRITICAL: Clear all other town bot data from $TownBotData array to prevent stale data
				// This ensures the next bot using this clientId doesn't see the old bot's data
				$TownBotData[%clientId, "SpawnBotInfo"] = "";
				$TownBotData[%clientId, "SpawnTime"] = "";
				$TownBotData[%clientId, "QuestItems"] = "";
				$TownBotData[%clientId, "KeyItems"] = "";
				$TownBotData[%clientId, "Consumables"] = "";
				$TownBotData[%clientId, "Armor"] = "";
				$TownBotData[%clientId, "Accessories"] = "";
				$TownBotData[%clientId, "Other"] = "";
				
				// CRITICAL: Also clear $EnemyBotData in case this clientId was previously used by an enemy bot
				// This prevents isRPGAI() from returning true due to stale enemy bot data
				$EnemyBotData[%clientId, "BotInfoAiName"] = "";
				$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
				
				// CRITICAL: Clear conversation state for all players who were talking to this bot
				// This ensures conversations restart from the beginning when the bot respawns
				for(%playerCl = Client::getFirst(); %playerCl != -1; %playerCl = Client::getNext(%playerCl))
				{
					$state[%clientId, %playerCl] = "";
					// Also clear LastPlayerInteraction tracking
					$LastPlayerInteraction[%clientId, %playerCl] = "";
				}
				
				// Clean up bot data (same as #deletebot command)
				storeData(%clientId, "noDropLootbagFlag", True);
				ClearEvents(%clientId);
				
				// CRITICAL: Clear zone data before deleting to prevent zone detection issues
				storeData(%clientId, "zone", "");
				storeData(%clientId, "tmpzone", "");
				
				// CRITICAL: Clear all storeData fields set during InitTownBotPostSpawn
				storeData(%clientId, "RACE", "");
				storeData(%clientId, "NoDropLoot", "");
				storeData(%clientId, "MountWeaponOnSpawn", "");
				storeData(%clientId, "MountWeaponOnTalk", "");
				storeData(%clientId, "ShowIdleMessage", "");
				storeData(%clientId, "LastInteractionTime", "");
				storeData(%clientId, "dumbAIflag", "");
				storeData(%clientId, "botTeam", "");
				storeData(%clientId, "AITarget", "");
				storeData(%clientId, "AILastDestination", "");
				storeData(%clientId, "AILastLoggedDist", "");
				storeData(%clientId, "AIMovementLoopRunning", "");
				
				// CRITICAL: Clear all directive table entries for this bot (by client ID)
				// Clear common directives (0-99) to prevent stale directive data
				for(%d = 0; %d <= 99; %d++)
				{
					$aidirectiveTable[%clientId, %d] = "";
				}
				
				// CRITICAL: Clear belt cached lists to prevent memory leaks
				$Belt::CachedList[%clientId, "QuestItems"] = "";
				$Belt::CachedList[%clientId, "KeyItems"] = "";
				$Belt::CachedList[%clientId, "Consumables"] = "";
				$Belt::CachedList[%clientId, "Armor"] = "";
				$Belt::CachedList[%clientId, "Accessories"] = "";
				$Belt::CachedList[%clientId, "Other"] = "";
				
				// Clean up bot group if applicable
				%b = AI::IsInWhichBotGroup(%clientId);
				if(%b != -1)
					AI::RemoveBotFromBotGroup(%clientId, %b);
				
				// Clear transient flags so the next occupant of this clientId starts clean
				storeData(%clientId, "HasLoadedAndSpawned", "");
				storeData(%clientId, "SpawnInvuln", "");
				%dispNameClear = Client::getName(%clientId);
				if(%dispNameClear != "" && %dispNameClear != -1)
					$SpawnInvulnByName[%dispNameClear] = "";
				storeData(%clientId, "ShovedByPlayer", "");
				$BotFrozen[%clientId] = "";
				storeData(%clientId, "AITarget", "");
				storeData(%clientId, "BotAttackLoopActive", "");
				
				// CRITICAL: Free AI number from $aiNumTable so it can be recycled
				// This ensures bot numbers restart from 0-20 instead of going to 100+
				// Get the number BEFORE clearing $tmpbotn to ensure we can free it
				%townBotNumber = $tmpbotn[%aiName];
				if(%townBotNumber != "" && %townBotNumber != -1 && %townBotNumber != "0")
				{
					$aiNumTable[%townBotNumber] = "";
					$tmpbotn[%aiName] = "";
					//echo("[SPAWN DEBUG] DespawnZoneBots(): Freed AI number " @ %townBotNumber @ " for town bot " @ %aiName @ " - number can now be recycled");
				}
				
			// CRITICAL: Mark this client ID as recently freed to prevent immediate reuse
			// This prevents new bots from getting the same client ID before cleanup completes
			$ClientIdRecentlyFreed[%clientId] = getSimTime();
			
			// CRITICAL: Clear retry counters (if any were set during spawn failures)
			$TownBotRetryGetAIIdCount[%botName] = "";
			$TownBotSpawnRetry[%botName] = "";
			
			// Collect clientId for removal (optimization: rebuild list once at end)
				%clientIdsToRemove = %clientIdsToRemove @ %clientId @ " ";
				%botCount++;
				
				// CRITICAL: Register bot in graveyard BEFORE deletion to prevent client ID reuse collision
				// Variables %aiName and %clientId are already defined in this scope
				AddToGraveyard(%aiName, %clientId);
				
				// Delete the player object directly (same approach as #deletebot)
				// CRITICAL: Add 1 second delay to deletion to help code load properly and functions/variables be called more effectively
				// CRITICAL: Use validation token to prevent scheduled deletion from affecting real players who get the same clientId
				%validationToken = %botName @ "_" @ getSimTime();
				$DespawnValidationToken[%clientId] = %validationToken;
				// Store player name for validation check in scheduled deletion
				%despawnPlayerName = Client::getName(%clientId);
				if(%despawnPlayerName == "" || %despawnPlayerName == -1)
					%despawnPlayerName = "Unknown";
				schedule("if($DespawnValidationToken[" @ %clientId @ "] == \"" @ %validationToken @ "\" && !isFile(\"temp\\\\" @ %despawnPlayerName @ ".cs\")) { if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ "); $DespawnValidationToken[" @ %clientId @ "] = \"\"; }", 1.0);
				echo("Despawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ")");
			}
			else
			{
				// Bot already deleted, but clear identification data anyway to prevent conflicts
				storeData(%clientId, "BotInfoAiName", "");
				$TownBotData[%clientId, "BotInfoAiName"] = "";
				$ClientData[%clientId, "BotInfoAiName"] = "";
				$BotInfoAiName[%clientId] = "";  // Clear direct array for fast lookup
				
				// CRITICAL: Free AI number from $aiNumTable even if bot is already deleted
				// This ensures bot numbers are recycled properly
				%townBotNumber = $tmpbotn[%aiName];
				if(%townBotNumber != "" && %townBotNumber != -1 && %townBotNumber != "0")
				{
					$aiNumTable[%townBotNumber] = "";
					$tmpbotn[%aiName] = "";
					//echo("[SPAWN DEBUG] DespawnZoneBots(): Freed AI number " @ %townBotNumber @ " for already-deleted town bot " @ %aiName @ " - number can now be recycled");
				}
				
				// Bot already deleted, still collect for list removal
				%clientIdsToRemove = %clientIdsToRemove @ %clientId @ " ";
				echo("Bot " @ %botName @ " already deleted, will remove from list");
			}
			
			// Clear spawn tracking
			$TownBotSpawned[%botName] = "";
			// Clear lingering no-drop flag so next occupant of this clientId can drop normally
			storeData(%clientId, "noDropLootbagFlag", "");
			$TownBotData[%clientId, "noDropLootbagFlag"] = "";
			$ClientData[%clientId, "noDropLootbagFlag"] = "";
			// Also clear invuln/name guards for reuse safety
			storeData(%clientId, "HasLoadedAndSpawned", "");
			storeData(%clientId, "SpawnInvuln", "");
			%dispNameClear2 = Client::getName(%clientId);
			if(%dispNameClear2 != "" && %dispNameClear2 != -1)
				$SpawnInvulnByName[%dispNameClear2] = "";
		}
	}
	
	// OPTIMIZATION: Rebuild TownBotList once for all removed bots instead of once per bot
	if(%botCount > 0)
	{
		%newList = "";
		for(%j = 0; (%id = GetWord($TownBotList, %j)) != -1; %j++)
		{
			// Check if this ID should be removed
			%shouldRemove = false;
			for(%k = 0; (%removeId = GetWord(%clientIdsToRemove, %k)) != -1; %k++)
			{
				if(%id == %removeId)
				{
					%shouldRemove = true;
					break;
				}
			}
			
			if(!%shouldRemove)
				%newList = %newList @ %id @ " ";
		}
		$TownBotList = %newList;
	}
	
	// CRITICAL: Also despawn enemy bots spawned from spawn points in this zone
	// This handles the case where multiple bots were incorrectly spawned per spawn point
	// (due to race conditions). We need to kill them and properly decrement spawn counters.
	// IMPORTANT: We must be very careful to only kill bots that are actually in this zone
	// to prevent killing enemy bots in other zones.
	// CRITICAL FIX: Client::getFirst()/getNext() only returns REAL player clients, NOT AI bots!
	// Use $BotRegistryList to iterate enemy bots (bots are not in client connection list)
	// 
	// CRITICAL FIX: Copy the list BEFORE iterating!
	// Player::Kill() -> UnregisterBot() modifies $BotRegistryList, removing the killed bot.
	// This shifts all subsequent entries down, causing the next bot to be skipped.
	// By copying the list first, we iterate a stable snapshot while the original is modified.
	%botListSnapshot = $BotRegistryList;
	for(%i = 0; (%botId = GetWord(%botListSnapshot, %i)) != -1; %i++)
	{
		// Validate bot ID is valid
		if(%botId == "" || %botId == "0")
			continue;
		
		// CRITICAL: Skip bots that were already processed by the first loop (town bots)
		// The first loop clears their data but schedules deletion for 1 second later
		// So GetBotIdList() might still return them before they're actually deleted
		%alreadyProcessed = false;
		for(%chk = 0; (%chkId = GetWord(%clientIdsToRemove, %chk)) != -1; %chk++)
		{
			if(%botId == %chkId)
			{
				%alreadyProcessed = true;
				break;
			}
		}
		if(%alreadyProcessed)
			continue;
		
		// Validate bot still exists
		%playerObj = Client::getOwnedObject(%botId);
		if(%playerObj == "" || %playerObj == -1)
			continue;
		
		// DEBUG: Trace enemy bot candidates
		echo("[DESPAWN DEBUG] Checking bot candidate: " @ %botId @ " (" @ Client::getName(%botId) @ ")");
		
		// UNIFIED SAFEGUARD: Verify this is a bot (handles Real Players vs Ghost Bots)
		if(!IsSafeToModify(%botId, "DespawnZoneBots (Enemy)"))
			continue;
		
		// CRITICAL: Determine bot type BEFORE checking HasLoadedAndSpawned so we can show accurate warning messages
		// Town bots have BotInfoAiName but NO SpawnBotInfo
		// Enemy bots have BotInfoAiName AND SpawnBotInfo
		%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
		%botInfoAiName = fetchData(%botId, "BotInfoAiName");
		%isTownBot = (%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0" && (%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0"));
		
		// Skip town bots (they're already handled above)
		if(%isTownBot)
		{
			// This is a town bot (has BotInfoAiName but no SpawnBotInfo) - skip, already handled above
			continue;
		}
		
		// ADDITIONAL SAFEGUARD: Only despawn bots that have fully loaded and spawned
		// This prevents despawning bots that are still initializing or in an invalid state
		if(!fetchData(%botId, "HasLoadedAndSpawned"))
		{
			// Now we know it's an enemy bot (not a town bot), so the message is accurate
			echo("WARNING: DespawnZoneBots - Enemy bot clientId " @ %botId @ " has not loaded and spawned yet. Skipping to prevent data corruption.");
			continue;
		}
		
		// CRITICAL: Only process enemy bots (must have SpawnBotInfo)
		if(%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0")
		{
			// Not an enemy bot - skip
			continue;
		}
		
		// CRITICAL: Verify bot is actually in the target zone using zone index, not just description
		// This prevents killing bots in other zones if zone descriptions match incorrectly
		%botZone = fetchData(%botId, "zone");
		
		// Fallback: If "zone" is 0 or empty (often overwritten by Zone::Update logic), check "tmpzone"
		if(%botZone == "" || %botZone == 0 || %botZone == -1)
		{
			%botZone = fetchData(%botId, "tmpzone");
			// If still empty, try direct array access for tmpzone as last resort
			if(%botZone == "" || %botZone == 0 || %botZone == -1)
			{
				%botZone = $EnemyBotData[%botId, "tmpzone"];
			}
		}



		
		if(%botZone == "" || %botZone == -1)
			continue; // Bot has no zone data, skip to be safe
		
		// Get target zone folder ID from the passed zone index (no loop needed - O(1) lookup)
		%targetZoneFolder = $Zone::FolderID[%zoneIndex];
		if(%targetZoneFolder == "" || %targetZoneFolder == -1)
		{
			echo("[DESPAWN DEBUG] Invalid targetZoneFolder for index " @ %zoneIndex);
			continue; // Invalid zone index, skip
		}
		
		echo("[DESPAWN DEBUG] Processing bot " @ %botId @ " for target zone INDEX " @ %zoneIndex @ " (folderID " @ %targetZoneFolder @ ")");
		
		// Compare bot's zone INDEX directly with target zone INDEX
		// CRITICAL FIX: %botZone is a zone INDEX (13, 17, etc.) and must be compared to %zoneIndex (also an index)
		// NOT to %targetZoneFolder (which is an object ID like 8568)
		%match = false;
		%originZone = "";
		%matchedViaOrigin = false;
		if(%botZone == %zoneIndex)
		{
			%match = true;
			echo("[DESPAWN DEBUG] MATCH! Bot " @ %botId @ " zone=" @ %botZone @ " matches target zoneIndex=" @ %zoneIndex);
		}
		else
		{
			// Mismatch on primary zone - check SpawnOriginZoneID (origin folder ID) against targetZoneFolder (folder ID)
			// This handles cases where bot was spawned from a specific folder
			%originZone = fetchData(%botId, "SpawnOriginZoneID");
			if(%originZone == "" || %originZone == 0 || %originZone == -1)
				%originZone = $EnemyBotData[%botId, "SpawnOriginZoneID"];
				
			if(%originZone == %targetZoneFolder)
			{
				// Match found via SpawnOriginZoneID (comparing folder IDs)!
				echo("[DESPAWN DEBUG] MATCH! Bot " @ %botId @ " matched via SpawnOriginZoneID=" @ %originZone @ " == targetZoneFolder=" @ %targetZoneFolder);
				%match = true;
				%matchedViaOrigin = true;
			}
		}

		if(!%match)
		{
			echo("[DESPAWN DEBUG] Zone Mismatch for " @ %botId @ ": botZone(index)=" @ %botZone @ " != targetZoneIndex=" @ %zoneIndex @ ", originZone(folderID)=" @ %originZone @ " != targetZoneFolder=" @ %targetZoneFolder @ ". Skipping.");
			continue; // Bot is in a different zone, skip to prevent killing bots in other zones
		}
		
		// NOTE: Zone description check removed - it was blocking legitimate despawns
		// Zone::getDesc(%botZone) returns -1 for zone indexes, causing false mismatches
		// The zone INDEX match above is sufficient for safe despawning
		
		// CRITICAL SAFEGUARD: Must have SpawnBotInfo to be considered an enemy bot
		%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
		if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
		{
			// No SpawnBotInfo - this is not an enemy bot, skip to be safe
			echo("WARNING: DespawnZoneBots - Skipping client " @ %botId @ " (" @ Client::getName(%botId) @ ") - no SpawnBotInfo (not an enemy bot)");
			continue;
		}
		
		// Check if this bot came from a spawn point
		if(GetWord(%spawnBotInfo, 0) == "SpawnPoint")
		{
			%spawnPointId = GetWord(%spawnBotInfo, 1);
			
			// CRITICAL: Clear enemy bot data arrays BEFORE killing
			// Player::onKilled() clears storeData but not these arrays
			// AI::onDroneKilled() may not be called for Player objects
			%botInfoAiName = fetchData(%botId, "BotInfoAiName");
			if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
			{
				// Fallback: try arrays if storeData is already cleared
				%botInfoAiName = $EnemyBotData[%botId, "BotInfoAiName"];
				if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
					%botInfoAiName = $ClientData[%botId, "BotInfoAiName"];
			}
			
			// Clear spawn scheduled flag to prevent respawn blocking
			if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			{
				$SpawnAIScheduled[%botInfoAiName] = "";
			}
			
			// Clear enemy bot data arrays (Player::onKilled() doesn't clear these)
			// CRITICAL: Do NOT clear BotInfoAiName or SpawnBotInfo here - AI::onDroneKilled() needs them
			// AI::onDroneKilled() is called by AI::delete() which is scheduled in Player::onKilled()
			// BotInfoAiName and SpawnBotInfo will be cleared by AI::onDroneKilled() or ClearVariables() after cleanup completes
			// $EnemyBotData[%botId, "SpawnBotInfo"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled() to identify enemy bots
			$EnemyBotData[%botId, "SpawnTime"] = "";
			// $EnemyBotData[%botId, "BotInfoAiName"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled()
			$EnemyBotData[%botId, "zone"] = "";
			$EnemyBotData[%botId, "tmpzone"] = "";
			$EnemyBotData[%botId, "SpawnOriginZoneID"] = "";
			$EnemyBotData[%botId, "RemortStep"] = "";
			// $ClientData[%botId, "SpawnBotInfo"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled() to identify enemy bots
			$ClientData[%botId, "SpawnTime"] = "";
			// $ClientData[%botId, "BotInfoAiName"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled()
			// $BotInfoAiName[%botId] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled()
			
			// CRITICAL: Also clear via storeData() to ensure all storage locations are cleaned
			// But keep BotInfoAiName and SpawnBotInfo in storeData() too - AI::onDroneKilled() uses fetchData() to find them
			storeData(%botId, "SpawnOriginZoneID", "");
			
			// Mark client ID as recently freed (Player::onKilled() will also do this, but doing it here ensures it's set)
			$ClientIdRecentlyFreed[%botId] = getSimTime();
			
			// Kill the bot - this will trigger Player::onKilled() which handles:
			// - Spawn counter decrement
			// - Bot tracking counter decrement
			// - AI number freeing
			// - storeData cleanup
			// - Directive table cleanup
			// - Belt cached list cleanup
			// - Pet/bot group cleanup
			storeData(%botId, "noDropLootbagFlag", True);
			// CRITICAL: Set noExperienceFlag to prevent EXP distribution when despawning
			// This prevents players from getting EXP for enemies they attacked but didn't kill
			// when those enemies despawn due to the zone becoming empty
			storeData(%botId, "noExperienceFlag", True);
			Player::Kill(%botId);
			echo("Despawned enemy bot from spawn point " @ %spawnPointId @ " (zone " @ %zoneIndex @ ", zoneDesc=" @ %zoneDesc @ ")");
		}
	}
}

// Schedule bot despawn for a zone (30 seconds after all players leave - delay prevents crash from too many operations)
function ScheduleZoneBotDespawn(%zoneIndex)
{
	if(%zoneIndex == 0 || %zoneIndex == "")
		return;
	
	// Schedule despawn after a delay (prevents crash from too many operations happening at once during zone changes)
	schedule("DespawnZoneBots(" @ %zoneIndex @ ");", 30);
}

// Check if despawn is still needed before executing (prevents duplicate despawns)
function CheckAndDespawnZoneBots(%zoneIndex)
{
	// Only despawn if the flag is still set to "pending" (not cleared by player re-entry)
	// AND verify that the zone is actually empty (player count <= 0)
	if($ZoneBotDespawnSchedule[%zoneIndex] == "pending")
	{
		%playerCount = $ZonePlayerCount[%zoneIndex];
		if(%playerCount == "")
			%playerCount = 0;
		
		// Only despawn if zone is actually empty
		if(%playerCount <= 0)
		{
			DespawnZoneBots(%zoneIndex);
		}
		else
		{
			// Player entered zone before despawn executed - clear the flag
			$ZoneBotDespawnSchedule[%zoneIndex] = "";
		}
	}
}

// Periodic check to despawn town bots in zones that are actually empty
// This catches cases where $ZonePlayerCount got out of sync
function PeriodicEmptyZoneCheck()
{
	// Check all zones (not just zones with town bots)
	for(%zoneIndex = 1; %zoneIndex <= $Zone::Count; %zoneIndex++)
	{
		%zoneFolderID = $Zone::FolderID[%zoneIndex];
		if(%zoneFolderID == "" || %zoneFolderID == -1)
			continue; // Invalid zone index, skip
		
		%playerList = Zone::getPlayerList(%zoneFolderID, 2); // Type 2 = real players only (not bots)
		%hasPlayers = (%playerList != "" && %playerList != -1);
		
		if(!%hasPlayers)
		{
			// Zone is empty - despawn bots and fix player count
			$ZonePlayerCount[%zoneIndex] = 0;
			DespawnZoneBots(%zoneIndex);
		}
		else
		{
			// Zone has players - fix the count if it's wrong
			%actualPlayerCount = GetWordCount(%playerList);
			%storedCount = $ZonePlayerCount[%zoneIndex];
			if(%storedCount == "")
				%storedCount = 0;
			
			if(%actualPlayerCount != %storedCount)
			{
				$ZonePlayerCount[%zoneIndex] = %actualPlayerCount;
				echo("WARNING: PeriodicEmptyZoneCheck - Zone " @ %zoneIndex @ " player count mismatch. Fixed: " @ %storedCount @ " -> " @ %actualPlayerCount);
			}
		}
	}
	
	// Schedule next check in 30 seconds
	schedule("PeriodicEmptyZoneCheck();", 30);
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
			
			if(%storedBotTeam != "" && %storedBotTeam != -1 && %storedBotTeam != "0" && %storedBotTeam != 0)
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
	
	// Schedule next check in 15 seconds (increased frequency for faster team fix detection)
	schedule("PeriodicBotTeamCheck();", 15);
}

// Post-spawn initialization for town bots (Player objects)
// This is called via schedule to ensure bot is fully initialized before setup
function InitTownBotPostSpawn(%aiName, %name)
{
	// Skip invalid bot names (like "0" which can appear in lists)
	if(%aiName == "" || %aiName == "0" || %aiName == -1 || %name == "" || %name == "0" || %name == -1)
		return;
	
	// For town bots, we already stored the client ID in $TownBotSpawned during spawn
	// Use that instead of AI::getId() which only works for Drones
	%clientId = $TownBotSpawned[%name];
	
	// If not found in stored list, try to get it from display name (town bots are Player objects)
	if(%clientId == -1 || %clientId == "")
	{
		%displayName = $BotInfo[%name, NAME];
		if(%displayName != "" && %displayName != -1)
		{
			%clientId = NEWgetClientByName(%displayName);
		}
	}
	
	// Note: We don't use AI::getId() for town bots since they're Player objects, not Drones
	// AI::getId() only works for Drones and will generate "Could not find drone" errors for Player objects
	
	// Validate that we got a valid ID
	if(%clientId == -1 || %clientId == "")
	{
		echo("ERROR: InitTownBotPostSpawn - Could not get client ID for " @ %name @ " (aiName: " @ %aiName @ "). Bot may be broken.");
		return;
	}
	
	// Get player object
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: InitTownBotPostSpawn - could not find player object for clientId " @ %clientId);
		return;
	}
	
	// Set bot name property (used by shop system and other systems)
	%playerObj.name = %name;
	
	// Validate and set race/armor to ensure correct model (fixes issue where bots spawn with wrong race after enemy bots are killed)
	%botRace = $BotInfo[%name, RACE];
	if(%botRace == "" || %botRace == -1)
	{
		echo("WARNING: InitTownBotPostSpawn - Bot " @ %name @ " has no RACE defined, defaulting to Human");
		%botRace = "Human";
	}
	
	%expectedArmor = $RaceToArmorType[%botRace];
	if(%expectedArmor == "" || %expectedArmor == -1)
	{
		echo("WARNING: InitTownBotPostSpawn - Could not find armor for race '" @ %botRace @ "' for bot " @ %name @ ", defaulting to MaleHumanArmor");
		%expectedArmor = "MaleHumanArmor";
	}
	
	// Store race data
	storeData(%clientId, "RACE", %botRace);
	
	// Check current armor and fix if wrong
	%currentArmor = Player::getArmor(%clientId);
	if(%currentArmor != %expectedArmor)
	{
		echo("WARNING: InitTownBotPostSpawn - Bot " @ %name @ " has wrong armor '" @ %currentArmor @ "', correcting to '" @ %expectedArmor @ "'");
		// CRITICAL FIX: Use Player::setArmor to change the armor model, not Client::setSkin
		// Client::setSkin only changes texture/skin, not the actual armor PlayerData
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj != "" && %playerObj != -1)
		{
			Player::setArmor(%playerObj, %expectedArmor);
			// Also set skin to ensure visual appearance matches
			Client::setSkin(%clientId, %expectedArmor);
			echo("DEBUG: InitTownBotPostSpawn - Applied armor fix for " @ %name @ ": Player::setArmor(" @ %playerObj @ ", " @ %expectedArmor @ ")");
		}
		else
		{
			echo("ERROR: InitTownBotPostSpawn - Could not get player object to fix armor for " @ %name);
		}
	}
	
	// Set default flag to prevent town bots from dropping loot (edge case bug protection)
	storeData(%clientId, "NoDropLoot", "true");
	
	// Set BotInfoAiName so town bots are recognized as RPG AI bots (for auto-equip and other bot features)
	// CRITICAL: Must use "TownBot_" prefix to match SpawnZoneBots format
	%aiNameFull = "TownBot_" @ %name;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	
	// CRITICAL: Clear SpawnBotInfo to prevent town bots from being mistaken for enemy bots
	// Town bots should NOT have SpawnBotInfo set (only enemy bots have it)
	// This prevents zone change detection from killing town bots
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", ""); // Also clear SpawnTime to be safe
	
	// Weapon mounting flags (can be overridden per bot if needed)
	// MountWeaponOnSpawn: If "true", weapon will be mounted when bot spawns (default: "false")
	// MountWeaponOnTalk: If "true", weapon will be mounted when player talks to bot (default: "false" - not used, weapons only mount on collision)
	// To override for a specific bot, set these flags after InitTownBotPostSpawn or in the map file
	if(fetchData(%clientId, "MountWeaponOnSpawn") == "")
		storeData(%clientId, "MountWeaponOnSpawn", "false");  // Default: don't mount on spawn
	if(fetchData(%clientId, "MountWeaponOnTalk") == "")
		storeData(%clientId, "MountWeaponOnTalk", "false");   // Default: don't mount on talk (weapons only mount on collision)
	
	// Idle message flag (can be overridden per bot if needed)
	// ShowIdleMessage: If "true", bot will show idle message if 30 minutes pass without interaction (default: "false")
	// To enable for a specific bot, set this flag after InitTownBotPostSpawn or in the map file
	if(fetchData(%clientId, "ShowIdleMessage") == "")
	{
		// Enable idle message for the 3 new Gian Echos bots
		if(%name == "Giovanni Echos" || %name == "GodFather Gian" || %name == "Uncle Tony")
			storeData(%clientId, "ShowIdleMessage", "true");  // Enable for new Gian Echos bots
		else
			storeData(%clientId, "ShowIdleMessage", "false");  // Default: don't show idle message
	}
	
	// Initialize last interaction time (current time)
	storeData(%clientId, "LastInteractionTime", getSimTime());
	
	// Disable AI behavior to prevent town bots from moving or attacking
	// Set AI variables to keep bots stationary
	AI::setVar(%aiName, "pathType", "none");  // Disable pathfinding
	AI::setVar(%aiName, "spotDist", 0);  // Disable target spotting
	AI::setVar(%aiName, "attackMode", 0);  // Disable attacking
	AI::setVar(%aiName, "iq", 0);  // Disable AI intelligence
	
	// Set "dumb AI" flag to prevent any AI behavior
	storeData(%clientId, "dumbAIflag", "true");
	
	// CRITICAL: Set team (may have been set already during spawn, but ensure it's correct)
	// Default to team 0 (same as players) if TEAM is not specified
	// This is a second set to ensure team is correct even if something reset it
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// Verify team was set correctly (debug check)
	%actualTeam = GameBase::getTeam(%clientId);
	if(%actualTeam != %botTeam)
	{
		echo("WARNING: InitTownBotPostSpawn - Team mismatch for " @ %name @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam @ ". Re-setting...");
		GameBase::setTeam(%clientId, %botTeam);
	}
	
	// CRITICAL: Schedule a team verification after a short delay to catch UpdateTeam() overriding it
	schedule("VerifyTownBotTeam(" @ %clientId @ ", \"" @ %name @ "\", " @ %botTeam @ ");", 0.2);
	
	// Set animation to root (idle)
	GameBase::playSequence(%clientId, 0, "root");
	
	// Add to TownBotList (may have been added already during spawn, but ensure it's there)
	// Check if already in list to avoid duplicates
	%alreadyInList = false;
	for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
	{
		if(%id == %clientId)
		{
			%alreadyInList = true;
			break;
		}
	}
	if(!%alreadyInList)
		$TownBotList = $TownBotList @ %clientId @ " ";
	
	// Store items from map file ITEMS field for later mounting
	%items = $BotInfo[%name, ITEMS];
	if(%items != "")
	{
		// Store the items string for later processing
		storeData(%clientId, "TownBotItems", %items);
	}
	
	// Add to MissionCleanup set (use player object, delay slightly to ensure it's fully initialized)
	// Schedule the addToSet call to ensure the player object is fully initialized
	schedule("addToSetMissionCleanup(" @ %clientId @ ");", 0.1);
}

// Helper function to add town bot to MissionCleanup after player object is fully initialized
function addToSetMissionCleanup(%clientId)
{
	// CRITICAL: Validate clientId first
	if(%clientId == "" || %clientId == -1)
	{
		echo("ERROR: addToSetMissionCleanup - Invalid clientId (" @ %clientId @ "), aborting");
		return;
	}
	
	%playerObj = Client::getOwnedObject(%clientId);
	
	// CRITICAL: Validate player object exists and is valid before adding to MissionCleanup
	if(%playerObj != "" && %playerObj != -1 && isObject(%playerObj))
	{
		// Double-check MissionCleanup exists
		if(isObject("MissionCleanup"))
		{
			if($MISSION_CLEANUP_DEBUG) echo("[DEBUG] addToSetMissionCleanup - Adding player object " @ %playerObj @ " to MissionCleanup for clientId " @ %clientId);
			addToSet("MissionCleanup", %playerObj);
		}
		else
		{
			echo("ERROR: addToSetMissionCleanup - MissionCleanup SimSet does not exist for clientId " @ %clientId);
		}
	}
	else
	{
		// Player object doesn't exist - this is normal for bots that have been deleted
		// Only log as warning if the client still exists (to avoid spam for deleted bots)
		if(Client::getName(%clientId) != "" && Client::getName(%clientId) != -1)
		{
			echo("WARNING: addToSetMissionCleanup - player object not found for clientId " @ %clientId @ " (playerObj=" @ %playerObj @ ")");
		}
	}
}

// Mount items for a single town bot (used for dynamically spawned bots)
function InitTownBotItemsForBot(%clientId, %botName)
{
	%items = fetchData(%clientId, "TownBotItems");
	if(%items == "")
		%items = $BotInfo[%botName, ITEMS];
	
	if(%items == "")
	{
		//echo("DEBUG: InitTownBotItemsForBot - No items found for " @ %botName);
		return;
	}
	
	//echo("DEBUG: InitTownBotItemsForBot - Processing items for " @ %botName @ " (clientId: " @ %clientId @ ")");
	
	// Get player object from clientId
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: InitTownBotItemsForBot - could not find player object for clientId " @ %clientId);
		return;
	}
	
	// First pass: Add all items to inventory (including equipped versions for armor/accessories)
	for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
	{
		%itemName = GetWord(%items, %j);
		%itemCount = GetWord(%items, %j + 1);
		
		// Skip non-item keywords
		if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
		   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
		   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
		   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
		   %itemName == "LVLS" || %itemName == "LVLE")
			continue;
		
		if(%itemName != "" && %itemCount != "" && %itemCount > 0)
		{
			%itemData = getItemData(%itemName);
			if(%itemData != "")
			{
				// Add base item to inventory
				Player::incItemCount(%clientId, %itemName, %itemCount);
				
				// For armor and accessories, also add the equipped version ("0" suffix) to inventory
				if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType || %itemData.className == "Accessory")
				{
					%equippedVersion = %itemName @ "0";
					%equippedData = getItemData(%equippedVersion);
					if(%equippedData != "")
					{
						Player::incItemCount(%clientId, %equippedVersion, %itemCount);
					}
				}
			}
		}
	}
	
	// Second pass: Mount items (use the same logic as InitTownBotItems)
	// This is a simplified version that mounts armor and shields (weapons mount on talk by default)
	%weaponsToMount = "";
	%armorToMount = "";
	%shieldsToMount = "";
	
	for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
	{
		%itemName = GetWord(%items, %j);
		%itemCount = GetWord(%items, %j + 1);
		
		// Skip non-item keywords
		if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
		   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
		   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
		   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
		   %itemName == "LVLS" || %itemName == "LVLE")
			continue;
		
		if(%itemName != "" && %itemCount != "" && %itemCount > 0)
		{
			%itemData = getItemData(%itemName);
			%accessoryType = $AccessoryVar[%itemName, $AccessoryType];
			
			// Check if it's a weapon
			%isWeapon = false;
			if(%itemData != "")
			{
				if(%itemData.className == "Weapon")
					%isWeapon = true;
			}
			if(!%isWeapon && (%accessoryType == 7 || %accessoryType == 9 || %accessoryType == 10))
				%isWeapon = true;
			
			if(%isWeapon)
			{
				if(%itemName != "Dagger")
					%weaponsToMount = %weaponsToMount @ %itemName @ " ";
			}
			else if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType)
			{
				%armorToMount = %armorToMount @ %itemName @ " ";
			}
			else if($AccessoryVar[%itemName, $AccessoryType] == $ShieldAccessoryType)
			{
				%shieldsToMount = %shieldsToMount @ %itemName @ " ";
			}
		}
	}
	
	// Mount armor (always mount armor on spawn)
	if(%armorToMount != "")
	{
		%armorName = GetWord(%armorToMount, 0);
		%equippedArmor = %armorName @ "0";
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck == -1 || %playerObjCheck == "")
		{
			// Bot was deleted - silently return
			return;
		}
		%armorCountInInv = Player::getItemCount(%clientId, %equippedArmor);
		if(%armorCountInInv > 0)
		{
			// Check if armor is already mounted - if so, verify it's the correct armor
			%currentArmor = Player::getMountedItem(%clientId, 1);
			if(%currentArmor == %equippedArmor)
			{
				// Already mounted correctly, but verify skin is correct
				%armorSkin = $ArmorSkin[%armorName];
				if(%armorSkin != "")
				{
					%currentSkin = Client::getSkin(%clientId);
					if(%currentSkin != %armorSkin)
					{
						echo("DEBUG: Armor " @ %equippedArmor @ " already mounted on " @ %botName @ ", but skin is wrong (current: " @ %currentSkin @ ", expected: " @ %armorSkin @ ") - fixing skin");
						Client::setSkin(%clientId, %armorSkin);
					}
				}
				echo("DEBUG: Armor " @ %equippedArmor @ " already mounted on " @ %botName @ ", skipping remount");
			}
			else
			{
				// Unmount existing armor if different armor is mounted
				if(%currentArmor != "" && %currentArmor != -1)
				{
					echo("DEBUG: Unmounting existing armor " @ %currentArmor @ " before mounting " @ %equippedArmor);
					Player::unMountItem(%clientId, 1);
				}
				
				%armorSkin = $ArmorSkin[%armorName];
				if(%armorSkin != "")
					Client::setSkin(%clientId, %armorSkin);
				Player::mountItem(%playerObj, %equippedArmor, 1, 0);
			}
		}
	}
	
	// Mount shields (always mount shields on spawn)
	if(%shieldsToMount != "")
	{
		%shieldName = GetWord(%shieldsToMount, 0);
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck == -1 || %playerObjCheck == "")
		{
			// Bot was deleted - silently return
			return;
		}
		%shieldCountInInv = Player::getItemCount(%clientId, %shieldName);
		if(%shieldCountInInv > 0)
		{
			%currentShield = Player::getMountedItem(%clientId, 2);
			if(%currentShield != "" && %currentShield != -1)
				Player::unMountItem(%clientId, 2);
			Player::mountItem(%playerObj, %shieldName, 2);
		}
	}
	
	// Weapons mount on collision only (MountWeaponOnSpawn is false by default)
	// Only mount weapon now if MountWeaponOnSpawn is explicitly set to true
	%mountWeaponOnSpawn = fetchData(%clientId, "MountWeaponOnSpawn");
	if(%mountWeaponOnSpawn == "true" && %weaponsToMount != "")
	{
		%weaponName = GetWord(%weaponsToMount, 0);
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck == -1 || %playerObjCheck == "")
		{
			// Bot was deleted - silently return
			return;
		}
		%weaponCountInInv = Player::getItemCount(%clientId, %weaponName);
		if(%weaponCountInInv > 0)
		{
			%currentWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
			if(%currentWeapon != "" && %currentWeapon != -1)
				Player::unMountItem(%clientId, $WeaponSlot);
			Player::mountItem(%playerObj, %weaponName, $WeaponSlot, 0);
			schedule("verifyWeaponMount(" @ %clientId @ ", \"" @ %weaponName @ "\", \"" @ %botName @ "\");", 0.1);
		}
	}
	
	//echo("DEBUG: InitTownBotItemsForBot - Completed for " @ %botName);
}

// Mount items on town bots after initialization completes
// This is called via schedule to prevent hanging during InitTownBots()
// Town bots are now Player objects, so we can use Player functions!
function InitTownBotItems()
{
	echo("===== InitTownBotItems() called =====");
	%mountedCount = 0;
	
	for(%i = 0; (%clientId = GetWord($TownBotList, %i)) != -1; %i++)
	{
		%items = fetchData(%clientId, "TownBotItems");
		if(%items != "")
		{
			%botName = GameBase::getMapName(%clientId);
			echo("DEBUG: InitTownBotItems - Processing items for town bot: " @ %botName @ " (clientId: " @ %clientId @ ")");
			echo("DEBUG: Items string: " @ %items);
			
			// Get player object from clientId
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj == "" || %playerObj == -1)
			{
				echo("ERROR: InitTownBotItems - could not find player object for clientId " @ %clientId);
				continue;
			}
			
			// First pass: Add all items to inventory (including equipped versions for armor/accessories)
			// Second pass: Mount items (this ensures all items are in inventory before mounting)
			for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
			{
				%itemName = GetWord(%items, %j);
				%itemCount = GetWord(%items, %j + 1);
				
				// Skip non-item keywords (CLASS, LVL, COINS, etc.)
				if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
				   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
				   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
				   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
				   %itemName == "LVLS" || %itemName == "LVLE")
					continue;
				
				if(%itemName != "" && %itemCount != "" && %itemCount > 0)
				{
					// Try to get item data - if it fails, skip this item
					%itemData = getItemData(%itemName);
					if(%itemData != "")
					{
						// Add base item to inventory
						Player::incItemCount(%clientId, %itemName, %itemCount);
						
						// For armor and accessories, also add the equipped version ("0" suffix) to inventory
						if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType || %itemData.className == "Accessory")
						{
							%equippedVersion = %itemName @ "0";
							%equippedData = getItemData(%equippedVersion);
							if(%equippedData != "")
							{
								Player::incItemCount(%clientId, %equippedVersion, %itemCount);
								echo("DEBUG: Added equipped version " @ %equippedVersion @ " to inventory for " @ %botName);
							}
						}
					}
				}
			}
			
			// Second pass: Mount items (prioritize special items over default Dagger)
			// First, collect all items and identify what to mount
			%weaponsToMount = "";
			%armorToMount = "";
			%shieldsToMount = "";
			%hasDagger = false;
			
			for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
			{
				%itemName = GetWord(%items, %j);
				%itemCount = GetWord(%items, %j + 1);
				
				// Skip non-item keywords
				if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
				   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
				   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
				   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
				   %itemName == "LVLS" || %itemName == "LVLE")
					continue;
				
				if(%itemName != "" && %itemCount != "" && %itemCount > 0)
				{
					%itemData = getItemData(%itemName);
					if(%itemData != "")
					{
						if(%itemData.className == "Weapon")
						{
							if(%itemName == "Dagger")
								%hasDagger = true;
							else
								%weaponsToMount = %weaponsToMount @ %itemName @ " ";
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType)
						{
							%armorToMount = %armorToMount @ %itemName @ " ";
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $ShieldAccessoryType)
						{
							%shieldsToMount = %shieldsToMount @ %itemName @ " ";
						}
					}
				}
			}
			
			// Now mount items: weapons first (skip Dagger if other weapons exist), then armor, then shields
			for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
			{
				%itemName = GetWord(%items, %j);
				%itemCount = GetWord(%items, %j + 1);
				
				// Skip non-item keywords (CLASS, LVL, COINS, etc.)
				if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
				   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
				   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
				   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
				   %itemName == "LVLS" || %itemName == "LVLE")
					continue;
				
				if(%itemName != "" && %itemCount != "" && %itemCount > 0)
				{
					// Try to get item data - if it fails, skip this item
					%itemData = getItemData(%itemName);
					%accessoryType = $AccessoryVar[%itemName, $AccessoryType];
					
					// Check if it's a weapon - use accessoryType as fallback if className is empty
					%isWeapon = false;
					if(%itemData != "")
					{
						if(%itemData.className == "Weapon")
							%isWeapon = true;
					}
					// Fallback: check accessoryType for weapon types (Slashing=7, Piercing/Polearm=9, Bludgeon=10)
					if(!%isWeapon && (%accessoryType == 7 || %accessoryType == 9 || %accessoryType == 10))
						%isWeapon = true;
					
					if(%itemData != "" || %isWeapon)
					{
						%classNameStr = "empty";
						if(%itemData != "" && %itemData.className != "")
							%classNameStr = %itemData.className;
						echo("DEBUG: Processing item " @ %itemName @ " - className: " @ %classNameStr @ ", accessoryType: " @ %accessoryType @ ", isWeapon: " @ %isWeapon);
						
						// Then, mount items visually
						if(%isWeapon)
						{
							// Check if weapon should be mounted on spawn (flag check)
							%mountWeaponOnSpawn = fetchData(%clientId, "MountWeaponOnSpawn");
							if(%mountWeaponOnSpawn != "true")
							{
								echo("DEBUG: Skipping weapon " @ %itemName @ " - MountWeaponOnSpawn flag is not set to 'true'");
								continue;
							}
							
							// Skip Dagger if there are other weapons to mount
							if(%itemName == "Dagger" && %weaponsToMount != "")
							{
								echo("DEBUG: Skipping Dagger - other weapons will be mounted instead");
								continue;
							}
							
							// Check if item is in inventory before mounting
							%itemCountInInv = Player::getItemCount(%clientId, %itemName);
							if(%itemCountInInv <= 0)
							{
								echo("DEBUG: Weapon " @ %itemName @ " not in inventory (count: " @ %itemCountInInv @ ") - cannot mount");
								continue;
							}
							
							// Mount weapon in slot 0
							// Check if there's already a weapon mounted - unmount it first if needed
							%currentWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
							if(%currentWeapon != "" && %currentWeapon != -1)
							{
								echo("DEBUG: Unmounting existing weapon " @ %currentWeapon @ " before mounting " @ %itemName);
								Player::unMountItem(%clientId, $WeaponSlot);
							}
							echo("DEBUG: Mounting weapon " @ %itemName @ " on town bot " @ %botName @ " (inventory count: " @ %itemCountInInv @ ")");
							// Use 4th parameter (0) like the standard armor mounting code does
							Player::mountItem(%playerObj, %itemName, $WeaponSlot, 0);
							
							// Small delay to ensure mount completes
							schedule("verifyWeaponMount(" @ %clientId @ ", \"" @ %itemName @ "\", \"" @ %botName @ "\");", 0.1);
							
							%mountedCount++;
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType)
						{
							// Mount armor in slot 1 (try equipped version first)
							%equippedArmor = %itemName @ "0";
							%equippedData = getItemData(%equippedArmor);
							if(%equippedData != "")
							{
								// Check if equipped armor is in inventory (should be added in first pass)
								%armorCountInInv = Player::getItemCount(%clientId, %equippedArmor);
								if(%armorCountInInv <= 0)
								{
									echo("DEBUG: Equipped armor " @ %equippedArmor @ " not in inventory (count: " @ %armorCountInInv @ ") - cannot mount");
									continue;
								}
								
								// Check if armor is already mounted - if so, verify it's the correct armor
								%currentArmor = Player::getMountedItem(%clientId, 1);
								if(%currentArmor == %equippedArmor)
								{
									// Already mounted correctly, but verify skin is correct
									// Get the armor skin from $ArmorSkin mapping (use base item name, not equipped version)
									%armorSkin = $ArmorSkin[%itemName];
									if(%armorSkin != "")
									{
										%currentSkin = Client::getSkin(%clientId);
										if(%currentSkin != %armorSkin)
										{
											echo("DEBUG: Armor " @ %equippedArmor @ " already mounted on " @ %botName @ ", but skin is wrong (current: " @ %currentSkin @ ", expected: " @ %armorSkin @ ") - fixing skin");
											Client::setSkin(%clientId, %armorSkin);
										}
									}
									%mountedCount++;
									continue;
								}
								
								// Get the armor skin from $ArmorSkin mapping (use base item name, not equipped version)
								%armorSkin = $ArmorSkin[%itemName];
								if(%armorSkin != "")
								{
									echo("DEBUG: Setting skin to " @ %armorSkin @ " for armor " @ %itemName);
									Client::setSkin(%clientId, %armorSkin);
								}
								
								// Unmount existing armor if different armor is mounted
								if(%currentArmor != "" && %currentArmor != -1)
								{
									echo("DEBUG: Unmounting existing armor " @ %currentArmor @ " before mounting " @ %equippedArmor);
									Player::unMountItem(%clientId, 1);
								}
								
								// Mount the armor item - this will display the armor visually
								echo("DEBUG: Mounting armor " @ %equippedArmor @ " on town bot " @ %botName @ " (inventory count: " @ %armorCountInInv @ ", skin: " @ %armorSkin @ ")");
								Player::mountItem(%playerObj, %equippedArmor, 1, 0);
							}
							else
							{
								// Check if base armor is in inventory
								%armorCountInInv = Player::getItemCount(%clientId, %itemName);
								if(%armorCountInInv <= 0)
								{
									echo("DEBUG: Armor " @ %itemName @ " not in inventory (count: " @ %armorCountInInv @ ") - cannot mount");
									continue;
								}
								
								// Check if armor is already mounted - if so, verify it's the correct armor
								%currentArmor = Player::getMountedItem(%clientId, 1);
								if(%currentArmor == %itemName)
								{
									// Already mounted correctly, but verify skin is correct
									// Get the armor skin from $ArmorSkin mapping
									%armorSkin = $ArmorSkin[%itemName];
									if(%armorSkin != "")
									{
										%currentSkin = Client::getSkin(%clientId);
										if(%currentSkin != %armorSkin)
										{
											echo("DEBUG: Armor " @ %itemName @ " already mounted on " @ %botName @ ", but skin is wrong (current: " @ %currentSkin @ ", expected: " @ %armorSkin @ ") - fixing skin");
											Client::setSkin(%clientId, %armorSkin);
										}
									}
									%mountedCount++;
									continue;
								}
								
								// Get the armor skin from $ArmorSkin mapping
								%armorSkin = $ArmorSkin[%itemName];
								if(%armorSkin != "")
								{
									echo("DEBUG: Setting skin to " @ %armorSkin @ " for armor " @ %itemName);
									Client::setSkin(%clientId, %armorSkin);
								}
								
								// Unmount existing armor if different armor is mounted
								if(%currentArmor != "" && %currentArmor != -1)
								{
									echo("DEBUG: Unmounting existing armor " @ %currentArmor @ " before mounting " @ %itemName);
									Player::unMountItem(%clientId, 1);
								}
								
								// Mount the armor item - this will display the armor visually
								echo("DEBUG: Mounting armor " @ %itemName @ " on town bot " @ %botName @ " (inventory count: " @ %armorCountInInv @ ", skin: " @ %armorSkin @ ")");
								Player::mountItem(%playerObj, %itemName, 1, 0);
							}
							%mountedCount++;
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $ShieldAccessoryType)
						{
							// Mount shield in slot 2
							// Check if there's already a shield mounted - unmount it first if needed
							%currentShield = Player::getMountedItem(%clientId, 2);
							if(%currentShield != "" && %currentShield != -1)
							{
								echo("DEBUG: Unmounting existing shield " @ %currentShield @ " before mounting " @ %itemName);
								Player::unMountItem(%clientId, 2);
							}
							echo("DEBUG: Mounting shield " @ %itemName @ " on town bot " @ %botName);
							Player::mountItem(%playerObj, %itemName, 2);
							%mountedCount++;
						}
						else if(%itemData.className == "Accessory")
						{
							// Mount accessories in slot 1 (try equipped version first)
							%equippedAccessory = %itemName @ "0";
							%equippedData = getItemData(%equippedAccessory);
							if(%equippedData != "")
							{
								echo("DEBUG: Mounting accessory " @ %equippedAccessory @ " on town bot " @ %botName);
								Player::incItemCount(%clientId, %equippedAccessory, %itemCount);
								Player::mountItem(%playerObj, %equippedAccessory, 1);
							}
							else
							{
								echo("DEBUG: Mounting accessory " @ %itemName @ " on town bot " @ %botName);
								Player::mountItem(%playerObj, %itemName, 1);
							}
							%mountedCount++;
						}
					}
					else
					{
						echo("DEBUG: Could not get item data for " @ %itemName);
					}
				}
			}
		}
	}
	
	echo("===== InitTownBotItems() completed - Mounted " @ %mountedCount @ " items =====");
}

// Helper function to mount armor after unmounting (with delay)
function mountArmorDelayed(%clientId, %armorName, %baseItemName, %botName)
{
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: mountArmorDelayed - could not find player object for clientId " @ %clientId);
		return;
	}
	
	// Verify item is in inventory before mounting
	%armorCountInInv = Player::getItemCount(%clientId, %armorName);
	if(%armorCountInInv <= 0)
	{
		echo("ERROR: mountArmorDelayed - armor " @ %armorName @ " not in inventory (count: " @ %armorCountInInv @ ")");
		return;
	}
	
	echo("DEBUG: Mounting armor " @ %armorName @ " on town bot " @ %botName @ " (delayed after unmount, inventory count: " @ %armorCountInInv @ ")");
	// Use Player::mountItem() with slot 1 and 4th parameter (0) - this will mount the armor item and change the skin
	// Player::getArmor() returns the skin name (like MaleHumanArmor7), not the item name
	// When we mount the armor item, it will automatically change the skin
	Player::mountItem(%playerObj, %armorName, 1, 0);
	
}

// Helper function to verify weapon was mounted after a delay
function verifyWeaponMount(%clientId, %expectedWeapon, %botName)
{
	// CRITICAL: Validate player object exists before calling Player::getMountedItem
	// Bot may have been deleted between schedule and execution
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Bot was deleted - silently return
		return;
	}
	
	%mountedCheck = Player::getMountedItem(%clientId, $WeaponSlot);
	if(%mountedCheck == %expectedWeapon)
		echo("DEBUG: Successfully mounted weapon " @ %expectedWeapon @ " on " @ %botName);
	else
		echo("DEBUG: WARNING - Weapon mount may have failed. Expected: " @ %expectedWeapon @ ", Got: " @ %mountedCheck);
}

// Helper function to mount weapon for town bot when player collides with them
function MountTownBotWeapon(%clientId)
{
	// Check if this is a town bot (fast check using BotInfoAiName flag - only town bots have this)
	%botName = fetchData(%clientId, "BotInfoAiName");
	if(%botName == "")
	{
		// Not a town bot (BotInfoAiName is only set for town bots)
		return;
	}
	
	// Check if weapon is already mounted FIRST (fast check before expensive operations)
	%currentWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
	if(%currentWeapon != "" && %currentWeapon != -1 && %currentWeapon != "Dagger")
	{
		// Weapon already mounted (and not default Dagger) - exit early
		return;
	}
	
	// Note: Weapons always mount on collision (no flag check needed)
	
	// Get bot name and items
	%botName = fetchData(%clientId, "BotInfoAiName");
	// Fallback to Client::getName or player object name if BotInfoAiName is invalid
	if(%botName == "" || %botName == -1 || %botName == "0")
	{
		%botName = Client::getName(%clientId);
		if(%botName == "" || %botName == -1 || %botName == "0")
		{
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
				%botName = %playerObj.name;
		}
	}
	
	%items = fetchData(%clientId, "TownBotItems");
	if(%items == "")
		%items = $BotInfo[%botName, ITEMS];
	
	if(%items == "")
		return;
	
	// Find the first weapon in inventory to mount
	%weaponToMount = "";
	
	for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
	{
		%itemName = GetWord(%items, %j);
		%itemCount = GetWord(%items, %j + 1);
		
		// Skip non-item keywords
		if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
		   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
		   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
		   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
		   %itemName == "LVLS" || %itemName == "LVLE")
			continue;
		
		if(%itemName != "" && %itemCount != "" && %itemCount > 0)
		{
			%itemData = getItemData(%itemName);
			%accessoryType = $AccessoryVar[%itemName, $AccessoryType];
			
			// Check if it's a weapon - use accessoryType as fallback if className is empty
			%isWeapon = false;
			if(%itemData != "")
			{
				if(%itemData.className == "Weapon")
					%isWeapon = true;
			}
			// Fallback: check accessoryType for weapon types (Slashing=7, Piercing/Polearm=9, Bludgeon=10)
			if(!%isWeapon && (%accessoryType == 7 || %accessoryType == 9 || %accessoryType == 10))
				%isWeapon = true;
			
			if(%isWeapon)
			{
				// Found first weapon - mount it (could be Dagger or any other weapon)
				%weaponToMount = %itemName;
				break;
			}
		}
	}
	
	if(%weaponToMount != "")
	{
		//echo("DEBUG: MountTownBotWeapon - Mounting weapon: " @ %weaponToMount);
	}
	else
	{
		//echo("DEBUG: MountTownBotWeapon - No weapons found in bot inventory");
	}
	
	if(%weaponToMount != "")
	{
		// Check if weapon is in inventory
		%weaponCount = Player::getItemCount(%clientId, %weaponToMount);
		if(%weaponCount > 0)
		{
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
			{
				// Unmount existing weapon if needed
				if(%currentWeapon != "" && %currentWeapon != -1)
					Player::unMountItem(%clientId, $WeaponSlot);
				
				// Mount the weapon
				Player::mountItem(%playerObj, %weaponToMount, $WeaponSlot, 0);
			}
		}
	}
}
function RotateTownBot(%clientId, %rot)
{
	dbecho($dbechoMode, "RotateTownBot(" @ %clientId @ ", " @ %rot @ ")");

	// Validate clientId
	if(%clientId == "" || %clientId == -1)
	{
		echo("ERROR: RotateTownBot - invalid clientId: " @ %clientId);
		return;
	}

	// Get bot name from stored data first (more reliable)
	%name = fetchData(%clientId, "BotInfoAiName");
	// Fallback to Client::getName or player object name if BotInfoAiName is invalid
	if(%name == "" || %name == -1 || %name == "0")
	{
		%name = Client::getName(%clientId);
		if(%name == "" || %name == -1 || %name == "0")
		{
			// Fallback: try to get from player object
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
			{
				%name = %playerObj.name;
			}
		}
	}
	
	if(%name == "" || %name == -1 || %name == "0")
	{
		echo("ERROR: RotateTownBot - could not determine bot name for clientId " @ %clientId);
		return;
	}
	
	%pos = GameBase::getPosition(%clientId);
	if(%pos == "")
	{
		echo("ERROR: RotateTownBot - could not get position for clientId " @ %clientId);
		return;
	}
	
	// For town bots (Player objects), just set the rotation directly - no need to delete/respawn
	// This is much simpler and avoids AI::spawn issues
	GameBase::setRotation(%clientId, %rot);
	
	// Also update the player object rotation if needed
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != "" && %playerObj != -1)
	{
		GameBase::setRotation(%playerObj, %rot);
	}
}

// Post-spawn initialization for rotated town bots
function RotateTownBotPostSpawn(%aiName, %name)
{
	// For town bots, we already stored the client ID in $TownBotSpawned during spawn
	// Use that instead of AI::getId() which only works for Drones
	%clientId = $TownBotSpawned[%name];
	
	// If not found in stored list, try to get it from display name (town bots are Player objects)
	if(%clientId == -1 || %clientId == "")
	{
		%displayName = $BotInfo[%name, NAME];
		if(%displayName != "" && %displayName != -1)
		{
			%clientId = NEWgetClientByName(%displayName);
		}
	}
	
	// Note: We don't use AI::getId() for town bots since they're Player objects, not Drones
	// AI::getId() only works for Drones and will generate "Could not find drone" errors for Player objects
	
	if(%clientId == -1 || %clientId == "")
	{
		echo("ERROR: RotateTownBotPostSpawn - Could not get client ID for " @ %name @ " (aiName: " @ %aiName @ ")");
		return;
	}
	
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: RotateTownBotPostSpawn - could not find player object for clientId " @ %clientId);
		return;
	}
	
	// Set bot name property
	%playerObj.name = %name;
	
	// Set default flag to prevent town bots from dropping loot
	storeData(%clientId, "NoDropLoot", "true");
	
	// Set BotInfoAiName
	storeData(%clientId, "BotInfoAiName", %name);
	
	// Get player object
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: RotateTownBotPostSpawn - could not find player object for clientId " @ %clientId);
		return;
	}
	
	// Disable AI behavior
	AI::setVar(%aiName, "pathType", "none");
	AI::setVar(%aiName, "spotDist", 0);
	AI::setVar(%aiName, "attackMode", 0);
	AI::setVar(%aiName, "iq", 0);
	storeData(%clientId, "dumbAIflag", "true");
	
	// Set team - Default to team 0 (same as players) if TEAM is not specified
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// Set animation
	GameBase::playSequence(%clientId, 0, "root");
	
	// Add back to TownBotList
	$TownBotList = $TownBotList @ %clientId @ " ";
	
	// Add to MissionCleanup (use player object, delay slightly to ensure it's fully initialized)
	schedule("addToSetMissionCleanup(" @ %clientId @ ");", 0.1);
	
	// Restore items if they were stored
	%items = fetchData(%clientId, "TownBotItems");
	if(%items == "")
	{
		// Try to get items from BotInfo if not stored
		%items = $BotInfo[%name, ITEMS];
		if(%items != "")
			storeData(%clientId, "TownBotItems", %items);
	}
	
	// Schedule item mounting
	if(%items != "")
		schedule("InitTownBotItems();", 0.2);
}

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

//These are for the pets
function Pet::BeforeTurnEvil(%clientId)
{
	dbecho($dbechoMode, "Pet::BeforeTurnEvil(" @ %clientId @ ")");

	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: Pet::BeforeTurnEvil - Bot " @ %clientId @ " no longer exists. Skipping.");
		return;
	}

	remoteSay(%clientId, 0, "#say I'm starting to get enough of this...");
}
function Pet::TurnEvil(%clientId)
{
	dbecho($dbechoMode, "Pet::TurnEvil(" @ %clientId @ ")");

	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: Pet::TurnEvil - Bot " @ %clientId @ " no longer exists. Skipping.");
		return;
	}

	remoteSay(%clientId, 0, "#shout To hell with you all! Die!");

	// Fixed: Use %clientId instead of undefined %aiId
	storeData(%clientId, "botAttackMode", 1);
	
	// For enemy bots that are Player objects, directives may not work
	// Only try to remove directive if we can get a valid AI name and the bot is a Drone
	%botName = fetchData(%clientId, "BotInfoAiName");
	// Fallback to Client::getName if BotInfoAiName is invalid
	if(%botName == "" || %botName == -1 || %botName == "0")
		%botName = Client::getName(%clientId);
	
	// Only try to remove directive if we have a valid bot name
	// AI::newDirectiveRemove will handle the case where AI::getId() fails (Player objects)
	if(%botName != "" && %botName != -1 && %botName != "0")
		AI::newDirectiveRemove(%botName, 99);
	storeData(%clientId, "tmpbotdata", "");

	GameBase::setTeam(%clientId, 1);
}

// Verify and restore town bot team if it was changed by UpdateTeam() or other functions
function VerifyTownBotTeam(%clientId, %botName, %expectedTeam)
{
	// Validate inputs
	if(%clientId == -1 || %clientId == "" || %botName == "" || %botName == -1)
		return;
	
	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return;  // Bot doesn't exist anymore
	
	// Verify this is actually a town bot
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%expectedAiName = "TownBot_" @ %botName;
	if(%botInfoAiName != %expectedAiName)
	{
		// Not the bot we're looking for, or not a town bot
		return;
	}
	
	// Check current team
	%currentTeam = GameBase::getTeam(%clientId);
	if(%currentTeam != %expectedTeam)
	{
		echo("WARNING: VerifyTownBotTeam - Team was changed for town bot " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %expectedTeam @ ", got " @ %currentTeam @ ". Restoring...");
		GameBase::setTeam(%clientId, %expectedTeam);
		
		// CRITICAL FIX: Refresh armor visual after team correction
		// When team was -1, engine shows enemy skin even with correct armor model
		// We need to re-apply armor to refresh the visual appearance
		%botRace = $BotInfo[%botName, RACE];
		if(%botRace != "" && %botRace != -1)
		{
			%expectedArmor = $RaceToArmorType[%botRace];
			if(%expectedArmor != "" && %expectedArmor != -1)
			{
				// Refresh armor to fix visual appearance after team correction
				Player::setArmor(%playerObj, %expectedArmor);
				Client::setSkin(%clientId, $Server::teamSkin[0]);
				echo("DEBUG: VerifyTownBotTeam - Refreshed armor for " @ %botName @ " after team correction (team " @ %currentTeam @ " -> " @ %expectedTeam @ ", armor='" @ %expectedArmor @ "')");
			}
		}
		
		// Verify it was restored
		%newTeam = GameBase::getTeam(%clientId);
		if(%newTeam != %expectedTeam)
		{
			echo("ERROR: VerifyTownBotTeam - Failed to restore team for town bot " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %expectedTeam @ ", got " @ %newTeam @ ".");
			// Try one more time after a short delay
			%cmd = "VerifyTownBotTeam(" @ %clientId @ ", \"" @ %botName @ "\", " @ %expectedTeam @ ");";
			schedule(%cmd, 0.1);
		}
	}
}

// ============================================================================
// FINAL WORD: ForceTownBotSkin
// This function runs at T+2.0s after spawn to guarantee the correct skin/team
// even if other scripts (running at ~1.0s) attempted to change it.
// ============================================================================
function ForceTownBotSkin(%clientId, %botName)
{
	// Validate client ID
	if(%clientId == "" || %clientId == -1)
		return;
	
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return;  // Bot doesn't exist anymore
	
	// Verify this is still a town bot (check for TownBot_ prefix)
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	if(String::findSubStr(%botInfoAiName, "TownBot_") != 0)
		return;  // Not a town bot
	
	// DIAGNOSTIC: Log all values before applying
	%currentTeam = GameBase::getTeam(%clientId);
	%currentObjTeam = GameBase::getTeam(%playerObj);
	%currentArmor = Player::getArmor(%clientId);
	%currentSkin = Client::getSkinBase(%clientId);
	
	// [FINAL FIX] debug wrapper
	if($TOWNBOT_SKIN_DEBUG)
	{
		echo("[FINAL FIX] ForceTownBotSkin: BEFORE - clientId=" @ %clientId @ ", playerObj=" @ %playerObj);
		echo("[FINAL FIX]   Team: client=" @ %currentTeam @ ", obj=" @ %currentObjTeam);
		echo("[FINAL FIX]   Armor: '" @ %currentArmor @ "', Skin: '" @ %currentSkin @ "'");
	}
	
	// Get expected armor from race
	%botRace = $BotInfo[%botName, RACE];
	if(%botRace == "" || %botRace == -1)
		%botRace = "MaleHuman";
	%expectedArmor = $RaceToArmorType[%botRace];
	if(%expectedArmor == "" || %expectedArmor == -1)
		%expectedArmor = "MaleHumanArmor7";
	
	// FORCE Team 0 on BOTH client AND player object
	GameBase::setTeam(%clientId, 0);
	GameBase::setTeam(%playerObj, 0);
	
	// FORCE armor refresh - triggers engine visual update
	Player::setArmor(%playerObj, %expectedArmor);
	
	// FORCE skin with HARDCODED "rpgbase" - exactly like the manual command that worked
	Client::setSkin(%clientId, "rpgbase");
	
	// DIAGNOSTIC: Log all values after applying
	%newTeam = GameBase::getTeam(%clientId);
	%newObjTeam = GameBase::getTeam(%playerObj);
	%newArmor = Player::getArmor(%clientId);
	%newSkin = Client::getSkinBase(%clientId);
	
	if($TOWNBOT_SKIN_DEBUG)
	{
		echo("[FINAL FIX] ForceTownBotSkin: AFTER - clientId=" @ %clientId);
		echo("[FINAL FIX]   Team: client=" @ %newTeam @ ", obj=" @ %newObjTeam);
		echo("[FINAL FIX]   Armor: '" @ %newArmor @ "', Skin: '" @ %newSkin @ "'");
		echo("[FINAL FIX] ForceTownBotSkin: DONE for " @ %botName);
	}
}

// ============================================================================
// AGGRESSIVE TEAM ENFORCEMENT FOR ENEMY BOTS
// This function is called multiple times after spawn to ensure team sticks
// ============================================================================

function EnforceEnemyBotTeam(%clientId, %expectedTeam, %attempts)
{
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
	// CRITICAL: Also reject -1 and 0 as invalid teams (0 is observer team, -1 is invalid)
	if(%clientId == -1 || %clientId == "" || %expectedTeam == "" || %expectedTeam == -1 || %expectedTeam == 0)
	{
		if($TEAM_ENFORCE_DEBUG) echo("[TEAM ENFORCE] ScheduleTeamEnforcement: Invalid parameters - clientId=" @ %clientId @ ", expectedTeam=" @ %expectedTeam @ " - Using default team 1");
		%expectedTeam = 1; // Default to team 1 (enemy) if invalid
	}
	
	if($TEAM_ENFORCE_DEBUG) echo("[TEAM ENFORCE] ScheduleTeamEnforcement: Scheduling enforcement for clientId=" @ %clientId @ " with expectedTeam=" @ %expectedTeam);
	
	// Schedule multiple enforcement attempts at different intervals
	schedule("EnforceEnemyBotTeam(" @ %clientId @ ", " @ %expectedTeam @ ", 0);", 0.1);
	schedule("EnforceEnemyBotTeam(" @ %clientId @ ", " @ %expectedTeam @ ", 0);", 0.3);
	schedule("EnforceEnemyBotTeam(" @ %clientId @ ", " @ %expectedTeam @ ", 0);", 0.5);
	schedule("EnforceEnemyBotTeam(" @ %clientId @ ", " @ %expectedTeam @ ", 0);", 1.0);
	schedule("EnforceEnemyBotTeam(" @ %clientId @ ", " @ %expectedTeam @ ", 0);", 2.0);  // Add a longer delay attempt
}

function VerifyEnemyBotTeam(%clientId, %botName, %expectedTeam)
{
	// Validate inputs
	if(%clientId == -1 || %clientId == "" || %botName == "" || %botName == -1)
		return;
	
	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return;  // Bot doesn't exist anymore
	
	// Verify this is actually an enemy bot
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	if(%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0")
		return;  // Not an enemy bot
	
	// Check current team - use Player object if available, otherwise fall back to client ID
	%currentTeam = -1;
	if(%playerObj != -1 && %playerObj != "")
		%currentTeam = GameBase::getTeam(%playerObj);
	else
		%currentTeam = GameBase::getTeam(%clientId);
	
	if(%currentTeam != %expectedTeam)
	{
		echo("WARNING: VerifyEnemyBotTeam - Team mismatch for enemy bot " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %expectedTeam @ ", got " @ %currentTeam @ ". Restoring...");
		
		// CRITICAL: Set team on BOTH Player object AND client ID
		// GameBase::setTeam() might not work on both, so we need to set it on both
		// Set on Player object first (more reliable)
		if(%playerObj != -1 && %playerObj != "")
		{
			GameBase::setTeam(%playerObj, %expectedTeam);
		}
		// Also set on client ID (backup)
		GameBase::setTeam(%clientId, %expectedTeam);
		
		// Set again on Player object to ensure it sticks
		if(%playerObj != -1 && %playerObj != "")
		{
			GameBase::setTeam(%playerObj, %expectedTeam);
		}
		
		// Verify using Player object first (more reliable)
		%newTeam = -1;
		if(%playerObj != -1 && %playerObj != "")
		{
			%newTeam = GameBase::getTeam(%playerObj);
		}
		if(%newTeam == -1)
		{
			// Fall back to client ID
			%newTeam = GameBase::getTeam(%clientId);
		}
		
		if(%newTeam != %expectedTeam)
		{
			echo("ERROR: VerifyEnemyBotTeam - Failed to restore team for enemy bot " @ %botName @ ". Team is still " @ %newTeam @ " instead of " @ %expectedTeam @ ". Retrying in 0.5s...");
			// Schedule a retry with shorter delay - might be a timing issue
			schedule("VerifyEnemyBotTeam(" @ %clientId @ ", \"" @ %botName @ "\", " @ %expectedTeam @ ");", 0.5);
		}
		else
		{
			echo("INFO: VerifyEnemyBotTeam - Successfully set team " @ %expectedTeam @ " for enemy bot " @ %botName @ " (clientId=" @ %clientId @ ", playerObj=" @ %playerObj @ ")");
		}
	}
	else
	{
		if($BOT_TEAM_DEBUG) echo("[BOT TEAM DEBUG] VerifyEnemyBotTeam - Enemy bot " @ %botName @ " (clientId=" @ %clientId @ ") has correct team " @ %expectedTeam);
	}
}

// ---------------------------------------------------------------------------------------------------------
// REINFORCED DISCONNECT HANDLER
// ---------------------------------------------------------------------------------------------------------
// This function is called by the engine when ANY client drops (disconnects or is deleted).
// It acts as the final safety net for:
// 1. Ghost Bot Prevention: Decrementing spawn counters if they weren't already.
// 2. Race Condition Prevention: Marking the ID as recently freed so it isn't reused immediately.
function onClientDrop(%clientId)
{
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
				DecrementSpawnCounter(%spawnPointId, %clientId);
			}
		}
		
		// 4. Cleanup Bot Registry
		if($BotRegistry[%clientId] != "")
		{
			$BotRegistry[%clientId] = "";
			$EnemyBotCount--; 
			$TotalBotCount--;
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

