//============================================================================
// playerdamage_util.cs — split from playerdamage.cs (original lines 6-305, 399-580)
// Extracted 2026-07-17 at commit 4aa0a3d. Mechanical text move, no behavior change.
// Shared helpers: id/name resolution, damage-text display.
// exec'd by playerdamage.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
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
			
			// Tier 2: Range loop fallback over the real BaseRep id pool (2049-2175;
			// 2048 is the server slot, ids above 2175 don't exist) - only if Tier 1 didn't find a match
			if(%foundCorrectId == -1)
			{
				for(%checkId = $BaseRepClientIdMin + 1; %checkId <= $BaseRepClientIdMax; %checkId++)
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
	
	// Tier 2: Range loop fallback over the real BaseRep id pool (2049-2175;
	// 2048 is the server slot, ids above 2175 don't exist) - only if Tier 1 didn't find a match
	if(%foundClientId == -1)
	{
		for(%checkId = $BaseRepClientIdMin + 1; %checkId <= $BaseRepClientIdMax; %checkId++)
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

// Erase a $damagedBy entry only if it hasn't been rewritten since the erase
// was scheduled. schedule() events can't be cancelled, and AI names are
// recycled within seconds of a bot dying (getAInumber hands out the lowest
// free number), so a blind '$damagedBy[name,i]=""' firing $damagedByEraseDelay
// later could wipe a DIFFERENT bot's live damage entry - and since the killing
// blow itself is never tracked (target is already dead by the time the
// tracking block runs), a wipe landing between the last hit and the kill paid
// zero exp. Each write stamps the entry with a fresh token; an erase carrying
// a stale token no-ops.
function DamagedByErase(%dname, %i, %token)
{
	if($damagedByStamp[%dname, %i] == %token)
	{
		$damagedBy[%dname, %i] = "";
		$damagedByStamp[%dname, %i] = "";
	}
}

// Compact verbose damage sentences into short floating text for the "pop" style
// "You hit X for 16364!" -> "16364"   "X's attack hit you for 16364!" -> "-16364"
// Critical hits -> "Crit! 16364"   Misses -> "Miss!" / "Dodged!"
// Unknown formats are returned unchanged.
function PopCompactDamageText(%msg, %viewType)
{
	// Strip font tags so parsing sees plain text
	%plain = %msg;
	%plain = String::replace(%plain, "<f0>", "");
	%plain = String::replace(%plain, "<f1>", "");
	%plain = String::replace(%plain, "<f2>", "");

	// Miss messages have no damage number
	if(String::findSubStr(%plain, ", but miss") != -1 || String::findSubStr(%plain, " missed ") != -1)
	{
		if(%viewType == "attacker")
			return "Miss!";
		return "Dodged!";
	}

	// Pull the amount after " for " (e.g. "16364!", "LCK!", "120 points of damage!")
	%pos = String::findSubStr(%plain, " for ");
	if(%pos == -1)
		return %msg; // Unknown format - leave unchanged
	%amount = String::getSubStr(%plain, %pos + 5, 99999);
	%amount = String::replace(%amount, " points of damage", "");
	%amount = String::replace(%amount, "!", "");
	if(%amount == "")
		return %msg;

	%prefix = "";
	if(String::findSubStr(%plain, "Bashed!") != -1 || String::findSubStr(%plain, "bashed") != -1)
		%prefix = "Bash! ";
	else if(String::findSubStr(%plain, "Pierced!") != -1 || String::findSubStr(%plain, "pierced") != -1)
		%prefix = "Pierce! ";
	else if(String::findSubStr(%plain, "Cleaved!") != -1 || String::findSubStr(%plain, "cleaved") != -1)
		%prefix = "Cleave! ";

	// Defender sees damage as negative (LCK has no number, so no minus sign)
	if(%viewType == "defender" && %amount != "LCK")
		%amount = "-" @ %amount;

	if(String::findSubStr(%plain, "Critical") != -1 || String::findSubStr(%plain, "critically") != -1)
		return "<f2>Crit! " @ %prefix @ %amount;
	return %prefix @ %amount;
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

		// Nameplate style: damage you deal shows on the KronosHUD target
		// frame instead of floating text, so attacker messages are
		// suppressed - except misses, which the target frame can't show.
		// Incoming damage still floats (pop animation, falls downward).
		if(%animationStyle == "nameplate")
		{
			%cleanMsg = PopCompactDamageText(%cleanMsg, %viewType);
			if(%viewType == "attacker" && %cleanMsg != "Miss!")
				return;
			// send the REAL style so KronosHUD can render nameplate-mode
			// taken damage differently (sinks downward vs pop's rise);
			// stock ATKText treats "nameplate" identically to "pop", so
			// clients without the HUD are unaffected
			remoteEval(%clientId, "ATKText", %cleanMsg, "nameplate", %viewType);
			return;
		}

		// The pop style shows short numbers instead of full sentences
		if(%animationStyle == "pop")
			%cleanMsg = PopCompactDamageText(%cleanMsg, %viewType);

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

