//============================================================================
// Estate.cs - Player housing / building system, Phase 1 (core)
//
// Server-side only; vanilla-client safe (all UX via #estate chat commands +
// Client::sendMessage). Players claim a plot (#estate found), build persistent
// StaticShape structures inside it (#estate build), and the structures survive
// mission reloads / server restarts via an exported registry re-spawned in
// Server::finishMissionLoad (after LoadWorld).
//
// Design + rationale: HOUSING_SYSTEM_DESIGN.md (repo root).
// Phase 1 scope: registry + Save/Load/RehydrateAll, #estate found/info/build/
// demolish/abandon, plot claim + placement validation, first structures.
// Coffer UPKEEP/DECAY (the drain) and turrets/permissions are Phase 2/3 - the
// Coffer BALANCE field exists here but is only funded by demolish/abandon refunds.
//
// PERSISTENCE SHAPE: all persisted data is 1-D arrays, matching the proven
// world-save/house export idiom (SaveWorldDeployables, HouseData::Save). Estates
// are indexed by %eid; structures are a FLAT list indexed by %k with an owning-
// estate back-pointer ($Estate::SEstate[%k]) - deliberately NOT 2-D [%eid,%sid],
// so export/exec round-trips cleanly on the 1998 engine.
//
// Variable-namespace discipline (critical for persistence correctness):
//   $Estate::*    persistent estate/structure data  -> exported to registry file
//   $EstateCfg::* config (costs, caps, datablocks)   -> set at exec, NOT exported
//   $EstateRt::*  runtime-only (live obj ids, index) -> rebuilt on load, NOT exported
// Save() names each $Estate::<field>* family explicitly, so config/runtime globals
// can never leak into the save file.
//============================================================================

//---------------------------------------------------------------- config
// MASTER GATE: feature is unfinished/untested - everything player-facing plus the
// upkeep loop and rehydration is inert unless Server.cs opts in ($pref::EstatesEnabled
// = true, dev only). Default OFF so a live deploy that ships this file changes nothing.
if($pref::EstatesEnabled == "")
	$pref::EstatesEnabled = false;

$EstateCfg::Version        = 1;
$EstateCfg::FoundCost      = 25000;   // coins to claim a plot
$EstateCfg::PlotRadius     = 40;      // world units; structures must be within this of center
$EstateCfg::MinPlotGap     = 2.0;     // multiplier of PlotRadius; min center-to-center spacing
$EstateCfg::MaxStructsBase   = 8;     // structure cap at tier 1
$EstateCfg::MaxStructsPerTier= 8;     // +cap per tier above 1
$EstateCfg::MaxTier          = 3;     // highest estate tier
$EstateCfg::UpgradeCostBase  = 100000;// tier N->N+1 costs UpgradeCostBase * N
$EstateCfg::MaxTurrets       = 3;     // guardian turrets per estate
$EstateCfg::TurretMinTier    = 2;     // turrets require estate tier >= this
$EstateCfg::StructMinSep   = 6;       // min spacing between two structures (units)
$EstateCfg::DemolishRefund = 0.25;    // fraction of build cost refunded on demolish
$EstateCfg::GlobalObjBudget= 400;     // hard ceiling on total estate objects server-wide
                                      // (general object cap is ~1024; patchServerNetcode is
                                      // disabled for client compat - see HOUSING_SYSTEM_DESIGN.md sec 8)

// Structure type registry. All Phase-1 types are real StaticShapeData datablocks
// spawned through the same primitive DeployBase/DeployPlatform use.
$EstateCfg::DB["wall"]      = "DepPlatLargeVert";     // vertical platform used as a wall
$EstateCfg::DB["platform"]  = "DepPlatLargeHorz";     // flat platform / floor
$EstateCfg::DB["forcefield"]= "StaticDoorForceField"; // owner/grouplist-gated door (staticshape.cs:758)
$EstateCfg::DB["turret"]    = "DeployableTurret";     // self-powered guardian turret (Turret.cs); owner-safe via verifyTarget
$EstateCfg::Cost["wall"]       = 2000;
$EstateCfg::Cost["platform"]   = 2500;
$EstateCfg::Cost["forcefield"] = 8000;
$EstateCfg::Cost["turret"]     = 25000;

// Coffer upkeep (Phase 2). Charged PER UPKEEP TICK (one tick = UpkeepFreq seconds).
// Grace/decay windows are measured in TICKS (= hours of server uptime), NOT real
// calendar days: robust with no date-diff parsing, survives restarts (counters are
// persisted), and only advances while the server is actually running/draining. For
// scale: HouseEarnings pays 15000*(remort+1) coins every 15 min at full control
// (gameevents.cs:397), so these upkeep rates are deliberately modest.
$EstateCfg::Upkeep["wall"]       = 50;    // coins/tick
$EstateCfg::Upkeep["platform"]   = 60;
$EstateCfg::Upkeep["forcefield"] = 300;
$EstateCfg::Upkeep["turret"]     = 1000;
$EstateCfg::UpkeepFreq   = 3600;  // seconds between upkeep ticks (1 hour)
$EstateCfg::GraceTicks   = 72;    // insolvent ticks before decay starts (~3 days uptime)
$EstateCfg::DormantTicks = 168;   // empty + broke ticks before the plot is reclaimed (~7 days uptime)

