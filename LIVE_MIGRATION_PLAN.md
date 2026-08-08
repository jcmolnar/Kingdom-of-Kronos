# Migrate dev scripts → live server (V0.8.1 Development → V0.8 BOV Plugins)

## Context

`C:\Users\Joe\Desktop\Kingdom of Kronos V0.8 BOV Plugins` is the **live production server** — no git, 185 real player saves in `Temp\` written daily, currently running a ~3-week-old script snapshot. The dev tree is ~3 weeks ahead: the Void conversion (23 armors + 17 accessories moved off engine ItemData onto the script-side belt), VSlots native inventory, the AI/rpgfunk/playerdamage/spells/weapons module splits, Estates, the lootbag popToken fix, and the equip-lag fix (578ms → 15ms).

Verified during planning:
- **Live has zero unique authored code.** Dev is a strict superset; the 8 live-only scripts (`Colloseum.cs`, `Ai - Copy.cs`, etc.) are never exec'd. Migration is one-way, dev → live, whole-folder.
- **The `sanitized of new weapons\` package is obsolete** — dev itself was stripped of new-model KOKBATCH content on 2026-07-14, so dev *is* the sanitized content. Worse, that folder's `Server.cs` has rotted (missing `VSlot::Init` and `VoidPrice::MirrorAll` → blank shop prices). Rename it `OBSOLETE_` so nobody grabs it by reflex.
- **`rpg\MISSIONS\KingdomKronos.mis` is already md5-identical** between dev and live — do not ship it.
- The existing `DEPLOY_CHECKLIST.md` is the scaffold; this plan supersedes its sections 0–3.

**Decisions taken:** host-profile file for per-machine config; live runs VSlots + Estates + AutoRestart + rcon; full validation gates; straight to general open after admin smoke tests.

---

## STATUS — nothing executed yet (re-verified 2026-08-08)

The plan was written 2026-08-04 and reviewed since. Every load-bearing claim still holds (mission md5 identical; 175 top-level `.cs`; `kronos_virtualitems.dll` absent on live; `kronos_datetime.dll` hashes differ; `MigrationTest.cs` + `ai_safeguards_off.cs` present in dev; `Server.cs:348/:351` line references accurate; live tree has no git). Five corrections below.

**C1 — Phase 0 is stale, and the repo is untagged.** `EnemyArmors.cs` is no longer dirty; **`rpg/scripts/Turret.cs` now has an uncommitted change (+88/−47)** that postdates the audit and has passed **no** gate. Whole-tree dirty count is ~474 entries, but nearly all are non-shipping (TribesSource, ModernHUD/client assets, config runtime, untracked `analysis/` + `stale/`); inside `rpg/scripts` only Turret.cs is modified.

**C2 — commit hashes are not a reliable reference here; the tag is the fix.** `HEAD = e0d932a` ("ModernHUD: integrate Kronos RPG client HUD", Jul 29) with **no tags**, and `git reflog` shows nothing after it. Hashes quoted in the tracked `CLAUDE_HANDOFF.md` (b71f821, 02a3f45, 013793e, 269adca, 505efab, b2ef434, …) **do not exist in this repo** — a future session will hunt for phantom commits. The *code* those notes describe **is** present and committed at HEAD (verified: Estate zones/clearance, the Void work), so nothing is lost. Action: correct `CLAUDE_HANDOFF.md` to cite the deploy **tag** instead of hashes, and treat Phase 0's tag as the single source of truth from here on.

**C3 — save arithmetic corrected, and it drifts.** Live `Temp\` holds **185 `.cs`**, of which **6 are server state** (`KingdomKronos_worldsave_`, `ServerTime`, `SealValue`, `HouseObjectives`, `watchdog_state`, `serverTempData28000`) → **~179 real player saves**. Do **not** hardcode any of these: the server is live and writing (newest state files stamped today 13:14). Rule: **record the exact file count at drain, then verify the backup matches that number.**

**C4 — `.gitignore` still lacks `config/DeployProfile.cs`** (only `config/RemoteConsole.local.cs` is listed). Phase 1 isn't complete without it or dev's own profile file gets committed and overwrites live's on a later deploy.

**C5 — this plan lives outside the repo** (`~\.claude\plans\` is per-account and invisible to other sessions — which is exactly why it was hard to find). Copy it to the repo as `LIVE_MIGRATION_PLAN.md` and add a supersede note to `DEPLOY_CHECKLIST.md`. Safe: top-level `.md` files are excluded from the deploy package anyway.

Because live wants *all four* previously dev-only features, `Server.cs` needs **no sanitization** — dev and live run byte-identical scripts. That is the ideal end state and the profile file exists to keep it that way.

---

## What ships / what does not

**Ships:** all ~175 top-level `.cs` from `rpg\scripts\`; `kronos_virtualitems.dll` (new to live) + rebuilt `kronos_datetime.dll` + any DLL whose hash differs; a live-variant `Plugins\Scripts\PluginLoader.cs`; `config\RemoteConsole.cs` + a **freshly generated** `RemoteConsole.local.cs` secret; `config\DeployProfile.cs`.

**Excluded — package receives `.cs` only, so binaries can't leak structurally:**
- Subdirs `stale\`, `analysis\`, `backup\`, `ChangeLogs\`, and all top-level `.md` docs
- `MigrationTest.cs` (give-everything harness) and `ai_safeguards_off.cs` (stubs out every AI player-protection gate) — neither is exec'd, both are one console command from disaster on live
- `kronos_reprobe.dll`; any `.bak`/`.preaifix`/`.inuse-old` DLL variants
- All `.dts`/`.vol`/`.bmp`/`Skins\*` (vanilla-compat mandate), the mission file, `config\ModernHUD\`/`UI\`/`Fonts\`/`Presto\`/`ClientPrefs.cs` (client-side), and live's own `ServerPrefs.cs`/`banlist.cs`/`TaurikAdmins.cs`

---

## Phase 0 — Freeze and tag

0. **Copy this plan into the repo** as `LIVE_MIGRATION_PLAN.md`; add a one-line supersede note to `DEPLOY_CHECKLIST.md` (its sections 0–3 are replaced by this). [C5]
1. **Commit `rpg/scripts/Turret.cs` (+88/−47) into the release** — reviewed and INCLUDED by decision. It carries four fixes: `dopplerVelocity` 2→0 on `TempleIndoorTurret`/`IndoorTurret` (players evaded targeting by standing still); `EnsureAllTurretsOnTeam1` → recursive `Turret::MaintainGroup` (the old hand-unrolled walk only reached 2 levels, so deeper-nested turrets were never fixed); new `Turret::MaintainRuntimeState` (recharge rates + re-activate Enabled turrets; DeployableTurret 5 / RocketTurret 14 / others 10; CameraTurret skipped); and a **new 15-second maintenance loop** using the generation-token idiom (Tribes 1 can't cancel schedules).
   **It ships gated, not free** — see G3a: it adds a permanent recurring recursive walk of the whole MissionGroup plus turret state writes every 15s, and live's object graph is far larger than dev's. [C1]
   Everything else dirty in the tree is non-shipping (TribesSource, client assets, config runtime, untracked `analysis/`+`stale/`) and needs no action — the package takes only top-level `.cs`.
2. Commit, then **`git tag -a live-YYYYMMDD`**. This tag — not a commit hash — is the release identity everywhere downstream. [C2]
3. Write the tag into `DEPLOY_MANIFEST.txt` (ships inside the live scripts folder, so the running server self-documents what it is), and **fix `CLAUDE_HANDOFF.md` to reference the tag instead of the non-existent hashes it currently cites.** [C2]

## Phase 1 — The one code change: host profile

`exec()` already resolves `config\*.cs` (proven by `Server.cs:351 exec(RemoteConsole)` → `config\RemoteConsole.cs`), so no new machinery is needed.

In `rpg/scripts/Server.cs`, before the pref block (~`:348`):

```
exec(DeployProfile);            // config\DeployProfile.cs - per host, gitignored
if($Deploy::Profile == "")
	$Deploy::Profile = "dev";
