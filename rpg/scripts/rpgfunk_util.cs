//============================================================================
// rpgfunk_util.cs — split from rpgfunk.cs (scatter split, Phase 2)
// Extracted 2026-07-18 at commit 5118b1b. Mechanical text move, no behavior change.
// string/number/list/display leaf utilities + debug flags (exec FIRST)
// Source line ranges listed in the rpgfunk.cs shell tombstone.
// exec'd by rpgfunk.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
$INVISIBILITY_DEBUG = 0; // Toggle [INVISIBILITY DEBUG] messages - set to 1 to diagnose invisible bots
$LOOTBAG_DEBUG = 0; // Toggle [LOOTBAG DEBUG] messages in this file
$LOADCHAR_DEBUG = 0; // Toggle per-field RAW/FINAL echoes in LoadCharacter.
function String::len(%string)
{
	//dbecho($dbechoMode, "String::len(" @ %string @ ")");

	%chunk = 10;
	%length = 0;

	for(%i = 0; String::getSubStr(%string, %i, 1) != ""; %i += %chunk)
		%length += %chunk;
	%length -= %chunk;

	%checkstr = String::getSubStr(%string, %length, 99999);
	for(%k = 0; String::getSubStr(%checkstr, %k, 1) != ""; %k++)
		%length++;

	if(%length == -%chunk)
		%length = 0;

	return %length;
}

function String::replace(%string, %search, %replace)
{
	dbecho($dbechoMode, "String::replace(" @ %string @ ", " @ %search @ ", " @ %replace @ ")");

	%loc = String::findSubStr(%string, %search);

	if(%loc != -1)
	{
		%ls = String::len(%search);

		%part1 = String::NEWgetSubStr(%string, 0, %loc);
		%part2 = String::NEWgetSubStr(%string, %loc + %ls, 99999);

		%string = %part1 @ %replace @ %part2;
	}

	return %string;
}

function String::create(%c, %len)
{
	dbecho($dbechoMode, "String::create(" @ %c @ ", " @ %len @ ")");

	%f = "";
	for(%i = 1; %i <= %len; %i++)
		%f = %f @ %c;

	return %f;
}

function String::ofindSubStr(%s, %f, %o)
{
	dbecho($dbechoMode, "String::ofindSubStr(" @ %s @ ", " @ %f @ ", " @ %o @ ")");

	%ns = String::NEWgetSubStr(%s, %o, 99999);
	return String::findSubStr(%ns, %f);
}

function String::toLower(%string)
{
	// Convert string to lowercase
	%len = String::len(%string);
	%result = "";
	
	// Character mapping for A-Z to a-z
	%upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	%lower = "abcdefghijklmnopqrstuvwxyz";
	
	for(%i = 0; %i < %len; %i++)
	{
		%char = String::getSubStr(%string, %i, 1);
		%pos = String::findSubStr(%upper, %char);
		if(%pos != -1)
			%char = String::getSubStr(%lower, %pos, 1);
		%result = %result @ %char;
	}
	
	return %result;
}

// Console command wrapper for String::toLower
// Allows players to call toLower() from console without "Unknown command" error
function toLower(%string)
{
	return String::toLower(%string);
}

function String::ICompare(%string1, %string2)
{
	// Case-insensitive string comparison
	// Returns 0 if strings are equal (case-insensitive), non-zero otherwise
	%str1 = String::toLower(%string1);
	%str2 = String::toLower(%string2);
	
	if(%str1 == %str2)
		return 0;
	else
		return 1;
}

// Number formatting helper
// Returns abbreviated format: 1.5k, 2.5m, 1.2b
// BUGFIX: whole-number quotients (25000/1000 = "25", no decimal point) used to hit
// findSubStr(".") == -1 and truncate to ONE character - the banker menu showed
// 25,000 coins as "2k" and 150,000 as "1k". Handle the no-decimal case explicitly.
function numFormat_trim(%val)
{
	%dotPos = String::findSubStr(%val, ".");
	if(%dotPos == -1)
		return %val; // Whole number - use as-is
	%disp = String::getSubStr(%val, 0, %dotPos + 2);
	if(String::getSubStr(%disp, String::len(%disp)-1, 1) == "0")
		%disp = String::getSubStr(%disp, 0, String::len(%disp)-2); // Remove .0
	return %disp;
}
function numFormat(%num)
{
	if(%num < 1000) return %num;

	if(%num < 1000000)
		return numFormat_trim(%num / 1000) @ "k";

	if(%num < 1000000000)
		return numFormat_trim(%num / 1000000) @ "m";

	return numFormat_trim(%num / 1000000000) @ "b";
}

// Wrapper function for Player::getItemCount with debug logging
// This helps track where "could not find player" errors are coming from
// Usage: Replace Player::getItemCount(%clientId, %item) with SafeGetItemCount(%clientId, %item, "FunctionName")
function SafeGetItemCount(%clientId, %item, %callerFunction)
{
	// Validate client ID
	if(%clientId == -1 || %clientId == "" || %clientId == "0")
	{
		echo("[DEBUG getItemCount] ERROR - Invalid clientId: '" @ %clientId @ "', item: '" @ %item @ "', called from: " @ %callerFunction);
		return 0;
	}
	
	// Check belt system first (migrated accessories, etc)
	if(isBeltItem(%item))
	{
		return Belt::HasThisStuff(%clientId, %item);
	}

	// Validate player object exists before calling Player::getItemCount
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Silently return 0 if player object is missing (common during spawn/respawn)
		return 0;
	}
	
	// NOTE: Removed isObject() check - it incorrectly rejects valid ItemData datablocks
	// ItemData objects like "Tool", "Blaster" etc. are valid but isObject() returns false for them
	// The engine's Player::getItemCount will handle invalid items gracefully

	// Player object exists - call the actual engine function
	%result = Player::getItemCount(%clientId, %item);
	return %result;
}

//=============================================================================
// END LOOTBAG AGGREGATION SYSTEM
//=============================================================================

function NEWgetClientByName(%name)
{
	dbecho($dbechoMode, "NEWgetClientByName(" @ %name @ ")");

	%list = GetEveryoneIdList();
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		%displayName = Client::getName(%id);
		// PERF: exact-match fast path first - String::ICompare lowercases both
		// strings char-by-char, and this function runs in spawn-hot paths
		if(%name == %displayName)
			return %id;
		if(String::ICompare(%name, %displayName) == 0)
			return %id;
	}
	return -1;
}