//---------------------------------------------------------------- helpers
function Estate::OwnerTeam(%eid)
{
	// Phase 1: estate structures sit on team 0 (Citizen), like players. The force-field
	// access gate keys on $owner, not team, so 0 is fine. Phase 3 turrets will need a
	// real hostile-targeting team.
	return 0;
}

// Return the estate id owned by %name, or "" if none. Maintains the runtime index.
function Estate::OfOwner(%name)
{
	if(%name == "")
		return "";
	%cached = $EstateRt::ByOwner[%name];
	if(%cached != "" && $Estate::Owner[%cached] == %name)
		return %cached;
	for(%e = 1; %e <= $Estate::Count; %e++)
	{
		if($Estate::Owner[%e] == %name)
		{
			$EstateRt::ByOwner[%name] = %e;
			return %e;
		}
	}
	return "";
}

function Estate::RebuildIndex()
{
	deleteVariables("EstateRt::ByOwner*");
	for(%e = 1; %e <= $Estate::Count; %e++)
		if($Estate::Owner[%e] != "")
			$EstateRt::ByOwner[$Estate::Owner[%e]] = %e;
}

// Count live structures belonging to an estate (flat-list scan).
function Estate::StructCount(%eid)
{
	%n = 0;
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == %eid)
			%n++;
	return %n;
}

// Count an estate's structures of one type (e.g. how many turrets it has).
function Estate::TypeCount(%eid, %type)
{
	%n = 0;
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == %eid && $Estate::SType[%k] == %type)
			%n++;
	return %n;
}

// Structure cap for an estate, scaled by its tier.
function Estate::MaxStructs(%eid)
{
	return $EstateCfg::MaxStructsBase + (($Estate::Tier[%eid] - 1) * $EstateCfg::MaxStructsPerTier);
}

// Cache the owner's current House on the estate (persisted) so rehydrated guardian
// turrets can spare housemates even while the owner is offline.
function Estate::RefreshOwnerHouse(%cl, %eid)
{
	$Estate::OwnerHouse[%eid] = fetchData(%cl, "MyHouse");
}

// Reserve a flat structure slot: reuse a freed one, else extend the high-water.
function Estate::AllocStructSlot()
{
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == "")
			return %k;
	$Estate::SCount = $Estate::SCount + 1;
	return $Estate::SCount;
}

//---------------------------------------------------------------- spawn / rehydrate
// Spawn one flat structure slot's live object from the registry. Same primitive as
// DeployPlatform (rpgfunk.cs:3304). Sets $owner (force-field gate) + $EstateOf (runtime
// obj->estate). Does NOT touch the registry - caller owns that.
function Estate::SpawnStructure(%k)
{
	%eid  = $Estate::SEstate[%k];
	%type = $Estate::SType[%k];
	if(%eid == "" || %type == "")
		return -1;
	%db = $EstateCfg::DB[%type];
	if(%db == "")
	{
		echo("[ESTATE] WARNING: unknown structure type '" @ %type @ "' (slot " @ %k @ ") - skipped");
		return -1;
	}

	%isTurret = (%type == "turret");
	if(%isTurret)
		%obj = newObject("", "Turret", %db, true);
	else
		%obj = newObject("", "StaticShape", %db, true);
	addToSet("MissionCleanup", %obj);

	if(%isTurret)
	{
		// Team 1 makes the turret consider team-0 players; Turret::verifyTarget (extended
		// for estates) then rejects the owner + permitted members regardless of House.
		// %obj.Team = owner's House additionally spares housemates via the same-House rule.
		GameBase::setTeam(%obj, 1);
		%obj.Team = $Estate::OwnerHouse[%eid];
	}
	else
		GameBase::setTeam(%obj, Estate::OwnerTeam(%eid));

	GameBase::setPosition(%obj, $Estate::SPos[%k]);
	GameBase::setRotation(%obj, $Estate::SRot[%k]);
	GameBase::setMapName(%obj, %db);
	if(!%isTurret)
		GameBase::startFadeIn(%obj); // turrets run their own deploy sequence via onAdd

	$owner[%obj]           = $Estate::Owner[%eid];
	$EstateOf[%obj]        = %eid;
	$EstateRt::SObjId[%k]  = %obj;
	$EstateRt::ObjInUse++;
	return %obj;
}

