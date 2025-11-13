//There are FOUR hard-coded groups:
//-Priest
//-Rogue
//-Warrior
//-Wizard

//Each of these has classes.  They are specified in here.
//Anything that does NOT have to do with visuals when it comes to classes should ALWAYS use the 0 offset in $ClassName.

$initcoins[Priest] = "3d6x10";
$initcoins[Rogue] = "2d6x10";
$initcoins[Warrior] = "5d4x10";
$initcoins[Wizard] = "1d4+1x10";

$MinHP[MaleHuman] = 12;
$MinHP[FemaleHuman] = 12;
$MinHP[Traveller] = 12;
$MinHP[Goblin] = 0;
$MinHP[Orc] = 7;
$MinHP[Ogre] = 20;
$MinHP[Pigman] = 1;
$MinHP[Undead] = 90;
$MinHP[Elf] = 10;
$MinHP[Minotaur] = 200;
$MinHP[Alien] = 500;
$MinHP[Demon] = 1000;
$MinHP[DeathKnight] = 5000;
$MinHP[God] = 4000;

$ClassName[1, 0] = "Cleric";
$ClassName[2, 0] = "Druid";
$ClassName[3, 0] = "Thief";
$ClassName[4, 0] = "Bard";
$ClassName[5, 0] = "Fighter";
$ClassName[6, 0] = "Paladin";
$ClassName[7, 0] = "Squire";
$ClassName[8, 0] = "Mage";
$ClassName[9, 0] =  "Red Flag";

$ClassGroup[Cleric] = "Priest";
$ClassGroup[Druid] = "Priest";
$ClassGroup[Thief] = "Rogue";
$ClassGroup[Bard] = "Rogue";
$ClassGroup[Fighter] = "Warrior";
$ClassGroup[Paladin] = "Warrior";
$ClassGroup[Squire] = "Warrior";
$ClassGroup[Mage] = "Wizard";

//===================================
// REMORTS
//===================================
for(%x = 1; %x < 5; %x = %x + 1)
{
	$ClassName[1, %x] = "Cleric";
	$ClassName[2, %x] = "Druid";
	$ClassName[3, %x] = "Thief";
	$ClassName[4, %x] = "Bard";
	$ClassName[5, %x] = "Fighter";
	$ClassName[6, %x] = "Paladin";
	$ClassName[7, %x] = "Squire";
	$ClassName[8, %x] = "Mage";
	$ClassName[9, %x] = "Red Flag";
}

for(%x = 5; %x < 15; %x = %x + 1)
{
	$ClassName[1, %x] = "Bishop";
	$ClassName[2, %x] = "Archdruid";
	$ClassName[3, %x] = "Bandit";
	$ClassName[4, %x] = "Lyricist";
	$ClassName[5, %x] = "Berzerker";
	$ClassName[6, %x] = "Avenger";
	$ClassName[7, %x] = "Dark Knight";
	$ClassName[8, %x] = "Archmage";
	$ClassName[9, %x] = "Hacker";
}

for(%x = 15; %x < 30; %x = %x + 1)
{
	$ClassName[1, %x] = "Saint";
	$ClassName[2, %x] = "Mystic";
	$ClassName[3, %x] = "Outlaw";
	$ClassName[4, %x] = "Musician";
	$ClassName[5, %x] = "Knight";
	$ClassName[6, %x] = "Centurian";
	$ClassName[7, %x] = "Gallant";
	$ClassName[8, %x] = "Sorcerer";
	$ClassName[9, %x] = "Smack Tard";
}

for(%x = 30; %x < 50; %x = %x + 1)
{
	$ClassName[1, %x] = "Pope";
	$ClassName[2, %x] = "Head Mystic";
	$ClassName[3, %x] = "Klepto";
	$ClassName[4, %x] = "Poet";
	$ClassName[5, %x] = "Guardian";
	$ClassName[6, %x] = "Crusader";
	$ClassName[7, %x] = "Dark Lord";
	$ClassName[8, %x] = "Wizard";
	$ClassName[9, %x] = "Embarresment";
}

