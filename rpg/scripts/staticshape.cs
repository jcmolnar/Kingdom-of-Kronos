//------------------------------------------------------------------------
// Generic static shapes
//------------------------------------------------------------------------


//------------------------------------------------------------------------
// Default power animation behavior for all static shapes

function StaticShape::onPower(%this,%power,%generator)
{
	if (%power) 
		GameBase::playSequence(%this,0,"power");
	else 
		GameBase::stopSequence(%this,0);
}

function StaticShape::onEnabled(%this)
{
	if (GameBase::isPowered(%this)) 
		GameBase::playSequence(%this,0,"power");
}

function StaticShape::onDisabled(%this)
{
	GameBase::stopSequence(%this,0);
}

function StaticShape::onDestroyed(%this)
{
	$owner[%this] = "";
	GameBase::stopSequence(%this,0);
	StaticShape::objectiveDestroyed(%this);
	calcRadiusDamage(%this, $DebrisDamageType, 2.5, 0.05, 25, 13, 2, 0.40, 0.1, 250, 100); 
}

function StaticShape::onDamage(%this,%type,%value,%pos,%vec,%mom,%object)
{
	%damageLevel = GameBase::getDamageLevel(%this);
	%dValue = %damageLevel + %value;
   %this.lastDamageObject = %object;
   %this.lastDamageTeam = GameBase::getTeam(%object);
	if(GameBase::getTeam(%this) == GameBase::getTeam(%object)) {
		%name = GameBase::getDataName(%this);
		if(%name.className == Generator || %name.className == Station) { 
			%TDS = $Server::TeamDamageScale;
			%dValue = %damageLevel + %value * %TDS;
			%disable = GameBase::getDisabledDamage(%this);
			if(!$Server::TourneyMode && %dValue > %disable - 0.05) {
            if(%damageLevel > %disable - 0.05)
               return;
            else
               %dValue = %disable - 0.05;
			}
		}
	}
	else
	{
		GameBase::setDamageLevel(%this,%dValue);
	}
}

function StaticShape::shieldDamage(%this,%type,%value,%pos,%vec,%mom,%object)
{
	%damageLevel = GameBase::getDamageLevel(%this);
   %this.lastDamageObject = %object;
   %this.lastDamageTeam = GameBase::getTeam(%object);
	if (%this.shieldStrength) {
		%energy = GameBase::getEnergy(%this);
		%strength = %this.shieldStrength;
		if (%type == $ShrapnelDamageType)
			%strength *= 0.5;
		else
			if (%type == $MortarDamageType)
				%strength *= 0.25;
			else
				if (%type == $BlasterDamageType)
					%strength *= 2.0;
		%absorb = %energy * %strength;
		if (%value < %absorb) {
			GameBase::setEnergy(%this,%energy - (%value / %strength));
			%centerPos = getBoxCenter(%this);
			%sphereVec = findPointOnSphere(getBoxCenter(%object),%centerPos,%vec,%this);
			%centerPosX = getWord(%centerPos,0);
			%centerPosY = getWord(%centerPos,1);
			%centerPosZ = getWord(%centerPos,2);

			%pointX = getWord(%pos,0);
			%pointY = getWord(%pos,1);
			%pointZ = getWord(%pos,2);

			%newVecX = %centerPosX - %pointX;
			%newVecY = %centerPosY - %pointY;
			%newVecZ = %centerPosZ - %pointZ;
			%norm = Vector::normalize(%newVecX @ " " @ %newVecY @ " " @ %newVecZ);
			%zOffset = 0;
			if(GameBase::getDataName(%this) == PulseSensor)
				%zOffset = (%pointZ-%centerPosZ) * 0.5;
			GameBase::activateShield(%this,%sphereVec,%zOffset);
		}
		else {
			GameBase::setEnergy(%this,0);
			StaticShape::onDamage(%this,%type,%value - %absorb,%pos,%vec,%mom,%object);
		}
	}
	else {
		StaticShape::onDamage(%this,%type,%value,%pos,%vec,%mom,%object);
	}
}

