// putting a global variable in the argument list means:
// if an argument is passed for that parameter it gets
// assigned to the global scope, not the scope of the function
function dbecho(){}

function pecho(%m)
{
	echo(String::getSubStr(%m, 0, 250));
}

function createTrainingServer()
{
	dbecho($dbechoMode, "createTrainingServer()");

	$SinglePlayer = true;
	createServer($pref::lastTrainingMission, false);
}

function remoteSetCLInfo(%clientId, %skin, %name, %email, %tribe, %url, %info, %autowp, %enterInv, %msgMask)
{
	dbecho($dbechoMode, "remoteSetCLInfo(" @ %clientId @ ", " @ %skin @ ", " @ %name @ ", " @ %email @ ", " @ %tribe @ ", " @ %url @ ", " @ %info @ ", " @ %autowp @ ", " @ %enterInv @ ", " @ %msgMask @ ")");

   $Client::info[%clientId, 0] = %skin;
   $Client::info[%clientId, 1] = %name;
   $Client::info[%clientId, 2] = %email;
   $Client::info[%clientId, 3] = %tribe;
   $Client::info[%clientId, 4] = %url;
   $Client::info[%clientId, 5] = %info;
   if(%autowp)
      %clientId.autoWaypoint = true;
   if(%enterInv)
      %clientId.noEnterInventory = true;
   if(%msgMask != "")
      %clientId.messageFilter = %msgMask;
}

function Server::storeData()
{
	dbecho($dbechoMode, "Server::storeData()");

   $ServerDataFile = "serverTempData" @ $Server::Port @ ".cs";

   // Temporarily save Info and Connect to restore after export
   // (we don't want to export these as they should come from ServerPrefs.cs)
   %savedInfo = $Server::Info;
   %savedConnect = $Server::Connect;
   
   // Clear these variables so they don't get exported
   $Server::Info = "";
   $Server::Connect = "";
   
   export("Server::*", "temp\\" @ $ServerDataFile, False);
   // CRITICAL: Use Server::LastMission instead of pref::lastMission to avoid triggering pref::* wildcard export
   // This prevents the engine from trying to export all client-side pref::* variables on the server
   if($pref::lastMission != "")
   {
       $Server::LastMission = $pref::lastMission;
       export("Server::LastMission", "temp\\" @ $ServerDataFile, true);
   }
   
   // Restore the values
   $Server::Info = %savedInfo;
   $Server::Connect = %savedConnect;
   
   EvalSearchPath();
}

function Server::refreshData()
{
	dbecho($dbechoMode, "Server::refreshData()");

   $ServerDataFile = "serverTempData" @ $Server::Port @ ".cs";
   
   // Delete the old temp file before loading to prevent stale data conflicts
   if(isFile("temp\\" @ $ServerDataFile))
   {
      File::delete("temp\\" @ $ServerDataFile);
      echo("Cleared old serverTempData before reload to prevent loading conflicts");
   }
   
   // Only load the temp file if it exists and we're explicitly refreshing
   // Note: Since we just deleted it above, this will only work if storeData() was called first
   if(isFile("temp\\" @ $ServerDataFile))
   {
      exec($ServerDataFile);  // reload prefs.
      
      // CRITICAL: Sync Server::LastMission back to pref::lastMission for compatibility
      // Server::LastMission is exported instead of pref::lastMission to avoid triggering pref::* wildcard export
      if($Server::LastMission != "" && $Server::LastMission != -1)
      {
          $pref::lastMission = $Server::LastMission;
      }
      
      // Restore $Server::Info with correct dynamic value (since we don't export it anymore)
      if($rpgver != "")
         $Server::Info = "Running RPG Mod ver " @ $rpgver @ "\nThis version of RPGMod created by Asnabel,\n Resurrected by Superfat/Jobo.";
   }
   
   checkMasterTranslation();
   // Use Server::LastMission as fallback if pref::lastMission is empty
   %missionToLoad = $pref::lastMission;
   if(%missionToLoad == "" && $Server::LastMission != "")
       %missionToLoad = $Server::LastMission;
   Server::loadMission(%missionToLoad, false);
}

function KickDaJackal(%clientId)
{
	dbecho($dbechoMode, "KickDaJackal(" @ %clientId @ ")");

//   Net::kick(%clientId, "The FBI has been notified.  You better buy a legit copy before they get to your house.");
}

