//rpg admin

$curVoteTopic = "";
$curVoteAction = "";
$curVoteOption = "";
$curVoteCount = 0;

function Admin::changeMissionMenu(%clientId)
{
return;
}

function processMenuCMType(%clientId, %options)
{
return;
}

function processMenuCMission(%clientId, %option)
{
return;
}

function remoteAdminPassword(%clientId, %password)
{
return;
}


function remoteSetPassword(%clientId, %password)
{
return;
}

function remoteSetTimeLimit(%clientId, %time)
{
return;
}

function remoteSetTeamInfo(%clientId, %team, %teamName, %skinBase)
{
return;
}

function remoteVoteYes(%clientId)
{
   %clientId.vote = "yes";
   centerprint(%clientId, "", 0);
}

function remoteVoteNo(%clientId)
{
   %clientId.vote = "no";
   centerprint(%clientId, "", 0);
}

function Admin::startMatch(%admin)
{
return;
}

function Admin::setTeamDamageEnable(%admin, %enabled)
{
return;
}

function Admin::kick(%admin, %clientId, %ban)
{
   if(%admin == -1 || %admin.adminLevel >= 4)
   {
      if(%ban && %admin.adminLevel < 5)
         return;
         
      if(%ban)
      {
         %word = "banned";
         %cmd = "BAN: ";
      }
      else
      {
         %word = "kicked";
         %cmd = "KICK: ";
      }
      if(%clientId.adminLevel >= 5)
      {
         if(%admin == -1)
            messageAll(0, "A super admin cannot be " @ %word @ ".");
         else
            Client::sendMessage(%admin, 0, "A super admin cannot be " @ %word @ ".");
         return;
      }
      %ip = Client::getTransportAddress(%clientId);

      echo(%cmd @ %admin @ " " @ %clientId @ " " @ %ip);

      if(%ip == "")
         return;
      if(%ban)
         BanList::add(%ip, 1800);
      else
         BanList::add(%ip, 180);

      %name = Client::getName(%clientId);

      if(%admin == -1)
      {
         MessageAll(0, %name @ " was " @ %word @ " from vote.");
         Net::kick(%clientId, "You were " @ %word @ " by  consensus.");
      }
      else
      {
         MessageAll(0, %name @ " was " @ %word @ " by " @ Client::getName(%admin) @ ".");
         Net::kick(%clientId, "You were " @ %word @ " by " @ Client::getName(%admin));
      }
   }
}

function Admin::setModeFFA(%clientId)
{
return;
}

function Admin::setModeTourney(%clientId)
{
return;
}

function Admin::voteFailed()
{
   $curVoteInitiator.numVotesFailed++;

   if($curVoteAction == "kick" || $curVoteAction == "admin")
      $curVoteOption.voteTarget = "";
}
function Admin::voteSucceded()
{
   $curVoteInitiator.numVotesFailed = "";
   if($curVoteAction == "kick")
   {
      if($curVoteOption.voteTarget)
         Admin::kick(-1, $curVoteOption);
   }
   else if($curVoteAction == "admin")
   {
      if($curVoteOption.voteTarget)
      {
         $curVoteOption.adminLevel = 4;
         messageAll(0, Client::getName($curVoteOption) @ " has become an administrator.");
         if($curVoteOption.menuMode == "options")
            Game::menuRequest($curVoteOption);
      }
      $curVoteOption.voteTarget = false;
   }
   else if($curVoteAction == "cmission")
   {
      messageAll(0, "Changing to mission " @ $curVoteOption @ ".");
		Vote::changeMission();
      Server::loadMission($curVoteOption);
   }
   else if($curVoteAction == "tourney")
      Admin::setModeTourney(-1);
   else if($curVoteAction == "ffa")
      Admin::setModeFFA(-1);
   else if($curVoteAction == "etd")
      Admin::setTeamDamageEnable(-1, true);
   else if($curVoteAction == "dtd")
      Admin::setTeamDamageEnable(-1, false);
   else if($curVoteOption == "smatch")
      Admin::startMatch(-1);
}

function Admin::countVotes(%curVote)
{
   // if %end is true, cancel the vote either way
   if(%curVote != $curVoteCount)
      return;

   %votesFor = 0;
   %votesAgainst = 0;
   %votesAbstain = 0;
   %totalClients = 0;
   %totalVotes = 0;
   for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
   {
      %totalClients++;
      if(%cl.vote == "yes")
      {
         %votesFor++;
         %totalVotes++;
      }
      else if(%cl.vote == "no")
      {
         %votesAgainst++;
         %totalVotes++;
      }
      else
         %votesAbstain++;
   }
   %minVotes = floor($Server::MinVotesPct * %totalClients);
   if(%minVotes < $Server::MinVotes)
      %minVotes = $Server::MinVotes;

   if(%totalVotes < %minVotes)
   {
      %votesAgainst += %minVotes - %totalVotes;
      %totalVotes = %minVotes;
   }
   %margin = $Server::VoteWinMargin;
   if($curVoteAction == "admin")
   {
      %margin = $Server::VoteAdminWinMargin;
      %totalVotes = %votesFor + %votesAgainst + %votesAbstain;
      if(%totalVotes < %minVotes)
         %totalVotes = %minVotes;
   }
   if(%votesFor / %totalVotes >= %margin)
   {
      messageAll(0, "Vote to " @ $curVoteTopic @ " passed: " @ %votesFor @ " to " @ %votesAgainst @ " with " @ %totalClients - (%votesFor + %votesAgainst) @ " abstentions.");
      Admin::voteSucceded();
   }
   else  // special team kick option:
   {
      if($curVoteAction == "kick") // check if the team did a majority number on him:
      {
         %votesFor = 0;
         %totalVotes = 0;
         for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
         {
            if(GameBase::getTeam(%cl) == $curVoteOption.kickTeam)
            {
               %totalVotes++;
               if(%cl.vote == "yes")
                  %votesFor++;
            }
         }
         if(%totalVotes >= $Server::MinVotes && %votesFor / %totalVotes >= $Server::VoteWinMargin)
         {
            messageAll(0, "Vote to " @ $curVoteTopic @ " passed: " @ %votesFor @ " to " @ %totalVotes - %votesFor @ ".");
            Admin::voteSucceded();
            $curVoteTopic = "";
            return;
         }
      }
      messageAll(0, "Vote to " @ $curVoteTopic @ " did not pass: " @ %votesFor @ " to " @ %votesAgainst @ " with " @ %totalClients - (%votesFor + %votesAgainst) @ " abstentions.");
      Admin::voteFailed();
   }
   $curVoteTopic = "";
}

function Admin::startVote(%clientId, %topic, %action, %option)
{
   if(%clientId.lastVoteTime == "")
      %clientId.lastVoteTime = -$Server::MinVoteTime;

   // we want an absolute time here.
   %time = getIntegerTime(true) >> 5;
   %diff = %clientId.lastVoteTime + $Server::MinVoteTime - %time;

   if(%diff > 0)
   {
      Client::sendMessage(%clientId, 0, "You can't start another vote for " @ floor(%diff) @ " seconds.");
      return;
   }
   if($curVoteTopic == "")
   {
      if(%clientId.numFailedVotes)
         %time += %clientId.numFailedVotes * $Server::VoteFailTime;

      %clientId.lastVoteTime = %time;
      $curVoteInitiator = %clientId;
      $curVoteTopic = %topic;
      $curVoteAction = %action;
      $curVoteOption = %option;
      if(%action == "kick")
         $curVoteOption.kickTeam = GameBase::getTeam($curVoteOption);
      $curVoteCount++;
      bottomprintall("<jc><f1>" @ Client::getName(%clientId) @ " <f0>initiated a vote to <f1>" @ $curVoteTopic, 10);
      for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
         %cl.vote = "";
      %clientId.vote = "yes";
      for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
         if(%cl.menuMode == "options")
            Game::menuRequest(%clientId);
      schedule("Admin::countVotes(" @ $curVoteCount @ ", true);", $Server::VotingTime, 35);
   }
   else
   {
      Client::sendMessage(%clientId, 0, "Voting already in progress.");
   }
}

