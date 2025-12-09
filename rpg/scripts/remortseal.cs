//Globals for seal system; %i is the id of the battle.
//$SealBattleActive: If true, then a seal battle is currently active for a player.
//Just leave this one as it is.
//$SealValue[%i]: The seal value that is assigned to the player who wins this battle.
///////////////////////////////////////////////////////////////////////
// DEFAULT SEAL MULTIPLIERS SHOULD BE DOABLE BY 4-5 FULLY GEARED PLAYERS. ADJUST AS NECESSARY FOR PLAYERBASE.
///////////////////////////////////////////////////////////////////////
$SealBattleActive = false;
$SealBattleRound1Spawned = false;  // Track if Round 1 was ever spawned
$SealBattleRound1Complete = false;  // Track if Round 1 was completed
$SealBattleRound2Complete = false;  // Track if Round 2 was completed
$SealBattleRound3Complete = false;  // Track if Round 3 was completed
$SealBattleCurrentRound = 0;  // Track the current active round loop (prevents overlapping loops)
$SealBattleLoopScheduled = false;  // Track if a loop is currently scheduled (prevents duplicate schedules)
%i=0;

// CRITICAL: Timing constants for seal battle system - all timings are calculated from these
$SealBattleCountdownDuration = 30;  // Total countdown time before battle starts (seconds)
$SealBattleBotSpawnDelay = 12;  // Time for bots to fully spawn and initialize (seconds)
$SealBattleFreezeDuration = 18;  // Time bots stay frozen after spawning completes (seconds)
// Total time from spawn to active: $SealBattleBotSpawnDelay + $SealBattleFreezeDuration = 30 seconds

// Centralized TotalSealValue management functions
function GetTotalSealValue()
{
    // Ensure TotalSealValue is always initialized and valid
    if($TotalSealValue == "" || $TotalSealValue < 20)
    {
        $TotalSealValue = 20;
        echo("WARNING: TotalSealValue was invalid, reset to 20");
    }
    return $TotalSealValue;
}

function IncrementTotalSealValue()
{
    // Get current value (ensures it's valid)
    %current = GetTotalSealValue();
    
    // Increment by 20
    $TotalSealValue = %current + 20;
    
    // Save to separate file immediately
    File::delete("temp\\SealValue.cs");
    export("TotalSealValue", "temp\\SealValue.cs", false);
    
    echo("TotalSealValue incremented to " @ $TotalSealValue);
    return $TotalSealValue;
}

function SetTotalSealValue(%value)
{
    // Ensure value is at least 20
    if(%value < 20)
        %value = 20;
    
    $TotalSealValue = %value;
    
    // Save to file immediately
    File::delete("temp\\SealValue.cs");
    export("TotalSealValue", "temp\\SealValue.cs", false);
    
    echo("TotalSealValue set to " @ $TotalSealValue);
    return $TotalSealValue;
}

// Round-specific multipliers for seal battle bots
// These multipliers are applied to base stats for each round
// Round 1: Base difficulty
// Round 2: 1.3x harder than Round 1 (2.0 * 1.3 = 2.6)
// Round 3: 1.6x harder than Round 1 (2.0 * 1.6 = 3.2)
$SealBattleRound1Multiplier = 2.0;
$SealBattleRound2Multiplier = 2.6;  // 1.3x harder than Round 1
$SealBattleRound3Multiplier = 3.2;  // 1.6x harder than Round 1

// Base multiplier that scales with TotalSealValue
// Uses a more conservative scaling approach to keep bots challenging but killable
// Formula: 1.0 + (sqrt(TotalSealValue / 20) * 0.3)
// This uses square root scaling so higher seal values don't scale as aggressively
// Examples:
//   TotalSealValue 20:  1.0 + (sqrt(1) * 0.3) = 1.3x
//   TotalSealValue 40:  1.0 + (sqrt(2) * 0.3) = 1.42x
//   TotalSealValue 60:  1.0 + (sqrt(3) * 0.3) = 1.52x
//   TotalSealValue 80:  1.0 + (sqrt(4) * 0.3) = 1.6x
//   TotalSealValue 100: 1.0 + (sqrt(5) * 0.3) = 1.67x
//   TotalSealValue 120: 1.0 + (sqrt(6) * 0.3) = 1.73x
function SealBattle::GetBaseStrengthMultiplier()
{
    %baseMultiplier = 1.0;
    %sealValue = GetTotalSealValue();
    %steps = %sealValue / 20;
    // Use square root scaling to prevent exponential growth
    %sqrtSteps = sqrt(%steps);
    %increment = 0.3;  // Much smaller increment per step
    %multiplier = %baseMultiplier + (%sqrtSteps * %increment);
    return %multiplier;
}

// Get the final multiplier for a specific round
// This combines the base multiplier (from TotalSealValue) with the round-specific multiplier
function SealBattle::GetRoundMultiplier(%round)
{
    %baseMult = SealBattle::GetBaseStrengthMultiplier();
    
    if(%round == 1)
        %roundMult = $SealBattleRound1Multiplier;
    else if(%round == 2)
        %roundMult = $SealBattleRound2Multiplier;
    else if(%round == 3)
        %roundMult = $SealBattleRound3Multiplier;
    else
        %roundMult = 1.0;  // Fallback
    
    // Final multiplier = base multiplier * round multiplier
    return %baseMult * %roundMult;
}

function SealBattle::GetParticipantNames()
{
	// Build a formatted list of participant names
	%participantNames = "";
	%participantCount = 0;
	%list = $SealBattleParticipants;
	
	// Parse comma-separated list
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
		
		%name = Client::getName(%participantId);
		if(%name != "")
		{
			if(%participantNames != "")
				%participantNames = %participantNames @ ", " @ %name;
			else
				%participantNames = %name;
			%participantCount++;
		}
	}
	
	return %participantNames;
}

function SealBattle::MessageColloseumPlayers(%message)
{
	// Send message to all players currently in the Colloseum zone
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(isRPGAI(%cl))
			continue;
		
		%clZoneId = fetchData(%cl, "zone");
		%clZoneDesc = Zone::getDesc(%clZoneId);
		if(%clZoneDesc == "Colloseum")
		{
			Client::sendMessage(%cl, $MsgWhite, %message);
		}
	}
}

function SealBattle::Begin(%clientId,%pos,%seal)
{
	if($SealBattleActive != true)
	{
		storeData(%clientId,"noDropLootbagFlag",false);
		$SealFighterDied = false;
		$SealBattleActive = true;
		
		// Store the zone description and house for checking all friendly players
		// Seal battles always take place in "Colloseum" zone, so we always check for that
		%house = fetchData(%clientId, "MyHouse");
		$SealBattleZone = "Colloseum";  // Seal battles always happen in Colloseum
		$SealBattleHouse = %house;
		
		// Initialize the list of players who join during the 30-second window
		$SealBattleParticipants = "";
		$SealBattleRound1Spawned = false;  // Reset Round 1 spawn flag
		$SealBattleRound1Complete = false;  // Reset Round 1 completion flag
		$SealBattleRound2Complete = false;  // Reset Round 2 completion flag
		$SealBattleRound3Complete = false;  // Reset Round 3 completion flag
		$SealBattleCurrentRound = 0;  // Reset current round tracker
		$SealBattleLoopScheduled = false;  // Reset loop scheduled flag
		
		// Initialize internal name tracking variables (used for reliable bot lookups)
		$SealBattle::FighterName = "";
		$SealBattle::MageName = "";
		$SealBattle::GuardianName = "";
		
		// Add the initiator to the participants list
		$SealBattleParticipants = AddToCommaList($SealBattleParticipants, %clientId);
		
		// Announce to all players that the seal battle will begin
		messageAll(2, "The seal battle will begin in " @ $SealBattleCountdownDuration @ " seconds! Type #helpseal now to join!");
		
		// CRITICAL: Spawn Round 1 bots IMMEDIATELY (they take $SealBattleBotSpawnDelay seconds to fully spawn)
		// Store the parameters in global variables to ensure they persist through the schedule
		$SealBattleScheduledClientId = %clientId;
		$SealBattleScheduledPos = %pos;
		$SealBattleScheduledSeal = %seal;
		
		// Spawn Round 1 bots immediately (frozen and invulnerable)
		// Bots take $SealBattleBotSpawnDelay seconds to fully spawn, and we want them frozen for $SealBattleFreezeDuration seconds AFTER they spawn
		// So unfreeze at $SealBattleCountdownDuration seconds (spawn delay + freeze duration = countdown duration)
		SealBattle::SpawnRound(%clientId, %pos, %seal, 1);
		$SealBattleRound1Spawned = true;  // Mark that Round 1 was spawned
		
		// Schedule countdown messages - calculated from countdown duration
		%message20Time = $SealBattleCountdownDuration - 20;
		%message10Time = $SealBattleCountdownDuration - 10;
		schedule("SealBattle::MessageColloseumPlayers(\"20 seconds until the battle begins...\");", %message20Time);
		schedule("SealBattle::MessageColloseumPlayers(\"10 seconds until the battle begins...\");", %message10Time);
		
		// CRITICAL: Do NOT schedule unfreeze here - it will be called in StartBattle() at countdown duration
		// This ensures bots are frozen for the full freeze duration after spawning (spawn delay + freeze duration = countdown duration)
		
		// Schedule the actual battle start after countdown duration
		schedule("SealBattle::StartBattle($SealBattleScheduledClientId, $SealBattleScheduledPos, $SealBattleScheduledSeal);", $SealBattleCountdownDuration);
	}
}

function SealBattle::StartBattle(%clientId,%pos,%seal)
{
	// Capture all players currently in Colloseum as participants (30-second window just ended)
	%colloseumEntrance = "-3588 -2364 354";
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(isRPGAI(%cl))
			continue;
		
		%clZoneId = fetchData(%cl, "zone");
		%clZoneDesc = Zone::getDesc(%clZoneId);
		if(%clZoneDesc == "Colloseum")
		{
			// Add to participants list if not already there
			if(String::findSubStr($SealBattleParticipants, %cl) == -1)
			{
				$SealBattleParticipants = AddToCommaList($SealBattleParticipants, %cl);
			}
			
			// Set lastzone to Colloseum zone ID so players spawn correctly if they die
			storeData(%cl, "lastzone", %clZoneId);
			// Also ensure they're at the Colloseum entrance
			%playerObj = Client::getOwnedObject(%cl);
			if(%playerObj != -1)
				GameBase::setPosition(%playerObj, %colloseumEntrance);
		}
	}
	
	// Build a formatted list of participant names for the message
	%participantNames = SealBattle::GetParticipantNames();
	
	// Send message with participant list
	if(%participantNames != "")
	{
		messageAll(2, "Wish " @ %participantNames @ " good luck in breaking the seal!");
	}
	
	// CRITICAL: Unfreeze Round 1 bots now (countdown duration has passed, bots have been frozen for freeze duration after spawning)
	// Bots spawned at time 0, took $SealBattleBotSpawnDelay seconds to spawn, and have been frozen for $SealBattleFreezeDuration seconds total
	// This is called exactly when the countdown duration ends, so bots become active as the battle starts
	SealBattle::UnfreezeRound(1);
	
	// Send message that Round 1 is starting
	SealBattle::MessageColloseumPlayers("Wave 1 bots are now active!");
	
	// Start the battle loop with Round 1
	SealBattle::Loop(%clientId,%pos,%seal,1);
}

