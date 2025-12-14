function InitZones()
{
	dbecho($dbechoMode, "InitZones()");

	$numZones = 0;
	%zcnt = 0;
	%umusiccnt = 0;

	%group = nameToId("MissionGroup\\Zones");

	if(%group != -1)
	{
		%count = Group::objectCount(%group);
		for(%i = 0; %i <= %count-1; %i++)
		{
			%object = Group::getObject(%group, %i);
			%system = Object::getName(%object);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);

			//---------------------------------------------------------------
			//THIS PART GATHERS SOUNDS FOR THE GENERIC UNKNOWN ZONE
			// there is no EXIT sound for the unknown zone.
			//---------------------------------------------------------------
			if(GetWord(%system, 0) == "ENTERSOUND")
			{	
				$Zone::EnterSound[0] = GetWord(%system, 1);
			}
			else if(GetWord(%system, 0) == "AMBIENTSOUND")
			{
				$Zone::AmbientSound[0] = GetWord(%system, 1);
				$Zone::AmbientSoundPerc[0] = GetWord(%system, 2);
			}
			else if(GetWord(%system, 0) == "MUSIC")
			{
				$Zone::Music[0, %umusiccnt++] = GetWord(%system, 1);
				$Zone::MusicTicks[0, %umusiccnt] = GetWord(%system, 2);
			}
			//---------------------------------------------------------------
			else
			{
				%zcnt++;

				%tmpgroup = nameToId("MissionGroup\\Zones\\" @ %system);
				%tmpcount = Group::objectCount(%tmpgroup);
				%marker = "";
				%musiccnt = 0;

				for(%z = 0; %z <= %tmpcount-1; %z++)
				{
					%tmpobject = Group::getObject(%tmpgroup, %z);
	
					if(getObjectType(%tmpobject) == "Marker")
					{
						if(%marker == "")
						{
							%marker = %tmpobject;
							$numZones++;
						}
					}
					else if(getObjectType(%tmpobject) == "SimGroup")
					{
						%n = Object::getName(%tmpobject);
						
						if(GetWord(%n, 0) == "ENTERSOUND")
						{	
							$Zone::EnterSound[%zcnt] = GetWord(%n, 1);
						}
						else if(GetWord(%n, 0) == "AMBIENTSOUND")
						{
							$Zone::AmbientSound[%zcnt] = GetWord(%n, 1);
							$Zone::AmbientSoundPerc[%zcnt] = GetWord(%n, 2);
						}
						else if(GetWord(%n, 0) == "EXITSOUND")
						{
							$Zone::ExitSound[%zcnt] = GetWord(%n, 1);
						}
						else if(GetWord(%n, 0) == "MUSIC")
						{
							$Zone::Music[%zcnt, %musiccnt++] = GetWord(%n, 1);
							$Zone::MusicTicks[%zcnt, %musiccnt] = GetWord(%n, 2);
						}
					}
				}
				
				%mname = Object::getName(%marker);
				$Zone::Marker[%zcnt] = GameBase::getPosition(%marker);
				$Zone::Length[%zcnt] = GetWord(%mname, 0);
				$Zone::Width[%zcnt] = GetWord(%mname, 1);
				$Zone::Height[%zcnt] = GetWord(%mname, 2);
				$Zone::SHeight[%zcnt] = GetWord(%mname, 3);
				$Zone::Type[%zcnt] = %type;
				$Zone::Desc[%zcnt] = %desc;
				$Zone::FolderID[%zcnt] = %tmpgroup;
			}
		}
		echo($numZones @ " zones initialized.");
	}
	
	// Initialize zone tickers before RecursiveZone is called
	$zoneTicker[1] = 0;
	$zoneTicker[2] = 0;
	
	// Initialize teleporter requirements for zones
	InitZoneTeleporterRequirements();
}

function InitZoneTeleporterRequirements()
{
	dbecho($dbechoMode, "InitZoneTeleporterRequirements()");
	
	// Map zones to their teleporter requirements based on teleporter destinations in the mission file
	// Format: $Zone::RemortRequirement["Zone Name"] = remort_level;
	// Format: $Zone::TournyRankRequirement["Zone Name"] = tourny_rank;
	
	// Angels Enigma - requires REMORT 50 (Stone to angel teleporter)
	$Zone::RemortRequirement["Angels Enigma"] = 50;
	
	// Gian Echos - requires REMORT 50 (Angel to Gian Echos teleporter, accessed from Angels Enigma)
	$Zone::RemortRequirement["Gian Echos"] = 50;
	
	// Admin's Demise - requires TOURNYRANK 9 (Yuliple to admins teleporter)
	$Zone::TournyRankRequirement["Admin's Demise"] = 9;
	
	// The Void - requires REMORT 100 (endgame dungeon with invisible enemies)
	$Zone::RemortRequirement["The Void"] = 100;
	
	// Add more zone requirements here as needed based on teleporter NEED lines
}

function RecursiveZone(%delay)
{
	dbecho($dbechoMode, "RecursiveZone(" @ %delay @ ")");

	// Zone tickers should be initialized in InitZones()
	// But add safety check in case they weren't initialized - use temp variables to avoid debug warnings
	%temp1 = $zoneTicker[1];
	if(%temp1 == "")
		%temp1 = 0;
	%temp2 = $zoneTicker[2];
	if(%temp2 == "")
		%temp2 = 0;

	//increment by 1 every $zoneCheckDelay seconds
	%temp1++;
	%temp2++;
	$zoneTicker[1] = %temp1;
	$zoneTicker[2] = %temp2;

	if($zoneTicker[1] >= 1)		//check zone every 2 seconds for players
	{
		DoZoneCheck(2, %delay);
		$zoneTicker[1] = "";
	}
//	if($zoneTicker[2] >= 15)	//check zone every 30 seconds for bots
//	{
//		DoZoneCheck(1, %delay);
//		$zoneTicker[2] = "";
//	}

	schedule("RecursiveZone(" @ %delay @ ");", %delay);
}

