//######################################################################################
// Skills
//######################################################################################

$SkillSlashing = 1;
$SkillPiercing = 2;
$SkillBludgeoning = 3;
$SkillDodging = 4;
$SkillWeightCapacity = 5;
$SkillBashing = 6;
$SkillStealing = 7;
$SkillHiding = 8;
$SkillBackstabbing = 9;
$SkillOffensiveCasting = 10;
$SkillDefensiveCasting = 11;
$SkillSpellResistance = 12;
$SkillHealing = 13;
$SkillArchery = 14;
$SkillEndurance = 15;
$SkillCriticals = 16;
$SkillMining = 17;
$SkillSpeech = 18;
$SkillSenseHeading = 19;
$SkillEnergy = 20;
$SkillHaggling = 21;
$SkillNeutralCasting = 22;
$MinLevel = "L";
$MinGroup = "G";
$MinClass = "C";
$MinRemort = "R";
$MinAdmin = "A";
$MinHouse = "H";

$SkillDesc[1] = "Slashing";
$SkillDesc[2] = "Piercing";
$SkillDesc[3] = "Bludgeoning";
$SkillDesc[4] = "Dodging";
$SkillDesc[5] = "Weight Capacity";
$SkillDesc[6] = "Bashing";
$SkillDesc[7] = "Stealing";
$SkillDesc[8] = "Hiding";
$SkillDesc[9] = "Backstabbing";
$SkillDesc[10] = "Offensive Casting";
$SkillDesc[11] = "Defensive Casting";
$SkillDesc[12] = "Spell Resistance";
$SkillDesc[13] = "Healing";
$SkillDesc[14] = "(no longer used)";
$SkillDesc[15] = "Endurance";
$SkillDesc[16] = "Critical";
$SkillDesc[17] = "(no longer used)";
$SkillDesc[18] = "Speech";
$SkillDesc[19] = "Sense Heading";
$SkillDesc[20] = "Energy";
$SkillDesc[21] = "Haggling";
$SkillDesc[22] = "Neutral Casting";
$SkillDesc[L] = "Level";
$SkillDesc[G] = "Group";
$SkillDesc[C] = "Class";
$SkillDesc[R] = "Remort";
$SkillDesc[A] = "Admin Level";
$SkillDesc[H] = "House";

//######################################################################################
// Class multipliers
//######################################################################################

//***********************************
// GENERAL RULES FOR MULTIPLIERS:
//***********************************
//- Maximum multiplier should be 2.0
//- Minimum multiplier should be 0.1
//- A 0.1 should be VERY rare.  The normal minimum is 0.2.  If a class should not even
//  be near a certain skill, that's when the 0.1 comes in.

//******** SUMMARY ******************
//- Primary skills use a 2.0 multiplier
//- Secondary skills use a 1.5 multiplier
//- Normal skills use a ~1.0 multiplier
//- Weak skills use a ~0.5 multiplier
//- VERY weak skills use a 0.2
//- Unsuitable skills for a specific class use a 0.1

//--------------
// Cleric
//--------------
// Clerics are good with Bludgeoning weapons but VERY good at healing spells.  They also
// know the basics behind offensive spells.

//Primary Skill: Defensive Casting
//Secondary Skills: Healing, Energy, Bludgeoning

$SkillMultiplier[Cleric, $SkillSlashing] = 0.6;
$SkillMultiplier[Cleric, $SkillPiercing] = 0.7;
$SkillMultiplier[Cleric, $SkillBludgeoning] = 1.5;
$SkillMultiplier[Cleric, $SkillDodging] = 0.7;
$SkillMultiplier[Cleric, $SkillWeightCapacity] = 1.0;
$SkillMultiplier[Cleric, $SkillBashing] = 0.5;
$SkillMultiplier[Cleric, $SkillStealing] = 0.2;
$SkillMultiplier[Cleric, $SkillHiding] = 0.2;
$SkillMultiplier[Cleric, $SkillBackstabbing] = 0.2;
$SkillMultiplier[Cleric, $SkillOffensiveCasting] = 0.9;
$SkillMultiplier[Cleric, $SkillDefensiveCasting] = 2.0;
$SkillMultiplier[Cleric, $SkillNeutralCasting] = 1.2;
$SkillMultiplier[Cleric, $SkillSpellResistance] = 1.5;
$SkillMultiplier[Cleric, $SkillHealing] = 2.0;
$SkillMultiplier[Cleric, $SkillArchery] = 0;
$SkillMultiplier[Cleric, $SkillEndurance] = 1.1;
$SkillMultiplier[Cleric, $SkillCriticals] = 0.5;
$SkillMultiplier[Cleric, $SkillSpeech] = 1.0;
$SkillMultiplier[Cleric, $SkillSenseHeading] = 1.0;
$SkillMultiplier[Cleric, $SkillEnergy] = 1.5;
$SkillMultiplier[Cleric, $SkillHaggling] = 1.0;
$EXPmultiplier[Cleric] = 3;

//--------------
// Druid
//--------------
// Druids are good with Bludgeoning weapons and are somewhat familiar with spells.  They specialize in Neutral casting.
// However they are also able to easily hide.

//Primary Skill: Neutral Casting
//Secondary Skill: Hiding, Slashing, Spell Resistance

