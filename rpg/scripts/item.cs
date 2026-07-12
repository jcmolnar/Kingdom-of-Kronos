//----------------------------------------------------------------------------

$ItemFavoritesKey = "KronosRPG";  // Change this if you add new items
                         	// and don't want to mess up everyone's
                         	// favorites - just put in something
                         	// that uniquely describes your new stuff.

//----------------------------------------------------------------------------

$ItemPopTime = 30;

$ToolSlot=0;
$WeaponSlot=0;
$BeltSlot=1;
$FlagSlot=2;
$DefaultSlot=3;

// Limit on number of special Items you can buy
$TeamItemMax[DepBasePack] = 200;

// Global object damage skins (staticShapes Turrets Stations Sensors)
DamageSkinData objectDamageSkins
{
   bmpName[0] = "dobj1_object";
   bmpName[1] = "dobj2_object";
   bmpName[2] = "dobj3_object";
   bmpName[3] = "dobj4_object";
   bmpName[4] = "dobj5_object";
   bmpName[5] = "dobj6_object";
   bmpName[6] = "dobj7_object";
   bmpName[7] = "dobj8_object";
   bmpName[8] = "dobj9_object";
   bmpName[9] = "dobj10_object";
};





//----------------------------------------------------------------------------
// Default Item object methods
//----------------------------------------------------------------------------

$PickupSound[Ammo] = "SoundPickupAmmo";
$PickupSound[Weapon] = "SoundPickupWeapon";
$PickupSound[Backpack] = "SoundPickupBackpack";
$PickupSound[Repair] = "SoundPickupHealth";
$PickupSound[Accessory] = "SoundPickupBackpack";

function Item::playPickupSound(%this)
{
	dbecho($dbechoMode, "Item::playPickupSound(" @ %this @ ")");

	%item = Item::getItemData(%this);
	%sound = $PickupSound[%item.className];
	if (%sound != "")  
		playSound(%sound,GameBase::getPosition(%this));
	else {
		// Generic item sound
		playSound(SoundPickupItem,GameBase::getPosition(%this));
	}
}	

function Item::respawn(%this)
{
	dbecho($dbechoMode, "Item::respawn(" @ %this @ ")");

	// If the item is rotating we respawn it,
	if(Item::isRotating(%this)) {
		Item::hide(%this,True);
		schedule("Item::hide(" @ %this @ ",false); GameBase::startFadeIn(" @ %this @ ");",$ItemRespawnTime,%this);
	}
	else { 
		deleteObject(%this);
	}
}	

function Item::onAdd(%this)
{
}



//----------------------------------------------------------------------------
// Default Inventory methods


function Item::pop(%item)
{
	dbecho($dbechoMode, "Item::pop(" @ %item @ ")");

 	GameBase::startFadeOut(%item);
	// CRITICAL: stamp a per-pop token so the deferred delete can confirm this is still the SAME item.
	// Object IDs get recycled within the 2.5s window; a recycled Player OR a different Item will not
	// carry this token, so SafeDeleteItem can refuse to delete the wrong (recycled) object.
	%popToken = %item @ "_" @ getSimTime();
	%item.popToken = %popToken;
	schedule("SafeDeleteItem(" @ %item @ ", \"" @ %popToken @ "\");", 2.5, %item);
}

// CRITICAL SAFEGUARD: Safe item deletion that verifies object type before deleting
// This prevents accidental Player deletion if object ID was recycled
function SafeDeleteItem(%obj, %token)
{
	if(!isObject(%obj))
		return; // Object already deleted

	// PRIMARY GUARD: token match. If %obj.popToken no longer equals the token we scheduled with, this
	// object ID was recycled to a DIFFERENT object since Item::pop() (a Player, or another Item) and we
	// must NOT delete it. Catches every recycle case, not just Player. (%token=="" => legacy caller,
	// falls through to the type checks below.)
	if(%token != "" && %obj.popToken != %token)
	{
		echo("CRITICAL SAFEGUARD: SafeDeleteItem - object " @ %obj @ " no longer matches its pop token (ID recycled, type=" @ getObjectType(%obj) @ "). Aborting delete.");
		return;
	}

	%type = getObjectType(%obj);
	if(%type == "Player")
	{
		echo("CRITICAL SAFEGUARD: SafeDeleteItem called on Player object " @ %obj @ "! Object ID was likely recycled. Aborting delete.");
		return;
	}
	
	// Additional safety: verify it's an Item type before deleting
	if(%type != "Item")
	{
		echo("SAFETY WARNING: SafeDeleteItem called on non-Item object " @ %obj @ " (Type: " @ %type @ "). Aborting.");
		return;
	}
	
	deleteObject(%obj);
}


//----------------------------------------------------------------------------
// Tools, Weapons & ammo
//----------------------------------------------------------------------------

ItemData Tool
{
	description = "Tool";
	showInventory = false;
};

function Tool::onUse(%player,%item)
{
	dbecho($dbechoMode, "Tool::onUse(" @ %player @ ", " @ %item @ ")");

	Player::mountItem(%player,%item,$ToolSlot);
}


//----------------------------------------------------------------------------

ItemData Ammo
{
	description = "Ammo";
	showInventory = false;
};

//----------------------------------------------------------------------------
// Backpacks
//----------------------------------------------------------------------------

