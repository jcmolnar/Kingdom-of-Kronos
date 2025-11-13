//Data for spells are defined in the SPELL DEFINITIONS section.  Unfortunately, not everything can be designed there
//(ie, special effects etc) so for the great majority, the spells are coded in the DoCastSpell function.  This method
//isn't the ideal one... maybe one day i'll fix that if the need comes along.

//-- SPELL TYPES (not in use due to hardcoding of spells...) -----------------------------------------------------

$SelfType = 1;				//casts only to self
$LOSType = 2;				//casts only at LOS

$SelfRadiusType = 3;			//casts to self and around self
$LOSRadiusType = 4;			//casts at LOS and around LOS

$SelfRadiusLOSType = 5;			//casts to self and around self and at LOS
$LOSRadiusSelfType = 6;			//casts at LOS and around LOS and to self

$SelfRadiusLOSRadiusType = 7;		//casts to self and around self and to LOS and around LOS

//-- SPELL DEFINITIONS -------------------------------------------------------------------------------------------

$Spell::keyword[1] = "firebomb";
$Spell::index[firebomb] = 1;
$Spell::name[1] = "Fire Bomb From Hell";
$Spell::description[1] = "Casts an explosive.";
$Spell::delay[1] = 1;
$Spell::recoveryTime[1] = 2;
$Spell::radius[1] = 10;
$Spell::damageValue[1] = "55";
$Spell::LOSrange[1] = 80;
$Spell::manaCost[1] = 6;
$Spell::startSound[1] = ActivateBF;
$Spell::endSound[1] = ExplodeLM;
$Spell::groupListCheck[1] = False;
$Spell::refVal[1] = 55;
$Spell::graceDistance[1] = 2;
$SkillType[firebomb] = $SkillOffensiveCasting;

$Spell::keyword[2] = "teleport";
$Spell::index[teleport] = 2;
$Spell::name[2] = "Teleport close to nearest zone";
$Spell::description[2] = "Teleports you near a zone";
$Spell::delay[2] = 3.5;
$Spell::recoveryTime[2] = 16.5;
$Spell::manaCost[2] = 8;
$Spell::startSound[2] = Portal11;
$Spell::endSound[2] = ActivateCH;
$Spell::groupListCheck[2] = False;
$Spell::refVal[2] = 0;
$Spell::graceDistance[2] = 2;
$SkillType[teleport] = $SkillNeutralCasting;

$Spell::keyword[3] = "transport";
$Spell::index[transport] = 3;
$Spell::name[3] = "Transport to zone";
$Spell::description[3] = "Transports to a specific zone";
$Spell::delay[3] = 4.0;
$Spell::recoveryTime[3] = 1;
$Spell::manaCost[3] = 12;
$Spell::startSound[3] = RespawnB;
$Spell::endSound[3] = ActivateCH;
$Spell::groupListCheck[3] = False;
$Spell::refVal[3] = 0;
$Spell::graceDistance[3] = 2;
$SkillType[transport] = $SkillNeutralCasting;

$Spell::keyword[4] = "advtransport";
$Spell::index[advtransport] = 4;
$Spell::name[4] = "Advanced Transport to zone";
$Spell::description[4] = "Transports self OR person in line-of-sight to a specific zone";
$Spell::delay[4] = 4.0;
$Spell::recoveryTime[4] = 27;
$Spell::LOSrange[4] = 500;
$Spell::manaCost[4] = 16;
$Spell::startSound[4] = RespawnB;
$Spell::endSound[4] = ActivateCH;
$Spell::groupListCheck[4] = True;
$Spell::refVal[4] = 0;
$Spell::graceDistance[4] = 2;
$SkillType[advtransport] = $SkillNeutralCasting;

$Spell::keyword[5] = "cloud";
$Spell::index[cloud] = 5;
$Spell::name[5] = "Cloud Attack";
$Spell::description[5] = "Casts an explosive.";
$Spell::delay[5] = 1;
$Spell::recoveryTime[5] = 3;
$Spell::radius[5] = 10;
$Spell::damageValue[5] = "85";
$Spell::LOSrange[5] = 80;
$Spell::manaCost[5] = 11;
$Spell::startSound[5] = ActivateBF;
$Spell::endSound[5] = ExplodeLM;
$Spell::groupListCheck[5] = False;
$Spell::refVal[5] = 85;
$Spell::graceDistance[5] = 2;
$SkillType[cloud] = $SkillOffensiveCasting;

$Spell::keyword[6] = "melt";
$Spell::index[melt] = 6;
$Spell::name[6] = "Melt Bomb Attack";
$Spell::description[6] = "Casts an explosive.";
$Spell::delay[6] = 1;
$Spell::recoveryTime[6] = 3;
$Spell::radius[6] = 10;
$Spell::damageValue[6] = "140";
$Spell::LOSrange[6] = 80;
$Spell::manaCost[6] = 15;
$Spell::startSound[6] = ActivateBF;
$Spell::endSound[6] = ExplodeLM;
$Spell::groupListCheck[6] = False;
$Spell::refVal[6] = 140;
$Spell::graceDistance[6] = 2;
$SkillType[melt] = $SkillOffensiveCasting;

$Spell::keyword[7] = "powercloud";
$Spell::index[powercloud] = 7;
$Spell::name[7] = "Power Cloud Attack";
$Spell::description[7] = "Casts three explosives.";
$Spell::delay[7] = 1;
$Spell::recoveryTime[7] = 3;
$Spell::radius[7] = 10;
$Spell::damageValue[7] = "70";
$Spell::LOSrange[7] = 80;
$Spell::manaCost[7] = 23;
$Spell::startSound[7] = ActivateBF;
$Spell::endSound[7] = ExplodeLM;
$Spell::groupListCheck[7] = False;
$Spell::refVal[7] = 210;
$Spell::graceDistance[7] = 2;
$SkillType[powercloud] = $SkillOffensiveCasting;

$Spell::keyword[8] = "heal";
$Spell::index[heal] = 8;
$Spell::name[8] = "Heal Self";
$Spell::description[8] = "Heals the caster.";
$Spell::delay[8] = 1.5;
$Spell::recoveryTime[8] = 2.25;
$Spell::damageValue[8] = -6;
$Spell::manaCost[8] = 2;
$Spell::startSound[8] = DeActivateWA;
$Spell::endSound[8] = ActivateAR;
$Spell::groupListCheck[8] = False;
$Spell::refVal[8] = -6;
$Spell::graceDistance[8] = 2;
$SkillType[heal] = $SkillDefensiveCasting;

$Spell::keyword[9] = "advheal1";
$Spell::index[advheal1] = 9;
$Spell::name[9] = "Heal Self or Other (1st)";
$Spell::description[9] = "Heals the caster or someone in the LOS.";
$Spell::delay[9] = 1.5;
$Spell::recoveryTime[9] = 3.25;
$Spell::damageValue[9] = -10;
$Spell::LOSrange[9] = 80;
$Spell::manaCost[9] = 3;
$Spell::startSound[9] = DeActivateWA;
$Spell::endSound[9] = ActivateAR;
$Spell::groupListCheck[9] = False;
$Spell::refVal[9] = -10;
$Spell::graceDistance[9] = 2;
$SkillType[advheal1] = $SkillDefensiveCasting;

$Spell::keyword[10] = "advheal2";
$Spell::index[advheal2] = 10;
$Spell::name[10] = "Heal Self Or Other (2nd)";
$Spell::description[10] = "Heals the caster or someone in the LOS.";
$Spell::delay[10] = 1.5;
$Spell::recoveryTime[10] = 4.0;
$Spell::damageValue[10] = -15;
$Spell::LOSrange[10] = 80;
$Spell::manaCost[10] = 4;
$Spell::startSound[10] = DeActivateWA;
$Spell::endSound[10] = ActivateAR;
$Spell::groupListCheck[10] = False;
$Spell::refVal[10] = -15;
$Spell::graceDistance[10] = 2;
$SkillType[advheal2] = $SkillDefensiveCasting;

$Spell::keyword[11] = "godlyheal";
$Spell::index[godlyheal] = 11;
$Spell::name[11] = "Godly Heal Self Or Other";
$Spell::description[11] = "Heals the caster or someone in the LOS.";
$Spell::delay[11] = 1.5;
$Spell::recoveryTime[11] = 6;
$Spell::damageValue[11] = -80;
$Spell::LOSrange[11] = 80;
$Spell::manaCost[11] = 15;
$Spell::startSound[11] = DeActivateWA;
$Spell::endSound[11] = ActivateAR;
$Spell::groupListCheck[11] = False;
$Spell::refVal[11] = -80;
$Spell::graceDistance[11] = 2;
$SkillType[godlyheal] = $SkillDefensiveCasting;

$Spell::keyword[12] = "beam";
$Spell::index[beam] = 12;
$Spell::name[12] = "Beam";
$Spell::description[12] = "Light gathers into a concentrated beam and causes intense damage to the target.";
$Spell::delay[12] = 0;
$Spell::recoveryTime[12] = 7;
$Spell::damageValue[12] = "180";
$Spell::LOSrange[12] = 1000;
$Spell::manaCost[12] = 35;
$Spell::startSound[12] = HitLevelDT;
$Spell::endSound[12] = HitBF;
$Spell::groupListCheck[12] = False;
$Spell::refVal[12] = 180;
$Spell::graceDistance[12] = 5;
$SkillType[beam] = $SkillOffensiveCasting;

$Spell::keyword[13] = "thorn";
$Spell::index[thorn] = 13;
$Spell::name[13] = "Thorn";
$Spell::description[13] = "Casts thorn.";
$Spell::delay[13] = 0.1;
$Spell::recoveryTime[13] = 0.5;
$Spell::radius[13] = 6;
$Spell::damageValue[13] = "20";
$Spell::LOSrange[13] = 300;
$Spell::manaCost[13] = 1;
$Spell::startSound[13] = ActivateFK;
$Spell::endSound[13] = DeflectAS;
$Spell::groupListCheck[13] = False;
$Spell::refVal[13] = 20;
$Spell::graceDistance[13] = 5;
$SkillType[thorn] = $SkillOffensiveCasting;

$Spell::keyword[14] = "fireball";
$Spell::index[fireball] = 14;
$Spell::name[14] = "Fireball";
$Spell::description[14] = "Casts a fireball.";
$Spell::delay[14] = 1;
$Spell::recoveryTime[14] = 1;
$Spell::radius[14] = 8;
$Spell::damageValue[14] = "35";
$Spell::LOSrange[14] = 80;
$Spell::manaCost[14] = 3;
$Spell::startSound[14] = ActivateAB;
$Spell::endSound[14] = LaunchFB;
$Spell::groupListCheck[14] = False;
$Spell::refVal[14] = 35;
$Spell::graceDistance[14] = 2;
$SkillType[fireball] = $SkillOffensiveCasting;

$Spell::keyword[15] = "icespike";
$Spell::index[icespike] = 15;
$Spell::name[15] = "Icespike";
$Spell::description[15] = "Casts icespike.";
$Spell::delay[15] = 0.1;
$Spell::recoveryTime[15] = 1;
$Spell::radius[15] = 6;
$Spell::damageValue[15] = "28";
$Spell::LOSrange[15] = 80;
$Spell::manaCost[15] = 3;
$Spell::startSound[15] = ActivateFK;
$Spell::endSound[15] = HitPawnDT;
$Spell::groupListCheck[15] = False;
$Spell::refVal[15] = 28;
$Spell::graceDistance[15] = 5;
$SkillType[icespike] = $SkillOffensiveCasting;

