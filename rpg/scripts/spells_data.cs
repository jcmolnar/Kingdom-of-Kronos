//============================================================================
// spells_data.cs — split from spells.cs (original lines 1-1313)
// Extracted 2026-07-17 at commit 4aa0a3d. Mechanical text move, no behavior change.
// exec'd by spells.cs (the shell) — do NOT add to Server.cs.
// constants + $Spell::* tables + casting helpers
// ==== BEGIN ORIGINAL PAYLOAD ====
//Data for spells are defined in the SPELL DEFINITIONS section.  Unfortunately, not everything can be designed there
//(ie, special effects etc) so for the great majority, the spells are coded in the DoCastSpell function.  This method
//isn't the ideal one... maybe one day i'll fix that if the need comes along.

//-- SPELL TYPES (not in use due to hardcoding of spells...) -----------------------------------------------------

$SelfType = 1;				//casts only to self
$LOSType = 2;				//casts only at LOS

$SelfRadiusType = 3;			//casts to self and around self
$LOSRadiusType = 4;			//casts at LOS and around LOS

$SelfRadiusLOSType = 5;			//casts to self and around self and at LOS
$LOSRadiusSelfType = 6;			//casts at LOS and around LOS and to self

$SelfRadiusLOSRadiusType = 7;		//casts to self and around self and to LOS and around LOS

//-- SPELL DEFINITIONS -------------------------------------------------------------------------------------------
$SPELL_DEBUG = 0;
$SpellExplosionMode = "rocket"; // "hybrid" or "rocket"

function SpellExplosion_UseHybrid()
{
	%mode = $SpellExplosionMode;
	if(%mode == "" || %mode == "hybrid" || %mode == "HYBRID")
		return true;
	return false;
}

// Ring/shockwave explosions (faceCamera = false in their ExplosionData) orient
// along the explosion axis. Rocket timeout explosions hardcode a HORIZONTAL
// axis (0,1,0) in the client engine (RocketDumb::readExplosion), which rotates
// the rings 90 degrees. Mine explosions use the mine's up vector (0,0,1), so
// ring bombs must keep using the Mine path to stay flat.
$SpellRingBomb[Bomb4] = true;	// Shockwave
$SpellRingBomb[Bomb5] = true;	// LargeShockwave (Tornado rings)
$SpellRingBomb[Bomb19] = true;	// WhiteShockwave
$SpellRingBomb[Bomb22] = true;	// IonShockwave
$SpellRingBomb[Bomb24] = true;	// PBShockWave
$SpellRingBomb[Bomb26] = true;	// TimeShockwave

// Per-spell damage-buffer flush window (seconds). SpellDamage_Buffered accumulates
// all damage to a target within this window and applies it in ONE Player::onDamage
// call. High-density AOE spells fire damage waves every ~0.2-0.3s, so a wider window
// coalesces many waves into a single onDamage execution - the total damage, the
// number of explosions, and the visuals are all unchanged; only the number of times
// the expensive stat/skill/message pipeline runs goes down. Normal spells keep the
// default 0.2s (set in SpellDamage_Buffered) for combat responsiveness.
//   50 Ion Blast / 48 Apocalypse: 21 damage waves over ~4s -> ~5 onDamage calls (was ~21)
//   46 Tornado: 37 waves over ~11s   51 Shredder / 66 Terminate: multi-wave
$Spell::damageBufferWindow[50] = 0.25;	// Ion Blast
$Spell::damageBufferWindow[48] = 1.0;	// Apocalypse
$Spell::damageBufferWindow[46] = 0.6;	// Tornado
$Spell::damageBufferWindow[51] = 0.6;	// Shredder
$Spell::damageBufferWindow[66] = 0.6;	// Terminate

function SetSpellExplosionMode(%mode)
{
	if(%mode == "hybrid" || %mode == "HYBRID")
		$SpellExplosionMode = "hybrid";
	else if(%mode == "rocket" || %mode == "ROCKET")
		$SpellExplosionMode = "rocket";
	else
	{
		echo("SetSpellExplosionMode: Invalid mode '" @ %mode @ "'. Use 'hybrid' or 'rocket'.");
		return;
	}

	echo("SetSpellExplosionMode: Using " @ $SpellExplosionMode @ " mode.");
}

function BuildSpellBombTransform(%sourceObj, %castPos)
{
	if(%castPos == "" || %castPos == -1)
		return "";
	
	%mt = "";
	if(%sourceObj != -1 && %sourceObj != "" && isObject(%sourceObj))
		%mt = GameBase::getMuzzleTransform(%sourceObj);
	
	// Fallback to identity basis if muzzle transform is unavailable.
	if(%mt == "" || %mt == -1)
		return "1 0 0 0 1 0 0 0 1 " @ %castPos;
	
	return getWord(%mt, 0) @ " " @ getWord(%mt, 1) @ " " @ getWord(%mt, 2) @ " " @
		getWord(%mt, 3) @ " " @ getWord(%mt, 4) @ " " @ getWord(%mt, 5) @ " " @
		getWord(%mt, 6) @ " " @ getWord(%mt, 7) @ " " @ getWord(%mt, 8) @ " " @ %castPos;
}

function SpellResolveSourceObject(%clientId)
{
	%sourceObj = -1;
	
	if(%clientId != 0 && %clientId != -1)
	{
		%candidate = Client::getOwnedObject(%clientId);
		if(%candidate != -1 && %candidate != "" && isObject(%candidate) && getObjectType(%candidate) == "Player")
			%sourceObj = %candidate;
	}
	
	if(%sourceObj == -1)
	{
		%candidate = Client::getOwnedObject(2048);
		if(%candidate != -1 && %candidate != "" && isObject(%candidate) && getObjectType(%candidate) == "Player")
			%sourceObj = %candidate;
	}
	
	if(%sourceObj == -1)
	{
		%list = GetEveryoneIdList();
		for(%i = 0; (%id = GetWord(%list, %i)) != -1; %i++)
		{
			%candidate = Client::getOwnedObject(%id);
			if(%candidate != -1 && %candidate != "" && isObject(%candidate) && getObjectType(%candidate) == "Player")
			{
				%sourceObj = %candidate;
				break;
			}
		}
	}
	
	return %sourceObj;
}

// Tornado visuals are authored with a different forward/up axis.
// Use a fixed 90-degree X-axis basis so the effect stays upright.
function BuildTornadoBombTransform(%castPos)
{
	if(%castPos == "" || %castPos == -1)
		return "";
	
	// Rotation basis = Rx(90deg): [1 0 0; 0 0 -1; 0 1 0]
	return "1 0 0 0 0 -1 0 1 0 " @ %castPos;
}

function PowerCloud_ClearCache(%clientId, %token)
{
	if($PowerCloudCacheToken[%clientId] != %token)
		return;
	
	SpellTargetCache_Clear(%clientId);
	$PowerCloudCacheToken[%clientId] = "";
}

function PowerCloud_NextToken(%clientId)
{
	if($PowerCloudTokenGen[%clientId] == "" || $PowerCloudTokenGen[%clientId] == -1)
		$PowerCloudTokenGen[%clientId] = 0;
	$PowerCloudTokenGen[%clientId]++;
	return $PowerCloudTokenGen[%clientId];
}