function createServer(%mission, %dedicated)
{
	dbecho($dbechoMode2, "createServer(" @ %mission @ ", " @ %dedicated @ ")");

	// Patch server netcode to enable extended functionality and version checking
	// This makes the server incompatible with standard 1.31 clients
	// COMMENTED OUT: To maintain compatibility with pre-1.31 clients
	//patchServerNetcode();
	
	// Set custom version rejection message for old clients
	// This message is shown when clients with incompatible versions try to connect
	// COMMENTED OUT: Requires patchServerNetcode() to be called first
	//setNetCodeRejectMessage("You're running an old Tribes RPG version - Download the newest version at TribesRPG.org to join in on the fun! NOTE: Remove -autojoin Worlds from the target field in the preferences of the Tribes RPG shortcut it places on your desktop.");

	// Clear old serverTempData file at startup to prevent loading conflicts
	$ServerDataFile = "serverTempData" @ $Server::Port @ ".cs";
	if(isFile("temp\\" @ $ServerDataFile))
	{
		File::delete("temp\\" @ $ServerDataFile);
		echo("Cleared old serverTempData at server startup");
	}

	deleteVariables("tmpBotGroup*");
	deleteVariables("aidirectiveTable*");
	deleteVariables("aiNumTable*");
	deleteVariables("tmpbotn*");
	deleteVariables("funk*");
	deleteVariables("Skill*");
	deleteVariables("world*");
	deleteVariables("Quest*");
	deleteVariables("loot*");
	deleteVariables("BotInfo*");
	deleteVariables("Merchant*");
	deleteVariables("NameForRace*");
	deleteVariables("BlockData*");
	deleteVariables("EventCommand*");
	deleteVariables("LoadOut*");
	$PetList = "";
	$DISlist = "";
	$SpawnPackList = "";
	$LoadOutList = "";
	$isRaining = "";

	$loadingMission = false;
	$ME::Loaded = false;
	if(%mission == "")
	{
		%mission = $pref::lastMission;
		// Fallback to Server::LastMission if pref::lastMission is empty
		if(%mission == "" && $Server::LastMission != "")
			%mission = $Server::LastMission;
	}

	if(%mission == "")
	{
		echo("Error: no mission provided.");
		return "False";
	}

	if(!$SinglePlayer)
		$pref::lastMission = %mission;

	//display the "loading" screen (only on client, not dedicated server)
	// CRITICAL: Only execute GUI calls if not dedicated server AND we're in single player or client mode
	// Check both $dedicated and $SinglePlayer to ensure we're on a client
	if(!$dedicated && ($SinglePlayer || nameToID("MainWindow") != -1))
	{
		// Additional safety check: verify MainWindow exists before calling GUI functions
		if(nameToID("MainWindow") != -1)
		{
			cursorOn(MainWindow);
			GuiLoadContentCtrl(MainWindow, "gui\\Loading.gui");
			renderCanvas(MainWindow);
		}
	}

	if(!%dedicated)
	{
		deleteServer();
	      purgeResources();
	      newServer();
      	focusServer();
	}
	if($SinglePlayer)
		newObject(serverDelegate, FearCSDelegate, true, "LOOPBACK", $Server::Port);
	else
		newObject(serverDelegate, FearCSDelegate, true, "IP", $Server::Port, "IPX", 	$Server::Port, "LOOPBACK", $Server::Port);
   
	exec(globals);
	// Load Ai.cs (from base\scripts.vol - we can't override it easily)
	exec(Ai);
	exec(rpgfunk);
	exec(skills);
	exec(house);
	exec(rpgarena);
	exec(sleep);
	exec(game);
	exec(admin);
	exec(Marker);
	exec(Trigger);
	exec(zone);
	exec(spells);
	exec(classes);
	exec(party);
	exec(jail);
	exec(NSound);
	exec(Help);
	exec(BaseExpData);
	exec(BaseDebrisData);
	exec(BaseProjData);
	exec(ArmorData);
	exec(Mission);
	exec(Item);
	exec(Accessory);
	exec(weapons);
	exec(armors);
	exec(Crystal);
	exec(Spawn);
	exec(connectivity);
	exec(gameevents);
	exec(shopping);
	exec(weight);
	exec(mana);
	exec(hp);
	exec(rpgstats);
	// rpghud.cs is client-side only (requires PrestoPack) - skip on dedicated servers
	if(!$dedicated)
		exec(rpghud);
	exec(playerdamage);
	exec(playerspawn);
	exec(itemevents);
	exec(Belt);
	exec(economy);
	exec(remote);
	exec(weaponHandling);
	exec(BonusState);
	//exec(depbase);
	exec(ferry);
	exec(Player);
	exec(Vehicle);
	exec(Turret);
	exec(Beacon);
	exec(StaticShape);
	exec(Station);
	exec(Moveable);
	exec(Sensor);
	exec(Mine);
	exec(InteriorLight);
	exec(comchat);
	exec(dtsviewer);
	exec(plugs);
	exec(version);
	exec(hackfix);
	exec(newstuff);
	exec(advertisements);
	exec(TaurikAdmins);
	exec(remortseal);
	exec(DebugInit);
	//exec(backpack); we implemented belt.cs instead of backpack.cs
	
	$Server::Info = "Running RPG Mod ver " @ $rpgver @ "\nThis version of RPGMod created by Asnabel,\n Further development by Jobo & Superfat.";

	// Save server data during initial server creation
	Server::storeData();

	// NOTE!! You must have declared all data blocks BEFORE you call
	// preloadServerDataBlocks.

	preloadServerDataBlocks();

	Server::loadMission( ($missionName = %mission), true );

	//**RPG

	CreateWeaponCyclingTables();

	// LoadWorld() is now called from Server::finishMissionLoad() after mission loads
	// TotalSealValue is now loaded from Server::finishMissionLoad() after mission loads
	
	InitCrystals();
	InitZones();
	InitFerry();
	echo("===== Calling InitTownBots() =====");
	InitTownBots();
	echo("===== InitTownBots() completed, TownBotList: " @ $TownBotList @ " =====");
	if(!$NoSpawn)
	{
		InitSpawnPoints();
		// Start the centralized spawn counter reconciliation loop
		StartSpawnCounterReconciliation();
	}

	// Start periodic lootbag aggregation (merges nearby lootbags to reduce clutter)
	// Uses guard inside StartLootbagAggregation to avoid duplicate schedules
	StartLootbagAggregation(30);
	
	// Start overlevel AFK zone enforcement (low-level zones protection)
	StartAFKZoneEnforcement();
	
	// Schedule periodic validation of spawn point counters to fix drift issues
	// ValidateSpawnPointCounters removed - replaced by ReconcileSpawnCounters()

	if($arenaOn)
	{
		if(!$NoSpawn)
			InitArena();
	}

	GenerateAllWeaponCosts();
	GenerateAllShieldCosts();
	GenerateAllArmorCosts();

	InitObjectives();

	// Initialize debug UDP broadcast (if enabled)
	InitDebugBroadcast();

	//permanent banlist
	//**
	
	// Start double-execution warning spam with persistent heartbeat
	if($ServerPrefs::DoubleExecuted)
	{
		newObject(DoubleExecWarning, SimSet);
		DoubleExecWarning.schedule(500, "checkDouble");
	}

	if(!%dedicated)
	{
		focusClient();

		if($IRC::DisconnectInSim == "")
		{
			$IRC::DisconnectInSim = true;
		}
		if($IRC::DisconnectInSim == true)
		{
			ircDisconnect();
			$IRCConnected = FALSE;
			$IRCJoinedRoom = FALSE;
		}
		// join up to the server
		$Server::Address = "LOOPBACK:" @ $Server::Port;
		$Server::JoinPassword = $Server::Password;
      	connect($Server::Address);
	}

	return "True";
}

