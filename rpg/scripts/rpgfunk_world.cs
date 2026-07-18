//============================================================================
// rpgfunk_world.cs — split from rpgfunk.cs (scatter split, Phase 2)
// Extracted 2026-07-18 at commit 5118b1b. Mechanical text move, no behavior change.
// world save/load + deployable rehydrators + lootbags + house objectives + weather/sky
// Source line ranges listed in the rpgfunk.cs shell tombstone.
// exec'd by rpgfunk.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
function StartRecord(%clientId)
{
	dbecho($dbechoMode, "StartRecord(" @ %clientId @ ")");

	//clear variables
	$recording[%clientId] = "";
	for(%t=1; $rec::type[%t] != ""; %t++)
		$rec::type[%t] = "";
	$recCount[%clientId]=0;

	$recording[%clientId] = 1;
}
function StopRecord(%clientId, %f)
{
	dbecho($dbechoMode, "StopRecord(" @ %clientId @ ", " @ %f @ ")");

	//%f = String::replace(%f, "\", "\\");
	File::delete(%f);
	export("rec::*", "temp\\" @ %f, false);

	//clear variables
	$recording[%clientId] = "";
	for(%t=1; $rec::type[%t] != ""; %t++)
		$rec::type[%t] = "";
	$recCount[%clientId]=0;
}
function AddObjectToRec(%clientId, %a, %pos, %rot)
{
	dbecho($dbechoMode, "AddObjectToRec(" @ %clientId @ ", " @ %a @ ", " @ %pos @ ", " @ %rot @ ")");

	//%pos: deploy position
	//%rot: player's rotation

	$recCount[%clientId]++;

	if($recCount[%clientId] == 1)
	{
		//this is the first object placed, so use it as a reference object
		$recRefpos[%clientId] = %pos;
		$recRefrot[%clientId] = %rot;
	}
	$rec::type[$recCount[%clientId]] = %a;

	$rec::pos[$recCount[%clientId]] = Vector::sub(%pos, $recRefpos[%clientId]);

	$rec::rot[$recCount[%clientId]] = %rot;
}
function DeployBase(%clientId, %f, %refPos, %refRot)
{
	dbecho($dbechoMode, "DeployBase(" @ %clientId @ ", " @ %f @ ", " @ %refPos @ ", " @ %refRot @ ")");

	//%refPos: deploy position
	//%refRot: player's rotation

	for(%t=1; $rec::type[%t] != ""; %t++)
		$rec::type[%t] = "";

	$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;	//thanks Presto
	exec(%f);
	
	$baseIndex++;
	for(%i = 1; $rec::type[%i] != ""; %i++)
	{
		if(%i == 1)
		{
			%newpos = %refPos;
			%newrot = $rec::rot[%i];
		}
		else
		{
			%a = Vector::add(%refPos, $rec::pos[%i]);

			%newpos = %a;
			%newrot = $rec::rot[%i];
		}

		if($rec::type[%i] == 1)
		{
			%a = DepPlatSmallHorz;
		}
		else if($rec::type[%i] == 2)
		{
			%a = DepPlatMediumHorz;
		}
		else if($rec::type[%i] == 3)
		{
			%a = DepPlatLargeHorz;
		}
		else if($rec::type[%i] == 4)
		{
			%a = DepPlatSmallVert;
			%newrot = "0 1.5708 " @ GetWord(%newrot, 2) + "1.5708";
			%newpos = GetWord(%newpos, 0) @ " " @ GetWord(%newpos, 1) @ " " @ (GetWord(%newpos, 2) + 2);
		}
		else if($rec::type[%i] == 5)
		{
			%a = DepPlatMediumVert;
			%newrot = "0 1.5708 " @ GetWord(%newrot, 2) + "1.5708";
			%newpos = GetWord(%newpos, 0) @ " " @ GetWord(%newpos, 1) @ " " @ (GetWord(%newpos, 2) + 3);
		}
		else if($rec::type[%i] == 6)
		{
			%a = DepPlatLargeVert;
			%newrot = "0 1.5708 " @ GetWord(%newrot, 2) + "1.5708";
			%newpos = GetWord(%newpos, 0) @ " " @ GetWord(%newpos, 1) @ " " @ (GetWord(%newpos, 2) + 4.5);
		}
		else if($rec::type[%i] == 7)
		{
			%a = StaticDoorForceField;
		}

		%depbase = newObject("","StaticShape",%a,true);
		addToSet("MissionCleanup", %depbase);
		GameBase::setTeam(%depbase, GameBase::getTeam(%clientId));
		GameBase::setPosition(%depbase, %newpos);
		GameBase::setRotation(%depbase, %newrot);
		GameBase::startFadeIn(%depbase);

		$owner[%depbase] = Client::getName(%clientId);
	}

	Client::sendMessage(%clientId,0,"Base deployed");
}

// Queue world saves so frequent gameplay events coalesce into minimal disk writes.
// Modes:
//   "deployables" => SaveWorldDeployables() only (lootbags/deployables crash safety)
//   "full"        => SaveWorld() (includes deployables + seal value + house objectives)
function RequestWorldSave(%reason, %delay, %mode)
{
	if(%reason == "")
		%reason = "unspecified";
	
	if(%delay == "" || %delay < 0)
		%delay = 0;
	
	%isFullSave = true;
	if(%mode == "deployables")
		%isFullSave = false;
	
	if(%isFullSave)
	{
		$WorldSavePendingFull = true;
		$WorldSavePendingDeployables = "";
	}
	else if(!$WorldSavePendingFull)
	{
		$WorldSavePendingDeployables = true;
	}
	
	$WorldSaveLastReason = %reason;
	%runAt = getSimTime() + %delay;
	
	%shouldSchedule = false;
	%existingRunAt = $WorldSaveNextRunAt;
	if($WorldSaveScheduled == "")
	{
		%shouldSchedule = true;
	}
	else if(%existingRunAt == "" || %runAt < %existingRunAt)
	{
		%shouldSchedule = true;
	}
	
	if(%shouldSchedule)
	{
		$WorldSaveScheduled = true;
		$WorldSaveNextRunAt = %runAt;
		$WorldSaveScheduleToken++;
		%token = $WorldSaveScheduleToken;
		schedule("ProcessWorldSaveQueue(" @ %token @ ");", %delay);
	}
}

function ProcessWorldSaveQueue(%token)
{
	// Token guard: ignore stale scheduled callbacks.
	if(%token != "" && %token != $WorldSaveScheduleToken)
		return;
	
	%now = getSimTime();
	%runAt = $WorldSaveNextRunAt;
	if(%runAt != "" && %now < %runAt)
	{
		%remaining = %runAt - %now;
		if(%remaining < 0)
			%remaining = 0;
		
		$WorldSaveScheduleToken++;
		%nextToken = $WorldSaveScheduleToken;
		schedule("ProcessWorldSaveQueue(" @ %nextToken @ ");", %remaining);
		return;
	}
	
	$WorldSaveScheduled = "";
	$WorldSaveNextRunAt = "";
	
	if(!$WorldSavePendingFull && !$WorldSavePendingDeployables)
		return;
	
	if($WorldSaveInProgress)
	{
		$WorldSaveScheduleToken++;
		%busyToken = $WorldSaveScheduleToken;
		$WorldSaveScheduled = true;
		$WorldSaveNextRunAt = getSimTime() + 0.1;
		schedule("ProcessWorldSaveQueue(" @ %busyToken @ ");", 0.1);
		return;
	}
	
	$WorldSaveInProgress = true;
	
	if($WorldSavePendingFull)
	{
		$WorldSavePendingFull = "";
		$WorldSavePendingDeployables = "";
		SaveWorld();
	}
	else
	{
		$WorldSavePendingDeployables = "";
		SaveWorldDeployables();
	}
	
	$WorldSaveInProgress = "";
	
	// If requests arrived while saving, run one more pass soon.
	if($WorldSavePendingFull || $WorldSavePendingDeployables)
	{
		// Respect any run time already requested while the save was in progress.
		if($WorldSaveScheduled == "")
		{
			%followDelay = 0.1;
			%followRunAt = $WorldSaveNextRunAt;
			if(%followRunAt != "" && %followRunAt > getSimTime())
				%followDelay = %followRunAt - getSimTime();
			else
				%followRunAt = getSimTime() + %followDelay;
			
			$WorldSaveScheduleToken++;
			%followToken = $WorldSaveScheduleToken;
			$WorldSaveScheduled = true;
			$WorldSaveNextRunAt = %followRunAt;
			schedule("ProcessWorldSaveQueue(" @ %followToken @ ");", %followDelay);
		}
	}
}

function SaveWorldScoutVehicle(%objID)
{
    if(%objID == -1 || %objID == "" || !isObject(%objID))
        return;
    if(GameBase::getDataName(%objID) != Scout)
        return;
    if(String::findSubStr($ScoutWorldSaveSeen, "|" @ %objID @ "|") != -1)
        return;

    $ScoutWorldSaveSeen = $ScoutWorldSaveSeen @ "|" @ %objID @ "| ";

    %ownerName = $owner[%objID];
    if(%ownerName == "")
        return;

    %isBotName = HasEnemyBotNamePrefix(%ownerName);
    if(!%isBotName)
    {
        %ownerClientId = $TempClientNameCache[%ownerName];
        if(%ownerClientId == "") %ownerClientId = -1;

        if(%ownerClientId != -1 && isRPGAI(%ownerClientId))
            %isBotName = true;
    }

    if(%isBotName)
        return;

    $ScoutWorldSaveIndex++;
    $world::object[$ScoutWorldSaveIndex] = "Scout";
    $world::owner[$ScoutWorldSaveIndex] = %ownerName;
    $world::pos[$ScoutWorldSaveIndex] = GameBase::getPosition(%objID);
    $world::rot[$ScoutWorldSaveIndex] = GameBase::getRotation(%objID);
    $world::team[$ScoutWorldSaveIndex] = GameBase::getTeam(%objID);
    $world::special[$ScoutWorldSaveIndex] = "";
}

function SaveWorldScoutVehicleGroup(%groupId)
{
    if(%groupId == -1 || %groupId == "" || !isObject(%groupId))
        return;

    %objCount = Group::objectCount(%groupId);
    for(%j = 0; %j < %objCount; %j++)
    {
        %objID = Group::getObject(%groupId, %j);
        if(%objID == -1 || %objID == "" || !isObject(%objID))
            continue;

        SaveWorldScoutVehicle(%objID);

        %objectType = getObjectType(%objID);
        if(%objectType == "SimGroup" || %objectType == "SimSet")
            SaveWorldScoutVehicleGroup(%objID);
    }
}

