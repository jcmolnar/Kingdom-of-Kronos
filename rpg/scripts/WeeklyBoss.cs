//==============================================================================
// WeeklyBoss.cs - the kingdom's weekly raid boss (v0.9)
//
// DESIGN (agreed 2026-07-04):
// - ONE shared boss per real-world week (Monday rollover via kronos_datetime).
//   The persistent thing is its REMAINING HP, not the bot: the boss only
//   exists while real players are in the Colloseum. When the arena empties,
//   we checkpoint its HP and despawn it OURSELVES (quiet noExp kill, same as
//   the daily elite timeout) - so the engine's empty-zone cleanup never gets
//   a chance to eat it, and low-level players can never be ambushed (the
//   Colloseum is a sealed doorless cube - teleport is the ONLY way in).
// - Summon: #weekly challenge teleports you in (return spot remembered) and
//   summons/joins; #weekly leave teleports you back. Resummons at saved HP.
// - Seal battles own the arena: challenge refuses while $SealBattleActive,
//   and the watchdog despawn-checkpoints the boss if a seal battle starts.
//   (Known interaction: weekly fighters inside at seal start get captured as
//   seal participants - that's the seal system's existing bystander rule.)
// - Offense adapts: ATK/DEF are rebuilt from the CHALLENGER's benchmark on
//   every summon (Daily::BenchStats ratios) while the HP pool is kingdom-wide.
// - Credit: every point of real damage a player lands (refreshHP hook in
//   playerdamage.cs, numeric HP units) accrues to $Weekly::Contrib[name]
//   (name-keyed: survives disconnects AND restarts via export/exec).
// - Reward on kill: contributors whose damage >= ContribThreshold x their own
//   benchmark HP get exp (% of level cost, self-scaling) + remort-scaled
//   coins. Killshot gets a bonus. AFK-tagging pays nothing.
// - Persistence: export("Weekly::*") to config\WeeklyBossState.cs on every
//   state change; exec'd back in Weekly::Init (same idiom as Players.cs /
//   IRCServers.cs / Server::storeData).
//
// Wiring:
// - exec'd from Server.cs after DailyQuest
// - Weekly::Init() from Mission::init (gameevents.cs), AFTER Daily::Init()
//   (reuses $Daily::HasRealDate probe + BenchStats; exec-time schedules are
//   flushed by mission load, so the loop must start here)
// - Weekly::OnDamage() from playerdamage.cs (refreshHP result hook)
// - Weekly::OnBossKilled() from Client::onKilled (WeeklyBossTag check)
// - ai.cs Bot_ClearStoreData clears WeeklyBossTag (stale-slot protection,
//   same class of bug as DailyEliteOwner)
//==============================================================================

$WEEKLY_DEBUG = 0;

//------------------------------------------------------------------------------
// Tuning
//------------------------------------------------------------------------------
// review #45: tuning constants live in Weekly::LoadTuning() so they can be RE-APPLIED
// after Weekly::Init exec's the saved state file. Weekly::Save exports ALL Weekly::*
// (including these), and that exec runs AFTER these defaults - so without re-applying,
// an admin's edits here were frozen to the first-saved values until
// config\WeeklyBossState.cs was manually deleted. Genuine STATE (BossHP/Stamp/Contrib/
// Tpl/LastQuartile) is set elsewhere (Rotate/damage) and legitimately persists.
function Weekly::LoadTuning()
{
	$Weekly::Enabled = 1;
	$Weekly::HPBudget = 250000;        // boss HP pool per week, NUMERIC hp units (RETUNE on test!)
	$Weekly::ContribThreshold = 0.25;  // reward if your damage >= 25% of YOUR benchmark MaxHP
	$Weekly::ExpRewardPct = 60;        // percent of current level's exp cost (daily pays 20)
	$Weekly::CoinBase = 10000;         // coins at remort 0
	$Weekly::CoinPerRemort = 0.5;      // +50% per remort step
	$Weekly::KillshotBonusMult = 0.5;  // killshot gets +50% on top
	$Weekly::SpawnPos = "-3609.04 -2363.99 354";  // seal-battle mage spawn spot - PROVEN inside the arena (the first guess, y=-2334, was ~25u beyond the north wall: bot spawned outside and fell to terrain)
	$Weekly::EntrancePos = "-3588 -2364 354";  // where players teleport in (seal battle entrance spot)
	$Weekly::ExitPos = "-335 -2339 65.5";      // town drop-off (SealBattle::TeleportOut position)
	$Weekly::ArenaZoneDesc = "Colloseum";   // Zone::getDesc match (seal battle uses the same)
	$Weekly::CheckPeriod = 30;         // seconds between checkpoint/empty-arena sweeps

	// boss template rotates weekly; reuses the daily-elite hunt templates
	$Weekly::BossPool = 5;
	$Weekly::BossTpl[0] = "Chief";
	$Weekly::BossTpl[1] = "BattleOx";
	$Weekly::BossTpl[2] = "Queen";
	$Weekly::BossTpl[3] = "DeathKnight";
	$Weekly::BossTpl[4] = "NullNull";

	// offense ratios (same knobs the daily elite uses; benchmark-relative)
	$Weekly::DmgRatio = 0.05;          // target hit = 5% of challenger's benchmark MaxHP
	$Weekly::DmgComp = 5;              // combat pipeline ~5x compensation (seal SetupBot value)
	$Weekly::DEFRatio = 0.3;           // of skill cap at challenger's remort
	$Weekly::MDEFRatio = 0.3;
}
Weekly::LoadTuning();   // apply the code-defined tuning at boot

