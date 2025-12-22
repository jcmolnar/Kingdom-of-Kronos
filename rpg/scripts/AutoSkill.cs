//====================================================================================================
// AutoSkill.cs - Automatic Skill Point Spending System
//====================================================================================================
// Allows players to set priority skills that automatically stay maxed as they level up.
// 
// Usage:
//   #autoskill set endurance, weight capacity, slashing
//   #autoskill show
//   #autoskill clear
//
// Data is stored in funkvar slot 55 for save/load persistence.
//
//----------------------------------------------------------------------------------------------------
// HOOKS / INTEGRATIONS:
//----------------------------------------------------------------------------------------------------
// Server.cs        - exec(AutoSkill); added to load this script
// rpgstats.cs      - AutoSkill_Process() called in Game::refreshClientScore() on level-up
// rpgfunk.cs       - SaveCharacter() saves AutoSkill_Priority to funkvar slot 55
// rpgfunk.cs       - LoadCharacter() loads AutoSkill_Priority from funkvar slot 55
// comchat.cs       - #autoskill command handler added
// skills.cs        - Uses GetNumSkills(), AddSkillPoint(), $SkillDesc[], $PlayerSkill[]
//====================================================================================================

// Get skill ID from skill name (case-insensitive, supports partial matches)
function AutoSkill_GetSkillId(%skillName)
{
	dbecho($dbechoMode, "AutoSkill_GetSkillId(" @ %skillName @ ")");
	
	// Trim whitespace from skill name
	%skillName = String::trim(%skillName);
	
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
	if(%skillId >= 1 && %skillId <= GetNumSkills())
		return $SkillDesc[%skillId];
	return "Unknown";
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
					if(%skillIds != "")
						%skillIds = %skillIds @ ",";
					%skillIds = %skillIds @ %skillId;
					
					if(%validSkills != "")
						%validSkills = %validSkills @ ", ";
					%validSkills = %validSkills @ AutoSkill_GetSkillName(%skillId);
					%count++;
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
					if(%skillIds != "")
						%skillIds = %skillIds @ ",";
					%skillIds = %skillIds @ %skillId;
					
					if(%validSkills != "")
						%validSkills = %validSkills @ ", ";
					%validSkills = %validSkills @ AutoSkill_GetSkillName(%skillId);
					%count++;
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
		Client::sendMessage(%clientId, $MsgBeige, "Auto-skill priority set: " @ %validSkills);
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
	
	Client::sendMessage(%clientId, $MsgBeige, "Auto-skill priority: " @ %display);
	Client::sendMessage(%clientId, $MsgBeige, "SP credits: " @ fetchData(%clientId, "SPcredits"));
}

// Process auto-skill spending - called when player gains SP (on level-up)
function AutoSkill_Process(%clientId)
{
	dbecho($dbechoMode, "AutoSkill_Process(" @ %clientId @ ")");
	
	%skillIdsOriginal = fetchData(%clientId, "AutoSkill_Priority");
	
	// Robust empty check - handle "", -1, 0, " ", whitespace
	if(%skillIdsOriginal == "" || %skillIdsOriginal == -1 || %skillIdsOriginal == "0" || %skillIdsOriginal == " ")
		return 0; // No auto-skills configured
	
	// Trim any whitespace
	%skillIdsOriginal = String::trim(%skillIdsOriginal);
	if(%skillIdsOriginal == "")
		return 0;
	
	%spCredits = fetchData(%clientId, "SPcredits");
	if(%spCredits <= 0)
		return 0; // No SP to spend
	
	%totalSpent = 0;
	%skillsUpgraded = "";
	
	// Calculate skill cap based on level
	%lvl = fetchData(%clientId, "LVL");
	%remortStep = fetchData(%clientId, "RemortStep");
	if(%remortStep == "")
		%remortStep = 0;
	%skillCap = ($skillRangePerLevel * %lvl) + 20 + (%remortStep * 2);
	
	// Parse skill IDs and spend SP on each skill in priority order
	%skillIds = %skillIdsOriginal;
	
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
		
		// Robust skill ID validation - must be non-empty, not -1, and a valid positive number
		%skillId = String::trim(%skillId);
		if(%skillId == "" || %skillId == -1 || %skillId == "0")
		{
			if(%skillIds == "")
				break;
			continue;
		}
		
		// Validate skill ID is a valid number in range
		%numSkills = GetNumSkills();
		if(%skillId < 1 || %skillId > %numSkills)
		{
			if(%skillIds == "")
				break;
			continue;
		}
		
		// Calculate how many SP needed to max this skill
		%currentSkill = $PlayerSkill[%clientId, %skillId];
		if(%currentSkill == "")
			%currentSkill = 0;
		
		// Skip if already at cap
		if(%currentSkill >= %skillCap)
			continue;
		
		// Calculate SP needed (accounting for class multiplier)
		%spNeeded = 0;
		%spSpentOnThisSkill = 0;
		
		// Spend SP until skill is maxed or out of SP
		while(%currentSkill < %skillCap && %spCredits > 0)
		{
			// Try to add a skill point
			if(AddSkillPoint(%clientId, %skillId))
			{
				storeData(%clientId, "SPcredits", 1, "dec");
				%spCredits = fetchData(%clientId, "SPcredits");
				%totalSpent++;
				%spSpentOnThisSkill++;
				%currentSkill = $PlayerSkill[%clientId, %skillId];
			}
			else
			{
				// Skill is capped or can't be upgraded further
				break;
			}
		}
		
		if(%spSpentOnThisSkill > 0)
		{
			if(%skillsUpgraded != "")
				%skillsUpgraded = %skillsUpgraded @ ", ";
			%skillsUpgraded = %skillsUpgraded @ AutoSkill_GetSkillName(%skillId) @ " (" @ FormatSkillDisplay(%clientId, %skillId) @ ")";
		}
		
		// Check if we're out of SP
		if(%spCredits <= 0)
			break;
		
		// If we've processed all skills
		if(%skillIds == "")
			break;
	}
	
	// Report results
	if(%totalSpent > 0)
	{
		Client::sendMessage(%clientId, $MsgBeige, "[Auto-Skill] Spent " @ %totalSpent @ " SP: " @ %skillsUpgraded);
		
		// Schedule a single throttled RefreshAll (same pattern as UseSkill)
		// This prevents spam while ensuring the UI updates after all skills are processed
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
