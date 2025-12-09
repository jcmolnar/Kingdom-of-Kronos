$numArenaPlayers = 4;

$ArenaSpawnIndexes = "44";

$teleportInArenaCost = 5;

$maxroster = 4;
$ArenaPickDuelersTime = 10;
$ArenaStartTime = 15;
$DoCheckMatchWin = False;

$ArenaBotMatchLengthInTicks = 10;
$ArenaBotMatchTicker = 0;

$ArenaEquipment = "RustyIronBlade 1 ButterKnife 1 CrackedStick 1 ";

//first part = chance, second part = prize
$tmp="";
$ArenaPrize[$tmp++] = "50000 COINS 50";
$ArenaPrize[$tmp++] = "20000 COINS 250";
$ArenaPrize[$tmp++] = "5000 COINS 500";
$ArenaPrize[$tmp++] = "500 COINS 1000";
$ArenaPrize[$tmp++] = "250 CheetaursPaws 1";
$ArenaPrize[$tmp++] = "50 COINS 5000";

function InitArena()
{
	dbecho($dbechoMode, "InitArena()");

	ClearRoster();
	ClearArenaDueler();

	//search for an arena

	%group = nameToID("MissionGroup\\TheArena");

	if(%group != -1)
	{
		ScheduleArenaMatch();

		StringArenaTextBox("The RPG Arena has started.  Welcome.");
	}
}

function ScheduleArenaMatch()
{
	dbecho($dbechoMode, "ScheduleArenaMatch()");

	if(HumansInArena())
	{
		FillWaitingRoom($ArenaSpawnIndexes);
		schedule("CreateArenaDueler(" @ $numArenaPlayers @ ");", $ArenaPickDuelersTime);
		schedule("StartArenaMatch();", $ArenaStartTime);
	}
	else
		schedule("ScheduleArenaMatch();", $ArenaStartTime);
}
function StartArenaMatch()
{
	dbecho($dbechoMode, "StartArenaMatch()");

	StringArenaTextBox("The duel has started!");

	%z = 1;
	for(%i = 1; %i <= $maxroster; %i++)
	{
		if($ArenaDueler[%i] != "")
		{
			if(%z == 1)
				%team = 0;
			else if(%z == -1)
				%team = 1;
			%z = -%z;

			%id = GetWord($ArenaDueler[%i], 0);

			GameBase::setTeam(%id, %team);
			GiveArenaEquipment(%id);

			//this way the fighter starts with a weapon in-hand
			if(Player::isAiControlled(%id))
				AI::SelectBestWeapon(%id);
			else
				RemoteNextWeapon(%id);

			TeleportToMarker(%id, "TheArena\\ArenaSpawnMarkers", 1, 0);
			Client::sendMessage(%id, 1, "The duel has started!");
			CloseArenaTextBox(%id);
		}
	}
	$DoCheckMatchWin = True;
}
function GiveArenaEquipment(%clientId)
{
	dbecho($dbechoMode, "GiveArenaEquipment(" @ %clientId @ ")");

	//give the client the arena equipment
	GiveThisStuff(%clientId, $ArenaEquipment, False);

	//for(%a = 1; $ArenaEquipment[%a] != ""; %a++)
	//	Player::setItemCount(%clientId, GetWord($ArenaEquipment[%a], 0), GetWord($ArenaEquipment[%a], 1));
	//RefreshAll(%clientId);
}

function CreateArenaDueler(%num)
{
	dbecho($dbechoMode, "CreateArenaDueler(" @ %num @ ")");

	//pick %num players, with priority to the client's at the beginning of the list
	%pick = 0;
	%playerFlag = False;
	while(%picked < %num)
	{
		%pick++;
		if(%pick > $maxroster)
		{
			//not enough players in roster to satisfy %num
			return -1;
		}
		if($ArenaRoster[%pick] != "")
		{
			%picked++;
			$ArenaDueler[%picked] = $ArenaRoster[%pick] @ " ALIVE";
			$ArenaRoster[%pick] = "";

			if(!Player::isAiControlled(GetWord($ArenaDueler[%picked], 0)))
			{
				//there is a human player, so don't start the counter for a limited match time.
				%playerFlag = True;
			}
		}
	}

	//fix bot's levels to match the average of the human players
	%cnt = 0;
	for(%i = 1; $ArenaDueler[%i] != ""; %i++)
	{
		%id = GetWord($ArenaDueler[%i], 0);
		if(!Player::isAiControlled(%id))
		{
			%cnt++;
			%l += fetchData(%id, "LVL");
		}
	}
	if(%cnt > 0)
	{
		%nlvl = round(%l / %cnt);
		%rr = round(%nlvl / 3);
		%nrr = round(getRandom() * (%rr * 2.5));
		%avg = Cap(%nlvl + (getRandom() * %rr) - %nrr, 1, "inf");
		for(%i = 1; $ArenaDueler[%i] != ""; %i++)
		{
			%id = GetWord($ArenaDueler[%i], 0);
			if(Player::isAiControlled(%id))
			{
				storeData(%id, "EXP", GetExp(%avg, %id));
				Game::refreshClientScore(%id);
				HardcodeAIskills(%id);

				setHP(%id, fetchData(%id, "MaxHP"));
			}
		}
	}

	if(!%playerFlag)
		$IsABotMatch = True;
	else
		$IsABotMatch = False;

	//rearrange the $ArenaRoster array (i don't like the current method i'm using btw...)
	%parseagain = 1;
	while(%parseagain == 1)
	{
		%q = 0;
		%parseagain = "";
		for(%i = $maxroster; %i >= 1; %i--)
		{
			if($ArenaRoster[%i] != "" && %q == 0)
				%q = 1;
			if($ArenaRoster[%i] == "" && %q == 1)
				%parseagain = 1;
		}

		if(%parseagain == 1)
		{
			for(%i = 1; %i <= $maxroster-1; %i++)
			{
				if($ArenaRoster[%i] == "")
				{
					$ArenaRoster[%i] = $ArenaRoster[%i+1];
					$ArenaRoster[%i+1] = "";
				}
			}
		}
	}
	//notify duelers that they will be fighting soon
	for(%i = 1; $ArenaDueler[%i] != ""; %i++)
	{
		Client::sendMessage(GetWord($ArenaDueler[%i], 0), 1, "You have been selected to compete in a duel.  Get ready!");
		StringArenaTextBox(Client::getName(GetWord($ArenaDueler[%i], 0)) @ " has been selected to compete.");
	}
}


//roster functions for arena
function AddToRoster(%clientId)
{
	dbecho($dbechoMode, "AddToRoster(" @ %clientId @ ")");

	for(%i = 1; %i <= $maxroster; %i++)
	{
		if(Player::isAiControlled($ArenaRoster[%i]) && !Player::isAiControlled(%clientId))
		{
			//if there's a bot in the roster, make way for a player trying to enter
			storeData($ArenaRoster[%i], "noDropLootbagFlag", True);
			playNextAnim($ArenaRoster[%i]);
			Player::Kill($ArenaRoster[%i]);

			$ArenaRoster[%i] = "";
		}
		if($ArenaRoster[%i] == "")
		{
			$ArenaRoster[%i] = %clientId;

			//set to the common team 0 (all players in roster go to team 0)
			GameBase::setTeam(%clientId, 0);
			setHP(%clientId, fetchData(%clientId, "MaxHP"));
			setMANA(%clientId, fetchData(%clientId, "MaxMANA"));

			//StringArenaTextBox(Client::getName(%clientId) @ " was added to the arena roster.");

			CreateArenaStorage(%clientId);

			return %i;
		}
	}
	return -1;
}
function CreateArenaStorage(%clientId)
{
	dbecho($dbechoMode, "CreateArenaStorage(" @ %clientId @ ")");

	//remember this client's equipment.
	//all he can keep is his armor
	//and take all the rest out
	//-- NEW METHOD: dump all the player's stuff into his bank storage

	for(%i = 1; $ArenaStorage[%clientId, %i] != ""; %i++)
		$ArenaStorage[%clientId, %i] = "";
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%checkItem = getItemData(%i);
		%checkItemCount = Player::getItemCount(%clientId, %checkItem);
		if(%checkItemCount && %checkItem.className != Armor)
		{
			%ii++;

			%b = %checkItem;
			if(%b.className == "Equipped")
				%b = String::getSubStr(%b, 0, String::len(%b)-1);
			
			storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), %b, %checkItemCount));
			$ArenaStorage[%clientId, %ii] = %b @ " " @ %checkItemCount;
			Player::setItemCount(%clientId, %checkItem, 0);
		}
	}

	RefreshAll(%clientId);
}
function RemoveFromRoster(%clientId)
{
	dbecho($dbechoMode, "RemoveFromRoster(" @ %clientId @ ")");

	for(%i = 1; %i <= $maxroster; %i++)
	{
		if($ArenaRoster[%i] == %clientId)
		{
			$ArenaRoster[%i] = "";

			return %i;
		}
	}
	return -1;
}
function IsInRoster(%clientId)
{
	dbecho($dbechoMode, "IsInRoster(" @ %clientId @ ")");

	for(%i = 1; %i <= $maxroster; %i++)
	{
		if($ArenaRoster[%i] == %clientId)
		{
			return True;
		}
	}
	return False;
}

