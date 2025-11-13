$fireTimeDelay = 0;

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
$AccessoryVar[BlackDiamondDreamSword, $SpecialVar] = "6 300";
$AccessoryVar[BlackDiamondAtomSplitter, $SpecialVar] = "6 450";
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
$AccessoryVar[BlackDiamondDreamSword, $Weight] = "3.5";
$AccessoryVar[BlackDiamondAtomSplitter, $Weight] = "3";
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

	//==== ANTI-SPAM CHECK, CAUSE FOR SPAM UNKNOWN ==========
	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastFireTime < $fireTimeDelay)
		return;
	%clientId.lastFireTime = %time;
	//=======================================================
		
	$los::object = "";
	if(GameBase::getLOSinfo(%player, %length))
	{
		%obj = getObjectType($los::object);
		if(%obj == "Player")
		{
			GameBase::virtual($los::object, "onDamage", "", 1.0, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, %weapon);
		}
	}

	PostAttack(%clientId, %weapon);
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
  	schedule("Item::Pop(" @ %arrow @ ");", 30, %arrow);

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
			schedule("bottomprint(" @ %clientId @ ", \" \" @ String::create(\"•\", " @ %ticks @ " - (" @ %chunklen @ " * " @ %i @ ")) @ \"\", " @ %d @ " + 0.25);", %d * %i);
	}

	if(%weapon == CastingBlade)
	{
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

	if($HardcodedItemCost[%item] != "")
		return $HardcodedItemCost[%item];

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

ItemImageData RustyIronBladeImage
{
	shapeFile  = "short_sword";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(RustyIronBlade);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData RustyIronBlade
{
	heading = "bWeapons";
	description = "Dull Iron Blade";
	className = "Weapon";
	shapeFile  = "short_sword";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = RustyIronBladeImage;
	price = 0;
	showWeaponBar = true;
};
function RustyIronBladeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(RustyIronBlade), RustyIronBlade);
}

function RustyIronBlade::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Rusty Iron Blade: <f0>Attack: <f2>9    <f0>Skill Slashing Req @ <f2>0    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$68    <f0>Weight: <f2>4 Lbs");
}
//****************************************************************************************************
//   SHARP IRON BLADE
//****************************************************************************************************

ItemImageData SharpIronBladeImage
{
	shapeFile  = "short_sword";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(SharpIronBlade);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData SharpIronBlade
{
	heading = "bWeapons";
	description = "Sharp Iron Blade";
	className = "Weapon";
	shapeFile  = "short_sword";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SharpIronBladeImage;
	price = 0;
	showWeaponBar = true;
};
function SharpIronBladeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SharpIronBlade), SharpIronBlade);
}

function SharpIronBlade::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Sharp Iron Blade: <f0>Attack: <f2>16    <f0>Skill Slashing Req @ <f2>45    <f0>Speed: <f2>1.01 Second    <f0>Price: <f2>$374    <f0>Weight: <f2>4.5 Lbs ");
}
//****************************************************************************************************
//   IRON BROADSWORD
//****************************************************************************************************

ItemImageData IronBroadSwordImage
{
	shapeFile  = "sword";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(IronBroadSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData IronBroadSword
{
	heading = "bWeapons";
	description = "Iron Broadsword";
	className = "Weapon";
	shapeFile  = "sword";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = IronBroadSwordImage;
	price = 0;
	showWeaponBar = true;
};
function IronBroadSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(IronBroadSword), IronBroadSword);
}

function IronBroadSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Iron Broad Sword: <f0>Attack: <f2>27    <f0>Skill Slashing Req @ <f2>90    <f0>Speed: <f2>1.12 Seconds    <f0>Price: <f2>$1,757    <f0>Weight: <f2>5 Lbs");
}
//****************************************************************************************************
//   STEEL BROADSWORD
//****************************************************************************************************

ItemImageData SteelBroadSwordImage
{
	shapeFile  = "sword";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(SteelBroadSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData SteelBroadSword
{
	heading = "bWeapons";
	description = "Steel Broadsword";
	className = "Weapon";
	shapeFile  = "sword";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SteelBroadSwordImage;
	price = 0;
	showWeaponBar = true;
};
function SteelBroadSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SteelBroadSword), SteelBroadSword);
}

function SteelBroadSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Steel Broad Sword: <f0>Attack: <f2>40    <f0>Skill Slashing Req @ <f2>140    <f0>Speed: <f2>1.12 Seconds    <f0>Price: <f2>$5,287    <f0>Weight: <f2>5.5 Lbs");
}
//****************************************************************************************************
//   STEEL LONGSWORD
//****************************************************************************************************

ItemImageData SteelLongSwordImage
{
	shapeFile  = "long_sword";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(SteelLongSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData SteelLongSword
{
	heading = "bWeapons";
	description = "Steel Longsword";
	className = "Weapon";
	shapeFile  = "long_sword";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SteelLongSwordImage;
	price = 0;
	showWeaponBar = true;
};
function SteelLongSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SteelLongSword), SteelLongSword);
}

function SteelLongSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Steel Long Sword: <f0>Attack: <f2>55    <f0>Skill Slashing Req @ <f2>200    <f0>Speed: <f2>1.34 Seconds    <f0>Price: <f2>$12,449    <f0>Weight: <f2>6 Lbs");
}
//****************************************************************************************************
//   GOLDEN LONGSWORD
//****************************************************************************************************

ItemImageData GoldenLongSwordImage
{
	shapeFile  = "long_sword";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(GoldenLongSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData GoldenLongSword
{
	heading = "bWeapons";
	description = "Golden Longsword";
	className = "Weapon";
	shapeFile  = "long_sword";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = GoldenLongSwordImage;
	price = 0;
	showWeaponBar = true;
};
function GoldenLongSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(GoldenLongSword), GoldenLongSword);
}

function GoldenLongSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Golden Long Sword: <f0>Attack: <f2>70    <f0>Skill Slashing Req @ <f2>260    <f0>Speed: <f2>1.46 Seconds    <f0>Price: <f2>$22,957    <f0>Weight: <f2>6.5 Lbs");
}
//****************************************************************************************************
//   GOLDEN BASTARDSWORD
//****************************************************************************************************

ItemImageData GoldenBastardSwordImage
{
	shapeFile  = "katana";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(GoldenBastardSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData GoldenBastardSword
{
	heading = "bWeapons";
	description = "Golden BastardSword";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = GoldenBastardSwordImage;
	price = 0;
	showWeaponBar = true;
};
function GoldenBastardSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(GoldenBastardSword), GoldenBastardSword);
}

function GoldenBastardSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Golden Bastard Sword: <f0>Attack: <f2>90    <f0>Skill Slashing Req @ <f2>340    <f0>Speed: <f2>1.57 Seconds    <f0>Price: <f2>$43,531    <f0>Weight: <f2>7 Lbs");
}
//****************************************************************************************************
//   CRYSTAL BASTARDSWORD
//****************************************************************************************************

ItemImageData CrystalBastardSwordImage
{
	shapeFile  = "katana";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(CrystalBastardSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData CrystalBastardSword
{
	heading = "bWeapons";
	description = "Crystal BastardSword";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = CrystalBastardSwordImage;
	price = 0;
	showWeaponBar = true;
};
function CrystalBastardSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrystalBastardSword), CrystalBastardSword);
}

function CrystalBastardSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Crystal Bastard Sword: <f0>Attack: <f2>115    <f0>Skill Slashing Req @ <f2>440    <f0>Speed: <f2>1.68 Seconds    <f0>Price: <f2>$83,526    <f0>Weight: <f2>7.5 Lbs");
}
//****************************************************************************************************
//   TEMPERED CRYSTAL BASTARDSWORD
//****************************************************************************************************

ItemImageData TemperedCrystalBastardSwordImage
{
	shapeFile  = "katana";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(TemperedCrystalBastardSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData TemperedCrystalBastardSword
{
	heading = "bWeapons";
	description = "Tempered Crystal BastardSword";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = TemperedCrystalBastardSwordImage;
	price = 0;
	showWeaponBar = true;
};
function TemperedCrystalBastardSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(TemperedCrystalBastardSword), TemperedCrystalBastardSword);
}

function TemperedCrystalBastardSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Tempered Crystal Bastard Sword: <f0>Attack: <f2>140    <f0>Skill Slashing Req @ <f2>540    <f0>Speed: <f2>1.79 Seconds    <f0>Price: <f2>$136,211    <f0>Weight: <f2>8 Lbs");
}
//****************************************************************************************************
//   CRYSTAL CLAYMORE
//****************************************************************************************************

ItemImageData CrystalClaymoreImage
{
	shapeFile  = "katana";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(CrystalClaymore);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData CrystalClaymore
{
	heading = "bWeapons";
	description = "Crystal Claymore";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = CrystalClaymoreImage;
	price = 0;
	showWeaponBar = true;
};
function CrystalClaymoreImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrystalClaymore), CrystalClaymore);
}

function CrystalClaymore::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Crystal Claymore: <f0>Attack: <f2>165    <f0>Skill Slashing Req @ <f2>640    <f0>Speed: <f2>1.91 Seconds    <f0>Price: <f2>$199,898    <f0>Weight: <f2>8.5 Lbs");
}
//****************************************************************************************************
//   DIAMOND CLAYMORE
//****************************************************************************************************

ItemImageData DiamondClaymoreImage
{
	shapeFile  = "katana";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondClaymore);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData DiamondClaymore
{
	heading = "bWeapons";
	description = "Diamond Claymore";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = DiamondClaymoreImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondClaymoreImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondClaymore), DiamondClaymore);
}

function DiamondClaymore::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Claymore: <f0>Attack: <f2>195    <f0>Skill Slashing Req @ <f2>740    <f0>Speed: <f2>1.91 Seconds    <f0>Price: <f2>$370,895    <f0>Weight: <f2>8.5 Lbs");
}

