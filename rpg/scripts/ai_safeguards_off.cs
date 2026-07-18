//============================================================================
// ai_safeguards_off.cs — EXPERIMENT OVERLAY: disables the AI player-protection
// gates. NOT exec'd by anything by default. DO NOT add to Server.cs or the
// Ai.cs shell. DEV SERVER ONLY.
//
// Purpose: test whether the clientId safeguards are still necessary. Townbots,
// enemy bots, and human players share the 2049-2175 clientId pool (2048 is the
// server slot); these gates exist against real player-hijack / black-screen
// incidents. With them stubbed, a bot CAN hijack or delete a real player's
// object — that is the failure mode being tested for.
//
// Usage (live, no reboot — fear script is last-exec-wins, resolved at call time):
//   exec("ai_safeguards_off");   <- protection OFF (these stubs win)
//   exec("ai_safeguards");       <- protection ON  (real bodies win again)
//
// Stub polarity (verified against callers): guards call
//   if(IsRealPlayer(x)) return;      -> stub returns false = "never a real player"
//   if(!IsSafeTo*(x))   return;      -> stub returns true  = "always safe"
// so every gate becomes pass-through. Side effect under test: the real bodies
// also call PlayerManager::reserveId() when they detect a real player; the
// stubs skip that reservation.
//
// Deliberately NOT stubbed: allocators (getAInumber/setAInumber,
// SpawnAIGetClientId, createAI), registry mutators, classifiers
// (isTownBot/isEnemyBot/Bot_DetermineType), graveyard, ghost/stale cleanup,
// and Bot_IsRealPlayer — stubbing those breaks spawning or leaks ids rather
// than testing player protection. The periodic scans have their own prefs:
// $GhostBotCleanupEnabled / $AINumberReconciliationEnabled ($Debug::SafeGuards
// is logging-only, not a behavior switch).
//============================================================================

function IsRealPlayer(%clientId)
{
	return false;
}

function IsSafeToModify(%clientId, %operation)
{
	return true;
}

function IsSafeToModifyForEnemyBot(%clientId, %operation)
{
	return true;
}

function IsSafeToDeletePlayerObject(%playerObj, %clientId, %operation)
{
	return true;
}

echo("*** AI SAFEGUARDS DISABLED (ai_safeguards_off.cs) — exec(\"ai_safeguards\"); to restore ***");