function SealBattle::Loop(%clientId,%pos,%seal,%round)
{
	%name = Client::getName(%clientId);
	%player = Client::getOwnedObject(%clientId);
	
	// Enforce Bane restriction
	%currentStance = fetchData(%clientId, "Stance");
	if(%currentStance == "MageBane" || %currentStance == "BladeBane")
	{
		storeData(%clientId, "Stance", "Normal");
		Client::sendMessage(%clientId, $MsgRed, "Something is wrong... my bane isn't working?");
		RefreshAll(%clientId);
	}
	
	// CRITICAL: Only process this loop if it matches the current active round
	// This prevents overlapping loops from previous rounds from interfering
	if($SealBattleCurrentRound != 0 && $SealBattleCurrentRound != %round)
	{
		// This is a stale loop from a previous round - ignore it
		return;
	}
	
	// Update the current active round
	$SealBattleCurrentRound = %round;
	$SealBattleLoopScheduled = false;  // Clear scheduled flag since we're now running
	
	// Check if all players are dead or have left - if so, kill all bots and exit immediately
	if($SealFighterDied)
	{
		// Play random enemy taunt sound
		%randtaunt = floor(getRandom()*3);
		if(%randtaunt == 0) %sound = "OgreTaunt1.wav";
		if(%randtaunt == 1) %sound = "UndeadTaunt1.wav";
		if(%randtaunt == 2) %sound = "GnollTaunt1.wav";
		Client::sendMessage(%clientId,$MsgRed,"You died...~w"@%sound);
		
		// Kill all seal bots from all rounds using internal names (reliable)
		// Try internal names first, fallback to display names
		if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
		{
			%f = AI::getClientIdFromName($SealBattle::FighterName);
			if(%f == "") %f = -1; // Normalize empty string to -1
			// CRITICAL SAFEGUARD: Only kill if it is actually an AI
			if(%f != -1 && Player::isAiControlled(%f)) Player::Kill(%f);
		}
		if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
		{
			%m = AI::getClientIdFromName($SealBattle::MageName);
			if(%m == "") %m = -1; // Normalize empty string to -1
			// CRITICAL SAFEGUARD: Only kill if it is actually an AI
			if(%m != -1 && Player::isAiControlled(%m)) Player::Kill(%m);
		}
		if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
		{
			%g = AI::getClientIdFromName($SealBattle::GuardianName);
			if(%g == "") %g = -1; // Normalize empty string to -1
			// CRITICAL SAFEGUARD: Only kill if it is actually an AI
			if(%g != -1 && Player::isAiControlled(%g)) Player::Kill(%g);
		}
		// Fallback: Also try display names for any bots we might have missed
		for(%r = 1; %r <= 3; %r++)
		{
			%g = NEWgetClientByName("SealGuardian" @ %r);
			%m = NEWgetClientByName("SealMage" @ %r);
			%f = NEWgetClientByName("SealFighter" @ %r);
			// CRITICAL SAFEGUARD: Only kill if it is actually an AI
			if(%g != -1 && Player::isAiControlled(%g)) Player::Kill(%g);
			if(%m != -1 && Player::isAiControlled(%m)) Player::Kill(%m);
			if(%f != -1 && Player::isAiControlled(%f)) Player::Kill(%f);
		}
		
		// Broadcast message that all players have died
		%participantNames = SealBattle::GetParticipantNames();
		messageAll(2, %participantNames @ " have died attempting to shatter the seal... Heavens be with us!");
		
		SealBattle::Conclude(%clientId, false);  // false = failure, teleport back
		return;
	}
	
	if(%round == 1)
	{
		// FIRST check if all players are dead - if so, end battle immediately
		SealBattle::LazyDeathCheck(%clientId);
		if($SealFighterDied)
		{
			return;
		}
		
		// CRITICAL: Check if Round 1 bots already exist using internal names (reliable)
		%fighter1Id = -1;
		%mage1Id = -1;
		%guardian1Id = -1;
		
		// Try internal name lookup first (reliable)
		if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
		{
			%fighter1Id = AI::getClientIdFromName($SealBattle::FighterName);
			if(%fighter1Id == "") %fighter1Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
		{
			%mage1Id = AI::getClientIdFromName($SealBattle::MageName);
			if(%mage1Id == "") %mage1Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
		{
			%guardian1Id = AI::getClientIdFromName($SealBattle::GuardianName);
			if(%guardian1Id == "") %guardian1Id = -1; // Normalize empty string to -1
		}
		
		// Fallback to display name lookup if internal names not available or lookup failed
		if(%fighter1Id == -1)
			%fighter1Id = NEWgetClientByName("SealFighter1");
		if(%mage1Id == -1)
			%mage1Id = NEWgetClientByName("SealMage1");
		if(%guardian1Id == -1)
			%guardian1Id = NEWgetClientByName("SealGuardian1");
		
		// If Round 1 bots exist, just continue checking (they're alive)
		if(%fighter1Id != -1 || %mage1Id != -1 || %guardian1Id != -1)
		{
			// Round 1 bots still exist - mark that Round 1 was spawned
			$SealBattleRound1Spawned = true;
			// Just continue checking
			// Don't spawn again, just let the loop continue
		}
		else
		{
			// Round 1 bots don't exist - check if Round 1 was ever spawned
			if($SealBattleRound1Spawned == true)
			{
				// Round 1 was spawned and bots are now dead - mark Round 1 as complete and spawn Round 2
				$SealBattleRound1Complete = true;  // Mark Round 1 as completed
				// Check Round 2 bots using internal names (reliable)
				%fighter2Id = -1;
				%mage2Id = -1;
				%guardian2Id = -1;
				if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
				{
					%fighter2Id = AI::getClientIdFromName($SealBattle::FighterName);
					if(%fighter2Id == "") %fighter2Id = -1; // Normalize empty string to -1
				}
				if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
				{
					%mage2Id = AI::getClientIdFromName($SealBattle::MageName);
					if(%mage2Id == "") %mage2Id = -1; // Normalize empty string to -1
				}
				if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
				{
					%guardian2Id = AI::getClientIdFromName($SealBattle::GuardianName);
					if(%guardian2Id == "") %guardian2Id = -1; // Normalize empty string to -1
				}
				// Fallback to display name lookup
				if(%fighter2Id == -1)
					%fighter2Id = NEWgetClientByName("SealFighter2");
				if(%mage2Id == -1)
					%mage2Id = NEWgetClientByName("SealMage2");
				if(%guardian2Id == -1)
					%guardian2Id = NEWgetClientByName("SealGuardian2");
				
				// Only spawn Round 2 if it doesn't already exist
				if(%fighter2Id == -1 && %mage2Id == -1 && %guardian2Id == -1)
				{
				// Round 1 was completed and Round 2 hasn't started - spawn Round 2
				$SealBattleCurrentRound = 2;  // Update current round BEFORE scheduling
				$SealBattleLoopScheduled = true;  // Mark that we're scheduling the next round
				SealBattle::SpawnRound(%clientId, %pos, %seal, 2);
				
				// CRITICAL: Schedule countdown messages for Round 2 - calculated from delays
				%totalRound2Time = $SealBattleBotSpawnDelay + $SealBattleFreezeDuration;
				%round2Message10Time = %totalRound2Time - 10;
				%round2Message5Time = %totalRound2Time - 5;
				SealBattle::MessageColloseumPlayers("Wave 2 bots are spawning! " @ $SealBattleFreezeDuration @ " seconds until they become active...");
				schedule("SealBattle::MessageColloseumPlayers(\"10 seconds until Wave 2 begins...\");", %round2Message10Time);
				schedule("SealBattle::MessageColloseumPlayers(\"5 seconds until Wave 2 begins...\");", %round2Message5Time);
				
				// CRITICAL: Schedule unfreeze after total time from spawn (spawn delay + freeze duration)
				// This ensures bots are frozen for the full freeze duration AFTER they finish spawning
				schedule("SealBattle::UnfreezeRound(2);", %totalRound2Time);
				// CRITICAL: Schedule the loop for Round 2 after bots are unfrozen
				schedule("SealBattle::Loop(" @ %clientId @ ", \"" @ %pos @ "\", " @ %seal @ ", 2);", 20);
				return;
				}
				else
				{
					// Round 2 already exists - switch to Round 2 loop
					$SealBattleCurrentRound = 2;  // Update current round BEFORE scheduling
					$SealBattleLoopScheduled = true;  // Mark that we're scheduling the next round
					schedule("SealBattle::Loop(" @ %clientId @ ", \"" @ %pos @ "\", " @ %seal @ ", 2);", 5);
					return;
				}
			}
			else
			{
				// Round 1 bots don't exist and weren't spawned - this shouldn't happen if Begin() worked correctly
				// But handle it gracefully by spawning them now
				%sealValue = GetTotalSealValue();
				messageall(2,"The current seal value is " @ %sealValue @ "");
				$SealBattleCurrentRound = 1;  // Update current round BEFORE scheduling
				$SealBattleLoopScheduled = true;  // Mark that we're scheduling the next round
				SealBattle::SpawnRound(%clientId, %pos, %seal, 1);
				$SealBattleRound1Spawned = true;  // Mark that Round 1 was spawned
				// CRITICAL: Schedule the loop for Round 1 after spawning (account for 12 second spawn delay)
				schedule("SealBattle::Loop(" @ %clientId @ ", \"" @ %pos @ "\", " @ %seal @ ", 1);", 15);
				return;
			}
		}
	}
	else if(%round == 2)
	{
		// FIRST check if all players are dead - if so, end battle immediately
		SealBattle::LazyDeathCheck(%clientId);
		if($SealFighterDied)
		{
			return;
		}
		
		// CRITICAL: Check if Round 2 bots already exist using internal names (reliable)
		%fighter2Id = -1;
		%mage2Id = -1;
		%guardian2Id = -1;
		if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
		{
			%fighter2Id = AI::getClientIdFromName($SealBattle::FighterName);
			if(%fighter2Id == "") %fighter2Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
		{
			%mage2Id = AI::getClientIdFromName($SealBattle::MageName);
			if(%mage2Id == "") %mage2Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
		{
			%guardian2Id = AI::getClientIdFromName($SealBattle::GuardianName);
			if(%guardian2Id == "") %guardian2Id = -1; // Normalize empty string to -1
		}
		// Fallback to display name lookup
		if(%fighter2Id == -1)
			%fighter2Id = NEWgetClientByName("SealFighter2");
		if(%mage2Id == -1)
			%mage2Id = NEWgetClientByName("SealMage2");
		if(%guardian2Id == -1)
			%guardian2Id = NEWgetClientByName("SealGuardian2");
		
		// Check if Round 2 bots are dead - if so, advance to Round 3
		if(%fighter2Id == -1 && %mage2Id == -1 && %guardian2Id == -1)
		{
			// Round 2 bots are dead - check if Round 3 bots already exist using internal names
			%fighter3Id = -1;
			%mage3Id = -1;
			%guardian3Id = -1;
			if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
			{
				%fighter3Id = AI::getClientIdFromName($SealBattle::FighterName);
				if(%fighter3Id == "") %fighter3Id = -1; // Normalize empty string to -1
			}
			if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
			{
				%mage3Id = AI::getClientIdFromName($SealBattle::MageName);
				if(%mage3Id == "") %mage3Id = -1; // Normalize empty string to -1
			}
			if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
			{
				%guardian3Id = AI::getClientIdFromName($SealBattle::GuardianName);
				if(%guardian3Id == "") %guardian3Id = -1; // Normalize empty string to -1
			}
			// Fallback to display name lookup
			if(%fighter3Id == -1)
				%fighter3Id = NEWgetClientByName("SealFighter3");
			if(%mage3Id == -1)
				%mage3Id = NEWgetClientByName("SealMage3");
			if(%guardian3Id == -1)
				%guardian3Id = NEWgetClientByName("SealGuardian3");
			
			// If Round 3 bots don't exist, spawn Round 3
			if(%fighter3Id == -1 && %mage3Id == -1 && %guardian3Id == -1)
			{
				// Round 2 bots are dead and Round 3 hasn't started - mark Round 2 as complete and spawn Round 3
				$SealBattleRound2Complete = true;  // Mark Round 2 as completed
				$SealBattleCurrentRound = 3;  // Update current round BEFORE scheduling
				$SealBattleLoopScheduled = true;  // Mark that we're scheduling the next round
				SealBattle::SpawnRound(%clientId, %pos, %seal, 3);
				
				// CRITICAL: Schedule countdown messages for Round 3 - calculated from delays
				%totalRound3Time = $SealBattleBotSpawnDelay + $SealBattleFreezeDuration;
				%round3Message10Time = %totalRound3Time - 10;
				%round3Message5Time = %totalRound3Time - 5;
				SealBattle::MessageColloseumPlayers("Wave 3 bots are spawning! " @ $SealBattleFreezeDuration @ " seconds until they become active...");
				schedule("SealBattle::MessageColloseumPlayers(\"10 seconds until Wave 3 begins...\");", %round3Message10Time);
				schedule("SealBattle::MessageColloseumPlayers(\"5 seconds until Wave 3 begins...\");", %round3Message5Time);
				
				// CRITICAL: Schedule unfreeze after total time from spawn (spawn delay + freeze duration)
				// This ensures bots are frozen for the full freeze duration AFTER they finish spawning
				schedule("SealBattle::UnfreezeRound(3);", %totalRound3Time);
				// CRITICAL: Schedule the loop for Round 3 after bots are unfrozen
				schedule("SealBattle::Loop(" @ %clientId @ ", \"" @ %pos @ "\", " @ %seal @ ", 3);", 20);
				return;
			}
			// If Round 3 bots exist, just continue checking Round 3
			// (This shouldn't happen, but handle it gracefully)
		}
		else
		{
			// Round 2 bots still exist - just continue checking
			// Don't spawn again, just let the loop continue
		}
	}
	else if(%round == 3)
	{
		// FIRST check if all players are dead - if so, end battle immediately
		SealBattle::LazyDeathCheck(%clientId);
		if($SealFighterDied)
		{
			return;
		}
		
		// CRITICAL: Check if Round 3 bots already exist using internal names (reliable)
		%fighter3Id = -1;
		%mage3Id = -1;
		%guardian3Id = -1;
		if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
		{
			%fighter3Id = AI::getClientIdFromName($SealBattle::FighterName);
			if(%fighter3Id == "") %fighter3Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
		{
			%mage3Id = AI::getClientIdFromName($SealBattle::MageName);
			if(%mage3Id == "") %mage3Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
		{
			%guardian3Id = AI::getClientIdFromName($SealBattle::GuardianName);
			if(%guardian3Id == "") %guardian3Id = -1; // Normalize empty string to -1
		}
		// Fallback to display name lookup
		if(%fighter3Id == -1)
			%fighter3Id = NEWgetClientByName("SealFighter3");
		if(%mage3Id == -1)
			%mage3Id = NEWgetClientByName("SealMage3");
		if(%guardian3Id == -1)
			%guardian3Id = NEWgetClientByName("SealGuardian3");
		
		// Check if Round 3 bots are dead - if so, conclude the battle (victory)
		if(%fighter3Id == -1 && %mage3Id == -1 && %guardian3Id == -1)
		{
			// Round 3 bots are dead - verify Round 2 bots are also dead (should be, but double-check) using internal names
			%fighter2Id = -1;
			%mage2Id = -1;
			%guardian2Id = -1;
			if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
			{
				%fighter2Id = AI::getClientIdFromName($SealBattle::FighterName);
				if(%fighter2Id == "") %fighter2Id = -1; // Normalize empty string to -1
			}
			if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
			{
				%mage2Id = AI::getClientIdFromName($SealBattle::MageName);
				if(%mage2Id == "") %mage2Id = -1; // Normalize empty string to -1
			}
			if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
			{
				%guardian2Id = AI::getClientIdFromName($SealBattle::GuardianName);
				if(%guardian2Id == "") %guardian2Id = -1; // Normalize empty string to -1
			}
			// Fallback to display name lookup
			if(%fighter2Id == -1)
				%fighter2Id = NEWgetClientByName("SealFighter2");
			if(%mage2Id == -1)
				%mage2Id = NEWgetClientByName("SealMage2");
			if(%guardian2Id == -1)
				%guardian2Id = NEWgetClientByName("SealGuardian2");
			
			// Only conclude if Round 2 bots are also dead (ensures Round 3 was actually completed)
			if(%fighter2Id == -1 && %mage2Id == -1 && %guardian2Id == -1)
			{
				// All rounds complete! Seal battle won!
				$SealBattleRound3Complete = true;  // Mark Round 3 as completed
				%participantNames = SealBattle::GetParticipantNames();
				
				// Send success message with participant list
				if(%participantNames != "")
				{
					messageAll(2, "The final wave has been beaten! The brave soldier(s) of Kronos-" @ %participantNames @ ", have shattered the seal! We can all rest and remort safely...for now. ~wflag_capture.wav");
				}
				else
				{
					messageAll(2, "The final wave has been beaten! The seal has been shattered...for now. ~wflag_capture.wav");
				}
				
				// Increment and save seal value using centralized function
				%newSealValue = IncrementTotalSealValue();
				messageall(2,"The remort cap is now " @ %newSealValue @ "!");
				
				Saveworld();
				SealBattle::Conclude(%clientId, true);  // true = success, don't teleport back
				return;
			}
			// If Round 2 bots are still alive, Round 3 hasn't been spawned yet - wait for Round 2 to finish
		}
		else
		{
			// Round 3 bots still exist - just continue checking
			// Don't spawn again, just let the loop continue
		}
	}
	else if(%round == 4)
	{
		// FIRST check if all players are dead - if so, end battle immediately
		SealBattle::LazyDeathCheck(%clientId);
		if($SealFighterDied)
		{
			return;
		}
		
		// Check if Round 3 bots are dead using internal names (reliable)
		%fighter3Id = -1;
		%mage3Id = -1;
		%guardian3Id = -1;
		if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
		{
			%fighter3Id = AI::getClientIdFromName($SealBattle::FighterName);
			if(%fighter3Id == "") %fighter3Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
		{
			%mage3Id = AI::getClientIdFromName($SealBattle::MageName);
			if(%mage3Id == "") %mage3Id = -1; // Normalize empty string to -1
		}
		if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
		{
			%guardian3Id = AI::getClientIdFromName($SealBattle::GuardianName);
			if(%guardian3Id == "") %guardian3Id = -1; // Normalize empty string to -1
		}
		// Fallback to display name lookup
		if(%fighter3Id == -1)
			%fighter3Id = NEWgetClientByName("SealFighter3");
		if(%mage3Id == -1)
			%mage3Id = NEWgetClientByName("SealMage3");
		if(%guardian3Id == -1)
			%guardian3Id = NEWgetClientByName("SealGuardian3");
		
		if(%fighter3Id == -1 && %mage3Id == -1 && %guardian3Id == -1)
		{
			// All rounds complete! Seal battle won!
			%participantNames = SealBattle::GetParticipantNames();
			
			// Send success message with participant list
			if(%participantNames != "")
			{
				messageAll(2, "The final wave has been beaten! The brave soldier(s) of Kronos-" @ %participantNames @ ", have shattered the seal! We can all rest and remort safely...for now. ~wflag_capture.wav");
			}
			else
			{
				messageAll(2, "The final wave has been beaten! The seal has been shattered...for now. ~wflag_capture.wav");
			}
			
			// Increment and save seal value using centralized function
			%newSealValue = IncrementTotalSealValue();
			messageall(2,"The remort cap is now " @ %newSealValue @ "!");
			
			Saveworld();
			SealBattle::Conclude(%clientId, true);  // true = success, don't teleport back
			return;
		}
	}
	
	// Continue the loop - check for deaths and bot status
	// CRITICAL: Only schedule the next iteration if we haven't already scheduled a round advance
	// This prevents overlapping loops when a round completes and advances to the next round
	if($SealFighterDied != true && $SealBattleLoopScheduled == false)
	{
		// Schedule the next loop iteration
		// For Round 1, check every 10 seconds (bots take time to spawn and fight)
		// For Round 2+, check every 5 seconds (faster progression)
		%loopDelay = 10;
		if(%round >= 2)
			%loopDelay = 5;
		
		// Schedule death checks periodically
		for(%i=0;%i<5;%i++)
			schedule("SealBattle::LazyDeathCheck("@%clientId@");", %loopDelay * %i, %player);
		
		// Schedule the next loop iteration
		$SealBattleLoopScheduled = true;  // Mark that we're scheduling a continuation
		schedule("SealBattle::Loop("@%clientId@",\""@%pos@"\","@%seal@","@%round@");", %loopDelay);
	}
}

// Helper function to spawn a single bot and capture its internal name
// This fixes the race condition where SetupBot fails to find bots by display name
// Arguments: %aiTypeIndex - spawn index, %displayName - display name (e.g., "SealFighter1"), %spawnLoc - spawn location string, %pos - battle position, %round - round number
function SealBattle::SpawnSingleBot(%aiTypeIndex, %displayName, %spawnLoc, %pos, %round)
{
	// Call AI::helper and capture the returned internal name (e.g., "RoundTwo497")
	%internalName = AI::helper(%aiTypeIndex, %displayName, %spawnLoc, default);
	
	// If spawn failed, log error and return
	if(%internalName == -1 || %internalName == "")
	{
		echo("ERROR SealBattle::SpawnSingleBot: Failed to spawn bot " @ %displayName @ " (aiTypeIndex=" @ %aiTypeIndex @ ")");
		return;
	}
	
	// Store internal name globally based on bot type (Fighter, Mage, or Guardian)
	// This allows the Loop function to check if bots are alive using reliable internal name lookups
	if(String::findSubStr(%displayName, "Fighter") != -1)
		$SealBattle::FighterName = %internalName;
	else if(String::findSubStr(%displayName, "Mage") != -1)
		$SealBattle::MageName = %internalName;
	else if(String::findSubStr(%displayName, "Guardian") != -1)
		$SealBattle::GuardianName = %internalName;
	
	// Schedule SetupBot using the internal name (100% reliable, available immediately)
	// CRITICAL: Schedule at 4.5 seconds after spawn to ensure SpawnAIGetClientId has completed first
	// SpawnAIGetClientId is scheduled at 3.0s, so 4.5s gives it time to register the bot
	// This prevents SetupBot from failing and SpawnAIGetClientId from deleting the bot as a "ghost"
	schedule("SealBattle::SetupBot(\"" @ %internalName @ "\", \"" @ %pos @ "\", " @ %round @ ");", 4.5);
	echo("[SEAL BATTLE] SealBattle::SpawnSingleBot: Spawned bot " @ %displayName @ " with internal name " @ %internalName @ ", scheduled SetupBot in 4.5 seconds (after SpawnAIGetClientId completes)");
}

// Unified function to spawn bots for any round
function SealBattle::SpawnRound(%clientId, %pos, %seal, %round)
{
	// CRITICAL: Check if bots for this round already exist before spawning
	// This prevents multiple spawns of the same round
	// Use internal names if available, fallback to display name lookup
	%fighterId = -1;
	%mageId = -1;
	%guardianId = -1;
	
	// Try internal name lookup first (reliable)
	if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
	{
		%fighterId = AI::getClientIdFromName($SealBattle::FighterName);
		if(%fighterId == "") %fighterId = -1; // Normalize empty string to -1
	}
	if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
	{
		%mageId = AI::getClientIdFromName($SealBattle::MageName);
		if(%mageId == "") %mageId = -1; // Normalize empty string to -1
	}
	if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
	{
		%guardianId = AI::getClientIdFromName($SealBattle::GuardianName);
		if(%guardianId == "") %guardianId = -1; // Normalize empty string to -1
	}
	
	// Fallback to display name lookup if internal names not available or lookup failed
	if(%fighterId == -1)
		%fighterId = NEWgetClientByName("SealFighter" @ %round);
	if(%mageId == -1)
		%mageId = NEWgetClientByName("SealMage" @ %round);
	if(%guardianId == -1)
		%guardianId = NEWgetClientByName("SealGuardian" @ %round);
	
	// Only spawn if bots don't already exist
	if(%fighterId == -1 && %mageId == -1 && %guardianId == -1)
	{
		// Send "round starting now" message to Colloseum players
		SealBattle::MessageColloseumPlayers("Wave " @ %round @ " is starting now!");
		
		// Get participant names for wave announcement
		%participantNames = SealBattle::GetParticipantNames();
		if(%participantNames != "")
			messageall(2, "" @ %participantNames @ " are entering wave " @ %round @ ".");
		else
			messageall(2, "" @ Client::getName(%clientId) @ " is entering wave " @ %round @ ".");
		
		// Use same bot types for all rounds: RoundOne (Fighter), RoundTwo (Mage), RoundThree (Guardian)
		// All rounds use spawnIndex 66 (RoundOne), 67 (RoundTwo), 68 (RoundThree)
		// Stat scaling is handled by SealBattle::SetupBot() based on the round parameter
		%fighterBot = 66;  // RoundOne - Fighter
		%mageBot = 67;     // RoundTwo - Mage
		%guardianBot = 68;  // RoundThree - Guardian
		
		// Validate spawn indexes exist
		if($spawnIndex[%fighterBot] == "" || $spawnIndex[%mageBot] == "" || $spawnIndex[%guardianBot] == "")
		{
			echo("ERROR: SealBattle::SpawnRound - Invalid spawn indexes (Round " @ %round @ "): fighter=" @ %fighterBot @ ", mage=" @ %mageBot @ ", guardian=" @ %guardianBot);
			return;
		}
		
		// Spawn the 3 bots for this round at specific positions
		// Fighter spawns at: -3608.97 -2371.97 355
		// Mage spawns at: -3609.04 -2363.99 354
		// Guardian spawns at: -3609.01 -2356 354
		// CRITICAL: Use SpawnSingleBot to capture internal names and avoid race conditions
		// Fighter: Call immediately
		SealBattle::SpawnSingleBot($spawnIndex[%fighterBot], "SealFighter" @ %round, "TempSpawn -3608.97 -2371.97 355 1", %pos, %round);
		
		// Spawn Mage 0.5 seconds after Fighter
		schedule("SealBattle::SpawnSingleBot(" @ $spawnIndex[%mageBot] @ ", \"SealMage" @ %round @ "\", \"TempSpawn -3609.04 -2363.99 354 1\", \"" @ %pos @ "\", " @ %round @ ");", 0.5);
		
		// Spawn Guardian 1.0 seconds after Fighter (0.5 seconds after Mage)
		schedule("SealBattle::SpawnSingleBot(" @ $spawnIndex[%guardianBot] @ ", \"SealGuardian" @ %round @ "\", \"TempSpawn -3609.01 -2356 354 1\", \"" @ %pos @ "\", " @ %round @ ");", 1.0);
		
		// CRITICAL: Do NOT schedule the loop here - let the main loop handle all scheduling
		// The loop will continue checking when it's scheduled from the main loop logic
	}
	else
	{
		// Bots for this round already exist - do nothing, let the main loop handle checking
		// The main loop will continue checking when it's scheduled
		return;
	}
}

function SealBattle::LazyDeathCheck(%clientId)
{
	// Check if all participants are dead OR have left the Colloseum zone
	// This handles cases where players died, respawned elsewhere, or teleported out
	
	if($SealBattleParticipants == "")
	{
		// Fallback: if no participant list, check the initiator
		if(IsDead(%clientId))
		{
			$SealFighterDied = true;
			return;
		}
		// Also check if initiator has left the Colloseum zone
		%clZoneId = fetchData(%clientId, "zone");
		%clZoneDesc = Zone::getDesc(%clZoneId);
		if(%clZoneDesc != "Colloseum")
		{
			// Initiator has left the Colloseum - end the battle
			$SealFighterDied = true;
		}
		return;
	}
	
	// Count all participants and check if they're all dead OR have left the Colloseum zone
	%aliveParticipantCount = 0;
	%totalParticipantCount = 0;
	%list = $SealBattleParticipants;
	
	// Parse comma-separated list
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
		
		// Skip if invalid client ID
		if(%participantId == "" || %participantId == -1)
			continue;
		
		// Check if client still exists
		%clName = Client::getName(%participantId);
		if(%clName == "")
			continue;
		
		// Count this participant
		%totalParticipantCount++;
		
		// Check if participant is still in the battle (alive AND in Colloseum zone)
		%isDead = IsDead(%participantId);
		if(!%isDead)
		{
			// Participant is alive - check if they're still in Colloseum
			%clZoneId = fetchData(%participantId, "zone");
			%clZoneDesc = Zone::getDesc(%clZoneId);
			if(%clZoneDesc == "Colloseum")
			{
				// Participant is alive and still in Colloseum - they're still in the battle
				%aliveParticipantCount++;
			}
			// If participant is alive but not in Colloseum, they've left the battle
		}
		// If participant is dead, they're not in the battle anymore
	}
	
	// Set SealFighterDied if:
	// 1. We found participants AND they're all dead or have left (%totalParticipantCount > 0 && %aliveParticipantCount == 0)
	// 2. OR we found no valid participants at all (%totalParticipantCount == 0) - all disconnected/invalid
	// This prevents setting it to true before participants have been added (only if $SealBattleParticipants was empty, which is handled above)
	if((%totalParticipantCount > 0 && %aliveParticipantCount == 0) || %totalParticipantCount == 0)
	{
		$SealFighterDied = true;
	}
}

function SealBattle::Conclude(%clientId, %success)
{
	// CRITICAL: Kill all seal bots from all rounds before concluding
	// This ensures bots are cleaned up even if Conclude() is called from outside the loop
	// Try internal names first (reliable)
	if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
	{
		%f = AI::getClientIdFromName($SealBattle::FighterName);
		if(%f == "") %f = -1; // Normalize empty string to -1
		if(%f != -1) Player::Kill(%f);
	}
	if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
	{
		%m = AI::getClientIdFromName($SealBattle::MageName);
		if(%m == "") %m = -1; // Normalize empty string to -1
		if(%m != -1) Player::Kill(%m);
	}
	if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
	{
		%g = AI::getClientIdFromName($SealBattle::GuardianName);
		if(%g == "") %g = -1; // Normalize empty string to -1
		if(%g != -1) Player::Kill(%g);
	}
	// Fallback: Also try display names for any bots we might have missed
	for(%r = 1; %r <= 3; %r++)
	{
		%g = NEWgetClientByName("SealGuardian" @ %r);
		%m = NEWgetClientByName("SealMage" @ %r);
		%f = NEWgetClientByName("SealFighter" @ %r);
		if(%g != -1) Player::Kill(%g);
		if(%m != -1) Player::Kill(%m);
		if(%f != -1) Player::Kill(%f);
	}
	
	// Get the zone description and house for finding all friendly players
	%zoneDesc = $SealBattleZone;  // This is now a zone description, not an ID
	%house = $SealBattleHouse;
	%lospos = "-3588 -2364 354";
	%outpos = "-335 -2339 65.5";
	
	// Find all friendly players who were in the seal battle zone and teleport them out
	if(%zoneDesc != "" && %zoneDesc != -1)
	{
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
		{
			// Skip AI bots
			if(isRPGAI(%cl))
				continue;
			
			// Check if player was in the seal battle zone (compare zone descriptions)
			// All players in Colloseum are considered friendly, regardless of house
			%clZoneId = fetchData(%cl, "zone");
			%clZoneDesc = Zone::getDesc(%clZoneId);
			if(%clZoneDesc != %zoneDesc)
				continue;
			
			// All players in Colloseum are friendly (no house check needed)
			// Clear lootbag flag
			storeData(%cl, "noDropLootbagFlag", false);
			
			// CRITICAL: Clear any movement-related flags that might have been accidentally set on players
			// These flags should only be set on bots, but clear them from players as a safety measure
			storeData(%cl, "frozen", "");
			storeData(%cl, "ShovedByPlayer", "");
			$BotFrozen[%cl] = "";
			$EnemyBotData[%cl, "frozen"] = "";
			$ClientData[%cl, "frozen"] = "";
			
			// Spawn player if dead, then teleport them back into Colloseum to get their pack
			if(IsDead(%cl))
			{
				Game::playerSpawn(%cl, True);
				// Schedule position fix after spawn completes - teleport to Colloseum entrance
				schedule("SealBattle::FixSpawnPosition(" @ %cl @ ", \"" @ %lospos @ "\");", 0.5, %cl);
			}
			
			if(!%success)
			{
				// Failure: Teleport to pack retrieval location (Colloseum entrance) first, then out after 1 minute
				// If player was already alive, teleport them now; if dead, position was fixed above
				if(!IsDead(%cl))
				{
					%playerObj = Client::getOwnedObject(%cl);
					if(%playerObj != -1)
						GameBase::setPosition(%playerObj, %lospos);
				}
				Client::sendMessage(%cl, $MsgWhite, "You have one minute to retrieve your pack before you are teleported out.");
				schedule("SealBattle::TeleportOut(" @ %cl @ ");", 60, %cl);
			}
			else
			{
				// Success: Teleport out immediately
				// If player was dead, wait for spawn to complete first
				if(IsDead(%cl))
				{
					schedule("SealBattle::TeleportOutSuccess(" @ %cl @ ", \"" @ %outpos @ "\");", 0.5, %cl);
				}
				else
				{
					%playerObj = Client::getOwnedObject(%cl);
					if(%playerObj != -1)
						GameBase::setPosition(%playerObj, %outpos);
					Client::sendMessage(%cl, $MsgWhite, "The seal battle has ended. You have been teleported out.");
				}
			}
		}
	}
	else
	{
		// Fallback: if zone not stored, just handle the initiator
		storeData(%clientId, "noDropLootbagFlag", false);
		
		// CRITICAL: Clear any movement-related flags that might have been accidentally set on players
		// These flags should only be set on bots, but clear them from players as a safety measure
		storeData(%clientId, "frozen", "");
		storeData(%clientId, "ShovedByPlayer", "");
		$BotFrozen[%clientId] = "";
		$EnemyBotData[%clientId, "frozen"] = "";
		$ClientData[%clientId, "frozen"] = "";
		if(IsDead(%clientId))
		{
			Game::playerSpawn(%clientId, True);
			// Schedule position fix after spawn completes
			schedule("SealBattle::FixSpawnPosition(" @ %clientId @ ", \"" @ %lospos @ "\");", 0.5, %clientId);
		}
		
		if(!%success)
		{
			// Failure: Teleport to pack retrieval location (Colloseum entrance) first, then out after 1 minute
			if(!IsDead(%clientId))
			{
				%playerObj = Client::getOwnedObject(%clientId);
				if(%playerObj != -1)
					GameBase::setPosition(%playerObj, %lospos);
			}
			Client::sendMessage(%clientId, $MsgWhite, "You have one minute to retrieve your pack before you are teleported out.");
			schedule("SealBattle::TeleportOut(" @ %clientId @ ");", 60, %clientId);
		}
		else
		{
			// Success: Teleport out immediately
			if(IsDead(%clientId))
			{
				schedule("SealBattle::TeleportOutSuccess(" @ %clientId @ ", \"" @ %outpos @ "\");", 0.5, %clientId);
			}
			else
			{
				%playerObj = Client::getOwnedObject(%clientId);
				if(%playerObj != -1)
					GameBase::setPosition(%playerObj, %outpos);
				Client::sendMessage(%clientId, $MsgWhite, "The seal battle has ended. You have been teleported out.");
			}
		}
	}
	
	// Teleport dead participants back into Colloseum to retrieve packs (on both success and failure)
	// This handles participants who may have respawned elsewhere and aren't in Colloseum anymore
	if($SealBattleParticipants != "")
	{
		%colloseumEntrance = "-3588 -2364 354";
		
		// Track which participants we've already handled in the zone loop above
		%handledParticipants = "";
		
		// First, mark all participants currently in Colloseum as already handled
		if(%zoneDesc != "" && %zoneDesc != -1)
		{
			for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			{
				if(isRPGAI(%cl))
					continue;
				
				%clZoneId = fetchData(%cl, "zone");
				%clZoneDesc = Zone::getDesc(%clZoneId);
				if(%clZoneDesc == %zoneDesc)
				{
					// This participant is in Colloseum and was handled by the zone loop above
					%handledParticipants = AddToCommaList(%handledParticipants, %cl);
				}
			}
		}
		
		// Parse comma-separated list of all participants
		%list = $SealBattleParticipants;
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
			
			// Skip if invalid client ID
			if(%participantId == "" || %participantId == -1)
				continue;
			
			// Check if client still exists
			%clName = Client::getName(%participantId);
			if(%clName == "")
				continue;
			
			// Check if this participant was already handled by the zone loop
			%alreadyHandled = false;
			%handledList = %handledParticipants;
			while(String::len(%handledList) > 0)
			{
				%handledCommaPos = String::findSubStr(%handledList, ",");
				if(%handledCommaPos > 0)
				{
					%handledId = String::getSubStr(%handledList, 0, %handledCommaPos);
					%handledList = String::getSubStr(%handledList, %handledCommaPos + 1, 99999);
				}
				else
				{
					%handledId = %handledList;
					%handledList = "";
				}
				
				if(%handledId == %participantId)
				{
					%alreadyHandled = true;
					break;
				}
			}
			
			// Skip if already handled by zone loop
			if(%alreadyHandled)
				continue;
			
			// This participant wasn't in Colloseum (likely respawned elsewhere) - teleport them back
			// Clear lootbag flag
			storeData(%participantId, "noDropLootbagFlag", false);
			
			// CRITICAL: Clear any movement-related flags that might have been accidentally set on players
			// These flags should only be set on bots, but clear them from players as a safety measure
			storeData(%participantId, "frozen", "");
			storeData(%participantId, "ShovedByPlayer", "");
			$BotFrozen[%participantId] = "";
			$EnemyBotData[%participantId, "frozen"] = "";
			$ClientData[%participantId, "frozen"] = "";
			
			// Spawn if dead, then teleport
			if(IsDead(%participantId))
			{
				// Don't set lastzone to Colloseum to prevent air spawns
				storeData(%participantId, "lastzone", "");
				Game::playerSpawn(%participantId, True);
				// Schedule teleport after spawn completes
				schedule("SealBattle::TeleportParticipantBack(" @ %participantId @ ", \"" @ %colloseumEntrance @ "\");", 0.5, %participantId);
			}
			else
			{
				// Already alive, teleport immediately
				SealBattle::TeleportParticipantBack(%participantId, %colloseumEntrance);
			}
			
			// On failure, schedule teleport out after 1 minute (same as zone loop)
			if(!%success)
			{
				Client::sendMessage(%participantId, $MsgWhite, "You have one minute to retrieve your pack before you are teleported out.");
				schedule("SealBattle::TeleportOut(" @ %participantId @ ");", 60, %participantId);
			}
		}
	}
	
	$SealFighterDied = false;
	$SealBattleActive = false;
	
	// Clear seal battle zone and house tracking
	$SealBattleZone = "";
	$SealBattleHouse = "";
	$SealBattleParticipants = "";
	
	// Clear all round tracking flags
	$SealBattleRound1Spawned = false;  // Reset Round 1 spawn flag
	$SealBattleRound1Complete = false;  // Reset Round 1 completion flag
	$SealBattleRound2Complete = false;  // Reset Round 2 completion flag
	$SealBattleRound3Complete = false;  // Reset Round 3 completion flag
	$SealBattleCurrentRound = 0;  // Reset current round tracker
	$SealBattleLoopScheduled = false;  // Reset loop scheduled flag
	
	// Clear internal name tracking variables
	$SealBattle::FighterName = "";
	$SealBattle::MageName = "";
	$SealBattle::GuardianName = "";
}

function SealBattle::FixSpawnPosition(%clientId, %targetPos)
{
	// Fix spawn position for a player who just respawned during seal battle
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != -1)
	{
		GameBase::setPosition(%playerObj, %targetPos);
	}
}

function SealBattle::TeleportParticipantBack(%clientId, %targetPos)
{
	// Teleport a participant back into Colloseum for pack retrieval
	// Clear lootbag flag so they can pick up their pack
	storeData(%clientId, "noDropLootbagFlag", false);
	
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != -1)
	{
		GameBase::setPosition(%playerObj, %targetPos);
		Client::sendMessage(%clientId, $MsgWhite, "You have been teleported back to retrieve your pack. Use #recall or transport out when you are finished.");
	}
}

function SealBattle::TeleportOutSuccess(%clientId, %outpos)
{
	// Teleport player out after successful seal battle (called after respawn completes)
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != -1)
		GameBase::setPosition(%playerObj, %outpos);
	Client::sendMessage(%clientId, $MsgWhite, "The seal battle has ended. You have been teleported out.");
}

function SealBattle::TeleportOut(%clientId)
{
	// Teleport player out after 1 minute
	%outpos = "-335 -2339 65.5";
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj != -1)
		GameBase::setPosition(%playerObj, %outpos);
	Client::sendMessage(%clientId, $MsgWhite, "You have been teleported out.");
}

function SealBattle::SetupBot(%botName, %pos, %round)
{
	// CRITICAL: Try looking up by Internal Name first (Reliable - available immediately after spawn)
	// %botName can now be either an internal name (e.g., "RoundTwo497") or a display name (e.g., "SealFighter2")
	%aiId = AI::getClientIdFromName(%botName);
	if(%aiId == "") %aiId = -1; // Normalize empty string to -1
	
	// Fallback: Try looking up by Display Name (Legacy/Safety - for backwards compatibility)
	if(%aiId == -1 || %aiId == "")
		%aiId = NEWgetClientByName(%botName);
	
	// If still not found, retry after 1 second - bot may still be initializing
	if(%aiId == -1 || %aiId == "")
	{
		// Retry after 1 second - bot may still be initializing
		schedule("SealBattle::SetupBot(\"" @ %botName @ "\", \"" @ %pos @ "\", " @ %round @ ");", 1);
		echo("WARNING SealBattle::SetupBot: Bot " @ %botName @ " not found yet (tried internal name and display name), retrying in 1 second...");
		return;
	}
	
	// CRITICAL: Check if bot is fully initialized before proceeding
	// Bot must have BotInfoAiName set and HasLoadedAndSpawned flag set
	%hasLoaded = fetchData(%aiId, "HasLoadedAndSpawned");
	%aiName = fetchData(%aiId, "BotInfoAiName");
	if(%aiName == "" || %aiName == -1 || %aiName == "0")
	{
		// Fallback: Try direct array
		%aiName = $BotInfoAiName[%aiId];
	}
	
	// If bot isn't fully initialized yet, retry after 1 second
	if(%aiName == "" || %aiName == -1 || %aiName == "0" || %hasLoaded != "True" && %hasLoaded != "true" && %hasLoaded != "1")
	{
		schedule("SealBattle::SetupBot(\"" @ %botName @ "\", \"" @ %pos @ "\", " @ %round @ ");", 1);
		echo("WARNING SealBattle::SetupBot: Bot " @ %botName @ " (clientId=" @ %aiId @ ") not fully initialized yet (BotInfoAiName=" @ %aiName @ ", HasLoadedAndSpawned=" @ %hasLoaded @ "), retrying in 1 second...");
		return;
	}
	
	// Determine guardtype based on bot type (Fighter/Mage/Guardian) - all rounds use the same bot types
	// All SealFighter bots use RoundOne, all SealMage bots use RoundTwo, all SealGuardian bots use RoundThree
	// Stat scaling is handled separately based on the round parameter
	%guardtype = "";
	
	// CRITICAL: %botName is now an internal name (e.g., "RoundTwo497"), so extract guardtype from it directly
	// Internal names have the format "RoundOne497", "RoundTwo497", "RoundThree497", etc.
	if(%botName != "" && %botName != -1)
	{
		// Extract guardtype from internal name (e.g., "RoundTwo497" -> "RoundTwo")
		// Extract guardtype by removing trailing digits (more reliable than clipTrailingNumbers)
		// This works backwards from the end to find and remove only trailing digits
		%guardtype = %botName;
		%len = String::len(%botName);
		%numStr = "";
		%digitString = "0123456789";
		
		// Find trailing digits (working backwards)
		for(%i = %len - 1; %i >= 0; %i--)
		{
			%char = String::getSubStr(%botName, %i, 1);
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
			%guardtype = String::getSubStr(%botName, 0, %len - String::len(%numStr));
		}
	}
	
	// Fallback: Try to infer from AI name if internal name extraction failed
	if((%guardtype == "" || %guardtype == -1) && %aiName != "" && %aiName != -1 && %aiName != "0")
	{
		// Extract guardtype from AI name (e.g., "RoundOne3" -> "RoundOne")
		// Extract guardtype by removing trailing digits (more reliable than clipTrailingNumbers)
		// This works backwards from the end to find and remove only trailing digits
		%guardtype = %aiName;
		%len = String::len(%aiName);
		%numStr = "";
		%digitString = "0123456789";
		
		// Find trailing digits (working backwards)
		for(%i = %len - 1; %i >= 0; %i--)
		{
			%char = String::getSubStr(%aiName, %i, 1);
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
			%guardtype = String::getSubStr(%aiName, 0, %len - String::len(%numStr));
		}
	}
	
	// Legacy fallback: Try display name pattern matching (for edge cases where extraction failed)
	// NOTE: With backwards digit removal working correctly, this should rarely be needed
	// However, it's kept as a safety net for edge cases (e.g., bot names without trailing digits)
	if((%guardtype == "" || %guardtype == -1))
	{
		// Try to get display name from the bot
		%displayName = Client::getName(%aiId);
		if(%displayName != "" && %displayName != -1)
		{
			if(String::findSubStr(%displayName, "Fighter") != -1)
				%guardtype = "RoundOne";  // Fighter always uses RoundOne
			else if(String::findSubStr(%displayName, "Mage") != -1)
				%guardtype = "RoundTwo";  // Mage always uses RoundTwo
			else if(String::findSubStr(%displayName, "Guardian") != -1)
				%guardtype = "RoundThree";  // Guardian always uses RoundThree
		}
	}
	
	// Final fallback: if still no guardtype, log warning
	// This should be extremely rare now that extraction works correctly
	if(%guardtype == "" || %guardtype == -1)
	{
		echo("WARNING: SealBattle::SetupBot - Could not determine guardtype for " @ %botName @ " (clientId=" @ %aiId @ ", aiName=" @ %aiName @ ") - extraction failed and display name fallback also failed");
		%guardtype = "RoundOne";  // Default to RoundOne as final fallback
	}
	
	// CRITICAL: Clear any stale zone data from this client ID
	storeData(%aiId, "zone", "");
	storeData(%aiId, "tmpzone", "");
	
	// Set bot behavior flags (Seal Battle-specific flags will be set at end of function)
	storeData(%aiId, "noDropLootbagFlag", True);
	storeData(%aiId, "SealBattleBot", true);  // Flag this bot as a seal battle bot
	
	// CRITICAL: Set the correct race and armor for seal battle bots
	storeData(%aiId, "RACE", "Seals");
	Player::setArmor(%aiId, "SealsArmor");
	
	// CRITICAL: Ensure bot has proper base stats and weapons before scaling
	%hasSkills = false;
	%ns = getNumSkills();
	for(%i = 1; %i <= %ns && !%hasSkills; %i++)
	{
		if($PlayerSkill[%aiId, %i] != "" && $PlayerSkill[%aiId, %i] > 0)
			%hasSkills = true;
	}
	
	// Check if bot already has equipment from AI::setWeapons()
	%hasWeapon = false;
	%weapon = Player::getMountedItem(%aiId, $WeaponSlot);
	if(%weapon != -1 && %weapon != "")
		%hasWeapon = true;
	
	// Get equipment string from bot's spawn configuration
	%equipString = "";
	if(%aiName != "" && %aiName != -1 && %aiName != "0")
	{
		// Try BotInfo with AI name first
		%items = $BotInfo[%aiName, ITEMS];
		if(%items != "")
		{
			%equipString = %items;
		}
		else
		{
			// Try BotEquipment with AI name
			%equipString = $BotEquipment[%aiName];
		}
	}
	
	// If we didn't find equipment with AI name, try guardtype
	if((%equipString == "" || %equipString == -1) && %guardtype != "")
	{
		// Try BotInfo with guardtype
		%equipString = $BotInfo[%guardtype, ITEMS];
		// If not found, try BotEquipment
		if(%equipString == "" || %equipString == -1)
			%equipString = $BotEquipment[%guardtype];
	}
	
	// Give equipment directly if we found it and bot doesn't have a weapon
	if(%equipString != "" && %equipString != -1)
	{
		if(!%hasWeapon)
		{
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Giving equipment to " @ %botName @ " (aiId=" @ %aiId @ ", aiName=" @ %aiName @ ", guardtype=" @ %guardtype @ ", round=" @ %round @ ")");
			// CRITICAL: Set auto-equip flags BEFORE giving equipment
			storeData(%aiId, "AutoEquipArmor", "true");
			storeData(%aiId, "AutoEquipShield", "true");
			storeData(%aiId, "AutoEquipAccessories", "true");
			// Store original equipment string for lootbag generation
			storeData(%aiId, "OriginalLootString", %equipString);
			// Give equipment directly
			GiveThisStuff(%aiId, %equipString, False);
		}
	}
	else
	{
		echo("ERROR: SealBattle::SetupBot(): Could not find equipment string for " @ %botName @ " (aiId=" @ %aiId @ ", aiName=" @ %aiName @ ", guardtype=" @ %guardtype @ ")");
	}
	
	// CRITICAL: Wait for HardcodeAIskills() to complete (it's called by AI::setWeapons in the spawn flow)
	// If bot doesn't have skills set yet, retry SetupBot after a delay
	// This ensures we're working with the correct base stats from the spawn configuration
	if(!%hasSkills)
	{
		// Skills haven't been set yet by AI::setWeapons -> HardcodeAIskills
		// Retry SetupBot after 2 seconds to allow HardcodeAIskills to complete
		schedule("SealBattle::SetupBot(\"" @ %botName @ "\", \"" @ %pos @ "\", " @ %round @ ");", 2);
		echo("WARNING SealBattle::SetupBot: Bot " @ %botName @ " (clientId=" @ %aiId @ ") skills not set yet, waiting for HardcodeAIskills to complete, retrying in 2 seconds...");
		return;
	}
	
	// CRITICAL: Check if stats have already been scaled for this round
	// If they have, we need to use the original base values, not the scaled ones
	%alreadyScaled = fetchData(%aiId, "SealBattleScaledRound");
	if(%alreadyScaled == %round)
	{
		// Stats were already scaled for this round - don't scale again
		echo("WARNING SealBattle::SetupBot: Bot " @ %botName @ " (clientId=" @ %aiId @ ") stats were already scaled for round " @ %round @ ", skipping scaling");
		// Still need to freeze and set invulnerability
		// (freeze logic continues below)
	}
	else
	{
		// CRITICAL: Store original base values BEFORE any scaling
		// This ensures we always scale from the original spawn configuration, not from previously scaled values
		%originalLVL = fetchData(%aiId, "LVL");
		if(%originalLVL == "" || %originalLVL == -1 || %originalLVL == 0)
			%originalLVL = 1;
		
		// Check if we have stored original values from a previous round
		%storedOriginalLVL = fetchData(%aiId, "SealBattleOriginalLVL");
		if(%storedOriginalLVL == "" || %storedOriginalLVL == -1 || %storedOriginalLVL == 0)
		{
			// Store original values for future reference
			// CRITICAL: Store in all data arrays for consistency
			storeData(%aiId, "SealBattleOriginalLVL", %originalLVL);
			$EnemyBotData[%aiId, "SealBattleOriginalLVL"] = %originalLVL;
			$ClientData[%aiId, "SealBattleOriginalLVL"] = %originalLVL;
			%storedOriginalLVL = %originalLVL;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Stored original LVL: " @ %originalLVL);
		}
		else
		{
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Found stored original LVL: " @ %storedOriginalLVL);
		}
		
		// CRITICAL: Restore original LVL before scaling (in case it was scaled in a previous round)
		// This ensures we always scale from the original spawn configuration, not from previously scaled values
		storeData(%aiId, "LVL", %storedOriginalLVL);
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Restored LVL to original: " @ %storedOriginalLVL);
		
		// Do the same for other base values that might have been scaled
		%originalRemortStep = fetchData(%aiId, "RemortStep");
		%storedOriginalRemortStep = fetchData(%aiId, "SealBattleOriginalRemortStep");
		if(%storedOriginalRemortStep == "" || %storedOriginalRemortStep == -1)
		{
			// CRITICAL: Store in all data arrays for consistency
			storeData(%aiId, "SealBattleOriginalRemortStep", %originalRemortStep);
			$EnemyBotData[%aiId, "SealBattleOriginalRemortStep"] = %originalRemortStep;
			$ClientData[%aiId, "SealBattleOriginalRemortStep"] = %originalRemortStep;
			%storedOriginalRemortStep = %originalRemortStep;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Stored original RemortStep: " @ %originalRemortStep);
		}
		else
		{
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Found stored original RemortStep: " @ %storedOriginalRemortStep);
		}
		
		// CRITICAL: Restore original RemortStep before scaling (in case it was scaled in a previous round)
		// This ensures we always scale from the original spawn configuration, not from previously scaled values
		storeData(%aiId, "RemortStep", %storedOriginalRemortStep);
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Restored RemortStep to original: " @ %storedOriginalRemortStep);
		
		// CRITICAL: Restore original skills BEFORE checking current values
		// This ensures we always work with true originals, not previously scaled values
		%storedOriginalEndurance = fetchData(%aiId, "SealBattleOriginalEndurance");
		if(%storedOriginalEndurance == "" || %storedOriginalEndurance == -1)
		{
			// Original not stored yet - get current value (should be from HardcodeAIskills, not scaled)
			%currentEndurance = $PlayerSkill[%aiId, $SkillEndurance];
			// CRITICAL: Store as original BEFORE any scaling happens
			storeData(%aiId, "SealBattleOriginalEndurance", %currentEndurance);
			$EnemyBotData[%aiId, "SealBattleOriginalEndurance"] = %currentEndurance;
			$ClientData[%aiId, "SealBattleOriginalEndurance"] = %currentEndurance;
			%storedOriginalEndurance = %currentEndurance;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Stored original Endurance: " @ %currentEndurance);
		}
		else
		{
			// Original exists - restore it FIRST before any calculations
			$PlayerSkill[%aiId, $SkillEndurance] = %storedOriginalEndurance;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Restored original Endurance: " @ %storedOriginalEndurance);
		}
		
		%storedOriginalEnergy = fetchData(%aiId, "SealBattleOriginalEnergy");
		if(%storedOriginalEnergy == "" || %storedOriginalEnergy == -1)
		{
			// Original not stored yet - get current value (should be from HardcodeAIskills, not scaled)
			%currentEnergy = $PlayerSkill[%aiId, $SkillEnergy];
			// CRITICAL: Store as original BEFORE any scaling happens
			storeData(%aiId, "SealBattleOriginalEnergy", %currentEnergy);
			$EnemyBotData[%aiId, "SealBattleOriginalEnergy"] = %currentEnergy;
			$ClientData[%aiId, "SealBattleOriginalEnergy"] = %currentEnergy;
			%storedOriginalEnergy = %currentEnergy;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Stored original Energy: " @ %currentEnergy);
		}
		else
		{
			// Original exists - restore it FIRST before any calculations
			$PlayerSkill[%aiId, $SkillEnergy] = %storedOriginalEnergy;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Restored original Energy: " @ %storedOriginalEnergy);
		}
		
		%storedOriginalWeightCapacity = fetchData(%aiId, "SealBattleOriginalWeightCapacity");
		if(%storedOriginalWeightCapacity == "" || %storedOriginalWeightCapacity == -1)
		{
			// Original not stored yet - get current value (should be from HardcodeAIskills, not scaled)
			%currentWeightCapacity = $PlayerSkill[%aiId, $SkillWeightCapacity];
			// CRITICAL: Store as original BEFORE any scaling happens
			storeData(%aiId, "SealBattleOriginalWeightCapacity", %currentWeightCapacity);
			$EnemyBotData[%aiId, "SealBattleOriginalWeightCapacity"] = %currentWeightCapacity;
			$ClientData[%aiId, "SealBattleOriginalWeightCapacity"] = %currentWeightCapacity;
			%storedOriginalWeightCapacity = %currentWeightCapacity;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Stored original WeightCapacity: " @ %currentWeightCapacity);
		}
		else
		{
			// Original exists - restore it FIRST before any calculations
			$PlayerSkill[%aiId, $SkillWeightCapacity] = %storedOriginalWeightCapacity;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Restored original WeightCapacity: " @ %storedOriginalWeightCapacity);
		}
		
		// CRITICAL: Now call RefreshAll() to calculate the bot's base stats from its spawn configuration
		// Do this BEFORE freezing so RefreshAll doesn't interfere with freeze state
		RefreshAll(%aiId);
		
		// Get the bot's calculated base stats
		%baseMaxHP = fetchData(%aiId, "MaxHP");
		%baseMaxMANA = fetchData(%aiId, "MaxMANA");
		%baseHP = fetchData(%aiId, "HP");
		%baseMANA = fetchData(%aiId, "MANA");
		%baseDEF = fetchData(%aiId, "DEF");
		%baseATK = fetchData(%aiId, "ATK");
		%baseMDEF = fetchData(%aiId, "MDEF");
		%baseDMG = fetchData(%aiId, "DMG");
		%baseLCK = fetchData(%aiId, "LCK");
		if(%baseLCK == "" || %baseLCK == -1)
			%baseLCK = 0;
		
		// Get the round-specific multiplier
		%mult = SealBattle::GetRoundMultiplier(%round);
		// Calculate round multiplier for debug message (TorqueScript doesn't support ternary operators)
		if(%round == 1)
			%roundMult = $SealBattleRound1Multiplier;
		else if(%round == 2)
			%roundMult = $SealBattleRound2Multiplier;
		else
			%roundMult = $SealBattleRound3Multiplier;
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Using multiplier: " @ %mult @ " (baseMult=" @ SealBattle::GetBaseStrengthMultiplier() @ ", roundMult=" @ %roundMult @ ")");
		
		// CRITICAL: Scale weapon damage BEFORE calculating/scaling ATK
		// This ensures ATK is calculated from scaled weapon damage, not unscaled
		// Check bot type using guardtype (determined from AI name) since %botName is now an internal name
		%isFighterOrGuardian = (String::findSubStr(%guardtype, "RoundOne") != -1 || String::findSubStr(%guardtype, "RoundThree") != -1);
		if(%isFighterOrGuardian)
		{
			%weapon = Player::getMountedItem(%aiId, $WeaponSlot);
			if(%weapon != -1 && %weapon != "")
			{
				// Get the weapon name (crop any trailing numbers)
				%weaponName = getCroppedItem(%weapon);
				
				// Get the base weapon damage from $AccessoryVar
				%baseWeaponDamage = GetAccessoryVar(%weaponName, $SpecialVar);
				
				if(%baseWeaponDamage != "" && %baseWeaponDamage != -1)
				{
					// Parse the damage range (format: "min-max" or just a number)
					%dashPos = String::findSubStr(%baseWeaponDamage, "-");
					if(%dashPos != -1)
					{
						// Damage range format: "min-max"
						%baseMin = String::getSubStr(%baseWeaponDamage, 0, %dashPos);
						%baseMax = String::getSubStr(%baseWeaponDamage, %dashPos + 1, 99999);
						
						// Scale both min and max by the round multiplier
						%scaledMin = floor(%baseMin * %mult);
						%scaledMax = floor(%baseMax * %mult);
						
						// Ensure minimum values
						if(%scaledMin < 1) %scaledMin = 1;
						if(%scaledMax < %scaledMin) %scaledMax = %scaledMin;
						
						%scaledWeaponDamage = %scaledMin @ "-" @ %scaledMax;
					}
					else
					{
						// Single number format
						%baseDamage = %baseWeaponDamage + 0;  // Convert to number
						if(%baseDamage > 0)
						{
							%scaledDamage = floor(%baseDamage * %mult);
							if(%scaledDamage < 1) %scaledDamage = 1;
							%scaledWeaponDamage = %scaledDamage;
						}
						else
						{
							%scaledWeaponDamage = %baseWeaponDamage;  // Keep original if invalid
						}
					}
					
					// CRITICAL: Store the scaled damage in a bot-specific array
					// This allows GetAccessoryVar to check if this weapon belongs to a seal battle bot
					// and return the scaled damage instead of the base damage
					$SealBattleWeaponDamage[%aiId, %weaponName] = %scaledWeaponDamage;
					
					// Also create a reverse lookup: weapon name -> client ID for faster lookup
					// This allows GetAccessoryVar to quickly find which bot owns this weapon
					$SealBattleWeaponOwner[%weaponName] = %aiId;
					
					echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled weapon damage for " @ %botName @ " (weapon: " @ %weaponName @ ")");
					echo("  Base damage: " @ %baseWeaponDamage @ " -> Scaled damage: " @ %scaledWeaponDamage @ " (multiplier: " @ %mult @ ")");
				}
			}
			else
			{
				echo("[SEAL BATTLE] SealBattle::SetupBot(): WARNING - Bot " @ %botName @ " (guardtype: " @ %guardtype @ ") has no weapon equipped, skipping weapon damage scaling");
			}
		}
		
		// CRITICAL: Recalculate ATK from scaled weapon damage
		// RefreshAll() will recalculate ATK using the scaled weapon damage via GetAccessoryVar override
		RefreshAll(%aiId);
		
		// Fetch the new baseATK (now includes scaled weapon damage)
		%baseATK = fetchData(%aiId, "ATK");
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Recalculated baseATK from scaled weapon: " @ %baseATK);
		
		// Scale underlying values that affect stat calculations (RefreshAll will recalculate MaxHP/MaxMANA/MaxWeight from these)
		// CRITICAL: Use the stored original values directly to ensure we scale from true originals, not previously scaled values
		
		// 1. Scale LVL (level) - directly adds to MaxHP
		// Use the restored original LVL value (storedOriginalLVL) instead of fetching again
		%baseLVL = %storedOriginalLVL;  // Use the restored original value
		if(%baseLVL == "" || %baseLVL == -1 || %baseLVL == 0)
			%baseLVL = 1;
		%scaledLVL = floor(%baseLVL * %mult);
		storeData(%aiId, "LVL", %scaledLVL);
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled LVL from " @ %baseLVL @ " to " @ %scaledLVL @ " (multiplier: " @ %mult @ ")");
		
		// 2. Scale RemortStep - affects HP, MANA, DEF, MDEF, ATK, MaxWeight
		// CRITICAL: Use the restored original RemortStep value (storedOriginalRemortStep) instead of fetching again
		// This ensures we scale from the true original value, not from a previously scaled value
		%baseRemortStep = %storedOriginalRemortStep;  // Use the restored original value
		if(%baseRemortStep != "" && %baseRemortStep != -1 && %baseRemortStep != 0)
		{
			%scaledRemortStep = floor(%baseRemortStep * %mult);
			storeData(%aiId, "RemortStep", %scaledRemortStep);
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled RemortStep from " @ %baseRemortStep @ " to " @ %scaledRemortStep @ " (multiplier: " @ %mult @ ")");
		}
		
		// 3. Scale Endurance skill - affects MaxHP calculation
		// CRITICAL: Use the restored original Endurance value (storedOriginalEndurance) instead of fetching from $PlayerSkill
		// This ensures we scale from the true original value, not from a previously scaled value
		%baseEndurance = %storedOriginalEndurance;  // Use the restored original value
		if(%baseEndurance != "" && %baseEndurance != -1 && %baseEndurance != 0)
		{
			%scaledEndurance = floor(%baseEndurance * %mult);
			$PlayerSkill[%aiId, $SkillEndurance] = %scaledEndurance;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled Endurance skill from " @ %baseEndurance @ " to " @ %scaledEndurance @ " (multiplier: " @ %mult @ ")");
		}
		
		// 4. Scale Energy skill - affects MaxMANA calculation
		// CRITICAL: Use the restored original Energy value (storedOriginalEnergy) instead of fetching from $PlayerSkill
		// This ensures we scale from the true original value, not from a previously scaled value
		%baseEnergy = %storedOriginalEnergy;  // Use the restored original value
		if(%baseEnergy != "" && %baseEnergy != -1 && %baseEnergy != 0)
		{
			%scaledEnergy = floor(%baseEnergy * %mult);
			$PlayerSkill[%aiId, $SkillEnergy] = %scaledEnergy;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled Energy skill from " @ %baseEnergy @ " to " @ %scaledEnergy @ " (multiplier: " @ %mult @ ")");
		}
		
		// 5. Scale WeightCapacity skill - affects MaxWeight calculation
		// CRITICAL: Use the restored original WeightCapacity value (storedOriginalWeightCapacity) instead of fetching from $PlayerSkill
		// This ensures we scale from the true original value, not from a previously scaled value
		%baseWeightCapacity = %storedOriginalWeightCapacity;  // Use the restored original value
		if(%baseWeightCapacity != "" && %baseWeightCapacity != -1 && %baseWeightCapacity != 0)
		{
			%scaledWeightCapacity = floor(%baseWeightCapacity * %mult);
			$PlayerSkill[%aiId, $SkillWeightCapacity] = %scaledWeightCapacity;
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled WeightCapacity skill from " @ %baseWeightCapacity @ " to " @ %scaledWeightCapacity @ " (multiplier: " @ %mult @ ")");
		}
		
		// Call RefreshAll() to recalculate MaxHP, MaxMANA, MaxWeight from scaled underlying values
		// CRITICAL: This also recalculates ATK with scaled weapon damage AND scaled RemortStep
		RefreshAll(%aiId);
		
		// CRITICAL: Fetch baseATK again after RefreshAll() - it now includes scaled weapon damage AND scaled RemortStep
		%baseATK = fetchData(%aiId, "ATK");
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Final baseATK (with scaled weapon + scaled RemortStep): " @ %baseATK);
		
		// CRITICAL: After RefreshAll(), get the calculated MaxHP and MaxMANA (should be scaled from underlying values)
		// Store these in the special array so fetchData() can return them directly for seal battle bots
		%maxHP = fetchData(%aiId, "MaxHP");
		%maxMANA = fetchData(%aiId, "MaxMANA");
		
		// CRITICAL: Store scaled MaxHP and MaxMANA in special array (storeData() blocks MaxHP/MaxMANA)
		// This ensures fetchData() returns the scaled values instead of recalculating from potentially reset underlying values
		// CRITICAL: Store FIRST, then call setHP/setMANA so they use the stored scaled values
		if(%maxHP != "" && %maxHP != -1 && %maxHP > 0)
		{
			$SealBattleScaledStats[%aiId, "MaxHP"] = %maxHP;
			// CRITICAL: Call setHP() AFTER storing MaxHP so it uses the stored scaled value
			// setHP() calculates damage level from HP/MaxHP ratio, so MaxHP must be correct
			setHP(%aiId, %maxHP);
			storeData(%aiId, "HP", %maxHP);
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Stored scaled MaxHP: " @ %maxHP @ ", Set HP to MaxHP via setHP()");
		}
		if(%maxMANA != "" && %maxMANA != -1 && %maxMANA > 0)
		{
			$SealBattleScaledStats[%aiId, "MaxMANA"] = %maxMANA;
			// CRITICAL: Call setMANA() AFTER storing MaxMANA so it uses the stored scaled value
			// setMANA() calculates energy level from MANA/MaxMANA ratio, so MaxMANA must be correct
			setMANA(%aiId, %maxMANA);
			storeData(%aiId, "MANA", %maxMANA);
			echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Stored scaled MaxMANA: " @ %maxMANA @ ", Set MANA to MaxMANA via setMANA()");
		}
		
		// Scale DEF and MDEF directly (they're calculated by RefreshAll, but we override them)
		%scaledDEF = floor(%baseDEF * %mult);
		%scaledMDEF = floor(%baseMDEF * %mult);
		storeData(%aiId, "DEF", %scaledDEF);
		$EnemyBotData[%aiId, "DEF"] = %scaledDEF;
		$ClientData[%aiId, "DEF"] = %scaledDEF;
		storeData(%aiId, "MDEF", %scaledMDEF);
		$EnemyBotData[%aiId, "MDEF"] = %scaledMDEF;
		$ClientData[%aiId, "MDEF"] = %scaledMDEF;
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled DEF from " @ %baseDEF @ " to " @ %scaledDEF @ " (multiplier: " @ %mult @ ")");
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled MDEF from " @ %baseMDEF @ " to " @ %scaledMDEF @ " (multiplier: " @ %mult @ ")");
		
		// Scale ATK and DMG directly
		// CRITICAL: Apply 50% additional ATK boost for seal battle bots
		%scaledATK = floor(%baseATK * %mult * 1.5);
		storeData(%aiId, "ATK", %scaledATK);
		$EnemyBotData[%aiId, "ATK"] = %scaledATK;
		$ClientData[%aiId, "ATK"] = %scaledATK;
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled ATK from " @ %baseATK @ " to " @ %scaledATK @ " (multiplier: " @ %mult @ ")");
		
		// Handle DMG - if baseDMG is empty or invalid, set a minimum value based on ATK
		if(%baseDMG == "" || %baseDMG == -1 || %baseDMG == 0)
		{
			if(%baseATK > 0)
				%baseDMG = floor(%baseATK * 0.1);
			else
				%baseDMG = 1;
		}
		%scaledDMG = floor(%baseDMG * %mult);
		if(%scaledDMG < 1)
			%scaledDMG = 1;
		storeData(%aiId, "DMG", %scaledDMG);
		$EnemyBotData[%aiId, "DMG"] = %scaledDMG;
		$ClientData[%aiId, "DMG"] = %scaledDMG;
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Scaled DMG from " @ %baseDMG @ " to " @ %scaledDMG @ " (multiplier: " @ %mult @ ")");
		
		// CRITICAL: Call RefreshAll() one more time AFTER setting scaled stats to ensure they persist
		// This ensures any subsequent RefreshAll() calls don't overwrite our scaled values
		RefreshAll(%aiId);
		
		// Re-apply scaled stats after RefreshAll() to ensure they stick
		storeData(%aiId, "DEF", %scaledDEF);
		$EnemyBotData[%aiId, "DEF"] = %scaledDEF;
		$ClientData[%aiId, "DEF"] = %scaledDEF;
		storeData(%aiId, "MDEF", %scaledMDEF);
		$EnemyBotData[%aiId, "MDEF"] = %scaledMDEF;
		$ClientData[%aiId, "MDEF"] = %scaledMDEF;
		storeData(%aiId, "ATK", %scaledATK);
		$EnemyBotData[%aiId, "ATK"] = %scaledATK;
		$ClientData[%aiId, "ATK"] = %scaledATK;
		storeData(%aiId, "DMG", %scaledDMG);
		$EnemyBotData[%aiId, "DMG"] = %scaledDMG;
		$ClientData[%aiId, "DMG"] = %scaledDMG;
		
		// CRITICAL: Store scaled stats in global array BEFORE HardcodeAIskills() runs
		// HardcodeAIskills() calls RefreshAllEnemyBot() which will overwrite these values
		// We store the calculated scaled values directly, not the fetched values
		// NOTE: MaxHP and MaxMANA are already stored above after RefreshAll() recalculates them
		$SealBattleScaledStats[%aiId, "DEF"] = %scaledDEF;
		$SealBattleScaledStats[%aiId, "MDEF"] = %scaledMDEF;
		$SealBattleScaledStats[%aiId, "ATK"] = %scaledATK;
		$SealBattleScaledStats[%aiId, "DMG"] = %scaledDMG;
		$SealBattleScaledStats[%aiId, "round"] = %round;
		%storedMaxHP = $SealBattleScaledStats[%aiId, "MaxHP"];
		%storedMaxMANA = $SealBattleScaledStats[%aiId, "MaxMANA"];
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Stored scaled stats in global array for ReapplyScaledStats (clientId=" @ %aiId @ ", round=" @ %round @ ")");
		echo("  DEF: " @ %scaledDEF @ ", MDEF: " @ %scaledMDEF @ ", ATK: " @ %scaledATK @ ", DMG: " @ %scaledDMG);
		if(%storedMaxHP != "" && %storedMaxHP != -1)
			echo("  MaxHP: " @ %storedMaxHP);
		if(%storedMaxMANA != "" && %storedMaxMANA != -1)
			echo("  MaxMANA: " @ %storedMaxMANA);
		
		// Mark that stats have been scaled for this round
		storeData(%aiId, "SealBattleScaledRound", %round);
	}
	
	// CRITICAL: Freeze bot and set invulnerability AFTER all RefreshAll() calls are complete
	// This ensures the bot is fully initialized and stats are calculated before being frozen
	// NOTE: Freeze flag may have already been set in HardcodeAIskills, but we set it again here to ensure it's active
	storeData(%aiId, "frozen", "true");
	$BotFrozen[%aiId] = "true";
	// Also set in EnemyBotData array for consistency
	$EnemyBotData[%aiId, "frozen"] = "true";
	
	// CRITICAL: Set no-damage flag (SpawnInvuln) in all necessary places
	// This prevents bots from taking damage until unfreeze is called
	// NOTE: HardcodeAIskills does NOT clear SpawnInvuln for seal battle bots, so this will persist
	storeData(%aiId, "SpawnInvuln", "true");
	$EnemyBotData[%aiId, "SpawnInvuln"] = "true";
	$ClientData[%aiId, "SpawnInvuln"] = "true";
	// Name-based guard for bots that haven't registered yet (15 second timeout, but unfreeze will clear it)
	$SpawnInvulnByName[%botName] = getSimTime();
	echo("[SEAL BATTLE] SealBattle::SetupBot(): Set freeze and SpawnInvuln (no-damage) flags for bot " @ %botName @ " (clientId=" @ %aiId @ ")");
	
	// CRITICAL: Ensure noDropLootbagFlag is set for seal battle bots (it may have been cleared by SpawnAIGetClientId)
	// Seal battle bots should NEVER drop lootbags
	storeData(%aiId, "noDropLootbagFlag", "true");
	$EnemyBotData[%aiId, "noDropLootbagFlag"] = "true";
	$ClientData[%aiId, "noDropLootbagFlag"] = "true";
	echo("[SEAL BATTLE] SealBattle::SetupBot(): Set noDropLootbagFlag for bot " @ %botName @ " (clientId=" @ %aiId @ ")");
	
	// CRITICAL: For Player objects, we need to use AI::setVar to actually freeze the bot
	// This disables AI seeking, targeting, and movement directives
	%aiName = fetchData(%aiId, "BotInfoAiName");
	if(%aiName == "" || %aiName == -1 || %aiName == "0")
		%aiName = $BotInfoAiName[%aiId];
	if(%aiName != "" && %aiName != -1 && %aiName != "0")
	{
		// Disable AI seeking (prevents movement)
		AI::setVar(%aiName, seekOff, 1);
		// Set spot distance to 0 (prevents targeting)
		AI::setVar(%aiName, SpotDist, 0);
		// Remove all active movement directives
		AI::newDirectiveRemove(%aiName, 99);
		// Clear any attack loops
		storeData(%aiId, "BotAttackLoopActive", "");
		storeData(%aiId, "AITarget", "");
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Set AI vars to freeze bot " @ %botName @ " (aiName=" @ %aiName @ ", clientId=" @ %aiId @ ")");
	}
	
	echo("[SEAL BATTLE] SealBattle::SetupBot(): Frozen and made invulnerable bot " @ %botName @ " (clientId=" @ %aiId @ ", round=" @ %round @ ") AFTER all initialization");
	
	// Scale LCK (Luck) - 5 LCK points per player in the Colloseum (after the initial 30 second wait timer)
	// Count players currently in the Colloseum zone
	%playerCount = 0;
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		// Skip AI bots
		if(isRPGAI(%cl))
			continue;
		
		// Check if player is in the Colloseum zone
		%clZoneId = fetchData(%cl, "zone");
		%clZoneDesc = Zone::getDesc(%clZoneId);
		if(%clZoneDesc == "Colloseum")
		{
			// Count this player (alive or dead, they're still in the battle)
			%playerCount++;
		}
	}
	
	// Set LCK to 5 * player count
	// CRITICAL: LCK is NOT scaled by round multiplier - it's based on player count only
	// This ensures all rounds have the same LCK based on difficulty (player count)
	%scaledLCK = 5 * %playerCount;
	if(%scaledLCK < 0)
		%scaledLCK = 0;  // LCK can't be negative
	storeData(%aiId, "LCK", %scaledLCK);
	// Also store in EnemyBotData and ClientData arrays for consistency
	$EnemyBotData[%aiId, "LCK"] = %scaledLCK;
	$ClientData[%aiId, "LCK"] = %scaledLCK;
	echo("[SEAL BATTLE] SealBattle::SetupBot(): Round " @ %round @ " - Set LCK to " @ %scaledLCK @ " (5 LCK per player, " @ %playerCount @ " players in Colloseum)");
	
	// NOTE: Scaled stats are already stored in global array during the scaling block above
	// We don't need to store them again here
	
	// CRITICAL: After all stat updates, explicitly set HP and MANA to their maximum values AGAIN
	// This is necessary because HardcodeAIskills() calls RefreshAll() which may have reset HP/MANA
	// CRITICAL: Use the stored scaled MaxHP/MaxMANA from $SealBattleScaledStats, not fetchData()
	// This ensures we use the correct scaled values even if RefreshAll() recalculated MaxHP incorrectly
	%finalMaxHP = $SealBattleScaledStats[%aiId, "MaxHP"];
	if(%finalMaxHP == "" || %finalMaxHP == -1 || %finalMaxHP == "0" || %finalMaxHP == 0)
	{
		// Fallback to fetchData if stored value not available
		%finalMaxHP = fetchData(%aiId, "MaxHP");
	}
	%finalMaxMANA = $SealBattleScaledStats[%aiId, "MaxMANA"];
	if(%finalMaxMANA == "" || %finalMaxMANA == -1 || %finalMaxMANA == "0" || %finalMaxMANA == 0)
	{
		// Fallback to fetchData if stored value not available
		%finalMaxMANA = fetchData(%aiId, "MaxMANA");
	}
	
	// CRITICAL: Use setHP() and setMANA() to properly set the damage/energy levels
	// These functions calculate the damage/energy level from HP/MaxHP ratio
	// This ensures the visual HP bar matches the actual HP value
	if(%finalMaxHP != "" && %finalMaxHP != -1 && %finalMaxHP > 0)
	{
		// setHP() will use fetchData("MaxHP") which should return our stored scaled value
		// But to be safe, we also store HP directly (though it's calculated dynamically)
		setHP(%aiId, %finalMaxHP);
		storeData(%aiId, "HP", %finalMaxHP);
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Final HP set to MaxHP: " @ %finalMaxHP @ " (stored scaled value)");
	}
	if(%finalMaxMANA != "" && %finalMaxMANA != -1 && %finalMaxMANA > 0)
	{
		// setMANA() will use fetchData("MaxMANA") which should return our stored scaled value
		// But to be safe, we also store MANA directly (though it's calculated dynamically)
		setMANA(%aiId, %finalMaxMANA);
		storeData(%aiId, "MANA", %finalMaxMANA);
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Final MANA set to MaxMANA: " @ %finalMaxMANA @ " (stored scaled value)");
	}
	
	// CRITICAL: Schedule re-application of scaled stats after HardcodeAIskills() completes
	// HardcodeAIskills() is called from AI::setWeapons() which runs after SetupBot, and it calls RefreshAllEnemyBot()
	// which recalculates DEF, MDEF, ATK, DMG from base values, overwriting our scaled stats
	// We schedule this with a delay to ensure HardcodeAIskills() has completed
	// Check if we have stored stats (only schedule if we just scaled, not if already scaled)
	%checkStoredDEF = $SealBattleScaledStats[%aiId, "DEF"];
	if(%checkStoredDEF != "" && %checkStoredDEF != -1)
	{
		echo("[SEAL BATTLE] SealBattle::SetupBot(): Scheduling ReapplyScaledStats for clientId=" @ %aiId @ " in 3 seconds");
		schedule("SealBattle::ReapplyScaledStats(" @ %aiId @ ", " @ %round @ ");", 3);
	}
	else
	{
		echo("[SEAL BATTLE] SealBattle::SetupBot(): WARNING - No stored scaled stats found for clientId=" @ %aiId @ ", cannot schedule ReapplyScaledStats");
	}
	
	// IMPORTANT: After all stat updates, directly set damage level to 0.0 (full health)
	// This ensures the visual health bar matches the actual HP
	// NOTE: We do NOT call RefreshAll() again here because DEF and MDEF are calculated by RefreshAll()
	// and we've already overridden them with scaled values. Calling RefreshAll() again would recalculate
	// DEF and MDEF and overwrite our scaled values.
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj != -1)
	{
		GameBase::setDamageLevel(%playerObj, 0.0);
	}
	
	// Display comprehensive stats for balancing
	%def = fetchData(%aiId, "DEF");
	%mdef = fetchData(%aiId, "MDEF");
	%atk = fetchData(%aiId, "ATK");
	%maxHP = fetchData(%aiId, "MaxHP");
	%hp = fetchData(%aiId, "HP");
	%maxMANA = fetchData(%aiId, "MaxMANA");
	%mana = fetchData(%aiId, "MANA");
	%maxWeight = fetchData(%aiId, "MaxWeight");
	%weight = fetchData(%aiId, "Weight");
	// CRITICAL: For seal battle bots, use fetchData("LCK") directly instead of AddPoints()
	// AddPoints() gets LCK from items, but seal battle bots have their LCK set directly by SetupBot()
	%isSealBattleBot = fetchData(%aiId, "SealBattleBot");
	if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
	{
		%lck = fetchData(%aiId, "LCK");
		if(%lck == "" || %lck == -1)
			%lck = 0;
	}
	else
	{
		%lck = AddPoints(%aiId, 2);
	}
	%stance = fetchData(%aiId, "Stance");
	%overweightStep = fetchData(%aiId, "OverweightStep");
	%overweightPenalty = (%overweightStep * 7.0);
	%storedDMG = fetchData(%aiId, "DMG");
	
	echo("=== SealBattle Bot Stats: " @ %botName @ " (ID: " @ %aiId @ ", Round: " @ %round @ ") ===");
	echo("  Round Multiplier: " @ %mult);
	echo("  Scaled Underlying Values:");
	echo("    LVL: " @ %baseLVL @ " -> " @ %scaledLVL);
	if(%baseRemortStep != "" && %baseRemortStep != -1 && %baseRemortStep != 0)
		echo("    RemortStep: " @ %baseRemortStep @ " -> " @ %scaledRemortStep);
	if(%baseEndurance != "" && %baseEndurance != -1 && %baseEndurance != 0)
		echo("    Endurance: " @ %baseEndurance @ " -> " @ %scaledEndurance);
	if(%baseEnergy != "" && %baseEnergy != -1 && %baseEnergy != 0)
		echo("    Energy: " @ %baseEnergy @ " -> " @ %scaledEnergy);
	if(%baseWeightCapacity != "" && %baseWeightCapacity != -1 && %baseWeightCapacity != 0)
		echo("    WeightCapacity: " @ %baseWeightCapacity @ " -> " @ %scaledWeightCapacity);
	echo("  Calculated Stats (from RefreshAll):");
	echo("    MaxHP: " @ %baseMaxHP @ " -> " @ %maxHP);
	echo("    MaxMANA: " @ %baseMaxMANA @ " -> " @ %maxMANA);
	echo("    MaxWeight: " @ %maxWeight);
	echo("  Directly Scaled Stats:");
	echo("    DEF: " @ %baseDEF @ " -> " @ %def);
	echo("    MDEF: " @ %baseMDEF @ " -> " @ %mdef);
	echo("    ATK: " @ %baseATK @ " -> " @ %atk);
	echo("    DMG: " @ %baseDMG @ " -> " @ %storedDMG);
	echo("    LCK: " @ %baseLCK @ " -> " @ %lck);
	echo("  Current Stats:");
	echo("    HP: " @ %hp @ " / " @ %maxHP @ "  MANA: " @ %mana @ " / " @ %maxMANA);
	echo("    Weight: " @ %weight @ " / " @ %maxWeight @ "  LCK: " @ %lck);
	if(%stance != "")
		echo("    Stance: " @ %stance);
	if(%overweightStep > 0)
		echo("    Overweight Penalty: " @ %overweightPenalty @ "%");
	echo("==========================================");
	
	// CRITICAL: Freeze and invulnerability are now set earlier (after HardcodeAIskills check)
	// This ensures the bot is fully initialized before being frozen
	
	// CRITICAL: Set Seal Battle-specific bot behavior flags AFTER all setup/unfreeze/shove operations
	// This ensures these flags only apply to Seal Battle bots and don't interfere with other operations
	// Note: Unfreeze and shove happen in separate functions (SealBattle::UnfreezeRound, SealBattle::ShoveRoundBots)
	// but we set flags here at the end of setup so they're ready when unfreeze/shove occur

	// Use mode 2 (follow) to enable sniffing + aggressive targeting
	storeData(%aiId, "botAttackMode", 2);
	// For mode 2, tmpbotdata should contain target ID, but we'll let sniffing find the closest player
	// Set to empty for now - sniffing will set AITarget automatically
	storeData(%aiId, "tmpbotdata", "");

	// Explicitly enable sniffing by clearing noBotSniff flag
	storeData(%aiId, "noBotSniff", "");

	// Keep AImoveChance = 1 for frequent movement
	storeData(%aiId, "AImoveChance", 1);

	// CRITICAL: Set increased detection range for Seal Battle bots
	// Seal Battle uses the same Colloseum arena (~65x60 units, diagonal ~88.5 units)
	// Set to 120 units to ensure full arena coverage with safety margin
	storeData(%aiId, "AImaxRangeOverride", 120);

	// Note: We do NOT force movement here - the natural AI system will handle it:
	// - AI::Periodic() runs every 5 seconds and will call AI::SelectMovement() when no target is found
	// - AImoveChance = 1 ensures bots move frequently (50% chance every 5 seconds)
	// - Sniffing will automatically find targets and handle movement
	// Forcing movement could interfere with bot initialization or conflict with the sniffing system
}

