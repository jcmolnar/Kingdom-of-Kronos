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
			echo("[SPAWN TRANSACTION] ReserveSpawnSlot(" @ %spawnPoint @ "): FAILED - Already at max (" @ %currentCounter @ "/" @ %maxs @ ")");
			$ReserveSpawnSlotLastFailLog[%spawnPoint] = %currentTime;
		}
		return false;
	}
	
	// Atomically increment counter
	$numAIperSpawnPoint[%spawnPoint]++;
	%newCounter = $numAIperSpawnPoint[%spawnPoint];
	
	// Mark this spawn point as having a reserved slot (prevents double-reservation)
	$SpawnSlotReserved[%spawnPoint] = "true";
	
	echo("[SPAWN TRANSACTION] ReserveSpawnSlot(" @ %spawnPoint @ "): SUCCESS - Reserved slot (" @ %currentCounter @ " -> " @ %newCounter @ "/" @ %maxs @ ")");
	return true;
}

// CommitSpawnSlot: Confirms spawn was successful (keeps the reserved slot)
// This should be called after bot is fully spawned and registered
function CommitSpawnSlot(%spawnPoint)
{
	if(%spawnPoint == "" || %spawnPoint == -1)
		return;
	
	// Clear the reservation flag - spawn was successful
	$SpawnSlotReserved[%spawnPoint] = "";
	
	echo("[SPAWN TRANSACTION] CommitSpawnSlot(" @ %spawnPoint @ "): Committed - Counter now: " @ $numAIperSpawnPoint[%spawnPoint]);
}

// RollbackSpawnSlot: Reverts a reserved slot (decrements counter)
// This should be called on ANY failure: engine error, script crash, duplicate ID, etc.
function RollbackSpawnSlot(%spawnPoint)
{
	if(%spawnPoint == "" || %spawnPoint == -1)
		return;
	
	%currentCounter = $numAIperSpawnPoint[%spawnPoint];
	if(%currentCounter == "" || %currentCounter == 0)
	{
		echo("[SPAWN TRANSACTION] RollbackSpawnSlot(" @ %spawnPoint @ "): WARNING - Counter already 0, cannot rollback");
		$SpawnSlotReserved[%spawnPoint] = "";
		return;
	}
	
	// Atomically decrement counter
	$numAIperSpawnPoint[%spawnPoint]--;
	%newCounter = $numAIperSpawnPoint[%spawnPoint];
	
	// Clear the reservation flag
	$SpawnSlotReserved[%spawnPoint] = "";
	
	echo("[SPAWN TRANSACTION] RollbackSpawnSlot(" @ %spawnPoint @ "): Rolled back (" @ %currentCounter @ " -> " @ %newCounter @ ")");
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
		// Allow spawning if zone is unknown (empty) OR if zone has players
		if(%zoneId == "" || %zonePlayerCount > 0)
			%flag = True;
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
		echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): cooldown active until " @ floor(%cooldownUntil) @ " (now=" @ floor(getSimTime()) @ "), skipping");
	// Schedule next loop and return
	schedule("SpawnLoop(" @ %this @ ");", %delay + 1);
	return;
}
// Clear expired cooldown
if(%cooldownUntil != "" && %cooldownUntil <= getSimTime())
	{
		echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): cooldown expired (was " @ floor(%cooldownUntil) @ ", now=" @ floor(getSimTime()) @ "), clearing");
	$SpawnPointCooldownUntil[%this] = "";
	}

	if(%flag)
		%flagStr = "true";
	else
		%flagStr = "false";
	//echo("[SPAWN DEBUG] SpawnLoop(" @ %this @ "): counter=" @ %currentCounter @ ", max=" @ %maxs @ ", flag=" @ %flagStr @ ", index=" @ %index @ ", inProgress=" @ %spawnInProgress);
	
	// CRITICAL FIX: Atomically reserve the slot. This increments the counter IMMEDIATELY.
	if(%flag && %spawnInProgress != "true" && ReserveSpawnSlot(%this))
	{
		// Mark spawn as in progress to prevent duplicate spawns
		$SpawnPointInProgress[%this] = "true";
		
		// Get the NEW counter value after reservation for logging
		%reservedCounter = $numAIperSpawnPoint[%this];
		
		echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): SLOT RESERVED @ " @ floor(getSimTime()) @ ", delay=" @ %delay @ ", max=" @ %maxs @ ", counter=" @ %reservedCounter @ ", idx=" @ %index);
		
		echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): ATTEMPTING SPAWN - calling AI::helper()");
		
		// Pass spawn point ID to helper so it can handle Rollback on failure
		// CRITICAL FIX: Added missing arguments (loadout="", spawnPointId=%this)
		%AIname = AI::helper($spawnIndex[%index], $spawnIndex[%index], "SpawnPoint " @ %this, "", %this);
		echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): AI::helper returned: " @ %AIname);
		
		// CRITICAL FIX: If spawning failed, rollback the reserved slot
		if(%AIname == -1 || %AIname == "")
		{
			echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): SPAWN FAILED - Rolling back reserved slot");
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
		// Skip debug messages for zone flag, counter, and spawn in progress to reduce console spam
		// Only log skip messages every 10 seconds to reduce frequency
		%currentTime = getIntegerTime(true);
		%lastSkipLogTime = $SpawnLoopLastSkipLog[%this];
		if(%lastSkipLogTime == "" || %lastSkipLogTime == "0" || %lastSkipLogTime == -1)
			%lastSkipLogTime = 0;
		
		%timeSinceLastLog = %currentTime - %lastSkipLogTime;
		%shouldLog = false;
		if(%timeSinceLastLog >= 10000) // 10 seconds (in milliseconds)
			%shouldLog = true;
		
		// if(!%flag)
		//	echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): SKIP - zone flag=false");
		if(%currentCounter >= %maxs && %shouldLog)
		{
			echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): SKIP - counter(" @ %currentCounter @ ") >= max(" @ %maxs @ ") @ " @ floor(getSimTime()));
			$SpawnLoopLastSkipLog[%this] = %currentTime;
		}
		if(%spawnInProgress == "true" && %shouldLog)
		{
			echo("[SPAWN FLOW] SpawnLoop(" @ %this @ "): SKIP - spawn already in progress @ " @ floor(getSimTime()));
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
