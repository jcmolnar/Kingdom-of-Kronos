//============================================================================
// ai_townbots.cs — split from Ai.cs (scatter split, ai.cs peel)
// Extracted 2026-07-18 at commit 320698f. Mechanical text move, no behavior change.
// Townbot/NPC systems: town client registry, zone spawn/despawn, items/mounts, dialogue helpers, pets, town team enforcement.
// exec'd by Ai.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====

function AI::sayLater(%clientId, %guardId, %message, %look)
{
	dbecho($dbechoMode, "AI::sayLater(" @ %clientId @ ", " @ %guardId @ ", " @ %message @ ", " @ %look @ ")");

	// Validate client ID exists
	if(%clientId == "" || %clientId == -1)
		return;

	// Get guard name (try BotInfo first, then Client::getName)
	%guardName = "";
	if(%guardId.name != "")
		%guardName = $BotInfo[%guardId.name, NAME];
	if(%guardName == "")
		%guardName = Client::getName(%guardId);
	if(%guardName == "")
		%guardName = "Banker"; // Fallback

	Client::sendMessage(%clientId, $MsgBeige, %guardName @ " tells you, \"" @ %message @ "\"");

	// Modern NPC dialogue window (KronosNPC): mirror the spoken line into
	// the HUD client's window when a conversation is open there, plus the
	// parsed [keyword]/CAPS options as clickable buttons. Captures EVERY
	// bot's dialogue (quest/teleport/generic/...) without touching the
	// per-bot handlers. Vanilla clients never set knpcWinOpen.
	if(%clientId.knpcWinOpen != "" && %clientId.hasKronosHUD)
	{
		remoteEval(%clientId, "KNPCLine", %message);
		%knpcOpts = KronosNPC_ExtractOpts(%message);
		if(%knpcOpts != "")
			remoteEval(%clientId, "KNPCOpts", %knpcOpts);
	}

	if(%look)
		AI::lookAtPlayer(%clientId, %guardId);
}
function AI::lookAtPlayer(%clientId, %guardId)
{
	dbecho($dbechoMode, "AI::lookAtPlayer(" @ %clientId @ ", " @ %guardId @ ")");

	// Validate both client and guard still exist
	%clientPlayer = Client::getOwnedObject(%clientId);
	// CRITICAL FIX: Fallback if engine hasn't updated getOwnedObject yet
	if(%clientPlayer == -1 || %clientPlayer == "")
		%clientPlayer = FindPlayerInBotGroup(%clientId);

	%guardPlayer = Client::getOwnedObject(%guardId);
	// CRITICAL FIX: Fallback for guard too
	if(%guardPlayer == -1 || %guardPlayer == "")
		%guardPlayer = FindPlayerInBotGroup(%guardId);

	if(%clientPlayer == "" || %clientPlayer == -1 || %guardPlayer == "" || %guardPlayer == -1)
		return;

	// Use player objects for position calculations
	%clpos = GameBase::getPosition(%clientPlayer);
	%gupos = GameBase::getPosition(%guardPlayer);

	// Calculate direction vector from bot to player
	%v1 = Vector::sub(%clpos, %gupos);

	// For horizontal rotation, use only X and Y components (ignore Z/height)
	%dx = GetWord(%v1, 0);
	%dy = GetWord(%v1, 1);
	%horizontalVec = %dx @ " " @ %dy @ " 0";
	
	%norm = Vector::normalize(%horizontalVec);
	%rot = Vector::getRotation(%norm);

	%gurot = GameBase::getRotation(%guardPlayer);
	%temp = Vector::sub(%rot, %gurot);
	%temp2 = GetWord(%temp, 2);

	// For town bots (Player objects), we want them to face TOWARDS the player, not away
	// The original code added 180 degrees which made them face away - remove that for town bots
	// CRITICAL: Use isTownBot() helper function which properly distinguishes town bots from enemy bots
	// Town bots have BotInfoAiName but NO SpawnBotInfo
	// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
	%isTownBot = isTownBot(%guardId);
	
	if(%isTownBot)
	{
		// Town bot - face towards player (don't add 180 degrees)
		// Preserve current pitch and roll, only update yaw (horizontal rotation)
		%currentPitch = GetWord(%gurot, 0);
		%currentRoll = GetWord(%gurot, 1);
		%newYaw = GetWord(%rot, 2);
		%rot = %currentPitch @ " " @ %currentRoll @ " " @ %newYaw;
	}
	else
	{
		// Enemy bot: face directly towards player; do not add 180
		%currentPitch = GetWord(%gurot, 0);
		%currentRoll = GetWord(%gurot, 1);
		%newYaw = GetWord(%rot, 2);
		%rot = %currentPitch @ " " @ %currentRoll @ " " @ %newYaw;
	}

	// Pass the guard clientId to RotateTownBot (town bots are now Player objects, need clientId)
	// %guardId is already a clientId, so use it directly
	RotateTownBot(%guardId, %rot);
}

// NEW: Reverse lookup - clientId to bot info (O(1) lookup)
// $TownBotClient[clientId, "botName"] = bot name
// $TownBotClient[clientId, "zone"] = zone index
// $TownBotClient[clientId, "spawnTime"] = getSimTime() when spawned
// $TownBotClientCooldown[clientId] = getSimTime() when freed (3-second reuse cooldown)

//=============================================================================
// Town Bot Client Registry Functions
// These provide O(1) lookup for "is this client ID a town bot?"
//=============================================================================

function RegisterTownBotClient(%clientId, %botName, %zone)
{
	if(%clientId == "" || %clientId == -1)
		return;
		
	$TownBotClient[%clientId, "botName"] = %botName;
	$TownBotClient[%clientId, "zone"] = %zone;
	$TownBotClient[%clientId, "spawnTime"] = getSimTime();
	
	echo("[TOWN BOT SPAWN] Registered " @ %botName @ " at clientId=" @ %clientId @ ", zone=" @ %zone);
}

function UnregisterTownBotClient(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return;
		
	%botName = $TownBotClient[%clientId, "botName"];
	%zone = $TownBotClient[%clientId, "zone"];
	
	$TownBotClient[%clientId, "botName"] = "";
	$TownBotClient[%clientId, "zone"] = "";
	$TownBotClient[%clientId, "spawnTime"] = "";
	
	// Set cooldown to prevent immediate reuse
	$TownBotClientCooldown[%clientId] = getSimTime();
	
	if(%botName != "" && %botName != -1)
		echo("[TOWN BOT DESPAWN] Unregistered " @ %botName @ " from clientId=" @ %clientId @ ", cooldown set");
}

function IsTownBotClientId(%clientId)
{
	// O(1) check if client ID is a town bot
	if(%clientId == "" || %clientId == -1)
		return false;
		
	%botName = $TownBotClient[%clientId, "botName"];
	return (%botName != "" && %botName != -1 && %botName != "0");
}



function InitTownBots()
{
	dbecho($dbechoMode, "InitTownBots() - Registering bots for dynamic loading");
	echo("===== Calling InitTownBots() - Registering bots for dynamic loading =====");

	$TownBotList = "";  // Clear the list (now stores clientIds, not Item object IDs)
	$TownBotRegistry = "";  // Clear bot registry

	%group = nameToId("MissionGroup/TownBots");

	if(%group != -1)
	{
		%cnt = Group::objectCount(%group);
		for(%i = 0; %i <= %cnt - 1; %i++)
		{
			%object = Group::getObject(%group, %i);
			if(%object == -1 || %object == "")
				continue;
				
			%name = Object::getName(%object);
			%marker = "";  // Initialize marker
			
			if(getObjectType(%object) == "SimGroup")
			{
				// Call GatherBotInfo to parse bot info and get marker
				// This function parses the SimGroup structure and returns the marker object
				%marker = GatherBotInfo(%object);
				
				// Debug logging for specific merchants
				if(%name == "merchant14" || %name == "merchant19")
				{
					echo("DEBUG: GatherBotInfo for " @ %name @ " returned marker=" @ %marker);
				}
				
				// If GatherBotInfo failed or returned invalid marker, skip this bot
				if(%marker == "" || %marker == -1)
				{
					echo("WARNING: InitTownBots - GatherBotInfo returned invalid marker for " @ %name);
					if(%name == "merchant14" || %name == "merchant19")
					{
						echo("ERROR: " @ %name @ " will NOT spawn - marker is invalid!");
					}
					continue;
				}
			}
			else
			{
				// If it's not a SimGroup, it might be a Marker directly
				if(getObjectType(%object) == "Marker")
					%marker = %object;
			}

			// Skip if we don't have a valid marker
			if(%marker == "" || %marker == -1)
			{
				echo("WARNING: InitTownBots - No valid marker found for " @ %name);
				continue;
			}

			// Register bot for dynamic loading (don't spawn yet)
			%spawnPos = GameBase::getPosition(%marker);
			%spawnRot = GameBase::getRotation(%marker);
			
			// Store bot position and rotation for later spawning
			$BotInfo[%name, SPAWN_POS] = %spawnPos;
			$BotInfo[%name, SPAWN_ROT] = %spawnRot;
			$BotInfo[%name, SPAWN_MARKER] = %marker;
			
			// Determine which zone this bot belongs to
			%botZone = GetBotZone(%spawnPos);
			
			// FALLBACK: If zone detection failed (zone 0), try to manually assign based on bot name/position
			if(%botZone == 0)
			{
				// Try to find zone by description for known merchants
				%posX = GetWord(%spawnPos, 0);
				%posY = GetWord(%spawnPos, 1);
				
				// Debug: List all zones to help identify the correct one
				if(%name == "merchant14" || %name == "merchant19")
				{
					echo("DEBUG: Fallback triggered for " @ %name @ " at position " @ %spawnPos);
					echo("DEBUG: Searching through " @ $numZones @ " zones for matching description...");
				}
				
				// Black Market merchant (merchant14) - position around -2583 -1133 191
				if(%name == "merchant14" || (%posX >= -2600 && %posX <= -2560 && %posY >= -1160 && %posY <= -1100))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Black Market") == 0 || 
						   String::findSubStr(%zoneDesc, "Black Market") >= 0 ||
						   String::findSubStr(%zoneDesc, "black market") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Black Market zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Black Market zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Porellis bots (banker8, merchant19, quest21) - positions around -4477 to -4491, 1819 to 1845
				else if(%name == "merchant19" || %name == "banker8" || %name == "quest21" || (%posX >= -4500 && %posX <= -4450 && %posY >= 1815 && %posY <= 1860))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Porellis") == 0 || 
						   String::findSubStr(%zoneDesc, "Porellis") >= 0 ||
						   String::findSubStr(%zoneDesc, "porellis") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Porellis zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Porellis zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Sanctuary merchants (merchant4, merchant5, assassin1) - position around -2844 -1323 694
				else if(%name == "merchant4" || %name == "merchant5" || %name == "assassin1" || (%posX >= -2860 && %posX <= -2830 && %posY >= -1330 && %posY <= -1315))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Sanctuary") == 0 || 
						   String::findSubStr(%zoneDesc, "Sanctuary") >= 0 ||
						   String::findSubStr(%zoneDesc, "sanctuary") >= 0 ||
						   String::findSubStr(%zoneDesc, "Empress Sanctuary") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Sanctuary zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Sanctuary zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Arbal Research Center (merchant9-11, banker4, hunt3, quest16) - position around -2034 -2870 302
				else if(%name == "merchant9" || %name == "merchant10" || %name == "merchant11" || %name == "banker4" || %name == "hunt3" || %name == "quest16" || (%posX >= -2050 && %posX <= -1970 && %posY >= -2900 && %posY <= -2840))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Arbal Research Center") == 0 || 
						   String::findSubStr(%zoneDesc, "Arbal") >= 0 ||
						   String::findSubStr(%zoneDesc, "arbal") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Arbal Research Center zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Arbal Research Center zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Kingdom of Kronos (merchant12-13, guildmaster1) - position around -3397 1712 1551
				else if(%name == "merchant12" || %name == "merchant13" || %name == "guildmaster1" || (%posX >= -3420 && %posX <= -3390 && %posY >= 1690 && %posY <= 1730))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Kingdom of Kronos") == 0 || 
						   String::findSubStr(%zoneDesc, "Kingdom of Kronos") >= 0 ||
						   String::findSubStr(%zoneDesc, "Kronos") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Kingdom of Kronos zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Kingdom of Kronos zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// Yuliple City (manager1) - position around -1198.5 1503.25 72.776
				else if(%name == "manager1" || (%posX >= -1200 && %posX <= -1195 && %posY >= 1500 && %posY <= 1510))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "Yuliple City") == 0 || 
						   String::findSubStr(%zoneDesc, "Yuliple") >= 0 ||
						   String::findSubStr(%zoneDesc, "yuliple") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to Yuliple City zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find Yuliple City zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
				// The Void (quest23) - position around 357 1093 246
				else if(%name == "quest23" || (%posX >= 340 && %posX <= 370 && %posY >= 1080 && %posY <= 1110))
				{
					for(%z = 1; %z <= $numZones; %z++)
					{
						%zoneDesc = $Zone::Desc[%z];
						// Try multiple possible descriptions
						if(String::ICompare(%zoneDesc, "The Void") == 0 || 
						   String::findSubStr(%zoneDesc, "The Void") >= 0 ||
						   String::findSubStr(%zoneDesc, "the void") >= 0)
						{
							%botZone = %z;
							echo("FIX: Manually assigned " @ %name @ " to The Void zone " @ %z @ " (desc: '" @ %zoneDesc @ "')");
							break;
						}
					}
					if(%botZone == 0)
					{
						echo("ERROR: Could not find The Void zone for " @ %name @ " - listing all zones:");
						for(%z = 1; %z <= $numZones; %z++)
						{
							echo("  Zone " @ %z @ ": '" @ $Zone::Desc[%z] @ "'");
						}
					}
				}
			}
			
			$TownBotZone[%name] = %botZone;
			$TownBotSpawned[%name] = "";  // Not spawned yet
			
			// Add to registry
			$TownBotRegistry = $TownBotRegistry @ %name @ " ";
			
			// Debug logging for specific merchants that aren't spawning
			if(%name == "merchant4" || %name == "merchant5" || %name == "merchant14" || %name == "merchant19" || %name == "banker1")
			{
				echo("DEBUG: Registered bot: " @ %name @ " at position " @ %spawnPos @ " for zone: " @ %botZone @ " (" @ $Zone::Desc[%botZone] @ ")");
				if(%botZone == 0)
				{
					echo("ERROR: " @ %name @ " was assigned to zone 0 (no zone) - bot will NOT spawn!");
				}
				// Verify it was actually added to registry
				if(%name == "banker1")
				{
					%regPos = String::findSubStr($TownBotRegistry, "banker1");
					if(%regPos >= 0)
						echo("DEBUG: banker1 confirmed in registry at position " @ %regPos);
					else
						echo("ERROR: banker1 NOT found in registry after adding! Registry: '" @ $TownBotRegistry @ "'");
				}
			}
			else
			{
				echo("Registered bot: " @ %name @ " for zone: " @ %botZone @ " (" @ $Zone::Desc[%botZone] @ ")");
			}
		}
	}
	
	echo("===== InitTownBots() completed - Registered " @ GetWordCount($TownBotRegistry) @ " bots for dynamic loading =====");
	
	// Start periodic empty zone check to catch zones that should be empty but aren't being despawned
	// Runs on $ZoneEmptyCheckInterval, with throttled deep audits to reduce load on stable empty zones
	schedule("PeriodicEmptyZoneCheck();", 60);  // Start after 60 seconds to let server fully initialize
	
	// Start periodic bot team check to fix bots that are on team -1
	// This runs every 30 seconds to verify and fix bots that ended up on team -1
	schedule("PeriodicBotTeamCheck();", 30);  // Start after 30 seconds to let server fully initialize
}

// Determine which zone a bot position belongs to (returns zone index, 0 if no zone)
function GetBotZone(%position)
{
	%posX = GetWord(%position, 0);
	%posY = GetWord(%position, 1);
	%posZ = GetWord(%position, 2);
	
	// Debug: Check if zones are initialized
	if($numZones == "" || $numZones == 0)
	{
		echo("ERROR: GetBotZone - $numZones is not set! Zones may not be initialized. Position: " @ %position);
		return 0;
	}
	
	// Debug logging for first few calls to see what's happening
	%debugThis = false;
	// Check if this is a known problematic position
	if((%posX >= -2860 && %posX <= -2830 && %posY >= -1330 && %posY <= -1315) ||  // Sanctuary merchants (merchant4/5)
	   (%posX >= -1200 && %posX <= -1195 && %posY >= 1500 && %posY <= 1510) ||  // manager1
	   (%posX >= -2590 && %posX <= -2570 && %posY >= -1160 && %posY <= -1120) ||  // Black Market merchant14
	   (%posX >= -4500 && %posX <= -4450 && %posY >= 1815 && %posY <= 1860))  // Porellis banker8/merchant19/quest21
	{
		%debugThis = true;
		echo("DEBUG: GetBotZone checking position " @ %position @ " against " @ $numZones @ " zones");
	}
	
	for(%z = 1; %z <= $numZones; %z++)
	{
		%zonePos = $Zone::Marker[%z];
		%zoneX = GetWord(%zonePos, 0);
		%zoneY = GetWord(%zonePos, 1);
		%zoneZ = GetWord(%zonePos, 2);
		
		%length = $Zone::Length[%z];
		%width = $Zone::Width[%z];
		%height = $Zone::Height[%z];
		%sheight = $Zone::SHeight[%z];
		
		// Check if zone data is valid
		if(%zonePos == "" || %length == "" || %width == "")
		{
			if(%debugThis)
				echo("DEBUG: GetBotZone - Zone " @ %z @ " has invalid data (pos='" @ %zonePos @ "', length='" @ %length @ "', width='" @ %width @ "')");
			continue;
		}
		
		// Check if position is within zone bounds
		%halfLength = %length / 2;
		%halfWidth = %width / 2;
		
		%minX = %zoneX - %halfLength;
		%maxX = %zoneX + %halfLength;
		%minY = %zoneY - %halfWidth;
		%maxY = %zoneY + %halfWidth;
		%minZ = %zoneZ - %sheight;
		%maxZ = %zoneZ + %height;
		
		// RELAXED Z BOUNDS: Allow some tolerance for Z coordinate (bots may be slightly above/below zone floor)
		// Add 10 units of tolerance below the zone minimum to account for bots on slightly lower ground
		%minZ = %minZ - 10;
		
		if(%debugThis && %z <= 5)  // Debug first 5 zones
		{
			echo("DEBUG: Zone " @ %z @ " (" @ $Zone::Desc[%z] @ "): center=(" @ %zoneX @ " " @ %zoneY @ " " @ %zoneZ @ "), bounds X[" @ %minX @ " to " @ %maxX @ "], Y[" @ %minY @ " to " @ %maxY @ "], Z[" @ %minZ @ " to " @ %maxZ @ "]");
			echo("DEBUG: Position (" @ %posX @ " " @ %posY @ " " @ %posZ @ ") - X in bounds: " @ (%posX >= %minX && %posX <= %maxX) @ ", Y in bounds: " @ (%posY >= %minY && %posY <= %maxY) @ ", Z in bounds: " @ (%posZ >= %minZ && %posZ <= %maxZ));
		}
		
		if(%posX >= %minX && %posX <= %maxX &&
		   %posY >= %minY && %posY <= %maxY &&
		   %posZ >= %minZ && %posZ <= %maxZ)
		{
			if(%debugThis)
				echo("DEBUG: GetBotZone found zone " @ %z @ " (" @ $Zone::Desc[%z] @ ") for position " @ %position);
			return %z;
		}
	}
	
	// Debug logging if no zone found for known positions
	if(%debugThis)
	{
		echo("ERROR: GetBotZone - No zone found for position " @ %position);
		echo("  Checked " @ $numZones @ " zones but position is outside all zone bounds");
		echo("  Position: X=" @ %posX @ ", Y=" @ %posY @ ", Z=" @ %posZ);
	}
	
	return 0;  // No zone found
}

