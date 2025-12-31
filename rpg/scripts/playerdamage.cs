$LOOTBAG_DEBUG = 0; // Toggle [DROP RATE DEBUG] messages in this file
// Helper function to get client ID from a Player object
// For real players, Player::getClient() returns their client ID
// For enemy bots, Player::getClient() returns -1, so we need to do a reverse lookup
// PHASE 2 FIX: Added reverse verification to prevent client ID misidentification
function GetClientIdFromPlayerObject(%playerObj)
{
	if(!isObject(%playerObj))
		return -1;
	
	// OPTIMIZATION: Check cache first to avoid O(N) lookup
	// This dramatically speeds up damage processing in populated areas (e.g. AOE spells)
	%cachedId = $BotClientCache[%playerObj];
	if(%cachedId != "" && Client::getOwnedObject(%cachedId) == %playerObj)
		return %cachedId;
	
	// Try Player::getClient() first (works for real players)
	%clientId = Player::getClient(%playerObj);
	if(%clientId != -1 && %clientId != "")
	{
		// PHASE 2 FIX: Reverse verification - verify the client ID actually owns this Player object
		%verifyPlayerObj = Client::getOwnedObject(%clientId);
		if(%verifyPlayerObj == %playerObj)
		{
			// PHASE 2 FIX: Additional validation - check if client ID was recently freed
			// BUT: Allow it if reverse verification passes (we're processing this specific entity)
			%recentlyFreed = $ClientIdRecentlyFreed[%clientId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				if(%timeSinceFreed < 10)
				{
					// Client ID was recently freed, but reverse verification passed
					// This means we're processing this specific entity's death/cleanup
					// Allow it to proceed (e.g., during Player::onKilled() for bot despawn)
					// Only warn if it's been more than 1 second (to avoid spam during immediate cleanup)
				// SUPPRESSED: This warning was causing console spam - it's informational only
				// if(%timeSinceFreed > 1)
				// {
				// 	echo("WARNING: GetClientIdFromPlayerObject - Client ID " @ %clientId @ " was recently freed " @ %timeSinceFreed @ "s ago, but reverse verification passed. Allowing for entity cleanup.");
				// }
					return %clientId; // Allow it - we're processing this entity
				}
				else
				{
					// Enough time has passed, clear the flag
					$ClientIdRecentlyFreed[%clientId] = "";
					return %clientId; // Valid client ID
				}
			}
			else
			{
				return %clientId; // Valid client ID (no recently freed flag)
			}
		}
		else
		{
			// Reverse verification failed - Player::getClient() returned wrong ID
			// This could be a timing issue (Player object not fully registered) or a real collision
			// TWO-TIER APPROACH: Use Client::getFirst()/getNext() first (most reliable), then range loop as fallback
			%foundCorrectId = -1;
			
			// Tier 1: Use Client::getFirst()/getNext() (most reliable method)
			for(%checkId = Client::getFirst(); %checkId != -1; %checkId = Client::getNext(%checkId))
			{
				%checkPlayerObj = Client::getOwnedObject(%checkId);
				if(%checkPlayerObj == %playerObj)
				{
					%foundCorrectId = %checkId;
					break;
				}
			}
			
			// Tier 2: Range loop fallback (2049-2200) - only if Tier 1 didn't find a match
			if(%foundCorrectId == -1)
			{
				for(%checkId = 2049; %checkId <= 2200; %checkId++)
				{
					%checkPlayerObj = Client::getOwnedObject(%checkId);
					if(%checkPlayerObj == %playerObj)
					{
						%foundCorrectId = %checkId;
						break;
					}
				}
			}
			
			if(%foundCorrectId != -1)
			{
				// Found the correct client ID via brute-force - use it instead
				// Only warn once per Player object to reduce spam
				%warningKey = "PlayerObj_" @ %playerObj;
				if($GetClientIdFromPlayerObject_Warning[%warningKey] == "")
				{
					echo("WARNING: GetClientIdFromPlayerObject - Player::getClient() returned incorrect client ID " @ %clientId @ " (owned by Player object " @ %verifyPlayerObj @ "). Corrected to client ID " @ %foundCorrectId @ " via brute-force search.");
					$GetClientIdFromPlayerObject_Warning[%warningKey] = getSimTime();
					// Clear warning flag after 30 seconds
					schedule("$GetClientIdFromPlayerObject_Warning[" @ %warningKey @ "] = \"\";", 30);
				}
				return %foundCorrectId; // Return the correct client ID
			}
			else
			{
				// Brute-force also failed
				// Check for "Ghost Object" scenario:
				// 1. Player::getClient(%playerObj) returned an ID (%clientId)
				// 2. That ID exists but owns a DIFFERENT object (%verifyPlayerObj)
				// 3. No other client owns the current object (%playerObj)
				
				if(%clientId != -1 && %clientId != "" && %verifyPlayerObj != -1 && %verifyPlayerObj != "")
				{
					// This is a "Ghost Object" - a stale player object that hasn't been fully deleted yet
					// but the client has already spawned a new player object.
					// This happens frequently during ReconcileSpawnCounters cleanup.
					// Log as info/debug only to prevent console spam
					if($GetClientIdFromPlayerObject_Warning[%clientId] == "")
					{
						// echo("INFO: GetClientIdFromPlayerObject - Detected Ghost/Stale Object " @ %playerObj @ " for client " @ %clientId @ " (current owned object is " @ %verifyPlayerObj @ "). Ignoring.");
						$GetClientIdFromPlayerObject_Warning[%clientId] = getSimTime();
						schedule("$GetClientIdFromPlayerObject_Warning[" @ %clientId @ "] = \"\";", 10);
					}
					return -1;
				}
				
				// CRITICAL FIX: If verifyPlayerObj is -1, this is likely a timing issue during bot spawn
				// The Player object exists and Player::getClient() returned a client ID, but
				// Client::getOwnedObject() returns -1 because the Player object isn't fully registered yet.
				// In this case, trust Player::getClient() and return the client ID anyway
				if(%verifyPlayerObj == -1 || %verifyPlayerObj == "")
				{
				// Timing issue - Player object not fully registered yet
					// Player::getClient() returned a client ID, so trust it
					// Only warn once per client ID to reduce spam
					if($GetClientIdFromPlayerObject_Warning[%clientId] == "")
					{
						echo("WARNING: GetClientIdFromPlayerObject - Timing issue detected for client ID " @ %clientId @ ". Player object exists but Client::getOwnedObject returns -1 (Player object not fully registered yet). Using client ID from Player::getClient() anyway.");
						$GetClientIdFromPlayerObject_Warning[%clientId] = getSimTime();
						schedule("$GetClientIdFromPlayerObject_Warning[" @ %clientId @ "] = \"\";", 30);
						
						// CRITICAL: Schedule a delayed orphan check for this specific client ID
						// If the timing issue persists after 10 seconds, it's truly an orphaned object
						// Only schedule if not already scheduled to avoid spam
						if($OrphanCleanupScheduled[%clientId] == "")
						{
							$OrphanCleanupScheduled[%clientId] = true;
							schedule("CleanupOrphanedClientId(" @ %clientId @ ", " @ %playerObj @ ");", 10);
						}
					}
					return %clientId; // Trust Player::getClient() - timing issue, not a real collision
				}
				
				// Real error: Brute-force failed and it's not a clear ghost case or timing issue
				// Only warn once per client ID to reduce spam
				if($GetClientIdFromPlayerObject_Warning[%clientId] == "")
				{
					echo("WARNING: GetClientIdFromPlayerObject - Reverse verification failed for client ID " @ %clientId @ ". Player object mismatch (Player::getClient returned " @ %clientId @ " but Client::getOwnedObject returned " @ %verifyPlayerObj @ "). Brute-force search also failed to find correct client ID.");
					$GetClientIdFromPlayerObject_Warning[%clientId] = getSimTime();
					// Clear warning flag after 10 seconds to allow re-warning if issue persists
					schedule("$GetClientIdFromPlayerObject_Warning[" @ %clientId @ "] = \"\";", 10);
				}
				// Fall through to brute-force search anyway (might find it on retry)
				%clientId = -1;
			}
		}
	}
	
	// For enemy bots, Player::getClient() returns -1
	// Enemy bots have client IDs, but Player::getClient() doesn't return them
	// We need to find the client ID that owns this Player object (reverse lookup)
	// TWO-TIER APPROACH: Use Client::getFirst()/getNext() first (most reliable), then range loop as fallback
	%foundClientId = -1;
	
	// Tier 1: Use Client::getFirst()/getNext() (most reliable method)
	for(%checkId = Client::getFirst(); %checkId != -1; %checkId = Client::getNext(%checkId))
	{
		// PHASE 2 FIX: Skip client IDs that were recently freed
		%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
		if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
		{
			%currentTime = getSimTime();
			%timeSinceFreed = %currentTime - %recentlyFreed;
			if(%timeSinceFreed < 10)
			{
				continue; // Skip recently freed client IDs
			}
			else
			{
				// Enough time has passed, clear the flag
				$ClientIdRecentlyFreed[%checkId] = "";
			}
		}
		
		%ownedPlayerObj = Client::getOwnedObject(%checkId);
		if(%ownedPlayerObj == %playerObj)
		{
			// PHASE 2 FIX: Reverse verification - double-check the client ID owns this Player object
			%verifyPlayerObj = Client::getOwnedObject(%checkId);
			if(%verifyPlayerObj == %playerObj)
			{
				// PHASE 2 FIX: If this client ID was recently freed but reverse verification passes,
				// allow it (we're processing this specific entity's cleanup/death)
				%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
				if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
				{
					%currentTime = getSimTime();
					%timeSinceFreed = %currentTime - %recentlyFreed;
					if(%timeSinceFreed < 10)
					{
						// Recently freed but reverse verification passed - allow it for cleanup
						// Only warn if it's been more than 1 second (to avoid spam during immediate cleanup)
						if(%timeSinceFreed > 1)
						{
							echo("WARNING: GetClientIdFromPlayerObject - Client ID " @ %checkId @ " was recently freed " @ %timeSinceFreed @ "s ago, but reverse verification passed. Allowing for entity cleanup.");
						}
						// Continue to return this ID - we're processing this entity
					}
					else
					{
						// Enough time has passed, clear the flag
						$ClientIdRecentlyFreed[%checkId] = "";
					}
				}
				
				// (Legacy validation removed - unified safeguards prioritize isFile() check elsewhere)
				
				%foundClientId = %checkId; // Found client ID for enemy bot or player
				$BotClientCache[%playerObj] = %foundClientId; // Cache it for next time
				break;
			}
		}
	}
	
	// Tier 2: Range loop fallback (2049-2200) - only if Tier 1 didn't find a match
	if(%foundClientId == -1)
	{
		for(%checkId = 2049; %checkId <= 2200; %checkId++)
		{
			// PHASE 2 FIX: Skip client IDs that were recently freed
			%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
			if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
			{
				%currentTime = getSimTime();
				%timeSinceFreed = %currentTime - %recentlyFreed;
				if(%timeSinceFreed < 10)
				{
					continue; // Skip recently freed client IDs
				}
				else
				{
					// Enough time has passed, clear the flag
					$ClientIdRecentlyFreed[%checkId] = "";
				}
			}
			
			%ownedPlayerObj = Client::getOwnedObject(%checkId);
			if(%ownedPlayerObj == %playerObj)
			{
				// PHASE 2 FIX: Reverse verification - double-check the client ID owns this Player object
				%verifyPlayerObj = Client::getOwnedObject(%checkId);
				if(%verifyPlayerObj == %playerObj)
				{
					// PHASE 2 FIX: If this client ID was recently freed but reverse verification passes,
					// allow it (we're processing this specific entity's cleanup/death)
					%recentlyFreed = $ClientIdRecentlyFreed[%checkId];
					if(%recentlyFreed != "" && %recentlyFreed != "0" && %recentlyFreed != -1)
					{
						%currentTime = getSimTime();
						%timeSinceFreed = %currentTime - %recentlyFreed;
						if(%timeSinceFreed < 10)
						{
							// Recently freed but reverse verification passed - allow it for cleanup
							// Only warn if it's been more than 1 second (to avoid spam during immediate cleanup)
							if(%timeSinceFreed > 1)
							{
								echo("WARNING: GetClientIdFromPlayerObject - Client ID " @ %checkId @ " was recently freed " @ %timeSinceFreed @ "s ago, but reverse verification passed. Allowing for entity cleanup.");
							}
							// Continue to return this ID - we're processing this entity
						}
						else
						{
							// Enough time has passed, clear the flag
							$ClientIdRecentlyFreed[%checkId] = "";
						}
					}
					
					// (Legacy validation removed - unified safeguards prioritize isFile() check elsewhere)
					
					%foundClientId = %checkId; // Found client ID for enemy bot or player
					$BotClientCache[%playerObj] = %foundClientId; // Cache it for next time
					break;
				}
			}
		}
	}
	
	if(%foundClientId != -1)
		return %foundClientId;
	
	// Fallback: return -1 if not found (shouldn't happen for valid bots)
	return -1;
}

function Client::onKilled(%clientId, %killerId, %damageType)
{
	dbecho($dbechoMode, "Client::onKilled(" @ %clientId @ ", " @ %killerId @ ", " @ %damageType @ ")");

	//This function is NOT an event, it must be MANUALLY CALLED!
	//At this point, the client can still be queried for getItemCounts, but is not considered an object anymore.

	//we can award the other players exp
	if(!fetchData(%clientId, "noExperienceFlag"))
		DistributeExpForKilling(%clientId);

	//The player with the killshot gets the official "kill"
	if(!IsInCommaList(fetchData(%killerId, "TempKillList"), Client::getName(%clientId)))
	storeData(%killerId, "TempKillList", AddToCommaList(fetchData(%killerId, "TempKillList"), Client::getName(%clientId)));

	if(%killerId != %clientId)
	{
		//a human player killed %clientId
		%n = Client::getName(%killerId);

		Client::sendMessage(%clientId, 0, "You were killed by " @ %n @ "!");

		//if(fetchData(%killerId, "bounty") == Client::getName(%clientId))
		//	storeData(%killerId, "bounty", fetchData(%clientId, "LVL") @ " !Q@W#E$R%T^Y&U*I(O)P");
	}
	else if(%killerId == %clientId)
	{
		Client::sendMessage(%clientId, 0, "You killed yourself!");
	}
	else if(%damageType == 11 || %damageType == 12 || %damageType == 2)
	{
		Client::sendMessage(%clientId, 0, "You were killed!");
	}

	//Check to see if the player was in a sanctioned battle
	if($Colloseum::Mode == "Seal")
	{
		if($Colloseum::Client == %clientId)
			Colloseum::EndSeal(%clientId, false);
		else if($Colloseum::Client == %killerId)
			if(Colloseum::isSealComplete())
				Colloseum::EndSeal(%killerId, true);
	}
	else if($Colloseum::Mode == "Colloseum")
	{
		if($Colloseum::Client == %clientId)
			Colloseum::roomReset(%clientId, true);
		else if($Colloseum::Client == %killerId)
			Colloseum::checkRound(%clientId, $Colloseum::Round);
	}
	else if(Duel::isDueling(%clientId) && Duel::isDueling(%killerId))
	{
		if($Duel::Challenger == Client::getName(%killerId))
			Duel::endDuel(true);
		else
			Duel::endDuel(false);
	}

	UnHide(%clientId);

	//========================================================================================================================

	//echo("GAME: kill " @ %killerId @ " " @ %clientId @ " " @ %damageType);
	%clientId.guiLock = true;
	Client::setGuiMode(%clientId, $GuiModePlay);

	Game::clientKilled(%clientId, %killerId);
}

// Helper function to get a consistent name for a client (player or bot)
// For enemy bots, tries BotInfoAiName first, then falls back to Client::getName()
// This ensures consistent naming for EXP distribution and damage tracking
// CRITICAL: Uses $BotType[] for O(1) bot detection instead of slow isRPGAI()
function GetClientOrBotName(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return "";
	
	// FAST PATH: Use $BotType[] cache for O(1) bot detection
	// This is set when bots spawn and cleared when they die, so it's reliable
	// Unlike isRPGAI() which can fail due to race conditions or file system checks
	%botType = $BotType[%clientId];
	if(%botType == "enemy" || %botType == "town")
	{
		%botName = fetchData(%clientId, "BotInfoAiName");
		if(%botName != "" && %botName != -1 && %botName != "0")
			return %botName;
	}
	
	// SECONDARY PATH: Check isRPGAI() as fallback for edge cases
	// (e.g., if $BotType wasn't set yet due to spawn timing)
	if(isRPGAI(%clientId))
	{
		%botName = fetchData(%clientId, "BotInfoAiName");
		if(%botName != "" && %botName != -1 && %botName != "0")
			return %botName;
	}
	
	// Fallback to Client::getName() for players or if BotInfoAiName is not set
	%name = Client::getName(%clientId);
	if(%name != "" && %name != -1 && %name != "0")
		return %name;
	
	// Last resort: return empty string (shouldn't happen, but be safe)
	return "";
}

// Helper function to display damage messages based on player preference
// %clientId: The client to send the message to
// %message: The message text (may contain alignment tags like <jl> or <jr>)
// %msgColor: Optional message color for chat mode (defaults to $MsgRed)
// %viewType: Optional view type for floating damage ("attacker" or "defender", defaults to "defender")
function DisplayDamageMessage(%clientId, %message, %msgColor, %viewType)
{
	if(%clientId == "" || %clientId == -1)
		return;
	
	// Get player's damage display preference
	%displayType = fetchData(%clientId, "damageDisplayType");
	if(%displayType == "")
		%displayType = "bottomprint"; // Default to bottomprint
	
	// If set to floating, send to ATKText system
	if(%displayType == "floating")
	{
		// Get player's animation style preference
		%animationStyle = fetchData(%clientId, "floatingAnimationStyle");
		if(%animationStyle == "" || %animationStyle == -1)
			%animationStyle = "float"; // Default style
		
		// Clean the message for ATKText (remove alignment tags)
		%cleanMsg = %message;
		%cleanMsg = String::replace(%cleanMsg, "<jl>", "");
		%cleanMsg = String::replace(%cleanMsg, "<jr>", "");
		%cleanMsg = String::replace(%cleanMsg, "<jc>", "");
		%cleanMsg = String::replace(%cleanMsg, "<f1>", "");
		
		// Default view type to "defender" if not specified
		if(%viewType == "" || %viewType == -1)
			%viewType = "defender";
		
		// Send to floating damage display with appropriate view type
		remoteEval(%clientId, "ATKText", %cleanMsg, %animationStyle, %viewType);
		return;
	}
	
	if(%displayType == "chat")
	{
		// Remove alignment tags for chat display (<jl>, <jr>, <jc>, etc.)
		%cleanMsg = %message;
		// Remove common alignment tags
		%cleanMsg = String::replace(%cleanMsg, "<jl>", "");
		%cleanMsg = String::replace(%cleanMsg, "<jr>", "");
		%cleanMsg = String::replace(%cleanMsg, "<jc>", "");
		%cleanMsg = String::replace(%cleanMsg, "<f1>", ""); // Font tag sometimes used
		
		// Use provided color or default to red
		if(%msgColor == "")
			%msgColor = $MsgRed;
		
		Client::sendMessage(%clientId, %msgColor, %cleanMsg);
	}
	else
	{
		// Use bottomprint with alignment tags (default behavior)
		bottomprint(%clientId, %message, floor(String::len(%message) / 20));
	}
}

function Game::clientKilled(%playerId, %killerId)
{
	dbecho($dbechoMode, "Game::clientKilled(" @ %playerId @ ", " @ %killerId @ ")");

	%set = nameToID("MissionCleanup/ObjectivesSet");
	if(%set != -1 && %set != "")
	{
		for(%i = 0; (%obj = Group::getObject(%set, %i)) != -1; %i++)
		{
			if(%obj != -1 && %obj != "")
				GameBase::virtual(%obj, "clientKilled", %playerId, %killerId);
		}
	}
}