// Function to unfreeze a round's bots and remove invulnerability
function SealBattle::UnfreezeRound(%round)
{
	// Use internal names first (reliable), fallback to display names
	%fighterId = -1;
	%mageId = -1;
	%guardianId = -1;
	
	// Try internal name lookup first (reliable)
	if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
	{
		%fighterId = AI::getClientIdFromName($SealBattle::FighterName);
		if(%fighterId == "") %fighterId = -1; // Normalize empty string to -1
	}
	if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
	{
		%mageId = AI::getClientIdFromName($SealBattle::MageName);
		if(%mageId == "") %mageId = -1; // Normalize empty string to -1
	}
	if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
	{
		%guardianId = AI::getClientIdFromName($SealBattle::GuardianName);
		if(%guardianId == "") %guardianId = -1; // Normalize empty string to -1
	}
	
	// Fallback to display name lookup
	if(%fighterId == -1)
		%fighterId = NEWgetClientByName("SealFighter" @ %round);
	if(%mageId == -1)
		%mageId = NEWgetClientByName("SealMage" @ %round);
	if(%guardianId == -1)
		%guardianId = NEWgetClientByName("SealGuardian" @ %round);
	
	// Safety check: If lookup fails (shouldn't happen 18-30 seconds after spawn, but handle gracefully)
	// Since Unfreeze happens well after spawn, both internal names and display names should be registered by now
	if(%fighterId == -1 && %mageId == -1 && %guardianId == -1)
	{
		echo("WARNING SealBattle::UnfreezeRound: Could not find any bots for round " @ %round @ " (tried internal names and display names)");
		echo("  This may indicate bots were killed or never spawned. Continuing anyway...");
		// Don't return - continue to send message and call ShoveRoundBots (which also has safety checks)
	}
	
	// Unfreeze and remove invulnerability for each bot
	if(%fighterId != -1 && %fighterId != "")
	{
		storeData(%fighterId, "frozen", "");
		$BotFrozen[%fighterId] = "";
		$EnemyBotData[%fighterId, "frozen"] = "";
		storeData(%fighterId, "SpawnInvuln", "");
		$EnemyBotData[%fighterId, "SpawnInvuln"] = "";
		$ClientData[%fighterId, "SpawnInvuln"] = "";
		$SpawnInvulnByName["SealFighter" @ %round] = "";
		
		// CRITICAL: Re-enable AI seeking and targeting for Player objects
		%fighterAiName = fetchData(%fighterId, "BotInfoAiName");
		if(%fighterAiName == "" || %fighterAiName == -1 || %fighterAiName == "0")
			%fighterAiName = $BotInfoAiName[%fighterId];
		if(%fighterAiName != "" && %fighterAiName != -1 && %fighterAiName != "0")
		{
			AI::setVar(%fighterAiName, seekOff, 0); // Re-enable AI seeking
			AI::setVar(%fighterAiName, SpotDist, $AIspotDist); // Restore spot distance
			// Trigger immediate AI::Periodic() call so bot detects and chases players immediately after unfreeze
			schedule("AI::Periodic(\"" @ %fighterAiName @ "\");", 0.1);
		}
	}
	
	if(%mageId != -1 && %mageId != "")
	{
		storeData(%mageId, "frozen", "");
		$BotFrozen[%mageId] = "";
		$EnemyBotData[%mageId, "frozen"] = "";
		storeData(%mageId, "SpawnInvuln", "");
		$EnemyBotData[%mageId, "SpawnInvuln"] = "";
		$ClientData[%mageId, "SpawnInvuln"] = "";
		$SpawnInvulnByName["SealMage" @ %round] = "";
		
		// CRITICAL: Re-enable AI seeking and targeting for Player objects
		%mageAiName = fetchData(%mageId, "BotInfoAiName");
		if(%mageAiName == "" || %mageAiName == -1 || %mageAiName == "0")
			%mageAiName = $BotInfoAiName[%mageId];
		if(%mageAiName != "" && %mageAiName != -1 && %mageAiName != "0")
		{
			AI::setVar(%mageAiName, seekOff, 0); // Re-enable AI seeking
			AI::setVar(%mageAiName, SpotDist, $AIspotDist); // Restore spot distance
			// Trigger immediate AI::Periodic() call so bot detects and chases players immediately after unfreeze
			schedule("AI::Periodic(\"" @ %mageAiName @ "\");", 0.1);
		}
	}
	
	if(%guardianId != -1 && %guardianId != "")
	{
		storeData(%guardianId, "frozen", "");
		$BotFrozen[%guardianId] = "";
		$EnemyBotData[%guardianId, "frozen"] = "";
		storeData(%guardianId, "SpawnInvuln", "");
		$EnemyBotData[%guardianId, "SpawnInvuln"] = "";
		$ClientData[%guardianId, "SpawnInvuln"] = "";
		$SpawnInvulnByName["SealGuardian" @ %round] = "";
		
		// CRITICAL: Re-enable AI seeking and targeting for Player objects
		%guardianAiName = fetchData(%guardianId, "BotInfoAiName");
		if(%guardianAiName == "" || %guardianAiName == -1 || %guardianAiName == "0")
			%guardianAiName = $BotInfoAiName[%guardianId];
		if(%guardianAiName != "" && %guardianAiName != -1 && %guardianAiName != "0")
		{
			AI::setVar(%guardianAiName, seekOff, 0); // Re-enable AI seeking
			AI::setVar(%guardianAiName, SpotDist, $AIspotDist); // Restore spot distance
			// Trigger immediate AI::Periodic() call so bot detects and chases players immediately after unfreeze
			schedule("AI::Periodic(\"" @ %guardianAiName @ "\");", 0.1);
		}
	}
	
	// Send message to Colloseum players (only for rounds 2 and 3, Round 1 message is handled in StartBattle)
	if(%round > 1)
		SealBattle::MessageColloseumPlayers("Wave " @ %round @ " bots are now active!");
	
	// CRITICAL: Apply a shove effect to all bots to get them moving when the round begins
	// This helps ensure bots start engaging properly after being unfrozen
	SealBattle::ShoveRoundBots(%round);
}

