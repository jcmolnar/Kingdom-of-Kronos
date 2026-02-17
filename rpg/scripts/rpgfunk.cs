$INVISIBILITY_DEBUG = 0; // Toggle [INVISIBILITY DEBUG] messages - set to 1 to diagnose invisible bots
$LOOTBAG_DEBUG = 0; // Toggle [LOOTBAG DEBUG] messages in this file

// Safe skin setter - validates skin before applying and logs potential invisibility issues
function Safe_SetSkin(%clientId, %skin, %callerContext)
{
	if(%skin == "" || %skin == -1)
	{
		%clientName = Client::getName(%clientId);
		if(%clientName == "" || %clientName == -1) %clientName = "Unknown";
		echo("[INVISIBILITY BUG] Safe_SetSkin: EMPTY SKIN detected for " @ %clientName @ " (clientId=" @ %clientId @ "). Context: " @ %callerContext @ ". NOT setting skin to prevent invisibility!");
		return;
	}
	
	if($INVISIBILITY_DEBUG)
	{
		%clientName = Client::getName(%clientId);
		if(%clientName == "" || %clientName == -1) %clientName = "Bot";
		echo("[INVISIBILITY DEBUG] Safe_SetSkin: Setting skin '" @ %skin @ "' for " @ %clientName @ " (clientId=" @ %clientId @ "). Context: " @ %callerContext);
	}
	
	// OPTIMIZATION: Only set skin if it's actually different to prevent engine/network spam
	if(Client::getSkinBase(%clientId) != %skin)
	{
		Client::setSkin(%clientId, %skin);
	}
}
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

//====================================================================================================
// BankStorage Multi-Field Split Functions
// Splits large BankStorage strings (>256 chars) across multiple funkvar fields to prevent
// engine buffer overflow during shutdown. Uses fields 16 (primary), 60, 61, 62, 63 (overflow).
//====================================================================================================

// SplitAndSaveBankStorage - Splits "item count item count..." into <=256 char chunks
// Saves to fields 16 (primary), 60, 61, 62, 63 (overflow)
// Sends warning to player if reaching field 63 (80% capacity)
function SplitAndSaveBankStorage(%clientId, %name, %fullString)
{
	// Define field mapping (primary + 4 overflow = 5 fields x 256 chars = 1280 max)
	$BankStorageFields[0] = 16;  // Primary field
	$BankStorageFields[1] = 60;  // Overflow 1
	$BankStorageFields[2] = 61;  // Overflow 2
	$BankStorageFields[3] = 62;  // Overflow 3
	$BankStorageFields[4] = 63;  // Overflow 4 (warning threshold)
	$BankStorageMaxFields = 5;
	$BankStorageMaxLen = 256;
	
	// Clear all fields first
	for(%f = 0; %f < $BankStorageMaxFields; %f++)
	{
		%fieldNum = $BankStorageFields[%f];
		$funk::var["[\"" @ %name @ "\", 0, " @ %fieldNum @ "]"] = "";
	}
	
	// Handle empty/null case
	if(%fullString == "" || %fullString == "0" || %fullString == -1)
	{
		$funk::var["[\"" @ %name @ "\", 0, 16]"] = "";
		return;
	}
	
	// Split at complete "item count" boundaries
	%currentField = 0;
	%currentString = "";
	%warnedPlayer = false;
	
	for(%i = 0; GetWord(%fullString, %i) != -1; %i += 2)
	{
		%item = GetWord(%fullString, %i);
		%count = GetWord(%fullString, %i + 1);
		
		// Skip invalid entries (including "0" which can appear from uninitialized BankStorage)
		if(%item == "" || %item == -1 || %item == "0" || %count == "" || %count == -1)
			continue;
		
		%pair = %item @ " " @ %count;
		%pairLen = String::len(%pair);
		
		// Check if adding this pair would exceed limit
		%currentLen = String::len(%currentString);
		%wouldExceed = false;
		if(%currentLen > 0)
		{
			// Need space + pair
			if(%currentLen + 1 + %pairLen > $BankStorageMaxLen)
				%wouldExceed = true;
		}
		else
		{
			// First item in field
			if(%pairLen > $BankStorageMaxLen)
			{
				// Single item exceeds limit - this shouldn't happen but handle it
				echo("WARNING: SplitAndSaveBankStorage - Single item '" @ %item @ "' exceeds max length!");
			}
		}
		
		if(%wouldExceed)
		{
			// Save current field and move to next
			%fieldNum = $BankStorageFields[%currentField];
			$funk::var["[\"" @ %name @ "\", 0, " @ %fieldNum @ "]"] = %currentString;
			
			%currentField++;
			%currentString = %pair;
			
			// Check if we're at capacity warning threshold (field 63)
			if(%currentField >= 4 && !%warnedPlayer)
			{
				%warnedPlayer = true;
				Client::sendMessage(%clientId, $MsgRed, "WARNING: Your bank storage is almost full! Remove some items to avoid bank storage corruption.");
				echo("[BANKSTORAGE] Warning sent to " @ Client::getName(%clientId) @ " - bank storage at 80% capacity");
			}
			
			// Check if we've exceeded all available fields
			if(%currentField >= $BankStorageMaxFields)
			{
				echo("ERROR: SplitAndSaveBankStorage - Exceeded all " @ $BankStorageMaxFields @ " fields for " @ %name @ "! Data may be truncated.");
				Client::sendMessage(%clientId, $MsgRed, "ERROR: Bank storage overflow! Some items may be lost. Please reduce stored items immediately.");
				return;
			}
		}
		else
		{
			// Add to current string
			if(%currentString != "")
				%currentString = %currentString @ " " @ %pair;
			else
				%currentString = %pair;
		}
	}
	
	// Save the last field
	if(%currentString != "")
	{
		%fieldNum = $BankStorageFields[%currentField];
		$funk::var["[\"" @ %name @ "\", 0, " @ %fieldNum @ "]"] = %currentString;
	}
	
	//echo("[BANKSTORAGE] Saved " @ %name @ " across " @ (%currentField + 1) @ " field(s)");
}

// JoinBankStorageFromLoad - Joins multiple fields back into single string
// Reads from fields 16 (primary), 60, 61, 62, 63 (overflow)
// Returns complete BankStorage string
function JoinBankStorageFromLoad(%name)
{
	// Define field mapping (same as save)
	$BankStorageFields[0] = 16;  // Primary field
	$BankStorageFields[1] = 60;  // Overflow 1
	$BankStorageFields[2] = 61;  // Overflow 2
	$BankStorageFields[3] = 62;  // Overflow 3
	$BankStorageFields[4] = 63;  // Overflow 4
	$BankStorageMaxFields = 5;
	
	%result = "";
	
	for(%f = 0; %f < $BankStorageMaxFields; %f++)
	{
		%fieldNum = $BankStorageFields[%f];
		%fieldData = $funk::var[%name, 0, %fieldNum];
		
		// Skip empty/null fields
		if(%fieldData == "" || %fieldData == "0" || %fieldData == -1 || %fieldData == " ")
			continue;
		
		// Concatenate with space separator
		if(%result != "")
			%result = %result @ " " @ %fieldData;
		else
			%result = %fieldData;
	}
	
	//echo("[BANKSTORAGE] Loaded " @ %name @ " with combined length " @ String::len(%result));
	return %result;
}

function viewGroupList(%clientId)
{
	dbecho($dbechoMode, "viewGroupList(" @ %clientId @ ")");

	bottomprint(%clientId, fetchData(%clientId, "grouplist"), 8);
}