function SaveWorldDeployables() {
    dbecho($dbechoMode, "SaveWorldDeployables()");
    
    // OPTIMIZATION: Cache connected client names to prevent O(N*M) lookups
    deleteVariables("$TempClientNameCache*");
    for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
    {
        %clName = Client::getName(%cl);
        if(%clName != "" && %clName != -1)
            $TempClientNameCache[%clName] = %cl;
    }
    
    // Clear all old world data to prevent stale entries from being saved
    // Clear up to 1000 entries (should be more than enough)
    for(%clearIdx = 1; %clearIdx <= 1000; %clearIdx++)
    {
        $world::object[%clearIdx] = "";
        $world::owner[%clearIdx] = "";
        $world::pos[%clearIdx] = "";
        $world::rot[%clearIdx] = "";
        $world::team[%clearIdx] = "";
        $world::special[%clearIdx] = "";
    }
    $world::objectCount = "";
    
    %i = 0;
    %ii = 0;
    %othercnt = 0;
    %lootbagCount = 0;
    %deployableCount = 0;
    
    // Optimized: Scan LootbagGroup for lootbags (safer and much faster than MissionCleanup)
    // CRITICAL FIX: Use isObject() instead of nameToID() for more reliable group lookup
    %currentTime = GetPersistentTime(); // Use persistent time instead of getSimTime()
    %maxAge = 86400; // 24 hours in seconds
    %expiredCount = 0;
    
    // Unified Scan: Check BOTH LootbagGroup and MissionCleanup to ensure no newly dropped lootbags are missed.
    // This resolves the bug where SaveWorld() missed items dropped between AggregateLootbags() runs.
    %groupsToScan = "";
    if(isObject("LootbagGroup")) %groupsToScan = %groupsToScan @ nameToID("LootbagGroup") @ " ";
    if(isObject("MissionCleanup")) %groupsToScan = %groupsToScan @ nameToID("MissionCleanup") @ " ";
    
    %processedLootbags = ""; // Track IDs to avoid duplicates if an object exists in both sets
    
    for(%g = 0; (%groupId = GetWord(%groupsToScan, %g)) != -1; %g++)
    {
        %objCount = Group::objectCount(%groupId);
        for(%j = 0; %j < %objCount; %j++)
        {
            %objID = Group::getObject(%groupId, %j);
            
            // Skip if already processed or invalid
            if(String::findSubStr(%processedLootbags, "|" @ %objID @ "|") != -1 || %objID == -1)
                continue;
            %processedLootbags = %processedLootbags @ "|" @ %objID @ "| ";

            %obj = GameBase::getDataName(%objID);
            
            // Skip if object doesn't exist or is invalid
            if(%obj == "" || %objID == -1)
            {
                continue;
            }
            
            // If scanning MissionCleanup, only process Lootbag items
            // If scanning LootbagGroup, all items should be lootbags
            if(%obj == "Lootbag")
            {
                // Skip lootbags that have been picked up/deleted (empty loot string)
                // This prevents deleted lootbags from being saved to worldsave
                if($loot[%objID] == "")
                {
                    continue;
                }
                
                // 24h Cleanup: DISABLED per user request (logic removed)
                // Merging logic is sufficient to keep counts low.
                
                // CRITICAL: Skip lootbags owned by bots (enemy or town bots)
                // Extract owner name from loot string (first word is owner name)
                %loot = $loot[%objID];
                %ownerName = getWord(%loot, 0);
                if(%ownerName != "")
                {
                    // Use centralized HasEnemyBotNamePrefix() from Ai.cs for consistent bot detection
                    // This ensures all enemy races are properly filtered from world saves
                    %isBotName = HasEnemyBotNamePrefix(%ownerName);
                    
                    // Also check if owner is currently a bot (in case bot still exists)
                    if(!%isBotName)
                    {
                        // Optimized: Use cache instead of NEWgetClientByName
                        %ownerClientId = $TempClientNameCache[%ownerName];
                        if(%ownerClientId == "") %ownerClientId = -1;
                        
                        if(%ownerClientId != -1 && isRPGAI(%ownerClientId))
                        {
                            %isBotName = true;
                        }
                    }
                    
                    if(%isBotName)
                    {
                        // This lootbag is owned by a bot - skip it (bots shouldn't have persistent lootbags)
                        continue;
                    }
                }
                
                %ii++;
                %lootbagCount++;
                $world::object[%ii] = %obj;
                $world::owner[%ii] = $owner[%objID];
                $world::pos[%ii] = GameBase::getPosition(%objID);
                $world::rot[%ii] = GameBase::getRotation(%objID);
                $world::team[%ii] = GameBase::getTeam(%objID);
                
                %w0 = getWord(%loot, 0);
                %w1 = getWord(%loot, 1);
                if (%w1 != "*")
                    %loot = %w0 @ " * " @ String::getSubStr(%loot, String::len(%w0)+String::len(%w1)+2, 99999);
                $world::special[%ii] = %loot;
            }
            else if(%obj == "Scout")
            {
                %ownerName = $owner[%objID];
                if(%ownerName == "")
                    continue;

                %isBotName = HasEnemyBotNamePrefix(%ownerName);
                if(!%isBotName)
                {
                    %ownerClientId = $TempClientNameCache[%ownerName];
                    if(%ownerClientId == "") %ownerClientId = -1;

                    if(%ownerClientId != -1 && isRPGAI(%ownerClientId))
                        %isBotName = true;
                }

                if(%isBotName)
                    continue;

                %ii++;
                $world::object[%ii] = %obj;
                $world::owner[%ii] = %ownerName;
                $world::pos[%ii] = GameBase::getPosition(%objID);
                $world::rot[%ii] = GameBase::getRotation(%objID);
                $world::team[%ii] = GameBase::getTeam(%objID);
                $world::special[%ii] = "";
            }
        }
    }

    // Scouts are usually nested inside MissionCleanup\Vehicle<clientId>, so scan recursively.
    $ScoutWorldSaveIndex = %ii;
    $ScoutWorldSaveSeen = %processedLootbags;
    if(isObject("MissionCleanup"))
        SaveWorldScoutVehicleGroup(nameToID("MissionCleanup"));
    %ii = $ScoutWorldSaveIndex;
    %processedLootbags = $ScoutWorldSaveSeen;
    $ScoutWorldSaveIndex = "";
    $ScoutWorldSaveSeen = "";
    
    // Then scan sequential IDs for other deployables (platforms, force fields, trees, etc.)
    while (%othercnt < 15) {
        %i++;
        %ID = 8361 + %i;
        %obj = GameBase::getDataName(%ID);
        if(String::findSubStr(%processedLootbags, "|" @ %ID @ "|") != -1)
            continue;
        if (String::findSubStr($WorldSaveList, "|" @ %obj @ "|") != -1 || %obj == "Scout") {
            // Skip lootbags here since we already handled them from MissionCleanup
            if(%obj != "Lootbag")
            {
                // CRITICAL: Skip deployables owned by bots (enemy or town bots)
                %ownerName = $owner[%ID];
                if(%obj == "Scout" && %ownerName == "")
                    continue;
                if(%ownerName != "")
                {
                    // Use centralized HasEnemyBotNamePrefix() from Ai.cs for consistent bot detection
                    // This ensures all enemy races are properly filtered from world saves
                    %isBotName = HasEnemyBotNamePrefix(%ownerName);
                    
                    // Also check if owner is currently a bot (in case bot still exists)
                    if(!%isBotName)
                    {
                        // Optimized: Use cache instead of NEWgetClientByName
                        %ownerClientId = $TempClientNameCache[%ownerName];
                        if(%ownerClientId == "") %ownerClientId = -1;
                        
                        if(%ownerClientId != -1 && isRPGAI(%ownerClientId))
                        {
                            %isBotName = true;
                        }
                    }
                    
                    if(%isBotName)
                    {
                        // This deployable is owned by a bot - skip it (bots shouldn't have persistent deployables)
                        continue;
                    }
                }
                
                %ii++;
                %deployableCount++;
                $world::object[%ii] = %obj;
                $world::owner[%ii] = $owner[%ID];
                $world::pos[%ii] = GameBase::getPosition(%ID);
                $world::rot[%ii] = GameBase::getRotation(%ID);
                $world::team[%ii] = GameBase::getTeam(%ID);
                $world::special[%ii] = "";
            }
        }
        if (%obj == "")
            %othercnt++;
        else
            %othercnt = 0;
    }

    // Set the final object count
    $world::objectCount = %ii;
    
    File::delete("temp\\" @ $missionName @ "_worldsave_.cs");
    
    // Always create the worldsave file, even if no deployables found
    if(%ii == 0) {
        export("world::objectCount", "temp\\" @ $missionName @ "_worldsave_.cs", false);
    } else {
        export("world::objectCount", "temp\\" @ $missionName @ "_worldsave_.cs", false);
        export("world::*", "temp\\" @ $missionName @ "_worldsave_.cs", true);
    }
    
    // Save server time so timestamps persist across restarts
    SaveServerTime();
    
    // Cleanup cache
    deleteVariables("$TempClientNameCache*");
}

function SaveWorld() {
    dbecho($dbechoMode, "SaveWorld()");
    echo("Saving world '" @ $missionName @ "_worldsave_.cs'...");

    // NOTE: SaveWorld only saves world objects (deployables, lootbags, house objectives, seal value)
    // It does NOT save character data for players or bots - that's handled by SaveCharacter()
    // Owner names in deployables are just metadata strings, not character data
    
    SaveWorldDeployables();
    
    // Save TotalSealValue to separate file (not from worldsave to avoid AI initialization issues)
    if($TotalSealValue == "" || $TotalSealValue < 20)
        $TotalSealValue = 20;
    
    // Ensure value is numeric (not string) before saving
    $TotalSealValue = $TotalSealValue + 0;
    
    File::delete("temp\\SealValue.cs");
    export("TotalSealValue", "temp\\SealValue.cs", false);
    
    // Save server time so lootbag timestamps persist across restarts
    SaveServerTime();
    
    // Save house objective captures (base control and flag commands)
    SaveHouseObjectives();
    echo("House objectives saved.");
}

