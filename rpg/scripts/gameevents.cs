function Mission::init()
{
	dbecho($dbechoMode, "Mission::init()");

	if($displayPingAndPL)
		setClientScoreHeading("Name\t\x50Zone\t\xDFLVL\t\xFFPing\t");
	else
		setClientScoreHeading("Name\t\x50Zone\t\xD5LVL\t\xFFClass\t");

	if(!$NoSpawn)
		AI::setupAI();

	//schedule("echo(\".--==< RecursiveWorld STARTED >==--.\");RecursiveWorld(1);", 60);

	echo(".--==< RecursiveWorld STARTED >==--.");
	RecursiveWorld(5);
	RecursiveZone(2);
	// Visibility safety net (playerspawn.cs) - must start here, not at exec time:
	// schedules made before mission load are flushed by the engine
	Game::StartVisibilitySafetyLoop();
	// Daily quest rotation loop (DailyQuest.cs) - same rule: must start here
	Daily::Init();

	$BlockOwnerAdminLevel[Server] = 5;
	for(%i = 1; $ServerQuest[%i] != ""; %i++)
		remoteSay(2048, 0, $ServerQuest[%i], "Server");
}

function Game::startMatch()
{
	dbecho($dbechoMode, "Game::startMatch()");

	$matchStarted = true;
	$missionStartTime = getSimTime();

	//for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	//	Game::refreshClientScore(%cl);
}

function Player::enterMissionArea(%player)
{
}

function Player::leaveMissionArea(%player)
{
}

