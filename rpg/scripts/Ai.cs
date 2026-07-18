//============================================================================
// Ai.cs — SHELL (was 14,037 lines; 4-way scatter split 2026-07-18 at 320698f)
// Mechanical text move, no behavior change. Topic map:
//   ai_safeguards.cs — safeguards + shared machinery (93 fns; clientId pool
//                      guards, allocators, registry, cleanup) — LOAD-BEARING
//   ai_enemybots.cs  — hostile-bot combat AI (17 fns)
//   ai_townbots.cs   — townbot/NPC + zone spawn systems (28 fns)
//   ai_tempspawn.cs  — command-spawn front door (3 fns; TempSpawn is a
//                      commandIssuer flavor — enemy SpawnPoint spawns enter here too)
// Server.cs exec slot 256 unchanged. Hot-reload: re-exec THIS file to reload all
// four parts. Safeguard experiment: exec("ai_safeguards_off") AFTER boot to stub
// the protection gates; exec("ai_safeguards") restores them live.
// Refactors never delete code; per-range map in the split commit + git history.
//============================================================================
exec("ai_safeguards");
exec("ai_enemybots");
exec("ai_townbots");
exec("ai_tempspawn");
