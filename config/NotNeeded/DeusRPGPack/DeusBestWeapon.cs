//By: Deus_ex_Machina
//
//	BestWeapon ver 2.0

$DeusBestWeaponver = "ver2.0";

//Re-wrote this whole file because it sucked trying to keep it up-to-date =p
//Now its much easyer and faster.

function BestWeapon(%weapon, %skill) {

	if(%weapon == "")
		return false;

	use(%weapon);
	Client::centerPrint("<jc><f0>Using best <f1>"@$DeusRPG::SkillName[%skill]@" Weapon\n<jc><f2>"@%weapon, 1); //Shows what best weapon your using =]  kinda buggy if you switch weapons real fast sometimes it doesn't show =\

	Schedule("Client::centerPrint(\"\", 1);", 4);

	return true;
}
function DeusRPGPack::func9() {

	for(%i = 1; $DeusRPG::WList[%i, $SkillBludgeoning] != ""; %i++) {
		if(getItemCount($DeusRPG::WList[%i, $SkillBludgeoning]))
				%best = $DeusRPG::WList[%i, $SkillBludgeoning];
	}

	if(!BestWeapon(%best, $SkillBludgeoning)) {
		Client::centerPrint("<jc><f0>You don't have a <f1>Bludgeoning weapon", 1);
		Schedule("Client::centerPrint(\"\", 1);", 3);
	}
}
function DeusRPGPack::func10() {

	for(%i = 1; $DeusRPG::WList[%i, $SkillPiercing] != ""; %i++) {
		if(getItemCount($DeusRPG::WList[%i, $SkillPiercing]))
				%best = $DeusRPG::WList[%i, $SkillPiercing];
	}

	if(!BestWeapon(%best, $SkillPiercing)) {
		Client::centerPrint("<jc><f0>You don't have a <f1>Piercing weapon", 1);
		Schedule("Client::centerPrint(\"\", 1);", 3);
	}
}
function DeusRPGPack::func11() {

	for(%i = 1; $DeusRPG::WList[%i, $SkillSlashing] != ""; %i++) {
		if(getItemCount($DeusRPG::WList[%i, $SkillSlashing]))
				%best = $DeusRPG::WList[%i, $SkillSlashing];
	}

	if(!BestWeapon(%best, $SkillSlashing)) {
		Client::centerPrint("<jc><f0>You don't have a <f1>Slashing weapon", 1);
		Schedule("Client::centerPrint(\"\", 1);", 3);
	}
}
function DeusRPGPack::func12() {

	for(%i = 1; $DeusRPG::WList[%i, $SkillArchery] != ""; %i++) {
		if(getItemCount($DeusRPG::WList[%i, $SkillArchery]))
				%best = $DeusRPG::WList[%i, $SkillArchery];
	}

	if(!BestWeapon(%best, $SkillArchery)) {
		Client::centerPrint("<jc><f0>You don't have a <f1>Archery weapon", 1);
		Schedule("Client::centerPrint(\"\", 1);", 3);
	}
}


$SkillSlashing = 1;//This way if you host it won't fuck up anything. =]
$SkillPiercing = 2;
$SkillBludgeoning = 3;
$SkillArchery = 14;

$DeusRPG::SkillName[$SkillSlashing] = "Slashing";
$DeusRPG::SkillName[$SkillPiercing] = "Piercing";
$DeusRPG::SKillName[$SkillBludgeoning] = "Bludgeoning";
$DeusRPG::SKillName[$SkillArchery] = "Archery";

%i=0;
$DeusRPG::WList[%i++, $SkillSlashing] = "Hatchet";
$DeusRPG::WList[%i++, $SkillSlashing] = "Broad Sword";
$DeusRPG::WList[%i++, $SkillSlashing] = "War Axe";
$DeusRPG::WList[%i++, $SkillSlashing] = "Long Sword";
$DeusRPG::WList[%i++, $SkillSlashing] = "Battle Axe";
$DeusRPG::WList[%i++, $SkillSlashing] = "Bastard Sword";
$DeusRPG::WList[%i++, $SkillSlashing] = "Halberd";
$DeusRPG::WList[%i++, $SkillSlashing] = "Claymore";
$DeusRPG::WList[%i++, $SkillSlashing] = "Keldrinite Long Sword";
//.................................................................................
%i=0;
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Club";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Quarter Staff";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Bone Club";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Spiked Club";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Mace";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Hammer Pick";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Spiked Bone Club";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Long Staff";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "War Hammer";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "Justice Staff";
$DeusRPG::WList[%i++, $SkillBludgeoning] = "War Maul";
//.................................................................................
%i=0;
$DeusRPG::WList[%i++, $SkillPiercing] = "Pick Axe";
$DeusRPG::WList[%i++, $SkillPiercing] = "Knife";
$DeusRPG::WList[%i++, $SkillPiercing] = "Dagger";
$DeusRPG::WList[%i++, $SkillPiercing] = "Short Sword";
$DeusRPG::WList[%i++, $SkillPiercing] = "Spear";
$DeusRPG::WList[%i++, $SkillPiercing] = "Gladius";
$DeusRPG::WList[%i++, $SkillPiercing] = "Trident";
$DeusRPG::WList[%i++, $SkillPiercing] = "Rapier";
$DeusRPG::WList[%i++, $SkillPiercing] = "Awl Pike";
//.................................................................................
%i=0;
$DeusRPG::WList[%i++, $SkillArchery] = "Sling";
$DeusRPG::WList[%i++, $SkillArchery] = "Short Bow";
$DeusRPG::WList[%i++, $SkillArchery] = "Light Crossbow";
$DeusRPG::WList[%i++, $SkillArchery] = "Long Bow";
$DeusRPG::WList[%i++, $SkillArchery] = "Composite Bow";
$DeusRPG::WList[%i++, $SkillArchery] = "Repeating Crossbow";
$DeusRPG::WList[%i++, $SkillArchery] = "Elven Bow";
$DeusRPG::WList[%i++, $SkillArchery] = "Aeolus's Wing";
$DeusRPG::WList[%i++, $SkillArchery] = "Heavy Crossbow";
%i="";

$DeusRPG::ScriptCheck++;