// Re-create every estate's structures after a mission load. Hooked in
// Server::finishMissionLoad AFTER LoadWorld() (MissionCleanup exists, mission loaded).
// Idempotent per load: structures live only in MissionCleanup (destroyed on reload) +
// the registry (authoritative), so there is no double-spawn.
function Estate::RehydrateAll()
{
	dbecho($dbechoMode, "Estate::RehydrateAll()");
	if(!$pref::EstatesEnabled)
		return; // master gate: feature off, don't load/spawn estate structures
	Estate::Load();
	$EstateRt::ObjInUse = 0;
	deleteVariables("EstateRt::SObjId*");
	Estate::RebuildIndex();

	%structs = 0;
	for(%k = 1; %k <= $Estate::SCount; %k++)
	{
		%eid = $Estate::SEstate[%k];
		if(%eid == "" || $Estate::Owner[%eid] == "")
			continue;
		if(Estate::SpawnStructure(%k) != -1)
			%structs++;
	}
	echo("[ESTATE] Rehydrated " @ $EstateRt::ObjInUse @ " structure(s) across " @ $Estate::Count @ " estate slot(s).");
}

//---------------------------------------------------------------- persistence
// Registry file is per-mission (estate coords only make sense on the mission they were
// built on), mirroring the world-save path idiom (rpgfunk.cs LoadWorld/SaveWorldDeployables):
// write to "temp\<mission>_estates_.cs"; check isFile("temp\..") but exec the bare name.
function Estate::Save(%echoOff)
{
	dbecho($dbechoMode, "Estate::Save(" @ %echoOff @ ")");
	%f = "temp\\" @ $missionName @ "_estates_.cs";
	File::delete(%f);
	export("Estate::Count",      %f, false);
	export("Estate::Owner*",     %f, true);
	export("Estate::CenterPos*", %f, true);
	export("Estate::Radius*",    %f, true);
	export("Estate::Coffer*",    %f, true);
	export("Estate::Tier*",      %f, true);
	export("Estate::GraceTicks*",%f, true);
	export("Estate::EmptyTicks*",%f, true);
	export("Estate::Members*",   %f, true);
	export("Estate::OwnerHouse*",%f, true);
	export("Estate::SCount",     %f, true);
	export("Estate::SEstate*",   %f, true);
	export("Estate::SType*",     %f, true);
	export("Estate::SPos*",      %f, true);
	export("Estate::SRot*",      %f, true);
	export("Estate::STier*",     %f, true);
	if(!%echoOff)
		echo("[ESTATE] Saved registry to " @ %f);
}

function Estate::Load()
{
	%fbare = $missionName @ "_estates_.cs";
	if(isFile("temp\\" @ %fbare))
	{
		$LoadingEstates = true;
		exec(%fbare);
		$LoadingEstates = false;
		echo("[ESTATE] Loaded registry '" @ %fbare @ "' (" @ $Estate::Count @ " estate slot(s), " @ $Estate::SCount @ " structure slot(s)).");
	}
}

// Debounced save (generation-token pattern; TorqueScript has no schedule cancel()).
// A burst of edits collapses to one disk write ~5s after the last request.
function Estate::RequestSave(%reason)
{
	$EstateRt::SaveToken++;
	%tok = $EstateRt::SaveToken;
	schedule("Estate::DoScheduledSave(" @ %tok @ ", \"" @ %reason @ "\");", 5);
}
function Estate::DoScheduledSave(%tok, %reason)
{
	if(%tok != $EstateRt::SaveToken)
		return; // superseded by a later request
	Estate::Save(true);
	dbecho($dbechoMode, "[ESTATE] autosave (" @ %reason @ ")");
}

//---------------------------------------------------------------- commands
function Estate::Found(%cl)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%name = Client::getName(%cl);
	if(Estate::OfOwner(%name) != "")
	{
		Client::sendMessage(%cl, $MsgRed, "You already own an estate. Use '#estate info'.");
		return;
	}
	if(Zone::getType(fetchData(%cl, "zone")) == "PROTECTED")
	{
		Client::sendMessage(%cl, $MsgRed, "You cannot found an estate inside protected territory.");
		return;
	}
	%center = GameBase::getPosition(%cl);
	%gap = $EstateCfg::PlotRadius * $EstateCfg::MinPlotGap;
	for(%e = 1; %e <= $Estate::Count; %e++)
	{
		if($Estate::Owner[%e] == "")
			continue;
		if(Vector::getDistance(%center, $Estate::CenterPos[%e]) < %gap)
		{
			Client::sendMessage(%cl, $MsgRed, "Too close to another estate. Move farther away and try again.");
			return;
		}
	}
	%cost = $EstateCfg::FoundCost;
	if(fetchData(%cl, "COINS") < %cost)
	{
		Client::sendMessage(%cl, $MsgRed, "Founding an estate costs " @ Number::Beautify(%cost, -3) @ " coins (you must carry them, not bank them).");
		return;
	}
	storeData(%cl, "COINS", %cost, "dec");

	// Reuse a freed estate slot if one exists, else extend the high-water.
	%eid = "";
	for(%e = 1; %e <= $Estate::Count; %e++)
		if($Estate::Owner[%e] == "") { %eid = %e; break; }
	if(%eid == "")
	{
		%eid = $Estate::Count + 1;
		$Estate::Count = %eid;
	}

	$Estate::Owner[%eid]      = %name;
	$Estate::CenterPos[%eid]  = %center;
	$Estate::Radius[%eid]     = $EstateCfg::PlotRadius;
	$Estate::Coffer[%eid]     = 0;
	$Estate::Tier[%eid]       = 1;
	$Estate::GraceTicks[%eid] = 0;
	$Estate::EmptyTicks[%eid] = 0;
	$Estate::Members[%eid]    = "";
	$Estate::OwnerHouse[%eid] = fetchData(%cl, "MyHouse");
	$EstateRt::ByOwner[%name] = %eid;

	Estate::RequestSave("found");
	Client::sendMessage(%cl, $MsgGreen, "Estate founded! Plot radius " @ $Estate::Radius[%eid] @ ". Build inside it with '#estate build <wall|platform|forcefield>' (aim at the ground).");
}

