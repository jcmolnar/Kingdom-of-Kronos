# CLAUDE HANDOFF — Kingdom of Kronos Development (written 2026-07-15)

Self-contained briefing for a brand-new Claude session/account with NO prior memory.
Read this whole file before touching anything. It supersedes any stale HANDOFF/notes.

---

## 1. What this project is

**Kingdom of Kronos** — a heavily modified Tribes 1 (1998 engine) RPG mod, run by Joe
(jcmolnar / joey@restorationlands.com). Server-side TorqueScript (old "CS" dialect) +
native plugin DLLs. Players connect with STOCK vanilla Tribes 1.30/1.40 clients — the
**vanilla-client compatibility mandate is a hard rule**: every feature must degrade to
exact stock behavior for clients without the KronosHUD handshake, and never require a
client download unless explicitly planned as a content rollout.

- **Working directory (dev server tree):** `C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development`
- **Git branch:** `testing` (main exists; user rarely uses PRs — commit directly to testing)
- **The user HOSTS THE DEV SERVER FROM THIS DIRECTORY** (T1Vista.exe + `-dedicated`).
  Script edits require a **server restart** to take effect (no live reload).
- **Live production server** is a SEPARATE tree (see §8 Live deploy). Dev edits get
  hand-copied/deployed there deliberately, never automatically.
- **Engine source ground truth:** `TribesSource/` (submodule-ish; community engine
  rebuild) — read-only reference for how the engine actually behaves. When script
  behavior is mysterious, grep here (e.g. `TribesSource/program/code/FearPlugin.cpp`).

### Process safety (CRITICAL)
- The user's OTHER server ("Red Moon RPG") runs as a process named **"Tribes"** —
  **NEVER kill any process named Tribes/tribes**. Only ever kill an exact PID you
  launched yourself. When a DLL copy fails "file in use", ASK the user to stop the
  server — do not stop it yourself.

---

## 2. Engine/scripting gotchas (all proven the hard way — violating these crashes things)

1. **No ternary `?:`** — syntax error. Use if/else.
2. **No `$=` / `!$=`** — syntax error (`==` already strcmps non-numeric operands).
3. **Item datablock caps:** engine `itemTypeList` cap = **200** on the live binary —
   `setItemCount/incItemCount` at index ≥200 is an **out-of-bounds WRITE → server
   crash**. Total ItemData registrations cap = 256. Current budget: ~221 registered,
   count-bearing items ≤197, hidden X0 "Equipped" datablocks parked at 198-220 with
   `showInventory=false` (they must NEVER receive counts — Ai.cs has isBeltItem guards).
4. **Shop bitfields are 128 bits** (`shoppingList[4]`/`buyList[4]` in FearPlayerPSC) —
   anything shop-visible must sit at index <128. Indices ≥128 phantom-alias.
5. **VSlot placeholder datablocks are ABSENT from the engine name→index map** — always
   address them by NUMERIC index (`$VSlot::NameToIdx`).
6. **`$ItemFavoritesKey`** (rpg/scripts/item.cs, currently "KronosRPG_V5") must be
   bumped on ANY registration-order change (clients store buy-favorites by index).
7. **Funk save files** (`Temp/<Name>.cs`) are EXEC'd on load — any single exported
   string >~1024 chars fails to parse and corrupts the character. Save loops must
   never persist belt-item engine counts (guards + `[VOID AUDIT]` echo tripwire exist
   in rpgfunk.cs updateSpawnStuff/SaveCharacter).
8. **Sim clocks freeze per frame** — `getSimTime()`/`getIntegerTime()` do not advance
   during script execution. For wall-clock timing use **`getRealMillis()`**
   (kronos_datetime.dll, added 2026-07-15).
9. **`fetchData` is NOT a plain lookup** for computed stats (DEF/MDEF/MaxHP/MaxMANA…):
   it runs `AddPoints` → `GetAccessoryList`, a full ~250-datablock scan. This was
   de-bloated 2026-07-15 (validate player once, direct `Player::getItemCount`) —
   578ms → 15ms per equip. Do NOT re-add per-iteration `Client::getOwnedObject`
   re-validation inside those loops (script is single-threaded).