function Game::menuRequest(%clientId)
{
	if(%clientId.IsInvalid)
		return;

	if(%clientId.choosingGroup)
	{
		MenuGroup(%clientId);
		return;
	}
	else if(%clientId.choosingClass)
	{
		MenuClass(%clientId);
		return;
	}

	%curItem = 0;
	Client::buildMenu(%clientId, "Options", "options", true);
	if($curVoteTopic != "" && %clientId.vote == "")
	{
		Client::addMenuItem(%clientId, %curItem++ @ "Vote YES to " @ $curVoteTopic, "voteYes " @ $curVoteCount);
		Client::addMenuItem(%clientId, %curItem++ @ "Vote NO to " @ $curVoteTopic, "voteNo " @ $curVoteCount);
	}
	else
	{
		if(%clientId.selClient)
		{
			%sel = %clientId.selClient;
			%selname = Client::getName(%sel);
	
			if(%clientId != %sel && fetchData(%sel, "HasLoadedAndSpawned"))
			{
                        if(IsInCommaList(fetchData(%clientId, "grouplist"), %selname))
					Client::addMenuItem(%clientId, %curItem++ @ "Remove from group-list", "remgroup " @ %sel);
				else
					Client::addMenuItem(%clientId, %curItem++ @ "Add to group-list", "addgroup " @ %sel);

                        if(IsInCommaList(fetchData(%clientId, "targetlist"), %selname))
					Client::addMenuItem(%clientId, %curItem++ @ "Remove from target-list", "remtarget " @ %sel);
				else
					Client::addMenuItem(%clientId, %curItem++ @ "Add to target-list", "addtarget " @ %sel);

				if(fetchData(%clientId, "partyOwned"))
				{
					if(IsInCommaList(fetchData(%clientId, "partylist"), %selname))
						Client::addMenuItem(%clientId, %curItem++ @ "Remove from your party", "remparty " @ %sel);
					else
					{
						if(CountObjInCommaList(fetchData(%clientId, "partylist")) < $maxpartymembers)
						{
							%p = IsInWhichParty(Client::getName(%sel));
							if(%p == -1)
								Client::addMenuItem(%clientId, %curItem++ @ "Invite to your party", "addparty " @ %sel);
							else if(GetWord(%p, 1) == "i")
								Client::addMenuItem(%clientId, %curItem++ @ "Cancel invitation", "cancelinv " @ %sel);
							else
								Client::addMenuItem(%clientId, %curItem++ @ "(Can't invite, already in a party)", "");
						}
						else
							Client::addMenuItem(%clientId, %curItem++ @ "(Can't invite, too many members)", "");
					}
				}

				if(%clientId.muted[%sel])
					Client::addMenuItem(%clientId, %curItem++ @ "Unmute", "unmute " @ %sel);
				else
					Client::addMenuItem(%clientId, %curItem++ @ "Mute", "mute " @ %sel);

			}
		}
		else
		{
			if(!IsDead(%clientId))
				Client::addMenuItem(%clientId, %curItem++ @ "View Your Stats" , "viewstats");

			if(!IsDead(%clientId))
				Client::addMenuItem(%clientId, %curItem++ @ "Backpack","viewbackpack");

			// Commented out - Ranged weapons menu option (no ranged weapons in game)
			//if(GetAccessoryList(%clientId, 9, -1) != "")
			//	Client::addMenuItem(%clientId, %curItem++ @ "Ranged weapons..." , "rweapons");
	
			if(!IsDead(%clientId))
				Client::addMenuItem(%clientId, %curItem++ @ "Skill Points..." , "sp");

			// Commented out - Turn Ignore #Global menu option
			//if(fetchData(%clientId, "ignoreGlobal"))
			//	Client::addMenuItem(%clientId, %curItem++ @ "Turn Ignore #Global OFF" , "gignoreoff");
			//else
			//	Client::addMenuItem(%clientId, %curItem++ @ "Turn Ignore #Global ON" , "gignoreon");

			if(fetchData(%clientId, "LCKconsequence") == "miss")
				Client::addMenuItem(%clientId, %curItem++ @ "Current LUCK Mode: Miss" , "lckdeath");
			else if(fetchData(%clientId, "LCKconsequence") == "death")
				Client::addMenuItem(%clientId, %curItem++ @ "Current Luck Mode: Death" , "lckmiss");

			Client::addMenuItem(%clientId, %curItem++ @ "Party Options..." , "partyoptions");
	
			Client::addMenuItem(%clientId, %curItem++ @ "Stances" , "menustance");

			Client::addMenuItem(%clientId, %curItem++ @ "Help Commands..." , "helpcommands");

			// Show current damage display type
			// Read all damage display values to ensure we have the correct state
			%damageDisplayType = fetchData(%clientId, "damageDisplayType");
			%floatingAnimationStyle = fetchData(%clientId, "floatingAnimationStyle");
			%floatingDamageNumbers = fetchData(%clientId, "floatingDamageNumbers");
			
			// If damageDisplayType is empty, check floatingDamageNumbers to infer the type
			if(%damageDisplayType == "" || %damageDisplayType == -1)
			{
				// Check if floating is enabled via the flag
				if(%floatingDamageNumbers == "1" || %floatingDamageNumbers == "true" || (%floatingDamageNumbers == "" && %floatingAnimationStyle != ""))
				{
					%damageDisplayType = "floating";
					// Set default style if missing
					if(%floatingAnimationStyle == "" || %floatingAnimationStyle == -1)
						%floatingAnimationStyle = "float";
					storeData(%clientId, "damageDisplayType", "floating");
					storeData(%clientId, "floatingAnimationStyle", %floatingAnimationStyle);
					storeData(%clientId, "floatingDamageNumbers", "1");
				}
				else
				{
					%damageDisplayType = "bottomprint"; // Default to bottomprint
					storeData(%clientId, "damageDisplayType", "bottomprint");
				}
			}
			
			if(%damageDisplayType == "bottomprint")
				Client::addMenuItem(%clientId, %curItem++ @ "Damage Display: Bottomprint" , "toggledamagedisplay");
			else if(%damageDisplayType == "chat")
				Client::addMenuItem(%clientId, %curItem++ @ "Damage Display: Chat" , "toggledamagedisplay");
			else if(%damageDisplayType == "floating")
			{
				%floatingStyle = fetchData(%clientId, "floatingAnimationStyle");
				// If style is empty, check if floatingDamageNumbers is enabled to determine if floating was intended
				if(%floatingStyle == "")
				{
					%floatingEnabled = fetchData(%clientId, "floatingDamageNumbers");
					if(%floatingEnabled == "1" || %floatingEnabled == "true" || %floatingEnabled == "")
					{
						%floatingStyle = "float"; // Default style
						storeData(%clientId, "floatingAnimationStyle", "float");
					}
					else
					{
						%floatingStyle = "float"; // Default fallback
					}
				}
				// Convert old styles to new ones
				if(%floatingStyle == "redmoon" || %floatingStyle == "wow")
				{
					%floatingStyle = "float"; // Convert old styles to float
					storeData(%clientId, "floatingAnimationStyle", "float");
				}
				if(%floatingStyle == "float")
					Client::addMenuItem(%clientId, %curItem++ @ "Damage Display: Float" , "toggledamagedisplay");
				else if(%floatingStyle == "test")
					Client::addMenuItem(%clientId, %curItem++ @ "Damage Display: FloatStyle2" , "toggledamagedisplay");
				else
				{
					// Unknown style - default to float and save it
					%floatingStyle = "float";
					storeData(%clientId, "floatingAnimationStyle", "float");
					Client::addMenuItem(%clientId, %curItem++ @ "Damage Display: Float" , "toggledamagedisplay");
				}
			}
			else
				Client::addMenuItem(%clientId, %curItem++ @ "Damage Display: Bottomprint" , "toggledamagedisplay");

			// Commented out - Current Default Talk menu option
			// Show current default talk setting
			//%currentDefaultTalk = fetchData(%clientId, "defaultTalk");
			//if(%currentDefaultTalk == "")
			//	%currentDefaultTalk = "#say"; // Default value if not set
			//
			// Determine next option to cycle to when clicked
			//if(%currentDefaultTalk == "#say")
			//	%nextOption = "defglobal";
			//else if(%currentDefaultTalk == "#global" && fetchData(%clientId, "MyHouse") != "")
			//	%nextOption = "defhouse";
			//else
			//	%nextOption = "defsay";
			//
			//Client::addMenuItem(%clientId, %curItem++ @ "Current Default Talk: " @ %currentDefaultTalk , %nextOption);
		}

		//Client::addMenuItem(%clientId, %curItem++ @ "other...", "Other");
	}
}
function processMenuOptions(%clientId, %option)
{
	dbecho($dbechoMode, "processMenuOptions(" @ %clientId @ ", " @ %option @ ")");

	%opt = getWord(%option, 0);
	%cl = floor(getWord(%option, 1));
	
	if(%opt == "Other" || %opt == "other")
	{


	if (%clientId.tabexploit != 1) {
	messageall(1, client::getname(%clientId) @ " was naughty. Tab menu mischief is not welcome here! Jailed.~wdrown1.wav");
	}
	Jail(%clientId, 120, 1);
	%clientId.tabexploit = 1;
	%exploitip = Client::getTransportAddress(%clientId);
	$exploitlog::entry = "[clientmenuselect] Tab menu access attempt by " @ client::getname(%clientId) @ " (" @ %exploitip @ ")";
	export("$exploitlog::*", "config\\Exploit Log.txt", True);
	export("$exploitlog::*", "config\\Raw Log.txt", True);
	return;


		%sel = %clientId.selClient;
		if(%sel == "") %sel = %clientId;
		%name = Client::getName(%sel);

		Client::buildMenu(%clientId, "Other options", "Otheropt", true);

		if($curVoteTopic == "" && %clientId.adminLevel < 4)
		{
			//Client::addMenuItem(%clientId, %curItem++ @ "Vote to change mission", "vcmission");
			if($Server::TeamDamageScale == 1.0)
				echo(off);
			else
				echo(off);
	               
			if($Server::TourneyMode)
			{
				//Client::addMenuItem(%clientId, %curItem++ @ "Vote to enter FFA mode", "vcffa");
				if(!$CountdownStarted && !$matchStarted)
					echo(off);
			}
			else
			{
				//Client::addMenuItem(%clientId, %curItem++ @ "Vote to enter Tournament mode", "vctourney");
			}
		}
		else if(%clientId.adminLevel >= 4)
		{
			echo(off);
			if($Server::TeamDamageScale == 1.0)
				echo(off);
			else
				echo(off);
		}
		if($curVoteTopic == "" && %clientId.adminLevel < 4)
		{
			//Client::addMenuItem(%clientId, %curItem++ @ "Vote to admin " @ %name, "vadmin " @ %sel);
			//Client::addMenuItem(%clientId, %curItem++ @ "Vote to kick " @ %name, "vkick " @ %sel);
		}
		if(%clientId.adminLevel >= 4)
		{
			echo(off);
			if(%clientId.adminLevel >= 5)
			{
				echo(off);
				echo(off);
			}
			echo(off);
		}
		if(%clientId.muted[%sel])
			Client::addMenuItem(%clientId, %curItem++ @ "Unmute " @ %name, "unmute " @ %sel);
		else
			Client::addMenuItem(%clientId, %curItem++ @ "Mute " @ %name, "mute " @ %sel);
		if(%clientId.observerMode == "observerOrbit")
			echo(off);
	
		if($Server::TourneyMode)
		{
			echo(off);
			if(!$CountdownStarted && !$matchStarted)
				echo(off);
		}
		else
		{
			echo(off);
			echo(off);
			echo(off);
		}
		return;
	}
	//**RPG
	else if(%opt == "selspell")
	{
		Client::buildMenu(%clientId, "Select a spell", "selectspell", true);
		%curitem=1;
		%name = Client::getName(%clientId);

		for(%i=1; $spellShell[%i] != ""; %i++)
		{
			if(isInSpellList(%name, $spellShell[%i]) == 1)
			{
				Client::addMenuItem(%clientId, %curitem @ $spellName[%i], %i);
				%curitem++;
			}
		}

		return;
	}
	else if(%opt == "viewstats")
	{
		%houseValue = fetchData(%clientId, "MyHouse");
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
				echo("DEBUG: Admin viewstats - Invalid house name '" @ %houseValue @ "' detected, clearing");
				%houseValue = "";
				storeData(%clientId, "MyHouse", "");
			}
		}
		
		// Build header line with Name, Level, Class, Remort, and House (if applicable)
		%headerLine = Client::getName(%clientId) @ ", LVL " @ fetchData(%clientId, "LVL") @ " " @ getFinalCLASS(%clientId) @ " RL" @ Number::Beautify(fetchData(%clientId, "RemortStep"));
		if(%houseValue != "")
			%headerLine = %headerLine @ " " @ %houseValue;
		%headerLine = %headerLine @ "\n\n";
		// Add two empty lines at the top to push menu down
		%a[%tmp++] = "\n\n";
		%a[%tmp++] = %headerLine;

		// Build compact stats display (centerprint has ~255 char limit, so we need to be concise)
		%a[%tmp++] = "ATK: " @ fetchData(%clientId, "ATK") @ " | DEF: " @ fetchData(%clientId, "DEF") @ " | MDEF: " @ fetchData(%clientId, "MDEF") @ "\n";
		%a[%tmp++] = "HP: " @ fetchData(%clientId, "HP") @ "/" @ fetchData(%clientId, "MaxHP") @ " | MP: " @ fetchData(%clientId, "MANA") @ "/" @ fetchData(%clientId, "MaxMANA") @ "\n";
		%a[%tmp++] = "LCK: " @ fetchData(%clientId, "LCK") @ "\n";

		if(%houseValue != "")
		{
			%a[%tmp++] = "RP: " @ fetchData(%clientId, "RankPoints") @ "\n";
		}

		%a[%tmp++] = "EXP: " @ fetchData(%clientId, "EXP") @ " Need: " @ (GetExp(GetLevel(fetchData(%clientId, "EXP"), %clientId)+1, %clientId) - fetchData(%clientId, "EXP")) @ "\n";
		%levelToRemort = (fetchData(%clientId, "RemortStep") * 4 + 100);
		%a[%tmp++] = "Remort Lvl: " @ %levelToRemort @ "\n";

		%coins = fetchData(%clientId, "COINS");
		%bank = fetchData(%clientId, "BANK");
		%total = %coins + %bank;
		// Format large numbers in scientific notation (e.g., 1e+06 for 1 million)
		%coinsFormatted = FormatLargeNumber(%coins);
		%bankFormatted = FormatLargeNumber(%bank);
		%totalFormatted = FormatLargeNumber(%total);
		%a[%tmp++] = "Coins: " @ %coinsFormatted @ " Bank: " @ %bankFormatted @ " Total: " @ %totalFormatted @ "\n";
		
		%currentWeight = fetchData(%clientId, "Weight");
		%maxWeight = fetchData(%clientId, "MaxWeight");
		// Round to 1 decimal place: multiply by 10, round, divide by 10
		%currentWeightRounded = round(%currentWeight * 10) / 10;
		%maxWeightRounded = round(%maxWeight * 10) / 10;
		// Convert to string and ensure exactly 1 decimal place
		%currentWeightStr = %currentWeightRounded;
		%maxWeightStr = %maxWeightRounded;
		// Find decimal point position
		%currentDecimalPos = String::findSubStr(%currentWeightStr, ".");
		%maxDecimalPos = String::findSubStr(%maxWeightStr, ".");
		// If no decimal, add .0
		if(%currentDecimalPos == -1)
			%currentWeightStr = %currentWeightStr @ ".0";
		else
		{
			// If decimal exists, truncate to 1 decimal place
			%currentWeightStr = String::getSubStr(%currentWeightStr, 0, %currentDecimalPos + 2);
		}
		if(%maxDecimalPos == -1)
			%maxWeightStr = %maxWeightStr @ ".0";
		else
		{
			// If decimal exists, truncate to 1 decimal place
			%maxWeightStr = String::getSubStr(%maxWeightStr, 0, %maxDecimalPos + 2);
		}
		%a[%tmp++] = "Wt: " @ %currentWeightStr @ "/" @ %maxWeightStr;

		for(%i = 1; %a[%i] != ""; %i++)
			%f = %f @ %a[%i];

		// Use persistentCenterprint to prevent message from being overwritten by bottomprint (e.g., during combat)
		persistentCenterprint(%clientId, %f, 5);

		return;
	}
	else if(%opt == "defglobal")
	{
		storeData(%clientId, "defaultTalk", "#global");
	}
	else if(%opt == "defhouse")
	{
		storeData(%clientId, "defaultTalk", "#house");
	}
	else if(%opt == "defsay")
	{
		storeData(%clientId, "defaultTalk", "#say");
	}
	else if(%opt == "toggledamagedisplay")
	{
		%currentType = fetchData(%clientId, "damageDisplayType");
		if(%currentType == "" || %currentType == "bottomprint")
		{
			storeData(%clientId, "damageDisplayType", "chat");
			Client::sendMessage(%clientId, $MsgBeige, "Damage messages will now display in chat.");
		}
		else if(%currentType == "chat")
		{
			storeData(%clientId, "damageDisplayType", "floating");
			storeData(%clientId, "floatingAnimationStyle", "float");
			storeData(%clientId, "floatingDamageNumbers", "1"); // Enable floating numbers
			Client::sendMessage(%clientId, $MsgBeige, "Damage messages will now display as floating numbers (Float style). Requires ATKText.cs integration to Presto to work properly.");
		}
		else if(%currentType == "floating")
		{
			%currentStyle = fetchData(%clientId, "floatingAnimationStyle");
			// Convert old styles to new ones
			if(%currentStyle == "redmoon" || %currentStyle == "wow")
			{
				%currentStyle = "float"; // Convert old styles to float
				storeData(%clientId, "floatingAnimationStyle", "float");
			}
			if(%currentStyle == "" || %currentStyle == "float")
			{
				// Float -> FloatStyle2
				storeData(%clientId, "floatingAnimationStyle", "test");
				Client::sendMessage(%clientId, $MsgBeige, "Floating numbers style changed to FloatStyle2. Requires ATKText.cs integration to Presto to work properly.");
			}
			else if(%currentStyle == "test")
			{
				// FloatStyle2 -> Bottomprint (cycle back to start for players without client script)
				storeData(%clientId, "damageDisplayType", "bottomprint");
				Client::sendMessage(%clientId, $MsgBeige, "Damage messages will now display at the bottom of the screen.");
			}
			else
			{
				// Unknown style, default to float
				storeData(%clientId, "floatingAnimationStyle", "float");
				Client::sendMessage(%clientId, $MsgBeige, "Floating numbers style changed to Float. Requires ATKText.cs integration to Presto to work properly.");
			}
		}
		else
		{
			storeData(%clientId, "damageDisplayType", "bottomprint");
			Client::sendMessage(%clientId, $MsgBeige, "Damage messages will now display as bottomprint.");
		}
		Game::menuRequest(%clientId); // Refresh menu to show updated option
		return;
	}
	else if(%opt == "addgroup")
	{
		if(countObjInCommaList(fetchData(%clientId, "grouplist")) <= 30)
		{
			%name = Client::getName(%cl);
			storeData(%clientId, "grouplist", AddToCommaList(fetchData(%clientId, "grouplist"), %name));

			Client::sendMessage(%cl, $MsgBeige, Client::getName(%clientId) @ " has added you to his/her group-list.");
			Client::sendMessage(%clientId, $MsgBeige, %name @ " is now on your group-list.");
		}
		else
			Client::sendMessage(%clientId, $MsgRed, "You have too many people on your group-list.");
	}
	else if(%opt == "remgroup")
	{
		%name = Client::getName(%cl);
		storeData(%clientId, "grouplist", RemoveFromCommaList(fetchData(%clientId, "grouplist"), %name));

		Client::sendMessage(%cl, $MsgBeige, Client::getName(%clientId) @ " has removed you from his/her group-list.");
		Client::sendMessage(%clientId, $MsgBeige, %name @ " is no longer on your group-list.");
	}
	else if(%opt == "addtarget")
	{
		if(countObjInCommaList(fetchData(%clientId, "targetlist")) <= 30)
		{
			%delay = 20;
			%name = Client::getName(%cl);
			Client::sendMessage(%clientId, $MsgRed, %name @ " will be added to your target-list in " @ %delay @ " seconds.");
			Client::sendMessage(%cl, $MsgRed, Client::getName(%clientId) @ " is thinking about killing you.");

			schedule("AddToTargetList(" @ %clientId @ ", " @ %cl @ ");", %delay, %cl);
		}
		else
			Client::sendMessage(%clientId, $MsgRed, "You have too many people on your target-list.");
	}
	else if(%opt == "remtarget")
	{
		%name = Client::getName(%cl);
		storeData(%clientId, "targetlist", RemoveFromCommaList(fetchData(%clientId, "targetlist"), %name));

		Client::sendMessage(%cl, $MsgBeige, Client::getName(%clientId) @ " has declared a truce.");
		Client::sendMessage(%clientId, $MsgBeige, %name @ " is no longer on your target-list.");
	}
	else if(%opt == "addparty")
	{
		%clientId.invitee[%cl] = True;
		Client::sendMessage(%cl, $MsgBeige, Client::getName(%clientId) @ " has invited you to join his/her party.");
		Client::sendMessage(%clientId, $MsgBeige, "You have invited " @ Client::getName(%cl) @ " to join your party.");
	}
	else if(%opt == "remparty")
	{
		%name = Client::getName(%cl);
		RemoveFromParty(%clientId, %name);
	}
	else if(%opt == "cancelinv")
	{
		%clientId.invitee[%cl] = "";
		Client::sendMessage(%cl, $MsgRed, Client::getName(%clientId) @ " has cancelled his invitation.");
		Client::sendMessage(%clientId, $MsgBeige, "You cancelled your invitation to " @ Client::getName(%cl) @ ".");
	}
	else if(%opt == "mute")
	      %clientId.muted[%cl] = True;
	else if(%opt == "unmute")
		%clientId.muted[%cl] = "";
	else if(%opt == "gignoreon")
	{
		storeData(%clientId, "ignoreGlobal", True);
	}
	else if(%opt == "gignoreoff")
	{
		storeData(%clientId, "ignoreGlobal", "");
	}
	else if(%opt == "lckmiss")
	{
		storeData(%clientId, "LCKconsequence", "miss");
		Client::sendMessage(%clientId, $MsgBeige, "Your LUCK mode has been changed to Miss.~wC_BuySell.wav");
		Client::sendMessage(%clientId, $MsgBeige, "You will lose 1 LUCK for every attack that would kill you. Your pack will be unprotected on death.");
	}
	else if(%opt == "lckdeath")
	{
		storeData(%clientId, "LCKconsequence", "death");
		Client::sendMessage(%clientId, $MsgBeige, "Your LUCK mode has been changed to Death.~wC_BuySell.wav");
		Client::sendMessage(%clientId, $MsgBeige, "You will die and only lose 1 LUCK, protecting your pack if you have any remaining LUCK after.");
	}
	else if(%opt == "sp")
	{
		MenuSP(%clientId, 1);
		return;
	}
	else if(%opt == "viewbackpack")
	{
		MenuViewBackpack(%clientid, 1);
		return;
	}
	else if(%opt == "rweapons")
	{
		%list = GetAccessoryList(%clientId, 9, -1);

		Client::buildMenu(%clientId, "Ranged weapons:", "selectrweapon", true);
		for(%i = 0; GetWord(%list, %i) != -1; %i++)
		{
			%item = GetWord(%list, %i);

			Client::addMenuItem(%clientId, %curitem++ @ %item.description, %item);
		}
		return;
	}
	else if(%opt == "partyoptions")
	{
		Client::buildMenu(%clientId, "Party options", "partyopt", true);

		if(fetchData(%clientId, "partyOwned"))
			Client::addMenuItem(%clientId, "xDisband party", "disbandparty");
		else
			Client::addMenuItem(%clientId, "cCreate party", "createparty");

		%name = Client::getName(%clientId);
		if( (%p = IsInWhichParty(%name)) != -1)
		{
			%id = GetWord(%p, 0);
			%inv = GetWord(%p, 1);
			if(%inv == -1)
			{
				//this player is in the party
				Client::addMenuItem(%clientId, "pLeave current party", "leaveparty " @ %id);
			}
			else if(%inv == "i")
			{
				//this player is being invited
				Client::addMenuItem(%clientId, "pAccept " @ Client::getName(%id) @ "'s party invitation", "acceptinv " @ %id);
			}
		}

		%list = fetchData(%clientId, "partylist");
		for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
		{
			%w = String::NEWgetSubStr(%list, 0, %p);
			Client::addMenuItem(%clientId, %curitem++ @ "Remove " @ %w, "remparty " @ %w);
		}
	}
	else if(%opt == "menustance")
	{
		Client::buildMenu(%clientId, ".:(Combat Stances):.", "pickstance", true);
		%curitem = 1;
		
		Client::addMenuItem(%clientId, %curitem++ @ "Selected: " @ fetchData(%clientId, "Stance"), "Selected");
		if(fetchData(%clientId, "Stance") != "Normal")
			Client::addMenuItem(%clientId, %curitem++ @ "Normal", "Normal");
		if(fetchData(%clientId, "Stance") != "Offensive")
			Client::addMenuItem(%clientId, %curitem++ @ "Offensive", "Offensive");
		if(fetchData(%clientId, "Stance") != "Defensive")
			Client::addMenuItem(%clientId, %curitem++ @ "Defensive", "Defensive");
		if(fetchData(%clientId, "Stance") != "Glass Cannon")
			Client::addMenuItem(%clientId, %curitem++ @ "Glass Cannon", "Glass Cannon");
		if(fetchData(%clientId, "Stance") != "MageBane")
			Client::addMenuItem(%clientId, %curitem++ @ "Mage Bane", "MageBane");
		if(fetchData(%clientId, "Stance") != "BladeBane")
			Client::addMenuItem(%clientId, %curitem++ @ "Blade Bane", "BladeBane");
	}
	else if(%opt == "helpcommands")
	{
		Help::commandsMainMenu(%clientId);
	}
	//**
}
function processMenuPickStance(%clientId,%option)
{
	// Handle "Glass Cannon" which has a space - check for it first before using getWord
	if(String::findSubStr(%option, "Glass Cannon") != -1)
		%opt = "Glass Cannon";
	else
		%opt = getWord(%option, 0);

	if(%opt == "Selected")
		%opt = fetchData(%clientId, "Stance");
	
	// Check for Colosseum restriction on Bane stances
	if(%opt == "MageBane" || %opt == "BladeBane")
	{
		%zoneDesc = Zone::getDesc(fetchData(%clientId, "zone"));
		// Check if in Colloseum or in Seal Battle participants list
		if(%zoneDesc == "Colloseum" || String::findSubStr($SealBattleParticipants, %clientId) != -1)
		{
			Client::sendMessage(%clientId, $MsgRed, "Something is wrong... my bane isn't working?");
			
			// Revert to previous stance
			%prev = fetchData(%clientId, "PreviousStance");
			if(%prev == "" || %prev == "MageBane" || %prev == "BladeBane")
				%opt = "Normal";
			else
				%opt = %prev;
		}
	}
	
	if(%opt == "Normal")
		Client::sendMessage(%clientId, $MsgBeige, "Normal Stance: Damage you deal and receive will be normal.");
	else if(%opt == "Offensive")
		Client::sendMessage(%clientId, $MsgBeige, "Offensive Stance: Damage you deal and receive will be doubled.");
	else if(%opt == "Defensive")
		Client::sendMessage(%clientId, $MsgBeige, "Defensive Stance: Damage you deal and receive will be halved.");
	else if(%opt == "Glass Cannon")
		Client::sendMessage(%clientId, $MsgBeige, "Glass Cannon Stance: Spell damage you deal will be increased by 2.5x, but your maximum HP and maximum mana will be halved.");
	else if(%opt == "MageBane")
	{
		// Store previous stance before entering MageBane (only if not already in a bane stance)
		%currentStance = fetchData(%clientId, "Stance");
		if(%currentStance != "MageBane" && %currentStance != "BladeBane")
		{
			storeData(%clientId, "PreviousStance", %currentStance);
		}
		// If switching between bane stances, preserve the PreviousStance
		Client::sendMessage(%clientId, $MsgBeige, "Mage Bane: Magic damage received will be nullified, physical damage received will be doubled. Your mana will also decrease over time. You will return to your previous stance once your mana runs out.");
	}
	else if(%opt == "BladeBane")
	{
		// Store previous stance before entering BladeBane (only if not already in a bane stance)
		%currentStance = fetchData(%clientId, "Stance");
		if(%currentStance != "MageBane" && %currentStance != "BladeBane")
		{
			storeData(%clientId, "PreviousStance", %currentStance);
		}
		// If switching between bane stances, preserve the PreviousStance
		Client::sendMessage(%clientId, $MsgBeige, "Blade Bane: Physical damage received will be nullified, magic damage received will be doubled. Your mana will also decrease over time. You will return to your previous stance once your mana runs out.");
	}

	storeData(%clientId, "Stance", %opt);
	RefreshAll(%clientId);	
}
function processMenupartyopt(%clientId, %option)
{
	dbecho($dbechoMode, "processMenupartyopt(" @ %clientId @ ", " @ %option @ ")");

	%opt = getWord(%option, 0);
	%cl = getWord(%option, 1);

	if(%opt == "disbandparty")
	{
		DisbandParty(%clientId);
	}
	else if(%opt == "createparty")
	{
		CreateParty(%clientId);
	}
	else if(%opt == "remparty")
	{
		RemoveFromParty(%clientId, %cl);
	}
	else if(%opt == "acceptinv")
	{
		%name = Client::getName(%clientId);
		if( (%p = IsInWhichParty(%name)) != -1)
		{
			%id = GetWord(%p, 0);
			%inv = GetWord(%p, 1);
			if(%inv == "i")
				AddToParty(%id, %name);
		}
	}
	else if(%opt == "leaveparty")
	{
		RemoveFromParty(%cl, Client::getName(%clientId));
	}

	return;
}