// Extract string by removing trailing digits (more reliable than clipTrailingNumbers)
// This works backwards from the end to find and remove only trailing digits
function StripTrailingDigits(%string)
{
	if(%string == "" || %string == -1)
		return %string;
	
	// Extract string by removing trailing digits (more reliable than clipTrailingNumbers)
	// This works backwards from the end to find and remove only trailing digits
	%result = %string;
	%len = String::len(%string);
	%numStr = "";
	%digitString = "0123456789";
	
	// Find trailing digits (working backwards)
	for(%i = %len - 1; %i >= 0; %i--)
	{
		%char = String::getSubStr(%string, %i, 1);
		// Check if character is a digit (0-9 only) - use String::findSubStr to avoid TorqueScript == comparison bug ("E" == "0" evaluates to true)
		if(String::findSubStr(%digitString, %char) != -1)
		{
			%numStr = %char @ %numStr;
		}
		else
		{
			break;
		}
	}
	
	// If we found trailing digits, remove them
	if(%numStr != "")
	{
		%result = String::getSubStr(%string, 0, %len - String::len(%numStr));
	}
	
	return %result;
}

function clipTrailingNumbers(%str)
{
	dbecho($dbechoMode, "clipTrailingNumbers(" @ %str @ ")");
	
	// Use the new robust StripTrailingDigits function
	return StripTrailingDigits(%str);
}

function GetEveryoneIdList()
{
	dbecho($dbechoMode, "GetEveryoneIdList()");

	// Use engine-native BaseRep::getFirst()/getNext() which includes BOTH players AND AI bots
	// This is much more efficient than combining Client::getFirst() with brute-force bot searches
	// From Tribes 1 engine source: "Like Client::getFirst(), but AI 'reps' are included."
	%list = "";
	
	for(%id = BaseRep::getFirst(); %id != -1; %id = BaseRep::getNext(%id))
	{
		%list = %list @ %id @ " ";
	}
	
	return Trim(%list);
}
function GetEveryoneNameList()
{
	dbecho($dbechoMode, "GetEveryoneNameList()");

	%list = "";
	%list = %list @ GetPlayerNameList();
	%list = %list @ GetBotNameList();
	return %list;
}

function GetBotIdList()
{
	// WATCHDOG: Track this function for freeze detection
	Watchdog_Enter("GetBotIdList");
	
	dbecho($dbechoMode, "GetBotIdList()");

	// Use engine-native BaseRep::getFirst()/getNext() which includes BOTH players AND AI bots
	// Then filter for bots using Player::isAiControlled() and isRPGAI()
	// This is much more efficient than our previous multi-method brute-force approach
	%list = "";
	%botsFound = 0;
	
	for(%id = BaseRep::getFirst(); %id != -1; %id = BaseRep::getNext(%id))
	{
		// Check if this is a bot using multiple methods
		%isBot = false;
		
		// Method 1: Engine-level AI check
		if(Player::isAiControlled(%id))
			%isBot = true;
		
		// Method 2: Script-level RPGAI check (checks bot data arrays)
		if(!%isBot && isRPGAI(%id))
			%isBot = true;
		
		// Method 3: Direct BotInfoAiName check for newly spawned bots
		if(!%isBot)
		{
			%botInfoAiName = $EnemyBotData[%id, "BotInfoAiName"];
			if(%botInfoAiName == "") %botInfoAiName = $TownBotData[%id, "BotInfoAiName"];
			if(%botInfoAiName == "") %botInfoAiName = $BotInfoAiName[%id];
			if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
				%isBot = true;
		}
		
		if(%isBot)
		{
			%list = %list @ %id @ " ";
			%botsFound++;
		}
	}
	
	if($GETBOTID_DEBUG) echo("[GETBOTIDLIST DEBUG] Using BaseRep iteration, found " @ %botsFound @ " bots");
	
	Watchdog_Exit();
	return Trim(%list);
}

function GetBotNameList()
{
	dbecho($dbechoMode, "GetBotNameList()");

	%list = "";

	%tempSet = nameToID("MissionCleanup");
	if(%tempSet != -1)
	{
		%num = Group::objectCount(%tempSet);
		for(%i = 0; %i <= %num-1; %i++)
		{
			%tempItem = Group::getObject(%tempSet, %i);
			if(getObjectType(%tempItem) == "Player")
			{
				%clientId = Player::getClient(%tempItem);
				
				// For enemy bots, Player::getClient() returns -1
				// We need to do a reverse lookup to find the client ID
				if(%clientId == -1 || %clientId == "")
				{
					// Search for the client ID that owns this Player object (reverse lookup)
					// Use Client::getFirst()/getNext() instead of brute-force range loop
					// This is more reliable and matches the pattern used in #listallclients and SpawnAIGetClientId()
					%botClientId = "";
					for(%checkId = Client::getFirst(); %checkId != -1; %checkId = Client::getNext(%checkId))
					{
						%ownedPlayerObj = Client::getOwnedObject(%checkId);
						if(%ownedPlayerObj == %tempItem)
						{
							%botClientId = %checkId;
							break;
						}
					}
					
					if(%botClientId != "" && %botClientId != -1)
					{
						%clientId = %botClientId; // Found client ID for enemy bot
					}
					else
					{
						// Not an enemy bot - skip this entry
						continue;
					}
				}
				
				if(Player::isAiControlled(%clientId))
				{
					//%list = %list @ Client::getName(%clientId) @ " ";
					%botName = fetchData(%clientId, "BotInfoAiName");
					// Only add to list if BotInfoAiName is valid (not empty, -1, or "0")
					if(%botName != "" && %botName != -1 && %botName != "0")
						%list = %list @ %botName @ " ";
				}
			}
		}
	}

	return %list;
}
function GetPlayerIdList()
{
	dbecho($dbechoMode, "GetPlayerIdList()");

	%list = "";
	for(%c = Client::getFirst(); %c != -1; %c = Client::getNext(%c))
	{
		%list = %list @ %c @ " ";
	}
	return %list;
}
function GetPlayerNameList()
{
	dbecho($dbechoMode, "GetPlayerNameList()");

	%list = "";
	for(%c = Client::getFirst(); %c != -1; %c = Client::getNext(%c))
	{
		%list = %list @ Client::getName(%c) @ " ";
	}
	return %list;
}

function FindInvalidChar(%name)
{
	dbecho($dbechoMode, "FindInvalidChar(" @ %name @ ")");

	//looks for invalid characters in player's name
	for(%a = 1; %a <= String::len($invalidChars); %a++)
	{
		%b = String::getSubStr($invalidChars, %a-1, 1);
		if(String::findSubStr(%name, %b) != -1)
		{
			return %a-1;
		}
	}
	return "";
}