$SkillMultiplier[Druid, $SkillSlashing] = 1.5;
$SkillMultiplier[Druid, $SkillPiercing] = 0.7;
$SkillMultiplier[Druid, $SkillBludgeoning] = 0.6;
$SkillMultiplier[Druid, $SkillDodging] = 2.0;
$SkillMultiplier[Druid, $SkillWeightCapacity] = 2.0;
$SkillMultiplier[Druid, $SkillBashing] = 0.5;
$SkillMultiplier[Druid, $SkillStealing] = 0.2;
$SkillMultiplier[Druid, $SkillHiding] = 2.0;
$SkillMultiplier[Druid, $SkillBackstabbing] = 0.5;
$SkillMultiplier[Druid, $SkillOffensiveCasting] = 0.7;
$SkillMultiplier[Druid, $SkillDefensiveCasting] = 0.7;
$SkillMultiplier[Druid, $SkillNeutralCasting] = 2.0;
$SkillMultiplier[Druid, $SkillSpellResistance] = 1.0;
$SkillMultiplier[Druid, $SkillHealing] = 1.3;
$SkillMultiplier[Druid, $SkillArchery] = 0;
$SkillMultiplier[Druid, $SkillEndurance] = 0.8;
$SkillMultiplier[Druid, $SkillCriticals] = 0.5;
$SkillMultiplier[Druid, $SkillSpeech] = 1.0;
$SkillMultiplier[Druid, $SkillSenseHeading] = 1.7;
$SkillMultiplier[Druid, $SkillEnergy] = 1.2;
$SkillMultiplier[Druid, $SkillHaggling] = 1.3;
$EXPmultiplier[Druid] = 3;

//--------------
// Thief
//--------------
//Thieves handle piercing weapons well enough, and are very good at hiding and backstabbing.
//And of course, they are great at stealing.

//Primary Skill: Stealing
//Secondary Skill: Hiding, Backstabbing, Piercing, Archery

$SkillMultiplier[Thief, $SkillSlashing] = 0.6;
$SkillMultiplier[Thief, $SkillPiercing] = 1.8;
$SkillMultiplier[Thief, $SkillBludgeoning] = 0.5;
$SkillMultiplier[Thief, $SkillDodging] = 1.1;
$SkillMultiplier[Thief, $SkillWeightCapacity] = 0.7;
$SkillMultiplier[Thief, $SkillBashing] = 0.2;
$SkillMultiplier[Thief, $SkillStealing] = 2.0;
$SkillMultiplier[Thief, $SkillHiding] = 2.0;
$SkillMultiplier[Thief, $SkillBackstabbing] = 2.0;
$SkillMultiplier[Thief, $SkillOffensiveCasting] = 0.2;
$SkillMultiplier[Thief, $SkillDefensiveCasting] = 0.2;
$SkillMultiplier[Thief, $SkillNeutralCasting] = 0.2;
$SkillMultiplier[Thief, $SkillSpellResistance] = 0.3;
$SkillMultiplier[Thief, $SkillHealing] = 0.5;
$SkillMultiplier[Thief, $SkillArchery] = 0;
$SkillMultiplier[Thief, $SkillEndurance] = 1.0;
$SkillMultiplier[Thief, $SkillCriticals] = 0.5;
$SkillMultiplier[Thief, $SkillSpeech] = 1.0;
$SkillMultiplier[Thief, $SkillSenseHeading] = 1.0;
$SkillMultiplier[Thief, $SkillEnergy] = 0.5;
$SkillMultiplier[Thief, $SkillHaggling] = 1.5;
$EXPmultiplier[Thief] = 3;

//--------------
// Bard
//--------------
//Bards are much like thieves, except that they are a bit more evenly balanced.

//Primary Skill: Stealing
//Secondary Skill: Archery

$SkillMultiplier[Bard, $SkillSlashing] = 1.3;
$SkillMultiplier[Bard, $SkillPiercing] = 1.6;
$SkillMultiplier[Bard, $SkillBludgeoning] = 1.3;
$SkillMultiplier[Bard, $SkillDodging] = 2.0;
$SkillMultiplier[Bard, $SkillWeightCapacity] = 0.8;
$SkillMultiplier[Bard, $SkillBashing] = 0.2;
$SkillMultiplier[Bard, $SkillStealing] = 2.0;
$SkillMultiplier[Bard, $SkillHiding] = 1.8;
$SkillMultiplier[Bard, $SkillBackstabbing] = 1.8;
$SkillMultiplier[Bard, $SkillOffensiveCasting] = 0.3;
$SkillMultiplier[Bard, $SkillDefensiveCasting] = 0.3;
$SkillMultiplier[Bard, $SkillNeutralCasting] = 0.6;
$SkillMultiplier[Bard, $SkillSpellResistance] = 0.5;
$SkillMultiplier[Bard, $SkillHealing] = 2.0;
$SkillMultiplier[Bard, $SkillArchery] = 0;
$SkillMultiplier[Bard, $SkillEndurance] = 2.0;
$SkillMultiplier[Bard, $SkillCriticals] = 0.5;
$SkillMultiplier[Bard, $SkillSpeech] = 1.0;
$SkillMultiplier[Bard, $SkillSenseHeading] = 1.5;
$SkillMultiplier[Bard, $SkillEnergy] = 0.6;
$SkillMultiplier[Bard, $SkillHaggling] = 2.0;
$EXPmultiplier[Bard] = 3;

//--------------
// Fighter
//--------------
// Fighters are great with swords, namely slashing weapons.  They are strong, but dumb.
// They know nothing when it comes to spells.  However they can easily wear armor and
// wield all kinds of weapons.

//Primary Skill: Slashing
//Secondary Skill: Bludgeoning

