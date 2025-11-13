//Globals for seal system; %i is the id of the battle.
//$SealBattleActive: If true, then a seal battle is currently active for a player.
//Just leave this one as it is.
//$SealValue[%i]: The seal value that is assigned to the player who wins this battle.
//$SealEnemy[%i,1]: The enemy used for the 1st wave. Use the bot's spawnIndex value.
//$SealEnemy[%i,2]: The enemy used for the 2nd wave. Use the bot's spawnIndex value.
//$SealEnemy[%i,3]: The enemy used for the final wave. Use the bot's spawnIndex value.
$SealBattleActive = false;
%i=0;

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

//multiplier for each additional seal that needs to be broken to dynamically scale
function SealBattle::GetEnemyStrengthMultiplier()
{
    %baseMultiplier = 1;
    %increment = 0.1;
    %sealValue = GetTotalSealValue();
    %steps = %sealValue / 20;
    %multiplier = %baseMultiplier + (%steps * %increment);
    return %multiplier;
}

function SealBattle::Begin(%clientId,%pos,%seal)
{
	if($SealBattleActive != true)
	{
		storeData(%clientId,"noDropLootbagFlag",false);
		$AImoveChance = 1;
		$SealFighterDied = false;
		$SealBattleActive = true;
		SealBattle::Loop(%clientId,%pos,%seal,1);
		storeData(Sealbot,"noDropLootbagFlag",True);
		storeData(Sealbot2,"noDropLootbagFlag",True);
		storeData(Sealbot3,"noDropLootbagFlag",True);

	}
}
function SealBattle::Loop(%clientId,%pos,%seal,%round)
{
	%name = Client::getName(%clientId);
	%player = Client::getOwnedObject(%clientId);
	%zone = Zone::getDesc(fetchData(%clientId, "zone"));
	if(%round == 1)
	{
		%bot = 66; //fighter bot
		%bot2 = 67; //mage bot
		%bot3 = 68; //Guardian bot
		AI::helper($spawnIndex[%bot],"SealFighter1","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealFighter1");
		storeData(%aiId, "botAttackMode", 1);
		storeData(%aiId, "tmpbotdata", %pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));

		AI::helper($spawnIndex[%bot2],"SealMage1","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealMage1");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));

		AI::helper($spawnIndex[%bot3],"SealGuardian1","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealGuardian1");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		// Example usage inside SealBattle::Loop after spawning each bot
		// Replace this block for each bot after AI::helper and storeData calls

		%round++;
		messageall(2,"" @ %name @ " has begun to break the seal. Type #helpseal to help.");
		%sealValue = GetTotalSealValue();
		messageall(2,"The current seal value is " @ %sealValue @ "");
	}
	else if(%round == 2 && NEWgetClientByName("SealFighter1") == -1
	&& NEWgetClientByName("SealMage1") == -1 && NEWgetClientByName("SealGuardian1") == -1)
	{
		%bot = 69;
		%bot2 = 70;
		%bot3 = 71;
		messageall(2,"" @ %name @ " is entering wave "@%round@".");
		AI::helper($spawnIndex[%bot],"SealFighter2","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealFighter2");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		AI::helper($spawnIndex[%bot2],"SealMage2","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealMage2");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		AI::helper($spawnIndex[%bot3],"SealGuardian2","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealGuardian2");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		%round++;
	}
	else if(%round == 3 && NEWgetClientByName("SealFighter2") == -1
	&& NEWgetClientByName("SealMage2") == -1 && NEWgetClientByName("SealGuardian2") == -1)
	{
		%bot = 72;
		%bot2 = 73;
		%bot3 = 74;
		messageall(2,"" @ %name @ " is entering wave "@%round@".");
		AI::helper($spawnIndex[%bot],"SealFighter3","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealFighter3");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		AI::helper($spawnIndex[%bot2],"SealMage3","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealMage3");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		AI::helper($spawnIndex[%bot3],"SealGuardian3","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealGuardian3");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		%round++;
	}
	else if(%round == 4  && NEWgetClientByName("SealFighter3") == -1
	&& NEWgetClientByName("SealMage3") == -1 && NEWgetClientByName("SealGuardian3") == -1)
	{
		%bot = 75;
		%bot2 = 76;
		%bot3 = 77;
		messageall(2,"" @ %name @ " is entering the final wave!");
		AI::helper($spawnIndex[%bot],"SealFighter4","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealFighter4");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		AI::helper($spawnIndex[%bot2],"SealMage4","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealMage4");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		AI::helper($spawnIndex[%bot3],"SealGuardian4","TempSpawn -3608 -2365 354 1",default);
		%aiId = NEWgetClientByName("SealGuardian4");
		storeData(%aiId,"botAttackMode",1);
		storeData(%aiId,"tmpbotdata",%pos);
		storeData(%aiId, "noDropLootbagFlag", True);
		%mult = SealBattle::GetEnemyStrengthMultiplier(); //adjust these multipliers to change difficulty scaling
		storeData(%aiId, "HP", floor(120 * %mult));
		storeData(%aiId, "DMG", floor(25 * %mult));
		storeData(%aiId, "DEF", floor(15 * %mult));
		%round++;
	}
	else if(%round == 5 && NEWgetClientByName("SealFighter4") == -1
		&& NEWgetClientByName("SealMage4") == -1 && NEWgetClientByName("SealGuardian4") == -1)
	{
		Client::sendMessage(%clientId,$MsgBeige,"The final wave has been beaten! The seal has been shattered...for now.~wflag_capture.wav");
		
		// Increment and save seal value using centralized function
		%newSealValue = IncrementTotalSealValue();
		messageall(2,"The remort cap is now " @ %newSealValue @ "!");
		
		Saveworld();
		SealBattle::Conclude(%clientId);
		return;
	}
	//if(%zone != "Colloseum") //$SealBattleActive == True && 
	//{
	//	if(NEWgetClientByName("SealGuardian") != -1)
	//		Player::Kill(NEWgetClientByName("SealGuardian"));
	//	if(NEWgetClientByName("SealMage") != -1)
	//		Player::Kill(NEWgetClientByName("SealMage"));
	//	if(NEWgetClientByName("SealFighter") != -1)
	//		Player::Kill(NEWgetClientByName("SealFighter"));
	//	//round 2
	//	if(NEWgetClientByName("SealGuardian1") != -1)
	//		Player::Kill(NEWgetClientByName("SealGuardian1"));
	//	if(NEWgetClientByName("SealMage1") != -1)
	//		Player::Kill(NEWgetClientByName("SealMage1"));
	//	if(NEWgetClientByName("SealFighter1") != -1)
	//		Player::Kill(NEWgetClientByName("SealFighter1"));
	//	///3rd rnd
	//	if(NEWgetClientByName("SealGuardian2") != -1)
	//		Player::Kill(NEWgetClientByName("SealGuardian2"));
	///	if(NEWgetClientByName("SealMage2") != -1)
	//		Player::Kill(NEWgetClientByName("SealMage2"));
	//	if(NEWgetClientByName("SealFighter2") != -1)
	//		Player::Kill(NEWgetClientByName("SealFighter2"));
	//	//round 3
	//	if(NEWgetClientByName("SealGuardian3") != -1)
	//		Player::Kill(NEWgetClientByName("SealGuardian3"));
	///	if(NEWgetClientByName("SealMage3") != -1)
	//		Player::Kill(NEWgetClientByName("SealMage3"));
	//	if(NEWgetClientByName("SealFighter3") != -1)
	//		Player::Kill(NEWgetClientByName("SealFighter3"));
	//	SealBattle::Conclude(%clientId);
	//	return;
	//}
	//if(%challengerzone != "Colloseum" && $SealBattleActive != true)
	//{
	//	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	//		Client::sendMessage(%cl, $MsgBeige, $Contestant @ " has died and failed at breaking the seal!");
	//	if(IsDead(%clientId))
	//		Game::playerSpawn(%clientId, True);
	//	%lospos = -3588 @ " " @ -2364 @ " " @ 354;
	//	GameBase::setPosition(%clientId, %lospos);
	//	Client::sendMessage(%clientId, $MsgWhite, "You have one minute to retrieve your pack before you are teleported out.");
	//if($SealFighterDied != true)
	//{