function CheckForReservedWords(%name)
{
	dbecho($dbechoMode, "CheckForReservedWords(" @ %name @ ")");

	%w[%c++] = "ArenaGladiator";
	%w[%c++] = "Traveller";
	%w[%c++] = "Goblin";
	%w[%c++] = "Gnoll";
	%w[%c++] = "Orc";
	%w[%c++] = "Ogre";
	%w[%c++] = "Elf";
	%w[%c++] = "Undead";
	%w[%c++] = "Minotaur";
	// PROTECTION FIX: block ALL enemy-race name prefixes/substrings. The bot-detection
	// fallbacks (HasEnemyBotNamePrefix, UpdateTeam's name check, isEnemyBot's display
	// name check) classify names containing these as BOTS - a new player named
	// "Godzilla"/"Demonic"/"Sealion" would be bot-classified before their first save
	// exists, which is exactly when the save-file safeguard can't protect them.
	%w[%c++] = "Demon";
	%w[%c++] = "God";
	%w[%c++] = "Angel";
	%w[%c++] = "Alien";
	%w[%c++] = "Zombie";
	%w[%c++] = "Void";
	%w[%c++] = "Pigman";
	%w[%c++] = "Pigmen";
	%w[%c++] = "Enemy";
	%w[%c++] = "Seal";
	%w[%c++] = "Admin";
	// Colloseum bot display names (substring match to catch variations)
	%w[%c++] = "RoundOne";
	%w[%c++] = "RoundTwo";
	%w[%c++] = "RoundThree";
	%w[%c++] = "NewbieRound";
	%w[%c++] = "AdventurerRound";
	%w[%c++] = "SoldierRound";
	%w[%c++] = "GladiatorRound";
	%w[%c++] = "StarRound";
	%w[%c++] = "SuperStarRound";
	%w[%c++] = "TitanRound";
	%w[%c++] = "DemiGodRound";
	%w[%c++] = "GodRound";
	%w[%c++] = "ImmortalRound";
	%w[%c++] = "TranscendentRound";
	// Legacy numeric round names (for backwards compatibility)
	%w[%c++] = "Round1";
	%w[%c++] = "Round2";
	%w[%c++] = "Round3";
	// Seal Battle bot display names (substring match to catch variations)
	%w[%c++] = "SealFighter";
	%w[%c++] = "SealMage";
	%w[%c++] = "SealGuardian";
	// Arena/Event bot display names (substring match to catch variations)
	%w[%c++] = "Emperor";
	%w[%c++] = "General";
	%w[%c++] = "Lord";
	%w[%c++] = "EliteGuard";
	%w[%c++] = "Guard";

	//exact words
	%ew[%d++] = "rpgfunk";
	%ew[%d++] = "crystal";
	%ew[%d++] = "game";
	%ew[%d++] = "item";
	%ew[%d++] = "mine";
	%ew[%d++] = "vehicle";
	%ew[%d++] = "comchat";
	%ew[%d++] = "server";
	%ew[%d++] = "turret";
	%ew[%d++] = "player";
	%ew[%d++] = "observer";
	%ew[%d++] = "ai";
	%ew[%d++] = "client";
	%ew[%d++] = "station";
	%ew[%d++] = "admin";
	%ew[%d++] = "staticshape";
	%ew[%d++] = "armordata";
	%ew[%d++] = "baseexpdata";
	%ew[%d++] = "baseprojdata";
	%ew[%d++] = "clientdefaults";
	%ew[%d++] = "nsound";
	%ew[%d++] = "shopping";
	%ew[%d++] = "zone";
	%ew[%d++] = "specialarmors";
	%ew[%d++] = "accessory";
	%ew[%d++] = "enemyarmors";
	%ew[%d++] = "spawn";
	%ew[%d++] = "registerobjects";
	%ew[%d++] = "registeruserobjects";
	%ew[%d++] = "tsdefaultmatprops";
	%ew[%d++] = "rpgstats";
	%ew[%d++] = "classes";
	%ew[%d++] = "weapons";
	%ew[%d++] = "globals";
	%ew[%d++] = "humanarmors";
	%ew[%d++] = "remote";
	%ew[%d++] = "playerspawn";
	%ew[%d++] = "gameevents";
	%ew[%d++] = "connectivity";
	%ew[%d++] = "playerdamage";
	%ew[%d++] = "economy";
	%ew[%d++] = "itemevents";
	%ew[%d++] = "weaponhandling";
	%ew[%d++] = "depbase";
	%ew[%d++] = "weight";
	%ew[%d++] = "mana";
	%ew[%d++] = "hp";
	%ew[%d++] = "rpgarena";
	%ew[%d++] = "ferry";
	%ew[%d++] = "spells";
	%ew[%d++] = "skills";
	%ew[%d++] = "serverdefaults";
	%ew[%d++] = "sleep";
	%ew[%d++] = "plugs";
	%ew[%d++] = "editorconfig";
	%ew[%d++] = "worlds";
	%ew[%d++] = "changemission";
	%ew[%d++] = "commander";
	%ew[%d++] = "editmission";
	%ew[%d++] = "gui";
	%ew[%d++] = "interiorlight";
	%ew[%d++] = "ircclient";
	%ew[%d++] = "med";
	%ew[%d++] = "missionlist";
	%ew[%d++] = "missiontypes";
	%ew[%d++] = "newmission";
	%ew[%d++] = "sae";
	%ew[%d++] = "playersetup";
	%ew[%d++] = "registervolume";
	%ew[%d++] = "ted";
	%ew[%d++] = "trees";
	%ew[%d++] = "trigger";
	%ew[%d++] = "basedebrisdata";
	%ew[%d++] = "beacon";
	%ew[%d++] = "chatmenu";
	%ew[%d++] = "clientdefaults";
	%ew[%d++] = "dm";
	%ew[%d++] = "editor";
	%ew[%d++] = "keys";
	%ew[%d++] = "loadshow";
	%ew[%d++] = "marker";
	%ew[%d++] = "menu";
	%ew[%d++] = "mission";
	%ew[%d++] = "move";
	%ew[%d++] = "moveable";
	%ew[%d++] = "options";
	%ew[%d++] = "sensor";
	%ew[%d++] = "sound";
	%ew[%d++] = "tag";
	%ew[%d++] = "terrains";
	%ew[%d++] = "objectives";
	%ew[%d++] = "tmpPrize";
	%ew[%d++] = "all";

	for(%i = 1; %w[%i] != ""; %i++)
	{
		if(String::findSubStr(%name, %w[%i]) != -1)
			return %w[%i];
	}
	for(%i = 1; %ew[%i] != ""; %i++)
	{
		if(String::ICompare(%name, %ew[%i]) == 0)
			return %ew[%i];
	}

	%list = GetBotNameList();
	for(%i = 0; (%b = GetWord(%list, %i)) != -1; %i++)
	{
		if(String::findSubStr(%name, %b) != -1)
			return %b;
	}

	return "";
}

