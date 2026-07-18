//============================================================================
// rpgfunk_admin.cs — split from rpgfunk.cs (scatter split, Phase 2)
// Extracted 2026-07-18 at commit 5118b1b. Mechanical text move, no behavior change.
// shutdown/countdown + AFK-zone enforcement + generators
// Source line ranges listed in the rpgfunk.cs shell tombstone.
// exec'd by rpgfunk.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====


function Down(%t)
{
	dbecho($dbechoMode, "Down(" @ %t @ ")");

	%tinsec = %t * 60;
	for(%i = %t; %i > 1; %i--)
	{
		%a = (%tinsec - (60 * %i));
		schedule("dmsg(" @ %i @ ", \"minutes\");", %a);
	}

	if(%tinsec > 60)
		%startfrom = 60;
	else
		%startfrom = %tinsec;

	for(%i = %startfrom; %i >= 1; %i -= 10)
	{
		%a = (%tinsec - %i);
		schedule("dmsg(" @ %i @ ", \"seconds\");", %a);
	}
	
	// BUGFIX: set $ServerShuttingDown at SAVE time (T-10s), not announce time.
	// It previously flipped at countdown START, so a long countdown (e.g. d(30))
	// disabled onClientDrop bot cleanup and GUI paths for the entire window,
	// leaking bot counters while play continued.
	if(%tinsec >= 10)
	{
		%saveTime = %tinsec - 10;
		schedule("$ServerShuttingDown = true; SaveAllCharacters(); SaveWorld();", %saveTime);
	}
	else
	{
		// If shutdown time is less than 10 seconds, save immediately
		$ServerShuttingDown = true;
		SaveAllCharacters();
		SaveWorld();
	}
	
	// CRITICAL: Skip focusServer() during shutdown - it tries to load GUI elements
	// On dedicated servers or when GUI is torn down, this causes the MainWindow error
	// CRITICAL: Clear large in-memory strings before quit to prevent engine buffer issues
	schedule("FinalizeShutdownAndExit();", %tinsec);
}

// FinalizeShutdownAndExit - last step of Down(): clear large data, export server
// state, then hard-exit the process via forceExit() (ForceExitPlugin.dll).
// Why hard exit: mem.dll/hudbot can leave a background thread alive after quit()
// (see Plugins\_newStuff.txt V0.12 note), so the process never exits and
// InfiniteSpawn never restarts the server. TerminateProcess cannot be blocked
// by a stuck thread. Since forceExit() skips the engine's onExit() script, the
// exports normally done there (GUI.CS dedicated branch) are done here first.
function FinalizeShutdownAndExit()
{
	ClearLargePlayerDataBeforeQuit();

	export("Server::*", "config\\ServerPrefs.cs", False);
	if($Server::LastMission != "")
		export("Server::LastMission", "config\\ServerPrefs.cs", True);
	BanList::export("config\\banlist.cs");

	echo("[SHUTDOWN] Server state exported, forcing process exit...");
	forceExit();

	// Only reached if ForceExitPlugin.dll isn't loaded - fall back to graceful quit
	echo("[SHUTDOWN] forceExit() unavailable, falling back to quit()...");
	quit();
}
function d(%t)
{
	Down(%t);
}

// ClearLargePlayerDataBeforeQuit - Clears large in-memory strings before quit()
// This prevents engine buffer issues during shutdown caused by very long strings
// in player data that may be processed during engine cleanup
function ClearLargePlayerDataBeforeQuit()
{
	echo("[SHUTDOWN] Clearing large player data before quit...");
	
	for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
	{
		// Skip bots - only clear player data
		if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
			continue;
		
		%name = Client::getName(%clientId);
		if(%name == "" || %name == -1)
			continue;
		
		// Clear large in-memory strings that could cause engine buffer issues
		// Data is already saved to file at this point, so clearing is safe
		storeData(%clientId, "BankStorage", "");
		storeData(%clientId, "spawnStuff", "");
		
		echo("[SHUTDOWN] Cleared large data for " @ %name);
	}
	
	echo("[SHUTDOWN] Large player data cleared, proceeding with quit...");
}