//------------------------------------------------------------------------------
// Week stamp: ISO-ish week index (weeks start Monday), from getRealDate().
// Serial-day math (Howard Hinnant civil-from-days) so month/year boundaries
// and downtime spanning the rollover all just work. Fallback: uptime weeks
// derived from the daily system's fallback day counter.
//------------------------------------------------------------------------------
function Weekly::DaySerial(%y, %m, %d)
{
	// days since 1970-01-01
	if(%m <= 2)
	{
		%y = %y - 1;
		%mp = %m + 9;
	}
	else
		%mp = %m - 3;
	%era = floor(%y / 400);
	%yoe = %y - (%era * 400);
	%doy = floor(((153 * %mp) + 2) / 5) + %d - 1;
	%doe = (%yoe * 365) + floor(%yoe / 4) - floor(%yoe / 100) + %doy;
	return (%era * 146097) + %doe - 719468;
}

function Weekly::WeekIndex()
{
	if($Daily::HasRealDate)
	{
		%ds = getRealDate();   // YYYYMMDD
		%y = floor(String::getSubStr(%ds, 0, 4));
		%m = floor(String::getSubStr(%ds, 4, 2));
		%d = floor(String::getSubStr(%ds, 6, 2));
		// 1970-01-01 was a Thursday (serial 0); +3 aligns floors to Mondays
		return "WK" @ floor((Weekly::DaySerial(%y, %m, %d) + 3) / 7);
	}
	return "UPWK" @ floor($Daily::FallbackDay / 7);
}

//------------------------------------------------------------------------------
// Persistence
//------------------------------------------------------------------------------
function Weekly::Save()
{
	// live-session values must not leak into the next boot
	%bc = $Weekly::BossClient;
	%bn = $Weekly::BossName;
	%sm = $Weekly::Summoning;   // in-flight summon flag - never persist it
	$Weekly::BossClient = "";
	$Weekly::BossName = "";
	$Weekly::Summoning = "";
	export("Weekly::*", "config\\WeeklyBossState.cs", False);
	$Weekly::BossClient = %bc;
	$Weekly::BossName = %bn;
	$Weekly::Summoning = %sm;
}