//****************************************************************************************************
//   DIAMOND LEGEND SWORD
//****************************************************************************************************

ItemImageData DiamondLegendSwordImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondLegendSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing2;
	sfxActivate = ActivateAS;
};
ItemData DiamondLegendSword
{
	heading = "bWeapons";
	description = "Diamond Legend Sword";
	className = "Weapon";
	shapeFile  = "elfinblade";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = DiamondLegendSwordImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondLegendSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondLegendSword), DiamondLegendSword);
}

function DiamondLegendSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Legend Sword: <f0>Attack: <f2>220    <f0>Skill Slashing Req @ <f2>840 Remort 1    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$9,425,500    <f0>Weight: <f2>4 Lbs");
}
//****************************************************************************************************
//   BLACK DIAMOND DREAM SWORD
//****************************************************************************************************

ItemImageData BlackDiamondDreamSwordImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(BlackDiamondDreamSword);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing2;
	sfxActivate = ActivateAS;
};
ItemData BlackDiamondDreamSword
{
	heading = "bWeapons";
	description = "Black Diamond Dream Sword";
	className = "Weapon";
	shapeFile  = "elfinblade";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = BlackDiamondDreamSwordImage;
	price = 0;
	showWeaponBar = true;
};
function BlackDiamondDreamSwordImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(BlackDiamondDreamSword), BlackDiamondDreamSword);
}

function BlackDiamondDreamSword::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Black Diamond Dream Sword: <f0>Attack: <f2>300    <f0>Skill Slashing Req @ <f2>1200 Remort 10    <f0>Speed: <f2>0.78 Seconds    <f0>Price: <f2>$48,670,000    <f0>Weight: <f2>3.5 Lbs");
}
//****************************************************************************************************
//   BLACK DIAMOND ATOM SPLITTER
//****************************************************************************************************

ItemImageData BlackDiamondAtomSplitterImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(BlackDiamondAtomSplitter);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing2;
	sfxActivate = ActivateAS;
};
ItemData BlackDiamondAtomSplitter
{
	heading = "bWeapons";
	description = "Black Diamond Atom Splitter";
	className = "Weapon";
	shapeFile  = "elfinblade";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = BlackDiamondAtomSplitterImage;
	price = 0;
	showWeaponBar = true;
};
function BlackDiamondAtomSplitterImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(BlackDiamondAtomSplitter), BlackDiamondAtomSplitter);
}

function BlackDiamondAtomSplitter::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Black Diamond Atom Splitter: <f0>Attack: <f2>450    <f0>Skill Slashing Req @ <f2>1580 Remort 30    <f0>Speed: <f2>0.67 Seconds    <f0>Price: <f2>$385,923,000    <f0>Weight: <f2>3 Lbs");
}
//****************************************************************************************************
//   BUTTER KNIFE
//****************************************************************************************************

ItemImageData ButterKnifeImage
{
	shapeFile  = "dagger";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(ButterKnife);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData ButterKnife
{
	heading = "bWeapons";
	description = "Butter Knife";
	className = "Weapon";
	shapeFile  = "dagger";
	hudIcon = "dagger";
	shadowDetailMask = 4;
	imageType = ButterKnifeImage;
	price = 0;
	showWeaponBar = true;
};
function ButterKnifeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(ButterKnife), ButterKnife);
}

function ButterKnife::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Butter Knife: <f0>Attack: <f2>5    <f0>Skill Piercing Req @ <f2>0    <f0>Speed: <f2>0.78 Seconds    <f0>Price: <f2>$12    <f0>Weight: <f2>3.5 Lbs");
}

//****************************************************************************************************
//   LONG KNIFE
//****************************************************************************************************

ItemImageData LongKnifeImage
{
	shapeFile  = "dagger";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(LongKnife);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData LongKnife
{
	heading = "bWeapons";
	description = "Long Knife";
	className = "Weapon";
	shapeFile  = "dagger";
	hudIcon = "dagger";
	shadowDetailMask = 4;
	imageType = LongKnifeImage;
	price = 0;
	showWeaponBar = true;
};
function LongKnifeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(LongKnife), LongKnife);
}

function LongKnife::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Long Knife: <f0>Attack: <f2>13    <f0>Skill Piercing Req @ <f2>45    <f0>Speed: <f2>0.78 Seconds    <f0>Price: <f2>$440    <f0>Weight: <f2>3.5 Lbs");
}

//****************************************************************************************************
//   IRON SPEAR
//****************************************************************************************************

ItemImageData IronSpearImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(IronSpear);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData IronSpear
{
	heading = "bWeapons";
	description = "Iron Spear";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "spear";
	shadowDetailMask = 4;
	imageType = IronSpearImage;
	price = 0;
	showWeaponBar = true;
};
function IronSpearImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(IronSpear), IronSpear);
}