function dmsg(%i, %w)
{
	echo("========= SERVER RESTARTING IN " @ %i @ " " @ %w @ " =========");
	messageAll(1, "Server restarting in " @ %i @ " " @ %w @ ". All characters will be saved automatically.");
}


function ActivateAllGenerators()
{
	dbecho($dbechoMode, "ActivateAllGenerators()");
	
	// Find all generators in MissionGroup and activate them
	%missionGroup = nameToID("MissionGroup");
	if(%missionGroup == -1)
		return;
	
	%generatorCount = 0;
	for(%i = 0; %i < Group::objectCount(%missionGroup); %i++)
	{
		%obj = Group::getObject(%missionGroup, %i);
		%dataName = GameBase::getDataName(%obj);
		
		if(%dataName == "Generator")
		{
			// Set generator to team 1 to match turrets (required for power connection)
			GameBase::setTeam(%obj, 1);
			
			// Activate the generator
			Generator::onActivate(%obj);
			
			%generatorCount++;
		}
	}
	
	if(%generatorCount > 0)
		echo("Activated " @ %generatorCount @ " generator(s).");
}

// ============================================================================
// Overlevel AFK zone enforcement
// ============================================================================

// Config (seconds/levels)
$AFKZoneCheckInterval = 30;
$AFKOverLevelBuffer = 5;
$AFKOverLevelInactivity = 180;
$AFKOverLevelWarnWindow = 30;
$AFKOverLevelMinZoneTime = 60;
$AFKOverLevelTeleportPos = "-905.1 -2201.61 522.14"; // Fallback position if marker group fails
$AFKTeleportMarkerGroup = "AFKTeleportPoints"; // Marker group in mission file for AFK teleport locations

// Hard caps by zone description (case-insensitive match)
$AFKZoneCap["Pig Den"] = 30;
$AFKZoneCap["Ogre Skybase"] = 50;
$AFKZoneCap["Ogre Stronghold"] = 50;
$AFKZoneCap["Ghost Town"] = 100;
$AFKZoneCap["Minotaur Tomb"] = 150;
$AFKZoneCap["Stone Henge"] = 250;
$AFKZoneCap["Demon Incubus"] = 400;

function AFKZone_GetDescFromFolder(%folderId)
{
	for(%z = 1; %z <= $numZones; %z++)
	{
		if($Zone::FolderID[%z] == %folderId)
			return $Zone::Desc[%z];
	}
	return "";
}

function AFKZone_GetCap(%desc)
{
	if(%desc == "" || %desc == -1)
		return "";

	// Direct lookup (Priority)
	if($AFKZoneCap[%desc] != "")
		return $AFKZoneCap[%desc];

	return "";
}

function AFKZone_ClearWarning(%id)
{
	$AFKZoneWarnUntil[%id] = "";
	$AFKZoneWarnPos[%id] = "";
	$AFKZoneWarnAck[%id] = "";
	$AFKZoneCode[%id] = ""; // Clear verification code
}

