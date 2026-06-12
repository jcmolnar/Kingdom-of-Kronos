//==============================================
// KronosHUD_Server.cs - Server-side HUD stat push
//==============================================
// Pushes RPG stats to clients for the ScriptGL KronosHUD.
// Vitals (HP, Mana, EXP, Gold, Level) are sent via remoteEval
// when they change. Metadata (Class, Zone) sent in a second call
// to avoid space-in-string issues with remoteEval arg parsing.
//
//----------------------------------------------
// HOOKS / INTEGRATIONS:
//----------------------------------------------
// Server.cs           - exec(KronosHUD_Server); added to load this script
// rpgstats.cs         - Game::refreshClientScore() calls KronosHUD_Push()
// rpgfunk.cs          - RefreshAll() calls KronosHUD_Push()
// playerdamage.cs     - Damage path calls KronosHUD_PushTarget()
//==============================================

// ============================================
// Main stat push - sends all RPG stats to client
// ============================================

function KronosHUD_Push(%clientId)
{
	// Watchdog: revive the LOS scan loop if it isn't running (mission
	// load flushes schedules, killing any loop started before/during it)
	if(getSimTime() - $KronosHUD::LOSLastTick > 5)
		KronosHUD_StartLOSScan();

	// Skip bots entirely
	if(Player::isAiControlled(%clientId))
		return;
	if(isRPGAI(%clientId))
		return;

	// Skip disconnected clients
	%clientName = Client::getName(%clientId);
	if(%clientName == "" || %clientName == -1)
		return;

	// Skip if player hasn't loaded and spawned yet
	if(!fetchData(%clientId, "HasLoadedAndSpawned"))
		return;

	// Gather vitals
	%hp = fetchData(%clientId, "HP");
	%maxHp = fetchData(%clientId, "MaxHP");
	%mana = fetchData(%clientId, "MANA");
	%maxMana = fetchData(%clientId, "MaxMANA");
	%exp = fetchData(%clientId, "EXP");
	%lvl = fetchData(%clientId, "LVL");
	%xpCur = GetExp(%lvl, %clientId);
	%nextLvl = %lvl + 1;
	%xpNext = GetExp(%nextLvl, %clientId);
	%gold = fetchData(%clientId, "COINS");
	%remort = fetchData(%clientId, "RemortStep");
	if(%remort == "" || %remort == -1)
		%remort = 0;

	// Push vitals (all numeric - safe for remoteEval)
	remoteEval(%clientId, "KronosHUD", %hp, %maxHp, %mana, %maxMana, %exp, %xpCur, %xpNext, %gold, %lvl, %remort);

	// Push metadata (class and zone may contain spaces - sent separately)
	%class = getFinalCLASS(%clientId);
	if(%class == "" || %class == -1)
		%class = "Unknown";
	%zone = Zone::getDesc(fetchData(%clientId, "zone"));
	if(%zone == "" || %zone == -1)
		%zone = "Unknown";

	remoteEval(%clientId, "KronosHUD2", %class, %zone);
}

// ============================================
// Target frame push - sends enemy info to attacker
// ============================================

// %damage (optional): damage just dealt - shown as a hit number on the
// client's target frame. Empty for LOS-scan pushes, "LCK" for LCK hits.
function KronosHUD_PushTarget(%shooterClient, %damagedClient, %damage)
{
	// Skip if shooter is a bot
	if(Player::isAiControlled(%shooterClient))
		return;
	if(isRPGAI(%shooterClient))
		return;

	// Skip disconnected clients
	if(Client::getName(%shooterClient) == "" || Client::getName(%shooterClient) == -1)
		return;

	// Get target name (GetClientOrBotName resolves bot names properly)
	%targetName = GetClientOrBotName(%damagedClient);
	if(%targetName == "" || %targetName == -1)
		%targetName = "Unknown";

	// Calculate target HP percentage
	%hp = fetchData(%damagedClient, "HP");
	%maxHp = fetchData(%damagedClient, "MaxHP");
	%targetHpPct = 0;
	if(%maxHp > 0)
		%targetHpPct = floor((%hp * 100) / %maxHp);
	if(%targetHpPct < 0)
		%targetHpPct = 0;
	if(%targetHpPct > 100)
		%targetHpPct = 100;

	remoteEval(%shooterClient, "KronosTarget", %targetName, %targetHpPct, %damage);
}