StaticShapeData FlagStand
{
   description = "Flag Stand";
	shapeFile = "flagstand";
	visibleToSensor = false;
};


function calcRadiusDamage(%this,%type,%radiusRatio,%damageRatio,%forceRatio,
	%rMax,%rMin,%dMax,%dMin,%fMax,%fMin) 
{
	%radius = GameBase::getRadius(%this);
	if(%radius) {
		%radius *= %radiusRatio;
		%damageValue = %radius * %damageRatio;
		%force = %radius * %forceRatio;
		if(%radius > %rMax)
			%radius = %rMax;
		else if(%radius < %rMin)
			%radius = %rMin;
		if(%damageValue > %dMax)
			%damageValue = %dMax; 
		else if(%damageValue < %dMin)
			%damageValue = %dMin;
		if(%force > %fMax)
			%force = %fMax; 
		else if(%force < %fMin)
			%force = %fMin;
		GameBase::applyRadiusDamage(%type,getBoxCenter(%this), %radius,
			%damageValue,%force,%this);
	}
}



function FlagStand::onDamage()
{
}

//------------------------------------------------------------------------
// Generators
//------------------------------------------------------------------------

function Generator::onEnabled(%this)
{
	GameBase::setActive(%this,true);
}

function Generator::onDisabled(%this)
{
	GameBase::stopSequence(%this,0);
 	GameBase::generatePower(%this, false);
}

function Generator::onDestroyed(%this)
{
	Generator::onDisabled(%this);
	StaticShape::objectiveDestroyed(%this);
	calcRadiusDamage(%this, $DebrisDamageType, 2.5, 0.05, 25, 13, 3, 0.55, 0.30, 250, 170); 
}

function Generator::onActivate(%this)
{
	GameBase::playSequence(%this,0,"power");
	GameBase::generatePower(%this, true);
}

function Generator::onDeactivate(%this)
{
	GameBase::stopSequence(%this,0);
 	GameBase::generatePower(%this, false);
}

//

StaticShapeData TowerSwitch
{
	description = "Tower Control Switch";
	className = "towerSwitch";
	shapeFile = "tower";
	showInventory = "false";
	visibleToSensor = true;
	mapFilter = 4;
	mapIcon = "M_generator";
};

StaticShapeData Generator
{
   description = "Generator";
   shapeFile = "generator";
	className = "Generator";
	debrisId = flashDebrisLarge;
	explosionId = flashExpLarge;
   maxDamage = 2.0;
	visibleToSensor = true;
	mapFilter = 4;
	mapIcon = "M_generator";
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
};

StaticShapeData SolarPanel
{
   description = "Solar Panel";
	shapeFile = "solar_med";
	className = "Generator";
	debrisId = flashDebrisMedium;
	maxDamage = 1.0;
	visibleToSensor = true;
	mapFilter = 4;
	mapIcon = "M_generator";
    damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = flashExpLarge;
};

StaticShapeData PortGenerator
{
   description = "Portable Generator";
   shapeFile = "generator_p";
	className = "Generator";
	debrisId = flashDebrisSmall;
   maxDamage = 1.6;
	mapIcon = "M_generator";
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = flashExpMedium;
	visibleToSensor = true;
	mapFilter = 4;
};


//------------------------------------------------------------------------
StaticShapeData SmallAntenna
{
	shapeFile = "anten_small";
	debrisId = defaultDebrisSmall;
	maxDamage = 1.0;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = flashExpMedium;
   description = "Small Antenna";
};

//------------------------------------------------------------------------
StaticShapeData MediumAntenna
{
	shapeFile = "anten_med";
	debrisId = flashDebrisSmall;
	maxDamage = 1.5;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = flashExpMedium;
   description = "Medium Antenna";
};