function RecursiveWorld(%seconds)
{
	dbecho($dbechoMode, "RecursiveWorld(" @ %seconds @ ")");

	//This function is a substitute for a few recursive schedule calls.  By having all schedule calls replaced by
	//this huge one, there should be less cause for lag.  As a standard, the RecursiveWorld should be called every
	//5 seconds.

	//(note, spawn crystal loop is not in this function, because I judge it causes less lag when used separately)

	$ticker[1] = floor($ticker[1]+1);
	$ticker[2] = floor($ticker[2]+1);
	$ticker[3] = floor($ticker[3]+1);
	$ticker[4] = floor($ticker[4]+1);
	$ticker[5] = floor($ticker[5]+1);
	$ticker[6] = floor($ticker[6]+1);
	$ticker[7] = floor($ticker[7]+1);
	$ticker[8] = floor($ticker[8]+1);
	// $ticker[9] removed - was unused

	if($ticker[1] >= ($SaveWorldFreq / %seconds))
	{
		// Check if players are falling off the map (bots already die when leaving zones)
		%list = GetPlayerIdList();  // Only check players, not bots
		for(%i = 0; GetWord(%list, %i) != -1; %i++)
		{
			%id = GetWord(%list, %i);
			%vel = Item::getVelocity(%id);
			if(getWord(%vel, 2) <= -500)
			{
				FellOffMap(%id);
			}
		}

		//Save World call
		RequestWorldSave("autosave_ticker", 0, "full");

		$ticker[1] = 0;
	}
	if($ticker[2] >= ($ChangeWeatherFreq / %seconds))
	{
		//change weather call
		ChangeWeather();

		$ticker[2] = 0;
	}
	if($ticker[3] >= 60 && $nightDayCycle)  // Every 5 minutes (60 ticks × 5s = 300s)
	{
		// Multiply by 60 since we now update every 60 ticks instead of every 1 tick
		%a = (($initHaze * 2) / $fullCycleTime) * %seconds * 60;

		$currentHaze -= %a;

		if($currentHaze < 0)
			%h = -$currentHaze;
		else
			%h = $currentHaze;

		if($currentHaze < -$initHaze)
			$currentHaze = $initHaze;

		setTerrainVisibility(8, 800, %h);

		//-------

		for(%i = 1; %i <= 5; %i++)
		{
			if($currentHaze >= $dayCycleHaze[%i] && $currentHaze <= $dayCycleHaze[%i-1])
			{
				if($currentSky != $dayCycleSky[%i])
				{
					$currentSky = $dayCycleSky[%i];
					ChangeSky($currentSky);
					break;
				}
			}
		}

		$ticker[3] = 0;
	}

	//arena schedules
	if($DoCheckMatchWin)
	{
		$ticker[4]++;
		if($ticker[4] >= 1)
		{
			//this part is if the match is only bots, then there is a time limit for the fight
			if($IsABotMatch)
			{
				$ArenaBotMatchTicker++;
				if($ArenaBotMatchTicker >= $ArenaBotMatchLengthInTicks)
				{
					//bots have been fighting for too long, kill em all off so the next match can take place.
					for(%i = 1; %i <= $maxroster; %i++)
					{
						%c = GetWord($ArenaDueler[%i], 0);
						%s = GetWord($ArenaDueler[%i], 1);
						if(%s == "ALIVE")
						{
							storeData(%c, "noDropLootbagFlag", True);
							playNextAnim(%c);
							Player::Kill(%c);
						}
					}
					$ArenaBotMatchTicker = 0;
					$IsABotMatch = False;

					StringArenaTextBox("Bot match was cut short.");
				}
			}

			if(CheckMatchWin())
			{
				$DoCheckMatchWin = False;
				$ArenaBotMatchTicker = 0;
				ClearArenaDueler();
				ScheduleArenaMatch();
			}

			$ticker[4] = 0;
		}
	}

	if($ticker[5] >= ($RecalcEconomyDelay) / %seconds)
	{
		//re-evaluate economy - OPTIMIZED: Only iterate spawned town merchants via $TownBotRegistry
		// instead of all bots (enemy + town) via GetBotIdList()
		
		for(%i = 0; (%aiName = GetWord($TownBotRegistry, %i)) != -1; %i++)
		{
			// Only process merchants (bots with SHOP defined)
			if($BotInfo[%aiName, SHOP] != "")
			{
				// Check if this merchant is currently spawned
				%clientId = $TownBotSpawned[%aiName];
				if(%clientId == "" || %clientId == -1)
					continue;  // Skip merchants that aren't spawned
				
				%max = getNumItems();
				for(%z = 0; %z < %max; %z++)
				{
					%checkItem = getItemData(%z);

					%p = GetItemCost(%checkItem);
					%q = GetItemCost(%checkItem) * ($resalePercentage/100);

					%b = $MerchantCounterB[%aiName, %checkItem];
					%s = $MerchantCounterS[%aiName, %checkItem];

					%constantB = 100;
					%constantS = 75;

					%x = round( %p - (%p * (%b/%constantB)) );
					%y = round( %q - (%q * (%s/%constantS)) );

					if(%x < 1) %x = 1;
					if(%y >= %p) %y = %p-1;

					$NewItemBuyCost[%aiName, %checkItem] = %x;
					$NewItemSellCost[%aiName, %checkItem] = %y;

					//reset counter
					$MerchantCounterB[%aiName, %checkItem] = "";
					$MerchantCounterS[%aiName, %checkItem] = "";
				}
			}
		}
		//messageAll($MsgBeige, "The merchants have revised their prices.");

		$ticker[5] = 0;
	}
	if($ticker[6] >= (300 / %seconds))
	{
		$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;	//thanks Presto

		//check for tmpPrize.cs, execute, and delete it.
		if(isFile("config\\tmpPrize.cs"))
		{
			$pAcnt = "";
			$pBcnt = "";

			//Make sure the stupid exec file gets exec'd...
			//Note: still doesn't work.  exec sucks.
			%goFlag = "";
			for(%i = 1; %i <= 2; %i++)
			{
				if(exec("tmpPrize.cs"))
				{
					%goFlag = True;
					break;
				}
				else
					$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;	//thanks Presto
			}

			if(%goFlag)
			{
				File::delete("config\\tmpPrize.cs");

				for(%i = 1; $PrizeA[%i] != ""; %i++)
				{
					OnOrOfflineGive($PrizeA[%i], "Trancephyte 1");
					$PrizeA[%i] = "";
				}
				for(%i = 1; $PrizeB[%i] != ""; %i++)
				{
					OnOrOfflineGive($PrizeB[%i], "Trancephyte 1 MagicDust 1");
					$PrizeB[%i] = "";
				}
				$pAcnt = "";
				$pBcnt = "";
			}
		}

		if($dedicated)
		{
			//rpgserv check
			%badFlag = "";
			if(isFile("config\\tmpData.cs"))
			{
				$tmpdata = "";
				if(exec("tmpData.cs"))
				{
					File::delete("config\\tmpData.cs");

					if($tmpdata != "160")
						%badFlag = True;

					$tmpdata = "";
				}
				else
					%badFlag = True;
			}
			else
				%badFlag = True;

			if(!%badFlag)
				$isRpgserv = True;
			else
				$isRpgserv = "";
		}

		//exec external file on server
		//useful for changing many variables while the server is running without having to type them at the console.
		if(isFile("temp\\[exec].cs"))
			exec("[exec].cs");

		$ticker[6] = 0;
	}
	if($ticker[7] >= (60 / %seconds))  // Every 60 seconds (was 20s)
	{
		//re-init the sound points.
		InitSoundPoints();

		$ticker[7] = 0;
	}

	if($ticker[8] >= (900 / %seconds))
	{
		//House payments
		HouseEarnings();

		$ticker[8] = 0;
	}
	//Call itself again, %seconds later.
	schedule("RecursiveWorld(" @ %seconds @ ");", %seconds);
}
function ScheduleSave(%clientId)
{
	// This function is deprecated - SaveCharacter is now called directly from RecursiveWorld
	// Keeping function for backwards compatibility but removing message
	SaveCharacter(%clientId);
}

