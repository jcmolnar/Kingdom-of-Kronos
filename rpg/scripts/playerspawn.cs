$AI_DEBUG_ENABLED = 1; // Toggle [DOT_OP_DEBUG] messages in this file
$AI_PERIODIC_DEBUG = 1; // Toggle [INERT DEBUG] messages in this file
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
		GameBase::startFadeIn(Client::getOwnedObject(%clientId));

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
				// Check for enemy bot name patterns (must be at start of name)
				if(String::findSubStr(%playerName, "Alien") == 0 || 
				   String::findSubStr(%playerName, "Admin") == 0 ||
				   String::findSubStr(%playerName, "Angel") == 0 ||
				   String::findSubStr(%playerName, "Demon") == 0 ||
				   String::findSubStr(%playerName, "Zombie") == 0 ||
				   String::findSubStr(%playerName, "Ogre") == 0 ||
				   String::findSubStr(%playerName, "Orc") == 0 ||
				   String::findSubStr(%playerName, "Pigman") == 0 ||
				   String::findSubStr(%playerName, "Pigmen") == 0 ||
				   String::findSubStr(%playerName, "Undead") == 0 ||
				   String::findSubStr(%playerName, "Minotaur") == 0 ||
				   String::findSubStr(%playerName, "Seal") == 0 ||
				   String::findSubStr(%playerName, "God") == 0 ||
				   String::findSubStr(%playerName, "Enemy") == 0)
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
	
	// Disable default command menu (Change Teams, etc) to allow RPG HUD on TAB
	remoteEval(%clientId, "setCommandStatus", 0);
	schedule("if(Client::getName(" @ %clientId @ ") != \"\") remoteEval(" @ %clientId @ ", \"setCommandStatus\", 0);", 0.5);
	schedule("if(Client::getName(" @ %clientId @ ") != \"\") remoteEval(" @ %clientId @ ", \"setCommandStatus\", 0);", 1.0);
	schedule("if(Client::getName(" @ %clientId @ ") != \"\") remoteEval(" @ %clientId @ ", \"setCommandStatus\", 0);", 2.0);
} 

function Game::autoRespawn(%clientId)
{
	dbecho($dbechoMode2, "Game::autoRespawn(" @ %clientId @ ")");

	if(%clientId.dead == 1)
		Game::playerSpawn(%clientId, True);
}