for(%x = 50; %x < 75; %x = %x + 1)
{
	$ClassName[1, %x] = "Diviner";
	$ClassName[2, %x] = "Prophet";
	$ClassName[3, %x] = "Criminal";
	$ClassName[4, %x] = "Actor";
	$ClassName[5, %x] = "Champion";
	$ClassName[6, %x] = "Vanquisher";
	$ClassName[7, %x] = "Dragoon";
	$ClassName[8, %x] = "Magus";
	$ClassName[9, %x] = "Fuck Tard";
}

for(%x = 75; %x < 100; %x = %x + 1)
{
	$ClassName[1, %x] = "Angel";
	$ClassName[2, %x] = "Oracle";
	$ClassName[3, %x] = "Mastermind";
	$ClassName[4, %x] = "Producer";
	$ClassName[5, %x] = "Conqueror";
	$ClassName[6, %x] = "Hero";
	$ClassName[7, %x] = "Dark Nobleman";
	$ClassName[8, %x] = "Grand Magus";
	$ClassName[9, %x] = "Shit Head";
}

for(%x = 100; %x < 9999; %x = %x + 1)
{
	$ClassName[1, %x] = "Halo";
	$ClassName[2, %x] = "Mind Reader";
	$ClassName[3, %x] = "Serial Killer";
	$ClassName[4, %x] = "Editor";
	$ClassName[5, %x] = "King RichardX";
	$ClassName[6, %x] = "Demi";
	$ClassName[7, %x] = "Evil Emperor";
	$ClassName[8, %x] = "Super Magus";
	$ClassName[9, %x] = "Red Fag";
}

function getFinalCLASS(%clientId)
{
	dbecho($dbechoMode, "getFinalCLASS(" @ %clientId @ ")");

	for(%i = 1; $ClassName[%i, 0] != ""; %i++)
	{
		if(String::ICompare($ClassName[%i, 0], fetchData(%clientId, "CLASS")) == 0)
			return $ClassName[%i, fetchData(%clientId, "RemortStep")];
	}
	return Joining;
}

function IsAClass(%class)
{
	dbecho($dbechoMode, "IsAClass(" @ %class @ ")");

	for(%i = 1; $ClassName[%i, 0] != ""; %i++)
	{
		if(String::ICompare(%class, $ClassName[%i, 0]) == 0)
			return True;
	}

	return False;
}

$WorldRank[0] = "Newbie";
$WorldRank[1] = "Adventurer";
$WorldRank[2] = "Soldier";
$WorldRank[3] = "Gladiator";
$WorldRank[4] = "Star";
$WorldRank[5] = "SuperStar";
$WorldRank[6] = "Titan";
$WorldRank[7] = "Demi-God";
$WorldRank[8] = "God";
$WorldRank[9] = "Admin 1";
$WorldRank[10] = "Admin 2";

$TournyRoundOne[0] = "King";
$TournyRoundTwo[0] = "Eradicator";
$TournyRoundThree[0] = "DeathStar";
$TournyRoundOne[1] = "DeathStar";
$TournyRoundTwo[1] = "BanishedKing";
$TournyRoundThree[1] = "Invader";
$TournyRoundOne[2] = "Invader";
$TournyRoundTwo[2] = "MoonBreaker";
$TournyRoundThree[2] = "BattleOx";
$TournyRoundOne[3] = "MoonBreaker";
$TournyRoundTwo[3] = "Banisher";
$TournyRoundThree[3] = "Incarnate";
$TournyRoundOne[4] = "Incarnate";
$TournyRoundTwo[4] = "Queen";
$TournyRoundThree[4] = "Disintegrator";
$TournyRoundOne[5] = "Disintegrator";
$TournyRoundTwo[5] = "Purifier";
$TournyRoundThree[5] = "Vaporizer";
$TournyRoundOne[6] = "Vaporizer";
$TournyRoundTwo[6] = "Insurrector";
$TournyRoundThree[6] = "SoulConsumer";
$TournyRoundOne[7] = "SoulConsumer";
$TournyRoundTwo[7] = "SoulEraser";
$TournyRoundThree[7] = "EndGame";
$TournyRoundOne[8] = "EndGame";
$TournyRoundTwo[8] = "Oni";
$TournyRoundThree[8] = "Crest";
$TournyRoundOne[9] = "Master";
$TournyRoundTwo[9] = "Ripper";
$TournyRoundThree[9] = "Flux";
$TournyRoundOne[10] = "Protector";
$TournyRoundTwo[10] = "Hybrid";
$TournyRoundThree[10] = "Crucifier";