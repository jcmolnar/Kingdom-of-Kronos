//----------------------------------------------------------------------------
// MINE DYNAMIC DATA

MineData AntipersonelMine
{
	className = "Mine";
   description = "Antipersonel Mine";
   shapeFile = "mine";
   shadowDetailMask = 4;
   explosionId = mineExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	baseDamageType = $ExplosionDamageType;
	kickBackStrength = 150;
	triggerRadius = 2.5;
	maxDamage = 2.5;
	shadowDetailMask = 0;
	destroyDamage = 1.0;
	damageLevel = {1.0, 1.0};
};

function AntipersonelMine::onAdd(%this)
{
	%this.damage = 0;
	AntipersonelMine::deployCheck(%this);
}

function AntipersonelMine::onCollision(%this,%object)
{
	%type = getObjectType(%object);
	%data = GameBase::getDataName(%this);
	if ((%type == "Player" || %data == AntipersonelMine || %data == Vehicle || %type == "Moveable") &&
			GameBase::isActive(%this)) 
		GameBase::setDamageLevel(%this, %data.maxDamage);
}

function AntipersonelMine::deployCheck(%this)
{
	if (GameBase::isAtRest(%this)) {
		GameBase::playSequence(%this,1,"deploy");
	 	GameBase::setActive(%this,true);
		%set = newObject("set",SimSet);
		if(1 != containerBoxFillSet(%set,$MineObjectType,GameBase::getPosition(%this),1,1,1,0)) {
			%data = GameBase::getDataName(%this);
			GameBase::setDamageLevel(%this, %data.maxDamage);
		}
		deleteObject(%set);
	}
	else 
		schedule("AntipersonelMine::deployCheck(" @ %this @ ");", 3, %this);
}	

function AntipersonelMine::onDestroyed(%this)
{
	$TeamItemCount[GameBase::getTeam(%this) @ "mineammo"]--;
}

function AntipersonelMine::onDamage(%this,%type,%value,%pos,%vec,%mom,%object)
{
   if (%type == $MineDamageType)
      %value = %value * 0.25;

	%data = GameBase::getDataName(%this);
	if((%data.maxDamage/1.5) < %this.damage+%value) 
		GameBase::setDamageLevel(%this, %data.maxDamage);
	else 
		%this.damage += %value;
}

//----------------------------------------------------------------------------

MineData Bomb1
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = mortarExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb1::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb2
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = mineExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb2::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb3
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = grenadeExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb3::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb4
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = Shockwave;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb4::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb5
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = LargeShockwave;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb5::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb6
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = rocketExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb6::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb7
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = energyExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb7::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb8
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = blasterExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb8::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb9
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = plasmaExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb9::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb10
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = turretExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb10::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb11
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = bulletExp0;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb11::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}


MineData Bomb12
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = debrisExpSmall;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb12::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb13
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = debrisExpMedium;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb13::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb14
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = debrisExpLarge;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb14::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb15
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = flashExpSmall;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb15::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb16
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = flashExpMedium;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb16::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb17
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = flashExpLarge;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb17::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb18
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = whiteExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb18::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb19
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = WhiteShockwave;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb19::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb20
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = IonExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb20::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb21
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = IonExp2;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb21::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb22
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = IonShockwave;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb22::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb23
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = IonShockwave2;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb23::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb24
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = PBShockWave;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb24::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb25
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = LitBoltExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb25::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb26
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = TimeShockwave;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb26::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb27
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = turretSlowExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb27::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}
MineData Bomb28
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "smoke";
	shadowDetailMask = 4;
	explosionId = acidExp;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb28::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Bomb29
{
	mass = 0.3;
	drag = 1.0;
	density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
	description = "Handgrenade";
	shapeFile = "zap_5";
	shadowDetailMask = 4;
	explosionId = bulletExp0;
	explosionRadius = 10.0;
	damageValue = 1.0;
	damageType = $NullDamageType;
	kickBackStrength = 0;
	triggerRadius = 0.5;
	maxDamage = 1.0;
};
function Bomb29::onAdd(%this)
{
	schedule("Mine::Detonate(" @ %this @ ");", 0.2, %this);
}

MineData Handgrenade
{
   mass = 0.3;
   drag = 1.0;
   density = 2.0;
	elasticity = 0.15;
	friction = 1.0;
	className = "Handgrenade";
   description = "Handgrenade";
   shapeFile = "grenade";
   shadowDetailMask = 4;
   explosionId = grenadeExp;
	explosionRadius = 10.0;
	damageValue = 0.5;
	baseDamageType = $ShrapnelDamageType;
	kickBackStrength = 100;
	triggerRadius = 0.5;
	maxDamage = 50;
};

function Handgrenade::onAdd(%this)
{
	%data = GameBase::getDataName(%this);
	schedule("Mine::Detonate(" @ %this @ ");",2.0,%this);
}

function Mine::onDamage(%this,%type,%value,%pos,%vec,%mom,%object)
{
   if (%type == $MineDamageType)
      %value = %value * 0.25;

	%damageLevel = GameBase::getDamageLevel(%this);
	GameBase::setDamageLevel(%this,%damageLevel + %value);
}

function Mine::Detonate(%this)
{
	%data = GameBase::getDataName(%this);
	GameBase::setDamageLevel(%this, %data.maxDamage);
}