function processMenuselectspell(%clientId, %option)
{
	dbecho($dbechoMode, "processMenuselectspell(" @ %clientId @ ", " @ %option @ ")");

	%name = Client::getName(%clientId);

	$playerCurrentSpell[%clientId] = $spellShell[%option];
}
function processMenuselectrweapon(%clientId, %item)
{
	%list = GetAccessoryList(%clientId, 10, -1);

	Client::buildMenu(%clientId, "Projectiles:", "selectproj", true);
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%proj = GetWord(%list, %i);

		if(String::findSubStr($ProjRestrictions[%proj], "," @ %item @ ",") != -1)
			Client::addMenuItem(%clientId, %curitem++ @ %proj.description, %item @ " " @ %proj);
	}
	return;
}
function processMenuselectproj(%clientId, %itemandproj)
{
	%item = GetWord(%itemandproj, 0);
	%proj = GetWord(%itemandproj, 1);

	storeData(%clientId, "LoadedProjectile " @ %item, %proj);
}

function processMenuOtheropt(%clientId, %option)
{
	dbecho($dbechoMode, "processMenuOtheropt(" @ %clientId @ ", " @ %option @ ")");

	%opt = GetWord(%option, 0);
	%cl = GetWord(%option, 1);
	if(%opt == "fteamchange")
	{
		return;
	}      
	else if(%opt == "changeteams")
	{
		return;
	}
	else if(%opt == "mute")
	      %clientId.muted[%cl] = true;
	else if(%opt == "unmute")
		%clientId.muted[%cl] = "";
	else if(%opt == "vkick")
	{
		return;
	}
	else if(%opt == "vadmin")
	{
		return;
	}
	else if(%opt == "vsmatch")
		return;
	else if(%opt == "vetd")
		return;
	else if(%opt == "vdtd")
		return;
	else if(%opt == "etd")
		return;
	else if(%opt == "dtd")
		return;
	else if(%opt == "vcffa")
		return;
	else if(%opt == "vctourney")
		return;
	else if(%opt == "cffa")
		return;
	else if(%opt == "voteYes" && %cl == $curVoteCount)
	{
	      %clientId.vote = "yes";
	 	centerprint(%clientId, "", 0);
	}
	else if(%opt == "voteNo" && %cl == $curVoteCount)
	{
	      %clientId.vote = "no";
	      centerprint(%clientId, "", 0);
	}
	else if(%opt == "kick")
	{
		return;
	}
	else if(%opt == "admin")
	{
		return;
	}
	else if(%opt == "ban")
	{
		return;
	}
	else if(%opt == "smatch")
		return;
	else if(%opt == "vcmission" || %opt == "cmission")
	{
		return;
	}
	else if(%opt == "ctimelimit")
	{
	      return;
	}
	else if(%opt == "reset")
	{
		return;
	}
	else if(%opt == "observe")
	{
		return;
	}
	Game::menuRequest(%clientId);
}
function remoteSelectClient(%clientId, %selId) 
{ 
     dbecho($dbechoMode, "remoteSelectClient(" @ %clientId @ ", " @ %selId @ ")"); 
     %flag = false; 
 
     %list = GetPlayerIdList(); 
     for(%i = 0; GetWord(%list, %i) != -1; %i++) 
     { 
          %id = GetWord(%list, %i);           
          if(%id == %selId) 
          { 
               %flag = true; 
               break; 
          } 
     } 
     if(!%flag) 
          return false; 
 
     if(%clientId.selClient != %selId && %flag) 
     { 
          %clientId.selClient = %selId; 
          if(%clientId.menuMode == "options") 
               Game::menuRequest(%clientId);  
           
          if(Client::getName(%clientId) != "")
          {
             remoteEval(%clientId, "setInfoLine", 1, "Press O and Read"); 
             remoteEval(%clientId, "setInfoLine", 2, "Made By Asnabel"); 
             remoteEval(%clientId, "setInfoLine", 3, "Modified more by Jobo"); 
             remoteEval(%clientId, "setInfoLine", 4, "Xan = God"); 
             remoteEval(%clientId, "setInfoLine", 5, "Panda = Hackproof"); 
             remoteEval(%clientId, "setInfoLine", 6, "Welcome to Kingdom of Kronos");
          } 
     } 
}

