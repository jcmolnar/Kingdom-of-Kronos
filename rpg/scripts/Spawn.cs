$SPAWNLOOP_DEBUG = 1;         // Controls [SPAWN DEBUG] messages
$SPAWN_TRANSACTION_DEBUG = 1; // Controls [SPAWN TRANSACTION] messages
// ============================================================================
// CRITICAL FIX #2: Transaction-Based Spawn System
// ============================================================================
// Prevents spawn counter drift by using atomic reserve/commit/rollback pattern

// ReserveSpawnSlot: Atomically reserves a spawn slot (increments counter)
// Returns true if slot was successfully reserved, false if spawn point is full
function ReserveSpawnSlot(%spawnPoint)
{
	if(%spawnPoint == "" || %spawnPoint == -1)
		return false;
	
	%info = Object::getName(%spawnPoint);
	if(%info == "")
		return false;
	
	%currentCounter = $numAIperSpawnPoint[%spawnPoint];
	if(%currentCounter == "")
		%currentCounter = 0;
	
	%maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
	
	// Check if spawn point is already at max capacity
	if(%currentCounter >= %maxs)
	{
		// Throttle failure messages to reduce console spam (only log once per 10 seconds per spawn point)
		%currentTime = getIntegerTime(true);
		%lastFailLogTime = $ReserveSpawnSlotLastFailLog[%spawnPoint];
		if(%lastFailLogTime == "" || %lastFailLogTime == "0" || %lastFailLogTime == -1)
			%lastFailLogTime = 0;
		
		%timeSinceLastLog = %currentTime - %lastFailLogTime;
		if(%timeSinceLastLog >= 10000) // 10 seconds (in milliseconds)
		{
			if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] ReserveSpawnSlot(" @ %spawnPoint @ "): FAILED - Already at max (" @ %currentCounter @ "/" @ %maxs @ ")");
			$ReserveSpawnSlotLastFailLog[%spawnPoint] = %currentTime;
		}
		return false;
	}
	
	// Atomically increment counter
	$numAIperSpawnPoint[%spawnPoint]++;
	%newCounter = $numAIperSpawnPoint[%spawnPoint];
	
	// Mark this spawn point as having a reserved slot (prevents double-reservation)
	$SpawnSlotReserved[%spawnPoint] = "true";
	// Store reservation timestamp for timeout detection
	$SpawnSlotReservedTime[%spawnPoint] = getSimTime();
	
	if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] ReserveSpawnSlot(" @ %spawnPoint @ "): SUCCESS - Reserved slot (" @ %currentCounter @ " -> " @ %newCounter @ "/" @ %maxs @ ") @ " @ $SpawnSlotReservedTime[%spawnPoint]);
	return true;
}

// CommitSpawnSlot: Confirms spawn was successful (keeps the reserved slot)
// This should be called after bot is fully spawned and registered
function CommitSpawnSlot(%spawnPoint)
{
	if(%spawnPoint == "" || %spawnPoint == -1)
	{
		if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] CommitSpawnSlot: ERROR - Invalid spawnPoint (" @ %spawnPoint @ ")");
		return;
	}
	
	// Check if slot was actually reserved
	if($SpawnSlotReserved[%spawnPoint] != "true")
	{
		if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] CommitSpawnSlot(" @ %spawnPoint @ "): WARNING - Slot was not reserved (possible double-commit or missing reservation)");
	}
	
	// Clear the reservation flag - spawn was successful
	$SpawnSlotReserved[%spawnPoint] = "";
	
	// Clear reservation timestamp if it exists
	$SpawnSlotReservedTime[%spawnPoint] = "";
	
	if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] CommitSpawnSlot(" @ %spawnPoint @ "): Committed - Counter now: " @ $numAIperSpawnPoint[%spawnPoint]);
}

