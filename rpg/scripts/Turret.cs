//----------------------------------------------------------------------------
// TURRET DYNAMIC DATA

TurretData PlasmaTurret
{
	maxDamage = 100;
	maxEnergy = 200;
	minGunEnergy = 75;
	maxGunEnergy = 6;
	reloadDelay = 0.4;
	range = 125;
	dopplerVelocity = 0;
	castLOS = true;
	supression = false;
	mapFilter = 2;
	mapIcon = "M_turret";
	visibleToSensor = true;
	debrisId = defaultDebrisMedium;
	className = "Turret";
	shapeFile = "hellfiregun";
	shieldShapeName = "shield_medium";
	speed = 4.0;
	speedModifier = 4.0;
	projectileType = FusionBolt;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 8;
	explosionId = LargeShockwave;
	description = "Fireball Spitter";
};
																						 
TurretData GuardianTurret
{
	maxDamage = 200;
	maxEnergy = 400;
	minGunEnergy = 75;
	maxGunEnergy = 6;
	reloadDelay = 0.3;
	range = 150;
	dopplerVelocity = 0;
	castLOS = true;
	supression = false;
	mapFilter = 2;
	mapIcon = "M_turret";
	visibleToSensor = true;
	debrisId = defaultDebrisMedium;
	className = "Turret";
	shapeFile = "hellfiregun";
	shieldShapeName = "shield_medium";
	speed = 4.0;
	speedModifier = 4.0;
	projectileType = GuardianBolt;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 8;
	explosionId = LargeShockwave;
	description = "Fireball Spitter";
};

TurretData RocketTurret
{
	maxDamage = 0.75;
	maxEnergy = 100;
	minGunEnergy = 60;
	maxGunEnergy = 60;
	range = 150;
	gunRange = 300;
	visibleToSensor = true;
	dopplerVelocity = 0;
	castLOS = true;
	supression = false;
	mapFilter = 2;
	mapIcon = "M_turret";
	debrisId = defaultDebrisLarge;
	className = "Turret";
	shapeFile = "missileturret";
	shieldShapeName = "shield_medium";
	speed = 2.0;
	speedModifier = 2.0;
//	projectileType = TurretMissile;
//	reloadDelay = 3.5;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 8;
   targetableFovRatio = 0.5;
	explosionId = LargeShockwave;
	description = "Rocket Turret";
};

function RocketTurret::onPower(%this,%power,%generator)
{
	if (%power) {
		%this.shieldStrength = 0.03;
		GameBase::setRechargeRate(%this,14);
	}
	else {
		%this.shieldStrength = 0;
		GameBase::setRechargeRate(%this,0);
		Turret::checkOperator(%this);
	}
	GameBase::setActive(%this,%power);
}

function RocketTurret::verifyTarget(%target)
{
   if (GameBase::virtual(%target, "getHeatFactor") >= 0.5)
      return "True";
   else
      return "False";
}

//--------------------------------------------

TurretData TempleIndoorTurret
{
	className = "Turret";
	shapeFile = "indoorgun";
	projectileType = TempleFusionBolt;
	maxDamage = 50;
	maxEnergy = 60;
	minGunEnergy = 20;
	maxGunEnergy = 6;
	reloadDelay = 0.2;
	speed = 5.0;
	speedModifier = 1.0;
	range = 50;
	visibleToSensor = true;
	dopplerVelocity = 2;
	castLOS = true;
	supression = false;
	supressable = false;
	pinger = false;
	mapFilter = 2;
	mapIcon = "M_turret";
	debrisId = defaultDebrisMedium;
	shieldShapeName = "shield";
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 8;
	explosionId = debrisExpMedium;
	description = "Indoor Turret";

};
																						 
//--------------------------------------------

TurretData IndoorTurret
{
	className = "Turret";
	shapeFile = "indoorgun";
	projectileType = MiniFusionBolt;
	maxDamage = 50;
	maxEnergy = 60;
	minGunEnergy = 20;
	maxGunEnergy = 6;
	reloadDelay = 0.3;
	speed = 5.0;
	speedModifier = 1.0;
	range = 25;
	visibleToSensor = true;
	dopplerVelocity = 2;
	castLOS = true;
	supression = false;
	supressable = false;
	pinger = false;
	mapFilter = 2;
	mapIcon = "M_turret";
	debrisId = defaultDebrisMedium;
	shieldShapeName = "shield";
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 8;
	explosionId = debrisExpMedium;
	description = "Indoor Turret";

};


//--------------------------------------------

