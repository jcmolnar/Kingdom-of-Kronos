//============================================================================
// rpgfunk.cs — SHELL (was ~9,090 lines; 5-way scatter split 2026-07-18 at 5118b1b)
// Mechanical text move, no behavior change. Topic map:
//   rpgfunk_util.cs    — string/number/list/display leaf utilities + debug flags (exec FIRST)
//   rpgfunk_persist.cs — SaveCharacter/LoadCharacter + bank-storage chunking + server time + Clear*
//   rpgfunk_world.cs   — world save/load + deployable rehydrators + lootbags + house objectives + weather/sky
//   rpgfunk_player.cs  — RefreshAll/GiveThisStuff/UpdateAppearance + team/race + requirements (HOT PATHS)
//   rpgfunk_admin.cs   — shutdown/countdown + AFK-zone enforcement + generators
// MUST stay at Server.cs exec slot 257 (before KronosHUD_Server @341: its
// Commafy deliberately overrides ours — later exec wins). Loop starters
// (StartLootbagAggregation/StartAFKZoneEnforcement/InitObjectives) are called
// from Server.cs post-loadMission — never add top-level calls here.
// Refactors never delete code; per-range map in the split commit message + git history.
//============================================================================
exec("rpgfunk_util");
exec("rpgfunk_persist");
exec("rpgfunk_world");
exec("rpgfunk_player");
exec("rpgfunk_admin");