// RollbackSpawnSlot: Reverts a reserved slot (decrements counter)
// This should be called on ANY failure: engine error, script crash, duplicate ID, etc.
function RollbackSpawnSlot(%spawnPoint)
{
	if(%spawnPoint == "" || %spawnPoint == -1)
	{
		if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] RollbackSpawnSlot: ERROR - Invalid spawnPoint (" @ %spawnPoint @ ")");
		return;
	}
	
	%currentCounter = $numAIperSpawnPoint[%spawnPoint];
	if(%currentCounter == "" || %currentCounter == 0)
	{
		if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] RollbackSpawnSlot(" @ %spawnPoint @ "): WARNING - Counter already 0, cannot rollback");
		$SpawnSlotReserved[%spawnPoint] = "";
		$SpawnSlotReservedTime[%spawnPoint] = "";
		return;
	}
	
	// Check reservation age for timeout detection
	%reservedTime = $SpawnSlotReservedTime[%spawnPoint];
	%reservationAge = -1;
	if(%reservedTime != "" && %reservedTime != -1)
	{
		%reservationAge = getSimTime() - %reservedTime;
		if(%reservationAge > 15)
		{
			if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] RollbackSpawnSlot(" @ %spawnPoint @ "): WARNING - Rolling back reservation that was " @ %reservationAge @ "s old (possible timeout/stuck reservation)");
		}
	}
	
	// Atomically decrement counter
	$numAIperSpawnPoint[%spawnPoint]--;
	%newCounter = $numAIperSpawnPoint[%spawnPoint];
	
	// Clear the reservation flag and timestamp
	$SpawnSlotReserved[%spawnPoint] = "";
	$SpawnSlotReservedTime[%spawnPoint] = "";
	
	if(%reservationAge >= 0)
		if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] RollbackSpawnSlot(" @ %spawnPoint @ "): Rolled back (" @ %currentCounter @ " -> " @ %newCounter @ ", reservation age: " @ %reservationAge @ "s)");
	else
		if($SPAWN_TRANSACTION_DEBUG) echo("[SPAWN TRANSACTION] RollbackSpawnSlot(" @ %spawnPoint @ "): Rolled back (" @ %currentCounter @ " -> " @ %newCounter @ ")");
}

function InitSpawnPoints()
{
	dbecho($dbechoMode, "InitSpawnPoints()");

	%group = nameToID("MissionGroup\\SpawnPoints");

	if(%group != -1)
	{
		for(%i = 0; %i <= Group::objectCount(%group)-1; %i++)
		{
		      %this = Group::getObject(%group, %i);
			%info = Object::getName(%this);

			$MarkerZone[%this] = ObjectInWhichZone(%this);

			if(%info != "")
			{
				$numAIperSpawnPoint[%this] = 0;
				%indexes = "";

				for(%z = 5; GetWord(%info, %z) != -1; %z++)
					%indexes = %indexes @ GetWord(%info, %z) @ " ";

				echo("===================================================");
				echo("Spawn Point was initialized, %this = " @ %this);
				echo("Max spawn per: " @ GetWord(%info, 0));
				echo("Min radius: " @ GetWord(%info, 1));
				echo("Max radius: " @ GetWord(%info, 2));
				echo("Min delay: " @ GetWord(%info, 3));
				echo("Max delay: " @ GetWord(%info, 4));
				echo("Spawn indexes: " @ %indexes);
				echo("Marker Zone ID: " @ $MarkerZone[%this]);
				echo("===================================================");

				SpawnLoop(%this);
			}
		}
	}
}