echo("[DEPLOY] profile = " @ $Deploy::Profile);
```

Then make the one knob worth staging profile-defaulted (replacing the hardcoded `:348`):

```
if($Deploy::EstatesEnabled == "")
	$Deploy::EstatesEnabled = true;
$pref::EstatesEnabled = $Deploy::EstatesEnabled;
```

**Use `==`, never `$=`** — `$=` is a syntax error on this engine (we fixed one in `comchat.cs` today, a7250ab). `==` already strcmps non-numeric operands.

`VSlots`, `AutoRestart` and `RemoteConsole` need no gate — live wants all three. **Add `config/DeployProfile.cs` to `.gitignore`** beside the existing `config/RemoteConsole.local.cs` entry (line 22) — still missing, and without it dev's profile gets committed and would overwrite live's on a later deploy [C4]. Live's file is two lines: `$Deploy::Profile = "live";` and (recommended) `$Deploy::EstatesEnabled = false;` for the first boot.

**Recommended Estates sequencing:** ship Estates *capable* but flipped off for the cutover, then enable it with a one-line profile edit + restart once the Void deploy has settled. It isolates two large changes, and turning it on later costs no redeploy. This honors "Estates on live" while not landing housing and a 185-character inventory migration in the same hour.

## Phase 2 — Dev validation gates (nothing touches live)

Each gate is pass/fail; a failure stops the deploy.

- **G1 — clean cold boot.** No `Unknown command` / `Unable to find function` / `exec: file not found`. Must be present: `[DEPLOY] profile = dev`, `[SLOT AUDIT] ItemData types registered: N / 256` (record N; no count-bearing item at index ≥200), `[VSLOT] windows ready ... DLL ok: ...dbm=...`, `Number::Beautify sanity`.
- **G2 — vanilla stock-client pass** (`C:\Users\Joe\Desktop\Tribes\Dynamix\Tribes`, *not* the broken 64-bit install). Inventory rows render; buy/sell/equip/unequip/drop/pickup; **real prices in the shop column**; bank deposit/withdraw on the shared 20-row window; carry-cap refusal fires *before* charging; no phantom X0 rows in any merchant pane; relog preserves state. **Zero `[VOID AUDIT]`.**
- **G3 — Estates test pass** (now player-facing): house/hut/tower placement height + facing, turret friendly-fire matrix (friendly = never fires; hostile = fires on strangers, never owner/members), perimeter warnings + tab-menu name, zone-clearance refusals, persistence across restart. Also **reduce `$EstateCfg::GlobalObjBudget`** for first live exposure and confirm live's peak object headroom — live runs far more deployables/bots/lootbags than dev, and the engine pool is ~1024 with `patchServerNetcode` deliberately disabled.
- **G3a — turret maintenance loop (new in this release).** The `Turret.cs` change adds a 15s recurring recursive MissionGroup walk that also writes turret state. Two checks: (i) **cost** — time one tick on dev with a `getRealMillis()` probe around `EnsureAllTurretsOnTeam1(true)` and confirm it's negligible against the live-sized mission, not just dev's; (ii) **Estates interaction** — `MaintainRuntimeState` special-cases `DeployableTurret`, which *is* the estate guardian datablock, forcing it active and recharge 5 every 15s, and re-asserts team 1 that `Estate::SpawnStructure` already sets. Confirm during the G3 Estates pass that guardian turrets still honour the friendly/hostile stance and don't get revived after being destroyed. Also verify only ONE loop runs after a mission reload (token generation guard).
- **G4 — asset scan.** Re-grep **all** scripts (not just the four historically suspect files) for new-model `shapeFile` references; re-verify the mission md5 still matches live.
- **G5 — plugin matrix.** Hash-compare every DLL (dev and live `kronos_datetime.dll` are *both* 124,928 bytes — size proves nothing). Build the live `PluginLoader.cs` = dev's minus the `kronos_reprobe` line.
- **G6 — 48h soak** with bots active: zero `[VOID AUDIT]`, clean `[LOOTBAG AGGREGATE]` and world-save cycles, no memory/handle growth (the splits moved ~30k lines).
- **G7 — `$VoidPerf` baseline.** Record `[VOIDPERF] RefreshAll(...)` numbers so the first "server feels laggy" report has a reference. Set back to 0.
- **G8 — ★ migration rehearsal against a COPY of the real 185 saves.** Copy live `Temp\` → dev (read-only on live; one-way, nothing ever goes back). Log in as ~6 real archetypes: heavily-geared, remort/ascended, full bank, legacy `BankStorage` holder, low-level, and one mid-session at last save. Verify nothing lost or duplicated, worn armor re-equips, **log out and back in for idempotency**, hand-diff two saves against their pre-migration copies. **Zero `[VOID AUDIT]`.** This is the only thing that can prove 185 characters survive before it's irreversible.
- **G9 — rollback rehearsal.** On dev, restore pre-migration saves + old scripts and confirm those characters load. Prove the procedure, don't just write it.
- **G10 — package integrity**, verified *from the package*: file count matches manifest, no subdirectories, no binaries, `MigrationTest.cs`/`ai_safeguards_off.cs` absent, `Server.cs` hash matches the tag.

## Phase 3 — Cutover

Everything before step 5 is fully reversible.

1. **Announce + drain** (T-30/-10/-2 min) so every last save is written cleanly.
2. **You stop the server** — plus `InfiniteSpawn.exe` and `server_watchdog.py`, or the wrapper restarts it mid-copy. **Never kill by process name** (another server shares `Tribes`/`T1Vista`).
3. **Verify down:** no `Temp\*.cs` mtimes advancing, no new `console.log` writes.
4. **★ Back up `Temp\` → `Temp.PREVOID-<stamp>\` (COPY, not move; ~881 KB)** and zip a second copy outside the server directory. This is the only artifact that can undo a bad migration on ~179 real characters.
   **Verify by count, not by memory:** run the census *at drain* (`find Temp -maxdepth 1 -name "*.cs" | wc -l`) and confirm the backup has the identical number. As of 2026-08-08 that is 185 `.cs` = ~179 player saves + 6 state files (`KingdomKronos_worldsave_`, `ServerTime`, `SealValue`, `HouseObjectives`, `watchdog_state`, `serverTempData28000`) — but it drifts as players join, so the recorded number wins. [C3]
5. **RENAME `rpg\scripts` → `rpg\scripts.bak-<date>`. Do not merge into it.** Live's `Ai.cs` is a 568 KB monolith; dev's `ai.cs` is a 1.1 KB shell loader plus 5 `ai_*.cs` modules (same for rpgfunk/playerdamage/spells/weapons). On a case-insensitive merge the old monoliths can survive alongside the new modules → duplicate definitions, last-exec-wins, undebuggable. A rename makes the deployed folder *provably* the validated artifact.
6. Copy package `scripts\` → `rpg\scripts\`; verify count + `Server.cs` hash.
7. Back up `Plugins\` → `Plugins.bak-<date>\`, then install `kronos_virtualitems.dll`, the rebuilt `kronos_datetime.dll`, any hash-differing DLL, and the live `PluginLoader.cs`.
8. Place `config\DeployProfile.cs`, `config\RemoteConsole.cs`, and a **new** `RemoteConsole.local.cs` secret (never copy dev's).
9. **Cold restart, manually and passworded, without the InfiniteSpawn wrapper** so a crash loop can't hide the error. Re-enable wrapper + watchdog after Phase 5.

## Phase 4 — Boot smoke tests (server still passworded)

**Abort strings** — any one of these means stop and roll back (nothing is irreversible yet): `Unknown command getRealMillis` (wrong `kronos_datetime.dll` — it's called unconditionally 7× inside `RefreshAll`, so this floods), `Unknown command vslot`, `[VSLOT] ... disabling Virtual Slots` (**not** a benign warning — `Void::AtCarryCap` checks only `hasKronosHUD`, never `$pref::VSlotsEnabled`, so caps stay enforced with no window to justify them), `exec: file not found`, `Unable to find function`, `[VOID AUDIT]`, `*** AI SAFEGUARDS DISABLED`, and — if Estates is staged off — `[SERVER] Automated restart scheduled` appearing at the wrong time.

**Must be present:** `[DEPLOY] profile = live`; `[SLOT AUDIT]` N **matching dev's G1 exactly**; `[VSLOT] windows ready ... DLL ok`; `Number::Beautify sanity`; town-bot init; a load line for every manifest DLL.

## Phase 5 — In-game smoke tests (admin-only, on a vanilla stock client)

1. **Void migration on your real character:** everything present, nothing duplicated, worn armor re-equipped, relog identical, zero `[VOID AUDIT]`.
2. **Stock GUI:** 8 weapon + 20 armor rows; real prices; buy/equip/sell/drop/pickup; bank; carry-cap refusal before charging; no phantom X0 rows.
3. **HUD parity** on a KronosHUD client: belt lists match, HUD exempt from caps.
4. **Combat/loot:** bot kill drops and merges; die with items → `#trackpack`, pack survives >5 min, owner-only (popToken + permanent lock).
5. **`#daily` / `#weekly` respond** — positive proof the *new* `kronos_datetime.dll` is in place, not merely non-erroring.
6. Force a world save; read your own `Temp\<name>.cs` by eye (belt field 48 populated, no X0 tokens in field 15). Spot-check `$VoidPerf` against the G7 baseline.