//------------------------------------------------------------------------------
// Rotation
//------------------------------------------------------------------------------
function Weekly::Rotate()
{
	%wk = Weekly::WeekIndex();
	if(%wk == $Weekly::Stamp)
		return;

	// review 2026-07-17: never roll over mid-fight. A live boss (or an in-flight
	// summon) still carries the OLD week's tag, HP and contributions; resetting
	// now would wipe contributions, let CheckLoop's HP checkpoint corrupt the new
	// pool with the old boss's leftover HP, and make the eventual kill pay nobody
	// (its WeeklyBossTag != the new Stamp). Defer - CheckLoop reconciles the boss
	// first and reaches Rotate again once the arena clears (kill or empty despawn).
	if(($Weekly::BossClient != "" && $Weekly::BossClient != -1) || $Weekly::Summoning)
		return;

	$Weekly::Stamp = %wk;
	$Weekly::BossHP = $Weekly::HPBudget;
	$Weekly::Slain = "";
	$Weekly::TopName = "";
	$Weekly::TopVal = 0;
	$Weekly::LastQuartile = 4;
	deleteVariables("Weekly::Contrib*");   // no $ prefix - matches ClearVariables/rpgfunk usage

	// numeric part of the stamp picks the template
	%n = floor(String::getSubStr(%wk, 2, 9));
	if(%wk == "UPWK" @ floor($Daily::FallbackDay / 7))
		%n = floor(String::getSubStr(%wk, 4, 9));
	$Weekly::Tpl = $Weekly::BossTpl[Daily::Mod(%n, $Weekly::BossPool)];

	Weekly::Save();
	echo("Weekly: rotated to " @ %wk @ " - boss " @ $Weekly::Tpl @ " HP " @ $Weekly::BossHP);
	messageAll($MsgBeige, "A new terror stalks the Colloseum: " @ $Weekly::Tpl @ "! Gather your strength - #weekly");
}

//------------------------------------------------------------------------------
// Arena scan (the SealBattle::StartBattle pattern)
//------------------------------------------------------------------------------
function Weekly::PlayersInArena()
{
	%n = 0;
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(isRPGAI(%cl))
			continue;
		if(Zone::getDesc(fetchData(%cl, "zone")) == $Weekly::ArenaZoneDesc)
			%n++;
	}
	return %n;
}

//------------------------------------------------------------------------------
// Summon / despawn lifecycle
//------------------------------------------------------------------------------
// The Colloseum is a sealed cube with NO doors - the only way in or out is
// teleport (the seal NPC does the same). #weekly challenge teleports you in
// (remembering where you stood), #weekly leave teleports you back.
function Weekly::Enter(%clientId)
{
	%pl = Client::getOwnedObject(%clientId);
	if(%pl == -1)
		return false;
	$Weekly::ReturnPos[%clientId] = GameBase::getPosition(%pl);
	GameBase::setPosition(%pl, $Weekly::EntrancePos);
	Client::sendMessage(%clientId, $MsgWhite, "You are teleported to the Colloseum. #weekly leave returns you when you're done.");
	return true;
}

function Weekly::Leave(%clientId)
{
	if($SealBattleActive == true)
	{
		Client::sendMessage(%clientId, $MsgBeige, "Not during a seal battle.");
		return;
	}
	if(Zone::getDesc(fetchData(%clientId, "zone")) != $Weekly::ArenaZoneDesc)
	{
		Client::sendMessage(%clientId, $MsgBeige, "You are not in the Colloseum.");
		return;
	}
	%pl = Client::getOwnedObject(%clientId);
	if(%pl == -1)
		return;
	%pos = $Weekly::ReturnPos[%clientId];
	if(%pos == "")
		%pos = $Weekly::ExitPos;   // seal battle's town drop-off (camped inside? this rescues you)
	$Weekly::ReturnPos[%clientId] = "";
	GameBase::setPosition(%pl, %pos);
	Client::sendMessage(%clientId, $MsgWhite, "You have been teleported out.");
}