$SkillMultiplier[Fighter, $SkillSlashing] = 2.0;
$SkillMultiplier[Fighter, $SkillPiercing] = 2.0;
$SkillMultiplier[Fighter, $SkillBludgeoning] = 2.0;
$SkillMultiplier[Fighter, $SkillDodging] = 1.5;
$SkillMultiplier[Fighter, $SkillWeightCapacity] = 1.5;
$SkillMultiplier[Fighter, $SkillBashing] = 1.6;
$SkillMultiplier[Fighter, $SkillStealing] = 0.2;
$SkillMultiplier[Fighter, $SkillHiding] = 0.2;
$SkillMultiplier[Fighter, $SkillBackstabbing] = 0.2;
$SkillMultiplier[Fighter, $SkillOffensiveCasting] = 0.1;
$SkillMultiplier[Fighter, $SkillDefensiveCasting] = 0.1;
$SkillMultiplier[Fighter, $SkillNeutralCasting] = 0.1;
$SkillMultiplier[Fighter, $SkillSpellResistance] = 0.2;
$SkillMultiplier[Fighter, $SkillHealing] = 1.2;
$SkillMultiplier[Fighter, $SkillArchery] = 0;
$SkillMultiplier[Fighter, $SkillEndurance] = 1.6;
$SkillMultiplier[Fighter, $SkillCriticals] = 0.5;
$SkillMultiplier[Fighter, $SkillSpeech] = 0.8;
$SkillMultiplier[Fighter, $SkillSenseHeading] = 0.4;
$SkillMultiplier[Fighter, $SkillEnergy] = 0.2;
$SkillMultiplier[Fighter, $SkillHaggling] = 1.0;
$EXPmultiplier[Fighter] = 3;

//--------------
// Paladin
//--------------
//Paladins are much like Fighters, except that they are a bit more evenly balanced.

//Primary Skill: Bludgeoning
//Secondary Skill: Healing

$SkillMultiplier[Paladin, $SkillSlashing] = 1.5;
$SkillMultiplier[Paladin, $SkillPiercing] = 1.5;
$SkillMultiplier[Paladin, $SkillBludgeoning] = 1.2;
$SkillMultiplier[Paladin, $SkillDodging] = 1.5;
$SkillMultiplier[Paladin, $SkillWeightCapacity] = 1.5;
$SkillMultiplier[Paladin, $SkillBashing] = 1.5;
$SkillMultiplier[Paladin, $SkillStealing] = 0.3;
$SkillMultiplier[Paladin, $SkillHiding] = 0.3;
$SkillMultiplier[Paladin, $SkillBackstabbing] = 0.3;
$SkillMultiplier[Paladin, $SkillOffensiveCasting] = 0.4;
$SkillMultiplier[Paladin, $SkillDefensiveCasting] = 1.2;
$SkillMultiplier[Paladin, $SkillNeutralCasting] = 1.0;
$SkillMultiplier[Paladin, $SkillSpellResistance] = 0.9;
$SkillMultiplier[Paladin, $SkillHealing] = 1.3;
$SkillMultiplier[Paladin, $SkillArchery] = 0;
$SkillMultiplier[Paladin, $SkillEndurance] = 1.5;
$SkillMultiplier[Paladin, $SkillCriticals] = 0.5;
$SkillMultiplier[Paladin, $SkillSpeech] = 0.8;
$SkillMultiplier[Paladin, $SkillSenseHeading] = 0.5;
$SkillMultiplier[Paladin, $SkillEnergy] = 0.9;
$SkillMultiplier[Paladin, $SkillHaggling] = 1.3;
$EXPmultiplier[Paladin] = 3;

//--------------
// Ranger
//--------------
// Rangers specialize in ranged weaponry.  They are also good at finding their way when lost.
// They can also wear armors and wield weapons easily enough.

//Primary Skill: Archery
//Secondary Skills: Slashing, Sense Heading

$SkillMultiplier[Squire, $SkillSlashing] = 1.0;
$SkillMultiplier[Squire, $SkillPiercing] = 1.0;
$SkillMultiplier[Squire, $SkillBludgeoning] = 1.0;
$SkillMultiplier[Squire, $SkillDodging] = 1.5;
$SkillMultiplier[Squire, $SkillWeightCapacity] = 1.5;
$SkillMultiplier[Squire, $SkillBashing] = 0.9;
$SkillMultiplier[Squire, $SkillStealing] = 0.3;
$SkillMultiplier[Squire, $SkillHiding] = 0.3;
$SkillMultiplier[Squire, $SkillBackstabbing] = 0.2;
$SkillMultiplier[Squire, $SkillOffensiveCasting] = 1.0;
$SkillMultiplier[Squire, $SkillDefensiveCasting] = 0.4;
$SkillMultiplier[Squire, $SkillNeutralCasting] = 1.0;
$SkillMultiplier[Squire, $SkillSpellResistance] = 0.9;
$SkillMultiplier[Squire, $SkillHealing] = 1.3;
$SkillMultiplier[Squire, $SkillArchery] = 0;
$SkillMultiplier[Squire, $SkillEndurance] = 1.5;
$SkillMultiplier[Squire, $SkillCriticals] = 0.5;
$SkillMultiplier[Squire, $SkillSpeech] = 1.0;
$SkillMultiplier[Squire, $SkillSenseHeading] = 2.0;
$SkillMultiplier[Squire, $SkillEnergy] = 1.6;
$SkillMultiplier[Squire, $SkillHaggling] = 0.8;
$EXPmultiplier[Squire] = 3;

//--------------
// Mage
//--------------
// Mages are horrible with weapons and armor, but excel in anything that
// relates to spells.

//Primary Skill: Offensive Casting
//Secondary Skills: Energy

