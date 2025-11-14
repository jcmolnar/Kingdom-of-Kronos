//Used to strip off the first letter of a heading, making the heading display menu more user friendly.
function Help::getDisplay(%heading)
{
	return String::getSubStr(%heading, 1, (String::Len(%heading) - 1));
}

//Generates the list of headings for use in the help menu; Called only on execution.
function Help::Headings()
{
	$Help::Headings = " ";
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%item = getItemData(%i);
		%heading = %item.heading;

		if(%heading != "aEquipped" && %item.showInventory)
			if(String::findSubStr($Help::Headings, " " @ %heading @ " ") == -1)
				$Help::Headings = $Help::Headings @ %heading @ " ";
	}
}

//Used to generate a list of items based on the provided heading; i.e. list all weapons, all armors, or all helmets.
function Help::Matches(%category)
{
	%string = " ";
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%item = getItemData(%i);
		%heading = %item.heading;
		
		if(%heading != "aEquipped" && %item.showInventory)
			if(%heading == %category)
				%string = %string @ %i @ " ";
	}

	return %string;
}

//Used to generate a list of spells based on the provided index
function Help::Spells(%index)
{
	%string = " ";
	%max = getNumSpells();
	for(%i = 0; %i <= %max; %i++)
		if($SkillType[$Spell::keyword[%i]] == %index)
			%string = %string @ %i @ " ";

	return %string;
}

function Help::mainMenu(%clientId)
{
	Client::buildMenu(%clientId, ".:(Help Menu):.", "helpMainMenu", true);
	%clientId.bulkNum = "";
	
	Client::addMenuItem(%clientId, %cnt++ @ "Equipment", "equip");
	Client::addMenuItem(%clientId, %cnt++ @ "Spells", "spell");
	Client::addMenuItem(%clientId, %cnt++ @ "Skills", "skill");
	Client::addMenuItem(%clientId, %cnt++ @ "Help Commands", "commands");
	Client::addMenuItem(%clientId, "xFinished", "done");
	return;
}

function processMenuhelpMainMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);

	if(%o != "done")
	{
		if(%o == "equip")
			Help::itemMenu(%clientId, 1);
		else if(%o == "spell")
			Help::spellMenu(%clientId);
		else if(%o == "skill")
			Help::skillListMenu(%clientId, 1);
		else if(%o == "commands")
			Help::commandsMainMenu(%clientId);
	}

	return;
}

function Help::spellDisplay(%index, %spell)
{
	%desc = $SkillDesc[$SkillType[%spell]];
	%damage = $Spell::damageValue[%index];
	if(!%damage)
		%damage = "None";

	%msg = "<f1>";
	%msg = %msg @ $Spell::name[%index] @ " (#cast " @ %spell @ ")\n\n";
	%msg = %msg @ "Casting Delay: " @ $Spell::delay[%index] @ "\n";
	%msg = %msg @ "Recovery Time: " @ $Spell::recoveryTime[%index] @ "\n";
	%msg = %msg @ "Damage Value: " @ %damage @ "\n";
	%msg = %msg @ "Mana Cost: " @ $Spell::manaCost[%index] @ "\n";
	%msg = %msg @ "Spell Type: " @ %desc @ "\n";
	%msg = %msg @ "Requirement: " @ getWord($SkillRestriction[%spell], 1) @ " " @ %desc @ "\n\n";
	%msg = %msg @ "<f0>Description: " @ $Spell::description[%index] @ "\n";
	
	return %msg;
}

function Help::itemMenu(%clientId, %page)
{	
	Client::buildMenu(%clientId, ".:(Item Categories):.", "helpItemMenu", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nf = $Help::Headings;
	%ns = CountObjInList(%nf);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%category = getword(%nf, %x);
		Client::addMenuItem(%clientId, %cnt++ @ Help::getDisplay(%category), %category @ " " @ %page @ " " @ %category);
		%x++;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %category);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page+1 @ " " @ %category);
	}
	else if(%page == %np+1 || (%ub == %ns && (%ns % %l) == 0))
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %category);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page+1 @ " " @ %category);
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %category);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %category);
	}

	return;
}

function processMenuhelpItemMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%c = GetWord(%opt, 2);
	
	if(%o != "page" && %o != "back")
	{
		Help::listMenu(%clientId, %p, %c);
		return;
	}

	if(%o != "back")
		Help::itemMenu(%clientId, %p);
	else
		Help::mainMenu(%clientId);

	return;
}

function Help::listMenu(%clientId, %page, %cat)
{	
	Client::buildMenu(%clientId, ".:(" @ Help::getDisplay(%cat) @ "):.", "helpListMenu", true);
	%clientId.bulkNum = 1;

	%l = 6;
	%nf = Help::Matches(%cat);
	%ns = CountObjInList(%nf);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%item = getItemData(getword(%nf, %x));
		Client::addMenuItem(%clientId, %cnt++ @ %item.description, "info " @ %page @ " " @ %item @ " " @ %cat);
		%x++;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %item @ " " @ %cat);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page @ " " @ %item @ " " @ %cat);
	}
	else if(%page == %np+1 || (%ub == %ns && (%ns % %l) == 0))
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %item @ " " @ %cat);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page @ " " @ %item @ " " @ %cat);
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %item @ " " @ %cat);
		Client::addMenuItem(%clientId, "p<< Last", "page " @ %page-1 @ " " @ %item @ " " @ %cat);
	}

	return;
}

function processMenuhelpListMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%i = GetWord(%opt, 2);
	%c = GetWord(%opt, 3);
	
	if(%o != "back")
	{
		if(%o != "page")
		{
			%msg = WhatIs(%i);
			centerprint(%clientId, %msg, floor(String::len(%msg) / 10));
		}

		Help::listMenu(%clientId, %p, %c);
	}
	else
		Help::itemMenu(%clientId, 1);

	return;
}

function Help::spellMenu(%clientId)
{
	Client::buildMenu(%clientId, ".:(Spell Types):.", "helpSpellMenu", true);
	%clientId.bulkNum = "";
	
	Client::addMenuItem(%clientId, %cnt++ @ $SkillDesc[$SkillOffensiveCasting], $SkillOffensiveCasting);
	Client::addMenuItem(%clientId, %cnt++ @ $SkillDesc[$SkillDefensiveCasting], $SkillDefensiveCasting);
	Client::addMenuItem(%clientId, %cnt++ @ $SkillDesc[$SkillNeutralCasting], $SkillNeutralCasting);
	Client::addMenuItem(%clientId, "xBack", "back");
	return;
}

function processMenuhelpSpellMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);

	if(%o != "back")
	{
		Help::spellListMenu(%clientId, 1, %o);
	}
	else
		Help::mainMenu(%clientId);

	return;
}

function Help::spellListMenu(%clientId, %page, %index)
{	
	Client::buildMenu(%clientId, ".:(" @ $SkillDesc[%index] @ "):.", "spellListMenu", true);

	%l = 6;
	%nf = Help::Spells(%index);
	%ns = CountObjInList(%nf);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;	
		
	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%spell = getword(%nf, %x);
		Client::addMenuItem(%clientId, %cnt++ @ $Spell::name[%spell], "info " @ %page @ " " @ $Spell::keyword[%spell] @ " " @ %index);
		%x++;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ $Spell::keyword[%spell] @ " " @ %index);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page @ " " @ $Spell::keyword[%spell] @ " " @ %index);
	}
	else if(%page == %np+1 || (%ub == %ns && (%ns % %l) == 0))
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ $Spell::keyword[%spell] @ " " @ %index);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page @ " " @ $Spell::keyword[%spell] @ " " @ %index);
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ $Spell::keyword[%spell] @ " " @ %index);
		Client::addMenuItem(%clientId, "p<< Last", "page " @ %page-1 @ " " @ $Spell::keyword[%spell] @ " " @ %index);
	}

	return;
}

function processMenuspellListMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%s = GetWord(%opt, 2);
	%i = GetWord(%opt, 3);
	
	if(%o != "back")
	{
		if(%o != "page")
		{
			%msg = Help::spellDisplay($Spell::index[%s], %s);
			centerprint(%clientId, %msg, floor(String::len(%msg) / 10));
		}

		Help::spellListMenu(%clientId, %p, %i);
	}
	else
		Help::spellMenu(%clientId, 1);

	return;
}
function Help::skillListMenu(%clientId, %page)
{	
	Client::buildMenu(%clientId, ".:(Skills):.", "skillListMenu", true);

	%l = 6;
	%nf = "";
	%ns = getNumSkills();
	for(%i = 1; %i <= %ns; %i++)
		%nf = %nf @ %i @ " ";
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%skill = getword(%nf, %x);
		Client::addMenuItem(%clientId, %cnt++ @ $SkillDesc[%skill], "info " @ %page @ " " @ %skill);
		%x++;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %skill);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page @ " " @ %skill);
	}
	else if(%page == %np+1 || (%ub == %ns && (%ns % %l) == 0))
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %skill);
		Client::addMenuItem(%clientId, "xBack", "back " @ %page @ " " @ %skill);
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %skill);
		Client::addMenuItem(%clientId, "p<< Last", "page " @ %page-1 @ " " @ %skill);
	}

	return;
}

function processMenuskillListMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%i = GetWord(%opt, 2);
	
	if(%o != "back")
	{
		if(%o != "page")
		{
			%msg = "<f1>";
			%msg = %msg @ $SkillDesc[%i] @ "\n\n<f0>";
			%msg = %msg @ $SkillInfo[%i];

			centerprint(%clientId, %msg, floor(String::len(%msg) / 10));
		}

		Help::skillListMenu(%clientId, %p);
	}
	else
		Help::mainMenu(%clientId);

	return;
}

//Call the function so the headings exist.
Help::Headings();

// ============================================
// Help Commands Menu System
// ============================================

function Help::commandsMainMenu(%clientId)
{
	Client::buildMenu(%clientId, ".:(Help Commands):.", "helpCommandsMain", true);
	%clientId.bulkNum = "";
	
	Client::addMenuItem(%clientId, "Armor", "armor");
	Client::addMenuItem(%clientId, "Weapons", "weapons");
	Client::addMenuItem(%clientId, "Spells", "spells");
	Client::addMenuItem(%clientId, "Zones", "zones");
	Client::addMenuItem(%clientId, "xBack", "back");
	return;
}

function processMenuhelpCommandsMain(%clientId, %opt)
{
	%o = GetWord(%opt, 0);

	if(%o == "armor")
		Help::showArmorCommand(%clientId);
	else if(%o == "weapons")
		Help::weaponsSubMenu(%clientId);
	else if(%o == "spells")
		Help::spellsSubMenu(%clientId);
	else if(%o == "zones")
		Help::showZones(%clientId);
	else if(%o == "back")
		Help::mainMenu(%clientId);

	return;
}

// Armor Command Display
function Help::showArmorCommand(%clientId)
{
	Client::sendMessage(%clientId, $MsgRed, "ARMOR LIST");
	Client::sendMessage(%clientId, $MsgWhite, "RatSkinShirt, StuddedLeatherSuit, ToughHideSuit, IronScaleMail");
	Client::sendMessage(%clientId, $MsgWhite, "SteelScaleMail, SteelBrigandineMail, GoldenBrigandineMail, GoldenChainMail");
	Client::sendMessage(%clientId, $MsgWhite, "CrystalChainMail, CrystalRingMail, CrystalBandedMail, CrystalSplintMail");
	Client::sendMessage(%clientId, $MsgWhite, "TungstenSplintMail, TungstenPlateMail, DiamondFieldPlate, DiamondFullPlate");
	Client::sendMessage(%clientId, $MsgWhite, "BlackDiamondFullPlate, RedDiamondPlate, WhiteDiamondPlate");
	Help::commandsMainMenu(%clientId);
}

// Weapons Sub Menu
function Help::weaponsSubMenu(%clientId)
{
	Client::buildMenu(%clientId, ".:(Weapon Types):.", "helpWeaponsSubMenu", true);
	%clientId.bulkNum = "";
	
	Client::addMenuItem(%clientId, "Slashing", "slash");
	Client::addMenuItem(%clientId, "Piercing", "pierce");
	Client::addMenuItem(%clientId, "Bludgeon", "bludge");
	Client::addMenuItem(%clientId, "xBack", "back");
	return;
}

function processMenuhelpWeaponsSubMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);

	if(%o == "slash")
		Help::showSlashWeapons(%clientId);
	else if(%o == "pierce")
		Help::showPierceWeapons(%clientId);
	else if(%o == "bludge")
		Help::showBludgeWeapons(%clientId);
	else if(%o == "back")
		Help::commandsMainMenu(%clientId);

	return;
}

function Help::showSlashWeapons(%clientId)
{
	Client::sendMessage(%clientId, $MsgRed, "SLASHING WEAPON LIST");
	Client::sendMessage(%clientId, $MsgWhite, "Hatchet, RustyIronBlade, SharpIronBlade, IronBroadSword, SteelBroadSword");
	Client::sendMessage(%clientId, $MsgWhite, "SteelLongSword, GoldenLongSword, GoldenBastardSword, CrystalBastardSword");
	Client::sendMessage(%clientId, $MsgWhite, "TemperedCrystalBastardSword, CrystalClaymore, DiamondClaymore, DiamondLegendSword");
	Client::sendMessage(%clientId, $MsgWhite, "BlackDiamondDreamSword, BlackDiamondAtomSplitter, TerminusEst");
	Help::weaponsSubMenu(%clientId);
}

function Help::showPierceWeapons(%clientId)
{
	Client::sendMessage(%clientId, $MsgRed, "PIERCING WEAPON LIST");
	Client::sendMessage(%clientId, $MsgWhite, "Knife, ButterKnife, LongKnife, IronSpear, SteelSpear, SteelPike");
	Client::sendMessage(%clientId, $MsgWhite, "GoldenPike, CrystalPike, CrystalTrident, TemperedCrystalTrident");
	Client::sendMessage(%clientId, $MsgWhite, "DiamondTrident, DiamondDeathSpear, DiamondLegendSpear, BlackDiamondDreamSpear");
	Client::sendMessage(%clientId, $MsgWhite, "BlackDiamondAtomPiercer");
	Help::weaponsSubMenu(%clientId);
}

function Help::showBludgeWeapons(%clientId)
{
	Client::sendMessage(%clientId, $MsgRed, "BLUDGEON WEAPON LIST");
	Client::sendMessage(%clientId, $MsgWhite, "Club, CrackedStick, IronStick, IronMace, SteelMace, SteelHammer");
	Client::sendMessage(%clientId, $MsgWhite, "SteelWarHammer, GoldenWarHammer, GoldenDivineMace, DiamondDivineMace");
	Client::sendMessage(%clientId, $MsgWhite, "DiamondBrainSpiller, DiamondLegendMace, BlackDiamondDreamMace, BlackDiamondStomSmasher");
	Help::weaponsSubMenu(%clientId);
}

// Spells Sub Menu
function Help::spellsSubMenu(%clientId)
{
	Client::buildMenu(%clientId, ".:(Spell Types):.", "helpSpellsSubMenu", true);
	%clientId.bulkNum = "";
	
	Client::addMenuItem(%clientId, "Offensive Spells", "offensive");
	Client::addMenuItem(%clientId, "Defensive Spells", "defensive");
	Client::addMenuItem(%clientId, "Neutral Spells", "neutral");
	Client::addMenuItem(%clientId, "xBack", "back");
	return;
}

function processMenuhelpSpellsSubMenu(%clientId, %opt)
{
	%o = GetWord(%opt, 0);

	if(%o == "offensive")
		Help::showOffensiveSpells(%clientId);
	else if(%o == "defensive")
		Help::showDefensiveSpells(%clientId);
	else if(%o == "neutral")
		Help::showNeutralSpells(%clientId);
	else if(%o == "back")
		Help::commandsMainMenu(%clientId);

	return;
}