**Any step-1 failure → roll back immediately.** Your save is expendable; 185 others are not.

## Phase 6 — Open, announce, watch

Open to general population (per your choice). Post the announcement **before** cutover, covering the three things players will notice:
- **Item favorites reset** — `$ItemFavoritesKey` bumped to `KronosRPG_V5`, clears on first connect. Say it or it reads as a bug.
- **New carry limits** — vanilla clients: 8 distinct weapons / 20 distinct armors. State plainly that **nothing is ever confiscated**: caps are enforced only at shop-buy and bank-withdraw, so loot, quests, migration and admin gives stay soft; over-cap players simply can't add a *new* name until back under. KronosHUD users exempt.
- **Belt items now appear in the stock inventory** — frame it as the feature it is.
- Add: "report anything missing immediately — we have your pre-update saves."

**Watch 24h:** `[VOID AUDIT]` (zero, always), `[VOIDPERF]` drift, lootbag/world-save cycles. **Set a commitment point in advance** (12–24h): past it, restoring `Temp.PREVOID` costs everyone their earned progress, so it becomes forward-fix only.

## Rollback

- **Level 1 (before any player login):** stop server → delete `rpg\scripts`, rename `.bak` back → restore `Plugins.bak` → remove `DeployProfile.cs` → restart. `Temp\` untouched.
- **Level 2 (after some logins):** Level 1 plus restore **only the affected characters'** saves from `Temp.PREVOID-<stamp>\`; leave never-migrated saves alone.
- **Level 3 (broad):** Level 1 plus restore the entire `Temp\`. Everything since cutover is lost.

**The failure mode to refuse: rolling scripts back without rolling saves back.** Old scripts read armor from field 15 / X0 tokens; migrated saves hold it in belt field 48 — gear ends up simultaneously stranded and duplicable. Scripts and saves roll back together or not at all.

## Top risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | Migration corrupts/strands/duplicates gear across ~179 real characters | G8 rehearsal on copied real saves; `Temp.PREVOID` backup verified by drain-time count; `[VOID AUDIT]` as hard abort |
| R2 | ItemData index ≥200 receives a count → OOB write → crash | `[SLOT AUDIT]` N matching dev exactly; X0 blocks parked 198-220 `showInventory=false`; `[VOID AUDIT]` is precisely this detector |
| R3 | Old `kronos_datetime.dll` → `getRealMillis` error flood in `RefreshAll` | Hash-compare (sizes identical); abort string at boot; `#daily` as positive proof |
| R4 | `kronos_virtualitems.dll` missing → silent cap enforcement with no visible window | Ship it; `[VSLOT] disabling` is an abort, not a warning |
| R5 | Stale monoliths surviving beside split modules | Folder **rename**, never merge; package built from staging and verified at G10 |
| R6 | Estates on live exhausts the shared ~1024 object pool | G3 budget reduction + live headroom check; staged enable after cutover |
| R7 | Wrapper restarts server mid-copy | Stop InfiniteSpawn + watchdog; verify down via `Temp\` mtimes; first boot manual |
| R8 | Deployed from a dirty/untagged tree | Phase 0 tag + `DEPLOY_MANIFEST.txt` shipped inside the live scripts folder. Live now: **untagged**, `Turret.cs` dirty and ungated, and existing docs cite hashes that don't exist — so this risk is currently REAL, not theoretical [C1/C2] |

## Verification

End-to-end proof is the gate sequence itself: **G8** (real saves migrate losslessly and idempotently) and **G2/G3** (the two never-live-tested surfaces — vanilla stock GUI and Estates) are the ones that actually de-risk this; G1/G4/G5/G10 are cheap mechanical checks; G6/G7 catch slow leaks and perf regressions; G9 proves the rollback works before it's needed. On live, Phase 4 asserts the boot log line-by-line and Phase 5 exercises the payload against a real character before any player is admitted.