// Function to apply a shove effect to all bots in a round to get them moving
function SealBattle::ReapplyScaledStats(%aiId, %round)
{
	// CRITICAL: Re-apply scaled stats after HardcodeAIskills() has completed
	// HardcodeAIskills() calls RefreshAllEnemyBot() which recalculates DEF, MDEF, ATK, DMG from base values
	// This overwrites our scaled stats, so we need to re-apply them
	
	echo("[SEAL BATTLE] SealBattle::ReapplyScaledStats(): ENTRY - clientId=" @ %aiId @ ", round=" @ %round);
	
	// Check if we have stored scaled stats for this bot
	%storedDEF = $SealBattleScaledStats[%aiId, "DEF"];
	%storedMDEF = $SealBattleScaledStats[%aiId, "MDEF"];
	%storedATK = $SealBattleScaledStats[%aiId, "ATK"];
	%storedDMG = $SealBattleScaledStats[%aiId, "DMG"];
	%storedRound = $SealBattleScaledStats[%aiId, "round"];
	
	echo("[SEAL BATTLE] SealBattle::ReapplyScaledStats(): Stored stats - DEF: " @ %storedDEF @ ", MDEF: " @ %storedMDEF @ ", ATK: " @ %storedATK @ ", DMG: " @ %storedDMG @ ", round: " @ %storedRound);
	
	// Verify bot still exists and is a seal battle bot
	%playerObj = Client::getOwnedObject(%aiId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Bot doesn't exist anymore, clear stored stats
		echo("[SEAL BATTLE] SealBattle::ReapplyScaledStats(): Bot no longer exists, clearing stored stats");
		$SealBattleScaledStats[%aiId, "DEF"] = "";
		$SealBattleScaledStats[%aiId, "MDEF"] = "";
		$SealBattleScaledStats[%aiId, "ATK"] = "";
		$SealBattleScaledStats[%aiId, "DMG"] = "";
		$SealBattleScaledStats[%aiId, "round"] = "";
		return;
	}
	
	// Verify this is for the correct round
	if(%storedRound != %round)
	{
		// Wrong round, don't re-apply
		echo("[SEAL BATTLE] SealBattle::ReapplyScaledStats(): Round mismatch (stored: " @ %storedRound @ ", requested: " @ %round @ "), skipping");
		return;
	}
	
	// Check if we have valid stored stats
	if(%storedDEF == "" || %storedDEF == -1 || %storedMDEF == "" || %storedMDEF == -1 || %storedATK == "" || %storedATK == -1 || %storedDMG == "" || %storedDMG == -1)
	{
		echo("[SEAL BATTLE] SealBattle::ReapplyScaledStats(): WARNING - Invalid stored stats, cannot re-apply");
		return;
	}
	
	// Re-apply scaled stats to all data arrays
	if(%storedDEF != "" && %storedDEF != -1)
	{
		storeData(%aiId, "DEF", %storedDEF);
		$EnemyBotData[%aiId, "DEF"] = %storedDEF;
		$ClientData[%aiId, "DEF"] = %storedDEF;
	}
	if(%storedMDEF != "" && %storedMDEF != -1)
	{
		storeData(%aiId, "MDEF", %storedMDEF);
		$EnemyBotData[%aiId, "MDEF"] = %storedMDEF;
		$ClientData[%aiId, "MDEF"] = %storedMDEF;
	}
	if(%storedATK != "" && %storedATK != -1)
	{
		storeData(%aiId, "ATK", %storedATK);
		$EnemyBotData[%aiId, "ATK"] = %storedATK;
		$ClientData[%aiId, "ATK"] = %storedATK;
	}
	if(%storedDMG != "" && %storedDMG != -1)
	{
		storeData(%aiId, "DMG", %storedDMG);
		$EnemyBotData[%aiId, "DMG"] = %storedDMG;
		$ClientData[%aiId, "DMG"] = %storedDMG;
	}
	
	// Also re-apply HP and MANA to max values
	// CRITICAL: Use the stored scaled MaxHP/MaxMANA from $SealBattleScaledStats, not fetchData()
	// This ensures we use the correct scaled values even if RefreshAll() recalculated MaxHP incorrectly
	%maxHP = $SealBattleScaledStats[%aiId, "MaxHP"];
	if(%maxHP == "" || %maxHP == -1 || %maxHP == "0" || %maxHP == 0)
	{
		// Fallback to fetchData if stored value not available
		%maxHP = fetchData(%aiId, "MaxHP");
	}
	%maxMANA = $SealBattleScaledStats[%aiId, "MaxMANA"];
	if(%maxMANA == "" || %maxMANA == -1 || %maxMANA == "0" || %maxMANA == 0)
	{
		// Fallback to fetchData if stored value not available
		%maxMANA = fetchData(%aiId, "MaxMANA");
	}
	
	// CRITICAL: Use setHP() and setMANA() to properly set the damage/energy levels
	// These functions calculate the damage/energy level from HP/MaxHP ratio
	// This ensures the visual HP bar matches the actual HP value
	if(%maxHP != "" && %maxHP != -1 && %maxHP > 0)
	{
		setHP(%aiId, %maxHP);
		storeData(%aiId, "HP", %maxHP);
	}
	if(%maxMANA != "" && %maxMANA != -1 && %maxMANA > 0)
	{
		setMANA(%aiId, %maxMANA);
		storeData(%aiId, "MANA", %maxMANA);
	}
	
	echo("[SEAL BATTLE] SealBattle::ReapplyScaledStats(): Re-applied scaled stats for bot (clientId=" @ %aiId @ ", round=" @ %round @ ") - DEF: " @ %storedDEF @ ", MDEF: " @ %storedMDEF @ ", ATK: " @ %storedATK @ ", DMG: " @ %storedDMG);
}

