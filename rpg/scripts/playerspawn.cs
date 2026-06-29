$AI_DEBUG_ENABLED = 0; // Toggle [DOT_OP_DEBUG] messages in this file
$AI_PERIODIC_DEBUG = 0; // Toggle [INERT DEBUG] messages in this file
function Game::pickObserverSpawn(%clientId)
{
	dbecho($dbechoMode2, "Game::pickObserverSpawn(" @ %clientId @ ")");

	%group = nameToID("MissionGroup\\ObserverDropPoints");
	%count = Group::objectCount(%group);

	if(%group == -1 || !%count)
		%group = nameToID("MissionGroup\\Teams\\team0\\DropPoints");
	%count = Group::objectCount(%group);
	if(%group == -1 || !%count)
		%group = nameToID("MissionGroup\\Teams\\team1\\DropPoints");
	%count = Group::objectCount(%group);
	if(%group == -1 || !%count)
		return -1;
	%spawnIdx = %clientId.lastObserverSpawn + 1;
	if(%spawnIdx >= %count)
		%spawnIdx = 0;
	%clientId.lastObserverSpawn = %spawnIdx;

	return Group::getObject(%group, %spawnIdx);
}

function Game::pickPlayerSpawn(%clientId, %respawn)
{
	dbecho($dbechoMode2, "Game::pickPlayerSpawn(" @ %clientId @ ", " @ %respawn @ ")");

	
	%lastzone = fetchData(%clientId, "lastzone");
	
	if(%lastzone == "" || %lastzone == -1)
	{
		%group = nameToID("MissionGroup/Teams/team0/DropPoints");
	}
	else
	{
		// Try zone's DropPoints first (original behavior - matches old code)
		%group = nameToID("MissionGroup/Zones/" @ Object::getName(%lastzone) @ "/DropPoints");
		%count = Group::objectCount(%group);
		// Only fall back to default if zone's DropPoints don't exist or are empty
		if(%group == -1 || !%count)
		{
			%group = nameToID("MissionGroup/Teams/team0/DropPoints");
		}
	}
	
	%count = Group::objectCount(%group);
	if(!%count)
	{
		return -1;
	}
	%spawnIdx = floor(getRandom() * (%count - 0.1));
	%value = %count;

	for(%i = %spawnIdx; %i < %value; %i++)
	{
		%set = newObject("set",SimSet);
		%obj = Group::getObject(%group, %i);
		%objPos = GameBase::getPosition(%obj);
		if(containerBoxFillSet(%set,$SimPlayerObjectType|$VehicleObjectType,%objPos,2,2,4,0) == 0)
		{
			deleteObject(%set);
			return %obj;		
		}
		if(%i == %count - 1)
		{
			%i = -1;
			%value = %spawnIdx;
		}
		deleteObject(%set);
	}
	return false;
}