$Spell::keyword[16] = "icestorm";
$Spell::index[icestorm] = 16;
$Spell::name[16] = "Icestorm";
$Spell::description[16] = "Casts icestorm.";
$Spell::delay[16] = 1;
$Spell::recoveryTime[16] = 2;
$Spell::radius[16] = 11;
$Spell::damageValue[16] = "45";
$Spell::LOSrange[16] = 80;
$Spell::manaCost[16] = 5;
$Spell::startSound[16] = ImpactTR;
$Spell::endSound[16] = Reflected;
$Spell::groupListCheck[16] = False;
$Spell::refVal[16] = 45;
$Spell::graceDistance[16] = 2;
$SkillType[icestorm] = $SkillOffensiveCasting;

$Spell::keyword[17] = "ironfist";
$Spell::index[ironfist] = 17;
$Spell::name[17] = "Ironfist";
$Spell::description[17] = "Casts ironfist.";
$Spell::delay[17] = 0.1;
$Spell::recoveryTime[17] = 6;
$Spell::radius[17] = 7;
$Spell::damageValue[17] = "128";
$Spell::LOSrange[17] = 80;
$Spell::manaCost[17] = 18;
$Spell::startSound[17] = UnravelAM;
$Spell::endSound[17] = NoSound;
$Spell::groupListCheck[17] = False;
$Spell::refVal[17] = 128;
$Spell::graceDistance[17] = 3;
$SkillType[ironfist] = $SkillOffensiveCasting;

$Spell::keyword[18] = "hellstorm";
$Spell::index[hellstorm] = 18;
$Spell::name[18] = "Hellstorm";
$Spell::description[18] = "Casts hellstorm.";
$Spell::delay[18] = 4;
$Spell::recoveryTime[18] = 10;
$Spell::radius[18] = 20;
$Spell::damageValue[18] = "265";
$Spell::LOSrange[18] = 80;
$Spell::manaCost[18] = 25;
$Spell::startSound[18] = LoopLS;
$Spell::endSound[18] = LaunchET;
$Spell::groupListCheck[18] = False;
$Spell::refVal[18] = 265;
$Spell::graceDistance[18] = 2;
$SkillType[hellstorm] = $SkillOffensiveCasting;

$Spell::keyword[19] = "dimensionrift";
$Spell::index[dimensionrift] = 19;
$Spell::name[19] = "Dimension Rift";
$Spell::description[19] = "Casts Dimension Rift.";
$Spell::delay[19] = 4;
$Spell::recoveryTime[19] = 8;
$Spell::radius[19] = 30;
$Spell::damageValue[19] = "320";
$Spell::LOSrange[19] = 80;
$Spell::manaCost[19] = 50;
$Spell::startSound[19] = LaunchLS;
$Spell::endSound[19] = Explode3FW;
$Spell::groupListCheck[19] = False;
$Spell::refVal[19] = 320;
$Spell::graceDistance[19] = 2;
$SkillType[dimensionrift] = $SkillOffensiveCasting;

$Spell::keyword[20] = "advheal3";
$Spell::index[advheal3] = 20;
$Spell::name[20] = "Heal Self Or Other (3rd)";
$Spell::description[20] = "Heals the caster or someone in the LOS.";
$Spell::delay[20] = 1.5;
$Spell::recoveryTime[20] = 4.75;
$Spell::damageValue[20] = -25;
$Spell::LOSrange[20] = 80;
$Spell::manaCost[20] = 5;
$Spell::startSound[20] = DeActivateWA;
$Spell::endSound[20] = ActivateAR;
$Spell::groupListCheck[20] = False;
$Spell::refVal[20] = -25;
$Spell::graceDistance[20] = 2;
$SkillType[advheal3] = $SkillDefensiveCasting;

$Spell::keyword[21] = "remort";
$Spell::index[remort] = 21;
$Spell::name[21] = "Remort";
$Spell::description[21] = "Remorts a level 100+(4*Remort Level) character to level 1, with bonuses.";
$Spell::delay[21] = 3.0;
$Spell::recoveryTime[21] = 1;
$Spell::damageValue[21] = 0;
$Spell::manaCost[21] = 1;
$Spell::startSound[21] = RespawnA;
$Spell::endSound[21] = RespawnC;
$Spell::groupListCheck[21] = False;
$Spell::refVal[21] = 0;
$Spell::graceDistance[21] = 2;
$SkillType[remort] = $SkillNeutralCasting;

$Spell::keyword[22] = "fullheal";
$Spell::index[fullheal] = 22;
$Spell::name[22] = "Full Heal Self";
$Spell::description[22] = "Fully heals the caster.";
$Spell::delay[22] = 1.5;
$Spell::recoveryTime[22] = 60;
$Spell::damageValue[22] = 0;
$Spell::manaCost[22] = 2;
$Spell::startSound[22] = DeActivateWA;
$Spell::endSound[22] = PlaceSeal;
$Spell::groupListCheck[22] = False;
$Spell::refVal[22] = -9998;
$Spell::graceDistance[22] = 2;
$SkillType[fullheal] = $SkillDefensiveCasting;

$Spell::keyword[23] = "massheal";
$Spell::index[massheal] = 23;
$Spell::name[23] = "Mass Heal";
$Spell::description[23] = "Heals caster and friendlies 10 meters around.";
$Spell::delay[23] = 1.5;
$Spell::recoveryTime[23] = 10;
$Spell::radius[23] = 10;
$Spell::damageValue[23] = -30;
$Spell::manaCost[23] = 12;
$Spell::startSound[23] = DeActivateWA;
$Spell::endSound[23] = ActivateAR;
$Spell::groupListCheck[23] = False;
$Spell::refVal[23] = -30;
$Spell::graceDistance[23] = 2;
$SkillType[massheal] = $SkillDefensiveCasting;

$Spell::keyword[24] = "massfullheal";
$Spell::index[massfullheal] = 24;
$Spell::name[24] = "Mass Full Heal";
$Spell::description[24] = "Fully Heals caster and friendlies 12 meters around.";
$Spell::delay[24] = 1.5;
$Spell::recoveryTime[24] = 150;
$Spell::radius[24] = 12;
$Spell::damageValue[24] = 0;
$Spell::manaCost[24] = 200;
$Spell::startSound[24] = DeActivateWA;
$Spell::endSound[24] = PlaceSeal;
$Spell::groupListCheck[24] = False;
$Spell::refVal[24] = -9999;
$Spell::graceDistance[24] = 2;
$SkillType[massfullheal] = $SkillDefensiveCasting;

$Spell::keyword[25] = "shield";
$Spell::index[shield] = 25;
$Spell::name[25] = "Shield Self";
$Spell::description[25] = "A magical shield adds 50 DEF to the caster.";
$Spell::delay[25] = 2.0;
$Spell::recoveryTime[25] = 8;
$Spell::damageValue[25] = "DEF 50";
$Spell::ticks[25] = 150;	//5 minutes
$Spell::manaCost[25] = 5;
$Spell::startSound[25] = ActivateTR;
$Spell::endSound[25] = ActivateTD;
$Spell::groupListCheck[25] = False;
$Spell::refVal[25] = -10;
$Spell::graceDistance[25] = 2;
$SkillType[shield] = $SkillDefensiveCasting;

$Spell::keyword[26] = "advshield1";
$Spell::index[advshield1] = 26;
$Spell::name[26] = "Shield Self Or Other (1st)";
$Spell::description[26] = "A magical shield that adds 80 DEF to the caster or target in LOS.";
$Spell::delay[26] = 2.0;
$Spell::recoveryTime[26] = 10;
$Spell::damageValue[26] = "DEF 80";
$Spell::ticks[26] = 165;	//5:30 minutes
$Spell::LOSrange[26] = 80;
$Spell::manaCost[26] = 8;
$Spell::startSound[26] = ActivateTR;
$Spell::endSound[26] = ActivateTD;
$Spell::groupListCheck[26] = False;
$Spell::refVal[26] = -11;
$Spell::graceDistance[26] = 2;
$SkillType[advshield1] = $SkillDefensiveCasting;

$Spell::keyword[27] = "advshield2";
$Spell::index[advshield2] = 27;
$Spell::name[27] = "Shield Self Or Other (2nd)";
$Spell::description[27] = "A magical shield that adds 70 DEF and 50 MDEF to the caster or target in LOS.";
$Spell::delay[27] = 2.0;
$Spell::recoveryTime[27] = 12;
$Spell::damageValue[27] = "DEF 70 MDEF 50";
$Spell::ticks[27] = 190;	//6:20 minutes
$Spell::LOSrange[27] = 80;
$Spell::manaCost[27] = 15;
$Spell::startSound[27] = ActivateTR;
$Spell::endSound[27] = ActivateTD;
$Spell::groupListCheck[27] = False;
$Spell::refVal[27] = -12;
$Spell::graceDistance[27] = 2;
$SkillType[advshield2] = $SkillDefensiveCasting;

$Spell::keyword[28] = "advshield3";
$Spell::index[advshield3] = 28;
$Spell::name[28] = "Shield Self Or Other (3rd)";
$Spell::description[28] = "A magical shield that adds 120 DEF and 80 MDEF to the caster or target in LOS.";
$Spell::delay[28] = 2.0;
$Spell::recoveryTime[28] = 14;
$Spell::damageValue[28] = "DEF 120 MDEF 80";
$Spell::ticks[28] = 218;	//7:16 minutes
$Spell::LOSrange[28] = 80;
$Spell::manaCost[28] = 18;
$Spell::startSound[28] = ActivateTR;
$Spell::endSound[28] = ActivateTD;
$Spell::groupListCheck[28] = False;
$Spell::refVal[28] = -13;
$Spell::graceDistance[28] = 2;
$SkillType[advshield3] = $SkillDefensiveCasting;

$Spell::keyword[29] = "advshield4";
$Spell::index[advshield4] = 29;
$Spell::name[29] = "Shield Self Or Other (4th)";
$Spell::description[29] = "A magical shield that adds 170 MDEF to the caster or target in LOS.";
$Spell::delay[29] = 2.0;
$Spell::recoveryTime[29] = 16;
$Spell::damageValue[29] = "MDEF 170";
$Spell::ticks[29] = 255;	//8:30 minutes
$Spell::LOSrange[29] = 80;
$Spell::manaCost[29] = 22;
$Spell::startSound[29] = ActivateTR;
$Spell::endSound[29] = ActivateTD;
$Spell::groupListCheck[29] = False;
$Spell::refVal[29] = -14;
$Spell::graceDistance[29] = 2;
$SkillType[advshield4] = $SkillDefensiveCasting;

