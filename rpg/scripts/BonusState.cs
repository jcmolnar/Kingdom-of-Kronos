//======================================================================
// Bonus States are special bonuses for a certain player that last a
// certain amount of ticks.  A tick is decreased every 2 seconds by
// the zone check.
//======================================================================

$maxBonusStates = 10;

function DecreaseBonusStateTicks(%clientId, %b)
{
	// Initialize %b if not provided to prevent debug warnings
	if(%b == "" || %b == -1)
		%b = "";
	
	if(%b != "")
	{
		// Use temp variable to safely check if BonusStateCnt exists before accessing
		%tempCnt = $BonusStateCnt[%clientId, %b];
		
		//Decrease specified tick for the player (only if it exists and is > 0)
		if(%tempCnt != "" && %tempCnt > 0)
		{
			%tempCnt--;
			$BonusStateCnt[%clientId, %b] = %tempCnt;

			if(%tempCnt <= 0)
			{
				$BonusStateCnt[%clientId, %b] = "";
				$BonusState[%clientId, %b] = "";
				playSound(BonusStateExpire, GameBase::getPosition(%clientId));
			}
		}
	}
	else
	{
		%totalbcnt = 0;
		%truebcnt = 0;

		//Decrease all ticks for that player
		for(%i = 1; %i <= $maxBonusStates; %i++)
		{
			// Use temp variable to safely check if BonusStateCnt exists before accessing
			%tempCnt = $BonusStateCnt[%clientId, %i];
			%tempState = $BonusState[%clientId, %i];
			
			// Check if BonusStateCnt exists and is > 0 before accessing
			if(%tempCnt != "" && %tempCnt > 0)
			{
				%tempCnt--;
				$BonusStateCnt[%clientId, %i] = %tempCnt;

				if(%tempCnt <= 0)
				{
					$BonusStateCnt[%clientId, %i] = "";
					$BonusState[%clientId, %i] = "";
					playSound(BonusStateExpire, GameBase::getPosition(%clientId));
					refreshAll(%clientId);
				}
				else
				{
					%totalbcnt++;
					if(%tempState != "Jail" && %tempState != "Theft")
						%truebcnt++;
				}
			}
		}

		if(%truebcnt > 0)
			storeData(%clientId, "isBonused", True);
		else
			storeData(%clientId, "isBonused", "");
			
	}
}

function AddBonusStatePoints(%clientId, %filter)
{
	%add = 0;
	for(%i = 1; %i <= $maxBonusStates; %i++)
	{
		// Check if BonusStateCnt exists and is > 0 before accessing
		if($BonusStateCnt[%clientId, %i] != "" && $BonusStateCnt[%clientId, %i] > 0)
		{
			for(%z = 0; (%p1 = GetWord($BonusState[%clientId, %i], %z)) != -1; %z+=2)
			{
				%p2 = GetWord($BonusState[%clientId, %i], %z+1);
				if(String::ICompare(%p1, %filter) == 0)
				{
					//same filter
					%add += %p2;
				}
			}
		}
	}

	return %add;
}

function UpdateBonusState(%clientId, %type, %ticks)
{
	//look thru the current bonus states and attempt to update
	%flag = False;
	for(%i = 1; %i <= $maxBonusStates; %i++)
	{
		// Check if BonusStateCnt exists and is > 0 before accessing
		if($BonusStateCnt[%clientId, %i] != "" && $BonusStateCnt[%clientId, %i] > 0)
		{
			if(String::ICompare($BonusState[%clientId, %i], %type) == 0)
			{
				$BonusStateCnt[%clientId, %i] = %ticks;
				%flag = True;
			}
		}
	}

	if(!%flag)
	{
		//couldn't find a current entry to update, so make a new entry
		for(%i = 1; %i <= $maxBonusStates; %i++)
		{
			// Check if BonusStateCnt doesn't exist or is <= 0 (available slot)
			if($BonusStateCnt[%clientId, %i] == "" || $BonusStateCnt[%clientId, %i] <= 0)
			{
				$BonusState[%clientId, %i] = %type;
				$BonusStateCnt[%clientId, %i] = %ticks;

				return True;
			}
		}
	}

	return %flag;
}