//==============================================================================
// DailyQuest.cs - rotating daily quest system (v0.9)
//
// DESIGN (agreed 2026-07-02):
// - The day's quests are shared TEMPLATES (themes); each player's concrete
//   contract is resolved at accept-time against THEIR character and stored via
//   storeData, so a remort 5 and a remort 100 player get appropriately-sized
//   work from the same Herald.
// - Difficulty/reward both key off a BENCHMARK: the player's real stat formulas
//   (mirrored from rpgstats.cs fetchData) evaluated with actual level/skills
//   but CEILING gear - the best items equippable at their remort ($SkillRestriction
//   filtered), regardless of what they own. Strip-naked / bank-the-gear
//   manipulation is impossible: inventory is never an input, and sandbagging
//   any real input (level/skills) lowers the reward by the same factor.
// - Same reference-player philosophy as SealBattle::GetReferencePlayerStats()
//   (remortseal.cs), but per-player and with real gear tables instead of the
//   hardcoded itemBonus=500 estimate.
// - Day stamp comes from kronos_datetime.dll getRealDate() so rotation survives
//   crashes/restarts; falls back to an uptime day counter if the plugin is
//   missing (rotation then rerolls on restart - install the plugin!).
//
// Wiring:
// - exec'd from Server.cs with the other rpg scripts
// - Daily::Init() called from Mission::init (gameevents.cs) - NEVER at exec
//   time (Server::loadMission flushes exec-time schedules)
// - Daily::OnKill(%killer, %victim) called from the kill pipeline (playerdamage.cs)
// - Herald dialogue (botType "dailyquest") + #daily live in comchat.cs
//==============================================================================

$DAILY_DEBUG = 0;

//------------------------------------------------------------------------------
// Tuning
//------------------------------------------------------------------------------
$Daily::ExpRewardPct = 20;          // percent of the CURRENT level's exp cost paid per daily
$Daily::CoinBase = 2500;            // coin payout at remort 0 (before daily multiplier)
$Daily::CoinPerRemort = 0.5;        // +50% coins per remort step
$Daily::CullCreditFactor = 3.0;     // cull completes when total victim MaxHP ~= this x benchmark HP
$Daily::CullMaxCreditPerKill = 34;  // hard cap: one kill pays at most 34% (so minimum 3 kills)
$Daily::FetchCount = 5;             // default count when a pool entry has no count of its own
$Daily::RingSlots = 1;              // ring slots assumed for the gear ceiling
$Daily::EliteEnabled = 0;           // elite daily disabled until the scaled elite bot lands

// Fetch item pools per tier. Tier from remort: 0:<25  1:<50  2:<75  3:<100  4:>=100
// Entries are "ItemName count" ("count" optional -> $Daily::FetchCount).
// Items taken from the static quest NPCs' NEED lists in KingdomKronos.mis;
// tier placement is an ESTIMATE from drop-source toughness - PLEASE ADJUST.
// An empty tier makes the Herald offer Cull instead of Fetch (graceful fallback).
$Daily::FetchPoolSize[0] = 3;
$Daily::FetchPool[0, 0] = "BlackStatue 5";
$Daily::FetchPool[0, 1] = "SkeletonBone 5";
$Daily::FetchPool[0, 2] = "OgreTooth 1";
$Daily::FetchPoolSize[1] = 4;
$Daily::FetchPool[1, 0] = "EnchantedStone 5";
$Daily::FetchPool[1, 1] = "AncientScroll 5";
$Daily::FetchPool[1, 2] = "Bible 5";
$Daily::FetchPool[1, 3] = "MinotaurHorn 1";
$Daily::FetchPoolSize[2] = 3;
$Daily::FetchPool[2, 0] = "AlienSpine 5";
$Daily::FetchPool[2, 1] = "AlienEye 1";
$Daily::FetchPool[2, 2] = "AngelsTear 5";
$Daily::FetchPoolSize[3] = 2;
$Daily::FetchPool[3, 0] = "DemonBreath 5";
$Daily::FetchPool[3, 1] = "KronoStone 5";
$Daily::FetchPoolSize[4] = 2;
$Daily::FetchPool[4, 0] = "VoidStone 5";
$Daily::FetchPool[4, 1] = "VirusFragment 5";