function Estate::Build(%cl, %type)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate. Use '#estate found' first.");
		return;
	}
	if(%type == "" || $EstateCfg::DB[%type] == "")
	{
		Client::sendMessage(%cl, $MsgBeige, "Usage: #estate build <wall|platform|forcefield|turret>");
		return;
	}
	if(%type == "turret")
	{
		if($Estate::Tier[%eid] < $EstateCfg::TurretMinTier)
		{
			Client::sendMessage(%cl, $MsgRed, "Guardian turrets require estate tier " @ $EstateCfg::TurretMinTier @ ". Use '#estate upgrade'.");
			return;
		}
		if(Estate::TypeCount(%eid, "turret") >= $EstateCfg::MaxTurrets)
		{
			Client::sendMessage(%cl, $MsgRed, "Turret limit reached (" @ $EstateCfg::MaxTurrets @ " per estate).");
			return;
		}
	}
	if(Estate::StructCount(%eid) >= Estate::MaxStructs(%eid))
	{
		Client::sendMessage(%cl, $MsgRed, "Estate structure limit reached (" @ Estate::MaxStructs(%eid) @ "). Use '#estate upgrade' for more.");
		return;
	}
	if($EstateRt::ObjInUse + 1 > $EstateCfg::GlobalObjBudget)
	{
		Client::sendMessage(%cl, $MsgRed, "The realm's builders are overextended right now. Try again later.");
		return;
	}
	if(!GameBase::getLOSInfo(%cl, 3))
	{
		Client::sendMessage(%cl, $MsgRed, "Aim at the ground where you want to build.");
		return;
	}
	if(Vector::dot($los::normal, "0 0 1") <= 0.7)
	{
		Client::sendMessage(%cl, $MsgRed, "The ground is too steep to build here.");
		return;
	}
	%pos = $los::position;
	if(Vector::getDistance(%pos, $Estate::CenterPos[%eid]) > $Estate::Radius[%eid])
	{
		Client::sendMessage(%cl, $MsgRed, "That spot is outside your plot (radius " @ $Estate::Radius[%eid] @ ").");
		return;
	}
	if(Zone::getType(fetchData(%cl, "zone")) == "PROTECTED")
	{
		Client::sendMessage(%cl, $MsgRed, "You cannot build in protected territory.");
		return;
	}
	for(%k = 1; %k <= $Estate::SCount; %k++)
	{
		if($Estate::SEstate[%k] != %eid)
			continue;
		if(Vector::getDistance(%pos, $Estate::SPos[%k]) < $EstateCfg::StructMinSep)
		{
			Client::sendMessage(%cl, $MsgRed, "Too close to another structure.");
			return;
		}
	}
	%cost = $EstateCfg::Cost[%type];
	if(fetchData(%cl, "COINS") < %cost)
	{
		Client::sendMessage(%cl, $MsgRed, "You need " @ Number::Beautify(%cost, -3) @ " coins to build a " @ %type @ ".");
		return;
	}

	// Bake the vertical-platform pose offset into the stored transform (same as DeployBase
	// rpgfunk.cs:566-568), so rehydration re-spawns it in place with no re-offset.
	%rot = GameBase::getRotation(%cl);
	if($EstateCfg::DB[%type] == "DepPlatLargeVert")
	{
		%rot = "0 1.5708 " @ GetWord(%rot, 2) + "1.5708";
		%pos = GetWord(%pos, 0) @ " " @ GetWord(%pos, 1) @ " " @ (GetWord(%pos, 2) + 4.5);
	}

	storeData(%cl, "COINS", %cost, "dec");
	Estate::RefreshOwnerHouse(%cl, %eid); // keep guardian-turret House current
	%k = Estate::AllocStructSlot();
	$Estate::SEstate[%k] = %eid;
	$Estate::SType[%k]   = %type;
	$Estate::SPos[%k]    = %pos;
	$Estate::SRot[%k]    = %rot;
	$Estate::STier[%k]   = 1;
	Estate::SpawnStructure(%k);

	Estate::RequestSave("build");
	Client::sendMessage(%cl, $MsgGreen, "Built a " @ %type @ ". (" @ Estate::StructCount(%eid) @ "/" @ Estate::MaxStructs(%eid) @ ")");
}

