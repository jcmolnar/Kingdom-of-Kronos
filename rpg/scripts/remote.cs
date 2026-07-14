function remoteTrue()
{
	//for some reason this function gets called from key binds, so i created it so the console doesn't get flooded with
	//remoteTrue: unknown commands.
	return;
}

function remoteCANCEL()
{
	// Stub function to prevent console spam from client-side "CANCEL" commands
	// This is called by the client when a menu is cancelled (e.g., by pressing ESC)
	// The server doesn't need to handle this, so we just ignore it
	return;
}

function remoterawKey(%client, %key, %mod)
{
	// Stub function to prevent console spam from client-side keybinds
	// This is called by the repack client when using numpad keys or other custom keybinds
	// The server doesn't need to handle these, so we just ignore them
	return;
}

function remotePlayMode(%clientId)
{
	// Keep the Kronos shop state in sync - without this, a shop closed
	// through this path leaves kshopOpen set and the next "i" press
	// toggles a phantom panel closed instead of opening one
	if(%clientId.kshopOpen != "")
		KronosShop_Close(%clientId);

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);
	ClearCurrentShopVars(%clientId);

	if(!%clientId.guiLock)
	{
		remoteSCOM(%clientId, -1);
		Client::setGuiMode(%clientId, $GuiModePlay);
	}
}

function remoteCommandMode(%clientId)
{
	if(!(%clientId.adminLevel >= 1))
	{
		//RPG players don't need commander mode.
		return;
	}

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);
	ClearCurrentShopVars(%clientId);

	// can't switch to command mode while a server menu is up
	if(!%clientId.guiLock)
	{
		remoteSCOM(%clientId, -1);  // force the bandwidth to be full command

		Client::setGuiMode(%clientId, $GuiModeCommand);
	}
}

function remoteInventoryMode(%clientId)
{
	// HUD clients: Kronos inventory screen instead of the stock gui
	// mode (some client configs bind the inventory key here instead
	// of ToggleInventoryMode, so both entries are gated)
	if(%clientId.hasKronosHUD)
	{
		if(%clientId.kshopOpen != "")
			KronosShop_Close(%clientId);
		else if(!%clientId.guiLock && !Observer::isObserver(%clientId))
			KronosShop_Open(%clientId, "inv", "");
		return;
	}

	if(!%clientId.guiLock && !Observer::isObserver(%clientId))
	{
		remoteSCOM(%clientId, -1);
		Client::setGuiMode(%clientId, $GuiModeInventory);

		Client::clearItemShopping(%clientId);
		Client::clearItemBuying(%clientId);

		%txt = "<f1><jc>COINS: " @ fetchData(%clientId, "COINS");
		Client::setInventoryText(%clientId, %txt);
	}
}

function remoteObjectivesMode(%clientId)
{
	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);
	ClearCurrentShopVars(%clientId);

	if(!%clientId.guiLock)
	{
		remoteSCOM(%clientId, -1);
		Client::setGuiMode(%clientId, $GuiModeObjectives);
	}
}

function remoteScoresOn(%clientId)
{
	if(!%clientId.menuMode)
		Game::menuRequest(%clientId);
}

function remoteScoresOff(%clientId)
{
	Client::cancelMenu(%clientId);
}

function remoteToggleCommandMode(%clientId)
{
	if(Client::getGuiMode(%clientId) != $GuiModeCommand)
		remoteCommandMode(%clientId);
	else
		remotePlayMode(%clientId);
}

function remoteToggleInventoryMode(%clientId)
{
	// HUD clients: toggle the Kronos inventory screen instead of the
	// stock CmdInventory gui mode (vanilla flow below is unchanged)
	if(%clientId.hasKronosHUD)
	{
		if(%clientId.kshopOpen != "")
			KronosShop_Close(%clientId);
		else if(!Observer::isObserver(%clientId) && !%clientId.guiLock)
			KronosShop_Open(%clientId, "inv", "");
		return;
	}

	Client::clearItemShopping(%clientId);
	Client::clearItemBuying(%clientId);
	ClearCurrentShopVars(%clientId);

	if(Client::getGuiMode(%clientId) != $GuiModeInventory)
		remoteInventoryMode(%clientId);
	else
		remotePlayMode(%clientId);
}

