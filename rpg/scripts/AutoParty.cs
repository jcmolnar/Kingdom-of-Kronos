//====================================================================================================
// AutoParty.cs - Automatic Party System
//====================================================================================================
// Allows players to enable auto-party so they automatically join/create parties when entering
// dungeons with other players who also have auto-party enabled.
// 
// Usage:
//   #autoparty on - Enable auto-party
//   #autoparty off - Disable auto-party
//   #autoparty - Show current status
//
// Data is stored in funkvar slot 56 for save/load persistence.
//
//----------------------------------------------------------------------------------------------------
// HOOKS / INTEGRATIONS:
//----------------------------------------------------------------------------------------------------
// Server.cs        - exec(AutoParty); added to load this script
// zone.cs          - AutoParty_OnZoneEnter() called from Zone::DoEnter() for DUNGEON zones only
// rpgfunk.cs       - SaveCharacter() saves AutoParty_Enabled to funkvar slot 56
// rpgfunk.cs       - LoadCharacter() loads AutoParty_Enabled from funkvar slot 56
// comchat.cs       - #autoparty command handler added
// party.cs         - Uses CreateParty(), AddToParty(), IsInWhichParty()
//====================================================================================================

// Enable auto-party for a player
function AutoParty_Enable(%clientId)
{
	dbecho($dbechoMode, "AutoParty_Enable(" @ %clientId @ ")");
	
	storeData(%clientId, "AutoParty_Enabled", "true");
	Client::sendMessage(%clientId, $MsgBeige, "Auto-Party ENABLED. You will automatically join parties in dungeons.");
}

// Disable auto-party for a player
function AutoParty_Disable(%clientId)
{
	dbecho($dbechoMode, "AutoParty_Disable(" @ %clientId @ ")");
	
	storeData(%clientId, "AutoParty_Enabled", "");
	Client::sendMessage(%clientId, $MsgBeige, "Auto-Party DISABLED.");
}

// Check if auto-party is enabled for a player
function AutoParty_IsEnabled(%clientId)
{
	%enabled = fetchData(%clientId, "AutoParty_Enabled");
	return (%enabled == "true" || %enabled == "True" || %enabled == "1");
}

// Show current auto-party status
function AutoParty_Show(%clientId)
{
	dbecho($dbechoMode, "AutoParty_Show(" @ %clientId @ ")");
	
	if(AutoParty_IsEnabled(%clientId))
		Client::sendMessage(%clientId, $MsgBeige, "Auto-Party is currently ENABLED.");
	else
		Client::sendMessage(%clientId, $MsgBeige, "Auto-Party is currently DISABLED.");
}

// Called when a player enters a DUNGEON zone - hooked from Zone::DoEnter
// NOTE: Zone type check and bot check is already done in the hook, so we skip those here
function AutoParty_OnZoneEnter(%clientId, %oldZone, %newZone)
{
	dbecho($dbechoMode, "AutoParty_OnZoneEnter(" @ %clientId @ ", " @ %oldZone @ ", " @ %newZone @ ")");
	
	// Skip if player doesn't have auto-party enabled
	if(!AutoParty_IsEnabled(%clientId))
		return;
	

	// Check if player is already in a party
	%playerName = Client::getName(%clientId);
	%currentParty = IsInWhichParty(%playerName);
	%partyOwner = GetWord(%currentParty, 0);
	
	if(%partyOwner != -1 && %partyOwner != "")
	{
		// Already in a party - don't do anything
		return;
	}
	
	// Get list of other players in this dungeon
	%playerList = Zone::getPlayerList(%newZone, 2); // type 2 = players only
	
	// Look for party to join or players to party with
	%foundParty = false;
	%foundAutoPartyPlayer = -1;
	
	for(%i = 0; GetWord(%playerList, %i) != -1; %i++)
	{
		%otherId = GetWord(%playerList, %i);
		
		// Skip self
		if(%otherId == %clientId)
			continue;
		
		// Skip bots
		if(Player::isAiControlled(%otherId))
			continue;
		
		// Check if they have auto-party enabled
		if(!AutoParty_IsEnabled(%otherId))
			continue;
		
		%otherName = Client::getName(%otherId);
		
		// Check if they own a party
		if(fetchData(%otherId, "partyOwned"))
		{
			// Check if party has room (max 4 members)
			%partyList = fetchData(%otherId, "partylist");
			%memberCount = CountObjInCommaList(%partyList);
			
			if(%memberCount < $maxpartymembers)
			{
				// Join their party
				AddToParty(%otherId, %playerName);
				Client::sendMessage(%clientId, $MsgBeige, "[Auto-Party] Joined " @ %otherName @ "'s party!");
				%foundParty = true;
				break;
			}
		}
		else
		{
			// Check if they're in someone else's party
			%theirParty = IsInWhichParty(%otherName);
			%theirPartyOwner = GetWord(%theirParty, 0);
			
			if(%theirPartyOwner != -1 && %theirPartyOwner != "")
			{
				// They're in a party - check if we can join
				%partyList = fetchData(%theirPartyOwner, "partylist");
				%memberCount = CountObjInCommaList(%partyList);
				
				if(%memberCount < $maxpartymembers)
				{
					// Check if party owner has auto-party enabled
					if(AutoParty_IsEnabled(%theirPartyOwner))
					{
						AddToParty(%theirPartyOwner, %playerName);
						%ownerName = Client::getName(%theirPartyOwner);
						Client::sendMessage(%clientId, $MsgBeige, "[Auto-Party] Joined " @ %ownerName @ "'s party!");
						%foundParty = true;
						break;
					}
				}
			}
			else
			{
				// They're not in any party - remember them for potential new party
				if(%foundAutoPartyPlayer == -1)
					%foundAutoPartyPlayer = %otherId;
			}
		}
	}
	
	// If we didn't find a party to join, but found another auto-party player, create a new party
	if(!%foundParty && %foundAutoPartyPlayer != -1)
	{
		// Create a new party with us as owner
		CreateParty(%clientId);
		
		// Add the other player
		%otherName = Client::getName(%foundAutoPartyPlayer);
		AddToParty(%clientId, %otherName);
		
		Client::sendMessage(%clientId, $MsgBeige, "[Auto-Party] Created party and added " @ %otherName @ "!");
		Client::sendMessage(%foundAutoPartyPlayer, $MsgBeige, "[Auto-Party] " @ %playerName @ " created a party with you!");
	}
}

echo("AutoParty.cs loaded");
