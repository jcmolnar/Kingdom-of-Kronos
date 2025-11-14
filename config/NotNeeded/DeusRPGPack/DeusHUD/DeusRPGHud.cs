//By : Deus_ex_Machina
//
//	RPGHUDs ver 2.561

$DeusHUDver = "ver2.561";
$DeusRPGHud::CBdelay = 2.0;
$DeusRPGHud::CB5delay = 5.0;

$DeusRPG::RPGLeft = "99%";
$DeusRPG::RPGTop = "95%";
$DeusRPG::RPGWidth = 165;
$DeusRPG::RPGHeight = 265;

$DeusRPG::SkillLeft = "100%";
$DeusRPG::SkillTop = "14%";
$DeusRPG::SkillWidth = 175;
$DeusRPG::SkillHeight = 280;

$DeusRPG::NLeft = "83%-2";
$DeusRPG::NTop = "14%";
$DeusRPG::NWidth = 20;
$DeusRPG::NHeight = 280;

$DeusRPG::TeamLeft = "24%";
$DeusRPG::TeamTop = "84%";
$DeusRPG::TeamWidth = 800;
$DeusRPG::TeamHeight = 50;

$DeusRPG::ItemLeft = "75%";
$DeusRPG::ItemTop = "2%";
$DeusRPG::ItemWidth = 190;
$DeusRPG::ItemHeight = 60;

$DeusRPG::MinerLeft = "0%";
$DeusRPG::MinerTop = "15%";
$DeusRPG::MinerWidth = 160;
$DeusRPG::MinerHeight = 200;

$DeusRPG::HPMPLeft = "2%";
$DeusRPG::HPMPTop = "4%";
$DeusRPG::HPMPWidth = 110;
$DeusRPG::HPMPHeight = 30;

$DeusRPG::AmmoLeft = "7%";
$DeusRPG::AmmoTop = "46%";

function DeusRPGHud::GetRpgData() {

	if($toggleHPMP == "") {
		$DeusRPGHud::mana = DeusRPG::FetchData("MANA");
		$DeusRPGHud::maxmana = DeusRPG::FetchData("MaxMANA");
		$DeusRPGHud::hp = DeusRPG::FetchData("HP");
		$DeusRPGHud::maxhp = DeusRPG::FetchData("MaxHP");
	}
	$DeusRPGHud::exp = DeusRPG::FetchData("EXP");
	$DeusRPGHud::coins = DeusRPG::FetchData("COINS");
	$DeusRPGHud::maxweight = DeusRPG::FetchData("MaxWeight");
	$DeusRPGHud::bank = DeusRPG::FetchData("BANK");
	$DeusRPGHud::lvl = DeusRPG::FetchData("LVL");
	$DeusRPGHud::lck = DeusRPG::FetchData("LCK");
	$DeusRPGHud::atk = DeusRPG::FetchData("ATK");
	$DeusRPGHud::def = DeusRPG::FetchData("DEF");
	$DeusRPGHud::mdef = DeusRPG::FetchData("MDEF");
	$DeusRPGHud::class = DeusRPG::FetchData("CLASS");
	$DeusRPGHud::race = DeusRPG::FetchData("RACE");
	$DeusRPGHud::RemortStep = DeusRPG::FetchData("RemortStep");
	$DeusRPGHud::LCKconsequence = DeusRPG::FetchData("LCKconsequence");
	$DeusRPGHud::MyHouse = DeusRPG::FetchData("MyHouse");
	$DeusRPGHud::RankPoints = DeusRPG::FetchData("RankPoints");
	$DeusRPGHud::SPcredits = DeusRPG::FetchData("SPcredits");
	$DeusRPGHud::isMimic = DeusRPG::FetchData("isMimic");
	$DeusRPGHud::bounty = DeusRPG::FetchData("bounty");
	$DeusRPGHud::zonedesc = DeusRPG::FetchData("zonedesc");

	$DeusRPGHud::expneeded = ((floor($DeusRPGHud::exp / 1000) + 1) * 1000) - $DeusRPGHud::exp;

	if($rfdcnt++ > 5) {
		$DeusRPGHud::weight = FixDecimals(DeusRPG::FetchData("Weight"));
		$rfdcnt = 0;
	}
}