//------------------------------------------------------------------------------
// Small helpers
//------------------------------------------------------------------------------
function Daily::Mod(%a, %b)
{
	if(%b == "" || %b == 0)
		return 0;
	return %a - (floor(%a / %b) * %b);
}

// Max level / skill cap a character CAN have at remort R
// (formulas mirrored from rpgstats.cs / skills.cs, same as remortseal.cs:230)
function Daily::MaxLevelAtRemort(%r)
{
	return 125 + (%r * 8);
}
function Daily::SkillCapAtRemort(%r)
{
	return (10 * Daily::MaxLevelAtRemort(%r)) + 20 + (%r * 2);
}

function Daily::TierForRemort(%r)
{
	if(%r < 25) return 0;
	if(%r < 50) return 1;
	if(%r < 75) return 2;
	if(%r < 100) return 3;
	return 4;
}

//------------------------------------------------------------------------------
// Day stamp (kronos_datetime.dll, with uptime fallback)
//------------------------------------------------------------------------------
function Daily::ProbeRealDate()
{
	%d = getRealDate();
	if(String::len(%d) == 8 && floor(%d) >= 20200101)
	{
		$Daily::HasRealDate = true;
		echo("Daily: real date source OK (" @ %d @ ")");
	}
	else
	{
		$Daily::HasRealDate = false;
		echo("Daily: WARNING - kronos_datetime.dll missing (getRealDate returned '" @ %d @ "'); using uptime fallback. Dailies REROLL on restart until the plugin is installed.");
	}
}

function Daily::GetDayStamp()
{
	if($Daily::HasRealDate)
		return getRealDate();
	return "UP" @ $Daily::FallbackDay;
}

// numeric seed from the day stamp, kept small so 32-bit float math stays exact
function Daily::SeedFromDay(%day)
{
	if($Daily::HasRealDate)
	{
		// YYYYMMDD -> YYYY*500 + MMDD (~1.01M max, well under float precision limits)
		%seed = (floor(String::getSubStr(%day, 0, 4)) * 500) + floor(String::getSubStr(%day, 4, 4));
	}
	else
		%seed = $Daily::FallbackDay;

	%seed = Daily::Mod(%seed, 9973);
	%seed = Daily::Mod((%seed * 17) + 3, 9973);
	return %seed;
}

//------------------------------------------------------------------------------
// Gear ceiling: best equippable stats at remort R, from live item data.
// Walks every ItemData (getNumItems/getItemData, same as shopping.cs SetupShop),
// filters by $SkillRestriction, reads $AccessoryVar $SpecialVar pairs.
// Auto-includes new items with no table maintenance. Cached per remort value.
//------------------------------------------------------------------------------

// can this item be equipped by a max-progressed character of this remort/class/
// group? Mirrors SkillCanUse (skills.cs:862) - the REAL restriction engine -
// not DualWield's 4-word parser: all pairs, codes L/R/A/G/C/H, else skill id.
function Daily::ItemAllowedAtRemort(%item, %remort, %class, %group)
{
	%restr = $SkillRestriction[%item];
	if(%restr == "" || %restr == -1)
		return true;

	%cflag = 0;
	%cmatch = 0;
	for(%i = 0; GetWord(%restr, %i) != -1; %i += 2)
	{
		%s = GetWord(%restr, %i);
		%n = GetWord(%restr, %i + 1);

		if(%s == "L")
		{
			if(Daily::MaxLevelAtRemort(%remort) < %n)
				return false;
		}
		else if(%s == "R")
		{
			if(%remort < %n)
				return false;
		}
		else if(%s == "A")
			return false;   // admin-gated gear never counts toward the benchmark
		else if(%s == "H")
			return false;   // house-gated gear never counts (situational)
		else if(%s == "G")
		{
			%cflag++;
			if(String::ICompare(%group, %n) == 0)
				%cmatch = 1;
		}
		else if(%s == "C")
		{
			%cflag++;
			if(String::ICompare(%class, %n) == 0)
				%cmatch = 1;
		}
		else
		{
			// skill requirement vs the cap achievable at this remort
			if(Daily::SkillCapAtRemort(%remort) < %n)
				return false;
		}
	}
	if(%cflag > 0 && %cmatch == 0)
		return false;
	return true;
}