// Number formatting helper
// Returns abbreviated format: 1.5k, 2.5m, 1.2b
function numFormat(%num)
{
	if(%num < 1000) return %num;
	
	if(%num < 1000000)
	{
		%k = %num / 1000;
		// Keep 1 decimal place
		%disp = String::getSubStr(%k, 0, String::findSubStr(%k, ".") + 2);
		if(String::getSubStr(%disp, String::len(%disp)-1, 1) == "0")
			%disp = String::getSubStr(%disp, 0, String::len(%disp)-2); // Remove .0
		return %disp @ "k";
	}
	
	if(%num < 1000000000)
	{
		%m = %num / 1000000;
		%disp = String::getSubStr(%m, 0, String::findSubStr(%m, ".") + 2);
		if(String::getSubStr(%disp, String::len(%disp)-1, 1) == "0")
			%disp = String::getSubStr(%disp, 0, String::len(%disp)-2);
		return %disp @ "m";
	}
	
	%b = %num / 1000000000;
	%disp = String::getSubStr(%b, 0, String::findSubStr(%b, ".") + 2);
	if(String::getSubStr(%disp, String::len(%disp)-1, 1) == "0")
		%disp = String::getSubStr(%disp, 0, String::len(%disp)-2);
	return %disp @ "b";
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

function updateSpawnStuff(%clientId)
{
	dbecho($dbechoMode2, "updateSpawnStuff(" @ %clientId @ ")");

	// CRITICAL: Skip bots - they don't need spawnStuff saved
	if(isRPGAI(%clientId))
		return "";

	//determine what player is carrying and transfer to spawnList
	%s = "";
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
		return ""; // Player object doesn't exist
	
	// Save mounted weapon before processing items (for reconnect remount)
	%mountedWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
	if(%mountedWeapon != "" && %mountedWeapon != -1)
	{
		storeData(%clientId, "savedMountedWeapon", %mountedWeapon);
	}
	else
	{
		storeData(%clientId, "savedMountedWeapon", "");
	}
	
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		// Re-validate player object in case it gets despawned during the loop
		%player = Client::getOwnedObject(%clientId);
		if(%player == -1 || %player == "")
			break; // Exit loop if player object no longer exists
		
		%checkItem = getItemData(%i);
		// CRITICAL: Never save flags to spawnStuff - flags are mission objectives, not player inventory
		if(%checkItem == "Flag" || %checkItem == Flag)
			continue;
		
		// CRITICAL: Validate player object exists before calling Player::getItemCount
		// Player object could become invalid during the loop
		if(%player == -1 || %player == "")
			break; // Exit loop if player object no longer exists
		
		// CRITICAL: Final validation right before Player::getItemCount call
		%playerFinal = Client::getOwnedObject(%clientId);
		if(%playerFinal == -1 || %playerFinal == "")
		{
			echo("[DEBUG getItemCount] updateSpawnStuff - Player object deleted during loop, clientId: " @ %clientId @ ", item: " @ %checkItem);
			break; // Exit loop if player object was deleted between checks
		}
		
		%itemcount = SafeGetItemCount(%clientId, %checkItem, "updateSpawnStuff");
		if(%itemcount)
			%s = %s @ %checkItem @ " " @ %itemcount @ " ";
	}

	storeData(%clientId, "spawnStuff", %s);

	return %s;
}
function isInSpellList(%name, %sname)
{
	dbecho($dbechoMode, "isInSpellList(" @ %name @ ", " @ %sname @ ")");

	%sname = %sname @ $sepchar;

	//check if %sname (includes delimiter) is in %name's SpellList
	if(String::findSubStr($SpellList[%name], %sname) != -1)
		return 1;
	else
		return 0;
}
function getSpellAtPos(%name, %pos)
{
	dbecho($dbechoMode, "getSpellAtPos(" @ %name @ ", " @ %pos @ ")");

	%s = $SpellList[%name];
	%n = 0;
	%oldpos = 0;

	for(%i=0; %i<=String::len(%s); %i++)
	{
		%a = String::getSubStr(%s, %i, 1);
		if(%a == ",")
		{
			%n++;
			if(%n == %pos && (%i+1) <= String::len(%s))
			{
				return String::getSubStr(%s, %oldpos, (%i-1)-%oldpos+1);
			}
			%oldpos = %i+1;
		}
	}
	return -1;
}
function countNumSpells(%clientId)
{
	dbecho($dbechoMode, "countNumSpells(" @ %clientId @ ")");

	%name = Client::getName(%clientId);
	%s = $SpellList[%name];
	%n = 0;

	for(%i=0; %i<=String::len(%s); %i++)
	{
		%a = String::getSubStr(%s, %i, 1);
		if(%a == ",") %n++;
	}

	return %n;
}
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
function DoCamp(%clientId, %savecharTry)
{
	dbecho($dbechoMode, "DoCamp(" @ %clientId @ ", " @ %savecharTry @ ")");

	if(%savecharTry)
	{
		%vel = Item::getVelocity(%clientId);
		if(getWord(%vel, 2) > -500)
		{
			if(!IsDead(%clientId))
			{
				storeData(%clientId, "campPos", GameBase::getPosition(%clientId));
				storeData(%clientId, "campRot", GameBase::getRotation(%clientId));
			}
			return True;
		}
	}
	else
	{
		if(GameBase::isAtRest(%clientId))
		{
			storeData(%clientId, "campPos", GameBase::getPosition(%clientId));
			storeData(%clientId, "campRot", GameBase::getRotation(%clientId));
			return True;
		}
	}
	return False;
}
function SaveCharacter(%clientId)
{
	Watchdog_Enter("SaveCharacter");
	dbecho($dbechoMode2, "SaveCharacter(" @ %clientId @ ")");

	// CRITICAL: Prevent saving AI bots (both enemy bots and town bots) - CHECK FIRST BEFORE ANYTHING ELSE
	// This must be the FIRST check to prevent bots from saving data to temp folder
	// Check multiple ways to ensure we catch all bots
	
	// Method 1: Check if AI controlled
	if(Player::isAiControlled(%clientId))
	{
		echo("WARNING: SaveCharacter - Attempted to save bot clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ ") - blocked by Player::isAiControlled()");
		return False;
	}
	
	// Method 2: Check isRPGAI() function
	if(isRPGAI(%clientId))
	{
		echo("WARNING: SaveCharacter - Attempted to save bot clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ ") - blocked by isRPGAI()");
		return False;
	}
	
	// Method 3: Look for bot data (SpawnBotInfo = enemy bot, BotInfoAiName = town bot)
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	if((%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1) || 
	   (%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1))
	{
		// This is a bot (enemy or town) - do not save
		echo("WARNING: SaveCharacter - Attempted to save bot clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ ") - blocked by bot data check (SpawnBotInfo='" @ %spawnBotInfo @ "', BotInfoAiName='" @ %botInfoAiName @ "')");
		return False;
	}

	//first pass check
	if(%clientId.isInvalid || !fetchData(%clientId, "HasLoadedAndSpawned"))
		return False;

	//second pass check, will cause 4 line flood if the client is invalid
	//only do this as a "last resort" test.  if the player is detected to be dead, then there shouldn't be a problem
	%player = Client::getOwnedObject(%clientId);
	if(!IsDead(%clientId) && %player != -1 && %player != "")
	{
		// CRITICAL: Validate player object ONCE before starting the inventory sanity check
		// This test adds/removes a Tool item to verify inventory system is working
		// The inc/dec sequence must run atomically to prevent Tool from getting stuck in inventory
		%playerCheck = Client::getOwnedObject(%clientId);
		if(%playerCheck == -1 || %playerCheck == "")
			return False; // Player object became invalid - abort before modifying inventory
		
		// Atomic inventory sanity check - use Player::getItemCount directly (not SafeGetItemCount)
		// SafeGetItemCount has an isObject() check that fails for items retrieved by index
		Player::incItemCount(%clientId, Tool);
		%x = Player::getItemCount(%clientId, Tool);
		Player::decItemCount(%clientId, Tool);
		%y = Player::getItemCount(%clientId, Tool);
		
		if(%x == %y)
			return False;
	}

	%name = Client::getName(%clientId);
	//echo("DEBUG SaveCharacter: Player = " @ %name @ " (" @ %clientId @ ")");

	if(!IsDead(%clientId) && !IsInRoster(%clientId) && !IsInArenaDueler(%clientId))
	{
		//echo("DEBUG SaveCharacter: Updating campPos and campRot from player position");
		storeData(%clientId, "campPos", GameBase::getPosition(%clientId));
		storeData(%clientId, "campRot", GameBase::getRotation(%clientId));
	}

	ClearFunkVar(%name);

	//the first identifier in the array is the player's name.
	//this is needed because we are using a global array ($funk::var), so if another player
	//attempts to save at the same time, then there won't be $funk::var's being overwritten

	//the second identifier in the array is either 0, 1, or 2
	//0: regular player variable
	//1: weapon/item
	//2: quest counters

	//the third identifier is simply for identifying what we're saving.

	//echo("DEBUG SaveCharacter: Saving character " @ %name @ " (" @ %clientId @ ")...");
	//echo("DEBUG SaveCharacter: Saving basic character data...");
	$funk::var["[\"" @ %name @ "\", 0, 1]"] = fetchData(%clientId, "RACE");
	$funk::var["[\"" @ %name @ "\", 0, 2]"] = fetchData(%clientId, "EXP");
	$funk::var["[\"" @ %name @ "\", 0, 3]"] = fetchData(%clientId, "campPos");
	$funk::var["[\"" @ %name @ "\", 0, 4]"] = fetchData(%clientId, "COINS");
	$funk::var["[\"" @ %name @ "\", 0, 5]"] = fetchData(%clientId, "isMimic");
	$funk::var["[\"" @ %name @ "\", 0, 6]"] = fetchData(%clientId, "BANK");
	$funk::var["[\"" @ %name @ "\", 0, 7]"] = Client::getName(%clientId);
	$funk::var["[\"" @ %name @ "\", 0, 8]"] = fetchData(%clientId, "grouplist");
	$funk::var["[\"" @ %name @ "\", 0, 9]"] = fetchData(%clientId, "defaultTalk");
	$funk::var["[\"" @ %name @ "\", 0, 10]"] = fetchData(%clientId, "password");
	$funk::var["[\"" @ %name @ "\", 0, 11]"] = fetchData(%clientId, "bounty");
	$funk::var["[\"" @ %name @ "\", 0, 12]"] = fetchData(%clientId, "inArena");
	$funk::var["[\"" @ %name @ "\", 0, 13]"] = fetchData(%clientId, "PlayerInfo");
	$funk::var["[\"" @ %name @ "\", 0, 14]"] = fetchData(%clientId, "deathmsg");
	//15 is done lower
	// CRITICAL: Split BankStorage across multiple fields (16, 60-63) with 256 char max each
	// This prevents engine buffer overflow during shutdown
	SplitAndSaveBankStorage(%clientId, %name, fetchData(%clientId, "BankStorage"));
	$funk::var["[\"" @ %name @ "\", 0, 17]"] = fetchData(%clientId, "campRot");
	//echo("DEBUG SaveCharacter: campRot = '" @ fetchData(%clientId, "campRot") @ "'");
	
	//echo("DEBUG SaveCharacter: Normalizing HP/MANA before save...");
	// Normalize HP and MANA - prevent negative values from being saved
	%hp = fetchData(%clientId, "HP");
	//echo("DEBUG SaveCharacter: HP RAW = '" @ %hp @ "'");
	%hpNum = %hp * 1;
	if(%hp == "" || %hp == " " || %hp == "0" || %hp == "-0" || %hpNum < 0)
	{
		//echo("DEBUG SaveCharacter: HP was invalid (empty/0/-0/negative), normalizing to empty string");
		%hp = "";
	}
	$funk::var["[\"" @ %name @ "\", 0, 18]"] = %hp;
	//echo("DEBUG SaveCharacter: HP FINAL = '" @ %hp @ "'");
	
	%mana = fetchData(%clientId, "MANA");
	//echo("DEBUG SaveCharacter: MANA RAW = '" @ %mana @ "'");
	%manaNum = %mana * 1;
	if(%mana == "" || %mana == " " || %mana == "0" || %mana == "-0" || %manaNum < 0)
	{
		//echo("DEBUG SaveCharacter: MANA was invalid (empty/0/-0/negative), normalizing to empty string");
		%mana = "";
	}
	$funk::var["[\"" @ %name @ "\", 0, 19]"] = %mana;
	//echo("DEBUG SaveCharacter: MANA FINAL = '" @ %mana @ "'");
	$funk::var["[\"" @ %name @ "\", 0, 20]"] = fetchData(%clientId, "LCKconsequence");
	$funk::var["[\"" @ %name @ "\", 0, 21]"] = fetchData(%clientId, "RemortStep");
	$funk::var["[\"" @ %name @ "\", 0, 22]"] = fetchData(%clientId, "LCK");
	$funk::var["[\"" @ %name @ "\", 0, 23]"] = $rpgver;
	$funk::var["[\"" @ %name @ "\", 0, 26]"] = fetchData(%clientId, "GROUP");
	$funk::var["[\"" @ %name @ "\", 0, 27]"] = fetchData(%clientId, "CLASS");
	$funk::var["[\"" @ %name @ "\", 0, 28]"] = fetchData(%clientId, "SPcredits");
	// CRITICAL: Save mounted weapon so it can be restored on reconnect
	// First, get the currently mounted weapon and save it to savedMountedWeapon
	%playerObj = Client::getOwnedObject(%clientId);
	%mountedWeapon = "";
	if(%playerObj != -1 && %playerObj != "")
	{
		%mountedWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
		if(%mountedWeapon != -1 && %mountedWeapon != "")
		{
			storeData(%clientId, "savedMountedWeapon", %mountedWeapon);
		}
		else
		{
			// No weapon mounted - clear savedMountedWeapon
			storeData(%clientId, "savedMountedWeapon", "");
		}
	}
	// Now fetch and save to character file
	// Note: Empty values will be saved as "0" by the persistence system (normal Tribes RPG behavior)
	$funk::var["[\"" @ %name @ "\", 0, 29]"] = fetchData(%clientId, "savedMountedWeapon");
	// CRITICAL: Ensure empty houses save as empty string, not "0"
	// GetHouseNumber returns "" for empty houses, but we need to ensure it's never saved as "0"
	%houseValue = fetchData(%clientId, "MyHouse");
	// Treat "0" and "House0" as empty (no house)
	if(%houseValue == "0" || %houseValue == "House0" || %houseValue == "house0")
		%houseValue = "";
	%houseNum = GetHouseNumber(%houseValue);
	// If houseNum is empty, 0, or "0", save as empty string (not "0")
	if(%houseNum == "" || %houseNum == 0 || %houseNum == "0")
		%houseNum = "";
	$funk::var["[\"" @ %name @ "\", 0, 30]"] = %houseNum;
	$funk::var["[\"" @ %name @ "\", 0, 31]"] = fetchData(%clientId, "RankPoints");
	$funk::var["[\"" @ %name @ "\", 0, 32]"] = fetchData(%clientId, "TournyRank");
	// Normalize equipped categories - use "0" for empty to match original inventory system behavior
	%questItems = fetchData(%clientId, "QuestItems");
	// Match field 15 behavior: if empty, save as empty string (persistence system may remove column)
	if(%questItems == "" || %questItems == " " || %questItems == "0")
		%questItems = "";
	
	// CRITICAL: Validate QuestItems string before saving to prevent corruption
	// Check if string contains suspicious patterns that might cause parsing issues
	%questItemsLen = String::len(%questItems);
	if(%questItemsLen > 0)
	{
		// CRITICAL: Normalize QuestItems - remove leading spaces but ensure trailing space
		// Remove leading spaces
		while(String::len(%questItems) > 0 && String::getSubStr(%questItems, 0, 1) == " ")
		{
			%questItems = String::getSubStr(%questItems, 1, 99999);
		}
		// Ensure trailing space exists (for proper spacing when items are added later)
		%len = String::len(%questItems);
		if(%len > 0 && String::getSubStr(%questItems, %len-1, 1) != " ")
		{
			%questItems = %questItems @ " ";
		}
		
		// Validate string format: should be "item1 count1 item2 count2 ... "
		%wordCount = 0;
		for(%i = 0; GetWord(%questItems, %i) != -1; %i++)
			%wordCount++;
		
		// BELT CORRUPTION CHECKS DISABLED - Only logging, no fixes
		// QuestItems should have even number of words (pairs of item+count)
		// Note: trailing space doesn't count as a word, so wordCount should still be even
		// if(%wordCount % 2 != 0)
		// {
		// 	%clientName = Client::getName(%clientId);
		// 	if(%clientName == "") %clientName = "Unknown";
		// 	echo("ERROR: SaveCharacter - QuestItems has odd number of words (" @ %wordCount @ "), may be corrupted!");
		// 	echo("  ClientId: " @ %clientId @ " (" @ %clientName @ ")");
		// 	echo("  QuestItems: '" @ %questItems @ "'");
		// 	// CORRUPTION FIXES DISABLED - Only logging
		// 	// Log the corruption but don't attempt to fix
		// 	%originalQuestItems = %questItems;
		// 	Belt::LogCorruptionFix(%clientId, "QuestItems", %originalQuestItems, %originalQuestItems);
		// 	// Keep original - don't modify
		// 	// %fixedQuestItems = Belt::FixCorruptedList(%questItems);
		// 	// if(%fixedQuestItems != "" && %fixedQuestItems != "0")
		// 	// {
		// 	// 	%questItems = %fixedQuestItems;
		// 	// 	// Normalize the fixed list too - remove leading spaces, ensure trailing space
		// 	// 	while(String::len(%questItems) > 0 && String::getSubStr(%questItems, 0, 1) == " ")
		// 	// 	{
		// 	// 		%questItems = String::getSubStr(%questItems, 1, 99999);
		// 	// 	}
		// 	// 	%len = String::len(%questItems);
		// 	// 	if(%len > 0 && String::getSubStr(%questItems, %len-1, 1) != " ")
		// 	// 	{
		// 	// 		%questItems = %questItems @ " ";
		// 	// 	}
		// 	// 	echo("  Fixed QuestItems: '" @ %questItems @ "'");
		// 	// 	Belt::LogCorruptionFix(%clientId, "QuestItems", %originalQuestItems, %fixedQuestItems);
		// 	// }
		// 	// else
		// 	// {
		// 	// 	// Fix failed - set to empty string (like field 15)
		// 	// 	echo("  WARNING: Fix failed, setting to empty string (like field 15)");
		// 	// 	%questItems = "";
		// 	// }
		// }
	}
	
	// Save QuestItems directly (like field 15 - empty string when empty, persistence system may remove column)
	$funk::var["[\"" @ %name @ "\", 0, 35]"] = %questItems;
	//echo("DEBUG SaveCharacter: QuestItems = '" @ %questItems @ "'");
	
	%keyItems = fetchData(%clientId, "KeyItems");
	// Match field 15 behavior: if empty, save as empty string (persistence system may remove column)
	if(%keyItems == "" || %keyItems == " " || %keyItems == "0")
		%keyItems = "";
	
	// CRITICAL: Normalize KeyItems - remove leading spaces but ensure trailing space
	// This ensures proper spacing when items are added later and prevents corruption
	// Pattern matches field 15 (spawnStuff) - equipped items have trailing space
	if(%keyItems != "")
	{
		// Remove leading spaces
		while(String::len(%keyItems) > 0 && String::getSubStr(%keyItems, 0, 1) == " ")
		{
			%keyItems = String::getSubStr(%keyItems, 1, 99999);
		}
		// Ensure trailing space exists (for proper spacing when items are added later)
		%len = String::len(%keyItems);
		if(%len > 0 && String::getSubStr(%keyItems, %len-1, 1) != " ")
		{
			%keyItems = %keyItems @ " ";
		}
	}
	
	// Save KeyItems directly (like field 15 - empty string when empty, persistence system may remove column)
	$funk::var["[\"" @ %name @ "\", 0, 36]"] = %keyItems;
	//echo("DEBUG SaveCharacter: KeyItems = '" @ %keyItems @ "'");
	
	%consumables = fetchData(%clientId, "Consumables");
	// Match field 15 behavior: if empty, save as empty string (persistence system may remove column)
	if(%consumables == "" || %consumables == " " || %consumables == "0")
		%consumables = "";
	
	// CRITICAL: Normalize Consumables - remove leading spaces but ensure trailing space
	// This ensures proper spacing when items are added later and prevents corruption
	// Pattern matches field 15 (spawnStuff) - equipped items have trailing space
	if(%consumables != "")
	{
		// Remove leading spaces
		while(String::len(%consumables) > 0 && String::getSubStr(%consumables, 0, 1) == " ")
		{
			%consumables = String::getSubStr(%consumables, 1, 99999);
		}
		// Ensure trailing space exists (for proper spacing when items are added later)
		%len = String::len(%consumables);
		if(%len > 0 && String::getSubStr(%consumables, %len-1, 1) != " ")
		{
			%consumables = %consumables @ " ";
		}
	}
	
	// Save Consumables directly (like field 15 - empty string when empty, persistence system may remove column)
	$funk::var["[\"" @ %name @ "\", 0, 37]"] = %consumables;
	//echo("DEBUG SaveCharacter: Consumables = '" @ %consumables @ "'");
	
	%armor = fetchData(%clientId, "Armor");
	// Match field 15 behavior: if empty, save as empty string (persistence system may remove column)
	if(%armor == "" || %armor == " " || %armor == "0")
		%armor = "";
	
	// CRITICAL: Normalize Armor - remove leading spaces but ensure trailing space
	// This ensures proper spacing when items are added later and prevents corruption
	// Pattern matches field 15 (spawnStuff) - equipped items have trailing space
	if(%armor != "")
	{
		// Remove leading spaces
		while(String::len(%armor) > 0 && String::getSubStr(%armor, 0, 1) == " ")
		{
			%armor = String::getSubStr(%armor, 1, 99999);
		}
		// Ensure trailing space exists (for proper spacing when items are added later)
		%len = String::len(%armor);
		if(%len > 0 && String::getSubStr(%armor, %len-1, 1) != " ")
		{
			%armor = %armor @ " ";
		}
	}
	
	// Save Armor directly (like field 15 - empty string when empty, persistence system may remove column)
	$funk::var["[\"" @ %name @ "\", 0, 48]"] = %armor;
	//echo("DEBUG SaveCharacter: Armor = '" @ %armor @ "'");
	
	%accessories = fetchData(%clientId, "Accessories");
	// Match field 15 behavior: if empty, save as empty string (persistence system may remove column)
	if(%accessories == "" || %accessories == " " || %accessories == "0")
		%accessories = "";
	
	// CRITICAL: Normalize Accessories - remove leading spaces but ensure trailing space
	// This ensures proper spacing when items are added later and prevents corruption
	// Pattern matches field 15 (spawnStuff) - equipped items have trailing space
	if(%accessories != "")
	{
		// Remove leading spaces
		while(String::len(%accessories) > 0 && String::getSubStr(%accessories, 0, 1) == " ")
		{
			%accessories = String::getSubStr(%accessories, 1, 99999);
		}
		// Ensure trailing space exists (for proper spacing when items are added later)
		%len = String::len(%accessories);
		if(%len > 0 && String::getSubStr(%accessories, %len-1, 1) != " ")
		{
			%accessories = %accessories @ " ";
		}
	}
	
	// Save Accessories directly (like field 15 - empty string when empty, persistence system may remove column)
	$funk::var["[\"" @ %name @ "\", 0, 49]"] = %accessories;
	//echo("DEBUG SaveCharacter: Accessories = '" @ %accessories @ "'");
	
	%other = fetchData(%clientId, "Other");
	// Match field 15 behavior: if empty, save as empty string (persistence system may remove column)
	if(%other == "" || %other == " " || %other == "0")
		%other = "";
	
	// CRITICAL: Normalize Other - remove leading spaces but ensure trailing space
	// This ensures proper spacing when items are added later and prevents corruption
	// Pattern matches field 15 (spawnStuff) - equipped items have trailing space
	if(%other != "")
	{
		// Remove leading spaces
		while(String::len(%other) > 0 && String::getSubStr(%other, 0, 1) == " ")
		{
			%other = String::getSubStr(%other, 1, 99999);
		}
		// Ensure trailing space exists (for proper spacing when items are added later)
		%len = String::len(%other);
		if(%len > 0 && String::getSubStr(%other, %len-1, 1) != " ")
		{
			%other = %other @ " ";
		}
	}
	
	// Save Other directly (like field 15 - empty string when empty, persistence system may remove column)
	$funk::var["[\"" @ %name @ "\", 0, 50]"] = %other;
	//echo("DEBUG SaveCharacter: Other = '" @ %other @ "'");
	
	// Save Equipped Belt Armor (field 51)
	%equippedBeltArmor = fetchData(%clientId, "EquippedBeltArmor");
	if(%equippedBeltArmor == "" || %equippedBeltArmor == "0" || %equippedBeltArmor == -1)
		%equippedBeltArmor = "";
	$funk::var["[\"" @ %name @ "\", 0, 51]"] = %equippedBeltArmor;
	//echo("DEBUG SaveCharacter: EquippedBeltArmor = '" @ %equippedBeltArmor @ "'");
	
	// Save Equipped Belt Accessories (field 52) - space-separated list
	%equippedBeltAccessories = fetchData(%clientId, "EquippedBeltAccessories");
	if(%equippedBeltAccessories == "" || %equippedBeltAccessories == "0" || %equippedBeltAccessories == -1)
		%equippedBeltAccessories = "";
	$funk::var["[\"" @ %name @ "\", 0, 52]"] = %equippedBeltAccessories;
	//echo("DEBUG SaveCharacter: EquippedBeltAccessories = '" @ %equippedBeltAccessories @ "'");
	
	// Save equipped off-hand weapon for dual wielding (field 53)
	%offHandWeapon = fetchData(%clientId, "DualWield_OffHandWeapon");
	if(%offHandWeapon == "" || %offHandWeapon == "0" || %offHandWeapon == -1)
		%offHandWeapon = "";
	$funk::var["[\"" @ %name @ "\", 0, 53]"] = %offHandWeapon;
	
	// Save Ascension talents (field 54)
	%ascTalents = fetchData(%clientId, "AscensionTalents");
	if(%ascTalents == "" || %ascTalents == "0" || %ascTalents == -1)
		%ascTalents = "";
	$funk::var["[\"" @ %name @ "\", 0, 54]"] = %ascTalents;
	
	// Save AutoSkill priority list (field 55)
	%autoSkillPriority = fetchData(%clientId, "AutoSkill_Priority");
	if(%autoSkillPriority == "" || %autoSkillPriority == "0" || %autoSkillPriority == -1)
		%autoSkillPriority = "";
	$funk::var["[\"" @ %name @ "\", 0, 55]"] = %autoSkillPriority;
	
	// Save AutoParty enabled state (field 56)
	%autoPartyEnabled = fetchData(%clientId, "AutoParty_Enabled");
	if(%autoPartyEnabled == "" || %autoPartyEnabled == "0" || %autoPartyEnabled == -1)
		%autoPartyEnabled = "";
	$funk::var["[\"" @ %name @ "\", 0, 56]"] = %autoPartyEnabled;
	

	// Sync StoredQuestItems and StoredKeyItems from BeltStorage before saving
	// This ensures saved data matches what's in bank storage
	// Validate and clean data to prevent negative values from being saved
	%beltStorage = fetchData(%clientId, "BeltStorage");
	//echo("DEBUG SaveCharacter: BeltStorage = '" @ %beltStorage @ "' (Player: " @ Client::getName(%clientId) @ ")");
	
	// CRITICAL: Check for corruption in BeltStorage before processing
	%bankStorageCleaned = "";
	%clientName = Client::getName(%clientId);
	if(%clientName == "") %clientName = "Unknown";
	if(%beltStorage != "" && %beltStorage != "0")
	{
		// Check for corruption (merged count+item patterns like "36CrystalBastardSword")
		%wordCount = 0;
		%hasCorruption = false;
		for(%i = 0; GetWord(%beltStorage, %i) != -1; %i++)
		{
			%wordCount++;
			%word = GetWord(%beltStorage, %i);
			// Check if word starts with digit and contains letters (merged pattern like "36CrystalBastardSword")
			%firstChar = String::getSubStr(%word, 0, 1);
			if(String::findSubStr("0123456789", %firstChar) != -1 && String::len(%word) > 1)
			{
				// Check if it contains letters (not just digits)
				%hasLetters = false;
				for(%j = 1; %j < String::len(%word); %j++)
				{
					%char = String::getSubStr(%word, %j, 1);
					if(String::findSubStr("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", %char) != -1)
					{
						%hasLetters = true;
						break;
					}
				}
				if(%hasLetters)
					%hasCorruption = true;
			}
		}
		
		// CORRUPTION FIXES DISABLED - Only logging
		// If odd word count or merged patterns detected, log corruption but don't fix
		%originalBankStorage = %beltStorage;
		if(%wordCount % 2 != 0 || %hasCorruption)
		{
			// Log the corruption but don't attempt to fix
			Belt::LogCorruptionFix(%clientId, "BeltStorage (affects stored categories: fields 38,39,42,43,45,46)", %originalBankStorage, %originalBankStorage);
			// Keep original - don't modify
			// %bankStorageFixed = Belt::FixCorruptedList(%beltStorage);
			// // CRITICAL: If FixCorruptedList returns empty or invalid, keep the original to prevent data loss
			// if(%bankStorageFixed != "" && %bankStorageFixed != "0")
			// {
			// 	%beltStorage = %bankStorageFixed;
			// 	if(%beltStorage != %originalBankStorage)
			// 		Belt::LogCorruptionFix(%clientId, "BeltStorage (affects stored categories: fields 38,39,42,43,45,46)", %originalBankStorage, %beltStorage);
			// }
			// else
			// {
			// 	// Fix failed - keep original and log warning
			// 	echo("WARNING: SaveCharacter - Belt::FixCorruptedList() returned empty for BankStorage, keeping original to prevent data loss");
			// 	echo("  ClientId: " @ %clientId @ " (" @ %clientName @ ")");
			// 	echo("  Original BankStorage: '" @ %originalBankStorage @ "'");
			// 	// Don't log as corruption fix since we're keeping the original
			// }
		}
		
		// Now parse and rebuild with proper spacing using Belt::AddToList()
		// CRITICAL: Only parse if beltStorage is not empty
		if(%beltStorage != "" && %beltStorage != "0")
		{
			for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
			{
				%itemName = GetWord(%beltStorage, %i);
				%itemCount = GetWord(%beltStorage, %i + 1);
				%countNum = %itemCount * 1;
				if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
					%bankStorageCleaned = Belt::AddToList(%bankStorageCleaned, %itemName @ " " @ %itemCount);
			}
		}
		
		// CRITICAL: If cleaned result is empty but original wasn't, keep the original to prevent data loss
		if(%bankStorageCleaned == "" && %originalBankStorage != "" && %originalBankStorage != "0")
		{
			echo("WARNING: SaveCharacter - BankStorage parsing resulted in empty string, keeping original to prevent data loss");
			echo("  ClientId: " @ %clientId @ " (" @ %clientName @ ")");
			echo("  Original BankStorage: '" @ %originalBankStorage @ "'");
			echo("  Attempted to parse: '" @ %beltStorage @ "'");
			%bankStorageCleaned = %originalBankStorage; // Keep original to prevent data loss
		}
		
		// CRITICAL: Remove trailing space from BankStorage (storage items don't have trailing spaces)
		// Belt::AddToList() adds trailing space for equipped items, but BankStorage is storage
		%len = String::len(%bankStorageCleaned);
		while(%len > 0 && String::getSubStr(%bankStorageCleaned, %len-1, 1) == " ")
		{
			%bankStorageCleaned = String::getSubStr(%bankStorageCleaned, 0, %len-1);
			%len = String::len(%bankStorageCleaned);
		}
		
		// Normalize both strings for comparison (remove all extra spaces, including double spaces and trailing spaces)
		// This prevents false positives from whitespace-only differences
		%originalNormalized = %originalBankStorage;
		// Remove trailing spaces
		%originalLen = String::len(%originalNormalized);
		while(%originalLen > 0 && String::getSubStr(%originalNormalized, %originalLen-1, 1) == " ")
		{
			%originalNormalized = String::getSubStr(%originalNormalized, 0, %originalLen-1);
			%originalLen = String::len(%originalNormalized);
		}
		// Remove double spaces (replace "  " with " ")
		while(String::findSubStr(%originalNormalized, "  ") != -1)
		{
			%originalNormalized = String::replace(%originalNormalized, "  ", " ");
		}
		
		%cleanedNormalized = %bankStorageCleaned;
		// Remove trailing spaces
		%cleanedLen = String::len(%cleanedNormalized);
		while(%cleanedLen > 0 && String::getSubStr(%cleanedNormalized, %cleanedLen-1, 1) == " ")
		{
			%cleanedNormalized = String::getSubStr(%cleanedNormalized, 0, %cleanedLen-1);
			%cleanedLen = String::len(%cleanedNormalized);
		}
		// Remove double spaces (replace "  " with " ")
		while(String::findSubStr(%cleanedNormalized, "  ") != -1)
		{
			%cleanedNormalized = String::replace(%cleanedNormalized, "  ", " ");
		}
		
		// Only log if normalized strings differ (indicates actual corruption, not just whitespace normalization)
		if(%originalNormalized != %cleanedNormalized && %originalBankStorage != "" && %originalBankStorage != "0" && %bankStorageCleaned != "")
			Belt::LogCorruptionFix(%clientId, "BeltStorage (affects stored categories: fields 38,39,42,43,45,46)", %originalBankStorage, %bankStorageCleaned);
		
		// Use cleaned version for further processing
		if(%bankStorageCleaned != "")
			%beltStorage = %bankStorageCleaned;
	}
	
	// Normalize "0" to empty string - "0" is not a valid belt storage value
	// If BeltStorage is just "0", it will be parsed as item='0', count='-1' (where -1 is end of string)
	// CRITICAL: If BeltStorage is empty but stored categories have data, rebuild BeltStorage from stored categories
	// This prevents data loss when BeltStorage gets cleared but stored categories still have items
	if(%beltStorage == "0" || %beltStorage == " " || %beltStorage == "")
	{
		//echo("DEBUG SaveCharacter: BeltStorage was '0'/empty, checking stored categories...");
		%storedQuestCheck = fetchData(%clientId, "StoredQuestItems");
		%storedKeyCheck = fetchData(%clientId, "StoredKeyItems");
		%storedConsumablesCheck = fetchData(%clientId, "StoredConsumables");
		%storedArmorCheck = fetchData(%clientId, "StoredArmor");
		%storedAccessoriesCheck = fetchData(%clientId, "StoredAccessories");
		%storedOtherCheck = fetchData(%clientId, "StoredOther");
		
		// Normalize "0" to empty string for checks
		if(%storedQuestCheck == "0" || %storedQuestCheck == " ") %storedQuestCheck = "";
		if(%storedKeyCheck == "0" || %storedKeyCheck == " ") %storedKeyCheck = "";
		if(%storedConsumablesCheck == "0" || %storedConsumablesCheck == " ") %storedConsumablesCheck = "";
		if(%storedArmorCheck == "0" || %storedArmorCheck == " ") %storedArmorCheck = "";
		if(%storedAccessoriesCheck == "0" || %storedAccessoriesCheck == " ") %storedAccessoriesCheck = "";
		if(%storedOtherCheck == "0" || %storedOtherCheck == " ") %storedOtherCheck = "";
		
		// If any stored category has data, rebuild BeltStorage from them
		// BUT: Only do this if we're NOT using the banker system (BeltStorage as single source of truth)
		// If BeltStorage is empty and we're using banker system, it means items were intentionally withdrawn
		// In that case, stored categories should also be empty (cleared during withdrawal)
		// If stored categories still have data, it means they're stale and should be cleared, not used to rebuild BeltStorage
		if(%storedQuestCheck != "" || %storedKeyCheck != "" || %storedConsumablesCheck != "" || 
		   %storedArmorCheck != "" || %storedAccessoriesCheck != "" || %storedOtherCheck != "")
		{
			// Check if this is a banker system user (has used BeltStorage recently)
			// If BeltStorage was intentionally emptied (via withdrawal), stored categories should be empty too
			// If they're not empty, they're stale and should be cleared
			// For now, we'll clear stored categories if BeltStorage is empty to prevent stale data
			echo("DEBUG SaveCharacter: BeltStorage was empty but stored categories have data - clearing stale stored categories");
			storeData(%clientId, "StoredQuestItems", "");
			storeData(%clientId, "StoredKeyItems", "");
			storeData(%clientId, "StoredConsumables", "");
			storeData(%clientId, "StoredArmor", "");
			storeData(%clientId, "StoredAccessories", "");
			storeData(%clientId, "StoredOther", "");
			%beltStorage = "";
			storeData(%clientId, "BeltStorage", "");
			// Set local variables to empty so they're saved as "0" (empty) to persistence
			%storedQuestCheck = "";
			%storedKeyCheck = "";
			%storedConsumablesCheck = "";
			%storedArmorCheck = "";
			%storedAccessoriesCheck = "";
			%storedOtherCheck = "";
		}
		else
		{
			//echo("DEBUG SaveCharacter: BeltStorage was '0'/empty, normalizing to empty string");
		%beltStorage = "";
		storeData(%clientId, "BeltStorage", "");
		}
	}
	
	%storedQuest = "";
	%storedKey = "";
	%storedConsumables = "";
	%storedArmor = "";
	%storedAccessories = "";
	%storedOther = "";
	%processedCount = 0;
	%removedCount = 0;
	
	// CRITICAL: If we cleared stored categories above (because BeltStorage was empty), 
	// ensure the local variables stay empty so they're saved as "0" to persistence
	// This prevents them from being repopulated from stale data
	if(%beltStorage == "" || %beltStorage == "0" || %beltStorage == " ")
	{
		%storedQuest = "";
		%storedKey = "";
		%storedConsumables = "";
		%storedArmor = "";
		%storedAccessories = "";
		%storedOther = "";
	}
	
	if(%beltStorage != "")
	{
		for(%i = 0; (%item = GetWord(%beltStorage, %i)) != -1; %i += 2)
		{
			%count = GetWord(%beltStorage, %i + 1);
			// Convert count to numeric to properly validate negative values
			%countNum = %count * 1;
			//echo("DEBUG SaveCharacter:   Processing belt item: item='" @ %item @ "', count='" @ %count @ "', countNum=" @ %countNum);
			
			// Only process valid entries (item is not empty/"0" and count is positive)
			if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %countNum > 0)
			{
				// Check if it's a belt item first (uses $BeltItem), then fall back to $AccessoryVar
				%category = $BeltItem[%item, "Type"];
				if(%category == "" || %category == -1)
					%category = $AccessoryVar[%item, $Category];
				if(%category == "" || %category == -1)
					%category = "QuestItems"; // Default category
				
				//echo("DEBUG SaveCharacter:   Processing item='" @ %item @ "', count=" @ %count @ ", category='" @ %category @ "'");
				
				// CRITICAL: Use Belt::AddToList to ensure proper spacing and prevent corruption
				if(%category == "QuestItems")
				{
					%storedQuest = Belt::AddToList(%storedQuest, %item @ " " @ %count);
					//echo("DEBUG SaveCharacter:     Added to StoredQuestItems: item='" @ %item @ "', count=" @ %count);
				}
				else if(%category == "KeyItems")
				{
					%storedKey = Belt::AddToList(%storedKey, %item @ " " @ %count);
					//echo("DEBUG SaveCharacter:     Added to StoredKeyItems: item='" @ %item @ "', count=" @ %count);
				}
				else if(%category == "Consumables")
				{
					%storedConsumables = Belt::AddToList(%storedConsumables, %item @ " " @ %count);
					//echo("DEBUG SaveCharacter:     Added to StoredConsumables: item='" @ %item @ "', count=" @ %count @ ", category='" @ %category @ "'");
				}
				else if(%category == "Armor")
				{
					%storedArmor = Belt::AddToList(%storedArmor, %item @ " " @ %count);
					//echo("DEBUG SaveCharacter:     Added to StoredArmor");
				}
				else if(%category == "Accessories")
				{
					%storedAccessories = Belt::AddToList(%storedAccessories, %item @ " " @ %count);
					//echo("DEBUG SaveCharacter:     Added to StoredAccessories");
				}
				else if(%category == "Other")
				{
					%storedOther = Belt::AddToList(%storedOther, %item @ " " @ %count);
					//echo("DEBUG SaveCharacter:     Added to StoredOther");
				}
				%processedCount++;
			}
			else
			{
				//echo("DEBUG SaveCharacter:     REMOVED invalid entry: item='" @ %item @ "', count='" @ %count @ "'");
				%removedCount++;
			}
		}
	}
	
	// Always rebuild and save cleaned BeltStorage, even if no items were removed
	// This ensures BeltStorage is always clean and matches the stored categories
	%cleanedBeltStorage = "";
	if(%storedQuest != "")
		%cleanedBeltStorage = %storedQuest;
	if(%storedKey != "")
	{
		if(%cleanedBeltStorage != "")
			%cleanedBeltStorage = %cleanedBeltStorage @ " " @ %storedKey;
		else
			%cleanedBeltStorage = %storedKey;
	}
	if(%storedConsumables != "")
	{
		if(%cleanedBeltStorage != "")
			%cleanedBeltStorage = %cleanedBeltStorage @ " " @ %storedConsumables;
		else
			%cleanedBeltStorage = %storedConsumables;
	}
	if(%storedArmor != "")
	{
		if(%cleanedBeltStorage != "")
			%cleanedBeltStorage = %cleanedBeltStorage @ " " @ %storedArmor;
		else
			%cleanedBeltStorage = %storedArmor;
	}
	if(%storedAccessories != "")
	{
		if(%cleanedBeltStorage != "")
			%cleanedBeltStorage = %cleanedBeltStorage @ " " @ %storedAccessories;
		else
			%cleanedBeltStorage = %storedAccessories;
	}
	if(%storedOther != "")
	{
		if(%cleanedBeltStorage != "")
			%cleanedBeltStorage = %cleanedBeltStorage @ " " @ %storedOther;
		else
			%cleanedBeltStorage = %storedOther;
	}
	// CRITICAL: BeltStorage is storage (like field 16 BankStorage) - should NOT have trailing space
	// Storage items format: "item1 count1 item2 count2" (no trailing space, unlike equipped items)
	// When empty, save as empty string (like field 16) - persistence system may remove column
	// Do NOT add trailing space - storage items don't have trailing spaces
	if(%cleanedBeltStorage == "" || %cleanedBeltStorage == "0")
		%cleanedBeltStorage = "";
	storeData(%clientId, "BeltStorage", %cleanedBeltStorage);
	if(%removedCount > 0)
		//echo("DEBUG SaveCharacter: Removed " @ %removedCount @ " invalid entries from belt storage");
	//echo("DEBUG SaveCharacter: Cleaned BeltStorage saved = '" @ %cleanedBeltStorage @ "'");
	
	// Update stored data if it changed (or clear if BeltStorage is empty)
	// Always update to ensure sync, even if empty (prevents stale data)
	storeData(%clientId, "StoredQuestItems", %storedQuest);
	storeData(%clientId, "StoredKeyItems", %storedKey);
	storeData(%clientId, "StoredConsumables", %storedConsumables);
	storeData(%clientId, "StoredArmor", %storedArmor);
	storeData(%clientId, "StoredAccessories", %storedAccessories);
	storeData(%clientId, "StoredOther", %storedOther);
	//echo("DEBUG SaveCharacter: StoredQuestItems = '" @ %storedQuest @ "'");
	//echo("DEBUG SaveCharacter: StoredKeyItems = '" @ %storedKey @ "'");
	//echo("DEBUG SaveCharacter: StoredConsumables = '" @ %storedConsumables @ "'");
	//echo("DEBUG SaveCharacter: StoredArmor = '" @ %storedArmor @ "'");
	//echo("DEBUG SaveCharacter: StoredAccessories = '" @ %storedAccessories @ "'");
	//echo("DEBUG SaveCharacter: StoredOther = '" @ %storedOther @ "'");
	//echo("DEBUG SaveCharacter: Processed " @ %processedCount @ " items from BeltStorage='" @ %beltStorage @ "'");
	//echo("DEBUG SaveCharacter: StoredArmor = '" @ %storedArmor @ "'");
	//echo("DEBUG SaveCharacter: StoredAccessories = '" @ %storedAccessories @ "'");
	//echo("DEBUG SaveCharacter: StoredOther = '" @ %storedOther @ "'");
	//echo("DEBUG SaveCharacter: StoredKeyItems = '" @ %storedKey @ "'");
	
	// Always save these variables, even if empty (use "0" for empty to match original inventory system)
	// Use the local variables we just set, not fetchData (which may return "0" for empty)
	%storedQuestSave = %storedQuest;
	%storedKeySave = %storedKey;
	%storedConsumablesSave = %storedConsumables;
	%storedArmorSave = %storedArmor;
	%storedAccessoriesSave = %storedAccessories;
	%storedOtherSave = %storedOther;
	
	// CRITICAL: Storage items should NOT have trailing spaces (unlike equipped items)
	// Remove trailing spaces from all storage items before saving
	%len = String::len(%storedQuestSave);
	while(%len > 0 && String::getSubStr(%storedQuestSave, %len-1, 1) == " ")
	{
		%storedQuestSave = String::getSubStr(%storedQuestSave, 0, %len-1);
		%len = String::len(%storedQuestSave);
	}
	%len = String::len(%storedKeySave);
	while(%len > 0 && String::getSubStr(%storedKeySave, %len-1, 1) == " ")
	{
		%storedKeySave = String::getSubStr(%storedKeySave, 0, %len-1);
		%len = String::len(%storedKeySave);
	}
	%len = String::len(%storedConsumablesSave);
	while(%len > 0 && String::getSubStr(%storedConsumablesSave, %len-1, 1) == " ")
	{
		%storedConsumablesSave = String::getSubStr(%storedConsumablesSave, 0, %len-1);
		%len = String::len(%storedConsumablesSave);
	}
	%len = String::len(%storedArmorSave);
	while(%len > 0 && String::getSubStr(%storedArmorSave, %len-1, 1) == " ")
	{
		%storedArmorSave = String::getSubStr(%storedArmorSave, 0, %len-1);
		%len = String::len(%storedArmorSave);
	}
	%len = String::len(%storedAccessoriesSave);
	while(%len > 0 && String::getSubStr(%storedAccessoriesSave, %len-1, 1) == " ")
	{
		%storedAccessoriesSave = String::getSubStr(%storedAccessoriesSave, 0, %len-1);
		%len = String::len(%storedAccessoriesSave);
	}
	%len = String::len(%storedOtherSave);
	while(%len > 0 && String::getSubStr(%storedOtherSave, %len-1, 1) == " ")
	{
		%storedOtherSave = String::getSubStr(%storedOtherSave, 0, %len-1);
		%len = String::len(%storedOtherSave);
	}
	
	// Normalize empty strings to "0" to match original inventory system behavior (fields 11-14 use "0" for empty)
	if(%storedQuestSave == "" || %storedQuestSave == " " || %storedQuestSave == "0")
		%storedQuestSave = "0";
	if(%storedKeySave == "" || %storedKeySave == " " || %storedKeySave == "0")
		%storedKeySave = "0";
	if(%storedConsumablesSave == "" || %storedConsumablesSave == " " || %storedConsumablesSave == "0")
		%storedConsumablesSave = "0";
	if(%storedArmorSave == "" || %storedArmorSave == " " || %storedArmorSave == "0")
		%storedArmorSave = "0";
	if(%storedAccessoriesSave == "" || %storedAccessoriesSave == " " || %storedAccessoriesSave == "0")
		%storedAccessoriesSave = "0";
	if(%storedOtherSave == "" || %storedOtherSave == " " || %storedOtherSave == "0")
		%storedOtherSave = "0";
	
	//echo("DEBUG SaveCharacter: Saving to fields 38-39-42-43-45-46 (StoredQuestItems/StoredKeyItems/StoredConsumables/StoredArmor/StoredAccessories/StoredOther)...");
	$funk::var["[\"" @ %name @ "\", 0, 38]"] = %storedQuestSave;
	//echo("DEBUG SaveCharacter: Field 38 (StoredQuestItems) = '" @ %storedQuestSave @ "'");
	$funk::var["[\"" @ %name @ "\", 0, 39]"] = %storedKeySave;
	//echo("DEBUG SaveCharacter: Field 39 (StoredKeyItems) = '" @ %storedKeySave @ "'");
	$funk::var["[\"" @ %name @ "\", 0, 42]"] = %storedConsumablesSave;
	//echo("DEBUG SaveCharacter: Field 42 (StoredConsumables) = '" @ %storedConsumablesSave @ "'");
	$funk::var["[\"" @ %name @ "\", 0, 43]"] = %storedArmorSave;
	//echo("DEBUG SaveCharacter: Field 43 (StoredArmor) = '" @ %storedArmorSave @ "'");
	$funk::var["[\"" @ %name @ "\", 0, 45]"] = %storedAccessoriesSave;
	//echo("DEBUG SaveCharacter: Field 45 (StoredAccessories) = '" @ %storedAccessoriesSave @ "'");
	$funk::var["[\"" @ %name @ "\", 0, 46]"] = %storedOtherSave;
	//echo("DEBUG SaveCharacter: Field 46 (StoredOther) = '" @ %storedOtherSave @ "'");
	$funk::var["[\"" @ %name @ "\", 0, 44]"] = fetchData(%clientId, "Stance");
	
	// Combine all damage display preferences into a single string: "displayType:animationStyle:enabledFlag"
	%damageDisplayType = fetchData(%clientId, "damageDisplayType");
	%floatingAnimationStyle = fetchData(%clientId, "floatingAnimationStyle");
	%floatingDamageNumbers = fetchData(%clientId, "floatingDamageNumbers");
	
	// Format: "displayType:animationStyle:enabledFlag"
	// For "bottomprint" or "chat", animation style and enabled flag are empty
	// For "floating", include animation style and enabled flag
	if(%damageDisplayType == "floating")
	{
		if(%floatingAnimationStyle == "")
			%floatingAnimationStyle = "float"; // Default (changed from redmoon)
		// Convert old styles to new ones when saving
		if(%floatingAnimationStyle == "redmoon" || %floatingAnimationStyle == "wow")
			%floatingAnimationStyle = "float"; // Convert old styles to float
		if(%floatingDamageNumbers == "")
			%floatingDamageNumbers = "1"; // Default enabled
		$funk::var["[\"" @ %name @ "\", 0, 47]"] = %damageDisplayType @ ":" @ %floatingAnimationStyle @ ":" @ %floatingDamageNumbers;
	}
	else
	{
		// For "bottomprint" or "chat", just store the display type (backward compatible)
		if(%damageDisplayType == "")
			%damageDisplayType = "bottomprint"; // Default
		$funk::var["[\"" @ %name @ "\", 0, 47]"] = %damageDisplayType @ "::";
	}

	//echo("DEBUG SaveCharacter: Saving skill variables...");
	//skill variables
	%cnt = 0;
	for(%i = 1; %i <= GetNumSkills(); %i++)
	{
		$funk::var["[\"" @ %name @ "\", 4, " @ %cnt++ @ "]"] = $PlayerSkill[%clientId, %i];
		$funk::var["[\"" @ %name @ "\", 4, " @ %cnt++ @ "]"] = $SkillCounter[%clientId, %i];
	}
	//echo("DEBUG SaveCharacter: Saved " @ GetNumSkills() @ " skills");

	//IP dump, for server admin look-up purposes
	$funk::var["[\"" @ %name @ "\", 0, 666]"] = Client::getTransportAddress(%clientId);

	%ii = 0;

	//determine which weapons player has

	%player = Client::getOwnedObject(%clientId);
	if(!IsDead(%clientId) && %player != -1 && %player != "")
	{
		%s = "";
		%max = getNumItems();
		for(%i = 0; %i < %max; %i++)
		{
			// Re-validate player object in case it gets despawned during the loop
			%player = Client::getOwnedObject(%clientId);
			if(%player == -1 || %player == "")
				break; // Exit loop if player object no longer exists
			
			%checkItem = getItemData(%i);
			// CRITICAL: Never save flags to character data - flags are mission objectives, not player inventory
			// If a player disconnects/crashes while carrying a flag, Flag::clientDropped should handle dropping it
			// But we must prevent flags from being saved to avoid issues on reload
			if(%checkItem == "Flag" || %checkItem == Flag)
				continue;
			
			// CRITICAL: Re-validate player object before calling SafeGetItemCount
			%playerCheck = Client::getOwnedObject(%clientId);
			if(%playerCheck == -1 || %playerCheck == "")
				break; // Exit loop if player object no longer exists
			
			%itemcount = SafeGetItemCount(%clientId, %checkItem, "SaveCharacter");
			if(%itemcount > $maxItem)
				%itemcount = $maxItem;
			if(%itemcount > 0)
				%s = %s @ %checkItem @ " " @ %itemcount @ " ";
		}
		$funk::var["[\"" @ %name @ "\", 0, 15]"] = %s;
	}
	else
		$funk::var["[\"" @ %name @ "\", 0, 15]"] = fetchData(%clientId, "spawnStuff");

	%cnt = 0;
	%list = GetBotIdList();
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		%aiName = fetchData(%id, "BotInfoAiName");

		if($QuestCounter[%name, %aiName] != "")
		{
			%cnt++;
			$funk::var["[\"" @ %name @ "\", 2, " @ %cnt @ "]"] = %aiName;
			$funk::var["[\"" @ %name @ "\", 3, " @ %cnt @ "]"] = $QuestCounter[%name, %aiName];
		}
	}

	//bonus state variables
	for(%i = 1; %i <= $maxBonusStates; %i++)
	{
		$funk::var["[\"" @ %name @ "\", 5, " @ %i @ "]"] = $BonusState[%clientId, %i];
		$funk::var["[\"" @ %name @ "\", 6, " @ %i @ "]"] = $BonusStateCnt[%clientId, %i];
	}
	

	File::delete("temp\\" @ %name @ ".cs");

	export("funk::var[\"" @ %name @ "\",*", "temp\\" @ %name @ ".cs", false);
	ClearFunkVar(%name);
	echo("Save for " @ %name @ " (" @ %clientId @ ") complete.");

	return True;
}

