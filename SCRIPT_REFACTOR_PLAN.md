# Big-Script Split/Refactor Plan (2026-07-17)

Synthesized from a 7-agent structural analysis (exec graph, duplicate definitions,
exec-time side effects, coupling, churn) of the 11 largest files in `rpg\scripts\`.
Nothing has been changed yet — this is the plan for review.

---

## Ground truth that makes splitting safe here

1. **Single load point.** Everything execs from one flat, unconditional list in
   `createServer` at `Server.cs:254-352`. Armor files chain through
   `armordata.cs:31-34` (HumanArmors → EnemyArmors → SpecialArmors). Mission
   change does NOT re-run the exec list.
2. **Zero `package` blocks** anywhere in `rpg\scripts` — overrides are pure
   exec-order last-wins.
3. **Zero top-level `schedule()`** in all 11 targets. All periodic loops are
   started from `Server.cs` post-loadMission (the old exec-time schedules were
   already refactored out — see comment at `Server.cs:427-431`). The
   mission-load schedule-flush hazard does not apply.
4. **Zero within-file duplicate function definitions** in all 11 targets — a
   split cannot invert a self-override because none exist.
5. **Only 3 live cross-file overrides** to respect:
   - `messageAll` — comchat.cs:12132 (3-param) must keep beating game.cs:209
     (2-param). Any comchat piece stays exec'd AFTER game.cs (Server.cs:263).
   - `Commafy` — KronosHUD_Server.cs:434 must keep beating rpgfunk.cs:7813.
     Any rpgfunk piece stays exec'd BEFORE KronosHUD_Server (Server.cs:341).
   - `Game::onPlayerConnected` (game.cs vs connectivity.cs) — not our files.
6. **Deploy tolerates splits cheaply.** Live deploy copies the whole `scripts\`
   folder atomically ("never cherry-pick"). None of the 11 targets has a
   sanitized twin. BUT `Server.cs` HAS a hand-maintained sanitized twin
   (`sanitized of new weapons\Server.cs`) — so we avoid editing Server.cs at all
   (see recipe).

## The mechanical recipe (used by every split below)

**Shell-file pattern** — the same pattern armordata.cs already uses:

1. Cut the original into part files by **contiguous line ranges on function
   boundaries**, preserving order exactly. Never scatter related definitions.
2. Replace the original file's body with a header comment + `exec(part1); ...
   exec(partK);` in order. The original filename survives as the shell, so
   **`Server.cs` is untouched** (no sanitized-twin mirroring, exec slot and all
   override winners preserved automatically).
3. Tombstone comments per project convention: the shell lists what moved where
   and when; each part file header states its source range.
4. One split = one commit. No behavior changes mixed in.

**Verification gate per split (all of these):**
- Function-count parity: `grep -c '^function'` over shell+parts must equal the
  original's count; zero duplicate names across the parts.
- Clean boot: console.log free of `Unable to find function` /
  `exec: file not found` / `Unknown command` (DEPLOY_CHECKLIST §4 gate).
- `$global` spot-checks via echo (each split lists its own).
- Per-file in-game smoke checklist (listed with each split).
- Rollback = `git revert` of the one commit.

---

## Verdicts at a glance

| File | Lines | Verdict | Phase |
|---|---|---|---|
| spells.cs | 4,155 | SPLIT 2-way (data / engine) | 1 (prove mechanic) |
| weapons.cs | 3,226 | SPLIT 2-way (combat+globals / datablocks) — index-safe strategy ONLY | 1 |
| playerdamage.cs | 5,349 | SPLIT 3-way (util / damage / death) | 1 |
| rpgfunk.cs | 9,072 | SPLIT 5-way (util / persist / world / player / admin) — the big win | 2 |
| ai.cs | 14,037 | Conservative 3-way possible (~1,270 lines out, 92% stays) — OPTIONAL | 3 |
| EnemyArmors.cs | 3,560 | Split bot-table vs datablocks has merit — DEFER until Void/Infinite balancing settles | 3 |
| Belt.cs | 5,470 | DEFER (active dev + exec-time item registry hazard). Fallback: UI-layer extraction | 3 |
| comchat.cs | 12,383 | CANNOT be text-split (one 12,040-line function). Real fix = staged handler-extraction refactor — separate opt-in project | separate |
| remortseal.cs | 2,927 | DO NOT SPLIT (one cohesive SealBattle feature) | — |
| HumanArmors.cs | 7,964 | DO NOT SPLIT (uniform data table; owns `$damageScale`/`$jumpSurfaceMinDot` that EnemyArmors needs) | — |
| MonsterAdminArmors.cs | 751 | DO NOT SPLIT (small, cohesive) | — |
| RaceArmor.cs | 3,665 | DEAD CODE — never exec'd. Do not touch, do not revive (its `skel` datablocks would override live mage armors) | — |

Churn since Apr 1 (higher = riskier to split right now): rpgfunk 21, Belt 17,
comchat 11, Ai 10, playerdamage 7, spells/weapons/remortseal 3, EnemyArmors 2,
HumanArmors 1.

---

## Phase 1 — prove the mechanic on quiet files

### 1a. spells.cs → spells.cs (shell) + 2 parts
- **`spells_data.cs`** = lines 1–1312: type constants, `$SPELL_DEBUG`,
  `$SpellRingBomb[]`, `$Spell::damageBufferWindow[]`, the casting-engine helper
  functions (22–202), and the full `$Spell::*` table block (204–1312). Owns 100%
  of exec-time statements.
- **`spells_engine.cs`** = lines 1314–4155: BeginCastSpell, DoCastSpell_Silent,
  **DoCastSpell kept whole** (1543–3123 — flat dispatch over shared locals with 3
  function defs textually embedded mid-body at 2876/2902/2916; internally
  unsplittable, cutting it risks brace imbalance killing ALL spells), bomb/
  Apocalypse/Tornado engines, target-cache/damage-buffer, DoBoxFunction,
  SpellCanCast. Zero top-level statements.
- **Smoke:** cast one of each category (firebomb, heal, shield, transport,
  remort at cap boundary, Apocalypse 48, Tornado 46, Ion Blast 50, PowerCloud 7
  full multi-wave + cleanup); Liquifier 51 spawns heat-laser turrets; `#help
  spells` table renders.

### 1b. weapons.cs → weapons.cs (datablocks, unmoved) + 1 part
**CONFIRMED: item indices are positional** — assigned sequentially as each
`ItemData` registers, in exec order (no numeric field; the SLOT PURGE comment at
weapons.cs:3222 "reclaims 12 of the ~256 indices" proves it). This is what the
>200-index buy crash and VirtualItems by-index replacement are sensitive to.
Therefore the ONLY safe strategy:
- **`weapons_combat.cs`** (new) = lines 1–852 (all `$AccessoryVar`/range/delay/
  cost tables + MeleeAttack/VoidWeaponAttack/ProjectileAttack/PickAxeSwing/
  PostAttack/DoRandomMining/GetRange/GetDelay + GenerateItemCost/
  GenerateAllWeaponCosts) + lines 3047–3220 (DPS diagnostics).
- **`weapons.cs`** keeps ONLY the ~50 weapon datablocks (~854–3045) **in exact
  current order**. Shell order: `exec(weapons_combat); // then datablocks below`
  — combat file MUST exec first because `fireTime = GetDelay(<W>)` runs at
  datablock-registration time.