// Post-spawn initialization function for town bots
// Called after a 2-second delay to allow Player object to register in client list
function SpawnZoneBotPostSpawn(%aiName, %botName, %displayName, %zoneIndex)
{
	// For town bots, AI::spawn() creates a Player object, not a Drone
	// AI::getClientIdFromName() calls AI::getId() internally which fails for Player objects
	// So we skip it and search through all clients by display name directly
	// CRITICAL: Set expectedAiName BEFORE searching so it's available for validation
	%expectedAiName = "TownBot_" @ %botName;
	%clientId = "";
	
	// Try NEWgetClientByName first (faster if it works)
	%clientId = NEWgetClientByName(%displayName);
	
	// If that fails, search through all clients by display name
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%list = GetEveryoneIdList();
		for(%clientIndex = 0; GetWord(%list, %clientIndex) != -1; %clientIndex++)
		{
			%id = GetWord(%list, %clientIndex);
			%clientDisplayName = Client::getName(%id);
			if(String::ICompare(%displayName, %clientDisplayName) == 0)
			{
				// Found a client with matching display name
				// Check if it's already assigned to a different bot
				%existingBotInfoAiName = fetchData(%id, "BotInfoAiName");
				if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
				{
					// This client doesn't have a BotInfoAiName set - it's the newly spawned bot!
					%clientId = %id;
					break;
				}
				else if(%existingBotInfoAiName == %expectedAiName)
				{
					// Already assigned to this bot (shouldn't happen, but handle it)
					%clientId = %id;
					break;
				}
				else
				{
					// This client is already assigned to a different bot - skip it
				}
			}
		}
	}
	
	// If still not found, try brute-force search through recent client IDs (like RetryGetAIId does)
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		// Try checking a range of recent client IDs (bots typically get high IDs)
		// Allocatable BaseRep range: 2048 is the server's reserved slot, first
		// assignable id is Min+1; old hardcoded 2200 upper bound was 25 ids too far
		%startId = $BaseRepClientIdMin + 1;
		%endId = $BaseRepClientIdMax;
		
		for(%checkId = %startId; %checkId <= %endId; %checkId++)
		{
			// CRITICAL: Check if this client ID was recently freed BEFORE checking name match
			// This prevents reusing client IDs that are still being cleaned up, even if the name matches
			%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				if(%timeSinceFreed < 5) // 5 seconds for cleanup
				{
					continue; // Skip this client ID, cleanup still in progress
				}
				else
				{
					// Enough time has passed, clear the flag
					$ClientIdRecentlyFreed[%checkId] = "";
				}
			}
			
			// CRITICAL: Check if Player object exists and is valid BEFORE checking name match
			%playerObj = Client::getOwnedObject(%checkId);
			if(%playerObj == -1 || %playerObj == "")
			{
				continue; // Skip this client ID, player object not ready
			}
			
			// Player object exists - check if it's actually valid
			if(!isObject(%playerObj))
			{
				continue; // Skip this client ID, it's a shell bot being cleaned up
			}
			
			%checkName = Client::getName(%checkId);
			if(%checkName != "" && %checkName != -1)
			{
				if(String::ICompare(%displayName, %checkName) == 0)
				{
					// Found it! Check if it's already assigned to a bot
					%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
					if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
					{
						%clientId = %checkId;
						break;
					}
					else if(%existingBotInfoAiName == %expectedAiName)
					{
						%clientId = %checkId;
						break;
					}
				}
			}
		}
	}
	
	// If that fails, retry after a short delay (timing issue - Player object may not be registered yet)
	if(%clientId == -1 || %clientId == "")
	{
		echo("WARNING: SpawnZoneBotPostSpawn - Could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ "). Retrying...");
		// Use longer delay - bots need time to register in client list (1.0s additional delay after the initial 1.0s)
		schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 1.0);
		return;
	}
	
	// VALIDATE: Check if player object actually exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		echo("ERROR: SpawnZoneBotPostSpawn - Bot " @ %botName @ " spawned but player object is invalid! clientId=" @ %clientId);
		// Clean up the broken bot - use deleteObject on the player object if we have the clientId
		// Note: We don't use AI::delete() for town bots since they're Player objects, not Drones
		if(%clientId != -1 && %clientId != "")
		{
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != -1 && %playerObj != "")
				deleteObject(%playerObj);
		}
		return;
	}
	
	// CRITICAL: Check if BotInfoAiName is already set BEFORE we set it
	// This prevents false positives from our own assignment
	%existingBotInfoAiName = fetchData(%clientId, "BotInfoAiName");
	
	// CRITICAL: Double-check that this clientId isn't already assigned to a different town bot
	// Only check if it was already set (not by us) and doesn't match expected
	if(%existingBotInfoAiName != "" && %existingBotInfoAiName != -1 && %existingBotInfoAiName != "0" && %existingBotInfoAiName != %expectedAiName)
	{
		echo("ERROR: SpawnZoneBotPostSpawn - Found clientId " @ %clientId @ " is already assigned to different bot: " @ %existingBotInfoAiName @ " (expected: " @ %expectedAiName @ "). This should not happen!");
		// This is a critical error - we found a clientId that's already assigned to a different bot
		// This means our search logic failed. Delete the newly spawned bot and retry.
		if(%playerObj != -1 && %playerObj != "")
			deleteObject(%playerObj);
		schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 0.1);
		return;
	}
	
	// CRITICAL: Set BotInfoAiName IMMEDIATELY to prevent UpdateZone from processing this bot
	// This must happen before any zone detection runs, so IsTownBot() works correctly
	%aiNameFull = "TownBot_" @ %botName;
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	
	// CRITICAL: Set team IMMEDIATELY after spawn to prevent default team -1
	// AI::spawn() may default Player objects to team -1, so we must set it right away
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// CRITICAL: First, clear any stale enemy bot data from this client ID
	// This prevents stale data from making the town bot look like an enemy bot
	%staleSpawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
	if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
	{
		// Clear stale enemy bot data - the player object at this client ID is the new town bot, not the old enemy bot
		echo("WARNING: SpawnZoneBotPostSpawn - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
		// Clear all enemy bot data fields that could cause conflicts from new array
		$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
		$EnemyBotData[%clientId, "SpawnTime"] = "";
		$EnemyBotData[%clientId, "RemortStep"] = "";
		$EnemyBotData[%clientId, "QuestItems"] = "";
		$EnemyBotData[%clientId, "KeyItems"] = "";
		$EnemyBotData[%clientId, "Consumables"] = "";
		$EnemyBotData[%clientId, "Armor"] = "";
		$EnemyBotData[%clientId, "Accessories"] = "";
		$EnemyBotData[%clientId, "Other"] = "";
		$EnemyBotData[%clientId, "noExperienceFlag"] = "";
		$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
		$EnemyBotData[%clientId, "dumbAIflag"] = "";
		$EnemyBotData[%clientId, "frozen"] = "";
		$EnemyBotData[%clientId, "noBotSniff"] = "";
		$EnemyBotData[%clientId, "SpellCastStep"] = "";
		$EnemyBotData[%clientId, "LCKconsequence"] = "";
		// NOTE: Don't clear BotInfoAiName here - we just set it for the town bot above!
		// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
		$ClientData[%clientId, "SpawnBotInfo"] = "";
		$ClientData[%clientId, "SpawnTime"] = "";
	}
	
	// Reset retry counter on successful spawn
	$TownBotSpawned[%botName] = %clientId;
	
	// NEW: Register in O(1) lookup table for fast client ID -> bot info lookups
	RegisterTownBotClient(%clientId, %botName, %zoneIndex);
	
	// PRIORITY 2: Set $BotType cache for O(1) bot type detection
	$BotType[%clientId] = "town";
	
	// Add to TownBotList immediately so bots can be found for interaction
	$TownBotList = $TownBotList @ %clientId @ " ";
	
	// CRITICAL: Clear SpawnBotInfo to prevent town bots from being mistaken for enemy bots
	// Town bots should NOT have SpawnBotInfo set (only enemy bots have it)
	// This prevents zone change detection from killing town bots
	$TownBotData[%clientId, "SpawnBotInfo"] = "";
	$TownBotData[%clientId, "SpawnTime"] = "";
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", "");
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "SpawnTime"] = "";
	
	// CRITICAL: Set HasLoadedAndSpawned flag to indicate bot is fully initialized
	// This allows DespawnZoneBots() to identify fully loaded bots
	storeData(%clientId, "HasLoadedAndSpawned", True);
	
	// CRITICAL: Schedule a team verification after a short delay to catch UpdateTeam() overriding it
	schedule("VerifyTownBotTeam(" @ %clientId @ ", \"" @ %botName @ "\", " @ %botTeam @ ");", 0.2);
	
	// Initialize bot post-spawn (must store clientId in $TownBotSpawned first so InitTownBotPostSpawn can find it)
	// InitTownBotPostSpawn expects to find the clientId in $TownBotSpawned[%botName]
	schedule("InitTownBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\");", 0.1);
	// Schedule item mounting after post-spawn completes (allow time for initialization)
	schedule("InitTownBotItemsForBot(" @ %clientId @ ", \"" @ %botName @ "\");", 1.5);
	echo("Spawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ")");
}