function SealBattle::ShoveRoundBots(%round)
{
	// Use internal names first (reliable), fallback to display names
	%fighterId = -1;
	%mageId = -1;
	%guardianId = -1;
	
	// Try internal name lookup first (reliable)
	if($SealBattle::FighterName != "" && $SealBattle::FighterName != -1)
	{
		%fighterId = AI::getClientIdFromName($SealBattle::FighterName);
		if(%fighterId == "") %fighterId = -1; // Normalize empty string to -1
	}
	if($SealBattle::MageName != "" && $SealBattle::MageName != -1)
	{
		%mageId = AI::getClientIdFromName($SealBattle::MageName);
		if(%mageId == "") %mageId = -1; // Normalize empty string to -1
	}
	if($SealBattle::GuardianName != "" && $SealBattle::GuardianName != -1)
	{
		%guardianId = AI::getClientIdFromName($SealBattle::GuardianName);
		if(%guardianId == "") %guardianId = -1; // Normalize empty string to -1
	}
	
	// Fallback to display name lookup
	if(%fighterId == -1)
		%fighterId = NEWgetClientByName("SealFighter" @ %round);
	if(%mageId == -1)
		%mageId = NEWgetClientByName("SealMage" @ %round);
	if(%guardianId == -1)
		%guardianId = NEWgetClientByName("SealGuardian" @ %round);
	
	// Get the center of the Colloseum arena (where players typically are)
	%arenaCenter = "-3608 -2365 354";
	
	// Apply shove to each bot
	%botIds = %fighterId @ " " @ %mageId @ " " @ %guardianId;
	for(%i = 0; (%botId = GetWord(%botIds, %i)) != -1; %i++)
	{
		if(%botId == "" || %botId == -1)
			continue;
		
		%playerObj = Client::getOwnedObject(%botId);
		if(%playerObj == -1 || %playerObj == "")
			continue;
		
		// Get bot's current position
		%botPos = GameBase::getPosition(%playerObj);
		
		// Calculate direction towards arena center (or a random direction if center is too close)
		%toCenter = Vector::sub(%arenaCenter, %botPos);
		// Calculate distance manually (TorqueScript doesn't have Vector::len)
		%dx = GetWord(%toCenter, 0);
		%dy = GetWord(%toCenter, 1);
		%dz = GetWord(%toCenter, 2);
		%distToCenter = sqrt(%dx * %dx + %dy * %dy + %dz * %dz);
		
		if(%distToCenter < 5)
		{
			// Too close to center, use a random horizontal direction
			%randomAngle = getRandom() * 6.28318; // 0 to 2*PI
			%impulseDir = Vector::getFromRot("0 0 " @ %randomAngle, 0, 1);
		}
		else
		{
			// Normalize direction to center
			%impulseDir = Vector::normalize(%toCenter);
		}
		
		// Apply a small horizontal impulse (no vertical component)
		%impulseStrength = 5.0; // Small push to get them moving
		// Manual vector scaling (TorqueScript doesn't have Vector::scale)
		%impulseX = GetWord(%impulseDir, 0) * %impulseStrength;
		%impulseY = GetWord(%impulseDir, 1) * %impulseStrength;
		%impulse = %impulseX @ " " @ %impulseY @ " 0"; // Zero out Z component
		
		// Apply the impulse
		Player::applyImpulse(%playerObj, %impulse);
		
		// Clear any lingering directives and briefly disable seeking to allow impulse to work
		%botName = fetchData(%botId, "BotInfoAiName");
		if(%botName == "" || %botName == -1 || %botName == "0")
			%botName = $BotInfoAiName[%botId];
		if(%botName != "" && %botName != -1 && %botName != "0")
		{
			AI::newDirectiveRemove(%botName, 99); // Remove movement directives
			$aidirectiveTable[%botId, 99] = ""; // Clear directive table
			// Briefly disable seeking, then re-enable after 0.2 seconds (shorter than normal shove)
			AI::setVar(%botName, seekOff, 1);
			schedule("AI::setVar(\"" @ %botName @ "\", seekOff, 0);", 0.2);
		}
		
		echo("[SEAL BATTLE] SealBattle::ShoveRoundBots(): Applied shove to bot " @ %botName @ " (clientId=" @ %botId @ ", round=" @ %round @ ")");
	}
}

// Stub functions for Duel system - returns false/does nothing since dueling system is not used
function Duel::isDueling(%clientId)
{
	return false;
}

function Duel::endDuel(%pass)
{
	// Stub - does nothing since dueling system is not used
}
