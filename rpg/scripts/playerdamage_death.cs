//============================================================================
// playerdamage_death.cs — split from playerdamage.cs (original lines 1-5, 306-398, 581-2924)
// Extracted 2026-07-17 at commit 4aa0a3d. Mechanical text move, no behavior change.
// $LOOTBAG_DEBUG flag + Client::onKilled + Game::clientKilled + Player::onKilled (kept whole).
// exec'd by playerdamage.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
$LOOTBAG_DEBUG = 0; // Toggle [DROP RATE DEBUG] messages in this file
// Helper function to get client ID from a Player object
// For real players, Player::getClient() returns their client ID
// For enemy bots, Player::getClient() returns -1, so we need to do a reverse lookup
// PHASE 2 FIX: Added reverse verification to prevent client ID misidentification
function Client::onKilled(%clientId, %killerId, %damageType)
{
	dbecho($dbechoMode, "Client::onKilled(" @ %clientId @ ", " @ %killerId @ ", " @ %damageType @ ")");

	//This function is NOT an event, it must be MANUALLY CALLED!
	//At this point, the client can still be queried for getItemCounts, but is not considered an object anymore.

	//we can award the other players exp
	if(!fetchData(%clientId, "noExperienceFlag"))
	{
		DistributeExpForKilling(%clientId);

		// Daily cull credit for the killshot player (DailyQuest.cs). Same
		// noExperienceFlag gate as exp: no-exp bots (quest/seal/etc.) never
		// pay daily credit. Victim must be an enemy bot, killer a real player.
		if(%killerId != "" && %killerId != -1 && %killerId != 0 && %killerId != %clientId)
		{
			if(!Player::isAiControlled(%killerId) && GetClientDataType(%clientId) == "enemybot")
				Daily::OnKill(%killerId, %clientId);
		}
	}

	// Weekly boss death (WeeklyBoss.cs). OUTSIDE the noExperienceFlag gate on
	// purpose: Weekly::Despawn strips the tag (and sets noExp) BEFORE its own
	// teardown kill, so a surviving tag here always means a genuine kill.
	if(fetchData(%clientId, "WeeklyBossTag") != "" && fetchData(%clientId, "WeeklyBossTag") != -1)
		Weekly::OnBossKilled(%clientId, %killerId);

	//The player with the killshot gets the official "kill"
	if(!IsInCommaList(fetchData(%killerId, "TempKillList"), Client::getName(%clientId)))
	storeData(%killerId, "TempKillList", AddToCommaList(fetchData(%killerId, "TempKillList"), Client::getName(%clientId)));

	// Environment/no-killer deaths (killerId 0/-1/"") must be checked FIRST:
	// the old != / == pair was exhaustive, so the "You were killed!" branch was
	// unreachable and environment kills printed "You were killed by !".
	if(%killerId == "" || %killerId == 0 || %killerId == -1)
	{
		Client::sendMessage(%clientId, 0, "You were killed!");
	}
	else if(%killerId == %clientId)
	{
		Client::sendMessage(%clientId, 0, "You killed yourself!");
	}
	else
	{
		//a human player (or bot) killed %clientId
		%n = Client::getName(%killerId);

		Client::sendMessage(%clientId, 0, "You were killed by " @ %n @ "!");

		//if(fetchData(%killerId, "bounty") == Client::getName(%clientId))
		//	storeData(%killerId, "bounty", fetchData(%clientId, "LVL") @ " !Q@W#E$R%T^Y&U*I(O)P");
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

	// BELT WEAPON death cleanup (BeltWeapons.cs): clear the equip binding
	// BEFORE the drop assembly below. The phantom shell mount has count 0,
	// so the assembly never sees it regardless - this just keeps state clean.
	// No-op for bots and players with no belt weapon equipped.
	BeltWeapon::OnDeath(%debugClientId);
	
	if($ONKILLED_DEBUG)
	{
		echo("[ONKILLED DEBUG] === Player::onKilled() ENTRY ===");
		echo("[ONKILLED DEBUG]   PlayerObject(%%this): " @ %playerObj);
		echo("[ONKILLED DEBUG]   Player::getClient(): " @ %clientIdFromGetClient);
		echo("[ONKILLED DEBUG]   GetClientIdFromPlayerObject(): " @ %clientIdFromHelper);
		echo("[ONKILLED DEBUG]   Player::isAiControlled(): " @ %isAiControlled);
		echo("[ONKILLED DEBUG]   Client::getName(): '" @ %nameFromClient @ "'");
		echo("[ONKILLED DEBUG]   BotInfoAiName (via clientId " @ %debugClientId @ "): '" @ %botInfoAiName @ "'");
		echo("[ONKILLED DEBUG]   SpawnBotInfo (via clientId " @ %debugClientId @ "): '" @ %spawnBotInfo @ "'");
	}
	
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
			Watchdog_Exit();
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

	// Cache bot-state early; cleanup later in this function can clear bot markers.
	// This prevents bot deaths from being misclassified as players for SaveWorld scheduling.
	%wasBotAtDeath = %isBot || %isAiControlled;
	if(!%wasBotAtDeath)
	{
		%botInfoAtDeath = fetchData(%clientId, "BotInfoAiName");
		%spawnInfoAtDeath = fetchData(%clientId, "SpawnBotInfo");
		if(Player::isAiControlled(%clientId) ||
		   (%botInfoAtDeath != "" && %botInfoAtDeath != -1 && %botInfoAtDeath != "0") ||
		   (%spawnInfoAtDeath != "" && %spawnInfoAtDeath != -1 && %spawnInfoAtDeath != "0"))
		{
			%wasBotAtDeath = true;
		}
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
						if($ONKILLED_DEBUG) echo("[DEBUG getItemCount] Player::onKilled - Player object deleted before weapon count check, clientId: " @ %clientId @ ", bot: " @ %botName @ ", weapon: " @ %eitem);
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
								
								// Case-insensitive comparison to handle weapon name inconsistencies (handles both string names and datablock IDs)
								if(String::ICompare(%origItem, %eitem) == 0 || (%eitem != "" && %eitem != -1 && String::ICompare(%origItem, Object::getName(%eitem)) == 0))
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

									// review #14: use >= 1000 to match the canonical "1 in X" cutoff
									// (rpgfunk.cs GiveThisStuff, and the generic-item + belt drop copies
									// below at the >= 1000 checks). This mounted-weapon copy alone used
									// > 100, so an identical loot % in 101-999 dropped at a totally
									// different rate for a weapon than for an item/belt entry.
									if(%percNum >= 1000)
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
			if($ONKILLED_DEBUG) echo("[DEBUG getItemCount] Player::onKilled - Player object doesn't exist before item loop, clientId: " @ %clientId @ ", bot: " @ %botName);

			// Emergency bot cleanup path:
			// If a bot reaches this return, we would otherwise skip the later
			// DecrementSpawnCounter/UnregisterBot path and leak spawn state.
			%emergencySpawnInfo = fetchData(%clientId, "SpawnBotInfo");
			%emergencyIsEnemyBot = (%emergencySpawnInfo != "" && %emergencySpawnInfo != "0" && %emergencySpawnInfo != -1);
			%emergencyIsBot = %wasBotAtDeath || %isBot || Player::isAiControlled(%clientId) || isRPGAI(%clientId) || %emergencyIsEnemyBot;
			if(%emergencyIsBot)
			{
				echo("WARNING: Player::onKilled - Missing player object before item loop for bot clientId " @ %clientId @ ". Running emergency DecrementSpawnCounter cleanup.");
				DecrementSpawnCounter(%clientId, %this);

				// Keep enemy-bot counters consistent with normal death path.
				if(%emergencyIsEnemyBot)
				{
					$ActiveEnemyBots--;
					$TotalActiveBots--;
					if($ActiveEnemyBots < 0)
						$ActiveEnemyBots = 0;
					if($TotalActiveBots < 0)
						$TotalActiveBots = 0;

					if($numAI > 0)
					{
						$numAI--;
						$Telemetry_NumAI_Dec++;
					}
					if($numAI < 0)
						$numAI = 0;

					Telemetry_RecordDeath();
				}
			}
			Watchdog_Exit();
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
				if($ONKILLED_DEBUG) echo("[DEBUG getItemCount] Player::onKilled - Player object deleted during item loop (first check), clientId: " @ %clientId @ ", bot: " @ %botName);
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
				if($ONKILLED_DEBUG) echo("[DEBUG getItemCount] Player::onKilled - Player object deleted during item loop, clientId: " @ %clientId @ ", bot: " @ %botName @ ", item: " @ %a);
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
							
							// Compare both direct names and resolved datablock IDs to name strings
							if(%origItem == %itemName || (%itemName != "" && %itemName != -1 && %origItem == Object::getName(%itemName)))
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
			if($LOOTBAG_DEBUG && %questItems != "" && %questItems != "0") echo("[LOOT DEBUG]   QuestItems: '" @ %questItems @ "'");
			if($LOOTBAG_DEBUG && %keyItems != "" && %keyItems != "0") echo("[LOOT DEBUG]   KeyItems: '" @ %keyItems @ "'");
			if($LOOTBAG_DEBUG && %consumables != "" && %consumables != "0") echo("[LOOT DEBUG]   Consumables: '" @ %consumables @ "'");
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
					TossLootbag(%clientId, %tmploot, 1, "*", 300, %this);
					if($LOOTBAG_DEBUG) echo("[LOOT DEBUG] TossLootbag returned for bot (clientId=" @ %clientId @ ")");
				}
			}
			else
			{
			%namelist = Client::getName(%clientId) @ ",";
			// DESIGN DECISION (2026-07-12): player death packs stay owner-locked forever.
			// %t=0 => TossLootbag never schedules the namelist->"*" unlock (and $LootbagPopTime
			// is -1, so no expiry pop either). Previously LCK>=0 unlocked after LVL*300s
			// (5-60 min) and LCK<0 after LVL*0.2s (5s+), letting anyone - including
			// Telekinesis auto-vacuum - take the pack. Luck no longer affects pack protection.
			TossLootbag(%clientId, %tmploot, 5, %namelist, 0, %this);
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
			
			// Performance: this block performs heavy storeData churn for enemy bots.
			// Lock routing to enemybot for the duration to avoid repeated type resolution.
			BeginStoreDataClientTypeOverride(%clientId, "enemybot");

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
			
			// Clear Seal Battle bot flags if this was a Seal Battle bot
			%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
			%hadSealBattleBotFlag = (%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1");
			if(%hadSealBattleBotFlag)
			{
				storeData(%clientId, "SealBattleBot", "");
				storeData(%clientId, "AImaxRangeOverride", ""); // Clear detection range override
			}
			
			// Clear Colloseum bot flags if this was a Colloseum bot
			%isColloseumBot = fetchData(%clientId, "ColloseumBot");
			if(%isColloseumBot == "true")
			{
				storeData(%clientId, "ColloseumBot", "");
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
			// Also clear the entry for the weapon that was actually mounted at death
			// (%eitem, captured in the loot section above - the guess-list can't cover
			// every custom weapon name). Empty when the no-drop path was taken.
			if(%eitem != "" && %eitem != -1)
				storeData(%clientId, "LoadedProjectile " @ %eitem, "");
			
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
			}

			// Seal battle bots are TempSpawn enemy bots, so they usually still have SpawnBotInfo here.
			// Use the pre-clear cached flag instead of refetching a value we already wiped above.
			if(%hadSealBattleBotFlag)
			{
				%weapon = Player::getMountedItem(%clientId, $WeaponSlot);
				if(%weapon != -1 && %weapon != "")
				{
					%weaponName = getCroppedItem(%weapon);
					$SealBattleWeaponDamage[%clientId, %weaponName] = "";
					if($SealBattleWeaponOwner[%weaponName] == %clientId)
						$SealBattleWeaponOwner[%weaponName] = "";
					echo("[SEAL BATTLE] Player::onKilled(): Cleared scaled weapon damage for Seal Battle Bot " @ %botInfoAiName @ " (weapon: " @ %weaponName @ ")");
				}
			}

			EndStoreDataClientTypeOverride(%clientId);
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
					DecrementSpawnCounter(%clientId, %this);
					
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
		
		// Drop the reverse-lookup cache entry for this player object - the object id
		// will be recycled by the engine and must not resolve to the old clientId
		$BotClientCache[%this] = "";

		schedule("deleteObject(" @ %this @ ");", $CorpseTimeoutValue + 2.5, %this);
		%clientId.observerMode = "dead";
		%clientId.dieTime = getSimTime();
		
		// Save character and world after death to prevent lootbag duplication on server crash
		// NOTE: SaveCharacter is already called immediately after clearing equipped items (see above)
		// This is just a backup save in case the first one didn't complete
		// Only save for players, not AI bots
		if(!%wasBotAtDeath && !isRPGAI(%clientId))
		{
			// Backup character save after 2 second delay (in case first save didn't complete)
			schedule("SaveCharacter(" @ %clientId @ ");", 2, %clientId);
			
			// Queue deployable-only world save after 3 seconds (preserves lootbag crash safety, avoids full-save storms)
			RequestWorldSave("player_death", 3, "deployables");
		}
	}
	
	Watchdog_Exit();
}