TurretData DeployableTurret
{
	className = "Turret";
	shapeFile = "remoteturret";
	projectileType = MiniFusionBolt;
	maxDamage = 0.65;
	maxEnergy = 60;
	minGunEnergy = 6;
	maxGunEnergy = 5;
	reloadDelay = 0.4;
	speed = 4.0;
	speedModifier = 1.5;
	range = 30;
	visibleToSensor = true;
	shadowDetailMask = 4;
	dopplerVelocity = 0;
	castLOS = true;
	supression = false;
	mapFilter = 2;
	mapIcon = "M_turret";
	debrisId = flashDebrisMedium;
	shieldShapeName = "shield";
	explosionId = flashExpMedium;
	description = "Remote Turret";
	damageSkinData = "objectDamageSkins";
};

//-----------------------------------------------

//Psynergy Liquifier
TurretData HeatLaserCannon
{
	maxDamage = 99;
	maxEnergy = 100;
	minGunEnergy = 1;
	maxGunEnergy = 1;
	range = 0;
	visibleToSensor = false;
	dopplerVelocity = 0;
	castLOS = true;
	supression = false;
	mapFilter = 0;
	mapIcon = "M_turret";
	debrisId = NullDebris;
	className = "Turret";
	projectileType = heatLaser;
	shapeFile = "camera";
	speed = 2.0;
	speedModifier = 2.0;
//	projectileType = LaserTurretShot;
	reloadDelay = 0.6;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 8;
	explosionId = blasterExp;
	description = "";
};

function DeployableTurret::onAdd(%this)
{
	schedule("DeployableTurret::deploy(" @ %this @ ");",1,%this);
	GameBase::setRechargeRate(%this,5);
	%this.shieldStrength = 0;
	if (GameBase::getMapName(%this) == "") {
		GameBase::setMapName (%this, "Remote Turret");
	}
}

function DeployableTurret::deploy(%this)
{
	GameBase::playSequence(%this,1,"deploy");
}

function DeployableTurret::onEndSequence(%this,%thread)
{
	GameBase::setActive(%this,true);
}

function DeployableTurret::onDestroyed(%this)
{
	Turret::onDestroyed(%this);
  	$TeamItemCount[GameBase::getTeam(%this) @ "TurretPack"]--;
}

// Override base class just in case.
function DeployableTurret::onPower(%this,%power,%generator) {}
function DeployableTurret::onEnabled(%this) 
{
	GameBase::setRechargeRate(%this,5);
	GameBase::setActive(%this,true);
}	


//--------------------------------------------

TurretData CameraTurret
{
	className = "Turret";
	shapeFile = "camera";
	maxDamage = 0.25;
	maxEnergy = 10;
	speed = 20;
	speedModifier = 1.0;
	range = 50;
	visibleToSensor = true;
	shadowDetailMask = 4;
	castLOS = true;
	supression = false;
	supressable = false;
	mapFilter = 2;
	mapIcon = "M_camera";
	debrisId = defaultDebrisSmall;
	FOV = 0.707;
	pinger = false;
	explosionId = debrisExpMedium;
	description = "Camera";
};

function CameraTurret::onAdd(%this)
{
	schedule("CameraTurret::deploy(" @ %this @ ");",1,%this);
	if (GameBase::getMapName(%this) == "") {
		GameBase::setMapName (%this, "Camera");
	}
}

function CameraTurret::deploy(%this)
{
	GameBase::playSequence(%this,1,"deploy");
}

function CameraTurret::onEndSequence(%this,%thread)
{
	GameBase::setActive(%this,true);
}

function CameraTurret::onDestroyed(%this)
{
	Turret::onDestroyed(%this);
  	$TeamItemCount[GameBase::getTeam(%this) @ "CameraPack"]--;
}	


//---------------------------------------------------