//------------------------------------------------------------------------
StaticShapeData LargeAntenna
{
	shapeFile = "anten_lrg";
	debrisId = defaultDebrisSmall;
	maxDamage = 1.5;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = debrisExpMedium;
   description = "Large Antenna";
};

//------------------------------------------------------------------------
StaticShapeData ArrayAntenna
{
	shapeFile = "anten_lava";
	debrisId = flashDebrisSmall;
	maxDamage = 1.5;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = flashExpMedium;
   description = "Array Antenna";
};

//------------------------------------------------------------------------
StaticShapeData RodAntenna
{
	shapeFile = "anten_rod";
	debrisId = defaultDebrisSmall;
	maxDamage = 1.5;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = debrisExpMedium;
   description = "Rod Antenna";
};

//------------------------------------------------------------------------
StaticShapeData ForceBeacon
{
	shapeFile = "force";
	debrisId = defaultDebrisSmall;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = debrisExpMedium;
   description = "Force Beacon";
};

//------------------------------------------------------------------------
StaticShapeData CargoCrate
{
	shapeFile = "magcargo";
	debrisId = flashDebrisSmall;
	maxDamage = 1.0;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = flashExpMedium;
	description = "CargoCrate";
};

//------------------------------------------------------------------------
StaticShapeData CargoBarrel
{
	shapeFile = "liqcyl";
	debrisId = defaultDebrisSmall;
	maxDamage = 1.0;
	damageSkinData = "objectDamageSkins";
	shadowDetailMask = 16;
	explosionId = debrisExpMedium;
   description = "Cargo Barrel";
};

