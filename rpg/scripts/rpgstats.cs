// ECON-FIX 2026-07-18: hard ceiling for currency/progress balances. Stays well
// below 2^31 (2,147,483,647) where the engine's int32 floor() wraps values
// negative, and below where %g stringification drift gets dangerous. Read at
// runtime by storeData only.
$Kronos::BalanceCap = 2000000000;

// Helper function to determine which data array to use for a client
// Returns: "player", "townbot", or "enemybot"
// CRITICAL: Player safeguard FIRST - never misidentify player as bot (causes black screen)
function GetClientDataType(%clientId)
{
	// PRIORITY 0: PLAYER SAFEGUARD - Check for player save file FIRST
	// If player has a character save file, they are DEFINITELY a player, NOT a bot
	// This prevents stale bot data from causing player misidentification (black screen)
	%playerName = Client::getName(%clientId);
	if(%playerName != "" && %playerName != -1)
	{
		%characterFile = "temp\\" @ %playerName @ ".cs";
		if(isFile(%characterFile))
		{
			return "player";  // Has save file = definitely a player
		}
	}
	
	// PRIORITY 2: Check $BotType cache (set at spawn, cleared on death)
	// This is the fast path for properly spawned bots
	%botType = $BotType[%clientId];
	if(%botType == "enemy")
		return "enemybot";
	if(%botType == "town")
		return "townbot";
	
	// LEGACY: If $BotType not set, check arrays (backwards compatibility)
	if(isRPGAI(%clientId))
	{
		// Check enemy bot array first
		%spawnBotInfoEnemy = $EnemyBotData[%clientId, "SpawnBotInfo"];
		if(%spawnBotInfoEnemy != "" && %spawnBotInfoEnemy != "0" && %spawnBotInfoEnemy != -1)
			return "enemybot";
		
		// Check town bot array
		%townBotAiName = $TownBotData[%clientId, "BotInfoAiName"];
		if(%townBotAiName != "" && %townBotAiName != "0" && %townBotAiName != -1)
			return "townbot";
		
		// Check old $ClientData for backwards compatibility
		%spawnBotInfo = $ClientData[%clientId, "SpawnBotInfo"];
		if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
			return "enemybot";
		
		%oldBotInfoAiName = $ClientData[%clientId, "BotInfoAiName"];
		if(%oldBotInfoAiName != "" && %oldBotInfoAiName != "0" && %oldBotInfoAiName != -1)
			return "townbot";
		
		// PRIORITY 3: Bot has no $BotType and no data - spawn was incomplete OR bot just died
		// Check if this is a recently freed client ID (normal during death cleanup)
		%recentlyFreed = $ClientIdRecentlyFreed[%clientId];
		if(%recentlyFreed == "" || %recentlyFreed == "0" || %recentlyFreed == -1)
		{
			// Not recently freed - this is unexpected, log warning (throttled to prevent spam)
			%now = getSimTime();
			%lastWarned = $GetClientDataTypeLastWarning[%clientId];
			if(%lastWarned == "" || %lastWarned == -1 || (%now - %lastWarned) > 5)
			{
				echo("WARNING: GetClientDataType - Bot " @ %clientId @ " has no $BotType and no data. Defaulting to player for safety.");
				$GetClientDataTypeLastWarning[%clientId] = %now;
			}
		}
		// Default to player (SAFE) - better to break bot than player
		return "player";
	}
	
	// Not a bot - definitely a player
	return "player";
}


// Helper function to get data from appropriate array
// During migration: checks new array first, falls back to old array
// After migration: only uses new arrays
function GetDataFromArray(%clientId, %type, %clientType)
{
	// Optional fast-path: caller may provide resolved type to avoid repeated lookups
	if(%clientType == "" || %clientType == -1)
		%clientType = GetClientDataType(%clientId);
	
	if(%clientType == "player")
	{
		// Players always use $ClientData
		return $ClientData[%clientId, %type];
	}
	else if(%clientType == "townbot")
	{
		// Town bots only use $TownBotData
		return $TownBotData[%clientId, %type];
	}
	else if(%clientType == "enemybot")
	{
		// Enemy bots only use $EnemyBotData
		return $EnemyBotData[%clientId, %type];
	}
	
	// Default fallback to old array (only for non-player/non-bot types if any exist)
	return $ClientData[%clientId, %type];
}

// Helper function to set data in appropriate array
// PRIORITY 4: Bots only write to their specific array (no dual-write to $ClientData)
// This prevents stale bot data in $ClientData from causing player misidentification
function SetDataInArray(%clientId, %type, %value, %clientType)
{
	// Optional fast-path: caller may provide resolved type to avoid repeated lookups
	if(%clientType == "" || %clientType == -1)
		%clientType = GetClientDataType(%clientId);
	
	if(%clientType == "player")
	{
		$ClientData[%clientId, %type] = %value;
	}
	else if(%clientType == "townbot")
	{
		$TownBotData[%clientId, %type] = %value;
		// CRITICAL: Ensure we also clear the legacy entry to prevent fallback/haunting
		$ClientData[%clientId, %type] = "";
	}
	else if(%clientType == "enemybot")
	{
		$EnemyBotData[%clientId, %type] = %value;
		// CRITICAL: Ensure we also clear the legacy entry to prevent fallback/haunting
		$ClientData[%clientId, %type] = "";
	}
	else
	{
		// Default fallback
		$ClientData[%clientId, %type] = %value;
	}
}