function SpellCast_NextToken(%clientId)
{
	if($SpellCastToken[%clientId] == "" || $SpellCastToken[%clientId] == -1)
		$SpellCastToken[%clientId] = 0;
	$SpellCastToken[%clientId]++;
	return $SpellCastToken[%clientId];
}

// Reclaims a per-cast caster record once all of that cast's detonations have
// fired (scheduled ~30s out, far beyond the ~1.5s max detonation window).
function SpellCast_ClearCaster(%clientId, %castToken)
{
	$SpellCasterObj[%clientId, %castToken] = "";
}

function SpellCast_IsValid(%clientId, %expectedCasterName, %castToken, %context)
{
	// Authoritative check (token-bearing casts): OBJECT IDENTITY.
	// At cast time we recorded, KEYED BY CAST TOKEN, the exact player object the
	// caster controlled. A delayed detonation is only valid if that same object
	// is STILL the one this client owns. Immune to clientId reuse (a recycled bot
	// owns a DIFFERENT object) and to respawn (a new object). We do NOT supersede
	// earlier casts by the same live bot: one that recasts before its prior bombs
	// land is legitimate, and each cast validates against its OWN captured object.
	// %expectedCasterName is now vestigial (kept only so schedule strings resolve).
	if(%castToken != "" && %castToken != -1)
	{
		%origObj = $SpellCasterObj[%clientId, %castToken];
		%curObj  = Client::getOwnedObject(%clientId);
		if(%origObj == "" || %origObj == -1 || !isObject(%origObj) || %curObj != %origObj || getObjectType(%curObj) != "Player")
		{
			echo("[SPELL SAFETY] BLOCKED orphan " @ %context @ " - clientId " @ %clientId @ " token " @ %castToken @ " caster object changed (cast=" @ %origObj @ ", now=" @ %curObj @ ")");
			return false;
		}

		// Identity confirmed; still honor the dead-caster gameplay rule.
		if(IsInGraveyard(%clientId) || IsInGraveyard(Client::getName(%clientId)))
		{
			echo("[SPELL SAFETY] BLOCKED dead caster " @ %context @ " - clientId " @ %clientId);
			return false;
		}

		return true;
	}

	// Every live caller passes a real cast token, so the block above always
	// returns. A missing token means an unscheduled/immediate call where the
	// caster is necessarily still valid this frame — allow it.
	return true;
}

$Spell::keyword[1] = "firebomb";
$Spell::index[firebomb] = 1;
$Spell::name[1] = "Fire Bomb From Hell";
$Spell::description[1] = "Casts an explosive.";
$Spell::delay[1] = 1;
$Spell::recoveryTime[1] = 2;
$Spell::radius[1] = 10;
$Spell::damageValue[1] = "55";
$Spell::LOSrange[1] = 80;
$Spell::manaCost[1] = 6;
$Spell::startSound[1] = ActivateBF;
$Spell::endSound[1] = ExplodeLM;
$Spell::groupListCheck[1] = False;
$Spell::refVal[1] = 55;
$Spell::graceDistance[1] = 2;
$SkillType[firebomb] = $SkillOffensiveCasting;

$Spell::keyword[2] = "teleport";
$Spell::index[teleport] = 2;
$Spell::name[2] = "Teleport close to nearest zone";
$Spell::description[2] = "Teleports you near a zone";
$Spell::delay[2] = 3.5;
$Spell::recoveryTime[2] = 16.5;
$Spell::manaCost[2] = 8;
$Spell::startSound[2] = Portal11;
$Spell::endSound[2] = ActivateCH;
$Spell::groupListCheck[2] = False;
$Spell::refVal[2] = 0;
$Spell::graceDistance[2] = 2;
$SkillType[teleport] = $SkillNeutralCasting;

$Spell::keyword[3] = "transport";
$Spell::index[transport] = 3;
$Spell::name[3] = "Transport to zone";
$Spell::description[3] = "Transports to a specific zone";
$Spell::delay[3] = 4.0;
$Spell::recoveryTime[3] = 1;
$Spell::manaCost[3] = 12;
$Spell::startSound[3] = RespawnB;
$Spell::endSound[3] = ActivateCH;
$Spell::groupListCheck[3] = False;
$Spell::refVal[3] = 0;
$Spell::graceDistance[3] = 2;
$SkillType[transport] = $SkillNeutralCasting;

$Spell::keyword[4] = "advtransport";
$Spell::index[advtransport] = 4;
$Spell::name[4] = "Advanced Transport to zone";
$Spell::description[4] = "Transports self OR person in line-of-sight to a specific zone";
$Spell::delay[4] = 4.0;
$Spell::recoveryTime[4] = 27;
$Spell::LOSrange[4] = 500;
$Spell::manaCost[4] = 16;
$Spell::startSound[4] = RespawnB;
$Spell::endSound[4] = ActivateCH;
$Spell::groupListCheck[4] = True;
$Spell::refVal[4] = 0;
$Spell::graceDistance[4] = 2;
$SkillType[advtransport] = $SkillNeutralCasting;

$Spell::keyword[5] = "cloud";
$Spell::index[cloud] = 5;
$Spell::name[5] = "Cloud Attack";
$Spell::description[5] = "Casts an explosive.";
$Spell::delay[5] = 1;
$Spell::recoveryTime[5] = 3;
$Spell::radius[5] = 10;
$Spell::damageValue[5] = "85";
$Spell::LOSrange[5] = 80;
$Spell::manaCost[5] = 11;
$Spell::startSound[5] = ActivateBF;
$Spell::endSound[5] = ExplodeLM;
$Spell::groupListCheck[5] = False;
$Spell::refVal[5] = 85;
$Spell::graceDistance[5] = 2;
$SkillType[cloud] = $SkillOffensiveCasting;

$Spell::keyword[6] = "melt";
$Spell::index[melt] = 6;
$Spell::name[6] = "Melt Bomb Attack";
$Spell::description[6] = "Casts an explosive.";
$Spell::delay[6] = 1;
$Spell::recoveryTime[6] = 3;
$Spell::radius[6] = 10;
$Spell::damageValue[6] = "140";
$Spell::LOSrange[6] = 80;
$Spell::manaCost[6] = 15;
$Spell::startSound[6] = ActivateBF;
$Spell::endSound[6] = ExplodeLM;
$Spell::groupListCheck[6] = False;
$Spell::refVal[6] = 140;
$Spell::graceDistance[6] = 2;
$SkillType[melt] = $SkillOffensiveCasting;

$Spell::keyword[7] = "powercloud";
$Spell::index[powercloud] = 7;
$Spell::name[7] = "Power Cloud Attack";
$Spell::description[7] = "Casts three explosives.";
$Spell::delay[7] = 1;
$Spell::recoveryTime[7] = 3;
$Spell::radius[7] = 10;
$Spell::damageValue[7] = "70";
$Spell::LOSrange[7] = 80;
$Spell::manaCost[7] = 23;
$Spell::startSound[7] = ActivateBF;
$Spell::endSound[7] = ExplodeLM;
$Spell::groupListCheck[7] = False;
$Spell::refVal[7] = 210;
$Spell::graceDistance[7] = 2;
$SkillType[powercloud] = $SkillOffensiveCasting;

$Spell::keyword[8] = "heal";
$Spell::index[heal] = 8;
$Spell::name[8] = "Heal Self";
$Spell::description[8] = "Heals the caster.";
$Spell::delay[8] = 1.5;
$Spell::recoveryTime[8] = 2.25;
$Spell::damageValue[8] = -6;
$Spell::manaCost[8] = 2;
$Spell::startSound[8] = DeActivateWA;
$Spell::endSound[8] = ActivateAR;
$Spell::groupListCheck[8] = False;
$Spell::refVal[8] = -6;
$Spell::graceDistance[8] = 2;
$SkillType[heal] = $SkillDefensiveCasting;

$Spell::keyword[9] = "advheal1";
$Spell::index[advheal1] = 9;
$Spell::name[9] = "Heal Self or Other (1st)";
$Spell::description[9] = "Heals the caster or someone in the LOS.";
$Spell::delay[9] = 1.5;
$Spell::recoveryTime[9] = 3.25;
$Spell::damageValue[9] = -10;
$Spell::LOSrange[9] = 80;
$Spell::manaCost[9] = 3;
$Spell::startSound[9] = DeActivateWA;
$Spell::endSound[9] = ActivateAR;
$Spell::groupListCheck[9] = False;
$Spell::refVal[9] = -10;
$Spell::graceDistance[9] = 2;
$SkillType[advheal1] = $SkillDefensiveCasting;

$Spell::keyword[10] = "advheal2";
$Spell::index[advheal2] = 10;
$Spell::name[10] = "Heal Self Or Other (2nd)";
$Spell::description[10] = "Heals the caster or someone in the LOS.";
$Spell::delay[10] = 1.5;
$Spell::recoveryTime[10] = 4.0;
$Spell::damageValue[10] = -15;
$Spell::LOSrange[10] = 80;
$Spell::manaCost[10] = 4;
$Spell::startSound[10] = DeActivateWA;
$Spell::endSound[10] = ActivateAR;
$Spell::groupListCheck[10] = False;
$Spell::refVal[10] = -15;
$Spell::graceDistance[10] = 2;
$SkillType[advheal2] = $SkillDefensiveCasting;

$Spell::keyword[11] = "godlyheal";
$Spell::index[godlyheal] = 11;
$Spell::name[11] = "Godly Heal Self Or Other";
$Spell::description[11] = "Heals the caster or someone in the LOS.";
$Spell::delay[11] = 1.5;
$Spell::recoveryTime[11] = 6;
$Spell::damageValue[11] = -80;
$Spell::LOSrange[11] = 80;
$Spell::manaCost[11] = 15;
$Spell::startSound[11] = DeActivateWA;
$Spell::endSound[11] = ActivateAR;
$Spell::groupListCheck[11] = False;
$Spell::refVal[11] = -80;
$Spell::graceDistance[11] = 2;
$SkillType[godlyheal] = $SkillDefensiveCasting;

$Spell::keyword[12] = "beam";
$Spell::index[beam] = 12;
$Spell::name[12] = "Beam";
$Spell::description[12] = "Light gathers into a concentrated beam and causes intense damage to the target.";
$Spell::delay[12] = 0;
$Spell::recoveryTime[12] = 7;
$Spell::damageValue[12] = "180";
$Spell::LOSrange[12] = 1000;
$Spell::manaCost[12] = 35;
$Spell::startSound[12] = HitLevelDT;
$Spell::endSound[12] = HitBF;
$Spell::groupListCheck[12] = False;
$Spell::refVal[12] = 180;
$Spell::graceDistance[12] = 5;
$SkillType[beam] = $SkillOffensiveCasting;

$Spell::keyword[13] = "thorn";
$Spell::index[thorn] = 13;
$Spell::name[13] = "Thorn";
$Spell::description[13] = "Casts thorn.";
$Spell::delay[13] = 0.1;
$Spell::recoveryTime[13] = 0.5;
$Spell::radius[13] = 6;
$Spell::damageValue[13] = "20";
$Spell::LOSrange[13] = 300;
$Spell::manaCost[13] = 1;
$Spell::startSound[13] = ActivateFK;
$Spell::endSound[13] = DeflectAS;
$Spell::groupListCheck[13] = False;
$Spell::refVal[13] = 20;
$Spell::graceDistance[13] = 5;
$SkillType[thorn] = $SkillOffensiveCasting;

$Spell::keyword[14] = "fireball";
$Spell::index[fireball] = 14;
$Spell::name[14] = "Fireball";
$Spell::description[14] = "Casts a fireball.";
$Spell::delay[14] = 1;
$Spell::recoveryTime[14] = 1;
$Spell::radius[14] = 8;
$Spell::damageValue[14] = "35";
$Spell::LOSrange[14] = 80;
$Spell::manaCost[14] = 3;
$Spell::startSound[14] = ActivateAB;
$Spell::endSound[14] = LaunchFB;
$Spell::groupListCheck[14] = False;
$Spell::refVal[14] = 35;
$Spell::graceDistance[14] = 2;
$SkillType[fireball] = $SkillOffensiveCasting;

$Spell::keyword[15] = "icespike";
$Spell::index[icespike] = 15;
$Spell::name[15] = "Icespike";
$Spell::description[15] = "Casts icespike.";
$Spell::delay[15] = 0.1;
$Spell::recoveryTime[15] = 1;
$Spell::radius[15] = 6;
$Spell::damageValue[15] = "28";
$Spell::LOSrange[15] = 80;
$Spell::manaCost[15] = 3;
$Spell::startSound[15] = ActivateFK;
$Spell::endSound[15] = HitPawnDT;
$Spell::groupListCheck[15] = False;
$Spell::refVal[15] = 28;
$Spell::graceDistance[15] = 5;
$SkillType[icespike] = $SkillOffensiveCasting;

$Spell::keyword[16] = "icestorm";
$Spell::index[icestorm] = 16;
$Spell::name[16] = "Icestorm";
$Spell::description[16] = "Casts icestorm.";
$Spell::delay[16] = 1;
$Spell::recoveryTime[16] = 2;
$Spell::radius[16] = 11;
$Spell::damageValue[16] = "45";
$Spell::LOSrange[16] = 80;
$Spell::manaCost[16] = 5;
$Spell::startSound[16] = ImpactTR;
$Spell::endSound[16] = Reflected;
$Spell::groupListCheck[16] = False;
$Spell::refVal[16] = 45;
$Spell::graceDistance[16] = 2;
$SkillType[icestorm] = $SkillOffensiveCasting;

$Spell::keyword[17] = "ironfist";
$Spell::index[ironfist] = 17;
$Spell::name[17] = "Ironfist";
$Spell::description[17] = "Casts ironfist.";
$Spell::delay[17] = 0.1;
$Spell::recoveryTime[17] = 6;
$Spell::radius[17] = 7;
$Spell::damageValue[17] = "128";
$Spell::LOSrange[17] = 80;
$Spell::manaCost[17] = 18;
$Spell::startSound[17] = UnravelAM;
$Spell::endSound[17] = NoSound;
$Spell::groupListCheck[17] = False;
$Spell::refVal[17] = 128;
$Spell::graceDistance[17] = 3;
$SkillType[ironfist] = $SkillOffensiveCasting;

$Spell::keyword[18] = "hellstorm";
$Spell::index[hellstorm] = 18;
$Spell::name[18] = "Hellstorm";
$Spell::description[18] = "Casts hellstorm.";
$Spell::delay[18] = 4;
$Spell::recoveryTime[18] = 10;
$Spell::radius[18] = 20;
$Spell::damageValue[18] = "265";
$Spell::LOSrange[18] = 80;
$Spell::manaCost[18] = 25;
$Spell::startSound[18] = LoopLS;
$Spell::endSound[18] = LaunchET;
$Spell::groupListCheck[18] = False;
$Spell::refVal[18] = 265;
$Spell::graceDistance[18] = 2;
$SkillType[hellstorm] = $SkillOffensiveCasting;

$Spell::keyword[19] = "dimensionrift";
$Spell::index[dimensionrift] = 19;
$Spell::name[19] = "Dimension Rift";
$Spell::description[19] = "Casts Dimension Rift.";
$Spell::delay[19] = 4;
$Spell::recoveryTime[19] = 8;
$Spell::radius[19] = 30;
$Spell::damageValue[19] = "320";
$Spell::LOSrange[19] = 80;
$Spell::manaCost[19] = 50;
$Spell::startSound[19] = LaunchLS;
$Spell::endSound[19] = Explode3FW;
$Spell::groupListCheck[19] = False;
$Spell::refVal[19] = 320;
$Spell::graceDistance[19] = 2;
$SkillType[dimensionrift] = $SkillOffensiveCasting;

$Spell::keyword[20] = "advheal3";
$Spell::index[advheal3] = 20;
$Spell::name[20] = "Heal Self Or Other (3rd)";
$Spell::description[20] = "Heals the caster or someone in the LOS.";
$Spell::delay[20] = 1.5;
$Spell::recoveryTime[20] = 4.75;
$Spell::damageValue[20] = -25;
$Spell::LOSrange[20] = 80;
$Spell::manaCost[20] = 5;
$Spell::startSound[20] = DeActivateWA;
$Spell::endSound[20] = ActivateAR;
$Spell::groupListCheck[20] = False;
$Spell::refVal[20] = -25;
$Spell::graceDistance[20] = 2;
$SkillType[advheal3] = $SkillDefensiveCasting;

$Spell::keyword[21] = "remort";
$Spell::index[remort] = 21;
$Spell::name[21] = "Remort";
$Spell::description[21] = "Remorts a level 100+(4*Remort Level) character to level 1, with bonuses.";
$Spell::delay[21] = 3.0;
$Spell::recoveryTime[21] = 1;
$Spell::damageValue[21] = 0;
$Spell::manaCost[21] = 1;
$Spell::startSound[21] = RespawnA;
$Spell::endSound[21] = RespawnC;
$Spell::groupListCheck[21] = False;
$Spell::refVal[21] = 0;
$Spell::graceDistance[21] = 2;
$SkillType[remort] = $SkillNeutralCasting;

$Spell::keyword[22] = "fullheal";
$Spell::index[fullheal] = 22;
$Spell::name[22] = "Full Heal Self";
$Spell::description[22] = "Fully heals the caster.";
$Spell::delay[22] = 1.5;
$Spell::recoveryTime[22] = 60;
$Spell::damageValue[22] = 0;
$Spell::manaCost[22] = 2;
$Spell::startSound[22] = DeActivateWA;
$Spell::endSound[22] = PlaceSeal;
$Spell::groupListCheck[22] = False;
$Spell::refVal[22] = -9998;
$Spell::graceDistance[22] = 2;
$SkillType[fullheal] = $SkillDefensiveCasting;

$Spell::keyword[23] = "massheal";
$Spell::index[massheal] = 23;
$Spell::name[23] = "Mass Heal";
$Spell::description[23] = "Heals caster and friendlies 10 meters around.";
$Spell::delay[23] = 1.5;
$Spell::recoveryTime[23] = 10;
$Spell::radius[23] = 10;
$Spell::damageValue[23] = -30;
$Spell::manaCost[23] = 12;
$Spell::startSound[23] = DeActivateWA;
$Spell::endSound[23] = ActivateAR;
$Spell::groupListCheck[23] = False;
$Spell::refVal[23] = -30;
$Spell::graceDistance[23] = 2;
$SkillType[massheal] = $SkillDefensiveCasting;

$Spell::keyword[24] = "massfullheal";
$Spell::index[massfullheal] = 24;
$Spell::name[24] = "Mass Full Heal";
$Spell::description[24] = "Fully Heals caster and friendlies 12 meters around.";
$Spell::delay[24] = 1.5;
$Spell::recoveryTime[24] = 150;
$Spell::radius[24] = 12;
$Spell::damageValue[24] = 0;
$Spell::manaCost[24] = 200;
$Spell::startSound[24] = DeActivateWA;
$Spell::endSound[24] = PlaceSeal;
$Spell::groupListCheck[24] = False;
$Spell::refVal[24] = -9999;
$Spell::graceDistance[24] = 2;
$SkillType[massfullheal] = $SkillDefensiveCasting;

$Spell::keyword[25] = "shield";
$Spell::index[shield] = 25;
$Spell::name[25] = "Shield Self";
$Spell::description[25] = "A magical shield adds 50 DEF to the caster.";
$Spell::delay[25] = 2.0;
$Spell::recoveryTime[25] = 8;
$Spell::damageValue[25] = "DEF 50";
$Spell::ticks[25] = 150;	//5 minutes
$Spell::manaCost[25] = 5;
$Spell::startSound[25] = ActivateTR;
$Spell::endSound[25] = ActivateTD;
$Spell::groupListCheck[25] = False;
$Spell::refVal[25] = -10;
$Spell::graceDistance[25] = 2;
$SkillType[shield] = $SkillDefensiveCasting;

$Spell::keyword[26] = "advshield1";
$Spell::index[advshield1] = 26;
$Spell::name[26] = "Shield Self Or Other (1st)";
$Spell::description[26] = "A magical shield that adds 80 DEF to the caster or target in LOS.";
$Spell::delay[26] = 2.0;
$Spell::recoveryTime[26] = 10;
$Spell::damageValue[26] = "DEF 80";
$Spell::ticks[26] = 165;	//5:30 minutes
$Spell::LOSrange[26] = 80;
$Spell::manaCost[26] = 8;
$Spell::startSound[26] = ActivateTR;
$Spell::endSound[26] = ActivateTD;
$Spell::groupListCheck[26] = False;
$Spell::refVal[26] = -11;
$Spell::graceDistance[26] = 2;
$SkillType[advshield1] = $SkillDefensiveCasting;

$Spell::keyword[27] = "advshield2";
$Spell::index[advshield2] = 27;
$Spell::name[27] = "Shield Self Or Other (2nd)";
$Spell::description[27] = "A magical shield that adds 70 DEF and 50 MDEF to the caster or target in LOS.";
$Spell::delay[27] = 2.0;
$Spell::recoveryTime[27] = 12;
$Spell::damageValue[27] = "DEF 70 MDEF 50";
$Spell::ticks[27] = 190;	//6:20 minutes
$Spell::LOSrange[27] = 80;
$Spell::manaCost[27] = 15;
$Spell::startSound[27] = ActivateTR;
$Spell::endSound[27] = ActivateTD;
$Spell::groupListCheck[27] = False;
$Spell::refVal[27] = -12;
$Spell::graceDistance[27] = 2;
$SkillType[advshield2] = $SkillDefensiveCasting;

$Spell::keyword[28] = "advshield3";
$Spell::index[advshield3] = 28;
$Spell::name[28] = "Shield Self Or Other (3rd)";
$Spell::description[28] = "A magical shield that adds 120 DEF and 80 MDEF to the caster or target in LOS.";
$Spell::delay[28] = 2.0;
$Spell::recoveryTime[28] = 14;
$Spell::damageValue[28] = "DEF 120 MDEF 80";
$Spell::ticks[28] = 218;	//7:16 minutes
$Spell::LOSrange[28] = 80;
$Spell::manaCost[28] = 18;
$Spell::startSound[28] = ActivateTR;
$Spell::endSound[28] = ActivateTD;
$Spell::groupListCheck[28] = False;
$Spell::refVal[28] = -13;
$Spell::graceDistance[28] = 2;
$SkillType[advshield3] = $SkillDefensiveCasting;

$Spell::keyword[29] = "advshield4";
$Spell::index[advshield4] = 29;
$Spell::name[29] = "Shield Self Or Other (4th)";
$Spell::description[29] = "A magical shield that adds 170 MDEF to the caster or target in LOS.";
$Spell::delay[29] = 2.0;
$Spell::recoveryTime[29] = 16;
$Spell::damageValue[29] = "MDEF 170";
$Spell::ticks[29] = 255;	//8:30 minutes
$Spell::LOSrange[29] = 80;
$Spell::manaCost[29] = 22;
$Spell::startSound[29] = ActivateTR;
$Spell::endSound[29] = ActivateTD;
$Spell::groupListCheck[29] = False;
$Spell::refVal[29] = -14;
$Spell::graceDistance[29] = 2;
$SkillType[advshield4] = $SkillDefensiveCasting;

$Spell::keyword[30] = "advshield5";
$Spell::index[advshield5] = 30;
$Spell::name[30] = "Shield Self Or Other (5th)";
$Spell::description[30] = "A magical shield that adds 150 DEF and 210 MDEF to the caster or target in LOS.";
$Spell::delay[30] = 2.0;
$Spell::recoveryTime[30] = 20;
$Spell::damageValue[30] = "DEF 150 MDEF 210";
$Spell::ticks[30] = 300;	//10 minutes
$Spell::LOSrange[30] = 80;
$Spell::manaCost[30] = 25;
$Spell::startSound[30] = ActivateTR;
$Spell::endSound[30] = ActivateTD;
$Spell::groupListCheck[30] = False;
$Spell::refVal[30] = -15;
$Spell::graceDistance[30] = 2;
$SkillType[advshield5] = $SkillDefensiveCasting;

$Spell::keyword[31] = "massshield";
$Spell::index[massshield] = 31;
$Spell::name[31] = "Mass Shield";
$Spell::description[31] = "A magical shield that adds 115 DEF and 105 MDEF to all friendlies within a 10 meter radius.";
$Spell::delay[31] = 2.0;
$Spell::recoveryTime[31] = 30;
$Spell::radius[31] = 10;
$Spell::damageValue[31] = "DEF 115 MDEF 105";
$Spell::ticks[31] = 270;	//9 minutes
$Spell::manaCost[31] = 20;
$Spell::startSound[31] = ActivateTR;
$Spell::endSound[31] = ActivateTD;
$Spell::groupListCheck[31] = False;
$Spell::refVal[31] = -16;
$Spell::graceDistance[31] = 2;
$SkillType[massshield] = $SkillDefensiveCasting;

$Spell::keyword[32] = "mimic";
$Spell::index[mimic] = 32;
$Spell::name[32] = "Mimic";
$Spell::description[32] = "A very dangerous spell that transforms the caster into the creature in his/her LOS.";
$Spell::delay[32] = 4.0;
$Spell::recoveryTime[32] = 60;
$Spell::LOSrange[32] = 80;
$Spell::damageValue[32] = 0;
$Spell::manaCost[32] = 80;
$Spell::startSound[32] = LoopSP;
$Spell::endSound[32] = AbsorbABS;
$Spell::groupListCheck[32] = False;
$Spell::refVal[32] = 1;
$Spell::graceDistance[32] = 2;
$SkillType[mimic] = $SkillNeutralCasting;

$Spell::keyword[33] = "masstransport";
$Spell::index[masstransport] = 33;
$Spell::name[33] = "Mass Transport";
$Spell::description[33] = "Transports self and all friendlies within a 6 meter radius to a specific zone.";
$Spell::delay[33] = 4.0;
$Spell::recoveryTime[33] = 45;
$Spell::radius[33] = 6;
$Spell::manaCost[33] = 50;
$Spell::startSound[33] = RespawnB;
$Spell::endSound[33] = ActivateCH;
$Spell::groupListCheck[33] = False;
$Spell::refVal[33] = 0;
$Spell::graceDistance[33] = 2;
$SkillType[masstransport] = $SkillNeutralCasting;

$Spell::keyword[34] = "advheal4";
$Spell::index[advheal4] = 34;
$Spell::name[34] = "Heal Self Or Other (4th)";
$Spell::description[34] = "Heals the caster or someone in the LOS.";
$Spell::delay[34] = 1.5;
$Spell::recoveryTime[34] = 5.0;
$Spell::damageValue[34] = -35;
$Spell::LOSrange[34] = 80;
$Spell::manaCost[34] = 6;
$Spell::startSound[34] = DeActivateWA;
$Spell::endSound[34] = ActivateAR;
$Spell::groupListCheck[34] = False;
$Spell::refVal[34] = -35;
$Spell::graceDistance[34] = 2;
$SkillType[advheal4] = $SkillDefensiveCasting;

$Spell::keyword[35] = "advheal5";
$Spell::index[advheal5] = 35;
$Spell::name[35] = "Heal Self Or Other (5th)";
$Spell::description[35] = "Heals the caster or someone in the LOS.";
$Spell::delay[35] = 1.5;
$Spell::recoveryTime[35] = 5.5;
$Spell::damageValue[35] = -50;
$Spell::LOSrange[35] = 80;
$Spell::manaCost[35] = 7;
$Spell::startSound[35] = DeActivateWA;
$Spell::endSound[35] = ActivateAR;
$Spell::groupListCheck[35] = False;
$Spell::refVal[35] = -50;
$Spell::graceDistance[35] = 2;
$SkillType[advheal5] = $SkillDefensiveCasting;

$Spell::keyword[36] = "advheal6";
$Spell::index[advheal6] = 36;
$Spell::name[36] = "Heal Self Or Other (6th)";
$Spell::description[36] = "Heals the caster or someone in the LOS.";
$Spell::delay[36] = 1.5;
$Spell::recoveryTime[36] = 6.0;
$Spell::damageValue[36] = -60;
$Spell::LOSrange[36] = 80;
$Spell::manaCost[36] = 8;
$Spell::startSound[36] = DeActivateWA;
$Spell::endSound[36] = ActivateAR;
$Spell::groupListCheck[36] = False;
$Spell::refVal[36] = -60;
$Spell::graceDistance[36] = 2;
$SkillType[advheal6] = $SkillDefensiveCasting;

$Spell::keyword[37] = "nuke";
$Spell::index[nuke] = 37;
$Spell::name[37] = "Nuclear Bomb";
$Spell::description[37] = "The god of all bombs is in your hands.";
$Spell::delay[37] = 7;
$Spell::recoveryTime[37] = 10;
$Spell::radius[37] = 200;
$Spell::damageValue[37] = 600;
$Spell::LOSrange[37] = 100;
$Spell::manaCost[37] = 100;
$Spell::startSound[37] = LaunchLS;
$Spell::endSound[37] = Explode3FW;
$Spell::groupListCheck[37] = False;
$Spell::refVal[37] = 600;
$Spell::graceDistance[37] = 2;
$SkillType[nuke] = $SkillOffensiveCasting;

$Spell::keyword[38] = "bullet";
$Spell::index[bullet] = 38;
$Spell::name[38] = "Bullet";
$Spell::description[38] = "snipe 'em, make them dance, pee your pants.";
$Spell::delay[38] = 0.1;
$Spell::recoveryTime[38] = 0.1;
$Spell::radius[38] = 5;
$Spell::damageValue[38] = 120;
$Spell::LOSrange[38] = 120;
$Spell::manaCost[38] = 20;
$Spell::startSound[38] = ActivateFK;
$Spell::endSound[38] = DeflectAS;
$Spell::groupListCheck[38] = False;
$Spell::refVal[38] = 120;
$Spell::graceDistance[38] = 5;
$SkillType[bullet] = $SkillOffensiveCasting;

$Spell::keyword[39] = "boom";
$Spell::index[boom] = 39;
$Spell::name[39] = "Bad Lab Experiment";
$Spell::description[39] = "These things happen everyday, and now you can make them!";
$Spell::delay[39] = 3;
$Spell::recoveryTime[39] = 20;
$Spell::radius[39] = 30;
$Spell::damageValue[39] = 140;
$Spell::LOSrange[39] = 50;
$Spell::manaCost[39] =30;
$Spell::startSound[39] = LaunchLS;
$Spell::endSound[39] = Explode3FW;
$Spell::groupListCheck[39] = False;
$Spell::refVal[39] = 140;	//was [19] - clobbered Dimension Rift's entry (refVal is display/reference data, no runtime consumer)
$Spell::graceDistance[39] = 2;
$SkillType[boom] = $SkillOffensiveCasting;

$Spell::keyword[40] = "freezerburn";
$Spell::index[freezerburn] = 40;
$Spell::name[40] = "Freezerburn";
$Spell::description[40] = "Freeze your opponent with bad packing.";
$Spell::delay[40] = 0.1;
$Spell::recoveryTime[40] = 0.5;
$Spell::radius[40] = 15;
$Spell::damageValue[40] = 200;
$Spell::LOSrange[40] = 100;
$Spell::manaCost[40] = 40;
$Spell::startSound[40] = ImpactTR;
$Spell::endSound[40] = Reflected;
$Spell::groupListCheck[40] = False;
$Spell::refVal[40] = 200;
$Spell::graceDistance[40] = 2;
$SkillType[freezerburn] = $SkillOffensiveCasting;

$Spell::keyword[41] = "boost";
$Spell::index[boost] = 41;
$Spell::name[41] = "Wind Booster";
$Spell::description[41] = "Use a burst of air to launch yourself forward.";
$Spell::delay[41] = 0.5;
$Spell::recoveryTime[41] = 1;
$Spell::damageValue[41] = "0";
$Spell::manaCost[41] = 15;
$Spell::startSound[41] = ActivateAB;
$Spell::endSound[41] = NoSound;
$Spell::groupListCheck[41] = False;
$Spell::graceDistance[41] = 100;
$SkillType[boost] = $SkillNeutralCasting;

$Spell::keyword[42] = "stop";
$Spell::index[stop] =42;
$Spell::name[42] = "Stop";
$Spell::description[42] = "Use a burst of air to cushion your speed.";
$Spell::delay[42] = 0.2;
$Spell::recoveryTime[42] = 5;
$Spell::damageValue[42] = "0";
$Spell::manaCost[42] = 5;
$Spell::startSound[42] = ActivateAB;
$Spell::endSound[42] = NoSound;
$Spell::groupListCheck[42] = False;
$Spell::graceDistance[42] = 200;
$SkillType[stop] = $SkillNeutralCasting;

$Spell::keyword[43] = "airfist";
$Spell::index[airfist] =43;
$Spell::name[43] = "Airfist";
$Spell::description[43] = "Magical wind fist blasts enemies into the sky.";
$Spell::delay[43] = 1.2;
$Spell::recoveryTime[43] = 3;
$Spell::radius[43] = 10;
$Spell::damageValue[43] = 0;
$Spell::manaCost[43] = 7;
$Spell::startSound[43] = ActivateAB;
$Spell::endSound[43] = NoSound;
$Spell::groupListCheck[43] = False;
$Spell::graceDistance[43] = 2;
$SkillType[airfist] = $SkillNeutralCasting;

$Spell::keyword[44] = "lightstep";
$Spell::index[lightstep] =44;
$Spell::name[44] = "Lightstep";
$Spell::description[44] = "A mass of air that helps you(or player in LOS) lift items.";
$Spell::delay[44] = 2.0;
$Spell::recoveryTime[44] = 10;
$Spell::ticks[44] = 90;
$Spell::LOSrange[44] = 80;
$Spell::manaCost[44] = 12;
$Spell::startSound[44] = ActivateTR;
$Spell::endSound[44] = ActivateTD;
$Spell::groupListCheck[44] = False;
$Spell::graceDistance[44] = 2;
$SkillType[lightstep] =  $SkillNeutralCasting;

$Spell::keyword[45] = "advshove";
$Spell::index[advshove] =45;
$Spell::name[45] = "Super Wind Shove";
$Spell::description[45] = "Use a burst of air to knock an enemy away.";
$Spell::delay[45] = 1;
$Spell::recoveryTime[45] = 3;
$Spell::LOSrange[45] = 80;
$Spell::manaCost[45] = 3;
$Spell::startSound[45] = NoSound;
$Spell::endSound[45] = ActivateAB;
$Spell::groupListCheck[45] = False;
$Spell::graceDistance[45] = 2;
$SkillType[advshove] = $SkillNeutralCasting;

$Spell::keyword[46] = "tornado";
$Spell::index[tornado] =46;
$Spell::name[46] = "Tornado Vortex";
$Spell::description[46] = "Creates a massive blasting column of air.";
$Spell::delay[46] = 9;
$Spell::recoveryTime[46] = 16;
$Spell::radius[46] = 70;
$Spell::damageValue[46] = "400";
$Spell::LOSrange[46] = 120;
$Spell::manaCost[46] = 130;
$Spell::startSound[46] = LaunchLS;
$Spell::endSound[46] = LaunchAB;
$Spell::groupListCheck[46] = False;
$Spell::graceDistance[46] = 2;
$Spell::refVal[46] = 1000;
$SkillType[tornado] = $SkillOffensiveCasting;

$Spell::keyword[47] = "heavystep";
$Spell::index[heavystep] =47;
$Spell::name[47] = "Heavystep";
$Spell::description[47] = "A mass of air that pushes down on your target in your LOS which slows them down.";
$Spell::delay[47] = 4.0;
$Spell::recoveryTime[47] = 5;
$Spell::ticks[47] = 8;
$Spell::LOSrange[47] = 80;
$Spell::manaCost[47] = 40;
$Spell::startSound[47] = ActivateTR;
$Spell::endSound[47] = ActivateTD;
$Spell::groupListCheck[47] = False;
$Spell::graceDistance[47] = 2;
$SkillType[heavystep] =  $SkillNeutralCasting;

$Spell::keyword[48] = "apocalypse";
$Spell::index[apocalypse] =48;
$Spell::name[48] = "Apocalypse";
$Spell::description[48] = "Creates massive explosions everywhere destroying everything in it's path!";
$Spell::delay[48] = 12;
$Spell::recoveryTime[48] = 19;
$Spell::radius[48] = 100;
$Spell::damageValue[48] = "600";
$Spell::LOSrange[48] = 150;
$Spell::manaCost[48] = 200;
$Spell::startSound[48] = LaunchLS;
$Spell::endSound[48] = LaunchAB;
$Spell::groupListCheck[48] = False;
$Spell::graceDistance[48] = 2;
$Spell::refVal[48] = 2000;
$SkillType[apocalypse] = $SkillOffensiveCasting;

$Spell::keyword[49] = "godlyshield";
$Spell::index[godlyshield] = 49;
$Spell::name[49] = "Shield Self Or Other (Godly)";
$Spell::description[49] = "A magical shield that adds 220 DEF and 300 MDEF to the caster or target in LOS.";
$Spell::delay[49] = 2.0;
$Spell::recoveryTime[49] = 20;	//was [30] - Godly Shield's help page showed no recovery time (table is display-only; cooldowns come from weapon fire delay)
$Spell::damageValue[49] = "DEF 220 MDEF 300";
$Spell::ticks[49] = 300;	//10 minutes
$Spell::LOSrange[49] = 80;
$Spell::manaCost[49] = 25;
$Spell::startSound[49] = ActivateTR;
$Spell::endSound[49] = ActivateTD;
$Spell::groupListCheck[49] = False;
$Spell::refVal[49] = -15;
$Spell::graceDistance[49] = 2;
$SkillType[godlyshield] = $SkillDefensiveCasting;

$Spell::keyword[50] = "ionblast";
$Spell::index[ionblast] = 50;
$Spell::name[50] = "Ion Blast";
$Spell::description[50] = "BOOM!";
$Spell::delay[50] = 12.0;
$Spell::recoveryTime[50] = 16.0;
$Spell::radius[50] = 50;
$Spell::damageValue[50] = "1500";
$Spell::LOSrange[50] = 500;
$Spell::manaCost[50] = 300;
$Spell::startSound[50] = LaunchLS;
$Spell::endSound[50] = LaunchAB;
$Spell::groupListCheck[50] = False;
$Spell::graceDistance[50] = 2;
$Spell::refVal[50] = 210;
$SkillType[ionblast] = $SkillOffensiveCasting;

$Spell::keyword[51] = "shredder";
$Spell::index[shredder] = 51;
$Spell::name[51] = "Shredding to pieces";
$Spell::description[51] = "Shred the enemy to pieces with this ultimate offensive spell!";
$Spell::delay[51] = 12.0;
$Spell::recoveryTime[51] = 16.0;
$Spell::radius[51] = 12;
$Spell::damageValue[51] = "3000";
$Spell::LOSrange[51] = 150;
$Spell::manaCost[51] = 500;
$Spell::startSound[51] = LaunchLS;
$Spell::endSound[51] = UnravelAM;
$Spell::groupListCheck[51] = False;
$Spell::graceDistance[51] = 2;
$Spell::refVal[51] = 210;
$SkillType[shredder] = $SkillOffensiveCasting;

$Spell::keyword[52] = "shieldplus1";
$Spell::index[shieldplus1] = 52;
$Spell::name[52] = "Shield Self Or Other (ShieldPlus1)";
$Spell::description[52] = "A magical shield that adds 300 DEF and 400 MDEF to the caster or target in LOS.";
$Spell::delay[52] = 2.0;
$Spell::recoveryTime[52] = 20;
$Spell::damageValue[52] = "DEF 300 MDEF 400";
$Spell::ticks[52] = 300;	//10 minutes
$Spell::LOSrange[52] = 80;
$Spell::manaCost[52] = 35;
$Spell::startSound[52] = ActivateTR;
$Spell::endSound[52] = ActivateTD;
$Spell::groupListCheck[52] = False;
$Spell::refVal[52] = -15;
$Spell::graceDistance[52] = 2;
$SkillType[shieldplus1] = $SkillDefensiveCasting;

$Spell::keyword[53] = "shieldplus2";
$Spell::index[shieldplus2] = 53;
$Spell::name[53] = "Shield Self Or Other (ShieldPlus2)";
$Spell::description[53] = "A magical shield that adds 400 DEF and 500 MDEF to the caster or target in LOS.";
$Spell::delay[53] = 2.0;
$Spell::recoveryTime[53] = 20;
$Spell::damageValue[53] = "DEF 400 MDEF 500";
$Spell::ticks[53] = 300;	//10 minutes
$Spell::LOSrange[53] = 80;
$Spell::manaCost[53] = 50;
$Spell::startSound[53] = ActivateTR;
$Spell::endSound[53] = ActivateTD;
$Spell::groupListCheck[53] = False;
$Spell::refVal[53] = -15;
$Spell::graceDistance[53] = 2;
$SkillType[shieldplus2] = $SkillDefensiveCasting;

$Spell::keyword[54] = "shieldplus3";
$Spell::index[shieldplus3] = 54;
$Spell::name[54] = "Shield Self Or Other (ShieldPlus3)";
$Spell::description[54] = "A magical shield that adds 500 DEF and 600 MDEF to the caster or target in LOS.";
$Spell::delay[54] = 2.0;
$Spell::recoveryTime[54] = 20;
$Spell::damageValue[54] = "DEF 500 MDEF 600";
$Spell::ticks[54] = 300;	//10 minutes
$Spell::LOSrange[54] = 80;
$Spell::manaCost[54] = 75;
$Spell::startSound[54] = ActivateTR;
$Spell::endSound[54] = ActivateTD;
$Spell::groupListCheck[54] = False;
$Spell::refVal[54] = -15;
$Spell::graceDistance[54] = 2;
$SkillType[shieldplus3] = $SkillDefensiveCasting;

$Spell::keyword[55] = "shieldplus4";
$Spell::index[shieldplus4] = 55;
$Spell::name[55] = "Shield Self Or Other (ShieldPlus4)";
$Spell::description[55] = "A magical shield that adds 600 DEF and 700 MDEF to the caster or target in LOS.";
$Spell::delay[55] = 2.0;
$Spell::recoveryTime[55] = 20;
$Spell::damageValue[55] = "DEF 600 MDEF 700";
$Spell::ticks[55] = 300;	//10 minutes
$Spell::LOSrange[55] = 80;
$Spell::manaCost[55] = 100;
$Spell::startSound[55] = ActivateTR;
$Spell::endSound[55] = ActivateTD;
$Spell::groupListCheck[55] = False;
$Spell::refVal[55] = -15;
$Spell::graceDistance[55] = 2;
$SkillType[shieldplus4] = $SkillDefensiveCasting;

$Spell::keyword[56] = "shieldplus5";
$Spell::index[shieldplus5] = 56;
$Spell::name[56] = "Shield Self Or Other (ShieldPlus5)";
$Spell::description[56] = "A magical shield that adds 700 DEF and 800 MDEF to the caster or target in LOS.";
$Spell::delay[56] = 2.0;
$Spell::recoveryTime[56] = 20;
$Spell::damageValue[56] = "DEF 700 MDEF 800";
$Spell::ticks[56] = 300;	//10 minutes
$Spell::LOSrange[56] = 80;
$Spell::manaCost[56] = 125;
$Spell::startSound[56] = ActivateTR;
$Spell::endSound[56] = ActivateTD;
$Spell::groupListCheck[56] = False;
$Spell::refVal[56] = -15;
$Spell::graceDistance[56] = 2;
$SkillType[shieldplus5] = $SkillDefensiveCasting;

