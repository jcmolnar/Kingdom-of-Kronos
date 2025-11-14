//By: Deus_ex_Machina
//
//	Skill Training ver 2.6

$DeusSkillTrainingver = "ver2.6";
function DeusRPG::Stopalltraining() {
	Stop::Speech();
	DeusRPG::Stop::Track();
	Stop::Track();
	Stop::Potion();
	DeusRPG::StopWeightTraining();
	DeusRPG::Stop::Hide();
	DeusRPG::Stop::Bash();
	Client::centerPrint("<jc><f0>All Misc Training <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
function DeusRPG::StopSTEALtraining() {
	Stop::Steal();
	DeusRPG::Mug::Stop();
	DropItems::Stop();
	DropCoins::Stop();
	Client::centerPrint("<jc><f0>All Steal Training and auto dropping <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
//=====$vars======//Don't not edit this! =]
 $DeusRPG::Bash = false; $DeusRPG::Hide = false; $DeusRPG::M2Spell = ""; $DeusRPG::M1Spell = ""; $DeusRPG::Spellfix = ""; $MiningMove = 0; $WMove = 0; $BestWeapon = 0; $DeusRPG::Speech = 0; $DeusRPG::Track = 0; $DeusRPG::Steal = 0; $DeusRPG::AutoCasting = 0; $Haggle::Check = 0; $WDelay = 2;
//===============//



function DeusRPG::AutoCast($DeusRPG::Spell) {
	$DeusRPG::AutoCasting = 1;
	$DeusRPG::MSpell = $DeusRPG::Spell;

	say(0, "#cast "@$DeusRPG::Spell);
	$DeusRPG::MCast = 1;
	DeusRPGPack::CheckSpellLoop();
	Client::centerPrint("<jc><f0>Auto cast "@$DeusRPG::Spell@" <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

function Stop::AutoCast() { $DeusRPG::AutoCasting = 0; $DeusRPG::MCast = 0;
	Client::centerPrint("<jc><f0>Auto cast "@$DeusRPG::Spell@" <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

function DeusRPGPack::CheckSpellLoop() {
	if($DeusRPG::AutoCasting == 1) {
		say(0, "#cast "@$DeusRPG::Spell);
		Schedule("DeusRPGPack::CheckSpellLoop();", 10); }
}

function DeusRPGPack::Stuff(%client, %msg) {
	if(%client)
		$lastClientMessage = %client;
	else {
		if(String::findSubStr(%msg, "You are ready to cast.") != -1) {
			if($DeusRPG::AutoCasting == 1) {
				say(0, "#cast "@$DeusRPG::Spell);
				return mute;
			}

			return true;
		}
		else if(String::findSubStr(%msg, "Insufficient mana ") != -1) {
			if($DeusRPG::AutoCasting == 1) {
				$DeusRPG::AutoCasting = 0;
				say(0, "#meditate");
				Schedule("say(0, \"#meditate\");", 3); //Just in case! (lag)
				$DeusRPG::Meditate = true;
				if($MTrack::Toggle == 1) {
					Schedule::Add("$DeusRPG::Track = 1; Track1::Loop();", 4); }
				Schedule("DeusRPGPack::CheckMana();", 5);
			}

			return true;
		}
		else if(String::findSubStr(%msg, "TSpeech") != -1)
			return mute; //If others have this they won't hear your speech shit :D
		else if(String::findSubStr(%msg, "You are no longer Hiding In Shadows.") != -1) {
			if($DeusRPG::Hide == true) {
				$Hide::Check = 1;
				say(0, "#hide");
				Schedule("say(0, \"#hide\");", 1.5);
			}
			return true;
		}
		else if(String::findSubStr(%msg, "You are no longer Hiding In Shadows.") != -1) {
			$Hide::Check = 0;
			DeusRPG::HideLoop();
			return true;
		}
		else if(String::findSubStr(%msg, " tells you, ") != -1) {
			if($DeusRPG::Options[SettingNewPW]) { //So admins can't #anon YourName tells you, hahahaha!_ownage!   =P
				%name = GetWord(%msg, 0);
				if(%name == Client::getName(getManagerId())) {

					%Nmsg = RemoveThisFromList("\"", %msg);	// Remove all  " s
					%newpw = GetWord(%Nmsg, 3);

					Schedule("say(0, \"#mypassword "@%newpw@"\");", 1.1);
				}
				$DeusRPG::Options[SettingNewPW] = "";
				ChangePW(%name, $DeusPW[$DeusRPG::CurrentNum], $DeusPW::Hostname[$DeusRPG::CurrentNum], %newpw);
			}
		} //Deus_ex_Machina tells you, "bLaH.."

		return true;
	}
	return true;
}

function DeusRPGPack::CheckMana() {
	$DeusRPGPack::Spell::mana = DeusRPG::FetchData("MANA");
	$DeusRPGPack::Spell::maxmana = DeusRPG::FetchData("MaxMANA");
	if($DeusRPG::Meditate == true) {
		if($DeusRPGPack::Spell::mana >= $DeusRPGPack::Spell::maxmana) {
			$DeusRPG::Meditate = false;
			$DeusRPG::Track = 0;
			if($MTrack::Toggle == 1) {
				Schedule::Add("say(0, \"#wake\");", 1);
				if($DeusRPG::MCast == 1) {
					$DeusRPG::AutoCasting = 1;
					Schedule::Add("DeusRPGPack::CheckSpellLoop();", 3);
				}
				Schedule::Add("say(0, \"#wake\");", 10); //Just in case!
			}
			else {
				say(0, "#wake");
				Schedule("DeusRPGPack::CheckSpellLoop();", 1);
				Schedule("say(0, \"#wake\");", 5); //Just in case! (lag)
				if($DeusRPG::MCast == 1) {
					$DeusRPG::AutoCasting = 1;
					Schedule("say(0, \"#cast "@$DeusRPG::Spell@"\");", 2);
				}
			}
		}
		else
			Schedule("DeusRPGPack::CheckMana();", 2);
	}
}
//===New
//===These functions set the name to track
function Xin_::GetChars() {
	deleteVariables("Client::Track*");
	%i = 0;
	for(%cl = 2049; %cl <= 2100; %cl++) {
		%name = Client::GetName(%cl);
		if(%name != "") {
			%i++;
			$Client::TrackName[%i] = %name;
		}
	}
	$Xin_::NameNum = %i;
}
function Xin_::NameMenu(%Xin_MenuCommand) {
	$Xin_::MenuCommand = %Xin_MenuCommand;
	if($Xin_::MenuCommand == first) {
		Xin_::GetChars();
		$Xin_::Position = 1; }
	if($Xin_::MenuCommand == more) {
		if(($Xin_::Position + 9) <= $Xin_::NameNum)
			$Xin_::Position = 9 + $Xin_::Position;
		else
			$Xin_::Position = 1; }
	Xin_::CreateMenu();
	Menu::Display(MenuTRACKNAME);
}
function Xin_::CreateMenu() {
	Menu::New(MenuTRACKNAME, "Select Person to Track/Trackpack");
		for(%i = 1; %i <= 9; %i++) {
			%current = (%i - 1) + $Xin_::Position;
			if(%currnet <= $Xin_::NameNum)
				Menu::AddChoice(MenuTRACKNAME, (%i) @ "Track: " @ $Client::TrackName[%current] @ " ", "Xin_::SetName("@%current@");");
		}
		Menu::AddChoice(MenuTRACKNAME, "mMore", "Xin_::NameMenu(more);");
		Menu::AddChoice(MenuTRACKNAME, "zStop", "Xin_::SetName(0);");
}
function Xin_::SetName(%i) {
	if(%i != 0)
		$Name::Track = $Client::TrackName[%i];
	Menu::Display(MenuTRACK);
	Client::centerPrint("<jc><f0>Tracking <f1>"@$Name::Track@"", 1);
	Schedule::Add("Client::centerPrint(\"\", 1);", 4);
}
//===New
//===AutoJump
$Xin_::Jump = 0;
function Xin_::JumpToggle() {
	$Xin_::Jump = !($Xin_::Jump);
	if($Xin_::Jump) {
	Xin_::AutoJumpLoop();
	}
}
function Xin_::AutoJumpLoop() {
	if($Xin_::Jump == 1) {
		postAction(2048, IDACTION_MOVEUP, 1.000000);
		postAction(2048, IDACTION_MOVEUP, 0.000000);
		Schedule::Add("Xin_::AutoJumpLoop();", 0.1); }
}
//===New
//===Track while meditate toggle
function Xin_MTrack::Toggle() {
	$MTrack::Toggle = !($MTrack::Toggle);
	if($MTrack::Toggle == 1) {
		Client::centerPrint("<jc><f0>Track/Meditate <f1>enabled", 1);
		Schedule::Add("Client::centerPrint(\"\", 1);", 8); }
	else {
		Client::centerPrint("<jc><f0>Track/Meditate <f1>disabled", 1);
		Schedule::Add("Client::centerPrint(\"\", 1);", 8); }
}
//===New
//===tracking
function Xin_::Track1() { $DeusRPG::Track = 1;
	Track1::Loop();
	Client::centerPrint("<jc><f0>Real Track Training <f1>started", 1);
	Schedule::Add("Client::centerPrint(\"\", 1);", 4);
}
function Track1::Loop() {
	if($DeusRPG::Track == 1) {
		say(0, "#compass town");
		Schedule::Add("Track1::Loop();", 1.5); }
}
function Stop::Track() { $DeusRPG::Track = 0;
	Client::centerPrint("<jc><f0>Track Training <f1>stopped", 1);
	Schedule::Add("Client::centerPrint(\"\", 1);", 4);
}
//===
$Name::Track = $PCFG::Name;
function Xin_::Track2() { $DeusRPG::Track = 1;
	Track2::Loop();
	Client::centerPrint("<jc><f0>Person Track Training <f1>started", 1);
	Schedule::Add("Client::centerPrint(\"\", 1);", 4);
}
function Track2::Loop() {
	if($DeusRPG::Track == 1) {
		say(0, "#track "@$Name::Track@"");
		Schedule::Add("Track2::Loop();", 1.5); }
}
//===
function Xin_::Track3() { $DeusRPG::Track = 1;
	Track3::Loop();
	Client::centerPrint("<jc><f0>Track Pack Training <f1>started", 1);
	Schedule::Add("Client::centerPrint(\"\", 1);", 4);
}
function Track3::Loop() {
	if($DeusRPG::Track == 1) {
		say(0, "#trackpack "@$Name::Track@"");
		Schedule::Add("Track3::Loop();", 1.5); }
}
//===New
function Xin_Meditate::Quickly() {
	if($DeusRPG::Meditate == true) {
		$DeusRPG::Meditate = false;
		say(0, "#wake");
		Schedule::Add("say(0, \"#wake\");", 1.5); //Just in case!
		if($DeusRPG::MCast == 1) {
			$DeusRPG::AutoCasting = 1;
			Schedule::Add("DeusRPGPack::CheckSpellLoop();", 3); }
	}
	else {
		$DeusRPG::Track = 0;
		$DeusRPG::AutoCasting = 0;
		say(0, "#meditate");
		Schedule::Add("say(0, \"#meditate\");", 1.5);
		$DeusRPG::Meditate = true;
		Schedule::Add("DeusRPGPack::CheckMana();", 2.5);
	}
}

//===Autohide
function DeusRPG::Start::Hide() { $DeusRPG::Hide = true;  $Hide::Check = 0;
	Client::centerPrint("<jc><f0>Hide Training <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
	DeusRPG::HideLoop();
}
function DeusRPG::HideLoop() {
	if($DeusRPG::Hide == true) {
		say(0, "#hide");
		Schedule("DeusRPG::HideLoop();", 30);
	}
}

function DeusRPG::Stop::Hide() { $DeusRPG::Hide = false;
	Client::centerPrint("<jc><f0>Hide Training <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
//===Autobash
function DeusRPG::Start::Bash() { $DeusRPG::Bash = true;
	Client::centerPrint("<jc><f0>Bash Training <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
	DeusRPG::BashLoop();
}
function DeusRPG::BashLoop() {
	if($DeusRPG::Bash == true) {
		say(0, "#bash");
		%delay = Cap(101 - DeusRPG::FetchDataTemp("LVL"), 5, 50);
		Schedule("DeusRPG::BashLoop();", %delay); }
}
function DeusRPG::Stop::Bash() { $DeusRPG::Bash = false;
	Client::centerPrint("<jc><f0>Bash Training <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
//===
function Start::Speech($DeusRPG::SpeechDelay) { $DeusRPG::Speech = 1;
	Start::Speech::Loop();
	Client::centerPrint("<jc><f0>Speech Training (Delay = "@$DeusRPG::SpeechDelay@") <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
function Start::Speech::Loop() {
	if($DeusRPG::Speech == 1) {
		say(0, "#say TSpeech");
		Schedule("Start::Speech::Loop();", $DeusRPG::SpeechDelay); }
}
function Stop::Speech() { $DeusRPG::Speech = 0;
	Client::centerPrint("<jc><f0>Speech Training <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
//================
function DeusRPG::Steal() { $DeusRPG::Steal = 1;
	Steal::Loop();
	Client::centerPrint("<jc><f0>Steal Training <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
function Steal::Loop() {
	if($DeusRPG::Steal == 1) {
		say(0, "#steal");
		Schedule("Steal::Loop();", 5.1); }
}
function Stop::Steal() { $DeusRPG::Steal = 0;
	Client::centerPrint("<jc><f0>Steal Training <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
//========
function DeusRPG::PickItem($DeusRPG::MugItem) {

	if($DeusRPG::MugItem != "")
		Client::centerPrint("<jc><f0>PickPocket, Mug, and auto drop item is now <f1>"@$DeusRPG::MugItem, 1);
	else
		echo("Syntax error in input.");

	Schedule("Client::centerPrint(\"\", 1);", 4);
}

function DeusRPG::PickCItem() {

	if($CItem != "") {
		Client::centerPrint("<jc><f0>PickPocket, Mug, and auto drop item is now <f1>"@$CItem, 1);
		Schedule("Client::centerPrint(\"\", 1);", 4);
	}
	else {
		Client::centerPrint("<jc><f0>Error. Press ~ and try again.\nHeres an example\n$CItem = \"Blue Potion\";", 1);
		Schedule("Client::centerPrint(\"\", 1);", 8);
		return;
	}
	$DeusRPG::MugItem = $CItem;
}

function DeusRPG::Mug::Start($DeusRPG::PickMug) {
	if($DeusRPG::MugItem != "") {
		DeusRPG::Mug::Loop();
		Client::centerPrint("<jc><f0>Auto "@$DeusRPG::PickMug@" item <f1>"@$DeusRPG::MugItem@" : <f2>Started", 1);
	}
	else
		Client::centerPrint("<jc><f0>First pick an item to "@$DeusRPG::PickMug@" in the Steal Menu.", 1);

	Schedule("Client::centerPrint(\"\", 1);", 4);
}

function DeusRPG::Mug::Loop() {

	if($DeusRPG::PickMug == Mug)
		say(0, "#mug");
	else if($DeusRPG::PickMug == Pickpocket)
		say(0, "#pickpocket");
	else
		return;

	Schedule("DeusRPG::TakeThis();",1.0);
	Schedule("DeusRPG::Mug::loop();", 15);
}

function DeusRPG::Mug::Stop() {
	Client::centerPrint("<jc><f0>Auto "@$DeusRPG::PickMug@" item<f1>"@DeusRPG::MugItem@" : <f2>Stoped", 1);
	$DeusRPG::PickMug = "";
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

//=======
function DeusRPG::TakeThis() {
	if($DeusRPG::MugItem == "")
		echo("Syntax error in input.");
		return;

	buy($DeusRPG::TakeThis);
}

//========
function DropItems::Start() {
	$DeusRPG::DropItems = 1;
	if($DeusRPG::MugItem != "") {
		DropItems::Loop();
		Client::centerPrint("<jc><f0>Auto dropping <f1>"@$DeusRPG::MugItem@" Items: <f2>Started", 1);
		Schedule("Client::centerPrint(\"\", 1);", 4);
	}
	else {
		Client::centerPrint("<jc><f0>First pick an item to drop in the Steal Menu.", 1);
		Schedule("Client::centerPrint(\"\", 1);", 4);
	}
}
function DropItems::Loop() {

	if($DeusRPG::MugItem == "") {
		echo("Syntax error in input.");
		return;
	}
	if($DeusRPG::DropItems == 0)
		return;
	drop($DeusRPG::MugItem);
	Schedule("DropItems::loop();", 15);
}
function DropItems::Stop() {
	$DeusRPG::DropItems = 0;
	Client::centerPrint("<jc><f0>Auto dropping <f1>"@$DeusRPG::MugItem@" Items: <f2>Stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

//=======
function DropCoins::Start($DeusRPG::Coins) { $DeusRPG::DropCoins = 1;
	DropCoins::Loop();
	Client::centerPrint("<jc><f0>Auto dropping <f1>"@$DeusRPG::Coins@" coins: <f2>Started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
function DropCoins::Loop() {
	if($DeusRPG::Dropcoins == 1) {
		say(0, "#dropcoins "@$DeusRPG::Coins@"");
		Schedule("DropCoins::loop();", 10);
	}
}
function DropCoins::Stop() { $DeusRPG::DropCoins = 0;
	Client::centerPrint("<jc><f0>Auto dropping <f1>"@$DeusRPG::Coins@" coins: <f2>Stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

function DeusRPG::Track() { $DeusRPG::Track = 1;
	FixTracking();
	DeusRPG::Track::Loop();
	Client::centerPrint("<jc><f0>Track Training <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

function FixTracking() {
	%cnt = 0;
	%skill = DeusRPG::FetchData("skill 19");
	if(%skill >= 3) {
		$DeusTrack[%cnt++] = "#compass town";
		$DeusTrack[%cnt++] = "#compass dungeon";
	}
	if(%skill >= 15)
		$DeusTrack[%cnt++] = "#track "@Client::getname(getManagerId());
	if(%skill >= 45)
		$DeusTrack[%cnt++] = "#zonelist all";
	if(%skill >= 85)
		$DeusTrack[%cnt++] = "#trackpack "@Client::getname(getManagerId());
}

function DeusRPG::Track::Loop(%i) {
	if($DeusTrack[%i] == "")
		%i = 0;
	if($DeusRPG::Track == 1) {
		say(0, $DeusTrack[%i++]);
		Schedule("DeusRPG::Track::Loop(%i);", 1);
	}
}
function DeusRPG::Stop::Track() { $DeusRPG::Track = 0;
	Client::centerPrint("<jc><f0>Track Training <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
// ==============================================
// Potion script here =]

//------------------------------------------------------------------------
$DeusRPGSkill::Potion[BluePotion, Strength] = 15;
$DeusRPGSkill::Potion[BluePotion, Desc] = "Blue Potion";

$DeusRPGSkill::Potion[CrystalBluePotion, Strength] = 60;
$DeusRPGSkill::Potion[CrystalBluePotion, Desc] = "Crystal Blue Potion";

$DeusRPGSkill::Potion[EnergyVial, Strength] = 16;
$DeusRPGSkill::Potion[EnergyVial, Desc] = "Energy Vial";

$DeusRPGSkill::Potion[CrystalEnergyVial, Strength] = 50;
$DeusRPGSkill::Potion[CrystalEnergyVial, Desc] = "Crystal Energy Vial";


$DeusRPGSkill::PotionSetup[AutoEnergy, Delay] = 1;
$DeusRPGSkill::PotionSetup[AutoEnergy, Running] = false;
$DeusRPGSkill::PotionSetup[AutoEnergy, Potion] = "";
$DeusRPGSkill::PotionSetup[AutoEnergy, Strength] = 0;
$DeusRPGSkill::PotionSetup[AutoEnergy, ItemDataName] = "MANA";
$DeusRPGSkill::PotionSetup[AutoEnergy, MaxItemDataName] = "MaxMANA";

$DeusRPGSkill::PotionSetup[AutoHeal, Delay] = 1;
$DeusRPGSkill::PotionSetup[AutoHeal, Running] = false;
$DeusRPGSkill::PotionSetup[AutoHeal, Potion] = "";
$DeusRPGSkill::PotionSetup[AutoHeal, Strength] = 0;
$DeusRPGSkill::PotionSetup[AutoHeal, ItemDataName] = "HP";
$DeusRPGSkill::PotionSetup[AutoHeal, MaxItemDataName] = "MaxHP";

//------------------------------------------------------------------------
function SetDelay::Potion(%DelayType, %SetDelay)
{
	Schedule::Cancel("Potion::Loop(" @ %DelayType@ ");");

	$DeusRPGSkill::PotionSetup[%DelayType, Delay] = %SetDelay;

	if($DeusRPGSkill::PotionSetup[%DelayType, Running] == true)
		Schedule::Add("Potion::Loop(" @ %DelayType @ ");", $DeusRPGSkill::PotionSetup[%DelayType, Delay]);

	Client::centerPrint("<jc><f0>Setting Potion " @ %DelayType @ " delay to<f1> " @ %SetDelay, 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

//------------------------------------------------------------------------
function DeusRPG::Use::Potion(%UseType, %Potion)
{
	if(%UseType != "" && %Potion != "")
	{
		// We do this so we don't need to pass these items into Potion::Loop
		// That way this can update the variables and still not need to recall
		// the loop if it is running (i.e. we change the potion to use)
		$DeusRPGSkill::PotionSetup[%UseType, Strength] = $DeusRPGSkill::Potion[%Potion, Strength];
		$DeusRPGSkill::PotionSetup[%UseType, Potion] = $DeusRPGSkill::Potion[%Potion, Desc];


		Client::centerPrint("<jc><f1>Potion<f0> " @ %UseType @ " (" @ $DeusRPGSkill::PotionSetup[%UseType, Potion] @ ") <f1>started", 1);
		Schedule("Client::centerPrint(\"\", 1);", 4);

		// If it is running, it will just use new potion
		if($DeusRPGSkill::PotionSetup[%UseType, Running] != true)
		{
			$DeusRPGSkill::PotionSetup[%UseType, Running] = true;
			Potion::Loop(%UseType);
		}
	}
	else
		echo("Empty param in Use::Potion. Type passed: " @ %UseType @ ". Potion name passed: " @ %Potion @ ".");
}

//------------------------------------------------------------------------
function Potion::Loop(%LoopType)
{
	if($DeusRPGSkill::PotionSetup[%LoopType, Running] == true)
	{
		if(getItemCount($DeusRPGSkill::PotionSetup[%LoopType, Potion]) == 0)
		{
			Client::centerPrint("<jc><f0>You don't have any " @ $DeusRPGSkill::PotionSetup[%LoopType, Potion] @ "s!", 1);
			Schedule("Client::centerPrint(\"\", 1);", 4);

			$DeusRPGSkill::PotionSetup[%LoopType, Running] = false;
			return;
		}

		%MyStat = DeusRPG::FetchData($DeusRPGSkill::PotionSetup[%LoopType, ItemDataName]);
		%MyMaxStat = DeusRPG::FetchData($DeusRPGSkill::PotionSetup[%LoopType, MaxItemDataName]);
		if(%MyStat <= (%MyMaxStat - $DeusRPGSkill::PotionSetup[%LoopType, Strength]))
			use($DeusRPGSkill::PotionSetup[%LoopType, Potion]);

		Schedule::Add("Potion::Loop(" @ %LoopType @ ");", $DeusRPGSkill::PotionSetup[%LoopType, Delay]);
	}
}

//------------------------------------------------------------------------
function Stop::Potion(%StopType)
{
	$DeusRPGSkill::PotionSetup[%StopType, Running] = false;

	Schedule::Cancel("Potion::Loop(" @ %StopType @ ");");

	Client::centerPrint("<jc><f1>Potion<f0> " @ %StopType @ " <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}

//
function DeusRPG::StopMoving() {
	postAction(2048, IDACTION_TURNLEFT, 0.000000);
	postAction(2048, IDACTION_TURNRIGHT, 0.000000);
	postAction(2048, IDACTION_MOVERIGHT, 0.000000);
	postAction(2048, IDACTION_MOVELEFT, 0.000000);
	postAction(2048, IDACTION_MOVEFORWARD, 0.000000);
	postAction(2048, IDACTION_MOVEBACK, 0.000000);
}
//
function Forward1() { postAction(2048, IDACTION_MOVEFORWARD, 1.000000); }
function Backward1() { postAction(2048, IDACTION_MOVEBACK, 1.000000); }
function TurnRight1() { postAction(2048, IDACTION_TURNRIGHT, 0.099988); }
function TurnLeft1() { postAction(2048, IDACTION_TURNLEFT, 0.099988); }
function MoveLeft1() { postAction(2048, IDACTION_MOVELEFT, 1.000000); }
function MoveRight1() { postAction(2048, IDACTION_MOVERIGHT, 1.000000); }
function Forward2() { postAction(2048, IDACTION_MOVEFORWARD, 1.000000); }
function Backward2() { postAction(2048, IDACTION_MOVEBACK, 1.000000); }
function TurnRight2() { postAction(2048, IDACTION_TURNRIGHT, 0.099988); }
function TurnLeft2() { postAction(2048, IDACTION_TURNLEFT, 0.099988); }
function MoveLeft2() { postAction(2048, IDACTION_MOVELEFT, 1.000000); }
function MoveRight2() { postAction(2048, IDACTION_MOVERIGHT, 1.000000); }
//
function StopForward1() { postAction(2048, IDACTION_MOVEFORWARD, 0.000000); }
function StopBackward1() { postAction(2048, IDACTION_MOVEBACK, 0.000000); }
function StopMoveLeft1() { postAction(2048, IDACTION_MOVELEFT, 0.000000); }
function StopMoveRight1() { postAction(2048, IDACTION_MOVERIGHT, 0.000000); }
function StopTurnRight1() { postAction(2048, IDACTION_TURNRIGHT, 0.000000); }
function StopTurnLeft1() { postAction(2048, IDACTION_TURNLEFT, 0.000000); }
function StopForward2() { postAction(2048, IDACTION_MOVEFORWARD, 0.000000); }
function StopBackward2() { postAction(2048, IDACTION_MOVEBACK, 0.000000); }
function StopMoveLeft2() { postAction(2048, IDACTION_MOVELEFT, 0.000000); }
function StopMoveRight2() { postAction(2048, IDACTION_MOVERIGHT, 0.000000); }
function StopTurnRight2() { postAction(2048, IDACTION_TURNRIGHT, 0.000000); }
function StopTurnLeft2() { postAction(2048, IDACTION_TURNLEFT, 0.000000); }
//
function Mining::Start($QuitRPG, %opt) {
	if($Server::HostPublicGame == true)
		echo("DesuRPG: Warning! If host public game is on you may get fetchdata(); errors if your not hosting!");
	if($SpamUser == true) {
		Client::centerPrint("<jc><f0>ERROR! You didn't run Tribes -mod rpg!! Quiting automine!", 1);
		echo("DeusRPG: WARNING!! YOU ARE NOT RUNNING -mod RPG !! You may get errors if you don't run Tribes.exe -mod RPG");
		return;
	}
	if($MiningMove == 1) //Already mining!
		return;
	if(!DeusRPG::CheckForPickAxe()) {
		Client::centerPrint("<jc><f0>Don't have any kind of <f1>Pick Axe!", 1);
		Schedule("Client::centerPrint(\"\", 1);", 2);
		return;
	}
	if($QuitRPG == "true") {
		Client::centerPrint("<jc><f0>Auto Mining <f1>started <f2>: <f0>OverWeight Quit = True", 1);
		Schedule("Client::centerPrint(\"\", 1);", 5);
	}
	else if($QuitRPG == "false") {
		Client::centerPrint("<jc><f0>Auto Mining <f1>started <f2>: <f0>OverWeight Quit = False", 1);
		Schedule("Client::centerPrint(\"\", 1);", 5);
	}
	else {
		Client::centerPrint("<jc><f0>Auto Mining <f1>started <f2>: <f0>Quit =<f2> ERROR!! Setting defualt Quit to false", 1);
		Schedule("Client::centerPrint(\"\", 1);", 15);
		echo("DeusRPGPack: Error in Automining with TRUE and FALSE....setting to FALSE");
		$QuitRPG = "false";
	}
	$MiningMove = 1;
	$Xin_::MineWatch = 0;
	$Xin_::MoveCount = 0;
	if(%opt == "old")
		Mining::Movement();
	else
		Xin_::AutoMine();
}

function Mining::Stop() {
	$MiningMove = "";
	DeusRPG::StopMoving();
	Schedule("DeusRPG::StopMoving();", 6);
	Schedule::Cancel(MineMover);
	postAction(2048, IDACTION_BREAK1, 1);
	Client::centerPrint("<jc><f0>Auto Mining <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);

}
function DeusRPG::CheckForPickAxe() {
	if(getItemCount("Hammer Pick")) //Check for a mining tool
		use("Hammer Pick");
	else if(getItemCount("Pick Axe"))
		use("Pick Axe");
	else if(getItemCount("Rusty Pick Axe"))
		use("Rusty Pick Axe");
	else {
		$MiningMove = "";
		return false;
	}
	return true;
}
function Mining::Movement() {
	if(!DeusRPG::CheckForPickAxe()) {
		Client::centerPrint("<jc><f0>Don't have any kind of <f1>Pick Axe!", 1);
		Schedule("Client::centerPrint(\"\", 1);", 2);
		Mining::Stop();
		return;
	}
	if($MiningMove == 1) {
		postAction(2048, IDACTION_FIRE1, 1);
		if($DeusRPG::QuitOnOverWeight)
			DeusRPG::Getweight();	//Check your weight
//Schedule("Backward1();", 0.5); Schedule("StopBackward1();", 0.9); Schedule("Forward1();", 4.5); Schedule("StopForward1();", 4.8199); Schedule("Mining::Movement();", 15); // OLD
		Schedule("Backward1();", 0.5);		//NEW
		Schedule("StopBackward1();", 0.7);	//Thanks to TangWei
		Schedule("Forward1();", 5.4);		//This will keep you on a crystal for about an hour or so if no lag...
		Schedule("StopForward1();", 5.632);
		Schedule::Add("Mining::Movement();", 10, MineMover);
	}
}


//===Xin_'s Mining
function Xin_::MiningWatcher(%client, %msg) {
	if(%client)
		$lastClientMessage = %client;
	else {
		if($MiningMove == 1) {
			if(String::findSubStr(%msg, "You found ") != -1)
				$Xin_::MineWatch = 1;
		}
	}
	return true;
}

//===New
function Xin_::AutoMine() {
	if(!DeusRPG::CheckForPickAxe()) {
		Client::centerPrint("<jc><f0>Don't have any kind of <f1>Pick Axe!", 1);
		Schedule("Client::centerPrint(\"\", 1);", 2);
		Mining::Stop();
		return;
	}
	if($MiningMove == 1) {
		postAction(2048, IDACTION_FIRE1, 1);
		if($DeusRPG::QuitOnOverWeight)
			DeusRPG::Getweight();
		if($Xin_::MineWatch != 1) {
			if($Xin_::MoveCount == 0)
				$Xin_::Direction = backward;
			if($Xin_::MoveCount == 7)
				$Xin_::Direction = forward;
			if($Xin_::Direction == backward) {
				$Xin_::MoveCount++;
				Backward1();
				Schedule::Add("StopBackward1();", 0.1);
			}
			else {
				$Xin_::MoveCount--;
				Forward1();
				Schedule::Add("StopForward1();", 0.1);
			}
		}
		Schedule::Add("$Xin_::MineWatch = 0;", 0.15);
		Schedule::Add("Xin_::AutoMine();", 2);
	}
}


function DeusRPG::GetWeight() {	//Check Weight
	$DeusRPG::MiningMaxWeight = DeusRPG::FetchDataTemp("MaxWeight");
	$DeusRPG::MiningWeight = FixDecimals(DeusRPG::FetchDataTemp("Weight"));

	if($DeusRPG::MiningWeight >= $DeusRPG::MiningMaxWeight) 	//If Weight is more then MaxWeight call Over::Weight
		DeusRPG::OverWeight();
}

function DeusRPG::OverWeight() {
	if($MiningMove == 1) {	//Make sure your still mining
		DeusRPG::GetWeight();
		if($DeusRPG::MiningWeight >= $DeusRPG::MiningMaxWeight) {	//Check again if Weight is more then MaxWeight
			Mining::Stop();
			Client::centerPrint("<jc><f0>Your over weight! <f1>"@$DeusRPG::MiningWeight@" <f0>/ <f1>"@$DeusRPG::MiningMaxWeight, 1);
			Schedule("Client::centerPrint(\"\", 1);", 5);
			Schedule("DeusRPG::StopMoving();", 1.5);
			postAction(2048, IDACTION_BREAK1, 1);
			if($QuitRPG == "true") {
				Schedule("say(0, \"#recall\");", 10.0);
				Client::centerPrint("<jc><f0>Your over weight! <f1>Recalling and shutting down...", 11);
				Schedule("Client::centerPrint(\"\", 1);", 290);
				Schedule("quit();", 300.0);
			}
		}
	}
}

function DeusRPG::WeightTraining() {
	$WMove = 1;
	DeusRPG::WeightLoop();
	Client::centerPrint("<jc><f0>Weight Training <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4); }

function DeusRPG::WeightLoop() {
	if($WMove == 1)
	{
		if(DeusRPG::FetchData("skill 5") >= ((DeusRPG::FetchData("LVL") * 10) + 20) && $DeusRPG::QuitOnOverWeight)
		{
			$WMove = 0;
			DeusRPG::StopMoving();
			Client::centerPrint("<jc><f0>Weight Training <f1>maxed. <f0>Stopping training.", 1);
			Schedule("Client::centerPrint(\"\", 1);", 4);
		}
		else
		{
			Schedule("Forward1();", 0.5);
			Schedule("TurnLeft1();", 0.6);
			Schedule("DeusRPG::WeightLoop();", 5, WLoop);
		}
	}
}

function DeusRPG::StopWeightTraining() {
	$WMove = 0;
	DeusRPG::StopMoving();
	Schedule("DeusRPG::StopMoving();", 5);
	Client::centerPrint("<jc><f0>Weight Training <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 6);
}
if($Server::HostPublicGame == true)
	echo("DesuRPG: Warning! If host public game is on you may get fetchdata(); errors if your not hosting!");
Event::Attach(eventClientMessage, DeusRPGPack::Stuff);
Event::Attach(eventClientMessage, XIn_::MiningWatcher);
$DeusRPG::ScriptCheck++;