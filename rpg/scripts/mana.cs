function setMANA(%clientId, %val)
{
	dbecho($dbechoMode, "setMANA(" @ %clientId @ ", " @ %val @ ")");

	%armor = Player::getArmor(%clientId);

	if(%val == "")
		%val = fetchData(%clientId, "MaxMANA");

	%a = %val * %armor.maxEnergy;
	%b = %a / fetchData(%clientId, "MaxMANA");

	if(%b < 0)
		%b = 0;
	else if(%b > %armor.maxEnergy)
		%b = %armor.maxEnergy;

	GameBase::setEnergy(Client::getOwnedObject(%clientId), %b);
}
function refreshMANA(%clientId, %value)
{
	dbecho($dbechoMode, "refreshMANA(" @ %clientId @ ", " @ %value @ ")");

	// Prevent mana increases while in bane stance (MageBane or BladeBane)
	// Negative %value means restoring/increasing mana
	if(%value < 0)
	{
		%stance = fetchData(%clientId, "Stance");
		if(%stance == "MageBane" || %stance == "BladeBane")
			return; // Don't restore mana while in bane stance
	}

	setMANA(%clientId, (fetchData(%clientId, "MANA") - %value));
}
function refreshMANAREGEN(%clientId)
{
	dbecho($dbechoMode, "refreshMANAREGEN(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before proceeding
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		// Player object doesn't exist (player/bot was deleted)
		return;
	}

	%a = ($PlayerSkill[%clientId, $SkillEnergy] / 3250);
	if(%clientId.sleepMode == 1)
		%b = 1.0 + %a;
	else if(%clientId.sleepMode == 2)
		%b = 2.25 + %a;
	else
		%b = %a;
	if(Zone::getType(fetchData(%clientId, "zone")) == "PROTECTED")
		%b = %a + 2;

	%c = AddPoints(%clientId, 11) / 800;

	%r = %b + %c;
	
	%stance = fetchData(%clientId, "Stance");
	if(%stance == "BladeBane" || %stance == "MageBane")
	{
		%r = -1;
		// Schedule a check for mana depletion (only if not already scheduled)
		if($BaneStanceCheck[%clientId] == "")
		{
			$BaneStanceCheck[%clientId] = true;
			schedule("CheckBaneStanceMana(" @ %clientId @ ");", 1);
		}
	}
	else
	{
		// Clear the check flag if not in bane stance
		$BaneStanceCheck[%clientId] = "";
	}

	GameBase::setRechargeRate(Client::getOwnedObject(%clientId), %r);
}
function CheckBaneStanceMana(%clientId)
{
	dbecho($dbechoMode, "CheckBaneStanceMana(" @ %clientId @ ")");

	// Check if client is still valid
	if(!isObject(Client::getOwnedObject(%clientId)))
	{
		$BaneStanceCheck[%clientId] = "";
		return;
	}

	%stance = fetchData(%clientId, "Stance");
	%mana = fetchData(%clientId, "MANA");
	
	// If still in bane stance and has mana, schedule another check
	if((%stance == "MageBane" || %stance == "BladeBane") && %mana != "" && %mana > 0)
	{
		schedule("CheckBaneStanceMana(" @ %clientId @ ");", 1);
		return;
	}
	
	// If in bane stance but out of mana, switch back to previous stance
	if((%stance == "MageBane" || %stance == "BladeBane") && (%mana == "" || %mana <= 0))
	{
		%previousStance = fetchData(%clientId, "PreviousStance");
		// If no previous stance stored, default to Normal
		if(%previousStance == "" || %previousStance == -1)
			%previousStance = "Normal";
		storeData(%clientId, "Stance", %previousStance);
		storeData(%clientId, "PreviousStance", ""); // Clear previous stance
		Client::sendMessage(%clientId, $MsgBeige, "You have run out of mana - Returning to " @ %previousStance @ ".");
		// Refresh mana regeneration to reset the drain rate now that we're out of bane stance
		refreshMANAREGEN(%clientId);
		$BaneStanceCheck[%clientId] = "";
		return;
	}
	
	// Not in bane stance anymore, clear the check flag
	$BaneStanceCheck[%clientId] = "";
}
