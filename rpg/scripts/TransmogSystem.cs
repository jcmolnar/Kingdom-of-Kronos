// TransmogStation.cs
// Handles the Transmog Station menu and logic for changing player skins

// ----------------------------------------------------------------------------------------------------
// Station Player Detection (Timer-based since animation callbacks don't fire for InventoryStation)
// ----------------------------------------------------------------------------------------------------

function TransmogStation::checkPlayerLeft(%station)
{
	// If station was deactivated (by selection, exit, etc.), stop checking
	if(!GameBase::isActive(%station))
		return;
	
	// Check if a player is still in range using LOS
	%target = Station::getTarget(%station);
	
	if(%target == -1)
	{
		// No player in range - deactivate the station so it can reopen on next touch
		GameBase::setActive(%station, false);
		return;
	}
	
	// Player still here, check again in 1 second
	schedule("TransmogStation::checkPlayerLeft(" @ %station @ ");", 1.0, %station);
}

// ----------------------------------------------------------------------------------------------------
// Transmog Menu Generation
// ----------------------------------------------------------------------------------------------------

function GenerateTransmogMenu(%clientId)
{
	Client::buildMenu(%clientId, "Transmog Station", "transmog", true);
	
	Client::addMenuItem(%clientId, "1Human Faces / Skins", "menu_faces_1");
	Client::addMenuItem(%clientId, "2Tribes Clan Skins", "menu_clans");
	Client::addMenuItem(%clientId, "3Robe Visual Skins", "menu_robes");
	Client::addMenuItem(%clientId, "4Armor Visual Skins", "menu_armor_1");
	Client::addMenuItem(%clientId, "5Monster Transformations", "menu_monsters_1");
	Client::addMenuItem(%clientId, "6Clear Transmog", "clear_transmog");
}