function Server::nextMission(%replay)
{
	dbecho($dbechoMode, "Server::nextMission(" @ %replay @ ")");

//THERE! now it won't change mission!!!

//   if(%replay || $Server::TourneyMode)
//      %nextMission = $missionName;
//   else
//      %nextMission = $nextMission[$missionName];
//   echo("Changing to mission ", %nextMission, ".");
//   // give the clients enough time to load up the victory screen
//   Server::loadMission(%nextMission);
}

function remoteCycleMission(%clientId)
{
	dbecho($dbechoMode, "remoteCycleMission(" @ %clientId @ ")");

   if(%clientId.adminLevel >= 4)
   {
      messageAll(0, Client::getName(%playerId) @ " cycled the mission.");
      Server::nextMission();
   }
}

function remoteDataFinished(%clientId)
{
	dbecho($dbechoMode, "remoteDataFinished(" @ %clientId @ ")");

   if(%clientId.dataFinished)
      return;
   %clientId.dataFinished = true;
   Client::setDataFinished(%clientId);
   %clientId.svNoGhost = ""; // clear the data flag
   if($ghosting)
   {
      %clientId.ghostDoneFlag = true; // allow a CGA done from this dude
      startGhosting(%clientId);  // let the ghosting begin!
   }
}

function remoteCGADone(%playerId)
{
	dbecho($dbechoMode, "remoteCGADone(" @ %playerId @ ")");

   if(!%playerId.ghostDoneFlag || !$ghosting)
      return;
   %playerId.ghostDoneFlag = "";

   Game::initialMissionDrop(%playerid);

	if ($cdTrack != "" && Client::getName(%playerId) != "")
		remoteEval (%playerId, setMusic, $cdTrack, $cdPlayMode);
   if(Client::getName(%playerId) != "")
   remoteEval(%playerId, MInfo, $missionName);
}

