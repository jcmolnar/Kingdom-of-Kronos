$nodroplist = "EnergyBall,";

function door::open(%clientid,%target)
{
	if(GameBase::getLOSinfo(Client::getOwnedObject(%ClientId), 10))
	{
		%target = $los::object;
		%obj = getObjectType(%target);
		%oname = object::getname(%target);
		%dir = getword(%oname,1);

		if(getWord(%oname,0) == "Door" && %target.open != true)
		{
			if(fetchdata(%clientid,"Kills") <= 0)
			{
				Client::sendmessage(%clientid,0,"You open the door.");

				if(%dir == "L" || %dir == "R")
					door::slide(%target,%dir);
				else if(%dir == "T")
					door::tele(%clientid,%target);
				else if(%dir == "S")
					Client::EnterWorld(%clientid,%target);

				PlaySound(SoundDoorOpen, gamebase::getposition(%clientid));

				if(%target.part)
				{
					PlaySound(SoundDoorOpen, gamebase::getposition(%clientid));
					if(%dir == "R")
						door::slide(%target.part,"L");
					else if(%dir == "L")
						door::slide(%target.part,"R");
				}
			}
			else
				Client::sendmessage(%clientid,1,"Its Locked.");
		}
	}
}

function door::slide(%obj,%dir)
{
	%pos = GameBase::getPosition(%obj);
	%x = getWord(%pos,0);	%y = getWord(%pos,1);	%z = getWord(%pos,2);

	if(%dir == "L")
		%xpos = (%x-3)@" "@%y@" "@%z;
	else
		%xpos = (%x+3)@" "@%y@" "@%z;

	%obj.open = true;
	%time = 0.1;

	if(%dir == "L")
		%f = 30;
	else
		%f = 25;

	for(%i=0;%i<=%f;%i++)
	{
		if(%dir == "L")
			%x = %x - 0.1;
		else
			%x = %x + 0.1;

		%npos = %x@" "@%y@" "@%z;
		schedule("GameBase::setPosition(\"" @ %obj @ "\" , \"" @ %npos @ "\");", %time, %obj);
		%time = %time + 0.1;
	}
	schedule("GameBase::setPosition(\"" @ %obj @ "\" , \"" @ %xpos @ "\");", %time, %obj);
	%time = %time + 3;
	%x = getword(%xpos,0);

	if(%dir == "L")
		%f = 25;
	else
		%f = 31;

	for(%i=0;%i<=%f;%i++)
	{
		if(%dir == "L")
			%x = %x + 0.1;
		else
			%x = %x - 0.1;
		%npos = %x@" "@%y@" "@%z;
		schedule("GameBase::setPosition(\"" @ %obj @ "\" , \"" @ %npos @ "\");", %time, %obj);
		%time = %time + 0.1;
	}
	schedule("GameBase::setPosition(\"" @ %obj @ "\" , \"" @ %obj.pos @ "\");", %time, %obj);
	schedule(%obj @ ".open = \"\";", %time);
}

function EmptyZoneCheck() 
{
	%mon = GetBotIdList();

	for(%i=0;getword(%mon,%i)!=-1;%i++)
	{
		%id = getword(%mon,%i);

		if(!$npc[%id])
		{
			%z = fetchData(%id, "zone");
			%plist = Zone::getPlayerList(%z, 2);

			if(%plist == "")
				felloffmap(%id);
		}
	}
}

function MenuSpells(%clientid)
{
	Client::buildMenu(%clientId, "Spell Options", "spellopt", true);

	%num = 0;
	Client::addMenuItem(%clientId, %num++@"Thorn", "Thorn");
	if(SkillCanUse(%clientId, "FireBall")) Client::addMenuItem(%clientId, %num++@"Fire Ball", "Fireball");
	if(SkillCanUse(%clientId, "IceSpike")) Client::addMenuItem(%clientId, %num++@"Ice Spike", "IceSpike");
	if(SkillCanUse(%clientId, "IceStorm")) Client::addMenuItem(%clientId, %num++@"Ice Storm", "IceStorm");
	if(SkillCanUse(%clientId, "Melt")) Client::addMenuItem(%clientId, %num++@"Melt", "Melt");
	if(SkillCanUse(%clientId, "DimensionRift")) Client::addMenuItem(%clientId, %num++@"DimensionRift", "DimensionRift");
	if(SkillCanUse(%clientId, "Doom")) Client::addMenuItem(%clientid, %num++@"Doom", "Doom");
}

function processMenuspellopt(%clientId, %option)
{
	%opt = getWord(%option, 0);
	%cl = getWord(%option, 1);

	if(%opt == "Cloud")
	{
		return;
	}

	if(SkillCanUse(%clientId, %opt) || %opt == "Thorn")
	{
		$spell[%clientid] = %opt;
		Client::sendmessage(%clientid,0,"You are now ready to cast "@%opt@".");
	}
	else
		Client::sendmessage(%clientid,0,"You can't cast "@%opt@" because you lack the necessary skills.");

	return;
}