function AFKZone_Teleport(%id)
{
	%obj = Client::getOwnedObject(%id);
	
	// CRITICAL FIX: Clear zone data BEFORE teleporting so zone shows as "Unknown"
	// and bot spawning logic properly sees the zone as having one less player
	%oldZoneFolder = fetchData(%id, "zone");
	if(%oldZoneFolder != "" && %oldZoneFolder != -1)
	{
		%oldZoneIndex = Zone::getIndex(%oldZoneFolder);
		if(%oldZoneIndex > 0)
		{
			// Decrement zone player count so bot spawning logic sees the change
			%count = $ZonePlayerCount[%oldZoneIndex];
			if(%count > 0)
			{
				$ZonePlayerCount[%oldZoneIndex] = %count - 1;
				echo("[AFKZONE] Decremented player count for zone " @ %oldZoneIndex @ " (was: " @ %count @ ", now: " @ ($ZonePlayerCount[%oldZoneIndex]) @ ")");
				
				// If zone is now empty, cancel pending spawns and schedule despawn
				if($ZonePlayerCount[%oldZoneIndex] <= 0)
				{
					CancelPendingZoneSpawn(%oldZoneIndex);
					schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
				}
			}
		}
	}
	
	// Clear stored zone data - player is being moved to "nowhere"
	storeData(%id, "zone", "");
	storeData(%id, "tmpzone", "");
	storeData(%id, "lastzone", "");
	
	if(%obj != -1 && %obj != "" && isObject(%obj))
	{
		// Try to teleport to a random marker from the AFK teleport group
		// Retry up to 5 times to find an unoccupied spot
		%teleportSuccess = False;
		for(%attempt = 0; %attempt < 5; %attempt++)
		{
			%result = TeleportToMarker(%id, $AFKTeleportMarkerGroup, true, true);
			if(%result != False && %result != "")
			{
				%teleportSuccess = true;
				$AFKZoneLastPos[%id] = %result;
				break;
			}
		}
		
		// Fallback to hardcoded position if marker group doesn't exist or all spots occupied
		if(!%teleportSuccess)
		{
			GameBase::setPosition(%obj, $AFKOverLevelTeleportPos);
			$AFKZoneLastPos[%id] = $AFKOverLevelTeleportPos;
		}
	}
	Client::sendMessage(%id, $MsgRed, "You have been moved out of this low-level zone.");
	AFKZone_ClearWarning(%id);
	$AFKZoneLastMove[%id] = getSimTime();
	
	// CRITICAL: Reset zone folder tracking to force AFK zone system to detect zone change
	// This ensures the next AFKZone_Tick() will recognize the new zone
	$AFKZoneLastFolder[%id] = "";
	$AFKZoneEnterTime[%id] = "";
	
	// Refresh player state (like FellOffMap does) to update zone and other player data
	RefreshAll(%id);
	
	// Force immediate zone check so player's zone is updated right away
	// This ensures zone tracking works immediately after teleport
	schedule("DoZoneCheck(2, 0);", 0.1);
}

