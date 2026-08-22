$HouseName[1] = "HouseKronos";
$HouseName[2] = "HouseArbal";
$HouseName[3] = "HouseCurama";
$HouseName[4] = "HouseYuliple";

//$HouseStartUpEq[1] = "AntivaRobe 1";
//$HouseStartUpEq[2] = "FenyarRobe 1";
//$HouseStartUpEq[3] = "TemminRobe 1";
//$HouseStartUpEq[4] = "VenkRobe 1";
$HouseStartUpEq[1] = "";
$HouseStartUpEq[2] = "";
$HouseStartUpEq[3] = "";
$HouseStartUpEq[4] = "";


$HouseMember[HouseYuliple] = 0;
$HouseMember[HouseCurama] = 0;
$HouseMember[HouseArbal] = 0;
$HouseMember[HouseKronos] = 0;

function GetHouseNumber(%n)
{
	dbecho($dbechoMode, "GetHouseNumber(" @ %n @ ")");

	for(%i = 1; $HouseName[%i] != ""; %i++)
	{
		//if($HouseName[%i] == %n)
		if(String::ICompare($HouseName[%i], %n) == 0)
			return %i;
	}
	return "";
}

function BootFromCurrentHouse(%clientId, %echo)
{
	dbecho($dbechoMode, "BootFromCurrentHouse(" @ %clientId @ ", " @ %echo @ ")");

	// CRITICAL: Skip bots - they should not be in houses
	if(isRPGAI(%clientId) || Player::isAiControlled(%clientId))
		return -1;

	%h = fetchData(%clientId, "MyHouse");
	
	// Treat "0", "House0", and "house0" as empty (no house) - invalid values
	if(%h == "0" || %h == "House0" || %h == "house0")
		%h = "";

	if(%h != "")
	{
		UnequipMountedStuff(%clientId);

		%hn = GetHouseNumber(%h);
		if(%echo) Client::sendMessage(%clientId, $MsgRed, "You have been booted from " @ $HouseName[%hn] @ " and have lost all rank points.");

		storeData(%clientId, "MyHouse", "");
		storeData(%clientId, "RankPoints", 0);
		
		// CRITICAL: Prevent $HouseMember from going negative
		if($HouseMember[%h] > 0)
			$HouseMember[%h] -= 1;
		else
			$HouseMember[%h] = 0;
		
		// Reset to unaffiliated citizen team
		GameBase::setTeam(%clientId, 0);
		
		// Update objectives display to refresh member counts
		UpdateHouseObjectivesDisplay();

		return %hn;
	}
	else
		return -1;
}

function JoinHouse(%clientId, %hn, %echo)
{
	dbecho($dbechoMode, "JoinHouse(" @ %clientId @ ", " @ %hn @ ", " @ %echo @ ")");

	// CRITICAL: Skip bots - they should not join houses
	if(isRPGAI(%clientId) || Player::isAiControlled(%clientId))
		return;
	
	// CRITICAL: Validate house number
	if(%hn == "" || %hn < 1 || %hn > 4 || $HouseName[%hn] == "")
	{
		echo("WARNING: JoinHouse - Invalid house number: " @ %hn @ " for client " @ %clientId);
		return;
	}
	
	// CRITICAL: Boot from current house first to prevent double-counting members
	// This also handles the case where a player switches houses
	%currentHouse = fetchData(%clientId, "MyHouse");
	// Treat invalid values as empty
	if(%currentHouse == "0" || %currentHouse == "House0" || %currentHouse == "house0")
		%currentHouse = "";
	
	if(%currentHouse != "" && %currentHouse != $HouseName[%hn])
	{
		// Player is in a different house - boot them first (silently, we'll announce new house)
		BootFromCurrentHouse(%clientId, false);
	}
	else if(%currentHouse == $HouseName[%hn])
	{
		// Player is already in this house - don't double-count
		if(%echo) Client::sendMessage(%clientId, $MsgBeige, "You are already a member of " @ $HouseName[%hn] @ ".");
		return;
	}

	storeData(%clientId, "MyHouse", $HouseName[%hn]);
	storeData(%clientId, "RankPoints", $joinHouseRankPoints);
	$HouseMember[$HouseName[%hn]] += 1;
	$HouseChecked[Client::getName(%clientId)] = 1;
	
	// All players remain on team 0 (Citizen) to allow NPC dialogue and enemy NPC combat
	// House affiliation is tracked via MyHouse property, not team number
	GameBase::setTeam(%clientId, 0);
	
	// Update objectives display to refresh member counts
	UpdateHouseObjectivesDisplay();

	if(%echo) Client::sendMessage(%clientId, $MsgBeige, "You have joined " @ $HouseName[%hn] @ " and have been awarded " @ $joinHouseRankPoints @ " rank points.");
}