function IronSpear::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Iron Spear: <f0>Attack: <f2>34    <f0>Skill Piercing Req @ <f2>90    <f0>Speed: <f2>1.34 Seconds    <f0>Price: <f2>$2,100    <f0>Weight: <f2>4.5 Lbs");
}
//****************************************************************************************************
//   STEEL SPEAR
//****************************************************************************************************

ItemImageData SteelSpearImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(SteelSpear);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData SteelSpear
{
	heading = "bWeapons";
	description = "Steel Spear";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "spear";
	shadowDetailMask = 4;
	imageType = SteelSpearImage;
	price = 0;
	showWeaponBar = true;
};
function SteelSpearImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SteelSpear), SteelSpear);
}

function SteelSpear::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Steel Spear: <f0>Attack: <f2>48    <f0>Skill Piercing Req @ <f2>140    <f0>Speed: <f2>1.49 Seconds    <f0>Price: <f2>$5,049    <f0>Weight: <f2>5 Lbs");
}
//****************************************************************************************************
//   STEEL PIKE
//****************************************************************************************************

ItemImageData SteelPikeImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(SteelPike);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData SteelPike
{
	heading = "bWeapons";
	description = "Steel Pike";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "spear";
	shadowDetailMask = 4;
	imageType = SteelPikeImage;
	price = 0;
	showWeaponBar = true;
};
function SteelPikeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SteelPike), SteelPike);
}

function SteelPike::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Steel Pike: <f0>Attack: <f2>65    <f0>Skill Piercing Req @ <f2>200    <f0>Speed: <f2>1.64 Seconds    <f0>Price: <f2>$10,993    <f0>Weight: <f2>5.5 Lbs");
}
//****************************************************************************************************
//   GOLDEN PIKE
//****************************************************************************************************

ItemImageData GoldenPikeImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(GoldenPike);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData GoldenPike
{
	heading = "bWeapons";
	description = "Golden Pike";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "spear";
	shadowDetailMask = 4;
	imageType = GoldenPikeImage;
	price = 0;
	showWeaponBar = true;
};
function GoldenPikeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(GoldenPike), GoldenPike);
}

function GoldenPike::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Golden Pike: <f0>Attack: <f2>83    <f0>Skill Piercing Req @ <f2>260    <f0>Speed: <f2>1.79 Seconds    <f0>Price: <f2>$19,685    <f0>Weight: <f2>6 Lbs");
}
//****************************************************************************************************
//   CRYSTAL PIKE
//****************************************************************************************************

ItemImageData CrystalPikeImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(CrystalPike);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData CrystalPike
{
	heading = "bWeapons";
	description = "Crystal Pike";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "spear";
	shadowDetailMask = 4;
	imageType = CrystalPikeImage;
	price = 0;
	showWeaponBar = true;
};
function CrystalPikeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrystalPike), CrystalPike);
}

function CrystalPike::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Crystal Pike: <f0>Attack: <f2>105    <f0>Skill Piercing Req @ <f2>340    <f0>Speed: <f2>1.94 Seconds    <f0>Price: <f2>$34,939    <f0>Weight: <f2>6.5 Lbs");
}
//****************************************************************************************************
//   CRYSTAL TRIDENT
//****************************************************************************************************

ItemImageData CrystalTridentImage
{
	shapeFile  = "trident";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(CrystalTrident);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData CrystalTrident
{
	heading = "bWeapons";
	description = "Crystal Trident";
	className = "Weapon";
	shapeFile  = "trident";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = CrystalTridentImage;
	price = 0;
	showWeaponBar = true;
};
function CrystalTridentImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrystalTrident), CrystalTrident);
}

function CrystalTrident::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Crystal Trident: <f0>Attack: <f2>130    <f0>Skill Piercing Req @ <f2>440    <f0>Speed: <f2>2.09 Seconds    <f0>Price: <f2>$58,536    <f0>Weight: <f2>7 Lbs");
}
//****************************************************************************************************
//   TEMPERED CRYSTAL TRIDENT
//****************************************************************************************************

ItemImageData TemperedCrystalTridentImage
{
	shapeFile  = "trident";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(TemperedCrystalTrident);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData TemperedCrystalTrident
{
	heading = "bWeapons";
	description = "Tempered Crystal Trident";
	className = "Weapon";
	shapeFile  = "trident";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = TemperedCrystalTridentImage;
	price = 0;
	showWeaponBar = true;
};
function TemperedCrystalTridentImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(TemperedCrystalTrident), TemperedCrystalTrident);
}

function TemperedCrystalTrident::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Tempered Crystal Trident: <f0>Attack: <f2>160    <f0>Skill Piercing Req @ <f2>540    <f0>Speed: <f2>2.24 Seconds    <f0>Price: <f2>$97,772    <f0>Weight: <f2>7.5 Lbs");
}
//****************************************************************************************************
//   DIAMOND TRIDENT
//****************************************************************************************************

ItemImageData DiamondTridentImage
{
	shapeFile  = "trident";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondTrident);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData DiamondTrident
{
	heading = "bWeapons";
	description = "Diamond Trident";
	className = "Weapon";
	shapeFile  = "trident";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = DiamondTridentImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondTridentImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondTrident), DiamondTrident);
}