function Weekly::Challenge(%clientId)
{
	if(!$Weekly::Enabled)
	{
		Client::sendMessage(%clientId, $MsgBeige, "The weekly hunt is not available.");
		return;
	}
	if($Weekly::Slain == $Weekly::Stamp)
	{
		Client::sendMessage(%clientId, $MsgBeige, "The kingdom's terror is already slain this week. A new one arrives Monday.");
		return;
	}
	if($SealBattleActive == true)
	{
		Client::sendMessage(%clientId, $MsgBeige, "The Colloseum is hosting a seal battle - try again later.");
		return;
	}
	if(IsDead(%clientId))
		return;
	// review 2026-07-17: jailed players must not escape confinement via the
	// arena teleport. Refuse BEFORE Weekly::Enter moves them.
	if(IsJailed(%clientId))
	{
		Client::sendMessage(%clientId, $MsgBeige, "You cannot answer the call from jail.");
		return;
	}

	// teleport in if needed (the arena has no doors)
	if(Zone::getDesc(fetchData(%clientId, "zone")) != $Weekly::ArenaZoneDesc)
	{
		if(!Weekly::Enter(%clientId))
			return;
	}

	// review 2026-07-17: don't spawn a SECOND boss if a summon is already in
	// flight. $Weekly::BossClient is set only in Weekly::Setup, 4.5s after the
	// summon, so the old BossClient-only guard let two challenges inside that
	// window each spawn a tagged boss - double payout plus an uncredited ghost
	// the watchdog never despawns. $Weekly::Summoning is set synchronously below
	// and cleared when Setup resolves (success or failure). The challenger has
	// already been teleported in above, so they still join the fight.
	if($Weekly::Summoning)
	{
		Client::sendMessage(%clientId, $MsgBeige, "The terror is answering the call - ready yourself!");
		return;
	}
	if($Weekly::BossClient != "" && $Weekly::BossClient != -1)
	{
		if(AI::getClientIdFromName($Weekly::BossName) != -1)
		{
			Client::sendMessage(%clientId, $MsgBeige, "The terror is already here - fight!");
			return;
		}
		// stale reference (bot died outside our bookkeeping) - clear and continue
		$Weekly::BossClient = "";
		$Weekly::BossName = "";
	}

	%internalName = AI::helper($Weekly::Tpl, "Kingdom Terror", "TempSpawn " @ $Weekly::SpawnPos @ " 1", default);
	if(%internalName == -1 || %internalName == "")
	{
		Client::sendMessage(%clientId, $MsgRed, "The terror failed to answer the call - try again in a moment.");
		return;
	}

	$Weekly::BossName = %internalName;
	$Weekly::SummonTime = getSimTime();   // grace vs the empty-arena sweep (zone data lags a teleport)
	$Weekly::Summoning = true;            // in flight until Setup resolves (blocks a concurrent double-spawn)
	// 4.5s: after SpawnAIGetClientId (3.0s) registers the bot - daily elite timing
	schedule("Weekly::Setup(\"" @ %internalName @ "\", " @ %clientId @ ");", 4.5);
	messageAll($MsgRed, Client::getName(%clientId) @ " has challenged the Kingdom Terror in the Colloseum! (" @ Weekly::HPPercent() @ "% strength remains)");
	echo("[WEEKLY] " @ Client::getName(%clientId) @ " (" @ %clientId @ ") summoned " @ $Weekly::Tpl @ " as " @ %internalName);
}