function Game::playerSpawn(%clientId, %respawn)
{
	dbecho($dbechoMode2, "Game::playerSpawn(" @ %clientId @ ", " @ %respawn @ ")");

	if(!$ghosting)
		return false;

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);

	if(fetchData(%clientId, "isMimic"))
	{
		storeData(%clientId, "RACE", Client::getGender(%clientId) @ "Human");
		storeData(%clientId, "isMimic", "");
	}

	if(%clientId.RespawnMeInArena)
	{
		%group = nameToID("MissionGroup\\TheArena\\TeleportEntranceMarkers");

		if(%group != -1)
		{
			%num = Group::objectCount(%group);

			%r = floor(getRandom() * %num);
			%spawnMarker = Group::getObject(%group, %r);
		}
		else
		{
			%spawnMarker = Game::pickPlayerSpawn(%clientId, %respawn);
		}

		RefreshArenaTextBox(%clientId);
	}
	else
	{
		%spawnMarker = Game::pickPlayerSpawn(%clientId, %respawn);

		//the player is spawning normally, ie. not in the arena
		storeData(%clientId, "inArena", "");
		CloseArenaTextBox(%clientId);
	}

	if(%spawnMarker && %spawnMarker != -1)
	{
		%clientId.guiLock = "";
		%clientId.dead = "";
		if(%spawnMarker == -1)
		{
			%spawnPos = "0 0 600";
			%spawnRot = "0 0 0";
		}
		else
		{
			%campPos = fetchData(%clientId, "campPos");
			// Check if player has a valid campPos and this is their first spawn (not a respawn)
			// Also check that campPos is not "0" or empty/whitespace
			if(%campPos != "" && %campPos != "0" && %campPos != -1 && String::len(%campPos) > 3 && !%respawn)
			{
				//if the player HAS a valid campPos and it is his FIRST TIME SPAWNING, then spawn him at this campPos
				%spawnPos = %campPos;
				%spawnRot = fetchData(%clientId, "campRot");
				if(%spawnRot == "" || %spawnRot == "0" || %spawnRot == -1)
					%spawnRot = "0 0 0";
			}
			else
			{
				// Use spawn marker position (default for new players or respawns)
				%spawnPos = GameBase::getPosition(%spawnMarker);
				%spawnRot = GameBase::getRotation(%spawnMarker);
				
				// Validate spawn position - ensure Z coordinate is not underground
				%posZ = GetWord(%spawnPos, 2);
				if(%posZ == "" || %posZ == -1 || (%posZ * 1) < 0)
				{
					// Get X and Y from marker, but use a safe Z height
					%posX = GetWord(%spawnPos, 0);
					%posY = GetWord(%spawnPos, 1);
					%spawnPos = %posX @ " " @ %posY @ " 100";  // Set Z to 100 (above ground)
				}
			}
		}

		%armor = $RaceToArmorType[fetchData(%clientId, "RACE")];

		%pl = spawnPlayer(%armor, %spawnPos, %spawnRot);
		PlaySound(SoundSpawn2, %spawnPos);
		// NOTE: GameBase::startFadeIn moved to AFTER Client::setOwnedObject (see below)
		// Previously this was calling startFadeIn on the OLD player object before the new one was assigned

		echo("SPAWN: cl:" @ %clientId @ " pl:" @ %pl @ " marker:" @ %spawnMarker @ " position: " @ %spawnPos @ " armor:" @ %armor);

		if(%pl != -1)
		{
			// CRITICAL: Check if this is a bot BEFORE calling UpdateTeam()
			// UpdateTeam() may reset bot teams if SpawnBotInfo isn't set yet
			// Check by display name pattern first (works even if SpawnBotInfo isn't set)
			%playerName = Client::getName(%clientId);
			%isBot = false;
			if(%playerName != "" && %playerName != -1)
			{
				// Use centralized HasEnemyBotNamePrefix() from Ai.cs for consistent bot detection
				// This ensures all enemy races (Alien, Admin, Angel, Demon, God, Minotaur, Ogre, Orc, Pigman, Undead, Zombie, Seal, Enemy, Void) are detected
				if(HasEnemyBotNamePrefix(%playerName))
				{
					%isBot = true;
				}
			}
			
			// Also check if SpawnBotInfo or BotInfoAiName is set (more reliable but may not be set yet)
			%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
			%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
			if((%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1) ||
			   (%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1))
			{
				%isBot = true;
			}
			
			// Only call UpdateTeam() for players, not bots
			// Bots have their teams set in SpawnAIGetClientId() or AI::setWeapons()
			if(!%isBot)
			{
				UpdateTeam(%clientId);
				// Set team on player object after UpdateTeam() has set it
				GameBase::setTeam(%pl, Client::getTeam(%clientId));
			}
			else
			{
				// Bot detected - don't call UpdateTeam() to prevent team reset
				// The team will be set correctly in SpawnAIGetClientId() or AI::setWeapons()
				// Don't set team here - it might be -1 and would overwrite the correct team
				// The team will be set on the player object in SpawnAIGetClientId() or AI::setWeapons()
			}
			Client::setOwnedObject(%clientId, %pl);
			Client::setControlObject(%clientId, %pl);
			
			// CRITICAL FIX: Call startFadeIn on the NEW player object AFTER setting ownership
			// This was previously called on line 164 BEFORE setOwnedObject, fading in the wrong object
			GameBase::startFadeIn(%pl);
			
			Game::playerSpawned(%pl, %clientId, %armor, %respawn);

			if(%respawn)	      
			{
				setHP(%clientId, fetchData(%clientId, "MaxHP"));
				setMANA(%clientId, fetchData(%clientId, "MaxMANA"));
			}
		else
		{
			%playerObj = Client::getOwnedObject(%clientId);
			%tmphp = fetchData(%clientId, "tmphp");
			%tmpmana = fetchData(%clientId, "tmpmana");
			
			// For new spawns, if tmphp is empty or 0, use MaxHP instead to prevent setting HP to 0 (which kills player)
			if(%tmphp == "" || %tmphp == "0" || %tmphp == -1 || (%tmphp * 1) <= 0)
			{
				%tmphp = fetchData(%clientId, "MaxHP");
				if(%tmphp == "" || %tmphp == 0 || %tmphp == -1)
				{
					%skipHP = True;
				}
			}
			
			// For new spawns, if tmpmana is empty or 0, use MaxMANA instead
			if(%tmpmana == "" || %tmpmana == "0" || %tmpmana == -1 || (%tmpmana * 1) <= 0)
			{
				%tmpmana = fetchData(%clientId, "MaxMANA");
				if(%tmpmana == "" || %tmpmana == 0 || %tmpmana == -1)
				{
					%skipMANA = True;
				}
			}
			
			if(!%skipHP)
			{
				setHP(%clientId, %tmphp);
			}
			else
			{
			}
			
			%playerObjAfterHP = Client::getOwnedObject(%clientId);
			if(%playerObjAfterHP == -1 || %playerObjAfterHP == "")
			{
			}
			else if(!%skipMANA)
			{
				setMANA(%clientId, %tmpmana);
			}
			else
			{
			}
			
			storeData(%clientId, "tmphp", "");
			storeData(%clientId, "tmpmana", "");
		}
			storeData(%clientId.possessId, "dumbAIflag", "");
		}
		return true;
	}
	else
	{
		Client::sendMessage(%clientId,0,"Sorry No Respawn Positions Are Empty - Try again later ");
		return false;
	}
}