function Daily::BuildGearCeil(%remort, %class, %group)
{
	%key = %remort @ "_" @ %class @ "_" @ %group;
	if($Daily::GearCeilDone[%key])
		return %key;
	$Daily::GearCeilDone[%key] = true;

	// per-slot best-of-each-stat trackers (slight overestimate vs a single "best
	// item" per slot - fine for a ceiling, keeps this a one-pass walk)
	// weapon slots: 7 sword, 8 axe, 9 polearm, 10 bludgeon, 11 ranged, 12 projectile
	// armor slots:  1 ring, 2 body, 3 boots, 4 back, 5 shield, 6 talisman, 13 head, 14 belt
	%bestATK = 0;
	for(%s = 1; %s <= 14; %s++)
	{
		%slotDEF[%s] = 0;
		%slotHP[%s] = 0;
		%slotMDEF[%s] = 0;
		%slotMANA[%s] = 0;
	}

	%max = getNumItems();
	for(%z = 0; %z < %max; %z++)
	{
		%item = getItemData(%z);
		%slot = $AccessoryVar[%item, $AccessoryType];
		if(%slot == "" || %slot == -1)
			continue;
		if(!Daily::ItemAllowedAtRemort(%item, %remort, %class, %group))
			continue;

		%sv = $AccessoryVar[%item, $SpecialVar];
		if(%sv == "" || %sv == -1)
			continue;

		if(%slot >= 7 && %slot <= 12)
		{
			// weapon/projectile: ATK is the first pair's value when its index is 6
			// (mirrors fetchData("ATK"): GetRoll(GetWord(specialVar, 1)))
			if(GetWord(%sv, 0) == 6)
			{
				%v = GetWord(%sv, 1);
				if(%v > %bestATK)
					%bestATK = %v;
			}
		}
		else
		{
			// armor/accessory: scan stat pairs (3=MDEF, 4=HP, 5=Mana, 7=DEF)
			for(%i = 0; GetWord(%sv, %i) != -1; %i += 2)
			{
				%idx = GetWord(%sv, %i);
				%val = GetWord(%sv, %i + 1);
				if(%val == -1 || %val == "")
					break;
				if(%idx == 7 && %val > %slotDEF[%slot])
					%slotDEF[%slot] = %val;
				else if(%idx == 4 && %val > %slotHP[%slot])
					%slotHP[%slot] = %val;
				else if(%idx == 3 && %val > %slotMDEF[%slot])
					%slotMDEF[%slot] = %val;
				else if(%idx == 5 && %val > %slotMANA[%slot])
					%slotMANA[%slot] = %val;
			}
		}
	}

	%def = 0; %hp = 0; %mdef = 0; %mana = 0;
	for(%s = 1; %s <= 6; %s++)
	{
		%mult = 1;
		if(%s == 1)
			%mult = $Daily::RingSlots;
		%def += %slotDEF[%s] * %mult;
		%hp += %slotHP[%s] * %mult;
		%mdef += %slotMDEF[%s] * %mult;
		%mana += %slotMANA[%s] * %mult;
	}
	%def += %slotDEF[13] + %slotDEF[14];
	%hp += %slotHP[13] + %slotHP[14];
	%mdef += %slotMDEF[13] + %slotMDEF[14];
	%mana += %slotMANA[13] + %slotMANA[14];

	$Daily::GearCeil[%key, "ATK"] = %bestATK;
	$Daily::GearCeil[%key, "DEF"] = %def;
	$Daily::GearCeil[%key, "HP"] = %hp;
	$Daily::GearCeil[%key, "MDEF"] = %mdef;
	$Daily::GearCeil[%key, "MANA"] = %mana;

	echo("Daily: gear ceiling [" @ %key @ "]: ATK=" @ %bestATK @ " DEF=" @ %def @ " HP=" @ %hp @ " MDEF=" @ %mdef);
	return %key;
}