function AFKZone_Tick()
{
	%now = getSimTime();
	%list = GetPlayerIdList();
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		if(%id == "" || %id == -1)
			continue;
		if(isRPGAI(%id) || Player::isAiControlled(%id))
			continue;
		
		// Admin Override: Admins > 5 are immune to AFK checks
		if(%id.adminLevel > 5)
			continue;
		
		// Player Exclusion: Specific players can be excluded from AFK checks
		%playerName = Client::getName(%id);
		if(%playerName != "" && %playerName != -1)
		{
			if(String::ICompare(%playerName, "Jobo") == 0)
				continue;
		}
		
		%obj = Client::getOwnedObject(%id);
		if(%obj == -1 || %obj == "" || !isObject(%obj))
		{
			AFKZone_ClearWarning(%id);
			continue;
		}
		
		%zoneFolder = ObjectInWhichZone(%obj);
		if(%zoneFolder == "" || %zoneFolder == -1)
		{
			AFKZone_ClearWarning(%id);
			continue;
		}
		
		%zoneDesc = AFKZone_GetDescFromFolder(%zoneFolder);
		// Strip "DUNGEON " or "PROTECTED " prefix if present to match config
		%zoneDesc = String::replace(%zoneDesc, "DUNGEON ", "");
		%zoneDesc = String::replace(%zoneDesc, "PROTECTED ", "");
		
		%cap = AFKZone_GetCap(%zoneDesc);
		if(%cap == "")
		{
			AFKZone_ClearWarning(%id);
			$AFKZoneLastFolder[%id] = %zoneFolder;
			$AFKZoneEnterTime[%id] = %now;
			// Debug: zone found but no cap
			//echo("[AFKZONE] No cap for zone '" @ %zoneDesc @ "' (folder " @ %zoneFolder @ ")");
			continue;
		}
		
		// Zone change resets timers/warnings
		if($AFKZoneLastFolder[%id] != %zoneFolder)
		{
			$AFKZoneLastFolder[%id] = %zoneFolder;
			$AFKZoneEnterTime[%id] = %now;
			AFKZone_ClearWarning(%id);
		}
		
		// Track movement
		%pos = GameBase::getPosition(%obj);
		%lastPos = $AFKZoneLastPos[%id];
		if(%lastPos == "" || Vector::getDistance(%pos, %lastPos) > 1)
		{
			$AFKZoneLastPos[%id] = %pos;
			$AFKZoneLastMove[%id] = %now;
			// Movement cancels any pending warning
			if($AFKZoneWarnUntil[%id] != "")
				AFKZone_ClearWarning(%id);
		}
		
		%lastMove = $AFKZoneLastMove[%id];
		if(%lastMove == "" || %lastMove == -1)
			%lastMove = %now;
		
		// NOTE: getSimTime() is SECONDS - these were previously misnamed "Ms"
		// (the math was correct against the seconds-based config; only the names lied)
		%inactivitySec = %now - %lastMove;
		%zoneTimeSec = %now - $AFKZoneEnterTime[%id];
		
		%lvl = fetchData(%id, "LVL");
		if(%lvl == "" || %lvl == -1)
			%lvl = 0;
		
		%name = Client::getName(%id);
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("AFKDebug: Player " @ %name @ " is in " @ %zoneDesc @ " (Cap: " @ %cap @ ")");
		
		// Overlevel check
		if(%lvl <= (%cap + $AFKOverLevelBuffer))
		{
			AFKZone_ClearWarning(%id);
			continue;
		}
		
		// Debug overlevel status before warning triggers
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[AFKZONE DEBUG] id=" @ %id @ " zone='" @ %zoneDesc @ "' cap=" @ %cap @ " lvl=" @ %lvl @ " inactivity=" @ %inactivitySec @ "s zonetime=" @ %zoneTimeSec @ "s warnUntil=" @ $AFKZoneWarnUntil[%id] @ " lastMove=" @ %lastMove @ " lastPos=" @ $AFKZoneLastPos[%id]);
		
		%warnUntil = $AFKZoneWarnUntil[%id];
		if(%warnUntil != "")
		{
			%warnAck = $AFKZoneWarnAck[%id];
			if(%warnAck != "")
			{
				// Honor a valid #verify response: clear this warning cycle and keep the player in-zone.
				AFKZone_ClearWarning(%id);
				$AFKZoneLastPos[%id] = %pos;
				$AFKZoneLastMove[%id] = %now;
				continue;
			}

			%warnPos = $AFKZoneWarnPos[%id];
			%movedSinceWarn = false;
			if(%warnPos != "" && Vector::getDistance(%pos, %warnPos) > 1)
				%movedSinceWarn = true;
			
			if(%movedSinceWarn)
			{
				AFKZone_ClearWarning(%id);
				continue;
			}
			
			if(%now >= %warnUntil)
			{
				// No movement and no valid #verify acknowledgment before expiry: teleport out.
				AFKZone_Teleport(%id);
				continue;
			}
			
			// Warning still active, wait
			continue;
		}
		
		// Issue warning if overlevel, inactive, and in zone long enough
		if(%zoneTimeSec >= $AFKOverLevelMinZoneTime && %inactivitySec >= $AFKOverLevelInactivity)
		{
			$AFKZoneWarnPos[%id] = %pos;
			$AFKZoneWarnUntil[%id] = %now + $AFKOverLevelWarnWindow;
			$AFKZoneWarnAck[%id] = "";
			
			// Generate random 4-digit verification code (1000-9999)
			%code = floor(getRandom() * 8999) + 1000;
			$AFKZoneCode[%id] = %code;
			
			Client::sendMessage(%id, $MsgRed, "WARNING: You are over level for " @ %zoneDesc @ ". Type #verify " @ %code @ " to stay.");
			echo("[AFKZONE] Warn -> id=" @ %id @ " zone='" @ %zoneDesc @ "' cap=" @ %cap @ " lvl=" @ %lvl @ " inactivity=" @ %inactivitySec @ "s zonetime=" @ %zoneTimeSec @ "s code=" @ %code);
		}
	}
	
	// reschedule
	schedule("AFKZone_Tick();", $AFKZoneCheckInterval);
}

function StartAFKZoneEnforcement()
{
	if($AFKZoneEnforceStarted)
		return;
	$AFKZoneEnforceStarted = true;
	AFKZone_Tick();
}