function Game::playerSpawned(%pl, %clientId, %armor)
{
	dbecho($dbechoMode2, "Game::playerSpawned(" @ %pl @ ", " @ %clientId @ ", " @ %armor @ ")");

	%currentTime = getSimTime();
	if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: ENTRY - pl=" @ %pl @ ", clientId=" @ %clientId @ ", armor=" @ %armor);
	
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%isBot = (Player::isAiControlled(%clientId) || 
	          (%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1) || 
	          (%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1));
	if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] Game::playerSpawned: ENTRY @ " @ %currentTime @ " - clientId=" @ %clientId @ ", isBot=" @ %isBot @ ", BotInfoAiName='" @ %botInfoAiName @ "', SpawnBotInfo='" @ %spawnBotInfo @ "'");

	if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: About to call storeData");
	storeData(%clientId, "HasLoadedAndSpawned", True);
	if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: storeData completed");
	
	// CRITICAL: Clear player connecting flag - player has fully spawned
	// Bot spawn code checks this flag to avoid collision with connecting players
	$ClientIdPlayerConnecting[%clientId] = "";
	
	// SPAWN PROTECTION: Give human players 5 seconds of invincibility after spawning
	// This prevents damage from bots that were attacking the previous clientId occupant
	if(!%isBot)
	{
		storeData(%clientId, "SpawnInvuln", "true");
		$ClientData[%clientId, "SpawnInvuln"] = "true";
		%playerName = Client::getName(%clientId);
		if(%playerName != "" && %playerName != -1)
		{
			$SpawnInvulnByName[%playerName] = getSimTime();
		}
		// Clear spawn protection after 5 seconds
		schedule("Game::ClearSpawnProtection(" @ %clientId @ ");", 5);
		
		// Ensure Telekinesis loop is running for players who own Telekinesis.
		// This makes startup independent from admin debug commands.
		Ascension::EnsureTelekinesisLoopForClient(%clientId);
	}

	if(%clientId.RespawnMeInArena)
	{
		//give him his equipment back
		RestorePreviousEquipment(%clientId);

		%clientId.RespawnMeInArena = "";
	}
	else
	{
		%spawnStuff = fetchData(%clientId, "spawnStuff");
		GiveThisStuff(%clientId, %spawnStuff, False);
		
		// Save weapon name for remount after RefreshAll
		%savedWeapon = fetchData(%clientId, "savedMountedWeapon");
	}

	%lck = fetchData(%clientId, "LCK");
	if(%lck < 0)
	{
		storeData(%clientId, "LCK", 0);
	}

	// CRITICAL: Save currently mounted weapon BEFORE RefreshAll() is called
	// RefreshAll() or its called functions might unmount the weapon
	%playerObj = Client::getOwnedObject(%clientId);
	%weaponToRemount = "";
	if(%playerObj != "" && %playerObj != -1)
	{
		%currentWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
		if(%currentWeapon != -1 && %currentWeapon != "")
		{
			// Weapon is mounted - save it for remounting after RefreshAll()
			%weaponToRemount = %currentWeapon;
		}
		else
		{
			// No weapon mounted - check if we have a saved weapon
			%savedWeapon = fetchData(%clientId, "savedMountedWeapon");
			if(%savedWeapon != "" && %savedWeapon != -1)
			{
				%weaponCount = Player::getItemCount(%clientId, %savedWeapon);
				if(%weaponCount > 0)
				{
					%weaponToRemount = %savedWeapon;
				}
			}
		}
	}

	// CRITICAL: Skip RefreshAll() for bots - they get it from AI::SetVar() which sets their skills
	// Bots don't need RefreshAll() here as their stats come from $BotInfo, not player data
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%isBot = (Player::isAiControlled(%clientId) || 
	          (%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1) || 
	          (%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1));
	
	if(!%isBot)
	{
		// NOTE: RefreshAll() is already called by GiveThisStuff() (line 5619 in rpgfunk.cs)
		// No need to call it again here to avoid redundant network packets and state sync issues
		// echo("[DOT_OP_DEBUG] Game::playerSpawned: RefreshAll already called by GiveThisStuff, skipping redundant call");
		
		// SAFETY ARCHITECTURE: Register Human Players
		if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: About to check/create PlayerGroup");
		if(!isObject("PlayerGroup"))
		{
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: Creating PlayerGroup SimSet");
			newObject("PlayerGroup", SimGroup, true);
		}
		if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: PlayerGroup exists, validating player object before add");
		if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: pl=" @ %pl @ ", isObject(pl)=" @ isObject(%pl) @ ", PlayerGroup type=" @ getObjectType(PlayerGroup));
		if(isObject(%pl))
		{
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: About to add player to PlayerGroup using addToSet (pl=" @ %pl @ ")");
			// Use addToSet() instead of .add() to avoid potential TorqueScript parsing issues
			addToSet(PlayerGroup, %pl);
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: addToSet(PlayerGroup, pl) completed");
		}
		else
		{
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: ERROR - Player object is invalid (pl=" @ %pl @ "), cannot add to PlayerGroup");
		}
	}
	else
	{
		// SAFETY ARCHITECTURE: Register AI Bots
		if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: About to check/create BotGroup");
		if(!isObject("BotGroup"))
		{
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: Creating BotGroup SimSet");
			newObject("BotGroup", SimGroup, true);
			if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] Game::playerSpawned: Created BotGroup SimSet");
		}
		if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: BotGroup exists, validating player object before add");
		if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: pl=" @ %pl @ ", isObject(pl)=" @ isObject(%pl) @ ", BotGroup type=" @ getObjectType(BotGroup));
		%countBefore = Group::objectCount(BotGroup);
		if(isObject(%pl))
		{
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: About to add bot to BotGroup using addToSet (pl=" @ %pl @ ")");
			// Use addToSet() instead of .add() to avoid potential TorqueScript parsing issues
			addToSet(BotGroup, %pl);
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: addToSet(BotGroup, pl) completed");
		}
		else
		{
			if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] Game::playerSpawned: ERROR - Player object is invalid (pl=" @ %pl @ "), cannot add to BotGroup");
		}
		%countAfter = Group::objectCount(BotGroup);
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[INERT DEBUG] Game::playerSpawned: Adding bot to BotGroup (obj=" @ %pl @ ", clientId=" @ %clientId @ ", count before=" @ %countBefore @ ", count after=" @ %countAfter @ ")");
	}
	
	// CRITICAL: After RefreshAll(), remount the weapon if it was unmounted
	if(%weaponToRemount != "" && %playerObj != "" && %playerObj != -1)
	{
		%currentWeaponAfterRefresh = Player::getMountedItem(%clientId, $WeaponSlot);
		if(%currentWeaponAfterRefresh == -1 || %currentWeaponAfterRefresh == "")
		{
			// Weapon was unmounted - remount it
			%weaponCount = Player::getItemCount(%clientId, %weaponToRemount);
			if(%weaponCount > 0)
			{
				Player::mountItem(%playerObj, %weaponToRemount, $WeaponSlot);
			}
		}
		// Clear saved weapon after attempting to remount
		if(%weaponToRemount == fetchData(%clientId, "savedMountedWeapon"))
		{
			storeData(%clientId, "savedMountedWeapon", "");
		}
	}
	
	// DUAL WIELD: Restore off-hand weapon visual on respawn/reconnect
	// This is scheduled slightly later to ensure main weapon is mounted first
	schedule("DualWield::RestoreOffHandVisual(" @ %clientId @ ");", 0.5);
	
	// REMOVED: remoteEval(%clientId, "setCommandStatus", 0) calls (x4)
	// No client defines remoteSetCommandStatus - these only produced
	// "RemoteSetCommandStatus: Unknown command" errors in the client
	// console on every spawn. setCommandStatus is a server-side engine
	// command (client, status, message), not a client remote function,
	// and the RPG menu on TAB is handled by Game::menuRequest anyway.
	
	// DEFENSIVE FIX: Final startFadeIn call at end of spawn to ensure visibility
	// This is a "belt and suspenders" approach - even if something caused the player/bot
	// to be faded out during spawn setup, this final call will bring them back visible
	// Fixes intermittent invisibility bug that can occur on connection or respawn
	%finalPlayerObj = Client::getOwnedObject(%clientId);
	if(%finalPlayerObj != "" && %finalPlayerObj != -1 && isObject(%finalPlayerObj))
	{
		GameBase::startFadeIn(%finalPlayerObj);
	}
} 