function DoZoneCheck(%w, %d)
{
	dbecho($dbechoMode, "DoZoneCheck(" @ %w @ ", " @ %d @ ")");

	//Massive zone check for entire world
	%mset = newObject("set", SimSet);
	%n = containerBoxFillSet(%mset, $SimPlayerObjectType, "0 0 0", 9999, 9999, 9999, 0);

	for(%z = 1; %z <= $numZones; %z++)
	{
		%set = newObject("set", SimSet);
		%n = containerBoxFillSet(%set, $SimPlayerObjectType, $Zone::Marker[%z], $Zone::Length[%z], $Zone::Width[%z], $Zone::Height[%z], $Zone::SHeight[%z]);
		Group::iterateRecursive(%set, setzoneflags, %z);
		deleteObject(%set);
	}
	
	Group::iterateRecursive(%mset, UpdateZone);
	deleteObject(%mset);
	
	// Decrement quest reload timers (only if they exist and are > 0)
	// Initialize temp variables first to prevent debug warnings
	%tempChief = "";
	%tempDK = "";
	%tempBK = "";
	%tempBO = "";
	%tempQueen = "";
	
	// Safely check and update quest reload timers
	// Note: Accessing non-existent array elements in Tribes 1 returns "", which is fine
	%tempChief = $QuestReload[Chief];
	if(%tempChief != "" && %tempChief > 0)
		$QuestReload[Chief] = %tempChief - 1;
	else if(%tempChief == 0)
		$QuestReload[Chief] = "";
	
	%tempDK = $QuestReload[DeathKnight];
	if(%tempDK != "" && %tempDK > 0)
		$QuestReload[DeathKnight] = %tempDK - 1;
	else if(%tempDK == 0)
		$QuestReload[DeathKnight] = "";
	
	%tempBK = $QuestReload[BanishedKing];
	if(%tempBK != "" && %tempBK > 0)
		$QuestReload[BanishedKing] = %tempBK - 1;
	else if(%tempBK == 0)
		$QuestReload[BanishedKing] = "";
	
	%tempBO = $QuestReload[BattleOx];
	if(%tempBO != "" && %tempBO > 0)
		$QuestReload[BattleOx] = %tempBO - 1;
	else if(%tempBO == 0)
		$QuestReload[BattleOx] = "";
	
	%tempQueen = $QuestReload[Queen];
	if(%tempQueen != "" && %tempQueen > 0)
		$QuestReload[Queen] = %tempQueen - 1;
	else if(%tempQueen == 0)
		$QuestReload[Queen] = "";
}
function setzoneflags(%object, %z)
{
	dbecho($dbechoMode, "setzoneflags(" @ %object @ ", " @ %z @ ")");

	// Use helper function to get client ID from Player object (handles enemy bots)
	%clientId = GetClientIdFromPlayerObject(%object);
	
	// If helper function returns -1, try using %object directly as client ID
	if(%clientId == -1 || %clientId == "")
		%clientId = %object;
	
	storeData(%clientId, "tmpzone", %z);
}