function Help::showOffensiveSpells(%clientId)
{
	%msg = "<f1>OFFENSIVE SPELL LIST<f0>\n";
	%msg = %msg @ "thorn, fireball, firebomb, icespike\n";
	%msg = %msg @ "boom, icestorm, ironfist, cloud\n";
	%msg = %msg @ "melt, powercloud, hellstorm, beam\n";
	%msg = %msg @ "bullet, freezerburn, dimensionrift\n";
	%msg = %msg @ "nuke, tornado, apocalypse\n";
	%msg = %msg @ "<f2>Advanced:<f0> ionblast, shredder\n";
	%msg = %msg @ "Terminate, Snipe";
	
	centerprint(%clientId, %msg, floor(String::len(%msg) / 10));
	Help::spellsSubMenu(%clientId);
}

function Help::showDefensiveSpells(%clientId)
{
	%msg = "<f1>DEFENSIVE SPELL LIST<f0>\n";
	%msg = %msg @ "heal, shield\n";
	%msg = %msg @ "advheal1-6, advshield1-6\n";
	%msg = %msg @ "healplus 1-6, shieldplus 1-6\n";
	%msg = %msg @ "massshield, massheal\n";
	%msg = %msg @ "fullheal, massfullheal\n";
	%msg = %msg @ "godlyshield, godlyheal";
	
	centerprint(%clientId, %msg, floor(String::len(%msg) / 10));
	Help::spellsSubMenu(%clientId);
}

function Help::showNeutralSpells(%clientId)
{
	%msg = "<f1>NEUTRAL SPELL LIST<f0>\n";
	%msg = %msg @ "stop, airfist, teleport, lightstep\n";
	%msg = %msg @ "transport, boost, advtransport\n";
	%msg = %msg @ "advshove, heavystep, remort\n";
	%msg = %msg @ "mimic, masstransport\n";
	%msg = %msg @ "airblast, airwarp";
	
	centerprint(%clientId, %msg, floor(String::len(%msg) / 10));
	Help::spellsSubMenu(%clientId);
}

// Zones Display
function Help::showZones(%clientId)
{
	Client::sendMessage(%clientId, $MsgRed, "ZONE LIST");
	
	// Build lists for each zone type
	%protectedMsg = "";
	%dungeonMsg = "";
	%freeforallMsg = "";
	%protectedCount = 0;
	%dungeonCount = 0;
	%freeforallCount = 0;
	
	for(%i = 1; %i <= $numZones; %i++)
	{
		%zoneName = $Zone::Desc[%i];
		%zoneType = $Zone::Type[%i];
		
		if(%zoneName != "")
		{
			if(%zoneType == "PROTECTED")
			{
				if(%protectedMsg != "")
					%protectedMsg = %protectedMsg @ ", ";
				%protectedMsg = %protectedMsg @ %zoneName;
				%protectedCount++;
			}
			else if(%zoneType == "DUNGEON")
			{
				if(%dungeonMsg != "")
					%dungeonMsg = %dungeonMsg @ ", ";
				%dungeonMsg = %dungeonMsg @ %zoneName;
				%dungeonCount++;
			}
			else if(%zoneType == "FREEFORALL")
			{
				if(%freeforallMsg != "")
					%freeforallMsg = %freeforallMsg @ ", ";
				%freeforallMsg = %freeforallMsg @ %zoneName;
				%freeforallCount++;
			}
		}
	}
	
	// Display PROTECTED zones in green
	if(%protectedCount > 0)
	{
		Client::sendMessage(%clientId, $MsgWhite, "PROTECTED:");
		Client::sendMessage(%clientId, $MsgGreen, %protectedMsg);
	}
	
	// Display DUNGEON zones in red
	if(%dungeonCount > 0)
	{
		Client::sendMessage(%clientId, $MsgWhite, "DUNGEON:");
		Client::sendMessage(%clientId, $MsgRed, %dungeonMsg);
	}
	
	// Display FREEFORALL zones in white
	if(%freeforallCount > 0)
	{
		Client::sendMessage(%clientId, $MsgWhite, "FREEFORALL:");
		Client::sendMessage(%clientId, $MsgWhite, %freeforallMsg);
	}
	
	if(%protectedCount == 0 && %dungeonCount == 0 && %freeforallCount == 0)
		Client::sendMessage(%clientId, $MsgWhite, "No zones found.");
	
	Help::commandsMainMenu(%clientId);
}