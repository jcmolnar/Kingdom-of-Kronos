//============================================================================
// weapons.cs — SPLIT 2026-07-17 at commit 4aa0a3d (was 3,226 lines)
// Mechanical text move, no behavior change. Line-range map:
//   original lines 1-890     -> weapons_combat.cs (stat tables + combat engine + cost gen)
//   original lines 3047-3227 (EOF; includes SLOT PURGE tombstone + commented base_weapons exec) -> weapons_combat.cs (DPS diagnostics + SLOT PURGE tombstone)
//   original lines 891-3046  -> KEPT BELOW, byte-identical.
// WARNING: ItemData declaration ORDER below assigns engine item indices
// (positional). NEVER reorder, insert, remove, or relocate these datablocks.
// Server.cs exec slot 287 unchanged. Refactors never delete code.
//============================================================================
exec("weapons_combat");
// ==== BEGIN ORIGINAL PAYLOAD ====
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
   KronosWeaponInfo(%client, "<f1>Rusty Iron Blade: <f0>Attack: <f2>9    <f0>Skill Slashing Req @ <f2>0    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$68    <f0>Weight: <f2>4 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Sharp Iron Blade: <f0>Attack: <f2>16    <f0>Skill Slashing Req @ <f2>45    <f0>Speed: <f2>1.01 Second    <f0>Price: <f2>$374    <f0>Weight: <f2>4.5 Lbs ");
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
   KronosWeaponInfo(%client, "<f1>Iron Broad Sword: <f0>Attack: <f2>27    <f0>Skill Slashing Req @ <f2>90    <f0>Speed: <f2>1.12 Seconds    <f0>Price: <f2>$1,757    <f0>Weight: <f2>5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Steel Broad Sword: <f0>Attack: <f2>40    <f0>Skill Slashing Req @ <f2>140    <f0>Speed: <f2>1.12 Seconds    <f0>Price: <f2>$5,287    <f0>Weight: <f2>5.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Steel Long Sword: <f0>Attack: <f2>55    <f0>Skill Slashing Req @ <f2>200    <f0>Speed: <f2>1.34 Seconds    <f0>Price: <f2>$12,449    <f0>Weight: <f2>6 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Golden Long Sword: <f0>Attack: <f2>70    <f0>Skill Slashing Req @ <f2>260    <f0>Speed: <f2>1.46 Seconds    <f0>Price: <f2>$22,957    <f0>Weight: <f2>6.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Golden Bastard Sword: <f0>Attack: <f2>90    <f0>Skill Slashing Req @ <f2>340    <f0>Speed: <f2>1.57 Seconds    <f0>Price: <f2>$43,531    <f0>Weight: <f2>7 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Crystal Bastard Sword: <f0>Attack: <f2>115    <f0>Skill Slashing Req @ <f2>440    <f0>Speed: <f2>1.68 Seconds    <f0>Price: <f2>$83,526    <f0>Weight: <f2>7.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Tempered Crystal Bastard Sword: <f0>Attack: <f2>140    <f0>Skill Slashing Req @ <f2>540    <f0>Speed: <f2>1.79 Seconds    <f0>Price: <f2>$136,211    <f0>Weight: <f2>8 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Crystal Claymore: <f0>Attack: <f2>165    <f0>Skill Slashing Req @ <f2>640    <f0>Speed: <f2>1.91 Seconds    <f0>Price: <f2>$199,898    <f0>Weight: <f2>8.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Claymore: <f0>Attack: <f2>195    <f0>Skill Slashing Req @ <f2>740    <f0>Speed: <f2>1.91 Seconds    <f0>Price: <f2>$370,895    <f0>Weight: <f2>8.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Legend Sword: <f0>Attack: <f2>220    <f0>Skill Slashing Req @ <f2>840 Remort 1    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$9,425,500    <f0>Weight: <f2>4 Lbs");
}

//****************************************************************************************************
//   SEAL FIGHTER BLADE (Dedicated Seal Battle weapon - prevents collision with world mobs)
//****************************************************************************************************

