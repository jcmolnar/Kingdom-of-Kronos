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
	
	// Handle zone player count for dynamic bot loading (skip AI bots)
	if(!Player::isAiControlled(%clientId))
	{
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
					// DEBUG: Commented out to reduce server lag
					//echo("[ZONE DEBUG] Zone " @ %zoneIndex @ " (" @ $Zone::Desc[%zoneIndex] @ ") is now empty - despawning bots in 30 seconds (player disconnected)");
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


	// this function located in connectivity.cs //
	// modified by Corona //


	##### MODIFY "CONNECTING" SCREEN GREETING:
	$Taurik::ConnectScreenMessage1 = "Kingdom of Kronos V0.8";
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

	// CRITICAL: Check if this client ID was previously used by a bot and clean it up
	// This prevents conflicts when a player connects and gets a client ID that was used by a bot
	%playerObj = Client::getOwnedObject(%clientId);
	%playerName = Client::getName(%clientId);
	
	// Check if this client ID has bot data (indicating it was used by a bot)
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	
	// If bot data exists but this is a real player (has a name and isn't a bot), clean up bot data
	if((%spawnBotInfo != "" || %botInfoAiName != "") && %playerName != "" && %playerName != -1)
	{
		// Check if this is actually a bot (isRPGAI) - if not, it's a real player and we need to clean up
		if(!isRPGAI(%clientId))
		{
			echo("WARNING: Server::onClientConnect - Client ID " @ %clientId @ " (" @ %playerName @ ") has bot data but is a real player. Cleaning up bot data...");
			
			// Clean up all bot data to prevent conflicts
			storeData(%clientId, "SpawnBotInfo", "");
			storeData(%clientId, "SpawnTime", "");
			storeData(%clientId, "BotInfoAiName", "");
			storeData(%clientId, "botTeam", "");
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