function GenerateFaceMenuPage1(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Faces (Page 1)", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Base Skin (Male)", "rpgbase.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Base Skin (Female)", "rpgbase.female");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 1 (Human)", "rpghuman.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 2 (Human)", "rpghuman0.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 3 (Human)", "rpghuman1.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 4 (Human)", "rpghuman2.male");
	
	Client::addMenuItem(%clientId, "nNext Page", "menu_faces_2");
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateFaceMenuPage2(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Faces (Page 2)", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Face 5 (Human)", "rpghuman3.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 6 (Human)", "rpghuman4.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 7 (Human)", "rpghuman6.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 8 (Human)", "rpghuman7.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 9 (Human)", "rpghuman8.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Face 10 (Human)", "rpghuman9.male");
	
	Client::addMenuItem(%clientId, "nNext Page", "menu_faces_3");
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateFaceMenuPage3(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Faces (Page 3)", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Face 11 (Human)", "rpghuman10.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Elf Skin (Male)", "rpgelf.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Elf Skin (Female)", "rpgelf.female");
	Client::addMenuItem(%clientId, %curItem++ @ "Pale Skin (Male)", "rpgwhitedude.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Alishaa (Female)", "alishaa.female");
	
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateTransmogMenuPage2(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Clan Skins", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Beagle (Male)", "beagle.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Beagle (Female)", "beagle.female");
	Client::addMenuItem(%clientId, %curItem++ @ "Diamond Sword (Male)", "dsword.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Diamond Sword (Female)", "dsword.female");
	Client::addMenuItem(%clientId, %curItem++ @ "Star Wolf (Male)", "swolf.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Star Wolf (Female)", "swolf.female");
	
	Client::addMenuItem(%clientId, "nNext Page", "menu_clans_2");
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateTransmogMenuPage2b(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Clan Skins (Pg 2)", "transmog", true);

	Client::addMenuItem(%clientId, %curItem++ @ "Child Phoenix (Male)", "cphoenix.male");
	Client::addMenuItem(%clientId, %curItem++ @ "Child Phoenix (Fem)", "cphoenix.female");

	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateRobeMenu(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Robe Visual Skins", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Black Robe", "robeblack");
	Client::addMenuItem(%clientId, %curItem++ @ "Blue Robe", "robeblue");
	Client::addMenuItem(%clientId, %curItem++ @ "Brown Robe", "robebrown");
	Client::addMenuItem(%clientId, %curItem++ @ "Green Robe", "robegreen");
	Client::addMenuItem(%clientId, %curItem++ @ "Orange Robe", "robeorange");
	Client::addMenuItem(%clientId, %curItem++ @ "Pink Robe", "robepink");
	Client::addMenuItem(%clientId, %curItem++ @ "Purple Robe", "robepurple");
	Client::addMenuItem(%clientId, %curItem++ @ "Red Robe", "robered");
	Client::addMenuItem(%clientId, %curItem++ @ "White Robe", "robewhite");
	
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateArmorVisualMenu1(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Armor Visuals (Pg 1)", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Banded Mail", "rpgbandedmail");
	Client::addMenuItem(%clientId, %curItem++ @ "Brigandine", "rpgbrigandine");
	Client::addMenuItem(%clientId, %curItem++ @ "Bronze Plate", "rpgbronzeplate");
	Client::addMenuItem(%clientId, %curItem++ @ "Chainmail", "rpgchainmail");
	Client::addMenuItem(%clientId, %curItem++ @ "Field Plate", "rpgfieldplate");
	Client::addMenuItem(%clientId, %curItem++ @ "Full Plate", "rpgfullplate");
	Client::addMenuItem(%clientId, %curItem++ @ "Hide Armor", "rpghide");
	Client::addMenuItem(%clientId, %curItem++ @ "Leather Armor", "rpgleather");
	
	Client::addMenuItem(%clientId, "nNext Page", "menu_armor_2");
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateArmorVisualMenu2(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Armor Visuals (Pg 2)", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Padded Armor", "rpgpadded");
	Client::addMenuItem(%clientId, %curItem++ @ "Platemail", "rpgplatemail");
	Client::addMenuItem(%clientId, %curItem++ @ "Ringmail", "rpgringmail");
	Client::addMenuItem(%clientId, %curItem++ @ "Scalemail", "rpgscalemail");
	Client::addMenuItem(%clientId, %curItem++ @ "Spiked Armor", "rpgspiked");
	Client::addMenuItem(%clientId, %curItem++ @ "Splintmail", "rpgsplintmail");
	Client::addMenuItem(%clientId, %curItem++ @ "Studded Leather", "rpgstudleather");
	
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateMonsterMenuPage1(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Monster Transform (Pg 1)", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Orc (Medium)", "orc");
	Client::addMenuItem(%clientId, %curItem++ @ "Ogre (Heavy)", "ogre");
	Client::addMenuItem(%clientId, %curItem++ @ "Ogre (Red Eye)", "ogre_red");
	Client::addMenuItem(%clientId, %curItem++ @ "Gnoll (Medium)", "gnoll");
	Client::addMenuItem(%clientId, %curItem++ @ "Minotaur", "minotaur");
	Client::addMenuItem(%clientId, %curItem++ @ "Zombie", "zombie");
	Client::addMenuItem(%clientId, %curItem++ @ "Skeleton", "undead");
	
	Client::addMenuItem(%clientId, "nNext Page", "menu_monsters_2");
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

function GenerateMonsterMenuPage2(%clientId)
{
	%curItem = 0;
	Client::buildMenu(%clientId, "Monster Transform (Pg 2)", "transmog", true);
	
	Client::addMenuItem(%clientId, %curItem++ @ "Alien", "alien");
	Client::addMenuItem(%clientId, %curItem++ @ "Uber", "uber");
	Client::addMenuItem(%clientId, %curItem++ @ "Angel", "angel");
	Client::addMenuItem(%clientId, %curItem++ @ "Admin Boss", "admin");
	Client::addMenuItem(%clientId, %curItem++ @ "Seals", "seals");
	Client::addMenuItem(%clientId, %curItem++ @ "God", "god");
	
	Client::addMenuItem(%clientId, "pBack to Main", "main_menu");
}

// ----------------------------------------------------------------------------------------------------
// Transmog Menu Processing
// ----------------------------------------------------------------------------------------------------

function processMenuTransmog(%clientId, %option)
{
	// Handle paging & categories
	if(%option == "main_menu") { GenerateTransmogMenu(%clientId); return; }
	
	if(%option == "menu_faces_1") { GenerateFaceMenuPage1(%clientId); return; }
	if(%option == "menu_faces_2") { GenerateFaceMenuPage2(%clientId); return; }
	if(%option == "menu_faces_3") { GenerateFaceMenuPage3(%clientId); return; }
	
	if(%option == "menu_clans") { GenerateTransmogMenuPage2(%clientId); return; }
	if(%option == "menu_clans_2") { GenerateTransmogMenuPage2b(%clientId); return; }
	
	if(%option == "menu_robes") { GenerateRobeMenu(%clientId); return; }
	if(%option == "menu_armor_1") { GenerateArmorVisualMenu1(%clientId); return; }
	if(%option == "menu_armor_2") { GenerateArmorVisualMenu2(%clientId); return; }
	
	if(%option == "menu_monsters_1") { GenerateMonsterMenuPage1(%clientId); return; }
	if(%option == "menu_monsters_2") { GenerateMonsterMenuPage2(%clientId); return; }
	
	if(%option == "clear_transmog")
	{
		storeData(%clientId, "PersonalSkin", "");
		storeData(%clientId, "IsMonsterSkin", "");
		storeData(%clientId, "TransmogRace", "");
		// $Client::info[%clientId, 0] = "rpgbase"; // CRITICAL: Never overwrite word 0, it's the client's name!
		Client::sendMessage(%clientId, 0, "Your appearance has been reset to defaults.");
		Game::refreshClientScore(%clientId);
		RefreshAll(%clientId);
		
		// Deactivate the station so the player can touch it again to reopen the menu
		%player = Client::getOwnedObject(%clientId);
		if(%player != "" && %player != -1)
		{
			%station = %player.Station;
			if(%station != "" && isObject(%station))
			{
				GameBase::setActive(%station, false);
			}
			%player.Station = "";
		}
		return;
	}
	
	%skinName = %option;
	
	// Validate selection
	if(%skinName == "")
		return;
		
	// Map friendly names to actual skin files/logic
	%finalSkin = "";
	%isMonster = false;
	%raceOverride = "";
	
	// Special mapping for monsters
	if(%skinName == "orc") { %finalSkin = "rpgorc"; %isMonster = true; %raceOverride = "Orc"; }
	else if(%skinName == "ogre") { %finalSkin = "rpgorc"; %isMonster = true; %raceOverride = "Ogre"; }
	else if(%skinName == "ogre_red") { %finalSkin = "redgodeye"; %isMonster = true; %raceOverride = "Ogre"; }
	else if(%skinName == "gnoll") { %finalSkin = "rpggnoll"; %isMonster = true; %raceOverride = "Pigman"; }
	else if(%skinName == "minotaur") { %finalSkin = "min"; %isMonster = true; %raceOverride = "Minotaur"; }
	else if(%skinName == "zombie") { %finalSkin = "zombie"; %isMonster = true; %raceOverride = "Zombie"; }
	else if(%skinName == "undead") { %finalSkin = "undead"; %isMonster = true; %raceOverride = "Undead"; }
	else if(%skinName == "alien") { %finalSkin = "alien"; %isMonster = true; %raceOverride = "Alien"; }
	else if(%skinName == "uber") { %finalSkin = "uber"; %isMonster = true; %raceOverride = "Uber"; }
	else if(%skinName == "angel") { %finalSkin = "angel"; %isMonster = true; %raceOverride = "Angel"; }
	else if(%skinName == "admin") { %finalSkin = "admin"; %isMonster = true; %raceOverride = "Admin"; }
	else if(%skinName == "seals") { %finalSkin = "seals"; %isMonster = true; %raceOverride = "Seals"; }
	else if(%skinName == "god") { %finalSkin = "god"; %isMonster = true; %raceOverride = "God"; }
	else
	{
		// Human/Tribes/Robe skins
		%finalSkin = %skinName;
	}
	
	// Save the preference
	if(%isMonster)
	{
		// $Client::info[%clientId, 0] = %finalSkin; // CRITICAL: Never overwrite word 0, it's the client's name!
		storeData(%clientId, "PersonalSkin", %finalSkin);
		storeData(%clientId, "IsMonsterSkin", true);
		storeData(%clientId, "TransmogRace", %raceOverride);
		Client::sendMessage(%clientId, 0, "You have been transformed into a " @ %skinName @ "!");
	}
	else
	{
		// $Client::info[%clientId, 0] = %finalSkin; // CRITICAL: Never overwrite word 0, it's the client's name!
		storeData(%clientId, "PersonalSkin", %finalSkin);
		storeData(%clientId, "IsMonsterSkin", ""); 
		storeData(%clientId, "TransmogRace", "");
		Client::sendMessage(%clientId, 0, "Your appearance has been updated.");
	}
	
	// Apply immediately
	Game::refreshClientScore(%clientId);
	RefreshAll(%clientId);
	
	// Deactivate the station so the player can touch it again to reopen the menu
	%player = Client::getOwnedObject(%clientId);
	if(%player != "" && %player != -1)
	{
		%station = %player.Station;
		if(%station != "" && isObject(%station))
		{
			GameBase::setActive(%station, false);
		}
		%player.Station = "";
	}
}