function ClearRoster()
{
	dbecho($dbechoMode, "ClearRoster()");

	for(%i = 1; %i <= $maxroster; %i++)
		$ArenaRoster[%i] = "";
}
function ClearArenaDueler()
{
	dbecho($dbechoMode, "ClearArenaDueler");

	for(%i = 1; %i <= $maxroster; %i++)
		$ArenaDueler[%i] = "";
}
function GetArenaDuelerIndex(%clientId)
{
	dbecho($dbechoMode, "GetArenaDuelerIndex(" @ %clientId @ ")");

	for(%i = 1; %i <= $maxroster; %i++)
	{
		if(GetWord($ArenaDueler[%i], 0) == %clientId)
		{
			return %i;
		}
	}
	return False;
}
function IsInArenaDueler(%clientId)
{
	dbecho($dbechoMode, "IsInArenaDueler(" @ %clientId @ ")");

	for(%i=1; %i <= $maxroster; %i++)
	{
		if(GetWord($ArenaDueler[%i], 0) == %clientId)
		{
			return True;
		}
	}
	return False;
}
function IsStillArenaFighting(%clientId)
{
	dbecho($dbechoMode, "IsStillArenaFighting(" @ %clientId @ ")");

	for(%i = 1; %i <= $maxroster; %i++)
	{
		%c = GetWord($ArenaDueler[%i], 0);
		%s = GetWord($ArenaDueler[%i], 1);
		if(%s == "ALIVE" && %c == %clientId)
			return True;
	}
	return False;
}
function RemoveFromArenaDueler(%clientId)
{
	dbecho($dbechoMode, "RemoveFromArenaDueler(" @ %clientId @ ")");

	for(%i=1; %i <= $maxroster; %i++)
	{
		if(GetWord($ArenaDueler[%i], 0) == %clientId)
		{
			$ArenaDueler[%i] = "";
			return %i;
		}
	}
	return -1;
}
function RemoveArenaEquipment(%clientId)
{
	dbecho($dbechoMode, "RemoveArenaEquipment(" @ %clientId @ ")");

	TakeThisStuff(%clientId, $ArenaEquipment);
	//remove the arenaequipment that the player was given
	//for(%i = 1; $ArenaEquipment[%i] != ""; %i++)
	//{
	//	if(Player::getItemCount(%clientId, GetWord($ArenaEquipment[%i], 0)))
	//		Player::setItemCount(%clientId, GetWord($ArenaEquipment[%i], 0), 0);
	//}

	//RefreshAll(%clientId);
}
function RestoreArenaStorage(%clientId)
{
	dbecho($dbechoMode, "RestoreArenaStorage(" @ %clientId @ ")");

	//restore this client's old equipment
	for(%i = 1; $ArenaStorage[%clientId, %i] != ""; %i++)
	{
		%item = GetWord($ArenaStorage[%clientId, %i], 0);
		%n = GetWord($ArenaStorage[%clientId, %i], 1);

		Item::giveItem(%clientId, %item, %n, False);
		storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), %item, -%n));
	}

	RefreshAll(%clientId);

	for(%i = 1; $ArenaStorage[%clientId, %i] != ""; %i++)
		$ArenaStorage[%clientId, %i] = "";
}
function CheckMatchWin()
{
	dbecho($dbechoMode, "CheckMatchWin()");

	%totalLiveTeams = 0;
	for(%i = 0; %i <= getNumTeams(); %i++)
		%teamCount[%i] = 0;	//just so it's not blank

	for(%i = 1; %i <= $maxroster; %i++)
	{
		%c = GetWord($ArenaDueler[%i], 0);
		%s = GetWord($ArenaDueler[%i], 1);
		if(%s == "ALIVE")
			%teamCount[GameBase::getTeam(%c)]++;
	}
	for(%i = 0; %teamCount[%i] != ""; %i++)
	{
		if(%teamCount[%i] > 0) %totalLiveTeams++;
	}
	//echo("%teamCount[0]: " @ %teamCount[0]);
	//echo("%teamCount[1]: " @ %teamCount[1]);
	//echo("Found " @ %totalLiveTeams @ " total live teams.");
	if(%totalLiveTeams == 1)
	{
		//determine prize.  prize is given to everyone on the team, no splitting.
		%prize = DetermineArenaPrize();

		//match is won
		for(%i = 0; %teamCount[%i] != ""; %i++)
		{
			if(%teamCount[%i] > 0)
			{
				//winners are on team %i
				for(%ii = 1; %ii <= $maxroster; %ii++)
				{
					%c = GetWord($ArenaDueler[%ii], 0);
					%s = GetWord($ArenaDueler[%ii], 1);

					if(%c != "")
					{
						if(GameBase::getTeam(%c) == %i && !IsDead(%c) && %s == "ALIVE")
						{
							//these are the winning players (%c)
							StringArenaTextBox(Client::getName(%c) @ " is victorious!");
							//echo(Client::getName(%c) @ " is victorious!");

							Client::sendMessage(%c, $MsgBeige, "You won a prize!");

							//award prize to player
							GiveThisStuff(%c, %prize, True);
							RefreshAll(%c);
						}
						//remove all remaining players from ArenaDueler and
						//send them back to the arena lobby
						if(%s == "ALIVE")
						{
							RestorePreviousEquipment(%c);
							ReturnToArenaLobby(%c);
						}
					}
				}
			}
		}
		return True;
	}
	else if(%totalLiveTeams == 0)
	{
		//match is a tie
		//simply schedule the next match, since the arena is clear.
		StringArenaTextBox("The match was a tie.");
		//echo("The match was a tie.");

		return True;
	}
	return False;
}
function ReturnToArenaLobby(%c)
{
	dbecho($dbechoMode, "ReturnToArenaLobby(" @ %c @ ")");

	if(Player::isAiControlled(%c))
	{
		storeData(%c, "noDropLootbagFlag", True);
		playNextAnim(%c);
		Player::Kill(%c);
	}
	else
	{
		UpdateTeam(%c);
		RefreshAll(%c);
		TeleportToMarker(%c, "TheArena\\TeleportEntranceMarkers", 0, 1);
		RefreshArenaTextBox(%c);
	}
}
function FillWaitingRoom(%indexes)
{
	dbecho($dbechoMode, "FillWaitingRoom(" @ %indexes @ ")");

	%group = nameToID("MissionGroup\\TheArena\\WaitingRoomMarkers");

	if(%group != -1)
	{
		for(%i = 1; %i <= $maxroster; %i++)
		{
			if($ArenaRoster[%i] == "")
			{
				//the WaitingRoomMarkers must contain at least as many markers as $maxroster
			      %marker = Group::getObject(%group, (%i-1));

				for(%z = 0; GetWord(%indexes, %z) != -1; %z++){}
				%r = floor(getRandom() * %z);

				//extra precautions
				if(%r < 0) %r = 0;
				if(%r > (%z-1)) %r = (%z-1);

				%index = GetWord(%indexes, %r);

				//schedule("SpawnArenaBot(" @ %index @ ", " @ %i @ ", " @ %marker @ ");", %i * 2);
				%AIname = AI::helper($spawnIndex[%index], "ArenaGladiator" @ %i, "MarkerSpawn " @ %marker);
				%aiId = AI::getId(%AIname);

				AddToRoster(%aiId);
			}
		}
		return 0;
	}
	return -1;
}
function SpawnArenaBot(%index, %i, %marker)
{
	dbecho($dbechoMode, "SpawnArenaBot(" @ %index @ ", " @ %i @ ", " @ %clientId @ ")");

	%AIname = AI::helper($spawnIndex[%index], "ArenaGladiator" @ %i, "MarkerSpawn " @ %marker);
	%aiId = AI::getId(%AIname);

	AddToRoster(%aiId);
}

function DetermineArenaPrize()
{
	dbecho($dbechoMode, "DetermineArenaPrize()");

	//1st step, count the total chances
	for(%i = 1; $ArenaPrize[%i] != ""; %i++)
		%total += GetWord($ArenaPrize[%i], 0);

	//2nd step, pick a random number between 1 and %total
	%num = $PrizeSeed;
	if($PrizeSeed == "")
		%num = floor(getRandom() * %total)+1;

	//3rd step
	%cnt=0;
	for(%i = 1; $ArenaPrize[%i] != ""; %i++)
	{
		%cnt += GetWord($ArenaPrize[%i], 0);

		if(%cnt > %num)
			return String::getSubStr($ArenaPrize[%i], String::len(GetWord($ArenaPrize[%i], 0)) + 1, 99999);
	}
}

function RestorePreviousEquipment(%c)
{
	dbecho($dbechoMode, "RestorePreviousEquipment(" @ %c @ ")");

	if(!Player::isAiControlled(%c) && !IsDead(%c))
	{
		RemoveArenaEquipment(%c);
		RestoreArenaStorage(%c);
	}
}

