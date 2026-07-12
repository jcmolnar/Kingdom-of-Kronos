//============================================================================
// BOT AUTO-EQUIP FLAGS DOCUMENTATION
//============================================================================
// This file implements auto-equip functionality for bots (town bots and enemy bots).
// When items are given to bots via Item::giveItem(), they will automatically equip
// weapons, armor, and shields based on the following rules:
//
// WEAPONS:
//   - All bots (town bots and enemy bots) automatically equip weapons
//   - No flag required
//
// ARMOR:
//   - Town bots: Always auto-equip armor (no flag needed)
//   - Enemy bots: Only auto-equip if "AutoEquipArmor" flag is set
//
// SHIELDS:
//   - Town bots: Always auto-equip shields (no flag needed)
//   - Enemy bots: Only auto-equip if "AutoEquipShield" flag is set
//
// ACCESSORIES (Rings, Necklaces/Talismans, Belts, Back items, Boots, Head items):
//   - Town bots: Always auto-equip accessories (no flag needed)
//   - Enemy bots: Only auto-equip if "AutoEquipAccessories" flag is set
//   - Note: Accessories are separate from armor and include: Ring, Talisman, Back,
//     Boots, Head, and Belt accessory types
//
// HOW TO USE THE FLAGS:
//   To enable armor/shield/accessory auto-equip for a specific enemy bot, set the
//   flag(s) when spawning or setting up the bot:
//
//   Example in AI::setWeapons() or bot spawn function:
//     storeData(%aiId, "AutoEquipArmor", "true");        // Enable armor auto-equip
//     storeData(%aiId, "AutoEquipShield", "true");       // Enable shield auto-equip
//     storeData(%aiId, "AutoEquipAccessories", "true");  // Enable accessories auto-equip
//
//   Where to apply:
//     - remortseal.cs: When spawning seal battle bots
//     - rpgarena.cs: When spawning arena bots
//     - Any bot setup function where you want specific enemy bots to equip armor/shields
//
// DEFAULT BEHAVIOR:
//   - Enemy bots without flags: Receive items in inventory but don't auto-equip
//     (items can drop as loot when bot dies)
//   - Enemy bots with flags: Auto-equip the flagged items (armor, shield, or accessories)
//   - Town bots: Always auto-equip all items (no flags needed)
//
// NOTE: All auto-equip bypasses skill checks for bots, allowing them to equip
//       any items regardless of skill requirements.
//
// TOWN BOT LOOT PROTECTION:
//   Town bots have "NoDropLoot" flag set to "true" by default in InitTownBots() to
//   prevent them from dropping loot if a player finds a bug and can attack them.
//
//   Default behavior:
//     - Town bots: Will NOT drop loot when killed (flag set by default)
//     - To allow a specific town bot to drop loot, set flag to "false" or remove it:
//       storeData(%townbot, "NoDropLoot", "false");  // Allow town bot to drop loot
//     - Enemy bots: Always drop loot (flag has no effect on enemy bots)
//
// TOWN BOT WEAPON MOUNTING FLAGS:
//   Town bots have two flags to control weapon mounting behavior:
//
//   MountWeaponOnSpawn:
//     - If "true": Weapon will be mounted when bot spawns (default: "false")
//     - If "false" or not set: Weapon will NOT be mounted on spawn
//     - Example: storeData(%townbot, "MountWeaponOnSpawn", "true");  // Mount weapon on spawn
//
//   MountWeaponOnTalk:
//     - NOTE: talk-mounting was REMOVED - weapons only mount on collision now, so this
//       flag is effectively inert (kept for compatibility). Default is "false".
//     - If "false" or not set (the default): Weapon will NOT be mounted when talked to
//     - Example: storeData(%townbot, "MountWeaponOnTalk", "false");  // Don't mount on talk
//
//   Where to apply:
//     - In InitTownBotPostSpawn() or after bot spawn:
//       storeData(%clientId, "MountWeaponOnSpawn", "true");   // Mount on spawn
//       storeData(%clientId, "MountWeaponOnTalk", "false");  // Don't mount on talk
//     - In map file or bot setup code before InitTownBots() completes
//
//   Default behavior:
//     - MountWeaponOnSpawn: "false" (weapons not mounted on spawn)
//     - MountWeaponOnTalk: "false" (talk-mounting removed; weapons only mount on collision)
//
// TOWN BOT IDLE MESSAGE FLAG:
//   Town bots have a flag to show an idle message if 30 minutes pass without interaction:
//
//   ShowIdleMessage:
//     - If "true": Bot will show idle message if 30 minutes pass without player interaction (default: "false")
//     - If "false" or not set: Bot will not show idle message
//     - Example: storeData(%townbot, "ShowIdleMessage", "true");  // Enable idle message
//
//   Idle Message:
//     - Message shown: "My Kronos - you startled me, I must've dozed off. Sorry about that - Don't tell my boss, please? Anyways - How can I help you today?"
//     - Only shows if 30 minutes (1800 seconds) have passed since last player interaction
//     - Shows before normal bot greeting/interaction
//     - Normal bot interaction continues after idle message
//
//   Where to apply:
//     - In InitTownBotPostSpawn() or after bot spawn:
//       storeData(%clientId, "ShowIdleMessage", "true");  // Enable idle message
//     - In map file or bot setup code before InitTownBots() completes
//
//   Default behavior:
//     - ShowIdleMessage: "false" (idle message disabled by default)
//     - LastInteractionTime: Set to current time when bot spawns
//     - Updated whenever a player interacts with the bot
//============================================================================