function UpdateZone(%object)
{
	dbecho($dbechoMode, "UpdateZone(" @ %object @ ")");

	// Use helper function to get client ID from Player object (handles enemy bots)
	%clientId = GetClientIdFromPlayerObject(%object);
	%usingFallback = false;
	
	// If helper function returns -1, try using %object directly as client ID (fallback)
	if(%clientId == -1 || %clientId == "")
	{
		%clientId = %object;
		%usingFallback = true; // Mark that we're using the Player object as fallback
	}
	
	// CRITICAL: If using fallback and Player object isn't valid, skip zone update (timing issue during spawn)
	// This happens when UpdateZone() is called during bot spawn before Player object is fully registered
	if(%usingFallback && (!isObject(%object) || %object == -1 || %object == ""))
	{
		// Player object not valid yet - this is a timing issue, skip zone update
		// Zone data will be set later when the bot is fully registered
		return;
	}
	
	// PHASE 3 FIX: Validate client ID belongs to correct entity before storing zone data
	// Skip validation if we're using fallback (Player object as client ID) - validation won't work correctly
	// BUT: Only skip if Player object is valid (fallback mode for bots with timing issues)
	if(!%usingFallback && %clientId != -1 && %clientId != "" && isObject(%object))
	{
		// Reverse verification - ensure the client ID actually owns this Player object
		%verifyPlayerObj = Client::getOwnedObject(%clientId);
		if(%verifyPlayerObj != %object && %verifyPlayerObj != -1 && %verifyPlayerObj != "")
		{
			// Client ID doesn't own this Player object - this is a collision!
			echo("ERROR: UpdateZone - Client ID " @ %clientId @ " does not own Player object " @ %object @ ". Player object belongs to client ID with Player object " @ %verifyPlayerObj @ ". Rejecting zone update to prevent data corruption.");
			return; // Reject zone update to prevent storing zone data under wrong client ID
		}
		
		// Entity type validation - verify client ID matches expected entity type
		// PRIORITY: Check bot indicators FIRST (bots can have HasLoadedAndSpawned set, so don't use it for player detection)
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%isBot = ((%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1) || 
		          (%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1) ||
		          Player::isAiControlled(%clientId) || isRPGAI(%clientId));
		
		// Only check for player indicators if NOT a bot (bots can have character files in some edge cases)
		%isPlayer = false;
		if(!%isBot)
		{
			%playerName = Client::getName(%clientId);
			if(%playerName != "" && %playerName != -1)
			{
				%characterFile = "temp\\" @ %playerName @ ".cs";
				if(isFile(%characterFile))
				{
					%isPlayer = true;
				}
			}
			// HasLoadedAndSpawned is NOT a reliable player indicator (bots set it too)
		}
		
		// Only flag collision if we have BOTH bot and player indicators AND they're mutually exclusive
		// Since we prioritize bot detection, this should rarely trigger, but it's a safety check
		if(%isPlayer && %isBot)
		{
			echo("ERROR: UpdateZone - Client ID " @ %clientId @ " has conflicting entity type indicators (both player and bot). Clearing zone data to prevent collision.");
			storeData(%clientId, "zone", "");
			storeData(%clientId, "tmpzone", "");
			$ClientData[%clientId, "zone"] = "";
			$EnemyBotData[%clientId, "zone"] = "";
			$TownBotData[%clientId, "zone"] = "";
			return; // Reject zone update
		}
	}
	
	// Skip AI-controlled clients to prevent [TOWNBOT DEBUG] spam
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId)) return;
	
	// CRITICAL: Skip zone updates for town bots - they stay in their spawn location and don't need zone tracking
	if(%clientId != -1 && %clientId != "")
	{
		%isTownBot = IsTownBot(%clientId);
		if(%isTownBot)
		{
			// Town bots should never trigger zone changes - they stay in their assigned zone
			return;
		}
	}
	
	// Check if player has walked too far from town bots and reset dialogue state
	// This ensures dialogue resets when walking away, not just when speaking while far away
	if(%clientId != -1 && %clientId != "" && !Player::isAiControlled(%clientId))
	{
		%clientPos = GameBase::getPosition(%clientId);
		%maxInteractionDist = $maxAIdistVec + 50;  // Same distance as used in comchat.cs
		%maxDistSq = %maxInteractionDist * %maxInteractionDist;
		
		// OPTIMIZATION: Position caching - skip check if player hasn't moved significantly (works for both inRange and tooFar)
		%lastPos = $LastTownBotCheckPos[%clientId];
		%lastResult = $LastTownBotCheckResult[%clientId];
		
		// If position hasn't changed much, skip entire check (regardless of previous result)
		if(%lastPos != "")
		{
			%distMoved = Vector::getDistance(%lastPos, %clientPos);
			if(%distMoved < 10)  // Player hasn't moved more than 10 units
			{
				// Skip check - state hasn't changed
				return;  // Early return to avoid unnecessary processing
			}
		}
		
		// Player moved significantly or first check - need to recalculate
		%tooFarFromAll = True;
		%previousResult = %lastResult;
		
		if($DebugTownBotDistance)
			echo("[TOWNBOT DEBUG] UpdateZone - Checking distance from townbots for player " @ %clientId);
		
		// Check distance to all town bots
		for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
		{
			%botPos = GameBase::getPosition(%id);
			if(%botPos != "" && %botPos != -1)
			{
				if($DebugTownBotDistance)
					echo("[TOWNBOT DEBUG] UpdateZone - Checking bot " @ %id);
				
				// Quick distance check using squared distance (avoids sqrt calculation)
				%dx = GetWord(%clientPos, 0) - GetWord(%botPos, 0);
				%dy = GetWord(%clientPos, 1) - GetWord(%botPos, 1);
				%dz = GetWord(%clientPos, 2) - GetWord(%botPos, 2);
				%distSq = %dx * %dx + %dy * %dy + %dz * %dz;
				
				if($DebugTownBotDistance)
				{
					%dist = Vector::getDistance(%clientPos, %botPos);
					echo("[TOWNBOT DEBUG] UpdateZone - Bot " @ %id @ " distance: " @ %dist @ " (max: " @ %maxInteractionDist @ ")");
				}
				
				// If player is within range of at least one bot, don't reset
				if(%distSq <= %maxDistSq)
				{
					%tooFarFromAll = False;
					if($DebugTownBotDistance)
						echo("[TOWNBOT DEBUG] UpdateZone - Player within range of bot " @ %id @ " - NOT resetting states");
					break;
				}
			}
			else if($DebugTownBotDistance)
			{
				echo("[TOWNBOT DEBUG] UpdateZone - Bot " @ %id @ " has invalid position, skipping");
			}
		}
		
		// Update cache
		$LastTownBotCheckPos[%clientId] = %clientPos;
		if(%tooFarFromAll)
		{
			$LastTownBotCheckResult[%clientId] = "tooFar";
			// Only reset if state changed from inRange to tooFar (not if already tooFar)
			if(%previousResult == "inRange" || %previousResult == "")
			{
				for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
				{
					$state[%id, %clientId] = "";
				}
			}
		}
		else
		{
			$LastTownBotCheckResult[%clientId] = "inRange";
		}
	}
	
	%zoneflag = fetchData(%clientId, "tmpzone");

	// Handle case where player left a zone (was in a zone, but now not detected in any zone)
	%currentZone = fetchData(%clientId, "zone");
	if(%currentZone != "" && %zoneflag == "")
	{
		// Player was in a zone but is no longer detected in any zone - they left
		if(!Player::isAiControlled(%clientId))
		{
			%oldZoneDesc = Zone::getDesc(%currentZone);
			
			// CRITICAL: Check if player left Colloseum during an active seal battle
			// Only end the battle if ALL participants have left the Colloseum
			if($SealBattleActive == true && %oldZoneDesc == "Colloseum")
			{
				// Check if this player is a participant in the seal battle
				if($SealBattleParticipants != "" && String::findSubStr($SealBattleParticipants, %clientId) >= 0)
				{
					// Check if ALL participants have left the Colloseum
					%allParticipantsLeft = true;
					%list = $SealBattleParticipants;
					
					// Parse comma-separated list of participants
					while(String::len(%list) > 0)
					{
						%commaPos = String::findSubStr(%list, ",");
						if(%commaPos > 0)
						{
							%participantId = String::getSubStr(%list, 0, %commaPos);
							%list = String::getSubStr(%list, %commaPos + 1, 99999);
						}
						else
						{
							%participantId = %list;
							%list = "";
						}
						
						if(%participantId == "" || %participantId == -1)
							continue;
						
						// Check if participant still exists and is in Colloseum
						%participantName = Client::getName(%participantId);
						if(%participantName != "")
						{
							%participantZoneId = fetchData(%participantId, "zone");
							%participantZoneDesc = Zone::getDesc(%participantZoneId);
							if(%participantZoneDesc == "Colloseum")
							{
								// At least one participant is still in Colloseum
								%allParticipantsLeft = false;
								break;
							}
						}
					}
					
					// Only end the battle if ALL participants have left
					if(%allParticipantsLeft)
					{
						%participantNames = SealBattle::GetParticipantNames();
						messageAll(2, "" @ %participantNames @ " have fled the battle to break the seal! All hope is lost...");
						
						// Find the initiator (first participant) to conclude the battle
						%initiatorId = GetWord($SealBattleParticipants, 0);
						if(%initiatorId == "" || %initiatorId == -1)
							%initiatorId = %clientId;  // Fallback to the player who left
						
						SealBattle::Conclude(%initiatorId, false);  // false = failure
						return;  // Exit early - battle has been concluded
					}
				}
			}
			
			%oldZoneIndex = Zone::getIndex(%currentZone);
			if(%oldZoneIndex > 0)
			{
				Zone::DoExit(%oldZoneIndex, %clientId);
				
				%oldCount = $ZonePlayerCount[%oldZoneIndex];
				if(%oldCount > 0)
					$ZonePlayerCount[%oldZoneIndex] = %oldCount - 1;
				
				// If no players left in old zone, despawn bots after 30 seconds
				if($ZonePlayerCount[%oldZoneIndex] <= 0)
				{
					// Cancel any pending spawn (player left before 10s delay expired)
					CancelPendingZoneSpawn(%oldZoneIndex);
					schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
				}
			}
		}
		return;  // Exit early since player is not in any zone
	}
	
	//check if the player was found inside a zone
	if(%zoneflag != "")
	{
		//the player is inside a zone!
	
		//check if the player's current zone matches the one he's detected in
		if(fetchData(%clientId, "zone") != $Zone::FolderID[%zoneflag])
		{
			// Simple rule: If an AI-controlled bot moves from Ghost Town to ANY other zone, kill it
			if(Player::isAiControlled(%clientId))
			{
				%oldZoneId = fetchData(%clientId, "zone");
				%newZoneId = $Zone::FolderID[%zoneflag];
				%oldZoneDesc = Zone::getDesc(%oldZoneId);
				%newZoneDesc = Zone::getDesc(%newZoneId);
				
				// If bot is moving from Ghost Town to any other zone, kill it
				if(%oldZoneDesc == "Ghost Town" && %newZoneDesc != "Ghost Town" && %newZoneDesc != "" && %newZoneDesc != -1)
				{
					// Kill the bot with no lootbag and no exp flags
					storeData(%clientId, "noDropLootbagFlag", True);
					storeData(%clientId, "noExperienceFlag", True);
					Player::Kill(%clientId);
					return; // Exit early since bot is being killed
				}
			}
			
			// CRITICAL: Check if player left Colloseum during an active seal battle
			// This handles zone changes (not just leaving zones completely)
			// Only end the battle if ALL participants have left the Colloseum
			if(!Player::isAiControlled(%clientId) && $SealBattleActive == true)
			{
				%oldZoneId = fetchData(%clientId, "zone");
				%oldZoneDesc = Zone::getDesc(%oldZoneId);
				%newZoneId = $Zone::FolderID[%zoneflag];
				%newZoneDesc = Zone::getDesc(%newZoneId);
				
				// If player was in Colloseum and is now in a different zone, check if all participants have left
				if(%oldZoneDesc == "Colloseum" && %newZoneDesc != "Colloseum" && %newZoneDesc != "" && %newZoneDesc != -1)
				{
					// Check if this player is a participant in the seal battle
					if($SealBattleParticipants != "" && String::findSubStr($SealBattleParticipants, %clientId) >= 0)
					{
						// Check if ALL participants have left the Colloseum
						%allParticipantsLeft = true;
						%list = $SealBattleParticipants;
						
						// Parse comma-separated list of participants
						while(String::len(%list) > 0)
						{
							%commaPos = String::findSubStr(%list, ",");
							if(%commaPos > 0)
							{
								%participantId = String::getSubStr(%list, 0, %commaPos);
								%list = String::getSubStr(%list, %commaPos + 1, 99999);
							}
							else
							{
								%participantId = %list;
								%list = "";
							}
							
							if(%participantId == "" || %participantId == -1)
								continue;
							
							// Check if participant still exists and is in Colloseum
							%participantName = Client::getName(%participantId);
							if(%participantName != "")
							{
								%participantZoneId = fetchData(%participantId, "zone");
								%participantZoneDesc = Zone::getDesc(%participantZoneId);
								if(%participantZoneDesc == "Colloseum")
								{
									// At least one participant is still in Colloseum
									%allParticipantsLeft = false;
									break;
								}
							}
						}
						
						// Only end the battle if ALL participants have left
						if(%allParticipantsLeft)
						{
							%participantNames = SealBattle::GetParticipantNames();
							messageAll(2, "" @ %participantNames @ " have fled the battle to break the seal! All hope is lost...");
							
							// Find the initiator (first participant) to conclude the battle
							%initiatorId = GetWord($SealBattleParticipants, 0);
							if(%initiatorId == "" || %initiatorId == -1)
								%initiatorId = %clientId;  // Fallback to the player who left
							
							SealBattle::Conclude(%initiatorId, false);  // false = failure
							return;  // Exit early - battle has been concluded
						}
					}
				}
			}
			
			// CRITICAL: Skip town bots - they should never change zones (they stay in their spawn location)
			%isTownBot = IsTownBot(%clientId);
			if(%isTownBot)
			{
				// Town bots should never change zones - if they do, something is wrong
				// Just update their stored zone data silently and return
				storeData(%clientId, "zone", $Zone::FolderID[%zoneflag]);
				return;
			}
			
			//the client's current zone does not match the one he really is in, so boot the player out of his
			//current zone (if any)
			%oldZone = fetchData(%clientId, "zone");
			if(%oldZone != "")
			{
				%oldZoneIndex = Zone::getIndex(%oldZone);
				%playerName = Client::getName(%clientId);
				
				// Check if this is a newly spawned enemy bot (within 5 seconds) - if so, skip logging zone change
				// This prevents false "changing zones" logs for bots that just spawned with stale zone data
				%shouldLogZoneChange = true;
				if(Player::isAiControlled(%clientId))
				{
					%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0")
					{
						%spawnTime = fetchData(%clientId, "SpawnTime");
						if(%spawnTime != "" && %spawnTime != -1)
						{
							%currentTime = GetPersistentTime();
							%timeSinceSpawn = %currentTime - %spawnTime;
							// If bot spawned within last 5 seconds, don't log zone change (likely stale data from reused clientId)
							if(%timeSinceSpawn <= 5)
							{
								%shouldLogZoneChange = false;
							}
						}
					}
				}
				
				if(%shouldLogZoneChange)
				{
					%oldZoneDesc = $Zone::Desc[%oldZoneIndex];
					%newZoneDesc = $Zone::Desc[%zoneflag];
					// DEBUG: Commented out to reduce server lag
					//echo("[ZONE DEBUG] Player " @ %playerName @ " (clientId=" @ %clientId @ ") changing zones: " @ %oldZoneIndex @ " (" @ %oldZoneDesc @ ") -> " @ %zoneflag @ " (" @ %newZoneDesc @ ")");
				}
				Zone::DoExit(%oldZoneIndex, %clientId);
				
				// Decrement player count for old zone (skip AI bots)
				if(!Player::isAiControlled(%clientId))
				{
					%oldCount = $ZonePlayerCount[%oldZoneIndex];
					if(%oldCount > 0)
						$ZonePlayerCount[%oldZoneIndex] = %oldCount - 1;
					
					// DEBUG: Commented out to reduce server lag
					//echo("[ZONE DEBUG] Zone " @ %oldZoneIndex @ " (" @ %oldZoneDesc @ ") now has " @ ($ZonePlayerCount[%oldZoneIndex]) @ " player(s)");
					
					// If no players left in old zone, despawn bots after 30 seconds (prevents crash from too many operations at once)
					// CRITICAL: Only process if zone index is valid (> 0)
					// Zone::getIndex() returns -1 for invalid zones (like "Unknown" zone)
					if($ZonePlayerCount[%oldZoneIndex] <= 0 && %oldZoneIndex > 0)
					{
						// Cancel any pending spawn (player left before 10s delay expired)
						CancelPendingZoneSpawn(%oldZoneIndex);
						schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
					}
				}
			}
	
		//throw the player inside this new zone
		%playerName = Client::getName(%clientId);
		%zoneDesc = $Zone::Desc[%zoneflag];
		%oldZoneDesc = "";
		if(%oldZone != "")
		{
			%oldZoneIndex = Zone::getIndex(%oldZone);
			if(%oldZoneIndex > 0)
				%oldZoneDesc = $Zone::Desc[%oldZoneIndex];
		}
		
		// Skip town bots from zone entry logging and counting (they're handled separately)
		%isTownBot = IsTownBot(%clientId);
		
		if(!%isTownBot)
			// DEBUG: Commented out to reduce server lag
			//echo("[ZONE DEBUG] Player " @ %playerName @ " (clientId=" @ %clientId @ ") entering zone " @ %zoneflag @ " (" @ %zoneDesc @ ") from zone " @ %oldZone @ " (" @ %oldZoneDesc @ ")");
		
		Zone::DoEnter(%zoneflag, %clientId);
		
		// Increment player count for new zone and spawn bots (skip AI bots and town bots)
		if(!Player::isAiControlled(%clientId) && !%isTownBot)
		{
				%newCount = $ZonePlayerCount[%zoneflag];
				if(%newCount == "")
					%newCount = 0;
				$ZonePlayerCount[%zoneflag] = %newCount + 1;
				// DEBUG: Commented out to reduce server lag
			//echo("[ZONE DEBUG] Zone " @ %zoneflag @ " (" @ %zoneDesc @ ") now has " @ ($ZonePlayerCount[%zoneflag]) @ " player(s)");
				
				// Clear any pending despawn schedule since a player entered the zone
				$ZoneBotDespawnSchedule[%zoneflag] = "";
				
				// If this is the first player in the zone, schedule bot spawn after delay
				// (Delay prevents spawns when players just pass through quickly)
				if($ZonePlayerCount[%zoneflag] == 1)
				{
					// Use ScheduleZoneSpawn for delayed verification
					// Default 10 second delay, configurable via $ZoneSpawnDelay
					ScheduleZoneSpawn(%zoneflag);
				}
			}
			
			// Check teleporter requirements for DUNGEON or PROTECTED zones (skip AI bots and town bots)
			if($Zone::Type[%zoneflag] == "DUNGEON" || $Zone::Type[%zoneflag] == "PROTECTED")
			{
				// Skip requirement checks for AI bots and town bots
				if(!Player::isAiControlled(%clientId) && !%isTownBot)
				{
					%zoneDesc = $Zone::Desc[%zoneflag];
					
					// Check if this zone has REMORT requirement
					%remortReq = $Zone::RemortRequirement[%zoneDesc];
					if(%remortReq != "")
					{
						%playerRemort = fetchData(%clientId, "RemortStep");
						if(%playerRemort == "")
							%playerRemort = 0;
						
						if(%playerRemort < %remortReq)
						{
							// Player doesn't meet REMORT requirement - kick them out
							Zone::DoExit(%zoneflag, %clientId);
							FellOffMap(%clientId);
							%msg = "<jc>You are not strong enough to be here. You need Remort " @ %remortReq @ "+ to enter this area.";
							if(Client::getName(%clientId) != "")
								centerprint(%clientId, %msg, floor(String::len("You are not strong enough to be here. You need Remort " @ %remortReq @ "+ to enter this area.") / 20));
							return;
						}
					}
					
					// Check if this zone has TOURNYRANK requirement
					%tournyReq = $Zone::TournyRankRequirement[%zoneDesc];
					if(%tournyReq != "")
					{
						%playerTourny = fetchData(%clientId, "TournyRank");
						if(%playerTourny == "")
							%playerTourny = 0;
						
						if(%playerTourny < %tournyReq)
						{
							// Player doesn't meet TOURNYRANK requirement - kick them out
							Zone::DoExit(%zoneflag, %clientId);
							FellOffMap(%clientId);
							%msg = "<jc>You are not strong enough to be here. You need Tournament Rank " @ %tournyReq @ "+ to enter this area.";
							if(Client::getName(%clientId) != "")
								centerprint(%clientId, %msg, floor(String::len("You are not strong enough to be here. You need Tournament Rank " @ %tournyReq @ "+ to enter this area.") / 20));
							return;
						}
					}
				}
			}
		}
		else
		{
			//the client is in the same zone as he was since the last zonecheck
			// Use temp variable to prevent debug warnings for unassigned variables
			%tempAmbientSound = $Zone::AmbientSound[%zoneflag];
			if(%tempAmbientSound != "")
			{
				%m = $Zone::AmbientSoundPerc[%zoneflag];
				if(%m == "") %m = 100;
	
				%r = floor(getRandom() * 100)+1;
				if(%r <= %m)
					Client::sendMessage(%clientId, 0, "~w" @ %tempAmbientSound);
			}
			if($Zone::Music[%zoneflag, 1] != "")
			{
				if(%clientId.MusicTicksLeft < 1)
				{
					for(%m = 1; $Zone::Music[%zoneflag, %m] != ""; %m++){}
					%m--;
					%clientId.currentMusic = floor(getRandom() * %m) + 1;

					Client::sendMessage(%clientId, 0, "~w" @ $Zone::Music[%zoneflag, %clientId.currentMusic]);
					%clientId.MusicTicksLeft = $Zone::MusicTicks[%zoneflag, %clientId.currentMusic]+2;
				}
			}
			if($Zone::Type[%zoneflag] == "WATER")
			{
				if(!IsDead(%clientId))
				{
					// Validate client is a player (not a bot) before checking items
					%playerObj = Client::getOwnedObject(%clientId);
					if(%playerObj != "" && %playerObj != -1 && !Player::isAiControlled(%clientId))
					{
						%noDrown = "";
						for(%i = 1; (%orb = $ItemList[Orb, %i]) != ""; %i++)
						{
							if($ProtectFromWater[%orb])
							{
								if(Player::getItemCount(%clientId, %orb @ "0"))
								{
									storeData(%clientId, "drownCounter", 0);
									%noDrown = True;
									break;
								}
							}
						}
					}
					else
					{
						%noDrown = "";
					}

					if(!%noDrown)
					{
						%dn = 10;

						storeData(%clientId, "drownCounter", 1, "inc");
						if((%dc = fetchData(%clientId, "drownCounter")) > %dn)
						{
							%dmg = Cap(floor(pow((%dc - %dn) / 1.2, 2)), 1.0, 1000) * "0.01";
							GameBase::virtual(%clientId, "onDamage", 0, %dmg, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId);
							%snd = radnomItems(3, SoundDrown1, SoundDrown2, SoundDrown3);
							playSound(%snd, GameBase::getPosition(%clientId));
						}
					}
				}
			}
		}

		//this simulates underwater
		if($Zone::Type[%zoneflag] == "WATER")
			if($underwaterEffects)
				gravWorkaround(%clientId, 1);
	}
	else
	{
		//the player is not inside any zone.
		//if the player has a current zone, then we need to kick him out of it
		%currentZone = fetchData(%clientId, "zone");
		if(%currentZone != "")
		{
			%oldZoneIndex = Zone::getIndex(%currentZone);
			// CRITICAL: Only process if zone index is valid (> 0)
			// Zone::getIndex() returns -1 for invalid zones (like "Unknown" zone)
			if(%oldZoneIndex > 0)
			{
				Zone::DoExit(%oldZoneIndex, %clientId);
				
				// Decrement player count for old zone (skip AI bots)
				if(!Player::isAiControlled(%clientId))
				{
					%oldCount = $ZonePlayerCount[%oldZoneIndex];
					if(%oldCount > 0)
						$ZonePlayerCount[%oldZoneIndex] = %oldCount - 1;
					
					// If no players left in old zone, despawn bots after 30 seconds
					if($ZonePlayerCount[%oldZoneIndex] <= 0)
					{
						// Cancel any pending spawn (player left before 10s delay expired)
						CancelPendingZoneSpawn(%oldZoneIndex);
						schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
					}
				}
			}
		}
	
		//start playing the ambient sound for the unknown zone
		if($Zone::AmbientSound[0] != "")
		{
			%m = $Zone::AmbientSoundPerc[0];
			if(%m == "") %m = 100;
			
			%r = floor(getRandom() * 100)+1;
			if(%r <= %m)
				Client::sendMessage(%clientId, 0, "~w" @ $Zone::AmbientSound[0]);
		}
	
		//play the enter sound for the unknown zone
		if($Zone::EnterSound[0] != "")
			Client::sendMessage(%clientId, 0, "~w" @ $Zone::EnterSound[0]);

		//play unknown zone music
		if($Zone::Music[0, 1] != "")
		{
			if(%clientId.MusicTicksLeft < 1)
			{
				for(%m = 1; $Zone::Music[0, %m] != ""; %m++){}
				%m--;
				%clientId.currentMusic = floor(getRandom() * %m) + 1;

				Client::sendMessage(%clientId, 0, "~w" @ $Zone::Music[0, %clientId.currentMusic]);
				%clientId.MusicTicksLeft = $Zone::MusicTicks[0, %clientId.currentMusic]+2;
			}
		}
	}

	//-----------------------------------------------------------
	// Decrease music ticks
	//-----------------------------------------------------------
	if(%clientId.MusicTicksLeft > 0)
		%clientId.MusicTicksLeft--;

	//-----------------------------------------------------------
	// Decrease bonus state ticks
	//-----------------------------------------------------------
	DecreaseBonusStateTicks(%clientId);

	//-----------------------------------------------------------
	// Check if the player has moved since last ZoneCheck
	//-----------------------------------------------------------
	%pos = GameBase::getPosition(%clientId);
	if(%pos != %clientId.zoneLastPos && !IsDead(%clientId))
	{
		// Validate client is a player (not a bot) before checking items
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj != "" && %playerObj != -1 && !Player::isAiControlled(%clientId))
		{
			//train Weight Capacity
			if(OddsAre(8))
				UseSkill(%clientId, $SkillWeightCapacity, True, True, "", True);

			//cycle thru orbs
			for(%i = 1; (%orb = $ItemList[Orb, %i]) != ""; %i++)
			{
				if(OddsAre($BurnOut[%orb]))
				{
					if(Player::getItemCount(%clientId, %orb @ "0"))
					{
						Client::sendMessage(%clientId, $MsgRed, "Your " @ %orb.description @ " has burned out.");
						Player::decItemCount(%clientId, %orb @ "0", 1);
						RefreshAll(%clientId);
					}
				}
				if($BurnOutInRain[%orb] > 0)
				{
					if(fetchData(%clientId, "zone") == "" && $isRaining)
					{
						if(OddsAre($BurnOutInRain[%orb]))
						{
							if(Player::getItemCount(%clientId, %orb @ "0"))
							{
								Client::sendMessage(%clientId, $MsgRed, "The rain has burned out your " @ %orb.description @ ".");
								Player::decItemCount(%clientId, %orb @ "0", 1);
								RefreshAll(%clientId);
							}
						}
					}
				}
			}

			//hard-coded list to save on CPU
			for(%z = 1; $ItemList[Badge, %z] != ""; %z++)
			{
				if(Player::getItemCount(%clientId, $ItemList[Badge, %z]))
				{
				%a = GetWord($BonusItem[$ItemList[Badge, %z]], 0);
				%b = GetWord($BonusItem[$ItemList[Badge, %z]], 1);
				%c = GetWord($BonusItem[$ItemList[Badge, %z]], 2);

				if(OddsAre(%c))
					GiveThisStuff(%clientId, %a @ " " @ %b, True);
			}
		}

			//perhaps leave scent
			if(!fetchData(%clientId, "invisible"))
			{
				if(OddsAre(floor($PlayerSkill[%clientId, $SkillSenseHeading] / 100)+1))
				{
					storeData(%clientId, "lastScent", GameBase::getPosition(%clientId));
				}
			}
		}
	}
	%clientId.zoneLastPos = %pos;

	storeData(%clientId, "tmpzone", "");
}