// Retry function for getting client ID when timing issues occur
// This handles cases where AI::spawn() succeeds but we can't get the client ID immediately
function RetryGetAIId(%aiName, %botName, %displayName, %zoneIndex)
{
	// For town bots, they are Player objects, not Drones
	// AI::getClientIdFromName() calls AI::getId() internally which fails for Player objects
	// So we skip it and use NEWgetClientByName() directly
	%clientId = NEWgetClientByName(%displayName);
	
	// If still not found, try searching through all client IDs directly (brute force approach)
	// This handles cases where GetEveryoneIdList() hasn't updated yet
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		// Try checking a range of recent client IDs (bots typically get high IDs)
		// Allocatable BaseRep range: 2048 is the server's reserved slot, first
		// assignable id is Min+1; old hardcoded 2200 upper bound was 25 ids too far
		%startId = $BaseRepClientIdMin + 1;
		%endId = $BaseRepClientIdMax;
		
		for(%checkId = %startId; %checkId <= %endId; %checkId++)
		{
			// CRITICAL: Check if this client ID was recently freed BEFORE checking name match
			// This prevents reusing client IDs that are still being cleaned up, even if the name matches
			%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				if(%timeSinceFreed < 5) // 5 seconds for cleanup
				{
					continue; // Skip this client ID, cleanup still in progress
				}
				else
				{
					// Enough time has passed, clear the flag
					$ClientIdRecentlyFreed[%checkId] = "";
				}
			}
			
			// CRITICAL: Check if Player object exists and is valid BEFORE checking name match
			%playerObj = Client::getOwnedObject(%checkId);
			if(%playerObj == -1 || %playerObj == "")
			{
				continue; // Skip this client ID, player object not ready
			}
			
			// Player object exists - check if it's actually valid
			if(!isObject(%playerObj))
			{
				continue; // Skip this client ID, it's a shell bot being cleaned up
			}
			
			%checkName = Client::getName(%checkId);
			if(%checkName != "" && %checkName != -1)
			{
				if(String::ICompare(%displayName, %checkName) == 0)
				{
					// Found it! Check if it's already assigned to a bot
					%existingBotInfoAiName = fetchData(%checkId, "BotInfoAiName");
					if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
					{
						%clientId = %checkId;
						break;
					}
				}
			}
		}
	}
	
	// If still not found, try one more time with GetEveryoneIdList() (might have updated)
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%clientId = NEWgetClientByName(%displayName);
	}
	
	// If still not found after all attempts, try one more brute-force pass over everyone (names may include prefixes)
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%listAll = GetEveryoneIdList();
		%expectedDisplayName = $BotInfo[%botName, NAME];
		for(%iBF = 0; (%cidBF = GetWord(%listAll, %iBF)) != -1; %iBF++)
		{
			if(%cidBF == "" || %cidBF == -1)
				continue;
			%pObjBF = Client::getOwnedObject(%cidBF);
			if(%pObjBF == -1 || %pObjBF == "")
				continue;
			%nmBF = Client::getName(%cidBF);
			if(%nmBF == "" || %nmBF == -1)
				continue;
			// Skip real players with save files
			if(isFile("temp\\" @ %nmBF @ ".cs"))
				continue;
			// Match exact displayName, or expected town bot name, or case-insensitive substring match
			if(String::ICompare(%nmBF, %displayName) == 0 ||
			   (%expectedDisplayName != "" && String::ICompare(%nmBF, %expectedDisplayName) == 0) ||
			   (%expectedDisplayName != "" && String::findSubStr(%nmBF, %expectedDisplayName) != -1))
			{
				%existingBotInfoAiName = fetchData(%cidBF, "BotInfoAiName");
				if(%existingBotInfoAiName == "" || %existingBotInfoAiName == -1 || %existingBotInfoAiName == "0")
				{
					%clientId = %cidBF;
					break;
				}
			}
		}
	}

	// If still not found after all attempts, check retry count
	if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
	{
		%retryCount = $TownBotRetryGetAIIdCount[%botName];
		if(%retryCount == "")
			%retryCount = 0;
		
		if(%retryCount < 10)
		{
			// Try again with exponential backoff
			$TownBotRetryGetAIIdCount[%botName] = %retryCount + 1;
			%delay = 0.5 * (%retryCount + 1);  // up to 5s by attempt 10
			echo("WARNING: RetryGetAIId - Could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ ", aiName: " @ %aiName @ "). Retrying (attempt " @ (%retryCount + 1) @ "/10) in " @ %delay @ "s...");
			schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", %delay);
			return;
		}
		else
		{
			echo("ERROR: RetryGetAIId - Still could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ ", aiName: " @ %aiName @ ") after 10 retries.");
			echo("  AI::spawn() may have succeeded but the Player object is not yet registered in the client list.");
			$TownBotRetryGetAIIdCount[%botName] = "";
			return;
		}
	}
	
	// Reset retry counter on success
	$TownBotRetryGetAIIdCount[%botName] = "";
	
	// Bot was found on retry - continue with initialization
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		echo("ERROR: RetryGetAIId - Bot " @ %botName @ " found but player object is invalid! clientId=" @ %clientId);
		return;
	}
	
	// Mark invulnerability during init to avoid early death before full setup
	storeData(%clientId, "SpawnInvuln", True);
	
	// CRITICAL: Check if this client ID is already in use by a REAL PLAYER (not a bot)
	// If a real player is using this client ID, we cannot use it for a bot
	// NOTE: For town bots, we check if the name matches known town bot patterns or if it's a real player name
	%actualName = Client::getName(%clientId);
	if(%actualName != "" && %actualName != -1)
	{
		// Check if the name matches enemy bot patterns (enemy bots have prefixes like "Alien", "Admin", "Demon", etc.)
		%isEnemyBotName = HasEnemyBotNamePrefix(%actualName);
		
		// Check if isRPGAI() returns true (bot data is set)
		%hasBotData = isRPGAI(%clientId);
		
		// If it's not an enemy bot name pattern and doesn't have bot data, it might be a real player
		// BUT: Town bots don't have enemy bot name patterns, so we need to check differently
		// For town bots, we check if the name matches the expected display name for this town bot
		%expectedDisplayName = $BotInfo[%botName, NAME];
		%isExpectedTownBot = false;
		if(%expectedDisplayName != "" && %expectedDisplayName != -1)
		{
			// Check if the actual name matches the expected town bot display name (with or without zone prefix)
			if(String::ICompare(%actualName, %expectedDisplayName) == 0 ||
			   String::findSubStr(%actualName, %expectedDisplayName) >= 0)
			{
				%isExpectedTownBot = true;
			}
		}
		
		// If it's not an enemy bot, doesn't have bot data, and doesn't match expected town bot name, it's likely a real player
		if(!%isEnemyBotName && !%hasBotData && !%isExpectedTownBot)
		{
			// This is likely a REAL PLAYER using this client ID - cannot spawn bot here
			echo("ERROR: RetryGetAIId - Client ID " @ %clientId @ " is already in use by real player '" @ %actualName @ "'. Cannot spawn town bot " @ %botName @ " (expected: '" @ %expectedDisplayName @ "'). Aborting...");
			// Clean up any bot data that might have been set
			storeData(%clientId, "SpawnBotInfo", "");
			storeData(%clientId, "BotInfoAiName", "");
			$TownBotData[%clientId, "SpawnBotInfo"] = "";
			$TownBotData[%clientId, "BotInfoAiName"] = "";
			$TownBotSpawned[%botName] = "";
			return;  // Abort spawn
		}
	}
	
	// CRITICAL: Check for ghost client ID (empty name Player object)
	// BUT: We need to verify this is actually a ghost and not just a bot whose name hasn't been set yet
	if(%actualName == "" || %actualName == -1)
	{
		// Name is empty - check if this AI name is already in use by another active bot
		%otherBotId = AI::getClientIdFromName(%aiName);
		if(%otherBotId != -1 && %otherBotId != "" && %otherBotId != %clientId)
		{
			%otherBotName = Client::getName(%otherBotId);
			if(%otherBotName != "" && %otherBotName != -1)
			{
				// Another bot with this AI name exists - this is a ghost
				echo("WARNING: RetryGetAIId - Detected ghost client ID " @ %clientId @ " (empty name) for bot " @ %botName @ ". Another bot exists (clientId=" @ %otherBotId @ "). Cleaning up ghost...");
				CleanupGhostClientId(%clientId, %aiName);
				return;
			}
		}
		
		// Name is empty but no other bot with this AI name exists
		// This might be a bot whose name hasn't been set yet - wait a bit and check again
		schedule("CheckGhostClientIdDelayed(" @ %clientId @ ", \"" @ %aiName @ "\");", 0.5);
		// Continue processing - if it's truly a ghost, the delayed check will clean it up
	}
	
	// CRITICAL: Set BotInfoAiName IMMEDIATELY to prevent UpdateTeam() from changing team
	// This must happen BEFORE setting team so UpdateTeam() recognizes it as a town bot
	%aiNameFull = "TownBot_" @ %botName;
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	
	// CRITICAL: Set team IMMEDIATELY to prevent default team -1
	// AI::spawn() may default Player objects to team -1, so we must set it right away
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// Verify team was set correctly (debug check)
	%actualTeam = GameBase::getTeam(%clientId);
	if(%actualTeam != %botTeam)
	{
		echo("WARNING: RetryGetAIId - Team mismatch for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam @ ". Re-setting...");
		GameBase::setTeam(%clientId, %botTeam);
	}
	
	// CRITICAL: Check if this client ID was previously used by an enemy bot
	// Only clear if the enemy bot is actually dead/stale (not still alive)
	%staleSpawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
	%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
	if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
	{
		// Verify the enemy bot is actually dead by checking if it's still an active enemy bot
		%currentSpawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
		%currentBotInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
		
		// If the current bot is an active enemy bot (has SpawnBotInfo but different BotInfoAiName),
		// then this is a conflict - don't clear data, log error instead
		if(%currentSpawnBotInfo != "" && %currentSpawnBotInfo != "0" && %currentSpawnBotInfo != -1 && %currentBotInfoAiName != "")
		{
			echo("ERROR: RetryGetAIId - Client ID " @ %clientId @ " is still in use by an active enemy bot! Cannot spawn town bot " @ %botName @ ". This should not happen - AI::spawn() should not reuse active client IDs.");
			// Don't clear data - the enemy bot is still alive
			// Delete the town bot that was just spawned
			if(%playerObj != -1 && %playerObj != "")
				deleteObject(%playerObj);
			return;
		}
		
		// Enemy bot is dead/stale - safe to clear
		echo("WARNING: RetryGetAIId - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
		// Clear all enemy bot data fields that could cause conflicts from new array
		$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
		$EnemyBotData[%clientId, "SpawnTime"] = "";
		$EnemyBotData[%clientId, "RemortStep"] = "";
		$EnemyBotData[%clientId, "QuestItems"] = "";
		$EnemyBotData[%clientId, "KeyItems"] = "";
		$EnemyBotData[%clientId, "Consumables"] = "";
		$EnemyBotData[%clientId, "Armor"] = "";
		$EnemyBotData[%clientId, "Accessories"] = "";
		$EnemyBotData[%clientId, "Other"] = "";
		$EnemyBotData[%clientId, "noExperienceFlag"] = "";
		$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
		$EnemyBotData[%clientId, "dumbAIflag"] = "";
		$EnemyBotData[%clientId, "frozen"] = "";
		$EnemyBotData[%clientId, "noBotSniff"] = "";
		$EnemyBotData[%clientId, "SpellCastStep"] = "";
		$EnemyBotData[%clientId, "LCKconsequence"] = "";
		$EnemyBotData[%clientId, "BotInfoAiName"] = "";
		// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
		$ClientData[%clientId, "SpawnBotInfo"] = "";
		$ClientData[%clientId, "SpawnTime"] = "";
	}
	
	// Reset retry counter on successful spawn
	$TownBotSpawnRetry[%botName] = "";
	
	$TownBotSpawned[%botName] = %clientId;
	
	// NEW: Register in O(1) lookup table for fast client ID -> bot info lookups
	RegisterTownBotClient(%clientId, %botName, %zoneIndex);
	
	$BotType[%clientId] = "town";  // PRIORITY 2: Set $BotType cache
	$TownBotList = $TownBotList @ %clientId @ " ";
	// CRITICAL: Set BotInfoAiName in $TownBotData FIRST so GetClientDataType identifies it as a town bot
	// This must happen before clearing SpawnBotInfo to ensure proper type detection
	// CRITICAL: Must use "TownBot_" prefix to match SpawnZoneBots format
	%aiNameFull = "TownBot_" @ %botName;
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	$TownBotData[%clientId, "SpawnBotInfo"] = "";
	$TownBotData[%clientId, "SpawnTime"] = "";
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", "");
	// Also directly clear from $ClientData to ensure it's gone (migration mode)
	$ClientData[%clientId, "SpawnBotInfo"] = "";
	$ClientData[%clientId, "SpawnTime"] = "";
	
	// CRITICAL: Set team IMMEDIATELY after setting BotInfoAiName to prevent UpdateTeam() from changing it
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// Verify team was set correctly (debug check)
	%actualTeam = GameBase::getTeam(%clientId);
	if(%actualTeam != %botTeam)
	{
		echo("WARNING: RetryGetAIId - Team mismatch for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam @ ". Re-setting...");
		GameBase::setTeam(%clientId, %botTeam);
		// Verify again after re-setting
		%actualTeam2 = GameBase::getTeam(%clientId);
		if(%actualTeam2 != %botTeam)
		{
			echo("ERROR: RetryGetAIId - Team still incorrect after re-set for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam2 @ ".");
		}
	}
	
	// CRITICAL: Set HasLoadedAndSpawned flag to indicate bot is fully initialized
	// This allows DespawnZoneBots() to identify fully loaded bots
	storeData(%clientId, "HasLoadedAndSpawned", True);
	// Clear spawn invulnerability now that init is complete
	storeData(%clientId, "SpawnInvuln", "");
	// Clear name-based invuln guard
	$SpawnInvulnByName[%displayName] = "";
	// Clear any lingering no-drop flags now that spawn succeeded
	storeData(%clientId, "noDropLootbagFlag", "");
	$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
	$ClientData[%clientId, "noDropLootbagFlag"] = "";
	// Clear any lingering no-drop flags now that spawn succeeded
	storeData(%clientId, "noDropLootbagFlag", "");
	$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
	$ClientData[%clientId, "noDropLootbagFlag"] = "";
	// Clear name-based invuln guard
	$SpawnInvulnByName[%displayName] = "";
	
	// CRITICAL: Schedule a team verification after a short delay to catch UpdateTeam() overriding it
	schedule("VerifyTownBotTeam(" @ %clientId @ ", \"" @ %botName @ "\", " @ %botTeam @ ");", 0.2);
	
	schedule("InitTownBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\");", 0.1);
	schedule("InitTownBotItemsForBot(" @ %clientId @ ", \"" @ %botName @ "\");", 1.5);
	echo("Spawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ") - found on retry");
}

// Spawn a single bot (helper function for retry after cleanup)
function SpawnSingleZoneBot(%botName, %zoneIndex)
{
	// Skip invalid bot names (like "0" which can appear in lists)
	if(%botName == "" || %botName == "0" || %botName == -1)
		return;
	
	if($TownBotZone[%botName] != %zoneIndex || $TownBotSpawned[%botName] != "")
		return;  // Bot not for this zone or already spawned
	
		// Spawn this bot
		%aiName = "TownBot_" @ %botName;
		
		// Get race and validate it exists
		%botRace = $BotInfo[%botName, RACE];
		if(%botRace == "" || %botRace == -1)
		{
			echo("ERROR: SpawnSingleZoneBot - Bot " @ %botName @ " has no RACE defined, defaulting to Human");
			%botRace = "Human";
		}
		
		%armor = $RaceToArmorType[%botRace];
		if(%armor == "" || %armor == -1)
		{
			echo("ERROR: SpawnSingleZoneBot - Could not find armor for race '" @ %botRace @ "' for bot " @ %botName @ ", defaulting to MaleHumanArmor");
			%armor = "MaleHumanArmor";
		}
		
		%spawnPos = $BotInfo[%botName, SPAWN_POS];
		%spawnRot = $BotInfo[%botName, SPAWN_ROT];
		
		// Use stored display name if available (preserves zone prefix like "Yuliple banker")
		// Otherwise fall back to $BotInfo[NAME] (which may be just "banker")
		%displayName = $TownBotDisplayName[%botName];
		if(%displayName == "" || %displayName == -1 || %displayName == "0")
			%displayName = $BotInfo[%botName, NAME];
		
		// CRITICAL INTEGRATION: Check server capacity before spawning
		%predictedId = PlayerManager::getFreeId();
		if(%predictedId == -1)
		{
			echo("CRITICAL: SpawnSingleZoneBot - Server is FULL! Aborting spawn for " @ %botName);
			return;
		}
		
		// RACE FIX (see SpawnZoneBots): a same-display-name AI lingering from a just-despawned town bot
		// (DespawnZoneBots defers its player deleteObject 1s) makes AI::spawn() error "already exists".
		// Tear it down first (engine truth via Player::isAiControlled, so a real player is never touched)
		// and cancel its pending deferred despawn-delete, so the spawn below succeeds.
		%existingSingleId = NEWgetClientByName(%displayName);
		if(%existingSingleId != -1 && %existingSingleId != "" && Player::isAiControlled(%existingSingleId))
		{
			$DespawnValidationToken[%existingSingleId] = "";
			%staleSingleObj = Client::getOwnedObject(%existingSingleId);
			if(%staleSingleObj != "" && %staleSingleObj != -1 && isObject(%staleSingleObj))
				deleteObject(%staleSingleObj);
			echo("[ZONE SPAWN] Cleared stale TownBot_" @ %botName @ " (clientId=" @ %existingSingleId @ ") before single respawn");
		}

		if(AI::spawn(%aiName, %armor, %spawnPos, %spawnRot, %displayName, "male2") != "false")
	{
		// For town bots, AI::spawn() creates a Player object, not a Drone
		// AI::getClientIdFromName() calls AI::getId() internally which fails for Player objects
		// So we skip it and use NEWgetClientByName() directly
		%clientId = NEWgetClientByName(%displayName);
		
		// If that fails, retry after a short delay (timing issue - Player object may not be registered yet)
		if(%clientId == -1 || %clientId == "" || %clientId == "False" || %clientId == "false")
		{
			echo("WARNING: SpawnSingleZoneBot - Could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ ", aiName: " @ %aiName @ "). Retrying...");
			schedule("RetryGetAIId(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 0.1);
			return;
		}
		
		// VALIDATE: Check if player object actually exists
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == -1 || %playerObj == "")
		{
			echo("ERROR: SpawnSingleZoneBot - Bot " @ %botName @ " spawned but player object is invalid! clientId=" @ %clientId);
			// Clean up the broken bot - use deleteObject on the player object if we have the clientId
			// Note: We don't use AI::delete() for town bots since they're Player objects, not Drones
			if(%clientId != -1 && %clientId != "")
			{
				%playerObj = Client::getOwnedObject(%clientId);
				if(%playerObj != -1 && %playerObj != "")
					deleteObject(%playerObj);
			}
			return;
		}
		
		// CRITICAL: Set BotInfoAiName IMMEDIATELY to prevent UpdateTeam() from setting team to 1
		// This must happen before UpdateTeam() runs (called by Game::playerSpawned())
		%aiNameFull = "TownBot_" @ %botName;
		storeData(%clientId, "BotInfoAiName", %aiNameFull);
		$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
		
		// CRITICAL: Set team IMMEDIATELY after spawn to prevent default team -1
		// AI::spawn() may default Player objects to team -1, so we must set it right away
		// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
		%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
		GameBase::setTeam(%clientId, %botTeam);
		
		// CRITICAL: First, clear any stale enemy bot data from this client ID
		// This prevents stale data from making the town bot look like an enemy bot
		// Note: The player object at %clientId is the NEW town bot, so we can safely clear stale enemy bot data
		%staleSpawnBotInfo = $EnemyBotData[%clientId, "SpawnBotInfo"];
		%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
		if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
		{
			// Clear stale enemy bot data - the player object at this client ID is the new town bot, not the old enemy bot
			echo("WARNING: SpawnSingleZoneBot - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
			// Clear all enemy bot data fields that could cause conflicts from new array
			$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
			$EnemyBotData[%clientId, "SpawnTime"] = "";
			$EnemyBotData[%clientId, "RemortStep"] = "";
			$EnemyBotData[%clientId, "QuestItems"] = "";
			$EnemyBotData[%clientId, "KeyItems"] = "";
			$EnemyBotData[%clientId, "Consumables"] = "";
			$EnemyBotData[%clientId, "Armor"] = "";
			$EnemyBotData[%clientId, "Accessories"] = "";
			$EnemyBotData[%clientId, "Other"] = "";
			$EnemyBotData[%clientId, "noExperienceFlag"] = "";
			$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
			$EnemyBotData[%clientId, "dumbAIflag"] = "";
			$EnemyBotData[%clientId, "frozen"] = "";
			$EnemyBotData[%clientId, "noBotSniff"] = "";
			$EnemyBotData[%clientId, "SpellCastStep"] = "";
			$EnemyBotData[%clientId, "LCKconsequence"] = "";
			$EnemyBotData[%clientId, "BotInfoAiName"] = "";
			// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
			$ClientData[%clientId, "SpawnBotInfo"] = "";
			$ClientData[%clientId, "SpawnTime"] = "";
		}
		
		// NOW check if there's an active enemy bot using this client ID (after clearing stale data)
		%currentSpawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
		%currentBotInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
		
		// Check if there's an active enemy bot using this client ID
		// Verify by checking if the player object at this client ID is actually an enemy bot
		%checkPlayerObj = Client::getOwnedObject(%clientId);
		%checkIsAlive = (%checkPlayerObj != "" && %checkPlayerObj != -1);
		%hasActiveEnemyBot = (%checkIsAlive && %currentSpawnBotInfo != "" && %currentSpawnBotInfo != "0" && %currentSpawnBotInfo != -1 && %currentBotInfoAiName != "" && %currentBotInfoAiName != %botName);
		
		if(%hasActiveEnemyBot)
		{
			// Active enemy bot conflict - delete and retry (up to 3 attempts)
			%retryCount = $TownBotSpawnRetry[%botName];
			if(%retryCount == "")
				%retryCount = 0;
			
			if(%retryCount < 3)
			{
				echo("WARNING: SpawnSingleZoneBot - Client ID " @ %clientId @ " is in use by an active enemy bot. Deleting and retrying spawn for town bot " @ %botName @ " (attempt " @ (%retryCount + 1) @ "/3).");
				// Delete the bot that was just spawned
				if(%playerObj != -1 && %playerObj != "")
					deleteObject(%playerObj);
				// Increment retry counter and retry
				$TownBotSpawnRetry[%botName] = %retryCount + 1;
				schedule("SpawnSingleZoneBot(\"" @ %botName @ "\", " @ %zoneIndex @ ");", 0.2);
				return;
			}
			else
			{
				// Max retries reached - give up
				echo("ERROR: SpawnSingleZoneBot - Failed to spawn town bot " @ %botName @ " after 3 retries. Client ID " @ %clientId @ " is still in use by an active enemy bot.");
				if(%playerObj != -1 && %playerObj != "")
					deleteObject(%playerObj);
				$TownBotSpawnRetry[%botName] = "";
				return;
			}
		}
		
		// Reset retry counter on successful spawn
		$TownBotSpawnRetry[%botName] = "";
		
		// Check if there's stale enemy bot data (dead/stale bot)
		// Also check $ClientData (old array) since we're in migration mode
		%staleSpawnBotInfoOld = $ClientData[%clientId, "SpawnBotInfo"];
		if(%staleSpawnBotInfo != "" && %staleSpawnBotInfo != "0" && %staleSpawnBotInfo != -1 || (%staleSpawnBotInfoOld != "" && %staleSpawnBotInfoOld != "0" && %staleSpawnBotInfoOld != -1))
		{
			// Enemy bot is dead/stale - safe to clear
			echo("WARNING: SpawnSingleZoneBot - Client ID " @ %clientId @ " was previously used by an enemy bot. Clearing stale enemy bot data for town bot " @ %botName @ ".");
			// Clear all enemy bot data fields that could cause conflicts from new array
			$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
			$EnemyBotData[%clientId, "SpawnTime"] = "";
			$EnemyBotData[%clientId, "RemortStep"] = "";
			$EnemyBotData[%clientId, "QuestItems"] = "";
			$EnemyBotData[%clientId, "KeyItems"] = "";
			$EnemyBotData[%clientId, "Consumables"] = "";
			$EnemyBotData[%clientId, "Armor"] = "";
			$EnemyBotData[%clientId, "Accessories"] = "";
			$EnemyBotData[%clientId, "Other"] = "";
			$EnemyBotData[%clientId, "noExperienceFlag"] = "";
			$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
			$EnemyBotData[%clientId, "dumbAIflag"] = "";
			$EnemyBotData[%clientId, "frozen"] = "";
			$EnemyBotData[%clientId, "noBotSniff"] = "";
			$EnemyBotData[%clientId, "SpellCastStep"] = "";
			$EnemyBotData[%clientId, "LCKconsequence"] = "";
			$EnemyBotData[%clientId, "BotInfoAiName"] = "";
			// CRITICAL: Also clear from $ClientData (old array) to prevent stale data from being read
			$ClientData[%clientId, "SpawnBotInfo"] = "";
			$ClientData[%clientId, "SpawnTime"] = "";
		}
		
		// Reset retry counter on successful spawn
		$TownBotSpawnRetry[%botName] = "";
		
		$TownBotSpawned[%botName] = %clientId;
		
		// NEW: Register in O(1) lookup table for fast client ID -> bot info lookups
		RegisterTownBotClient(%clientId, %botName, %zoneIndex);
		
		$BotType[%clientId] = "town";  // PRIORITY 2: Set $BotType cache
		
		// Add to TownBotList immediately so bots can be found for interaction
		$TownBotList = $TownBotList @ %clientId @ " ";
		
		// CRITICAL: Set BotInfoAiName in $TownBotData FIRST so GetClientDataType identifies it as a town bot
		// This must happen before clearing SpawnBotInfo to ensure proper type detection
		// CRITICAL: Must use "TownBot_" prefix to match SpawnZoneBots format
		%aiNameFull = "TownBot_" @ %botName;
		$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
		storeData(%clientId, "BotInfoAiName", %aiNameFull);
		
		// CRITICAL: Clear SpawnBotInfo to prevent town bots from being mistaken for enemy bots
		// Town bots should NOT have SpawnBotInfo set (only enemy bots have it)
		// This prevents zone change detection from killing town bots
		// Clear from both new array and old array to ensure no stale data
		$TownBotData[%clientId, "SpawnBotInfo"] = "";
		$TownBotData[%clientId, "SpawnTime"] = "";
		storeData(%clientId, "SpawnBotInfo", "");
		storeData(%clientId, "SpawnTime", ""); // Also clear SpawnTime to be safe
		// Also directly clear from $ClientData to ensure it's gone (migration mode)
		$ClientData[%clientId, "SpawnBotInfo"] = "";
		$ClientData[%clientId, "SpawnTime"] = "";
		
		// CRITICAL: Set team IMMEDIATELY after setting BotInfoAiName to prevent UpdateTeam() from changing it
		// Team was already set earlier, but set it again here to ensure it's correct after all data clearing
		// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
		%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
		GameBase::setTeam(%clientId, %botTeam);
		
		// Verify team was set correctly (debug check)
		%actualTeam = GameBase::getTeam(%clientId);
		if(%actualTeam != %botTeam)
		{
			echo("WARNING: SpawnSingleZoneBot - Team mismatch for " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ ", got " @ %actualTeam @ ". Re-setting...");
			GameBase::setTeam(%clientId, %botTeam);
		}
		
		// CRITICAL: Set HasLoadedAndSpawned flag to indicate bot is fully initialized
		// This allows DespawnZoneBots() to identify fully loaded bots
		storeData(%clientId, "HasLoadedAndSpawned", True);
		
		schedule("InitTownBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\");", 0.1);
		schedule("InitTownBotItemsForBot(" @ %clientId @ ", \"" @ %botName @ "\");", 1.5);
		echo("Spawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ")");
	}
}

// Helper function to extract a short zone name for bot display names
// Examples: "Yuliple City" -> "Yuliple", "Kingdom of Kronos" -> "Kronos", "Empress Sanctuary" -> "Empress"
function GetZoneShortName(%zoneDesc)
{
	if(%zoneDesc == "" || %zoneDesc == -1)
		return "";
	
	if(String::ICompare(%zoneDesc, "The Sandbox") == 0 || String::ICompare(%zoneDesc, "Sandbox") == 0)
		return "";
	
	// Common patterns - use GetWord for reliable parsing:
	// "Kingdom of Kronos" -> "Kronos" (take 3rd word)
	%word0 = GetWord(%zoneDesc, 0);
	%word1 = GetWord(%zoneDesc, 1);
	if(%word0 == "Kingdom" && %word1 == "of")
	{
		%word2 = GetWord(%zoneDesc, 2);
		if(%word2 != "" && %word2 != -1)
			return %word2; // Return "Kronos"
	}
	
	// "X City" -> "X" (take first word)
	if(String::findSubStr(%zoneDesc, " City") != -1)
		return %word0;
	
	// "X Sanctuary" -> "X" (take first word)
	if(String::findSubStr(%zoneDesc, " Sanctuary") != -1)
		return %word0;
	
	// Default: return first word (usually the city/zone name)
	return %word0;
}

// ============================================================================
// DELAYED ZONE SPAWN VERIFICATION
// Prevents spawning bots when players quickly pass through a zone
// ============================================================================
$ZoneSpawnDelay = 10;      // Seconds to wait before spawning ENEMY ZONES (set to 0 to disable delay)
$TownSpawnDelay = 1;       // Seconds to wait before spawning TOWN ZONES (shorter delay for town bots)

// Called when first player enters zone - schedules spawn verification
function ScheduleZoneSpawn(%zoneIndex)
{
	if(%zoneIndex == 0 || %zoneIndex == "")
		return;
	
	// EXEMPTION: Colloseum zone is managed by the Seal Battle system in remortseal.cs
	// Do not use normal spawn verification - let the seal battle handle its own bots
	// NOTE: Use zone description check instead of hardcoded index (zone indices can change)
	%zoneDesc = $Zone::Desc[%zoneIndex];
	if(String::findSubStr(%zoneDesc, "Colloseum") >= 0 || String::findSubStr(%zoneDesc, "Colosseum") >= 0)
		return;
	
	// Mark that we have a pending spawn for this zone
	$ZoneSpawnPending[%zoneIndex] = getSimTime();
	
	// Determine if this is a town zone (has town bots) or enemy zone
	%isTownZone = false;
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
	{
		if($TownBotZone[%botName] == %zoneIndex)
		{
			%isTownZone = true;
			break;
		}
	}
	
	// Get appropriate delay based on zone type
	%delay = $ZoneSpawnDelay;  // Default to enemy zone delay (10s)
	if(%isTownZone)
	{
		%delay = $TownSpawnDelay;  // Town zones use shorter delay (1s)
		if(%delay == "" || %delay < 0)
			%delay = 1;
	}
	else
	{
		if(%delay == "" || %delay < 0)
			%delay = 10;  // Default to 10 seconds for enemy zones
	}
	
	if(%delay == 0)
	{
		// No delay - spawn immediately (legacy behavior)
		SpawnZoneBots(%zoneIndex);
		$ZoneSpawnPending[%zoneIndex] = "";
	}
	else
	{
		// Schedule verification after delay
		%zoneDesc = $Zone::Desc[%zoneIndex];
		%zoneType = "enemy";
		if(%isTownZone)
			%zoneType = "town";
		echo("[ZONE SPAWN] Scheduling " @ %zoneType @ " spawn verification for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") in " @ %delay @ " seconds");
		schedule("VerifyAndSpawnZoneBots(" @ %zoneIndex @ ");", %delay);
	}
}

// Called after delay - verifies players are still in zone before spawning
function VerifyAndSpawnZoneBots(%zoneIndex)
{
	if(%zoneIndex == 0 || %zoneIndex == "")
		return;
	
	%zoneDesc = $Zone::Desc[%zoneIndex];
	
	// Check if spawn was cancelled (player left before delay expired)
	if($ZoneSpawnPending[%zoneIndex] == "")
	{
		echo("[ZONE SPAWN] Spawn cancelled for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - players left before delay expired");
		return;
	}
	
	// Verify players are still in zone
	%playerCount = $ZonePlayerCount[%zoneIndex];
	if(%playerCount <= 0)
	{
		echo("[ZONE SPAWN] Aborting spawn for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - zone is now empty");
		$ZoneSpawnPending[%zoneIndex] = "";
		return;
	}
	
	// Players still in zone - proceed with spawn
	echo("[ZONE SPAWN] Verified players in zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - spawning bots now");
	$ZoneSpawnPending[%zoneIndex] = "";
	SpawnZoneBots(%zoneIndex);
}

// Cancel pending spawn when zone becomes empty
function CancelPendingZoneSpawn(%zoneIndex)
{
	if($ZoneSpawnPending[%zoneIndex] != "")
	{
		%zoneDesc = $Zone::Desc[%zoneIndex];
		echo("[ZONE SPAWN] Cancelling pending spawn for zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") - zone emptied");
		$ZoneSpawnPending[%zoneIndex] = "";
	}
}

// Spawn all bots for a specific zone
function SpawnZoneBots(%zoneIndex)
{
	%zoneDesc = $Zone::Desc[%zoneIndex];
	%zoneShortName = GetZoneShortName(%zoneDesc);
	
	if(%zoneIndex == 0 || %zoneIndex == "")
		return;
	
	// Clear any pending despawn for this zone (Tribes doesn't have cancel() function)
	// Set flag to empty to prevent scheduled despawn from executing
	$ZoneBotDespawnSchedule[%zoneIndex] = "";
	
	%wordCount = GetWordCount($TownBotRegistry);
	
	for(%i = 0; %i < %wordCount; %i++)
	{
		%botName = GetWord($TownBotRegistry, %i);
		
		// Skip invalid bot names (like "0" which can appear in lists)
		if(%botName == "" || %botName == "0" || %botName == -1)
			continue;
		
		if($TownBotZone[%botName] == %zoneIndex && $TownBotSpawned[%botName] == "")
		{
			// Spawn this bot
			%aiName = "TownBot_" @ %botName;
			
			// Validate aiName is not empty or "0" (shouldn't happen, but safety check)
			if(%aiName == "" || %aiName == "0" || %aiName == "TownBot_0" || %aiName == -1)
			{
				echo("ERROR: SpawnZoneBots - Invalid aiName constructed from botName='" @ %botName @ "', skipping");
				continue;
			}
			
			// Get race and validate it exists
			%botRace = $BotInfo[%botName, RACE];
			if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Bot=" @ %botName @ " | $BotInfo[RACE]='" @ %botRace @ "'");
			if(%botRace == "" || %botRace == -1)
			{
				echo("ERROR: SpawnZoneBots - Bot " @ %botName @ " has no RACE defined, defaulting to MaleHuman");
				%botRace = "MaleHuman";
			}
			
			%armor = $RaceToArmorType[%botRace];
			if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Bot=" @ %botName @ " | Race='" @ %botRace @ "' | $RaceToArmorType='" @ %armor @ "'");
			if(%armor == "" || %armor == -1)
			{
				echo("ERROR: SpawnZoneBots - Could not find armor for race '" @ %botRace @ "' for bot " @ %botName @ ", defaulting to MaleHumanArmor7");
				%armor = "MaleHumanArmor7";
			}
			
			%spawnPos = $BotInfo[%botName, SPAWN_POS];
			%spawnRot = $BotInfo[%botName, SPAWN_ROT];
			%baseDisplayName = $BotInfo[%botName, NAME];
			
			// Only prepend zone short name to generic merchants and bankers to make them unique
			%displayName = %baseDisplayName;
			%isMerchant = (String::findSubStr(%botName, "merchant") == 0);
			%isBanker = (String::findSubStr(%botName, "banker") == 0);
			
			if((%isMerchant || %isBanker) && %zoneShortName != "" && %zoneShortName != -1)
			{
				// Only prepend to generic merchants/bankers (e.g. "Melee Merchant", "banker")
				// Custom named merchants (e.g. "GodFather Gian", "Uncle Tony") remain unchanged
				%isGeneric = false;
				if(%baseDisplayName == "Melee Merchant" ||
				   %baseDisplayName == "Armor Merchant" ||
				   %baseDisplayName == "Miscellaneous Merchant" ||
				   %baseDisplayName == "General Merchant" ||
				   %baseDisplayName == "banker" ||
				   %baseDisplayName == "Banker")
				{
					%isGeneric = true;
				}

				if(%isGeneric)
				{
					%displayName = %zoneShortName @ " " @ %baseDisplayName;
				}
			}
			
			// Check if we already have a stored ID for this bot
			%storedId = $TownBotSpawned[%botName];
			if(%storedId != "" && %storedId != -1)
			{
				// We have a stored ID, verify the bot still exists
				%playerObj = Client::getOwnedObject(%storedId);
				if(%playerObj != "" && %playerObj != -1)
				{
					// CRITICAL: Check for ghost client ID (empty name Player object)
					%storedName = Client::getName(%storedId);
					if(%storedName == "" || %storedName == -1)
					{
						// This is a ghost client ID - clean it up
						echo("WARNING: SpawnZoneBots - Found ghost client ID " @ %storedId @ " for bot " @ %botName @ " (empty name). Cleaning up...");
						%aiNameFull = "TownBot_" @ %botName;
						CleanupGhostClientId(%storedId, %aiNameFull);
						$TownBotSpawned[%botName] = "";
						// Continue to spawn a new bot
					}
					else
				{
					// Bot already exists and is valid - skip spawning
					continue;
					}
				}
				else
				{
					// Stored ID is stale (bot was deleted), clear it
					$TownBotSpawned[%botName] = "";
				}
			}
			
			// Check if an AI with this name already exists (could be an orphaned AI or enemy bot with corrupted name)
			// For town bots, they are Player objects, not Drones, so AI::getId() will always fail
			// Only check by display name for town bots
			%existingId = "";
			if(%displayName != "" && %displayName != -1)
			{
				%existingId = NEWgetClientByName(%displayName);
			}
			// Note: We don't use AI::getId() for town bots since they're Player objects, not Drones
			// AI::getId() only works for Drones and will generate "Could not find drone" errors for Player objects
			%useModifiedAiName = false;
			if(%existingId != -1 && %existingId != "")
			{
				// CRITICAL: Check for ghost client ID (empty name Player object)
				%existingName = Client::getName(%existingId);
				if(%existingName == "" || %existingName == -1)
				{
					// This is a ghost client ID - clean it up
					echo("WARNING: SpawnZoneBots - Found ghost client ID " @ %existingId @ " with empty name for bot " @ %botName @ ". Cleaning up...");
					%aiNameFull = "TownBot_" @ %botName;
					CleanupGhostClientId(%existingId, %aiNameFull);
					%existingId = "";  // Clear so we can spawn a new bot
				}
			}
			
			if(%existingId != -1 && %existingId != "")
			{
				// CRITICAL: Check if this client ID is already in use by a REAL PLAYER (not a bot)
				// If a real player is using this client ID, we cannot spawn a bot with it
				// NOTE: We check by display name patterns because isRPGAI() may not work yet (bot data not set)
				%existingPlayerName = Client::getName(%existingId);
				if(%existingPlayerName != "" && %existingPlayerName != -1)
				{
					// Check if the name matches bot patterns (enemy bots have prefixes, town bots match expected names)
					%isBotName = false;
					
					// Check enemy bot patterns
					%isBotName = HasEnemyBotNamePrefix(%existingPlayerName);
					
					// Check if it matches expected town bot display name
					%expectedDisplayName = $BotInfo[%botName, NAME];
					if(!%isBotName && %expectedDisplayName != "" && %expectedDisplayName != -1)
					{
						if(String::ICompare(%existingPlayerName, %expectedDisplayName) == 0 ||
						   String::findSubStr(%existingPlayerName, %expectedDisplayName) >= 0)
						{
							%isBotName = true;  // Matches expected town bot name
						}
					}
					
					// Also check if isRPGAI() returns true (bot data is set)
					if(!%isBotName && isRPGAI(%existingId))
					{
						%isBotName = true;  // Bot data exists, so it's a bot
					}
					
					// If it's not a bot name pattern and not a bot, it's a real player
					if(!%isBotName)
					{
						// This is a REAL PLAYER using this client ID - cannot spawn bot here
						echo("WARNING: SpawnZoneBots - Client ID " @ %existingId @ " is already in use by real player '" @ %existingPlayerName @ "'. Cannot spawn bot " @ %botName @ ". Skipping...");
						continue;  // Skip spawning this bot
					}
				}
				
				// An AI with this name already exists - check if it's actually a town bot
				// CRITICAL: Check $TownBotData and $TownBotSpawned FIRST to avoid stale data from $ClientData
				%existingBotInfoAiNameTown = $TownBotData[%existingId, "BotInfoAiName"];
				%existingSpawnBotInfoTown = $TownBotData[%existingId, "SpawnBotInfo"];
				
				// Check if this client ID is registered as a town bot
				%isRegisteredTownBot = false;
				%registeredTownBotName = "";
				for(%regIndex = 0; %regIndex < $TownBotRegistryCount; %regIndex++)
				{
					%regBotName = $TownBotRegistry[%regIndex];
					if(%regBotName != "" && %regBotName != "0" && %regBotName != -1)
					{
						if($TownBotSpawned[%regBotName] == %existingId)
						{
							%isRegisteredTownBot = true;
							%registeredTownBotName = %regBotName;
							break;
						}
					}
				}
				
				// If it's a registered town bot with the correct name, skip spawning
				if(%isRegisteredTownBot && %registeredTownBotName == %botName)
				{
					// Bot already exists and is valid - update stored ID and skip spawning
					$TownBotSpawned[%botName] = %existingId;
					// Ensure O(1) lookup table is also updated
					if(!IsTownBotClientId(%existingId))
						RegisterTownBotClient(%existingId, %botName, %zoneIndex);
					continue;
				}
				
				// If it's a registered town bot with a different name, this is a conflict (same display name, different bot)
				// This shouldn't happen, but handle it gracefully
				if(%isRegisteredTownBot && %registeredTownBotName != %botName)
				{
					echo("WARNING: SpawnZoneBots - Town bot " @ %botName @ " has same display name as existing town bot " @ %registeredTownBotName @ " (clientId=" @ %existingId @ "). This is a naming conflict.");
					// Continue to spawn - AI::spawn() will handle the conflict
				}
				
				// Check if it's an enemy bot by checking $EnemyBotData directly (avoid stale $ClientData)
				%existingSpawnBotInfoEnemy = $EnemyBotData[%existingId, "SpawnBotInfo"];
				%existingBotInfoAiNameEnemy = $EnemyBotData[%existingId, "BotInfoAiName"];
				
				// Also check $ClientData as fallback (but only if not found in new arrays)
				if(%existingSpawnBotInfoEnemy == "" || %existingSpawnBotInfoEnemy == "0" || %existingSpawnBotInfoEnemy == -1)
				{
					%existingSpawnBotInfo = $ClientData[%existingId, "SpawnBotInfo"];
					%existingBotInfoAiName = $ClientData[%existingId, "BotInfoAiName"];
				}
				else
				{
					%existingSpawnBotInfo = %existingSpawnBotInfoEnemy;
					%existingBotInfoAiName = %existingBotInfoAiNameEnemy;
				}
				
				// If it's an enemy bot (has SpawnBotInfo but wrong BotInfoAiName)
				// Since town bots and enemy bots spawn in different zones, this should only happen if the enemy bot is dead/stale
				if(%existingSpawnBotInfo != "" && %existingSpawnBotInfo != "0" && %existingSpawnBotInfo != -1 && %existingBotInfoAiName != %botName)
				{
					// Verify the enemy bot is actually still alive
					%enemyBotPlayerObj = Client::getOwnedObject(%existingId);
					%enemyBotIsAlive = (%enemyBotPlayerObj != "" && %enemyBotPlayerObj != -1);
					
					// If enemy bot is still alive, this is unexpected - retry spawning to get a different client ID
					// (Town bots and enemy bots should never be in the same zones)
					if(%enemyBotIsAlive)
					{
						%retryCount = $TownBotSpawnRetry[%botName];
						if(%retryCount == "")
							%retryCount = 0;
						
						if(%retryCount < 3)
						{
							echo("WARNING: SpawnZoneBots - Cannot spawn town bot " @ %botName @ ". Active enemy bot (clientId=" @ %existingId @ ") with same display name found. Retrying spawn (attempt " @ (%retryCount + 1) @ "/3).");
							$TownBotSpawnRetry[%botName] = %retryCount + 1;
							// Retry spawning ONLY this specific bot after a delay (not the entire zone)
							schedule("SpawnSingleZoneBot(\"" @ %botName @ "\", " @ %zoneIndex @ ");", 0.5);
							continue;
						}
						else
						{
							// Max retries reached - give up
							echo("ERROR: SpawnZoneBots - Failed to spawn town bot " @ %botName @ " after 3 retries. Active enemy bot (clientId=" @ %existingId @ ") still has same display name.");
							$TownBotSpawnRetry[%botName] = "";
							continue;
						}
					}
					
					// Enemy bot is dead/stale - allow spawn, stale data will be cleared after spawn
					echo("INFO: SpawnZoneBots - Enemy bot (clientId=" @ %existingId @ ") with same display name is dead/stale. Allowing town bot spawn - stale data will be cleared.");
					// Continue to spawn - stale data will be cleared after spawn
				}
				
				// Otherwise, it's an orphaned AI or unknown state - AI::spawn() will clean it up
			}
			
			// RACE FIX (town bot respawn vs deferred despawn): if an AI with this display name still
			// exists here, it is almost always a just-despawned town bot whose player deleteObject was
			// deferred 1s by DespawnZoneBots (its $TownBotSpawned/$EnemyBotData were wiped, so it was not
			// reclaimed above). AI::spawn() does NOT replace a same-named AI -- it errors "An AI named ...
			// already exists!" and the respawn fails. Tear that stale AI down NOW (engine truth via
			// Player::isAiControlled, so a real player is never touched) and cancel its pending deferred
			// despawn-delete, so the fresh AI::spawn() below succeeds with a clean slot.
			if(%existingId != -1 && %existingId != "" && Player::isAiControlled(%existingId))
			{
				$DespawnValidationToken[%existingId] = "";   // cancel DespawnZoneBots' scheduled deleteObject (Ai.cs-local)
				%staleObj = Client::getOwnedObject(%existingId);
				if(%staleObj != "" && %staleObj != -1 && isObject(%staleObj))
					deleteObject(%staleObj);
				echo("[ZONE SPAWN] Cleared stale TownBot_" @ %botName @ " (clientId=" @ %existingId @ ") before respawn");
			}

			// No stored ID means this is a fresh spawn - just proceed to spawn
			
			if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: CALLING AI::spawn() for " @ %botName @ " with armor='" @ %armor @ "'");
			
			// CRITICAL INTEGRATION: Check server capacity before spawning
			%predictedId = PlayerManager::getFreeId();
			if(%predictedId == -1)
			{
				echo("CRITICAL: SpawnZoneBots - Server is FULL! Aborting spawn for " @ %botName);
				// Clean up stored pending state?
				// Just continue, maybe next slot frees up?
				continue;
			}
			
			if(AI::spawn(%aiName, %armor, %spawnPos, %spawnRot, %displayName, "male2") != "false")
			{
				if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: AI::spawn() SUCCEEDED for " @ %botName);
				// DEBUG: Commented out to reduce server lag
	//echo("[TOWN BOT DEBUG] AI::spawn() returned SUCCESS for " @ %botName);
				// CRITICAL: Try to get client ID immediately to set team before UpdateTeam() runs
				// UpdateTeam() may be called by Game::playerSpawned() before SpawnZoneBotPostSpawn() runs
				// If we can get the client ID now, set team immediately to prevent UpdateTeam() from setting it to team 1
				
				// CRITICAL INTEGRATION: Check predicted ID first
				%immediateClientId = "";
				if(%predictedId != "" && %predictedId != -1 && Client::getName(%predictedId) == %displayName)
				{
					%immediateClientId = %predictedId;
					if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Prediction SUCCESS for " @ %botName @ " (ID: " @ %immediateClientId @ ")");
				}
				else
				{
					%immediateClientId = NEWgetClientByName(%displayName);
				}
				if(%immediateClientId != -1 && %immediateClientId != "")
			{
					// DEBUG: Check what armor the bot actually got
					%actualArmor = Player::getArmor(%immediateClientId);
					%actualTeam = GameBase::getTeam(%immediateClientId);
					%playerObj = Client::getOwnedObject(%immediateClientId);
					%dataName = "";
					if(%playerObj != -1 && %playerObj != "")
					{
						%dataName = GameBase::getDataName(%playerObj);
						// CRITICAL FIX: Mark object as town bot explicitly to prevent UpdateAppearance from messing with it
						%playerObj.isTownBot = true;
					}
					
					if($TOWNBOT_RACE_DEBUG)
					{
						echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Bot=" @ %botName @ " spawned | clientId=" @ %immediateClientId @ " | playerObj=" @ %playerObj);
						echo("[TOWNBOT RACE DEBUG]   Expected armor='" @ %armor @ "' | Player::getArmor='" @ %actualArmor @ "' | GameBase::getDataName='" @ %dataName @ "' | Team=" @ %actualTeam);
					}
					
					// CRITICAL DEBUG: Check for orphaned objects with this client ID in MissionCleanup
					if(isObject("MissionCleanup"))
					{
						%group = nameToID("MissionCleanup");
						%count = Group::objectCount(%group);
						%orphanCount = 0;
						for(%j = %count - 1; %j >= 0; %j--)
						{
							%obj = Group::getObject(%group, %j);
							if(!isObject(%obj)) continue;
							if(getObjectType(%obj) != "Player") continue;
							
							%objClientId = Player::getClient(%obj);
							if(%objClientId == %immediateClientId && %obj != %playerObj)
							{
								%orphanData = GameBase::getDataName(%obj);
								echo("[TOWNBOT RACE DEBUG]   WARNING: Found orphaned object " @ %obj @ " (data=" @ %orphanData @ ") also claiming clientId=" @ %immediateClientId @ "!");
								%orphanCount++;
								// Delete the orphan immediately
								echo("[TOWNBOT RACE DEBUG]   Deleting orphan " @ %obj);
								deleteObject(%obj);
							}
						}
						if(%orphanCount > 0)
							if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG]   Cleaned " @ %orphanCount @ " orphaned objects for clientId=" @ %immediateClientId);
					}
					
					// Got client ID immediately - set BotInfoAiName and team right away
				%aiNameFull = "TownBot_" @ %botName;
					storeData(%immediateClientId, "BotInfoAiName", %aiNameFull);
					$TownBotData[%immediateClientId, "BotInfoAiName"] = %aiNameFull;
					// Set team to 0 immediately to prevent UpdateTeam() from setting it to team 1
					GameBase::setTeam(%immediateClientId, 0);
					
					// CRITICAL FIX: Clear ExpDistributed flag for new town bots
					// This prevents the bot from inheriting "EXP already distributed" status from a previous bot/client
					storeData(%immediateClientId, "ExpDistributed", "");
					
					// CRITICAL FIX: Always refresh armor visual after setting team
					// When team was -1 (engine default), the visual skin is wrong even if armor model is correct
					// We must re-apply armor after team change to fix the visual appearance
					if(%armor != "" && %armor != -1)
					{
						if($TOWNBOT_RACE_DEBUG) echo("[TOWNBOT RACE DEBUG] SpawnZoneBots: Refreshing armor visual for " @ %botName @ " after team set (armor='" @ %armor @ "')");
						Player::setArmor(%playerObj, %armor);
						Safe_SetSkin(%immediateClientId, $Server::teamSkin[0], "AI_Spawn_Timer_Helper immediate");
						
						// FINAL WORD: Schedule one more skin enforcement at T+2.0s
						// This runs after ALL other scheduled scripts (especially any ~1.0s culprits)
						schedule("ForceTownBotSkin(" @ %immediateClientId @ ", \"" @ %botName @ "\");", 2.0);
					}
				}
				
				// CRITICAL: Add 1 second delay before looking up client ID to allow Player object to register
				// This prevents "Could not get client ID" warnings on fresh server restarts
				// Schedule the client ID lookup and initialization with a delay
				schedule("SpawnZoneBotPostSpawn(\"" @ %aiName @ "\", \"" @ %botName @ "\", \"" @ %displayName @ "\", " @ %zoneIndex @ ");", 1.0);
				continue; // Continue to next bot - this one will be initialized in scheduled call
			}
		}
	}
}

// Despawn all bots for a specific zone
function DespawnZoneBots(%zoneIndex)
{
	// CRITICAL: Reject invalid zone indices (0, empty, or -1)
	// Zone -1 is "Unknown" zone and should never trigger despawn
	if(%zoneIndex == 0 || %zoneIndex == "" || %zoneIndex == -1)
		return;
	
	// NOTE: Zone 25 (Colloseum) seal bots are now properly despawned via TempSpawn handling
	// No exemption needed - seal bots have SpawnBotInfo="TempSpawn ..." and will be killed when zone empties
	
	// CRITICAL: Verify zone is actually empty before despawning (double-check against $ZonePlayerCount)
	// This prevents despawning bots when players are still in the zone (if $ZonePlayerCount got out of sync)
	%zoneFolderID = $Zone::FolderID[%zoneIndex];
	if(%zoneFolderID == "" || %zoneFolderID == -1)
		return; // Invalid zone index

	// Reuse standard AI debug flags so despawn tracing can be toggled globally.
	%despawnDebug = ($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG);
	
	// DEBUG: Log all connected players and their zone data for diagnosis
	%zoneDesc = $Zone::Desc[%zoneIndex];
	if(%despawnDebug) echo("[DESPAWN DEBUG] === DespawnZoneBots(" @ %zoneIndex @ ") - " @ %zoneDesc @ " ===");
	if(%despawnDebug) echo("[DESPAWN DEBUG] Zone FolderID: " @ %zoneFolderID @ ", $ZonePlayerCount: " @ $ZonePlayerCount[%zoneIndex]);
	
	// DEBUG: Iterate all connected players and show their zone data  
	if(%despawnDebug)
	{
		echo("[DESPAWN DEBUG] Connected players zone check:");
		%debugPlayerCount = 0;
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
		{
			%debugPlayerCount++;
			%playerName = Client::getName(%cl);
			%playerZone = fetchData(%cl, "zone");
			%playerZoneIndex = Zone::getIndex(%playerZone);
			%isInTargetZone = (%playerZone == %zoneFolderID);
			echo("[DESPAWN DEBUG]   Player: " @ %playerName @ " (clientId=" @ %cl @ ") - zone='" @ %playerZone @ "' (index=" @ %playerZoneIndex @ ") matchesTarget=" @ %isInTargetZone);
		}
		echo("[DESPAWN DEBUG] Total connected players: " @ %debugPlayerCount);
	}
	
	%playerList = Zone::getPlayerList(%zoneFolderID, 2); // Type 2 = real players only (not bots)
	%hasPlayers = (%playerList != "" && %playerList != -1);
	
	if(%despawnDebug) echo("[DESPAWN DEBUG] Zone::getPlayerList returned: '" @ %playerList @ "' hasPlayers=" @ %hasPlayers);
	
	if(%hasPlayers)
	{
		// Zone has players - don't despawn, but fix the count
		%actualPlayerCount = GetWordCount(%playerList);
		$ZonePlayerCount[%zoneIndex] = %actualPlayerCount;
		echo("WARNING: DespawnZoneBots - Zone " @ %zoneIndex @ " has " @ %actualPlayerCount @ " player(s) but was scheduled for despawn. Fixed player count.");
		return;
	}
	
	// DEBUG: If no players found, this despawn will proceed - log prominently
	if(%despawnDebug) echo("[DESPAWN DEBUG] *** PROCEEDING WITH DESPAWN - Zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") detected as EMPTY ***");
	
	// Clear the despawn schedule flag
	$ZoneBotDespawnSchedule[%zoneIndex] = "";
	
	// Get zone description to match bots in this zone
	%zoneDesc = $Zone::Desc[%zoneIndex];
	if(%zoneDesc == "")
		return;
	
	// First, collect all town bot clientIds to remove from TownBotList (optimization: rebuild list once at end)
	%clientIdsToRemove = "";
	%botCount = 0;
	%despawnRunKey = %zoneIndex @ "_" @ getSimTime();
	
	// First, despawn all town bots in this zone
	for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
	{
		// Skip invalid bot names (like "0" which can appear in lists)
		if(%botName == "" || %botName == "0" || %botName == -1)
			continue;
		
		if($TownBotZone[%botName] == %zoneIndex && $TownBotSpawned[%botName] != "")
		{
			%clientId = $TownBotSpawned[%botName];
			%aiName = "TownBot_" @ %botName;
			
			// UNIFIED SAFEGUARD: Verify this is a bot (handles Real Players vs Ghost Bots)
			if(!IsSafeToModify(%clientId, "DespawnZoneBots (Town)"))
				continue;
			
			// ADDITIONAL SAFEGUARD: Only despawn bots that have fully loaded and spawned
			// This prevents despawning bots that are still initializing or in an invalid state
			if(!fetchData(%clientId, "HasLoadedAndSpawned"))
			{
				echo("WARNING: DespawnZoneBots - Bot " @ %botName @ " (clientId " @ %clientId @ ") has not loaded and spawned yet. Skipping despawn to prevent data corruption.");
				continue;
			}
			
			// Get the player object (same as clientId for AI bots)
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
			{
				// CRITICAL: Clear all bot identification data BEFORE deletion to prevent conflicts
				// This prevents old bots from being detected when searching for new bots with same display name
				storeData(%clientId, "BotInfoAiName", "");
				$TownBotData[%clientId, "BotInfoAiName"] = "";
				$ClientData[%clientId, "BotInfoAiName"] = "";
				$BotInfoAiName[%clientId] = "";  // Clear direct array for fast lookup
				
				// CRITICAL: Clear all other town bot data from $TownBotData array to prevent stale data
				// This ensures the next bot using this clientId doesn't see the old bot's data
				$TownBotData[%clientId, "SpawnBotInfo"] = "";
				$TownBotData[%clientId, "SpawnTime"] = "";
				$TownBotData[%clientId, "QuestItems"] = "";
				$TownBotData[%clientId, "KeyItems"] = "";
				$TownBotData[%clientId, "Consumables"] = "";
				$TownBotData[%clientId, "Armor"] = "";
				$TownBotData[%clientId, "Accessories"] = "";
				$TownBotData[%clientId, "Other"] = "";
				
				// CRITICAL: Also clear $EnemyBotData in case this clientId was previously used by an enemy bot
				// This prevents isRPGAI() from returning true due to stale enemy bot data
				$EnemyBotData[%clientId, "BotInfoAiName"] = "";
				$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
				
				// CRITICAL: Clear conversation state for all players who were talking to this bot
				// This ensures conversations restart from the beginning when the bot respawns
				for(%playerCl = Client::getFirst(); %playerCl != -1; %playerCl = Client::getNext(%playerCl))
				{
					$state[%clientId, %playerCl] = "";
					// Also clear LastPlayerInteraction tracking
					$LastPlayerInteraction[%clientId, %playerCl] = "";
				}
				
				// Clean up bot data (same as #deletebot command)
				storeData(%clientId, "noDropLootbagFlag", True);
				ClearEvents(%clientId);
				
				// CRITICAL: Clear zone data before deleting to prevent zone detection issues
				storeData(%clientId, "zone", "");
				storeData(%clientId, "tmpzone", "");
				
				// CRITICAL: Clear all storeData fields set during InitTownBotPostSpawn
				storeData(%clientId, "RACE", "");
				storeData(%clientId, "NoDropLoot", "");
				storeData(%clientId, "MountWeaponOnSpawn", "");
				storeData(%clientId, "MountWeaponOnTalk", "");
				storeData(%clientId, "ShowIdleMessage", "");
				storeData(%clientId, "LastInteractionTime", "");
				storeData(%clientId, "dumbAIflag", "");
				storeData(%clientId, "botTeam", "");
				storeData(%clientId, "AITarget", "");
				storeData(%clientId, "AILastDestination", "");
				storeData(%clientId, "AILastLoggedDist", "");
				storeData(%clientId, "AIMovementLoopRunning", "");
				
				// CRITICAL: Clear all directive table entries for this bot (by client ID)
				// Clear common directives (0-99) to prevent stale directive data
				for(%d = 0; %d <= 99; %d++)
				{
					$aidirectiveTable[%clientId, %d] = "";
				}
				
				// CRITICAL: Clear belt cached lists to prevent memory leaks
				$Belt::CachedList[%clientId, "QuestItems"] = "";
				$Belt::CachedList[%clientId, "KeyItems"] = "";
				$Belt::CachedList[%clientId, "Consumables"] = "";
				$Belt::CachedList[%clientId, "Armor"] = "";
				$Belt::CachedList[%clientId, "Accessories"] = "";
				$Belt::CachedList[%clientId, "Other"] = "";
				
				// Clean up bot group if applicable
				%b = AI::IsInWhichBotGroup(%clientId);
				if(%b != -1)
					AI::RemoveBotFromBotGroup(%clientId, %b);
				
				// Clear transient flags so the next occupant of this clientId starts clean
				storeData(%clientId, "HasLoadedAndSpawned", "");
				storeData(%clientId, "SpawnInvuln", "");
				%dispNameClear = Client::getName(%clientId);
				if(%dispNameClear != "" && %dispNameClear != -1)
					$SpawnInvulnByName[%dispNameClear] = "";
				storeData(%clientId, "ShovedByPlayer", "");
				$BotFrozen[%clientId] = "";
				storeData(%clientId, "AITarget", "");
				storeData(%clientId, "BotAttackLoopActive", "");
				
				// CRITICAL: Free AI number from $aiNumTable so it can be recycled
				// This ensures bot numbers restart from 0-20 instead of going to 100+
				// Get the number BEFORE clearing $tmpbotn to ensure we can free it
				%townBotNumber = $tmpbotn[%aiName];
				if(%townBotNumber != "" && %townBotNumber != -1 && %townBotNumber != "0")
				{
					$aiNumTable[%townBotNumber] = "";
					$tmpbotn[%aiName] = "";
					//echo("[SPAWN DEBUG] DespawnZoneBots(): Freed AI number " @ %townBotNumber @ " for town bot " @ %aiName @ " - number can now be recycled");
				}
				
			// CRITICAL: Mark this client ID as recently freed to prevent immediate reuse
			// This prevents new bots from getting the same client ID before cleanup completes
			$ClientIdRecentlyFreed[%clientId] = getSimTime();
			
			// CRITICAL: Clear retry counters (if any were set during spawn failures)
			$TownBotRetryGetAIIdCount[%botName] = "";
			$TownBotSpawnRetry[%botName] = "";
			
			// Collect clientId for removal (optimization: rebuild list once at end)
				%clientIdsToRemove = %clientIdsToRemove @ %clientId @ " ";
				$DespawnRemoveMark[%despawnRunKey, %clientId] = 1;
				%botCount++;
				
				// CRITICAL: Register bot in graveyard BEFORE deletion to prevent client ID reuse collision
				// Variables %aiName and %clientId are already defined in this scope
				AddToGraveyard(%aiName, %clientId);
				
				// Delete the player object directly (same approach as #deletebot)
				// CRITICAL: Add 1 second delay to deletion to help code load properly and functions/variables be called more effectively
				// CRITICAL: Use validation token to prevent scheduled deletion from affecting real players who get the same clientId
				%validationToken = %botName @ "_" @ getSimTime();
				$DespawnValidationToken[%clientId] = %validationToken;
				// Store player name for validation check in scheduled deletion
				%despawnPlayerName = Client::getName(%clientId);
				if(%despawnPlayerName == "" || %despawnPlayerName == -1)
					%despawnPlayerName = "Unknown";
				schedule("if($DespawnValidationToken[" @ %clientId @ "] == \"" @ %validationToken @ "\" && !isFile(\"temp\\\\" @ %despawnPlayerName @ ".cs\")) { if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ "); $DespawnValidationToken[" @ %clientId @ "] = \"\"; }", 1.0);
				if(%despawnDebug) echo("Despawned bot: " @ %botName @ " (zone " @ %zoneIndex @ ")");
			}
			else
			{
				// Bot already deleted, but clear identification data anyway to prevent conflicts
				storeData(%clientId, "BotInfoAiName", "");
				$TownBotData[%clientId, "BotInfoAiName"] = "";
				$ClientData[%clientId, "BotInfoAiName"] = "";
				$BotInfoAiName[%clientId] = "";  // Clear direct array for fast lookup
				
				// CRITICAL: Free AI number from $aiNumTable even if bot is already deleted
				// This ensures bot numbers are recycled properly
				%townBotNumber = $tmpbotn[%aiName];
				if(%townBotNumber != "" && %townBotNumber != -1 && %townBotNumber != "0")
				{
					$aiNumTable[%townBotNumber] = "";
					$tmpbotn[%aiName] = "";
					//echo("[SPAWN DEBUG] DespawnZoneBots(): Freed AI number " @ %townBotNumber @ " for already-deleted town bot " @ %aiName @ " - number can now be recycled");
				}
				
				// Bot already deleted, still collect for list removal
				%clientIdsToRemove = %clientIdsToRemove @ %clientId @ " ";
				$DespawnRemoveMark[%despawnRunKey, %clientId] = 1;
				if(%despawnDebug) echo("Bot " @ %botName @ " already deleted, will remove from list");
			}
			
			// Clear spawn tracking
			$TownBotSpawned[%botName] = "";
			
			// NEW: Clear O(1) lookup table and set cooldown
			UnregisterTownBotClient(%clientId);
			// Clear lingering no-drop flag so next occupant of this clientId can drop normally
			storeData(%clientId, "noDropLootbagFlag", "");
			$TownBotData[%clientId, "noDropLootbagFlag"] = "";
			$ClientData[%clientId, "noDropLootbagFlag"] = "";
			// Also clear invuln/name guards for reuse safety
			storeData(%clientId, "HasLoadedAndSpawned", "");
			storeData(%clientId, "SpawnInvuln", "");
			%dispNameClear2 = Client::getName(%clientId);
			if(%dispNameClear2 != "" && %dispNameClear2 != -1)
				$SpawnInvulnByName[%dispNameClear2] = "";
		}
	}
	
	// OPTIMIZATION: Rebuild TownBotList once for all removed bots instead of once per bot
	if(%botCount > 0)
	{
		%newList = "";
		for(%j = 0; (%id = GetWord($TownBotList, %j)) != -1; %j++)
		{
			// O(1) membership check for this despawn pass (avoids nested scans on large lists)
			if($DespawnRemoveMark[%despawnRunKey, %id] == "")
				%newList = %newList @ %id @ " ";
		}
		$TownBotList = %newList;
	}
	
	// CRITICAL: Also despawn enemy bots spawned from spawn points in this zone
	// This handles the case where multiple bots were incorrectly spawned per spawn point
	// (due to race conditions). We need to kill them and properly decrement spawn counters.
	// IMPORTANT: We must be very careful to only kill bots that are actually in this zone
	// to prevent killing enemy bots in other zones.
	// CRITICAL FIX: Client::getFirst()/getNext() only returns REAL player clients, NOT AI bots!
	// Use $BotRegistryList to iterate enemy bots (bots are not in client connection list)
	// 
	// CRITICAL FIX: Copy the list BEFORE iterating!
	// Player::Kill() -> UnregisterBot() modifies $BotRegistryList, removing the killed bot.
	// This shifts all subsequent entries down, causing the next bot to be skipped.
	// By copying the list first, we iterate a stable snapshot while the original is modified.
	%botListSnapshot = $BotRegistryList;
	for(%i = 0; (%botId = GetWord(%botListSnapshot, %i)) != -1; %i++)
	{
		// Validate bot ID is valid
		if(%botId == "" || %botId == "0")
			continue;
		
		// CRITICAL: Skip bots that were already processed by the first loop (town bots)
		// The first loop clears their data but schedules deletion for 1 second later
		// So GetBotIdList() might still return them before they're actually deleted
		if($DespawnRemoveMark[%despawnRunKey, %botId] != "")
			continue;
		
		// Validate bot still exists
		%playerObj = Client::getOwnedObject(%botId);
		if(%playerObj == "" || %playerObj == -1)
			continue;
		
		// DEBUG: Trace enemy bot candidates
		if(%despawnDebug) echo("[DESPAWN DEBUG] Checking bot candidate: " @ %botId @ " (" @ Client::getName(%botId) @ ")");
		
		// UNIFIED SAFEGUARD: Verify this is a bot (handles Real Players vs Ghost Bots)
		if(!IsSafeToModify(%botId, "DespawnZoneBots (Enemy)"))
			continue;
		
		// CRITICAL: Determine bot type BEFORE checking HasLoadedAndSpawned so we can show accurate warning messages
		// Town bots have BotInfoAiName but NO SpawnBotInfo
		// Enemy bots have BotInfoAiName AND SpawnBotInfo
		%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
		%botInfoAiName = fetchData(%botId, "BotInfoAiName");
		%isTownBot = (%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0" && (%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0"));
		
		// Skip town bots (they're already handled above)
		if(%isTownBot)
		{
			// This is a town bot (has BotInfoAiName but no SpawnBotInfo) - skip, already handled above
			continue;
		}
		
		// ADDITIONAL SAFEGUARD: Only despawn bots that have fully loaded and spawned
		// This prevents despawning bots that are still initializing or in an invalid state
		if(!fetchData(%botId, "HasLoadedAndSpawned"))
		{
			// Now we know it's an enemy bot (not a town bot), so the message is accurate
			echo("WARNING: DespawnZoneBots - Enemy bot clientId " @ %botId @ " has not loaded and spawned yet. Skipping to prevent data corruption.");
			continue;
		}
		
		// CRITICAL: Only process enemy bots (must have SpawnBotInfo)
		if(%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0")
		{
			// Not an enemy bot - skip
			continue;
		}
		
		// CRITICAL: Verify bot is actually in the target zone using zone index, not just description
		// This prevents killing bots in other zones if zone descriptions match incorrectly
		%botZone = fetchData(%botId, "zone");
		
		// Fallback: If "zone" is 0 or empty (often overwritten by Zone::Update logic), check "tmpzone"
		if(%botZone == "" || %botZone == 0 || %botZone == -1)
		{
			%botZone = fetchData(%botId, "tmpzone");
			// If still empty, try direct array access for tmpzone as last resort
			if(%botZone == "" || %botZone == 0 || %botZone == -1)
			{
				%botZone = $EnemyBotData[%botId, "tmpzone"];
			}
		}



		
		if(%botZone == "" || %botZone == -1)
			continue; // Bot has no zone data, skip to be safe
		
		// Get target zone folder ID from the passed zone index (no loop needed - O(1) lookup)
		%targetZoneFolder = $Zone::FolderID[%zoneIndex];
		if(%targetZoneFolder == "" || %targetZoneFolder == -1)
		{
			if(%despawnDebug) echo("[DESPAWN DEBUG] Invalid targetZoneFolder for index " @ %zoneIndex);
			continue; // Invalid zone index, skip
		}
		
		if(%despawnDebug) echo("[DESPAWN DEBUG] Processing bot " @ %botId @ " for target zone INDEX " @ %zoneIndex @ " (folderID " @ %targetZoneFolder @ ")");
		
		// Compare bot's zone INDEX directly with target zone INDEX
		// CRITICAL FIX: %botZone is a zone INDEX (13, 17, etc.) and must be compared to %zoneIndex (also an index)
		// NOT to %targetZoneFolder (which is an object ID like 8568)
		%match = false;
		%originZone = "";
		%matchedViaOrigin = false;
		if(%botZone == %zoneIndex)
		{
			%match = true;
			if(%despawnDebug) echo("[DESPAWN DEBUG] MATCH! Bot " @ %botId @ " zone=" @ %botZone @ " matches target zoneIndex=" @ %zoneIndex);
		}
		else
		{
			// Mismatch on primary zone - check SpawnOriginZoneID (origin folder ID) against targetZoneFolder (folder ID)
			// This handles cases where bot was spawned from a specific folder
			%originZone = fetchData(%botId, "SpawnOriginZoneID");
			if(%originZone == "" || %originZone == 0 || %originZone == -1)
				%originZone = $EnemyBotData[%botId, "SpawnOriginZoneID"];
				
			if(%originZone == %targetZoneFolder)
			{
				// Match found via SpawnOriginZoneID (comparing folder IDs)!
				if(%despawnDebug) echo("[DESPAWN DEBUG] MATCH! Bot " @ %botId @ " matched via SpawnOriginZoneID=" @ %originZone @ " == targetZoneFolder=" @ %targetZoneFolder);
				%match = true;
				%matchedViaOrigin = true;
			}
		}

		if(!%match)
		{
			if(%despawnDebug) echo("[DESPAWN DEBUG] Zone Mismatch for " @ %botId @ ": botZone(index)=" @ %botZone @ " != targetZoneIndex=" @ %zoneIndex @ ", originZone(folderID)=" @ %originZone @ " != targetZoneFolder=" @ %targetZoneFolder @ ". Skipping.");
			continue; // Bot is in a different zone, skip to prevent killing bots in other zones
		}
		
		// NOTE: Zone description check removed - it was blocking legitimate despawns
		// Zone::getDesc(%botZone) returns -1 for zone indexes, causing false mismatches
		// The zone INDEX match above is sufficient for safe despawning
		
		// CRITICAL SAFEGUARD: Must have SpawnBotInfo to be considered an enemy bot
		%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
		if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
		{
			// No SpawnBotInfo - this is not an enemy bot, skip to be safe
			echo("WARNING: DespawnZoneBots - Skipping client " @ %botId @ " (" @ Client::getName(%botId) @ ") - no SpawnBotInfo (not an enemy bot)");
			continue;
		}
		
		// Check if this bot came from a spawn point OR a temp spawn (seal battle bots use TempSpawn)
		%spawnType = GetWord(%spawnBotInfo, 0);
		if(%spawnType == "SpawnPoint" || %spawnType == "TempSpawn")
		{
			%spawnPointId = GetWord(%spawnBotInfo, 1);
			
			// CRITICAL: Clear enemy bot data arrays BEFORE killing
			// Player::onKilled() clears storeData but not these arrays
			// AI::onDroneKilled() may not be called for Player objects
			%botInfoAiName = fetchData(%botId, "BotInfoAiName");
			if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
			{
				// Fallback: try arrays if storeData is already cleared
				%botInfoAiName = $EnemyBotData[%botId, "BotInfoAiName"];
				if(%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0")
					%botInfoAiName = $ClientData[%botId, "BotInfoAiName"];
			}
			
			// Clear spawn scheduled flag to prevent respawn blocking
			if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			{
				$SpawnAIScheduled[%botInfoAiName] = "";
			}
			
			// Clear enemy bot data arrays (Player::onKilled() doesn't clear these)
			// CRITICAL: Do NOT clear BotInfoAiName or SpawnBotInfo here - AI::onDroneKilled() needs them
			// AI::onDroneKilled() is called by AI::delete() which is scheduled in Player::onKilled()
			// BotInfoAiName and SpawnBotInfo will be cleared by AI::onDroneKilled() or ClearVariables() after cleanup completes
			// $EnemyBotData[%botId, "SpawnBotInfo"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled() to identify enemy bots
			$EnemyBotData[%botId, "SpawnTime"] = "";
			// $EnemyBotData[%botId, "BotInfoAiName"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled()
			$EnemyBotData[%botId, "zone"] = "";
			$EnemyBotData[%botId, "tmpzone"] = "";
			$EnemyBotData[%botId, "SpawnOriginZoneID"] = "";
			$EnemyBotData[%botId, "RemortStep"] = "";
			// $ClientData[%botId, "SpawnBotInfo"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled() to identify enemy bots
			$ClientData[%botId, "SpawnTime"] = "";
			// $ClientData[%botId, "BotInfoAiName"] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled()
			// $BotInfoAiName[%botId] = ""; // DO NOT CLEAR - needed by AI::onDroneKilled()
			
			// CRITICAL: Also clear via storeData() to ensure all storage locations are cleaned
			// But keep BotInfoAiName and SpawnBotInfo in storeData() too - AI::onDroneKilled() uses fetchData() to find them
			storeData(%botId, "SpawnOriginZoneID", "");
			
			// Mark client ID as recently freed (Player::onKilled() will also do this, but doing it here ensures it's set)
			$ClientIdRecentlyFreed[%botId] = getSimTime();
			
			// Kill the bot - this will trigger Player::onKilled() which handles:
			// - Spawn counter decrement
			// - Bot tracking counter decrement
			// - AI number freeing
			// - storeData cleanup
			// - Directive table cleanup
			// - Belt cached list cleanup
			// - Pet/bot group cleanup
			storeData(%botId, "noDropLootbagFlag", True);
			// CRITICAL: Set noExperienceFlag to prevent EXP distribution when despawning
			// This prevents players from getting EXP for enemies they attacked but didn't kill
			// when those enemies despawn due to the zone becoming empty
			storeData(%botId, "noExperienceFlag", True);
			Player::Kill(%botId);
			if(%despawnDebug) echo("Despawned enemy bot from " @ %spawnType @ " " @ %spawnPointId @ " (zone " @ %zoneIndex @ ", zoneDesc=" @ %zoneDesc @ ")");
		}
	}

	// Clear temporary removal marks created for this despawn pass.
	for(%clearIdx = 0; (%clearId = GetWord(%clientIdsToRemove, %clearIdx)) != -1; %clearIdx++)
	{
		$DespawnRemoveMark[%despawnRunKey, %clearId] = "";
	}
}


// Periodic check to despawn town bots in zones that are actually empty
// This catches cases where $ZonePlayerCount got out of sync
function PeriodicEmptyZoneCheck()
{
	%checkInterval = $ZoneEmptyCheckInterval;
	if(%checkInterval == "" || %checkInterval <= 0)
		%checkInterval = 30;
	
	%auditInterval = $ZoneEmptyAuditInterval;
	if(%auditInterval == "" || %auditInterval <= 0)
		%auditInterval = 180;
	
	%despawnRetryInterval = $ZoneEmptyDespawnRetryInterval;
	if(%despawnRetryInterval == "" || %despawnRetryInterval <= 0)
		%despawnRetryInterval = 120;
	
	%now = getSimTime();
	
	// Only run full player-list scans for zones that are active/pending, plus periodic safety audits.
	for(%zoneIndex = 1; %zoneIndex <= $Zone::Count; %zoneIndex++)
	{
		%zoneFolderID = $Zone::FolderID[%zoneIndex];
		if(%zoneFolderID == "" || %zoneFolderID == -1)
			continue; // Invalid zone index, skip
		
		%storedCount = $ZonePlayerCount[%zoneIndex];
		if(%storedCount == "")
			%storedCount = 0;
		
		%despawnPending = ($ZoneBotDespawnSchedule[%zoneIndex] == "pending");
		%lastAudit = $ZoneEmptyLastAudit[%zoneIndex];
		%shouldAudit = false;
		
		// Active zones and pending despawns are always audited.
		if(%storedCount > 0 || %despawnPending)
		{
			%shouldAudit = true;
		}
		// Stable empty zones are only audited periodically as a safety net.
		else if(%lastAudit == "" || %lastAudit == -1 || (%now - %lastAudit) >= %auditInterval)
		{
			%shouldAudit = true;
		}
		
		if(!%shouldAudit)
			continue;
		
		$ZoneEmptyLastAudit[%zoneIndex] = %now;
		
		%playerList = Zone::getPlayerList(%zoneFolderID, 2); // Type 2 = real players only (not bots)
		%hasPlayers = (%playerList != "" && %playerList != -1);
		
		if(!%hasPlayers)
		{
			// Zone is empty - keep count synced.
			$ZonePlayerCount[%zoneIndex] = 0;
			
			// Despawn immediately on transitions/pending work, otherwise retry at a slower safety cadence.
			%lastDespawnAttempt = $ZoneEmptyLastDespawnAttempt[%zoneIndex];
			%shouldDespawn = false;
			if(%storedCount > 0 || %despawnPending)
			{
				%shouldDespawn = true;
			}
			else if(%lastDespawnAttempt == "" || %lastDespawnAttempt == -1 || (%now - %lastDespawnAttempt) >= %despawnRetryInterval)
			{
				%shouldDespawn = true;
			}
			
			if(%shouldDespawn)
			{
				$ZoneEmptyLastDespawnAttempt[%zoneIndex] = %now;
				DespawnZoneBots(%zoneIndex);
			}
		}
		else
		{
			// Zone has players - fix the count if it's wrong
			%actualPlayerCount = GetWordCount(%playerList);
			
			if(%actualPlayerCount != %storedCount)
			{
				$ZonePlayerCount[%zoneIndex] = %actualPlayerCount;
				echo("WARNING: PeriodicEmptyZoneCheck - Zone " @ %zoneIndex @ " player count mismatch. Fixed: " @ %storedCount @ " -> " @ %actualPlayerCount);
			}
		}
	}
	
	schedule("PeriodicEmptyZoneCheck();", %checkInterval);
}

// Post-spawn initialization for town bots (Player objects)
// This is called via schedule to ensure bot is fully initialized before setup
function InitTownBotPostSpawn(%aiName, %name)
{
	// Skip invalid bot names (like "0" which can appear in lists)
	if(%aiName == "" || %aiName == "0" || %aiName == -1 || %name == "" || %name == "0" || %name == -1)
		return;
	
	// For town bots, we already stored the client ID in $TownBotSpawned during spawn
	// Use that instead of AI::getId() which only works for Drones
	%clientId = $TownBotSpawned[%name];
	
	// If not found in stored list, try to get it from display name (town bots are Player objects)
	if(%clientId == -1 || %clientId == "")
	{
		%displayName = $BotInfo[%name, NAME];
		if(%displayName != "" && %displayName != -1)
		{
			%clientId = NEWgetClientByName(%displayName);
		}
	}
	
	// Note: We don't use AI::getId() for town bots since they're Player objects, not Drones
	// AI::getId() only works for Drones and will generate "Could not find drone" errors for Player objects
	
	// Validate that we got a valid ID
	if(%clientId == -1 || %clientId == "")
	{
		echo("ERROR: InitTownBotPostSpawn - Could not get client ID for " @ %name @ " (aiName: " @ %aiName @ "). Bot may be broken.");
		return;
	}
	
	// Get player object
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: InitTownBotPostSpawn - could not find player object for clientId " @ %clientId);
		return;
	}
	
	// Set bot name property (used by shop system and other systems)
	%playerObj.name = %name;
	
	// Validate and set race/armor to ensure correct model (fixes issue where bots spawn with wrong race after enemy bots are killed)
	%botRace = $BotInfo[%name, RACE];
	if(%botRace == "" || %botRace == -1)
	{
		echo("WARNING: InitTownBotPostSpawn - Bot " @ %name @ " has no RACE defined, defaulting to Human");
		%botRace = "Human";
	}
	
	%expectedArmor = $RaceToArmorType[%botRace];
	if(%expectedArmor == "" || %expectedArmor == -1)
	{
		echo("WARNING: InitTownBotPostSpawn - Could not find armor for race '" @ %botRace @ "' for bot " @ %name @ ", defaulting to MaleHumanArmor");
		%expectedArmor = "MaleHumanArmor";
	}
	
	// Store race data
	storeData(%clientId, "RACE", %botRace);
	
	// Check current armor and fix if wrong
	%currentArmor = Player::getArmor(%clientId);
	if(%currentArmor != %expectedArmor)
	{
		echo("WARNING: InitTownBotPostSpawn - Bot " @ %name @ " has wrong armor '" @ %currentArmor @ "', correcting to '" @ %expectedArmor @ "'");
		// CRITICAL FIX: Use Player::setArmor to change the armor model, not Client::setSkin
		// Client::setSkin only changes texture/skin, not the actual armor PlayerData
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj != "" && %playerObj != -1)
		{
			Player::setArmor(%playerObj, %expectedArmor);
			// Also set skin to ensure visual appearance matches
			Safe_SetSkin(%clientId, %expectedArmor, "InitTownBotPostSpawn armor fix");
			echo("DEBUG: InitTownBotPostSpawn - Applied armor fix for " @ %name @ ": Player::setArmor(" @ %playerObj @ ", " @ %expectedArmor @ ")");
		}
		else
		{
			echo("ERROR: InitTownBotPostSpawn - Could not get player object to fix armor for " @ %name);
		}
	}
	
	// Set default flag to prevent town bots from dropping loot (edge case bug protection)
	storeData(%clientId, "NoDropLoot", "true");
	
	// Set BotInfoAiName so town bots are recognized as RPG AI bots (for auto-equip and other bot features)
	// CRITICAL: Must use "TownBot_" prefix to match SpawnZoneBots format
	%aiNameFull = "TownBot_" @ %name;
	storeData(%clientId, "BotInfoAiName", %aiNameFull);
	$TownBotData[%clientId, "BotInfoAiName"] = %aiNameFull;
	
	// CRITICAL: Clear SpawnBotInfo to prevent town bots from being mistaken for enemy bots
	// Town bots should NOT have SpawnBotInfo set (only enemy bots have it)
	// This prevents zone change detection from killing town bots
	storeData(%clientId, "SpawnBotInfo", "");
	storeData(%clientId, "SpawnTime", ""); // Also clear SpawnTime to be safe
	
	// Weapon mounting flags (can be overridden per bot if needed)
	// MountWeaponOnSpawn: If "true", weapon will be mounted when bot spawns (default: "false")
	// MountWeaponOnTalk: If "true", weapon will be mounted when player talks to bot (default: "false" - not used, weapons only mount on collision)
	// To override for a specific bot, set these flags after InitTownBotPostSpawn or in the map file
	if(fetchData(%clientId, "MountWeaponOnSpawn") == "")
		storeData(%clientId, "MountWeaponOnSpawn", "false");  // Default: don't mount on spawn
	if(fetchData(%clientId, "MountWeaponOnTalk") == "")
		storeData(%clientId, "MountWeaponOnTalk", "false");   // Default: don't mount on talk (weapons only mount on collision)
	
	// Idle message flag (can be overridden per bot if needed)
	// ShowIdleMessage: If "true", bot will show idle message if 30 minutes pass without interaction (default: "false")
	// To enable for a specific bot, set this flag after InitTownBotPostSpawn or in the map file
	if(fetchData(%clientId, "ShowIdleMessage") == "")
	{
		// Enable idle message for the 3 new Gian Echos bots
		if(%name == "Giovanni Echos" || %name == "GodFather Gian" || %name == "Uncle Tony")
			storeData(%clientId, "ShowIdleMessage", "true");  // Enable for new Gian Echos bots
		else
			storeData(%clientId, "ShowIdleMessage", "false");  // Default: don't show idle message
	}
	
	// Initialize last interaction time (current time)
	storeData(%clientId, "LastInteractionTime", getSimTime());
	
	// Disable AI behavior to prevent town bots from moving or attacking
	// Set AI variables to keep bots stationary
	AI::setVar(%aiName, "pathType", "none");  // Disable pathfinding
	AI::setVar(%aiName, "spotDist", 0);  // Disable target spotting
	AI::setVar(%aiName, "attackMode", 0);  // Disable attacking
	AI::setVar(%aiName, "iq", 0);  // Disable AI intelligence
	
	// Set "dumb AI" flag to prevent any AI behavior
	storeData(%clientId, "dumbAIflag", "true");
	
	// CRITICAL: Set team (may have been set already during spawn, but ensure it's correct)
	// Default to team 0 (same as players) if TEAM is not specified
	// This is a second set to ensure team is correct even if something reset it
	// Town bots ALWAYS use team 0 (Citizen) regardless of BotInfo TEAM setting
	%botTeam = 0;  // Town bots are ALWAYS team 0 (Citizen)
	GameBase::setTeam(%clientId, %botTeam);
	
	// Verify team was set correctly (debug check)
	%actualTeam = GameBase::getTeam(%clientId);
	if(%actualTeam != %botTeam)
	{
		echo("WARNING: InitTownBotPostSpawn - Team mismatch for " @ %name @ " (clientId=" @ %clientId @ "). Expected " @ %botTeam @ " (Citizen), got " @ %actualTeam @ ". Re-setting...");
		GameBase::setTeam(%clientId, %botTeam);
	}
	
	// CRITICAL: Schedule a team verification after a short delay to catch UpdateTeam() overriding it
	schedule("VerifyTownBotTeam(" @ %clientId @ ", \"" @ %name @ "\", " @ %botTeam @ ");", 0.2);
	
	// Set animation to root (idle)
	GameBase::playSequence(%clientId, 0, "root");
	
	// Add to TownBotList (may have been added already during spawn, but ensure it's there)
	// Check if already in list to avoid duplicates
	%alreadyInList = false;
	for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
	{
		if(%id == %clientId)
		{
			%alreadyInList = true;
			break;
		}
	}
	if(!%alreadyInList)
		$TownBotList = $TownBotList @ %clientId @ " ";
	
	// Store items from map file ITEMS field for later mounting
	%items = $BotInfo[%name, ITEMS];
	if(%items != "")
	{
		// Store the items string for later processing
		storeData(%clientId, "TownBotItems", %items);
	}
	
	// Add to MissionCleanup set (use player object, delay slightly to ensure it's fully initialized)
	// Schedule the addToSet call to ensure the player object is fully initialized
	schedule("addToSetMissionCleanup(" @ %clientId @ ");", 0.1);
}

// Helper function to add town bot to MissionCleanup after player object is fully initialized
function addToSetMissionCleanup(%clientId)
{
	// CRITICAL: Validate clientId first
	if(%clientId == "" || %clientId == -1)
	{
		echo("ERROR: addToSetMissionCleanup - Invalid clientId (" @ %clientId @ "), aborting");
		return;
	}
	
	%playerObj = Client::getOwnedObject(%clientId);
	
	// CRITICAL: Validate player object exists and is valid before adding to MissionCleanup
	if(%playerObj != "" && %playerObj != -1 && isObject(%playerObj))
	{
		// Double-check MissionCleanup exists
		if(isObject("MissionCleanup"))
		{
			if($MISSION_CLEANUP_DEBUG) echo("[DEBUG] addToSetMissionCleanup - Adding player object " @ %playerObj @ " to MissionCleanup for clientId " @ %clientId);
			addToSet("MissionCleanup", %playerObj);
		}
		else
		{
			echo("ERROR: addToSetMissionCleanup - MissionCleanup SimSet does not exist for clientId " @ %clientId);
		}
	}
	else
	{
		// Player object doesn't exist - this is normal for bots that have been deleted
		// Only log as warning if the client still exists (to avoid spam for deleted bots)
		if(Client::getName(%clientId) != "" && Client::getName(%clientId) != -1)
		{
			echo("WARNING: addToSetMissionCleanup - player object not found for clientId " @ %clientId @ " (playerObj=" @ %playerObj @ ")");
		}
	}
}

// Mount items for a single town bot (used for dynamically spawned bots)
function InitTownBotItemsForBot(%clientId, %botName)
{
	%items = fetchData(%clientId, "TownBotItems");
	if(%items == "")
		%items = $BotInfo[%botName, ITEMS];
	
	if(%items == "")
	{
		//echo("DEBUG: InitTownBotItemsForBot - No items found for " @ %botName);
		return;
	}
	
	//echo("DEBUG: InitTownBotItemsForBot - Processing items for " @ %botName @ " (clientId: " @ %clientId @ ")");
	
	// Get player object from clientId
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: InitTownBotItemsForBot - could not find player object for clientId " @ %clientId);
		return;
	}
	
	// First pass: Add all items to inventory (including equipped versions for armor/accessories)
	for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
	{
		%itemName = GetWord(%items, %j);
		%itemCount = GetWord(%items, %j + 1);
		
		// Skip non-item keywords
		if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
		   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
		   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
		   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
		   %itemName == "LVLS" || %itemName == "LVLE")
			continue;
		
		if(%itemName != "" && %itemCount != "" && %itemCount > 0)
		{
			%itemData = getItemData(%itemName);
			if(%itemData != "")
			{
				// Add base item to inventory
				Player::incItemCount(%clientId, %itemName, %itemCount);

				// For armor and accessories, also add the equipped version ("0" suffix) to inventory
				// VOID 2026-07-15: NOT for belt-converted armor - its X0 datablocks sit at
				// indexes 198-220 (VoidLegacyEquipped.cs), ABOVE the 200 itemTypeList cap,
				// where incItemCount is an OOB WRITE that crashed the server on every town
				// spawn. Bots don't need the count: the mount pass mounts X0 by NAME.
				if(!isBeltItem(%itemName) && ($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType || %itemData.className == "Accessory"))
				{
					%equippedVersion = %itemName @ "0";
					%equippedData = getItemData(%equippedVersion);
					if(%equippedData != "")
					{
						Player::incItemCount(%clientId, %equippedVersion, %itemCount);
					}
				}
			}
		}
	}
	
	// Second pass: Mount items (use the same logic as InitTownBotItems)
	// This is a simplified version that mounts armor and shields (weapons mount on talk by default)
	%weaponsToMount = "";
	%armorToMount = "";
	%shieldsToMount = "";
	
	for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
	{
		%itemName = GetWord(%items, %j);
		%itemCount = GetWord(%items, %j + 1);
		
		// Skip non-item keywords
		if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
		   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
		   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
		   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
		   %itemName == "LVLS" || %itemName == "LVLE")
			continue;
		
		if(%itemName != "" && %itemCount != "" && %itemCount > 0)
		{
			%itemData = getItemData(%itemName);
			%accessoryType = $AccessoryVar[%itemName, $AccessoryType];
			
			// Check if it's a weapon
			%isWeapon = false;
			if(%itemData != "")
			{
				if(%itemData.className == "Weapon")
					%isWeapon = true;
			}
			if(!%isWeapon && (%accessoryType == 7 || %accessoryType == 9 || %accessoryType == 10))
				%isWeapon = true;
			
			if(%isWeapon)
			{
				if(%itemName != "Dagger")
					%weaponsToMount = %weaponsToMount @ %itemName @ " ";
			}
			else if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType)
			{
				%armorToMount = %armorToMount @ %itemName @ " ";
			}
			else if($AccessoryVar[%itemName, $AccessoryType] == $ShieldAccessoryType)
			{
				%shieldsToMount = %shieldsToMount @ %itemName @ " ";
			}
		}
	}
	
	// Mount armor (always mount armor on spawn)
	if(%armorToMount != "")
	{
		%armorName = GetWord(%armorToMount, 0);
		%equippedArmor = %armorName @ "0";
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck == -1 || %playerObjCheck == "")
		{
			// Bot was deleted - silently return
			return;
		}
		%armorCountInInv = Player::getItemCount(%clientId, %equippedArmor);
		// VOID 2026-07-15: belt-converted armor has NO X0 counts (X0s at 198-220 sit
		// above the 200 cap; the count write there is an OOB server crash, so the
		// give pass skips it). Mounting is by NAME - treat belt armor as mountable.
		if(%armorCountInInv > 0 || isBeltItem(%armorName))
		{
			// Check if armor is already mounted - if so, verify it's the correct armor
			%currentArmor = Player::getMountedItem(%clientId, 1);
			if(%currentArmor == %equippedArmor)
			{
				// Already mounted correctly, but verify skin is correct
				%armorSkin = $ArmorSkin[%armorName];
				if(%armorSkin != "")
				{
					%currentSkin = Client::getSkin(%clientId);
					if(%currentSkin != %armorSkin)
					{
						echo("DEBUG: Armor " @ %equippedArmor @ " already mounted on " @ %botName @ ", but skin is wrong (current: " @ %currentSkin @ ", expected: " @ %armorSkin @ ") - fixing skin");
						Safe_SetSkin(%clientId, %armorSkin, "InitTownBotItemsForBot armor remount");
					}
				}
				echo("DEBUG: Armor " @ %equippedArmor @ " already mounted on " @ %botName @ ", skipping remount");
			}
			else
			{
				// Unmount existing armor if different armor is mounted
				if(%currentArmor != "" && %currentArmor != -1)
				{
					echo("DEBUG: Unmounting existing armor " @ %currentArmor @ " before mounting " @ %equippedArmor);
					Player::unMountItem(%clientId, 1);
				}
				
				%armorSkin = $ArmorSkin[%armorName];
				if(%armorSkin != "")
					Safe_SetSkin(%clientId, %armorSkin, "InitTownBotItemsForBot armor mount");
				Player::mountItem(%playerObj, %equippedArmor, 1, 0);
			}
		}
	}
	
	// Mount shields (always mount shields on spawn)
	if(%shieldsToMount != "")
	{
		%shieldName = GetWord(%shieldsToMount, 0);
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck == -1 || %playerObjCheck == "")
		{
			// Bot was deleted - silently return
			return;
		}
		%shieldCountInInv = Player::getItemCount(%clientId, %shieldName);
		if(%shieldCountInInv > 0)
		{
			%currentShield = Player::getMountedItem(%clientId, 2);
			if(%currentShield != "" && %currentShield != -1)
				Player::unMountItem(%clientId, 2);
			Player::mountItem(%playerObj, %shieldName, 2);
		}
	}
	
	// Weapons mount on collision only (MountWeaponOnSpawn is false by default)
	// Only mount weapon now if MountWeaponOnSpawn is explicitly set to true
	%mountWeaponOnSpawn = fetchData(%clientId, "MountWeaponOnSpawn");
	if(%mountWeaponOnSpawn == "true" && %weaponsToMount != "")
	{
		%weaponName = GetWord(%weaponsToMount, 0);
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck == -1 || %playerObjCheck == "")
		{
			// Bot was deleted - silently return
			return;
		}
		%weaponCountInInv = Player::getItemCount(%clientId, %weaponName);
		if(%weaponCountInInv > 0)
		{
			%currentWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
			if(%currentWeapon != "" && %currentWeapon != -1)
				Player::unMountItem(%clientId, $WeaponSlot);
			Player::mountItem(%playerObj, %weaponName, $WeaponSlot, 0);
			schedule("verifyWeaponMount(" @ %clientId @ ", \"" @ %weaponName @ "\", \"" @ %botName @ "\");", 0.1);
		}
	}
	
	//echo("DEBUG: InitTownBotItemsForBot - Completed for " @ %botName);
}