function remoteToggleObjectivesMode(%clientId)
{
	if(Client::getGuiMode(%clientId) != $GuiModeObjectives)
		remoteObjectivesMode(%clientId);
	else
		remotePlayMode(%clientId);
}

function remoteConsider(%clientId)
{
	MenuViewBelt(%clientId, 1);
}

function remoteUseItem(%clientId, %type)
{
	dbecho($dbechoMode, "remoteUseItem(" @ %clientId @ ", " @ %type @ ")");

	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastWaitActionTime > $waitActionDelay)
	{
		%clientId.lastWaitActionTime = %time;

		%clientId.throwStrength = 1;

		%item = getItemData(%type);

		// Virtual belt slots (VirtualSlots.cs, Phase D): a click on a VSlot row
		// equips the mapped belt weapon instead of engine-using the inert
		// placeholder ItemData. IsSlotItem is false unless $pref::VSlotsEnabled,
		// so this is a no-op with the feature off.
		if(VSlot::IsSlotItem(%item))
		{
			VSlot::OnUseClick(%clientId, %item);
			return;
		}

		if(%item == Backpack)
		{
			%item = -1;
			remoteConsider(Player::getClient(%clientId));
		}
		else
		{
			if (%item == Weapon) 
	      	      %item = Player::getMountedItem(%clientId,$WeaponSlot);
	
			if(%item != -1)
			{
				Player::useItem(%clientId, %item);
			}
		}
	}
}

function remoteThrowItem(%clientId,%type,%strength)
{
	//echo("Throw item: " @ %type @ " " @ %strength);
	%item = getItemData(%type);
	if (%item == Grenade || %item == MineAmmo) {
		if (%strength < 0)
			%strength = 0;
		else
			if (%strength > 100)
				%strength = 100;
		%clientId.throwStrength = 0.3 + 0.7 * (%strength / 100);
		Player::useItem(%clientId,%item);
	}
}

function remoteDropItem(%clientId,%type)
{
	dbecho($dbechoMode, "remoteDropItem(" @ %clientId @ ", " @ %item @ ")");

	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastWaitActionTime > $waitActionDelay)
	{
		%clientId.lastWaitActionTime = %time;
	
		if($droppingAllowed == 1)
		{
			if((Client::getOwnedObject(%clientId)).driver != 1) {
				//echo("Drop item: ",%type);
				%clientId.throwStrength = 1;

				%item = getItemData(%type);
				// Phase D: a VSlot placeholder row is a per-client display proxy,
				// not a real carried item - it can't be dropped. No-op with the
				// feature off (VSlot::IsSlotItem is false).
				if(VSlot::IsSlotItem(%item))
				{
					// Phase D: dropping a VSlot row drops the belt weapon it proxies
					// (Belt::DropItem auto-unequips, tosses a lootbag, saves). Re-sync
					// the rows after so the emptied slot updates.
					%item = VSlot::MappedItem(%clientId, %item);
					if(%item == "" || %item == -1)
						Client::sendMessage(%clientId, $MsgWhite, "That backpack slot is empty.");
					else
					{
						// VOID Phase 1b: rows can proxy any belt category now (armor
						// window) - drop in the item's OWN category, not just Weapons.
						Belt::DropItem(%clientId, %item, 1, $BeltItem[%item, "Type"]);
						VSlot::Sync(%clientId);
					}
					return;
				}
				if(%item == Weapon)
				{
					%item = Player::getMountedItem(%clientId,$WeaponSlot);
					// Belt-weapon sync: unequip belt state if this drops the
					// shell of the equipped belt weapon (phantom mounts are
					// count 0 - the engine drops nothing for those)
					BeltWeapon::GuardShellTransfer(%clientId, %item);
					Player::dropItem(%clientId,%item);
				}
				else if(%item == Ammo)
				{
					%item = Player::getMountedItem(%clientId,$WeaponSlot);
					if(%item.className == Weapon)
					{
						%item = %item.imageType.ammoType;
						Player::dropItem(%clientId,%item);
					}
				}
				else if (%item.className == Equipped)
				{
					Client::sendMessage(%clientId, $MsgRed, "You can't drop an equipped item!~wC_BuySell.wav");
				}
				else if ($LoreItem[%item])
				{
					Client::sendMessage(%clientId, $MsgRed, "You can't drop a lore item!~wC_BuySell.wav");
				}
				else
				{
					BeltWeapon::GuardShellTransfer(%clientId, %item);
					Player::dropItem(%clientId,%item);
				}
			}
		}
	}
}
function remoteDeployItem(%clientId,%type)
{
	//echo("Deploy item: ",%type);
	%item = getItemData(%type);
	Player::deployItem(%clientId,%item);
}