function SaveAllCharacters()
{
	dbecho($dbechoMode, "SaveAllCharacters()");
	
	%list = GetPlayerIdList();
	%count = 0;
	%delay = 0;
	
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		
		// CRITICAL: Skip bots - SaveCharacter already has checks, but prevent scheduled calls for bots
		// This prevents scheduled SaveCharacter calls from executing on deleted bots
		if(isRPGAI(%id))
		{
			// Skip bots - they shouldn't be saved
			continue;
		}
		
		// Save character with a small delay to stagger saves and reduce server load
		schedule("SaveCharacter(" @ %id @ ");", %delay, %id);
		%delay += 0.1;
		%count++;
	}
	
	if(%count > 0)
		echo("Saving all " @ %count @ " players...");
	else
		echo("No players to save.");
}

function LoadCharacter(%clientId)
{
	dbecho($dbechoMode2, "LoadCharacter(" @ %clientId @ ")");

	// CRITICAL: Prevent bots from loading character save files
	// Bots should never load save files - they get their data from $BotInfo and spawn parameters
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	if((%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1) || 
	   (%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1))
	{
		%botType = "Unknown";
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
			%botType = "Enemy Bot";
		else if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
			%botType = "Town Bot";
		
		echo("ERROR: LoadCharacter - Attempted to load character save file for " @ %botType @ " (clientId=" @ %clientId @ ", BotInfoAiName='" @ %botInfoAiName @ "', SpawnBotInfo='" @ %spawnBotInfo @ "')");
		echo("  Bots should NEVER load character save files - they get their data from $BotInfo and spawn parameters");
		echo("  This call is being BLOCKED to prevent bot data corruption");
		return; // Block the load
	}
	
	// Also check the old method for compatibility
	// CRITICAL: Check isRPGAI() FIRST before Player::isAiControlled() to catch bots that might not have Player::isAiControlled() set
	// This prevents bots from loading player save data, which would corrupt bot data
	if(isRPGAI(%clientId))
	{
		// Double-check: If isRPGAI() returns true but BotInfoAiName and SpawnBotInfo are empty, this might be stale data
		// Clear it and allow the load to proceed (this handles the case where a player gets a client ID that was previously used by a bot)
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if((%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0") && 
		   (%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0"))
		{
			// Stale data - clear it and allow player to load
			echo("WARNING: LoadCharacter() - isRPGAI() returned true for clientId " @ %clientId @ " but bot data is empty. Clearing stale data and allowing player load.");
			$BotInfoAiName[%clientId] = "";
			storeData(%clientId, "BotInfoAiName", "");
			storeData(%clientId, "SpawnBotInfo", "");
			// Continue with load (don't return)
		}
		else
		{
			// This is actually a bot - block the load
			echo("WARNING: LoadCharacter() called for bot " @ %clientId @ " (isRPGAI check) - bots should not load player data. Skipping load.");
			return;
		}
	}
	
	// Also check Player::isAiControlled() for additional safety
	if(Player::isAiControlled(%clientId))
	{
		echo("WARNING: LoadCharacter() called for bot " @ %clientId @ " (Player::isAiControlled check) - bots should not load player data. Skipping load.");
		return;
	}

	// Clear temporary player state variables before loading character data
	ClearPlayerVariables(%clientId);

	%name = Client::getName(%clientId);
	%filename = %name @ ".cs";

	$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;	//thanks Presto

	// CRITICAL: Clear $funk::var arrays BEFORE loading character data
	// This ensures old data doesn't interfere with character loading
	// Must be called AFTER ClearPlayerVariables() but BEFORE exec() to clear old data
	ClearFunkVar(%name);

	if(isFile("temp\\" @ %filename))
	{
		//load character
		echo("Loading character " @ %name @ " (" @ %clientId @ ")...");

		for(%retry = 0; %retry < 5; %retry++)		//This might not be necessary, but it's to ensure that the
		{								//exec doesn't get flakey when there's lag.
			exec(%filename);
			if($funk::var[%name, 0, 1] != "")
				break;
		}
		storeData(%clientId, "RACE", $funk::var[%name, 0, 1]);
		storeData(%clientId, "EXP", $funk::var[%name, 0, 2]);
		storeData(%clientId, "campPos", $funk::var[%name, 0, 3]);
		storeData(%clientId, "COINS", $funk::var[%name, 0, 4]);
		storeData(%clientId, "isMimic", $funk::var[%name, 0, 5]);
		storeData(%clientId, "BANK", $funk::var[%name, 0, 6]);
		storeData(%clientId, "tmpname", $funk::var[%name, 0, 7]);
		
		// Load and clean grouplist - remove leading "0" prefix if present (corruption fix)
		%grouplist = $funk::var[%name, 0, 8];
		if(%grouplist != "" && %grouplist != -1)
		{
			// Check if it starts with "0" followed by a letter (corruption pattern like "0Jarn,")
			%firstChar = String::getSubStr(%grouplist, 0, 1);
			%secondChar = String::getSubStr(%grouplist, 1, 1);
			if(%firstChar == "0" && %secondChar != "" && %secondChar != "," && %secondChar != " ")
			{
				// Remove leading "0" prefix
				%grouplist = String::getSubStr(%grouplist, 1, 99999);
				echo("DEBUG LoadCharacter: Cleaned grouplist - removed leading '0' prefix. Original: '" @ $funk::var[%name, 0, 8] @ "', Cleaned: '" @ %grouplist @ "'");
			}
		}
		storeData(%clientId, "grouplist", %grouplist);
		// Prevent bots from having defaultTalk set to #global (security fix)
		%defaultTalkValue = $funk::var[%name, 0, 9];
		if((Player::isAiControlled(%clientId) || isRPGAI(%clientId)) && %defaultTalkValue == "#global")
		{
			// Bots should not use #global chat - force to #say instead
			%defaultTalkValue = "#say";
		}
		storeData(%clientId, "defaultTalk", %defaultTalkValue);
		storeData(%clientId, "password", $funk::var[%name, 0, 10]);
		storeData(%clientId, "bounty", $funk::var[%name, 0, 11]);
		storeData(%clientId, "inArena", $funk::var[%name, 0, 12]);
		storeData(%clientId, "PlayerInfo", $funk::var[%name, 0, 13]);
		storeData(%clientId, "deathmsg", $funk::var[%name, 0, 14]);
		// CRITICAL: Filter out flags from spawnStuff - flags are mission objectives, not player inventory
		// This prevents old saves that might have flags from loading them
		%spawnStuffRaw = $funk::var[%name, 0, 15];
		%spawnStuffCleaned = "";
		if(%spawnStuffRaw != "" && %spawnStuffRaw != "0")
		{
			for(%i = 0; GetWord(%spawnStuffRaw, %i) != -1; %i += 2)
			{
				%item = GetWord(%spawnStuffRaw, %i);
				%count = GetWord(%spawnStuffRaw, %i + 1);
				// Skip flags - they should never be in player inventory
				if(%item == "Flag" || %item == Flag)
					continue;
				// Add non-flag items to cleaned list
				if(%spawnStuffCleaned != "")
					%spawnStuffCleaned = %spawnStuffCleaned @ " " @ %item @ " " @ %count;
				else
					%spawnStuffCleaned = %item @ " " @ %count;
			}
		}
		storeData(%clientId, "spawnStuff", %spawnStuffCleaned);
		// CRITICAL: Join BankStorage from multiple fields (16, 60-63) for backward compatibility
		// Old saves only have field 16, new saves may use overflow fields 60-63
		%bankStorageRaw = JoinBankStorageFromLoad(%name);
		%bankStorageCleaned = "";
		if(%bankStorageRaw != "" && %bankStorageRaw != "0")
		{
			// Check if it starts with "0 " - if so, skip the first two words (item "0" and its count)
			%firstWord = GetWord(%bankStorageRaw, 0);
			%startIndex = 0;
			if(%firstWord == "0")
			{
				// Skip the leading "0" and its count (next word)
				%startIndex = 2;
				//echo("DEBUG LoadCharacter: BankStorage starts with '0', skipping first 2 words");
			}
			
			// Parse and clean the rest of BankStorage
			for(%i = %startIndex; GetWord(%bankStorageRaw, %i) != -1; %i += 2)
			{
				%item = GetWord(%bankStorageRaw, %i);
				%count = GetWord(%bankStorageRaw, %i + 1);
				// Skip invalid entries (item is "0" or empty, count is invalid)
				if(%item == "" || %item == -1 || %item == "0" || %count == "" || %count == -1 || %count == "0")
					continue;
				// Convert count to numeric to validate
				%countNum = %count * 1;
				if(%countNum <= 0)
					continue;
				// Add valid entries to cleaned list
				if(%bankStorageCleaned != "")
					%bankStorageCleaned = %bankStorageCleaned @ " " @ %item @ " " @ %count;
				else
					%bankStorageCleaned = %item @ " " @ %count;
			}
		}
		storeData(%clientId, "BankStorage", %bankStorageCleaned);
		// Validate campRot - handle missing values and clean "-0" values
		%campRot = $funk::var[%name, 0, 17];
		if(%campRot == "" || %campRot == -1)
		{
			%campRot = "";
			//echo("DEBUG: campRot was missing/empty, defaulting to empty string");
		}
		else
		{
			// Clean up "-0" values in rotation (replace "-0" with "0")
			// Rotation format is "x y z" (3 space-separated numbers)
			%cleanedRot = "";
			for(%i = 0; (%word = GetWord(%campRot, %i)) != -1; %i++)
			{
				// Convert "-0" to "0", and validate numeric value
				if(%word == "-0" || %word == "0")
					%word = "0";
				else
				{
					// Convert to numeric to handle any negative values properly
					%wordNum = %word * 1;
					%word = %wordNum;
				}
				
				if(%cleanedRot != "")
					%cleanedRot = %cleanedRot @ " " @ %word;
				else
					%cleanedRot = %word;
			}
			if(%cleanedRot != %campRot)
				//echo("DEBUG: campRot cleaned from '" @ %campRot @ "' to '" @ %cleanedRot @ "'");
			%campRot = %cleanedRot;
		}
		storeData(%clientId, "campRot", %campRot);
		//echo("DEBUG: campRot FINAL = '" @ %campRot @ "'");
		
		//echo("DEBUG: Loading tmphp/tmpmana (fields 18-19)...");
		// Normalize tmphp and tmpmana - convert "-0", "0", or negative values to empty string
		%tmphp = $funk::var[%name, 0, 18];
		//echo("DEBUG: tmphp RAW = '" @ %tmphp @ "'");
		if(%tmphp == "" || %tmphp == " " || %tmphp == "0" || %tmphp == "-0" || (%tmphp * 1) <= 0)
		{
			//echo("DEBUG: tmphp was invalid (empty/0/-0/negative), normalizing to empty string");
			%tmphp = "";
		}
		storeData(%clientId, "tmphp", %tmphp);
		//echo("DEBUG: tmphp FINAL = '" @ %tmphp @ "'");
		
		%tmpmana = $funk::var[%name, 0, 19];
		//echo("DEBUG: tmpmana RAW = '" @ %tmpmana @ "'");
		if(%tmpmana == "" || %tmpmana == " " || %tmpmana == "0" || %tmpmana == "-0" || (%tmpmana * 1) <= 0)
		{
			//echo("DEBUG: tmpmana was invalid (empty/0/-0/negative), normalizing to empty string");
			%tmpmana = "";
		}
		storeData(%clientId, "tmpmana", %tmpmana);
		//echo("DEBUG: tmpmana FINAL = '" @ %tmpmana @ "'");
		//echo("DEBUG: Loading character stats and properties...");
		storeData(%clientId, "LCKconsequence", $funk::var[%name, 0, 20]);
		//echo("DEBUG: LCKconsequence = '" @ $funk::var[%name, 0, 20] @ "'");
		storeData(%clientId, "RemortStep", $funk::var[%name, 0, 21]);
		//echo("DEBUG: RemortStep = '" @ $funk::var[%name, 0, 21] @ "'");
		storeData(%clientId, "LCK", $funk::var[%name, 0, 22]);
		//echo("DEBUG: LCK = '" @ $funk::var[%name, 0, 22] @ "'");
		storeData(%clientId, "tmpLastSaveVer", $funk::var[%name, 0, 23]);
		//echo("DEBUG: tmpLastSaveVer = '" @ $funk::var[%name, 0, 23] @ "'");
		storeData(%clientId, "GROUP", $funk::var[%name, 0, 26]);
		//echo("DEBUG: GROUP = '" @ $funk::var[%name, 0, 26] @ "'");
		storeData(%clientId, "CLASS", $funk::var[%name, 0, 27]);
		//echo("DEBUG: CLASS = '" @ $funk::var[%name, 0, 27] @ "'");
		storeData(%clientId, "SPcredits", $funk::var[%name, 0, 28]);
		//echo("DEBUG: SPcredits = '" @ $funk::var[%name, 0, 28] @ "'");
		// CRITICAL: Load saved mounted weapon so it can be restored on spawn
		storeData(%clientId, "savedMountedWeapon", $funk::var[%name, 0, 29]);
		
		//echo("DEBUG: Loading MyHouse (field 30)...");
		// Validate MyHouse number before array access - prevent crash if field 30 doesn't exist
		%houseNum = $funk::var[%name, 0, 30];
		//echo("DEBUG: MyHouse RAW (field 30) = '" @ %houseNum @ "'");
		// If houseNum is empty, -1, 0, or "0", treat as no house (empty string)
		// Houses start at index 1, so 0 is not a valid house number
		if(%houseNum == "" || %houseNum == -1 || %houseNum == 0 || %houseNum == "0")
		{
			//echo("DEBUG: MyHouse field was missing/empty/0, setting to empty string (no house)");
			%houseName = "";
		}
		else
		{
		%houseName = $HouseName[%houseNum];
			// Validate that houseName exists and is not empty
		if(%houseName == "" || %houseName == -1)
			{
				//echo("DEBUG: MyHouse number " @ %houseNum @ " is invalid, setting to empty string (no house)");
			%houseName = "";
			}
		}
		// CRITICAL: Always check for "House0", "0", or "house0" before storing
		// This prevents any case where "House0" might have been saved as a string
		if(%houseName == "House0" || %houseName == "0" || %houseName == "house0")
		{
			//echo("DEBUG: LoadCharacter - Detected invalid house name '" @ %houseName @ "' before storing, clearing");
			%houseName = "";
		}
		storeData(%clientId, "MyHouse", %houseName);
		//echo("DEBUG: MyHouse FINAL = '" @ %houseName @ "' (from houseNum " @ %houseNum @ ")");
		
		// CRITICAL: Double-check after storing - if somehow "House0" got through, clear it
		// This handles edge cases where the house might be set elsewhere
		%loadedHouse = fetchData(%clientId, "MyHouse");
		if(%loadedHouse == "House0" || %loadedHouse == "0" || %loadedHouse == "house0")
		{
			//echo("DEBUG: LoadCharacter - Player " @ %name @ " had invalid house '" @ %loadedHouse @ "' after load, clearing house assignment");
			storeData(%clientId, "MyHouse", "");
			// Boot them from the invalid house (this will also reset rank points and unequip items)
			BootFromCurrentHouse(%clientId);
		}
		
		storeData(%clientId, "RankPoints", $funk::var[%name, 0, 31]);
		//echo("DEBUG: RankPoints = '" @ $funk::var[%name, 0, 31] @ "'");
		storeData(%clientId, "TournyRank", $funk::var[%name, 0, 32]);
		//echo("DEBUG: TournyRank = '" @ $funk::var[%name, 0, 32] @ "'");
		
		echo("DEBUG: Loading belt items (QuestItems/KeyItems)...");
		// Normalize QuestItems and KeyItems - convert "0" to empty string
		%questItems = $funk::var[%name, 0, 35];
		echo("DEBUG: QuestItems RAW = '" @ %questItems @ "'");
		
		// BELT CORRUPTION CHECKS DISABLED - No validation or fixes
		// CRITICAL: Validate QuestItems string after loading to detect corruption
		// if(%questItems != "" && %questItems != " " && %questItems != "0")
		// {
		// 	%wordCount = 0;
		// 	for(%i = 0; GetWord(%questItems, %i) != -1; %i++)
		// 		%wordCount++;
		// 	
		// 	// QuestItems should have even number of words (pairs of item+count)
		// 	if(%wordCount % 2 != 0)
		// 	{
		// 		echo("ERROR: LoadCharacter - QuestItems has odd number of words (" @ %wordCount @ "), CORRUPTED!");
		// 		echo("  Player: " @ %name);
		// 		echo("  Corrupted QuestItems: '" @ %questItems @ "'");
		// 		echo("  This indicates data corruption in funk::var field 35");
		// 		// Set to empty to prevent further corruption
		// 		%questItems = "";
		// 	}
		// 	// Also check for suspicious patterns like "ath 1 nt 35" which suggests truncation
		// 	else if(String::findSubStr(%questItems, "ath") != -1 || String::findSubStr(%questItems, "nt") != -1)
		// 	{
		// 		// Check if this looks like truncated item names
		// 		%firstItem = GetWord(%questItems, 0);
		// 		%secondItem = GetWord(%questItems, 2);
		// 		if(String::len(%firstItem) <= 3 || String::len(%secondItem) <= 2)
		// 		{
		// 			echo("ERROR: LoadCharacter - QuestItems appears corrupted (truncated item names)!");
		// 			echo("  Player: " @ %name);
		// 			echo("  Corrupted QuestItems: '" @ %questItems @ "'");
		// 			echo("  First item: '" @ %firstItem @ "', Second item: '" @ %secondItem @ "'");
		// 			// Set to empty to prevent further corruption
		// 			%questItems = "";
		// 		}
		// 	}
		// }
		
		if(%questItems == "" || %questItems == " " || %questItems == "0")
		{
			echo("DEBUG: QuestItems was empty/'0', normalizing to empty string");
			%questItems = "";
		}
		storeData(%clientId, "QuestItems", %questItems);
		echo("DEBUG: QuestItems FINAL = '" @ %questItems @ "'");
		
		%keyItems = $funk::var[%name, 0, 36];
		echo("DEBUG: KeyItems RAW = '" @ %keyItems @ "'");
		if(%keyItems == "" || %keyItems == " " || %keyItems == "0")
		{
			echo("DEBUG: KeyItems was empty/'0', normalizing to empty string");
			%keyItems = "";
		}
		storeData(%clientId, "KeyItems", %keyItems);
		echo("DEBUG: KeyItems FINAL = '" @ %keyItems @ "'");
		
		// Load Consumables from field 37 (new field for potions)
		%consumables = $funk::var[%name, 0, 37];
		echo("DEBUG: Consumables RAW (field 37) = '" @ %consumables @ "'");
		if(%consumables == "" || %consumables == " " || %consumables == "0" || %consumables == -1)
		{
			echo("DEBUG: Consumables was empty/'0', normalizing to empty string");
			%consumables = "";
		}
		storeData(%clientId, "Consumables", %consumables);
		echo("DEBUG: Consumables FINAL = '" @ %consumables @ "'");
		
		// Load Armor from field 48 (equipped category)
		%armor = $funk::var[%name, 0, 48];
		echo("DEBUG: Armor RAW (field 48) = '" @ %armor @ "'");
		if(%armor == "" || %armor == " " || %armor == "0" || %armor == -1)
		{
			echo("DEBUG: Armor was empty/'0', normalizing to empty string");
			%armor = "";
		}
		storeData(%clientId, "Armor", %armor);
		echo("DEBUG: Armor FINAL = '" @ %armor @ "'");
		
		// Load Accessories from field 49 (equipped category)
		%accessories = $funk::var[%name, 0, 49];
		echo("DEBUG: Accessories RAW (field 49) = '" @ %accessories @ "'");
		if(%accessories == "" || %accessories == " " || %accessories == "0" || %accessories == -1)
		{
			echo("DEBUG: Accessories was empty/'0', normalizing to empty string");
			%accessories = "";
		}
		storeData(%clientId, "Accessories", %accessories);
		echo("DEBUG: Accessories FINAL = '" @ %accessories @ "'");
		
		// Load Other from field 50 (equipped category)
		%other = $funk::var[%name, 0, 50];
		echo("DEBUG: Other RAW (field 50) = '" @ %other @ "'");
		if(%other == "" || %other == " " || %other == "0" || %other == -1)
		{
			echo("DEBUG: Other was empty/'0', normalizing to empty string");
			%other = "";
		}
		storeData(%clientId, "Other", %other);
		echo("DEBUG: Other FINAL = '" @ %other @ "'");
		
		// Load Equipped Belt Armor from field 51
		%equippedBeltArmor = $funk::var[%name, 0, 51];
		echo("DEBUG: EquippedBeltArmor RAW (field 51) = '" @ %equippedBeltArmor @ "'");
		if(%equippedBeltArmor == "" || %equippedBeltArmor == " " || %equippedBeltArmor == "0" || %equippedBeltArmor == -1)
		{
			echo("DEBUG: EquippedBeltArmor was empty/'0', normalizing to empty string");
			%equippedBeltArmor = "";
		}
		storeData(%clientId, "EquippedBeltArmor", %equippedBeltArmor);
		echo("DEBUG: EquippedBeltArmor FINAL = '" @ %equippedBeltArmor @ "'");
		
		// Load Equipped Belt Accessories from field 52 (space-separated list)
		%equippedBeltAccessories = $funk::var[%name, 0, 52];
		echo("DEBUG: EquippedBeltAccessories RAW (field 52) = '" @ %equippedBeltAccessories @ "'");
		if(%equippedBeltAccessories == "" || %equippedBeltAccessories == " " || %equippedBeltAccessories == "0" || %equippedBeltAccessories == -1)
		{
			echo("DEBUG: EquippedBeltAccessories was empty/'0', normalizing to empty string");
			%equippedBeltAccessories = "";
		}
		storeData(%clientId, "EquippedBeltAccessories", %equippedBeltAccessories);
		echo("DEBUG: EquippedBeltAccessories FINAL = '" @ %equippedBeltAccessories @ "'");
		
		// Load equipped off-hand weapon for dual wielding (field 53)
		%offHandWeapon = $funk::var[%name, 0, 53];
		if(%offHandWeapon == "" || %offHandWeapon == " " || %offHandWeapon == "0" || %offHandWeapon == -1)
			%offHandWeapon = "";
		storeData(%clientId, "DualWield_OffHandWeapon", %offHandWeapon);
		
		// Load Ascension talents (field 54)
		%ascTalents = $funk::var[%name, 0, 54];
		if(%ascTalents == "" || %ascTalents == " " || %ascTalents == "0" || %ascTalents == -1)
			%ascTalents = "";
		else if(String::getSubStr(%ascTalents, 0, 2) == "0 ")
			%ascTalents = String::getSubStr(%ascTalents, 2, 99999);
		storeData(%clientId, "AscensionTalents", %ascTalents);
		
		// Load AutoSkill priority list (field 55)
		%autoSkillPriority = $funk::var[%name, 0, 55];
		if(%autoSkillPriority == "" || %autoSkillPriority == " " || %autoSkillPriority == "0" || %autoSkillPriority == -1)
			%autoSkillPriority = "";
		storeData(%clientId, "AutoSkill_Priority", %autoSkillPriority);
		
		// Load AutoParty enabled state (field 56)
		%autoPartyEnabled = $funk::var[%name, 0, 56];
		if(%autoPartyEnabled == "" || %autoPartyEnabled == " " || %autoPartyEnabled == "0" || %autoPartyEnabled == -1)
			%autoPartyEnabled = "";
		storeData(%clientId, "AutoParty_Enabled", %autoPartyEnabled);
		
		// Note: Visual re-mount happens in Game::playerSpawn via schedule

		
		echo("DEBUG: Loading stored belt items (StoredQuestItems/StoredKeyItems)...");
		// Handle StoredQuestItems and StoredKeyItems - convert space or "0" to empty string if needed
		// Also handle case where variable doesn't exist in old save files (defaults to empty)
		// Validate and clean corrupted data like "0 -1" before storing
		%storedQuest = $funk::var[%name, 0, 38];
		echo("DEBUG: StoredQuestItems RAW (field 38) = '" @ %storedQuest @ "'");
		if(%storedQuest == "" || %storedQuest == " " || %storedQuest == "0")
		{
			echo("DEBUG: StoredQuestItems was empty/'0', normalizing to empty string");
			%storedQuest = "";
		}
		else
		{
			// Clean up corrupted entries (item "0", count <= 0, negative values, etc.)
			%cleanedQuest = "";
			%removedCount = 0;
			for(%i = 0; GetWord(%storedQuest, %i) != -1; %i += 2)
			{
				%item = GetWord(%storedQuest, %i);
				%count = GetWord(%storedQuest, %i + 1);
				// Convert count to numeric to properly handle negative values like "-1" or "-0"
				%countNum = %count * 1;
				//echo("DEBUG:   Checking entry: item='" @ %item @ "', count='" @ %count @ "', countNum=" @ %countNum);
				// Only keep valid entries (item is not empty/"0" and count is a positive number)
				if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %countNum > 0)
				{
					if(%cleanedQuest != "")
						%cleanedQuest = %cleanedQuest @ " ";
					%cleanedQuest = %cleanedQuest @ %item @ " " @ %count;
				}
				else
				{
					//echo("DEBUG:   REMOVED invalid entry: item='" @ %item @ "', count='" @ %count @ "'");
					%removedCount++;
				}
			}
			if(%removedCount > 0)
				//echo("DEBUG: StoredQuestItems cleaned - removed " @ %removedCount @ " invalid entries");
			if(%cleanedQuest != %storedQuest)
				//echo("DEBUG: StoredQuestItems changed from '" @ %storedQuest @ "' to '" @ %cleanedQuest @ "'");
			%storedQuest = %cleanedQuest;
		}
		storeData(%clientId, "StoredQuestItems", %storedQuest);
		echo("DEBUG: StoredQuestItems FINAL = '" @ %storedQuest @ "'");
		
		// Load StoredKeyItems from field 39
		%storedKey = $funk::var[%name, 0, 39];
		echo("DEBUG: StoredKeyItems RAW (field 39) = '" @ %storedKey @ "'");
		if(%storedKey == "" || %storedKey == " " || %storedKey == "0")
		{
			echo("DEBUG: StoredKeyItems was empty/'0', normalizing to empty string");
			%storedKey = "";
		}
		else
		{
			// Clean up corrupted entries (item "0", count <= 0, negative values, etc.)
			%cleanedKey = "";
			%removedCount = 0;
			for(%i = 0; GetWord(%storedKey, %i) != -1; %i += 2)
			{
				%item = GetWord(%storedKey, %i);
				%count = GetWord(%storedKey, %i + 1);
				// Convert count to numeric to properly handle negative values like "-1" or "-0"
				%countNum = %count * 1;
				//echo("DEBUG:   Checking entry: item='" @ %item @ "', count='" @ %count @ "', countNum=" @ %countNum);
				// Only keep valid entries (item is not empty/"0" and count is a positive number)
				if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %countNum > 0)
				{
					if(%cleanedKey != "")
						%cleanedKey = %cleanedKey @ " ";
					%cleanedKey = %cleanedKey @ %item @ " " @ %count;
				}
				else
				{
					//echo("DEBUG:   REMOVED invalid entry: item='" @ %item @ "', count='" @ %count @ "'");
					%removedCount++;
				}
			}
			if(%removedCount > 0)
				//echo("DEBUG: StoredKeyItems cleaned - removed " @ %removedCount @ " invalid entries");
			if(%cleanedKey != %storedKey)
				//echo("DEBUG: StoredKeyItems changed from '" @ %storedKey @ "' to '" @ %cleanedKey @ "'");
			%storedKey = %cleanedKey;
		}
		storeData(%clientId, "StoredKeyItems", %storedKey);
		echo("DEBUG: StoredKeyItems FINAL = '" @ %storedKey @ "'");
		
		//echo("DEBUG: Loading bank items and inventory...");
		// NOTE: BankGemItems, BankRareItems, BankKeysItems, and BankScrollsItems are no longer used
		// Fields 33-34 are unused legacy bank fields
		// Fields 35-36 are now used for QuestItems and KeyItems (equipped belt items)
		// These old bank variables are not loaded to avoid field conflicts
		storeData(%clientId, "BankGemItems", "");
		storeData(%clientId, "BankRareItems", "");
		storeData(%clientId, "BankKeysItems", "");
		storeData(%clientId, "BankScrollsItems", "");
		// Field 37 is now used for Consumables (belt items like potions)
		%consumables = $funk::var[%name, 0, 37];
		echo("DEBUG: Consumables RAW (field 37) = '" @ %consumables @ "'");
		if(%consumables == "" || %consumables == " " || %consumables == "0")
		{
			echo("DEBUG: Consumables was empty/'0', normalizing to empty string");
			%consumables = "";
		}
		storeData(%clientId, "Consumables", %consumables);
		echo("DEBUG: Consumables FINAL = '" @ %consumables @ "'");
		// BankUniqueItems is no longer used - keep empty for backward compatibility
		storeData(%clientId, "BankUniqueItems", "");
		// CRITICAL FIX: Position 38 is now used for StoredQuestItems (loaded above)
		// GemItems is unused and has been removed - do not load from position 38
		// Position 39 is now used for StoredKeyItems (loaded above)
		// Position 42 is now used for StoredConsumables (bank storage for consumables)
		%storedConsumables = $funk::var[%name, 0, 42];
		echo("DEBUG: StoredConsumables RAW (field 42) = '" @ %storedConsumables @ "'");
		if(%storedConsumables == "" || %storedConsumables == " " || %storedConsumables == "0")
		{
			echo("DEBUG: StoredConsumables was empty/'0', normalizing to empty string");
			%storedConsumables = "";
		}
		storeData(%clientId, "StoredConsumables", %storedConsumables);
		echo("DEBUG: StoredConsumables FINAL = '" @ %storedConsumables @ "'");
		// Position 43 is now used for StoredArmor (bank storage for armor)
		%storedArmor = $funk::var[%name, 0, 43];
		//echo("DEBUG: StoredArmor RAW (field 43) = '" @ %storedArmor @ "'");
		if(%storedArmor == "" || %storedArmor == " " || %storedArmor == "0")
		{
			//echo("DEBUG: StoredArmor was empty/'0', normalizing to empty string");
			%storedArmor = "";
		}
		storeData(%clientId, "StoredArmor", %storedArmor);
		//echo("DEBUG: StoredArmor FINAL = '" @ %storedArmor @ "'");
		// Position 45 is now used for StoredAccessories (bank storage for accessories)
		%storedAccessories = $funk::var[%name, 0, 45];
		//echo("DEBUG: StoredAccessories RAW (field 45) = '" @ %storedAccessories @ "'");
		if(%storedAccessories == "" || %storedAccessories == " " || %storedAccessories == "0")
		{
			//echo("DEBUG: StoredAccessories was empty/'0', normalizing to empty string");
			%storedAccessories = "";
		}
		storeData(%clientId, "StoredAccessories", %storedAccessories);
		//echo("DEBUG: StoredAccessories FINAL = '" @ %storedAccessories @ "'");
		// Position 46 is now used for StoredOther (bank storage for other items)
		%storedOther = $funk::var[%name, 0, 46];
		//echo("DEBUG: StoredOther RAW (field 46) = '" @ %storedOther @ "'");
		if(%storedOther == "" || %storedOther == " " || %storedOther == "0")
		{
			//echo("DEBUG: StoredOther was empty/'0', normalizing to empty string");
			%storedOther = "";
		}
		storeData(%clientId, "StoredOther", %storedOther);
		//echo("DEBUG: StoredOther FINAL = '" @ %storedOther @ "'");
		// RareItems is unused - do not load from position 39
		storeData(%clientId, "RareItems", "");
		//echo("DEBUG: RareItems = '' (unused)");
		// NOTE: KeysItems, ScrollsItems, and UniqueItems are no longer used
		// Set them to empty strings to prevent conflicts with belt storage fields
		// Field 40 was used for KeysItems (now unused)
		// Field 41 was used for ScrollsItems (now unused)
		// Field 42 is now used for StoredConsumables (belt storage), not UniqueItems
		storeData(%clientId, "KeysItems", "");
		storeData(%clientId, "ScrollsItems", "");
		storeData(%clientId, "UniqueItems", "");
		//echo("DEBUG: KeysItems = '' (unused)");
		//echo("DEBUG: ScrollsItems = '' (unused)");
		//echo("DEBUG: UniqueItems = '' (unused)");
		storeData(%clientId, "Stance", $funk::var[%name, 0, 44]);
		//echo("DEBUG: Stance = '" @ $funk::var[%name, 0, 44] @ "'");
		
		// Load damage display preferences from combined string: "displayType:animationStyle:enabledFlag"
		%damageDisplayString = $funk::var[%name, 0, 47];
		if(%damageDisplayString == "" || %damageDisplayString == -1)
		{
			// Default values if not set
			%damageDisplayType = "bottomprint";
			%floatingAnimationStyle = "float"; // Default (changed from redmoon)
			%floatingDamageNumbers = "";
		}
		else
		{
			// Handle backward compatibility: if old format (just "bottomprint" or "chat"), parse it
			if(String::findSubStr(%damageDisplayString, ":") == -1)
			{
				// Old format - just the display type
				%damageDisplayType = %damageDisplayString;
				%floatingAnimationStyle = "float"; // Default (changed from redmoon)
				%floatingDamageNumbers = "";
			}
			else
			{
				// Parse the combined string format: "displayType:animationStyle:enabledFlag"
				// Handle formats: "displayType::" (double colon for non-floating) or "displayType:style:flag" (floating)
				%firstColon = String::findSubStr(%damageDisplayString, ":");
				
				if(%firstColon != -1)
				{
					%damageDisplayType = String::getSubStr(%damageDisplayString, 0, %firstColon);
					
					// Check if there's a second colon (could be "::" or ":style:flag")
					%afterFirstColon = String::getSubStr(%damageDisplayString, %firstColon + 1, 99999);
					%secondColon = String::findSubStr(%afterFirstColon, ":");
					
					if(%secondColon != -1)
					{
						// Format: "displayType:style:flag" or "displayType::"
						if(%secondColon == 0)
						{
							// Double colon format: "displayType::" (empty style and flag)
							%floatingAnimationStyle = "";
							%floatingDamageNumbers = "";
						}
						else
						{
							// Format: "displayType:style:flag"
							%floatingAnimationStyle = String::getSubStr(%afterFirstColon, 0, %secondColon);
							%floatingDamageNumbers = String::getSubStr(%afterFirstColon, %secondColon + 1, 99999);
						}
					}
					else
					{
						// Only one colon (format: "displayType:animationStyle" - old format)
						if(%afterFirstColon == "")
						{
							// Just "displayType:" with nothing after
							%floatingAnimationStyle = "";
							%floatingDamageNumbers = "";
						}
						else
						{
							%floatingAnimationStyle = %afterFirstColon;
							%floatingDamageNumbers = "";
						}
					}
				}
				else
				{
					// No colons found (old format - just display type)
					%damageDisplayType = %damageDisplayString;
					%floatingAnimationStyle = "";
					%floatingDamageNumbers = "";
				}
			}
			
			// Set defaults if empty
			if(%damageDisplayType == "" || %damageDisplayType == -1)
				%damageDisplayType = "bottomprint";
			if(%floatingAnimationStyle == "" || %floatingAnimationStyle == -1)
				%floatingAnimationStyle = "float"; // Default (changed from redmoon)
			
			// Convert old styles to new ones when loading
			if(%floatingAnimationStyle == "redmoon" || %floatingAnimationStyle == "wow")
				%floatingAnimationStyle = "float"; // Convert old styles to float
			if(%floatingDamageNumbers == "" || %floatingDamageNumbers == -1)
			{
				// Only enable if using floating display
				if(%damageDisplayType == "floating")
					%floatingDamageNumbers = "1";
				else
					%floatingDamageNumbers = "";
			}
		}
		
		storeData(%clientId, "damageDisplayType", %damageDisplayType);
		storeData(%clientId, "floatingAnimationStyle", %floatingAnimationStyle);
		storeData(%clientId, "floatingDamageNumbers", %floatingDamageNumbers);

		//echo("DEBUG: Calling Belt::BankStorageConversion...");
		Belt::BankStorageConversion(%clientid);
		//echo("DEBUG: Belt::BankStorageConversion complete");

		//echo("DEBUG: Loading skill variables...");
		//skill variables
		%cnt = 0;
		for(%i = 1; %i <= GetNumSkills(); %i++)
		{
			$PlayerSkill[%clientId, %i] = $funk::var[%name, 4, %cnt++];
			$SkillCounter[%clientId, %i] = $funk::var[%name, 4, %cnt++];
		}
		//echo("DEBUG: Loaded " @ GetNumSkills() @ " skills");

		//echo("DEBUG: Loading quest counters...");
		%questCounterCount = 0;
		for(%i = 1; $funk::var[%name, 3, %i] != ""; %i++)
		{
			$QuestCounter[%name, $funk::var[%name, 2, %i]] = $funk::var[%name, 3, %i];
			%questCounterCount++;
		}
		//echo("DEBUG: Loaded " @ %questCounterCount @ " quest counters");

		//echo("DEBUG: Loading bonus state variables...");
		//bonus state variables
		for(%i = 1; %i <= $maxBonusStates; %i++)
		{
			$BonusState[%clientId, %i] = $funk::var[%name, 5, %i];
			$BonusStateCnt[%clientId, %i] = $funk::var[%name, 6, %i];
		}
		//echo("DEBUG: Loaded bonus states (max=" @ $maxBonusStates @ ")");

		//== VERSION CONVERSION ROUTINES ============================

		//echo("DEBUG: Running version conversion/default checks...");
		//temp--------
		if(fetchData(%clientId, "RemortStep") == "")
		{
			//echo("DEBUG: RemortStep was empty, defaulting to 0");
			storeData(%clientId, "RemortStep", 0);
		}
		if(fetchData(%clientId, "LCKconsequence") == "")
		{
			//echo("DEBUG: LCKconsequence was empty, defaulting to 'death'");
			storeData(%clientId, "LCKconsequence", "death");
		}
		if(fetchData(%clientId, "tmphp") == "")
		{
			//echo("DEBUG: tmphp was empty after load, defaulting to 1");
			storeData(%clientId, "tmphp", 1);
		}
		if(fetchData(%clientId, "tmpmana") == "")
		{
			//echo("DEBUG: tmpmana was empty after load, defaulting to 1");
			storeData(%clientId, "tmpmana", 1);
		}
		if(fetchData(%clientId, "tmpname") == "")
		{
			//echo("DEBUG: tmpname was empty, defaulting to player name");
			storeData(%clientId, "tmpname", %name);
		}
		if(fetchData(%clientId, "TournyRank") == "")
		{
			//echo("DEBUG: TournyRank was empty, defaulting to 0");
			storeData(%clientId, "TournyRank", 0);
		}
		if(fetchData(%clientId, "Stance") == "")
		{
			//echo("DEBUG: Stance was empty, defaulting to 'Normal'");
			storeData(%clientId, "Stance", "Normal");
		}
		//------------

		//===========================================================
		
		// Reapply belt item stat bonuses from equipped armor and accessories
		Belt::ReapplyEquippedStats(%clientId);
		
		// NOTE: RefreshAll() is NOT called here because:
		// 1. LoadCharacter() runs BEFORE player spawn (no Player object exists yet)
		// 2. RefreshAll() requires a valid Player object and would just return early anyway
		// 3. RefreshAll() is already called by GiveThisStuff() during Game::playerSpawned() after spawn
		// This prevents redundant calls and potential client crashes from calling RefreshAll before spawn
		
		// ===== DEBUG: COMPLETE INVENTORY SUMMARY =====
		echo("===== INVENTORY SUMMARY for " @ %name @ " (" @ %clientId @ ") =====");
		
		// Player Inventory (regular items - not belt)
		echo("--- PLAYER INVENTORY ---");
		echo("  spawnStuff: " @ fetchData(%clientId, "spawnStuff"));
		echo("  savedMountedWeapon: " @ fetchData(%clientId, "savedMountedWeapon"));
		
		// Regular Bank Storage (main bank - not belt storage)
		echo("--- REGULAR BANK STORAGE ---");
		echo("  BankStorage: " @ fetchData(%clientId, "BankStorage"));
		
		// Belt Items (carried)
		echo("--- BELT ITEMS (Carried) ---");
		echo("  QuestItems: " @ fetchData(%clientId, "QuestItems"));
		echo("  KeyItems: " @ fetchData(%clientId, "KeyItems"));
		echo("  Consumables: " @ fetchData(%clientId, "Consumables"));
		echo("  Armor: " @ fetchData(%clientId, "Armor"));
		echo("  Accessories: " @ fetchData(%clientId, "Accessories"));
		echo("  Other: " @ fetchData(%clientId, "Other"));
		
		// Equipped Belt Items
		echo("--- EQUIPPED BELT ITEMS ---");
		echo("  EquippedBeltArmor: " @ fetchData(%clientId, "EquippedBeltArmor"));
		echo("  EquippedBeltAccessories: " @ fetchData(%clientId, "EquippedBeltAccessories"));
		
		// Belt Storage at Banker (stored belt items)
		echo("--- BELT STORAGE (at Banker) ---");
		echo("  StoredQuestItems: " @ fetchData(%clientId, "StoredQuestItems"));
		echo("  StoredKeyItems: " @ fetchData(%clientId, "StoredKeyItems"));
		echo("  StoredConsumables: " @ fetchData(%clientId, "StoredConsumables"));
		echo("  StoredArmor: " @ fetchData(%clientId, "StoredArmor"));
		echo("  StoredAccessories: " @ fetchData(%clientId, "StoredAccessories"));
		echo("  StoredOther: " @ fetchData(%clientId, "StoredOther"));
		
		echo("=============================================");
		
		//echo("===== DEBUG LoadCharacter: COMPLETE =====");
		echo("Load complete.");
		
		// Announce player join to all players in server chat (green text)
		messageAll($MsgGreen, %name @ " has joined the server.");
	}
	else
	{
		//give defaults
		//echo("===== DEBUG LoadCharacter: NEW CHARACTER CREATION =====");
		echo("Giving defaults to new player " @ %clientId @ " (" @ %name @ ")");
		//echo("DEBUG: Save file not found, initializing new character with defaults");
		
		//echo("DEBUG: Setting basic character data...");
		storeData(%clientId, "RACE", Client::getGender(%clientId) @ "Human");
		//echo("DEBUG: RACE = '" @ Client::getGender(%clientId) @ "Human'");
		storeData(%clientId, "EXP", 0);
		//echo("DEBUG: EXP = 0");
		storeData(%clientId, "campPos", "");
		//echo("DEBUG: campPos = ''");
		storeData(%clientId, "BANK", $initbankcoins);
		//echo("DEBUG: BANK = " @ $initbankcoins);
		storeData(%clientId, "grouplist", "");
		//echo("DEBUG: grouplist = ''");
		storeData(%clientId, "defaultTalk", "#say");
		//echo("DEBUG: defaultTalk = '#say'");
		storeData(%clientId, "password", $Client::info[%clientId, 5]);
		//echo("DEBUG: password = '" @ $Client::info[%clientId, 5] @ "'");
		storeData(%clientId, "LCK", $initLCK);
		//echo("DEBUG: LCK = " @ $initLCK);
		storeData(%clientId, "PlayerInfo", "");
		//echo("DEBUG: PlayerInfo = ''");
		storeData(%clientId, "ignoreGlobal", "");
		//echo("DEBUG: ignoreGlobal = ''");
		storeData(%clientId, "LCKconsequence", "death");
		//echo("DEBUG: LCKconsequence = 'death'");
		storeData(%clientId, "tmphp", "");
		//echo("DEBUG: tmphp = '' (new character)");
		storeData(%clientId, "tmpmana", "");
		//echo("DEBUG: tmpmana = '' (new character)");
		storeData(%clientId, "RemortStep", 0);
		//echo("DEBUG: RemortStep = 0");
		storeData(%clientId, "tmpname", %name);
		//echo("DEBUG: tmpname = '" @ %name @ "'");
		storeData(%clientId, "tmpLastSaveVer", $rpgver);
		//echo("DEBUG: tmpLastSaveVer = '" @ $rpgver @ "'");
		storeData(%clientId, "bounty", 0);
		//echo("DEBUG: bounty = 0");
		storeData(%clientId, "isMimic", "");
		//echo("DEBUG: isMimic = ''");
		storeData(%clientId, "MyHouse", "");
		//echo("DEBUG: MyHouse = ''");
		storeData(%clientId, "RankPoints", 0);
		//echo("DEBUG: RankPoints = 0");
		storeData(%clientId, "TournyRank", 0);
		//echo("DEBUG: TournyRank = 0");
		storeData(%clientId, "Stance", "Normal");
		//echo("DEBUG: Stance = 'Normal'");
		
		//echo("DEBUG: Initializing belt storage fields for new character...");
		// Initialize belt storage fields for new characters
		storeData(%clientId, "QuestItems", "");
		echo("DEBUG: QuestItems = ''");
		storeData(%clientId, "KeyItems", "");
		echo("DEBUG: KeyItems = ''");
		storeData(%clientId, "Consumables", "");
		echo("DEBUG: Consumables = ''");
		storeData(%clientId, "StoredQuestItems", "");
		echo("DEBUG: StoredQuestItems = ''");
		storeData(%clientId, "StoredKeyItems", "");
		echo("DEBUG: StoredKeyItems = ''");
		storeData(%clientId, "StoredConsumables", "");
		echo("DEBUG: StoredConsumables = ''");
		storeData(%clientId, "StoredArmor", "");
		//echo("DEBUG: StoredArmor = ''");
		storeData(%clientId, "StoredAccessories", "");
		//echo("DEBUG: StoredAccessories = ''");
		storeData(%clientId, "StoredOther", "");
		//echo("DEBUG: StoredOther = ''");
		storeData(%clientId, "AscensionTalents", "");
		//echo("DEBUG: AscensionTalents = ''");
		storeData(%clientId, "BeltStorage", "");
		//echo("DEBUG: BeltStorage = ''");
		storeData(%clientId, "Consumables", "");
		//echo("DEBUG: Consumables = ''");
		storeData(%clientId, "Armor", "");
		//echo("DEBUG: Armor = ''");
		storeData(%clientId, "Accessories", "");
		//echo("DEBUG: Accessories = ''");
		storeData(%clientId, "Other", "");
		//echo("DEBUG: Other = ''");

		%clientId.choosingGroup = True;
		//echo("DEBUG: Set choosingGroup = True");

		//echo("DEBUG: Initializing all skills to 0...");
		SetAllSkills(%clientId, 0);
		//echo("DEBUG: Skills initialized");

		storeData(%clientId, "spawnStuff", "PickAxe 1");
		//echo("DEBUG: spawnStuff = 'PickAxe 1'");
		
		// CRITICAL: Potions go to Consumables (belt storage) not spawnStuff (inventory)
		// This ensures new players have potions in their belt, not their bank
		storeData(%clientId, "Consumables", "BluePotion 1 CrystalBluePotion 3 ");
		
		// CRITICAL: Initialize COINS for new characters (they get coins when they choose class, but need initial value)
		// This ensures COINS is set to 0 initially (will be set properly when class is chosen)
		storeData(%clientId, "COINS", 0);
		
		// NOTE: RefreshAll() is NOT called here because:
		// 1. LoadCharacter() runs BEFORE player spawn (no Player object exists yet)
		// 2. RefreshAll() requires a valid Player object and would just return early anyway
		// 3. RefreshAll() is already called by GiveThisStuff() during Game::playerSpawned() after spawn
		// This prevents redundant calls and potential client crashes from calling RefreshAll before spawn
		
		//echo("===== DEBUG LoadCharacter: NEW CHARACTER CREATION COMPLETE =====");
	}

	ClearFunkVar(%name);
}

function OnOrOfflineGive(%name, %award)
{
	dbecho($dbechoMode, "OnOrOfflineGive(" @ %name @ ", " @ %award @ ")");

	%clientId = NEWgetClientByName(%name);
	//messageAll($MsgRed, "DEBUG: %name: " @ %name);
	//messageAll($MsgRed, "DEBUG: %clientId: " @ %clientId);
	//messageAll($MsgRed, "DEBUG: %award: " @ %award);
	if(%clientId != -1)
	{
		//player is in-game, simply store the item in storage
		Client::sendMessage(%clientId, $MsgBeige, "You have received a prize for downloading Theory Of Trance music, check your storage!");
		for(%i = 0; GetWord(%award, %i) != -1; %i+=2)
			storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), GetWord(%award, %i), GetWord(%award, %i+1)));
	}
	else
	{
		//player is not in-game. load character file, make changes, then save
		%filename = %name @ ".cs";

		$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;	//thanks Presto

		if(isFile("temp\\" @ %filename))
		{
			//load character
			ClearFunkVar(%name);
			exec(%filename);

			//pass variables thru, while adding the awarded item
			$funk::var["[\"" @ %name @ "\", 0, 1]"] = $funk::var[%name, 0, 1];
			$funk::var["[\"" @ %name @ "\", 0, 2]"] = $funk::var[%name, 0, 2];
			$funk::var["[\"" @ %name @ "\", 0, 3]"] = $funk::var[%name, 0, 3];
			$funk::var["[\"" @ %name @ "\", 0, 4]"] = $funk::var[%name, 0, 4];
			$funk::var["[\"" @ %name @ "\", 0, 5]"] = $funk::var[%name, 0, 5];
			$funk::var["[\"" @ %name @ "\", 0, 6]"] = $funk::var[%name, 0, 6];
			$funk::var["[\"" @ %name @ "\", 0, 7]"] = $funk::var[%name, 0, 7];
			$funk::var["[\"" @ %name @ "\", 0, 8]"] = $funk::var[%name, 0, 8];
			$funk::var["[\"" @ %name @ "\", 0, 9]"] = $funk::var[%name, 0, 9];
			$funk::var["[\"" @ %name @ "\", 0, 10]"] = $funk::var[%name, 0, 10];
			$funk::var["[\"" @ %name @ "\", 0, 11]"] = $funk::var[%name, 0, 11];
			$funk::var["[\"" @ %name @ "\", 0, 12]"] = $funk::var[%name, 0, 12];
			$funk::var["[\"" @ %name @ "\", 0, 13]"] = $funk::var[%name, 0, 13];
			$funk::var["[\"" @ %name @ "\", 0, 14]"] = $funk::var[%name, 0, 14];
			$funk::var["[\"" @ %name @ "\", 0, 15]"] = $funk::var[%name, 0, 15];
			for(%i = 0; GetWord(%award, %i) != -1; %i+=2)
				$funk::var["[\"" @ %name @ "\", 0, 16]"] = SetStuffString($funk::var[%name, 0, 16], GetWord(%award, %i), GetWord(%award, %i+1));
			$funk::var["[\"" @ %name @ "\", 0, 17]"] = $funk::var[%name, 0, 17];
			$funk::var["[\"" @ %name @ "\", 0, 18]"] = $funk::var[%name, 0, 18];
			$funk::var["[\"" @ %name @ "\", 0, 19]"] = $funk::var[%name, 0, 19];
			$funk::var["[\"" @ %name @ "\", 0, 20]"] = $funk::var[%name, 0, 20];
			$funk::var["[\"" @ %name @ "\", 0, 21]"] = $funk::var[%name, 0, 21];
			$funk::var["[\"" @ %name @ "\", 0, 22]"] = $funk::var[%name, 0, 22];
			$funk::var["[\"" @ %name @ "\", 0, 23]"] = $funk::var[%name, 0, 23];
			$funk::var["[\"" @ %name @ "\", 0, 26]"] = $funk::var[%name, 0, 26];
			$funk::var["[\"" @ %name @ "\", 0, 27]"] = $funk::var[%name, 0, 27];
			$funk::var["[\"" @ %name @ "\", 0, 28]"] = $funk::var[%name, 0, 28];
			//$funk::var["[\"" @ %name @ "\", 0, 29]"] = $funk::var[%name, 0, 29];
			$funk::var["[\"" @ %name @ "\", 0, 30]"] = $funk::var[%name, 0, 30];
			$funk::var["[\"" @ %name @ "\", 0, 31]"] = $funk::var[%name, 0, 31];
			$funk::var["[\"" @ %name @ "\", 0, 666]"] = $funk::var[%name, 0, 666];

			//skills
			%cnt = 0;
			for(%i = 1; %i <= GetNumSkills(); %i++)
			{
				%cnt++;
				$funk::var["[\"" @ %name @ "\", 4, " @ %cnt @ "]"] = $funk::var[%name, 4, %cnt];
				%cnt++;
				$funk::var["[\"" @ %name @ "\", 4, " @ %cnt @ "]"] = $funk::var[%name, 4, %cnt];
			}

			//quests
			for(%i = 1; $funk::var[%name, 2, %i] != ""; %i++)
				$funk::var["[\"" @ %name @ "\", 2, " @ %i @ "]"] = $funk::var[%name, 2, %i];
			for(%i = 1; $funk::var[%name, 3, %i] != ""; %i++)
				$funk::var["[\"" @ %name @ "\", 3, " @ %i @ "]"] = $funk::var[%name, 3, %i];

			//bonus state variables
			for(%i = 1; %i <= $maxBonusStates; %i++)
			{
				$funk::var["[\"" @ %name @ "\", 5, " @ %i @ "]"] = $funk::var[%name, 5, %i];
				$funk::var["[\"" @ %name @ "\", 6, " @ %i @ "]"] = $funk::var[%name, 6, %i];
			}

			//save character
			File::delete("temp\\" @ %name @ ".cs");
			export("funk::var[\"" @ %name @ "\",*", "temp\\" @ %name @ ".cs", false);

			ClearFunkVar(%name);
		}
	}
}

