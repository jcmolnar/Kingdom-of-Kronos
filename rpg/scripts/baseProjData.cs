$ImpactDamageType		  = -1;
$LandingDamageType	  =  0;
$BulletDamageType      =  1;
$EnergyDamageType      =  2;
$PlasmaDamageType      =  3;
$ExplosionDamageType   =  4;
$ShrapnelDamageType    =  5;
$LaserDamageType       =  6;
$MortarDamageType      =  7;
$BlasterDamageType     =  8;
$ElectricityDamageType =  9;
$CrushDamageType       = 10;
$DebrisDamageType      = 11;
$MissileDamageType     = 12;
$MineDamageType        = 13;
$NullDamageType        = 14;
$SpellDamageType        = 15;

//--------------------------------------
BulletData FusionBolt
{
   bulletShapeName    = "fusionbolt.dts";
   explosionTag       = turretExp;
   mass               = 0.05;

   damageClass        = 0;       // 0 impact, 1, radius
   damageValue        = 1;
   damageType         = $MissileDamageType;

   muzzleVelocity     = 80.0;
   totalTime          = 8.0;
   liveTime           = 6.0;
   isVisible          = True;

   rotationPeriod = 1.5;
};
//---------------------------------------
BulletData Dforsnipe
{
   bulletShapeName    = "fusionbolt.dts";
   explosionTag       = turretExp;
   mass               = 0.05;

   damageClass        = 0;       // 0 impact, 1, radius
   damageValue        = 50;	//this will change damage of snipe//damage = damagevalue * 100
   damageType         = $MissileDamageType;

   muzzleVelocity     = 80.0;
   totalTime          = 8.0;
   liveTime           = 6.0;
   isVisible          = True;

   rotationPeriod = 1.5;
};

//--------------------------------------
BulletData MiniFusionBolt
{
   bulletShapeName    = "enbolt.dts";
   explosionTag       = energyExp;

   damageClass        = 0;
   damageValue        = 0.75;
   damageType         = $MissileDamageType;

   muzzleVelocity     = 80.0;
   totalTime          = 4.0;
   liveTime           = 2.0;

   lightRange         = 3.0;
   lightColor         = { 0.25, 0.25, 1.0 };
   //inheritedVelocityScale = 0.5;
   isVisible          = True;

   rotationPeriod = 1;
};
function MiniFusionBolt::onAdd(%this)
{
}

//--------------------------------------
BulletData TempleFusionBolt
{
   bulletShapeName    = "enbolt.dts";
   explosionTag       = energyExp;

   damageClass        = 0;
   damageValue        = 2;
   damageType         = $MissileDamageType;

   muzzleVelocity     = 100.0;
   totalTime          = 4.0;
   liveTime           = 2.0;

   lightRange         = 3.0;
   lightColor         = { 0.25, 0.25, 1.0 };
   //inheritedVelocityScale = 0.5;
   isVisible          = True;

   rotationPeriod = 1;
};

//--------------------------------------
RocketData FlierRocket
{
   bulletShapeName  = "rocket.dts";
   explosionTag     = rocketExp;
   collisionRadius  = 0.0;
   mass             = 2.0;

   damageClass      = 1;       // 0 impact, 1, radius
   damageValue      = 1.0;
   damageType       = $MissileDamageType;

   explosionRadius  = 9.5;
   kickBackStrength = 100.0;
   muzzleVelocity   = 120.0;
   terminalVelocity = 150.0;
   acceleration     = 10.0;
   totalTime        = 10.0;
   liveTime         = 11.0;
   lightRange       = 5.0;
   lightColor       = { 1.0, 0.7, 0.5 };
   //inheritedVelocityScale = 0.1;

   // rocket specific
   trailType   = 2;                // smoke trail
   trailString = "rsmoke.dts";
   smokeDist   = 1.8;

   soundId = SoundJetHeavy;
};

//-------------------------------------- 
RocketData discammo
{ 
   bulletShapeName = "discb.dts";
   explosionTag    = rocketExp;

   collisionRadius = 0.0;
   mass            = 2.0;

   damageClass      = 1;       // 0 impact, 1, radius
   damageValue      = 0.06;
   damageType       = $MissileDamageType;  //$SpellDamageType; 

   explosionRadius  = 7.5;
   kickBackStrength = 15.0;

   muzzleVelocity   = 110.0;
   terminalVelocity = 150.0;
   acceleration     = 20.0;

   totalTime        = 6.5;
   liveTime         = 8.0;

   lightRange       = 5.0;
   lightColor       = { 0.4, 0.4, 1.0 };

   inheritedVelocityScale = 0.5;

   // rocket specific
   trailType   = 1;
   trailLength = 15;
   trailWidth  = 0.3;

   soundId = SoundDiscSpin;
};
//-------------------------------------------
LaserData heatLaser
{
	laserBitmapName   = "laserpulse.bmp";
	hitName           = "laserhit.dts";

	damageConversion  = 0.0;
	baseDamageType    = $LaserDamageType;

 	beamTime          = 6.5;

	lightRange        = 10.0;
	lightColor        = { 0.2, 0.2, 1.0 };

	detachFromShooter = false;
	hitSoundId        = explosion4;
};

