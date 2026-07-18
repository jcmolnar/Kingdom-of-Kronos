//============================================================================
// spells_engine.cs — split from spells.cs (original lines 1314-4155)
// Extracted 2026-07-17 at commit 4aa0a3d. Mechanical text move, no behavior change.
// exec'd by spells.cs (the shell) — do NOT add to Server.cs.
// BeginCastSpell / DoCastSpell (kept whole) / bomb+batch engines / damage / DoBoxFunction
// ==== BEGIN ORIGINAL PAYLOAD ====
function BeginCastSpell(%clientId, %keyword)
{
	dbecho($dbechoMode, "BeginCastSpell(" @ %clientId @ ", " @ %keyword @ ")");

	%w1 = GetWord(%keyword, 0);
	%w1Len = String::len(%w1);
	%w2 = String::getSubStr(%keyword, %w1Len + 1, 99999);

	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "" || !isObject(%player))
	{
		Client::sendMessage(%clientId, $MsgWhite, "You cannot cast right now.");
		return False;
	}
	%playerPos = GameBase::getPosition(%player);

		for(%i = 1; $Spell::keyword[%i] != ""; %i++)
		{
			if(String::ICompare($Spell::keyword[%i], %w1) == 0)
			{
				%spellKeyword = $Spell::keyword[%i];
				if(SkillCanUse(%clientId, %spellKeyword))
				{
				%manaCost = $Spell::manaCost[%i];
				
				// CRITICAL: Skip mana check for all enemy bots (including seal battle bots, Colloseum bots, etc.)
				// Enemy bots may have low mana but should still be able to cast spells
				// Check using Player::isAiControlled() or isRPGAI() to catch all bot types
				// Exclude town bots (they have BotInfoAiName starting with "TownBot_" but no SpawnBotInfo)
				%spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
				%botInfoAiName = fetchData(%clientId, "BotInfoAiName");
				%isEnemyBot = false;
				if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
				{
					// Check if it's an enemy bot (has SpawnBotInfo) or a seal/Colloseum bot (AI controlled but no BotInfoAiName starting with "TownBot_")
					if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
					{
						%isEnemyBot = true; // Has SpawnBotInfo - enemy bot
					}
					else if(%botInfoAiName == "" || %botInfoAiName == "0" || %botInfoAiName == -1 || String::findSubStr(%botInfoAiName, "TownBot_") != 0)
					{
						// No BotInfoAiName or doesn't start with "TownBot_" - likely a seal/Colloseum bot
						%isEnemyBot = true;
					}
					// If BotInfoAiName starts with "TownBot_", it's a town bot - don't skip mana check
				}
				
				// For enemy bots, skip mana check. For players and town bots, check if they have enough mana.
				if(%isEnemyBot || fetchData(%clientId, "MANA") >= %manaCost)
					{
					Client::sendMessage(%clientId, $MsgBeige, "Casting " @ $Spell::name[%i] @ ".~spellc");

					%losRange = $Spell::LOSrange[%i];
					if(GameBase::getLOSinfo(%player, %losRange))
					{
						%lospos = $los::position;
						%losobj = $los::object;
					}
					else
					{
						%lospos = "";
						%losobj = 0;
					}
	
					storeData(%clientId, "SpellCastStep", 1);
	
					%tempManaCost = floor(%manaCost / 2);
					refreshMANA(%clientId, %tempManaCost);
					playSound($Spell::startSound[%i], %playerPos);

					%skt = $SkillType[%spellKeyword];
					%sk1 = $PlayerSkill[%clientId, %skt];
					%gsa = GetSkillAmount(%spellKeyword, %skt);
					%sk = Cap(%sk1 - %gsa, 0, "inf");
					%rt = $Spell::recoveryTime[%i];
					%rtHalf = %rt / 2;
					%recovTime = $Spell::delay[%i] + Cap(%rtHalf + ((1000 - %sk) / 1000 * %rtHalf), %rtHalf, %rt);

					// KronosHUD cast bar: spell name, time until the spell
					// fires, and full recovery time
					if(!Player::isAiControlled(%clientId))
						if(%clientId.hasKronosHUD)
							remoteEval(%clientId, "KronosCast", $Spell::name[%i], $Spell::delay[%i], %recovTime);

					// CRITICAL: Capture caster name at cast time for identity validation
					// This prevents ghost damage if another bot takes this clientId before spell fires
					%casterName = Client::getName(%clientId);
					%safeCasterName = String::replace(%casterName, "\"", "");
					%safeW2 = String::replace(%w2, "\"", "");
					%castToken = SpellCast_NextToken(%clientId);
					// Record the player object controlling THIS cast, KEYED BY TOKEN,
					// so each cast validates against its own caster (a bot may recast
					// before earlier bombs land). Reclaimed ~30s out, after all of this
					// cast's detonations have fired.
					$SpellCasterObj[%clientId, %castToken] = Client::getOwnedObject(%clientId);
					schedule("SpellCast_ClearCaster(" @ %clientId @ ", " @ %castToken @ ");", 30);

					schedule("%retval=DoCastSpell(" @ %clientId @ ", " @ %i @ ", \"" @ %playerPos @ "\", \"" @ %lospos @ "\", \"" @ %losobj @ "\", \"" @ %safeW2 @ "\", \"" @ %safeCasterName @ "\", " @ %castToken @ "); if(%retval){refreshMANA(" @ %clientId @ ", " @ %tempManaCost @ ");}", $Spell::delay[%i]);
					schedule("storeData(" @ %clientId @ ", \"SpellCastStep\", \"\");sendDoneRecovMsg(" @ %clientId @ ");", %recovTime);
				
					// ASCENSION: Spell Echo - 15% chance to cast offensive spells twice (no extra mana cost)
					if(Ascension::HasTalent(%clientId, "SpellEcho") && $SkillType[%spellKeyword] == $SkillOffensiveCasting)
					{
						if(floor(getRandom() * 100) < 15)
						{
							// Schedule echo cast slightly after original - use SILENT version (no explosions)
							%echoDelay = $Spell::delay[%i] + 0.5;
							schedule("DoCastSpell_Silent(" @ %clientId @ ", " @ %i @ ", \"" @ %playerPos @ "\", \"" @ %lospos @ "\", \"" @ %losobj @ "\", \"" @ %safeW2 @ "\", \"" @ %safeCasterName @ "\", " @ %castToken @ ");", %echoDelay);
							Client::sendMessage(%clientId, 0, "Spell Echo!");
						}
					}
		
					return True;
				}
				else
					Client::sendMessage(%clientId, $MsgWhite, "Insufficient mana to cast this spell.");
			}
			else
				Client::sendMessage(%clientId, $MsgWhite, "You can't cast this spell because you lack the necessary skills.");

			return False;
		}
	}
	Client::sendMessage(%clientId, $MsgWhite, "This spell seems unfamiliar to you.");

	return False;
}

//============================================================================
// SILENT SPELL CAST - Used by Spell Echo to apply damage without visuals
// This reduces visual clutter and improves performance for echoed spells
//============================================================================
function DoCastSpell_Silent(%clientId, %index, %oldpos, %castPos, %castObj, %w2, %expectedCasterName, %castToken)
{
	dbecho($dbechoMode, "DoCastSpell_Silent(" @ %clientId @ ", " @ %index @ ")");
	
	if(!SpellCast_IsValid(%clientId, %expectedCasterName, %castToken, "echo"))
		return False;
	
	%casterObj = Client::getOwnedObject(%clientId);
	if(%casterObj == -1 || %casterObj == "" || !isObject(%casterObj))
		return False;
	
	// Get spell info
	%spellRadius = $Spell::radius[%index];
	%spellDamage = $Spell::damageValue[%index];
	%skilltype = $SkillType[$Spell::keyword[%index]];
	
	// Only apply damage for offensive spells with radius damage
	if(%skilltype != $SkillOffensiveCasting)
		return False;
	
	// For radius spells, apply damage silently (no explosions)
	if(%spellRadius != "" && %spellRadius > 0 && %castPos != "")
	{
		// SPECIAL HANDLING: Multi-explosion "Intensive" spells
		// Run the full batch sequence silently instead of just one hit
		// Indices: 48 (Apocalypse), 50 (Ion Blast), 51 (Shredder), 66 (Terminate), 67 (Tornado)
		if(%index == 48 || %index == 50 || %index == 66)
		{
			// These use ApocalypseBatchExplosions
			// Assume data arrays are already populated by the original cast
			%apocToken = $ApocalypseDataToken[%clientId];
			ApocalypseBatchExplosions(%clientId, %index, 0, true, %apocToken);
			return True;
		}
		else if(%index == 46)
		{
			// Tornado (Index 46) uses its own batch function
			TornadoBatchExplosions(%clientId, %index, 0, true);
			return True;
		}
		else if(%index == 51)
		{
			// Shredder uses its own batch function - but wait, it's not a batch function in spells.cs!
			// Index 51 (Shredder) uses custom schedule loop in DoCastSpell.
			// We need to replicate that loop here silently.
			
			// Cache position parsing
			%castX = GetWord(%castPos, 0);
			%castY = GetWord(%castPos, 1);
			%castZ = GetWord(%castPos, 2);
			%zPos = %castZ + 600; // Unused for silent, but matching structure
			
			%newPos = %castX @ " " @ %castY @ " " @ (%castZ + 1.4);
			
			// 1. Skip the 25 lasers (visual only?) - Wait, lasers might do damage on impact?
			// The original code separates lasers (visuals) from bombs (damage).
			// If we skip lasers, we skip that visual clutter.
			// The bombs do the actual radius damage.
			
			// 2. Schedule the 4 explosions (damage only)
			for(%i = 0; %i < 4; %i++)
			{
				// Call ApocalypseCreateExplosion with silent=true
				// Original used: CreateAndDetBomb(%clientId, "Bomb23", %newPos, true, %index);
				schedule("ApocalypseCreateExplosion(" @ %clientId @ ", \"Bomb23\", \"" @ %newPos @ "\", 1, " @ %index @ ", true);", %i / 5);
			}
			return True;
		}

		// Use cached damage if available (for other spells)
		// count == 0 is a valid cache (all targets filtered, e.g. town bots),
		// SpellRadiusDamage_Cached handles it without falling back.
		if($SpellTargetCache[%clientId, "count"] != "")
		{
			SpellRadiusDamage_Cached(%clientId, %castPos, %index);
		}
		else
		{
			// No cache - use standard radius damage (no visuals)
			SpellRadiusDamage(%clientId, %castPos, %index);
		}
		return True;
	}
	
	// For single-target spells (LOS spells), apply damage to target
	if(%castObj != "" && %castObj != 0 && isObject(%castObj))
	{
		if(getObjectType(%castObj) == "Player")
		{
			SpellDamage(%clientId, %castObj, %spellDamage, %index);
			return True;
		}
	}
	
	return False;
}