function ResetPlayer(%clientId)
{
	dbecho($dbechoMode2, "ResetPlayer(" @ %clientId @ ")");

	echo("===== DEBUG ResetPlayer: START =====");
	%name = Client::getName(%clientId);
	%filename = %name @ ".cs";
	echo("DEBUG ResetPlayer: Player = " @ %name @ " (" @ %clientId @ ")");
	echo("DEBUG ResetPlayer: Filename = " @ %filename);

	File::delete("temp\\" @ %filename);
	echo("DEBUG ResetPlayer: Deleted save file (if existed)");

	echo("DEBUG ResetPlayer: Calling LoadCharacter...");
	LoadCharacter(%clientId);
	echo("DEBUG ResetPlayer: LoadCharacter complete");

	echo("DEBUG ResetPlayer: Calling StartStatSelection...");
	StartStatSelection(%clientId);
	echo("===== DEBUG ResetPlayer: COMPLETE =====");
}


function GetPersistentTime() {
    // Returns persistent time that continues across server restarts
    // Uses accumulated elapsed time + current session time
    if($ServerTimeElapsed == "")
        $ServerTimeElapsed = 0;
    if($ServerTimeStart == "")
        $ServerTimeStart = getSimTime();
    
    return $ServerTimeElapsed + (getSimTime() - $ServerTimeStart);
}

