function Invasion()
{
	if($InvasionTimer == "")
		$InvasionTimer = 0;
	if($InvasionTimer == 0)
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgRed, "Areinis Village is about to be invaded!");
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
		%lospos = -666.131 @ " " @ 764.96 @ " " @ 131.593;
		%n = AI::helper(Enforcer, Enforcer0, "TempSpawn " @ %lospos @ " " @ 4, default);
		%id = NEWgetClientByName("Enforcer0");
		storeData(%id, "frozen", True);
		%lospos = -457 @ " " @ 769 @ " " @ 137.25;
		%n = AI::helper(General, General0, "TempSpawn " @ %lospos @ " " @ 0, default);
		%id = NEWgetClientByName("General0");
		storeData(%id, "frozen", True);
		for (%x = 0; %x < 3; %x = %x + 1)
		{ 
			%lospos = -664 @ " " @ 770 + (3 * %x) @ " " @ 131.791;
			%n = AI::helper(MazeYeti, Lord @ %x, "TempSpawn " @ %lospos @ " " @ 4, default);
			%id = NEWgetClientByName("Lord" @ %x );
			storeData(%id, "frozen", True);
		}
		for (%x = 0; %x < 4; %x = %x + 1)
		{ 
			%lospos = -672 @ " " @ 768 + (%x * 3) @ " " @ 131.281;
			%n = AI::helper(MazeYetiShaman, MazeYetiShaman @ %x, "TempSpawn " @ %lospos @ " " @ 4, default);
			%id = NEWgetClientByName("MazeYetiShama" @ %x );
			storeData(%id, "frozen", True);
		}
		for (%x = 0; %x < 5; %x = %x + 1)
		{ 
			%lospos = -681 + (%x * 3) @ " " @ 769 + (%x * 3) @ " " @ 130.661;
			%n = AI::helper(MazeYeti, Guard @ %x, "TempSpawn " @ %lospos @ " " @ 4, default);
			%id = NEWgetClientByName("MazeYetiShama" @ %x );
			storeData(%id, "frozen", True);
		}
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgRed, "The are preparing their attack, everyone be prepared!");
	}
	if($InvasionTimer >= 24)
	{
		%id = NEWgetClientByName("Enforcer0");
		if($InvasionTimer == 24)
			storeData(%id, "frozen", "");
		%lospos = -460 @ " " @ 763 @ " " @ 137.191;
		storeData(%id, "botAttackMode", 3);
		storeData(%id, "tmpbotdata", %lospos);
		for(%x = 0; %x < 3; %x = %x + 1)
		{
			%id = NEWgetClientByName("Lord" @ %x );
			if($InvasionTimer == 24)
				storeData(%id, "frozen", "");
			%lospos = -460 @ " " @ 763 @ " " @ 137.191;
			storeData(%id, "botAttackMode", 3);
			storeData(%id, "tmpbotdata", %lospos);	
		}
		for(%x = 0; %x < 4; %x = %x + 1)
		{
			%id = NEWgetClientByName("MazeYetiShama" @ %x );
			if($InvasionTimer == 24)
				storeData(%id, "frozen", "");
			%lospos = -460 @ " " @ 763 @ " " @ 137.191;
			storeData(%id, "botAttackMode", 3);
			storeData(%id, "tmpbotdata", %lospos);	
		}
		for(%x = 0; %x < 5; %x = %x + 1)
		{
			%id = NEWgetClientByName("Guard" @ %x );
			if($InvasionTimer == 24)
				storeData(%id, "frozen", "");
			%lospos = -460 @ " " @ 763 @ " " @ 137.191;
			storeData(%id, "botAttackMode", 3);
			storeData(%id, "tmpbotdata", %lospos);	
		}
		%id = NEWgetClientByName("General0");
		if($InvasionTimer == 24)
			storeData(%id, "frozen", "");
		%lospos = -460 @ " " @ 763 @ " " @ 137.191;
		storeData(%id, "botAttackMode", 3);
		storeData(%id, "tmpbotdata", %lospos);
	}
	if($InvasionTimer == 24)
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, $MsgRed, "The army is advancing! Defend the General or you will all die!");
	if($InvasionTimer >= 24)
	{
		%id = NEWgetClientByName("General0");
		%ids = NEWgetClientByName("Enforcer0");
		if(%id == -1)
		{
			for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			{
				if(Vector::getDistance(GameBase::getPosition(%cl), GameBase::getPosition(%ids)) <= 500)
				{
					playNextAnim(%cl);
					Player::Kill(%cl);
					Client::sendMessage(%cl, $MsgRed, "The city has been Destroyed!!");
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
					Client::sendMessage(%cl, $MsgRed, "The Undead Enforcer has been destroyed and the General still lives! You have been paid for your efforts!");
				}
			}
			$InvasionTimer = "";
		}
	}
	
	if($InvasionTimer != "")
	{
		$InvasionTimer = $InvasionTimer + 1;
		schedule("Invasion();",5);
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