function remoteConsider(%clientId)
{
	dbecho($dbechoMode, "remoteConsider(" @ %clientId @ ")");

	%msgText[7] = "Easy prey!";
	%msgText[6] = "Shouldn't be a problem at all.";
	%msgText[5] = "You should win.";
	%msgText[4] = "Looks like an even fight.";
	%msgText[3] = "You might get killed...";
	%msgText[2] = "Looks VERY risky...";
	%msgText[1] = "You will NOT survive!";

	%msgColor[7] = $MsgGreen;
	%msgColor[6] = $MsgBeige;
	%msgColor[5] = $MsgBeige;
	%msgColor[4] = $MsgWhite;
	%msgColor[3] = $MsgRed;
	%msgColor[2] = $MsgRed;
	%msgColor[1] = $MsgRed;

	%maxMsg = 7;
	%midMsg = 4;
	%minMsg = 1;

	%nothingMsg = "You see nothing of interest.";
	%length = 500;
	%sawsomething = "";

	%player = Client::getOwnedObject(%clientId);
	%clientname = Client::getName(%clientId);
	%clientpos = GameBase::getPosition(%clientId);

	$los::object = "";
	$los::position = "";
	if(GameBase::getLOSinfo(%player, %length))
	{
		%object = $los::object;
		%objpos = $los::position;
		%obj = getObjectType(%object);
		%cl = Player::getClient(%object);

		%index = GetEventCommandIndex(%object.tag, "onConsider");

		if(%obj == "Player")
		{
			DisplayGetInfo(%clientId, %cl, %object);
			%sawsomething = True;
		}
		else if(%obj == "InteriorShape" && %object.tag != "" && %clientId.adminLevel >= 1)
		{
			Client::sendMessage(%clientId, $MsgWhite, %object @ "'s tag name: " @ %object.tag);
			%sawsomething = True;
		}
		else if(%clientId.adminLevel >= 3)
		{
			Client::sendMessage(%clientId, $MsgWhite, "Position at LOS is " @ %objpos);
			%sawsomething = True;
		}

		if(%index != -1)
		{
			%closest = 999999;
			%cindex = "";

			//pick the event with the closest radius, matching criteria of event
			for(%i2 = 0; (%index2 = GetWord(%index, %i2)) != -1; %i2++)
			{
				%ec = $EventCommand[%object.tag, %index2];

				%targetname = GetWord(%ec, 4);
				if(String::ICompare(%targetname, %clientname) == 0 || String::ICompare(%targetname, "all") == 0)
				{
					%radius = GetWord(%ec, 2);
					if(Vector::getDistance(%objpos, %clientpos) <= %radius)
					{
						if(%radius < %closest)
						{
							%closest = %radius;
							%cindex = %index2;
						}
					}
				}
			}

			if(%cindex != "")
			{
				%ec = $EventCommand[%object.tag, %cindex];

				%name = GetWord(%ec, 0);
				if((%cl = NEWgetClientByName(%name)) == -1)
					%cl = 2048;
				%keep = GetWord(%ec, 3);

				%cmd = String::NEWgetSubStr(%ec, String::findSubStr(%ec, ">")+1, 99999);
				%pcmd = ParseBlockData(%cmd, %clientId, "");
				if(!%keep)
					$EventCommand[%object.tag, %cindex] = "";
				remoteSay(%cl, 0, %pcmd, %name);

				%sawsomething = True;
			}
		}
	}

	if(!%sawsomething)
		Client::sendMessage(%clientId, $MsgWhite, %nothingMsg);
}