$ArenaTextBoxLines = 6;
function RefreshArenaTextBox(%clientId)
{
	dbecho($dbechoMode, "RefreshArenaTextBox(" @ %clientId @ ")");

	%final = "";
	for(%i = 1; %i <= $ArenaTextBoxLines; %i++)
	{
		%final = %final @ $ArenaTextBox[%i] @ "\n";
	}
	bottomprint(%clientId, %final, -1);
}
function StringArenaTextBox(%text)
{
	dbecho($dbechoMode, "StringArenaTextBox(" @ %text @ ")");

	for(%i = 2; %i <= $ArenaTextBoxLines; %i++)
	{
		$ArenaTextBox[%i-1] = $ArenaTextBox[%i];
	}
	$ArenaTextBox[$ArenaTextBoxLines] = %text;

	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(fetchData(%cl, "inArena"))
			RefreshArenaTextBox(%cl);
	}
}
function CloseArenaTextBox(%clientId)
{
	dbecho($dbechoMode, "CloseArenaTextBox(" @ %clientId @ ")");

	bottomprint(%clientId, "", -1);
}

function CheckAndBootFromArena(%clientId)
{
	dbecho($dbechoMode, "CheckAndBootFromArena(" @ %clientId @ ")");

	storeData(%clientId, "inArena", "");
	CloseArenaTextBox(%clientId);

	if(IsInRoster(%clientId))
	{
		RestorePreviousEquipment(%clientId);
           	RemoveFromRoster(%clientId);
	}
	if(IsInArenaDueler(%clientId))
	{
		RestorePreviousEquipment(%clientId);
            RemoveFromArenaDueler(%clientId);
	}

	if(!Player::isAiControlled(%clientId) && GameBase::getTeam(%clientId) == 1)
		GameBase::setTeam(%clientId, 0);
}

function HumansInArena()
{
	%list = GetPlayerIdList();
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		if(fetchData(%id, "inArena"))
			return True;
	}
	return False;
}

function sr()
{
	echo("-----------------------------------------");
	for(%i = 1; %i <= $maxroster; %i++)
		echo("$ArenaRoster[" @ %i @ "]: " @ $ArenaRoster[%i]);
	echo("-----------------------------------------");
}
function sd()
{
	echo("-----------------------------------------");
	for(%i = 1; %i <= $maxroster; %i++)
		echo("$ArenaDueler[" @ %i @ "]: " @ $ArenaDueler[%i]);
	echo("-----------------------------------------");
}

function DuelFight()
{
	%challengerId = NEWgetClientByName($DuelChallenger);
	%opponentId = NEWgetClientByName($DuelOpponent);
	%challengerzone = Zone::getDesc(fetchData(%challengerId, "zone"));
	%opponentzone = Zone::getDesc(fetchData(%opponentId, "zone"));
	%challengerhouse = Zone::getDesc(fetchData(%challengerId, "zone"));
	%opponenthouse = Zone::getDesc(fetchData(%opponentId, "zone"));
	if(%challengerzone != "DuelingArena" && $DuelOn == True)
	{
		Jail(%challengerId, 600, 1);
		Client::sendMessage(%challengerId, 1, "You have been jailed for 10 minutes for running from the duel!");
		if(fetchData(%challengerId, "MyHouse") != "" && fetchData(%opponentId, "MyHouse") != "" && fetchData(%challengerId, "MyHouse") != fetchData(%opponentId, "MyHouse"))
		{
			storeData(%opponentId, "RankPoints", 1, "inc");
			storeData(%challengerId, "RankPoints", -2, "inc");
			Client::sendMessage(%challengerId, 0, "You have lost two rank points!");
			Client::sendMessage(%opponentId, 0, "You have received a rank point!");
		}
		$DuelOn = False;
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgBeige, $DuelChallenger @ " has fled from the duel and has been jailed for 10 minutes!");
		$DuelChallenger = "";
	}
	if(%opponentzone != "DuelingArena" && $DuelOn == True)
	{
		Jail(%opponentId, 600, 1);
		Client::sendMessage(%opponentId, 1, "You have been jailed for 10 minutes for running from the duel!");
		if(fetchData(%challengerId, "MyHouse") != "" && fetchData(%opponentId, "MyHouse") != "" && fetchData(%challengerId, "MyHouse") != fetchData(%opponentId, "MyHouse"))
		{
			storeData(%challengerId, "RankPoints", 1, "inc");
			storeData(%opponentId, "RankPoints", -2, "inc");
			Client::sendMessage(%opponentId, 0, "You have lost two rank points!");
			Client::sendMessage(%challengerId, 0, "You have received a rank point!");
		}
		$DuelOn = False;
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgBeige, $DuelOpponent @ " has fled from the duel and has been jailed for 10 minutes!");
		$DuelOpponent = "";
	}
	if($DuelOn == False && $DuelChallenger != "" && $DuelOpponent == "")
	{
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgBeige, $DuelChallenger @ " has won the duel!");
		%lospos = -398.909 @ " " @ -2324.59 @ " " @ 78;
		GameBase::setPosition(%challengerId, %lospos);
		$DuelChallenger = "";
	}
	else if($DuelOn == False && $DuelOpponent != "" && $DuelChallenger == "")
	{
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgBeige, $DuelOpponent @ " has won the duel!");
		%lospos = -398.909 @ " " @ -2324.59 @ " " @ 78;
		GameBase::setPosition(%opponentId, %lospos);
		$DuelOpponent = "";
	}
	else if($DuelOn == False && $DuelOpponent != "" & $DuelChallenger != "")
	{
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgBeige, $DuelLoser @ " has run from the duel!");
		%lospos = -398.909 @ " " @ -2324.59 @ " " @ 78;
		GameBase::setPosition(%opponentId, %lospos);
		GameBase::setPosition(%challengerId, %lospos);
		$DuelOpponent = "";
		$DuelChallenger = "";
		$DuelLoser = "";
	}

	if($DuelOn == True)
		schedule("DuelFight();",2);
}
function UndeadInvasion()
{
	if($InvasionTimer == "")
		$InvasionTimer = 0;
	if($InvasionTimer == 0)
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgRed, "The Arbal Research Center is about to be invaded! Go there and protect it!");
	if($InvasionTimer == 17)
	{
		for(%cl = 1000; %cl < 10001; %cl = %cl + 1)
		{
			if (Player::isAiControlled(%cl))
			{
				storeData(%cl, "noDropLootbagFlag", True);
				Player::Kill(%cl);
			}
		}
	}
	if($InvasionTimer == 18)
	{
		%lospos = -2376 @ " " @ -2343 @ " " @ 65;
		%n = AI::helper(Emperor, Emperor0, "TempSpawn " @ %lospos @ " " @ 4, default);
		%id = NEWgetClientByName("Emperor0");
		storeData(%id, "frozen", True);
		%lospos = -2130 @ " " @ -2747 @ " " @ 208.104;
		%n = AI::helper(General, General0, "TempSpawn " @ %lospos @ " " @ 0, default);
		%id = NEWgetClientByName("General0");
		storeData(%id, "frozen", True);
		for (%x = 0; %x < 3; %x = %x + 1)
		{ 
			%lospos = -2359 @ " " @ -2357 + (3 * %x) @ " " @ 65;
			%n = AI::helper(King, Lord @ %x, "TempSpawn " @ %lospos @ " " @ 4, default);
			%id = NEWgetClientByName("Lord" @ %x );
			storeData(%id, "frozen", True);
		}
		for (%x = 0; %x < 4; %x = %x + 1)
		{ 
			%lospos = -2345 @ " " @ -2365 + (%x * 3) @ " " @ 65;
			%n = AI::helper(EliteGuard, EliteGuard @ %x, "TempSpawn " @ %lospos @ " " @ 4, default);
			%id = NEWgetClientByName("EliteGuard" @ %x );
			storeData(%id, "frozen", True);
		}
		for (%x = 0; %x < 5; %x = %x + 1)
		{ 
			%lospos = -2337 + (%x * 3) @ " " @ -2416 + (%x * 3) @ " " @ 67;
			%n = AI::helper(Guard, Guard @ %x, "TempSpawn " @ %lospos @ " " @ 4, default);
			%id = NEWgetClientByName("EliteGuard" @ %x );
			storeData(%id, "frozen", True);
		}
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgRed, "The Undead have gathered their army and will strike soon!");
	}
	if($InvasionTimer >= 24)
	{
		%id = NEWgetClientByName("Emperor0");
		if($InvasionTimer == 24)
			storeData(%id, "frozen", "");
		%lospos = -2130 @ " " @ -2747 @ " " @ 208.104;
		storeData(%id, "botAttackMode", 3);
		storeData(%id, "tmpbotdata", %lospos);
		for(%x = 0; %x < 3; %x = %x + 1)
		{
			%id = NEWgetClientByName("Lord" @ %x );
			if($InvasionTimer == 24)
				storeData(%id, "frozen", "");
			%lospos = -2130 @ " " @ -2747 @ " " @ 208.104;
			storeData(%id, "botAttackMode", 3);
			storeData(%id, "tmpbotdata", %lospos);	
		}
		for(%x = 0; %x < 4; %x = %x + 1)
		{
			%id = NEWgetClientByName("EliteGuard" @ %x );
			if($InvasionTimer == 24)
				storeData(%id, "frozen", "");
			%lospos = -2130 @ " " @ -2747 @ " " @ 208.104;
			storeData(%id, "botAttackMode", 3);
			storeData(%id, "tmpbotdata", %lospos);	
		}
		for(%x = 0; %x < 5; %x = %x + 1)
		{
			%id = NEWgetClientByName("Guard" @ %x );
			if($InvasionTimer == 24)
				storeData(%id, "frozen", "");
			%lospos = -2130 @ " " @ -2747 @ " " @ 208.104;
			storeData(%id, "botAttackMode", 3);
			storeData(%id, "tmpbotdata", %lospos);	
		}
		%id = NEWgetClientByName("General0");
		if($InvasionTimer == 24)
			storeData(%id, "frozen", "");
		%lospos = -2130 @ " " @ -2747 @ " " @ 208.104;
		storeData(%id, "botAttackMode", 3);
		storeData(%id, "tmpbotdata", %lospos);
	}
	if($InvasionTimer == 24)
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgRed, "The Undead army is advancing! Defend the General or you will all die!");
	if($InvasionTimer >= 24)
	{
		%id = NEWgetClientByName("General0");
		%ids = NEWgetClientByName("Emperor0");
		if(%id == -1)
		{
			for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			{
				if(Vector::getDistance(GameBase::getPosition(%cl), GameBase::getPosition(%ids)) <= 500)
				{
					playNextAnim(%cl);
					Player::Kill(%cl);
					Client::sendMessage(%cl, $MsgRed, "The General has fallen! Arbal has been captured!");
				}
			}
			$InvasionTimer = "";
		}
		if(%ids == -1)
		{
			for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			{
				if(Vector::getDistance(GameBase::getPosition(%cl), GameBase::getPosition(%id)) <= 500)
				{
					storeData(%cl, "EXP", 4000, "inc");
					Game::refreshClientScore(%cl);
					storeData(%cl, "COINS", fetchData(%cl, "LVL") * 500, "inc");
					RefreshAll(%cl);
					Client::sendMessage(%cl, $MsgRed, "The Undead Emperor has been destroyed and the General still lives! You have been paid for your efforts!");
				}
			}
			$InvasionTimer = "";
		}
	}
	
	if($InvasionTimer != "")
	{
		$InvasionTimer = $InvasionTimer + 1;
		schedule("UndeadInvasion();",5);
	}
	if($InvasionTimer == "")
	{
		for(%cl = 1000; %cl < 10001; %cl = %cl + 1)
		{
			if (Player::isAiControlled(%cl))
			{
				storeData(%cl, "noDropLootbagFlag", True);
				Player::Kill(%cl);
			}
		}
	}
}