// ============================================
// TAB menu info box (bottom score-screen window)
// ============================================
// Fills the stock InfoCtrlBox (6 setInfoLine rows) - an engine
// control every client has, so this works for vanilla clients too.
// Own stats show when the menu opens; clicking a player name on the
// scoreboard shows that player instead (via remoteSelectClient).

// Player's own stats - called from Game::menuRequest when no player
// is selected on the scoreboard
function KronosMenu_SendOwnInfo(%clientId)
{
	if(Player::isAiControlled(%clientId))
		return;
	if(Client::getName(%clientId) == "" || Client::getName(%clientId) == -1)
		return;

	%remort = fetchData(%clientId, "RemortStep");
	if(%remort == "" || %remort == -1)
		%remort = 0;

	%exp = fetchData(%clientId, "EXP");
	%expNeed = GetExp(GetLevel(%exp, %clientId) + 1, %clientId) - %exp;

	%coins = fetchData(%clientId, "COINS");
	%bank = fetchData(%clientId, "BANK");

	%weight = round(fetchData(%clientId, "Weight") * 10) / 10;
	%maxWeight = round(fetchData(%clientId, "MaxWeight") * 10) / 10;

	remoteEval(%clientId, "setInfoLine", 1, Client::getName(%clientId) @ " - Lv " @ fetchData(%clientId, "LVL") @ " " @ getFinalCLASS(%clientId) @ " RL" @ %remort);
	remoteEval(%clientId, "setInfoLine", 2, "ATK " @ fetchData(%clientId, "ATK") @ "   DEF " @ fetchData(%clientId, "DEF") @ "   MDEF " @ fetchData(%clientId, "MDEF") @ "   LCK " @ fetchData(%clientId, "LCK"));
	remoteEval(%clientId, "setInfoLine", 3, "HP " @ fetchData(%clientId, "HP") @ "/" @ fetchData(%clientId, "MaxHP") @ "   MP " @ fetchData(%clientId, "MANA") @ "/" @ fetchData(%clientId, "MaxMANA"));
	remoteEval(%clientId, "setInfoLine", 4, "EXP " @ %exp @ "   (Need " @ %expNeed @ ")");
	remoteEval(%clientId, "setInfoLine", 5, "Coins " @ FormatLargeNumber(%coins) @ "   Bank " @ FormatLargeNumber(%bank) @ "   Total " @ FormatLargeNumber(%coins + %bank));
	remoteEval(%clientId, "setInfoLine", 6, "Weight " @ %weight @ " / " @ %maxWeight);
}

// Player list for the modern TAB menu - REQUEST-DRIVEN: only clients
// running KronosMenu.cs ask for it (remoteEval(2048, KMGetPlayers) on
// every menu open), so vanilla clients never receive these pushes
// (their stock engine scoreboard still works untouched).
$KronosMenu::MaxListRows = 16; // must match $KM::MaxPRows client-side

function remoteKMGetPlayers(%clientId)
{
	if(Player::isAiControlled(%clientId))
		return;
	if(isRPGAI(%clientId))
		return;
	if(Client::getName(%clientId) == "" || Client::getName(%clientId) == -1)
		return;

	%sent = 0;
	%total = 0;
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		if(Player::isAiControlled(%cl))
			continue;
		if(isRPGAI(%cl))
			continue;
		if(Client::getName(%cl) == "" || Client::getName(%cl) == -1)
			continue;

		%total++;
		if(%sent >= $KronosMenu::MaxListRows)
			continue;

		%remort = fetchData(%cl, "RemortStep");
		if(%remort == "" || %remort == -1)
			%remort = 0;

		// name last - it may contain spaces
		remoteEval(%clientId, "KMPlayer", %sent, %cl, fetchData(%cl, "LVL"), %remort, getFinalCLASS(%cl), Client::getName(%cl));
		%sent++;
	}
	remoteEval(%clientId, "KMPlayerCount", %sent, %total);
}

