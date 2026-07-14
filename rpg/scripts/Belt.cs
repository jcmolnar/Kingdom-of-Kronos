$BELT_DEBUG = 0; // Toggle verbose belt menu/storage debug echoes (these spammed the console unconditionally on every menu open)

$Belt::Count["QuestItems"] = 0;
$Belt::Count["KeyItems"] = 0;
$Belt::Count["Deployables"] = 0;
$Belt::Count["Consumables"] = 0;
$Belt::Count["Armor"] = 0;
$Belt::Count["Accessories"] = 0;
$Belt::Count["Other"] = 0;
$Belt::Count["Weapons"] = 0;

$Belt::Categories[1] = "QuestItems";
$Belt::Categories[2] = "KeyItems";
$Belt::Categories[3] = "Deployables";
$Belt::Categories[4] = "Consumables";
$Belt::Categories[5] = "Armor";
$Belt::Categories[6] = "Accessories";
$Belt::Categories[7] = "Other";
// Weapons = datablock-less belt weapons (BeltWeapons.cs). Registering the
// category here surfaces them in every generic belt path: view/sell/store
// menus, KronosHUD inventory, weight totals, and the death-drop loop
// (carried belt items dropping on death is standard belt behavior).
// Persistence: "Weapons" = save field 58, "StoredWeapons" = field 59 (rpgfunk.cs).
$Belt::Categories[8] = "Weapons";

// Alias function for backward compatibility - code may still reference isBackpackItem
function isBackpackItem(%item)
{
	return isBeltItem(%item);
}

function Belt::BankStorageConversion(%clientId)
{
	// Use BeltStorage as single source of truth, derive separate categories from it for backwards compatibility
	// On character load, ALWAYS rebuild BeltStorage from loaded stored categories to ensure sync
	// This ensures BeltStorage matches what was saved in funk::var fields 38, 39, 42, 43, 45, 46
	
	// First, get the loaded stored categories (these were loaded from funk::var in rpgfunk.cs)
	%storedQuest = fetchData(%clientId, "StoredQuestItems");
	%storedKey = fetchData(%clientId, "StoredKeyItems");
	%storedConsumables = fetchData(%clientId, "StoredConsumables");
	%storedArmor = fetchData(%clientId, "StoredArmor");
	%storedAccessories = fetchData(%clientId, "StoredAccessories");
	%storedOther = fetchData(%clientId, "StoredOther");
	%storedWeapons = fetchData(%clientId, "StoredWeapons");

	//echo("DEBUG Belt::BankStorageConversion: Loaded categories - Quest='" @ %storedQuest @ "', Key='" @ %storedKey @ "', Consumables='" @ %storedConsumables @ "'");
	
	// Normalize "0" to empty string
	if(%storedQuest == "0" || %storedQuest == " ")
		%storedQuest = "";
	if(%storedKey == "0" || %storedKey == " ")
		%storedKey = "";
	if(%storedConsumables == "0" || %storedConsumables == " ")
		%storedConsumables = "";
	if(%storedArmor == "0" || %storedArmor == " ")
		%storedArmor = "";
	if(%storedAccessories == "0" || %storedAccessories == " ")
		%storedAccessories = "";
	if(%storedOther == "0" || %storedOther == " ")
		%storedOther = "";
	if(%storedWeapons == "0" || %storedWeapons == " ")
		%storedWeapons = "";

	// Clean and combine separate categories into BeltStorage
	%cleanedQuest = "";
	%removedQuest = 0;
	for(%i = 0; GetWord(%storedQuest, %i) != -1; %i += 2)
	{
		%item = GetWord(%storedQuest, %i);
		%count = GetWord(%storedQuest, %i + 1);
		// Convert count to numeric to properly handle negative values like "-1" or "-0"
		%countNum = %count * 1;
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
		{
			%cleanedQuest = Belt::AddToList(%cleanedQuest, %item @ " " @ %count);
		}
		else
		{
			%removedQuest++;
		}
	}
	// Belt::AddToList doesn't add trailing spaces, so no need to remove them
	// Update if cleaned
	if(%cleanedQuest != %storedQuest)
	{
		storeData(%clientId, "StoredQuestItems", %cleanedQuest);
		%storedQuest = %cleanedQuest;
	}
	
	// Clean up invalid entries from StoredKeyItems
	%cleanedKey = "";
	%removedKey = 0;
	for(%i = 0; GetWord(%storedKey, %i) != -1; %i += 2)
	{
		%item = GetWord(%storedKey, %i);
		%count = GetWord(%storedKey, %i + 1);
		// Convert count to numeric to properly handle negative values like "-1" or "-0"
		%countNum = %count * 1;
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
		{
			%cleanedKey = Belt::AddToList(%cleanedKey, %item @ " " @ %count);
		}
		else
		{
			%removedKey++;
		}
	}
	// Belt::AddToList doesn't add trailing spaces, so no need to remove them
	// Update if cleaned
	if(%cleanedKey != %storedKey)
	{
		storeData(%clientId, "StoredKeyItems", %cleanedKey);
		%storedKey = %cleanedKey;
	}
	
	// Clean up invalid entries from StoredConsumables
	%cleanedConsumables = "";
	%removedConsumables = 0;
	for(%i = 0; GetWord(%storedConsumables, %i) != -1; %i += 2)
	{
		%item = GetWord(%storedConsumables, %i);
		%count = GetWord(%storedConsumables, %i + 1);
		// Convert count to numeric to properly handle negative values like "-1" or "-0"
		%countNum = %count * 1;
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
		{
			%cleanedConsumables = Belt::AddToList(%cleanedConsumables, %item @ " " @ %count);
		}
		else
		{
			%removedConsumables++;
		}
	}
	// Belt::AddToList doesn't add trailing spaces, so no need to remove them
	// Update if cleaned
	if(%cleanedConsumables != %storedConsumables)
	{
		storeData(%clientId, "StoredConsumables", %cleanedConsumables);
		%storedConsumables = %cleanedConsumables;
	}
	
	// Clean up invalid entries from StoredArmor
	%cleanedArmor = "";
	%removedArmor = 0;
	for(%i = 0; GetWord(%storedArmor, %i) != -1; %i += 2)
	{
		%item = GetWord(%storedArmor, %i);
		%count = GetWord(%storedArmor, %i + 1);
		// Convert count to numeric to properly handle negative values like "-1" or "-0"
		%countNum = %count * 1;
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
		{
			%cleanedArmor = Belt::AddToList(%cleanedArmor, %item @ " " @ %count);
		}
		else
		{
			%removedArmor++;
		}
	}
	// Belt::AddToList doesn't add trailing spaces, so no need to remove them
	// Update if cleaned
	if(%cleanedArmor != %storedArmor)
	{
		storeData(%clientId, "StoredArmor", %cleanedArmor);
		%storedArmor = %cleanedArmor;
	}
	
	// Clean up invalid entries from StoredAccessories
	%cleanedAccessories = "";
	%removedAccessories = 0;
	for(%i = 0; GetWord(%storedAccessories, %i) != -1; %i += 2)
	{
		%item = GetWord(%storedAccessories, %i);
		%count = GetWord(%storedAccessories, %i + 1);
		// Convert count to numeric to properly handle negative values like "-1" or "-0"
		%countNum = %count * 1;
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
		{
			%cleanedAccessories = Belt::AddToList(%cleanedAccessories, %item @ " " @ %count);
		}
		else
		{
			%removedAccessories++;
		}
	}
	// Belt::AddToList doesn't add trailing spaces, so no need to remove them
	// Update if cleaned
	if(%cleanedAccessories != %storedAccessories)
	{
		storeData(%clientId, "StoredAccessories", %cleanedAccessories);
		%storedAccessories = %cleanedAccessories;
	}
	
	// Clean up invalid entries from StoredOther
	%cleanedOther = "";
	%removedOther = 0;
	for(%i = 0; GetWord(%storedOther, %i) != -1; %i += 2)
	{
		%item = GetWord(%storedOther, %i);
		%count = GetWord(%storedOther, %i + 1);
		// Convert count to numeric to properly handle negative values like "-1" or "-0"
		%countNum = %count * 1;
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
		{
			%cleanedOther = Belt::AddToList(%cleanedOther, %item @ " " @ %count);
		}
		else
		{
			%removedOther++;
		}
	}
	// Belt::AddToList doesn't add trailing spaces, so no need to remove them
	// Update if cleaned
	if(%cleanedOther != %storedOther)
	{
		storeData(%clientId, "StoredOther", %cleanedOther);
		%storedOther = %cleanedOther;
	}

	// Clean up invalid entries from StoredWeapons (belt weapons - BeltWeapons.cs)
	%cleanedWeapons = "";
	for(%i = 0; GetWord(%storedWeapons, %i) != -1; %i += 2)
	{
		%item = GetWord(%storedWeapons, %i);
		%count = GetWord(%storedWeapons, %i + 1);
		%countNum = %count * 1;
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
			%cleanedWeapons = Belt::AddToList(%cleanedWeapons, %item @ " " @ %count);
	}
	if(%cleanedWeapons != %storedWeapons)
	{
		storeData(%clientId, "StoredWeapons", %cleanedWeapons);
		%storedWeapons = %cleanedWeapons;
	}

		// Combine StoredQuestItems, StoredKeyItems, StoredConsumables, StoredArmor, StoredAccessories, and StoredOther into BeltStorage
		%beltStorage = "";
		if(%storedQuest != "")
			%beltStorage = %storedQuest;
		if(%storedKey != "")
		{
			if(%beltStorage != "")
				%beltStorage = %beltStorage @ " " @ %storedKey;
			else
				%beltStorage = %storedKey;
		}
		if(%storedConsumables != "")
		{
			if(%beltStorage != "")
				%beltStorage = %beltStorage @ " " @ %storedConsumables;
			else
				%beltStorage = %storedConsumables;
		}
		if(%storedArmor != "")
		{
			if(%beltStorage != "")
				%beltStorage = %beltStorage @ " " @ %storedArmor;
			else
				%beltStorage = %storedArmor;
		}
		if(%storedAccessories != "")
		{
			if(%beltStorage != "")
				%beltStorage = %beltStorage @ " " @ %storedAccessories;
			else
				%beltStorage = %storedAccessories;
		}
		if(%storedOther != "")
		{
			if(%beltStorage != "")
				%beltStorage = %beltStorage @ " " @ %storedOther;
			else
				%beltStorage = %storedOther;
		}
		if(%storedWeapons != "")
		{
			if(%beltStorage != "")
				%beltStorage = %beltStorage @ " " @ %storedWeapons;
			else
				%beltStorage = %storedWeapons;
		}

		// Final cleanup pass on BeltStorage to remove any invalid entries (item "0", count 0, etc.)
		// This prevents "0 0" entries from persisting in BeltStorage
		%finalCleaned = "";
		for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
		{
			%item = GetWord(%beltStorage, %i);
			%count = GetWord(%beltStorage, %i + 1);
			// Convert count to numeric to properly handle negative values like "-1" or "-0"
			%countNum = %count * 1;
			if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
			{
				%finalCleaned = %finalCleaned @ %item @ " " @ %count @ " ";
			}
		}
	%beltStorage = %finalCleaned;
	
	// CRITICAL: Add trailing space to BeltStorage for consistency with spawnStuff format
	// This ensures belt items can be properly concatenated with other lists without breaking the format
	if(%beltStorage != "" && %beltStorage != "0")
		%beltStorage = %beltStorage @ " ";
	
	// Always update BeltStorage from saved data on load to ensure it matches
	// This fixes cases where BeltStorage might be out of sync
	storeData(%clientId, "BeltStorage", %beltStorage);
	//echo("DEBUG Belt::BankStorageConversion: Rebuilt BeltStorage='" @ %beltStorage @ "'");
	
	// Update stored categories to match the cleaned versions we just processed
	// This ensures they stay in sync with BeltStorage
	storeData(%clientId, "StoredQuestItems", %storedQuest);
	storeData(%clientId, "StoredKeyItems", %storedKey);
	storeData(%clientId, "StoredConsumables", %storedConsumables);
	storeData(%clientId, "StoredArmor", %storedArmor);
	storeData(%clientId, "StoredAccessories", %storedAccessories);
	storeData(%clientId, "StoredOther", %storedOther);
	storeData(%clientId, "StoredWeapons", %storedWeapons);
	//echo("DEBUG Belt::BankStorageConversion: Updated StoredQuestItems='" @ %storedQuest @ "'");
	
	// Also clean up equipped categories (QuestItems, KeyItems) to remove corrupted entries
	// Skip GetNS cleanup during early load to avoid "Unknown command" errors
	// GetNS will be called later when functions are fully loaded
	// For now, just do basic validation
	for(%i = 1; $Belt::Categories[%i] != ""; %i++)
	{
		%category = $Belt::Categories[%i];
		%equipped = fetchData(%clientId, %category);
		
		// Basic cleanup: normalize "0" to empty string
		if(%equipped == "0" || %equipped == " ")
			%equipped = "";
		
		// Simple validation: remove invalid entries (item "0", count 0, etc.)
		%cleanedEquipped = "";
		for(%j = 0; GetWord(%equipped, %j) != -1; %j += 2)
		{
			%item = GetWord(%equipped, %j);
			%count = GetWord(%equipped, %j + 1);
			%countNum = %count * 1;
			if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
				%cleanedEquipped = %cleanedEquipped @ %item @ " " @ %count @ " ";
		}
		%len = String::len(%cleanedEquipped);
		if(%len > 0 && String::getSubStr(%cleanedEquipped, %len-1, 1) == " ")
			%cleanedEquipped = String::getSubStr(%cleanedEquipped, 0, %len-1);
		
		if(%cleanedEquipped != %equipped)
		{
			storeData(%clientId, %category, %cleanedEquipped);
		}
	}
	
	// Force refresh to update any open menus
	RefreshAll(%clientId);
}

function MenuViewBackpack(%clientId, %page)
{
	MenuViewBelt(%clientId, %page);
}

function MenuViewBelt(%clientId, %page)
{
	%beltWeight = Belt::GetWeight(%clientId);
	Client::buildMenu(%clientId, ".:(Backpack):. Weight: " @ %beltWeight, "ViewBelt", true);
	
	%cnt = 1; // Initialize menu counter
	for(%i = 1; %i < Belt::GetLastItem(); %i++)
	{
		if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]))
			Client::addMenuItem(%clientId, %cnt++ @ ". " @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i]);
	}

	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}

function processMenuViewBelt(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);

	if($Belt::Count[%o] > 0)
	{
		MenuBeltGear(%clientId, %o, 1);
		return;
	}

	if(%o != "done")
	MenuViewBelt(%clientId, %p);

	return;
}

function MenuBeltGear(%clientId, %type, %page)
{
	%disp = Belt::Display(%type);

	%msg = "<jc><f2>To drop 'bulk' " @ %disp @ ", please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
	
	Client::buildMenu(%clientId, %disp @ ":", "BeltGear", true);
	%clientId.bulkNum = "";

	%cnt = 1; // Initialize menu counter
	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = Belt::GetNS(%clientId, %type);
	%ns = GetWord(%nf, 0);
	%np = floor((%ns - 1) / %l);	// review #37: 0-indexed last page = ceil(count/pageSize)-1. floor(ns/l) was one too high when the count was an exact multiple of 6, rendering a phantom empty "Next" page.
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf, %x);
		%amnt = Belt::HasThisStuff(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt++ @ ": " @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type);
	}

	return;
}

function processMenuBeltGear(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o == "back")
	{
		MenuViewBelt(%clientId, 1);
		return;
	}

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)
		%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)
		%clientId.bulkNum = 500;

		MenuBeltDrop(%clientId, %o, %t);
		return;
	}

	if(%o != "done")
	MenuBeltGear(%clientId, %t, %p);

	return;
}

function MenuBeltDrop(%clientId, %item, %type)
{
	%name = $BeltItem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	%cmnt = Belt::HasThisStuff(%clientId, %item);

	if(%amnt > %cmnt)
	%amnt = %cmnt;

	Client::buildMenu(%clientId, %name @ " (" @ %cmnt @ ")", "BeltDrop", true);
	
	%cnt = 1; // Initialize menu counter
	
	// Check if item is consumable (potions are in Consumables category)
	// Also check if the item itself is a known potion (in case type isn't passed correctly)
	%isConsumable = (%type == "Consumables" || %item == "BluePotion" || %item == "CrystalBluePotion" || %item == "EnergyVial" || %item == "CrystalEnergyVial");
	
	if(%isConsumable)
	{
		Client::addMenuItem(%clientId, %cnt++ @ ": Use", %type @ " use " @ %item);
	}
	
	// Check if item is deployable (DepBasePack has onDeploy callback)
	if(%item == "DepBasePack")
	{
		Client::addMenuItem(%clientId, %cnt++ @ "Deploy", %type @ " deploy " @ %item);
	}
	
	// Check if item is Armor - show Equip option
	if(%type == "Armor")
	{
		// Check if this armor is currently equipped
		%equippedArmor = fetchData(%clientId, "EquippedBeltArmor");
		if(%equippedArmor == %item)
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Unequip", %type @ " unequip " @ %item);
		}
		else
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Equip", %type @ " equip " @ %item);
		}
	}
	
	// Check if item is a belt weapon - show Equip/Unequip (BeltWeapons.cs)
	if(%type == "Weapons")
	{
		if(fetchData(%clientId, "EquippedBeltWeapon") == %item)
			Client::addMenuItem(%clientId, %cnt++ @ "Unequip", %type @ " unequip " @ %item);
		else
			Client::addMenuItem(%clientId, %cnt++ @ "Equip", %type @ " equip " @ %item);
	}

	// Check if item is Accessory - show Equip/Unequip based on slot availability
	if(%type == "Accessories")
	{
		// Get accessory type and slot limits
		%accessoryType = $AccessoryVar[%item, $AccessoryType];
		%maxSlots = $maxAccessory[%accessoryType];
		if(%maxSlots == "" || %maxSlots == 0)
			%maxSlots = 1;
		
		// Count how many of THIS SPECIFIC ITEM are already equipped (not just this type)
		%equippedList = fetchData(%clientId, "EquippedBeltAccessories");
		%thisItemEquippedCount = 0;
		for(%i = 0; GetWord(%equippedList, %i) != -1; %i++)
		{
			if(GetWord(%equippedList, %i) == %item)
				%thisItemEquippedCount++;
		}
		
		// Get how many of this item the player has in inventory
		%inventoryCount = Belt::HasThisStuff(%clientId, %item);
		
		// DEBUG: Log menu decision
		if($BELT_DEBUG) echo("[BELT MENU DEBUG] Item: " @ %item @ ", AccessoryType: " @ %accessoryType @ ", ThisItemEquipped: " @ %thisItemEquippedCount @ ", InInventory: " @ %inventoryCount @ ", MaxSlots: " @ %maxSlots);
		
		// Show Unequip if at least 1 of this item is equipped
		// Show Equip if: (items in inventory > items equipped) AND (total equipped of this type < max slots)
		if(%thisItemEquippedCount > 0)
		{
			// At least one is equipped - show Unequip
			if($BELT_DEBUG) echo("[BELT MENU DEBUG] Showing Unequip (" @ %thisItemEquippedCount @ " equipped)");
			Client::addMenuItem(%clientId, %cnt++ @ "Unequip", %type @ " unequip " @ %item);
		}
		
		// Check if we can equip more
		%currentTypeCount = Belt::GetEquippedAccessoryCountByType(%clientId, %accessoryType);
		if(%inventoryCount > %thisItemEquippedCount && %currentTypeCount < %maxSlots)
		{
			// Have more in inventory and slots available - show Equip
			if($BELT_DEBUG) echo("[BELT MENU DEBUG] Showing Equip (can equip more: inv=" @ %inventoryCount @ ", equipped=" @ %thisItemEquippedCount @ ", typeSlots=" @ %currentTypeCount @ "/" @ %maxSlots @ ")");
			Client::addMenuItem(%clientId, %cnt++ @ "Equip", %type @ " equip " @ %item);
		}
		else if(%currentTypeCount >= %maxSlots && %thisItemEquippedCount == 0)
		{
			// No slots available and this item not equipped - show message
			if($BELT_DEBUG) echo("[BELT MENU DEBUG] Showing 'At max' message");
			%typeName = $LocationDesc[%accessoryType];
			if(%typeName == "")
				%typeName = "accessory";
			Client::addMenuItem(%clientId, %cnt++ @ "(At max " @ %typeName @ "s)", "disabled");
		}
	}
	
	Client::addMenuItem(%clientId, %cnt++ @ "Drop (" @ %amnt @ ")", %type @ " drop " @ %item @ " " @ %amnt);
	Client::addMenuItem(%clientId, %cnt++ @ "Examine", %type @ " examine " @ %item);
	Client::addMenuItem(%clientId, "bBack", %type @ " back " @ %item);
	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}

function processMenuBeltDrop(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);

	if(%amnt <= 0) %amnt = 1;

	if(%amnt != %clientId.bulkNum)
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuBeltDrop(%clientId, %item, %type);
	}
	else if(%option == "back")
	{
		// Go back to the category item list
		MenuBeltGear(%clientId, %type, 1);
	}
	else if(%option == "use")
	{
		// Use consumable item (potions, etc.)
		Belt::UseItem(%clientId, %item, %type);
		// Refresh the menu to show updated counts (stay in belt menu)
		MenuBeltDrop(%clientId, %item, %type);
	}
	else if(%option == "deploy")
	{
		Belt::DeployItem(%clientId, %item, %type);
	}
	else if(%option == "drop")
	{
		Belt::DropItem(%clientId, %item, %amnt, %type);
		// Refresh the menu to show updated counts (stay in belt menu)
		// Check if item still exists before refreshing to that item's menu
		%remaining = Belt::HasThisStuff(%clientId, %item);
		if(%remaining > 0)
		{
			MenuBeltDrop(%clientId, %item, %type);
		}
		else
		{
			// Item fully depleted - go back to category menu
			MenuBeltGear(%clientId, %type, 1);
		}
	}
	else if(%option == "examine")
	{
		Belt::WhatIs(%clientId, %item);
		// Return to item menu after examine
		MenuBeltDrop(%clientId, %item, %type);
	}
	else if(%option == "equip")
	{
		// Equip armor or accessory
		if(%type == "Armor")
		{
			Belt::EquipArmor(%clientId, %item);
		}
		else if(%type == "Accessories")
		{
			Belt::EquipAccessory(%clientId, %item);
		}
		else if(%type == "Weapons")
		{
			BeltWeapon::Equip(%clientId, %item);
		}
		// Refresh the menu to show updated equip status
		MenuBeltDrop(%clientId, %item, %type);
	}
	else if(%option == "unequip")
	{
		// Unequip armor or accessory
		if(%type == "Armor")
		{
			Belt::UnequipArmor(%clientId, %item);
		}
		else if(%type == "Accessories")
		{
			Belt::UnequipAccessory(%clientId, %item);
		}
		else if(%type == "Weapons")
		{
			BeltWeapon::Unequip(%clientId, false);
		}
		// Refresh the menu to show updated equip status
		MenuBeltDrop(%clientId, %item, %type);
	}
	return;
}