function LoadWorld() {
    dbecho($dbechoMode, "LoadWorld()");
    %filename = $missionName @ "_worldsave_.cs";
    if (isFile("temp\\" @ %filename)) {
        echo("Loading world '" @ $missionName @ "_worldsave_.cs'...");
        messageAll(2, "LoadWorld in progress...");
        
        // NOTE: LoadWorld only loads world objects (deployables, lootbags, house objectives, seal value)
        // It does NOT load character data for players or bots - that's handled by LoadCharacter()
        // Owner names in deployables are just metadata strings, not character data
        
        $ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;
        $LoadingWorldSave = true;
        exec(%filename);
        $LoadingWorldSave = false;

        // Restore house objective captures (base control and flag commands)
        LoadHouseObjectives();
        echo("House objectives restored.");

        for (%i = 1; $world::object[%i] != ""; %i++) {
            if ($world::object[%i] == "DepPlatSmallHorz" ||
                $world::object[%i] == "DepPlatMediumHorz" ||
                $world::object[%i] == "DepPlatLargeHorz" ||
                $world::object[%i] == "DepPlatSmallVert" ||
                $world::object[%i] == "DepPlatMediumVert" ||
                $world::object[%i] == "DepPlatLargeVert") {
                // WORLD-SAVE FIX: LargeHorz/LargeVert are produced by DeployBase but were
                // missing from this reload chain, so saved Large platforms never rehydrated.
                // DeployPlatform spawns %plattype generically; Vert pos/rot offsets are already
                // baked into the saved transform (applied in DeployBase), so no re-offset here.
                DeployPlatform($world::owner[%i], $world::team[%i], $world::pos[%i], $world::rot[%i], $world::object[%i]);
            } else if ($world::object[%i] == "StaticDoorForceField") {
                DeployForceField($world::owner[%i], $world::team[%i], $world::pos[%i], $world::rot[%i]);
            } else if ($world::object[%i] == "DeployableTree") {
                DeployTree($world::owner[%i], $world::team[%i], $world::pos[%i], $world::rot[%i]);
            } else if ($world::object[%i] == "Lootbag") {
                DeployLootbag($world::pos[%i], $world::rot[%i], $world::special[%i]);
            } else if ($world::object[%i] == "Scout") {
                DeployScoutVehicle($world::owner[%i], $world::team[%i], $world::pos[%i], $world::rot[%i]);
            }
        }

        messageAll(2, "LoadWorld complete.");
    } else {
        echo("ERROR: Couldn't find world '" @ $missionName @ "_worldsave_.cs'");
    }
}
function DeployPlatform(%name, %team, %pos, %rot, %plattype)
{
	dbecho($dbechoMode, "DeployPlatform(" @ %name @ ", " @ %team @ ", " @ %pos @ ", " @ %rot @ ", " @ %plattype @ ")");

	%platform = newObject("", "StaticShape", %plattype, true);

	$owner[%platform] = %name;

	if($recording[getClientByName(%name)] == 1)
		AddObjectToRec(getClientByName(%name), 1, %pos, %rot);

//	if(%plattype == "DepPlatSmallVert")
//	{
//		%rot = "0 1.5708 " @ GetWord(%rot, 2) + "1.5708";
//		%pos = GetWord(%pos, 0) @ " " @ GetWord(%pos, 1) @ " " @ (GetWord(%pos, 2) + 2);
//	}
//	else if(%plattype == "DepPlatMediumVert")
//	{
//		%rot = "0 1.5708 " @ GetWord(%rot, 2) + "1.5708";
//		%pos = GetWord(%pos, 0) @ " " @ GetWord(%pos, 1) @ " " @ (GetWord(%pos, 2) + 3);
//	}
//	else if(%plattype == "DepPlatLargeVert")
//	{
//		%rot = "0 1.5708 " @ GetWord(%rot, 2) + "1.5708";
//		%pos = GetWord(%pos, 0) @ " " @ GetWord(%pos, 1) @ " " @ (GetWord(%pos, 2) + 4.5);
//	}

	addToSet("MissionCleanup", %platform);
	GameBase::setTeam(%platform, %team);
	GameBase::setPosition(%platform, %pos);
	GameBase::setRotation(%platform, %rot);
	Gamebase::setMapName(%platform, %plattype);
	GameBase::startFadeIn(%platform);
	playSound(SoundPickupBackpack, %pos);
	playSound(ForceFieldOpen, %pos);
}

// WORLD-SAVE FIX: LoadWorld (above) calls DeployForceField for saved "StaticDoorForceField"
// objects, but the function was never defined -- re-enabling force-field persistence would
// have crashed the LoadWorld loop on an undefined-function call. Mirrors DeployPlatform /
// RecreateForceField (staticshape.cs:795). $owner MUST be set: StaticDoorForceField::onCollision
// (staticshape.cs:758) reads $owner[%this] for its owner+grouplist access gate.
function DeployForceField(%name, %team, %pos, %rot)
{
	dbecho($dbechoMode, "DeployForceField(" @ %name @ ", " @ %team @ ", " @ %pos @ ", " @ %rot @ ")");

	%fField = newObject("", "StaticShape", StaticDoorForceField, true);

	$owner[%fField] = %name;

	addToSet("MissionCleanup", %fField);
	GameBase::setTeam(%fField, %team);
	GameBase::setPosition(%fField, %pos);
	GameBase::setRotation(%fField, %rot);
	GameBase::setMapName(%fField, "StaticDoorForceField");
	GameBase::startFadeIn(%fField);
	playSound(ForceFieldOpen, %pos);

	return %fField;
}

// WORLD-SAVE FIX: LoadWorld dispatches this for a saved datablock literally named
// "DeployableTree", which is NOT a defined StaticShapeData (real tree shapes are
// TreeShape/TreeShapeTwo/PhantomStrangerTree1-3, staticshape.cs:594+), and no code path
// currently saves a tree -- so this branch is presently unreachable. The function was still
// undefined, a latent crash if a real tree-deploy+save feature is ever added. Spawn a valid
// default tree so this rehydrator can never fault the LoadWorld loop. When a real tree-build
// feature lands (see HOUSING_SYSTEM_DESIGN.md), pass/store the actual datablock name instead.
function DeployTree(%name, %team, %pos, %rot)
{
	dbecho($dbechoMode, "DeployTree(" @ %name @ ", " @ %team @ ", " @ %pos @ ", " @ %rot @ ")");

	%tree = newObject("", "StaticShape", TreeShape, true);

	$owner[%tree] = %name;

	addToSet("MissionCleanup", %tree);
	GameBase::setTeam(%tree, %team);
	GameBase::setPosition(%tree, %pos);
	GameBase::setRotation(%tree, %rot);
	GameBase::setMapName(%tree, "TreeShape");
	GameBase::startFadeIn(%tree);

	return %tree;
}

function DeployLootbag(%pos, %rot, %special)
{
	dbecho($dbechoMode, "DeployLootbag(" @ %pos @ ", " @ %rot @ ", " @ %special @ ")");

	%lootbag = newObject("", "Item", "Lootbag", 1, false);

	$loot[%lootbag] = %special;

 	addToSet("MissionCleanup", %lootbag);
	
	GameBase::setPosition(%lootbag, %pos);
	GameBase::setRotation(%lootbag, %rot);
	GameBase::setMapName(%lootbag, "Backpack");

	return %lootbag;
}

function DeployScoutVehicle(%ownerName, %team, %pos, %rot)
{
	dbecho($dbechoMode, "DeployScoutVehicle(" @ %ownerName @ ", " @ %team @ ", " @ %pos @ ", " @ %rot @ ")");

	if(%ownerName == "" || %ownerName == -1)
		return -1;

	%ownerClient = NEWgetClientByName(%ownerName);
	%group = -1;
	if(%ownerClient != -1 && %ownerClient != "")
	{
		%group = nameToId("MissionCleanup\\Vehicle" @ %ownerClient);
		if(%group == -1)
		{
			%group = newObject("Vehicle" @ %ownerClient, SimGroup);
			addToSet("MissionCleanup", %group);
		}
	}

	%scout = newObject("Flyer", "Flier", "Scout", true);
	if(%group != -1)
		addToSet(%group, %scout);
	else
		addToSet("MissionCleanup", %scout);

	GameBase::setTeam(%scout, %team);
	GameBase::setPosition(%scout, %pos);
	GameBase::setRotation(%scout, %rot);
	$owner[%scout] = %ownerName;
	$ScoutVehicleActiveByOwnerName[%ownerName] = %scout;

	if(%ownerClient != -1 && %ownerClient != "")
	{
		$ScoutVehicleActive[%ownerClient] = %scout;
		$ScoutVehicleOwner[%scout] = %ownerClient;
	}

	return %scout;
}

//=============================================================================
// LOOTBAG AGGREGATION SYSTEM
// Periodically merges nearby lootbags to prevent lootbag spam from bot kills
// This runs on $LootbagAggregateInterval (30s) and merges lootbags within $LootbagAggregateRadius (10 units)
//=============================================================================

$LootbagAggregateRadius = 10;      // Distance within which lootbags are merged
$LootbagAggregateInterval = 30;    // Interval in seconds (30 = every 30 seconds)

// Helper: determine if a lootbag owner name belongs to a bot (enemy or town)
function IsLootOwnerBot(%ownerName)
{
	if(%ownerName == "" || %ownerName == -1)
		return false;

	// If a player save exists with this name, treat as player
	if(isFile("temp\\" @ %ownerName @ ".cs"))
		return false;

	// Town bot registry
	%townBotId = $TownBotSpawned[%ownerName];
	if(%townBotId != "" && %townBotId != -1)
	{
		if(isRPGAI(%townBotId))
			return true;
	}

	// Enemy bot registry list
	for(%i = 0; (%cid = GetWord($BotRegistryList, %i)) != -1; %i++)
	{
		%botDisplay = Client::getName(%cid);
		%botAiName = fetchData(%cid, "BotInfoAiName");

		if(%botAiName != "" && String::ICompare(%botAiName, %ownerName) == 0)
			return true;

		if(%botDisplay != "" && String::ICompare(%botDisplay, %ownerName) == 0)
		{
			if(isRPGAI(%cid))
				return true;
		}
	}

	return false;
}