function isPlayerBusy(%client)
{
	%state = Player::getItemState(%client, $WeaponSlot);
	if(%state == "Fire")
		return true;
	else
		return false;
}

function Item::giveItem(%player, %item, %delta, %showmsg)
{
	dbecho($dbechoMode, "Item::giveItem(" @ %player @ ", " @ %item @ ", " @ %delta @ ", " @ %showmsg @ ")");

	// Validate player object first - check for empty, -1, False, and ensure it's an object
	if(%player == "" || %player == -1 || %player == False || !isObject(%player))
	{
		// Silently return 0 for invalid objects - don't spam console with errors
		// This can happen when objects are being cleaned up during disconnect/death
		return 0;
	}

	// Get client ID - try Player::getClient first (for regular players)
	%clientId = Player::getClient(%player);
	
	// If Player::getClient returns -1 or empty, this might be a town bot (Item object)
	// Skip town bots entirely to prevent hanging - they're Item objects, not Player objects
	if(%clientId == -1 || %clientId == "")
	{
		// Silently return 0 - don't spam console with errors
		// This can happen when objects are being cleaned up during disconnect/death
		// OR when trying to give items to Item objects (town bots)
		return 0;
	}

	//i used to restrict what you could pick up here, but that sucks, so i made
	//it so you can pick up anything, but you can't EQUIP anything. (see Item::onUse)

	//also, the only reason you'd be getting a giveItem of an Equipped type is
	//by giving the client an item and pre-equipping it.

	// Check if it's a Belt item
	if(isBeltItem(%item))
	{
		Belt::GiveThisStuff(%clientId, %item, %delta, %showmsg);
		return %delta;
	}

	if(%showmsg)
		Client::sendMessage(%clientId, 0, "You received " @ %delta @ " " @ %item.description @ ".~loot");

	// Validate player object exists before modifying items
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
	{
		echo("ERROR: Item::giveItem - could not find player object for clientId " @ %clientId);
		return 0;
	}

	Player::incItemCount(%clientId, %item, %delta);

	// Auto-equip items for bots (enemy bots and town bots) - bypasses skill checks
	// Only applies to bots, players still go through normal skill checks when manually equipping
	// Town bots are now Player objects, so they can use standard player item functions
	if(isRPGAI(%clientId))
	{
		//echo("[SPAWN DEBUG] Item::giveItem(): isRPGAI=true for clientId=" @ %clientId @ ", item=" @ %item);
		// Check if this is a town bot (for armor auto-equip restriction)
		%isTownBot = false;
		for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
		{
			if(%id == %clientId)
			{
				%isTownBot = true;
				break;
			}
		}
		//echo("[SPAWN DEBUG] Item::giveItem(): isTownBot=" @ %isTownBot);

		%itemData = getItemData(%item);
		//echo("[SPAWN DEBUG] Item::giveItem(): getItemData(" @ %item @ ") returned className=" @ %itemData.className);
		
		if(%itemData != "")
		{
			// Check if item is a weapon using className or fallback to accessoryType
			%isWeapon = false;
			if(%itemData.className == "Weapon")
			{
				%isWeapon = true;
				//echo("[SPAWN DEBUG] Item::giveItem(): Weapon detected via className");
			}
			else
			{
				// Fallback: check accessoryType for weapon types (Slashing=7, Piercing/Polearm=9, Bludgeon=10)
				%accessoryType = $AccessoryVar[%item, $AccessoryType];
				//echo("[SPAWN DEBUG] Item::giveItem(): Checking accessoryType=" @ %accessoryType);
				if(%accessoryType == 7 || %accessoryType == 9 || %accessoryType == 10)
				{
					%isWeapon = true;
					//echo("[SPAWN DEBUG] Item::giveItem(): Weapon detected via accessoryType=" @ %accessoryType);
				}
			}
			
			if(%isWeapon)
			{
				// Auto-mount weapons if no weapon is currently mounted
				// Applies to all bots (town bots and enemy bots)
				%mountedWeapon = Player::getMountedItem(%clientId, $WeaponSlot);
				//echo("[SPAWN DEBUG] Item::giveItem(): Weapon detected, mountedWeapon=" @ %mountedWeapon @ ", attempting to mount " @ %item);
				if(%mountedWeapon == "" || %mountedWeapon == -1)
				{
					// No weapon mounted, automatically mount this weapon (bypasses skill checks)
					// Use %playerObj which is correctly set for both regular players and town bot Item objects
					Player::mountItem(%playerObj, %item, $WeaponSlot);
					//echo("[SPAWN DEBUG] Item::giveItem(): Called Player::mountItem() for " @ %item);
					%verifyMounted = Player::getMountedItem(%clientId, $WeaponSlot);
					//echo("[SPAWN DEBUG] Item::giveItem(): Verified mounted weapon=" @ %verifyMounted);
				}
				else
				{
					//echo("[SPAWN DEBUG] Item::giveItem(): Weapon already mounted (" @ %mountedWeapon @ "), skipping auto-mount");
				}
			}
			// Auto-equip armor if no armor is currently equipped
			// Applies to town bots (always) OR enemy bots with "AutoEquipArmor" flag set
			// Use $AccessoryVar to identify armor (same $AccessoryVar[item, $AccessoryType] pattern used elsewhere)
			// Enemy bots without the flag will drop armor but not equip it
			if(!%isWeapon && $AccessoryVar[%item, $AccessoryType] == $BodyAccessoryType)
			{
				// Check if this bot should auto-equip armor:
				// - Town bots always equip armor
				// - Enemy bots only if "AutoEquipArmor" flag is set to "true"
				%shouldEquipArmor = %isTownBot;
				if(!%shouldEquipArmor)
				{
					// Check for individual flag on enemy bot
					%shouldEquipArmor = (fetchData(%clientId, "AutoEquipArmor") == "true");
				}

				if(%shouldEquipArmor)
				{
					%currentArmor = Player::getArmor(%clientId);
					if(%currentArmor == "" || %currentArmor == -1)
					{
						// No armor equipped, automatically equip this armor (bypasses skill checks)
						// Armor uses slot 1 and requires the equipped version (with "0" suffix)
						// Check if equipped version exists, otherwise use base item
					%equippedArmor = %item @ "0";
					%equippedData = getItemData(%equippedArmor);
					if(%equippedData != "")
						Player::mountItem(%playerObj, %equippedArmor, 1);
					else
						Player::mountItem(%playerObj, %item, 1);
					}
				}
			}
			// Auto-mount shields if no shield is currently mounted
			// Applies to town bots (always) OR enemy bots with "AutoEquipShield" flag set
			// Use $AccessoryVar to identify shields (same $AccessoryVar[item, $AccessoryType] pattern used elsewhere)
			// Enemy bots without the flag will drop shields but not equip them
			if(!%isWeapon && $AccessoryVar[%item, $AccessoryType] == $ShieldAccessoryType)
			{
				// Check if this bot should auto-equip shield:
				// - Town bots always equip shields
				// - Enemy bots only if "AutoEquipShield" flag is set to "true"
				%shouldEquipShield = %isTownBot;
				if(!%shouldEquipShield)
				{
					// Check for individual flag on enemy bot
					%shouldEquipShield = (fetchData(%clientId, "AutoEquipShield") == "true");
				}

				if(%shouldEquipShield)
				{
					%mountedShield = Player::getMountedItem(%clientId, 2);
					if(%mountedShield == "" || %mountedShield == -1)
					{
						// No shield mounted, automatically mount this shield (slot 2, bypasses skill checks)
						Player::mountItem(%playerObj, %item, 2);
					}
				}
			}
			// Auto-equip accessories (rings, necklaces/talismans, belts, back items, boots, head items)
			// Applies to town bots (always) OR enemy bots with "AutoEquipAccessories" flag set
			// Accessories include: Ring, Talisman, Back, Boots, Head, Belt accessory types
			// Enemy bots without the flag will drop accessories but not equip them
			if(!%isWeapon && %itemData.className == "Accessory" && $AccessoryVar[%item, $AccessoryType] != $BodyAccessoryType && $AccessoryVar[%item, $AccessoryType] != $ShieldAccessoryType)
			{
				// Check if this bot should auto-equip accessories:
				// - Town bots always equip accessories
				// - Enemy bots only if "AutoEquipAccessories" flag is set to "true"
				%shouldEquipAccessory = %isTownBot;
				if(!%shouldEquipAccessory)
				{
					// Check for individual flag on enemy bot
					%shouldEquipAccessory = (fetchData(%clientId, "AutoEquipAccessories") == "true");
				}

				if(%shouldEquipAccessory)
				{
					// Accessories use slot 1 and require the equipped version (with "0" suffix)
					// Check if equipped version exists, otherwise use base item
					%equippedAccessory = %item @ "0";
					%equippedData = getItemData(%equippedAccessory);
					if(%equippedData != "")
						Player::mountItem(%playerObj, %equippedAccessory, 1);
					else
						Player::mountItem(%playerObj, %item, 1);
				}
			}
		}
	}

	return %delta;
}