//------------------------------------------------------------------------------
// Benchmark: the player's stat formulas (rpgstats.cs fetchData) with actual
// level/skills but ceiling gear. Returns "MaxHP ATK DEF MDEF".
//------------------------------------------------------------------------------
function Daily::BenchStats(%clientId)
{
	%r = fetchData(%clientId, "RemortStep");
	if(%r == "" || %r == -1)
		%r = 0;
	%key = Daily::BuildGearCeil(%r, fetchData(%clientId, "CLASS"), fetchData(%clientId, "GROUP"));

	%lvl = fetchData(%clientId, "LVL");
	if(%lvl == "" || %lvl == -1)
		%lvl = 1;
	%end = $PlayerSkill[%clientId, $SkillEndurance];
	if(%end == "")
		%end = 0;
	%minhp = $MinHP[fetchData(%clientId, "RACE")];
	if(%minhp == "")
		%minhp = 12;

	// MaxHP mirror (rpgstats.cs:296): MinHP + End*0.6 + gearHP + R*End/8 + LVL
	%hp = floor(%minhp + (%end * 0.6) + $Daily::GearCeil[%key, "HP"] + floor(%r * (%end / 8)) + %lvl);
	// ATK mirror (rpgstats.cs:267): weaponATK + RemortStep
	%atk = floor($Daily::GearCeil[%key, "ATK"] + %r);
	// DEF/MDEF mirror (rpgstats.cs:210/235): gear + RemortStep*2
	%def = floor($Daily::GearCeil[%key, "DEF"] + (%r * 2));
	%mdef = floor($Daily::GearCeil[%key, "MDEF"] + (%r * 2));

	if($DAILY_DEBUG)
		echo("[DAILY DEBUG] BenchStats " @ Client::getName(%clientId) @ " R" @ %r @ " L" @ %lvl @ ": HP=" @ %hp @ " ATK=" @ %atk @ " DEF=" @ %def @ " MDEF=" @ %mdef);

	return %hp @ " " @ %atk @ " " @ %def @ " " @ %mdef;
}

//------------------------------------------------------------------------------
// Rotation
//------------------------------------------------------------------------------
function Daily::Rotate()
{
	%day = Daily::GetDayStamp();
	if(%day == $Daily::CurrentDay)
		return;
	$Daily::CurrentDay = %day;

	%seed = Daily::SeedFromDay(%day);
	// daily reward multiplier: 0.8 .. 1.5 in 0.1 steps
	$Daily::RewardMult = 0.8 + (Daily::Mod(%seed, 8) / 10);
	// fetch item pick per tier
	for(%t = 0; %t <= 4; %t++)
	{
		if($Daily::FetchPoolSize[%t] > 0)
			$Daily::FetchPick[%t] = Daily::Mod(%seed + (%t * 7), $Daily::FetchPoolSize[%t]);
		else
			$Daily::FetchPick[%t] = -1;
	}

	echo("Daily: rotated to day " @ %day @ " (seed " @ %seed @ ", reward x" @ $Daily::RewardMult @ ")");
}

function Daily::AnnounceNewDay()
{
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(!Player::isAiControlled(%cl))
			Client::sendMessage(%cl, $MsgBeige, "New daily quests are posted! Visit a Daily Herald or use #daily." );
	}
}

//------------------------------------------------------------------------------
// Contracts
//------------------------------------------------------------------------------