function DoCastSpell(%clientId, %index, %oldpos, %castPos, %castObj, %w2, %expectedCasterName, %castToken)
{
	dbecho($dbechoMode, "DoCastSpell(" @ %clientId @ ", " @ %index @ ", " @ %oldpos @ ", " @ %castPos @ ", " @ %castObj @ ", " @ %w2 @ ", " @ %expectedCasterName @ ")");

	if(!SpellCast_IsValid(%clientId, %expectedCasterName, %castToken, "spell"))
	{
		storeData(%clientId, "SpellCastStep", "");
		return False;
	}

	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "" || !isObject(%player))
	{
		storeData(%clientId, "SpellCastStep", "");
		return False;
	}

	if(Vector::getDistance(%oldpos, GameBase::getPosition(%player)) > $Spell::graceDistance[%index])
	{
		Client::sendMessage(%clientId, $MsgBeige, "Your casting was interrupted.");
		if(%clientId.hasKronosHUD)
			remoteEval(%clientId, "KronosCastStop");
		storeData(%clientId, "SpellCastStep", 2);

		return False;
	}


	//group-list check
	if($Spell::groupListCheck[%index])
	{
		%cl = Player::getClient(%castObj);
		if( !(IsInCommaList(fetchData(%clientId, "grouplist"), Client::getName(%cl)) && IsInCommaList(fetchData(%cl, "grouplist"), Client::getName(%clientId))) && %cl != %clientId && %cl != -1)
		{
			Client::sendMessage(%clientId, $MsgBeige, "You are not part of the target's group.");
			storeData(%clientId, "SpellCastStep", 2);

			return False;
		}
	}

	//==================================================================

	// Early target guard: neutral/defensive spells cannot be cast on bots (enemy or town)
	// review #17: EXCEPT Mimic (index 32), whose entire purpose is to target a
	// creature in LOS and copy its RACE - it is registered $SkillNeutralCasting, so
	// this guard was blocking it before its own index==32 logic (~line 2401) could
	// ever run, making the spell non-functional for its documented use.
	%skilltype = $SkillType[$Spell::keyword[%index]];
	if((%skilltype == $SkillNeutralCasting || %skilltype == $SkillDefensiveCasting) && %index != 32)
	{
		if(isObject(%castObj) && getObjectType(%castObj) == "Player")
		{
			%tgtId = GetClientIdFromPlayerObject(%castObj);
			if(%tgtId == -1 || %tgtId == "")
				%tgtId = %castObj;
			
			if(IsEnemyBot(%tgtId) || isTownBot(%tgtId))
			{
				Client::sendMessage(%clientId, $MsgBeige, "You cannot cast that on bots.");
				storeData(%clientId, "SpellCastStep", 2);
				return False;
			}
		}
	}

	//unfortunately hard-coded part -- although that is the original purpose of Tribes scripting
	if(%index == 1)
	{
		//firebomb spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb1", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}

	if(%index == 2)
	{
		//teleport zone spell

		%zoneId = GetNearestZone(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			// Check if player is carrying a flag - drop it if they are
			%player = Client::getOwnedObject(%clientId);
			if(%player != -1 && %player != "" && %player.carryFlag != "" && %player.carryFlag != -1)
			{
				%flagObj = %player.carryFlag;
				if(%flagObj != -1 && %flagObj != "")
				{
					%flagObj.carrier = -1;
				}
				%player.carryFlag = "";
				Player::setItemCount(%clientId, Flag, 0);
				Client::sendMessage(%clientId, $MsgRed, "You dropped the flag when you teleported.");
			}
			
 			Client::sendMessage(%clientId, $MsgBeige, "Teleporting near " @ Zone::getDesc(%zoneId));

			//teleport

			%mpos = Zone::getMarker(%zoneId);
			if(!fetchData(%clientId, "invisible"))
				GameBase::startFadeIn(%clientId);

			GameBase::setPosition(%clientId, %mpos);
			CheckAndBootFromArena(%clientId);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			Player::setDamageFlash(%clientId, 0.7);
			%extraDelay = 0.22;	//sometimes the endSound doesn't get played unless there is sufficient delay

			%castPos = SetOnGround(%clientId, 500);
			//%castPos = %newpos;

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Teleportation failed.");
			%returnFlag = False;
		}
	}

	if(%index == 3)
	{
		//Transport zone spell

		%zoneId = GetZoneByKeywords(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			// Check if player is carrying a flag - drop it if they are
			%player = Client::getOwnedObject(%clientId);
			if(%player != -1 && %player != "" && %player.carryFlag != "" && %player.carryFlag != -1)
			{
				%flagObj = %player.carryFlag;
				if(%flagObj != -1 && %flagObj != "")
				{
					%flagObj.carrier = -1;
				}
				%player.carryFlag = "";
				Player::setItemCount(%clientId, Flag, 0);
				Client::sendMessage(%clientId, $MsgRed, "You dropped the flag when you transported.");
			}
			
			Client::sendMessage(%clientId, $MsgBeige, "Transporting to " @ Zone::getDesc(%zoneId));

			//teleport

			%system = Object::getName(%zoneId);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);

			%castPos = TeleportToMarker(%clientId, "Zones\\" @ %system @ "\\DropPoints", False, True);
			CheckAndBootFromArena(%clientId);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			if(!fetchData(%clientId, "invisible"))
				GameBase::startFadeIn(%clientId);

			Player::setDamageFlash(%clientId, 0.7);
			%extraDelay = 0.22;	//sometimes the endSound doesn't get played unless there is sufficient delay

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Transportation failed.");
			%returnFlag = False;
		}
	}

	if(%index == 4)
	{
		//Advanced Transport zone spell

		%zoneId = GetZoneByKeywords(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			if(getObjectType(%castObj) == "Player")
				%id = Player::getClient(%castObj);
			else
				%id = %clientId;

			// Check if player being transported is carrying a flag - drop it if they are
			%transportedPlayer = Client::getOwnedObject(%id);
			if(%transportedPlayer != -1 && %transportedPlayer != "" && %transportedPlayer.carryFlag != "" && %transportedPlayer.carryFlag != -1)
			{
				%flagObj = %transportedPlayer.carryFlag;
				if(%flagObj != -1 && %flagObj != "")
				{
					%flagObj.carrier = -1;
				}
				%transportedPlayer.carryFlag = "";
				Player::setItemCount(%id, Flag, 0);
				if(%clientId != %id)
				{
					Client::sendMessage(%clientId, $MsgRed, "The flag was dropped when you transported them. Thought you were smart, eh?");
					Client::sendMessage(%id, $MsgRed, "You dropped the flag when you were transported. Thought you were smart, eh?");
				}
				else
				{
					Client::sendMessage(%id, $MsgRed, "You dropped the flag when you transported. Thought you were smart, eh?");
				}
			}
			
			Client::sendMessage(%clientId, $MsgBeige, "Transporting to " @ Zone::getDesc(%zoneId));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, "You are being transported to " @ Zone::getDesc(%zoneId));

			//teleport

			%system = Object::getName(%zoneId);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);

			%castPos = TeleportToMarker(%id, "Zones\\" @ %system @ "\\DropPoints", False, True);
			CheckAndBootFromArena(%id);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			if(!fetchData(%id, "invisible"))
				GameBase::startFadeIn(%id);

			Player::setDamageFlash(%id, 0.7);
			%extraDelay = 0.22;	//sometimes the endSound doesn't get played unless there is sufficient delay

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Transportation failed.");
			%returnFlag = False;
		}
	}
	if(%index == 5)
	{
		//cloud spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb2", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}
	if(%index == 6)
	{
		//melt spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb3", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}
	if(%index == 7)
	{
		//power cloud spell, casts to LOS with radius damage

		if(%castPos != "")
		{
			// Build once for the 3 pulse sequence so each pulse avoids a full container query.
			%pcToken = PowerCloud_NextToken(%clientId);
			$PowerCloudCacheToken[%clientId] = %pcToken;
			%spellRadius = $Spell::radius[%index];
			if(%spellRadius == "" || %spellRadius <= 0)
				%spellRadius = 10;
			%cacheRadius = %spellRadius + 12; // movement buffer during the pulse window
			SpellTargetCache_Build(%clientId, %castPos, %cacheRadius);
			%safeExpectedCasterName = String::replace(%expectedCasterName, "\"", "");

			// Keep these on scheduler root (not object-bound) to avoid edge cases when
			// the player object changes between cast and delayed pulses.
			schedule("SpellPowerCloudPulse(" @ %clientId @ ", \"" @ %castPos @ "\", " @ %index @ ", \"" @ %safeExpectedCasterName @ "\", " @ %pcToken @ ", " @ %castToken @ ");", 0.0);
			schedule("SpellPowerCloudPulse(" @ %clientId @ ", \"" @ %castPos @ "\", " @ %index @ ", \"" @ %safeExpectedCasterName @ "\", " @ %pcToken @ ", " @ %castToken @ ");", 0.5);
			schedule("SpellPowerCloudPulse(" @ %clientId @ ", \"" @ %castPos @ "\", " @ %index @ ", \"" @ %safeExpectedCasterName @ "\", " @ %pcToken @ ", " @ %castToken @ ");", 1.0);
			schedule("PowerCloud_ClearCache(" @ %clientId @ ", " @ %pcToken @ ");", 1.5);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");

			%returnFlag = False;
		}
	}
	if(%index == 8)
	{
		//heal self spell

		Client::sendMessage(%clientId, $MsgBeige, "Healing self");

		%r = $Spell::damageValue[%index] / $TribesDamageToNumericDamage;
		refreshHP(%clientId, %r);

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}


	if(%index == 9 || %index == 10 || %index == 11 || %index == 20 || %index == 34 || %index == 35 || %index == 36)
	{
		//heal self or other (LOS) 1st, 2nd, 3rd, 4th, 5th, 6th, godly

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		// Cache name lookups
		%targetName = Client::getName(%id);
		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ %targetName);
		if(%clientId != %id)
		{
			%casterName = Client::getName(%clientId);
			Client::sendMessage(%id, $MsgBeige, %casterName @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");
		}

		refreshHP(%id, $Spell::damageValue[%index] / $TribesDamageToNumericDamage);
		%castPos = GameBase::getPosition(%id);
		%returnFlag = True;
	}
	if(%index == 60)
	{
//CurePlus1

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 61)
	{
//CurePlus2

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.2)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 62)
	{
//CurePlus3

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.4)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 63)
	{
//CurePlus4

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.6)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 64)
	{
//CurePlus5

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");

		%r = round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.8)) / $TribesDamageToNumericDamage; //$Spell::damageValue[%index] + 

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
if(%index == 65)
	{
//CurePlus5

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		Client::sendMessage(%clientId, $MsgBeige, "Healing " @ Client::getName(%id));
		if(%clientId != %id)
			Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");

		%r = $Spell::damageValue[%index] + round(($PlayerSkill[%clientId, $SkillDefensiveCasting] * -1.9)) / $TribesDamageToNumericDamage;

		refreshHP(%id, %r);

		%castPos = GameBase::getPosition(%id);

		%returnFlag = True;
	}
	if(%index == 12)
	{
		//laser beam

		if(getObjectType(%castObj) == "Player")
			%id = Player::getClient(%castObj);

		%trans = GameBase::getMuzzleTransform(%clientId);
		%p = Projectile::spawnProjectile("heatlaser", %trans, %player, "0 0 0", 1.0);

		%mom1 = Vector::getFromRot( GameBase::getRotation(%clientId), -60, 1 );
		Player::applyImpulse(%clientId, %mom1);

		%r = $Spell::damageValue[%index];
	
		if(%id != "")
		{
			//%miss = CalcSpellMiss(%clientId, %id, %index);

			SpellDamage(%clientId, %id, %r, %index);
			%mom2 = Vector::getFromRot( GameBase::getRotation(%clientId), 50, 1 );
			Player::applyImpulse(%id, %mom2);
		}

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 67)
	{
		//snipe

		if(getObjectType(%castObj) == "Player")
			%id = Player::getClient(%castObj);

		%trans = GameBase::getMuzzleTransform(%clientId);
		Projectile::spawnProjectile("dforsnipe", %trans, %player, "0 1 0", 7.0);
		Projectile::spawnProjectile("heatLaser", %trans, %player, "0 5 0", 25.0);

		%mom1 = Vector::getFromRot( GameBase::getRotation(%clientId), -60, 1 );
		Player::applyImpulse(%clientId, %mom1);

		%r = $Spell::damageValue[%index];
	
		if(%id != "")
		{
			//%miss = CalcSpellMiss(%clientId, %id, %index);

			SpellDamage(%clientId, %id, %r, %index);
			%mom2 = Vector::getFromRot( GameBase::getRotation(%clientId), 50, 1 );
			Player::applyImpulse(%id, %mom2);
		}

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 52 || %index == 53 || %index == 54 || %index == 55 || %index == 56 || %index == 57)
	{
		//shieldplus from 1-6 in order

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		// Cache name lookups
		%targetName = Client::getName(%id);
		Client::sendMessage(%clientId, $MsgBeige, "Shielding " @ %targetName);
		if(%clientId != %id)
		{
			%casterName = Client::getName(%clientId);
			Client::sendMessage(%id, $MsgBeige, %casterName @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");
		}

		UpdateBonusState(%id, $Spell::damageValue[%index], $Spell::ticks[%index]);
		%castPos = GameBase::getPosition(%id);
		%returnFlag = True;
	}
	if(%index == 13 || %index == 38)
	{
		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb11", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 14)
	{
		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb9", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 15)
	{
		if(%castPos != "")
		{
			CreateAndDetBomb(%clientId, "Bomb7", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 16 || %index == 40)
	{
		if(%castPos != "")
		{
			// Cache position parsing
			%castX = GetWord(%castPos, 0);
			%castY = GetWord(%castPos, 1);
			%castZ = GetWord(%castPos, 2);
			
			%minrad = 0;
			%maxrad = $Spell::radius[%index] / 2;
			for(%i = 0; %i <= 8; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);
				%newPos = (GetWord(%tempPos, 0) + %castX) @ " " @ (GetWord(%tempPos, 1) + %castY) @ " " @ %castZ;
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb10\", \"" @ %newPos @ "\", False, " @ %index @ ", " @ %castToken @ ");", %i / 7, %player);
			}
			CreateAndDetBomb(%clientId, "Bomb10", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 17)
	{
		if(%castPos != "")
		{
			// Cache position parsing
			%castX = GetWord(%castPos, 0);
			%castY = GetWord(%castPos, 1);
			%castZ = GetWord(%castPos, 2);
			
			%minrad = 0;
			%maxrad = $Spell::radius[%index];
			for(%i = 0; %i <= 8; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);
				%newPos = (GetWord(%tempPos, 0) + %castX) @ " " @ (GetWord(%tempPos, 1) + %castY) @ " " @ (%castZ + (%i / 3));
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb12\", \"" @ %newPos @ "\", False, " @ %index @ ", " @ %castToken @ ");", %i / 24, %player);
			}
			CreateAndDetBomb(%clientId, "Bomb12", %castPos, True, %index);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 18)
	{
		if(%castPos != "")
		{
			// Cache position parsing
			%castX = GetWord(%castPos, 0);
			%castY = GetWord(%castPos, 1);
			%castZ = GetWord(%castPos, 2);
			
			%minrad = 0;
			%maxrad = 5;
			for(%i = 0; %i <= 24; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);
				%newPos = (GetWord(%tempPos, 0) + %castX) @ " " @ (GetWord(%tempPos, 1) + %castY) @ " " @ (%castZ + 72 - (%i * 3));
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb9\", \"" @ %newPos @ "\", False, " @ %index @ ", " @ %castToken @ ");", %i / 16, %player);
			}
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb1\", \"" @ %castPos @ "\", True, " @ %index @ ", " @ %castToken @ ");", 1.5, %player);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 19 || %index == 37 || %index == 39)
	{
		if(%castPos != "")
		{
			// Cache position parsing
			%castX = GetWord(%castPos, 0);
			%castY = GetWord(%castPos, 1);
			%castZ = GetWord(%castPos, 2);
			
			%minrad = 0;
			%maxrad = 4;
			for(%i = 0; %i <= 10; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);
				%zOffset = %castZ + (%i / 4);
				%newPos = (GetWord(%tempPos, 0) + %castX) @ " " @ (GetWord(%tempPos, 1) + %castY) @ " " @ %zOffset;
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb7\", \"" @ %newPos @ "\", False, " @ %index @ ", " @ %castToken @ ");", %i / 20, %player);
			}
			for(%i = 0; %i <= 10; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);
				%zOffset = %castZ + (%i / 4);
				%newPos = (GetWord(%tempPos, 0) + %castX) @ " " @ (GetWord(%tempPos, 1) + %castY) @ " " @ %zOffset;
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb8\", \"" @ %newPos @ "\", False, " @ %index @ ", " @ %castToken @ ");", %i / 20, %player);
			}

			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb5\", \"" @ %castPos @ "\", False, " @ %index @ ", " @ %castToken @ ");", 1.0, %player);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb6\", \"" @ %castPos @ "\", False, " @ %index @ ", " @ %castToken @ ");", 1.05, %player);
			schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb14\", \"" @ %castPos @ "\", True, " @ %index @ ", " @ %castToken @ ");", 1.1, %player);

			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