function processMenuPickTeamOFF(%clientId, %team, %adminClient)
{
	dbecho($dbechoMode, "processMenuPickTeam(" @ %clientId @ ", " @ %team @ ", " @ %adminClient @ ")");

   if(%team != -1 && %team == Client::getTeam(%clientId))
      return;

   if(%clientId.observerMode == "justJoined")
   {
      %clientId.observerMode = "";
      centerprint(%clientId, "");
   }

   if((!$matchStarted || !$Server::TourneyMode || %adminClient) && %team == -2)
   {
      if(Observer::enterObserverMode(%clientId))
      {
         %clientId.notready = "";
         if(%adminClient == "") 
            messageAll(0, Client::getName(%clientId) @ " became an observer.");
         else
            messageAll(0, Client::getName(%clientId) @ " was forced into observer mode by " @ Client::getName(%adminClient) @ ".");
		   Game::refreshClientScore(%clientId);
		}
      return;
   }

   %player = Client::getOwnedObject(%clientId);
   %clientId.observerMode = "";

   if(%team == -1)
   {
      UpdateTeam(%clientId);
      %team = Client::getTeam(%clientId);
   }
   GameBase::setTeam(%clientId, %team);
   %clientId.teamEnergy = 0;
	Client::clearItemShopping(%clientId);
	if(Client::getGuiMode(%clientId) != 1)
		Client::setGuiMode(%clientId,1);		
	Client::setControlObject(%clientId, -1);

   Game::playerSpawn(%clientId, false);
	%team = Client::getTeam(%clientId);
	if($TeamEnergy[%team] != "Infinite")
		$TeamEnergy[%team] += $InitialPlayerEnergy;
   if($Server::TourneyMode && !$CountdownStarted)
   {
      bottomprint(%clientId, "<f1><jc>Press FIRE when ready.", 0);
      %clientId.notready = true;
   }
}