$Spell::keyword[30] = "advshield5";
$Spell::index[advshield5] = 30;
$Spell::name[30] = "Shield Self Or Other (5th)";
$Spell::description[30] = "A magical shield that adds 150 DEF and 210 MDEF to the caster or target in LOS.";
$Spell::delay[30] = 2.0;
$Spell::recoveryTime[30] = 20;
$Spell::damageValue[30] = "DEF 150 MDEF 210";
$Spell::ticks[30] = 300;	//10 minutes
$Spell::LOSrange[30] = 80;
$Spell::manaCost[30] = 25;
$Spell::startSound[30] = ActivateTR;
$Spell::endSound[30] = ActivateTD;
$Spell::groupListCheck[30] = False;
$Spell::refVal[30] = -15;
$Spell::graceDistance[30] = 2;
$SkillType[advshield5] = $SkillDefensiveCasting;

$Spell::keyword[31] = "massshield";
$Spell::index[massshield] = 31;
$Spell::name[31] = "Mass Shield";
$Spell::description[31] = "A magical shield that adds 115 DEF and 105 MDEF to all friendlies within a 10 meter radius.";
$Spell::delay[31] = 2.0;
$Spell::recoveryTime[31] = 30;
$Spell::radius[31] = 10;
$Spell::damageValue[31] = "DEF 115 MDEF 105";
$Spell::ticks[31] = 270;	//9 minutes
$Spell::manaCost[31] = 20;
$Spell::startSound[31] = ActivateTR;
$Spell::endSound[31] = ActivateTD;
$Spell::groupListCheck[31] = False;
$Spell::refVal[31] = -16;
$Spell::graceDistance[31] = 2;
$SkillType[massshield] = $SkillDefensiveCasting;

$Spell::keyword[32] = "mimic";
$Spell::index[mimic] = 32;
$Spell::name[32] = "Mimic";
$Spell::description[32] = "A very dangerous spell that transforms the caster into the creature in his/her LOS.";
$Spell::delay[32] = 4.0;
$Spell::recoveryTime[32] = 60;
$Spell::LOSrange[32] = 80;
$Spell::damageValue[32] = 0;
$Spell::manaCost[32] = 80;
$Spell::startSound[32] = LoopSP;
$Spell::endSound[32] = AbsorbABS;
$Spell::groupListCheck[32] = False;
$Spell::refVal[32] = 1;
$Spell::graceDistance[32] = 2;
$SkillType[mimic] = $SkillNeutralCasting;

$Spell::keyword[33] = "masstransport";
$Spell::index[masstransport] = 33;
$Spell::name[33] = "Mass Transport";
$Spell::description[33] = "Transports self and all friendlies within a 6 meter radius to a specific zone.";
$Spell::delay[33] = 4.0;
$Spell::recoveryTime[33] = 45;
$Spell::radius[33] = 6;
$Spell::manaCost[33] = 50;
$Spell::startSound[33] = RespawnB;
$Spell::endSound[33] = ActivateCH;
$Spell::groupListCheck[33] = False;
$Spell::refVal[33] = 0;
$Spell::graceDistance[33] = 2;
$SkillType[masstransport] = $SkillNeutralCasting;

$Spell::keyword[34] = "advheal4";
$Spell::index[advheal4] = 34;
$Spell::name[34] = "Heal Self Or Other (4th)";
$Spell::description[34] = "Heals the caster or someone in the LOS.";
$Spell::delay[34] = 1.5;
$Spell::recoveryTime[34] = 5.0;
$Spell::damageValue[34] = -35;
$Spell::LOSrange[34] = 80;
$Spell::manaCost[34] = 6;
$Spell::startSound[34] = DeActivateWA;
$Spell::endSound[34] = ActivateAR;
$Spell::groupListCheck[34] = False;
$Spell::refVal[34] = -35;
$Spell::graceDistance[34] = 2;
$SkillType[advheal4] = $SkillDefensiveCasting;

$Spell::keyword[35] = "advheal5";
$Spell::index[advheal5] = 35;
$Spell::name[35] = "Heal Self Or Other (5th)";
$Spell::description[35] = "Heals the caster or someone in the LOS.";
$Spell::delay[35] = 1.5;
$Spell::recoveryTime[35] = 5.5;
$Spell::damageValue[35] = -50;
$Spell::LOSrange[35] = 80;
$Spell::manaCost[35] = 7;
$Spell::startSound[35] = DeActivateWA;
$Spell::endSound[35] = ActivateAR;
$Spell::groupListCheck[35] = False;
$Spell::refVal[35] = -50;
$Spell::graceDistance[35] = 2;
$SkillType[advheal5] = $SkillDefensiveCasting;

$Spell::keyword[36] = "advheal6";
$Spell::index[advheal6] = 36;
$Spell::name[36] = "Heal Self Or Other (6th)";
$Spell::description[36] = "Heals the caster or someone in the LOS.";
$Spell::delay[36] = 1.5;
$Spell::recoveryTime[36] = 6.0;
$Spell::damageValue[36] = -60;
$Spell::LOSrange[36] = 80;
$Spell::manaCost[36] = 8;
$Spell::startSound[36] = DeActivateWA;
$Spell::endSound[36] = ActivateAR;
$Spell::groupListCheck[36] = False;
$Spell::refVal[36] = -60;
$Spell::graceDistance[36] = 2;
$SkillType[advheal6] = $SkillDefensiveCasting;

$Spell::keyword[37] = "nuke";
$Spell::index[nuke] = 37;
$Spell::name[37] = "Nuclear Bomb";
$Spell::description[37] = "The god of all bombs is in your hands.";
$Spell::delay[37] = 7;
$Spell::recoveryTime[37] = 10;
$Spell::radius[37] = 200;
$Spell::damageValue[37] = 600;
$Spell::LOSrange[37] = 100;
$Spell::manaCost[37] = 100;
$Spell::startSound[37] = LaunchLS;
$Spell::endSound[37] = Explode3FW;
$Spell::groupListCheck[37] = False;
$Spell::refVal[37] = 600;
$Spell::graceDistance[37] = 2;
$SkillType[nuke] = $SkillOffensiveCasting;

$Spell::keyword[38] = "bullet";
$Spell::index[bullet] = 38;
$Spell::name[38] = "Bullet";
$Spell::description[38] = "snipe 'em, make them dance, pee your pants.";
$Spell::delay[38] = 0.1;
$Spell::recoveryTime[38] = 0.1;
$Spell::radius[38] = 5;
$Spell::damageValue[38] = 120;
$Spell::LOSrange[38] = 120;
$Spell::manaCost[38] = 20;
$Spell::startSound[38] = ActivateFK;
$Spell::endSound[38] = DeflectAS;
$Spell::groupListCheck[38] = False;
$Spell::refVal[38] = 120;
$Spell::graceDistance[38] = 5;
$SkillType[bullet] = $SkillOffensiveCasting;

$Spell::keyword[39] = "boom";
$Spell::index[boom] = 39;
$Spell::name[39] = "Bad Lab Experiment";
$Spell::description[39] = "These things happen everyday, and now you can make them!";
$Spell::delay[39] = 3;
$Spell::recoveryTime[39] = 20;
$Spell::radius[39] = 30;
$Spell::damageValue[39] = 140;
$Spell::LOSrange[39] = 50;
$Spell::manaCost[39] =30;
$Spell::startSound[39] = LaunchLS;
$Spell::endSound[39] = Explode3FW;
$Spell::groupListCheck[39] = False;
$Spell::refVal[19] = 140;
$Spell::graceDistance[39] = 2;
$SkillType[boom] = $SkillOffensiveCasting;

$Spell::keyword[40] = "freezerburn";
$Spell::index[freezerburn] = 40;
$Spell::name[40] = "Freezerburn";
$Spell::description[40] = "Freeze your opponent with bad packing.";
$Spell::delay[40] = 0.1;
$Spell::recoveryTime[40] = 0.5;
$Spell::radius[40] = 15;
$Spell::damageValue[40] = 200;
$Spell::LOSrange[40] = 100;
$Spell::manaCost[40] = 40;
$Spell::startSound[40] = ImpactTR;
$Spell::endSound[40] = Reflected;
$Spell::groupListCheck[40] = False;
$Spell::refVal[40] = 200;
$Spell::graceDistance[40] = 2;
$SkillType[freezerburn] = $SkillOffensiveCasting;

$Spell::keyword[41] = "boost";
$Spell::index[boost] = 41;
$Spell::name[41] = "Wind Booster";
$Spell::description[41] = "Use a burst of air to launch yourself forward.";
$Spell::delay[41] = 0.5;
$Spell::recoveryTime[41] = 1;
$Spell::damageValue[41] = "0";
$Spell::manaCost[41] = 15;
$Spell::startSound[41] = ActivateAB;
$Spell::endSound[41] = NoSound;
$Spell::groupListCheck[41] = False;
$Spell::graceDistance[41] = 100;
$SkillType[boost] = $SkillNeutralCasting;

$Spell::keyword[42] = "stop";
$Spell::index[stop] =42;
$Spell::name[42] = "Stop";
$Spell::description[42] = "Use a burst of air to cushion your speed.";
$Spell::delay[42] = 0.2;
$Spell::recoveryTime[42] = 5;
$Spell::damageValue[42] = "0";
$Spell::manaCost[42] = 5;
$Spell::startSound[42] = ActivateAB;
$Spell::endSound[42] = NoSound;
$Spell::groupListCheck[42] = False;
$Spell::graceDistance[42] = 200;
$SkillType[stop] = $SkillNeutralCasting;

$Spell::keyword[43] = "airfist";
$Spell::index[airfist] =43;
$Spell::name[43] = "Airfist";
$Spell::description[43] = "Magical wind fist blasts enemies into the sky.";
$Spell::delay[43] = 1.2;
$Spell::recoveryTime[43] = 3;
$Spell::radius[43] = 10;
$Spell::damageValue[43] = 0;
$Spell::manaCost[43] = 7;
$Spell::startSound[43] = ActivateAB;
$Spell::endSound[43] = NoSound;
$Spell::groupListCheck[43] = False;
$Spell::graceDistance[43] = 2;
$SkillType[airfist] = $SkillNeutralCasting;

$Spell::keyword[44] = "lightstep";
$Spell::index[lightstep] =44;
$Spell::name[44] = "Lightstep";
$Spell::description[44] = "A mass of air that helps you(or player in LOS) lift items.";
$Spell::delay[44] = 2.0;
$Spell::recoveryTime[44] = 10;
$Spell::ticks[44] = 90;
$Spell::LOSrange[44] = 80;
$Spell::manaCost[44] = 12;
$Spell::startSound[44] = ActivateTR;
$Spell::endSound[44] = ActivateTD;
$Spell::groupListCheck[44] = False;
$Spell::graceDistance[44] = 2;
$SkillType[lightstep] =  $SkillNeutralCasting;

$Spell::keyword[45] = "advshove";
$Spell::index[advshove] =45;
$Spell::name[45] = "Super Wind Shove";
$Spell::description[45] = "Use a burst of air to knock an enemy away.";
$Spell::delay[45] = 1;
$Spell::recoveryTime[45] = 3;
$Spell::LOSrange[45] = 80;
$Spell::manaCost[45] = 3;
$Spell::startSound[45] = NoSound;
$Spell::endSound[45] = ActivateAB;
$Spell::groupListCheck[45] = False;
$Spell::graceDistance[45] = 2;
$SkillType[advshove] = $SkillNeutralCasting;