$SkillMultiplier[Mage, $SkillSlashing] = 0.3;
$SkillMultiplier[Mage, $SkillPiercing] = 0.9;
$SkillMultiplier[Mage, $SkillBludgeoning] = 0.3;
$SkillMultiplier[Mage, $SkillDodging] = 1.2;
$SkillMultiplier[Mage, $SkillWeightCapacity] = 0.6;
$SkillMultiplier[Mage, $SkillBashing] = 0.2;
$SkillMultiplier[Mage, $SkillStealing] = 0.2;
$SkillMultiplier[Mage, $SkillHiding] = 0.2;
$SkillMultiplier[Mage, $SkillBackstabbing] = 0.2;
$SkillMultiplier[Mage, $SkillOffensiveCasting] = 2.0;
$SkillMultiplier[Mage, $SkillDefensiveCasting] = 1.0;
$SkillMultiplier[Mage, $SkillNeutralCasting] = 1.8;
$SkillMultiplier[Mage, $SkillSpellResistance] = 1.5;
$SkillMultiplier[Mage, $SkillHealing] = 0.7;
$SkillMultiplier[Mage, $SkillArchery] = 0;
$SkillMultiplier[Mage, $SkillEndurance] = 0.7;
$SkillMultiplier[Mage, $SkillCriticals] = 0.5;
$SkillMultiplier[Mage, $SkillSpeech] = 1.2;
$SkillMultiplier[Mage, $SkillSenseHeading] = 0.7;
$SkillMultiplier[Mage, $SkillEnergy] = 2.0;
$SkillMultiplier[Mage, $SkillHaggling] = 1.0;
$EXPmultiplier[Mage] = 3;

//--------------
// Red Flag
//--------------
// Clerics are good with Bludgeoning weapons but VERY good at healing spells.  They also
// know the basics behind offensive spells.

//For hacking set a player to this or anything that u dont like
//that they do. or if u just dont like them period..this is a bad class lol

$SkillMultiplier[RedFlag, $SkillSlashing] = 0.1;
$SkillMultiplier[RedFlag, $SkillPiercing] = 0.1;
$SkillMultiplier[RedFlag, $SkillBludgeoning] = 0.1;
$SkillMultiplier[RedFlag, $SkillDodging] = 0.1;
$SkillMultiplier[RedFlag, $SkillWeightCapacity] = 0.1;
$SkillMultiplier[RedFlag, $SkillBashing] = 0.1;
$SkillMultiplier[RedFlag, $SkillStealing] = 0.1;
$SkillMultiplier[RedFlag, $SkillHiding] = 0.1;
$SkillMultiplier[RedFlag, $SkillBackstabbing] = 0.1;
$SkillMultiplier[RedFlag, $SkillOffensiveCasting] = 0.1;
$SkillMultiplier[RedFlag, $SkillDefensiveCasting] = 0.1;
$SkillMultiplier[RedFlag, $SkillNeutralCasting] = 0.1;
$SkillMultiplier[RedFlag, $SkillSpellResistance] = 0.1;
$SkillMultiplier[RedFlag, $SkillHealing] = 0.1;
$SkillMultiplier[RedFlag, $SkillArchery] = 0;
$SkillMultiplier[RedFlag, $SkillEndurance] = 0.1;
$SkillMultiplier[RedFlag, $SkillCriticals] = 0.1;
$SkillMultiplier[RedFlag, $SkillSpeech] = 0.1;
$SkillMultiplier[RedFlag, $SkillSenseHeading] = 0.1;
$SkillMultiplier[RedFlag, $SkillEnergy] = 0.1;
$SkillMultiplier[RedFlag, $SkillHaggling] = 0.1;
$EXPmultiplier[RedFlag] = 2;

//######################################################################################
// Skill Restriction tables
//######################################################################################

//To determine skill restrictions, do the following:
//
//-Determine the following variables first:
//	(weapon):
//	a = ATK * 1.1 (archery is 0.75)
//	b = Delay = Cap((Weight / 3), 1, "inf")
//
//	(armor):
//	a = (DEF + MDEF) / 6
//	b = 1.0
//
//-To find out what the skill restriction number is, follow this formula, where s is the final skill restriction:
//	s = Cap((a / b) - 20), 0, "inf") * 10.0;
//

$SkillRestriction[BluePotion] = $SkillHealing @ " 0";
$SkillRestriction[CrystalBluePotion] = $SkillHealing @ " 0";

$SkillRestriction[CheetaursPaws] = $MinLevel @ " 8";
$SkillRestriction[BootsOfGliding] = $MinLevel @ " 25";
$SkillRestriction[WindWalkers] = $MinLevel @ " 40";
$SkillRestriction[WindPaws] = $MinLevel @ " 60";
//Custom armor restricts
$SkillRestriction[RatSkinShirt] = $SkillEndurance @ " 0";
$SkillRestriction[LeatherShirt] = $SkillEndurance @ " 45";
$SkillRestriction[LeatherSuit] = $SkillEndurance @ " 90";
$SkillRestriction[StuddedLeatherSuit] = $SkillEndurance @ " 140";
$SkillRestriction[ToughHideSuit] = $SkillEndurance @ " 190";
$SkillRestriction[IronScaleMail] = $SkillEndurance @ " 240";
$SkillRestriction[SteelScaleMail] = $SkillEndurance @ " 310";
$SkillRestriction[SteelBrigandineMail] = $SkillEndurance @ " 380";
$SkillRestriction[GoldenBrigandineMail] = $SkillEndurance @ " 440";
$SkillRestriction[GoldenChainMail] = $SkillEndurance @ " 510";
$SkillRestriction[CrystalChainMail] = $SkillEndurance @ " 580";
$SkillRestriction[CrystalRingMail] = $SkillEndurance @ " 650";
$SkillRestriction[CrystalBandedMail] = $SkillEndurance @ " 720";
$SkillRestriction[CrystalSplintMail] = $SkillEndurance @ " 800";
$SkillRestriction[TungstenSplintMail] = $SkillEndurance @ " 870";
$SkillRestriction[TungstenPlateMail] = $SkillEndurance @ " 940";
$SkillRestriction[DiamondPlateMail] = $SkillEndurance @ " 1010";
$SkillRestriction[DiamondFieldPlate] = $SkillEndurance @ " 1080";
$SkillRestriction[DiamondFullPlate] = $SkillEndurance @ " 1250";
$SkillRestriction[BlackDiamondFullPlate] = $SkillEndurance @ " 1550";

