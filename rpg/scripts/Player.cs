$PlayerAnim::Crouching = 25;
$PlayerAnim::DieChest = 26;
$PlayerAnim::DieHead = 27;
$PlayerAnim::DieGrabBack = 28;
$PlayerAnim::DieRightSide = 29;
$PlayerAnim::DieLeftSide = 30;
$PlayerAnim::DieLegLeft = 31;
$PlayerAnim::DieLegRight = 32;
$PlayerAnim::DieBlownBack = 33;
$PlayerAnim::DieSpin = 34;
$PlayerAnim::DieForward = 35;
$PlayerAnim::DieForwardKneel = 36;
$PlayerAnim::DieBack = 37;

// Player & Armor data block callbacks

function Player::onAdd(%this)
{
	dbecho($dbechoMode, "Player::onAdd(" @ %this @ ")");

	//reset the player's recharge rates for HP and MANA
      GameBase::setRechargeRate(%this, 0);
	GameBase::setAutoRepairRate(%this, 0);
    
    // SAFETY ARCHITECTURE: Ensure AI bots trigger Game::playerSpawned
    // AI::spawn() creates the object but might bypass standard spawn scripts
    %clientId = Player::getClient(%this);
    if(Player::isAiControlled(%clientId))
    {
        // Use schedule to allow object to fully initialize
        %currentTime = getSimTime();
        echo("[INERT DEBUG] Player::onAdd: Scheduling Game::playerSpawned(obj=" @ %this @ ", clientId=" @ %clientId @ ") in 0.1s @ " @ %currentTime);
        schedule("Game::playerSpawned(" @ %this @ ", " @ %clientId @ ", \"" @ Player::getArmor(%this) @ "\");", 0.1);
    }
}

function Player::onRemove(%this)
{
	//do nothing
	//look into this for possible SaveCharacter?
}

function Player::onNoAmmo(%player,%imageSlot,%itemType)
{
	//echo("No ammo for weapon ",%itemType.description," slot(",%imageSlot,")");
}


function radnomItems(%num, %an0, %an1, %an2, %an3, %an4, %an5, %an6)
{
	return %an[floor(getRandom() * (%num - 0.01))];
}

function Player::onCollision(%this,%object)
{
	dbecho($dbechoMode, "Player::onCollision(" @ %this @ ", " @ %object @ ")");

	// Use helper function to get client ID from Player object (handles enemy bots)
	%clientId = GetClientIdFromPlayerObject(%object);
	if(%clientId == -1 || %clientId == "")
		%clientId = %object;
	
	// Check if colliding with a town bot and mount weapon
	if(isObject(%object) && getObjectType(%object) == "Player")
	{
		// Use helper function to get client ID from Player object (handles enemy bots)
		%botClientId = GetClientIdFromPlayerObject(%object);
		if(%botClientId == -1 || %botClientId == "")
			%botClientId = %object;
		
		if(%botClientId != -1 && %botClientId != "")
		{
			// Check if the collided object is a town bot (not an enemy bot)
			// CRITICAL: Use isTownBot() to properly distinguish town bots from enemy bots
			%botAiName = fetchData(%botClientId, "BotInfoAiName");
			if(%botAiName != "" && %botAiName != -1 && %botAiName != "0" && isTownBot(%botClientId))
			{
				// Check if %this is a real player (not AI controlled)
				%playerClientId = GetClientIdFromPlayerObject(%this);
				if(%playerClientId == -1 || %playerClientId == "")
					%playerClientId = %this;
				
				if(%playerClientId != -1 && %playerClientId != "" && !Player::isAiControlled(%playerClientId))
				{
					// Mount weapon for town bot on collision
					schedule("MountTownBotWeapon(" @ %botClientId @ ");", 0.1);
					// Make town bot face the player on collision
					schedule("AI::lookAtPlayer(" @ %playerClientId @ ", " @ %botClientId @ ");", 0.1);

					// Determine Bot Type and Trigger Menus if applicable
					%botType = %botAiName;
					// Remove trailing digits / "TownBot_" prefix logic is complex, simpler to just check substring
					// The %botAiName here is raw from BotInfoAiName e.g. "TownBot_Banker1"
					
					// Strip "TownBot_" if present
					// Universal Greeting for ALL Town Bots
					// Check cooldown to prevent spam (5 seconds)
					%simTime = getSimTime();
					if(%botClientId.lastGreetingTime == "" || (%simTime - %botClientId.lastGreetingTime) > 5.0)
					{
						// Update cooldown
						%botClientId.lastGreetingTime = %simTime;
						
						// Determine voice gender
						// ALERT: We always randomize the voice (1-5) based on armor to ensure variety in town
						// This ignores specific bot voice settings in favor of ambient crowd variety
						%botArmor = Player::getArmor(%botClientId);
						%rand = floor(getRandom() * 5) + 1; // Random 1-5
						
						%voice = "male" @ %rand; // Default to male 1-5
						if(String::findSubStr(%botArmor, "female") != -1)
							%voice = "female" @ %rand; // Female 1-5
						
						// Store consistent voice for this session (so Farewell matches Greeting)
						%botClientId.sessionVoice = %voice;
						
						Client::sendMessage(%playerClientId, 0, "~w" @ %voice @ ".whello.wav");
					}
					
					// Specific Interaction Menus
					if(String::findSubStr(%botType, "Banker") != -1 || String::findSubStr(%botType, "banker") != -1)
					{
						// HUD clients get the modern bank storage UI (player
						// equipment <-> bank storage + coin all-in/out); vanilla
						// clients keep the stock banker menu.
						if(%playerClientId.hasKronosHUD)
							KronosBank_Open(%playerClientId, %botClientId);
						else
							SetupBankDefault(%playerClientId, %botClientId);
					}
					else if(String::findSubStr(%botType, "Merchant") != -1 || String::findSubStr(%botType, "merchant") != -1)
					{
						// Get shop indices from bot info
						%shopBotName = %botAiName;
						if(String::findSubStr(%shopBotName, "TownBot_") == 0)
							%shopBotName = String::getSubStr(%shopBotName, 8, 999);
						%shopIndices = $BotInfo[%shopBotName, SHOP];
						
						// Use Belt::Shop for unified menu (Standard Shop, Buy Accessories, Sell)
						Belt::Shop(%playerClientId, %botClientId, %shopIndices);
					}
					else if(String::findSubStr(%botType, "Ascension") != -1 || String::findSubStr(%botType, "ascension") != -1)
					{
						SetupAscensionShop(%playerClientId, %botClientId, 0);
					}
					// Generic NPC (not banker/merchant/ascension): HUD clients
					// get the modern dialogue window; vanilla clients keep the
					// stock behavior (just the greeting sound above).
					else if(%playerClientId.hasKronosHUD)
					{
						KronosNPC_Open(%playerClientId, %botClientId);
					}
				}
			}
		}
	}
}