//--------------------------------------
BulletData GuardianBolt
{
   bulletShapeName    = "fusionbolt.dts";
   explosionTag       = turretExp;
   mass               = 0.05;

   damageClass        = 0;       // 0 impact, 1, radius
   damageValue        = 3;
   damageType         = $MissileDamageType;

   muzzleVelocity     = 120.0;
   totalTime          = 8.0;
   liveTime           = 6.0;
   isVisible          = True;

   rotationPeriod = 1.5;
};

LaserData sniperLaser
{
	laserBitmapName   = "forcefield.bmp";
	hitName           = "laserhit.dts";
	damageConversion  = 0.0;
	baseDamageType    = $LaserDamageType;

	beamTime          = 1.5;

	lightRange        = 10.0;
	lightColor        = { 0.2, 0.2, 1.0 };

	detachFromShooter = false;
	hitSoundId        = NoSound;
};

function SeekingMissile::updateTargetPercentage(%target)
{
	dbecho($dbechoMode, "SeekingMissile::updateTargetPercentage(" @ %target @ ")");

	return GameBase::virtual(%target, "getHeatFactor");
}

//LightningData turretCharge
//{
//   bitmapName       = "lightningNew.bmp";

//   damageType       = $ElectricityDamageType;
//   boltLength       = 40.0;
//   coneAngle        = 35.0;
//   damagePerSec      = 0.06;
//   energyDrainPerSec = 60.0;
//   segmentDivisions = 4;
//   numSegments      = 5;
//   beamWidth        = 0.125;

//   updateTime   = 120;
//   skipPercent  = 0.5;
//   displaceBias = 0.15;

//   lightRange = 3.0;
//   lightColor = { 0.25, 0.25, 0.85 };

//   soundId = SoundELFFire;
//};

function Lightning::damageTarget(%target, %timeSlice, %damPerSec, %enDrainPerSec, %pos, %vec, %mom, %shooterId)
{
	dbecho($dbechoMode, "Lightning::damageTarget(" @ %target @ ", " @ %timeSlice @ ", " @ %damPerSec @ ", " @ %enDrainPerSec @ ", " @ %pos @ ", " @ %vec @ ", " @ %mom @ ", " @ %shooterId @ ")");

   %damVal = %timeSlice * %damPerSec;
   %enVal  = %timeSlice * %enDrainPerSec;

   GameBase::applyDamage(%target, $ElectricityDamageType, %damVal, %pos, %vec, %mom, %shooterId);

   %energy = GameBase::getEnergy(%target);
   %energy = %energy - %enVal;
   if (%energy < 0) {
      %energy = 0;
   }
   GameBase::setEnergy(%target, %energy);
}

//--------------------------------------
// Spell Explosion RocketData Projectiles
// These replace MineData for better networking performance
// All have 0.1 second lifetime for instant explosion
//--------------------------------------

RocketData SpellBomb1
{
	bulletShapeName = "";
	explosionTag = mortarExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb2
{
	bulletShapeName = "";
	explosionTag = mineExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb3
{
	bulletShapeName = "";
	explosionTag = grenadeExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb4
{
	bulletShapeName = "";
	explosionTag = Shockwave;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb5
{
	bulletShapeName = "";
	explosionTag = LargeShockwave;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb6
{
	bulletShapeName = "";
	explosionTag = rocketExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb7
{
	bulletShapeName = "";
	explosionTag = energyExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb8
{
	bulletShapeName = "";
	explosionTag = blasterExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb9
{
	bulletShapeName = "";
	explosionTag = plasmaExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb10
{
	bulletShapeName = "";
	explosionTag = turretExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb11
{
	bulletShapeName = "";
	explosionTag = bulletExp0;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb12
{
	bulletShapeName = "";
	explosionTag = debrisExpSmall;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb13
{
	bulletShapeName = "";
	explosionTag = debrisExpMedium;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb14
{
	bulletShapeName = "";
	explosionTag = debrisExpLarge;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb15
{
	bulletShapeName = "";
	explosionTag = flashExpSmall;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb16
{
	bulletShapeName = "";
	explosionTag = flashExpMedium;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb17
{
	bulletShapeName = "";
	explosionTag = flashExpLarge;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb18
{
	bulletShapeName = "";
	explosionTag = whiteExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb19
{
	bulletShapeName = "";
	explosionTag = WhiteShockwave;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb20
{
	bulletShapeName = "";
	explosionTag = IonExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb21
{
	bulletShapeName = "";
	explosionTag = IonExp2;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb22
{
	bulletShapeName = "";
	explosionTag = IonShockwave;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb23
{
	bulletShapeName = "";
	explosionTag = IonShockwave2;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb24
{
	bulletShapeName = "";
	explosionTag = PBShockWave;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb25
{
	bulletShapeName = "";
	explosionTag = LitBoltExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb26
{
	bulletShapeName = "";
	explosionTag = TimeShockwave;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb27
{
	bulletShapeName = "";
	explosionTag = turretSlowExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb28
{
	bulletShapeName = "";
	explosionTag = acidExp;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};

RocketData SpellBomb29
{
	bulletShapeName = "";
	explosionTag = bulletExp0;
	collisionRadius = 0.5;
	mass = 0.1;
	damageClass = 1;
	damageValue = 1.0;
	damageType = $NullDamageType;
	explosionRadius = 10.0;
	kickBackStrength = 0;
	muzzleVelocity = 1.0;
	totalTime = 0.2;
	liveTime = 0.2;
	isVisible = False;
};