function CheckForProtectedWords(%string)
{
	dbecho($dbechoMode, "CheckForProtectedWords(" @ %string @ ")");

	//this function checks for words that shouldn't be used in the #if statement due to its extremely powerful nature
	%w[1] = "Admin";
	%w[2] = "ResetPlayer";
	%w[3] = "storedata";
	%w[4] = "down";
	%w[5] = "quit";
	%w[6] = "eval";
	
	for(%i = 1; %w[%i] != ""; %i++)
	{
		if(String::findSubStr(%string, %w[%i]) != -1)
			return %w[%i];
	}

	return "";
}

function RandomPositionXY(%minrad, %maxrad)
{
	dbecho($dbechoMode, "RandomPositionXY(" @ %minrad @ ", " @ %maxrad @ ")");

	%diff = %maxrad - %minrad;

	%tmpX = floor(getRandom() * (%diff*2)) - %diff;
	if(%tmpX < 0)
		%tmpX -= %minrad;
	else
		%tmpX += %minrad;

	%tmpY = floor(getRandom() * (%diff*2)) - %diff;
	if(%tmpY < 0)
		%tmpY -= %minrad;
	else
		%tmpY += %minrad;

	return %tmpX @ " " @ %tmpY @ " ";
}

function OddsAre(%n)
{
	dbecho($dbechoMode, "OddsAre(" @ %n @ ")");

	%a = floor(getRandom() * %n);
	if(%a == %n-1)
		return True;
	else
		return False;
}


// ECON-FIX 2026-07-18: the engine's floor() is an int32 cast (console.cpp
// sprintf "%d") - any value at/past 2^31 wraps to -2147483648. Balances
// (COINS/BANK/EXP) and top-tier item costs legitimately exceed that. Above the
// guard threshold the engine's %g stringification (6 sig figs) is already
// integer-valued, so truncation is a numeric no-op and we return the value
// untouched instead of wrapping it. Use this instead of floor() for anything
// coin/EXP-scale.
function SafeFloor(%n)
{
	if(%n >= 2000000000 || %n <= -2000000000)
		return %n;
	return floor(%n);
}

function round(%n)
{
//	dbecho($dbechoMode, "round(" @ %n @ ")");

	// ECON-FIX 2026-07-18: guard the int32 floor() wrap (see SafeFloor above).
	// At this magnitude %g values are integer-valued; rounding is a no-op.
	if(%n >= 2000000000 || %n <= -2000000000)
		return %n;

	if(%n < 0)
	{
		%t = -1;
		%n = -%n;
	}	
	else if(%n >= 0)
		%t = 1;

	%f = floor(%n);
	%a = %n - %f;
	if(%a < 0.5)
		%b = 0;
	else if(%a >= 0.5)
		%b = 1;

	return (%f + %b) * %t;
}

function SetStuffString(%stuff, %item, %amount)
{
	dbecho($dbechoMode, "SetStuffString(" @ %stuff @ ", " @ %item @ ", " @ %amount @ ")");

	//replaces both Add and Remove stuff string functions by enabling negative values for %amount

	// CRITICAL: Handle uninitialized BankStorage that may come in as 0 or "0"
	// This can happen when $ClientData[clientId, "BankStorage"] was never set
	// Use explicit string comparison to avoid TorqueScript's loose type coercion
	%stuffStr = %stuff @ "";  // Force string conversion
	if(%stuffStr == "0" || %stuffStr == "")
		%stuff = "";

	//echo("DEBUG SetStuffString: INPUT - stuff='" @ %stuff @ "', item='" @ %item @ "', amount=" @ %amount);
	%stuff = FixStuffString(%stuff);
	//echo("DEBUG SetStuffString: AFTER FixStuffString - stuff='" @ %stuff @ "'");

	%searchStr = " " @ %item @ " ";
	%pos = String::findSubStr(%stuff, %searchStr);
	//echo("DEBUG SetStuffString: Search for '" @ %searchStr @ "' found at position " @ %pos);

	if(%pos != -1)
	{
		%a = String::NEWgetSubStr(%stuff, %pos+1, 99999);
		%amt = GetWord(%a, 1);	//getword 0 would be the item, so getword 1 is the amount (which follows the item)
		//echo("DEBUG SetStuffString: Found item, substring after item='" @ %a @ "', extracted count='" @ %amt @ "'");
		
		// Validate extracted count - if it's "0", empty, or not a valid number, the string is corrupted
		// Try to find the actual count by looking for the next word that's a number
		%amtNum = %amt * 1;
		
		// CRITICAL FIX: Use String::findSubStr to safely check for invalid values like "False" without triggering TorqueScript boolean evaluation issues
		// Also strict check against 0/empty/invalid
		if(%amt == "" || %amt == -1 || String::findSubStr(%amt, "False") != -1 || String::findSubStr(%amt, "Msg") != -1 || (%amtNum == 0 && %amt != "0"))
		{
			//echo("DEBUG SetStuffString: WARNING - Extracted count '" @ %amt @ "' is invalid! String may be corrupted. Trying to find actual count...");
			// Try to get the NEXT word as the count
			// BUGFIX: this retry previously re-read GetWord(%a, 1) - the same word it
			// just rejected - so the advertised recovery could never succeed
			%amt = GetWord(%a, 2);
			%amtNum = %amt * 1;
			if(%amt == "" || %amt == -1 || String::findSubStr(%amt, "False") != -1 || String::findSubStr(%amt, "Msg") != -1 || (%amtNum == 0 && %amt != "0"))
			{
				echo("DEBUG SetStuffString: ERROR - Cannot find valid count for item '" @ %item @ "' in corrupted string '" @ %stuff @ "'. Skipping operation.");
				return %stuff; // Return original string unchanged if we can't parse it
			}
		}

		%part1 = String::NEWgetSubStr(%stuff, 0, %pos+1);
		%skipLen = %pos+String::len(%item)+String::len(%amt)+3;
		%part2 = String::NEWgetSubStr(%stuff, %skipLen, 99999);
		//echo("DEBUG SetStuffString: part1='" @ %part1 @ "', skipLen=" @ %skipLen @ ", part2='" @ %part2 @ "'");

		%b = %amt + %amount;
		//echo("DEBUG SetStuffString: count calculation: " @ %amt @ " + " @ %amount @ " = " @ %b);
		
		if(%b <= 0)
		{
			%part3 = "";
			//echo("DEBUG SetStuffString: Removing item (count <= 0)");
		}
		else
		{
			%part3 = %item @ " " @ %b @ " ";
			//echo("DEBUG SetStuffString: Keeping item with new count, part3='" @ %part3 @ "'");
		}

		%final = %part1 @ %part2 @ %part3;
		//echo("DEBUG SetStuffString: FINAL result='" @ %final @ "'");
	}
	else
	{
		%final = %stuff @ %item @ " " @ %amount @ " ";
		//echo("DEBUG SetStuffString: Item not found, adding new item, FINAL='" @ %final @ "'");
	}

	// Normalize result: trim leading/trailing spaces and convert " " or "0" to empty string
	%final = FixStuffString(%final);
	%len = String::len(%final);
	// Remove trailing space if present
	if(%len > 0 && String::getSubStr(%final, %len-1, 1) == " ")
		%final = String::getSubStr(%final, 0, %len-1);
	// Remove leading space if present
	if(%len > 0 && String::getSubStr(%final, 0, 1) == " ")
		%final = String::getSubStr(%final, 1, 99999);
	// Normalize "0" or empty/whitespace-only strings to empty string
	if(%final == "0" || %final == " " || %final == "")
		%final = "";
	
	//echo("DEBUG SetStuffString: NORMALIZED result='" @ %final @ "'");
	return %final;
}