// Temporary override helpers for high-churn cleanup paths.
// Use Begin/End pairs around tightly scoped bot cleanup blocks.
function BeginStoreDataClientTypeOverride(%clientId, %clientType)
{
	if(%clientId == "" || %clientId == -1)
		return;
	if(%clientType == "" || %clientType == -1)
		return;
	if(%clientType != "player" && %clientType != "townbot" && %clientType != "enemybot")
		return;

	%depth = $StoreDataClientTypeOverrideDepth[%clientId];
	if(%depth == "" || %depth == -1)
		%depth = 0;

	// Only set/refresh the type on first entry.
	if(%depth <= 0)
		$StoreDataClientTypeOverride[%clientId] = %clientType;

	$StoreDataClientTypeOverrideDepth[%clientId] = %depth + 1;
}

function EndStoreDataClientTypeOverride(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return;

	%depth = $StoreDataClientTypeOverrideDepth[%clientId];
	if(%depth == "" || %depth == -1 || %depth <= 1)
	{
		$StoreDataClientTypeOverrideDepth[%clientId] = "";
		$StoreDataClientTypeOverride[%clientId] = "";
		return;
	}

	$StoreDataClientTypeOverrideDepth[%clientId] = %depth - 1;
}

function fetchData(%clientId, %type)
{
	dbecho($dbechoMode, "fetchData(" @ %clientId @ ", " @ %type @ ")");
	if(%type == "BANK")
		return Bank::GetText(%clientId);

	if(%type == "LVL")
	{
		// CRITICAL: For seal battle bots, use the stored LVL directly
		// The standard GetLevel(EXP) calculation returns 1 for bots (0 EXP) regardless of difficultly
		// We MUST use the manually set LVL from SetupBot for stat scaling to work
		%isSealBattleBot = GetDataFromArray(%clientId, "SealBattleBot");
		if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
		{
			%storedLVL = GetDataFromArray(%clientId, "LVL");
			if(%storedLVL != "" && %storedLVL != -1 && %storedLVL != 0)
				return %storedLVL;
		}

		%a = GetLevel(fetchData(%clientId, "EXP"), %clientId);
		return %a;
	}
	else if(%type == "DEF")
	{
		// CRITICAL: For seal battle bots, check for stored scaled value FIRST
		// This ensures scaled stats from SealBattle::SetupBot() take precedence over dynamic calculation
		%isSealBattleBot = GetDataFromArray(%clientId, "SealBattleBot");
		if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
		{
			// Check for stored scaled DEF value
			%storedDEF = GetDataFromArray(%clientId, "DEF");
			if(%storedDEF != "" && %storedDEF != -1 && %storedDEF != "0" && %storedDEF != 0)
			{
				// Return stored scaled value instead of calculating
				return %storedDEF + 0; // Convert to number
			}
		}
		
		// Default: Calculate dynamically from skills, items, and RemortStep
		%a = AddPoints(%clientId, 7);
		%b = AddBonusStatePoints(%clientId, "DEF");
		%c = (%a + %b + (fetchData(%clientId, "RemortStep") * 2));
		%d = (fetchData(%clientId, "OverweightStep") * 7.0) / 100;
		%e = Cap(%c - (%c * %d), 0, "inf");
		
		return floor(%e);
	}
	else if(%type == "MDEF")
	{
		// CRITICAL: For seal battle bots, check for stored scaled value FIRST
		// This ensures scaled stats from SealBattle::SetupBot() take precedence over dynamic calculation
		%isSealBattleBot = GetDataFromArray(%clientId, "SealBattleBot");
		if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
		{
			// Check for stored scaled MDEF value
			%storedMDEF = GetDataFromArray(%clientId, "MDEF");
			if(%storedMDEF != "" && %storedMDEF != -1 && %storedMDEF != "0" && %storedMDEF != 0)
			{
				// Return stored scaled value instead of calculating
				return %storedMDEF + 0; // Convert to number
			}
		}
		
		// Default: Calculate dynamically from skills, items, and RemortStep
		%a = AddPoints(%clientId, 3);
		%b = AddBonusStatePoints(%clientId, "MDEF");
		%c = (%a + %b + (fetchData(%clientId, "RemortStep") * 2));
		%d = (fetchData(%clientId, "OverweightStep") * 7.0) / 100;
		%e = Cap(%c - (%c * %d), 0, "inf");
		
		return floor(%e);
	}
	else if(%type == "ATK")
	{
		// CRITICAL: For seal battle bots, check for stored scaled value FIRST
		// This ensures scaled stats from SealBattle::SetupBot() take precedence over dynamic calculation
		%isSealBattleBot = GetDataFromArray(%clientId, "SealBattleBot");
		if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
		{
			// Check for stored scaled ATK value
			%storedATK = GetDataFromArray(%clientId, "ATK");
			if(%storedATK != "" && %storedATK != -1 && %storedATK != "0" && %storedATK != 0)
			{
				// Return stored scaled value instead of calculating
				return %storedATK + 0; // Convert to number
			}
		}
		
		// Default: Calculate dynamically from weapon, bonus states, and RemortStep
		%weapon = Player::getMountedItem(%clientId, $WeaponSlot);

		if(%weapon != -1)
		{
			%a = AddBonusStatePoints(%clientId, "ATK");

			if(GetAccessoryVar(%weapon, $AccessoryType) == $RangedAccessoryType)
				%weapon = fetchData(%clientId, "LoadedProjectile " @ %weapon);

			%b = GetRoll(GetWord(GetAccessoryVar(%weapon, $SpecialVar), 1));
			%c = fetchData(%clientId, "RemortStep");

			return %a + %b + %c;
		}
		else
			return 0;
	}
	else if(%type == "MaxHP")
	{
		// CRITICAL: For seal battle bots, check for stored scaled value FIRST
		// This ensures scaled MaxHP from SealBattle::SetupBot() takes precedence over dynamic calculation
		%isSealBattleBot = GetDataFromArray(%clientId, "SealBattleBot");
		if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
		{
			// Check for stored scaled MaxHP value (stored in special array since storeData() blocks MaxHP)
			%storedMaxHP = $SealBattleScaledStats[%clientId, "MaxHP"];
			if(%storedMaxHP != "" && %storedMaxHP != -1 && %storedMaxHP != "0" && %storedMaxHP != 0)
			{
				// Return stored scaled value instead of calculating
				// Apply stance modifier if needed
				if(fetchData(%clientId, "Stance") == "Glass Cannon")
					return floor((%storedMaxHP + 0) * 0.5); // Convert to number and apply stance
				else
					return %storedMaxHP + 0; // Convert to number
			}
		}
		
		// Default: Calculate dynamically from skills, items, RemortStep, and LVL
		%a = $MinHP[fetchData(%clientId, "RACE")] + ($PlayerSkill[%clientId, $SkillEndurance] * 0.6);
		%b = AddPoints(%clientId, 4);
		%c = floor(fetchData(%clientId, "RemortStep") * ($PlayerSkill[%clientId, $SkillEndurance] / 8));
		%d = fetchData(%clientId, "LVL");
		%e = AddBonusStatePoints(%clientId, "MaxHP");

		%baseHP = floor(%a + %b + %c + %d + %e);
		
		if(fetchData(%clientId, "Stance") == "Glass Cannon")
			return floor(%baseHP * 0.5);
		else
			return %baseHP;
	}
	else if(%type == "HP")
	{
		%armor = Player::getArmor(%clientId);

		%c = %armor.maxDamage - GameBase::getDamageLevel(Client::getOwnedObject(%clientId));
		%a = %c * fetchData(%clientId, "MaxHP");
		%b = %a / %armor.maxDamage;

		return round(%b);
	}
	else if(%type == "MaxMANA")
	{
		// CRITICAL: For seal battle bots, check for stored scaled value FIRST
		// This ensures scaled MaxMANA from SealBattle::SetupBot() takes precedence over dynamic calculation
		%isSealBattleBot = GetDataFromArray(%clientId, "SealBattleBot");
		if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
		{
			// Check for stored scaled MaxMANA value (stored in special array since storeData() blocks MaxMANA)
			%storedMaxMANA = $SealBattleScaledStats[%clientId, "MaxMANA"];
			if(%storedMaxMANA != "" && %storedMaxMANA != -1 && %storedMaxMANA != "0" && %storedMaxMANA != 0)
			{
				// Return stored scaled value instead of calculating
				// Apply stance modifier if needed
				if(fetchData(%clientId, "Stance") == "Glass Cannon")
					return round((%storedMaxMANA + 0) * 0.5); // Convert to number and apply stance
				else
					return %storedMaxMANA + 0; // Convert to number
			}
		}
		
		// Default: Calculate dynamically from skills, items, and RemortStep
		%a = 8 + round( $PlayerSkill[%clientId, $SkillEnergy] * (1/3) + (fetchData(%clientId, "RemortStep") * 2));
		%b = AddPoints(%clientId, 5);
		%c = AddBonusStatePoints(%clientId, "MaxMANA");

		// %c (bonus states) was computed but dropped from the returns - MaxMANA
		// buffs silently did nothing. Docs (STAT_VARIABLES_REFERENCE.md) and the
		// MaxHP formula both include the bonus-state term.
		if(fetchData(%clientId, "Stance") == "Glass Cannon")
			return round((%a + %b + %c) * 0.5);
		else
			return %a + %b + %c;
	}
	else if(%type == "MANA")
	{
		%armor = Player::getArmor(%clientId);

		%a = GameBase::getEnergy(Client::getOwnedObject(%clientId)) * fetchData(%clientId, "MaxMANA");
		%b = %a / %armor.maxEnergy;

		return round(%b);
	}
	else if(%type == "MaxWeight")
	{
		%a = 50 + $PlayerSkill[%clientId, $SkillWeightCapacity] + (fetchData(%clientId, "RemortStep") * 4);
		//%b = AddPoints(%clientId, 9);
		%c = AddBonusStatePoints(%clientId, "MaxWeight");

		return FixDecimals(%a + %c);
	}
	else if(%type == "Weight")
	{
		return GetWeight(%clientId);
	}
	else if(%type == "RankPoints")
	{
		return Cap(floor($ClientData[%clientId, %type]), 0, "250");
	}
	else if(%type == "TournyRank")
	{
		return Cap(floor($ClientData[%clientId, %type]), 0, "inf");
	}
	else if(%type == "OverweightStep")
	{
		return Cap(floor($ClientData[%clientId, %type]), 0, "inf");
	}
	else if(%type == "SlowdownHitFlag")
	{
		if(Player::isAiControlled(%clientId))
			return False;
		else
		{
			// Use temp variable to safely check if ClientData exists before accessing
			%tempData = $ClientData[%clientId, %type];
			if(%tempData != "")
				return %tempData;
			else
				return False;
		}
	}
	else
	{
		// Use GetDataFromArray to properly handle player/townbot/enemybot data sources
		return GetDataFromArray(%clientId, %type);
	}

	return False;
}
function remotefetchData(%clientId, %type)
{
	dbecho($dbechoMode, "remotefetchData(" @ %clientId @ ", " @ %type @ ")");

	//rpgfetchdata specific vartypes
	if (String::FindSubStr(%type, "password") != -1)
	{
	return;
	}
	if(%type == "zonedesc")
	{
		%r = fetchData(%clientId, "zone");
		%data = Zone::getDesc(%r);
	}
	else if(%type == "password")
	{
	return;
	}
	else if(%type == "servername")
	{
		%data = $Server::HostName;
	}
	else if(GetWord(%type, 0) == "skill" && (%s = GetWord(%type, 1)) != -1)
	{
		%data = $PlayerSkill[%clientId, %s];
	}
	else if(GetWord(%type, 0) == "getbuycost" && (%s = GetWord(%type, 1)) != -1)
	{
		%data = getBuyCost(%clientId, %s);
	}
	else if(GetWord(%type, 0) == "getsellcost" && (%s = GetWord(%type, 1)) != -1)
	{
		%data = getSellCost(%clientId, %s);
	}
	else if(GetWord(%type, 0) == "skillcanuse" && (%s = GetWord(%type, 1)) != -1)
	{
		%data = SkillCanUse(%clientId, %s);
	}
	else if(GetWord(%type, 0) == "spellcancast" && (%s = GetWord(%type, 1)) != -1)
	{
		%data = SpellCanCast(%clientId, %s);
	}
	else if(GetWord(%type, 0) == "skillcancastnow" && (%s = GetWord(%type, 1)) != -1)
	{
		%data = SpellCanCastNow(%clientId, %s);
	}
	else
		%data = fetchData(%clientId, %type);

	if(Client::getName(%clientId) != "")
		remoteEval(%clientId, SetRPGdata, %data, %type);
}

