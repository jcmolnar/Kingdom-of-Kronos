banlist::add("IP:220.240.250.154",9999999);

function wipeout()
{
	%list = GetBotIdList();

	for(%i=0;getword(%list,%i) != -1;%i++)
	{
		if($npc[getword(%list,%i)] != "True")
			felloffmap(getword(%list,%i));
	}
}

function linebreak::check(%cropped,%w1)
{
	if(%w1 != "#setinfo" && %w1 != "#addinfo")
	{
	 	if(String::findSubStr(%cropped, "\t") != -1)			%block = 1;
		else if(String::findSubStr(%cropped, "\n") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x01") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x02") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x03") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x04") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x05") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x06") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x07") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x08") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x09") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x10") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x11") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x12") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x13") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x14") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x15") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x16") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x17") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x18") != -1)		%block = 1;
		else if(String::findSubStr(%cropped, "\x19") != -1)		%block = 1;
	}
	else if(String::findSubStr(%cropped, "<Bloding.bmp>") != -1)	%block = 1;

	if(%block == 1)
		return true;
	else
		return false;
}

function Carling::setTeam(%clientid, %team)
{
	if(Player::isAiControlled(%clientId))
		storedata(%clientid,"GROUP","AI");
	else
		gamebase::setteam(%clientid,0);
}

function Carling::getTeam(%clientid)
{
	if(Player::isAiControlled(%clientId))
		%team = 2;
	else
		%team = 1;

	return %team;
}

function nexec(%f)
{
	$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;
	exec(%f);
}

function carling::changerace(%clientid, %team)
{
	if(%team >= 0 && %team < 11) 
	{
		if(%team == 1)		%team = "Human";		// #
		else if(%team == 2)	%team = "Goblin";		// #
		else if(%team == 3)	%team = "Orc";
		else if(%team == 4)	%team = "Zombie";
		else if(%team == 5)	%team = "Skeleton";
		else if(%team == 6)	%team = "Minotaur";	// #
		else if(%team == 7)	%team = "Uber";		// #
		else if(%team == 8)	%team = "Demon";
		else if(%team == 9)	%team = "Ogre";
		else if(%team == 10)	%team = "Ghost";

		storedata(%clientid,"GROUP",%team);
		RefreshWeight(%clientId);
		UpdateAppearance(%clientId);
		RefreshAll(%clientId);
	}
	else
	{
		Client::sendmessage(%clientid, 0, "Syntax: #changerace 1-10");
		return;
	}
}

$RomanNumeral[1] = "I";
$RomanNumeral[5] = "V";
$RomanNumeral[10] = "X";
$RomanNumeral[50] = "L";
$RomanNumeral[100] = "C";
$RomanNumeral[500] = "D";
$RomanNumeral[1000] = "M";

function RomanNumeral::Display(%n)
{
	if(%n <= 0)
		return "";

	%len = String::len(%n);

	if(%len == 1)
		%w = String::getSubStr(%n, 0, 1);	//tens
	else if(%len == 2)
	{
		%x = String::getSubStr(%n, 0, 1);	//units
		%w = String::getSubStr(%n, 1, 2);	//tens
	}
	else if(%len == 3)
	{
		%y = String::getSubStr(%n, 0, 1);	//hundreds
		%x = String::getSubStr(%n, 1, 1);	//units
		%w = String::getSubStr(%n, 2, 3);	//tens
	}
	else if(%len >= 4)
	{
		%z = String::getSubStr(%n, 0, 1);	//thosands
		%y = String::getSubStr(%n, 1, 1);	//hundreds
		%x = String::getSubStr(%n, 2, 1);	//units
		%w = String::getSubStr(%n, 3, 4);	//tens
	}
//-------------------------------------------------
//--- Units----------------------------------------
//-------------------------------------------------
	if(%w <= 3)
	{
		for(%i=1;%i<=%w;%i++)
			%a = %a@$RomanNumeral[1];
	}
	else if(%w == 4)
		%a = $RomanNumeral[1]@$RomanNumeral[5];
	else if(%w == 5)
		%a = $RomanNumeral[5];
	else if(%w > 5 && %w < 9)
	{
		%a = $RomanNumeral[5];
		for(%i=6;%i<=%w;%i++)
			%a = %a@$RomanNumeral[1];
	}
	else if(%w == 9)
		%a = $RomanNumeral[1]@$RomanNumeral[10];
//-------------------------------------------------
//--- Tens ----------------------------------------
//-------------------------------------------------
	if(%x <= 3)
		for(%i=1;%i<=%x;%i++)
			%b = %b@$RomanNumeral[10];
	else if(%x == 4)
		%b = $RomanNumeral[10]@$RomanNumeral[50];
	else if(%x == 5)
		%b = $RomanNumeral[50];
	else if(%x > 5 && %x < 9)
	{
		%b = $RomanNumeral[50];
		for(%i=6;%i<=%x;%i++)
			%b = %b@$RomanNumeral[10];
	}		
	else if(%x == 9)
		%b = $RomanNumeral[10]@$RomanNumeral[100];
//-------------------------------------------------
//--- Hundreds ------------------------------------
//-------------------------------------------------
	if(%y <= 3)
		for(%i=1;%i<=%y;%i++)
			%c = %c@$RomanNumeral[100];
	else if(%y == 4)
		%c = $RomanNumeral[100]@$RomanNumeral[500];
	else if(%y == 5)
		%c = $RomanNumeral[500];
	else if(%y > 5 && %y < 9)
	{
		%c = $RomanNumeral[500];
		for(%i=6;%i<=%y;%i++)
			%c = %c@$RomanNumeral[100];
	}		
	else if(%y == 9)
		%c = $RomanNumeral[100]@$RomanNumeral[1000];
//-------------------------------------------------
//--- Thosands ------------------------------------
//-------------------------------------------------
	if(%z <= 9)
		for(%i=1;%i<=%z;%i++)
			%d = %d@$RomanNumeral[1000];
//-------------------------------------------------
	return %d@%c@%b@%a;
}