// Mount items on town bots after initialization completes
// This is called via schedule to prevent hanging during InitTownBots()
// Town bots are now Player objects, so we can use Player functions!
function InitTownBotItems()
{
	echo("===== InitTownBotItems() called =====");
	%mountedCount = 0;
	
	for(%i = 0; (%clientId = GetWord($TownBotList, %i)) != -1; %i++)
	{
		%items = fetchData(%clientId, "TownBotItems");
		if(%items != "")
		{
			%botName = GameBase::getMapName(%clientId);
			echo("DEBUG: InitTownBotItems - Processing items for town bot: " @ %botName @ " (clientId: " @ %clientId @ ")");
			echo("DEBUG: Items string: " @ %items);
			
			// Get player object from clientId
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj == "" || %playerObj == -1)
			{
				echo("ERROR: InitTownBotItems - could not find player object for clientId " @ %clientId);
				continue;
			}
			
			// First pass: Add all items to inventory (including equipped versions for armor/accessories)
			// Second pass: Mount items (this ensures all items are in inventory before mounting)
			for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
			{
				%itemName = GetWord(%items, %j);
				%itemCount = GetWord(%items, %j + 1);
				
				// Skip non-item keywords (CLASS, LVL, COINS, etc.)
				if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
				   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
				   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
				   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
				   %itemName == "LVLS" || %itemName == "LVLE")
					continue;
				
				if(%itemName != "" && %itemCount != "" && %itemCount > 0)
				{
					// Try to get item data - if it fails, skip this item
					%itemData = getItemData(%itemName);
					if(%itemData != "")
					{
						// Add base item to inventory
						Player::incItemCount(%clientId, %itemName, %itemCount);

						// For armor and accessories, also add the equipped version ("0" suffix) to inventory
						// VOID 2026-07-15: NOT for belt-converted armor - X0s at 198-220 are above
						// the 200 cap; incItemCount there = OOB write (server crash on town spawn).
						if(!isBeltItem(%itemName) && ($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType || %itemData.className == "Accessory"))
						{
							%equippedVersion = %itemName @ "0";
							%equippedData = getItemData(%equippedVersion);
							if(%equippedData != "")
							{
								Player::incItemCount(%clientId, %equippedVersion, %itemCount);
								echo("DEBUG: Added equipped version " @ %equippedVersion @ " to inventory for " @ %botName);
							}
						}
					}
				}
			}
			
			// Second pass: Mount items (prioritize special items over default Dagger)
			// First, collect all items and identify what to mount
			%weaponsToMount = "";
			%armorToMount = "";
			%shieldsToMount = "";
			%hasDagger = false;
			
			for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
			{
				%itemName = GetWord(%items, %j);
				%itemCount = GetWord(%items, %j + 1);
				
				// Skip non-item keywords
				if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
				   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
				   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
				   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
				   %itemName == "LVLS" || %itemName == "LVLE")
					continue;
				
				if(%itemName != "" && %itemCount != "" && %itemCount > 0)
				{
					%itemData = getItemData(%itemName);
					if(%itemData != "")
					{
						if(%itemData.className == "Weapon")
						{
							if(%itemName == "Dagger")
								%hasDagger = true;
							else
								%weaponsToMount = %weaponsToMount @ %itemName @ " ";
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType)
						{
							%armorToMount = %armorToMount @ %itemName @ " ";
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $ShieldAccessoryType)
						{
							%shieldsToMount = %shieldsToMount @ %itemName @ " ";
						}
					}
				}
			}
			
			// Now mount items: weapons first (skip Dagger if other weapons exist), then armor, then shields
			for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
			{
				%itemName = GetWord(%items, %j);
				%itemCount = GetWord(%items, %j + 1);
				
				// Skip non-item keywords (CLASS, LVL, COINS, etc.)
				if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
				   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
				   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
				   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
				   %itemName == "LVLS" || %itemName == "LVLE")
					continue;
				
				if(%itemName != "" && %itemCount != "" && %itemCount > 0)
				{
					// Try to get item data - if it fails, skip this item
					%itemData = getItemData(%itemName);
					%accessoryType = $AccessoryVar[%itemName, $AccessoryType];
					
					// Check if it's a weapon - use accessoryType as fallback if className is empty
					%isWeapon = false;
					if(%itemData != "")
					{
						if(%itemData.className == "Weapon")
							%isWeapon = true;
					}
					// Fallback: check accessoryType for weapon types (Slashing=7, Piercing/Polearm=9, Bludgeon=10)
					if(!%isWeapon && (%accessoryType == 7 || %accessoryType == 9 || %accessoryType == 10))
						%isWeapon = true;
					
					if(%itemData != "" || %isWeapon)
					{
						%classNameStr = "empty";
						if(%itemData != "" && %itemData.className != "")
							%classNameStr = %itemData.className;
						echo("DEBUG: Processing item " @ %itemName @ " - className: " @ %classNameStr @ ", accessoryType: " @ %accessoryType @ ", isWeapon: " @ %isWeapon);
						
						// Then, mount items visually
						if(%isWeapon)
						{
							// Check if weapon should be mounted on spawn (flag check)
							%mountWeaponOnSpawn = fetchData(%clientId, "MountWeaponOnSpawn");
							if(%mountWeaponOnSpawn != "true")
							{
								echo("DEBUG: Skipping weapon " @ %itemName @ " - MountWeaponOnSpawn flag is not set to 'true'");
								continue;
							}
							
							// Skip Dagger if there are other weapons to mount
							if(%itemName == "Dagger" && %weaponsToMount != "")
							{
								echo("DEBUG: Skipping Dagger - other weapons will be mounted instead");
								continue;
							}
							
							// Check if item is in inventory before mounting
							%itemCountInInv = Player::getItemCount(%clientId, %itemName);
							if(%itemCountInInv <= 0)
							{
								echo("DEBUG: Weapon " @ %itemName @ " not in inventory (count: " @ %itemCountInInv @ ") - cannot mount");
								continue;
							}
							
							// Mount weapon in slot 0
							// Check if there's already a weapon mounted - unmount it first if needed
							%currentWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
							if(%currentWeapon != "" && %currentWeapon != -1)
							{
								echo("DEBUG: Unmounting existing weapon " @ %currentWeapon @ " before mounting " @ %itemName);
								Player::unMountItem(%clientId, $WeaponSlot);
							}
							echo("DEBUG: Mounting weapon " @ %itemName @ " on town bot " @ %botName @ " (inventory count: " @ %itemCountInInv @ ")");
							// Use 4th parameter (0) like the standard armor mounting code does
							Player::mountItem(%playerObj, %itemName, $WeaponSlot, 0);
							
							// Small delay to ensure mount completes
							schedule("verifyWeaponMount(" @ %clientId @ ", \"" @ %itemName @ "\", \"" @ %botName @ "\");", 0.1);
							
							%mountedCount++;
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $BodyAccessoryType)
						{
							// Mount armor in slot 1 (try equipped version first)
							%equippedArmor = %itemName @ "0";
							%equippedData = getItemData(%equippedArmor);
							if(%equippedData != "")
							{
								// Check if equipped armor is in inventory (should be added in first pass)
								// VOID 2026-07-15: belt-converted armor has NO X0 counts (first pass
								// skips them; X0s at 198-220 are above the 200 cap = OOB writes).
								// Mounting is by NAME, no count needed - bypass the gate for belt
								// armor or town bots spawn unarmored.
								if(!isBeltItem(%itemName))
								{
								%armorCountInInv = Player::getItemCount(%clientId, %equippedArmor);
								if(%armorCountInInv <= 0)
								{
									echo("DEBUG: Equipped armor " @ %equippedArmor @ " not in inventory (count: " @ %armorCountInInv @ ") - cannot mount");
									continue;
								}
								}
								
								// Check if armor is already mounted - if so, verify it's the correct armor
								%currentArmor = Player::getMountedItem(%clientId, 1);
								if(%currentArmor == %equippedArmor)
								{
									// Already mounted correctly, but verify skin is correct
									// Get the armor skin from $ArmorSkin mapping (use base item name, not equipped version)
									%armorSkin = $ArmorSkin[%itemName];
									if(%armorSkin != "")
									{
										%currentSkin = Client::getSkin(%clientId);
										if(%currentSkin != %armorSkin)
										{
											echo("DEBUG: Armor " @ %equippedArmor @ " already mounted on " @ %botName @ ", but skin is wrong (current: " @ %currentSkin @ ", expected: " @ %armorSkin @ ") - fixing skin");
											Safe_SetSkin(%clientId, %armorSkin, "InitTownBotItems equipped armor skin fix");
										}
									}
									%mountedCount++;
									continue;
								}
								
								// Get the armor skin from $ArmorSkin mapping (use base item name, not equipped version)
								%armorSkin = $ArmorSkin[%itemName];
								if(%armorSkin != "")
								{
									echo("DEBUG: Setting skin to " @ %armorSkin @ " for armor " @ %itemName);
									Safe_SetSkin(%clientId, %armorSkin, "InitTownBotItems equipped armor mount");
								}
								
								// Unmount existing armor if different armor is mounted
								if(%currentArmor != "" && %currentArmor != -1)
								{
									echo("DEBUG: Unmounting existing armor " @ %currentArmor @ " before mounting " @ %equippedArmor);
									Player::unMountItem(%clientId, 1);
								}
								
								// Mount the armor item - this will display the armor visually
								echo("DEBUG: Mounting armor " @ %equippedArmor @ " on town bot " @ %botName @ " (inventory count: " @ %armorCountInInv @ ", skin: " @ %armorSkin @ ")");
								Player::mountItem(%playerObj, %equippedArmor, 1, 0);
							}
							else
							{
								// Check if base armor is in inventory
								%armorCountInInv = Player::getItemCount(%clientId, %itemName);
								if(%armorCountInInv <= 0)
								{
									echo("DEBUG: Armor " @ %itemName @ " not in inventory (count: " @ %armorCountInInv @ ") - cannot mount");
									continue;
								}
								
								// Check if armor is already mounted - if so, verify it's the correct armor
								%currentArmor = Player::getMountedItem(%clientId, 1);
								if(%currentArmor == %itemName)
								{
									// Already mounted correctly, but verify skin is correct
									// Get the armor skin from $ArmorSkin mapping
									%armorSkin = $ArmorSkin[%itemName];
									if(%armorSkin != "")
									{
										%currentSkin = Client::getSkin(%clientId);
										if(%currentSkin != %armorSkin)
										{
											echo("DEBUG: Armor " @ %itemName @ " already mounted on " @ %botName @ ", but skin is wrong (current: " @ %currentSkin @ ", expected: " @ %armorSkin @ ") - fixing skin");
											Safe_SetSkin(%clientId, %armorSkin, "InitTownBotItems base armor skin fix");
										}
									}
									%mountedCount++;
									continue;
								}
								
								// Get the armor skin from $ArmorSkin mapping
								%armorSkin = $ArmorSkin[%itemName];
								if(%armorSkin != "")
								{
									echo("DEBUG: Setting skin to " @ %armorSkin @ " for armor " @ %itemName);
									Safe_SetSkin(%clientId, %armorSkin, "InitTownBotItems base armor mount");
								}
								
								// Unmount existing armor if different armor is mounted
								if(%currentArmor != "" && %currentArmor != -1)
								{
									echo("DEBUG: Unmounting existing armor " @ %currentArmor @ " before mounting " @ %itemName);
									Player::unMountItem(%clientId, 1);
								}
								
								// Mount the armor item - this will display the armor visually
								echo("DEBUG: Mounting armor " @ %itemName @ " on town bot " @ %botName @ " (inventory count: " @ %armorCountInInv @ ", skin: " @ %armorSkin @ ")");
								Player::mountItem(%playerObj, %itemName, 1, 0);
							}
							%mountedCount++;
						}
						else if($AccessoryVar[%itemName, $AccessoryType] == $ShieldAccessoryType)
						{
							// Mount shield in slot 2
							// Check if there's already a shield mounted - unmount it first if needed
							%currentShield = Player::getMountedItem(%clientId, 2);
							if(%currentShield != "" && %currentShield != -1)
							{
								echo("DEBUG: Unmounting existing shield " @ %currentShield @ " before mounting " @ %itemName);
								Player::unMountItem(%clientId, 2);
							}
							echo("DEBUG: Mounting shield " @ %itemName @ " on town bot " @ %botName);
							Player::mountItem(%playerObj, %itemName, 2);
							%mountedCount++;
						}
						else if(%itemData.className == "Accessory")
						{
							// Mount accessories in slot 1 (try equipped version first)
							%equippedAccessory = %itemName @ "0";
							%equippedData = getItemData(%equippedAccessory);
							if(%equippedData != "")
							{
								echo("DEBUG: Mounting accessory " @ %equippedAccessory @ " on town bot " @ %botName);
								Player::incItemCount(%clientId, %equippedAccessory, %itemCount);
								Player::mountItem(%playerObj, %equippedAccessory, 1);
							}
							else
							{
								echo("DEBUG: Mounting accessory " @ %itemName @ " on town bot " @ %botName);
								Player::mountItem(%playerObj, %itemName, 1);
							}
							%mountedCount++;
						}
					}
					else
					{
						echo("DEBUG: Could not get item data for " @ %itemName);
					}
				}
			}
		}
	}
	
	echo("===== InitTownBotItems() completed - Mounted " @ %mountedCount @ " items =====");
}