function DiamondTrident::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Trident: <f0>Attack: <f2>190    <f0>Skill Piercing Req @ <f2>640    <f0>Speed: <f2>2.39 Seconds    <f0>Price: <f2>$145,430    <f0>Weight: <f2>8 Lbs");
}
//****************************************************************************************************
//   DIAMOND DEATH SPEAR
//****************************************************************************************************

ItemImageData DiamondDeathSpearImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondDeathSpear);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData DiamondDeathSpear
{
	heading = "bWeapons";
	description = "Diamond Death Spear";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = DiamondDeathSpearImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondDeathSpearImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondDeathSpear), DiamondDeathSpear);
}

function DiamondDeathSpear::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Death Spear: <f0>Attack: <f2>220    <f0>Skill Piercing Req @ <f2>740    <f0>Speed: <f2>2.39 Seconds    <f0>Price: <f2>$250,167    <f0>Weight: <f2>8 Lbs");
}
//****************************************************************************************************
//   DIAMOND LEGEND SPEAR
//****************************************************************************************************

ItemImageData DiamondLegendSpearImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondLegendSpear);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData DiamondLegendSpear
{
	heading = "bWeapons";
	description = "Diamond Legend Spear";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = DiamondLegendSpearImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondLegendSpearImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondLegendSpear), DiamondLegendSpear);
}

function DiamondLegendSpear::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Legend Spear: <f0>Attack: <f2>260    <f0>Skill Piercing Req @ <f2>840 Remort 1    <f0>Speed: <f2>1.19 Seconds    <f0>Price: <f2>$6,032,280    <f0>Weight: <f2>4 Lbs");
}
//****************************************************************************************************
//   BLACK DIAMOND DREAM SPEAR
//****************************************************************************************************

ItemImageData BlackDiamondDreamSpearImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(BlackDiamondDreamSpear);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData BlackDiamondDreamSpear
{
	heading = "bWeapons";
	description = "Black Diamond Dream Spear";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = BlackDiamondDreamSpearImage;
	price = 0;
	showWeaponBar = true;
};
function BlackDiamondDreamSpearImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(BlackDiamondDreamSpear), BlackDiamondDreamSpear);
}

function BlackDiamondDreamSpear::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Black Diamond Dream Spear: <f0>Attack: <f2>350    <f0>Skill Piercing Req @ <f2>1200 Remort 10    <f0>Speed: <f2>1.04 Seconds    <f0>Price: <f2>$29,696,300    <f0>Weight: <f2>3.5 Lbs");
}
//****************************************************************************************************
//   BLACK DIAMOND ATOM PIERCER
//****************************************************************************************************

ItemImageData BlackDiamondAtomPiercerImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(BlackDiamondAtomPiercer);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData BlackDiamondAtomPiercer
{
	heading = "bWeapons";
	description = "Black Diamond Atom Piercer";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = BlackDiamondAtomPiercerImage;
	price = 0;
	showWeaponBar = true;
};
function BlackDiamondAtomPiercerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(BlackDiamondAtomPiercer), BlackDiamondAtomPiercer);
}

function BlackDiamondAtomPiercer::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Black Diamond Atom Piercer: <f0>Attack: <f2>520    <f0>Skill Piercing Req @ <f2>1580 Remort 30    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$227,275,000    <f0>Weight: <f2>3 Lbs");
}
//****************************************************************************************************
//   KNIFE
//****************************************************************************************************

ItemImageData KnifeImage
{
	shapeFile  = "dagger";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 1.0;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData Knife
{
	heading = "bWeapons";
	description = "Knife";
	className = "Weapon";
	shapeFile  = "dagger";
	hudIcon = "dagger";
	shadowDetailMask = 4;
	imageType = KnifeImage;
	price = 0;
	showWeaponBar = true;
};
function KnifeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Knife), Knife);
}

function Knife::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Knife: <f0>Attack: <f2>55    <f0>Skill Piercing Req @ <f2>Remort 5    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$50,000    <f0>Weight: <f2>4 Lbs");
}
//****************************************************************************************************
//   DAGGER
//****************************************************************************************************

ItemImageData DaggerImage
{
	shapeFile  = "dagger";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(Dagger);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData Dagger
{
	heading = "bWeapons";
	description = "Dagger";
	className = "Weapon";
	shapeFile  = "dagger";
	hudIcon = "dagger";
	shadowDetailMask = 4;
	imageType = DaggerImage;
	price = 0;
	showWeaponBar = true;
};
function DaggerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Dagger), Dagger);
}

//****************************************************************************************************
//   HATCHET
//****************************************************************************************************