- **NEVER** reorder, relocate, or re-slot the datablocks. No strategy 2.
- **Smoke:** clean boot (no "unknown function GetDelay"); buy the highest-tier
  BlackDiamond/Atom shop weapons (closest to the >200 ceiling) — no crash; swing
  sword/spear/mace (correct damage, reach, cadence); belt-weapon equip still
  shell-swaps; bots still cast via CastingBlade; shop prices correct.

### 1c. playerdamage.cs → 3 files
Both mega-functions stay atomic, so every townbot-immunity guard (all inside
`Player::onDamage`: vehicle-impact 3058, INSTANT_KILL 3312, fall damage 3766,
team-null 3854, retaliation-skip 4294) is untouchable by construction.
- **`playerdamage_util.cs`** = GetClientIdFromPlayerObject (6–304),
  GetClientOrBotName, DamagedByErase, PopCompactDamageText,
  DisplayDamageMessage (through 579).
- **`playerdamage.cs`** (shell + core) = `Player::onDamage` (2925–5194) whole,
  MythicWeapon::IsProtectedTarget, SkyRender::Impale, EchoFang::Echo,
  remoteKill.
- **`playerdeath.cs`** = `$LOOTBAG_DEBUG = 0;` (line 1 — its only reader is the
  death code), Client::onKilled (306–393), Game::clientKilled (581–594),
  `Player::onKilled` (596–2923) whole.