function Item::onCollision(%this,%object)
{
	dbecho($dbechoMode, "Item::onCollision(" @ %this @ ", " @ %object @ ")");

	// Validate object first - check for empty, -1, False, and ensure it's an object
	if(%object == "" || %object == -1 || %object == False || !isObject(%object))
		return;

	if(getObjectType(%object) != "Player")
		return;

	%clientId = Player::getClient(%object);

	// Validate client ID before proceeding (player might be disconnecting)
	if(%clientId == -1 || %clientId == "")
		return;

	// Town NPCs never pick up ANY dropped item (equipment, lootbags, projectiles):
	// they wander through the town drop piles (merchant/bank areas) and would
	// otherwise vacuum up player gear. $BotType cache is the O(1) fast path;
	// the isTownBot() fallback only runs for AI clients the cache missed, so
	// real players never pay for its isFile/registry checks.
	if($BotType[%clientId] == "town")
		return;
	if($BotType[%clientId] == "" && Player::isAiControlled(%clientId) && isTownBot(%clientId))
		return;

	%armor = Player::getArmor(%clientId);

	if(!IsDead(%clientId))
      {
		%time = getIntegerTime(true) >> 5;
		if(%time - %clientId.lastItemPickupTime <= 0.1)
			return 0;

		%clientId.lastItemPickupTime = %time;

		%item = Item::getItemData(%this);

            if(%item == "Lootbag")
            {
			%msg = "";

			%ownerName = GetWord($loot[%this], 0);
			%namelist = GetWord($loot[%this], 1);
			if($loot[%this] == "")
				%msg = "You found an empty backpack.";
			else
			{
				// CRITICAL: Enemy bots should NEVER pick up lootbags
				// This prevents: 1) overweight debuffs from accumulating coins, 2) item hoarding
				// Instead, a periodic function aggregates nearby lootbags for players
				if(isRPGAI(%clientId) || Player::isAiControlled(%clientId))
				{
					%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
					{
						// This is an enemy bot - do not allow lootbag pickup
						return;
					}
				}
				
				if(IsInCommaList(%namelist, Client::getName(%clientId)) || %namelist == "*")
				{
					if(String::ICompare(%ownerName, Client::getName(%clientId)) == 0)
						%msg = "You found one of your backpacks.";
					else if(%ownerName == "*")
						%msg = "You found a backpack.";
					else
						%msg = "You found one of " @ %ownerName @ "'s backpacks.";
				}
			}

			if(%msg != "")
			{
				%newloot = String::getSubStr($loot[%this], String::len(%ownerName)+String::len(%namelist)+2, 99999);

				Client::sendMessage(%clientId, 0, %msg);
				
				// DEBUG: Log when enemy bots pick up lootbags
				if(isRPGAI(%clientId))
				{
					%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
					}
				}
				
				// CRITICAL: Set flag to indicate this is a lootbag pickup (not spawn equipment)
				// This allows GiveThisStuff to properly distinguish between spawn equipment and lootbag items
				// Lootbag items should always be given directly (no percentage roll) and should always drop when bot dies
				storeData(%clientId, "IsLootbagPickup", "true");
				GiveThisStuff(%clientId, %newloot, True);
				// Clear the flag immediately after processing
				storeData(%clientId, "IsLootbagPickup", "");
				
				// DEBUG: Log belt items after pickup
				if(isRPGAI(%clientId))
				{
					%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
					{
						%botName = fetchData(%clientId, "BotInfoAiName");
						if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
					}
				}
				
				// Clean up lootbag data BEFORE saving world, so SaveWorldDeployables() won't save it
				if(%this.tag != "")
				{
					$tagToObjectId[%this.tag] = "";
					$SpawnPackList = RemoveFromCommaList($SpawnPackList, %this.tag);
				}
				Item::playPickupSound(%this);
				$loot[%this] = "";

				if(%ownerName != "*")
				{
					%ownerId = NEWgetClientByName(%ownerName);
					storeData(%ownerId, "lootbaglist", RemoveFromCommaList(fetchData(%ownerId, "lootbaglist"), %this));
				}

				//event stuff
				%i = GetEventCommandIndex(%this, "onpickup");
				if(%i != -1)
				{
					%name = GetWord($EventCommand[%this, %i], 0);
					%type = GetWord($EventCommand[%this, %i], 1);
					%cl = NEWgetClientByName(%name);
					if(%cl == -1)
						%cl = 2048;

					%cmd = String::NEWgetSubStr($EventCommand[%this, %i], String::findSubStr($EventCommand[%this, %i], ">")+1, 99999);
					%pcmd = ParseBlockData(%cmd, %clientId, "");
					$EventCommand[%this, %i] = "";
					remoteSay(%cl, 0, %pcmd, %name);
				}
				if(GameBase::getMapName(%this) == "GamblePack")
				{
					%bot = getWord($GambleBot,1);
					if(getWord(%newloot,1) > 0)
						AI::sayLater(%clientId,%bot,"You... win! Great job! If you'd care for another round, by all means, speak to me.",true);
					else
						AI::sayLater(%clientId,%bot,"Oh, it looks like you lost. Better luck next time, eh? If you'd care for another round, by all means, speak to me.",true);
					$GambleBot = "";
					%list = $GamblePackList;
					for(%i=0;%i<6;%i++)
					{
						%pack = getWord(%list,%i);
						if(%pack)
						{
							if(%pack != %this && GameBase::getMapName(%pack) == "GamblePack")
								deleteObject(%pack);
						}
					}
					$GamblePackList = "";
				}
				
				// Delete the lootbag object BEFORE saving world, so SaveWorldDeployables() won't find it
				deleteObject(%this);
				ClearEvents(%this);
				
				// Save character and world immediately after picking up a player-owned lootbag
				// This ensures the player's new items are saved and the lootbag is removed from worldsave
				// in case of server crash
				// Save for player-owned lootbags (owner name is not "*" and matches the player picking it up)
				// OR if namelist is not "*" (original player lootbag format)
				// NOTE: Lootbag must be deleted BEFORE SaveWorld() so SaveWorldDeployables() won't save it
				%isPlayerOwned = (%ownerName != "*" && String::ICompare(%ownerName, Client::getName(%clientId)) == 0) || %namelist != "*";
				if(%isPlayerOwned)
				{
					SaveCharacter(%clientId);
					RequestWorldSave("lootbag_pickup", 0, "deployables");
				}
				
				// Schedule a worldsave to remove this lootbag from the save file
				// Delay by 30 seconds to keep the worldsave current while limiting spam
				// Only save deployables (lootbags), not house objectives or seal values
				if($LootbagSaveWorldScheduled == "")
				{
					$LootbagSaveWorldScheduled = true;
					// Check if SaveWorld is about to run soon (within 2 minutes)
					%timeUntilSaveWorld = ($SaveWorldFreq - ($ticker[1] * 5)) * 5; // ticker increments every 5 seconds
					// For frequent syncing, use a short fixed delay; ignore overlap optimization
					%delay = 30;
					schedule("RequestWorldSave(\"lootbag_sync\", 0, \"deployables\"); $LootbagSaveWorldScheduled = \"\";", %delay);
				}
			}
			else
			{
				if(%ownerName == "*")
					Client::sendMessage(%clientId, $MsgRed, "You do not have the right to take this backpack.");
				else
					Client::sendMessage(%clientId, $MsgRed, "You do not have the right to take " @ %ownerName @ "'s backpack.");
			}
            }
            else if(%item.className == "Projectile")
            {
			%damagedClient = %clientId;
			%shooterClient = %this.owner;
			if(%shooterClient != "")
			{
				%vec = Vector::getDistance("0 0 0", Item::getVelocity(%this));
				if(%vec == 0 && $ProjectileDoubleCheck[%this])
					%vec = 3.0;
			}
			else
				%vec = 0;	//don't let thrown projectiles damage!

			$ProjectileDoubleCheck[%this] = "";

			if(%vec >= 2.5)
			{
				GameBase::virtual(%object, "onDamage", $DamageType[%item], 1.0, "0 0 0", "0 0 0", "0 0 0", "torso", %this.weapon, %shooterClient, %item);
			}
			else
			{
				if(Item::giveItem(%object, %item, %this.delta, True))
				{
					Item::playPickupSound(%this);
					RefreshAll(%clientId);
				}
			}

			deleteObject(%this);
		}
            else if(%item.className == "Accessory" || $LoreItem[%item] == True)
            {
			if(Item::giveItem(%object, %item, 1, True))
			{
				Item::playPickupSound(%this);
				RefreshAll(%clientId);
				deleteObject(%this);
			}
		}
		else if(%item.className == "TownBot")
		{
			//do nothing.
		}
            else
            {
            	//%count = Player::getItemCount(%object,%item);
            	if(Item::giveItem(%object, %item, %this.delta, True))
                  {
                  	Item::playPickupSound(%this);
				RefreshAll(%clientId);
                        Item::respawn(%this);
			}
		}
	}
}

