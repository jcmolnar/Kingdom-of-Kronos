//====================================================================================================
// AutoSkill.cs - Automatic Skill Point Spending System
//====================================================================================================
// Allows players to set priority skills that automatically stay maxed as they level up.
// 
// Usage:
//   #autoskill set endurance, weight capacity, slashing
//   #autoskill set 1,2,3,4,5
//   #autoskill all
//   #autoskill show
//   #autoskill hide
//   #autoskill showmsg
//   #autoskill clear
//
// Data is stored in funkvar slot 55 and slot 57 for save/load persistence.
//
//----------------------------------------------------------------------------------------------------
// HOOKS / INTEGRATIONS:
//----------------------------------------------------------------------------------------------------
// Server.cs        - exec(AutoSkill); added to load this script
// rpgstats.cs      - AutoSkill_Process() called in Game::refreshClientScore() on level-up
// rpgfunk.cs       - SaveCharacter() saves AutoSkill_Priority to slot 55 and AutoSkill_Mute to slot 57
// rpgfunk.cs       - LoadCharacter() loads AutoSkill_Priority from slot 55 and AutoSkill_Mute from slot 57
// comchat.cs       - #autoskill command handler added
// skills.cs        - Uses GetNumSkills(), AddSkillPoint(), $SkillDesc[], $PlayerSkill[]
//====================================================================================================

// Get skill ID from skill name (case-insensitive, supports partial matches)
function AutoSkill_GetSkillId(%skillName)
{
	dbecho($dbechoMode, "AutoSkill_GetSkillId(" @ %skillName @ ")");
	
	// Trim whitespace from skill name
	%skillName = String::trim(%skillName);

	// Numeric skill IDs are accepted directly: #autoskill set 1,2,3
	if(%skillName >= 1 && $SkillDesc[%skillName] != "")
	{
		%desc = $SkillDesc[%skillName];
		if(String::findSubStr(%desc, "no longer") == -1 && String::findSubStr(%desc, "No Longer") == -1)
			return %skillName;
	}

	// Check each skill for a match using case-insensitive comparison
	%numSkills = GetNumSkills();
	for(%i = 1; %i <= %numSkills; %i++)
	{
		%desc = $SkillDesc[%i];
		
		// Skip "no longer used" skills
		if(String::findSubStr(%desc, "no longer") != -1)
			continue;
		if(String::findSubStr(%desc, "No Longer") != -1)
			continue;
		
		// Exact match (case-insensitive)
		if(String::ICompare(%desc, %skillName) == 0)
			return %i;
		
		// Check if skill description starts with the input (for partial matching)
		// Use ICompare on the substring
		%inputLen = String::len(%skillName);
		%descStart = String::getSubStr(%desc, 0, %inputLen);
		if(String::ICompare(%descStart, %skillName) == 0)
			return %i;
	}
	
	return -1; // Not found
}

// Get skill name from ID
function AutoSkill_GetSkillName(%skillId)
{
	if(%skillId >= 1 && $SkillDesc[%skillId] != "")
		return $SkillDesc[%skillId];
	return "Unknown";
}

function AutoSkill_HasSkillId(%skillIds, %skillId)
{
	if(%skillIds == "" || %skillId == "" || %skillId == -1)
		return false;

	%wrapped = "," @ %skillIds @ ",";
	if(String::findSubStr(%wrapped, "," @ %skillId @ ",") != -1)
		return true;

	return false;
}