function Player::getHeatFactor(%this)
{
	dbecho($dbechoMode, "Player::getHeatFactor(" @ %this @ ")");

        // Hack to avoid turret turret not tracking vehicles.
        // Assumes that if we are not in the player we are
        // controlling a vechicle, which is not always correct
        // but should be OK for now.
        %clientId = Player::getClient(%this);
        if (Client::getControlObject(%clientId) != %this)
                return 1.0;

   %time = getIntegerTime(true) >> 5;
   %lastTime = Player::lastJetTime(%this) >> 10;

   if ((%lastTime + 1.5) < %time) {
      return 0.0;
   } else {
      %diff = %time - %lastTime;
      %heat = 1.0 - (%diff / 1.5);
      return %heat;
   }
}

function Player::jump(%this,%mom)
{
	dbecho($dbechoMode, "Player::jump(" @ %this @ ", " @ %mom @ ")");

   %cl = GameBase::getControlClient(%this);
   if(%cl != -1)
   {
      %vehicle = Player::getMountObject (%this);
                %this.lastMount = %vehicle;
                %this.newMountTime = getSimTime() + 3.0;
                Player::setMountObject(%this, %vehicle, 0);
                Player::setMountObject(%this, -1, 0);
                Player::applyImpulse(%pl,%mom);
                playSound (GameBase::getDataName(%this).dismountSound, GameBase::getPosition(%this));
   }
}


//----------------------------------------------------------------------------

$animNumber = 25;
function playNextAnim(%clientId)
{
	dbecho($dbechoMode, "playNextAnim(" @ %clientId @ ")");

        if($animNumber > 36)
                $animNumber = 25;
        Player::setAnimation(%clientId, $animNumber++);
}

function Client::takeControl(%clientId, %objectId)
{
	dbecho($dbechoMode, "Client::takeControl(" @ %clientId @ ", " @ %objectId @ ")");

   // remote control
   if(%objectId == -1)
   {
      //echo("objectId = " @ %objectId);
      return;
   }
   if(GameBase::getTeam(%objectId) != Client::getTeam(%clientId))
   {
      //echo(GameBase::getTeam(%objectId) @ " " @ Client::getTeam(%clientId));
      return;
   }
   if(GameBase::getControlClient(%objectId) != -1)
   {
      echo("Ctrl Client = " @ GameBase::getControlClient(%objectId));
      return;
   }
        %name = GameBase::getDataName(%objectId);
        if(%name != CameraTurret && %name != DeployableTurret)
   {
           if(!GameBase::isPowered(%objectId))
                {
              // echo("Turret " @ %objectId @ " not powered.");
              return;
                }
   }
   if(!(Client::getOwnedObject(%clientId)).CommandTag && GameBase::getDataName(%objectId) != CameraTurret &&
      !$TestCheats) {
                Client::SendMessage(%clientId,0,"Must be at a Command Station to control turrets");
                return;
   }
   if(GameBase::getDamageState(%objectId) == "Enabled") {
        Client::setControlObject(%clientId, %objectId);
        Client::setGuiMode(%clientId, $GuiModePlay);
        }
}

function remoteCmdrMountObject(%clientId, %objectIdx)
{
	dbecho($dbechoMode, "remoteCmdrMountObject(" @ %clientId @ ", " @ %objectIdx @ ")");

   Client::takeControl(%clientId, getObjectByTargetIndex(%objectIdx));
}

function checkControlUnmount(%clientId)
{
	dbecho($dbechoMode, "checkControlUnmount(" @ %clientId @ ")");

   %ownedObject = Client::getOwnedObject(%clientId);
   %ctrlObject = Client::getControlObject(%clientId);
   if(%ownedObject != %ctrlObject)
   {
      if(%ownedObject == -1 || %ctrlObject == -1)
         return;
      if(getObjectType(%ownedObject) == "Player" && Player::getMountObject(%ownedObject) == %ctrlObject)
         return;
      Client::setControlObject(%clientId, %ownedObject);
   }
}