function gravWorkaround(%clientId, %method)
{
	dbecho($dbechoMode, "gravWorkaround(" @ %clientId @ ", " @ %method @ ")");

	if(%method == 1)
	{
		%rzdelay = 2;
		%steps = 24;

		for(%i = 0; %i < %steps; %i++)
		{
			%d = %i / (%steps / %rzdelay);
			schedule("Player::applyImpulse(" @ %clientId @ ", \"0 0 13\");", %d, %clientId);
		}
	}
	else if(%method == 2)
	{
		if($xyvel == "") $xyvel = 0.8;
		if($nzvel == "") $nzvel = 0.2;
		if($pzvel == "") $pzvel = 1.0;
		if($impulse == "") $impulse = 4;

		Player::applyImpulse(%clientId, "0 0 " @ $impulse);

		%vel = Item::getVelocity(%clientId);
		
		%xvel = GetWord(%vel, 0) * $xyvel;
		%yvel = GetWord(%vel, 1) * $xyvel;
		%zvel = GetWord(%vel, 2);

		if(%zvel < 0)
			%zvel *= $nzvel;
		else
			%zvel *= $pzvel;

		%nvel = %xvel @ " " @ %yvel @ " " @ %zvel;

		Item::setVelocity(%clientId, %nvel);
	}
}