10. **`Game::refreshClientScore`** (the LIVE one is in rpgstats.cs — dm.cs and
    Training_AI.cs versions are NOT exec'd on Kronos) tail-calls `KronosHUD_Push`.
    RefreshAll must NOT push again (a redundant 2nd push was removed — 013793e).
11. **Schedules are flushed on mission load** — never start periodic loops at exec
    time; start from `Mission::init` (gameevents.cs) or a watchdog, with a generation
    guard (see Estate::Init/UpkeepLoop pattern).
12. **exec() string literal limit ~1024 chars** per string.
13. **Zone FolderIDs are NUMERIC SimGroup object ids** — several consumers compare the
    player zone var numerically (incl. the bot cross-zone melee guard). NEVER write a
    synthetic string into the real zone funkvar (this exact mistake was made and
    reverted on 2026-07-15 — see §6 tab-menu zones).
14. **Case-sensitive git path quirk:** the AI file is `rpg/scripts/Ai.cs` (capital A)
    in git even though many tools show ai.cs.
15. Kronos mem.dll plugins MUST use the `getPlugin()` descriptor pattern
    (kronos_datetime.cpp is the annotated template); `plugin_open` convention is blocked.

---

## 3. Workflow conventions

- **Commit style:** one logical change per commit, detailed body explaining WHY,
  ending with `Co-Authored-By:` trailer. Commit as you complete verified work
  (the user has endorsed this cadence all session).
- **Brace check before every commit** (comments stripped):
  `awk 'BEGIN{d=0} {line=$0; gsub(/\/\/.*/,"",line); n=gsub(/{/,"{",line); m=gsub(/}/,"}",line); d+=n-m} END{print FILENAME": depth="d}' file.cs`
- **Partial staging:** when a file mixes your hunks with user WIP, filter
  `git diff` by hunk-header context with awk and `git apply --cached` (pattern used
  repeatedly this session for rpgfunk.cs).
- **Current user WIP (do NOT commit):** `rpg/scripts/EnemyArmors.cs` ZombieArmor
  animData hunk (part of the user's zombie model work — the .dts/.bmp changes in git
  status are the same project); `config/ClientPrefs.cs`, `config/ServerPrefs.cs`,
  `config/banlist.cs`, `config/WeeklyBossState.cs` (runtime state);
  `rpg/MISSIONS/KingdomKronos.mis`; model/skin binary files; `.gitignore` (user's);
  `Plugins/*.dll` binaries EXCEPT when you rebuilt them deliberately.
  Everything else script-side is now committed (a large uncommitted backlog was
  cleaned up 2026-07-15 — see git log 269adca..505efab).
- **Debug flags** (all default off, free to leave in): `$VoidPerf` (perf probes:
  Belt equip phases, RefreshAll phase map, KShop panel push), `$ShopDebug`,
  `$ShopDebugCtx`, `$MeleeDebug`, `$ZONE_DEBUG`.
- **Review conventions:** debug-gate or tombstone rather than delete; `DESIGN
  DECISION` comments for deliberate gameplay choices; verify engine claims against
  TribesSource before asserting.
- **Persistent memory:** this Claude account keeps memory files at
  `C:\Users\Joe\.claude\projects\C--Users-Joe-Desktop-Kingdom-of-Kronos-V0-8-1-Development\memory\`
  — a NEW account won't have them; this file replaces them. If you are the new
  account, consider seeding your own memory from this file's key facts.

### Test clients
- **Good stock test client:** `C:\Users\Joe\Desktop\Tribes\Dynamix\Tribes` (T1Vista.exe).
- The `Dynamix - 64 Bit` install is BROKEN (crashes at "item image" on join) — a full
  day was once lost bisecting "impossible" crashes that were just this install.
- KronosHUD client half lives OUTSIDE the repo at `C:\Dynamix\Tribes\config\Presto\`
  (KronosShop.cs, KronosHUD.cs, ATKText.cs…). Server half is
  `rpg/scripts/KronosHUD_Server.cs` + `KronosHUD.cs`-adjacent server files.
  Client-half edits need a CLIENT restart; they are not in this repo.
- RPG asset source folder (zips/loose files, useful for asset hunts):
  `C:\Users\Joe\Desktop\Tribes 1.40.655\RPG`.

### Plugins (Plugins/ + Plugins/Source/)
- `kronos_playermanager.dll` — funk-store persistence (fields on `$funk::var`).
- `kronos_virtualitems.dll` — Phase D per-client ItemData push (`vslotPushItem`) —
  REQUIRED for VSlots on stock clients.
- `kronos_datetime.dll` — getRealDate/getRealTime/getRealDayOfWeek/**getRealMillis**.
  Rebuild: `Plugins/Source/kronos_datetime/build_kronos_datetime.bat` (MSVC x86).
  The copy into `Plugins\` FAILS SILENTLY (bat doesn't check) while the server runs —
  verify timestamps after building.
- `kronosfix_server.dll`, `kronos_aiteardown.dll`, ForceExitPlugin (backs `forceExit()`
  so hung mem.dll threads can't block restarts), kronos_reprobe (dev-only RE probe).
- Loader flags: `Plugins/Scripts/PluginLoader.cs` (committed cae6acb).

---

## 4. THE VOID SYSTEM (belt/backpack item conversion) — COMPLETE, in soak

The flagship project: migrating items OFF scarce engine ItemData slots onto a
datablock-less script-side "belt" system ("Void backpack/storage" to players),
displayed in the stock GUI via VSlot placeholder rows rewritten per-client by
kronos_virtualitems.dll, and in KronosHUD via its ScriptGL panels.

**Status: conversion COMPLETE and validated on dev** (armors 23, accessories 17,
weapons earlier phases; lazy idempotent save migration; carry caps; unified banker;
prices; HUD parity; perf fixed). Remaining work is validation + deploy (§8) and the
long-tail items below.

Key architecture (files):
- `rpg/scripts/Belt.cs` — core belt inventory, equip/unequip, caps
  (`Void::CarryCap`/`AtCarryCap`, hasKronosHUD exempt), bank deposit/withdraw,
  UseItem dispatch.
- `rpg/scripts/BeltWeapons.cs` — registrations (VoidArmor/VoidAccessory RegisterAll),
  VoidPrice::MirrorAll (called from Server.cs AFTER GenerateAll*Costs),
  execs VoidLegacyEquipped.cs last.
- `rpg/scripts/VirtualSlots.cs` — 48 placeholders (weapons 8 @2-9, armor 20 @10-29,
  bank 20 @30-49), VSlot::Sync (skips hasKronosHUD clients + bots), SyncBank,
  OnUseClick. Exec'd FIRST in Server.cs (under the 200 cap).
- `rpg/scripts/VoidLegacyEquipped.cs` — 23 hidden X0 datablocks @198-220.
- `rpg/scripts/rpgfunk.cs` — migration (GiveThisStuff intercepts, FilterSpawnStuff
  dedup), save guards, RefreshAll (debounced HUD panel push via KShop_QueuedSync).
- `rpg/scripts/economy.cs`, `shopping.cs`, `remote.cs` — buy/sell/bank/drop routing
  incl. the engine-index belt-drop dupe-mint guard.
- `rpg/scripts/KronosHUD_Server.cs` — HUD panels; PushInv/Bank_PushInv SKIP VSlot
  placeholders + belt names (frozen-proxy ghost-row fix 02a3f45 — placeholder counts
  freeze for HUD clients, so they must never render in HUD panels).
- Funk fields: 15 spawnStuff(BY NAME), 16+60-63 BankStorage, 35-46 stored categories,
  48 carried "Armor" list, 49 Accessories, 51 EquippedBeltArmor, 58 EquippedBeltWeapon.
  Worn engine armor cache = "WornEngineArmor". `GetWornArmor()` helper.

Open Void items:
- **Multi-day soak on dev** — watch for `[VOID AUDIT]` lines (belt counts leaking into
  engine inventory) and any crash.
- **Vanilla-client full pass** (stock GUI buy/sell/equip/drop/bank, favorites) — the
  HUD got all the recent attention.
- **Confirm the old >200-index buy crash is dead** (buy the most expensive shop item).
- **Phase 2 reclaim** (POST-live-soak, weeks out): delete transitional engine armor/
  accessory datablocks + VoidLegacyEquipped.cs; any reindex must keep shop items <128;
  bump favorites key; regenerate sanitized deploy set coherently.
- MigTest harness: `rpg/scripts/MigrationTest.cs` (exec by hand; MigTest::GiveAll) —
  DEV ONLY, never ship.
- User's dev character: `Temp/Jobo.cs` (server must be STOPPED for hand-edits to stick;
  backups exist). They currently have ~everything via MigTest.

Perf lessons (2026-07-15, keep sacred): equip lag was 578ms→15ms via (a) removing
RefreshAll's duplicate KronosHUD_Push, (b) de-bloating GetAccessoryList/AddPoints
(Accessory.cs). SaveCharacter also fell 156→31ms. `fetchData("DEF")` runs per combat
hit — these loops are the server's hottest script path.

---

## 5. KronosHUD (ScriptGL HUD) — working, feature-complete for Void

Handshake: `KHudOn` → `%clientId.hasKronosHUD`. Channels: vitals (KronosHUD_Push),
shop/inv panel (KronosShop_*), bank panel (KronosBank_*). Recent fixes: Use-button
equips belt gear, "(worn)"/"(equipped)" markers, open-panel refresh via debounced
KShop_QueuedSync (0.2s, flag cleared on panel open/close), drop refresh, ghost-row
exclusion. `#say STORAGE` at banker routes HUD clients to KronosBank_Open.
HUD clients are EXEMPT from Void carry caps (deliberate).

---

## 6. ESTATES (player housing) — TODAY'S ACTIVE PROJECT, dev-enabled, mid-live-test

`rpg/scripts/Estate.cs` (~1600 lines). Master gate `$pref::EstatesEnabled` —
**defaults OFF in Estate.cs; dev Server.cs sets true (line ~348); LIVE MUST NOT
ENABLE IT** (inline comment says so). Vanilla-safe: all UX via `#estate` chat.

Everything implemented (Phases 1-4 + today's additions, ALL COMMITTED):
- **Core:** found (25k coins, refuses PROTECTED zones + within 100u of any
  PROTECTED/DUNGEON/FREEFORALL zone box edge (WATER exempt) + 2x-radius gap from other
  estates) / build / demolish (25% refund, stand within 12u) / abandon / info / where /
  list + reclaim (admin ≥4) / upgrade (tier 1-3, 100k*N; MaxStructs 8/16/24).
- **Persistence:** own registry `temp\<mission>_estates_.cs`, 1-D arrays only,
  namespace discipline ($Estate::=persisted, $EstateCfg::=config, $EstateRt::=runtime).
  Rehydrated in Server::finishMissionLoad after LoadWorld. Save is debounced
  (RequestSave).
- **Coffer economy:** deposit/withdraw (coins only), hourly UpkeepTick,
  solvent→grace(72 ticks)→decay(newest structure first)→reclaim(168 empty+broke ticks).
  Ticks = server-uptime hours, persisted.
- **Members:** permit/evict (single-word names), pass force fields, spared by turrets.
- **Structures** (`$EstateCfg::DB[type]`): wall=DepPlatLargeVert (KNOWN ISSUE: edge
  collision holes — inherent to elevator_9x9), platform=DepPlatLargeHorz,
  forcefield=StaticDoorForceField (ACCESS DOOR: opens 3s for owner/members),
  barrier=RForceField (always-solid), crate=CargoCrate (solid stock box),
  wall2=VerticalPanelB (CULL CANDIDATE — it renders as an "electrical dashboard"),
  turret=DeployableTurret (tier ≥2, max 3, range 30).
- **HOUSES = runtime INTERIORS** (the big unlock, e0849c8): `.dis` DB values spawn via
  `newObject("", InteriorShape, file)` — the proven Tent primitive (sleep.cs:191).
  One whole building = ONE object. Catalog restricted to VOLs the mission already
  mounts (client-guaranteed): hut=npchut 15k, house=house1 40k, lhouse 75k/T2,
  tavern 75k/T2, tower=magetower 100k/T2, keep 250k/T3 (MinTier gates).
  **NOT YET VERIFIED IN-GAME:** spawn height (origins vary — may need per-type Z
  offset), facing, whether a keep fits the 40u plot. TEST THESE FIRST.
- **Zones/stance** (314828e): `#estate mode <friendly|hostile>` (aliases
  protected/dungeon; persisted $Estate::Mode; default friendly; cleared on
  abandon/reclaim). Friendly = turrets NEVER fire (Turret.cs verifyTarget hook);
  hostile = fire on non-members. Warning perimeter = 2x plot radius (80u), swept every
  3s (Estate::ZoneLoop, generation-guarded, started from Estate::Init in
  gameevents Mission::init). Ring 80 > max turret reach 70 → always warned first.
- **Named grounds** (07a40c1): `#estate name <multi word name>` (32 chars,
  persisted $Estate::Name). Estate::ZoneName(eid) = name or "<owner>'s estate grounds".
  Used in all perimeter messages.
- **Tab-menu zone display** (505efab — READ THIS HISTORY): first attempt registered
  estates as real $Zone entries (e9f23f5) — REVERTED (44ce594) because zone FolderIDs
  are numeric object ids and consumers (bot cross-zone melee guard!) numeric-compare
  them. Final approach is DISPLAY-ONLY: refreshClientScore (rpgstats.cs) and
  KronosHUD_Push substitute Estate::ZoneName when `%clientId.estateZone` is set
  (maintained by ZoneLoop); the real zone funkvar is NEVER written; $zonedis admin
  disguise still wins; ZoneLoop calls Game::refreshClientScore on ring transitions.
- **Object budget:** $EstateCfg::GlobalObjBudget=400 server-wide (engine object pool
  ~1024, patchServerNetcode deliberately disabled for pre-1.31 client compat),
  checked in Build; `#estate list` (admin) shows objects N/400.
- **Estates have ZERO ItemData involvement** — no datablocks registered, coins only.
  Cannot touch the 200/256/128 item budgets or VSlots.
- Estate router lives in comchat.cs (~line 496, `#estate` branch).
- Design docs: `HOUSING_SYSTEM_DESIGN.md` (repo root). The separate
  `rpg/scripts/analysis/HOUSE_TREASURY_SYSTEM_TRACKER.md` (House=FACTION steward NPC
  system) is DESIGN-ONLY, zero code — "House" = the four factions, NOT estates.

**Estate immediate test checklist (where live-testing stopped):**
1. `#estate build house` / `hut` / `tower` — verify interiors spawn at correct height/
   facing, have working collision/doorways, persist across restart, demolish OK.
2. Turret friendly-fire matrix: friendly mode = never fires; hostile = fires on
   stranger, never owner/member. (Highest-risk untested logic.)
3. Perimeter messages + tab-menu name display (walk in/out, rename, check Tab + HUD).
4. barrier/crate collision A/B; cull wall2; decide the wall story (crate rows? barrier?).
5. Zone-clearance founding refusals near a town/dungeon/FFA.
6. Upkeep/decay: shrink `$EstateCfg::UpkeepFreq`/GraceTicks in console to watch the
   state machine, or verify one real tick after an hour.
Known deferred: banner structure (FlagStand carries capture logic), Steward NPC,
storage vault (needs Belt.cs), siege/HP raiding (Phase 5), OneWayWall/ethrenwalls/
castle assets (would need VOL distribution = content rollout rules).

---

## 7. Other live systems & recent fixes (context you'll need)

- **Weekly Boss** (`rpg/scripts/WeeklyBoss.cs`, committed): Colloseum raid, persistent
  HP pool (config/WeeklyBossState.cs runtime file), `#weekly` teleport, seal-battle
  arena handoff. NEEDS a live-style test pass.
- **Daily quests** (`DailyQuest.cs`): rotating dailies keyed on kronos_datetime day
  stamp. Latent known bug: DualWielding GetRemort() undefined (unfixed, low priority).
- **Remote web admin console** (committed 10c50bd): engine telnet rcon
  (`config/RemoteConsole.cs`, port 28001 localhost-only; secret in gitignored
  `config/RemoteConsole.local.cs`) + `rpg/scripts/RemoteAdminHelpers.cs` (fenced
  listings for the Node admin app at `C:\Users\Joe\Desktop\Tribes Browser Based\test\admin\`).
  Docs: `REMOTE_ADMIN_CONSOLE.md`. User says it WORKS.
- **Lootbag popToken** (bfb663f): deferred Item::Pop validates a per-bag token so
  recycled object ids can't delete fresh bags. NEEDS hand-copy to live.
- **Death packs owner-locked forever** (aadc957): DESIGN DECISION — no timed unlock.
- **comchat `$=` syntax error fixed** (a7250ab) — HEAD previously shipped a parse error
  in the #spawnpointscan handler.
- **Townbots**: immune to player damage; share the 2049-2175 clientId pool with enemy
  bots and players; BaseRep id 2048 = server slot, never allocated. Ai.cs safeguard
  layering is deliberate — don't "clean it up".
- **Shout distance 5** is a deliberate anti-exploit nerf; **party EXP rebalance** is
  designed but deferred until asked.
- Rocket FX must explode via 0.01s timeout not collision; chat filter `~category`
  tags (last, never starting with 'w').

---

## 8. Live deploy plan (the four gates — agreed with the user)

**Gate 1 — validation on dev:** vanilla-client Void pass; multi-day soak
([VOID AUDIT] clean, no crashes, reconnect cycles); top-shop-item buy test.
**Gate 2 — hygiene:** MigrationTest.cs and kronos_reprobe never ship;
$VoidPerf/$ShopDebug off; **live Server.cs must NOT set $pref::EstatesEnabled**.
**Gate 3 — deploy set:** regenerate the SANITIZED file set from testing (never ship
new-model .dts/KOKBATCH content to live — standing rule); ship
kronos_virtualitems.dll (new live dependency for VSlots) AND the rebuilt
kronos_datetime.dll (**MANDATORY: perf probes call getRealMillis() unconditionally —
old DLL = console error spam**); PluginLoader flags; popToken fix rides along.
**Gate 4 — cutover:** back up live `Temp\` (all saves) BEFORE first boot — rollback
after players log in requires restoring saves too (migration moves armor into belt
fields); announce the one-time favorites reset (V5 key) + Void system + carry caps
(existing over-cap players grandfathered).
Deploy checklist file exists: `DEPLOY_CHECKLIST.md` (repo root).

---

## 9. Current tree state (as of this handoff)

- Everything script-side committed through **505efab**. See `git log 269adca..505efab`
  for the 2026-07-15 arc: Void HUD fixes → perf hunt → backlog cleanup (Estates,
  remote admin, parked fixes) → Estate live-test fixes & features (LOS, houses,
  zones, naming, clearance, display-only tab name).
- Modified-but-uncommitted (leave alone): user's zombie-model work (EnemyArmors.cs
  animData + rpg/*.dts + zombie skins), config runtime files, .gitignore, mission
  file, Plugins binaries, TribesSource pointer.
- Untracked (leave): `rpg/scripts/analysis/*` trackers, `rpg/scripts/stale/*`,
  `Temp/`, backups, `FEATURES_DESIGN.md`, `FABLE_REVIEW_CHECKLIST.md`, `Functions.txt`.

## 10. Where to resume

The user was mid-Estate-live-test. Next concrete actions, in order:
1. User restarts dev server → run the Estate test checklist (§6), starting with
   `#estate build house` placement verification and the turret friendly-fire matrix.
2. Fix whatever those tests surface (placement Z offsets, facing, etc.).
3. Cull losing structure types (wall2 confirmed dead; decide wall vs crate/barrier).
4. In parallel/after: Void Gate-1 validation (vanilla pass + soak) → live deploy gates.
5. Longer arc: Weekly Boss test pass; Estates Phase 5 ideas (siege) only after soak;
   Void Phase 2 reclaim weeks after live.

Working style the user expects: investigate before guessing (their bug reports are
accurate but causes are often 2-3 layers deep); instrument with probes when blind
(getRealMillis + gated echoes); verify engine behavior against TribesSource; commit
in small documented steps; never break vanilla clients; ask before anything
destructive or live-touching, otherwise proceed autonomously.