$SkillRestriction[IronHelmet] = $SkillEndurance @ " 180";
$SkillRestriction[GoldenHelmet] = $SkillEndurance @ " 380";
$SkillRestriction[CrystalHelmet] = $SkillEndurance @ " 620";
$SkillRestriction[DiamondHelmet] = $SkillEndurance @ " 900";
$SkillRestriction[BlackDiamondHelmet] = $SkillEndurance @ " 1300";

$SkillRestriction[AntiMagicBelt] = $SkillEndurance @ " 500";
$SkillRestriction[MajorAntiMagicBelt] = $SkillEndurance @ " 920";
$SkillRestriction[ExtremeAntiMagicBelt] = $SkillEndurance @ " 1340";
$SkillRestriction[GodlyAntiMagicBelt] = $SkillEndurance @ " 2700";

$SkillRestriction[SteelKnightShield] = $SkillEndurance @ " 300";
$SkillRestriction[CrystalKnightShield] = $SkillEndurance @ " 600";
$SkillRestriction[DiamondKnightShield] = $SkillEndurance @ " 1100";
$SkillRestriction[BlackDiamondKnightShield] = $SkillEndurance @ " 3100";

$SkillRestriction[Hatchet] = $MinRemort @ " 5";
//Custom weap restricts
$SkillRestriction[RustyIronBlade] = $SkillSlashing @ " 0";
$SkillRestriction[SharpIronBlade] = $SkillSlashing @ " 45";
$SkillRestriction[IronBroadSword] = $SkillSlashing @ " 90";
$SkillRestriction[SteelBroadSword] = $SkillSlashing @ " 140";
$SkillRestriction[SteelLongSword] = $SkillSlashing @ " 200";
$SkillRestriction[GoldenLongSword] = $SkillSlashing @ " 260";
$SkillRestriction[GoldenBastardSword] = $SkillSlashing @ " 340";
$SkillRestriction[CrystalBastardSword] = $SkillSlashing @ " 440";
$SkillRestriction[TemperedCrystalBastardSword] = $SkillSlashing @ " 540";
$SkillRestriction[CrystalClaymore] = $SkillSlashing @ " 640";
$SkillRestriction[DiamondClaymore] = $SkillSlashing @ " 740";
$SkillRestriction[DiamondLegendSword] = $SkillSlashing @ " 840 " @ $MinRemort @ " 1";
$SkillRestriction[BlackDiamondDreamSword] = $SkillSlashing @ " 1200 " @ $MinRemort @ " 10";
$SkillRestriction[BlackDiamondAtomSplitter] = $SkillSlashing @ " 1580 " @ $MinRemort @ " 30";
//.................................................................................
$SkillRestriction[Club] = $MinRemort @ " 5";
//Custom weap restricts
$SkillRestriction[CrackedStick] = $SkillBludgeoning @ " 0";
$SkillRestriction[IronStick] = $SkillBludgeoning @ " 45";
$SkillRestriction[IronMace] = $SkillBludgeoning @ " 90";
$SkillRestriction[SteelMace] = $SkillBludgeoning @ " 140";
$SkillRestriction[SteelHammer] = $SkillBludgeoning @ " 200";
$SkillRestriction[SteelWarHammer] = $SkillBludgeoning @ " 260";
$SkillRestriction[GoldenWarHammer] = $SkillBludgeoning @ " 340";
$SkillRestriction[GoldenDivineMace] = $SkillBludgeoning @ " 440";
$SkillRestriction[CrystalDivineMace] = $SkillBludgeoning @ " 540";
$SkillRestriction[DiamondDivineMace] = $SkillBludgeoning @ " 640";
$SkillRestriction[DiamondBrainSpiller] = $SkillBludgeoning @ " 740";
$SkillRestriction[DiamondLegendMace] = $SkillBludgeoning @ " 840 " @ $MinRemort @ " 1";
$SkillRestriction[BlackDiamondDreamMace] = $SkillBludgeoning @ " 1200 " @ $MinRemort @ " 10";
$SkillRestriction[BlackDiamondAtomSmasher] = $SkillBludgeoning @ " 1580 " @ $MinRemort @ " 30";
//.................................................................................
$SkillRestriction[PickAxe] = $SkillPiercing @ " 0";
$SkillRestriction[Knife] = $MinRemort @ " 5";
//Custom weap restricts
$SkillRestriction[ButterKnife] = $SkillPiercing @ " 0";
$SkillRestriction[LongKnife] = $SkillPiercing @ " 45";
$SkillRestriction[IronSpear] = $SkillPiercing @ " 90";
$SkillRestriction[SteelSpear] = $SkillPiercing @ " 140";
$SkillRestriction[SteelPike] = $SkillPiercing @ " 200";
$SkillRestriction[GoldenPike] = $SkillPiercing @ " 260";
$SkillRestriction[CrystalPike] = $SkillPiercing @ " 340";
$SkillRestriction[CrystalTrident] = $SkillPiercing @ " 440";
$SkillRestriction[TemperedCrystalTrident] = $SkillPiercing @ " 540";
$SkillRestriction[DiamondTrident] = $SkillPiercing @ " 640";
$SkillRestriction[DiamondDeathSpear] = $SkillPiercing @ " 740";
$SkillRestriction[DiamondLegendSpear] = $SkillPiercing @ " 840 " @ $MinRemort @ " 1";
$SkillRestriction[BlackDiamondDreamSpear] = $SkillPiercing @ " 1200 " @ $MinRemort @ " 10";
$SkillRestriction[BlackDiamondAtomPiercer] = $SkillPiercing @ " 1580 " @ $MinRemort @ " 30";
//.................................................................................