function SaveServerTime() {
    // Save the accumulated server time so it persists across restarts
    if($ServerTimeElapsed == "")
        $ServerTimeElapsed = 0;
    if($ServerTimeStart == "")
        $ServerTimeStart = getSimTime();
    
    // Update accumulated time with current session
    $ServerTimeElapsed = $ServerTimeElapsed + (getSimTime() - $ServerTimeStart);
    $ServerTimeStart = getSimTime();
    
    // Save to file
    File::delete("temp\\ServerTime.cs");
    export("ServerTimeElapsed", "temp\\ServerTime.cs", false);
}

function LoadServerTime() {
    // Load accumulated server time from previous sessions
    if(isFile("temp\\ServerTime.cs"))
    {
        exec("ServerTime.cs");
        if($ServerTimeElapsed == "")
            $ServerTimeElapsed = 0;
        echo("DEBUG LoadServerTime: Loaded accumulated time: " @ $ServerTimeElapsed @ " seconds");
        
        // 48-hour reset (172800 seconds) - reset server time periodically
        if($ServerTimeElapsed > 172800)
        {
            echo("DEBUG LoadServerTime: Time exceeded 48 hours (" @ $ServerTimeElapsed @ "s), resetting to 0");
            $ServerTimeElapsed = 0;
            File::delete("temp\\ServerTime.cs");
        }
    }
    else
    {
        $ServerTimeElapsed = 0;
        echo("DEBUG LoadServerTime: No saved time found, starting fresh");
    }
    $ServerTimeStart = getSimTime();
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
        }
    }
    
    // Then scan sequential IDs for other deployables (platforms, force fields, trees, etc.)
    %deployableCount = 0;
    while (%othercnt < 15) {
        %i++;
        %ID = 8361 + %i;
        %obj = GameBase::getDataName(%ID);
        if (String::findSubStr($WorldSaveList, "|" @ %obj @ "|") != -1) {
            // Skip lootbags here since we already handled them from MissionCleanup
            if(%obj != "Lootbag")
            {
                // CRITICAL: Skip deployables owned by bots (enemy or town bots)
                %ownerName = $owner[%ID];
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
                $world::object[%i] == "DepPlatSmallVert" ||
                $world::object[%i] == "DepPlatMediumVert") {
                DeployPlatform($world::owner[%i], $world::team[%i], $world::pos[%i], $world::rot[%i], $world::object[%i]);
            } else if ($world::object[%i] == "StaticDoorForceField") {
                DeployForceField($world::owner[%i], $world::team[%i], $world::pos[%i], $world::rot[%i]);
            } else if ($world::object[%i] == "DeployableTree") {
                DeployTree($world::owner[%i], $world::team[%i], $world::pos[%i], $world::rot[%i]);
            } else if ($world::object[%i] == "Lootbag") {
                DeployLootbag($world::pos[%i], $world::rot[%i], $world::special[%i]);
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

//=============================================================================
// LOOTBAG AGGREGATION SYSTEM
// Periodically merges nearby lootbags to prevent lootbag spam from bot kills
// This runs every 2 minutes and combines lootbags within 25 units of each other
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

function UpdateAppearance(%clientId)
{
	// Recursion Guard: Prevent infinite loops
	if($InUpdateAppearance[%clientId]) return;
	$InUpdateAppearance[%clientId] = true;
	
	%clientName = Client::getName(%clientId);
	dbecho($dbechoMode, "UpdateAppearance(" @ %clientId @ ")");
	
	%clientName = Client::getName(%clientId);
	dbecho($dbechoMode, "UpdateAppearance(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before proceeding
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	if(%player == -1 || %player == "")
	{
		// Player object doesn't exist (player/bot was deleted)
		$InUpdateAppearance[%clientId] = false;
		return;
	}
	
	// CRITICAL FIX: Check explicit flag on player object (most robust)
	if(%player.isTownBot == "true" || %player.isTownBot == true)
	{
		// This is definitely a town bot - skip
		dbecho($dbechoMode, "UpdateAppearance skipped for bot " @ %clientId @ " (isTownBot flag set)");
		$InUpdateAppearance[%clientId] = false;
		return;
	}

	// Skip all bots - their appearance is managed during spawn and should not be changed
	// Town bots have BotInfoAiName set but no SpawnBotInfo
	// Enemy bots have SpawnBotInfo set
	// Both types already have their armor/skin set correctly during spawn, so we shouldn't overwrite it
	if(isRPGAI(%clientId))
	{
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		
		// If it's a town bot (BotInfoAiName but no SpawnBotInfo) - skip
		if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0" && (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
		if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0" && (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
		{
			// This is a town bot - skip UpdateAppearance to prevent skin reset
			$InUpdateAppearance[%clientId] = false;
			return;
		}
		
		// If it's an enemy bot (has SpawnBotInfo) - skip
		// Enemy bots have their armor/skin set from $BotInfo[botName, RACE] and equipment string in SpawnAI()
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Enemy bot - don't change their appearance (already set from $BotInfo and equipment string)
			$InUpdateAppearance[%clientId] = false;
			return;
		}
	}

	//Determine armor from shields
	%armor = -1;
	%shield = -1;
	
	// CRITICAL: Re-validate player object before calling GetAccessoryList (which calls Player::getItemCount)
	%playerCheck = Client::getOwnedObject(%clientId);
	if(%playerCheck == -1 || %playerCheck == "")
	if(%playerCheck == -1 || %playerCheck == "")
	{
		// Player object was deleted between validation and this call
		$InUpdateAppearance[%clientId] = false;
		return;
	}
	
	%list = GetAccessoryList(%clientId, 2, "3 7");
	for(%i = 0; (%w = getCroppedItem(GetWord(%list, %i))) != -1; %i++)
	{
		if($AccessoryVar[%w, $AccessoryType] == $BodyAccessoryType)
			%armor = %w;
		else if($AccessoryVar[%w, $AccessoryType] == $ShieldAccessoryType)
			%shield = %w;
	}
	
	// Store armor name to player data so armor effects can be looked up (used by playerdamage.cs)
	// This allows armor special effects (RETRIBUTION, STATIC_DISCHARGE, PHASE_SHIFT) to work
	if(%armor != -1 && %armor != "")
		storeData(%clientId, "Armor", %armor);
	else
		storeData(%clientId, "Armor", "");
	
	// CRITICAL: Re-validate player object before using it
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	if(%player == -1 || %player == "")
	{
		// Player object was deleted during GetAccessoryList
		$InUpdateAppearance[%clientId] = false;
		return;
	}
	%race = fetchData(%clientId, "RACE");
	%model = Player::getArmor(%clientId);
	%cw = String::getSubStr(%model, String::findSubStr(%model, "Armor"), 99999);
	// HARDEN EXTRACTION: If %cw is just "Armor" (extracted from a monster like OrcArmor), 
	// it leads to "MaleHumanArmor" (invalid/slow) when switching back to human via Transmog.
	// Humans MUST have a numeric suffix (0-11). Default to Armor7 (balanced) if missing.
	%lastChar = String::getSubStr(%cw, String::len(%cw)-1, 1);
	if(%lastChar < "0" || %lastChar > "9") %cw = "Armor7";
	%skinbase = Client::getSkinBase(%clientId);
	
	// Initialize %apm, only access $ArmorPlayerModel if %armor is valid
	%apm = "";
	if(%armor != -1 && %armor != "")
	{
		if($ArmorPlayerModel[%armor] != "")
			%apm = $ArmorPlayerModel[%armor];
	}

	//=================================
	// Update skin & Race (Transmog Overrides)
	//=================================
	%personalSkin = fetchData(%clientId, "PersonalSkin");
	%tmRace = fetchData(%clientId, "TransmogRace");
	
	if(%personalSkin != "" && %personalSkin != "0")
	{
		%skinbase = %personalSkin;
		
		// 1. Race Override (TransmogRace)
		if(%tmRace != "" && %tmRace != "0")
		{
		    %race = %tmRace;
		}
		else
		{
		    // Legacy fallback for skins without TransmogRace flag
		    if(%personalSkin == "rpgorc") %race = "Orc";
		    else if(%personalSkin == "rpggnoll") %race = "Pigman";
		    else if(%personalSkin == "min") %race = "Minotaur";
		    else if(%personalSkin == "undead") %race = "Undead";
		    else if(%personalSkin == "zombie") %race = "Zombie";
		    else if(%personalSkin == "chewbacca") %race = "Ogre"; 
		    else if(%personalSkin == "storm_daemon") %race = "Demon";
		    else if(%personalSkin == "redgodeye") %race = "Ogre";
		}

		// 2. Human-only Gender Overrides
		if(%race == "MaleHuman" || %race == "FemaleHuman")
		{
			if(String::findSubStr(%personalSkin, "female") != -1) %race = "FemaleHuman";
			else if(String::findSubStr(%personalSkin, "male") != -1) %race = "MaleHuman";
		}
		
		// 3. Universal Suffix Stripping
		%suffixPos = String::findSubStr(%skinbase, ".male");
		if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
		%suffixPos = String::findSubStr(%skinbase, ".female");
		if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
	}
	else if(%race == "MaleHuman" || %race == "FemaleHuman")
	{
		%skinbase = "rpgbase";
	}

	//=================================
	// Update Visuals (Armor/Model)
	//=================================
	if(%race == "MaleHuman" || %race == "FemaleHuman")
	{
		if(%armor != -1)
		{
			// Regular gear visual
			%skinbase = $ArmorSkin[%armor];
			
			// Transmog visual override
			if(%personalSkin != "" && %personalSkin != "0")
			{
				%skinbase = %personalSkin;
				%suffixPos = String::findSubStr(%skinbase, ".male");
				if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
				%suffixPos = String::findSubStr(%skinbase, ".female");
				if(%suffixPos != -1) %skinbase = String::getSubStr(%skinbase, 0, %suffixPos);
			}
		}
	}
	else if(%race == "DeathKnight")
	{
		%skinbase = "cphoenix";
		%apm = "";
		%cw = "Armor22";
		%armor = 0;
	}
	else
	{
		%p = $RaceToArmorType[%race];
		%armor = -1;
	}

	//=================================
	// Update player model (Armor)
	//=================================
	%adminBoots = (Player::getItemCount(%clientId, "AdminBoots0") > 0);
	if(%adminBoots)
	{
		// ADMIN BOOTS OVERRIDE: Select specialized variant for flying
		if(%race == "Orc" || %race == "Pigman") %p = "AdminBootsMediumArmor";
		else if(%race == "Ogre") %p = "AdminBootsHeavyArmor";
		else if(%race == "Zombie") %p = "AdminBootsZombieArmor";
		else if(%race == "Undead") %p = "AdminBootsSkelArmor";
		else if(%race == "Minotaur") %p = "AdminBootsMinotaurArmor";
		else if(%race == "Angel") %p = "AdminBootsFemaleRobedArmor";
		else if(%race == "Admin") %p = "AdminBootsRobedArmor";
		else if(%race == "Demon" || %race == "Alien" || %race == "Seals" || %race == "God" || %race == "Uber") %p = "AdminBootsMonsterArmor";
		// Check for: actual robe equipped (apm=="Robed"), Transmog robe skin, or robed race
		else if(%apm == "Robed" && %race == "MaleHuman") %p = "AdminBootsRobedArmor";
		else if(%apm == "Robed" && %race == "FemaleHuman") %p = "AdminBootsFemaleRobedArmor";
		else if(%race == "MaleHumanRobed" || (String::findSubStr(%personalSkin, "robe") != -1 && %race == "MaleHuman")) %p = "AdminBootsRobedArmor";
		else if(%race == "FemaleHumanRobed" || (String::findSubStr(%personalSkin, "robe") != -1 && %race == "FemaleHuman")) %p = "AdminBootsFemaleRobedArmor";
		else %p = "AdminBootsArmor";
	}
	else if(%armor != -1)
	{
		%p = %race @ %apm @ %cw;
	}
	else
	{
		// Default Armor handling (Naked/No Body Accessory)
		// For humans: Use %race @ %cw to preserve the current speed tier (set by RefreshWeight)
		// %cw was already extracted from current armor at line 3819 and defaults to "Armor7" if invalid
		// This avoids both: the invisibility bug (Armor0 -> Armor7 double-switch) AND
		// breaking speed boots (which set Armor8-11 via RefreshWeight)
		if(%race == "MaleHuman" || %race == "FemaleHuman")
			%p = %race @ %cw; 
		else
			%p = $RaceToArmorType[%race];
	}

	%ae = GameBase::getEnergy(%player);

	//=================================
	// Set Armor (Check logic)
	//=================================
	// We only set armor if it's different. This prevents the "armor ping-pong"
	// that triggers repeated attack animations even when no change is needed.
	if(Player::getArmor(%clientId) != %p && %p != "")
	{
		Player::setArmor(%clientId, %p);
		GameBase::setEnergy(%player, %ae);
	}
	
	//=================================
	// Update skin (After Armor)
	//=================================
	// CRITICAL FIX: Validate skin before setting - empty skin causes invisibility!
	if(%skinbase == "" || %skinbase == -1)
	{
		echo("[INVISIBILITY BUG] UpdateAppearance: EMPTY SKINBASE detected for " @ %clientName @ " (clientId=" @ %clientId @ "). Race=" @ %race @ ", Armor=" @ %armor @ ", ArmorSkin[armor]=" @ $ArmorSkin[%armor] @ ". Falling back to rpgbase.");
		%skinbase = "rpgbase";
	}
	
	// CRITICAL FIX: Set skin AFTER armor change to prevent reversion to default/enemy skin
	if(Client::getSkinBase(%clientId) != %skinbase)
	{
		// Log skin changes for debugging invisibility issues
		if($SKIN_DEBUG) echo("[SKIN DEBUG] UpdateAppearance: Setting skin for " @ %clientName @ " from '" @ Client::getSkinBase(%clientId) @ "' to '" @ %skinbase @ "'");
		Client::setSkin(%clientId, %skinbase);
	}

	//=================================
	// Update shields and Orb
	//=================================
	if(%shield != -1)
	{
		if(Player::getMountedItem(%clientId, 2) != %shield)
		{
			Player::unmountItem(%clientId, 2);
			Player::mountItem(%clientId, %shield, 2);
		}
	}
	else
	{
		%player = Client::getOwnedObject(%clientId);
		if(%player != -1)
		{
			if(Player::getMountedItem(%clientId, 2) != -1)
				Player::unmountItem(%clientId, 2);

			for(%i = 1; $ItemList[Orb, %i] != ""; %i++)
			{
				// Re-validate player object in loop in case it gets despawned mid-execution
				%playerCheck = Client::getOwnedObject(%clientId);
				if(%playerCheck == "" || %playerCheck == -1)
					break; // Exit loop if player object no longer exists
				
				if(SafeGetItemCount(%clientId, $ItemList[Orb, %i] @ "0", "UpdateAppearance"))
					Player::mountItem(%clientId, $ItemList[Orb, %i] @ "0", 2);
			}
		}
	}
	
	// Release Recursion Guard
	$InUpdateAppearance[%clientId] = false;
}

function UpdateTeam(%clientId)
{
	dbecho($dbechoMode, "UpdateTeam(" @ %clientId @ ")");

	// CRITICAL: Check display name patterns FIRST to identify bots before SpawnBotInfo is set
	// This prevents UpdateTeam() from running for bots when called from Game::playerSpawn()
	// during AI::spawn() before SpawnBotInfo and BotInfoAiName are set
	%playerName = Client::getName(%clientId);
	%isBotByName = false;
	if(%playerName != "" && %playerName != -1)
	{
		// Check for enemy bot name patterns (anywhere in name, not just start)
		// CRITICAL: Use != -1 to match pattern anywhere in name (allows "Giant Demon Lord" to match "Demon")
		if(String::findSubStr(%playerName, "Alien") != -1 || 
		   String::findSubStr(%playerName, "Admin") != -1 ||
		   String::findSubStr(%playerName, "Angel") != -1 ||
		   String::findSubStr(%playerName, "Demon") != -1 ||
		   String::findSubStr(%playerName, "Zombie") != -1 ||
		   String::findSubStr(%playerName, "Ogre") != -1 ||
		   String::findSubStr(%playerName, "Orc") != -1 ||
		   String::findSubStr(%playerName, "Pigman") != -1 ||
		   String::findSubStr(%playerName, "Pigmen") != -1 ||
		   String::findSubStr(%playerName, "Undead") != -1 ||
		   String::findSubStr(%playerName, "Minotaur") != -1 ||
		   String::findSubStr(%playerName, "Seal") != -1 ||
		   String::findSubStr(%playerName, "God") != -1 ||
		   String::findSubStr(%playerName, "Void") != -1 ||
		   String::findSubStr(%playerName, "Enemy") != -1)
		{
			%isBotByName = true;
		}
		
		// Check for town bot name patterns (quest NPCs, merchants, etc.)
		// Town bots typically have names like "Troy", "Rufus", "Merchant", etc.
		// But we can't reliably identify them by name alone, so we rely on BotInfoAiName check below
	}

	// CRITICAL: Skip all bots - they have their team set from $BotInfo[botName, TEAM] in SpawnAI()
	// Town bots have BotInfoAiName but no SpawnBotInfo
	// Enemy bots have SpawnBotInfo set
	// Both types already have their team set correctly during spawn, so we shouldn't overwrite it
	
	// FIRST SAFEGUARD: Check Player::isAiControlled - most reliable check for AI bots
	if(Player::isAiControlled(%clientId))
	{
		// This is an AI-controlled entity - do not modify team
		return;
	}
	
	// PHASE 2 FIX: Also check the centralized bot registry for registered bots
	// This provides another layer of protection against team modification
	if(IsBotRegistered(%clientId))
	{
		// Bot is in the registry - absolutely do not modify team
		return;
	}
	
	if(isRPGAI(%clientId) || %isBotByName)
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		
		// If it's a town bot (BotInfoAiName but no SpawnBotInfo) - skip
		if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1 && (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
		{
			// Town bot - don't change their team (should already be set to 0)
			return;
		}
		
		// If it's an enemy bot (has SpawnBotInfo OR matches enemy bot name pattern) - skip
		// Enemy bots have their team set from $BotInfo[botName, TEAM] in SpawnAI()
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Enemy bot - don't change their team (already set from $BotInfo[botName, TEAM])
			return;
		}
		
		// If it matches enemy bot name pattern but SpawnBotInfo isn't set yet (during spawn)
		// This is a newly spawned enemy bot - don't set team, let SpawnAIGetClientId() handle it
		if(%isBotByName)
		{
			// This is an enemy bot being spawned - don't set team here
			// SpawnAIGetClientId() will set the correct team shortly
			return;
		}
	}

	%race = fetchData(%clientId, "RACE");
	%t = $TeamForRace[%race];
	
	// CRITICAL: For players, default to team 0 (friendly) if race is invalid or not set
	// For bots, this should never happen as they have their team set elsewhere
	// NOTE: Team 0 is VALID (Citizen/friendly) - don't treat it as invalid!
	// Only treat empty string or -1 as invalid
	if(%t == "" || %t == -1)
	{
		// Check if this is a bot or player
		%isBot = isRPGAI(%clientId) || Player::isAiControlled(%clientId);
		if(%isBot)
		{
			// Bot with invalid race - default to team 1 (enemy)
			echo("WARNING: UpdateTeam - Invalid team for bot race '" @ %race @ "', defaulting to team 1");
			%t = 1;
		}
		else
		{
			// Player with invalid/missing race - default to team 0 (friendly)
			// Only log if race is actually missing, not when team 0 is correct
			if(%race == "" || %race == -1)
			{
				echo("WARNING: UpdateTeam - Missing race for player, defaulting to team 0");
			}
			%t = 0;
		}
	}
	
	// CRITICAL: Validate team is not -1 before setting
	// -1 means team hasn't been set yet, so we should never set it to -1
	// NOTE: 0 is a VALID team (friendly), so don't treat it as invalid!
	if(%t == -1 || %t == "-1")
	{
		// Check if this is a bot or player
		%isBot = isRPGAI(%clientId) || Player::isAiControlled(%clientId);
		if(%isBot)
		{
			echo("ERROR: UpdateTeam - Team is -1 after validation for bot! Defaulting to team 1 for clientId " @ %clientId);
			%t = 1;
		}
		else
		{
			echo("ERROR: UpdateTeam - Team is -1 after validation for player! Defaulting to team 0 for clientId " @ %clientId);
			%t = 0;
		}
	}
	
	GameBase::setTeam(%clientId, %t);
	
	// CRITICAL: For enemy bots, immediately restore team from botTeam if UpdateTeam() changed it
	// This prevents UpdateTeam() from overwriting the correct team set during spawn
	if(isRPGAI(%clientId))
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Enemy bot - restore team from botTeam if stored
			%storedBotTeam = fetchData(%clientId, "botTeam");
			
			// If botTeam is not stored, try to get from BotInfoAiName
			if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
			{
				%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
				if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
				{
					%storedBotTeam = $BotInfo[%botInfoAiName, TEAM];
					if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
					{
						%storedBotTeam = 1; // Default to team 1 if not specified
					}
					// Store it for future use
					storeData(%clientId, "botTeam", %storedBotTeam);
				}
				else
				{
					// Can't determine team - default to 1
					%storedBotTeam = 1;
					storeData(%clientId, "botTeam", 1);
				}
			}
			
			// Validate storedBotTeam is not -1
			if(%storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
			{
				echo("ERROR: UpdateTeam - botTeam is -1 for enemy bot (clientId=" @ %clientId @ "). Defaulting to team 1.");
				%storedBotTeam = 1;
				storeData(%clientId, "botTeam", 1);
			}
			
			// CRITICAL: Check Player object first (team is more reliably set there)
			// Then fall back to client ID if Player object doesn't exist
			%playerObjForTeam = Client::getOwnedObject(%clientId);
			%currentTeam = -1;
			if(%playerObjForTeam != -1 && %playerObjForTeam != "")
			{
				%currentTeam = GameBase::getTeam(%playerObjForTeam);
			}
			// If still -1, check client ID
			if(%currentTeam == -1)
			{
				%currentTeam = GameBase::getTeam(%clientId);
			}
			
			// CRITICAL: If current team is -1, ALWAYS restore it
			// If current team doesn't match stored team, restore it
			if(%currentTeam == -1 || %currentTeam != %storedBotTeam)
			{
				// Team was changed by UpdateTeam() or is -1 - restore it immediately
				GameBase::setTeam(%clientId, %storedBotTeam);
				%playerObjForTeam = Client::getOwnedObject(%clientId);
				if(%playerObjForTeam != -1 && %playerObjForTeam != "")
					GameBase::setTeam(%playerObjForTeam, %storedBotTeam);
				if(%currentTeam == -1)
				{
					if($BOT_TEAM_DEBUG) echo("[BOT TEAM DEBUG] UpdateTeam - Restored enemy bot team from -1 to " @ %storedBotTeam @ " for clientId " @ %clientId);
				}
				else
				{
					echo("WARNING: UpdateTeam - Restored enemy bot team from " @ %currentTeam @ " to " @ %storedBotTeam @ " for clientId " @ %clientId);
				}
			}
		}
	}
}
function ChangeRace(%clientId, %race)
{
	dbecho($dbechoMode, "ChangeRace(" @ %clientId @ ", " @ %race @ ")");

	if(%race == "DeathKnight")
		storeData(%clientId, "RACE", "DeathKnight");
	else if(%race == "Human")
		storeData(%clientId, "RACE", Client::getGender(%clientId) @ "Human");
	else if(%race == "MaleHuman")
		storeData(%clientId, "RACE", "MaleHuman");
	else if(%race == "FemaleHuman")
		storeData(%clientId, "RACE", "FemaleHuman");

	setHP(%clientId, fetchData(%clientId, "MaxHP"));
	
	// CRITICAL: Check if AdminBoots wants to preserve mana (prevents mana refresh on stance changes)
	%preserveMana = fetchData(%clientId, "AdminBootsPreserveMana");
	if(%preserveMana != "" && %preserveMana != -1 && %preserveMana != "0")
	{
		// AdminBoots wants to preserve mana - restore it instead of setting to full
		%maxMana = fetchData(%clientId, "MaxMANA");
		if(%preserveMana < %maxMana)
			setMANA(%clientId, %preserveMana);
		else
			setMANA(%clientId, fetchData(%clientId, "MaxMANA"));
	}
	else
	{
		// Normal behavior - set mana to full
		setMANA(%clientId, fetchData(%clientId, "MaxMANA"));
	}

	RefreshAll(%clientId);
	
}


// Clear temporary player state variables (for players only)
// This clears temporary flags and state that should be reset on connect/load
function ClearPlayerVariables(%clientId)
{
	dbecho($dbechoMode2, "ClearPlayerVariables(" @ %clientId @ ")");

	// CRITICAL SAFEGUARD: Only clear variables for players, not bots
	if(isRPGAI(%clientId) || Player::isAiControlled(%clientId))
	{
		echo("WARNING: ClearPlayerVariables - Attempted to clear variables for bot clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ "). Use ClearVariables() for bots instead.");
		return;
	}

	%name = Client::getName(%clientId);

	// Clear temporary client state variables
	%clientId.IsInvalid = "";
	%clientId.currentShop = "";
	%clientId.currentBank = "";
	%clientId.currentSmith = "";
	%clientId.currentBeltBank = "";
	%clientId.currentBeltSell = "";
	%clientId.adminLevel = "";
	%clientId.lastWaitActionTime = "";
	%clientId.choosingGroup = "";
	%clientId.choosingClass = "";
	%clientId.possessId = "";
	%clientId.sleepMode = "";
	%clientId.lastSaveCharTime = "";
	%clientId.replyTo = "";
	%clientId.lastTriggerTime = "";
	%clientId.lastFireTime = "";
	%clientId.lastItemPickupTime = "";
	%clientId.MusicTicksLeft = "";
	%clientId.doExport = "";
	%clientId.TryingToSteal = "";
	%clientId.lastGetWeight = "";
	%clientId.echoOff = "";
	%clientId.lastMissMessage = "";
	%clientId.lastMinePos = "";
	%clientId.bulkNum = "";
	%clientId.zoneLastPos = "";

	// Clear possessedBy
	$possessedBy[%clientId] = "";

	// Clear quest counters for town bots
	for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
	{
		$state[%id, %clientId] = "";
		%tempCounter = $QuestCounter[%name, %id.name];
		if(%tempCounter != "")
			$QuestCounter[%name, %id.name] = "";
	}

	// Clear damage tracking
	for(%i = 1; %i <= $maxDamagedBy; %i++)
		$damagedBy[%name, %i] = "";

	// CRITICAL: Clear all skills before loading character data
	// Skills will be reloaded from the character file after this
	SetAllSkills(%clientId, "");

	// Clear events
	ClearEvents(%clientId);

	// Clear bonus state variables
	deleteVariables("BonusState" @ %clientId @ "*");
	deleteVariables("BonusStateCnt" @ %clientId @ "*");

	// Clear client data variables
	deleteVariables("ClientData" @ %clientId @ "*");
}

// Clear bot variables (for bots only)
// This is the original ClearVariables function, now restricted to bots only
function ClearVariables(%clientId)
{
	dbecho($dbechoMode2, "ClearVariables(" @ %clientId @ ")");

	// CRITICAL SAFEGUARD: Prevent cleaning up player clientIDs
	// Only clear variables for bots (AI-controlled entities), not real players
	if(!isRPGAI(%clientId) && !Player::isAiControlled(%clientId))
	{
		echo("WARNING: ClearVariables - Attempted to clear variables for player clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ "). Use ClearPlayerVariables() for players instead.");
		return;
	}
	
	// ADDITIONAL SAFEGUARD: Only clear variables for bots that have fully loaded and spawned
	// This prevents cleaning up bots that are still initializing or in an invalid state
	// CRITICAL: Double-check this is actually a bot (not a player) before proceeding
	// Even though first check should catch players, this provides extra protection
	if(!isRPGAI(%clientId) && !Player::isAiControlled(%clientId))
	{
		echo("WARNING: ClearVariables - Second check detected player clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ "). Use ClearPlayerVariables() for players instead.");
		return;
	}
	
	if(!fetchData(%clientId, "HasLoadedAndSpawned"))
	{
		echo("WARNING: ClearVariables - Bot clientId " @ %clientId @ " has not loaded and spawned yet (HasLoadedAndSpawned not set). Skipping cleanup to prevent data corruption.");
		return;
	}

	%name = Client::getName(%clientId);

	//clear variables

	ClearFunkVar(%name);

	$possessedBy[%clientId] = "";

	//this is only for bots
	// Use temp variable to safely check if BotInfoAiName exists before accessing
	%tempBotName = fetchData(%clientId, "BotInfoAiName");
	if(%tempBotName != "")
		$BotFollowDirective[%tempBotName] = "";

	//clear directives
	$aidirectiveTable[%clientId, 99] = "";

	%clientId.IsInvalid = "";
	%clientId.currentShop = "";
	%clientId.currentBank = "";
	%clientId.currentSmith = "";
	%clientId.currentBeltBank = "";
	%clientId.currentBeltSell = "";
	%clientId.adminLevel = "";
	%clientId.lastWaitActionTime = "";
	%clientId.choosingGroup = "";
	%clientId.choosingClass = "";
	%clientId.possessId = "";
	%clientId.sleepMode = "";
	%clientId.lastSaveCharTime = "";
	%clientId.replyTo = "";
	// TEMPORARILY DISABLED - stealType clearing
	//%clientId.stealType = "";
	%clientId.lastTriggerTime = "";
	%clientId.lastFireTime = "";
	%clientId.lastItemPickupTime = "";
	%clientId.MusicTicksLeft = "";
	%clientId.doExport = "";
	%clientId.TryingToSteal = "";
	%clientId.lastGetWeight = "";
	%clientId.echoOff = "";
	%clientId.lastMissMessage = "";
	%clientId.lastMinePos = "";
	%clientId.bulkNum = "";
	%clientId.zoneLastPos = "";

	for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
	{
		$state[%id, %clientId] = "";
		// Use temp variable to safely check if QuestCounter exists before accessing
		%tempCounter = $QuestCounter[%name, %id.name];
		if(%tempCounter != "")
			$QuestCounter[%name, %id.name] = "";
	}

	for(%i = 1; %i <= $maxDamagedBy; %i++)
		$damagedBy[%name, %i] = "";

	SetAllSkills(%clientId, "");

	ClearEvents(%clientId);

	deleteVariables("BonusState" @ %clientId @ "*");
	deleteVariables("BonusStateCnt" @ %clientId @ "*");

	deleteVariables("ClientData" @ %clientId @ "*");
	
	// PRIORITY 1: Use unified ClearAllBotData() for all bot data clearing
	// This consolidates cleanup from multiple locations and ensures nothing is missed
	ClearAllBotData(%clientId, false);
}
function ClearFunkVar(%name)
{
	dbecho($dbechoMode2, "ClearFunkVar(" @ %name @ ")");

	%method = 1;
	if(%method == 0)
	{
		//clear regular data
		for(%i = 1; %i <= 35; %i++)
		{
			$funk::var["[\"" @ %name @ "\", 0, " @ %i @ "]"] = "";
			$funk::var[%name, 0, %i] = "";
		}
	
		for(%i = 1; $funk::var["[\"" @ %name @ "\", 2, " @ %i @ "]"] != ""; %i++)
			$funk::var["[\"" @ %name @ "\", 2, " @ %i @ "]"] = "";
		for(%i = 1; $funk::var[%name, 2, %i] != ""; %i++)
			$funk::var[%name, 2, %i] = "";
	
		for(%i = 1; $funk::var["[\"" @ %name @ "\", 3, " @ %i @ "]"] != ""; %i++)
			$funk::var["[\"" @ %name @ "\", 3, " @ %i @ "]"] = "";
		for(%i = 1; $funk::var[%name, 3, %i] != ""; %i++)
			$funk::var[%name, 3, %i] = "";
	
		for(%i = 1; $funk::var["[\"" @ %name @ "\", 4, " @ %i @ "]"] != ""; %i++)
			$funk::var["[\"" @ %name @ "\", 4, " @ %i @ "]"] = "";
		for(%i = 1; $funk::var[%name, 4, %i] != ""; %i++)
			$funk::var[%name, 4, %i] = "";
	}
	else
	{
		deleteVariables("funk::var[\"" @ %name @ "\"*");
		deleteVariables("funk::var" @ %name @ "*");
	}

}


function Down(%t)
{
	dbecho($dbechoMode, "Down(" @ %t @ ")");

	%tinsec = %t * 60;
	for(%i = %t; %i > 1; %i--)
	{
		%a = (%tinsec - (60 * %i));
		schedule("dmsg(" @ %i @ ", \"minutes\");", %a);
	}

	if(%tinsec > 60)
		%startfrom = 60;
	else
		%startfrom = %tinsec;

	for(%i = %startfrom; %i >= 1; %i -= 10)
	{
		%a = (%tinsec - %i);
		schedule("dmsg(" @ %i @ ", \"seconds\");", %a);
	}
	
	// CRITICAL: Set shutdown flag BEFORE scheduling saves
	// This prevents GUI functions (bottomprint, etc.) from being called during shutdown
	$ServerShuttingDown = true;
	
	// Save all characters and world 10 seconds before shutdown
	if(%tinsec >= 10)
	{
		%saveTime = %tinsec - 10;
		schedule("SaveAllCharacters(); SaveWorld();", %saveTime);
	}
	else
	{
		// If shutdown time is less than 10 seconds, save immediately
		SaveAllCharacters();
		SaveWorld();
	}
	
	// CRITICAL: Skip focusServer() during shutdown - it tries to load GUI elements
	// On dedicated servers or when GUI is torn down, this causes the MainWindow error
	// Just call quit() directly instead
	// CRITICAL: Clear large in-memory strings before quit to prevent engine buffer issues
	schedule("ClearLargePlayerDataBeforeQuit(); quit();", %tinsec);
}
function d(%t)
{
	Down(%t);
}

// ClearLargePlayerDataBeforeQuit - Clears large in-memory strings before quit()
// This prevents engine buffer issues during shutdown caused by very long strings
// in player data that may be processed during engine cleanup
function ClearLargePlayerDataBeforeQuit()
{
	echo("[SHUTDOWN] Clearing large player data before quit...");
	
	for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
	{
		// Skip bots - only clear player data
		if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
			continue;
		
		%name = Client::getName(%clientId);
		if(%name == "" || %name == -1)
			continue;
		
		// Clear large in-memory strings that could cause engine buffer issues
		// Data is already saved to file at this point, so clearing is safe
		storeData(%clientId, "BankStorage", "");
		storeData(%clientId, "spawnStuff", "");
		
		echo("[SHUTDOWN] Cleared large data for " @ %name);
	}
	
	echo("[SHUTDOWN] Large player data cleared, proceeding with quit...");
}

function dmsg(%i, %w)
{
	echo("========= SERVER RESTARTING IN " @ %i @ " " @ %w @ " =========");
	messageAll(1, "Server restarting in " @ %i @ " " @ %w @ ". All characters will be saved automatically.");
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
			schedule("Item::Pop(" @ %lootbag @ ");", $LootbagPopTime, %lootbag);
			// CRITICAL FIX: Validate client ID exists before using it in scheduled function
			// The bot/player may be deleted by the time this runs, so check first
			schedule("%tempClientId = " @ %clientId @ "; %tempLootbag = " @ %lootbag @ "; if(Client::getName(%tempClientId) != \"\" && Client::getName(%tempClientId) != -1) { storeData(%tempClientId, \"lootbaglist\", RemoveFromCommaList(fetchData(%tempClientId, \"lootbaglist\"), %tempLootbag)); }", $LootbagPopTime, %lootbag);
		}
	}

	%loot = %ownerName @ " " @ %namelist @ " " @ %loot;

	$loot[%lootbag] = %loot;
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


