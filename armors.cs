function GenerateAllArmorCosts()
{
//Custom item costs
	$ItemCost[RatSkinShirt] = GenerateItemCost(RatSkinShirt);
	$ItemCost[StuddedLeatherSuit] = GenerateItemCost(StuddedLeatherSuit);
	$ItemCost[ToughHideSuit] = GenerateItemCost(ToughHideSuit);
	$ItemCost[IronScaleMail] = GenerateItemCost(IronScaleMail);
	$ItemCost[SteelScaleMail] = GenerateItemCost(SteelScaleMail);
	$ItemCost[SteelBrigandineMail] = GenerateItemCost(SteelBrigandineMail);
	$ItemCost[GoldenBrigandineMail] = GenerateItemCost(GoldenBrigandineMail);
	$ItemCost[GoldenChainMail] = GenerateItemCost(GoldenChainMail);
	$ItemCost[CrystalChainMail] = GenerateItemCost(CrystalChainMail);
	$ItemCost[CrystalRingMail] = GenerateItemCost(CrystalRingMail);
	$ItemCost[CrystalBandedMail] = GenerateItemCost(CrystalBandedMail);
	$ItemCost[CrystalSplintMail] = GenerateItemCost(CrystalSplintMail);
	$ItemCost[TungstenSplintMail] = GenerateItemCost(TungstenSplintMail);
	$ItemCost[TungstenPlateMail] = GenerateItemCost(TungstenPlateMail);
	$ItemCost[DiamondPlateMail] = GenerateItemCost(DiamondPlateMail);
	$ItemCost[DiamondFieldPlate] = GenerateItemCost(DiamondFieldPlate);
	$ItemCost[DiamondFullPlate] = GenerateItemCost(DiamondFullPlate);
	$ItemCost[BlackDiamondFullPlate] = 304678000;
}
//Custom armor types
$AccessoryVar[RatSkinShirt, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[StuddedLeatherSuit, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[ToughHideSuit, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[IronScaleMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[SteelScaleMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[SteelBrigandineMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[GoldenBrigandineMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[GoldenChainMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[CrystalChainMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[CrystalRingMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[CrystalBandedMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[CrystalSplintMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[TungstenSplintMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[TungstenPlateMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[DiamondPlateMail, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[DiamondFieldPlate, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[DiamondFullPlate, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[BlackDiamondFullPlate, $AccessoryType] = $BodyAccessoryType;

//Custom armor specs
$AccessoryVar[RatSkinShirt, $SpecialVar] = "7 20 4 4";
$AccessoryVar[StuddedLeatherSuit, $SpecialVar] = "7 80 4 19 3 5";
$AccessoryVar[ToughHideSuit, $SpecialVar] = "7 110 4 28 3 10";
$AccessoryVar[IronScaleMail, $SpecialVar] = "7 145 4 38 3 15";
$AccessoryVar[SteelScaleMail, $SpecialVar] = "7 185 4 49 3 20";
$AccessoryVar[SteelBrigandineMail, $SpecialVar] = "7 225 4 60 3 25";
$AccessoryVar[GoldenBrigandineMail, $SpecialVar] = "7 270 4 75 3 30";
$AccessoryVar[GoldenChainMail, $SpecialVar] = "7 315 4 90 3 40";
$AccessoryVar[CrystalChainMail, $SpecialVar] = "7 360 4 110 3 50";
$AccessoryVar[CrystalRingMail, $SpecialVar] = "7 410 4 130 3 60";
$AccessoryVar[CrystalBandedMail, $SpecialVar] = "7 460 4 155 3 70";
$AccessoryVar[CrystalSplintMail, $SpecialVar] = "7 515 4 180 3 80";
$AccessoryVar[TungstenSplintMail, $SpecialVar] = "7 570 4 210 3 90";
$AccessoryVar[TungstenPlateMail, $SpecialVar] = "7 630 4 245 3 100";
$AccessoryVar[DiamondPlateMail, $SpecialVar] = "7 690 4 280 3 115";
$AccessoryVar[DiamondFieldPlate, $SpecialVar] = "7 750 4 320 3 130";
$AccessoryVar[DiamondFullPlate, $SpecialVar] = "7 900 4 420 3 160 10 2";
$AccessoryVar[BlackDiamondFullPlate, $SpecialVar] = "7 1200 4 600 3 200 10 2.5";

//Custom armor weight
$AccessoryVar[RatSkinShirt, $Weight] = 7;
$AccessoryVar[StuddedLeatherSuit, $Weight] = 26;
$AccessoryVar[ToughHideSuit, $Weight] = 35;
$AccessoryVar[IronScaleMail, $Weight] = 46;
$AccessoryVar[SteelScaleMail, $Weight] = 59;
$AccessoryVar[SteelBrigandineMail, $Weight] = 73;
$AccessoryVar[GoldenBrigandineMail, $Weight] = 90;
$AccessoryVar[GoldenChainMail, $Weight] = 110;
$AccessoryVar[CrystalChainMail, $Weight] = 135;
$AccessoryVar[CrystalRingMail, $Weight] = 165;
$AccessoryVar[CrystalBandedMail, $Weight] = 200;
$AccessoryVar[CrystalSplintMail, $Weight] = 240;
$AccessoryVar[TungstenSplintMail, $Weight] = 280;
$AccessoryVar[TungstenPlateMail, $Weight] = 325;
$AccessoryVar[DiamondPlateMail, $Weight] = 375;
$AccessoryVar[DiamondFieldPlate, $Weight] = 425;
$AccessoryVar[DiamondFullPlate, $Weight] = 500;
$AccessoryVar[BlackDiamondFullPlate, $Weight] = 700;

//Custom armor descriptions
$AccessoryVar[RatSkinShirt, $MiscInfo] = "A shirt made from rat skins";
$AccessoryVar[StuddedLeatherSuit, $MiscInfo] = "A suit made from thick leather";
$AccessoryVar[ToughHideSuit, $MiscInfo] = "A suit made from a tough hide";
$AccessoryVar[IronScaleMail, $MiscInfo] = "A high quality iron scale mail";
$AccessoryVar[SteelScaleMail, $MiscInfo] = "A high quality steel scale mail";
$AccessoryVar[SteelBrigandineMail, $MiscInfo] = "A high quality steel brigandine mail";
$AccessoryVar[GoldenBrigandineMail, $MiscInfo] = "A high quality golden brigandine mail";
$AccessoryVar[GoldenChainMail, $MiscInfo] = "A high quality golden chainmail";
$AccessoryVar[CrystalChainMail, $MiscInfo] = "A high quality crystal chainmail";
$AccessoryVar[CrystalRingMail, $MiscInfo] = "A high quality crystal ringmail";
$AccessoryVar[CrystalBandedMail, $MiscInfo] = "A high quality crystal banded mail";
$AccessoryVar[CrystalSplintMail, $MiscInfo] = "A high quality crystal splintmail";
$AccessoryVar[TungstenSplintMail, $MiscInfo] = "A high quality tempered crystal splintmail";
$AccessoryVar[TungstenPlateMail, $MiscInfo] = "An almost impenetrable tempered crystal platemail";
$AccessoryVar[DiamondPlateMail, $MiscInfo] = "An almost impenetrable diamond platemail";
$AccessoryVar[DiamondFieldPlate, $MiscInfo] = "An almost impenetrable diamond field plate";
$AccessoryVar[DiamondFullPlate, $MiscInfo] = "An almost impenetrable diamond full plate";
$AccessoryVar[BlackDiamondFullPlate, $MiscInfo] = "An almost impenetrable black diamond full plate";

//$ArmorSkin[PaddedArmor] = "rpgpadded";
//$ArmorSkin[LeatherArmor] = "rpgleather";
//$ArmorSkin[StuddedLeather] = "rpgstudleather";
//$ArmorSkin[SpikedLeather] = "rpgspiked";
//$ArmorSkin[HideArmor] = "rpghide";
//$ArmorSkin[ScaleMail] = "rpgscalemail";
//$ArmorSkin[BrigandineArmor] = "rpgbrigandine";
//$ArmorSkin[ChainMail] = "rpgchainmail";
//$ArmorSkin[RingMail] = "rpgringmail";
//$ArmorSkin[BandedMail] = "rpgbandedmail";
//$ArmorSkin[SplintMail] = "rpgsplintmail";
//$ArmorSkin[BronzePlateMail] = "rpgbronzeplate";
//$ArmorSkin[PlateMail] = "rpgplatemail";
//$ArmorSkin[FieldPlateArmor] = "rpgfieldplate";
//$ArmorSkin[FullPlateArmor] = "rpgfullplate";
//$ArmorSkin[ApprenticeRobe] = "robepink";
//$ArmorSkin[LightRobe] = "robepurple";
//$ArmorSkin[BloodRobe] = "robered";
//$ArmorSkin[AdvisorRobe] = "robeblue";

//$ArmorSkin[FineRobe] = "robebrown";
//$ArmorSkin[DragonMail] = "rpghuman6";
//$ArmorSkin[KeldrinArmor] = "rpgfullplate";
//Custom armor skins
$ArmorSkin[RatSkinShirt] = "rpgpadded";
$ArmorSkin[StuddedLeatherSuit] = "rpgstudleather";
$ArmorSkin[ToughHideSuit] = "rpghide";
$ArmorSkin[IronScaleMail] = "rpgscalemail";
$ArmorSkin[SteelScaleMail] = "rpgscalemail";
$ArmorSkin[SteelBrigandineMail] = "rpgbrigandine";
$ArmorSkin[GoldenBrigandineMail] = "rpgbrigandine";
$ArmorSkin[GoldenChainMail] = "rpgchainmail";
$ArmorSkin[CrystalChainMail] = "rpgchainmail";
$ArmorSkin[CrystalRingMail] = "rpgringmail";
$ArmorSkin[CrystalBandedMail] = "rpgbandedmail";
$ArmorSkin[CrystalSplintMail] = "rpgsplintmail";
$ArmorSkin[TungstenSplintMail] = "rpgsplintmail";
$ArmorSkin[TungstenPlateMail] = "rpgplatemail";
$ArmorSkin[DiamondPlateMail] = "rpgplatemail";
$ArmorSkin[DiamondFieldPlate] = "rpgfieldplate";
$ArmorSkin[DiamondFullPlate] = "rpgfullplate";
$ArmorSkin[BlackDiamondFullPlate] = "rpgfullplate";

//the way it works is:
// $RACE[%clientId] @ $ArmorPlayerModel[WhateverArmor]
//Custom armor style?
$ArmorPlayerModel[RatSkinShirt] = "";
$ArmorPlayerModel[StuddedLeatherSuit] = "";
$ArmorPlayerModel[ToughHideSuit] = "";
$ArmorPlayerModel[IronScaleMail] = "";
$ArmorPlayerModel[SteelScaleMail] = "";
$ArmorPlayerModel[SteelBrigandineMail] = "";
$ArmorPlayerModel[GoldenBrigandineMail] = "";
$ArmorPlayerModel[GoldenChainMail] = "";
$ArmorPlayerModel[CrystalChainMail] = "";
$ArmorPlayerModel[CrystalRingMail] = "";
$ArmorPlayerModel[CrystalBandedMail] = "";
$ArmorPlayerModel[CrystalSplintMail] = "";
$ArmorPlayerModel[TungstenSplintMail] = "";
$ArmorPlayerModel[TungstenPlateMail] = "";
$ArmorPlayerModel[DiamondPlateMail] = "";
$ArmorPlayerModel[DiamondFieldPlate] = "";
$ArmorPlayerModel[DiamondFullPlate] = "";
$ArmorPlayerModel[BlackDiamondFullPlate] = "";

//$ArmorHitSound[StuddedLeather] = SoundHitLeather;
//$ArmorHitSound[SplintMail] = SoundHitChain;
//$ArmorHitSound[FullPlateArmor] = SoundHitPlate;
//$ArmorHitSound[FineRobe] = SoundHitFlesh;
//Custom armor sound
$ArmorHitSound[RatSkinShirt] = SoundHitLeather;
$ArmorHitSound[StuddedLeatherSuit] = SoundHitLeather;
$ArmorHitSound[ToughHideSuit] = SoundHitLeather;
$ArmorHitSound[IronScaleMail] = SoundHitChain;
$ArmorHitSound[SteelScaleMail] = SoundHitChain;
$ArmorHitSound[SteelBrigandineMail] = SoundHitChain;
$ArmorHitSound[GoldenBrigandineMail] = SoundHitChain;
$ArmorHitSound[GoldenChainMail] = SoundHitChain;
$ArmorHitSound[CrystalChainMail] = SoundHitChain;
$ArmorHitSound[CrystalRingMail] = SoundHitChain;
$ArmorHitSound[CrystalBandedMail] = SoundHitChain;
$ArmorHitSound[CrystalSplintMail] = SoundHitChain;
$ArmorHitSound[TungstenSplintMail] = SoundHitChain;
$ArmorHitSound[TungstenPlateMail] = SoundHitChain;
$ArmorHitSound[DiamondPlateMail] = SoundHitChain;
$ArmorHitSound[DiamondFieldPlate] = SoundHitPlate;
$ArmorHitSound[DiamondFullPlate] = SoundHitPlate;
$ArmorHitSound[BlackDiamondFullPlate] = SoundHitPlate;

//this list is used to make things easy when cycling between armors
//Custom armor orders
$ArmorList[1] = "RatSkinShirt";
$ArmorList[2] = "StuddedLeatherSuit";
$ArmorList[3] = "ToughHideSuit";
$ArmorList[4] = "IronScaleMail";
$ArmorList[5] = "SteelScaleMail";
$ArmorList[6] = "SteelBrigandineMail";
$ArmorList[7] = "GoldenBrigandineMail";
$ArmorList[8] = "GoldenChainMail";
$ArmorList[9] = "CrystalChainMail";
$ArmorList[10] = "CrystalRingMail";
$ArmorList[11] = "CrystalBandedMail";
$ArmorList[12] = "CrystalSplintMail";
$ArmorList[13] = "TungstenSplintMail";
$ArmorList[14] = "TungstenPlateMail";
$ArmorList[15] = "DiamondPlateMail";
$ArmorList[16] = "DiamondFieldPlate";
$ArmorList[17] = "DiamondFullPlate";
$ArmorList[18] = "BlackDiamondFullPlate";

//============================================================================
ItemData RatSkinShirt
{
	description = "Rat Skin Shirt";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData RatSkinShirt0
{
	description = "Rat Skin Shirt";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};
//============================================================================
ItemData StuddedLeatherSuit
{
	description = "Studded Leather Suit";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData StuddedLeatherSuit0
{
	description = "Studded Leather Suit";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData ToughHideSuit
{
	description = "Tough Hide Suit";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData ToughHideSuit0
{
	description = "Tough Hide Suit";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData IronScaleMail
{
	description = "Iron Scale Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData IronScaleMail0
{
	description = "Iron Scale Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData SteelScaleMail
{
	description = "Steel Scale Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData SteelScaleMail0
{
	description = "Steel Scale Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData SteelBrigandineMail
{
	description = "Steel Brigandine Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData SteelBrigandineMail0
{
	description = "Steel Brigandine Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData GoldenBrigandineMail
{
	description = "Golden Brigandine Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData GoldenBrigandineMail0
{
	description = "Golden Brigandine Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData GoldenChainMail
{
	description = "Golden Chain Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData GoldenChainMail0
{
	description = "Golden Chain Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData CrystalChainMail
{
	description = "Crystal Chain Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData CrystalChainMail0
{
	description = "Crystal Chain Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData CrystalRingMail
{
	description = "Crystal Ring Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData CrystalRingMail0
{
	description = "Crystal Ring Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData CrystalBandedMail
{
	description = "Crystal Banded Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData CrystalBandedMail0
{
	description = "Crystal Banded Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData CrystalSplintMail
{
	description = "Crystal Splint Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData CrystalSplintMail0
{
	description = "Crystal Splint Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData TungstenSplintMail
{
	description = "Tungsten Splint Mail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData TungstenSplintMail0
{
	description = "Tungsten Splint Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData TungstenPlateMail
{
	description = "Tungsten Platemail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData TungstenPlateMail0
{
	description = "Tungsten Platemail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData DiamondPlateMail
{
	description = "Diamond Platemail";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData DiamondPlateMail0
{
	description = "Diamond Platemail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData DiamondFieldPlate
{
	description = "Diamond Field Plate";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData DiamondFieldPlate0
{
	description = "Diamond Field Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData DiamondFullPlate
{
	description = "Diamond Full Plate";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData DiamondFullPlate0
{
	description = "Diamond Full Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

//============================================================================
ItemData BlackDiamondFullPlate
{
	description = "Black Diamond Full Plate";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData BlackDiamondFullPlate0
{
	description = "Black Diamond Full Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};