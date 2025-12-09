function setHP(%clientId, %val)
{
	dbecho($dbechoMode, "setHP(" @ %clientId @ ", " @ %val @ ")");

	%armor = Player::getArmor(%clientId);
	%lckProtected = false; // Flag to track if LCK protection triggered
	
	// Get current damage level BEFORE any calculations (in case LCK protection triggers)
	%currentDamageLevel = GameBase::getDamageLevel(Client::getOwnedObject(%clientId));

	if(%val < 0)
		%val = 0;
	if(%val == "")
		%val = fetchData(%clientId, "MaxHP");

	// CRITICAL: For seal battle bots, ensure we use the stored scaled MaxHP
	// fetchData("MaxHP") should already check $SealBattleScaledStats first, but we verify here
	%maxHP = fetchData(%clientId, "MaxHP");
	
	%a = %val * %armor.maxDamage;
	%b = %a / %maxHP;
	%c = %armor.maxDamage - %b;

	if(%c < 0)
		%c = 0;
	else if(%c > %armor.maxDamage)
		%c = %armor.maxDamage;

	if(%c == %armor.maxDamage && !IsStillArenaFighting(%clientId))
	{
		%currentLCK = fetchData(%clientId, "LCK");
		%lckConsequence = fetchData(%clientId, "LCKconsequence");
		
		
		storeData(%clientId, "LCK", 1, "dec");
		%newLCK = fetchData(%clientId, "LCK");

		if(%newLCK >= 0)
		{
			Client::sendMessage(%clientId, $MsgRed, "You have permanently lost an LCK point!");

			if(%lckConsequence == "miss")
			{
				// LCK protection triggered - keep damage level at current level (don't set to max)
				// Use the damage level we captured at the start (before this fatal hit)
				%c = %currentDamageLevel;
				%val = -1;
				%lckProtected = true; // Flag to prevent setting damage to max
			}
		}
	}

	// Only set damage level to max if LCK protection didn't trigger
	if(!%lckProtected)
		GameBase::setDamageLevel(Client::getOwnedObject(%clientId), %c);

	return %val;
}
function refreshHP(%clientId, %value)
{
	dbecho($dbechoMode, "refreshHP(" @ %clientId @ ", " @ %value @ ")");

	return setHP(%clientId, fetchData(%clientId, "HP") - round(%value * $TribesDamageToNumericDamage));
}
function refreshHPREGEN(%clientId)
{
	dbecho($dbechoMode, "refreshHPREGEN(" @ %clientId @ ")");
	
	// CRITICAL: Validate player object exists before proceeding
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		// Player object doesn't exist (player/bot was deleted)
		return;
	}
	
	if($PlayerSkill[%clientId, $SkillHealing] < 1000)
		%a = $PlayerSkill[%clientId, $SkillHealing] / 250000;
	else
		%a = (1000 / 250000) + (($PlayerSkill[%clientId, $SkillHealing] - 1000) / 750000);
	if(%clientId.sleepMode == 1)
		%b = %a + 0.0200;
	else if(%clientId.sleepMode == 2)
		%b = %a;
	else
		%b = %a;
	if(Zone::getType(fetchData(%clientId, "zone")) == "PROTECTED")
		%b = %a + 0.0200;

	%c = AddPoints(%clientId, 10) / 2000;

	%r = %b + %c;

	// CRITICAL: Re-validate player object before setting auto repair rate
	%playerCheck = Client::getOwnedObject(%clientId);
	if(%playerCheck == -1 || %playerCheck == "")
	{
		// Player object was deleted during AddPoints
		return;
	}

	GameBase::setAutoRepairRate(%playerCheck, %r);
}