function MenuSellBelt(%clientId)
{
	Client::buildMenu(%clientId, "Pack Sell:", "SellBelt", true);

	for(%i = 1; %i < Belt::GetLastItem(); %i++)
	if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]))
	Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i]);
	
	Client::addMenuItem(%clientId, "xFinished", "done");
	return;
}

function processMenuSellBelt(%clientId, %opt)
{
	if($Belt::Count[%opt] > 0)
	{
		MenuSellBeltItem(%clientId, %opt, 1);
		return;
	}

	if(%opt == "done")
	{
		// Clear stored orders for all belt types
		for(%i = 1; $Belt::Categories[%i] != ""; %i++)
		{
			%clientId.beltItemOrder[$Belt::Categories[%i]] = "";
			%clientId.beltStoredItemOrder[$Belt::Categories[%i]] = "";
		}
		Client::cancelMenu(%clientId);
		return;
	}

	if(%opt != "done")
	MenuSellBelt(%clientId);

	return;
}

function MenuSellBeltItem(%clientId, %type, %page)
{
	%disp = Belt::Display(%type);

	// Store the original order when first opening the menu (page 1)
	if(%page == 1 && %clientId.beltItemOrder[%type] == "")
	{
		%stuff = fetchData(%clientId, %type);
		%orderList = "";
		for(%i = 0; (%item = getWord(%stuff, %i)) != -1; %i+=2)
		{
			%amnt = getWord(%stuff, %i + 1);
			if(%amnt > 0)
			{
				%orderList = %orderList @ " " @ %item;
			}
		}
		%clientId.beltItemOrder[%type] = String::NEWgetSubStr(%orderList, 1, 99999);
	}

	Client::buildMenu(%clientId, %disp @ " Sell:", "SellBeltItem", true);

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = Belt::GetNS(%clientId, %type);
	%ns = GetWord(%nf, 0);
	%np = floor((%ns - 1) / %l);	// review #37: 0-indexed last page = ceil(count/pageSize)-1. floor(ns/l) was one too high when the count was an exact multiple of 6, rendering a phantom empty "Next" page.
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf, %x);
		%amnt = Belt::HasThisStuff(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
	}

	return;
}

function processMenuSellBeltItem(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o == "back")
	{
		MenuSellBelt(%clientId);
		return;
	}

	if(%o != "page" && %o != "done")
	{
		MenuSellBeltItemFinal(%clientId, %o, %t, "sell");
		return;
	}

	if(%o != "done")
		MenuSellBeltItem(%clientId, %t, %p);

	return;
}

function MenuSellBeltItemFinal(%clientId, %item, %type, %mode)
{
	// Validate item parameter
	if(%item == "" || %item == -1 || %item == "0")
	{
		%clientName = Client::getName(%clientId);
		echo("ERROR: MenuSellBeltItemFinal - Invalid item parameter: '" @ %item @ "' type: '" @ %type @ "' mode: '" @ %mode @ "' ClientId: " @ %clientId @ " (" @ %clientName @ ")");
		return;
	}
	
	%name = $BeltItem[%item, "Name"];
	if(%name == "")
	{
		// Fallback to AccessoryVar for non-belt items or items without BeltItem registration
		%name = $AccessoryVar[%item, $Name];
		if(%name == "")
			%name = %item;
	}
	
	%cnt = 1; // Initialize menu counter
	
	// Get available count based on mode
	if(%mode == "withdraw")
	{
		// Check if withdrawing from banker (BeltStorage) or from belt storage (StoredQuestItems/StoredKeyItems)
		if(%clientId.currentBeltBank != "")
			%cmnt = Belt::ItemCount(%item, fetchData(%clientId, "BeltStorage"));
		else
			%cmnt = Belt::ItemCount(%item, fetchData(%clientId, "Stored" @ %type));
	}
	else if(%mode == "store" || %mode == "sell")
		%cmnt = Belt::ItemCount(%item, fetchData(%clientId, %type));
	else
		%cmnt = 0;

	Client::buildMenu(%clientId, %name @ " (" @ %cmnt @ ")", "SellBeltItemFinal", true);

	// Show Sell/Withdraw/Store 1 button if player has 1 or more (always show if count > 0)
	if(%cmnt >= 1)
	{
		if(%mode == "sell")
		{
			%cost1 = Belt::GetSellCost(%clientId, %item) * 1;
			Client::addMenuItem(%clientId, %cnt++ @ "Sell 1 ($" @ %cost1 @ ")", %type @ " sell " @ %item @ " 1");
		}
		else if(%mode == "withdraw")
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Withdraw 1", %type @ " withdraw " @ %item @ " 1");
		}
		else if(%mode == "store")
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Store 1", %type @ " store " @ %item @ " 1");
		}
	}

	// Show Sell/Withdraw/Store 5 button if player has 5 or more
	if(%cmnt >= 5)
	{
		if(%mode == "sell")
		{
			%cost5 = Belt::GetSellCost(%clientId, %item) * 5;
			Client::addMenuItem(%clientId, %cnt++ @ "Sell 5 ($" @ %cost5 @ ")", %type @ " sell " @ %item @ " 5");
		}
		else if(%mode == "withdraw")
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Withdraw 5", %type @ " withdraw " @ %item @ " 5");
		}
		else if(%mode == "store")
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Store 5", %type @ " store " @ %item @ " 5");
		}
	}

	// Show Sell/Withdraw/Store 10 button if player has 10 or more
	if(%cmnt >= 10)
	{
		if(%mode == "sell")
		{
			%cost10 = Belt::GetSellCost(%clientId, %item) * 10;
			Client::addMenuItem(%clientId, %cnt++ @ "Sell 10 ($" @ %cost10 @ ")", %type @ " sell " @ %item @ " 10");
		}
		else if(%mode == "withdraw")
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Withdraw 10", %type @ " withdraw " @ %item @ " 10");
		}
		else if(%mode == "store")
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Store 10", %type @ " store " @ %item @ " 10");
		}
	}

	// Always show Sell/Withdraw/Store All button
	if(%mode == "sell")
	{
		%costAll = Belt::GetSellCost(%clientId, %item) * %cmnt;
		Client::addMenuItem(%clientId, %cnt++ @ "Sell All (" @ %cmnt @ ") ($" @ %costAll @ ")", %type @ " sell " @ %item @ " " @ %cmnt);
	}
	else if(%mode == "withdraw")
	{
		Client::addMenuItem(%clientId, %cnt++ @ "Withdraw All (" @ %cmnt @ ")", %type @ " withdraw " @ %item @ " " @ %cmnt);
	}
	else if(%mode == "store")
		{
			Client::addMenuItem(%clientId, %cnt++ @ "Store All (" @ %cmnt @ ")", %type @ " store " @ %item @ " " @ %cmnt);
		}

	Client::addMenuItem(%clientId, "bBack", %type @ " back " @ %item @ " " @ %mode);
	Client::addMenuItem(%clientId, "xFinished", "ineedanextrawordsothisisithaha done");
	return;
}

function processMenuSellBeltItemFinal(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	
	// Check for menu closure option first, before validating other parameters
	if(%type == "ineedanextrawordsothisisithaha" || %option == "done")
	{
		// Clear the belt sell flag and stored order when menu is closed
		%clientId.currentBeltSell = "";
		// Clear stored orders for all belt types
		for(%i = 1; $Belt::Categories[%i] != ""; %i++)
		{
			%clientId.beltItemOrder[$Belt::Categories[%i]] = "";
			%clientId.beltStoredItemOrder[$Belt::Categories[%i]] = "";
		}
		Client::cancelMenu(%clientId);
		return;
	}
	
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);
	
	// Validate parameters
	if(%item == "" || %item == -1)
	{
		%clientName = Client::getName(%clientId);
		echo("ERROR: processMenuSellBeltItemFinal - Invalid item parameter: '" @ %item @ "' from option: '" @ %opt @ "' ClientId: " @ %clientId @ " (" @ %clientName @ ")");
		return;
	}
	if(%amnt == "" || %amnt == -1)
	{
		%clientName = Client::getName(%clientId);
		echo("ERROR: processMenuSellBeltItemFinal - Invalid amount parameter: '" @ %amnt @ "' from option: '" @ %opt @ "' ClientId: " @ %clientId @ " (" @ %clientName @ ")");
		return;
	}
	%amnt = floor(%amnt); // Ensure %amnt is a number
	
	if(%option == "back")
	{
		%mode = GetWord(%opt, 3);
		if(%mode == "sell")
			MenuSellBeltItem(%clientId, %type, 1);
		else if(%mode == "store")
		{
			// Check if storing to banker or belt storage
			if(%clientId.currentBeltBank != "")
				Belt::ShowDepositCategoryMenu(%clientId);
			else
				MenuBeltStoreThisItem(%clientId, %type, 1, %mode);
		}
		else if(%mode == "withdraw")
		{
			// Check if withdrawing from banker or belt storage
			if(%clientId.currentBeltBank != "")
				Belt::ShowWithdrawCategoryMenu(%clientId);
			else
				MenuBeltWithdrawThisItem(%clientId, %type, 1, %mode);
		}
		return;
	}
	else if(%option == "sell")
	{
		%cmnt = Belt::HasThisStuff(%clientId, %item);
		if(%cmnt >= %amnt)
		{
			// CRITICAL: Auto-unequip if selling equipped accessories
			%category = $BeltItem[%item, "Type"];
			if(%category == "Accessories")
			{
				for(%unequipCount = 0; %unequipCount < %amnt; %unequipCount++)
				{
					if(Belt::IsAccessoryEquipped(%clientId, %item))
						Belt::UnequipAccessory(%clientId, %item);
				}
			}
			else if(%category == "Armor" && fetchData(%clientId, "EquippedBeltArmor") == %item)
			{
				Belt::UnequipArmor(%clientId, %item);
			}
			else if(%category == "Weapons" && fetchData(%clientId, "EquippedBeltWeapon") == %item)
			{
				BeltWeapon::Unequip(%clientId, true);
			}

			%cost = Belt::GetSellCost(%clientId, %item) * %amnt;
			Client::sendMessage(%clientId, $MsgWhite, "You sold " @ %amnt @ " " @ %item @ " for " @ Number::Beautify(%cost, -3) @ " coins.");
			UseSkill(%clientId, $SkillHaggling, true, true);
			storeData(%clientId, "COINS", %cost, "inc");
			Belt::TakeThisStuff(%clientId, %item, %amnt);
			RefreshAll(%clientId);
			%clientId.bulkNum = 1;
			// Save character after selling to ensure coins and item changes are persisted
			SaveCharacter(%clientId);
			// Return to sell menu after selling
			MenuSellBeltItem(%clientId, %type, 1);
		}
	}
	else if(%option == "store")
	{
		// Get the registered item name (what's actually stored in the equipped category)
		%registeredItem = $BeltItem[%item, "Item"];
		if(%registeredItem == "")
		{
			%clientName = Client::getName(%clientId);
			echo("ERROR: Belt deposit - Item '" @ %item @ "' not found in belt item registry! ClientId: " @ %clientId @ " (" @ %clientName @ ")");
			return;
		}
		
		%cmnt = Belt::HasThisStuff(%clientId, %item);
		if(%cmnt >= %amnt)
		{
			// NOTE: auto-unequip of equipped accessories/armor now happens INSIDE the
			// success branches below, AFTER the 25-slot capacity check passes.
			// Previously it ran here, so a full storage refused the deposit but the
			// player's gear had already been silently unequipped.

			// Check if storing to banker (BeltStorage) or to belt storage (old system)
			if(%clientId.currentBeltBank != "")
			{
				// Storing to banker - work directly with BeltStorage as single source of truth
				%beltStorage = fetchData(%clientId, "BeltStorage");
				
				// Normalize "0" to empty string
				if(%beltStorage == "0" || %beltStorage == " ")
					%beltStorage = "";
				
				// Clean BeltStorage before adding to remove any invalid entries
				%cleanedStorage = "";
				for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
				{
					%itemName = GetWord(%beltStorage, %i);
					%itemCount = GetWord(%beltStorage, %i + 1);
					%countNum = %itemCount * 1;
					if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
						%cleanedStorage = %cleanedStorage @ %itemName @ " " @ %itemCount @ " ";
				}
				%len = String::len(%cleanedStorage);
				if(%len > 0 && String::getSubStr(%cleanedStorage, %len-1, 1) == " ")
					%cleanedStorage = String::getSubStr(%cleanedStorage, 0, %len-1);
				%beltStorage = %cleanedStorage;
				
				// Count unique items in BeltStorage
				%currentStorageCount = 0;
				for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
				{
					%itemName = GetWord(%beltStorage, %i);
					if(%itemName != "" && %itemName != -1 && %itemName != "0")
						%currentStorageCount++;
				}
				
				if(%currentStorageCount < 25)
				{
					// Add item directly to BeltStorage (single source of truth)
					%newBeltStorage = SetStuffString(%beltStorage, %registeredItem, %amnt);
					
					// Clean result after SetStuffString to remove any "0" entries
					%cleanedResult = "";
					for(%i = 0; GetWord(%newBeltStorage, %i) != -1; %i += 2)
					{
						%itemName = GetWord(%newBeltStorage, %i);
						%itemCount = GetWord(%newBeltStorage, %i + 1);
						%countNum = %itemCount * 1;
						if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
							%cleanedResult = %cleanedResult @ %itemName @ " " @ %itemCount @ " ";
					}
					%len = String::len(%cleanedResult);
					if(%len > 0 && String::getSubStr(%cleanedResult, %len-1, 1) == " ")
						%cleanedResult = String::getSubStr(%cleanedResult, 0, %len-1);
					
					// Save cleaned BeltStorage
					storeData(%clientId, "BeltStorage", %cleanedResult);
					
					// CRITICAL: Also update the stored category to keep it in sync with BeltStorage
					// This prevents SaveCharacter from rebuilding BeltStorage from stale stored categories
					%storedCategory = "Stored" @ %type;
					%currentStored = fetchData(%clientId, %storedCategory);
					if(%currentStored == "0" || %currentStored == " ") %currentStored = "";
					
					// Add item to stored category to match BeltStorage
					%newStored = SetStuffString(%currentStored, %registeredItem, %amnt);
					
					// Clean result after SetStuffString
					%cleanedStored = "";
					for(%i = 0; GetWord(%newStored, %i) != -1; %i += 2)
					{
						%itemName = GetWord(%newStored, %i);
						%itemCount = GetWord(%newStored, %i + 1);
						%countNum = %itemCount * 1;
						if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
							%cleanedStored = %cleanedStored @ %itemName @ " " @ %itemCount @ " ";
					}
					%len = String::len(%cleanedStored);
					if(%len > 0 && String::getSubStr(%cleanedStored, %len-1, 1) == " ")
						%cleanedStored = String::getSubStr(%cleanedStored, 0, %len-1);
					
					// Save cleaned stored category
					storeData(%clientId, %storedCategory, %cleanedStored);

					// Auto-unequip equipped accessories/armor (capacity check passed)
					%category = $BeltItem[%item, "Type"];
					if(%category == "Accessories")
					{
						for(%unequipCount = 0; %unequipCount < %amnt; %unequipCount++)
						{
							if(Belt::IsAccessoryEquipped(%clientId, %item))
								Belt::UnequipAccessory(%clientId, %item);
						}
					}
					else if(%category == "Armor" && fetchData(%clientId, "EquippedBeltArmor") == %item)
					{
						Belt::UnequipArmor(%clientId, %item);
					}
					else if(%category == "Weapons" && fetchData(%clientId, "EquippedBeltWeapon") == %item)
					{
						BeltWeapon::Unequip(%clientId, true);
					}

					// Remove from equipped belt
					Belt::TakeThisStuff(%clientId, %item, %amnt);
					
					%clientId.bulkNum = 1;
					RefreshAll(%clientId);
					SaveCharacter(%clientId);
					
					%itemName = $BeltItem[%registeredItem, "Name"];
					if(%itemName == "")
					{
						// Fallback to AccessoryVar for non-belt items
						%itemName = $AccessoryVar[%registeredItem, $Name];
						if(%itemName == "")
							%itemName = %registeredItem;
					}
					Client::sendMessage(%clientId, $MsgGreen, "Deposited " @ %amnt @ " " @ %itemName @ " into backpack storage.");
					
					// Clear stored order so menu rebuilds with current items
					%clientId.beltDepositItemOrder = "";
					
					// Return to deposit category menu
					Belt::ShowDepositCategoryMenu(%clientId);
				}
				else
				{
					Client::sendMessage(%clientId, $MsgRed, "You can only place 25 different items into backpack storage.~wC_BuySell.wav");
				}
			}
			else
			{
				// Storing to belt storage (old system)
				if(CountObjInList(fetchData(%clientId, "Stored" @ %type)) / 2 < 25)
				{
					// Auto-unequip equipped accessories/armor (capacity check passed)
					%category = $BeltItem[%item, "Type"];
					if(%category == "Accessories")
					{
						for(%unequipCount = 0; %unequipCount < %amnt; %unequipCount++)
						{
							if(Belt::IsAccessoryEquipped(%clientId, %item))
								Belt::UnequipAccessory(%clientId, %item);
						}
					}
					else if(%category == "Armor" && fetchData(%clientId, "EquippedBeltArmor") == %item)
					{
						Belt::UnequipArmor(%clientId, %item);
					}
					else if(%category == "Weapons" && fetchData(%clientId, "EquippedBeltWeapon") == %item)
					{
						BeltWeapon::Unequip(%clientId, true);
					}

					storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %registeredItem, %amnt));
					Belt::TakeThisStuff(%clientId, %item, %amnt);
					%clientId.bulkNum = 1;
					RefreshAll(%clientId);
					SaveCharacter(%clientId);
					MenuBeltStoreThisItem(%clientId, %type, 1, "store");
				}
				else
				{
					Client::sendMessage(%clientId, $MsgRed, "You can only place 25 different items into the " @ Belt::Display(%type) @ " storage.~wC_BuySell.wav");
				}
			}
		}
	}
	else if(%option == "withdraw")
	{
		// Get the registered item name (what's actually stored in BeltStorage)
		%registeredItem = $BeltItem[%item, "Item"];
		if(%registeredItem == "")
		{
			// Try using %item directly - it might already be the registered name from BeltStorage
			%registeredItem = %item;
		}
		
		// Check if withdrawing from banker (BeltStorage) or from belt storage (old system)
		if(%clientId.currentBeltBank != "")
		{
			// Withdrawing from banker - work directly with BeltStorage as single source of truth
			%beltStorage = fetchData(%clientId, "BeltStorage");
			
			// Normalize "0" to empty string
			if(%beltStorage == "0" || %beltStorage == " ")
				%beltStorage = "";
			
			// Clean BeltStorage before checking count
			%cleanedStorage = "";
			for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
			{
				%itemName = GetWord(%beltStorage, %i);
				%itemCount = GetWord(%beltStorage, %i + 1);
				%countNum = %itemCount * 1;
				if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
					%cleanedStorage = %cleanedStorage @ %itemName @ " " @ %itemCount @ " ";
			}
			%len = String::len(%cleanedStorage);
			if(%len > 0 && String::getSubStr(%cleanedStorage, %len-1, 1) == " ")
				%cleanedStorage = String::getSubStr(%cleanedStorage, 0, %len-1);
			%beltStorage = %cleanedStorage;
			
			// Check count in BeltStorage
			%cmnt = Belt::ItemCount(%registeredItem, %beltStorage);
			
			if(%cmnt >= %amnt)
			{
				// Remove item directly from BeltStorage
				%newBeltStorage = SetStuffString(%beltStorage, %registeredItem, -%amnt);
				
				// Clean result after SetStuffString to remove any "0" entries
				%cleanedResult = "";
				for(%i = 0; GetWord(%newBeltStorage, %i) != -1; %i += 2)
				{
					%itemName = GetWord(%newBeltStorage, %i);
					%itemCount = GetWord(%newBeltStorage, %i + 1);
					%countNum = %itemCount * 1;
					if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
						%cleanedResult = %cleanedResult @ %itemName @ " " @ %itemCount @ " ";
				}
				%len = String::len(%cleanedResult);
				if(%len > 0 && String::getSubStr(%cleanedResult, %len-1, 1) == " ")
					%cleanedResult = String::getSubStr(%cleanedResult, 0, %len-1);
				
				// Save cleaned BeltStorage
				storeData(%clientId, "BeltStorage", %cleanedResult);
				
				// CRITICAL: Also update the stored category to keep it in sync with BeltStorage
				// This prevents SaveCharacter from rebuilding BeltStorage from stale stored categories
				%storedCategory = "Stored" @ %type;
				%currentStored = fetchData(%clientId, %storedCategory);
				if(%currentStored == "0" || %currentStored == " ") %currentStored = "";
				
				// Remove item from stored category to match BeltStorage
				%newStored = SetStuffString(%currentStored, %registeredItem, -%amnt);
				
				// Clean result after SetStuffString
				%cleanedStored = "";
				for(%i = 0; GetWord(%newStored, %i) != -1; %i += 2)
				{
					%itemName = GetWord(%newStored, %i);
					%itemCount = GetWord(%newStored, %i + 1);
					%countNum = %itemCount * 1;
					if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
						%cleanedStored = %cleanedStored @ %itemName @ " " @ %itemCount @ " ";
				}
				%len = String::len(%cleanedStored);
				if(%len > 0 && String::getSubStr(%cleanedStored, %len-1, 1) == " ")
					%cleanedStored = String::getSubStr(%cleanedStored, 0, %len-1);
				
				// Save cleaned stored category
				storeData(%clientId, %storedCategory, %cleanedStored);
				
				// CRITICAL: If BeltStorage is now empty after withdrawal, clear ALL stored categories
				// This prevents SaveCharacter from rebuilding BeltStorage from stale stored categories
				if(%cleanedResult == "" || %cleanedResult == "0" || %cleanedResult == " ")
				{
					storeData(%clientId, "StoredQuestItems", "");
					storeData(%clientId, "StoredKeyItems", "");
					storeData(%clientId, "StoredConsumables", "");
					storeData(%clientId, "StoredArmor", "");
					storeData(%clientId, "StoredAccessories", "");
					storeData(%clientId, "StoredOther", "");
				}
				
				// Add to equipped belt
				Belt::GiveThisStuff(%clientId, %item, %amnt, 1);
				
				%clientId.bulkNum = 1;
				RefreshAll(%clientId);
				SaveCharacter(%clientId);
				
				%itemName = $BeltItem[%registeredItem, "Name"];
				if(%itemName == "")
				{
					// Fallback to AccessoryVar for non-belt items
					%itemName = $AccessoryVar[%registeredItem, $Name];
					if(%itemName == "")
						%itemName = %registeredItem;
				}
				Client::sendMessage(%clientId, $MsgGreen, "Withdrew " @ %amnt @ " " @ %itemName @ " from backpack storage.");
				
				// Clear stored order so menu rebuilds with current items
				%clientId.beltWithdrawItemOrder = "";
				
				// Return to withdraw category menu
				Belt::ShowWithdrawCategoryMenu(%clientId);
			}
			else
			{
				Client::sendMessage(%clientId, $MsgRed, "You don't have enough " @ $BeltItem[%registeredItem, "Name"] @ " in storage.");
			}
		}
		else
		{
			// Withdrawing from belt storage (old system)
			%cmnt = Belt::GetStored(%clientId, %opt);
			if(%cmnt >= %amnt)
			{
				Belt::GiveThisStuff(%clientId, %item, %amnt, 1);
				storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %item, -%amnt));
				%clientId.bulkNum = 1;
				RefreshAll(%clientId);
				SaveCharacter(%clientId);
				MenuBeltWithdrawThisItem(%clientId, %opt, 1, "withdraw");
			}
		}
	}
	return;
}
						