function GetStuffStringCount(%stuff, %item)
{
	dbecho($dbechoMode, "GetStuffStringCount(" @ %stuff @ ", " @ %item @ ")");

	%stuff = FixStuffString(%stuff);

	%pos = String::findSubStr(%stuff, " " @ %item @ " ");

	if(%pos != -1)
	{
		%a = String::NEWgetSubStr(%stuff, %pos+1, 99999);
		%amt = GetWord(%a, 1);

		return %amt;
	}

	return 0;
}

function FixStuffString(%stuff)
{
	dbecho($dbechoMode, "FixStuffString(" @ %stuff @ ")");

	%nstuff = " ";
	for(%i = 0; GetWord(%stuff, %i) != -1; %i++)
	{
		%w = GetWord(%stuff, %i);
		%nstuff = %nstuff @ %w @ " ";
	}

	return %nstuff;
}

function IsStuffStringEquiv(%s1, %s2, %dblCheck)
{
	dbecho($dbechoMode, "IsStuffStringEquiv(" @ %s1 @ ", " @ %s2 @ ", " @ %dblCheck @ ")");

	//this function COULD be laggy, it all depends on how many items are in %s1.  Below 5, IMO, should be just fine

	%s1 = " " @ %s1;
	%s2 = " " @ %s2;
	for(%x = 0; (%w = GetWord(%s1, %x)) != -1; %x+=2)
	{
		%w2 = GetWord(%s1, %x+1);

		if(String::findSubStr(%s2, " " @ %w @ " " @ %w2) == -1)
			return False;
	}
	if(%x == 0)			//do a dblCheck if %s1 is null.
		%dblCheck = True;

	if(%dblCheck)
	{
		//This will slow down the function, but will get a more accurate reading.
		//If you do NOT do a dblCheck, then %s2 could contain additional items that %s1 does not contain, and still
		//return True.  If this is not a concern, then you don't have to do a dblCheck
		for(%x = 0; (%w = GetWord(%s2, %x)) != -1; %x+=2)
		{
			%w2 = GetWord(%s2, %x+1);
	
			if(String::findSubStr(%s1, " " @ %w @ " " @ %w2) == -1)
				return False;
		}
	}

	return True;
}

//$tst = "I am typing a whole bunch of bullshit on this screen so I can fix this stupid bug concerning the storage. The string::getsubstr function can only get two-hundred fifty five (255) characters from a string, so whenever this function was performed on the storage stuff string, alot of info would get lost. Players were actually capable of spawning items that aren't normally supposed to be spawned, like the Deployable Base for example. This is a big problem, but with this NEWgetSubStr function that I wrote, which splits up strings into chunks of 255 in order to get the string portion properly, the storage bug should go away and there should be a hell of a lot less cheating.";
function String::NEWgetSubStr(%s, %x, %y)
{
	dbecho($dbechoMode, "String::NEWgetSubStr(" @ %s @ ", " @ %x @ ", " @ %y @ ")");

	// Validate input parameters
	if(%s == "" || %y == "")
		return "";

	%len = %y;
	%chunks = floor(%len / 255) + 1;

	%q = %len;
	%nx = %x;
	%final = "";

	for(%i = 1; %i <= %chunks; %i++)
	{
		%q = %q - 255;
		if(%q <= 0)
			%chunkLen = %q+255;
		else
			%chunkLen = 255;

		%final = %final @ String::getSubStr(%s, %nx, %chunkLen);
		%nx = %nx + %chunkLen;
	}

	return %final;
}

function GetRoll(%roll, %optionalMinMax)
{
	dbecho($dbechoMode, "GetRoll(" @ %roll @ ", " @ %optionalMinMax @ ")");

	//this function accepts the following syntax, where N is any positive number NOT containing a +:
	//NdN
	//NdN+N
	//NdN-N
	//NdNxN
	//NdN+NxN
	//NdN-NxN

	%d = String::findSubStr(%roll, "d");
	%p = String::findSubStr(%roll, "+");
	if(%p == -1)
		%m = String::findSubStr(%roll, "-");
	%x = String::findSubStr(%roll, "x");

	if(%d == -1)
		return %roll;

	if(%x == -1)
		%x = String::len(%roll);

	%numDice = floor(String::getSubStr(%roll, 0, %d));
	if(%p != -1)
	{
		%diceFaces = String::getSubStr(%roll, %d+1, %p-%d-1);
		%bonus = String::getSubStr(%roll, %p+1, %x-1);
	}
	else if(%p == -1 && %m != -1)
	{
		%diceFaces = String::getSubStr(%roll, %d+1, %m-%d-1);
		%bonus = -String::getSubStr(%roll, %m+1, %x-1);
	}
	else
		%diceFaces = String::getSubStr(%roll, %d+1, 99999);

	%total = 0;
	for(%i = 1; %i <= %numDice; %i++)
	{
		if(%optionalMinMax == "min")
			%r = 1;
		else if(%optionalMinMax == "max")
			%r = %diceFaces;
		else
			%r = floor(getRandom() * %diceFaces)+1;

		%total += %r;
	}

	if(%bonus != "")
		%total += %bonus;

	if(%x != String::len(%roll))
		%total *= String::getSubStr(%roll, %x+1, 99999);

	return %total;
}