$Spell::keyword[57] = "shieldplus6";
$Spell::index[shieldplus6] = 57;
$Spell::name[57] = "Shield Self Or Other (ShieldPlus6)";
$Spell::description[57] = "A magical shield that adds 800 DEF and 900 MDEF to the caster or target in LOS.";
$Spell::delay[57] = 2.0;
$Spell::recoveryTime[57] = 20;
$Spell::damageValue[57] = "DEF 800 MDEF 900";
$Spell::ticks[57] = 300;	//10 minutes
$Spell::LOSrange[57] = 80;
$Spell::manaCost[57] = 150;
$Spell::startSound[57] = ActivateTR;
$Spell::endSound[57] = ActivateTD;
$Spell::groupListCheck[57] = False;
$Spell::refVal[57] = -15;
$Spell::graceDistance[57] = 2;
$SkillType[shieldplus6] = $SkillDefensiveCasting;

%snumber = "58";
$Spell::keyword[%snumber] = "Airblast";
$Spell::index[Airblast] = %snumber;
$Spell::name[%snumber] = "Air blast";
$Spell::description[%snumber] = "Casts a blast of air to send all targets within 15 meters of you flying.";
$Spell::delay[%snumber] = 0.5;
$Spell::recoveryTime[%snumber] = 5.0;
$Spell::radius[%snumber] = 15;
$Spell::manaCost[%snumber] = 100;
$Spell::startSound[%snumber] = SoundJetHeavy;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -3000;
$Spell::graceDistance[%snumber] = 2;
$SkillType[Airblast] = $SkillNeutralCasting;

%snumber = "59";
$Spell::keyword[%snumber] = "Airwarp";
$Spell::index[Airwarp] = %snumber;
$Spell::name[%snumber] = "Air warp";
$Spell::description[%snumber] = "Directly warps air to send all targets within 35 meters of you flying.";
$Spell::delay[%snumber] = 1.5;
$Spell::recoveryTime[%snumber] = 7.0;
$Spell::radius[%snumber] = 35;
$Spell::manaCost[%snumber] = 300;
$Spell::startSound[%snumber] = SoundJetHeavy;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -3000;
$Spell::graceDistance[%snumber] = 2;
$SkillType[Airwarp] = $SkillNeutralCasting;

%snumber = "60";
$Spell::keyword[%snumber] = "HealPlus1";
$Spell::index[HealPlus1] = %snumber;
$Spell::name[%snumber] = "Heal Plus (1st)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1;
$Spell::recoveryTime[%snumber] = 15;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 60;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus1] = $SkillDefensiveCasting;

%snumber = "61";
$Spell::keyword[%snumber] = "HealPlus2";
$Spell::index[HealPlus2] = %snumber;
$Spell::name[%snumber] = "Heal Plus (2nd)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.1;
$Spell::recoveryTime[%snumber] = 20;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 100;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus2] = $SkillDefensiveCasting;

%snumber = "62";
$Spell::keyword[%snumber] = "HealPlus3";
$Spell::index[HealPlus3] = %snumber;
$Spell::name[%snumber] = "Heal Plus (3rd)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.2;
$Spell::recoveryTime[%snumber] = 25;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 150;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus3] = $SkillDefensiveCasting;

%snumber = "63";
$Spell::keyword[%snumber] = "HealPlus4";
$Spell::index[HealPlus4] = %snumber;
$Spell::name[%snumber] = "Heal Plus (4th)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.3;
$Spell::recoveryTime[%snumber] = 30;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 250;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus4] = $SkillDefensiveCasting;

%snumber = "64";
$Spell::keyword[%snumber] = "HealPlus5";
$Spell::index[HealPlus5] = %snumber;
$Spell::name[%snumber] = "Heal Plus (5th)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 1.4;
$Spell::recoveryTime[%snumber] = 35;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 500;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus5] = $SkillDefensiveCasting;

%snumber = "65";
$Spell::keyword[%snumber] = "HealPlus6";
$Spell::index[HealPlus6] = %snumber;
$Spell::name[%snumber] = "Heal Plus (6th)";
$Spell::description[%snumber] = "Heals the caster or someone in the LOS.";
$Spell::delay[%snumber] = 0.0;
$Spell::recoveryTime[%snumber] = 40;
$Spell::damageValue[%snumber] = -100;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 800;
$Spell::startSound[%snumber] = DeActivateWA;
$Spell::endSound[%snumber] = ActivateAR;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[HealPlus6] = $SkillDefensiveCasting;

$Spell::keyword[66] = "Terminate";
$Spell::index[Terminate] = 66;
$Spell::name[66] = "Terminate";
$Spell::description[66] = "If you see this, you're dead.";
$Spell::delay[66] = 24;
$Spell::recoveryTime[66] = 60;
$Spell::radius[66] = 200;
$Spell::damageValue[66] = 4000;
$Spell::LOSrange[66] = 100;
$Spell::manaCost[66] = 1000;
$Spell::startSound[66] = LaunchLS;
$Spell::endSound[66] = Explode3FW;
$Spell::groupListCheck[66] = False;
$Spell::refVal[66] = 600;
$Spell::graceDistance[66] = 2;
$SkillType[Terminate] = $SkillOffensiveCasting;

%snumber = "67";
$Spell::keyword[%snumber] = "Snipe";
$Spell::index[Snipe] = %snumber;
$Spell::name[%snumber] = "Snipe";
$Spell::description[%snumber] = "Hits the target with dead on precision.";
$Spell::delay[%snumber] = 0.0;
$Spell::recoveryTime[%snumber] = 1;
$Spell::damageValue[%snumber] = 3000;
$Spell::LOSrange[%snumber] = 80;
$Spell::manaCost[%snumber] = 800;
$Spell::startSound[%snumber] = HitLevelDT;
$Spell::endSound[%snumber] = HitBF;
$Spell::groupListCheck[%snumber] = False;
$Spell::refVal[%snumber] = -80;
$Spell::graceDistance[%snumber] = 2;
$SkillType[Snipe] = $SkillOffensiveCasting;

$Spell::keyword[68] = "advshield6";
$Spell::index[advshield6] = 68;
$Spell::name[68] = "Shield Self Or Other (6th)";
$Spell::description[68] = "A magical shield that adds 180 DEF and 260 MDEF to the caster or target in LOS.";
$Spell::delay[68] = 2.0;
$Spell::recoveryTime[68] = 22;
$Spell::damageValue[68] = "DEF 180 MDEF 260";
$Spell::ticks[68] = 330;	//11 minutes
$Spell::LOSrange[68] = 80;
$Spell::manaCost[68] = 28;
$Spell::startSound[68] = ActivateTR;
$Spell::endSound[68] = ActivateTD;
$Spell::groupListCheck[68] = False;
$Spell::refVal[68] = -16;
$Spell::graceDistance[68] = 2;
$SkillType[advshield6] = $SkillDefensiveCasting;
//----------------------------------------------------------------------------------------------------------------

