//============================================================================
// rpgfunk_persist.cs — split from rpgfunk.cs (scatter split, Phase 2)
// Extracted 2026-07-18 at commit 5118b1b. Mechanical text move, no behavior change.
// SaveCharacter/LoadCharacter + bank-storage chunking + server time + Clear*
// Source line ranges listed in the rpgfunk.cs shell tombstone.
// exec'd by rpgfunk.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====

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
		// VOID 2026-07-14: never save VSlot placeholder rows - their counts are
		// per-client DISPLAY state (VirtualSlots.cs Sync), not carried items.
		// Persisting them fed placeholder names back through Item::giveItem on
		// login (found live: "VSlot0 1 VSlotArmor0 101" in a save -> crashes).
		if($VSlot::IsSlot[%i])
			continue;
		// VOID AUDIT 2026-07-15: engine counts for BELT-registered items are
		// never legitimate (the belt lists are authoritative; every give path
		// routes them there). Twice now a stray path minted engine copies and
		// bloated spawnStuff past the ~1024 exec parse limit, corrupting the
		// character on load. Do not persist them - and shout, so the culprit
		// path names itself.
		if(isBeltItem(%checkItem))
		{
			%bcount = SafeGetItemCount(%clientId, %checkItem, "updateSpawnStuff-beltaudit");
			if(%bcount > 0)
				echo("[VOID AUDIT] " @ Client::getName(%clientId) @ " has ENGINE count " @ %bcount @ " of belt item '" @ %checkItem @ "' at save time - NOT persisted (find what minted it!)");
			continue;
		}
		
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
		// SLOT PURGE 2026-07-13: probe repointed Tool->Backpack (Tool ItemData removed).
		// Any registered item works: the inc/dec runs back-to-back in single-threaded script,
		// nets to zero at any starting count, and only the x!=y delta is checked.
		Player::incItemCount(%clientId, Backpack);
		%x = Player::getItemCount(%clientId, Backpack);
		Player::decItemCount(%clientId, Backpack);
		%y = Player::getItemCount(%clientId, Backpack);
		
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
	// BANK-FMT 1: field 6 is the sub-million remainder; 64 stores whole
	// million-coin chunks, 65 is the format, and 66 retains the original
	// legacy text for rollback. Never reconstruct the full balance numerically.
	$funk::var["[\"" @ %name @ "\", 0, 6]"] = Bank::RawRemainder(%clientId);
	$funk::var["[\"" @ %name @ "\", 0, 64]"] = Bank::RawChunks(%clientId);
	$funk::var["[\"" @ %name @ "\", 0, 65]"] = $Bank::SaveFormat;
	$funk::var["[\"" @ %name @ "\", 0, 66]"] = GetDataFromArray(%clientId, "BANK_LEGACY_BACKUP");
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
	// review 2026-07-17: tripwire - this carried list is exported as ONE line, NOT
	// chunked like BankStorage. Bounded today by the ~34 registered accessories
	// (~900 chars worst case), but a save line >~1024 chars corrupts the character.
	// If accessories are ever added past this, chunk field 49 (see SplitAndSaveBankStorage).
	if(String::len(%accessories) > 950)
		echo("[SAVE WARN] " @ %name @ " Accessories (field 49) is " @ String::len(%accessories) @ " chars - approaching the ~1024 save-line corruption limit; field 49 needs chunking.");
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
	
	// Save AutoSkill mute state (field 57)
	%autoSkillMute = fetchData(%clientId, "AutoSkill_Mute");
	if(%autoSkillMute == "" || %autoSkillMute == "0" || %autoSkillMute == -1)
		%autoSkillMute = "";
	$funk::var["[\"" @ %name @ "\", 0, 57]"] = %autoSkillMute;

	// Save belt Weapons list (field 58) - BeltWeapons.cs datablock-less weapons
	%beltWeapons = fetchData(%clientId, "Weapons");
	if(%beltWeapons == "" || %beltWeapons == " " || %beltWeapons == "0" || %beltWeapons == -1)
		%beltWeapons = "";
	$funk::var["[\"" @ %name @ "\", 0, 58]"] = %beltWeapons;
	// StoredWeapons (bank storage) is saved as field 59 BELOW, after the
	// BeltStorage->categories sync recomputes it alongside fields 38-46.


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
		%storedWeaponsCheck = fetchData(%clientId, "StoredWeapons");

		// Normalize "0" to empty string for checks
		if(%storedQuestCheck == "0" || %storedQuestCheck == " ") %storedQuestCheck = "";
		if(%storedKeyCheck == "0" || %storedKeyCheck == " ") %storedKeyCheck = "";
		if(%storedConsumablesCheck == "0" || %storedConsumablesCheck == " ") %storedConsumablesCheck = "";
		if(%storedArmorCheck == "0" || %storedArmorCheck == " ") %storedArmorCheck = "";
		if(%storedAccessoriesCheck == "0" || %storedAccessoriesCheck == " ") %storedAccessoriesCheck = "";
		if(%storedOtherCheck == "0" || %storedOtherCheck == " ") %storedOtherCheck = "";
		if(%storedWeaponsCheck == "0" || %storedWeaponsCheck == " ") %storedWeaponsCheck = "";
		
		// If any stored category has data, rebuild BeltStorage from them
		// BUT: Only do this if we're NOT using the banker system (BeltStorage as single source of truth)
		// If BeltStorage is empty and we're using banker system, it means items were intentionally withdrawn
		// In that case, stored categories should also be empty (cleared during withdrawal)
		// If stored categories still have data, it means they're stale and should be cleared, not used to rebuild BeltStorage
		if(%storedQuestCheck != "" || %storedKeyCheck != "" || %storedConsumablesCheck != "" ||
		   %storedArmorCheck != "" || %storedAccessoriesCheck != "" || %storedOtherCheck != "" ||
		   %storedWeaponsCheck != "")
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
			storeData(%clientId, "StoredWeapons", "");
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
		%storedWeapons = "";
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
				else if(%category == "Weapons")
				{
					%storedWeapons = Belt::AddToList(%storedWeapons, %item @ " " @ %count);
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
	if(%storedWeapons != "")
	{
		if(%cleanedBeltStorage != "")
			%cleanedBeltStorage = %cleanedBeltStorage @ " " @ %storedWeapons;
		else
			%cleanedBeltStorage = %storedWeapons;
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
	storeData(%clientId, "StoredWeapons", %storedWeapons);
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
	%storedWeaponsSave = %storedWeapons;
	
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
	%len = String::len(%storedWeaponsSave);
	while(%len > 0 && String::getSubStr(%storedWeaponsSave, %len-1, 1) == " ")
	{
		%storedWeaponsSave = String::getSubStr(%storedWeaponsSave, 0, %len-1);
		%len = String::len(%storedWeaponsSave);
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
	if(%storedWeaponsSave == "" || %storedWeaponsSave == " " || %storedWeaponsSave == "0")
		%storedWeaponsSave = "0";
	
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
	$funk::var["[\"" @ %name @ "\", 0, 59]"] = %storedWeaponsSave;	// bank-stored belt weapons
	$funk::var["[\"" @ %name @ "\", 0, 44]"] = fetchData(%clientId, "Stance");
	// review 2026-07-17: persist daily-quest state (slot 33) - it lived only in
	// $ClientData, which LoadCharacter wipes, so relog re-opened the once-per-day
	// gate (a repeatable turn-in farm) and dropped in-progress dailies. Slot 33 is
	// virgin and inside the 32-63 offline-award passthrough (no extra copy needed).
	$funk::var["[\"" @ %name @ "\", 0, 33]"] = Daily::PackState(%clientId);
	
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
			// VOID 2026-07-14: never save VSlot placeholder rows (display-only counts;
			// see updateSpawnStuff note).
			if($VSlot::IsSlot[%i])
				continue;
			// VOID AUDIT 2026-07-15: never persist engine counts for belt items
			// (see updateSpawnStuff note - belt lists are authoritative).
			if(isBeltItem(%checkItem))
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
	

	%savePath = "temp\\" @ %name @ ".cs";
	if(GetDataFromArray(%clientId, "BANK_NEEDS_FILE_BACKUP"))
	{
		%stagingPath = "temp\\" @ %name @ ".bankfmt1.new";
		%backupPath = "temp\\" @ %name @ ".bankfmt0.bak";
		File::delete(%stagingPath);
		if(isFile(%stagingPath))
		{
			echo("ERROR: Bank migration staging file could not be cleared for " @ %name @ ". Original save retained.");
			ClearFunkVar(%name);
			return False;
		}

		export("funk::var[\"" @ %name @ "\",*", %stagingPath, false);
		if(!isFile(%stagingPath))
		{
			echo("ERROR: Bank migration staging export failed for " @ %name @ ". Original save retained.");
			ClearFunkVar(%name);
			return False;
		}

		if(!isFile(%backupPath) && !File::copy(%savePath, %backupPath))
		{
			echo("ERROR: Bank migration backup failed for " @ %name @ ". Original save retained.");
			File::delete(%stagingPath);
			ClearFunkVar(%name);
			return False;
		}

		if(!File::copy(%stagingPath, %savePath))
		{
			echo("ERROR: Bank migration replacement failed for " @ %name @ ". Backup retained at " @ %backupPath @ ".");
			File::delete(%stagingPath);
			ClearFunkVar(%name);
			return False;
		}

		File::delete(%stagingPath);
		SetDataInArray(%clientId, "BANK_NEEDS_FILE_BACKUP", "", GetClientDataType(%clientId));
		echo("[BANK-MIGRATE] Save replaced for " @ %name @ "; original retained at " @ %backupPath @ ".");
	}
	else
	{
		File::delete(%savePath);
		export("funk::var[\"" @ %name @ "\",*", %savePath, false);
	}
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
		storeData(%clientId, "EXP", SafeFloor($funk::var[%name, 0, 2]));    // clean fractional EXP; SafeFloor: plain floor() int32-wraps >=2^31 negative (ECON-FIX 2026-07-18)
		storeData(%clientId, "campPos", $funk::var[%name, 0, 3]);
		storeData(%clientId, "COINS", SafeFloor($funk::var[%name, 0, 4]));  // clean fractional coins; SafeFloor guards the int32 wrap
		storeData(%clientId, "isMimic", $funk::var[%name, 0, 5]);
		// Migrate legacy field-6 balances before storeData can apply the int32
		// guard. New saves load the already-split remainder/chunks directly.
		Bank::Load(%clientId, $funk::var[%name, 0, 6],
			$funk::var[%name, 0, 64], $funk::var[%name, 0, 65],
			$funk::var[%name, 0, 66]);
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
				if($LOADCHAR_DEBUG) echo("DEBUG LoadCharacter: Cleaned grouplist - removed leading '0' prefix. Original: '" @ $funk::var[%name, 0, 8] @ "', Cleaned: '" @ %grouplist @ "'");
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
		
		// CRITICAL: Double-check after storing - if a corrupt house value got through, clear it.
		// HOUSE-ZERO FIX 2026-08-22. This self-heal never healed anything; three defects:
		//   1. Order was inverted - it cleared MyHouse and THEN called
		//      BootFromCurrentHouse, which re-reads MyHouse, found it already blank, and
		//      took its `else return -1` path doing none of its work.
		//   2. Even with the order corrected the Boot call is a guaranteed no-op here:
		//      BootFromCurrentHouse itself normalizes "0"/"House0"/"house0" to "" and then
		//      tests `if(%h != "")`. It treats a corrupt value as "no house" BY DESIGN, so
		//      there is no house to boot from. The old comment's promise ("will also reset
		//      rank points and unequip items") was never reachable through that call - and
		//      RankPoints is reloaded from the save file on the very next line anyway,
		//      which would have overwritten any reset.
		//   3. The clear did not clear: storeData(..., "") stored the string "0" (see
		//      storeData in rpgstats.cs), so this block detected "0", rewrote "0", and hit
		//      the identical state on every single login.
		// Detection is now POSITIVE (anything GetHouseNumber rejects), so "None", -1 and
		// any future spelling are caught, not just the three blacklisted strings.
		// DELIBERATE: RankPoints are NOT reset. Joe's call 2026-08-22 - players keep the
		// rank points they accrued while the bug was live. Do not "fix" this back.
		%loadedHouse = fetchData(%clientId, "MyHouse");
		if(%loadedHouse != "" && GetHouseNumber(%loadedHouse) == "")
		{
			echo("[HOUSE-FIX] LoadCharacter: " @ %name @ " (" @ %clientId @ ") had invalid house '" @ %loadedHouse @ "' - cleared (rank points kept).");
			storeData(%clientId, "MyHouse", "");
		}
		
		storeData(%clientId, "RankPoints", $funk::var[%name, 0, 31]);
		//echo("DEBUG: RankPoints = '" @ $funk::var[%name, 0, 31] @ "'");
		storeData(%clientId, "TournyRank", $funk::var[%name, 0, 32]);
		//echo("DEBUG: TournyRank = '" @ $funk::var[%name, 0, 32] @ "'");
		
		if($LOADCHAR_DEBUG) echo("DEBUG: Loading belt items (QuestItems/KeyItems)...");
		// Normalize QuestItems and KeyItems - convert "0" to empty string
		%questItems = $funk::var[%name, 0, 35];
		if($LOADCHAR_DEBUG) echo("DEBUG: QuestItems RAW = '" @ %questItems @ "'");
		
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
			if($LOADCHAR_DEBUG) echo("DEBUG: QuestItems was empty/'0', normalizing to empty string");
			%questItems = "";
		}
		storeData(%clientId, "QuestItems", %questItems);
		if($LOADCHAR_DEBUG) echo("DEBUG: QuestItems FINAL = '" @ %questItems @ "'");
		
		%keyItems = $funk::var[%name, 0, 36];
		if($LOADCHAR_DEBUG) echo("DEBUG: KeyItems RAW = '" @ %keyItems @ "'");
		if(%keyItems == "" || %keyItems == " " || %keyItems == "0")
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: KeyItems was empty/'0', normalizing to empty string");
			%keyItems = "";
		}
		storeData(%clientId, "KeyItems", %keyItems);
		if($LOADCHAR_DEBUG) echo("DEBUG: KeyItems FINAL = '" @ %keyItems @ "'");
		
		// Load Consumables from field 37 (new field for potions)
		%consumables = $funk::var[%name, 0, 37];
		if($LOADCHAR_DEBUG) echo("DEBUG: Consumables RAW (field 37) = '" @ %consumables @ "'");
		if(%consumables == "" || %consumables == " " || %consumables == "0" || %consumables == -1)
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: Consumables was empty/'0', normalizing to empty string");
			%consumables = "";
		}
		storeData(%clientId, "Consumables", %consumables);
		if($LOADCHAR_DEBUG) echo("DEBUG: Consumables FINAL = '" @ %consumables @ "'");
		
		// Load Armor from field 48 (equipped category)
		%armor = $funk::var[%name, 0, 48];
		if($LOADCHAR_DEBUG) echo("DEBUG: Armor RAW (field 48) = '" @ %armor @ "'");
		if(%armor == "" || %armor == " " || %armor == "0" || %armor == -1)
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: Armor was empty/'0', normalizing to empty string");
			%armor = "";
		}
		// VOID MIGRATION 2026-07-14: field 48 historically held the WORN engine armor
		// NAME (a single word, written by the old UpdateAppearance); post-conversion it
		// holds the belt carried-armor LIST ("name count " pairs). A legacy single-word
		// value would corrupt belt list parsing - clear it here; the worn state
		// re-migrates from the spawnStuff "Xxx0" token (GiveThisStuff) on this same
		// login, which re-gives the armor to the belt and re-equips it.
		if(%armor != "" && GetWord(%armor, 1) == -1 && isBeltItem(GetWord(%armor, 0)))
		{
			echo("[VOID MIGRATE] " @ %name @ ": legacy worn-armor value '" @ %armor @ "' in field 48 cleared (re-migrates via spawnStuff).");
			%armor = "";
		}
		storeData(%clientId, "Armor", %armor);
		if($LOADCHAR_DEBUG) echo("DEBUG: Armor FINAL = '" @ %armor @ "'");
		
		// Load Accessories from field 49 (equipped category)
		%accessories = $funk::var[%name, 0, 49];
		if($LOADCHAR_DEBUG) echo("DEBUG: Accessories RAW (field 49) = '" @ %accessories @ "'");
		if(%accessories == "" || %accessories == " " || %accessories == "0" || %accessories == -1)
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: Accessories was empty/'0', normalizing to empty string");
			%accessories = "";
		}
		storeData(%clientId, "Accessories", %accessories);
		if($LOADCHAR_DEBUG) echo("DEBUG: Accessories FINAL = '" @ %accessories @ "'");
		
		// Load Other from field 50 (equipped category)
		%other = $funk::var[%name, 0, 50];
		if($LOADCHAR_DEBUG) echo("DEBUG: Other RAW (field 50) = '" @ %other @ "'");
		if(%other == "" || %other == " " || %other == "0" || %other == -1)
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: Other was empty/'0', normalizing to empty string");
			%other = "";
		}
		storeData(%clientId, "Other", %other);
		if($LOADCHAR_DEBUG) echo("DEBUG: Other FINAL = '" @ %other @ "'");
		
		// Load Equipped Belt Armor from field 51
		%equippedBeltArmor = $funk::var[%name, 0, 51];
		if($LOADCHAR_DEBUG) echo("DEBUG: EquippedBeltArmor RAW (field 51) = '" @ %equippedBeltArmor @ "'");
		if(%equippedBeltArmor == "" || %equippedBeltArmor == " " || %equippedBeltArmor == "0" || %equippedBeltArmor == -1)
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: EquippedBeltArmor was empty/'0', normalizing to empty string");
			%equippedBeltArmor = "";
		}
		storeData(%clientId, "EquippedBeltArmor", %equippedBeltArmor);
		if($LOADCHAR_DEBUG) echo("DEBUG: EquippedBeltArmor FINAL = '" @ %equippedBeltArmor @ "'");
		
		// Load Equipped Belt Accessories from field 52 (space-separated list)
		%equippedBeltAccessories = $funk::var[%name, 0, 52];
		if($LOADCHAR_DEBUG) echo("DEBUG: EquippedBeltAccessories RAW (field 52) = '" @ %equippedBeltAccessories @ "'");
		if(%equippedBeltAccessories == "" || %equippedBeltAccessories == " " || %equippedBeltAccessories == "0" || %equippedBeltAccessories == -1)
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: EquippedBeltAccessories was empty/'0', normalizing to empty string");
			%equippedBeltAccessories = "";
		}
		storeData(%clientId, "EquippedBeltAccessories", %equippedBeltAccessories);
		if($LOADCHAR_DEBUG) echo("DEBUG: EquippedBeltAccessories FINAL = '" @ %equippedBeltAccessories @ "'");
		
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
		
		// Load AutoSkill mute state (field 57)
		%autoSkillMute = $funk::var[%name, 0, 57];
		if(%autoSkillMute == "" || %autoSkillMute == " " || %autoSkillMute == "0" || %autoSkillMute == -1)
			%autoSkillMute = "";
		storeData(%clientId, "AutoSkill_Mute", %autoSkillMute);

		// Load belt Weapons list (field 58) - BeltWeapons.cs datablock-less weapons.
		// Equip state deliberately does NOT persist (players re-equip after login);
		// only the backpack contents do.
		%beltWeapons = $funk::var[%name, 0, 58];
		if(%beltWeapons == "" || %beltWeapons == " " || %beltWeapons == "0" || %beltWeapons == -1)
			%beltWeapons = "";
		storeData(%clientId, "Weapons", %beltWeapons);
		storeData(%clientId, "EquippedBeltWeapon", "");
		storeData(%clientId, "BeltWeaponShellLoaned", "");

		// Load bank-stored belt Weapons (field 59)
		%storedWeapons = $funk::var[%name, 0, 59];
		if(%storedWeapons == "" || %storedWeapons == " " || %storedWeapons == "0" || %storedWeapons == -1)
			%storedWeapons = "";
		storeData(%clientId, "StoredWeapons", %storedWeapons);
		
		// Note: Visual re-mount happens in Game::playerSpawn via schedule

		
		if($LOADCHAR_DEBUG) echo("DEBUG: Loading stored belt items (StoredQuestItems/StoredKeyItems)...");
		// Handle StoredQuestItems and StoredKeyItems - convert space or "0" to empty string if needed
		// Also handle case where variable doesn't exist in old save files (defaults to empty)
		// Validate and clean corrupted data like "0 -1" before storing
		%storedQuest = $funk::var[%name, 0, 38];
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredQuestItems RAW (field 38) = '" @ %storedQuest @ "'");
		if(%storedQuest == "" || %storedQuest == " " || %storedQuest == "0")
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: StoredQuestItems was empty/'0', normalizing to empty string");
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
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredQuestItems FINAL = '" @ %storedQuest @ "'");
		
		// Load StoredKeyItems from field 39
		%storedKey = $funk::var[%name, 0, 39];
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredKeyItems RAW (field 39) = '" @ %storedKey @ "'");
		if(%storedKey == "" || %storedKey == " " || %storedKey == "0")
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: StoredKeyItems was empty/'0', normalizing to empty string");
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
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredKeyItems FINAL = '" @ %storedKey @ "'");
		
		//echo("DEBUG: Loading bank items and inventory...");
		// NOTE: BankGemItems, BankRareItems, BankKeysItems, and BankScrollsItems are no longer used
		// Fields 33-34 are unused legacy bank fields
		// Fields 35-36 are now used for QuestItems and KeyItems (equipped belt items)
		// These old bank variables are not loaded to avoid field conflicts
		storeData(%clientId, "BankGemItems", "");
		storeData(%clientId, "BankRareItems", "");
		storeData(%clientId, "BankKeysItems", "");
		storeData(%clientId, "BankScrollsItems", "");
		// (Duplicate Consumables load removed - field 37 is already loaded earlier
		// in this function with the same normalization)
		// BankUniqueItems is no longer used - keep empty for backward compatibility
		storeData(%clientId, "BankUniqueItems", "");
		// CRITICAL FIX: Position 38 is now used for StoredQuestItems (loaded above)
		// GemItems is unused and has been removed - do not load from position 38
		// Position 39 is now used for StoredKeyItems (loaded above)
		// Position 42 is now used for StoredConsumables (bank storage for consumables)
		%storedConsumables = $funk::var[%name, 0, 42];
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredConsumables RAW (field 42) = '" @ %storedConsumables @ "'");
		if(%storedConsumables == "" || %storedConsumables == " " || %storedConsumables == "0")
		{
			if($LOADCHAR_DEBUG) echo("DEBUG: StoredConsumables was empty/'0', normalizing to empty string");
			%storedConsumables = "";
		}
		storeData(%clientId, "StoredConsumables", %storedConsumables);
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredConsumables FINAL = '" @ %storedConsumables @ "'");
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
		// review 2026-07-17: restore persisted daily-quest state (slot 33). No-op
		// for legacy characters (no D1 tag) so their dailies simply start fresh.
		Daily::UnpackState(%clientId, $funk::var[%name, 0, 33]);
		
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
		Bank::SetLegacy(%clientId, $initbankcoins);
		SetDataInArray(%clientId, "BANK_LEGACY_BACKUP", "", GetClientDataType(%clientId));
		SetDataInArray(%clientId, "BANK_NEEDS_FILE_BACKUP", "", GetClientDataType(%clientId));
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
		if($LOADCHAR_DEBUG) echo("DEBUG: QuestItems = ''");
		storeData(%clientId, "KeyItems", "");
		if($LOADCHAR_DEBUG) echo("DEBUG: KeyItems = ''");
		storeData(%clientId, "Consumables", "");
		if($LOADCHAR_DEBUG) echo("DEBUG: Consumables = ''");
		storeData(%clientId, "StoredQuestItems", "");
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredQuestItems = ''");
		storeData(%clientId, "StoredKeyItems", "");
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredKeyItems = ''");
		storeData(%clientId, "StoredConsumables", "");
		if($LOADCHAR_DEBUG) echo("DEBUG: StoredConsumables = ''");
		storeData(%clientId, "StoredArmor", "");
		//echo("DEBUG: StoredArmor = ''");
		storeData(%clientId, "StoredAccessories", "");
		//echo("DEBUG: StoredAccessories = ''");
		storeData(%clientId, "StoredOther", "");
		//echo("DEBUG: StoredOther = ''");
		storeData(%clientId, "StoredWeapons", "");
		storeData(%clientId, "Weapons", "");
		storeData(%clientId, "EquippedBeltWeapon", "");
		storeData(%clientId, "BeltWeaponShellLoaned", "");
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
			// review #13: award moved below (after the 32-63 passthrough) so it
			// operates on the FULL split BankStorage. Pass field 16 through here; the
			// re-split below authoritatively overwrites it.
			$funk::var["[\"" @ %name @ "\", 0, 16]"] = $funk::var[%name, 0, 16];
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

			// BUGFIX (data-loss trap): this passthrough previously stopped at field 31,
			// silently DROPPING fields 29 and 32-63 on offline awards - all belt
			// categories, stored belt, BankStorage overflow (60-63), stance, damage
			// prefs, Ascension talents, dual-wield off-hand, and AutoSkill config.
			$funk::var["[\"" @ %name @ "\", 0, 29]"] = $funk::var[%name, 0, 29];
			for(%f = 32; %f <= 66; %f++)
				$funk::var["[\"" @ %name @ "\", 0, " @ %f @ "]"] = $funk::var[%name, 0, %f];

			// review #13: apply the offline award onto the FULL BankStorage. The old
			// code did SetStuffString on field 16 ONLY - if the awarded item already
			// lived in an overflow field (60-63), it wasn't found and a duplicate was
			// appended to field 16, corrupting the stack on next login
			// (JoinBankStorageFromLoad merges all 5 fields with no dedup). Join input
			// fields 16+60-63, apply the award, then re-split into the OUTPUT fields.
			// This runs AFTER the field-16 and 32-63 passthroughs above so it
			// authoritatively overwrites the OUTPUT 16/60-63 they just wrote.
			%fullBank = JoinBankStorageFromLoad(%name);
			for(%i = 0; GetWord(%award, %i) != -1; %i += 2)
				%fullBank = SetStuffString(%fullBank, GetWord(%award, %i), GetWord(%award, %i + 1));
			SplitAndSaveBankStorage(-1, %name, %fullBank);

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

	// Clear damage tracking (and the erase-schedule stamps - see DamagedByErase)
	for(%i = 1; %i <= $maxDamagedBy; %i++)
	{
		$damagedBy[%name, %i] = "";
		$damagedByStamp[%name, %i] = "";
	}

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
	{
		$damagedBy[%name, %i] = "";
		$damagedByStamp[%name, %i] = "";
	}

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