// Setup function for Colloseum arena bots - ensures they have equipment and proper stats
// %internalName: The internal name returned by AI::helper (e.g., "RoundTwo497") - 100% reliable
// Helper function to trigger immediate AI::Periodic() call after bot is fully initialized
// This ensures bots start chasing immediately after full initialization (5.0s)
// instead of waiting for the first scheduled AI::Periodic() call (5s after ai::callbackPeriodic)
function Colloseum::TriggerImmediatePeriodic(%internalName, %targetClientId)
{
	// Get client ID from internal name
	%aiId = AI::getClientIdFromName(%internalName);
	if(%aiId == "") %aiId = -1; // Normalize empty string to -1
	
	// If bot not found yet, it may still be initializing - skip this call
	// SetupBot() will handle the AI::Periodic() call later
	if(%aiId == -1 || %aiId == "")
	{
		return;
	}
	
	// Use targetClientId if provided, otherwise fallback to global variable
	if(%targetClientId == "" || %targetClientId == -1)
		%targetClientId = $Colloseum::TargetClientId;
	
		// CRITICAL: Ensure bot behavior flags are set BEFORE calling AI::Periodic()
		// SetupBot() runs at 4.5s, but we're calling this at 5.0s, so flags should be set
		// But set them here as a safety measure in case SetupBot() hasn't run yet
		// Use mode 2 (follow) to enable sniffing + aggressive targeting
		storeData(%aiId, "botAttackMode", 2);
		// For mode 2, tmpbotdata should contain target ID, but we'll let sniffing find the closest player
		storeData(%aiId, "tmpbotdata", "");
		// Explicitly enable sniffing by clearing noBotSniff flag
		storeData(%aiId, "noBotSniff", "");
		// Keep AImoveChance = 1 for frequent movement
		storeData(%aiId, "AImoveChance", 1);
		// Set AImaxRangeOverride for Colloseum bots to cover the entire arena
		storeData(%aiId, "AImaxRangeOverride", 120);
	
		// Get BotInfoAiName to call AI::Periodic()
		%botAiName = fetchData(%aiId, "BotInfoAiName");
		if(%botAiName == "" || %botAiName == -1 || %botAiName == "0")
			%botAiName = $BotInfoAiName[%aiId];
		
		// CRITICAL: Ensure AI engine variables are set correctly for movement
		// seekOff must be 0 for bots to move (1 = disabled)
		// SpotDist must be > 0 for bots to target players
		if(%botAiName != "" && %botAiName != -1 && %botAiName != "0")
		{
			AI::setVar(%botAiName, seekOff, 0);
			// Get weapon range for SpotDist (if weapon is mounted)
			%playerObjForWeapon = Client::getOwnedObject(%aiId);
			if(%playerObjForWeapon != -1 && %playerObjForWeapon != "")
			{
				%equippedWeapon = Player::getMountedItem(%playerObjForWeapon, $WeaponSlot);
				if(%equippedWeapon != -1 && %equippedWeapon != "")
				{
					%weaponRange = GetRange(%equippedWeapon);
					if(%weaponRange != "" && %weaponRange != -1 && %weaponRange > 0)
						AI::setVar(%botAiName, SpotDist, %weaponRange);
					else
						AI::setVar(%botAiName, SpotDist, $AIspotDist);
				}
				else
				{
					// No weapon yet - use default SpotDist
					AI::setVar(%botAiName, SpotDist, $AIspotDist);
				}
			}
			else
			{
				// No player object - use default SpotDist
				AI::setVar(%botAiName, SpotDist, $AIspotDist);
			}
		}
	
	// If BotInfoAiName is set, trigger immediate movement/chasing
	if(%botAiName != "" && %botAiName != -1 && %botAiName != "0")
	{
		// Ensure bot is not frozen before triggering movement
		%isFrozen = fetchData(%aiId, "frozen");
		%isBotFrozen = $BotFrozen[%aiId];
		// Bot is not frozen if: isFrozen is empty, -1, or 0 (0 means not frozen)
		%notFrozen = (%isFrozen == "" || %isFrozen == -1 || %isFrozen == 0);
		%notBotFrozen = (%isBotFrozen == "" || %isBotFrozen != "true");
		if(%notFrozen && %notBotFrozen)
		{
			// Bot is not frozen - directly target the specified player
			%playerObj = Client::getOwnedObject(%aiId);
			if(%playerObj != -1 && %playerObj != "")
			{
				%aiPos = GameBase::getPosition(%playerObj);
				%aiTeam = GameBase::getTeam(%playerObj);
				if(%aiTeam == -1)
					%aiTeam = fetchData(%aiId, "botTeam");
				if(%aiTeam == "" || %aiTeam == -1)
					%aiTeam = 1; // Default to team 1 (enemy)
				
				// Directly target the specified player if valid
				%useTarget = false;
				if(%targetClientId != "" && %targetClientId != -1)
				{
					// Validate target clientID exists and is in Colloseum zone
					%targetPlayerObj = Client::getOwnedObject(%targetClientId);
					if(%targetPlayerObj != -1 && %targetPlayerObj != "")
					{
						%targetZone = Zone::getDesc(fetchData(%targetClientId, "zone"));
						if(%targetZone == "Colloseum")
						{
							%targetTeam = GameBase::getTeam(%targetPlayerObj);
							%targetInvisible = fetchData(%targetClientId, "invisible");
							if(%targetTeam != %aiTeam && !fetchData(%targetClientId, "invisible"))
							{
								%useTarget = true;
							}
						}
					}
				}
				
				// If direct target is valid, use it; otherwise fallback to sniffing
				if(%useTarget)
				{
					// Set target and follow them (botAttackMode 2 uses AI::newDirectiveFollow)
					storeData(%aiId, "AITarget", %targetClientId);
					storeData(%aiId, "tmpbotdata", %targetClientId); // For botAttackMode 2
					// Use AI::newDirectiveFollow for mode 2 (follow mode)
					AI::newDirectiveFollow(%botAiName, %targetClientId, 0, 99);
				}
				else
				{
					// Fallback to sniffing logic if direct target is invalid
					%closest = 500000;
					%closestId = "";
					%botMaxRange = fetchData(%aiId, "AImaxRangeOverride");
					if(%botMaxRange == "" || %botMaxRange == -1 || %botMaxRange == "0")
						%botMaxRange = $AImaxRange;
					%b = %botMaxRange * 2;
					%set = newObject("set", SimSet);
					%n = containerBoxFillSet(%set, $SimPlayerObjectType, %aiPos, %b, %b, %b, 0);
					for(%i = 0; %i < Group::objectCount(%set); %i++)
					{
						%targetPlayerObj = Group::getObject(%set, %i);
						if(%targetPlayerObj != -1 && %targetPlayerObj != "")
						{
							%id = GetClientIdFromPlayerObject(%targetPlayerObj);
							if(%id == -1 || %id == "")
								%id = %targetPlayerObj;
							
							%targetTeam = GameBase::getTeam(%targetPlayerObj);
							
							if(%id != -1 && %id != "" && %targetTeam != %aiTeam && !fetchData(%id, "invisible"))
							{
								%targetPos = GameBase::getPosition(%targetPlayerObj);
								%dist = Vector::getDistance(%aiPos, %targetPos);
								if(%dist < %closest)
								{
									%closest = %dist;
									%closestId = %id;
								}
							}
						}
					}
					deleteObject(%set);
					
					// If we found a target within range, move towards them
					if(%closest <= %botMaxRange && %closestId != "")
					{
						%targetPlayer = Client::getOwnedObject(%closestId);
						if(%targetPlayer != -1 && %targetPlayer != "")
						{
							// Set target and follow them (botAttackMode 2 uses AI::newDirectiveFollow)
							storeData(%aiId, "AITarget", %closestId);
							storeData(%aiId, "tmpbotdata", %closestId); // For botAttackMode 2
							// Use AI::newDirectiveFollow for mode 2 (follow mode)
							AI::newDirectiveFollow(%botAiName, %closestId, 0, 99);
						}
					}
				}
			}
			
			// Also call AI::Periodic() as backup (it will handle ongoing chasing)
			if(%botAiName != "" && %botAiName != -1 && %botAiName != "0")
			{
				AI::Periodic(%botAiName);
			}
		}
	}
}