function Item::onMount(%player,%item)
{
}

function Item::onUnmount(%player,%item)
{
}

function Item::onUse(%player,%item)
{
	dbecho($dbechoMode, "Item::onUse(" @ %player @ ", " @ %item @ ")");

	%clientId = Player::getClient(%player);

	if(!IsDead(%clientId))
	{
		//this is how you toggle back and forth from equipped to carrying.
		if(%item.className == Accessory)
		{
			// Check for robe talent requirements (must unlock talent before equipping)
			if(%item == "JudgementRobe" && !Ascension::HasTalent(%clientId, "JudgementRobeTalent"))
			{
				Client::sendMessage(%clientId, $MsgRed, "You must unlock the Judgement Robe talent first! Visit an Ascension trainer.");
				return;
			}
			if(%item == "StormRobe" && !Ascension::HasTalent(%clientId, "StormRobeTalent"))
			{
				Client::sendMessage(%clientId, $MsgRed, "You must unlock the Storm Robe talent first! Visit an Ascension trainer.");
				return;
			}
			if(%item == "VoidRobe" && !Ascension::HasTalent(%clientId, "VoidRobeTalent"))
			{
				Client::sendMessage(%clientId, $MsgRed, "You must unlock the Void Robe talent first! Visit an Ascension trainer.");
				return;
			}

			%cnt = 0;
			%max = getNumItems();
			for(%i = 0; %i < %max; %i++)
			{
				%checkItem = getItemData(%i);
				if(%checkItem.className == Equipped && GetAccessoryVar(%checkItem, $AccessoryType) == GetAccessoryVar(%item, $AccessoryType))
					%cnt += Player::getItemCount(%player, %checkItem);
			}

			if(SkillCanUse(%clientId, %item))
			{
				if(%cnt < $maxAccessory[GetAccessoryVar(%item, $AccessoryType)])
				{
					Client::sendMessage(%clientId, $MsgBeige, "You equipped " @ %item.description @ ".");
					Player::setItemCount(%player, %item, Player::getItemCount(%player, %item)-1);
					Player::setItemCount(%player, %item @ "0", Player::getItemCount(%player, %item @ "0")+1);
				}
				else
					Client::sendMessage(%clientId, $MsgRed, "You can't equip this item because you have too many already equipped.~wC_BuySell.wav");
			}
			else
				Client::sendMessage(%clientId, $MsgRed, "You can't equip this item because you lack the necessary skills.~wC_BuySell.wav");

			if($OverrideMountPoint[%item] == "")
				Player::mountItem(%player, %item @ "0", 1, 0);
		}
		else if(%item.className == Equipped)
		{
			%o = String::getSubStr(%item, 0, String::len(%item)-1);	//remove the 0
			Client::sendMessage(%clientId, $MsgBeige, "You unequipped " @ %item.description @ ".");
			Player::setItemCount(%player, %item, Player::getItemCount(%player, %item)-1);
			Player::setItemCount(%player, %o, Player::getItemCount(%player, %o)+1);

			// Skip unmounting armor for town bots (they need to keep their armor mounted)
			%isTownBot = false;
			%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
			%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
			// Town bots have BotInfoAiName but not SpawnBotInfo (enemy bots have SpawnBotInfo)
			if(%botInfoAiName != "" && %botInfoAiName != "0" && %spawnBotInfo == "")
				%isTownBot = true;

			if($OverrideMountPoint[%item] == "" && !%isTownBot)
				Player::unMountItem(%player, 1);
		}
		else
		{
			RPGmountItem(%player, %item, $DefaultSlot);
		}

		refreshHP(%clientId, 0);
		refreshMANA(%clientId, 0);
		RefreshAll(%clientId);
	}
}