function storeData(%clientId, %type, %amt, %special)
{
	// Initialize parameters using safe concatenation to handle unassigned variables
	// This prevents warnings when parameters aren't passed
	%tempAmt = %amt @ "";  // If unassigned, becomes empty string; if assigned, stays same
	// review #20: dropped the "|| %tempAmt == -1" clause. == is numeric, so it
	// caught a LEGITIMATE amount of exactly -1 (a stat penalty, refund, or any
	// expression evaluating to -1) and silently zeroed it. Only a missing arg
	// (which concatenates to the empty string) should default to 0; a real -1
	// stringifies to "-1", which "== \"\"" (-1 == 0) correctly rejects.
	if(%tempAmt == "")
		%amt = 0;
	else
		%amt = %tempAmt;
	
	%tempSpecial = %special @ "";  // If unassigned, becomes empty string; if assigned, stays same
	if(%tempSpecial == "" || %tempSpecial == -1)
		%special = "";
	else
		%special = %tempSpecial;
	
	dbecho($dbechoMode, "storeData(" @ %clientId @ ", " @ %type @ ", " @ %amt @ ", " @ %special @ ")");
	if(%type == "BANK")
	{
		Bank::StoreCompat(%clientId, %amt, %special);
		return;
	}
	if(%type == "COINS" && %special == "inc" && %amt > 0)
	{
		Bank::CreditCoins(%clientId, %amt);
		return;
	}

	if(%type == "HP")
	{
		setHP(%clientId, %amt);
	}
	else if(%type == "MANA")
	{
		setMANA(%clientId, %amt);
	}
	else if(%type == "MaxHP" || %type == "MaxMANA" || %type == "MaxWeight" || %type == "Weight")
	{
		echo("Invalid call to storeData for " @ %type @ " : Can't manually set this variable.");
	}
	else
	{
		// Optional fast-path for high-frequency cleanup blocks.
		// When an override is active, skip expensive type resolution checks.
		%clientType = $StoreDataClientTypeOverride[%clientId];
		if(%clientType == "" || %clientType == -1)
			%clientType = GetClientDataType(%clientId);
		
		// Get current value from appropriate array
		// Pass resolved client type so storeData only resolves type once per call
		%currentValue = GetDataFromArray(%clientId, %type, %clientType);
		if(%currentValue == "")
			%currentValue = 0;

		if(%special == "inc")
			%newValue = %currentValue + %amt;
		else if(%special == "dec")
			%newValue = %currentValue - %amt;
		else if(%special == "strinc")
			%newValue = %currentValue @ %amt;
		else
			%newValue = %amt;

		// Only check for "cap" if %special is not empty and has a second word
		if(%special != "")
		{
			%specialWord1 = GetWord(%special, 1);
			if(%specialWord1 != "" && %specialWord1 == "cap")
				%newValue = Cap(%newValue, GetWord(%special, 2), GetWord(%special, 3));
		}

		// ECON-FIX 2026-07-18: clamp currency/progress balances to
		// [0, $Kronos::BalanceCap] at the single write choke point. Guards every
		// earn/spend site at once: keeps balances below the engine's int32
		// floor() wrap (2^31), and stops admin-bypass buys (economy.cs
		// checkResources skips the affordability check at adminLevel >= 4) from
		// driving COINS negative. strinc excluded (string append, not numeric).
		if((%type == "COINS" || %type == "BANK" || %type == "EXP") && %special != "strinc")
			%newValue = Cap(%newValue, 0, $Kronos::BalanceCap);

		// Store in appropriate array
		// Pass resolved client type so storeData only resolves type once per call
		SetDataInArray(%clientId, %type, %newValue, %clientType);
	}
}

