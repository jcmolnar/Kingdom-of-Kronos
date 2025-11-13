$AccessoryVar[TerminusEst, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[TerminusEst, $SpecialVar] = "6 600";
$AccessoryVar[TerminusEst, $Weight] = 10;
$AccessoryVar[TerminusEst, $MiscInfo] = "A sword used by a great warrior Terminus Est = This is the end";
$SkillType[TerminusEst] = $SkillSlashing;
$ItemCost[TerminusEst] = 700000000;
$SkillRestriction[TerminusEst] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
//****************************************************************************************************
//   Terminus Est
//****************************************************************************************************

ItemImageData TerminusEstImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing2;
	sfxActivate = ActivateAS;
};
ItemData TerminusEst
{
	heading = "bWeapons";
	description = "Terminus Est";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = TerminusEstImage;
	price = 0;
	showWeaponBar = true;
};
function TerminusEstImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(TerminusEst), TerminusEst);
}

function TerminusEst::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   bottomprint(%client, "<f1>Terminus Est: <f0>Attack: <f2>600    <f0>Skill Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$700,000,000    <f0>Weight: <f2>10 Lbs");
}


//ARMORS
$ItemCost[RedDiamondPlate] = 900000000;
$AccessoryVar[RedDiamondPlate, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[RedDiamondPlate, $SpecialVar] = "7 1500 4 800 3 250 10 3.0";
$AccessoryVar[RedDiamondPlate, $Weight] = 900;
$AccessoryVar[RedDiamondPlate, $MiscInfo] = "Mastery put into armor. Rare red diamond makes it stronger than Black diamond.";
$ArmorSkin[RedDiamondPlate] = "rpghuman7";
$ArmorPlayerModel[RedDiamondPlate] = "";
$ArmorHitSound[RedDiamondPlate] = SoundHitPlate;
$ArmorList[19] = "RedDiamondPlate";
//============================================================================
ItemData RedDiamondPlate
{
	description = "Red Diamond Plate";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData RedDiamondPlate0
{
	description = "Red Diamond Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};
$ItemCost[WhiteDiamondPlate] = 1000000000;
$AccessoryVar[WhiteDiamondPlate, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[WhiteDiamondPlate, $SpecialVar] = "7 2000 4 1000 3 500 10 4.0";
$AccessoryVar[WhiteDiamondPlate, $Weight] = 1500;
$AccessoryVar[WhiteDiamondPlate, $MiscInfo] = "The rarest of white diamonds put into armor!";
$ArmorSkin[WhiteDiamondPlate] = "rpghuman9";
$ArmorPlayerModel[WhiteDiamondPlate] = "";
$ArmorHitSound[WhiteDiamondPlate] = SoundHitPlate;
$ArmorList[987] = "WhiteDiamondPlate";
//============================================================================
ItemData WhiteDiamondPlate
{
	description = "White Diamond Plate";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData WhiteDiamondPlate0
{
	description = "White Diamond Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};
//============================================================================