ItemImageData HatchetImage
{
	shapeFile  = "hatchet";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = 1.0;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = AxeSlash2;
};
ItemData Hatchet
{
	heading = "bWeapons";
	description = "Hatchet";
	className = "Weapon";
	shapeFile  = "hatchet";
	hudIcon = "axe";
	shadowDetailMask = 4;
	imageType = HatchetImage;
	price = 0;
	showWeaponBar = true;
};
function HatchetImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Hatchet), Hatchet);
}

function Hatchet::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Hatchet: <f0>Attack: <f2>60    <f0>Skill Slashing Req @ <f2>Remort 5    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$50,000    <f0>Weight: <f2>5 Lbs");
}
//****************************************************************************************************
//   PICK AXE
//****************************************************************************************************

ItemImageData PickAxeImage
{
	shapeFile = "Pick";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = 1.0;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing1;
	sfxActivate = CrossbowSwitch1;
};
ItemData PickAxe
{
	heading = "bWeapons";
	description = "Pick Axe";
	className = "Weapon";
	shapeFile = "Pick";
	hudIcon = "pick";
	shadowDetailMask = 4;
	imageType = PickAxeImage;
	price = 0;
	showWeaponBar = true;
};
function PickAxeImage::onFire(%player, %slot)
{
	PickAxeSwing(%player, GetRange(PickAxe), PickAxe);
}

function PickAxe::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Pick Axe: <f0>Attack: <f2>3    <f0>Skill Piercing Req @ <f2>0    <f0>Speed: <f2>1.09 Seconds    <f0>Price: <f2>$0    <f0>Weight: <f2>4 Lbs");
}
//****************************************************************************************************
//   CRACKED STICK
//****************************************************************************************************

ItemImageData CrackedStickImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(CrackedStick);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData CrackedStick
{
	heading = "bWeapons";
	description = "Cracked Stick";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "club";
	shadowDetailMask = 4;
	imageType = CrackedStickImage;
	price = 0;
	showWeaponBar = true;
};
function CrackedStickImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrackedStick), CrackedStick);
}

function CrackedStick::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Cracked Stick: <f0>Attack: <f2>13    <f0>Skill Bludgeoning Req @ <f2>0    <f0>Speed: <f2>1.23 Seconds    <f0>Price: <f2>$82    <f0>Weight: <f2>4.5 Lbs");
}
//****************************************************************************************************
//   IRON STICK
//****************************************************************************************************

ItemImageData IronStickImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(IronStick);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData IronStick
{
	heading = "bWeapons";
	description = "Iron Stick";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "club";
	shadowDetailMask = 4;
	imageType = IronStickImage;
	price = 0;
	showWeaponBar = true;
};
function IronStickImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(IronStick), IronStick);
}

function IronStick::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Iron Stick: <f0>Attack: <f2>23    <f0>Skill Bludgeoning Req @ <f2>45    <f0>Speed: <f2>1.37 Seconds    <f0>Price: <f2>$462    <f0>Weight: <f2>5 Lbs");
}
//****************************************************************************************************
//   IRON MACE
//****************************************************************************************************

ItemImageData IronMaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(IronMace);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData IronMace
{
	heading = "bWeapons";
	description = "Iron Mace";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "club";
	shadowDetailMask = 4;
	imageType = IronMaceImage;
	price = 0;
	showWeaponBar = true;
};
function IronMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(IronMace), IronMace);
}

function IronMace::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Iron Mace: <f0>Attack: <f2>23    <f0>Skill Bludgeoning Req @ <f2>90    <f0>Speed: <f2>1.51 Seconds    <f0>Price: <f2>$1,704    <f0>Weight: <f2>5.5 Lbs");
}
//****************************************************************************************************
//   STEEL MACE
//****************************************************************************************************

ItemImageData SteelMaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(SteelMace);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData SteelMace
{
	heading = "bWeapons";
	description = "Steel Mace";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "club";
	shadowDetailMask = 4;
	imageType = SteelMaceImage;
	price = 0;
	showWeaponBar = true;
};
function SteelMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SteelMace), SteelMace);
}

function SteelMace::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Steel Mace: <f0>Attack: <f2>49    <f0>Skill Bludgeoning Req @ <f2>140    <f0>Speed: <f2>1.64 Seconds    <f0>Price: <f2>$3,864    <f0>Weight: <f2>6 Lbs");
}
//****************************************************************************************************
//   STEEL HAMMER
//****************************************************************************************************

ItemImageData SteelHammerImage
{
	shapeFile  = "hammer";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(SteelHammer);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing6;
	sfxActivate = AxeSlash2;
};
ItemData SteelHammer
{
	heading = "bWeapons";
	description = "Steel Hammer";
	className = "Weapon";
	shapeFile  = "hammer";
	hudIcon = "hammer";
	shadowDetailMask = 4;
	imageType = SteelHammerImage;
	price = 0;
	showWeaponBar = true;
};
function SteelHammerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SteelHammer), SteelHammer);
}

function SteelHammer::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Steel Hammer: <f0>Attack: <f2>62    <f0>Skill Bludgeoning Req @ <f2>200    <f0>Speed: <f2>1.78 Seconds    <f0>Price: <f2>$6,884    <f0>Weight: <f2>6.5 Lbs");
}
//****************************************************************************************************
//   STEEL WARHAMMER
//****************************************************************************************************