$Spell::keyword[46] = "tornado";
$Spell::index[tornado] =46;
$Spell::name[46] = "Tornado Vortex";
$Spell::description[46] = "Creates a massive blasting column of air.";
$Spell::delay[46] = 9;
$Spell::recoveryTime[46] = 16;
$Spell::radius[46] = 70;
$Spell::damageValue[46] = "400";
$Spell::LOSrange[46] = 120;
$Spell::manaCost[46] = 130;
$Spell::startSound[46] = LaunchLS;
$Spell::endSound[46] = LaunchAB;
$Spell::groupListCheck[46] = False;
$Spell::graceDistance[46] = 2;
$Spell::refVal[46] = 1000;
$SkillType[tornado] = $SkillOffensiveCasting;

$Spell::keyword[47] = "heavystep";
$Spell::index[heavystep] =47;
$Spell::name[47] = "Heavystep";
$Spell::description[47] = "A mass of air that pushes down on your target in your LOS which slows them down.";
$Spell::delay[47] = 4.0;
$Spell::recoveryTime[47] = 5;
$Spell::ticks[47] = 8;
$Spell::LOSrange[47] = 80;
$Spell::manaCost[47] = 40;
$Spell::startSound[47] = ActivateTR;
$Spell::endSound[47] = ActivateTD;
$Spell::groupListCheck[47] = False;
$Spell::graceDistance[47] = 2;
$SkillType[heavystep] =  $SkillNeutralCasting;

$Spell::keyword[48] = "apocalypse";
$Spell::index[apocalypse] =48;
$Spell::name[48] = "Apocalypse";
$Spell::description[48] = "Creates massive explosions everywhere destroying everything in it's path!";
$Spell::delay[48] = 12;
$Spell::recoveryTime[48] = 19;
$Spell::radius[48] = 100;
$Spell::damageValue[48] = "600";
$Spell::LOSrange[48] = 150;
$Spell::manaCost[48] = 200;
$Spell::startSound[48] = LaunchLS;
$Spell::endSound[48] = LaunchAB;
$Spell::groupListCheck[48] = False;
$Spell::graceDistance[48] = 2;
$Spell::refVal[48] = 2000;
$SkillType[apocalypse] = $SkillOffensiveCasting;

$Spell::keyword[49] = "godlyshield";
$Spell::index[godlyshield] = 49;
$Spell::name[49] = "Shield Self Or Other (Godly)";
$Spell::description[49] = "A magical shield that adds 220 DEF and 300 MDEF to the caster or target in LOS.";
$Spell::delay[49] = 2.0;
$Spell::recoveryTime[30] = 20;
$Spell::damageValue[49] = "DEF 220 MDEF 300";
$Spell::ticks[49] = 300;	//10 minutes
$Spell::LOSrange[49] = 80;
$Spell::manaCost[49] = 25;
$Spell::startSound[49] = ActivateTR;
$Spell::endSound[49] = ActivateTD;
$Spell::groupListCheck[49] = False;
$Spell::refVal[49] = -15;
$Spell::graceDistance[49] = 2;
$SkillType[godlyshield] = $SkillDefensiveCasting;

$Spell::keyword[50] = "ionblast";
$Spell::index[ionblast] = 50;
$Spell::name[50] = "Ion Blast";
$Spell::description[50] = "BOOM!";
$Spell::delay[50] = 12.0;
$Spell::recoveryTime[50] = 16.0;
$Spell::radius[50] = 50;
$Spell::damageValue[50] = "1500";
$Spell::LOSrange[50] = 500;
$Spell::manaCost[50] = 300;
$Spell::startSound[50] = LaunchLS;
$Spell::endSound[50] = LaunchAB;
$Spell::groupListCheck[50] = False;
$Spell::graceDistance[50] = 2;
$Spell::refVal[50] = 210;
$SkillType[ionblast] = $SkillOffensiveCasting;

$Spell::keyword[51] = "shredder";
$Spell::index[shredder] = 51;
$Spell::name[51] = "Shredding to pieces";
$Spell::description[51] = "Shred the enemy to pieces with this ultimate offensive spell!";
$Spell::delay[51] = 12.0;
$Spell::recoveryTime[51] = 16.0;
$Spell::radius[51] = 12;
$Spell::damageValue[51] = "3000";
$Spell::LOSrange[51] = 150;
$Spell::manaCost[51] = 500;
$Spell::startSound[51] = LaunchLS;
$Spell::endSound[51] = UnravelAM;
$Spell::groupListCheck[51] = False;
$Spell::graceDistance[51] = 2;
$Spell::refVal[51] = 210;
$SkillType[shredder] = $SkillOffensiveCasting;

$Spell::keyword[52] = "shieldplus1";
$Spell::index[shieldplus1] = 52;
$Spell::name[52] = "Shield Self Or Other (ShieldPlus1)";
$Spell::description[52] = "A magical shield that adds 300 DEF and 400 MDEF to the caster or target in LOS.";
$Spell::delay[52] = 2.0;
$Spell::recoveryTime[52] = 20;
$Spell::damageValue[52] = "DEF 300 MDEF 400";
$Spell::ticks[52] = 300;	//10 minutes
$Spell::LOSrange[52] = 80;
$Spell::manaCost[52] = 35;
$Spell::startSound[52] = ActivateTR;
$Spell::endSound[52] = ActivateTD;
$Spell::groupListCheck[52] = False;
$Spell::refVal[52] = -15;
$Spell::graceDistance[52] = 2;
$SkillType[shieldplus1] = $SkillDefensiveCasting;

$Spell::keyword[53] = "shieldplus2";
$Spell::index[shieldplus2] = 53;
$Spell::name[53] = "Shield Self Or Other (ShieldPlus2)";
$Spell::description[53] = "A magical shield that adds 400 DEF and 500 MDEF to the caster or target in LOS.";
$Spell::delay[53] = 2.0;
$Spell::recoveryTime[53] = 20;
$Spell::damageValue[53] = "DEF 400 MDEF 500";
$Spell::ticks[53] = 300;	//10 minutes
$Spell::LOSrange[53] = 80;
$Spell::manaCost[53] = 50;
$Spell::startSound[53] = ActivateTR;
$Spell::endSound[53] = ActivateTD;
$Spell::groupListCheck[53] = False;
$Spell::refVal[53] = -15;
$Spell::graceDistance[53] = 2;
$SkillType[shieldplus2] = $SkillDefensiveCasting;

$Spell::keyword[54] = "shieldplus3";
$Spell::index[shieldplus3] = 54;
$Spell::name[54] = "Shield Self Or Other (ShieldPlus3)";
$Spell::description[54] = "A magical shield that adds 500 DEF and 600 MDEF to the caster or target in LOS.";
$Spell::delay[54] = 2.0;
$Spell::recoveryTime[54] = 20;
$Spell::damageValue[54] = "DEF 500 MDEF 600";
$Spell::ticks[54] = 300;	//10 minutes
$Spell::LOSrange[54] = 80;
$Spell::manaCost[54] = 75;
$Spell::startSound[54] = ActivateTR;
$Spell::endSound[54] = ActivateTD;
$Spell::groupListCheck[54] = False;
$Spell::refVal[54] = -15;
$Spell::graceDistance[54] = 2;
$SkillType[shieldplus3] = $SkillDefensiveCasting;

$Spell::keyword[55] = "shieldplus4";
$Spell::index[shieldplus4] = 55;
$Spell::name[55] = "Shield Self Or Other (ShieldPlus4)";
$Spell::description[55] = "A magical shield that adds 600 DEF and 700 MDEF to the caster or target in LOS.";
$Spell::delay[55] = 2.0;
$Spell::recoveryTime[55] = 20;
$Spell::damageValue[55] = "DEF 600 MDEF 700";
$Spell::ticks[55] = 300;	//10 minutes
$Spell::LOSrange[55] = 80;
$Spell::manaCost[55] = 100;
$Spell::startSound[55] = ActivateTR;
$Spell::endSound[55] = ActivateTD;
$Spell::groupListCheck[55] = False;
$Spell::refVal[55] = -15;
$Spell::graceDistance[55] = 2;
$SkillType[shieldplus4] = $SkillDefensiveCasting;

$Spell::keyword[56] = "shieldplus5";
$Spell::index[shieldplus5] = 56;
$Spell::name[56] = "Shield Self Or Other (ShieldPlus5)";
$Spell::description[56] = "A magical shield that adds 700 DEF and 800 MDEF to the caster or target in LOS.";
$Spell::delay[56] = 2.0;
$Spell::recoveryTime[56] = 20;
$Spell::damageValue[56] = "DEF 700 MDEF 800";
$Spell::ticks[56] = 300;	//10 minutes
$Spell::LOSrange[56] = 80;
$Spell::manaCost[56] = 125;
$Spell::startSound[56] = ActivateTR;
$Spell::endSound[56] = ActivateTD;
$Spell::groupListCheck[56] = False;
$Spell::refVal[56] = -15;
$Spell::graceDistance[56] = 2;
$SkillType[shieldplus5] = $SkillDefensiveCasting;

$Spell::keyword[57] = "shieldplus6";
$Spell::index[shieldplus6] = 57;
$Spell::name[57] = "Shield Self Or Other (ShieldPlus6)";
$Spell::description[57] = "A magical shield that adds 800 DEF and 900 MDEF to the caster or target in LOS.";
$Spell::delay[57] = 2.0;
$Spell::recoveryTime[57] = 20;
$Spell::damageValue[57] = "DEF 800 MDEF 900";
$Spell::ticks[57] = 300;	//10 minutes
$Spell::LOSrange[57] = 80;
$Spell::manaCost[57] = 150;
$Spell::startSound[57] = ActivateTR;
$Spell::endSound[57] = ActivateTD;
$Spell::groupListCheck[57] = False;
$Spell::refVal[57] = -15;
$Spell::graceDistance[57] = 2;
$SkillType[shieldplus6] = $SkillDefensiveCasting;

%snumber = "58";
$Spell::keyword[%snumber] = "Airblast";
$Spell::index[Airblast] = %snumber;
$Spell::name[%snumber] = "Air blast";
$Spell::description[%snumber] = "Casts a blast of air to send all targets within 15 meters of you flying.";
$Spell::delay[%snumber] = 0.5;
$Spell::recoveryTime[%snumber] = 5.0;
$Spell::radius[%snumber] = 15;
$Spell::manaCost[%snumber] = 100;
$Spell::startSound[%snumber] = SoundJetHeavy;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -3000;
$Spell::graceDistance[%snumber] = 2;
$SkillType[Airblast] = $SkillNeutralCasting;

%snumber = "59";
$Spell::keyword[%snumber] = "Airwarp";
$Spell::index[Airwarp] = %snumber;
$Spell::name[%snumber] = "Air warp";
$Spell::description[%snumber] = "Directly warps air to send all targets within 35 meters of you flying.";
$Spell::delay[%snumber] = 1.5;
$Spell::recoveryTime[%snumber] = 7.0;
$Spell::radius[%snumber] = 35;
$Spell::manaCost[%snumber] = 300;
$Spell::startSound[%snumber] = SoundJetHeavy;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -3000;
$Spell::graceDistance[%snumber] = 2;
$SkillType[Airwarp] = $SkillNeutralCasting;