function Item::onDrop(%player,%item)
{
	dbecho($dbechoMode, "Item::onDrop(" @ %player @ ", " @ %item @ ")");

	if($matchStarted)
	{
		if(%item.className != Armor)
		{
			if(%item.className == Projectile)
				%delta = 20;
			else
				%delta = 1;

			if(Player::getItemCount(%player, %item) < %delta)
				%delta = Player::getItemCount(%player, %item);

			if(%delta > 0)
			{
				%obj = newObject("","Item",%item,1,false);
				%obj.delta = %delta;
	 	 	  	schedule("Item::Pop(" @ %obj @ ");", $ItemPopTime, %obj);
	 	 	 	addToSet("MissionCleanup", %obj);

				if(IsDead(%player)) 
					GameBase::throw(%obj, %player, 10, true);
				else {
					GameBase::throw(%obj, %player, 15, false);
					Item::playPickupSound(%obj);
				}

				Player::decItemCount(%player,%item,%delta);
				RefreshAll(Player::getClient(%player));

				return %obj;
			}
		}
	}
}

function Ammo::onDrop(%player,%item)
{
	dbecho($dbechoMode, "Ammo::onDrop(" @ %player @ ", " @ %item @ ")");

	if($matchStarted)
	{
		if(%item.className == Ammo)
			%delta = 20;
		else
			%delta = 1;

		if(Player::getItemCount(%player, %item) < %delta)
			%delta = Player::getItemCount(%player, %item);

		if(%delta > 0)
		{
			%obj = newObject("","Item",%item,%delta,false);
			%obj.delta = %delta;
	      	schedule("Item::Pop(" @ %obj @ ");", $ItemPopTime, %obj);

      		addToSet("MissionCleanup", %obj);
			GameBase::throw(%obj,%player,20,false);
			Item::playPickupSound(%obj);
			Player::decItemCount(%player,%item,%delta);

			RefreshAll(Player::getClient(%player));
		}
	}
}	

function Item::onDeploy(%player,%item,%pos)
{
}