function AggregateLootbags()
{
	Watchdog_Enter("AggregateLootbags");
	dbecho($dbechoMode, "AggregateLootbags()");
	
	// Safety limit to prevent freeze from processing too many lootbags in one pass
	// With O(n²) distance checks, 50 bags = 2500 iterations, 100 bags = 10000 iterations
	%maxLootbagsPerPass = 30;
	
	%totalMerged = 0;
	%totalLootbags = 0;
	
	// First, collect all lootbag objects
	%lootbagList = "";
	%lootbagCount = 0;
	%processedObjects = ""; // Track objects we've already processed to avoid duplicates
	
	// CRITICAL FIX: Use isObject() instead of nameToID() for more reliable group lookup
	// Scan LootbagGroup first (if it exists)
	if(isObject("LootbagGroup"))
	{
		%group = nameToID("LootbagGroup");
		%count = Group::objectCount(%group);
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] LootbagGroup found with " @ %count @ " objects");
		
		for(%i = 0; %i < %count; %i++)
		{
			%obj = Group::getObject(%group, %i);
			
			// Basic existence check
			if(!isObject(%obj)) continue;
			
			// Track this object as processed
			%processedObjects = %processedObjects @ %obj @ " ";
			
			// CRITICAL SAFEGUARD: Skip Player objects that somehow got into LootbagGroup
			// This should never happen but protects against accidental player object registration
			%objType = getObjectType(%obj);
			if(%objType == "Player")
			{
				echo("CRITICAL WARNING: AggregateLootbags found Player object " @ %obj @ " in LootbagGroup! Skipping to prevent player destruction.");
				continue;
			}
			
			%lootData = $loot[%obj];
			if(%lootData == "" || %lootData == -1)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip (empty loot) obj=" @ %obj);
				continue;
			}

			// Parse owner/namelist to distinguish player-owned packs
			%ownerName = GetWord(%lootData, 0);
			%namelist = GetWord(%lootData, 1);

			// Determine if player-owned (bots are allowed)
			%isPlayerOwned = false;
			%isBotOwner = IsLootOwnerBot(%ownerName);

			if(!%isBotOwner)
			{
				if(%ownerName != "" && %ownerName != "*" && %ownerName != "COINS")
					%isPlayerOwned = true;
				else if(%ownerName == "*" && %namelist != "" && %namelist != "*")
					%isPlayerOwned = true;
			}

			if(%isPlayerOwned)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip player pack obj=" @ %obj @ " owner=" @ %ownerName);
				continue;
			}

			// Include bot/neutral lootbag
			%mapName = GameBase::getMapName(%obj);
			if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Include bot pack obj=" @ %obj @ " owner=" @ %ownerName @ " botOwner=" @ %isBotOwner @ " map=" @ %mapName @ " loot='" @ %lootData @ "' (from LootbagGroup)");
			%lootbagList = %lootbagList @ %obj @ " ";
			%lootbagCount++;
		}
	}
	else
	{
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] LootbagGroup does not exist, creating it");
		newObject("LootbagGroup", SimGroup, true);
	}
	
	// ALWAYS scan MissionCleanup (not just as fallback) to catch lootbags that weren't added to LootbagGroup
	// This handles cases where Lootbag::onAdd() failed or lootbags weren't added to LootbagGroup
	if(isObject("MissionCleanup"))
	{
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Scanning MissionCleanup for additional lootbags (LootbagGroup had " @ %lootbagCount @ " lootbags)");
		%missionGroup = nameToID("MissionCleanup");
		%missionCount = Group::objectCount(%missionGroup);
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] MissionCleanup has " @ %missionCount @ " objects");
		
		for(%i = 0; %i < %missionCount; %i++)
		{
			%obj = Group::getObject(%missionGroup, %i);
			
			// Basic existence check
			if(!isObject(%obj)) continue;
			
			// Skip if already processed from LootbagGroup
			if(String::findSubStr(%processedObjects, %obj @ " ") != -1)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip (already processed from LootbagGroup) obj=" @ %obj);
				continue;
			}
			
			// Check if it's a lootbag (Item with mapName "Backpack" or has $loot data)
			%objType = getObjectType(%obj);
			%mapName = GameBase::getMapName(%obj);
			%lootData = $loot[%obj];
			
			// Skip if not a lootbag (not an Item, or not Backpack mapName, or no loot data)
			if(%objType != "Item" || (%mapName != "Backpack" && %lootData == ""))
				continue;
			
			// Skip if empty loot
			if(%lootData == "" || %lootData == -1)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip (empty loot) obj=" @ %obj);
				continue;
			}
			
			// Track this object as processed
			%processedObjects = %processedObjects @ %obj @ " ";
			
			// Try to add to LootbagGroup for future runs
			if(isObject("LootbagGroup"))
				addToSet(LootbagGroup, %obj);

			// Parse owner/namelist to distinguish player-owned packs
			%ownerName = GetWord(%lootData, 0);
			%namelist = GetWord(%lootData, 1);

			// Determine if player-owned (bots are allowed)
			%isPlayerOwned = false;
			%isBotOwner = IsLootOwnerBot(%ownerName);

			if(!%isBotOwner)
			{
				if(%ownerName != "" && %ownerName != "*" && %ownerName != "COINS")
					%isPlayerOwned = true;
				else if(%ownerName == "*" && %namelist != "" && %namelist != "*")
					%isPlayerOwned = true;
			}

			if(%isPlayerOwned)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip player pack obj=" @ %obj @ " owner=" @ %ownerName);
				continue;
			}

			// Include bot/neutral lootbag
			if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Include bot pack obj=" @ %obj @ " owner=" @ %ownerName @ " botOwner=" @ %isBotOwner @ " map=" @ %mapName @ " loot='" @ %lootData @ "' (from MissionCleanup)");
			%lootbagList = %lootbagList @ %obj @ " ";
			%lootbagCount++;
		}
	}
	
	%totalLootbags = %lootbagCount;
	
	// If less than 2 lootbags, nothing to merge
	if(%lootbagCount < 2)
	{
		// Schedule next run (log for visibility even when no merge happens)
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE] Run complete - lootbags found: " @ %lootbagCount @ " (no merge needed)");
		schedule("AggregateLootbags();", $LootbagAggregateInterval);
		return;
	}
	
	// Safety limit: If too many lootbags, only process N at a time to avoid O(n²) freeze
	// Use a rotating offset so different lootbags get processed each pass
	if(%lootbagCount > %maxLootbagsPerPass)
	{
		// Initialize offset on first use
		if($LootbagAggregateOffset == "" || $LootbagAggregateOffset == -1)
			$LootbagAggregateOffset = 0;
		
		// Build a windowed list starting from offset
		%windowedList = "";
		%windowedCount = 0;
		%originalCount = %lootbagCount;
		
		// Start from offset, wrap around if needed
		for(%w = 0; %w < %maxLootbagsPerPass && %w < %originalCount; %w++)
		{
			%idx = ($LootbagAggregateOffset + %w) % %originalCount;
			%bag = GetWord(%lootbagList, %idx);
			if(%bag != "" && %bag != -1)
			{
				%windowedList = %windowedList @ %bag @ " ";
				%windowedCount++;
			}
		}
		
		// Advance offset for next pass (rotate through all bags)
		$LootbagAggregateOffset = ($LootbagAggregateOffset + %maxLootbagsPerPass) % %originalCount;
		
		echo("[LOOTBAG AGGREGATE] " @ %originalCount @ " lootbags found, processing window of " @ %windowedCount @ " (offset=" @ ($LootbagAggregateOffset - %maxLootbagsPerPass) @ ")");
		
		%lootbagList = %windowedList;
		%lootbagCount = %windowedCount;
	}
	
	// Track which lootbags have been merged (to skip them in future iterations)
	%merged = "";
	
	// For each lootbag, find nearby lootbags and merge them
	for(%i = 0; %i < %lootbagCount; %i++)
	{
		%bag1 = GetWord(%lootbagList, %i);
		if(%bag1 == -1 || %bag1 == "")
			continue;
		
		// Skip if already merged into another bag
		if(String::findSubStr(%merged, %bag1 @ " ") != -1)
			continue;
		
		%pos1 = GameBase::getPosition(%bag1);
		if(%pos1 == "" || %pos1 == -1)
		{
			if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip bag1=" @ %bag1 @ " - invalid position: '" @ %pos1 @ "'");
			continue;
		}
		
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Checking bag1=" @ %bag1 @ " at position " @ %pos1);
		
		// Find all nearby lootbags to merge into this one
		for(%j = %i + 1; %j < %lootbagCount; %j++)
		{
			%bag2 = GetWord(%lootbagList, %j);
			if(%bag2 == -1 || %bag2 == "")
				continue;
			
			// Skip if already merged
			if(String::findSubStr(%merged, %bag2 @ " ") != -1)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip bag2=" @ %bag2 @ " - already merged");
				continue;
			}
			
			%pos2 = GameBase::getPosition(%bag2);
			if(%pos2 == "" || %pos2 == -1)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Skip bag2=" @ %bag2 @ " - invalid position: '" @ %pos2 @ "'");
				continue;
			}
			
			// Check distance
			%dist = Vector::getDistance(%pos1, %pos2);
			if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Distance check: bag1=" @ %bag1 @ " bag2=" @ %bag2 @ " dist=" @ %dist @ " radius=" @ $LootbagAggregateRadius);
			
			if(%dist <= $LootbagAggregateRadius)
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Attempting merge: bag1=" @ %bag1 @ " bag2=" @ %bag2 @ " (dist=" @ %dist @ " <= radius=" @ $LootbagAggregateRadius @ ")");
				// Merge bag2 into bag1
				%result = MergeLootbags(%bag1, %bag2);
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Merge result: bag1=" @ %bag1 @ " bag2=" @ %bag2 @ " result=" @ %result);
				if(%result)
				{
					%merged = %merged @ %bag2 @ " ";
					%totalMerged++;
					if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Merge successful: bag2=" @ %bag2 @ " merged into bag1=" @ %bag1);
				}
				else
				{
					if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Merge failed: bag1=" @ %bag1 @ " bag2=" @ %bag2);
				}
			}
			else
			{
				if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Distance too far: bag1=" @ %bag1 @ " bag2=" @ %bag2 @ " dist=" @ %dist @ " > radius=" @ $LootbagAggregateRadius);
			}
		}
	}
	
	if(%totalMerged > 0)
	{
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE] Merged " @ %totalMerged @ " lootbags (from " @ %totalLootbags @ " total)");
	}
	else
	{
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE] Run complete - lootbags found: " @ %totalLootbags @ " (no merge performed)");
	}

	// After aggregation, schedule a deployable-only world save to persist merged lootbags
	// Use a short delay (30s) and a guard to avoid stacking schedules
	if($LootbagAggregateSaveScheduled == "")
	{
		$LootbagAggregateSaveScheduled = true;
		schedule("RequestWorldSave(\"lootbag_aggregate\", 0, \"deployables\"); $LootbagAggregateSaveScheduled = \"\";", 5);
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE] Scheduled SaveWorldDeployables in 5s after aggregation");
	}
	
	// Schedule next run
	schedule("AggregateLootbags();", $LootbagAggregateInterval);
}