%snumber = "60";
$Spell::keyword[%snumber] = "HealPlus1";
$Spell::index[HealPlus1] = %snumber;
$Spell::name[%snumber] = "Heal Plus (1st)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1;
$Spell::recoveryTime[%snumber] = 15;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 60;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus1] = $SkillDefensiveCasting;

%snumber = "61";
$Spell::keyword[%snumber] = "HealPlus2";
$Spell::index[HealPlus2] = %snumber;
$Spell::name[%snumber] = "Heal Plus (2nd)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.1;
$Spell::recoveryTime[%snumber] = 20;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 100;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus2] = $SkillDefensiveCasting;

%snumber = "62";
$Spell::keyword[%snumber] = "HealPlus3";
$Spell::index[HealPlus3] = %snumber;
$Spell::name[%snumber] = "Heal Plus (3rd)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.2;
$Spell::recoveryTime[%snumber] = 25;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 150;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus3] = $SkillDefensiveCasting;

%snumber = "63";
$Spell::keyword[%snumber] = "HealPlus4";
$Spell::index[HealPlus4] = %snumber;
$Spell::name[%snumber] = "Heal Plus (4th)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.3;
$Spell::recoveryTime[%snumber] = 30;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 250;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus4] = $SkillDefensiveCasting;

%snumber = "64";
$Spell::keyword[%snumber] = "HealPlus5";
$Spell::index[HealPlus5] = %snumber;
$Spell::name[%snumber] = "Heal Plus (5th)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.4;
$Spell::recoveryTime[%snumber] = 35;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 500;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus5] = $SkillDefensiveCasting;

%snumber = "65";
$Spell::keyword[%snumber] = "HealPlus6";
$Spell::index[HealPlus6] = %snumber;
$Spell::name[%snumber] = "Heal Plus (6th)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 0.0;
$Spell::recoveryTime[%snumber] = 40;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 800;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus6] = $SkillDefensiveCasting;

$Spell::keyword[66] = "Terminate";
$Spell::index[Terminate] = 66;
$Spell::name[66] = "Terminate";
$Spell::description[66] = "If you see this, you're dead.";
$Spell::delay[66] = 24;
$Spell::recoveryTime[66] = 60;
$Spell::radius[66] = 200;
$Spell::damageValue[66] = 4000;
$Spell::LOSrange[66] = 100;
$Spell::manaCost[66] = 1000;
$Spell::startSound[66] = LaunchLS;
$Spell::endSound[66] = Explode3FW;
$Spell::groupListCheck[66] = False;
$Spell::refVal[66] = 600;
$Spell::graceDistance[66] = 2;
$SkillType[Terminate] = $SkillOffensiveCasting;

%snumber = "67";
$Spell::keyword[%snumber] = "Snipe";
$Spell::index[Snipe] = %snumber;
$Spell::name[%snumber] = "Snipe";
$Spell::description[%snumber] = "Hits the target with dead on precision.";
$Spell::delay[%snumber] = 0.0;
$Spell::recoveryTime[%snumber] = 1;
$Spell::damageValue[%snumber] = 3000;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 800;
$Spell::startSound[%snumber] = HitLevelDT;
$Spell::endSound[%snumber] = HitBF;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[Snipe] = $SkillOffensiveCasting;
//----------------------------------------------------------------------------------------------------------------

function BeginCastSpell(%clientId, %keyword)
{
	dbecho($dbechoMode, "BeginCastSpell(" @ %clientId @ ", " @ %keyword @ ")");

	%w1 = GetWord(%keyword, 0);
	%w2 = String::getSubStr(%keyword, String::len(%w1)+1, 99999);

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		if(String::ICompare($Spell::keyword[%i], %w1) == 0)
		{
			if(SkillCanUse(%clientId, $Spell::keyword[%i]))
			{
				if(fetchData(%clientId, "MANA") >= $Spell::manaCost[%i])
				{
					Client::sendMessage(%clientId, $MsgBeige, "Casting " @ $Spell::name[%i] @ ".");

					%player = Client::getOwnedObject(%clientId);
					if(GameBase::getLOSinfo(%player, $Spell::LOSrange[%i]))
					{
						%lospos = $los::position;
						%losobj = $los::object;
					}
					else
					{
						%lospos = "";
						%losobj = 0;
					}
	
					storeData(%clientId, "SpellCastStep", 1);
	
					%tempManaCost = floor($Spell::manaCost[%i] / 2);
					refreshMANA(%clientId, %tempManaCost);
					playSound($Spell::startSound[%i], GameBase::getPosition(%clientId));

					%skt = $SkillType[$Spell::keyword[%i]];
					%sk1 = $PlayerSkill[%clientId, %skt];
					%gsa = GetSkillAmount($Spell::keyword[%i], %skt);
					%sk2 = %sk1 - %gsa;
					%sk = Cap(%sk2, 0, "inf");
					%rt = $Spell::recoveryTime[%i];
					%a = %rt / 2;
					%b = (1000 - %sk) / 1000;
					%c = %b * %a;
					%recovTime = $Spell::delay[%i] + Cap(%a + %c, %a, %rt);	//recovery time is never smaller than half of the original and never bigger than the original.

					schedule("%retval=DoCastSpell(" @ %clientId @ ", " @ %i @ ", \"" @ GameBase::getPosition(%clientId) @ "\", \"" @ %lospos @ "\", \"" @ %losobj @ "\", \"" @ %w2 @ "\"); if(%retval){refreshMANA(" @ %clientId @ ", " @ %tempManaCost @ ");}", $Spell::delay[%i]);
					schedule("storeData(" @ %clientId @ ", \"SpellCastStep\", \"\");sendDoneRecovMsg(" @ %clientId @ ");", %recovTime);
		
					return True;
				}
				else
					Client::sendMessage(%clientId, $MsgWhite, "Insufficient mana to cast this spell.");
			}
			else
				Client::sendMessage(%clientId, $MsgWhite, "You can't cast this spell because you lack the necessary skills.");

			return False;
		}
	}
	Client::sendMessage(%clientId, $MsgWhite, "This spell seems unfamiliar to you.");

	return False;
}