//------------------------------------------------------------------------
StaticShapeData SquarePanel
{
	shapeFile = "teleport_square";
	debrisId = flashDebrisSmall;
	maxDamage = 0.3;
	damageSkinData = "objectDamageSkins";
	explosionId = flashExpMedium;
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData VerticalPanel
{
	shapeFile = "teleport_vertical";
	debrisId = defaultDebrisSmall;
	explosionId = debrisExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData BluePanel
{
	shapeFile = "panel_blue";
	debrisId = flashDebrisSmall;
	explosionId = flashExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData YellowPanel
{
	shapeFile = "panel_yellow";
	debrisId = defaultDebrisSmall;
	explosionId = debrisExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData SetPanel
{
	shapeFile = "panel_set";
	debrisId = flashDebrisSmall;
	explosionId = flashExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData VerticalPanelB
{
	shapeFile = "panel_vertical";
	debrisId = defaultDebrisSmall;
	explosionId = debrisExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData DisplayPanelOne
{
	shapeFile = "display_one";
	debrisId = flashDebrisSmall;
	explosionId = flashExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData DisplayPanelTwo
{
	shapeFile = "display_two";
	debrisId = defaultDebrisSmall;
	explosionId = debrisExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData DisplayPanelThree
{
	shapeFile = "display_three";
	debrisId = flashDebrisSmall;
	explosionId = flashExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData HOnePanel
{
	shapeFile = "dsply_h1";
	debrisId = defaultDebrisSmall;
	explosionId = debrisExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData HTwoPanel
{
	shapeFile = "dsply_h2";
	debrisId = flashDebrisSmall;
	explosionId = flashExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData SOnePanel
{
	shapeFile = "dsply_s1";
	debrisId = defaultDebrisSmall;
	explosionId = debrisExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData STwoPanel
{
	shapeFile = "dsply_s2";
	debrisId = flashDebrisSmall;
	explosionId = flashExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData VOnePanel
{
	shapeFile = "dsply_v1";
	debrisId = defaultDebrisSmall;
	explosionId = debrisExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData VTwoPanel
{
	shapeFile = "dsply_v2";
	debrisId = flashDebrisSmall;
	explosionId = flashExpMedium;
	maxDamage = 0.5;
	damageSkinData = "objectDamageSkins";
   description = "Panel";
};

//------------------------------------------------------------------------
StaticShapeData RForceField
{
	shapeFile = "forcefield";
	debrisId = defaultDebrisSmall;
	maxDamage = 10000.0;
	isTranslucent = true;
	description = "Force Field";
};

//------------------------------------------------------------------------
StaticShapeData ElectricalBeam
{
	shapeFile = "zap";
	maxDamage = 10000.0;
	isTranslucent = true;
    description = "Electrical Beam";
   disableCollision = true;
};

StaticShapeData ElectricalBeamBig
{
	shapeFile = "zap_5";
	maxDamage = 10000.0;
	isTranslucent = true;
    description = "Electrical Beam";
   disableCollision = true;
};

StaticShapeData PoweredElectricalBeam
{
	shapeFile = "zap";
	maxDamage = 10000.0;
	isTranslucent = true;
    description = "Electrical Beam";
   disableCollision = true;
};

//function to fade in electrical beam based on base power.
function PoweredElectricalBeam::onPower(%this, %power, %generator)
{
   if(%power)
	  GameBase::startFadeIn(%this);
   else
      GameBase::startFadeOut(%this);
}
      
//-----------------------------------------------------------------------
StaticShapeData Cactus1
{
	shapeFile = "cactus1";
	debrisId = defaultDebrisSmall;
	maxDamage = 0.4;
   description = "Cactus";
};
//------------------------------------------------------------------------
StaticShapeData Cactus2
{
	shapeFile = "cactus2";
	debrisId = defaultDebrisSmall;
	maxDamage = 0.4;
   description = "Cactus";
};
//------------------------------------------------------------------------
StaticShapeData Cactus3
{
	shapeFile = "cactus3";
	debrisId = defaultDebrisSmall;
	maxDamage = 0.4;
   description = "Cactus";
};

//------------------------------------------------------------------------
StaticShapeData SteamOnGrass
{
	shapeFile = "steamvent_grass";
	maxDamage = 999.0;
	isTranslucent = "True";
   description = "Steam Vent";
};

//------------------------------------------------------------------------
StaticShapeData SteamOnMud
{
	shapeFile = "steamvent_mud";
	maxDamage = 999.0;
	isTranslucent = "True";
   description = "Steam Vent";
};

//------------------------------------------------------------------------
StaticShapeData TreeShape
{
	shapeFile = "tree1";
	maxDamage = 99999.0;
	isTranslucent = "True";
   description = "Tree";
};
StaticShapeData TreeShapeTwo
{
	shapeFile = "tree2";
	maxDamage = 99999.0;
	isTranslucent = "True";
   description = "Tree";
};

StaticShapeData PhantomStrangerTree1
{
	shapeFile = "rpgtree1";
	maxDamage = 99999.0;
	isTranslucent = "True";
	description = "PhantomStranger's Tree Small";
};
StaticShapeData PhantomStrangerTree2
{
	shapeFile = "bigrpgtree1";
	maxDamage = 99999.0;
	isTranslucent = "True";
	description = "PhantomStranger's Tree Medium";
};
StaticShapeData PhantomStrangerTree3
{
	shapeFile = "REALLYbigrpgtree";
	maxDamage = 99999.0;
	isTranslucent = "True";
	description = "PhantomStranger's Tree Large";
};

//------------------------------------------------------------------------
StaticShapeData SteamOnGrass2
{
	shapeFile = "steamvent2_grass";
	maxDamage = 999.0;
	isTranslucent = "True";
};

//------------------------------------------------------------------------
StaticShapeData SteamOnMud2
{
	shapeFile = "steamvent2_mud";
	maxDamage = 999.0;
	isTranslucent = "True";
   description = "Steam Vent";
};
//------------------------------------------------------------------------
StaticShapeData PlantOne
{
	shapeFile = "plant1";
	debrisId = defaultDebrisSmall;
	maxDamage = 99999.0;
   description = "Plant";
};

//------------------------------------------------------------------------
StaticShapeData PlantTwo
{
	shapeFile = "plant2";
	debrisId = defaultDebrisSmall;
	maxDamage = 99999.0;
   description = "Plant";
};


//platforms
StaticShapeData DepPlatSmallHorz
{
	shapeFile = "elevator_4x4";
	debrisId = defaultDebrisSmall;
	maxDamage = 9999;
	visibleToSensor = false;
	isTranslucent = true;
   	description = "SmallPlatform";
};
function DepPlatSmallHorz::onDestroyed(%this)
{
	StaticShape::onDestroyed(%this);
}
StaticShapeData DepPlatMediumHorz
{
	shapeFile = "elevator6x6thin";
	debrisId = defaultDebrisSmall;
	maxDamage = 9999;
	visibleToSensor = false;
	isTranslucent = true;
   	description = "MediumPlatform";
};
function DepPlatMediumHorz::onDestroyed(%this)
{
	StaticShape::onDestroyed(%this);
}
StaticShapeData DepPlatLargeHorz
{
	shapeFile = "elevator_9x9";
	debrisId = defaultDebrisSmall;
	maxDamage = 9999;
	visibleToSensor = false;
	isTranslucent = true;
   	description = "LargePlatform";
};
function DepPlatLargeHorz::onDestroyed(%this)
{
	StaticShape::onDestroyed(%this);
}

StaticShapeData DepPlatSmallVert
{
	shapeFile = "elevator_4x4";
	debrisId = defaultDebrisSmall;
	maxDamage = 9999;
	visibleToSensor = false;
	isTranslucent = true;
   	description = "SmallPlatform";
};
function DepPlatSmallVert::onDestroyed(%this)
{
	StaticShape::onDestroyed(%this);
}
StaticShapeData DepPlatMediumVert
{
	shapeFile = "elevator6x6thin";
	debrisId = defaultDebrisSmall;
	maxDamage = 9999;
	visibleToSensor = false;
	isTranslucent = true;
   	description = "MediumPlatform";
};
function DepPlatMediumVert::onDestroyed(%this)
{
	StaticShape::onDestroyed(%this);
}
StaticShapeData DepPlatLargeVert
{
	shapeFile = "elevator_9x9";
	debrisId = defaultDebrisSmall;
	maxDamage = 9999;
	visibleToSensor = false;
	isTranslucent = true;
   	description = "LargePlatform";
};
function DepPlatLargeVert::onDestroyed(%this)
{
	StaticShape::onDestroyed(%this);
}


//Deployable fade-out forcefield
StaticShapeData StaticDoorForceField
{
	shapeFile = "ForceField";
	debrisId = defaultDebrisSmall;
	maxDamage = 10000.0;
	visibleToSensor = false;
	isTranslucent = true;
   	description = "Door Force Field";
};
function StaticDoorForceField::onCollision(%this, %object)
{
	%owner = $owner[%this];
	%clientId = Player::getClient(%object);
	%name = Client::getName(%clientId);

	// review #24: $grouplist[%owner] is NEVER populated anywhere (only ever read -
	// here and in the dead Vehicle.cs copy), so the group check always failed and
	// ONLY the owner themselves (%name == %owner) could pass their own field. Use
	// the real per-player grouplist (fetchData, keyed by the owner's clientId),
	// matching every other group feature (Admin.cs/comchat.cs/spells.cs/sleep.cs).
	%ownerCl = NEWgetClientByName(%owner);
	// Estate.cs: if this force field belongs to an estate, its permitted members pass too.
	%eid = $EstateOf[%this];
	%isEstateMember = (%eid != "" && %eid != 0 && IsInCommaList($Estate::Members[%eid], %name));
        if(%name == %owner || %isEstateMember || (%ownerCl != -1 && IsInCommaList(fetchData(%ownerCl, "grouplist"), %name)))
	{
		echo(%this);
		if($recreatingfField[%this] == "")
		{
			Client::sendMessage(%clientId,0,"Access granted.");

			%pos = GameBase::getPosition(%this);
			%rot = GameBase::getRotation(%this);
			playSound(ForceFieldClose, %pos);
			//refreshing $owner in case the new force field has a different ID
			%backupowner = $owner[%this];
			$owner[%this] = "";
			// ESTATE FIX 2026-07-17: the recreate spawns a NEW object id, so the
			// estate binding must ride along - otherwise permitted members lock out
			// after the first open ($EstateOf lost with the deleted id) and the
			// registry slot map keeps pointing at this dead id forever (demolish/
			// decay then can't delete the live door and leaks it). Find the flat
			// slot now; RecreateForceField re-binds and re-points it.
			%eslot = "";
			if(%eid != "" && %eid != 0)
			{
				for(%k = 1; %k <= $Estate::SCount; %k++)
					if($EstateRt::SObjId[%k] == %this) { %eslot = %k; break; }
				$EstateOf[%this] = ""; // ids recycle - never leave a stale binding
			}
			deleteObject(%this);

			$recreatingfField[%this] = 1;
			schedule("RecreateForceField(\"" @ %this @ "\", \"" @ %pos @ "\", \"" @ %rot @ "\", \"" @ %backupowner @ "\", \"" @ %eid @ "\", \"" @ %eslot @ "\");", 3);
		}
	}
	else
	{
		Client::sendMessage(%clientId,0,"Access denied.~wError_Message.wav");
	}
}
function RecreateForceField(%this, %pos, %rot, %backupowner, %eid, %eslot)
{
	// ESTATE FIX 2026-07-17: %eid/%eslot arrive only for estate doors (empty for
	// legacy/deployed fields - behavior unchanged for those). If the estate's
	// registry slot was freed or re-used while the door was open (demolish/decay/
	// abandon inside the 3s window), do NOT resurrect an orphan - the estate no
	// longer owns a door here. SObjId[%eslot] == %this is the "slot untouched
	// since the door opened" invariant.
	if(%eid != "" && %eid != 0)
	{
		if($Estate::SEstate[%eslot] != %eid || $Estate::SType[%eslot] != "forcefield" || $EstateRt::SObjId[%eslot] != %this)
		{
			$EstateRt::ObjInUse--; // FreeStructSlot saw a dead id and skipped its decrement
			$recreatingfField[%this] = "";
			return;
		}
	}
	%fField = newObject("","StaticShape",StaticDoorForceField,true);
	$owner[%fField] = %backupowner;
	addToSet("MissionCleanup", %fField);
	GameBase::setPosition(%fField, %pos);
	GameBase::setRotation(%fField, %rot);
	playSound(ForceFieldOpen,%pos);
	if(%eid != "" && %eid != 0)
	{
		$EstateOf[%fField] = %eid;           // members keep passing after reopen
		$EstateRt::SObjId[%eslot] = %fField; // demolish/decay finds the CURRENT door
	}
	$recreatingfField[%this] = "";
}
function StaticDoorForceField::onDestroyed(%this)
{
	$owner[%this] = "";
	StaticShape::onDestroyed(%this);
}	
//**

StaticShapeData SoundPoint
{
	shapeFile = "bullet";
	maxDamage = 999.0;
	isTranslucent = "True";
};

StaticShapeData WavyWater1
{
	shapeFile = "forcefield";
	maxDamage = 999.0;
	isTranslucent = "True";
};
StaticShapeData WavyWater2
{
	shapeFile = "forcefield2";
	maxDamage = 999.0;
	isTranslucent = "True";
};
StaticShapeData WavyWater4
{
	shapeFile = "forcefield4";
	maxDamage = 999.0;
	isTranslucent = "True";
};
StaticShapeData WavyWater16
{
	shapeFile = "forcefield16";
	maxDamage = 999.0;
	isTranslucent = "True";
};