// today's fetch pool entry for THIS player: "ItemName count" ("" if tier empty)
function Daily::FetchItemFor(%clientId)
{
	%r = fetchData(%clientId, "RemortStep");
	if(%r == "" || %r == -1)
		%r = 0;
	%tier = Daily::TierForRemort(%r);
	%pick = $Daily::FetchPick[%tier];
	if(%pick == -1 || %pick == "")
		return "";
	%entry = $Daily::FetchPool[%tier, %pick];
	if(%entry == "")
		return "";
	%count = GetWord(%entry, 1);
	if(%count == -1 || %count == "" || %count <= 0)
		%count = $Daily::FetchCount;
	return GetWord(%entry, 0) @ " " @ %count;
}

// themes: "Fetch" | "Cull" | "Elite"
function Daily::Accept(%clientId, %theme)
{
	if($Daily::CurrentDay == "")
		Daily::Rotate();

	if(%theme == "Elite" && !$Daily::EliteEnabled)
	{
		Client::sendMessage(%clientId, $MsgBeige, "The elite hunt is not available yet.");
		return false;
	}

	if(fetchData(%clientId, "DailyDone" @ %theme) == $Daily::CurrentDay)
	{
		Client::sendMessage(%clientId, $MsgBeige, "You have already completed today's " @ %theme @ " daily. Come back tomorrow!");
		return false;
	}

	// one active contract at a time
	%activeTheme = fetchData(%clientId, "DailyTheme");
	if(fetchData(%clientId, "DailyDay") == $Daily::CurrentDay && %activeTheme != "" && %activeTheme != -1)
	{
		Client::sendMessage(%clientId, $MsgBeige, "You already have an active " @ %activeTheme @ " daily. Finish or abandon it first (#daily abandon).");
		return false;
	}

	if(%theme == "Fetch")
	{
		%entry = Daily::FetchItemFor(%clientId);
		if(%entry == "")
		{
			Client::sendMessage(%clientId, $MsgBeige, "No fetch work today for someone of your standing - try the cull.");
			return false;
		}
		%item = GetWord(%entry, 0);
		%count = GetWord(%entry, 1);
		storeData(%clientId, "DailyTargetItem", %item);
		storeData(%clientId, "DailyTargetCount", %count);
		Client::sendMessage(%clientId, $MsgGreen, "Daily accepted: bring me " @ %count @ " x " @ %item @ ".");
	}
	else if(%theme == "Cull")
	{
		storeData(%clientId, "DailyProgress", 0);
		Client::sendMessage(%clientId, $MsgGreen, "Daily accepted: cull monsters worthy of your strength. Progress shows as you kill (see #daily).");
	}
	else if(%theme == "Elite")
	{
		// Daily::SpawnElite(%clientId) - lands with the scaled elite bot
		storeData(%clientId, "DailyProgress", 0);
		Client::sendMessage(%clientId, $MsgGreen, "Daily accepted: slay the elite.");
	}
	else
		return false;

	storeData(%clientId, "DailyDay", $Daily::CurrentDay);
	storeData(%clientId, "DailyTheme", %theme);
	return true;
}

function Daily::Abandon(%clientId)
{
	%theme = fetchData(%clientId, "DailyTheme");
	if(%theme == "" || %theme == -1 || fetchData(%clientId, "DailyDay") != $Daily::CurrentDay)
	{
		Client::sendMessage(%clientId, $MsgBeige, "You have no active daily.");
		return;
	}
	storeData(%clientId, "DailyTheme", "");
	storeData(%clientId, "DailyProgress", 0);
	storeData(%clientId, "DailyTargetItem", "");
	Client::sendMessage(%clientId, $MsgBeige, "Daily abandoned. You can accept it again today.");
}