ItemImageData SteelWarHammerImage
{
	shapeFile  = "hammer";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(SteelWarHammer);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing6;
	sfxActivate = AxeSlash2;
};
ItemData SteelWarHammer
{
	heading = "bWeapons";
	description = "Steel WarHammer";
	className = "Weapon";
	shapeFile  = "hammer";
	hudIcon = "hammer";
	shadowDetailMask = 4;
	imageType = SteelWarHammerImage;
	price = 0;
	showWeaponBar = true;
};
function SteelWarHammerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SteelWarHammer), SteelWarHammer);
}

function SteelWarHammer::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Steel War Hammer: <f0>Attack: <f2>77    <f0>Skill Bludgeoning Req @ <f2>260    <f0>Speed: <f2>1.92 Seconds    <f0>Price: <f2>$11,632    <f0>Weight: <f2>7 Lbs");
}
//****************************************************************************************************
//   GOLDEN WARHAMMER
//****************************************************************************************************

ItemImageData GoldenWarHammerImage
{
	shapeFile  = "hammer";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(GoldenWarHammer);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing6;
	sfxActivate = AxeSlash2;
};
ItemData GoldenWarHammer
{
	heading = "bWeapons";
	description = "Golden WarHammer";
	className = "Weapon";
	shapeFile  = "hammer";
	hudIcon = "hammer";
	shadowDetailMask = 4;
	imageType = GoldenWarHammerImage;
	price = 0;
	showWeaponBar = true;
};
function GoldenWarHammerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(GoldenWarHammer), GoldenWarHammer);
}

function GoldenWarHammer::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Golden War Hammer: <f0>Attack: <f2>95    <f0>Skill Bludgeoning Req @ <f2>340    <f0>Speed: <f2>2.06 Seconds    <f0>Price: <f2>$19,604    <f0>Weight: <f2>7.5 Lbs");
}
//****************************************************************************************************
//   GOLDEN DIVINE MACE
//****************************************************************************************************

ItemImageData GoldenDivineMaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(GoldenDivineMace);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData GoldenDivineMace
{
	heading = "bWeapons";
	description = "Golden Divine Mace";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = GoldenDivineMaceImage;
	price = 0;
	showWeaponBar = true;
};
function GoldenDivineMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(GoldenDivineMace), GoldenDivineMace);
}

function GoldenDivineMace::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Golden Divine Mace: <f0>Attack: <f2>120    <f0>Skill Bludgeoning Req @ <f2>440    <f0>Speed: <f2>2.19 Seconds    <f0>Price: <f2>$36,648    <f0>Weight: <f2>8 Lbs");
}
//****************************************************************************************************
//   CRYSTAL DIVINE MACE
//****************************************************************************************************

ItemImageData CrystalDivineMaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(CrystalDivineMace);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData CrystalDivineMace
{
	heading = "bWeapons";
	description = "Crystal Divine Mace";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = CrystalDivineMaceImage;
	price = 0;
	showWeaponBar = true;
};
function CrystalDivineMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrystalDivineMace), CrystalDivineMace);
}

function CrystalDivineMace::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Crystal Divine Mace: <f0>Attack: <f2>150    <f0>Skill Bludgeoning Req @ <f2>540    <f0>Speed: <f2>2.33 Seconds    <f0>Price: <f2>$66,865    <f0>Weight: <f2>8.5 Lbs");
}
//****************************************************************************************************
//   DIAMOND DIVINE MACE
//****************************************************************************************************

ItemImageData DiamondDivineMaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondDivineMace);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData DiamondDivineMace
{
	heading = "bWeapons";
	description = "Diamond Divine Mace";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = DiamondDivineMaceImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondDivineMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondDivineMace), DiamondDivineMace);
}

function DiamondDivineMace::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Divine Mace: <f0>Attack: <f2>180    <f0>Skill Bludgeoning Req @ <f2>640    <f0>Speed: <f2>2.47 Seconds    <f0>Price: <f2>$106,250    <f0>Weight: <f2>9 Lbs");
}
//****************************************************************************************************
//   DIAMOND BRAIN SPILLER
//****************************************************************************************************

ItemImageData DiamondBrainSpillerImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondBrainSpiller);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData DiamondBrainSpiller
{
	heading = "bWeapons";
	description = "Diamond Brain Spiller";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = DiamondBrainSpillerImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondBrainSpillerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondBrainSpiller), DiamondBrainSpiller);
}

function DiamondBrainSpiller::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Brain Spiller: <f0>Attack: <f2>210    <f0>Skill Bludgeoning Req @ <f2>740    <f0>Speed: <f2>2.47 Seconds    <f0>Price: <f2>$187,944    <f0>Weight: <f2>9 Lbs");
}
//****************************************************************************************************
//   DIAMOND LEGEND MACE
//****************************************************************************************************