// Helper function to verify weapon was mounted after a delay
function verifyWeaponMount(%clientId, %expectedWeapon, %botName)
{
	// CRITICAL: Validate player object exists before calling Player::getMountedItem
	// Bot may have been deleted between schedule and execution
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Bot was deleted - silently return
		return;
	}
	
	%mountedCheck = Player::getMountedItem(%clientId, $WeaponSlot);
	if(%mountedCheck == %expectedWeapon)
		echo("DEBUG: Successfully mounted weapon " @ %expectedWeapon @ " on " @ %botName);
	else
		echo("DEBUG: WARNING - Weapon mount may have failed. Expected: " @ %expectedWeapon @ ", Got: " @ %mountedCheck);
}

// Helper function to mount weapon for town bot when player collides with them
function MountTownBotWeapon(%clientId)
{
	// Check if this is a town bot (fast check using BotInfoAiName flag - only town bots have this)
	%botName = fetchData(%clientId, "BotInfoAiName");
	if(%botName == "")
	{
		// Not a town bot (BotInfoAiName is only set for town bots)
		return;
	}
	
	// Check if weapon is already mounted FIRST (fast check before expensive operations)
	%currentWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
	if(%currentWeapon != "" && %currentWeapon != -1 && %currentWeapon != "Dagger")
	{
		// Weapon already mounted (and not default Dagger) - exit early
		return;
	}
	
	// Note: Weapons always mount on collision (no flag check needed)
	
	// Get bot name and items
	%botName = fetchData(%clientId, "BotInfoAiName");
	// Fallback to Client::getName or player object name if BotInfoAiName is invalid
	if(%botName == "" || %botName == -1 || %botName == "0")
	{
		%botName = Client::getName(%clientId);
		if(%botName == "" || %botName == -1 || %botName == "0")
		{
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
				%botName = %playerObj.name;
		}
	}
	
	%items = fetchData(%clientId, "TownBotItems");
	if(%items == "")
		%items = $BotInfo[%botName, ITEMS];
	
	if(%items == "")
		return;
	
	// Find the first weapon in inventory to mount
	%weaponToMount = "";
	
	for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
	{
		%itemName = GetWord(%items, %j);
		%itemCount = GetWord(%items, %j + 1);
		
		// Skip non-item keywords
		if(%itemName == "CLASS" || %itemName == "LVL" || %itemName == "COINS" || 
		   %itemName == "REMORT" || %itemName == "LCK" || %itemName == "TOURNYRANK" || 
		   %itemName == "RankPoints" || %itemName == "EXP" || %itemName == "AI" || 
		   %itemName == "CNT" || %itemName == "CNTAFFECTS" || %itemName == "LVLG" || 
		   %itemName == "LVLS" || %itemName == "LVLE")
			continue;
		
		if(%itemName != "" && %itemCount != "" && %itemCount > 0)
		{
			%itemData = getItemData(%itemName);
			%accessoryType = $AccessoryVar[%itemName, $AccessoryType];
			
			// Check if it's a weapon - use accessoryType as fallback if className is empty
			%isWeapon = false;
			if(%itemData != "")
			{
				if(%itemData.className == "Weapon")
					%isWeapon = true;
			}
			// Fallback: check accessoryType for weapon types (Slashing=7, Piercing/Polearm=9, Bludgeon=10)
			if(!%isWeapon && (%accessoryType == 7 || %accessoryType == 9 || %accessoryType == 10))
				%isWeapon = true;
			
			if(%isWeapon)
			{
				// Found first weapon - mount it (could be Dagger or any other weapon)
				%weaponToMount = %itemName;
				break;
			}
		}
	}
	
	if(%weaponToMount != "")
	{
		//echo("DEBUG: MountTownBotWeapon - Mounting weapon: " @ %weaponToMount);
	}
	else
	{
		//echo("DEBUG: MountTownBotWeapon - No weapons found in bot inventory");
	}
	
	if(%weaponToMount != "")
	{
		// Check if weapon is in inventory
		%weaponCount = Player::getItemCount(%clientId, %weaponToMount);
		if(%weaponCount > 0)
		{
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
			{
				// Unmount existing weapon if needed
				if(%currentWeapon != "" && %currentWeapon != -1)
					Player::unMountItem(%clientId, $WeaponSlot);
				
				// Mount the weapon
				Player::mountItem(%playerObj, %weaponToMount, $WeaponSlot, 0);
			}
		}
	}
}
function RotateTownBot(%clientId, %rot)
{
	dbecho($dbechoMode, "RotateTownBot(" @ %clientId @ ", " @ %rot @ ")");

	// Validate clientId
	if(%clientId == "" || %clientId == -1)
	{
		echo("ERROR: RotateTownBot - invalid clientId: " @ %clientId);
		return;
	}

	// Get bot name from stored data first (more reliable)
	%name = fetchData(%clientId, "BotInfoAiName");
	// Fallback to Client::getName or player object name if BotInfoAiName is invalid
	if(%name == "" || %name == -1 || %name == "0")
	{
		%name = Client::getName(%clientId);
		if(%name == "" || %name == -1 || %name == "0")
		{
			// Fallback: try to get from player object
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj != "" && %playerObj != -1)
			{
				%name = %playerObj.name;
			}
		}
	}
	
	if(%name == "" || %name == -1 || %name == "0")
	{
		echo("ERROR: RotateTownBot - could not determine bot name for clientId " @ %clientId);
		return;
	}
	
	%pos = GameBase::getPosition(%clientId);
	if(%pos == "")
	{
		echo("ERROR: RotateTownBot - could not get position for clientId " @ %clientId);
		return;
	}
	
	// For town bots (Player objects), just set the rotation directly - no need to delete/respawn
	// This is much simpler and avoids AI::spawn issues
	GameBase::setRotation(%clientId, %rot);
	
	// Also update the player object rotation if needed
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != "" && %playerObj != -1)
	{
		GameBase::setRotation(%playerObj, %rot);
	}
}