function Weekly::Setup(%internalName, %challenger)
{
	%aiId = AI::getClientIdFromName(%internalName);
	if(%aiId == "" || %aiId == -1)
	{
		echo("[WEEKLY] Setup: " @ %internalName @ " not found (spawn failed?)");
		if($Weekly::BossName == %internalName)
		{
			$Weekly::BossName = "";
			$Weekly::BossClient = "";
		}
		$Weekly::Summoning = false;   // summon resolved (failed) - allow a retry
		return;
	}

	// challenger left/died between summon and setup? benchmark still fine -
	// it's non-manipulable data, not live state.
	%bench = Daily::BenchStats(%challenger);
	%benchHP = GetWord(%bench, 0);
	%r = fetchData(%challenger, "RemortStep");
	if(%r == "" || %r == -1)
		%r = 0;
	%skillCap = Daily::SkillCapAtRemort(%r);

	%hp = floor($Weekly::BossHP);   // the PERSISTENT pool - resume where the kingdom left it
	if(%hp < 1)
		%hp = 1;
	%atk = floor((%benchHP * $Weekly::DmgRatio) / $Weekly::DmgComp);
	if(%atk < 1)
		%atk = 1;
	%def = floor(%skillCap * $Weekly::DEFRatio);
	%mdef = floor(%skillCap * $Weekly::MDEFRatio);

	// seal-style override (RefreshAll-proof), same as Daily::SetupElite
	storeData(%aiId, "SealBattleBot", true);
	storeData(%aiId, "SealBattleScaledRound", 1);
	$SealBattleScaledStats[%aiId, "round"] = 1;

	$SealBattleScaledStats[%aiId, "MaxHP"] = %hp;
	setHP(%aiId, %hp);
	storeData(%aiId, "HP", %hp);

	storeData(%aiId, "ATK", %atk);
	$EnemyBotData[%aiId, "ATK"] = %atk;
	$ClientData[%aiId, "ATK"] = %atk;
	$SealBattleScaledStats[%aiId, "ATK"] = %atk;
	storeData(%aiId, "DMG", %atk);
	$EnemyBotData[%aiId, "DMG"] = %atk;
	$ClientData[%aiId, "DMG"] = %atk;
	$SealBattleScaledStats[%aiId, "DMG"] = %atk;
	storeData(%aiId, "DEF", %def);
	$EnemyBotData[%aiId, "DEF"] = %def;
	$ClientData[%aiId, "DEF"] = %def;
	$SealBattleScaledStats[%aiId, "DEF"] = %def;
	storeData(%aiId, "MDEF", %mdef);
	$EnemyBotData[%aiId, "MDEF"] = %mdef;
	$ClientData[%aiId, "MDEF"] = %mdef;
	$SealBattleScaledStats[%aiId, "MDEF"] = %mdef;

	storeData(%aiId, "WeeklyBossTag", $Weekly::Stamp);
	// review 2026-07-17: the boss reward is the weekly bounty ONLY (paid by
	// Weekly::OnBossKilled, which runs OUTSIDE the noExperienceFlag gate). Without
	// this flag the kill also fired the normal pipeline - DistributeExpForKilling
	// (per-damager kill-exp on a 250k-HP victim) AND Daily::OnKill cull credit
	// (the boss is an enemybot with no DailyEliteOwner) - a triple reward.
	storeData(%aiId, "noExperienceFlag", True);
	$Weekly::BossClient = %aiId;
	$Weekly::Summoning = false;   // summon resolved (live) - guard hands off to BossClient

	// force it onto the arena floor - if the spawn snapped/fell anywhere else
	// (terrain under the interior, wall clip), this recovers it
	%botObj = Client::getOwnedObject(%aiId);
	if(%botObj != -1)
	{
		GameBase::setPosition(%botObj, $Weekly::SpawnPos);
		echo("[WEEKLY] boss positioned at " @ GameBase::getPosition(%botObj));
	}

	echo("[WEEKLY] Setup " @ %internalName @ " (clientId " @ %aiId @ "): HP=" @ %hp @ " ATK=" @ %atk @ " DEF=" @ %def @ " MDEF=" @ %mdef @ " (challenger bench " @ %benchHP @ ")");
	messageAll($MsgRed, "The Kingdom Terror has arrived in the Colloseum!");
}

// quiet checkpoint-despawn: clear the tag FIRST so Client::onKilled cannot
// mistake our own teardown for a player kill
function Weekly::Despawn(%reason)
{
	%aiId = $Weekly::BossClient;
	$Weekly::BossClient = "";
	%name = $Weekly::BossName;
	$Weekly::BossName = "";

	if(%aiId == "" || %aiId == -1)
		return;

	%hp = fetchData(%aiId, "HP");
	if(%hp != "" && %hp != -1 && %hp > 0)
		$Weekly::BossHP = floor(%hp);

	storeData(%aiId, "WeeklyBossTag", "");
	storeData(%aiId, "noExperienceFlag", True);
	// review 2026-07-17: a quiet checkpoint-despawn (arena empty / seal battle) is
	// NOT a death - suppress the lootbag, or every despawn litters the sealed
	// arena with the boss's coins/gear (the drop gate keys on noDropLootbagFlag).
	storeData(%aiId, "noDropLootbagFlag", True);
	Player::Kill(%aiId);
	Weekly::Save();
	echo("[WEEKLY] boss " @ %name @ " despawned (" @ %reason @ ") at HP " @ $Weekly::BossHP);
}