function SpawnLoop(%this)
{
	// WATCHDOG: Track this function for freeze detection
	Watchdog_Enter("SpawnLoop");
	
	dbecho($dbechoMode, "SpawnLoop(" @ %this @ ")");

	%info = Object::getName(%this);

	%mindelay = GetWord(%info, 3);
	%maxdelay = GetWord(%info, 4);
	%diff = %maxdelay - %mindelay;
	%delay = floor(getRandom() * %diff) + %mindelay;

	%indexes = "";
	for(%i = 5; GetWord(%info, %i) != -1; %i++)
		%indexes = %indexes @ GetWord(%info, %i) @ " ";

	%r = floor(getRandom() * (%i-5));
	%index = GetWord(%indexes, %r);

	%flag = "";
	if($SelectiveZoneBotSpawning)
	{
	%zoneId = $MarkerZone[%this];
		%zonePlayerCount = Zone::getNumPlayers(%zoneId);
		// Spawn zone rules:
		// - Zone is unknown/empty → NO spawning (spawn point must have valid zone)
		// - Zone is valid AND has players → Allow spawning
		// - Zone is valid but no players → Block spawning
		if(%zoneId != "" && %zoneId != -1 && %zonePlayerCount > 0)
		{
			%flag = True;
		}
		// else: Zone unknown OR zone valid but no players - don't spawn
		
		if(%flag)
			%flagStr = "true";
		else
			%flagStr = "false";
		//echo("[SPAWN DEBUG] SpawnLoop(" @ %this @ "): zoneId=" @ %zoneId @ ", zonePlayers=" @ %zonePlayerCount @ ", flag=" @ %flagStr);
	}
	else
		%flag = True;

	%currentCounter = $numAIperSpawnPoint[%this];
	if(%currentCounter == "")
		%currentCounter = 0;
	%maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
	
	// CRITICAL: Check if a spawn is already in progress for this spawn point
	// This prevents multiple spawns when SpawnLoop() runs during the 2-second delay
	%spawnInProgress = $SpawnPointInProgress[%this];
	
// Per-spawnpoint cooldown after bot death to give cleanup extra time
// If cooldown is active and not expired, skip this run
%cooldownUntil = $SpawnPointCooldownUntil[%this];
if(%cooldownUntil != "" && %cooldownUntil > getSimTime())
{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): cooldown active until " @ floor(%cooldownUntil) @ " (now=" @ floor(getSimTime()) @ "), skipping");
	// Schedule next loop and return
	schedule("SpawnLoop(" @ %this @ ");", %delay + 1);
	return;
}
// Clear expired cooldown
if(%cooldownUntil != "" && %cooldownUntil <= getSimTime())
	{
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): cooldown expired (was " @ floor(%cooldownUntil) @ ", now=" @ floor(getSimTime()) @ "), clearing");
	$SpawnPointCooldownUntil[%this] = "";
	}

	if(%flag)
		%flagStr = "true";
	else
		%flagStr = "false";
	
	// Enhanced debug logging to show spawnpoint state
	%registeredCount = GetRegisteredBotCount(%this);
	%reservedStatus = $SpawnSlotReserved[%this];
	if(%reservedStatus == "")
		%reservedStatus = "none";
	%reservedAge = "none";
	if($SpawnSlotReservedTime[%this] != "" && $SpawnSlotReservedTime[%this] != -1)
	{
		%reservedAge = getSimTime() - $SpawnSlotReservedTime[%this];
		%reservedAge = %reservedAge @ "s";
	}
	
	// Fix inProgress display - show explicit true/false
	%inProgressStr = "false";
	if(%spawnInProgress == "true")
		%inProgressStr = "true";
	
	// Get zone information for flag explanation
	%zoneInfo = "";
	if($SelectiveZoneBotSpawning)
	{
		%zoneId = $MarkerZone[%this];
		%zonePlayerCount = Zone::getNumPlayers(%zoneId);
		if(%zoneId != "" && %zoneId != -1)
			%zoneInfo = ", zone=" @ %zoneId @ ", zonePlayers=" @ %zonePlayerCount;
		else
			%zoneInfo = ", zone=unknown";
	}
	
	// Get cooldown information
	%cooldownInfo = "";
	if(%cooldownUntil != "" && %cooldownUntil != -1)
	{
		%cooldownRemaining = %cooldownUntil - getSimTime();
		if(%cooldownRemaining > 0)
			%cooldownInfo = ", cooldown=" @ floor(%cooldownRemaining) @ "s";
		else
			%cooldownInfo = ", cooldown=expired";
	}
	else
		%cooldownInfo = ", cooldown=none";
	
	// Debug logging removed to reduce console spam
	//echo("[SPAWN DEBUG] SpawnLoop(" @ %this @ "): counter=" @ %currentCounter @ "/" @ %maxs @ ", registered=" @ %registeredCount @ ", flag=" @ %flagStr @ %zoneInfo @ ", inProgress=" @ %inProgressStr @ ", reserved=" @ %reservedStatus @ ", reservedAge=" @ %reservedAge @ %cooldownInfo);
	
	// CRITICAL FIX: Atomically reserve the slot. This increments the counter IMMEDIATELY.
	if(%flag && %spawnInProgress != "true" && ReserveSpawnSlot(%this))
	{
		// Mark spawn as in progress to prevent duplicate spawns
		$SpawnPointInProgress[%this] = "true";
		
		// Get the NEW counter value after reservation for logging
		%reservedCounter = $numAIperSpawnPoint[%this];
		
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): SLOT RESERVED @ " @ floor(getSimTime()) @ ", delay=" @ %delay @ ", max=" @ %maxs @ ", counter=" @ %reservedCounter @ ", idx=" @ %index);
		
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): ATTEMPTING SPAWN - calling AI::helper()");
		
		// Pass spawn point ID to helper so it can handle Rollback on failure
		// CRITICAL FIX: Added missing arguments (loadout="", spawnPointId=%this)
		%AIname = AI::helper($spawnIndex[%index], $spawnIndex[%index], "SpawnPoint " @ %this, "", %this);
		if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): AI::helper returned: " @ %AIname);
		
		// CRITICAL FIX: If spawning failed, rollback the reserved slot
		if(%AIname == -1 || %AIname == "")
		{
			if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): SPAWN FAILED - Rolling back reserved slot");
			RollbackSpawnSlot(%this);
			$SpawnPointInProgress[%this] = "";
		}
		else
		{
			// Clear the in-progress flag after spawn delay completes (internal delay is 10s, add buffer)
			// This allows the next spawn to proceed after the delay
			// Note: Flag is also cleared in SpawnAIGetClientId when spawn completes
			schedule("if($SpawnPointInProgress[" @ %this @ "] == \"true\") { $SpawnPointInProgress[" @ %this @ "] = \"\"; echo(\"[SPAWN FLOW] SpawnLoop(" @ %this @ "): clearing in-progress flag via timeout @ \" @ floor(getSimTime()) @ \"\"); }", 12);
		}
	}
	else
	{
		// Enhanced debug logging to show WHY ReserveSpawnSlot() failed
		%currentTime = getIntegerTime(true);
		%lastSkipLogTime = $SpawnLoopLastSkipLog[%this];
		if(%lastSkipLogTime == "" || %lastSkipLogTime == "0" || %lastSkipLogTime == -1)
			%lastSkipLogTime = 0;
		
		%timeSinceLastLog = %currentTime - %lastSkipLogTime;
		%shouldLog = false;
		if(%timeSinceLastLog >= 10000) // 10 seconds (in milliseconds)
			%shouldLog = true;
		
		// Detailed failure reasons
		%failureReasons = "";
		if(!%flag)
			%failureReasons = %failureReasons @ "zone_flag=false ";
		if(%spawnInProgress == "true")
			%failureReasons = %failureReasons @ "inProgress=true ";
		if(%currentCounter >= %maxs)
			%failureReasons = %failureReasons @ "counter(" @ %currentCounter @ ")>=max(" @ %maxs @ ") ";
		
		// Check if ReserveSpawnSlot would fail
		if(%flag && %spawnInProgress != "true" && %currentCounter >= %maxs)
		{
			%failureReasons = %failureReasons @ "ReserveSpawnSlot()=FULL ";
		}
		
		if(%shouldLog && %failureReasons != "")
		{
			if($SPAWNLOOP_DEBUG) echo("[SPAWN DEBUG] SpawnLoop(" @ %this @ "): SKIP - " @ %failureReasons @ "@ " @ floor(getSimTime()));
			$SpawnLoopLastSkipLog[%this] = %currentTime;
		}
	}

	// always call back the spawn loop, in case a spot is freed up for a helper to spawn
	// NOTE: per-bot spawn now includes a 5s internal delay; leave the loop fast (random 1–2s) but avoid overlap with in-progress spawns
	if(%spawnInProgress != "true")
		schedule("SpawnLoop(" @ %this @ ");", %delay);
	else
		// If a spawn is already in progress, reschedule with a small backoff to avoid tight reentry
		schedule("SpawnLoop(" @ %this @ ");", %delay + 1);
}