// Safe network message sender to avoid Tribes 255-character crash
// %tag: optional ~category suffix appended to every chunk (e.g. "~stats" so
// the client chat filter can hide automatic level-up spend broadcasts; the
// engine strips everything from "~" for clients without the filter)
function AutoSkill_SendSafeMessage(%clientId, %prefix, %list, %tag)
{
	if(%tag == -1)
		%tag = "";
	%chunk = "";
	%remaining = %list;
	
	while(String::len(%remaining) > 0)
	{
		%commaPos = String::findSubStr(%remaining, ",");
		if(%commaPos == -1)
		{
			%item = %remaining;
			%remaining = "";
		}
		else
		{
			%item = String::getSubStr(%remaining, 0, %commaPos);
			%remaining = String::getSubStr(%remaining, %commaPos + 1, 999);
		}
		
		%item = String::trim(%item);
		if(%item != "")
		{
			if(%chunk == "")
			{
				%chunk = %item;
			}
			else if(String::len(%prefix @ %chunk @ ", " @ %item) < 200)
			{
				%chunk = %chunk @ ", " @ %item;
			}
			else
			{
				Client::sendMessage(%clientId, $MsgBeige, %prefix @ %chunk @ "," @ %tag);
				%prefix = "  "; // Indent subsequent chunks
				%chunk = %item;
			}
		}
	}
	
	if(%chunk != "")
	{
		Client::sendMessage(%clientId, $MsgBeige, %prefix @ %chunk @ %tag);
	}
}

function AutoSkill_SetAll(%clientId)
{
	dbecho($dbechoMode, "AutoSkill_SetAll(" @ %clientId @ ")");

	%skillIds = "";
	%validSkills = "";
	%count = 0;
	%numSkills = GetNumSkills();

	for(%i = 1; %i <= %numSkills; %i++)
	{
		%desc = $SkillDesc[%i];
		if(String::findSubStr(%desc, "no longer") != -1)
			continue;
		if(String::findSubStr(%desc, "No Longer") != -1)
			continue;

		if(%skillIds != "")
			%skillIds = %skillIds @ ",";
		%skillIds = %skillIds @ %i;

		if(%validSkills != "")
			%validSkills = %validSkills @ ", ";
		%validSkills = %validSkills @ AutoSkill_GetSkillName(%i);
		%count++;
	}

	storeData(%clientId, "AutoSkill_Priority", %skillIds);
	AutoSkill_SendSafeMessage(%clientId, "Auto-skill priority set to all active skills: ", %validSkills);
	Client::sendMessage(%clientId, $MsgBeige, "Your SP will automatically keep these skills maxed when you level up.");

	return %count;
}

// Set auto-skill priority list (comma-separated skill names)
function AutoSkill_Set(%clientId, %skillList)
{
	dbecho($dbechoMode, "AutoSkill_Set(" @ %clientId @ ", " @ %skillList @ ")");
	
	%validSkills = "";
	%invalidSkills = "";
	%skillIds = "";
	
	// Parse comma-separated list
	%count = 0;
	for(%i = 0; %i < 50; %i++) // Max 50 iterations to prevent infinite loop
	{
		// Find next comma
		%commaPos = String::findSubStr(%skillList, ",");
		
		if(%commaPos == -1)
		{
			// No more commas, this is the last skill
			%skill = String::trim(%skillList);
			if(%skill != "")
			{
				%skillId = AutoSkill_GetSkillId(%skill);
				if(%skillId != -1)
				{
					if(!AutoSkill_HasSkillId(%skillIds, %skillId))
					{
						if(%skillIds != "")
							%skillIds = %skillIds @ ",";
						%skillIds = %skillIds @ %skillId;

						if(%validSkills != "")
							%validSkills = %validSkills @ ", ";
						%validSkills = %validSkills @ AutoSkill_GetSkillName(%skillId);
						%count++;
					}
				}
				else
				{
					if(%invalidSkills != "")
						%invalidSkills = %invalidSkills @ ", ";
					%invalidSkills = %invalidSkills @ %skill;
				}
			}
			break;
		}
		else
		{
			// Extract skill name before comma
			%skill = String::trim(String::getSubStr(%skillList, 0, %commaPos));
			%skillList = String::getSubStr(%skillList, %commaPos + 1, 999);
			
			if(%skill != "")
			{
				%skillId = AutoSkill_GetSkillId(%skill);
				if(%skillId != -1)
				{
					if(!AutoSkill_HasSkillId(%skillIds, %skillId))
					{
						if(%skillIds != "")
							%skillIds = %skillIds @ ",";
						%skillIds = %skillIds @ %skillId;

						if(%validSkills != "")
							%validSkills = %validSkills @ ", ";
						%validSkills = %validSkills @ AutoSkill_GetSkillName(%skillId);
						%count++;
					}
				}
				else
				{
					if(%invalidSkills != "")
						%invalidSkills = %invalidSkills @ ", ";
					%invalidSkills = %invalidSkills @ %skill;
				}
			}
		}
	}
	
	// Store the skill IDs
	storeData(%clientId, "AutoSkill_Priority", %skillIds);
	
	// Report to player
	if(%count > 0)
	{
		AutoSkill_SendSafeMessage(%clientId, "Auto-skill priority set: ", %validSkills);
		Client::sendMessage(%clientId, $MsgBeige, "Your SP will automatically keep these skills maxed when you level up.");
	}
	else
	{
		Client::sendMessage(%clientId, $MsgRed, "No valid skill names found.");
	}
	
	if(%invalidSkills != "")
	{
		Client::sendMessage(%clientId, $MsgRed, "Unknown skills (ignored): " @ %invalidSkills);
	}
	
	return %count;
}

