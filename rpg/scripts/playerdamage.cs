//============================================================================
// playerdamage.cs — SHELL (was 5,349 lines; split 2026-07-17 at commit 4aa0a3d)
// Mechanical text move, no behavior change. Tombstone / line-range map:
//   original lines 6-305, 399-580        -> playerdamage_util.cs  (helpers)
//   original lines 1-5, 306-398, 581-2924 -> playerdamage_death.cs (Client::onKilled, Game::clientKilled, Player::onKilled)
//   original lines 2925-5349             -> playerdamage_core.cs  (Player::onDamage + mythic + remoteKill)
// Both mega-functions kept whole; every townbot-immunity guard is inside
// Player::onDamage and is untouched. Server.cs exec slot 298 unchanged
// (keeps Client::onKilled / Game::clientKilled winners over game.cs).
// Refactors never delete code.
//============================================================================
exec("playerdamage_util");
exec("playerdamage_death");
exec("playerdamage_core");
