function Item::Event(%player, %item, %event) {
	if(%event == "Equip") {
		if(%item == DragonShield) {
			Player::MountItem(%player,DragonShield2,99);
		}
	}
	if(%event == "UnEquip") {
		if(%item == DragonShield) {
			Player::UnMountItem(%player,99);
		}
	}
	//add more events here.
}

//****************************************************************************************************
//   DevilsClaw
//****************************************************************************************************

$WeaponRange[DevilsClaw] = 5.5;
$WeaponDelay[DevilsClaw] = 1.1;
$AccessoryVar[DevilsClaw, $SpecialVar] = "6 220";
$AccessoryVar[DevilsClaw, $AccessoryType] = $SwordAccessoryType;
$AccessoryVar[DevilsClaw, $Weight] = "0.1";
$AccessoryVar[DevilsClaw, $MiscInfo] = "The DevilsClaw.";
$SkillType[DevilsClaw] = $SkillPiercing;
$ItemCost[DevilsClaw] = 15;

ItemImageData DevilsClawImage
{
	shapeFile  = "katana";
	mountPoint = 0;
	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = $WeaponDelay[DevilsClaw];
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;

	sfxFire = SoundSwing3;
	sfxActivate = AxeSlash2;
};
ItemData DevilsClaw
{
	heading = "bWeapons";
	description = "Devils Claw";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = DevilsClawImage;
	price = 0;
	showWeaponBar = true;
};
ItemImageData DevilsClawImage2
{
	shapeFile  = "katana";
	mountPoint = 2;
	mountRotation = { 0, -2, 0 };
	mountOffSet = { -0.15, 0.2, 0 };
	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = $WeaponDelay[DevilsClaw];
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;
};
ItemData DevilsClaw2
{
	heading = "bWeapons";
	description = "Devils Claw";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = DevilsClawImage2;
	price = 0;
	showWeaponBar = true;
};

ItemImageData DevilsClawImage3
{
	shapeFile  = "katana";
	mountPoint = 2;
	mountRotation = { 0, 3, 0 };
	mountOffSet = { -0.14, 0.2, -0.08 };
	weaponType = 0; // Single Shot
	reloadTime = 0;
	fireTime = $WeaponDelay[DevilsClaw];
	minEnergy = 0;
	maxEnergy = 0;

	accuFire = true;
};
ItemData DevilsClaw3
{
	heading = "bWeapons";
	description = "Devils Claw";
	className = "Weapon";
	shapeFile  = "katana";
	hudIcon = "katana";
	shadowDetailMask = 4;
	imageType = DevilsClawImage3;
	price = 0;
	showWeaponBar = true;
};

function DevilsClaw::onMount(%player, %imageSlot)
{
	%client = GameBase::getOwnerClient(%player);
	Player::unmountItem(%player,4);
	Player::unmountItem(%player,5);
	Player::UnmountItem(%player,6);
}

function DevilsClaw::onUnmount(%player,%imageSlot)
{
    Player::mountItem(%player,DevilsClaw2,4);
    Player::mountItem(%player,DevilsClaw3,5);
}

function DevilsClawImage::onFire(%player, %slot)
{
	MeleeAttack(%player, $WeaponRange[DevilsClaw], DevilsClaw);
}

//===============================================
//============--------Harpoon--------============
//===============================================

SoundData SoundHarpFire
{
   wavFileName = "arrowhit.wav";
   profile = Profile3dMedium;
};

$AccessoryVar[Harpoon, $SpecialVar] = "6 100";
$AccessoryVar[Harpoon, $AccessoryType] = $BludgeonAccessoryType;
$AccessoryVar[Harpoon, $Weight] = "30.0";
$AccessoryVar[Harpoon, $MiscInfo] = "Use to kill stuff in water!!!";
$WeaponRange[Harpoon] = 1000.0;
$WeaponDelay[Harpoon] = 1.3;
$SkillType[Harpoon] = $SkillArchery;

$SkillRestriction[harpoon] = $SkillArchery @ " 500 " @ $MinRemort @ " 6";

