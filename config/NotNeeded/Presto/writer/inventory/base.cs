// ---------------------------------------------------------------------------
// inventory\base.cs -- Version 1.1 -- June 9, 2000
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// http://www.planetstarsiege.com/lorne/
// ---------------------------------------------------------------------------

// BASE armor:

//Inv::InitArmor(%itemname, %maxWeapons, %energyCapacity, %jetRate, %rechargeRate, %itemtype)
Inv::InitArmor("Light Armor", 3, 60, 25, 8);
Inv::InitArmor("Medium Armor", 4, 80, 32, 8);
Inv::InitArmor("Heavy Armor", 5, 110, 35, 8);

// BASE vehicles:

// Scout is handled by inventory.cs to take care of issues with some mods
Inv::InitVehicle("LPC", 2);
Inv::InitVehicle("HPC", 4);

// BASE weapons and ammo:

//Inv::initWeapon(%itemname, %itemnameWhenFound, %ammoname, %ammonameWhenFound, %fireTime, %reloadTime, %minEnergy, %maxEnergy, %spinUp, %spinDown, %itemtype, %ammotype)
Inv::InitWeapon("Blaster", "Blaster", "", "", 0.3, 0, "", 5, 6);
Inv::InitWeapon("Chaingun", "Chaingun", "Bullet", "Bullet", 0.2, 0, "", "", 0.5, 3);
Inv::InitWeapon("Disc Launcher", "Disc Launcher", "Disc", "Disc", 1.25, 0.25, "", "", 0.25);
Inv::InitWeapon("ELF gun", "ELF Gun", "", "", "", 0.2, 3, 11);

if(Inv::exists("Grenade Ammo"))
{
    Inv::InitWeapon("Grenade Launcher", "Grenade Launcher", "Grenade Ammo", "Grenade Ammo", 0.5, 0.5);
}
else
{
    Inv::force("Grenade_Ammo", "GrenadeAmmo");
    Inv::InitWeapon("Grenade Launcher", "Grenade Launcher", "GrenadeAmmo", "GrenadeAmmo", 0.5, 0.5);
}

Inv::InitWeapon("Laser Rifle", "Laser Rifle", "", "", 0.5, 0.1, 10, 60);

if(Inv::exists("Mortar Ammo"))
{
    Inv::InitWeapon("Mortar", "Mortar", "Mortar Ammo", "Mortar Ammo", 2.0, 0.5);
}
else
{
    Inv::force("Mortar_Ammo", "MortarAmmo");
    Inv::InitWeapon("Mortar", "Mortar", "MortarAmmo", "MortarAmmo", 2.0, 0.5);
}

Inv::InitWeapon("Plasma Gun", "Plasma Gun", "Plasma Bolt", "Plasma Bolt", 0.5, 0.1);
Inv::InitWeapon("Repair Gun", "", "", "", "", "", 3, 10);

// BASE tools:

Inv::InitTool("Targeting Laser", "Targeting Laser", "", "", "", 1.0, 5, 15);

// BASE deployable packs:

Inv::InitDeployable("Inventory Station", "DeployableInvPack backpack");
Inv::InitDeployable("Ammo Station", "DeployableAmmoPack backpack");
Inv::InitDeployable("Motion Sensor", "MotionSensorPack backpack");
Inv::InitDeployable("Pulse Sensor", "PulseSensorPack backpack");
Inv::InitDeployable("Sensor Jammer", "DeployableSensorJammerPack backpack");
Inv::InitDeployable("Camera", "CameraPack backpack");
Inv::InitDeployable("Turret", "TurretPack backpack");

// BASE usable packs:

//Inv::InitToggleable(%itemname, %itemnameWhenFound, %minEnergy, %maxEnergy, %defaultsOn, %ammoname, %ammonameWhenFound, %itemtype, %ammotype)
Inv::InitToggleable("Repair Pack", "RepairPack backpack", 0, 0);
Inv::InitToggleable("Shield Pack", "shieldpack backpack", 4, 9);
Inv::InitToggleable("Sensor Jammer Pack", "SensorJammerPack backpack", "", 10);

// BASE packs:

Inv::InitPack("Energy Pack", "EnergyPack backpack", false, false, -1, -3, true);
Inv::InitPack("Ammo Pack", "ammopack backpack");

// BASE throwable:

Inv::InitThrowable("Mine", "Mine");
Inv::InitThrowable("Grenade", "Grenade");


// BASE misc equipment:

Inv::InitMisc("Repair Kit", "Repair Kit");
Inv::InitMisc("Beacon", "Beacon");