// kill-credit hook - call for real-player killers of enemy bots only
// (call site in the kill pipeline already filters townbots/players)
function Daily::OnKill(%killer, %victim)
{
	if($Daily::CurrentDay == "")
		return;
	if(fetchData(%killer, "DailyDay") != $Daily::CurrentDay)
		return;
	if(fetchData(%killer, "DailyTheme") != "Cull")
		return;
	%p = fetchData(%killer, "DailyProgress");
	if(%p == "" || %p == -1)
		%p = 0;
	if(%p >= 100)
		return;

	%benchHP = GetWord(Daily::BenchStats(%killer), 0);
	if(%benchHP <= 0)
		return;
	%vHP = fetchData(%victim, "MaxHP");
	if(%vHP == "" || %vHP == -1 || %vHP <= 0)
		return;

	// credit proportional to victim toughness vs the killer's benchmark:
	// strong-for-you monsters pay a lot, greys pay a trickle
	%credit = floor((%vHP * 100) / (%benchHP * $Daily::CullCreditFactor));
	if(%credit < 1)
		%credit = 1;
	if(%credit > $Daily::CullMaxCreditPerKill)
		%credit = $Daily::CullMaxCreditPerKill;

	%p = %p + %credit;
	if(%p > 100)
		%p = 100;
	storeData(%killer, "DailyProgress", %p);

	if(%p >= 100)
		Client::sendMessage(%killer, $MsgGreen, "Daily cull COMPLETE! Return to a Daily Herald for your reward.");
	else
		Client::sendMessage(%killer, $MsgBeige, "Daily cull: " @ %p @ "% (+" @ %credit @ ")");
}

// turn-in at the Herald; returns true if a reward was paid
function Daily::TryTurnIn(%clientId)
{
	%theme = fetchData(%clientId, "DailyTheme");
	if(%theme == "" || %theme == -1 || fetchData(%clientId, "DailyDay") != $Daily::CurrentDay)
		return false;

	if(%theme == "Fetch")
	{
		%item = fetchData(%clientId, "DailyTargetItem");
		%count = fetchData(%clientId, "DailyTargetCount");
		if(%item == "" || %item == -1)
			return false;
		if(HasThisStuff(%clientId, %item @ " " @ %count) != True)
			return false;
		TakeThisStuff(%clientId, %item @ " " @ %count, True);
		Daily::GrantReward(%clientId, "Fetch");
		return true;
	}
	else if(%theme == "Cull" || %theme == "Elite")
	{
		if(fetchData(%clientId, "DailyProgress") < 100)
			return false;
		Daily::GrantReward(%clientId, %theme);
		return true;
	}
	return false;
}

function Daily::GrantReward(%clientId, %theme)
{
	// EXP: a percentage of the CURRENT level's cost - self-scales at any remort.
	// Coins: remort-scaled. Both x the daily multiplier. All inputs are the same
	// non-manipulable ones the benchmark uses, so sandbagging is pointless.
	%lvl = fetchData(%clientId, "LVL");
	%expCost = GetExp(%lvl + 1, %clientId) - GetExp(%lvl, %clientId);
	if(%expCost < 0)
		%expCost = 0;
	%exp = floor(%expCost * ($Daily::ExpRewardPct / 100) * $Daily::RewardMult);

	%r = fetchData(%clientId, "RemortStep");
	if(%r == "" || %r == -1)
		%r = 0;
	%coins = floor($Daily::CoinBase * (1 + (%r * $Daily::CoinPerRemort)) * $Daily::RewardMult);

	storeData(%clientId, "EXP", %exp, "inc");
	storeData(%clientId, "COINS", %coins, "inc");
	storeData(%clientId, "DailyDone" @ %theme, $Daily::CurrentDay);
	storeData(%clientId, "DailyTheme", "");
	storeData(%clientId, "DailyProgress", 0);
	storeData(%clientId, "DailyTargetItem", "");
	Game::refreshClientScore(%clientId);

	// always-on audit line (item/exp/coin grants are restore-from-console data)
	echo("[DAILY REWARD] " @ Client::getName(%clientId) @ " (" @ %clientId @ ") completed " @ %theme @ " day " @ $Daily::CurrentDay @ ": +" @ %exp @ " exp, +" @ %coins @ " coins");
	Client::sendMessage(%clientId, $MsgGreen, "Daily complete! Reward: " @ %exp @ " exp and " @ %coins @ " coins.");
}

