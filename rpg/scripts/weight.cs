function GetWeight(%clientId)
{
	dbecho($dbechoMode, "GetWeight(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before proceeding
	%playerObj = Client::getOwnedObject(%clientId);
	if(%playerObj == -1 || %playerObj == "")
	{
		// Player object doesn't exist (player/bot was deleted) - return 0
		return 0;
	}

	if(IsDead(%clientId) || !fetchData(%clientId, "HasLoadedAndSpawned") || %clientId.IsInvalid)
		return 0;

	//== HELPS REDUCE LAG WHEN THERE ARE SIMULTANEOUS CALLS ======
	%time = getIntegerTime(true);
	if(%time - %clientId.lastGetWeight <= 1 && fetchData(%clientId, "tmpWeight") != "")
	{
		// CRITICAL FIX: Must also restore the global ArmorMod side-effect from cache
		// Otherwise RefreshWeight uses stale/empty ArmorMod and skin doesn't update
		$GetWeight::ArmorMod = fetchData(%clientId, "tmpArmorMod");
		return fetchData(%clientId, "tmpWeight");
	}
	%clientId.lastGetWeight = %time;
	//============================================================

	$GetWeight::ArmorMod = "";
	%total = 0;

	//add up items
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%checkItem = getItemData(%i);
		%itemcount = SafeGetItemCount(%clientId, %checkItem, "GetWeight");

		if(%itemcount)
		{
			%weight = GetAccessoryVar(%checkItem, $Weight);
			if(%weight != "" && %weight != False)
				%total += %weight * %itemcount;

			//Replaces the laggy AddPoints(%clientId, 8) in RefreshWeight (the real lag comes from GetAccessoryList however)
			%specialvar = GetAccessoryVar(%checkItem, $SpecialVar);
			if(GetWord(%specialvar, 0) == 8 && %checkItem.className == Equipped)
				$GetWeight::ArmorMod = GetWord(%specialvar, 1);
		}
	}

	// VOID BUGFIX 2026-08-18: CheetaursPaws/BootsOfGliding/WindWalkers/WindPaws
	// became BELT accessories in the Void pass (BeltWeapons.cs
	// VoidAccessory::RegisterAll), so they no longer exist as an "X0"
	// Equipped-class datablock in the engine inventory - the loop above never
	// sees them and $GetWeight::ArmorMod stayed empty -> RefreshWeight never
	// swapped to the speed/glide/jetpack armor. Belt::ApplyAccessoryStats only
	// handles stat indices 3/4/5/6/7/10/11, not 8, so read the type-8 mod
	// straight off the equipped-belt-accessory list here. AdminBoots stays
	// engine-side and is still caught by the loop above.
	%equippedAcc = fetchData(%clientId, "EquippedBeltAccessories");
	if(%equippedAcc != "" && %equippedAcc != "0")
	{
		for(%i = 0; (%acc = GetWord(%equippedAcc, %i)) != -1; %i++)
		{
			%specialvar = $AccessoryVar[%acc, $SpecialVar];
			if(GetWord(%specialvar, 0) == 8)
				$GetWeight::ArmorMod = GetWord(%specialvar, 1);
		}
	}

	//add up backpack weight
	%total += Belt::GetWeight(%clientid);

	//add up coins
	%total += fetchData(%clientId, "COINS") * $coinweight;

	storeData(%clientId, "tmpWeight", %total);
	storeData(%clientId, "tmpArmorMod", $GetWeight::ArmorMod); // Cache the side-effect too
	return %total;
}

function RefreshWeight(%clientId)
{
	dbecho($dbechoMode2, "RefreshWeight(" @ %clientId @ ")");

	// CRITICAL: Validate player object exists before proceeding
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
	{
		// Player object doesn't exist (player/bot was deleted)
		return;
	}

	if(!fetchData(%clientId, "SlowdownHitFlag"))
	{
		%weight = fetchData(%clientId, "Weight");
		
		%changeweightstep = 5;

		//determine the new armor to use
		%race = fetchData(%clientId, "RACE");
		%newarmor = $ArmorForSpeed[%race, 0];
		
		%spill = %weight - fetchData(%clientId, "MaxWeight");

		%num = floor(%spill / %changeweightstep);

		if(%num > 0)
		{
			//overweight, select appropriate armor
			for(%i = -1; %i >= -%num; %i--)
			{
				if($ArmorForSpeed[%race, %i] != "")
					%newarmor = $ArmorForSpeed[%race, %i];
				else
					break;
			}
		}
		else
		{
			//when not overweight, the special armor-modifying items come in
			%x = $GetWeight::ArmorMod;
			if(%x > 0)
				%newarmor = $ArmorForSpeed[%race, %x];
		}
	}
	else
	{
		%race = fetchData(%clientId, "RACE");
		%newarmor = $ArmorForSpeed[%race, -5];
	}

	%a = Player::getArmor(%clientId);
	%ae = GameBase::getEnergy(%player);

	if(%a != %newarmor && %newarmor != "" && Player::getItemCount(%clientId, "AdminBoots0") <= 0)
	{
		//set the new armor
		Player::setArmor(%clientId, %newarmor);
		GameBase::setEnergy(%player, %ae);
	}

	//save the %num in a global variable for use on stats (in order to give penalties to other stats for being overweight)
	storeData(%clientId, "OverweightStep", %num);
}