// Free one flat structure slot: delete its live object and blank its registry fields.
function Estate::FreeStructSlot(%k)
{
	%obj = $EstateRt::SObjId[%k];
	if(%obj != "" && %obj != -1 && isObject(%obj))
	{
		deleteObject(%obj);
		$EstateRt::ObjInUse--;
	}
	$Estate::SEstate[%k]  = "";
	$Estate::SType[%k]    = "";
	$Estate::SPos[%k]     = "";
	$Estate::SRot[%k]     = "";
	$Estate::STier[%k]    = "";
	$EstateRt::SObjId[%k] = "";
}

function Estate::Demolish(%cl)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate.");
		return;
	}
	%ppos = GameBase::getPosition(%cl);
	%best = -1;
	%bestDist = 999999;
	for(%k = 1; %k <= $Estate::SCount; %k++)
	{
		if($Estate::SEstate[%k] != %eid)
			continue;
		%d = Vector::getDistance(%ppos, $Estate::SPos[%k]);
		if(%d < %bestDist)
		{
			%bestDist = %d;
			%best = %k;
		}
	}
	if(%best == -1 || %bestDist > 12)
	{
		Client::sendMessage(%cl, $MsgRed, "Stand next to a structure you own to demolish it.");
		return;
	}

	%type = $Estate::SType[%best];
	%refund = floor($EstateCfg::Cost[%type] * $EstateCfg::DemolishRefund);
	Estate::FreeStructSlot(%best);

	if(%refund > 0)
		storeData(%cl, "COINS", %refund, "inc");
	Estate::RequestSave("demolish");
	Client::sendMessage(%cl, $MsgGreen, "Demolished a " @ %type @ ". Refunded " @ Number::Beautify(%refund, -3) @ " coins. (" @ Estate::StructCount(%eid) @ "/" @ Estate::MaxStructs(%eid) @ ")");
}

function Estate::Abandon(%cl)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate.");
		return;
	}
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == %eid)
			Estate::FreeStructSlot(%k);

	%refund = $Estate::Coffer[%eid];
	%name = $Estate::Owner[%eid];
	$Estate::Owner[%eid]      = "";
	$Estate::CenterPos[%eid]  = "";
	$Estate::Radius[%eid]     = "";
	$Estate::Coffer[%eid]     = "";
	$Estate::Tier[%eid]       = "";
	$Estate::GraceTicks[%eid] = "";
	$Estate::EmptyTicks[%eid] = "";
	$Estate::Members[%eid]    = "";
	$Estate::OwnerHouse[%eid] = "";
	$EstateRt::ByOwner[%name] = "";

	if(%refund > 0)
		storeData(%cl, "COINS", %refund, "inc");
	Estate::RequestSave("abandon");
	Client::sendMessage(%cl, $MsgGreen, "Estate abandoned. Coffer balance returned: " @ Number::Beautify(%refund, -3) @ " coins.");
}

function Estate::Info(%cl)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgBeige, "You have no estate. Stand where you want it and use '#estate found' (" @ Number::Beautify($EstateCfg::FoundCost, -3) @ " coins).");
		return;
	}
	%due = Estate::UpkeepDue(%eid);
	Client::sendMessage(%cl, $MsgBeige, "=== Your Estate (#" @ %eid @ ")  Tier " @ $Estate::Tier[%eid] @ " ===");
	Client::sendMessage(%cl, $MsgBeige, "Plot radius " @ $Estate::Radius[%eid] @ "  |  Coffer " @ Number::Beautify($Estate::Coffer[%eid], -3) @ "  |  Structures " @ Estate::StructCount(%eid) @ "/" @ Estate::MaxStructs(%eid));
	if(%due > 0)
	{
		%hrs = 0;
		if($Estate::Coffer[%eid] >= %due)
			%hrs = floor($Estate::Coffer[%eid] / %due);
		if($Estate::GraceTicks[%eid] > 0)
			Client::sendMessage(%cl, $MsgRed, "Upkeep " @ Number::Beautify(%due, -3) @ "/hr  |  COFFER EMPTY - structures decay in ~" @ (($EstateCfg::GraceTicks - $Estate::GraceTicks[%eid]) + 1) @ " hr. Use '#estate deposit'.");
		else
			Client::sendMessage(%cl, $MsgBeige, "Upkeep " @ Number::Beautify(%due, -3) @ "/hr  |  Coffer funds ~" @ %hrs @ " more hour(s).");
	}
	%line = "";
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == %eid)
			%line = %line @ $Estate::SType[%k] @ " ";
	if(%line != "")
		Client::sendMessage(%cl, $MsgBeige, "Built: " @ %line);
	if($Estate::Members[%eid] != "")
		Client::sendMessage(%cl, $MsgBeige, "Members: " @ $Estate::Members[%eid]);
	Client::sendMessage(%cl, $MsgBeige, "Commands: build <type> | deposit/withdraw <n|all> | permit/evict <name> | upgrade | demolish | abandon");
}