function MenuSP(%clientId, %page)
{
	dbecho($dbechoMode, "MenuSP(" @ %clientId @ ", " @ %page @ ")");

	Client::buildMenu(%clientId, "You have " @ fetchData(%clientId, "SPcredits") @ " SP credits", "sp", true);

	%clientId.bulkNum = "";

	%l = 6;
	%ns = GetNumSkills();
	%np = floor(%ns / %l);
	
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	for(%i = %lb; %i <= %ub; %i++)
		Client::addMenuItem(%clientId, %cnt++ @ "(" @ FormatSkillDisplay(%clientId, %i) @ ") " @ $SkillDesc[%i], %i @ " " @ %page);

	if(%page == 1)
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1);
	}

	return;
}
function processMenusp(%clientId, %opt)
{
	dbecho($dbechoMode, "processMenusp(" @ %clientId @ ", " @ %opt @ ")");

	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);

	// Reset "no longer used" skills (Stealing=7, Mining=17) to 0 if they have any value
	if(%o == 7 || %o == 17)
	{
		if($PlayerSkill[%clientId, %o] != 0 && $PlayerSkill[%clientId, %o] != "")
		{
			$PlayerSkill[%clientId, %o] = 0;
			$SkillCounter[%clientId, %o] = 0;
			RefreshAll(%clientId);
			Client::sendMessage(%clientId, $MsgBeige, $SkillDesc[%o] @ " has been reset to 0 (no longer used).");
			MenuSP(%clientId, %p);
			return;
		}
	}

	if(fetchData(%clientId, "SPcredits") > 0 && %o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)
			%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 1000 && !(%clientId.adminLevel >= 1) )
			%clientId.bulkNum = 1000;

		for(%i = 1; %i <= %clientId.bulkNum; %i++)
		{
			if(fetchData(%clientId, "SPcredits") > 0)
			{
				if(AddSkillPoint(%clientId, %o))
					storeData(%clientId, "SPcredits", 1, "dec");
				else
					break;
			}
			else
				break;
		}

		RefreshAll(%clientId);
	}

	if(%o != "done")
		MenuSP(%clientId, %p);
}
function processMenunull(%clientId, %opt)
{
	return;
}