function DeusRPGHud::GetHPMPRpgData() {
	$DeusRPGHud::mana = DeusRPG::FetchData("MANA");
	$DeusRPGHud::maxmana = DeusRPG::FetchData("MaxMANA");
	$DeusRPGHud::hp = DeusRPG::FetchData("HP");
	$DeusRPGHud::maxhp = DeusRPG::FetchData("MaxHP");
}

function DeusRPGHud::GetGroupRpgData() {
	$DeusRPGHud::tmpname = Client::getname(getManagerId()); //DeusRPG::FetchData("tmpname");
	$DeusRPGHud::group = DeusRPG::FetchData("grouplist");
}

// Skill data is broken up into:
// $DeusRPGHud::Skill[-Number-, Desc]
// $DeusRPGHud::Skill[-Number-, Amount]
//
// e.g.
// $DeusRPGHud::Skill[1, Desc] would = "Slashing"
// $DeusRPGHud::Skill[1, Amount] would = the amount their skill is

$DeusRPGHud::Skill[1, Desc] = "Slashing";
$DeusRPGHud::Skill[2, Desc] = "Piercing";
$DeusRPGHud::Skill[3, Desc] = "Bludgeoning";
$DeusRPGHud::Skill[4, Desc] = "Dodging";
$DeusRPGHud::Skill[5, Desc] = "Weight Capacity";
$DeusRPGHud::Skill[6, Desc] = "Bashing";
$DeusRPGHud::Skill[7, Desc] = "Stealing";
$DeusRPGHud::Skill[8, Desc] = "Hiding";
$DeusRPGHud::Skill[9, Desc] = "Backstabbing";
$DeusRPGHud::Skill[10, Desc] = "Offensive Casting";
$DeusRPGHud::Skill[11, Desc] = "Defensive Casting";
$DeusRPGHud::Skill[12, Desc] = "Spell Resistance";
$DeusRPGHud::Skill[13, Desc] = "Healing";
$DeusRPGHud::Skill[14, Desc] = "Archery";
$DeusRPGHud::Skill[15, Desc] = "Endurance";
$DeusRPGHud::Skill[16, Desc] = "(no longer used)";
$DeusRPGHud::Skill[17, Desc] = "Mining";
$DeusRPGHud::Skill[18, Desc] = "Speech";
$DeusRPGHud::Skill[19, Desc] = "Sense Heading";
$DeusRPGHud::Skill[20, Desc] = "Energy";
$DeusRPGHud::Skill[21, Desc] = "Haggling";
$DeusRPGHud::Skill[22, Desc] = "Neutral Casting";

$DeusRPGHud::SkillCount = 22;

function DeusRPGHud::GetSkillRpgData() {
	for(%i = 1; %i <= $DeusRPGHud::SkillCount; %i++)
		if(%i != 16) // temporary code until they fix the skill count in TRPG
		{
			$DeusRPGHud::Skill[%i, Amount] = DeusRPG::FetchData("skill " @ %i);
		}
}