function GetCombo(%n)
{
	dbecho($dbechoMode, "GetCombo(" @ %n @ ")");

	//--- This is used so ComboTables don't get overwritten by simultaneous calls ---
	$w++;
	if($w > 20) $w = 1;
	//-------------------------------------------------------------------------------

	for(%i = 1; $ComboTable[$w, %i] != ""; %i++)
		$ComboTable[$w, %i] = "";

	%cnt = 0;

	while(%i != -1)
	{
		for(%i = 0; pow(2, %i) <= %n; %i++){}
		%i--;

		if(%i >= 0)
		{
			$ComboTable[$w, %cnt++] = pow(2, %i);
			%n -= pow(2, %i);
		}
	}

	return $w;
}

function IsPartOfCombo(%combo, %n)
{
	dbecho($dbechoMode, "IsPartOfCombo(" @ %combo @ ", " @ %n @ ")");

	%w = GetCombo(%combo);

	%flag = false;

	for(%i = 1; $ComboTable[%w, %i] != ""; %i++)
	{
		if(%n == $ComboTable[%w, %i])
			%flag = true;

		//It's a good idea to clean up after oneself, especially with all the ComboTables that would be floating around
		$ComboTable[%w, %i] = "";
	}

	return %flag;
}

function IsDead(%id)
{
	dbecho($dbechoMode, "IsDead(" @ %id @ ")");

	// Use helper function to get client ID from Player object (handles enemy bots)
	%clientId = GetClientIdFromPlayerObject(%id);
	
	// If helper function returns -1, try using %id directly as client ID
	if(%clientId == -1 || %clientId == "")
		%clientId = %id;
	
	%player = Client::getOwnedObject(%clientId);

	if(%player == -1 || %player == "")
		return True;
	else
		return False;
}

function Cap(%n, %lb, %ub)
{
	dbecho($dbechoMode, "Cap(" @ %n @ ", " @ %lb @ ", " @ %ub @ ")");

	if(%lb != "inf")
	{
		if(%n < %lb)
			%n = %lb;
	}

	if(%ub != "inf")
	{
		if(%n > %ub)
			%n = %ub;
	}

	return %n;
}

function GetNESW(%pos1, %pos2)
{
	dbecho($dbechoMode, "GetNESW(" @ %pos1 @ ", " @ %pos2 @ ")");

	%v1 = Vector::sub(%pos1, %pos2);
	%v2 = Vector::getRotation(%v1);
	%a = GetWord(%v2, 2);

	if(%a >= 2.7475 && %a <= 3.15 || %a >= -3.15 && %a <= -2.7475)
		%d = "North";
	else if(%a >= 1.9625 && %a <= 2.7475)
		%d = "North East";
	else if(%a >= 1.1775 && %a <= 1.9625)
		%d = "East";
	else if(%a >= 0.3925 && %a <= 1.1775)
		%d = "South East";
	else if(%a >= -0.3925 && %a <= 0.3925)
		%d = "South";
	else if(%a >= -1.1775 && %a <= -0.3925)
		%d = "South West";
	else if(%a >= -1.9625 && %a <= -1.1775)
		%d = "West";
	else if(%a >= -2.7475 && %a <= -1.9625)
		%d = "North West";

	return %d;
}

function DisplayGetInfo(%clientId, %id, %obj)
{
	dbecho($dbechoMode, "DisplayGetInfo(" @ %clientId @ ", " @ %id @ ", " @ %obj @ ")");

	if(%clientId.adminLevel >= 1)
		%showid = %id @ " (" @ %obj @ ")";
	else
		%showid = "";

	// Get team info (for bots, try Player object first, then client ID, then stored botTeam)
	%team = -1;
	%playerObj = Client::getOwnedObject(%id);
	if(%playerObj != -1 && %playerObj != "")
		%team = GameBase::getTeam(%playerObj);
	if(%team == -1)
		%team = GameBase::getTeam(%id);
	if(%team == -1)
	{
		%storedTeam = fetchData(%id, "botTeam");
		if(%storedTeam != "" && %storedTeam != -1 && %storedTeam != "0" && %storedTeam != 0)
			%team = %storedTeam;
	}
	// Only show team info for admins
	if(%clientId.adminLevel >= 1)
		%teamInfo = " Team: " @ %team;
	else
		%teamInfo = "";

	%houseValue = fetchData(%id, "MyHouse");
	// Treat "0", "House0", and "house0" as empty (no house) - new players might have these invalid values
	if(%houseValue == "0" || %houseValue == "House0" || %houseValue == "house0")
		%houseValue = "";
	// CRITICAL: If houseValue is still not empty, verify it's a valid house name
	if(%houseValue != "")
	{
		%houseNum = GetHouseNumber(%houseValue);
		// If GetHouseNumber returns empty, it's not a valid house - clear it
		if(%houseNum == "" || %houseNum == 0 || %houseNum == "0")
		{
			echo("DEBUG: DisplayGetInfo - Invalid house name '" @ %houseValue @ "' detected, clearing");
			%houseValue = "";
			storeData(%id, "MyHouse", "");
		}
	}
	if(%houseValue != "")
		%house = "*** Proud member of <f2>" @ %houseValue @ "<f0>";
	else
		%house = "";

	// Build display message
	%msg = "<jc><f1>" @ Client::getName(%id) @ ", LEVEL " @ fetchData(%id, "LVL") @ " " @ getFinalCLASS(%id) @ " REMORT " @ fetchData(%id, "RemortStep") @ "<f0> " @ " " @ %showid @ %teamInfo @ "\n" @ %house @ "\nWorld Rank: " @ $WorldRank[fetchData(%id, "TournyRank")] @ "\nBounty: " @ fetchData(%id, "bounty");
	
	// Add Ascension talents if the player has any (players only)
	%talents = fetchData(%id, "AscensionTalents");
	if(%talents != "" && %talents != "0" && !Player::isAiControlled(%id) && !isRPGAI(%id))
	{
		%talentDisplay = "";
		for(%t = 0; (%talentId = GetWord(%talents, %t)) != -1; %t++)
		{
			if(%talentId != "" && %talentId != "0")
			{
				%talentName = $AscensionTalent[%talentId, Name];
				if(%talentName != "")
				{
					if(%talentDisplay != "")
						%talentDisplay = %talentDisplay @ ", ";
					%talentDisplay = %talentDisplay @ %talentName;
				}
			}
		}
		if(%talentDisplay != "")
			%msg = %msg @ "\n<f2>Ascension:<f0> " @ %talentDisplay;
	}
	
	%msg = %msg @ "\n" @ fetchData(%id, "PlayerInfo");
	if(fetchData(%id, "PlayerInfo") == "")
		%msg = %msg @ "A mere citizen of the Kingdom.";

	// Send a compact info line for client-side parsers (e.g., Presto auto-remort)
	// Format: [PRESTOINFO] lvl=<lvl> remort=<remortStep>
	%infoLvl = fetchData(%id, "LVL");
	%infoRemort = fetchData(%id, "RemortStep");
	Client::sendMessage(%clientId, 0, "[PRESTOINFO] lvl=" @ %infoLvl @ " remort=" @ %infoRemort);

	// Use persistentCenterprint to prevent message from being overwritten by bottomprint (e.g., during combat)
	persistentCenterprint(%clientId, %msg, 5);
}

