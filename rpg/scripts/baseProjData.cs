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
// These replace MineData for better networking performance.
// Pattern (from known-working RocketExpFX): zero collision radius and a 0.01s
// lifetime so the projectile never collides - it expires via totalTime, which
// makes the server broadcast a timeout explosion at the spawn position.
// Spawning with collisionRadius > 0 at ground level collided with terrain,
// and static-geometry hits skip the server explosion ("trust the client"),
// so no explosion was ever shown.
//--------------------------------------

RocketData SpellBomb1
{
	bulletShapeName = "breath.dts";
	explosionTag = mortarExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb2
{
	bulletShapeName = "breath.dts";
	explosionTag = mineExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb3
{
	bulletShapeName = "breath.dts";
	explosionTag = grenadeExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb4
{
	bulletShapeName = "breath.dts";
	explosionTag = Shockwave;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb5
{
	bulletShapeName = "breath.dts";
	explosionTag = LargeShockwave;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb6
{
	bulletShapeName = "breath.dts";
	explosionTag = rocketExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb7
{
	bulletShapeName = "breath.dts";
	explosionTag = energyExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb8
{
	bulletShapeName = "breath.dts";
	explosionTag = blasterExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb9
{
	bulletShapeName = "breath.dts";
	explosionTag = plasmaExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb10
{
	bulletShapeName = "breath.dts";
	explosionTag = turretExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb11
{
	bulletShapeName = "breath.dts";
	explosionTag = bulletExp0;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb12
{
	bulletShapeName = "breath.dts";
	explosionTag = debrisExpSmall;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb13
{
	bulletShapeName = "breath.dts";
	explosionTag = debrisExpMedium;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb14
{
	bulletShapeName = "breath.dts";
	explosionTag = debrisExpLarge;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb15
{
	bulletShapeName = "breath.dts";
	explosionTag = flashExpSmall;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb16
{
	bulletShapeName = "breath.dts";
	explosionTag = flashExpMedium;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb17
{
	bulletShapeName = "breath.dts";
	explosionTag = flashExpLarge;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb18
{
	bulletShapeName = "breath.dts";
	explosionTag = whiteExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb19
{
	bulletShapeName = "breath.dts";
	explosionTag = WhiteShockwave;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb20
{
	bulletShapeName = "breath.dts";
	explosionTag = IonExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb21
{
	bulletShapeName = "breath.dts";
	explosionTag = IonExp2;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb22
{
	bulletShapeName = "breath.dts";
	explosionTag = IonShockwave;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb23
{
	bulletShapeName = "breath.dts";
	explosionTag = IonShockwave2;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb24
{
	bulletShapeName = "breath.dts";
	explosionTag = PBShockWave;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb25
{
	bulletShapeName = "breath.dts";
	explosionTag = LitBoltExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb26
{
	bulletShapeName = "breath.dts";
	explosionTag = TimeShockwave;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb27
{
	bulletShapeName = "breath.dts";
	explosionTag = turretSlowExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb28
{
	bulletShapeName = "breath.dts";
	explosionTag = acidExp;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

RocketData SpellBomb29
{
	bulletShapeName = "breath.dts";
	explosionTag = bulletExp0;
	collisionRadius = 0.0;
	mass = 1.0;
	damageClass = 0;
	damageValue = 0.0;
	damageType = $NullDamageType;
	explosionRadius = 0.0;
	kickBackStrength = 0.0;
	muzzleVelocity = 0.1;
	terminalVelocity = 0.1;
	acceleration = 0.01;
	totalTime = 0.01;
	liveTime = 0.01;
	lightRange = 0.1;
	lightColor = {1.0,1.0,1.0};
	trailType = 0;
	soundId = NoSound;
};

//--------------------------------------
// Homing Missile Prototype (CRASHES - ENGINE MISMATCH)
//--------------------------------------
//SeekingMissileData MagicMissile
//{
//   bulletShapeName    = "enbolt.dts"; // Using energy bolt visual
//   explosionTag       = energyExp;
//   
//   damageClass        = 1; // Radius damage
//   damageValue        = 10.0;
//   damageType         = $SpellDamageType;
//   
//   muzzleVelocity     = 50.0;
//   totalTime          = 10.0; // Long life to track
//   liveTime           = 10.0;
//   
//   // Homing Parameters (from source analysis)
//   seekingTurningRadius    = 20.0; 
//   nonSeekingTurningRadius = 50.0; 
//   proximityDist           = 0.5;
//   smokeDist               = 1.5;
//   
//   lightRange         = 3.0;
//   lightColor         = { 0.4, 0.4, 1.0 };
//   
//   isVisible          = True;
//   
//   smokeName          = "smoke.dts"; // Native field found in source
//};
// Callback to calculate heat factor for target selection (required by engine)
function MagicMissile::updateTargetPercentage(%target)
{
   return 1.0; // Always track if valid target
}

//--------------------------------------
// Bouncing Projectile Prototype (DISABLED - POTENTIAL CRASH)
//--------------------------------------
//GrenadeData AcidFlask
//{
//   bulletShapeName    = "grenade.dts";
//   explosionTag       = acidExp;
//   
//   damageClass        = 1; 
//   damageValue        = 0.0; // Needs custom script damage? Or native? 
//   damageType         = $SpellDamageType;
//   
//   elasticity         = 0.6; // Bounciness (0.0 = no bounce, 1.0 = superball)
//   
//   muzzleVelocity     = 40.0;
//   totalTime          = 10.0;
//   liveTime           = 3.0; // Explodes after 3 seconds
//   
//   isVisible          = True;
//   
//   smokeName          = "smoke.dts"; // Native field found in source
//};

//--------------------------------------
// Shield Visuals Prototype (DISABLED - CAUSES ENGINE CRASH)
//--------------------------------------

// 1. Ice Shield visual
//PlayerData ShieldedIceArmor
//{
//   className = "Armor";
//   shapeFile = "larmor"; // Fixed: removed .dts
//   
//   // The magic field discovered in source:
//   shieldShapeName = "iceshield";
//   
//   // Other required fields to make it work as armor
//   flameShapeName = "flame"; // Fixed: removed .dts
//   visibleToSensor = True;
//   mapFilter = 1;
//   mapIcon = "M_player";
//   maxDamage = 100.0;
//   maxEnergy = 100.0;
//   maxForwardSpeed = 10.0; // Slow down?
//};

// 2. Dome Shield visual
//PlayerData ShieldedDomeArmor
//{
//   className = "Armor";
//   shapeFile = "larmor"; // Fixed: removed .dts
//   
//   // Using the dome field
//   shieldShapeName = "domefiled";
//   
//   flameShapeName = "flame"; // Fixed: removed .dts
//   visibleToSensor = True;
//   mapFilter = 1;
//   mapIcon = "M_player";
//   maxDamage = 100.0;
//   maxEnergy = 100.0;
//   maxForwardSpeed = 10.0;
//};

//--------------------------------------
// Chaos Lightning Prototype
//--------------------------------------
LightningData ChaosBolt
{
   bitmapName       = "lightning.bmp"; 
   boltLength       = 40.0; 
   damagePerSec     = 10.0;
   energyDrainPerSec = 10.0;
   
   // Chaos Parameters (from projLightning.cpp)
   displaceBias     = 0.5; // High jitter (default 0.25)
   beamWidth        = 0.5; // Thick beam (default 0.2)
   segmentDivisions = 4;   // Detailed
   
   explosionTag     = energyExp; // Standard hit
};