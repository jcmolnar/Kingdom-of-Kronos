function Game::initialMissionDrop(%clientId)
{
	dbecho($dbechoMode2, "Game::initialMissionDrop(" @ %clientId @ ")");

	Client::setGuiMode(%clientId, $GuiModePlay);
	%clientId.observerMode = "";

	centerprint(%clientId, "", 0);

	//===================================================
	// Look for invalid characters in the player's name.
	// If none are found, LoadCharacter
	//===================================================

	%name = Client::getName(%clientId);

	%retval = FindInvalidChar(%name);
	if(%retval != "")
	{
		%kickMsg = "You are using invalid characters in your name.  Use a simpler name.  Suggested clan tag characters are dashes and underscores.";
		%clientId.IsInvalid = True;
	}
	else
	{
		%rw = CheckForReservedWords(%name);
		if(%rw != "")
		{
			%kickMsg = "You are using a reserved word in your name (" @ %rw @ ").";
			%clientId.IsInvalid = True;
		}
		else
		{
			//==================================================
			// Check for duplicate names with players currently
			// on server. Also check for duplicate IP's
			//==================================================
			%flag = False;
			%list = GetPlayerIdList();
			%pip = Client::getTransportAddress(%clientId);
			for(%i = 0; (%id = GetWord(%list, %i)) != -1; %i++)
			{
				%n = Client::getName(%id);
				if(String::ICompare(%n, %name) == 0 && %id != %clientId)
				{
					%kickMsg = "This character name is currently in use.";
					%clientId.IsInvalid = True;
					%flag = True;
					break;
				}

				if(!$allowDuplicateIPs)
				{
					%ip = Client::getTransportAddress(%id);
					if(String::ICompare(TrimIP(%ip), TrimIP(%pip)) == 0 && %id != %clientId)
					{
						%kickMsg = "You are not allowed to run two clients on the same server.";
						%clientId.IsInvalid = True;
						%flag = True;
						break;
					}
				}
			}

			if(!%flag)
			{
				LoadCharacter(%clientId);

				if(String::Compare(fetchData(%clientId, "tmpname"), Client::getName(%clientId)) != 0)
				{
					%kickMsg = "This character name already exists. Please choose another.";
					%clientId.IsInvalid = True;
				}

				//==================================================
				// Now that the profile is loaded, we can verify
				// the password.
				//==================================================
	
				if($Client::info[%clientId, 5] == "")
				{
					%kickMsg = "You have not entered a password to protect your character. Select a password in the \"Other info\" field in your profile.";
					%clientId.IsInvalid = True;
				}
				if(fetchData(%clientId, "password") != $Client::info[%clientId, 5] && fetchData(%clientId, "password") != "")
				{
					%kickMsg = "This character name has already been selected by someone else on this server, or you are using an incorrect profile password. Change your password in \"Other info\" in your profile.";
					%clientId.IsInvalid = True;
				}
			}
		}
	}

	//==================================================
	// If there was invalid characters in the player's
	// name or the password was incorrect, then stick
	// the player in observer mode so he can be kicked
	// out soon after.
	//==================================================

	if(%clientId.IsInvalid)
	{
		schedule("Net::kick(" @ %clientId @ ", \"" @ %kickMsg @ "\");", 20);
		centerprint(%clientId, %kickMsg @ " You will automatically be kicked within 20 seconds.  If not, please disconnect manually.", 0);

		Client::setControlObject(%clientId, Client::getObserverCamera(%clientId));
		%camSpawn = Game::pickObserverSpawn(%clientId);
		Observer::setFlyMode(%clientId, GameBase::getPosition(%camSpawn), GameBase::getRotation(%camSpawn), false, false);
	}
	else
	{
		//==================================================
		// Everything went fine, spawn the player (or make
		// him/her choose stats if creating a new char)
		//==================================================

		if(%clientId.choosingGroup)
                  StartStatSelection(%clientId);
		else
			Game::playerSpawn(%clientId, false);
		if($HouseChecked[Client::getName(%clientId)] != 1)
		{
			$HouseMember[fetchData(%clientId, "MyHouse")] += 1;
			$HouseChecked[Client::getName(%clientId)] = 1;
		}
		
		// Recalculate and update member counts display when player connects
		RecalcHouseMemberCounts();
		UpdateHouseObjectivesDisplay();
	}
}