//**RPG
function Turret::verifyTarget(%this, %target)
{
	%Id = Player::getClient(%target);
	
	// DEBUG: Log verifyTarget calls to diagnose team 0 targeting issue
	%turretTeam = GameBase::getTeam(%this);
	%playerTeam = GameBase::getTeam(%target);
	%playerName = Client::getName(%Id);
	
	// Throttle debug output (only log once per 5 seconds per turret)
	%currentTime = getIntegerTime(true);
	%lastLogTime = $TurretVerifyTargetLastLog[%this];
	if(%lastLogTime == "" || %lastLogTime == "0" || %lastLogTime == -1)
		%lastLogTime = 0;
	%timeSinceLastLog = %currentTime - %lastLogTime;
	%shouldLog = false;
	if(%timeSinceLastLog >= 5000) // 5 seconds
		%shouldLog = true;
	
	if(%shouldLog)
	{
		echo("[TURRET DEBUG] verifyTarget() called - Turret team=" @ %turretTeam @ ", Player team=" @ %playerTeam @ ", Player=" @ %playerName);
		$TurretVerifyTargetLastLog[%this] = %currentTime;
	}
	
	// Check if target is a valid player
	if(%Id == -1 || %Id == "")
	{
		if(%shouldLog)
			echo("[TURRET DEBUG] verifyTarget() - Invalid target (no client ID)");
		return "False"; // Invalid target
	}

	// Estate.cs: guardian turret stance (ZONES 2026-07-15). Friendly/protected
	// estates (the default - $Estate::Mode != "hostile") hold fire on EVERYONE.
	// Hostile ("dungeon") estates fire on strangers but NEVER on the owner or
	// permitted members, regardless of House (a houseless owner must still be
	// safe on their land). Players are warned at 2x plot radius before any of
	// this matters (Estate::ZoneLoop).
	%estateId = $EstateOf[%this];
	if(%estateId != "" && %estateId != 0)
	{
		if(!Estate::IsHostile(%estateId))
			return "False";
		%pName = Client::getName(%Id);
		if(%pName == $Estate::Owner[%estateId] || IsInCommaList($Estate::Members[%estateId], %pName))
			return "False";
	}

	// Get player's house (can be empty string if player has no house)
	%House = fetchData(%Id, "MyHouse");
	
	// If player has no house, they're always a valid target
	if(%House == "" || %House == -1)
	{
		if(%shouldLog)
			echo("[TURRET DEBUG] verifyTarget() - Player has no house, APPROVED");
		return "True";
	}
	
	// Get turret's house from .Team property
	%turretHouse = %this.Team;
	
	// If turret has no house assigned (neutral), target all players
	if(%turretHouse == "" || %turretHouse == -1)
	{
		if(%shouldLog)
			echo("[TURRET DEBUG] verifyTarget() - Turret has no house (neutral), APPROVED");
		return "True";
	}
	
	// Only target players from opposing houses (different house)
	if(%turretHouse == %House)
	{
		if(%shouldLog)
			echo("[TURRET DEBUG] verifyTarget() - Same house (" @ %turretHouse @ "), REJECTED");
		return "False"; // Same house - don't target
	}
	
	// Different house - valid target
	if(%shouldLog)
		echo("[TURRET DEBUG] verifyTarget() - Different house (turret=" @ %turretHouse @ ", player=" @ %House @ "), APPROVED");
	return "True";
}

// Ensure all turrets in the mission are on team 1 (enemy of team 0 players)
// This function should be called after mission load to fix any turrets that were initialized with wrong team
function EnsureAllTurretsOnTeam1()
{
	%missionGroup = nameToID("MissionGroup");
	if(%missionGroup == -1)
	{
		echo("[TURRET FIX] ERROR: MissionGroup not found, cannot fix turret teams");
		return;
	}
	
	%turretCount = 0;
	%fixedCount = 0;
	
	// Recursively search for all turrets
	%objCount = Group::objectCount(%missionGroup);
	for(%i = 0; %i < %objCount; %i++)
	{
		%obj = Group::getObject(%missionGroup, %i);
		if(%obj == -1 || %obj == "")
			continue;
		
		%dataName = GameBase::getDataName(%obj);
		
		// Check if this is a turret
		if(String::findSubStr(%dataName, "Turret") != -1)
		{
			%turretCount++;
			%currentTeam = GameBase::getTeam(%obj);
			if(%currentTeam != 1)
			{
				GameBase::setTeam(%obj, 1);
				%fixedCount++;
				echo("[TURRET FIX] Fixed turret " @ %obj @ " (" @ %dataName @ ") - was team " @ %currentTeam @ ", set to team 1");
			}
		}
		
		// Also check sub-groups recursively
		if(getObjectType(%obj) == "SimGroup")
		{
			%subObjCount = Group::objectCount(%obj);
			for(%j = 0; %j < %subObjCount; %j++)
			{
				%subObj = Group::getObject(%obj, %j);
				if(%subObj == -1 || %subObj == "")
					continue;
				
				%subDataName = GameBase::getDataName(%subObj);
				if(String::findSubStr(%subDataName, "Turret") != -1)
				{
					%turretCount++;
					%subCurrentTeam = GameBase::getTeam(%subObj);
					if(%subCurrentTeam != 1)
					{
						GameBase::setTeam(%subObj, 1);
						%fixedCount++;
						echo("[TURRET FIX] Fixed turret " @ %subObj @ " (" @ %subDataName @ ") - was team " @ %subCurrentTeam @ ", set to team 1");
					}
				}
			}
		}
	}
	
	if(%turretCount > 0)
		echo("[TURRET FIX] Checked " @ %turretCount @ " turret(s), fixed " @ %fixedCount @ " turret(s) to team 1");
}
//**