function Smoke::Check(%clientid)
{
	%target = Client::getOwnedObject(%clientid);

	if(fetchdata(%clientid,"GROUP") == "Ghost")
	{
		if($allowSmokeTrail[%target] != "true")
		{
			enableSmokeTrail(%target,true);
		}
	}
	else if($allowSmokeTrail[%target] == "true")
	{
		if(client::getname(%clientid) != "Ghost" && !$npc[%clientid])
		enableSmokeTrail(%target,false);
	}
}

exec(SmokeTrails);


function item::add(%tag,%item,%pos,%rot,%type,%pos2,%rot2)
{
	%switch = true;

	if(%type == "dis") {
		%type = "InteriorShape";
		%item = %item@".dis";
	}
	else if(%type == "static")
		%type = "StaticShape";
	else if(%type == "turret")
	{
		%type = "Turret";
		%switch = false;
	}
	else return;

	%object = newObject(%tag,%type,%item,%switch);
	addToSet("MissionCleanup", %object);
	GameBase::setPosition(%object,%pos);
	GameBase::setRotation(%object,%rot);

	if(%type == "Turret")
	{
		GameBase::setActive(%object,true);
		GameBase::setTeam(%object,0);
		GameBase::setRechargeRate(%object,14);

		if(%pos2 != "") 
		{
			%this = newObject(%tag,"StaticShape","CommandStation",true);
			addToSet("MissionCleanup", %this);
			GameBase::setRotation(%this,%rot2);
			GameBase::setPosition(%this,%pos2);
			GameBase::setActive(%this,true);
			GameBase::setRechargeRate(%this,14);
			GameBase::setTeam(%this,0);
			%this.control = %object;
		}
	}

	if(getword(%tag,0) == "Door" && isObject(%object))
	{
		if(getword(%tag,1) == "T")
		{
			%object.pos = %pos2;
			%object.rot = %rot2;
		}
		else
			%object.pos = %pos;
	}
	return %object;
}