function Zone::DoEnter(%z, %clientId)
{
	dbecho($dbechoMode, "Zone::DoEnter(" @ %z @ ", " @ %clientId @ ")");

	%oldZone = fetchData(%clientId, "zone");
	%newZone = $Zone::FolderID[%z];
	
	%playerName = Client::getName(%clientId);
	%zoneDesc = $Zone::Desc[%z];
	%zoneType = $Zone::Type[%z];
	%oldZoneDesc = "";
	%oldZoneIndex = -1;
	if(%oldZone != "")
	{
		%oldZoneIndex = Zone::getIndex(%oldZone);
		if(%oldZoneIndex > 0)
			%oldZoneDesc = $Zone::Desc[%oldZoneIndex];
	}
	
	// Skip town bots from zone entry logging (they're handled separately)
	%isTownBot = IsTownBot(%clientId);
	
	if(!Player::isAiControlled(%clientId) && !%isTownBot)
	{
		// DEBUG: Commented out to reduce server lag
		//echo("[ZONE DEBUG] Zone::DoEnter - Player " @ %playerName @ " (clientId=" @ %clientId @ ") entering zone " @ %z @ " (" @ %zoneDesc @ ", type=" @ %zoneType @ ") from zone " @ %oldZoneIndex @ " (" @ %oldZoneDesc @ ")");
	}

	storeData(%clientId, "zone", $Zone::FolderID[%z]);

	if($Zone::Type[%z] == "PROTECTED")
	{
		%msg = "You have entered " @ $Zone::Desc[%z] @ ".  This is protected territory.";
		%color = $MsgBeige;
	}
	else if($Zone::Type[%z] == "DUNGEON")
	{
		%msg = "You have entered " @ $Zone::Desc[%z] @ ".  Beware of enemies!";
		%color = $MsgRed;
	}
	else if($Zone::Type[%z] == "WATER")
	{
		%msg = "";
	}
	else if($Zone::Type[%z] == "FREEFORALL")
	{
		%msg = "You have entered " @ $Zone::Desc[%z] @ ".";
		%color = $MsgRed;
	}

	if($Zone::EnterSound[%z] != "")
		%msg = %msg @ "~w" @ $Zone::EnterSound[%z];

	// Skip sending zone entry messages to town bots
	if(%msg != "" && !%isTownBot)
		Client::sendMessage(%clientId, %color, %msg);

	if(!Player::isAiControlled(%clientId) && !%isTownBot)
		Game::refreshClientScore(%clientId);	//this is so players can see which zone this client is in

	Zone::onEnter(%clientId, %oldZone, %newZone);
}