// %retryCount: Number of retry attempts (default 0, max 10)
function Colloseum::SetupBot(%internalName, %guardtype, %pos, %retryCount, %targetClientId)
{
	// Initialize retry count if not provided
	if(%retryCount == "")
		%retryCount = 0;
	
	// CRITICAL: Validate internal name is not -1 or empty (indicates spawn failure)
	if(%internalName == -1 || %internalName == "")
	{
		echo("ERROR Colloseum::SetupBot: Invalid internal name (" @ %internalName @ ") - spawn likely failed. Cannot setup bot.");
		// Clear the bot name variable to prevent further attempts
		if(%internalName == $Colloseum::BotName1)
			$Colloseum::BotName1 = "";
		else if(%internalName == $Colloseum::BotName2)
			$Colloseum::BotName2 = "";
		else if(%internalName == $Colloseum::BotName3)
			$Colloseum::BotName3 = "";
		return;
	}
	
	// CRITICAL: Use AI::getClientIdFromName to lookup by internal name (reliable, available immediately)
	%aiId = AI::getClientIdFromName(%internalName);
	if(%aiId == "") %aiId = -1; // Normalize empty string to -1
	
	// Fallback: Try display name lookup for backwards compatibility
	if(%aiId == -1 || %aiId == "")
		%aiId = NEWgetClientByName(%internalName);
	
	// If bot not found yet, retry after 0.5 seconds (bot may still be initializing)
	if(%aiId == -1 || %aiId == "")
	{
		// Check retry count before scheduling another retry (max 10 retries)
		if(%retryCount >= 10)
		{
			echo("ERROR Colloseum::SetupBot: Bot " @ %internalName @ " not found after 10 retries. Giving up and clearing bot name.");
			// Clear the bot name variable to prevent further attempts
			if(%internalName == $Colloseum::BotName1)
				$Colloseum::BotName1 = "";
			else if(%internalName == $Colloseum::BotName2)
				$Colloseum::BotName2 = "";
			else if(%internalName == $Colloseum::BotName3)
				$Colloseum::BotName3 = "";
			return;
		}
		
		// Retry after 0.5 seconds - bot may still be initializing
		// Use targetClientId if provided, otherwise fallback to global variable
		if(%targetClientId == "" || %targetClientId == -1)
			%targetClientId = $Colloseum::TargetClientId;
		schedule("Colloseum::SetupBot(\"" @ %internalName @ "\", \"" @ %guardtype @ "\", \"" @ %pos @ "\", " @ (%retryCount + 1) @ ", " @ %targetClientId @ ");", 0.5);
		echo("WARNING Colloseum::SetupBot: Bot " @ %internalName @ " not found yet, retrying in 0.5 seconds... (attempt " @ (%retryCount + 1) @ "/10)");
		return;
	}
	
	// CRITICAL: Clear any stale zone data from this client ID
	// This prevents false "changing zones" detection for Colloseum arena bots
	storeData(%aiId, "zone", "");
	storeData(%aiId, "tmpzone", "");
	
	// Set bot behavior flags (Colloseum-specific flags will be set at end of function)
	storeData(%aiId, "noDropLootbagFlag", True);
	
	// Ensure bot has skills set
	%hasSkills = false;
	%ns = getNumSkills();
	for(%i = 1; %i <= %ns && !%hasSkills; %i++)
	{
		if($PlayerSkill[%aiId, %i] != "" && $PlayerSkill[%aiId, %i] > 0)
			%hasSkills = true;
	}
	
	if(!%hasSkills)
	{
		HardcodeAIskills(%aiId);
	}
	
	// Get equipment string from bot's guardtype configuration
	%equipString = "";
	
	// Try to get guardtype from bot's BotInfoAiName if not provided or if lookup fails
	%botInfoAiName = fetchData(%aiId, "BotInfoAiName");
	if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
	{
		// Extract guardtype from BotInfoAiName (it might be in the format "guardtypeName" or similar)
		// Extract guardtype by removing trailing digits (more reliable than clipTrailingNumbers)
		// This works backwards from the end to find and remove only trailing digits
		%guardtypeFromName = %botInfoAiName;
		%len = String::len(%botInfoAiName);
		%numStr = "";
		%digitString = "0123456789";
		
		// Find trailing digits (working backwards)
		for(%i = %len - 1; %i >= 0; %i--)
		{
			%char = String::getSubStr(%botInfoAiName, %i, 1);
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
			%guardtypeFromName = String::getSubStr(%botInfoAiName, 0, %len - String::len(%numStr));
		}
		// Only use extracted guardtype if it's different from original (i.e., digits were found and removed)
		// This validation ensures we don't use the original name if extraction didn't find trailing digits
		if(%guardtypeFromName != "" && %guardtypeFromName != %botInfoAiName)
			%guardtype = %guardtypeFromName;
	}
	
	// Try to get equipment using guardtype
	if(%guardtype != "")
	{
		// Try BotInfo first
		%equipString = $BotInfo[%guardtype, ITEMS];
		// If not found, try BotEquipment
		if(%equipString == "")
			%equipString = $BotEquipment[%guardtype];
	}
	
	// If still no equipment found, try getting it from spawn configuration
	%spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
	if(%equipString == "" && %spawnBotInfo != "")
	{
		// Bot was spawned from a spawn point or spawnIndex - equipment might already be given
		// But let's try to get it from the bot's current items or spawn config
		// For now, we'll rely on RefreshAll() to calculate stats from what the bot already has
	}
	
	// Check if bot already has equipment from AI::setWeapons()
	// AI::setWeapons() runs 0.2s after spawn, Colloseum::SetupBot() runs 0.7s after spawn
	// If AI::setWeapons() already gave equipment, we should NOT give it again
	%hasWeapon = false;
	%weapon = Player::getMountedItem(%aiId, $WeaponSlot);
	if(%weapon != -1 && %weapon != "")
		%hasWeapon = true;

	// Check if OriginalLootString is already set (indicates AI::setWeapons() already ran)
	%existingOriginalLootString = fetchData(%aiId, "OriginalLootString");
	%equipmentAlreadyGiven = (%existingOriginalLootString != "" && %existingOriginalLootString != -1);

	// Give equipment directly if we found it AND it hasn't been given already
	if(%equipString != "" && !%equipmentAlreadyGiven)
	{
		// CRITICAL: Set auto-equip flags BEFORE giving equipment
		storeData(%aiId, "AutoEquipArmor", "true");
		storeData(%aiId, "AutoEquipShield", "true");
		storeData(%aiId, "AutoEquipAccessories", "true");
		// Store original equipment string for lootbag generation
		storeData(%aiId, "OriginalLootString", %equipString);
		// Give equipment directly
		GiveThisStuff(%aiId, %equipString, False);
		// Ensure skills are set after giving equipment (equipment affects stat calculations)
		if(!%hasSkills)
			HardcodeAIskills(%aiId);
	}
	else if(%equipmentAlreadyGiven)
	{
		// AI::setWeapons() already gave equipment - just ensure skills are set
		if(!%hasSkills)
			HardcodeAIskills(%aiId);
	}
	
	// CRITICAL: Set the correct race and armor for Colloseum bots BEFORE RefreshAll()
	// This ensures the bot has the correct race/armor when stats are calculated
	if(%guardtype != "")
	{
		%race = $NameForRace[%guardtype];
		if(%race != "" && %race != -1)
		{
			storeData(%aiId, "RACE", %race);
			%armor = $RaceToArmorType[%race];
			if(%armor != "" && %armor != -1)
			{
				Player::setArmor(%aiId, %armor);
			}
		}
	}
	
	// Call RefreshAll() to calculate the bot's stats from its spawn configuration
	RefreshAll(%aiId);
	
	// CRITICAL: Set race and armor AGAIN after RefreshAll() to ensure they stick
	// RefreshAll() or other functions might reset the race/armor, so we need to override it
	if(%guardtype != "")
	{
		%race = $NameForRace[%guardtype];
		if(%race != "" && %race != -1)
		{
			storeData(%aiId, "RACE", %race);
			%armor = $RaceToArmorType[%race];
			if(%armor != "" && %armor != -1)
			{
				Player::setArmor(%aiId, %armor);
			}
		}
	}
	
	// Set HP and MANA to max to ensure bot is at full health/mana
	%maxHP = fetchData(%aiId, "MaxHP");
	%maxMANA = fetchData(%aiId, "MaxMANA");
	if(%maxHP != "" && %maxHP != -1 && %maxHP > 0)
		setHP(%aiId, %maxHP);
	if(%maxMANA != "" && %maxMANA != -1 && %maxMANA > 0)
		setMANA(%aiId, %maxMANA);
	
	// CRITICAL: Schedule race/armor to be set again after 2.5 seconds to override any periodic resets
	// Something (possibly RefreshAll() or a periodic function) is resetting the race to "malehuman" after ~2 seconds
	if(%guardtype != "")
	{
		%race = $NameForRace[%guardtype];
		if(%race != "" && %race != -1)
		{
			%armor = $RaceToArmorType[%race];
			if(%armor != "" && %armor != -1)
			{
				schedule("storeData(" @ %aiId @ ", \"RACE\", \"" @ %race @ "\"); Player::setArmor(" @ %aiId @ ", \"" @ %armor @ "\");", 2.5);
			}
		}
	}
	
	// Ensure bot has weapon mounted - check again after giving equipment
	%weapon = Player::getMountedItem(%aiId, $WeaponSlot);
	if(%weapon == -1 || %weapon == "")
	{
		// Bot still doesn't have weapon mounted - try to mount first weapon from equipment
		if(%equipString != "")
		{
			// Find first weapon in equipment string and mount it
			for(%i = 0; GetWord(%equipString, %i) != -1; %i += 2)
			{
				%itemName = GetWord(%equipString, %i);
				%itemCount = GetWord(%equipString, %i + 1);
				
				if(%itemName != "" && %itemCount > 0)
				{
					// Check if this is a weapon item (has WeaponItem data)
					%itemData = getItemData(%itemName);
					if(%itemData != -1 && %itemData.weaponSlots != "")
					{
						// This is a weapon - mount it
						%weaponCount = Player::getItemCount(%aiId, %itemName);
						if(%weaponCount > 0)
						{
							RemoteMountItem(%aiId, %itemName, $WeaponSlot);
							break; // Mounted weapon, stop searching
						}
					}
				}
			}
		}
	}
	
	// CRITICAL: Set Colloseum-specific bot behavior flags AFTER all setup/unfreeze/shove operations
	// This ensures these flags only apply to Colloseum bots and don't interfere with other operations
	storeData(%aiId, "ColloseumBot", "true"); // Identify as Colloseum bot for cleanup on death

	// Use mode 2 (follow) to enable sniffing + aggressive targeting
	storeData(%aiId, "botAttackMode", 2);
	// For mode 2, tmpbotdata should contain target ID, but we'll let sniffing find the closest player
	// Set to empty for now - sniffing will set AITarget automatically
	storeData(%aiId, "tmpbotdata", "");

	// Explicitly enable sniffing by clearing noBotSniff flag
	storeData(%aiId, "noBotSniff", "");

	// Keep AImoveChance = 1 for frequent movement
	storeData(%aiId, "AImoveChance", 1);

	// CRITICAL: Set increased detection range for Colloseum bots
	// Colloseum is ~65x60 units with diagonal ~88.5 units
	// Set to 120 units to ensure full arena coverage with safety margin
	storeData(%aiId, "AImaxRangeOverride", 120);
	
	// CRITICAL: Trigger immediate movement after setup is complete
	// This ensures bots start chasing immediately after all setup is done
	%botAiName = fetchData(%aiId, "BotInfoAiName");
	if(%botAiName != "" && %botAiName != -1 && %botAiName != "0")
	{
		// Schedule immediate periodic call after a short delay to ensure everything is set
		// Use targetClientId if provided, otherwise fallback to global variable
		if(%targetClientId == "" || %targetClientId == -1)
			%targetClientId = $Colloseum::TargetClientId;
		// Schedule immediate periodic call after a short delay to ensure everything is set
		schedule("Colloseum::TriggerImmediatePeriodic(\"" @ %internalName @ "\", " @ %targetClientId @ ");", 0.5);
	}
	
	// Trigger immediate first AI::Periodic() call so bot detects and chases players immediately
	// (ai::callbackPeriodic schedules the first call 5 seconds later, causing bots to stand still)
	// NOTE: This is now handled by Colloseum::TriggerImmediatePeriodic() scheduled above
	if(false)
	{
		schedule("AI::Periodic(\"" @ %botAiName @ "\");", 0.1);
	}
}