//.................................................................................




// Chat functions
$SkillRestriction["#say"] = $SkillSpeech @ " 0";
$SkillRestriction["#shout"] = $SkillSpeech @ " 0";
$SkillRestriction["#whisper"] = $SkillSpeech @ " 0";
$SkillRestriction["#tell"] = $SkillSpeech @ " 0";
$SkillRestriction["#global"] = $SkillSpeech @ " 0";
$SkillRestriction["#house"] = $SkillSpeech @ " 10";
$SkillRestriction["#zone"] = $SkillSpeech @ " 0";
$SkillRestriction["#zonedis"] = $MinRemort @ " 1";
$SkillRestriction["#group"] = $SkillSpeech @ " 6";
$SkillRestriction["#party"] = $SkillSpeech @ " 0";
$SkillRestriction["#steal"] = $SkillStealing @ " 15";
$SkillRestriction["#pickpocket"] = $SkillStealing @ " 270";
$SkillRestriction["#mug"] = $SkillStealing @ " 620";
$SkillRestriction["#compass"] = $SkillSenseHeading @ " 3";
$SkillRestriction["#track"] = $SkillSenseHeading @ " 15";
$SkillRestriction["#trackpack"] = $SkillSenseHeading @ " 85";
$SkillRestriction["#hide"] = $SkillHiding @ " 15";
$SkillRestriction["#bash"] = $SkillBashing @ " 15";
$SkillRestriction["#shove"] = $SkillBashing @ " 5";
$SkillRestriction["#zonelist"] = $SkillSenseHeading @ " 45";
$SkillRestriction["#advcompass"] = $SkillSenseHeading @ " 20";

// Spells
$SkillRestriction[thorn] = $SkillOffensiveCasting @ " 15";
$SkillRestriction[fireball] = $SkillOffensiveCasting @ " 20";
$SkillRestriction[firebomb] = $SkillOffensiveCasting @ " 35";
$SkillRestriction[icespike] = $SkillOffensiveCasting @ " 45";
$SkillRestriction[boom] = $SkillOffensiveCasting @ " 65";
$SkillRestriction[icestorm] = $SkillOffensiveCasting @ " 85";
$SkillRestriction[ironfist] = $SkillOffensiveCasting @ " 110";
$SkillRestriction[cloud] = $SkillOffensiveCasting @ " 145";
$SkillRestriction[melt] = $SkillOffensiveCasting @ " 220";
$SkillRestriction[powercloud] = $SkillOffensiveCasting @ " 340";
$SkillRestriction[hellstorm] = $SkillOffensiveCasting @ " 420";
$SkillRestriction[beam] = $SkillOffensiveCasting @ " 520";
$SkillRestriction[bullet] = $SkillOffensiveCasting @ " 600";
$SkillRestriction[freezerburn] = $SkillOffensiveCasting @ " 650 " @ $MinRemort @ " 1";
$SkillRestriction[dimensionrift] = $SkillOffensiveCasting @ " 750";
$SkillRestriction[nuke] = $SkillOffensiveCasting @ " 900 " @ $MinRemort @ " 1";
$SkillRestriction[tornado] = $SkillOffensiveCasting @ " 1000 " @ $MinRemort @ " 3";
$SkillRestriction[apocalypse] = $SkillOffensiveCasting @ " 1300 " @ $MinRemort @ " 10";
$SkillRestriction[ionblast] = $SkillOffensiveCasting @ " 2000 " @ $MinRemort @ " 30";
$SkillRestriction[shredder] = $SkillOffensiveCasting @ " 2500 " @ $MinRemort @ " 50";
$SkillRestriction[terminate] = $SkillOffensiveCasting @ " 3000 " @ $MinRemort @ " 100";
$SkillRestriction[snipe] = $SkillOffensiveCasting @ " 3200 " @ $MinRemort @ " 150";

$SkillRestriction[stop] = $SkillNeutralCasting @ " 20";
$SkillRestriction[airfist] = $SkillNeutralCasting @ " 60";
$SkillRestriction[teleport] = $SkillNeutralCasting @ " 60";
$SkillRestriction[lightstep] = $SkillNeutralCasting @ " 120";
$SkillRestriction[transport] = $SkillNeutralCasting @ " 200";
$SkillRestriction[boost] = $SkillNeutralCasting @ " 300";
$SkillRestriction[advtransport] = $SkillNeutralCasting @ " 350";
$SkillRestriction[advshove] = $SkillNeutralCasting @ " 450";
$SkillRestriction[heavystep] = $SkillNeutralCasting @ " 700";
$SkillRestriction[remort] = $SkillNeutralCasting @ " 0 " @ $MinLevel @ " 100";
$SkillRestriction[mimic] = $SkillNeutralCasting @ " 145 " @ $MinRemort @ " 2";
$SkillRestriction[masstransport] = $SkillNeutralCasting @ " 650 " @ $MinRemort @ " 1";