function Server::onClientDisconnect(%clientId)
{
	dbecho($dbechoMode2, "Server::onClientDisconnect(" @ %clientId @ ")");
	
	// SAFEGUARD: Clear the player's save file cache when they disconnect
	// This ensures the next client using this ID doesn't inherit a stale cache status
	ClearPlayerSaveFileCache(%clientId);
	
	// Handle zone player count for dynamic bot loading (skip AI bots)
	if(!Player::isAiControlled(%clientId))
	{
		// Announce player disconnect to all players in server chat (red text)
		// Only announce for players who have actually loaded and spawned (not connection attempts that failed)
		%playerName = Client::getName(%clientId);
		if(%playerName != "" && fetchData(%clientId, "HasLoadedAndSpawned"))
		{
			messageAll($MsgRed, %playerName @ " has disconnected from the server.");
		}
		%zone = fetchData(%clientId, "zone");
		if(%zone != "")
		{
			%zoneIndex = Zone::getIndex(%zone);
			if(%zoneIndex > 0)
			{
				%count = $ZonePlayerCount[%zoneIndex];
				if(%count > 0)
					$ZonePlayerCount[%zoneIndex] = %count - 1;
				
				// If no players left in zone, despawn bots after 30 seconds (prevents crash from too many operations at once)
				if($ZonePlayerCount[%zoneIndex] <= 0)
				{
					// Cancel any pending spawn (player disconnected before 10s delay expired)
					CancelPendingZoneSpawn(%zoneIndex);
					schedule("DespawnZoneBots(" @ %zoneIndex @ ");", 30);
				}
			}
		}
	}

	Client::setControlObject(%clientId, -1);

      if(!%clientId.IsInvalid && fetchData(%clientId, "HasLoadedAndSpawned"))
	{
		//Arena stuff
		if(IsInRoster(%clientId))
		{
			RestorePreviousEquipment(%clientId);
            	RemoveFromRoster(%clientId);
		}
		if(IsInArenaDueler(%clientId))
		{
			RestorePreviousEquipment(%clientId);
	            RemoveFromArenaDueler(%clientId);
		}

		//Pet stuff
		%list = fetchData(%clientId, "PersonalPetList");
		for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
		{
			%w = String::NEWgetSubStr(%list, 0, %p);
			FellOffMap(%w);
		}

		//Camp stuff
		%camp = nameToId("MissionCleanup\\Camp" @ %clientId);
		if(%camp != -1)
			DoCampSetup(%clientId, 5);

		%name = Client::getName(%clientId);
		$zonedis[%name] = "";

		// CRITICAL: Save currently mounted weapon before SaveCharacter() is called
		// This ensures the weapon state is persisted and can be restored on reconnect
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj != -1 && %playerObj != "")
		{
			%mountedWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
			if(%mountedWeapon != -1 && %mountedWeapon != "")
			{
				// Save the mounted weapon so it can be restored on reconnect
				storeData(%clientId, "savedMountedWeapon", %mountedWeapon);
			}
		}

		SaveCharacter(%clientId);

		ClearEvents(%clientId);
		
		// Recalculate and update member counts display when player disconnects
		RecalcHouseMemberCounts();
		UpdateHouseObjectivesDisplay();
	}

	for(%i = 0; %i < 10; %i++)
		$Client::info[%clientId, %i] = "";

      echo("GAME: clientdrop " @ %clientId);

	%set = nameToID("MissionCleanup/ObjectivesSet");
	for(%i = 0; (%obj = Group::getObject(%set, %i)) != -1; %i++)
      GameBase::virtual(%obj, "clientDropped", %clientId);
}