function MenuStoreBelt(%clientId, %mode)
{
	Client::buildMenu(%clientId, "Backpack " @ %mode @ ":", "StoreBelt", true);
	if(%mode == "store")
	{
		for(%i = 1; %i < Belt::GetLastItem(); %i++)
		if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]))
		Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i] @ " " @ %mode);
		
		Client::addMenuItem(%clientId, "nWithdraw Items", "mode store change");
		Client::addMenuItem(%clientId, "xFinished", "done");
	}
	else
	{
		%mode="withdraw";
		for(%i = 1; %i < Belt::GetLastItem(); %i++)
		{
			%fix = getWord(fetchData(%clientId, "Stored" @ $Belt::Categories[%i]), 2);
			if(%fix != -1)
				Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i] @ " " @ %mode);
		}
		
		Client::addMenuItem(%clientId, "nStore Items", "mode withdraw change");
		Client::addMenuItem(%clientId, "xFinished", "done");
	}
	return;
}

function processMenuStoreBelt(%clientId, %opt)
{
	%o = getword(%opt, 0);
	%m = getword(%opt, 1);
	%c = getword(%opt, 2);
	if(%c == "change")
	{
		if(%m == "store")
		MenuStoreBelt(%clientId, "withdraw");
		if(%m == "withdraw")
		MenuStoreBelt(%clientId, "store");
		return;
	}
	if(%m == "store")
	{
		MenuBeltStoreThisItem(%clientId, %o, 1, "store");
		return;
	}
	if(%m == "withdraw")
	{
		MenuBeltWithdrawThisItem(%clientId, %o, 1, "withdraw");
		return;
	}
	return;
}

function Belt::StorageConversion(%clientId)
{
	for(%i=0; getword(fetchData(%clientId, "StorageTruce"), %i) != -1; %i+=2)
	{
		%item = getword(fetchData(%clientId, "StorageTruce"), %i);
		if(isBeltItem(%item))
		{
			%itemcount = getword(fetchData(%clientId, "StorageTruce"), %i+1);
			%a = String::replace(fetchData(%clientId, "StorageTruce"), " " @ %item @ " " @ %itemcount, "");
			%list = String::NEWgetSubStr(%a, 0, 99999);
			storeData(%clientId, "StorageTruce", %list);
			Client::sendMessage(%clientId, $MsgBeige, "Now converting " @ %itemcount @ " " @ %item @ "s to your backpack storage.");
			%type = $BeltItem[%item, "Type"];
			if(%type == "")
				%type = "Other";
			storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %item, %itemcount));
		}
	}
}

function Belt::ItemsOnThemConversion(%clientId)
{
	for(%i=0; getword(fetchData(%clientId, "SpawnStuff"), %i) != -1; %i+=2)
	{
		%item = getword(fetchData(%clientId, "SpawnStuff"), %i);
		if(isBeltItem(%item))
		{
			%itemcount = getword(fetchData(%clientId, "SpawnStuff"), %i+1);
			%a = String::replace(fetchData(%clientId, "SpawnStuff"), " " @ %item @ " " @ %itemcount, "");
			%list = String::NEWgetSubStr(%a, 0, 99999);
			storeData(%clientId, "SpawnStuff", %list);
			Client::sendMessage(%clientId, $MsgBeige, "Now converting " @ %itemcount @ " " @ %item @ "s to your backpack storage.");
			%type = $BeltItem[%item, "Type"];
			if(%type == "")
				%type = "Other";
			storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %item, %itemcount));
		}
	}
}

function Belt::GetWeight(%clientId)
{
	//== HELPS REDUCE LAG WHEN THERE ARE SIMULTANEOUS CALLS ======
	%time = getIntegerTime(true);
	if(%time - %clientId.lastGetBeltWeight <= 1 && fetchData(%clientId, "tmpBeltWeight") != "")
	{
		%cached = fetchData(%clientId, "tmpBeltWeight");
		return %cached;
	}
	%clientId.lastGetBeltWeight = %time;
	//============================================================
	
	%weight = 0;
	for(%i = 1; $Belt::Categories[%i] != ""; %i++)
	{
		%category = $Belt::Categories[%i];
		%string = fetchData(%clientId, %category);
		for(%j = 0; (%item = GetWord(%string, %j)) != -1; %j+=2)
		{
			%count = GetWord(%string, %j+1);
			if(%item == "" || %item == -1 || %item == "0")
			{
				continue;
			}
			
			%itemWeight = $AccessoryVar[%item, $Weight];
			%addWeight = %itemWeight * %count;
			%weight += %addWeight;
		}
	}
	storeData(%clientId, "tmpBeltWeight", %weight);
	return %weight;
}

function Belt::GetNS(%clientId, %type)
{
	// CRITICAL SAFETY CHECK: GetNS should NEVER process storage categories
	// Storage categories: StoredQuestItems, StoredKeyItems, BeltStorage, StoredConsumables, StoredArmor, StoredAccessories, StoredOther
	// Equipped categories: QuestItems, KeyItems, Deployables, Consumables, Armor, Accessories, Other
	if(%type == "StoredQuestItems" || %type == "StoredKeyItems" || %type == "BeltStorage" || 
	   %type == "StoredConsumables" || %type == "StoredArmor" || %type == "StoredAccessories" || %type == "StoredOther")
	{
		%clientName = Client::getName(%clientId);
		if(%clientName == "") %clientName = "Unknown";
		echo("ERROR: Belt::GetNS - Attempted to process STORAGE category '" @ %type @ "' instead of EQUIPPED category!");
		echo("  GetNS should only be called with equipped categories (QuestItems, KeyItems, etc.)");
		echo("  ClientId: " @ %clientId @ " (" @ %clientName @ ")");
		return "0 ";
	}
	
	%count = 0;
	%stuff = fetchData(%clientId, %type);
	
	// CRITICAL FIX: Detect and fix missing spaces between count and item
	// Corrupted data like "2AlienSpine Enchanted Stone" needs to be fixed
	// Pattern: If a word starts with a digit and contains letters immediately after, it's corrupted
	// IMPORTANT: Only process words that actually start with a digit (0-9), not letters
	// This must be very strict to avoid false positives (e.g., "EnchantedStone" should NEVER be processed)
	%wasCorrupted = false;
	%fixedStuff = "";
	
	// Process words in pairs (item name, count)
	// Belt storage format is: "item1 count1 item2 count2 ..."
	for(%i = 0; GetWord(%stuff, %i) != -1; %i += 2)
	{
		%item = GetWord(%stuff, %i);
		%count = GetWord(%stuff, %i + 1);
		
		// If count is missing (end of string), item is corrupted or malformed
		if(%count == -1 || %count == "")
		{
			// Check if the item word itself looks corrupted (starts with digit + has letters)
			%firstChar = String::getSubStr(%item, 0, 1);
			if(%firstChar >= "0" && %firstChar <= "9" && String::len(%item) > 1)
			{
				// Try to split count from item (e.g., "2AlienSpine" -> "2" and "AlienSpine")
				%splitPoint = -1;
				for(%j = 1; %j < String::len(%item); %j++)
				{
					%char = String::getSubStr(%item, %j, 1);
				if((%char >= "a" && %char <= "z") || (%char >= "A" && %char <= "Z"))
				{
						%splitPoint = %j;
					break;
				}
					// Stop if we hit a non-digit
					if(%char < "0" || %char > "9")
						break;
				}
				
				if(%splitPoint > 0)
				{
					%countPart = String::getSubStr(%item, 0, %splitPoint);
					%itemPart = String::getSubStr(%item, %splitPoint, 99999);
					
					// Validate count part is numeric (but not scientific notation)
					// Check if countPart contains 'e' or 'E' which could be scientific notation
					%hasScientificNotation = (String::findSubStr(%countPart, "e") != -1 || String::findSubStr(%countPart, "E") != -1);
					
					if(!%hasScientificNotation)
					{
						%countNumeric = %countPart * 1;
						// Validate: numeric value should equal the string when converted back
						// This catches cases where scientific notation would parse differently
						if(%countNumeric == %countPart && %countPart != "" && %countNumeric > 0)
						{
							// Valid corruption - separate count and item
							if(%fixedStuff != "")
								%fixedStuff = %fixedStuff @ " ";
							%fixedStuff = %fixedStuff @ %itemPart @ " " @ %countPart;
							%wasCorrupted = true;
							continue;
						}
					}
					// If it contains 'e'/'E', it might be scientific notation or part of item name
					// Skip this entry to avoid misinterpreting it
				}
			}
			// Not corrupted or couldn't split - skip this malformed entry
			continue;
		}
		
		// CRITICAL: Never parse counts as scientific notation - extract numeric prefix if corrupted
		// If count contains 'e'/'E', extract just the numeric prefix (e.g., "3" from "3AlienSpine")
			%hasScientificNotation = (String::findSubStr(%count, "e") != -1 || String::findSubStr(%count, "E") != -1);
		%originalCount = %count;
			
		if(%hasScientificNotation || %count == "" || %count == -1)
			{
			// Count contains 'e'/'E' or is invalid - try to extract numeric prefix
			// This handles corruption like "3AlienSpine" where space is missing
			%numericPrefix = "";
			%digitString = "0123456789";
			for(%k = 0; %k < String::len(%count); %k++)
			{
				%char = String::getSubStr(%count, %k, 1);
				// Check if character is a digit (0-9 only) - use String::findSubStr to avoid TorqueScript == comparison bug ("E" == "0" evaluates to true)
				if(String::findSubStr(%digitString, %char) != -1)
				{
					%numericPrefix = %numericPrefix @ %char;
				}
				else
				{
					// Hit non-digit character - stop extracting
					break;
				}
			}
			
			if(%numericPrefix != "" && %numericPrefix != "0")
			{
				// Successfully extracted numeric prefix - use it instead
				%count = %numericPrefix;
					%clientName = Client::getName(%clientId);
					if(%clientName == "") %clientName = "Unknown";
				echo("WARNING: Belt::GetNS - Count '" @ %originalCount @ "' contains 'e'/'E' or non-numeric in " @ %type @ ". Extracted numeric prefix: '" @ %count @ "'");
				echo("  ClientId: " @ %clientId @ " (" @ %clientName @ "), Item: '" @ %item @ "', Original Count: '" @ %originalCount @ "', Using: '" @ %count @ "'");
				}
				else
				{
				// No valid numeric prefix found - skip this entry
					%clientName = Client::getName(%clientId);
					if(%clientName == "") %clientName = "Unknown";
				echo("WARNING: Belt::GetNS - Count '" @ %originalCount @ "' contains 'e'/'E' and has no valid numeric prefix in " @ %type @ ". Skipping entry.");
				echo("  ClientId: " @ %clientId @ " (" @ %clientName @ "), Item: '" @ %item @ "', Count: '" @ %originalCount @ "'");
					continue; // Skip this entry
				}
			}
		
		// Validate that count is now a pure integer (only digits)
		%isPureInteger = true;
		for(%k = 0; %k < String::len(%count); %k++)
		{
			%char = String::getSubStr(%count, %k, 1);
			if(%char < "0" || %char > "9")
			{
				%isPureInteger = false;
				break;
			}
		}
		
		// Only accept pure integer counts - skip any that still aren't valid
		if(!%isPureInteger || %count == "" || %count == -1)
				{
			%clientName = Client::getName(%clientId);
			if(%clientName == "") %clientName = "Unknown";
			echo("WARNING: Belt::GetNS - Count '" @ %count @ "' is not a pure integer in " @ %type @ ". Corruption detected, skipping entry.");
			echo("  ClientId: " @ %clientId @ " (" @ %clientName @ "), Item: '" @ %item @ "', Count: '" @ %count @ "'");
			continue; // Skip this entry
		}
		
		// Now safely parse as integer (no scientific notation possible - we've already extracted numeric prefix)
		%countNumeric = %count * 1;
		
		// Normal entry - add item and count as-is
			if(%fixedStuff != "")
				%fixedStuff = %fixedStuff @ " ";
		%fixedStuff = %fixedStuff @ %item @ " " @ %count;
	}
	
	// If corruption was fixed, use the fixed list and log it
	if(%wasCorrupted)
	{
		%clientName = Client::getName(%clientId);
		echo("CRITICAL: Belt::GetNS - Fixed corrupted data in " @ %type @ " (missing spaces between count and item). ClientId: " @ %clientId @ " (" @ %clientName @ ")");
		echo("  Original: '" @ %stuff @ "'");
		echo("  Fixed: '" @ %fixedStuff @ "'");
		%stuff = %fixedStuff;
	}
	
	// Clean up invalid entries (item "0", empty items, count 0 or negative)
	// Handle corrupted lists by iterating word-by-word and properly pairing items with counts
	%cleanedStuff = "";
	%hasInvalidEntries = false;
	for(%i = 0; GetWord(%stuff, %i) != -1; %i += 2)
	{
		%item = GetWord(%stuff, %i);
		%itemCount = GetWord(%stuff, %i + 1);
		
		// Skip invalid entries
		if(%item == "" || %item == -1 || %item == "0" || %itemCount == "" || %itemCount == -1 || %itemCount == "0" || %itemCount == "-1" || (%itemCount * 1) <= 0)
		{
			%hasInvalidEntries = true;
			continue;
		}
		
		// Add valid entry
		if(%cleanedStuff != "")
			%cleanedStuff = %cleanedStuff @ " ";
		%cleanedStuff = %cleanedStuff @ %item @ " " @ %itemCount;
	}
	
	// If we cleaned invalid entries, save the cleaned version back
	if(%hasInvalidEntries)
	{
		%len = String::len(%cleanedStuff);
		if(%len > 0 && String::getSubStr(%cleanedStuff, %len-1, 1) == " ")
			%cleanedStuff = String::getSubStr(%cleanedStuff, 0, %len-1);
		storeData(%clientId, %type, %cleanedStuff);
		%stuff = %cleanedStuff;
	}
	else
	{
		// Use original stuff if no cleaning was needed
		%cleanedStuff = %stuff;
	}
	
	// Build list from cleaned stuff (items that passed validation)
	%list = "";
	%count = 0;
	for(%i = 0; GetWord(%cleanedStuff, %i) != -1; %i+=2)
	{
		%item = GetWord(%cleanedStuff, %i);
		%itemCount = GetWord(%cleanedStuff, %i+1);
		// Only add valid entries (already validated in cleanup loop above)
		if(%item != "" && %item != -1 && %item != "0" && %itemCount > 0)
		{
			%list = %list @ " " @ %item;
			%count++;
		}
	}
	
	return %count @ " " @ %list;
}

function Belt::GetSellCost(%clientId, %item)
{
	%p = $HardcodedItemCost[%item];
	%cost = round(%p * ($resalePercentage/100));

	%p = round($PlayerSkill[%clientId, $SkillHaggling] / 11) / 100;
	%x = round(%cost * Cap(%p, 0.0, 1.0) );
	%cost += %x;

	return %cost;
}

function Belt::HasThisStuff(%clientId, %item)
{
	%item = $BeltItem[%item, "Item"];
	%type = $BeltItem[%item, "Type"];
	%list = fetchData(%clientId, %type);
	%amnt = Belt::ItemCount(%item, %list);
	return %amnt;
}

//==========================//
//====== MUG BELT END ======//
//==========================//

function Belt::GiveThisStuff(%clientId, %item, %amnt, %echo)
{
	// Check if this is an enemy bot early - bots need optimized path with fewer checks
	%isBot = isRPGAI(%clientId);
	
	if(%amnt > 0)
	{
		%originalItem = %item;
		%item = $BeltItem[%item, "Item"];
		%type = $BeltItem[%item, "Type"];
		
		// CRITICAL SAFETY CHECK: Ensure we're only using EQUIPPED categories, never storage categories
		// Storage categories: StoredQuestItems, StoredKeyItems, BeltStorage, StoredConsumables, StoredArmor, StoredAccessories, StoredOther
		// Equipped categories: QuestItems, KeyItems, Deployables, Consumables, Armor, Accessories, Other
		if(%type == "StoredQuestItems" || %type == "StoredKeyItems" || %type == "BeltStorage" || 
		   %type == "StoredConsumables" || %type == "StoredArmor" || %type == "StoredAccessories" || %type == "StoredOther")
		{
			%clientName = Client::getName(%clientId);
			if(%clientName == "") %clientName = "Unknown";
			echo("ERROR: Belt::GiveThisStuff - Item '" @ %originalItem @ "' is registered with STORAGE category '" @ %type @ "' instead of EQUIPPED category!");
			echo("  This should never happen - belt items should be registered with equipped categories (QuestItems, KeyItems, etc.)");
			echo("  ClientId: " @ %clientId @ " (" @ %clientName @ ")");
			return;
		}
		
		// Debug logging for quest items (especially AlienSpine)
		if(%isBot && (%originalItem == "AlienSpine" || %item == "AlienSpine"))
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = "Unknown";
			//echo("[LOOT DEBUG] Belt::GiveThisStuff: Giving " @ %amnt @ " " @ %originalItem @ " to bot " @ %botName @ " (clientId=" @ %clientId @ ")");
			//echo("[LOOT DEBUG]   Lookup: originalItem='" @ %originalItem @ "', registeredItem='" @ %item @ "', type='" @ %type @ "'");
		}
		
		// Validate that the item was found in the belt item registry (for all players, not just AI)
		if(%item == "" || %type == "")
		{
			%clientName = Client::getName(%clientId);
			if(%clientName == "") %clientName = "Unknown";
			if(%isBot)
			{
				%botName = fetchData(%clientId, "BotInfoAiName");
				if(%botName == "" || %botName == "0") %botName = "Unknown";
				%clientName = "bot " @ %botName;
			}
			echo("ERROR: Belt::GiveThisStuff - Item '" @ %originalItem @ "' not found in belt item registry for " @ %clientName @ " (ClientId: " @ %clientId @ ")");
			echo("  Lookup result: item='" @ %item @ "', type='" @ %type @ "'");
			return;
		}
		
		%list = fetchData(%clientId, %type);
		
		// CRITICAL: fetchData returns "0" for non-existent variables in TorqueScript
		// Convert "0" to empty string to prevent it from being treated as a valid list entry
		if(%list == "0")
			%list = "";
		
		// For enemy bots: Skip corruption checks and GetNS to reduce load
		// Bots are controlled by server, corruption is unlikely, and GetNS causes race conditions
		if(%isBot)
		{
			// Fast path for bots: minimal validation, skip GetNS, skip most debug output
			// Handle "0" entry cleanup - check if list starts with "0" and remove it
			// (This handles cases where "0" might be part of a corrupted list like "0 AlienSpine 1")
			%firstWord = GetWord(%list, 0);
			if(%firstWord == "0")
			{
				// List starts with "0" - remove it and the next word (which might be an item name or count)
				// This handles both "0" (empty) and "0 AlienSpine 1" (corrupted) cases
				%cleanedList = "";
				for(%i = 2; GetWord(%list, %i) != -1; %i++)
				{
					%word = GetWord(%list, %i);
					if(%cleanedList != "")
						%cleanedList = %cleanedList @ " " @ %word;
					else
						%cleanedList = %word;
				}
				%list = %cleanedList;
				storeData(%clientId, %type, %list);
			}
		}
		else
		{
			// Full path for players: corruption checks, GetNS cleanup, full debug output
			// Clean up corrupted entries before processing (items starting with "0", empty, invalid counts, etc.)
			%testItem = GetWord(%list, 0);
			%testCount = GetWord(%list, 1);
			%isCorrupted = False;
			if(%testItem != "" && %testItem != -1)
			{
				// Try to get count for the first item - if it's non-numeric, the list is corrupted
				%testLookup = Belt::ItemCount(%testItem, %list);
				%testNumeric = %testLookup * 1;
				if(%testLookup != "" && %testNumeric != %testLookup && %testLookup != "0")
				{
					// Count is non-numeric - list is severely corrupted (e.g., "10KronoStone" merged count+item)
					%clientName = Client::getName(%clientId);
					echo("CRITICAL: Belt::GiveThisStuff - List for " @ %type @ " is severely corrupted (non-numeric count: '" @ %testLookup @ "'). Logging only, not fixing.");
					echo("  Corrupted list: '" @ %list @ "'");
					
					// CORRUPTION FIXES DISABLED - Only logging
					// Log the corruption but don't attempt to fix
					Belt::LogCorruptionFix(%clientId, %type, %list, %list);
					// Keep original list - don't modify
					// %fixedList = Belt::FixCorruptedList(%list);
					// if(%fixedList != "" && %fixedList != "0")
					// {
					// 	%list = %fixedList;
					// 	storeData(%clientId, %type, %list);
					// 	echo("  Fixed list: '" @ %list @ "'");
					// }
					// else
					// {
					// 	// Fix failed, reset as last resort
					// 	echo("  WARNING: Fix failed, resetting category as last resort");
					// 	%isCorrupted = True;
					// 	%list = "";
					// 	storeData(%clientId, %type, "");
					// }
				}
			}
			
			// For players: Use Belt::GetNS to clean and rebuild
			if(!%isCorrupted)
			{
				%nsResult = Belt::GetNS(%clientId, %type);
				%itemCount = GetWord(%nsResult, 0);
				
				// Use the cleaned list that Belt::GetNS just saved
				%cleanedList = fetchData(%clientId, %type);
				
				// If the cleaned list is just "0" or invalid, treat it as empty
				if(%cleanedList == "0" || (GetWord(%cleanedList, 0) == "0" && GetWord(%cleanedList, 1) == ""))
				{
					%cleanedList = "";
					storeData(%clientId, %type, "");
				}
				
				%list = %cleanedList;
			}
		}
		
		%count = Belt::ItemCount(%item, %list);
		
		// DEBUG: Log when bots receive belt items (quest items, weapons, etc.)
		if(%isBot && (%type == "QuestItems" || %type == "KeyItems" || %type == "Consumables"))
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
		}
		
		// If items exist in equipped category when picking up from lootbag,
		// consolidate them normally with the picked up items.
		// This is normal if picking up an enemy lootbag while alive.
		// Only log a warning if count is suspiciously high (> 100) to flag potential duplication issues,
		// but still consolidate normally (don't erase items)
		if(%count > 0 && !%isBot)
		{
			if(%count > 100)
			{
				// Suspiciously high count - log a warning but still consolidate
			%clientName = Client::getName(%clientId);
				echo("WARNING: Belt::GiveThisStuff - Item '" @ %item @ "' has suspiciously high count (" @ %count @ ") in " @ %type @ " when picking up from lootbag! ClientId: " @ %clientId @ " (" @ %clientName @ ")");
				echo("  This may indicate duplication/corruption, but items will still be consolidated normally.");
				// Display the current list as-is (this is just for debugging - the list will be fixed by Belt::AddToList after this)
				echo("  Current list (before consolidation): '" @ %list @ "'");
			}
		}

		if(%echo && !%isBot) Client::sendMessage(%clientId, 0, "You received " @ %amnt @ " " @ $BeltItem[%item, "Name"] @ ".~loot");

		if(%count > 0)
		{
			// CRITICAL: Use UpdateCountInPlace to preserve item position in list
			// This prevents belt reordering when adding more of an existing item
			%newCount = %count + %amnt;
			%list = Belt::UpdateCountInPlace(%list, %item, %newCount);
		}
		else
		{
			// Item doesn't exist yet - add it to the list (will append to end - this is expected for new items)
			%list = Belt::AddToList(%list, %item @ " " @ %amnt);
		}
		
		// DEBUG: Log final list after adding item for bots
		if(%isBot && (%type == "QuestItems" || %type == "KeyItems" || %type == "Consumables"))
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = Client::getName(%clientId);
			%verifyCount = Belt::ItemCount(%item, %list);
		}
		
		// CRITICAL: Keep trailing space to match field 15 (spawnStuff) format
		// Field 15 uses format: "item1 count1 item2 count2 " (with trailing space)
		// This ensures proper spacing when items are added later and prevents corruption
		// Do NOT remove trailing space - it's required for consistency with inventory items

		storeData(%clientId, %type, %list);
		
		// Build AllBelt - ensure proper spacing between QuestItems and KeyItems to prevent corruption
		%questItems = fetchData(%clientId, "QuestItems");
		if(%questItems == "0" || %questItems == "")
			%questItems = "";
		%keyItems = fetchData(%clientId, "KeyItems");
		if(%keyItems == "0" || %keyItems == "")
			%keyItems = "";
		
		// CRITICAL: Add space between QuestItems and KeyItems to prevent items from merging
		// Example: If QuestItems ends with "1" and KeyItems starts with "DemonBreath", 
		// without a space it becomes "1DemonBreath" (corruption)
		if(%questItems != "" && %keyItems != "")
			%allBelt = %questItems @ " " @ %keyItems;
		else if(%questItems != "")
			%allBelt = %questItems;
		else if(%keyItems != "")
			%allBelt = %keyItems;
		else
			%allBelt = "";
		storeData(%clientId, "AllBelt", %allBelt);
		
		// Debug logging for quest items
		if(%isBot && (%originalItem == "AlienSpine" || %item == "AlienSpine"))
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = "Unknown";
			//echo("[LOOT DEBUG] Belt::GiveThisStuff: After store - stored list='" @ %list @ "', cached list='" @ $Belt::CachedList[%clientId, %type] @ "'");
		}
		
		// Debug logging for quest items (especially AlienSpine)
		if(%isBot && (%originalItem == "AlienSpine" || %item == "AlienSpine"))
		{
			%botName = fetchData(%clientId, "BotInfoAiName");
			if(%botName == "" || %botName == "0") %botName = "Unknown";
			%finalCount = Belt::ItemCount(%item, %list);
			//echo("[LOOT DEBUG] Belt::GiveThisStuff: Successfully added " @ %amnt @ " " @ %item @ " to bot " @ %botName @ ". Final count in " @ %type @ ": " @ %finalCount);
			//echo("[LOOT DEBUG]   Final list: '" @ %list @ "'");
		}

		// Phase D: a change to the belt-weapon list re-pushes this client's native
		// inventory rows (VirtualSlots.cs). No-op unless $pref::VSlotsEnabled;
		// Sync itself skips HUD clients and bots.
		if(%type == "Weapons")
			VSlot::Sync(%clientId);
	}
}

