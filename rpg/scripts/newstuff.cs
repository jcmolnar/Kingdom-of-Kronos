$AccessoryVar[TerminusEst, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[TerminusEst, $SpecialVar] = "6 600";
$AccessoryVar[TerminusEst, $Weight] = 10;
$AccessoryVar[TerminusEst, $MiscInfo] = "A sword used by a great warrior Terminus Est = This is the end";
$SkillType[TerminusEst] = $SkillSlashing;
$ItemCost[TerminusEst] = 700000000;
$SkillRestriction[TerminusEst] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[TerminusEst] = 0.5;
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
   KronosWeaponInfo(%client, "<f1>Terminus Est: <f0>Attack: <f2>600    <f0>Skill Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$700,000,000    <f0>Weight: <f2>10 Lbs");
}

//****************************************************************************************************
//   Aeco Seorei
//****************************************************************************************************
$AccessoryVar[AecoSeorei, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[AecoSeorei, $SpecialVar] = "6 600";
$AccessoryVar[AecoSeorei, $Weight] = 10;
$AccessoryVar[AecoSeorei, $MiscInfo] = "A legendary spear of unmatched power and speed";
$SkillType[AecoSeorei] = $SkillPiercing;
$ItemCost[AecoSeorei] = 700000000;
$SkillRestriction[AecoSeorei] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[AecoSeorei] = 0.5;

ItemImageData AecoSeoreiImage
{
	shapeFile  = "spear";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData AecoSeorei
{
	heading = "bWeapons";
	description = "Aeco Seorei";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = AecoSeoreiImage;
	price = 0;
	showWeaponBar = true;
};
function AecoSeoreiImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(AecoSeorei), AecoSeorei);
}

function AecoSeorei::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Aeco Seorei: <f0>Attack: <f2>600    <f0>Skill Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$700,000,000    <f0>Weight: <f2>10 Lbs");
}

//****************************************************************************************************
//   Morning Star
//****************************************************************************************************
$AccessoryVar[MorningStar, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[MorningStar, $SpecialVar] = "6 600";
$AccessoryVar[MorningStar, $Weight] = 10;
$AccessoryVar[MorningStar, $MiscInfo] = "A devastating mace of legendary power";
$SkillType[MorningStar] = $SkillBludgeoning;
$ItemCost[MorningStar] = 700000000;
$SkillRestriction[MorningStar] = $SkillBludgeoning @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[MorningStar] = 0.5;

ItemImageData MorningStarImage
{
	shapeFile  = "mace";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData MorningStar
{
	heading = "bWeapons";
	description = "Morning Star";
	className = "Weapon";
	shapeFile  = "mace";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = MorningStarImage;
	price = 0;
	showWeaponBar = true;
};
function MorningStarImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(MorningStar), MorningStar);
}

function MorningStar::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Morning Star: <f0>Attack: <f2>600    <f0>Skill Bludgeoning Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$700,000,000    <f0>Weight: <f2>10 Lbs");
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
	$SkillRestriction[RedDiamondPlate] = $SkillEndurance @ " 1950 " @ $MinRemort @ " 50";