function Player::onKilled(%this)
{
	// WATCHDOG: Track this function for freeze detection
	Watchdog_Enter("Player::onKilled");
	dbecho($dbechoMode, "Player::onKilled(" @ %this @ ")");
	
	// CRITICAL DEBUG: Log object ID and attempt to identify what this is
	%playerObj = %this;
	%clientIdFromGetClient = Player::getClient(%this);
	%clientIdFromHelper = GetClientIdFromPlayerObject(%this);
	%nameFromClient = "";
	%nameFromAI = "";
	%isAiControlled = Player::isAiControlled(%this);
	
	// Try to get name via client ID
	if(%clientIdFromGetClient != -1 && %clientIdFromGetClient != "")
		%nameFromClient = Client::getName(%clientIdFromGetClient);
	if(%clientIdFromHelper != -1 && %clientIdFromHelper != "")
		%nameFromClient = Client::getName(%clientIdFromHelper);
	
	// Get bot data for additional context - use clientId not playerObj as key
	%debugClientId = %clientIdFromHelper;
	if(%debugClientId == -1 || %debugClientId == "")
		%debugClientId = %clientIdFromGetClient;
	%botInfoAiName = fetchData(%debugClientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%debugClientId, "SpawnBotInfo");
	
	echo("[ONKILLED DEBUG] === Player::onKilled() ENTRY ===");
	echo("[ONKILLED DEBUG]   PlayerObject(%%this): " @ %playerObj);
	echo("[ONKILLED DEBUG]   Player::getClient(): " @ %clientIdFromGetClient);
	echo("[ONKILLED DEBUG]   GetClientIdFromPlayerObject(): " @ %clientIdFromHelper);
	echo("[ONKILLED DEBUG]   Player::isAiControlled(): " @ %isAiControlled);
	echo("[ONKILLED DEBUG]   Client::getName(): '" @ %nameFromClient @ "'");
	echo("[ONKILLED DEBUG]   BotInfoAiName (via clientId " @ %debugClientId @ "): '" @ %botInfoAiName @ "'");
	echo("[ONKILLED DEBUG]   SpawnBotInfo (via clientId " @ %debugClientId @ "): '" @ %spawnBotInfo @ "'");
	
	// CRITICAL: Detect if this might be a real player
	%hasCharFile = false;
	if(%nameFromClient != "" && %nameFromClient != -1)
	{
		%characterFile = "temp\\" @ %nameFromClient @ ".cs";
		%hasCharFile = isFile(%characterFile);
	}
	
	// WARNING: If this looks like a real player (has char file) but is being killed via this path
	if(%hasCharFile && !%isAiControlled)
	{
		// Get zone info for diagnostic purposes
		%clientIdForZone = %clientIdFromHelper;
		if(%clientIdForZone == "" || %clientIdForZone == -1)
			%clientIdForZone = %clientIdFromGetClient;
		%zoneIndex = fetchData(%clientIdForZone, "zone");
		%zoneType = Zone::getType(%zoneIndex);
		%hp = fetchData(%clientIdForZone, "HP");
		%killerId = fetchData(%clientIdForZone, "tmpkillerid");
		
		echo("*** CRITICAL WARNING *** Player::onKilled() called on REAL PLAYER: '" @ %nameFromClient @ "' (clientId=" @ %clientIdForZone @ ", playerObj=" @ %playerObj @ ")");
		echo("*** CRITICAL WARNING *** Zone: " @ %zoneIndex @ " (Type: " @ %zoneType @ "), HP: " @ %hp @ ", KillerID: " @ %killerId);
		echo("*** CRITICAL WARNING *** BotInfoAiName='" @ %botInfoAiName @ "', SpawnBotInfo='" @ %spawnBotInfo @ "'");
		
		// EXTRA CRITICAL: If player in PROTECTED zone - this should NEVER happen
		if(%zoneType == "PROTECTED")
		{
			echo("*** CRITICAL ERROR *** REAL PLAYER DIED IN PROTECTED ZONE! This is a bug!");
			echo("*** CRITICAL ERROR *** PlayerObj=" @ %playerObj @ " OwnsObj=" @ Client::getOwnedObject(%clientIdForZone));
			echo("*** CRITICAL ERROR *** InLootbagGroup: " @ (String::findSubStr(Group::objectCount(nameToID("LootbagGroup")), %playerObj) != -1));
		}
	}

	//At this point, the client can still be queried for getItemCounts, and is also still an object
	//Player::Kill calls this function

	// Use helper function to get client ID from Player object
	%clientId = GetClientIdFromPlayerObject(%this);
	
	// DEBUG: Check player object status - try multiple ways to get the name
	// NOTE: %this IS the Player object, not a client ID
	%isPlayerObjValid = isObject(%this);
	%playerNameFromThis = Client::getName(%this);
	%playerNameFromObj = %this.name;
	%isBot = false;
	
	// Check if this is a bot (enemy bots have client IDs 2049-2200, but Player::getClient() returns -1)
	if(%clientId == -1 || %clientId == "")
	{
		// Fallback: Check if this is an enemy bot by looking for BotInfoAiName or SpawnBotInfo
		// This is a last resort if client ID lookup fails
		%botInfoAiName = fetchData(%this, "BotInfoAiName");
		%spawnBotInfo = fetchData(%this, "SpawnBotInfo");
		
		// If it's an enemy bot (has SpawnBotInfo) or a town bot (has BotInfoAiName but no SpawnBotInfo)
		if((%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0") || 
		   (%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0"))
		{
			// This is a bot - use %this as the client ID (fallback for compatibility)
			%clientId = %this;
			%isBot = true;
		}
		else
		{
			// Not a bot and no valid client ID - this shouldn't happen, but return to be safe
			if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): ERROR - Invalid clientId for player object " @ %this @ " and not a bot. Display name from %this: '" @ %playerNameFromThis @ "'");
			return;
		}
	}
	else
	{
		// CRITICAL SAFEGUARD: Check if player has a character save file FIRST
		// If a character save file exists, this is DEFINITELY a real player, NOT a bot
		// This prevents false positives from stale bot data in arrays ($EnemyBotData, $TownBotData, etc.)
		// that can cause isRPGAI() to incorrectly return true for real players
		%playerNameCheck = Client::getName(%clientId);
		%hasCharacterFile = false;
		if(%playerNameCheck != "" && %playerNameCheck != -1)
		{
			%characterFile = "temp\\" @ %playerNameCheck @ ".cs";
			if(isFile(%characterFile))
			{
				%hasCharacterFile = true;
				// CRITICAL: This is a real player with a save file - NEVER treat as bot
				// Clear any stale bot data that might be in arrays for this clientId
				$EnemyBotData[%clientId, "BotInfoAiName"] = "";
				$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
				$TownBotData[%clientId, "BotInfoAiName"] = "";
				$TownBotData[%clientId, "SpawnBotInfo"] = "";
				$ClientData[%clientId, "BotInfoAiName"] = "";
				$ClientData[%clientId, "SpawnBotInfo"] = "";
				$BotInfoAiName[%clientId] = "";
				if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Player " @ %playerNameCheck @ " (clientId=" @ %clientId @ ") has character save file - cleared stale bot data from arrays, treating as REAL PLAYER");
			}
		}
		
		// Only check for bot indicators if player doesn't have a character file
		if(!%hasCharacterFile)
		{
			// CRITICAL: Check for actual bot indicators, NOT just client ID range
			// Players can also have client IDs in the 2049-2200 range, so we must verify bot data
			%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
			%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
			
			// Only flag as bot if it has actual bot data
			if((%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0") || 
			   (%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0"))
			{
				%isBot = true;
			}
			// Also check if it's AI-controlled using engine functions
			else if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
			{
				%isBot = true;
			}
		}
		// If player has a character file, %isBot stays false (initialized at top of function)
	}
	
	// Debug logging for bots
	if(%isBot)
	{
		// Try to get name from clientId if %this didn't work
		%playerNameFromClientId = Client::getName(%clientId);
		%playerObjFromClientId = Client::getOwnedObject(%clientId);
		%playerObjStatusFromThis = "INVALID";
		if(%isPlayerObjValid)
			%playerObjStatusFromThis = "VALID";
		%playerObjStatusFromClientId = "INVALID";
		if(%playerObjFromClientId != -1 && %playerObjFromClientId != "" && isObject(%playerObjFromClientId))
			%playerObjStatusFromClientId = "VALID";
		%playerObjStatusFromThisStr = "INVALID";
		if(%isPlayerObjValid)
			%playerObjStatusFromThisStr = "VALID";
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Bot detected - BotInfoAiName='" @ %botInfoAiName @ "', SpawnBotInfo='" @ %spawnBotInfo @ "', Display name from %this: '" @ %playerNameFromThis @ "', Display name from %clientId: '" @ %playerNameFromClientId @ "', Display name from %this.name: '" @ %playerNameFromObj @ "', isObject(%this): " @ %playerObjStatusFromThisStr @ ", Player object from %clientId: " @ %playerObjStatusFromClientId @ ", clientId=" @ %clientId);
	}
	else
	{
		// Real player - just log basic info
		%playerNameFromClientId = Client::getName(%clientId);
		if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Real player - clientId=" @ %clientId @ ", Display name from %this: '" @ %playerNameFromThis @ "', Display name from %clientId: '" @ %playerNameFromClientId @ "'");
	}
	
	%killerId = fetchData(%clientId, "tmpkillerid");
	storeData(%clientId, "tmpkillerid", "");
	
	// Validate killerId - if invalid, set to 0 (no killer)
	if(%killerId == -1 || %killerId == "")
		%killerId = 0;

	//revert
	if(%clientId.possessId != "" && %clientId.possessId != -1)
	{
		Client::setControlObject(%clientId.possessId, %clientId.possessId);
		storeData(%clientId.possessId, "dumbAIflag", "");
		$possessedBy[%clientId.possessId] = "";
	}
	Client::setControlObject(%clientId, %clientId);

	// CRITICAL: For enemy bots, explicitly clear noDropLootbagFlag at the start to prevent stale array values
	// But if a caller intentionally set noDropLootbagFlag (e.g., zone despawn), preserve it.
	%spawnBotInfoCheck = fetchData(%clientId, "SpawnBotInfo");
	%preSetNoDrop = fetchData(%clientId, "noDropLootbagFlag");
	if(%spawnBotInfoCheck != "" && %spawnBotInfoCheck != "0" && %spawnBotInfoCheck != -1)
	{
		// This is an enemy bot - explicitly clear the flag from all possible storage locations
		// unless it was intentionally set before death (e.g., zone despawn cleanup).
		if(!%preSetNoDrop)
		{
			storeData(%clientId, "noDropLootbagFlag", "");
			$EnemyBotData[%clientId, "noDropLootbagFlag"] = "";
			$ClientData[%clientId, "noDropLootbagFlag"] = "";
		}
	}

	// SAFETY: If AI-controlled but not fully initialized (no SpawnBotInfo/HasLoadedAndSpawned), force no-drop to avoid stray owner-named packs
	%isAIControlled = Player::isAiControlled(%clientId);
	%hasLoaded = fetchData(%clientId, "HasLoadedAndSpawned");
	if(%isAIControlled && (%spawnBotInfoCheck == "" || %spawnBotInfoCheck == -1 || %spawnBotInfoCheck == "0") && !%hasLoaded)
	{
		storeData(%clientId, "noDropLootbagFlag", True);
		$EnemyBotData[%clientId, "noDropLootbagFlag"] = True;
		$ClientData[%clientId, "noDropLootbagFlag"] = True;
	}

	%noDropFlag = fetchData(%clientId, "noDropLootbagFlag");
	%isJailed = %clientId.isJailed;
	%isDueling = Duel::isDueling(%clientId);
	
	if(%noDropFlag || %isJailed || %isDueling)
	{
		//do nothing
	}
	else
	{
		%tmploot = "";

		if(fetchData(%clientId, "COINS") > 0)
		{
			%coinsDrop = floor(fetchData(%clientId, "COINS"));
			%tmploot = SetStuffString(%tmploot, "COINS", %coinsDrop);
		}
		storeData(%clientId, "COINS", 0);

		%eitem = player::getMountedItem(%clientId, $WeaponSlot);
		if(%eitem != -1 && $preventDrop[%eitem] != true)
		{
			// Always prevent dropping CastingBlade - it's AI-only
			if(%eitem == CastingBlade)
			{
				// Skip mounted weapon handling for CastingBlade
			}
			else
			{
				%isAI = isRPGAI(%clientId);
				%shouldDropWeapon = true;
				%weaponDropRoll = true; // Track if drop chance passed for AI bots
				
				%player = Client::getOwnedObject(%clientId);
				if(%player != -1 && %player != "" && isObject(%player))
				{
					// CRITICAL: Re-validate player object right before Player::getItemCount call
					// Bot may have been deleted (AI::delete() scheduled) between check and this call
					%playerCheck = Client::getOwnedObject(%clientId);
					if(%playerCheck == -1 || %playerCheck == "" || !isObject(%playerCheck))
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "")
							%botName = fetchData(%clientId, "SpawnBotInfo");
						echo("[DEBUG getItemCount] Player::onKilled - Player object deleted before weapon count check, clientId: " @ %clientId @ ", bot: " @ %botName @ ", weapon: " @ %eitem);
						// Skip weapon handling if player object is gone
						%eamnt = 0;
					}
					else
					{
						// Get current weapon count
						%eamnt = Player::getItemCount(%clientId, %eitem);
					}
					
					// CRITICAL: Drop chance now happens on DEATH (not on spawn) for weapons
					// OriginalLootString contains weapons with percentage format (e.g., "weapon count/percentage")
					// Weapons in OriginalLootString: roll percentage on death
					// Weapons NOT in OriginalLootString but in inventory: from lootbags, always drop 100%
					%originalWeaponCount = 0;
					%originalWeaponCountStr = "";
					%hasExtras = false;
					%isWeaponInOriginalLootString = false;
					%weaponDropRollSucceeded = false;
					
					if(%isAI && %eamnt > 0)
					{
						%originalLootString = fetchData(%clientId, "OriginalLootString");
						if(%originalLootString != "")
						{
							// Search for weapon in original loot string
							// Skip known non-item keywords: CLASS, LVL, COINS, REMORT, LCK, TOURNYRANK, RankPoints, EXP, AI, CNT, CNTAFFECTS, LVLG, LVLS, LVLE
							// Note: When we skip a keyword, we also need to skip its value, so we increment by 2 in the continue statement
							for(%k = 0; GetWord(%originalLootString, %k) != -1; %k += 2)
							{
								%origItem = GetWord(%originalLootString, %k);
								
								// Skip known non-item keywords AND their values (increment k by 2 total, but loop already does +2, so just continue)
								if(%origItem == "CLASS" || %origItem == "LVL" || %origItem == "COINS" || %origItem == "REMORT" || 
								   %origItem == "LCK" || %origItem == "TOURNYRANK" || %origItem == "RankPoints" || 
								   %origItem == "EXP" || %origItem == "AI" || %origItem == "CNT" || %origItem == "CNTAFFECTS" || 
								   %origItem == "LVLG" || %origItem == "LVLS" || %origItem == "LVLE")
									continue; // Loop already increments by 2, so this correctly skips both keyword and its value
								
								// Case-insensitive comparison to handle weapon name inconsistencies
								if(String::ICompare(%origItem, %eitem) == 0)
								{
									%originalCountStr = GetWord(%originalLootString, %k + 1);
									%isWeaponInOriginalLootString = true;
									
									// Extract count and percentage from format "count/percentage" or just "count"
									%spos = String::findSubStr(%originalCountStr, "/");
									if(%spos > 0)
									{
										%originalWeaponCount = floor(String::getSubStr(%originalCountStr, 0, %spos));
										%originalWeaponCountStr = %originalCountStr; // Keep full string for percentage roll
									}
									else
									{
										%originalWeaponCount = floor(%originalCountStr);
										%originalWeaponCountStr = %originalCountStr;
									}
									
									// If bot has more than original count, it picked up extras from lootbags
									if(%eamnt > %originalWeaponCount)
									{
										%hasExtras = true;
									}
									break;
								}
							}
						}
					}
					
					// Determine what to drop based on whether weapon is in OriginalLootString
					%weaponDropCount = 0;
					%ammoDropCount = 0;
					
					if(%isAI)
					{
						if(%isWeaponInOriginalLootString)
						{
							// Weapon is in OriginalLootString - roll percentage on death
							%spos = String::findSubStr(%originalWeaponCountStr, "/");
							if(%spos > 0)
							{
								// Has percentage format - roll it
								%perc = String::getSubStr(%originalWeaponCountStr, %spos+1, 99999);
								
								// Use existing drop rate roll logic (same as belt items below)
								%rangePos = String::findSubStr(%perc, "-");
								%firstChar = String::getSubStr(%perc, 0, 1);
								%weaponDropRollSucceeded = false;
								
								if(%rangePos > 0 && %firstChar != "-")
								{
									// Variable drop rate range
									%minPerc = String::getSubStr(%perc, 0, %rangePos);
									%maxPerc = String::getSubStr(%perc, %rangePos + 1, 99999);
									%chance = %minPerc + floor(getRandom() * (%maxPerc - %minPerc + 1));
									if(%chance > %maxPerc) %chance = %maxPerc;
									%roll = floor(getRandom() * 100) + 1;
									if(%roll <= %chance)
										%weaponDropRollSucceeded = true;
								}
								else if(%perc < 0)
								{
									// Negative percentages
									%absPerc = -%perc;
									%roll = floor(getRandom() * 100) + 1;
									%minRoll = 100 - %absPerc + 1;
									if(%absPerc >= 100) %minRoll = 100;
									if(%roll >= %minRoll)
										%weaponDropRollSucceeded = true;
								}
								else
								{
									// Single fixed percentage or "1 in X" format
									%percNum = %perc * 1;
									
									if(%percNum > 100)
									{
										// "1 in X" format
										%roll = floor(getRandom() * %percNum) + 1;
										if(%roll == 1)
											%weaponDropRollSucceeded = true;
									}
									else if(%percNum < 1)
									{
										// Decimal percentage
										%roll = floor(getRandom() * 100000) + 1;
										%target = %percNum * 1000;
										if(%roll <= %target)
											%weaponDropRollSucceeded = true;
									}
									else
									{
										// Normal percentage
										%roll = floor(getRandom() * 100) + 1;
										if(%roll <= %percNum)
											%weaponDropRollSucceeded = true;
									}
								}
								
								if(%weaponDropRollSucceeded)
								{
									// Roll succeeded - drop the weapon
									// If bot has extras (picked up from lootbags), drop ALL weapons (original + extras)
									// If no extras, drop only the original count
									if(%hasExtras)
									{
										// Bot has extras - drop ALL weapons (original passed roll + extras from lootbags always drop)
										%weaponDropCount = %eamnt;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Weapon: " @ %eitem @ " | Percentage: " @ %perc @ "% | Roll: SUCCESS | Dropping all " @ %eamnt @ " weapons (original: " @ %originalWeaponCount @ ", extras: " @ (%eamnt - %originalWeaponCount) @ ")");
									}
									else
									{
										// No extras - drop only the original count
										%weaponDropCount = %originalWeaponCount;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Weapon: " @ %eitem @ " | Percentage: " @ %perc @ "% | Roll: SUCCESS | Dropping " @ %weaponDropCount @ " weapons");
									}
									%shouldDropWeapon = true;
									%weaponDropRoll = true;
								}
								else
								{
									// Roll failed - don't drop original, but drop extras if any
									if(%hasExtras)
									{
										%weaponDropCount = %eamnt - %originalWeaponCount;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Weapon: " @ %eitem @ " | Percentage: " @ %perc @ "% | Roll: FAILED | Dropping only extras: " @ %weaponDropCount @ " weapons");
										%shouldDropWeapon = true;
										%weaponDropRoll = true;
									}
									else
									{
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Weapon: " @ %eitem @ " | Percentage: " @ %perc @ "% | Roll: FAILED | Not dropping weapon");
										%shouldDropWeapon = false;
										%weaponDropRoll = false;
									}
								}
							}
							else
							{
								// No percentage format - default to 15% drop chance
								%roll = floor(getRandom() * 100) + 1;
								if(%roll <= 15)
								{
									// 15% roll succeeded - drop the weapon
									if(%hasExtras)
										%weaponDropCount = %eamnt;
									else
										%weaponDropCount = %originalWeaponCount;
									%shouldDropWeapon = true;
									%weaponDropRoll = true;
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Weapon: " @ %eitem @ " | No percentage format - defaulting to 15% | Roll: " @ %roll @ " (need <= 15) | SUCCESS | Dropping " @ %weaponDropCount @ " weapons");
								}
								else
								{
									// 15% roll failed - don't drop original, but drop extras if any
									if(%hasExtras)
									{
										%weaponDropCount = %eamnt - %originalWeaponCount;
										%shouldDropWeapon = true;
										%weaponDropRoll = true;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Weapon: " @ %eitem @ " | No percentage format - defaulting to 15% | Roll: " @ %roll @ " (need <= 15) | FAILED | Dropping only extras: " @ %weaponDropCount @ " weapons");
									}
									else
									{
										%shouldDropWeapon = false;
										%weaponDropRoll = false;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Weapon: " @ %eitem @ " | No percentage format - defaulting to 15% | Roll: " @ %roll @ " (need <= 15) | FAILED | Not dropping weapon");
									}
								}
							}
						}
						else
						{
							// Weapon NOT in OriginalLootString - it's from a lootbag, always drop 100%
							%weaponDropCount = %eamnt;
							%shouldDropWeapon = true;
							%weaponDropRoll = true;
							if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Weapon " @ %eitem @ " is from lootbag (not in OriginalLootString). Dropping all " @ %weaponDropCount @ " weapons (100% drop).");
						}
					}
					else
					{
						// For players: always drop weapons (they've already passed drop rate checks when acquired)
						%weaponDropCount = %eamnt;
						%shouldDropWeapon = true;
					}
					
					// Handle ammo drop
					if(%weaponDropCount > 0 && fetchData(%clientId, "LoadedProjectile " @ %eitem) != "")
					{
						%eammo = fetchData(%clientId, "LoadedProjectile " @ %eitem);
						if((%ammoCount = player::getItemCount(%clientId, %eammo)))
						{
							// For AI bots, always drop ammo if weapon is dropping (ignore LCK)
							// For players, use normal LCK logic
							if(%isAI || fetchData(%clientId, "LCK") <= 0)
							{
								%ammoDropCount = %ammoCount;
								%newTmploot = SetStuffString(%tmploot, %eammo, %ammoDropCount);
								// CRITICAL: Only remove from inventory AFTER successfully adding to lootbag
								if(%newTmploot != "False" && %newTmploot != "")
								{
									%tmploot = %newTmploot;
									if(!%isAI)
										Player::setItemCount(%clientId, %eammo, 0);
								}
								else
								{
									echo("WARNING: playerdamage.cs - SetStuffString failed for ammo '" @ %eammo @ "'. NOT removing from inventory.");
								}
							}
							else
								Player::setItemCount(%clientId, %eammo, %ammoCount);
						}				
					}
					
					// Handle weapon drop
					if(%weaponDropCount > 0)
					{
						// For AI bots, always drop weapon if drop chance passed or has extras (ignore LCK)
						// For players, use normal LCK logic
						if(%isAI || fetchData(%clientId, "LCK") <= 0)
						{
							%newTmploot = SetStuffString(%tmploot, %eitem, %weaponDropCount);
							// CRITICAL: Only remove from inventory AFTER successfully adding to lootbag
							if(%newTmploot != "False" && %newTmploot != "")
							{
								%tmploot = %newTmploot;
								if(!%isAI && %player != -1)
									Player::setItemCount(%clientId, %eitem, %eamnt - %weaponDropCount);
							}
							else
							{
								echo("WARNING: playerdamage.cs - SetStuffString failed for weapon '" @ %eitem @ "'. NOT removing from inventory.");
							}
						}
					}

				}
				else
				{
					// No player object - can't get weapon count
				}
			}
		}
		
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == "" || %playerObj == -1 || !isObject(%playerObj))
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "")
				%botName = fetchData(%clientId, "SpawnBotInfo");
			echo("[DEBUG getItemCount] Player::onKilled - Player object doesn't exist before item loop, clientId: " @ %clientId @ ", bot: " @ %botName);
			return; // Player object doesn't exist
		}
		
		%max = getNumItems();
		for(%i = 0; %i < %max; %i++)
		{
			// Re-validate player object in case it gets despawned during the loop
			%playerObj = Client::getOwnedObject(%clientId);
			if(%playerObj == "" || %playerObj == -1 || !isObject(%playerObj))
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				if(%botName == "")
					%botName = fetchData(%clientId, "SpawnBotInfo");
				echo("[DEBUG getItemCount] Player::onKilled - Player object deleted during item loop (first check), clientId: " @ %clientId @ ", bot: " @ %botName);
				break; // Exit loop if player object no longer exists
			}
			
			%a = getItemData(%i);
			
			// CRITICAL: Final validation right before Player::getItemCount call
			%playerObjFinal = Client::getOwnedObject(%clientId);
			if(%playerObjFinal == "" || %playerObjFinal == -1 || !isObject(%playerObjFinal))
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				if(%botName == "")
					%botName = fetchData(%clientId, "SpawnBotInfo");
				echo("[DEBUG getItemCount] Player::onKilled - Player object deleted during item loop, clientId: " @ %clientId @ ", bot: " @ %botName @ ", item: " @ %a);
				break; // Exit loop if player object was deleted between checks
			}
			
			%itemcount = Player::getItemCount(%clientId, %a);

			if(%itemcount)
			{
				// Skip the mounted weapon - it's already handled above
				if(%a == %eitem)
				{
					continue;
				}
				
				%flag = false;
				%lck = fetchData(%clientId, "LCK");
				%isAI = isRPGAI(%clientId);
				%shouldDropItem = true;
				%dropItemCount = %itemcount;

				if(%lck <= 0)
				{
					// When LCK <= 0, drop everything except protected items
					%flag = true;
					
					if($preventDrop[%a] == true)
					{
						%flag = false;
					}
					else if(%a.className == "Equipped")
					{
						%flag = false;
					}
					else if(%a.description == fetchData(%clientId, "eArmor") || %a.description == fetchData(%clientId, "eHelm"))
					{
						%flag = false;
					}
				}
				else
				{
					// When LCK > 0, only drop equipped items and lore items (mounted weapon is handled separately above)
					if(%a.className == "Equipped" || $LoreItem[%a] == True)
					{
						%flag = true;
					}
				}
				
				// Always prevent dropping CastingBlade
				if(%a == CastingBlade)
				{
					%flag = false;
				}

				// For AI bots, check drop rate from OriginalLootString
				if(%flag && %isAI)
				{
					%originalLootString = fetchData(%clientId, "OriginalLootString");
					%itemName = %a;
					if(%itemName.className == "Equipped")
						%itemName = String::getSubStr(%itemName, 0, String::len(%itemName)-1);
					
					%originalCountStr = "";
					%originalCount = 0;
					
					if(%originalLootString != "")
					{
						// Search for this item in the original loot string
						// Skip known non-item keywords: CLASS, LVL, COINS, REMORT, LCK, TOURNYRANK, RankPoints, EXP, AI, CNT, CNTAFFECTS, LVLG, LVLS, LVLE
						// Note: When we skip a keyword, we also need to skip its value, so we increment by 2 in the continue statement
						for(%k = 0; GetWord(%originalLootString, %k) != -1; %k += 2)
						{
							%origItem = GetWord(%originalLootString, %k);
							
							// Skip known non-item keywords AND their values (increment k by 2 total, but loop already does +2, so just continue)
							if(%origItem == "CLASS" || %origItem == "LVL" || %origItem == "COINS" || %origItem == "REMORT" || 
							   %origItem == "LCK" || %origItem == "TOURNYRANK" || %origItem == "RankPoints" || 
							   %origItem == "EXP" || %origItem == "AI" || %origItem == "CNT" || %origItem == "CNTAFFECTS" || 
							   %origItem == "LVLG" || %origItem == "LVLS" || %origItem == "LVLE")
								continue; // Loop already increments by 2, so this correctly skips both keyword and its value
							
							if(%origItem == %itemName)
							{
								%originalCountStr = GetWord(%originalLootString, %k + 1);
								%spos = String::findSubStr(%originalCountStr, "/");
								if(%spos > 0)
								{
									%originalCount = floor(String::getSubStr(%originalCountStr, 0, %spos));
								}
								else
								{
									%originalCount = floor(%originalCountStr);
								}
								break;
							}
						}
					}
					
					// Roll drop rate if item found in OriginalLootString with percentage
					%rollSucceeded = false;
					if(%originalCountStr != "")
					{
						%spos = String::findSubStr(%originalCountStr, "/");
						if(%spos > 0)
						{
							// Has percentage format (e.g., "1/100000")
							%original = String::getSubStr(%originalCountStr, 0, %spos);
							%perc = String::getSubStr(%originalCountStr, %spos+1, 99999);
							
							// Use inline drop rate logic (same as in rpgfunk.cs GiveThisStuff)
							// Extract the logic from rpgfunk.cs inline implementation
							%rangePos = String::findSubStr(%perc, "-");
							%firstChar = String::getSubStr(%perc, 0, 1);
							%rollSucceeded = false;
							
							if(%rangePos > 0 && %firstChar != "-")
							{
								// Variable drop rate range
								%minPerc = String::getSubStr(%perc, 0, %rangePos);
								%maxPerc = String::getSubStr(%perc, %rangePos + 1, 99999);
								%chance = %minPerc + floor(getRandom() * (%maxPerc - %minPerc + 1));
								if(%chance > %maxPerc) %chance = %maxPerc;
								%roll = floor(getRandom() * 100) + 1;
								if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %itemName @ " | Percentage: " @ %minPerc @ "-" @ %maxPerc @ "% (range) | Roll: " @ %roll @ " (need roll <= " @ %chance @ ")");
								if(%roll <= %chance)
								{
									%rollSucceeded = true;
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %itemName @ " will drop (roll " @ %roll @ " <= " @ %chance @ ")");
								}
								else
								{
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %itemName @ " will not drop (roll " @ %roll @ " > " @ %chance @ ")");
								}
							}
							else if(%perc < 0)
							{
								// Negative percentages
								%absPerc = -%perc;
								%roll = floor(getRandom() * 100) + 1;
								%minRoll = 100 - %absPerc + 1;
								if(%absPerc >= 100) %minRoll = 100;
								if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %itemName @ " | Percentage: -" @ %absPerc @ "% (negative) | Roll: " @ %roll @ " (need roll >= " @ %minRoll @ ")");
								if(%roll >= %minRoll)
								{
									%rollSucceeded = true;
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %itemName @ " will drop (roll " @ %roll @ " >= " @ %minRoll @ ")");
								}
								else
								{
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %itemName @ " will not drop (roll " @ %roll @ " < " @ %minRoll @ ")");
								}
							}
							else
							{
								// Single fixed percentage or "1 in X" format
								%percNum = %perc * 1;
								
								if(%percNum >= 1000)
								{
									// "1 in X" format
									%roll = floor(getRandom() * %percNum) + 1;
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %itemName @ " | Format: 1 in " @ %percNum @ " | Roll: " @ %roll @ " (need roll == 1)");
									if(%roll == 1)
									{
										%rollSucceeded = true;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %itemName @ " will drop (roll " @ %roll @ " == 1)");
									}
									else
									{
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %itemName @ " will not drop (roll " @ %roll @ " != 1)");
									}
								}
								else if(%percNum < 1)
								{
									// Decimal percentage
									%roll = floor(getRandom() * 100000) + 1;
									%target = %percNum * 1000;
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %itemName @ " | Percentage: " @ %percNum @ "% (decimal) | Roll: " @ %roll @ " (need roll <= " @ %target @ ")");
									if(%roll <= %target)
									{
										%rollSucceeded = true;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %itemName @ " will drop (roll " @ %roll @ " <= " @ %target @ ")");
									}
									else
									{
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %itemName @ " will not drop (roll " @ %roll @ " > " @ %target @ ")");
									}
								}
								else
								{
									// Normal percentage
									%roll = floor(getRandom() * 100) + 1;
									if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %itemName @ " | Percentage: " @ %percNum @ "% | Roll: " @ %roll @ " (need roll <= " @ %percNum @ ")");
									if(%roll <= %percNum)
									{
										%rollSucceeded = true;
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %itemName @ " will drop (roll " @ %roll @ " <= " @ %percNum @ ")");
									}
									else
									{
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %itemName @ " will not drop (roll " @ %roll @ " > " @ %percNum @ ")");
									}
								}
							}
							if(%rollSucceeded)
							{
								%shouldDropItem = true;
							}
							else
							{
								%shouldDropItem = false;
							}
						}
						else
						{
							// No percentage format - guaranteed drop
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %itemName @ " | No percentage format - guaranteed drop (100%)");
							%rollSucceeded = true;
							%shouldDropItem = true;
						}
					}
					else
					{
						// Item not in OriginalLootString - likely picked up from lootbag, drop 100%
						if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %itemName @ " | NOT found in OriginalLootString - treating as lootbag pickup, drop 100%");
						%rollSucceeded = true;
						%shouldDropItem = true;
					}
					
					// Only drop if roll succeeded
					if(!%shouldDropItem)
						%flag = false;
				}

				if(%flag && %shouldDropItem)
				{
					%b = %a;
					if(%b.className == "Equipped")
						%b = String::getSubStr(%b, 0, String::len(%b)-1);

					%newTmploot = SetStuffString(%tmploot, %b, %dropItemCount);
					if(%newTmploot != "False" && %newTmploot != "")
					{
						%tmploot = %newTmploot;
						// CRITICAL: Only remove from inventory AFTER successfully adding to lootbag
						if(!isRPGAI(%clientId))	
							Player::setItemCount(%clientId, %a, 0);
					}
					else
					{
						// SetStuffString failed - DO NOT remove items from inventory!
						echo("WARNING: playerdamage.cs - SetStuffString returned invalid data ('" @ %newTmploot @ "'). NOT removing item '" @ %a @ "' from player inventory to prevent item loss.");
					}
				}

			}
		}
		
		// Add belt items (quest items, key items, etc.) to loot
		// IMPORTANT: Only equipped belt items should drop, NOT stored items
		// Equipped belt items: All categories from $Belt::Categories array
		// Stored belt items (NOT dropped): StoredQuestItems, StoredKeyItems, BeltStorage, StoredConsumables, StoredArmor, StoredAccessories, StoredOther
		// Build beltCategories from $Belt::Categories array to include all categories
		%beltCategories = "";
		for(%i = 1; $Belt::Categories[%i] != ""; %i++)
		{
			%cat = $Belt::Categories[%i];
			if(%beltCategories != "")
				%beltCategories = %beltCategories @ " " @ %cat;
			else
				%beltCategories = %cat;
		}

		// Get original loot string with percentage chances for AI bots
		%originalLootString = "";
		if(isRPGAI(%clientId))
		{
			%originalLootString = fetchData(%clientId, "OriginalLootString");
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0")
			{
				// Fallback to Client::getName() if BotInfoAiName is not set
				%botName = Client::getName(%clientId);
				if(%botName == "" || %botName == "0") %botName = "Unknown";
			}
			// Log bot's belt contents and OriginalLootString when bot dies
			%questItems = fetchData(%clientId, "QuestItems");
			%keyItems = fetchData(%clientId, "KeyItems");
			%consumables = fetchData(%clientId, "Consumables");
			if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Bot " @ %botName @ " (clientId=" @ %clientId @ ") died. OriginalLootString='" @ %originalLootString @ "'");
			if(%questItems != "" && %questItems != "0") echo("[LOOT DEBUG]   QuestItems: '" @ %questItems @ "'");
			if(%keyItems != "" && %keyItems != "0") echo("[LOOT DEBUG]   KeyItems: '" @ %keyItems @ "'");
			if(%consumables != "" && %consumables != "0") echo("[LOOT DEBUG]   Consumables: '" @ %consumables @ "'");
			%questItems = fetchData(%clientId, "QuestItems");
			//echo("[LOOT DEBUG] Bot " @ %botName @ " QuestItems in belt: '" @ %questItems @ "'");
		}

		for(%catIdx = 0; (%category = getWord(%beltCategories, %catIdx)) != -1; %catIdx++)
		{
			// Only read from equipped belt categories - never from stored items (StoredQuestItems, StoredKeyItems, BeltStorage, StoredConsumables, StoredArmor, StoredAccessories, StoredOther)
			// Safety check: ensure we're only processing equipped categories, not stored ones
			if(%category == "StoredQuestItems" || %category == "StoredKeyItems" || %category == "BeltStorage" || %category == "StoredConsumables" || %category == "StoredArmor" || %category == "StoredAccessories" || %category == "StoredOther")
			{
				//echo("ERROR: Player::onKilled - Attempted to drop stored belt items! Category: " @ %category);
				continue; // Skip stored items - they should never be dropped
			}
			
			%beltList = fetchData(%clientId, %category);
			
			for(%j = 0; (%beltItemName = getWord(%beltList, %j)) != -1; %j += 2)
			{
				%beltItemCount = getWord(%beltList, %j + 1);
				
				// Handle corrupted belt entries like "0 AlienSpine 1" where "0" is invalid but "AlienSpine" is the real item
				// If current item is "0" and the "count" looks like an item name, check if the next word is a valid count
				if(%beltItemName == "0" && %beltItemCount != "" && %beltItemCount != -1 && %beltItemCount != "0")
				{
					%nextWord = getWord(%beltList, %j + 2);
					%nextWordNum = %nextWord * 1;
					// If next word is a valid numeric count, then beltItemCount is actually the item name
					if(%nextWord != "" && %nextWord != -1 && %nextWordNum > 0 && %nextWordNum == %nextWord)
					{
						// This is a corrupted entry: "0 AlienSpine 1" -> treat "AlienSpine" as item, "1" as count
						%beltItemName = %beltItemCount;
						%beltItemCount = %nextWord;
						%j += 1; // Skip the "0" entry
						//echo("[LOOT DEBUG] Player::onKilled(): Fixed corrupted belt entry - treating '" @ %beltItemName @ "' as item with count " @ %beltItemCount);
					}
					else
					{
						// Not a corrupted entry, just skip it
						continue;
					}
				}
				
				// Skip invalid entries (item is empty, or count <= 0)
				if(%beltItemName == "" || %beltItemName == -1 || %beltItemName == "0" || %beltItemCount <= 0)
					continue;
				
				// Get registered item name for proper lookup
				%registeredItemName = %beltItemName;
				if($BeltItem[%beltItemName, "Item"] != "")
					%registeredItemName = $BeltItem[%beltItemName, "Item"];
				
				// CRITICAL SAFETY CHECK: Verify this item is NOT in storage before even processing
				// Check ALL storage locations with BOTH the raw name and registered name
				%beltStorage = fetchData(%clientId, "BeltStorage");
				%storedQuest = fetchData(%clientId, "StoredQuestItems");
				%storedKey = fetchData(%clientId, "StoredKeyItems");
				
				// Check with raw item name
				%storedCount1 = Belt::ItemCount(%beltItemName, %beltStorage);
				%storedQuestCount1 = Belt::ItemCount(%beltItemName, %storedQuest);
				%storedKeyCount1 = Belt::ItemCount(%beltItemName, %storedKey);
				
				// Check with registered item name (if different)
				%storedCount2 = 0;
				%storedQuestCount2 = 0;
				%storedKeyCount2 = 0;
				if(%registeredItemName != %beltItemName)
				{
					%storedCount2 = Belt::ItemCount(%registeredItemName, %beltStorage);
					%storedQuestCount2 = Belt::ItemCount(%registeredItemName, %storedQuest);
					%storedKeyCount2 = Belt::ItemCount(%registeredItemName, %storedKey);
				}
				
				%totalStoredCount = %storedCount1 + %storedQuestCount1 + %storedKeyCount1 + %storedCount2 + %storedQuestCount2 + %storedKeyCount2;
				
				// If item exists in storage, this is just informational - equipped and stored are separate locations
				// All equipped items should drop (they're in your belt/inventory), stored items remain safe in storage
				if(%totalStoredCount > 0)
				{
					%clientName = Client::getName(%clientId);
					//echo("INFO: Player::onKilled - Item '" @ %beltItemName @ "' (registered: '" @ %registeredItemName @ "') exists in BOTH equipped (" @ %category @ ") AND storage. ClientId: " @ %clientId @ " (" @ %clientName @ ")");
					//echo("  Equipped count: " @ %beltItemCount @ " (will drop), Total stored count: " @ %totalStoredCount @ " (protected in storage)");
					//echo("  Stored by raw name: BeltStorage=" @ %storedCount1 @ ", StoredQuestItems=" @ %storedQuestCount1 @ ", StoredKeyItems=" @ %storedKeyCount1);
					//echo("  Stored by registered name: BeltStorage=" @ %storedCount2 @ ", StoredQuestItems=" @ %storedQuestCount2 @ ", StoredKeyItems=" @ %storedKeyCount2);
					//echo("  Dropping all " @ %beltItemCount @ " equipped items - stored items remain safe");
					// Continue processing - drop all equipped items, stored items are separate and protected
				}
				
				if(%beltItemCount > 0)
				{
					// Check drop rate for this item
					%shouldDrop = true;
					%dropCount = %beltItemCount;
					
					if(isRPGAI(%clientId))
					{
						// CRITICAL: Drop chance happens on DEATH (not on spawn)
						// OriginalLootString contains items with percentage format (e.g., "itemname count/percentage")
						// Items in OriginalLootString: roll drop chance on death using the percentage
						// Items NOT in OriginalLootString but in belt: these are from lootbags, always drop 100%
						
						%originalCountStr = "";
						%originalItemName = "";
						%originalCount = 0;
						%hasExtras = false;
						%isInOriginalLootString = false;
						
						if(%originalLootString != "")
						{
							// Search for this item in the original loot string
							// Skip known non-item keywords: CLASS, LVL, COINS, REMORT, LCK, TOURNYRANK, RankPoints, EXP, AI, CNT, CNTAFFECTS, LVLG, LVLS, LVLE
							// Note: When we skip a keyword, we also need to skip its value, so we increment by 2 in the continue statement
							for(%k = 0; GetWord(%originalLootString, %k) != -1; %k += 2)
							{
								%origItem = GetWord(%originalLootString, %k);
								
								// Skip known non-item keywords AND their values (increment k by 2 total, but loop already does +2, so just continue)
								if(%origItem == "CLASS" || %origItem == "LVL" || %origItem == "COINS" || %origItem == "REMORT" || 
								   %origItem == "LCK" || %origItem == "TOURNYRANK" || %origItem == "RankPoints" || 
								   %origItem == "EXP" || %origItem == "AI" || %origItem == "CNT" || %origItem == "CNTAFFECTS" || 
								   %origItem == "LVLG" || %origItem == "LVLS" || %origItem == "LVLE")
									continue; // Loop already increments by 2, so this correctly skips both keyword and its value
								
								// Check both registered name and raw name (case-insensitive for reliability)
								// Also check reverse lookup: if OriginalLootString item has a registered name, check that too
								%origItemRegistered = %origItem;
								if($BeltItem[%origItem, "Item"] != "")
									%origItemRegistered = $BeltItem[%origItem, "Item"];
								
								if(String::ICompare(%origItem, %registeredItemName) == 0 || 
								   String::ICompare(%origItem, %beltItemName) == 0 ||
								   String::ICompare(%origItemRegistered, %registeredItemName) == 0 ||
								   String::ICompare(%origItemRegistered, %beltItemName) == 0)
								{
									%originalCountStr = GetWord(%originalLootString, %k + 1);
									%originalItemName = %origItem;
									%isInOriginalLootString = true;
									
									// OriginalLootString contains percentage format (e.g., "1/15" means 1 item with 15% drop chance)
									// Extract original count and percentage
									%spos = String::findSubStr(%originalCountStr, "/");
									if(%spos > 0)
									{
										%originalCount = floor(String::getSubStr(%originalCountStr, 0, %spos));
									}
									else
									{
										%originalCount = floor(%originalCountStr);
									}
									
									// Check if bot has more than original count (picked up extras from lootbags)
									if(%beltItemCount > %originalCount)
									{
										%hasExtras = true;
										if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " found in OriginalLootString with original count: " @ %originalCount @ ", belt count: " @ %beltItemCount @ " - HAS EXTRAS");
									}
									else
									{
										if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " found in OriginalLootString with original count: " @ %originalCount @ ", belt count: " @ %beltItemCount @ " - NO EXTRAS");
									}
									
									break;
								}
							}
							
							if(!%isInOriginalLootString)
							{
								if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " NOT found in OriginalLootString. OriginalLootString='" @ %originalLootString @ "'. Treating as lootbag pickup (100% drop).");
							}
						}
						else
						{
							// OriginalLootString is empty - all items in belt are from lootbags
							if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] OriginalLootString is empty for bot " @ %clientId @ ". All items in belt are from lootbags (100% drop).");
						}
						
						// Determine drop behavior based on whether item is in OriginalLootString
						if(%isInOriginalLootString)
						{
							// Item is in OriginalLootString - roll drop chance on death using the percentage
							%spos = String::findSubStr(%originalCountStr, "/");
							%rollSucceeded = false;
							
							if(%spos > 0)
							{
								// Has percentage format (e.g., "1/15") - roll it on death
								%perc = String::getSubStr(%originalCountStr, %spos+1, 99999);
								
								// Use inline drop rate logic (same as in rpgfunk.cs GiveThisStuff)
								%rangePos = String::findSubStr(%perc, "-");
								%firstChar = String::getSubStr(%perc, 0, 1);
								
								if(%rangePos > 0 && %firstChar != "-")
								{
									// Variable drop rate range
									%minPerc = String::getSubStr(%perc, 0, %rangePos);
									%maxPerc = String::getSubStr(%perc, %rangePos + 1, 99999);
									%chance = %minPerc + floor(getRandom() * (%maxPerc - %minPerc + 1));
									if(%chance > %maxPerc) %chance = %maxPerc;
									%roll = floor(getRandom() * 100) + 1;
									if(%roll <= %chance)
										%rollSucceeded = true;
									if(%rollSucceeded)
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %perc @ "% (range: " @ %minPerc @ "-" @ %maxPerc @ "%) | Roll: " @ %roll @ " (need roll <= " @ %chance @ ") | SUCCESS");
									else
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %perc @ "% (range: " @ %minPerc @ "-" @ %maxPerc @ "%) | Roll: " @ %roll @ " (need roll <= " @ %chance @ ") | FAILED");
								}
								else if(%perc < 0)
								{
									// Negative percentages
									%absPerc = -%perc;
									%roll = floor(getRandom() * 100) + 1;
									%minRoll = 100 - %absPerc + 1;
									if(%absPerc >= 100) %minRoll = 100;
									if(%roll >= %minRoll)
										%rollSucceeded = true;
									if(%rollSucceeded)
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %perc @ "% (negative) | Roll: " @ %roll @ " (need roll >= " @ %minRoll @ ") | SUCCESS");
									else
										if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %perc @ "% (negative) | Roll: " @ %roll @ " (need roll >= " @ %minRoll @ ") | FAILED");
								}
								else
								{
									// Single fixed percentage or "1 in X" format
									%percNum = %perc * 1;
									
									if(%percNum >= 1000)
									{
										// "1 in X" format
										%roll = floor(getRandom() * %percNum) + 1;
										if(%roll == 1)
											%rollSucceeded = true;
										if(%rollSucceeded)
											if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: 1 in " @ %percNum @ " | Roll: " @ %roll @ " (need roll == 1) | SUCCESS");
										else
											if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: 1 in " @ %percNum @ " | Roll: " @ %roll @ " (need roll == 1) | FAILED");
									}
									else if(%percNum < 1)
									{
										// Decimal percentage
										%roll = floor(getRandom() * 100000) + 1;
										%target = %percNum * 1000;
										if(%roll <= %target)
											%rollSucceeded = true;
										if(%rollSucceeded)
											if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %percNum @ "% (decimal) | Roll: " @ %roll @ " (need roll <= " @ %target @ ") | SUCCESS");
										else
											if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %percNum @ "% (decimal) | Roll: " @ %roll @ " (need roll <= " @ %target @ ") | FAILED");
									}
									else
									{
										// Normal percentage
										%roll = floor(getRandom() * 100) + 1;
										if(%roll <= %percNum)
											%rollSucceeded = true;
										if(%rollSucceeded)
											if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %percNum @ "% | Roll: " @ %roll @ " (need roll <= " @ %percNum @ ") | SUCCESS");
										else
											if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | Percentage: " @ %percNum @ "% | Roll: " @ %roll @ " (need roll <= " @ %percNum @ ") | FAILED");
									}
								}
							}
							else
							{
								// No percentage format - this should NOT happen for quest items/key items
								// Items without percentage format in OriginalLootString are likely configuration errors
								// For safety, treat as 0% drop rate (don't drop) unless explicitly configured with percentage
								%rollSucceeded = false;
								if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %beltItemName @ " | No percentage format in OriginalLootString - treating as 0% drop (configuration error). Item should have format like 'ItemName 1/30' for chance-based drops.");
							}
							
							if(%rollSucceeded)
							{
								// Roll succeeded - drop the item
								// If bot has extras (picked up from lootbags), drop ALL items (original + extras)
								// If no extras, drop only the original count
								if(%hasExtras)
								{
									// Bot has extras - drop ALL items (original passed roll + extras from lootbags always drop)
									%shouldDrop = true;
									%dropCount = %beltItemCount;
									if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " passed drop roll and bot has extras. Dropping all " @ %beltItemCount @ " items (original: " @ %originalCount @ ", extras: " @ (%beltItemCount - %originalCount) @ ")");
								}
								else
								{
									// No extras - drop only the original count (that passed the roll)
									%shouldDrop = true;
									%dropCount = %originalCount;
									if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " passed drop roll. Dropping " @ %dropCount @ " items (original count from spawn).");
								}
							}
							else
							{
								// Roll failed - don't drop original, but drop extras if any
								if(%hasExtras)
								{
									%shouldDrop = true;
									%dropCount = %beltItemCount - %originalCount;
									if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " failed drop roll but bot has extras. Dropping only extras: " @ %dropCount @ " items (belt: " @ %beltItemCount @ ", original: " @ %originalCount @ ")");
								}
								else
								{
									%shouldDrop = false;
									%dropCount = 0;
									if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " failed drop roll. Not dropping item. Belt count: " @ %beltItemCount @ ", original count: " @ %originalCount);
								}
							}
						}
						else
						{
							// Item NOT in OriginalLootString - it's from a lootbag, always drop 100%
							%shouldDrop = true;
							%dropCount = %beltItemCount;
							if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Item " @ %beltItemName @ " is from lootbag (not in OriginalLootString). Dropping all " @ %dropCount @ " items (100% drop).");
						}
					}
					else
					{
						// For players: items in their belt should drop 100% on death
						// (they've already passed drop rate checks when acquired)
						// Only items picked up from lootbags are in equipped belt
						%shouldDrop = true;
						%dropCount = %beltItemCount;
					}
					
					// Drop the items if drop rate check passed
					if(%shouldDrop && %dropCount > 0)
					{
						// Use registered item name for loot
						%lootItemName = %registeredItemName;
						%tmploot = SetStuffString(%tmploot, %lootItemName, %dropCount);
						
						// DEBUG: Log when enemy bots drop quest items/weapons
						if(isRPGAI(%clientId))
						{
							%botName = fetchData(%clientId, "BotInfoAiName");
							if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
						}
						
						// CRITICAL: Remove only the items that were actually dropped, not the entire category
						// This prevents items from being lost when only a portion should drop (e.g., bot has 2, drops 1, should keep 1)
						// Use Belt::TakeThisStuff to remove only the dropped amount
						// This properly consolidates items and preserves any remaining count
						
						// CRITICAL: Auto-unequip accessories/armor before removing on death
						%itemCategory = $BeltItem[%beltItemName, "Type"];
						if(%itemCategory == "Accessories")
						{
							for(%unequipCount = 0; %unequipCount < %dropCount; %unequipCount++)
							{
								if(Belt::IsAccessoryEquipped(%clientId, %beltItemName))
									Belt::UnequipAccessory(%clientId, %beltItemName);
							}
						}
						else if(%itemCategory == "Armor" && fetchData(%clientId, "EquippedBeltArmor") == %beltItemName)
						{
							Belt::UnequipArmor(%clientId, %beltItemName);
						}
						
						Belt::TakeThisStuff(%clientId, %beltItemName, %dropCount);
						
						// DEBUG: Log when enemy bots have items removed after drop
						if(isRPGAI(%clientId))
						{
							%botName = fetchData(%clientId, "BotInfoAiName");
							if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
						}
						//echo("[LOOT DEBUG] Added " @ %dropCount @ " " @ %lootItemName @ " to lootbag");
					}
					else if(!%shouldDrop)
					{
						// Drop rate check failed - for bots, clear the items that didn't drop
						// For players, items should always drop (100%), so this shouldn't happen
						if(isRPGAI(%clientId) && %beltItemCount > 0)
						{
							// Bot had items but drop rate failed - remove them (they weren't added to lootbag)
							// Use Belt::TakeThisStuff to remove all items (dropCount would be 0, so remove the full count)
							
							// CRITICAL: Auto-unequip accessories/armor before removing on death
							%itemCategory = $BeltItem[%beltItemName, "Type"];
							if(%itemCategory == "Accessories")
							{
								for(%unequipCount = 0; %unequipCount < %beltItemCount; %unequipCount++)
								{
									if(Belt::IsAccessoryEquipped(%clientId, %beltItemName))
										Belt::UnequipAccessory(%clientId, %beltItemName);
								}
							}
							else if(%itemCategory == "Armor" && fetchData(%clientId, "EquippedBeltArmor") == %beltItemName)
							{
								Belt::UnequipArmor(%clientId, %beltItemName);
							}
							
							Belt::TakeThisStuff(%clientId, %beltItemName, %beltItemCount);
							
							// DEBUG: Log when enemy bots have items removed due to failed drop rate
							%botName = fetchData(%clientId, "BotInfoAiName");
							if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
						}
						//echo("[LOOT DEBUG] NOT adding " @ %beltItemName @ " to lootbag - drop rate check failed");
					}
				}
			}
		}
		
		// CRITICAL FIX: Save character immediately after clearing equipped items to prevent duplication
		// This ensures the cleared equipped items are saved before the player can pick up their lootbag
		// If SaveCharacter runs after the player picks up their lootbag, it would save the restored items
		// Only save for players, not AI bots
		if(!isRPGAI(%clientId))
		{
			// Save immediately after clearing equipped items (with small delay to ensure clears are processed)
			schedule("SaveCharacter(" @ %clientId @ ");", 0.1, %clientId);
		}

		if(%tmploot != "")
		{
			if(isRPGAI(%clientId))
			{
				// Check if this is a town bot with "NoDropLoot" flag set
				%isTownBot = false;
				for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
				{
					if(%id == %clientId)
					{
						%isTownBot = true;
						break;
					}
				}
				
				// Only drop loot if not a town bot OR if town bot doesn't have "NoDropLoot" flag
				%shouldDropLoot = true;
				if(%isTownBot && fetchData(%clientId, "NoDropLoot") == "true")
				{
					%shouldDropLoot = false;
				}
				
				if(%shouldDropLoot)
				{
					if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] Calling TossLootbag for bot (clientId=" @ %clientId @ ", tmploot='" @ %tmploot @ "')");
					TossLootbag(%clientId, %tmploot, 1, "*", 300);
					if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] TossLootbag returned for bot (clientId=" @ %clientId @ ")");
				}
			}
			else
			{
			%namelist = Client::getName(%clientId) @ ",";
			if(fetchData(%clientId, "LCK") >= 0)
			{
				TossLootbag(%clientId, %tmploot, 5, %namelist, Cap(fetchData(%clientId, "LVL") * 300, 300, 3600));
			}
			else
				TossLootbag(%clientId, %tmploot, 5, %namelist, Cap(fetchData(%clientId, "LVL") * 0.2, 5, "inf"));
			}
		}
	}


	updateSpawnStuff(%clientId);

	//house stuff
	%victimH = fetchData(%clientId, "MyHouse");
	%killerH = "";
	%khn = "";
	if(%killerId > 0)
	{
		%killerH = fetchData(%killerId, "MyHouse");
		%khn = $House::Index[%killerH];
	}
	%vhn = $House::Index[%victimH];
	if(%vhn != "")
	{
		//a house member is killed

		if(fetchData(%clientId, "LCK") < 0)
		{
			//this house member had no LCK at time of death

			if(fetchData(%clientId, "RankPoints") <= 0)
			{
				//no LCK and no Rank Points! you're booted!
				BootFromCurrentHouse(%clientId, true);
			}
			else
			Client::sendMessage(%clientId, $MsgRed, "You have lost all your Rank Points because you died with 0 LCK!");

			storeData(%clientId, "RankPoints", 0);
		}

		//victim loses two rank points
		Client::sendMessage(%clientId, $MsgWhite, "You lost 2 Rank Points.");
		storeData(%clientId, "RankPoints", 2, "dec");

		if(%killerId > 0 && %khn != "")
		{
			if(%khn != %vhn)
			{
				//both contenders are in a house, different from each other
				Client::sendMessage(%killerId, $MsgWhite, "You gained 1 Rank Point!");
				storeData(%killerId, "RankPoints", 1, "inc");
			}
			else
			{
				//both contenders are in the same house, happens if one target-lists the other.
				Client::sendMessage(%killerId, $MsgWhite, "You lost 1 Rank Point.");
				storeData(%killerId, "RankPoints", 1, "dec");
			}
		}
	}
	else if(%vhn == "" && %killerId > 0 && %khn != "")
	{
		//a house member killed a non-house member. no bonuses or punishments
	}
	
	// Save current zone to lastzone BEFORE it gets cleared (for respawn location)
	// If player dies during seal battle in Colloseum, don't set lastzone to Colloseum
	// (Colloseum may not have DropPoints, causing air spawns)
	%currentZone = fetchData(%clientId, "zone");
	if($SealBattleActive && !isRPGAI(%clientId))
	{
		%clZoneDesc = Zone::getDesc(%currentZone);
		if(%clZoneDesc == "Colloseum")
		{
			// Don't set lastzone to Colloseum - let them spawn at default location, then teleport to recall
			// This prevents spawning in the air if Colloseum has no DropPoints
			storeData(%clientId, "lastzone", "");
			// Schedule recall after respawn (respawn happens after $AutoRespawn delay)
			schedule("FellOffMap(" @ %clientId @ ");", $AutoRespawn + 0.5, %clientId);
		}
		else
		{
			storeData(%clientId, "lastzone", %currentZone);
		}
	}
	else
	{
		storeData(%clientId, "lastzone", %currentZone);
	}
	
	if(!isRPGAI(%clientId) && fetchData(%clientId, "LCK") < 0)
		storeData(%clientId, "zone", "");
	
	%zoneType = Zone::getType(fetchData(%clientId, "zone"));
	if(%zoneType == "COLLECT" || %zoneType == "DISPLAY" || %zoneType == "CONTROL")
		storeData(%clientId, "zone", "");
	
	if(fetchData(%clientId, "deathmsg") != "")
	{
		%killerName = "";
		%kitemDesc = "";
		if(%killerId > 0)
		{
			%killerName = Client::getName(%killerId);
			%kitem = Player::getMountedItem(%killerId, $WeaponSlot);
			if(%kitem != -1 && %kitem != "")
			{
				%kitemDesc = %kitem.description;
				if(%kitemDesc == "")
					%kitemDesc = "Unknown";
			}
			else
			{
				%kitemDesc = "Unknown";
			}
		}
		else
		{
			%killerName = "Unknown";
			%kitemDesc = "Unknown";
		}
		%msg = nsprintf(fetchData(%clientId, "deathmsg"), %killerName, Client::getName(%clientId), %kitemDesc);
		remoteSay(%clientId, 0, %msg);
	}

	//========================================================================================================================
	// CRITICAL: Call Client::onKilled() for enemy bots BEFORE clearing BotInfoAiName
	// This ensures EXP distribution has access to the correct bot name
	//========================================================================================================================
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
	{
		// This is an enemy bot - call Client::onKilled() to distribute EXP
		// Check if EXP was already distributed (prevent duplicate calls)
		%expDistributed = fetchData(%clientId, "ExpDistributed");
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		
		if(%expDistributed == "" || %expDistributed == "0" || %expDistributed == -1)
		{
			// EXP not yet distributed - call Client::onKilled() to distribute it
			Client::onKilled(%clientId, %killerId, 0);
			storeData(%clientId, "ExpDistributed", "1");
		}
	}
	else
	{
		// Not an enemy bot - log for debugging
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	}

	if(isRPGAI(%clientId))
	{
		//Check to see if it was a summoned
		if(getWord(fetchData(%clientId, "Summoner"), 0) != -1)
			Summon::Release(%clientId);
	
		//event stuff
		%i = GetEventCommandIndex(%clientId, "onkill");
		if(%i != -1)
		{
			%name = GetWord($EventCommand[%clientId, %i], 0);
			%type = GetWord($EventCommand[%clientId, %i], 1);
			%cl = NEWgetClientByName(%name);
			if(%cl == -1)
			%cl = 2048;

			%cmd = String::NEWgetSubStr($EventCommand[%clientId, %i], String::findSubStr($EventCommand[%clientId, %i], ">")+1, 99999);
			%pcmd = ParseBlockData(%cmd, %clientId, %killerid);
			$EventCommand[%clientId, %i] = "";
			schedule("remoteSay(" @ %cl @ ", 0, \"" @ %pcmd @ "\", \"" @ %name @ "\");", 2);
		}
		ClearEvents(%clientId);
	}

	storeData(%clientId, "noDropLootbagFlag", "");

	storeData(%clientId, "SpellCastStep", "");
	%clientId.sleepMode = "";
	refreshHPREGEN(%clientId);
	refreshMANAREGEN(%clientId);

	Client::setControlObject(%clientId, %clientId);
	storeData(%clientId, "dumbAIflag", "");

	PlaySound(RandomRaceSound(fetchData(%clientId, "RACE"), Death), GameBase::getPosition(%clientId));

	//========================================================================================================================

	%clientId.dead = 1;
	if($AutoRespawn > 0)
	schedule("Game::autoRespawn(" @ %clientId @ ");",$AutoRespawn,%clientId);

	if(fetchData(%clientId, "flashMode"))
		Player::setDamageFlash(%this, 0.75);

	if(%clientId != -1)
	{
		if(%this.vehicle != "")
		{
			if(%this.driver != "")
			{
				%this.driver = "";
				Client::setControlObject(Player::getClient(%this), %this);
				Player::setMountObject(%this, -1, 0);
			}
			else
			{
				%this.vehicle.Seat[%this.vehicleSlot-2] = "";
				%this.vehicleSlot = "";
			}
			%this.vehicle = "";
		}
		schedule("GameBase::startFadeOut(" @ %this @ ");", $CorpseTimeoutValue, %this);
		Client::setOwnedObject(%clientId, -1);
		Client::setControlObject(%clientId, Client::getObserverCamera(%clientId));
		Observer::setOrbitObject(%clientId, %this, 15, 15, 15);
		//========================================================================================================================
		// Enemy Bot Data Cleanup
		//========================================================================================================================
		// CRITICAL: Clear enemy bot data when enemy bots die to prevent stale data from persisting
		// This ensures the next bot that uses this clientId doesn't see the old bot's data
		// NOTE: %spawnBotInfo was already fetched above for EXP distribution check
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// This is an enemy bot - clean up all enemy bot data
			// Get BotInfoAiName BEFORE clearing it (needed for AI::delete)
			%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
			
			// CRITICAL: Use centralized DecrementSpawnCounter() for reliable counter management
			// This function uses the bot registry and multiple fallbacks to find the spawn point
			// Pass %this as excludeObject to prevent UnregisterBot from deleting the dying player object
			DecrementSpawnCounter(%clientId, %this);
			
			// CRITICAL: Decrement bot tracking counters here (Player::onKilled is called first and is more reliable)
			$ActiveEnemyBots--;
			$TotalActiveBots--;
			if($ActiveEnemyBots < 0)
				$ActiveEnemyBots = 0;
			if($TotalActiveBots < 0)
				$TotalActiveBots = 0;
			
			// CRITICAL: Decrement $numAI counter when enemy bot dies
			// This prevents the count from accumulating over time
			if($numAI > 0)
			{
				$numAI--;
				$Telemetry_NumAI_Dec++;  // Track $numAI decrements
			}
			if($numAI < 0)
				$numAI = 0;
			
			Telemetry_RecordDeath();  // Track bot death processed
			
			if($BOT_TRACK_DEBUG) echo("[BOT TRACK] Enemy bot died: " @ %botInfoAiName @ " (clientId=" @ %clientId @ ") | Total Enemy: " @ $ActiveEnemyBots @ " | Total All: " @ $TotalActiveBots @ " | $numAI: " @ $numAI);
			
			// CRITICAL: Free AI number from $aiNumTable so it can be recycled
			// This ensures bot numbers restart from 0-20 instead of going to 100+
			// BotInfoAiName is the full AI name (e.g., "Liquifier20")
			%aiName = %botInfoAiName;
			%numberFreed = false;
			
			// Priority 1: Try using BotInfoAiName (most reliable)
			if(%aiName != "" && %aiName != -1 && %aiName != "0")
			{
				%aiNumber = $tmpbotn[%aiName];
				// CRITICAL: Check if AI number exists and is valid (including 0)
				// setAInumber() always stores a numeric value (never string "0" as sentinel)
				// The old check `%aiNumber != "0"` failed when %aiNumber was the number 0
				// because TorqueScript may treat 0 == "0" as true, causing number 0 to be skipped
				// Solution: If it exists (not empty, not -1), it's a valid number to free (including 0)
				if(%aiNumber != "" && %aiNumber != -1)
				{
					// Valid number (including 0) - free it with cooldown
					$aiNumTable[%aiNumber] = "";
					$AINumberCooldown[%aiNumber] = getSimTime();  // Set cooldown timestamp
					$tmpbotn[%aiName] = "";
					%numberFreed = true;
					Telemetry_RecordAINumberFreed();  // Track AI number freed
					if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN DEBUG] Player::onKilled(): Freed AI number " @ %aiNumber @ " for enemy bot " @ %aiName @ " (via BotInfoAiName) - cooldown set for 3s");
				}
			}
			
			// Priority 2: Fallback - try extracting number from display name if BotInfoAiName failed
			// Display name format: "AdminLiquifier20" -> extract "20" and try to find bot name "Liquifier20"
			if(!%numberFreed)
			{
				%displayName = Client::getName(%clientId);
				if(%displayName != "" && %displayName != -1)
				{
					// Try to extract the number from the end of the display name
					// Pattern: "AdminLiquifier20" -> extract "20", then try "Liquifier20", "AdminLiquifier20", etc.
					%lastChar = String::getSubStr(%displayName, String::len(%displayName) - 1, 1);
					%digitString = "0123456789";
					%isNumeric = (String::findSubStr(%digitString, %lastChar) != -1);
					
					if(%isNumeric)
					{
						// Extract trailing number
						%numStart = String::len(%displayName) - 1;
						%extractedNumber = "";
						for(%i = %numStart; %i >= 0; %i--)
						{
							%char = String::getSubStr(%displayName, %i, 1);
							// Check if character is a digit (0-9 only) - use String::findSubStr to avoid TorqueScript == comparison bug ("E" == "0" evaluates to true)
							if(String::findSubStr(%digitString, %char) != -1)
							{
								%extractedNumber = %char @ %extractedNumber;
							}
							else
							{
								break;
							}
						}
						
						// Try multiple possible bot name formats
						%possibleNames[0] = %displayName; // Full display name
						%possibleNames[1] = String::getSubStr(%displayName, 0, %i + 1) @ %extractedNumber; // Without prefix
						
						// Try each possible name
						for(%j = 0; %j < 2; %j++)
						{
							%tryName = %possibleNames[%j];
							if(%tryName != "" && %tryName != -1)
							{
								%tryNumber = $tmpbotn[%tryName];
								// CRITICAL: Check if AI number exists and is valid (including 0)
								// setAInumber() always stores a numeric value, so if it exists, it's valid (including 0)
								// The old check `%tryNumber != "0"` failed when %tryNumber was the number 0
								if(%tryNumber != "" && %tryNumber != -1)
								{
									// Found it! Free the number (including 0) with cooldown
									$aiNumTable[%tryNumber] = "";
									$AINumberCooldown[%tryNumber] = getSimTime();  // Set cooldown timestamp
									$tmpbotn[%tryName] = "";
									%numberFreed = true;
									Telemetry_RecordAINumberFreed();  // Track AI number freed
									if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN DEBUG] Player::onKilled(): Freed AI number " @ %tryNumber @ " for enemy bot " @ %tryName @ " (via display name fallback) - cooldown set for 3s");
									break;
								}
							}
						}
					}
				}
			}
			
			// Priority 3: Last resort - search all $tmpbotn entries for this client ID's display name
			// This is expensive but ensures we don't leak numbers
			if(!%numberFreed)
			{
				%displayName = Client::getName(%clientId);
				if(%displayName != "" && %displayName != -1)
				{
					// Search through a reasonable range of bot names
					// This is a best-effort cleanup - we can't search all possible names
					// But we can try common patterns
					%commonPrefixes = "Liquifier Obliterator Abolisher Banisher Crucifier Devourer Incarnate MoonBreaker";
					for(%i = 0; (%prefix = GetWord(%commonPrefixes, %i)) != -1; %i++)
					{
						// Check if display name contains this prefix
						if(String::findSubStr(%displayName, %prefix) != -1)
						{
							// Try numbers 0-200 (reasonable range)
							for(%n = 0; %n <= 200; %n++)
							{
								%tryName = %prefix @ %n;
								%tryNumber = $tmpbotn[%tryName];
								// CRITICAL: Check if AI number exists and is valid (including 0)
								// setAInumber() always stores a numeric value, so if it exists, it's valid (including 0)
								// The old check `%tryNumber != "0"` failed when %tryNumber was the number 0
								if(%tryNumber != "" && %tryNumber != -1)
								{
									// Check if this number matches what we'd expect from the display name
									// If display name ends with this number, it's likely a match
									%expectedSuffix = %n;
									if(String::findSubStr(%displayName, %expectedSuffix) != -1)
									{
										// Found it! Free the number (including 0) with cooldown
										$aiNumTable[%tryNumber] = "";
										$AINumberCooldown[%tryNumber] = getSimTime();  // Set cooldown timestamp
										$tmpbotn[%tryName] = "";
										%numberFreed = true;
										Telemetry_RecordAINumberFreed();  // Track AI number freed
										if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN DEBUG] Player::onKilled(): Freed AI number " @ %tryNumber @ " for enemy bot " @ %tryName @ " (via exhaustive search fallback) - cooldown set for 3s");
										break;
									}
								}
							}
							if(%numberFreed)
								break;
						}
					}
				}
			}
			
			if(!%numberFreed)
			{
				echo("WARNING: Player::onKilled() - Could not free AI number for enemy bot (clientId=" @ %clientId @ ", BotInfoAiName='" @ %botInfoAiName @ "', DisplayName='" @ Client::getName(%clientId) @ "'). Number may leak! DEBUG: ");
				// Add debug to show what we tried
				%aiNumberPrimary = getAInumberFromName(%botInfoAiName);
				echo("  DEBUG: botInfoAiName='" @ %botInfoAiName @ "', primaryNumber=" @ %aiNumberPrimary @ ", tmpbotn[aiName]=" @ $tmpbotn[%botInfoAiName] @ ", aiNumTable[primary]=" @ $aiNumTable[%aiNumberPrimary]);
				echo("  DEBUG: displayName='" @ Client::getName(%clientId) @ "', recentlyFreedFlag=" @ $ClientIdRecentlyFreed[%clientId]);
			}
			
			// CRITICAL: Delete AI name from engine registry to free it for reuse
			// This prevents "An AI named X already exists!" errors when respawning
			// NOTE: AI::delete() works for Drones, but may not work for Player objects
			// However, we should still try to clear it to prevent name conflicts
			// NOTE: The engine may delete the player object as part of death processing
			// This is expected behavior - the player object is deleted by the engine after Player::onKilled() is called
			// We check here for debugging purposes, but it's normal for the object to be deleted by this point
			
			// CRITICAL: Mark this client ID as recently freed to prevent immediate reuse
			// This prevents new bots from getting the same client ID before cleanup completes
			// CRITICAL: Set this flag ALWAYS for bots, even if BotInfoAiName is empty (shell bots, corrupted bots, etc.)
			$ClientIdRecentlyFreed[%clientId] = getSimTime();
			if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Marked client ID " @ %clientId @ " as recently freed (bot: " @ %botInfoAiName @ ")");
			
			// Per-spawnpoint post-death cooldown: prevent immediate respawn on this spawnpoint
			%spawnPointId = getBotSpawnPoint(%clientId);
			if(%spawnPointId != "" && %spawnPointId != -1)
			{
				$SpawnPointCooldownUntil[%spawnPointId] = getSimTime() + 5; // 5s cooldown after death
				if($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) echo("[SPAWN COOL] Applied 5s cooldown to SpawnPoint " @ %spawnPointId @ " after bot death (clientId=" @ %clientId @ ")");
			}
			
			// CRITICAL: Must be scheduled with a very short delay to prevent crashes during death processing
			// The engine needs time to finish the death callback chain before we can safely delete the AI name
			if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			{
				// CRITICAL: Clear spawn scheduled flag IMMEDIATELY when bot dies
				// This prevents stale flags from blocking respawns if bot dies during spawn delay
				$SpawnAIScheduled[%botInfoAiName] = "";
				if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Cleared $SpawnAIScheduled flag for bot " @ %botInfoAiName);
				
			// CRITICAL: Escape quotes in bot name to prevent syntax errors
				%escapedName = String::replace(%botInfoAiName, "\"", "\\\"");
				// CRITICAL: Add 1 second delay to deletion to help code load properly
				// Use SafeAIDelete wrapper to verify it's still a bot (prevents ghost shells if player connects during delay)
				schedule("SafeAIDelete(\"" @ %escapedName @ "\", " @ %clientId @ ");", 1.0);
				if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Scheduled SafeAIDelete() for bot " @ %botInfoAiName @ " (clientId=" @ %clientId @ ")");
			}
			
			// CRITICAL FIX: Clear SpawnBotInfo and BotInfoAiName IMMEDIATELY!
			// Previously, these were NOT cleared here with a comment saying AI::onDroneKilled would do it.
			// But AI::onDroneKilled is for DRONE objects, and enemy bots are PLAYER objects!
			// This stale data caused PreSpawnCleanup to delete WRONG bots when client IDs were recycled.
			storeData(%clientId, "SpawnBotInfo", "");
			storeData(%clientId, "BotInfoAiName", "");
			$EnemyBotData[%clientId, "SpawnBotInfo"] = "";
			$EnemyBotData[%clientId, "BotInfoAiName"] = "";
			$ClientData[%clientId, "SpawnBotInfo"] = "";
			$ClientData[%clientId, "BotInfoAiName"] = "";
			$BotInfoAiName[%clientId] = "";
			if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Cleared SpawnBotInfo/BotInfoAiName IMMEDIATELY for clientId " @ %clientId);
			
			storeData(%clientId, "RemortStep", "");
			storeData(%clientId, "QuestItems", "");
			storeData(%clientId, "KeyItems", "");
			storeData(%clientId, "Consumables", "");
			storeData(%clientId, "Armor", "");
			storeData(%clientId, "Accessories", "");
			storeData(%clientId, "Other", "");
			storeData(%clientId, "noExperienceFlag", "");
			storeData(%clientId, "noDropLootbagFlag", "");
			storeData(%clientId, "dumbAIflag", "");
			storeData(%clientId, "frozen", "");
			storeData(%clientId, "noBotSniff", "");
			storeData(%clientId, "SpellCastStep", "");
			storeData(%clientId, "LCKconsequence", "");
			storeData(%clientId, "AIattackMarker", "");
			storeData(%clientId, "ExpDistributed", ""); // Clear EXP distribution flag
			storeData(%clientId, "ShovedByPlayer", ""); // Clear shove flag
			storeData(%clientId, "botAttackMode", ""); // Clear bot attack mode
			storeData(%clientId, "tmpbotdata", ""); // Clear bot targeting data
			storeData(%clientId, "noBotSniff", ""); // Clear bot sniffing flag
			
			// Clear Seal Battle bot flags if this was a Seal Battle bot
			%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
			if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
			{
				storeData(%clientId, "SealBattleBot", "");
				storeData(%clientId, "AImoveChance", ""); // Clear per-bot AImoveChance
				storeData(%clientId, "AImaxRangeOverride", ""); // Clear detection range override
			}
			
			// Clear Colloseum bot flags if this was a Colloseum bot
			%isColloseumBot = fetchData(%clientId, "ColloseumBot");
			if(%isColloseumBot == "true")
			{
				storeData(%clientId, "ColloseumBot", "");
				storeData(%clientId, "AImoveChance", ""); // Clear per-bot AImoveChance
				storeData(%clientId, "AImaxRangeOverride", ""); // Clear detection range override
			}
			// CRITICAL: Clear all seal battle specific data to prevent transfer to new bots/players
			storeData(%clientId, "SealBattleScaledRound", ""); // Clear scaled round marker
			storeData(%clientId, "SealBattleOriginalLVL", ""); // Clear original LVL storage
			storeData(%clientId, "SealBattleOriginalRemortStep", ""); // Clear original RemortStep storage
			storeData(%clientId, "SealBattleOriginalEndurance", ""); // Clear original Endurance storage
			storeData(%clientId, "SealBattleOriginalEnergy", ""); // Clear original Energy storage
			storeData(%clientId, "SealBattleOriginalWeightCapacity", ""); // Clear original WeightCapacity storage
			storeData(%clientId, "AImoveChance", ""); // Clear AI move chance (set for seal battle bots)
			storeData(%clientId, "RACE", ""); // Clear race (seal battle bots set to "Seals")
			storeData(%clientId, "DeathProcessed", ""); // Clear death processed flag (in case AI::onDroneKilled doesn't run)
			storeData(%clientId, "HasLoadedAndSpawned", ""); // Clear spawn flag
			storeData(%clientId, "OriginalLootString", ""); // Clear original loot string (set in AI::setWeapons)
			
			// CRITICAL: Clear seal battle scaled stats from all data arrays
			// These scaled stats could transfer to new bots/players if not cleared
			storeData(%clientId, "DEF", "");
			storeData(%clientId, "MDEF", "");
			storeData(%clientId, "ATK", "");
			storeData(%clientId, "DMG", "");
			$EnemyBotData[%clientId, "DEF"] = "";
			$EnemyBotData[%clientId, "MDEF"] = "";
			$EnemyBotData[%clientId, "ATK"] = "";
			$EnemyBotData[%clientId, "DMG"] = "";
			$ClientData[%clientId, "DEF"] = "";
			$ClientData[%clientId, "MDEF"] = "";
			$ClientData[%clientId, "ATK"] = "";
			$ClientData[%clientId, "DMG"] = "";
			
			// CRITICAL: Clear spawn invulnerability flags (set for seal battle bots)
			storeData(%clientId, "SpawnInvuln", "");
			$EnemyBotData[%clientId, "SpawnInvuln"] = "";
			$ClientData[%clientId, "SpawnInvuln"] = "";
			%displayName = Client::getName(%clientId);
			if(%displayName != "" && %displayName != -1)
				$SpawnInvulnByName[%displayName] = "";
			
			// CRITICAL: Clear bot frozen flags (set for seal battle bots)
			$BotFrozen[%clientId] = "";
			$EnemyBotData[%clientId, "frozen"] = "";
			
			// CRITICAL: Clear seal battle scaled stats from global array (prevents transfer to new bots/players)
			// This global array stores MaxHP, MaxMANA, DEF, MDEF, ATK, DMG, LCK, and round
			$SealBattleScaledStats[%clientId, "DEF"] = "";
			$SealBattleScaledStats[%clientId, "MDEF"] = "";
			$SealBattleScaledStats[%clientId, "ATK"] = "";
			$SealBattleScaledStats[%clientId, "DMG"] = "";
			$SealBattleScaledStats[%clientId, "MaxHP"] = "";
			$SealBattleScaledStats[%clientId, "MaxMANA"] = "";
			$SealBattleScaledStats[%clientId, "LCK"] = "";
			$SealBattleScaledStats[%clientId, "round"] = "";
			
			// CRITICAL: Clear all LoadedProjectile entries (could be multiple weapons)
			// These are set in AI::setWeapons for weapons with ammo
			// We need to clear them all, but we don't know which weapons were equipped
			// So we'll clear common weapon names that might have been equipped
			// NOTE: This is a best-effort cleanup - some entries might persist if weapon names don't match
			// But this is better than leaving all of them
			%commonWeapons = "Crossbow Bow Rifle Pistol Shotgun";
			for(%i = 0; (%weapon = GetWord(%commonWeapons, %i)) != -1; %i++)
			{
				storeData(%clientId, "LoadedProjectile " @ %weapon, "");
			}
			
			// CRITICAL: Clear all EventCommand entries (0-99) to prevent stale event commands
			// EventCommand entries are set during bot lifetime for various events
			for(%i = 0; %i <= 99; %i++)
			{
				$EventCommand[%clientId, %i] = "";
			}
			
			// CRITICAL: Clear retry counters and spawn flags (set during spawn process)
			if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
			{
				$EnemyBotClientIdRetry[%botInfoAiName] = "";
				$EnemyBotSpawnRetry[%botInfoAiName] = "";
				$Directive99RemovalAttempted[%botInfoAiName] = "";
			}
			
			// CRITICAL: DO NOT clear SpawnBotInfo, SpawnTime, or BotInfoAiName from arrays here
			// AI::onDroneKilled needs to read these from arrays as fallback if storeData is cleared
			// These will be cleared by AI::onDroneKilled after it processes the bot death
			
			// CRITICAL: Clear all directive table entries for this bot (by client ID)
			// Clear common directives (0-99) to prevent stale directive data
			for(%d = 0; %d <= 99; %d++)
			{
				$aidirectiveTable[%clientId, %d] = "";
			}
			
				// CRITICAL: Clear all directive table entries for this bot (by client ID)
				// Clear common directives (0-99) to prevent stale directive data
				for(%d = 0; %d <= 99; %d++)
				{
					$aidirectiveTable[%clientId, %d] = "";
				}
				
				// CRITICAL: Clear belt cached lists to prevent memory leaks and graphical glitches
				$Belt::CachedList[%clientId, "QuestItems"] = "";
				$Belt::CachedList[%clientId, "KeyItems"] = "";
				$Belt::CachedList[%clientId, "Consumables"] = "";
				$Belt::CachedList[%clientId, "Armor"] = "";
				$Belt::CachedList[%clientId, "Accessories"] = "";
				$Belt::CachedList[%clientId, "Other"] = "";
				
				// CRITICAL: Clear any AI movement/behavior data that might cause issues
			storeData(%clientId, "AITarget", "");
			storeData(%clientId, "AILastDestination", "");
			storeData(%clientId, "AILastLoggedDist", "");
			storeData(%clientId, "AIMovementLoopRunning", "");
			storeData(%clientId, "BotAttackLoopActive", ""); // Clear attack loop flag (set for seal battle bots)
			storeData(%clientId, "botTeam", "");
			
			// CRITICAL: Clear name-based spawn invulnerability flags for seal battle bots
			// These are set using display names like "SealFighter1", "SealMage2", etc.
			%displayName = Client::getName(%clientId);
			if(%displayName != "" && %displayName != -1)
			{
				// Check if this is a seal battle bot name pattern
				if(String::findSubStr(%displayName, "SealFighter") == 0 || 
				   String::findSubStr(%displayName, "SealMage") == 0 || 
				   String::findSubStr(%displayName, "SealGuardian") == 0)
				{
					$SpawnInvulnByName[%displayName] = "";
				}
			}
			
			// CRITICAL: Clear additional flags from arrays to prevent stale data
			$EnemyBotData[%clientId, "ShovedByPlayer"] = "";
			$EnemyBotData[%clientId, "botAttackMode"] = "";
			$EnemyBotData[%clientId, "tmpbotdata"] = "";
			$EnemyBotData[%clientId, "SealBattleBot"] = "";
			$EnemyBotData[%clientId, "DeathProcessed"] = "";
			$EnemyBotData[%clientId, "HasLoadedAndSpawned"] = "";
			$EnemyBotData[%clientId, "AImoveChance"] = "";
			$EnemyBotData[%clientId, "RACE"] = "";
			$ClientData[%clientId, "ShovedByPlayer"] = "";
			$ClientData[%clientId, "botAttackMode"] = "";
			$ClientData[%clientId, "tmpbotdata"] = "";
			$ClientData[%clientId, "SealBattleBot"] = "";
			$ClientData[%clientId, "DeathProcessed"] = "";
			$ClientData[%clientId, "HasLoadedAndSpawned"] = "";
			$ClientData[%clientId, "AImoveChance"] = "";
			$ClientData[%clientId, "RACE"] = "";
			
			// CRITICAL: Clean up pet and bot group data (same as AI::onDroneKilled does for Drones)
			// This prevents stale references that could cause graphical glitches
			$PetList = RemoveFromCommaList($PetList, %clientId);
			%petowner = fetchData(%clientId, "petowner");
			if(%petowner != "" && %petowner != -1 && %petowner != "0")
			{
				storeData(%petowner, "PersonalPetList", RemoveFromCommaList(fetchData(%petowner, "PersonalPetList"), %clientId));
			}
			storeData(%clientId, "petowner", "");
			
			// Clean up bot group if applicable
			%b = AI::IsInWhichBotGroup(%clientId);
			if(%b != -1)
				AI::RemoveBotFromBotGroup(%clientId, %b);
			
			// CRITICAL: Clean up BotInfoAiName for bots without SpawnBotInfo (seal battle bots, etc.)
			// These bots are Player objects, not Drones, so AI::onDroneKilled() is NOT called for them
			// Therefore, we must clean up BotInfoAiName here to prevent stale data
			if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
			{
				// This is a bot without SpawnBotInfo (seal battle bot or other non-spawn-point bot)
				// Clear BotInfoAiName since AI::onDroneKilled() won't be called
				if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
				{
					$BotInfoAiName[%clientId] = "";
					storeData(%clientId, "BotInfoAiName", "");
					$EnemyBotData[%clientId, "BotInfoAiName"] = "";
					$ClientData[%clientId, "BotInfoAiName"] = "";
					if($BOT_SHELL_DEBUG) echo("[BOT SHELL DEBUG] Player::onKilled(): Cleared BotInfoAiName for non-spawn-point bot " @ %botInfoAiName @ " (clientId=" @ %clientId @ ")");
				}
				
				// CRITICAL: Clean up scaled weapon damage arrays for seal battle bots
				%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
				if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
				{
					%weapon = Player::getMountedItem(%clientId, $WeaponSlot);
					if(%weapon != -1 && %weapon != "")
					{
						%weaponName = getCroppedItem(%weapon);
						// Clear the scaled damage for this bot's weapon
						$SealBattleWeaponDamage[%clientId, %weaponName] = "";
						// Clear the reverse lookup if this bot was the owner
						if($SealBattleWeaponOwner[%weaponName] == %clientId)
							$SealBattleWeaponOwner[%weaponName] = "";
						echo("[SEAL BATTLE] Player::onKilled(): Cleared scaled weapon damage for Seal Battle Bot " @ %botInfoAiName @ " (weapon: " @ %weaponName @ ")");
					}
				}
			}
		}
		else
		{
			// SpawnBotInfo is missing - try fallback: check if this is an enemy bot via BotInfoAiName
			%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
			if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			{
				// Check if this looks like an enemy bot (not a town bot)
				// Town bots typically have names like "merchant1", "banker", etc.
				// Enemy bots have names like "Devourer6", "Incarnate3", etc.
				%displayName = Client::getName(%clientId);
				%isLikelyEnemyBot = false;
				
				// Check if display name matches enemy bot patterns (Pigman*, Demon*, Alien*, etc.)
				if(String::findSubStr(%displayName, "Pigman") != -1 || 
				   String::findSubStr(%displayName, "Demon") != -1 || 
				   String::findSubStr(%displayName, "Alien") != -1 ||
				   String::findSubStr(%displayName, "God") != -1 ||
				   String::findSubStr(%displayName, "Admin") != -1 ||
				   String::findSubStr(%displayName, "Zombie") != -1)
				{
					%isLikelyEnemyBot = true;
				}
				
				if(%isLikelyEnemyBot)
				{
					// This is likely an enemy bot but SpawnBotInfo is missing
					// Use centralized DecrementSpawnCounter() which has multiple fallback methods
					echo("WARNING: Player::onKilled - Enemy bot " @ %botInfoAiName @ " (clientId=" @ %clientId @ ") died but SpawnBotInfo is missing! Using DecrementSpawnCounter() fallbacks...");
					DecrementSpawnCounter(%clientId);
					
					// CRITICAL FIX: Also decrement $numAI and record death telemetry!
					// This was missing, causing $numAI leaks for bots that die before SpawnAIGetClientId runs
					if($numAI > 0)
					{
						$numAI--;
						$Telemetry_NumAI_Dec++;
					}
					if($numAI < 0)
						$numAI = 0;
					
					Telemetry_RecordDeath();  // Track bot death processed
					
					if($BOT_TRACK_DEBUG) echo("[BOT TRACK] Enemy bot died (fallback path): " @ %botInfoAiName @ " (clientId=" @ %clientId @ ") | $numAI: " @ $numAI);
				}
			}
			else
			{
				// CRITICAL FIX: Check if this is an AI-controlled bot without ANY bot markers
				// This catches bots that die VERY early (before BotInfoAiName is set)
				if(Player::isAiControlled(%clientId))
				{
					%displayName = Client::getName(%clientId);
					// Only process if it looks like an enemy bot name pattern
					if(String::findSubStr(%displayName, "Pigman") != -1 || 
					   String::findSubStr(%displayName, "Demon") != -1 || 
					   String::findSubStr(%displayName, "Alien") != -1 ||
					   String::findSubStr(%displayName, "God") != -1 ||
					   String::findSubStr(%displayName, "Admin") != -1 ||
					   String::findSubStr(%displayName, "Ogre") != -1 ||
					   String::findSubStr(%displayName, "Orc") != -1 ||
					   String::findSubStr(%displayName, "Undead") != -1 ||
					   String::findSubStr(%displayName, "Minotaur") != -1 ||
					   String::findSubStr(%displayName, "Zombie") != -1)
					{
						echo("WARNING: Player::onKilled - AI-controlled bot died without any bot markers! DisplayName=" @ %displayName @ ", clientId=" @ %clientId);
						
						// Decrement $numAI since AI::helper incremented it
						if($numAI > 0)
						{
							$numAI--;
							$Telemetry_NumAI_Dec++;
						}
						if($numAI < 0)
							$numAI = 0;
						
						Telemetry_RecordDeath();
						
						if($BOT_TRACK_DEBUG) echo("[BOT TRACK] Unmarked enemy bot died: " @ %displayName @ " (clientId=" @ %clientId @ ") | $numAI: " @ $numAI);
					}
				}
			}
		}
		//========================================================================================================================
		// Town Bot Data Cleanup
		//========================================================================================================================
		if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
		{
			// Check if this is actually a town bot (BotInfoAiName must start with "TownBot_")
			// Enemy bots can also have BotInfoAiName set, but it won't start with "TownBot_"
			%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
			%isTownBot = false;
			
			if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			{
				// CRITICAL: Only treat as town bot if BotInfoAiName starts with "TownBot_"
				// Enemy bots have BotInfoAiName set to their bot name (e.g., "MoonBreaker2"), not "TownBot_*"
				if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
				{
					%isTownBot = true;
				}
			}
			
			if(%isTownBot)
			{
				// This is a town bot - clean up town bot data
				// CRITICAL: Decrement bot tracking counters here (Player::onKilled is called first and is more reliable)
				$ActiveTownBots--;
				$TotalActiveBots--;
				if($ActiveTownBots < 0)
					$ActiveTownBots = 0;
				if($TotalActiveBots < 0)
					$TotalActiveBots = 0;
				if($BOT_TRACK_DEBUG) echo("[BOT TRACK] Town bot died: " @ %botInfoAiName @ " (clientId=" @ %clientId @ ") | Total Town: " @ $ActiveTownBots @ " | Total All: " @ $TotalActiveBots);
				
				// CRITICAL: Extract bot name from BotInfoAiName (format: "TownBot_merchant1" -> "merchant1")
				// FALLBACK: If BotInfoAiName is empty, search $TownBotSpawned by clientId
				%botName = "";
				if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
				{
					%botName = %botInfoAiName;
					if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
					{
						%botName = String::getSubStr(%botInfoAiName, 8, 999);  // Remove "TownBot_" prefix (8 characters)
					}
				}
				
				// FALLBACK: If bot name is still empty, search $TownBotSpawned by clientId
				if(%botName == "" || %botName == "0" || %botName == -1)
				{
					for(%i = 0; (%regBotName = GetWord($TownBotRegistry, %i)) != -1; %i++)
					{
						if($TownBotSpawned[%regBotName] == %clientId)
						{
							%botName = %regBotName;
							echo("WARNING: Player::onKilled - Found bot name '" @ %botName @ "' by searching $TownBotSpawned for clientId " @ %clientId @ " (BotInfoAiName was empty)");
							break;
						}
					}
				}
				
				// Remove from town bot list
				$TownBotList = RemoveFromCommaList($TownBotList, %clientId);
				
				// CRITICAL: Clear spawn tracking - use fallback search if bot name is still unknown
				if(%botName != "" && %botName != -1 && %botName != "0")
				{
					$TownBotSpawned[%botName] = "";
					if($BOT_CLEANUP_DEBUG) echo("[TOWN BOT CLEANUP] Player::onKilled - Cleared $TownBotSpawned[" @ %botName @ "] for clientId " @ %clientId);
				}
				else
				{
					// Bot name is unknown - search and clear by clientId (safety fallback)
					echo("WARNING: Player::onKilled - Bot name is empty for town bot (clientId=" @ %clientId @ "). Searching $TownBotSpawned by clientId...");
					for(%i = 0; (%regBotName = GetWord($TownBotRegistry, %i)) != -1; %i++)
					{
						if($TownBotSpawned[%regBotName] == %clientId)
						{
							$TownBotSpawned[%regBotName] = "";
							if($BOT_CLEANUP_DEBUG) echo("[TOWN BOT CLEANUP] Player::onKilled - Cleared $TownBotSpawned[" @ %regBotName @ "] by clientId search (clientId=" @ %clientId @ ")");
							break;
						}
					}
				}
				
				// NEW: Clear O(1) lookup table and set cooldown
				UnregisterTownBotClient(%clientId);
				
				// CRITICAL: DO NOT clear BotInfoAiName, SpawnBotInfo, or SpawnTime here
				// AI::onDroneKilled needs to read these to identify bot type and perform cleanup
				// These will be cleared by AI::onDroneKilled after it processes the bot death
				// Only clear data that AI::onDroneKilled doesn't need
				storeData(%clientId, "QuestItems", "");
				storeData(%clientId, "KeyItems", "");
				storeData(%clientId, "Consumables", "");
				storeData(%clientId, "Armor", "");
				storeData(%clientId, "Accessories", "");
				storeData(%clientId, "Other", "");
				storeData(%clientId, "zone", "");
				storeData(%clientId, "tmpzone", "");
				storeData(%clientId, "botTeam", "");
				storeData(%clientId, "AITarget", "");
				storeData(%clientId, "AILastDestination", "");
				storeData(%clientId, "AILastLoggedDist", "");
				storeData(%clientId, "AIMovementLoopRunning", "");
				
				// CRITICAL: DO NOT clear BotInfoAiName, SpawnBotInfo, or SpawnTime from arrays here
				// AI::onDroneKilled needs to read these from arrays as fallback if storeData is cleared
				// These will be cleared by AI::onDroneKilled after it processes the bot death
				$TownBotData[%clientId, "QuestItems"] = "";
				$TownBotData[%clientId, "KeyItems"] = "";
				$TownBotData[%clientId, "Consumables"] = "";
				$TownBotData[%clientId, "Armor"] = "";
				$TownBotData[%clientId, "Accessories"] = "";
				$TownBotData[%clientId, "Other"] = "";
				
				// CRITICAL: Clear from $ClientData for backwards compatibility
				$ClientData[%clientId, "BotInfoAiName"] = "";
				$ClientData[%clientId, "SpawnBotInfo"] = "";
				$ClientData[%clientId, "SpawnTime"] = "";
				
				// CRITICAL: Clear belt cached lists to prevent memory leaks
				$Belt::CachedList[%clientId, "QuestItems"] = "";
				$Belt::CachedList[%clientId, "KeyItems"] = "";
				$Belt::CachedList[%clientId, "Consumables"] = "";
				$Belt::CachedList[%clientId, "Armor"] = "";
				$Belt::CachedList[%clientId, "Accessories"] = "";
				$Belt::CachedList[%clientId, "Other"] = "";
				
				// CRITICAL: Clear bot group if applicable
				%b = AI::IsInWhichBotGroup(%clientId);
				if(%b != -1)
					AI::RemoveBotFromBotGroup(%clientId, %b);
				
				// CRITICAL: Free AI number from $aiNumTable so it can be recycled
				// This ensures bot numbers restart from 0-20 instead of going to 100+
				// BotInfoAiName is the full AI name (e.g., "TownBot_merchant1")
				%aiName = %botInfoAiName;
				if(%aiName != "" && %aiName != -1 && %aiName != "0")
				{
					// Get the number BEFORE clearing $tmpbotn to ensure we can free it
					%townBotNumber = $tmpbotn[%aiName];
					if(%townBotNumber != "" && %townBotNumber != -1 && %townBotNumber != "0")
					{
						$aiNumTable[%townBotNumber] = "";
						$tmpbotn[%aiName] = "";
						//echo("[SPAWN DEBUG] Player::onKilled(): Freed AI number " @ %townBotNumber @ " for town bot " @ %aiName @ " - number can now be recycled");
					}
				}
			}
			else if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
			{
				// This is an enemy bot that had SpawnBotInfo cleared but still has BotInfoAiName
				// This shouldn't happen, but handle it gracefully
				echo("WARNING: Player::onKilled - Bot " @ %botInfoAiName @ " (clientId=" @ %clientId @ ") has BotInfoAiName but no SpawnBotInfo. Treating as enemy bot.");
				// Decrement enemy bot counters (already decremented above, but ensure they're correct)
				// Don't double-decrement, just log the warning
			}
		}
		
		schedule("deleteObject(" @ %this @ ");", $CorpseTimeoutValue + 2.5, %this);
		%clientId.observerMode = "dead";
		%clientId.dieTime = getSimTime();
		
		// Save character and world after death to prevent lootbag duplication on server crash
		// NOTE: SaveCharacter is already called immediately after clearing equipped items (see above)
		// This is just a backup save in case the first one didn't complete
		// Only save for players, not AI bots
		if(!isRPGAI(%clientId))
		{
			// Backup character save after 2 second delay (in case first save didn't complete)
			schedule("SaveCharacter(" @ %clientId @ ");", 2, %clientId);
			
			// Save world after 3 second delay (allows character saves to complete first)
			schedule("SaveWorld();", 3);
		}
	}
}