function DisplayGetStats(%clientId, %id, %obj)
{
	dbecho($dbechoMode, "DisplayGetStats(" @ %clientId @ ", " @ %id @ ", " @ %obj @ ")");

	if(%clientId.adminLevel >= 1)
		%showid = %id @ " (" @ %obj @ ")";
	else
		%showid = "";

	// Get team info (for bots, try Player object first, then client ID, then stored botTeam)
	%team = -1;
	%playerObj = Client::getOwnedObject(%id);
	if(%playerObj != -1 && %playerObj != "")
		%team = GameBase::getTeam(%playerObj);
	if(%team == -1)
		%team = GameBase::getTeam(%id);
	if(%team == -1)
	{
		%storedTeam = fetchData(%id, "botTeam");
		if(%storedTeam != "" && %storedTeam != -1 && %storedTeam != "0" && %storedTeam != 0)
			%team = %storedTeam;
	}
	// Only show team info for admins
	if(%clientId.adminLevel >= 1)
		%teamInfo = " Team: " @ %team;
	else
		%teamInfo = "";

	%name = Client::getName(%id);
	%def = fetchData(%id, "DEF");
	%mdef = fetchData(%id, "MDEF");
	%atk = fetchData(%id, "ATK");
	%maxHP = fetchData(%id, "MaxHP");
	%hp = fetchData(%id, "HP");
	%maxMANA = fetchData(%id, "MaxMANA");
	%mana = fetchData(%id, "MANA");
	%maxWeight = fetchData(%id, "MaxWeight");
	%weight = fetchData(%id, "Weight");
	
	// For enemy bots, LCK is stored directly in fetchData
	// For players, AddPoints() calculates LCK from items (but might not include base LCK)
	// Use fetchData for bots, AddPoints for players
	if(isRPGAI(%id))
	{
		%lck = fetchData(%id, "LCK");
		if(%lck == "" || %lck == -1)
			%lck = 0;
	}
	else
	{
		%lck = AddPoints(%id, 2);
		// AddPoints only gets LCK from items, so add base LCK if it exists
		%baseLCK = fetchData(%id, "LCK");
		if(%baseLCK != "" && %baseLCK != -1)
			%lck = %lck + %baseLCK;
	}
	
	%stance = fetchData(%id, "Stance");
	%overweightStep = fetchData(%id, "OverweightStep");

	%msg = "<jc><f1>" @ %name @ " - Combat Stats<f0> " @ %showid @ %teamInfo;
	%msg = %msg @ "\n<f2>DEF:<f0> " @ %def @ "  <f2>MDEF:<f0> " @ %mdef @ "  <f2>ATK:<f0> " @ %atk;
	%msg = %msg @ "\n<f2>HP:<f0> " @ %hp @ " / " @ %maxHP @ "  <f2>MANA:<f0> " @ %mana @ " / " @ %maxMANA;
	%msg = %msg @ "\n<f2>Weight:<f0> " @ %weight @ " / " @ %maxWeight;
	%msg = %msg @ "\n<f2>LCK:<f0> " @ %lck;
	
	if(%stance != "")
		%msg = %msg @ "  <f2>Stance:<f0> " @ %stance;
	
	if(%overweightStep > 0)
		%msg = %msg @ "\n<f2>Overweight:<f0> " @ %overweightStep @ "%";

	// Use persistentCenterprint to prevent message from being overwritten by bottomprint (e.g., during combat)
	persistentCenterprint(%clientId, %msg, 5);
}

function WhatIs(%item)
{
	dbecho($dbechoMode, "WhatIs(" @ %item @ ")");

	//--------- GATHER INFO ------------------
	if(%item.description == False)	
		%desc = %item;
	else
		%desc = %item.description;

	%t = GetAccessoryVar(%item, $AccessoryType);
	%w = GetAccessoryVar(%item, $Weight);
	%c = GetItemCost(%item);
	%s = $SkillDesc[$SkillType[%item]];

	if(GetDelay(%item) != "" && GetDelay(%item) != 0)
		%sd = GetDelay(%item);
	else
		%sd = "";

	if($LocationDesc[%t] != "")
		%loc = " - Type: " @ $LocationDesc[%t];
	else
		%loc = "";

	if($AccessoryVar[%item, $MiscInfo] != "")
		%nfo = $AccessoryVar[%item, $MiscInfo];
	else
		%nfo = "There is no further information available.";

	%si = $Spell::index[%item];
	if(%si != "")
	{
		%desc = $Spell::name[%si];
		%nfo = $Spell::description[%si];
		%atkinfo = $Spell::damageValue[%si];
		%sd = $Spell::delay[%si];
		%sr = $Spell::recoveryTime[%si];
		%sm = $Spell::manaCost[%si];
	}

	//--------- BUILD MSG --------------------
	%msg = "";
	%msg = %msg @ "<jc><f1>" @ %desc @ %loc @ "\n";
	%msg = %msg @ "\nBonuses: " @ WhatSpecialVars(%item);
	if(%s != "")
		%msg = %msg @ "\nSkill Type: " @ %s;
	%msg = %msg @ "\nRestrictions: " @ WhatSkills(%item);
	if(%w != "")
		%msg = %msg @ "\nWeight: " @ %w;
	if(%c != "")
		%msg = %msg @ "\nPrice: $" @ Commafy(%c);
	if(%sd != "")
		%msg = %msg @ "\nDelay: " @ FixDecimals(%sd) @ " sec";
	if(%sr != "")
		%msg = %msg @ "\nRecovery: " @ %sr @ " sec";
	if(%sm != "")
		%msg = %msg @ "\nMana: " @ %sm;

	%msg = %msg @ "\n\n<f0>" @ %nfo;

	return %msg;
}

function FixDecimals(%c)
{
	dbecho($dbechoMode, "FixDecimals(" @ %c @ ")");

	// build the "N.d" string by hand - the old (round(x*10)/10)*1.000001
	// trick reintroduced float noise (delays printed with ~20 decimals)
	%d = round(%c * 10);
	%i = floor(%d / 10);
	%f = %d - (%i * 10);
	return %i @ "." @ %f;
}