//------------------------------------------------------------------------------
// Contribution is keyed by player NAME so it survives disconnect/restart, but the
// key becomes a console VARIABLE-NAME SUBSCRIPT ($Weekly::Contrib[key]) and Save's
// export() writes that name verbatim (only VALUES are escaped). A space - or any
// non-identifier char - in a player's name would emit an unparseable line into
// config\WeeklyBossState.cs, breaking its exec from that line on and wiping
// Stamp/Slain/BossHP; the weekly boss then reset to full and became refarmable
// every restart (a single hit from a space-named player was enough). NameKey
// sanitizes to an identifier-safe subscript: keep [A-Za-z0-9], map everything
// else to '_'. findSubStr uppercases both operands, so the membership test still
// accepts lowercase letters. Space-free names are unchanged (old entries still
// match); residual key collisions are a negligible bounty-share fairness edge,
// not the data-loss bug. TopName keeps the raw display name (a quoted value).
//------------------------------------------------------------------------------
function Weekly::NameKey(%name)
{
	%out = "";
	%len = String::len(%name);
	for(%i = 0; %i < %len; %i++)
	{
		%ch = String::getSubStr(%name, %i, 1);
		if(String::findSubStr("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", %ch) != -1)
			%out = %out @ %ch;
		else
			%out = %out @ "_";
	}
	if(%out == "")
		%out = "_";   // all-punctuation name still yields a valid, non-empty key
	return %out;
}

//------------------------------------------------------------------------------
// Damage credit (playerdamage.cs refreshHP hook; %numDmg is NUMERIC hp units,
// i.e. already x $TribesDamageToNumericDamage - same scale as benchmark MaxHP)
//------------------------------------------------------------------------------
function Weekly::OnDamage(%shooter, %numDmg)
{
	if(%numDmg <= 0)
		return;
	if(Player::isAiControlled(%shooter) || isRPGAI(%shooter))
		return;

	%name = Client::getName(%shooter);
	%key = Weekly::NameKey(%name);
	if($Weekly::Contrib[%key] == "")
		$Weekly::Contrib[%key] = 0;
	$Weekly::Contrib[%key] += %numDmg;

	if($Weekly::Contrib[%key] > $Weekly::TopVal)
	{
		$Weekly::TopVal = $Weekly::Contrib[%key];
		$Weekly::TopName = %name;
	}

	// threshold announcements when the LIVE hp drops below 75/50/25% of the
	// weekly budget (%q = which quartile it's IN: 3=100-75 .. 0=25-0)
	%cur = fetchData($Weekly::BossClient, "HP");
	if(%cur != "" && %cur != -1 && $Weekly::HPBudget > 0)
	{
		%q = floor((%cur * 4) / $Weekly::HPBudget);
		if(%q < $Weekly::LastQuartile && %q <= 2)
		{
			$Weekly::LastQuartile = %q;
			messageAll($MsgBeige, "The Kingdom Terror staggers - below " @ ((%q + 1) * 25) @ "% of its strength!");
		}
	}
}

function Weekly::HPPercent()
{
	if($Weekly::HPBudget <= 0)
		return 0;
	return floor(($Weekly::BossHP * 100) / $Weekly::HPBudget);
}