function TrimIP(%ip)
{
	%a = String::getSubStr(%ip, 3, 99999);
	%p = String::findSubStr(%a, ":");
	%z = String::getSubStr(%a, 0, %p);

	return %z;
}
function HouseEarnings()
{
	// Clamp BaseControl and FlagCommand to prevent negative values causing negative house bonuses
	if($BaseControl[HouseKronos] < 0)
		$BaseControl[HouseKronos] = 0;
	if($BaseControl[HouseArbal] < 0)
		$BaseControl[HouseArbal] = 0;
	if($BaseControl[HouseCurama] < 0)
		$BaseControl[HouseCurama] = 0;
	if($BaseControl[HouseYuliple] < 0)
		$BaseControl[HouseYuliple] = 0;
	if($FlagCommand[HouseKronos] < 0)
		$FlagCommand[HouseKronos] = 0;
	if($FlagCommand[HouseArbal] < 0)
		$FlagCommand[HouseArbal] = 0;
	if($FlagCommand[HouseCurama] < 0)
		$FlagCommand[HouseCurama] = 0;
	if($FlagCommand[HouseYuliple] < 0)
		$FlagCommand[HouseYuliple] = 0;
	
	// Calculate TOTAL bases and flags controlled across ALL houses
	// This is used to calculate each house's SHARE of control
	%totalBases = $BaseControl[HouseKronos] + $BaseControl[HouseArbal] + $BaseControl[HouseCurama] + $BaseControl[HouseYuliple];
	%totalFlags = $FlagCommand[HouseKronos] + $FlagCommand[HouseArbal] + $FlagCommand[HouseCurama] + $FlagCommand[HouseYuliple];
	%totalObjectives = %totalBases + %totalFlags;
	
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		// CRITICAL: Skip bots - they should not receive house earnings
		if(isRPGAI(%cl) || Player::isAiControlled(%cl))
			continue;
		
		%house = fetchData(%cl, "MyHouse");
		if(%house != "")
		{
			%baseCount = $BaseControl[%house];
			%flagCount = $FlagCommand[%house];
			
			// Ensure values are non-negative (safety check)
			if(%baseCount < 0)
				%baseCount = 0;
			if(%flagCount < 0)
				%flagCount = 0;
			
			%remortStep = fetchData(%cl, "RemortStep");
			
			// Calculate this house's SHARE of total objectives (0.0 to 1.0)
			// If no objectives exist at all, everyone gets minimum (0% share)
			%houseObjectives = %baseCount + %flagCount;
			if(%totalObjectives > 0)
				%controlShare = %houseObjectives / %totalObjectives;
			else
				%controlShare = 0;
			
			// REWARD SYSTEM BASED ON RELATIVE CONTROL:
			// - 100% control = 100% of maximum reward
			// - 50% control = 50% of maximum reward  
			// - 0% control = 25% of maximum reward (base/minimum)
			// 
			// Multiplier ranges from 0.25 (minimum) to 1.0 (maximum)
			// Formula: 0.25 + (0.75 * controlShare)
			%shareMultiplier = 0.25 + (0.75 * %controlShare);
			
			// Calculate maximum possible rewards (what you'd get with 100% control)
			%maxCoinReward = 15000 * (%remortStep + 1);  // Base coin reward at full control
			%maxExpReward = 300 * %remortStep;           // EXP scales with remort
			%maxRankReward = 2;                          // Max rank points (for controlling all 8 objectives)
			%maxSkillReward = 1 * %remortStep;           // Skill points scale with remort
			
			// Apply share multiplier to get actual rewards
			%reward = floor(%maxCoinReward * %shareMultiplier);
			%expReward = floor(%maxExpReward * %shareMultiplier);
			%rpreward = floor(%maxRankReward * %shareMultiplier);
			%spreward = floor(%maxSkillReward * %shareMultiplier);
			
			// Ensure minimum rewards are never negative
			if(%reward < 0)
				%reward = 0;
			if(%expReward < 0)
				%expReward = 0;
			if(%rpreward < 0)
				%rpreward = 0;
			if(%spreward < 0)
				%spreward = 0;
			
			// Check if player is at max level - if so, don't give EXP (prevents going past max level)
			%playerLevel = fetchData(%cl, "LVL");
			%maxLevel = %remortStep * 4 + 100;
			%shouldGiveExp = true;
			if(%playerLevel >= %maxLevel)
			{
				%shouldGiveExp = false;
				%expReward = 0; // Set to 0 so normal message doesn't show
			}
			
			storeData(%cl, "BANK", %reward, "inc");
			if(%shouldGiveExp)
				storeData(%cl, "EXP", %expReward, "inc");
			storeData(%cl, "RankPoints", %rpreward, "inc");
			storeData(%cl, "SPcredits", %spreward, "inc");
			
			// Calculate share percentage for display
			%sharePercent = floor(%controlShare * 100);
			
			if(%reward > 0)
				Client::sendMessage(%cl, $MsgBeige, "You received " @ %reward @ " coins from your house. (" @ %sharePercent @ "% objective control)");
			if(%expReward > 0)
				Client::sendMessage(%cl, $MsgBeige, "You received " @ %expReward @ " experience points for being loyal to your house.");
			else if(!%shouldGiveExp)
				Client::sendMessage(%cl, $MsgBeige, "You are currently able to remort, and your house's leadership has cut you off from this perk. They suggest either remorting, or finding someone else to freeload from.");
			if(%rpreward > 0)
				Client::sendMessage(%cl, $MsgBeige, "You received " @ %rpreward @ " rank points for being loyal to your house.");
			if(%spreward > 0)
				Client::sendMessage(%cl, $MsgBeige, "You received " @ %spreward @ " skill points for being loyal to your house.");
			RefreshAll(%cl);
		}
	}
	
	// Save all player characters after house earnings are distributed
	%list = GetPlayerIdList();
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
	}
}