function DeusRPGHud::UpdateDeusRPGhud(%hud) {
	if($FastUpdate == "")
		DeusRPGHud::GetRpgData();

	if($DeusRPGHud::class == "")
		HUD::AddTextLine(%hud, "<JC><f0>\nWeclome to RPG!\n\nDeusRPGHUDs "@$DeusHUDver@" by:\n<f1>Deus_ex_Machina\n<f0>and edited by:\n<f1>Shots<JL>\n\n\n<f0>Credits for making\nthe RPGMOD\n<f1>JeremyIrons <f2>- <f0>Tribes RPG creator and coder\n<f1>Alaric <f2>- <f0>Webmaster\n<f1>Tenabrae <f2>- <f0>3D Modeler\n<f1>Angel Kill <f2>- <f0>Prefabs and Models\n<f1>HiVoltage <f2>- <f0>3D Modeler\n<f0>And many more!");
	else {
		if ($DeusRPGHud::RemortStep == "0")
			HUD::AddTextLine(%hud, "<f1>" @ $DeusRPGHud::class @ " <f0>Level <f2>" @ $DeusRPGHud::lvl);
		else
			HUD::AddTextLine(%hud, "<f1>" @ $DeusRPGHud::class @ " <f0>Level <f2>" @ $DeusRPGHud::lvl @ " <f0>RL <f2>" @ $DeusRPGHud::RemortStep);

		if ($DeusRPGHud::isMimic == "true")
			HUD::AddTextLine(%hud, "<f0>Mimic <f1>" @ $DeusRPGHud::race);
		else
			HUD::AddTextLine(%hud, "<f1>" @ $DeusRPGHud::race);

		if($DeusRPGHud::zonedesc == "-1")
			HUD::AddTextLine(%hud, "<f0>Zone <f1>Unknown");
		else
			HUD::AddTextLine(%hud, "<f0>Zone <f1>" @ $DeusRPGHud::zonedesc);

		if($DeusRPGHud::bounty == "0")
			HUD::AddTextLine(%hud, "<f0>No Bounty");
		else
			HUD::AddTextLine(%hud, "<f0>Bounty: <f2>" @ $DeusRPGHud::bounty);

		if($DeusRPGHud::MyHouse == "")
		HUD::AddTextLine(%hud, "<f0>No House");
		else
			HUD::AddTextLine(%hud, "<f0>In <f1>" @ $DeusRPGHud::MyHouse @ "<f0>. Rank <f2>" @ $DeusRPGHud::RankPoints);

		if($DeusRPGHud::SPcredits > "0")
			HUD::AddTextLine(%hud, "<f0>SP: <f2>" @ $DeusRPGHud::SPcredits @ "\n");
		else
			HUD::AddTextLine(%hud, "<f0>No SP\n");

		if($DeusRPGHud::hp > 20000000) //um yeah...when you die your HP goes very high....not 0...
			HUD::AddTextLine(%hud, "<f2>You are dead");
		else
			HUD::AddTextLine(%hud, "<f0>HP: <f2>" @ $DeusRPGHud::hp @ "<f0> / <f2>" @ $DeusRPGHud::maxhp);

		if($DeusRPGHud::mana > 20000000 || $DeusRPGHud::mana < 1)
			HUD::AddTextLine(%hud, "<f2>Out of Mana");
		else
			HUD::AddTextLine(%hud, "<f0>MP: <f2>" @ $DeusRPGHud::mana @ "<f0> / <f2>" @ $DeusRPGHud::maxmana);

		HUD::AddTextLine(%hud, "<f0>EXP: <f2>" @ $DeusRPGHud::exp @ "<f0> / <f2>" @ $DeusRPGHud::expneeded);
		HUD::AddTextLine(%hud, "<f0>Weight: <f2>" @ $DeusRPGHud::weight @ "<f0> / <f2>" @ $DeusRPGHud::maxweight @ "\n");

		if($DeusRPGHud::coins < 1)
			HUD::AddTextLine(%hud, "<f2>No coins on you");
		else
			HUD::AddTextLine(%hud, "<f0>Coins: <f2>" @ $DeusRPGHud::coins);

		if($DeusRPGHud::bank < 1)
			HUD::AddTextLine(%hud, "<f2>No coins in Bank\n");
		else
			HUD::AddTextLine(%hud, "<f0>Bank: <f2>" @ $DeusRPGHud::bank @ "\n");

		HUD::AddTextLine(%hud, "<f0>ATK: <f2>" @ $DeusRPGHud::atk);
		HUD::AddTextLine(%hud, "<f0>DEF: <f2>" @ $DeusRPGHud::def);
		HUD::AddTextLine(%hud, "<f0>MDEF: <f2>" @ $DeusRPGHud::mdef @ "\n");

		if($DeusRPGHud::lck < 1)
			HUD::AddTextLine(%hud, "<f2>Out of LCK!");
		else
			HUD::AddTextLine(%hud, "<f0>LCK: <f2>" @ $DeusRPGHud::lck @ " <f0>set to : <f2>" @ $DeusRPGHud::LCKconsequence);

		return $DeusRPGHud::CBdelay;
	}
	return $DeusRPGHud::CBdelay;
}