// Clear auto-skill preferences
function AutoSkill_Clear(%clientId)
{
	dbecho($dbechoMode, "AutoSkill_Clear(" @ %clientId @ ")");
	
	storeData(%clientId, "AutoSkill_Priority", "");
	Client::sendMessage(%clientId, $MsgBeige, "Auto-skill priority cleared. SP will no longer be auto-spent.");
}

// Hide auto-skill level-up messages
function AutoSkill_Hide(%clientId)
{
	dbecho($dbechoMode, "AutoSkill_Hide(" @ %clientId @ ")");
	storeData(%clientId, "AutoSkill_Mute", "true");
	Client::sendMessage(%clientId, $MsgBeige, "Auto-skill level-up messages are now hidden. Use #autoskill showmsg to show them again.");
	SaveCharacter(%clientId);
}

// Show auto-skill level-up messages again
function AutoSkill_ShowMsg(%clientId)
{
	dbecho($dbechoMode, "AutoSkill_ShowMsg(" @ %clientId @ ")");
	storeData(%clientId, "AutoSkill_Mute", "");
	Client::sendMessage(%clientId, $MsgBeige, "Auto-skill level-up messages will now be displayed.");
	SaveCharacter(%clientId);
}


// Show current auto-skill settings
function AutoSkill_Show(%clientId)
{
	dbecho($dbechoMode, "AutoSkill_Show(" @ %clientId @ ")");
	
	%skillIds = fetchData(%clientId, "AutoSkill_Priority");
	
	if(%skillIds == "" || %skillIds == -1)
	{
		Client::sendMessage(%clientId, $MsgBeige, "No auto-skill priority set. Use #autoskill set skill, skill2, ... to configure.");
		return;
	}
	
	// Parse the skill IDs and build display string
	%display = "";
	%priority = 1;
	
	for(%i = 0; %i < 50; %i++) // Max 50 iterations
	{
		%commaPos = String::findSubStr(%skillIds, ",");
		
		if(%commaPos == -1)
		{
			// Last skill ID
			%skillId = %skillIds;
			if(%skillId != "" && %skillId != -1)
			{
				if(%display != "")
					%display = %display @ ", ";
				%display = %display @ %priority @ ". " @ AutoSkill_GetSkillName(%skillId);
			}
			break;
		}
		else
		{
			%skillId = String::getSubStr(%skillIds, 0, %commaPos);
			%skillIds = String::getSubStr(%skillIds, %commaPos + 1, 999);
			
			if(%skillId != "" && %skillId != -1)
			{
				if(%display != "")
					%display = %display @ ", ";
				%display = %display @ %priority @ ". " @ AutoSkill_GetSkillName(%skillId);
				%priority++;
			}
		}
	}
	
	AutoSkill_SendSafeMessage(%clientId, "Auto-skill priority: ", %display);
	Client::sendMessage(%clientId, $MsgBeige, "SP credits: " @ fetchData(%clientId, "SPcredits"));
	
	if(fetchData(%clientId, "AutoSkill_Mute") == "true")
		Client::sendMessage(%clientId, $MsgBeige, "Level-up messages: Hidden (Use #autoskill showmsg to show)");
	else
		Client::sendMessage(%clientId, $MsgBeige, "Level-up messages: Visible (Use #autoskill hide to hide)");
}