//============================================================================
ItemImageData RedDiamondPlateImage
{
	shapeFile = "orb";  // Use orb shape (same as AdminOrb) - small and works with light
	mountPoint = 1;  // Body armor mount point
	mountOffset = {0.0, 0.0, 1.5};  // Center on chest area (Z = up/down)
	mountRotation = {0, 0, 0};  // Adjust rotation of light source

	lightType = 1;   // Always on
	lightRadius = 10;
	lightTime = 9999;
	lightColor = { 1.0, 0.0, 0.0 };  // Bright red glow (RGB: red=1.0, green=0.2, blue=0.2)
};
ItemData RedDiamondPlate
{
	shapeFile  = "";
	description = "Red Diamond Plate";
	className = "Accessory";
	shapeFile = "discammo";
	imageType = RedDiamondPlateImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData RedDiamondPlate0
{
	description = "Red Diamond Plate";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = RedDiamondPlateImage;

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
	$SkillRestriction[WhiteDiamondPlate] = $SkillEndurance @ " 2400 " @ $MinRemort @ " 75";
//============================================================================
ItemImageData WhiteDiamondPlateImage
{
	shapeFile = "orb";  // Use orb shape (same as AdminOrb) - small and works with light
	mountPoint = 1;  // Body armor mount point
	mountOffset = {0.0, 0.0, 1.5};  // Center on chest area (Z = up/down)
	mountRotation = {0, 0, 0};  // Adjust rotation of light source

	lightType = 1;   // on
	lightRadius = 20;
	lightTime = 9999;
	lightColor = { 1.0, 1.0, 1.0 };  // Bright white
};
ItemData WhiteDiamondPlate
{
	description = "White Diamond Plate";
	className = "Accessory";
	shapeFile = "discammo";
	imageType = WhiteDiamondPlateImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData WhiteDiamondPlate0
{
	description = "White Diamond Plate";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = WhiteDiamondPlateImage;

	heading = "aArmor";
};
//============================================================================

//****************************************************************************************************
//   WHITE DIAMOND VOID WEAPONS (Remort 75 Tier)
//****************************************************************************************************

// WhiteDiamondVoidCutter (Sword)
$AccessoryVar[WhiteDiamondVoidCutter, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[WhiteDiamondVoidCutter, $SpecialVar] = "6 750";
$AccessoryVar[WhiteDiamondVoidCutter, $Weight] = 2.5;
$AccessoryVar[WhiteDiamondVoidCutter, $MiscInfo] = "A blade forged from the rarest white diamond, capable of cutting through the void itself";
$SkillType[WhiteDiamondVoidCutter] = $SkillSlashing;
$ItemCost[WhiteDiamondVoidCutter] = 5000000000;
$SkillRestriction[WhiteDiamondVoidCutter] = $SkillSlashing @ " 2500 " @ $MinRemort @ " 75";
$WeaponDelay[WhiteDiamondVoidCutter] = 0.45;

ItemImageData WhiteDiamondVoidCutterImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.45;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing2;
	sfxActivate = ActivateAS;
};
ItemData WhiteDiamondVoidCutter
{
	heading = "bWeapons";
	description = "White Diamond Void Cutter";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = WhiteDiamondVoidCutterImage;
	price = 0;
	showWeaponBar = true;
};
function WhiteDiamondVoidCutterImage::onFire(%player, %slot)
{
	VoidWeaponAttack(%player, GetRange(WhiteDiamondVoidCutter), WhiteDiamondVoidCutter);
}

function WhiteDiamondVoidCutter::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>White Diamond Void Cutter: <f0>Attack: <f2>750    <f0>Skill Slashing Req @ <f2>2500 Remort 75    <f0>Speed: <f2>0.45 Seconds    <f0>Price: <f2>$5,000,000,000    <f0>Weight: <f2>2.5 Lbs");
}

// WhiteDiamondVoidCrusher (Bludgeoning)
$AccessoryVar[WhiteDiamondVoidCrusher, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[WhiteDiamondVoidCrusher, $SpecialVar] = "6 1146";
$AccessoryVar[WhiteDiamondVoidCrusher, $Weight] = 2.5;
$AccessoryVar[WhiteDiamondVoidCrusher, $MiscInfo] = "A mace forged from the rarest white diamond, capable of crushing the void itself";
$SkillType[WhiteDiamondVoidCrusher] = $SkillBludgeoning;
$ItemCost[WhiteDiamondVoidCrusher] = 5000000000;
$SkillRestriction[WhiteDiamondVoidCrusher] = $SkillBludgeoning @ " 2500 " @ $MinRemort @ " 75";

ItemImageData WhiteDiamondVoidCrusherImage
{
	shapeFile  = "hammer";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = GetDelay(WhiteDiamondVoidCrusher);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData WhiteDiamondVoidCrusher
{
	heading = "bWeapons";
	description = "White Diamond Void Crusher";
	className = "Weapon";
	shapeFile  = "hammer";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = WhiteDiamondVoidCrusherImage;
	price = 0;
	showWeaponBar = true;
};
function WhiteDiamondVoidCrusherImage::onFire(%player, %slot)
{
	VoidWeaponAttack(%player, GetRange(WhiteDiamondVoidCrusher), WhiteDiamondVoidCrusher);
}

function WhiteDiamondVoidCrusher::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>White Diamond Void Crusher: <f0>Attack: <f2>1146    <f0>Skill Bludgeoning Req @ <f2>2500 Remort 75    <f0>Speed: <f2>" @ GetDelay(WhiteDiamondVoidCrusher) @ " Seconds    <f0>Price: <f2>$5,000,000,000    <f0>Weight: <f2>2.5 Lbs");
}

// WhiteDiamondVoidImpaler (Polearm)
$AccessoryVar[WhiteDiamondVoidImpaler, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[WhiteDiamondVoidImpaler, $SpecialVar] = "6 750";
$AccessoryVar[WhiteDiamondVoidImpaler, $Weight] = 2.5;
$AccessoryVar[WhiteDiamondVoidImpaler, $MiscInfo] = "A spear forged from the rarest white diamond, capable of impaling the void itself";
$SkillType[WhiteDiamondVoidImpaler] = $SkillPiercing;
$ItemCost[WhiteDiamondVoidImpaler] = 5000000000;
$SkillRestriction[WhiteDiamondVoidImpaler] = $SkillPiercing @ " 2500 " @ $MinRemort @ " 75";
$WeaponDelay[WhiteDiamondVoidImpaler] = 0.45;

ItemImageData WhiteDiamondVoidImpalerImage
{
	shapeFile  = "trident";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.45;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData WhiteDiamondVoidImpaler
{
	heading = "bWeapons";
	description = "White Diamond Void Impaler";
	className = "Weapon";
	shapeFile  = "trident";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = WhiteDiamondVoidImpalerImage;
	price = 0;
	showWeaponBar = true;
};
function WhiteDiamondVoidImpalerImage::onFire(%player, %slot)
{
	VoidWeaponAttack(%player, GetRange(WhiteDiamondVoidImpaler), WhiteDiamondVoidImpaler);
}

function WhiteDiamondVoidImpaler::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>White Diamond Void Impaler: <f0>Attack: <f2>750    <f0>Skill Piercing Req @ <f2>2500 Remort 75    <f0>Speed: <f2>0.45 Seconds    <f0>Price: <f2>$5,000,000,000    <f0>Weight: <f2>2.5 Lbs");
}

//****************************************************************************************************
//   SPECIAL WEAPONS (Remort 100 Tier)
//****************************************************************************************************

// Final Verdict (Bludgeoning - BattleAxe) - 1% instant kill on non-boss targets
$AccessoryVar[FinalVerdict, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[FinalVerdict, $SpecialVar] = "6 900";
$AccessoryVar[FinalVerdict, $Weight] = 3.0;
$AccessoryVar[FinalVerdict, $MiscInfo] = "An executioner's axe that delivers final judgement - 1% chance to instantly kill non-boss targets. Note: Does not work on Quest, Seal or Colloseum Bots.";
$SkillType[FinalVerdict] = $SkillBludgeoning;
$ItemCost[FinalVerdict] = 10000000000;
$SkillRestriction[FinalVerdict] = $SkillBludgeoning @ " 2800 " @ $MinRemort @ " 100";
$WeaponDelay[FinalVerdict] = 0.5;
$WeaponEffect[FinalVerdict] = "INSTANT_KILL";
$WeaponEffectChance[FinalVerdict] = 1;

ItemImageData FinalVerdictImage
{
	shapeFile  = "BattleAxe";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData FinalVerdict
{
	heading = "bWeapons";
	description = "Final Verdict";
	className = "Weapon";
	shapeFile  = "BattleAxe";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = FinalVerdictImage;
	price = 0;
	showWeaponBar = true;
};
function FinalVerdictImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(FinalVerdict), FinalVerdict);
}

function FinalVerdict::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Final Verdict: <f0>Attack: <f2>900    <f0>Skill Bludgeoning Req @ <f2>2800 Remort 100    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$10,000,000,000    <f0>Weight: <f2>3.0 Lbs    <f3>SPECIAL: 1% Instant Kill (Not Quest/Seal/Colloseum Bots)");
}

// Storm Caller (Piercing - Trident) - Lightning strike every 5 hits
$AccessoryVar[StormCaller, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[StormCaller, $SpecialVar] = "6 900";
$AccessoryVar[StormCaller, $Weight] = 2.0;
$AccessoryVar[StormCaller, $MiscInfo] = "A trident that calls down lightning every 5th strike";
$SkillType[StormCaller] = $SkillPiercing;
$ItemCost[StormCaller] = 10000000000;
$SkillRestriction[StormCaller] = $SkillPiercing @ " 2800 " @ $MinRemort @ " 100";
$WeaponDelay[StormCaller] = 0.5;
$WeaponEffect[StormCaller] = "LIGHTNING_STRIKE";
$WeaponEffectFrequency[StormCaller] = 5;

ItemImageData StormCallerImage
{
	shapeFile  = "trident";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData StormCaller
{
	heading = "bWeapons";
	description = "Storm Caller";
	className = "Weapon";
	shapeFile  = "trident";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = StormCallerImage;
	price = 0;
	showWeaponBar = true;
};
function StormCallerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(StormCaller), StormCaller);
}

function StormCaller::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Storm Caller: <f0>Attack: <f2>900    <f0>Skill Piercing Req @ <f2>2800 Remort 100    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$10,000,000,000    <f0>Weight: <f2>2.0 Lbs    <f3>SPECIAL: Lightning Strike every 5 hits");
}

// World Splitter (Slashing - Claymore) - Odd hits physical, even hits magic
$AccessoryVar[WorldSplitter, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[WorldSplitter, $SpecialVar] = "6 900";
$AccessoryVar[WorldSplitter, $Weight] = 3.5;
$AccessoryVar[WorldSplitter, $MiscInfo] = "A claymore that splits between physical and magical damage, bypassing alternating defenses";
$SkillType[WorldSplitter] = $SkillSlashing;
$ItemCost[WorldSplitter] = 10000000000;
$SkillRestriction[WorldSplitter] = $SkillSlashing @ " 2800 " @ $MinRemort @ " 100";
$WeaponDelay[WorldSplitter] = 0.5;
$WeaponEffect[WorldSplitter] = "ALTERNATING_DAMAGE";

ItemImageData WorldSplitterImage
{
	shapeFile  = "katana";
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
ItemData WorldSplitter
{
	heading = "bWeapons";
	description = "World Splitter";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = WorldSplitterImage;
	price = 0;
	showWeaponBar = true;
};
function WorldSplitterImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(WorldSplitter), WorldSplitter);
}

function WorldSplitter::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>World Splitter: <f0>Attack: <f2>900    <f0>Skill Slashing Req @ <f2>2800 Remort 100    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$10,000,000,000    <f0>Weight: <f2>3.5 Lbs    <f3>SPECIAL: Alternating Phys/Magic Damage");
}
//============================================================================

//****************************************************************************************************
//   MYTHIC WEAPONS (Remort 125 Tier)
//****************************************************************************************************

// Soul Reaver (Bludgeoning - Hammer) - 5% lifesteal on every hit, kills bank Souls (max 10),
// next hit at 10 souls erupts in a Soul Nova for massive bonus damage. Souls are lost on death.
$AccessoryVar[SoulReaver, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[SoulReaver, $SpecialVar] = "6 1000";
$AccessoryVar[SoulReaver, $Weight] = 3.0;
$AccessoryVar[SoulReaver, $MiscInfo] = "A hammer that drinks the life of its victims - heals the wielder on every strike and harvests souls from kills. At 10 souls the next strike unleashes a Soul Nova. Souls are lost on death.";
$SkillType[SoulReaver] = $SkillBludgeoning;
$ItemCost[SoulReaver] = 25000000000;
$SkillRestriction[SoulReaver] = $SkillBludgeoning @ " 3000 " @ $MinRemort @ " 125";
$WeaponDelay[SoulReaver] = 0.5;
$WeaponEffect[SoulReaver] = "SOUL_HARVEST";
$WeaponEffectLifesteal[SoulReaver] = 5;	// % of damage dealt returned as HP
$WeaponEffectMaxSouls[SoulReaver] = 10;	// souls needed to trigger Soul Nova

ItemImageData SoulReaverImage
{
	shapeFile  = "hammer";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData SoulReaver
{
	heading = "bWeapons";
	description = "Soul Reaver";
	className = "Weapon";
	shapeFile  = "hammer";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = SoulReaverImage;
	price = 0;
	showWeaponBar = true;
};
function SoulReaverImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SoulReaver), SoulReaver);
}