function MenuGroup(%clientId)
{
	dbecho($dbechoMode, "MenuGroup(" @ %clientId @ ")");

	Client::buildMenu(%clientId, "Pick a group to procede:", "pickgroup", true);
	Client::addMenuItem(%clientId, "1Priest", 1);
	Client::addMenuItem(%clientId, "2Rogue", 2);
	Client::addMenuItem(%clientId, "3Warrior", 3);
	Client::addMenuItem(%clientId, "4Wizard", 4);

	return;
}
function processMenupickgroup(%clientId, %opt)
{
	dbecho($dbechoMode, "processMenupickgroup(" @ %clientId @ ", " @ %opt @ ")");

	if(%opt == 1)
		storeData(%clientId, "GROUP", "Priest");
	else if(%opt == 2)
		storeData(%clientId, "GROUP", "Rogue");
	else if(%opt == 3)
		storeData(%clientId, "GROUP", "Warrior");
	else if(%opt == 4)
		storeData(%clientId, "GROUP", "Wizard");

	%clientId.choosingGroup = "";
	%clientId.choosingClass = True;

	MenuClass(%clientId);
}

function MenuClass(%clientId)
{
	dbecho($dbechoMode, "MenuClass(" @ %clientId @ ")");

	Client::buildMenu(%clientId, "Choose your class to procede:", "pickclass", true);

	%op = 0;
	for(%i = 1; $ClassName[%i, 0] != ""; %i++)
	{
		if(String::ICompare(fetchData(%clientId, "GROUP"), $ClassGroup[$ClassName[%i, 0]]) == 0)
		{
			%op++;
			Client::addMenuItem(%clientId, %op @ $ClassName[%i, 0], %op);
		}
	}
	Client::addMenuItem(%clientId, "x<-- BACK", "back");


	return;
}
function processMenupickclass(%clientId, %opt)
{
	dbecho($dbechoMode, "processMenupickclass(" @ %clientId @ ", " @ %opt @ ")");

	if(%opt == "back")
	{
		%clientId.choosingClass = "";
		%clientId.choosingGroup = True;
		storeData(%clientId, "GROUP", "");

		MenuGroup(%clientId);
		return;
	}

	%op = 0;
	for(%i = 1; $ClassName[%i, 0] != ""; %i++)
	{
		if(String::ICompare(fetchData(%clientId, "GROUP"), $ClassGroup[$ClassName[%i, 0]]) == 0)
		{
			%op++;
			if(%op == %opt)
				storeData(%clientId, "CLASS", $ClassName[%i, 0]);
		}
	}

	//let the player enter the world
	%clientId.choosingClass = "";
	Game::playerSpawn(%clientId, false);

	//######### set a few start-up variables ########
	storeData(%clientId, "COINS", GetRoll($initcoins[fetchData(%clientId, "GROUP")]));
	Bank::SetParts(%clientId, 0, 0);            // Initialize chunked bank coins to 0
	SetDataInArray(%clientId, "BANK_LEGACY_BACKUP", "", GetClientDataType(%clientId));
	SetDataInArray(%clientId, "BANK_NEEDS_FILE_BACKUP", "", GetClientDataType(%clientId));
	storeData(%clientId, "BankStorage", "");   // Initialize bank item storage to empty

	//add $autoStartupSP for each skill
	for(%i = 1; %i <= getNumSkills(); %i++)
		AddSkillPoint(%clientId, %i, $autoStartupSP);
	//###############################################

	centerprint(%clientId, "<f1>Server powered by the RPG MOD version " @ $rpgver @ "<f0>\nWelcome to Kingdom of Kronos!\n" @ $loginMsg, 15);
}