//These are for the pets
function Pet::BeforeTurnEvil(%clientId)
{
	dbecho($dbechoMode, "Pet::BeforeTurnEvil(" @ %clientId @ ")");

	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: Pet::BeforeTurnEvil - Bot " @ %clientId @ " no longer exists. Skipping.");
		return;
	}

	remoteSay(%clientId, 0, "#say I'm starting to get enough of this...");
}
function Pet::TurnEvil(%clientId)
{
	dbecho($dbechoMode, "Pet::TurnEvil(" @ %clientId @ ")");

	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: Pet::TurnEvil - Bot " @ %clientId @ " no longer exists. Skipping.");
		return;
	}

	remoteSay(%clientId, 0, "#shout To hell with you all! Die!");

	// Fixed: Use %clientId instead of undefined %aiId
	storeData(%clientId, "botAttackMode", 1);
	
	// For enemy bots that are Player objects, directives may not work
	// Only try to remove directive if we can get a valid AI name and the bot is a Drone
	%botName = fetchData(%clientId, "BotInfoAiName");
	// Fallback to Client::getName if BotInfoAiName is invalid
	if(%botName == "" || %botName == -1 || %botName == "0")
		%botName = Client::getName(%clientId);
	
	// Only try to remove directive if we have a valid bot name
	// AI::newDirectiveRemove will handle the case where AI::getId() fails (Player objects)
	if(%botName != "" && %botName != -1 && %botName != "0")
		AI::newDirectiveRemove(%botName, 99);
	storeData(%clientId, "tmpbotdata", "");

	GameBase::setTeam(%clientId, 1);
}