function SoulReaver::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
   KronosWeaponInfo(%client, "<f1>Soul Reaver: <f0>Attack: <f2>1000    <f0>Skill Bludgeoning Req @ <f2>3000 Remort 125    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$25,000,000,000    <f0>Weight: <f2>3.0 Lbs    <f3>SPECIAL: 5% Lifesteal + Kills bank Souls, 10 Souls = SOUL NOVA");
}

// Sky Render (Piercing - Trident) - Every 4th hit harpoons the target skyward,
// then impales them with a delayed strike where they land.
$AccessoryVar[SkyRender, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[SkyRender, $SpecialVar] = "6 1000";
$AccessoryVar[SkyRender, $Weight] = 2.0;
$AccessoryVar[SkyRender, $MiscInfo] = "A trident that commands the sky itself - every 4th strike launches the victim into the air and impales them with a delayed skyfall strike. Note: Does not launch Quest, Seal or Colloseum Bots.";
$SkillType[SkyRender] = $SkillPiercing;
$ItemCost[SkyRender] = 25000000000;
$SkillRestriction[SkyRender] = $SkillPiercing @ " 3000 " @ $MinRemort @ " 125";
$WeaponDelay[SkyRender] = 0.5;
$WeaponEffect[SkyRender] = "SKY_LAUNCH";
$WeaponEffectFrequency[SkyRender] = 4;	// launch every Nth hit

ItemImageData SkyRenderImage
{
	shapeFile  = "trident";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData SkyRender
{
	heading = "bWeapons";
	description = "Sky Render";
	className = "Weapon";
	shapeFile  = "trident";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = SkyRenderImage;
	price = 0;
	showWeaponBar = true;
};
function SkyRenderImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SkyRender), SkyRender);
}

function SkyRender::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
   KronosWeaponInfo(%client, "<f1>Sky Render: <f0>Attack: <f2>1000    <f0>Skill Piercing Req @ <f2>3000 Remort 125    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$25,000,000,000    <f0>Weight: <f2>2.0 Lbs    <f3>SPECIAL: Every 4th hit launches target skyward + delayed impale");
}

// Echo Fang (Slashing - Katana) - Every hit echoes 2 seconds later for 40% of its damage.
// Consecutive hits within 1.5s build Momentum: +10% bonus damage per stack (max +50%).
$AccessoryVar[EchoFang, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[EchoFang, $SpecialVar] = "6 1000";
$AccessoryVar[EchoFang, $Weight] = 3.5;
$AccessoryVar[EchoFang, $MiscInfo] = "A blade that cuts through time - every wound reopens 2 seconds later for 40% of its damage, and relentless attacks build Momentum for up to +50% bonus damage";
$SkillType[EchoFang] = $SkillSlashing;
$ItemCost[EchoFang] = 25000000000;
$SkillRestriction[EchoFang] = $SkillSlashing @ " 3000 " @ $MinRemort @ " 125";
$WeaponDelay[EchoFang] = 0.5;
$WeaponEffect[EchoFang] = "ECHO_STRIKE";
$WeaponEffectEchoPercent[EchoFang] = 40;	// % of dealt damage repeated as echo
$WeaponEffectEchoDelay[EchoFang] = 2.0;		// seconds until echo fires

ItemImageData EchoFangImage
{
	shapeFile  = "katana";
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
ItemData EchoFang
{
	heading = "bWeapons";
	description = "Echo Fang";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = EchoFangImage;
	price = 0;
	showWeaponBar = true;
};
function EchoFangImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(EchoFang), EchoFang);
}

function EchoFang::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
   KronosWeaponInfo(%client, "<f1>Echo Fang: <f0>Attack: <f2>1000    <f0>Skill Slashing Req @ <f2>3000 Remort 125    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$25,000,000,000    <f0>Weight: <f2>3.5 Lbs    <f3>SPECIAL: Hits echo for 40% dmg after 2s + Momentum stacks up to +50%");
}
//============================================================================

//****************************************************************************************************
//   SPECIAL ARMORS (Remort 100 Tier)
//****************************************************************************************************