// Merge the contents of bag2 into bag1, then delete bag2 if fully merged
function MergeLootbags(%bag1, %bag2)
{
	Watchdog_Enter("MergeLootbags");
	dbecho($dbechoMode, "MergeLootbags(" @ %bag1 @ ", " @ %bag2 @ ")");
	
	// CRITICAL SAFEGUARD: Never delete Player objects
	// First validate objects exist before checking type
	if(%bag1 == -1 || %bag1 == "" || !isObject(%bag1))
	{
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] ERROR: bag1 is invalid (" @ %bag1 @ ") - ABORTING");
		return false;
	}
	if(%bag2 == -1 || %bag2 == "" || !isObject(%bag2))
	{
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] ERROR: bag2 is invalid (" @ %bag2 @ ") - ABORTING");
		return false;
	}
	
	%bag1Type = getObjectType(%bag1);
	%bag2Type = getObjectType(%bag2);
	if(%bag1Type == "Player" || %bag2Type == "Player")
	{
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] CRITICAL ERROR: Attempted to merge Player objects! bag1=" @ %bag1 @ " (type=" @ %bag1Type @ "), bag2=" @ %bag2 @ " (type=" @ %bag2Type @ ") - ABORTING");
		return false;
	}
	
	// Get contents of both bags
	%loot1 = $loot[%bag1];
	%loot2 = $loot[%bag2];
	if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Before merge: bag1=" @ %bag1 @ " loot='" @ %loot1 @ "' | bag2=" @ %bag2 @ " loot='" @ %loot2 @ "'");
	
	if(%loot2 == "" || %loot2 == -1)
	{
		// bag2 is empty, use safe delete wrapper
		$loot[%bag2] = "";
		SafeDeleteLootbag(%bag2);
		return true;
	}
	
	// Parse bag1 contents
	// Format: "OwnerName NameList COINS X Item1 Y Item2 Z ..."
	%owner1 = GetWord(%loot1, 0);
	%namelist1 = GetWord(%loot1, 1);
	%contents1 = String::getSubStr(%loot1, String::len(%owner1) + String::len(%namelist1) + 2, 99999);
	
	// Parse bag2 contents
	%owner2 = GetWord(%loot2, 0);
	%namelist2 = GetWord(%loot2, 1);
	%contents2 = String::getSubStr(%loot2, String::len(%owner2) + String::len(%namelist2) + 2, 99999);
	
	// Merge the item lists (supports partial merges)
	// Returns: "MergedContents | RemainingContents"
	%mergeResult = MergeLootContents(%contents1, %contents2);
	
	// Split result
	%splitPos = String::findSubStr(%mergeResult, "|");
	if(%splitPos == -1)
	{
		// Should not happen with new function, but fallback
		%mergedContents = %mergeResult;
		%remainingContents = "";
	}
	else
	{
		%mergedContents = String::getSubStr(%mergeResult, 0, %splitPos);
		%remainingContents = String::getSubStr(%mergeResult, %splitPos + 1, 99999);
	}
	
	%mergedContents = Trim(%mergedContents);
	%remainingContents = Trim(%remainingContents);
	
	// Merge namelists (combine who can pick up)
	%mergedNamelist = MergeNamelists(%namelist1, %namelist2);
	
	// Use the older/primary bag's owner, but with merged namelist
	%newLoot1 = %owner1 @ " " @ %mergedNamelist @ " " @ %mergedContents;
	
	// Update bag1 with merged contents
	$loot[%bag1] = %newLoot1;
	if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] After merge: bag1=" @ %bag1 @ " loot='" @ $loot[%bag1] @ "'");
	
	if(%remainingContents == "")
	{
		// Full merge success - delete bag2
		// Full merge success - safe delete bag2
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Full merge successful. Deleting bag2=" @ %bag2);
		$loot[%bag2] = "";
		SafeDeleteLootbag(%bag2);
		return true;
	}
	else
	{
		// Partial merge - update bag2 with leftovers
		// Keep original owner/namelist for bag2
		%newLoot2 = %owner2 @ " " @ %namelist2 @ " " @ %remainingContents;
		$loot[%bag2] = %newLoot2;
		if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE DEBUG] Partial merge. Updated bag2=" @ %bag2 @ " leftovers='" @ $loot[%bag2] @ "'");
		return true; // Return true as we successfully merged *something* (or tried)
	}
}

// Merge two loot content strings with partial merge support
// Returns "MergedString|RemainingString"
function MergeLootContents(%contents1, %contents2)
{
	dbecho($dbechoMode, "MergeLootContents(" @ %contents1 @ ", " @ %contents2 @ ")");
	
	// Parse contents1 into an associative array
	%itemCount = 0;
	
	// Parse contents1
	for(%i = 0; GetWord(%contents1, %i) != -1; %i += 2)
	{
		%item = GetWord(%contents1, %i);
		%count = GetWord(%contents1, %i + 1);
		
		if(%item == -1 || %item == "" || %count == -1 || %count == "")
			continue;
		
		%count = %count * 1; // Convert to number
		if(%count <= 0)
			continue;
			
		%tmpItem[%itemCount] = %item;
		%tmpCount[%itemCount] = %count;
		%itemCount++;
	}
	
	// Store initial state string for length checking
	%currentString = %contents1;
	
	// Parse contents2 into a list to process
	%srcItemCount = 0;
	for(%i = 0; GetWord(%contents2, %i) != -1; %i += 2)
	{
		%item = GetWord(%contents2, %i);
		%count = GetWord(%contents2, %i + 1);
		
		if(%item == -1 || %item == "" || %count == -1 || %count == "")
			continue;
			
		%count = %count * 1;
		if(%count <= 0)
			continue;
			
		%srcItem[%srcItemCount] = %item;
		%srcCount[%srcItemCount] = %count;
		%srcItemCount++;
	}
	
	%remainingString = "";
	
	// Process source items one by one
	for(%k = 0; %k < %srcItemCount; %k++)
	{
		%addItem = %srcItem[%k];
		%addCount = %srcCount[%k];
		
		// Attempt to add this item to our internal list
		%found = false;
		%newString = "";
		
		// 1. Update internal array first (simulate the add)
		%addedToIndex = -1;
		%originalCountAtIdx = 0;
		
		for(%j = 0; %j < %itemCount; %j++)
		{
			if(%tmpItem[%j] == %addItem)
			{
				%originalCountAtIdx = %tmpCount[%j];
				%tmpCount[%j] = %tmpCount[%j] + %addCount;
				%addedToIndex = %j;
				%found = true;
				break;
			}
		}
		
		if(!%found)
		{
			%tmpItem[%itemCount] = %addItem;
			%tmpCount[%itemCount] = %addCount;
			%addedToIndex = %itemCount;
			%itemCount++;
		}
		
		// 2. Generate the string
		for(%i = 0; %i < %itemCount; %i++)
		{
			if(%tmpItem[%i] != "" && %tmpCount[%i] > 0)
			{
				%newString = %newString @ %tmpItem[%i] @ " " @ %tmpCount[%i] @ " ";
			}
		}
		%newString = Trim(%newString);
		
		// 3. Check length (Limit is 255, keep safe buffer ~240)
		if(String::len(%newString) > 240)
		{
			// Too long! Revert this item
			if(%found)
			{
				%tmpCount[%addedToIndex] = %originalCountAtIdx; // Restore original count
			}
			else
			{
				%itemCount--; // Remove the new item
				%tmpItem[%addedToIndex] = "";
				%tmpCount[%addedToIndex] = "";
			}
			
			// Add to remaining string
			%remainingString = %remainingString @ %addItem @ " " @ %addCount @ " ";
		}
		else
		{
			// Fits! Keep it.
			%currentString = %newString;
		}
	}
	
	%remainingString = Trim(%remainingString);
	
	return %currentString @ "|" @ %remainingString;
}

// Merge two namelists (who can pick up the lootbag)
function MergeNamelists(%namelist1, %namelist2)
{
	// If either is "*" (anyone can pick up), result is "*"
	if(%namelist1 == "*" || %namelist2 == "*")
		return "*";
	
	// If same, return as-is
	if(%namelist1 == %namelist2)
		return %namelist1;
	
	// Combine the two lists (comma-separated)
	// For simplicity, if they're different players, allow both
	return %namelist1 @ "," @ %namelist2;
}

// Start the lootbag aggregation system after server initialization.
// Use a guard to avoid duplicate schedules.
function StartLootbagAggregation(%initialDelay)
{
	if($LootbagAggregateStarted)
		return;
	if(%initialDelay == "" || %initialDelay < 0)
		%initialDelay = 30; // default 30s after server start
	$LootbagAggregateStarted = true;
	if($LOOTBAG_DEBUG) echo("[LOOTBAG AGGREGATE] Scheduling first run in " @ %initialDelay @ "s (interval " @ $LootbagAggregateInterval @ "s, radius " @ $LootbagAggregateRadius @ ")");
	schedule("AggregateLootbags();", %initialDelay);
}

function ChangeWeather()
{
	dbecho($dbechoMode, "ChangeWeather()");

	//credits go to LabRat for the original code for this... Thanks Lab!
	if(OddsAre(1))
	{
		$isRaining = "";
		$isSnowing = "";

		if(isObject("weather"))
			deleteObject("weather");

		%t = floor(getRandom() * 100);
		if(%t < 50)
		{
			// Clear weather - no precipitation
			// (nothing to create, weather object already deleted)
		}
		else
		{
			// Snow
			$isSnowing = True;
			%weather = newObject("weather", Snowfall, 1, 0, 0, snow);
			messageAll(2, "Weather Report: Snow");
		}
		
		// TODO: Re-enable rain in spring
		// if(%t < 33)
		// {
		// 	// Rain
		// 	$isRaining = True;
		// 	%intensity = getRandom();
		// 	%x = -1 + (getRandom() * 1.5);
		// 	%y = -1 + (getRandom() * 1.5);
		// 	%z = -300 + (floor(getRandom() * 40));
		// 	%vec = %x @ " " @ %y @ " " @ %z;
		// 	%weather = newObject("weather", Snowfall, %intensity, %vec, 0, 1);
		// 	messageAll(2, "Weather Report: Rain");
		// }
	}
}

function TeleportToMarker(%clientId, %markergroup, %testpos, %random)
{
	dbecho($dbechoMode, "TeleportToMarker(" @ %clientId @ ", " @ %markergroup @ ", " @ %testpos @ ", " @ %random @ ")");

	%group = nameToID("MissionGroup\\" @ %markergroup);

	if(%group != -1)
	{	
		%num = Group::objectCount(%group);

		if(%random)
		{
			%r = floor(getRandom() * %num);
		      %marker = Group::getObject(%group, %r);
		
			%worldLoc = GameBase::getPosition(%marker);
			%worldRot = GameBase::getRotation(%marker);
	
			if(%testpos)
			{
				%set = newObject("tempset", SimSet);
				%n = containerBoxFillSet(%set, $SimPlayerObjectType, %worldLoc, 1.0, 1.0, 1.5, getWord(%worldLoc, 2));
				deleteObject(%set);

				// FIX: Check if spot is EMPTY (%n == 0), not occupied
				if(%n == 0)
				{
					GameBase::setPosition(%clientId, %worldLoc);
					GameBase::setRotation(%clientId, %worldRot);
					return %worldLoc;
				}
			}
			else
			{
				GameBase::setPosition(%clientId, %worldLoc);
				GameBase::setRotation(%clientId, %worldRot);
				return %worldLoc;
			}
		}
		else
		{
			for(%i = 0; %i <= %num-1; %i++)
			{
			      %marker = Group::getObject(%group, %i);
			
				%worldLoc = GameBase::getPosition(%marker);
				%worldRot = GameBase::getRotation(%marker);
		
				if(%testpos)
				{
					//this is part of the method SF uses for their teleporters.  thanks Hosed
					%set = newObject("tempset", SimSet);
					%n = containerBoxFillSet(%set, $SimPlayerObjectType, %worldLoc, 1.0, 1.0, 1.5, getWord(%worldLoc, 2));
					deleteObject(%set);

					if(%n == 0)
					{
						GameBase::setPosition(%clientId, %worldLoc);
						GameBase::setRotation(%clientId, %worldRot);
						return %worldLoc;
					}
				}
				else
				{
					GameBase::setPosition(%clientId, %worldLoc);
					GameBase::setRotation(%clientId, %worldRot);
					return %worldLoc;
				}
			}
		}
	}
	
	return False;
}

