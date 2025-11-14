// ---------------------------------------------------------------------------
// inventory\insomniax.cs -- Version 0.4 -- June 19, 2000
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// Thanks to Neofight for trimming this down to only the Insomniax items :)
//
// http://www.planetstarsiege.com/lorne/
// ---------------------------------------------------------------------------

include("writer\\inventory\\base.cs");

// MOD armor:

// None: All base armors (unaltered)

// MOD vehicles:

Inv::InitVehicle("Firestorm Bomber");
Inv::InitVehicle("Stealth LPC");
// Plus all base vehicles (unaltered)

// MOD weapons and ammo:

Inv::InitWeapon("MAG Gun");
Inv::InitWeapon("Pyro-Torch", "", "Pyro Charge");
Inv::InitWeapon("IX-2000 Sniper Rifle", "", "Rifle Ammo");
Inv::InitWeapon("Shotgun", "", "Shotgun Shells");
Inv::InitWeapon("Phalanxx Cannon", "", "Phalanxx Ammo");
Inv::InitWeapon("Rocket Launcher", "", "RocketAmmo");
Inv::InitWeapon("EMP Grenade Launcher", "", "EMPGrenadeAmmo");
Inv::InitWeapon("Heavy Thruster"); // "invisible" weapon used to control rocket booster
Inv::InitWeapon("Medium Thruster"); // "invisible" weapon used to control rocket booster
// Plus all base weapons (unaltered)

// MOD deployable packs:

Inv::InitDeployable("Small Force Field");
Inv::InitDeployable("Sentry");
Inv::InitDeployable("Avenger");
Inv::InitDeployable("Flak Cannon");
// Plus all base deployables (unaltered)

// MOD usable packs:

Inv::InitToggleable("Cloaking Device");
Inv::InitToggleable("Vengeance Missile Pack");
Inv::InitPack("Rocket Booster"); 
// Plus all base packs (unaltered)

// MOD packs:

Inv::InitPack("Heat Sink");
// Plus all base packs (unaltered)

// MOD throwable:

// None

// MOD misc equipment:

// None