function DoCastSpell(%clientId, %index, %oldpos, %castPos, %castObj, %w2)
{
	dbecho($dbechoMode, "DoCastSpell(" @ %clientId @ ", " @ %index @ ", " @ %oldpos @ ", " @ %castPos @ ", " @ %castObj @ ", " @ %w2 @ ")");

	%player = Client::getOwnedObject(%clientId);

	if(Vector::getDistance(%oldpos, GameBase::getPosition(%clientId)) > $Spell::graceDistance[%index])
	{
		Client::sendMessage(%clientId, $MsgBeige, "Your casting was interrupted.");
		storeData(%clientId, "SpellCastStep", 2);

		return False;
	}

	//group-list check
	if($Spell::groupListCheck[%index])
	{
		%cl = Player::getClient(%castObj);
		if( !(IsInCommaList(fetchData(%clientId, "grouplist"), Client::getName(%cl)) && IsInCommaList(fetchData(%cl, "grouplist"), Client::getName(%clientId))) && %cl != %clientId && %cl != -1)
		{
			Client::sendMessage(%clientId, $MsgBeige, "You are not part of the target's group.");
			storeData(%clientId, "SpellCastStep", 2);

			return False;
		}
	}

	//==================================================================

	//unfortunately hard-coded part -- although that is the original purpose of Tribes scripting
	if(%index == 1)
	{
		//firebomb spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb1", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}

	if(%index == 2)
	{
		//teleport zone spell

		%zoneId = GetNearestZone(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			Client::sendMessage(%clientId, $MsgBeige, "Teleporting near " @ Zone::getDesc(%zoneId));

			//teleport

			%mpos = Zone::getMarker(%zoneId);
			if(!fetchData(%clientId, "invisible"))
				GameBase::startFadeIn(%clientId);

			GameBase::setPosition(%clientId, %mpos);
			CheckAndBootFromArena(%clientId);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			Player::setDamageFlash(%clientId, 0.7);
			%extraDelay = 0.22;	//sometimes the endSound doesn't get played unless there is sufficient delay

			%castPos = SetOnGround(%clientId, 500);
			//%castPos = %newpos;

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Teleportation failed.");
			%returnFlag = False;
		}
	}

	if(%index == 3)
	{
		//Transport zone spell

		%zoneId = GetZoneByKeywords(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			Client::sendMessage(%clientId, $MsgBeige, "Transporting to " @ Zone::getDesc(%zoneId));

			//teleport

			%system = Object::getName(%zoneId);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);

			%castPos = TeleportToMarker(%clientId, "Zones\\" @ %system @ "\\DropPoints", False, True);
			CheckAndBootFromArena(%clientId);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			if(!fetchData(%clientId, "invisible"))
				GameBase::startFadeIn(%clientId);

			Player::setDamageFlash(%clientId, 0.7);
			%extraDelay = 0.22;	//sometimes the endSound doesn't get played unless there is sufficient delay

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Transportation failed.");
			%returnFlag = False;
		}
	}

	if(%index == 4)
	{
		//Advanced Transport zone spell

		%zoneId = GetZoneByKeywords(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			if(getObjectType(%castObj) == "Player")
				%id = Player::getClient(%castObj);
			else
				%id = %clientId;

			Client::sendMessage(%clientId, $MsgBeige, "Transporting to " @ Zone::getDesc(%zoneId));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, "You are being transported to " @ Zone::getDesc(%zoneId));

			//teleport

			%system = Object::getName(%zoneId);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);

			%castPos = TeleportToMarker(%id, "Zones\\" @ %system @ "\\DropPoints", False, True);
			CheckAndBootFromArena(%id);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			if(!fetchData(%id, "invisible"))
				GameBase::startFadeIn(%id);

			Player::setDamageFlash(%id, 0.7);
			%extraDelay = 0.22;	//sometimes the endSound doesn't get played unless there is sufficient delay

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Transportation failed.");
			%returnFlag = False;
		}
	}
	if(%index == 5)
	{
		//cloud spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb2", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}
	if(%index == 6)
	{
		//melt spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb3", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}
	if(%index == 7)
	{
		//power cloud spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			for(%i = 0; %i <= 2; %i++)
			{
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb2\", \"" @ %castPos @ "\", True, " @ %index @ ");", %i / 2, %player);
			}

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}
	if(%index == 8)
	{
		//heal self spell

		Client::sendMessage(%clientId, $MsgBeige, "Healing self");

		%r = $Spell::damageValue[%index] / $TribesDamageToNumericDamage;
		refreshHP(%clientId, %r);

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}


	if(%index == 9 || %index == 10 || %index == 11 || %index == 20 || %index == 34 || %index == 35 || %index == 36)
	{
		//heal self or other (LOS) 1st, 2nd, 3rd, 4th, 5th, 6th, godly

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		%r = $Spell::damageValue[%index] / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
	if(%index == 60)
	{
//CurePlus1

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 61)
	{
//CurePlus2

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.2)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 62)
	{
//CurePlus3

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.4)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 63)
	{
//CurePlus4

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.6)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 64)
	{
//CurePlus5

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.8)) / $TribesDamageToNumericDamage; //$Spell::damageValue[%index] + 

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 65)
	{
//CurePlus5

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		%r = $Spell::damageValue[%index] + round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.9)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
	if(%index == 12)
	{
		//laser beam

		if(getObjectType(%castObj) == "Player")
			%id = Player::getClient(%castObj);

		%trans = GameBase::getMuzzleTransform(%clientId);
		%p = Projectile::spawnProjectile("heatlaser", %trans, %player, "0 0 0", 1.0);

		%mom1 = Vector::getFromRot( GameBase::getRotation(%clientId), -60, 1 );
		Player::applyImpulse(%clientId, %mom1);

		%r = $Spell::damageValue[%index];
	
		if(%id != "")
		{
			//%miss = CalcSpellMiss(%clientId, %id, %index);

			SpellDamage(%clientId, %id, %r, %index);
			%mom2 = Vector::getFromRot( GameBase::getRotation(%clientId), 50, 1 );
			Player::applyImpulse(%id, %mom2);
		}

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 67)
	{
		//snipe

		if(getObjectType(%castObj) == "Player")
			%id = Player::getClient(%castObj);

		%trans = GameBase::getMuzzleTransform(%clientId);
		Projectile::spawnProjectile("dforsnipe", %trans, %player, "0 1 0", 7.0);
		Projectile::spawnProjectile("heatLaser", %trans, %player, "0 5 0", 25.0);

		%mom1 = Vector::getFromRot( GameBase::getRotation(%clientId), -60, 1 );
		Player::applyImpulse(%clientId, %mom1);

		%r = $Spell::damageValue[%index];
	
		if(%id != "")
		{
			//%miss = CalcSpellMiss(%clientId, %id, %index);

			SpellDamage(%clientId, %id, %r, %index);
			%mom2 = Vector::getFromRot( GameBase::getRotation(%clientId), 50, 1 );
			Player::applyImpulse(%id, %mom2);
		}

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 52 || %index == 53 || %index == 54 || %index == 55 || %index == 56 || %index == 57)
	{
		//shieldplus from 1-6 in order

	if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Shielding " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		UpdateBonusState(%id, $Spell::damageValue[%index], $Spell::ticks[%index]);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
	if(%index == 13 || %index == 38)
	{
		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb11", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 14)
	{
		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb9", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 15)
	{
		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb7", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 16 || %index == 40)
	{
		if(%castPos != "")
		{
			%minrad = 0;
			%maxrad = $Spell::radius[%index] / 2;
			for(%i = 0; %i <= 8; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb10\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 7, %player);
			}
			CreateAndDetBomb(%clientId, "Bomb10", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 17)
	{
		if(%castPos != "")
		{
			%minrad = 0;
			%maxrad = $Spell::radius[%index];
			for(%i = 0; %i <= 8; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 3);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb12\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 24, %player);
			}
			CreateAndDetBomb(%clientId, "Bomb12", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 18)
	{
		if(%castPos != "")
		{
			%minrad = 0;
			%maxrad = 5;
			for(%i = 0; %i <= 24; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + 72 - (%i * 3);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb9\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 16, %player);
			}
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb1\", \"" @ %castPos @ "\", True, " @ %index @ ");", %i / 16, %player);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 19 || %index == 37 || %index == 39)
	{
		if(%castPos != "")
		{
			%minrad = 0;
			%maxrad = 4;
			for(%i = 0; %i <= 10; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb7\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 20, %player);
			}
			for(%i = 0; %i <= 10; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb8\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 20, %player);
			}

			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %castPos @ "\", False, " @ %index @ ");", 1.0, %player);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb6\", \"" @ %castPos @ "\", False, " @ %index @ ");", 1.05, %player);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %castPos @ "\", True, " @ %index @ ");", 1.1, %player);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
//	if(%index == 21)
//	{
//		if((!fetchData(%clientId, "currentlyRemorting")) && (fetchData(%clientId, "LVL") > fetchData(%clientId, "RemortStep") * 4 + 99 ))
//		{
//			%castPos = DoRemort(%clientId);		
//
	//		%extraDelay = 0.22;
	//		%returnFlag = True;
	//	}
	//	else
	//		%returnFlag = False;
	//}
if (%index == 21)
{
    %remortStep = fetchData(%clientId, "RemortStep");
    %level = fetchData(%clientId, "LVL");

    if (!fetchData(%clientId, "currentlyRemorting"))
    {
        if (%level > %remortStep * 4 + 99)
        {
            %sealValue = GetTotalSealValue();
            if (%remortStep <= %sealValue)
            {
                %castPos = DoRemort(%clientId);
                %extraDelay = 0.22;
                %returnFlag = True;
                Client::sendMessage(%clientId, $MsgBeige, "Congratulations! You have successfully remorted.");
            }
            else
            {
                %returnFlag = False;
                Client::sendMessage(%clientId, $MsgWhite, "You are being held back by the seal. Help break the seal to increase the remort cap. Try searching around Kronos for a solution.");
            }
        }
        else
        {
            %returnFlag = False;
            Client::sendMessage(%clientId, $MsgWhite, "You are not strong enough to remort yet. Reach level " @ (%remortStep * 4 + 100) @ " to remort.");
        }
    }
    else
    {
        %returnFlag = False;
        Client::sendMessage(%clientId, $MsgWhite, "You are already in the process of remorting.");
    }
}

	if(%index == 22)
	{
		//full heal self spell

		Client::sendMessage(%clientId, $MsgBeige, "Fully healing self");

		setHP(%clientId, fetchData(%clientId, "MaxHP"));

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 23 || %index == 24 || %index == 31)
	{
		//23 = mass heal spell
		//24 = mass full heal spell
		//31 = mass shield spell

		%b = $Spell::radius[%index] * 2;
		%set = newObject("set", SimSet);
		%n = containerBoxFillSet(%set, $SimPlayerObjectType, GameBase::getPosition(%clientId), %b, %b, %b, 0);

		Group::iterateRecursive(%set, DoBoxFunction, %clientId, %index, %w2);
		deleteObject(%set);

		%overrideEndSound = True;

		%returnFlag = True;
	}
	if(%index == 25)
	{
		//shield self spell

		Client::sendMessage(%clientId, $MsgBeige, "Shielding self");

		UpdateBonusState(%clientId, $Spell::damageValue[%index], $Spell::ticks[%index]);

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 58 || %index == 59)
	{
//Air Blast
//Air Warp

		%b = $Spell::radius[%index] * 2;
		%set = newObject("set", SimSet);
		%n = containerBoxFillSet(%set, $SimPlayerObjectType, GameBase::getPosition(%clientId), %b, %b, %b, 0);

		Group::iterateRecursive(%set, DoBoxFunction, %clientId, %index, %w2);
		deleteObject(%set);

		%overrideEndSound = True;

		%returnFlag = True;
	}
	
if(%index == 26 || %index == 27 || %index == 28 || %index == 29 || %index == 30 || %index == 49)
	{
		//shield self or other (LOS) 1st, 2nd, 3rd, 4th, 5th, godly

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Shielding " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		UpdateBonusState(%id, $Spell::damageValue[%index], $Spell::ticks[%index]);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
	if(%index == 32)
	{
		//mimic spell
		if(Zone::getType(fetchData(%clientId, "zone")) == "PROTECTED")
		{
			Client::sendMessage(%clientId, $MsgRed, "You can't cast mimic in protected territory.");
			%overrideEndsound = True;
			%returnFlag = False;
		}
		else
		{
			%id = Player::getClient(%castObj);
			if(getObjectType(%castObj) == "Player")
			{
				%skilltype = $SkillType[$Spell::keyword[%index]];
				%troll = fetchData(%id, "LVL") + floor(getRandom() * ($PlayerSkill[%id, %skilltype] + ($PlayerSkill[%id, $SkillSpellResistance] * (1/2)) ));
				%yroll = fetchData(%clientId, "LVL") + floor(getRandom() * $PlayerSkill[%clientId, %skilltype]);

				if(%yroll > %troll)
				{
// ** this code used to put all your items into storage upon mimic.
//					%max = getNumItems();
//					for(%i = 0; %i < %max; %i++)
//					{
//						%checkItem = getItemData(%i);
//						%checkItemCount = Player::getItemCount(%clientId, %checkItem);
//						if(%checkItemCount)
//						{
//							%b = %checkItem;
//							if(%b.className == "Equipped")
//								%b = String::getSubStr(%b, 0, String::len(%b)-1);
//			
//							storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), %b, %checkItemCount));
//							Player::setItemCount(%clientId, %checkItem, 0);
//						}
//					}
					storeData(%clientId, "RACE", fetchData(%id, "RACE"));
					storeData(%clientId, "isMimic", True);
				
					UpdateTeam(%clientId);
					RefreshAll(%clientId);
				
					%castPos = GameBase::getPosition(%clientId);
					%returnFlag = True;
				}
				else
				{
					Client::sendMessage(%clientId, $MsgBeige, "Mimic failed.");
					%overrideEndsound = True;
					%returnFlag = False;
				}
			}
			else
			{
				Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
				%overrideEndsound = True;
				%returnFlag = False;
			}
		}
	}
	if(%index == 33)
	{
		//mass transport spell

		%zoneId = GetZoneByKeywords(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			%b = $Spell::radius[%index] * 2;
			%set = newObject("set", SimSet);
			%n = containerBoxFillSet(%set, $SimPlayerObjectType, GameBase::getPosition(%clientId), %b, %b, %b, 0);

			Group::iterateRecursive(%set, DoBoxFunction, %clientId, %index, %zoneId);
			deleteObject(%set);

			%overrideEndSound = True;

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Mass Transportation failed.");
			%returnFlag = False;
		}
	}
	if(%index == 41)
	{//windbooster
		%b = GameBase::getRotation(%clientId);
		%c1 = Cap($PlayerSkill[%clientId, $SkillType[boost]] * 1.8 + 20,75,1000);
		%c2 = %c1/3;
		%mom = Vector::getFromRot( %b, %c1, %c2 );
		Player::applyImpulse(%clientId, %mom);
		%returnFlag = True;
	}
	if(%index == 42)
	{//stop
		Item::setVelocity(%clientId, "0 0 10");
		%returnFlag = True;
	}
	if(%index == 43)
	{//airfist
		%player = Client::getOwnedObject(%ClientId);
		if(GameBase::getLOSinfo(%player, 3))
		{
			%id = Player::getClient($los::object);
			if(!(Player::isAiControlled(%id) && GameBase::getTeam(%id) == GameBase::getTeam(%TrueClientId)))
			{
            		%b = GameBase::getRotation(%clientId);
				%c1 = Cap($PlayerSkill[%clientId, $SkillType[advshove]]/4 + 30,30,500);
				%c2 = %c1/4;
				%mom = Vector::getFromRot( %b, %c1, %c2 );
				Player::applyImpulse(%id, %mom);
				//IHitHim(%clientid,%id,%c1 / 10);
				%returnFlag = True;	
			}
		}
        	else
        	{
			%returnFlag = False;	
		}
	}
	if(%index == 44)
	{//lightstep
		%weight = round(Cap($PlayerSkill[%clientId, $SkillType[lightstep]] /3 + 40,40,3000));
		%amount = "MaxWeight " @ %weight; 
		

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Lightstepping " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		UpdateBonusState(%id, %amount, $Spell::ticks[%index]);
		refreshAll(%id);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
	if(%index == 45)
	{//advshove
		%player = Client::getOwnedObject(%ClientId);
		if(GameBase::getLOSinfo(%player, 5))
		{
			%id = Player::getClient($los::object);
			if(!(Player::isAiControlled(%id) && GameBase::getTeam(%id) == GameBase::getTeam(%TrueClientId)))
			{
            		%b = GameBase::getRotation(%clientId);
				%c1 = Cap($PlayerSkill[%clientId, $SkillType[advshove]]/3 + 70,70,850);
				%c2 = %c1/4;
				%mom = Vector::getFromRot( %b, %c1, %c2 );
				Player::applyImpulse(%id, %mom);
				//IHitHim(%clientid,%id,%c1 / 10);
				%returnFlag = True;	
			}
		}
        	else
        	{
			%returnFlag = False;	
		}
	}
	if(%index == 46)
	{//tornado      if only remort looked this cool
		if(%castPos != "")
		{
			%xpos=getword(%castpos,0);
			%ypos=getword(%castpos,1);
			%zpos=getword(%castpos,2);
			%counter = 5;
			schedule("playSound(LaunchET, \"" @ %castPos @ "\");",%counter);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+85 @ "\", False, " @ %index @ ");",%counter+0.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+80 @ "\", False, " @ %index @ ");",%counter+0.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+75 @ "\", False, " @ %index @ ");",%counter+0.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+70 @ "\", False, " @ %index @ ");",%counter+0.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+65 @ "\", False, " @ %index @ ");",%counter+1.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+60 @ "\", False, " @ %index @ ");",%counter+1.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+55 @ "\", False, " @ %index @ ");",%counter+1.8);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+50 @ "\", False, " @ %index @ ");",%counter+2.1);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+45 @ "\", False, " @ %index @ ");",%counter+2.4);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+40 @ "\", False, " @ %index @ ");",%counter+2.7);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+35 @ "\", False, " @ %index @ ");",%counter+3.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+30 @ "\", False, " @ %index @ ");",%counter+3.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+25 @ "\", False, " @ %index @ ");",%counter+3.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+20 @ "\", False, " @ %index @ ");",%counter+3.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+15 @ "\", False, " @ %index @ ");",%counter+4.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+4.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+5 @ "\", False, " @ %index @ ");",%counter+4.8);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+5.1);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+5 @ "\", False, " @ %index @ ");",%counter+5.4);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+5.7);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+15 @ "\", False, " @ %index @ ");",%counter+6.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+20 @ "\", False, " @ %index @ ");",%counter+6.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+25 @ "\", False, " @ %index @ ");",%counter+6.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+30 @ "\", False, " @ %index @ ");",%counter+6.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+25 @ "\", False, " @ %index @ ");",%counter+7.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+20 @ "\", False, " @ %index @ ");",%counter+7.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+15 @ "\", False, " @ %index @ ");",%counter+7.8);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+8.1);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+5 @ "\", False, " @ %index @ ");",%counter+8.4);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+8.7);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+9.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+9.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+9.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+9.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+10.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+10.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+10.8);
			Client::sendMessage(%clientId, $MsgBeige, "Run for Cover.");
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
	if(%index == 50)
	{//Ion Blast
		if(%castPos != "")
		{
			%minrad = 0;
			%maxrad = 50;
			for(%i = 0; %i <= 30; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb20\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 5, %player);
			}
			for(%i = 0; %i <= 30; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb21\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 5, %player);
			}
			for(%i = 0; %i <= 25; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb1\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 4, %player);
			}
			for(%i = 0; %i <= 25; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb6\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 4, %player);
			}
			for(%i = 0; %i <= 20; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb17\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 3, %player);
			}
			for(%i = 0; %i <= 20; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %newPos @ "\", False, " @ %index @ ");", %i / 3, %player);
			}
			for(%i = 0; %i <= 10; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb22\", \"" @ %newPos @ "\", True, " @ %index @ ");", %i / 2.5, %player);
			}
			for(%i = 0; %i <= 10; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);

				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2) + (%i / 4);
		
				%newPos = %xPos @ " " @ %yPos @ " " @ %zPos;

				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb23\", \"" @ %newPos @ "\", True, " @ %index @ ");", %i / 2.5, %player);
			}
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb22\", \"" @ %newPos @ "\", True, " @ %index @ ");", 0.5, %player);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb23\", \"" @ %newPos @ "\", True, " @ %index @ ");", 0.5, %player);
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 51)
	{//Psynergy Liquifier
		if(%castPos != "")
		{
			%minrad = 0;
			%maxrad = 12;
			for(%i=0;%i<25;%i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);
				%xPos = GetWord(%tempPos, 0) + GetWord(%castPos, 0);
				%yPos = GetWord(%tempPos, 1) + GetWord(%castPos, 1);
				%zPos = GetWord(%castPos, 2)+600;
				schedule("Liquifier::fireLaser("@%player@","@%xPos@","@%yPos@","@%zPos@");",%i/15);
			}
			%xpos=getword(%castpos,0);
			%ypos=getword(%castpos,1);
			%zpos=getword(%castpos,2);
			%newPos = %xPos @ " " @ %yPos @ " " @ %zPos+1.4;
			for(%i=0;%i<4;%i++)
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb23\", \"" @ %newPos @ "\", true, " @ %index @ ");",%i/5);
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
	function Liquifier::fireLaser(%player,%x,%y,%z)
{
	%pos = %x@" "@%y@" "@%z;
	%turret = Liquifier::deployTurret(%player,%pos);
	%trans = GameBase::getMuzzleTransform(%turret);
	%vel = "0 0 0";
	%trans1= getWord(%trans,0);
	%trans2= getWord(%trans,1);
	%trans3= getWord(%trans,2);
//	%trans4= getWord(%trans,3);
//	%trans5= getWord(%trans,4);
//	%trans6= getWord(%trans,5);
	%trans4= (getRandom()*2-getRandom()*3)/80;
	%trans5= (getRandom()*2-getRandom()*3)/80;
	%trans6= -1.3-getRandom()/50;
	%trans7= getWord(%trans,6);
	%trans8= getWord(%trans,7);
	%trans9= getWord(%trans,8);
	%trans10=%x;
	%trans11=%y;
	%trans12=%z;
	%trans = %trans1 @" "@ %trans2 @" "@ %trans3 @" "@ %trans4 @" "@ %trans5 @" "@ %trans6 @" "@ %trans7 @" "@ %trans8 @" "@ %trans9 @" "@ %trans10 @" "@ %trans11 @" "@ %trans12;
	Projectile::spawnProjectile("HeatLaser",%trans,%turret,%vel);
	Projectile::spawnProjectile("HeatLaser",%trans,%turret,%vel);
	Projectile::spawnProjectile("HeatLaser",%trans,%turret,%vel);
}
function Liquifier::deployTurret(%player,%pos)
{
	%client = Player::getClient(%player);
	%rot = "0 0 0";
	%turret = newObject("Option","Turret",HeatLaserCannon,true);
	addToSet("MissionCleanup",%turret);
	GameBase::setTeam(%turret,GameBase::getTeam(%player));
	GameBase::setPosition(%turret,%pos);
	GameBase::setRotation(%turret,%rot);
	Client::setOwnedObject(%client,%turret);
	Client::setOwnedObject(%client,%player);
	schedule("GameBase::setDamageLevel("@%turret@",999);",0.1);
	return %turret;
}
function Turret::objectiveDestroyed() {}


	if(%index == 47)
	{//heavystep
		%weight = round(Cap($PlayerSkill[%clientId, $SkillType[heavystep]] /5 + 20,20,3000));
		%amount = "MaxWeight " @ %weight * -1; 
		

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			Client::sendMessage(%TrueClientId, $MsgWhite, "Unable to find target.");
			//%id = %ClientId;

		Client::sendMessage(%clientId, $MsgBeige, "Heavystepping " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

		UpdateBonusState(%id, %amount, $Spell::ticks[%index]);
		refreshAll(%id);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}

	if(%index == 48)
	{//apocalypse      if only remort looked this cool
		if(%castPos != "")
		{
			%xpos=getword(%castpos,0);
			%ypos=getword(%castpos,1);
			%zpos=getword(%castpos,2);
			%counter = 5;
			schedule("playSound(LaunchET, \"" @ %castPos @ "\");",%counter);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+85 @ "\", False, " @ %index @ ");",%counter+0.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos+30 @ " " @ %zpos+75 @ "\", False, " @ %index @ ");",%counter+0.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos-30 @ " " @ %zpos+65 @ "\", False, " @ %index @ ");",%counter+0.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+55 @ "\", False, " @ %index @ ");",%counter+0.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos @ " " @ %zpos+45 @ "\", False, " @ %index @ ");",%counter+1.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos @ " " @ %zpos+35 @ "\", False, " @ %index @ ");",%counter+1.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+25 @ "\", False, " @ %index @ ");",%counter+1.8);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos+30 @ " " @ %zpos+15 @ "\", False, " @ %index @ ");",%counter+2.1);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos-30 @ " " @ %zpos+5 @ "\", False, " @ %index @ ");",%counter+2.4);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos-30 @ " " @ %ypos-30 @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+2.7);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos+30 @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+3.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+20 @ "\", False, " @ %index @ ");",%counter+3.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+3.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+3.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos+30 @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+4.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos-30 @ " " @ %zpos+20 @ "\", False, " @ %index @ ");",%counter+4.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+4.8);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+5.1);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+5.4);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos @ " " @ %zpos+20 @ "\", False, " @ %index @ ");",%counter+5.7);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+6.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+6.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos+30 @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+6.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+6.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos-30 @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+7.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+7.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos+30 @ " " @ %zpos+10 @ "\", False, " @ %index @ ");",%counter+7.8);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+8.1);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos-30 @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+8.4);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+8.7);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos-30 @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+9.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+9.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos+30 @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+9.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+9.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos+30 @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+10.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+10.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos @ " " @ %zpos+0 @ "\", False, " @ %index @ ");",%counter+10.8);
			Client::sendMessage(%clientId, $MsgBeige, "Run for Cover.");
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
	if(%index == 66)
	{//apocalypse      if only remort looked this cool
		if(%castPos != "")
		{
			%xpos=getword(%castpos,0);
			%ypos=getword(%castpos,1);
			%zpos=getword(%castpos,2);
			%counter = 5;
			schedule("playSound(LaunchET, \"" @ %castPos @ "\");",%counter);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+85 @ "\", True, " @ %index @ ");",%counter+0.0);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos @ " " @ %ypos+30 @ " " @ %zpos+75 @ "\", True, " @ %index @ ");",%counter+0.3);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos-30 @ " " @ %zpos+65 @ "\", True, " @ %index @ ");",%counter+0.6);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb10\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+55 @ "\", True, " @ %index @ ");",%counter+0.9);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos+30 @ " " @ %ypos @ " " @ %zpos+45 @ "\", True, " @ %index @ ");",%counter+1.2);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos-30 @ " " @ %ypos @ " " @ %zpos+35 @ "\", True, " @ %index @ ");",%counter+1.5);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos @ " " @ %ypos @ " " @ %zpos+25 @ "\", True, " @ %index @ ");",%counter+1.8);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos+30 @ " " @ %ypos+30 @ " " @ %zpos+15 @ "\", True, " @ %index @ ");",%counter+2.1);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %xpos+30 @ " " @ %ypos-30 @ " " @ %zpos+5 @ "\", True, " @ %index @ ");",%counter+2.4);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %xpos-30 @ " " @ %ypos-30 @ " " @ %zpos+0 @ "\", True, " @ %index @ ");",%counter+2.7);
			Client::sendMessage(%clientId, $MsgBeige, "Run for Cover.");
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	Player::setAnimation(%clientId, 39);

	if(!%overrideEndSound)
	{
		if(%extraDelay == "")
			playSound($Spell::endSound[%index], %castPos);
		else
			schedule("playSound(" @ $Spell::endSound[%index] @ ", \"" @ %castPos @ "\");", %extraDelay);
	}


	//==================================================================

	%skilltype = $SkillType[$Spell::keyword[%index]];
	if(%returnFlag == True)
	{
		storeData(%clientId, "SpellCastStep", 2);

		if(%skilltype == $SkillNeutralCasting || %skilltype == $SkillDefensiveCasting)
			UseSkill(%clientId, %skilltype, True, True);
		UseSkill(%clientId, $SkillEnergy, True, True);

		return True;
	}
	else if(%returnFlag == False)
	{
		storeData(%clientId, "SpellCastStep", 2);

		UseSkill(%clientId, %skilltype, False, True);
		UseSkill(%clientId, $SkillEnergy, False, True);

		return False;
	}
}