//	if(%index == 21)
//	{
//		if((!fetchData(%clientId, "currentlyRemorting")) && (fetchData(%clientId, "LVL") > fetchData(%clientId, "RemortStep") * 4 + 99 ))
//		{
//			%castPos = DoRemort(%clientId);		
//
	//		%extraDelay = 0.22;
	//		%returnFlag = True;
	//	}
	//	else
	//		%returnFlag = False;
	//}
if (%index == 21)
{
    %remortStep = fetchData(%clientId, "RemortStep");
    %level = fetchData(%clientId, "LVL");

    if (!fetchData(%clientId, "currentlyRemorting"))
    {
        if (%level > %remortStep * 4 + 99)
        {
            %sealValue = GetTotalSealValue();
            // Players can only remort if their remort step is LESS THAN the seal value
            // So at remort 20 with seal value 20, they cannot remort until seal is broken (value becomes 40)
            if (%remortStep < %sealValue)
            {
                %castPos = DoRemort(%clientId);
                %extraDelay = 0.22;
                %returnFlag = True;
                Client::sendMessage(%clientId, $MsgBeige, "Congratulations! You have successfully remorted.");
            }
            else
            {
                %returnFlag = False;
                Client::sendMessage(%clientId, $MsgWhite, "You are being held back by the seal. Help break the seal to increase the remort cap. Try searching around Kronos for a solution.");
            }
        }
        else
        {
            %returnFlag = False;
            Client::sendMessage(%clientId, $MsgWhite, "You are not strong enough to remort yet. Reach level " @ (%remortStep * 4 + 100) @ " to remort.");
        }
    }
    else
    {
        %returnFlag = False;
        Client::sendMessage(%clientId, $MsgWhite, "You are already in the process of remorting.");
    }
}

	if(%index == 22)
	{
		//full heal self spell

		Client::sendMessage(%clientId, $MsgBeige, "Fully healing self");

		setHP(%clientId, fetchData(%clientId, "MaxHP"));

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 23 || %index == 24 || %index == 31)
	{
		//23 = mass heal spell
		//24 = mass full heal spell
		//31 = mass shield spell

		%b = $Spell::radius[%index] * 2;
		%set = newObject("set", SimSet);
		%n = containerBoxFillSet(%set, $SimPlayerObjectType, GameBase::getPosition(%clientId), %b, %b, %b, 0);

		Group::iterateRecursive(%set, DoBoxFunction, %clientId, %index, %w2);
		deleteObject(%set);

		%overrideEndSound = True;

		%returnFlag = True;
	}
	if(%index == 25)
	{
		//shield self spell

		Client::sendMessage(%clientId, $MsgBeige, "Shielding self");

		UpdateBonusState(%clientId, $Spell::damageValue[%index], $Spell::ticks[%index]);

		%castPos = GameBase::getPosition(%clientId);

		%returnFlag = True;
	}
	if(%index == 58 || %index == 59)
	{
//Air Blast
//Air Warp

		%b = $Spell::radius[%index] * 2;
		%set = newObject("set", SimSet);
		%n = containerBoxFillSet(%set, $SimPlayerObjectType, GameBase::getPosition(%clientId), %b, %b, %b, 0);

		Group::iterateRecursive(%set, DoBoxFunction, %clientId, %index, %w2);
		deleteObject(%set);

		%overrideEndSound = True;

		%returnFlag = True;
	}
	
	if(%index == 26 || %index == 27 || %index == 28 || %index == 29 || %index == 30 || %index == 49)
	{
		//shield self or other (LOS) 1st, 2nd, 3rd, 4th, 5th, godly

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		// Cache name lookups
		%targetName = Client::getName(%id);
		Client::sendMessage(%clientId, $MsgBeige, "Shielding " @ %targetName);
		if(%clientId != %id)
		{
			%casterName = Client::getName(%clientId);
			Client::sendMessage(%id, $MsgBeige, %casterName @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");
		}

		UpdateBonusState(%id, $Spell::damageValue[%index], $Spell::ticks[%index]);
		%castPos = GameBase::getPosition(%id);
		%returnFlag = True;
	}
	if(%index == 32)
	{
		//mimic spell
		if(Zone::getType(fetchData(%clientId, "zone")) == "PROTECTED")
		{
			Client::sendMessage(%clientId, $MsgRed, "You can't cast mimic in protected territory.");
			%overrideEndsound = True;
			%returnFlag = False;
		}
		else
		{
			%id = Player::getClient(%castObj);
			if(getObjectType(%castObj) == "Player")
			{
				%skilltype = $SkillType[$Spell::keyword[%index]];
				%troll = fetchData(%id, "LVL") + floor(getRandom() * ($PlayerSkill[%id, %skilltype] + ($PlayerSkill[%id, $SkillSpellResistance] * (1/2)) ));
				%yroll = fetchData(%clientId, "LVL") + floor(getRandom() * $PlayerSkill[%clientId, %skilltype]);

				if(%yroll > %troll)
				{
// ** this code used to put all your items into storage upon mimic.
//					%max = getNumItems();
//					for(%i = 0; %i < %max; %i++)
//					{
//						%checkItem = getItemData(%i);
//						%checkItemCount = Player::getItemCount(%clientId, %checkItem);
//						if(%checkItemCount)
//						{
//							%b = %checkItem;
//							if(%b.className == "Equipped")
//								%b = String::getSubStr(%b, 0, String::len(%b)-1);
//			
//							storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), %b, %checkItemCount));
//							Player::setItemCount(%clientId, %checkItem, 0);
//						}
//					}
					storeData(%clientId, "RACE", fetchData(%id, "RACE"));
					storeData(%clientId, "isMimic", True);
				
					UpdateTeam(%clientId);
					RefreshAll(%clientId);
				
					%castPos = GameBase::getPosition(%clientId);
					%returnFlag = True;
				}
				else
				{
					Client::sendMessage(%clientId, $MsgBeige, "Mimic failed.");
					%overrideEndsound = True;
					%returnFlag = False;
				}
			}
			else
			{
				Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
				%overrideEndsound = True;
				%returnFlag = False;
			}
		}
	}
	if(%index == 33)
	{
		//mass transport spell

		%zoneId = GetZoneByKeywords(%clientId, %w2, 3);

		if(%zoneId != False)
		{
			%b = $Spell::radius[%index] * 2;
			%set = newObject("set", SimSet);
			%n = containerBoxFillSet(%set, $SimPlayerObjectType, GameBase::getPosition(%clientId), %b, %b, %b, 0);

			Group::iterateRecursive(%set, DoBoxFunction, %clientId, %index, %zoneId);
			deleteObject(%set);

			%overrideEndSound = True;

			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Mass Transportation failed.");
			%returnFlag = False;
		}
	}
	if(%index == 41)
	{//windbooster
		%b = GameBase::getRotation(%clientId);
		%c1 = Cap($PlayerSkill[%clientId, $SkillType[boost]] * 1.8 + 20,75,1000);
		%c2 = %c1/3;
		%mom = Vector::getFromRot( %b, %c1, %c2 );
		Player::applyImpulse(%clientId, %mom);
		%returnFlag = True;
	}
	if(%index == 42)
	{//stop
		Item::setVelocity(%clientId, "0 0 10");
		%returnFlag = True;
	}
	if(%index == 43)
	{//airfist
		%player = Client::getOwnedObject(%ClientId);
		if(GameBase::getLOSinfo(%player, 3))
		{
			%targetPlayerObj = $los::object;
			// CRITICAL: Use GetClientIdFromPlayerObject() for proper client ID lookup
			// Player::getClient() returns -1 for AI bots, so we need reverse lookup
			%id = GetClientIdFromPlayerObject(%targetPlayerObj);
			
			// Check if target is a town bot (town bots should not be affected by shoving spells)
			// CRITICAL: Use isTownBot() helper function which properly distinguishes town bots from enemy bots
			// Town bots have BotInfoAiName but NO SpawnBotInfo
			// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
			%isTownBot = isTownBot(%id);
			
			if(%id != -1 && !(Player::isAiControlled(%id) && GameBase::getTeam(%id) == GameBase::getTeam(%clientId)) && !%isTownBot)
			{
            		%b = GameBase::getRotation(%clientId);
				%c1 = Cap($PlayerSkill[%clientId, $SkillType[advshove]]/4 + 30,30,500);
				%c2 = %c1/4;
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
				
				//IHitHim(%clientid,%id,%c1 / 10);
				%returnFlag = True;	
			}
		}
        	else
        	{
			%returnFlag = False;	
		}
	}
	if(%index == 44)
	{//lightstep
		%weight = round(Cap($PlayerSkill[%clientId, $SkillType[lightstep]] / 3 + 40, 40, 3000));
		%amount = "MaxWeight " @ %weight;

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
			%id = %clientId;

		// Cache name lookups
		%targetName = Client::getName(%id);
		Client::sendMessage(%clientId, $MsgBeige, "Lightstepping " @ %targetName);
		if(%clientId != %id)
		{
			%casterName = Client::getName(%clientId);
			Client::sendMessage(%id, $MsgBeige, %casterName @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");
		}

		UpdateBonusState(%id, %amount, $Spell::ticks[%index]);
		refreshAll(%id);
		%castPos = GameBase::getPosition(%id);
		%returnFlag = True;
	}
	if(%index == 45)
	{//advshove
		%player = Client::getOwnedObject(%ClientId);
		if(GameBase::getLOSinfo(%player, 5))
		{
			%targetPlayerObj = $los::object;
			// CRITICAL: Use GetClientIdFromPlayerObject() for proper client ID lookup
			// Player::getClient() returns -1 for AI bots, so we need reverse lookup
			%id = GetClientIdFromPlayerObject(%targetPlayerObj);
			
			// Check if target is a town bot (town bots should not be affected by shoving spells)
			// CRITICAL: Use isTownBot() helper function which properly distinguishes town bots from enemy bots
			// Town bots have BotInfoAiName but NO SpawnBotInfo
			// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
			%isTownBot = isTownBot(%id);
			
			if(%id != -1 && !(Player::isAiControlled(%id) && GameBase::getTeam(%id) == GameBase::getTeam(%clientId)) && !%isTownBot)
			{
            		%b = GameBase::getRotation(%clientId);
				%c1 = Cap($PlayerSkill[%clientId, $SkillType[advshove]]/3 + 70,70,850);
				%c2 = %c1/4;
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
				
				//IHitHim(%clientid,%id,%c1 / 10);
				%returnFlag = True;	
			}
		}
        	else
        	{
			%returnFlag = False;	
		}
	}
	if(%index == 46)
	{//tornado      if only remort looked this cool
		if(%castPos != "")
		{
			// Cache position parsing
			%xpos = GetWord(%castPos, 0);
			%ypos = GetWord(%castPos, 1);
			%zpos = GetWord(%castPos, 2);
			
			// PERFORMANCE: Build target cache (Vertical column, radius 30 is enough)
			SpellTargetCache_Build(%clientId, %castPos, 30);
			$ApocalypseUseCachedDamage[%clientId] = true;
			
			%basePos = %xpos @ " " @ %ypos @ " " @ %zpos;
			
			%counter = 5;
			schedule("playSound(LaunchET, \"" @ %castPos @ "\");", %counter);
			
			// Store explosion data for batch processing
			$TornadoData[%clientId, 0] = "Bomb5 " @ (%zpos + 85) @ " False " @ (%counter + 0.0);
			$TornadoData[%clientId, 1] = "Bomb5 " @ (%zpos + 80) @ " False " @ (%counter + 0.3);
			$TornadoData[%clientId, 2] = "Bomb5 " @ (%zpos + 75) @ " False " @ (%counter + 0.6);
			$TornadoData[%clientId, 3] = "Bomb5 " @ (%zpos + 70) @ " False " @ (%counter + 0.9);
			$TornadoData[%clientId, 4] = "Bomb5 " @ (%zpos + 65) @ " False " @ (%counter + 1.2);
			$TornadoData[%clientId, 5] = "Bomb5 " @ (%zpos + 60) @ " False " @ (%counter + 1.5);
			$TornadoData[%clientId, 6] = "Bomb5 " @ (%zpos + 55) @ " False " @ (%counter + 1.8);
			$TornadoData[%clientId, 7] = "Bomb5 " @ (%zpos + 50) @ " False " @ (%counter + 2.1);
			$TornadoData[%clientId, 8] = "Bomb5 " @ (%zpos + 45) @ " False " @ (%counter + 2.4);
			$TornadoData[%clientId, 9] = "Bomb5 " @ (%zpos + 40) @ " False " @ (%counter + 2.7);
			$TornadoData[%clientId, 10] = "Bomb5 " @ (%zpos + 35) @ " False " @ (%counter + 3.0);
			$TornadoData[%clientId, 11] = "Bomb5 " @ (%zpos + 30) @ " False " @ (%counter + 3.3);
			$TornadoData[%clientId, 12] = "Bomb5 " @ (%zpos + 25) @ " False " @ (%counter + 3.6);
			$TornadoData[%clientId, 13] = "Bomb5 " @ (%zpos + 20) @ " False " @ (%counter + 3.9);
			$TornadoData[%clientId, 14] = "Bomb5 " @ (%zpos + 15) @ " False " @ (%counter + 4.2);
			$TornadoData[%clientId, 15] = "Bomb5 " @ (%zpos + 10) @ " False " @ (%counter + 4.5);
			$TornadoData[%clientId, 16] = "Bomb5 " @ (%zpos + 5) @ " False " @ (%counter + 4.8);
			$TornadoData[%clientId, 17] = "Bomb5 " @ %zpos @ " True " @ (%counter + 5.1);
			$TornadoData[%clientId, 18] = "Bomb5 " @ (%zpos + 5) @ " False " @ (%counter + 5.4);
			$TornadoData[%clientId, 19] = "Bomb5 " @ (%zpos + 10) @ " False " @ (%counter + 5.7);
			$TornadoData[%clientId, 20] = "Bomb5 " @ (%zpos + 15) @ " False " @ (%counter + 6.0);
			$TornadoData[%clientId, 21] = "Bomb5 " @ (%zpos + 20) @ " False " @ (%counter + 6.3);
			$TornadoData[%clientId, 22] = "Bomb5 " @ (%zpos + 25) @ " False " @ (%counter + 6.6);
			$TornadoData[%clientId, 23] = "Bomb5 " @ (%zpos + 30) @ " False " @ (%counter + 6.9);
			$TornadoData[%clientId, 24] = "Bomb5 " @ (%zpos + 25) @ " False " @ (%counter + 7.2);
			$TornadoData[%clientId, 25] = "Bomb5 " @ (%zpos + 20) @ " False " @ (%counter + 7.5);
			$TornadoData[%clientId, 26] = "Bomb5 " @ (%zpos + 15) @ " False " @ (%counter + 7.8);
			$TornadoData[%clientId, 27] = "Bomb5 " @ (%zpos + 10) @ " False " @ (%counter + 8.1);
			$TornadoData[%clientId, 28] = "Bomb5 " @ (%zpos + 5) @ " False " @ (%counter + 8.4);
			$TornadoData[%clientId, 29] = "Bomb5 " @ %zpos @ " True " @ (%counter + 8.7);
			$TornadoData[%clientId, 30] = "Bomb5 " @ %zpos @ " False " @ (%counter + 9.0);
			$TornadoData[%clientId, 31] = "Bomb5 " @ %zpos @ " True " @ (%counter + 9.3);
			$TornadoData[%clientId, 32] = "Bomb5 " @ %zpos @ " False " @ (%counter + 9.6);
			$TornadoData[%clientId, 33] = "Bomb5 " @ %zpos @ " True " @ (%counter + 9.9);
			$TornadoData[%clientId, 34] = "Bomb5 " @ %zpos @ " False " @ (%counter + 10.2);
			$TornadoData[%clientId, 35] = "Bomb5 " @ %zpos @ " True " @ (%counter + 10.5);
			$TornadoData[%clientId, 36] = "Bomb5 " @ %zpos @ " False " @ (%counter + 10.8);
			$TornadoDataCount[%clientId] = 37;
			$TornadoBasePos[%clientId] = %basePos;
			
			// Start batch processing
			schedule("TornadoBatchExplosions(" @ %clientId @ ", " @ %index @ ", 0);", %counter);
			
			Client::sendMessage(%clientId, $MsgBeige, "Run for Cover.");
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
	if(%index == 50)
	{//Ion Blast - OPTIMIZED BATCH VERSION
		if(%castPos != "")
		{
			// Validate clientId
			if(%clientId == 0 || %clientId == -1)
			{
				Client::sendMessage(%clientId, $MsgBeige, "Error: Invalid client ID.");
				%returnFlag = False;
				return;
			}
			
			// Cache position parsing
			%castX = GetWord(%castPos, 0);
			%castY = GetWord(%castPos, 1);
			%castZ = GetWord(%castPos, 2);
			
			// Initialize Batch Data
			if($ApocalypseDataToken[%clientId] == "" || $ApocalypseDataToken[%clientId] == -1)
				$ApocalypseDataToken[%clientId] = 0;
			$ApocalypseDataToken[%clientId]++;
			%apocToken = $ApocalypseDataToken[%clientId];

			$ApocalypseBasePos[%clientId] = %castPos;
			$ApocalypseUseCachedDamage[%clientId] = true;
			
			// Build target cache for the entire duration (50m spell rad + 40m offset)
			SpellTargetCache_Build(%clientId, %castPos, 90);
			$ApocalypseUseCachedDamage[%clientId] = true;
			
			%minrad = 0;
			%maxrad = 40;
			
			%count = 0;
			// 3. Interleaved Generation (Visuals + Damage together) to ensure Sorted Time
			// This prevents batching issues and ensures the spell finishes in ~4 seconds
			for(%t = 0; %t <= 40; %t++)
			{
				%curTime = %t * 0.1;
				
				// Add 1-2 random visuals per tick (Total ~60 visuals)
				%numVis = 1 + floor(getRandom() * 1.5);
				for(%v = 0; %v < %numVis; %v++)
				{
					%rnd = floor(getRandom() * 6);
					%bType = "Bomb20";
					if(%rnd == 1) %bType = "Bomb21";
					else if(%rnd == 2) %bType = "Bomb1";
					else if(%rnd == 3) %bType = "Bomb6";
					else if(%rnd == 4) %bType = "Bomb17";
					else if(%rnd == 5) %bType = "Bomb14";
					
					%tempPos = RandomPositionXY(%minrad, %maxrad);
					%xOff = GetWord(%tempPos, 0);
					%yOff = GetWord(%tempPos, 1);
					%zOff = (%t / 10);
					
					// Format: type, x, y, z, damage, delay
					$ApocalypseData[%clientId, %count] = %bType @ " " @ %xOff @ " " @ %yOff @ " " @ %zOff @ " False " @ %curTime;
					%count++;
				}
				
				// Add Damage Bomb every 2nd tick (Total 21 damage bombs)
				if(%t % 2 == 0)
				{
					// Bomb23 only (IonShockwave2, faceCamera=true): keeps Ion Blast pure
					// rocketdata. Bomb22 (IonShockwave) is a $SpellRingBomb and would
					// spawn Mines, which lag when many damage hits land at once.
					%dType = "Bomb23";

					%tempPos = RandomPositionXY(%minrad, %maxrad);
					%xOff = GetWord(%tempPos, 0);
					%yOff = GetWord(%tempPos, 1);
					%zOff = (%t / 10);
					
					$ApocalypseData[%clientId, %count] = %dType @ " " @ %xOff @ " " @ %yOff @ " " @ %zOff @ " True " @ %curTime;
					%count++;
				}
			}
			
			// Final center blasts at end (T=4.1s) - Bomb23 only (pure rocketdata, see above)
			$ApocalypseData[%clientId, %count] = "Bomb23 0 0 0 True 4.1";
			%count++;
			$ApocalypseData[%clientId, %count] = "Bomb23 0 0 0 True 4.1";
			%count++;
			
			$ApocalypseDataCount[%clientId] = %count;
			
			// Start batch processing
			schedule("ApocalypseBatchExplosions(" @ %clientId @ ", " @ %index @ ", 0, 0, " @ %apocToken @ ");", 0.1);
			
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	if(%index == 51)
	{//Psynergy Liquifier
		if(%castPos != "")
		{
			// Cache position parsing
			%castX = GetWord(%castPos, 0);
			%castY = GetWord(%castPos, 1);
			%castZ = GetWord(%castPos, 2);
			%zPos = %castZ + 600;
			
			%minrad = 0;
			%maxrad = 12;
			for(%i = 0; %i < 25; %i++)
			{
				%tempPos = RandomPositionXY(%minrad, %maxrad);
				%xPos = GetWord(%tempPos, 0) + %castX;
				%yPos = GetWord(%tempPos, 1) + %castY;
				schedule("Liquifier::fireLaser(" @ %player @ ", " @ %xPos @ ", " @ %yPos @ ", " @ %zPos @ ");", %i / 15);
			}
			%newPos = %castX @ " " @ %castY @ " " @ (%castZ + 1.4);
			for(%i = 0; %i < 4; %i++)
				schedule("CreateAndDetBomb(" @ %clientId @ ", \"Bomb23\", \"" @ %newPos @ "\", true, " @ %index @ ", " @ %castToken @ ");", %i / 5);
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
	function Liquifier::fireLaser(%player,%x,%y,%z)
{
	%pos = %x@" "@%y@" "@%z;
	%turret = Liquifier::deployTurret(%player,%pos);
	%trans = GameBase::getMuzzleTransform(%turret);
	%vel = "0 0 0";
	%trans1= getWord(%trans,0);
	%trans2= getWord(%trans,1);
	%trans3= getWord(%trans,2);
//	%trans4= getWord(%trans,3);
//	%trans5= getWord(%trans,4);
//	%trans6= getWord(%trans,5);
	%trans4= (getRandom()*2-getRandom()*3)/80;
	%trans5= (getRandom()*2-getRandom()*3)/80;
	%trans6= -1.3-getRandom()/50;
	%trans7= getWord(%trans,6);
	%trans8= getWord(%trans,7);
	%trans9= getWord(%trans,8);
	%trans10=%x;
	%trans11=%y;
	%trans12=%z;
	%trans = %trans1 @" "@ %trans2 @" "@ %trans3 @" "@ %trans4 @" "@ %trans5 @" "@ %trans6 @" "@ %trans7 @" "@ %trans8 @" "@ %trans9 @" "@ %trans10 @" "@ %trans11 @" "@ %trans12;
	Projectile::spawnProjectile("heatLaser",%trans,%turret,%vel);
	Projectile::spawnProjectile("heatLaser",%trans,%turret,%vel);
	Projectile::spawnProjectile("heatLaser",%trans,%turret,%vel);
}
function Liquifier::deployTurret(%player,%pos)
{
	%client = Player::getClient(%player);
	%rot = "0 0 0";
	%turret = newObject("Option","Turret",HeatLaserCannon,true);
	addToSet("MissionCleanup",%turret);
	GameBase::setTeam(%turret,GameBase::getTeam(%player));
	GameBase::setPosition(%turret,%pos);
	GameBase::setRotation(%turret,%rot);
	Client::setOwnedObject(%client,%turret);
	Client::setOwnedObject(%client,%player);
	schedule("GameBase::setDamageLevel("@%turret@",999);",0.1);
	return %turret;
}
function Turret::objectiveDestroyed() {}


	if(%index == 47)
	{//heavystep
		%weight = round(Cap($PlayerSkill[%clientId, $SkillType[heavystep]] / 5 + 20, 20, 3000));
		%amount = "MaxWeight " @ (%weight * -1);

		if(getObjectType(%castObj) == "Player" && !Player::isAiControlled(%clientId))
			%id = Player::getClient(%castObj);
		else
		{
			// review #18: %TrueClientId is never bound in DoCastSpell (it exists only
			// in comchat.cs's remoteSay), so all three reads here silently resolved to
			// "" - the airfist/advshove same-team guards above were dead, and this
			// failure message went to client "" instead of the caster. Use %clientId.
			Client::sendMessage(%clientId, $MsgWhite, "Unable to find target.");
			// review #38: replicate the shared failure tail inline instead of a bare
			// return - every other spell failure path falls through to it and grants
			// partial (+0.3) skill-training credit + the SpellCastStep bookkeeping.
			// (TorqueScript has no goto, so the common tail's %returnFlag==False block
			// is duplicated here.)
			storeData(%clientId, "SpellCastStep", 2);
			UseSkill(%clientId, %skilltype, False, True);
			UseSkill(%clientId, $SkillEnergy, False, True);
			return False;
		}

		// Cache name lookups
		%targetName = Client::getName(%id);
		Client::sendMessage(%clientId, $MsgBeige, "Heavystepping " @ %targetName);
		if(%clientId != %id)
		{
			%casterName = Client::getName(%clientId);
			Client::sendMessage(%id, $MsgBeige, %casterName @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");
		}

		UpdateBonusState(%id, %amount, $Spell::ticks[%index]);
		refreshAll(%id);
		%castPos = GameBase::getPosition(%id);
		%returnFlag = True;
	}

	if(%index == 48)
	{//apocalypse      if only remort looked this cool - OPTIMIZED VERSION
		if(%castPos != "")
		{
			// Validate clientId - ensure it's a valid player client (not 0 or -1)
			if(%clientId == 0 || %clientId == -1)
			{
				Client::sendMessage(%clientId, $MsgBeige, "Error: Invalid client ID for spell cast.");
				%returnFlag = False;
				return;
			}
			
			%xpos=getword(%castpos,0);
			%ypos=getword(%castpos,1);
			%zpos=getword(%castpos,2);
			
			// Stamp this Apocalypse cast with a generation token so stale scheduled
			// callbacks from older casts cannot wipe/override current data.
			if($ApocalypseDataToken[%clientId] == "" || $ApocalypseDataToken[%clientId] == -1)
				$ApocalypseDataToken[%clientId] = 0;
			$ApocalypseDataToken[%clientId]++;
			%apocToken = $ApocalypseDataToken[%clientId];
			
			// PERFORMANCE: Build target cache (Max offset is ~30, spell radius is 100)
			SpellTargetCache_Build(%clientId, %castPos, 130);
			$ApocalypseUseCachedDamage[%clientId] = true;
			
			// Store explosion data in arrays for batch processing
			// Format: bombType, xOffset, yOffset, zOffset, doDamage, delay
			$ApocalypseData[%clientId, 0] = "Bomb5 0 0 85 False 0.0";
			$ApocalypseData[%clientId, 1] = "Bomb5 0 30 75 False 0.3";
			$ApocalypseData[%clientId, 2] = "Bomb5 0 -30 65 False 0.6";
			$ApocalypseData[%clientId, 3] = "Bomb5 0 0 55 False 0.9";
			$ApocalypseData[%clientId, 4] = "Bomb5 30 0 45 False 1.2";
			$ApocalypseData[%clientId, 5] = "Bomb5 -30 0 35 False 1.5";
			$ApocalypseData[%clientId, 6] = "Bomb5 0 0 25 False 1.8";
			$ApocalypseData[%clientId, 7] = "Bomb5 30 30 15 False 2.1";
			$ApocalypseData[%clientId, 8] = "Bomb5 30 -30 5 False 2.4";
			$ApocalypseData[%clientId, 9] = "Bomb14 -30 -30 0 True 2.7";
			$ApocalypseData[%clientId, 10] = "Bomb5 -30 30 10 False 3.0";
			$ApocalypseData[%clientId, 11] = "Bomb5 0 0 20 False 3.3";
			$ApocalypseData[%clientId, 12] = "Bomb5 0 0 10 False 3.6";
			$ApocalypseData[%clientId, 13] = "Bomb14 0 0 0 True 3.9";
			$ApocalypseData[%clientId, 14] = "Bomb5 0 30 10 False 4.2";
			$ApocalypseData[%clientId, 15] = "Bomb5 0 -30 20 False 4.5";
			$ApocalypseData[%clientId, 16] = "Bomb5 30 0 10 False 4.8";
			$ApocalypseData[%clientId, 17] = "Bomb14 0 0 0 True 5.1";
			$ApocalypseData[%clientId, 18] = "Bomb5 0 0 10 False 5.4";
			$ApocalypseData[%clientId, 19] = "Bomb5 30 0 20 False 5.7";
			$ApocalypseData[%clientId, 20] = "Bomb5 -30 0 10 False 6.0";
			$ApocalypseData[%clientId, 21] = "Bomb14 0 0 0 True 6.3";
			$ApocalypseData[%clientId, 22] = "Bomb5 -30 30 10 False 6.6";
			$ApocalypseData[%clientId, 23] = "Bomb14 0 0 0 True 6.9";
			$ApocalypseData[%clientId, 24] = "Bomb5 30 -30 10 False 7.2";
			$ApocalypseData[%clientId, 25] = "Bomb14 0 0 0 True 7.5";
			$ApocalypseData[%clientId, 26] = "Bomb5 30 30 10 False 7.8";
			$ApocalypseData[%clientId, 27] = "Bomb14 0 0 0 True 8.1";
			$ApocalypseData[%clientId, 28] = "Bomb5 -30 -30 0 False 8.4";
			$ApocalypseData[%clientId, 29] = "Bomb14 0 0 0 True 8.7";
			$ApocalypseData[%clientId, 30] = "Bomb5 30 -30 0 False 9.0";
			$ApocalypseData[%clientId, 31] = "Bomb14 0 0 0 True 9.3";
			$ApocalypseData[%clientId, 32] = "Bomb5 -30 30 0 False 9.6";
			$ApocalypseData[%clientId, 33] = "Bomb14 0 0 0 True 9.9";
			$ApocalypseData[%clientId, 34] = "Bomb5 0 30 0 False 10.2";
			$ApocalypseData[%clientId, 35] = "Bomb14 0 0 0 True 10.5";
			$ApocalypseData[%clientId, 36] = "Bomb5 -30 0 0 False 10.8";
			$ApocalypseDataCount[%clientId] = 37;
			$ApocalypseBasePos[%clientId] = %xpos @ " " @ %ypos @ " " @ %zpos;
			
			// Start the batch processing
			schedule("playSound(LaunchET, \"" @ %castPos @ "\");", 5);
			schedule("ApocalypseBatchExplosions(" @ %clientId @ ", " @ %index @ ", 0, 0, " @ %apocToken @ ");", 5);
			
			Client::sendMessage(%clientId, $MsgBeige, "Run for Cover.");
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}
	if(%index == 66)
	{//apocalypse      if only remort looked this cool - OPTIMIZED VERSION
		if(%castPos != "")
		{
			%xpos=getword(%castpos,0);
			%ypos=getword(%castpos,1);
			%zpos=getword(%castpos,2);
			
			if($ApocalypseDataToken[%clientId] == "" || $ApocalypseDataToken[%clientId] == -1)
				$ApocalypseDataToken[%clientId] = 0;
			$ApocalypseDataToken[%clientId]++;
			%apocToken = $ApocalypseDataToken[%clientId];
			
			// Store explosion data in arrays for batch processing
			$ApocalypseData[%clientId, 0] = "Bomb5 0 0 85 True 0.0";
			$ApocalypseData[%clientId, 1] = "Bomb14 0 30 75 True 0.3";
			$ApocalypseData[%clientId, 2] = "Bomb5 0 -30 65 True 0.6";
			$ApocalypseData[%clientId, 3] = "Bomb10 0 0 55 True 0.9";
			$ApocalypseData[%clientId, 4] = "Bomb14 30 0 45 True 1.2";
			$ApocalypseData[%clientId, 5] = "Bomb5 -30 0 35 True 1.5";
			$ApocalypseData[%clientId, 6] = "Bomb5 0 0 25 True 1.8";
			$ApocalypseData[%clientId, 7] = "Bomb14 30 30 15 True 2.1";
			$ApocalypseData[%clientId, 8] = "Bomb5 30 -30 5 True 2.4";
			$ApocalypseData[%clientId, 9] = "Bomb14 -30 -30 0 True 2.7";
			$ApocalypseDataCount[%clientId] = 10;
			$ApocalypseBasePos[%clientId] = %xpos @ " " @ %ypos @ " " @ %zpos;
			
			// PERFORMANCE: Pre-cache all entities in the spell's maximum radius
			// This avoids calling containerBoxFillSet for every explosion (10+ times)
			// Max radius includes spell radius (200) + max offset (30) = 230
			%maxSpellRadius = $Spell::radius[%index] + 150; // 150 is the max z-offset for index 66
			SpellTargetCache_Build(%clientId, %castPos, %maxSpellRadius);
			$ApocalypseUseCachedDamage[%clientId] = true;
			
			// Start the batch processing
			schedule("playSound(LaunchET, \"" @ %castPos @ "\");", 5);
			schedule("ApocalypseBatchExplosions(" @ %clientId @ ", " @ %index @ ", 0, 0, " @ %apocToken @ ");", 5);
			
			Client::sendMessage(%clientId, $MsgBeige, "Run for Cover.");
			%overrideEndSound = True;
			%returnFlag = True;
		}
		else
		{
			Client::sendMessage(%clientId, $MsgBeige, "Could not find a target.");
			%returnFlag = False;
		}
	}

	Player::setAnimation(%clientId, 39);

	if(!%overrideEndSound)
	{
		if(%extraDelay == "")
			playSound($Spell::endSound[%index], %castPos);
		else
			schedule("playSound(" @ $Spell::endSound[%index] @ ", \"" @ %castPos @ "\");", %extraDelay);
	}


	//==================================================================

	if(%returnFlag == True)
	{
		storeData(%clientId, "SpellCastStep", 2);

		if(%skilltype == $SkillNeutralCasting || %skilltype == $SkillDefensiveCasting)
			UseSkill(%clientId, %skilltype, True, True);
		UseSkill(%clientId, $SkillEnergy, True, True);

		return True;
	}
	else if(%returnFlag == False)
	{
		storeData(%clientId, "SpellCastStep", 2);

		UseSkill(%clientId, %skilltype, False, True);
		UseSkill(%clientId, $SkillEnergy, False, True);

		return False;
	}
}

function SpellPowerCloudPulse(%clientId, %castPos, %index, %expectedCasterName, %token, %castToken)
{
	if($PowerCloudCacheToken[%clientId] != %token)
	{
		if($SPELL_DEBUG)
			echo("[SPELL DEBUG] PowerCloud pulse blocked (token mismatch). clientId=" @ %clientId @ " expected=" @ %token @ " active=" @ $PowerCloudCacheToken[%clientId]);
		return;
	}
	
	if(!SpellCast_IsValid(%clientId, %expectedCasterName, %castToken, "PowerCloud pulse"))
		return;
	
	if(%castPos == "" || %castPos == -1)
		return;

	%casterObj = Client::getOwnedObject(%clientId);
	if(%casterObj == -1 || %casterObj == "" || !isObject(%casterObj) || getObjectType(%casterObj) != "Player")
	{
		if($SPELL_DEBUG)
			echo("[SPELL DEBUG] PowerCloud pulse blocked (invalid caster object). clientId=" @ %clientId @ " casterObj=" @ %casterObj);
		return;
	}
	
	// count == 0 is a valid cache (all targets filtered, e.g. town bots) -
	// keep the visual pulse but let SpellRadiusDamage_Cached no-op the damage.
	if($SpellTargetCache[%clientId, "count"] != "")
	{
		CreateAndDetBomb_VisualOnly(%clientId, "Bomb2", %castPos, %index);
		SpellRadiusDamage_Cached(%clientId, %castPos, %index);
	}
	else
	{
		CreateAndDetBomb(%clientId, "Bomb2", %castPos, 1, %index, %castToken);
	}
}

function CreateAndDetBomb(%clientId, %b, %castPos, %doDamage, %index, %castToken)
{
	dbecho($dbechoMode, "CreateAndDetBomb(" @ %clientId @ ", " @ %b @ ", " @ %castPos @ ", " @ %index @ ")");

	if(%castToken != "" && %castToken != -1 && !SpellCast_IsValid(%clientId, "", %castToken, "spell bomb"))
		return;

	// Convert bomb type (e.g., "Bomb1") to projectile name (e.g., "SpellBomb1")
	%projName = "Spell" @ %b;
	
	// Hybrid mode keeps Mine visuals; rocket mode skips Mine creation and uses projectile-only FX.
	%sourceObj = SpellResolveSourceObject(%clientId);
	
	if(%sourceObj != -1)
	{
		// Ring bombs always take the Mine path: Mine::Detonate explodes with the
		// mine's upright axis, while rocket timeout explosions are rotated 90 degrees.
		if(SpellExplosion_UseHybrid() || $SpellRingBomb[%b])
		{
			%bomb = newObject("", "Mine", %b);
			if(%bomb != -1)
			{
				addToSet("MissionCleanup", %bomb);
				GameBase::setPosition(%bomb, %castPos);
				schedule("if(isObject(" @ %bomb @ ")) deleteObject(" @ %bomb @ ");", 0.4);
			}
		}

		// Skip the projectile for ring bombs so the Mine's flat ring isn't
		// doubled with a rotated copy.
		if(!$SpellRingBomb[%b])
		{
			if(%index == 46)
				%trans = BuildTornadoBombTransform(%castPos);
			else
				%trans = BuildSpellBombTransform(%sourceObj, %castPos);
			if(%trans != "")
				Projectile::spawnProjectile(%projName, %trans, %sourceObj, "0 0 0");
		}
	}
	else if($SPELL_DEBUG)
	{
		echo("[SPELL DEBUG] CreateAndDetBomb: No valid source object for clientId " @ %clientId @ ", bomb=" @ %b);
	}

	if(%doDamage && %clientId != 0 && %clientId != -1)
		SpellRadiusDamage(%clientId, %castPos, %index);
	
	// PERFORMANCE: Throttle sounds to avoid network clatter for high-density spells
	if(getSimTime() - $LastSpellExplosionSound > 0.1)
	{
		$LastSpellExplosionSound = getSimTime();
		playSound($Spell::endSound[%index], %castPos);
	}
}

// Visual-only version of CreateAndDetBomb for use with cached damage system
// Creates the explosion effects but skips the damage calculation
function CreateAndDetBomb_VisualOnly(%clientId, %b, %castPos, %index)
{
	dbecho($dbechoMode, "CreateAndDetBomb_VisualOnly(" @ %clientId @ ", " @ %b @ ", " @ %castPos @ ", " @ %index @ ")");

	%projName = "Spell" @ %b;
	
	// Hybrid mode keeps Mine visuals; rocket mode skips Mine creation and uses projectile-only FX.
	%sourceObj = SpellResolveSourceObject(%clientId);
	
	if(%sourceObj != -1)
	{
		// Ring bombs always take the Mine path: Mine::Detonate explodes with the
		// mine's upright axis, while rocket timeout explosions are rotated 90 degrees.
		if(SpellExplosion_UseHybrid() || $SpellRingBomb[%b])
		{
			%bomb = newObject("", "Mine", %b);
			if(%bomb != -1)
			{
				addToSet("MissionCleanup", %bomb);
				GameBase::setPosition(%bomb, %castPos);
				schedule("if(isObject(" @ %bomb @ ")) deleteObject(" @ %bomb @ ");", 0.4);
			}
		}

		// Skip the projectile for ring bombs so the Mine's flat ring isn't
		// doubled with a rotated copy.
		if(!$SpellRingBomb[%b])
		{
			if(%index == 46)
				%trans = BuildTornadoBombTransform(%castPos);
			else
				%trans = BuildSpellBombTransform(%sourceObj, %castPos);
			if(%trans != "")
				Projectile::spawnProjectile(%projName, %trans, %sourceObj, "0 0 0");
		}
	}
	else if($SPELL_DEBUG)
	{
		echo("[SPELL DEBUG] CreateAndDetBomb_VisualOnly: No valid source object for clientId " @ %clientId @ ", bomb=" @ %b);
	}
	
	// NOTE: No damage calculation here - that's done via SpellRadiusDamage_Cached
	
	// PERFORMANCE: Throttle sounds
	if(getSimTime() - $LastSpellExplosionSound > 0.1)
	{
		$LastSpellExplosionSound = getSimTime();
		playSound($Spell::endSound[%index], %castPos);
	}
}

// Optimized batch processing function for Apocalypse spell
function ApocalypseBatchExplosions(%clientId, %index, %startIdx, %silent, %token)
{
	// Validate clientId - if it's 0 or invalid, try to get it from stored data
	if(%clientId == 0 || %clientId == -1)
	{
		// Try to find a valid clientId from the stored data
		// This shouldn't happen, but handle it gracefully
		return;
	}
	
	// Default token for compatibility with older call sites.
	if(%token == "" || %token == -1)
		%token = $ApocalypseDataToken[%clientId];
	
	// Ignore stale callbacks from older Apocalypse casts.
	if(%token != $ApocalypseDataToken[%clientId])
		return;
	
	%basePos = $ApocalypseBasePos[%clientId];
	%count = $ApocalypseDataCount[%clientId];
	
	if(%basePos == "" || %count == "")
		return; // Data not found, abort
	
	%xpos = getWord(%basePos, 0);
	%ypos = getWord(%basePos, 1);
	%zpos = getWord(%basePos, 2);
	
	%batchSize = 10; // Process 10 explosions per batch
	// PERFORMANCE: If silent (Echo), we can process even more damage-only hits per batch
	if(%silent) %batchSize = 25;
	
	// Get the start delay for this batch to calculate relative scheduling
	%firstData = $ApocalypseData[%clientId, %startIdx];
	%startDelay = 0;
	if(%firstData != "")
		%startDelay = getWord(%firstData, 5);
	
	// Normalize silent value to 0 or 1 for safe use in schedule()
	if(%silent == "" || %silent == "false" || %silent == "0") %silent = 0;
	else %silent = 1;
	
	// Process explosions in batches
	if($SPELL_DEBUG) echo("[SPELL DEBUG] Batch Start: " @ %index @ " Client: " @ %clientId @ " StartIdx: " @ %startIdx @ " Silent: " @ %silent);
	
	%processedInBatch = 0;
	%damageExplosionsThisBatch = 0;
	for(%i = %startIdx; %i < %count && %processedInBatch < %batchSize; %i++)
	{
		%data = $ApocalypseData[%clientId, %i];
		if(%data == "")
			continue;
		
		%bombType = getWord(%data, 0);
		%xOffset = getWord(%data, 1);
		%yOffset = getWord(%data, 2);
		%zOffset = getWord(%data, 3);
		%doDamageStr = getWord(%data, 4);
		%delay = getWord(%data, 5);
		
		// PERFORMANCE: If silent (Echo), skip visual-only entries entirely to save scheduling overhead
		if(%silent && %doDamageStr == "False")
			continue;
		
		%processedInBatch++;
		
		// Convert string to boolean (numeric 1 or 0 for schedule compatibility)
		%doDamage = (%doDamageStr == "True");
		if(%doDamage)
			%doDamageNum = 1;
		else
			%doDamageNum = 0;
		
		%pos = (%xpos + %xOffset) @ " " @ (%ypos + %yOffset) @ " " @ (%zpos + %zOffset);
		
		// Calculate delay relative to the START of this batch
		// This ensures explosions are spaced out correctly (e.g. 0.1s, 0.2s, 0.3s)
		%relativeDelay = %delay - %startDelay;
		if(%relativeDelay < 0) %relativeDelay = 0;
		
		// Schedule this explosion with proper timing
		// Using explicit 0/1 for numeric values to avoid syntax errors in console
		if(%relativeDelay > 0)
		{
			schedule("ApocalypseCreateExplosion(" @ %clientId @ ", \"" @ %bombType @ "\", \"" @ %pos @ "\", " @ %doDamageNum @ ", " @ %index @ ", " @ %silent @ ");", %relativeDelay);
		}
		else
		{
			ApocalypseCreateExplosion(%clientId, %bombType, %pos, %doDamageNum, %index, %silent);
		}
		
		%lastDelay = %delay;
	}
	
	// Schedule next batch if there are more explosions
	%nextIdx = %i;
	if(%nextIdx < %count)
	{
		%nextData = $ApocalypseData[%clientId, %nextIdx];
		if(%nextData != "")
		{
			// Wait for explosions in THIS batch to complete, then start next batch
			// %lastDelay is the absolute time of the last explosion we scheduled
			// %startDelay is the absolute time of the first explosion in this batch
			// The difference is how long the explosions in this batch take to fire
			%batchDuration = %lastDelay - %startDelay;
			if(%batchDuration < 0.1) %batchDuration = 0.1;
			
			schedule("ApocalypseBatchExplosions(" @ %clientId @ ", " @ %index @ ", " @ %nextIdx @ ", " @ %silent @ ", " @ %token @ ");", %batchDuration);
		}
	}
	else
	{
		// Cleanup when all batches are done - BUT ONLY IF NOT SILENT
		// Silent batches reuse the data from the main visual batch, so they shouldn't clean it up
		if(!%silent)
		{
			// Clean up data arrays after a short delay to ensure all explosions are processed
			schedule("ApocalypseCleanup(" @ %clientId @ ", " @ %count @ ", " @ %token @ ");", 12.0);
		}
	}

	if($SPELL_DEBUG) echo("[SPELL DEBUG] Batch Done: " @ %index @ " Processed: " @ %processedInBatch);
}

// Helper function to create a single explosion
function ApocalypseCreateExplosion(%clientId, %bombType, %pos, %doDamage, %index, %silent)
{
	// Validate clientId before proceeding
	if(%clientId == 0 || %clientId == -1)
		return; // Invalid client ID, abort
	
	if(%doDamage)
	{
		// PERFORMANCE: Use cached entities if available, otherwise fall back to standard
		if($ApocalypseUseCachedDamage[%clientId] == true)
		{
			// If silent (echo), only do damage - skip visuals
			if(!%silent)
			{
				// Create visual explosion without damage (we do damage separately with cache)
				CreateAndDetBomb_VisualOnly(%clientId, %bombType, %pos, %index);
			}
			
			// Apply damage using cached entity list (silent/echo does this too)
			SpellRadiusDamage_Cached(%clientId, %pos, %index);
		}
		else
		{
			if(%silent)
			{
				// Silent uncached path: Visuals skipped, just damage
				SpellRadiusDamage(%clientId, %pos, %index);
			}
			else
			{
				// Standard path - queries containerBoxFillSet each time
				CreateAndDetBomb(%clientId, %bombType, %pos, True, %index);
			}
		}
	}
	else
	{
		// NO DAMAGE case (visual only)
		// If silent (echo), we do nothing at all to save performance
		if(%silent) return;

		// PERFORMANCE: Use the optimized visual-only helper instead of re-implementing
		// so mode switching (hybrid vs rocket) and sound throttling stay centralized.
		CreateAndDetBomb_VisualOnly(%clientId, %bombType, %pos, %index);
	}
}

// Cleanup function to remove data arrays
function ApocalypseCleanup(%clientId, %count, %token)
{
	// Ignore stale cleanup callbacks from older casts.
	if(%token != $ApocalypseDataToken[%clientId])
		return;

	if($SPELL_DEBUG) echo("[SPELL DEBUG] Apocalypse Finished. Total Damage Calls: " @ $SpellTotalDamageCalls[%clientId]);
	$SpellTotalDamageCalls[%clientId] = 0;

	for(%i = 0; %i < %count; %i++)
		$ApocalypseData[%clientId, %i] = "";
	$ApocalypseDataCount[%clientId] = "";
	$ApocalypseBasePos[%clientId] = "";
	$ApocalypseUseCachedDamage[%clientId] = "";
	
	// Also clear cached spell targets
	SpellTargetCache_Clear(%clientId);
}

//============================================================================
// SPELL TARGET CACHING SYSTEM
// Performance optimization: Cache entities at spell start instead of querying
// containerBoxFillSet for every explosion. Reduces O(N*E) to O(N+E) where
// N = number of explosions and E = number of entities in range.
//============================================================================

// Cache all entities within max spell radius around a position
// Call this ONCE when AOE spell starts, before any explosions
function SpellTargetCache_Build(%clientId, %centerPos, %maxRadius)
{
	// Clear any existing cache
	SpellTargetCache_Clear(%clientId);
	
	// Query all players within the maximum radius
	%boxSize = %maxRadius * 2;
	%set = newObject("set", SimSet);
	%n = containerBoxFillSet(%set, $SimPlayerObjectType, %centerPos, %boxSize, %boxSize, %boxSize, 0);
	
	// Store cached entities in global arrays ("count" is set after the loop,
	// since town bots are filtered out below)
	$SpellTargetCache[%clientId, "centerPos"] = %centerPos;
	$SpellTargetCache[%clientId, "maxRadius"] = %maxRadius;
	
	// Store each entity's object ID and position (position cached for distance calculations)
	%stored = 0;
	for(%i = 0; %i < %n; %i++)
	{
		%obj = Group::getObject(%set, %i);

		// Town bots can never take spell damage (always nulled in Player::onDamage),
		// so exclude them here. Checking once at cache build avoids running the full
		// damage pipeline for every explosion against every town bot in a populated town.
		%targetClientId = Player::getClient(%obj);
		if(%targetClientId == -1)
			%targetClientId = GetClientIdFromPlayerObject(%obj);
		if(%targetClientId != -1 && isTownBot(%targetClientId))
			continue;

		$SpellTargetCache[%clientId, "obj", %stored] = %obj;
		$SpellTargetCache[%clientId, "pos", %stored] = GameBase::getPosition(%obj);
		%stored++;
	}
	$SpellTargetCache[%clientId, "count"] = %stored;

	deleteObject(%set);

	return %stored;
}

// Clear the cached entities for a client
function SpellTargetCache_Clear(%clientId)
{
	%count = $SpellTargetCache[%clientId, "count"];
	if(%count == "")
		return;

	for(%i = 0; %i < %count; %i++)
	{
		$SpellTargetCache[%clientId, "obj", %i] = "";
		$SpellTargetCache[%clientId, "pos", %i] = "";
	}
	$SpellTargetCache[%clientId, "count"] = "";
	$SpellTargetCache[%clientId, "centerPos"] = "";
	$SpellTargetCache[%clientId, "maxRadius"] = "";
}

// Apply radius damage using CACHED entities instead of containerBoxFillSet
// This is the optimized version of SpellRadiusDamage for batch explosions
function SpellRadiusDamage_Cached(%clientId, %explosionPos, %index)
{
	dbecho($dbechoMode, "SpellRadiusDamage_Cached(" @ %clientId @ ", " @ %explosionPos @ ", " @ %index @ ")");
	
	%count = $SpellTargetCache[%clientId, "count"];
	if(%count == "")
	{
		// No cache - fall back to standard method (shouldn't happen but handle gracefully)
		SpellRadiusDamage(%clientId, %explosionPos, %index);
		return;
	}
	// count == 0 means a cache WAS built but every entity in range was filtered out
	// (e.g. all town bots) - nothing to damage, and we must NOT fall back to the
	// uncached path or every explosion would re-query and re-filter the same targets.
	if(%count == 0)
		return;
	
	%spellRadius = $Spell::radius[%index];
	
	// Iterate through cached entities and apply damage based on distance from THIS explosion
	for(%i = 0; %i < %count; %i++)
	{
		%obj = $SpellTargetCache[%clientId, "obj", %i];
		
		// Validate object still exists (may have died/disconnected since cache was built)
		if(!isObject(%obj))
			continue;
		
		// Get CURRENT position (entity may have moved since cache was built)
		%objPos = GameBase::getPosition(%obj);
		
		// Calculate distance from THIS explosion's position
		%dist = Vector::getDistance(%explosionPos, %objPos);
		
		// Only damage if within this explosion's radius
		if(%dist <= %spellRadius)
		{
			if($SPELL_DEBUG) %hitsThisExplosion++;
			%newDamage = SpellCalcRadiusDamage(%dist, %spellRadius, $Spell::damageValue[%index], 5, 100);
			SpellDamage_Buffered(%clientId, %obj, %newDamage, %index);
		}
	}
	
	if($SPELL_DEBUG && %hitsThisExplosion > 0)
		echo("[SPELL DEBUG] Explosion Hit " @ %hitsThisExplosion @ " targets.");
}

//============================================================================
// DAMAGE BUFFERING SYSTEM
// Performance optimization: Accumulate damage for a target within a short
// window and apply it in a single onDamage call. This reduces the overhead
// of expensive stat calculations (DEF/MDEF/Skills) in Player::onDamage.
//============================================================================

function SpellDamage_Buffered(%clientId, %targetId, %damageValue, %index)
{
	// If buffer is empty for this target/caster, schedule a flush
	if($SpellDamageBuffer[%targetId, %clientId] == "" || $SpellDamageBuffer[%targetId, %clientId] == 0)
	{
		// Flush window coalesces multiple explosion hits into one Player::onDamage
		// call. Per-spell override ($Spell::damageBufferWindow) lets high-density AOE
		// spells use a wider window (see definitions near top of file); everything
		// else defaults to 0.2s, fast enough to feel responsive in combat.
		%window = $Spell::damageBufferWindow[%index];
		if(%window == "" || %window == -1)
			%window = 0.2;
		schedule("SpellDamageBuffer_Flush(" @ %clientId @ ", " @ (%targetId+0) @ ", " @ %index @ ");", %window);
	}

	$SpellDamageBuffer[%targetId, %clientId] += %damageValue;
}

function SpellDamageBuffer_Flush(%clientId, %targetId, %index)
{
	%totalDamage = $SpellDamageBuffer[%targetId, %clientId];
	if(%totalDamage == "" || %totalDamage == 0)
		return;
	
	// Target may have died/despawned since this flush was scheduled.
	if(!isObject(%targetId) || getObjectType(%targetId) != "Player")
	{
		$SpellDamageBuffer[%targetId, %clientId] = 0;
		return;
	}
	
	// Reset buffer BEFORE calling SpellDamage to avoid race conditions if 
	// another explosion hits during the flush processing.
	$SpellDamageBuffer[%targetId, %clientId] = 0;
	
	// Apply the accumulated damage
	SpellDamage(%clientId, %targetId, %totalDamage, %index);
}


// Optimized batch processing function for Tornado spell
function TornadoBatchExplosions(%clientId, %index, %startIdx, %silent)
{
	if(%clientId == 0 || %clientId == -1)
		return;
	
	%basePos = $TornadoBasePos[%clientId];
	%count = $TornadoDataCount[%clientId];
	
	if(%basePos == "" || %count == "")
		return;
	
	%xpos = getWord(%basePos, 0);
	%ypos = getWord(%basePos, 1);
	
	// Normalize silent value to 0 or 1 for safe use in schedule() and boolean checks
	if(%silent == "" || %silent == "false" || %silent == "0") %silent = 0;
	else %silent = 1;
	
	%batchSize = 5; // Process 5 explosions per batch
	// PERFORMANCE: If silent (Echo), we can process more damage-only hits per batch
	if(%silent) %batchSize = 20;

	// Get the start delay for this batch
	%firstData = $TornadoData[%clientId, %startIdx];
	%startDelay = 0;
	if(%firstData != "")
		%startDelay = getWord(%firstData, 3);
	
	%processedInBatch = 0;
	for(%i = %startIdx; %i < %count && %processedInBatch < %batchSize; %i++)
	{
		%data = $TornadoData[%clientId, %i];
		if(%data == "")
			continue;
		
		%bombType = getWord(%data, 0);
		%zOffset = getWord(%data, 1);
		%doDamageStr = getWord(%data, 2);
		%delay = getWord(%data, 3);
		
		// PERFORMANCE: If silent (Echo), skip visual-only entries entirely to save scheduling overhead
		if(%silent && %doDamageStr == "False")
			continue;
		
		%processedInBatch++;

		%doDamage = (%doDamageStr == "True");
		if(%doDamage)
			%doDamageNum = 1;
		else
			%doDamageNum = 0;
		
		%pos = %xpos @ " " @ %ypos @ " " @ %zOffset;
		%relativeDelay = %delay - %startDelay;
		if(%relativeDelay < 0) %relativeDelay = 0;
		
		if(%relativeDelay > 0)
		{
			schedule("ApocalypseCreateExplosion(" @ %clientId @ ", \"" @ %bombType @ "\", \"" @ %pos @ "\", " @ %doDamageNum @ ", " @ %index @ ", " @ %silent @ ");", %relativeDelay);
		}
		else
		{
			ApocalypseCreateExplosion(%clientId, %bombType, %pos, %doDamageNum, %index, %silent);
		}
		
		%lastDelay = %delay;
	}
	
	// Schedule next batch if there are more explosions
	%nextIdx = %i;
	if(%nextIdx < %count)
	{
		%nextData = $TornadoData[%clientId, %nextIdx];
		if(%nextData != "")
		{
			// Wait for explosions in THIS batch to complete, then start next batch
			%batchDuration = %lastDelay - %startDelay;
			if(%batchDuration < 0.1) %batchDuration = 0.1;
			
			schedule("TornadoBatchExplosions(" @ %clientId @ ", " @ %index @ ", " @ %nextIdx @ ", " @ %silent @ ");", %batchDuration);
		}
	}
	else
	{
		// Cleanup - ONLY if not silent
		if(!%silent)
		{
			// Cleanup
			for(%i = 0; %i < %count; %i++)
			$TornadoData[%clientId, %i] = "";
		$TornadoDataCount[%clientId] = "";
		$TornadoBasePos[%clientId] = "";
		
		// Tornado uses the shared cached-damage path. Clear these at the end
		// so later spells don't accidentally reuse stale cache/flags.
		$ApocalypseUseCachedDamage[%clientId] = "";
		SpellTargetCache_Clear(%clientId);
	}
	}
}

function SpellDamage(%clientId, %targetId, %damageValue, %index)
{
	$SpellTotalDamageCalls[%clientId]++;
	dbecho($dbechoMode, "SpellDamage(" @ %clientId @ ", " @ %targetId @ ", " @ %damageValue @ ", " @ %index @ ")");
	
	// Accept both Player objects and client IDs.
	%targetObj = %targetId;
	if(!isObject(%targetObj))
		return;
	
	if(getObjectType(%targetObj) != "Player")
	{
		%resolvedObj = Client::getOwnedObject(%targetId);
		if(%resolvedObj == -1 || %resolvedObj == "" || !isObject(%resolvedObj))
			return;
		if(getObjectType(%resolvedObj) != "Player")
			return;
		%targetObj = %resolvedObj;
	}

	// SEAL BATTLE FIX: Prevent SealMage bots from damaging themselves with their own spells
	// Check if caster is a SealMage and target is the same bot
	%casterDisplayName = Client::getName(%clientId);
	if(String::findSubStr(%casterDisplayName, "SealMage") == 0)
	{
		// Caster is a SealMage - check if target is the same bot (self-damage)
		%targetClientId = Player::getClient(%targetObj);
		if(%targetClientId == -1)
			%targetClientId = GetClientIdFromPlayerObject(%targetObj);
		
		if(%targetClientId == %clientId)
		{
			// SealMage trying to damage itself - skip
			return;
		}
	}

	// SEAL BATTLE: apply the seal bot spell damage multiplier.
	// Computed in SealBattle::SetupBot so hits land at the target fraction of
	// reference-player HP at every seal value - OffensiveCasting alone scales
	// linearly with the seal while player HP scales ~quadratically, so without
	// this the mage's relative damage decays ~1/R (correct at R20, negligible
	// at high seals).
	// GUARD: gated on isAiControlled + the SealBattleBot flag so a stale entry
	// on a reused clientId can never amplify a real player's spells
	// (SealBattle::ClearBotData clears both the flag and the multiplier).
	// GUARD 2: only amplify DAMAGE (positive values) - healing spells use negative
	// damageValues and amplifying those would fight $SealBotHealingReduction.
	%spellMult = $SealBattleSpellDmgMult[%clientId];
	if(%spellMult != "" && %spellMult > 1 && %damageValue > 0)
	{
		if(Player::isAiControlled(%clientId) && fetchData(%clientId, "SealBattleBot"))
			%damageValue = floor(%damageValue * %spellMult);
	}

	GameBase::virtual(%targetObj, "onDamage", $SpellDamageType, %damageValue, "0 0 0", "0 0 0", "0 0 0", "torso", "front_right", %clientId, $Spell::keyword[%index]);
}

function SpellRadiusDamage(%clientId, %pos, %index)
{
	dbecho($dbechoMode, "SpellRadiusDamage(" @ %clientId @ ", " @ %pos @ ", " @ %index @ ")");
	
	if(%pos == "" || %pos == -1)
		return;
	
	if($Spell::radius[%index] == "" || $Spell::radius[%index] <= 0)
		return;

	%b = $Spell::radius[%index] * 2;
	%set = newObject("set", SimSet);
	%n = containerBoxFillSet(%set, $SimPlayerObjectType, %pos, %b, %b, %b, 0);

	Group::iterateRecursive(%set, DoSpellDamage, %clientId, %pos, %index);
	deleteObject(%set);
}
function DoSpellDamage(%object, %clientId, %pos, %index)
{
	dbecho($dbechoMode, "DoSpellDamage(" @ %object @ ", " @ %clientId @ ", " @ %pos @ ", " @ %index @ ")");

	// %object is always a Player object (either a player client or AI bot)
	// GameBase::virtual() requires the Player object directly, not a client ID
	// Both GameBase::getPosition() and SpellDamage() accept Player objects
	
	// Validate that the target object exists and is initialized
	if(!isObject(%object))
	{
		// Target not initialized yet - skip this damage call
		return;
	}

	%percMin = 5;
	%percMax = 100;

	%dist = Vector::getDistance(%pos, GameBase::getPosition(%object));

	if(%dist <= $Spell::radius[%index])
	{
		// Town bots can never take spell damage (always nulled in Player::onDamage),
		// so skip them before entering the damage pipeline.
		%targetClientId = Player::getClient(%object);
		if(%targetClientId == -1)
			%targetClientId = GetClientIdFromPlayerObject(%object);
		if(%targetClientId != -1 && isTownBot(%targetClientId))
			return;

		%newDamage = SpellCalcRadiusDamage(%dist, $Spell::radius[%index], $Spell::damageValue[%index], %percMin, %percMax);
		// SpellDamage_Buffered() accumulates damage for targets within short windows
		// to reduce the frequency of expensive onDamage calls.
		SpellDamage_Buffered(%clientId, %object, %newDamage, %index);
	}
}

function SpellCalcRadiusDamage(%dist, %radius, %dmg, %percMin, %percMax)
{
	dbecho($dbechoMode, "SpellCalcRadiusDamage(" @ %dist @ ", " @ %radius @ ", " @ %dmg @ ", " @ %percMin @ ", " @ %percMax @ ")");
	
	if(%radius == "" || %radius <= 0)
		return 0;
	if(%dmg == "" || %dmg == 0)
		return 0;

	// Cache division result
	%dmgPerRadius = %dmg / %radius;
	%newdmg = %dmg - (%dist * %dmgPerRadius);

	%p = (%newdmg * 100) / %dmg;

	if(%p < %percMin)
		%p = %percMin;
	else if(%p > %percMax)
		%p = %percMax;

	return (%p * %dmg) / 100;
}

function GetBestSpell(%clientId, %type, %semiRandomSpell)
{
	dbecho($dbechoMode, "GetBestSpell(" @ %clientId @ ", " @ %type @ ", " @ %semiRandomSpell @ ")");

	%wdelay = 10;	//weights
	%wrecov = 0.5;

	%bestSpell = -1;
	%backupSpell = "";
	%highest = 0.1;
	%mana = fetchData(%clientId, "MANA");

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		%keyword = $Spell::keyword[%i];
		%canUse = SkillCanUse(%clientId, %keyword);
		
		if(%canUse)
		{
			%manaCost = $Spell::manaCost[%i];
			%hasMana = (%mana >= %manaCost);
			
			if(%hasMana)
			{
				%d = ($Spell::delay[%i] / %wdelay) + ($Spell::recoveryTime[%i] / %wrecov);
				%v = ((100 / %d) * $Spell::refVal[%i]) * %type;

				if(%semiRandomSpell)
				{
					%r = getRandom() * 100;
					%rr = getRandom() * 100;
				}
				else
				{
					%r = 1;
					%rr = 0;
				}

				if(%v > %highest)
				{
					if(%r > %rr)
					{
						%bestSpell = %i;
						%highest = %v;
					}
					else
					{
						%backupSpell = %i;
					}
				}
			}
		}
	}
	
	if(%bestSpell == -1 && %backupSpell != "")
		%bestSpell = %backupSpell;

	return %bestSpell;
}

function CalcSpellMiss(%clientId, %targetId, %index)
{
	dbecho($dbechoMode, "CalcSpellMiss(" @ %clientId @ ", " @ %targetId @ ", " @ %index @ ")");

	%range = $Spell::LOSrange[%index];
	%dist = Vector::getDistance(GameBase::getPosition(%clientId), GameBase::getPosition(%targetId));

	%m = floor((getRandom() * %range)) + (%range / 6);

	//echo(%dist @ " / " @ %range @ " : --> " @ %m);
	if(%m > %dist)
		return False;
	else
		return True;
}

function sendDoneRecovMsg(%clientId)
{
	//this function is here just to make the schedule command where this is called easier to read
	Client::sendMessage(%clientId, $MsgBeige, "You are ready to cast.~spellc");
}

function DoBoxFunction(%object, %clientId, %index, %extra)
{
	dbecho($dbechoMode, "DoBoxFunction(" @ %object @ ", " @ %clientId @ ", " @ %index @ ", " @ %extra @ ")");

	// CRITICAL: Use GetClientIdFromPlayerObject() for proper client ID lookup
	// Player::getClient() returns -1 for AI bots, so we need reverse lookup
	%id = GetClientIdFromPlayerObject(%object);

	if(%index == 23)
	{
		if(GameBase::getTeam(%clientId) == GameBase::getTeam(%id))
		{
			Client::sendMessage(%clientId, $MsgBeige, "Mass Healing " @ Client::getName(%id));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, "You are being Mass Healed by " @ Client::getName(%clientId));

			%r = $Spell::damageValue[%index] / $TribesDamageToNumericDamage;
			refreshHP(%id, %r);

			%castPos = GameBase::getPosition(%id);

			CreateAndDetBomb(%clientId, "Bomb10", %castPos, False, %index);
			playSound($Spell::endSound[%index], %castPos);
		}
	}
if(%index == 58 || %index == 59)
	{
if(%id != -1 && %id != %clientId)
{
//Air Blast	
//Air Warp	
		
			// Check if target is a town bot (town bots should not be affected by shoving spells)
			// CRITICAL: Use isTownBot() helper function which properly distinguishes town bots from enemy bots
			// Town bots have BotInfoAiName but NO SpawnBotInfo
			// Enemy bots have BOTH BotInfoAiName AND SpawnBotInfo
			%isTownBot = isTownBot(%id);
			
			if(!%isTownBot)
			{
				Client::sendMessage(%clientId, $MsgBeige, "Air Blasting " @ Client::getName(%id));
				if(%clientId != %id)
					Client::sendMessage(%id, $MsgBeige, "You are being blasted into the air by " @ Client::getName(%clientId));
				%b = GameBase::getRotation(%Id)       ;
				%c1 = Cap(150 + ($PlayerSkill[%clientId, $SkillNeutralCasting] / 2), 0, 3500);
				%c2 = %c1 / 3;
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
					Player::applyImpulse(%object, %mom);
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
				
				%castPos = GameBase::getPosition(%id);
			}
		
	}
}
	if(%index == 24)
	{
		if(GameBase::getTeam(%clientId) == GameBase::getTeam(%id))
		{
			Client::sendMessage(%clientId, $MsgBeige, "Mass Fully Healing " @ Client::getName(%id));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, "You are being Mass Fully Healed by " @ Client::getName(%clientId));

			setHP(%id, fetchData(%id, "MaxHP"));

			%castPos = GameBase::getPosition(%id);

			CreateAndDetBomb(%clientId, "Bomb10", %castPos, False, %index);
			playSound($Spell::endSound[%index], %castPos);
		}
	}
	if(%index == 31)
	{
		if(GameBase::getTeam(%clientId) == GameBase::getTeam(%id))
		{
			Client::sendMessage(%clientId, $MsgBeige, "Shielding " @ Client::getName(%id));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is casting " @ $Spell::name[%index] @ " on you.~spellc");

			UpdateBonusState(%id, $Spell::damageValue[%index], $Spell::ticks[%index]);

			%castPos = GameBase::getPosition(%id);

			CreateAndDetBomb(%clientId, "Bomb10", %castPos, False, %index);
			playSound($Spell::endSound[%index], %castPos);
		}
	}
	if(%index == 33)
	{
		if(IsInCommaList(fetchData(%clientId, "grouplist"), Client::getName(%id)) && IsInCommaList(fetchData(%id, "grouplist"), Client::getName(%clientId)) || %clientId == %id)
		{
			Client::sendMessage(%clientId, $MsgBeige, "Transporting " @ Client::getName(%id) @ " to " @ Zone::getDesc(%extra));
			if(%clientId != %id)
				Client::sendMessage(%id, $MsgBeige, Client::getName(%clientId) @ " is transporting you to " @ Zone::getDesc(%extra));

			//teleport

			%system = Object::getName(%extra);
			%type = GetWord(%system, 0);
			%desc = String::getSubStr(%system, String::len(%type)+1, 9999);

			%castPos = TeleportToMarker(%id, "Zones\\" @ %system @ "\\DropPoints", False, True);
			CheckAndBootFromArena(%id);
			NullItemList(%clientId, Lore, $MsgRed, "You lost all %1s you were carrying when you teleported.");

			if(!fetchData(%id, "invisible"))
				GameBase::startFadeIn(%id);

			Player::setDamageFlash(%id, 0.7);

			%extraDelay = 0.22;
			schedule("playSound(" @ $Spell::endSound[%index] @ ", \"" @ %castPos @ "\");", %extraDelay);
		}
	}
}

function SpellCanCast(%clientId, %keyword)
{
	dbecho($dbechoMode, "SpellCanCast(" @ %clientId @ ", " @ %keyword @ ")");

	// Block spellcasting while dual wielding
	if(DualWield::IsEnabled(%clientId) && !$DualWield::AllowSpellcasting)
	{
		Client::sendMessage(%clientId, $MsgRed, "Cannot cast spells while dual wielding!");
		return False;
	}

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		if(String::ICompare($Spell::keyword[%i], %keyword) == 0)
		{
			if(SkillCanUse(%clientId, $Spell::keyword[%i]))
			{
				if(fetchData(%clientId, "MaxMANA") >= $Spell::manaCost[%i])
					return True;
			}
		}
	}
	return False;
}
function SpellCanCastNow(%clientId, %keyword)
{
	dbecho($dbechoMode, "SpellCanCastNow(" @ %clientId @ ", " @ %keyword @ ")");

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		if(String::ICompare($Spell::keyword[%i], %keyword) == 0)
		{
			if(SkillCanUse(%clientId, $Spell::keyword[%i]))
			{
				if(fetchData(%clientId, "MANA") >= $Spell::manaCost[%i])
					return True;
			}
		}
	}
	return False;
}

// Override command status to disable default command menu (Change Teams, etc)
// This allows TAB to show RPG menu instead for all players (including admins)
function Client::setCommandStatus(%this, %status)
{
	// All players should use RPG menu, not base Tribes command menu
	// REMOVED: remoteEval(%this, "setCommandStatus", 0) - no client has
	// remoteSetCommandStatus, it only printed "Unknown command" errors
	// in the client console
	// Open RPG options menu instead
	Game::menuRequest(%this);
}

// Note: remoteToggleCommandMode is defined in remote.cs
// This stub was removed so remote.cs version takes precedence
