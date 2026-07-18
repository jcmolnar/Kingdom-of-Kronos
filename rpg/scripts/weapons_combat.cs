//============================================================================
// weapons_combat.cs — split from weapons.cs (original lines 1-890 and 3047-3227 (EOF; includes SLOT PURGE tombstone + commented base_weapons exec))
// Extracted 2026-07-17 at commit 4aa0a3d. Mechanical text move, no behavior change.
// Stat tables + combat engine (MeleeAttack..GetDelay) + cost gen + DPS diagnostics.
// MUST exec BEFORE the weapons.cs datablocks: fireTime = GetDelay(...) runs at
// datablock registration time.
// exec'd by weapons.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
$fireTimeDelay = 0;

// Bot melee diagnostics: set true (server console) to log bot swings, LOS
// results, attack-loop ticks/exits, and bot->player damage arrivals.
// Used to pin the July 2026 "bots stop attacking until player moves" bug
// (zone-0 mismatch killing AI::ContinuousAttack).
$MeleeDebug = false;

$RustyDamageAmp = 0.7;
$RustyWeightAmp = 1.5;
$RustyCostAmp = 0.3;

$RangeTable[$AxeAccessoryType] = 4;
$RangeTable[$SwordAccessoryType] = 4;
$RangeTable[$PolearmAccessoryType] = 5;
$RangeTable[$BludgeonAccessoryType] = 4;

$DelayFactorTable[$RingAccessoryType] = "0.0";
$DelayFactorTable[$BodyAccessoryType] = "0.0";
$DelayFactorTable[$BootsAccessoryType] = "0.0";
$DelayFactorTable[$BackAccessoryType] = "0.0";
$DelayFactorTable[$ShieldAccessoryType] = "0.0";
$DelayFactorTable[$TalismanAccessoryType] = "0.0";
$DelayFactorTable[$AxeAccessoryType] = "1.1";
$DelayFactorTable[$SwordAccessoryType] = "0.9";
$DelayFactorTable[$PolearmAccessoryType] = "1.2";
$DelayFactorTable[$BludgeonAccessoryType] = "1.1";
$DelayFactorTable[$RangedAccessoryType] = "1.0";
$DelayFactorTable[$ProjectileAccessoryType] = "1.0";
$DelayFactorTable[$HeadAccessoryType] = "0.0";

$CostFactorTable[$RingAccessoryType] = "1.0";
$CostFactorTable[$BodyAccessoryType] = "1.0";
$CostFactorTable[$BootsAccessoryType] = "1.0";
$CostFactorTable[$BackAccessoryType] = "1.0";
// AdminBoots has no mana cost for flight
$CostFactorTable["AdminBoots"] = "0.0";
$CostFactorTable["AdminBoots0"] = "0.0";
$CostFactorTable[$ShieldAccessoryType] = "50.0";
$CostFactorTable[$TalismanAccessoryType] = "1.0";
$CostFactorTable[$SwordAccessoryType] = "0.7";
$CostFactorTable[$AxeAccessoryType] = "0.7";
$CostFactorTable[$PolearmAccessoryType] = "0.7";
$CostFactorTable[$BludgeonAccessoryType] = "0.7";
$CostFactorTable[$RangedAccessoryType] = "1.0";
$CostFactorTable[$ProjectileAccessoryType] = "0.01";
$CostFactorTable[$HeadAccessoryType] = "1";

//****************************************************************************************************