// Judgement Robe - 10% Damage Reflection
$ItemCost[JudgementRobe] = 1000000000;
$AccessoryVar[JudgementRobe, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[JudgementRobe, $SpecialVar] = "7 2500 4 1500 3 750 10 5.0";
$AccessoryVar[JudgementRobe, $Weight] = 100;
$AccessoryVar[JudgementRobe, $MiscInfo] = "A robe woven with the essence of divine judgement - 25% chance to reflect 10% of incoming damage back to attackers.";
$ArmorSkin[JudgementRobe] = "robered";
$ArmorPlayerModel[JudgementRobe] = "Robed";
$ArmorHitSound[JudgementRobe] = SoundHitFlesh;
$ArmorList[988] = "JudgementRobe";
$SkillRestriction[JudgementRobe] = $MinRemort @ " 100";
$ArmorEffect[JudgementRobe] = "RETRIBUTION";
$ArmorEffectChance[JudgementRobe] = 25;

ItemImageData JudgementRobeImage
{
	shapeFile = "orb";
	mountPoint = 1;
	mountOffset = {0.0, 0.0, 1.5};
	mountRotation = {0, 0, 0};

	lightType = 1;
	lightRadius = 15;
	lightTime = 9999;
	lightColor = { 1.0, 0.2, 0.2 };
};
ItemData JudgementRobe
{
	description = "Judgement Robe";
	className = "Accessory";
	shapeFile = "discammo";
	imageType = JudgementRobeImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData JudgementRobe0
{
	description = "Judgement Robe";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = JudgementRobeImage;

	heading = "aArmor";
};

// Storm Robe - 25% chance to zap attacker for 500 damage
$ItemCost[StormRobe] = 1000000000;
$AccessoryVar[StormRobe, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[StormRobe, $SpecialVar] = "7 2200 4 1200 3 1200 10 4.5";
$AccessoryVar[StormRobe, $Weight] = 100;
$AccessoryVar[StormRobe, $MiscInfo] = "A robe crackling with storm energy - 25% chance to zap attackers for 500 damage.";
$ArmorSkin[StormRobe] = "robeblue";
$ArmorPlayerModel[StormRobe] = "Robed";
$ArmorHitSound[StormRobe] = SoundHitFlesh;
$ArmorList[989] = "StormRobe";
$SkillRestriction[StormRobe] = $MinRemort @ " 100";
$ArmorEffect[StormRobe] = "STATIC_DISCHARGE";
$ArmorEffectChance[StormRobe] = 25;

ItemImageData StormRobeImage
{
	shapeFile = "orb";
	mountPoint = 1;
	mountOffset = {0.0, 0.0, 1.5};
	mountRotation = {0, 0, 0};

	lightType = 1;
	lightRadius = 15;
	lightTime = 9999;
	lightColor = { 0.3, 0.5, 1.0 };
};
ItemData StormRobe
{
	description = "Storm Robe";
	className = "Accessory";
	shapeFile = "discammo";
	imageType = StormRobeImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData StormRobe0
{
	description = "Storm Robe";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = StormRobeImage;

	heading = "aArmor";
};

// Void Robe - 5% chance to completely dodge an attack
$ItemCost[VoidRobe] = 1000000000;
$AccessoryVar[VoidRobe, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[VoidRobe, $SpecialVar] = "7 2100 4 1100 3 1500 10 4.0";
$AccessoryVar[VoidRobe, $Weight] = 100;
$AccessoryVar[VoidRobe, $MiscInfo] = "A robe that phases between dimensions - 5% chance to completely avoid incoming attacks.";
$ArmorSkin[VoidRobe] = "robeblack";
$ArmorPlayerModel[VoidRobe] = "Robed";
$ArmorHitSound[VoidRobe] = SoundHitFlesh;
$ArmorList[990] = "VoidRobe";
$SkillRestriction[VoidRobe] = $MinRemort @ " 100";
$ArmorEffect[VoidRobe] = "PHASE_SHIFT";
$ArmorEffectChance[VoidRobe] = 5;

ItemImageData VoidRobeImage
{
	shapeFile = "orb";
	mountPoint = 1;
	mountOffset = {0.0, 0.0, 1.5};
	mountRotation = {0, 0, 0};

	lightType = 1;
	lightRadius = 12;
	lightTime = 9999;
	lightColor = { 0.1, 0.0, 0.2 };
};
ItemData VoidRobe
{
	description = "Void Robe";
	className = "Accessory";
	shapeFile = "discammo";
	imageType = VoidRobeImage;

	heading = "eMiscellany";
	price = 0;
};
ItemData VoidRobe0
{
	description = "Void Robe";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = VoidRobeImage;

	heading = "aArmor";
};
//============================================================================
//TEST WEAPONS 
//===========================
// Gainu (Bludgeoning - BattleAxe) - 1% instant kill on non-boss targets
$AccessoryVar[Test, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[Test, $SpecialVar] = "6 900";
$AccessoryVar[Test, $Weight] = 3.0;
$AccessoryVar[Test, $MiscInfo] = "An executioner's axe that delivers final judgement - 1% chance to instantly kill non-boss targets. Note: Does not work on Quest, Seal or Colloseum Bots.";
$SkillType[Test] = $SkillBludgeoning;
$ItemCost[Test] = 10000000000;
$SkillRestriction[Test] = $SkillBludgeoning @ " 2800 " @ $MinRemort @ " 100";
$WeaponDelay[Test] = 0.5;
$WeaponEffect[Test] = "INSTANT_KILL";
$WeaponEffectChance[Test] = 1;

ItemImageData TestImage
{
	shapeFile  = "Test";
	mountPoint = 0;

	weaponType = 0;
	reloadTime = 0;
	fireTime = 0.5;
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing5;
	sfxActivate = AxeSlash2;
};
ItemData Test
{
	heading = "bWeapons";
	description = "Final Verdict";
	className = "Weapon";
	shapeFile  = "Test";
	hudIcon = "sclub";
	shadowDetailMask = 4;
	imageType = TestImage;
	price = 0;
	showWeaponBar = true;
};
function TestImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Test), Test);
}

function Test::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Test: <f0>Attack: <f2>900    <f0>Skill Bludgeoning Req @ <f2>2800 Remort 100    <f0>Speed: <f2>0.50 Seconds    <f0>Price: <f2>$10,000,000,000    <f0>Weight: <f2>3.0 Lbs    <f3>SPECIAL: 1% Instant Kill (Not Quest/Seal/Colloseum Bots)");
}

// ===== BEGIN KOKBATCH (auto-generated) =====
//=== Crimson Thorn (sword) ===
$AccessoryVar[CrimsonThorn, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[CrimsonThorn, $SpecialVar] = "6 600";
$AccessoryVar[CrimsonThorn, $Weight] = 10;
$AccessoryVar[CrimsonThorn, $MiscInfo] = "Crimson Thorn";
$SkillType[CrimsonThorn] = $SkillSlashing;
$ItemCost[CrimsonThorn] = 700000000;
$SkillRestriction[CrimsonThorn] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[CrimsonThorn] = 0.5;
ItemImageData CrimsonThornImage
{
	shapeFile  = "wkok01";
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
ItemData CrimsonThorn
{
	heading = "bWeapons";
	description = "Crimson Thorn";
	className = "Weapon";
	shapeFile  = "wkok01";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = CrimsonThornImage;
	price = 0;
	showWeaponBar = true;
};
function CrimsonThornImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrimsonThorn), CrimsonThorn);
}
function CrimsonThorn::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Crimson Thorn: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Devil's Ruin (sword) ===
$AccessoryVar[DevilsRuin, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[DevilsRuin, $SpecialVar] = "6 600";
$AccessoryVar[DevilsRuin, $Weight] = 10;
$AccessoryVar[DevilsRuin, $MiscInfo] = "Devil's Ruin";
$SkillType[DevilsRuin] = $SkillSlashing;
$ItemCost[DevilsRuin] = 700000000;
$SkillRestriction[DevilsRuin] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[DevilsRuin] = 0.5;
ItemImageData DevilsRuinImage
{
	shapeFile  = "wkok02";
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
ItemData DevilsRuin
{
	heading = "bWeapons";
	description = "Devil's Ruin";
	className = "Weapon";
	shapeFile  = "wkok02";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = DevilsRuinImage;
	price = 0;
	showWeaponBar = true;
};
function DevilsRuinImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DevilsRuin), DevilsRuin);
}
function DevilsRuin::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Devil's Ruin: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Tusker's Cleaver (axe) ===
$AccessoryVar[TuskerCleaver, $AccessoryType] = $AxeAccessoryType;
$AccessoryVar[TuskerCleaver, $SpecialVar] = "6 600";
$AccessoryVar[TuskerCleaver, $Weight] = 10;
$AccessoryVar[TuskerCleaver, $MiscInfo] = "Tusker's Cleaver";
$SkillType[TuskerCleaver] = $SkillSlashing;
$ItemCost[TuskerCleaver] = 700000000;
$SkillRestriction[TuskerCleaver] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[TuskerCleaver] = 0.5;
ItemImageData TuskerCleaverImage
{
	shapeFile  = "wkok04";
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
ItemData TuskerCleaver
{
	heading = "bWeapons";
	description = "Tusker's Cleaver";
	className = "Weapon";
	shapeFile  = "wkok04";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = TuskerCleaverImage;
	price = 0;
	showWeaponBar = true;
};
function TuskerCleaverImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(TuskerCleaver), TuskerCleaver);
}
function TuskerCleaver::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Tusker's Cleaver: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Ember of Solitude (axe) ===
$AccessoryVar[EmberSolitude, $AccessoryType] = $AxeAccessoryType;
$AccessoryVar[EmberSolitude, $SpecialVar] = "6 600";
$AccessoryVar[EmberSolitude, $Weight] = 10;
$AccessoryVar[EmberSolitude, $MiscInfo] = "Ember of Solitude";
$SkillType[EmberSolitude] = $SkillSlashing;
$ItemCost[EmberSolitude] = 700000000;
$SkillRestriction[EmberSolitude] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[EmberSolitude] = 0.5;
ItemImageData EmberSolitudeImage
{
	shapeFile  = "wkok05";
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
ItemData EmberSolitude
{
	heading = "bWeapons";
	description = "Ember of Solitude";
	className = "Weapon";
	shapeFile  = "wkok05";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = EmberSolitudeImage;
	price = 0;
	showWeaponBar = true;
};
function EmberSolitudeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(EmberSolitude), EmberSolitude);
}
function EmberSolitude::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Ember of Solitude: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Wyrmpoint (spear) ===
$AccessoryVar[Wyrmpoint, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[Wyrmpoint, $SpecialVar] = "6 600";
$AccessoryVar[Wyrmpoint, $Weight] = 10;
$AccessoryVar[Wyrmpoint, $MiscInfo] = "Wyrmpoint";
$SkillType[Wyrmpoint] = $SkillPiercing;
$ItemCost[Wyrmpoint] = 700000000;
$SkillRestriction[Wyrmpoint] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Wyrmpoint] = 0.5;
ItemImageData WyrmpointImage
{
	shapeFile  = "wkok06";
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
ItemData Wyrmpoint
{
	heading = "bWeapons";
	description = "Wyrmpoint";
	className = "Weapon";
	shapeFile  = "wkok06";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = WyrmpointImage;
	price = 0;
	showWeaponBar = true;
};
function WyrmpointImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Wyrmpoint), Wyrmpoint);
}
function Wyrmpoint::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Wyrmpoint: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Sanctified Arc (spear) ===
$AccessoryVar[SanctifiedArc, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[SanctifiedArc, $SpecialVar] = "6 600";
$AccessoryVar[SanctifiedArc, $Weight] = 10;
$AccessoryVar[SanctifiedArc, $MiscInfo] = "Sanctified Arc";
$SkillType[SanctifiedArc] = $SkillPiercing;
$ItemCost[SanctifiedArc] = 700000000;
$SkillRestriction[SanctifiedArc] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[SanctifiedArc] = 0.5;
ItemImageData SanctifiedArcImage
{
	shapeFile  = "wkok07";
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
ItemData SanctifiedArc
{
	heading = "bWeapons";
	description = "Sanctified Arc";
	className = "Weapon";
	shapeFile  = "wkok07";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SanctifiedArcImage;
	price = 0;
	showWeaponBar = true;
};
function SanctifiedArcImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SanctifiedArc), SanctifiedArc);
}
function SanctifiedArc::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Sanctified Arc: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Skullcrusher (mace) ===
$AccessoryVar[Skullcrusher, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[Skullcrusher, $SpecialVar] = "6 600";
$AccessoryVar[Skullcrusher, $Weight] = 10;
$AccessoryVar[Skullcrusher, $MiscInfo] = "Skullcrusher";
$SkillType[Skullcrusher] = $SkillBludgeoning;
$ItemCost[Skullcrusher] = 700000000;
$SkillRestriction[Skullcrusher] = $SkillBludgeoning @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Skullcrusher] = 0.5;
ItemImageData SkullcrusherImage
{
	shapeFile  = "wkok08";
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
ItemData Skullcrusher
{
	heading = "bWeapons";
	description = "Skullcrusher";
	className = "Weapon";
	shapeFile  = "wkok08";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SkullcrusherImage;
	price = 0;
	showWeaponBar = true;
};
function SkullcrusherImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Skullcrusher), Skullcrusher);
}
function Skullcrusher::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Skullcrusher: <f0>Attack: <f2>600    <f0>Bludgeoning Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Ameno's Wrath (staff) ===
$AccessoryVar[AmenoWrath, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[AmenoWrath, $SpecialVar] = "6 600";
$AccessoryVar[AmenoWrath, $Weight] = 10;
$AccessoryVar[AmenoWrath, $MiscInfo] = "Ameno's Wrath";
$SkillType[AmenoWrath] = $SkillPiercing;
$ItemCost[AmenoWrath] = 700000000;
$SkillRestriction[AmenoWrath] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[AmenoWrath] = 0.5;
ItemImageData AmenoWrathImage
{
	shapeFile  = "wkok09";
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
ItemData AmenoWrath
{
	heading = "bWeapons";
	description = "Ameno's Wrath";
	className = "Weapon";
	shapeFile  = "wkok09";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = AmenoWrathImage;
	price = 0;
	showWeaponBar = true;
};
function AmenoWrathImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(AmenoWrath), AmenoWrath);
}
function AmenoWrath::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Ameno's Wrath: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Shadowfang (dagger) ===
$AccessoryVar[Shadowfang, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[Shadowfang, $SpecialVar] = "6 600";
$AccessoryVar[Shadowfang, $Weight] = 10;
$AccessoryVar[Shadowfang, $MiscInfo] = "Shadowfang";
$SkillType[Shadowfang] = $SkillPiercing;
$ItemCost[Shadowfang] = 700000000;
$SkillRestriction[Shadowfang] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Shadowfang] = 0.5;
ItemImageData ShadowfangImage
{
	shapeFile  = "wkok10";
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
ItemData Shadowfang
{
	heading = "bWeapons";
	description = "Shadowfang";
	className = "Weapon";
	shapeFile  = "wkok10";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = ShadowfangImage;
	price = 0;
	showWeaponBar = true;
};
function ShadowfangImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Shadowfang), Shadowfang);
}
function Shadowfang::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Shadowfang: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Abyssal Heart (sword) ===
$AccessoryVar[AbyssalHeart, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[AbyssalHeart, $SpecialVar] = "6 600";
$AccessoryVar[AbyssalHeart, $Weight] = 10;
$AccessoryVar[AbyssalHeart, $MiscInfo] = "Abyssal Heart";
$SkillType[AbyssalHeart] = $SkillSlashing;
$ItemCost[AbyssalHeart] = 700000000;
$SkillRestriction[AbyssalHeart] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[AbyssalHeart] = 0.5;
ItemImageData AbyssalHeartImage
{
	shapeFile  = "wkok11";
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
ItemData AbyssalHeart
{
	heading = "bWeapons";
	description = "Abyssal Heart";
	className = "Weapon";
	shapeFile  = "wkok11";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = AbyssalHeartImage;
	price = 0;
	showWeaponBar = true;
};
function AbyssalHeartImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(AbyssalHeart), AbyssalHeart);
}
function AbyssalHeart::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Abyssal Heart: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Aetheris Edge (sword) ===
$AccessoryVar[AetherisEdge, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[AetherisEdge, $SpecialVar] = "6 600";
$AccessoryVar[AetherisEdge, $Weight] = 10;
$AccessoryVar[AetherisEdge, $MiscInfo] = "Aetheris Edge";
$SkillType[AetherisEdge] = $SkillSlashing;
$ItemCost[AetherisEdge] = 700000000;
$SkillRestriction[AetherisEdge] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[AetherisEdge] = 0.5;
ItemImageData AetherisEdgeImage
{
	shapeFile  = "wkok12";
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
ItemData AetherisEdge
{
	heading = "bWeapons";
	description = "Aetheris Edge";
	className = "Weapon";
	shapeFile  = "wkok12";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = AetherisEdgeImage;
	price = 0;
	showWeaponBar = true;
};
function AetherisEdgeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(AetherisEdge), AetherisEdge);
}
function AetherisEdge::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Aetheris Edge: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Astral Edge (sword) ===
$AccessoryVar[AstralEdge, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[AstralEdge, $SpecialVar] = "6 600";
$AccessoryVar[AstralEdge, $Weight] = 10;
$AccessoryVar[AstralEdge, $MiscInfo] = "Astral Edge";
$SkillType[AstralEdge] = $SkillSlashing;
$ItemCost[AstralEdge] = 700000000;
$SkillRestriction[AstralEdge] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[AstralEdge] = 0.5;
ItemImageData AstralEdgeImage
{
	shapeFile  = "wkok13";
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
ItemData AstralEdge
{
	heading = "bWeapons";
	description = "Astral Edge";
	className = "Weapon";
	shapeFile  = "wkok13";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = AstralEdgeImage;
	price = 0;
	showWeaponBar = true;
};
function AstralEdgeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(AstralEdge), AstralEdge);
}
function AstralEdge::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Astral Edge: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Dragonslayer (sword) ===
$AccessoryVar[Dragonslayer, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[Dragonslayer, $SpecialVar] = "6 600";
$AccessoryVar[Dragonslayer, $Weight] = 10;
$AccessoryVar[Dragonslayer, $MiscInfo] = "Dragonslayer";
$SkillType[Dragonslayer] = $SkillSlashing;
$ItemCost[Dragonslayer] = 700000000;
$SkillRestriction[Dragonslayer] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Dragonslayer] = 0.5;
ItemImageData DragonslayerImage
{
	shapeFile  = "wkok14";
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
ItemData Dragonslayer
{
	heading = "bWeapons";
	description = "Dragonslayer";
	className = "Weapon";
	shapeFile  = "wkok14";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = DragonslayerImage;
	price = 0;
	showWeaponBar = true;
};
function DragonslayerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Dragonslayer), Dragonslayer);
}
function Dragonslayer::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Dragonslayer: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Frostbite (sword) ===
$AccessoryVar[Frostbite, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[Frostbite, $SpecialVar] = "6 600";
$AccessoryVar[Frostbite, $Weight] = 10;
$AccessoryVar[Frostbite, $MiscInfo] = "Frostbite";
$SkillType[Frostbite] = $SkillSlashing;
$ItemCost[Frostbite] = 700000000;
$SkillRestriction[Frostbite] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Frostbite] = 0.5;
ItemImageData FrostbiteImage
{
	shapeFile  = "wkok15";
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
ItemData Frostbite
{
	heading = "bWeapons";
	description = "Frostbite";
	className = "Weapon";
	shapeFile  = "wkok15";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = FrostbiteImage;
	price = 0;
	showWeaponBar = true;
};
function FrostbiteImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Frostbite), Frostbite);
}
function Frostbite::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Frostbite: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Valor's Edge (sword) ===
$AccessoryVar[ValorsEdge, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[ValorsEdge, $SpecialVar] = "6 600";
$AccessoryVar[ValorsEdge, $Weight] = 10;
$AccessoryVar[ValorsEdge, $MiscInfo] = "Valor's Edge";
$SkillType[ValorsEdge] = $SkillSlashing;
$ItemCost[ValorsEdge] = 700000000;
$SkillRestriction[ValorsEdge] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[ValorsEdge] = 0.5;
ItemImageData ValorsEdgeImage
{
	shapeFile  = "wkok16";
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
ItemData ValorsEdge
{
	heading = "bWeapons";
	description = "Valor's Edge";
	className = "Weapon";
	shapeFile  = "wkok16";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = ValorsEdgeImage;
	price = 0;
	showWeaponBar = true;
};
function ValorsEdgeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(ValorsEdge), ValorsEdge);
}
function ValorsEdge::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Valor's Edge: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Void Reaver (sword) ===
$AccessoryVar[VoidReaver, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[VoidReaver, $SpecialVar] = "6 600";
$AccessoryVar[VoidReaver, $Weight] = 10;
$AccessoryVar[VoidReaver, $MiscInfo] = "Void Reaver";
$SkillType[VoidReaver] = $SkillSlashing;
$ItemCost[VoidReaver] = 700000000;
$SkillRestriction[VoidReaver] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[VoidReaver] = 0.5;
ItemImageData VoidReaverImage
{
	shapeFile  = "wkok17";
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
ItemData VoidReaver
{
	heading = "bWeapons";
	description = "Void Reaver";
	className = "Weapon";
	shapeFile  = "wkok17";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = VoidReaverImage;
	price = 0;
	showWeaponBar = true;
};
function VoidReaverImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(VoidReaver), VoidReaver);
}
function VoidReaver::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Void Reaver: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Crimson Fiend (sword) ===
$AccessoryVar[CrimsonFiend, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[CrimsonFiend, $SpecialVar] = "6 600";
$AccessoryVar[CrimsonFiend, $Weight] = 10;
$AccessoryVar[CrimsonFiend, $MiscInfo] = "Crimson Fiend";
$SkillType[CrimsonFiend] = $SkillSlashing;
$ItemCost[CrimsonFiend] = 700000000;
$SkillRestriction[CrimsonFiend] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[CrimsonFiend] = 0.5;
ItemImageData CrimsonFiendImage
{
	shapeFile  = "wkok18";
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
ItemData CrimsonFiend
{
	heading = "bWeapons";
	description = "Crimson Fiend";
	className = "Weapon";
	shapeFile  = "wkok18";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = CrimsonFiendImage;
	price = 0;
	showWeaponBar = true;
};
function CrimsonFiendImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(CrimsonFiend), CrimsonFiend);
}
function CrimsonFiend::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Crimson Fiend: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Magma Hook (sword) ===
$AccessoryVar[MagmaHook, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[MagmaHook, $SpecialVar] = "6 600";
$AccessoryVar[MagmaHook, $Weight] = 10;
$AccessoryVar[MagmaHook, $MiscInfo] = "Magma Hook";
$SkillType[MagmaHook] = $SkillSlashing;
$ItemCost[MagmaHook] = 700000000;
$SkillRestriction[MagmaHook] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[MagmaHook] = 0.5;
ItemImageData MagmaHookImage
{
	shapeFile  = "wkok19";
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
ItemData MagmaHook
{
	heading = "bWeapons";
	description = "Magma Hook";
	className = "Weapon";
	shapeFile  = "wkok19";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = MagmaHookImage;
	price = 0;
	showWeaponBar = true;
};
function MagmaHookImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(MagmaHook), MagmaHook);
}
function MagmaHook::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Magma Hook: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Glavenus Edge (sword) ===
$AccessoryVar[Glavenus, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[Glavenus, $SpecialVar] = "6 600";
$AccessoryVar[Glavenus, $Weight] = 10;
$AccessoryVar[Glavenus, $MiscInfo] = "Glavenus Edge";
$SkillType[Glavenus] = $SkillSlashing;
$ItemCost[Glavenus] = 700000000;
$SkillRestriction[Glavenus] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Glavenus] = 0.5;
ItemImageData GlavenusImage
{
	shapeFile  = "wkok20";
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
ItemData Glavenus
{
	heading = "bWeapons";
	description = "Glavenus Edge";
	className = "Weapon";
	shapeFile  = "wkok20";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = GlavenusImage;
	price = 0;
	showWeaponBar = true;
};
function GlavenusImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Glavenus), Glavenus);
}
function Glavenus::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Glavenus Edge: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Mythic Dragonblade (sword) ===
$AccessoryVar[MythicDragon, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[MythicDragon, $SpecialVar] = "6 600";
$AccessoryVar[MythicDragon, $Weight] = 10;
$AccessoryVar[MythicDragon, $MiscInfo] = "Mythic Dragonblade";
$SkillType[MythicDragon] = $SkillSlashing;
$ItemCost[MythicDragon] = 700000000;
$SkillRestriction[MythicDragon] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[MythicDragon] = 0.5;
ItemImageData MythicDragonImage
{
	shapeFile  = "wkok21";
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
ItemData MythicDragon
{
	heading = "bWeapons";
	description = "Mythic Dragonblade";
	className = "Weapon";
	shapeFile  = "wkok21";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = MythicDragonImage;
	price = 0;
	showWeaponBar = true;
};
function MythicDragonImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(MythicDragon), MythicDragon);
}
function MythicDragon::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Mythic Dragonblade: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Schiavona (sword) ===
$AccessoryVar[Schiavona, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[Schiavona, $SpecialVar] = "6 600";
$AccessoryVar[Schiavona, $Weight] = 10;
$AccessoryVar[Schiavona, $MiscInfo] = "Schiavona";
$SkillType[Schiavona] = $SkillSlashing;
$ItemCost[Schiavona] = 700000000;
$SkillRestriction[Schiavona] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Schiavona] = 0.5;
ItemImageData SchiavonaImage
{
	shapeFile  = "wkok22";
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
ItemData Schiavona
{
	heading = "bWeapons";
	description = "Schiavona";
	className = "Weapon";
	shapeFile  = "wkok22";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SchiavonaImage;
	price = 0;
	showWeaponBar = true;
};
function SchiavonaImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Schiavona), Schiavona);
}
function Schiavona::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Schiavona: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Spectral Edge (sword) ===
$AccessoryVar[SpectralEdge, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[SpectralEdge, $SpecialVar] = "6 600";
$AccessoryVar[SpectralEdge, $Weight] = 10;
$AccessoryVar[SpectralEdge, $MiscInfo] = "Spectral Edge";
$SkillType[SpectralEdge] = $SkillSlashing;
$ItemCost[SpectralEdge] = 700000000;
$SkillRestriction[SpectralEdge] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[SpectralEdge] = 0.5;
ItemImageData SpectralEdgeImage
{
	shapeFile  = "wkok23";
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
ItemData SpectralEdge
{
	heading = "bWeapons";
	description = "Spectral Edge";
	className = "Weapon";
	shapeFile  = "wkok23";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SpectralEdgeImage;
	price = 0;
	showWeaponBar = true;
};
function SpectralEdgeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SpectralEdge), SpectralEdge);
}
function SpectralEdge::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Spectral Edge: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Demon's Edge (sword) ===
$AccessoryVar[DemonsEdge, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[DemonsEdge, $SpecialVar] = "6 600";
$AccessoryVar[DemonsEdge, $Weight] = 10;
$AccessoryVar[DemonsEdge, $MiscInfo] = "Demon's Edge";
$SkillType[DemonsEdge] = $SkillSlashing;
$ItemCost[DemonsEdge] = 700000000;
$SkillRestriction[DemonsEdge] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[DemonsEdge] = 0.5;
ItemImageData DemonsEdgeImage
{
	shapeFile  = "wkok24";
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
ItemData DemonsEdge
{
	heading = "bWeapons";
	description = "Demon's Edge";
	className = "Weapon";
	shapeFile  = "wkok24";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = DemonsEdgeImage;
	price = 0;
	showWeaponBar = true;
};
function DemonsEdgeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DemonsEdge), DemonsEdge);
}
function DemonsEdge::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Demon's Edge: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Blade of Gainu (sword) ===
$AccessoryVar[GainuBlade, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[GainuBlade, $SpecialVar] = "6 600";
$AccessoryVar[GainuBlade, $Weight] = 10;
$AccessoryVar[GainuBlade, $MiscInfo] = "Blade of Gainu";
$SkillType[GainuBlade] = $SkillSlashing;
$ItemCost[GainuBlade] = 700000000;
$SkillRestriction[GainuBlade] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[GainuBlade] = 0.5;
ItemImageData GainuBladeImage
{
	shapeFile  = "wkok25";
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
ItemData GainuBlade
{
	heading = "bWeapons";
	description = "Blade of Gainu";
	className = "Weapon";
	shapeFile  = "wkok25";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = GainuBladeImage;
	price = 0;
	showWeaponBar = true;
};
function GainuBladeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(GainuBlade), GainuBlade);
}
function GainuBlade::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Blade of Gainu: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Devil's Scythe (axe) ===
$AccessoryVar[DevilScythe, $AccessoryType] = $AxeAccessoryType;
$AccessoryVar[DevilScythe, $SpecialVar] = "6 600";
$AccessoryVar[DevilScythe, $Weight] = 10;
$AccessoryVar[DevilScythe, $MiscInfo] = "Devil's Scythe";
$SkillType[DevilScythe] = $SkillSlashing;
$ItemCost[DevilScythe] = 700000000;
$SkillRestriction[DevilScythe] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[DevilScythe] = 0.5;
ItemImageData DevilScytheImage
{
	shapeFile  = "wkok26";
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
ItemData DevilScythe
{
	heading = "bWeapons";
	description = "Devil's Scythe";
	className = "Weapon";
	shapeFile  = "wkok26";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = DevilScytheImage;
	price = 0;
	showWeaponBar = true;
};
function DevilScytheImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DevilScythe), DevilScythe);
}
function DevilScythe::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Devil's Scythe: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Twin Cleaver (axe) ===
$AccessoryVar[TwinCleaver, $AccessoryType] = $AxeAccessoryType;
$AccessoryVar[TwinCleaver, $SpecialVar] = "6 600";
$AccessoryVar[TwinCleaver, $Weight] = 10;
$AccessoryVar[TwinCleaver, $MiscInfo] = "Twin Cleaver";
$SkillType[TwinCleaver] = $SkillSlashing;
$ItemCost[TwinCleaver] = 700000000;
$SkillRestriction[TwinCleaver] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[TwinCleaver] = 0.5;
ItemImageData TwinCleaverImage
{
	shapeFile  = "wkok27";
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
ItemData TwinCleaver
{
	heading = "bWeapons";
	description = "Twin Cleaver";
	className = "Weapon";
	shapeFile  = "wkok27";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = TwinCleaverImage;
	price = 0;
	showWeaponBar = true;
};
function TwinCleaverImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(TwinCleaver), TwinCleaver);
}
function TwinCleaver::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Twin Cleaver: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Runic Reaver (axe) ===
$AccessoryVar[RunicReaver, $AccessoryType] = $AxeAccessoryType;
$AccessoryVar[RunicReaver, $SpecialVar] = "6 600";
$AccessoryVar[RunicReaver, $Weight] = 10;
$AccessoryVar[RunicReaver, $MiscInfo] = "Runic Reaver";
$SkillType[RunicReaver] = $SkillSlashing;
$ItemCost[RunicReaver] = 700000000;
$SkillRestriction[RunicReaver] = $SkillSlashing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[RunicReaver] = 0.5;
ItemImageData RunicReaverImage
{
	shapeFile  = "wkok28";
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
ItemData RunicReaver
{
	heading = "bWeapons";
	description = "Runic Reaver";
	className = "Weapon";
	shapeFile  = "wkok28";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = RunicReaverImage;
	price = 0;
	showWeaponBar = true;
};
function RunicReaverImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(RunicReaver), RunicReaver);
}
function RunicReaver::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Runic Reaver: <f0>Attack: <f2>600    <f0>Slashing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Ancient Lance (spear) ===
$AccessoryVar[AncientLance, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[AncientLance, $SpecialVar] = "6 600";
$AccessoryVar[AncientLance, $Weight] = 10;
$AccessoryVar[AncientLance, $MiscInfo] = "Ancient Lance";
$SkillType[AncientLance] = $SkillPiercing;
$ItemCost[AncientLance] = 700000000;
$SkillRestriction[AncientLance] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[AncientLance] = 0.5;
ItemImageData AncientLanceImage
{
	shapeFile  = "wkok29";
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
ItemData AncientLance
{
	heading = "bWeapons";
	description = "Ancient Lance";
	className = "Weapon";
	shapeFile  = "wkok29";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = AncientLanceImage;
	price = 0;
	showWeaponBar = true;
};
function AncientLanceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(AncientLance), AncientLance);
}
function AncientLance::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Ancient Lance: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Fangtian Ji (spear) ===
$AccessoryVar[FangtianJi, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[FangtianJi, $SpecialVar] = "6 600";
$AccessoryVar[FangtianJi, $Weight] = 10;
$AccessoryVar[FangtianJi, $MiscInfo] = "Fangtian Ji";
$SkillType[FangtianJi] = $SkillPiercing;
$ItemCost[FangtianJi] = 700000000;
$SkillRestriction[FangtianJi] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[FangtianJi] = 0.5;
ItemImageData FangtianJiImage
{
	shapeFile  = "wkok30";
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
ItemData FangtianJi
{
	heading = "bWeapons";
	description = "Fangtian Ji";
	className = "Weapon";
	shapeFile  = "wkok30";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = FangtianJiImage;
	price = 0;
	showWeaponBar = true;
};
function FangtianJiImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(FangtianJi), FangtianJi);
}
function FangtianJi::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Fangtian Ji: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Mogura Trident (spear) ===
$AccessoryVar[Mogura, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[Mogura, $SpecialVar] = "6 600";
$AccessoryVar[Mogura, $Weight] = 10;
$AccessoryVar[Mogura, $MiscInfo] = "Mogura Trident";
$SkillType[Mogura] = $SkillPiercing;
$ItemCost[Mogura] = 700000000;
$SkillRestriction[Mogura] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Mogura] = 0.5;
ItemImageData MoguraImage
{
	shapeFile  = "wkok31";
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
ItemData Mogura
{
	heading = "bWeapons";
	description = "Mogura Trident";
	className = "Weapon";
	shapeFile  = "wkok31";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = MoguraImage;
	price = 0;
	showWeaponBar = true;
};
function MoguraImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Mogura), Mogura);
}
function Mogura::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Mogura Trident: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Pikestaff (spear) ===
$AccessoryVar[Pikestaff, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[Pikestaff, $SpecialVar] = "6 600";
$AccessoryVar[Pikestaff, $Weight] = 10;
$AccessoryVar[Pikestaff, $MiscInfo] = "Pikestaff";
$SkillType[Pikestaff] = $SkillPiercing;
$ItemCost[Pikestaff] = 700000000;
$SkillRestriction[Pikestaff] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[Pikestaff] = 0.5;
ItemImageData PikestaffImage
{
	shapeFile  = "wkok32";
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
ItemData Pikestaff
{
	heading = "bWeapons";
	description = "Pikestaff";
	className = "Weapon";
	shapeFile  = "wkok32";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = PikestaffImage;
	price = 0;
	showWeaponBar = true;
};
function PikestaffImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(Pikestaff), Pikestaff);
}
function Pikestaff::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Pikestaff: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== War Hammer (hammer) ===
$AccessoryVar[WarHammer, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[WarHammer, $SpecialVar] = "6 600";
$AccessoryVar[WarHammer, $Weight] = 10;
$AccessoryVar[WarHammer, $MiscInfo] = "War Hammer";
$SkillType[WarHammer] = $SkillBludgeoning;
$ItemCost[WarHammer] = 700000000;
$SkillRestriction[WarHammer] = $SkillBludgeoning @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[WarHammer] = 0.5;
ItemImageData WarHammerImage
{
	shapeFile  = "wkok33";
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
ItemData WarHammer
{
	heading = "bWeapons";
	description = "War Hammer";
	className = "Weapon";
	shapeFile  = "wkok33";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = WarHammerImage;
	price = 0;
	showWeaponBar = true;
};
function WarHammerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(WarHammer), WarHammer);
}
function WarHammer::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>War Hammer: <f0>Attack: <f2>600    <f0>Bludgeoning Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Dwarven Maul (hammer) ===
$AccessoryVar[DwarvenMaul, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[DwarvenMaul, $SpecialVar] = "6 600";
$AccessoryVar[DwarvenMaul, $Weight] = 10;
$AccessoryVar[DwarvenMaul, $MiscInfo] = "Dwarven Maul";
$SkillType[DwarvenMaul] = $SkillBludgeoning;
$ItemCost[DwarvenMaul] = 700000000;
$SkillRestriction[DwarvenMaul] = $SkillBludgeoning @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[DwarvenMaul] = 0.5;
ItemImageData DwarvenMaulImage
{
	shapeFile  = "wkok34";
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
ItemData DwarvenMaul
{
	heading = "bWeapons";
	description = "Dwarven Maul";
	className = "Weapon";
	shapeFile  = "wkok34";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = DwarvenMaulImage;
	price = 0;
	showWeaponBar = true;
};
function DwarvenMaulImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(DwarvenMaul), DwarvenMaul);
}
function DwarvenMaul::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Dwarven Maul: <f0>Attack: <f2>600    <f0>Bludgeoning Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Rune Hammer (hammer) ===
$AccessoryVar[RuneHammer, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[RuneHammer, $SpecialVar] = "6 600";
$AccessoryVar[RuneHammer, $Weight] = 10;
$AccessoryVar[RuneHammer, $MiscInfo] = "Rune Hammer";
$SkillType[RuneHammer] = $SkillBludgeoning;
$ItemCost[RuneHammer] = 700000000;
$SkillRestriction[RuneHammer] = $SkillBludgeoning @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[RuneHammer] = 0.5;
ItemImageData RuneHammerImage
{
	shapeFile  = "wkok35";
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
ItemData RuneHammer
{
	heading = "bWeapons";
	description = "Rune Hammer";
	className = "Weapon";
	shapeFile  = "wkok35";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = RuneHammerImage;
	price = 0;
	showWeaponBar = true;
};
function RuneHammerImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(RuneHammer), RuneHammer);
}
function RuneHammer::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Rune Hammer: <f0>Attack: <f2>600    <f0>Bludgeoning Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Paladin's Mace (mace) ===
$AccessoryVar[PaladinMace, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[PaladinMace, $SpecialVar] = "6 600";
$AccessoryVar[PaladinMace, $Weight] = 10;
$AccessoryVar[PaladinMace, $MiscInfo] = "Paladin's Mace";
$SkillType[PaladinMace] = $SkillBludgeoning;
$ItemCost[PaladinMace] = 700000000;
$SkillRestriction[PaladinMace] = $SkillBludgeoning @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[PaladinMace] = 0.5;
ItemImageData PaladinMaceImage
{
	shapeFile  = "wkok36";
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
ItemData PaladinMace
{
	heading = "bWeapons";
	description = "Paladin's Mace";
	className = "Weapon";
	shapeFile  = "wkok36";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = PaladinMaceImage;
	price = 0;
	showWeaponBar = true;
};
function PaladinMaceImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(PaladinMace), PaladinMace);
}
function PaladinMace::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Paladin's Mace: <f0>Attack: <f2>600    <f0>Bludgeoning Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Glacial Staff (staff) ===
$AccessoryVar[GlacialStaff, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[GlacialStaff, $SpecialVar] = "6 600";
$AccessoryVar[GlacialStaff, $Weight] = 10;
$AccessoryVar[GlacialStaff, $MiscInfo] = "Glacial Staff";
$SkillType[GlacialStaff] = $SkillPiercing;
$ItemCost[GlacialStaff] = 700000000;
$SkillRestriction[GlacialStaff] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[GlacialStaff] = 0.5;
ItemImageData GlacialStaffImage
{
	shapeFile  = "wkok37";
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
ItemData GlacialStaff
{
	heading = "bWeapons";
	description = "Glacial Staff";
	className = "Weapon";
	shapeFile  = "wkok37";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = GlacialStaffImage;
	price = 0;
	showWeaponBar = true;
};
function GlacialStaffImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(GlacialStaff), GlacialStaff);
}
function GlacialStaff::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Glacial Staff: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