function Zone::DoExit(%z, %clientId)
{
	dbecho($dbechoMode, "Zone::DoExit(" @ %z @ ", " @ %clientId @ ")");

	%zoneLeft = fetchData(%clientId, "zone");

	storeData(%clientId, "zone", "");

	if($Zone::Type[%z] == "PROTECTED")
	{
		%msg = "You have left " @ $Zone::Desc[%z] @ ".";
		%color = $MsgRed;
	}
	else if($Zone::Type[%z] == "DUNGEON")
	{
		%msg = "You have left " @ $Zone::Desc[%z] @ ".";
		%color = $MsgBeige;
		//schedule("WipeFromZone(" @ %z @ ");",120);
	}
	else if($Zone::Type[%z] == "WATER")
	{
		%msg = "";
	}
	else if($Zone::Type[%z] == "FREEFORALL")
	{
		%msg = "You have left " @ $Zone::Desc[%z] @ ".";
		%color = $MsgBeige;
	}

	if($Zone::ExitSound[%z] != "")
		%msg = %msg @ "~w" @ $Zone::ExitSound[%z];

	if(%msg != "")
	      Client::sendMessage(%clientId, %color, %msg);

	if(!Player::isAiControlled(%clientId))
		Game::refreshClientScore(%clientId);	//this is so players can see which zone this client is in

	Zone::onExit(%clientId, %zoneLeft);
}

