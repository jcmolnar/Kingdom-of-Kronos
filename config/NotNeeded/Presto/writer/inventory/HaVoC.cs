// ---------------------------------------------------------------------------
// inventory\HaVoC.cs -- Version 0.1 -- June 9, 2000
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// http://www.planetstarsiege.com/lorne/
// ---------------------------------------------------------------------------

// Temporarily includes a whole bunch of items from other mods...I still need
// sort the items according to mod type.

// MOD armor:

Inv::InitArmor("Alien");
Inv::InitArmor("Arbitor");
Inv::InitArmor("Assassin");
Inv::InitArmor("Assault");
Inv::InitArmor("Burster");
Inv::InitArmor("Chemeleon"); // misspelled in the mod?
Inv::InitArmor("Cyborg");
Inv::InitArmor("Dragoon");
Inv::InitArmor("DreadNaught");
Inv::InitArmor("Engineer");
Inv::InitArmor("Goliath");
Inv::InitArmor("Hoplite");
Inv::InitArmor("Juggernaught");
Inv::InitArmor("Mercenary");
Inv::InitArmor("Myrmidon");
Inv::InitArmor("Peltast");
Inv::InitArmor("Scout Armor");
Inv::InitArmor("Sniper");
Inv::InitArmor("Spy");

// MOD vehicles:

Inv::InitVehicle("Firestorm Bomber");
Inv::InitVehicle("Stealth LPC");
Inv::InitVehicle("Wraith");
Inv::InitVehicle("Interceptor");
Inv::InitVehicle("BomberLPC");
Inv::InitVehicle("StealthHPC");

// MOD weapons and ammo:

Inv::InitWeapon("BoomStick", "", "Boom Shell");
Inv::InitWeapon("Boom Stick", "", "Boom Shell"); // needed for Shifter?
Inv::InitWeapon("Dart Rifle", "", "Poison Dart");

if(Inv::exists("EMPGrenadeAmmo"))
{
    Inv::Force("EMP_Grenade_Launcher_Ammo", "", "EMPGrenadeAmmo");
    Inv::InitWeapon("EMP Grenade Launcher", "", "EMPGrenadeAmmo");
}
else if(Inv::exists("EMP Grenades"))
{
    Inv::Force("EMP_Grenade_Launcher_Ammo", "", "EMP Grenades");
    Inv::InitWeapon("EMP Grenade Launcher", "", "EMP Grenades");
}

Inv::InitWeapon("Engineer Repair-Gun");
Inv::InitWeapon("Equalizer", "", "Bullet");
Inv::InitWeapon("FGC-9000", "", "FGC-9000 Nukes");
Inv::InitWeapon("Flame Thrower");
Inv::InitWeapon("Flare Gun", "", "Flares");
Inv::InitWeapon("Fusion Blaster");
Inv::InitWeapon("Gauss Gun");
Inv::InitWeapon("Grav Gun");
Inv::InitWeapon("Hyper Blaster");
Inv::InitWeapon("Ion Rifle");
Inv::InitWeapon("IX-2000 Sniper Rifle", "", "Rifle Ammo");
Inv::InitWeapon("Jailers Gun");
Inv::InitWeapon("Laser Rifle, Rapidfire"); // becomes $Inv::Laser_Rifle_Rapidfire
Inv::InitWeapon("MAG Gun");
Inv::InitWeapon("Magnum", "", "Magnum Bullets");
Inv::InitWeapon("Mine Launcher", "", "Mine Launcher Ammo");
Inv::InitWeapon("Omega Cannon");
Inv::InitWeapon("Particle Accelerator");
Inv::InitWeapon("Phalanxx Cannon", "", "Phalanxx Ammo");
Inv::InitWeapon("Pyro-Torch", "", "Pyro Charge");
Inv::InitWeapon("Railgun", "", "Railgun Bolt");

if(Inv::exists("RocketAmmo"))
{
    Inv::Force("Rocket_Ammo", "", "RocketAmmo");
    Inv::InitWeapon("Rocket Launcher", "", "RocketAmmo");
}
else if(Inv::exists("Rockets"))
{
    Inv::Force("Rocket_Ammo", "", "Rockets");
    Inv::InitWeapon("Rocket Launcher", "", "Rockets");
}

Inv::InitWeapon("Shockwave Cannon");

if(Inv::exists("Shotgun Shells"))
{
    Inv::Force("Shotgun_Shell", "", "Shotgun Shells");
    Inv::InitWeapon("Shotgun", "", "Shotgun Shells");
}
else if(Inv::exists("Shotgun Shell"))
{
    Inv::InitWeapon("Shotgun", "", "Shotgun Shell");
}

Inv::InitWeapon("Sniper Rifle", "", "Sniper Bullet");
Inv::InitWeapon("Stinger", "", "Rockets");
Inv::InitWeapon("Tactical Nuke", "", "Nuclear Warhead");

if(Inv::exists("Thunderbolt"))
{
    Inv::InitWeapon("Thunderbolt");
    Inv::InitWeapon("Thunderbolt", "", "", getItemType("Thunderbolt") + 1);
}

Inv::InitWeapon("Volter");
Inv::InitWeapon("Vulcan", "", "Vulcan Bullet");

// MOD deployable packs:

Inv::InitDeployable("3X4 Force Field");
Inv::InitDeployable("3X4 Field Door");
Inv::InitDeployable("4X8 Force Field");
Inv::InitDeployable("4X8 Field Door");
Inv::InitDeployable("4X14 Force Field");
Inv::InitDeployable("4X14 Field Door");
Inv::InitDeployable("4X17 Force Field");
Inv::InitDeployable("4X17 Field Door");
Inv::InitDeployable("5X3 Force Field");
Inv::InitDeployable("5X3 Field Door");
Inv::InitDeployable("Air Large Platform");
Inv::InitDeployable("AirBase");
Inv::InitDeployable("Air Base");
Inv::InitDeployable("Air Ammo Pad");
Inv::InitDeployable("Accelerator Pad");
Inv::InitDeployable("Avenger");
Inv::InitDeployable("Base Alarm");
Inv::InitDeployable("Blast Wall");
Inv::InitDeployable("Chaingun Turret");
Inv::InitDeployable("Command Station");
Inv::InitDeployable("Deployable Platform");
Inv::InitDeployable("Deployable ELF Turret");
Inv::InitDeployable("Dissection Turret");
Inv::InitDeployable("ELF Turret");
Inv::InitDeployable("Emp Turret");
Inv::InitDeployable("Flag Decoy Pack");
Inv::InitDeployable("Flak Cannon");
Inv::InitDeployable("Flak Turret");
Inv::InitDeployable("Force Field");
Inv::InitDeployable("Force Field, Door");
Inv::InitDeployable("Force Field, Large");
Inv::InitDeployable("Force Field, Small");
Inv::InitDeployable("Guard Dog");
Inv::InitDeployable("Hologram");
Inv::InitDeployable("Jail Cell");
Inv::InitDeployable("Jail Capture Pad");
Inv::InitDeployable("Interceptor Pack");
Inv::InitDeployable("Ion Turret");
Inv::InitDeployable("Laser Turret");
Inv::InitDeployable("Large Force Field");
Inv::InitDeployable("Launch Pad");
Inv::InitDeployable("LR Motion Sensor");
Inv::InitDeployable("Mechanical Tree");
Inv::InitDeployable("Mini-ELF Turret");
Inv::InitDeployable("Mini-Plasma Turret");
Inv::InitDeployable("Missile Control Station");
Inv::InitDeployable("Missile Turret");
Inv::InitDeployable("Mortar Turret");
Inv::InitDeployable("Outpost");
Inv::InitDeployable("Panel One");
Inv::InitDeployable("Panel Two");
Inv::InitDeployable("Panel Three");
Inv::InitDeployable("Panel Four");
Inv::InitDeployable("Plasma Turret");
Inv::InitDeployable("Rail Turret");
Inv::InitDeployable("Rocket Turret");
Inv::InitDeployable("Seeking Missile Turret");
Inv::InitDeployable("Sentry");
Inv::InitDeployable("Shock Turret");
Inv::InitDeployable("Small Force Field");
Inv::InitDeployable("Sniper Pad");
Inv::InitDeployable("Solar Panel");
Inv::InitDeployable("Springboard");
Inv::InitDeployable("Springboard Pad");
Inv::InitDeployable("StealthHPC Pack");
Inv::InitDeployable("Stealth HPC Pack");
Inv::InitDeployable("Suicide DetPack");
Inv::InitDeployable("Teleport Pad");
Inv::InitDeployable("Teleport Pad Decoy");
Inv::InitDeployable("Vulcan Turret");
Inv::InitDeployable("Watchdog");

// MOD usable packs:

Inv::InitToggleable("Auto-Rocket Cannon");
Inv::InitToggleable("Cloaking Device");
Inv::InitToggleable("Cybernetic Laser");
Inv::InitToggleable("Lightning Pack");
Inv::InitToggleable("Regeneration Pack");
Inv::InitToggleable("StealthShield Pack");
Inv::InitToggleable("Vengeance Missile Pack");


// MOD packs:

Inv::InitPack("Accelerator Device");
Inv::InitPack("Command Laptop");
Inv::InitPack("Containment Pack");
Inv::InitPack("EMP Missile");
Inv::InitPack("FGC-9000 Pack");
Inv::InitPack("Flight Pack");
Inv::InitPack("HaVoC Missile");
Inv::InitPack("Healing Plant");
Inv::InitPack("Heat Sink");
Inv::InitPack("Kamikaze Pack");
Inv::InitPack("Napalm Missile");
Inv::InitPack("Nuke Pack");
Inv::InitPack("ObeliskofDeath");
Inv::InitPack("ObeliskofLight");
Inv::InitPack("ObeliskofPowerSource");
Inv::InitPack("PhaseLok");
Inv::InitPack("Phoenix Missile");
Inv::InitPack("Repairgun");
Inv::InitPack("Rocket Battery");
Inv::InitPack("Rocket Booster"); // should this be InitMisc instead?
Inv::InitPack("Rocket Launcher Pack", "", false, true, "", "", "", "Auto Rockets", "");
Inv::InitPack("Seeker");
Inv::InitPack("Shield Generator");
Inv::InitPack("SpyPod Missile");
Inv::InitPack("Toxin Missile");

// MOD throwable:

// MOD misc equipment:
