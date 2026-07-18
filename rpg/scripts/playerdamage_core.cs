//============================================================================
// playerdamage_core.cs — split from playerdamage.cs (original lines 2925-5349)
// Extracted 2026-07-17 at commit 4aa0a3d. Mechanical text move, no behavior change.
// Player::onDamage (kept whole; ALL townbot-immunity guards live inside it) + mythic cluster + remoteKill.
// exec'd by playerdamage.cs (the shell) — do NOT add to Server.cs.
// ==== BEGIN ORIGINAL PAYLOAD ====
function Player::onDamage(%this,%type,%value,%pos,%vec,%mom,%vertPos,%rweapon,%object,%weapon,%preCalcMiss)
{
	dbecho($dbechoMode, "Player::onDamage(" @ %this @ ", " @ %type @ ", " @ %value @ ", " @ %pos @ ", " @ %vec @ ", " @ %mom @ ", " @ %vertPos @ ", " @ %rweapon @ ", " @ %object @ ", " @ %weapon @ ", " @ %preCalcMiss @ ")");
	if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Player::onDamage ENTRY: Victim=" @ %this @ ", ShooterObj=" @ %object @ ", Damage=" @ %value @ ", Type=" @ %type @ ", PreCalcMiss=" @ %preCalcMiss);
	// $MeleeDebug (weapons.cs): log bot-vs-player damage arrivals
	if($MeleeDebug && Player::isAiControlled(%object) && !Player::isAiControlled(GetClientIdFromPlayerObject(%this)))
		echo("[MELEE DEBUG] onDamage: bot " @ GetClientOrBotName(%object) @ " -> victim " @ %this @ " value=" @ %value @ " weapon=" @ %weapon);

	%skilltype = $SkillType[%weapon];

	if(%object != -1 && %type != $NullDamageType && !Player::IsDead(%this))
	{	
		// Use helper function to get client ID from Player object
		%damagedClient = GetClientIdFromPlayerObject(%this);
		
		// Fallback: if helper function returns -1, use Player object as client ID (for compatibility)
		if(%damagedClient == -1 || %damagedClient == "")
		{
			%damagedClient = %this;
		}
		
		// PHASE 4 FIX: Validate client ID belongs to correct entity before processing damage
		if(%damagedClient != -1 && %damagedClient != "" && isObject(%this))
		{
			// Reverse verification - ensure the client ID actually owns this Player object
			if(%damagedClient != %this)
			{
				// Only verify if we got a different client ID (not using %this as fallback)
				%verifyPlayerObj = Client::getOwnedObject(%damagedClient);
				if(%verifyPlayerObj != %this && %verifyPlayerObj != -1 && %verifyPlayerObj != "")
				{
					// Client ID doesn't own this Player object - this is a collision!
					echo("ERROR: Player::onDamage - Client ID " @ %damagedClient @ " does not own Player object " @ %this @ ". Player object belongs to client ID with Player object " @ %verifyPlayerObj @ ". Rejecting damage to prevent routing to wrong entity.");
					return; // Reject damage to prevent routing to wrong entity
				}
				
				// review #64: removed the "entity type validation" that was here. It computed
				// %isBot (via isRPGAI -> isFile("temp\\<name>.cs")) and %isPlayer (a SECOND
				// isFile on the same path), then checked `if(%isPlayer && %isBot)` - but
				// %isPlayer is only ever set true inside `if(!%isBot)`, so that branch is
				// UNREACHABLE. It cost TWO disk stats per hit on every real player in the
				// hottest per-hit path, all to feed a dead check. The collision check above
				// (does %damagedClient actually own %this) is the real validation and stays.
			}
		}
		
		// Early-initialization guard: if AI bot not fully initialized or spawn-invuln is on, ignore all damage
		if(Player::isAiControlled(%damagedClient))
		{
			%hasLoaded = fetchData(%damagedClient, "HasLoadedAndSpawned");
			%spawnInvuln = fetchData(%damagedClient, "SpawnInvuln");
			// Additional name-based guard for bots that haven't registered yet
			%nameGuard = "";
			%damagedName = Client::getName(%damagedClient);
			if(%damagedName != "" && %damagedName != -1)
			{
				%nameGuardTime = $SpawnInvulnByName[%damagedName];
				if(%nameGuardTime != "" && %nameGuardTime != -1)
				{
					if((getSimTime() - %nameGuardTime) < 15)
						%nameGuard = "true";
				}
			}

			if(!%hasLoaded || %spawnInvuln || %nameGuard == "true")
			{
				// Hard ignore: exit immediately to prevent any processing, messages, or side effects
				if($DamageDebugEnabled) echo("[DAMAGE DEBUG] AI Early Exit: Invuln/Loading/NameGuard. Loaded=" @ %hasLoaded @ " Invuln=" @ %spawnInvuln @ " NameGuard=" @ %nameGuard);
				return;
			}
		}
		else
		{
			// HUMAN PLAYER spawn protection: 5 seconds of invincibility after connecting
			// Prevents damage from bots attacking the previous clientId occupant
			%spawnInvuln = fetchData(%damagedClient, "SpawnInvuln");
			if(%spawnInvuln)
			{
				if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Player Early Exit: Spawn protection active");
				return;
			}
			// Also check name-based guard
			%damagedName = Client::getName(%damagedClient);
			if(%damagedName != "" && %damagedName != -1)
			{
				%nameGuardTime = $SpawnInvulnByName[%damagedName];
				if(%nameGuardTime != "" && %nameGuardTime != -1)
				{
					if((getSimTime() - %nameGuardTime) < 5)
					{
						if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Player Early Exit: Name guard active");
						return;
					}
				}
			}
		}

		// Safe zone guard: if damagedClient is in a safe zone and not in an arena, ignore damage
		%safeZone = fetchData(%damagedClient, "safeZone");
		%arena = fetchData(%damagedClient, "arena");
		%safeZoneGuard = fetchData(%damagedClient, "safeZoneGuard"); // Additional guard for safe zone entry/exit
		if((%safeZone && !%arena) || %safeZoneGuard)
		{
			if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Exit: Safe Zone. SafeZone=" @ %safeZone @ " Arena=" @ %arena @ " SafeGuard=" @ %safeZoneGuard);
			return;
		}
		
		// %object is the shooter's client ID (for players) or Player object (for AI shooters)
		// For spell damage, %object might be a client ID, so try to get the Player object
		%shooterClient = %object;
		if(isObject(%object))
		{
			%objectType = getObjectType(%object);
			if(%objectType == "Player")
			{
				// %object is a Player object - use helper function to get client ID
				%shooterClientId = GetClientIdFromPlayerObject(%object);
				if(%shooterClientId != -1 && %shooterClientId != "")
				{
					%shooterClient = %shooterClientId; // Found client ID (works for both players and bots)
				}
				else
				{
					// Fallback: use Player object as client ID (for compatibility with old code)
					// This should rarely happen, but keeps the code working if client ID lookup fails
					%shooterClient = %object;
				}
			}
		}
		if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Shooter Resolved: " @ %shooterClient @ " (Original: " @ %object @ ")");

		// Town NPCs should not die from player-controlled vehicle body impacts.
		// Scout guns still use MissileDamageType and continue through the normal vehicle-combat path.
		if(IsTownBot(%damagedClient) && (%type == $ImpactDamageType || %type == $CrushDamageType) && isObject(%object))
		{
			%impactObjectType = getObjectType(%object);
			if(%impactObjectType == "Vehicle" || %impactObjectType == "Flier")
			{
				if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Blocked vehicle impact damage to town bot " @ %damagedClient @ " from object " @ %object);
				return;
			}
		}

		// PHASE 5 FIX: Check if the shooter's client ID was recently freed
		// This blocks "ghost damage" from projectiles/spells of dead bots whose IDs were immediately reused
		// CRITICAL: Window must be 30 seconds to match flag clearing duration and cover long-cast spells
		%shooterRecentlyFreed = $ClientIdRecentlyFreed[%shooterClient];
		if(%shooterRecentlyFreed != "" && %shooterRecentlyFreed != "0" && %shooterRecentlyFreed != -1)
		{
			// Safety window of 30 seconds - matches flag clearing and covers longest spell cast times
			if((getSimTime() - %shooterRecentlyFreed) < 30)
			{
				if($DamageDebugEnabled) echo("[DAMAGE FIX] BLOCKED ghost damage from recently freed Client ID " @ %shooterClient @ ". Timestamp: " @ %shooterRecentlyFreed);
				return;
			}
		}

		
		%damagedClientPos = GameBase::getPosition(%damagedClient);
		%shooterClientPos = GameBase::getPosition(%shooterClient);

		// Get currently equipped armor (VOID CONVERSION 2026-07-14: via GetWornArmor -
		// belt EquippedBeltArmor first, engine WornEngineArmor fallback. The old
		// fetchData("Armor") read now returns the belt carried-armor LIST, not a name.)
		%damagedCurrentArmor = GetWornArmor(%damagedClient);

		//==============
		//PROCESS STATS
		//==============
		%isMiss = false;
		%Pierce = false;
		%Bash = false;
		%Cleave = false;
		%sameTeamNull = false;

		//------------- CREATE DAMAGE VALUE -------------
		if(%type == $SpellDamageType)
		{
			//For the case of SPELLS, the initial damage has already been determined before calling this function

			%dmg = %value;
			%skillValue = $PlayerSkill[%shooterClient, %skilltype];
			if(%skillValue == "" || %skillValue == -1)
				%skillValue = 1000; // Default skill value if not set (for NPCs or edge cases)
			
			%value = round((%dmg * %skillValue) / 1000);

			%ab = (getRandom() * (fetchData(%damagedClient, "MDEF") / 10)) + 1;
			%value = Cap(%value - %ab, 0, "inf");

			%value = (%value / $TribesDamageToNumericDamage);
			
		}
		else if(%type != $LandingDamageType)
		{
			// Check if shooter is a turret - turrets use projectile base damage, not physical damage formula
			%isTurretDamage = false;
			if(isObject(%object))
			{
				%objectClassName = %object.className;
				if(%objectClassName == "Turret")
					%isTurretDamage = true;
			}
			
			// Also detect turret projectiles by checking if it's MissileDamageType from a source without skills/ATK
			// Turrets use MissileDamageType projectiles but don't have player skills or ATK
			if(!%isTurretDamage && %type == $MissileDamageType)
			{
				// Check if shooter doesn't have ATK or skills (indicating it's a turret, not a player)
				%shooterATK = fetchData(%shooterClient, "ATK");
				if((%shooterATK == "" || %shooterATK == -1 || %shooterATK == 0) && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
				{
					// Check if it's not a vehicle projectile (vehicles are handled separately)
					%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
					%isVehicle = false;
					if(%shooterPlayerObj != -1)
					{
						%shooterType = getObjectType(%shooterPlayerObj);
						// Check if shooter object IS a vehicle/flier
						if(%shooterType == "Vehicle" || %shooterType == "Flier")
							%isVehicle = true;
						// Also check if player is IN a vehicle (for when player fires from vehicle)
						if(!%isVehicle && %shooterPlayerObj.vehicle != "")
							%isVehicle = true;
					}
					// If not a vehicle and no ATK/skills, it's likely a turret
					if(!%isVehicle)
						%isTurretDamage = true;
				}
			}
			
			// For turret damage, use the projectile's base damage value directly
			// Turrets don't have ATK, weapons, or skills, so we can't use the physical damage formula
			if(%isTurretDamage)
			{
				// Use the projectile's damageValue (from baseProjData.cs)
				// FusionBolt, GuardianBolt, etc. have damageValue = 1 (in Tribes damage units)
				// Convert from Tribes damage to numeric damage
				// Turret projectiles typically have damageValue = 1, which needs to be scaled appropriately
				%baseTurretDamage = 100; // Base turret damage in Tribes damage units (equivalent to 100 damage)
				
				// Scale turret damage with player level to keep threat consistent across all levels
				// Higher level players have more DEF and HP, so we scale base damage to compensate
				// Formula: baseDamage * (1 + (level / 100))
				// This means: Level 0 = 1x, Level 100 = 2x, Level 200 = 3x, Level 500 = 6x
				%playerLevel = fetchData(%damagedClient, "LVL");
				if(%playerLevel == "" || %playerLevel == -1)
					%playerLevel = 1;
				%levelMultiplier = 1 + (%playerLevel / 100);
				%scaledTurretDamage = %baseTurretDamage * %levelMultiplier;
				%value = %scaledTurretDamage;
				
				// Apply DEF reduction (turrets should still respect DEF)
				// Higher level players have more DEF, which naturally reduces the scaled damage
				%ab = (getRandom() * (fetchData(%damagedClient, "DEF") / 10)) + 1;
				%value = Cap(%value - %ab, 1, "inf");
				
				// Add random variation (±15%)
				%a = (%value * 0.15);
				%r = round((getRandom() * (%a*2)) - %a);
				%value += %r;
				if(%value < 1)
					%value = 1;
				
				// Convert from Tribes damage to numeric damage
				%value = (%value / $TribesDamageToNumericDamage);
			}
			else
			{
				%multi = 1;

				if(fetchData(%damagedClient, "invisible"))
				{
					UnHide(%damagedClient);
				}			

				//Bash
			if(fetchData(%shooterClient, "NextHitBash"))
			{
				if(%skilltype == $SkillBludgeoning)
				{
					// BALANCED BASH: Use diminishing returns and cap to prevent OP scaling with high-level weapons
					%bashSkill = $PlayerSkill[%shooterClient, $SkillBashing];
					if(%bashSkill == "" || %bashSkill == -1)
						%bashSkill = 0;
					
					// Cap bashing skill at 1000 for calculation
					if(%bashSkill > 1000)
						%bashSkill = 1000;
					
					// Diminishing returns formula: Use piecewise linear scaling
					// First 500 skill: linear scaling (skill / 500)
					// After 500 skill: diminishing returns (500/500 + (skill-500)/1000)
					// This means: 100 skill = +0.20x, 500 skill = +1.00x, 1000 skill = +1.50x
					// Old formula: 1000 skill = +2.13x (too OP)
					if(%bashSkill <= 500)
					{
						%bashMultiplier = %bashSkill / 500;
					}
					else
					{
						%bashMultiplier = 1.0 + ((%bashSkill - 500) / 1000);
					}
					
					// Cap maximum bash bonus at +1.5x (prevents extreme scaling with high-tier weapons)
					if(%bashMultiplier > 1.5)
						%bashMultiplier = 1.5;
					
					%multi += %bashMultiplier;
					%b = GameBase::getRotation(%shooterClient);
					// MASSIVELY SCALED DOWN: Use square root scaling and cap to prevent enemies from disappearing
					// Old formula: SkillBashing / 15 (could reach 200+ at high levels)
					// New formula: sqrt(SkillBashing) / 2, capped at 10
					%bashSkillForShove = $PlayerSkill[%shooterClient, $SkillBashing];
					if(%bashSkillForShove == "" || %bashSkillForShove == -1)
						%bashSkillForShove = 0;
					%c = sqrt(%bashSkillForShove) / 2;
					if(%c > 10)
						%c = 10;  // Cap at 10 to prevent extreme shove forces
					%Bash = true;
				}

				%delay = Cap(300 - ($PlayerSkill[%shooterClient, $SkillBashing] / 30), 120, 300);
				schedule("storeData(" @ %shooterClient @ ", \"blockBash\", \"\");", %delay);
				storeData(%shooterClient, "NextHitBash", "");
			}
	
			//Pierce
			if(fetchData(%shooterClient, "NextHitPierce"))
			{
				if(%skilltype == $SkillPiercing)
				{
					%multi += $PlayerSkill[%shooterClient, $SkillPiercing] / 470;
					%Pierce = true;
				}
				
				%delay = Cap(360 - ($PlayerSkill[%shooterClient, $SkillPiercing] / 100), 30, 360);
				schedule("storeData(" @ %shooterClient @ ", \"blockPierce\", \"\");", %delay);
				storeData(%shooterClient, "NextHitPierce", "");				
			}
			
			//Cleave
			if(fetchData(%shooterClient, "NextHitCleave"))
			{
				if(%skilltype == $SkillSlashing)
				{
					%multi += $PlayerSkill[%shooterClient, $SkillSlashing] / 470;
					%Cleave = true;
				}

				%delay = Cap(360 - ($PlayerSkill[%shooterClient, $SkillSlashing] / 100), 30, 360);
				schedule("storeData(" @ %shooterClient @ ", \"blockCleave\", \"\");", %delay);
				storeData(%shooterClient, "NextHitCleave", "");
			}			

			//============================================================================
			// SPECIAL WEAPON EFFECTS
			//============================================================================
			
			// Track hit counter for weapon effects
			%weaponEffect = $WeaponEffect[%weapon];
			if(%weaponEffect != "")
			{
				// Increment hit counter for this weapon on this target
				// Use global array - TorqueScript doesn't support dynamic property access
				%hitCount = $WeaponHitCount[%shooterClient, %weapon, %damagedClient];
				if(%hitCount == "" || %hitCount == -1)
					%hitCount = 0;
				%hitCount++;
				$WeaponHitCount[%shooterClient, %weapon, %damagedClient] = %hitCount;

				
				// FINAL VERDICT - 1% instant kill on non-boss targets
				if(%weaponEffect == "INSTANT_KILL")
				{
					%chance = $WeaponEffectChance[%weapon];
					if(%chance == "" || %chance == -1) %chance = 1;
					
					%roll = floor(getRandom() * 100) + 1;  // 1-100
					if(%roll <= %chance)
					{
						// Check if target is protected (town bots, same team, party members, bosses)
						%isProtected = false;
						%targetName = Client::getName(%damagedClient);
						%spawnBotInfo = fetchData(%damagedClient, "SpawnBotInfo");
						
						// Protection 1: Town bots are immune
						if(isTownBot(%damagedClient))
						{
							%isProtected = true;
						}
						
						// Protection 2: Same team players (unless in duel or on hit list)
						if(!%isProtected)
						{
							%shooterTeam = GameBase::getTeam(%shooterClient);
							%targetTeam = GameBase::getTeam(%damagedClient);
							
							// If same team and target is NOT an enemy bot
							if(%shooterTeam == %targetTeam && !IsEnemyBot(%damagedClient))
							{
								// Check if in duel or on hit list
								%inDuel = (fetchData(%shooterClient, "DuelTarget") == %damagedClient);
								%onHitList = (String::findSubStr(fetchData(%shooterClient, "Hitlist"), Client::getName(%damagedClient)) != -1);
								
								if(!%inDuel && !%onHitList)
								{
									%isProtected = true;
								}
							}
						}
						
						// Protection 3: Party members are immune
						if(!%isProtected)
						{
							%shooterParty = fetchData(%shooterClient, "PARTY");
							%targetParty = fetchData(%damagedClient, "PARTY");
							if(%shooterParty != "" && %shooterParty != -1 && %shooterParty == %targetParty)
							{
								%isProtected = true;
							}
						}
						
						// Protection 4: Bosses are immune
						if(!%isProtected)
						{
							if(String::findSubStr(%targetName, "Boss") != -1 || 
							   String::findSubStr(%targetName, "King") != -1 ||
							   String::findSubStr(%targetName, "Queen") != -1 ||
							   String::findSubStr(%spawnBotInfo, "Boss") != -1)
							{
								%isProtected = true;
							}
						}
						
						// Protection 5: Seal Battle bots are immune
						if(!%isProtected)
						{
							%isSealBot = fetchData(%damagedClient, "SealBattleBot");
							if(%isSealBot == "true" || %isSealBot == "True" || %isSealBot == "1")
							{
								%isProtected = true;
							}
							// Also check for Seal in display name
							if(String::findSubStr(%targetName, "SealFighter") == 0 || 
							   String::findSubStr(%targetName, "SealMage") == 0 ||
							   String::findSubStr(%targetName, "SealGuardian") == 0)
							{
								%isProtected = true;
							}
						}
						
						// Protection 6: Colloseum arena bots are immune
						if(!%isProtected)
						{
							// Check if target is in Colloseum zone
							%targetZoneId = fetchData(%damagedClient, "zone");
							if(%targetZoneId == "" || %targetZoneId == -1 || %targetZoneId == "0")
							{
								%targetObj = Client::getOwnedObject(%damagedClient);
								if(%targetObj != -1 && %targetObj != "")
									%targetZoneId = ObjectInWhichZone(%targetObj);
							}

							if(%targetZoneId != "" && %targetZoneId != -1 && %targetZoneId != "0")
							{
								%targetZoneDesc = Zone::getDesc(%targetZoneId);
								if(String::ICompare(%targetZoneDesc, "Colloseum") == 0)
								{
									%isProtected = true;
								}
							}
						}
						
						if(!%isProtected)
						{
							// INSTANT KILL! Call Player::kill directly to bypass LCK
							%targetName = Client::getName(%damagedClient);
							
							// Send special message BEFORE killing
							Client::sendMessage(%shooterClient, 0, "~wgame/explode3.wav");
							Client::sendMessage(%shooterClient, $MsgRed, "FINAL VERDICT! " @ %targetName @ " has been judged!");
							Client::sendMessage(%damagedClient, $MsgRed, "FINAL VERDICT! You have been instantly slain by Final Verdict!");
							
							// Kill the target directly - bypasses LCK and all other checks
							Player::kill(%damagedClient);
							
							// Return early - don't process rest of damage logic
							return;
						}
					}
				}
				
				// STORM CALLER - Lightning strike every N hits
				if(%weaponEffect == "LIGHTNING_STRIKE")
				{
					%frequency = $WeaponEffectFrequency[%weapon];
					if(%frequency == "" || %frequency == -1) %frequency = 5;
					
					if((%hitCount % %frequency) == 0)
					{
						// Trigger lightning strike!
						%targetPos = GameBase::getPosition(Client::getOwnedObject(%damagedClient));
						if(%targetPos != "" && %targetPos != -1)
						{
							// Create lightning visual effect
							CreateAndDetBomb(%shooterClient, "Bomb21", %targetPos, False, 0);
							playSound(shockExplosion, %targetPos);
							
							// Calculate bonus magic damage (scales with Piercing skill)
							%lightningDmg = 500;  // Base lightning damage
							%piercingSkill = $PlayerSkill[%shooterClient, $SkillPiercing];
							if(%piercingSkill == "" || %piercingSkill == -1) %piercingSkill = 1000;
							%lightningDmg = round((%lightningDmg * %piercingSkill) / 1000);
							
							// Apply MDEF reduction for lightning damage
							%targetMDEF = fetchData(%damagedClient, "MDEF");
							if(%targetMDEF == "" || %targetMDEF == -1) %targetMDEF = 0;
							%mdefReduction = (getRandom() * (%targetMDEF / 10)) + 1;
							%lightningDmg = floor(Cap(%lightningDmg - %mdefReduction, 1, "inf"));

							// Defer the bonus: %value gets fully recomputed below, so adding it
							// here would be wiped out. Applied after the damage formula instead.
							%lightningBonus = %lightningDmg;

							Client::sendMessage(%shooterClient, $MsgYellow, "LIGHTNING STRIKE! +" @ %lightningDmg @ " bonus damage!");
						}
					}
				}
				
				// WORLD SPLITTER - Alternating physical/magic damage
				if(%weaponEffect == "ALTERNATING_DAMAGE")
				{
					// Odd hits = physical (DEF), Even hits = magic (MDEF)
					if((%hitCount % 2) == 0)
					{
						// Even hit - use MDEF instead of DEF
						%useMDEF = true;
					}
				}
			}


			if(%rweapon != "")
				%rweapondamage = GetRoll(GetWord(GetAccessoryVar(%rweapon, $SpecialVar), 1));
			else
				%rweapondamage = 0;
			%weapondamage = GetRoll(GetWord(GetAccessoryVar(%weapon, $SpecialVar), 1));

			%playerattack = fetchData(%shooterClient, "ATK") - %weapondamage;
			%skillValue = $PlayerSkill[%shooterClient, %skilltype];
			if(%skillValue == "" || %skillValue == -1)
				%skillValue = 1000; // Default skill value if not set (for NPCs or edge cases)
			
			// NOTE: Final Verdict instant kills return early from the effect block above,
			// so no skip-flag is needed here (the old %instantKill guards were dead code -
			// the variable was never assigned anywhere).
			%value = round((( (%weapondamage + (%playerattack + %rweapondamage)) / 1000) * %skillValue) * %multi);

			%ab = (getRandom() * (fetchData(%damagedClient, "DEF") / 10)) + 1;

			// World Splitter: Even hits use MDEF instead of DEF
			if(%useMDEF)
			{
				%ab = (getRandom() * (fetchData(%damagedClient, "MDEF") / 10)) + 1;
			}

			%value = Cap(%value - %ab, 1, "inf");

			%a = (%value * 0.15);
			%r = round((getRandom() * (%a*2)) - %a);
			%value += %r;
			if(%value < 1)
				%value = 1;

			// Storm Caller lightning bonus (deferred from the effects block above so the
			// damage formula recompute doesn't wipe it out)
			if(%lightningBonus > 0)
				%value += %lightningBonus;


			if(%Bash)	//i'm doing this condition here because %mom is dependant on %value
			{
				// MASSIVELY SCALED DOWN: Reduce shove force by using damage scaling and capping
				// Old formula: (SkillBashing / 15) / 15 * %value = SkillBashing / 225 * %value
				// At 2000 skill, 2000 damage: 2000/225 * 2000 = 17,777 (way too high!)
				// New formula: %c * sqrt(%value) / 10, with additional cap
				// %c is already capped at 10, so max forward force = 10 * sqrt(damage) / 10 = sqrt(damage)
				// At 2000 damage: sqrt(2000) = 44.7 (much more reasonable)
				%c1 = %c * sqrt(%value) / 10;
				// Cap forward force to prevent extreme values (max 50 units)
				if(%c1 > 50)
					%c1 = 50;
				// Vertical component: small upward push (1/10th of forward force, max 5)
				%c2 = %c1 / 10;
				if(%c2 > 5)
					%c2 = 5;
				%mom = Vector::getFromRot( %b, %c1, %c2 );
			}

			// Vehicle Combat skill bonus: +1 damage per 100 skill points (capped at 1000 = +10 damage)
			// Check if this is a vehicle projectile
			%isVehicleProjectile = false;
			
			// Get the shooter's player object to check if it's a vehicle
			%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
			if(%shooterPlayerObj != -1)
			{
				%shooterType = getObjectType(%shooterPlayerObj);
				// Check if it's a vehicle object
				if(%shooterType == "Vehicle" || %shooterType == "Flier")
				{
					%isVehicleProjectile = true;
				}
			}
			// Also check if shooter is a client ID but the player is in a vehicle
			if(!%isVehicleProjectile && %type == $MissileDamageType && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
			{
				// This is likely a vehicle projectile - check if player is in a vehicle
				%playerObj = Client::getOwnedObject(%shooterClient);
				if(%playerObj != -1 && %playerObj.vehicle != "")
				{
					%isVehicleProjectile = true;
				}
			}
			
			if(%isVehicleProjectile && !isRPGAI(%shooterClient))
			{
				// Get Vehicle Combat skill (Skill 14)
				%vehicleSkill = $PlayerSkill[%shooterClient, $SkillVehicleCombat];
				if(%vehicleSkill == "" || %vehicleSkill == -1)
					%vehicleSkill = 0;
				
				// Cap skill at 1000
				if(%vehicleSkill > 1000)
					%vehicleSkill = 1000;
				
				// Calculate bonus: +1 damage per 100 skill points
				%vehicleBonus = floor(%vehicleSkill / 100);
				
				// Add bonus to damage (in Tribes damage units, before conversion)
				%value += %vehicleBonus;
			}

			%value = (%value / $TribesDamageToNumericDamage);
			}
		}

		//------------- DETERMINE MISS OR HIT -------------
		if(%preCalcMiss == "")
		{
			if(%type != $LandingDamageType && %shooterClient != %damagedClient && %shooterClient != 0)
			{
				// Check if shooter is a vehicle - vehicles always hit (no miss calculation)
				%isVehicle = false;
				
				// Get the shooter's player object to check if it's a vehicle
				%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
				if(%shooterPlayerObj != -1)
				{
					%shooterType = getObjectType(%shooterPlayerObj);
					// Check if it's a vehicle object
					if(%shooterType == "Vehicle" || %shooterType == "Flier")
					{
						%isVehicle = true;
					}
				}
				// Also check if shooter is a client ID but the player is in a vehicle
				// For vehicle projectiles, the engine might pass the controlling player's client ID
				// but we can detect this by checking if the skill type is invalid/empty
				// and the damage type is MissileDamageType (vehicle projectiles)
				if(!%isVehicle && %type == $MissileDamageType && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
				{
					// This is likely a vehicle projectile - check if player is in a vehicle
					%playerObj = Client::getOwnedObject(%shooterClient);
					if(%playerObj != -1 && %playerObj.vehicle != "")
					{
						%isVehicle = true;
					}
				}
				
				if(%isVehicle)
				{
					// Vehicles always hit - skip miss calculation
					%isMiss = false;
				}
				else
				{
					// Normal player/weapon miss calculation
					if(%type == $SpellDamageType)
					{
						%defenderDEF = fetchData(%damagedClient, "MDEF");
						%defenderSkill = $PlayerSkill[%damagedClient, $SkillSpellResistance];
						if(%defenderSkill == "" || %defenderSkill == -1)
							%defenderSkill = 0;
						%x = (%defenderDEF / 5) + %defenderSkill + 5;
						%defenseType = "MDEF";
						%skillTypeName = "SpellResistance";
					}
					else
					{
						%defenderDEF = fetchData(%damagedClient, "DEF");
						%defenderSkill = $PlayerSkill[%damagedClient, $SkillDodging];
						if(%defenderSkill == "" || %defenderSkill == -1)
							%defenderSkill = 0;
						%x = (%defenderDEF / 5) + %defenderSkill + 5;
						%defenseType = "DEF";
						%skillTypeName = "Dodging";
					}
					%attackerSkill = $PlayerSkill[%shooterClient, %skilltype];
					if(%attackerSkill == "" || %attackerSkill == -1)
						%attackerSkill = 1000; // Default skill value if not set (for NPCs or edge cases)
					%y = %attackerSkill + 5;
					
					%n = %x + %y;
					
					%r = floor(getRandom() * %n) + 1;
					
					// Calculate hit/miss percentages
					%missChance = (%x / %n) * 100;
					%hitChance = (%y / %n) * 100;
					
					%attackerName = Client::getName(%shooterClient);
					if(%attackerName == "")
						%attackerName = "AI Bot";
					%defenderName = Client::getName(%damagedClient);
					if(%defenderName == "")
						%defenderName = "AI Bot";
					
					%initialMiss = false;
					if(%r <= %x)
					{
						if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Calculated MISS! r=" @ %r @ " <= x=" @ %x @ " (Def=" @ %defenderDEF @ " Atk=" @ %attackerSkill @ ")");
						%isMiss = true;
						%initialMiss = true;
					}
					
					// LCK-based miss check
					%lckMissChance = (AddPoints(%damagedClient, 2) / 100);
					%lckRoll = getRandom();
					%lckMiss = false;
					if(!%initialMiss && %lckRoll <= %lckMissChance)
					{
						%isMiss = true;
						%lckMiss = true;
					}
					
					// Get all defender stats for debug output
					%defenderDEFValue = fetchData(%damagedClient, "DEF");
					%defenderMDEFValue = fetchData(%damagedClient, "MDEF");
					%defenderDodgingSkill = $PlayerSkill[%damagedClient, $SkillDodging];
					if(%defenderDodgingSkill == "" || %defenderDodgingSkill == -1)
						%defenderDodgingSkill = 0;
					%defenderSpellResistanceSkill = $PlayerSkill[%damagedClient, $SkillSpellResistance];
					if(%defenderSpellResistanceSkill == "" || %defenderSpellResistanceSkill == -1)
						%defenderSpellResistanceSkill = 0;
					
					// Debug output for hit/miss calculation
					//echo("DEBUG HIT/MISS: " @ %attackerName @ " -> " @ %defenderName);
					%damageTypeName = "Physical";
					if(%type == $SpellDamageType)
						%damageTypeName = "Spell";
					//echo("  Damage Type: " @ %damageTypeName);
					//echo("  Defender Stats: DEF=" @ %defenderDEFValue @ ", MDEF=" @ %defenderMDEFValue @ ", Dodging=" @ %defenderDodgingSkill @ ", SpellResistance=" @ %defenderSpellResistanceSkill);
					//if(%type == $SpellDamageType)
					//	echo("  Defender Used: MDEF=" @ %defenderMDEFValue @ " (MDEF/5=" @ floor(%defenderMDEFValue / 5) @ "), SpellResistance=" @ %defenderSpellResistanceSkill @ ", Base=5");
					//else
					//	echo("  Defender Used: DEF=" @ %defenderDEFValue @ " (DEF/5=" @ floor(%defenderDEFValue / 5) @ "), Dodging=" @ %defenderDodgingSkill @ ", Base=5");
					//echo("  Attacker: Skill=" @ %attackerSkill @ " (type: " @ %skilltype @ "), Base=5");
					//echo("  Calculation: x=" @ %x @ " (defender), y=" @ %y @ " (attacker), n=" @ %n @ " (total)");
					//echo("  Roll: r=" @ %r @ " (1-" @ %n @ ")");
					// Only show miss % when a player is attacking a bot (not when bots attack players)
					//if(!isRPGAI(%shooterClient) && isRPGAI(%damagedClient))
					//	echo("Player " @ %attackerName @ " (ID: " @ %shooterClient @ ") attacking bot - Miss Chance: " @ floor(%missChance * 100) / 100 @ "%, Hit Chance: " @ floor(%hitChance * 100) / 100 @ "%");
					//if(%initialMiss)
					//	echo("  RESULT: MISS (r <= x: " @ %r @ " <= " @ %x @ ")");
					//else if(%lckMiss)
					//{
					//	echo("  RESULT: MISS (LCK check: roll " @ floor(%lckRoll * 10000) / 10000 @ " <= " @ floor(%lckMissChance * 10000) / 10000 @ ")");
					//	echo("  LCK: " @ fetchData(%damagedClient, "LCK") @ ", LCK Miss Chance: " @ floor(%lckMissChance * 10000) / 10000);
					//}
					//else
					//	echo("  RESULT: HIT");
				}
			}
		}
		//------------- CHECK FOR A CRITICAL HIT -------------
		if(getRandom() > 0.5)
		{
			%norm = 1.000;
			%critskill = $PlayerSkill[%shooterClient, $SkillCriticals] / 3000;
			%critchance = %norm - %critskill;
			%critchance -= (AddPoints(%shooterClient, 1) / 100);
			if(%critchance < 0.1)
				%critchance = 0.1;
			if(getRandom() > %critchance)
				%criticalAttack = true;
		}
		
		//=======================================|WATER CHECKS|=========================================
		//------------------------------------
		// CHECK IF PLAYER LANDED ON WATER
		//------------------------------------
		if(%damagedClient == %shooterClient && %type == $LandingDamageType)
		{
			%object = "";
			for(%i = 0; %i >= -3.15; %i -= 1.57)
			{
				if(GameBase::getLOSInfo(Client::getOwnedObject(%damagedClient), 5, %i @ " 0 0"))
				{
					if(getObjectType($los::object) == "InteriorShape" && String::getSubStr(Object::getName($los::object), 0, 5) == "water")
					{
						%object = $los::object;
						break;
					}
				}
			}
			
			if(%object != "")
			{
				%value *= $waterDamageAmp;
				playSound(SoundSplash1, %damagedClientPos);
			}
		}

		//---------------------------------------
		// CHECK IF PLAYER LANDED WHILE IN WATER
		//---------------------------------------
		if(%damagedClient == %shooterClient && %type == $LandingDamageType)
		{
			if(Zone::getType(fetchData(%damagedClient, "zone")) == "WATER")
			%value *= $waterDamageAmp;
		}
		else if(%damagedClient != %shooterClient && %type == $LandingDamageType)
			%value = (fetchData(%damagedClient, "MaxHP") * (%value / 100) / 100);
		
		//---------------------------------------
		// PREVENT BOTS FROM TAKING COLLISION/FALL DAMAGE
		//---------------------------------------
		// Enemy bots and town bots should not take damage from being shoved or falling
		// This prevents them from taking collision damage when players shove them
		if(%type == $LandingDamageType)
		{
			%isEnemyBot = IsEnemyBot(%damagedClient);
			%isTownBot = IsTownBot(%damagedClient);
			if(%isEnemyBot || %isTownBot)
			{
				// Set damage to 0 for all bots (enemy and town) when taking landing/collision damage
				%value = 0;
				%isMiss = false;
				%noImpulse = true;
			}
		}
		//============================================================================================

		//------------------------------------------------
		// SEAL BATTLE BOT DAMAGE PREVENTION
		//------------------------------------------------
		// Prevent seal battle bots from damaging each other, but allow them to damage players
		// Also allow mages to damage themselves with their spells
		if(fetchData(%shooterClient, "SealBattleBot") == "true" && fetchData(%damagedClient, "SealBattleBot") == "true" && %shooterClient != %damagedClient)
		{
			// Both are seal battle bots and not the same entity - prevent damage
			%value = 0;
			%isMiss = false;
			%noImpulse = true;
			%sameTeamNull = true;
		}
		
		// Prevent players NOT in Colloseum from damaging seal battle bots that ARE in Colloseum
		// This prevents camping outside and sniping bots during seal battles
		if($SealBattleActive == true && !isRPGAI(%shooterClient) && fetchData(%damagedClient, "SealBattleBot") == "true")
		{
			%shooterZone = Zone::getType(fetchData(%shooterClient, "zone"));
			%damagedZone = Zone::getType(fetchData(%damagedClient, "zone"));
			
			// If shooter is NOT in Colloseum but damaged bot IS in Colloseum, prevent damage
			if(%shooterZone != "Colloseum" && %damagedZone == "Colloseum")
			{
				%value = 0;
				%isMiss = false;
				%noImpulse = true;
				%sameTeamNull = true;
			}
		}
		
		// Prevent players NOT in Colloseum from damaging Colloseum arena bots that ARE in Colloseum
		// This prevents camping outside and sniping bots during Colloseum arena battles
		// Colloseum bots are named "Round1", "Round2", "Round3" (from rpgarena.cs)
		if($Fight != "" && !isRPGAI(%shooterClient) && isRPGAI(%damagedClient))
		{
			%damagedName = Client::getName(%damagedClient);
			%isColloseumBot = (%damagedName == "Round1" || %damagedName == "Round2" || %damagedName == "Round3");
			
			if(%isColloseumBot)
			{
				%shooterZone = Zone::getType(fetchData(%shooterClient, "zone"));
				%damagedZone = Zone::getType(fetchData(%damagedClient, "zone"));
				
				// If shooter is NOT in Colloseum but damaged bot IS in Colloseum, prevent damage
				if(%shooterZone != "Colloseum" && %damagedZone == "Colloseum")
				{
					%value = 0;
					%isMiss = false;
					%noImpulse = true;
					%sameTeamNull = true;
				}
			}
		}

		//------------------------------------------------
		// AI BOT TO AI BOT DAMAGE PREVENTION
		//------------------------------------------------
		// Prevent all AI bots from damaging each other, but allow:
		// - AI bots to damage players
		// - AI bots to damage themselves (self-damage from spells, handled later)
		if(isRPGAI(%shooterClient) && isRPGAI(%damagedClient) && %shooterClient != %damagedClient)
		{
			// Both are AI bots and not the same entity - prevent damage
			%value = 0;
			%isMiss = false;
			%noImpulse = true;
			%sameTeamNull = true;
		}

		//------------------------------------------------
		// SAME TEAM CHECKS
		//------------------------------------------------
		// AI mobs can always attack players, skip all same-team checks for AI attackers
		if(!Duel::isDueling(%damagedClient) && !Duel::isDueling(%shooterClient) && !isRPGAI(%shooterClient))
		{
			// Prevent all player-to-player damage when seal battle is active and players are in Colloseum zone (allows AI to still damage players)
			if($SealBattleActive == true && %shooterClient != %damagedClient)
			{
				%damagedZone = Zone::getType(fetchData(%damagedClient, "zone"));
				%shooterZone = Zone::getType(fetchData(%shooterClient, "zone"));
				
				if(%damagedZone == "Colloseum" || %shooterZone == "Colloseum")
				{
					// Only show message once per seal battle per client to avoid spam
					if(fetchData(%shooterClient, "sealDamageMsgShown") != "true")
					{
						Client::sendMessage(%shooterClient, $MsgWhite, "Player-on-Player damage is disabled in Colloseum during seal battles.");
						storeData(%shooterClient, "sealDamageMsgShown", "true");
					}
					if(fetchData(%damagedClient, "sealDamageMsgShown") != "true")
					{
						Client::sendMessage(%damagedClient, $MsgWhite, "Player-on-Player damage is disabled in Colloseum during seal battles.");
						storeData(%damagedClient, "sealDamageMsgShown", "true");
					}
					
					%value = 0;
					%isMiss = false;
					%noImpulse = true;
					%sameTeamNull = true;
				}
			}
			
			if(Client::getTeam(%damagedClient) == Client::getTeam(%shooterClient) && %shooterClient != %damagedClient)
			{
				// TEMPORARILY DISABLED - HasTheftFlag check
				//if(!HasTheftFlag(%damagedClient))
				//{
				// Prevent damage if damaged player is in PROTECTED zone (regardless of shooter's zone)
				if(Zone::getType(fetchData(%damagedClient, "zone")) == "PROTECTED")
				{
					%value = 0;
					%isMiss = false;
					%noImpulse = true;
					%sameTeamNull = true;
				}
				//}
					
					///no target-list involved
					if(!(IsInCommaList(fetchData(%damagedClient, "targetlist"), Client::getName(%shooterClient)) || IsInCommaList(fetchData(%shooterClient, "targetlist"), Client::getName(%damagedClient))) )
					{	
						%dhn = $House::Index[fetchData(%damagedClient, "MyHouse")];
						%shn = $House::Index[fetchData(%shooterClient, "MyHouse")];
						if(%dhn == %shn)
						{
							%value = 0;
							%isMiss = false;
							%noImpulse = true;
							%sameTeamNull = true;
						}
						else
						{
							if(%dhn == "" || %shn == "")
							{
								//one of the people involved is not in a house, so no damage occurs
								%value = 0;
								%isMiss = false;
								%noImpulse = true;
								%sameTeamNull = true;
							}
						}
					}

					if(Zone::getType(fetchData(%damagedClient, "zone")) != "PROTECTED" && Zone::getType(fetchData(%shooterClient, "zone")) != "PROTECTED")
					{
						if(fetchData(%damagedClient, "partyOwned"))
						{
							if(IsInCommaList(fetchData(%damagedClient, "partylist"), Client::getName(%shooterClient)))
							{
								%value = 0;
								%isMiss = false;
								%noImpulse = true;
								%sameTeamNull = true;
							}
						}
						else if(fetchData(%shooterClient, "partyOwned"))
						{
							if(IsInCommaList(fetchData(%shooterClient, "partylist"), Client::getName(%damagedClient)))
							{
								%value = 0;
								%isMiss = false;
								%noImpulse = true;
								%sameTeamNull = true;
							}
						}
					}
				}
				else
				{
					//one of the people involved has the other one on his/her target-list.
					//so let damage go thru
				}
			}
		}
		//-------------------------------------------------
		// SAME PLAYER CHECKS
		//-------------------------------------------------
		if(%damagedClient == %shooterClient)
		{
			if(isRPGAI(%damagedClient))
				%value = %value / 3;
			else if(Zone::getType(fetchData(%damagedClient, "zone")) == "PROTECTED")
				%value = 0;
			else if(%type == $SpellDamageType)
				%value = %value / 3;
		}

		if(!IsDead(%this))
		{
			%armor = Player::getArmor(%this);
			storeData(%damagedClient, "tmpkillerid", %shooterClient);

			%hitby = Client::getName(%shooterClient);
			%msgcolor = "";

			if(%isMiss)
			{
				%msgcolor = $MsgRed;
				%value = 0;
			}
			else if(!%isMiss && %value == 0 && %shooterClient != %damagedClient)
			{
				%msgcolor = $MsgWhite;
			}
			if(%msgcolor != "")
			{
				// Default damageMode to true if not set (for backwards compatibility)
				%shooterDamageMode = fetchData(%shooterClient, "damageMode");
				if(%shooterDamageMode == "")
					%shooterDamageMode = true;
				%damagedDamageMode = fetchData(%damagedClient, "damageMode");
				if(%damagedDamageMode == "")
					%damagedDamageMode = true;
				
				%isAI = isRPGAI(%shooterClient);
				
				if(%type != $SpellDamageType)
				{
					// Only send message to shooter if they're a player (not AI)
					if(!%isAI && %shooterDamageMode)
					{
						%msg = "<jl>You try to hit " @ Client::getName(%damagedClient) @ ", but miss!";
						DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
					}

					// Always send miss message to damaged player
					if(%damagedDamageMode)
					{
						%time = getIntegerTime(true) >> 5;
						if(%time - %damagedClient.lastMissMessage > 2)
						{
							%damagedClient.lastMissMessage = %time;
							%msg = "<jr>" @ %hitby @ " tries to hit you, but misses!";
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
					}
				}
				else
				{
					// Only send message to shooter if they're a player (not AI)
					if(!%isAI && %shooterDamageMode)
					{
						if(%type == $SpellDamageType)
						{
							%msg = "<jl>" @ Client::getName(%damagedClient) @ " resists your spell!";
							DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
						}
						else
							Client::sendMessage(%shooterClient, %msgcolor, Client::getName(%damagedClient) @ " resists your spell!");
					}
					if(%damagedDamageMode)
					{
						if(%type == $SpellDamageType)
						{
							%msg = "<jr>You resist " @ %hitby @ "'s spell!";
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
						else
							Client::sendMessage(%damagedClient, %msgcolor, "You resist " @ %hitby @ "'s spell!");
					}
				}
			}

			//-------------------------------------------------
			// SKILLS
			//-------------------------------------------------
			if(%skilltype >= 1 && !%sameTeamNull && %shooterClient != %damagedClient)
			{
				%base1 = Cap(35 + (fetchData(%shooterClient, "LVL") - fetchData(%damagedClient, "LVL")), 1, "inf");
				%base2 = Cap(35 + (fetchData(%damagedClient, "LVL") - fetchData(%shooterClient, "LVL")), 1, "inf");
				if(%isMiss)
				{
					UseSkill(%shooterClient, %skilltype, false, true);
					UseSkill(%damagedClient, $SkillEndurance, false, true, 60);
					
					if(%type == $SpellDamageType)
						UseSkill(%damagedClient, $SkillSpellResistance, true, true, %base2);
					else
						UseSkill(%damagedClient, $SkillDodging, true, true, %base2 * (3/5));
				}
				else if(!%isMiss && %value == 0)
				{
					UseSkill(%shooterClient, %skilltype, false, true);
					UseSkill(%damagedClient, $SkillEndurance, false, true, 60);
					
					if(%type == $SpellDamageType)
						UseSkill(%damagedClient, $SkillSpellResistance, true, true, %base2);
					else
						UseSkill(%damagedClient, $SkillDodging, true, true, %base2 * (3/5));
				}
				else
				{
					UseSkill(%shooterClient, %skilltype, true, true, %base1);
					UseSkill(%damagedClient, $SkillEndurance, true, true, 60);

					if(%type == $SpellDamageType)
						UseSkill(%damagedClient, $SkillSpellResistance, true, true, %base2);
				}

				if(%Bash || %Cleave || %Pierce)
					UseSkill(%shooterClient, $SkillBashing, true, true);
			}

			// Ensure %value is numeric before processing
			if(%value == "" || %value == -1)
				%value = 0;
			
			if(%value)
			{
				if(%value < 0)
					%value = 0;
				if(%criticalAttack)
					%value *= 2;
				
				// Check if shooter is a turret - turret damage bypasses all stance modifiers (except Normal)
				// Use comprehensive turret detection (same as earlier in function)
				%isTurretDamage = false;
				if(isObject(%object))
				{
					%objectClassName = %object.className;
					if(%objectClassName == "Turret")
						%isTurretDamage = true;
				}
				
				// Also detect turret projectiles by checking if it's MissileDamageType from a source without skills/ATK
				// Turrets use MissileDamageType projectiles but don't have player skills or ATK
				if(!%isTurretDamage && %type == $MissileDamageType)
				{
					// Check if shooter doesn't have ATK or skills (indicating it's a turret, not a player)
					%shooterATK = fetchData(%shooterClient, "ATK");
					if((%shooterATK == "" || %shooterATK == -1 || %shooterATK == 0) && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
					{
						// Check if it's not a vehicle projectile (vehicles are handled separately)
						%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
						%isVehicle = false;
						if(%shooterPlayerObj != -1)
						{
							%shooterType = getObjectType(%shooterPlayerObj);
							// Check if shooter object IS a vehicle/flier
							if(%shooterType == "Vehicle" || %shooterType == "Flier")
								%isVehicle = true;
							// Also check if player is IN a vehicle (for when player fires from vehicle)
							if(!%isVehicle && %shooterPlayerObj.vehicle != "")
								%isVehicle = true;
						}
						// If not a vehicle and no ATK/skills, it's likely a turret
						if(!%isVehicle)
							%isTurretDamage = true;
					}
				}
				
				// Offensive stance: doubles damage dealt and received (all damage types, including spells)
				// Turret damage bypasses stance modifiers
				if(!%isTurretDamage && fetchData(%shooterClient, "Stance") == "Offensive")
					%value *= 2;
				if(!%isTurretDamage && fetchData(%damagedClient, "Stance") == "Offensive")
					%value *= 2;
				
				// Defensive stance: halves damage dealt and received (all damage types)
				// Turret damage bypasses stance modifiers
				if(!%isTurretDamage && fetchData(%shooterClient, "Stance") == "Defensive")
					%value /= 2;
				if(!%isTurretDamage && fetchData(%damagedClient, "Stance") == "Defensive")
					%value /= 2;
				
				// Glass Cannon stance: 2.5x spell damage dealt, -50% max HP and -50% max mana (handled in rpgstats.cs)
				// Turret damage is affected by Glass Cannon (allows turrets to benefit from Glass Cannon stance)
				if(fetchData(%shooterClient, "Stance") == "Glass Cannon" && %type == $SpellDamageType)
					%value *= 2.5;

				// MageBane: nullifies magic damage received, doubles physical damage received
				// Turret damage bypasses bane stances
				if(!%isTurretDamage && fetchData(%damagedClient, "MANA") > 0)
				{
					if(%type == $SpellDamageType && fetchData(%damagedClient, "Stance") == "MageBane") 
						%value = 0;
					if(%type != $SpellDamageType && fetchData(%damagedClient, "Stance") == "MageBane")
						%value *= 2;
				}
				
				// BladeBane: nullifies physical damage received, doubles magic damage received
				// Turret damage bypasses bane stances
				if(!%isTurretDamage && fetchData(%damagedClient, "MANA") > 0)
				{
					if(%type != $SpellDamageType && fetchData(%damagedClient, "Stance") == "BladeBane")
						%value = 0;
					if(%type == $SpellDamageType && fetchData(%damagedClient, "Stance") == "BladeBane")
					%value *= 2;
			}
			
			// =================================================================
			// ASCENSION TALENTS (permanent abilities from remort/SP sacrifice)
			// =================================================================
			
			// Berserker's Rage: +30% damage when attacker HP is below 25%
			if(Ascension::HasTalent(%shooterClient, "BerserkerRage"))
			{
				%attackerHP = fetchData(%shooterClient, "HP");
				%attackerMaxHP = fetchData(%shooterClient, "MaxHP");
				if(%attackerMaxHP > 0 && %attackerHP <= (%attackerMaxHP * 0.25))
					%value = floor(%value * 1.30);
			}
			
			// Iron Skin: 15% damage reduction from all sources
			if(Ascension::HasTalent(%damagedClient, "IronSkin"))
				%value = floor(%value * 0.85);
			
			// Dodge Mastery: 10% chance to completely avoid damage
			// NOTE: this runs AFTER the standard miss-messaging block, so send our
			// own feedback to BOTH sides here (the attacker previously got no
			// message at all - their swing just silently did nothing)
			if(Ascension::HasTalent(%damagedClient, "DodgeMastery"))
			{
				if(floor(getRandom() * 100) < 10)
				{
					%value = 0;
					%isMiss = true;
					Client::sendMessage(%damagedClient, $MsgGreen, "Dodge Mastery!");
					if(!isRPGAI(%shooterClient) && %shooterClient != %damagedClient)
						Client::sendMessage(%shooterClient, $MsgRed, Client::getName(%damagedClient) @ " dodges your attack!");
				}
			}
			
		// =================================================================
			
			// =================================================================
			// ARMOR SPECIAL EFFECTS (Pre-Damage)
			// =================================================================
			
			// Check for armor special effects on the damaged player
			%armorEffect = $ArmorEffect[%damagedCurrentArmor];
			%armorEffectChance = $ArmorEffectChance[%damagedCurrentArmor];
			
			// PHASE_SHIFT (Void Robe): 5% chance to completely dodge an attack
			if(%armorEffect == "PHASE_SHIFT" && %value > 0 && !%isMiss && %shooterClient != %damagedClient)
			{
				%phaseRoll = floor(getRandom() * 100);
				if(%phaseRoll < %armorEffectChance)
				{
					// Phase shift triggered - complete dodge
					%value = 0;
					%isMiss = true;
					Client::sendMessage(%damagedClient, $MsgBeige, "PHASE SHIFT! You phase through the attack!");
					Client::sendMessage(%shooterClient, $MsgBeige, Client::getName(%damagedClient) @ " phases through your attack!");
				}
			}
			
			// Store pre-damage value for RETRIBUTION calculation
			%preDamageValue = %value;
			
			// Ensure value doesn't become 0 or negative after stance modifiers
			if(%value < 0)
				%value = 0;
				
				if(%Pierce)
				{
					//Not really any way to code this using a player's skill that won't
					// end up being too powerful at one point (ultimate pk anyone?)
					// instead let's just go with a 5% chance!

					//%regular = 1.0;
					//%chance = $PlayerSkill[%shooterClient, $SkillBashing] / 50000;
					//%special = %regular - %chance;
				
					//if(%special < 0.5)
					//	%special = 0.5;

					//if(getRandom() > %special)
					if(getRandom() > 0.95)
						$lckBypass[%damagedClient] = true;
				}
				
				%backupValue = %value;

				%rhp = refreshHP(%damagedClient, %value);

				// Weekly boss contribution (WeeklyBoss.cs): credit real landed
				// damage in NUMERIC hp units (refreshHP subtracts value x
				// $TribesDamageToNumericDamage; -1 return = LCK miss, no credit)
				if($Weekly::BossClient != "" && %damagedClient == $Weekly::BossClient && %rhp != -1 && %value > 0)
					Weekly::OnDamage(%shooterClient, round(%value * $TribesDamageToNumericDamage));

				%lckMiss = false;
				if(%rhp == -1)
				{
					%value = -1;	//There was an LCK miss
					%lckMiss = true;
				}
				else
				{
					if(!%noImpulse) Player::applyImpulse(%this,%mom);
						%noImpulse = "";

					if(%damagedCurrentArmor != "")
						%ahs = $ArmorHitSound[%damagedCurrentArmor];
					else
						%ahs = SoundHitFlesh;
					if(%skilltype == $SkillSlashing)
						PlaySound(%ahs, %damagedClientPos);
					else if(%skilltype == $SkillBludgeoning)
						PlaySound(%ahs, %damagedClientPos);
					else if(%skilltype == $SkillPiercing)
						PlaySound(%ahs, %damagedClientPos);
					else if(%skilltype == $SkillVehicleCombat)
						PlaySound(SoundArrowHit2, %damagedClientPos);
				}

				PlaySound(RandomRaceSound(fetchData(%damagedClient, "RACE"), Hit), %damagedClientPos);

				// =================================================================
				// ARMOR SPECIAL EFFECTS (Post-Damage Retaliation)
				// =================================================================
				
				// Only apply retaliation effects if damage was actually dealt and shooter is valid
				if(%preDamageValue > 0 && %shooterClient != %damagedClient && !isTownBot(%shooterClient))
				{
					// RETRIBUTION (Judgement Robe): 25% chance to reflect 10% of damage back to attacker
					if(%armorEffect == "RETRIBUTION")
					{
						%retribRoll = floor(getRandom() * 100);
						if(%retribRoll < %armorEffectChance)
						{
							// Calculate reflection based on displayed damage (not internal units)
							// First convert to displayed damage, then take 10%, then convert back to internal
							%displayedDamage = %preDamageValue * $TribesDamageToNumericDamage;
							%reflectDmgDisplay = floor(%displayedDamage * 0.10);
							%reflectAmount = %reflectDmgDisplay / $TribesDamageToNumericDamage;
							
							if(%reflectAmount > 0)
							{
								// Apply reflected damage to attacker
								refreshHP(%shooterClient, %reflectAmount);
								Client::sendMessage(%damagedClient, $MsgBeige, "RETRIBUTION! Reflected " @ %reflectDmgDisplay @ " damage!");
								Client::sendMessage(%shooterClient, $MsgRed, "You take " @ %reflectDmgDisplay @ " reflected damage!");
								playSound(SoundShieldHit, %shooterClientPos);
							}
						}
					}
					
					// STATIC_DISCHARGE (Storm Robe): 25% chance to zap attacker for 500 damage
					if(%armorEffect == "STATIC_DISCHARGE")
					{
						%staticRoll = floor(getRandom() * 100);
						if(%staticRoll < %armorEffectChance)
						{
							// Static discharge triggered - zap the attacker
							%zapDamage = 500 / $TribesDamageToNumericDamage; // Convert to internal damage units
							refreshHP(%shooterClient, %zapDamage);
							Client::sendMessage(%damagedClient, $MsgBeige, "STATIC DISCHARGE! Zapped attacker for 500 damage!");
							Client::sendMessage(%shooterClient, $MsgRed, "STATIC SHOCK! You take 500 lightning damage!");
								playSound(shockExplosion, %shooterClientPos);
							
							// Create lightning visual effect at attacker position
							// Use Bomb20 ("blue explosion" type visual often used for lightning/shock in RPGs)
							CreateAndDetBomb_VisualOnly(%damagedClient, "Bomb20", %shooterClientPos, -1);
						}
					}
				}

				// =================================================================
				// MYTHIC WEAPON EFFECTS (Post-Damage, Remort 125 Tier)
				// These hooks run on the FINAL dealt damage (%backupValue, internal
				// units) so lifesteal/echoes scale with what actually landed.
				// Deaths caused by the bonus refreshHP calls below are picked up by
				// the existing Player::IsDead check further down, so kill credit and
				// Client::onKilled flow through the normal path.
				// =================================================================
				if(%weaponEffect != "" && !%isMiss && !%lckMiss && %backupValue > 0 && %shooterClient != %damagedClient)
				{
					// SOUL REAVER - lifesteal on every hit, Soul Nova at max souls
					if(%weaponEffect == "SOUL_HARVEST")
					{
						// Lifesteal: heal % of displayed damage dealt
						%stealPct = $WeaponEffectLifesteal[%weapon];
						if(%stealPct == "" || %stealPct == -1) %stealPct = 5;
						%dealtDisplay = %backupValue * $TribesDamageToNumericDamage;
						%healAmt = floor(%dealtDisplay * %stealPct / 100);
						if(%healAmt > 0)
						{
							%curHP = fetchData(%shooterClient, "HP");
							%maxHP = fetchData(%shooterClient, "MaxHP");
							%newHP = %curHP + %healAmt;
							if(%newHP > %maxHP)
								%newHP = %maxHP;
							if(%newHP > %curHP)
								setHP(%shooterClient, %newHP);
						}

						// Soul Nova: at max souls the current strike erupts
						%maxSouls = $WeaponEffectMaxSouls[%weapon];
						if(%maxSouls == "" || %maxSouls == -1) %maxSouls = 10;
						%souls = $SoulStacks[%shooterClient];
						if(%souls == "" || %souls == -1) %souls = 0;
						if(%souls >= %maxSouls)
						{
							$SoulStacks[%shooterClient] = 0;

							// Nova damage scales with souls consumed and Bludgeoning skill
							%novaDmg = 200 * %souls;
							%bludgeonSkill = $PlayerSkill[%shooterClient, $SkillBludgeoning];
							if(%bludgeonSkill == "" || %bludgeonSkill == -1) %bludgeonSkill = 1000;
							%novaDmg = round((%novaDmg * %bludgeonSkill) / 1000);

							// Nova is magical - reduced by MDEF
							%targetMDEF = fetchData(%damagedClient, "MDEF");
							if(%targetMDEF == "" || %targetMDEF == -1) %targetMDEF = 0;
							%mdefReduction = (getRandom() * (%targetMDEF / 10)) + 1;
							%novaDmg = floor(Cap(%novaDmg - %mdefReduction, 1, "inf"));

							refreshHP(%damagedClient, %novaDmg / $TribesDamageToNumericDamage);
							CreateAndDetBomb_VisualOnly(%shooterClient, "Bomb10", %damagedClientPos, -1);
							playSound(shockExplosion, %damagedClientPos);
							Client::sendMessage(%shooterClient, $MsgRed, "SOUL NOVA! " @ %souls @ " souls erupt for " @ %novaDmg @ " bonus damage!");
							Client::sendMessage(%damagedClient, $MsgRed, "SOUL NOVA! Harvested souls erupt against you for " @ %novaDmg @ " damage!");
						}
					}

					// SKY RENDER - every Nth hit launches the target skyward, then a
					// delayed impale strikes where they land
					if(%weaponEffect == "SKY_LAUNCH")
					{
						%frequency = $WeaponEffectFrequency[%weapon];
						if(%frequency == "" || %frequency == -1) %frequency = 4;

						if(%hitCount > 0 && (%hitCount % %frequency) == 0)
						{
							// Same protections as Final Verdict: no launching town bots,
							// teammates, party members, bosses, seal or colloseum bots
							if(!MythicWeapon::IsProtectedTarget(%shooterClient, %damagedClient))
							{
								// Launch skyward (vertical impulse; bash shoves cap at ~5 vertical,
								// 40 gives a clearly airborne launch without orbiting them)
								Player::applyImpulse(%this, "0 0 40");
								playSound(shockExplosion, %damagedClientPos);
								Client::sendMessage(%shooterClient, $MsgYellow, "SKY RENDER! " @ Client::getName(%damagedClient) @ " is launched skyward!");
								Client::sendMessage(%damagedClient, $MsgRed, "SKY RENDER! You are hurled into the sky!");

								// Delayed impale where they come down - pass the player object
								// id so a respawn/disconnect in the meantime cancels it
								schedule("SkyRender::Impale(" @ %shooterClient @ ", " @ %damagedClient @ ", " @ %this @ ");", 1.5);
							}
						}
					}

					// ECHO FANG - hits pool into a delayed echo, consecutive hits build Momentum
					if(%weaponEffect == "ECHO_STRIKE")
					{
						// Momentum: stacks from previous consecutive hits boost this hit
						%now = getSimTime();
						%combo = $EchoCombo[%shooterClient];
						if(%combo == "" || %combo == -1) %combo = 0;
						if(%now > $EchoComboExpire[%shooterClient])
							%combo = 0;

						if(%combo > 0)
						{
							%momentumDmg = floor(%backupValue * 0.10 * %combo * $TribesDamageToNumericDamage);
							if(%momentumDmg > 0)
							{
								refreshHP(%damagedClient, %momentumDmg / $TribesDamageToNumericDamage);
								if(%combo == 5)
									Client::sendMessage(%shooterClient, $MsgYellow, "MAX MOMENTUM! +" @ %momentumDmg @ " bonus damage!");
							}
						}

						// Build the combo for the next hit (max 5 = +50%)
						%combo++;
						if(%combo > 5)
							%combo = 5;
						$EchoCombo[%shooterClient] = %combo;
						$EchoComboExpire[%shooterClient] = %now + 1.5;

						// Echo: pool 40% of dealt damage, one delayed resolution per target
						%echoPct = $WeaponEffectEchoPercent[%weapon];
						if(%echoPct == "" || %echoPct == -1) %echoPct = 40;
						%echoDelay = $WeaponEffectEchoDelay[%weapon];
						if(%echoDelay == "" || %echoDelay == -1) %echoDelay = 2.0;

						%echoDmg = %backupValue * %echoPct / 100;
						%pool = $EchoPool[%shooterClient, %damagedClient];
						if(%pool == "" || %pool == -1) %pool = 0;
						$EchoPool[%shooterClient, %damagedClient] = %pool + %echoDmg;

						if($EchoPending[%shooterClient, %damagedClient] != true)
						{
							$EchoPending[%shooterClient, %damagedClient] = true;
							schedule("EchoFang::Echo(" @ %shooterClient @ ", " @ %damagedClient @ ", " @ %this @ ");", %echoDelay);
						}
					}
				}

			//display amount of damage caused
			// Check for LCK miss first before converting -1 to 0
			if(%lckMiss)
			{
				// Handle LCK miss message (this will be handled in the else if block below)
				%convValue = -1; // Keep as -1 to trigger LCK miss message
			}
			else
			{
				if(%value == "" || %value == -1)
					%value = 0;
				%convValue = round(%value * $TribesDamageToNumericDamage);
			}

			// Initialize LCK hit flag early (will be set later if LCK bypass or LCK death prevention occurs)
			%isLCKHit = false;
			
			// Check for LCK bypass (pierce attacks) before checking convValue (LCK hits bypass damage, so convValue might be 0)
			if($lckBypass[%damagedClient] == true)
			{
				%isLCKHit = true;
				$lckBypass[%damagedClient] = false; // Clear flag
			}
			
			// Also check for LCK death prevention (when attack would kill but LCK prevents death)
			if(%lckMiss == true)
			{
				%isLCKHit = true;
			}
			
			// Build messages if there's damage OR if it's an LCK hit (LCK hits show even with 0 damage or -1 convValue)
			if(%convValue > 0 || %isLCKHit)
			{
				if(%shooterClient == %damagedClient)
					{
						if(%type == $CrushDamageType)
							%hitby = "moving object";
						else if(%type == $DebrisDamageType)
							%hitby = "debris";
						else if(%type == $SpellDamageType)
							%hitby = "your " @ $Spell::name[$Spell::index[%weapon]];
						else
							%hitby = "yourself";
					}
					else if(%shooterClient == 0)
					{
						if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%hitby = $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "'");
								%hitby = "a powerful force";
							}
						}
						else
							%hitby = "a powerful force";
					}
					else
					{
						if(fetchData(%shooterClient, "invisible"))
							%hitby = "an unknown assailant";
						else if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%hitby = Client::getName(%shooterClient) @ "'s " @ $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "', shooter=" @ %shooterClient);
								%hitby = Client::getName(%shooterClient);
							}
						}
						else
							%hitby = Client::getName(%shooterClient);
					}

					if(%Pierce)
					{
						%daction = "pierced";
						%saction = "pierced";
					}
					else if(%Bash)
					{
						%daction = "bashed";
						%saction = "bashed";
					}
					else if(%Cleave)
					{
						%daction = "cleaved";
						%saction = "cleaved";
					}					
					else
					{
						%daction = "damaged";
						%saction = "damaged";
					}

					//--------------------
					// Build ATKText messages (bouncing damage display)
					//--------------------
					// Get enemy name for messages
					%enemyName = Client::getName(%damagedClient);
					%attackerName = Client::getName(%shooterClient);
					
					// LCK hit flag was already checked and set earlier (before convValue check)
					
					// Build attacker message (player dealing damage)
					if(%isLCKHit)
					{
						%Val1 = "You hit " @ %enemyName @ " for LCK!";
					}
					else
					{
						%Val1 = "You hit " @ %enemyName @ " for " @ %convValue @ "!";
					}
					
					// Build defender message (player taking damage)
					// Always use "'s attack" format as requested
					if(%isLCKHit)
					{
						%Val2 = %attackerName @ "'s attack hit you for LCK!";
					}
					else
					{
						%Val2 = %attackerName @ "'s attack hit you for " @ %convValue @ "!";
					}

					if(%Bash)
					{
						%Val1 = "Bashed! " @ %Val1;
						%Val2 = "Bashed! " @ %Val2;
					}
					else if(%Pierce)
					{
						%Val1 = "Pierced! " @ %Val1;
						%Val2 = "Pierced! " @ %Val2;
					}
					else if(%Cleave)
					{
						%Val1 = "Cleaved! " @ %Val1;
						%Val2 = "Cleaved! " @ %Val2;
					}

					if(%criticalAttack)
					{
						%Val1 = "<f2>Critical hit! <f0>" @ %Val1;
						%Val2 = "<f2>Critical hit! <f1>" @ %Val2;
					}

					//--------------------
					//display to involved
					//--------------------
					// Default damageMode to true if not set (for backwards compatibility)
					%shooterDamageMode = fetchData(%shooterClient, "damageMode");
					if(%shooterDamageMode == "")
						%shooterDamageMode = true;
					%damagedDamageMode = fetchData(%damagedClient, "damageMode");
					if(%damagedDamageMode == "")
						%damagedDamageMode = true;
					
					if(%shooterClient != %damagedClient)
						if(%shooterDamageMode)
						{
							%spellName = "";
							if(%type == $SpellDamageType && %weapon != "")
							{
								%spellIndex = $Spell::index[%weapon];
								if(%spellIndex != "")
									%spellName = " with " @ $Spell::name[%spellIndex];
							}
							// Check for LCK hit first - use %Val1 which already has the correct LCK message
							if(%isLCKHit)
							{
								// Use %Val1 which was already constructed with "You hit [enemy] for LCK!" message
								%msg = "<jl>" @ %Val1;
								DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
							}
							else if(%type == $SpellDamageType)
							{
								if(%criticalAttack)
								{
									%msg = "<jl>You critically " @ %saction @ " " @ Client::getName(%damagedClient) @ %spellName @ " for " @ %convValue @ " points of damage!";
									DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
								}
								else
								{
									%msg = "<jl>You " @ %saction @ " " @ Client::getName(%damagedClient) @ %spellName @ " for " @ %convValue @ " points of damage!";
									DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
								}
							}
							else
							{
								// For weapon attacks, use %Val1 which already has the correct message
								%msg = "<jl>" @ %Val1;
								DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
							}
							
							// REMOVED: ATKText for attacker is now handled by DisplayDamageMessage above
							// The following block was causing duplicate floating damage messages
							// if(!Player::isAiControlled(%shooterClient) && %shooterClient != "" && %shooterClient != -1 && %shooterClient != 0)
							if(false) // DISABLED
							{
								// Validate client is connected
								%clientName = Client::getName(%shooterClient);
								if(%clientName != "" && %clientName != -1)
								{
									%displayType = fetchData(%shooterClient, "damageDisplayType");
									%floatingEnabled = fetchData(%shooterClient, "floatingDamageNumbers");
									// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
									if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
									{
									// Get animation style preference
									%animationStyle = fetchData(%shooterClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "attacker" = player dealing damage (red), "defender" = player taking damage (current color)
									// Send text with tags - client will handle color conversion for attacker view
									// Remove justify tags but keep font color tags for critical hits
									%cleanVal1 = String::replace(%Val1, "<jc>", "");
									%cleanVal1 = String::replace(%cleanVal1, "<jr>", "");
									%cleanVal1 = String::replace(%cleanVal1, "<jl>", "");
									remoteEval(%shooterClient, "ATKText", %cleanVal1, %animationStyle, "attacker");
									}
								}
							}
						}
					
					// Push target frame to attacker's ScriptGL HUD (with damage dealt)
					if(%shooterClient != %damagedClient)
					{
						if(%isLCKHit)
							KronosHUD_PushTarget(%shooterClient, %damagedClient, "LCK");
						else
							KronosHUD_PushTarget(%shooterClient, %damagedClient, %convValue);
					}

					if(%damagedDamageMode)
					{
						// Check for LCK hit first - use %Val2 which already has the correct LCK message
						if(%isLCKHit)
						{
							// Use %Val2 which was already constructed with "[attacker]'s attack hit you for LCK!" message
							%msg = "<jr>" @ %Val2;
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
						else if(%type == $SpellDamageType)
						{
							if(%criticalAttack)
							{
								%msg = "<jr>You were critically " @ %daction @ " by " @ %hitby @ " for " @ %convValue @ " points of damage!";
								DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
							}
							else
							{
								%msg = "<jr>You were " @ %daction @ " by " @ %hitby @ " for " @ %convValue @ " points of damage!";
								DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
							}
						}
						else
						{
							// For weapon attacks, use %Val2 which already has the correct message
							%msg = "<jr>" @ %Val2;
							DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						}
						
						// REMOVED: ATKText for defender is now handled by DisplayDamageMessage above
						// The following block was causing duplicate floating damage messages
						// if(!Player::isAiControlled(%damagedClient) && %damagedClient != "" && %damagedClient != -1 && %damagedClient != 0)
						if(false) // DISABLED
						{
							// Validate client is connected
							%clientName = Client::getName(%damagedClient);
							if(%clientName != "" && %clientName != -1)
							{
								%displayType = fetchData(%damagedClient, "damageDisplayType");
								%floatingEnabled = fetchData(%damagedClient, "floatingDamageNumbers");
								// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
								if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
								{
									// Get animation style preference
									%animationStyle = fetchData(%damagedClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "attacker" = player dealing damage (red), "defender" = player taking damage (current color)
									remoteEval(%damagedClient, "ATKText", "<jc>" @ %Val2, %animationStyle, "defender");
								}
							}
						}
					}

					//--------------------
					//display to radius
					//--------------------
					if(%shooterClient == 0)
					{
						if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%sname = $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "' in radius message");
								%sname = "A powerful force";
							}
						}
						else
							%sname = "A powerful force";
						// CRITICAL: Use GetClientOrBotName for consistent naming (especially for enemy bots)
						%dname = GetClientOrBotName(%damagedClient);
					}
					else if(%shooterClient == %damagedClient)
					{
						%sname = Client::getName(%shooterClient);
						if(String::ICompare(Client::getGender(%damagedClient), "Male") == 0)
							%dname = "himself";
						else if(String::ICompare(Client::getGender(%damagedClient), "Female") == 0)
							%dname = "herself";
						else
							%dname = "itself";
					}
					else
					{
						if(fetchData(%shooterClient, "invisible"))
							%sname = "An unknown assailant";
						else if(%type == $SpellDamageType && %weapon != "")
						{
							%spellIndex = $Spell::index[%weapon];
							if(%spellIndex != "")
								%sname = Client::getName(%shooterClient) @ "'s " @ $Spell::name[%spellIndex];
							else
							{
								//echo("DEBUG: Spell index not found for weapon='" @ %weapon @ "' in radius message, shooter=" @ %shooterClient);
								%sname = Client::getName(%shooterClient);
							}
						}
						else
							%sname = Client::getName(%shooterClient);
						
						// CRITICAL: Use GetClientOrBotName for consistent naming (especially for enemy bots)
						%dname = GetClientOrBotName(%damagedClient);
					}

					// Send radius messages only to players who don't have floating damage enabled
					// Get positions for radius check (validate clients first)
					%damagedPos = GameBase::getPosition(%damagedClient);
					%shooterPos = "";
					if(%shooterClient != 0 && %shooterClient != "" && %shooterClient != -1)
						%shooterPos = GameBase::getPosition(%shooterClient);
					else
						%shooterPos = %damagedPos; // Use damaged position as fallback
					
					%radiusMsg = "";
					// Check if LCK hit (initialize if not already set)
					if(%isLCKHit == "")
						%isLCKHit = false;
					if(%isLCKHit)
					{
						if(%criticalAttack)
							%radiusMsg = %sname @ " critically hits " @ %dname @ " for LCK!";
						else
							%radiusMsg = %sname @ " hits " @ %dname @ " for LCK!";
					}
					else
					{
						if(%criticalAttack)
							%radiusMsg = %sname @ " critically hits " @ %dname @ " for " @ %convValue @ " points of damage!";
						else
							%radiusMsg = %sname @ " hits " @ %dname @ " for " @ %convValue @ " points of damage!";
					}
					
					// review #65: merged the two full Client::getFirst rescans (radius chat +
					// spectator ATKText) into ONE pass - they iterated every connected client
					// with identical attacker/defender/AI + radius skips and are mutually
					// exclusive (non-floating gets chat, floating gets ATKText), so this halves
					// the per-hit getPosition/getDistance work. Build the spectator message
					// up-front so the single loop can emit either. Per-branch checks are
					// preserved exactly: the chat branch keeps the dead-skip, the ATKText branch
					// keeps the name check and does NOT skip dead.
					if(%isLCKHit == "")
						%isLCKHit = false;
					if(%isLCKHit)
						%spectatorMsg = %sname @ " hit " @ %dname @ " for LCK!";
					else
						%spectatorMsg = %sname @ " hit " @ %dname @ " for " @ %convValue @ "!";
					if(%criticalAttack)
						%spectatorMsg = "<f2>Critical hit! <f0>" @ %spectatorMsg;

					for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
					{
						// Common skips (both original loops): attacker/defender/AI, invalid, no position
						if(%cl == %shooterClient || %cl == %damagedClient || Player::isAiControlled(%cl))
							continue;
						if(%cl == "" || %cl == -1 || %cl == 0)
							continue;
						%clPos = GameBase::getPosition(%cl);
						if(%clPos == "")
							continue;
						%dist1 = Vector::getDistance(%clPos, %damagedPos);
						%dist2 = Vector::getDistance(%clPos, %shooterPos);
						if(%dist1 > $maxSAYdistVec && %dist2 > $maxSAYdistVec)
							continue; // Too far away

						%displayType = fetchData(%cl, "damageDisplayType");
						%floatingEnabled = fetchData(%cl, "floatingDamageNumbers");
						if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
						{
							// floating-damage users: spectator ATKText (old 2nd loop - name check, no dead skip)
							%clientName = Client::getName(%cl);
							if(%clientName == "" || %clientName == -1)
								continue;
							%animationStyle = fetchData(%cl, "floatingAnimationStyle");
							if(%animationStyle == "")
								%animationStyle = "float"; // Default style
							if(%animationStyle == "redmoon" || %animationStyle == "wow")
								%animationStyle = "float";
							if(%animationStyle == "nameplate")
								%animationStyle = "pop";
							remoteEval(%cl, "ATKText", %spectatorMsg, %animationStyle, "spectator");
						}
						else
						{
							// everyone else: radius chat (old 1st loop - skip dead)
							if(IsDead(%cl))
								continue;
							Client::sendMessage(%cl, $MsgBeige, %radiusMsg);
						}
					}
				}
				else if(%convValue < 0)
				{
					//this happens when there's a LCK consequence as miss

					if(%shooterClient == 0)
					{
						if(%type == $SpellDamageType && %weapon != "")
							%hitby = $Spell::name[$Spell::index[%weapon]];
						else
							%hitby = "A powerful force";
					}
					else
					{
						if(%type == $SpellDamageType && %weapon != "")
							%hitby = Client::getName(%shooterClient) @ "'s " @ $Spell::name[$Spell::index[%weapon]];
						else
							%hitby = Client::getName(%shooterClient);
					}

					// Default damageMode to true if not set (for backwards compatibility)
					%shooterDamageMode = fetchData(%shooterClient, "damageMode");
					if(%shooterDamageMode == "")
						%shooterDamageMode = true;
					%damagedDamageMode = fetchData(%damagedClient, "damageMode");
					if(%damagedDamageMode == "")
						%damagedDamageMode = true;
					
					if(%shooterDamageMode)
					{
						%msg = "<jl>You try to hit " @ Client::getName(%damagedClient) @ ", but miss! (LCK)";
						DisplayDamageMessage(%shooterClient, %msg, %msgcolor, "attacker");
						
						// REMOVED: ATKText for LCK miss is now handled by DisplayDamageMessage above
						// This block was sending a second floating message ("Your attack missed X!")
						// on top of DisplayDamageMessage's ("You try to hit X, but miss! (LCK)")
						// if(!Player::isAiControlled(%shooterClient) && %shooterClient != "" && %shooterClient != -1 && %shooterClient != 0)
						if(false) // DISABLED
						{
							// Validate client is connected
							%clientName = Client::getName(%shooterClient);
							if(%clientName != "" && %clientName != -1)
							{
								%displayType = fetchData(%shooterClient, "damageDisplayType");
								%floatingEnabled = fetchData(%shooterClient, "floatingDamageNumbers");
								// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
								if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
								{
									// Get animation style preference
									%animationStyle = fetchData(%shooterClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "attacker" = player dealing damage (red)
									%enemyName = Client::getName(%damagedClient);
									remoteEval(%shooterClient, "ATKText", "Your attack missed " @ %enemyName @ "!", %animationStyle, "attacker");
								}
							}
						}
					}
					if(%damagedDamageMode)
					{
						%msg = "<jr>" @ %hitby @ " tries to hit you, but misses! (LCK)";
						DisplayDamageMessage(%damagedClient, %msg, %msgcolor, "defender");
						
						// REMOVED: ATKText for LCK miss is now handled by DisplayDamageMessage above
						// This block was sending a second floating message ("X's attack missed you!")
						// on top of DisplayDamageMessage's ("X tries to hit you, but misses! (LCK)")
						// if(!Player::isAiControlled(%damagedClient) && %damagedClient != "" && %damagedClient != -1 && %damagedClient != 0)
						if(false) // DISABLED
						{
							// Validate client is connected
							%clientName = Client::getName(%damagedClient);
							if(%clientName != "" && %clientName != -1)
							{
								%displayType = fetchData(%damagedClient, "damageDisplayType");
								%floatingEnabled = fetchData(%damagedClient, "floatingDamageNumbers");
								// Enable if damageDisplayType is "floating" OR floatingDamageNumbers is enabled
								if(%displayType == "floating" || %floatingEnabled == "" || %floatingEnabled == "1" || %floatingEnabled == "true")
								{
									// Get animation style preference
									%animationStyle = fetchData(%damagedClient, "floatingAnimationStyle");
									if(%animationStyle == "")
										%animationStyle = "float"; // Default style (changed from redmoon)
									// Convert old styles to new ones
									if(%animationStyle == "redmoon" || %animationStyle == "wow")
										%animationStyle = "float";
									// Pass view type: "defender" = player taking damage (current color)
									%attackerName = Client::getName(%shooterClient);
									remoteEval(%damagedClient, "ATKText", %attackerName @ "'s attack missed you!", %animationStyle, "defender");
								}
							}
						}
					}
				}

				//-------------------------------------------
				//add entry to damagedClient's damagedBy list
				//-------------------------------------------

				//make new entry with shooter's name
				// CRITICAL: Include LCK misses (%lckMiss) in $damagedBy so players get EXP credit
				// Even though LCK protection prevented the damage, the player still "hit" the enemy
				// and should get credit for the kill if the enemy dies from other damage
				if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Check Reg Block: Shooter=" @ %shooterClient @ " IsMiss=" @ %isMiss @ " Value=" @ %value @ " TargetDead=" @ %targetIsDead @ " ExpDist=" @ fetchData(%damagedClient, "ExpDistributed"));
				if( %shooterClient != 0 && !%isMiss)
				{
					// CRITICAL: Don't add damage to $damagedBy if the target is already dead
					// Damage events can arrive after death, and adding them would be pointless
					// Check if the target is dead or if EXP was already distributed
					%targetIsDead = Player::IsDead(%this);
					%expAlreadyDistributed = fetchData(%damagedClient, "ExpDistributed");
					%expDistributed = (%expAlreadyDistributed != "" && %expAlreadyDistributed != "0" && %expAlreadyDistributed != -1);
					
					if(%targetIsDead || %expDistributed)
					{
						// Target is dead or EXP already distributed - skip adding to $damagedBy
						// This prevents late-arriving damage events from being tracked after death
						if($DamageDebugEnabled) echo("[DAMAGE DEBUG] REJECTED: ExpAlreadyDistributed=" @ %expDistributed @ " Dead=" @ %targetIsDead @ " for " @ %damagedClient);
					}
					else
					{
					if(%shooterClient == 0)
						%sname = "A powerful force";
					else
						%sname = Client::getName(%shooterClient);

					// CRITICAL: Use BotInfoAiName for enemy bots, Client::getName() for regular players
					// This ensures $damagedBy is keyed by the correct name for EXP distribution
					%botInfoAiName = fetchData(%damagedClient, "BotInfoAiName");
					if(%botInfoAiName != "" && %botInfoAiName != "0" && %botInfoAiName != -1)
						%dname = %botInfoAiName;
					else
						%dname = Client::getName(%damagedClient);
					
					if(%shooterClient != %damagedClient)
					{
						// First, check if this shooter already has an entry in $damagedBy
						// If they do, add the damage to their existing entry instead of creating a new one
						// This makes better use of the limited array space
						%existingIndex = "";
						for(%i = 1; %i <= $maxDamagedBy; %i++)
						{
							%entry = $damagedBy[%dname, %i];
							if(%entry != "")
							{
								%entryShooter = GetWord(%entry, 0);
								if(%entryShooter == %sname)
								{
									%existingIndex = %i;
									break;
								}
							}
						}
						
						if(%existingIndex != "")
						{
							// Shooter already has an entry - add damage to existing entry
							%existingEntry = $damagedBy[%dname, %existingIndex];
							%existingDamage = GetWord(%existingEntry, 1);
							
							// CRITICAL: Validate existing damage value - if invalid, use 0
							%existingDamageNum = %existingDamage * 1;
							if(%existingDamageNum == "" || %existingDamageNum == -1)
								%existingDamageNum = 0;
							
							%newDamage = %existingDamageNum + %backupValue;
							$damagedBy[%dname, %existingIndex] = %sname @ " " @ %newDamage;
							
							if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Updated " @ %sname @ " damage on " @ %dname @ " to " @ %newDamage @ " (Index: " @ %existingIndex @ ")");

							// Token-gated erase (see DamagedByErase): old schedules can't be
							// cancelled, so each write stamps the entry and stale erases no-op.
							// A blind clear here was wiping entries for recycled bot names.
							$DamagedBySeq++;
							$damagedByStamp[%dname, %existingIndex] = $DamagedBySeq;
							schedule("DamagedByErase(\"" @ %dname @ "\", " @ %existingIndex @ ", " @ $DamagedBySeq @ ");", $damagedByEraseDelay);
						}
						else
						{
							// Shooter doesn't have an entry yet - create a new one
							%index = "";
							for(%i = 1; %i <= $maxDamagedBy; %i++)
							{
								if($damagedBy[%dname, %i] == "" && %index == "")
									%index = %i;
							}
							if(%index != "")
							{
								$damagedBy[%dname, %index] = %sname @ " " @ %backupValue;
								if($DamageDebugEnabled) echo("[DAMAGE DEBUG] Added " @ %sname @ " damage on " @ %dname @ ": " @ %backupValue @ " (Index: " @ %index @ ")");
								// Token-gated erase (see DamagedByErase) - stale schedules no-op
								$DamagedBySeq++;
								$damagedByStamp[%dname, %index] = $DamagedBySeq;
								schedule("DamagedByErase(\"" @ %dname @ "\", " @ %index @ ", " @ $DamagedBySeq @ ");", $damagedByEraseDelay);
							}
							else
							{
								//too many hits on waiting list, he doesn't get in on exp.
								if($DamageDebugEnabled) echo("[DAMAGE DEBUG] FAILED to add " @ %sname @ " damage on " @ %dname @ " - List Full!");
							}
						}
					}
					}
				}

				if(fetchData(%damagedClient, "flashMode"))
				{
					%flash = Player::getDamageFlash(%this) + %value * 2;
					if(%flash > 0.75)
						%flash = 0.75;
					Player::setDamageFlash(%this,%flash);
				}

				//If player not dead then play a random hurt sound
				if(!Player::IsDead(%this))
				{
					if(%damagedClient.lastDamage < getSimTime())
					{
						%sound = radnomItems(3,injure1,injure2,injure3);
						playVoice(%damagedClient,%sound);
						%damagedClient.lastdamage = getSimTime() + 1.5;
					}
				}
				else		//player died
				{
					if(isRPGAI(%shooterClient))
					{
						RemotePlayAnim(%shooterClient, 8);
						PlaySound(RandomRaceSound(fetchData(%shooterClient, "RACE"), Taunt), %shooterClientPos);
					}

					if( Player::isCrouching(%this) )
					%curDie = $PlayerAnim::Crouching;
					else
					%curDie = radnomItems(3, $PlayerAnim::DieLeftSide, $PlayerAnim::DieChest, $PlayerAnim::DieForwardKneel);

					Player::setAnimation(%this, %curDie);

					if(%type == $ImpactDamageType && %object.clLastMount != "")
					%shooterClient = %object.clLastMount;

					// SOUL REAVER - killing blow banks a soul (up to max)
					if(%weaponEffect == "SOUL_HARVEST" && %shooterClient != %damagedClient)
					{
						%maxSouls = $WeaponEffectMaxSouls[%weapon];
						if(%maxSouls == "" || %maxSouls == -1) %maxSouls = 10;
						%souls = $SoulStacks[%shooterClient];
						if(%souls == "" || %souls == -1) %souls = 0;
						if(%souls < %maxSouls)
						{
							%souls++;
							$SoulStacks[%shooterClient] = %souls;
							if(%souls == %maxSouls)
								Client::sendMessage(%shooterClient, $MsgRed, "SOUL HARVESTED! (" @ %souls @ "/" @ %maxSouls @ ") Souls at maximum - your next strike unleashes SOUL NOVA!");
							else
								Client::sendMessage(%shooterClient, $MsgYellow, "SOUL HARVESTED! (" @ %souls @ "/" @ %maxSouls @ ")");
						}
					}

					// Death wipes the victim's mythic weapon state (souls are lost on
					// death by design; also prevents stale data on recycled client IDs)
					$SoulStacks[%damagedClient] = 0;
					$EchoCombo[%damagedClient] = 0;
					// Reset this shooter's per-target hit counter too, so a recycled
					// clientId doesn't inherit the count and trigger every-Nth-hit
					// effects (Sky Render / Storm Caller) off-schedule. Best-effort:
					// other shooters' counters vs this target expire naturally.
					if(%weapon != "")
						$WeaponHitCount[%shooterClient, %weapon, %damagedClient] = "";

					Client::onKilled(%damagedClient, %shooterClient, %type);
				}  // closes else (player died)
			}  // closes if(%value)

			if(%isMiss)
			{
				if(fetchData(%damagedClient, "isBonused"))
				{
					GameBase::activateShield(%this, "0 0 1.57", 1.47);
					PlaySound(SoundHitShield, %damagedClientPos);
				}
			}  // closes if(%isMiss)
		}  // closes if(!IsDead(%this))
	}  // closes function Player::onDamage
// =================================================================
// MYTHIC WEAPON HELPERS (Remort 125 Tier)
// =================================================================

// Shared protection check for mythic weapon effects that do more than damage
// (launches, etc). Mirrors Final Verdict's protections: town bots, same-team
// players (outside duels/hitlist), party members, bosses, seal battle bots,
// and colloseum arena bots are all protected.
function MythicWeapon::IsProtectedTarget(%shooterClient, %damagedClient)
{
	%targetName = Client::getName(%damagedClient);
	%spawnBotInfo = fetchData(%damagedClient, "SpawnBotInfo");

	// Protection 1: Town bots
	if(isTownBot(%damagedClient))
		return true;

	// Protection 2: Same team players (unless in duel or on hit list)
	%shooterTeam = GameBase::getTeam(%shooterClient);
	%targetTeam = GameBase::getTeam(%damagedClient);
	if(%shooterTeam == %targetTeam && !IsEnemyBot(%damagedClient))
	{
		%inDuel = (fetchData(%shooterClient, "DuelTarget") == %damagedClient);
		%onHitList = (String::findSubStr(fetchData(%shooterClient, "Hitlist"), Client::getName(%damagedClient)) != -1);
		if(!%inDuel && !%onHitList)
			return true;
	}

	// Protection 3: Party members
	%shooterParty = fetchData(%shooterClient, "PARTY");
	%targetParty = fetchData(%damagedClient, "PARTY");
	if(%shooterParty != "" && %shooterParty != -1 && %shooterParty == %targetParty)
		return true;

	// Protection 4: Bosses
	if(String::findSubStr(%targetName, "Boss") != -1 ||
	   String::findSubStr(%targetName, "King") != -1 ||
	   String::findSubStr(%targetName, "Queen") != -1 ||
	   String::findSubStr(%spawnBotInfo, "Boss") != -1)
		return true;

	// Protection 5: Seal Battle bots
	%isSealBot = fetchData(%damagedClient, "SealBattleBot");
	if(%isSealBot == "true" || %isSealBot == "True" || %isSealBot == "1")
		return true;
	if(String::findSubStr(%targetName, "SealFighter") == 0 ||
	   String::findSubStr(%targetName, "SealMage") == 0 ||
	   String::findSubStr(%targetName, "SealGuardian") == 0)
		return true;

	// Protection 6: Colloseum arena bots
	%targetZoneId = fetchData(%damagedClient, "zone");
	if(%targetZoneId == "" || %targetZoneId == -1 || %targetZoneId == "0")
	{
		%targetObj = Client::getOwnedObject(%damagedClient);
		if(%targetObj != -1 && %targetObj != "")
			%targetZoneId = ObjectInWhichZone(%targetObj);
	}
	if(%targetZoneId != "" && %targetZoneId != -1 && %targetZoneId != "0")
	{
		%targetZoneDesc = Zone::getDesc(%targetZoneId);
		if(String::ICompare(%targetZoneDesc, "Colloseum") == 0)
			return true;
	}

	return false;
}

// SKY RENDER delayed impale - fires 1.5s after the launch, hitting the target
// where they are (usually mid-fall or on landing). %objAtLaunch is the player
// object at launch time so a respawn/disconnect in the meantime cancels it.
function SkyRender::Impale(%shooterClient, %damagedClient, %objAtLaunch)
{
	%targetObj = Client::getOwnedObject(%damagedClient);
	if(%targetObj == -1 || %targetObj == "" || %targetObj != %objAtLaunch)
		return;
	if(IsDead(%damagedClient))
		return;

	%targetPos = GameBase::getPosition(%targetObj);
	if(%targetPos == "" || %targetPos == -1)
		return;

	// Impale damage scales with Piercing skill, reduced by MDEF
	%impaleDmg = 600;
	%piercingSkill = $PlayerSkill[%shooterClient, $SkillPiercing];
	if(%piercingSkill == "" || %piercingSkill == -1) %piercingSkill = 1000;
	%impaleDmg = round((%impaleDmg * %piercingSkill) / 1000);

	%targetMDEF = fetchData(%damagedClient, "MDEF");
	if(%targetMDEF == "" || %targetMDEF == -1) %targetMDEF = 0;
	%mdefReduction = (getRandom() * (%targetMDEF / 10)) + 1;
	%impaleDmg = floor(Cap(%impaleDmg - %mdefReduction, 1, "inf"));

	CreateAndDetBomb_VisualOnly(%shooterClient, "Bomb20", %targetPos, -1);
	playSound(shockExplosion, %targetPos);
	refreshHP(%damagedClient, %impaleDmg / $TribesDamageToNumericDamage);

	Client::sendMessage(%shooterClient, $MsgYellow, "SKYFALL IMPALE! +" @ %impaleDmg @ " bonus damage!");
	Client::sendMessage(%damagedClient, $MsgRed, "SKYFALL IMPALE! The trident strikes you from above for " @ %impaleDmg @ " damage!");
}

// ECHO FANG delayed echo - resolves the pooled echo damage for one
// shooter/target pair. Hits landed while an echo is pending pool into it, so
// each pair has at most one schedule in flight. %objAtHit is the target's
// player object at hit time so a respawn/disconnect discards the pool.
function EchoFang::Echo(%shooterClient, %damagedClient, %objAtHit)
{
	$EchoPending[%shooterClient, %damagedClient] = "";
	%pool = $EchoPool[%shooterClient, %damagedClient];
	$EchoPool[%shooterClient, %damagedClient] = 0;

	if(%pool == "" || %pool == -1 || %pool <= 0)
		return;

	%targetObj = Client::getOwnedObject(%damagedClient);
	if(%targetObj == -1 || %targetObj == "" || %targetObj != %objAtHit)
		return;
	if(IsDead(%damagedClient))
		return;

	%echoDisplay = round(%pool * $TribesDamageToNumericDamage);
	if(%echoDisplay < 1)
		return;

	%targetPos = GameBase::getPosition(%targetObj);
	refreshHP(%damagedClient, %pool);
	if(%targetPos != "" && %targetPos != -1)
		playSound(SoundHitShield, %targetPos);

	Client::sendMessage(%shooterClient, $MsgYellow, "ECHO! Your wounds reopen for " @ %echoDisplay @ " delayed damage!");
	Client::sendMessage(%damagedClient, $MsgRed, "ECHO! Your wounds reopen for " @ %echoDisplay @ " damage!");
}

function remoteKill(%clientId)
{
	dbecho($dbechoMode, "remoteKill(" @ %clientId @ ")");

	if(!$matchStarted)
		return;
	if(%clientId.isJailed || Duel::isDueling(%clientId))
		return;
	
	%player = Client::getOwnedObject(%clientId);
	if(%player != -1 && getObjectType(%player) == "Player" && !IsDead(%clientId))
	{
		storeData(%clientId, "LCK", 1, "dec");

		if(fetchData(%clientId, "LCK") >= 0)
			Client::sendMessage(%clientId, $MsgRed, "You have permanently lost an LCK point!");

		playNextAnim(%clientId);
		Player::kill(%clientId);
	}
}