//Now you can see what keys you have bind in game!
$DeusRPGPack::KeyDesc[1, Desc] = "DeusHP MP Hud toggle";
$DeusRPGPack::KeyDesc[2, Desc] = "DeusRPG Hud toggle (Main HUD)";
$DeusRPGPack::KeyDesc[3, Desc] = "DeusGroup Hud toggle";
$DeusRPGPack::KeyDesc[4, Desc] = "DeusItem Hud toggle (Shows all your potions on you)";
$DeusRPGPack::KeyDesc[5, Desc] = "DeusMiner Hud toggle";
$DeusRPGPack::KeyDesc[6, Desc] = "DeusSkill Hud toggle";
$DeusRPGPack::KeyDesc[7, Desc] = "DeusAmmo Hud toggle";
$DeusRPGPack::KeyDesc[8, Desc] = "Deus# Hud toggle (only really good for admins)";
$DeusRPGPack::KeyDesc[9, Desc] = "Pick Best Bludgeoning";
$DeusRPGPack::KeyDesc[10, Desc] = "Pick Best Piercing";
$DeusRPGPack::KeyDesc[11, Desc] = "Pick Best Slashing";
$DeusRPGPack::KeyDesc[12, Desc] = "Pick Best Archery";
$DeusRPGPack::KeyDesc[13, Desc] = "Toggle AutoRun";
$DeusRPGPack::KeyDesc[14, Desc] = "Toggle AutoFire";
$DeusRPGPack::KeyDesc[15, Desc] = "Display Training Main Menu";
$DeusRPGPack::KeyDesc[16, Desc] = "NewbieGuide: Page Up";
$DeusRPGPack::KeyDesc[17, Desc] = "NewbieGuide: Page Down";

function DeusRPGHud::UpdateDeusKeyBindhud(%hud) {
	HUD::AddTextLine(%hud, "<f2><jc>DeusRPG Pack<f0>\n-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-\n<f1>version <f2>"@$RPGPackver@"\n<f0>Done by:\n<f1>Deus_ex_Machina\n<f0>and\n<f1>Shots\n<jl>");
	HUD::AddTextLine(%hud, "<f1>E-Mail Khris142@yahoo.com\n<f0>Mail me or post on forums of any problams or ideas that\nyou would like to see fixed or changed.\n");
	HUD::AddTextLine(%hud, "<f2>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-");

	for(%i = 1; %i <= $DeusRPGPackPrefs::DeusRPG::NumBinds; %i++)
		if($DeusRPGPackPrefs::DeusRPG::KeyBind[%i] == "**")
			$DeusRPG::blank = true;
		else
			HUD::AddTextLine(%hud, "<f0>Key "@$DeusRPGPackPrefs::DeusRPG::KeyBind[%i]@" <f2>- <f1>"@$DeusRPGPack::KeyDesc[%i, Desc]);
	HUD::AddTextLine(%hud, "<f2>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-");
}
function DeusRPGHud::UpdateDeusKeyBind1hud(%hud) {
	HUD::AddTextLine(%hud, "<jc><f0>QuickCast KeyBinds");
	HUD::AddTextLine(%hud, "<f2>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<>-<jl>");
	for(%i = 1; %i <= $QuickCast::NumBinds; %i++)
		if($DeusRPGPackPrefs::QuickCastSpell::Spell[%i] == "")
			$DeusRPG::blank = true;
		else
			HUD::AddTextLine(%hud, "<f0>Key <f1>"@$DeusRPGPackPrefs::QuickCast::KeyBind[%i]@" <f0>is Spell <f1>"@$DeusRPGPackPrefs::QuickCastSpell::Spell[%i]);
}

