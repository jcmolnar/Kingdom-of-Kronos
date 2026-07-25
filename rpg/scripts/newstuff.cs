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
// VOID Phase 1b 2026-07-14: the 5 "Equipped"-class X0 datablocks (RedDiamondPlate0..VoidRobe0) moved to VoidLegacyEquipped.cs - see armors.cs note.
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

// Launch tuning. Impulse is MOMENTUM, not velocity - the engine divides it by
// the target's mass (SimMovement::applyImpulse, simMovement.cpp:204) and
// gravity is -9.8 (simTerrain.cpp:120), so:
//    apex(m)  = (impulse / mass)^2 / 19.6
//    airtime  = 2 * (impulse / mass) / 9.8
// Enemy bots are all mass 9.0 (EnemyArmors.cs), so 120 = ~9m apex, ~2.7s air.
// The old value of 40 was ~1m apex - lower than the bot's own jump (75).
// Players vary 8.0-19.5 mass, so heavy armor is launched less. That is left
// as-is: there is no script accessor for a live object's mass.
$SkyRender::LaunchImpulse = 120;	// ~9m apex on a mass-9 target
$SkyRender::ImpaleDelay = 2.7;		// MUST track airtime above, or the impale
					// fires before they land

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

// ===== KOKBATCH REMOVED 2026-07-14 =====
// The 37 new-model (wkok) weapon datablocks + images were stripped so dev runs the
// sanitized (live-parity) item set during the Void conversion - new-model content
// requires client downloads and must not leak to live. Full block is in git history
// (commit d0b61b4 and earlier) and regenerates from the KOKBATCH pipeline when the
// models are ready to ship.