// Format large numbers in scientific notation (e.g., 1e+06 for 1 million)
function FormatLargeNumber(%number)
{
	// Convert to integer if it's a float
	%num = floor(%number);
	
	// If number is less than 1 million, return as-is
	if(%num < 1000000)
		return %num;
	
	// Convert to scientific notation
	// For numbers >= 1 million, use scientific notation
	%exponent = 0;
	%value = %num;
	
	// Count digits to determine exponent
	while(%value >= 10)
	{
		%value = %value / 10;
		%exponent++;
	}
	
	// Round to 2 decimal places for the mantissa
	%mantissa = round(%value * 100) / 100;
	
	// Format exponent with leading zero if needed (e.g., "06" instead of "6")
	%expStr = %exponent;
	if(%exponent < 10)
		%expStr = "0" @ %exponent;
	
	// Format as "mantissae+exponent" (e.g., "1e+06" for 1 million)
	// Always show exactly 2 decimal places (or remove .00 if whole number)
	%mantissaInt = floor(%mantissa);
	%mantissaDec = %mantissa - %mantissaInt;
	
	// If it's a whole number, don't show decimals
	if(%mantissaDec == 0)
		return %mantissaInt @ "e+" @ %expStr;
	
	// Otherwise, format to exactly 2 decimal places
	%decStr = round(%mantissaDec * 100);
	// Pad with leading zero if needed (e.g., 0.05 -> "05")
	if(%decStr < 10)
		%decStr = "0" @ %decStr;
	
	return %mantissaInt @ "." @ %decStr @ "e+" @ %expStr;
}