function Belt::TakeThisStuff(%clientId, %item, %amnt)
{
	if(%amnt > 0)
	{
		%isBot = isRPGAI(%clientId);
		%originalItem = %item;
		%item = $BeltItem[%item, "Item"];
		%type = $BeltItem[%item, "Type"];
		
		// Validate item was found
		if(%item == "" || %type == "")
		{
			%clientName = Client::getName(%clientId);
			echo("ERROR: Belt::TakeThisStuff - Item '" @ %originalItem @ "' not found in belt item registry for " @ %clientName @ " (ClientId: " @ %clientId @ ")");
			return;
		}
		
		%list = fetchData(%clientId, %type);
		
		// Performance fast-path for bots:
		// bots are server-controlled and already use streamlined add paths,
		// so skip expensive corruption scanning and GetNS normalization here.
		if(%isBot)
		{
			if(%list == "0")
				%list = "";
		}
		else
		{

		// Clean up corrupted entries before processing (items starting with "0", empty, invalid counts, etc.)
		// Check if list is severely corrupted (non-numeric counts indicate corruption)
		// Scan entire list, not just first item, to catch corruption like "BlackStatue 3KronoStone 14"
		%isCorrupted = False;
		if(%list != "" && %list != "0")
		{
			// Check all items in the list for corruption
			for(%i = 0; GetWord(%list, %i) != -1; %i += 2)
			{
				%testItem = GetWord(%list, %i);
				%testCount = GetWord(%list, %i + 1);
				
				if(%testItem != "" && %testItem != -1 && %testCount != "" && %testCount != -1)
				{
					// Validate the count we got from GetWord is numeric
					%testCountNum = %testCount * 1;
					if(%testCountNum == %testCount && %testCount != "0" && %testCountNum > 0)
					{
						// Count is valid numeric - verify ItemCount returns the same value
						%testLookup = Belt::ItemCount(%testItem, %list);
						%testLookupNum = %testLookup * 1;
						
						// Only flag as corrupted if ItemCount returns something completely different
						// Allow small differences due to parsing, but not non-numeric values
						if(%testLookup != "" && %testLookup != "0" && %testLookupNum != %testLookup)
						{
							// ItemCount returned non-numeric value - this indicates corruption
							%clientName = Client::getName(%clientId);
							if(%clientName == "") %clientName = "Unknown";
							echo("CRITICAL: Belt::TakeThisStuff - List for " @ %type @ " is severely corrupted (non-numeric count: '" @ %testLookup @ "'). Logging only, not fixing.");
							echo("  Corrupted list: '" @ %list @ "'");
							echo("  ClientId: " @ %clientId @ " (" @ %clientName @ ")");
							echo("  Item: '" @ %testItem @ "', Expected count: '" @ %testCount @ "', ItemCount returned: '" @ %testLookup @ "'");
							
							// CORRUPTION FIXES DISABLED - Only logging
							// Log the corruption but don't attempt to fix
							Belt::LogCorruptionFix(%clientId, %type, %list, %list);
							// Keep original list - don't modify
							// %fixedList = Belt::FixCorruptedList(%list);
							// if(%fixedList != "" && %fixedList != "0")
							// {
							// 	%list = %fixedList;
							// 	storeData(%clientId, %type, %list);
							// 	echo("  Fixed list: '" @ %list @ "'");
							// }
							// else
							// {
							// 	// Fix failed, reset as last resort
							// 	echo("  WARNING: Fix failed, resetting category as last resort");
							// 	%isCorrupted = True;
							// 	%list = "";
							// 	storeData(%clientId, %type, "");
							// }
							break; // Exit loop once corruption is found and handled
						}
					}
					// If testCount is non-numeric, that's also corruption
					else if(%testCount != "0" && (%testCountNum != %testCount || %testCountNum <= 0))
					{
						// Count from GetWord is non-numeric - this is corruption
						%clientName = Client::getName(%clientId);
						if(%clientName == "") %clientName = "Unknown";
						echo("CRITICAL: Belt::TakeThisStuff - List for " @ %type @ " is severely corrupted (non-numeric count from GetWord: '" @ %testCount @ "'). Logging only, not fixing.");
						echo("  Corrupted list: '" @ %list @ "'");
						echo("  ClientId: " @ %clientId @ " (" @ %clientName @ ")");
						echo("  Item: '" @ %testItem @ "', Count: '" @ %testCount @ "'");
						
						// CORRUPTION FIXES DISABLED - Only logging
						// Log the corruption but don't attempt to fix
						Belt::LogCorruptionFix(%clientId, %type, %list, %list);
						// Keep original list - don't modify
						// %fixedList = Belt::FixCorruptedList(%list);
						// if(%fixedList != "" && %fixedList != "0")
						// {
						// 	%list = %fixedList;
						// 	storeData(%clientId, %type, %list);
						// 	echo("  Fixed list: '" @ %list @ "'");
						// }
						// else
						// {
						// 	// Fix failed, reset as last resort
						// 	echo("  WARNING: Fix failed, resetting category as last resort");
						// 	%isCorrupted = True;
						// 	%list = "";
						// 	storeData(%clientId, %type, "");
						// }
						break; // Exit loop once corruption is found and handled
					}
				}
			}
		}
		
		// If not corrupted, use Belt::GetNS to clean and rebuild
		// Note: Removed the aggressive pattern detection that was incorrectly flagging valid strings
		// The ItemCount check above should catch actual corruption (non-numeric counts)
		
		if(!%isCorrupted)
		{
			%nsResult = Belt::GetNS(%clientId, %type);
			%itemCount = GetWord(%nsResult, 0);
			
			// Use the cleaned list that Belt::GetNS just saved
			%cleanedList = fetchData(%clientId, %type);
			
			// If the cleaned list is just "0" or invalid, treat it as empty
			if(%cleanedList == "0" || (GetWord(%cleanedList, 0) == "0" && GetWord(%cleanedList, 1) == ""))
			{
				%cleanedList = "";
				storeData(%clientId, %type, "");
			}
			
			%list = %cleanedList;
		}
		}
		
		%count = Belt::ItemCount(%item, %list);
		
		if(%count < %amnt)
	{
		%clientName = Client::getName(%clientId);
			echo("WARNING: Belt::TakeThisStuff - Attempting to remove " @ %amnt @ " of item '" @ %item @ "', but only " @ %count @ " available. ClientId: " @ %clientId @ " (" @ %clientName @ ")");
			%amnt = %count; // Only remove what's available
		}
		
		%remainingAmnt = %count - %amnt;

		// CRITICAL: Use UpdateCountInPlace to preserve item position in list
		// This prevents belt reordering when item counts change
		%list = Belt::UpdateCountInPlace(%list, %item, %remainingAmnt);
		
		if(%remainingAmnt <= 0)
		{
			// CRITICAL: If item was completely removed, ensure trailing space is preserved for equipped categories
			// Equipped categories (QuestItems, KeyItems, Consumables, Armor, Accessories, Other) need trailing space
			// This matches field 15 (spawnStuff) format: "item1 count1 item2 count2 " (with trailing space)
			// Also ensure proper spacing between items (no merging like "15BlackStatue")
			if(%list != "" && %list != "0")
			{
				%len = String::len(%list);
				// Ensure trailing space exists (required for equipped items format)
				if(%len > 0 && String::getSubStr(%list, %len-1, 1) != " ")
				{
					%list = %list @ " ";
				}
				
				// CRITICAL: Check for merged items (like "15BlackStatue") and fix spacing
				// This can happen if RemoveFromList didn't preserve spacing correctly
				%fixedList = "";
				%wordCount = 0;
				for(%i = 0; GetWord(%list, %i) != -1; %i++)
					%wordCount++;
				
				// Rebuild list with proper spacing between items
				for(%i = 0; %i < %wordCount; %i += 2)
				{
					%itemName = GetWord(%list, %i);
					%itemCount = GetWord(%list, %i + 1);
					
					if(%itemName != "" && %itemName != -1 && %itemCount != "" && %itemCount != -1)
					{
						if(%fixedList != "")
							%fixedList = %fixedList @ " ";
						%fixedList = %fixedList @ %itemName @ " " @ %itemCount;
					}
				}
				
				// Add trailing space for equipped items format
				if(%fixedList != "")
					%fixedList = %fixedList @ " ";
				
				%list = %fixedList;
			}
		}

		// Normalize empty lists - store as empty string, not "0"
		if(%list == "" || %list == "0")
		{
			%list = "";
		}
		
		storeData(%clientId, %type, %list);
		
		// Build AllBelt - ensure each category is normalized (empty or "0" becomes "")
		// CRITICAL: Add space between QuestItems and KeyItems to prevent items from merging
		%questItems = fetchData(%clientId, "QuestItems");
		if(%questItems == "0" || %questItems == "")
			%questItems = "";
		
		%keyItems = fetchData(%clientId, "KeyItems");
		if(%keyItems == "0" || %keyItems == "")
			%keyItems = "";
		
		// CRITICAL: Add space between QuestItems and KeyItems to prevent items from merging
		// Example: If QuestItems ends with "1" and KeyItems starts with "DemonBreath", 
		// without a space it becomes "1DemonBreath" (corruption)
		if(%questItems != "" && %keyItems != "")
			%allBelt = %questItems @ " " @ %keyItems;
		else if(%questItems != "")
			%allBelt = %questItems;
		else if(%keyItems != "")
			%allBelt = %keyItems;
		else
			%allBelt = "";
		storeData(%clientId, "AllBelt", %allBelt);

		// Phase D: mirror GiveThisStuff - re-push native inventory rows when the
		// belt-weapon list shrank (sell/drop/bank). No-op unless
		// $pref::VSlotsEnabled; Sync skips HUD clients and bots.
		if(%type == "Weapons")
			VSlot::Sync(%clientId);
	}
}

function Belt::LogCorruptionFix(%clientId, %category, %original, %fixed)
{
	// Log corruption fix for debugging purposes
	%clientName = Client::getName(%clientId);
	if(%clientName == "") %clientName = "Unknown";
	
	// Echo to console only - file writing was causing server crashes
	echo("[BELT CORRUPTION FIX] ClientId: " @ %clientId @ " (" @ %clientName @ "), Category: " @ %category);
	echo("  Original: '" @ %original @ "'");
	echo("  Fixed: '" @ %fixed @ "'");
}

function Belt::ItemCount(%item, %list)
{
	%count = GetStuffStringCount(%list, %item);
	return %count;
}

// CORRUPTION FIXES DISABLED - This function now only returns the original list unchanged
// Fix corrupted belt list where count and item name are merged (e.g., "10KronoStone" -> "10" and "KronoStone")
function Belt::FixCorruptedList(%list)
{
	// CORRUPTION FIXES DISABLED - Just return the original list unchanged
	// All fixing logic has been commented out - only logging remains active
	if(%list == "" || %list == "0")
		return "";
	
	// Return original list unchanged - no fixes applied
			return %list;
	
	// ALL FIXING LOGIC HAS BEEN REMOVED - CORRUPTION FIXES ARE DISABLED
	// Original fixing code was removed since it didn't work correctly
}

function Belt::isEmpty(%clientId, %type)
{
	%stuff = fetchData(%clientId, %type);
	if(getWord(%stuff, 0) != -1 && getWord(%stuff, 1) != -1)
		return False;
	else
		return True;
}

// ------------------- //
// Non Menu Functions  //
// ------------------- //

//==========================//
//======MUG BELT START======//
//==========================//
// TEMPORARILY DISABLED - Belt::Mug functions
//==========================//
//====== MUG BELT END ======//
//==========================//

function MenuBeltWithdrawThisItem(%clientId, %type, %page, %mode)
{
	%type = getword(%type, 0);
	%disp = Belt::Display(%type);
	
	// Store the original order when first opening the menu (page 1)
	if(%page == 1 && %clientId.beltStoredItemOrder[%type] == "")
	{
		%storedStuff = fetchdata(%clientId, "Stored" @ %type);
		%orderList = "";
		for(%i = 0; (%item = getWord(%storedStuff, %i)) != -1; %i+=2)
		{
			%amnt = getWord(%storedStuff, %i + 1);
			if(%amnt > 0)
			{
				%orderList = %orderList @ " " @ %item;
			}
		}
		%clientId.beltStoredItemOrder[%type] = String::NEWgetSubStr(%orderList, 1, 99999);
	}
	
	Client::buildMenu(%clientId, %disp @ ":", "BeltWithdrawThisItem", true);

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = fetchdata(%clientId, "Stored" @ %type);
	
	// Use stored order if available
	if(%clientId.beltStoredItemOrder[%type] != "")
	{
		%storedOrder = %clientId.beltStoredItemOrder[%type];
		%orderedList = "";
		%count = 0;
		
		// Build ordered list based on stored order
		for(%i = 0; (%item = getWord(%storedOrder, %i)) != -1; %i++)
		{
			%amnt = Belt::ItemCount(%item, %nf);
	if(%amnt > 0)
	{
				%orderedList = %orderedList @ " " @ %item @ " " @ %amnt;
				%count++;
			}
		}
		%nf = String::NEWgetSubStr(%orderedList, 1, 99999);
		%ns = %count - 1;
	}
	else
	{
		%ns = (CountObjInList(%nf) / 2) - 1;
	}
	
	%np = floor((%ns - 1) / %l);	// review #37: 0-indexed last page = ceil(count/pageSize)-1. floor(ns/l) was one too high when the count was an exact multiple of 6, rendering a phantom empty "Next" page.
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = (%lb - 1) * 2 + 2;
	for(%i = %lb; %i <= %ub; %i++)
	{	
		%item = getword(%nf, %x);
		%amnt = getword(%nf, %x + 1);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ %item, %item @ " " @ %page @ " " @ %type @ " " @ %mode);
		%x+=2;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type @ " " @ %mode);
	}

	return;
}
function processMenuBeltWithdrawThisItem(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);
	%mode = getword(%opt, 3);

	if(%o == "back")
	{
		MenuStoreBelt(%clientId, %mode);
		return;
	}

	if(%o != "page" && %o != "done")
	{
		MenuSellBeltItemFinal(%clientId, %o, %t, %mode);
		return;
	}

	if(%o != "done")
	MenuBeltWithdrawThisItem(%clientId, %t, %p, %mode);

	return;
}
function MenuBeltStoreThisItem(%clientId, %type, %page, %mode)
{
	%type = getword(%type, 0);
	%disp = Belt::Display(%type);
	
	%msg = "<jc><f2>To store 'bulk' " @ %disp @ ", please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
	
	// Store the original order when first opening the menu (page 1)
	if(%page == 1 && %clientId.beltItemOrder[%type] == "")
	{
		%stuff = fetchData(%clientId, %type);
		%orderList = "";
		for(%i = 0; (%item = getWord(%stuff, %i)) != -1; %i+=2)
		{
			%amnt = getWord(%stuff, %i + 1);
			if(%amnt > 0)
			{
				%orderList = %orderList @ " " @ %item;
			}
		}
		%clientId.beltItemOrder[%type] = String::NEWgetSubStr(%orderList, 1, 99999);
	}
	
	Client::buildMenu(%clientId, %disp @ ":", "BeltStoreThisItem", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = Belt::GetNS(%clientId, %type);
	%ns = GetWord(%nf, 0);
	%np = floor((%ns - 1) / %l);	// review #37: 0-indexed last page = ceil(count/pageSize)-1. floor(ns/l) was one too high when the count was an exact multiple of 6, rendering a phantom empty "Next" page.
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = %lb;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%item = getword(%nf, %x);
		%amnt = Belt::HasThisStuff(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type @ " " @ %mode);
		%x++;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "bBack", "back " @ %type @ " " @ %mode);
	}

	return;
}
function processMenuBeltStoreThisItem(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);
	%mode = getword(%opt, 3);

	if(%o == "back")
	{
		MenuStoreBelt(%clientId, %mode);
		return;
	}

	if(%o != "page" && %o != "done")
	{
		MenuSellBeltItemFinal(%clientId, %o, %t, %mode);
		return;
	}

	if(%o != "done")
	MenuBeltStoreThisItem(%clientId, %t, %p, %mode);

	return;
}

// ------------------- //
// Non Menu Functions  //
// ------------------- //