function Server::loadMission(%missionName, %immed)
{
	dbecho($dbechoMode, "Server::loadMission(" @ %missionName @ ", " @ %immed @ ")");

   if($loadingMission)
      return;

   %missionFile = "missions\\" $+ %missionName $+ ".mis";
   if(File::FindFirst(%missionFile) == "")
   {
      %missionName = $firstMission;
      %missionFile = "missions\\" $+ %missionName $+ ".mis";
      if(File::FindFirst(%missionFile) == "")
      {
         echo("invalid nextMission and firstMission...");
         echo("aborting mission load.");
         return;
      }
   }
   echo("Notfifying players of mission change: ", getNumClients(), " in game");
   for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
   {
      if(Client::getName(%cl) != "")
   {
      Client::setGuiMode(%cl, $GuiModeVictory);
      %cl.guiLock = true;
      %cl.nospawn = true;
      remoteEval(%cl, missionChangeNotify, %missionName);
      }
   }

   $loadingMission = true;
   $missionName = %missionName;
   $missionFile = %missionFile;
   $prevNumTeams = getNumTeams();

   // Only delete MissionGroup and MissionCleanup if they exist (prevents errors if they don't exist yet)
   // This can happen on first server startup or if mission hasn't loaded yet
   if(nameToID("MissionGroup") != -1)
      deleteObject("MissionGroup");
   if(nameToID("MissionCleanup") != -1)
      deleteObject("MissionCleanup");
   deleteObject("ConsoleScheduler");
   resetPlayerManager();
   resetGhostManagers();
   $matchStarted = false;
   $countdownStarted = false;
   $ghosting = false;

   resetSimTime(); // deal with time imprecision

   newObject(ConsoleScheduler, SimConsoleScheduler);
   if(!%immed)
      schedule("Server::finishMissionLoad();", 18);
   else
      Server::finishMissionLoad();      
}

function Server::finishMissionLoad()
{
	dbecho($dbechoMode, "Server::finishMissionLoad()");

   $loadingMission = false;
	$TestMissionType = "";
   // instant off of the manager
   setInstantGroup(0);
   newObject(MissionCleanup, SimGroup);

   exec($missionFile);
   Mission::init();
   
   // Register additional teams (8-11) that aren't in the mission file
   // Teams 0-7 are registered by the mission file, but we need to add teams 8-11 for Seals, Gods, Angels, Admins
   for(%i = 8; %i <= 11; %i++)
   {
      %teamName = $Server::teamName[%i];
      %teamSkin = $Server::teamSkin[%i];
      if(%teamName != "" && %teamName != -1)
      {
         // Check if team already exists (from mission file)
         %existingTeam = nameToID("MissionGroup\\Teams\\team" @ %i);
         if(%existingTeam == -1 || %existingTeam == "")
         {
            // Team doesn't exist - create it
            newObject("Team" @ %i, TeamGroup);
            addToSet("MissionGroup\\Teams", "Team" @ %i);
            echo("INFO: Server::finishMissionLoad() - Registered team " @ %i @ " (" @ %teamName @ ")");
         }
      }
   }
   
   // Load server time (for persistent lootbag timestamps) BEFORE loading world
   LoadServerTime();
   
   // Load world save data (deployables, etc.) AFTER mission file is loaded
   LoadWorld();
   
   // Load house objectives AFTER LoadWorld (so MissionGroup exists and is populated)
   LoadHouseObjectives();
   
   // CRITICAL: Ensure all turrets are on team 1 (enemy of team 0 players) to activate targeting
   // This fixes turrets that were initialized from mission file with wrong team (0 or -1)
   EnsureAllTurretsOnTeam1();
   
   // Activate generators again after LoadHouseObjectives in case any were deactivated
   // This ensures generators power turrets even after world loads
   ActivateAllGenerators();
   
   // Load TotalSealValue from separate file AFTER all other initialization is complete
   // This prevents it from interfering with server startup
   if(isFile("temp\\SealValue.cs"))
   {
      $ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;	//thanks Presto
      exec("SealValue.cs");
      // Ensure the loaded value is numeric (export sometimes saves as string)
      if($TotalSealValue != "")
         $TotalSealValue = $TotalSealValue + 0;
   }
   
   if($prevNumTeams != getNumTeams())
   {
      // loop thru clients and setTeam to -1;
      // CRITICAL: Skip AI bots to prevent resetting their teams
      messageAll(0, "New teamcount - resetting teams.");
      for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
      {
         // Skip bots - they have their teams set explicitly during spawn
         if(Player::isAiControlled(%cl) || isRPGAI(%cl) || IsBotRegistered(%cl))
            continue;
         GameBase::setTeam(%cl, -1);
      }
   }

   $ghosting = true;
   for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
   {
      if(!%cl.svNoGhost)
      {
         %cl.ghostDoneFlag = true;
         startGhosting(%cl);
      }
   }
   if($SinglePlayer)
      Game::startMatch();
   else if($Server::warmupTime && !$Server::TourneyMode)
      Server::Countdown($Server::warmupTime);
   else if(!$Server::TourneyMode)
      Game::startMatch();

   $teamplay = (getNumTeams() != 1);
   purgeResources(true);

   // make sure the match happens within 5-10 hours.
   schedule("Server::CheckMatchStarted();", 3600);
   schedule("Server::nextMission();", 18000);
   
   return "True";
}