ItemImageData SealFighterBladeImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(SealFighterBlade);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing2;
	sfxActivate = ActivateAS;
};
ItemData SealFighterBlade
{
	heading = "bWeapons";
	description = "Seal Fighter Blade";
	className = "Weapon";
	shapeFile  = "elfinblade";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SealFighterBladeImage;
	price = 0;
	showWeaponBar = true;
};
function SealFighterBladeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SealFighterBlade), SealFighterBlade);
}

function SealFighterBlade::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Seal Fighter Blade: <f0>Attack: <f2>220    <f0>Skill Slashing    <f0>Speed: <f2>0.89 Seconds");
}

//****************************************************************************************************
//   SEAL GUARDIAN BLADE (Dedicated Seal Battle weapon - prevents collision with world mobs)
//****************************************************************************************************

ItemImageData SealGuardianBladeImage
{
	shapeFile  = "elfinblade";
	mountPoint = 0;

	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = GetDelay(SealGuardianBlade);
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing2;
	sfxActivate = ActivateAS;
};
ItemData SealGuardianBlade
{
	heading = "bWeapons";
	description = "Seal Guardian Blade";
	className = "Weapon";
	shapeFile  = "elfinblade";
	hudIcon = "blaster";
	shadowDetailMask = 4;
	imageType = SealGuardianBladeImage;
	price = 0;
	showWeaponBar = true;
};
function SealGuardianBladeImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(SealGuardianBlade), SealGuardianBlade);
}

function SealGuardianBlade::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Seal Guardian Blade: <f0>Attack: <f2>220    <f0>Skill Slashing    <f0>Speed: <f2>0.89 Seconds");
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
   KronosWeaponInfo(%client, "<f1>Black Diamond Dream Sword: <f0>Attack: <f2>300    <f0>Skill Slashing Req @ <f2>1200 Remort 10    <f0>Speed: <f2>0.78 Seconds    <f0>Price: <f2>$48,670,000    <f0>Weight: <f2>3.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Black Diamond Atom Splitter: <f0>Attack: <f2>450    <f0>Skill Slashing Req @ <f2>1580 Remort 30    <f0>Speed: <f2>0.67 Seconds    <f0>Price: <f2>$385,923,000    <f0>Weight: <f2>3 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Butter Knife: <f0>Attack: <f2>5    <f0>Skill Piercing Req @ <f2>0    <f0>Speed: <f2>0.78 Seconds    <f0>Price: <f2>$12    <f0>Weight: <f2>3.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Long Knife: <f0>Attack: <f2>13    <f0>Skill Piercing Req @ <f2>45    <f0>Speed: <f2>0.78 Seconds    <f0>Price: <f2>$440    <f0>Weight: <f2>3.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Iron Spear: <f0>Attack: <f2>34    <f0>Skill Piercing Req @ <f2>90    <f0>Speed: <f2>1.34 Seconds    <f0>Price: <f2>$2,100    <f0>Weight: <f2>4.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Steel Spear: <f0>Attack: <f2>48    <f0>Skill Piercing Req @ <f2>140    <f0>Speed: <f2>1.49 Seconds    <f0>Price: <f2>$5,049    <f0>Weight: <f2>5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Steel Pike: <f0>Attack: <f2>65    <f0>Skill Piercing Req @ <f2>200    <f0>Speed: <f2>1.64 Seconds    <f0>Price: <f2>$10,993    <f0>Weight: <f2>5.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Golden Pike: <f0>Attack: <f2>83    <f0>Skill Piercing Req @ <f2>260    <f0>Speed: <f2>1.79 Seconds    <f0>Price: <f2>$19,685    <f0>Weight: <f2>6 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Crystal Pike: <f0>Attack: <f2>105    <f0>Skill Piercing Req @ <f2>340    <f0>Speed: <f2>1.94 Seconds    <f0>Price: <f2>$34,939    <f0>Weight: <f2>6.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Crystal Trident: <f0>Attack: <f2>130    <f0>Skill Piercing Req @ <f2>440    <f0>Speed: <f2>2.09 Seconds    <f0>Price: <f2>$58,536    <f0>Weight: <f2>7 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Tempered Crystal Trident: <f0>Attack: <f2>160    <f0>Skill Piercing Req @ <f2>540    <f0>Speed: <f2>2.24 Seconds    <f0>Price: <f2>$97,772    <f0>Weight: <f2>7.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Trident: <f0>Attack: <f2>190    <f0>Skill Piercing Req @ <f2>640    <f0>Speed: <f2>2.39 Seconds    <f0>Price: <f2>$145,430    <f0>Weight: <f2>8 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Death Spear: <f0>Attack: <f2>220    <f0>Skill Piercing Req @ <f2>740    <f0>Speed: <f2>2.39 Seconds    <f0>Price: <f2>$250,167    <f0>Weight: <f2>8 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Legend Spear: <f0>Attack: <f2>260    <f0>Skill Piercing Req @ <f2>840 Remort 1    <f0>Speed: <f2>1.19 Seconds    <f0>Price: <f2>$6,032,280    <f0>Weight: <f2>4 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Black Diamond Dream Spear: <f0>Attack: <f2>350    <f0>Skill Piercing Req @ <f2>1200 Remort 10    <f0>Speed: <f2>1.04 Seconds    <f0>Price: <f2>$29,696,300    <f0>Weight: <f2>3.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Black Diamond Atom Piercer: <f0>Attack: <f2>520    <f0>Skill Piercing Req @ <f2>1580 Remort 30    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$227,275,000    <f0>Weight: <f2>3 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Knife: <f0>Attack: <f2>55    <f0>Skill Piercing Req @ <f2>Remort 5    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$50,000    <f0>Weight: <f2>4 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Hatchet: <f0>Attack: <f2>60    <f0>Skill Slashing Req @ <f2>Remort 5    <f0>Speed: <f2>0.89 Seconds    <f0>Price: <f2>$50,000    <f0>Weight: <f2>5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Pick Axe: <f0>Attack: <f2>3    <f0>Skill Piercing Req @ <f2>0    <f0>Speed: <f2>1.09 Seconds    <f0>Price: <f2>$0    <f0>Weight: <f2>4 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Cracked Stick: <f0>Attack: <f2>13    <f0>Skill Bludgeoning Req @ <f2>0    <f0>Speed: <f2>1.23 Seconds    <f0>Price: <f2>$82    <f0>Weight: <f2>4.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Iron Stick: <f0>Attack: <f2>23    <f0>Skill Bludgeoning Req @ <f2>45    <f0>Speed: <f2>1.37 Seconds    <f0>Price: <f2>$462    <f0>Weight: <f2>5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Iron Mace: <f0>Attack: <f2>36  <f0>Skill Bludgeoning Req @ <f2>90    <f0>Speed: <f2>1.51 Seconds    <f0>Price: <f2>$1,704    <f0>Weight: <f2>5.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Steel Mace: <f0>Attack: <f2>49    <f0>Skill Bludgeoning Req @ <f2>140    <f0>Speed: <f2>1.64 Seconds    <f0>Price: <f2>$3,864    <f0>Weight: <f2>6 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Steel Hammer: <f0>Attack: <f2>62    <f0>Skill Bludgeoning Req @ <f2>200    <f0>Speed: <f2>1.78 Seconds    <f0>Price: <f2>$6,884    <f0>Weight: <f2>6.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Steel War Hammer: <f0>Attack: <f2>77    <f0>Skill Bludgeoning Req @ <f2>260    <f0>Speed: <f2>1.92 Seconds    <f0>Price: <f2>$11,632    <f0>Weight: <f2>7 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Golden War Hammer: <f0>Attack: <f2>95    <f0>Skill Bludgeoning Req @ <f2>340    <f0>Speed: <f2>2.06 Seconds    <f0>Price: <f2>$19,604    <f0>Weight: <f2>7.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Golden Divine Mace: <f0>Attack: <f2>120    <f0>Skill Bludgeoning Req @ <f2>440    <f0>Speed: <f2>2.19 Seconds    <f0>Price: <f2>$36,648    <f0>Weight: <f2>8 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Crystal Divine Mace: <f0>Attack: <f2>150    <f0>Skill Bludgeoning Req @ <f2>540    <f0>Speed: <f2>2.33 Seconds    <f0>Price: <f2>$66,865    <f0>Weight: <f2>8.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Divine Mace: <f0>Attack: <f2>180    <f0>Skill Bludgeoning Req @ <f2>640    <f0>Speed: <f2>2.47 Seconds    <f0>Price: <f2>$106,250    <f0>Weight: <f2>9 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Brain Spiller: <f0>Attack: <f2>210    <f0>Skill Bludgeoning Req @ <f2>740    <f0>Speed: <f2>2.47 Seconds    <f0>Price: <f2>$187,944    <f0>Weight: <f2>9 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Diamond Legend Mace: <f0>Attack: <f2>240    <f0>Skill Bludgeoning Req @ <f2>840 Remort 1    <f0>Speed: <f2>1.09 Seconds    <f0>Price: <f2>$6,189,820    <f0>Weight: <f2>4 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Black Diamond Dream Mace: <f0>Attack: <f2>330    <f0>Skill Bludgeoning Req @ <f2>1200 Remort 10    <f0>Speed: <f2>0.96 Seconds    <f0>Price: <f2>$32957500    <f0>Weight: <f2>3.5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Black Diamond Atom Smasher: <f0>Attack: <f2>490    <f0>Skill Bludgeoning Req @ <f2>1580 Remort 30    <f0>Speed: <f2>0.82 Seconds    <f0>Price: <f2>$251700000    <f0>Weight: <f2>3 Lbs");
}
//****************************************************************************************************
//   AXE TEST
//****************************************************************************************************

ItemImageData crystalspearImage
{
	shapeFile  = "spear";
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
ItemData crystalspear
{
	heading = "bWeapons";
	description = "spear";
	className = "Weapon";
	shapeFile  = "spear";
	hudIcon = "axe";
	shadowDetailMask = 4;
	imageType = crystalspearImage;
	price = 0;
	showWeaponBar = true;
};
function crystalspearImage::onFire(%player, %slot)
{
	MeleeAttack(%player, GetRange(crystalspear), crystalspear);
}

function crystalspear::onMount(%player,%item,$WeaponSlot) 
{   %client = Player::getclient(%player); 
   KronosWeaponInfo(%client, "<f1>Crystal Spear: <f0>Attack: <f2>60    <f0>Skill Slashing Req @ <f2>0    <f0>Speed: <f2>1.0 Seconds    <f0>Price: <f2>$0    <f0>Weight: <f2>5 Lbs");
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
   KronosWeaponInfo(%client, "<f1>Club: <f0>Attack: <f2>55    <f0>Skill Bludgeoning Req @ <f2>0    <f0>Speed: <f2>1.09 Seconds    <f0>Price: <f2>$50,000    <f0>Weight: <f2>4 Lbs");
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
	// Initialize variables at the start to prevent unassigned warnings
	%hasCast = false;
	%index = -1;
	%length = 0;
	
	%clientId = Player::getClient(%player);
	if(%clientId == "")
		%clientId = 0;
	
	// For enemy bots, Player::getClient() returns -1, so we need to do a reverse lookup
	if(%clientId == -1 || %clientId == 0)
	{
		%clientId = GetClientIdFromPlayerObject(%player);
		
		// If still no client ID, try using the player object directly
		if(%clientId == -1 || %clientId == "")
			%clientId = %player;
	}

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

	// Only calculate length if we have a valid spell index
	if(%index != -1 && $Spell::LOSrange[%index] != "")
		%length = $Spell::LOSrange[%index] - 1;
	else
		%length = 0;
		
	$los::object = "";
	%losResult = GameBase::getLOSinfo(%player, %length);
	
	if(%losResult && %index != -1)
	{
		%obj = getObjectType($los::object);
		
		if(%obj == "Player")
		{
			if(Player::isAiControlled(%clientId))
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				// Fallback to Client::getName if BotInfoAiName is invalid
				if(%botName == "" || %botName == -1 || %botName == "0")
					%botName = Client::getName(%clientId);
				
				if(%botName != "" && %botName != -1 && %botName != "0")
					AI::newDirectiveRemove(%botName, 99);
			}
			
			%spellKeyword = $Spell::keyword[%index];
			%castCommand = "#cast " @ %spellKeyword;
			remoteSay(%clientId, 0, %castCommand);
			%hasCast = True;
		}
	}
	
	if(!%hasCast)
	{
		if(OddsAre(3))
			MeleeAttack(%player, GetRange(Hatchet), CastingBlade);	//mimic the hatchet range
	}
}