function TossLootbag(%clientId, %loot, %vel, %namelist, %t, %sourceObj)
{
	dbecho($dbechoMode2, "TossLootbag(" @ %clientId @ ", " @ %loot @ ", " @ %vel @ ", " @ %namelist @ ", " @ %t @ ")");

	// CRITICAL: Validate loot string is not empty before proceeding
	// Check if loot is empty, -1, or just whitespace (check first non-whitespace char)
	if(%loot == "" || %loot == -1 || (GetWord(%loot, 0) == "" && GetWord(%loot, 1) == ""))
	{
		echo("ERROR: TossLootbag - Empty or invalid loot string for clientId " @ %clientId @ " (loot='" @ %loot @ "'), aborting");
		return; // Abort if no loot to drop
	}

	%player = Client::getOwnedObject(%clientId);
	%throwSource = %player;
	if(%sourceObj != "" && %sourceObj != -1 && isObject(%sourceObj) && getObjectType(%sourceObj) == "Player")
		%throwSource = %sourceObj;
	%ownerName = Client::getName(%clientId);

	// DEBUG: Log when enemy bots drop lootbags
	if(isRPGAI(%clientId))
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = %ownerName;

			// Ensure bot lootbags are treated as bot/neutral: use '*' owner/namelist
			%ownerName = "*";
			%namelist = "*";
		}
	}
	else
	{
		// If AI-controlled but not recognized by isRPGAI (early death), still force neutral owner/namelist
		if(Player::isAiControlled(%clientId))
		{
			%ownerName = "*";
			%namelist = "*";
		}
	}

	%lootbag = newObject("", "Item", "Lootbag", 1, false);

	// CRITICAL: Validate lootbag was created successfully before using it
	if(%lootbag == "" || %lootbag == -1 || !isObject(%lootbag))
	{
		echo("ERROR: TossLootbag - Failed to create lootbag object for clientId " @ %clientId @ " (lootbag=" @ %lootbag @ ", loot='" @ %loot @ "')");
		return; // Abort if lootbag creation failed
	}

	// DEBUG: Log successful lootbag creation (use echo so it always prints)
	if($LOOTBAG_DEBUG) echo("[LOOTBAG DEBUG] TossLootbag - Created lootbag " @ %lootbag @ " for clientId " @ %clientId @ " with loot='" @ %loot @ "'");

	if(%t > 0)
		schedule("$loot[" @ %lootbag @ "] = \"" @ %ownerName @ " * " @ %loot @ "\";", %t, %lootbag);
	else
	{
		if($LootbagPopTime != -1)
		{
			// CRITICAL: stamp a pop token so the deferred pop can't hit a recycled ID (item.cs Item::pop)
			%lootbag.popToken = %lootbag @ "_pop_" @ getSimTime();
			schedule("Item::Pop(" @ %lootbag @ ", \"" @ %lootbag.popToken @ "\");", $LootbagPopTime, %lootbag);
			// CRITICAL FIX: Validate client ID exists before using it in scheduled function
			// The bot/player may be deleted by the time this runs, so check first
			schedule("%tempClientId = " @ %clientId @ "; %tempLootbag = " @ %lootbag @ "; if(Client::getName(%tempClientId) != \"\" && Client::getName(%tempClientId) != -1) { storeData(%tempClientId, \"lootbaglist\", RemoveFromCommaList(fetchData(%tempClientId, \"lootbaglist\"), %tempLootbag)); }", $LootbagPopTime, %lootbag);
		}
	}

	%loot = %ownerName @ " " @ %namelist @ " " @ %loot;

	$loot[%lootbag] = %loot;
	// remember the newest bag so rapid drops can merge into it
	// (KronosHUD_Server.cs remoteKShopBeltDrop)
	%clientId.lastLootbag = %lootbag;
	storeData(%clientId, "lootbaglist", AddToCommaList(fetchData(%clientId, "lootbaglist"), %lootbag));

	// CRITICAL: Validate MissionCleanup exists before adding lootbag
	if(!isObject("MissionCleanup"))
	{
		echo("ERROR: TossLootbag - MissionCleanup SimSet does not exist, cannot add lootbag " @ %lootbag @ " for clientId " @ %clientId);
		// MissionCleanup should exist, but if it doesn't, create it
		newObject("MissionCleanup", SimGroup, true);
		echo("ERROR: TossLootbag - Created MissionCleanup SimSet (should have existed at server startup)");
	}

	// CRITICAL: Re-validate lootbag is still valid before adding to MissionCleanup
	if(%lootbag == "" || %lootbag == -1 || !isObject(%lootbag))
	{
		echo("ERROR: TossLootbag - Lootbag object became invalid before addToSet for clientId " @ %clientId @ " (lootbag=" @ %lootbag @ ", loot='" @ %loot @ "')");
		return; // Abort if lootbag is no longer valid
	}

	// DEBUG: Log before addToSet (use echo so it always prints)
	if($LOOTBAG_DEBUG) echo("[LOOTBAG DEBUG] TossLootbag - About to add lootbag " @ %lootbag @ " to MissionCleanup (clientId=" @ %clientId @ ", loot='" @ %loot @ "')");
	if($LOOTBAG_DEBUG) echo("[LOOTBAG DEBUG] TossLootbag - lootbag type check: isObject=" @ isObject(%lootbag) @ ", getObjectType=" @ getObjectType(%lootbag));
	
	addToSet("MissionCleanup", %lootbag);
	
	// Also add to LootbagGroup for optimized saving and aggregation if it exists
	if(isObject("LootbagGroup"))
		addToSet("LootbagGroup", %lootbag);
	
	// DEBUG: Log after addToSet (use echo so it always prints)
	if($LOOTBAG_DEBUG) echo("[LOOTBAG DEBUG] TossLootbag - Successfully added lootbag " @ %lootbag @ " to MissionCleanup");
	GameBase::setMapName(%lootbag, "Backpack");
	if(%throwSource != "" && %throwSource != -1 && isObject(%throwSource) && getObjectType(%throwSource) == "Player")
	{
		GameBase::throw(%lootbag, %throwSource, %vel, false);
	}
	else
	{
		%fallbackPos = "";
		if(%player != "" && %player != -1 && isObject(%player))
			%fallbackPos = GameBase::getPosition(%player);
		if((%fallbackPos == "" || %fallbackPos == -1) && %sourceObj != "" && %sourceObj != -1 && isObject(%sourceObj) && getObjectType(%sourceObj) == "Player")
			%fallbackPos = GameBase::getPosition(%sourceObj);
		if(%fallbackPos == "" || %fallbackPos == -1)
			%fallbackPos = "0 0 0";
		echo("WARNING: TossLootbag - Invalid throw source for clientId=" @ %clientId @ ". Placing lootbag at " @ %fallbackPos);
		GameBase::setPosition(%lootbag, %fallbackPos);
	}

	//Make sure there aren't more than 15 packs per player... This is to resolve lag problems
	%lootbaglist = fetchData(%clientId, "lootbaglist");
	if(CountObjInCommaList(%lootbaglist) > 15)
	{
		%p = String::findSubStr(%lootbaglist, ",");
		%w = String::getSubStr(%lootbaglist, 0, %p);

		// CRITICAL: lootbaglist can hold stale IDs (pickup-time removal fails silently when the
		// owner is offline, itemevents.cs Item::onCollision). Only pop if the object at this ID
		// is still THIS player's lootbag; otherwise the ID was recycled - just drop the list entry.
		if(isObject(%w) && getObjectType(%w) == "Item" && GetWord($loot[%w], 0) == %ownerName)
			Item::Pop(%w);
		storeData(%clientId, "lootbaglist", RemoveFromCommaList(%lootbaglist, %w));
	}

}