function DeusRPGHud::toggleDeusKeyBindhud() { HUD::ToggleDisplay(DeusKeyBindhud); HUD::ToggleDisplay(DeusKeyBind1hud); }  //Use in DeusChatbind.cs

// Do not edit!
if($DeusRPGPackPrefs::FirstTime == "")
	$DeusRPGPackPrefs::FirstTime = true;
else
	$DeusRPGPackPrefs::FirstTime = false;

//You should not edit this part. This is where is checks for a pos and if there isn't one it gives a default pos and if you moved it in game and quit it loads your new pos =]
function DeusRPGHud::CheckPos() {
	if($Hud::prefs::last::DeusRPGhud == "" || $DeusRPGPackPrefs::FirstTime == true)
		$Hud::prefs::last::DeusRPGhud = $DeusRPG::RPGLeft@" "@$DeusRPG::RPGTop@" "@$DeusRPG::RPGWidth@" "@$DeusRPG::RPGHeight;

	if($Hud::prefs::last::DeusHPMPhud == "" || $DeusRPGPackPrefs::FirstTime == true)
		$Hud::prefs::last::DeusHPMPhud = $DeusRPG::HPMPLeft@" "@$DeusRPG::HPMPTop@" "@$DeusRPG::HPMPWidth@" "@$DeusRPG::HPMPHeight;

	if($Hud::prefs::last::DeusITEMhud == "" || $DeusRPGPackPrefs::FirstTime == true)
		$Hud::prefs::last::DeusITEMhud = $DeusRPG::ItemLeft@" "@$DeusRPG::ItemTop@" "@$DeusRPG::ItemWidth@" "@$DeusRPG::ItemHeight;

	if($Hud::prefs::last::DeusMINERhud == "" || $DeusRPGPackPrefs::FirstTime == true)
		$Hud::prefs::last::DeusMINERhud = $DeusRPG::MinerLeft@" "@$DeusRPG::MinerTop@" "@$DeusRPG::MinerWidth@" "@$DeusRPG::MinerHeight;

	if($Hud::prefs::last::DeusTEAMhud == "" || $DeusRPGPackPrefs::FirstTime == true)
		$Hud::prefs::last::DeusTEAMhud = $DeusRPG::TeamLeft@" "@$DeusRPG::TeamTop@" "@$DeusRPG::TeamWidth@" "@$DeusRPG::TeamHeight;

	if($Hud::prefs::last::DeusSKILLhud == "" || $DeusRPGPackPrefs::FirstTime == true)
		$Hud::prefs::last::DeusSKILLhud = $DeusRPG::SkillLeft@" "@$DeusRPG::SkillTop@" "@$DeusRPG::SkillWidth@" "@$DeusRPG::SkillHeight;

	if($Hud::prefs::last::DeusAmmohud == "" || $DeusRPGPackPrefs::FirstTime == true) {
		$Hud::prefs::last::DeusAmmohud = $DeusRPG::AmmoLeft@" "@$DeusRPG::AmmoTop@" "@$DeusRPG::AmmoWidth@" "@$DeusRPG::AmmoHeight;
	}
	if($Hud::prefs::last::DeusNhud == "" || $DeusRPGPackPrefs::FirstTime == true)
		$Hud::prefs::last::DeusNhud = $DeusRPG::NLeft@" "@$DeusRPG::NTop@" "@$DeusRPG::NWidth@" "@$DeusRPG::NHeight;

	if($DeusRPGPackPrefs::FirstTime) {
		echo("DeusRPGHUD: First time using DeusRPGHud Giving default position to huds!");
		$DeusRPGPackPrefs::FirstTime = false;
	}
	if($debug == true) echo("DeusRPG pos \'"@$Hud::prefs::last::DeusRPGhudPosition@"\'");
	if($debug == true) echo("DeusHPMP pos \'"@$Hud::prefs::last::DeusHPMPhudPosition@"\'");
	if($debug == true) echo("DeusITEM pos \'"@$Hud::prefs::last::DeusITEMhudPosition@"\'");
	if($debug == true) echo("DeusMINER pos \'"@$Hud::prefs::last::DeusMINERhudPosition@"\'");
	if($debug == true) echo("DeusTEAM pos \'"@$Hud::prefs::last::DeusTEAMhudPosition@"\'");
	if($debug == true) echo("DeusSKILL pos \'"@$Hud::prefs::last::DeusSKILLhudPosition@"\'");
	if($debug == true) echo("DeusAMMO pos \'"@$Hud::prefs::last::DeusAmmohudPosition@"\'");
	if($debug == true) echo("DeusN pos \'"@$Hud::prefs::last::DeusNhudPosition@"\'");
}
DeusRPGHud::CheckPos();