//------------------------------------------------------------------------------
// Death -> payout (Client::onKilled hook; only fires on a genuine kill, since
// Weekly::Despawn strips the tag before its own teardown kill)
//------------------------------------------------------------------------------
function Weekly::OnBossKilled(%victim, %killer)
{
	if(fetchData(%victim, "WeeklyBossTag") != $Weekly::Stamp)
		return;   // stale tag from an earlier week: ignore (teardown clears it too)

	storeData(%victim, "WeeklyBossTag", "");
	$Weekly::BossClient = "";
	$Weekly::BossName = "";
	$Weekly::Slain = $Weekly::Stamp;
	$Weekly::BossHP = 0;

	%killerName = "";
	if(%killer != "" && %killer != -1 && !Player::isAiControlled(%killer))
		%killerName = Client::getName(%killer);

	messageAll($MsgGreen, "THE KINGDOM TERROR HAS FALLEN! Killshot: " @ %killerName @ " - Top contributor: " @ $Weekly::TopName);
	messageAll($MsgBeige, "Champions will be carried home in 20 seconds (#weekly leave to go now).");
	schedule("Weekly::Evacuate();", 20);
	echo("[WEEKLY] boss slain, week " @ $Weekly::Stamp @ ", killshot " @ %killerName @ ", top " @ $Weekly::TopName @ " (" @ $Weekly::TopVal @ ")");

	// pay online contributors (name-keyed damage vs their OWN benchmark)
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(isRPGAI(%cl) || Player::isAiControlled(%cl))
			continue;
		%name = Client::getName(%cl);
		%c = $Weekly::Contrib[Weekly::NameKey(%name)];
		if(%c == "" || %c <= 0)
			continue;

		%benchHP = GetWord(Daily::BenchStats(%cl), 0);
		if(%benchHP <= 0)
			continue;
		if(%c < floor(%benchHP * $Weekly::ContribThreshold))
		{
			Client::sendMessage(%cl, $MsgBeige, "You struck the Terror, but not hard enough for a share of the bounty.");
			continue;
		}

		%lvl = fetchData(%cl, "LVL");
		%expCost = GetExp(%lvl + 1, %cl) - GetExp(%lvl, %cl);
		if(%expCost < 0)
			%expCost = 0;
		%exp = floor(%expCost * ($Weekly::ExpRewardPct / 100));

		%r = fetchData(%cl, "RemortStep");
		if(%r == "" || %r == -1)
			%r = 0;
		%coins = floor($Weekly::CoinBase * (1 + (%r * $Weekly::CoinPerRemort)));

		if(%cl == %killer)
		{
			%exp = floor(%exp * (1 + $Weekly::KillshotBonusMult));
			%coins = floor(%coins * (1 + $Weekly::KillshotBonusMult));
		}

		// HOUSE-GATE 2026-08-22: storeData's EXP chokepoint denies gains for a house-less
		// character at $houseRequiredLevel+. Coins are still paid; zero the exp here too
		// so the bounty line and the audit echo report what was actually granted.
		%expBlocked = IsExpHouseBlocked(%cl);
		if(%expBlocked)
			%exp = 0;

		storeData(%cl, "EXP", %exp, "inc");
		storeData(%cl, "COINS", %coins, "inc");
		Game::refreshClientScore(%cl);
		echo("[WEEKLY REWARD] " @ %name @ " (" @ %cl @ "): +" @ %exp @ " exp, +" @ %coins @ " coins (contrib " @ %c @ ")");
		if(%expBlocked)
			Client::sendMessage(%cl, $MsgGreen, "Weekly bounty! " @ Number::Beautify(%coins, -3) @ " coins. No experience - you must join a house to continue growing stronger.~house");
		else
			Client::sendMessage(%cl, $MsgGreen, "Weekly bounty! " @ Number::Beautify(%exp, -3) @ " exp and " @ Number::Beautify(%coins, -3) @ " coins.");
	}

	Weekly::Save();
}

// after a kill, ferry every remaining fighter home (the cube has no doors);
// each goes back to where they challenged from, or the town drop-off
function Weekly::Evacuate()
{
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(isRPGAI(%cl) || Player::isAiControlled(%cl))
			continue;
		if(Zone::getDesc(fetchData(%cl, "zone")) != $Weekly::ArenaZoneDesc)
			continue;
		%pl = Client::getOwnedObject(%cl);
		if(%pl == -1)
			continue;
		%pos = $Weekly::ReturnPos[%cl];
		if(%pos == "")
			%pos = $Weekly::ExitPos;
		$Weekly::ReturnPos[%cl] = "";
		GameBase::setPosition(%pl, %pos);
		Client::sendMessage(%cl, $MsgWhite, "You have been teleported out of the Colloseum.");
	}
}

//------------------------------------------------------------------------------
// Status (#weekly)
//------------------------------------------------------------------------------
function Weekly::Status(%clientId)
{
	Client::sendMessage(%clientId, $MsgBeige, "--- Weekly Boss (week " @ $Weekly::Stamp @ ") ---");
	if($Weekly::Slain == $Weekly::Stamp)
		Client::sendMessage(%clientId, 0, $Weekly::Tpl @ ": SLAIN this week. A new terror arrives Monday.");
	else
	{
		Client::sendMessage(%clientId, 0, $Weekly::Tpl @ ": " @ Weekly::HPPercent() @ "% strength remains.");
		if($Weekly::BossClient != "" && $Weekly::BossClient != -1)
			Client::sendMessage(%clientId, 0, "It is IN THE COLLOSEUM right now - join the fight!");
		else
			Client::sendMessage(%clientId, 0, "Challenge it: #weekly challenge teleports you to the Colloseum arena.");
	}

	%name = Client::getName(%clientId);
	%c = $Weekly::Contrib[Weekly::NameKey(%name)];
	if(%c == "" || %c <= 0)
		Client::sendMessage(%clientId, 0, "Your contribution: none yet.");
	else
	{
		%benchHP = GetWord(Daily::BenchStats(%clientId), 0);
		%need = floor(%benchHP * $Weekly::ContribThreshold);
		if(%c >= %need)
			Client::sendMessage(%clientId, 0, "Your contribution: " @ Number::Beautify(floor(%c), -3) @ " damage - BOUNTY SECURED (paid when it dies).");
		else
			Client::sendMessage(%clientId, 0, "Your contribution: " @ Number::Beautify(floor(%c), -3) @ " / " @ Number::Beautify(%need, -3) @ " damage for a bounty share.");
	}
	if($Weekly::TopName != "")
		Client::sendMessage(%clientId, 0, "Top contributor: " @ $Weekly::TopName @ " (" @ Number::Beautify(floor($Weekly::TopVal), -3) @ ")");
}