ItemData Backpack
{				
	description = "Backpack";
	showInventory = false;
};

function Belt::onUse(%player,%item)
{
	dbecho($dbechoMode, "Belt::onUse(" @ %player @ ", " @ %item @ ")");

	if (Player::getMountedItem(%player,$BeltSlot) != %item) {
		Player::mountItem(%player,%item,$BeltSlot);
	}
	else {
		Player::trigger(%player,$BeltSlot);
	}
}

function checkDeployArea(%clientId, %pos)
{
	dbecho($dbechoMode, "checkDeployArea(" @ %clientId @ ", " @ %pos @ ")");

  	%set=newObject("set",SimSet);
	%num=containerBoxFillSet(%set,$StaticObjectType | $ItemObjectType | $SimPlayerObjectType,%pos,1,1,1,1);
	if(!%num) {
		deleteObject(%set);
		return 1;
	}
	else if(%num == 1 && getObjectType(Group::getObject(%set,0)) == "Player") { 
		%obj = Group::getObject(%set,0);	
		if(Player::getClient(%obj) == %clientId)	
			Client::sendMessage(%clientId, 0, "Unable to deploy - You're in the way");
		else
			Client::sendMessage(%clientId, 0, "Unable to deploy - Player in the way");
	}
	else
	{
		deleteObject(%set);	// review #36: this branch (multi-object, or a single non-Player object) returned WITHOUT freeing the temp SimSet - one leaked sim object per deploy attempt near clutter. The other two branches already free it.
		return 1;
	}

	//	Client::sendMessage(%clientId, 0, "Unable to deploy - Item in the way");

	deleteObject(%set);
	return 0;
		

}
//----------------------------------------------------------------------------
// Remote deploy for items

function Item::deployShape(%player,%name,%shape,%item)
{
	dbecho($dbechoMode, "Item::deployShape(" @ %player @ ", " @ %name @ ", " @ %shape @ ", " @ %item @ ")");

	%clientId = Player::getClient(%player);
	if($TeamItemCount[GameBase::getTeam(%player) @ %item] < $TeamItemMax[%item]) {
		if (GameBase::getLOSInfo(%player,3)) {
			// GetLOSInfo sets the following globals:
			// 	los::position
			// 	los::normal
			// 	los::object
			%obj = getObjectType($los::object);
			if (%obj == "SimTerrain" || %obj == "InteriorShape") {
				if (Vector::dot($los::normal,"0 0 1") > 0.7) {
					if(checkDeployArea(%clientId, $los::position)) {
						if(Zone::getType($zone[%clientId]) != "PROTECTED" || %clientId.adminLevel >= 4)
						{
							%sensor = newObject("","Sensor",%shape,true);
			 	        	   	addToSet("MissionCleanup", %sensor);
							GameBase::setTeam(%sensor,GameBase::getTeam(%player));
							GameBase::setPosition(%sensor,$los::position);
							Gamebase::setMapName(%sensor,%name);
							Client::sendMessage(%clientId, 0, %item.description @ " deployed");
							playSound(SoundPickupBackpack,$los::position);
							echo("MSG: ",%clientId," deployed a ",%name);
							return true;
						}
						else 
							Client::sendMessage(%clientId,0,"You are not allowed to deploy this item inside protected territory.");
					}
				}
				else 
					Client::sendMessage(%clientId,0,"Can only deploy on flat surfaces");
			}
			else 
				Client::sendMessage(%clientId,0,"Can only deploy on terrain or buildings");
		}
		else 
			Client::sendMessage(%clientId,0,"Deploy position out of range");
	}
	else
	 	Client::sendMessage(%clientId,0,"Deployable Item limit reached for " @ %name @ "s");
	return false;
}

//----------------------------------------------------------------------------

ItemData RepairPatch
{
	description = "Repair Patch";
	className = "Repair";
	shapeFile = "armorPatch";
	heading = "eMiscellany";
	shadowDetailMask = 4;
  	price = 1;
};

function RepairPatch::onCollision(%this,%object)
{
	dbecho($dbechoMode, "RepairPatch::onCollision(" @ %this @ ", " @ %object @ ")");

	if (getObjectType(%object) == "Player") {
		if(GameBase::getDamageLevel(%object)) {
			refreshHP(Player::getClient(%object), -0.125);
			%item = Item::getItemData(%this);
			Item::playPickupSound(%this);
			Item::respawn(%this);
		}
	}
}

function RepairPatch::onUse(%player,%item)
{
	dbecho($dbechoMode, "RepairPatch::onUse(" @ %player @ ", " @ %item @ ")");

	Player::decItemCount(%player,%item);
	refreshHP(Player::getClient(%player), -0.1);
}

ItemImageData FlagImage
{
	shapeFile = "flag";
	mountPoint = 2;
	mountOffset = { 0, 0, -0.35 };
	mountRotation = { 0, 0, 0 };

	lightType = 2;   // Pulsing
	lightRadius = 4;
	lightTime = 1.5;
	lightColor = { 1, 1, 1};
};

ItemData Flag
{
	description = "Flag";
	shapeFile = "flag";
	imageType = FlagImage;
	showInventory = false;
	shadowDetailMask = 4;
   validateShape = true;

	lightType = 2;   // Pulsing
	lightRadius = 4;
	lightTime = 1.5;
	lightColor = { 1, 1, 1 };
};