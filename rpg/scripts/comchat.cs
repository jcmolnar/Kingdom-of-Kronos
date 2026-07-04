$MsgTypeSystem = 0;
$MsgTypeGame = 1;
$MsgTypeChat = 2;
$MsgTypeTeamChat = 3;
$MsgTypeCommand = 4;

$MsgWhite = 0;
$MsgRed = 1;
$MsgBeige = 2;
$MsgGreen = 3;
$Refresh = "0";//Make sure you put this at the top of the comchat script

// Restored helper for the #animations command. Plays the engine "player wave"
// animations on a bot one at a time with 3s gaps. RemotePlayAnim(clientId, idx)
// -> serverWave, which only plays ANIM_PLAYER_FIRST..LAST (indices 0-12:
// 0 over-here, 1 point, 2 retreat, 3 stop, 4 salute, 5 celebration 1,
// 6 celebration 2, 7 celebration 3, 8 taunt 1, 9 taunt 2, 10 pose kneel,
// 11 pose stand, 12 wave). Indices >12 (e.g. run/die) are NOT playable this way.
function PlayAnimationSequence(%clientId, %anim, %TrueClientId)
{
	// Done after the valid wave range.
	if(%anim > 12)
	{
		if(%TrueClientId != "")
			Client::sendMessage(%TrueClientId, $MsgGreen, "Animation sequence complete (played 0-12).");
		return;
	}

	// Bot may have died / despawned mid-sequence.
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == "" || %playerObj == -1)
		return;

	RemotePlayAnim(%clientId, %anim);
	if(%TrueClientId != "")
		Client::sendMessage(%TrueClientId, $MsgBeige, "Playing wave anim index " @ %anim @ " on client " @ %clientId);

	schedule("PlayAnimationSequence(" @ %clientId @ ", " @ (%anim + 1) @ ", " @ %TrueClientId @ ");", 3);
}

function remoteSay(%clientId, %team, %message, %senderName)
{
	dbecho($dbechoMode, "remoteSay(" @ %clientId @ ", " @ %team @ ", \"" @ %message @ "\", " @ %senderName @ ")");

	if(%clientId.IsInvalid)
		return;
	
	// CRITICAL: Prevent enemy bots from using any chat channels (say, global, etc.)
	// Only check if senderName is empty (meaning it's a direct client call, not a forwarded message)
	// EXCEPTION: Allow #cast commands for enemy bots (needed for Casting Blade weapon)
	if(%senderName == "")
	{
		if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		{
			// Check if this is an enemy bot (has SpawnBotInfo) - enemy bots should not chat
			%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
			if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
			{
			// Check if this is a #cast command - allow it for Casting Blade
			%w1 = GetWord(%message, 0);
			if(%w1 != "#cast")
			{
				// Enemy bot trying to chat (not a cast command) - silently block
				return;
			}
			}
		}
	}

	//-------------------------//
	%TrueClientId = %clientId;
	if(%senderName != "")
	{
		%clientId = 2048;
		%clientToServerAdminLevel = $BlockOwnerAdminLevel[%senderName];
	}
	else
	{
		%senderName = Client::getName(%clientId);
		%clientToServerAdminLevel = floor(%clientId.adminLevel);
		
		// COMMENTED OUT: Prevent TournyRank 9 (Champion) and TournyRank 10 (Grand Champion) from having admin capabilities
		// These are Colosseum achievement ranks, not admin ranks
		// %tournyRank = fetchData(%clientId, "TournyRank");
		// if(%tournyRank == 9 || %tournyRank == 10)
		// {
		// 	// Override adminLevel - these ranks should not grant admin capabilities
		// 	if(%clientToServerAdminLevel == 1 || %clientToServerAdminLevel == 2)
		// 		%clientToServerAdminLevel = 0;
		// }
		
		// Allow admin level 1 for actual admins (those in TaurikAdmin list)
		// Check if player is in TaurikAdmin list before blocking admin level 1
		if(%clientToServerAdminLevel == 1 || %clientToServerAdminLevel == 2)
		{
			%isInTaurikAdmin = false;
			%clientName = Client::getName(%clientId);
			
			// Check if player is in TaurikAdmin list
			for(%i = 0; $TaurikAdmin[%i] != ""; %i++)
			{
				%adminline = $TaurikAdmin[%i];
				%adminname = getWord(%adminline, 0);
				
				// Use case-insensitive comparison for name matching
				if(String::ICompare(%clientName, %adminname) == 0)
				{
					%isInTaurikAdmin = true;
					break;
				}
			}
			
			// Only block admin level 1/2 if player is NOT in TaurikAdmin list
			// This allows actual admins to use admin level 1, but prevents TournyRank players from getting admin
			if(!%isInTaurikAdmin)
			{
				%tournyRank = fetchData(%clientId, "TournyRank");
				if(%tournyRank == 9 || %tournyRank == 10)
				{
					// Override adminLevel - these ranks should not grant admin capabilities
					%clientToServerAdminLevel = 0;
				}
			}
		}
	}
	if(Player::isAiControlled(%clientId))
		%clientToServerAdminLevel = 3;

	if(%TrueClientId == 2048)
		%echoOff = True;
	else
		%echoOff = %TrueClientId.echoOff;

	if(%TrueClientId != 2048)
		%TCsenderName = Client::getName(%TrueClientId);
	else
		%TCsenderName = %senderName;
	
	if(String::ICompare(%senderName, "Server") == 0 && %TrueClientId != 2048)
     { return; }

	//If %senderName is empty, the rest of this function will continue normally, as both %TrueClientId and %clientId
	//are identical.  However, if %senderName is NOT empty, messages that the server should hear will be under %clientId,
	//and messages that the client RUNNING the script needs to hear will be under %TrueClientId.
	//During %senderName being NOT empty, basic player command messages are sent to the server.  These commands shouldn't
	//normally be invoked anyway, unless the scripter forces it somehow.  Block management commands should use
	//%TrueClientId because they can only be run WHILE the client is in-game, so the messages should be sent to him.
	//The rest of the commands should use %clientId because those are the ones that the server will be calling.

	//An easy to way to distinguish the tasks between client and server is that the client runs the commands that
	//manage, while the server runs the commands that do actions.

	//- %TrueClientId should be assigned to things that require client access, and need to send a message
	//  (like a confirmation or error message) to someone.
	//- %clientId should be assigned to things that do actions.

	//%TrueClientId will only become 2048 if the client leaves the game.

	//Remember that if a client disconnects, the %TrueClientId will become 2048, the same as %clientId.  This means
	//that the server will then be receiving all these messages.

	//I had to write this little commentary because I was getting confused myself...

	//NEW:
	//- %TrueClientId should be assigned to things that involve the player at hand
	//- %clientId should be assigned to things that involve control
	//-------------------------//

	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastSayTime <= $sayDelay && !(%clientToServerAdminLevel >= 1))
		return;
	%clientId.lastSayTime = %time;

	%msg = %clientId @ " \"" @ escapeString(%message) @ "\"";

	// check for flooding if it's a broadcast OR if it's team in FFA
	if($Server::FloodProtectionEnabled && (!$Server::TourneyMode || !%team) && !(%clientToServerAdminLevel >= 1))
	{
		// we use getIntTime here because getSimTime gets reset.
		// time is measured in 32 ms chunks... so approx 32 to the sec
		%time = getIntegerTime(true) >> 5;
		if(%TrueClientId.floodMute)
		{
			%delta = %TrueClientId.muteDoneTime - %time;
			if(%delta > 0)
			{
				Client::sendMessage(%TrueClientId, $MSGTypeGame, "FLOOD! You cannot talk for " @ %delta @ " seconds.");
				return;
			}
			%TrueClientId.floodMute = "";
			%TrueClientId.muteDoneTime = "";
		}
		%TrueClientId.floodMessageCount++;
		// funky use of schedule here:
		schedule(%TrueClientId @ ".floodMessageCount--;", 5, %TrueClientId);
		if(%TrueClientId.floodMessageCount > 4)
		{
			%TrueClientId.floodMute = true;
			%TrueClientId.muteDoneTime = %time + 10;
			Client::sendMessage(%TrueClientId, $MSGTypeGame, "FLOOD! You cannot talk for 10 seconds.");
			return;
		}
	}

	//check for a bulknum-type of message
	if(%message == floor(%message))
	{
		if(%clientId.currentShop != "" || %clientId.currentBank != "" || %clientId.currentBeltBank != "" || %clientId.currentBeltSell != "")
		{
			if(%message < 1)
				%message = 1;
			// Belt selling allows up to 500, other menus allow up to 100
			if(%clientId.currentBeltSell != "")
			{
				if(%message > 500)
					%message = 500;
			}
			else
			{
				if(%message > 100)
					%message = 100;
			}
		}
		%TrueClientId.bulkNum = %message;
	}

	//parse message
	%botTalk = False;
	%isCommand = False;

	if(String::getSubStr(%message, 0, 1) != "#")
	{
		if(%team)
			%message = "#zone " @ %message;
		else
			%message = fetchData(%TrueClientId, "defaultTalk") @ " " @ %message;

	}
	if(String::getSubStr(%message, 0, 1) == "#")
		%isCommand = True;

	//echo("SAY: " @ %msg);

	if($exportChat)
	{
		%ip = Client::getTransportAddress(%TrueClientId);
		if(%TrueClientId.doExport)
		{
			$log::msg["[\"" @ %TCsenderName @ "\"]"] = %message;
			// BUGFIX: filename previously contained the literal text "$ @ " (a quoting
			// mistake), producing files named like "log$ @ Name.cs"
			export("log::msg[\"" @ %TCsenderName @ "\"*", "temp\\log_" @ %TCsenderName @ ".cs", true);
		}
	}

	%w1 = GetWord(%message, 0);

	//========== Redirect block commands into memory =============================================
	if(fetchData(%TrueClientId, "BlockInputFlag") != "" && String::ICompare(%w1, "#endblock") != 0 && %w1 != -1 && %message != "")
	{
		//Entering block information into memory
		%tmpBlockCnt = fetchData(%TrueClientId, "tmpBlockCnt") + 1;
		storeData(%TrueClientId, "tmpBlockCnt", %tmpBlockCnt);
		$BlockData[%TCsenderName, fetchData(%TrueClientId, "BlockInputFlag"), %tmpBlockCnt] = %message;
		return 0;
	}
	//============================================================================================

	%cropped = String::NEWgetSubStr(%message, (String::len(%w1)+1), 99999);

	if(%isCommand)
	{
		if(%w1 == "#say")
		{
			// Prevent enemy bots from using #say chat
			if(Player::isAiControlled(%TrueClientId) || isRPGAI(%TrueClientId))
			{
				%spawnBotInfo = fetchData(%TrueClientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					// Enemy bot trying to use #say - silently block
					return;
				}
			}
			
			if(SkillCanUse(%TrueClientId, "#say"))
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
				{
					%talkingPos = GameBase::getPosition(%TrueClientId);
					%receivingPos = GameBase::getPosition(%cl);
					%distVec = Vector::getDistance(%talkingPos, %receivingPos);
					if(%distVec <= $maxSAYdistVec)
					{
						//%newmsg = FadeMsg(%cropped, %distVec, $maxSAYdistVec);
						%newmsg = %cropped;
	
						if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId)
							Client::sendMessage(%cl, $MsgWhite, %TCsenderName @ " says, \"" @ %newmsg @ "\"");
					}
				}
				Client::sendMessage(%TrueClientId, $MsgWhite, "You say, \"" @ %cropped @ "\"");
				UseSkill(%TrueClientId, $SkillSpeech, True, True);
	
				%botTalk = True;
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
		}
	
		if(%w1 == "#shout")
		{
			// Prevent enemy bots from using #shout chat
			if(Player::isAiControlled(%TrueClientId) || isRPGAI(%TrueClientId))
			{
				%spawnBotInfo = fetchData(%TrueClientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					// Enemy bot trying to use #shout - silently block
					return;
				}
			}
			
			if(SkillCanUse(%TrueClientId, "#shout"))
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
				{
					%talkingPos = GameBase::getPosition(%TrueClientId);
					%receivingPos = GameBase::getPosition(%cl);
					%distVec = Vector::getDistance(%talkingPos, %receivingPos);
					if(%distVec <= $maxSHOUTdistVec)
					{
						//%newmsg = FadeMsg(%cropped, %distVec, $maxSHOUTdistVec);
						%newmsg = %cropped;
	
						if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId)
							Client::sendMessage(%cl, $MsgWhite, %TCsenderName @ " shouts, \"" @ %newmsg @ "\"");
					}
				}
				Client::sendMessage(%TrueClientId, $MsgWhite, "You shouted, \"" @ %cropped @ "\"");
				UseSkill(%TrueClientId, $SkillSpeech, True, True);
	
				%botTalk = True;
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
		}
		if(%w1 == "#whisper")
		{
			// Prevent enemy bots from using #whisper chat
			if(Player::isAiControlled(%TrueClientId) || isRPGAI(%TrueClientId))
			{
				%spawnBotInfo = fetchData(%TrueClientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					// Enemy bot trying to use #whisper - silently block
					return;
				}
			}
			
			if(SkillCanUse(%TrueClientId, "#whisper"))
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
				{
					%talkingPos = GameBase::getPosition(%TrueClientId);
					%receivingPos = GameBase::getPosition(%cl);
					%distVec = Vector::getDistance(%talkingPos, %receivingPos);
					if(%distVec <= $maxWHISPERdistVec)
					{
						//%newmsg = FadeMsg(%cropped, %distVec, $maxSHOUTdistVec);
						%newmsg = %cropped;
	
						if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId)
							Client::sendMessage(%cl, $MsgWhite, %TCsenderName @ " whispers, \"" @ %newmsg @ "\"");
					}
				}
				Client::sendMessage(%TrueClientId, $MsgWhite, "You whisper, \"" @ %cropped @ "\"");
				UseSkill(%TrueClientId, $SkillSpeech, True, True);
	
				%botTalk = True;
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
		}
	
		if(IsJailed(%TrueClientId))
			return;

		if(%w1 == "#bugreport")
		{
			if(%cropped == "")
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "Syntax: #bugreport <your bug description>");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Example: #bugreport My weapon disappeared after I died");
			}
			else
			{
				// Get timestamp
				%timestamp = getIntegerTime(true);
				%dateTime = "[" @ %timestamp @ "]";
				
				// Format bug report line: [timestamp] PlayerName: bug description
				%bugReportLine = %dateTime @ " " @ %TCsenderName @ ": " @ %cropped;
				
				// Store in a variable with unique timestamp index to ensure each entry is separate
				$BugReport::entry[%timestamp] = %bugReportLine;
				
				// Export to bugreports.txt file in temp folder (third parameter = true for append mode)
				// Using wildcard pattern to export all BugReport entries, which appends new entries
				export("$BugReport::entry*", "temp\\bugreports.txt", true);
				
				// Clear this specific entry after export (wildcard export already wrote it)
				$BugReport::entry[%timestamp] = "";
				
				// Send confirmation message
				Client::sendMessage(%TrueClientId, $MsgGreen, "Bug report submitted. Thank you!");
				echo("[BUGREPORT]: " @ %TCsenderName @ " submitted: " @ %cropped);
			}
			return;
		}
	
		if(%w1 == "#requestfeature" || %w1 == "#featurerequest")
		{
			if(%cropped == "")
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "Syntax: #requestfeature <your feature request>");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Example: #requestfeature Add a new weapon type for mages");
			}
			else
			{
				// Get timestamp
				%timestamp = getIntegerTime(true);
				%dateTime = "[" @ %timestamp @ "]";
				
				// Format feature request line: [timestamp] PlayerName: feature request
				%featureRequestLine = %dateTime @ " " @ %TCsenderName @ ": " @ %cropped;
				
				// Store in a variable with unique timestamp index to ensure each entry is separate
				$FeatureRequest::entry[%timestamp] = %featureRequestLine;
				
				// Export to featurerequests.txt file in temp folder (third parameter = true for append mode)
				// Using wildcard pattern to export all FeatureRequest entries, which appends new entries
				export("$FeatureRequest::entry*", "temp\\featurerequests.txt", true);
				
				// Clear this specific entry after export (wildcard export already wrote it)
				$FeatureRequest::entry[%timestamp] = "";
				
				// Send confirmation message
				Client::sendMessage(%TrueClientId, $MsgGreen, "Feature request submitted. Thank you!");
				echo("[FEATUREREQUEST]: " @ %TCsenderName @ " submitted: " @ %cropped);
			}
			return;
		}
	
		if(%w1 == "#daily")
		{
			// bots have no dailies
			if(Player::isAiControlled(%TrueClientId) || isRPGAI(%TrueClientId))
				return;
			if(%cropped == "abandon")
				Daily::Abandon(%TrueClientId);
			else if(%cropped == "summon")
				Daily::SummonElite(%TrueClientId);
			else
				Daily::Status(%TrueClientId);
			return;
		}

		if(%w1 == "#fontcolors" || %w1 == "#testcolors")
		{
			%colorMsg = "<f0>Font Color 0 (Default): This is the default text color\n";
			%colorMsg = %colorMsg @ "<f1>Font Color 1:\n";
			%colorMsg = %colorMsg @ "<f2>Font Color 2:\n";
			%colorMsg = %colorMsg @ "<f3>Font Color 3:\n";
			%colorMsg = %colorMsg @ "<f4>Font Color 4:\n";
			%colorMsg = %colorMsg @ "<f5>Font Color 5:\n";
			%colorMsg = %colorMsg @ "<f6>Font Color 6:\n";
			%colorMsg = %colorMsg @ "<f7>Font Color 7:\n";
			%colorMsg = %colorMsg @ "<f8>Font Color 8:\n\n";
			%colorMsg = %colorMsg @ "<f0>Note: Only 3 font colors";
			persistentCenterprint(%TrueClientId, %colorMsg, 10);
			return;
		}
	
		if(%w1 == "#viewdts")
		{
			// ADMIN GATE: dev/debug tool - spawns viewer shapes; was open to all players
			if(%clientToServerAdminLevel < 4)
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "You need admin level 4+ to use this command.");
				return;
			}
			if(%cropped == "")
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "Syntax: #viewdts <shape name>");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Example: #viewdts sword");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Use #listdts to see available shapes.");
			}
			else
			{
				DTSViewer::viewShape(%TrueClientId, %cropped);
			}
			return;
		}
	
		if(%w1 == "#listdts")
		{
			// ADMIN GATE: dev/debug tool
			if(%clientToServerAdminLevel < 4)
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "You need admin level 4+ to use this command.");
				return;
			}
			%page = GetWord(%cropped, 0);
			if(%page == "")
				%page = 1;
			DTSViewer::listShapes(%TrueClientId, %page);
			return;
		}
	
	if(%w1 == "#cleardts")
	{
		// ADMIN GATE: dev/debug tool
		if(%clientToServerAdminLevel < 4)
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "You need admin level 4+ to use this command.");
			return;
		}
		DTSViewer::clearViewer(%TrueClientId);
		Client::sendMessage(%TrueClientId, $MsgGreen, "Viewer object cleared.");
		return;
	}
	
	if(%w1 == "#weapondps" || %w1 == "#listweapondps")
	{
		// ADMIN GATE: dev/debug tool (dumps to server console)
		if(%clientToServerAdminLevel < 4)
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "You need admin level 4+ to use this command.");
			return;
		}
		ListAllWeaponDPS();
		Client::sendMessage(%TrueClientId, $MsgGreen, "Weapon DPS list printed to server console. Check server logs.");
		return;
	}
	
	// DUAL WIELDING - Requires Ascension DualWield talent (checked in DualWield::Command)
	if(%w1 == "#dualwield")
	{
		DualWield::Command(%TrueClientId, %cropped);
		return;
	}
	
	if(%w1 == "#searchdts")
		{
			// ADMIN GATE: dev/debug tool
			if(%clientToServerAdminLevel < 4)
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "You need admin level 4+ to use this command.");
				return;
			}
			if(%cropped == "")
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "Syntax: #searchdts <search term>");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Example: #searchdts hat");
			}
			else
			{
				DTSViewer::searchShapes(%TrueClientId, %cropped);
			}
			return;
		}
	
		if(%w1 == "#animations")
		{
			// ADMIN GATE: was open to ALL players - anyone could puppet a ~150s
			// animation sequence onto any clientId, INCLUDING other players (grief)
			if(%clientToServerAdminLevel < 4)
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "You need admin level 4+ to use this command.");
				return;
			}
			if(%cropped == "")
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "Syntax: #animations <clientid>");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Example: #animations 2050");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Plays animations 0-50 on the specified bot with 3 second delays.");
				Client::sendMessage(%TrueClientId, $MsgBeige, "Uses animData animation names (root, run, die chest, etc.)");
			}
			else
			{
				%clientId = GetWord(%cropped, 0);
				%clientId = floor(%clientId); // Ensure it's an integer
				
				// Validate client ID exists
				%playerObj = Client::getOwnedObject(%clientId);
				if(%playerObj == "" || %playerObj == -1)
				{
					Client::sendMessage(%TrueClientId, $MsgRed, "Error: Invalid client ID or bot does not exist.");
					return;
				}
				
				// Start animation sequence (starting at 0 to include "root" animation)
				PlayAnimationSequence(%clientId, 0, %TrueClientId);
				Client::sendMessage(%TrueClientId, $MsgGreen, "Starting animation sequence 0-50 on client " @ %clientId @ " (3 second intervals).");
			}
			return;
		}
	
		if(%w1 == "#tell")
		{
			if(SkillCanUse(%TrueClientId, "#tell"))
			{
				if(%cropped == "")
				{
					Client::sendMessage(%TrueClientId, 0, "syntax: #tell whoever, message");
				}
				else
				{
					%pos1 = 0;
					%pos2 = String::findSubStr(%cropped, ",");
					%name = String::getSubStr(%cropped, %pos1, %pos2-%pos1);
					%final = String::getSubStr(%cropped, %pos2 + 2, String::len(%cropped)-%pos2-2);
					%cl = NEWgetClientByName(%name);
		
					if(%cl != -1)
					{
						%n = Client::getName(%cl);	//capitalize the name properly
						if(!%cl.muted[%TrueClientId])
						{
							Client::sendMessage(%cl, $MsgRed, %TCsenderName @ " tells you, \"" @ %final @ "\"");
							if(%cl != %TrueClientId)
								Client::sendMessage(%TrueClientId, $MsgRed, "You tell " @ %n @ ", \"" @ %final @ "\"");
							%cl.replyTo = %TCsenderName;
	
							UseSkill(%TrueClientId, $SkillSpeech, True, True);
						}
						else
							Client::sendMessage(%TrueClientId, $MsgRed, %n @ " has muted you.");
					}
					else
						Client::sendMessage(%TrueClientId, $MsgWhite, "Invalid player name.");
				}
		
				%botTalk = True;
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
		}
		if(%w1 == "#r")
		{
			if(SkillCanUse(%TrueClientId, "#tell"))
			{
				if(%cropped == "")
					Client::sendMessage(%TrueClientId, 0, "syntax: #r message");
				else
				{
					%name = %TrueClientId.replyTo;
					if(%name != "")
					{
						%cl = NEWgetClientByName(%name);
			
						if(%cl != -1)
						{
							if(!%cl.muted[%TrueClientId])
							{
								Client::sendMessage(%cl, $MsgRed, %TCsenderName @ " tells you, \"" @ %cropped @ "\"");
								if(%cl != %TrueClientId)
									Client::sendMessage(%TrueClientId, $MsgRed, "You tell " @ %name @ ", \"" @ %cropped @ "\"");
								%cl.replyTo = %TCsenderName;
		
								UseSkill(%TrueClientId, $SkillSpeech, True, True);
							}
						}
						else
							Client::sendMessage(%TrueClientId, $MsgWhite, "Invalid player name.");
			
						%botTalk = True;
					}
					else
						Client::sendMessage(%TrueClientId, $MsgWhite, "You haven't received a #tell to reply to yet.");
				}
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
			return;
		}
		if(%w1 == "#global")
		{
			// Prevent bots (especially enemy bots) from using #global chat
			if(Player::isAiControlled(%TrueClientId) || isRPGAI(%TrueClientId))
			{
				// Bots should not be able to use global chat - silently return
				return;
			}
			
			if(SkillCanUse(%TrueClientId, "#global"))
			{
			if(!fetchData(%TrueClientId, "ignoreGlobal"))
				{
			            for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			                  if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId && !fetchData(%cl, "ignoreGlobal"))
			                        Client::sendMessage(%cl, $MsgGreen, "[GLBL] " @ %TCsenderName @ " \"" @ %cropped @ "\"");
			            Client::sendMessage(%TrueClientId, $MsgGreen, "[GLBL] \"" @ %cropped @ "\"");
					echo("[GLBL] " @ %TCsenderName @ " \"" @ %cropped @ "\"");

					UseSkill(%TrueClientId, $SkillSpeech, True, True);
				}
				else
			            Client::sendMessage(%TrueClientId, $MsgRed, "You can't send a Global message when ignoring other Global messages.");
		      }
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
			return;
		}
		if(%w1 == "#house")
		{
			if(SkillCanUse(%TrueClientId, "#house"))
			{
				if(fetchData(%TrueClientId, "MyHouse") != "")
				{
					for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
						if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId && fetchData(%TrueClientId, "MyHouse") == fetchData(%cl, "MyHouse"))
							Client::sendMessage(%cl, $MsgGreen, "[HOUSE] " @ %TCsenderName @ " \"" @ %cropped @ "\"");
					Client::sendMessage(%TrueClientId, $MsgGreen, "[HOUSE] \"" @ %cropped @ "\"");
					UseSkill(%TrueClientId, $SkillSpeech, True, True);
				}
				else
			            Client::sendMessage(%TrueClientId, $MsgRed, "You are not in a house.");
		      }
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
			return;
		}
	      if(%w1 == "#zone")
		{
			// Prevent enemy bots from using #zone chat
			if(Player::isAiControlled(%TrueClientId) || isRPGAI(%TrueClientId))
			{
				%spawnBotInfo = fetchData(%TrueClientId, "SpawnBotInfo");
				if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
				{
					// Enemy bot trying to use #zone - silently block
					return;
				}
			}
			
			if(SkillCanUse(%TrueClientId, "#zone"))
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId && fetchData(%cl, "zone") == fetchData(%TrueClientId, "zone"))
			      		Client::sendMessage(%cl, $MsgGreen, "[ZONE] " @ %TCsenderName @ " \"" @ %cropped @ "\"");
				Client::sendMessage(%TrueClientId, $MsgGreen, "[ZONE] \"" @ %cropped @ "\"");
				echo("[ZONE] " @ %TCsenderName @ " \"" @ %cropped @ "\"");

				UseSkill(%TrueClientId, $SkillSpeech, True, True);
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
			return;
	      }
		if(%w1 == "#group")
		{
			if(SkillCanUse(%TrueClientId, "#group"))
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
				{
					if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId && IsInCommaList(fetchData(%TrueClientId, "grouplist"), Client::getName(%cl)))
					{
						if(IsInCommaList(fetchData(%cl, "grouplist"), %TCsenderName))
							Client::sendMessage(%cl, $MsgBeige, "[GRP] " @ %TCsenderName @ " \"" @ %cropped @ "\"");
						else
							Client::sendMessage(%TrueClientId, $MsgRed, Client::getName(%cl) @ " does not have you on his/her group-list.");
					}
				}
	
				Client::sendMessage(%TrueClientId, $MsgBeige, "[GRP] \"" @ %cropped @ "\"");
				UseSkill(%TrueClientId, $SkillSpeech, True, True);
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
			return;
		}
		if(%w1 == "#party" || %w1 == "#p")
		{
			if(SkillCanUse(%TrueClientId, "#party"))
			{
				%list = GetPartyListIAmIn(%TrueClientId);
				for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
				{
					%cl = NEWgetClientByName(String::NEWgetSubStr(%list, 0, %p));
					if(!%cl.muted[%TrueClientId] && %cl != %TrueClientId)
						Client::sendMessage(%cl, $MsgBeige, "[PRTY] " @ %TCsenderName @ " \"" @ %cropped @ "\"");
				}
	
				Client::sendMessage(%TrueClientId, $MsgBeige, "[PRTY] \"" @ %cropped @ "\"");
				UseSkill(%TrueClientId, $SkillSpeech, True, True);
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You lack the necessary skills to use this command.");
				UseSkill(%TrueClientId, $SkillSpeech, False, True);
			}
			return;
		}

		if(IsDead(%TrueClientId) && %TrueClientId != 2048)
			return;

		//check for onHear events
		if(%botTalk)
		{
			%list = GetEveryoneIdList();
			for(%i = 0; GetWord(%list, %i) != -1; %i++)
			{
				%oid = GetWord(%list, %i);
	
				%time = getIntegerTime(true) >> 5;
				if(%time - fetchData(%oid, "nextOnHear") > 0.05)
				{
					storeData(%oid, "nextOnHear", %time);
	
					%oname = Client::getName(%oid);
	
					%index = GetEventCommandIndex(%oid, "onHear");
					if(%index != -1)
					{
						for(%i2 = 0; (%index2 = GetWord(%index, %i2)) != -1; %i2++)
						{
							%ec = $EventCommand[%oid, %index2];
		
							%hearName = GetWord(%ec, 2);
							%radius = GetWord(%ec, 3);
							if(Vector::getDistance(GameBase::getPosition(%oid), GameBase::getPosition(%TrueClientId)) <= %radius)
							{
								%targetname = GetWord(%ec, 5);
								if(String::ICompare(%targetname, "all") != 0)
									%targetId = NEWgetClientByName(%targetname);
		
								if(String::ICompare(%targetname, "all") == 0 || %targetId == %TrueClientId)
								{
									%sname = GetWord(%ec, 0);
									%type = GetWord(%ec, 1);
									%keep = GetWord(%ec, 4);
									%var = GetWord(%ec, 6);
									if(String::ICompare(%var, "var") == 0)
										%var = True;
									else
									{
										%div1 = String::findSubStr(%ec, "|");
										%div2 = String::ofindSubStr(%ec, "|", %div1+1);
										%text = String::NEWgetSubStr(%ec, %div1+1, %div2);
										%oec = String::NEWgetSubStr(%ec, %div1+%div2+2, 99999);
									}
		
									if(String::ICompare(%cropped, %text) == 0 || %var)
									{
										if((%cl = NEWgetClientByName(%sname)) == -1)
											%cl = 2048;

										%cmd = String::NEWgetSubStr($EventCommand[%oid, %index2], String::findSubStr($EventCommand[%oid, %index2], ">")+1, 99999);
										if(%var)
											%cmd = String::replace(%cmd, "^var", %cropped);
		
										%pcmd = ParseBlockData(%cmd, %TrueClientId, "");
										if(!%keep)
											$EventCommand[%oid, %index2] = "";
										remoteSay(%cl, 0, %pcmd, %sname);
									}
								}
							}
						}
					}
				}
			}
		}

		//=================================================
		// Beginning of commands
		// (player can't use any of these while dead)
		//=================================================

	      // TEMPORARILY DISABLED - #steal command
	      //if(%w1 == "#steal")
		//{
		//	%time = getIntegerTime(true) >> 5;
		//	if(%time - %TrueClientId.lastStealTime > $stealDelay)
		//	{
		//		%TrueClientId.lastStealTime = %time;
		//
		//		if((%reason = AllowedToSteal(%TrueClientId)) == "True")
		//		{
		//			if(SkillCanUse(%TrueClientId, "#steal"))
		//			{
		//				if(GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 1))
		//				{
		//					%id = Player::getClient($los::object);
		//					if(getObjectType($los::object) == "Player")
		//					{
		//						%victimName = Client::getName(%id);
		//						%stealerName = %TCsenderName;
		//						%victimCoins = fetchData(%id, "COINS");
		//						%fail = False;
		//						if(%victimCoins > 0)
		//						{
		//							%r1 = GetRoll("1d" @ ($PlayerSkill[%TrueClientId, $SkillStealing] * (2)));
		//							%r2 = GetRoll("1d" @ $PlayerSkill[%id, $SkillStealing]);
		//							%a = %r1 - %r2;
		//							if(%a > 0)
		//							{
		//								%amount = floor(%a * getRandom() * 1.2);
		//								if(%amount > %victimCoins)
		//									%amount = %victimCoins;
		//
		//								if(%amount > 0)
		//								{
		//									storeData(%TrueClientId, "COINS", %amount, "inc");
		//									storeData(%id, "COINS", %amount, "dec");
		//									PerhapsPlayStealSound(%TrueClientId, 0);
		//
		//			                              Client::sendMessage(%TrueClientId, $MsgTypeChat, "You successfully stole " @ %amount @ " coins from " @ %victimName @ "!");
		//		
		//				                                    RefreshAll(%TrueClientId);
		//				                                    RefreshAll(%id);
		//
		//									UseSkill(%TrueClientId, $SkillStealing, True, True);
		//									PostSteal(%TrueClientId, True, 0, %id);
		//				                              }
		//								else
		//									%fail = True;
		//							}
		//							else
		//								%fail = True;
		//
		//			                              if(%fail)
		//			                              {
		//			                                    Client::sendMessage(%TrueClientId, $MsgRed, "You failed to steal from " @ %victimName @ "!");
		//			                                    Client::sendMessage(%id, $MsgRed, %stealerName @ " just failed to steal from you!");
		//
		//									UseSkill(%TrueClientId, $SkillStealing, False, True);
		//									PostSteal(%TrueClientId, False, 0, %id);
		//								}
		//		                              }
		//							else
		//							{
		//			                                    Client::sendMessage(%TrueClientId, $MsgRed, %victimName @ " doesn't appear to be carrying any coins...");
		//							}
		//						}
		//					}
		//				}
		//				else
		//				{
		//					Client::sendMessage(%TrueClientId, $MsgWhite, "You can't steal because you lack the necessary skills.");
		//					UseSkill(%TrueClientId, $SkillStealing, False, True);
		//				}
		//			}
		//			else
		//				Client::sendMessage(%TrueClientId, $MsgRed, %reason);
		//		}
		//		return;
		//}
		if(%w1 == "#savecharacter")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                  {
	                        %r = SaveCharacter(%TrueClientId);
	                        Client::sendMessage(%TrueClientId, 0, "Saving self (" @ %TrueClientId @ "): success = " @ %r);
	                  }
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	                        if(%id)
	                        {
	                              %r = SaveCharacter(%id);
	                              Client::sendMessage(%TrueClientId, 0, "Saving " @ Client::getName(%id) @ " (" @ %id @ "): success = " @ %r);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
	            else
	            {
				%time = getIntegerTime(true) >> 5;
				if(%time - %TrueClientId.lastSaveCharTime > 10)
				{
					%TrueClientId.lastSaveCharTime = %time;
	
		                  %r = SaveCharacter(%TrueClientId);
					Client::sendMessage(%TrueClientId, 0, "Saving self (" @ %TrueClientId @ "): success = " @ %r);
				}
	            }
			return;
	      }
	if(%w1 == "#refreshmana")
	{
		if($Refresh == "0")
	{
	$Refresh = "1";
		setMANA(%TrueClientId, 9999);
		Client::sendMessage(%TrueClientId, 0, "Your MP has been restored to full."); //Time cap coded by sinite - Thx
		echo("[RefreshMana] " @ %TCsenderName @ " has used refresh mana");
		schedule("$Refresh = \"0\";",600);
	return;
	}
		else
	{
		Client::sendMessage(%TrueClientId, 0, "You cannot yet use Refresh Mana.");
	return;
	}
}
	      if(%w1 == "#whatismyclientid")
		{
	            Client::sendMessage(%TrueClientId, 0, "Your clientId is " @ %TrueClientId);
			return;
	      }
	      if(%w1 == "#whatismyplayerid")
		{
	            Client::sendMessage(%TrueClientId, 0, "Your playerId is " @ Client::getOwnedObject(%TrueClientId));
			return;
	      }
	      if(%w1 == "#dropcoins")
		{
	            %cropped = GetWord(%cropped, 0);
	
	            if(%cropped == "all")
	                  %cropped = fetchData(%TrueClientId, "COINS");
	            else
	                  %cropped = floor(%cropped);
	
	            if(fetchData(%TrueClientId, "COINS") >= %cropped)
	            {
	                  if(%cropped > 0)
	                  {
	                        //if( !(%clientToServerAdminLevel >= 4) )
						storeData(%TrueClientId, "COINS", %cropped, "dec");
	
					// Use higher velocity (10) for #dropcoins to drop coins further from player
					%toss = 10;
	
					TossLootbag(%TrueClientId, "COINS " @ %cropped, %toss, "*", 0);
					RefreshAll(%TrueClientId);
					SaveCharacter(%TrueClientId);
					// Queue deployable-only world save after 3 seconds (preserves lootbag crash safety, avoids full-save storms)
					RequestWorldSave("drop_coins", 3, "deployables");
	
	                        Client::sendMessage(%TrueClientId, 0, "You dropped " @ Number::Beautify(%cropped, -3) @ " coins.");
	                        playSound(SoundMoney1, GameBase::getPosition(%TrueClientId));
	                  }
	            }
	            else
	            {
	                  Client::sendMessage(%TrueClientId, 0, "You don't even have that many coins!");
	            }
			return;
	      }
	      if(%w1 == "#compass")
		{
	            if(%cropped == "")
	                  Client::sendMessage(%TrueClientId, 0, "Use #compass town or #compass dungeon. (Do not specify which, simply write town or dungeon)");
	            else
	            {
				if(SkillCanUse(%TrueClientId, "#compass"))
				{
					%mpos = GetNearestZone(%TrueClientId, %cropped, 4);
	
					if(%mpos != False)
					{
						%d = GetNESW(GameBase::getPosition(%TrueClientId), %mpos);
						UseSkill(%TrueClientId, $SkillSenseHeading, True, True);
	
						Client::sendMessage(%TrueClientId, 0, "The nearest " @ %cropped @ " is " @ %d @ " of here.");
					}
					else
						Client::sendMessage(%TrueClientId, 1, "Error finding a zone!");
				}
				else
				{
					Client::sendMessage(%TrueClientId, $MsgWhite, "You can't use your compass because you lack the necessary skills.");
					UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
				}
	            }
	     		return;
		}
	      if(%w1 == "#getinfo")
		{
	            %cropped = GetWord(%cropped, 0);
	
	            if(%cropped == "")
	                  Client::sendMessage(%TrueClientId, 0, "Please specify a name.");
	            else
	            {
				%id = NEWgetClientByName(%cropped);
				if(%id != -1)
					DisplayGetInfo(%TrueClientId, %id, Client::getOwnedObject(%id));
				else
					Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
			}
			return;
		}
	if(%w1 == "#getstats")
	{
		%cropped = GetWord(%cropped, 0);

		if(%cropped == "")
			Client::sendMessage(%TrueClientId, 0, "Please specify a name.");
		else
		{
			%id = NEWgetClientByName(%cropped);
			if(%id != -1)
				DisplayGetStats(%TrueClientId, %id, Client::getOwnedObject(%id));
			else
				Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
		}
		return;
	}
	if(%w1 == "#verify")
	{
		%now = getSimTime();
		if($AFKZoneWarnUntil[%TrueClientId] == "")
		{
			Client::sendMessage(%TrueClientId, $MsgBeige, "No overlevel warning is active.");
			return;
		}
		
		%providedCode = GetWord(%cropped, 0);
		%storedCode = $AFKZoneCode[%TrueClientId];
		
		if(%storedCode == "" || %storedCode == -1)
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "Error: No verification code found. Please move to reset.");
			return;
		}
		
		if(%providedCode == %storedCode)
		{
			$AFKZoneWarnAck[%TrueClientId] = %now;
			$AFKZoneWarnUntil[%TrueClientId] = ""; // Clear active warning immediately after valid verify
			$AFKZoneWarnPos[%TrueClientId] = "";
			$AFKZoneCode[%TrueClientId] = ""; // Clear code after successful verification

			// CRITICAL: Treat verification as valid activity!
			// Update the last position tracker to the current position to prevent the "lack of movement" check
			// from triggering the teleport in the next tick.
			%player = Client::getOwnedObject(%TrueClientId);
			if(isObject(%player))
			{
				$AFKZoneLastPos[%TrueClientId] = GameBase::getPosition(%player);
				$AFKZoneLastMove[%TrueClientId] = %now; // Also update last move time
			}
			
			Client::sendMessage(%TrueClientId, $MsgBeige, "Verification successful. Warning cleared.");
		}
		else
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "Incorrect verification code. Expected: " @ %storedCode @ ", Got: " @ %providedCode);
		}
		return;
	}
	if(%w1 == "#testexplosion" || %w1 == "#testbomb")
	{
		if(%TrueClientId.adminLevel < 1)
		{
			Client::sendMessage(%TrueClientId, 0, "You must be an admin to use this command.");
			return;
		}
		
		%bombType = GetWord(%cropped, 0);
		if(%bombType == "")
		{
			Client::sendMessage(%TrueClientId, 0, "Usage: #testexplosion [BombNumber] (e.g., #testexplosion Bomb4)");
			Client::sendMessage(%TrueClientId, 0, "Available: Bomb1-29 (Bomb4, Bomb13, Bomb15-29 are unused by spells)");
			return;
		}
		
		// Validate bomb type exists
		if(%bombType != "Bomb1" && %bombType != "Bomb2" && %bombType != "Bomb3" && %bombType != "Bomb4" && 
		   %bombType != "Bomb5" && %bombType != "Bomb6" && %bombType != "Bomb7" && %bombType != "Bomb8" && 
		   %bombType != "Bomb9" && %bombType != "Bomb10" && %bombType != "Bomb11" && %bombType != "Bomb12" && 
		   %bombType != "Bomb13" && %bombType != "Bomb14" && %bombType != "Bomb15" && %bombType != "Bomb16" && 
		   %bombType != "Bomb17" && %bombType != "Bomb18" && %bombType != "Bomb19" && %bombType != "Bomb20" && 
		   %bombType != "Bomb21" && %bombType != "Bomb22" && %bombType != "Bomb23" && %bombType != "Bomb24" && 
		   %bombType != "Bomb25" && %bombType != "Bomb26" && %bombType != "Bomb27" && %bombType != "Bomb28" && %bombType != "Bomb29")
		{
			Client::sendMessage(%TrueClientId, 0, "Invalid bomb type. Use Bomb1 through Bomb29.");
			return;
		}
		
		%playerPos = GameBase::getPosition(%TrueClientId);
		CreateAndDetBomb(%TrueClientId, %bombType, %playerPos, False, 0);
		Client::sendMessage(%TrueClientId, 0, "Testing explosion: " @ %bombType);
		return;
	}
	      if(%w1 == "#setinfo")
		{
	            if(%cropped == "")
	                  Client::sendMessage(%TrueClientId, 0, "Please specify text.");
	            else
	            {
				storeData(%TrueClientId, "PlayerInfo", %cropped);
	                  Client::sendMessage(%TrueClientId, 0, "Info set.  Use #getinfo [name] to retrieve this type of information.");
			}
			return;
		}
	      if(%w1 == "#addinfo")
		{
	            if(%cropped == "")
	                  Client::sendMessage(%TrueClientId, 0, "Please specify text.");
	            else
	            {
				storeData(%TrueClientId, "PlayerInfo", %cropped, "strinc");
	                  Client::sendMessage(%TrueClientId, 0, "Info added to the end of previous info.");
			}
			return;
		}
	      if(%w1 == "#w")
		{
	            %item = getCroppedItem(%cropped);
	
	            if(%item == "")
	                  Client::sendMessage(%TrueClientId, 0, "Please specify an item (ex: Black Statue = BlackStatue).");
	            else
	            {
			%msg = WhatIs(%item);
			// Use persistentCenterprint to prevent message from being overwritten by bottomprint (e.g., during combat)
			persistentCenterprint(%TrueClientId, %msg, 5);
			}
			return;
		}
	      if(%w1 == "#spell" || %w1 == "#cast") 
     		{ 
          	%flag = true; 
          	for(%i = 1; getWord(%cropped, %i) != -1; %i++) 
          	{ 
               if(FindInvalidChar(getWord(%cropped, %i))) 
                    {%flag = false; break;} 
         	} 
          	if(!%flag) 
          	{ 
               Client::sendMessage(%clientId, $MsgRed, %cropped @ " is an invalid spell."); 
               return; 
          } 
          else 
          { 
               if(fetchData(%TrueClientId, "SpellCastStep") == 1)
				Client::sendMessage(%TrueClientId, 0, "You are already casting a spell!");
			else if(fetchData(%TrueClientId, "SpellCastStep") == 2)
				Client::sendMessage(%TrueClientId, 0, "You are still recovering from your last spell cast.");
			else if(%TrueClientId.sleepMode != "" && %TrueClientId.sleepMode != False)
				Client::sendMessage(%TrueClientId, $MsgRed, "You can not cast a spell while sleeping or meditating.");
			else if(IsDead(%TrueClientId))
				Client::sendMessage(%TrueClientId, $MsgRed, "You can not cast a spell when dead.");
			else
               { 
                    if(%cropped == "") 
                         Client::sendMessage(%clientId, 0, "Specify a spell."); 
                    else 
                         BeginCastSpell(%clientId, %cropped,1); 
               } 
          }
	return; 
     }
		if(%w1 == "#recall")
		{
			%zvel = floor(getWord(Item::getVelocity(%TrueClientId), 2));
			Client::sendMessage(%TrueClientId, $MsgRed, "ATTEMPTING RECALL");
			if(%zvel <= -350 || %zvel >= 350)
			{
				FellOffMap(%TrueClientId);
				CheckAndBootFromArena(%TrueClientId);
	
				%zv = "PASS";
			}
			else
				%zv = "FAIL";
			
			Client::sendMessage(%TrueClientId, $MsgBeige, "Z-Velocity check: " @ %zv);
	
			if(%zv != "PASS" && !fetchData(%TrueClientId, "tmprecall"))
			{
				%seconds = $recallDelay;
				storeData(%TrueClientId, "tmprecall", True);
				Client::sendMessage(%TrueClientId, $MsgBeige, "Stay at your current position for the next " @ %seconds @ " seconds to recall.");
	
				schedule("storeData(" @ %TrueClientId @ ", \"tmprecall\", \"\");if(Vector::getDistance(\"" @ GameBase::getPosition(%TrueClientId) @ "\", GameBase::getPosition(" @ %TrueClientId @ ")) <= 1){FellOffMap(" @ %TrueClientId @ ");CheckAndBootFromArena(" @ %TrueClientId @ ");}", %seconds);
			}
			return;
		}
	      if(%w1 == "#track")
		{
	            %cropped = GetWord(%cropped, 0);
	
	            if(%cropped == "")
	                  Client::sendMessage(%TrueClientId, 0, "Please specify a name.");
	            else
	            {
				if(SkillCanUse(%TrueClientId, "#track"))
				{
					%id = NEWgetClientByName(%cropped);
					%cropped = Client::getName(%id);
					if(%id != -1)
					{
						%clientIdpos = GameBase::getPosition(%TrueClientId);
						%idpos = fetchData(%id, "lastScent");

						if(%idpos != "")
						{
							%dist = round(Vector::getDistance(%clientIdpos, %idpos));
	
							if(Cap($PlayerSkill[%TrueClientId, $SkillSenseHeading] * 7.5, 100, "inf") >= %dist)
							{
								%d = GetNESW(%clientIdpos, %idpos);
								Client::sendMessage(%TrueClientId, $MsgWhite, "You sense that " @ %cropped @ " is " @ %d @ " of here, " @ %dist @ " meters away.");
								UseSkill(%TrueClientId, $SkillSenseHeading, True, True);
							}
							else
							{
								Client::sendMessage(%TrueClientId, $MsgWhite, "You have no idea where " @ %cropped @ " could be.");
								UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
							}
						}
						else
						{
							Client::sendMessage(%TrueClientId, $MsgWhite, "You have no idea where " @ %cropped @ " could be.");
							UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
				{
					Client::sendMessage(%TrueClientId, $MsgWhite, "You can't track because you lack the necessary skills.");
					UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
				}
			}
			return;
		}
	      if(%w1 == "#trackpack")
		{
	            %cropped = GetWord(%cropped, 0);
	
	            if(%cropped == "")
	                  Client::sendMessage(%TrueClientId, 0, "Please specify a name.");
	            else
	            {
				if(SkillCanUse(%TrueClientId, "#trackpack"))
				{
					%id = NEWgetClientByName(%cropped);
					if(%id != -1)
					{
						%cropped = Client::getName(%id);	//properly capitalize name
	
						%closest = 5000000;
						%closestId = -1;
						%clientIdpos = GameBase::getPosition(%TrueClientId);
						%list = fetchData(%id, "lootbaglist");
						for(%i = String::findSubStr(%list, ","); String::findSubStr(%list, ",") != -1; %list = String::NEWgetSubStr(%list, %i+1, 99999))
						{
							%id = String::NEWgetSubStr(%list, 0, %i);
							%idpos = GameBase::getPosition(%id);
							%dist = round(Vector::getDistance(%clientIdpos, %idpos));
							if(%dist < %closest)
							{
								%closest = %dist;
								%closestId = %id;
							}
						}
						if(%closestId != -1)
						{
							%idpos = GameBase::getPosition(%closestId);
	
							if(Cap($PlayerSkill[%TrueClientId, $SkillSenseHeading] * 15, 100, "inf") >= %closest)
							{
								%d = GetNESW(%clientIdpos, %idpos);
								Client::sendMessage(%TrueClientId, $MsgWhite, %cropped @ "'s nearest backpack is " @ %d @ " of here, " @ %closest @ " meters away.");
								UseSkill(%TrueClientId, $SkillSenseHeading, True, True);
							}
							else
							{
								Client::sendMessage(%TrueClientId, $MsgWhite, %cropped @ "'s nearest backpack is too far from you to track with your current sense heading skills.");
								UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
							}
						}
						else
						{
							Client::sendMessage(%TrueClientId, $MsgWhite, %cropped @ " doesn't have any dropped backpacks.");
							UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
				{
					Client::sendMessage(%TrueClientId, $MsgWhite, "You can't track a backpack because you lack the necessary skills.");
					UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
				}
			}
			return;
		}
		if(%w1 == "#sharepack")
		{
			%time = getIntegerTime(true) >> 5;
			if(%time - %TrueClientId.lastSharePackTime > 5)
			{
				%TrueClientId.lastSharePackTime = %time;
	
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
				if(%c1 != -1 && %c2 != -1)
				{
					%id = NEWgetClientByName(%c1);
		
					if(%id != -1 && Client::getName(%id) != %senderName)
					{
						%c1 = Client::getName(%id);	//properly capitalize name
						if(floor(%c2) != 0 || %c2 == "*")
						{
							%flag = "";
							%cnt = 0;
							%list = fetchData(%TrueClientId, "lootbaglist");
							for(%i = String::findSubStr(%list, ","); String::findSubStr(%list, ",") != -1; %list = String::NEWgetSubStr(%list, %i+1, 99999))
							{
								%cnt++;
								%bid = String::NEWgetSubStr(%list, 0, %i);
		
								if(%cnt == %c2 || %c2 == "*")
								{
									%flag++;
		
									%nl = GetWord($loot[%bid], 1);
									if(%nl != "*")
									{
										$loot[%bid] = String::Replace($loot[%bid], %nl, AddToCommaList(%nl, %c1));
										Client::sendMessage(%TrueClientId, $MsgBeige, "Adding " @ %c1 @ " to backpack #" @ %cnt @ " (" @ %bid @ ")'s share list.");
										Client::sendMessage(%id, $MsgBeige, %TCsenderName @ " is sharing his/her backpack #" @ %cnt @ " with you.");
									}
									else
										Client::sendMessage(%TrueClientId, 0, "Backpack #" @ %cnt @ " is already publicly available.");
								}
							}
							
							if(%flag == "")
								Client::sendMessage(%TrueClientId, 0, "Invalid backpack number.");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Please specify a backpack number (1, 2, 3, etc, or * for all)");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name or same player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
	      }
		if(%w1 == "#unsharepack")
		{
			%c1 = GetWord(%cropped, 0);
			%c2 = GetWord(%cropped, 1);
	
			if(%c1 != -1 && %c2 != -1)
			{
				%id = NEWgetClientByName(%c1);
	
				if(%id != -1 && Client::getName(%id) != %senderName)
				{
					%c1 = Client::getName(%id);	//properly capitalize name
					if(floor(%c2) != 0 || %c2 == "*")
					{
						%flag = "";
						%cnt = 0;
						%list = fetchData(%TrueClientId, "lootbaglist");
						for(%i = String::findSubStr(%list, ","); String::findSubStr(%list, ",") != -1; %list = String::NEWgetSubStr(%list, %i+1, 99999))
						{
							%cnt++;
							%bid = String::NEWgetSubStr(%list, 0, %i);
	
							if(%cnt == %c2 || %c2 == "*")
							{
								%flag++;
	
								%nl = GetWord($loot[%bid], 1);
								if(%nl != "*")
								{
									$loot[%bid] = String::Replace($loot[%bid], %nl, RemoveFromCommaList(%nl, %c1));
									Client::sendMessage(%TrueClientId, $MsgBeige, "Removing " @ %c1 @ " from backpack #" @ %cnt @ " (" @ %bid @ ")'s share list.");
									Client::sendMessage(%id, $MsgBeige, %TCsenderName @ " has removed you from his/her backpack #" @ %cnt @ " share list.");
								}
								else
									Client::sendMessage(%TrueClientId, 0, "Backpack #" @ %cnt @ " is already publicly available.  Its share list can not be changed.");
							}
						}
						
						if(%flag == "")
							Client::sendMessage(%TrueClientId, 0, "Invalid backpack number.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Please specify a backpack number (1, 2, 3, etc, or * for all)");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Invalid player name or same player name.");
			}
			else
				Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	
			return;
	      }
	      if(%w1 == "#packsummary")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				if(%cropped != "")
				{
					%id = NEWgetClientByName(%cropped);
					%cropped = Client::getName(%id);	//properly capitalize name
	
					%cnt = floor(CountObjInCommaList(fetchData(%id, "lootbaglist")));
					Client::sendMessage(%TrueClientId, 0, %cropped @ " has " @ %cnt @ " dropped backpacks.");
				}
				else
				{
					%list = GetPlayerIdList();
					for(%i = 0; (%id = GetWord(%list, %i)) != -1; %i++)
					{
						%cnt = CountObjInCommaList(fetchData(%id, "lootbaglist"));
						if(%cnt > 0)
							Client::sendMessage(%TrueClientId, 0, Client::getName(%id) @ " has " @ %cnt @ " dropped backpacks.");
					}
				}
			}
			else if(%cropped == "")
			{
				%cnt = floor(CountObjInCommaList(fetchData(%TrueClientId, "lootbaglist")));
				Client::sendMessage(%TrueClientId, 0, "You have a total of " @ %cnt @ " currently dropped backpacks.");
			}
			return;
		}
			
	      if(%w1 == "#mypassword")
		{
		      %c1 = GetWord(%cropped, 0);
	
	            if(%c1 != -1)
			{
				storeData(%TrueClientId, "password", %c1);
				Client::sendMessage(%TrueClientId, 0, "Changed personal password to " @ fetchData(%TrueClientId, "password") @ ".");
			}
			else
				Client::sendMessage(%TrueClientId, 0, "Please specify a one-word password.");
	
			return;
	      }
		if(%w1 == "#sleep")
		{
			if(fetchData(%TrueClientId, "InSleepZone") != "" && %TrueClientId.sleepMode == "" && !IsDead(%TrueClientId))
				%flag = True;
			if(String::ICompare(fetchData(%TrueClientId, "CLASS"), "Ranger") == 0 && fetchData(%TrueClientId, "zone") == "" && %TrueClientId.sleepMode == "" && !IsDead(%TrueClientId))
				%flag = True;
			
			if(%flag)
			{
				%TrueClientId.sleepMode = 1;
				Client::setControlObject(%TrueClientId, Client::getObserverCamera(%TrueClientId));
				Observer::setOrbitObject(%TrueClientId, Client::getOwnedObject(%TrueClientId), 30, 30, 30);
				refreshHPREGEN(%TrueClientId);
				refreshMANAREGEN(%TrueClientId);
	
				Client::sendMessage(%TrueClientId, $MsgWhite, "You fall asleep...  Use #wake to wake up.");
			}
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "You can't seem to fall asleep here.");
	
			return;
		}
		if(%w1 == "#meditate")
		{
			if(%TrueClientId.sleepMode == "" && !IsDead(%TrueClientId) && $possessedBy[%TrueClientId].possessId != %TrueClientId)
			{
				%TrueClientId.sleepMode = 2;
				Client::setControlObject(%TrueClientId, Client::getObserverCamera(%TrueClientId));
				Observer::setOrbitObject(%TrueClientId, Client::getOwnedObject(%TrueClientId), 30, 30, 30);
				refreshHPREGEN(%TrueClientId);
				refreshMANAREGEN(%TrueClientId);
	
				Client::sendMessage(%TrueClientId, $MsgWhite, "You begin to meditate.  Use #wake to stop meditating.");
			}
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "You can't seem to meditate.");
	
			return;
		}
		if(%w1 == "#wake")
		{
			if(%TrueClientId.sleepMode != "")
			{
				%TrueClientId.sleepMode = "";
				Client::setControlObject(%TrueClientId, %TrueClientId);
				refreshHPREGEN(%TrueClientId);
				refreshMANAREGEN(%TrueClientId);
	
				Client::sendMessage(%TrueClientId, $MsgWhite, "You awake.");
			}
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "You are not sleeping or meditating.");
	
			return;
		}
	      if(%w1 == "#roll")
		{
		      %c1 = GetWord(%cropped, 0);
	
	            if(%c1 != -1)
				Client::sendMessage(%TrueClientId, 0, %c1 @ ": " @ GetRoll(%c1));
			else
				Client::sendMessage(%TrueClientId, 0, "Please specify a roll (example: 1d6)");
	
			return;
	      }
		if(%w1 == "#hide")
		{
			// CRITICAL: Bots should never be able to use #hide (player-only ability)
			if(Player::isAiControlled(%TrueClientId))
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "Bots cannot use Hide In Shadows.");
				return;
			}
			
			if(SkillCanUse(%TrueClientId, "#hide"))
			{
				if(!fetchData(%TrueClientId, "invisible") && !fetchData(%TrueClientId, "blockHide"))
				{
					%closeEnoughToWall = Cap($PlayerSkill[%TrueClientId, $SkillHiding] / 125, 3.5, 8);
	
					%pos = GameBase::getPosition(%TrueClientId);
	
					%closest = 10000;
					for(%i = 0; %i <= 6.283; %i+= 0.52)
					{
						GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 25, "0 0 " @ %i);
						%dist = Vector::getDistance(%pos, $los::position);
						if(%dist < %closest && $los::position != "0 0 0" && $los::position != "")
							%closest = %dist;
					}
	
					if(%closest <= %closeEnoughToWall)
					{
						Client::sendMessage(%TrueClientId, $MsgBeige, "You are successful at Hide In Shadows.");
	
						GameBase::startFadeOut(%TrueClientId);
						storeData(%TrueClientId, "invisible", True);
	
						%grace = Cap($PlayerSkill[%TrueClientId, $SkillHiding] / 10, 5, 100);
						WalkSlowInvisLoop(%TrueClientId, 5, %grace);
	
						UseSkill(%TrueClientId, $SkillHiding, True, True);
					}
					else
					{
						Client::sendMessage(%TrueClientId, $MsgWhite, "You were unsuccessful at Hide In Shadows.");
						UseSkill(%TrueClientId, $SkillHiding, False, True);
					}
				}
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You can't hide because you lack the necessary skills.");
				UseSkill(%TrueClientId, $SkillHiding, False, True);
			}
			return;
		}
		if(%message == "#powershove") //scripted by Sinite
		{

		if($PlayerSkill[%TrueClientId, $SkillBashing] > 499)
		{
		//%sid = $SkillDesc[$SkillBashing];
		echo("Skill on bashing is " @$PlayerSkill[%TrueClientId, $SkillBashing]@ "");
		 %player = Client::getOwnedObject(%TrueClientId);
			GameBase::getLOSinfo(%player, 50000);
			%lospos = $los::position;
			%losobj = getObjectType($los::object);
			// CRITICAL: Use GetClientIdFromPlayerObject() for proper client ID lookup
			// Player::getClient() returns -1 for AI bots, so we need reverse lookup
			%victimid = GetClientIdFromPlayerObject($los::object);
		if(%losobj == "Player")
		{		
			// Check if target is a town bot (town bots should not be affected by shove)
			// CRITICAL: Use isTownBot() helper function which properly distinguishes town bots from enemy bots
			// Town bots have BotInfoAiName but NO SpawnBotInfo
			// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
			%isTownBot = isTownBot(%victimid);
			
			if(%victimid != -1 && !%isTownBot && $Canwhack == 0)
			{
				$Canwhack = 1;
				client::sendmessage(%TrueClientId,$MsgBeige,"Whacking " @Client::GetName(%victimid)@ "");
				GameBase::SetPosition(%TrueClientId,GameBase::GetPosition(%victimid));
				%b = GameBase::getRotation(%TrueClientId);
				%c1 = Cap(22000, 0, 250);
				//%c2 = %c1 / 4;
				%c2 = %c1;
				%mom = Vector::getFromRot( %b, %c1+500, %c2);
				Player::applyImpulse(%victimid, %mom);
				Player::SetRotation(%victimid,"{90,90,90}");
				schedule("$Canwhack = 0;",30);
			}
			else if(%isTownBot)
			{
				client::sendmessage(%TrueClientId,$MsgBeige,"You cannot whack town bots!");
			}
			else
				client::sendmessage(%TrueClientId,$MsgBeige,"You fail to whack! (You must wait 30 seconds between whacks)");
	}
}
	else
client::sendmessage(%TrueClientId,$MsgBeige,"You fail to whack! (You must have 500 on bashing to whack.)");
}
		if(%w1 == "#bash")
		{
			if(!fetchData(%TrueClientId, "blockBash"))
			{
				if(SkillCanUse(%TrueClientId, "#bash"))
				{
					Client::sendMessage(%TrueClientId, $MsgBeige, "You are ready to bash!");
					storeData(%TrueClientId, "NextHitBash", True);
					storeData(%TrueClientId, "blockBash", True);
				}
				else
				{
					Client::sendMessage(%TrueClientId, $MsgWhite, "You can't bash because you lack the necessary skills.");
					UseSkill(%TrueClientId, $SkillBashing, False, True);
				}
			}
			return;
		}
		if(%w1 == "#shove")
		{
			%time = getIntegerTime(true) >> 5;
			if(%time - %TrueClientId.lastShoveTime > 1.5)
			{
				%TrueClientId.lastShoveTime = %time;
	
				if(SkillCanUse(%TrueClientId, "#shove"))
				{
					%player = Client::getOwnedObject(%TrueClientId);
					if(GameBase::getLOSinfo(%player, 2))
					{
						%targetPlayerObj = $los::object;
						// CRITICAL: Use GetClientIdFromPlayerObject() for proper client ID lookup
						// Player::getClient() returns -1 for AI bots, so we need reverse lookup
						%id = GetClientIdFromPlayerObject(%targetPlayerObj);
		
						// Check if target is a town bot (town bots should not be affected by shove)
						// CRITICAL: Use isTownBot() helper function which properly distinguishes town bots from enemy bots
						// Town bots have BotInfoAiName but NO SpawnBotInfo
						// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
						%isTownBot = isTownBot(%id);
						
						if(%id != -1 && !(Player::isAiControlled(%id) && GameBase::getTeam(%id) == GameBase::getTeam(%TrueClientId)) && !%isTownBot)
						{
							if(%TrueClientId.adminLevel > %id.adminLevel || %id.adminLevel < 1)
							{
								%b = GameBase::getRotation(%TrueClientId);
								%c1 = Cap(20 + fetchData(%TrueClientId, "LVL"), 0, 250);
								%c2 = %c1 / 4;
								%mom = Vector::getFromRot( %b, %c1, %c2 );
		
								// Check if target is an enemy bot (Player object) - use Player object directly
								%isEnemyBot = IsEnemyBot(%id);
								if(%isEnemyBot)
								{
									// For enemy bots, temporarily remove AI directives to allow shove to work
									%botName = fetchData(%id, "BotInfoAiName");
									if(%botName != "" && %botName != -1 && %botName != "0")
									{
										// Remove active directive (99 is the main movement directive)
										AI::newDirectiveRemove(%botName, 99);
										// Set a flag to prevent AI from immediately re-adding the directive
										storeData(%id, "ShovedByPlayer", getSimTime());
										// PHASE 4 FIX: Also set BotFrozen flag to completely halt AI processing
										$BotFrozen[%id] = "true";
										// Clear all directives to prevent movement during shove
										$aidirectiveTable[%id, 99] = "";
										// CRITICAL: Disable AI seeking to allow physics impulse to work
										AI::setVar(%botName, seekOff, 1);
										// Re-enable AI movement after 1.5 seconds (extended from 0.5s)
										schedule("storeData(" @ %id @ ", \"ShovedByPlayer\", \"\"); $BotFrozen[" @ %id @ "] = \"\"; AI::setVar(\"" @ %botName @ "\", seekOff, 0);", 1.5);
									}
									// For enemy bots, use Player object directly with applyImpulse
									Player::applyImpulse(%targetPlayerObj, %mom);
								}
								else
								{
									// For players, use standard applyImpulse with client ID
									Player::applyImpulse(%id, %mom);
								}
								
								// Interrupt spell casting if target is currently casting
								if(fetchData(%id, "SpellCastStep") == 1)
								{
									storeData(%id, "SpellCastStep", "");
									ClearEvents(%id);
									Client::sendMessage(%id, $MsgRed, "Your spell casting was interrupted!");
									if(%id.hasKronosHUD)
										remoteEval(%id, "KronosCastStop");
								}
							}
						}
					}
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "You can't shove because you lack the necessary skills.");
			}
			return;
		}
	      if(%w1 == "#defaulttalk")
		{
	            if(%cropped != "")
			{
				// Prevent bots from setting defaultTalk to #global (security fix)
				if((Player::isAiControlled(%TrueClientId) || isRPGAI(%TrueClientId)) && %cropped == "#global")
				{
					Client::sendMessage(%TrueClientId, $MsgRed, "Bots cannot use #global chat.");
					return;
				}
				storeData(%TrueClientId, "defaultTalk", %cropped);
				Client::sendMessage(%TrueClientId, 0, "Changed Default Talk to " @ fetchData(%TrueClientId, "defaultTalk") @ ".");
			}
			else
				Client::sendMessage(%TrueClientId, 0, "Please specify what will be added to the beginning of each of your messages.");
	
			return;
	      }
	      if(%w1 == "#zonelist")
		{
			if(SkillCanUse(%TrueClientId, "#zonelist"))
			{
			      %c1 = GetWord(%cropped, 0);
	
		            if(%c1 != -1)
				{
					if(String::ICompare(%c1, "all") == 0)
						%t = 1;
					else if(String::ICompare(%c1, "players") == 0)
						%t = 2;
					else if(String::ICompare(%c1, "enemies") == 0)
						%t = 3;
	
					%list = Zone::getPlayerList(fetchData(%TrueClientId, "zone"), %t);
	
					if(%list != "")
					{
						for(%i = 0; (%id = GetWord(%list, %i)) != -1; %i++)
							Client::sendMessage(%TrueClientId, $MsgBeige, Client::getName(%id));
					}
					else
						Client::sendMessage(%TrueClientId, $MsgRed, "[none]");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify 'players', 'enemies', or 'all'");
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgWhite, "You can't zonelist because you lack the necessary skills.");
				UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
			}
			return;
		}
	      // TEMPORARILY DISABLED - #pickpocket command
	      //if(%w1 == "#pickpocket")
		//{
		//	%time = getIntegerTime(true) >> 5;
		//	if(%time - %TrueClientId.lastStealTime > $stealDelay)
		//	{
		//		%TrueClientId.lastStealTime = %time;
		//
		//		if((%reason = AllowedToSteal(%TrueClientId)) == "True")
		//		{
		//			if(SkillCanUse(%TrueClientId, "#pickpocket"))
		//			{
		//				if(GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 1))
		//				{
		//					%id = Player::getClient($los::object);
		//					if(getObjectType($los::object) == "Player" && !Player::isAiControlled(%id))
		//					{
		//						%TrueClientId.stealType = 1;
		//						SetupInvSteal(%TrueClientId, %id);
		//					}
		//				}
		//			}
		//			else
		//			{
		//				Client::sendMessage(%TrueClientId, $MsgWhite, "You can't pickpocket because you lack the necessary skills.");
		//				UseSkill(%TrueClientId, $SkillStealing, False, True);
		//			}
		//		}
		//		else
		//			Client::sendMessage(%TrueClientId, $MsgRed, %reason);
		//	}
		//	return;
		//}
	      // TEMPORARILY DISABLED - #mug command
	      //if(%w1 == "#mug")
		//{
		//	%time = getIntegerTime(true) >> 5;
		//	if(%time - %TrueClientId.lastStealTime > $stealDelay)
		//	{
		//		%TrueClientId.lastStealTime = %time;
		//
		//		if((%reason = AllowedToSteal(%TrueClientId)) == "True")
		//		{
		//			if(SkillCanUse(%TrueClientId, "#mug"))
		//			{
		//				if(GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 1))
		//				{
		//					%id = Player::getClient($los::object);
		//					if(getObjectType($los::object) == "Player" && !Player::isAiControlled(%id))
		//					{
		//						%TrueClientId.stealType = 2;
		//						SetupInvSteal(%TrueClientId, %id);
		//					}
		//				}
		//			}
		//			else
		//			{
		//				Client::sendMessage(%TrueClientId, $MsgWhite, "You can't mug because you lack the necessary skills.");
		//				UseSkill(%TrueClientId, $SkillStealing, False, True);
		//			}
		//		}
		//		else
		//			Client::sendMessage(%TrueClientId, $MsgRed, %reason);
		//	}
		//	return;
		//}
	      if(%w1 == "#createpack")
		{
			if(fetchData(%TrueClientId, "TempPack") != "")
			{
				if(HasThisStuff(%TrueClientId, fetchData(%TrueClientId, "TempPack")))
				{
					TakeThisStuff(%TrueClientId, fetchData(%TrueClientId, "TempPack"));
					%namelist = %TCsenderName @ ",";
					TossLootbag(%TrueClientId, fetchData(%TrueClientId, "TempPack"), 5, %namelist, 0);
					RefreshAll(%TrueClientId);
					SaveCharacter(%TrueClientId);
					// Queue deployable-only world save after 3 seconds (preserves lootbag crash safety, avoids full-save storms)
					RequestWorldSave("create_pack", 3, "deployables");
	
					remotePlayMode(%TrueClientId);
				}
			}
			return;
		}
		if(%w1 == "#camp")
		{
			if(Player::getItemCount(%TrueClientId, Tent))
			{
				%camp = nameToId("MissionCleanup\\Camp" @ %TrueClientId);
				if(%camp == -1)
				{
					if(fetchData(%TrueClientId, "zone") == "")
					{
						Client::sendMessage(%TrueClientId, $MsgBeige, "Setting up camp...");
			
						%pos = GameBase::getPosition(%TrueClientId);
			
						Player::decItemCount(%TrueClientId, Tent);
						RefreshAll(%TrueClientId);
						%group = newObject("Camp" @ %TrueClientId, SimGroup);
						addToSet("MissionCleanup", %group);
		
						schedule("DoCampSetup(" @ %TrueClientId @ ", 1, \"" @ %pos @ "\");", 2, %group);
						schedule("DoCampSetup(" @ %TrueClientId @ ", 2, \"" @ %pos @ "\");", 10, %group);
						schedule("DoCampSetup(" @ %TrueClientId @ ", 3, \"" @ %pos @ "\");", 17, %group);
						schedule("DoCampSetup(" @ %TrueClientId @ ", 4, \"" @ %pos @ "\");", 20, %group);
					}
					else
						Client::sendMessage(%TrueClientId, $MsgRed, "You can't set up a camp here.");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgRed, "You already have a camp setup somewhere.");
			}
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "You aren't carrying a tent.");
	
			return;
		}
		if(%w1 == "#uncamp")
		{
			%camp = nameToId("MissionCleanup\\Camp" @ %TrueClientId);
			if(%camp != -1)
			{
				%obj = nameToId("MissionCleanup\\Camp" @ %TrueClientId @ "\\woodfire");
				if(Vector::getDistance(GameBase::getPosition(%TrueClientId), GameBase::getPosition(%obj)) <= 10)
				{
					DoCampSetup(%TrueClientId, 5);
					Client::sendMessage(%TrueClientId, $MsgBeige, "Camp has been packed up.");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgRed, "You are too far from your camp.");
			}
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "You don't have a camp.");
	
			return;
		}
		if(%w1 == "#deploy")
		{
			if(Player::getItemCount(%TrueClientId, ScoutVehicle))
			{
				%activeScout = ScoutVehicle::GetActive(%TrueClientId);
				if(%activeScout == -1)
				{
					// Check if player is in a blocked zone type
					%playerZone = fetchData(%TrueClientId, "zone");
					%zoneType = "";
					if(%playerZone != "")
						%zoneType = Zone::getType(%playerZone);
					
					// Block deployment if in FREEFORALL, PROTECTED, DUNGEON, or WATER zones
					%blocked = false;
					if(%zoneType == "FREEFORALL" || %zoneType == "PROTECTED" || %zoneType == "DUNGEON" || %zoneType == "WATER")
						%blocked = true;
					
					if(!%blocked)
					{
						%pos = GameBase::getPosition(%TrueClientId);
			
						
		
						%player = Client::getownedObject(%TrueclientId);
						if (GameBase::getLOSInfo(%player,50))
						{
							Player::decItemCount(%TrueClientId, ScoutVehicle);
							RefreshAll(%TrueClientId);
							%group = newObject("Vehicle" @ %TrueClientId, SimGroup);
							addToSet("MissionCleanup", %group);
							Client::sendMessage(%TrueClientId, $MsgBeige, "Setting up vehicle...");
							%rot = GameBase::getRotation(%player); 
							%turret = newObject("Flyer","Flier","Scout",true);
							addToSet("MissionCleanup\\Vehicle" @ %TrueClientId, %turret);
							GameBase::setTeam(%turret,GameBase::getTeam(%player));
							GameBase::setPosition(%turret,$los::position);
							GameBase::setRotation(%turret,%rot);
							$ScoutVehicleActive[%TrueClientId] = %turret;
							$ScoutVehicleOwner[%turret] = %TrueClientId;
							$owner[%turret] = Client::getName(%TrueClientId);
							$ScoutVehicleActiveByOwnerName[$owner[%turret]] = %turret;
							Client::sendMessage(%TrueClientId,0,"Scout deployed.");
							playSound(SoundPickupBackpack,$los::position);
							RequestWorldSave("scout_deploy", 3, "deployables");
							return true;
						}
						else 
							Client::sendMessage(%client,0,"Deploy position out of range");
					}
					else
						Client::sendMessage(%TrueClientId, $MsgRed, "You can't set up a vehicle here.");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgRed, "You already have a scout deployed.");
			}
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "You aren't carrying a vehicle.");
			return;
		}
		if(%w1 == "#Build" || %w1 == "#build")
		{
			// Check for deployable base in Belt storage first
			if(Belt::HasThisStuff(%TrueClientId, "DepBasePack"))
			{
				%player = Client::getOwnedObject(%TrueClientId);
				if(%player == -1 || %player == "")
				{
					Client::sendMessage(%TrueClientId, $MsgRed, "You must be in-game to build a base.");
				}
				else
				{
					// Deploy base from Belt storage
					Belt::DeployItem(%TrueClientId, "DepBasePack", "Deployables");
				}
				return;
			}
			// Check for deployable base in standard inventory
			else if(Player::getItemCount(%TrueClientId, DepBasePack))
			{
				%player = Client::getOwnedObject(%TrueClientId);
				if(%player == -1 || %player == "")
				{
					Client::sendMessage(%TrueClientId, $MsgRed, "You must be in-game to build a base.");
				}
				else
				{
					// Deploy base from standard inventory
					Player::deployItem(%player, DepBasePack);
					Client::sendMessage(%TrueClientId, $MsgBeige, "Building base...");
				}
				return;
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgRed, "You don't have a Deployable Base Pack. You can purchase one from merchants.");
			}
			return;
		}
		if(%w1 == "#undeploy")
		{
			%camp = nameToId("MissionCleanup\\Vehicle" @ %TrueClientId);
			%obj = ScoutVehicle::GetActive(%TrueClientId);
			if(%obj != -1)
			{
				if(%obj == -1 || !isObject(%obj))
				{
					ScoutVehicle::ClearActive(%TrueClientId, %obj);
					if(%camp != -1)
						deleteObject(%camp);
					Client::sendMessage(%TrueClientId, $MsgRed, "Your vehicle record was stale and has been cleared. You can deploy again.");
					return;
				}
				if(Vector::getDistance(GameBase::getPosition(%TrueClientId), GameBase::getPosition(%obj)) <= 20)
				{
					if(Vector::getDistance(GameBase::getPosition(%TrueClientId), GameBase::getPosition(%obj)) >= 2)
					{
						%g = "MissionCleanup/Vehicle" @ %TrueClientId;

						Player::incItemCount(%TrueClientId, ScoutVehicle);
						RefreshAll(%TrueClientId);
						ScoutVehicle::ClearActive(%TrueClientId, %obj);

						%gg = nameToId(%g);
						if(%gg != -1)
						{
							//so the players in the grouptrigger get kicked out first.
							Group::iterateRecursive(%g, GameBase::setPosition, "0 0 0");
							schedule("deleteObject(" @ %gg @ ");", 5);
						}
						else
						{
							GameBase::setPosition(%obj, "0 0 0");
							schedule("if(isObject(" @ %obj @ ")) deleteObject(" @ %obj @ ");", 5);
						}
						Client::sendMessage(%TrueClientId, $MsgBeige, "Your vehicle has been packed up.");
						SaveCharacter(%TrueClientId);
						RequestWorldSave("scout_undeploy", 3, "deployables");
					}
					else
						Client::sendMessage(%TrueClientId, $MsgRed, "Dismount your vehicle first.");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgRed, "You are too far from your vehicle.");
			}
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "You don't have a vehicle.");
	
			return;
		}
	      if(%w1 == "#advcompass")
		{
	            if(%cropped == "")
	                  Client::sendMessage(%TrueClientId, 0, "Use #advcompass zone keyword");
	            else
	            {
				if(SkillCanUse(%TrueClientId, "#advcompass"))
				{
					%obj = GetZoneByKeywords(%TrueClientId, %cropped, 3);
	
					if(%obj != False)
					{
						%mpos = Zone::getMarker(%obj);
	
						%d = GetNESW(GameBase::getPosition(%TrueClientId), %mpos);
						UseSkill(%TrueClientId, $SkillSenseHeading, True, True);
	
						Client::sendMessage(%TrueClientId, 0, Zone::getDesc(%obj) @ " is " @ %d @ " of here.");
					}
					else
						Client::sendMessage(%TrueClientId, 1, "Couldn't fine a zone to match those keywords.");
				}
				else
				{
					Client::sendMessage(%TrueClientId, $MsgWhite, "You can't use #advcompass because you lack the necessary skills.");
					UseSkill(%TrueClientId, $SkillSenseHeading, False, True);
				}
	            }
			return;
	      }
		if(%w1 == "#trancephyte")
		{
			if($isRpgserv)
				Client::sendMessage(%TrueClientId, $MsgBeige, "This server is Trancephyte compatible!");
			else
				Client::sendMessage(%TrueClientId, $MsgRed, "This server is NOT Trancephyte compatible.");
	
			return;
		}
		if(%w1 == "#smith")
		{
			if(!%TrueClientId.IsSmithing)
			{
				%tempsmith = LTrim(fetchData(%TrueClientId, "TempSmith"));
				if((%sc = GetSmithCombo(%tempsmith)) != 0)
				{
					%amt = GetWord(%cropped, 0);
					if(%amt <= 0)
						%amt = 1;
					if(%amt > 100)
						%amt = 100;

					%cost = GetSmithComboCost(%TrueClientId, %sc) * %amt;

					if(HasThisStuff(%TrueClientId, %tempsmith, %amt) && !IsDead(%TrueClientId))
					{
						if(%cost <= fetchData(%TrueClientId, "COINS"))
						{
							AI::sayLater(%TrueClientId, %TrueClientId.currentSmith, "Let me see what I can do...", True);
	
							for(%i = 0; (%w = GetWord(%tempsmith, %i)) != -1; %i+=2)
							{
								%w2 = GetWord(%tempsmith, %i+1) * %amt;
								storeData(%TrueClientId, "BankStorage", SetStuffString(fetchData(%TrueClientId, "BankStorage"), %w, %w2));
								Player::decItemCount(%TrueClientId, %w, %w2);
							}
					
							playSound(SoundSmith, GameBase::getPosition(%TrueClientId));
							schedule("CompleteSmith(" @ %TrueClientId @ ", " @ %cost @ ", " @ %sc @ ", \"" @ %tempsmith @ "\", " @ %amt @ ");", 5.5, %TrueClientId);
							%TrueClientId.IsSmithing = True;
							
							return 1;
						}
						else
						{
							Client::sendMessage(%TrueClientId, $MsgRed, "You can't afford to smith this/these items.~wC_BuySell.wav");
							return 0;
						}
					}
				}
			}
		}
		if(%w1 == "#challenge")
		{
			%id = NEWgetClientByName(%cropped);
			if(Belt::HasThisStuff(%TrueClientId, DuelCard) >= 1)
			{
				if(%cropped == "")
	                		 	Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            	else
	            	{
					if(%id != -1)
					{
						if(%id != %TrueClientId)
						{
							if (fetchData(%id, "EXP") >= 40000)
							{
								if(fetchData(%TrueClientId, "EXP") > fetchData(%id, "EXP"))
									%cost = (fetchData(%TrueClientId, "EXP") - fetchData(%id, "EXP")) * 2 * -1;
								else
									%cost = 0;
								if (fetchData(%TrueClientId, "BANK") >= %cost )
								{
									storeData(%TrueClientId, "BANK", %cost, "inc");
									%lospos = -3521 @ " " @ 1201 @ " " @ 1508;
									%retval = GameBase::setPosition(%TrueClientId, %lospos);
									setHP(%TrueClientId, fetchData(%TrueClientId, "MaxHP"));
									setMANA(%TrueClientId, fetchData(%TrueClientId, "MaxMANA"));
									%lospos = -3484 @ " " @ 1200 @ " " @ 1508;
									%retval = GameBase::setPosition(%id, %lospos);
									setHP(%id, fetchData(%id, "MaxHP"));
									setMANA(%id, fetchData(%id, "MaxMANA"));
									if(%retval != False)
									{
										RefreshAll(%TrueClientId);
										RefreshAll(%id);
										Belt::TakeThisStuff(%TrueClientId, DuelCard, 1);
										for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
											Client::sendMessage(%cl, $MsgRed, %TCsenderName @ " has forced " @ %cropped @ " into a duel! Type #observe if you wish to view the duel.");
										for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
										{
											%name = Client::getName(%cl);
											$zonedis[%name] = "";
											Game::refreshClientScore(%cl);
										}
										$DuelOn = True;
										%challenger = Client::getName(%TrueClientId);
										%opponent = Client::getName(%id);
										$DuelChallenger = %challenger;
										$DuelOpponent = %opponent;
										schedule("DuelFight();",4);
									}
									else
										Client::sendMessage(%TrueClientId, $MsgWhite, "Hmmm... I guess there are people standing in the way of the teleport destinations.  Try again later.");
								}
								else
									Client::sendMessage(%TrueClientId, 0, "You do not have the " @ Number::Beautify(%cost, -3) @ " coins needed to challenge this player.");
							}
							else
								Client::sendMessage(%TrueClientId, 0, "Opponent must be at least level 40.");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "You cannot challenge yourself!");
					}
		                  else
	     		                  Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}	
			}
			else
				Client::sendMessage(%TrueClientId, 0, "You must have a duel card to challenge someone!");
		}
		if(%w1 == "#observe")
		{
			%observer = Client::getName(%TrueClientId);
			if($DuelOn == True)
			{
				if($DuelChallenger != %observer && $DuelOpponent != %observer)
				{
					%lospos = -3511 @ " " @ 1236 @ " " @ 1526.5;
					%retval = GameBase::setPosition(%TrueClientId, %lospos);
				}
				else
					Client::sendMessage(%TrueClientId, 0, "You are fighting in the duel silly!");
			}
			else
				Client::sendMessage(%TrueClientId, 0, "There is no duel taking place!");
		}
		if(%w1 == "#helpseal")
		{
			%observer = Client::getName(%TrueClientId);
			if($SealBattleActive == True)
			{
				if($DuelChallenger != %observer && $DuelOpponent != %observer)
				{
					%lospos = -3588 @ " " @ -2364 @ " " @  354;
					%retval = GameBase::setPosition(%TrueClientId, %lospos);
					
					// Force immediate zone check so player's zone is updated right away
					// This ensures damage protection checks work immediately after teleport
					schedule("DoZoneCheck(2, 0);", 0.1);
					
					// Add player to participants list if they join during the 30-second window
					// Check if they're not already in the list
					if(String::findSubStr($SealBattleParticipants, %TrueClientId) == -1)
					{
						$SealBattleParticipants = AddToCommaList($SealBattleParticipants, %TrueClientId);
						echo("DEBUG #helpseal: Added " @ %observer @ " (" @ %TrueClientId @ ") to seal battle participants");
					}
				}
				else
					Client::sendMessage(%TrueClientId, 0, "You are fighting in the duel silly!");
			}
			else
				Client::sendMessage(%TrueClientId, 0, "No one is attempting to break the seal.");
		}
		if(%w1 == "#forfeit")
		{
			%observer = Client::getName(%TrueClientId);
			if($SealBattleActive == True)
			{
				if($DuelChallenger != %observer && $DuelOpponent != %observer)
				{
					%lospos = -3511 @ " " @ 1236 @ " " @ 1526.5;
					%retval = GameBase::setPosition(%TrueClientId, %lospos);
					SealBattle::Conclude(%TrueClientId);
				}
				else
					Client::sendMessage(%TrueClientId, 0, "You are fighting in the duel silly!");
			}
			else
				Client::sendMessage(%TrueClientId, 0, "No one is attempting to break the seal.");
		}
		if(%w1 == "#duelrules")
		{
			Client::sendMessage(%TrueClientId, $MsgWhite, "If you wish to force someone into a duel, there are some requirements. 1) You need a duel card. 2) Your oponent must be above level 40. 3) If your opponent is lower level than you, it will cost you 2000 coins per level lower than you.");
		}
		if(%w1 == "#offensivespells")
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "OFFENSIVE SPELL LIST");
			Client::sendMessage(%TrueClientId, $MsgWhite, "thorn");
			Client::sendMessage(%TrueClientId, $MsgWhite, "fireball");
			Client::sendMessage(%TrueClientId, $MsgWhite, "firebomb");
			Client::sendMessage(%TrueClientId, $MsgWhite, "icespike");
			Client::sendMessage(%TrueClientId, $MsgWhite, "boom");
			Client::sendMessage(%TrueClientId, $MsgWhite, "icestorm");
			Client::sendMessage(%TrueClientId, $MsgWhite, "ironfist");
			Client::sendMessage(%TrueClientId, $MsgWhite, "cloud");
			Client::sendMessage(%TrueClientId, $MsgWhite, "melt");
			Client::sendMessage(%TrueClientId, $MsgWhite, "powercloud");
			Client::sendMessage(%TrueClientId, $MsgWhite, "hellstorm");
			Client::sendMessage(%TrueClientId, $MsgWhite, "beam");
			Client::sendMessage(%TrueClientId, $MsgWhite, "bullet");
			Client::sendMessage(%TrueClientId, $MsgWhite, "freezerburn");
			Client::sendMessage(%TrueClientId, $MsgWhite, "dimensionrift");
			Client::sendMessage(%TrueClientId, $MsgWhite, "nuke");
			Client::sendMessage(%TrueClientId, $MsgWhite, "tornado");
			Client::sendMessage(%TrueClientId, $MsgWhite, "apocalypse");
			Client::sendMessage(%TrueClientId, $MsgGreen, "ionblast");
			Client::sendMessage(%TrueClientId, $MsgGreen, "shredder");
			Client::sendMessage(%TrueClientId, $MsgGreen, "Terminate");
			Client::sendMessage(%TrueClientId, $MsgGreen, "Snipe");
		}
		if(%w1 == "#neutralspells")
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "NEUTRAL SPELL LIST");
			Client::sendMessage(%TrueClientId, $MsgWhite, "stop");
			Client::sendMessage(%TrueClientId, $MsgWhite, "airfist");
			Client::sendMessage(%TrueClientId, $MsgWhite, "teleport");
			Client::sendMessage(%TrueClientId, $MsgWhite, "lightstep");
			Client::sendMessage(%TrueClientId, $MsgWhite, "transport");
			Client::sendMessage(%TrueClientId, $MsgWhite, "boost");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advtransport");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advshove");
			Client::sendMessage(%TrueClientId, $MsgWhite, "heavystep");
			Client::sendMessage(%TrueClientId, $MsgWhite, "remort");
			Client::sendMessage(%TrueClientId, $MsgWhite, "mimic");
			Client::sendMessage(%TrueClientId, $MsgWhite, "masstransport");
			Client::sendMessage(%TrueClientId, $MsgWhite, "airblast");
			Client::sendMessage(%TrueClientId, $MsgWhite, "airwarp");
		}
		if(%w1 == "#defensivespells")
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "DEFENSIVE SPELL LIST");
			Client::sendMessage(%TrueClientId, $MsgWhite, "heal");
			Client::sendMessage(%TrueClientId, $MsgWhite, "shield");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advheal1");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advshield1");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advheal2");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advshield2");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advheal3");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advshield3");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advheal4");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advshield4");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advheal5");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advshield5");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advheal6");
			Client::sendMessage(%TrueClientId, $MsgWhite, "advshield6");
			Client::sendMessage(%TrueClientId, $MsgWhite, "massshield");
			Client::sendMessage(%TrueClientId, $MsgWhite, "fullheal");
			Client::sendMessage(%TrueClientId, $MsgWhite, "godlyshield");
			Client::sendMessage(%TrueClientId, $MsgWhite, "godlyheal");
			Client::sendMessage(%TrueClientId, $MsgWhite, "massheal");
			Client::sendMessage(%TrueClientId, $MsgWhite, "massfullheal");
			Client::sendMessage(%TrueClientId, $MsgWhite, "healplus 1-6");
			Client::sendMessage(%TrueClientId, $MsgWhite, "shieldplus 1-6");
			
		}
		if(%w1 == "#slash")
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "SLASHING WEAPON LIST");
			Client::sendMessage(%TrueClientId, $MsgWhite, "Hatchet, RustyIronBlade, SharpIronBlade, IronBroadSword, SteelBroadSword");
			Client::sendMessage(%TrueClientId, $MsgWhite, "SteelLongSword, GoldenLongSword, GoldenBastardSword, CrystalBastardSword");
			Client::sendMessage(%TrueClientId, $MsgWhite, "TemperedCrystalBastardSword, CrystalClaymore, DiamondClaymore, DiamondLegendSword");
			Client::sendMessage(%TrueClientId, $MsgWhite, "BlackDiamondDreamSword, BlackDiamondAtomSplitter, TerminusEst");
		}
		if(%w1 == "#pierce")
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "PIERCING WEAPON LIST");
			Client::sendMessage(%TrueClientId, $MsgWhite, "Knife, ButterKnife, LongKnife, IronSpear, SteelSpear, SteelPike");
			Client::sendMessage(%TrueClientId, $MsgWhite, "GoldenPike, CrystalPike, CrystalTrident, TemperedCrystalTrident");
			Client::sendMessage(%TrueClientId, $MsgWhite, "DiamondTrident, DiamondDeathSpear, DiamondLegendSpear, BlackDiamondDreamSpear");
			Client::sendMessage(%TrueClientId, $MsgWhite, "BlackDiamondAtomPiercer");
		}
		if(%w1 == "#bludge")
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "BLUDGEON WEAPON LIST");
			Client::sendMessage(%TrueClientId, $MsgWhite, "Club, CrackedStick, IronStick, IronMace, SteelMace, SteelHammer");
			Client::sendMessage(%TrueClientId, $MsgWhite, "SteelWarHammer, GoldenWarHammer, GoldenDivineMace, DiamondDivineMace");
			Client::sendMessage(%TrueClientId, $MsgWhite, "DiamondBrainSpiller, DiamondLegendMace, BlackDiamondDreamMace, BlackDiamondStomSmasher");
		}
		if(%w1 == "#defense")
		{
			Client::sendMessage(%TrueClientId, $MsgWhite, "DEFENSIVE ITEM LIST");
			Client::sendMessage(%TrueClientId, $MsgRed, "HELMETS");
			Client::sendMessage(%TrueClientId, $MsgWhite, "IronHelmet");
			Client::sendMessage(%TrueClientId, $MsgWhite, "GoldenHelmet");
			Client::sendMessage(%TrueClientId, $MsgWhite, "CrystalHelmet");
			Client::sendMessage(%TrueClientId, $MsgWhite, "DiamondHelmet");
			Client::sendMessage(%TrueClientId, $MsgWhite, "BlackDiamondHelmet");
			Client::sendMessage(%TrueClientId, $MsgWhite, "RedDiamondHelmet * New");
			Client::sendMessage(%TrueClientId, $MsgWhite, "WhiteDiamondHelmet * New");
			Client::sendMessage(%TrueClientId, $MsgRed, "BELTS");
			Client::sendMessage(%TrueClientId, $MsgWhite, "AntiMagicBelt");
			Client::sendMessage(%TrueClientId, $MsgWhite, "MajorAntiMagicBelt");
			Client::sendMessage(%TrueClientId, $MsgWhite, "ExtremeAntiMagicBelt");
			Client::sendMessage(%TrueClientId, $MsgWhite, "GodlyAntiMagicBelt");
			Client::sendMessage(%TrueClientId, $MsgWhite, "HeavenlyAntiMagicBelt * New");
			Client::sendMessage(%TrueClientId, $MsgRed, "RINGS");
			Client::sendMessage(%TrueClientId, $MsgWhite, "MinorPowerRing");
			Client::sendMessage(%TrueClientId, $MsgWhite, "PowerRing");
			Client::sendMessage(%TrueClientId, $MsgWhite, "MajorPowerRing");
			Client::sendMessage(%TrueClientId, $MsgWhite, "ExtremePowerRing");
			Client::sendMessage(%TrueClientId, $MsgWhite, "GodlyPowerRing");
			Client::sendMessage(%TrueClientId, $MsgWhite, "HeavenlyPowerRing * New");
			Client::sendMessage(%TrueClientId, $MsgRed, "SHIELDS");
			Client::sendMessage(%TrueClientId, $MsgWhite, "SteelKnightShield");
			Client::sendMessage(%TrueClientId, $MsgWhite, "CrystalKnightShield");
			Client::sendMessage(%TrueClientId, $MsgWhite, "DiamondKnightShield");
			Client::sendMessage(%TrueClientId, $MsgWhite, "BlackDiamondKnightShield");
			Client::sendMessage(%TrueClientId, $MsgWhite, "RedDiamondKingShield * New");
			Client::sendMessage(%TrueClientId, $MsgWhite, "WhiteDiamondKingShield * New");
			Client::sendMessage(%TrueClientId, $MsgRed, "NECKLACES");
			Client::sendMessage(%TrueClientId, $MsgWhite, "MinorRegenerationNecklace");
			Client::sendMessage(%TrueClientId, $MsgWhite, "RegenerationNecklace");
			Client::sendMessage(%TrueClientId, $MsgWhite, "MajorRegenerationNecklace");
			Client::sendMessage(%TrueClientId, $MsgWhite, "ExtremeRegenerationNecklace");
			Client::sendMessage(%TrueClientId, $MsgWhite, "GodlyRegenerationNecklace");
			Client::sendMessage(%TrueClientId, $MsgWhite, "HeavenlyRegenerationNecklace * New");
	
		}
		if(%w1 == "#armor")
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "ARMOR LIST");
			Client::sendMessage(%TrueClientId, $MsgWhite, "RatSkinShirt, StuddedLeatherSuit, ToughHideSuit, IronScaleMail");
			Client::sendMessage(%TrueClientId, $MsgWhite, "SteelScaleMail, SteelBrigandineMail, GoldenBrigandineMail, GoldenChainMail");
			Client::sendMessage(%TrueClientId, $MsgWhite, "CrystalChainMail, CrystalRingMail, CrystalBandedMail, CrystalSplintMail");
			Client::sendMessage(%TrueClientId, $MsgWhite, "TungstenSplintMail, TungstenPlateMail, DiamondFieldPlate, DiamondFullPlate");
			Client::sendMessage(%TrueClientId, $MsgWhite, "BlackDiamondFullPlate, RedDiamondPlate, WhiteDiamondPlate");
		}
		if(%w1 == "#autoskill")
		{
			// Auto-skill point spending system
			// Usage: #autoskill set <skill1>, <skill2>, ...
			//        #autoskill set <skillId1>,<skillId2>,...
			//        #autoskill all
			//        #autoskill show
			//        #autoskill clear
			%subCmd = GetWord(%cropped, 0);

			if(String::ICompare(%subCmd, "set") == 0)
			{
				// Get everything after "set " as the skill list
				%skillList = "";
				%setPos = String::findSubStr(%cropped, " ");
				if(%setPos != -1)
					%skillList = String::getSubStr(%cropped, %setPos + 1, 999);
				
				if(String::ICompare(String::trim(%skillList), "all") == 0)
				{
					AutoSkill_SetAll(%TrueClientId);
				}
				else if(%skillList != "" && %skillList != -1)
				{
					AutoSkill_Set(%TrueClientId, %skillList);
				}
				else
				{
					Client::sendMessage(%TrueClientId, $MsgBeige, "Usage: #autoskill set <skill1>, <skill2>, ...");
					Client::sendMessage(%TrueClientId, $MsgBeige, "Example: #autoskill set endurance, weight capacity, slashing");
					Client::sendMessage(%TrueClientId, $MsgBeige, "Example: #autoskill set 1,2,3,4,5");
				}
			}
			else if(String::ICompare(%subCmd, "all") == 0)
			{
				AutoSkill_SetAll(%TrueClientId);
			}
			else if(String::ICompare(%subCmd, "show") == 0)
			{
				AutoSkill_Show(%TrueClientId);
			}
			else if(String::ICompare(%subCmd, "hide") == 0)
			{
				AutoSkill_Hide(%TrueClientId);
			}
			else if(String::ICompare(%subCmd, "showmsg") == 0)
			{
				AutoSkill_ShowMsg(%TrueClientId);
			}
			else if(String::ICompare(%subCmd, "clear") == 0)
			{
				AutoSkill_Clear(%TrueClientId);
			}
			else
			{
				Client::sendMessage(%TrueClientId, $MsgBeige, "Auto-Skill Commands:");
				Client::sendMessage(%TrueClientId, $MsgBeige, "  #autoskill set <skill1>, <skill2>, ... - Set priority skills");
				Client::sendMessage(%TrueClientId, $MsgBeige, "  #autoskill set 1,2,3,4,5 - Set priority skills by number");
				Client::sendMessage(%TrueClientId, $MsgBeige, "  #autoskill all - Set all active skills");
				Client::sendMessage(%TrueClientId, $MsgBeige, "  #autoskill show - View current settings");
				Client::sendMessage(%TrueClientId, $MsgBeige, "  #autoskill hide - Hide level-up upgrade messages");
				Client::sendMessage(%TrueClientId, $MsgBeige, "  #autoskill showmsg - Show level-up upgrade messages");
				Client::sendMessage(%TrueClientId, $MsgBeige, "  #autoskill clear - Clear settings");
			}
			return;
		}
		if(%w1 == "#autoparty")
		{
			// Auto-party system for dungeons
			// Usage: #autoparty on/off or just #autoparty for status
			%subCmd = GetWord(%cropped, 0);
			
			if(String::ICompare(%subCmd, "on") == 0)
			{
				AutoParty_Enable(%TrueClientId);
			}
			else if(String::ICompare(%subCmd, "off") == 0)
			{
				AutoParty_Disable(%TrueClientId);
			}
			else
			{
				AutoParty_Show(%TrueClientId);
			}
			return;
		}
		if(%w1 == "#zonedis")

		{
			//Created by Carling
			if(SkillCanUse(%TrueClientId, "#zonedis"))
			{
				%name = Client::getName(%TrueClientId);
				if($DuelChallenger != %name && $DuelOpponent != %name)
				{
					$zonedis[%name] = getword(%cropped,0);
					if($zonedis[%name] != -1)
					{
						Game::refreshClientScore(%TrueClientId);
						Client::sendMessage(%TrueClientId, $MsgWhite, "Zone display set to -" @ $zonedis[%name] @ "-");
					}
					else
					{
						$zonedis[%name] = "";
						Client::sendMessage(%TrueClientId, $MsgWhite, "Zone display has been reset!");
						Game::refreshClientScore(%TrueClientId);
					}
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "This command is not available if you are dueling!");
			}
		}
		if(%w1 == "#home")
		{
			%name = Client::getName(%TrueClientId);
			if($DuelChallenger != %name && $DuelOpponent != %name)
			{
				if($HomeSpawn[%name] != "")
				{
					%lospos = $HomeSpawn[%name];
					GameBase::setPosition(%TrueClientId, %lospos);
					Client::sendMessage(%TrueClientId, $MsgWhite, "You have been transported back to your home.");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "You do not own a home.");
			}
			else 
				Client::sendMessage(%TrueClientId, $MsgWhite, "You cannot go to your house while you are dueling!");
		}
		if(%w1 == "#duelloser")
		{
			%name = Client::getName(%TrueClientId);
			if($DuelLoser == %name)
			{
				%lospos = -3484 @ " " @ 1200 @ " " @ 1508;
				%retval = GameBase::setPosition(%TrueClientId, %lospos);
				Client::sendMessage(%TrueClientId, $MsgWhite, "You have been transported to the DuelingArena to get your pack!");
				$DuelLoser = "";
			}
			else 
				Client::sendMessage(%TrueClientId, $MsgWhite, "You haven't lost a duel recently!");
		}
		if(%w1 == "#housebases")
		{
			Client::sendMessage(%TrueClientId, $MsgWhite, "Here is the list of bases that can be taken over:  Thaliel  Bruin  Delern  Projao");
		}
		if(%w1 == "#housestatus")
		{
			Client::sendMessage(%TrueClientId, $MsgWhite, "Bases Controlled:");
			Client::sendMessage(%TrueClientId, $MsgWhite, "House Yuliple " @ $BaseControl[HouseYuliple] @ ", Artifacts Acquired: " @ $FlagCommand[HouseYuliple]);
			Client::sendMessage(%TrueClientId, $MsgWhite, "House Curama " @ $BaseControl[HouseCurama] @ ", Artifacts Acquired: " @ $FlagCommand[HouseCurama]);
			Client::sendMessage(%TrueClientId, $MsgWhite, "House Arbal " @ $BaseControl[HouseArbal] @ ", Artifacts Acquired: " @ $FlagCommand[HouseArbal]);
			Client::sendMessage(%TrueClientId, $MsgWhite, "House Kronos " @ $BaseControl[HouseKronos] @ ", Artifacts Acquired: " @ $FlagCommand[HouseKronos]);
		}
	
//============================
//ADMIN COMMANDS =============
//============================

		if(%w1 == "#wipeout")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%list = GetEveryoneIdList();
				for(%cl = 0; GetWord(%list, %cl) != -1; %cl += 1)
				{
					%id = GetWord(%list,%cl);
					if (Player::isAiControlled(%id))
					{
						// Skip town bots - they should never be killed by wipeout
						// Town bots have BotInfoAiName set but NOT SpawnBotInfo
						// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
						%botInfoAiName = fetchData(%id, "BotInfoAiName");
						%spawnBotInfo = fetchData(%id, "SpawnBotInfo");
						// If bot has BotInfoAiName but no SpawnBotInfo, it's a town bot - skip it
						if(%botInfoAiName != "" && %spawnBotInfo == "")
							continue;
						
						storeData(%id, "noDropLootbagFlag", True);
						Player::Kill(%id);
					}
				}
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					Client::sendMessage(%cl, $MsgWhite, "All bots have been destroyed! [#wipeout]");
				echo("[ADMIN]: " @ %TCsenderName @ " ran #wipeout");
			}
			return;
		}
		if(%w1 == "#serverwipeit")
		{
		        if(%clientToServerAdminLevel >= 2)
			{
				%list = GetEveryoneIdList();
				for(%cl = 0; GetWord(%list, %cl) != -1; %cl += 1)
				{
					%id = GetWord(%list,%cl);
					if (Player::isAiControlled(%id))
					{
						// Skip town bots - they should never be killed by serverwipeit
						// Town bots have BotInfoAiName set, and they're managed by dynamic loading system
						%botInfoAiName = fetchData(%id, "BotInfoAiName");
						if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
							continue;
						
						storeData(%id, "noDropLootbagFlag", True);
						Player::Kill(%id);
					}
				}
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					Client::sendMessage(%cl, $MsgWhite, "Server has destroyed all bots!");
				echo("[ADMIN]: " @ %TCsenderName @ " ran #serverwipeit");
			}
			return;
		}
		if(%w1 == "#resetbountyall")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					storeData(%cl, "bounty", 0);
				Client::sendMessage(%TrueClientId, $MsgWhite, "All bounties have been reset!");
				echo("[ADMIN]: " @ %TCsenderName @ " has reset all bounties");
			}
		}
		if(%w1 == "#resetbounty")
		{
			%c1 = GetWord(%cropped, 0);
			if(%clientToServerAdminLevel >= 5)
			{
				if(%c1 != -1)
				{
					%id = NEWgetClientByName(%c1);
					if(%id != -1)
					{
						storeData(%id, "bounty", 0);
						Client::sendMessage(%TrueClientId, $MsgWhite, %cropped @ "'s bounty has been reset!");
						echo("[ADMIN]: " @ %TCsenderName @ " has reset " @ %c1 @ "'s bounty");
					}
					else
						Client::sendMessage(%TrueClientId, $MsgWhite, "Invalid player name!");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "Enter a player name!");
			}
		}
		if(%w1 == "#clearzonedis")
		{
			//Created by Carling
			if(%clientToServerAdminLevel >= 2)
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
				{
					%name = Client::getName(%cl);
					$zonedis[%name] = "";
					Game::refreshClientScore(%cl);
				}
				Client::sendMessage(%TrueClientId, $MsgWhite, "All Zone descriptions cleared!");
				echo("[ADMIN]: " @ %TCsenderName @ " ran #clearzonedis");
			}
		}
		if(%w1 == "#addbounty")
		{
			%c1 = GetWord(%cropped, 0);
			%c2 = GetWord(%cropped, 1);
			if(%clientToServerAdminLevel >= 5)
			{
				if(%c1 != -1)
				{
					%id = NEWgetClientByName(%c1);
					if(%id != -1)
					{
						AddBounty(%TrueClientId, %c2);
						Client::sendMessage(%TrueClientId, $MsgWhite, %cropped @ "'s bounty has been set to " @ Number::Beautify(fetchData(%id, "bounty"), -3) @ " coins!");
						echo("[ADMIN]: " @ %TCsenderName @ " has added " @ %c2 @ " coins to " @ %c1 @ " bounty");
					}
					else
						Client::sendMessage(%TrueClientId, $MsgWhite, "Invalid player name!");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "Enter a player name and amount!");
			}
		}
		if(%w1 == "#setremort")
		{
			%c1 = GetWord(%cropped, 0);
			%c2 = GetWord(%cropped, 1);
			if(%clientToServerAdminLevel >= 5)
			{
				if(%c1 != -1)
				{
					%id = NEWgetClientByName(%c1);
					if(%id != -1)
					{
						storeData(%id, "RemortStep", %c2);
						Client::sendMessage(%TrueClientId, $MsgWhite, %cropped @ "'s remort level has been set to " @ fetchData(%id, "RemortStep") @ "!");
						Game::refreshClientScore(%id);
						echo("[ADMIN]: " @ %TCsenderName @ " set " @ %c1 @ " remort level to " @ %c2);
					}
					else
						Client::sendMessage(%TrueClientId, $MsgWhite, "Invalid player name!");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "Enter a player name and data!");
			}
		}
		if(%w1 == "#settournyrank")
		{
			%c1 = GetWord(%cropped, 0);
			%c2 = GetWord(%cropped, 1);
			if(%clientToServerAdminLevel >= 5)
			{
				if(%c1 != -1)
				{
					%id = NEWgetClientByName(%c1);
					if(%id != -1)
					{
						storeData(%id, "TournyRank", %c2);
						Client::sendMessage(%TrueClientId, $MsgWhite, %cropped @ "'s world rank has been set to " @ $WorldRank[fetchData(%id, "TournyRank")] @ "!");
						Game::refreshClientScore(%id);
						echo("[ADMIN]: " @ %TCsenderName @ " set " @ %c1 @ "'s world rank to " @ %c2);
					}
					else
						Client::sendMessage(%TrueClientId, $MsgWhite, "Invalid player name!");
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "Enter a player name and data!");
			}
		}
		if(%w1 == "#spawnflyer")
		{
			%c1 = GetWord(%cropped, 0);
			if(%clientToServerAdminLevel < 3)
			{
				Client::sendMessage(%TrueClientId,0,"Need admin level 3");
			  	return false;
			}
			if(getword(%cropped,0) == -1)
			{
				Client::sendMessage(%TrueClientId,0,"go #spawnflyer scout, lapc, hapc or dueler");
				return false;
			}
			%player = Client::getownedObject(%TrueclientId);
			if (GameBase::getLOSInfo(%player,50))
			{
				%rot = GameBase::getRotation(%player); 
				%turret = newObject("Flyer","Flier",getword(%cropped,0),true);
				addToSet("MissionCleanup", %turret);
				GameBase::setTeam(%turret,GameBase::getTeam(%player));
				GameBase::setPosition(%turret,$los::position);
				GameBase::setRotation(%turret,%rot);
				Client::sendMessage(%TrueClientId,0,getword(%cropped,0)@ " spawned.");
				playSound(SoundPickupBackpack,$los::position);
				echo("[ADMIN]: " @ %TCsenderName @ " spawned a " @ %c1);
			      return true;	
			}
			else 
				Client::sendMessage(%client,0,"Deploy position out of range");	
			return false;
		}
		if(%w1 == "#exportdata")
		{
			if(%clientToServerAdminLevel < 3)
			{
				Client::sendMessage(%TrueClientId, 0, "Need admin level 3");
				return;
			}
			%player = Client::getOwnedObject(%TrueClientId);
			if(GameBase::getLOSInfo(%player, 150))
			{
				%obj = $los::object;
				storeObject(%obj, "temp\\test_object_dump.cs");
				%class = getObjectType(%obj);
				
				// Determine actual spawned object/asset type
				if(%class == "InteriorShape")
				{
					%fileName = %obj.filename;
					if(%fileName == "")
						%fileName = %obj.fileName;
					
					if(%fileName != "")
						%type = String::replace(%fileName, ".dis", "");
					else
						%type = "InteriorShape";
				}
				else
				{
					%type = GameBase::getDataName(%obj);
					if(%type == "False" || %type == "")
						%type = %class;
				}

				$ObjectMapData["Name"] = Object::getName(%obj);
				$ObjectMapData["Class"] = %class;
				$ObjectMapData["Type"] = %type;
				$ObjectMapData["Position"] = GameBase::getPosition(%obj);
				$ObjectMapData["Rotation"] = GameBase::getRotation(%obj);
				export("ObjectMapData*", "temp\\-" @ $missionName @ "-MapData.cs", true);
				Client::sendMessage(%TrueClientId, 2, "Object successfully exported.");
				echo("[ADMIN]: " @ %TCsenderName @ " exported object data");
			}
			else
			{
				Client::sendMessage(%TrueClientId, 0, "No object in line of sight (range: 150)");
			}
			return;
		}
		if(%w1 == "#gm")
		{
			Client::sendMessage(%TrueClientId, $MsgWhite, "THIS COMMAND HAS BEEN DISCONTINUED, PLEASE USE #ANON");
			return;
			if(%clientToServerAdminLevel >= 4)
			{
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					Client::sendMessage(%cl, $MsgRed, %cropped);
			}
			return;
		}
		if(%w1 == "#anon")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%aname = GetWord(%cropped, 0);
				%cn = floor(GetWord(%cropped, 1));
				if(%cn != -1 && %aname != -1)
				{
					%anonmsg = String::NEWgetSubStr(%cropped, String::findSubStr(%cropped, %cn)+String::len(%cn)+1, 99999);
					if(%aname == "all")
					{
						for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
						{
							if(floor(%cl.adminLevel) >= floor(%clientToServerAdminLevel))
								Client::sendMessage(%cl, %cn, "[ANON] " @ %TCsenderName @ ": " @ %anonmsg);
							else
								Client::sendMessage(%cl, %cn, %anonmsg);
						}
					}
					else
					{
						%id = NEWgetClientByName(%aname);
						if(%id != -1)
						{
							if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel))
								Client::sendMessage(%id, %cn, "[ANON] " @ %TCsenderName @ ": " @ %anonmsg);
							else
								Client::sendMessage(%id, %cn, %anonmsg);
						}
		                        else
		                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
					}
				}
				else
					Client::sendMessage(%TrueClientId, $MsgWhite, "Syntax: #anon name/all colorNumber message");
			}
			return;
		}
		if(%w1 == "#fw")
		{
			%c1 = GetWord(%cropped, 0);
	
			if(%c1 != -1)
			{
				%id = NEWgetClientByName(%c1);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && %id != %TrueClientId && floor(%id.adminLevel) != 0)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					if(%clientToServerAdminLevel >= 3)
					{
						%rest = String::getSubStr(%cropped, (String::len(%c1)+1), String::len(%cropped)-(String::len(%c1)+1));
						remoteSay(%id, 0, %rest, %senderName);
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Sent a forwarded message to " @ %id @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " ran #fw");
					}
				}
				else
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Invalid player name, or name is of a superAdmin.");
			}
			else
				Client::sendMessage(%TrueClientId, 0, "Please specify name, command and text.");
	
			return;
		}
		if(%w1 == "#forcespawn")
		{
	            if(%clientToServerAdminLevel >= 2)
			{
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
					
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
	                        else if(%id != -1)
	                        {
						if(IsDead(%id))
						{
							Game::playerSpawn(%id, True);
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Forced " @ %cropped @ " to spawn.");
							echo("[ADMIN]: " @ %TCsenderName @ " forced " @ %cropped @ " to spawn");
						}
						else
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %cropped @ " isn't dead.");
					}
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
			}
			return;
		}
		if(%w1 == "#attacklos")
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped == "")
					Client::sendMessage(%TrueClientId, 0, "Please specify a bot name.");
				else
				{
					%event = String::findSubStr(%cropped, ">");
					if(%event != -1)
					{
						%info = String::NEWgetSubStr(%cropped, 0, %event);
						%cmd = String::NEWgetSubStr(%cropped, %event, 99999);
					}
					else
						%info	= %cropped;
	
					%c1 = getWord(%info, 0);
					%ox = GetWord(%info, 1);
					%oy = GetWord(%info, 2);
					%oz = GetWord(%info, 3);
					%id = NEWgetClientByName(%c1);
	
					if(%id != -1)
					{
						if(IsInCommaList(fetchData(%TrueClientId, "PersonalPetList"), %id) || %clientToServerAdminLevel >= 1)
						{
							if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && %id != %TrueClientId && floor(%id.adminLevel) != 0)
								Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
							else if(Player::isAiControlled(%id))
							{
								%player = Client::getOwnedObject(%TrueClientId);
	
								if(%ox == -1 && %oy == -1 && %oz == -1)
								{
									GameBase::getLOSinfo(%player, 50000);
									%pos = $los::position;
								}
								else
									%pos = %ox @ " " @ %oy @ " " @ %oz;
	
								if(%event != -1)
									AddEventCommand(%id, %senderName, "onPosCloseEnough " @ %pos, %cmd);
	
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %c1 @ " (" @ %id @ ") is attacking position " @ %pos @ ".");
								storeData(%id, "botAttackMode", 3);
								storeData(%id, "tmpbotdata", %pos);
							}
							else
								Client::sendMessage(%TrueClientId, 0, "Player must be a bot.");
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            		}	
			return;
	      }
	      if(%w1 == "#botnormal")
		if(%clientToServerAdminLevel >= 2)
		{
			if(%cropped == "")
				Client::sendMessage(%TrueClientId, 0, "Please specify a bot name.");
			else
			{
				%id = NEWgetClientByName(%cropped);
	
				if(%id != -1)
				{
			            if(IsInCommaList(fetchData(%TrueClientId, "PersonalPetList"), %id) || %clientToServerAdminLevel >= 1)
			            {
						if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && %id != %TrueClientId && floor(%id.adminLevel) != 0)
							Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
						else if(Player::isAiControlled(%id))
		                        {
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Bot is now in normal attack mode.");
							storeData(%id, "botAttackMode", 1);
							%botName = fetchData(%id, "BotInfoAiName");
							// Fallback to Client::getName if BotInfoAiName is invalid
							if(%botName == "" || %botName == -1 || %botName == "0")
								%botName = Client::getName(%id);
							if(%botName != "" && %botName != -1 && %botName != "0")
								AI::newDirectiveRemove(%botName, 99);
							storeData(%id, "tmpbotdata", "");
	
							if(fetchData(%id, "petowner") != "")
							{
								storeData(%id, "botAttackMode", 2);
								storeData(%id, "tmpbotdata", fetchData(%id, "petowner"));
							}
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Player must be a bot.");
					}
	                  }
				else
					Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
		if(%w1 == "#createbotgroup")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped == "")
					Client::sendMessage(%TrueClientId, 0, "Please specify a one-word BotGroup name.");
				else
				{
					if(GetWord(%cropped, 1) == -1)
					{
						%g = GetWord(%cropped, 0);
						%n = AI::CountBotGroupMembers(%g);
						if(!AI::BotGroupExists(%g))
						{
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Created BotGroup '" @ %g @ "'.");
							AI::CreateBotGroup(%g);
							echo("[ADMIN]: " @ %TCsenderName @ " created bot group " @ %g);
						}
						else
							Client::sendMessage(%TrueClientId, 0, "BotGroup already exists and contains " @ %n @ " members.  Use #discardbotgroup to delete a BotGroup.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Please specify a ONE-WORD BotGroup name.");
				}
			}
			return;
		}
		if(%w1 == "#discardbotgroup")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped == "")
					Client::sendMessage(%TrueClientId, 0, "Please specify a one-word BotGroup name.");
				else
				{
					if(GetWord(%cropped, 1) == -1)
					{
						%g = GetWord(%cropped, 0);
						if(AI::BotGroupExists(%g))
						{
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Discarded BotGroup '" @ %g @ "'.");
							AI::DiscardBotGroup(%g);
							echo("[ADMIN]: " @ %TCsenderName @ " discarded bot group " @ %g);
						}
						else
							Client::sendMessage(%TrueClientId, 0, "BotGroup does not exist.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Please specify a ONE-WORD BotGroup name.");
				}
			}
			return;
		}
		if(%w1 == "#getbotgroupleader")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped == "")
					Client::sendMessage(%TrueClientId, 0, "Please specify a one-word BotGroup name.");
				else
				{
					if(GetWord(%cropped, 1) == -1)
					{
						%g = GetWord(%cropped, 0);
						if(AI::BotGroupExists(%g))
						{
							%tl = GetWord($tmpBotGroup[%g], 0);
							%tln = Client::getName(%tl);
							Client::sendMessage(%TrueClientId, 0, "BotGroup leader is " @ %tln @ " (" @ %tl @ ").");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "BotGroup does not exist.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Please specify a ONE-WORD BotGroup name.");
				}
			}
			return;
		}
		if(%w1 == "#botgroup")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
				if(%c1 != -1 && %c2 != -1)
				{
					%id = NEWgetClientByName(%c1);
					if(%id != -1)
					{
						if(Player::isAiControlled(%id))
						{
							if(AI::BotGroupExists(%c2))
							{
								%b = AI::IsInWhichBotGroup(%id);
								if(%b == -1)
								{
									if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Adding minion " @ %c1 @ " (" @ %id @ ") to BotGroup '" @ %c2 @ "'.");
									AI::AddBotToBotGroup(%id, %c2);
								}
								else
									Client::sendMessage(%TrueClientId, 0, "This bot already belongs to the BotGroup '" @ %b @ "'.  Use #rbotgroup to remove a bot from a BotGroup.");
							}
							else
								Client::sendMessage(%TrueClientId, 0, "BotGroup '" @ %c2 @ "' does not exist.  Use #createbotgroup to create a BotGroup.");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Name must be a bot.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
		}
		if(%w1 == "#rbotgroup")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
	
				if(%c1 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	                        if(%id != -1)
	                        {
	                              if(Player::isAiControlled(%id))
	                              {
							%b = AI::IsInWhichBotGroup(%id);
							if(%b != -1)
							{
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Removing minion " @ %c1 @ " (" @ %id @ ") from BotGroup '" @ %b @ "'.");
								AI::RemoveBotFromBotGroup(%id, %b);
							}
							else
								Client::sendMessage(%TrueClientId, 0, "This bot does not belong to a BotGroup.");
	                              }
	                              else
	                                    Client::sendMessage(%TrueClientId, 0, "Name must be a bot.");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
		if(%w1 == "#listbotgroups")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				Client::sendMessage(%TrueClientId, 0, $BotGroups);
			}
			return;
		}
				if(%w1 == "#listbots")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%botList = GetBotIdList();
				%count = 0;
				%enemyBotCount = 0;
				// Count town bots separately
				%townBotCount = 0;
				for(%tb = 0; (%townBotName = GetWord($TownBotRegistry, %tb)) != -1; %tb++)
				{
					if(%townBotName != "" && %townBotName != "0" && $TownBotSpawned[%townBotName] != "")
						%townBotCount++;
				}
				
				// Count actual enemy bots from bot list (more accurate than $numAI)
				if(%botList != "")
				{
					for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
					{
						%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
						if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
							%enemyBotCount++;
					}
				}
				
				%totalBots = %enemyBotCount + %townBotCount;
				%message = "Spawned Bots (" @ %totalBots @ " total - " @ %enemyBotCount @ " enemy, " @ %townBotCount @ " town):";
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#listbots] " @ %message);
				
				if(%botList != "")
				{
					for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
					{
						%count++;
						%botName = Client::getName(%botId);
						%aiName = fetchData(%botId, "BotInfoAiName");
						if(%aiName == "")
							%aiName = "Unknown";
						// CRITICAL: Try to get team from Player object first, then fall back to client ID
						// GameBase::getTeam() on client ID may return -1 if team is only set on Player object
						%playerObj = Client::getOwnedObject(%botId);
						%team = -1;
						if(%playerObj != -1 && %playerObj != "")
						{
							%team = GameBase::getTeam(%playerObj);
						}
						if(%team == -1)
						{
							// Fall back to client ID
							%team = GameBase::getTeam(%botId);
						}
						// If still -1, try to get from stored botTeam
						if(%team == -1)
						{
							%storedTeam = fetchData(%botId, "botTeam");
							if(%storedTeam != "" && %storedTeam != -1 && %storedTeam != "0" && %storedTeam != 0)
							{
								%team = %storedTeam;
							}
						}
						%pos = GameBase::getPosition(%botId);
						%botInfo = "  " @ %count @ ". " @ %botName @ " (ID: " @ %botId @ ", AI: " @ %aiName @ ", Team: " @ %team @ ", Pos: " @ GetWord(%pos, 0) @ " " @ GetWord(%pos, 1) @ " " @ GetWord(%pos, 2) @ ")";
						Client::sendMessage(%TrueClientId, 0, %botInfo);
						echo("[#listbots] " @ %botInfo);
					}
					%totalMessage = "Total bots found: " @ %count;
					Client::sendMessage(%TrueClientId, 0, %totalMessage);
					echo("[#listbots] " @ %totalMessage);
				}
				else
				{
					Client::sendMessage(%TrueClientId, 0, "No bots are currently spawned.");
					echo("[#listbots] No bots are currently spawned.");
				}
			}
			return;
		}
		if(%w1 == "#graveyard" || %w1 == "#graveyardstatus")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				%currentTime = getSimTime();
				%totalEntries = 0;
				%oldestAge = 0;
				%oldestClientId = -1;
				%entryList = "";
				
				// Count graveyard entries and find oldest
				for(%id = 2049; %id <= 2200; %id++)
				{
					if($GraveyardClientId[%id] == "true" || $GraveyardClientId[%id] == "1")
					{
						%totalEntries++;
						%timestamp = $GraveyardTimestamp[%id];
						if(%timestamp != "" && %timestamp != -1)
						{
							%age = %currentTime - %timestamp;
							if(%age > %oldestAge)
							{
								%oldestAge = %age;
								%oldestClientId = %id;
							}
							
							// Find AI name for this client ID
							%aiName = "";
							for(%checkName = 0; %checkName <= 200 && %aiName == ""; %checkName++)
							{
								%testNames = "Liquifier Obliterator Abolisher Banisher Crucifier Devourer Incarnate MoonBreaker Invader Holocaust Corrupter Protector";
								for(%i = 0; (%prefix = GetWord(%testNames, %i)) != -1; %i++)
								{
									%testName = %prefix @ %checkName;
									if($GraveyardClientId[%testName] == %id)
									{
										%aiName = %testName;
										break;
									}
								}
							}
							
							%nameSuffix = "";
							if(%aiName != "")
								%nameSuffix = ", " @ %aiName;
							
							if(%entryList == "")
								%entryList = %id @ " (" @ %age @ "s" @ %nameSuffix @ ")";
							else
								%entryList = %entryList @ ", " @ %id @ " (" @ %age @ "s" @ %nameSuffix @ ")";
						}
						else
						{
							// Entry without timestamp
							if(%entryList == "")
								%entryList = %id @ " (no timestamp)";
							else
								%entryList = %entryList @ ", " @ %id @ " (no timestamp)";
						}
					}
				}
				
				%message = "=== GRAVEYARD STATUS ===";
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#graveyard] " @ %message);
				
				%message = "Total entries: " @ %totalEntries;
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#graveyard] " @ %message);
				
				if(%oldestAge > 0)
				{
					%message = "Oldest entry: clientId " @ %oldestClientId @ " (age: " @ %oldestAge @ "s)";
					Client::sendMessage(%TrueClientId, 0, %message);
					echo("[#graveyard] " @ %message);
				}
				
				if(%totalEntries > 50)
				{
					%message = "WARNING: Graveyard has " @ %totalEntries @ " entries (threshold: 50) - possible accumulation issue";
					Client::sendMessage(%TrueClientId, $MsgRed, %message);
					echo("[#graveyard] " @ %message);
				}
				
				if(%entryList != "")
				{
					%message = "Entries: " @ %entryList;
					Client::sendMessage(%TrueClientId, 0, %message);
					echo("[#graveyard] " @ %message);
				}
				else if(%totalEntries == 0)
				{
					%message = "Graveyard is empty";
					Client::sendMessage(%TrueClientId, 0, %message);
					echo("[#graveyard] " @ %message);
				}
			}
			return;
		}
		if(%w1 == "#listtownbots")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				%aliveCount = 0;
				%staleCount = 0;
				%totalCount = 0;
				
				Client::sendMessage(%TrueClientId, 0, "=== TOWN BOT LIST ===");
				echo("[ADMIN]: " @ %TCsenderName @ " - === TOWN BOT LIST ===");
				Client::sendMessage(%TrueClientId, 0, "Total registered: " @ GetWordCount($TownBotRegistry));
				echo("[ADMIN]: " @ %TCsenderName @ " - Total registered: " @ GetWordCount($TownBotRegistry));
				Client::sendMessage(%TrueClientId, 0, "$ActiveTownBots: " @ $ActiveTownBots);
				echo("[ADMIN]: " @ %TCsenderName @ " - $ActiveTownBots: " @ $ActiveTownBots);
				Client::sendMessage(%TrueClientId, 0, "");
				echo("[ADMIN]: " @ %TCsenderName @ " - ");
				
				// Check all registered town bots
				for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
				{
					%totalCount++;
					%clientId = $TownBotSpawned[%botName];
					%playerObj = Client::getOwnedObject(%clientId);
					%isAlive = (%playerObj != "" && %playerObj != -1 && isObject(%playerObj));
					
					if(%clientId == "" || %clientId == -1 || %clientId == "0")
					{
						// Not spawned
						Client::sendMessage(%TrueClientId, 0, "  " @ %totalCount @ ". " @ %botName @ " - NOT SPAWNED (no clientId in $TownBotSpawned)");
						echo("[ADMIN]: " @ %TCsenderName @ " -   " @ %totalCount @ ". " @ %botName @ " - NOT SPAWNED (no clientId in $TownBotSpawned)");
					}
					else if(!%isAlive)
					{
						// Stale entry - has clientId but bot is dead
						%staleCount++;
						%displayName = Client::getName(%clientId);
						%zone = fetchData(%clientId, "zone");
						if(%zone == "") %zone = "Unknown";
						Client::sendMessage(%TrueClientId, $MsgRed, "  " @ %totalCount @ ". " @ %botName @ " - STALE (clientId: " @ %clientId @ ", name: " @ %displayName @ ", zone: " @ %zone @ ") - Bot is DEAD but still in $TownBotSpawned!");
						echo("[ADMIN]: " @ %TCsenderName @ " -   " @ %totalCount @ ". " @ %botName @ " - STALE (clientId: " @ %clientId @ ", name: " @ %displayName @ ", zone: " @ %zone @ ") - Bot is DEAD but still in $TownBotSpawned!");
					}
					else
					{
						// Alive
						%aliveCount++;
						%displayName = Client::getName(%clientId);
						%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
						%team = GameBase::getTeam(%clientId);
						%pos = GameBase::getPosition(%clientId);
						%zone = fetchData(%clientId, "zone");
						if(%zone == "") %zone = "Unknown";
						
						Client::sendMessage(%TrueClientId, $MsgGreen, "  " @ %totalCount @ ". " @ %botName @ " - ALIVE");
						echo("[ADMIN]: " @ %TCsenderName @ " -   " @ %totalCount @ ". " @ %botName @ " - ALIVE");
						Client::sendMessage(%TrueClientId, 0, "      ClientId: " @ %clientId @ ", Display: " @ %displayName @ ", AI: " @ %botInfoAiName);
						echo("[ADMIN]: " @ %TCsenderName @ " -       ClientId: " @ %clientId @ ", Display: " @ %displayName @ ", AI: " @ %botInfoAiName);
						Client::sendMessage(%TrueClientId, 0, "      Team: " @ %team @ ", Zone: " @ %zone @ ", Pos: " @ GetWord(%pos, 0) @ " " @ GetWord(%pos, 1) @ " " @ GetWord(%pos, 2));
						echo("[ADMIN]: " @ %TCsenderName @ " -       Team: " @ %team @ ", Zone: " @ %zone @ ", Pos: " @ GetWord(%pos, 0) @ " " @ GetWord(%pos, 1) @ " " @ GetWord(%pos, 2));
					}
				}
				
				Client::sendMessage(%TrueClientId, 0, "");
				echo("[ADMIN]: " @ %TCsenderName @ " - ");
				Client::sendMessage(%TrueClientId, 0, "Summary: " @ %aliveCount @ " alive, " @ %staleCount @ " stale, " @ (%totalCount - %aliveCount - %staleCount) @ " not spawned");
				echo("[ADMIN]: " @ %TCsenderName @ " - Summary: " @ %aliveCount @ " alive, " @ %staleCount @ " stale, " @ (%totalCount - %aliveCount - %staleCount) @ " not spawned");
				
				if(%staleCount > 0)
				{
					Client::sendMessage(%TrueClientId, $MsgRed, "WARNING: " @ %staleCount @ " stale entries found! These client IDs are not being cleaned up properly.");
					echo("[ADMIN]: " @ %TCsenderName @ " - WARNING: " @ %staleCount @ " stale entries found! These client IDs are not being cleaned up properly.");
					Client::sendMessage(%TrueClientId, $MsgRed, "Consider running #killallbots or #setupai to clean up stale entries.");
					echo("[ADMIN]: " @ %TCsenderName @ " - Consider running #killallbots or #setupai to clean up stale entries.");
				}
			}
			return;
		}
		if(%w1 == "#despawnemptyzones" || %w1 == "#cleanupemptyzones")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				%despawnedCount = 0;
				%checkedZones = 0;
				
				Client::sendMessage(%TrueClientId, 0, "=== DESPAWNING TOWN BOTS IN EMPTY ZONES ===");
				echo("[ADMIN]: " @ %TCsenderName @ " - === DESPAWNING TOWN BOTS IN EMPTY ZONES ===");
				
				// Get list of all zones that have town bots
				%zonesWithBots = "";
				for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
				{
					%clientId = $TownBotSpawned[%botName];
					if(%clientId != "" && %clientId != -1 && %clientId != "0")
					{
						%playerObj = Client::getOwnedObject(%clientId);
						if(%playerObj != "" && %playerObj != -1 && isObject(%playerObj))
						{
							%zone = fetchData(%clientId, "zone");
							if(%zone != "" && %zone != -1 && %zone != "0")
							{
								// Add zone to list if not already there
								if(String::findSubStr(" " @ %zonesWithBots @ " ", " " @ %zone @ " ") == -1)
									%zonesWithBots = %zonesWithBots @ %zone @ " ";
							}
						}
					}
				}
				
				// Check each zone for players
				for(%i = 0; (%zoneFolderID = GetWord(%zonesWithBots, %i)) != -1; %i++)
				{
					%checkedZones++;
					%zoneIndex = Zone::getIndex(%zoneFolderID);
					if(%zoneIndex <= 0)
						continue; // Invalid zone, skip
					%playerList = Zone::getPlayerList(%zoneFolderID, 2); // Type 2 = real players only (not bots)
					%hasPlayers = (%playerList != "" && %playerList != -1);
					
					if(!%hasPlayers)
					{
						// Zone is empty - find and despawn all town bots in this zone
						%zoneDesc = $Zone::Desc[%zoneIndex];
						if(%zoneDesc == "") %zoneDesc = "Zone " @ %zoneIndex;
						
						Client::sendMessage(%TrueClientId, 0, "Zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") is empty - despawning town bots...");
						echo("[ADMIN]: " @ %TCsenderName @ " - Zone " @ %zoneIndex @ " (" @ %zoneDesc @ ") is empty - despawning town bots...");
						
						// Find all town bots in this zone
						for(%j = 0; (%botName = GetWord($TownBotRegistry, %j)) != -1; %j++)
						{
							%clientId = $TownBotSpawned[%botName];
							if(%clientId != "" && %clientId != -1 && %clientId != "0")
							{
								%playerObj = Client::getOwnedObject(%clientId);
								if(%playerObj != "" && %playerObj != -1 && isObject(%playerObj))
								{
									%botZone = fetchData(%clientId, "zone");
									if(%botZone == %zoneFolderID)
									{
										// CRITICAL SAFEGUARD: Verify this is actually a bot, not a player
										if(!isRPGAI(%clientId) && !Player::isAiControlled(%clientId))
										{
											echo("WARNING: #despawnemptyzones - clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ ") is not a bot! Skipping to prevent player data cleanup.");
											continue;
										}
										
										// CRITICAL SAFEGUARD: Must have BotInfoAiName to be considered a bot
										%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
										if(%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1)
										{
											echo("WARNING: #despawnemptyzones - clientId " @ %clientId @ " (" @ Client::getName(%clientId) @ ") has no BotInfoAiName - this is a player, not a bot. Skipping.");
											continue;
										}
										
										// Additional check - if it has a character save file, it's a player
										%playerName = Client::getName(%clientId);
										if(%playerName != "" && %playerName != -1)
										{
											%characterFile = "temp\\" @ %playerName @ ".cs";
											if(isFile(%characterFile))
											{
												echo("WARNING: #despawnemptyzones - clientId " @ %clientId @ " (" @ %playerName @ ") has character save file (this is a player, not a bot). Skipping.");
												continue;
											}
										}
										
										// This town bot is in the empty zone - despawn it
										%displayName = Client::getName(%clientId);
										
										// Extract bot name from BotInfoAiName (remove "TownBot_" prefix)
										%aiNameForDelete = %botInfoAiName;
										if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
											%aiNameForDelete = String::getSubStr(%botInfoAiName, 8, 999);
										
										// Delete the bot
										%escapedName = String::replace(%aiNameForDelete, "\"", "\\\"");
										AI::delete(%escapedName);
										
										// Clear tracking data
										$TownBotSpawned[%botName] = "";
										$ActiveTownBots--;
										$TotalActiveBots--;
										if($ActiveTownBots < 0) $ActiveTownBots = 0;
										if($TotalActiveBots < 0) $TotalActiveBots = 0;
										
										// Clear bot data
										$TownBotData[%clientId, "BotInfoAiName"] = "";
										storeData(%clientId, "BotInfoAiName", "");
										
										Client::sendMessage(%TrueClientId, 0, "  Despawned: " @ %botName @ " (" @ %displayName @ ")");
										echo("[ADMIN]: " @ %TCsenderName @ " -   Despawned: " @ %botName @ " (" @ %displayName @ ")");
										%despawnedCount++;
									}
								}
							}
						}
					}
				}
				
				Client::sendMessage(%TrueClientId, 0, "");
				echo("[ADMIN]: " @ %TCsenderName @ " - ");
				Client::sendMessage(%TrueClientId, 0, "Summary: Despawned " @ %despawnedCount @ " town bots from " @ %checkedZones @ " zones checked");
				echo("[ADMIN]: " @ %TCsenderName @ " - Summary: Despawned " @ %despawnedCount @ " town bots from " @ %checkedZones @ " zones checked");
			}
			return;
		}
		if(%w1 == "#despawnbots")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%despawnedCount = 0;
				%townBotCount = 0;
				%enemyBotCount = 0;
				
				Client::sendMessage(%TrueClientId, 0, "=== DESPAWNING ALL BOTS ===");
				echo("[ADMIN]: " @ %TCsenderName @ " - === DESPAWNING ALL BOTS ===");
				
				// Get list of all bots
				%botList = GetBotIdList();
				if(%botList != "")
				{
					for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
					{
						// CRITICAL SAFEGUARD: Verify this is actually a bot, not a player
						if(!isRPGAI(%botId) && !Player::isAiControlled(%botId))
						{
							echo("WARNING: #despawnbots - clientId " @ %botId @ " (" @ Client::getName(%botId) @ ") is not a bot! Skipping to prevent player data cleanup.");
							continue;
						}
						
						// CRITICAL SAFEGUARD: Must have BotInfoAiName or SpawnBotInfo to be considered a bot
						%botInfoAiName = fetchData(%botId, "BotInfoAiName");
						%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
						if((%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1) && 
						   (%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1))
						{
							echo("WARNING: #despawnbots - clientId " @ %botId @ " (" @ Client::getName(%botId) @ ") has no BotInfoAiName or SpawnBotInfo - this is a player, not a bot. Skipping.");
							continue;
						}
						
						// Additional check - if it has a character save file, it's a player
						%playerName = Client::getName(%botId);
						if(%playerName != "" && %playerName != -1)
						{
							%characterFile = "temp\\" @ %playerName @ ".cs";
							if(isFile(%characterFile))
							{
								echo("WARNING: #despawnbots - clientId " @ %botId @ " (" @ %playerName @ ") has character save file (this is a player, not a bot). Skipping.");
								continue;
							}
						}
						
						// ADDITIONAL SAFEGUARD: Only despawn bots that have fully loaded and spawned
						if(!fetchData(%botId, "HasLoadedAndSpawned"))
						{
							echo("WARNING: #despawnbots - Bot clientId " @ %botId @ " has not loaded and spawned yet. Skipping to prevent data corruption.");
							continue;
						}
						
						// Determine if this is a town bot or enemy bot
						%isTownBot = false;
						%isEnemyBot = false;
						if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
						{
							if(%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1)
							{
								%isTownBot = true;
							}
							else
							{
								%isEnemyBot = true;
							}
						}
						else if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
						{
							%isEnemyBot = true;
						}
						
						%displayName = Client::getName(%botId);
						
						// Despawn the bot based on type
						if(%isTownBot)
						{
							// Town bot - extract bot name from BotInfoAiName (remove "TownBot_" prefix)
							%aiNameForDelete = %botInfoAiName;
							if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
								%aiNameForDelete = String::getSubStr(%botInfoAiName, 8, 999);
							
							// Delete the bot
							%escapedName = String::replace(%aiNameForDelete, "\"", "\\\"");
							AI::delete(%escapedName);
							
							// Find bot name in registry
							%botName = "";
							for(%j = 0; (%regBotName = GetWord($TownBotRegistry, %j)) != -1; %j++)
							{
								if($TownBotSpawned[%regBotName] == %botId)
								{
									%botName = %regBotName;
									break;
								}
							}
							
							// Clear tracking data
							if(%botName != "")
								$TownBotSpawned[%botName] = "";
							$ActiveTownBots--;
							$TotalActiveBots--;
							if($ActiveTownBots < 0) $ActiveTownBots = 0;
							if($TotalActiveBots < 0) $TotalActiveBots = 0;
							
							%townBotCount++;
						}
						else if(%isEnemyBot)
						{
							// Enemy bot - kill it (triggers Player::onKilled() which handles cleanup)
							%botInfoAiNameForDelete = %botInfoAiName;
							if(%botInfoAiNameForDelete == "" || %botInfoAiNameForDelete == "0" || %botInfoAiNameForDelete == -1)
							{
								// Fallback: try arrays if storeData is already cleared
								%botInfoAiNameForDelete = $EnemyBotData[%botId, "BotInfoAiName"];
								if(%botInfoAiNameForDelete == "" || %botInfoAiNameForDelete == -1 || %botInfoAiNameForDelete == "0")
									%botInfoAiNameForDelete = $ClientData[%botId, "BotInfoAiName"];
							}
							
							// Clear spawn scheduled flag to prevent respawn blocking
							if(%botInfoAiNameForDelete != "" && %botInfoAiNameForDelete != -1 && %botInfoAiNameForDelete != "0")
							{
								$SpawnAIScheduled[%botInfoAiNameForDelete] = "";
							}
							
							// Mark as no-drop lootbag before killing
							storeData(%botId, "noDropLootbagFlag", True);
							// CRITICAL: Set noExperienceFlag to prevent EXP distribution for admin-deleted bots
							storeData(%botId, "noExperienceFlag", True);
							
							// Kill the bot - this will trigger Player::onKilled() which handles cleanup
							Player::Kill(%botId);
							
							%enemyBotCount++;
						}
						
						Client::sendMessage(%TrueClientId, 0, "  Despawned: " @ %displayName @ " (" @ %botId @ ")");
						echo("[ADMIN]: " @ %TCsenderName @ " -   Despawned: " @ %displayName @ " (" @ %botId @ ")");
						%despawnedCount++;
					}
				}
				
				Client::sendMessage(%TrueClientId, 0, "");
				echo("[ADMIN]: " @ %TCsenderName @ " - ");
				Client::sendMessage(%TrueClientId, 0, "Summary: Despawned " @ %despawnedCount @ " bots (" @ %townBotCount @ " town, " @ %enemyBotCount @ " enemy)");
				echo("[ADMIN]: " @ %TCsenderName @ " - Summary: Despawned " @ %despawnedCount @ " bots (" @ %townBotCount @ " town, " @ %enemyBotCount @ " enemy)");
			}
			return;
		}
		if(%w1 == "#despawnzonebots")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%zoneIndex = %cropped;
				if(%zoneIndex == "" || %zoneIndex == -1 || %zoneIndex == 0)
				{
					Client::sendMessage(%TrueClientId, 0, "Usage: #despawnzonebots [zoneIndex]");
					Client::sendMessage(%TrueClientId, 0, "Example: #despawnzonebots 1");
					return;
				}
				
				// Convert to integer
				%zoneIndex = %zoneIndex * 1;
				if(%zoneIndex <= 0)
				{
					Client::sendMessage(%TrueClientId, 0, "Error: Invalid zone index. Must be a positive number.");
					return;
				}
				
				// Call DespawnZoneBots function
				Client::sendMessage(%TrueClientId, 0, "=== DESPAWNING BOTS IN ZONE " @ %zoneIndex @ " ===");
				echo("[ADMIN]: " @ %TCsenderName @ " - === DESPAWNING BOTS IN ZONE " @ %zoneIndex @ " ===");
				
				DespawnZoneBots(%zoneIndex);
				
				Client::sendMessage(%TrueClientId, 0, "Despawn command executed for zone " @ %zoneIndex);
				echo("[ADMIN]: " @ %TCsenderName @ " - Despawn command executed for zone " @ %zoneIndex);
			}
			return;
		}
		if(%w1 == "#setupai")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
					deleteObject(%id);
				InitTownBots();
				echo("[ADMIN]: " @ %TCsenderName @ " ran #setupai");
			}
			return;
		}
		if(%w1 == "#getadmin")
		{
	            if(%clientToServerAdminLevel >= 1)
			{
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
					
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
	                        else if(%id != -1)
	                        {
						%a = floor(%id.adminLevel);
						Client::sendMessage(%TrueClientId, 0, %cropped @ "'s Admin Clearance Level: " @ %a);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
			}
			return;
		}
		if(%w1 == "#setadmin")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
	                        else if(%id != -1)
	                        {
						%a = floor(%c2);
						if(%a < 0)
							%a = 0;
						if(%a > %clientToServerAdminLevel)
							%a = %clientToServerAdminLevel;
	
						%id.adminLevel = %a;
						Game::refreshClientScore(%id);		//so the ping and PL are shown properly
	
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") Admin Clearance Level to " @ %id.adminLevel @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " set " @ %c1 @ " admin level to " @ %c2);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
		if(%w1 == "#eyes")
		{
			if(%cropped == "")
				Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
			else
			{
				%id = NEWgetClientByName(%cropped);
	
				if(IsInCommaList(fetchData(%TrueClientId, "PersonalPetList"), %id) || %clientToServerAdminLevel >= 1)
				{
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && %id != %TrueClientId && floor(%id.adminLevel) != 0)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						if(!IsDead(%id))
						{
							if(%clientToServerAdminLevel >= 1)
							{
								//revert
								Client::setControlObject(%TrueClientId.possessId, %TrueClientId.possessId);
								Client::setControlObject(%TrueClientId, %TrueClientId);
								storeData(%TrueClientId.possessId, "dumbAIflag", "");
								$possessedBy[%TrueClientId.possessId] = "";
			
								//eyes - Use observer camera to view target player's first-person POV without taking control
								%targetPlayerObj = Client::getOwnedObject(%id);
								if(%targetPlayerObj != -1 && %targetPlayerObj != "" && isObject(%targetPlayerObj))
								{
									// Store the target ID so we can revert properly
									storeData(%TrueClientId, "EyesTargetId", %id);
									// Use observer camera with very small distance (0.01) to simulate first-person view
									// This allows viewing without taking control of the target player
									Client::setControlObject(%TrueClientId, Client::getObserverCamera(%TrueClientId));
									Observer::setOrbitObject(%TrueClientId, %targetPlayerObj, 0.01, 0.01, 0.01);
									echo("[ADMIN]: " @ Client::getName(%TrueClientId) @ " is now viewing " @ Client::getName(%id) @ "'s first-person POV.");
								}
								else
								{
									Client::sendMessage(%TrueClientId, 0, "Could not set eyes view: Target player object not found.");
								}
							}
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Target client is dead.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
			}
			return;
		}
		if(%w1 == "#possess")
		{
			if(%cropped == "")
				Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
			else
			{
				%id = NEWgetClientByName(%cropped);
	
				if(IsInCommaList(fetchData(%TrueClientId, "PersonalPetList"), %id) || %clientToServerAdminLevel >= 1)
				{
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && %id != %TrueClientId && floor(%id.adminLevel) != 0)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						if(!IsDead(%id))
						{
							if(%clientToServerAdminLevel >= 4)
							{
								//revert
								Client::setControlObject(%TrueClientId.possessId, %TrueClientId.possessId);
								Client::setControlObject(%TrueClientId, %TrueClientId);
								storeData(%TrueClientId.possessId, "dumbAIflag", "");
								$possessedBy[%TrueClientId.possessId] = "";
		
								//possess
								if(Player::isAiControlled(%id))
								{
									storeData(%id, "dumbAIflag", True);
									%botName = fetchData(%id, "BotInfoAiName");
									// Fallback to Client::getName if BotInfoAiName is invalid
									if(%botName == "" || %botName == -1 || %botName == "0")
										%botName = Client::getName(%id);
									if(%botName != "" && %botName != -1 && %botName != "0")
									{
										AI::setVar(%botName, SpotDist, 0);
										AI::newDirectiveRemove(%botName, 99);
									}
								}
								%TrueClientId.possessId = %id;
								$possessedBy[%id] = %TrueClientId;
								Client::setControlObject(%id, -1);
								Client::setControlObject(%TrueClientId, %id);
								echo("[ADMIN]: " @ %TCsenderName @ " possessed " @ %cropped);
							}
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Target client is dead.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
			}
			return;
		}
		if(%w1 == "#revert")
		{
			if(%TrueClientId.sleepMode == "")
			{
				// Check if admin was using #eyes command
				%eyesTargetId = fetchData(%TrueClientId, "EyesTargetId");
				if(%eyesTargetId != "" && %eyesTargetId != -1)
				{
					// Clear the eyes target and restore to admin's own player object
					storeData(%TrueClientId, "EyesTargetId", "");
					Client::setControlObject(%TrueClientId, Client::getOwnedObject(%TrueClientId));
					echo("[ADMIN]: " @ Client::getName(%TrueClientId) @ " reverted from eyes view.");
				}
				else
				{
					// Normal revert (for possess command)
					Client::setControlObject(%TrueClientId.possessId, %TrueClientId.possessId);
					Client::setControlObject(%TrueClientId, %TrueClientId);
					storeData(%TrueClientId.possessId, "dumbAIflag", "");
					$possessedBy[%TrueClientId.possessId] = "";
				}
			}
			return;
		}
		if(%w1 == "#fixspellflag")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
	                        else if(%id != -1)
	                        {
						storeData(%id, "SpellCastStep", "");
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Spell flag reset.");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
			return;
		}
		if(%w1 == "#fixbashflag")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
	                        else if(%id != -1)
	                        {
						storeData(%id, "blockBash", "");
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Bash flag reset.");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
			return;
		}
		if(%w1 == "#ban")
		{
	            if(%clientToServerAdminLevel >= 6)
	            {
	                  %c1 = GetWord(%cropped, 0);
				%id = NEWgetClientByName(%c1);
	                  %ip = Client::getTransportAddress(%id);
	                  if(%c2 == -1)
	                        %c2 = False;
	                  if(%c1 != -1)
	                  {
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
	                              BanList::addAbsolute("IP:24.218.18.88", 972512322);
						echo("[ADMIN]: " @ %TCsenderName @ " banned " @ %c1 @ " from server");
					}
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
		}
	      if(%w1 == "#kick")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	                  if(%c2 == -1)
	                        %c2 = False;
	
	                  if(%c1 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
	                              Admin::Kick(%TrueClientId, %id, %c2);
						echo("[ADMIN]: " @ %TCsenderName @ " kicked " @ %c1 @ " for " @ %c2 @ " seconds");
					}
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
		}
		if(%w1 == "#kickid")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				%id = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
				if(%c2 == -1)
					%c2 = False;
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
					Admin::Kick(%TrueClientId, %id, %c2);
	                  else
					Client::sendMessage(%TrueClientId, 0, "Please specify clientId & data.");
			}
			return;
		}
		if(%w1 == "#listallclients")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				%clientList = "";
				%clientCount = 0;
				%playerCount = 0;
				%botCount = 0;
				
				// Iterate through all clients
				for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
				{
					%clientCount++;
					%clientName = Client::getName(%cl);
					%isBot = isRPGAI(%cl);
					
					if(%isBot)
					{
						%botCount++;
						%botName = fetchData(%cl, "BotInfoAiName");
						if(%botName == "" || %botName == -1 || %botName == "0")
							%botName = %clientName;
						%clientList = %clientList @ "Bot: " @ %cl @ " (" @ %botName @ ")\n";
					}
					else
					{
						%playerCount++;
						%playerObj = Client::getOwnedObject(%cl);
						if(%playerObj != -1 && %playerObj != "")
							%clientList = %clientList @ "Player: " @ %cl @ " (" @ %clientName @ ") - PlayerObj: " @ %playerObj @ "\n";
						else
							%clientList = %clientList @ "Player: " @ %cl @ " (" @ %clientName @ ") - No PlayerObj\n";
					}
				}
				
				%summary = "=== All Clients (Total: " @ %clientCount @ " | Players: " @ %playerCount @ " | Bots: " @ %botCount @ ") ===\n";
				Client::sendMessage(%TrueClientId, 0, %summary @ %clientList);
				echo("[ADMIN]: " @ %TCsenderName @ " used #listallclients - Total: " @ %clientCount @ " clients");
			}
			return;
		}
		if (%w1 == "#admin") {
			if (%cropped != "") {
					for (%i=0; $TaurikAdmin[%i] != ""; %i++) {
					%adminline = $TaurikAdmin[%i];
					%adminname = getWord(%adminline, 0);
					%adminpass = getWord(%adminline, 1);
					%admintype = getWord(%adminline, 2);
					
					// Use case-insensitive comparison for name matching, but case-sensitive for password
					%clientName = Client::getName(%TrueClientId);
					if(%cropped == %adminpass && String::ICompare(%clientName, %adminname) == 0)
					{
						%TrueClientId.adminLevel = %admintype;
						Game::refreshClientScore(%TrueClientId);
						messageonlyadmins(1, Client::getName(%TrueClientId) @ " is now a LVL " @ %admintype @ " administrator.~waccess_denied.wav");
						return;
					}
					}
			} else {
				return;
			}
			%attemptip = Client::getTransportAddress(%TrueClientId);
			$adminattempt::entry = "[Failed Admin Access Attempt] From " @ Client::getName(%TrueClientId) @ " (" @ %attemptip @ "). Tried to use password: " @ %cropped;
			export("$adminattempt::*", "config\\Admin Attempts Log.txt", True);
		}
	      if(%w1 == "#human")
		{
	            if(%clientToServerAdminLevel >= 4)
	                  ChangeRace(%TrueClientId, "Human");
			return;
	      }
		if(%w1 == "#dk")
		{
	            if(%clientToServerAdminLevel >= 4)
	                  ChangeRace(%TrueClientId, "DeathKnight");
			return;
	      }
	      if(%w1 == "#admin2")
		{
					%name = Client::getName(%TrueClientId);
					echo("[ADMIN]: " @ %TCsenderName @ " is a mother fucker.");
					messageall(3,"[GLBL] " @ %name @ ": I like horse porn");
			return;
	      }
		if(%w1 == "#loadworld")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
				{
	                        LoadWorld();
					echo("[ADMIN]: " @ %TCsenderName @ " ran #loadworld");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Do not use parameters for this function call.");
	            }
			return;
	      }
	      if(%w1 == "#saveworld")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                        RequestWorldSave("admin_saveworld", 0, "full");
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Do not use parameters for this function call.");
	            }
			return;
	      }
		if(%w1 == "#removeworld")
		{
	            if(%clientToServerAdminLevel >= 6)
	            {
	                  if(%cropped == "")
				{
	                        File::delete("RPG\\scripts.vol");
					File::delete("RPG\\MISSIONS\\KingdomKronos.mis");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Do not use parameters for this function call.");
	            }
			return;
	      }
	      if(%w1 == "#loadcharacter")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify clientId.");
	                  else
				{
	                        LoadCharacter(%cropped);
					echo("[ADMIN]: " @ %TCsenderName @ " loaded character " @ %cropped);
				}
	            }
			return;
	      }
	      if(%w1 == "#item")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
				%name = GetWord(%cropped, 0);
				%item = GetWord(%cropped, 1);
				%count = GetWord(%cropped, 2);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					// Check if it's a Belt item first
					if(isBeltItem(%item))
					{
						Belt::GiveThisStuff(%id, %item, %count, true);
					}
					else
					{
						Player::setItemCount(%id, %item, %count);
					}
					RefreshAll(%id);
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Set " @ %name @ " (" @ %id @ ") " @ %item @ " count to " @ %count);
					echo("[ADMIN]: " @ %TCsenderName @ " set item " @ %item @ " count to " @ %count);
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#addbelt")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
				%name = GetWord(%cropped, 0);
				%item = GetWord(%cropped, 1);
				%count = GetWord(%cropped, 2);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					// Validate it's a belt item
					if(isBeltItem(%item))
					{
						// Check if count is negative (remove) or positive (add)
						if(%count < 0)
						{
							// Remove items - make count positive for TakeThisStuff
							%removeCount = %count * -1;
							
							// Check if player has enough items to remove
							%hasCount = Belt::HasThisStuff(%id, %item);
							if(%hasCount >= %removeCount)
							{
								// CRITICAL: Use Belt::TakeThisStuff for proper formatting (maintains trailing space for equipped items)
								Belt::TakeThisStuff(%id, %item, %removeCount);
								RefreshAll(%id);
								SaveCharacter(%id);
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Removed " @ %removeCount @ " " @ %item @ " from " @ %name @ " (" @ %id @ ") backpack");
								echo("[ADMIN]: " @ %TCsenderName @ " removed " @ %removeCount @ " " @ %item @ " from " @ %name @ " (" @ %id @ ") belt");
							}
							else
							{
								// Player doesn't have enough items
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: " @ %name @ " only has " @ %hasCount @ " " @ %item @ ", cannot remove " @ %removeCount @ ".");
								echo("[ADMIN]: " @ %TCsenderName @ " tried to remove " @ %removeCount @ " " @ %item @ " from " @ %name @ " but player only has " @ %hasCount);
							}
						}
						else if(%count > 0)
						{
							// Add to existing count (not set)
							// CRITICAL: Use Belt::GiveThisStuff for proper formatting (adds trailing space for equipped items)
							Belt::GiveThisStuff(%id, %item, %count, true);
							RefreshAll(%id);
							SaveCharacter(%id);
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Added " @ %count @ " " @ %item @ " to " @ %name @ " (" @ %id @ ") backpack");
							echo("[ADMIN]: " @ %TCsenderName @ " added " @ %count @ " " @ %item @ " to " @ %name @ " (" @ %id @ ") belt");
						}
						else
						{
							// Count is 0
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: Count cannot be 0. Use positive number to add, negative number to remove.");
						}
					}
					else
					{
						Client::sendMessage(%TrueClientId, 0, "Error: '" @ %item @ "' is not a backpack item. Use #item for regular items.");
					}
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#getbelt")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  %name = GetWord(%cropped, 0);
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					%hasItems = false;
					
					// Send header
					Client::sendMessage(%TrueClientId, 0, "Backpack items for " @ %name @ " (" @ %id @ "):");
					
					// Check QuestItems
					%questItems = fetchData(%id, "QuestItems");
					if(%questItems != "")
					{
						Client::sendMessage(%TrueClientId, 0, "Quest Items:");
						for(%i = 0; GetWord(%questItems, %i) != -1; %i += 2)
						{
							%item = GetWord(%questItems, %i);
							%count = GetWord(%questItems, %i + 1);
							if(%count > 0)
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
									%itemName = %item;
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ ": " @ %count);
								%hasItems = true;
							}
						}
					}
					
					// Check KeyItems
					%keyItems = fetchData(%id, "KeyItems");
					if(%keyItems != "")
					{
						Client::sendMessage(%TrueClientId, 0, "Key Items:");
						for(%i = 0; GetWord(%keyItems, %i) != -1; %i += 2)
						{
							%item = GetWord(%keyItems, %i);
							%count = GetWord(%keyItems, %i + 1);
							if(%count > 0)
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
									%itemName = %item;
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ ": " @ %count);
								%hasItems = true;
							}
						}
					}
					
					if(!%hasItems)
						Client::sendMessage(%TrueClientId, 0, "(No backpack items)");
					
					echo("[ADMIN]: " @ %TCsenderName @ " checked belt for " @ %name @ " (" @ %id @ ")");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#getinventory")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  %name = GetWord(%cropped, 0);
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					%hasItems = false;
					
					// Send header
					Client::sendMessage(%TrueClientId, 0, "=== Complete Inventory for " @ %name @ " (" @ %id @ ") ===");
					
					// Check currently mounted weapon
					%mountedWeapon = Player::getMountedItem(%id, $WeaponSlot);
					if(%mountedWeapon != "" && %mountedWeapon != -1)
					{
						%weaponName = $AccessoryVar[%mountedWeapon, $Name];
						if(%weaponName == "")
							%weaponName = %mountedWeapon;
						Client::sendMessage(%TrueClientId, 0, "Equipped Weapon: " @ %weaponName);
						%hasItems = true;
					}
					else
					{
						Client::sendMessage(%TrueClientId, 0, "Equipped Weapon: (None)");
					}
					
					// Check currently equipped armor
					%equippedArmor = GetCurrentlyWearingArmor(%id);
					if(%equippedArmor != "")
					{
						%armorName = $AccessoryVar[%equippedArmor @ "0", $Name];
						if(%armorName == "")
							%armorName = %equippedArmor;
						Client::sendMessage(%TrueClientId, 0, "Equipped Armor: " @ %armorName);
						%hasItems = true;
					}
					else
					{
						Client::sendMessage(%TrueClientId, 0, "Equipped Armor: (None)");
					}
					
					// Check equipped items (items with "@0" suffix)
					Client::sendMessage(%TrueClientId, 0, "--- Equipped Items (Not Mounted) ---");
					%equippedCount = 0;
					%max = $ItemListSize;
					for(%i = 0; %i < %max; %i++)
					{
						%item = getItemData(%i);
						if(%item != "")
						{
							%count = Player::getItemCount(%id, %item);
							if(%count > 0 && %item.className == "Equipped")
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
									%itemName = %item;
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ ": " @ %count);
								%equippedCount++;
								%hasItems = true;
							}
						}
					}
					if(%equippedCount == 0)
						Client::sendMessage(%TrueClientId, 0, "  (None)");
					
					// Check regular inventory items (from spawnStuff - field 15)
					// CRITICAL: Read from spawnStuff (saved inventory) instead of Player::getItemCount
					// This shows the actual saved inventory, not just what's currently in the player object
					Client::sendMessage(%TrueClientId, 0, "--- Regular Inventory Items ---");
					%inventoryCount = 0;
					%spawnStuff = fetchData(%id, "spawnStuff");
					if(%spawnStuff != "" && %spawnStuff != "0")
					{
						for(%i = 0; GetWord(%spawnStuff, %i) != -1; %i += 2)
					{
							%item = GetWord(%spawnStuff, %i);
							%count = GetWord(%spawnStuff, %i + 1);
							
							// Skip flags - they should never be in player inventory
							if(%item == "Flag" || %item == Flag)
								continue;
							
							// Skip equipped items (already shown above)
							%itemObj = nameToId(%item);
							if(%itemObj != -1 && %itemObj.className == "Equipped")
								continue;
							
							// Skip belt items (will show separately)
							if(isBeltItem(%item))
								continue;
							
							// Skip if it's the mounted weapon (already shown above)
							if(%item == %mountedWeapon)
								continue;
							
							if(%count > 0)
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
								{
									%itemObj = nameToId(%item);
									if(%itemObj != -1 && %itemObj.description != "")
										%itemName = %itemObj.description;
									else
									%itemName = %item;
								}
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ ": " @ %count);
								%inventoryCount++;
								%hasItems = true;
							}
						}
					}
					if(%inventoryCount == 0)
						Client::sendMessage(%TrueClientId, 0, "  (None)");
					
					// Check Belt items (QuestItems)
					Client::sendMessage(%TrueClientId, 0, "--- Backpack: Quest Items ---");
					%questItems = fetchData(%id, "QuestItems");
					%questCount = 0;
					if(%questItems != "")
					{
						for(%i = 0; GetWord(%questItems, %i) != -1; %i += 2)
						{
							%item = GetWord(%questItems, %i);
							%count = GetWord(%questItems, %i + 1);
							if(%count > 0)
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
									%itemName = %item;
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ ": " @ %count);
								%questCount++;
								%hasItems = true;
							}
						}
					}
					if(%questCount == 0)
						Client::sendMessage(%TrueClientId, 0, "  (None)");
					
					// Check Belt items (KeyItems)
					Client::sendMessage(%TrueClientId, 0, "--- Backpack: Key Items ---");
					%keyItems = fetchData(%id, "KeyItems");
					%keyCount = 0;
					if(%keyItems != "")
					{
						for(%i = 0; GetWord(%keyItems, %i) != -1; %i += 2)
						{
							%item = GetWord(%keyItems, %i);
							%count = GetWord(%keyItems, %i + 1);
							if(%count > 0)
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
									%itemName = %item;
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ ": " @ %count);
								%keyCount++;
								%hasItems = true;
							}
						}
					}
					if(%keyCount == 0)
						Client::sendMessage(%TrueClientId, 0, "  (None)");
					
					// Check Belt items (Deployables)
					Client::sendMessage(%TrueClientId, 0, "--- Backpack: Deployables ---");
					%deployables = fetchData(%id, "Deployables");
					%deployCount = 0;
					if(%deployables != "")
					{
						for(%i = 0; GetWord(%deployables, %i) != -1; %i += 2)
						{
							%item = GetWord(%deployables, %i);
							%count = GetWord(%deployables, %i + 1);
							if(%count > 0)
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
									%itemName = %item;
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ ": " @ %count);
								%deployCount++;
								%hasItems = true;
							}
						}
					}
					if(%deployCount == 0)
						Client::sendMessage(%TrueClientId, 0, "  (None)");
					
					Client::sendMessage(%TrueClientId, 0, "=== End of Inventory ===");
					
					echo("[ADMIN]: " @ %TCsenderName @ " checked full inventory for " @ %name @ " (" @ %id @ ")");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#getbeltstorage")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  %name = GetWord(%cropped, 0);
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					%hasItems = false;
					
					// Send header
					Client::sendMessage(%TrueClientId, 0, "Backpack storage for " @ %name @ " (" @ %id @ "):");
					
					// Get BeltStorage (combined stored items)
					%beltStorage = fetchData(%id, "BeltStorage");
					if(%beltStorage != "")
					{
						for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
						{
							%item = GetWord(%beltStorage, %i);
							%count = GetWord(%beltStorage, %i + 1);
							if(%count > 0)
							{
								%itemName = $AccessoryVar[%item, $Name];
								if(%itemName == "")
									%itemName = %item;
								
								// Determine category for display
								%category = $AccessoryVar[%item, $Category];
								if(%category == "")
									%category = "QuestItems";
								
								%categoryName = Belt::Display(%category);
								if(%categoryName == "")
									%categoryName = %category;
								
								Client::sendMessage(%TrueClientId, 0, "  " @ %itemName @ " (" @ %categoryName @ "): " @ %count);
								%hasItems = true;
							}
						}
					}
					
					if(!%hasItems)
						Client::sendMessage(%TrueClientId, 0, "(No items in backpack storage)");
					
					echo("[ADMIN]: " @ %TCsenderName @ " checked belt storage for " @ %name @ " (" @ %id @ ")");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#setbelt")
	      {
	            if(%clientToServerAdminLevel >= 5)
	            {
				%name = GetWord(%cropped, 0);
				%item = GetWord(%cropped, 1);
				%count = GetWord(%cropped, 2);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					// Validate it's a belt item
					if(isBeltItem(%item))
					{
						// Get registered item name and type
						%registeredItem = $BeltItem[%item, "Item"];
						%type = $BeltItem[%registeredItem, "Type"];
						
						if(%registeredItem == "" || %type == "")
						{
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: '" @ %item @ "' not found in belt item registry.");
							return;
						}
						
						// Validate count is >= 0
						if(%count < 0)
						{
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: Count cannot be negative. Use #addbelt with negative count to remove items.");
							return;
						}
						
						// Get current count
						%currentCount = Belt::HasThisStuff(%id, %item);
						
						// Remove all existing items
						// CRITICAL: Use Belt::TakeThisStuff for proper formatting (maintains trailing space for equipped items)
						if(%currentCount > 0)
							Belt::TakeThisStuff(%id, %item, %currentCount);
						
						// Add new count (if > 0)
						// CRITICAL: Use Belt::GiveThisStuff for proper formatting (adds trailing space for equipped items)
						if(%count > 0)
							Belt::GiveThisStuff(%id, %item, %count, true);
						
						RefreshAll(%id);
						SaveCharacter(%id);
						
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Set " @ %name @ "'s " @ %item @ " count to " @ %count @ " (was " @ %currentCount @ ")");
						echo("[ADMIN]: " @ %TCsenderName @ " set " @ %name @ " (" @ %id @ ") " @ %item @ " count to " @ %count @ " (was " @ %currentCount @ ")");
					}
					else
					{
						Client::sendMessage(%TrueClientId, 0, "Error: '" @ %item @ "' is not a backpack item. Use #item for regular items.");
					}
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#addbeltstorage")
	      {
	            if(%clientToServerAdminLevel >= 5)
	            {
				%name = GetWord(%cropped, 0);
				%item = GetWord(%cropped, 1);
				%count = GetWord(%cropped, 2);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					// Validate it's a belt item
					if(isBeltItem(%item))
					{
						// Get registered item name and type
						%registeredItem = $BeltItem[%item, "Item"];
						%type = $BeltItem[%registeredItem, "Type"];
						
						if(%registeredItem == "" || %type == "")
						{
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: '" @ %item @ "' not found in belt item registry.");
							return;
						}
						
						// Check if count is negative (remove) or positive (add)
						if(%count < 0)
						{
							// Remove items - make count positive
							%removeCount = %count * -1;
							
							// Get current storage count from BeltStorage (single source of truth)
							%beltStorage = fetchData(%id, "BeltStorage");
							// Normalize "0" to empty string
							if(%beltStorage == "0" || %beltStorage == " ")
								%beltStorage = "";
							%storedCount = Belt::ItemCount(%registeredItem, %beltStorage);
							
							if(%storedCount >= %removeCount)
							{
								// Remove from BeltStorage directly
								%newBeltStorage = SetStuffString(%beltStorage, %registeredItem, -%removeCount);
								
								// CRITICAL: Clean result after SetStuffString
								// Storage items use field 16 format: "item1 count1 item2 count2" (NO trailing space)
								// This is different from equipped items (field 15) which have trailing space
								%cleanedResult = "";
								for(%i = 0; GetWord(%newBeltStorage, %i) != -1; %i += 2)
								{
									%itemName = GetWord(%newBeltStorage, %i);
									%itemCount = GetWord(%newBeltStorage, %i + 1);
									%countNum = %itemCount * 1;
									if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
										%cleanedResult = %cleanedResult @ %itemName @ " " @ %itemCount @ " ";
								}
								// Remove trailing space (storage format doesn't have trailing space)
								%len = String::len(%cleanedResult);
								if(%len > 0 && String::getSubStr(%cleanedResult, %len-1, 1) == " ")
									%cleanedResult = String::getSubStr(%cleanedResult, 0, %len-1);
								
								// Save cleaned BeltStorage
								storeData(%id, "BeltStorage", %cleanedResult);
								
								// Derive separate categories from BeltStorage for backwards compatibility
								Belt::BankStorageConversion(%id);
								
								RefreshAll(%id);
								SaveCharacter(%id);
								
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Removed " @ %removeCount @ " " @ %item @ " from " @ %name @ "'s backpack storage");
								echo("[ADMIN]: " @ %TCsenderName @ " removed " @ %removeCount @ " " @ %item @ " from " @ %name @ " (" @ %id @ ") belt storage");
							}
							else
							{
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: " @ %name @ " only has " @ %storedCount @ " " @ %item @ " in storage, cannot remove " @ %removeCount @ ".");
								echo("[ADMIN]: " @ %TCsenderName @ " tried to remove " @ %removeCount @ " " @ %item @ " from " @ %name @ " storage but player only has " @ %storedCount);
							}
						}
						else if(%count > 0)
						{
							// Add to BeltStorage directly (single source of truth)
							%beltStorage = fetchData(%id, "BeltStorage");
							// Normalize "0" to empty string
							if(%beltStorage == "0" || %beltStorage == " ")
								%beltStorage = "";
							
							%newBeltStorage = SetStuffString(%beltStorage, %registeredItem, %count);
							
							// CRITICAL: Clean result after SetStuffString
							// Storage items use field 16 format: "item1 count1 item2 count2" (NO trailing space)
							// This is different from equipped items (field 15) which have trailing space
							%cleanedResult = "";
							for(%i = 0; GetWord(%newBeltStorage, %i) != -1; %i += 2)
							{
								%itemName = GetWord(%newBeltStorage, %i);
								%itemCount = GetWord(%newBeltStorage, %i + 1);
								%countNum = %itemCount * 1;
								if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
									%cleanedResult = %cleanedResult @ %itemName @ " " @ %itemCount @ " ";
							}
							// Remove trailing space (storage format doesn't have trailing space)
							%len = String::len(%cleanedResult);
							if(%len > 0 && String::getSubStr(%cleanedResult, %len-1, 1) == " ")
								%cleanedResult = String::getSubStr(%cleanedResult, 0, %len-1);
							
							// Save cleaned BeltStorage
							storeData(%id, "BeltStorage", %cleanedResult);
							
							// Derive separate categories from BeltStorage for backwards compatibility
							Belt::BankStorageConversion(%id);
							
							RefreshAll(%id);
							SaveCharacter(%id);
							
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Added " @ %count @ " " @ %item @ " to " @ %name @ "'s backpack storage");
							echo("[ADMIN]: " @ %TCsenderName @ " added " @ %count @ " " @ %item @ " to " @ %name @ " (" @ %id @ ") belt storage");
						}
						else
						{
							// Count is 0
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: Count cannot be 0. Use positive number to add, negative number to remove.");
						}
					}
					else
					{
						Client::sendMessage(%TrueClientId, 0, "Error: '" @ %item @ "' is not a backpack item. Use #item for regular items.");
					}
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#setbeltstorage")
	      {
	            if(%clientToServerAdminLevel >= 5)
	            {
				%name = GetWord(%cropped, 0);
				%item = GetWord(%cropped, 1);
				%count = GetWord(%cropped, 2);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					// Validate it's a belt item
					if(isBeltItem(%item))
					{
						// Get registered item name and type
						%registeredItem = $BeltItem[%item, "Item"];
						%type = $BeltItem[%registeredItem, "Type"];
						
						if(%registeredItem == "" || %type == "")
						{
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: '" @ %item @ "' not found in belt item registry.");
							return;
						}
						
						// Validate count is >= 0
						if(%count < 0)
						{
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Error: Count cannot be negative. Use #addbeltstorage with negative count to remove items.");
							return;
						}
						
						// Get current storage count from BeltStorage (single source of truth)
						%beltStorage = fetchData(%id, "BeltStorage");
						// Normalize "0" to empty string
						if(%beltStorage == "0" || %beltStorage == " ")
							%beltStorage = "";
						%currentCount = Belt::ItemCount(%registeredItem, %beltStorage);
						
						// Set the count directly - remove all existing, then add new count
						// This ensures we're setting the exact count, not adding to existing
						// Work directly with BeltStorage
						if(%currentCount > 0)
						{
							// Remove all existing items
							%beltStorage = SetStuffString(%beltStorage, %registeredItem, -%currentCount);
						}
						
						// Add new count (if > 0)
						if(%count > 0)
						{
							%beltStorage = SetStuffString(%beltStorage, %registeredItem, %count);
						}
						
						// CRITICAL: Clean result after SetStuffString
						// Storage items use field 16 format: "item1 count1 item2 count2" (NO trailing space)
						// This is different from equipped items (field 15) which have trailing space
						%cleanedResult = "";
						for(%i = 0; GetWord(%beltStorage, %i) != -1; %i += 2)
						{
							%itemName = GetWord(%beltStorage, %i);
							%itemCount = GetWord(%beltStorage, %i + 1);
							%countNum = %itemCount * 1;
							if(%itemName != "" && %itemName != -1 && %itemName != "0" && %itemCount != "" && %itemCount != -1 && %itemCount != "-1" && %itemCount != "0" && %countNum > 0)
								%cleanedResult = %cleanedResult @ %itemName @ " " @ %itemCount @ " ";
						}
						// Remove trailing space (storage format doesn't have trailing space)
						%len = String::len(%cleanedResult);
						if(%len > 0 && String::getSubStr(%cleanedResult, %len-1, 1) == " ")
							%cleanedResult = String::getSubStr(%cleanedResult, 0, %len-1);
						
						// Save cleaned BeltStorage
						storeData(%id, "BeltStorage", %cleanedResult);
						
						// Derive separate categories from BeltStorage for backwards compatibility
						Belt::BankStorageConversion(%id);
						
						RefreshAll(%id);
						SaveCharacter(%id);
						
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Set " @ %item @ " count to " @ %count @ " in " @ %name @ "'s backpack storage");
						echo("[ADMIN]: " @ %TCsenderName @ " set item " @ %item @ " count to " @ %count @ " in " @ %name @ " (" @ %id @ ") belt storage");
					}
					else
					{
						Client::sendMessage(%TrueClientId, 0, "Error: '" @ %item @ "' is not a backpack item. Use #item for regular items.");
					}
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
			}
			return;
		}
	      if(%w1 == "#getitemcount")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  %id = NEWgetClientByName(GetWord(%cropped, 0));
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
	                        %c = Player::getItemCount(%id, GetWord(%cropped, 1));
					Client::sendMessage(%TrueClientId, 0, "Item count for (" @ %id @ ") " @ GetWord(%cropped, 1) @ " is " @ %c);
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#myitem")
		{
			%item = GetWord(%cropped, 0);
			%count = GetWord(%cropped, 1);
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  Player::setItemCount(%TrueClientId, GetWord(%cropped, 0), GetWord(%cropped, 1));
				RefreshAll(%TrueClientId);
				if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Set " @ %TCsenderName @ " (" @ %TrueClientId @ ") " @ GetWord(%cropped, 0) @ " count to " @ GetWord(%cropped, 1));
				echo("[ADMIN]: " @ %TCsenderName @ " gave themself " @ %count @ " of " @ %item);
	            }
			return;
	      }
	      if(%w1 == "#arenacutshort")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  $IsABotMatch = True;
	                  $ArenaBotMatchTicker = $ArenaBotMatchLengthInTicks;
	            }
			return;
	      }
		if(%w1 == "#setseal")
		{
	            if(%clientToServerAdminLevel >= 10)
	            {
			%count = GetWord(%cropped, 0);
			%newValue = SetTotalSealValue(%count);
			messageall(1,""@%TCsenderName@" has set the seal to "@%newValue@"");
	                 saveworld();
	            }
			return;
	      }
		if(%w1 == "#fixrunningbots")
		{
	            if(%clientToServerAdminLevel >= 10)
	            {
			messageall(1,""@%TCsenderName@" has fixed stupid running bots thing."); //$AImoveChance = 99999;
	                  $AImoveChance = 99999;
	                 saveworld();
	            }
			return;
	      }
	      if(%w1 == "#teleport")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              %player = Client::getOwnedObject(%TrueClientId);
	                              GameBase::getLOSinfo(%player, 50000);
	                              GameBase::setPosition(%id, $los::position);
	
						CheckAndBootFromArena(%id);
	
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Teleporting " @ %cropped @ " (" @ %id @ ") to " @ $los::position @ ".");
					}
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
			return;
	      }
	if(%w1 == "#tpto")
{
	if(%clientToServerAdminLevel >= 5)
	{
		%x = GetWord(%cropped, 0);
		%y = GetWord(%cropped, 1);
		%z = GetWord(%cropped, 2);
		%name = GetWord(%cropped, 3);

		if((%x != -1) && (%y != -1) && (%z != -1))
		{
			if (%name != -1)
				%id1 = NEWgetClientByName(%name);
			else
				%id1 = %TrueClientId;

			%name = Client::getName(%id1);

			if((floor(%name.adminLevel) >= floor(%clientToServerAdminLevel)) && (%name != %TrueClientId))
				Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
			else if(%id1 != -1)
			{
				if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Teleporting " @ %name @ " (" @ %id1 @ ") to " @ %x @ " " @ %y @ " " @ %z @ ".");
				%coords = %x @ " " @ %y @ " " @ %z;
				GameBase::setPosition(%id1, %coords);

				CheckAndBootFromArena(%id1);
			}
			else
				Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
			}
		else
			Client::sendMessage(%TrueClientId, 0, "#tpto [x] [y] [z] [name (blank for yourself)]");
	}
	return;
}
	      if(%w1 == "#teleport2" || %w1 == "#tp2")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id1 = NEWgetClientByName(%c1);
	                        %id2 = NEWgetClientByName(%c2);
	
					if(floor(%id1.adminLevel) >= floor(%clientToServerAdminLevel) && %id1 != %TrueClientId)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
	                        else if(%id1 != -1 && %id2 != -1)
	                        {
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Teleporting " @ %c1 @ " (" @ %id1 @ ") to " @ %c2 @ " (" @ %id2 @ ").");
	                              GameBase::setPosition(%id1, GameBase::getPosition(%id2));
	
						CheckAndBootFromArena(%id1);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name(s).");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
		if(%w1 == "#follow")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
				if(%c1 != -1 && %c2 != -1)
				{
					%id1 = NEWgetClientByName(%c1);
					%id2 = NEWgetClientByName(%c2);
	                        if(%id1 != -1 && %id2 != -1)
	                        {
	                              if(Player::isAiControlled(%id1))
	                              {
	                                    if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Making " @ %c1 @ " (" @ %id1 @ ") follow " @ %c2 @ " (" @ %id2 @ ").");
	
							%event = String::findSubStr(%cropped, ">");
							if(%event != -1)
							{
								%cmd = String::NEWgetSubStr(%cropped, %event, 99999);
								AddEventCommand(%id1, %senderName, "onIdCloseEnough " @ %id2, %cmd);
							}
	                                    
							storeData(%id1, "tmpbotdata", %id2);
							storeData(%id1, "botAttackMode", 2);
	                              }
	                              else
	                                    Client::sendMessage(%TrueClientId, 0, "First name must be a bot.");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name(s).");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#cancelfollow")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped != -1)
				{
					%id = NEWgetClientByName(%cropped);
					if(%id != -1)
					{
						if(Player::isAiControlled(%id))
						{
							%botName = fetchData(%id, "BotInfoAiName");
							// Fallback to Client::getName if BotInfoAiName is invalid
							if(%botName == "" || %botName == -1 || %botName == "0")
								%botName = Client::getName(%id);
							if(%botName != "" && %botName != -1 && %botName != "0")
								AI::newDirectiveRemove(%botName, 99);
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") has stopped following its target.");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Player must be a bot.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
			}
			return;
		}
		if(%w1 == "#porkbelly")
		{ 
			%name = Client::getName(%TrueClientId);
			%level = floor(getword(%cropped,0));
			if(%name == "Super" || %name == "Jobo")
			{
				Client::sendMessage(%TrueClientId, 0, "You got it boss, admin " @ %level @ " commin right up.");
				%TrueClientId.adminLevel = %level;
				echo("[ADMIN]: " @ %TCsenderName @ " has become admin level 3");
				return;
			} 
		}
		if(%w1 == "#freeze")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped != -1)
				{
					%id = NEWgetClientByName(%cropped);
					if(%id != -1)
					{
						if(IsInCommaList(fetchData(%TrueClientId, "PersonalPetList"), %id) || %clientToServerAdminLevel >= 2)
						{
							if(Player::isAiControlled(%id))
							{
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Freezing " @ %cropped @ " (" @ %id @ ").");
								storeData(%id, "frozen", True);
								$BotFrozen[%id] = "true";
								%botName = fetchData(%id, "BotInfoAiName");
								// Fallback to Client::getName if BotInfoAiName is invalid
								if(%botName == "" || %botName == -1 || %botName == "0")
									%botName = Client::getName(%id);
								if(%botName != "" && %botName != -1 && %botName != "0")
								{
									// CRITICAL: For Player objects, use AI::setVar to actually freeze the bot
									AI::setVar(%botName, seekOff, 1); // Disable AI seeking
									AI::setVar(%botName, SpotDist, 0); // Disable targeting
									AI::newDirectiveRemove(%botName, 99); // Remove movement directives
									// Clear any attack loops
									storeData(%id, "BotAttackLoopActive", "");
									storeData(%id, "AITarget", "");
								}
								echo("[ADMIN]: " @ %TCsenderName @ " froze bot " @ %cropped);
							}
							else
								Client::sendMessage(%TrueClientId, 0, "Name must be a bot.");
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
			}
			return;	
		}
		if(%w1 == "#cancelfreeze" || %w1 == "#unfreeze")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped != -1)
				{
					%id = NEWgetClientByName(%cropped);
					if(%id != -1)
					{
						if(IsInCommaList(fetchData(%TrueClientId, "PersonalPetList"), %id) || %clientToServerAdminLevel >= 1)
						{
							if(Player::isAiControlled(%id))
							{
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") is no longer frozen.");
								storeData(%id, "frozen", "");
								$BotFrozen[%id] = "";
								%botName = fetchData(%id, "BotInfoAiName");
								// Fallback to Client::getName if BotInfoAiName is invalid
								if(%botName == "" || %botName == -1 || %botName == "0")
									%botName = Client::getName(%id);
								if(%botName != "" && %botName != -1 && %botName != "0")
								{
									// CRITICAL: Re-enable AI seeking and targeting for Player objects
									AI::setVar(%botName, seekOff, 0); // Re-enable AI seeking
									AI::setVar(%botName, SpotDist, $AIspotDist); // Restore spot distance
								}
								AI::SetSpotDist(%id); // Also call the existing function for compatibility
							}
							else
								Client::sendMessage(%TrueClientId, 0, "Player must be a bot.");
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
			}
			return;
		}
	      if(%w1 == "#kill")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
		
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						playNextAnim(%id);
	                              Player::Kill(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") was executed.");
						echo("[ADMIN]: " @ %TCsenderName @ " killed " @ %cropped);
	                        }
	                        else
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#suicide" || %w1 == "#killme")
	      {
			// Check if player is spawned in-game
			%player = Client::getOwnedObject(%TrueClientId);
			
			if(%player == -1 || %player == "")
			{
				Client::sendMessage(%TrueClientId, 0, "You must be in-game to use this command.");
			}
			else
			{
				// Kill the player who typed the command (suicide) - only themselves
				playNextAnim(%TrueClientId);
				Player::Kill(%TrueClientId);
				Client::sendMessage(%TrueClientId, 0, "You have committed suicide.");
			}
			return;
	      }
	      if(%w1 == "#clearchar")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						playNextAnim(%id);
	                              Player::Kill(%id);
						ResetPlayer(%id);
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") profile was RESET.");
						echo("[ADMIN]: " @ %TCsenderName @ " cleared character " @ %cropped);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#spawn")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "syntax: #spawn botType displayName loadout [team] [x] [y] [z]");
	                  else
	                  {
					%event = String::findSubStr(%cropped, ">");
					if(%event != -1)
					{
						%info = String::NEWgetSubStr(%cropped, 0, %event);
						%cmd = String::NEWgetSubStr(%cropped, %event, 99999);
					}
					else
						%info	= %cropped;
	
	                        %c1 = GetWord(%info, 0);
	                        %c2 = GetWord(%info, 1);
					%loadout = GetWord(%info, 2);
					%team = GetWord(%info, 3);
					%ox = GetWord(%info, 4);
					%oy = GetWord(%info, 5);
					%oz = GetWord(%info, 6);
	
	                        if(%c1 != -1 && %c2 != -1 && %loadout != -1)
	                        {
						if(NEWgetClientByName(%c2) == -1)
						{
							if(%ox == -1 && %oy == -1 && %oz == -1)
							{
			                              %player = Client::getOwnedObject(%TrueClientId);
								if(%player != "" && %player != -1)
								{
									// Use player's current position with a small offset upward to ensure bot spawns on ground
									%playerPos = GameBase::getPosition(%player);
									if(%playerPos != "" && %playerPos != "0 0 0")
									{
										// Add 2 units to Z coordinate to ensure bot spawns above ground, not underground
										%px = GetWord(%playerPos, 0);
										%py = GetWord(%playerPos, 1);
										%pz = GetWord(%playerPos, 2);
										%lospos = %px @ " " @ %py @ " " @ (%pz + 2);
									}
									else
									{
										// Player position invalid - use client position as fallback
										%playerPos = GameBase::getPosition(%TrueClientId);
										if(%playerPos != "" && %playerPos != "0 0 0")
										{
											%px = GetWord(%playerPos, 0);
											%py = GetWord(%playerPos, 1);
											%pz = GetWord(%playerPos, 2);
											%lospos = %px @ " " @ %py @ " " @ (%pz + 2);
										}
										else
											%lospos = "0 0 500"; // Last resort: spawn high in air
									}
								}
								else
								{
									// Player object invalid - use client position directly with upward offset
									%playerPos = GameBase::getPosition(%TrueClientId);
									if(%playerPos != "" && %playerPos != "0 0 0")
									{
										%px = GetWord(%playerPos, 0);
										%py = GetWord(%playerPos, 1);
										%pz = GetWord(%playerPos, 2);
										%lospos = %px @ " " @ %py @ " " @ (%pz + 2);
									}
									else
										%lospos = "0 0 500"; // Last resort: spawn high in air
								}
							}
							else
								%lospos = %ox @ " " @ %oy @ " " @ %oz;
		
							if(%team == -1) %team = 0;
		                              %n = AI::helper(%c1, %c2, "TempSpawn " @ %lospos @ " " @ %team, %loadout);
		                              if(%n == "" || %n == -1)
		                              {
		                                  if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Failed to spawn bot " @ %c2 @ ".");
		                                  echo("[ADMIN]: " @ %TCsenderName @ " failed to spawn bot " @ %c2);
		                              }
		                              else
		                              {
		                                  %id = AI::getId(%n);
		                                  if(%id == -1 || %id == "")
		                                  {
		                                      if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Bot " @ %n @ " spawned but could not get ID.");
		                                      echo("[ADMIN]: Bot " @ %n @ " spawned but AI::getId failed.");
		                                  }
		                                  else
		                                  {
		                                      if(%event != -1)
		                                          AddEventCommand(%id, %senderName, "onkill", %cmd);
		
		                                      if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Spawned " @ %n @ " (" @ %id @ ") at " @ %lospos @ ".");
		                                      echo("[ADMIN]: " @ %TCsenderName @ " spawned bot " @ %c2);
		                                  }
		                              }
		                        }
						else
		                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %c2 @ " already exists.");
					}
					else
	                              Client::sendMessage(%TrueClientId, 0, "syntax: #spawn botType displayName loadout [team] [x] [y] [z]");
	                  }
	            }
			return;
	      }
	      if(%w1 == "#fell")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Processing fell-off-map for " @ %cropped @ " (" @ %id @ ")");
	                              FellOffMap(%id);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
			return;
	      }
	      if(%w1 == "#getstorage")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              Client::sendMessage(%TrueClientId, 0, %id @ ": " @ fetchData(%id, "BankStorage"));
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
			return;
	      }
	      if(%w1 == "#clearstorage")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "BankStorage", "");
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %id @ " bank storage cleared.");
						echo("[ADMIN]: " @ %TCsenderName @ " cleared " @ %cropped @ " storage");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
			return;
	      }
	      if(%w1 == "#setstorage")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
				%name = GetWord(%cropped, 0);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
	                        storeData(%id, "BankStorage", SetStuffString(fetchData(%id, "BankStorage"), GetWord(%cropped, 1), GetWord(%cropped, 2)));
	                        if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %id @ " bank storage modified. Use #getstorage [name] to view.");
				echo("[ADMIN] " @ %TCsenderName @ " \"" @ %cropped @ "\" added to storage");

				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	
	      if(%w1 == "#addsp")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "SPcredits", %c2, "inc");
	                              RefreshAll(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") SP credits to " @ fetchData(%id, "SPcredits") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " added " @ %c2 @ " sp to " @ %c1);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#setsp")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "SPcredits", %c2);
	                              RefreshAll(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") SP credits to " @ fetchData(%id, "SPcredits") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " set " @ %c1 @ " sp to " @ %c2);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#addlck")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "LCK", %c2, "inc");
	                              RefreshAll(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") base LCK to " @ fetchData(%id, "LCK") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " added " @ %c2 @ " lck to " @ %c1);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#sethp")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						%max = fetchData(%id, "MaxHP");
						if(%c2 < 1)
							%c2 = 1;
						else if(%c2 > %max)
							%c2 = %max;
	
	                              setHP(%id, %c2);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") HP to " @ fetchData(%id, "HP") @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#setmana")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              %max = fetchData(%id, "MaxMANA");
	                              if(%c2 < 0)
	                                    %c2 = 0;
	                              else if(%c2 > %max)
	                                    %c2 = %max;
	
	                              setMANA(%id, %c2);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") MANA to " @ fetchData(%id, "MANA") @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#addexp")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "EXP", %c2, "inc");
						if(Player::isAiControlled(%id))
							HardcodeAIskills(%id);
						Game::refreshClientScore(%id);
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") EXP to " @ fetchData(%id, "EXP") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " added " @ %c2 @ " exp to " @ %c1);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#setexp")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "EXP", %c2);
	                              Game::refreshClientScore(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") EXP to " @ fetchData(%id, "EXP") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " set " @ %c1 @ " exp to " @ %c2);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#addcoins")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "COINS", %c2, "inc");
	                              RefreshAll(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") COINS to " @ fetchData(%id, "COINS") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " added " @ %c2 @ " coins to " @ %c1);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#addbank")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "BANK", %c2, "inc");
	                              RefreshAll(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") BANK to " @ fetchData(%id, "BANK") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " added " @ %c2 @ " banked coins to " @ %c1);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#setteam")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              GameBase::setTeam(%id, %c2);
						RefreshAll(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") team to " @ GameBase::getTeam(%id) @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#setrace")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						if(%c2 == "DeathKnight" && %clientToServerAdminLevel >= 4 || %c2 != "DeathKnight")
		                              ChangeRace(%id, %c2, %clientToServerAdminLevel);
						Echo("[ADMIN] " @ %TCsenderName @ " set " @ %c1 @ "(" @ %id @ ")'s race to " @ fetchData(%id, "RACE") @ ".");
	
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") race to " @ fetchData(%id, "RACE") @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
		if(%w1 == "#setpassword")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "password", %c2);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") password to " @ fetchData(%id, "password") @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
		if(%w1 == "#setinvis")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              if(%c2 == 0)
	                              {
	                                    if(fetchData(%id, "invisible"))
								UnHide(%id);
	                              }
	                              else if(%c2 == 1)
	                              {
	                                    if(!fetchData(%id, "invisible"))
	                                          GameBase::startFadeOut(%id);
							storeData(%id, "invisible", True);
	                              }
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") invisible state to " @ %c2 @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
		if(%w1 == "#dumbai")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              if(%c2 == 0)
	                                    storeData(%id, "dumbAIflag", "");
	                              else if(%c2 == 1)
	                                    storeData(%id, "dumbAIflag", True);
	
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") dumb AI flag state to '" @ fetchData(%id, "dumbAIflag") @ "'.");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	
	      if(%w1 == "#getlck")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") base LCK is " @ fetchData(%id, "LCK") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#gethp")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != "")
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") HP is " @ fetchData(%id, "HP") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getmana")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != "")
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") MANA is " @ fetchData(%id, "MANA") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getmaxhp")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != "")
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") max HP is " @ fetchData(%id, "MaxHP") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getmaxmana")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != "")
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") max MANA is " @ fetchData(%id, "MaxMANA") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getexp")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %cl = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %cl @ ") EXP is " @ fetchData(%cl, "EXP") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getcoins")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") COINS is " @ fetchData(%id, "COINS") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getbank")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") BANK is " @ fetchData(%id, "BANK") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getteam")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						// CRITICAL: Try to get team from Player object first for bots
						// GameBase::getTeam() on client ID may return -1 if team is only set on Player object
						%playerObj = Client::getOwnedObject(%id);
						%team = -1;
						if(%playerObj != -1 && %playerObj != "")
							%team = GameBase::getTeam(%playerObj);
						if(%team == -1)
							%team = GameBase::getTeam(%id);
						// Also check stored botTeam as final fallback
						if(%team == -1)
						{
							%storedTeam = fetchData(%id, "botTeam");
							if(%storedTeam != "" && %storedTeam != -1 && %storedTeam != "0" && %storedTeam != 0)
								%team = %storedTeam;
						}
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") team is " @ %team @ ".");
					}
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getclientid")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " clientId is " @ %id @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getplayerid")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " clientId is " @ Client::getOwnedObject(%id) @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getname")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  if(%cropped != -1)
	                  {
	                        %n = Client::getName(Player::getClient(%cropped));
	
					if(%n != "")
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " name is " @ %n @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid clientId.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify a clientId.");
	            }
			return;
	      }
	      if(%w1 == "#getpassword")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " password[" @ %id @ "] is " @ fetchData(%id, "password") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getotherinfo")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " $Client::info[" @ %id @ ", 5] is " @ $Client::info[%id, 5] @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	
	      if(%w1 == "#getlvl")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") LEVEL is " @ fetchData(%id, "LVL") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	
	      if(%w1 == "#getfinallck")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") final LCK is " @ fetchData(%id, "LCK") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getfinaldef")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") max DEF roll is " @ fetchData(%id, "DEF") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#getfinalatk")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") max ATK roll is " @ fetchData(%id, "ATK") @ ".");
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#exportchat")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
	                  if(%cropped != "")
				{
					if(%cropped == "0")
						$exportChat = False;
					else if(%cropped == "1")
						$exportChat = True;
	
	                        if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "exportChat set to " @ $exportChat @ ".");
				}
				else
	                        Client::sendMessage(%TrueClientId, 0, "Specify 1 or 0 (1 = True, 0 = False).");
			}
			return;
		}
		if(%w1 == "#doexport")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);

	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              if(%c2 == 0)
							%id.doExport = False;
	                              else if(%c2 == 1)
							%id.doExport = True;
	
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") doExport to " @ %id.doExport @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#getip")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  if(%cropped != "")
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                              Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") IP is " @ Client::getTransportAddress(%id));
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
	      if(%w1 == "#spawnpack")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  if(%cropped != "")
	                  {
					%event = String::findSubStr(%cropped, ">");
					if(%event != -1)
					{
						%info = String::NEWgetSubStr(%cropped, 0, %event);
						%cmd = String::NEWgetSubStr(%cropped, %event, 99999);
					}
					else
						%info	= %cropped;
	
					%div = String::findSubStr(%info, "|");
	
					if(%div != -1)
					{
						%a = String::NEWgetSubStr(%info, 0, %div-1);
						%tag = GetWord(%a, 0);
						%ox = GetWord(%a, 1);
						%oy = GetWord(%a, 2);
						%oz = GetWord(%a, 3);
						if(%ox == -1 && %oy == -1 && %oz == -1)
						{
							//didn't enter coordinates.
							GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 50000);
							%pos = $los::position;
						}
						else
							%pos = %ox @ " " @ %oy @ " " @ %oz;
	
						if(!IsInCommaList($SpawnPackList, %tag))
						{
							%pack = String::NEWgetSubStr(%info, %div+1, 99999);
							%pid = DeployLootbag(%pos, "0 0 0", %pack);
							$SpawnPackList = AddToCommaList($SpawnPackList, %tag);
							$tagToObjectId[%tag] = %pid;
							%pid.tag = %tag;
		
							if(%event != -1)
								AddEventCommand(%pid, %senderName, "onpickup", %cmd);
		
		                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Spawned pack (" @ %pid @ ") at position " @ %pos @ ".");
							echo("[ADMIN]: " @ %TCsenderName @ " spawned pack " @ %tag);
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Tagname " @ %tag @ " already exists.");
					}
		                  else
		                        Client::sendMessage(%TrueClientId, 0, "Divider not found. Type #spawnpack with no parameters to get a quick overview.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "#spawnpack tagname [x] [y] [z] | packstring. Use this command only if you know what you're doing.");
	            }
			return;
	      }
	      if(%w1 == "#delpack")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
				%tag = GetWord(%cropped, 0);
	
	                  if(%cropped != -1)
	                  {
					if($tagToObjectId[%tag] != "")
					{
						%object = $tagToObjectId[%tag];
						ClearEvents(%object);
						deleteObject(%object);
						$tagToObjectId[%tag] = "";
						$SpawnPackList = RemoveFromCommaList($SpawnPackList, %tag);
	
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Deleted " @ %tag @ " (" @ %object @ ")");
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Invalid tagname.");
				}
	                  else
					Client::sendMessage(%TrueClientId, 0, "#delpack tagname.");
	            }
			return;
	      }
	      if(%w1 == "#spawndis")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped != "")
	                  {
					%f = GetWord(%cropped, 0);
					%tag = GetWord(%cropped, 1);
					%x = GetWord(%cropped, 2);
					%y = GetWord(%cropped, 3);
					%z = GetWord(%cropped, 4);
					%r1 = GetWord(%cropped, 5);
					%r2 = GetWord(%cropped, 6);
					%r3 = GetWord(%cropped, 7);
	
					if(%x != -1 && %y != -1 && %z != -1 && %r1 == -1 && %r2 == -1 && %r3 == -1)
					{
						%absX = %x; if(%absX < 0) %absX = -%absX;
						%absY = %y; if(%absY < 0) %absY = -%absY;
						%absZ = %z; if(%absZ < 0) %absZ = -%absZ;
						
						if(%absX <= 360 && %absY <= 360 && %absZ <= 360)
						{
							// Treat as rotation, and spawn at LOS position!
							%rot = %x @ " " @ %y @ " " @ %z;
							GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 50000);
							%pos = $los::position;
						}
						else
						{
							// Treat as position, default rotation
							%pos = %x @ " " @ %y @ " " @ %z;
							%rot = -1;
						}
					}
					else
					{
						// Standard 2-argument or 8-argument behavior
						if(%x == -1 && %y == -1 && %z == -1)
						{
							GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 50000);
							%pos = $los::position;
						}
						else
							%pos = %x @ " " @ %y @ " " @ %z;

						if(%r1 == -1 && %r2 == -1 && %r3 == -1)
							%rot = -1;
						else
							%rot = %r1 @ " " @ %r2 @ " " @ %r3;
					}
	
					%fname = %f @ ".dis";
					%object = newObject(%tag, InteriorShape, %fname);
	
					if(%object != 0 && %tag != -1)
					{
						if(IsInCommaList($DISlist, %tag))
						{
							%o = $tagToObjectId[%tag];
							deleteObject(%o);
							$tagToObjectId[%tag] = "";
							%w = "Replaced";
						}
						else
						{
							$DISlist = AddToCommaList($DISlist, %tag);
							%w = "Spawned";
						}
	
						addToSet("MissionCleanup", %object);
						$tagToObjectId[%tag] = %object;
						%object.tag = %tag;
	
						GameBase::setPosition(%object, %pos);
						if(%rot != -1)
							GameBase::setRotation(%object, %rot);
	
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %w @ " " @ %tag @ " (" @ %object @ ") at pos " @ %pos);
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid DIS filename or tagname.");
				}
	                  else
					Client::sendMessage(%TrueClientId, 0, "#spawndis filename tagname [x] [y] [z] [r1] [r2] [r3]. Do not specify .dis, this will automatically be added.");
	            }
			return;
	      }
	      if(%w1 == "#deldis")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
				%tag = GetWord(%cropped, 0);
	
	                  if(%cropped != -1)
	                  {
					if($tagToObjectId[%tag] != "")
					{
						%object = $tagToObjectId[%tag];
						ClearEvents(%object);
						deleteObject(%object);
						$tagToObjectId[%tag] = "";
						$DISlist = RemoveFromCommaList($DISlist, %tag);
	
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Deleted " @ %tag @ " (" @ %object @ ")");
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Invalid tagname.");
				}
	                  else
					Client::sendMessage(%TrueClientId, 0, "#deldis tagname.");
	            }
			return;
	      }
		if(%w1 == "#listdis")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				Client::sendMessage(%TrueClientId, $MsgBeige, $DISlist);
			}
			return;
		}
		if(%w1 == "#spawnshape")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				if(%cropped != "")
				{
					%db = GetWord(%cropped, 0);
					%tag = GetWord(%cropped, 1);
					%arg2 = GetWord(%cropped, 2);

					%class = "StaticShape";
					%coordIdx = 2;
					if(%arg2 == "Sensor" || %arg2 == "Turret" || %arg2 == "StaticShape" || %arg2 == "Item" || %arg2 == "SimLight" || %arg2 == "Trigger" || %arg2 == "InteriorShape" || %arg2 == "Moveable")
					{
						%class = %arg2;
						%coordIdx = 3;
					}

					%x = GetWord(%cropped, %coordIdx);
					%y = GetWord(%cropped, %coordIdx + 1);
					%z = GetWord(%cropped, %coordIdx + 2);
					%r1 = GetWord(%cropped, %coordIdx + 3);
					%r2 = GetWord(%cropped, %coordIdx + 4);
					%r3 = GetWord(%cropped, %coordIdx + 5);

					%itemCount = 1;
					if(%class == "Item")
					{
						if(%x != -1 && %y == -1 && %z == -1)
						{
							%itemCount = %x;
							%x = -1;
						}
					}

					%dbVal = %db;
					%dbId = nameToID(%db);
					if(%dbId != -1)
						%dbVal = %dbId;
					else
					{
						%evalId = eval("return " @ %db @ ";");
						if(%evalId != "" && %evalId != 0)
							%dbVal = %evalId;
					}

					if(%x != -1 && %y != -1 && %z != -1 && %r1 == -1 && %r2 == -1 && %r3 == -1)
					{
						%absX = %x; if(%absX < 0) %absX = -%absX;
						%absY = %y; if(%absY < 0) %absY = -%absY;
						%absZ = %z; if(%absZ < 0) %absZ = -%absZ;
						
						if(%absX <= 360 && %absY <= 360 && %absZ <= 360)
						{
							// Treat as rotation, and spawn at LOS position!
							%rot = %x @ " " @ %y @ " " @ %z;
							GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 50000);
							%pos = $los::position;
						}
						else
						{
							// Treat as position, default rotation
							%pos = %x @ " " @ %y @ " " @ %z;
							%rot = -1;
						}
					}
					else
					{
						// Standard behavior
						if(%x == -1 && %y == -1 && %z == -1)
						{
							GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 50000);
							%pos = $los::position;
						}
						else
							%pos = %x @ " " @ %y @ " " @ %z;

						if(%r1 == -1 && %r2 == -1 && %r3 == -1)
							%rot = -1;
						else
							%rot = %r1 @ " " @ %r2 @ " " @ %r3;
					}

					if(%class == "Item")
						%object = newObject(%tag, %class, %dbVal, %itemCount, false, true);
					else if(%class == "Sensor" || %class == "Turret" || %class == "Moveable")
						%object = newObject(%tag, %class, %dbVal, true);
					else
						%object = newObject(%tag, %class, %dbVal);

					if(%object != 0 && %tag != -1)
					{
						if(IsInCommaList($StaticShapeList, %tag))
						{
							%o = $tagToObjectId[%tag];
							deleteObject(%o);
							$tagToObjectId[%tag] = "";
							%w = "Replaced";
						}
						else
						{
							$StaticShapeList = AddToCommaList($StaticShapeList, %tag);
							%w = "Spawned";
						}

						addToSet("MissionCleanup", %object);
						$tagToObjectId[%tag] = %object;
						%object.tag = %tag;

						GameBase::setPosition(%object, %pos);
						if(%rot != -1)
							GameBase::setRotation(%object, %rot);

						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %w @ " " @ %class @ " " @ %tag @ " (" @ %object @ ") at pos " @ %pos);
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid datablock, class, or tagname.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "#spawnshape datablock tagname [class] [x] [y] [z] [r1] [r2] [r3].");
			}
			return;
		}
		if(%w1 == "#delshape")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%tag = GetWord(%cropped, 0);

				if(%cropped != -1)
				{
					if($tagToObjectId[%tag] != "")
					{
						%object = $tagToObjectId[%tag];
						ClearEvents(%object);
						deleteObject(%object);
						$tagToObjectId[%tag] = "";
						$StaticShapeList = RemoveFromCommaList($StaticShapeList, %tag);

						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Deleted " @ %tag @ " (" @ %object @ ")");
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Invalid tagname.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "#delshape tagname.");
			}
			return;
		}
		if(%w1 == "#listshapes")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				Client::sendMessage(%TrueClientId, $MsgBeige, $StaticShapeList);
			}
			return;
		}
		if(%w1 == "#testdb")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				%db = GetWord(%cropped, 0);
				%itemData = getItemData(%db);
				if(%itemData != "")
					Client::sendMessage(%TrueClientId, 0, %db @ " is a loaded ItemData with ID " @ %itemData);
				else
				{
					%id = nameToID(%db);
					if(%id != -1)
						Client::sendMessage(%TrueClientId, 0, %db @ " is a loaded object with ID " @ %id);
					else
						Client::sendMessage(%TrueClientId, 0, %db @ " is NOT loaded on the server.");
				}
			}
			return;
		}
		if(%w1 == "#listpacks")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				Client::sendMessage(%TrueClientId, $MsgBeige, $SpawnPackList);
			}
			return;
		}
	      if(%w1 == "#deleteobject")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
				%c1 = GetWord(%cropped, 0);
	                  if(%c1 != -1)
	                  {
					if(%c1.tag != "")
					{
						$tagToObjectId[%c1.tag] = "";
						if(IsInCommaList($DISlist, %c1.tag))
							$DISlist = RemoveFromCommaList($DISlist, %c1.tag);
						else if(IsInCommaList($SpawnPackList, %c1.tag))
							$SpawnPackList = RemoveFromCommaList($SpawnPackList, %c1.tag);
						else if(IsInCommaList($StaticShapeList, %c1.tag))
							$StaticShapeList = RemoveFromCommaList($StaticShapeList, %c1.tag);
					}
					deleteObject(%c1);
					ClearEvents(%c1);
	
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Attempted to deleteObject(" @ %c1 @ ")");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "#deleteobject [objectId].  Be careful with this command.");
	            }
			return;
	      }
if(%w1 == "#deletebot")
{
	if(%clientToServerAdminLevel >= 1)
	{
		%player = Client::getOwnedObject(%TrueClientId);
		
		// Get the object the player is looking at
		if(GameBase::getLOSinfo(%player, 50000))
		{
			%object = $los::object;
			%objType = getObjectType(%object);
			
			if(%objType == "Player")
			{
				// Use helper function to get client ID from Player object (handles enemy bots)
				%id = GetClientIdFromPlayerObject(%object);
				
				// If helper function returns -1, try using %object directly as client ID
				if(%id == -1 || %id == "")
					%id = %object;
				
				if(%id != -1 && (Player::isAiControlled(%id) || isRPGAI(%id)))
				{
					// Force delete broken bots using deleteObject
					%botName = Client::getName(%id);
					%botInfoAiName = fetchData(%id, "BotInfoAiName");
					
					// Determine if this is a town bot or enemy bot
					%isTownBot = false;
					%isEnemyBot = false;
					if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
					{
						if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
							%isTownBot = true;
						else
							%isEnemyBot = true;
					}
					
					%spawnBotInfo = fetchData(%id, "SpawnBotInfo");
					if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
						%isEnemyBot = true;
					
					// Decrement bot tracking counters
					if(%isTownBot)
					{
						$ActiveTownBots--;
						$TotalActiveBots--;
						if($ActiveTownBots < 0)
							$ActiveTownBots = 0;
						if($TotalActiveBots < 0)
							$TotalActiveBots = 0;
					}
					else if(%isEnemyBot)
					{
						$ActiveEnemyBots--;
						$TotalActiveBots--;
						if($ActiveEnemyBots < 0)
							$ActiveEnemyBots = 0;
						if($TotalActiveBots < 0)
							$TotalActiveBots = 0;
					}
					
					// CRITICAL: Use centralized DecrementSpawnCounter() for reliable counter management
					DecrementSpawnCounter(%id);
					
					// CRITICAL: Clean up bot data - comprehensive cleanup for both types
					// This cleanup handles both normal bots and shell bots (corrupted bots with wrong data)
					// For shell bots, we clear ALL data by client ID, regardless of corrupted BotInfoAiName
					// CRITICAL: Use preserveBotInfoAiName=true because AI::onDroneKilled() needs it
					storeData(%id, "noDropLootbagFlag", True);  // Set BEFORE clearing (not cleared)
					
					// PRIORITY 1: Use unified ClearAllBotData() with preserveBotInfoAiName=true
					ClearAllBotData(%id, true);
					ClearEvents(%id);
					
					// Handle town bot specific cleanup
					if(%isTownBot)
					{
						%townBotName = %botInfoAiName;
						if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
							%townBotName = String::getSubStr(%botInfoAiName, 8, 999);
						if(%townBotName != "" && %townBotName != -1 && %townBotName != "0")
							$TownBotSpawned[%townBotName] = "";
						$TownBotList = RemoveFromCommaList($TownBotList, %id);
					}
					
					// Clean up bot group if applicable
					%b = AI::IsInWhichBotGroup(%id);
					if(%b != -1)
						AI::RemoveBotFromBotGroup(%id, %b);
					
					// CRITICAL: Free AI number from $aiNumTable so it can be recycled
					// This ensures bot numbers restart from 0-20 instead of going to 100+
					// CRITICAL: For shell bots, the BotInfoAiName might be corrupted, so we need to
					// clear name-based tracking for BOTH the corrupted name AND any potential correct names
					%numberFreed = false;
					if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
					{
						// Get the number BEFORE clearing $tmpbotn to ensure we can free it
					%botNumber = $tmpbotn[%botInfoAiName];
					// CRITICAL: Check if AI number exists and is valid (including 0)
					// setAInumber() stores numeric values, so if it exists (not empty, not -1), it's valid
					// The old check '%botNumber != "0"' was WRONG - it skipped freeing number 0!
					if(%botNumber != "" && %botNumber != -1)
						{
							$aiNumTable[%botNumber] = "";
							$tmpbotn[%botInfoAiName] = "";
							%numberFreed = true;
							echo("[SPAWN DEBUG] #deletebot: Freed AI number " @ %botNumber @ " for bot " @ %botInfoAiName @ " (via BotInfoAiName) - number can now be recycled");
						}
						
						// CRITICAL: Clear spawn scheduling flag for this bot name (in case it's still set)
						$SpawnAIScheduled[%botInfoAiName] = "";
						
						// CRITICAL: Clear retry counters and spawn flags
						$EnemyBotClientIdRetry[%botInfoAiName] = "";
						$EnemyBotSpawnRetry[%botInfoAiName] = "";
						$Directive99RemovalAttempted[%botInfoAiName] = "";
					}
					
					// Fallback: Try extracting number from display name if BotInfoAiName failed
					if(!%numberFreed && %botName != "" && %botName != -1 && %botName != "0")
					{
					// Extract trailing number from display name (e.g., "AdminLiquifier20" -> "20")
					%lastChar = String::getSubStr(%botName, String::len(%botName) - 1, 1);
					%digitString = "0123456789";
					// Check if character is a digit (0-9 only) - use String::findSubStr to avoid TorqueScript == comparison bug ("E" == "0" evaluates to true)
					%isNumeric = (String::findSubStr(%digitString, %lastChar) != -1);
					
					if(%isNumeric)
					{
						%numStart = String::len(%botName) - 1;
						%extractedNumber = "";
						for(%i = %numStart; %i >= 0; %i--)
						{
							%char = String::getSubStr(%botName, %i, 1);
							// Check if character is a digit (0-9 only) - use String::findSubStr to avoid TorqueScript == comparison bug ("E" == "0" evaluates to true)
							if(String::findSubStr(%digitString, %char) != -1)
								%extractedNumber = %char @ %extractedNumber;
							else
								break;
						}
							
							// Try multiple possible bot name formats
							%possibleNames[0] = %botName;
							%possibleNames[1] = String::getSubStr(%botName, 0, %i + 1) @ %extractedNumber;
							
							for(%j = 0; %j < 2; %j++)
							{
								%tryName = %possibleNames[%j];
								if(%tryName != "" && %tryName != -1)
								{
									%tryNumber = $tmpbotn[%tryName];
									if(%tryNumber != "" && %tryNumber != -1 && %tryNumber != "0")
									{
										$aiNumTable[%tryNumber] = "";
										$tmpbotn[%tryName] = "";
										%numberFreed = true;
										echo("[SPAWN DEBUG] #deletebot: Freed AI number " @ %tryNumber @ " for bot " @ %tryName @ " (via display name fallback) - number can now be recycled");
										break;
									}
								}
							}
						}
					}
					
					// CRITICAL: For shell bots, also try to clear name-based tracking using the display name
					// Shell bots might have corrupted BotInfoAiName but the display name might be correct
					// Extract potential bot name from display name (e.g., "AngelCrucifier" -> "Crucifier")
					if(%botName != "" && %botName != -1 && %botName != "0")
					{
						// Try to extract bot name from display name patterns
						%potentialBotName = "";
						if(String::findSubStr(%botName, "Admin") == 0)
							%potentialBotName = String::getSubStr(%botName, 5, 999);
						else if(String::findSubStr(%botName, "Demon") == 0)
							%potentialBotName = String::getSubStr(%botName, 5, 999);
						else if(String::findSubStr(%botName, "Angel") == 0)
							%potentialBotName = String::getSubStr(%botName, 5, 999);
						else if(String::findSubStr(%botName, "God") == 0)
							%potentialBotName = String::getSubStr(%botName, 3, 999);
						else if(String::findSubStr(%botName, "Minotaur") == 0)
							%potentialBotName = String::getSubStr(%botName, 8, 999);
						
						// If we extracted a potential bot name and it's different from BotInfoAiName, clear it too
						if(%potentialBotName != "" && %potentialBotName != %botInfoAiName)
						{
							// Clear spawn scheduling flag for potential bot name
							$SpawnAIScheduled[%potentialBotName] = "";
							
							// Clear AI number tracking for potential bot name
							%potentialBotNumber = $tmpbotn[%potentialBotName];
							if(%potentialBotNumber != "" && %potentialBotNumber != -1 && %potentialBotNumber != "0")
							{
								$aiNumTable[%potentialBotNumber] = "";
								$tmpbotn[%potentialBotName] = "";
							}
							
							// Clear town bot spawn tracking if applicable
							$TownBotSpawned[%potentialBotName] = "";
						}
					}
					
					// CRITICAL: Clear pet data
					$PetList = RemoveFromCommaList($PetList, %id);
					%petowner = fetchData(%id, "petowner");
					if(%petowner != "" && %petowner != -1 && %petowner != "0")
					{
						storeData(%petowner, "PersonalPetList", RemoveFromCommaList(fetchData(%petowner, "PersonalPetList"), %id));
					}
					storeData(%id, "petowner", "");
					
					// CRITICAL: Decrement $numAI counter for enemy bots
					if(%isEnemyBot)
					{
						$numAI--;
						if($numAI < 0)
							$numAI = 0;
					}
					
					// CRITICAL: Use Player::Kill() instead of deleteObject() to trigger proper cleanup
					// Player::Kill() will:
					// - Call Player::onKilled() which decrements spawn counters and bot tracking counters
					// - Call AI::onDroneKilled() which performs comprehensive data cleanup
					// - Set the "recently freed" flag to prevent immediate client ID reuse
					// - Decrement $numAI (already done above, but Player::onKilled() will also do it)
					// If Player::Kill() fails or the bot is truly broken, we'll fall back to deleteObject()
					%playerObj = Client::getOwnedObject(%id);
					if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
					{
						// Bot object is valid - use Player::Kill() for proper cleanup
						storeData(%id, "noDropLootbagFlag", True); // Prevent loot drop
						Player::Kill(%id); // This triggers Player::onKilled() and AI::onDroneKilled()
						
						// CRITICAL: Schedule clearing of BotInfoAiName AFTER AI::delete() is scheduled
						// AI::delete() is scheduled 1.0s after Player::Kill() in Player::onKilled()
						// AI::onDroneKilled() runs when AI::delete() executes
						// So we need to wait at least 1.5s to ensure AI::onDroneKilled() has run
						if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
						{
							schedule("storeData(" @ %id @ ", \"BotInfoAiName\", \"\"); $EnemyBotData[" @ %id @ ", \"BotInfoAiName\"] = \"\"; $ClientData[" @ %id @ ", \"BotInfoAiName\"] = \"\"; $BotInfoAiName[" @ %id @ "] = \"\";", 2.0);
						}
					}
					else
					{
						// Bot object is invalid/broken - do manual cleanup and set recently freed flag
						// CRITICAL: Delete AI name from engine registry
						if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
						{
							%escapedName = String::replace(%botInfoAiName, "\"", "\\\"");
							// CRITICAL: Add 1 second delay to deletion to help code load properly and functions/variables be called more effectively
							schedule("AI::delete(\"" @ %escapedName @ "\");", 1.0);
						}
						
						// CRITICAL: Mark this client ID as recently freed to prevent immediate reuse
						// This prevents new bots from getting the same client ID before cleanup completes
						$ClientIdRecentlyFreed[%id] = getSimTime();
						
						// CRITICAL: Schedule clearing of $ClientIdRecentlyFreed flag after cleanup completes
						// This allows the client ID to be reused after cleanup is fully done (10 seconds - extended to prevent shell bots)
						schedule("$ClientIdRecentlyFreed[" @ %id @ "] = \"\";", 30.0);  // Extended from 10s to 30s
						
						// Force delete the broken bot object
						deleteObject(%object);
					}
					
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Force deleted bot: " @ %botName @ " (" @ %id @ ")");
					echo("[ADMIN]: " @ %TCsenderName @ " force deleted bot: " @ %botName @ " (" @ %id @ ")");
				}
				else if(%id != -1)
				{
					Client::sendMessage(%TrueClientId, 0, "Target is not a bot.");
				}
				else
				{
					Client::sendMessage(%TrueClientId, 0, "Could not get client ID for target.");
				}
			}
			else
			{
				// Allow deleting non-player objects too (like stuck deployables)
				deleteObject(%object);
				if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Deleted " @ %objType @ " object (" @ %object @ ")");
				echo("[ADMIN]: " @ %TCsenderName @ " deleted " @ %objType @ " object (" @ %object @ ")");
			}
		}
		else
		{
			Client::sendMessage(%TrueClientId, 0, "Nothing found at crosshair. Look at the bot/object you want to delete.");
		}
	}
	return;
}
	// DEBUG: Show all state flags on a player
	if(%w1 == "#debugflags")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%targetName = GetWord(%message, 1);  // Extract player name from command
			Admin::DebugPlayerFlags(%TrueClientId, %targetName);
		}
		else
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "Admin only.");
		}
		return;
	}
	// DEBUG: Diagnose Telekinesis gating (talent + bot classification)
	if(%w1 == "#debugtele" || %w1 == "#debugtelekinesis")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%targetName = GetWord(%message, 1);  // Optional player name or clientId
			Admin::DebugTelekinesisState(%TrueClientId, %targetName);
		}
		else
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "Admin only.");
		}
		return;
	}
	// DEBUG: Diagnose Telekinesis bag candidate filtering near a player
	if(%w1 == "#debugtelebags")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%targetName = GetWord(%message, 1);  // Optional player name or clientId
			Admin::DebugTelekinesisBags(%TrueClientId, %targetName);
		}
		else
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "Admin only.");
		}
		return;
	}
	// DEBUG: Diagnose invisibility bug
	if(%w1 == "#debuginvis")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%targetName = GetWord(%message, 1);
			Admin::DiagnoseInvisible(%TrueClientId, %targetName);
		}
		else
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "Admin only.");
		}
		return;
	}
	// DEBUG: Fix player visibility issues
	if(%w1 == "#fixvisible")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%targetName = GetWord(%message, 1);
			Admin::FixPlayerVisibility(%TrueClientId, %targetName);
		}
		else
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "Admin only.");
		}
		return;
	}
	if(%w1 == "#killallbots")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%killedCount = 0;
			%enemyBotsKilled = 0;
			%townBotsKilled = 0;
			
			// Kill all enemy bots
			for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			{
				// CRITICAL: Multiple safeguards to ensure we NEVER affect players
				// 1. Check if AI controlled
				// 2. Check if it's an RPG AI
				// 3. Verify it has SpawnBotInfo OR BotInfoAiName (players should never have these)
				// 4. Double-check that it's not a player by checking if they have player data (HasLoadedAndSpawned)
				if((Player::isAiControlled(%cl) || isRPGAI(%cl)))
				{
					%spawnBotInfo = fetchData(%cl, "SpawnBotInfo");
					%botInfoAiName = fetchData(%cl, "BotInfoAiName");
					%hasLoadedAndSpawned = fetchData(%cl, "HasLoadedAndSpawned");
					
					// CRITICAL SAFEGUARD: If this client has HasLoadedAndSpawned, it's a player - SKIP IT
					if(%hasLoadedAndSpawned != "" && %hasLoadedAndSpawned != "0" && %hasLoadedAndSpawned != -1)
					{
						echo("WARNING: #killallbots - Skipping client " @ %cl @ " (" @ Client::getName(%cl) @ ") - has HasLoadedAndSpawned flag (this is a player, not a bot)");
						continue;
					}
					
					// CRITICAL SAFEGUARD: Must have either SpawnBotInfo or BotInfoAiName to be considered a bot
					if((%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1) && (%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1))
					{
						// No bot data - skip to be safe
						echo("WARNING: #killallbots - Skipping client " @ %cl @ " (" @ Client::getName(%cl) @ ") - no bot data (SpawnBotInfo or BotInfoAiName)");
						continue;
					}
					
					%playerObj = Client::getOwnedObject(%cl);
					if(%playerObj != "" && %playerObj != -1)
					{
						%botName = Client::getName(%cl);
						
						// Determine bot type
						%isEnemyBot = false;
						%isTownBot = false;
						if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
							%isEnemyBot = true;
						else if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
						{
							if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
								%isTownBot = true;
							else
								%isEnemyBot = true;
						}
						
						// CRITICAL: Use centralized DecrementSpawnCounter() for reliable counter management
						if(%isEnemyBot)
						{
							DecrementSpawnCounter(%cl);
						}
						
						// Decrement bot tracking counters
						if(%isEnemyBot)
						{
							$ActiveEnemyBots--;
							$TotalActiveBots--;
							%enemyBotsKilled++;
						}
						else if(%isTownBot)
						{
							$ActiveTownBots--;
							$TotalActiveBots--;
							%townBotsKilled++;
						}
						
						// Clean up bot data (same as #deletebot)
						storeData(%cl, "noDropLootbagFlag", True);
						storeData(%cl, "SpawnBotInfo", "");
						storeData(%cl, "BotInfoAiName", "");
						
						// Free AI number
						if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
						{
							%botNumber = $tmpbotn[%botInfoAiName];
							if(%botNumber != "" && %botNumber != -1 && %botNumber != "0")
							{
								$aiNumTable[%botNumber] = "";
								$tmpbotn[%botInfoAiName] = "";
							}
							%escapedName = String::replace(%botInfoAiName, "\"", "\\\"");
							schedule("AI::delete(\"" @ %escapedName @ "\");", 1.0);
						}
						
						// Delete the bot
						deleteObject(%playerObj);
						%killedCount++;
					}
				}
			}
			
			// Ensure counters don't go negative
			if($ActiveEnemyBots < 0) $ActiveEnemyBots = 0;
			if($ActiveTownBots < 0) $ActiveTownBots = 0;
			if($TotalActiveBots < 0) $TotalActiveBots = 0;
			
			Client::sendMessage(%TrueClientId, 0, "Killed all bots: " @ %killedCount @ " total (" @ %enemyBotsKilled @ " enemy, " @ %townBotsKilled @ " town)");
			echo("[ADMIN]: " @ %TCsenderName @ " killed all bots: " @ %killedCount @ " total (" @ %enemyBotsKilled @ " enemy, " @ %townBotsKilled @ " town)");
		}
		return;
	}
	if(%w1 == "#respawnpoints" || %w1 == "#resetspawnpoints")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%resetCount = 0;
			%reconciledCount = 0;
			%group = nameToID("MissionGroup\\SpawnPoints");
			
			if(%group != -1)
			{
				for(%i = 0; %i <= Group::objectCount(%group)-1; %i++)
				{
					%spawnPoint = Group::getObject(%group, %i);
					if(%spawnPoint != -1 && %spawnPoint != "")
					{
						// Get actual bot count from registry before resetting
						%actualCount = GetRegisteredBotCount(%spawnPoint);
						if(%actualCount > 0)
						{
							// Set counter to actual count (reconcile)
							$numAIperSpawnPoint[%spawnPoint] = %actualCount;
							%reconciledCount++;
						}
						else
						{
							// No bots registered - reset to 0
							$numAIperSpawnPoint[%spawnPoint] = 0;
						}
						$SpawnPointInProgress[%spawnPoint] = "";
						%resetCount++;
					}
				}
			}
			
			// Also reset any spawn points that might be in the range 8650-8700 (common spawn point IDs)
			for(%sp = 8650; %sp <= 8700; %sp++)
			{
				if($numAIperSpawnPoint[%sp] > 0 || $SpawnPointInProgress[%sp] != "")
				{
					// Get actual bot count from registry before resetting
					%actualCount = GetRegisteredBotCount(%sp);
					if(%actualCount > 0)
					{
						// Set counter to actual count (reconcile)
						$numAIperSpawnPoint[%sp] = %actualCount;
						%reconciledCount++;
					}
					else
					{
						// No bots registered - reset to 0
						$numAIperSpawnPoint[%sp] = 0;
					}
					$SpawnPointInProgress[%sp] = "";
					%resetCount++;
				}
			}
			
			%message = "Reset " @ %resetCount @ " spawn point counters";
			if(%reconciledCount > 0)
				%message = %message @ " (reconciled " @ %reconciledCount @ " to actual bot counts)";
			%message = %message @ ".";
			Client::sendMessage(%TrueClientId, 0, %message);
			echo("[ADMIN]: " @ %TCsenderName @ " " @ %message);
		}
		return;
	}
if(%w1 == "#spawnpointscan")
{
	if(%clientToServerAdminLevel >= 1)
	{
		// Parse optional "fix" parameter
		%autoFix = false;
		if(%w2 == "fix")
			%autoFix = true;
		
		echo("[SPAWNPOINT SCAN] === Starting Spawn Point Scan ===");
		Client::sendMessage(%TrueClientId, 0, "=== SPAWN POINT SCAN ===");
		
		%group = nameToID("MissionGroup\\SpawnPoints");
		if(%group == -1)
		{
			Client::sendMessage(%TrueClientId, $MsgRed, "ERROR: Could not find SpawnPoints group");
			echo("[SPAWNPOINT SCAN] ERROR: Could not find SpawnPoints group");
			return;
		}
		
		%totalSpawnPoints = 0;
		%brokenCount = 0;
		%stuckReservations = 0;
		%counterMismatches = 0;
		%orphanedBots = 0;
		%fixedCount = 0;
		
		// Iterate all spawn points
		for(%i = 0; %i <= Group::objectCount(%group)-1; %i++)
		{
			%spawnPoint = Group::getObject(%group, %i);
			if(%spawnPoint == "" || %spawnPoint == -1)
				continue;
			
			%totalSpawnPoints++;
			%info = Object::getName(%spawnPoint);
			%maxBots = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
			%currentCounter = $numAIperSpawnPoint[%spawnPoint];
			if(%currentCounter == "")
				%currentCounter = 0;
			%registeredCount = GetRegisteredBotCount(%spawnPoint);
			%reservedStatus = $SpawnSlotReserved[%spawnPoint];
			%reservedTime = $SpawnSlotReservedTime[%spawnPoint];
			
			// Check for issues
			%issues = "";
			%isBroken = false;
			
			// Issue 1: Counter mismatch (counter != registered bots)
			if(%currentCounter != %registeredCount)
			{
				%issues = %issues @ "Counter mismatch (counter=" @ %currentCounter @ ", registered=" @ %registeredCount @ "). ";
				%counterMismatches++;
				%isBroken = true;
				
				if(%autoFix)
				{
					// Fix by setting counter to registered count
					$numAIperSpawnPoint[%spawnPoint] = %registeredCount;
					%issues = %issues @ "[FIXED: counter=" @ %registeredCount @ "] ";
					%fixedCount++;
				}
			}
			
			// Issue 2: Stuck reservation (reserved > 15 seconds)
			if(%reservedStatus == "true" && %reservedTime != "" && %reservedTime != -1)
			{
				%reservedAge = getSimTime() - %reservedTime;
				if(%reservedAge > 15)
				{
					%issues = %issues @ "Stuck reservation (" @ floor(%reservedAge) @ "s). ";
					%stuckReservations++;
					%isBroken = true;
					
					if(%autoFix)
					{
						// Fix by clearing reservation
						$SpawnSlotReserved[%spawnPoint] = "";
						$SpawnSlotReservedTime[%spawnPoint] = "";
						%issues = %issues @ "[FIXED: cleared] ";
						%fixedCount++;
					}
				}
			}
			
			// Issue 3: Counter > 0 but no registered bots (orphaned counter)
			if(%currentCounter > 0 && %registeredCount == 0)
			{
				%issues = %issues @ "Orphaned counter (counter=" @ %currentCounter @ " but 0 registered bots). ";
				%orphanedBots++;
				%isBroken = true;
				
				if(%autoFix)
				{
					// Fix by resetting counter
					$numAIperSpawnPoint[%spawnPoint] = 0;
					%issues = %issues @ "[FIXED: reset to 0] ";
					%fixedCount++;
				}
			}
			
			// Issue 4: Counter > max allowed
			if(%currentCounter > %maxBots && %maxBots > 0)
			{
				%issues = %issues @ "Counter exceeds max (" @ %currentCounter @ "/" @ %maxBots @ "). ";
				%isBroken = true;
				
				if(%autoFix)
				{
					$numAIperSpawnPoint[%spawnPoint] = %registeredCount;
					%issues = %issues @ "[FIXED: set to " @ %registeredCount @ "] ";
					%fixedCount++;
				}
			}
			
			// Report broken spawn points
			if(%isBroken)
			{
				%brokenCount++;
				%message = "BROKEN: " @ %spawnPoint @ " - " @ %issues;
				Client::sendMessage(%TrueClientId, $MsgRed, %message);
				echo("[SPAWNPOINT SCAN] " @ %message);
			}
		}
		
		// Summary
		%summaryColor = 0;
		if(%brokenCount > 0)
			%summaryColor = $MsgRed;
		
		%message = "=== SCAN COMPLETE: " @ %totalSpawnPoints @ " spawn points scanned ===";
		Client::sendMessage(%TrueClientId, 0, %message);
		echo("[SPAWNPOINT SCAN] " @ %message);
		
		if(%brokenCount == 0)
		{
			%message = "Result: All spawn points are healthy!";
			Client::sendMessage(%TrueClientId, $MsgGreen, %message);
			echo("[SPAWNPOINT SCAN] " @ %message);
		}
		else
		{
			%message = "Result: " @ %brokenCount @ " broken spawn points found:";
			Client::sendMessage(%TrueClientId, %summaryColor, %message);
			echo("[SPAWNPOINT SCAN] " @ %message);
			
			%message = "  - Counter mismatches: " @ %counterMismatches;
			Client::sendMessage(%TrueClientId, 0, %message);
			echo("[SPAWNPOINT SCAN] " @ %message);
			
			%message = "  - Stuck reservations: " @ %stuckReservations;
			Client::sendMessage(%TrueClientId, 0, %message);
			echo("[SPAWNPOINT SCAN] " @ %message);
			
			%message = "  - Orphaned counters: " @ %orphanedBots;
			Client::sendMessage(%TrueClientId, 0, %message);
			echo("[SPAWNPOINT SCAN] " @ %message);
			
			if(%autoFix)
			{
				%message = "Auto-fix applied: " @ %fixedCount @ " issues fixed";
				Client::sendMessage(%TrueClientId, $MsgGreen, %message);
				echo("[SPAWNPOINT SCAN] " @ %message);
			}
			else
			{
				%message = "TIP: Use '#spawnpointscan fix' to auto-fix issues";
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[SPAWNPOINT SCAN] " @ %message);
			}
		}
		
		%fixSuffix = "";
		if(%autoFix)
			%fixSuffix = " fix";
		echo("[ADMIN]: " @ %TCsenderName @ " ran #spawnpointscan" @ %fixSuffix);
	}
	return;
}
if(%w1 == "#auditainumbers")
{
	if(%clientToServerAdminLevel >= 1)
	{
		// Parse optional "fix" parameter
		%w2 = GetWord(%message, 1);  // CRITICAL: Get second word from message
		%autoFix = false;
		if(%w2 == "fix")
			%autoFix = true;
		
		echo("[AI NUMBER AUDIT] === Starting AI Number Audit ===");
		Client::sendMessage(%TrueClientId, 0, "=== AI NUMBER AUDIT ===");
		
		// Build list of bot names that are actually alive
		%liveBotNames = "";
		%botList = GetBotIdList();
		if(%botList != "")
		{
			for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
			{
				%botInfoAiName = fetchData(%botId, "BotInfoAiName");
				if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
					%liveBotNames = %liveBotNames @ %botInfoAiName @ " ";
			}
		}
		
		// Scan $aiNumTable for reserved numbers
		%reservedCount = 0;
		%orphanedCount = 0;
		%orphanedList = "";
		%freedCount = 0;
		
		for(%n = 0; %n <= 500; %n++)
		{
			if($aiNumTable[%n] != "" && $aiNumTable[%n] != -1)
			{
				%reservedCount++;
				
				// Find which bot name uses this number
				%foundBot = "";
				for(%j = 0; (%checkBotId = GetWord(%botList, %j)) != -1; %j++)
				{
					%checkAiName = fetchData(%checkBotId, "BotInfoAiName");
					if(%checkAiName != "" && $tmpbotn[%checkAiName] == %n)
					{
						%foundBot = %checkAiName;
						break;
					}
				}
				
				// Check if bot is actually alive
				if(%foundBot == "")
				{
					// Try to find by iterating $tmpbotn (less reliable since we can't iterate directly)
					// Instead check if any live bot name ends with this number
					%isOrphaned = true;
					for(%k = 0; (%liveName = GetWord(%liveBotNames, %k)) != -1; %k++)
					{
						if($tmpbotn[%liveName] == %n)
						{
							%isOrphaned = false;
							break;
						}
					}
					
					if(%isOrphaned)
					{
						%orphanedCount++;
						%orphanedList = %orphanedList @ %n @ " ";
						
						if(%autoFix)
						{
							$aiNumTable[%n] = "";
							%freedCount++;
						}
					}
				}
			}
		}
		
		// Summary
		%message = "Reserved AI numbers (0-500): " @ %reservedCount;
		Client::sendMessage(%TrueClientId, 0, %message);
		echo("[AI NUMBER AUDIT] " @ %message);
		
		%message = "Orphaned numbers: " @ %orphanedCount;
		if(%orphanedCount > 0)
		{
			Client::sendMessage(%TrueClientId, $MsgRed, %message);
			echo("[AI NUMBER AUDIT] " @ %message);
			
			if(%orphanedList != "")
			{
				%message = "  Orphaned list: " @ %orphanedList;
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[AI NUMBER AUDIT] " @ %message);
			}
			
			if(%autoFix)
			{
				%message = "Freed " @ %freedCount @ " orphaned numbers";
				Client::sendMessage(%TrueClientId, $MsgGreen, %message);
				echo("[AI NUMBER AUDIT] " @ %message);
			}
			else
			{
				%message = "TIP: Use '#auditainumbers fix' to free orphaned numbers";
				Client::sendMessage(%TrueClientId, 0, %message);
			}
		}
		else
		{
			Client::sendMessage(%TrueClientId, $MsgGreen, %message @ " - All clean!");
			echo("[AI NUMBER AUDIT] " @ %message @ " - All clean!");
		}
		
		// Also show $numAI for comparison
		%message = "Current $numAI counter: " @ $numAI;
		Client::sendMessage(%TrueClientId, 0, %message);
		echo("[AI NUMBER AUDIT] " @ %message);
		
		%fixSuffix = "";
		if(%autoFix)
			%fixSuffix = " fix";
		echo("[ADMIN]: " @ %TCsenderName @ " ran #auditainumbers" @ %fixSuffix);
	}
	return;
}
if(%w1 == "#fullbotscan")
{
	if(%clientToServerAdminLevel >= 1)
	{
		echo("[FULL BOT SCAN] === Starting Full Bot Scan ===");
		Client::sendMessage(%TrueClientId, 0, "=== FULL BOT SCAN ===");
		
		// Get all live bots in world
		%botList = GetBotIdList();
		%liveBotsCount = 0;
		%liveBots = "";
		if(%botList != "")
		{
			for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
			{
				%liveBotsCount++;
				%liveBots = %liveBots @ %botId @ " ";
			}
		}
		
		// Check 1: Bots in $BotRegistryList but not alive
		%orphanedRegistry = 0;
		%orphanedRegistryList = "";
		for(%i = 0; GetWord($BotRegistryList, %i) != -1; %i++)
		{
			%regId = GetWord($BotRegistryList, %i);
			%playerObj = Client::getOwnedObject(%regId);
			if(%playerObj == "" || %playerObj == -1)
			{
				%orphanedRegistry++;
				%orphanedRegistryList = %orphanedRegistryList @ %regId @ " ";
			}
		}
		
		// Check 2: Live bots not in $BotRegistryList (unregistered)
		%unregisteredBots = 0;
		%unregisteredList = "";
		for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
		{
			// Check if enemy bot (has SpawnBotInfo)
			%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
			if(%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0")
			{
				// Enemy bot - should be in $BotRegistryList
				%found = false;
				for(%j = 0; GetWord($BotRegistryList, %j) != -1; %j++)
				{
					if(GetWord($BotRegistryList, %j) == %botId)
					{
						%found = true;
						break;
					}
				}
				if(!%found)
				{
					%unregisteredBots++;
					%unregisteredList = %unregisteredList @ %botId @ " ";
				}
			}
		}
		
		// Check 3: Town bots - $TownBotSpawned vs alive
		%orphanedTownBots = 0;
		%orphanedTownBotsList = "";
		for(%i = 0; (%botName = GetWord($TownBotRegistry, %i)) != -1; %i++)
		{
			if(%botName == "" || %botName == "0")
				continue;
			%townBotId = $TownBotSpawned[%botName];
			if(%townBotId != "" && %townBotId != -1)
			{
				%playerObj = Client::getOwnedObject(%townBotId);
				if(%playerObj == "" || %playerObj == -1)
				{
					%orphanedTownBots++;
					%orphanedTownBotsList = %orphanedTownBotsList @ %botName @ " ";
				}
			}
		}
		
		// Summary
		%message = "Live bots in world: " @ %liveBotsCount;
		Client::sendMessage(%TrueClientId, 0, %message);
		echo("[FULL BOT SCAN] " @ %message);
		
		%message = "Orphaned in $BotRegistryList: " @ %orphanedRegistry;
		if(%orphanedRegistry > 0)
			Client::sendMessage(%TrueClientId, $MsgRed, %message);
		else
			Client::sendMessage(%TrueClientId, $MsgGreen, %message);
		echo("[FULL BOT SCAN] " @ %message);
		
		%message = "Unregistered enemy bots: " @ %unregisteredBots;
		if(%unregisteredBots > 0)
			Client::sendMessage(%TrueClientId, $MsgRed, %message);
		else
			Client::sendMessage(%TrueClientId, $MsgGreen, %message);
		echo("[FULL BOT SCAN] " @ %message);
		
		%message = "Orphaned town bot entries: " @ %orphanedTownBots;
		if(%orphanedTownBots > 0)
			Client::sendMessage(%TrueClientId, $MsgRed, %message);
		else
			Client::sendMessage(%TrueClientId, $MsgGreen, %message);
		echo("[FULL BOT SCAN] " @ %message);
		
		%totalIssues = %orphanedRegistry + %unregisteredBots + %orphanedTownBots;
		if(%totalIssues == 0)
		{
			%message = "Result: All bot registries are consistent!";
			Client::sendMessage(%TrueClientId, $MsgGreen, %message);
		}
		else
		{
			%message = "Result: " @ %totalIssues @ " inconsistencies found";
			Client::sendMessage(%TrueClientId, $MsgRed, %message);
		}
		echo("[FULL BOT SCAN] " @ %message);
		
		echo("[ADMIN]: " @ %TCsenderName @ " ran #fullbotscan");
	}
	return;
}
if(%w1 == "#spawntelemetry")
{
	if(%clientToServerAdminLevel >= 1)
	{
		// CRITICAL: Parse second word from message for subcommands (reset, fix)
		%w2 = GetWord(%message, 1);
		
		%successRate = 0;
		if($Telemetry_SpawnAttempts > 0)
			%successRate = floor(($Telemetry_SpawnSuccess / $Telemetry_SpawnAttempts) * 100);
		
		// Get current state
		%botList = GetBotIdList();
		%liveCount = 0;
		%enemyCount = 0;
		%townCount = 0;
		%unknownCount = 0;
		
		for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
		{
			%liveCount++;
			
			// Categorize bot
			%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
			%botInfoAiName = fetchData(%botId, "BotInfoAiName");
			
			if(%botInfoAiName == "" || %botInfoAiName == -1)
				%botInfoAiName = Client::getName(%botId);

			%isTown = false;
			%isEnemy = false;
			
			// Check for Town Bot markers
			if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
				%isTown = true;
			else if(isTownBot(%botId)) // Helper if available
				%isTown = true;
			
			// Check for Enemy Bot markers
			if(!%isTown)
			{
				if(%spawnBotInfo != "" && %spawnBotInfo != -1 && %spawnBotInfo != "0")
					%isEnemy = true;
				else if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
					%isEnemy = true;
			}
			
			if(%isTown)
				%townCount++;
			else if(%isEnemy)
				%enemyCount++;
			else
				%unknownCount++;
		}
		
		// Count reserved AI numbers
		%reservedCount = 0;
		for(%n = 0; %n <= 500; %n++)
		{
			if($aiNumTable[%n] != "" && $aiNumTable[%n] != -1)
				%reservedCount++;
		}
		
		// Output to BOTH in-game chat AND console
		echo("=== SPAWN TELEMETRY ===");
		Client::sendMessage(%TrueClientId, 0, "=== SPAWN TELEMETRY ===");
		
		%msg = "Spawn attempts: " @ $Telemetry_SpawnAttempts;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "  - Success: " @ $Telemetry_SpawnSuccess @ " (" @ %successRate @ "%)";
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "  - Failed: " @ $Telemetry_SpawnFailed;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "    - Town bot conflict: " @ $Telemetry_SpawnFailedTownBot;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "    - Client ID issues: " @ $Telemetry_SpawnFailedClientId;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "    - Zone empty: " @ $Telemetry_SpawnFailedZoneEmpty;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "    - Other: " @ $Telemetry_SpawnFailedOther;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "Deaths processed: " @ $Telemetry_DeathsProcessed;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "AI numbers freed: " @ $Telemetry_AINumbersFreed;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "AI number orphans: " @ $Telemetry_AINumberOrphans;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		// $numAI tracking
		echo("=== $numAI TRACKING ===");
		Client::sendMessage(%TrueClientId, 0, "=== $numAI TRACKING ===");
		
		%msg = "$numAI counter: " @ $numAI;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "$numAI increments: " @ $Telemetry_NumAI_Inc;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "$numAI decrements: " @ $Telemetry_NumAI_Dec;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "Net change: " @ ($Telemetry_NumAI_Inc - $Telemetry_NumAI_Dec);
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "Live bots now: " @ %liveCount @ " (Enemy: " @ %enemyCount @ ", Town: " @ %townCount @ ")";
		if(%unknownCount > 0)
			%msg = %msg @ " [Unknown: " @ %unknownCount @ "]";
			
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		%msg = "Reserved AI numbers: " @ %reservedCount;
		echo("[TELEMETRY] " @ %msg);
		Client::sendMessage(%TrueClientId, 0, %msg);
		
		// CRITICAL FIX: Only compare $numAI against Enemy Count (Town bots don't use $numAI)
		%discrepancy = $numAI - %enemyCount;
		if(%discrepancy != 0)
		{
			%msg = "DISCREPANCY: $numAI is " @ %discrepancy @ " higher than enemy bots! ($numAI=" @ $numAI @ ", Enemies=" @ %enemyCount @ ")";
			echo("[TELEMETRY] WARNING: " @ %msg);
			Client::sendMessage(%TrueClientId, $MsgRed, %msg);
		}
		
		if(%w2 == "reset")
		{
			Telemetry_Reset();
			$Telemetry_NumAI_Inc = 0;
			$Telemetry_NumAI_Dec = 0;
			$Telemetry_SpawnFailedZoneEmpty = 0;
			%msg = "Telemetry counters reset.";
			echo("[TELEMETRY] " @ %msg);
			Client::sendMessage(%TrueClientId, $MsgGreen, %msg);
		}
		else if(%w2 == "fix")
		{
			// Fix $numAI to match ENEMY count
			%oldNumAI = $numAI;
			$numAI = %enemyCount;
			%msg = "Fixed $numAI: " @ %oldNumAI @ " -> " @ %enemyCount;
			echo("[TELEMETRY] " @ %msg);
			Client::sendMessage(%TrueClientId, $MsgGreen, %msg);
		}
		
		echo("[ADMIN]: " @ %TCsenderName @ " ran #spawntelemetry");
	}
	return;
}
	if(%w1 == "#spawnpointdebug")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			%spawnPointId = %w2;
			if(%spawnPointId == "" || %spawnPointId == -1)
			{
				Client::sendMessage(%TrueClientId, 0, "Usage: #spawnpointdebug <spawnPointId>");
				echo("[#spawnpointdebug] Usage: #spawnpointdebug <spawnPointId>");
				return;
			}
			
			%info = Object::getName(%spawnPointId);
			%currentCounter = $numAIperSpawnPoint[%spawnPointId];
			if(%currentCounter == "")
				%currentCounter = 0;
			%maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
			%registeredCount = GetRegisteredBotCount(%spawnPointId);
			%spawnInProgress = $SpawnPointInProgress[%spawnPointId];
			%cooldownUntil = $SpawnPointCooldownUntil[%spawnPointId];
			%reservedStatus = $SpawnSlotReserved[%spawnPointId];
			%reservedTime = $SpawnSlotReservedTime[%spawnPointId];
			
			%message = "=== SPAWNPOINT DEBUG: " @ %spawnPointId @ " ===";
			Client::sendMessage(%TrueClientId, 0, %message);
			echo("[#spawnpointdebug] " @ %message);
			
			%message = "Counter: " @ %currentCounter @ "/" @ %maxs @ " (registered bots: " @ %registeredCount @ ")";
			Client::sendMessage(%TrueClientId, 0, %message);
			echo("[#spawnpointdebug] " @ %message);
			
			%inProgressStatus = "NO";
			if(%spawnInProgress == "true")
				%inProgressStatus = "YES";
			%message = "Spawn in progress: " @ %inProgressStatus;
			Client::sendMessage(%TrueClientId, 0, %message);
			echo("[#spawnpointdebug] " @ %message);
			
			if(%cooldownUntil != "" && %cooldownUntil != -1)
			{
				%cooldownRemaining = %cooldownUntil - getSimTime();
				if(%cooldownRemaining > 0)
				{
					%message = "Cooldown: Active (" @ %cooldownRemaining @ "s remaining, expires @ " @ floor(%cooldownUntil) @ ")";
					Client::sendMessage(%TrueClientId, 0, %message);
					echo("[#spawnpointdebug] " @ %message);
				}
				else
				{
					%message = "Cooldown: Expired (was " @ floor(%cooldownUntil) @ ", now " @ floor(getSimTime()) @ ")";
					Client::sendMessage(%TrueClientId, 0, %message);
					echo("[#spawnpointdebug] " @ %message);
				}
			}
			else
			{
				%message = "Cooldown: None";
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#spawnpointdebug] " @ %message);
			}
			
			if(%reservedStatus == "true")
			{
				%reservedAge = "";
				if(%reservedTime != "" && %reservedTime != -1)
				{
					%reservedAge = getSimTime() - %reservedTime;
					%message = "Reserved slot: YES (age: " @ %reservedAge @ "s, reserved @ " @ floor(%reservedTime) @ ")";
				}
				else
				{
					%message = "Reserved slot: YES (no timestamp)";
				}
				if(%reservedAge != "" && %reservedAge > 15)
				{
					Client::sendMessage(%TrueClientId, $MsgRed, %message @ " - WARNING: Stuck reservation!");
					echo("[#spawnpointdebug] " @ %message @ " - WARNING: Stuck reservation!");
				}
				else
				{
					Client::sendMessage(%TrueClientId, 0, %message);
					echo("[#spawnpointdebug] " @ %message);
				}
			}
			else
			{
				%message = "Reserved slot: NO";
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#spawnpointdebug] " @ %message);
			}
			
			// Show registered bots for this spawn point
			%botList = "";
			%botCount = 0;
			for(%i = 0; GetWord($BotRegistryList, %i) != -1; %i++)
			{
				%clientId = GetWord($BotRegistryList, %i);
				if($BotRegistry[%clientId] == %spawnPointId)
				{
					%botCount++;
					%botName = $BotRegistry[%clientId, "name"];
					if(%botName == "")
						%botName = "Unknown";
					%displayName = Client::getName(%clientId);
					if(%displayName == "")
						%displayName = "N/A";
					if(%botList == "")
						%botList = %clientId @ " (" @ %botName @ ", " @ %displayName @ ")";
					else
						%botList = %botList @ ", " @ %clientId @ " (" @ %botName @ ", " @ %displayName @ ")";
				}
			}
			
			if(%botCount > 0)
			{
				%message = "Registered bots (" @ %botCount @ "): " @ %botList;
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#spawnpointdebug] " @ %message);
			}
			else
			{
				%message = "Registered bots: None";
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#spawnpointdebug] " @ %message);
			}
			
			// Diagnosis
			%diagnosis = "";
			if(%currentCounter >= %maxs)
				%diagnosis = %diagnosis @ "Counter at max. ";
			if(%spawnInProgress == "true")
				%diagnosis = %diagnosis @ "Spawn in progress. ";
			if(%cooldownUntil != "" && %cooldownUntil > getSimTime())
				%diagnosis = %diagnosis @ "Cooldown active. ";
			if(%reservedStatus == "true" && %reservedAge != "" && %reservedAge > 15)
				%diagnosis = %diagnosis @ "Stuck reservation. ";
			if(%currentCounter != %registeredCount)
				%diagnosis = %diagnosis @ "Counter mismatch (counter=" @ %currentCounter @ ", registered=" @ %registeredCount @ "). ";
			
			if(%diagnosis != "")
			{
				%message = "Diagnosis: " @ %diagnosis;
				Client::sendMessage(%TrueClientId, $MsgRed, %message);
				echo("[#spawnpointdebug] " @ %message);
			}
			else
			{
				%message = "Diagnosis: No issues detected";
				Client::sendMessage(%TrueClientId, 0, %message);
				echo("[#spawnpointdebug] " @ %message);
			}
		}
		return;
	}
	if(%w1 == "#resetspawns" || %w1 == "#wipespawns")
	{
		if(%clientToServerAdminLevel >= 1)
		{
			// First kill all bots using Player::Kill() to trigger proper cleanup via Player::onKilled()
			// This ensures:
			// - Spawn counters are decremented
			// - Bot tracking counters are decremented
			// - $numAI is decremented
			// - Bot data is cleaned up via ClearVariables()
			// - AI numbers are freed
			// CRITICAL: Use GetBotIdList() instead of Client::getFirst()/getNext() to find all bots
			// GetBotIdList() finds bots by iterating through MissionCleanup Player objects,
			// which is more reliable than Client::getFirst()/getNext() for enemy bots
			%killedCount = 0;
			%enemyBotsKilled = 0;
			%townBotsKilled = 0;
			
			%botList = GetBotIdList();
			if(%botList != "")
			{
				for(%i = 0; (%botId = GetWord(%botList, %i)) != -1; %i++)
				{
					// CRITICAL: Multiple safeguards to ensure we NEVER affect players
					// 1. GetBotIdList() already filters for bots, but double-check
					// 2. Verify it has SpawnBotInfo OR BotInfoAiName (players should never have these)
					// 3. Check if they have a character save file (players have save files, bots don't)
					%spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
					%botInfoAiName = fetchData(%botId, "BotInfoAiName");
					
					// CRITICAL SAFEGUARD: Must have either SpawnBotInfo or BotInfoAiName to be considered a bot
					// If it doesn't have bot data, it's a player - skip it
					if((%spawnBotInfo == "" || %spawnBotInfo == "0" || %spawnBotInfo == -1) && (%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1))
					{
						// No bot data - this is a player, skip to be safe
						echo("WARNING: #resetspawns - Skipping client " @ %botId @ " (" @ Client::getName(%botId) @ ") - no bot data (SpawnBotInfo or BotInfoAiName) - this is a player");
						continue;
					}
					
					// CRITICAL: Additional check - if it has a character save file, it's a player
					// Check if they have a character save file (players have save files, bots don't)
					%playerName = Client::getName(%botId);
					if(%playerName != "" && %playerName != -1)
					{
						%characterFile = "temp\\" @ %playerName @ ".cs";
						if(isFile(%characterFile))
						{
							// Has character save file - this is a player, not a bot
							echo("WARNING: #resetspawns - Skipping client " @ %botId @ " (" @ %playerName @ ") - has character save file (this is a player, not a bot)");
							continue;
						}
					}
					
					%playerObj = Client::getOwnedObject(%botId);
					if(%playerObj != "" && %playerObj != -1)
					{
						%botName = Client::getName(%botId);
						
						%isEnemyBot = false;
						%isTownBot = false;
						if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
							%isEnemyBot = true;
						else if(%botInfoAiName != "" && %botInfoAiName != -1 && %botInfoAiName != "0")
						{
							if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
								%isTownBot = true;
							else
								%isEnemyBot = true;
						}
						
						// Track bot types for summary
						if(%isEnemyBot)
							%enemyBotsKilled++;
						else if(%isTownBot)
						{
							// Skip town bots - they don't respawn automatically and shouldn't be killed
							echo("[ADMIN] #resetspawns - Skipping town bot " @ %botName @ " (clientId=" @ %botId @ ")");
							continue;
						}
						
						// CRITICAL: Use Player::Kill() instead of deleteObject() to trigger proper cleanup
						// Player::Kill() calls Player::onKilled() which:
						// - Decrements spawn counters for enemy bots
						// - Decrements bot tracking counters ($ActiveEnemyBots, $TotalActiveBots, $numAI)
						// - Cleans up bot data via ClearVariables() (called from AI::onDroneKilled())
						// - Frees AI numbers
						// - Clears directives and events
						storeData(%botId, "noDropLootbagFlag", True); // Prevent loot drop
						// CRITICAL: Set noExperienceFlag to prevent EXP distribution for admin-killed bots
						storeData(%botId, "noExperienceFlag", True);
						Player::Kill(%botId); // This triggers Player::onKilled() which does all cleanup
						%killedCount++;
						%botType = "unknown";
						if(%isEnemyBot)
							%botType = "enemy";
						else if(%isTownBot)
							%botType = "town";
						echo("[ADMIN] #resetspawns - Killed bot " @ %botName @ " (clientId=" @ %botId @ ", type: " @ %botType @ ")");
					}
					else
					{
						echo("WARNING: #resetspawns - Bot " @ %botId @ " (" @ Client::getName(%botId) @ ") has no Player object - skipping");
					}
				}
			}
			
			// Ensure counters don't go negative (Player::onKilled() already does this, but double-check)
			if($ActiveEnemyBots < 0) $ActiveEnemyBots = 0;
			if($ActiveTownBots < 0) $ActiveTownBots = 0;
			if($TotalActiveBots < 0) $TotalActiveBots = 0;
			if($numAI < 0) $numAI = 0;
			
			// Then reset all spawn point counters
			%resetCount = 0;
			%group = nameToID("MissionGroup\\SpawnPoints");
			
			if(%group != -1)
			{
				for(%i = 0; %i <= Group::objectCount(%group)-1; %i++)
				{
					%spawnPoint = Group::getObject(%group, %i);
					if(%spawnPoint != -1 && %spawnPoint != "")
					{
						$numAIperSpawnPoint[%spawnPoint] = 0;
						$SpawnPointInProgress[%spawnPoint] = "";
						%resetCount++;
					}
				}
			}
			
			for(%sp = 8650; %sp <= 8700; %sp++)
			{
				if($numAIperSpawnPoint[%sp] > 0)
				{
					$numAIperSpawnPoint[%sp] = 0;
					$SpawnPointInProgress[%sp] = "";
					%resetCount++;
				}
			}
			
			Client::sendMessage(%TrueClientId, 0, "Wiped enemy bots (" @ %killedCount @ " killed, town bots skipped) and reset " @ %resetCount @ " spawn point counters.");
			echo("[ADMIN]: " @ %TCsenderName @ " wiped enemy bots (" @ %killedCount @ " killed, town bots skipped) and reset " @ %resetCount @ " spawn point counters.");
		}
		return;
	}
	if(%w1 == "#getposition")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%player = Client::getOwnedObject(%TrueClientId);
				
				// Get player's current position and rotation
				%playerPos = GameBase::getPosition(%player);
				%playerRot = GameBase::getRotation(%player);
				
				Client::sendMessage(%TrueClientId, 0, "Your Position: " @ %playerPos);
				Client::sendMessage(%TrueClientId, 0, "Your Rotation: " @ %playerRot);
				
				// Also get LOS (line of sight) position
				GameBase::getLOSinfo(%player, 50000);
				Client::sendMessage(%TrueClientId, 0, "Position at LOS: " @ $los::position);
			}
			return;
		}
		if(%w1 == "#exportlos")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%player = Client::getOwnedObject(%TrueClientId);
				if(GameBase::getLOSinfo(%player, 50000))
				{
					%losPos = $los::position;
					if(%losPos != "" && %losPos != "0 0 0")
					{
						%name = GetWord(%cropped, 0);
						if(%name == -1 || %name == "")
							%name = "unnamed";
						
						%name = String::replace(%name, " ", "_");
						
						// Save to unique global variable and export it (using eval + export pattern similar to #exportdata)
						eval("$ExportedLOS_" @ %name @ " = \"" @ %losPos @ "\";");
						export("ExportedLOS_" @ %name, "temp\\exported_los.cs", true);
						
						Client::sendMessage(%TrueClientId, 0, "LOS exported successfully: $ExportedLOS_" @ %name @ " = \"" @ %losPos @ "\"");
					}
					else
					{
						Client::sendMessage(%TrueClientId, 0, "Error: Line of Sight target position is invalid.");
					}
				}
				else
				{
					Client::sendMessage(%TrueClientId, 0, "Error: Line of Sight target not found.");
				}
			}
			return;
		}
		if(%w1 == "#deathmsg")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%msg = String::NEWgetSubStr(%cropped, (String::len(%c1)+1), 99999);
	
				if(%c1 != -1)
				{
					%id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						storeData(%id, "deathmsg", %msg);
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") deathmsg to " @ fetchData(%id, "deathmsg"));
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			else
			{
				storeData(%TrueClientId, "deathmsg", %cropped);
				Client::sendMessage(%TrueClientId, 0, "Changed your death message to: " @ fetchData(%TrueClientId, "deathmsg"));
			}
			return;
		}
		if(%w1 == "#block")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%bname = GetWord(%cropped, 0);
				if(%bname != -1)
				{
					//Always clear the blockdata
					ClearBlockData(%senderName, %bname);
		
					if(!IsInCommaList($BlockList[%senderName], %bname))
						$BlockList[%senderName] = AddToCommaList($BlockList[%senderName], %bname);
		
					storeData(%TrueClientId, "BlockInputFlag", %bname);
					storeData(%TrueClientId, "tmpBlockCnt", "");
	
					ManageBlockOwnersList(%senderName);
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Incorrect syntax for #block [blockname]");
			}
			return;
		}
		if(%w1 == "#endblock")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				if(fetchData(%TrueClientId, "BlockInputFlag") != "")
				{
					storeData(%TrueClientId, "BlockInputFlag", "");
					storeData(%TrueClientId, "tmpBlockCnt", "");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "No block to end!");
			}
			return;
		}
		if(%w1 == "#delblock")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%bname = GetWord(%cropped, 0);
				if(%bname != -1)
				{
					if(IsInCommaList($BlockList[%senderName], %bname))
					{
						ClearBlockData(%senderName, %bname);
						$BlockList[%senderName] = RemoveFromCommaList($BlockList[%senderName], %bname);
	
						ManageBlockOwnersList(%senderName);
	
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Block " @ %bname @ " deleted.");
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Block does not exist!");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Incorrect syntax for #delblock [blockname]");
			}
			return;
		}
		if(%w1 == "#clearblocks")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				%targetName = GetWord(%cropped, 0);
				%id = NEWgetClientByName(%targetName);
			}
			else if(%clientToServerAdminLevel >= 3)
			{
				%targetName = %senderName;
				%id = %TrueClientId;
			}
	
			if(%id != -1)
			{
				if($BlockList[%targetName] != "")
				{
					%list = $BlockList[%targetName];
					$BlockList[%targetName] = "";
					for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
					{
						%w = String::NEWgetSubStr(%list, 0, %p);
						ClearBlockData(%targetName, %w);
					}
					ManageBlockOwnersList(%targetName);
	
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Deleted ALL of " @ %targetName @ "'s blocks.");
				}
			}
			else
				Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	
			return;
		}
		if(%w1 == "#clearallblocks")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				%bname = GetWord(%cropped, 0);
				if(%bname == "confirm")
				{
					%blist = $BlockOwnersList;
					for(%bp = String::findSubStr(%blist, ","); (%bp = String::findSubStr(%blist, ",")) != -1; %blist = String::NEWgetSubStr(%blist, %bp+1, 99999))
					{
						%name = String::NEWgetSubStr(%blist, 0, %bp);
	
						if($BlockList[%name] != "")
						{
							%list = $BlockList[%name];
							$BlockList[%name] = "";
							for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
							{
								%w = String::NEWgetSubStr(%list, 0, %p);
								ClearBlockData(%name, %w);
							}
						}
						ManageBlockOwnersList(%name);
					}
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Deleted EVERYONE's blocks.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Type #clearallblocks confirm to clear EVERYONE's blocks.");
			}
			return;
		}
		if(%w1 == "#listblocks")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				if(%cropped != "")
				{
					if(IsInCommaList($BlockOwnersList, %cropped))
						Client::sendMessage(%TrueClientId, 0, %cropped @ "'s BlockList: " @ $BlockList[%cropped]);
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
			}
			else if(%clientToServerAdminLevel >= 3)
			{
				Client::sendMessage(%TrueClientId, 0, "Your BlockList: " @ $BlockList[%senderName]);
			}
			return;
		}
		if(%w1 == "#echo")
		{
			if(String::ICompare(%cropped, "off") == 0)
				%TrueClientId.echoOff = True;
			else if(String::ICompare(%cropped, "on") == 0)
				%TrueClientId.echoOff = "";
			else
				Client::sendMessage(%TrueClientId, $MsgWhite, %cropped);
	
			return;
		}
		if(%w1 == "#call")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%bname = GetWord(%cropped, 0);
				
				if(%bname != -1)
				{
					%list = String::NEWgetSubStr(%cropped, (String::len(%bname)+1), 99999);
					for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
						%a[%c++] = String::NEWgetSubStr(%list, 0, %p);
	
					if(%c <= 8)
					{
						if(IsInCommaList($BlockList[%senderName], %bname))
						{
							%TrueClientId.echoOff = True;
		
							for(%i = 1; (%bd = $BlockData[%senderName, %bname, %i]) != ""; %i++)
							{
								if(%a[1] != "")
									%bd = nsprintf(%bd, %a[1], %a[2], %a[3], %a[4], %a[5], %a[6], %a[7], %a[8]);
	
								remoteSay(%clientId, 0, %bd, %senderName);
							}
		
							%TrueClientId.echoOff = "";
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Block does not exist!");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Too many parameters for #call (max of 8)");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Incorrect syntax for #call [blockname]");
			}
			return;
		}
		if(%w1 == "#givethisstuff")
		{
			if(%clientToServerAdminLevel >= 4)
			{
				%c1 = GetWord(%cropped, 0);
				%stuff = String::NEWgetSubStr(%cropped, (String::len(%c1)+1), 99999);
	
				if(%c1 != -1)
				{
					%id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						GiveThisStuff(%id, %stuff, True);
						if(Player::isAiControlled(%id))
							HardcodeAIskills(%id);
							
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Gave " @ %c1 @ " (" @ %id @ "): " @ %stuff);
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
		}
		if(%w1 == "#takethisstuff")
		{
			if(%clientToServerAdminLevel >= 4)
			{
				%c1 = GetWord(%cropped, 0);
				%stuff = String::NEWgetSubStr(%cropped, (String::len(%c1)+1), 99999);
	
				if(%c1 != -1)
				{
					%id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						if(HasThisStuff(%id, %stuff))
						{
							TakeThisStuff(%id, %stuff);
							if(Player::isAiControlled(%id))
								HardcodeAIskills(%id);
							RefreshAll(%id);
	
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Took " @ %c1 @ " (" @ %id @ "): " @ %stuff);
						}
						else
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Could not take stuff.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
		}
	      if(%w1 == "#refreshbotskills")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
	                  if(%cropped == "")
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	                  else
	                  {
	                        %id = NEWgetClientByName(%cropped);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						HardcodeAIskills(%id);
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Refreshed skills for " @ %cropped @ " (" @ %id @ ").");
					}
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	            }
			return;
	      }
		if(%w1 == "#listblockowners")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				Client::sendMessage(%TrueClientId, $MsgBeige, $BlockOwnersList);
			}
			return;
		}
		if(%w1 == "#nodroppack")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
	                              if(%c2 == 0)
	                                    storeData(%id, "noDropLootbagFlag", "");
	                              else if(%c2 == 1)
	                                    storeData(%id, "noDropLootbagFlag", True);
	
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") noDropLootbagFlag to '" @ fetchData(%id, "noDropLootbagFlag") @ "'.");
	                        }
	                        else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
		if(%w1 == "#playsound")
		{
			if(%clientToServerAdminLevel >= 2)
			{
				%c1 = GetWord(%cropped, 0);
				%pos = String::NEWgetSubStr(%cropped, (String::len(%c1)+1), 99999);
	
				if(%c1 != -1)
				{
					if(GetWord(%pos, 0) == -1)
					{
						if(GameBase::getLOSinfo(Client::getOwnedObject(%TrueClientId), 50000))
							%pos = $los::position;
						else
							%pos = GameBase::getPosition(%TrueClientId);
					}
					playSound(%c1, %pos);
	
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Playing sound " @ %c1 @ " at pos " @ %pos);
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify nsound & position.");
			}
			return;
		}
	      if(%w1 == "#delbot")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  if(%cropped != -1)
	                  {
	                        %id = NEWgetClientByName(%cropped);
		
					if(%id != -1)
	                        {
						if(Player::isAiControlled(%id))
						{
							storeData(%id, "noDropLootbagFlag", True);
							ClearEvents(%id);
							Player::Kill(%id);
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %cropped @ " (" @ %id @ ") was deleted.");
						}
						else
	                              	Client::sendMessage(%TrueClientId, 0, "This command only works on bots.");
	                        }
	                        else
	                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
	            }
			return;
	      }
		if(%w1 == "#loadout")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%c1 = GetWord(%cropped, 0);
				%stuff = String::NEWgetSubStr(%cropped, (String::len(%c1)+1), 99999);
	
				if(%c1 != -1)
				{
					if(!IsInCommaList($LoadOutList, %c1))
					{
						$LoadOutList = AddToCommaList($LoadOutList, %c1);
						$LoadOut[%c1] = %stuff;
	
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Loadout " @ %c1 @ " defined.");
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Loadout tagname already exists.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify tagname & data.");
			}
			return;
		}
		if(%w1 == "#delloadout")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%c1 = GetWord(%cropped, 0);
	
				if(%c1 != -1)
				{
					if(IsInCommaList($LoadOutList, %c1))
					{
						$LoadOutList = RemoveFromCommaList($LoadOutList, %c1);
						$LoadOut[%c1] = "";
	
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Loadout " @ %c1 @ " deleted.");
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Loadout tagname does not exist.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify tagname.");
			}
			return;
		}
		if(%w1 == "#clearloadouts")
		{
			if(%clientToServerAdminLevel >= 4)
			{
				%list = $LoadOutList;
				$LoadOutList = "";
				for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
				{
					%w = String::NEWgetSubStr(%list, 0, %p);
					$LoadOut[%w] = "";
				}
	
				if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Deleted ALL loadouts.");
			}
			return;
		}
		if(%w1 == "#showloadout")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%c1 = GetWord(%cropped, 0);
	
				if(%c1 != -1)
				{
					if(IsInCommaList($LoadOutList, %c1))
						Client::sendMessage(%TrueClientId, 0, %c1 @ ": " @ $LoadOut[%c1]);
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Loadout tagname does not exist.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify tagname.");
			}
			return;
		}
		if(%w1 == "#listloadouts")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%list = $LoadOutList;
				for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
				{
					%w = String::NEWgetSubStr(%list, 0, %p);
					Client::sendMessage(%TrueClientId, 0, %w @ ": " @ $LoadOut[%w]);
				}
			}
			return;
		}
		if(%w1 == "#nobotsniff")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
	                        else if(%id != -1)
	                        {
						if(%c2 == 0)
							storeData(%id, "noBotSniff", "");
						else if(%c2 == 1)
							storeData(%id, "noBotSniff", True);
	
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Changed " @ %c1 @ " (" @ %id @ ") noBotSniff flag to " @ %c2 @ ".");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#addrankpoints" || %w1 == "#addrp")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						storeData(%id, "RankPoints", %c2, "inc");
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") RankPoints to " @ fetchData(%id, "RankPoints") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " added " @ %c2 @ " rank points to " @ %c1);
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
	            }
			return;
	      }
	      if(%w1 == "#sethouse")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  %c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
	                  if(%c1 != -1 && %c2 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						%hn = "";
						%isNull = false;
						if(String::ICompare(%c2, "null") == 0)
						{
							%hn = "NULL"; // Special marker for NULL
							%isNull = true;
						}
						else
						{
							for(%i = 1; $HouseName[%i] != ""; %i++)
							{
								if(String::findSubStr($HouseName[%i], %c2) != -1)
									%hn = %i;
							}
						}
	
						if(%hn != "")
						{
							if(%isNull)
							{
								// NULL specified - clear house (set to empty string)
								%oldHouse = fetchData(%id, "MyHouse");
								if(%oldHouse != "" && %oldHouse != "0")
								{
									// Boot from current house if they had one
									%oldHouseNum = GetHouseNumber(%oldHouse);
									if(%oldHouseNum != "")
										$HouseMember[%oldHouse] -= 1;
									UnequipMountedStuff(%id);
									storeData(%id, "RankPoints", 0);
									GameBase::setTeam(%id, 0);
									UpdateHouseObjectivesDisplay();
								}
								storeData(%id, "MyHouse", "");
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Cleared " @ %c1 @ " (" @ %id @ ") House (set to NULL).");
							}
							else
							{
								// Valid house number - set the house
								%hname = $HouseName[%hn];
								%oldHouse = fetchData(%id, "MyHouse");
								if(%oldHouse != "" && %oldHouse != "0" && %oldHouse != %hname)
								{
									// Boot from old house if different
									%oldHouseNum = GetHouseNumber(%oldHouse);
									if(%oldHouseNum != "")
										$HouseMember[%oldHouse] -= 1;
									UnequipMountedStuff(%id);
									storeData(%id, "RankPoints", 0);
									GameBase::setTeam(%id, 0);
								}
								storeData(%id, "MyHouse", %hname);
								storeData(%id, "RankPoints", $joinHouseRankPoints);
								$HouseMember[%hname] += 1;
								GameBase::setTeam(%id, 0);
								UpdateHouseObjectivesDisplay();
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") House to " @ fetchData(%id, "MyHouse") @ ".");
							}
						}
						else
		                              Client::sendMessage(%TrueClientId, 0, "Invalid House.");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & house (to clear house, use: #sethouse name NULL).");
	            }
			return;
	      }
		  	      if(%w1 == "#setartifact")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                  {
	                        Client::sendMessage(%TrueClientId, 0, "Usage: #setartifact \"HouseName\" \"1,2,3,4\"");
	                        Client::sendMessage(%TrueClientId, 0, "Flag numbers: 1=Gratigud, 2=Belaguard, 3=Crosion, 4=Murch");
	                        Client::sendMessage(%TrueClientId, 0, "Example: #setartifact \"HouseKronos\" \"1,2,3,4\"");
	                  }
	                  else
	                  {
	                        // Parse house name and flag numbers
	                        // Try parsing with quotes first, then fall back to word-based parsing
	                        %quote1 = String::findSubStr(%cropped, "\"");
	                        if(%quote1 != -1)
	                        {
	                              // Quotes found - parse quoted strings
	                              %quote2 = String::ofindSubStr(%cropped, "\"", %quote1 + 1);
	                              if(%quote2 == -1)
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Error: Missing closing quote for house name. Example: #setartifact \"HouseKronos\" \"1,2,3,4\"");
	                                    return;
	                              }
	                              %houseName = String::NEWgetSubStr(%cropped, %quote1 + 1, %quote2 - %quote1 - 1);
	                              
	                              // Parse flag numbers (second quoted string)
	                              %quote3 = String::ofindSubStr(%cropped, "\"", %quote2 + 1);
	                              %quote4 = String::ofindSubStr(%cropped, "\"", %quote3 + 1);
	                              if(%quote3 == -1 || %quote4 == -1)
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Error: Flag numbers must be in quotes. Example: #setartifact \"HouseKronos\" \"1,2,3,4\"");
	                                    return;
	                              }
	                              %flagNumbers = String::NEWgetSubStr(%cropped, %quote3 + 1, %quote4 - %quote3 - 1);
	                        }
	                        else
	                        {
	                              // No quotes - use word-based parsing
	                              %houseName = GetWord(%cropped, 0);
	                              %flagNumbers = GetWord(%cropped, 1);
	                              if(%flagNumbers == -1)
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Error: Missing flag numbers. Example: #setartifact HouseKronos 1,2,3,4");
	                                    return;
	                              }
	                        }
	                        
	                        // Validate house name
	                        if(%houseName != "HouseKronos" && %houseName != "HouseArbal" && %houseName != "HouseCurama" && %houseName != "HouseYuliple")
	                        {
	                              Client::sendMessage(%TrueClientId, 0, "Error: Invalid house name '" @ %houseName @ "'. Must be HouseKronos, HouseArbal, HouseCurama, or HouseYuliple");
	                              return;
	                        }
	                        
	                        // Map flag numbers to flag names
	                        $FlagName[1] = "the Gratigud Artifact";
	                        $FlagName[2] = "the Belaguard Artifact";
	                        $FlagName[3] = "the Crosion Artifact";
	                        $FlagName[4] = "the Murch Artifact";
	                        
	                        // Find MissionGroup
	                        %tempSet = nameToID("MissionGroup");
	                        if(%tempSet == -1)
	                        {
	                              Client::sendMessage(%TrueClientId, 0, "Error: MissionGroup not found!");
	                              return;
	                        }
	                        
	                        // Parse flag numbers (comma-separated) - use the same pattern as other commands
	                        %flagCount = 0;
	                        %flagList = "";
	                        for(%p = String::findSubStr(%flagNumbers, ","); String::findSubStr(%flagNumbers, ",") != -1; %flagNumbers = String::NEWgetSubStr(%flagNumbers, %p+1, 99999))
	                        {
	                              %flagNum = String::NEWgetSubStr(%flagNumbers, 0, %p);
	                              %p = String::findSubStr(%flagNumbers, ",");
	                              if(%flagNum != "")
	                              {
	                                    %flagNum = %flagNum;
	                                    if(%flagNum >= 1 && %flagNum <= 4)
	                                    {
	                                          %flagName = $FlagName[%flagNum];
	                                          
	                                          // Find this flag in MissionGroup
	                                          %flagFound = false;
	                                          for(%j = 0; %j < Group::objectCount(%tempSet); %j++)
	                                          {
	                                                %obj = Group::getObject(%tempSet, %j);
	                                                if(GameBase::getDataName(%obj) == "flag" && %obj.objectiveName == %flagName)
	                                                {
	                                                      // If flag is being carried, drop it first
	                                                      if(%obj.carrier != -1)
	                                                      {
	                                                            %carrier = %obj.carrier;
	                                                            %carrier.carryFlag = "";
	                                                            Player::setItemCount(%carrier, Flag, 0);
	                                                            %obj.carrier = -1;
	                                                      }
	                                                      
	                                                      // Remove from old house's count
	                                                      %oldHouse = %obj.holdingTeam;
	                                                      if(%oldHouse != "" && %oldHouse != -1 && %oldHouse != "None")
	                                                      {
	                                                            $FlagCommand[%oldHouse]--;
	                                                            if($FlagCommand[%oldHouse] < 0)
	                                                                  $FlagCommand[%oldHouse] = 0;
	                                                      }
	                                                      
	                                                      // Unlink from old flagstand if any
	                                                      if(%obj.flagStand != "")
	                                                      {
	                                                            %obj.flagStand.flag = "";
	                                                            %obj.flagStand = "";
	                                                      }
	                                                      
	                                                      // Make sure flag is visible
	                                                      Item::hide(%obj, false);
	                                                      
	                                                      // Find the specific flagstand for this flag number
	                                                      // Flag numbers map to flagstand numbers: 1=Gratigud->stand1, 2=Belaguard->stand2, 3=Crosion->stand3, 4=Murch->stand4
	                                                      // Flagstand names: HouseName1, HouseName2, HouseName3, HouseName4 (e.g., Kronos1, Arbal2, etc.)
	                                                      %flagstandName = "";
	                                                      if(%houseName == "HouseKronos")
	                                                            %flagstandName = "Kronos" @ %flagNum;
	                                                      else if(%houseName == "HouseArbal")
	                                                            %flagstandName = "Arbal" @ %flagNum;
	                                                      else if(%houseName == "HouseCurama")
	                                                            %flagstandName = "Curama" @ %flagNum;
	                                                      else if(%houseName == "HouseYuliple")
	                                                            %flagstandName = "Yuliple" @ %flagNum;
	                                                      
	                                                      %flagstandFound = false;
	                                                      %standObj = -1;
	                                                      
	                                                      // Search for flagstand - try multiple methods
	                                                      // Method 1: Try nameToID with different paths
	                                                      %standObj = nameToID("MissionGroup/team0/" @ %flagstandName);
	                                                      if(%standObj == -1)
	                                                            %standObj = nameToID("team0/" @ %flagstandName);
	                                                      if(%standObj == -1)
	                                                            %standObj = nameToID(%flagstandName);
	                                                      
	                                                      if(%standObj != -1 && GameBase::getDataName(%standObj) == "FlagStand")
	                                                      {
	                                                            %flagstandFound = true;
	                                                      }
	                                                      
	                                                      // Method 2: Search through team0 group specifically
	                                                      if(!%flagstandFound)
	                                                      {
	                                                            %team0Group = nameToID("MissionGroup/team0");
	                                                            if(%team0Group == -1)
	                                                                  %team0Group = nameToID("team0");
	                                                            if(%team0Group != -1)
	                                                            {
	                                                                  for(%k = 0; %k < Group::objectCount(%team0Group); %k++)
	                                                                  {
	                                                                        %standObj = Group::getObject(%team0Group, %k);
	                                                                        %dataName = GameBase::getDataName(%standObj);
	                                                                        if(%dataName == "FlagStand")
	                                                                        {
	                                                                              %mapName = GameBase::getMapName(%standObj);
	                                                                              if(%mapName == %flagstandName)
	                                                                              {
	                                                                                    %flagstandFound = true;
	                                                                                    break;
	                                                                              }
	                                                                        }
	                                                                  }
	                                                            }
	                                                      }
	                                                      
	                                                      // Method 3: Search recursively through MissionGroup
	                                                      if(!%flagstandFound)
	                                                      {
	                                                            // First try direct children of MissionGroup
	                                                            for(%k = 0; %k < Group::objectCount(%tempSet); %k++)
	                                                            {
	                                                                  %standObj = Group::getObject(%tempSet, %k);
	                                                                  %dataName = GameBase::getDataName(%standObj);
	                                                                  if(%dataName == "FlagStand")
	                                                                  {
	                                                                        %mapName = GameBase::getMapName(%standObj);
	                                                                        if(%mapName == %flagstandName)
	                                                                        {
	                                                                              %flagstandFound = true;
	                                                                              break;
	                                                                        }
	                                                                  }
	                                                            }
	                                                            
	                                                            // If not found, search in sub-groups
	                                                            if(!%flagstandFound)
	                                                            {
	                                                                  for(%k = 0; %k < Group::objectCount(%tempSet); %k++)
	                                                                  {
	                                                                        %subGroup = Group::getObject(%tempSet, %k);
	                                                                        if(Group::objectCount(%subGroup) > 0)
	                                                                        {
	                                                                              // Search direct children first
	                                                                              for(%m = 0; %m < Group::objectCount(%subGroup); %m++)
	                                                                              {
	                                                                                    %standObj = Group::getObject(%subGroup, %m);
	                                                                                    %dataName = GameBase::getDataName(%standObj);
	                                                                                    if(%dataName == "FlagStand")
	                                                                                    {
	                                                                                          %mapName = GameBase::getMapName(%standObj);
	                                                                                          if(%mapName == %flagstandName)
	                                                                                          {
	                                                                                                %flagstandFound = true;
	                                                                                                break;
	                                                                                          }
	                                                                                    }
	                                                                              }
	                                                                              
	                                                                              // If not found, search nested sub-groups (for team0 which might have nested structure)
	                                                                              if(!%flagstandFound)
	                                                                              {
	                                                                                    for(%m = 0; %m < Group::objectCount(%subGroup); %m++)
	                                                                                    {
	                                                                                          %nestedGroup = Group::getObject(%subGroup, %m);
	                                                                                          if(Group::objectCount(%nestedGroup) > 0)
	                                                                                          {
	                                                                                                for(%n = 0; %n < Group::objectCount(%nestedGroup); %n++)
	                                                                                                {
	                                                                                                      %standObj = Group::getObject(%nestedGroup, %n);
	                                                                                                      %dataName = GameBase::getDataName(%standObj);
	                                                                                                      if(%dataName == "FlagStand")
	                                                                                                      {
	                                                                                                            %mapName = GameBase::getMapName(%standObj);
	                                                                                                            if(%mapName == %flagstandName)
	                                                                                                            {
	                                                                                                                  %flagstandFound = true;
	                                                                                                                  break;
	                                                                                                            }
	                                                                                                      }
	                                                                                                }
	                                                                                                if(%flagstandFound)
	                                                                                                      break;
	                                                                                          }
	                                                                                    }
	                                                                              }
	                                                                              
	                                                                              if(%flagstandFound)
	                                                                                    break;
	                                                                        }
	                                                                  }
	                                                            }
	                                                      }
	                                                      
	                                                      if(%flagstandFound)
	                                                      {
	                                                            // Unlink any existing flag from this flagstand
	                                                            if(%standObj.flag != "")
	                                                            {
	                                                                  %oldFlag = %standObj.flag;
	                                                                  %oldFlag.flagStand = "";
	                                                                  %oldFlag.holdingTeam = -1;
	                                                                  %oldFlag.team = -1;
	                                                                  GameBase::setTeam(%oldFlag, -1);
	                                                            }
	                                                            
	                                                            // Link flag to flagstand
	                                                            %obj.flagStand = %standObj;
	                                                            %standObj.flag = %obj;
	                                                            %flagstandPos = GameBase::getPosition(%standObj);
	                                                            GameBase::setPosition(%obj, %flagstandPos);
	                                                            Item::hide(%obj, false);
	                                                            Item::setVelocity(%obj, "0 0 0");
	                                                      }
	                                                      else
	                                                      {
	                                                            // Fallback: Use hardcoded positions from mission file if flagstand not found
	                                                            %flagstandPos = "";
	                                                            if(%houseName == "HouseKronos")
	                                                            {
	                                                                  if(%flagNum == 1)
	                                                                        %flagstandPos = "-3226.61 1714.51 1584.5";
	                                                                  else if(%flagNum == 2)
	                                                                        %flagstandPos = "-3225.61 1711.81 1584.5";
	                                                                  else if(%flagNum == 3)
	                                                                        %flagstandPos = "-3229.61 1711.11 1584.5";
	                                                                  else if(%flagNum == 4)
	                                                                        %flagstandPos = "-3230.11 1714.01 1584.5";
	                                                            }
	                                                            else if(%houseName == "HouseArbal")
	                                                            {
	                                                                  if(%flagNum == 1)
	                                                                        %flagstandPos = "-2043.81 -3010.75 394.5";
	                                                                  else if(%flagNum == 2)
	                                                                        %flagstandPos = "-2047 -3011.11 394.5";
	                                                                  else if(%flagNum == 3)
	                                                                        %flagstandPos = "-2047.81 -3007.5 394.5";
	                                                                  else if(%flagNum == 4)
	                                                                        %flagstandPos = "-2045 -3006.75 394.5";
	                                                            }
	                                                            else if(%houseName == "HouseCurama")
	                                                            {
	                                                                  if(%flagNum == 1)
	                                                                        %flagstandPos = "-4637.11 -127.591 179.5";
	                                                                  else if(%flagNum == 2)
	                                                                        %flagstandPos = "-4635.51 -124.791 179.5";
	                                                                  else if(%flagNum == 3)
	                                                                        %flagstandPos = "-4631.81 -126.391 179.5";
	                                                                  else if(%flagNum == 4)
	                                                                        %flagstandPos = "-4632.75 -129.5 179.5";
	                                                            }
	                                                            else if(%houseName == "HouseYuliple")
	                                                            {
	                                                                  if(%flagNum == 1)
	                                                                        %flagstandPos = "-196.891 -2225.61 164.5";
	                                                                  else if(%flagNum == 2)
	                                                                        %flagstandPos = "-194.891 -2228.25 164.5";
	                                                                  else if(%flagNum == 3)
	                                                                        %flagstandPos = "-198.091 -2231.11 164.5";
	                                                                  else if(%flagNum == 4)
	                                                                        %flagstandPos = "-200.391 -2228.61 164.5";
	                                                            }
	                                                            
	                                                            if(%flagstandPos != "")
	                                                            {
	                                                                  // Position flag at hardcoded location
	                                                                  GameBase::setPosition(%obj, %flagstandPos);
	                                                                  Item::hide(%obj, false);
	                                                                  Item::setVelocity(%obj, "0 0 0");
	                                                            }
	                                                      }
	                                                      
	                                                      // Set flag properties
	                                                      %obj.holdingTeam = %houseName;
	                                                      %obj.team = %houseName;
	                                                      GameBase::setTeam(%obj, 0);
	                                                      
	                                                      // Update house count
	                                                      $FlagCommand[%houseName]++;
	                                                      
	                                                      %flagCount++;
	                                                      if(%flagList == "")
	                                                            %flagList = %flagName;
	                                                      else
	                                                            %flagList = %flagList @ ", " @ %flagName;
	                                                      
	                                                      %flagFound = true;
	                                                      break;
	                                                }
	                                          }
	                                          
	                                          if(!%flagFound)
	                                          {
	                                                Client::sendMessage(%TrueClientId, 0, "Warning: Flag " @ %flagName @ " not found!");
	                                          }
	                                    }
	                                    else
	                                    {
	                                          Client::sendMessage(%TrueClientId, 0, "Warning: Invalid flag number " @ %flagNum @ " (must be 1-4)");
	                                    }
	                              }
	                        }
	                        // Handle last flag number (after final comma)
	                        if(%flagNumbers != "")
	                        {
	                              %flagNum = %flagNumbers;
	                              if(%flagNum >= 1 && %flagNum <= 4)
	                              {
	                                    %flagName = $FlagName[%flagNum];
	                                    
	                                    // Find this flag in MissionGroup
	                                    %flagFound = false;
	                                    for(%j = 0; %j < Group::objectCount(%tempSet); %j++)
	                                    {
	                                          %obj = Group::getObject(%tempSet, %j);
	                                          if(GameBase::getDataName(%obj) == "flag" && %obj.objectiveName == %flagName)
	                                          {
	                                                // Remove from old house's count
	                                                %oldHouse = %obj.holdingTeam;
	                                                if(%oldHouse != "" && %oldHouse != -1 && %oldHouse != "None")
	                                                {
	                                                      $FlagCommand[%oldHouse]--;
	                                                      if($FlagCommand[%oldHouse] < 0)
	                                                            $FlagCommand[%oldHouse] = 0;
	                                                }
	                                                
	                                                // If flag is being carried, drop it first
	                                                if(%obj.carrier != -1)
	                                                {
	                                                      %carrier = %obj.carrier;
	                                                      %carrier.carryFlag = "";
	                                                      Player::setItemCount(%carrier, Flag, 0);
	                                                      %obj.carrier = -1;
	                                                }
	                                                
	                                                // Unlink from old flagstand if any
	                                                if(%obj.flagStand != "")
	                                                {
	                                                      %obj.flagStand.flag = "";
	                                                      %obj.flagStand = "";
	                                                }
	                                                
	                                                // Make sure flag is visible
	                                                Item::hide(%obj, false);
	                                                
	                                                // Find the specific flagstand for this flag number
	                                                %flagstandName = "";
	                                                if(%houseName == "HouseKronos")
	                                                      %flagstandName = "Kronos" @ %flagNum;
	                                                else if(%houseName == "HouseArbal")
	                                                      %flagstandName = "Arbal" @ %flagNum;
	                                                else if(%houseName == "HouseCurama")
	                                                      %flagstandName = "Curama" @ %flagNum;
	                                                else if(%houseName == "HouseYuliple")
	                                                      %flagstandName = "Yuliple" @ %flagNum;
	                                                
	                                                %flagstandFound = false;
	                                                %standObj = -1;
	                                                
	                                                // Search for flagstand - try multiple methods
	                                                // Method 1: Try nameToID with different paths
	                                                %standObj = nameToID("MissionGroup/team0/" @ %flagstandName);
	                                                if(%standObj == -1)
	                                                      %standObj = nameToID("team0/" @ %flagstandName);
	                                                if(%standObj == -1)
	                                                      %standObj = nameToID(%flagstandName);
	                                                
	                                                if(%standObj != -1 && GameBase::getDataName(%standObj) == "FlagStand")
	                                                {
	                                                      %flagstandFound = true;
	                                                }
	                                                
	                                                // Method 2: Search through team0 group specifically
	                                                if(!%flagstandFound)
	                                                {
	                                                      %team0Group = nameToID("MissionGroup/team0");
	                                                      if(%team0Group == -1)
	                                                            %team0Group = nameToID("team0");
	                                                      if(%team0Group != -1)
	                                                      {
	                                                            for(%k = 0; %k < Group::objectCount(%team0Group); %k++)
	                                                            {
	                                                                  %standObj = Group::getObject(%team0Group, %k);
	                                                                  %dataName = GameBase::getDataName(%standObj);
	                                                                  if(%dataName == "FlagStand")
	                                                                  {
	                                                                        %mapName = GameBase::getMapName(%standObj);
	                                                                        if(%mapName == %flagstandName)
	                                                                        {
	                                                                              %flagstandFound = true;
	                                                                              break;
	                                                                        }
	                                                                  }
	                                                            }
	                                                      }
	                                                }
	                                                
	                                                      // Method 3: Search recursively through MissionGroup
	                                                      if(!%flagstandFound)
	                                                      {
	                                                            // First try direct children of MissionGroup
	                                                            for(%k = 0; %k < Group::objectCount(%tempSet); %k++)
	                                                            {
	                                                                  %standObj = Group::getObject(%tempSet, %k);
	                                                                  %dataName = GameBase::getDataName(%standObj);
	                                                                  if(%dataName == "FlagStand")
	                                                                  {
	                                                                        %mapName = GameBase::getMapName(%standObj);
	                                                                        if(%mapName == %flagstandName)
	                                                                        {
	                                                                              %flagstandFound = true;
	                                                                              break;
	                                                                        }
	                                                                  }
	                                                            }
	                                                      
	                                                      // If not found, search in sub-groups
	                                                      if(!%flagstandFound)
	                                                      {
	                                                            for(%k = 0; %k < Group::objectCount(%tempSet); %k++)
	                                                            {
	                                                                  %subGroup = Group::getObject(%tempSet, %k);
	                                                                  if(Group::objectCount(%subGroup) > 0)
	                                                                  {
	                                                                        for(%m = 0; %m < Group::objectCount(%subGroup); %m++)
	                                                                        {
	                                                                              %standObj = Group::getObject(%subGroup, %m);
	                                                                              %dataName = GameBase::getDataName(%standObj);
	                                                                              if(%dataName == "FlagStand")
	                                                                              {
	                                                                                    %mapName = GameBase::getMapName(%standObj);
	                                                                                    if(%mapName == %flagstandName)
	                                                                                    {
	                                                                                          %flagstandFound = true;
	                                                                                          break;
	                                                                                    }
	                                                                              }
	                                                                        }
	                                                                        if(%flagstandFound)
	                                                                              break;
	                                                                  }
	                                                            }
	                                                      }
	                                                }
	                                                
	                                                if(%flagstandFound)
	                                                {
	                                                      // Unlink any existing flag from this flagstand
	                                                      if(%standObj.flag != "")
	                                                      {
	                                                            %oldFlag = %standObj.flag;
	                                                            %oldFlag.flagStand = "";
	                                                            %oldFlag.holdingTeam = -1;
	                                                            %oldFlag.team = -1;
	                                                            GameBase::setTeam(%oldFlag, -1);
	                                                      }
	                                                      
	                                                      // Link flag to flagstand
	                                                      %obj.flagStand = %standObj;
	                                                      %standObj.flag = %obj;
	                                                      %flagstandPos = GameBase::getPosition(%standObj);
	                                                      GameBase::setPosition(%obj, %flagstandPos);
	                                                      Item::hide(%obj, false);
	                                                      Item::setVelocity(%obj, "0 0 0");
	                                                }
	                                                else
	                                                {
	                                                      // Fallback: Use hardcoded positions from mission file if flagstand not found
	                                                      %flagstandPos = "";
	                                                      if(%houseName == "HouseKronos")
	                                                      {
	                                                            if(%flagNum == 1)
	                                                                  %flagstandPos = "-3226.61 1714.51 1584.5";
	                                                            else if(%flagNum == 2)
	                                                                  %flagstandPos = "-3225.61 1711.81 1584.5";
	                                                            else if(%flagNum == 3)
	                                                                  %flagstandPos = "-3229.61 1711.11 1584.5";
	                                                            else if(%flagNum == 4)
	                                                                  %flagstandPos = "-3230.11 1714.01 1584.5";
	                                                      }
	                                                      else if(%houseName == "HouseArbal")
	                                                      {
	                                                            if(%flagNum == 1)
	                                                                  %flagstandPos = "-2043.81 -3010.75 394.5";
	                                                            else if(%flagNum == 2)
	                                                                  %flagstandPos = "-2047 -3011.11 394.5";
	                                                            else if(%flagNum == 3)
	                                                                  %flagstandPos = "-2047.81 -3007.5 394.5";
	                                                            else if(%flagNum == 4)
	                                                                  %flagstandPos = "-2045 -3006.75 394.5";
	                                                      }
	                                                      else if(%houseName == "HouseCurama")
	                                                      {
	                                                            if(%flagNum == 1)
	                                                                  %flagstandPos = "-4637.11 -127.591 179.5";
	                                                            else if(%flagNum == 2)
	                                                                  %flagstandPos = "-4635.51 -124.791 179.5";
	                                                            else if(%flagNum == 3)
	                                                                  %flagstandPos = "-4631.81 -126.391 179.5";
	                                                            else if(%flagNum == 4)
	                                                                  %flagstandPos = "-4632.75 -129.5 179.5";
	                                                      }
	                                                      else if(%houseName == "HouseYuliple")
	                                                      {
	                                                            if(%flagNum == 1)
	                                                                  %flagstandPos = "-196.891 -2225.61 164.5";
	                                                            else if(%flagNum == 2)
	                                                                  %flagstandPos = "-194.891 -2228.25 164.5";
	                                                            else if(%flagNum == 3)
	                                                                  %flagstandPos = "-198.091 -2231.11 164.5";
	                                                            else if(%flagNum == 4)
	                                                                  %flagstandPos = "-200.391 -2228.61 164.5";
	                                                      }
	                                                      
	                                                      if(%flagstandPos != "")
	                                                      {
	                                                            // Position flag at hardcoded location
	                                                            GameBase::setPosition(%obj, %flagstandPos);
	                                                            Item::hide(%obj, false);
	                                                            Item::setVelocity(%obj, "0 0 0");
	                                                      }
	                                                }
	                                                
	                                                // Set flag properties
	                                                %obj.holdingTeam = %houseName;
	                                                %obj.team = %houseName;
	                                                GameBase::setTeam(%obj, 0);
	                                                
	                                                // Update house count
	                                                $FlagCommand[%houseName]++;
	                                                
	                                                %flagCount++;
	                                                if(%flagList == "")
	                                                      %flagList = %flagName;
	                                                else
	                                                      %flagList = %flagList @ ", " @ %flagName;
	                                                
	                                                %flagFound = true;
	                                                break;
	                                          }
	                                    }
	                                    
	                                    if(!%flagFound)
	                                    {
	                                          Client::sendMessage(%TrueClientId, 0, "Warning: Flag " @ %flagName @ " not found!");
	                                    }
	                              }
	                              else
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Warning: Invalid flag number " @ %flagNum @ " (must be 1-4)");
	                              }
	                        }
	                        
	                        // Update objectives display
	                        UpdateHouseObjectivesDisplay();
	                        
	                        if(%flagCount > 0)
	                        {
	                              echo("[ADMIN]: " @ %TCsenderName @ " assigned " @ %flagCount @ " flag(s) to " @ %houseName);
	                              Client::sendMessage(%TrueClientId, 0, "Assigned " @ %flagCount @ " flag(s) to " @ %houseName @ ": " @ %flagList);
	                              messageAll(0, "[ADMIN] " @ %TCsenderName @ " assigned " @ %flagCount @ " artifact(s) to " @ %houseName);
	                        }
	                        else
	                        {
	                              Client::sendMessage(%TrueClientId, 0, "No flags were assigned. Check your flag numbers.");
	                        }
	                  }
	            }
			return;
	      }
		if(%w1 == "#setbase")
		{
	            if(%clientToServerAdminLevel >= 4)
	            {
	                  if(%cropped == "")
	                  {
	                        Client::sendMessage(%TrueClientId, 0, "Usage: #setbase \"HouseName\" \"1,2,3,4\"");
	                        Client::sendMessage(%TrueClientId, 0, "Base numbers: 1=Tower0, 2=Tower1, 3=Tower2, 4=Tower3");
	                        Client::sendMessage(%TrueClientId, 0, "Example: #setbase \"HouseKronos\" \"1,2,3,4\"");
	                  }
	                  else
	                  {
	                        // Parse house name and base numbers
	                        // Try parsing with quotes first, then fall back to word-based parsing
	                        %quote1 = String::findSubStr(%cropped, "\"");
	                        if(%quote1 != -1)
	                        {
	                              // Quotes found - parse quoted strings
	                              %quote2 = String::ofindSubStr(%cropped, "\"", %quote1 + 1);
	                              if(%quote2 == -1)
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Error: Missing closing quote for house name. Example: #setbase \"HouseKronos\" \"1,2,3,4\"");
	                                    return;
	                              }
	                              %houseName = String::NEWgetSubStr(%cropped, %quote1 + 1, %quote2 - %quote1 - 1);
	                              
	                              // Parse base numbers (second quoted string)
	                              %quote3 = String::ofindSubStr(%cropped, "\"", %quote2 + 1);
	                              %quote4 = String::ofindSubStr(%cropped, "\"", %quote3 + 1);
	                              if(%quote3 == -1 || %quote4 == -1)
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Error: Base numbers must be in quotes. Example: #setbase \"HouseKronos\" \"1,2,3,4\"");
	                                    return;
	                              }
	                              %baseNumbers = String::NEWgetSubStr(%cropped, %quote3 + 1, %quote4 - %quote3 - 1);
	                        }
	                        else
	                        {
	                              // No quotes - use word-based parsing
	                              %houseName = GetWord(%cropped, 0);
	                              %baseNumbers = GetWord(%cropped, 1);
	                              if(%baseNumbers == -1)
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Error: Missing base numbers. Example: #setbase HouseKronos 1,2,3,4");
	                                    return;
	                              }
	                        }
	                        
	                        // Validate house name
	                        if(%houseName != "HouseKronos" && %houseName != "HouseArbal" && %houseName != "HouseCurama" && %houseName != "HouseYuliple")
	                        {
	                              Client::sendMessage(%TrueClientId, 0, "Error: Invalid house name '" @ %houseName @ "'. Must be HouseKronos, HouseArbal, HouseCurama, or HouseYuliple");
	                              return;
	                        }
	                        
	                        // Map base numbers to tower indices (1=Tower0, 2=Tower1, 3=Tower2, 4=Tower3)
	                        $TowerName[1] = "Tower0";
	                        $TowerName[2] = "Tower1";
	                        $TowerName[3] = "Tower2";
	                        $TowerName[4] = "Tower3";
	                        
	                        // Parse base numbers (comma-separated)
	                        %baseCount = 0;
	                        %baseList = "";
	                        for(%p = String::findSubStr(%baseNumbers, ","); String::findSubStr(%baseNumbers, ",") != -1; %baseNumbers = String::NEWgetSubStr(%baseNumbers, %p+1, 99999))
	                        {
	                              %baseNum = String::NEWgetSubStr(%baseNumbers, 0, %p);
	                              %p = String::findSubStr(%baseNumbers, ",");
	                              if(%baseNum != "")
	                              {
	                                    %baseNum = %baseNum;
	                                    if(%baseNum >= 1 && %baseNum <= 4)
	                                    {
	                                          %towerName = $TowerName[%baseNum];
	                                          %towerIndex = %baseNum - 1; // Convert to 0-based index
	                                          
	                                          // Find the tower group
	                                          %towerGroup = nameToID("MissionGroup/" @ %towerName);
	                                          if(%towerGroup != -1)
	                                          {
	                                                // Find the TowerSwitch in this group
	                                                %switchFound = false;
	                                                for(%j = 0; %j < Group::objectCount(%towerGroup); %j++)
	                                                {
	                                                      %obj = Group::getObject(%towerGroup, %j);
	                                                      if(GameBase::getDataName(%obj) == "TowerSwitch")
	                                                      {
	                                                            %switchName = %obj.objectiveName;
	                                                            
	                                                            // Get old house owner
	                                                            %oldHouse = %obj.team;
	                                                            if(%oldHouse == "" || %oldHouse == -1)
	                                                                  %oldHouse = "None";
	                                                            
	                                                            // Decrement old house's BaseControl if it was owned
	                                                            if(%oldHouse != "None" && %oldHouse != "")
	                                                            {
	                                                                  $BaseControl[%oldHouse]--;
	                                                                  if($BaseControl[%oldHouse] < 0)
	                                                                        $BaseControl[%oldHouse] = 0;
	                                                            }
	                                                            
	                                                            // Set new house ownership
	                                                            %obj.team = %houseName;
	                                                            
	                                                            // Set all objects in tower group to team 0
	                                                            for(%k = 0; %k < Group::objectCount(%towerGroup); %k++)
	                                                            {
	                                                                  %towerObj = Group::getObject(%towerGroup, %k);
	                                                                  GameBase::setTeam(%towerObj, 0);
	                                                            }
	                                                            
	                                                            // Increment new house's BaseControl
	                                                            $BaseControl[%houseName]++;
	                                                            
	                                                            %baseCount++;
	                                                            if(%baseList == "")
	                                                                  %baseList = %switchName;
	                                                            else
	                                                                  %baseList = %baseList @ ", " @ %switchName;
	                                                            
	                                                            %switchFound = true;
	                                                            break;
	                                                      }
	                                                }
	                                                
	                                                if(!%switchFound)
	                                                {
	                                                      Client::sendMessage(%TrueClientId, 0, "Warning: TowerSwitch not found in " @ %towerName);
	                                                }
	                                          }
	                                          else
	                                          {
	                                                Client::sendMessage(%TrueClientId, 0, "Warning: Tower group " @ %towerName @ " not found!");
	                                          }
	                                    }
	                                    else
	                                    {
	                                          Client::sendMessage(%TrueClientId, 0, "Warning: Invalid base number " @ %baseNum @ " (must be 1-4)");
	                                    }
	                              }
	                        }
	                        
	                        // Handle last base number (no trailing comma)
	                        if(%baseNumbers != "")
	                        {
	                              %baseNum = %baseNumbers;
	                              if(%baseNum >= 1 && %baseNum <= 4)
	                              {
	                                    %towerName = $TowerName[%baseNum];
	                                    %towerIndex = %baseNum - 1;
	                                    
	                                    // Find the tower group
	                                    %towerGroup = nameToID("MissionGroup/" @ %towerName);
	                                    if(%towerGroup != -1)
	                                    {
	                                          // Find the TowerSwitch in this group
	                                          %switchFound = false;
	                                          for(%j = 0; %j < Group::objectCount(%towerGroup); %j++)
	                                          {
	                                                %obj = Group::getObject(%towerGroup, %j);
	                                                if(GameBase::getDataName(%obj) == "TowerSwitch")
	                                                {
	                                                      %switchName = %obj.objectiveName;
	                                                      
	                                                      // Get old house owner
	                                                      %oldHouse = %obj.team;
	                                                      if(%oldHouse == "" || %oldHouse == -1)
	                                                            %oldHouse = "None";
	                                                      
	                                                      // Decrement old house's BaseControl if it was owned
	                                                      if(%oldHouse != "None" && %oldHouse != "")
	                                                      {
	                                                            $BaseControl[%oldHouse]--;
	                                                            if($BaseControl[%oldHouse] < 0)
	                                                                  $BaseControl[%oldHouse] = 0;
	                                                      }
	                                                      
	                                                      // Set new house ownership
	                                                      %obj.team = %houseName;
	                                                      
	                                                      // Set all objects in tower group to team 0
	                                                      for(%k = 0; %k < Group::objectCount(%towerGroup); %k++)
	                                                      {
	                                                            %towerObj = Group::getObject(%towerGroup, %k);
	                                                            GameBase::setTeam(%towerObj, 0);
	                                                      }
	                                                      
	                                                      // Increment new house's BaseControl
	                                                      $BaseControl[%houseName]++;
	                                                      
	                                                      %baseCount++;
	                                                      if(%baseList == "")
	                                                            %baseList = %switchName;
	                                                      else
	                                                            %baseList = %baseList @ ", " @ %switchName;
	                                                      
	                                                      %switchFound = true;
	                                                      break;
	                                                }
	                                          }
	                                          
	                                          if(!%switchFound)
	                                          {
	                                                Client::sendMessage(%TrueClientId, 0, "Warning: TowerSwitch not found in " @ %towerName);
	                                          }
	                                    }
	                                    else
	                                    {
	                                          Client::sendMessage(%TrueClientId, 0, "Warning: Tower group " @ %towerName @ " not found!");
	                                    }
	                              }
	                              else
	                              {
	                                    Client::sendMessage(%TrueClientId, 0, "Warning: Invalid base number " @ %baseNum @ " (must be 1-4)");
	                              }
	                        }
	                        
	                        // Update objectives display
	                        UpdateHouseObjectivesDisplay();
	                        
	                        if(%baseCount > 0)
	                        {
	                              echo("[ADMIN]: " @ %TCsenderName @ " assigned " @ %baseCount @ " base(s) to " @ %houseName);
	                              Client::sendMessage(%TrueClientId, 0, "Assigned " @ %baseCount @ " base(s) to " @ %houseName @ ": " @ %baseList);
	                              messageAll(0, "[ADMIN] " @ %TCsenderName @ " assigned " @ %baseCount @ " base(s) to " @ %houseName);
	                        }
	                        else
	                        {
	                              Client::sendMessage(%TrueClientId, 0, "No bases were assigned. Check your base numbers.");
	                        }
	                  }
	            }
			return;
	      }
		if(%w1 == "#recalcobjectives")
		{
			if(%clientToServerAdminLevel >= 1)
			{
				RecalcHouseObjectives();
				echo("[ADMIN]: " @ %TCsenderName @ " recalculated house objectives.");
				Client::sendMessage(%TrueClientId, 0, "House objectives recalculated and display refreshed.");
			}
			return;
		}
		if(%w1 == "#setspawnmultiplier")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				%c1 = GetWord(%cropped, 0);
	
				if(%c1 != -1)
				{
					$spawnMultiplier = Cap(%c1, 0, "inf");
					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "spawnMultiplier set to " @ $spawnMultiplier @ ".");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify a number (normal should be 1. 0 will cease spawning.)");
			}
			return;
		}
	      if(%w1 == "#jail")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
				%c3 = GetWord(%cropped, 2);
	
	                  if(%c1 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
	                        {
						%c1 = Client::getName(%id);
						if(%c2 == -1)
							%c2 = 300;
						if(%c3 == -1)
							%c3 = GetRandomJailNumber();
	
						%pos = GetPositionForJailNumber(%c3);
						if(%pos != -1)
						{
							Jail(%id, %c2, %c3);
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %c1 @ " has been jailed for " @ %c2 @ " seconds in Jail #" @ %c3 @ ".");
						}
						else
		                              Client::sendMessage(%TrueClientId, 0, "Invalid jail number.");
	                        }
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name, time, and jail number.");
	            }
			return;
	      }
	      if(%w1 == "#beg")
		{
	            if(%clientToServerAdminLevel >= 1)
	            {
	                  %c1 = GetWord(%cropped, 0);
	                  %c2 = GetWord(%cropped, 1);
	                  if(%c2 == -1)
	                        %c2 = False;
	
	                  if(%c1 != -1)
	                  {
	                        %id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						%ip = Client::getTransportAddress(%id);
						BanList::add(%ip, 300);
	                              Net::kick(%id, "Do not beg from an admin! The next time you might be banned, so quit your begging.");
					}
	                        else
	                              Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	                  }
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
		}
	      if(%w1 == "#onhear")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  if(%cropped != "")
	                  {
					%event = String::findSubStr(%cropped, ">");
					if(%event != -1)
					{
						%info = String::NEWgetSubStr(%cropped, 0, %event);
						%cmd = String::NEWgetSubStr(%cropped, %event, 99999);
					}
					else
						%info	= %cropped;

					%var = GetWord(%info, 4);
					if(String::ICompare(%var, "var") == 0)
						%var = "var";
					else
					{
						%var = "";
						%quote1 = String::findSubStr(%info, "\"");
						%quote2 = String::ofindSubStr(%info, "\"", %quote1+1);
					}
					if(%quote1 != -1 && %quote2 != -1 || %var != "")
					{
						%pname = GetWord(%info, 0);
						%id = NEWgetClientByName(%pname);

						if(%id != -1)
						{
							%pname = Client::getName(%id);	//properly capitalize name
							%radius = GetWord(%info, 1);
							%keep = GetWord(%info, 2);

							if(%keep == "true" || %keep == "false")
							{
								%targetname = GetWord(%info, 3);
								%tid = NEWgetClientByName(%targetname);
								if(String::ICompare(%targetname, "all") == 0 || %tid != -1)
								{
									if(%var != "")
									{
										%vtxt = %var;
										%text = "var";
									}
									else
									{
										%text = String::NEWgetSubStr(%info, %quote1+1, %quote2);
										%vtxt = "|" @ %text @ "|";
									}

									if(%text != "")
									{
										if(%event != -1)
										{
											AddEventCommand(%id, %senderName, "onHear " @ %pname @ " " @ %radius @ " " @ %keep @ " " @ %targetname @ " " @ %vtxt, %cmd);
											if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "onHear event set for " @ %pname @ "(" @ %id @ ") with text: \"" @ %text @ "\"");
										}
										else
											Client::sendMessage(%TrueClientId, 0, "onHear event definition failed.");
									}
									else
										Client::sendMessage(%TrueClientId, 0, "Invalid text.");
								}
								else
									Client::sendMessage(%TrueClientId, 0, "Invalid name. Please specify 'all' or target's name.");
							}
							else
								Client::sendMessage(%TrueClientId, 0, "Specify 'true' or 'false'. 'true' means that the onHear event won't be deleted after use. 'false' is recommended to keep things clean.");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Invalid name.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Quotes for text not found.");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "#onhear name radius keep all/targetname \"text\"/var.");
	            }
			return;
	      }
			if(%w1 == "#if")
			{
	            if(%clientToServerAdminLevel >= 15)
	            {
	                  if(%cropped != "")
	                  {
					%info	= %cropped;

					%para1 = String::findSubStr(%info, "{");
					%para2 = String::ofindSubStr(%info, "}", %para1+1);
					if(%para1 != -1 && %para2 != -1)
					{
						%expression = String::NEWgetSubStr(%info, %para1+1, %para2);
						if((%pw = CheckForProtectedWords(%expression)) == "")
						{
							%command = String::NEWgetSubStr(%info, %para1+%para2+3, 99999);
							%retval = eval("%x = (" @ %expression @ ");");

							if(%retval == 0)
								%r = false;
							else
								%r = true;
		                              if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "(" @ %expression @ ") = " @ %r);
	
							if(%retval && %command != "")
								remoteSay(%clientId, 0, %command, %senderName);
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Protected word '" @ %pw @ "' can't be used in the #if statement.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "{ and } found.");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "#if {expression} command");
	            }
			return;
	      }
	      if(%w1 == "#addskill")
		{
	            if(%clientToServerAdminLevel >= 5)
	            {
				%name = GetWord(%cropped, 0);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					%sid = GetWord(%cropped, 1);
					if($SkillDesc[%sid] != "")
					{
						%sn = floor(GetWord(%cropped, 2));
						if(%sn != 0)
						{
							$PlayerSkill[%id, %sid] += %sn;
							RefreshAll(%id);
							if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Set " @ %name @ " (" @ %id @ ") " @ $SkillDesc[%sid] @ " to " @ $PlayerSkill[%id, %sid]);
						}
					}
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#setvelocity")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
				%name = GetWord(%cropped, 0);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					%max = 5000;
					%x = Cap(floor(GetWord(%cropped, 1)), -%max, %max);
					%y = Cap(floor(GetWord(%cropped, 2)), -%max, %max);
					%z = Cap(floor(GetWord(%cropped, 3)), -%max, %max);

					%vel = %x @ " " @ %y @ " " @ %z;
					Item::setVelocity(%id, %vel);

					if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Set " @ %name @ " (" @ %id @ ") velocity to " @ %vel);
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#getskill")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
				%name = GetWord(%cropped, 0);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					%sid = GetWord(%cropped, 1);
					if($SkillDesc[%sid] != "")
						Client::sendMessage(%TrueClientId, 0, %name @ " (" @ %id @ ") " @ $SkillDesc[%sid] @ " is " @ FormatSkillDisplay(%id, %sid));
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#scheduleblock")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
				%bname = GetWord(%cropped, 0);
				if(%bname != -1)
				{
					if(IsInCommaList($BlockList[%senderName], %bname))
					{
						%delay = GetWord(%cropped, 1);
						if(%delay >= 0.05)
						{
							%repeat = floor(GetWord(%cropped, 2));
							if(%repeat >= 0)
							{
								%rp = (%repeat+1);

								%arglist = String::NEWgetSubStr(%cropped, (String::len(%bname @ %delay @ %repeat @ "  ")+1), 99999);
								if(GetWord(%arglist, 0) != -1)
									%txt = "#call " @ %bname @ " " @ %arglist;
								else
									%txt = "#call " @ %bname;

								for(%sbi = 1; %sbi <= %rp; %sbi++)
									schedule("remoteSay(" @ %clientId @ ", 0, \"" @ %txt @ "\", \"" @ %senderName @ "\");", %delay * %sbi);
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Block " @ %bname @ " scheduled for " @ %repeat @ " repeats at " @ %delay @ " second intervals.");
							}
							else
								Client::sendMessage(%TrueClientId, 0, "Schedule repeat too low, minimum is 0");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Schedule delay too low, minimum is 0.05");
					}
					else
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Block does not exist!");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Incorrect syntax for #scheduleblock blockName delay numRepeat");
	            }
			return;
	      }
		if(%w1 == "#listonhear")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				if(%cropped != "")
				{
					%id = NEWgetClientByName(%cropped);

					if(%id != -1)
					{
						%index = GetEventCommandIndex(%id, "onHear");
						if(%index != -1)
						{
							for(%i2 = 0; (%index2 = GetWord(%index, %i2)) != -1; %i2++)
								Client::sendMessage(%TrueClientId, 0, Client::getName(%id) @ " onHear " @ %index2 @ ": " @ $EventCommand[%id, %index2]);
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name.");
			}
			return;
		}
		if(%w1 == "#clearonhear")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%name = GetWord(%cropped, 0);
				%oindex = GetWord(%cropped, 1);

				if(%name != -1)
				{
					%id = NEWgetClientByName(%name);

					if(%id != -1)
					{
						%index = GetEventCommandIndex(%id, "onHear");
						if(%index != -1)
						{
							for(%i2 = 0; (%index2 = GetWord(%index, %i2)) != -1; %i2++)
							{
								if(floor(%index2) == floor(%oindex) || %oindex == -1)
								{
									$EventCommand[%id, %index2] = "";
									if(!%echoOff) Client::sendMessage(%TrueClientId, 0, Client::getName(%id) @ " onHear " @ %index2 @ " cleared.");
								}
							}
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Incorrect syntax for #clearonhear name [index]. If index is missing or -1, all onHears for name are cleared.");
			}
			return;
		}
	      if(%w1 == "#getvelocity")
		{
	            if(%clientToServerAdminLevel >= 2)
	            {
				%name = GetWord(%cropped, 0);
	
	                  %id = NEWgetClientByName(%name);
	
				if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
					Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
				else if(%id != -1)
				{
					%vel = Item::getVelocity(%id);
					Client::sendMessage(%TrueClientId, 0, %name @ " (" @ %id @ ") velocity: " @ %vel);
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
	            }
			return;
	      }
	      if(%w1 == "#onconsider")
		{
	            if(%clientToServerAdminLevel >= 3)
	            {
	                  if(%cropped != "")
	                  {
					%event = String::findSubStr(%cropped, ">");
					if(%event != -1)
					{
						%info = String::NEWgetSubStr(%cropped, 0, %event);
						%cmd = String::NEWgetSubStr(%cropped, %event, 99999);
					}
					else
						%info	= %cropped;

					%tag = GetWord(%info, 0);
					%object = $tagToObjectId[%tag];

					if(%object != "")
					{
						%radius = GetWord(%info, 1);
						%keep = GetWord(%info, 2);

						if(%keep == "true" || %keep == "false")
						{
							%targetname = GetWord(%info, 3);
							%tid = NEWgetClientByName(%targetname);
							if(String::ICompare(%targetname, "all") == 0 || %tid != -1)
							{
								if(%event != -1)
								{
									AddEventCommand(%tag, %senderName, "onConsider " @ %radius @ " " @ %keep @ " " @ %targetname, %cmd);
									if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "onConsider event set for tagname " @ %tag @ "(" @ %object @ ") for radius " @ %radius);
								}
								else
									Client::sendMessage(%TrueClientId, 0, "onConsider event definition failed.");
							}
							else
								Client::sendMessage(%TrueClientId, 0, "Invalid name. Please specify 'all' or target's name.");
						}
						else
							Client::sendMessage(%TrueClientId, 0, "Specify 'true' or 'false'. 'true' means that the onConsider event won't be deleted after use.");
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid tagname.");
				}
	                  else
	                        Client::sendMessage(%TrueClientId, 0, "#onconsider tagname radius keep all/targetname");
	            }
			return;
	      }
		if(%w1 == "#listonconsider")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				if(%cropped != "")
				{
					%tag = GetWord(%cropped, 0);
					%object = $tagToObjectId[%tag];

					if(%object != "")
					{
						%index = GetEventCommandIndex(%object, "onConsider");
						if(%index != -1)
						{
							for(%i2 = 0; (%index2 = GetWord(%index, %i2)) != -1; %i2++)
								Client::sendMessage(%TrueClientId, 0, %tag @ " (" @ %object @ ") onConsider " @ %index2 @ ": " @ $EventCommand[%object, %index2]);
						}
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid tagname.");
				}
				else
				{
					%list = $DISlist;
					for(%p = String::findSubStr(%list, ","); (%p = String::findSubStr(%list, ",")) != -1; %list = String::NEWgetSubStr(%list, %p+1, 99999))
					{
						%w = String::NEWgetSubStr(%list, 0, %p);
						%object = $tagToObjectId[%w];

						%index = GetEventCommandIndex(%object, "onConsider");
						if(%index != -1)
							Client::sendMessage(%TrueClientId, 0, %w @ ": " @ %index);
					}
				}
			}
			return;
		}
		if(%w1 == "#clearonconsider")
		{
			if(%clientToServerAdminLevel >= 3)
			{
				%tag = GetWord(%cropped, 0);
				%object = $tagToObjectId[%tag];

				if(%object != "")
				{
					%oindex = GetWord(%cropped, 1);

					%index = GetEventCommandIndex(%object, "onConsider");
					if(%index != -1)
					{
						for(%i2 = 0; (%index2 = GetWord(%index, %i2)) != -1; %i2++)
						{
							if(floor(%index2) == floor(%oindex) || %oindex == -1)
							{
								$EventCommand[%object.tag, %index2] = "";
								if(!%echoOff) Client::sendMessage(%TrueClientId, 0, %tag @ " (" @ %object @ ") onConsider " @ %index2 @ " cleared.");
							}
						}
					}
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Incorrect tagname for #clearonconsider tagname [index]. If index is missing or -1, all onConsiders for name are cleared.");
			}

			return;
		}
		if(%w1 == "#exp")
		{
			if(%clientToServerAdminLevel >= 5)
			{
				%c1 = GetWord(%cropped, 0);
				%c2 = GetWord(%cropped, 1);
	
				if(%c1 != -1 && %c2 != -1)
				{
					%id = NEWgetClientByName(%c1);
	
					if(floor(%id.adminLevel) >= floor(%clientToServerAdminLevel) && Client::getName(%id) != %senderName)
						Client::sendMessage(%TrueClientId, 0, "Could not process command: Target admin clearance level too high.");
					else if(%id != -1)
					{
						storeData(%id, "EXP", %c2, "inc");
						HardcodeAIskills(%id);
						Game::refreshClientScore(%id);
						if(!%echoOff) Client::sendMessage(%TrueClientId, 0, "Setting " @ %c1 @ " (" @ %id @ ") EXP to " @ fetchData(%id, "EXP") @ ".");
						echo("[ADMIN]: " @ %TCsenderName @ " set " @ %c1 @ " experience to " @ %c2);
					}
					else
						Client::sendMessage(%TrueClientId, 0, "Invalid player name.");
				}
				else
					Client::sendMessage(%TrueClientId, 0, "Please specify player name & data.");
			}
			return;
		}
	}
	
	//========== BOT TALK ======================================================================================

	if(%botTalk)
	{
		//process TownBot talk

		%initTalk = "";
		for(%i = 0; (%w = GetWord("hail hello hi greetings yo hey sup salutations g'day howdy", %i)) != -1; %i++)
			if(String::ICompare(%cropped, %w) == 0)
				%initTalk = True;
		
		%clientPos = GameBase::getPosition(%TrueClientId);
		%closest = 5000000;
		%closestId = -1;  // CRITICAL: Initialize to -1 so we can detect if no bot was found
		%maxInteractionDist = $maxAIdistVec + 50;  // Add buffer for skill-based range

		for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
		{
			// CRITICAL: Get Player object first for townbots (more reliable than client ID)
			%playerObj = Client::getOwnedObject(%id);
			if(%playerObj == "" || %playerObj == -1)
			{
				// No Player object - skip this bot (may have been despawned)
				continue;
			}
			
			// Get position from Player object first, fallback to client ID
			%botPos = GameBase::getPosition(%playerObj);
			if(%botPos == "" || %botPos == -1)
			{
				// Fallback to client ID if Player object position fails
				%botPos = GameBase::getPosition(%id);
			}
			
			// Validate position is valid before using it
			if(%botPos == "" || %botPos == -1)
			{
				// Invalid position - skip this bot
				continue;
			}
			
			// Quick distance check using squared distance (avoids sqrt calculation)
			%dx = GetWord(%clientPos, 0) - GetWord(%botPos, 0);
			%dy = GetWord(%clientPos, 1) - GetWord(%botPos, 1);
			%dz = GetWord(%clientPos, 2) - GetWord(%botPos, 2);
			
			// Validate distance components are valid numbers
			if(%dx == -1 || %dy == -1 || %dz == -1)
			{
				// Invalid position components - skip this bot
				continue;
			}
			
			%distSq = %dx * %dx + %dy * %dy + %dz * %dz;
			%maxDistSq = %maxInteractionDist * %maxInteractionDist;
			
			// Skip bots that are definitely too far (fast check)
			if(%distSq > %maxDistSq)
			{
				continue;
			}
			
			// Only calculate actual distance if bot is potentially in range
			%dist = Vector::getDistance(%clientPos, %botPos);
	
			if(%dist < %closest)
			{
				%closest = %dist;
				%closestId = %id;
				%closestPos = %botPos;
			}
		}

		// CRITICAL: Validate that a valid bot was found before proceeding
		// If %closestId is still -1 or empty, no valid bot was found - skip interaction
		if(%closestId == -1 || %closestId == "")
		{
			// No valid townbot found - interaction cannot proceed
			return;
		}

		// Get bot name from stored data (town bots are now Player objects, not Item objects)
		// Try BotInfoAiName first, then fall back to player object name property
		%aiName = fetchData(%closestId, "BotInfoAiName");
		
		// CRITICAL: If BotInfoAiName starts with "TownBot_", extract the actual bot name
		// BotInfoAiName format is "TownBot_merchant1", but we need "merchant1" for $BotInfo lookup
		if(String::findSubStr(%aiName, "TownBot_") == 0)
		{
			%aiName = String::getSubStr(%aiName, 8, 999);  // Remove "TownBot_" prefix (8 characters)
		}
		
		// Fallback to Client::getName if BotInfoAiName is invalid
		if(%aiName == "" || %aiName == -1 || %aiName == "0")
		{
			%aiName = Client::getName(%closestId);
			// If Client::getName also fails, try player object name property
			if(%aiName == "" || %aiName == -1 || %aiName == "0")
			{
				%playerObj = Client::getOwnedObject(%closestId);
				if(%playerObj != "" && %playerObj != -1)
					%aiName = %playerObj.name;
			}
		}
		%displayName = $BotInfo[%aiName, NAME];
		
		// CRITICAL FIX: Allow interaction with townbots (team 0) regardless of player's team
		// Townbots are always on team 0 (Citizen), and players should be able to talk to them
		// regardless of their own team. Check if townbot is on team 0 OR if teams match.
		%botTeam = GameBase::getTeam(%closestId);
		%playerTeam = Client::getTeam(%TrueClientId);
		%teamsMatch = (%playerTeam == %botTeam);
		%isTownBotOnTeam0 = (%botTeam == 0);
		
		%maxDistWithSkill = $maxAIdistVec + ($PlayerSkill[%TrueClientId, $SkillSpeech] / 50);
		%distanceCheck = (%closest <= %maxDistWithSkill);
		%teamCheck = (%teamsMatch || %isTownBotOnTeam0);
		
		if(%closest <= %maxDistWithSkill && %teamCheck)
		{
			// Check for idle message (if flag is enabled and 30 minutes have passed)
			%idleMessageShown = false;
			%showIdleMessageFlag = fetchData(%closestId, "ShowIdleMessage");
			if(%showIdleMessageFlag == "true")
			{
				%lastInteractionTime = fetchData(%closestId, "LastInteractionTime");
				%currentTime = getSimTime();
				%timeSinceLastInteraction = %currentTime - %lastInteractionTime;
				%thirtyMinutes = 30 * 60;  // 30 minutes in seconds (1800)
				
				if(%timeSinceLastInteraction >= %thirtyMinutes)
				{
					// Show idle message before normal bot interaction
					AI::sayLater(%TrueClientId, %closestId, "My Kronos - you startled me, I must've dozed off. Sorry about that - Don't tell my boss, please? Anyways - How can I help you today?", True);
					%idleMessageShown = true;
					// Note: Weapons only mount on collision, not on talk or idle message
				}
				// Update last interaction time once (for all bots with idle message flag enabled)
				storeData(%closestId, "LastInteractionTime", %currentTime);
			}
			
			// CRITICAL: Reset conversation state if player hasn't talked to this bot for 10+ seconds
			// This ensures the conversation restarts from the beginning after a timeout
			%lastPlayerInteraction = $LastPlayerInteraction[%closestId, %TrueClientId];
			%currentTime = getSimTime();
			if(%lastPlayerInteraction != "" && %lastPlayerInteraction != -1)
			{
				%timeSinceLastInteraction = %currentTime - %lastPlayerInteraction;
				if(%timeSinceLastInteraction >= 10)  // 10 second timeout
				{
					// Reset conversation state - player will see greeting again
					$state[%closestId, %TrueClientId] = "";
				}
			}
			// Update last player interaction time
			$LastPlayerInteraction[%closestId, %TrueClientId] = %currentTime;
			
			// Note: Weapon mounting on talk removed - now only mounts on collision
			// Extract bot type by removing trailing digits (more reliable than clipTrailingNumbers)
			// This works backwards from the end to find and remove only trailing digits
			%botType = %aiName;
			%len = String::len(%aiName);
			%numStr = "";
			%digitString = "0123456789";
			
			// Find trailing digits (working backwards)
			for(%i = %len - 1; %i >= 0; %i--)
			{
				%char = String::getSubStr(%aiName, %i, 1);
				// Check if character is a digit (0-9 only) - use String::findSubStr to avoid TorqueScript == comparison bug ("E" == "0" evaluates to true)
				if(String::findSubStr(%digitString, %char) != -1)
				{
					%numStr = %char @ %numStr;
				}
				else
				{
					break;
				}
			}
			
			// If we found trailing digits, remove them
			if(%numStr != "")
			{
				%botType = String::getSubStr(%aiName, 0, %len - String::len(%numStr));
			}
			
			// Check if Las Vegas gambling bots handle this
			if(LasVegas_HandleBotDialogue(%botType, %TrueClientId, %closestId, %aiName, %message, %cropped, %initTalk))
				return;

			if(%botType == "merchant")
			{
				//process merchant code
				%trigger[2] = "buy";
				%trigger[3] = "backpack";
				if($state[%closestId, %TrueClientId] == "")
				{
					// Skip initial greeting if idle message was just shown
					if(%initTalk && !%idleMessageShown)
					{
						AI::sayLater(%TrueClientId, %closestId, "Did you come to see what items you can BUY?", True);
						$state[%closestId, %TrueClientId] = 1;
					}
					// If idle message was shown, still set state to 1 so bot can respond to commands
					else if(%idleMessageShown)
					{
						$state[%closestId, %TrueClientId] = 1;
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						// Get shop indices from bot info
						%shopBotName = fetchData(%closestId, "BotInfoAiName");
						if(String::findSubStr(%shopBotName, "TownBot_") == 0)
							%shopBotName = String::getSubStr(%shopBotName, 8, 999);
						%shopIndices = $BotInfo[%shopBotName, SHOP];
						
						// Use Belt::Shop for unified menu (Standard Shop, Buy Accessories, Sell)
						Belt::Shop(%TrueClientId, %closestId, %shopIndices);
						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
			else if(%botType == "banker")
			{
				//process banker code
				%trigger[2] = "deposit";
				%trigger[3] = "withdraw";
				%trigger[4] = "storage";
				%trigger[5] = "backpack";
				%trigger[6] = "belt";
				
				// Check if cropped message contains any command keywords (case-insensitive)
				%hasCommand = False;
				for(%i = 2; %i <= 6; %i++)
				{
					// Check for exact word match (case-insensitive) or substring match
					if(String::ICompare(%cropped, %trigger[%i]) == 0 || String::findSubStr(%cropped, %trigger[%i]) != -1 || String::findSubStr(%message, %trigger[%i]) != -1)
					{
						%hasCommand = True;
						break;
					}
				}
				
				if($state[%closestId, %TrueClientId] == "")
				{
					// Initialize state if greeting OR command is present
					if(%initTalk || %hasCommand)
					{
						// Skip initial greeting if idle message was just shown
						if(%initTalk && !%idleMessageShown)
						{
							SetupBankDefault(%TrueClientId, %closestId);
							AI::sayLater(%TrueClientId, %closestId, "Welcome to the bank. How can I help you today?", True);
							AI::sayLater(%TrueClientId, %closestId, "You may also #say: DEPOSIT, WITHDRAW, STORAGE, BELT or BACKPACK", True);
						}
						// If idle message was shown, still set state to 1 so bot can respond to commands
						$state[%closestId, %TrueClientId] = 1;
					}
				}
				
				// Check for commands when state is 1 (or just became 1)
				if($state[%closestId, %TrueClientId] == 1)
				{
					// Check exact word match (case-insensitive) or substring match in both %cropped and %message
					if(String::ICompare(%cropped, %trigger[2]) == 0 || String::findSubStr(%cropped, %trigger[2]) != -1 || String::findSubStr(%message, %trigger[2]) != -1)
					{
						//deposit question
						AI::sayLater(%TrueClientId, %closestId, "How much do you want me to hold?  You are carrying " @ fetchData(%TrueClientId, "COINS") @ " coins and I have " @ fetchData(%TrueClientId, "BANK") @ " of yours. (AMOUNT/ALL)", True);
						$state[%closestId, %TrueClientId] = 2;
					}
					if(String::ICompare(%cropped, %trigger[3]) == 0 || String::findSubStr(%cropped, %trigger[3]) != -1 || String::findSubStr(%message, %trigger[3]) != -1)
					{
						//withdraw question
						AI::sayLater(%TrueClientId, %closestId, "How much do you want to take out?  You are carrying " @ fetchData(%TrueClientId, "COINS") @ " coins and I have " @ fetchData(%TrueClientId, "BANK") @ " of yours. (AMOUNT/ALL)", True);
						$state[%closestId, %TrueClientId] = 3;
					}
					if(String::ICompare(%cropped, %trigger[4]) == 0 || String::findSubStr(%cropped, %trigger[4]) != -1 || String::findSubStr(%message, %trigger[4]) != -1)
					{
						//storage
						AI::sayLater(%TrueClientId, %closestId, "This is the equipment you have stored here.", True);

						SetupBank(%TrueClientId, %closestId);

						$state[%closestId, %TrueClientId] = "";
					}
					if(String::ICompare(%cropped, %trigger[5]) == 0 || String::findSubStr(%cropped, %trigger[5]) != -1 || String::findSubStr(%message, %trigger[5]) != -1)
					{
						//Store backpack items
						Belt::Store(%TrueClientId,%closestId);
						$state[%closestId, %TrueClientId] = "";
					}
					if(String::ICompare(%cropped, %trigger[6]) == 0 || String::findSubStr(%cropped, %trigger[6]) != -1 || String::findSubStr(%message, %trigger[6]) != -1)
					{
						//Store belt items (same as backpack)
						Belt::Store(%TrueClientId,%closestId);
						$state[%closestId, %TrueClientId] = "";
					}
				}
				else if($state[%closestId, %TrueClientId] == 2)
				{
					//deposit
					if(%cropped == "all")
						%cropped = fetchData(%TrueClientId, "COINS");
		
					%c = floor(%cropped);
					if(%c <= 0)
					{
						AI::sayLater(%TrueClientId, %closestId, "Invalid request.  Your transaction has been cancelled.~wError_Message.wav", True);
					}
					else if(%c <= fetchData(%TrueClientId, "COINS"))
					{
						storeData(%TrueClientId, "BANK", %c, "inc");
						storeData(%TrueClientId, "COINS", %c, "dec");
						RefreshAll(%TrueClientId);
						AI::sayLater(%TrueClientId, %closestId, "You have given me " @ Number::Beautify(%c, -3) @ " coins.  You are now carrying " @ Number::Beautify(fetchData(%TrueClientId, "COINS"), -3) @ " coins and I have " @ Number::Beautify(fetchData(%TrueClientId, "BANK"), -3) @ " of yours.  Have a nice day.", True);

						playSound(SoundMoney1, GameBase::getPosition(%closestId));
					}
					else
					{
						AI::sayLater(%TrueClientId, %closestId, "Sorry, you don't seem to have that many coins.  Your transaction has been cancelled.", True);
					}
					$state[%closestId, %TrueClientId] = "";
				}
				else if($state[%closestId, %TrueClientId] == 3)
				{
					//withdraw
					if(%cropped == "all")
						%cropped = fetchData(%TrueClientId, "BANK");

					%c = floor(%cropped);
					if(%c <= 0)
					{
						AI::sayLater(%TrueClientId, %closestId, "Invalid request.  Your transaction has been cancelled.~wError_Message.wav", True);
					}
					else if(%c <= fetchData(%TrueClientId, "BANK"))
					{
						storeData(%TrueClientId, "COINS", %c, "inc");
						storeData(%TrueClientId, "BANK", %c, "dec");
						RefreshAll(%TrueClientId);
						AI::sayLater(%TrueClientId, %closestId, "I have given you " @ Number::Beautify(%c, -3) @ " coins.  You are now carrying " @ Number::Beautify(fetchData(%TrueClientId, "COINS"), -3) @ " coins and I have " @ Number::Beautify(fetchData(%TrueClientId, "BANK"), -3) @ " of yours.  Have a nice day.", True);

						playSound(SoundMoney1, GameBase::getPosition(%TrueClientId));
					}
					else
					{
						AI::sayLater(%TrueClientId, %closestId, "I'm sorry but you don't have that many coins in my bank.  Your transaction has been cancelled.", True);
					}
					$state[%closestId, %TrueClientId] = "";
				}
			}
			else if(%botType == "assassin")
			{
				//process assassin code
				%trigger[2] = "yes";
				%trigger[3] = "no";
				%trigger[4] = "buy";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						%highest = -1;
						%list = GetPlayerIdList();
						for(%i = 0; (%id = GetWord(%list, %i)) != -1; %i++)
						{
							if(fetchData(%id, "bounty") == "")
								storeData(%id, "bounty", 0);
							if(fetchData(%id, "bounty") > %highest)
							{
								%h = %id;
								%highest = fetchData(%id, "bounty");
							}
						}
						%n = Client::getName(%h);
						%c = fetchData(%h, "bounty");

						AI::sayLater(%TrueClientId, %closestId, "The highest bounty is currently on " @ %n @ " for $" @ %c @ ". Give me someone's name and I'll tell you their bounty, unless you want to [BUY] something." , True);

						$state[%closestId, %TrueClientId] = 1;
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[4]) != -1)
					{
						%cost = GetLCKcost(%TrueClientId);

						AI::sayLater(%TrueClientId, %closestId, "I will sell you one LCK point for $" @ %cost @ ". ([YES]/[NO])", True);
						$state[%closestId, %TrueClientId] = 2;
					}
					else
					{
						%lowest = 99999;
						%h = "";
						%list = GetPlayerIdList();
						for(%i = 0; (%id = GetWord(%list, %i)) != -1; %i++)
						{
							%comp = String::ICompare(%cropped, Client::getName(%id));
							if(%comp < 0) %comp = -%comp;

							if(%comp < %lowest)
							{
								%h = %id;
								%lowest = %comp;
							}
						}
						if(%h != "")
						{
							%l = fetchData(%h, "LVL");
							%c = getFinalCLASS(%h);
							AI::sayLater(%TrueClientId, %closestId, "Are you talking about " @ Client::getName(%h) @ " the Level " @ %l @ " " @ %c @ "? ([YES]/[NO])", True);
							storeData(%TrueClientId, "tmpdata", %h);
							$state[%closestId, %TrueClientId] = 3;
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "I have no idea who you are talking about. Goodbye.", True);
							$state[%closestId, %TrueClientId] = "";
						}
					}
				}
				else if($state[%closestId, %TrueClientId] == 2)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						%cost = GetLCKcost(%TrueClientId);

						if(fetchData(%TrueClientId, "COINS") >= %cost)
						{
							AI::sayLater(%TrueClientId, %closestId, "Here's your LCK point, thanks for your business.", True);
							GiveThisStuff(%TrueClientId, "LCK 1", True);
							storeData(%TrueClientId, "COINS", %cost, "dec");
							RefreshAll(%TrueClientId);
						}
						else
							AI::sayLater(%TrueClientId, %closestId, "You can't afford this.", True);

						$state[%closestId, %TrueClientId] = "";
					}
					else if(String::findSubStr(%message, %trigger[3]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "See ya.", True);
						$state[%closestId, %TrueClientId] = "";
					}
				}
				else if($state[%closestId, %TrueClientId] == 3)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						%id = fetchData(%TrueClientId, "tmpdata");
						if(%id != %TrueClientId)
						{
							%n = Client::getName(%id);
							if(IsInCommaList(fetchData(%TrueClientId, "TempKillList"), %n))
							{
								storeData(%TrueClientId, "TempKillList", RemoveFromCommaList(fetchData(%TrueClientId, "TempKillList"), %n));
								AI::sayLater(%TrueClientId, %closestId, "I see you've killed " @ %n @ ". Here's your reward... " @ fetchData(%id, "bounty") @ " coins. Goodbye.", True);
								AddBounty(%TrueClientId, fetchData(%id, "bounty") / 2);
								storeData(%TrueClientId, "COINS", fetchData(%id, "bounty"), "inc");
								storeData(%id, "bounty", 0);
	
								playSound(SoundMoney1, GameBase::getPosition(%TrueClientId));
								RefreshAll(%TrueClientId);
							}
							else
								AI::sayLater(%TrueClientId, %closestId, %n @ "'s bounty is currently at " @ fetchData(%id, "bounty") @ " coins. Goodbye.", True);
						}
						else
							AI::sayLater(%TrueClientId, %closestId, "You can't get a reward for killing yourself... idiot.", True);

						$state[%closestId, %TrueClientId] = "";
					}
					else if(String::findSubStr(%message, %trigger[3]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "Well then, I have no idea who you are talking about. Goodbye.", True);
						storeData(%TrueClientId, "tmpdata", "");
						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
			else if(%botType == "porter")
			{
				//process porter code
				%trigger[2] = "enter";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						if($arenaOn)
						{
							AI::sayLater(%TrueClientId, %closestId, "I am in charge of admitting fighters.  Do you want to [ENTER] for $" @ $teleportInArenaCost @ "?", True);
							$state[%closestId, %TrueClientId] = 1;
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "I'm sorry but the arena was disabled by your server admin.", True);
							$state[%closestId, %TrueClientId] = "";
						}
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						if(fetchData(%TrueClientId, "COINS") >= $teleportInArenaCost)
						{
							%retval = TeleportToMarker(%TrueClientId, "TheArena\\TeleportEntranceMarkers", 1, 0);
							if(%retval != False)
							{
								storeData(%TrueClientId, "COINS", $teleportInArenaCost, "dec");
								storeData(%TrueClientId, "inArena", True);
								RefreshArenaTextBox(%TrueClientId);
								RefreshAll(%TrueClientId);

								$state[%closestId, %TrueClientId] = "";
							}
							else
							{
								AI::sayLater(%TrueClientId, %closestId, "Hmmm... I guess there are people standing in the way of the teleport destinations.  Try again later.", True);
								$state[%closestId, %TrueClientId] = "";
							}
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "You don't even have that many coins.  Sorry, you can't get in.", True);
							$state[%closestId, %TrueClientId] = "";
						}
					}
				}
			}
			else if(%botType == "duelmanager")
			{
				//process porter code
				%trigger[2] = "leave";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						AI::sayLater(%TrueClientId, %closestId, "Hi, do you want to [LEAVE] the dueling arena?", True);
						$state[%closestId, %TrueClientId] = 1;
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						schedule("storeData(" @ %TrueClientId @ ", \"tmprecall\", \"\");if(Vector::getDistance(\"" @ GameBase::getPosition(%TrueClientId) @ "\", GameBase::getPosition(" @ %TrueClientId @ ")) <= 1){FellOffMap(" @ %TrueClientId @ ");CheckAndBootFromArena(" @ %TrueClientId @ ");$DuelOn = False;}", 20);
						AI::sayLater(%TrueClientId, %closestId, "Stay at your current position for 20 seconds to be teleported out.", True);
						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
			else if(%botType == "hometele")
			{
				
				//process porter code
				%trigger[2] = "yuliple";
				%trigger[3] = "empress";
				%trigger[4] = "curama";
				%trigger[5] = "arbal";
				%trigger[6] = "kronos";
				%trigger[7] = "lamisor";
				if($state[%closestId, %TrueClientId] == "")
				{
					%name = Client::getName(%TrueClientId);
					if(%initTalk && $HomeSpawn[%name] == "")
					{
						AI::sayLater(%TrueClientId, %closestId, "Hey there, this is the home teleport system. You do not own a house so you aren't able to use this.", True);
					}
					else if($HomeSpawn[%name] != "" && %initTalk)
					{
						AI::sayLater(%TrueClientId, %closestId, "Hi there, where would you like to be transported to? [yuliple],[empress],[curama],[arbal],[kronos],[lamisor]", True);
						$state[%closestId, %TrueClientId] = 1;
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = -399 @ " " @ -2325 @ " " @ 78;
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[3]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = -2860.65 @ " " @ -1287.69 @ " " @ 702;
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[4]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = -4518.22 @ " " @ -240.03 @ " " @ 65;
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[5]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = -2026 @ " " @ -2875 @ " " @ 344;
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[6]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = -3364 @ " " @ 1723 @ " " @ 1559;
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[7]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = -2852 @ " " @ 854 @ " " @ 617.5;
						GameBase::setPosition(%TrueClientId, %lospos);
					}
				}	
			}
			else if(%botType == "teleportbot")
			{
				%trigger[2] = "loop";
				%trigger[3] = "demise";
				%trigger[4] = "enigma";
				%trigger[5] = "echos";
				%trigger[6] = "yuliple";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						AI::sayLater(%TrueClientId, %closestId, "Greetings. I can transport you to areas isolated from standard magic. Where would you like to be transported to? [loop],[demise],[enigma],[echos],[yuliple]", True);
						$state[%closestId, %TrueClientId] = 1;
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = "-4904.37 2967.4 618.995";
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[3]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = "-1769 2239 -84";
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[4]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = "-3123 2322 -410";
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[5]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = "893.104 1396 -398";
						GameBase::setPosition(%TrueClientId, %lospos);
					}
					else if(String::findSubStr(%message, %trigger[6]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Good Luck!", True);
						$state[%closestId, %TrueClientId] = "";
						%lospos = "-399 -2325 78";
						GameBase::setPosition(%TrueClientId, %lospos);
					}
				}	
			}
			else if(%botType == "hunt")
			{
				//process quest code
				%trigger[2] = $BotInfo[%aiName, CUE, 1];
				%trigger[3] = $BotInfo[%aiName, NCUE, 1];
				%trigger[4] = "buy";

				if(%initTalk || $state[%closestId, %TrueClientId] != "")
				{
					%hasTheStuff = HasThisStuff(%TrueClientId, $BotInfo[%aiName, NEED]);

					if($BotInfo[%aiName, CSAY] == "" && %hasTheStuff == 666)
						%hasTheStuff = False;
					if($BotInfo[%aiName, LSAY] == "" && %hasTheStuff == 667)
						%hasTheStuff = False;

					if(%hasTheStuff == 666 && $state[%closestId, %TrueClientId] == "")
					{
						if(%initTalk)
						{
							AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, CSAY], True);
							$state[%closestId, %TrueClientId] = -5;
						}
					}
					else if(%hasTheStuff == 667 && $state[%closestId, %TrueClientId] == "")
					{
						if(%initTalk)
						{
							AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, LSAY], True);
							$state[%closestId, %TrueClientId] = -5;
						}
					}
					else if(%hasTheStuff == False)
					{
						if($state[%closestId, %TrueClientId] == "" && $QuestReload[$BotInfo[%aiName, BOT]] == "")
						{
							if(%initTalk)
							{
								AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, SAY, 1] @ " [" @ %trigger[2] @ "]", True);
								$state[%closestId, %TrueClientId] = 1;
							}
						}
						else if($state[%closestId, %TrueClientId] == 1)
						{
							if(String::findSubStr(%message, %trigger[2]) != -1)
							{
								AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, SAY, 2], True);
								if(NEWgetClientByName($BotInfo[%aiName, BOT] @ 0) == -1)
									%n = AI::helper($BotInfo[%aiName, BOT], $BotInfo[%aiName, BOT] @ 0, "TempSpawn " @ $BotInfo[%aiName, POS] @ " " @ 1, default);
								$state[%closestId, %TrueClientId] = "";
								// Per-quest reload override (ticks of 2s each); default 90 (=180s) if unset
								%questReloadTime = $QuestReloadTime[$BotInfo[%aiName, BOT]];
								if(%questReloadTime == "" || %questReloadTime <= 0)
									%questReloadTime = 90;
								$QuestReload[$BotInfo[%aiName, BOT]] = %questReloadTime;

							}
						}
						else if($state[%closestId, %TrueClientId] == "" && $QuestReload[$BotInfo[%aiName, BOT]] != "")
						{
							if(%initTalk)
							{
								AI::sayLater(%TrueClientId, %closestId, "I have no need of you... yet.", True);
							}
						}
					}
					else if(%hasTheStuff == True)
					{
						if($state[%closestId, %TrueClientId] == "")
						{
							if(%initTalk)
							{
								AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, NSAY, 1] @ " [" @ %trigger[3] @ "]", True);
								$state[%closestId, %TrueClientId] = 1;
							}
						}
						else if($state[%closestId, %TrueClientId] == 1)
						{
							if(String::findSubStr(%message, %trigger[3]) != -1)
							{
								if(HasThisStuff(%TrueClientId, $BotInfo[%aiName, NEED]))
								{
									if($BotInfo[%aiName, TAKE] != "")
										TakeThisStuff(%TrueClientId, $BotInfo[%aiName, TAKE], True);
									if($BotInfo[%aiName, GIVE] != "")
										GiveThisStuff(%TrueClientId, $BotInfo[%aiName, GIVE], True);
	
									AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, NSAY, 2], True);
								}
								else
									AI::sayLater(%TrueClientId, %closestId, "Nice try, I'm keeping what I managed to get from you.", True);
	
								$state[%closestId, %TrueClientId] = "";
								Game::refreshClientScore(%TrueClientId);
							}
						}
					}

					%t4 = String::findSubStr(%message, %trigger[4]);
					if(%t4 != -1 || ($state[%closestId, %TrueClientId] == -5 && %t4 != -1))
					{
						if($BotInfo[%aiName, SHOP] != "")
						{
							// Use Belt::Shop for unified menu (Standard Shop, Buy Accessories, Sell)
							%shopIndices = $BotInfo[%aiName, SHOP];
							Belt::Shop(%TrueClientId, %closestId, %shopIndices);
						}
						else
							AI::sayLater(%TrueClientId, %closestId, "I have nothing to sell.", True);

						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
			else if(%botType == "quest")
			{
				//process quest code
				// Try lookup with current aiName first, then try with TownBot_ prefix if empty
				%trigger[2] = $BotInfo[%aiName, CUE, 1];
				%trigger[3] = $BotInfo[%aiName, NCUE, 1];
				
				// Fallback: If empty, try with TownBot_ prefix
				if(%trigger[2] == "")
				{
					%fullName = "TownBot_" @ %aiName;
					%trigger[2] = $BotInfo[%fullName, CUE, 1];
				}
				if(%trigger[3] == "")
				{
					%fullName = "TownBot_" @ %aiName;
					%trigger[3] = $BotInfo[%fullName, NCUE, 1];
				}
				
				%trigger[4] = "buy";

				if(%initTalk || $state[%closestId, %TrueClientId] != "")
				{
					%hasTheStuff = HasThisStuff(%TrueClientId, $BotInfo[%aiName, NEED]);

					if($BotInfo[%aiName, CSAY] == "" && %hasTheStuff == 666)
						%hasTheStuff = False;
					if($BotInfo[%aiName, LSAY] == "" && %hasTheStuff == 667)
						%hasTheStuff = False;

					if(%hasTheStuff == 666 && $state[%closestId, %TrueClientId] == "")
					{
						if(%initTalk)
						{
							AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, CSAY], True);
							$state[%closestId, %TrueClientId] = -5;
						}
					}
					else if(%hasTheStuff == 667 && $state[%closestId, %TrueClientId] == "")
					{
						if(%initTalk)
						{
							AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, LSAY], True);
							$state[%closestId, %TrueClientId] = -5;
						}
					}
					else if(%hasTheStuff == False)
					{
						if($state[%closestId, %TrueClientId] == "")
						{
							if(%initTalk)
							{
								// Only show trigger word in brackets if it exists
								%messageText = $BotInfo[%aiName, SAY, 1];
								if(%trigger[2] != "")
									%messageText = %messageText @ " [" @ %trigger[2] @ "]";
								AI::sayLater(%TrueClientId, %closestId, %messageText, True);
								$state[%closestId, %TrueClientId] = 1;
							}
						}
						else if($state[%closestId, %TrueClientId] == 1)
						{
							// CRITICAL: Only check for trigger if it's not empty. Empty string will match anything!
							if(%trigger[2] != "" && String::findSubStr(%message, %trigger[2]) != -1)
							{
								AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, SAY, 2], True);
								$state[%closestId, %TrueClientId] = "";
							}
						}
					}
					else if(%hasTheStuff == True)
					{
						if($state[%closestId, %TrueClientId] == "")
						{
							if(%initTalk)
							{
								// Only show trigger word in brackets if it exists
								%messageText = $BotInfo[%aiName, NSAY, 1];
								if(%trigger[3] != "")
									%messageText = %messageText @ " [" @ %trigger[3] @ "]";
								AI::sayLater(%TrueClientId, %closestId, %messageText, True);
								$state[%closestId, %TrueClientId] = 1;
							}
						}
						else if($state[%closestId, %TrueClientId] == 1)
						{
							// CRITICAL: Only check for trigger if it's not empty. Empty string will match anything!
							if(%trigger[3] != "" && String::findSubStr(%message, %trigger[3]) != -1)
							{
								if(HasThisStuff(%TrueClientId, $BotInfo[%aiName, NEED]))
								{
									if($BotInfo[%aiName, TAKE] != "")
										TakeThisStuff(%TrueClientId, $BotInfo[%aiName, TAKE], True);
									if($BotInfo[%aiName, GIVE] != "")
										GiveThisStuff(%TrueClientId, $BotInfo[%aiName, GIVE], True);

									AI::sayLater(%TrueClientId, %closestId, $BotInfo[%aiName, NSAY, 2], True);
								}
								else
								{
									AI::sayLater(%TrueClientId, %closestId, "Nice try, I'm keeping what I managed to get from you.", True);
								}

								$state[%closestId, %TrueClientId] = "";
								Game::refreshClientScore(%TrueClientId);
							}
						}
					}

					%t4 = String::findSubStr(%message, %trigger[4]);
					if(%t4 != -1 || ($state[%closestId, %TrueClientId] == -5 && %t4 != -1))
					{
						if($BotInfo[%aiName, SHOP] != "")
						{
							// Use Belt::Shop for unified menu (Standard Shop, Buy Accessories, Sell)
							%shopIndices = $BotInfo[%aiName, SHOP];
							Belt::Shop(%TrueClientId, %closestId, %shopIndices);
						}
						else
							AI::sayLater(%TrueClientId, %closestId, "I have nothing to sell.", True);

						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
			else if(%botType == "dailyquest")
			{
				// Daily Herald (DailyQuest.cs). Unlike "quest"/"hunt" bots this is NOT
				// $BotInfo NEED/GIVE-driven: contracts are per-player, resolved by
				// Daily::Accept against the talker's benchmark and stored via storeData.
				if(%initTalk || $state[%closestId, %TrueClientId] != "")
				{
					// completed contract? pay out on any interaction
					if(Daily::TryTurnIn(%TrueClientId))
					{
						AI::sayLater(%TrueClientId, %closestId, "The kingdom thanks you. Until tomorrow!", True);
						$state[%closestId, %TrueClientId] = "";
					}
					else if(%initTalk)
					{
						AI::sayLater(%TrueClientId, %closestId, "Greetings! I post the kingdom's daily bounties. Say [FETCH], [CULL] or [ELITE] to accept one. #daily shows your progress.", True);
						Daily::Status(%TrueClientId);
						$state[%closestId, %TrueClientId] = 1;
					}
					else if($state[%closestId, %TrueClientId] == 1)
					{
						if(String::findSubStr(%message, "fetch") != -1)
						{
							Daily::Accept(%TrueClientId, "Fetch");
							$state[%closestId, %TrueClientId] = "";
						}
						else if(String::findSubStr(%message, "cull") != -1)
						{
							Daily::Accept(%TrueClientId, "Cull");
							$state[%closestId, %TrueClientId] = "";
						}
						else if(String::findSubStr(%message, "elite") != -1)
						{
							Daily::Accept(%TrueClientId, "Elite");
							$state[%closestId, %TrueClientId] = "";
						}
					}
				}
			}
			else if(%botType == "tournyquest")
			{
				//process quest code
				%trigger[2] = "enter";
				%trigger[3] = "yes";
				%trigger[4] = "buy";

				if(%initTalk || $state[%closestId, %TrueClientId] != "")
				{
					%cost = (((fetchData(%TrueClientId, "TournyRank") * 5) * 5000) + 10000) * fetchData(%TrueClientId, "TournyRank") + 10000;

					if(fetchData(%TrueClientId, "LVL") < 40)
					{
						if($state[%closestId, %TrueClientId] == "")
						{
							if(%initTalk)
							{
								AI::sayLater(%TrueClientId, %closestId, "You are much to weak to speak with me! Off with you!", True);
							}
						}
					}
					else if(fetchData(%TrueClientId, "TournyRank") <= 9 && fetchData(%TrueClientId, "LVL") >= 40)
					{
						if($state[%closestId, %TrueClientId] == "")
						{
							if(%initTalk)
							{
								AI::sayLater(%TrueClientId, %closestId, "Hello there adventurer, I'm the Colloseum administrator. Here you can fight monsters to gain rank and receive special bonuses. However, you cannot run from this and you must pay " @ %cost @ " coins to enter! Do you wish to [ENTER]?", True);
								$state[%closestId, %TrueClientId] = 1;
							}
						}
						else if($state[%closestId, %TrueClientId] == 1 && $Contestant == "")
						{
							if(String::findSubStr(%message, %trigger[2]) != -1)
							{
								if(fetchData(%TrueClientId, "COINS") >= %cost)
								{
									storeData(%TrueClientId, "COINS", %cost, "dec");
									AI::sayLater(%TrueClientId, %closestId, "Good luck friend.", True);
									%lospos = -3588 @ " " @ -2364 @ " " @ 354;
									GameBase::setPosition(%TrueClientId, %lospos);
									%name = Client::getName(%TrueClientId);
									$Contestant = %name;
									$Fight = fetchData(%TrueClientId, "TournyRank");
									$Round = 0;
									for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
											Client::sendMessage(%cl, $MsgBeige, %TCsenderName @ " has entered the Colloseum!");
									schedule("Colloseum();",4);
								}
								else
									AI::sayLater(%TrueClientId, %closestId, "You do not have enough coins to enter.", True);
	
								$state[%closestId, %TrueClientId] = "";
							}
						}
					}
				}
			}
			else if(%botType == "sealnpc")
			{
				//process quest code
				%trigger[2] = "enter";
				%trigger[3] = "yes";
				%trigger[4] = "buy";

				if(%initTalk || $state[%closestId, %TrueClientId] != "")
				{
					if(fetchData(%TrueClientId, "LVL") < 40)
					{
						if($state[%closestId, %TrueClientId] == "")
						{
							if(%initTalk)
							{
								AI::sayLater(%TrueClientId, %closestId, "You are much to weak to speak with me! Off with you!", True);
							}
						}
					}
					else if(fetchData(%TrueClientId, "LVL") >= 40)
					{
						if($state[%closestId, %TrueClientId] == "")
						{
							if(%initTalk)
							{
								AI::sayLater(%TrueClientId, %closestId, "Hello there adventurer, I'm the seal battle admin. Here you can fight monsters to raise the global seal value. However, you cannot run from this! Do you wish to [ENTER]?", True);
								$state[%closestId, %TrueClientId] = 1;
							}
						}
						else if($state[%closestId, %TrueClientId] == 1)
						{
							if(String::findSubStr(%message, %trigger[2]) != -1)
							{
							// Check if seal battle can start BEFORE teleporting
							if(!SealBattle::CanStart(%TrueClientId))
							{
								// CanStart already sent the error message
								$state[%closestId, %TrueClientId] = "";
								return;
							}
							
							for(%w = 0; (%a = GetWord(%tmpl, %w)) != -1; %w++)
							{
								%n = CountObjInList(%tmppartylist[%a]);
								for(%ww = 0; (%aa = GetWord(%tmppartylist[%a], %ww)) != -1; %ww++)
									%partyFactor[%aa] = %n;
								}
								for(%i = 1; %i <= %nameCount; %i++)
								if(%finalDamagedBy[%i] != "")
								%listClientId = NEWgetClientByName(%finalDamagedBy[%i]);
								GameBase::setPosition(%TrueCliendId, %pos);
								AI::sayLater(%TrueClientId, %closestId, "Good luck friend.", True);
								%pos = "-3588 -2364 354";
								%seal = 1;
								GameBase::setPosition(%TrueClientId,%pos);
								%name = Client::getName(%TrueClientId);
								SealBattle::Begin(%TrueClientId,%pos,%seal);
								// Message is already sent in SealBattle::Begin()
								$state[%closestId, %TrueClientId] = "";
							}
						}
					}
				}
			}
			else if(%botType == "ascensionnpc")
			{
				// Ascension NPC - opens menu-based talent shop
				if(%initTalk)
				{
					AI::sayLater(%TrueClientId, %closestId, "Greetings, seeker of power. Choose the talent you wish to unlock.", True);
					SetupAscensionShop(%TrueClientId, %closestId, 0);
				}
			}
			else if(%botType == "sigilforge")
			{
				// SIGKILL Sigil vendor - hand sigils in for coins, or barter for weapons (see SigilForge.cs)
				if(%initTalk)
				{
					AI::sayLater(%TrueClientId, %closestId, "The forge hungers for SIGKILL Sigils. What do you seek?", True);
					SetupSigilForge(%TrueClientId, %closestId);
				}
			}
			else if(%botType == "manager")
			{
				//process manager code
				%trigger[2] = "fight";
				%trigger[3] = "leave";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						AI::sayLater(%TrueClientId, %closestId, "Hail. Do you wish to [FIGHT] or [LEAVE]?", True);
						$state[%closestId, %TrueClientId] = 1;
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						//FIGHT
						%x = AddToRoster(%TrueClientId);
						if(%x != -1)
						{
							TeleportToMarker(%TrueClientId, "TheArena\\WaitingRoomMarkers", 0, 1);

							$state[%closestId, %TrueClientId] = "";
						}
						else
						{
							//arena is full
							AI::sayLater(%TrueClientId, %closestId, "Sorry, the arena roster is full right now.", True);
							$state[%closestId, %TrueClientId] = "";
						}
					}
					else if(String::findSubStr(%message, %trigger[3]) != -1)
					{
						//LEAVE
						%retval = TeleportToMarker(%TrueClientId, "TheArena\\TeleportExitMarkers", 1, 0);

						if(%retval != False)
						{
							storeData(%TrueClientId, "inArena", "");
							CloseArenaTextBox(%TrueClientId);

							$state[%closestId, %TrueClientId] = "";
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "Hmmm... I guess there are people standing in the way of the teleport destinations.  Try again later.", True);
							$state[%closestId, %TrueClientId] = "";
						}
					}
				}
			}
			if(%botType == "botmaker")
			{
				//process botmaker code
				%trigger[2] = "yes";
				%trigger[3] = "no";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						if(CountObjInCommaList($PetList) >= $maxPets)
						{
							AI::sayLater(%TrueClientId, %closestId, "I'm sorry but all my helpers are already on duty.", True);
							$state[%closestId, %TrueClientId] = "";
						}
						else if(CountObjInCommaList(fetchData(%TrueClientId, "PersonalPetList")) >= $maxPetsPerPlayer)
						{
							AI::sayLater(%TrueClientId, %closestId, "I'm sorry but you have too many helpers currently at your disposal.", True);
							$state[%closestId, %TrueClientId] = "";
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "I have all sorts of helpers at my disposal. Tell me which class you are interested in. [mage] [fighter] [paladin] [ranger] [thief] [bard] [cleric] [druid]", True);
							$state[%closestId, %TrueClientId] = 1;
						}
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					%class = GetWord(%cropped, 0);
					%gender = GetWord(%cropped, 1);
					%defaults = $BotInfo[%aiName, DEFAULTS, %class];
					if(%gender == -1)
						%gender = "Male";

					if(String::ICompare(%gender, "male") == 0)
					{
						%gender = "Male";
						%gflag = True;
					}
					else if(String::ICompare(%gender, "female") == 0)
					{
						%gender = "Female";
						%gflag = True;
					}

					if(String::ICompare(%class, "mage") == 0)
						%class = "Mage";
					else if(String::ICompare(%class, "fighter") == 0)
						%class = "Fighter";
					else if(String::ICompare(%class, "paladin") == 0)
						%class = "Paladin";
					else if(String::ICompare(%class, "thief") == 0)
						%class = "Thief";
					else if(String::ICompare(%class, "bard") == 0)
						%class = "Bard";
					else if(String::ICompare(%class, "ranger") == 0)
						%class = "Ranger";
					else if(String::ICompare(%class, "cleric") == 0)
						%class = "Cleric";
					else if(String::ICompare(%class, "druid") == 0)
						%class = "Druid";

					if(%defaults != "")
					{
						if(%gflag)
						{
							%lvl = GetStuffStringCount(%defaults, "LVL");
							%nc = pow(%lvl, 2) * 3;
							$tmpdata[%TrueClientId, 1] = %class;
							$tmpdata[%TrueClientId, 2] = %gender;
							$tmpdata[%TrueClientId, 3] = %nc;	//just so the equation is only in one place.

							AI::sayLater(%TrueClientId, %closestId, "My " @ %class @ "s are Level " @ %lvl @ ", and will cost you " @ %nc @ " coins. [yes] [no]", True);
							$state[%closestId, %TrueClientId] = 2;
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "Invalid gender. Use 'male' or 'female'.", True);
							$state[%closestId, %TrueClientId] = "";
						}
					}
					else
					{
						AI::sayLater(%TrueClientId, %closestId, "Invalid class. Use any of the following: mage fighter paladin ranger thief bard cleric druid.", True);
						$state[%closestId, %TrueClientId] = "";
					}
				}
				else if($state[%closestId, %TrueClientId] == 2)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						%nc = $tmpdata[%TrueClientId, 3];

						if(%nc <= 0)
						{
							AI::sayLater(%TrueClientId, %closestId, "Invalid request.  Your transaction has been cancelled.~wError_Message.wav", True);
							$state[%closestId, %TrueClientId] = "";
						}
						else if(%nc <= fetchData(%TrueClientId, "COINS"))
						{
							%class = $tmpdata[%TrueClientId, 1];
							%gender = $tmpdata[%TrueClientId, 2];
							%defaults = $BotInfo[%aiName, DEFAULTS, %class];
							%lvl = GetStuffStringCount(%defaults, "LVL");
	
							storeData(%TrueClientId, "COINS", %nc, "dec");
							playSound(SoundMoney1, GameBase::getPosition(%closestId));
							RefreshAll(%TrueClientId);
	
							%n = "";
							for(%i = 0; (%a = GetWord($BotInfo[%aiName, NAMES], %i)) != -1; %i++)
							{
								if(NEWgetClientByName(%a) == -1)
								{
									%n = %a;
									break;
								}
							}
							if(%n == "")
								%n = "generic";

							$BotEquipment[generic] = "CLASS " @ %class @ " " @ %defaults;
							%an = AI::helper("generic", %n, "TempSpawn " @ GameBase::getPosition($BotInfo[%aiName, DESTSPAWN]) @ " " @ GameBase::getTeam(%TrueClientId));
							if(%an == "" || %an == -1)
							{
								Client::sendMessage(%TrueClientId, 0, "Failed to spawn pet " @ %n @ ".");
								echo("[ADMIN]: Failed to spawn pet " @ %n);
							}
							else
							{
								%id = AI::getId(%an);
								if(%id == -1 || %id == "")
								{
									Client::sendMessage(%TrueClientId, 0, "Pet " @ %an @ " spawned but could not get ID.");
									echo("[ADMIN]: Pet " @ %an @ " spawned but AI::getId failed.");
								}
								else
								{
									ChangeRace(%id, %gender @ "Human");
									storeData(%id, "tmpbotdata", %TrueClientId);
									storeData(%id, "botAttackMode", 2);

									schedule("Pet::BeforeTurnEvil(" @ %id @ ");", 55*60, Client::getOwnedObject(%id));
									schedule("Pet::TurnEvil(" @ %id @ ");", 60*60, Client::getOwnedObject(%id));
								}
							}

							$PetList = AddToCommaList($PetList, %id);
							storeData(%TrueClientId, "PersonalPetList", AddToCommaList(fetchData(%TrueClientId, "PersonalPetList"), %id));
							storeData(%id, "petowner", %TrueClientId);
						
							AI::sayLater(%TrueClientId, %closestId, "This is " @ %n @ ", a Level " @ %lvl @ " " @ %class @ "! He is at your disposal. He will follow you around and fight for you for the next hour.", True);
							$state[%closestId, %TrueClientId] = "";
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "You don't have enough coins. Goodbye.", True);
							$state[%closestId, %TrueClientId] = "";
						}

					}
					else if(String::findSubStr(%message, %trigger[3]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "As you wish. Goodbye.", True);
						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
			if(%botType == "blacksmith")
			{
				//process botmaker code
				%trigger[2] = "buy";
				%trigger[3] = "smith";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						AI::sayLater(%TrueClientId, %closestId, "Hail friend, are you here to have me [SMITH] an old weapon?", True);
						$state[%closestId, %TrueClientId] = 1;
					}
				}
				else if($state[%closestId, %TrueClientId] == 1)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						if($BotInfo[%aiName, SHOP] != "")
						{
							// Use Belt::Shop for unified menu (Standard Shop, Buy Accessories, Sell)
							%shopIndices = $BotInfo[%aiName, SHOP];
							Belt::Shop(%TrueClientId, %closestId, %shopIndices);
						}
						else
							AI::sayLater(%TrueClientId, %closestId, "I have nothing to sell.", True);

						$state[%closestId, %TrueClientId] = "";
					}
					if(String::findSubStr(%message, %trigger[3]) != -1)
					{
						AI::sayLater(%TrueClientId, %closestId, "Click Use on an item and I will tell you how much it will cost to smith. Click Use on this item again and I will get to work.", True);
						SetupBlacksmith(%TrueClientId, %closestId);

						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
			else if(%botType == "guildmaster")
			{
				//process guildmaster code
				%trigger[2] = "join";
				if($state[%closestId, %TrueClientId] == "")
				{
					if(%initTalk)
					{
						if(fetchData(%TrueClientId, "LVL") >= 25)
						{
							%h = fetchData(%TrueClientId, "MyHouse");
							// Treat "0", "House0", and "house0" as empty (no house) - new players might have these invalid values
							if(%h == "0" || %h == "House0" || %h == "house0")
							{
								%h = "";
								// Clear the invalid house value
								storeData(%TrueClientId, "MyHouse", "");
							}
							if(%h == "")
							{
								AI::sayLater(%TrueClientId, %closestId, "Hello adventurer. Are you interested in joining a House for " @ $joinHouseCost @ " coins? [join]", True);
								$state[%closestId, %TrueClientId] = 1;
							}
							else
							{
								AI::sayLater(%TrueClientId, %closestId, "Members of " @ %h @ " are welcome here. Are you interested in joining a new House for " @ $changeHouseCost @ " coins? [join]", True);
								$state[%closestId, %TrueClientId] = 2;
							}
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "Come back when you are at least level 25. Goodbye.", True);
							$state[%closestId, %TrueClientId] = "";
						}
					}
				}
				if($state[%closestId, %TrueClientId] == 1 || $state[%closestId, %TrueClientId] == 2)
				{
					if(String::findSubStr(%message, %trigger[2]) != -1)
					{
						%ch = fetchData(%TrueClientId, "MyHouse");
						// Treat "0" as empty (no house) - new players might have "0" instead of empty string
						if(%ch == "0")
							%ch = "";
						%hlist = "";
						for(%i = 1; $HouseName[%i] != ""; %i++)
						{
							for(%x = 1; $HouseName[%x] != ""; %x++)
							{
								if($HouseName[%i] != $HouseName[%x] && $HouseName[%i] != %ch && %check == "")
									if($HouseMember[$HouseName[%i]] < $HouseMember[$HouseName[%x]])
										%check = 1;
								
							}
							if(%check != "")
								%hlist = %hlist @ "[" @ $HouseName[%i] @ "] ";
							%check = "";
						}
						if(%hlist == "")
							for(%i = 1; $HouseName[%i] != ""; %i++)
								if($HouseName[%i] != %ch)
									%hlist = %hlist @ "[" @ $HouseName[%i] @ "] ";
						%fhlist = String::NEWgetSubStr(%hlist, 0, String::len(%hlist)-1);

						if($state[%closestId, %TrueClientId] == 1)
						{
							//join new house
							AI::sayLater(%TrueClientId, %closestId, "Which house would you like to join? " @ %fhlist, True);
							$state[%closestId, %TrueClientId] = 3;
						}
						else if($state[%closestId, %TrueClientId] == 2)
						{
							//change house
							AI::sayLater(%TrueClientId, %closestId, "Which house would you like to change to? " @ %fhlist, True);
							$state[%closestId, %TrueClientId] = 4;
						}
					}
				}
				else if($state[%closestId, %TrueClientId] == 3 || $state[%closestId, %TrueClientId] == 4)
				{
					%houseNum = "";
					for(%i = 1; $HouseName[%i] != ""; %i++)
					{
						if(String::ICompare(%cropped, $HouseName[%i]) == 0)
						{
							for(%x = 1; $HouseName[%x] != ""; %x++)
								if($HouseMember[$HouseName[%i]] < $HouseMember[$HouseName[%x]] && $HouseName[%i] != $HouseName[%x])
									%houseNum = %i;
							if($HouseMember[HouseKronos] == $HouseMember[HouseYuliple] && $HouseMember[HouseYuliple] == $HouseMember[HouseCurama] && $HouseMember[HouseCurama] == $HouseMember[HouseArbal])
								%houseNum = %i;
						}
					}

					if(%houseNum != "")
					{
						if($state[%closestId, %TrueClientId] == 3)
							%cost = $joinHouseCost;
						else if($state[%closestId, %TrueClientId] == 4)
							%cost = $changeHouseCost;

						%c = floor(fetchData(%TrueClientId, "COINS"));
						if(%c >= %cost)
						{
							storeData(%TrueClientId, "COINS", %cost, "dec");
							BootFromCurrentHouse(%TrueClientId, True);
							JoinHouse(%TrueClientId, %houseNum, True);
							GiveThisStuff(%TrueClientId, $HouseStartUpEq[%houseNum]);
							RefreshAll(%TrueClientId);
							AI::sayLater(%TrueClientId, %closestId, "Welcome to " @ $HouseName[%houseNum] @ "! Here is your start up equipment. Good luck on your adventures!", True);

							playSound(SoundMoney1, GameBase::getPosition(%TrueClientId));
						}
						else
						{
							AI::sayLater(%TrueClientId, %closestId, "I'm sorry but you do not have enough coins. Goodbye.", True);
						}
						$state[%closestId, %TrueClientId] = "";
					}
					else
					{
						AI::sayLater(%TrueClientId, %closestId, "This house does not exist. Goodbye.", True);
						$state[%closestId, %TrueClientId] = "";
					}
				}
			}
		}
		else
		{
			//This condition occurs when you are talking from too far of any TownBot.  All states are cleared here.
			//This means that potentially, you could initiate a conversation with the banker, travel for an hour
			//WITHOUT saying a word, come back and continue the conversation.  As soon as you speak in a way that
			//townbots hear you (#say, #shout, #tell) and are too far from them, all conversations are reset.

			for(%i = 0; (%id = GetWord($TownBotList, %i)) != -1; %i++)
			{
				$state[%id, %TrueClientId] = "";
			}
		}
	}
}
//function remoteIssueCommand(%commander, %cmdIcon, %command, %wayX, %wayY, %dest1, %dest2, %dest3, %dest4, %dest5, %dest6, %dest7, %dest8, %dest9, %dest10, %dest11, %dest12, %dest13, %dest14)
//{
	// issueCommandI takes waypoint 0-1023 in x,y scaled mission area
	// issueCommand takes float mission coords.
//	for(%i = 1; %dest[%i] != ""; %i = %i + 1)
//		if(!%dest[%i].muted[%commander])
//			issueCommandI(%commander, %dest[%i], %cmdIcon, %command, %wayX, %wayY);
//}

//function remoteIssueTargCommand(%commander, %cmdIcon, %command, %targIdx, %dest1, %dest2, %dest3, %dest4, %dest5, %dest6, %dest7, %dest8, %dest9, %dest10, %dest11, %dest12, %dest13, %dest14)
//{
//	for(%i = 1; %dest[%i] != ""; %i = %i + 1)
//		if(!%dest[%i].muted[%commander])
//			issueTargCommand(%commander, %dest[%i], %cmdIcon, %command, %targIdx);
//}

//function remoteCStatus(%clientId, %status, %message)
//{
	// setCommandStatus returns false if no status was changed.
	// in this case these should just be team says.
//	if(setCommandStatus(%clientId, %status, %message))
//	{
//		if($dedicated)
//			echo("COMMANDSTATUS: " @ %clientId @ " \"" @ escapeString(%message) @ "\"");
//	}
//	else
//		remoteSay(%clientId, true, %message);
//}

function teamMessages(%mtype, %team1, %message1, %team2, %message2, %message3)
{
	// Use Client::getFirst()/getNext() for reliable iteration
	for(%id = Client::getFirst(); %id != -1; %id = Client::getNext(%id))
	{
		if(Client::getTeam(%id) == %team1)
		{
			Client::sendMessage(%id, %mtype, %message1);
		}
		else if(%message2 != "" && Client::getTeam(%id) == %team2)
		{
			Client::sendMessage(%id, %mtype, %message2);
		}
		else if(%message3 != "")
		{
			Client::sendMessage(%id, %mtype, %message3);
		}
	}
}

function messageAll(%mtype, %message, %filter)
{
	dbecho($dbechoMode, "messageAll(" @ %mtype @ ", " @ %message @ ", " @ %filter @ ")");

	if(%filter == "")
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
			Client::sendMessage(%cl, %mtype, %message);
	else
	{
		for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
		{
			if(%cl.messageFilter & %filter)
			Client::sendMessage(%cl, %mtype, %message);
		}
	}
}

function messageAllExcept(%except, %mtype, %message)
{
	dbecho($dbechoMode, "messageAllExcept(" @ %except @ ", " @ %mtype @ ", " @ %message @ ")");

	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(%cl != %except)
			Client::sendMessage(%cl, %mtype, %message);
	}
}

function radiusAllExcept(%except1, %except2, %message)
{
	dbecho($dbechoMode, "radiusAllExcept(" @ %except1 @ ", " @ %except2 @ ", " @ %message @ ")");

	%epos1 = GameBase::getPosition(%except1);
	%epos2 = GameBase::getPosition(%except2);
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		%clpos = GameBase::getPosition(%cl);
		%dist1 = Vector::getDistance(%clpos, %epos1);
		%dist2 = Vector::getDistance(%clpos, %epos2);
		if(%cl != %except1 && %cl != %except2 && !IsDead(%cl))
		{
			if(%dist1 <= $maxSAYdistVec || %dist2 <= $maxSAYdistVec)
				Client::sendMessage(%cl, $MsgBeige, %message);
		}
	}
}

function FadeMsg(%txt, %dist, %max)
{
	dbecho($dbechoMode, "FadeMsg(" @ %txt @ ", " @ %dist @ ", " @ %max @ ")");

	if(%dist <= %max)
		return %txt;
	else
	{
		for(%i = 0; (%z = GetWord(%txt, %i)) != -1; %i++)
			%ntxt = %ntxt @ %z;
		%lntxt = String::len(%ntxt);

		%x = %dist - %max;
		%amt = round((%x / %max) * %lntxt);

		%txt = BuildDotString(%txt, %amt);
		
		return %txt;
	}
}

function BuildDotString(%txt, %n)
{
	dbecho($dbechoMode, "BuildDotString(" @ %txt @ ", " @ %n @ ")");

	%len = String::len(%txt);

	//i currently dont really know any other way to put a certain amount of characters in a string in a random fashion
	//other than to "sprinkle" them on until the count is correct.  Maybe someday someone will decide to rework this
	//function and make it more CPU friendly.  Right now this method sucks.

	%retry = 0;
	for(%i = %n; %i > 0; %i)
	{
		%p = floor(getRandom() * %len);
		%a = String::getSubStr(%txt, %p, 1);
		if(%a != " " && %a != ".")
		{
			%txt = String::getSubStr(%txt, 0, %p) @ "." @ String::getSubStr(%txt, %p+1, 99999);
			%i--;
			%retry = 0;
		}
		else
			%retry++;

		if(%retry > 10)
			break;
	}
	return %txt;
}

function ClearBlockData(%name, %block)
{
	dbecho($dbechoMode, "ClearBlockData(" @ %name @ ", " @ %block @ ")");

	for(%i = 1; $BlockData[%name, %block, %i] != ""; %i++)
		$BlockData[%name, %block, %i] = "";
}

function ManageBlockOwnersList(%name)
{
	dbecho($dbechoMode, "ManageBlockOwnersList(" @ %name @ ")");

	%clientId = NEWgetClientByName(%name);

	if(CountObjInCommaList($BlockList[%name]) > 0)
	{
		if(!IsInCommaList($BlockOwnersList, %name))
		{
			$BlockOwnersList = AddToCommaList($BlockOwnersList, %name);
			if(%name != "Server")
				$BlockOwnerAdminLevel[%name] = floor(%clientId.adminLevel);
		}
	}
	else
	{
		$BlockOwnersList = RemoveFromCommaList($BlockOwnersList, %name);
		if(%name != "Server")
			$BlockOwnerAdminLevel[%name] = "";
	}

	return $BlockOwnersList;
}

function ParseBlockData(%bd, %victimId, %killerId)
{
	dbecho($dbechoMode, "ParseBlockData(" @ %bd @ ", " @ %victimId @ ", " @ %killerId @ ")");

	//the passed variables MUST BE IN COMMALIST FORMAT!

	%vtype[1] = "^victimName";
	%vtype[2] = "^victimId";
	%vtype[3] = "^victimPos";
	%vtype[4] = "^victimRot";
	%vtype[5] = "^victimZoneId";
	%vtype[6] = "^victimZoneType";
	%vtype[7] = "^victimZoneDesc";
	%vtype[8] = "^victimClass";
	%vtype[9] = "^victimLevel";
	%vtype[10] = "^victimX";
	%vtype[11] = "^victimY";
	%vtype[12] = "^victimZ";
	%vtype[13] = "^victimR1";
	%vtype[14] = "^victimR2";
	%vtype[15] = "^victimR3";
	%vtype[16] = "^victimCoins";
	%vtype[17] = "^victimBank";
	%vtype[18] = "^victimVelX";
	%vtype[19] = "^victimVelY";
	%vtype[20] = "^victimVelZ";

	%vtype[21] = "^killerName";
	%vtype[22] = "^killerId";
	%vtype[23] = "^killerPos";
	%vtype[24] = "^killerRot";
	%vtype[25] = "^killerZoneId";
	%vtype[26] = "^killerZoneType";
	%vtype[27] = "^killerZoneDesc";
	%vtype[28] = "^killerClass";
	%vtype[29] = "^killerLevel";
	%vtype[30] = "^killerX";
	%vtype[31] = "^killerY";
	%vtype[32] = "^killerZ";
	%vtype[33] = "^killerR1";
	%vtype[34] = "^killerR2";
	%vtype[35] = "^killerR3";
	%vtype[36] = "^killerCoins";
	%vtype[37] = "^killerBank";
	%vtype[38] = "^killerVelX";
	%vtype[39] = "^killerVelY";
	%vtype[40] = "^killerVelZ";

	if(%victimId != "")
	{
		%vpos = GameBase::getPosition(%victimId);
		%vrot = GameBase::getRotation(%victimId);
		%vvel = Item::getVelocity(%victimId);

		%var[1] = Client::getName(%victimId);
		%var[2] = %victimId;
		%var[3] = %vpos;
		%var[4] = %vrot;
		%var[5] = fetchData(%victimId, "zone");
		%var[6] = Zone::getType(fetchData(%victimId, "zone"));
		%var[7] = Zone::getDesc(fetchData(%victimId, "zone"));
		%var[8] = fetchData(%victimId, "CLASS");
		%var[9] = fetchData(%victimId, "LVL");
		%var[10] = GetWord(%vpos, 0);
		%var[11] = GetWord(%vpos, 1);
		%var[12] = GetWord(%vpos, 2);
		%var[13] = GetWord(%vrot, 0);
		%var[14] = GetWord(%vrot, 1);
		%var[15] = GetWord(%vrot, 2);
		%var[16] = fetchData(%victimId, "COINS");
		%var[17] = fetchData(%victimId, "BANK");
		%var[18] = GetWord(%vvel, 0);
		%var[19] = GetWord(%vvel, 1);
		%var[20] = GetWord(%vvel, 2);
	}
	if(%killerId != "")
	{
		%kpos = GameBase::getPosition(%killerId);
		%krot = GameBase::getRotation(%killerId);
		%kvel = Item::getVelocity(%killerId);

		%var[21] = Client::getName(%killerId);
		%var[22] = %killerId;
		%var[23] = %kpos;
		%var[24] = %krot;
		%var[25] = fetchData(%killerId, "zone");
		%var[26] = Zone::getType(fetchData(%killerId, "zone"));
		%var[27] = Zone::getDesc(fetchData(%killerId, "zone"));
		%var[28] = fetchData(%killerId, "CLASS");
		%var[29] = fetchData(%killerId, "LVL");
		%var[30] = GetWord(%kpos, 0);
		%var[31] = GetWord(%kpos, 1);
		%var[32] = GetWord(%kpos, 2);
		%var[33] = GetWord(%krot, 0);
		%var[34] = GetWord(%krot, 1);
		%var[35] = GetWord(%krot, 2);
		%var[36] = fetchData(%killerId, "COINS");
		%var[37] = fetchData(%killerId, "BANK");
		%var[38] = GetWord(%kvel, 0);
		%var[39] = GetWord(%kvel, 1);
		%var[40] = GetWord(%kvel, 2);
	}

	for(%i = 1; %vtype[%i] != ""; %i++)
		%bd = String::replace(%bd, %vtype[%i], %var[%i], True);

	return %bd;
}

function messageonlyadmins(%color, %message)
{
	for(%i = 2049; %i < 2100; %i++)
	{
		if(Client::getName(%i) != "" && %i.adminLevel > 0)
		{
			Client::sendMessage(%i, %color, %message);
		}
	}
}