// Process auto-skill spending - called when player gains SP (on level-up)
function AutoSkill_Process(%clientId)
{
	dbecho($dbechoMode, "AutoSkill_Process(" @ %clientId @ ")");
	
	%skillIdsOriginal = fetchData(%clientId, "AutoSkill_Priority");
	echo("[DEBUG] AutoSkill_Process: clientId=" @ %clientId @ " spCredits=" @ fetchData(%clientId, "SPcredits") @ " skillIds=" @ %skillIdsOriginal);
	
	// Robust empty check - handle "", -1, 0, " ", whitespace
	if(%skillIdsOriginal == "" || %skillIdsOriginal == -1 || %skillIdsOriginal == "0" || %skillIdsOriginal == " ")
		return 0; // No auto-skills configured
	
	// Trim any whitespace
	%skillIdsOriginal = String::trim(%skillIdsOriginal);
	if(%skillIdsOriginal == "")
		return 0;
	
	%spCredits = fetchData(%clientId, "SPcredits") + 0;
	if(%spCredits <= 0)
		return 0; // No SP to spend
	
	%totalSpent = 0;
	%skillsUpgraded = "";
	
	// Calculate skill cap based on level
	%lvl = fetchData(%clientId, "LVL") + 0;
	%remortStep = fetchData(%clientId, "RemortStep") + 0;
	if(%remortStep == "")
		%remortStep = 0;
	%ub = (($skillRangePerLevel + 0) * %lvl) + 20 + (%remortStep * 2);
	
	// Parse skill IDs and spend SP on each skill in priority order
	%skillIds = %skillIdsOriginal;
	
	// Cache the number of skills outside the loop to optimize performance
	%numSkills = GetNumSkills() + 0;
	
	for(%priority = 0; %priority < 50 && %spCredits > 0; %priority++)
	{
		%commaPos = String::findSubStr(%skillIds, ",");
		
		if(%commaPos == -1)
		{
			%skillId = %skillIds;
			%skillIds = "";
		}
		else
		{
			%skillId = String::getSubStr(%skillIds, 0, %commaPos);
			%skillIds = String::getSubStr(%skillIds, %commaPos + 1, 999);
		}
		
		// Robust skill ID validation
		%skillId = String::trim(%skillId);
		if(%skillId == "" || %skillId == -1 || %skillId == "0")
		{
			if(%skillIds == "")
				break;
			continue;
		}
		
		// Validate skill ID is a valid number in range
		if((%skillId + 0) < 1 || (%skillId + 0) > %numSkills)
		{
			if(%skillIds == "")
				break;
			continue;
		}
		
		// Skip no longer used skills (stealing: 7, mining: 17)
		if((%skillId + 0) == 7 || (%skillId + 0) == 17)
		{
			if(%skillIds == "")
				break;
			continue;
		}
		
		// Calculate how many SP needed to max this skill
		%currentSkill = $PlayerSkill[%clientId, %skillId] + 0;
		
		// Determine absolute cap for the skill
		%absoluteLimit = 999999;
		if(%skillId == 6)      %absoluteLimit = 1000; // bashing
		else if(%skillId == 14) %absoluteLimit = 1000; // vehicle combat
		else if(%skillId == 16) %absoluteLimit = 1000; // criticals
		else if(%skillId == 18) %absoluteLimit = 100;  // speech
		else if(%skillId == 21) %absoluteLimit = 1000; // haggling
		
		// Effective cap is the minimum of level upper bound and absolute limit
		%effectiveCap = %ub + 0;
		if(%absoluteLimit < %effectiveCap)
			%effectiveCap = %absoluteLimit;
		
		// Skip if already at cap
		if(%currentSkill >= %effectiveCap)
		{
			if(%skillIds == "")
				break;
			continue;
		}
		
		// Get multiplier (custom logic for skill 16)
		%multiplier = 1.0;
		if(%skillId == 16)
		{
			%class = fetchData(%clientId, "CLASS");
			%multiplier = 0.5; // Default Criticals multiplier
			if(%class != "" && %class != -1)
			{
				if($SkillMultiplier[%class, $SkillCriticals] != "")
					%multiplier = $SkillMultiplier[%class, $SkillCriticals] + 0;
			}
		}
		else
		{
			%multiplier = GetSkillMultiplier(%clientId, %skillId) + 0;
		}
		
		if(%multiplier <= 0)
		{
			if(%skillIds == "")
				break;
			continue;
		}
		
		// Single-step mathematical allocation
		%pointsNeeded = %effectiveCap - %currentSkill;
		%rawSpNeeded = %pointsNeeded / %multiplier;
		%spNeeded = floor(%rawSpNeeded);
		if(%spNeeded < %rawSpNeeded)
			%spNeeded = %spNeeded + 1;
		%spNeeded = %spNeeded + 0;
		
		%spToSpend = %spCredits + 0;
		if(%spNeeded < %spToSpend)
			%spToSpend = %spNeeded + 0;
		
		if(%spToSpend > 0)
		{
			%skillIncrease = %spToSpend * %multiplier;
			%newSkill = %currentSkill + %skillIncrease;
			if(%newSkill > %effectiveCap)
				%newSkill = %effectiveCap;
			
			// Round appropriately based on skill type
			if(%skillId == 16)
			{
				%newSkill = round(%newSkill * 10) / 10;
			}
			else
			{
				%newSkill = FixDecimals(%newSkill);
			}
			
			// Set the new skill value directly
			$PlayerSkill[%clientId, %skillId] = %newSkill;
			
			// Decrement SP
			storeData(%clientId, "SPcredits", %spToSpend, "dec");
			%spCredits = fetchData(%clientId, "SPcredits") + 0;
			%totalSpent += %spToSpend;
			
			if(%skillsUpgraded != "")
				%skillsUpgraded = %skillsUpgraded @ ", ";
			%skillsUpgraded = %skillsUpgraded @ AutoSkill_GetSkillName(%skillId) @ " (" @ FormatSkillDisplay(%clientId, %skillId) @ ")";
		}
		
		// Check if we're out of SP or processed all skills
		if(%spCredits <= 0 || %skillIds == "")
			break;
	}
	
	// Report results
	if(%totalSpent > 0)
	{
		if(fetchData(%clientId, "AutoSkill_Mute") != "true")
		{
			AutoSkill_SendSafeMessage(%clientId, "[Auto-Skill] Spent " @ %totalSpent @ " SP: ", %skillsUpgraded, "~stats");
		}
		
		// Schedule a single throttled RefreshAll (same pattern as UseSkill)
		if($AutoSkillRefreshScheduled[%clientId] != "true")
		{
			$AutoSkillRefreshScheduled[%clientId] = "true";
			schedule("$AutoSkillRefreshScheduled[" @ %clientId @ "] = \"\"; RefreshAll(" @ %clientId @ ", \"true\");", 2);
		}
	}
	
	return %totalSpent;
}

// String::trim helper if not already defined
function String::trim(%str)
{
	// Remove leading spaces
	while(String::getSubStr(%str, 0, 1) == " " && String::len(%str) > 0)
		%str = String::getSubStr(%str, 1, 999);
	
	// Remove trailing spaces
	while(String::getSubStr(%str, String::len(%str) - 1, 1) == " " && String::len(%str) > 0)
		%str = String::getSubStr(%str, 0, String::len(%str) - 1);
	
	return %str;
}

echo("AutoSkill.cs loaded");