function Server::onClientConnect(%clientId)
{
	// CRITICAL SAFEGUARD: Immediately reserve this client ID for the connecting player
	// This prevents race conditions where a bot spawn might try to use this ID while
	// the player connection is being processed. Bot spawn code should check this flag.
	$ClientIdPlayerConnecting[%clientId] = getSimTime();
	
	// CRITICAL INTEGRATION: Check if a BOT is occupying this client ID
	// NOTE: By the time this runs, the engine has already created the player object
	// for the connecting player, so we must check if it's AI-controlled (bot) vs real player
	%occupyingObj = Client::getOwnedObject(%clientId);
	if(%occupyingObj != -1 && %occupyingObj != "" && isObject(%occupyingObj))
	{
		// Check if this is an AI-controlled bot (not the player who just connected)
		if(Player::isAiControlled(%clientId))
		{
			%occupyingName = Client::getName(%clientId);
			echo("CRITICAL: Server::onClientConnect - Client ID " @ %clientId @ " occupied by BOT '" @ %occupyingName @ "'. Forcing cleanup...");
			// Mark as no-drop to prevent lootbag spam
			storeData(%clientId, "noDropLootbagFlag", True);
			deleteObject(%occupyingObj);
			// Clear any stale bot data
			ClearAllBotData(%clientId, false);
		}
		// If not AI-controlled, it's the connecting player's object - that's normal
	}


	// this function located in connectivity.cs //
	// modified by Corona //


	##### MODIFY "CONNECTING" SCREEN GREETING:
	$Taurik::ConnectScreenMessage1 = "Kingdom of Kronos V0.8.1";
	$Taurik::ConnectScreenMessage2 = "Updates Every Friday!";


	%connectlogip = Client::getTransportAddress(%clientId);
	$connectlog::entry = "[Connection] " @ client::getname(%clientId) @ " (" @ %connectlogip @ ")";
	export("$connectlog::*", "config\\Connections Log.txt", True);
	export("$connectlog::*", "config\\Raw Log.txt", True);


	##### IGNORE SECTION BELOW IF NOT USING TCTRPG BAN SYSTEM ADD-ON
	//%addr = String::replaceban(Client::getTransportAddress(%clientId), ":", " ");
	//%addr = String::replaceban(%addr, ".", " ");
	//%addr = getWord(%addr, 1) @ " " @ getWord(%addr, 2) @ " " @ getWord(%addr, 3) @ " " @ getWord(%addr, 4);
	//	if (CocaineBanCheck(%clientId, %addr) == "ban") {
	//	schedule("CocaineKick(" @ %clientId @ ");", 0.1, %clientId);
	//	}
	##### IGNORE SECTION ABOVE IF NOT USING TCTRPG BAN SYSTEM ADD-ON


	dbecho($dbechoMode2, "Server::onClientConnect(" @ %clientId @ ")");

	// SAFEGUARD OPTIMIZATION: Initialize PlayerHasSaveFile cache
	// This avoids repeated disk I/O (isFile) checks in IsRealPlayer() and other safeguards
	// We only cache 'false' if we are certain it's a bot (AI controlled), otherwise leave empty for safety
	%checkName = Client::getName(%clientId);
	if(%checkName != "" && %checkName != -1 && isFile("temp\\" @ %checkName @ ".cs"))
		$PlayerHasSaveFile[%clientId] = true;
	else if(Player::isAiControlled(%clientId))
		$PlayerHasSaveFile[%clientId] = false;

	// CRITICAL FIX: Check if this client ID was previously used by a bot and clean it up
	// This prevents conflicts when a player connects and gets a client ID that was used by a bot
	%playerObj = Client::getOwnedObject(%clientId);
	%playerName = Client::getName(%clientId);
	
	// PHASE 1 FIX: Check if client ID was recently freed (still being cleaned up)
	%recentlyFreed = $ClientIdRecentlyFreed[%clientId];
	if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
	{
		%currentTime = getSimTime();
		%timeSinceFreed = %currentTime - %recentlyFreed;
		if(%timeSinceFreed < 30)  // Extended from 10s to 30s to catch more contaminated IDs
		{
			// Client ID was recently freed - FORCE DEEP CLEANUP and wait for state to clear
			echo("WARNING: Server::onClientConnect - Client ID " @ %clientId @ " was recently freed " @ %timeSinceFreed @ "s ago. Forcing deep cleanup and delaying player spawn...");
			
			// CRITICAL: Force deep cleanup of any leftover bot data BEFORE player spawns
			// This prevents player from inheriting stale bot state that causes blackscreen
			ClearAllBotData(%clientId, false);
			
			// Also clear any registry entries for this ID
			if($BotRegistry[%clientId] != "")
			{
				$BotRegistry[%clientId] = "";
				echo("Server::onClientConnect - Cleared contaminated BotRegistry for client " @ %clientId);
			}
			
			// Clear the flag and schedule a retry with longer delay to allow ghost state to clear
			$ClientIdRecentlyFreed[%clientId] = "";
			schedule("Server::onClientConnect(" @ %clientId @ ");", 3.0);  // Extended from 1s to 3s for blackscreen fix
			return;
		}
		else
		{
			// Enough time has passed, clear the flag
			$ClientIdRecentlyFreed[%clientId] = "";
		}
	}
	
	// PHASE 1 FIX: Check if an active bot is using this client ID
	// If a bot Player object exists and is AI-controlled, force cleanup/kill the bot
	if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
	{
		%isAiControlled = Player::isAiControlled(%clientId);
		%isRPGAI = isRPGAI(%clientId);
		
		if(%isAiControlled || %isRPGAI)
		{
			// Active bot found at this client ID - this is a collision!
			%botName = Client::getName(%clientId);
			echo("CRITICAL: Server::onClientConnect - Client ID " @ %clientId @ " is occupied by active bot '" @ %botName @ "'. Forcing cleanup to prevent collision...");
			
			// Force kill/delete the bot
			if(isObject(%playerObj))
			{
				// Mark as no-drop and no-exp to prevent side effects
				storeData(%clientId, "noDropLootbagFlag", True);
				storeData(%clientId, "noExperienceFlag", True);
				// Kill the bot's Player object
				deleteObject(%playerObj);
				echo("CRITICAL: Deleted bot Player object " @ %playerObj @ " for client ID " @ %clientId);
			}
		}
	}
	
	// Check if this client ID has bot data (indicating it was used by a bot)
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	
	// UNCONDITIONAL CLEANUP: If *any* bot data exists from a previous session, WIPE IT OUT.
	// This prevents "Data Leakage" where a new client (Real Player OR New Bot) inherits stale info.
	// Previously, we only cleaned up if we detected a Real Player, which allowed New Bots to inherit bad data.
	if(%spawnBotInfo != "" || %botInfoAiName != "" || $EnemyBotData[%clientId, "BotInfoAiName"] != "")
	{
		echo("WARNING: Server::onClientConnect - Client ID " @ %clientId @ " (" @ %playerName @ ") has stale bot data from previous session. Wiping all data to prevent conflicts.");
		
		// PRIORITY 1: Use unified ClearAllBotData() for all bot data clearing
		ClearAllBotData(%clientId, false);
		
		// Also clean registry for this ID if it exists
		if($BotRegistry[%clientId] != "")
		{
			$BotRegistry[%clientId] = "";
			echo("Server::onClientConnect - Cleared stale BotRegistry for client " @ %clientId);
		}
		
		// Clear from town bot tracking if present
		for(%regIndex = 0; %regIndex < $TownBotRegistryCount; %regIndex++)
		{
			%regBotName = $TownBotRegistry[%regIndex];
			if($TownBotSpawned[%regBotName] == %clientId)
			{
				$TownBotSpawned[%regBotName] = "";
				break;
			}
		}
	}

	if(!String::NCompare(Client::getTransportAddress(%clientId), "LOOPBACK", 8))
	{
		// force admin the loopback dude
		%clientId.adminLevel = 5;
	}
	echo("CONNECT: " @ %clientId @ " \"" @ escapeString(Client::getName(%clientId)) @ "\" " @ Client::getTransportAddress(%clientId));

	%clientId.noghost = true;
	%clientId.messageFilter = -1; // all messages

	if(Client::getName(%clientId) != "")
	{
		remoteEval(%clientId, SVInfo, version(), $Server::Hostname, $modList, $Server::Info, $ItemFavoritesKey);
		remoteEval(%clientId, MODInfo, $Taurik::ConnectScreenMessage1);
		$updatemodinfoID = %clientId;
		schedule("if($updatemodinfoID != -1 && Client::getName($updatemodinfoID) != \"\") remoteEval($updatemodinfoID, MODInfo, $Taurik::ConnectScreenMessage2);", 0.5);
		remoteEval(%clientId, FileURL, $Server::FileURL);
		// Enable hiding of enemy joined team messages for latest tribes repack clients
		remoteEval(%clientId, "Client::JoinMessages", true);
	}

//-------------------------------------------------------------

	// Clear temporary player state variables so the profile is as clean as possible
	ClearPlayerVariables(%clientId);
	Game::refreshClientScore(%clientId);	//so the player appears in the score list right away
}

function Game::onPlayerConnected(%playerId)
{
}

function Client::leaveGame(%clientId)
{
}