$AccessoryVar[Hatchet, $AccessoryType] = $AxeAccessoryType;
$AccessoryVar[Club, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[Knife, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[Dagger, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[PickAxe, $AccessoryType] = $AxeAccessoryType;
$AccessoryVar[CastingBlade, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[CrystalSpear, $AccessoryType] = $PolearmAccessoryType;
//Custom Weap Types
$AccessoryVar[RustyIronBlade, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[ButterKnife, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[CrackedStick, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[SharpIronBlade, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[LongKnife, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[IronStick, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[IronBroadSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[IronSpear, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[IronMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[SteelBroadSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[SteelSpear, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[SteelMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[SteelLongSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[SteelPike, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[SteelHammer, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[GoldenLongSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[GoldenPike, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[SteelWarHammer, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[GoldenBastardSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[CrystalPike, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[GoldenWarHammer, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[CrystalBastardSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[TemperedCrystalBastardSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[CrystalTrident, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[TemperedCrystalTrident, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[GoldenDivineMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[CrystalDivineMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[CrystalClaymore, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[DiamondTrident, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[DiamondDivineMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[DiamondClaymore, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[DiamondDeathSpear, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[DiamondBrainSpiller, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[DiamondLegendSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[SealFighterBlade, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[SealGuardianBlade, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[DiamondLegendSpear, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[DiamondLegendMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[BlackDiamondDreamSword, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[BlackDiamondDreamSpear, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[BlackDiamondDreamMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[BlackDiamondAtomSplitter, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[BlackDiamondAtomPiercer, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[BlackDiamondAtomSmasher, $AccessoryType] = $BludgeonAccessoryType;

$AccessoryVar[Hatchet, $SpecialVar] = "6 60";			//12 (5)
//Custom Weap Damages
$AccessoryVar[RustyIronBlade, $SpecialVar] = "6 9";			//12 (5)
$AccessoryVar[SharpIronBlade, $SpecialVar] = "6 16";
$AccessoryVar[IronBroadSword, $SpecialVar] = "6 27";
$AccessoryVar[SteelBroadSword, $SpecialVar] = "6 40";
$AccessoryVar[SteelLongSword, $SpecialVar] = "6 55";
$AccessoryVar[GoldenLongSword, $SpecialVar] = "6 70";
$AccessoryVar[GoldenBastardSword, $SpecialVar] = "6 90";
$AccessoryVar[CrystalBastardSword, $SpecialVar] = "6 115";
$AccessoryVar[TemperedCrystalBastardSword, $SpecialVar] = "6 140";
$AccessoryVar[CrystalClaymore, $SpecialVar] = "6 165";
$AccessoryVar[DiamondClaymore, $SpecialVar] = "6 195";
$AccessoryVar[DiamondLegendSword, $SpecialVar] = "6 220";
$AccessoryVar[SealFighterBlade, $SpecialVar] = "6 220";
$AccessoryVar[SealGuardianBlade, $SpecialVar] = "6 220";
$AccessoryVar[BlackDiamondDreamSword, $SpecialVar] = "6 300";
$AccessoryVar[BlackDiamondAtomSplitter, $SpecialVar] = "6 450";
$AccessoryVar[CrystalSpear, $SpecialVar] = "6 60";
//.................................................................................
$AccessoryVar[Club, $SpecialVar] = "6 55";			//12 (3)
//Custom Weap Damages
$AccessoryVar[CrackedStick, $SpecialVar] = "6 13";
$AccessoryVar[IronStick, $SpecialVar] = "6 23";
$AccessoryVar[IronMace, $SpecialVar] = "6 36";
$AccessoryVar[SteelMace, $SpecialVar] = "6 49";
$AccessoryVar[SteelHammer, $SpecialVar] = "6 62";
$AccessoryVar[SteelWarHammer, $SpecialVar] = "6 77";
$AccessoryVar[GoldenWarHammer, $SpecialVar] = "6 95";
$AccessoryVar[GoldenDivineMace, $SpecialVar] = "6 120";
$AccessoryVar[CrystalDivineMace, $SpecialVar] = "6 150";
$AccessoryVar[DiamondDivineMace, $SpecialVar] = "6 180";
$AccessoryVar[DiamondBrainSpiller, $SpecialVar] = "6 210";
$AccessoryVar[DiamondLegendMace, $SpecialVar] = "6 240";
$AccessoryVar[BlackDiamondDreamMace, $SpecialVar] = "6 330";
$AccessoryVar[BlackDiamondAtomSmasher, $SpecialVar] = "6 490";
//.................................................................................
$AccessoryVar[PickAxe, $SpecialVar] = "6 3";			//12 (4)
$AccessoryVar[Knife, $SpecialVar] = "6 55";			//18 (1)
$AccessoryVar[Dagger, $SpecialVar] = "6 23";			//23 (3)
//Custom Weap Damages
$AccessoryVar[ButterKnife, $SpecialVar] = "6 5";
$AccessoryVar[LongKnife, $SpecialVar] = "6 13";
$AccessoryVar[IronSpear, $SpecialVar] = "6 34";
$AccessoryVar[SteelSpear, $SpecialVar] = "6 48";
$AccessoryVar[SteelPike, $SpecialVar] = "6 65";
$AccessoryVar[GoldenPike, $SpecialVar] = "6 83";
$AccessoryVar[CrystalPike, $SpecialVar] = "6 105";
$AccessoryVar[CrystalTrident, $SpecialVar] = "6 130";
$AccessoryVar[TemperedCrystalTrident, $SpecialVar] = "6 160";
$AccessoryVar[DiamondTrident, $SpecialVar] = "6 190";
$AccessoryVar[DiamondDeathSpear, $SpecialVar] = "6 220";
$AccessoryVar[DiamondLegendSpear, $SpecialVar] = "6 260";
$AccessoryVar[BlackDiamondDreamSpear, $SpecialVar] = "6 350";
$AccessoryVar[BlackDiamondAtomPiercer, $SpecialVar] = "6 520";
//.................................................................................
$AccessoryVar[CastingBlade, $SpecialVar] = "6 18";
//.................................................................................

$AccessoryVar[Hatchet, $Weight] = 5;
//Custom weap weights
$AccessoryVar[RustyIronBlade, $Weight] = "4";
$AccessoryVar[SharpIronBlade, $Weight] = "4.5";
$AccessoryVar[IronBroadSword, $Weight] = "5";
$AccessoryVar[SteelBroadSword, $Weight] = "5.5";
$AccessoryVar[SteelLongSword, $Weight] = "6";
$AccessoryVar[GoldenLongSword, $Weight] = "6.5";
$AccessoryVar[GoldenBastardSword, $Weight] = "7";
$AccessoryVar[CrystalBastardSword, $Weight] = "7.5";
$AccessoryVar[TemperedCrystalBastardSword, $Weight] = "8";
$AccessoryVar[CrystalClaymore, $Weight] = "8.5";
$AccessoryVar[DiamondClaymore, $Weight] = "8.5";
$AccessoryVar[DiamondLegendSword, $Weight] = "4";
$AccessoryVar[SealFighterBlade, $Weight] = "4";
$AccessoryVar[SealGuardianBlade, $Weight] = "4";
$AccessoryVar[BlackDiamondDreamSword, $Weight] = "3.5";
$AccessoryVar[BlackDiamondAtomSplitter, $Weight] = "3";
$AccessoryVar[CrystalSpear, $Weight] = 5;
//.................................................................................
$AccessoryVar[Club, $Weight] = 4;
//Custom weap weights
$AccessoryVar[CrackedStick, $Weight] = 4.5;
$AccessoryVar[IronStick, $Weight] = 5;
$AccessoryVar[IronMace, $Weight] = 5.5;
$AccessoryVar[SteelMace, $Weight] = 6;
$AccessoryVar[SteelHammer, $Weight] = 6.5;
$AccessoryVar[SteelWarHammer, $Weight] = 7;
$AccessoryVar[GoldenWarHammer, $Weight] = 7.5;
$AccessoryVar[GoldenDivineMace, $Weight] = 8;
$AccessoryVar[CrystalDivineMace, $Weight] = 8.5;
$AccessoryVar[DiamondDivineMace, $Weight] = 9;
$AccessoryVar[DiamondBrainSpiller, $Weight] = 9;
$AccessoryVar[DiamondLegendMace, $Weight] = 4;
$AccessoryVar[BlackDiamondDreamMace, $Weight] = 3.5;
$AccessoryVar[BlackDiamondAtomSmasher, $Weight] = 3;
//.................................................................................
$AccessoryVar[PickAxe, $Weight] = 4;
$AccessoryVar[Knife, $Weight] = 4;
$AccessoryVar[Dagger, $Weight] = 3;
//Custom weap weights
$AccessoryVar[ButterKnife, $Weight] = 3.5;
$AccessoryVar[LongKnife, $Weight] = 3.5;
$AccessoryVar[IronSpear, $Weight] = 4.5;
$AccessoryVar[SteelSpear, $Weight] = 5;
$AccessoryVar[SteelPike, $Weight] = 5.5;
$AccessoryVar[GoldenPike, $Weight] = 6;
$AccessoryVar[CrystalPike, $Weight] = 6.5;
$AccessoryVar[CrystalTrident, $Weight] = 7;
$AccessoryVar[TemperedCrystalTrident, $Weight] = 7.5;
$AccessoryVar[DiamondTrident, $Weight] = 8;
$AccessoryVar[DiamondDeathSpear, $Weight] = 8;
$AccessoryVar[DiamondLegendSpear, $Weight] = 4;
$AccessoryVar[BlackDiamondDreamSpear, $Weight] = 3.5;
$AccessoryVar[BlackDiamondAtomPiercer, $Weight] = 3;

$AccessoryVar[CastingBlade, $Weight] = "0.5";

$AccessoryVar[Hatchet, $MiscInfo] = "A hatchet";
$AccessoryVar[Club, $MiscInfo] = "A club";
$AccessoryVar[Knife, $MiscInfo] = "A knife";
$AccessoryVar[Dagger, $MiscInfo] = "A dagger";
$AccessoryVar[PickAxe, $MiscInfo] = "A pick axe";
$AccessoryVar[CastingBlade, $MiscInfo] = "Selects the best spell and casts it.  Used only for bots.";
$AccessoryVar[CrystalSpear, $MiscInfo] = "A Crystal Spear";
//Custom weap Descriptions
$AccessoryVar[RustyIronBlade, $MiscInfo] = "An unreliable dull Iron Blade";
$AccessoryVar[ButterKnife, $MiscInfo] = "A simple and almost useless Butter Knife";
$AccessoryVar[CrackedStick, $MiscInfo] = "A Cracked Stick that could fall apart any minute";
$AccessoryVar[SharpIronBlade, $MiscInfo] = "A well taken care of Iron Blade";
$AccessoryVar[LongKnife, $MiscInfo] = "A finely crafted Long Knife";
$AccessoryVar[IronStick, $MiscInfo] = "A durable stick made from iron";
$AccessoryVar[IronBroadSword, $MiscInfo] = "A sharp high quality iron broadsword";
$AccessoryVar[IronSpear, $MiscInfo] = "A long spear made from iron";
$AccessoryVar[IronMace, $MiscInfo] = "A durable mace made from iron";
$AccessoryVar[SteelBroadSword, $MiscInfo] = "A sharp high quality steel broadsword";
$AccessoryVar[SteelSpear, $MiscInfo] = "A long spear made from steel";
$AccessoryVar[SteelMace, $MiscInfo] = "A durable mace made from steel";
$AccessoryVar[SteelLongSword, $MiscInfo] = "A sharp high quality steel longsword";
$AccessoryVar[SteelPike, $MiscInfo] = "A long pike made from steel";
$AccessoryVar[SteelHammer, $MiscInfo] = "A huge hammer made from steel";
$AccessoryVar[GoldenLongSword, $MiscInfo] = "A sharp high quality golden longsword";
$AccessoryVar[GoldenPike, $MiscInfo] = "A long pike made from gold";
$AccessoryVar[SteelWarHammer, $MiscInfo] = "A huge war hammer made from steel";
$AccessoryVar[GoldenBastardSword, $MiscInfo] = "A devastatingly strong golden bastardsword";
$AccessoryVar[CrystalPike, $MiscInfo] = "A long pike made from crystal";
$AccessoryVar[GoldenWarHammer, $MiscInfo] = "A huge war hammer made from gold";
$AccessoryVar[CrystalBastardSword, $MiscInfo] = "A devastatingly strong crystal bastardsword";
$AccessoryVar[TemperedCrystalBastardSword, $MiscInfo] = "A devastatingly strong tempered crystal bastardsword";
$AccessoryVar[CrystalTrident, $MiscInfo] = "A destructive trident made from crystal";
$AccessoryVar[TemperedCrystalTrident, $MiscInfo] = "A destructive trident made from tempered crystal";
$AccessoryVar[GoldenDivineMace, $MiscInfo] = "A golden mace enfused with the power of the gods";
$AccessoryVar[CrystalDivineMace, $MiscInfo] = "A crystal mace enfused with the power of the gods";
$AccessoryVar[CrystalClaymore, $MiscInfo] = "An extremely devastatingly strong crystal claymore";
$AccessoryVar[DiamondTrident, $MiscInfo] = "A destructive trident made from diamond";
$AccessoryVar[DiamondDivineMace, $MiscInfo] = "A diamond mace enfused with the power of the gods";
$AccessoryVar[DiamondClaymore, $MiscInfo] = "An extremely devastatingly strong diamond claymore";
$AccessoryVar[DiamondDeathSpear, $MiscInfo] = "A destructive spear whose soul purpose is to cause death";
$AccessoryVar[DiamondBrainSpiller, $MiscInfo] = "A diamond mace that is extremely good at spilling the brains of its foes";
$AccessoryVar[DiamondLegendSword, $MiscInfo] = "A blessed sword of legends";
$AccessoryVar[SealFighterBlade, $MiscInfo] = "A sacred blade wielded by Seal Fighters";
$AccessoryVar[SealGuardianBlade, $MiscInfo] = "A sacred blade wielded by Seal Guardians";
$AccessoryVar[DiamondLegendSpear, $MiscInfo] = "A blessed spear of legends";
$AccessoryVar[DiamondLegendMace, $MiscInfo] = "A blessed mace of legends";
$AccessoryVar[BlackDiamondDreamSword, $MiscInfo] = "A sword of dreams made from the rare black diamond";
$AccessoryVar[BlackDiamondDreamSpear, $MiscInfo] = "A spear of dreams made from the rare black diamond";
$AccessoryVar[BlackDiamondDreamMace, $MiscInfo] = "A mace of dreams made from the rare black diamond";
$AccessoryVar[BlackDiamondAtomSplitter, $MiscInfo] = "A sword, made from the rare black diamond, it's so sharp, it can split atoms";
$AccessoryVar[BlackDiamondAtomPiercer, $MiscInfo] = "A spear, made from the rare black diamond, it's so sharp, it can pierce atoms";
$AccessoryVar[BlackDiamondAtomSmasher, $MiscInfo] = "A mace, made from the rare black diamond, it's so sharp, it can smash atoms";

//NOTE: See shopping.cs for the shopIndexes

$SkillType[Hatchet] = $SkillSlashing;
$SkillType[Club] = $SkillBludgeoning;
$SkillType[Knife] = $SkillPiercing;
$SkillType[Dagger] = $SkillPiercing;
$SkillType[PickAxe] = $SkillPiercing;
$SkillType[CastingBlade] = $SkillPiercing;
$SkillType[CrystalSpear] = $SkillPiercing;
//Custom Weap Skilltypes
$SkillType[RustyIronBlade] = $SkillSlashing;
$SkillType[ButterKnife] = $SkillPiercing;
$SkillType[CrackedStick] = $SkillBludgeoning;
$SkillType[SharpIronBlade] = $SkillSlashing;
$SkillType[LongKnife] = $SkillPiercing;
$SkillType[IronStick] = $SkillBludgeoning;
$SkillType[IronBroadSword] = $SkillSlashing;
$SkillType[IronSpear] = $SkillPiercing;
$SkillType[IronMace] = $SkillBludgeoning;
$SkillType[SteelBroadSword] = $SkillSlashing;
$SkillType[SteelSpear] = $SkillPiercing;
$SkillType[SteelMace] = $SkillBludgeoning;
$SkillType[SteelLongSword] = $SkillSlashing;
$SkillType[SteelPike] = $SkillPiercing;
$SkillType[SteelHammer] = $SkillBludgeoning;
$SkillType[GoldenLongSword] = $SkillSlashing;
$SkillType[GoldenPike] = $SkillPiercing;
$SkillType[SteelWarHammer] = $SkillBludgeoning;
$SkillType[GoldenBastardSword] = $SkillSlashing;
$SkillType[CrystalPike] = $SkillPiercing;
$SkillType[GoldenWarHammer] = $SkillBludgeoning;
$SkillType[CrystalBastardSword] = $SkillSlashing;
$SkillType[TemperedCrystalBastardSword] = $SkillSlashing;
$SkillType[CrystalTrident] = $SkillPiercing;
$SkillType[TemperedCrystalTrident] = $SkillPiercing;
$SkillType[GoldenDivineMace] = $SkillBludgeoning;
$SkillType[CrystalDivineMace] = $SkillBludgeoning;
$SkillType[CrystalClaymore] = $SkillSlashing;
$SkillType[DiamondTrident] = $SkillPiercing;
$SkillType[DiamondDivineMace] = $SkillBludgeoning;
$SkillType[DiamondClaymore] = $SkillSlashing;
$SkillType[DiamondDeathSpear] = $SkillPiercing;
$SkillType[DiamondBrainSpiller] = $SkillBludgeoning;
$SkillType[DiamondLegendSword] = $SkillSlashing;
$SkillType[SealFighterBlade] = $SkillSlashing;
$SkillType[SealGuardianBlade] = $SkillSlashing;
$SkillType[DiamondLegendSpear] = $SkillPiercing;
$SkillType[DiamondLegendMace] = $SkillBludgeoning;
$SkillType[BlackDiamondDreamSword] = $SkillSlashing;
$SkillType[BlackDiamondDreamSpear] = $SkillPiercing;
$SkillType[BlackDiamondDreamMace] = $SkillBludgeoning;
$SkillType[BlackDiamondAtomSplitter] = $SkillSlashing;
$SkillType[BlackDiamondAtomPiercer] = $SkillPiercing;
$SkillType[BlackDiamondAtomSmasher] = $SkillBludgeoning;

$WeaponRange[Sling] = 35;
$WeaponRange[ShortBow] = 120;
$WeaponRange[LongBow] = 200;
$WeaponRange[ElvenBow] = 260;
$WeaponRange[CompositeBow] = 360;
$WeaponRange[LightCrossbow] = 300;
$WeaponRange[AeolusWing] = 400;
$WeaponRange[HeavyCrossbow] = 500;
$WeaponRange[RepeatingCrossbow] = 280;
$WeaponRange[CastingBlade] = 1000;	//will swing from anywhere...BUT will be able to snipe with beam

$WeaponDelay[Sling] = 1.5;

$ProjRestrictions[SmallRock] = ",Sling,";
$ProjRestrictions[BasicArrow] = ",ShortBow,LongBow,ElvenBow,CompositeBow,RShortBow,";
$ProjRestrictions[SheafArrow] = ",ShortBow,LongBow,ElvenBow,CompositeBow,RShortBow,";
$ProjRestrictions[BladedArrow] = ",ShortBow,LongBow,ElvenBow,CompositeBow,RShortBow,";
$ProjRestrictions[LightQuarrel] = ",LightCrossbow,HeavyCrossbow,RLightCrossbow,";
$ProjRestrictions[HeavyQuarrel] = ",LightCrossbow,HeavyCrossbow,RLightCrossbow,";
$ProjRestrictions[ShortQuarrel] = ",RepeatingCrossbow,";
$ProjRestrictions[StoneFeather] = ",AeolusWing,";
$ProjRestrictions[MetalFeather] = ",AeolusWing,";
$ProjRestrictions[Talon] = ",AeolusWing,";
$ProjRestrictions[CeraphumsFeather] = ",AeolusWing,";

function GenerateAllWeaponCosts()
{
	dbecho($dbechoMode, "GenerateAllWeaponCosts()");

	//All item costs that need to be Generated must be in a function, later called after all files have been exec'd.
	//This function, among other similar ones, is run once only in server.cs.

	$ItemCost[Hatchet] = 50000;
	$ItemCost[Club] = 50000;
	$ItemCost[Knife] = 50000;
	$ItemCost[Dagger] = 0;
	$ItemCost[CastingBlade] = 0;
	$ItemCost[CrystalSpear] = 0;
//Custom weap Costs
	$ItemCost[RustyIronBlade] = GenerateItemCost(RustyIronBlade);
	$ItemCost[ButterKnife] = GenerateItemCost(ButterKnife);
	$ItemCost[CrackedStick] = GenerateItemCost(CrackedStick);
	$ItemCost[SharpIronBlade] = GenerateItemCost(SharpIronBlade);
	$ItemCost[LongKnife] = GenerateItemCost(LongKnife);
	$ItemCost[IronStick] = GenerateItemCost(IronStick);
	$ItemCost[IronBroadSword] = GenerateItemCost(IronBroadSword);
	$ItemCost[IronSpear] = GenerateItemCost(IronSpear);
	$ItemCost[IronMace] = GenerateItemCost(IronMace);
	$ItemCost[SteelBroadSword] = GenerateItemCost(SteelBroadSword);
	$ItemCost[SteelSpear] = GenerateItemCost(SteelSpear);
	$ItemCost[SteelMace] = GenerateItemCost(SteelMace);
	$ItemCost[SteelLongSword] = GenerateItemCost(SteelLongSword);
	$ItemCost[SteelPike] = GenerateItemCost(SteelPike);
	$ItemCost[SteelHammer] = GenerateItemCost(SteelHammer);
	$ItemCost[GoldenLongSword] = GenerateItemCost(GoldenLongSword);
	$ItemCost[GoldenPike] = GenerateItemCost(GoldenPike);
	$ItemCost[SteelWarHammer] = GenerateItemCost(SteelWarHammer);
	$ItemCost[GoldenBastardSword] = GenerateItemCost(GoldenBastardSword);
	$ItemCost[CrystalPike] = GenerateItemCost(CrystalPike);
	$ItemCost[GoldenWarHammer] = GenerateItemCost(GoldenWarHammer);
	$ItemCost[CrystalBastardSword] = GenerateItemCost(CrystalBastardSword);
	$ItemCost[TemperedCrystalBastardSword] = GenerateItemCost(TemperedCrystalBastardSword);
	$ItemCost[CrystalTrident] = GenerateItemCost(CrystalTrident);
	$ItemCost[TemperedCrystalTrident] = GenerateItemCost(TemperedCrystalTrident);
	$ItemCost[GoldenDivineMace] = GenerateItemCost(GoldenDivineMace);
	$ItemCost[CrystalDivineMace] = GenerateItemCost(CrystalDivineMace);
	$ItemCost[CrystalClaymore] = GenerateItemCost(CrystalClaymore);
	$ItemCost[DiamondTrident] = GenerateItemCost(DiamondTrident);
	$ItemCost[DiamondDivineMace] = GenerateItemCost(DiamondDivineMace);
	$ItemCost[DiamondClaymore] = GenerateItemCost(DiamondClaymore);
	$ItemCost[DiamondDeathSpear] = GenerateItemCost(DiamondDeathSpear);
	$ItemCost[DiamondBrainSpiller] = GenerateItemCost(DiamondBrainSpiller);
	$ItemCost[DiamondLegendSword] = GenerateItemCost(DiamondLegendSword);
	$ItemCost[DiamondLegendSpear] = GenerateItemCost(DiamondLegendSpear);
	$ItemCost[DiamondLegendMace] = GenerateItemCost(DiamondLegendMace);
	$ItemCost[BlackDiamondDreamSword] = GenerateItemCost(BlackDiamondDreamSword);
	$ItemCost[BlackDiamondDreamSpear] = GenerateItemCost(BlackDiamondDreamSpear);
	$ItemCost[BlackDiamondDreamMace] = GenerateItemCost(BlackDiamondDreamMace);
	$ItemCost[BlackDiamondAtomSplitter] = GenerateItemCost(BlackDiamondAtomSplitter);
	$ItemCost[BlackDiamondAtomPiercer] = GenerateItemCost(BlackDiamondAtomPiercer);
	$ItemCost[BlackDiamondAtomSmasher] = GenerateItemCost(BlackDiamondAtomSmasher);

	$ItemCost[RHatchet] = round($ItemCost[Hatchet] * $RustyCostAmp);
	$ItemCost[RBroadSword] = round($ItemCost[BroadSword] * $RustyCostAmp);
	$ItemCost[RLongSword] = round($ItemCost[LongSword] * $RustyCostAmp);
	$ItemCost[RClub] = round($ItemCost[Club] * $RustyCostAmp);
	$ItemCost[RSpikedClub] = round($ItemCost[SpikedClub] * $RustyCostAmp);
	$ItemCost[RKnife] = round($ItemCost[Knife] * $RustyCostAmp);
	$ItemCost[RDagger] = round($ItemCost[Dagger] * $RustyCostAmp);
	$ItemCost[RShortSword] = round($ItemCost[ShortSword] * $RustyCostAmp);
	$ItemCost[RPickAxe] = round($ItemCost[PickAxe] * $RustyCostAmp);
	$ItemCost[RShortBow] = round($ItemCost[ShortBow] * $RustyCostAmp);
	$ItemCost[RLightCrossbow] = round($ItemCost[LightCrossbow] * $RustyCostAmp);
	$ItemCost[RWarAxe] = round($ItemCost[WarAxe] * $RustyCostAmp);
}

//****************************************************************************************************

function MeleeAttack(%player, %length, %weapon)
{
	dbecho($dbechoMode, "MeleeAttack(" @ %player @ ", " @ %length @ ")");

	%clientId = Player::getClient(%player);
	if(%clientId == "")
		%clientId = 0;

	// BELT WEAPON identity swap (BeltWeapons.cs): if the mounted weapon is the
	// equipped belt weapon's SHELL, the swing counts as the belt weapon - the
	// damage path reads every stat from name-keyed tables, so this one swap
	// makes it a first-class weapon everywhere downstream.
	%beltWeapon = fetchData(%clientId, "EquippedBeltWeapon");
	if(%beltWeapon != "" && %beltWeapon != "0" && $BeltWeapon[%beltWeapon, "Shell"] == %weapon)
	{
		%weapon = %beltWeapon;
		%length = GetRange(%weapon);
	}

	//==== ANTI-SPAM CHECK, CAUSE FOR SPAM UNKNOWN ==========
	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastFireTime < $fireTimeDelay)
	{
		if($MeleeDebug && Player::isAiControlled(%clientId))
			echo("[MELEE DEBUG] " @ GetClientOrBotName(%clientId) @ " swing BLOCKED by anti-spam");
		return;
	}
	%clientId.lastFireTime = %time;
	//=======================================================

	// CRITICAL: Block attacks during skill upgrade RefreshAll to prevent attack spam exploit
	// When upgrading skills while swinging, RefreshAll re-triggers onFire causing repeated attacks
	if($SkillUpgradeRefreshScheduled[%clientId] == "true")
	{
		if($MeleeDebug && Player::isAiControlled(%clientId))
			echo("[MELEE DEBUG] " @ GetClientOrBotName(%clientId) @ " swing BLOCKED by SkillUpgradeRefreshScheduled");
		return;
	}

	$los::object = "";
	if(GameBase::getLOSinfo(%player, %length))
	{
		%obj = getObjectType($los::object);
		if($MeleeDebug && Player::isAiControlled(%clientId))
			echo("[MELEE DEBUG] " @ GetClientOrBotName(%clientId) @ " swing: LOS hit " @ %obj @ " (" @ $los::object @ ")");
		if(%obj == "Player")
		{
			GameBase::virtual($los::object, "onDamage", $BulletDamageType, 1.0, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, %weapon);
		}
	}
	else if($MeleeDebug && Player::isAiControlled(%clientId))
		echo("[MELEE DEBUG] " @ GetClientOrBotName(%clientId) @ " swing: LOS MISS (nothing in " @ %length @ "u along view)");

	PostAttack(%clientId, %weapon);
	
	// DUAL WIELD HOOK: Trigger off-hand attack if dual wielding is enabled
	// Only trigger if this is a primary weapon attack (not already an off-hand attack)
	if(DualWield::IsEnabled(%clientId) && !$DualWield::IsOffHandAttack[%clientId])
	{
		// Mark that we're doing an off-hand attack to prevent infinite recursion
		$DualWield::IsOffHandAttack[%clientId] = true;
		
		// Trigger animation
		DualWield::OnPrimaryFire(%clientId, %weapon);
		
		// Schedule off-hand damage
		schedule("DualWield::FireOffHandMelee(" @ %clientId @ ", " @ %player @ ", " @ %weapon @ ");", $DualWield::OffHandDelayOffset);
		
		// Schedule clearing the flag
		schedule("$DualWield::IsOffHandAttack[" @ %clientId @ "] = false;", $DualWield::OffHandDelayOffset + 0.1);
	}
}

// Custom attack function for Void weapons with spell animations
function VoidWeaponAttack(%player, %length, %weapon)
{
	dbecho($dbechoMode, "VoidWeaponAttack(" @ %player @ ", " @ %length @ ", " @ %weapon @ ")");

	%clientId = Player::getClient(%player);
	if(%clientId == "")
		%clientId = 0;

	// BELT WEAPON identity swap (BeltWeapons.cs) - same as MeleeAttack above
	%beltWeapon = fetchData(%clientId, "EquippedBeltWeapon");
	if(%beltWeapon != "" && %beltWeapon != "0" && $BeltWeapon[%beltWeapon, "Shell"] == %weapon)
	{
		%weapon = %beltWeapon;
		%length = GetRange(%weapon);
	}

	//==== ANTI-SPAM CHECK, CAUSE FOR SPAM UNKNOWN ==========
	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastFireTime < $fireTimeDelay)
		return;
	%clientId.lastFireTime = %time;
	//=======================================================

	// CRITICAL: Block attacks during skill upgrade RefreshAll to prevent attack spam exploit
	if($SkillUpgradeRefreshScheduled[%clientId] == "true")
		return;

	// Perform damage check
	$los::object = "";
	if(GameBase::getLOSinfo(%player, %length))
	{
		%obj = getObjectType($los::object);
		if(%obj == "Player")
		{
			GameBase::virtual($los::object, "onDamage", $BulletDamageType, 1.0, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, %weapon);
		}
	}
	
	// Always calculate bomb position in front of player (not at player position)
	// This ensures the visual effect appears in front of the player regardless of whether a target was hit
	%playerPos = GameBase::getPosition(%player);
	%playerRot = GameBase::getRotation(%player);
	%forwardDist = 3.0; // Distance in front of player for bomb effect
	
	// Use Vector::getFromRot to calculate forward offset from rotation
	%forwardOffset = Vector::getFromRot(%playerRot, %forwardDist);
	
	// Add forward offset to player position to get bomb position
	%hitPos = Vector::add(%playerPos, %forwardOffset);
	
	// CRITICAL: If position calculation failed, use player position as fallback
	// This ensures bomb always shows, even during LCK or other special states
	if(%hitPos == "" || %hitPos == -1)
	{
		%hitPos = %playerPos;
	}
	
	// Track attack count for Bomb27 special effect (every 50 attacks)
	%attackCount = fetchData(%clientId, "VoidWeaponAttackCount");
	if(%attackCount == "" || %attackCount == -1)
		%attackCount = 0;
	%attackCount++;
	storeData(%clientId, "VoidWeaponAttackCount", %attackCount);
	
	// Add spell animation effect - randomly select one of 3 bomb types (Bomb21, Bomb25, Bomb28)
	// Every 50 attacks, use Bomb27 instead
	%bombType = "";
	if((%attackCount % 50) == 0)
	{
		// Every 50th attack, use Bomb27
		%bombType = "Bomb27";
	}
	else
	{
		// Randomly select from 3 bomb types
		%rand = floor(getRandom() * 3);
		if(%rand == 0)
			%bombType = "Bomb21";
		else if(%rand == 1)
			%bombType = "Bomb25";
		else
			%bombType = "Bomb28";
	}
	
	// Always create bomb effect (even if position is at player position)
	// This ensures visual effects show during LCK and other special states
	if(%hitPos != "" && %hitPos != -1)
	{
		CreateAndDetBomb(%clientId, %bombType, %hitPos, False, 0);
		// Play custom sound for Void weapon explosions
		playSound(shockExplosion, %hitPos);
	}
	else
	{
		// Last resort: use player position directly if all else fails
		if(%playerPos != "" && %playerPos != -1)
		{
			CreateAndDetBomb(%clientId, %bombType, %playerPos, False, 0);
			playSound(shockExplosion, %playerPos);
		}
	}

	PostAttack(%clientId, %weapon);
	
	// DUAL WIELD HOOK: Trigger off-hand attack if dual wielding is enabled
	// Only trigger if this is a primary weapon attack (not already an off-hand attack)
	if(DualWield::IsEnabled(%clientId) && !$DualWield::IsOffHandAttack[%clientId])
	{
		// Mark that we're doing an off-hand attack to prevent infinite recursion
		$DualWield::IsOffHandAttack[%clientId] = true;
		
		// Trigger animation
		DualWield::OnPrimaryFire(%clientId, %weapon);
		
		// Schedule off-hand damage
		schedule("DualWield::FireOffHandMelee(" @ %clientId @ ", " @ %player @ ", " @ %weapon @ ");", $DualWield::OffHandDelayOffset);
		
		// Schedule clearing the flag
		schedule("$DualWield::IsOffHandAttack[" @ %clientId @ "] = false;", $DualWield::OffHandDelayOffset + 0.1);
	}
}

function ProjectileAttack(%clientId, %weapon, %vel)
{
	dbecho($dbechoMode, "ProjectileAttack(" @ %clientId @ ", " @ %weapon @ ", " @ %vel @ ")");

	//==== ANTI-SPAM CHECK, CAUSE FOR SPAM UNKNOWN ==========
	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastFireTime <= $fireTimeDelay)
		return;
	%clientId.lastFireTime = %time;
	//=======================================================

	if(fetchData(%clientId, "LoadedProjectile " @ %weapon) == "")
		return;
	if(Player::getItemCount(%clientId, fetchData(%clientId, "LoadedProjectile " @ %weapon)) <= 0)
		return;

//	%losflag = "";
//	if(GameBase::getLOSinfo(Client::getOwnedObject(%clientId), 50000))
//	{
//		%target = $los::object;
//		%obj = getObjectType(%target);
//		%dist = Vector::getDistance(GameBase::getPosition(%clientId), GameBase::getPosition(Player::getClient(%target)));
//
//		if(%dist <= GetRange(%weapon))
//		{
//	        if(%obj == "Player")
//			{
//				%factor = sqrt(%dist) / 6.454;
//				%vel = %dist / %factor;
//	
//				%zoffset = 0.25;
//				%losflag = True;
//			}
//		}
//	}
//	if(!%losflag)
//	{
		%zoffset = 0.44;
//	}

	%arrow = newObject("", "Item", fetchData(%clientId, "LoadedProjectile " @ %weapon), 1, false);
	%arrow.owner = %clientId;
	%arrow.delta = 1;
	%arrow.weapon = %weapon;

	addToSet("MissionCleanup", %arrow);
	// CRITICAL: stamp a pop token so the 30s deferred pop can't hit a recycled ID (item.cs Item::pop)
	%arrow.popToken = %arrow @ "_pop_" @ getSimTime();
  	schedule("Item::Pop(" @ %arrow @ ", \"" @ %arrow.popToken @ "\");", 30, %arrow);

	//double-check stuff
	$ProjectileDoubleCheck[%arrow] = True;
	schedule("$ProjectileDoubleCheck[" @ %arrow @ "] = \"\";", 1.5, %arrow);

	%rot = GameBase::getRotation(%clientId);
	%newrot = (GetWord(%rot, 0) - %zoffset) @ " " @ GetWord(%rot, 1) @ " " @ GetWord(%rot, 2);

	GameBase::setRotation(%clientId, %newrot);
	GameBase::throw(%arrow, Client::getOwnedObject(%clientId), %vel, false);
	GameBase::setRotation(%arrow, %rot);
	GameBase::setRotation(%clientId, %rot);

	Player::decItemCount(%clientId, fetchData(%clientId, "LoadedProjectile " @ %weapon));

	PostAttack(%clientId, %weapon);
}

function PickAxeSwing(%player, %length, %weapon)
{
	dbecho($dbechoMode, "PickAxeSwing(" @ %player @ ", " @ %length @ ")");

	%clientId = Player::getClient(%player);
	if(%clientId == "")
		%clientId = 0;

	//==== ANTI-SPAM CHECK, CAUSE FOR SPAM UNKNOWN ==========
	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastFireTime <= $fireTimeDelay)
		return;
	%clientId.lastFireTime = %time;
	//=======================================================
	
	// CRITICAL: Block attacks during skill upgrade RefreshAll to prevent attack spam exploit
	if($SkillUpgradeRefreshScheduled[%clientId] == "true")
		return;

	$los::object = "";
	if(GameBase::getLOSinfo(%player, %length))
	{
		%target = $los::object;
		%obj = getObjectType(%target);
		%type = GameBase::getDataName(%target);

            if(%type == "Crystal")
		{
			%brflag = String::findSubStr(fetchData(%clientId, "RACE"), "Human");	//must be human to mine
			if(Vector::getDistance(%clientId.lastMinePos, GameBase::getPosition(%clientId)) > 1.0 && %brflag != -1)
			{
				playSound(SoundHitore, GameBase::getPosition(%target));	//vectrex, modified by JI

				%score = DoRandomMining(%clientId, %target);
				if(%score != "")
				{
					Player::incItemCount(%clientId, %score, 1);
					RefreshAll(%clientId);
					Client::sendMessage(%clientId, 0, "You found " @ %score.description @ ".");

					if( floor(getRandom() * 10) == 5)
						%clientId.lastMinePos = GameBase::getPosition(%clientId);
				}
				UseSkill(%clientId, $SkillMining, True, True);
			}
			else
				playSound(SoundHitore2, GameBase::getPosition(%target));
		}		

		if(%obj == "Player")
			GameBase::virtual(%target, "onDamage", "", 1.0, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, %weapon);
	}

	PostAttack(%clientId, %weapon);
}

function PostAttack(%clientId, %weapon)
{
	dbecho($dbechoMode, "PostAttack(" @ %clientId @ ", " @ %weapon @ ")");

	if($postAttackGraphBar)
	{
		%t = GetDelay(%weapon);
		%ticks = 30;
		%chunks = 10;

		%chunklen = floor(%ticks / %chunks);
		%d = %t / %chunks;

		for(%i = 0; %i <= %chunks; %i++)
			schedule("bottomprint(" @ %clientId @ ", \" \" @ String::create(\"�\", " @ %ticks @ " - (" @ %chunklen @ " * " @ %i @ ")) @ \"\", " @ %d @ " + 0.25);", %d * %i);
	}

	if(%weapon == CastingBlade)
	{
		// Initialize castingBladeBeat if it doesn't exist
		if(%clientId.castingBladeBeat == "")
			%clientId.castingBladeBeat = 0;
		
		%x = floor(%clientId.castingBladeBeat);
		if(%x != 0)
		{
			if(%x == 1)
				playSound(MClip5, GameBase::getPosition(%clientId));
			else if(%x == 2)
				playSound(MClip6, GameBase::getPosition(%clientId));
		}

		%x++;
		if(%x > 2) %x = 1;

		%clientId.castingBladeBeat = %x;
	}
}

function DoRandomMining(%clientId, %crystal)
{
	dbecho($dbechoMode, "DoRandomMining(" @ %clientId @ ", " @ %crystal @ ")");

	%lastscore = "";
	for(%i = 1; $ItemList[Mining, %i] != ""; %i++)
	{
		%w1 = GetWord($ItemList[Mining, %i], 1) - %crystal.bonus[%i];
		%n = Cap( (%w1 * getRandom()) + (%w1 / 2), 0, %w1);
		%r = 1 + ($PlayerSkill[%clientId, $SkillMining] * (1/10)) * getRandom();

		if(%n > %r)
			return %lastscore;

		%lastscore = GetWord($ItemList[Mining, %i], 0);
	}
	return %lastscore;
}

function GetRange(%weapon)
{
	dbecho($dbechoMode, "GetRange(" @ %weapon @ ")");

	%minRange = 2.0;
	if($WeaponRange[%weapon] != "")
		return %minRange + $WeaponRange[%weapon];
	else
		return %minRange + $RangeTable[$AccessoryVar[%weapon, $AccessoryType]];
}
function GetDelay(%weapon)
{
	dbecho($dbechoMode, "GetDelay(" @ %weapon @ ")");

	if($WeaponDelay[%weapon] != "")
		return $WeaponDelay[%weapon];
	else
	{
		%a = 4.0;
		%b = Cap($AccessoryVar[%weapon, $Weight] / %a, 0.5, "inf");
		%c = %b * $DelayFactorTable[$AccessoryVar[%weapon, $AccessoryType]];
		return %c;
	}
}

function GenerateItemCost(%item)
{
	dbecho($dbechoMode, "GenerateItemCost(" @ %item @ ")");

	// CRITICAL: AdminBoots should have cost of 0 for flight mana calculation
	// The engine uses item cost to calculate flight mana cost, so 0 = free flight
	if(%item == "AdminBoots" || %item == "AdminBoots0")
		return 0;

	if($HardcodedItemCost[%item] != "")
		return $HardcodedItemCost[%item];

	// AdminBoots has no mana cost for flight - check item name first, then fall back to AccessoryType
	if($CostFactorTable[%item] != "")
		%cft = $CostFactorTable[%item];
	else
		%cft = $CostFactorTable[$AccessoryVar[%item, $AccessoryType]];

	%a = GetDelay(%item);
	if(%a < 0.5 && %a != 0)
		%a = 0.5;
	else if(%a == 0)
		%a = 1;

	%b6 = AddItemSpecificPoints(%item, "6") * 1.2;	//ATK
	%b7 = AddItemSpecificPoints(%item, "7") / 6;	//DEF
	%b3 = AddItemSpecificPoints(%item, "3") / 6;	//MDEF

	%extracost = 0;
	for(%i = 1; $SmithCombo[%i] != ""; %i++)
	{
		for(%j = 0; (%w = GetWord($SmithComboResult[%i], %j)) != -1; %j+=2)
		{
			if(String::ICompare(%item, %w) == 0)
			{
				%n = GetWord($SmithComboResult[%i], %j+1);
				for(%k = 0; (%w2 = GetWord($SmithCombo[%i], %k)) != -1; %k+=2)
				{
					%n2 = GetWord($SmithCombo[%i], %k+1);
					%extracost += (GenerateItemCost(%w2) * %n2);
				}
				%extracost *= %n;
				break;
			}
		}
		if(%extracost > 0)
			break;
	}
	%extracost = %extracost * ($ResalePercentage / 100);
	
	%c = (%b6 + %b7 + %b3) / %a;
	%d = Cap(0.01 * pow(%c, 3.7), 0, "inf");
	%e = Cap(%d * %cft, 1, "inf");
	%f = floor(%e + %extracost);

	return %f;
}

//****************************************************************************************************
//   RUSTY IRON BLADE
//****************************************************************************************************

//====== "Projectiles" ======================================================


//===========================================================================================
//===========================================================================================
//===========================================================================================
//====================================             ==========================================
//====================================   RUSTIES   ==========================================
//====================================             ==========================================
//===========================================================================================
//===========================================================================================
//===========================================================================================

//Notes on smithed items and rusties:
//-To determine the cost of a final combined item, add up all the costs of the materials
// involved and divide by $RustyCostAmp.

//****************************************************************************************************
//   Weapon DPS Calculator
//****************************************************************************************************
function CalculateWeaponDPS(%weapon)
{
	// Get damage from SpecialVar (format: "6 <damage>")
	%specialVar = $AccessoryVar[%weapon, $SpecialVar];
	if(%specialVar == "" || %specialVar == -1)
		return -1;
	
	%damageStr = GetWord(%specialVar, 1);
	if(%damageStr == "" || %damageStr == -1)
		return -1;
	
	// GetRoll handles both dice rolls and plain numbers
	%damage = GetRoll(%damageStr);
	if(%damage == "" || %damage == -1)
		return -1;
	
	// Get attack delay
	%delay = GetDelay(%weapon);
	if(%delay == "" || %delay == -1 || %delay == 0)
		return -1;
	
	// Calculate DPS
	%dps = %damage / %delay;
	return %dps;
}

function ListAllWeaponDPS()
{
	echo("");
	echo("=========================================");
	echo("WEAPON DPS COMPARISON");
	echo("=========================================");
	echo("");
	
	// Array to store weapon data for sorting
	%weaponCount = 0;
	
	// Find all weapons (items with SpecialVar starting with "6" for ATK)
	// Check weapons.cs definitions
	%weaponList = "Hatchet RustyIronBlade SharpIronBlade IronBroadSword SteelBroadSword SteelLongSword GoldenLongSword GoldenBastardSword CrystalBastardSword TemperedCrystalBastardSword CrystalClaymore DiamondClaymore DiamondLegendSword BlackDiamondDreamSword BlackDiamondAtomSplitter Club CrackedStick IronStick IronMace SteelMace SteelHammer SteelWarHammer GoldenWarHammer GoldenDivineMace CrystalDivineMace DiamondDivineMace DiamondBrainSpiller DiamondLegendMace BlackDiamondDreamMace BlackDiamondAtomSmasher PickAxe Knife Dagger ButterKnife LongKnife IronSpear SteelSpear SteelPike GoldenPike CrystalPike CrystalTrident TemperedCrystalTrident DiamondTrident DiamondDeathSpear DiamondLegendSpear BlackDiamondDreamSpear BlackDiamondAtomPiercer CastingBlade TerminusEst AecoSeorei MorningStar WhiteDiamondVoidCutter WhiteDiamondVoidCrusher WhiteDiamondVoidImpaler";
	
	for(%i = 0; (%weapon = GetWord(%weaponList, %i)) != -1; %i++)
	{
		%specialVar = $AccessoryVar[%weapon, $SpecialVar];
		if(%specialVar != "" && %specialVar != -1)
		{
			%firstWord = GetWord(%specialVar, 0);
			if(%firstWord == "6")  // ATK type
			{
				%dps = CalculateWeaponDPS(%weapon);
				if(%dps != -1)
				{
					%damage = GetRoll(GetWord(%specialVar, 1));
					%delay = GetDelay(%weapon);
					%weaponType = $AccessoryVar[%weapon, $AccessoryType];
					
					// Store for sorting
					$WeaponDPSList[%weaponCount, "name"] = %weapon;
					$WeaponDPSList[%weaponCount, "dps"] = %dps;
					$WeaponDPSList[%weaponCount, "damage"] = %damage;
					$WeaponDPSList[%weaponCount, "delay"] = %delay;
					$WeaponDPSList[%weaponCount, "type"] = %weaponType;
					%weaponCount++;
				}
			}
		}
	}
	
	// Sort by DPS (bubble sort - simple but works)
	for(%i = 0; %i < %weaponCount - 1; %i++)
	{
		for(%j = 0; %j < %weaponCount - %i - 1; %j++)
		{
			if($WeaponDPSList[%j, "dps"] < $WeaponDPSList[%j + 1, "dps"])
			{
				// Swap
				%tempName = $WeaponDPSList[%j, "name"];
				%tempDPS = $WeaponDPSList[%j, "dps"];
				%tempDamage = $WeaponDPSList[%j, "damage"];
				%tempDelay = $WeaponDPSList[%j, "delay"];
				%tempType = $WeaponDPSList[%j, "type"];
				
				$WeaponDPSList[%j, "name"] = $WeaponDPSList[%j + 1, "name"];
				$WeaponDPSList[%j, "dps"] = $WeaponDPSList[%j + 1, "dps"];
				$WeaponDPSList[%j, "damage"] = $WeaponDPSList[%j + 1, "damage"];
				$WeaponDPSList[%j, "delay"] = $WeaponDPSList[%j + 1, "delay"];
				$WeaponDPSList[%j, "type"] = $WeaponDPSList[%j + 1, "type"];
				
				$WeaponDPSList[%j + 1, "name"] = %tempName;
				$WeaponDPSList[%j + 1, "dps"] = %tempDPS;
				$WeaponDPSList[%j + 1, "damage"] = %tempDamage;
				$WeaponDPSList[%j + 1, "delay"] = %tempDelay;
				$WeaponDPSList[%j + 1, "type"] = %tempType;
			}
		}
	}
	
	// Display sorted results
	echo("Rank | Weapon Name                    | Damage | Delay (s) | DPS      | Type");
	echo("-----|--------------------------------|--------|-----------|----------|----------");
	
	for(%i = 0; %i < %weaponCount; %i++)
	{
		%name = $WeaponDPSList[%i, "name"];
		%dps = $WeaponDPSList[%i, "dps"];
		%damage = $WeaponDPSList[%i, "damage"];
		%delay = $WeaponDPSList[%i, "delay"];
		%type = $WeaponDPSList[%i, "type"];
		
		// Format type name
		if(%type == $SwordAccessoryType)
			%typeName = "Sword";
		else if(%type == $PolearmAccessoryType)
			%typeName = "Polearm";
		else if(%type == $BludgeonAccessoryType)
			%typeName = "Bludgeon";
		else if(%type == $AxeAccessoryType)
			%typeName = "Axe";
		else
			%typeName = %type;
		
		// Format numbers
		%rank = %i + 1;
		%damageFormatted = FixDecimals(%damage);
		%delayFormatted = FixDecimals(%delay);
		%dpsFormatted = FixDecimals(%dps);
		
		// Pad name to 30 chars
		%namePadded = %name;
		%nameLen = String::len(%name);
		if(%nameLen < 30)
		{
			for(%k = %nameLen; %k < 30; %k++)
				%namePadded = %namePadded @ " ";
		}
		
		echo(%rank @ "    | " @ %namePadded @ " | " @ %damageFormatted @ "     | " @ %delayFormatted @ "      | " @ %dpsFormatted @ " | " @ %typeName);
	}
	
	echo("");
	echo("Total weapons: " @ %weaponCount);
	echo("=========================================");
	echo("");
	
	// Clean up
	for(%i = 0; %i < %weaponCount; %i++)
	{
		$WeaponDPSList[%i, "name"] = "";
		$WeaponDPSList[%i, "dps"] = "";
		$WeaponDPSList[%i, "damage"] = "";
		$WeaponDPSList[%i, "delay"] = "";
		$WeaponDPSList[%i, "type"] = "";
	}
}

// SLOT PURGE 2026-07-13: base_weapons.cs no longer exec'd - its 12 ItemData (5 stock ammo
// types, Grenade, and the 6 stock guns Chaingun..EnergyRifle) were never given, sold, or
// placed anywhere in KOK (only referenced by the un-exec'd Training_*.cs scripts and dead
// string/sound tables). Not registering them reclaims 12 of the ~256 ItemData indices.
// The file is kept on disk for reference; re-add this exec to restore stock weapons.
//exec("base_weapons.cs");