//------------------------------------------------------------------------------
// Init + watchdog (start from Mission::init ONLY, after Daily::Init)
//------------------------------------------------------------------------------
function Weekly::Init()
{
	// review #49: the loop schedule must re-arm on EVERY mission load (Server::loadMission
	// flushes all pending schedules, killing the previous CheckLoop). The
	// $Weekly::LoopStarted guard used to skip this WHOLE function on the 2nd+ mission load,
	// so the flushed loop never restarted and the weekly boss stopped rolling over /
	// checkpointing. One-time state restore stays guarded; the loop is always re-scheduled.
	if(!$Weekly::LoopStarted)
	{
		$Weekly::LoopStarted = true;

		// restore last saved state (contributions, remaining HP, stamp)
		if(isFile("config\\WeeklyBossState.cs"))
			exec("config\\WeeklyBossState.cs");
		// review #45: the saved file also carries the tuning constants (Weekly::Save
		// exports all Weekly::*) and just overwrote them - re-apply the code-defined
		// tuning so script edits aren't frozen to the first-saved values.
		Weekly::LoadTuning();
		// live-session fields never survive a boot
		$Weekly::BossClient = "";
		$Weekly::BossName = "";
		$Weekly::Summoning = false;   // no summon can be in flight at boot
		if($Weekly::LastQuartile == "")
			$Weekly::LastQuartile = 4;   // 0 is a legal persisted value (below 25%)

		Weekly::Rotate();
		echo("Weekly: init - week " @ $Weekly::Stamp @ ", boss " @ $Weekly::Tpl @ ", HP " @ $Weekly::BossHP @ "/" @ $Weekly::HPBudget);
	}
	schedule("Weekly::CheckLoop();", $Weekly::CheckPeriod);
}

function Weekly::CheckLoop()
{
	// review 2026-07-17: reconcile the live boss BEFORE the rollover. This clears
	// a stale handle so it can't defer the rollover forever, and - when the arena
	// empties on the rollover tick - lets Despawn run first so Rotate's reset then
	// authoritatively overwrites the HP checkpoint. The old order (Rotate first)
	// let the checkpoint stomp the freshly-reset pool with the old boss's HP.
	if($Weekly::BossClient != "" && $Weekly::BossClient != -1)
	{
		if(AI::getClientIdFromName($Weekly::BossName) == -1)
		{
			// bot vanished outside our control (engine cleanup, admin, crash-kill):
			// the last checkpointed HP stands
			echo("[WEEKLY] boss " @ $Weekly::BossName @ " disappeared - keeping HP checkpoint " @ $Weekly::BossHP);
			$Weekly::BossClient = "";
			$Weekly::BossName = "";
			Weekly::Save();
		}
		else if($SealBattleActive == true)
			Weekly::Despawn("seal battle starting - arena handed over");
		else if(Weekly::PlayersInArena() == 0 && (getSimTime() - $Weekly::SummonTime) > 60)
			Weekly::Despawn("arena empty");
		else
		{
			// periodic HP checkpoint while the fight is live, so even a hard
			// crash only loses seconds of progress
			%hp = fetchData($Weekly::BossClient, "HP");
			if(%hp != "" && %hp != -1 && %hp > 0)
			{
				$Weekly::BossHP = floor(%hp);
				Weekly::Save();
			}
		}
	}

	Weekly::Rotate();   // week rollover (deferred inside Rotate while a boss is live)

	schedule("Weekly::CheckLoop();", $Weekly::CheckPeriod);
}