//==========================//
//======MUG BELT START======//
//==========================//
// TEMPORARILY DISABLED - Belt::Mug functions
//function Belt::Mug(%clientId, %id)
//{
//	%clientId.MugID=%id;
//	Belt::MugBeltMain(%clientId, %id);
//	client::sendmessage(%clientId, "You are belt mugging");
//}
//function Belt::MugBeltMain(%clientId, %id)
//{
//	Client::buildMenu(%clientId, client::getname(%id) @ "'s Belt:", "MugBeltMain", true);
//	for(%i = 1; %i < Belt::GetLastItem(); %i++)
//	if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]) > 0)
//	Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i]);	
//	Client::addMenuItem(%clientId, "xDone", "done");
//	return;
//}
//function processMenuMugBeltMain(%clientId, %opt)
//{
//	%o = GetWord(%opt, 0);
//	%p = GetWord(%opt, 1);
//
//	if($Belt::Count[%o] > 0)
//	{
//		MenuBelt::MugThisType(%clientId, %o, 1);
//		return;
//	}
//	return;
//}
//function MenuBelt::MugThisType(%clientId, %type, %page)
//{
//	%id=%clientId.MugId;
//	%disp = Belt::Display(%type);
//
//	Client::buildMenu(%clientId, %disp @ ":", "Belt::MugThisType", true);
//	%clientId.bulkNum = "";
//
//	%l = 6;
//	%nx = $Belt::Count[%type];
//	%nf = Belt::GetNS(%id, %type);
//	%ns = GetWord(%nf, 0);
//	%np = floor((%ns - 1) / %l);	// review #37: 0-indexed last page = ceil(count/pageSize)-1. floor(ns/l) was one too high when the count was an exact multiple of 6, rendering a phantom empty "Next" page.
//	%lb = (%page * %l) - (%l-1);
//	%ub = %lb + (%l-1);
//	if(%ub > %ns)
//	%ub = %ns;
//
//	%x = %lb - 1;
//	for(%i = %lb; %i <= %ub; %i++)
//	{
//		%x++;
//		%item = getword(%nf, %x);
//		%amnt = Belt::HasThisStuff(%id, %item);
//		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type);
//	}
//
//	if(%page == 1)
//	{
//		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
//		Client::addMenuItem(%clientId, "xDone", "done");
//	}
//	else if(%page == %np+1)
//	{
//		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
//		Client::addMenuItem(%clientId, "xDone", "done");
//	}
//	else
//	{
//		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
//		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
//	}
//
//	return;
//}
//function processMenuBelt::MugThisType(%clientId, %opt)
//{
//	%o = GetWord(%opt, 0);
//	%p = GetWord(%opt, 1);
//	%t = GetWord(%opt, 2);
//
//	if(%o != "page" && %o != "done")
//	{
//		%clientId.bulkNum = 1;
//
//		MenuBelt::MugChoose(%clientId, %o, %t);
//		return;
//	}
//
//	return;
//}
//function MenuBelt::MugChoose(%clientId, %item, %type)
//{
//	%id=%clientId.MugId;
//	%name = $BeltItem[%item, "Name"];
//	%amnt = %clientId.bulkNum;
//	%cmnt = Belt::HasThisStuff(%id, %item);
//
//	if(%amnt > %cmnt)
//	%amnt = %cmnt;
//
//	Client::buildMenu(%clientId, %name @ " (" @ %cmnt @ ")", "Belt::MugChoose", true);
//
//	Client::addMenuItem(%clientId, %cnt++ @ "Mug " @ %item, %type @ " MUG " @ %item @ " 1");
//	Client::addMenuItem(%clientId, %cnt++ @ "Examine", %type @ " examine " @ %item);
//	Client::addMenuItem(%clientId, %cnt++ @ "Back", %type @ " back " @ %item);
//	Client::addMenuItem(%clientId, "xDone", "done");
//	return;
//}
//
//function processMenuBelt::MugChoose(%clientId, %opt)
//{
//	%type = GetWord(%opt, 0);
//	%option = GetWord(%opt, 1);
//	%item = GetWord(%opt, 2);
//
//	if(%option == "MUG")
//	{
//		Client::sendMessage(%clientId, $MsgBeige, "Attempting to Mug " @ client::getName(%clientId.MugId) @ "'s belt...");
//		schedule("Belt::MugItem(" @ %clientId @ ", " @ %item @ ", " @ %type @ ");", 5);
//	}
//	else if(%option == "examine")
//	{
//		%msg = WhatIs(%item);
//		bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
//		MenuBelt::MugChoose(%clientId, %item, %type);
//	}
//	else if(%option == "back")
//	{
//		MenuBelt::MugThisType(%clientId, %option, 1);
//	}
//	return;
//}
//function Belt::MugItem(%clientId, %item, %type)
//{
//	%cl=%clientId.MugId;
//	%list = fetchData(%cl, %type);
//	%victimName = Client::getName(%cl);
//	%stealerName = Client::getName(%clientId);
//
//	for(%i=0; getword(fetchData(%cl, %type), %i) != -1; %i+=2)
//	{
//		if(getword(fetchData(%cl, %type), %i) == %item)
//		%icnt = getword(fetchData(%cl, %type), %i+1);
//	}
//	%clientId.TryingToSteal = "";
//
//	//weights
//	%itemweight = GetAccessoryVar(%item, $Weight);
//	%wweight = 10;
//	if(Player::getMountedItem(%clientId, $WeaponSlot) == %item || %item.className == Equipped)
//	%handweight = 5.0;
//	else
//	%handweight = 1.0;
//	%FailWeight = (%itemweight * %wweight) * %handweight;
//
//	if(Vector::getDistance(GameBase::getPosition(%clientId), GameBase::getPosition(%cl)) > 2)
//	Client::sendMessage(%clientId, $MsgWhite, "Your target has wandered off...");
//	else
//	{
//		%r1 = GetRoll("1d" @ $PlayerSkill[%clientId, $SkillStealing]);
//		%r2 = GetRoll("1d" @ ($PlayerSkill[%cl, $SkillStealing] + $PlayerSkill[%cl, $SkillDodging]));
//		%b = %r1 - %r2;
//		%a = %b - %FailWeight;
//		if(%a > 0 && %icnt > 0)
//		{
//			%newcnt = %icnt-1;
//			Client::sendMessage(%clientId, $MsgBeige, "You successfully stole a " @ %item @ " from " @ %victimName @ "!");
//			Belt::TakeThisStuff(%cl, %item, 1);
//			Belt::GiveThisStuff(%clientId, %item, 1);
//			PerhapsPlayStealSound(%clientId, %clientId.stealType);
//
//			if(%newcnt > 0)
//			MenuBelt::MugChoose(%clientId, %item, %type);
//
//			RefreshAll(%clientId);
//			RefreshAll(%cl);
//
//			UseSkill(%clientId, $SkillStealing, true, true);
//			PostSteal(%clientId, true, %clientId.stealType);
//
//			return true;
//		}
//		else
//		{
//			Client::cancelMenu(%clientId);
//			Client::sendMessage(%clientId, $MsgRed, "You failed to mug " @ %victimName @ "'s belt!");
//			Client::sendMessage(%cl, $MsgRed, %stealerName @ " just failed to mug your belt!");
//
//			UseSkill(%clientId, $SkillStealing, false, true);
//			PostSteal(%clientId, false, %clientId.stealType);
//		}
//	}
//
//	//force kick from Inv Screen
//	remotePlayMode(%clientId);
//
//	return false;
//}
//==========================//
//====== MUG BELT END ======//
//==========================//

function isBeltItem(%item)
{
	%lookup = $BeltItem[%item, "Item"];
	%compare = String::ICompare(%lookup, %item);
	if(%compare == 0)
	{
		return True;
	}
	return False;
}

function Belt::WhatIs(%clientId, %item)
{
	dbecho($dbechoMode, "Belt::WhatIs(" @ %clientId @ ", " @ %item @ ")");

	if(%item.description == false)
		%desc = $BeltItem[%item, "Name"];
	else
		%desc = %item.description;
		
	%w = GetAccessoryVar(%item, $Weight);
	%c = GetItemCost(%item);
	%t = Belt::Display($BeltItem[%item, "Type"]);
	
	if($AccessoryVar[%item, $MiscInfo] != "")
		%nfo = $AccessoryVar[%item, $MiscInfo];
	else
		%nfo = "There is no further information available.";

	%msg = "";
	%msg = %msg @ "<f1>" @ %desc @ "\n";
	if(%w != "")
	%msg = %msg @ "\nWeight: " @ %w;
	if(%t != "")
	%msg = %msg @ "\nType: " @ %t;	
	if(%c != "")
	%msg = %msg @ "\nPrice: $" @ %c;
	
	
	%msg = %msg @ "\n\n<f0>" @ %nfo;
	KronosExamineInfo(%clientId, %msg, floor(String::len(%msg) / 20));
}

function Belt::GetLastItem()
{
	for(%i = 1; $Belt::Categories[%i] != ""; %i++) {}
	
	return %i;
}

function Belt::Display(%type)
{
	%disp = ""; // Initialize to empty string
	if(%type == "QuestItems") %disp = "Quest Items";
	else if(%type == "KeyItems") %disp = "Key Items";
	else if(%type == "Deployables") %disp = "Deployables";
	else if(%type == "Consumables") %disp = "Consumables";
	else if(%type == "Armor") %disp = "Armor";
	else if(%type == "Accessories") %disp = "Accessories";
	else if(%type == "Other") %disp = "Other";
	else if(%type == "Weapons") %disp = "Weapons";
	
	// If no match, return the type name itself as fallback
	if(%disp == "")
		%disp = %type;

	return %disp;
}

function Belt::Sell(%clientId, %npc)
{
	// Set flag to track that player is in belt sell menu (for bulkNum chat input)
	%clientId.currentBeltSell = %npc;
	AI::sayLater(%clientId, %npc, "What would you like to sell?", true);
	MenuSellBelt(%clientId);
}

function Belt::DeployItem(%clientId, %item, %type)
{
	// Check if player has the item in Belt storage
	if(!Belt::HasThisStuff(%clientId, %item))
	{
		Client::sendMessage(%clientId, $MsgRed, "You don't have any " @ $BeltItem[%item, "Name"] @ " in your backpack.");
		return;
	}
	
	// Get player object - needed for Player::deployItem
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		Client::sendMessage(%clientId, $MsgRed, "You must be in-game to deploy items.");
		return;
	}
	
	// Check if this is DepBasePack - deployables still use ItemData
	// For non-deployable belt items (quest items, key items), skip ItemData validation
	if(%item == "DepBasePack")
	{
		// Get ItemData object for deployables (DepBasePack still uses ItemData)
	%itemData = getItemData(%item);
	if(%itemData == "")
	{
			Client::sendMessage(%clientId, $MsgRed, "Invalid item data for " @ %item @ ".");
		return;
	}
	
	// Set flag so onDeploy knows to remove from Belt instead of standard inventory
	%clientId.beltDeploying = %item;
	
	// Call Player::deployItem - this triggers the onDeploy callback
	Player::deployItem(%player, %itemData);
	}
	else
	{
		// Non-deployable belt items don't need ItemData - they're string-based
		// This path is for future belt items that might be deployable but not yet implemented
		Client::sendMessage(%clientId, $MsgRed, "Cannot deploy " @ %item @ " - this item is not deployable.");
		return;
	}
	
	// Clear flag after a moment (in case deployment fails)
	schedule("if(" @ %clientId @ ".beltDeploying != \"\") " @ %clientId @ ".beltDeploying = \"\";", 2);
}

function Belt::UseItem(%clientId, %item, %type)
{
	dbecho($dbechoMode, "Belt::UseItem(" @ %clientId @ ", " @ %item @ ", " @ %type @ ")");

	// Belt weapons (BeltWeapons.cs): "use" = equip toggle. This is what the
	// KronosHUD Use button reaches for Weapons-category rows.
	if(%type == "Weapons")
	{
		if($BeltItem[%item, "Item"] != "")
			%item = $BeltItem[%item, "Item"];	// accept display-name alias
		if(fetchData(%clientId, "EquippedBeltWeapon") == %item)
			BeltWeapon::Unequip(%clientId, false);
		else
			BeltWeapon::Equip(%clientId, %item);
		return;
	}

	// Check if player has the item in Belt storage
	if(!Belt::HasThisStuff(%clientId, %item))
	{
		Client::sendMessage(%clientId, $MsgRed, "You don't have any " @ $BeltItem[%item, "Name"] @ " in your backpack.");
		return;
	}
	
	// Get player object - needed for onUse function
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		Client::sendMessage(%clientId, $MsgRed, "You must be in-game to use items.");
		return;
	}
	
	// Check if item has an onUse function (potions, etc.)
	// Call the item's onUse function with player object and item name
	// The onUse function will handle consuming the item (via Belt::TakeThisStuff)
	if(%item == "BluePotion")
		BluePotion::onUse(%player, %item);
	else if(%item == "CrystalBluePotion")
		CrystalBluePotion::onUse(%player, %item);
	else if(%item == "EnergyVial")
		EnergyVial::onUse(%player, %item);
	else if(%item == "CrystalEnergyVial")
		CrystalEnergyVial::onUse(%player, %item);
	else
	{
		Client::sendMessage(%clientId, $MsgRed, "Cannot use " @ $BeltItem[%item, "Name"] @ " - this item is not consumable.");
		return;
	}
}

function Belt::DropItem(%clientId, %item, %amnt, %type)
{
	%chk = Belt::HasThisStuff(%clientId, %item);
	if(%chk >= %amnt)
	{
		// CRITICAL: Auto-unequip if item is equipped before dropping
		if(%type == "Accessories" && Belt::IsAccessoryEquipped(%clientId, %item))
		{
			echo("[BELT DROP] Auto-unequipping accessory before drop: " @ %item);
			// review #15: unequip ONE instance per dropped unit (rings etc. can be
			// worn in 2 slots). UnequipAccessory strips a single instance per call,
			// so a single call while bulk-dropping %amnt left a phantom equipped
			// entry - and its stat bonus, persisted and re-applied on every login -
			// for an item the player no longer owns. The sell and both deposit
			// paths already loop this; DropItem was the lone exception.
			for(%unequipCount = 0; %unequipCount < %amnt; %unequipCount++)
				Belt::UnequipAccessory(%clientId, %item);
		}
		else if(%type == "Armor" && fetchData(%clientId, "EquippedBeltArmor") == %item)
		{
			echo("[BELT DROP] Auto-unequipping armor before drop: " @ %item);
			Belt::UnequipArmor(%clientId, %item);
		}
		else if(%type == "Weapons" && fetchData(%clientId, "EquippedBeltWeapon") == %item)
		{
			echo("[BELT DROP] Auto-unequipping belt weapon before drop: " @ %item);
			BeltWeapon::Unequip(%clientId, true);
		}

		Belt::TakeThisStuff(%clientId, %item, %amnt);
		TossLootbag(%clientId, %item @ " " @ %amnt, 8, "*", 0, 1);
		SaveCharacter(%clientId);
		// Queue deployable-only world save after 3 seconds (preserves lootbag crash safety, avoids full-save storms)
		RequestWorldSave("belt_drop", 3, "deployables");
	}
}

// CRITICAL: Update an item's count IN-PLACE without changing its position in the list
// This prevents belt reordering when item counts change
// Returns the updated list, or the original list if item not found
function Belt::UpdateCountInPlace(%list, %itemName, %newCount)
{
	// If new count is 0 or less, remove the item entirely (use RemoveFromList for that)
	if(%newCount <= 0)
	{
		// Find current count and remove
		%currentCount = Belt::ItemCount(%itemName, %list);
		if(%currentCount > 0)
			return Belt::RemoveFromList(%list, %itemName @ " " @ %currentCount);
		return %list;
	}
	
	// Preserve trailing space status
	%hadTrailingSpace = false;
	%len = String::len(%list);
	while(%len > 0 && String::getSubStr(%list, %len-1, 1) == " ")
	{
		%hadTrailingSpace = true;
		%list = String::getSubStr(%list, 0, %len-1);
		%len = String::len(%list);
	}
	
	// Count words in list
	%wordCount = 0;
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
		%wordCount++;
	
	// Rebuild list, updating the count for the target item IN-PLACE
	%rebuiltList = "";
	%found = false;
	
	for(%i = 0; %i < %wordCount; %i += 2)
	{
		%currentItem = GetWord(%list, %i);
		%currentCount = GetWord(%list, %i + 1);
		
		// If this is the target item, use the new count instead
		if(%currentItem == %itemName)
		{
			%currentCount = %newCount;
			%found = true;
		}
		
		// Add item with proper spacing
		if(%rebuiltList != "")
			%rebuiltList = %rebuiltList @ " ";
		%rebuiltList = %rebuiltList @ %currentItem @ " " @ %currentCount;
	}
	
	// Restore trailing space if original had it
	if(%hadTrailingSpace && %rebuiltList != "")
		%rebuiltList = %rebuiltList @ " ";
	
	return %rebuiltList;
}

function Belt::AddToList(%list, %item)
{
	// Validate item string before adding
	// Parse item string format: "itemname count" (e.g., "AlienSpine 10")
	%itemName = GetWord(%item, 0);
	%itemCount = GetWord(%item, 1);
	
	// Skip invalid entries: empty item, item "0", empty count, count -1 or "0", count <= 0
	%countNum = %itemCount * 1;
	if(%itemName == "" || %itemName == -1 || %itemName == "0" || %itemCount == "" || %itemCount == -1 || %itemCount == "-1" || %itemCount == "0" || %countNum <= 0)
	{
		// Invalid entry - don't add it to the list
		return %list;
	}
	
	// CRITICAL: Normalize the list first - remove any trailing spaces to prevent double spaces
	// But we'll add it back at the end to match spawnStuff format
	%len = String::len(%list);
	while(%len > 0 && String::getSubStr(%list, %len-1, 1) == " ")
	{
		%list = String::getSubStr(%list, 0, %len-1);
		%len = String::len(%list);
	}
	
	// CRITICAL: Ensure proper spacing when adding new item
	// Format: "item1 count1 item2 count2 " (item name comes first, then count, with trailing space)
	// Example: "AlienSpine 5 Bible 10 VirusFragment 15 " (matches spawnStuff format)
	if(%list != "" && %list != "0")
	{
		// List has content - add space before new item and trailing space after (format: "existing item count ")
		%list = %list @ " " @ %itemName @ " " @ %itemCount @ " ";
	}
	else
	{
		// List is empty - start with item and trailing space (format: "item count ")
		%list = %itemName @ " " @ %itemCount @ " ";
	}
	
	return %list;
}

function Belt::RemoveFromList(%list, %item)
{
	// CRITICAL: For equipped categories, we need to preserve trailing space
	// But we still need to normalize for pattern matching
	%hadTrailingSpace = false;
	%len = String::len(%list);
	// Remove ALL trailing spaces (there might be multiple)
	while(%len > 0 && String::getSubStr(%list, %len-1, 1) == " ")
	{
		%hadTrailingSpace = true;
		%list = String::getSubStr(%list, 0, %len-1);
		%len = String::len(%list);
	}
	
	// CRITICAL: Rebuild the list word-by-word to ensure proper spacing
	// This prevents items from merging when we remove an item
	%wordCount = 0;
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
		%wordCount++;
	
	// Rebuild list with proper spacing, skipping the item we want to remove
	%rebuiltList = "";
	%itemToRemove = %item;
	%itemName = GetWord(%itemToRemove, 0);
	%itemCount = GetWord(%itemToRemove, 1);
	
	for(%i = 0; %i < %wordCount; %i += 2)
	{
		%currentItem = GetWord(%list, %i);
		%currentCount = GetWord(%list, %i + 1);
		
		// Skip if this is the item we want to remove
		if(%currentItem == %itemName && %currentCount == %itemCount)
			continue;
		
		// Add item with proper spacing
		if(%rebuiltList != "")
			%rebuiltList = %rebuiltList @ " ";
		%rebuiltList = %rebuiltList @ %currentItem @ " " @ %currentCount;
	}
	
	// CRITICAL: If the original list had a trailing space, restore it
	// This preserves the field 15 format for equipped items
	if(%hadTrailingSpace && %rebuiltList != "")
		%rebuiltList = %rebuiltList @ " ";
	
	return %rebuiltList;
}

// DEAD CODE (no callers as of Jul 2026): kept for reference only. If resurrected,
// beware: findSubStr is a substring match, so "PowerRing" matches "MajorPowerRing".
function Belt::IsInList(%list, %item)
{
	if(String::findSubStr(%list, %item) != -1)
		return True;
	else
		return False;
}

function Belt::GetStored(%clientId, %opt)
{	
	%type = GetWord(%opt, 0);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);

	for(%i=0; (%item2=getword(fetchData(%clientId, "Stored" @ %type), %i)) != -1; %i+=2)
	{
		if(%item == %item2)
		{
			%amnt=getword(fetchData(%clientId, "Stored" @ %type), %i+1);
			return %amnt;
		}
	}
}

// DEAD CODE (no callers as of Jul 2026, and $Belt::Storage[...] is never set anywhere,
// so the body is a no-op even if called). DO NOT resurrect as-is - it has two latent
// bugs: "%b = getword(%tmploot, %i++)" reads the SAME word as %a (post-increment
// returns the old index, so %b gets the item name, not the count), and the category
// concat at the top joins lists without a space separator (merge corruption).
function Belt::GetDeathItems(%clientId)
{
	%tmploot = "";
	if(fetchData(%clientId, "LCK") < 0)
	{
		for(%i = 1; $Belt::Categories[%i] != ""; %i++)
			if($Belt::Storage[$Belt::Categories[%i]])
				%tmploot = %tmploot @ fetchData(%clientId, $Belt::Categories[%i]);

		for(%i=0;getword(%tmploot, %i)!=-1;%i++)
		{
			%a = getword(%tmploot, %i);
			%b = getword(%tmploot, %i++);

			Belt::TakeThisStuff(%clientId, %a, %b);
		}
	}
	
	return %tmploot;
}

function BeltItem::Add(%name, %item, %type, %weight, %cost)
{
	// CRITICAL: assign index BEFORE incrementing. "%num = $Belt::Count[%type]++;"
	// returns the NEW value in this engine (see AssignOpExprNode::eval), which made
	// indices 1-based while all iteration loops are 0-based - hiding the last item
	// registered in every category from shop/belt menus.
	%num = $Belt::Count[%type];
	$Belt::Count[%type]++;
	$BeltItem[%num, "Num", %type] = %item;
	$BeltItem[%item, "Item"] = %item;
	$BeltItem[%item, "Name"] = %name;
	$BeltItem[%item, "Type"] = %type;
	$AccessoryVar[%item, $Weight] = %weight;
	$HardcodedItemCost[%item] = %cost;
	
	// Also register the display name as an alias to the item name for lookup purposes
	// This handles cases where items might be referenced by their display name
	$BeltItem[%name, "Item"] = %item;
}

//===================
//  Minerals (Removed - not in this mod)
//===================
// Mining items removed from Kingdom of Kronos

//===================
//  Quest Items
//===================

BeltItem::Add("Black Statue", "BlackStatue", "QuestItems", 1.0, 300);
BeltItem::Add("Ruby Necklace", "RubyNecklace", "QuestItems", 2.5, 300);
BeltItem::Add("Enchanted Stone", "EnchantedStone", "QuestItems", 5.0, 2450);
BeltItem::Add("Skeleton Bone", "SkeletonBone", "QuestItems", 2.5, 5860);
BeltItem::Add("Ogre Tooth", "OgreTooth", "QuestItems", 2.5, 6000);
BeltItem::Add("Ancient Scroll", "AncientScroll", "QuestItems", 5.0, 40000);
BeltItem::Add("Crown", "Crown", "QuestItems", 2.5, 45000);
BeltItem::Add("Krono Stone", "KronoStone", "QuestItems", 5.0, 80000);
BeltItem::Add("Minotaur Horn", "MinotaurHorn", "QuestItems", 2.5, 100000);
BeltItem::Add("Alien Spine", "AlienSpine", "QuestItems", 5.0, 150000);
BeltItem::Add("Alien Eye", "AlienEye", "QuestItems", 2.5, 500000);
BeltItem::Add("Demon Breath", "DemonBreath", "QuestItems", 5.0, 500000);
BeltItem::Add("Bible", "Bible", "QuestItems", 5.0, 800000);
BeltItem::Add("Virus Fragment", "VirusFragment", "QuestItems", 2.0, 850000);
BeltItem::Add("Angel's Tear", "AngelsTear", "QuestItems", 2.5, 900000);
BeltItem::Add("Void Stone", "VoidStone", "QuestItems", 5.0, 1000000);
BeltItem::Add("SIGKILL Sigil", "SigkillSigil", "QuestItems", 5.0, 2000000);
BeltItem::Add("NULL", "NullItem", "QuestItems", 1.0, 500000);

$AccessoryVar[BlackStatue, $MiscInfo] = "A black statue";
$AccessoryVar[RubyNecklace, $MiscInfo] = "A ruby necklace";
$AccessoryVar[EnchantedStone, $MiscInfo] = "An enchanted stone";
$AccessoryVar[SkeletonBone, $MiscInfo] = "A skeleton bone";
$AccessoryVar[OgreTooth, $MiscInfo] = "An ogre tooth";
$AccessoryVar[AncientScroll, $MiscInfo] = "A very old look scroll";
$AccessoryVar[Crown, $MiscInfo] = "A very valuable crown.";
$AccessoryVar[KronoStone, $MiscInfo] = "A mystical stone sizzling with power";
$AccessoryVar[MinotaurHorn, $MiscInfo] = "A horn from a Minotaur's Head";
$AccessoryVar[AlienSpine, $MiscInfo] = "A long spine from an alien";
$AccessoryVar[AlienEye, $MiscInfo] = "A gooey eye from the Alien Queen";
$AccessoryVar[DemonBreath, $MiscInfo] = "The very rare breath of a demon";
$AccessoryVar[Bible, $MiscInfo] = "Do you have time to talk about your lord and savior?";
$AccessoryVar[VirusFragment, $MiscInfo] = "A fragment of corrupted virus - be careful, it could be contagious!";
$AccessoryVar[AngelsTear, $MiscInfo] = "A tear shed from an angel when it dies.";
$AccessoryVar[VoidStone, $MiscInfo] = "A mysterious stone pulsing with void energy from beyond the realm.";
$AccessoryVar[SigkillSigil, $MiscInfo] = "A powerful sigil carrying a SIGKILL command, pulsing with code-terminating energy.";
$AccessoryVar[NullItem, $MiscInfo] = "NULL";


