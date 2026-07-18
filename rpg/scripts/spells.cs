//============================================================================
// spells.cs — SHELL (was 4,155 lines; split 2026-07-17 at commit 4aa0a3d)
// Mechanical text move, no behavior change. Tombstone / line-range map:
//   original lines 1-1313    -> spells_data.cs   (constants + $Spell::* tables + helpers)
//   original lines 1314-4155 -> spells_engine.cs (BeginCastSpell..setCommandStatus; DoCastSpell kept whole)
// Server.cs exec slot 268 unchanged. Refactors never delete code.
//============================================================================
exec("spells_data");
exec("spells_engine");