$SkillRestriction[heal] = $SkillDefensiveCasting @ " 10";
$SkillRestriction[advheal1] = $SkillDefensiveCasting @ " 80";
$SkillRestriction[advheal2] = $SkillDefensiveCasting @ " 110";
$SkillRestriction[advheal3] = $SkillDefensiveCasting @ " 200";
$SkillRestriction[advheal4] = $SkillDefensiveCasting @ " 320";
$SkillRestriction[advheal5] = $SkillDefensiveCasting @ " 400";
$SkillRestriction[advheal6] = $SkillDefensiveCasting @ " 500";
$SkillRestriction[godlyheal] = $SkillDefensiveCasting @ " 600";
$SkillRestriction[fullheal] = $SkillDefensiveCasting @ " 750";
$SkillRestriction[massheal] = $SkillDefensiveCasting @ " 850 " @ $MinRemort @ " 2";
$SkillRestriction[massfullheal] = $SkillDefensiveCasting @ " 950 " @ $MinRemort @ " 3";
$SkillRestriction[shield] = $SkillDefensiveCasting @ " 20";
$SkillRestriction[advshield1] = $SkillDefensiveCasting @ " 60";
$SkillRestriction[advshield2] = $SkillDefensiveCasting @ " 140";
$SkillRestriction[advshield3] = $SkillDefensiveCasting @ " 290";
$SkillRestriction[advshield4] = $SkillDefensiveCasting @ " 420";
$SkillRestriction[advshield5] = $SkillDefensiveCasting @ " 635";
$SkillRestriction[massshield] = $SkillDefensiveCasting @ " 680";
$SkillRestriction[godlyshield] = $SkillDefensiveCasting @ " 720";
$SkillRestriction[airblast] = $SkillNeutralCasting @ " 900 " @ $MinRemort @ " 5";
$SkillRestriction[airwarp] = $SkillNeutralCasting @ " 1100 " @ $MinRemort @ " 25";
//Jobo's New spells
$SkillRestriction[shieldplus1] = $SkillDefensiveCasting @ " 900 " @ $MinRemort @ " 5";
$SkillRestriction[shieldplus2] = $SkillDefensiveCasting @ " 1100 " @ $MinRemort @ " 15";
$SkillRestriction[shieldplus3] = $SkillDefensiveCasting @ " 1300 " @ $MinRemort @ " 30";
$SkillRestriction[shieldplus4] = $SkillDefensiveCasting @ " 1500 " @ $MinRemort @ " 50";
$SkillRestriction[shieldplus5] = $SkillDefensiveCasting @ " 1750 " @ $MinRemort @ " 100";
$SkillRestriction[shieldplus6] = $SkillDefensiveCasting @ " 2000 " @ $MinRemort @ " 150";
$SkillRestriction[healplus1] = $SkillDefensiveCasting @ " 900 " @ $MinRemort @ " 5";
$SkillRestriction[healplus2] = $SkillDefensiveCasting @ " 1100 " @ $MinRemort @ " 15";
$SkillRestriction[healplus3] = $SkillDefensiveCasting @ " 1300 " @ $MinRemort @ " 30";
$SkillRestriction[healplus4] = $SkillDefensiveCasting @ " 1500 " @ $MinRemort @ " 50";
$SkillRestriction[healplus5] = $SkillDefensiveCasting @ " 1750 " @ $MinRemort @ " 100";
$SkillRestriction[healplus6] = $SkillDefensiveCasting @ " 2000 " @ $MinRemort @ " 150";




//######################################################################################
// Skill functions
//######################################################################################

function GetNumSkills()
{
	dbecho($dbechoMode, "GetNumSkills()");

	for(%i = 1; $SkillDesc[%i] != ""; %i++){}
	return %i-1;
}

function AddSkillPoint(%clientId, %skill, %delta)
{
	dbecho($dbechoMode, "AddSkillPoint(" @ %clientId @ ", " @ %skill @ ", " @ %delta @ ")");

	if(%delta == "")
		%delta = 1;

	////////temporary/////////////////////////
	if(%skill == 14 || %skill == 17)	//weapon handling
		return False;

	if(%skill == 16)	//critical attacks
	{
		if($PlayerSkill[%clientId, $SkillCriticals] >= 1000)
		{
			$PlayerSkill[%clientId, $SkillCriticals] = 1000;
			return false;
		}
	}
	if(%skill == 21)	//haggle
	{
		if($PlayerSkill[%clientId, $SkillHaggling] >= 1000)
		{
			$PlayerSkill[%clientId, $SkillHaggling] = 1000;
			return false;
		}
	}

	//////////////////////////////////////////

	//==== CAPS ================
	//if($PlayerSkill[%clientId, %skill] >= $SkillCap)
	//	return False;

	%ub = ($skillRangePerLevel * fetchData(%clientId, "LVL")) + 20 + (fetchData(%clientId, "RemortStep") * 2);
	if($PlayerSkill[%clientId, %skill] >= %ub)
		return False;
	//==========================
	if(%skill == 16)
	{
		%a = $SkillMultiplier[fetchData(%clientId, "CLASS"), $SkillCriticals];
		%b = $PlayerSkill[%clientId, %skill];
		%c = %a + %b;
		%d = round(%c * 10);
		%e = (%d / 10) * 1.000001;

		$PlayerSkill[%clientId, %skill] = %e;

		return True;
	}
	else
	{
		%a = GetSkillMultiplier(%clientId, %skill) * %delta;
		%b = $PlayerSkill[%clientId, %skill];
		%c = %a + %b;
		%d = round(%c * 10);
		%e = (%d / 10) * 1.000001;

		$PlayerSkill[%clientId, %skill] = %e;

		return True;
	}
}
function GetPlayerSkill(%clientId, %skill)
{
	return $PlayerSkill[%clientId, %skill];
}
function GetSkillMultiplier(%clientId, %skill)
{
	dbecho($dbechoMode, "GetSkillMultiplier(" @ %clientId @ ", " @ %skill @ ")");

	%a = $SkillMultiplier[fetchData(%clientId, "CLASS"), %skill];
	%b = fetchData(%clientId, "RemortStep") * 0.1;

	%c = Cap(%a + %b, "inf", 30.0);

	return FixDecimals(%c);
}
function GetEXPmultiplier(%clientId)
{
	dbecho($dbechoMode, "GetEXPmultiplier(" @ %clientId @ ")");

	%a = $EXPmultiplier[fetchData(%clientId, "CLASS")];
	%b = fetchData(%clientId, "RemortStep") * 0.5;

	%c = %a + %b;

	return FixDecimals(%c);
}