// Helper function to check if two players are in the same house
function IsSameHouse(%client1, %client2)
{
	// Bots are never in the same house as anyone
	if(isRPGAI(%client1) || Player::isAiControlled(%client1))
		return False;
	if(isRPGAI(%client2) || Player::isAiControlled(%client2))
		return False;
	
	%house1 = fetchData(%client1, "MyHouse");
	%house2 = fetchData(%client2, "MyHouse");
	
	// Treat invalid values as empty
	if(%house1 == "0" || %house1 == "House0" || %house1 == "house0")
		%house1 = "";
	if(%house2 == "0" || %house2 == "House0" || %house2 == "house0")
		%house2 = "";
	
	// If either player has no house, they're not in the same house
	if(%house1 == "" || %house2 == "")
		return False;
	
	return String::ICompare(%house1, %house2) == 0;
}

// Helper function to get house name from client
function GetClientHouse(%clientId)
{
	// Bots don't have houses
	if(isRPGAI(%clientId) || Player::isAiControlled(%clientId))
		return "";
	
	%house = fetchData(%clientId, "MyHouse");
	
	// Treat invalid values as empty
	if(%house == "0" || %house == "House0" || %house == "house0")
		return "";

	return %house;
}

// HOUSE-ZERO FIX 2026-08-22: the house name of %clientId, or "" if they are not in
// a real house. Use this for every membership test - NEVER compare
// fetchData(id,"MyHouse") against "" directly.
//
// Why: storeData(id, key, "") stores the STRING "0", not empty (rpgstats.cs, see the
// $DataIsString table), and the engine's comparator makes "0" == "" FALSE
// (darkstar/console/code/eval.cpp compare() falls to strcmp; isFloat("") is false).
// A raw `== ""` test therefore reads a CLEARED house as "in a house", which inverted
// four separate guards at once: objective capture, the level-60 exp block, the rank
// exp bonus, and HouseEarnings payouts.
//
// This validates POSITIVELY against $HouseName[] rather than blacklisting the known
// bad spellings ("0"/"House0"/"house0"/-1/"None"), so any future corrupt value fails
// closed - denying house perks rather than granting them. Unlike GetClientHouse()
// this deliberately does NOT special-case AI, so callers keep their existing bot
// semantics.
function GetHouseOf(%clientId)
{
	%h = fetchData(%clientId, "MyHouse");
	if(GetHouseNumber(%h) == "")
		return "";
	return %h;
}

// HOUSE-GATE 2026-08-22: True if %clientId is barred from gaining experience by the
// house requirement (no house, at or past $houseRequiredLevel). Single source of truth
// for the rule - enforced in storeData's EXP chokepoint, and consulted by reward
// screens (dailies, weekly boss) so they report what was actually granted instead of
// promising exp the chokepoint then denies.
//
// Bots are never blocked: they have no house by definition and gating them would
// silently change bot progression.
function IsExpHouseBlocked(%clientId)
{
	if(isRPGAI(%clientId) || Player::isAiControlled(%clientId))
		return False;

	if(GetHouseOf(%clientId) != "")
		return False;

	return fetchData(%clientId, "LVL") >= $houseRequiredLevel;
}

// House data file paths
$House::LoadFile = "[KoK-Houses].cs";
$House::SaveFile = $House::LoadFile;

// Save house data to file
function HouseData::Save(%echoOff) {
    File::delete($House::SaveFile);
    export("$BaseControl*", $House::SaveFile, false);
    export("$FlagCommand*", $House::SaveFile, true);
    export("$HouseMember*", $House::SaveFile, true);
    if (!%echoOff)
        echo("House data saved to: " @ $House::SaveFile);
}

// Load house data from file
function HouseData::Load() {
    if (isfile($House::LoadFile)) {
        exec($House::LoadFile);
        echo("House data loaded from: " @ $House::LoadFile);
    }
}

// Reset all house data
function HouseData::Reset()
{
	deleteVariables("HouseData::*");
	
	for(%i = 1; %i <= 4; %i++)
	{
		$HouseData::Data[%i, 0] = 0;
		for(%j = 1; %j <= 8; %j++)
			$HouseData::Data[%i, %j] = false;
	}
}