- Contested names (`Client::onKilled`, `Game::clientKilled` vs game.cs/dm.cs)
  stay winners automatically because the shell keeps slot 298 (after game.cs
  @263; dm.cs/objectives.cs never load on the RPG mission).
- **Smoke:** kill an enemy bot (EXP, lootbag, kill message, daily/weekly
  credit); ram a townbot with a vehicle (no damage); melee a townbot (no
  damage); 5s spawn invuln holds; A→B same-weapon damage number identical
  pre/post; `$LOOTBAG_DEBUG=1` still prints `[DROP RATE DEBUG]`.

## Phase 2 — the big win

### rpgfunk.cs → rpgfunk.cs (shell) + 5 parts
Highest churn (21 commits since Apr) — land it in a quiet window, coordinate
with any in-flight work, single mechanical commit. File is unusually clean: 140+
unique functions, zero exec-time calls/schedules/objects, only independent
constant `$globals`.
- **`rpgfunk_util.cs`** (exec 1st) — String:: library (32–131; stock overrides,
  exactly one definition each), numFormat, SafeGetItemCount, NEWgetClientByName,
  id/name-list helpers, name validation, random/round/Cap, stuff-string family
  (7093–7283), dice/combo, display/info, comma-list + Commafy, event commands,
  sprintf/Trim. Owns the 3 debug-flag globals (lines 1–3).
- **`rpgfunk_persist.cs`** — BankStorage split/join, updateSpawnStuff, DoCamp,
  SaveCharacter/SaveAllCharacters/LoadCharacter (634–2660), OnOrOfflineGive,
  ResetPlayer, server-time, Clear*Variables/ClearFunkVar.
- **`rpgfunk_world.cs`** — world-save queue, deployables save, SaveWorld/
  LoadWorld + Deploy* rehydrators, lootbag aggregation (owns `$LootbagAggregate*`
  globals 3500–3501), TossLootbag, base recording/DeployBase, teleport/weather/
  sky, house objectives (8165–8722), SafeDeleteLootbag.
- **`rpgfunk_player.cs`** — Safe_SetSkin, spell-list helpers, appearance/team/
  race (4165–4764), RefreshAll/RefreshAllEnemyBot (5798–6207, THE hot path —
  KHudOn-gated KShop sync at 6119 stays gated), HasThisStuff/TakeThisStuff,
  Void migration helpers, GiveThisStuff (6494–7053), spawn/stealth/target-list/
  bounty, UnequipMountedStuff, TownBot_PlayFarewell.
- **`rpgfunk_admin.cs`** — shutdown/countdown (4986–5091), AFK-zone enforcement
  (owns `$AFKZone*`/`$AFKOverLevel*` globals 8729–8744), ActivateAllGenerators.
- Constraints: all parts exec from the shell at slot 257 → automatically before
  KronosHUD_Server @341 (preserves `Commafy` winner). Loop starters
  (StartLootbagAggregation/StartAFKZoneEnforcement/InitObjectives) remain plain
  defs — Server.cs:436/448/477 calls them post-loadMission; no top-level calls
  may be introduced during the move.
- **Smoke:** boot; join+spawn; equip (RefreshAll); loot pickup (GiveThisStuff);
  `SaveWorld()`; let the 30s lootbag-aggregate, 30s AFK, 60s objectives loops
  each fire once; disconnect/reconnect (Save/LoadCharacter round-trip);
  `echo($LootbagAggregateInterval)`→30, `echo($AFKZoneCap["Pig Den"])`→30;
  KronosHUD panel number formatting unchanged (Commafy winner intact); vanilla
  client unaffected.