function Server::CheckMatchStarted()
{
	dbecho($dbechoMode, "Server::CheckMatchStarted()");

   // if the match hasn't started yet, just reset the map
   // timing issue.
   if(!$matchStarted)
      Server::nextMission(true);
}

function Server::Countdown(%time)
{
	dbecho($dbechoMode, "Server::Countdown(" @ %time @ ")");

   $countdownStarted = true;
   schedule("Game::startMatch();", %time);
   Game::notifyMatchStart(%time);
   schedule("exec(advertisements);",%time);
   if(%time > 30)
      schedule("Game::notifyMatchStart(30);", %time - 30);
   if(%time > 15)
      schedule("Game::notifyMatchStart(15);", %time - 15);
   if(%time > 10)
      schedule("Game::notifyMatchStart(10);", %time - 10);
   if(%time > 5)
      schedule("Game::notifyMatchStart(5);", %time - 5);
}

function Client::setInventoryText(%clientId, %txt)
{
	dbecho($dbechoMode, "Client::setInventoryText(" @ %clientId @ ", " @ %txt @ ")");

	if(Client::getName(%clientId) != "")
	remoteEval(%clientId, "ITXT", %txt);
}

function centerprint(%clientId, %msg, %timeout)
{
	dbecho($dbechoMode, "centerprint(" @ %clientId @ ", " @ %msg @ ", " @ %timeout @ ")");

   if(%timeout == "")
      %timeout = 5;
   if(%timeout == -1)
        %timeout = "";
   if(Client::getName(%clientId) != "")
   remoteEval(%clientId, "CP", %msg, %timeout);
}

// Persistent centerprint - re-displays message periodically to prevent it from being overwritten
// This is useful for stats/info displays that should remain visible even when bottomprint is used (e.g., during combat)
function persistentCenterprint(%clientId, %msg, %duration)
{
	dbecho($dbechoMode, "persistentCenterprint(" @ %clientId @ ", " @ %msg @ ", " @ %duration @ ")");

	if(Client::getName(%clientId) == "")
		return;

	// Default duration is 30 seconds if not specified
	if(%duration == "" || %duration == 0)
		%duration = 30;

	// Store the message for this client so we can re-display it
	$PersistentCenterprint[%clientId] = %msg;
	$PersistentCenterprintDuration[%clientId] = %duration;
	$PersistentCenterprintStartTime[%clientId] = getSimTime();
	$PersistentCenterprintActive[%clientId] = true; // Flag to track if this message is still active

	// Display the message immediately
	centerprint(%clientId, %msg, -1); // Use -1 for infinite timeout, we'll manage it ourselves

	// Schedule periodic refresh every 1.5 seconds to ensure message stays visible
	// This prevents it from being overwritten by bottomprint or other centerprint calls
	// Note: We use a flag-based approach since Tribes doesn't have cancel() function
	schedule("persistentCenterprintRefresh(" @ %clientId @ ");", 1.5);
}

