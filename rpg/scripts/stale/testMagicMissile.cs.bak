// Test script for Magic Missile Prototype
// Exec this file: exec("rpg/scripts/testMagicMissile.cs");
// Then call: FireMissile(2049); (replace with your ClientID)

function FireMissile(%client)
{
   %player = Client::getOwnedObject(%client);
   if (!isObject(%player))
   {
      echo("Error: Invalid player object for client " @ %client);
      return;
   }
   
   %muzzle = GameBase::getMuzzleTransform(%player);
   
   // Spawn the MagicMissile
   // Arguments: DatablockName, Transform, ShooterID, Offset
   %proj = Projectile::spawnProjectile("MagicMissile", %muzzle, %player, %muzzle);
   
   if (%proj != -1 && %proj != "")
      echo("Magic Missile Fired! Projectile ID: " @ %proj);
   else
      echo("Failed to spawn Magic Missile. Check if 'SeekingMissileData' is supported by your engine.");
}

echo("Magic Missile Test Script Loaded. Use FireMissile(clientId); to test.");

function FireFlask(%client)
{
   %player = Client::getOwnedObject(%client);
   %muzzle = GameBase::getMuzzleTransform(%player);
   %proj = Projectile::spawnProjectile("AcidFlask", %muzzle, %player, %muzzle);
   
   if (%proj != -1 && %proj != "")
      echo("Acid Flask Fired! Projectile ID: " @ %proj);
   else
      echo("Failed to spawn Acid Flask. Check if 'GrenadeData' is supported by your engine.");
}

//function TestShield(%client, %type)
//{
//   %player = Client::getOwnedObject(%client);
//   
//   if(%type == 1)
//      Player::setArmor(%client, "ShieldedIceArmor");
//   else if(%type == 2)
//      Player::setArmor(%client, "ShieldedDomeArmor");
//   else
//      Player::setArmor(%client, "larmor"); // Reset
//      
//   echo("Swapped armor to type: " @ %type);
//}

function FireChaos(%client)
{
   %player = Client::getOwnedObject(%client);
   %muzzle = GameBase::getMuzzleTransform(%player);
   %proj = Projectile::spawnProjectile("ChaosBolt", %muzzle, %player, %muzzle);
   echo("Fired Chaos Bolt: " @ %proj);
}