ItemImageData HarpoonImage
{
	shapeFile  = "crossbow";
	mountPoint = 0;
	weaponType = 0;
	reloadTime = 0;
	showInventory = false;
	fireTime = $WeaponDelay[Harpoon];
	accuFire = false;
	sfxFire = SoundHarpFire;
	sfxActivate = CrossbowSwitch1;
};
ItemData Harpoon
{
	heading = "bWeapons";
	description = "Harpoon";
	className = "Weapon";
	shapeFile  = "Crossbow";
	hudIcon = "trident";
	shadowDetailMask = 4;
	imageType = HarpoonImage;
	price = 0;
	showWeaponBar = true;
};
function HarpoonImage::onFire(%player, %slot) 
{
	if(fetchData(%clientid, "MANA") > 20)
	{
	GameBase::getLOSinfo(%player, 40);
	setMANA(%clientid, fetchData(%clientid, "MANA") - 20);
	%client = GameBase::getOwnerClient(%player);
	%trans = GameBase::getMuzzleTransform(%player);
	%vel = Item::getVelocity(%player);

	Projectile::spawnProjectile("HarpoonShot",%trans,%player,%vel);
	}
}

RocketData HarpoonShot
{ 
	bulletShapeName = "spear.dts"; 
	explosionTag = bulletExp0; 
	collisionRadius = 0.0; 
	mass = 2.0;
	damageClass = 1;
	damageValue = 3.5; 
	baseDamageType = $SpellDamageType;
	explosionRadius = 2.0;
	kickBackStrength = 0.0;
	muzzleVelocity   = 40.0;
	terminalVelocity = 40.0;
	acceleration = 2.0;
	totalTime = 2.0;
	liveTime = 1.6;
	lightRange = 20.0;
	colors[0] = { 10.0, 0.75, 0.75 };
	colors[1] = { 1.0, 0.25, 10.25 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "MortarTrail.dts";
	smokeDist   = 3;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.1;
};

RocketData HarpoonBlank
{ 
	bulletShapeName = "spear.dts"; 
	explosionTag = bulletExp0; 
	collisionRadius = 0.0; 
	mass = 2.0;
	damageClass = 0;
	damageValue = 0; 
	baseDamageType = $SpellDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity   = 40.0;
	terminalVelocity = 40.0;
	acceleration = 2.0;
	totalTime = 2.0;
	liveTime = 1.6;
	lightRange = 20.0;
	colors[0] = { 10.0, 0.75, 0.75 };
	colors[1] = { 1.0, 0.25, 10.25 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "MortarTrail.dts";
	smokeDist   = 3;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.1;
};


//----------------------------------------------------------- 
// StormCharger
//----------------------------------------------------------- 
$AccessoryVar[StormCharger, $SpecialVar] = "11 500";
$AccessoryVar[StormCharger, $Weight] = "30";
$SkillType[StormCharger] = $SkillSenseHeading;
$AccessoryVar[StormCharger, $MiscInfo] = "<f2>COMET<f0> - Pain, Pain, Pain, Pain... ... ...";
$ItemCost[StormCharger] = 0;
$SkillRestriction[StormCharger] = $MinAdmin @ " 3";
$WeaponRange[StormCharger] = 1000;
$WeaponDelay[StormCharger] = 1;

ItemImageData StormChargerImage 
{ 
	shapeFile = "MortarGun"; 
	mountPoint = 0; 
	mountoffset = "0"; 
	weaponType = 0;		
	reloadTime = 0; 
	showInventory = true; 
	fireTime = $WeaponDelay[StormCharger]; 
	minEnergy = 0; 
	maxEnergy = 0; 
	accuFire = false; 
	sfxFire = SoundFireMortar; 
	sfxActivate = SoundPickUpWeapon; 
}; 
ItemData StormCharger
{ 
	heading = "bWeapons"; 
	description = "Storm Charger"; 
	className = "Weapon"; 
	shapeFile = "MortarGun"; 
	showInventory = true; 
	hudIcon = "MortarGun"; 
	shadowDetailMask = 4; 
	imageType = StormChargerImage; 
	price = 0; 
	showWeaponBar = true; 
}; 
function StormChargerImage::onFire(%player, %slot) 
{ 
	%client = GameBase::getOwnerClient(%player); 
	%trans = GameBase::getMuzzleTransform(%player); 
	%vel = Item::getVelocity(%player); 

	Projectile::spawnProjectile("Comet",%trans,%player,%vel); 
	Projectile::spawnProjectile("Comet2",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank",%trans,%player,%vel);
	Projectile::spawnProjectile("CometBlank2",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank3",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank4",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank5",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank6",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank7",%trans,%player,%vel);
	Projectile::spawnProjectile("CometBlank8",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank9",%trans,%player,%vel); 
	Projectile::spawnProjectile("CometBlank10",%trans,%player,%vel); 
	Projectile::spawnProjectile("FireFlames",%trans,%player,%vel);
	Projectile::spawnProjectile("FireFlames2",%trans,%player,%vel);
	Projectile::spawnProjectile("FireFlamesBlank",%trans,%player,%vel);
	Projectile::spawnProjectile("FireFlamesBlank2",%trans,%player,%vel);
} 
//====================== 
ExplosionData CometExp
{
	shapeName = "fusionex.dts";
	soundId   = turretExplosion;

	faceCamera = true;
	randomSpin = truee;
	faceCamera = true;
	randomSpin = true;
	hasLight   = true;
	lightRange = 80.0;

	timeScale = 0.5;

	timeZero = 0.0;
	timeOne  = 0.500;

	colors[0]  = { 0.0, 0.0, 0.0 };
	colors[1]  = { 3.0, 3.0, 3.0 };
	colors[2]  = { 3.0, 3.0, 3.0 };
	radFactors = { 0.0, 3.0, 3.0 };
};

ExplosionData FireExp
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = false;
	randomSpin = false;
	hasLight = true;
	lightRange = 25.0;

	timeZero = 0.200;
	timeOne = 0.950;

	colors[0]  = { 0.0, 0.0, 1.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

ExplosionData FireExp2
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = true;
	randomSpin = true;
	hasLight = true;
	lightRange = 25.0;

	timeZero = 0.200;
	timeOne = 0.950;

	colors[0]  = { 1.0, 0.0, 0.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

ExplosionData FireExp3
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = false;
	randomSpin = true;
	hasLight = true;
	lightRange = 25.0;
	timescale = 2.45;
	timeZero = 0.250;
	timeOne = 1.550;

	colors[0]  = { 0.0, 1.0, 0.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

ExplosionData FireExp4
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = true;
	randomSpin = true;
	hasLight = true;
	lightRange = 25.0;
	timescale = 2.45;
	timeZero = 0.250;
	timeOne = 1.550;

	colors[0]  = { 1.0, 0.0, 1.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

RocketData FireFlames
{
	bulletShapeName = "plasmabolt.dts";
	explosionTag = FireExp;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 50.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 15.0;
	lightColor = { 1.0, 0, 0 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "Shockwave.DTS";
	smokeDist = 8.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FireFlames2
{
	bulletShapeName = "fusionbolt.dts";
	explosionTag = FireExp2;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 50.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0;
	lightColor = { 0, 1.0, 0 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "PlasmaEX.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FireFlamesBlank
{
	bulletShapeName = "";
	explosionTag = FireExp3;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.0;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 0.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0;
	lightColor = { 0, 0, 1 };
	inheritedVelocityScale = 0.5;
	trailType = 0;
	trailString = "Shockwave.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FireFlamesBlank2
{
	bulletShapeName = "";
	explosionTag = FireExp4;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 1.0;
	lightColor = { 1.0, 1.0, 9.5 };
	inheritedVelocityScale = 0.5;
	trailType = 0;
	trailString = "Shockwave.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData Comet
{
	bulletShapeName = "fusionbolt.dts"; 
	explosionTag = CometExp; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "Shockwave.DTS"; 
	smokeDist   = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData Comet2
{
	bulletShapeName = "Shockwave.DTS"; 
	explosionTag = CometExp; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.0;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "Shockwave.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank
{
	bulletShapeName = ""; 
	explosionTag = GrenadeExp; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.0;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "Shockwave.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank2
{
	bulletShapeName = ""; 
	explosionTag = MineExp; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.0;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "Shockwave.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank3
{
	bulletShapeName = ""; 
	explosionTag = GrenadeExp; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "MortarTrail.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank4
{
	bulletShapeName = ""; 
	explosionTag = mortarExp; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "MortarTrail.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank5
{
	bulletShapeName = ""; 
	explosionTag = turretExp; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "MortarTrail.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank6
{
	bulletShapeName = ""; 
	explosionTag = bulletExp0; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "MortarTrail.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank7
{
	bulletShapeName = ""; 
	explosionTag = bulletExp1; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "MortarTrail.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank8
{
	bulletShapeName = ""; 
	explosionTag = bulletExp2; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "MortarTrail.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank9
{
	bulletShapeName = ""; 
	explosionTag = FlashExpSmall; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "Shockwave_Large.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

RocketData CometBlank10
{
	bulletShapeName = ""; 
	explosionTag = FlashExpMedium; 
	collisionRadius = 0.0;
	mass = 2.0; 
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType; 
	explosionRadius = 15.0; 
	kickBackStrength = 0.0; 
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 10.0; 
	colors[0] = { 3.0, 0.75, 0.75 };
	colors[1] = { 3.0, 0.25, 0.25 }; 
	inheritedVelocityScale = 0.5; 
	trailType = 2; 
	trailString = "Shockwave_Large.DTS"; 
	smokeDist = 1.8;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.5;
};

//----------------------------------------------------------- 
// PhantomWind
//----------------------------------------------------------- 
$AccessoryVar[PhantomWind, $SpecialVar] = "11 500";
$AccessoryVar[PhantomWind, $Weight] = "30";
$SkillType[PhantomWind] = $SkillSenseHeading;
$AccessoryVar[PhantomWind, $MiscInfo] = "<f2>COMET<f0> - Pain, Pain, Pain, Pain... ... ...";
$ItemCost[PhantomWind] = 0;
$SkillRestriction[PhantomWind] = $MinAdmin @ " 3";
$WeaponRange[PhantomWind] = 1000;
$WeaponDelay[PhantomWind] = 1;

ItemImageData PhantomWindImage 
{ 
	shapeFile = "MortarGun"; 
	mountPoint = 0; 
	mountoffset = "0"; 
	weaponType = 0;		
	reloadTime = 0; 
	showInventory = true; 
	fireTime = $WeaponDelay[PhantomWind]; 
	minEnergy = 0; 
	maxEnergy = 0; 
	accuFire = false; 
	sfxFire = SoundFireMortar; 
	sfxActivate = SoundPickUpWeapon; 
}; 
ItemData PhantomWind
{ 
	heading = "bWeapons"; 
	description = "Phantom Wind"; 
	className = "Weapon"; 
	shapeFile = "MortarGun"; 
	showInventory = true; 
	hudIcon = "MortarGun"; 
	shadowDetailMask = 4; 
	imageType = PhantomWindImage; 
	price = 0; 
	showWeaponBar = true; 
}; 
function PhantomWindImage::onFire(%player, %slot) 
{ 
	%client = GameBase::getOwnerClient(%player); 
	%trans = GameBase::getMuzzleTransform(%player); 
	%vel = Item::getVelocity(%player); 

	Projectile::spawnProjectile("FAProj",%trans,%player,%vel); 
	Projectile::spawnProjectile("FAProj2",%trans,%player,%vel); 
	Projectile::spawnProjectile("FABlank",%trans,%player,%vel);
	Projectile::spawnProjectile("FABlank2",%trans,%player,%vel);
	Projectile::spawnProjectile("FABlank3",%trans,%player,%vel);
}

//====================== 
ExplosionData FAExp
{
	shapeName = "fusionex.dts";
	soundId   = turretExplosion;

	faceCamera = true;
	randomSpin = true;
	faceCamera = true;
	randomSpin = true;
	hasLight   = true;
	lightRange = 80.0;

	timeScale = 0.5;

	timeZero = 0.0;
	timeOne  = 0.500;

	colors[0]  = { 0.0, 0.0, 0.0 };
	colors[1]  = { 3.0, 3.0, 3.0 };
	colors[2]  = { 3.0, 3.0, 3.0 };
	radFactors = { 0.0, 3.0, 3.0 };
};

ExplosionData FAExp2
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = false;
	randomSpin = false;
	hasLight = true;
	lightRange = 25.0;

	timeZero = 0.200;
	timeOne = 0.950;

	colors[0]  = { 0.0, 0.0, 1.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

ExplosionData FAExp3
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = true;
	randomSpin = true;
	hasLight = true;
	lightRange = 25.0;

	timeZero = 0.200;
	timeOne = 0.950;

	colors[0]  = { 1.0, 0.0, 0.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

ExplosionData FAExp4
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = false;
	randomSpin = true;
	hasLight = true;
	lightRange = 25.0;
	timescale = 2.45;
	timeZero = 0.250;
	timeOne = 1.550;

	colors[0]  = { 0.0, 1.0, 0.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

ExplosionData FAExp5
{
	shapeName = "Shockwave_Large.DTS";
	soundId   = shockExplosion;

	faceCamera = true;
	randomSpin = true;
	hasLight = true;
	lightRange = 25.0;
	timescale = 2.45;
	timeZero = 0.250;
	timeOne = 1.550;

	colors[0]  = { 1.0, 0.0, 1.0 };
	colors[1]  = { 5.0, 4.0, 5.0 };
	colors[2]  = { 0.1, 0.0, 10.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

ExplosionData FAExp6
{
	shapeName = "fusionex.dts";
	soundId   = turretExplosion;

	faceCamera = true;
	randomSpin = true;
	faceCamera = true;
	randomSpin = true;
	hasLight   = true;
	lightRange = 80.0;

	timeScale = 5.5;

	timeZero = 5.0;
	timeOne  = 5.500;

	colors[0]  = { 0.0, 0.0, 0.0 };
	colors[1]  = { 3.0, 3.0, 3.0 };
	colors[2]  = { 3.0, 3.0, 3.0 };
	radFactors = { 0.0, 3.0, 3.0 };
};

RocketData FAProj
{
	bulletShapeName = "plasmabolt.dts";
	explosionTag = FAExp;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 50.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 15.0;
	lightColor = { 1.0, 0, 0 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "plasmaex.dts";
	smokeDist = 8.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FAProj2
{
	bulletShapeName = "fusionbolt.dts";
	explosionTag = FAExp2;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 50.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 20.0;
	lightColor = { 0, 1.0, 0 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "Shockwave_Large.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FABlank
{
	bulletShapeName = "";
	explosionTag = FireExp3;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 50.0;
	lightColor = { 0, 0, 1 };
	inheritedVelocityScale = 0.5;
	trailType = 0;
	trailString = "Shockwave_Large.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FABlank2
{
	bulletShapeName = "";
	explosionTag = FireExp4;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 1.0;
	lightColor = { 1.0, 1.0, 9.5 };
	inheritedVelocityScale = 0.5;
	trailType = 0;
	trailString = "Shockwave_Large.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FABlank3
{
	bulletShapeName = "";
	explosionTag = FAExp5;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 100.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 48.0;
	acceleration     = 10.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 1.0;
	lightColor = { 1.0, 1.0, 9.5 };
	inheritedVelocityScale = 0.5;
	trailType = 0;
	trailString = "Shockwave_Large.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

//====================== 
ExplosionData FBExp
{
	shapeName = "fusionex.dts";
	soundId   = turretExplosion;

	faceCamera = true;
	randomSpin = true;
	faceCamera = true;
	randomSpin = true;
	hasLight   = true;
	lightRange = 25.0;

	timeScale = 0.5;

	timeZero = 0.0;
	timeOne  = 0.500;

	colors[0]  = { 0.0, 0.0, 0.0 };
	colors[1]  = { 3.0, 3.0, 3.0 };
	colors[2]  = { 3.0, 3.0, 3.0 };
	radFactors = { 0.0, 3.0, 3.0 };
};

ExplosionData FBExp2
{
	shapeName = "fusionbolt.DTS";
	soundId   = shockExplosion;

	faceCamera = false;
	randomSpin = false;
	hasLight = true;
	lightRange = 25.0;

	timeZero = 0.200;
	timeOne = 0.950;

	colors[0]  = { 0.0, 0.0, 1.0 };
	colors[1]  = { 0.0, 0.0, 0.0 };
	colors[2]  = { 0.1, 0.0, 5.0 };
	radFactors = { 0.1, 0.1, 3.0 };
};

RocketData FBProj
{
	bulletShapeName = "plasmabolt.dts";
	explosionTag = FBExp;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 5.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 80.0;
	acceleration     = 20.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 3.0;
	lightColor = { 1.0, 0, 0 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "MortarTrail.DTS";
	smokeDist = 8.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData FBProj2
{
	bulletShapeName = "fusionbolt.dts";
	explosionTag = FBExp2;
	collisionRadius = 0.0;
	mass = 2.0;
	damageClass = 1;
	damageValue = 1.80;
	baseDamageType = $SpellDamageType;
	explosionRadius = 15.0;
	kickBackStrength = 5.5;
	muzzleVelocity = 50.0;
	terminalVelocity = 80.0;
	acceleration     = 19.0;
	totalTime = 3.0; 
	liveTime = 3.0; 
	lightRange = 5.0;
	lightColor = { 0, 1.0, 0 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "fusionbolt.DTS";
	smokeDist = 7.0;
	soundId = Explode3FW;
	rotationPeriod = 0.5;
};

RocketData TitansCannonShot
{ 
	bulletShapeName = "Rocket.dts"; 
	explosionTag = RocketEXP; 
	collisionRadius = 0.0; 
	mass = 2.0;
	damageClass = 1;
	damageValue = 3.5; 
	baseDamageType = $SpellDamageType;
	explosionRadius = 2.0;
	kickBackStrength = 0.0;
	muzzleVelocity   = 40.0;
	terminalVelocity = 40.0;
	acceleration = 2.0;
	totalTime = 2.0;
	liveTime = 1.6;
	lightRange = 20.0;
	colors[0] = { 10.0, 0.75, 0.75 };
	colors[1] = { 1.0, 0.25, 10.25 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "plasmatrail.dts";
	smokeDist = 4;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.1;
};

RocketData TitansCannonBlank
{ 
	bulletShapeName = "Rocket.dts"; 
	explosionTag = RocketEXP; 
	collisionRadius = 0.0; 
	mass = 2.0;
	damageClass = 1;
	damageValue = 3.5; 
	baseDamageType = $SpellDamageType;
	explosionRadius = 2.0;
	kickBackStrength = 0.0;
	muzzleVelocity   = 40.0;
	terminalVelocity = 40.0;
	acceleration = 2.0;
	totalTime = 2.0;
	liveTime = 1.6;
	lightRange = 20.0;
	colors[0] = { 10.0, 0.75, 0.75 };
	colors[1] = { 1.0, 0.25, 10.25 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "plasmatrail.dts";
	smokeDist = 4;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.1;
};

RocketData TitansCannonBlank1
{ 
	bulletShapeName = "Fusionbolt.DTS"; 
	explosionTag = RocketEXP; 
	collisionRadius = 0.0; 
	mass = 2.0;
	damageClass = 1;
	damageValue = 3.5; 
	baseDamageType = $SpellDamageType;
	explosionRadius = 2.0;
	kickBackStrength = 0.0;
	muzzleVelocity   = 40.0;
	terminalVelocity = 40.0;
	acceleration = 2.0;
	totalTime = 2.0;
	liveTime = 1.6;
	lightRange = 20.0;
	colors[0] = { 10.0, 0.75, 0.75 };
	colors[1] = { 1.0, 0.25, 10.25 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "plasmatrail.dts";
	smokeDist = 4;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.1;
};

RocketData TitansCannonBlank2
{ 
	bulletShapeName = "PlasmaEX.DTS"; 
	explosionTag = RocketEXP; 
	collisionRadius = 0.0; 
	mass = 2.0;
	damageClass = 1;
	damageValue = 3.5; 
	baseDamageType = $SpellDamageType;
	explosionRadius = 2.0;
	kickBackStrength = 0.0;
	muzzleVelocity   = 40.0;
	terminalVelocity = 40.0;
	acceleration = 2.0;
	totalTime = 2.0;
	liveTime = 1.6;
	lightRange = 20.0;
	colors[0] = { 10.0, 0.75, 0.75 };
	colors[1] = { 1.0, 0.25, 10.25 };
	inheritedVelocityScale = 0.5;
	trailType = 2;
	trailString = "plasmatrail.dts";
	smokeDist = 4;
	soundId = SoundJetHeavy;
	rotationPeriod = 0.1;
};