function OldGetLevel(%ex, %clientId)
{
	dbecho($dbechoMode, "GetLevel(" @ %ex @ ", " @ %clientId @ ")");

	%m = GetEXPmultiplier(%clientId);

	if(%m != 0)
	{
		%a = (  (-500 * %m) + FixDecimals(sqrt( (250000 * %m * %m) + (2000 * %m * %ex) ))  ) / (1000 * %m);
		%b = floor(%a) + 1;
	}

	return %b;
}
function OldGetExp(%level, %clientId)
{
	dbecho($dbechoMode, "GetExp(" @ %level @ ", " @ %clientId @ ")");

	%m = GetEXPmultiplier(%clientId);

	%level--;
	%a = (500 * %level) + (500 * %level * %level);
	%b = floor( (%a * %m) + 0.2);

	return %b;
}

function GetLevel(%ex, %clientId)
{
	dbecho($dbechoMode, "GetLevel(" @ %ex @ ", " @ %clientId @ ")");

	%n = 1000;
	%b = floor(%ex / %n) + 1;

	return %b;
}
function GetExp(%level, %clientId)
{
	dbecho($dbechoMode, "GetExp(" @ %level @ ", " @ %clientId @ ")");

	%n = 1000;
	%b = (%level - 1) * %n;

	return %b;
}