function lightspot::add(%pos,%type)
{
	%obj = newObject("","Item","LightSpot"@%type,1,false);
	%obj.delta = %delta;
	addToSet("MissionCleanup", %obj);
	gamebase::setposition(%obj,%pos);
	return %obj;
}

$SkillRestriction[doom] = $SkillOffensiveCasting @ " 1000";
$Spell::keyword[37] = "doom";
$Spell::index[doom] = 37;
$Spell::name[37] = "Doom";
$Spell::description[37] = "All enemies in the cast radius are overcome by a feeling of doom.";
$Spell::delay[37] = 1.5;
$Spell::recoveryTime[37] = 30.0;
$Spell::damageValue[37] = 60;
$Spell::LOSrange[37] = 80;
$Spell::manaCost[37] = 21;
$Spell::startSound[37] = DeActivateWA;
$Spell::endSound[37] = ActivateAR;
$Spell::groupListCheck[37] = False;
$Spell::refVal[37] = -60;
$Spell::graceDistance[37] = 2;
$SkillType[doom] = $SkillOffensiveCasting;

function Doom::AreaCheck(%obj,%num)
{
	%pos = GameBase::getPosition(%obj);
	%ids = GetEveryoneIdList();

	for(%i=0;getword(%ids,%i) != -1;%i++)
	{
		%id = getWord(%ids,%i);
		%group = fetchdata(%id,"GROUP");

		if(%group != "Ghost" && %group != "Zombie" && %group != "Skeleton" && %group != "Demon")
		{
			%pos2 = GameBase::getPosition(%id);
			%dist = Vector::getDistance(%pos,%pos2);

			if(%dist <= 40)
			{
				%hp = fetchdata(%id,"HP");
				%value = %hp/1000;
				refreshHP(%id, %value);
				Player::setDamageFlash(%id,1);

				$doom[%id] = 1;
				RefreshWeight(%id);
				schedule("$doom["@%id@"] = \"\";",1,%id);
				schedule("RefreshWeight("@%id@");",(20-%num),%id);
			}
		}
	}
}

function Ai::DropLoot(%clientid,%loot)
{
	%pos = GameBase::getPosition(%clientid);
	%pos = Vector::add(%pos,"0 0 0.5");
	%player = Client::getOwnedObject(%clientId);

	for(%i=0;getword(%loot,%i) != -1;%i++)
	{
		%item = getword(%loot,%i);
		%amnt = getword(%loot,%i++);

		if(%item != "COINS")
			AI::ItemDrop(%player,%pos,%item,%amnt);
	}
}

function AI::ItemDrop(%player,%pos,%item,%amnt)
{
	%obj = newObject("","Item",%item,1,false);
	%obj.delta = %amnt;
	schedule("Item::Pop(" @ %obj @ ");", (9 + (getrandom()*2)), %obj);
	addToSet("MissionCleanup", %obj);
	GameBase::throw(%obj, %player, 4, true);
}

function EnergyBallImage::onFire(%player, %slot)
{
	%clientId = Player::getClient(%player);
	if($spell[%clientid] != "" && $spell[%clientid] != -1)
	{
		if(%ClientId.sleepMode == 1 || %ClientId.sleepMode == 2)
			return;

		if($spell[%clientid] == "Doom")
		{
			%s = $Spell::index[$spell[%clientid]];
			%delay = $Spell::recoveryTime[%s];
			%mana = $Spell::manaCost[%s];

			if($spell2[%clientid] == "")
			{
				if(fetchData(%clientId, "MANA") >= %mana)
				{
					%tempManaCost = floor(%mana);
					refreshMANA(%clientId, %tempManaCost);

					$spell2[%clientid] = 1;
					schedule("EnergyBallRdyCast("@%clientid@","@%delay@");",%delay,%clientid);
	
					%obj = lightspot::add(gamebase::getposition(%clientid),2);
 	 			  	schedule("Item::Pop(" @ %obj @ ");", 20, %obj);
					%time = 0;
					for(%i = 0; %i <= 20; %i++)
					{
						schedule("Doom::AreaCheck("@%obj@","@%i@");",%time,%obj);
						%time++;
					}
				}
				else
					Client::sendMessage(%clientId, $MsgWhite, "Insufficient mana to cast this spell.");
			}
			else
				Client::sendMessage(%clientid,1,"You must wait longer before you can cast again.");
		}
		else
		{
			BeginCastSpell(%clientId, $spell[%clientid]);
		}
	}
	else
		Client::SendMessage(%clientid,0,"You must select a spell from the Spells list (tab menu)");
}

function EnergyBallRdyCast(%clientid,%delay)
{
	$spell2[%clientid] = "";
	if(%delay > 2)
		Client::sendmessage(%clientid,0,"You are ready to cast!");
}