function spawn::choice()
{
	item::add("cgoblin","FlagStand","-1994 -2016.5 -1982.5","0 0 0","static");
	item::add("cminotaur","FlagStand","-1997 -2016.5 -1982.5","0 0 0","static");
	item::add("cuber","FlagStand","-2000 -2016.5 -1982.5","0 0 0","static");
	item::add("cghost","FlagStand","-2003 -2016.5 -1982.5","0 0 0","static");
	item::add("chuman","FlagStand","-2006 -2016.5 -1982.5","0 0 0","static");

	%n = AI::helper("runt", "Goblin", "TempSpawn -1994 -2018 -1982.5 0", "default");
	%id = AI::getId(%n);
	storeData(%id, "frozen", True);
	AI::setVar(fetchData(%id, "BotInfoAiName"), SpotDist, 0);
	AI::newDirectiveRemove(fetchData(%id, "BotInfoAiName"), 99);
	gamebase::setrotation(%id,"0 0 0");
	$npc[%id] = "True";

	%n = AI::helper("reaper", "Minotaur", "TempSpawn -1997 -2018 -1982.5 0", "default");
	%id = AI::getId(%n);
	storeData(%id, "frozen", True);
	AI::setVar(fetchData(%id, "BotInfoAiName"), SpotDist, 0);
	AI::newDirectiveRemove(fetchData(%id, "BotInfoAiName"), 99);
	gamebase::setrotation(%id,"0 0 0");
	gamebase::setteam(%id,6);
	$npc[%id] = "True";
	storedata(%id,"GROUP","Minotaur");

	%n = AI::helper("Sloth", "Uber", "TempSpawn -2000 -2018 -1982.5 0", "default");
	%id = AI::getId(%n);
	storeData(%id, "frozen", True);
	AI::setVar(fetchData(%id, "BotInfoAiName"), SpotDist, 0);
	AI::newDirectiveRemove(fetchData(%id, "BotInfoAiName"), 99);
	gamebase::setrotation(%id,"0 0 0");
	$npc[%id] = "True";

	%n = AI::helper("Specta", "Ghost", "TempSpawn -2003 -2018 -1982.5 0", "default");
	%id = AI::getId(%n);
	storeData(%id, "frozen", True);
	AI::setVar(fetchData(%id, "BotInfoAiName"), SpotDist, 0);
	AI::newDirectiveRemove(fetchData(%id, "BotInfoAiName"), 99);
	gamebase::setrotation(%id,"0 0 0");
	$npc[%id] = "True";
	%target = Client::getOwnedObject(%id);
	enableSmokeTrail(%target,true);

	%r = floor(getrandom() * 2) + 1;
	if(%r == 1)
		%type = "Generic";
	else 
		%type = "Porter";

	%n = AI::helper(%type, "Human", "TempSpawn -2006 -2018 -1982.5 0", "default");
	%id = AI::getId(%n);
	storeData(%id, "frozen", True);
	AI::setVar(fetchData(%id, "BotInfoAiName"), SpotDist, 0);
	AI::newDirectiveRemove(fetchData(%id, "BotInfoAiName"), 99);
	gamebase::setrotation(%id,"0 0 0");
	$npc[%id] = "True";
	%target = Client::getOwnedObject(%id);
	gamebase::setteam(%id,1);
	storedata(%id,"GROUP","Human");
}

$NameForRace[Specta] = "Ghost";
$ArmorTypeToRace[GhostArmor7] = "Ghost";
$RaceToArmorType[Ghost] = "GhostArmor7";
$spawnIndex[54] = "Ghost";
$BotEquipment[Ghost] = 	"CLASS Mage LVL 527/50 COINS 135/50 LCK 4 CastingBlade 1 DragonScale 1/-300 Emerald 1/-1000";
$TeamForRace[Ghost] = 4;
$RaceSound[Ghost, Death, 1] = SoundUndeadDeath1;
$RaceSound[Ghost, Acquired, 1] = SoundUndeadAcquired1;
$RaceSound[Ghost, Hit, 1] = SoundUndeadHit1;
$RaceSound[Ghost, Hit, 2] = SoundUndeadHit2;
$RaceSound[Ghost, Taunt, 1] = SoundUndeadTaunt1;
$RaceSound[Ghost, RandomWait, 1] = SoundUndeadRandom1;

function Carling::EnterWorld(%clientid,%obj)
{
	%pos = "-2436.83 -248.797 65.999";

	%rand = floor(getrandom() * 3) + 1;

	if(%rand == 1)
		%pos = "-2450.71 -242.829 65.9999";
	else if(%rand == 2)
		%pos = "-2433.26 -235.207 60.875";
	else if(%rand == 3)
		%pos = "-2444.45 -255.114 56";

	if(fetchdata(%clientid,"GROUP") != "Ghost")
	{
		storedata(%clientid,"COINS",20);
		GiveThisStuff(%clientId, "PickAxe 1", False);
		storedata(%clientId, "BankStorage", "Hatchet 1 Club 1 BluePotion 3 CrystalBluePotion 2");
	}
	else
	{
		GiveThisStuff(%clientId, "EnergyBall 1", False);
	}

	storedata(%clientid,"SP",20);
	storedata(%clientid,"CharState",2);
	storedata(%clientid,"LCK",8);
	gamebase::setposition(%clientid,%pos);
	gamebase::setrotation(%clientid,"0 -0 -0.450602");

	for(%i = 1; %i <= getNumSkills(); %i++)
	{
		%s = GetSkillMultiplier(%clientId, %i);
		$PlayerSkill[%clientId, %i] = %s;
	}
}

function Carling::newChar(%clientid)
{
	storedata(%clientid,"CharState",1);
	storedata(%clientid,"GROUP","Human");
	Game::playerSpawn(%clientId, false);

	for(%i = 1; %i <= getNumSkills(%clientid); %i++)
	{
		$PlayerSkill[%clientId, %i] = 0;
	}

	centerprint(%clientId, "<f1> Server powered by the RPG MOD version " @ $rpgver @ "<f0>\n\n" @ $loginMsg, 15);
}