function DistributeExpForKilling(%damagedClient)
{
	dbecho($dbechoMode2, "DistributeExpForKilling(" @ %damagedClient @ ")");

	// CRITICAL: Use GetClientOrBotName() for consistent naming with damage tracking
	// This ensures $damagedBy lookups use the SAME name that was used to track damage
	// EXCEPTION: For seal battle bots, use display name (Client::getName()) instead
	// so players see "SealFighter1" instead of "RoundOne3" in death messages
	%isSealBattleBot = fetchData(%damagedClient, "SealBattleBot");
	if(%isSealBattleBot == "true" || %isSealBattleBot == "True" || %isSealBattleBot == "1")
	{
		// Seal battle bot - use display name for death messages
		%dname = Client::getName(%damagedClient);
	}
	else
	{
		// Regular player or enemy bot - use GetClientOrBotName() for consistency
		// with damage tracking (same function used in Player::DealDamage)
		%dname = GetClientOrBotName(%damagedClient);
	}
	
	%dlvl = fetchData(%damagedClient, "LVL");

	// DEBUG: Log EXP distribution attempt
	%spawnBotInfo = fetchData(%damagedClient, "SpawnBotInfo");
	%isEnemyBot = (%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1);

	%count = 0;

	//parse $damagedBy and create %finalDamagedBy
	%nameCount = 0;
	%listCount = 0;
	%total = 0;
	for(%i = 1; %i <= $maxDamagedBy; %i++)
	{
		if($damagedBy[%dname, %i] != "")
		{
			%listCount++;

			%n = GetWord($damagedBy[%dname, %i], 0);
			%d = GetWord($damagedBy[%dname, %i], 1);

			%flag = 0;
			for(%z = 1; %z <= %nameCount; %z++)
			{
				if(%finalDamagedBy[%z] == %n)
				{
					%flag = 1;
					%dCounter[%n] += %d;
				}
			}
			if(%flag == 0)
			{
				%nameCount++;
				%finalDamagedBy[%nameCount] = %n;
				%dCounter[%n] = %d;

				%p = IsInWhichParty(%n);
				if(%p != -1)
				{
					%id = GetWord(%p, 0);
					%inv = GetWord(%p, 1);
					if(%inv == -1)
					{
						%tmppartylist[%id] = %tmppartylist[%id] @ %n @ " ";
						if(String::findSubStr(%tmpl, %id @ " ") == -1)
							%tmpl = %tmpl @ %id @ " ";
					}
				}
			}
			%total += %d;
		}
	}

	//clear $damagedBy (and the erase-schedule stamps - see DamagedByErase in playerdamage.cs)
	for(%i = 1; %i <= $maxDamagedBy; %i++)
	{
		$damagedBy[%dname, %i] = "";
		$damagedByStamp[%dname, %i] = "";
	}

	//parse thru all tmppartylists and determine the number of same party members involved in exp split
	for(%w = 0; (%a = GetWord(%tmpl, %w)) != -1; %w++)
	{
		%n = CountObjInList(%tmppartylist[%a]);
		for(%ww = 0; (%aa = GetWord(%tmppartylist[%a], %ww)) != -1; %ww++)
			%partyFactor[%aa] = %n;
	}

	//distribute exp
	for(%i = 1; %i <= %nameCount; %i++)
	{
		if(%finalDamagedBy[%i] != "")
		{
			%listClientId = NEWgetClientByName(%finalDamagedBy[%i]);

			// Shooter disconnected (or a bot despawned) between damaging and the
			// kill - skip them instead of storeData/fetchData against id -1
			if(%listClientId == -1 || %listClientId == "")
				continue;

			%slvl = fetchData(%listClientId, "LVL");

			if(isRPGAI(%damagedClient))
			{
				%f = (101 - %slvl) / 10;
				if(%slvl > 81) %f = 2;
				if(%slvl > 124) %f = 1.75;
				if(%slvl > 149) %f = 1.5;
				if(%slvl > 174) %f = 1.4;
				if(%slvl > 199) %f = 1.3;
				if(%slvl > 224) %f = 1.2;
				if(%slvl > 249) %f = 1.1;
				if(%slvl > 274) %f = 1;
				if(%slvl > 299) %f = 0.9;
				if(%slvl > 324) %f = 0.8;
				if(%slvl > 349) %f = 0.7;

				%a = (%dlvl - %slvl) + 8;
				%b = %a * %f;
				if(%b < 1) %b = 1;

				%z = %b * 0.10;
				%y = getRandom() * %z;
				%r = %y - (%z / 2);

				%c = %b + %r;
				
				// REMORT-AWARE EXPERIENCE MULTIPLIER
				// Maps player's progress toward their max level onto the original 150-950 scale
				// This ensures higher remort players get appropriate exp at high absolute levels
				%d = 1.9;
				%playerLevel = fetchData(%listClientId, "LVL");
				
				if(%playerLevel > 149)
				{
					%remortStep = fetchData(%listClientId, "RemortStep");
					%maxLevel = 125 + (%remortStep * 8);
					
					// Calculate progress from level 150 to max level (0.0 to 1.0)
					%levelRange = %maxLevel - 149;
					if(%levelRange > 0)
					{
						%progress = (%playerLevel - 149) / %levelRange;
						
						// Map progress onto original 150-950 scale (801 = 950 - 149)
						%effectiveLevel = 149 + (%progress * 801);
						
						// Apply original formula to effective level
						%e = floor(%effectiveLevel / 50);
						%d = 1.9 - (0.1 * %e);
					}
					
					// Clamp to minimum of 0.1 to prevent zero/negative exp
					if(%d < 0.1)
						%d = 0.1;
				}

				%value = (%c * %d) * (1 + (fetchData(%listClientId, "TournyRank") * 0.05));
				
				// ASCENSION: Experience Affinity (multiplicative, stacks before house bonus)
				%value = %value * Ascension::GetExpMultiplier(%listClientId);
			}
			else
			{
				%value = 0;
			}

			//rank point bonus
			if(fetchData(%listClientId, "MyHouse") != "")
			{
				%ph = Cap(GetRankBonus(%listClientId), 1.00, 3.00);
				%value = %value * %ph;
			}
			if(fetchData(%listClientId, "MyHouse") == "" && fetchData(%listClientId, "LVL") >= 60)
			{
				%value = 0;
				Client::sendMessage(%listClientId, 0, "You have gained no experience! You must join a house to continue growing stronger!~house");
			}
			if(fetchData(%listClientId, "LVL") >= 125+(fetchData(%listClientId,"RemortStep")*8))
			{
				%value = 0;
				%msg = "<jc>You can no longer gain experience. Your body has reached it's peak limit!";
				if(Client::getName(%listClientId) != "")
					centerprint(%listClientId, %msg, floor(String::len(%msg) / 20));
			}
			if(fetchData(%listClientId, "LVL") >= 125+(fetchData(%listClientId,"RemortStep")*8)) //this funciton lowers your lvl if you are over the cap
			{
				%lvllowerthing = (124000+(fetchData(%listClientId,"RemortStep")*8) * 1000);
				storeData(%listClientId, "EXP", %lvllowerthing);
	              		Game::refreshClientScore(%listClientid);
			}
			%perc = %dCounter[%finalDamagedBy[%i]] / %total;
			// Cap EXP per kill - multiplied by Ascension Affinity
			%expCap = (1000 + (fetchData(%listclientId, "RemortStep") * 50)) * Ascension::GetExpMultiplier(%listClientId);
			%final = floor(Cap(round( %value * %perc ), "inf", %expCap));  // floor: %expCap is a float multiplier and can leak decimals through the cap

			//determine party exp
			%pf = %partyFactor[%finalDamagedBy[%i]];
			if(%pf != "" && %pf >= 2)
				%pvalue = round(%final * (1.0 + (%pf * 0.3)));
			else
				%pvalue = 0;

			storeData(%listClientId, "EXP", %final, "inc");
			if(%final > 0)
				Client::sendMessage(%listClientId, 0, %dname @ " has died and you gained " @ Number::Beautify(%final, -3) @ " experience!");
			else if(%final < 0)
				Client::sendMessage(%listClientId, 0, %dname @ " has died and you lost " @ Number::Beautify(-%final, -3) @ " experience.");
			else if(%final == 0)
				Client::sendMessage(%listClientId, 0, %dname @ " has died.");

			if(%pvalue != 0)
			{
				storeData(%listClientId, "EXP", %pvalue, "inc");
				Client::sendMessage(%listClientId, $MsgWhite, "You have gained " @ Number::Beautify(%pvalue, -3) @ " party experience!");
			}

			Game::refreshClientScore(%listClientId);
		}
	}
}

function StartStatSelection(%clientId)
{
	dbecho($dbechoMode, "StartStatSelection(" @ %clientId @ ")");

	%group = nameToId("MissionGroup\\ObserverDropPoints");
	%observerMarker = Group::getObject(%group, 0);
	
	Client::setControlObject(%clientId, Client::getObserverCamera(%clientId));
	Observer::setFlyMode(%clientId, GameBase::getPosition(%observerMarker), GameBase::getRotation(%observerMarker), false, true);

	storeData(%clientId, "SPcredits", $initSPcredits);

	MenuGroup(%clientId);
}