function round(%n)
{
//	dbecho($dbechoMode, "round(" @ %n @ ")");

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

function RefreshAll(%clientId, %fromSkillUpgrade)
{
	// WATCHDOG: Track this function for freeze detection
	Watchdog_Enter("RefreshAll");
	
	if($AI_DEBUG_ENABLED) echo("[DOT_OP_DEBUG] RefreshAll: ENTRY - clientId=" @ %clientId @ ", fromSkillUpgrade=" @ %fromSkillUpgrade);
	dbecho($dbechoMode, "RefreshAll(" @ %clientId @ ", " @ %fromSkillUpgrade @ ")");

	// DEBUG: Log when RefreshAll is called from a skill upgrade to track frequency and identify spam
	// CRITICAL: Validate player object exists before logging debug info to prevent errors
	if(($AI_DEBUG_ENABLED || $AI_SPAWN_DEBUG) && (%fromSkillUpgrade == "true" || %fromSkillUpgrade == "1" || %fromSkillUpgrade == 1))
	{
		%playerObjCheck = Client::getOwnedObject(%clientId);
		if(%playerObjCheck != -1 && %playerObjCheck != "")
		{
			%playerName = Client::getName(%clientId);
			if(%playerName == "" || %playerName == -1)
				%playerName = "Unknown";
			%isBot = isRPGAI(%clientId);
			%botInfo = "";
			if(%isBot)
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				if(%botName == "" || %botName == "0")
					%botName = fetchData(%clientId, "SpawnBotInfo");
				if(%botName != "" && %botName != "0")
					%botInfo = " (Bot: " @ %botName @ ")";
			}
			echo("[DEBUG RefreshAll] Called from skill upgrade - clientId: " @ %clientId @ ", player: '" @ %playerName @ "'" @ %botInfo);
		}
	}

//	echo("===== DEBUG RefreshAll: START =====");
//echo("DEBUG RefreshAll: clientId = " @ %clientId);

	// CRITICAL: Validate client ID and player object exist before proceeding
	if(%clientId == -1 || %clientId == "" || %clientId == "0")
	{
		// Clear scheduled flag for invalid client ID
		$RefreshAllScheduled[%clientId] = "";
		return;  // Invalid client ID
	}
	
	// CRITICAL: Early check for disconnected clients - if Client::getName() returns empty, client has disconnected
	// This prevents RefreshAll from running on disconnected clients (which causes spam from scheduled calls)
	%clientName = Client::getName(%clientId);
	if(%clientName == "" || %clientName == -1)
	{
		// Client has disconnected - clear scheduled flag and silently return to prevent spam from scheduled RefreshAll calls
		$RefreshAllScheduled[%clientId] = "";
		return;
	}
	
	// CRITICAL: Check player object exists early (before any other processing)
	// This prevents errors when RefreshAll is called on disconnected clients or deleted bots
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Player object doesn't exist - clear scheduled flag and silently return
		// This can happen for bots that are being deleted or clients that have disconnected
		$RefreshAllScheduled[%clientId] = "";
		return;
	}
	
	// CRITICAL: Prevent multiple scheduled RefreshAll calls from stacking up
	// If a RefreshAll is already scheduled, don't schedule another one (prevents spam from rapid skill upgrades)
	// Check if RefreshAll is already scheduled for this client
	if($RefreshAllScheduled[%clientId] == "true" || $RefreshAllScheduled[%clientId] == "1")
	{
		// RefreshAll is already scheduled - don't run another one
		return;
	}
	
	// Clear scheduled flag if we're proceeding with RefreshAll immediately
	$RefreshAllScheduled[%clientId] = "";
	
	// Player object was already validated above, so we can use it directly here
	%playerName = Client::getName(%clientId);
	
	// CRITICAL: For enemy bots, skip most RefreshAll functionality since they use simplified AI system
	// Only do minimal updates (team restoration) and skip player-specific logic
	// EXCEPTION: Seal battle bots need full stat calculations (MaxHP, MaxMANA, etc.) so they are NOT treated as regular enemy bots
	%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
	%isEnemyBot = false;
	if(isRPGAI(%clientId))
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			// Only treat as enemy bot if it's NOT a seal battle bot
			// Seal battle bots need full RefreshAll() calculations for scaled stats
			if(%isSealBattleBot != "true" && %isSealBattleBot != "True" && %isSealBattleBot != "1")
			{
				%isEnemyBot = true;
			}
		}
	}
	
	// For enemy bots (but NOT seal battle bots), use dedicated RefreshAllEnemyBot function
	// This function does NOT touch the team at all - it only does minimal updates
	if(%isEnemyBot)
	{
		// Call dedicated enemy bot refresh function that does NOT touch team
		RefreshAllEnemyBot(%clientId);
		// Skip all other RefreshAll functionality for enemy bots
		return;
	}


	// CRITICAL: Skip AdminBoots handling for town bots - they shouldn't have AdminBoots and don't need race changes
	%isTownBot = false;
	if(isRPGAI(%clientId))
	{
		%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		// Town bots have BotInfoAiName but no SpawnBotInfo
		if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0" && (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
		{
			%isTownBot = true;
		}
	}
	
	// Check if AdminBoots are equipped and trigger race change sequence if needed (skip for town bots)
	if(!%isTownBot)
	{
		// CRITICAL: Re-validate player object before calling Player::getItemCount
		%playerCheck = Client::getOwnedObject(%clientId);
		if(%playerCheck == -1 || %playerCheck == "")
		{
			// Player object was deleted between initial check and this call
			echo("[DEBUG getItemCount] RefreshAll - Player object deleted before AdminBoots check, clientId: " @ %clientId);
			return;
		}
		
		%adminBootsCount = 0;
		// CRITICAL: Final validation right before Player::getItemCount call
		%playerCheckFinal = Client::getOwnedObject(%clientId);
		if(%playerCheckFinal != -1 && %playerCheckFinal != "")
		{
			%adminBootsCount = Player::getItemCount(%clientId, "AdminBoots0");
		}
		else
		{
			echo("[DEBUG getItemCount] RefreshAll - Player object invalid right before AdminBoots0 check, clientId: " @ %clientId);
		}
		if(%adminBootsCount > 0)
		{
			%hasTriggered = fetchData(%clientId, "AdminBootsRaceChangeTriggered");
			if(%hasTriggered != "true")
			{
				storeData(%clientId, "AdminBootsRaceChangeTriggered", "true");
				// Store original armor
				%originalArmor = Player::getArmor(%clientId);
				storeData(%clientId, "AdminBootsOriginalArmor", %originalArmor);
				// Start the race change sequence immediately
				AdminBoots::startRaceChangeSequence(%clientId);
			}
		}
		else
		{
			// AdminBoots not equipped - clear the trigger flag if it exists
			%hasTriggered = fetchData(%clientId, "AdminBootsRaceChangeTriggered");
			if(%hasTriggered == "true")
			{
				storeData(%clientId, "AdminBootsRaceChangeTriggered", "");
			}
		}
	}

	%race = fetchData(%clientId, "RACE");
	//echo("DEBUG RefreshAll: RACE = '" @ %race @ "'");
	if(String::findSubStr(%race, "Human") != -1)
	{
//		echo("DEBUG RefreshAll: Calling RefreshWeight...");
		// CRITICAL: GetWeight() must be called before RefreshWeight() to set $GetWeight::ArmorMod
		// This ensures accessories with SpecialVar type 8 (like Wind Paws) properly apply their modifiers
		GetWeight(%clientId);
		RefreshWeight(%clientId);
//		echo("DEBUG RefreshAll: RefreshWeight completed");
	}

//	echo("DEBUG RefreshAll: Calling UpdateAppearance...");
	// CRITICAL: Skip UpdateAppearance for bots - they have their armor set during spawn
	// UpdateAppearance was a legacy workaround for non-engine spawning that caused
	// town bots to have their armor changed to AdminArmor immediately after spawn
	%isBot = isRPGAI(%clientId);
	%clientName = Client::getName(%clientId);
	
	// Recursion Guard: Prevent infinite loops if callbacks trigger RefreshAll again
	if($InRefreshAll[%clientId]) return;
	$InRefreshAll[%clientId] = true;

	if($TOWNBOT_ARMOR_DEBUG) echo("[TOWNBOT ARMOR DEBUG] RefreshAll: clientId=" @ %clientId @ " name='" @ %clientName @ "' isRPGAI=" @ %isBot);
	if(!%isBot)
	{
		if($TOWNBOT_ARMOR_DEBUG) echo("[TOWNBOT ARMOR DEBUG] RefreshAll: Calling UpdateAppearance for " @ %clientId);
		UpdateAppearance(%clientId);
	}
	else
	{
		if($TOWNBOT_ARMOR_DEBUG) echo("[TOWNBOT ARMOR DEBUG] RefreshAll: SKIPPING UpdateAppearance for bot " @ %clientId);
	}
//	echo("DEBUG RefreshAll: UpdateAppearance completed");

//	echo("DEBUG RefreshAll: Calling refreshHPREGEN...");
	refreshHPREGEN(%clientId);
//	echo("DEBUG RefreshAll: refreshHPREGEN completed");

//	echo("DEBUG RefreshAll: Calling refreshMANAREGEN...");
	refreshMANAREGEN(%clientId);
//	echo("DEBUG RefreshAll: refreshMANAREGEN completed");

//	echo("DEBUG RefreshAll: Calling Game::refreshClientScore...");
	Game::refreshClientScore(%clientId);