function Game::autoRespawn(%clientId)
{
	dbecho($dbechoMode2, "Game::autoRespawn(" @ %clientId @ ")");

	if(%clientId.dead == 1)
		Game::playerSpawn(%clientId, True);
}

//============================================================================
// SPAWN PROTECTION - Clear invincibility after timeout
//============================================================================
function Game::ClearSpawnProtection(%clientId)
{
	// Validate client still exists
	if(Client::getName(%clientId) == "" || Client::getName(%clientId) == -1)
		return;
	
	// Clear spawn protection flags
	storeData(%clientId, "SpawnInvuln", "");
	$ClientData[%clientId, "SpawnInvuln"] = "";
	
	%playerName = Client::getName(%clientId);
	if(%playerName != "" && %playerName != -1)
	{
		$SpawnInvulnByName[%playerName] = "";
	}
}

//============================================================================
// VISIBILITY SAFETY NET - Periodic loop to fix random invisibility
//============================================================================
$VisibilitySafetyInterval = 30;  // Seconds between visibility checks

function Game::StartVisibilitySafetyLoop()
{
	echo("[VISIBILITY] Starting periodic visibility safety net (every " @ $VisibilitySafetyInterval @ "s)");
	schedule("Game::VisibilitySafetyCheck();", $VisibilitySafetyInterval);
}

function Game::VisibilitySafetyCheck()
{
	// Loop through all connected players and ensure they're visible
	// This catches random invisibility issues that can occur during gameplay
	
	for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
	{
		// Skip if client has no name (not fully connected)
		%name = Client::getName(%clientId);
		if(%name == "" || %name == -1)
			continue;
		
		// Skip bots - they have defensive fixes in their spawn paths
		if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
			continue;
		
		// Skip if intentionally invisible (Hide in Shadows skill)
		if(fetchData(%clientId, "invisible"))
			continue;
		
		// Skip dead players
		if(IsDead(%clientId))
			continue;
		
		// Get player object and ensure visibility
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj != "" && %playerObj != -1 && isObject(%playerObj))
		{
			GameBase::startFadeIn(%playerObj);
		}
	}
	
	// Schedule next check
	schedule("Game::VisibilitySafetyCheck();", $VisibilitySafetyInterval);
}

// Start the visibility safety loop after server initialization (60 second delay)
schedule("Game::StartVisibilitySafetyLoop();", 60);
