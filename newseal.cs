function Seal();
{
	%clientId = NEWgetClientByName($Contestant);
	%challengerzone = Zone::getDesc(fetchData(%clientId, "zone"));
	%lospos = -3608 @ " " @ -2365 @ " " @ 354;
	
	if($Fight != "")
	{
		$AImoveChance = 1;
		if($Round == 1 && NEWgetClientByName(Round1) == -1)
		{
			AI::helper($TournyRoundOne[5], Round1, "TempSpawn " @ %lospos @ " " @ 1, default);
			AI::helper($TournyRoundTwo[5], Round2, "TempSpawn " @ %lospos @ " " @ 1, default);
			AI::helper($TournyRoundThree[5], Round3, "TempSpawn " @ %lospos @ " " @ 1, default);
			%roundoneId = NEWgetClientByName(Round1);
			%roundtwoId = NEWgetClientByName(Round2);
			%roundthreeId = NEWgetClientByName(Round3);
			%pos = -3608 @ " " @ -2365 @ " " @ 354;
			storeData(%roundoneId, "botAttackMode", 3);
			storeData(%roundoneId, "tmpbotdata", %pos);
			storeData(%roundtwoId, "botAttackMode", 3);
			storeData(%roundtwoId, "tmpbotdata", %pos);
			storeData(%roundthreeId, "botAttackMode", 3);
			storeData(%roundthreeId, "tmpbotdata", %pos);
			$Round = $Round + 1;
			for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
				Client::sendMessage(%cl, $MsgBeige, $Contestant @ " has completed the first wave of the seal!");
		}
		else if($Round == 2 && NEWgetClientByName(Round3) == -1 && NEWgetClientByName(Round2) == -1 && NEWgetClientByName(Round1) == -1)
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
}