//	echo("DEBUG RefreshAll: Game::refreshClientScore completed");

	// NOTE: Enemy bot team restoration is now handled at the beginning of RefreshAll()
	// This code is kept for town bots (if any) but enemy bots return early

	// Ensure AdminBootsArmor is set if AdminBoots are equipped (skip for town bots)
	if(!%isTownBot)
	{
		// The armor itself has the speed values baked in, so we just need to ensure it's applied
		// Re-check player object exists (it might have been deleted during RefreshAll)
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == -1 || %playerObj == "")
		if(%playerObj == -1 || %playerObj == "")
		{
			// Player object was deleted during RefreshAll - silently return
			$InRefreshAll[%clientId] = false;
			return;
		}
		
		// CRITICAL: Re-validate player object before calling Player::getItemCount
		%playerCheck2 = Client::getOwnedObject(%clientId);
		if(%playerCheck2 == -1 || %playerCheck2 == "")
		if(%playerCheck2 == -1 || %playerCheck2 == "")
		{
			// Player object was deleted between check and this call
			$InRefreshAll[%clientId] = false;
			return;
		}
		
		// CRITICAL: Final validation right before Player::getItemCount call
		%playerCheckFinal2 = Client::getOwnedObject(%clientId);
		%adminBootsCount = 0;
		if(%playerCheckFinal2 != -1 && %playerCheckFinal2 != "")
		{
			%adminBootsCount = Player::getItemCount(%clientId, "AdminBoots0");
		}
		else
		{
			echo("[DEBUG getItemCount] RefreshAll (AdminBootsArmor check) - Player object invalid right before AdminBoots0 check, clientId: " @ %clientId);
		}
		if(%adminBootsCount > 0)
		{
			// Make sure AdminBootsArmor is set (in case UpdateAppearance changed it)
			// BUG FIX: Recognize all specialized AdminBoots variants to prevent redundant overrides
			if(String::findSubStr(Player::getArmor(%clientId), "AdminBoots") == -1)
			{
				// Check if player is wearing a Robe
				%isRobed = false;
				
				// CRITICAL FIX: Use GetAccessoryList to find the armor, just like UpdateAppearance does.
				// Player::getMountedItem does NOT work for RPG accessories/armors.
				%list = GetAccessoryList(%clientId, 2, "3 7");
				for(%i = 0; (%w = getCroppedItem(GetWord(%list, %i))) != -1; %i++)
				{
					if($AccessoryVar[%w, $AccessoryType] == $BodyAccessoryType)
					{
						// Found body armor, check if it's a robe
						if($ArmorPlayerModel[%w] == "Robed")
						{
							%isRobed = true;
							break;
						}
					}
				}
				
				if(%isRobed)
					Player::setArmor(%clientId, "AdminBootsRobedArmor");
				else
					Player::setArmor(%clientId, "AdminBootsArmor");
			}
		}
	}

	// NOTE: Enemy bot team restoration is now handled at the beginning of RefreshAll()
	// Enemy bots return early, so this code only runs for players and town bots

//	echo("===== DEBUG RefreshAll: COMPLETE =====");
	
	// WATCHDOG: Clear tracking for this function
	Watchdog_Exit();

	// CRITICAL: Clear the skill upgrade refresh flag ONLY when the top-level 
	// call from UseSkill completes (indicated by %fromSkillUpgrade).
	// This prevents nested RefreshAll calls (like those in the AdminBoots sequence)
	// from dropping the guard while the refresh process is still busy.
	if(%fromSkillUpgrade == "true" || %fromSkillUpgrade == "1" || %fromSkillUpgrade == 1)
	{
		$SkillUpgradeRefreshScheduled[%clientId] = "";
	}
	
	// Release Recursion Guard
	$InRefreshAll[%clientId] = false;
}

// CRITICAL: New function specifically for enemy bots - does NOT touch team at all
// This is an exact replica of the enemy bot handling in RefreshAll, but without any team restoration
function RefreshAllEnemyBot(%clientId)
{
	dbecho($dbechoMode, "RefreshAllEnemyBot(" @ %clientId @ ")");

	// CRITICAL: Validate client ID and player object exist before proceeding
	if(%clientId == -1 || %clientId == "" || %clientId == "0")
	{
		return;  // Invalid client ID
	}
	
	// CRITICAL: Early check for disconnected clients
	%clientName = Client::getName(%clientId);
	if(%clientName == "" || %clientName == -1)
	{
		return;
	}
	
	// CRITICAL SAFEGUARD: Check if this is a real player with a save file FIRST
	// If a character save file exists, this is DEFINITELY a real player, NOT a bot
	// This prevents RefreshAllEnemyBot from running on players, which could cause client crashes
	if(%clientName != "" && %clientName != -1)
	{
		%characterFile = "temp\\" @ %clientName @ ".cs";
		if(isFile(%characterFile))
		{
			// This is a real player with a save file - abort immediately to prevent client crash
			echo("[CRITICAL] RefreshAllEnemyBot(): SAFEGUARD - Detected real player " @ %clientName @ " (clientId=" @ %clientId @ ") with save file. Aborting to prevent client crash.");
			return;
		}
	}
	
	// CRITICAL: Additional safeguard - verify this is actually a bot
	// Check for bot markers (BotInfoAiName, SpawnBotInfo, or in bot registry)
	%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
	%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
	%inRegistry = ($BotRegistry[%clientId] != "" && $BotRegistry[%clientId] != -1);
	
	// If none of these bot markers exist, this is likely a player - abort
	if((%botInfoAiName == "" || %botInfoAiName == -1 || %botInfoAiName == "0") && 
	   (%spawnBotInfo == "" || %spawnBotInfo == -1 || %spawnBotInfo == "0") && 
	   !%inRegistry)
	{
		// No bot markers found - this is likely a player, abort to prevent client crash
		echo("[CRITICAL] RefreshAllEnemyBot(): SAFEGUARD - No bot markers found for clientId " @ %clientId @ " (" @ %clientName @ "). Aborting to prevent client crash.");
		return;
	}
	
	// CRITICAL: Check player object exists early
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		return;
	}
	
	// CRITICAL: Clear stance for enemy bots - they should not have stances enabled
	// This ensures stance is always cleared even if something else tries to set it
	storeData(%clientId, "Stance", "");
	
	// DO NOT TOUCH TEAM - This function is specifically designed to NOT modify team
	// The team should already be set correctly by SpawnAIGetClientId() or AI::setWeapons()
	// Any team modifications here could cause the team to reset to -1
	
	// Skip all other RefreshAll functionality for enemy bots
	return;
}

function HasThisStuff(%clientId, %list, %multiplier)
{
	dbecho($dbechoMode, "HasThisStuff(" @ %clientId @ ", " @ %list @ ")");

	if(%list == "")
		return True;

	if(%multiplier == "" || %multiplier <= 0)
		%multiplier = 1;

	%name = Client::getName(%clientId);

	//--------
	// PASS 1
	//--------
	%flag = False;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "LVLG")
		{
			if(fetchData(%clientId, "LVL") > %w2)
				%flag = True;
			else
				%flag = 667;
		}
		else if(%w == "LVLS")
		{
			if(fetchData(%clientId, "LVL") < %w2)
				%flag = True;
			else
				%flag = 667;
		}
		else if(%w == "LVLE")
		{
			if(fetchData(%clientId, "LVL") == %w2)
				%flag = True;
			else
				%flag = 667;
		}
	}

	if(%flag == 667)
		return %flag;


	//--------
	// PASS 2
	//--------
	%cntindex = 0;
	%flag = False;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "CNT")
		{
			%cntindex++;
			%tmpcnt[%cntindex] = %w2;
		}
		else if(%w == "CNTAFFECTS")
		{
			%tmpcntaffects[%cntindex] = %w2;
		}
	}

	//Process the counter data, if any
	for(%i = 1; %tmpcnt[%i] != ""; %i++)
	{
		if(%tmpcnt[%i] != "" && %tmpcntaffects[%i] != "")
		{
			%firstchar = String::getSubStr(%tmpcnt[%i], 0, 1);
			%n = floor(String::getSubStr(%tmpcnt[%i], 1, 9999));
			if(%firstchar == "<")
			{
				if($QuestCounter[%name, %tmpcntaffects[%i]] < %n)
					%flag = True;
				else
					%flag = 666;
			}
			else if(%firstchar == ">")
			{
				if($QuestCounter[%name, %tmpcntaffects[%i]] > %n)
					%flag = True;
				else
					%flag = 666;
			}
			else if(%firstchar == "=")
			{
				if($QuestCounter[%name, %tmpcntaffects[%i]] == %n)
					%flag = True;
				else
					%flag = 666;
			}
		}
		if(%flag == 666)
			return %flag;
	}


	//--------
	// PASS 3
	//--------
	%flag = True;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "COINS")
		{
			if(fetchData(%clientId, "COINS") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "REMORT")
		{
			if(fetchData(%clientId, "RemortStep") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "TOURNYRANK")
		{
			if(fetchData(%clientId, "TournyRank") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "RankPoints")
		{
			if(fetchData(%clientId, "RankPoints") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "AI")
		{
			%isAI = Player::isAIcontrolled(%clientId);
			if(%isAI == %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w == "EXP")
		{
			if(fetchData(%clientId, "EXP") >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(isBeltItem(%w))
		{
			%amnt = Belt::HasThisStuff(%clientid,%w);
			if(%amnt >= %w2)
				%flag = True;
			else
				return False;
		}
		else if(%w != "COINS" && %w != "REMORT" && %w != "LVLG" && %w != "LVLS" && %w != "LVLE" && %w != "CNT" && %w != "CNTAFFECTS" && %w != "RankPoints" && %w != "AI" && %w != "EXP" && %w != "TOURNYRANK")
		{
			if(Player::getItemCount(%clientId, %w) >= %w2)
				%flag = True;
			else
				return False;
		}
	}

	return %flag;
}

function TakeThisStuff(%clientId, %list, %multiplier)
{
	dbecho($dbechoMode, "TakeThisStuff(" @ %clientId @ ", " @ %list @ ")");

	if(%multiplier == "" || %multiplier <= 0)
		%multiplier = 1;

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "COINS")
		{
			if(fetchData(%clientId, "COINS") >= %w2)
				storeData(%clientId, "COINS", %w2, "dec");
			else
				return False;
		}
		else if(%w == "EXP")
		{
			if(fetchData(%clientId, "EXP") >= %w2)
				storeData(%clientId, "EXP", %w2, "dec");
			else
				return False;
		}
		else if(%w == "CNT" || %w == "CNTAFFECTS" || %w == "LVLG" || %w == "LVLS" || %w == "LVLE")
		{
			//ignore
		}
		else if(isBeltItem(%w))
		{
			%amount = Belt::HasThisStuff(%clientid,%w);
			if(%amount >= %w2)
				Belt::TakeThisStuff(%clientid,%w,%w2);
			else
				return False;
		}
		else
		{
			%amount = Player::getItemCount(%clientId, %w);
			if(%amount >= %w2)
				Player::setItemCount(%clientId, %w, %amount-%w2);
			else
				return False;
		}
	}

	return True;
}

function GiveThisStuff(%clientId, %list, %echo, %multiplier)
{
	// Initialize %list to empty string if not provided
	if(%list == "")
		%list = "";
	
	// CRITICAL: Validate clientId before proceeding
	// If clientId is False, -1, empty, or invalid, return early to prevent errors
	if(%clientId == "False" || %clientId == "false" || %clientId == -1 || %clientId == "" || %clientId == "0")
	{
		echo("ERROR: GiveThisStuff - Invalid clientId: " @ %clientId @ ". Cannot give items.");
		return;
	}
	
	dbecho($dbechoMode, "GiveThisStuff(" @ %clientId @ ", " @ %list @ ", " @ %echo @ ")");

	%name = Client::getName(%clientId);
	
	// CRITICAL: Validate that we got a valid name (bot/player still exists)
	if(%name == "" || %name == -1)
	{
		echo("ERROR: GiveThisStuff - could not find player object for clientId " @ %clientId @ ". Bot/player may have been deleted.");
		return;
	}

	if(%multiplier == "" || %multiplier <= 0)
		%multiplier = 1;

	%cntindex = 0;
	
	// CRITICAL: Track if this is spawn equipment for enemy bots (to store in OriginalLootString with percentage format)
	// The roll will happen on death, not on spawn
	%isProcessingSpawnEquipment = false;
	if(isRPGAI(%clientId))
	{
		%isLootbagPickup = (fetchData(%clientId, "IsLootbagPickup") == "true");
		%isProcessingSpawnEquipment = (!%isLootbagPickup && (fetchData(%clientId, "SpawnBotInfo") != ""));
	}

	for(%i = 0; GetWord(%list, %i) != -1; %i+=2)
	{
		%w = GetWord(%list, %i);
		%w2 = GetWord(%list, %i+1);
		
		// Debug logging for LCK processing
		if(isRPGAI(%clientId) && %w == "LCK")
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = "Unknown";
			//echo("[SPAWN DEBUG] GiveThisStuff(): Found LCK in loop - w='" @ %w @ "', w2='" @ %w2 @ "' for bot " @ %botName);
		}

		//if there is a / in %w2, then what trails after the / is the minimum random number between 0 and 100 which
		//is applied as a percentage to the starting number of %w2
		// For AI bots on SPAWN, skip the percentage roll - it will be rolled when the bot dies
		// For AI bots picking up LOOTBAGS, give the items directly (lootbag contents already passed drop chance)
		// For players, roll the percentage immediately
		%spos = String::findSubStr(%w2, "/");
		if(%spos > 0)
		{
			%original = String::getSubStr(%w2, 0, %spos);
			%perc = String::getSubStr(%w2, %spos+1, 99999);

			if(isRPGAI(%clientId))
			{
				// CRITICAL: Check if this is a lootbag pickup using the flag set in Item::onCollision
				// This is more reliable than checking SpawnBotInfo, since bots still have SpawnBotInfo when picking up lootbags
				%isLootbagPickup = (fetchData(%clientId, "IsLootbagPickup") == "true");
				%isSpawnEquipment = (!%isLootbagPickup && (fetchData(%clientId, "SpawnBotInfo") != ""));
				
				// DEBUG: Log when bots receive items via GiveThisStuff
				%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					%botName = fetchData(%clientId, "BotInfoAiName");
					if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
					// Only log quest items, weapons, consumables, and accessories (not LCK, EXP, COINS, etc.)
					if(String::findSubStr(%w, "Spine") != -1 || String::findSubStr(%w, "Fragment") != -1 || 
					   String::findSubStr(%w, "Breath") != -1 || String::findSubStr(%w, "Sword") != -1 ||
					   String::findSubStr(%w, "Potion") != -1 || String::findSubStr(%w, "Vial") != -1 ||
					   String::findSubStr(%w, "AdminBoots") != -1 || String::findSubStr(%w, "Boots") != -1)
					{
					}
				}
				
				if(%isSpawnEquipment)
				{
					// CRITICAL: For AI bots on spawn, give ALL items (no roll on spawn)
					// The roll will happen on death in Player::onKilled
					// Store items in OriginalLootString with percentage format (e.g., "item count/percentage")
					// This allows the death handler to roll once per item
					%w2 = %original;
				}
				else
				{
					// For AI bots picking up lootbags, give the items directly (no percentage roll)
					// Lootbag contents already passed the drop chance when the lootbag was created
					// These items will always drop when the bot dies (handled in playerdamage.cs)
					%w2 = %original;
				}
			}
			else
			{
				// For players, roll the percentage immediately
				// Check if percentage is a range (e.g., "10-15" or "5-10")
				%rangePos = String::findSubStr(%perc, "-");
				%firstChar = String::getSubStr(%perc, 0, 1);
				if(%rangePos > 0 && %firstChar != "-")
				{
					// Variable drop rate range (e.g., 10-15% means roll between 10% and 15%)
					%minPerc = String::getSubStr(%perc, 0, %rangePos);
					%maxPerc = String::getSubStr(%perc, %rangePos + 1, 99999);
					
					// Randomly choose a drop chance within the range
					%chance = %minPerc + floor(getRandom() * (%maxPerc - %minPerc + 1));
					if(%chance > %maxPerc) %chance = %maxPerc;
					
					%roll = floor(getRandom() * 100) + 1;  // Roll 1-100
					
					if(%roll <= %chance)
						%w2 = %original;
					else
						%w2 = 0;
				}
				else if(%perc < 0)
				{
					// Negative percentages indicate rare drops (low chance)
					%absPerc = -%perc;  // Convert -100 to 100
					%roll = floor(getRandom() * 100) + 1;  // Roll 1-100
					
					// For -100, need roll of 100 (1% chance)
					// For -50, need roll >= 51 (50% chance)
					// Formula: need roll >= (100 - absPerc + 1), but for absPerc >= 100, need exact 100
					%minRoll = 100 - %absPerc + 1;
					if(%absPerc >= 100) %minRoll = 100;  // -100 or more rare -> need exactly 100 (1% chance)
					
					if(%roll >= %minRoll)
						%w2 = %original;
					else
						%w2 = 0;
				}
				else
				{
					// Single fixed percentage or "1 in X" format
					// Convert string to number for proper comparison
					%percNum = %perc * 1;
					
					// If number >= 1000, treat as "1 in X" format (e.g., 1/100000 = 1 in 100,000 chance)
					// Otherwise treat as percentage (e.g., 0.001 = 0.001%, 5 = 5%)
					if(%percNum >= 1000)
					{
						// "1 in X" format - roll 1 to X, need exactly 1
						%roll = floor(getRandom() * %percNum) + 1;  // Roll 1 to %percNum
						
						if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %w @ " | Format: 1 in " @ %percNum @ " | Roll: " @ %roll @ " (need roll == 1)");
						
						if(%roll == 1)
						{
							%w2 = %original;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %w @ " will drop (roll " @ %roll @ " == 1)");
						}
						else
						{
							%w2 = 0;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %w @ " will not drop (roll " @ %roll @ " != 1)");
						}
					}
					else if(%percNum < 1)
					{
						// For decimal percentages (e.g., 0.001%), roll 1-100000
						// 0.001% = 1 in 100,000 chance
						%roll = floor(getRandom() * 100000) + 1;  // Roll 1-100000
						%target = %percNum * 1000;  // Convert 0.001 to 1 (for 0.001% = 1 in 100000)
						
						if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %w @ " | Percentage: " @ %percNum @ "% | Roll: " @ %roll @ " | Target: " @ %target @ " (need roll <= " @ %target @ ")");
						
						if(%roll <= %target)
						{
							%w2 = %original;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %w @ " will drop (roll " @ %roll @ " <= " @ %target @ ")");
						}
						else
						{
							%w2 = 0;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %w @ " will not drop (roll " @ %roll @ " > " @ %target @ ")");
						}
					}
					else
					{
						// For normal percentages (>= 1%), roll 1-100
						%roll = floor(getRandom() * 100) + 1;  // Roll 1-100
						
						if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] Item: " @ %w @ " | Percentage: " @ %percNum @ "% | Roll: " @ %roll @ " (need roll <= " @ %percNum @ ")");
						
						if(%roll <= %percNum)
						{
							%w2 = %original;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] SUCCESS - " @ %w @ " will drop (roll " @ %roll @ " <= " @ %percNum @ ")");
						}
						else
						{
							%w2 = 0;
							if($LOOTBAG_DEBUG) echo("[DROP RATE DEBUG] FAILED - " @ %w @ " will not drop (roll " @ %roll @ " > " @ %percNum @ ")");
						}
					}
				}
				
				if(%w2 < 0) %w2 = 0;
			}
		}

		//if there is a d in %w2 AND it has a number on either side, then it's a dice roll
		%dpos = String::findSubStr(%w2, "d");
		%l1 = String::getSubStr(%w2, %dpos-1, 1);
		%l2 = floor(%l1);
		%r1 = String::getSubStr(%w2, %dpos+1, 1);
		%r2 = floor(%r1);
		if(%dpos > 0 && String::ICompare(%l1, %l2) == 0 && String::ICompare(%r1, %r2) == 0)
		{
			%w2 = GetRoll(%w2);
			if(%w2 < 1) %w2 = 1;
		}

		%tw2 = %w2 * 1;
		if(%tw2 == %w2)
			%w2 *= %multiplier;

		if(%w == "COINS")
		{
			// ASCENSION: Gold Digger - +25% coin drops
			if(Ascension::HasTalent(%clientId, "GoldDigger"))
				%w2 = floor(%w2 * 1.25);
			
			storeData(%clientId, "COINS", %w2, "inc");
			if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " coins.");
		}
		else if(%w == "EXP")
		{
			// Check if player is already at max level for their remort
			%currentLevel = fetchData(%clientId, "LVL");
			%remortStep = fetchData(%clientId, "RemortStep");
			%maxLevel = 125 + (%remortStep * 8);
			
			if(%currentLevel >= %maxLevel)
			{
				// Player is already at max level - don't grant EXP
				if(%echo) 
				{
					%msg = "<jc>You can no longer gain experience. Your body has reached it's peak limit!";
					centerprint(%clientId, %msg, floor(String::len(%msg) / 20));
				}
			}
			else
			{
				// Player is below max level - grant EXP
				storeData(%clientId, "EXP", %w2, "inc");
				if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " experience.");
			}
		}
		else if(%w == "LCK")
		{
			// CRITICAL: For seal battle bots, DO NOT overwrite LCK from equipment string
			// Seal battle bots have their LCK set by SealBattle::SetupBot() based on player count
			// This must take precedence over the base LCK from equipment string
			%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
			if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
			{
				// Seal battle bot - skip LCK setting from equipment string
				// SetupBot() will set the correct scaled LCK based on player count
				if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " LCK (ignored for seal battle bot).");
				// Continue to next item without setting LCK
			}
			else
			{
				// For enemy bots on spawn, ALWAYS SET LCK from equipment string (don't increment)
				// This is because clientIds are reused, so LCK from previous bots persists
				// The equipment string represents the bot's base stats, so we should always set it
				%currentLCK = fetchData(%clientId, "LCK");
				%isSpawnPointBot = (fetchData(%clientId, "SpawnBotInfo") != "");
				
				// Debug logging for bots
				if(isRPGAI(%clientId))
				{
					%botName = fetchData(%clientId, "BotInfoAiName");
					if(%botName == "" || %botName == "0") %botName = "Unknown";
					//echo("[SPAWN DEBUG] GiveThisStuff(): Processing LCK for bot " @ %botName @ " (clientId=" @ %clientId @ ")");
					//echo("[SPAWN DEBUG] GiveThisStuff(): currentLCK='" @ %currentLCK @ "', newLCK=" @ %w2 @ ", isSpawnPointBot=" @ %isSpawnPointBot);
				}
				
				// For enemy bots (SpawnPoint bots), always SET LCK from equipment string
				// This ensures bots get their correct base LCK even when clientIds are reused
				if(isRPGAI(%clientId) && %isSpawnPointBot)
				{
					// Enemy bot spawn - always set LCK directly from equipment string
					storeData(%clientId, "LCK", %w2);
					if(isRPGAI(%clientId))
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = "Unknown";
						//echo("[SPAWN DEBUG] GiveThisStuff(): SET LCK to " @ %w2 @ " for enemy bot " @ %botName @ " (was '" @ %currentLCK @ "')");
						%verifyLCK = fetchData(%clientId, "LCK");
						//echo("[SPAWN DEBUG] GiveThisStuff(): Verified LCK='" @ %verifyLCK @ "'");
					}
				}
				else
				{
					// Player or town bot receiving additional LCK - increment
					storeData(%clientId, "LCK", %w2, "inc");
					if(isRPGAI(%clientId))
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = "Unknown";
						//echo("[SPAWN DEBUG] GiveThisStuff(): INCREMENTED LCK by " @ %w2 @ " for bot " @ %botName @ " (current was '" @ %currentLCK @ "')");
						%verifyLCK = fetchData(%clientId, "LCK");
						//echo("[SPAWN DEBUG] GiveThisStuff(): Verified LCK='" @ %verifyLCK @ "'");
					}
				}
				if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " LCK.");
			}
		}
		else if(%w == "SP")
		{
			storeData(%clientId, "SPcredits", %w2, "inc");
			if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " Skill Points.");
		}
		else if(%w == "CLASS")
		{
			storeData(%clientId, "CLASS", %w2);
			storeData(%clientId, "GROUP", $ClassGroup[fetchData(%clientId, "CLASS")]);
		}
		else if(%w == "LVL")
		{
			//note: the class MUST be specified in %stuff prior to this call
			storeData(%clientId, "EXP", GetExp(%w2, %clientId) + 100);
		}
		else if(%w == "REMORT")
		{
			//note: the class MUST be specified in %stuff prior to this call
			storeData(%clientId, "RemortStep", %w2);
		}
		else if(%w == "TOURNYRANK")
		{
			//note: the class MUST be specified in %stuff prior to this call
			storeData(%clientId, "TournyRank", %w2);
		}
		else if(%w == "TEAM")
		{
			GameBase::setTeam(%clientId, %w2);
			if(%echo) Client::sendMessage(%clientId, 0, "Team set to " @ %w2 @ ".");
		}
		else if(%w == "RankPoints")
		{
			storeData(%clientId, "RankPoints", %w2, "inc");
			if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %w2 @ " Rank Points.");
		}
		else if(%w == "CNT")
		{
			%cntindex++;
			%tmpcnt[%cntindex] = %w2;
		}
		else if(%w == "CNTAFFECTS")
		{
			%tmpcntaffects[%cntindex] = %w2;
		}
		else if(isBackpackItem(%w))
		{
			// Validate item and count before giving to belt
			// Skip invalid entries: empty item, item "0", empty count, count -1, count <= 0
			if(%w != "" && %w != -1 && %w != "0" && %w2 != "" && %w2 != -1 && %w2 != "-1" && (%w2 * 1) > 0)
			{
				// DEBUG: Log when bots receive belt items via GiveThisStuff (isBackpackItem path)
				if(isRPGAI(%clientId))
				{
					%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
						%beltType = $BeltItem[%w, "Type"];
					}
				}
				Belt::GiveThisStuff(%clientId, %w, %w2, %echo);
			}
		}
		else
		{
			// Get player object - Item::giveItem expects a player object, not a clientId
			%player = Client::getOwnedObject(%clientId);
			
			// CRITICAL: Validate player object exists before calling Player::getItemCount or Item::giveItem
			if(%player == -1 || %player == "" || !isObject(%player))
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				if(%botName == "")
					%botName = fetchData(%clientId, "SpawnBotInfo");
				if(%botName == "")
					%botName = Client::getName(%clientId);
				echo("[DEBUG getItemCount] GiveThisStuff - Player object not found, clientId: " @ %clientId @ ", bot: " @ %botName @ ", item: " @ %w @ ", count: " @ %w2);
				// Skip giving item if player object doesn't exist
				continue;
			}
			
			// DEBUG: Log when bots receive weapons via GiveThisStuff
			if(isRPGAI(%clientId))
			{
				%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					%botName = fetchData(%clientId, "BotInfoAiName");
					if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
					%itemData = getItemData(%w);
					if(%itemData.className == "Weapon" || String::findSubStr(%w, "Sword") != -1 || String::findSubStr(%w, "Blade") != -1)
					{
						// CRITICAL: Re-validate player object right before Player::getItemCount call
						%playerCheck = Client::getOwnedObject(%clientId);
						if(%playerCheck != -1 && %playerCheck != "" && isObject(%playerCheck))
						{
							%currentWeaponCount = Player::getItemCount(%clientId, %w);
						}
					}
				}
			}
			//echo("[SPAWN DEBUG] GiveThisStuff(): clientId=" @ %clientId @ ", player=" @ %player @ ", item=" @ %w @ ", count=" @ %w2);
			if(%player != "" && %player != -1)
			{
				%result = Item::giveItem(%player, %w, %w2, %echo);
				
				// Weapons are given through Item::giveItem
				// OriginalLootString is already set in Ai.cs with percentage format, so no need to track here
				//echo("[SPAWN DEBUG] GiveThisStuff(): Item::giveItem() returned=" @ %result);
			}
			else
			{
				echo("ERROR: GiveThisStuff - could not find player object for clientId " @ %clientId);
			}
		}
	}

	// CRITICAL: For enemy bots, use RefreshAllEnemyBot() which does NOT touch team
	// For players and other bots (town bots, seal battle bots), use RefreshAll()
	%isEnemyBot = false;
	if(isRPGAI(%clientId))
	{
		%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
		{
			%isSealBattleBot = fetchData(%clientId, "SealBattleBot");
			// Only treat as enemy bot if it's NOT a seal battle bot
			// Seal battle bots need full RefreshAll() calculations for scaled stats
			if(%isSealBattleBot != "true" && %isSealBattleBot != "True" && %isSealBattleBot != "1")
			{
				%isEnemyBot = true;
			}
		}
	}
	
	if(%isEnemyBot)
	{
		// Enemy bot - use RefreshAllEnemyBot() which does NOT touch team
		RefreshAllEnemyBot(%clientId);
	}
	else
	{
		// Player or other bot type - use normal RefreshAll()
		RefreshAll(%clientId);
	}
	
	// CRITICAL: OriginalLootString is already set in Ai.cs with percentage format (e.g., "item count/percentage")
	// The roll will happen on death in Player::onKilled, not on spawn
	// Items not in OriginalLootString but in belt are from lootbags and will always drop 100%

	//Process the counter data, if any
	for(%i = 1; %tmpcnt[%i] != ""; %i++)
	{
		if(%tmpcnt[%i] != "" && %tmpcntaffects[%i] != "")
		{
			%first = String::getSubStr(%tmpcnt[%i], 0, 1);
			if(%first == "+" || %first == "-")
				$QuestCounter[%name, %tmpcntaffects[%i]] += floor(%tmpcnt[%i]);
			else
				$QuestCounter[%name, %tmpcntaffects[%i]] = floor(%tmpcnt[%i]);
		}
	}
}
	