HUD::New(DeusRPGhud, DeusRPGHud::UpdateDeusRPGhud, $Hud::prefs::last::DeusRPGhud);

HUD::New(DeusKeyBindhud, DeusRPGHud::UpdateDeusKeyBindhud, "100 55% 400 400");
HUD::New(DeusKeyBind1hud, DeusRPGHud::UpdateDeusKeyBind1hud, "500 55% 300 400");


HUD::Display(DeusRPGhud);

// HUD::Display(DeusHPMPhud);
// HUD::Display(DeusITEMhud);
// HUD::Display(DeusMINERhud);
// HUD::Display(DeusTEAMhud);
// HUD::Display(DeusSKILLhud);
// HUD::Display(DeusAmmohud);

$DeusRPGHUD::ScriptCheck = 0;
exec("DeusRPGPack\\DeusHUD\\HPMP.cs");
exec("DeusRPGPack\\DeusHUD\\ITEMS.cs");
exec("DeusRPGPack\\DeusHUD\\MINER.cs");
exec("DeusRPGPack\\DeusHUD\\TEAM.cs");
exec("DeusRPGPack\\DeusHUD\\SKILL.cs");
exec("DeusRPGPack\\DeusHUD\\AMMO.cs");

if($DeusRPGHUD::ScriptCheck == 6)
	echo("DeusRPGHUDs loaded!");
else {
	$error = true;
	$DeusRPG::ScriptCheck--;
	echo("DeusRPGHUDs loaded but with errors!");
}

function DeusRPGPack::func1() { HUD::ToggleDisplay(DeusHPMPhud); if($toggleHPMP == "") $toggleHPMP = true; else $toggleHPMP = ""; }
function DeusRPGPack::func2() { HUD::ToggleDisplay(DeusRPGhud); }
function DeusRPGPack::func3() { HUD::ToggleDisplay(DeusTEAMhud); }
function DeusRPGPack::func4() { HUD::ToggleDisplay(DeusITEMhud); }
function DeusRPGPack::func5() { HUD::ToggleDisplay(DeusMINERhud); }
function DeusRPGPack::func6() { HUD::ToggleDisplay(DeusSKILLhud); }
function DeusRPGPack::func7() { $DeusRPG::AmmoHeight = 50; HUD::ToggleDisplay(DeusAmmohud); }
function DeusRPGPack::func8() { HUD::ToggleDisplay(DeusNhud); }

Event::Attach(eventExit, HUD::DeleteAll, hud);

$DeusRPG::ScriptCheck++;