function Turret::onAdd(%this)
{
	if (GameBase::getMapName(%this) == "")
	{
		GameBase::setMapName (%this, "Turret");
	}
	
	// CRITICAL: Ensure all turrets are on team 1 (enemy of team 0 players) to activate targeting
	// The engine's built-in targeting system requires turrets to be on a different team than players
	// Team 0 = players (citizens), Team 1 = enemy team for turrets
	%currentTeam = GameBase::getTeam(%this);
	if(%currentTeam != 1)
	{
		GameBase::setTeam(%this, 1);
		echo("[TURRET INIT] Turret " @ %this @ " was on team " @ %currentTeam @ ", set to team 1 for targeting");
	}
}

function Turret::onActivate(%this)
{
	GameBase::playSequence(%this,0,power);
}

function Turret::onDeactivate(%this)
{
	GameBase::stopSequence(%this,0);
	Turret::checkOperator(%this);
}

function Turret::onSetTeam(%this,%oldTeam)
{
	if(GameBase::getTeam(%this) != Client::getTeam(GameBase::getControlClient(%this))) 
		Turret::checkOperator(%this);

}

function Turret::checkOperator(%this)
{
   %cl = GameBase::getControlClient(%this);
   if(%cl != -1) {
   	%pl = Client::getOwnedObject(%cl);
		Player::setMountObject(%pl, -1,0);
	   Client::setControlObject(%cl, %pl);
   }
	Client::setGuiMode(%cl,2);
}

function Turret::onPower(%this,%power,%generator)
{
	if (%power) {
		%this.shieldStrength = 0.03;
		GameBase::setRechargeRate(%this,10);
	}
	else {
		%this.shieldStrength = 0;
		GameBase::setRechargeRate(%this,0);
		Turret::checkOperator(%this);
	}
	GameBase::setActive(%this,%power);
}

function Turret::onEnabled(%this)
{
	if (GameBase::isPowered(%this)) {
		%this.shieldStrength = 0.03;
		GameBase::setRechargeRate(%this,10);
		GameBase::setActive(%this,true);
	}
}

function Turret::onDisabled(%this)
{
	%this.shieldStrength = 0;
	GameBase::setRechargeRate(%this,0);
	Turret::onDeactivate(%this);
}

function Turret::onDestroyed(%this)
{
	//**RPG
//	for(%i=1; $turretIndex[%i] != %this; %i++)
//	{}
//	$turretPos[%i] = "";
//	$turretIndex[%i] = "";
//	$owner[%this] = "";

	$owner[%this] = "";
	//**

	StaticShape::objectiveDestroyed(%this);
	%this.shieldStrength = 0;
	GameBase::setRechargeRate(%this,0);
	Turret::onDeactivate(%this);
	Turret::objectiveDestroyed(%this);
	calcRadiusDamage(%this, $DebrisDamageType, 2.5, 0.05, 25, 9, 3, 0.40, 
		0.1, 200, 100); 
}

function Turret::onDamage(%this,%type,%value,%pos,%vec,%mom,%object)
{
        if(%this.objectiveLine)
		%this.lastDamageTeam = GameBase::getTeam(%object);
	%TDS= 1;
	if(GameBase::getTeam(%this) == GameBase::getTeam(%object)) {
		%name = GameBase::getDataName(%this);
		if(%name != DeployableTurret && %name != CameraTurret )
			%TDS = $Server::TeamDamageScale;
	}
	StaticShape::shieldDamage(%this,%type,%value * %TDS,%pos,%vec,%mom,%object);
}

function Turret::onControl (%this, %object)
{
	%clientId = Player::getClient(%object);
	Client::sendMessage(%clientId,0,"Controlling turret " @ %this);
}

function Turret::onDismount (%this, %object)
{
	%clientId = Player::getClient(%object);
	Client::sendMessage(%clientId,0,"Leaving turret " @ %this);
}

//function Turret::onCollision (%this, %object)
//{
//      if (getObjectType (%object) == "Player")
//      {
//              Player::mountObject (%object, %this);
//      }
//}