function getSpawnIndex(%aiName)
{
	dbecho($dbechoMode, "getSpawnIndex(" @ %aiName @ ")");

	for(%i = 1; $spawnIndex[%i] != ""; %i++)
	{
		if($spawnIndex[%i] == %aiName)
			return %i;
	}
	return -1;
}

function FellOffMap(%id)
{
	dbecho($dbechoMode, "FellOffMap(" @ %id @ ")");

	RefreshAll(%id);

	if(Player::isAiControlled(%id))
	{
		storeData(%id, "noDropLootbagFlag", True);
		playNextAnim(%id);
		Player::Kill(%id);
	}
	else
	{
		CheckAndBootFromArena(%id);
		Item::setVelocity(%id, "0 0 0");
		TeleportToMarker(%id, "TheArena\\TeleportExitMarkers", 0, 0);

		Client::sendMessage(%id, $MsgRed, "You were restored to the arena exit marker.");
	}
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
			// Try to get the next word as the count
			%amt = GetWord(%a, 1);
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

	if(%player == -1)
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

function SetOnGround(%clientId, %extraZ)
{
	dbecho($dbechoMode, "SetOnGround(" @ %clientId @ ", " @ %extra2 @ ")");

	%maxdist = 5000;

	%origpos = GameBase::getPosition(%clientId);

	%x = GetWord(%origpos, 0);
	%y = GetWord(%origpos, 1);
	%z = GetWord(%origpos, 2);

	%finalpos = %x @ " " @ %y @ " " @ %z + %extraZ;

	GameBase::setPosition(%clientId, %finalpos);

	%index = 0;
	//for(%i = 0; %i >= -3.15; %i -= 1.57)
	for(%i = 0; %i >= -4.725; %i -= 0.785)
	{
		if(GameBase::getLOSinfo(Client::getOwnedObject(%clientId), %maxdist, %i @ " 0 0"))
		{
			%index++;
			%pos[%index] = $los::position;
		}
	}

	%closest = %maxdist+1;
	for(%j = 1; %j <= %index; %j++)
	{
		%dist = Vector::getDistance(%pos[%j], %finalpos);
		if(%dist < %closest)
		{
			%closest = %dist;
			%closestIndex = %j;
		}
	}

	if(%pos[%closestIndex] != "")
		GameBase::setPosition(%clientId, %pos[%closestIndex]);
	else
		GameBase::setPosition(%clientId, %origpos);

	return %pos[%closestIndex];
}

function WalkSlowInvisLoop(%clientId, %delay, %grace)
{
	dbecho($dbechoMode, "WalkSlowInvisLoop(" @ %clientId @ ", " @ %delay @ ", " @ %grace @ ")");

	%pos = GameBase::getPosition(%clientId);
	if(fetchData(%clientId, "lastPos") == "")
		storeData(%clientId, "lastPos", %pos);

	if(Vector::getDistance(%pos, fetchData(%clientId, "lastPos")) <= %grace && fetchData(%clientId, "invisible"))
	{
		storeData(%clientId, "lastPos", GameBase::getPosition(%clientId));
		schedule("WalkSlowInvisLoop(" @ %clientId @ ", " @ %delay @ ", " @ %grace @ ");", %delay, %clientId);
	}
	else
	{
		if(fetchData(%clientId, "invisible"))
			UnHide(%clientId);

		Client::sendMessage(%clientId, $MsgRed, "You are no longer Hiding In Shadows.");

	}
}
function UnHide(%clientId)
{
	dbecho($dbechoMode, "UnHide(" @ %clientId @ ")");

	if(fetchData(%clientId, "invisible"))
	{
		GameBase::startFadeIn(%clientId);
		storeData(%clientId, "invisible", "");
	}

	storeData(%clientId, "lastPos", "");
	storeData(%clientId, "blockHide", True);
	schedule("storeData(" @ %clientId @ ", \"blockHide\", \"\");", 10);
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

function AddToTargetList(%clientId, %cl)
{
	dbecho($dbechoMode, "AddToTargetList(" @ %clientId @ ", " @ %cl @ ")");

	%name = Client::getName(%cl);
	if(!IsInCommaList(fetchData(%clientId, "targetlist"), %name))
	{
		storeData(%clientId, "targetlist", AddToCommaList(fetchData(%clientId, "targetlist"), %name));

		Client::sendMessage(%cl, $MsgRed, Client::getName(%clientId) @ " wants you dead!  Travel carefully!");
		Client::sendMessage(%clientId, $MsgRed, %name @ " has been notified of your intentions.");

		schedule("RemoveFromTargetList(" @ %clientId @ ", " @ %cl @ ");", 10 * 60);
	}
}
function RemoveFromTargetList(%clientId, %cl)
{
	dbecho($dbechoMode, "RemoveFromTargetList(" @ %clientId @ ", " @ %cl @ ")");

	%name = Client::getName(%cl);
	if(IsInCommaList(fetchData(%clientId, "targetlist"), %name))
	{
		storeData(%clientId, "targetlist", RemoveFromCommaList(fetchData(%clientId, "targetlist"), %name));

		Client::sendMessage(%cl, $MsgBeige, Client::getName(%clientId) @ " was forced to declare a truce.");
		Client::sendMessage(%clientId, $MsgBeige, %name @ " has expired on your target-list.");
	}
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
		%loc = " - Location: " @ $LocationDesc[%t];
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
		%msg = %msg @ "\nPrice: $" @ %c;
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

	%d = round(%c * 10);
	%m = (%d / 10) * 1.000001;

	return %m;
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

function AddBounty(%clientId, %amt)
{
	dbecho($dbechoMode, "AddBounty(" @ %clientId @ ", " @ %amt @ ")");

	%b = fetchData(%clientId, "bounty") + %amt;
	storeData(%clientId, "bounty", Cap(%b, 0, 100000000));

	return fetchData(%clientId, "bounty");
}

// TEMPORARILY DISABLED - PostSteal function
//function PostSteal(%clientId, %success, %type, %targetId)
//{
//	dbecho($dbechoMode, "PostSteal(" @ %clientId @ ", " @ %success @ ", " @ %type @ ")");
//
//	if(%type == 0)
//	{
//		//regular steal
//		if(%success)
//			AddBounty(%clientId, (5.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//		else
//			AddBounty(%clientId, (5.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//	}
//	else if(%type == 1)
//	{
//		//pickpocket
//		if(%success)
//			AddBounty(%clientId, (10 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//		else
//			AddBounty(%clientId, (10.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//	}
//	else if(%type == 2)
//	{
//		//mug
//		if(%success)
//			AddBounty(%clientId, (15.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//		else
//			AddBounty(%clientId, (15.5 * fetchData(%targetId, "LVL")) * (fetchData(%targetId, "RemortStep") + 1));
//	}
//
//	if(%success)
//		UpdateBonusState(%clientId, "Theft 1", 20 / 2);
//	else
//		UpdateBonusState(%clientId, "Theft 1", 120 / 2);
//}

function GetTypicalTossStrength(%clientId)
{
	dbecho($dbechoMode, "GetTypicalTossStrength(" @ %clientId @ ")");

	if(fetchData(%clientId, "RACE") == "DeathKnight")
	{
		%toss = 10;
	}
	else
	{
		%a = Player::getArmor(%clientId);
		%b = String::getSubStr(%a, String::len(%a)-1, 1);
		%toss = Cap($speed[fetchData(%clientId, "RACE"), %b]-2, 3, 10);
	}

	return %toss;
}

// TEMPORARILY DISABLED - AllowedToSteal function
//function AllowedToSteal(%clientId)
//{
//	dbecho($dbechoMode, "AllowedToSteal(" @ %clientId @ ")");
//
//	if(fetchData(%clientId, "InSleepZone") != "")
//		return "You can't steal inside a sleeping area.";
//	//else if(Zone::getType(fetchData(%clientId, "zone")) == "PROTECTED")
//	//	return "You can't steal from someone in protected territory.";
//
//	return "True";
//}

// TEMPORARILY DISABLED - PerhapsPlayStealSound function
//function PerhapsPlayStealSound(%clientId, %type)
//{
//	dbecho($dbechoMode, "PerhapsPlayStealSound(" @ %clientId @ ", " @ %type @ ")");
//
//	if(%type == 0)
//		%snd = SoundMoney1;
//	else if(%type == 1)
//		%snd = SoundPickupItem;
//	else if(%type == 2)
//		%snd = SoundPickupItem;
//
//	%r = getRandom() * 1000;
//	%n = 1000 - $PlayerSkill[%clientId, $SkillStealing];
//	if(%r <= %n)
//	{
//		playSound(%snd, GameBase::getPosition(%clientId));
//		return True;
//	}
//	else
//		return False;
//}

function GetLCKcost(%clientId)
{
	dbecho($dbechoMode, "GetLCKcost(" @ %clientId @ ")");

	%a = floor( pow(2, Cap(fetchData(%clientId, "LCK"), 0, 26)) * 15 ) + 100;

	return Cap(%a, 0, "inf");
}
function Getrpcost(%clientId)
{
	dbecho($dbechoMode, "Getrpcost(" @ %clientId @ ")");

	%a = floor( pow(2, Cap(fetchData(%clientId, "RankPoints"), 0, 26)) * 7 ) + 100;

	return Cap(%a, 0, "inf");
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

function UnequipMountedStuff(%clientId)
{
	dbecho($dbechoMode, "UnequipMountedStuff(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before calling Player::getItemCount
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return; // Player object doesn't exist

	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		// Re-validate player object in case it gets despawned during the loop
		%playerObj = Client::getOwnedObject(%clientId);
		if(%playerObj == "" || %playerObj == -1)
			break; // Exit loop if player object no longer exists
		
		%a = getItemData(%i);
		%itemcount = Player::getItemCount(%clientId, %a);

		if(%itemcount)
		{
			if(%a.className == "Equipped")
			{
				%b = String::getSubStr(%a, 0, String::len(%a)-1);
				// Remove ALL equipped items, not just one
				Player::decItemCount(%clientId, %a, %itemcount);
				Player::incItemCount(%clientId, %b, %itemcount);
			}
			else if(Player::getMountedItem(%clientId, $WeaponSlot) == %a)
			{
				Player::unMountItem(%clientId, $WeaponSlot);
			}
		}
	}
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
	%baseKronos = $BaseControl[HouseKronos];
	%baseArbal = $BaseControl[HouseArbal];
	%baseCurama = $BaseControl[HouseCurama];
	%baseYuliple = $BaseControl[HouseYuliple];
	
	%flagKronos = $FlagCommand[HouseKronos];
	%flagArbal = $FlagCommand[HouseArbal];
	%flagCurama = $FlagCommand[HouseCurama];
	%flagYuliple = $FlagCommand[HouseYuliple];
	
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
	if(%baseKronos != "")
		$BaseControl[HouseKronos] = %baseKronos;
	if(%baseArbal != "")
		$BaseControl[HouseArbal] = %baseArbal;
	if(%baseCurama != "")
		$BaseControl[HouseCurama] = %baseCurama;
	if(%baseYuliple != "")
		$BaseControl[HouseYuliple] = %baseYuliple;
	
	if(%flagKronos != "")
		$FlagCommand[HouseKronos] = %flagKronos;
	if(%flagArbal != "")
		$FlagCommand[HouseArbal] = %flagArbal;
	if(%flagCurama != "")
		$FlagCommand[HouseCurama] = %flagCurama;
	if(%flagYuliple != "")
		$FlagCommand[HouseYuliple] = %flagYuliple;
	
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


function ActivateAllGenerators()
{
	dbecho($dbechoMode, "ActivateAllGenerators()");
	
	// Find all generators in MissionGroup and activate them
	%missionGroup = nameToID("MissionGroup");
	if(%missionGroup == -1)
		return;
	
	%generatorCount = 0;
	for(%i = 0; %i < Group::objectCount(%missionGroup); %i++)
	{
		%obj = Group::getObject(%missionGroup, %i);
		%dataName = GameBase::getDataName(%obj);
		
		if(%dataName == "Generator")
		{
			// Set generator to team 1 to match turrets (required for power connection)
			GameBase::setTeam(%obj, 1);
			
			// Activate the generator
			Generator::onActivate(%obj);
			
			%generatorCount++;
		}
	}
	
	if(%generatorCount > 0)
		echo("Activated " @ %generatorCount @ " generator(s).");
}

// ============================================================================
// Overlevel AFK zone enforcement
// ============================================================================

// Config (seconds/levels)
$AFKZoneCheckInterval = 30;
$AFKOverLevelBuffer = 5;
$AFKOverLevelInactivity = 180;
$AFKOverLevelWarnWindow = 30;
$AFKOverLevelMinZoneTime = 60;
$AFKOverLevelTeleportPos = "-905.1 -2201.61 522.14"; // Fallback position if marker group fails
$AFKTeleportMarkerGroup = "AFKTeleportPoints"; // Marker group in mission file for AFK teleport locations

// Hard caps by zone description (case-insensitive match)
$AFKZoneCap["Pig Den"] = 30;
$AFKZoneCap["Ogre Skybase"] = 50;
$AFKZoneCap["Ogre Stronghold"] = 50;
$AFKZoneCap["Ghost Town"] = 100;
$AFKZoneCap["Minotaur Tomb"] = 150;
$AFKZoneCap["Stone Henge"] = 250;
$AFKZoneCap["Demon Incubus"] = 400;

function AFKZone_GetDescFromFolder(%folderId)
{
	for(%z = 1; %z <= $numZones; %z++)
	{
		if($Zone::FolderID[%z] == %folderId)
			return $Zone::Desc[%z];
	}
	return "";
}

function AFKZone_GetCap(%desc)
{
	if(%desc == "" || %desc == -1)
		return "";

	// Direct lookup (Priority)
	if($AFKZoneCap[%desc] != "")
		return $AFKZoneCap[%desc];

	return "";
}

function AFKZone_ClearWarning(%id)
{
	$AFKZoneWarnUntil[%id] = "";
	$AFKZoneWarnPos[%id] = "";
	$AFKZoneWarnAck[%id] = "";
	$AFKZoneCode[%id] = ""; // Clear verification code
}

function AFKZone_Teleport(%id)
{
	%obj = Client::getOwnedObject(%id);
	
	// CRITICAL FIX: Clear zone data BEFORE teleporting so zone shows as "Unknown"
	// and bot spawning logic properly sees the zone as having one less player
	%oldZoneFolder = fetchData(%id, "zone");
	if(%oldZoneFolder != "" && %oldZoneFolder != -1)
	{
		%oldZoneIndex = Zone::getIndex(%oldZoneFolder);
		if(%oldZoneIndex > 0)
		{
			// Decrement zone player count so bot spawning logic sees the change
			%count = $ZonePlayerCount[%oldZoneIndex];
			if(%count > 0)
			{
				$ZonePlayerCount[%oldZoneIndex] = %count - 1;
				echo("[AFKZONE] Decremented player count for zone " @ %oldZoneIndex @ " (was: " @ %count @ ", now: " @ ($ZonePlayerCount[%oldZoneIndex]) @ ")");
				
				// If zone is now empty, cancel pending spawns and schedule despawn
				if($ZonePlayerCount[%oldZoneIndex] <= 0)
				{
					CancelPendingZoneSpawn(%oldZoneIndex);
					schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
				}
			}
		}
	}
	
	// Clear stored zone data - player is being moved to "nowhere"
	storeData(%id, "zone", "");
	storeData(%id, "tmpzone", "");
	storeData(%id, "lastzone", "");
	
	if(%obj != -1 && %obj != "" && isObject(%obj))
	{
		// Try to teleport to a random marker from the AFK teleport group
		// Retry up to 5 times to find an unoccupied spot
		%teleportSuccess = False;
		for(%attempt = 0; %attempt < 5; %attempt++)
		{
			%result = TeleportToMarker(%id, $AFKTeleportMarkerGroup, true, true);
			if(%result != False && %result != "")
			{
				%teleportSuccess = true;
				$AFKZoneLastPos[%id] = %result;
				break;
			}
		}
		
		// Fallback to hardcoded position if marker group doesn't exist or all spots occupied
		if(!%teleportSuccess)
		{
			GameBase::setPosition(%obj, $AFKOverLevelTeleportPos);
			$AFKZoneLastPos[%id] = $AFKOverLevelTeleportPos;
		}
	}
	Client::sendMessage(%id, $MsgRed, "You have been moved out of this low-level zone.");
	AFKZone_ClearWarning(%id);
	$AFKZoneLastMove[%id] = getSimTime();
	
	// CRITICAL: Reset zone folder tracking to force AFK zone system to detect zone change
	// This ensures the next AFKZone_Tick() will recognize the new zone
	$AFKZoneLastFolder[%id] = "";
	$AFKZoneEnterTime[%id] = "";
	
	// Refresh player state (like FellOffMap does) to update zone and other player data
	RefreshAll(%id);
	
	// Force immediate zone check so player's zone is updated right away
	// This ensures zone tracking works immediately after teleport
	schedule("DoZoneCheck(2, 0);", 0.1);
}

function AFKZone_Tick()
{
	%now = getSimTime();
	%list = GetPlayerIdList();
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		if(%id == "" || %id == -1)
			continue;
		if(isRPGAI(%id) || Player::isAiControlled(%id))
			continue;
		
		// Admin Override: Admins > 5 are immune to AFK checks
		if(%id.adminLevel > 5)
			continue;
		
		// Player Exclusion: Specific players can be excluded from AFK checks
		%playerName = Client::getName(%id);
		if(%playerName != "" && %playerName != -1)
		{
			if(String::ICompare(%playerName, "Jobo") == 0)
				continue;
		}
		
		%obj = Client::getOwnedObject(%id);
		if(%obj == -1 || %obj == "" || !isObject(%obj))
		{
			AFKZone_ClearWarning(%id);
			continue;
		}
		
		%zoneFolder = ObjectInWhichZone(%obj);
		if(%zoneFolder == "" || %zoneFolder == -1)
		{
			AFKZone_ClearWarning(%id);
			continue;
		}
		
		%zoneDesc = AFKZone_GetDescFromFolder(%zoneFolder);
		// Strip "DUNGEON " or "PROTECTED " prefix if present to match config
		%zoneDesc = String::replace(%zoneDesc, "DUNGEON ", "");
		%zoneDesc = String::replace(%zoneDesc, "PROTECTED ", "");
		
		%cap = AFKZone_GetCap(%zoneDesc);
		if(%cap == "")
		{
			AFKZone_ClearWarning(%id);
			$AFKZoneLastFolder[%id] = %zoneFolder;
			$AFKZoneEnterTime[%id] = %now;
			// Debug: zone found but no cap
			//echo("[AFKZONE] No cap for zone '" @ %zoneDesc @ "' (folder " @ %zoneFolder @ ")");
			continue;
		}
		
		// Zone change resets timers/warnings
		if($AFKZoneLastFolder[%id] != %zoneFolder)
		{
			$AFKZoneLastFolder[%id] = %zoneFolder;
			$AFKZoneEnterTime[%id] = %now;
			AFKZone_ClearWarning(%id);
		}
		
		// Track movement
		%pos = GameBase::getPosition(%obj);
		%lastPos = $AFKZoneLastPos[%id];
		if(%lastPos == "" || Vector::getDistance(%pos, %lastPos) > 1)
		{
			$AFKZoneLastPos[%id] = %pos;
			$AFKZoneLastMove[%id] = %now;
			// Movement cancels any pending warning
			if($AFKZoneWarnUntil[%id] != "")
				AFKZone_ClearWarning(%id);
		}
		
		%lastMove = $AFKZoneLastMove[%id];
		if(%lastMove == "" || %lastMove == -1)
			%lastMove = %now;
		
		%inactivityMs = %now - %lastMove;
		%zoneTimeMs = %now - $AFKZoneEnterTime[%id];
		
		%lvl = fetchData(%id, "LVL");
		if(%lvl == "" || %lvl == -1)
			%lvl = 0;
		
		%name = Client::getName(%id);
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("AFKDebug: Player " @ %name @ " is in " @ %zoneDesc @ " (Cap: " @ %cap @ ")");
		
		// Overlevel check
		if(%lvl <= (%cap + $AFKOverLevelBuffer))
		{
			AFKZone_ClearWarning(%id);
			continue;
		}
		
		// Debug overlevel status before warning triggers
		if($AI_DEBUG_ENABLED || $AI_PERIODIC_DEBUG) echo("[AFKZONE DEBUG] id=" @ %id @ " zone='" @ %zoneDesc @ "' cap=" @ %cap @ " lvl=" @ %lvl @ " inactivity=" @ %inactivityMs @ "ms zonetime=" @ %zoneTimeMs @ "ms warnUntil=" @ $AFKZoneWarnUntil[%id] @ " lastMove=" @ %lastMove @ " lastPos=" @ $AFKZoneLastPos[%id]);
		
		%warnUntil = $AFKZoneWarnUntil[%id];
		if(%warnUntil != "")
		{
			%warnAck = $AFKZoneWarnAck[%id];
			if(%warnAck != "")
			{
				// Honor a valid #verify response: clear this warning cycle and keep the player in-zone.
				AFKZone_ClearWarning(%id);
				$AFKZoneLastPos[%id] = %pos;
				$AFKZoneLastMove[%id] = %now;
				continue;
			}

			%warnPos = $AFKZoneWarnPos[%id];
			%movedSinceWarn = false;
			if(%warnPos != "" && Vector::getDistance(%pos, %warnPos) > 1)
				%movedSinceWarn = true;
			
			if(%movedSinceWarn)
			{
				AFKZone_ClearWarning(%id);
				continue;
			}
			
			if(%now >= %warnUntil)
			{
				// No movement and no valid #verify acknowledgment before expiry: teleport out.
				AFKZone_Teleport(%id);
				continue;
			}
			
			// Warning still active, wait
			continue;
		}
		
		// Issue warning if overlevel, inactive, and in zone long enough
		// Issue warning if overlevel, inactive, and in zone long enough
		if(%zoneTimeMs >= $AFKOverLevelMinZoneTime && %inactivityMs >= $AFKOverLevelInactivity)
		{
			$AFKZoneWarnPos[%id] = %pos;
			$AFKZoneWarnUntil[%id] = %now + $AFKOverLevelWarnWindow;
			$AFKZoneWarnAck[%id] = "";
			
			// Generate random 4-digit verification code (1000-9999)
			%code = floor(getRandom() * 8999) + 1000;
			$AFKZoneCode[%id] = %code;
			
			Client::sendMessage(%id, $MsgRed, "WARNING: You are over level for " @ %zoneDesc @ ". Type #verify " @ %code @ " to stay.");
			echo("[AFKZONE] Warn -> id=" @ %id @ " zone='" @ %zoneDesc @ "' cap=" @ %cap @ " lvl=" @ %lvl @ " inactivity=" @ %inactivityMs @ "ms zonetime=" @ %zoneTimeMs @ "ms code=" @ %code);
		}
	}
	
	// reschedule
	schedule("AFKZone_Tick();", $AFKZoneCheckInterval);
}

function StartAFKZoneEnforcement()
{
	if($AFKZoneEnforceStarted)
		return;
	$AFKZoneEnforceStarted = true;
	AFKZone_Tick();
}

// ============================================================
// OBJECT SAFETY ARCHITECTURE
// ============================================================

// Global Safety Wrapper for object deletion
// Prevents accidental deletion of Players/Bots when targeting generic IDs
function SafeDeleteObject(%obj)
{
	// 1. Basic Validation
	if(%obj == "" || %obj == -1) 
		return;
	
	if(!isObject(%obj)) 
		return;

	// 2. Identify Object Type
	%type = getObjectType(%obj);
	
	// 3. CRITICAL SAFEGUARDS
	
	// PROTECT PLAYERS / BOTS
	if(%type == "Player")
	{
		%client = Player::getClient(%obj);
		%name = Client::getName(%client);
		echo("CRITICAL SAFEGUARD: Attempted to delete Player object " @ %obj @ " (" @ %name @ ") via generic SafeDeleteObject! Stack Trace:");
		trace(1); trace(0); // Dump stack to console to catch the culprit
		return; // ABORT DELETION
	}
	
	// 4. Safe to Delete
	deleteObject(%obj);
}

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

function TownBot_PlayFarewell(%playerClientId, %botClientId)
{
	if(%playerClientId == "" || %botClientId == "")
		return;

	// Check if a session voice was already assigned during greeting
	%voice = %botClientId.sessionVoice;
	
	// ROBUSTNESS CHECK: Verify voice is valid (must contain "male" or "female")
	// This handles cases where sessionVoice might be empty, "0", or undefined
	if(String::findSubStr(%voice, "male") == -1 && String::findSubStr(%voice, "female") == -1)
	{
		// Fallback: Randomize voice if no valid session voice exists
		%botArmor = Player::getArmor(%botClientId);
		%rand = floor(getRandom() * 5) + 1; // Random 1-5
		
		%voice = "male" @ %rand; // Default to male 1-5
		if(String::findSubStr(%botArmor, "female") != -1)
			%voice = "female" @ %rand; // Female 1-5
	}
	
	// Randomly choose between "No Problem" (wnoprob) and "Bye" (wbye)
	%suffix = "wbye";
	if(floor(getRandom() * 2) == 1) // 50% chance
		%suffix = "wnoprob";
	
	Client::sendMessage(%playerClientId, 0, "~w" @ %voice @ "." @ %suffix @ ".wav");
}