function CreateAndDetBomb(%clientId, %b, %castPos, %doDamage, %index)
{
	dbecho($dbechoMode, "CreateAndDetBomb(" @ %clientId @ ", " @ %b @ ", " @ %castPos @ ", " @ %index @ ")");

	%player = Client::getOwnedObject(%clientId);

	%bomb = newObject("", "Mine", %b);

	addToSet("MissionCleanup", %bomb);

//	GameBase::Throw(%bomb, %player, 0, false);
	GameBase::setPosition(%bomb, %castPos);
	
	if(%doDamage)
		SpellRadiusDamage(%clientId, %castPos, %index);

	playSound($Spell::endSound[%index], %castPos);
}

function SpellDamage(%clientId, %targetId, %damageValue, %index)
{
	dbecho($dbechoMode, "SpellDamage(" @ %clientId @ ", " @ %targetId @ ", " @ %damageValue @ ", " @ %index @ ")");

	GameBase::virtual(%targetId, "onDamage", $SpellDamageType, %damageValue, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, $Spell::keyword[%index]);
}

function SpellRadiusDamage(%clientId, %pos, %index)
{
	dbecho($dbechoMode, "SpellRadiusDamage(" @ %clientId @ ", " @ %pos @ ", " @ %index @ ")");

	%b = $Spell::radius[%index] * 2;
	%set = newObject("set", SimSet);
	%n = containerBoxFillSet(%set, $SimPlayerObjectType, %pos, %b, %b, %b, 0);

	Group::iterateRecursive(%set, DoSpellDamage, %clientId, %pos, %index);
	deleteObject(%set);
}
function DoSpellDamage(%object, %clientId, %pos, %index)
{
	dbecho($dbechoMode, "DoSpellDamage(" @ %object @ ", " @ %clientId @ ", " @ %pos @ ", " @ %index @ ")");

	%id = Player::getClient(%object);

	%percMin = 5;
	%percMax = 100;

	%dist = Vector::getDistance(%pos, GameBase::getPosition(%id));

	if(%dist <= $Spell::radius[%index])
	{
		%newDamage = SpellCalcRadiusDamage(%dist, $Spell::radius[%index], $Spell::damageValue[%index], %percMin, %percMax);
		SpellDamage(%clientId, %id, %newDamage, %index);
	}
}