function SetAllSkills(%clientId, %n)
{
	dbecho($dbechoMode, "SetAllSkills(" @ %clientId @ ", " @ %n @ ")");

	for(%i = 1; $SkillDesc[%i] != ""; %i++)
		$PlayerSkill[%clientId, %i] = %n;
}

function SkillCanUse(%clientId, %thing)
{
	dbecho($dbechoMode, "SkillCanUse(" @ %clientId @ ", " @ %thing @ ")");

	if(%clientId.adminLevel >= 5)
		return True;

	%flag = 0;
	%gc = 0;
	%gcflag = 0;
	for(%i = 0; GetWord($SkillRestriction[%thing], %i) != -1; %i+=2)
	{
		%s = GetWord($SkillRestriction[%thing], %i);
		%n = GetWord($SkillRestriction[%thing], %i+1);

		if(%s == "L")
		{
			if(fetchData(%clientId, "LVL") < %n)
				%flag = 1;
		}
		else if(%s == "R")
		{
			if(fetchData(%clientId, "RemortStep") < %n)
				%flag = 1;
		}
		else if(%s == "A")
		{
			if(%clientId.adminLevel < %n)
				%flag = 1;
		}
		else if(%s == "G")
		{
			%gcflag++;
			if(String::ICompare(fetchData(%clientId, "GROUP"), %n) == 0)
				%gc = 1;
		}
		else if(%s == "C")
		{
			%gcflag++;
			if(String::ICompare(fetchData(%clientId, "CLASS"), %n) == 0)
				%gc = 1;
		}
		else if(%s == "H")
		{
			%hflag++;
			if(String::ICompare(fetchData(%clientId, "MyHouse"), %n) == 0)
				%hh = 1;
		}
		else
		{
			if($PlayerSkill[%clientId, %s] < %n)
				%flag = 1;
		}
	}

	//First, if there are any class/group restrictions, house restrictions, check these first.
	if(%gcflag > 0)
	{
		if(%gc == 0)
			%flag = 1;
	}
	if(%hflag > 0)
	{
		if(%hh == 0)
			%flag = 1;
	}

	
	if(%flag != 1)
		return True;
	else
		return False;
}

function UseSkill(%clientId, %skilltype, %successful, %showmsg, %base, %refreshall)
{
	dbecho($dbechoMode, "UseSkill(" @ %clientId @ ", " @ %skilltype @ ", " @ %successful @ ", " @ %showmsg @ ", " @ %base @ ", " @ %refreshall @ ")");

	if(%base == "") %base = 30;

	%ub = ($skillRangePerLevel * fetchData(%clientId, "LVL")) + 20 + (fetchData(%clientId, "RemortStep") * 2);
	if($PlayerSkill[%clientId, %skilltype] < %ub)
	{
		if(%successful)
			$SkillCounter[%clientId, %skilltype] += 1;
		else
			$SkillCounter[%clientId, %skilltype] += 0.3;

		%p = 1 - ($PlayerSkill[%clientId, %skilltype] / 1150);
		%e = round( (%base / GetSkillMultiplier(%clientId, %skilltype)) * %p );

		if($SkillCounter[%clientId, %skilltype] >= %e)
		{
			$SkillCounter[%clientId, %skilltype] = 0;
			%retval = AddSkillPoint(%clientId, %skilltype, 1);

			if(%retval)
			{
				if(%showmsg)
					Client::sendMessage(%clientId, $MsgBeige, "You have increased your skill in " @ $SkillDesc[%skilltype] @ " (" @ $PlayerSkill[%clientId, %skilltype] @ ")");
				if(%refreshall)
					RefreshAll(%clientId);
			}
		}
	}
}

function WhatSkills(%thing)
{
	dbecho($dbechoMode, "WhatSkills(" @ %thing @ ")");

	%t = "";
	for(%i = 0; GetWord($SkillRestriction[%thing], %i) != -1; %i+=2)
	{
		%s = GetWord($SkillRestriction[%thing], %i);
		%n = GetWord($SkillRestriction[%thing], %i+1);

		%t = %t @ $SkillDesc[%s] @ ": " @ %n @ ", ";
	}
	if(%t == "")
		%t = "None";
	else
		%t = String::getSubStr(%t, 0, String::len(%t)-2);
	
	return %t;
}

function GetSkillAmount(%thing, %skill)
{
	dbecho($dbechoMode, "GetSkillAmount(" @ %thing @ ", " @ %skill @ ")");

	for(%i = 0; GetWord($SkillRestriction[%thing], %i) != -1; %i+=2)
	{
		%s = GetWord($SkillRestriction[%thing], %i);

		if(%s == %skill)
			return GetWord($SkillRestriction[%thing], %i+1);
	}
	return 0;
}