function WipeFromZone(%z)
{
	%run = 1;

	// Only check for PLAYERS in the zone, not AI bots
	// If only AI bots remain, we should still run cleanup to kill them
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		// Skip AI bots - only check if players are in the zone
		if(!Player::isAiControlled(%cl) && Zone::getDesc(fetchData(%cl, "zone")) == $Zone::Desc[%z])
			%run = 0;
	}

	if(%run == 1)
	{
		%list = GetEveryoneIdList();
		for(%cl = 0; GetWord(%list, %cl) != -1; %cl += 1)
		{
			%id = GetWord(%list,%cl);
			// Skip town bots - they should never be wiped from zones
			// Town bots have BotInfoAiName starting with "TownBot_", and they're managed by dynamic loading system
			// CRITICAL: Only skip if BotInfoAiName starts with "TownBot_" - enemy bots also have BotInfoAiName set
			%botInfoAiName = fetchData(%id, "BotInfoAiName");
			if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			{
				// Only skip if it's a town bot (starts with "TownBot_")
				if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
					continue; // Town bot - skip
			}
			
			// CRITICAL: Skip seal battle bots - they are managed by remortseal.cs
			%sealBattleBot = fetchData(%id, "SealBattleBot");
			%isSealBot = (%sealBattleBot == "true" || %sealBattleBot == "True" || %sealBattleBot == "1");
			if(!%isSealBot)
			{
				%botName = Client::getName(%id);
				if(%botName != "")
				{
					if(String::findSubStr(%botName, "SealFighter") == 0 || 
					   String::findSubStr(%botName, "SealMage") == 0 || 
					   String::findSubStr(%botName, "SealGuardian") == 0)
					{
						%isSealBot = true;
					}
				}
			}
			if(%isSealBot)
				continue; // Skip seal battle bots
			
			// CRITICAL: Skip Colloseum arena bots - they are managed by rpgarena.cs
			%botName = Client::getName(%id);
			%isColloseumBot = false;
			if(%botName != "")
			{
				if(%botName == "Round1" || %botName == "Round2" || %botName == "Round3")
				{
					%isColloseumBot = true;
				}
			}
			// Also check if bot is in Colloseum zone
			if(!%isColloseumBot)
			{
				%botZone = fetchData(%id, "zone");
				%botZoneDesc = Zone::getDesc(%botZone);
				if(%botZoneDesc == "Colloseum")
				{
					%isColloseumBot = true;
				}
			}
			if(%isColloseumBot)
				continue; // Skip Colloseum arena bots
			
			if (Player::isAiControlled(%id) && Zone::getDesc(fetchData(%id, "zone")) == $Zone::Desc[%z])
			{
				storeData(%id, "noDropLootbagFlag", True);
				Player::Kill(%id);
			}
		}
	}
}

// Helper function to check if a client is a town bot
// Town bots have BotInfoAiName that starts with "TownBot_"
function IsTownBot(%clientId)
{
	dbecho($dbechoMode, "IsTownBot(" @ %clientId @ ")");

	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
	{
		// Check if BotInfoAiName starts with "TownBot_"
		if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
			return True;
	}
	return False;
}

function IsInBetween(%x, %r1, %r2)
{
	dbecho($dbechoMode, "IsInBetween(" @ %x @ ", " @ %r1 @ ", " @ %r2 @ ")");

	if(%r1 > %r2)
	{
		%tmp = %r1;
		%r1 = %r2;
		%r2 = %tmp;
	}
	if(%x >= %r1 && %x <= %r2)
		return True;
	else
		return False;
}

function Zone::getIndex(%z)
{
	dbecho($dbechoMode, "Zone::getIndex(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return %i;
			}
		}
	}
	return -1;
}
function Zone::getMarker(%z)
{
	dbecho($dbechoMode, "Zone::getMarker(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return $Zone::Marker[%i];
			}
		}
	}
	return -1;
}
function Zone::getType(%z)
{
	dbecho($dbechoMode, "Zone::getType(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return $Zone::Type[%i];
			}
		}
	}
	return -1;
}
function Zone::getDesc(%z)
{
	dbecho($dbechoMode, "Zone::getDesc(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return $Zone::Desc[%i];
			}
		}
	}
	return -1;
}
function Zone::getEnterSound(%z)
{
	dbecho($dbechoMode, "Zone::getEnterSound(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return $Zone::EnterSound[%i];
			}
		}
	}
	return -1;
}
function Zone::getAmbientSound(%z)
{
	dbecho($dbechoMode, "Zone::getAmbientSound(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return $Zone::AmbientSound[%i];
			}
		}
	}
	return -1;
}
function Zone::getAmbientSoundPerc(%z)
{
	dbecho($dbechoMode, "Zone::getAmbientSoundPerc(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return $Zone::AmbientSoundPerc[%i];
			}
		}
	}
	return -1;
}
function Zone::getExitSound(%z)
{
	dbecho($dbechoMode, "Zone::getExitSound(" @ %z @ ")");

	if(%z != "")
	{
		for(%i = 1; %i <= $numZones; %i++)
		{
			if($Zone::FolderID[%i] == %z)
			{
				return $Zone::ExitSound[%i];
			}
		}
	}
	return -1;
}

function Zone::onEnter(%clientId, %oldZone, %newZone)
{
	dbecho($dbechoMode, "Zone::onEnter(" @ %clientId @ ", " @ %oldZone @ ", " @ %newZone @ ")");

	refreshHPREGEN(%clientId);	//this is because you regen faster or slower depending on the zone you are in
	refreshMANAREGEN(%clientId);

	if(Zone::getType(%newZone) == "WATER")
	{
		//Client::sendMessage(%clientId, $MsgBeige, "You have entered water!");
		storeData(%clientId, "drownCounter", "");
	}
	if(Zone::getType(%newZone) == "PROTECTED")
	{
		if(fetchData(%clientId, "isMimic"))
		{
			storeData(%clientId, "RACE", Client::getGender(%clientId) @ "Human");
			storeData(%clientId, "isMimic", "");
			UpdateTeam(%clientId);
			RefreshAll(%clientId);

			playSound(AbsorbABS, GameBase::getPosition(%clientId));
		}
	}
}

function Zone::onExit(%clientId, %zoneLeft)
{
	dbecho($dbechoMode, "Zone::onExit(" @ %clientId @ ", " @ %zoneLeft @ ")");

	refreshHPREGEN(%clientId);	//this is because you regen faster or slower depending on the zone you are in
	refreshMANAREGEN(%clientId);

	if(Zone::getType(%zoneLeft) == "WATER")
	{
		//Client::sendMessage(%clientId, $MsgBeige, "You have left water!");
		storeData(%clientId, "drownCounter", "");
	}
}

function GetNearestZone(%clientId, %zonetype, %returnType)
{
	dbecho($dbechoMode, "GetNearestZone(" @ %clientId @ ", " @ %zonetype @ ", " @ %returnType @ ")");

	//%zonetype can be "town", "dungeon" or "freeforall"

	%closestDist = 500000;
	%closestZone = "";
	%mpos = "";
	%clpos = GameBase::getPosition(%clientId);

	for(%i = 1; %i <= $numZones; %i++)
	{
		%type = $Zone::Type[%i];
		if(%type == "PROTECTED" && String::ICompare(%zonetype, "town") == 0 || %type == "DUNGEON" && String::ICompare(%zonetype, "dungeon") == 0 || %type == "FREEFORALL" && String::ICompare(%zonetype, "freeforall") == 0 || %zonetype == -1)
		{
			%finalpos = $Zone::Marker[%i];
	
			%dist = Vector::getDistance(%finalpos, %clpos);
			if(%dist < %closestDist)
			{
				%closestDist = %dist;
				%closestZoneDesc = $Zone::Desc[%i];
				%closestZone = $Zone::FolderID[%i];
				%mpos = %finalpos;
			}
		}
	}

	if(%mpos == "")		//no zones were found (this means there are NO zones in the map...)
		return False;
	
	//%returnType:
	//1 = returns the distance from the client to the nearest zone
	//2 = returns the description of the zone nearest to the client
	//3 = returns the zone id of the zone nearest to the client
	//4 = returns the position of the middle of the zone nearest to the client

	if(%returnType == 1)
		return %closestDist;
	else if(%returnType == 2)
		return %closestZoneDesc;
	else if(%returnType == 3)
		return %closestZone;
	else if(%returnType == 4)
		return %mpos;
}