// Internal function to refresh persistent centerprint messages
function persistentCenterprintRefresh(%clientId)
{
	// Check if this message is still active (may have been cleared by a new persistentCenterprint call)
	if(!$PersistentCenterprintActive[%clientId])
		return;

	// Check if client still exists and message should still be displayed
	if(Client::getName(%clientId) == "")
	{
		// Client disconnected, clean up
		$PersistentCenterprint[%clientId] = "";
		$PersistentCenterprintDuration[%clientId] = "";
		$PersistentCenterprintStartTime[%clientId] = "";
		$PersistentCenterprintActive[%clientId] = "";
		return;
	}

	// Check if duration has expired
	%elapsed = getSimTime() - $PersistentCenterprintStartTime[%clientId];
	if(%elapsed >= $PersistentCenterprintDuration[%clientId])
	{
		// Duration expired, clear the message and clean up
		centerprint(%clientId, "", 0);
		$PersistentCenterprint[%clientId] = "";
		$PersistentCenterprintDuration[%clientId] = "";
		$PersistentCenterprintStartTime[%clientId] = "";
		$PersistentCenterprintActive[%clientId] = "";
		return;
	}

	// Re-display the message to keep it visible (this will restore it if bottomprint overwrote it)
	if($PersistentCenterprint[%clientId] != "" && $PersistentCenterprintActive[%clientId])
		centerprint(%clientId, $PersistentCenterprint[%clientId], -1);

	// Schedule next refresh (continue refreshing until duration expires or flag is cleared)
	if($PersistentCenterprintActive[%clientId])
		schedule("persistentCenterprintRefresh(" @ %clientId @ ");", 1.5);
}

// Clear a persistent centerprint message for a client
function clearPersistentCenterprint(%clientId)
{
	if(Client::getName(%clientId) != "")
		centerprint(%clientId, "", 0);
	
	// Set flag to false so any scheduled refreshes will stop
	$PersistentCenterprintActive[%clientId] = false;
	
	// Clean up stored data
	$PersistentCenterprint[%clientId] = "";
	$PersistentCenterprintDuration[%clientId] = "";
	$PersistentCenterprintStartTime[%clientId] = "";
}

function bottomprint(%clientId, %msg, %timeout)
{
	dbecho($dbechoMode, "bottomprint(" @ %clientId @ ", " @ %msg @ ", " @ %timeout @ ")");

   if(%timeout == "")
      %timeout = 5;
   if(%timeout == -1)
        %timeout = "";
   if(Client::getName(%clientId) != "")
   remoteEval(%clientId, "BP", %msg, %timeout);
}

function topprint(%clientId, %msg, %timeout)
{
	dbecho($dbechoMode, "topprint(" @ %clientId @ ", " @ %msg @ ", " @ %timeout @ ")");

   if(%timeout == "")
      %timeout = 5;
   if(%timeout == -1)
        %timeout = "";
   if(Client::getName(%clientId) != "")
   remoteEval(%clientId, "TP", %msg, %timeout);
}

function msg(%msg, %timeout)
{
	dbecho($dbechoMode, "centerprintall(" @ %msg @ ", " @ %timeout @ ")");

   if(%timeout == "")
      %timeout = 10;
   if(%timeout == -1)
        %timeout = "";
   for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
   {
      if(Client::getName(%clientId) != "")
      remoteEval(%clientId, "CP", %msg, %timeout);
   }
}

function centerprintall(%msg, %timeout)
{
	dbecho($dbechoMode, "centerprintall(" @ %msg @ ", " @ %timeout @ ")");

   if(%timeout == "")
      %timeout = 5;
   if(%timeout == -1)
        %timeout = "";
   for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
      remoteEval(%clientId, "CP", %msg, %timeout);
}

function bottomprintall(%msg, %timeout)
{
	dbecho($dbechoMode, "bottomprintall(" @ %msg @ ", " @ %timeout @ ")");

   if(%timeout == "")
      %timeout = 5;
   if(%timeout == -1)
        %timeout = "";
   for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
   {
      if(Client::getName(%clientId) != "")
      remoteEval(%clientId, "BP", %msg, %timeout);
   }
}

function topprintall(%msg, %timeout)
{
	dbecho($dbechoMode, "topprintall(" @ %msg @ ", " @ %timeout @ ")");

   if(%timeout == "")
      %timeout = 5;
   if(%timeout == -1)
        %timeout = "";
   for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
   {
      if(Client::getName(%clientId) != "")
      remoteEval(%clientId, "TP", %msg, %timeout);
   }
}

function green(%message)
{
messageall(3,%message);
}