// Thousands separators for display ("1234567" -> "1,234,567"). Integers only.
function Commafy(%n)
{
	%n = floor(%n);
	if(%n < 1000)
		return %n;
	%out = "";
	while(%n >= 1000)
	{
		%r = %n - (floor(%n / 1000) * 1000);
		%n = floor(%n / 1000);
		if(%r < 10)
			%r = "00" @ %r;
		else if(%r < 100)
			%r = "0" @ %r;
		%out = "," @ %r @ %out;
	}
	return %n @ %out;
}

function AddToCommaList(%list, %item)
{
	dbecho($dbechoMode, "AddToCommaList(" @ %list @ ", " @ %item @ ")");

	%list = %list @ %item @ $sepchar;

	return %list;
}
function RemoveFromCommaList(%list, %item)
{
	dbecho($dbechoMode, "RemoveFromCommaList(" @ %list @ ", " @ %item @ ")");

	%a = $sepchar @ %list;
	%a = String::replace(%a, $sepchar @ %item @ $sepchar, ",");
	%list = String::NEWgetSubStr(%a, 1, 99999);

	return %list;
}
function IsInCommaList(%list, %item)
{
	dbecho($dbechoMode, "IsInCommaList(" @ %list @ ", " @ %item @ ")");

	%a = $sepchar @ %list;
	if(String::findSubStr(%a, "," @ %item @ ",") != -1)
		return True;
	else
		return False;
}
function CountObjInCommaList(%list)
{
	dbecho($dbechoMode, "CountObjInCommaList(" @ %list @ ")");

	%cnt = 0;
	for(%i = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
		%cnt++;
	return %cnt;
}

function CountObjInList(%list)
{
	dbecho($dbechoMode, "CountObjInList(" @ %list @ ")");

	for(%i = 0; GetWord(%list, %i) != -1; %i++){}

	return %i;
}

function GetEventCommandIndex(%object, %type)
{
	dbecho($dbechoMode, "GetEventCommandIndex(" @ %object @ ", " @ %type @ ")");

	%list = "";

	//5 event commands max. per object
	for(%i = 1; %i <= $maxEvents; %i++)
	{
		// Use temp variable to safely check if EventCommand exists before accessing
		%tempCmd = $EventCommand[%object, %i];
		if(%tempCmd != "")
		{
			%t = GetWord(%tempCmd, 1);
			if(String::ICompare(%t, %type) == 0)
				%list = %list @ %i @ " ";
		}
	}

	if(%list != "")
		return String::getSubStr(%list, 0, String::len(%list)-1);
	else
		return -1;
}

function AddEventCommand(%object, %senderName, %type, %cmd)
{
	dbecho($dbechoMode, "AddEventCommand(" @ %object @ ", " @ %senderName @ ", " @ %type @ ", " @ %cmd @ ")");

	for(%i = 1; %i <= $maxEvents; %i++)
	{
		if($EventCommand[%object, %i] == "" || String::ICompare(GetWord($EventCommand[%object, %i], 1), %type) == 0)
		{
			$EventCommand[%object, %i] = %senderName @ " " @ %type @ " " @ %cmd;
			return %i;
		}
	}
	return -1;
}

function ClearEvents(%id)
{
	dbecho($dbechoMode, "ClearEvents(" @ %id @ ")");

	for(%i = 1; %i <= $maxEvents; %i++)
	{
		$EventCommand[%id, %i] = "";
		if(%id.tag != False)
			$EventCommand[%id.tag, %i] = "";
	}
}

function msprintf(%in, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8)
{
	dbecho($dbechoMode, "msprintf(" @ %in @ ", " @ %a1 @ ", " @ %a2 @ ", " @ %a3 @ ", " @ %a4 @ ", " @ %a5 @ ", " @ %a6 @ ", " @ %a7 @ ", " @ %a8 @ ")");

	%final = "";

	%cnt = 0;
	%list = %in;
	for(%p = String::findSubStr(%list, "%"); (%p = String::findSubStr(%list, "%")) != -1; %p = String::findSubStr(%list, "%"))
	{
		%crash++;
		if(%crash > 30)
		{
			echo("FATAL CRASH BUG...contact JeremyIrons and tell him his msprintf is fucking up");
			break;
		}

		%list = String::NEWgetSubStr(%list, %p+1, 99999);
		%cnt = String::getSubStr(%list, 0, 1);

		%check = String::findSubStr(%list, "%");
		if(%check == -1) %check = 99999;
		%endsign = String::findSubStr(%list, ";");

		if(%endsign != -1 && %endsign < %check)
		{
			%ev = String::NEWgetSubStr(%list, 1, %endsign);
			%a[%cnt] = eval("%x = " @ %a[%cnt] @ %ev);

			%in = String::replace(%in, %ev, "");
		}
	}

	return sprintf(%in, %a[1], %a[2], %a[3], %a[4], %a[5], %a[6], %a[7], %a[8]);
}

function nsprintf(%in, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8)
{
	dbecho($dbechoMode, "nsprintf(" @ %in @ ", " @ %a1 @ ", " @ %a2 @ ", " @ %a3 @ ", " @ %a4 @ ", " @ %a5 @ ", " @ %a6 @ ", " @ %a7 @ ", " @ %a8 @ ")");

	%list = %in;
	for(%p = String::findSubStr(%list, "%"); (%p = String::findSubStr(%list, "%")) != -1; %p = String::findSubStr(%list, "%"))
	{
		%list = String::NEWgetSubStr(%list, %p+1, 99999);
		%w = String::getSubStr(%list, 0, 1);
		if(!IsInCommaList("1,2,3,4,5,6,7,8,", %w))
			return "Error in syntax";
	}

	return msprintf(%in, %a[1], %a[2], %a[3], %a[4], %a[5], %a[6], %a[7], %a[8]);
}

function LTrim(%s)
{
	dbecho($dbechoMode, "LTrim(" @ %s @ ")");

	%a = GetWord(%s, 0);
	%p1 = String::findSubStr(%s, %a);
	%s = String::NEWgetSubStr(%s, %p1, 99999);

	return %s;
}

function RTrim(%s)
{
	// Simple RTrim implementation
	if(%s == "") return "";
	%len = String::len(%s);
	%end = %len - 1;
	while(%end >= 0 && String::getSubStr(%s, %end, 1) == " ")
		%end--;
		
	return String::getSubStr(%s, 0, %end + 1);
}

function Trim(%s)
{
	return RTrim(LTrim(%s));
}