function GetZoneByKeywords(%clientId, %keywords, %returnType)
{
	dbecho($dbechoMode, "GetZoneByKeywords(" @ %clientId @ ", " @ %keywords @ ", " @ %returnType @ ")");

	%mpos = "";

	%group = nameToId("MissionGroup\\Zones");

	if(%group != -1)
	{
		//IMPORTANT: zone markers must be objects 0 and 1 in the zone's folder

		%count = Group::objectCount(%group);
		for(%i = 0; %i <= %count-1; %i++)
		{
			%object = Group::getObject(%group, %i);
			%system = Object::getName(%object);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);
			if(%type == "PROTECTED" || %type == "DUNGEON" || %type == "FREEFORALL")
			{
				if(String::findSubStr(%desc, %keywords) != -1)
				{
					//get the two markers
					%tmpgroup = nameToId("MissionGroup\\Zones\\" @ %system);

					%m1pos = GameBase::getPosition(Group::getObject(%tmpgroup, 0));
					%m2pos = GameBase::getPosition(Group::getObject(%tmpgroup, 1));

					%mx = (((GetWord(%m2pos, 0) - GetWord(%m1pos, 0)) / 2) + GetWord(%m1pos, 0));
					%my = (((GetWord(%m2pos, 1) - GetWord(%m1pos, 1)) / 2) + GetWord(%m1pos, 1));
					%mz = (((GetWord(%m2pos, 2) - GetWord(%m1pos, 2)) / 2) + GetWord(%m1pos, 2));

					%mpos = %mx @ " " @ %my @ " " @ %mz;
					%dist = Vector::getDistance(%mpos, GameBase::getPosition(%clientId));

					//%returnType:
					//1 = returns the distance from the client to the zone
					//2 = returns the description of the zone
					//3 = returns the zone id
					//4 = returns the position of the middle of the zone

					if(%returnType == 1)
						return %dist;
					else if(%returnType == 2)
						return %desc;
					else if(%returnType == 3)
						return %object;
					else if(%returnType == 4)
						return %mpos;
				}
			}
		}
		return False;	
	}
	else
		return False;
}

function Zone::getNumPlayers(%z, %all)
{
	dbecho($dbechoMode, "Zone::getNumPlayers(" @ %z @ ", " @ %all @ ")");

	// Initialize variables to prevent unassigned warnings
	%list = "";
	if(%all == "" || %all == -1)
		%all = false;
	
	if(%all)
		%list = GetEveryoneIdList();
	else
	{
		%list = GetPlayerIdList();
		// Ensure %list is initialized (GetPlayerIdList should always return at least "")
		if(%list == "")
			%list = "";
	}

	%n = 0;
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		
		// CRITICAL: Skip shell bots (bots with no player object)
		// Shell bots have bot data but no valid player object, and should not be counted
		%playerObj = Client::getOwnedObject(%id);
		%botInfoAiName = fetchData(%id, "BotInfoAiName");
		%spawnBotInfo = fetchData(%id, "SpawnBotInfo");
		%hasBotData = ((%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0") || 
		               (%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0"));
		
		// If this is a bot (has bot data) but has no player object, it's a shell bot - skip it
		if(%hasBotData && (%playerObj == -1 || %playerObj == ""))
		{
			// Shell bot detected - skip it to prevent FindPlayerInBotGroup() from being called
			continue;
		}

		if(fetchData(%id, "zone") == %z)
			%n++;
	}

	return %n;
}

function ObjectInWhichZone(%object)
{
	dbecho($dbechoMode, "ObjectInWhichZone(" @ %object @ ")");

	//not perfect but good enough

	%fid = "";
	%closest = 99999;
	%objpos = GameBase::getPosition(%object);
	for(%z = 1; %z <= $numZones; %z++)
	{
		%rad = ($Zone::Length[%z] + $Zone::Width[%z] + $Zone::Height[%z]) / 3;
		%dist = Vector::getDistance(%objpos, $Zone::Marker[%z]);
		if(%dist <= %rad)
		{
			if(%dist < %closest)
			{
				%closest = %dist;
				%fid = $Zone::FolderID[%z];
			}
		}
	}
	return %fid;
}

function Zone::getPlayerList(%z, %type)
{
	dbecho($dbechoMode, "Zone::getPlayerList(" @ %z @ ", " @ %type @ ")");

	// Initialize %list to empty string
	%list = "";
	if(%type == 1)
		%list = GetEveryoneIdList();
	else if(%type == 2)
		%list = GetPlayerIdList();
	else if(%type == 3)
		%list = GetBotIdList();

	%n = 0;
	%aa = "";
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);

		if(fetchData(%id, "zone") == %z)
			%aa = %aa @ %id @ " ";
	}

	return %aa;
}
//function EmptyZoneCheck()
//{                                                // Begin function: scans bots and removes those in empty/invalid zones.
//   %mon = GetBotIdList();                        // Get a space-delimited list of bot *player object IDs*.

//   for (%i = 0; getword(%mon, %i) != -1; %i++)   // Iterate the list word-by-word until getWord(...) returns -1 (no more words).
//   {                                             // Start loop body for the current index %i.
//      %id = getword(%mon, %i);                   // Extract the current bot's player object ID from the list.

      						 // Only process non-NPC bots (keep this if you don't want NPCs culled)
//      if (!$npc[%id])                            // Skip if this ID is flagged as an NPC; proceed only for regular bots.
//      {                                          // Begin processing for a non-NPC bot.
//         %z = fetchData(%id, "zone");            // Read the bot's *raw* zone key from your mod's data store.
         					 // If not in a valid zone, kill immediately
//         if (%z == "" || %z == "unknown")      // Treat blank/unknown as out-of-zone (invalid)                          // If the normalized zone is invalid/empty (not a real zone key)...
//         {                                       // Enter immediate cleanup branch.
//            FellOffMap(%id);                     // Call your cleanup: for AI this kills the bot (no loot); for players, teleports.
//            continue;                            // Skip the rest of this loop iteration; move on to the next bot.
//         }                                       // End invalid-zone branch.

         					 // Now do the normal player presence check using the REAL zone key
//         %plist = Zone::getPlayerList(%zn, 2);   // Get a list of *players* (type=2) who are in the same valid zone.
//         if (%plist == "")                       // If no players are present in that zone...
//            FellOffMap(%id);                     // ...clean up this bot (AI dies; prevents idling bots in empty zones).
//      }                                          // End non-NPC guard.
//   }                                             // End for-loop over all bot IDs.
//}                                                // End function.
//function EmptyZoneCheckLoop()
//{                                                // Self-rescheduling loop wrapper.
//   EmptyZoneCheck();                             // Run the cleanup once.
//  schedule("EmptyZoneCheckLoop();", 20);        // Run again in 60 seconds.
//}

// Start it once after mission init (e.g., in onMissionLoadDone)
//schedule("EmptyZoneCheckLoop();", 20);