function SpellCalcRadiusDamage(%dist, %radius, %dmg, %percMin, %percMax)
{
	dbecho($dbechoMode, "SpellCalcRadiusDamage(" @ %dist @ ", " @ %radius @ ", " @ %dmg @ ", " @ %percMin @ ", " @ %percMax @ ")");

	%newdmg = %dmg - (%dist * (%dmg / %radius));

	%p = (%newdmg * 100) / %dmg;

	if(%p < %percMin)
		%p = %percMin;
	else if(%p > %percMax)
		%p = %percMax;

	%newdmg = (%p * %dmg) / 100;

	return %newdmg;
}

function GetBestSpell(%clientId, %type, %semiRandomSpell)
{
	dbecho($dbechoMode, "GetBestSpell(" @ %clientId @ ", " @ %type @ ", " @ %semiRandomSpell @ ")");

	%wdelay = 10;	//weights
	%wrecov = 0.5;

	%bestSpell = -1;
	%backupSpell = "";
	%highest = 0.1;

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		if(SkillCanUse(%clientId, $Spell::keyword[%i]))
		{
			if(fetchData(%clientId, "MANA") >= $Spell::manaCost[%i])
			{
				%d = ( ($Spell::delay[%i] / %wdelay) + ($Spell::recoveryTime[%i] / %wrecov) );
				%x = (100 / %d) * $Spell::refVal[%i];
				%v =  %x * %type;

				if(%semiRandomSpell)
				{
					%r = getRandom() * 100;
					%rr = getRandom() * 100;
				}
				else
				{
					%r = 1;
					%rr = 0;
				}

				if(%v > %highest)
				{
					if(%r > %rr)
					{
						%bestSpell = %i;
						%highest = %v;
					}
					else
						%backupSpell = %i;
				}
			}
		}
	}
	if(%bestSpell == -1 && %backupSpell != "")
		%bestSpell = %backupSpell;

	return %bestSpell;
}

function CalcSpellMiss(%clientId, %targetId, %index)
{
	dbecho($dbechoMode, "CalcSpellMiss(" @ %clientId @ ", " @ %targetId @ ", " @ %index @ ")");

	%range = $Spell::LOSrange[%index];
	%dist = Vector::getDistance(GameBase::getPosition(%clientId), GameBase::getPosition(%targetId));

	%m = floor((getRandom() * %range)) + (%range / 6);

	//echo(%dist @ " / " @ %range @ " : --> " @ %m);
	if(%m > %dist)
		return False;
	else
		return True;
}

function sendDoneRecovMsg(%clientId)
{
	//this function is here just to make the schedule command where this is called easier to read
	Client::sendMessage(%clientId, $MsgBeige, "You are ready to cast.");
}

function DoBoxFunction(%object, %clientId, %index, %extra)
{
	dbecho($dbechoMode, "DoBoxFunction(" @ %object @ ", " @ %clientId @ ", " @ %index @ ", " @ %extra @ ")");

	%id = Player::getClient(%object);

	if(%index == 23)
	{
		if(GameBase::getTeam(%clientId) == GameBase::getTeam(%id))
		{
			Client::sendMessage(%clientId, $MsgBeige, "Mass Healing " @ Client::getName(%id));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, "You are being Mass Healed by " @ Client::getName(%clientId));

			%r = $Spell::damageValue[%index] / $TribesDamageToNumericDamage;
			refreshHP(%id, %r);

			%castPos = GameBase::getPosition(%id);

			CreateAndDetBomb(%clientId, "Bomb10", %castPos, False, %index);
			playSound($Spell::endSound[%index], %castPos);
		}
	}
if(%index == 58 || %index == 59)
	{
if(%id != %clientId)
{
//Air Blast	
//Air Warp	
		
			Client::sendMessage(%clientId, $MsgBeige, "Air Blasting " @ Client::getName(%id));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, "You are being blasted into the air by " @ Client::getName(%clientId));
                        %b = GameBase::getRotation(%Id)       ;
				%c1 = Cap(150 + ($PlayerSkill[%clientId, $SkillNeutralCasting] / 2), 0, 3500);
				%c2 = %c1 / 3;
				%mom = Vector::getFromRot( %b, %c1, %c2 );
		
				Player::applyImpulse(%id, %mom);
			%castPos = GameBase::getPosition(%id);
			
		
	}
}
	if(%index == 24)
	{
		if(GameBase::getTeam(%clientId) == GameBase::getTeam(%id))
		{
			Client::sendMessage(%clientId, $MsgBeige, "Mass Fully Healing " @ Client::getName(%id));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, "You are being Mass Fully Healed by " @ Client::getName(%clientId));

			setHP(%id, fetchData(%id, "MaxHP"));

			%castPos = GameBase::getPosition(%id);

			CreateAndDetBomb(%clientId, "Bomb10", %castPos, False, %index);
			playSound($Spell::endSound[%index], %castPos);
		}
	}
	if(%index == 31)
	{
		if(GameBase::getTeam(%clientId) == GameBase::getTeam(%id))
		{
			Client::sendMessage(%clientId, $MsgBeige, "Shielding " @ Client::getName(%id));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.");

			UpdateBonusState(%id, $Spell::damageValue[%index], $Spell::ticks[%index]);

			%castPos = GameBase::getPosition(%id);

			CreateAndDetBomb(%clientId, "Bomb10", %castPos, False, %index);
			playSound($Spell::endSound[%index], %castPos);
		}
	}
	if(%index == 33)
	{
		if(IsInCommaList(fetchData(%clientId, "grouplist"), Client::getName(%id)) && IsInCommaList(fetchData(%id, "grouplist"), Client::getName(%clientId)) || %clientId == %id)
		{
			Client::sendMessage(%clientId, $MsgBeige, "Transporting " @ Client::getName(%id) @ " to " @ Zone::getDesc(%extra));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is transporting you to " @ Zone::getDesc(%extra));

			//teleport

			%system = Object::getName(%extra);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);

			%castPos = TeleportToMarker(%id, "Zones\\" @ %system @ "\\DropPoints", False, True);
			CheckAndBootFromArena(%id);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			if(!fetchData(%id, "invisible"))
				GameBase::startFadeIn(%id);

			Player::setDamageFlash(%id, 0.7);

			%extraDelay = 0.22;
			schedule("playSound(" @ $Spell::endSound[%index] @ ", \"" @ %castPos @ "\");", %extraDelay);
		}
	}
}

function SpellCanCast(%clientId, %keyword)
{
	dbecho($dbechoMode, "SpellCanCast(" @ %clientId @ ", " @ %keyword @ ")");

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		if(String::ICompare($Spell::keyword[%i], %keyword) == 0)
		{
			if(SkillCanUse(%clientId, $Spell::keyword[%i]))
			{
				if(fetchData(%clientId, "MaxMANA") >= $Spell::manaCost[%i])
					return True;
			}
		}
	}
	return False;
}
function SpellCanCastNow(%clientId, %keyword)
{
	dbecho($dbechoMode, "SpellCanCastNow(" @ %clientId @ ", " @ %keyword @ ")");

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		if(String::ICompare($Spell::keyword[%i], %keyword) == 0)
		{
			if(SkillCanUse(%clientId, $Spell::keyword[%i]))
			{
				if(fetchData(%clientId, "MANA") >= $Spell::manaCost[%i])
					return True;
			}
		}
	}
	return False;
}