//		for(%i=0;%i<10;%i++)
//		schedule("SealBattle::LazyDeathCheck("@%clientId@");",%i/2,%player);
//		schedule("SealBattle::Loop("@%clientId@",\""@%pos@"\","@%seal@","@%round@","@$SealFighterDied@");",5);
//		}
//	}
//	
//	}
//	if(%zone != "Colloseum" && $SealBattleActive = true)
//	for(%i=0;%i<10;%i++)	
//	messageall(2,"" @ %name @ " has fled the battle to break the seal! All hope is lost...");
//	SealBattle::Conclude(%clientId);
//	return;
//	}
	if($SealFighterDied)
	{
		//Play random enemy taunt sound (just to annoy the hell out of the player >:D)
		%randtaunt = floor(getRandom()*3);
		if(%randtaunt == 0) %sound = "OgreTaunt1.wav";
		if(%randtaunt == 1) %sound = "UndeadTaunt1.wav";
		if(%randtaunt == 2) %sound = "GnollTaunt1.wav";
		Client::sendMessage(%cl,$MsgRed,"You died...~w"@%sound);
		if(NEWgetClientByName("SealGuardian1") != -1)
			Player::Kill(NEWgetClientByName("SealGuardian1"));
		if(NEWgetClientByName("SealMage1") != -1)
			Player::Kill(NEWgetClientByName("SealMage1"));
		if(NEWgetClientByName("SealFighter1") != -1)
			Player::Kill(NEWgetClientByName("SealFighter1"));
		//round 2
		if(NEWgetClientByName("SealGuardian2") != -1)
			Player::Kill(NEWgetClientByName("SealGuardian2"));
		if(NEWgetClientByName("SealMage2") != -1)
			Player::Kill(NEWgetClientByName("SealMage2"));
		if(NEWgetClientByName("SealFighter2") != -1)
			Player::Kill(NEWgetClientByName("SealFighter2"));
		///3rd rnd
		if(NEWgetClientByName("SealGuardian3") != -1)
			Player::Kill(NEWgetClientByName("SealGuardian3"));
		if(NEWgetClientByName("SealMage3") != -1)
			Player::Kill(NEWgetClientByName("SealMage3"));
		if(NEWgetClientByName("SealFighter3") != -1)
			Player::Kill(NEWgetClientByName("SealFighter3"));
		//round 3
		if(NEWgetClientByName("SealGuardian4") != -1)
			Player::Kill(NEWgetClientByName("SealGuardian4"));
		if(NEWgetClientByName("SealMage4") != -1)
			Player::Kill(NEWgetClientByName("SealMage4"));
		if(NEWgetClientByName("SealFighter4") != -1)
			Player::Kill(NEWgetClientByName("SealFighter4"));
		SealBattle::Conclude(%clientId);
		if(IsDead(%clientId))
			Game::playerSpawn(%clientId, True);
		%lospos = "-3588 -2364 354";
		GameBase::setPosition(%clientId, %lospos);
		Client::sendMessage(%clientId, $MsgWhite, "You have one minute to retrieve your pack before you are teleported out.");
		return;
	}
	if($SealFighterDied != true)
	{
		for(%i=0;%i<10;%i++)
		schedule("SealBattle::LazyDeathCheck("@%clientId@");",%i/2,%player);
		schedule("SealBattle::Loop("@%clientId@",\""@%pos@"\","@%seal@","@%round@","@$SealFighterDied@");",5);
		}
	}
function SealBattle::LazyDeathCheck(%clientId)
{
	if(IsDead(%clientId))
		$SealFighterDied = true;

}
function SealBattle::Conclude(%clientId)
{
	storeData(%clientId,"noDropLootbagFlag",false);
	GameBase::setPosition(%clientId,"-335 -2339 65.5");
	$SealFighterDied = false;
	$SealBattleActive = false;
	$AImoveChance = 99999;
}