function Game::refreshClientScore(%clientId)
{
	dbecho($dbechoMode2, "Game::refreshClientScore(" @ %clientId @ ")");

	if(fetchData(%clientId, "HasLoadedAndSpawned"))
	{
		if(GetLevel(fetchData(%clientId, "EXP"), %clientId) != fetchData(%clientId, "templvl") && fetchData(%clientId, "HasLoadedAndSpawned") && fetchData(%clientId, "templvl") != "")
		{
			//client has leveled up
			%lvls = (GetLevel(fetchData(%clientId, "EXP"), %clientId) - fetchData(%clientId, "templvl"));

			storeData(%clientId, "SPcredits", (%lvls * $SPgainedPerLevel), "inc");

			if(%lvls > 0)
			{
				if(%lvls == 1)
					Client::sendMessage(%clientId,0,"You have gained a level!");		
				else
					Client::sendMessage(%clientId,0,"You have gained " @ %lvls @ " levels!");
				Client::sendMessage(%clientId,0,"Welcome to level " @ fetchData(%clientId, "LVL"));
				PlaySound(SoundLevelUp, GameBase::getPosition(%clientId));
				
				// Auto-spend SP on priority skills
				AutoSkill_Process(%clientId);
			}

			else if(%lvls < 0)
			{
				if(%lvls == -1)
					Client::sendMessage(%clientId,0,"You have lost a level...");		
				else
					Client::sendMessage(%clientId,0,"You have lost " @ -%lvls @ " levels...");
				Client::sendMessage(%clientId,0,"You are now level " @ fetchData(%clientId, "LVL"));
			}
		}
		storeData(%clientId, "templvl", GetLevel(fetchData(%clientId, "EXP"), %clientId));
	}

	%z = Zone::getDesc(fetchData(%clientId, "zone"));
	if(%z == -1)
		%z = "unknown";
	// ESTATE ZONES 2026-07-15: DISPLAY-ONLY estate grounds name in the zone
	// column. Deliberately does NOT touch the real zone var (several consumers
	// assume numeric FolderIDs - e.g. the bot cross-zone melee guard); this is
	// pure presentation. %clientId.estateZone is maintained by Estate::ZoneLoop.
	if($pref::EstatesEnabled && %clientId.estateZone != "" && $Estate::Owner[%clientId.estateZone] != "")
		%z = Estate::ZoneName(%clientId.estateZone);
	//By Carling!
	%name = client::getname(%clientId);
	if($zonedis[%name] != "")
		%z = "-" @ $zonedis[%name] @ "-";

	if($displayPingAndPL)
		Client::setScore(%clientId, "%n\t" @ %z @ "\t  " @ fetchData(%clientId, "LVL") @ "\t%p\t%l", fetchData(%clientId, "LVL"));
	else
	{
            Client::setScore(%clientId, "%n\t" @ %z @ "\t  " @ fetchData(%clientId, "LVL") @ "\t" @ getFinalCLASS(%clientId) @ " RL" @ fetchData(%clientId, "RemortStep") @ "\t%l", fetchData(%clientId, "LVL"));
	}

	// Push stats to ScriptGL KronosHUD
	KronosHUD_Push(%clientId);
}

function DoRemort(%clientId)
{
	dbecho($dbechoMode, "DoRemort(" @ %clientId @ ")");

	storeData(%clientId, "RemortStep", 1, "inc");

	storeData(%clientId, "EXP", 0);
	storeData(%clientId, "templvl", 1);
	storeData(%clientId, "LCK", 1, "inc");
	storeData(%clientId, "SPcredits", $initSPcredits, "inc");
	storeData(%clientId, "currentlyRemorting", "");
	storeData(%clientId, "RankPoints", 0, "inc");

	//skill variables
	%cnt = 0;
	for(%i = 1; %i <= GetNumSkills(); %i++)
	{
		$PlayerSkill[%clientId, %i] = 0;
		$SkillCounter[%clientId, %i] = 0;
	}
	for(%i = 1; %i <= getNumSkills(); %i++)
		AddSkillPoint(%clientId, %i, $autoStartupSP);

	// Unequip dual-wielded weapon before remort (returns to inventory)
	// (Toggle mode is preserved - Ascension talent persists through remort)
	DualWield::UnequipOffHand(%clientId);
	
	// Unequip all belt accessories and armor before remort
	// This prevents stat bonuses from carrying over to level 1
	%equippedAccessories = fetchData(%clientId, "EquippedBeltAccessories");
	for(%i = 0; GetWord(%equippedAccessories, %i) != -1; %i++)
	{
		%accessory = GetWord(%equippedAccessories, %i);
		if(Belt::IsAccessoryEquipped(%clientId, %accessory))
			Belt::UnequipAccessory(%clientId, %accessory);
	}
	
	%equippedArmor = fetchData(%clientId, "EquippedBeltArmor");
	if(%equippedArmor != "" && %equippedArmor != "0")
		Belt::UnequipArmor(%clientId, %equippedArmor);
	
	UnequipMountedStuff(%clientId);
	
	Player::setDamageFlash(%clientId, 1.0);
	Item::setVelocity(%clientId, "0 0 0");
	%pos = TeleportToMarker(%clientId, "Teams/team0/DropPoints", 0, 0);

	playSound(RespawnC, GameBase::getPosition(%clientId));
	
	RefreshAll(%clientId);

	// Process auto-skill spending after remort (player has fresh SP credits)
	AutoSkill_Process(%clientId);

	Client::sendMessage(%clientId, $MsgBeige, "Welcome to Remort Level " @ fetchData(%clientId, "RemortStep") @ "! Your stats have all increased!");

	return %pos;
}

function GetRankBonus(%clientId)
{
	dbecho($dbechoMode, "GetRankBonus(" @ %clientId @ ")");

	return 1 + ( fetchData(%clientId, "RankPoints") / 100 );
}