function Player::onDamage(%this,%type,%value,%pos,%vec,%mom,%vertPos,%rweapon,%object,%weapon,%preCalcMiss)
{
	dbecho($dbechoMode, "Player::onDamage(" @ %this @ ", " @ %type @ ", " @ %value @ ", " @ %pos @ ", " @ %vec @ ", " @ %mom @ ", " @ %vertPos @ ", " @ %rweapon @ ", " @ %object @ ", " @ %weapon @ ", " @ %preCalcMiss @ ")");
	if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Player::onDamage ENTRY: Victim=" @ %this @ ", ShooterObj=" @ %object @ ", Damage=" @ %value @ ", Type=" @ %type @ ", PreCalcMiss=" @ %preCalcMiss);

	%skilltype = $SkillType[%weapon];

	if(%object != -1 && %type != $NullDamageType && !Player::IsDead(%this))
	{	
		// Use helper function to get client ID from Player object
		%damagedClient = GetClientIdFromPlayerObject(%this);
		
		// Fallback: if helper function returns -1, use Player object as client ID (for compatibility)
		if(%damagedClient == -1 || %damagedClient == "")
		{
			%damagedClient = %this;
		}
		
		// PHASE 4 FIX: Validate client ID belongs to correct entity before processing damage
		if(%damagedClient != -1 && %damagedClient != "" && isObject(%this))
		{
			// Reverse verification - ensure the client ID actually owns this Player object
			if(%damagedClient != %this)
			{
				// Only verify if we got a different client ID (not using %this as fallback)
				%verifyPlayerObj = Client::getOwnedObject(%damagedClient);
				if(%verifyPlayerObj != %this && %verifyPlayerObj != -1 && %verifyPlayerObj != "")
				{
					// Client ID doesn't own this Player object - this is a collision!
					echo("ERROR: Player::onDamage - Client ID " @ %damagedClient @ " does not own Player object " @ %this @ ". Player object belongs to client ID with Player object " @ %verifyPlayerObj @ ". Rejecting damage to prevent routing to wrong entity.");
					return; // Reject damage to prevent routing to wrong entity
				}
				
				// Entity type validation - verify client ID matches expected entity type
				// PRIORITY: Check bot indicators FIRST (bots can have HasLoadedAndSpawned set, so don't use it for player detection)
				%spawnBotInfo = fetchData(%damagedClient, "SpawnBotInfo");
				%botInfoAiName = fetchData(%damagedClient, "BotInfoAiName");
				%isBot = ((%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1) || 
				          (%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1) ||
				          Player::isAiControlled(%damagedClient) || isRPGAI(%damagedClient));
				
				// Only check for player indicators if NOT a bot
				%isPlayer = false;
				if(!%isBot)
				{
					%playerName = Client::getName(%damagedClient);
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
				
				// If entity type indicators conflict, reject damage to prevent routing to wrong entity
				// This should rarely trigger now since we prioritize bot detection
				if(%isPlayer && %isBot)
				{
					echo("ERROR: Player::onDamage - Client ID " @ %damagedClient @ " has conflicting entity type indicators (both player and bot). Rejecting damage to prevent collision.");
					return; // Reject damage
				}
			}
		}
		
		// Early-initialization guard: if AI bot not fully initialized or spawn-invuln is on, ignore all damage
		if(Player::isAiControlled(%damagedClient))
		{
			%hasLoaded = fetchData(%damagedClient, "HasLoadedAndSpawned");
			%spawnInvuln = fetchData(%damagedClient, "SpawnInvuln");
			// Additional name-based guard for bots that haven't registered yet
			%nameGuard = "";
			%damagedName = Client::getName(%damagedClient);
			if(%damagedName != "" && %damagedName != -1)
			{
				%nameGuardTime = $SpawnInvulnByName[%damagedName];
				if(%nameGuardTime != "" && %nameGuardTime != -1)
				{
					if((getSimTime() - %nameGuardTime) < 15)
						%nameGuard = "true";
				}
			}

			if(!%hasLoaded || %spawnInvuln || %nameGuard == "true")
			{
				// Hard ignore: exit immediately to prevent any processing, messages, or side effects
				if($DamageDebugEnabled) echo("[DAMAGE DEBUG] AI Early Exit: Invuln/Loading/NameGuard. Loaded=" @ %hasLoaded @ " Invuln=" @ %spawnInvuln @ " NameGuard=" @ %nameGuard);
				return;
			}
		}
		else
		{
			// HUMAN PLAYER spawn protection: 5 seconds of invincibility after connecting
			// Prevents damage from bots attacking the previous clientId occupant
			%spawnInvuln = fetchData(%damagedClient, "SpawnInvuln");
			if(%spawnInvuln)
			{
				if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Player Early Exit: Spawn protection active");
				return;
			}
			// Also check name-based guard
			%damagedName = Client::getName(%damagedClient);
			if(%damagedName != "" && %damagedName != -1)
			{
				%nameGuardTime = $SpawnInvulnByName[%damagedName];
				if(%nameGuardTime != "" && %nameGuardTime != -1)
				{
					if((getSimTime() - %nameGuardTime) < 5)
					{
						if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Player Early Exit: Name guard active");
						return;
					}
				}
			}
		}

		// Safe zone guard: if damagedClient is in a safe zone and not in an arena, ignore damage
		%safeZone = fetchData(%damagedClient, "safeZone");
		%arena = fetchData(%damagedClient, "arena");
		%safeZoneGuard = fetchData(%damagedClient, "safeZoneGuard"); // Additional guard for safe zone entry/exit
		if((%safeZone && !%arena) || %safeZoneGuard)
		{
			if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Exit: Safe Zone. SafeZone=" @ %safeZone @ " Arena=" @ %arena @ " SafeGuard=" @ %safeZoneGuard);
			return;
		}
		
		// %object is the shooter's client ID (for players) or Player object (for AI shooters)
		// For spell damage, %object might be a client ID, so try to get the Player object
		%shooterClient = %object;
		if(isObject(%object))
		{
			%objectType = getObjectType(%object);
			if(%objectType == "Player")
			{
				// %object is a Player object - use helper function to get client ID
				%shooterClientId = GetClientIdFromPlayerObject(%object);
				if(%shooterClientId != -1 && %shooterClientId != "")
				{
					%shooterClient = %shooterClientId; // Found client ID (works for both players and bots)
				}
				else
				{
					// Fallback: use Player object as client ID (for compatibility with old code)
					// This should rarely happen, but keeps the code working if client ID lookup fails
					%shooterClient = %object;
				}
			}
		}
		if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Shooter Resolved: " @ %shooterClient @ " (Original: " @ %object @ ")");

		// PHASE 5 FIX: Check if the shooter's client ID was recently freed
		// This blocks "ghost damage" from projectiles/spells of dead bots whose IDs were immediately reused
		// CRITICAL: Window must be 30 seconds to match flag clearing duration and cover long-cast spells
		%shooterRecentlyFreed = $ClientIdRecentlyFreed[%shooterClient];
		if(%shooterRecentlyFreed != "" && %shooterRecentlyFreed != "0" && %shooterRecentlyFreed != -1)
		{
			// Safety window of 30 seconds - matches flag clearing and covers longest spell cast times
			if((getSimTime() - %shooterRecentlyFreed) < 30)
			{
				if($DamageDebugEnabled) echo("[DAMAGE FIX] BLOCKED ghost damage from recently freed Client ID " @ %shooterClient @ ". Timestamp: " @ %shooterRecentlyFreed);
				return;
			}
		}

		
		%damagedClientPos = GameBase::getPosition(%damagedClient);
		%shooterClientPos = GameBase::getPosition(%shooterClient);

		%damagedCurrentArmor = GetCurrentlyWearingArmor(%damagedClient);

		//==============
		//PROCESS STATS
		//==============
		%isMiss = false;
		%Pierce = false;
		%Bash = false;
		%Cleave = false;
		%sameTeamNull = false;

		//------------- CREATE DAMAGE VALUE -------------
		if(%type == $SpellDamageType)
		{
			//For the case of SPELLS, the initial damage has already been determined before calling this function

			%dmg = %value;
			%skillValue = $PlayerSkill[%shooterClient, %skilltype];
			if(%skillValue == "" || %skillValue == -1)
				%skillValue = 1000; // Default skill value if not set (for NPCs or edge cases)
			
			%value = round((%dmg * %skillValue) / 1000);

			%ab = (getRandom() * (fetchData(%damagedClient, "MDEF") / 10)) + 1;
			%value = Cap(%value - %ab, 0, "inf");

			%value = (%value / $TribesDamageToNumericDamage);
			
		}
		else if(%type != $LandingDamageType)
		{
			// Check if shooter is a turret - turrets use projectile base damage, not physical damage formula
			%isTurretDamage = false;
			if(isObject(%object))
			{
				%objectClassName = %object.className;
				if(%objectClassName == "Turret")
					%isTurretDamage = true;
			}
			
			// Also detect turret projectiles by checking if it's MissileDamageType from a source without skills/ATK
			// Turrets use MissileDamageType projectiles but don't have player skills or ATK
			if(!%isTurretDamage && %type == $MissileDamageType)
			{
				// Check if shooter doesn't have ATK or skills (indicating it's a turret, not a player)
				%shooterATK = fetchData(%shooterClient, "ATK");
				if((%shooterATK == "" || %shooterATK == -1 || %shooterATK == 0) && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
				{
					// Check if it's not a vehicle projectile (vehicles are handled separately)
					%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
					%isVehicle = false;
					if(%shooterPlayerObj != -1)
					{
						%shooterType = getObjectType(%shooterPlayerObj);
						// Check if shooter object IS a vehicle/flier
						if(%shooterType == "Vehicle" || %shooterType == "Flier")
							%isVehicle = true;
						// Also check if player is IN a vehicle (for when player fires from vehicle)
						if(!%isVehicle && %shooterPlayerObj.vehicle != "")
							%isVehicle = true;
					}
					// If not a vehicle and no ATK/skills, it's likely a turret
					if(!%isVehicle)
						%isTurretDamage = true;
				}
			}
			
			// For turret damage, use the projectile's base damage value directly
			// Turrets don't have ATK, weapons, or skills, so we can't use the physical damage formula
			if(%isTurretDamage)
			{
				// Use the projectile's damageValue (from baseProjData.cs)
				// FusionBolt, GuardianBolt, etc. have damageValue = 1 (in Tribes damage units)
				// Convert from Tribes damage to numeric damage
				// Turret projectiles typically have damageValue = 1, which needs to be scaled appropriately
				%baseTurretDamage = 100; // Base turret damage in Tribes damage units (equivalent to 100 damage)
				
				// Scale turret damage with player level to keep threat consistent across all levels
				// Higher level players have more DEF and HP, so we scale base damage to compensate
				// Formula: baseDamage * (1 + (level / 100))
				// This means: Level 0 = 1x, Level 100 = 2x, Level 200 = 3x, Level 500 = 6x
				%playerLevel = fetchData(%damagedClient, "LVL");
				if(%playerLevel == "" || %playerLevel == -1)
					%playerLevel = 1;
				%levelMultiplier = 1 + (%playerLevel / 100);
				%scaledTurretDamage = %baseTurretDamage * %levelMultiplier;
				%value = %scaledTurretDamage;
				
				// Apply DEF reduction (turrets should still respect DEF)
				// Higher level players have more DEF, which naturally reduces the scaled damage
				%ab = (getRandom() * (fetchData(%damagedClient, "DEF") / 10)) + 1;
				%value = Cap(%value - %ab, 1, "inf");
				
				// Add random variation (±15%)
				%a = (%value * 0.15);
				%r = round((getRandom() * (%a*2)) - %a);
				%value += %r;
				if(%value < 1)
					%value = 1;
				
				// Convert from Tribes damage to numeric damage
				%value = (%value / $TribesDamageToNumericDamage);
			}
			else
			{
				%multi = 1;

				if(fetchData(%damagedClient, "invisible"))
				{
					UnHide(%damagedClient);
				}			

				//Bash
			if(fetchData(%shooterClient, "NextHitBash"))
			{
				if(%skilltype == $SkillBludgeoning)
				{
					// BALANCED BASH: Use diminishing returns and cap to prevent OP scaling with high-level weapons
					%bashSkill = $PlayerSkill[%shooterClient, $SkillBashing];
					if(%bashSkill == "" || %bashSkill == -1)
						%bashSkill = 0;
					
					// Cap bashing skill at 1000 for calculation
					if(%bashSkill > 1000)
						%bashSkill = 1000;
					
					// Diminishing returns formula: Use piecewise linear scaling
					// First 500 skill: linear scaling (skill / 500)
					// After 500 skill: diminishing returns (500/500 + (skill-500)/1000)
					// This means: 100 skill = +0.20x, 500 skill = +1.00x, 1000 skill = +1.50x
					// Old formula: 1000 skill = +2.13x (too OP)
					if(%bashSkill <= 500)
					{
						%bashMultiplier = %bashSkill / 500;
					}
					else
					{
						%bashMultiplier = 1.0 + ((%bashSkill - 500) / 1000);
					}
					
					// Cap maximum bash bonus at +1.5x (prevents extreme scaling with high-tier weapons)
					if(%bashMultiplier > 1.5)
						%bashMultiplier = 1.5;
					
					%multi += %bashMultiplier;
					%b = GameBase::getRotation(%shooterClient);
					// MASSIVELY SCALED DOWN: Use square root scaling and cap to prevent enemies from disappearing
					// Old formula: SkillBashing / 15 (could reach 200+ at high levels)
					// New formula: sqrt(SkillBashing) / 2, capped at 10
					%bashSkillForShove = $PlayerSkill[%shooterClient, $SkillBashing];
					if(%bashSkillForShove == "" || %bashSkillForShove == -1)
						%bashSkillForShove = 0;
					%c = sqrt(%bashSkillForShove) / 2;
					if(%c > 10)
						%c = 10;  // Cap at 10 to prevent extreme shove forces
					%Bash = true;
				}

				%delay = Cap(300 - ($PlayerSkill[%shooterClient, $SkillBashing] / 30), 120, 300);
				schedule("storeData(" @ %shooterClient @ ", \"blockBash\", \"\");", %delay);
				storeData(%shooterClient, "NextHitBash", "");
			}
	
			//Pierce
			if(fetchData(%shooterClient, "NextHitPierce"))
			{
				if(%skilltype == $SkillPiercing)
				{
					%multi += $PlayerSkill[%shooterClient, $SkillPiercing] / 470;
					%Pierce = true;
				}
				
				%delay = Cap(360 - ($PlayerSkill[%shooterClient, $SkillPiercing] / 100), 30, 360);
				schedule("storeData(" @ %shooterClient @ ", \"blockPierce\", \"\");", %delay);
				storeData(%shooterClient, "NextHitPierce", "");				
			}
			
			//Cleave
			if(fetchData(%shooterClient, "NextHitCleave"))
			{
				if(%skilltype == $SkillSlashing)
				{
					%multi += $PlayerSkill[%shooterClient, $SkillSlashing] / 470;
					%Cleave = true;
				}

				%delay = Cap(360 - ($PlayerSkill[%shooterClient, $SkillSlashing] / 100), 30, 360);
				schedule("storeData(" @ %shooterClient @ ", \"blockCleave\", \"\");", %delay);
				storeData(%shooterClient, "NextHitCleave", "");
			}			

			//============================================================================
			// SPECIAL WEAPON EFFECTS
			//============================================================================
			
			// Track hit counter for weapon effects
			%weaponEffect = $WeaponEffect[%weapon];
			if(%weaponEffect != "")
			{
				// Increment hit counter for this weapon on this target
				// Use global array - TorqueScript doesn't support dynamic property access
				%hitCount = $WeaponHitCount[%shooterClient, %weapon, %damagedClient];
				if(%hitCount == "" || %hitCount == -1)
					%hitCount = 0;
				%hitCount++;
				$WeaponHitCount[%shooterClient, %weapon, %damagedClient] = %hitCount;

				
				// FINAL VERDICT - 1% instant kill on non-boss targets
				if(%weaponEffect == "INSTANT_KILL")
				{
					%chance = $WeaponEffectChance[%weapon];
					if(%chance == "" || %chance == -1) %chance = 1;
					
					%roll = floor(getRandom() * 100) + 1;  // 1-100
					if(%roll <= %chance)
					{
						// Check if target is protected (town bots, same team, party members, bosses)
						%isProtected = false;
						%targetName = Client::getName(%damagedClient);
						%spawnBotInfo = fetchData(%damagedClient, "SpawnBotInfo");
						
						// Protection 1: Town bots are immune
						if(isTownBot(%damagedClient))
						{
							%isProtected = true;
						}
						
						// Protection 2: Same team players (unless in duel or on hit list)
						if(!%isProtected)
						{
							%shooterTeam = GameBase::getTeam(%shooterClient);
							%targetTeam = GameBase::getTeam(%damagedClient);
							
							// If same team and target is NOT an enemy bot
							if(%shooterTeam == %targetTeam && !IsEnemyBot(%damagedClient))
							{
								// Check if in duel or on hit list
								%inDuel = (fetchData(%shooterClient, "DuelTarget") == %damagedClient);
								%onHitList = (String::findSubStr(fetchData(%shooterClient, "Hitlist"), Client::getName(%damagedClient)) != -1);
								
								if(!%inDuel && !%onHitList)
								{
									%isProtected = true;
								}
							}
						}
						
						// Protection 3: Party members are immune
						if(!%isProtected)
						{
							%shooterParty = fetchData(%shooterClient, "PARTY");
							%targetParty = fetchData(%damagedClient, "PARTY");
							if(%shooterParty != "" && %shooterParty != -1 && %shooterParty == %targetParty)
							{
								%isProtected = true;
							}
						}
						
						// Protection 4: Bosses are immune
						if(!%isProtected)
						{
							if(String::findSubStr(%targetName, "Boss") != -1 || 
							   String::findSubStr(%targetName, "King") != -1 ||
							   String::findSubStr(%targetName, "Queen") != -1 ||
							   String::findSubStr(%spawnBotInfo, "Boss") != -1)
							{
								%isProtected = true;
							}
						}
						
						// Protection 5: Seal Battle bots are immune
						if(!%isProtected)
						{
							%isSealBot = fetchData(%damagedClient, "SealBattleBot");
							if(%isSealBot == "true" || %isSealBot == "True" || %isSealBot == "1")
							{
								%isProtected = true;
							}
							// Also check for Seal in display name
							if(String::findSubStr(%targetName, "SealFighter") == 0 || 
							   String::findSubStr(%targetName, "SealMage") == 0 ||
							   String::findSubStr(%targetName, "SealGuardian") == 0)
							{
								%isProtected = true;
							}
						}
						
						// Protection 6: Colloseum arena bots are immune
						if(!%isProtected)
						{
							// Check if target is in Colloseum zone
							%targetPos = GameBase::getPosition(Client::getOwnedObject(%damagedClient));
							if(%targetPos != "" && %targetPos != -1)
							{
								%targetZone = Zone::fetchZone(getWord(%targetPos, 0), getWord(%targetPos, 1), getWord(%targetPos, 2), "DESC");
								if(%targetZone == "Colloseum")
								{
									%isProtected = true;
								}
							}
						}
						
						if(!%isProtected)
						{
							// INSTANT KILL! Call Player::kill directly to bypass LCK
							%targetName = Client::getName(%damagedClient);
							
							// Send special message BEFORE killing
							Client::sendMessage(%shooterClient, 0, "~wgame/explode3.wav");
							Client::sendMessage(%shooterClient, $MsgRed, "FINAL VERDICT! " @ %targetName @ " has been judged!");
							Client::sendMessage(%damagedClient, $MsgRed, "FINAL VERDICT! You have been instantly slain by Final Verdict!");
							
							// Kill the target directly - bypasses LCK and all other checks
							Player::kill(%damagedClient);
							
							// Return early - don't process rest of damage logic
							return;
						}
					}
				}
				
				// STORM CALLER - Lightning strike every N hits
				if(%weaponEffect == "LIGHTNING_STRIKE")
				{
					%frequency = $WeaponEffectFrequency[%weapon];
					if(%frequency == "" || %frequency == -1) %frequency = 5;
					
					if((%hitCount % %frequency) == 0)
					{
						// Trigger lightning strike!
						%targetPos = GameBase::getPosition(Client::getOwnedObject(%damagedClient));
						if(%targetPos != "" && %targetPos != -1)
						{
							// Create lightning visual effect
							CreateAndDetBomb(%shooterClient, "Bomb21", %targetPos, False, 0);
							playSound(shockExplosion, %targetPos);
							
							// Calculate bonus magic damage (scales with Piercing skill)
							%lightningDmg = 500;  // Base lightning damage
							%piercingSkill = $PlayerSkill[%shooterClient, $SkillPiercing];
							if(%piercingSkill == "" || %piercingSkill == -1) %piercingSkill = 1000;
							%lightningDmg = round((%lightningDmg * %piercingSkill) / 1000);
							
							// Apply MDEF reduction for lightning damage
							%targetMDEF = fetchData(%damagedClient, "MDEF");
							if(%targetMDEF == "" || %targetMDEF == -1) %targetMDEF = 0;
							%mdefReduction = (getRandom() * (%targetMDEF / 10)) + 1;
							%lightningDmg = floor(Cap(%lightningDmg - %mdefReduction, 1, "inf"));
							
							// Add lightning damage to total
							%value += %lightningDmg;
							
							Client::sendMessage(%shooterClient, $MsgYellow, "LIGHTNING STRIKE! +" @ %lightningDmg @ " bonus damage!");
						}
					}
				}
				
				// WORLD SPLITTER - Alternating physical/magic damage
				if(%weaponEffect == "ALTERNATING_DAMAGE")
				{
					// Odd hits = physical (DEF), Even hits = magic (MDEF)
					if((%hitCount % 2) == 0)
					{
						// Even hit - use MDEF instead of DEF
						%useMDEF = true;
					}
				}
			}


			if(%rweapon != "")
				%rweapondamage = GetRoll(GetWord(GetAccessoryVar(%rweapon, $SpecialVar), 1));
			else
				%rweapondamage = 0;
			%weapondamage = GetRoll(GetWord(GetAccessoryVar(%weapon, $SpecialVar), 1));

			%playerattack = fetchData(%shooterClient, "ATK") - %weapondamage;
			%skillValue = $PlayerSkill[%shooterClient, %skilltype];
			if(%skillValue == "" || %skillValue == -1)
				%skillValue = 1000; // Default skill value if not set (for NPCs or edge cases)
			
			// Skip normal damage calculation if instant kill triggered (Final Verdict)
			if(!%instantKill)
			{
				%value = round((( (%weapondamage + (%playerattack + %rweapondamage)) / 1000) * %skillValue) * %multi);
			}


			// Skip DEF reduction and variance for instant kill (Final Verdict)
			if(!%instantKill)
			{
				%ab = (getRandom() * (fetchData(%damagedClient, "DEF") / 10)) + 1;
				
				// World Splitter: Even hits use MDEF instead of DEF
				if(%useMDEF)
				{
					%ab = (getRandom() * (fetchData(%damagedClient, "MDEF") / 10)) + 1;
				}
				
				%value = Cap(%value - %ab, 1, "inf");

				%a = (%value * 0.15);
				%r = round((getRandom() * (%a*2)) - %a);
				%value += %r;
				if(%value < 1)
					%value = 1;
			}


			if(%Bash)	//i'm doing this condition here because %mom is dependant on %value
			{
				// MASSIVELY SCALED DOWN: Reduce shove force by using damage scaling and capping
				// Old formula: (SkillBashing / 15) / 15 * %value = SkillBashing / 225 * %value
				// At 2000 skill, 2000 damage: 2000/225 * 2000 = 17,777 (way too high!)
				// New formula: %c * sqrt(%value) / 10, with additional cap
				// %c is already capped at 10, so max forward force = 10 * sqrt(damage) / 10 = sqrt(damage)
				// At 2000 damage: sqrt(2000) = 44.7 (much more reasonable)
				%c1 = %c * sqrt(%value) / 10;
				// Cap forward force to prevent extreme values (max 50 units)
				if(%c1 > 50)
					%c1 = 50;
				// Vertical component: small upward push (1/10th of forward force, max 5)
				%c2 = %c1 / 10;
				if(%c2 > 5)
					%c2 = 5;
				%mom = Vector::getFromRot( %b, %c1, %c2 );
			}

			// Vehicle Combat skill bonus: +1 damage per 100 skill points (capped at 1000 = +10 damage)
			// Check if this is a vehicle projectile
			%isVehicleProjectile = false;
			
			// Get the shooter's player object to check if it's a vehicle
			%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
			if(%shooterPlayerObj != -1)
			{
				%shooterType = getObjectType(%shooterPlayerObj);
				// Check if it's a vehicle object
				if(%shooterType == "Vehicle" || %shooterType == "Flier")
				{
					%isVehicleProjectile = true;
				}
			}
			// Also check if shooter is a client ID but the player is in a vehicle
			if(!%isVehicleProjectile && %type == $MissileDamageType && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
			{
				// This is likely a vehicle projectile - check if player is in a vehicle
				%playerObj = Client::getOwnedObject(%shooterClient);
				if(%playerObj != -1 && %playerObj.vehicle != "")
				{
					%isVehicleProjectile = true;
				}
			}
			
			if(%isVehicleProjectile && !isRPGAI(%shooterClient))
			{
				// Get Vehicle Combat skill (Skill 14)
				%vehicleSkill = $PlayerSkill[%shooterClient, $SkillVehicleCombat];
				if(%vehicleSkill == "" || %vehicleSkill == -1)
					%vehicleSkill = 0;
				
				// Cap skill at 1000
				if(%vehicleSkill > 1000)
					%vehicleSkill = 1000;
				
				// Calculate bonus: +1 damage per 100 skill points
				%vehicleBonus = floor(%vehicleSkill / 100);
				
				// Add bonus to damage (in Tribes damage units, before conversion)
				%value += %vehicleBonus;
			}

			%value = (%value / $TribesDamageToNumericDamage);
			}
		}

		//------------- DETERMINE MISS OR HIT -------------
		if(%preCalcMiss == "")
		{
			if(%type != $LandingDamageType && %shooterClient != %damagedClient && %shooterClient != 0)
			{
				// Check if shooter is a vehicle - vehicles always hit (no miss calculation)
				%isVehicle = false;
				
				// Get the shooter's player object to check if it's a vehicle
				%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
				if(%shooterPlayerObj != -1)
				{
					%shooterType = getObjectType(%shooterPlayerObj);
					// Check if it's a vehicle object
					if(%shooterType == "Vehicle" || %shooterType == "Flier")
					{
						%isVehicle = true;
					}
				}
				// Also check if shooter is a client ID but the player is in a vehicle
				// For vehicle projectiles, the engine might pass the controlling player's client ID
				// but we can detect this by checking if the skill type is invalid/empty
				// and the damage type is MissileDamageType (vehicle projectiles)
				if(!%isVehicle && %type == $MissileDamageType && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
				{
					// This is likely a vehicle projectile - check if player is in a vehicle
					%playerObj = Client::getOwnedObject(%shooterClient);
					if(%playerObj != -1 && %playerObj.vehicle != "")
					{
						%isVehicle = true;
					}
				}
				
				if(%isVehicle)
				{
					// Vehicles always hit - skip miss calculation
					%isMiss = false;
				}
				else
				{
					// Normal player/weapon miss calculation
					if(%type == $SpellDamageType)
					{
						%defenderDEF = fetchData(%damagedClient, "MDEF");
						%defenderSkill = $PlayerSkill[%damagedClient, $SkillSpellResistance];
						if(%defenderSkill == "" || %defenderSkill == -1)
							%defenderSkill = 0;
						%x = (%defenderDEF / 5) + %defenderSkill + 5;
						%defenseType = "MDEF";
						%skillTypeName = "SpellResistance";
					}
					else
					{
						%defenderDEF = fetchData(%damagedClient, "DEF");
						%defenderSkill = $PlayerSkill[%damagedClient, $SkillDodging];
						if(%defenderSkill == "" || %defenderSkill == -1)
							%defenderSkill = 0;
						%x = (%defenderDEF / 5) + %defenderSkill + 5;
						%defenseType = "DEF";
						%skillTypeName = "Dodging";
					}
					%attackerSkill = $PlayerSkill[%shooterClient, %skilltype];
					if(%attackerSkill == "" || %attackerSkill == -1)
						%attackerSkill = 1000; // Default skill value if not set (for NPCs or edge cases)
					%y = %attackerSkill + 5;
					
					%n = %x + %y;
					
					%r = floor(getRandom() * %n) + 1;
					
					// Calculate hit/miss percentages
					%missChance = (%x / %n) * 100;
					%hitChance = (%y / %n) * 100;
					
					%attackerName = Client::getName(%shooterClient);
					if(%attackerName == "")
						%attackerName = "AI Bot";
					%defenderName = Client::getName(%damagedClient);
					if(%defenderName == "")
						%defenderName = "AI Bot";
					
					%initialMiss = false;
					if(%r <= %x)
					{
						if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Calculated MISS! r=" @ %r @ " <= x=" @ %x @ " (Def=" @ %defenderDEF @ " Atk=" @ %attackerSkill @ ")");
						%isMiss = true;
						%initialMiss = true;
					}
					
					// LCK-based miss check
					%lckMissChance = (AddPoints(%damagedClient, 2) / 100);
					%lckRoll = getRandom();
					%lckMiss = false;
					if(!%initialMiss && %lckRoll <= %lckMissChance)
					{
						%isMiss = true;
						%lckMiss = true;
					}
					
					// Get all defender stats for debug output
					%defenderDEFValue = fetchData(%damagedClient, "DEF");
					%defenderMDEFValue = fetchData(%damagedClient, "MDEF");
					%defenderDodgingSkill = $PlayerSkill[%damagedClient, $SkillDodging];
					if(%defenderDodgingSkill == "" || %defenderDodgingSkill == -1)
						%defenderDodgingSkill = 0;
					%defenderSpellResistanceSkill = $PlayerSkill[%damagedClient, $SkillSpellResistance];
					if(%defenderSpellResistanceSkill == "" || %defenderSpellResistanceSkill == -1)
						%defenderSpellResistanceSkill = 0;
					
					// Debug output for hit/miss calculation
					//echo("DEBUG HIT/MISS: " @ %attackerName @ " -> " @ %defenderName);
					%damageTypeName = "Physical";
					if(%type == $SpellDamageType)
						%damageTypeName = "Spell";
					//echo("  Damage Type: " @ %damageTypeName);
					//echo("  Defender Stats: DEF=" @ %defenderDEFValue @ ", MDEF=" @ %defenderMDEFValue @ ", Dodging=" @ %defenderDodgingSkill @ ", SpellResistance=" @ %defenderSpellResistanceSkill);
					//if(%type == $SpellDamageType)
					//	echo("  Defender Used: MDEF=" @ %defenderMDEFValue @ " (MDEF/5=" @ floor(%defenderMDEFValue / 5) @ "), SpellResistance=" @ %defenderSpellResistanceSkill @ ", Base=5");
					//else
					//	echo("  Defender Used: DEF=" @ %defenderDEFValue @ " (DEF/5=" @ floor(%defenderDEFValue / 5) @ "), Dodging=" @ %defenderDodgingSkill @ ", Base=5");
					//echo("  Attacker: Skill=" @ %attackerSkill @ " (type: " @ %skilltype @ "), Base=5");
					//echo("  Calculation: x=" @ %x @ " (defender), y=" @ %y @ " (attacker), n=" @ %n @ " (total)");
					//echo("  Roll: r=" @ %r @ " (1-" @ %n @ ")");
					// Only show miss % when a player is attacking a bot (not when bots attack players)
					//if(!isRPGAI(%shooterClient) && isRPGAI(%damagedClient))
					//	echo("Player " @ %attackerName @ " (ID: " @ %shooterClient @ ") attacking bot - Miss Chance: " @ floor(%missChance * 100) / 100 @ "%, Hit Chance: " @ floor(%hitChance * 100) / 100 @ "%");
					//if(%initialMiss)
					//	echo("  RESULT: MISS (r <= x: " @ %r @ " <= " @ %x @ ")");
					//else if(%lckMiss)
					//{
					//	echo("  RESULT: MISS (LCK check: roll " @ floor(%lckRoll * 10000) / 10000 @ " <= " @ floor(%lckMissChance * 10000) / 10000 @ ")");
					//	echo("  LCK: " @ fetchData(%damagedClient, "LCK") @ ", LCK Miss Chance: " @ floor(%lckMissChance * 10000) / 10000);
					//}
					//else
					//	echo("  RESULT: HIT");
				}
			}
		}
		//------------- CHECK FOR A CRITICAL HIT -------------
		if(getRandom() > 0.5)
		{
			%norm = 1.000;
			%critskill = $PlayerSkill[%shooterClient, $SkillCriticals] / 3000;
			%critchance = %norm - %critskill;
			%critchance -= (AddPoints(%shooterClient, 1) / 100);
			if(%critchance < 0.1)
				%critchance = 0.1;
			if(getRandom() > %critchance)
				%criticalAttack = true;
		}
		
		//=======================================|WATER CHECKS|=========================================
		//------------------------------------
		// CHECK IF PLAYER LANDED ON WATER
		//------------------------------------
		if(%damagedClient == %shooterClient && %type == $LandingDamageType)
		{
			%object = "";
			for(%i = 0; %i >= -3.15; %i -= 1.57)
			{
				if(GameBase::getLOSInfo(Client::getOwnedObject(%damagedClient), 5, %i @ " 0 0"))
				{
					if(getObjectType($los::object) == "InteriorShape" && String::getSubStr(Object::getName($los::object), 0, 5) == "water")
					{
						%object = $los::object;
						break;
					}
				}
			}
			
			if(%object != "")
			{
				%value *= $waterDamageAmp;
				playSound(SoundSplash1, %damagedClientPos);
			}
		}

		//---------------------------------------
		// CHECK IF PLAYER LANDED WHILE IN WATER
		//---------------------------------------
		if(%damagedClient == %shooterClient && %type == $LandingDamageType)
		{
			if(Zone::getType(fetchData(%damagedClient, "zone")) == "WATER")
			%value *= $waterDamageAmp;
		}
		else if(%damagedClient != %shooterClient && %type == $LandingDamageType)
			%value = (fetchData(%damagedClient, "MaxHP") * (%value / 100) / 100);
		
		//---------------------------------------
		// PREVENT BOTS FROM TAKING COLLISION/FALL DAMAGE
		//---------------------------------------
		// Enemy bots and town bots should not take damage from being shoved or falling
		// This prevents them from taking collision damage when players shove them
		if(%type == $LandingDamageType)
		{
			%isEnemyBot = IsEnemyBot(%damagedClient);
			%isTownBot = IsTownBot(%damagedClient);
			if(%isEnemyBot || %isTownBot)
			{
				// Set damage to 0 for all bots (enemy and town) when taking landing/collision damage
				%value = 0;
				%isMiss = false;
				%noImpulse = true;
			}
		}
		//============================================================================================

		//------------------------------------------------
		// SEAL BATTLE BOT DAMAGE PREVENTION
		//------------------------------------------------
		// Prevent seal battle bots from damaging each other, but allow them to damage players
		// Also allow mages to damage themselves with their spells
		if(fetchData(%shooterClient, "SealBattleBot") == "true" && fetchData(%damagedClient, "SealBattleBot") == "true" && %shooterClient != %damagedClient)
		{
			// Both are seal battle bots and not the same entity - prevent damage
			%value = 0;
			%isMiss = false;
			%noImpulse = true;
			%sameTeamNull = true;
		}
		
		// Prevent players NOT in Colloseum from damaging seal battle bots that ARE in Colloseum
		// This prevents camping outside and sniping bots during seal battles
		if($SealBattleActive == true && !isRPGAI(%shooterClient) && fetchData(%damagedClient, "SealBattleBot") == "true")
		{
			%shooterZone = Zone::getType(fetchData(%shooterClient, "zone"));
			%damagedZone = Zone::getType(fetchData(%damagedClient, "zone"));
			
			// If shooter is NOT in Colloseum but damaged bot IS in Colloseum, prevent damage
			if(%shooterZone != "Colloseum" && %damagedZone == "Colloseum")
			{
				%value = 0;
				%isMiss = false;
				%noImpulse = true;
				%sameTeamNull = true;
			}
		}
		
		// Prevent players NOT in Colloseum from damaging Colloseum arena bots that ARE in Colloseum
		// This prevents camping outside and sniping bots during Colloseum arena battles
		// Colloseum bots are named "Round1", "Round2", "Round3" (from rpgarena.cs)
		if($Fight != "" && !isRPGAI(%shooterClient) && isRPGAI(%damagedClient))
		{
			%damagedName = Client::getName(%damagedClient);
			%isColloseumBot = (%damagedName == "Round1" || %damagedName == "Round2" || %damagedName == "Round3");
			
			if(%isColloseumBot)
			{
				%shooterZone = Zone::getType(fetchData(%shooterClient, "zone"));
				%damagedZone = Zone::getType(fetchData(%damagedClient, "zone"));
				
				// If shooter is NOT in Colloseum but damaged bot IS in Colloseum, prevent damage
				if(%shooterZone != "Colloseum" && %damagedZone == "Colloseum")
				{
					%value = 0;
					%isMiss = false;
					%noImpulse = true;
					%sameTeamNull = true;
				}
			}
		}

		//------------------------------------------------
		// AI BOT TO AI BOT DAMAGE PREVENTION
		//------------------------------------------------
		// Prevent all AI bots from damaging each other, but allow:
		// - AI bots to damage players
		// - AI bots to damage themselves (self-damage from spells, handled later)
		if(isRPGAI(%shooterClient) && isRPGAI(%damagedClient) && %shooterClient != %damagedClient)
		{
			// Both are AI bots and not the same entity - prevent damage
			%value = 0;
			%isMiss = false;
			%noImpulse = true;
			%sameTeamNull = true;
		}

		//------------------------------------------------
		// SAME TEAM CHECKS
		//------------------------------------------------
		// AI mobs can always attack players, skip all same-team checks for AI attackers
		if(!Duel::isDueling(%damagedClient) && !Duel::isDueling(%shooterClient) && !isRPGAI(%shooterClient))
		{
			// Prevent all player-to-player damage when seal battle is active and players are in Colloseum zone (allows AI to still damage players)
			if($SealBattleActive == true && %shooterClient != %damagedClient)
			{
				%damagedZone = Zone::getType(fetchData(%damagedClient, "zone"));
				%shooterZone = Zone::getType(fetchData(%shooterClient, "zone"));
				
				if(%damagedZone == "Colloseum" || %shooterZone == "Colloseum")
				{
					// Only show message once per seal battle per client to avoid spam
					if(fetchData(%shooterClient, "sealDamageMsgShown") != "true")
					{
						Client::sendMessage(%shooterClient, $MsgWhite, "Player-on-Player damage is disabled in Colloseum during seal battles.");
						storeData(%shooterClient, "sealDamageMsgShown", "true");
					}
					if(fetchData(%damagedClient, "sealDamageMsgShown") != "true")
					{
						Client::sendMessage(%damagedClient, $MsgWhite, "Player-on-Player damage is disabled in Colloseum during seal battles.");
						storeData(%damagedClient, "sealDamageMsgShown", "true");
					}
					
					%value = 0;
					%isMiss = false;
					%noImpulse = true;
					%sameTeamNull = true;
				}
			}
			
			if(Client::getTeam(%damagedClient) == Client::getTeam(%shooterClient) && %shooterClient != %damagedClient)
			{
				// TEMPORARILY DISABLED - HasTheftFlag check
				//if(!HasTheftFlag(%damagedClient))
				//{
				// Prevent damage if damaged player is in PROTECTED zone (regardless of shooter's zone)
				if(Zone::getType(fetchData(%damagedClient, "zone")) == "PROTECTED")
				{
					%value = 0;
					%isMiss = false;
					%noImpulse = true;
					%sameTeamNull = true;
				}
				//}
					
					///no target-list involved
					if(!(IsInCommaList(fetchData(%damagedClient, "targetlist"), Client::getName(%shooterClient)) || IsInCommaList(fetchData(%shooterClient, "targetlist"), Client::getName(%damagedClient))) )
					{	
						%dhn = $House::Index[fetchData(%damagedClient, "MyHouse")];
						%shn = $House::Index[fetchData(%shooterClient, "MyHouse")];
						if(%dhn == %shn)
						{
							%value = 0;
							%isMiss = false;
							%noImpulse = true;
							%sameTeamNull = true;
						}
						else
						{
							if(%dhn == "" || %shn == "")
							{
								//one of the people involved is not in a house, so no damage occurs
								%value = 0;
								%isMiss = false;
								%noImpulse = true;
								%sameTeamNull = true;
							}
						}
					}

					if(Zone::getType(fetchData(%damagedClient, "zone")) != "PROTECTED" && Zone::getType(fetchData(%shooterClient, "zone")) != "PROTECTED")
					{
						if(fetchData(%damagedClient, "partyOwned"))
						{
							if(IsInCommaList(fetchData(%damagedClient, "partylist"), Client::getName(%shooterClient)))
							{
								%value = 0;
								%isMiss = false;
								%noImpulse = true;
								%sameTeamNull = true;
							}
						}
						else if(fetchData(%shooterClient, "partyOwned"))
						{
							if(IsInCommaList(fetchData(%shooterClient, "partylist"), Client::getName(%damagedClient)))
							{
								%value = 0;
								%isMiss = false;
								%noImpulse = true;
								%sameTeamNull = true;
							}
						}
					}
				}
				else
				{
					//one of the people involved has the other one on his/her target-list.
					//so let damage go thru
				}
			}
		}
		//-------------------------------------------------
		// SAME PLAYER CHECKS
		//-------------------------------------------------
		if(%damagedClient == %shooterClient)
		{
			if(isRPGAI(%damagedClient))
				%value = %value / 3;
			else if(Zone::getType(fetchData(%damagedClient, "zone")) == "PROTECTED")
				%value = 0;
			else if(%type == $SpellDamageType)
				%value = %value / 3;
		}

		if(!IsDead(%this))
		{
			%armor = Player::getArmor(%this);
			storeData(%damagedClient, "tmpkillerid", %shooterClient);

			%hitby = Client::getName(%shooterClient);
			%msgcolor = "";

			if(%isMiss)
			{
				%msgcolor = $MsgRed;
				%value = 0;
			}
			else if(!%isMiss && %value == 0 && %shooterClient != %damagedClient)
			{
				%msgcolor = $MsgWhite;
			}
			if(%msgcolor != "")
			{
				// Default damageMode to true if not set (for backwards compatibility)
				%shooterDamageMode = fetchData(%shooterClient, "damageMode");
				if(%shooterDamageMode == "")
					%shooterDamageMode = true;
				%damagedDamageMode = fetchData(%damagedClient, "damageMode");
				if(%damagedDamageMode == "")
					%damagedDamageMode = true;
				
				%isAI = isRPGAI(%shooterClient);
				
				if(%type != $SpellDamageType)
				{
					// Only send message to shooter if they're a player (not AI)
					if(!%isAI && %shooterDamageMode)
					{
						%msg = "<jl>You try to hit " @ Client::getName(%damagedClient) @ ", but miss!";
						DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
					}

					// Always send miss message to damaged player
					if(%damagedDamageMode)
					{
						%time = getIntegerTime(true) >> 5;
						if(%time - %damagedClient.lastMissMessage > 2)
						{
							%damagedClient.lastMissMessage = %time;
							%msg = "<jr>" @ %hitby @ " tries to hit you, but misses!";
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
					}
				}
				else
				{
					// Only send message to shooter if they're a player (not AI)
					if(!%isAI && %shooterDamageMode)
					{
						if(%type == $SpellDamageType)
						{
							%msg = "<jl>" @ Client::getName(%damagedClient) @ " resists your spell!";
							DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
						}
						else
							Client::sendMessage(%shooterClient, %msgcolor, Client::getName(%damagedClient) @ " resists your spell!");
					}
					if(%damagedDamageMode)
					{
						if(%type == $SpellDamageType)
						{
							%msg = "<jr>You resist " @ %hitby @ "'s spell!";
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
						else
							Client::sendMessage(%damagedClient, %msgcolor, "You resist " @ %hitby @ "'s spell!");
					}
				}
			}

			//-------------------------------------------------
			// SKILLS
			//-------------------------------------------------
			if(%skilltype >= 1 && !%sameTeamNull && %shooterClient != %damagedClient)
			{
				%base1 = Cap(35 + (fetchData(%shooterClient, "LVL") - fetchData(%damagedClient, "LVL")), 1, "inf");
				%base2 = Cap(35 + (fetchData(%damagedClient, "LVL") - fetchData(%shooterClient, "LVL")), 1, "inf");
				if(%isMiss)
				{
					UseSkill(%shooterClient, %skilltype, false, true);
					UseSkill(%damagedClient, $SkillEndurance, false, true, 60);
					
					if(%type == $SpellDamageType)
						UseSkill(%damagedClient, $SkillSpellResistance, true, true, %base2);
					else
						UseSkill(%damagedClient, $SkillDodging, true, true, %base2 * (3/5));
				}
				else if(!%isMiss && %value == 0)
				{
					UseSkill(%shooterClient, %skilltype, false, true);
					UseSkill(%damagedClient, $SkillEndurance, false, true, 60);
					
					if(%type == $SpellDamageType)
						UseSkill(%damagedClient, $SkillSpellResistance, true, true, %base2);
					else
						UseSkill(%damagedClient, $SkillDodging, true, true, %base2 * (3/5));
				}
				else
				{
					UseSkill(%shooterClient, %skilltype, true, true, %base1);
					UseSkill(%damagedClient, $SkillEndurance, true, true, 60);

					if(%type == $SpellDamageType)
						UseSkill(%damagedClient, $SkillSpellResistance, true, true, %base2);
				}

				if(%Bash || %Cleave || %Pierce)
					UseSkill(%shooterClient, $SkillBashing, true, true);
			}

			// Ensure %value is numeric before processing
			if(%value == "" || %value == -1)
				%value = 0;
			
			if(%value)
			{
				if(%value < 0)
					%value = 0;
				if(%criticalAttack)
					%value *= 2;
				
				// Check if shooter is a turret - turret damage bypasses all stance modifiers (except Normal)
				// Use comprehensive turret detection (same as earlier in function)
				%isTurretDamage = false;
				if(isObject(%object))
				{
					%objectClassName = %object.className;
					if(%objectClassName == "Turret")
						%isTurretDamage = true;
				}
				
				// Also detect turret projectiles by checking if it's MissileDamageType from a source without skills/ATK
				// Turrets use MissileDamageType projectiles but don't have player skills or ATK
				if(!%isTurretDamage && %type == $MissileDamageType)
				{
					// Check if shooter doesn't have ATK or skills (indicating it's a turret, not a player)
					%shooterATK = fetchData(%shooterClient, "ATK");
					if((%shooterATK == "" || %shooterATK == -1 || %shooterATK == 0) && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
					{
						// Check if it's not a vehicle projectile (vehicles are handled separately)
						%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
						%isVehicle = false;
						if(%shooterPlayerObj != -1)
						{
							%shooterType = getObjectType(%shooterPlayerObj);
							// Check if shooter object IS a vehicle/flier
							if(%shooterType == "Vehicle" || %shooterType == "Flier")
								%isVehicle = true;
							// Also check if player is IN a vehicle (for when player fires from vehicle)
							if(!%isVehicle && %shooterPlayerObj.vehicle != "")
								%isVehicle = true;
						}
						// If not a vehicle and no ATK/skills, it's likely a turret
						if(!%isVehicle)
							%isTurretDamage = true;
					}
				}
				
				// Offensive stance: doubles damage dealt and received (all damage types, including spells)
				// Turret damage bypasses stance modifiers
				if(!%isTurretDamage && fetchData(%shooterClient, "Stance") == "Offensive")
					%value *= 2;
				if(!%isTurretDamage && fetchData(%damagedClient, "Stance") == "Offensive")
					%value *= 2;
				
				// Defensive stance: halves damage dealt and received (all damage types)
				// Turret damage bypasses stance modifiers
				if(!%isTurretDamage && fetchData(%shooterClient, "Stance") == "Defensive")
					%value /= 2;
				if(!%isTurretDamage && fetchData(%damagedClient, "Stance") == "Defensive")
					%value /= 2;
				
				// Glass Cannon stance: 2.5x spell damage dealt, -50% max HP and -50% max mana (handled in rpgstats.cs)
				// Turret damage is affected by Glass Cannon (allows turrets to benefit from Glass Cannon stance)
				if(fetchData(%shooterClient, "Stance") == "Glass Cannon" && %type == $SpellDamageType)
					%value *= 2.5;

				// MageBane: nullifies magic damage received, doubles physical damage received
				// Turret damage bypasses bane stances
				if(!%isTurretDamage && fetchData(%damagedClient, "MANA") > 0)
				{
					if(%type == $SpellDamageType && fetchData(%damagedClient, "Stance") == "MageBane") 
						%value = 0;
					if(%type != $SpellDamageType && fetchData(%damagedClient, "Stance") == "MageBane")
						%value *= 2;
				}
				
				// BladeBane: nullifies physical damage received, doubles magic damage received
				// Turret damage bypasses bane stances
				if(!%isTurretDamage && fetchData(%damagedClient, "MANA") > 0)
				{
					if(%type != $SpellDamageType && fetchData(%damagedClient, "Stance") == "BladeBane")
						%value = 0;
					if(%type == $SpellDamageType && fetchData(%damagedClient, "Stance") == "BladeBane")
					%value *= 2;
			}
			
			// =================================================================
			// ASCENSION TALENTS (permanent abilities from remort/SP sacrifice)
			// =================================================================
			
			// Berserker's Rage: +30% damage when attacker HP is below 25%
			if(Ascension::HasTalent(%shooterClient, "BerserkerRage"))
			{
				%attackerHP = fetchData(%shooterClient, "HP");
				%attackerMaxHP = fetchData(%shooterClient, "MaxHP");
				if(%attackerMaxHP > 0 && %attackerHP <= (%attackerMaxHP * 0.25))
					%value = floor(%value * 1.30);
			}
			
			// Iron Skin: 15% damage reduction from all sources
			if(Ascension::HasTalent(%damagedClient, "IronSkin"))
				%value = floor(%value * 0.85);
			
			// Dodge Mastery: 10% chance to completely avoid damage
			if(Ascension::HasTalent(%damagedClient, "DodgeMastery"))
			{
				if(floor(getRandom() * 100) < 10)
				{
					%value = 0;
					%isMiss = true;
					Client::sendMessage(%damagedClient, 0, "Dodge Mastery!");
				}
			}
			
			// =================================================================
				
			// Ensure value doesn't become 0 or negative after stance modifiers
			if(%value < 0)
				%value = 0;
				
				if(%Pierce)
				{
					//Not really any way to code this using a player's skill that won't
					// end up being too powerful at one point (ultimate pk anyone?)
					// instead let's just go with a 5% chance!

					//%regular = 1.0;
					//%chance = $PlayerSkill[%shooterClient, $SkillBashing] / 50000;
					//%special = %regular - %chance;
				
					//if(%special < 0.5)
					//	%special = 0.5;

					//if(getRandom() > %special)
					if(getRandom() > 0.95)
						$lckBypass[%damagedClient] = true;
				}
				
				%backupValue = %value;

				%rhp = refreshHP(%damagedClient, %value);

				%lckMiss = false;
				if(%rhp == -1)
				{
					%value = -1;	//There was an LCK miss
					%lckMiss = true;
				}
				else
				{
					if(!%noImpulse) Player::applyImpulse(%this,%mom);
						%noImpulse = "";

					if(%damagedCurrentArmor != "")
						%ahs = $ArmorHitSound[%damagedCurrentArmor];
					else
						%ahs = SoundHitFlesh;
					if(%skilltype == $SkillSlashing)
						PlaySound(%ahs, %damagedClientPos);
					else if(%skilltype == $SkillBludgeoning)
						PlaySound(%ahs, %damagedClientPos);
					else if(%skilltype == $SkillPiercing)
						PlaySound(%ahs, %damagedClientPos);
					else if(%skilltype == $SkillVehicleCombat)
						PlaySound(SoundArrowHit2, %damagedClientPos);
				}

				PlaySound(RandomRaceSound(fetchData(%damagedClient, "RACE"), Hit), %damagedClientPos);

			//display amount of damage caused
			// Check for LCK miss first before converting -1 to 0
			if(%lckMiss)
			{
				// Handle LCK miss message (this will be handled in the else if block below)
				%convValue = -1; // Keep as -1 to trigger LCK miss message
			}
			else
			{
				if(%value == "" || %value == -1)
					%value = 0;
				%convValue = round(%value * $TribesDamageToNumericDamage);
			}

			// Initialize LCK hit flag early (will be set later if LCK bypass or LCK death prevention occurs)
			%isLCKHit = false;
			
			// Check for LCK bypass (pierce attacks) before checking convValue (LCK hits bypass damage, so convValue might be 0)
			if($lckBypass[%damagedClient] == true)
			{
				%isLCKHit = true;
				$lckBypass[%damagedClient] = false; // Clear flag
			}
			
			// Also check for LCK death prevention (when attack would kill but LCK prevents death)
			if(%lckMiss == true)
			{
				%isLCKHit = true;
			}
			
			// Build messages if there's damage OR if it's an LCK hit (LCK hits show even with 0 damage or -1 convValue)
			if(%convValue > 0 || %isLCKHit)
			{
				if(%shooterClient == %damagedClient)
					{
						if(%type == $CrushDamageType)
							%hitby = "moving object";
						else if(%type == $DebrisDamageType)
							%hitby = "debris";
						else if(%type == $SpellDamageType)
							%hitby = "your " @ $Spell::name[$Spell::index[%weapon]];
						else
							%hitby = "yourself";
					}
					else if(%shooterClient == 0)
					{
						if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%hitby = $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "'");
								%hitby = "a powerful force";
							}
						}
						else
							%hitby = "a powerful force";
					}
					else
					{
						if(fetchData(%shooterClient, "invisible"))
							%hitby = "an unknown assailant";
						else if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%hitby = Client::getName(%shooterClient) @ "'s " @ $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "', shooter=" @ %shooterClient);
								%hitby = Client::getName(%shooterClient);
							}
						}
						else
							%hitby = Client::getName(%shooterClient);
					}

					if(%Pierce)
					{
						%daction = "pierced";
						%saction = "pierced";
					}
					else if(%Bash)
					{
						%daction = "bashed";
						%saction = "bashed";
					}
					else if(%Cleave)
					{
						%daction = "cleaved";
						%saction = "cleaved";
					}					
					else
					{
						%daction = "damaged";
						%saction = "damaged";
					}

					//--------------------
					// Build ATKText messages (bouncing damage display)
					//--------------------
					// Get enemy name for messages
					%enemyName = Client::getName(%damagedClient);
					%attackerName = Client::getName(%shooterClient);
					
					// LCK hit flag was already checked and set earlier (before convValue check)
					
					// Build attacker message (player dealing damage)
					if(%isLCKHit)
					{
						%Val1 = "You hit " @ %enemyName @ " for LCK!";
					}
					else
					{
						%Val1 = "You hit " @ %enemyName @ " for " @ %convValue @ "!";
					}
					
					// Build defender message (player taking damage)
					// Always use "'s attack" format as requested
					if(%isLCKHit)
					{
						%Val2 = %attackerName @ "'s attack hit you for LCK!";
					}
					else
					{
						%Val2 = %attackerName @ "'s attack hit you for " @ %convValue @ "!";
					}

					if(%Bash)
					{
						%Val1 = "Bashed! " @ %Val1;
						%Val2 = "Bashed! " @ %Val2;
					}
					else if(%Pierce)
					{
						%Val1 = "Pierced! " @ %Val1;
						%Val2 = "Pierced! " @ %Val2;
					}
					else if(%Cleave)
					{
						%Val1 = "Cleaved! " @ %Val1;
						%Val2 = "Cleaved! " @ %Val2;
					}

					if(%criticalAttack)
					{
						%Val1 = "<f2>Critical hit! <f0>" @ %Val1;
						%Val2 = "<f2>Critical hit! <f1>" @ %Val2;
					}

					//--------------------
					//display to involved
					//--------------------
					// Default damageMode to true if not set (for backwards compatibility)
					%shooterDamageMode = fetchData(%shooterClient, "damageMode");
					if(%shooterDamageMode == "")
						%shooterDamageMode = true;
					%damagedDamageMode = fetchData(%damagedClient, "damageMode");
					if(%damagedDamageMode == "")
						%damagedDamageMode = true;
					
					if(%shooterClient != %damagedClient)
						if(%shooterDamageMode)
						{
							%spellName = "";
							if(%type == $SpellDamageType && %weapon != "")
							{
								%spellIndex = $Spell::index[%weapon];
								if(%spellIndex != "")
									%spellName = " with " @ $Spell::name[%spellIndex];
							}
							// Check for LCK hit first - use %Val1 which already has the correct LCK message
							if(%isLCKHit)
							{
								// Use %Val1 which was already constructed with "You hit [enemy] for LCK!" message
								%msg = "<jl>" @ %Val1;
								DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
							}
							else if(%type == $SpellDamageType)
							{
								if(%criticalAttack)
								{
									%msg = "<jl>You critically " @ %saction @ " " @ Client::getName(%damagedClient) @ %spellName @ " for " @ %convValue @ " points of damage!";
									DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
								}
								else
								{
									%msg = "<jl>You " @ %saction @ " " @ Client::getName(%damagedClient) @ %spellName @ " for " @ %convValue @ " points of damage!";
									DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
								}
							}
							else
							{
								// For weapon attacks, use %Val1 which already has the correct message
								%msg = "<jl>" @ %Val1;
								DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
							}
							
							// REMOVED: ATKText for attacker is now handled by DisplayDamageMessage above
							// The following block was causing duplicate floating damage messages
							// if(!Player::isAiControlled(%shooterClient) && %shooterClient != "" && %shooterClient != -1 && %shooterClient != 0)
							if(false) // DISABLED
							{
								// Validate client is connected
								%clientName = Client::getName(%shooterClient);
								if(%clientName != "" && %clientName != -1)
								{
									%displayType = fetchData(%shooterClient, "damageDisplayType");
									%floatingEnabled = fetchData(%shooterClient, "floatingDamageNumbers");
									// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
									if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
									{
									// Get animation style preference
									%animationStyle = fetchData(%shooterClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "attacker" = player dealing damage (red), "defender" = player taking damage (current color)
									// Send text with tags - client will handle color conversion for attacker view
									// Remove justify tags but keep font color tags for critical hits
									%cleanVal1 = String::replace(%Val1, "<jc>", "");
									%cleanVal1 = String::replace(%cleanVal1, "<jr>", "");
									%cleanVal1 = String::replace(%cleanVal1, "<jl>", "");
									remoteEval(%shooterClient, "ATKText", %cleanVal1, %animationStyle, "attacker");
									}
								}
							}
						}
					
					if(%damagedDamageMode)
					{
						// Check for LCK hit first - use %Val2 which already has the correct LCK message
						if(%isLCKHit)
						{
							// Use %Val2 which was already constructed with "[attacker]'s attack hit you for LCK!" message
							%msg = "<jr>" @ %Val2;
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
						else if(%type == $SpellDamageType)
						{
							if(%criticalAttack)
							{
								%msg = "<jr>You were critically " @ %daction @ " by " @ %hitby @ " for " @ %convValue @ " points of damage!";
								DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
							}
							else
							{
								%msg = "<jr>You were " @ %daction @ " by " @ %hitby @ " for " @ %convValue @ " points of damage!";
								DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
							}
						}
						else
						{
							// For weapon attacks, use %Val2 which already has the correct message
							%msg = "<jr>" @ %Val2;
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
						
						// REMOVED: ATKText for defender is now handled by DisplayDamageMessage above
						// The following block was causing duplicate floating damage messages
						// if(!Player::isAiControlled(%damagedClient) && %damagedClient != "" && %damagedClient != -1 && %damagedClient != 0)
						if(false) // DISABLED
						{
							// Validate client is connected
							%clientName = Client::getName(%damagedClient);
							if(%clientName != "" && %clientName != -1)
							{
								%displayType = fetchData(%damagedClient, "damageDisplayType");
								%floatingEnabled = fetchData(%damagedClient, "floatingDamageNumbers");
								// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
								if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
								{
									// Get animation style preference
									%animationStyle = fetchData(%damagedClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "attacker" = player dealing damage (red), "defender" = player taking damage (current color)
									remoteEval(%damagedClient, "ATKText", "<jc>" @ %Val2, %animationStyle, "defender");
								}
							}
						}
					}

					//--------------------
					//display to radius
					//--------------------
					if(%shooterClient == 0)
					{
						if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%sname = $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "' in radius message");
								%sname = "A powerful force";
							}
						}
						else
							%sname = "A powerful force";
						// CRITICAL: Use GetClientOrBotName for consistent naming (especially for enemy bots)
						%dname = GetClientOrBotName(%damagedClient);
					}
					else if(%shooterClient == %damagedClient)
					{
						%sname = Client::getName(%shooterClient);
						if(String::ICompare(Client::getGender(%damagedClient), "Male") == 0)
							%dname = "himself";
						else if(String::ICompare(Client::getGender(%damagedClient), "Female") == 0)
							%dname = "herself";
						else
							%dname = "itself";
					}
					else
					{
						if(fetchData(%shooterClient, "invisible"))
							%sname = "An unknown assailant";
						else if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%sname = Client::getName(%shooterClient) @ "'s " @ $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "' in radius message, shooter=" @ %shooterClient);
								%sname = Client::getName(%shooterClient);
							}
						}
						else
							%sname = Client::getName(%shooterClient);
						
						// CRITICAL: Use GetClientOrBotName for consistent naming (especially for enemy bots)
						%dname = GetClientOrBotName(%damagedClient);
					}

					// Send radius messages only to players who don't have floating damage enabled
					// Get positions for radius check (validate clients first)
					%damagedPos = GameBase::getPosition(%damagedClient);
					%shooterPos = "";
					if(%shooterClient != 0 && %shooterClient != "" && %shooterClient != -1)
						%shooterPos = GameBase::getPosition(%shooterClient);
					else
						%shooterPos = %damagedPos; // Use damaged position as fallback
					
					%radiusMsg = "";
					// Check if LCK hit (initialize if not already set)
					if(%isLCKHit == "")
						%isLCKHit = false;
					if(%isLCKHit)
					{
						if(%criticalAttack)
							%radiusMsg = %sname @ " critically hits " @ %dname @ " for LCK!";
						else
							%radiusMsg = %sname @ " hits " @ %dname @ " for LCK!";
					}
					else
					{
						if(%criticalAttack)
							%radiusMsg = %sname @ " critically hits " @ %dname @ " for " @ %convValue @ " points of damage!";
						else
							%radiusMsg = %sname @ " hits " @ %dname @ " for " @ %convValue @ " points of damage!";
					}
					
					// Send chat messages only to players without floating damage enabled
					for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					{
						// Skip attacker, defender, and AI
						if(%cl == %shooterClient || %cl == %damagedClient || Player::isAiControlled(%cl))
							continue;
						
						// Skip if client is invalid or dead
						if(%cl == "" || %cl == -1 || %cl == 0 || IsDead(%cl))
							continue;
						
						// Check if within radius
						%clPos = GameBase::getPosition(%cl);
						%dist1 = Vector::getDistance(%clPos, %damagedPos);
						%dist2 = Vector::getDistance(%clPos, %shooterPos);
						if(%dist1 > $maxSAYdistVec && %dist2 > $maxSAYdistVec)
							continue; // Too far away
						
						// Check if this player has floating damage enabled - if so, skip chat message
						%displayType = fetchData(%cl, "damageDisplayType");
						%floatingEnabled = fetchData(%cl, "floatingDamageNumbers");
						if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
							continue; // Skip chat message, they'll get floating text instead
						
						// Send chat message to players without floating damage
						Client::sendMessage(%cl, $MsgBeige, %radiusMsg);
					}
					
					// Send ATKText to nearby players (spectators) if they have floating damage enabled
					// Build spectator message: "[attacker] hit [defender] for [damage]!"
					// Check if LCK hit (initialize if not already set)
					if(%isLCKHit == "")
						%isLCKHit = false;
					if(%isLCKHit)
					{
						%spectatorMsg = %sname @ " hit " @ %dname @ " for LCK!";
					}
					else
					{
						%spectatorMsg = %sname @ " hit " @ %dname @ " for " @ %convValue @ "!";
					}
					if(%criticalAttack)
						%spectatorMsg = "<f2>Critical hit! <f0>" @ %spectatorMsg;
					
					// Reuse positions from earlier (already validated)
					
					// Send to all nearby players (except attacker and defender)
					for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					{
						// Skip attacker, defender, and AI
						if(%cl == %shooterClient || %cl == %damagedClient || Player::isAiControlled(%cl))
							continue;
						
						// Skip if client is invalid
						if(%cl == "" || %cl == -1 || %cl == 0)
							continue;
						
						// Check if within radius
						%clPos = GameBase::getPosition(%cl);
						if(%clPos == "")
							continue; // Invalid position
						%dist1 = Vector::getDistance(%clPos, %damagedPos);
						%dist2 = Vector::getDistance(%clPos, %shooterPos);
						if(%dist1 > $maxSAYdistVec && %dist2 > $maxSAYdistVec)
							continue; // Too far away
						
						// Validate client is connected
						%clientName = Client::getName(%cl);
						if(%clientName == "" || %clientName == -1)
							continue;
						
						// Check if floating numbers are enabled
						%displayType = fetchData(%cl, "damageDisplayType");
						%floatingEnabled = fetchData(%cl, "floatingDamageNumbers");
						if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
						{
							// Get animation style preference
							%animationStyle = fetchData(%cl, "floatingAnimationStyle");
							if(%animationStyle == "")
								%animationStyle = "float"; // Default style
							// Convert old styles to new ones
							if(%animationStyle == "redmoon" || %animationStyle == "wow")
								%animationStyle = "float";
							// Send as spectator view (different position, white color)
							remoteEval(%cl, "ATKText", %spectatorMsg, %animationStyle, "spectator");
						}
					}
				}
				else if(%convValue < 0)
				{
					//this happens when there's a LCK consequence as miss

					if(%shooterClient == 0)
					{
						if(%type == $SpellDamageType && %weapon != "")
							%hitby = $Spell::name[$Spell::index[%weapon]];
						else
							%hitby = "A powerful force";
					}
					else
					{
						if(%type == $SpellDamageType && %weapon != "")
							%hitby = Client::getName(%shooterClient) @ "'s " @ $Spell::name[$Spell::index[%weapon]];
						else
							%hitby = Client::getName(%shooterClient);
					}

					// Default damageMode to true if not set (for backwards compatibility)
					%shooterDamageMode = fetchData(%shooterClient, "damageMode");
					if(%shooterDamageMode == "")
						%shooterDamageMode = true;
					%damagedDamageMode = fetchData(%damagedClient, "damageMode");
					if(%damagedDamageMode == "")
						%damagedDamageMode = true;
					
					if(%shooterDamageMode)
					{
						%msg = "<jl>You try to hit " @ Client::getName(%damagedClient) @ ", but miss! (LCK)";
						DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
						
						// Send ATKText for LCK miss to attacker (only if not AI controlled)
						// Check if floating numbers are enabled (either explicitly or via damageDisplayType)
						if(!Player::isAiControlled(%shooterClient) && %shooterClient != "" && %shooterClient != -1 && %shooterClient != 0)
						{
							// Validate client is connected
							%clientName = Client::getName(%shooterClient);
							if(%clientName != "" && %clientName != -1)
							{
								%displayType = fetchData(%shooterClient, "damageDisplayType");
								%floatingEnabled = fetchData(%shooterClient, "floatingDamageNumbers");
								// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
								if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
								{
									// Get animation style preference
									%animationStyle = fetchData(%shooterClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "attacker" = player dealing damage (red)
									%enemyName = Client::getName(%damagedClient);
									remoteEval(%shooterClient, "ATKText", "Your attack missed " @ %enemyName @ "!", %animationStyle, "attacker");
								}
							}
						}
					}
					if(%damagedDamageMode)
					{
						%msg = "<jr>" @ %hitby @ " tries to hit you, but misses! (LCK)";
						DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						
						// Send ATKText for LCK miss to defender (only if not AI controlled)
						// Check if floating numbers are enabled (either explicitly or via damageDisplayType)
						if(!Player::isAiControlled(%damagedClient) && %damagedClient != "" && %damagedClient != -1 && %damagedClient != 0)
						{
							// Validate client is connected
							%clientName = Client::getName(%damagedClient);
							if(%clientName != "" && %clientName != -1)
							{
								%displayType = fetchData(%damagedClient, "damageDisplayType");
								%floatingEnabled = fetchData(%damagedClient, "floatingDamageNumbers");
								// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
								if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
								{
									// Get animation style preference
									%animationStyle = fetchData(%damagedClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "defender" = player taking damage (current color)
									%attackerName = Client::getName(%shooterClient);
									remoteEval(%damagedClient, "ATKText", %attackerName @ "'s attack missed you!", %animationStyle, "defender");
								}
							}
						}
					}
				}

				//-------------------------------------------
				//add entry to damagedClient's damagedBy list
				//-------------------------------------------

				//make new entry with shooter's name
				// CRITICAL: Include LCK misses (%lckMiss) in $damagedBy so players get EXP credit
				// Even though LCK protection prevented the damage, the player still "hit" the enemy
				// and should get credit for the kill if the enemy dies from other damage
				if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Check Reg Block: Shooter=" @ %shooterClient @ " IsMiss=" @ %isMiss @ " Value=" @ %value @ " TargetDead=" @ %targetIsDead @ " ExpDist=" @ fetchData(%damagedClient, "ExpDistributed"));
				if( %shooterClient != 0 && !%isMiss)
				{
					// CRITICAL: Don't add damage to $damagedBy if the target is already dead
					// Damage events can arrive after death, and adding them would be pointless
					// Check if the target is dead or if EXP was already distributed
					%targetIsDead = Player::IsDead(%this);
					%expAlreadyDistributed = fetchData(%damagedClient, "ExpDistributed");
					%expDistributed = (%expAlreadyDistributed != "" && %expAlreadyDistributed != "0" && %expAlreadyDistributed != -1);
					
					if(%targetIsDead || %expDistributed)
					{
						// Target is dead or EXP already distributed - skip adding to $damagedBy
						// This prevents late-arriving damage events from being tracked after death
						if($DamageDebugEnabled) echo("[DAMAGE DEBUG] REJECTED: ExpAlreadyDistributed=" @ %expDistributed @ " Dead=" @ %targetIsDead @ " for " @ %damagedClient);
					}
					else
					{
					if(%shooterClient == 0)
						%sname = "A powerful force";
					else
						%sname = Client::getName(%shooterClient);

					// CRITICAL: Use BotInfoAiName for enemy bots, Client::getName() for regular players
					// This ensures $damagedBy is keyed by the correct name for EXP distribution
					%botInfoAiName = fetchData(%damagedClient, "BotInfoAiName");
					if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
						%dname = %botInfoAiName;
					else
						%dname = Client::getName(%damagedClient);
					
					if(%shooterClient != %damagedClient)
					{
						// First, check if this shooter already has an entry in $damagedBy
						// If they do, add the damage to their existing entry instead of creating a new one
						// This makes better use of the limited array space
						%existingIndex = "";
						for(%i = 1; %i <= $maxDamagedBy; %i++)
						{
							%entry = $damagedBy[%dname, %i];
							if(%entry != "")
							{
								%entryShooter = GetWord(%entry, 0);
								if(%entryShooter == %sname)
								{
									%existingIndex = %i;
									break;
								}
							}
						}
						
						if(%existingIndex != "")
						{
							// Shooter already has an entry - add damage to existing entry
							%existingEntry = $damagedBy[%dname, %existingIndex];
							%existingDamage = GetWord(%existingEntry, 1);
							
							// CRITICAL: Validate existing damage value - if invalid, use 0
							%existingDamageNum = %existingDamage * 1;
							if(%existingDamageNum == "" || %existingDamageNum == -1)
								%existingDamageNum = 0;
							
							%newDamage = %existingDamageNum + %backupValue;
							$damagedBy[%dname, %existingIndex] = %sname @ " " @ %newDamage;
							
							if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Updated " @ %sname @ " damage on " @ %dname @ " to " @ %newDamage @ " (Index: " @ %existingIndex @ ")");

							// CRITICAL: Reschedule the erase timer when updating existing entry
							// This ensures the entry doesn't get erased too early, especially for rapid attacks
							// Note: We can't cancel the old schedule, but scheduling a new one will extend the timer
							// The old schedule will try to clear an entry that may have been updated, which is harmless
							schedule("$damagedBy[\"" @ %dname @ "\", " @ %existingIndex @ "] = \"\";", $damagedByEraseDelay);
						}
						else
						{
							// Shooter doesn't have an entry yet - create a new one
							%index = "";
							for(%i = 1; %i <= $maxDamagedBy; %i++)
							{
								if($damagedBy[%dname, %i] == "" && %index == "")
									%index = %i;
							}
							if(%index != "")
							{
								$damagedBy[%dname, %index] = %sname @ " " @ %backupValue;
								if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Added " @ %sname @ " damage on " @ %dname @ ": " @ %backupValue @ " (Index: " @ %index @ ")");
								schedule("$damagedBy[\"" @ %dname @ "\", " @ %index @ "] = \"\";", $damagedByEraseDelay);
							}
							else
							{
								//too many hits on waiting list, he doesn't get in on exp.
								if($DamageDebugEnabled) echo("[DAMAGE DEBUG] FAILED to add " @ %sname @ " damage on " @ %dname @ " - List Full!");
							}
						}
					}
					}
				}

				if(fetchData(%damagedClient, "flashMode"))
				{
					%flash = Player::getDamageFlash(%this) + %value * 2;
					if(%flash > 0.75)
						%flash = 0.75;
					Player::setDamageFlash(%this,%flash);
				}

				//If player not dead then play a random hurt sound
				if(!Player::IsDead(%this))
				{
					if(%damagedClient.lastDamage < getSimTime())
					{
						%sound = radnomItems(3,injure1,injure2,injure3);
						playVoice(%damagedClient,%sound);
						%damagedClient.lastdamage = getSimTime() + 1.5;
					}
				}
				else		//player died
				{
					if(isRPGAI(%shooterClient))
					{
						RemotePlayAnim(%shooterClient, 8);
						PlaySound(RandomRaceSound(fetchData(%shooterClient, "RACE"), Taunt), %shooterClientPos);
					}

					if( Player::isCrouching(%this) )
					%curDie = $PlayerAnim::Crouching;
					else
					%curDie = radnomItems(3, $PlayerAnim::DieLeftSide, $PlayerAnim::DieChest, $PlayerAnim::DieForwardKneel);

					Player::setAnimation(%this, %curDie);

					if(%type == $ImpactDamageType && %object.clLastMount != "")
					%shooterClient = %object.clLastMount;

					Client::onKilled(%damagedClient, %shooterClient, %type);
				}  // line 1237 - closes else from line 1219
			}  // line 1238 - closes if(!Player::IsDead(%this)) from line 1209

			if(%isMiss)  // line 1240
			{
				if(fetchData(%damagedClient, "isBonused"))  // line 1242
				{
					GameBase::activateShield(%this, "0 0 1.57", 1.47);  // line 1244
					PlaySound(SoundHitShield, %damagedClientPos);  // line 1245
				}  // closes if(fetchData(%damagedClient, "isBonused")) from line 1243
			}  // closes if(%isMiss) from line 1241
		}  // closes if(%value) from line 859
	}  // closes if(!IsDead(%this)) from line 759

  // closes function Player::onDamage from line 440
function remoteKill(%clientId)
{
	dbecho($dbechoMode, "remoteKill(" @ %clientId @ ")");

	if(!$matchStarted)
		return;
	if(%clientId.isJailed || Duel::isDueling(%clientId))
		return;
	
	%player = Client::getOwnedObject(%clientId);
	if(%player != -1 && getObjectType(%player) == "Player" && !IsDead(%clientId))
	{
		storeData(%clientId, "LCK", 1, "dec");

		if(fetchData(%clientId, "LCK") >= 0)
			Client::sendMessage(%clientId, $MsgRed, "You have permanently lost an LCK point!");

		playNextAnim(%clientId);
		Player::kill(%clientId);
	}
}