// Verify and restore town bot team if it was changed by UpdateTeam() or other functions
function VerifyTownBotTeam(%clientId, %botName, %expectedTeam)
{
	// Validate inputs
	if(%clientId == -1 || %clientId == "" || %botName == "" || %botName == -1)
		return;
	
	// Validate bot still exists
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return;  // Bot doesn't exist anymore
	
	// Verify this is actually a town bot
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%expectedAiName = "TownBot_" @ %botName;
	if(%botInfoAiName != %expectedAiName)
	{
		// Not the bot we're looking for, or not a town bot
		return;
	}
	
	// Check current team
	%currentTeam = GameBase::getTeam(%clientId);
	if(%currentTeam != %expectedTeam)
	{
		echo("WARNING: VerifyTownBotTeam - Team was changed for town bot " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %expectedTeam @ ", got " @ %currentTeam @ ". Restoring...");
		GameBase::setTeam(%clientId, %expectedTeam);
		
		// CRITICAL FIX: Refresh armor visual after team correction
		// When team was -1, engine shows enemy skin even with correct armor model
		// We need to re-apply armor to refresh the visual appearance
		%botRace = $BotInfo[%botName, RACE];
		if(%botRace != "" && %botRace != -1)
		{
			%expectedArmor = $RaceToArmorType[%botRace];
			if(%expectedArmor != "" && %expectedArmor != -1)
			{
				// Refresh armor to fix visual appearance after team correction
				Player::setArmor(%playerObj, %expectedArmor);
				Safe_SetSkin(%clientId, $Server::teamSkin[0], "SetupBot town bot");
				echo("DEBUG: VerifyTownBotTeam - Refreshed armor for " @ %botName @ " after team correction (team " @ %currentTeam @ " -> " @ %expectedTeam @ ", armor='" @ %expectedArmor @ "')");
			}
		}
		
		// Verify it was restored
		%newTeam = GameBase::getTeam(%clientId);
		if(%newTeam != %expectedTeam)
		{
			echo("ERROR: VerifyTownBotTeam - Failed to restore team for town bot " @ %botName @ " (clientId=" @ %clientId @ "). Expected " @ %expectedTeam @ ", got " @ %newTeam @ ".");
			// Try one more time after a short delay
			%cmd = "VerifyTownBotTeam(" @ %clientId @ ", \"" @ %botName @ "\", " @ %expectedTeam @ ");";
			schedule(%cmd, 0.1);
		}
	}
}