## Phase 3 — deferred / optional

- **ai.cs conservative 3-way (OPTIONAL):** peel `ai_combat.cs` (3504–4521 +
  4522–4860 + 8370–8535 + 8588–9119, ~950 lines, verified id-free) and
  `ai_groups.cs` (9120–9439, ~320 lines). MUSTS: lift `$numAI = 0;` (line 4852,
  buried mid-file) back to core with tombstone; pull getAInumber/setAInumber
  (8536–8586) back to core; ALL id-safeguard machinery (2048/2049–2175 pool
  scans, getFreeId/reserveId/releaseId sites, recently-freed tokens, validation
  predicates, ghost/stale cleanup, town-bot client registry, the 2,016-line
  SpawnAIGetClientId monolith) stays in core — it spans ~92% of the file, which
  is why the gain is modest. Honest alternative: leave whole. Smoke: join as
  human among live bots — clientId in 2049–2175, 2048 never assigned, no bot
  hijacks the player object (InspectBot's REAL PLAYER LIST); re-exec all parts
  mid-session — no duplicated periodic heartbeats; `echo($numAI)` numeric.
- **EnemyArmors.cs 2-way (after balancing settles):** volatile bot/spawn/
  equipment table (1–712) stays `EnemyArmors.cs`; the 17 stable PlayerData
  blocks (713–3560) → `EnemyArmorData.cs`; both exec'd from armordata.cs AFTER
  HumanArmors (28 refs to `$damageScale`/`$jumpSurfaceMinDot` owned by
  HumanArmors lines 1–2). Smoke: spawn/hit every enemy type incl. VoidArmor and
  Infinite-tier bots; normal damage numbers.
- **Belt.cs (only if ever mandated):** extract `BeltMenus.cs` (all Menu*/
  processMenu*/Shop/Store/Sell/Deploy/Use/Drop UI). The exec-time item registry
  (BeltItem::Add defs+calls, lines 3125–3287) NEVER moves — it needs Accessory.cs
  (@286) before it and BeltWeapons.cs (@349) after it.

## Separate opt-in project — comchat.cs

comchat.cs is **one function**: `remoteSay` spans lines 43–12,082 (~230-command
dispatcher + townbot dialogue engine in one lexical scope). A fear-script
function body cannot span files → no text-move split exists. Options:
- **Path A (mechanical, near-cosmetic):** peel the 10 tail helpers (12112–12383,
  ~340 lines) into `comchat_helpers.cs` exec'd right after comchat (keeps
  `messageAll` 3-param winner). File stays ~12k lines.
- **Path B (real fix, behavioral risk, one cluster per commit with in-game
  verification):** convert the if-chain into per-cluster handler functions
  (player / admin / debug / townbot-dialogue files), threading ALL preamble
  locals as parameters (no closures in fear script). Hard constraints:
  `#say`/`#shout`/`#whisper` stay in core — their deliberate fall-through sets
  `%botTalk` so townbots "hear" players (extracting them silently kills all
  townbot interaction); extracted `return;` becomes `return true;` + dispatcher
  re-returns; block-scripting handlers stay co-located with their
  ClearBlockData/ParseBlockData helpers.
Path B is a refactor, not a split — schedule it as its own project when wanted.

---

## Standing warnings (apply to everything above)

- RaceArmor.cs, backpack.cs, newseal.cs, rpghud.cs, hackfix.cs are dead/disabled
  files that shadow live function names — never add execs for them.
- ai.cs is hot-reloadable by design; split parts inherit that (re-exec test in
  the ai.cs smoke list).
- Refactors never delete code: tombstones at old sites, debug code gated not
  removed.
- Any change that DOES touch `Server.cs` must be mirrored by hand into
  `sanitized of new weapons\Server.cs` (it differs deliberately: VSlots/Estates
  off, AutoRestart off). The shell-file recipe exists to avoid this.