function Colloseum()
{
	// Prevent checking for death if we just spawned a bot (wait 10 seconds for name replication)
	if($Colloseum::LastSpawnTime != "" && (getSimTime() - $Colloseum::LastSpawnTime) < 10.0)
	{
		schedule("Colloseum();", 2.0);
		return;
	}
	
	%clientId = NEWgetClientByName($Contestant);
	%challengerzone = Zone::getDesc(fetchData(%clientId, "zone"));
	%lospos = -3608 @ " " @ -2365 @ " " @ 354;
	
	// Store target clientID globally for bot targeting (available throughout Colloseum event)
	if(%clientId != -1 && %clientId != "")
		$Colloseum::TargetClientId = %clientId;
	
	// Enforce Bane restriction
	if(%clientId != -1 && %clientId != "")
	{
		%currentStance = fetchData(%clientId, "Stance");
		if(%currentStance == "MageBane" || %currentStance == "BladeBane")
		{
			storeData(%clientId, "Stance", "Normal");
			Client::sendMessage(%clientId, $MsgRed, "Something is wrong... my bane isn't working?");
			RefreshAll(%clientId);
		}
	}
	
	if($Fight != "")
	{
		$AImoveChance = 1;
		// Round 1: Check if Round 1 is active but bot is dead/missing
		// Helper check: Verify bot is truly gone or dead (not just missing from lookup)
		%round1BotId = -1;
		if($Colloseum::BotName1 != "" && $Colloseum::BotName1 != -1)
			%round1BotId = AI::getClientIdFromName($Colloseum::BotName1);
		%round1IsDeadOrGone = ($Colloseum::BotName1 == "" || %round1BotId == -1 || (%round1BotId != -1 && IsDead(%round1BotId)));
		
		if($Round == 0 && %round1IsDeadOrGone)
		{
			%guardtype = $ColloseumRoundOne[$Fight];
			// Validate guardtype is not empty before spawning
			if(%guardtype == "" || %guardtype == -1)
			{
				echo("ERROR Colloseum: Round 1 - Invalid guardtype for $Fight=" @ $Fight @ ". Cannot spawn bot.");
				return;
			}
			// Use bot type name directly as display name (matches internal name pattern)
			%displayName = %guardtype;
			
			// Initialize spawn retry counter if not set
			if($Colloseum::SpawnRetryCount1 == "")
				$Colloseum::SpawnRetryCount1 = 0;
			
			// Capture internal name from AI::helper return value
			$Colloseum::BotName1 = AI::helper(%guardtype, %displayName, "TempSpawn " @ %lospos @ " " @ 1, default);
			
			// Validate spawn was successful before proceeding
			if($Colloseum::BotName1 == -1 || $Colloseum::BotName1 == "")
			{
				// Spawn failed - retry if under limit (max 3 retries)
				if($Colloseum::SpawnRetryCount1 < 3)
				{
					$Colloseum::SpawnRetryCount1 = $Colloseum::SpawnRetryCount1 + 1;
					echo("WARNING Colloseum: Round 1 spawn failed (attempt " @ $Colloseum::SpawnRetryCount1 @ "/3), retrying in 1 second...");
					schedule("Colloseum();", 1.0);
					return;
				}
				else
				{
					// Max retries reached - clear variables and log error
					echo("ERROR Colloseum: Round 1 spawn failed after 3 retries. Cannot spawn bot for guardtype " @ %guardtype);
					$Colloseum::BotName1 = "";
					$Colloseum::SpawnRetryCount1 = "";
					return;
				}
			}
			
			// Spawn successful - clear retry counter and proceed
			$Colloseum::SpawnRetryCount1 = "";
			
			// Set spawn cooldown timer to prevent premature death checks
			$Colloseum::LastSpawnTime = getSimTime();
			
			%pos = -3608 @ " " @ -2365 @ " " @ 354;
			// Pass INTERNAL NAME to setup (4.5s delay to ensure SpawnAIGetClientId completes first)
			// SpawnAIGetClientId is scheduled at 3.0s, so 4.5s gives it time to register the bot
			schedule("Colloseum::SetupBot(\"" @ $Colloseum::BotName1 @ "\", \"" @ %guardtype @ "\", \"" @ %pos @ "\", 0, " @ %clientId @ ");", 4.5);
			
			// Trigger immediate AI::Periodic() call at 5.0s (after bot is fully initialized)
			// This ensures bot starts chasing immediately after full initialization, before SetupBot() runs
			if($Colloseum::BotName1 != -1 && $Colloseum::BotName1 != "")
			{
				schedule("Colloseum::TriggerImmediatePeriodic(\"" @ $Colloseum::BotName1 @ "\", " @ %clientId @ ");", 5.0);
			}
			
			// Only increment round if we actually spawned (internal name is valid)
			if($Colloseum::BotName1 != -1 && $Colloseum::BotName1 != "")
				$Round = $Round + 1;
		}
		// Round 2: Check if Round 1 bot is dead and Round 2 hasn't started
		else if($Round == 1)
		{
			// Helper check: Verify Round 1 bot is truly gone or dead (not just missing from lookup)
			%round1BotIdForRound2 = -1;
			if($Colloseum::BotName1 != "" && $Colloseum::BotName1 != -1)
				%round1BotIdForRound2 = AI::getClientIdFromName($Colloseum::BotName1);
			%round1IsDeadOrGoneForRound2 = ($Colloseum::BotName1 == "" || %round1BotIdForRound2 == -1 || (%round1BotIdForRound2 != -1 && IsDead(%round1BotIdForRound2)));
			
			if(%round1IsDeadOrGoneForRound2)
			{
				%guardtype = $ColloseumRoundTwo[$Fight];
				// Validate guardtype is not empty before spawning
				if(%guardtype == "" || %guardtype == -1)
				{
					echo("ERROR Colloseum: Round 2 - Invalid guardtype for $Fight=" @ $Fight @ ". Cannot spawn bot.");
					return;
				}
				// Use bot type name directly as display name (matches internal name pattern)
				%displayName = %guardtype;
				
				// Initialize spawn retry counter if not set
				if($Colloseum::SpawnRetryCount2 == "")
					$Colloseum::SpawnRetryCount2 = 0;
				
				// Capture internal name from AI::helper return value
				$Colloseum::BotName2 = AI::helper(%guardtype, %displayName, "TempSpawn " @ %lospos @ " " @ 1, default);
				
				// Validate spawn was successful before proceeding
				if($Colloseum::BotName2 == -1 || $Colloseum::BotName2 == "")
				{
					// Spawn failed - retry if under limit (max 3 retries)
					if($Colloseum::SpawnRetryCount2 < 3)
					{
						$Colloseum::SpawnRetryCount2 = $Colloseum::SpawnRetryCount2 + 1;
						echo("WARNING Colloseum: Round 2 spawn failed (attempt " @ $Colloseum::SpawnRetryCount2 @ "/3), retrying in 1 second...");
						schedule("Colloseum();", 1.0);
						return;
					}
					else
					{
						// Max retries reached - clear variables and log error
						echo("ERROR Colloseum: Round 2 spawn failed after 3 retries. Cannot spawn bot for guardtype " @ %guardtype);
						$Colloseum::BotName2 = "";
						$Colloseum::SpawnRetryCount2 = "";
						return;
					}
				}
				
				// Spawn successful - clear retry counter and proceed
				$Colloseum::SpawnRetryCount2 = "";
				
				// Set spawn cooldown timer to prevent premature death checks
				$Colloseum::LastSpawnTime = getSimTime();
				
				%pos = -3608 @ " " @ -2365 @ " " @ 354;
				// Pass INTERNAL NAME to setup (4.5s delay to ensure SpawnAIGetClientId completes first)
				// SpawnAIGetClientId is scheduled at 3.0s, so 4.5s gives it time to register the bot
				schedule("Colloseum::SetupBot(\"" @ $Colloseum::BotName2 @ "\", \"" @ %guardtype @ "\", \"" @ %pos @ "\", 0, " @ %clientId @ ");", 4.5);
				
				// Trigger immediate AI::Periodic() call at 5.0s (after bot is fully initialized)
				// This ensures bot starts chasing immediately after full initialization, before SetupBot() runs
				if($Colloseum::BotName2 != -1 && $Colloseum::BotName2 != "")
				{
					schedule("Colloseum::TriggerImmediatePeriodic(\"" @ $Colloseum::BotName2 @ "\", " @ %clientId @ ");", 5.0);
				}
				
				// Only increment round if we actually spawned (internal name is valid)
				if($Colloseum::BotName2 != -1 && $Colloseum::BotName2 != "")
				{
					$Round = $Round + 1;
					for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
						Client::sendMessage(%cl, $MsgBeige, $Contestant @ " has completed round 1 in the Colloseum!");
				}
			}
		}
		// Round 3: Check if Round 2 bot is dead and Round 3 hasn't started
		else if($Round == 2)
		{
			// Helper check: Verify Round 2 bot is truly gone or dead (not just missing from lookup)
			%round2BotId = -1;
			if($Colloseum::BotName2 != "" && $Colloseum::BotName2 != -1)
				%round2BotId = AI::getClientIdFromName($Colloseum::BotName2);
			%round2IsDeadOrGone = ($Colloseum::BotName2 == "" || %round2BotId == -1 || (%round2BotId != -1 && IsDead(%round2BotId)));
			
			if(%round2IsDeadOrGone)
			{
				%guardtype = $ColloseumRoundThree[$Fight];
				// Validate guardtype is not empty before spawning
				if(%guardtype == "" || %guardtype == -1)
				{
					echo("ERROR Colloseum: Round 3 - Invalid guardtype for $Fight=" @ $Fight @ ". Cannot spawn bot.");
					return;
				}
				// Use bot type name directly as display name (matches internal name pattern)
				%displayName = %guardtype;
				
				// Initialize spawn retry counter if not set
				if($Colloseum::SpawnRetryCount3 == "")
					$Colloseum::SpawnRetryCount3 = 0;
				
				// Capture internal name from AI::helper return value
				$Colloseum::BotName3 = AI::helper(%guardtype, %displayName, "TempSpawn " @ %lospos @ " " @ 1, default);
				
				// Validate spawn was successful before proceeding
				if($Colloseum::BotName3 == -1 || $Colloseum::BotName3 == "")
				{
					// Spawn failed - retry if under limit (max 3 retries)
					if($Colloseum::SpawnRetryCount3 < 3)
					{
						$Colloseum::SpawnRetryCount3 = $Colloseum::SpawnRetryCount3 + 1;
						echo("WARNING Colloseum: Round 3 spawn failed (attempt " @ $Colloseum::SpawnRetryCount3 @ "/3), retrying in 1 second...");
						schedule("Colloseum();", 1.0);
						return;
					}
					else
					{
						// Max retries reached - clear variables and log error
						echo("ERROR Colloseum: Round 3 spawn failed after 3 retries. Cannot spawn bot for guardtype " @ %guardtype);
						$Colloseum::BotName3 = "";
						$Colloseum::SpawnRetryCount3 = "";
						return;
					}
				}
				
				// Spawn successful - clear retry counter and proceed
				$Colloseum::SpawnRetryCount3 = "";
				
				// Set spawn cooldown timer to prevent premature death checks
				$Colloseum::LastSpawnTime = getSimTime();
				
				%pos = -3608 @ " " @ -2365 @ " " @ 354;
				// Pass INTERNAL NAME to setup (4.5s delay to ensure SpawnAIGetClientId completes first)
				// SpawnAIGetClientId is scheduled at 3.0s, so 4.5s gives it time to register the bot
				schedule("Colloseum::SetupBot(\"" @ $Colloseum::BotName3 @ "\", \"" @ %guardtype @ "\", \"" @ %pos @ "\", 0, " @ %clientId @ ");", 4.5);
				
				// Trigger immediate AI::Periodic() call at 5.0s (after bot is fully initialized)
				// This ensures bot starts chasing immediately after full initialization, before SetupBot() runs
				if($Colloseum::BotName3 != -1 && $Colloseum::BotName3 != "")
				{
					schedule("Colloseum::TriggerImmediatePeriodic(\"" @ $Colloseum::BotName3 @ "\", " @ %clientId @ ");", 5.0);
				}
				
				// Only increment round if we actually spawned (internal name is valid)
				if($Colloseum::BotName3 != -1 && $Colloseum::BotName3 != "")
				{
					$Round = $Round + 1;
					for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
						Client::sendMessage(%cl, $MsgBeige, $Contestant @ " has completed round 2 in the Colloseum!");
				}
			}
		}
		// Round 4: Check if Round 3 bot is dead and Round 4 (3-bot wave) hasn't started
		else if($Round == 3)
		{
			// Helper check: Verify Round 3 bot is truly gone or dead (not just missing from lookup)
			%round3BotId = -1;
			if($Colloseum::BotName3 != "" && $Colloseum::BotName3 != -1)
				%round3BotId = AI::getClientIdFromName($Colloseum::BotName3);
			%round3IsDeadOrGone = ($Colloseum::BotName3 == "" || %round3BotId == -1 || (%round3BotId != -1 && IsDead(%round3BotId)));
			
			if(%round3IsDeadOrGone)
			{
				%guardtype1 = $ColloseumRoundOne[$Fight];
				%guardtype2 = $ColloseumRoundTwo[$Fight];
				%guardtype3 = $ColloseumRoundThree[$Fight];
				
				// Validate all guardtypes are not empty before spawning
				if(%guardtype1 == "" || %guardtype1 == -1 || %guardtype2 == "" || %guardtype2 == -1 || %guardtype3 == "" || %guardtype3 == -1)
				{
					echo("ERROR Colloseum: Round 4 - Invalid guardtype(s) for $Fight=" @ $Fight @ ". guardtype1=" @ %guardtype1 @ ", guardtype2=" @ %guardtype2 @ ", guardtype3=" @ %guardtype3 @ ". Cannot spawn bots.");
					return;
				}
				
				// Use bot type names directly as display names (matches internal name pattern)
				%displayName1 = %guardtype1;
				%displayName2 = %guardtype2;
				%displayName3 = %guardtype3;
				
				// Initialize spawn retry counter if not set (shared for all 3 bots in this wave)
				if($Colloseum::SpawnRetryCount4 == "")
					$Colloseum::SpawnRetryCount4 = 0;
				
				// Capture internal names from AI::helper return values
				$Colloseum::BotName1 = AI::helper(%guardtype1, %displayName1, "TempSpawn " @ %lospos @ " " @ 1, default);
				$Colloseum::BotName2 = AI::helper(%guardtype2, %displayName2, "TempSpawn " @ %lospos @ " " @ 1, default);
				$Colloseum::BotName3 = AI::helper(%guardtype3, %displayName3, "TempSpawn " @ %lospos @ " " @ 1, default);
				
				// Validate at least one spawn was successful before proceeding
				%anySpawnSucceeded = false;
				if($Colloseum::BotName1 != -1 && $Colloseum::BotName1 != "")
					%anySpawnSucceeded = true;
				if($Colloseum::BotName2 != -1 && $Colloseum::BotName2 != "")
					%anySpawnSucceeded = true;
				if($Colloseum::BotName3 != -1 && $Colloseum::BotName3 != "")
					%anySpawnSucceeded = true;
				
				if(!%anySpawnSucceeded)
				{
					// All spawns failed - retry if under limit (max 3 retries)
					if($Colloseum::SpawnRetryCount4 < 3)
					{
						$Colloseum::SpawnRetryCount4 = $Colloseum::SpawnRetryCount4 + 1;
						echo("WARNING Colloseum: Round 4 (3-bot wave) spawn failed (attempt " @ $Colloseum::SpawnRetryCount4 @ "/3), retrying in 1 second...");
						schedule("Colloseum();", 1.0);
						return;
					}
					else
					{
						// Max retries reached - clear variables and log error
						echo("ERROR Colloseum: Round 4 (3-bot wave) spawn failed after 3 retries. Cannot spawn bots.");
						$Colloseum::BotName1 = "";
						$Colloseum::BotName2 = "";
						$Colloseum::BotName3 = "";
						$Colloseum::SpawnRetryCount4 = "";
						return;
					}
				}
				
				// At least one spawn successful - clear retry counter and proceed
				$Colloseum::SpawnRetryCount4 = "";
				
				// Set spawn cooldown timer to prevent premature death checks
				$Colloseum::LastSpawnTime = getSimTime();
				
				%pos = -3608 @ " " @ -2365 @ " " @ 354;
				// Pass INTERNAL NAMES to setup (4.5s delay to ensure SpawnAIGetClientId completes first)
				// SpawnAIGetClientId is scheduled at 3.0s, so 4.5s gives it time to register the bots
				// Only schedule SetupBot for bots that spawned successfully
				if($Colloseum::BotName1 != -1 && $Colloseum::BotName1 != "")
				{
					schedule("Colloseum::SetupBot(\"" @ $Colloseum::BotName1 @ "\", \"" @ %guardtype1 @ "\", \"" @ %pos @ "\", 0, " @ %clientId @ ");", 4.5);
					// Trigger immediate AI::Periodic() call at 5.0s (after bot is fully initialized)
					schedule("Colloseum::TriggerImmediatePeriodic(\"" @ $Colloseum::BotName1 @ "\", " @ %clientId @ ");", 5.0);
				}
				if($Colloseum::BotName2 != -1 && $Colloseum::BotName2 != "")
				{
					schedule("Colloseum::SetupBot(\"" @ $Colloseum::BotName2 @ "\", \"" @ %guardtype2 @ "\", \"" @ %pos @ "\", 0, " @ %clientId @ ");", 4.5);
					// Trigger immediate AI::Periodic() call at 5.0s (after bot is fully initialized)
					schedule("Colloseum::TriggerImmediatePeriodic(\"" @ $Colloseum::BotName2 @ "\", " @ %clientId @ ");", 5.0);
				}
				if($Colloseum::BotName3 != -1 && $Colloseum::BotName3 != "")
				{
					schedule("Colloseum::SetupBot(\"" @ $Colloseum::BotName3 @ "\", \"" @ %guardtype3 @ "\", \"" @ %pos @ "\", 0, " @ %clientId @ ");", 4.5);
					// Trigger immediate AI::Periodic() call at 5.0s (after bot is fully initialized)
					schedule("Colloseum::TriggerImmediatePeriodic(\"" @ $Colloseum::BotName3 @ "\", " @ %clientId @ ");", 5.0);
				}
				
				// Only increment round if at least one bot spawned successfully
				if(($Colloseum::BotName1 != -1 && $Colloseum::BotName1 != "") || ($Colloseum::BotName2 != -1 && $Colloseum::BotName2 != "") || ($Colloseum::BotName3 != -1 && $Colloseum::BotName3 != ""))
				{
					$Round = $Round + 1;
					for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
						Client::sendMessage(%cl, $MsgBeige, $Contestant @ " has completed round 3 in the Colloseum!");
				}
			}
		}
		// Round 5 (Final): Check if all three Round 4 bots are dead
		else if($Round == 4)
		{
			// Helper checks: Verify all bots are truly gone or dead (not just missing from lookup)
			%round4Bot1Id = -1;
			%round4Bot2Id = -1;
			%round4Bot3Id = -1;
			if($Colloseum::BotName1 != "" && $Colloseum::BotName1 != -1)
				%round4Bot1Id = AI::getClientIdFromName($Colloseum::BotName1);
			if($Colloseum::BotName2 != "" && $Colloseum::BotName2 != -1)
				%round4Bot2Id = AI::getClientIdFromName($Colloseum::BotName2);
			if($Colloseum::BotName3 != "" && $Colloseum::BotName3 != -1)
				%round4Bot3Id = AI::getClientIdFromName($Colloseum::BotName3);
			%round4Bot1IsDeadOrGone = ($Colloseum::BotName1 == "" || %round4Bot1Id == -1 || (%round4Bot1Id != -1 && IsDead(%round4Bot1Id)));
			%round4Bot2IsDeadOrGone = ($Colloseum::BotName2 == "" || %round4Bot2Id == -1 || (%round4Bot2Id != -1 && IsDead(%round4Bot2Id)));
			%round4Bot3IsDeadOrGone = ($Colloseum::BotName3 == "" || %round4Bot3Id == -1 || (%round4Bot3Id != -1 && IsDead(%round4Bot3Id)));
			
			if(%round4Bot1IsDeadOrGone && %round4Bot2IsDeadOrGone && %round4Bot3IsDeadOrGone)
			{
				storeData(%clientId, "TournyRank", 1, "inc");
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					Client::sendMessage(%cl, $MsgBeige, $Contestant @ " has completed completed the Colloseum and earned the rank of " @ $WorldRank[fetchData(%clientId, "TournyRank")] @ "!");
				%lospos = -335 @ " " @ -2339 @ " " @ 65.5;
				GameBase::setPosition(%clientId, %lospos);
				$Round = "";
				$Contestant = "";
				$Fight = "";
				$AImoveChance = 99999;
				// Clear internal name tracking variables, spawn cooldown, retry counters, and target clientID
				$Colloseum::BotName1 = "";
				$Colloseum::BotName2 = "";
				$Colloseum::BotName3 = "";
				$Colloseum::LastSpawnTime = "";
				$Colloseum::SpawnRetryCount1 = "";
				$Colloseum::SpawnRetryCount2 = "";
				$Colloseum::SpawnRetryCount3 = "";
				$Colloseum::SpawnRetryCount4 = "";
				$Colloseum::TargetClientId = "";
			}
		}
	}
		
	if(%challengerzone != "Colloseum" && $Fight != "")
	{
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgBeige, $Contestant @ " has died and failed in the Colloseum!");
		if(IsDead(%clientId))
			Game::playerSpawn(%clientId, True);
		%lospos = -3588 @ " " @ -2364 @ " " @ 354;
		GameBase::setPosition(%clientId, %lospos);
		Client::sendMessage(%clientId, $MsgWhite, "You have one minute to retrieve your pack before you are teleported out.");
		if(NEWgetClientByName(Round3) != -1)
		{
			storeData(NEWgetClientByName(Round3), "noDropLootbagFlag", True);
			Player::Kill(NEWgetClientByName(Round3));
		}
		if(NEWgetClientByName(Round2) != -1)
		{
			storeData(NEWgetClientByName(Round2), "noDropLootbagFlag", True);
			Player::Kill(NEWgetClientByName(Round2));
		}
		if(NEWgetClientByName(Round1) != -1)
		{
			storeData(NEWgetClientByName(Round1), "noDropLootbagFlag", True);
			Player::Kill(NEWgetClientByName(Round1));
		}
		$Fight = "";
		schedule("ColloLost();",60);
	}

	if($Contestant != "")
		schedule("Colloseum();",5);
}

function ColloLost()
{
	%clientId = NEWgetClientByName($Contestant);
	%lospos = -335 @ " " @ -2339 @ " " @ 65.5;
	GameBase::setPosition(%clientId, %lospos);
	$Round = "";
	$Contestant = "";
	$Fight = "";
	$AImoveChance = 99999;
	$Colloseum::TargetClientId = "";
}