//===================
//  Key Items
//===================
BeltItem::Add("DuelCard", "DuelCard", "KeyItems", 2.0, 100000);

$AccessoryVar[DuelCard, $MiscInfo] = "A special card that allows you to #challenge someone to a duel.";

//===================
//  Deployables
//===================
// review #4: "Deployable Base Pack" (40,000 coins) is UNUSABLE - its datablock
// script depbase.cs is never exec'd (Server.cs: //exec(depbase);), so
// getItemData("DepBasePack") returns "" and Belt::DeployItem always fails with
// "Invalid item data", permanently eating the player's 40k with no refund. Even
// if depbase.cs were re-enabled, DepBasePack::deployShape never returns true
// (its success path has no `return true;`), so deploy would still fail. Pulled
// from sale so no one can lose coins on it. To restore the feature: uncomment
// exec(depbase) in Server.cs, add the missing `return true;` in
// depbase.cs::deployShape, then re-add this line.
//BeltItem::Add("Deployable Base Pack", "DepBasePack", "Deployables", 0.0, 40000);

//===================
//  Consumables
//===================
BeltItem::Add("Blue Potion", "BluePotion", "Consumables", 4.0, 15);
BeltItem::Add("Crystal Blue Potion", "CrystalBluePotion", "Consumables", 10.0, 100);
BeltItem::Add("Energy Vial", "EnergyVial", "Consumables", 2.0, 15);
BeltItem::Add("Crystal Energy Vial", "CrystalEnergyVial", "Consumables", 5.0, 100);

//===================
//  Armor (TEST ITEMS - Belt System Integration)
//===================
// These are test armor items to verify the belt system works correctly for armor
// Note: Armor works differently than other belt items - it affects player appearance/stats
// Players can only have one armor equipped at a time

BeltItem::Add("Test Leather Armor", "TestLeatherArmor", "Armor", 5.0, 500);
BeltItem::Add("Test Iron Armor", "TestIronArmor", "Armor", 15.0, 2500);
BeltItem::Add("Test Steel Armor", "TestSteelArmor", "Armor", 25.0, 10000);

$AccessoryVar[TestLeatherArmor, $MiscInfo] = "A simple leather armor for testing the belt system. DEF +5";
$AccessoryVar[TestIronArmor, $MiscInfo] = "A sturdy iron armor for testing the belt system. DEF +15";
$AccessoryVar[TestSteelArmor, $MiscInfo] = "A strong steel armor for testing the belt system. DEF +30";

// Armor stat bonuses (using $SpecialVar format: "statIndex bonus statIndex bonus ...")
// 7 = DEF
$AccessoryVar[TestLeatherArmor, $SpecialVar] = "7 5";
$AccessoryVar[TestIronArmor, $SpecialVar] = "7 15";
$AccessoryVar[TestSteelArmor, $SpecialVar] = "7 30";

// Mark these as body armor type
$AccessoryVar[TestLeatherArmor, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[TestIronArmor, $AccessoryType] = $BodyAccessoryType;
$AccessoryVar[TestSteelArmor, $AccessoryType] = $BodyAccessoryType;

//===================
//  Accessories (TEST ITEMS - Belt System Integration)
//===================
// These are test accessory items to verify the belt system works correctly
// Accessories include rings, necklaces, and belts

//===================
//  Accessories - Rings, Necklaces, Belts (Migrated from ItemData)
//===================

// Power Rings (max 2 equipped) - $RingAccessoryType
BeltItem::Add("Minor Power Ring", "MinorPowerRing", "Accessories", 2, 800);
BeltItem::Add("Power Ring", "PowerRing", "Accessories", 5, 2500);
BeltItem::Add("Major Power Ring", "MajorPowerRing", "Accessories", 15, 20000);
BeltItem::Add("Extreme Power Ring", "ExtremePowerRing", "Accessories", 25, 300000);
BeltItem::Add("Godly Power Ring", "GodlyPowerRing", "Accessories", 60, 2000000);
BeltItem::Add("Heavenly Power Ring", "HeavenlyPowerRing", "Accessories", 90, 10000000);

// Regeneration Necklaces (max 1 equipped) - $TalismanAccessoryType
BeltItem::Add("Minor Regeneration Necklace", "MinorRegenerationNecklace", "Accessories", 20, 3000);
BeltItem::Add("Regeneration Necklace", "RegenerationNecklace", "Accessories", 50, 40000);
BeltItem::Add("Major Regeneration Necklace", "MajorRegenerationNecklace", "Accessories", 100, 10000000);
BeltItem::Add("Extreme Regeneration Necklace", "ExtremeRegenerationNecklace", "Accessories", 250, 50000000);
BeltItem::Add("Godly Regeneration Necklace", "GodlyRegenerationNecklace", "Accessories", 750, 120000000);
BeltItem::Add("Heavenly Regeneration Necklace", "HeavenlyRegenerationNecklace", "Accessories", 1125, 300000000);

// AntiMagic Belts (max 1 equipped) - $BeltAccessoryType
BeltItem::Add("Antimagic Belt", "AntiMagicBelt", "Accessories", 20, 200000);
BeltItem::Add("Major Antimagic Belt", "MajorAntiMagicBelt", "Accessories", 40, 1500000);
BeltItem::Add("Extreme AntiMagic Belt", "ExtremeAntiMagicBelt", "Accessories", 70, 12000000);
BeltItem::Add("Godly AntiMagic Belt", "GodlyAntiMagicBelt", "Accessories", 200, 75000000);
BeltItem::Add("Heavenly AntiMagic Belt", "HeavenlyAntiMagicBelt", "Accessories", 300, 200000000);

// NOTE: $AccessoryVar definitions (type, stats, weight) remain in Accessory.cs
// They are already properly defined and shared by both Belt.cs and the old ItemData system

//===================
//  Other (Miscellaneous belt items)
//===================
// Placeholder for other miscellaneous items that don't fit other categories

//===================
//  Armor and Accessory Equip/Unequip Functions
//===================

// Check if an accessory is currently equipped
// Returns true if the item is in the equipped accessories list
function Belt::IsAccessoryEquipped(%clientId, %item)
{
	dbecho($dbechoMode, "Belt::IsAccessoryEquipped(" @ %clientId @ ", " @ %item @ ")");
	
	%equippedList = fetchData(%clientId, "EquippedBeltAccessories");
	if(%equippedList == "" || %equippedList == "0")
		return false;
	
	// Check if item is in the equipped list
	for(%i = 0; GetWord(%equippedList, %i) != -1; %i++)
	{
		if(GetWord(%equippedList, %i) == %item)
			return true;
	}
	return false;
}

// Get count of equipped accessories of a specific type
function Belt::GetEquippedAccessoryCountByType(%clientId, %accessoryType)
{
	dbecho($dbechoMode, "Belt::GetEquippedAccessoryCountByType(" @ %clientId @ ", " @ %accessoryType @ ")");
	
	%equippedList = fetchData(%clientId, "EquippedBeltAccessories");
	if(%equippedList == "" || %equippedList == "0")
		return 0;
	
	if($BELT_DEBUG) echo("[COUNT DEBUG] Looking for type " @ %accessoryType @ " in equipped list: " @ %equippedList);
	
	%count = 0;
	for(%i = 0; GetWord(%equippedList, %i) != -1; %i++)
	{
		%equippedItem = GetWord(%equippedList, %i);
		%itemType = $AccessoryVar[%equippedItem, $AccessoryType];
		if($BELT_DEBUG) echo("[COUNT DEBUG] Item: " @ %equippedItem @ ", Type: " @ %itemType @ ", Target: " @ %accessoryType @ ", Match: " @ (%itemType == %accessoryType));
		if(%itemType == %accessoryType)
			%count++;
	}
	if($BELT_DEBUG) echo("[COUNT DEBUG] Final count for type " @ %accessoryType @ ": " @ %count);
	return %count;
}

// Equip armor from belt inventory
function Belt::EquipArmor(%clientId, %item)
{
	dbecho($dbechoMode, "Belt::EquipArmor(" @ %clientId @ ", " @ %item @ ")");
	
	// Validate player has the item
	if(!Belt::HasThisStuff(%clientId, %item))
	{
		Client::sendMessage(%clientId, $MsgRed, "You don't have that armor in your backpack.");
		return;
	}
	
	// Get player object
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		Client::sendMessage(%clientId, $MsgRed, "Error: Could not find player object.");
		return;
	}

	// VOID CONVERSION 2026-07-14: engine armor equips were gated by SkillCanUse
	// (itemevents.cs Item::onUse); the belt path must gate identically or the
	// $SkillRestriction on every converted armor is silently lost.
	if(!SkillCanUse(%clientId, %item))
	{
		Client::sendMessage(%clientId, $MsgRed, "You can't equip this item because you lack the necessary skills.~wC_BuySell.wav");
		return;
	}

	// Unequip current armor first (if any)
	%currentArmor = fetchData(%clientId, "EquippedBeltArmor");
	if(%currentArmor != "" && %currentArmor != "0" && %currentArmor != %item)
	{
		Belt::UnequipArmor(%clientId, %currentArmor);
	}
	
	// Store the equipped armor
	storeData(%clientId, "EquippedBeltArmor", %item);
	
	// Apply armor stat bonuses
	Belt::ApplyAccessoryStats(%clientId, %item, true);
	
	// Get the item's display name
	%itemName = $BeltItem[%item, "Name"];
	if(%itemName == "")
		%itemName = %item;
	
	Client::sendMessage(%clientId, $MsgGreen, "You equipped " @ %itemName @ ".");
	echo("[BELT EQUIP] " @ Client::getName(%clientId) @ " equipped armor: " @ %item);
	
	// Refresh player stats
	RefreshAll(%clientId);
	SaveCharacter(%clientId);
}

// Unequip armor
function Belt::UnequipArmor(%clientId, %item)
{
	dbecho($dbechoMode, "Belt::UnequipArmor(" @ %clientId @ ", " @ %item @ ")");
	
	%currentArmor = fetchData(%clientId, "EquippedBeltArmor");
	if(%currentArmor != %item)
	{
		Client::sendMessage(%clientId, $MsgRed, "That armor is not currently equipped.");
		return;
	}
	
	// Remove armor stat bonuses
	Belt::ApplyAccessoryStats(%clientId, %item, false);
	
	// Clear equipped armor
	storeData(%clientId, "EquippedBeltArmor", "");
	
	// Get the item's display name
	%itemName = $BeltItem[%item, "Name"];
	if(%itemName == "")
		%itemName = %item;
	
	Client::sendMessage(%clientId, $MsgYellow, "You unequipped " @ %itemName @ ".");
	echo("[BELT UNEQUIP] " @ Client::getName(%clientId) @ " unequipped armor: " @ %item);
	
	// Refresh player stats
	RefreshAll(%clientId);
	SaveCharacter(%clientId);
}


// Equip accessory from belt inventory
function Belt::EquipAccessory(%clientId, %item)
{
	dbecho($dbechoMode, "Belt::EquipAccessory(" @ %clientId @ ", " @ %item @ ")");
	
	// Validate player has the item
	if(!Belt::HasThisStuff(%clientId, %item))
	{
		Client::sendMessage(%clientId, $MsgRed, "You don't have that accessory.");
		return;
	}
	
	// Get accessory type and check max slots
	%accessoryType = $AccessoryVar[%item, $AccessoryType];
	%maxSlots = $maxAccessory[%accessoryType];
	if(%maxSlots == "" || %maxSlots == 0)
		%maxSlots = 1; // Default to 1 slot if not defined
	
	// Count total equipped of this TYPE (not just this item)
	%currentTypeCount = Belt::GetEquippedAccessoryCountByType(%clientId, %accessoryType);
	
	if(%currentTypeCount >= %maxSlots)
	{
		%typeName = $LocationDesc[%accessoryType];
		if(%typeName == "")
			%typeName = "accessory";
		Client::sendMessage(%clientId, $MsgRed, "You can only equip " @ %maxSlots @ " " @ %typeName @ " at a time. Unequip one first.");
		return;
	}
	
	// Add to equipped list (allow duplicates for items like rings)
	%equippedList = fetchData(%clientId, "EquippedBeltAccessories");
	if(%equippedList == "" || %equippedList == "0")
		%equippedList = %item;
	else
		%equippedList = %equippedList @ " " @ %item;
	storeData(%clientId, "EquippedBeltAccessories", %equippedList);
	
	// Apply accessory stat bonuses
	Belt::ApplyAccessoryStats(%clientId, %item, true);
	
	// Get the item's display name
	%itemName = $BeltItem[%item, "Name"];
	if(%itemName == "")
		%itemName = %item;
	
	Client::sendMessage(%clientId, $MsgGreen, "You equipped " @ %itemName @ ".");
	echo("[BELT EQUIP] " @ Client::getName(%clientId) @ " equipped accessory: " @ %item);
	
	// Refresh player stats
	RefreshAll(%clientId);
	SaveCharacter(%clientId);
}

// Unequip accessory
function Belt::UnequipAccessory(%clientId, %item)
{
	dbecho($dbechoMode, "Belt::UnequipAccessory(" @ %clientId @ ", " @ %item @ ")");
	
	if(!Belt::IsAccessoryEquipped(%clientId, %item))
	{
		Client::sendMessage(%clientId, $MsgRed, "That accessory is not currently equipped.");
		return;
	}
	
	// Remove ONE instance of the item from equipped list (important for items like rings where you can have 2 of the same)
	%equippedList = fetchData(%clientId, "EquippedBeltAccessories");
	%newList = "";
	%removed = false;
	
	for(%i = 0; GetWord(%equippedList, %i) != -1; %i++)
	{
		%equippedItem = GetWord(%equippedList, %i);
		// Skip the first instance of the item we're unequipping
		if(%equippedItem == %item && !%removed)
		{
			%removed = true;
			continue; // Skip this one
		}
		// Keep all other items
		if(%newList == "")
			%newList = %equippedItem;
		else
			%newList = %newList @ " " @ %equippedItem;
	}
	
	storeData(%clientId, "EquippedBeltAccessories", %newList);
	
	// Remove accessory stat bonuses (only once, for the one we unequipped)
	Belt::ApplyAccessoryStats(%clientId, %item, false);
	
	// Get the item's display name
	%itemName = $BeltItem[%item, "Name"];
	if(%itemName == "")
		%itemName = %item;
	
	Client::sendMessage(%clientId, $MsgYellow, "You unequipped " @ %itemName @ ".");
	echo("[BELT UNEQUIP] " @ Client::getName(%clientId) @ " unequipped accessory: " @ %item);
	
	// Refresh player stats
	RefreshAll(%clientId);
	SaveCharacter(%clientId);
}

// Apply or remove accessory/armor stat bonuses
// %apply = true to add stats, false to remove
function Belt::ApplyAccessoryStats(%clientId, %item, %apply)
{
	dbecho($dbechoMode, "Belt::ApplyAccessoryStats(" @ %clientId @ ", " @ %item @ ", " @ %apply @ ")");
	
	%specialVar = $AccessoryVar[%item, $SpecialVar];
	if(%specialVar == "" || %specialVar == "0")
		return; // No stats to apply
	
	// Parse the stat string format: "statIndex value statIndex value ..."
	for(%i = 0; GetWord(%specialVar, %i) != -1; %i += 2)
	{
		%statIndex = GetWord(%specialVar, %i);
		%statValue = GetWord(%specialVar, %i + 1);
		
		if(%statIndex == "" || %statIndex == -1 || %statValue == "" || %statValue == -1)
			continue;
		
		// Get the stat name based on index
		// 3 = MDEF, 4 = HP, 5 = Mana, 6 = ATK, 7 = DEF, 10 = HP regen, 11 = Mana regen
		%statName = "";
		if(%statIndex == 3) %statName = "BeltMDEFBonus";
		else if(%statIndex == 4) %statName = "BeltHPBonus";
		else if(%statIndex == 5) %statName = "BeltManaBonus";
		else if(%statIndex == 6) %statName = "BeltATKBonus";
		else if(%statIndex == 7) %statName = "BeltDEFBonus";
		else if(%statIndex == 10) %statName = "BeltHPRegenBonus";
		else if(%statIndex == 11) %statName = "BeltManaRegenBonus";
		
		if(%statName == "")
			continue;
		
		// Get current bonus value
		%currentBonus = fetchData(%clientId, %statName);
		if(%currentBonus == "" || %currentBonus == "0")
			%currentBonus = 0;
		
		// Apply or remove the stat
		if(%apply)
			%newBonus = %currentBonus + %statValue;
		else
			%newBonus = %currentBonus - %statValue;
		
		// Store the new bonus value
		storeData(%clientId, %statName, %newBonus);
		
		// Log the stat change
		if(%apply)
			%changeSign = "+";
		else
			%changeSign = "-";
		echo("[BELT STATS] " @ Client::getName(%clientId) @ " - " @ %statName @ ": " @ %currentBonus @ " -> " @ %newBonus @ " (" @ %changeSign @ %statValue @ " from " @ %item @ ")");
	}
}

// Re-apply all equipped belt item stats (called on load)
function Belt::ReapplyEquippedStats(%clientId)
{
	dbecho($dbechoMode, "Belt::ReapplyEquippedStats(" @ %clientId @ ")");
	
	// First clear all belt bonuses
	storeData(%clientId, "BeltMDEFBonus", 0);
	storeData(%clientId, "BeltHPBonus", 0);
	storeData(%clientId, "BeltManaBonus", 0);
	storeData(%clientId, "BeltATKBonus", 0);
	storeData(%clientId, "BeltDEFBonus", 0);
	storeData(%clientId, "BeltHPRegenBonus", 0);
	storeData(%clientId, "BeltManaRegenBonus", 0);
	
	// Re-apply equipped armor stats
	%equippedArmor = fetchData(%clientId, "EquippedBeltArmor");
	if(%equippedArmor != "" && %equippedArmor != "0")
	{
		Belt::ApplyAccessoryStats(%clientId, %equippedArmor, true);
	}
	
	// Re-apply equipped accessory stats
	%equippedList = fetchData(%clientId, "EquippedBeltAccessories");
	if(%equippedList != "" && %equippedList != "0")
	{
		for(%i = 0; GetWord(%equippedList, %i) != -1; %i++)
		{
			%item = GetWord(%equippedList, %i);
			if(%item != "" && %item != -1)
			{
				Belt::ApplyAccessoryStats(%clientId, %item, true);
			}
		}
	}
	
	echo("[BELT STATS] Reapplied equipped stats for " @ Client::getName(%clientId));
}

//===================
//  Belt Storage at Banker (Backpack menu)
//===================
function Belt::Store(%clientId, %bankerId)
{
	dbecho($dbechoMode, "Belt::Store(" @ %clientId @ ", " @ %bankerId @ ")");
	
	%clientId.currentBeltBank = %bankerId;
	
	%msg = "<jc><f2>To Deposit/Withdraw 'bulk' Backpack Items, please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, 10);
	
	// Show menu with deposit and withdraw options
	Client::buildMenu(%clientId, "Backpack Storage:", "BeltStorage", true);
	Client::addMenuItem(%clientId, "1Deposit Backpack Items", "deposit");
	Client::addMenuItem(%clientId, "2Withdraw Backpack Items", "withdraw");
	Client::addMenuItem(%clientId, "x BACK", "back");
	
	AI::sayLater(%clientId, %bankerId, "Would you like to DEPOSIT or WITHDRAW backpack items?", True);
}

function processMenuBeltStorage(%clientId, %option)
{
	if(%option == "deposit")
	{
		Belt::ShowDepositCategoryMenu(%clientId);
	}
	else if(%option == "withdraw")
	{
		Belt::ShowWithdrawCategoryMenu(%clientId);
	}
	else if(%option == "back" || %option == "cancel")
	{
		// Clear the belt bank flag and stored orders when cancelled
		%bankerId = %clientId.currentBeltBank;
		%clientId.currentBeltBank = "";
		%clientId.beltDepositItemOrder = "";
		%clientId.beltWithdrawItemOrder = "";
		
		// Reset state so bot can respond to "hi" again
		// If going back, we call SetupBankDefault which handles building the new menu
		if(%bankerId != "")
		{
			// Return to main banker menu
			SetupBankDefault(%clientId, %bankerId);
		}
		else
		{
			// If fully cancelling/exiting, clear state to empty so "hi" works
			$state[%bankerId, %clientId] = "";
			Client::cancelMenu(%clientId);
		}
	}
	// If cancel, do nothing
}

// Show category selection menu for deposit
function Belt::ShowDepositCategoryMenu(%clientId)
{
	Client::buildMenu(%clientId, "Deposit - Choose Category:", "BeltDepositCategory", true);
	
	%cnt = 1;
	%hasItems = false;
	
	// Check each category for items
	for(%i = 1; $Belt::Categories[%i] != ""; %i++)
	{
		%category = $Belt::Categories[%i];
		%items = fetchData(%clientId, %category);
		
		// Count items in this category
		%itemCount = 0;
		if(%items != "" && %items != "0")
		{
			for(%j = 0; GetWord(%items, %j) != -1; %j += 2)
			{
				%count = GetWord(%items, %j + 1);
				if(%count > 0)
					%itemCount++;
			}
		}
		
		if(%itemCount > 0)
		{
			%displayName = Belt::Display(%category);
			Client::addMenuItem(%clientId, %cnt++ @ %displayName @ " (" @ %itemCount @ " types)", %category);
			%hasItems = true;
		}
	}
	
	if(!%hasItems)
		Client::addMenuItem(%clientId, "1No items to deposit", "none");
	
	Client::addMenuItem(%clientId, "aAll Categories", "all");
	Client::addMenuItem(%clientId, "xBack", "back");
}

function processMenuBeltDepositCategory(%clientId, %option)
{
	if(%option == "back")
	{
		Belt::Store(%clientId, %clientId.currentBeltBank);
		return;
	}
	
	if(%option == "none")
		return;
	
	if(%option == "all")
	{
		// Show all items across all categories (old behavior)
		Belt::ShowDepositMenu(%clientId);
		return;
	}
	
	// Show items in the selected category
	Belt::ShowDepositCategoryItems(%clientId, %option);
}

// Show items in a specific category for deposit
function Belt::ShowDepositCategoryItems(%clientId, %category)
{
	%displayName = Belt::Display(%category);
	Client::buildMenu(%clientId, "Deposit " @ %displayName @ ":", "BeltDepositCategoryItems", true);
	
	%cnt = 1;
	%items = fetchData(%clientId, %category);
	%hasItems = false;
	
	if(%items != "" && %items != "0")
	{
		for(%i = 0; GetWord(%items, %i) != -1; %i += 2)
		{
			%item = GetWord(%items, %i);
			%count = GetWord(%items, %i + 1);
			
			if(%count > 0)
			{
				%itemName = $BeltItem[%item, "Name"];
				if(%itemName == "")
				{
					%itemName = $AccessoryVar[%item, $Name];
					if(%itemName == "")
						%itemName = %item;
				}
				Client::addMenuItem(%clientId, %cnt++ @ %itemName @ " (" @ %count @ ")", %item @ " " @ %category);
				%hasItems = true;
			}
		}
	}
	
	if(!%hasItems)
		Client::addMenuItem(%clientId, "1No " @ %displayName @ " to deposit", "none");
	
	Client::addMenuItem(%clientId, "xBack", "back");
}