// ============================================================================
// FINAL WORD: ForceTownBotSkin
// This function runs at T+2.0s after spawn to guarantee the correct skin/team
// even if other scripts (running at ~1.0s) attempted to change it.
// ============================================================================
function ForceTownBotSkin(%clientId, %botName)
{
	// Validate client ID
	if(%clientId == "" || %clientId == -1)
		return;
	
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return;  // Bot doesn't exist anymore
	
	// Verify this is still a town bot (check for TownBot_ prefix)
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	if(String::findSubStr(%botInfoAiName, "TownBot_") != 0)
		return;  // Not a town bot
	
	// DIAGNOSTIC: Log all values before applying
	%currentTeam = GameBase::getTeam(%clientId);
	%currentObjTeam = GameBase::getTeam(%playerObj);
	%currentArmor = Player::getArmor(%clientId);
	%currentSkin = Client::getSkinBase(%clientId);
	
	// [FINAL FIX] debug wrapper
	if($TOWNBOT_SKIN_DEBUG)
	{
		echo("[FINAL FIX] ForceTownBotSkin: BEFORE - clientId=" @ %clientId @ ", playerObj=" @ %playerObj);
		echo("[FINAL FIX]   Team: client=" @ %currentTeam @ ", obj=" @ %currentObjTeam);
		echo("[FINAL FIX]   Armor: '" @ %currentArmor @ "', Skin: '" @ %currentSkin @ "'");
	}
	
	// Get expected armor from race
	%botRace = $BotInfo[%botName, RACE];
	if(%botRace == "" || %botRace == -1)
		%botRace = "MaleHuman";
	%expectedArmor = $RaceToArmorType[%botRace];
	if(%expectedArmor == "" || %expectedArmor == -1)
		%expectedArmor = "MaleHumanArmor7";
	
	// FORCE Team 0 on BOTH client AND player object
	GameBase::setTeam(%clientId, 0);
	GameBase::setTeam(%playerObj, 0);
	
	// FORCE armor refresh - triggers engine visual update
	Player::setArmor(%playerObj, %expectedArmor);
	
	// FORCE skin with HARDCODED "rpgbase" - exactly like the manual command that worked
	Safe_SetSkin(%clientId, "rpgbase", "SetupBot default");
	
	// DIAGNOSTIC: Log all values after applying
	%newTeam = GameBase::getTeam(%clientId);
	%newObjTeam = GameBase::getTeam(%playerObj);
	%newArmor = Player::getArmor(%clientId);
	%newSkin = Client::getSkinBase(%clientId);
	
	if($TOWNBOT_SKIN_DEBUG)
	{
		echo("[FINAL FIX] ForceTownBotSkin: AFTER - clientId=" @ %clientId);
		echo("[FINAL FIX]   Team: client=" @ %newTeam @ ", obj=" @ %newObjTeam);
		echo("[FINAL FIX]   Armor: '" @ %newArmor @ "', Skin: '" @ %newSkin @ "'");
		echo("[FINAL FIX] ForceTownBotSkin: DONE for " @ %botName);
	}
}