//============================================================================
// Phase 2 - Coffer economy: deposit/withdraw + the upkeep/decay tick.
//============================================================================

// Sum of per-tick upkeep across an estate's live structures.
function Estate::UpkeepDue(%eid)
{
	%due = 0;
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == %eid)
			%due = %due + $EstateCfg::Upkeep[$Estate::SType[%k]];
	return %due;
}

function Estate::Deposit(%cl, %amtStr)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate. Use '#estate found' first.");
		return;
	}
	%have = fetchData(%cl, "COINS");
	if(%amtStr == "all")
		%amt = %have;
	else
		%amt = floor(%amtStr);
	if(%amt <= 0)
	{
		Client::sendMessage(%cl, $MsgRed, "Usage: #estate deposit <amount|all> (deposits carried coins into your coffer).");
		return;
	}
	if(%amt > %have)
	{
		Client::sendMessage(%cl, $MsgRed, "You are only carrying " @ Number::Beautify(%have, -3) @ " coins.");
		return;
	}
	storeData(%cl, "COINS", %amt, "dec");
	$Estate::Coffer[%eid] = $Estate::Coffer[%eid] + %amt;
	// A deposit that restores solvency should lift grace immediately (not wait a tick).
	if($Estate::GraceTicks[%eid] > 0 && $Estate::Coffer[%eid] >= Estate::UpkeepDue(%eid))
		$Estate::GraceTicks[%eid] = 0;
	Estate::RequestSave("deposit");
	Client::sendMessage(%cl, $MsgGreen, "Deposited " @ Number::Beautify(%amt, -3) @ " coins. Coffer: " @ Number::Beautify($Estate::Coffer[%eid], -3) @ ".");
}

function Estate::Withdraw(%cl, %amtStr)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate.");
		return;
	}
	%bal = $Estate::Coffer[%eid];
	if(%amtStr == "all")
		%amt = %bal;
	else
		%amt = floor(%amtStr);
	if(%amt <= 0 || %bal <= 0)
	{
		Client::sendMessage(%cl, $MsgRed, "Your coffer holds " @ Number::Beautify(%bal, -3) @ " coins. Usage: #estate withdraw <amount|all>.");
		return;
	}
	if(%amt > %bal)
		%amt = %bal;
	$Estate::Coffer[%eid] = $Estate::Coffer[%eid] - %amt;
	storeData(%cl, "COINS", %amt, "inc");
	Estate::RequestSave("withdraw");
	Client::sendMessage(%cl, $MsgGreen, "Withdrew " @ Number::Beautify(%amt, -3) @ " coins. Coffer: " @ Number::Beautify($Estate::Coffer[%eid], -3) @ ".");
}

// Grant a player estate membership: passes force fields + is spared by guardian turrets.
// NOTE: single-word names only (the router passes one word); multi-word names are a
// known limitation to revisit if needed.
function Estate::Permit(%cl, %targetName)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate.");
		return;
	}
	if(%targetName == "")
	{
		Client::sendMessage(%cl, $MsgBeige, "Usage: #estate permit <player name> (grants force-field access; turrets hold fire).");
		return;
	}
	if(%targetName == $Estate::Owner[%eid])
	{
		Client::sendMessage(%cl, $MsgRed, "You are the owner.");
		return;
	}
	if(IsInCommaList($Estate::Members[%eid], %targetName))
	{
		Client::sendMessage(%cl, $MsgBeige, %targetName @ " is already a member.");
		return;
	}
	$Estate::Members[%eid] = AddToCommaList($Estate::Members[%eid], %targetName);
	Estate::RequestSave("permit");
	Client::sendMessage(%cl, $MsgGreen, "Granted " @ %targetName @ " access to your estate.");
	Estate::Notify(%targetName, "You have been granted access to " @ $Estate::Owner[%eid] @ "'s estate.");
}

function Estate::Evict(%cl, %targetName)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate.");
		return;
	}
	if(%targetName == "" || !IsInCommaList($Estate::Members[%eid], %targetName))
	{
		Client::sendMessage(%cl, $MsgRed, "That player is not a member. Usage: #estate evict <player name>.");
		return;
	}
	$Estate::Members[%eid] = RemoveFromCommaList($Estate::Members[%eid], %targetName);
	Estate::RequestSave("evict");
	Client::sendMessage(%cl, $MsgGreen, "Revoked " @ %targetName @ "'s access.");
}

function Estate::Upgrade(%cl)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "You have no estate.");
		return;
	}
	%tier = $Estate::Tier[%eid];
	if(%tier >= $EstateCfg::MaxTier)
	{
		Client::sendMessage(%cl, $MsgRed, "Your estate is already at the maximum tier (" @ $EstateCfg::MaxTier @ ").");
		return;
	}
	%cost = $EstateCfg::UpgradeCostBase * %tier;
	if(fetchData(%cl, "COINS") < %cost)
	{
		Client::sendMessage(%cl, $MsgRed, "Upgrading to tier " @ (%tier + 1) @ " costs " @ Number::Beautify(%cost, -3) @ " coins.");
		return;
	}
	storeData(%cl, "COINS", %cost, "dec");
	$Estate::Tier[%eid] = %tier + 1;
	Estate::RefreshOwnerHouse(%cl, %eid);
	Estate::RequestSave("upgrade");
	%msg = "Estate upgraded to tier " @ $Estate::Tier[%eid] @ "! Structure cap is now " @ Estate::MaxStructs(%eid) @ ".";
	if($Estate::Tier[%eid] == $EstateCfg::TurretMinTier)
		%msg = %msg @ " Guardian turrets unlocked - '#estate build turret'.";
	Client::sendMessage(%cl, $MsgGreen, %msg);
}

//---------------------------------------------------------------- QoL / ops
function Estate::Where(%cl)
{
	if(isRPGAI(%cl) || Player::isAiControlled(%cl))
		return;
	%eid = Estate::OfOwner(Client::getName(%cl));
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgBeige, "You have no estate. Use '#estate found'.");
		return;
	}
	%d = Vector::getDistance(GameBase::getPosition(%cl), $Estate::CenterPos[%eid]);
	if(%d <= $Estate::Radius[%eid])
		Client::sendMessage(%cl, $MsgBeige, "You are standing on your estate (#" @ %eid @ ").");
	else
		Client::sendMessage(%cl, $MsgBeige, "Your estate (#" @ %eid @ ") is about " @ floor(%d) @ " units away.");
}

function Estate::Help(%cl)
{
	Client::sendMessage(%cl, $MsgBeige, "=== Estate commands ===");
	Client::sendMessage(%cl, $MsgBeige, "#estate found  - claim a plot where you stand (" @ Number::Beautify($EstateCfg::FoundCost, -3) @ " coins)");
	Client::sendMessage(%cl, $MsgBeige, "#estate build <wall|platform|forcefield|turret>  - aim at ground in your plot");
	Client::sendMessage(%cl, $MsgBeige, "#estate deposit/withdraw <n|all>  - fund the coffer that pays upkeep");
	Client::sendMessage(%cl, $MsgBeige, "#estate permit/evict <name>  - grant/revoke member access");
	Client::sendMessage(%cl, $MsgBeige, "#estate upgrade | demolish | abandon | where | info");
}

// Admin overview of all estates (adminLevel >= 4).
function Estate::List(%cl)
{
	if(%cl.adminLevel < 4)
	{
		Client::sendMessage(%cl, $MsgRed, "That command is for admins.");
		return;
	}
	%shown = 0;
	%total = 0;
	for(%e = 1; %e <= $Estate::Count; %e++)
	{
		if($Estate::Owner[%e] == "")
			continue;
		%total++;
		if(%shown >= 25)
			continue;
		%shown++;
		%tag = "";
		if($Estate::GraceTicks[%e] > 0)
			%tag = " [GRACE]";
		Client::sendMessage(%cl, $MsgBeige, "#" @ %e @ " " @ $Estate::Owner[%e] @ " T" @ $Estate::Tier[%e] @ " structs=" @ Estate::StructCount(%e) @ " coffer=" @ Number::Beautify($Estate::Coffer[%e], -3) @ %tag);
	}
	Client::sendMessage(%cl, $MsgGreen, "Estates: " @ %total @ " total  |  objects " @ $EstateRt::ObjInUse @ "/" @ $EstateCfg::GlobalObjBudget @ ".");
	if(%shown < %total)
		Client::sendMessage(%cl, $MsgBeige, "(showing first " @ %shown @ " of " @ %total @ ")");
}

// Admin force-reclaim of a named player's estate (adminLevel >= 4). Coffer is forfeited -
// this is a cleanup tool for griefed/abandoned plots, not a refund path.
function Estate::AdminReclaim(%cl, %name)
{
	if(%cl.adminLevel < 4)
	{
		Client::sendMessage(%cl, $MsgRed, "That command is for admins.");
		return;
	}
	if(%name == "")
	{
		Client::sendMessage(%cl, $MsgBeige, "Usage: #estate reclaim <owner name>");
		return;
	}
	%eid = Estate::OfOwner(%name);
	if(%eid == "")
	{
		Client::sendMessage(%cl, $MsgRed, "No estate is owned by '" @ %name @ "'.");
		return;
	}
	%structs = Estate::StructCount(%eid);
	Estate::ReclaimEstate(%eid);
	Estate::RequestSave("admin_reclaim");
	Client::sendMessage(%cl, $MsgGreen, "Reclaimed " @ %name @ "'s estate (#" @ %eid @ ") - " @ %structs @ " structure(s) removed.");
}

// Remove the NEWEST structure of an estate (highest flat index). Returns its type, or "".
function Estate::DecayOneStructure(%eid)
{
	%target = -1;
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == %eid)
			%target = %k; // last match wins = newest built
	if(%target == -1)
		return "";
	%type = $Estate::SType[%target];
	Estate::FreeStructSlot(%target);
	return %type;
}

// Free an estate entirely (plot reclaimed). Coffer is assumed already 0.
function Estate::ReclaimEstate(%eid)
{
	for(%k = 1; %k <= $Estate::SCount; %k++)
		if($Estate::SEstate[%k] == %eid)
			Estate::FreeStructSlot(%k);
	%name = $Estate::Owner[%eid];
	$Estate::Owner[%eid]      = "";
	$Estate::CenterPos[%eid]  = "";
	$Estate::Radius[%eid]     = "";
	$Estate::Coffer[%eid]     = "";
	$Estate::Tier[%eid]       = "";
	$Estate::GraceTicks[%eid] = "";
	$Estate::EmptyTicks[%eid] = "";
	$Estate::Members[%eid]    = "";
	$Estate::OwnerHouse[%eid] = "";
	$EstateRt::ByOwner[%name] = "";
}

// Message an owner if they are currently online.
function Estate::Notify(%name, %msg)
{
	%cl = getClientByName(%name);
	if(%cl != -1 && %cl != "")
		Client::sendMessage(%cl, $MsgBeige, "[Estate] " @ %msg);
}

// One pass over all estates: charge upkeep, run the solvency -> grace -> decay ->
// reclaim state machine. Called by the self-rescheduling Estate::UpkeepLoop.
function Estate::UpkeepTick()
{
	dbecho($dbechoMode, "Estate::UpkeepTick()");
	%changed = false;
	for(%e = 1; %e <= $Estate::Count; %e++)
	{
		if($Estate::Owner[%e] == "")
			continue;
		%due = Estate::UpkeepDue(%e);

		if(%due <= 0)
		{
			// No structures to maintain. Reset grace; if the plot is also broke and idle
			// for a long time, reclaim it so empty claims don't squat the map forever.
			$Estate::GraceTicks[%e] = 0;
			if($Estate::Coffer[%e] <= 0)
			{
				$Estate::EmptyTicks[%e] = $Estate::EmptyTicks[%e] + 1;
				%changed = true;
				if($Estate::EmptyTicks[%e] > $EstateCfg::DormantTicks)
				{
					Estate::Notify($Estate::Owner[%e], "Your empty, unfunded estate has been reclaimed by the realm.");
					Estate::ReclaimEstate(%e);
				}
			}
			else if($Estate::EmptyTicks[%e] != 0)
			{
				$Estate::EmptyTicks[%e] = 0;
				%changed = true;
			}
			continue;
		}

		$Estate::EmptyTicks[%e] = 0;

		if($Estate::Coffer[%e] >= %due)
		{
			$Estate::Coffer[%e] = $Estate::Coffer[%e] - %due;
			if($Estate::GraceTicks[%e] > 0)
				Estate::Notify($Estate::Owner[%e], "Your estate coffer is funded again - upkeep resumed.");
			$Estate::GraceTicks[%e] = 0;
			%changed = true;
		}
		else
		{
			// Insolvent: drain whatever remains to zero and advance the grace counter.
			$Estate::Coffer[%e] = 0;
			$Estate::GraceTicks[%e] = $Estate::GraceTicks[%e] + 1;
			%changed = true;
			if($Estate::GraceTicks[%e] > $EstateCfg::GraceTicks)
			{
				%removed = Estate::DecayOneStructure(%e);
				if(%removed != "")
					Estate::Notify($Estate::Owner[%e], "Coffer empty - your " @ %removed @ " has crumbled. Deposit coins ('#estate deposit') to stop the decay.");
			}
			else
			{
				%left = ($EstateCfg::GraceTicks - $Estate::GraceTicks[%e]) + 1;
				Estate::Notify($Estate::Owner[%e], "WARNING: estate coffer empty. Structures begin crumbling in ~" @ %left @ " hour(s). Use '#estate deposit'.");
			}
		}
	}
	if(%changed)
		Estate::RequestSave("upkeep");
}

// Start the upkeep loop. Called from Mission::init (gameevents.cs) - the ONLY safe place
// to start periodic loops (exec-time schedules are flushed on mission load). The gen token
// guards against a double-start within one mission's lifetime.
function Estate::Init()
{
	dbecho($dbechoMode, "Estate::Init()");
	if(!$pref::EstatesEnabled)
		return; // master gate: feature off, no upkeep loop
	$EstateRt::UpkeepGen++;
	schedule("Estate::UpkeepLoop(" @ $EstateRt::UpkeepGen @ ");", $EstateCfg::UpkeepFreq);
}
function Estate::UpkeepLoop(%gen)
{
	if(%gen != $EstateRt::UpkeepGen)
		return; // superseded by a newer Init
	Estate::UpkeepTick();
	schedule("Estate::UpkeepLoop(" @ %gen @ ");", $EstateCfg::UpkeepFreq);
}