// Another player's public info - called from remoteSelectClient when
// a player name is clicked on the scoreboard
function KronosMenu_SendPlayerInfo(%clientId, %selId)
{
	if(Client::getName(%clientId) == "" || Client::getName(%clientId) == -1)
		return;

	%remort = fetchData(%selId, "RemortStep");
	if(%remort == "" || %remort == -1)
		%remort = 0;

	%house = fetchData(%selId, "MyHouse");
	if(%house == "" || %house == "0" || %house == "House0" || %house == "house0")
		%house = "None";

	%zone = Zone::getDesc(fetchData(%selId, "zone"));
	if(%zone == "" || %zone == -1)
		%zone = "Unknown";

	remoteEval(%clientId, "setInfoLine", 1, "Player: " @ Client::getName(%selId));
	remoteEval(%clientId, "setInfoLine", 2, "Lv " @ fetchData(%selId, "LVL") @ " " @ getFinalCLASS(%selId) @ " RL" @ %remort);
	remoteEval(%clientId, "setInfoLine", 3, "House: " @ %house);
	remoteEval(%clientId, "setInfoLine", 4, "Zone: " @ %zone);
	remoteEval(%clientId, "setInfoLine", 5, "HP " @ fetchData(%selId, "HP") @ "/" @ fetchData(%selId, "MaxHP") @ "   MP " @ fetchData(%selId, "MANA") @ "/" @ fetchData(%selId, "MaxMANA"));
	remoteEval(%clientId, "setInfoLine", 6, "");
}

// ============================================
// LOS target scan - target frame appears when
// a player looks at another player or enemy bot
// ============================================
// Periodic raycast down each player's view. Throttled via
// $KronosHUD::LOSScanPeriod. Town bots are skipped (immune to
// damage, a red enemy frame would be misleading).

$KronosHUD::LOSScanPeriod = 0.5; // seconds between scans
$KronosHUD::LOSRange = 120;      // meters
$KronosHUD::LOSDebug = false;    // set true at server console to trace the scan

function KronosHUD_LOSScan(%gen)
{
	// Generation guard: re-exec'ing this file bumps the generation,
	// which kills any previously scheduled scan loop
	if(%gen != $KronosHUD::LOSGen)
		return;
	schedule("KronosHUD_LOSScan(" @ %gen @ ");", $KronosHUD::LOSScanPeriod);
	$KronosHUD::LOSLastTick = getSimTime();

	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
	{
		// Real, spawned, living players only
		if(Player::isAiControlled(%cl))
			continue;
		if(!fetchData(%cl, "HasLoadedAndSpawned"))
			continue;
		if(IsDead(%cl))
			continue;
		%pobj = Client::getOwnedObject(%cl);
		if(%pobj == "" || %pobj == -1 || %pobj == 0)
			continue;

		// Raycast down the player's view
		if(!GameBase::getLOSInfo(%pobj, $KronosHUD::LOSRange))
		{
			if($KronosHUD::LOSDebug)
				echo("[KronosHUD LOS] " @ %cl @ ": no LOS hit within range");
			continue;
		}
		if(getObjectType($los::object) != "Player")
		{
			if($KronosHUD::LOSDebug)
				echo("[KronosHUD LOS] " @ %cl @ ": hit " @ getObjectType($los::object) @ " (" @ $los::object @ ")");
			continue;
		}

		// GetClientIdFromPlayerObject works for bots too
		// (Player::getClient returns -1 for AI)
		%targetId = GetClientIdFromPlayerObject($los::object);
		if(%targetId == "" || %targetId == -1 || %targetId == %cl)
			continue;
		if(IsDead(%targetId))
			continue;
		if(isTownBot(%targetId))
		{
			if($KronosHUD::LOSDebug)
				echo("[KronosHUD LOS] " @ %cl @ ": target " @ %targetId @ " is a town bot, skipped");
			continue;
		}

		if($KronosHUD::LOSDebug)
			echo("[KronosHUD LOS] " @ %cl @ " -> target " @ %targetId @ " (" @ GetClientOrBotName(%targetId) @ ")");
		KronosHUD_PushTarget(%cl, %targetId);
	}
}

// Start (or restart) the scan loop. Generation counter kills any
// previously scheduled loop, so calling this twice is safe.
function KronosHUD_StartLOSScan()
{
	$KronosHUD::LOSGen++;
	$KronosHUD::LOSLastTick = getSimTime();
	echo("KronosHUD: LOS scan starting (gen " @ $KronosHUD::LOSGen @ ")");
	KronosHUD_LOSScan($KronosHUD::LOSGen);
}

// NOTE: Do NOT schedule the scan at exec time - Tribes flushes pending
// schedules when the mission loads, which silently kills the loop.
// Instead the loop is started/revived by the watchdog in KronosHUD_Push,
// which fires constantly once players are in the game.

echo("KronosHUD_Server: stat push system loaded");