function processMenuBeltDepositCategoryItems(%clientId, %option)
{
	if(%option == "back")
	{
		Belt::ShowDepositCategoryMenu(%clientId);
		return;
	}
	
	if(%option == "none")
		return;
	
	%item = GetWord(%option, 0);
	%category = GetWord(%option, 1);
	
	// Show the deposit amount menu
	MenuSellBeltItemFinal(%clientId, %item, %category, "store");
}

// Show category selection menu for withdraw
function Belt::ShowWithdrawCategoryMenu(%clientId)
{
	Client::buildMenu(%clientId, "Withdraw - Choose Category:", "BeltWithdrawCategory", true);
	
	%cnt = 1;
	%hasItems = false;
	
	%beltStorage = fetchData(%clientId, "BeltStorage");
	
	// Count items by category in storage
	%categoryCount = "";
	for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
	{
		%item = GetWord(%beltStorage, %i);
		%count = GetWord(%beltStorage, %i + 1);
		
		if(%item != "" && %item != "0" && %count > 0)
		{
			%itemCategory = $BeltItem[%item, "Type"];
			if(%itemCategory == "")
				%itemCategory = "Other"; // Default category
			
			// Increment category counter
			%currentCount = %categoryCount[%itemCategory];
			if(%currentCount == "")
				%currentCount = 0;
			%categoryCount[%itemCategory] = %currentCount + 1;
		}
	}
	
	// Show categories that have items
	for(%i = 1; $Belt::Categories[%i] != ""; %i++)
	{
		%category = $Belt::Categories[%i];
		%itemCount = %categoryCount[%category];
		
		if(%itemCount > 0)
		{
			%displayName = Belt::Display(%category);
			Client::addMenuItem(%clientId, %cnt++ @ %displayName @ " (" @ %itemCount @ " types)", %category);
			%hasItems = true;
		}
	}
	
	if(!%hasItems)
		Client::addMenuItem(%clientId, "1No items in storage", "none");
	
	Client::addMenuItem(%clientId, "aAll Categories", "all");
	Client::addMenuItem(%clientId, "xBack", "back");
}

function processMenuBeltWithdrawCategory(%clientId, %option)
{
	if(%option == "back")
	{
		Belt::Store(%clientId, %clientId.currentBeltBank);
		return;
	}
	
	if(%option == "none")
		return;
	
	if(%option == "all")
	{
		// Show all items across all categories (old behavior)
		Belt::ShowWithdrawMenu(%clientId, 1);
		return;
	}
	
	// Show items in the selected category
	Belt::ShowWithdrawCategoryItems(%clientId, %option, 1);
}

// Show items in a specific category for withdraw
function Belt::ShowWithdrawCategoryItems(%clientId, %category, %page)
{
	// Default to page 1 if not specified
	if(%page == "" || %page < 1)
		%page = 1;
	
	%displayName = Belt::Display(%category);
	Client::buildMenu(%clientId, "Withdraw " @ %displayName @ ":", "BeltWithdrawCategoryItems", true);
	
	%beltStorage = fetchData(%clientId, "BeltStorage");
	
	// Pagination settings
	%l = 6; // Items per page
	
	// First pass: count total items in this category
	%totalItems = 0;
	for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
	{
		%item = GetWord(%beltStorage, %i);
		%count = GetWord(%beltStorage, %i + 1);
		
		if(%item != "" && %item != "0" && %count > 0)
		{
			%itemCategory = $BeltItem[%item, "Type"];
			if(%itemCategory == "")
				%itemCategory = "Other";
			
			if(%itemCategory == %category)
				%totalItems++;
		}
	}
	
	// Calculate pagination
	%np = floor((%totalItems - 1) / %l); // Number of pages (0-indexed)
	%lb = (%page - 1) * %l; // Lower bound (0-indexed)
	%ub = %lb + %l - 1; // Upper bound (0-indexed)
	if(%ub >= %totalItems)
		%ub = %totalItems - 1;
	
	%cnt = 1;
	%itemIndex = 0;
	%hasItems = false;
	
	// Second pass: display items for current page
	for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
	{
		%item = GetWord(%beltStorage, %i);
		%count = GetWord(%beltStorage, %i + 1);
		
		if(%item != "" && %item != "0" && %count > 0)
		{
			%itemCategory = $BeltItem[%item, "Type"];
			if(%itemCategory == "")
				%itemCategory = "Other"; // Default category
			
			if(%itemCategory == %category)
			{
				// Only display items within the current page range
				if(%itemIndex >= %lb && %itemIndex <= %ub)
				{
					%itemName = $BeltItem[%item, "Name"];
					if(%itemName == "")
					{
						%itemName = $AccessoryVar[%item, $Name];
						if(%itemName == "")
							%itemName = %item;
					}
					Client::addMenuItem(%clientId, %cnt++ @ ": " @ %itemName @ " (" @ %count @ ")", %item @ " " @ %category @ " " @ %page);
					%hasItems = true;
				}
				%itemIndex++;
			}
		}
	}
	
	if(!%hasItems)
		Client::addMenuItem(%clientId, "1No " @ %displayName @ " in storage", "none");
	
	// Add pagination buttons
	if(%page == 1)
	{
		if(%totalItems > %l)
			Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1) @ " " @ %category);
		Client::addMenuItem(%clientId, "xBack", "back");
	}
	else if(%page >= %np + 1)
	{
		// Last page
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1) @ " " @ %category);
		Client::addMenuItem(%clientId, "xBack", "back");
	}
	else
	{
		// Middle page
		Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1) @ " " @ %category);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1) @ " " @ %category);
		Client::addMenuItem(%clientId, "xBack", "back");
	}
}

function processMenuBeltWithdrawCategoryItems(%clientId, %option)
{
	if(%option == "back")
	{
		Belt::ShowWithdrawCategoryMenu(%clientId);
		return;
	}
	
	if(%option == "none")
		return;
	
	// Handle page navigation
	if(GetWord(%option, 0) == "page")
	{
		%newPage = GetWord(%option, 1);
		%category = GetWord(%option, 2);
		Belt::ShowWithdrawCategoryItems(%clientId, %category, %newPage);
		return;
	}
	
	%item = GetWord(%option, 0);
	%category = GetWord(%option, 1);
	// Page is now word 2, but we don't need it for the final menu
	
	// Show the withdraw amount menu
	MenuSellBeltItemFinal(%clientId, %item, %category, "withdraw");
}

function Belt::ShowDepositMenu(%clientId)
{
	// Always rebuild the order list to prevent duplicates from old stored orders
	// Clear stored order to force rebuild each time
	%clientId.beltDepositItemOrder = "";
	
	// Build the order list
		%orderList = "";
		%seenItems = ""; // Track items we've already added to prevent duplicates in order list
		// Check QuestItems
		%questItems = fetchData(%clientId, "QuestItems");
		if(%questItems != "")
		{
			for(%i = 0; GetWord(%questItems, %i) != -1; %i += 2)
			{
				%item = GetWord(%questItems, %i);
				%count = GetWord(%questItems, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item to the order list
					if(String::findSubStr(%seenItems, " " @ %item @ " ") == -1)
				{
					%orderList = %orderList @ " " @ %item @ " QuestItems";
						%seenItems = %seenItems @ " " @ %item @ " ";
					}
				}
			}
		}
		// Check KeyItems
		%keyItems = fetchData(%clientId, "KeyItems");
		if(%keyItems != "")
		{
			for(%i = 0; GetWord(%keyItems, %i) != -1; %i += 2)
			{
				%item = GetWord(%keyItems, %i);
				%count = GetWord(%keyItems, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item to the order list
					if(String::findSubStr(%seenItems, " " @ %item @ " ") == -1)
				{
					%orderList = %orderList @ " " @ %item @ " KeyItems";
						%seenItems = %seenItems @ " " @ %item @ " ";
					}
				}
			}
		}
		// Check Consumables
		%consumables = fetchData(%clientId, "Consumables");
		if(%consumables != "")
		{
			for(%i = 0; GetWord(%consumables, %i) != -1; %i += 2)
			{
				%item = GetWord(%consumables, %i);
				%count = GetWord(%consumables, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item to the order list
					if(String::findSubStr(%seenItems, " " @ %item @ " ") == -1)
					{
						%orderList = %orderList @ " " @ %item @ " Consumables";
						%seenItems = %seenItems @ " " @ %item @ " ";
					}
				}
			}
		}
		// Check Armor
		%armor = fetchData(%clientId, "Armor");
		if(%armor != "")
		{
			for(%i = 0; GetWord(%armor, %i) != -1; %i += 2)
			{
				%item = GetWord(%armor, %i);
				%count = GetWord(%armor, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item to the order list
					if(String::findSubStr(%seenItems, " " @ %item @ " ") == -1)
					{
						%orderList = %orderList @ " " @ %item @ " Armor";
						%seenItems = %seenItems @ " " @ %item @ " ";
					}
				}
			}
		}
		// Check Accessories
		%accessories = fetchData(%clientId, "Accessories");
		if(%accessories != "")
		{
			for(%i = 0; GetWord(%accessories, %i) != -1; %i += 2)
			{
				%item = GetWord(%accessories, %i);
				%count = GetWord(%accessories, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item to the order list
					if(String::findSubStr(%seenItems, " " @ %item @ " ") == -1)
					{
						%orderList = %orderList @ " " @ %item @ " Accessories";
						%seenItems = %seenItems @ " " @ %item @ " ";
					}
				}
			}
		}
		// Check Other
		%other = fetchData(%clientId, "Other");
		if(%other != "")
		{
			for(%i = 0; GetWord(%other, %i) != -1; %i += 2)
			{
				%item = GetWord(%other, %i);
				%count = GetWord(%other, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item to the order list
					if(String::findSubStr(%seenItems, " " @ %item @ " ") == -1)
					{
						%orderList = %orderList @ " " @ %item @ " Other";
						%seenItems = %seenItems @ " " @ %item @ " ";
					}
				}
			}
		}
		%clientId.beltDepositItemOrder = String::NEWgetSubStr(%orderList, 1, 99999);
	
	Client::buildMenu(%clientId, "Deposit Backpack Items:", "BeltDeposit", true);
	
	%cnt = 1;
	%addedItems = ""; // Track items we've already added to prevent duplicates
	// Use stored order if available
	if(%clientId.beltDepositItemOrder != "")
	{
		%storedOrder = %clientId.beltDepositItemOrder;
		for(%i = 0; (%itemData = getWord(%storedOrder, %i)) != -1; %i += 2)
		{
			%item = %itemData;
			%category = getWord(%storedOrder, %i + 1);
			// Check if we've already added this item
			if(String::findSubStr(%addedItems, " " @ %item @ " ") == -1)
			{
			%count = Belt::HasThisStuff(%clientId, %item);
			if(%count > 0)
			{
					%itemName = $BeltItem[%item, "Name"];
					if(%itemName == "")
					{
						// Fallback to AccessoryVar for non-belt items
				%itemName = $AccessoryVar[%item, $Name];
				if(%itemName == "")
					%itemName = %item;
					}
				Client::addMenuItem(%clientId, %cnt @ %itemName @ " (" @ %count @ ")", %item @ " " @ %category);
				%cnt++;
					// Mark this item as added
					%addedItems = %addedItems @ " " @ %item @ " ";
				}
			}
		}
	}
	else
	{
		// Fallback to normal order if no stored order
		%addedItems = ""; // Track items we've already added to prevent duplicates
		// Check QuestItems
		%questItems = fetchData(%clientId, "QuestItems");
		if(%questItems != "")
		{
			for(%i = 0; GetWord(%questItems, %i) != -1; %i += 2)
			{
				%item = GetWord(%questItems, %i);
				%count = GetWord(%questItems, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item
					if(String::findSubStr(%addedItems, " " @ %item @ " ") == -1)
					{
						%itemName = $BeltItem[%item, "Name"];
						if(%itemName == "")
						{
							// Fallback to AccessoryVar for non-belt items
					%itemName = $AccessoryVar[%item, $Name];
					if(%itemName == "")
						%itemName = %item;
						}
					Client::addMenuItem(%clientId, %cnt @ %itemName @ " (" @ %count @ ")", %item @ " QuestItems");
					%cnt++;
						// Mark this item as added
						%addedItems = %addedItems @ " " @ %item @ " ";
					}
				}
			}
		}
		
		// Check KeyItems
		%keyItems = fetchData(%clientId, "KeyItems");
		if(%keyItems != "")
		{
			for(%i = 0; GetWord(%keyItems, %i) != -1; %i += 2)
			{
				%item = GetWord(%keyItems, %i);
				%count = GetWord(%keyItems, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item
					if(String::findSubStr(%addedItems, " " @ %item @ " ") == -1)
					{
						%itemName = $BeltItem[%item, "Name"];
						if(%itemName == "")
						{
							// Fallback to AccessoryVar for non-belt items
					%itemName = $AccessoryVar[%item, $Name];
					if(%itemName == "")
						%itemName = %item;
						}
					Client::addMenuItem(%clientId, %cnt @ %itemName @ " (" @ %count @ ")", %item @ " KeyItems");
					%cnt++;
						// Mark this item as added
						%addedItems = %addedItems @ " " @ %item @ " ";
					}
				}
			}
		}
		
		// Check Consumables
		%consumables = fetchData(%clientId, "Consumables");
		if(%consumables != "")
		{
			for(%i = 0; GetWord(%consumables, %i) != -1; %i += 2)
			{
				%item = GetWord(%consumables, %i);
				%count = GetWord(%consumables, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item
					if(String::findSubStr(%addedItems, " " @ %item @ " ") == -1)
					{
						%itemName = $BeltItem[%item, "Name"];
						if(%itemName == "")
						{
							// Fallback to AccessoryVar for non-belt items
							%itemName = $AccessoryVar[%item, $Name];
							if(%itemName == "")
								%itemName = %item;
						}
						Client::addMenuItem(%clientId, %cnt @ %itemName @ " (" @ %count @ ")", %item @ " Consumables");
						%cnt++;
						// Mark this item as added
						%addedItems = %addedItems @ " " @ %item @ " ";
					}
				}
			}
		}

		// Check Armor
		%armorItems = fetchData(%clientId, "Armor");
		if(%armorItems != "")
		{
			for(%i = 0; GetWord(%armorItems, %i) != -1; %i += 2)
			{
				%item = GetWord(%armorItems, %i);
				%count = GetWord(%armorItems, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item
					if(String::findSubStr(%addedItems, " " @ %item @ " ") == -1)
					{
						%itemName = $BeltItem[%item, "Name"];
						if(%itemName == "")
						{
							%itemName = $AccessoryVar[%item, $Name];
							if(%itemName == "")
								%itemName = %item;
						}
						Client::addMenuItem(%clientId, %cnt @ %itemName @ " (" @ %count @ ")", %item @ " Armor");
						%cnt++;
						// Mark this item as added
						%addedItems = %addedItems @ " " @ %item @ " ";
					}
				}
			}
		}

		// Check Accessories
		%accessoryItems = fetchData(%clientId, "Accessories");
		if(%accessoryItems != "")
		{
			for(%i = 0; GetWord(%accessoryItems, %i) != -1; %i += 2)
			{
				%item = GetWord(%accessoryItems, %i);
				%count = GetWord(%accessoryItems, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item
					if(String::findSubStr(%addedItems, " " @ %item @ " ") == -1)
					{
						%itemName = $BeltItem[%item, "Name"];
						if(%itemName == "")
						{
							%itemName = $AccessoryVar[%item, $Name];
							if(%itemName == "")
								%itemName = %item;
						}
						Client::addMenuItem(%clientId, %cnt @ %itemName @ " (" @ %count @ ")", %item @ " Accessories");
						%cnt++;
						// Mark this item as added
						%addedItems = %addedItems @ " " @ %item @ " ";
					}
				}
			}
		}

		// Check Other
		%otherItems = fetchData(%clientId, "Other");
		if(%otherItems != "")
		{
			for(%i = 0; GetWord(%otherItems, %i) != -1; %i += 2)
			{
				%item = GetWord(%otherItems, %i);
				%count = GetWord(%otherItems, %i + 1);
				if(%count > 0)
				{
					// Check if we've already added this item
					if(String::findSubStr(%addedItems, " " @ %item @ " ") == -1)
					{
						%itemName = $BeltItem[%item, "Name"];
						if(%itemName == "")
						{
							%itemName = $AccessoryVar[%item, $Name];
							if(%itemName == "")
								%itemName = %item;
						}
						Client::addMenuItem(%clientId, %cnt @ %itemName @ " (" @ %count @ ")", %item @ " Other");
						%cnt++;
						// Mark this item as added
						%addedItems = %addedItems @ " " @ %item @ " ";
					}
				}
			}
		}
	}
	
	if(%cnt == 1)
		Client::addMenuItem(%clientId, "1No backpack items to deposit", "none");
	
	Client::addMenuItem(%clientId, "xBack", "back");
}

function processMenuBeltDeposit(%clientId, %option)
{
	%clientName = Client::getName(%clientId);
	if($BELT_DEBUG) echo("DEBUG processMenuBeltDeposit: ENTER - clientId=" @ %clientId @ " (" @ %clientName @ "), option='" @ %option @ "'");
	
	if(%option == "back")
	{
		if($BELT_DEBUG) echo("DEBUG processMenuBeltDeposit: Back option selected");
		// Clear stored order when menu is closed
		%clientId.beltDepositItemOrder = "";
		Belt::Store(%clientId, %clientId.currentBeltBank);
		return;
	}
	
	if(%option == "none")
	{
		if($BELT_DEBUG) echo("DEBUG processMenuBeltDeposit: No items to deposit");
		return;
	}
	
	%item = GetWord(%option, 0);
	%category = GetWord(%option, 1);
	if($BELT_DEBUG) echo("DEBUG processMenuBeltDeposit: Parsed - item='" @ %item @ "', category='" @ %category @ "'");
	
	// Show the 5/10/All menu instead of directly depositing
	if($BELT_DEBUG) echo("DEBUG processMenuBeltDeposit: Showing deposit menu for item");
	MenuSellBeltItemFinal(%clientId, %item, %category, "store");
}

function Belt::ShowWithdrawMenu(%clientId, %page)
{
	%clientName = Client::getName(%clientId);
	if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: ENTER - clientId=" @ %clientId @ " (" @ %clientName @ "), page=" @ %page);
	
	// Default to page 1 if not specified
	if(%page == "" || %page < 1)
		%page = 1;
	
	// Clean up BeltStorage first - remove any invalid entries (item "0", count 0, etc.)
	%beltStorage = fetchData(%clientId, "BeltStorage");
	if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: BEFORE cleanup BeltStorage='" @ %beltStorage @ "'");
	%cleanedStorage = "";
	%removedCount = 0;
	for(%i = 0; GetWord(%beltStorage, %i) != -1; %i+=2)
	{
		%item = GetWord(%beltStorage, %i);
		%count = GetWord(%beltStorage, %i+1);
		if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: Checking item[" @ %i @ "]='" @ %item @ "', count[" @ (%i+1) @ "]='" @ %count @ "'");
		// Convert count to numeric to properly handle negative values like "-1" or "-0"
		%countNum = %count * 1;
		// Only keep valid entries (item is not empty/"0", count is not -1 or "0", and count > 0)
		if(%item != "" && %item != -1 && %item != "0" && %count != "" && %count != -1 && %count != "-1" && %count != "0" && %countNum > 0)
		{
			%cleanedStorage = %cleanedStorage @ %item @ " " @ %count @ " ";
			if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: Added valid entry - item='" @ %item @ "', count=" @ %count);
		}
		else
		{
			if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: Skipping invalid entry - item='" @ %item @ "', count='" @ %count @ "'");
			%removedCount++;
		}
	}
	// Update BeltStorage if it was cleaned (remove trailing space)
	if(%cleanedStorage != %beltStorage)
	{
		if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: Cleaned " @ %removedCount @ " invalid entries from BeltStorage");
		// Remove trailing space if present
		%len = String::len(%cleanedStorage);
		if(%len > 0 && String::getSubStr(%cleanedStorage, %len-1, 1) == " ")
			%cleanedStorage = String::getSubStr(%cleanedStorage, 0, %len-1);
		if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: AFTER cleanup BeltStorage='" @ %cleanedStorage @ "'");
		storeData(%clientId, "BeltStorage", %cleanedStorage);
		%beltStorage = %cleanedStorage;
	}
	else
	{
		if($BELT_DEBUG) echo("DEBUG Belt::ShowWithdrawMenu: No cleanup needed");
	}
	
	// Store the original order when first opening the menu (page 1)
	if(%page == 1 && %clientId.beltWithdrawItemOrder == "")
	{
		%orderList = "";
		for(%i = 0; GetWord(%beltStorage, %i) != -1; %i+=2)
		{
			%item = GetWord(%beltStorage, %i);
			%count = GetWord(%beltStorage, %i+1);
			if(%item != "" && %item != -1 && %item != "0" && %count > 0)
			{
				%orderList = %orderList @ " " @ %item;
			}
		}
		%clientId.beltWithdrawItemOrder = String::NEWgetSubStr(%orderList, 1, 99999);
	}
	
	Client::buildMenu(%clientId, "Withdraw Backpack Items:", "BeltWithdraw", true);
	
	// Pagination settings
	%l = 6; // Items per page
	
	// Count total items
	%totalItems = 0;
	if(%clientId.beltWithdrawItemOrder != "")
	{
		%storedOrder = %clientId.beltWithdrawItemOrder;
		for(%i = 0; (%item = getWord(%storedOrder, %i)) != -1; %i++)
		{
			%count = Belt::ItemCount(%item, %beltStorage);
			if(%count > 0)
				%totalItems++;
		}
	}
	else
	{
		for(%i = 0; GetWord(%beltStorage, %i) != -1; %i+=2)
		{
			%item = GetWord(%beltStorage, %i);
			%count = GetWord(%beltStorage, %i+1);
			if(%item != "" && %item != -1 && %item != "0" && %count > 0)
				%totalItems++;
		}
	}
	
	// Calculate pagination
	%np = floor((%totalItems - 1) / %l); // Number of pages (0-indexed)
	%lb = (%page - 1) * %l; // Lower bound (0-indexed)
	%ub = %lb + %l - 1; // Upper bound (0-indexed)
	if(%ub >= %totalItems)
		%ub = %totalItems - 1;
	
	%cnt = 1;
	%itemIndex = 0;
	
	// Use stored order if available
	if(%clientId.beltWithdrawItemOrder != "")
	{
		%storedOrder = %clientId.beltWithdrawItemOrder;
		for(%i = 0; (%item = getWord(%storedOrder, %i)) != -1; %i++)
		{
			%count = Belt::ItemCount(%item, %beltStorage);
			if(%count > 0)
			{
				// Only display items within the current page range
				if(%itemIndex >= %lb && %itemIndex <= %ub)
				{
					%itemName = $BeltItem[%item, "Name"];
					if(%itemName == "")
					{
						// Fallback to AccessoryVar for non-belt items
						%itemName = $AccessoryVar[%item, $Name];
						if(%itemName == "")
							%itemName = %item;
					}
					Client::addMenuItem(%clientId, %cnt @ ": " @ %itemName @ " (" @ %count @ ")", %item);
					%cnt++;
				}
				%itemIndex++;
			}
		}
	}
	else
	{
		// Fallback to normal order if no stored order
		for(%i = 0; GetWord(%beltStorage, %i) != -1; %i+=2)
		{
			%item = GetWord(%beltStorage, %i);
			%count = GetWord(%beltStorage, %i+1);
			
			// Skip invalid entries (item is "0" or empty, or count is 0 or negative)
			if(%item == "" || %item == -1 || %item == "0" || %count <= 0)
				continue;
			
			// Only display items within the current page range
			if(%itemIndex >= %lb && %itemIndex <= %ub)
			{
				%itemName = $BeltItem[%item, "Name"];
				if(%itemName == "")
				{
					// Fallback to AccessoryVar for non-belt items
					%itemName = $AccessoryVar[%item, $Name];
					if(%itemName == "")
						%itemName = %item;
				}
				
				Client::addMenuItem(%clientId, %cnt @ ": " @ %itemName @ " (" @ %count @ ")", %item);
				%cnt++;
			}
			%itemIndex++;
		}
	}
	
	if(%cnt == 1)
		Client::addMenuItem(%clientId, "1No items in storage", "none");
	
	// Add pagination buttons
	if(%page == 1)
	{
		if(%totalItems > %l)
			Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1));
		Client::addMenuItem(%clientId, "xBack", "back");
	}
	else if(%page >= %np + 1)
	{
		// Last page
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1));
		Client::addMenuItem(%clientId, "xBack", "back");
	}
	else
	{
		// Middle page
		Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1));
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1));
		Client::addMenuItem(%clientId, "xBack", "back");
	}
}