ItemImageData DiamondLegendMaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(DiamondLegendMace);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData DiamondLegendMace
{
	heading = "bWeapons";
	description = "Diamond Legend Mace";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = DiamondLegendMaceImage;
	price = 0;
	showWeaponBar = true;
};
function DiamondLegendMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DiamondLegendMace), DiamondLegendMace);
}

function DiamondLegendMace::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Diamond Legend Mace: <f0>Attack: <f2>240    <f0>Skill Bludgeoning Req @ <f2>840 Remort 1    <f0>Speed: <f2>1.09 Seconds    <f0>Price: <f2>$6,189,820    <f0>Weight: <f2>4 Lbs");
}
//****************************************************************************************************
//   BLACK DIAMOND DREAM MACE
//****************************************************************************************************

ItemImageData BlackDiamondDreamMaceImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(BlackDiamondDreamMace);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData BlackDiamondDreamMace
{
	heading = "bWeapons";
	description = "Black Diamond Dream Mace";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = BlackDiamondDreamMaceImage;
	price = 0;
	showWeaponBar = true;
};
function BlackDiamondDreamMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(BlackDiamondDreamMace), BlackDiamondDreamMace);
}

function BlackDiamondDreamMace::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Black Diamond Dream Mace: <f0>Attack: <f2>330    <f0>Skill Bludgeoning Req @ <f2>1200 Remort 10    <f0>Speed: <f2>0.96 Seconds    <f0>Price: <f2>$32957500    <f0>Weight: <f2>3.5 Lbs");
}
//****************************************************************************************************
//   BLACK DIAMOND ATOM SMASHER
//****************************************************************************************************

ItemImageData BlackDiamondAtomSmasherImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(BlackDiamondAtomSmasher);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData BlackDiamondAtomSmasher
{
	heading = "bWeapons";
	description = "Black Diamond Atom Smasher";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = BlackDiamondAtomSmasherImage;
	price = 0;
	showWeaponBar = true;
};
function BlackDiamondAtomSmasherImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(BlackDiamondAtomSmasher), BlackDiamondAtomSmasher);
}

function BlackDiamondAtomSmasher::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Black Diamond Atom Smasher: <f0>Attack: <f2>490    <f0>Skill Bludgeoning Req @ <f2>1580 Remort 30    <f0>Speed: <f2>0.82 Seconds    <f0>Price: <f2>$251700000    <f0>Weight: <f2>3 Lbs");
}
//****************************************************************************************************
//   CLUB
//****************************************************************************************************

ItemImageData ClubImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = 1.0;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData Club
{
	heading = "bWeapons";
	description = "Club";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "club";
	shadowDetailMask = 4;
	imageType = ClubImage;
	price = 0;
	showWeaponBar = true;
};
function ClubImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Club), Club);
}

function Club::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Club: <f0>Attack: <f2>55    <f0>Skill Bludgeoning Req @ <f2>0    <f0>Speed: <f2>1.09 Seconds    <f0>Price: <f2>$50,000    <f0>Weight: <f2>4 Lbs");
}
//****************************************************************************************************
//   CASTING BLADE
//****************************************************************************************************

ItemImageData CastingBladeImage
{
	shapeFile  = "dagger";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(CastingBlade);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = NoSound;
	sfxActivate = NoSound;
};
ItemData CastingBlade
{
	heading = "bWeapons";
	description = "Casting Blade";
	className = "Weapon";
	shapeFile  = "dagger";
	hudIcon = "dagger";
	shadowDetailMask = 4;
	imageType = CastingBladeImage;
	price = 0;
	showWeaponBar = true;
};
function CastingBladeImage::onFire(%player, %slot)
{
	%clientId = Player::getClient(%player);
	if(%clientId == "")
		%clientId = 0;

//	if(Player::isAIcontrolled(%clientId))
//	{
//		if(fetchData(%clientId, "HP") <= (fetchData(%clientId, "MaxHP")/3))
//		{
//			if( floor(getRandom() * 10) > 7 )
//				%doHealSpell = True;
//		}
//	}
//	if(%doHealSpell)
//		%index = GetBestSpell(%clientId, -1, True);
//	else

	%index = GetBestSpell(%clientId, 1, True);

	%length = $Spell::LOSrange[%index]-1;
		
	$los::object = "";
	if(GameBase::getLOSinfo(%player, %length) && %index != -1)
	{
		%obj = getObjectType($los::object);
		if(%obj == "Player")
		{
			if(Player::isAiControlled(%clientId))
			{
				AI::newDirectiveRemove(fetchData(%clientId, "BotInfoAiName"), 99);
			}
			remoteSay(%clientId, 0, "#cast " @ $Spell::keyword[%index]);
			%hasCast = True;
		}
	}
	if(!%hasCast)
	{
		if(OddsAre(3))
			MeleeAttack(%player, GetRange(Hatchet), CastingBlade);	//mimic the hatchet range
	}
	%hasCast = "";
}

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