function ChangeSky(%sky)
{
	dbecho($dbechoMode, "ChangeSky(" @ %sky @ ")");

	%group = nameToId("MissionGroup\\LandScape");
	if(%group != -1)
	{
		%count = Group::objectCount(%group);
		for(%i = 0; %i <= %count-1; %i++)
		{
			%object = Group::getObject(%group, %i);
			if(getObjectType(%object) == "Sky")
			{
				deleteobject(%object);
			}
		}
	}

	%newsky = newObject(Sky, Sky, 0, 0, 0, %sky, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
	addToSet("MissionGroup\\LandScape", %newsky);
}

function InitObjectives()
{
	dbecho($dbechoMode, "InitObjectives()");

	Team::setObjective(0, 1, "<jc><f2>Welcome To The Kingdom of Kronos RPG!");
	Team::setObjective(0, 2, "");
	Team::setObjective(0, 3, "<jc><f2>For eons, the Kingdom of Kronos was an advanced technological hub.");
	Team::setObjective(0, 4, "<jc><f2>The Krono Stone stood as the fundamental pillar - a divine artifact");
	Team::setObjective(0, 5, "<jc><f2>that stabilized reality, anchoring the simulation in a golden age of peace.");
	Team::setObjective(0, 6, "<jc><f2>But the Stone fell into the wrong hands. The magi-scientists of the");
	Team::setObjective(0, 7, "<jc><f2>Arbal Research Center sought to harness its infinite processing power.");
	Team::setObjective(0, 8, "<jc><f2>In their hubris, they cracked the artifact, unleashing a catastrophic");
	Team::setObjective(0, 9, "<jc><f2>Exception that shattered the timeline and rewrote the laws of physics.");
	Team::setObjective(0, 10, "<jc><f2>Now, the Kingdom is a broken loop. High-tech weaponry devolved into");
	Team::setObjective(0, 11, "<jc><f2>swords and spears. Nano-Tech armor lost to space, replaced with forged metal.");
	Team::setObjective(0, 12, "<jc><f2>Citizens exposed to raw, corrupted data transformed into Pig Men, Ogres,");
	Team::setObjective(0, 13, "<jc><f2>Undead, Minotaurs, Aliens, Demons, Gods, Angels, and Invisible Voids.");
	Team::setObjective(0, 14, "<jc><f2>Even the Admin Bots, once guardians, have gone rogue - desperate to purge all life.");
	Team::setObjective(0, 15, "<jc><f2>Hope lies only in the Remort. Fight your way to the Seal of Kronos,");
	Team::setObjective(0, 16, "<jc><f2>sacrifice your form to be reborn with a Soul resilient to corruption.");
	Team::setObjective(0, 17, "<jc><f2>From the safe haven of Yuliple City, to the chaotic darkness of The Void,");	
	Team::setObjective(0, 18, "<jc><f2>you must master the broken world to restore the Kingdom - or rule its ruins");
	Team::setObjective(0, 19, "");	
	Team::setObjective(0, 20, "");
	Team::setObjective(0, 21, "<jc><f2>----------------------------------------------------------------Hosted and Modified Heavily by Jobo---------------------------------------------------------------------------------");
	Team::setObjective(0, 22, "<jc><f2>Bases / Artifacts / Members Online:");
	Team::setObjective(0, 23, "<jc><f2>  House Yuliple: " @ $BaseControl[HouseYuliple] @ " bases, " @ $FlagCommand[HouseYuliple] @ " artifacts, " @ $HouseMember[HouseYuliple] @ " members online");
	Team::setObjective(0, 24, "<jc><f2>  House Curama: " @ $BaseControl[HouseCurama] @ " bases, " @ $FlagCommand[HouseCurama] @ " artifacts, " @ $HouseMember[HouseCurama] @ " members online");
	Team::setObjective(0, 25, "<jc><f2>  House Arbal: " @ $BaseControl[HouseArbal] @ " bases, " @ $FlagCommand[HouseArbal] @ " artifacts, " @ $HouseMember[HouseArbal] @ " members online");
	Team::setObjective(0, 26, "<jc><f2>  House Kronos: " @ $BaseControl[HouseKronos] @ " bases, " @ $FlagCommand[HouseKronos] @ " artifacts, " @ $HouseMember[HouseKronos] @ " members online");

	for(%i = 1; %i < getNumTeams(); %i++)
	{
		Team::setObjective(%i, 1, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 2, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 3, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 4, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 5, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 6, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 7, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 8, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 9, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 10, "<f7><jc>KILL ALL HUMAN PLAYERS");
		Team::setObjective(%i, 11, "<f7><jc>KILL ALL HUMAN PLAYERS");
	}
	
	// Start recursive refresh of objectives screen every 60 seconds
	RecursiveObjectivesRefresh();
}

function UpdateHouseObjectivesDisplay()
{
	dbecho($dbechoMode, "UpdateHouseObjectivesDisplay()");
	
	// Clamp BaseControl and FlagCommand to prevent negative values
	// This fixes the bug where negative base counts cause negative house bonuses
	for(%i = 0; %i <= 3; %i++)
	{
		%houseNames[0] = "HouseYuliple";
		%houseNames[1] = "HouseCurama";
		%houseNames[2] = "HouseArbal";
		%houseNames[3] = "HouseKronos";
		
		%house = %houseNames[%i];
		if($BaseControl[%house] < 0)
			$BaseControl[%house] = 0;
		if($FlagCommand[%house] < 0)
			$FlagCommand[%house] = 0;
		if($HouseMember[%house] < 0)
			$HouseMember[%house] = 0;
	}
	
	// Update the house objectives display with current BaseControl, FlagCommand, and HouseMember values
	// Update only the house data lines (slots 18-21) - header at slot 17 stays the same
	Team::setobjective(0, 23, "<jc><f2>  House Yuliple: " @ $BaseControl[HouseYuliple] @ " bases, " @ $FlagCommand[HouseYuliple] @ " artifacts, " @ $HouseMember[HouseYuliple] @ " members online");
	Team::setobjective(0, 24, "<jc><f2>  House Curama: " @ $BaseControl[HouseCurama] @ " bases, " @ $FlagCommand[HouseCurama] @ " artifacts, " @ $HouseMember[HouseCurama] @ " members online");
	Team::setobjective(0, 25, "<jc><f2>  House Arbal: " @ $BaseControl[HouseArbal] @ " bases, " @ $FlagCommand[HouseArbal] @ " artifacts, " @ $HouseMember[HouseArbal] @ " members online");
	Team::setobjective(0, 26, "<jc><f2>  House Kronos: " @ $BaseControl[HouseKronos] @ " bases, " @ $FlagCommand[HouseKronos] @ " artifacts, " @ $HouseMember[HouseKronos] @ " members online");
}

function RecursiveObjectivesRefresh()
{
	dbecho($dbechoMode, "RecursiveObjectivesRefresh()");
	
	// Recalculate house objectives from actual game state to ensure accuracy
	RecalcHouseObjectives();
	
	// Schedule next refresh in 60 seconds
	schedule("RecursiveObjectivesRefresh();", 60);
}

function SaveHouseObjectives()
{
	// Save house objective captures to separate file
	File::delete("temp\\HouseObjectives.cs");
	
	// Save base control (towerswitch captures) for each house
	// Export might have issues with array syntax, so save to temp variables first
	// BUGFIX: these were assigned to LOCALS (%baseKronos) while export() writes
	// GLOBALS ($baseKronos) - the exported values were always empty, so the
	// counter round-trip never worked (masked by RecalcHouseObjectives' 60s recompute)
	$baseKronos = $BaseControl[HouseKronos];
	$baseArbal = $BaseControl[HouseArbal];
	$baseCurama = $BaseControl[HouseCurama];
	$baseYuliple = $BaseControl[HouseYuliple];

	$flagKronos = $FlagCommand[HouseKronos];
	$flagArbal = $FlagCommand[HouseArbal];
	$flagCurama = $FlagCommand[HouseCurama];
	$flagYuliple = $FlagCommand[HouseYuliple];
	
	// Export as simple variables (no array syntax)
	export("baseKronos", "temp\\HouseObjectives.cs", false);
	export("baseArbal", "temp\\HouseObjectives.cs", true);
	export("baseCurama", "temp\\HouseObjectives.cs", true);
	export("baseYuliple", "temp\\HouseObjectives.cs", true);
	export("flagKronos", "temp\\HouseObjectives.cs", true);
	export("flagArbal", "temp\\HouseObjectives.cs", true);
	export("flagCurama", "temp\\HouseObjectives.cs", true);
	export("flagYuliple", "temp\\HouseObjectives.cs", true);
	
	// Save flag positions and team ownership
	// Flags are direct children of MissionGroup, TowerSwitches are inside Tower0-Tower3 SimGroups
	%tempSet = nameToID("MissionGroup");
	%flagCount = 0;
	
	for(%i = 0; %i < Group::objectCount(%tempSet); %i++)
	{
		%obj = Group::getObject(%tempSet, %i);
		%dataName = GameBase::getDataName(%obj);
		
		if(%dataName == "flag")
		{
			// Found a flag - save its data
			%objName = %obj.objectiveName;
			%pos = GameBase::getPosition(%obj);
			
			// Use holdingTeam property to get which house holds this flag
			// (all players are on team 0, so GameBase::getTeam() won't tell us the house)
			%holdingTeam = %obj.holdingTeam;
			if(%holdingTeam == "" || %holdingTeam == -1)
				%holdingTeam = "None";
			
			// Store the data for export using numeric index
			$SavedFlag[%flagCount, "name"] = %objName;
			$SavedFlag[%flagCount, "position"] = %pos;
			$SavedFlag[%flagCount, "holdingTeam"] = %holdingTeam;
			
			%flagCount++;
		}
	}
	
	// Save Tower SimGroup teams (Tower0 through Tower3)
	// TowerSwitches are inside these SimGroups, not direct children of MissionGroup
	%switchCount = 0;
	for(%t = 0; %t <= 3; %t++)
	{
		%towerGroup = nameToID("MissionGroup/Tower" @ %t);
		if(%towerGroup != -1)
		{
			// Find the TowerSwitch in this group to get the team and objectiveName
			for(%j = 0; %j < Group::objectCount(%towerGroup); %j++)
			{
				%obj = Group::getObject(%towerGroup, %j);
				%dataName = GameBase::getDataName(%obj);
				if(%dataName == "TowerSwitch")
				{
					%objName = %obj.objectiveName;
					
					// Use .team property to get which house controls this tower
					// (all players are on team 0, so GameBase::getTeam() won't tell us the house)
					%houseTeam = %obj.team;
					if(%houseTeam == "" || %houseTeam == -1)
						%houseTeam = "None";
					
					// Save to both Tower array (for bulk team setting) and Switch array (for individual restoration)
					$SavedTower[%t, "houseTeam"] = %houseTeam;
					$SavedSwitch[%switchCount, "name"] = %objName;
					$SavedSwitch[%switchCount, "houseTeam"] = %houseTeam;
					
					%switchCount++;
					break;
				}
			}
		}
	}
	
	if(%flagCount > 0)
		export("SavedFlag*", "temp\\HouseObjectives.cs", true);
	if(%switchCount > 0)
		export("SavedSwitch*", "temp\\HouseObjectives.cs", true);
	export("SavedTower*", "temp\\HouseObjectives.cs", true);
	
	echo("Saved " @ %flagCount @ " flag positions and " @ %switchCount @ " towerswitch teams.");
}

function LoadHouseObjectives()
{
	// Load house data from save file (includes BaseControl, FlagCommand, and HouseMember)
	HouseData::Load();
	
	// Restore house objective captures from file
	exec("HouseObjectives.cs");
	
	// Restore BaseControl and FlagCommand from temp variables (if they exist)
	// BUGFIX: these were read as LOCALS (%baseKronos) which the exec'd file can't
	// set - the file sets GLOBALS ($baseKronos), so the restore never fired
	if($baseKronos != "")
		$BaseControl[HouseKronos] = $baseKronos;
	if($baseArbal != "")
		$BaseControl[HouseArbal] = $baseArbal;
	if($baseCurama != "")
		$BaseControl[HouseCurama] = $baseCurama;
	if($baseYuliple != "")
		$BaseControl[HouseYuliple] = $baseYuliple;

	if($flagKronos != "")
		$FlagCommand[HouseKronos] = $flagKronos;
	if($flagArbal != "")
		$FlagCommand[HouseArbal] = $flagArbal;
	if($flagCurama != "")
		$FlagCommand[HouseCurama] = $flagCurama;
	if($flagYuliple != "")
		$FlagCommand[HouseYuliple] = $flagYuliple;
	
	// Restore flag positions and team ownership using numeric indices
	%tempSet = nameToID("MissionGroup");
	if(%tempSet == -1)
	{
		echo("ERROR LoadHouseObjectives: MissionGroup not found!");
		return;
	}
	
	%flagsRestored = 0;
	%switchesRestored = 0;
	
	// Restore flags by matching objectiveName with saved data
	// Read the exported variables directly (export converts arrays to underscore format)
	for(%savedIdx = 0; %savedIdx < 10; %savedIdx++)
	{
		// Read exported format: $SavedFlag0_name, $SavedFlag0_holdingTeam, etc.
		%savedName = "";
		%savedPos = "";
		%savedHoldingTeam = "";
		
		if(%savedIdx == 0)
		{
			%savedName = $SavedFlag0_name;
			%savedPos = $SavedFlag0_position;
			%savedHoldingTeam = $SavedFlag0_holdingTeam;
		}
		else if(%savedIdx == 1)
		{
			%savedName = $SavedFlag1_name;
			%savedPos = $SavedFlag1_position;
			%savedHoldingTeam = $SavedFlag1_holdingTeam;
		}
		else if(%savedIdx == 2)
		{
			%savedName = $SavedFlag2_name;
			%savedPos = $SavedFlag2_position;
			%savedHoldingTeam = $SavedFlag2_holdingTeam;
		}
		else if(%savedIdx == 3)
		{
			%savedName = $SavedFlag3_name;
			%savedPos = $SavedFlag3_position;
			%savedHoldingTeam = $SavedFlag3_holdingTeam;
		}
		else if(%savedIdx == 4)
		{
			%savedName = $SavedFlag4_name;
			%savedPos = $SavedFlag4_position;
			%savedHoldingTeam = $SavedFlag4_holdingTeam;
		}
		else if(%savedIdx == 5)
		{
			%savedName = $SavedFlag5_name;
			%savedPos = $SavedFlag5_position;
			%savedHoldingTeam = $SavedFlag5_holdingTeam;
		}
		else if(%savedIdx == 6)
		{
			%savedName = $SavedFlag6_name;
			%savedPos = $SavedFlag6_position;
			%savedHoldingTeam = $SavedFlag6_holdingTeam;
		}
		else if(%savedIdx == 7)
		{
			%savedName = $SavedFlag7_name;
			%savedPos = $SavedFlag7_position;
			%savedHoldingTeam = $SavedFlag7_holdingTeam;
		}
		else if(%savedIdx == 8)
		{
			%savedName = $SavedFlag8_name;
			%savedPos = $SavedFlag8_position;
			%savedHoldingTeam = $SavedFlag8_holdingTeam;
		}
		else if(%savedIdx == 9)
		{
			%savedName = $SavedFlag9_name;
			%savedPos = $SavedFlag9_position;
			%savedHoldingTeam = $SavedFlag9_holdingTeam;
		}
		
		if(%savedName == "")
			break;
		
		if(%savedHoldingTeam == "" || %savedHoldingTeam == -1)
			%savedHoldingTeam = "None";
		
		// Find this flag in MissionGroup
		for(%i = 0; %i < Group::objectCount(%tempSet); %i++)
		{
			%obj = Group::getObject(%tempSet, %i);
			if(GameBase::getDataName(%obj) == "flag" && %obj.objectiveName == %savedName)
			{
				// Restore position
				GameBase::setPosition(%obj, %savedPos);
				
				// Restore which house holds this flag
				if(%savedHoldingTeam != "None" && %savedHoldingTeam != "")
				{
					%obj.holdingTeam = %savedHoldingTeam;
					%obj.team = %savedHoldingTeam;
					
					// Find a flagstand for this house and link the flag to it
					%flagstandFound = false;
					for(%j = 0; %j < Group::objectCount(%tempSet); %j++)
					{
						%standObj = Group::getObject(%tempSet, %j);
						if(GameBase::getDataName(%standObj) == "FlagStand" && %standObj.team == %savedHoldingTeam && %standObj.flag == "")
						{
							// Link flag to flagstand
							%obj.flagStand = %standObj;
							%standObj.flag = %obj;
							%flagstandFound = true;
							break;
						}
					}
					
					// Flags at flagstands should have GameBase::setTeam set to 0
					GameBase::setTeam(%obj, 0);
				}
				else
				{
					%obj.holdingTeam = -1;
					%obj.team = -1;
					%obj.flagStand = "";
					
					// Neutral flags MUST have GameBase::setTeam set to -1 so they can be captured
					GameBase::setTeam(%obj, -1);
				}
				
				echo("Restored flag '" @ %savedName @ "' to position " @ %savedPos @ " held by " @ %savedHoldingTeam);
				%flagsRestored++;
				break;
			}
		}
	}
	
	// Restore Tower SimGroup teams (Tower0 through Tower3)
	// Read the exported variables directly (export converts arrays to underscore format)
	for(%t = 0; %t <= 3; %t++)
	{
		%houseTeam = "";
		%switchName = "";
		
		// Read exported format: $SavedTower0_houseTeam, $SavedSwitch0_name, etc.
		if(%t == 0)
		{
			%houseTeam = $SavedTower0_houseTeam;
			%switchName = $SavedSwitch0_name;
		}
		else if(%t == 1)
		{
			%houseTeam = $SavedTower1_houseTeam;
			%switchName = $SavedSwitch1_name;
		}
		else if(%t == 2)
		{
			%houseTeam = $SavedTower2_houseTeam;
			%switchName = $SavedSwitch2_name;
		}
		else if(%t == 3)
		{
			%houseTeam = $SavedTower3_houseTeam;
			%switchName = $SavedSwitch3_name;
		}
		
		// Always try to restore, even if it's "None" or empty
		%towerGroup = nameToID("MissionGroup/Tower" @ %t);
		if(%towerGroup != -1)
		{
			if(%houseTeam == "" || %houseTeam == -1)
				%houseTeam = "None";
			
			// Set team for all objects in the tower group to 0 (all players are on team 0)
			// But set the .team property on TowerSwitch to the house name
			for(%j = 0; %j < Group::objectCount(%towerGroup); %j++)
			{
				%obj = Group::getObject(%towerGroup, %j);
				GameBase::setTeam(%obj, 0);
				
				// If it's a TowerSwitch, also restore the .team property (house name)
				if(GameBase::getDataName(%obj) == "TowerSwitch")
				{
					if(%houseTeam != "None" && %houseTeam != "")
						%obj.team = %houseTeam;
					else
						%obj.team = "";
				}
			}
			%switchesRestored++;
		}
		else
		{
			echo("WARNING: Tower" @ %t @ " group not found in MissionGroup!");
		}
	}
	
	echo("Restored " @ %flagsRestored @ " flags and " @ %switchesRestored @ " towerswitches.");
	
	// Recalculate house member counts from all currently connected players
	// This ensures the counts match the actual connected players, not just saved values
	RecalcHouseMemberCounts();
	
	// Update the objectives display with the loaded/recalculated values
	// This refreshes the screen that was initialized in InitObjectives() before LoadHouseObjectives() ran
	UpdateHouseObjectivesDisplay();
}

// Recalculate house member counts from all currently connected players
function RecalcHouseMemberCounts()
{
	dbecho($dbechoMode, "RecalcHouseMemberCounts()");
	
	// Reset all house member counts
	$HouseMember[HouseKronos] = 0;
	$HouseMember[HouseArbal] = 0;
	$HouseMember[HouseCurama] = 0;
	$HouseMember[HouseYuliple] = 0;
	
	// Count members from all currently connected clients
	// Use Client::getFirst()/getNext() for reliable iteration
	for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
	{
		if(Client::getOwnedObject(%clientId) != -1)
		{
			%house = fetchData(%clientId, "MyHouse");
			if(%house != "")
			{
				$HouseMember[%house]++;
			}
		}
	}
}

function RecalcHouseObjectives()
{
	dbecho($dbechoMode, "RecalcHouseObjectives()");
	
	%tempSet = nameToID("MissionGroup");
	if(%tempSet == -1)
	{
		echo("ERROR RecalcHouseObjectives: MissionGroup not found!");
		return;
	}
	
	// Recalculate $BaseControl and $FlagCommand from actual flag/tower states
	// This ensures they match the actual game state, not just saved values
	// Reset all counts first
	$BaseControl[HouseKronos] = 0;
	$BaseControl[HouseArbal] = 0;
	$BaseControl[HouseCurama] = 0;
	$BaseControl[HouseYuliple] = 0;
	
	$FlagCommand[HouseKronos] = 0;
	$FlagCommand[HouseArbal] = 0;
	$FlagCommand[HouseCurama] = 0;
	$FlagCommand[HouseYuliple] = 0;
	
	// Count flags held by each house
	for(%i = 0; %i < Group::objectCount(%tempSet); %i++)
	{
		%obj = Group::getObject(%tempSet, %i);
		if(GameBase::getDataName(%obj) == "flag")
		{
			%holdingTeam = %obj.holdingTeam;
			if(%holdingTeam != "" && %holdingTeam != -1 && %holdingTeam != "None")
				$FlagCommand[%holdingTeam]++;
		}
	}
	
	// Count towers controlled by each house
	for(%t = 0; %t <= 3; %t++)
	{
		%towerGroup = nameToID("MissionGroup/Tower" @ %t);
		if(%towerGroup != -1)
		{
			for(%j = 0; %j < Group::objectCount(%towerGroup); %j++)
			{
				%obj = Group::getObject(%towerGroup, %j);
				if(GameBase::getDataName(%obj) == "TowerSwitch")
				{
					%houseTeam = %obj.team;
					if(%houseTeam != "" && %houseTeam != -1 && %houseTeam != "None")
						$BaseControl[%houseTeam]++;
					break;
				}
			}
		}
	}
	
	// Recalculate house member counts from all currently connected players
	RecalcHouseMemberCounts();
	
	// Update the objectives display with the recalculated values
	UpdateHouseObjectivesDisplay();
	
	// Silenced debug output - BaseControl, FlagCommand, and HouseMember values
	// echo("BaseControl: Kronos=" @ $BaseControl[HouseKronos] @ ", Arbal=" @ $BaseControl[HouseArbal] @ ", Curama=" @ $BaseControl[HouseCurama] @ ", Yuliple=" @ $BaseControl[HouseYuliple]);
	// echo("FlagCommand: Kronos=" @ $FlagCommand[HouseKronos] @ ", Arbal=" @ $FlagCommand[HouseArbal] @ ", Curama=" @ $FlagCommand[HouseCurama] @ ", Yuliple=" @ $FlagCommand[HouseYuliple]);
	// echo("HouseMember: Kronos=" @ $HouseMember[HouseKronos] @ ", Arbal=" @ $HouseMember[HouseArbal] @ ", Curama=" @ $HouseMember[HouseCurama] @ ", Yuliple=" @ $HouseMember[HouseYuliple]);
}

// ============================================================
// OBJECT SAFETY ARCHITECTURE
// ============================================================

// Specialized wrapper for Lootbags to ensure we only delete actual lootbags
function SafeDeleteLootbag(%obj)
{
	if(!isObject(%obj)) return;
	
	%type = getObjectType(%obj);
	if(%type == "Player")
	{
		echo("CRITICAL SAFEGUARD: SafeDeleteLootbag called on Player object " @ %obj @ "! ABORTING.");
		return;
	}
	
	%mapName = GameBase::getMapName(%obj);
	if(%mapName != "Backpack" && %mapName != "Lootbag")
	{
		// Not strictly a lootbag by name, but if it's an Item it might be okay.
		// Asking for caution here.
		if(%type != "Item")
		{
			echo("SAFETY WARNING: SafeDeleteLootbag called on non-Item object " @ %obj @ " (Type: " @ %type @ "). Skipping.");
			return;
		}
	}
	
	deleteObject(%obj);
}