function processMenuBeltWithdraw(%clientId, %option)
{
	%clientName = Client::getName(%clientId);
	if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: ENTER - clientId=" @ %clientId @ " (" @ %clientName @ "), option='" @ %option @ "'");
	
	if(%option == "back")
	{
		if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: Back option selected");
		// Clear stored order when menu is closed
		%clientId.beltWithdrawItemOrder = "";
		Belt::Store(%clientId, %clientId.currentBeltBank);
		return;
	}
	
	if(%option == "none")
	{
		if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: No items in storage");
		return;
	}
	
	// Handle page navigation
	if(GetWord(%option, 0) == "page")
	{
		%newPage = GetWord(%option, 1);
		if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: Navigating to page " @ %newPage);
		Belt::ShowWithdrawMenu(%clientId, %newPage);
		return;
	}
	
	%item = %option;
	if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: Item='" @ %item @ "'");
	
	// Validate item is not empty
	if(%item == "" || %item == -1)
	{
		echo("ERROR: processMenuBeltWithdraw - Invalid item parameter: '" @ %item @ "' from option: '" @ %option @ "' ClientId: " @ %clientId @ " (" @ %clientName @ ")");
		return;
	}
	
	// Determine which category this item belongs to using BeltItem lookup
	// First try the item as-is (it might already be the registered name from BeltStorage)
	%category = $BeltItem[%item, "Type"];
	if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: Lookup category='" @ %category @ "'");
	
	if(%category == "")
	{
		if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: Category not found, defaulting to QuestItems");
		// Item might not be in BeltItem registry directly - try to find it by iterating
		// This shouldn't happen if BeltStorage only contains registered item names, but handle it anyway
		%category = "QuestItems"; // Default category
		
		// Try to find the item in BeltStorage to confirm it exists
		%beltStorage = fetchData(%clientId, "BeltStorage");
		if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: BeltStorage='" @ %beltStorage @ "'");
		%count = Belt::ItemCount(%item, %beltStorage);
		if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: Item count in storage=" @ %count);
		
		if(%count <= 0)
		{
			echo("ERROR: processMenuBeltWithdraw - Item '" @ %item @ "' not found in BeltStorage! ClientId: " @ %clientId @ " (" @ %clientName @ ")");
			return;
		}
	}
	
	// Show the 5/10/All menu instead of directly withdrawing
	if($BELT_DEBUG) echo("DEBUG processMenuBeltWithdraw: Showing withdraw menu for item '" @ %item @ "' category='" @ %category @ "'");
	MenuSellBeltItemFinal(%clientId, %item, %category, "withdraw");
}

//============================================
// BELT MERCHANT SHOP SYSTEM
// Buy belt accessories from merchants
//============================================

function Belt::GetBuyCost(%clientId, %item)
{
	// Get base cost from HardcodedItemCost (same source as sell cost)
	%baseCost = $HardcodedItemCost[%item];
	if(%baseCost == "" || %baseCost == 0)
		%baseCost = 100; // Default price if not defined
	
	%cost = %baseCost;
	
	// Apply haggling discount (reduces buy price)
	%hagglingPercent = round($PlayerSkill[%clientId, $SkillHaggling] / 50) / 100;
	%hagglingPercent = Cap(%hagglingPercent, 0.0, 0.5);
	%cost = round(%cost * (1.0 - %hagglingPercent));
	
	// Apply TournyRank discount
	%tournyRankPercent = 0.03 * fetchData(%clientId, "TournyRank");
	%cost = round(%cost * (1.0 - %tournyRankPercent));
	
	// Ensure cost doesn't go below 1
	if(%cost < 1)
		%cost = 1;
	
	return %cost;
}

// Count how many belt items in %category match the merchant's shop index list.
// Used to hide empty Buy Accessories / Buy Consumables menu options.
function Belt::CountShopItems(%shopIndices, %category)
{
	%count = 0;
	%beltCount = $Belt::Count[%category];
	for(%i = 0; %i < %beltCount; %i++)
	{
		%item = $BeltItem[%i, "Num", %category];
		if(%item == "" || %item == -1)
			continue;

		%itemShopIndex = $AccessoryVar[%item, $ShopIndex];
		if(%itemShopIndex == "" || %itemShopIndex == -1)
			continue;

		for(%j = 0; GetWord(%shopIndices, %j) != -1; %j++)
		{
			if(GetWord(%shopIndices, %j) == %itemShopIndex)
			{
				%count++;
				break;
			}
		}
	}
	return %count;
}

function Belt::Shop(%clientId, %npc, %shopIndices)
{
	// Set flag to track that player is in belt shop menu
	%clientId.currentBeltShop = %npc;
	%clientId.beltShopIndices = %shopIndices;

	AI::sayLater(%clientId, %npc, "Welcome! What can I help you with?", true);

	// HUD clients: skip the multi-option menu entirely - the Kronos
	// shop screen is all-in-one (standard items + belt accessories /
	// consumables, buying AND selling). Vanilla menu flow unchanged.
	if(%clientId.hasKronosHUD)
	{
		SetupShop(%clientId, %npc);
		return;
	}

	// Build top-level menu
	Client::buildMenu(%clientId, ".:( Shop ):.", "BeltShop", true);
	%cnt = 1;
	Client::addMenuItem(%clientId, %cnt++ @ ". Standard Shop", "standard");
	// Only offer accessory/consumable submenus when this merchant actually stocks something there
	if(Belt::CountShopItems(%shopIndices, "Accessories") > 0)
		Client::addMenuItem(%clientId, %cnt++ @ ". Buy Accessories", "buy");
	if(Belt::CountShopItems(%shopIndices, "Consumables") > 0)
		Client::addMenuItem(%clientId, %cnt++ @ ". Buy Consumables", "buyconsumables");
	Client::addMenuItem(%clientId, %cnt++ @ ". Sell Backpack Items", "sell");
	Client::addMenuItem(%clientId, "xFinished", "done");
}

function processMenuBeltShop(%clientId, %opt)
{
	if(%opt == "standard")
	{
		%npc = %clientId.currentBeltShop;
		SetupShop(%clientId, %npc);
	}
	else if(%opt == "buy")
	{
		MenuBuyBeltAccessories(%clientId, 1);
	}
	else if(%opt == "buyconsumables")
	{
		MenuBuyBeltConsumables(%clientId, 1);
	}
	else if(%opt == "sell")
	{
		// Use existing sell menu system
		MenuSellBelt(%clientId);
	}
	else if(%opt == "done")
	{
		%clientId.currentBeltShop = "";
		%clientId.beltShopIndices = "";
		Client::cancelMenu(%clientId);
	}
}

function MenuBuyBeltAccessories(%clientId, %page)
{
	%shopIndices = %clientId.beltShopIndices;
	
	Client::buildMenu(%clientId, ".:( Buy Accessories ):.", "BuyBeltAccessories", true);
	
	// Build list of purchasable items based on shop indices
	// Shop indices are defined in $AccessoryVar[item, $ShopIndex]
	%itemList = "";
	%itemCount = 0;
	
	// Iterate through all belt items in Accessories category
	%beltCount = $Belt::Count["Accessories"];
	for(%i = 0; %i < %beltCount; %i++)
	{
		%item = $BeltItem[%i, "Num", "Accessories"];
		if(%item == "" || %item == -1)
			continue;
		
		%itemShopIndex = $AccessoryVar[%item, $ShopIndex];
		if(%itemShopIndex == "" || %itemShopIndex == -1)
			continue;
		
		// Check if this item's shop index is in the merchant's shop list
		for(%j = 0; GetWord(%shopIndices, %j) != -1; %j++)
		{
			if(GetWord(%shopIndices, %j) == %itemShopIndex)
			{
				%itemList = %itemList @ %item @ " ";
				%itemCount++;
				break;
			}
		}
	}
	
	// Pagination
	%l = 6; // Items per page
	if(%page == "" || %page == -1 || %page == 0) %page = 1;
	%np = floor(%itemCount / %l);
	%lb = (%page * %l) - (%l - 1);
	%ub = %lb + (%l - 1);
	if(%ub > %itemCount)
		%ub = %itemCount;
	
	%cnt = 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%item = GetWord(%itemList, %i - 1);
		if(%item == "" || %item == -1)
			continue;
		
		%name = $BeltItem[%item, "Name"];
		if(%name == "")
			%name = %item;
		
		%cost = Belt::GetBuyCost(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt @ ". " @ %name @ " ($" @ %cost @ ")", %item @ " " @ %page);
		%cnt++;
	}
	
	// If no items found, show message
	if(%itemCount == 0)
	{
		Client::addMenuItem(%clientId, "1: (No accessories available)", "noitems");
	}
	
	// Navigation
	if(%page == 1)
	{
		if(%itemCount > 6)
			Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1));
		Client::addMenuItem(%clientId, "bBack", "back");
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page >= %np + 1 || %np == 0)
	{
		if(%page > 1)
			Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1));
		Client::addMenuItem(%clientId, "bBack", "back");
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1));
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1));
		Client::addMenuItem(%clientId, "bBack", "back");
		Client::addMenuItem(%clientId, "xDone", "done");
	}
}

function processMenuBuyBeltAccessories(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	
	if(%o == "back")
	{
		// Return to main shop menu
		Belt::Shop(%clientId, %clientId.currentBeltShop, %clientId.beltShopIndices);
		return;
	}
	
	if(%o == "done")
	{
		%clientId.currentBeltShop = "";
		%clientId.beltShopIndices = "";
		Client::cancelMenu(%clientId);
		return;
	}
	
	if(%o == "page")
	{
		MenuBuyBeltAccessories(%clientId, %p);
		return;
	}

	if(%o == "noitems" || %o == "cantafford")
	{
		// Ignore - stay on the same menu
		MenuBuyBeltAccessories(%clientId, 1);
		return;
	}

	// Player selected an item to buy - show buy confirmation menu
	MenuBuyBeltItem(%clientId, %o, %p);
}

// Buy Consumables menu - mirrors MenuBuyBeltAccessories but for Consumables category
function MenuBuyBeltConsumables(%clientId, %page)
{
	%shopIndices = %clientId.beltShopIndices;
	
	Client::buildMenu(%clientId, ".:( Buy Consumables ):.", "BuyBeltConsumables", true);
	
	// Build list of purchasable items based on shop indices
	// Shop indices are defined in $AccessoryVar[item, $ShopIndex]
	%itemList = "";
	%itemCount = 0;
	
	// Iterate through all belt items in Consumables category
	%beltCount = $Belt::Count["Consumables"];
	for(%i = 0; %i < %beltCount; %i++)
	{
		%item = $BeltItem[%i, "Num", "Consumables"];
		if(%item == "" || %item == -1)
			continue;
		
		%itemShopIndex = $AccessoryVar[%item, $ShopIndex];
		if(%itemShopIndex == "" || %itemShopIndex == -1)
			continue;
		
		// Check if this item's shop index is in the merchant's shop list
		for(%j = 0; GetWord(%shopIndices, %j) != -1; %j++)
		{
			if(GetWord(%shopIndices, %j) == %itemShopIndex)
			{
				%itemList = %itemList @ %item @ " ";
				%itemCount++;
				break;
			}
		}
	}
	
	// Pagination
	%l = 6; // Items per page
	if(%page == "" || %page == -1 || %page == 0) %page = 1;
	%np = floor(%itemCount / %l);
	%lb = (%page * %l) - (%l - 1);
	%ub = %lb + (%l - 1);
	if(%ub > %itemCount)
		%ub = %itemCount;
	
	%cnt = 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%item = GetWord(%itemList, %i - 1);
		if(%item == "" || %item == -1)
			continue;
		
		%name = $BeltItem[%item, "Name"];
		if(%name == "")
			%name = %item;
		
		%cost = Belt::GetBuyCost(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt @ ". " @ %name @ " ($" @ %cost @ ")", %item @ " " @ %page);
		%cnt++;
	}
	
	// If no items found, show message
	if(%itemCount == 0)
	{
		Client::addMenuItem(%clientId, "1: (No consumables available)", "noitems");
	}
	
	// Navigation
	if(%page == 1)
	{
		if(%itemCount > 6)
			Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1));
		Client::addMenuItem(%clientId, "bBack", "back");
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page >= %np + 1 || %np == 0)
	{
		if(%page > 1)
			Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1));
		Client::addMenuItem(%clientId, "bBack", "back");
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ (%page + 1));
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ (%page - 1));
		Client::addMenuItem(%clientId, "bBack", "back");
		Client::addMenuItem(%clientId, "xDone", "done");
	}
}

function processMenuBuyBeltConsumables(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	
	if(%o == "back")
	{
		// Return to main shop menu
		Belt::Shop(%clientId, %clientId.currentBeltShop, %clientId.beltShopIndices);
		return;
	}
	
	if(%o == "done")
	{
		%clientId.currentBeltShop = "";
		%clientId.beltShopIndices = "";
		Client::cancelMenu(%clientId);
		return;
	}
	
	if(%o == "page")
	{
		MenuBuyBeltConsumables(%clientId, %p);
		return;
	}
	
	if(%o == "noitems" || %o == "cantafford")
	{
		// Ignore - stay on the same menu
		MenuBuyBeltConsumables(%clientId, 1);
		return;
	}
	
	// Player selected an item to buy - show buy confirmation menu
	// Pass "consumables" as the category so back button returns to consumables menu
	MenuBuyBeltItemConsumable(%clientId, %o, %p);
}

// Buy menu for a specific consumable item
function MenuBuyBeltItemConsumable(%clientId, %item, %fromPage)
{
	%name = $BeltItem[%item, "Name"];
	if(%name == "")
		%name = %item;
	
	%cost = Belt::GetBuyCost(%clientId, %item);
	%coins = fetchData(%clientId, "COINS");
	
	Client::buildMenu(%clientId, %name, "BuyBeltItemConsumable", true);
	
	// Add INFO option first as option 1
	// Add "info" as word 1, dummy "x" as word 2 to match buy quantity position
	Client::addMenuItem(%clientId, "1: INFO", %item @ " info x " @ %fromPage);
	
	if(%coins >= %cost)
	{
		Client::addMenuItem(%clientId, "2: Buy 1 ($" @ %cost @ ")", %item @ " buy 1 " @ %fromPage);
	}
	else
	{
		Client::addMenuItem(%clientId, "2: (Cannot afford - $" @ %cost @ ")", "cantafford");
	}
	
	// Show bulk options if player can afford
	if(%coins >= %cost * 5)
		Client::addMenuItem(%clientId, "3: Buy 5 ($" @ (%cost * 5) @ ")", %item @ " buy 5 " @ %fromPage);
	if(%coins >= %cost * 10)
		Client::addMenuItem(%clientId, "4: Buy 10 ($" @ (%cost * 10) @ ")", %item @ " buy 10 " @ %fromPage);
	
	Client::addMenuItem(%clientId, "bBack", "back " @ %fromPage);
	Client::addMenuItem(%clientId, "xDone", "done");
}

function processMenuBuyBeltItemConsumable(%clientId, %opt)
{
	%item = GetWord(%opt, 0);
	%action = GetWord(%opt, 1);
	%qty = GetWord(%opt, 2);
	%fromPage = GetWord(%opt, 3);

	// review #16: %opt is client-supplied (menu.cs clientMenuSelect echoes the
	// code back to the server), so %qty is untrusted. A negative qty makes %cost
	// negative and CREDITS coins on "buy" (money dupe). Clamp to a positive int.
	%qty = floor(%qty);
	if(%qty < 1)
		%qty = 1;

	if(%item == "back")
	{
		MenuBuyBeltConsumables(%clientId, %action);
		return;
	}
	
	if(%item == "done")
	{
		%clientId.currentBeltShop = "";
		%clientId.beltShopIndices = "";
		Client::cancelMenu(%clientId);
		return;
	}
	
	if(%item == "cantafford")
	{
		MenuBuyBeltItemConsumable(%clientId, %item, %fromPage);
		return;
	}
	
	if(%action == "info")
	{
		// Display consumable info using the same function as #w command (Acessory logic)
		%msg = WhatIs(%item);
		KronosExamineInfo(%clientId, %msg, 5);
		
		MenuBuyBeltItemConsumable(%clientId, %item, %fromPage);
		return;
	}
	
	if(%action == "buy")
	{
		%cost = Belt::GetBuyCost(%clientId, %item) * %qty;
		%coins = fetchData(%clientId, "COINS");
		
		if(%coins >= %cost)
		{
			// review #20: pay via positive "dec", not "inc" of a negative amount -
			// storeData treats a literal -1 as "unassigned" and zeroes it, so a
			// heavily-discounted cost-1 item was previously free.
			storeData(%clientId, "COINS", %cost, "dec");
			Belt::GiveThisStuff(%clientId, %item, %qty, true);
			
			%name = $BeltItem[%item, "Name"];
			if(%name == "")
				%name = %item;
			
			Client::sendMessage(%clientId, $MsgGreen, "You purchased " @ %qty @ " " @ %name @ " for $" @ %cost @ ".~wbuysellsound.wav");
			
			RefreshAll(%clientId);
			SaveCharacter(%clientId);
		}
		else
		{
			Client::sendMessage(%clientId, $MsgRed, "You cannot afford this item.~wC_BuySell.wav");
		}
		
		MenuBuyBeltItemConsumable(%clientId, %item, %fromPage);
		return;
	}
	
	// Default - return to item menu
	MenuBuyBeltItemConsumable(%clientId, %item, %fromPage);
}

function MenuBuyBeltItem(%clientId, %item, %fromPage)
{
	%name = $BeltItem[%item, "Name"];
	if(%name == "")
		%name = %item;
	
	%cost = Belt::GetBuyCost(%clientId, %item);
	%coins = fetchData(%clientId, "COINS");
	
	Client::buildMenu(%clientId, %name, "BuyBeltItem", true);
	
	// Add INFO option first as option 1
	// Add "info" as word 1, dummy "x" as word 2 to match buy quantity position
	Client::addMenuItem(%clientId, "1: INFO", %item @ " info x " @ %fromPage);
	
	if(%coins >= %cost)
	{
		Client::addMenuItem(%clientId, "2: Buy 1 ($" @ %cost @ ")", %item @ " buy 1 " @ %fromPage);
	}
	else
	{
		Client::addMenuItem(%clientId, "2: (Cannot afford - $" @ %cost @ ")", "cantafford");
	}
	
	// Show bulk options if player can afford
	if(%coins >= %cost * 5)
		Client::addMenuItem(%clientId, "3: Buy 5 ($" @ (%cost * 5) @ ")", %item @ " buy 5 " @ %fromPage);
	if(%coins >= %cost * 10)
		Client::addMenuItem(%clientId, "4: Buy 10 ($" @ (%cost * 10) @ ")", %item @ " buy 10 " @ %fromPage);
	
	Client::addMenuItem(%clientId, "bBack", "back " @ %fromPage);
	Client::addMenuItem(%clientId, "xDone", "done");
}

function processMenuBuyBeltItem(%clientId, %opt)
{
	%item = GetWord(%opt, 0);
	%action = GetWord(%opt, 1);
	%amount = GetWord(%opt, 2);
	%fromPage = GetWord(%opt, 3);

	// review #16: %opt is client-supplied, so %amount is untrusted. A negative
	// amount makes %cost negative; the affordability gate passes and "dec" of a
	// negative amount CREDITS coins (money dupe). Clamp to a positive int.
	%amount = floor(%amount);
	if(%amount < 1)
		%amount = 1;

	if(%item == "done" || %action == "")
	{
		%clientId.currentBeltShop = "";
		%clientId.beltShopIndices = "";
		Client::cancelMenu(%clientId);
		return;
	}
	
	if(%item == "back")
	{
		// %item is "back", %action is the page number
		MenuBuyBeltAccessories(%clientId, %action);
		return;
	}
	
	if(%action == "back")
	{
		// Old format fallback
		MenuBuyBeltAccessories(%clientId, %amount);
		return;
	}
	
	if(%action == "cantafford")
	{
		MenuBuyBeltItem(%clientId, %item, %fromPage);
		return;
	}
	
	if(%action == "info")
	{
		// Display accessory info using the same function as #w command
		%msg = WhatIs(%item);
		KronosExamineInfo(%clientId, %msg, 5);
		
		// Return to the buy menu
		MenuBuyBeltItem(%clientId, %item, %fromPage);
		return;
	}
	
	if(%action == "buy")
	{
		%cost = Belt::GetBuyCost(%clientId, %item) * %amount;
		%coins = fetchData(%clientId, "COINS");
		
		if(%coins < %cost)
		{
			Client::sendMessage(%clientId, $MsgRed, "You cannot afford this purchase.~wC_BuySell.wav");
			MenuBuyBeltItem(%clientId, %item, %fromPage);
			return;
		}
		
		// Deduct coins
		storeData(%clientId, "COINS", %cost, "dec");
		
		// Give item to belt storage
		Belt::GiveThisStuff(%clientId, %item, %amount);
		
		// Play sound and notify
		%name = $BeltItem[%item, "Name"];
		if(%name == "")
			%name = %item;
		Client::sendMessage(%clientId, $MsgWhite, "You purchased " @ %amount @ " " @ %name @ ".~wbuysellsound.wav");
		
		// Use haggling skill
		UseSkill(%clientId, $SkillHaggling, True, True);
		
		// Refresh and return to item menu
		RefreshAll(%clientId);
		MenuBuyBeltItem(%clientId, %item, %fromPage);
	}
}