//=== Staff of Midas (staff) ===
$AccessoryVar[MidasStaff, $AccessoryType] = $PolearmAccessoryType;
$AccessoryVar[MidasStaff, $SpecialVar] = "6 600";
$AccessoryVar[MidasStaff, $Weight] = 10;
$AccessoryVar[MidasStaff, $MiscInfo] = "Staff of Midas";
$SkillType[MidasStaff] = $SkillPiercing;
$ItemCost[MidasStaff] = 700000000;
$SkillRestriction[MidasStaff] = $SkillPiercing @ " 2030 " @ $MinRemort @ " 50";
$WeaponDelay[MidasStaff] = 0.5;
ItemImageData MidasStaffImage
{
	shapeFile  = "wkok38";
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
ItemData MidasStaff
{
	heading = "bWeapons";
	description = "Staff of Midas";
	className = "Weapon";
	shapeFile  = "wkok38";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = MidasStaffImage;
	price = 0;
	showWeaponBar = true;
};
function MidasStaffImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(MidasStaff), MidasStaff);
}
function MidasStaff::onMount(%player,%item,$WeaponSlot)
{   %client = Player::getclient(%player);
	KronosWeaponInfo(%client, "<f1>Staff of Midas: <f0>Attack: <f2>600    <f0>Piercing Req @ <f2>2030 Remort 50    <f0>Speed: <f2>0.5 Seconds    <f0>Weight: <f2>10 Lbs");
}

// ===== END KOKBATCH =====