//------------------------------------------------------------------------------
// Status (#daily)
//------------------------------------------------------------------------------
function Daily::Status(%clientId)
{
	if($Daily::CurrentDay == "")
		Daily::Rotate();

	Client::sendMessage(%clientId, $MsgBeige, "--- Daily Quests (day " @ $Daily::CurrentDay @ ", rewards x" @ $Daily::RewardMult @ ") ---");

	%activeTheme = fetchData(%clientId, "DailyTheme");
	if(fetchData(%clientId, "DailyDay") != $Daily::CurrentDay)
		%activeTheme = "";

	// Fetch
	%fetchEntry = Daily::FetchItemFor(%clientId);
	if(fetchData(%clientId, "DailyDoneFetch") == $Daily::CurrentDay)
		Client::sendMessage(%clientId, 0, "Fetch: COMPLETE");
	else if(%activeTheme == "Fetch")
		Client::sendMessage(%clientId, 0, "Fetch: ACTIVE - bring " @ fetchData(%clientId, "DailyTargetCount") @ " x " @ fetchData(%clientId, "DailyTargetItem") @ " to a Herald");
	else if(%fetchEntry == "")
		Client::sendMessage(%clientId, 0, "Fetch: (no fetch work for your tier today)");
	else
		Client::sendMessage(%clientId, 0, "Fetch: available - " @ GetWord(%fetchEntry, 1) @ " x " @ GetWord(%fetchEntry, 0));

	// Cull
	if(fetchData(%clientId, "DailyDoneCull") == $Daily::CurrentDay)
		Client::sendMessage(%clientId, 0, "Cull: COMPLETE");
	else if(%activeTheme == "Cull")
		Client::sendMessage(%clientId, 0, "Cull: ACTIVE - " @ fetchData(%clientId, "DailyProgress") @ "% culled");
	else
		Client::sendMessage(%clientId, 0, "Cull: available - slay monsters worthy of your strength");

	// Elite
	if(!$Daily::EliteEnabled)
		Client::sendMessage(%clientId, 0, "Elite: (coming soon)");
	else if(fetchData(%clientId, "DailyDoneElite") == $Daily::CurrentDay)
		Client::sendMessage(%clientId, 0, "Elite: COMPLETE");
	else if(%activeTheme == "Elite")
		Client::sendMessage(%clientId, 0, "Elite: ACTIVE");
	else
		Client::sendMessage(%clientId, 0, "Elite: available");

	Client::sendMessage(%clientId, $MsgBeige, "Visit a Daily Herald to accept or turn in. #daily abandon drops your active daily.");
}

//------------------------------------------------------------------------------
// Init + rotation loop (start from Mission::init ONLY - exec-time schedules are
// flushed by Server::loadMission, same rule as Game::StartVisibilitySafetyLoop)
//------------------------------------------------------------------------------
function Daily::Init()
{
	if($Daily::LoopStarted)
		return;
	$Daily::LoopStarted = true;

	Daily::ProbeRealDate();
	if(!$Daily::HasRealDate)
	{
		$Daily::FallbackDay = 1;
		$Daily::FallbackTicks = 0;
	}
	Daily::Rotate();
	schedule("Daily::CheckRotateLoop();", 300);
}

function Daily::CheckRotateLoop()
{
	if(!$Daily::HasRealDate)
	{
		// 288 x 300s = 24h of uptime per fallback day
		$Daily::FallbackTicks++;
		if($Daily::FallbackTicks >= 288)
		{
			$Daily::FallbackTicks = 0;
			$Daily::FallbackDay++;
		}
	}

	if(Daily::GetDayStamp() != $Daily::CurrentDay)
	{
		Daily::Rotate();
		Daily::AnnounceNewDay();
	}
	schedule("Daily::CheckRotateLoop();", 300);
}
