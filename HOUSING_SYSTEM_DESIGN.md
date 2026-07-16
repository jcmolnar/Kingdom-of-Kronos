# Kingdom of Kronos — Estates & Coffers (Player Housing / Building System)

Full design for a server-side, vanilla-client-safe system that lets players **buy land,
build persistent structures** (walls, force fields, turrets, storage vaults, crafting stations,
banners), and **fund upkeep from a per-estate treasury (a Coffer)**. Structures survive mission
reloads and server restarts. Zero client changes.

> **Naming — deliberate.** "House" is already the four great **Houses** (HouseKronos/Arbal/
> Curama/Yuliple) — a faction/membership system with rank points and base-control objectives
> (`house.cs`, `$HouseMember*`, `$BaseControl*`, `[KoK-Houses].cs`). This system must **not**
> reuse that noun. Player real estate = **Estate**; its treasury = **Coffer**; the buildable
> pieces = **Structures**; the claim token = **Deed**; the manager NPC = **Steward**.

---

## 0. Grounding (what already exists — verified in code)

| Piece | Where | Reuse for Estates |
|---|---|---|
| Deploy primitive | `DeployBase` rpgfunk.cs:511/575, `DeployPlatform` :3308 — `newObject("","StaticShape",<db>,true)` + `addToSet("MissionCleanup",obj)` + `$owner[obj]=name` + `setTeam/setPosition/setRotation` + `startFadeIn` | Spawn every structure |
| Persistence engine | `RequestWorldSave(reason,delay,mode)` rpgfunk.cs:2817, `ProcessWorldSaveQueue` :2863 (token-debounced, earliest-deadline-wins), `SaveWorldDeployables` :2994, `LoadWorld` :3262 | Debounce pattern + rehydrate hook |
| Save schema | `$world::object/owner/pos/rot/team/special[]` (6 fields), `export("world::*", "temp\<mission>_worldsave_.cs", true)`, gated by `$WorldSaveList` membership + sequential-ID sweep from obj id 8362, stops after 15 empty | Reference; Estates use their **own** registry (§6) |
| Export-registry pattern | `HouseData::Save` house.cs:171 — `File::delete` then `export("$BaseControl*",...)` / `export("$HouseMember*",...,append)` to `[KoK-Houses].cs`; `HouseData::Load` execs it | Estate registry file |
| Rehydrate hook | `Server::finishMissionLoad` Server.cs:609 → `MissionCleanup` created :617 → `Mission::init` → `LoadWorld()` scheduled ~18 ticks later :604/646 | Add `Estate::RehydrateAll()` beside `LoadWorld` |
| Money primitives | `fetchData(%c,"COINS")` on-hand, `fetchData(%c,"BANK")` banked; **`storeData(%c,"COINS",%amt,"inc"|"dec")`** incremental add/subtract (Banking.cs:130-148) | Move coin ↔ coffer |
| Economic scale anchor | `HouseEarnings` gameevents.cs:397: `15000 * (remortStep+1)` coins/member/tick at full control | Calibrate all costs |
| Periodic tick | `RecursiveWorld(%seconds)` gameevents.cs:50, 5s cadence, slots `$ticker[1..8]`, pattern `if($ticker[N] >= ($Freq/%seconds)){...; $ticker[N]=0;}` | Upkeep/accrual tick |
| Deployable datablocks | `StaticDoorForceField` (staticshape.cs:749, owner-gated `onCollision` :758, `RecreateForceField` :797); `TurretData` PlasmaTurret / DeployableTurret (Turret.cs, `onAdd→deploy` ~233); platforms DepPlat{Small,Medium,Large}{Horz,Vert}; DeployableTree | Structure catalog base |

**Confirmed live bug (fix as Phase 0):** `LoadWorld` (rpgfunk.cs:3289/3291) calls `DeployForceField`
and `DeployTree` — **neither is defined anywhere** (`function DeployForceField`/`DeployTree`
return zero grep hits). Saved force fields and trees silently fail to rehydrate today. Also
`DepPlatLargeHorz/Vert` are produced by `DeployBase` but the LoadWorld chain only re-instantiates
Small/Medium — Large platforms don't come back either.

---

## 1. Design pillars

1. **Server-authoritative & vanilla-safe.** All state on the server; all UX through chat,
   `bottomprint`, stock menus, and the Steward NPC. No client mod, ever.
2. **Persistence is the feature.** A structure that doesn't come back after a restart is a bug.
   Everything routes through one authoritative registry that is written debounced and replayed
   on load.
3. **The object cap is sacred.** ~1024 general objects, and `patchServerNetcode()` (the raise to
   ~4094) is **deliberately disabled** for pre-1.31 client compat (Server.cs:121). Every design
   decision is subordinate to a global object budget with hard back-pressure (§8). This is the
   difference between "cool feature" and "server won't boot the map."
4. **Coffer or it crumbles.** No free permanence. Structures cost coin to place *and* to keep;
   an insolvent estate decays and is reclaimed, which is also the pressure valve that keeps the
   object budget from filling up with abandoned builds.
5. **No new grief vectors.** Can't wall off towns, spawns, NPCs, or box in players; can't build
   in protected/OOB zones; turrets respect the existing team/House combat rules.

---

## 2. Vocabulary & entities

- **Estate** — one player's claim: a plot center + radius, an owner, a member list, a Coffer, and
  a list of Structures. **One estate per player** (v1). Identified by `%eid` (a small integer) and
  keyed to the owner's character name.
- **Deed** — the belt item you buy from the Steward to found an estate. Consumed on claim.
- **Structure** — a deployed object belonging to an estate (wall, force field, turret, vault,
  station, banner). Each occupies **one** general-object slot.
- **Coffer** — the estate treasury (a coin balance). Pays upkeep automatically; deposit/withdraw
  by owner (and permitted members).
- **Steward** — the NPC (a new botType branch in comchat.cs:10676) that sells Deeds/structures
  and exposes management dialogue. Backed by `#estate ...` chat commands for HUD-less flows.

---

## 3. Data model & registry

Authoritative estate state lives in exported globals, persisted to **`[KoK-Estates].cs`**
(mirroring `HouseData::Save`), **not** in the 6-field `$world::*` schema (too narrow, and its
15-empty-ID-stop sweep is fragile for a growing structure set).

```
// --- per estate (keyed by integer %eid, 1..$Estate::Max) ---
$Estate::Owner[%eid]        = "<charName>";      // authoritative owner
$Estate::CenterPos[%eid]    = "x y z";           // plot center (claim point)
$Estate::Radius[%eid]       = 40;                // plot radius (world units)
$Estate::Coffer[%eid]       = 0;                 // treasury balance (coins)
$Estate::Members[%eid]      = "name1 name2 ..."; // permitted members (space list)
$Estate::Tier[%eid]         = 1;                 // upgrade tier → caps & perks
$Estate::LastUpkeep[%eid]   = <simTime>;         // for catch-up accrual
$Estate::StructCount[%eid]  = 0;

// --- per structure (keyed by %eid,%sid) ---
$Estate::S_Type[%eid,%sid]  = "EstateWall";      // structure datablock/type key
$Estate::S_Pos[%eid,%sid]   = "x y z";
$Estate::S_Rot[%eid,%sid]   = "ax ay az deg";
$Estate::S_Tier[%eid,%sid]  = 1;
$Estate::S_HP[%eid,%sid]    = 100;               // for siege model (§9), else omit
$Estate::S_ObjId[%eid,%sid] = 0;                 // live object id (runtime only, not saved)

// --- reverse index (runtime, rebuilt on load) ---
$Estate::ByOwner[<charName>] = %eid;
$owner[<liveObjId>]          = <charName>;        // reuse existing global for access checks
$EstateOf[<liveObjId>]       = %eid;              // live obj → estate, for demolish/upkeep
```

**Save:** `Estate::Save(%echoOff)` = `File::delete($Estate::File)` then
`export("$Estate::Owner*",file,false)` and appended `export("$Estate::*",file,true)` — the same
delete-then-export idiom as `HouseData::Save` (house.cs:171). `$Estate::S_ObjId`/`$EstateOf`/
`$Estate::ByOwner` are **runtime-only** (rebuilt on load) and must be excluded from export (store
them under a non-exported prefix, e.g. `$EstateRT::*`, so the `$Estate::*` wildcard skips them).

**Debounce:** never call `Estate::Save()` directly from gameplay code. Route through a thin
`Estate::RequestSave(%reason)` that reuses the existing token-debounce discipline
(`RequestWorldSave` pattern) so a burst of edits collapses to one disk write. Save triggers:
claim, build, demolish, coffer deposit/withdraw, tier upgrade, member change — each a
`RequestSave` with a few-second delay.

---

## 4. The Coffer system (the money)

### 4.1 Wallet integration
Coin moves between a player's on-hand `COINS` funkvar and the estate Coffer using the existing
incremental primitive:

```
// deposit %amt from owner's hand into coffer
if(fetchData(%cl,"COINS") >= %amt) {
    storeData(%cl,"COINS",%amt,"dec");
    $Estate::Coffer[%eid] += %amt;
    Estate::RequestSave("coffer_deposit");
}
// withdraw (owner only) reverses it
```

Also allow **BANK → Coffer** transfers (both are just funkvars) for convenience at the Steward.

### 4.2 Upkeep (the drain)
Every structure has a per-tick upkeep. On the upkeep tick (§4.4) the estate's total upkeep is
debited from the Coffer:

```
%due = 0;
for(%s=1; %s<=$Estate::StructCount[%eid]; %s++)
    %due += $Estate::UpkeepOf[$Estate::S_Type[%eid,%s], $Estate::S_Tier[%eid,%s]];
$Estate::Coffer[%eid] -= %due;
```

### 4.3 Insolvency → grace → decay → reclaim (state machine)
This is the object-cap pressure valve; specify it exactly.

```
Coffer >= due        → SOLVENT.        Debit; clear any grace flag.
Coffer < due, >=0    → debit to 0, enter GRACE. Stamp $Estate::GraceStart[%eid].
Coffer == 0, in grace, GRACE window (e.g. 3 real days via kronos_datetime) elapsed
                     → DECAY. Each subsequent upkeep tick, remove the newest / least-defended
                       structure (or lowest-priority), free its object slot, refund 0.
All structures gone  → estate DORMANT. Plot claim held for a longer window (e.g. 14 days),
                       then RECLAIMED: $Estate::* freed, owner may re-found elsewhere.
Owner tops up coffer before decay completes → back to SOLVENT; decay halts.
```

Grace/decay windows use **real-world time** via `kronos_datetime.dll` `getRealDate()` (already a
dependency for dailies), not sim time, so a restart doesn't reset the clock. Warn the owner via
`~house`-tagged chat on login when in GRACE.

### 4.4 Accrual & the tick hook
Upkeep runs on a **new estate tick** folded into `RecursiveWorld` right after `HouseEarnings()`
(both are periodic economy). Add one ticker slot:

```
if($ticker[8] >= ($EstateUpkeepFreq / %seconds)) {   // reuse/repurpose a slot; audit 1..8 first
    Estate::UpkeepTick();
    $ticker[8] = 0;
}
```

`$EstateUpkeepFreq` ≈ **3600s** (hourly) — slow enough that a modest coffer lasts days,
fast enough that abandonment reclaims slots within a session or two. `Estate::UpkeepTick()`
iterates estates, applies §4.3, and `RequestSave("upkeep")` once at the end.

> **Ticker audit needed:** slots 1–8 are reportedly all used (save/weather/day-night/arena/
> economy/prize/sound/HouseEarnings). If none is free, run the estate tick on its **own**
> self-rescheduling loop started from `Mission::init` (gameevents.cs:1) using the generation-token
> pattern (no `cancel()`), independent of `RecursiveWorld`. Either works; a dedicated loop is
> cleaner and avoids fighting for a slot.

### 4.5 Optional: passive income
Estates can *earn* (rent/production) as a coin sink-balancer — e.g. a **Storage Vault** or
**Workshop** structure yields a trickle into the Coffer per tick, capped so upkeep still dominates.
Keep net cash-flow negative at rest so coffers must be actively fed. (Design lever, off by default.)

---

## 5. Structure catalog

Costs calibrated to the `15000×(remort+1)` earnings anchor: a fresh player earns thousands/tick,
so a starter wall in the low-thousands and a turret in the tens-of-thousands feels right.
**Object cost = 1** for every structure (each is one general-pool object). Tune all numbers in a
single `$Estate::Cost[type,tier]` / `$Estate::Upkeep[type,tier]` table.

| Structure | Base datablock | Build cost | Upkeep/hr | Obj | Notes / effect |
|---|---|---|---|---|---|
| **Wall / Barrier** | StaticShape platform (`DepPlat*`) | 2,000 | 50 | 1 | Reuse existing platform shapes; snap to plot |
| **Force Field (door)** | `StaticDoorForceField` | 8,000 | 300 | 1 | Owner/member-gated `onCollision` (staticshape.cs:758) |
| **Turret (plasma)** | `PlasmaTurret`/`DeployableTurret` | 25,000 | 1,000 | 1 | Team = owner's; §7 balance; per-estate cap |
| **Storage Vault** | StaticShape (new/borrow) | 15,000 | 200 | 1 | Personal item stash (ties to backpack.cs Bank\<type\>) |
| **Crafting Station** | StaticShape | 20,000 | 250 | 1 | Enables/《discounts》 crafting (feature #20) at estate |
| **Banner / Flag** | StaticShape | 3,000 | 25 | 1 | Cosmetic; shows owner name/House; claim marker |
| **Teleport Pad** | StaticShape | 40,000 | 1,500 | 2 | Optional; pad↔pad within owner's estates |
| **Beacon / Spawn Point** | StaticShape | 30,000 | 800 | 1 | Optional respawn at estate (PvE zones only) |

Tiers upgrade a structure in place (higher HP / turret damage / vault capacity) for a cost
multiplier and higher upkeep. Cosmetic-only structures (banner) have trivial upkeep so a broke
estate keeps its marker last.

---

## 6. Spawning & deployment pipeline

Reuse the proven primitive; wrap it per structure type.

```
function Estate::SpawnStructure(%eid, %sid) {
    %type = $Estate::S_Type[%eid,%sid];
    %db   = $Estate::DB[%type];                       // type → datablock name
    %obj  = newObject("", "StaticShape", %db, true);  // (or "Turret"/"Item" per type)
    addToSet("MissionCleanup", %obj);                 // dies on reload — we rebuild from registry
    GameBase::setTeam(%obj, Estate::OwnerTeam(%eid));
    GameBase::setPosition(%obj, $Estate::S_Pos[%eid,%sid]);
    GameBase::setRotation(%obj, $Estate::S_Rot[%eid,%sid]);
    GameBase::setMapName(%obj, %db);
    GameBase::startFadeIn(%obj);
    $owner[%obj]   = $Estate::Owner[%eid];            // reuse existing access-check global
    $EstateRT::S_ObjId[%eid,%sid] = %obj;             // runtime link
    $EstateOf[%obj] = %eid;
    return %obj;
}
```

**Interactive build flow** (player-driven):
1. Buy a structure item (belt item) from the Steward → `getBuyCost`/`buyItem` (economy.cs:170)
   deducts COINS.
2. Player uses/deploys it (`Item::onDeploy` itemevents.cs:737, `depbase.cs` pipeline). Compute
   placement position from the player's aim/`getLOSInfo` (as `depbase` already does).
3. **Validation gauntlet** (all must pass, else refund & message):
   - inside the player's own estate plot (dist(center, pos) ≤ radius);
   - not in a protected/town/OOB zone (reuse `depbase.cs` zone guard — *confirm exact var*);
   - surface normal/slope acceptable (depbase already checks);
   - no overlap with an existing structure (min-separation radius check);
   - estate structure cap not exceeded (per-tier) and **global object budget OK** (§8).
4. Register: append to `$Estate::S_*[%eid, ++StructCount]`, `Estate::SpawnStructure`,
   `Estate::RequestSave("build")`.

**Load flow** (`Estate::RehydrateAll()`, hooked at Server.cs:646 next to `LoadWorld`):
`Estate::Load()` execs `[KoK-Estates].cs` → for each `%eid`, rebuild runtime indices and
`Estate::SpawnStructure` every `%sid`. Because structures are in `MissionCleanup` (destroyed on
reload), the registry is the single source of truth; there is no double-spawn risk if rehydrate
runs exactly once after cleanup.

---

## 7. Turrets (special handling)

- **Object pool, not id pool.** Deployed turrets/StaticShapes get general-pool object ids (the
  save sweep probes ids from 8362 upward), **not** the 2049–2175 BaseRep pool — confirmed. So
  turrets do **not** compete with players/bots/companions for the 127 slots. They spend from the
  1024 object cap instead.
- **Team & targeting.** Set turret team to the owner's team so it engages hostiles under the
  existing combat rules. In this mod everyone is nominally team 0 (Citizen) with House affiliation
  tracked via `MyHouse` (house.cs:115) — so "enemy" for a turret must be defined explicitly:
  target enemy-House players / flagged-hostile / enemy bots, **never** same-House or town NPCs.
  This needs a targeting predicate wired to the House model (design choice in §9).
- **Balance.** High build + upkeep cost; strict per-estate cap (e.g. tier-gated 1→3); damage
  tuned well below player weapons so turrets deter, not dominate. Turrets in GRACE/decay lose
  power first.
- **Perf.** Each active turret is AI-ish load; the per-estate cap + global budget bound total
  turret count server-wide.

---

## 8. Object-cap safety architecture (the critical constraint)

Single global budget, checked before **every** spawn, with headroom reserved for the engine,
NPCs, projectiles, lootbags, and vehicles.

```
$Estate::GlobalObjBudget = 400;      // max objects Estates may hold server-wide (tune below cap)
$Estate::GlobalObjInUse  = <sum of all live structures>;   // maintained on spawn/despawn
```

- **Reserve, don't hope.** Before spawning, require `GlobalObjInUse + cost <= GlobalObjBudget`
  **and** an engine-level sanity check on current total object count. If either fails, refuse the
  build ("The realm's builders are overextended — try later") and refund.
- **Per-player & per-estate caps** as the first-line limiter (e.g. tier 1 = 8 structures, tier 3 =
  24), so no single player can exhaust the budget.
- **Decay is the release valve** (§4.3): insolvent estates free slots automatically, so the budget
  self-heals without admin intervention.
- **Budget math.** Assume ≤400 estate objects. At an average 10 structures/estate that's ~40 fully
  built estates concurrently — generous for this playerbase, comfortably under 1024 with room for
  everything else. Revisit only if `patchServerNetcode()` is ever enabled (it currently is not,
  and enabling it breaks pre-1.31 clients — see Server.cs:121).

---

## 9. Ownership, permissions, PvP/siege

- **Access control.** Reuse `$owner[obj]` (already stamped by the deploy primitives and consulted
  by the force-field `onCollision` gate, staticshape.cs:758). Extend the gate to also pass
  `$Estate::Members[%eid]`. Only owner can demolish, upgrade, withdraw from coffer, or edit
  membership; members can pass force fields and use vault/stations (configurable).
- **Demolish & refund.** Owner demolishes a structure → delete object, free budget slot, refund a
  fraction (e.g. 25%) of build cost to the coffer (discourages build/refund churn).
- **Siege model — pick one (design decision for you):**
  - **A. Sanctuary (simplest, safest).** Estates are non-combat: structures are invulnerable,
    turrets optional, no raiding. Coffers only lost to upkeep. Least grief, least PvP depth.
  - **B. Raidable (adds the `S_HP` field).** Structures have HP; enemy-House players can damage/
    destroy them; a breached estate exposes its Vault / a slice of its Coffer. Richer, but needs
    careful anti-grief (attack windows, defender notifications, cooldowns) and more object churn.
  - **C. Hybrid.** Sanctuary in PvE zones, raidable in contested/PvP zones — ties into the
    existing zone system (`$ZonePlayerCount`, connectivity.cs) and the House base-control loop.
  - Recommendation: ship **A** first (fastest, safest, proves persistence + coffers), design the
    `S_HP` field in from day one (store it, default invulnerable), then enable **B/C** as a later
    phase once the economy and object budget are proven stable.

---

## 10. Anti-grief & placement rules

- No building in towns, shops, NPC areas, spawn points, OOB, or existing protected zones (reuse
  `depbase.cs` zone guard).
- No claiming a plot overlapping another estate or too near town/objectives (min distance).
- Plot radius bounds where structures may go; can't project structures outside your plot.
- Can't fully enclose a public path/NPC/player — enforce via the overlap/separation check plus a
  "no structure within N units of an NPC/spawn" rule.
- Force fields default to owner+members-only pass, but **cannot** be placed to trap players in
  public space (plot-bounds rule handles this).
- Rate-limit builds (per-player cooldown) to prevent object-spam even within budget.

---

## 11. Commands & NPC UX (all vanilla-safe)

Steward NPC (new botType branch, comchat.cs:10676) + `#estate` verbs via the `remoteSay` router
(comchat.cs:43). All output via chat (`~house` tag), `bottomprint`, or stock menu (gui mode 4).

```
#estate found              claim plot at your location (consumes a Deed)
#estate info               show plot, tier, coffer, structures, upkeep/hr, solvency
#estate build <type>       buy+place a structure (or use the Steward menu)
#estate demolish           demolish structure you're aiming at (owner)
#estate deposit <amt>      COINS/BANK → Coffer
#estate withdraw <amt>     Coffer → COINS (owner)
#estate permit <name>      add/remove a member
#estate upgrade            raise estate tier (caps/perks)
#estate abandon            tear down & reclaim plot (partial refund to coffer→owner)
```

Long lists chunk under the 255-char cap (reuse `AutoSkill_SendSafeMessage` pattern).

---

## 12. Persistence correctness & edge cases (checklist)

- **Rehydrate exactly once**, after `MissionCleanup` is (re)created and before players act
  (Server.cs:617/646). Guard with a `$Estate::Rehydrated` flag per mission load.
- **No double-spawn**: structures live only in `MissionCleanup` (transient) + the registry
  (authoritative). Never save live object ids (`$EstateRT::*` excluded from export).
- **Debounced writes only** via `Estate::RequestSave` — avoid disk thrash and mid-save races
  (mirror `ProcessWorldSaveQueue`'s `$WorldSaveInProgress` re-arm).
- **Restart safety**: real-time grace/decay clocks (kronos_datetime) so downtime doesn't nuke or
  freeze estates.
- **Owner rename/deletion**: estate keyed to char name — decide policy if a character is deleted
  (reclaim after dormancy).
- **Object-budget accounting stays consistent** across demolish/decay/reload (recount on load,
  don't trust a saved counter).
- **Fix the legacy `DeployForceField`/`DeployTree` gap** and the missing `DepPlatLarge*` reload
  branch while in `LoadWorld` — even though Estates use their own registry, the generic world-save
  path is still live for platforms/forcefields deployed the old way.

---

## 13. Implementation phases

- **Phase 0 — bugfix (ships alone):** define `DeployForceField`/`DeployTree` (mirror
  `DeployPlatform`), add `DepPlatLarge*` to the `LoadWorld` chain. Verifiable immediately: deploy a
  force field, reload the mission, confirm it returns.
- **Phase 1 — Estate core:** registry (§3), `Estate::Save/Load/RehydrateAll`, `#estate found/info`,
  Deed item, plot claim + zone/overlap validation, one Wall + Banner structure. Prove persistence.
- **Phase 2 — Coffer:** deposit/withdraw, upkeep tick (§4.4), insolvency→grace→decay→reclaim
  (§4.3), object budget (§8). Prove an abandoned estate self-reclaims.
- **Phase 3 — Structure catalog:** force fields (owner/member gate), storage vault, crafting
  station, turrets (§7) with per-estate caps and team targeting.
- **Phase 4 — Steward NPC & polish:** dialogue menus, upgrade tiers, member permissions,
  `#estate` full verb set, chat chunking.
- **Phase 5 — Siege (optional):** enable `S_HP`, raiding model B/C, defender notifications,
  zone-gated PvP.

---

## 14. Open items to confirm before coding (blocked by a session limit during research)

These don't change the architecture but need a 5-minute code check before implementation:
1. **Exact `depbase.cs` deploy guards** — the precise function names + the protected-zone variable
   used for the placement gauntlet (§6 step 3). Known to exist (LOS/normal/zone); need identifiers.
2. **Turret datablock specifics** — exact `PlasmaTurret`/`DeployableTurret` field names, how team/
   target is set, and whether they self-register in a set on `onAdd→deploy` (Turret.cs ~233).
3. **`buyItem` deduction path** — confirm `buyItem` (economy.cs:170) is the clean hook to charge
   for structure items, or whether to charge directly via `storeData(...,"COINS",...,"dec")`.
4. **`RecursiveWorld` ticker slot availability** — audit which of `$ticker[1..8]` is free; if none,
   use a dedicated `Mission::init` loop for `Estate::UpkeepTick` (§4.4).
5. **Turret targeting predicate** — how to define "hostile" given the everyone-on-team-0 + House
   affiliation model (§7/§9); reuse whatever `AI::ContinuousAttack` uses to pick enemies.

> **All five resolved during implementation** (2026-07-12): (1) zone gate = `Zone::getType(fetchData(%cl,"zone"))=="PROTECTED"`, placement = `GameBase::getLOSInfo(%cl,3)` → `$los::position`/`$los::normal`; (2) `DeployableTurret` is self-powered, spawned `newObject("","Turret",db,true)`; (3) charge directly via `storeData(%cl,"COINS",amt,"dec")` (money = `COINS`/`BANK` funkvars); (4) all 8 tickers used → dedicated `Estate::Init` loop from `Mission::init`; (5) `Turret::verifyTarget` is the per-target script callback — estates reject owner+members there.

---

## 15. AS-BUILT status (2026-07-12, `testing` branch, uncommitted, NOT runtime-tested)

Phases 0–4 implemented. Files: **`rpg/scripts/Estate.cs`** (new, ~640 lines, 36 fns) + hooks in
`rpgfunk.cs` (Phase 0), `Server.cs` (exec + rehydrate), `comchat.cs` (`#estate` router),
`gameevents.cs` (`Estate::Init` in `Mission::init`), `staticshape.cs` (force-field member gate),
`Turret.cs` (`verifyTarget` owner/member immunity).

Deviations from the design above worth noting:
- **Persistence is flat 1-D**, not 2-D `[%eid,%sid]` (§3 showed 2-D) — structures are a flat list
  keyed by `%k` with `$Estate::SEstate[%k]` back-pointer, because 2-D `export`/`exec` round-trip is
  unproven on this engine. Namespaces: `$Estate::*` (exported) / `$EstateCfg::*` / `$EstateRt::*`.
- **Grace/decay measured in upkeep TICKS (uptime hours), not `getRealDate()` calendar days** —
  `getRealDate()` format is unverified here; tick-counting is robust and persists.
- **Deed is command-driven** (`#estate found`) not a belt item (deferred to a later UX pass).
- **Banner / storage vault / crafting station / Steward NPC deferred** (banner: no safe datablock;
  vault/crafting: need Belt.cs depth / the crafting feature; Steward: low value with no vanilla GUI).
- **Siege (Phase 5) not built** — structures are effectively invulnerable (Sanctuary model); the
  `S_HP` field is *not* yet in the registry (add it when siege is built).

---

## 16. Dev-server test checklist (Phases 0–4)

Run on a **dev/non-production** server. Per the dev workflow, edits made here must be hand-copied to
wherever you actually test. Watch the console for `[ESTATE] ...` lines throughout.

### Setup — temporary accelerators (REVERT after testing)
In `Estate.cs` config, temporarily set so decay happens in minutes, not days:
```
$EstateCfg::UpkeepFreq   = 30;   // was 3600  (tick every 30s)
$EstateCfg::GraceTicks   = 2;    // was 72    (decay after ~1 min insolvent)
$EstateCfg::DormantTicks = 3;    // was 168
$EstateCfg::FoundCost    = 100;  // was 25000 (so you can test without grinding coins)
```
Also give your test character plenty of coins. **Restore all four values when done.**

### Phase 1 — claim, build, persist  (the core proof)
1. Stand in open (non-town) ground → `#estate found` → expect "Estate founded!"; `#estate info` shows it.
2. `#estate found` again → "You already own an estate."
3. Walk into a town/protected zone → `#estate found` (on a 2nd char) → refused.
4. Aim at flat ground inside your plot → `#estate build platform` → a platform appears; `#estate info` lists it.
5. `#estate build wall` → a vertical wall appears. `#estate build forcefield` → a force field appears.
6. Aim at the sky → `#estate build wall` → "Aim at the ground." Aim outside plot → "outside your plot."
7. **Persistence:** note structure positions, then reload the mission (or restart the server). Console should log `[ESTATE] Rehydrated N structure(s)`. Confirm every structure reappears in the same spot and `#estate info` is unchanged.

### Phase 2 — coffer & decay
8. `#estate deposit 5000` → coffer rises, carried coins drop. `#estate withdraw all` → reverses.
9. `#estate deposit 200`, then wait through upkeep ticks (30s each with the accelerator). `#estate info` should show the coffer dropping by the upkeep/hr each tick.
10. Let the coffer hit 0 → expect a "COFFER EMPTY — structures decay in ~N" warning, then after `GraceTicks` ticks, structures **crumble one per tick** (newest first) with a notify. Confirm object count drops.
11. Deposit again mid-decay → decay **stops**, "coffer funded again."
12. Let an estate fully decay + stay broke past `DormantTicks` → the **plot is reclaimed** (console notify); `#estate info` → "You have no estate."

### Phase 3 — permissions, turrets, tiers
13. Build a force field. Have a **second player** walk into it → "Access denied." `#estate permit <name>` on them → they now pass; `#estate evict <name>` → denied again.
14. `#estate build turret` at tier 1 → refused ("require tier 2"). `#estate upgrade` → tier 2, cap rises. Now `#estate build turret` works.
15. **FRIENDLY-FIRE (critical):** stand near your own turret → it must **NOT** shoot you. A permitted member → **NOT** shot. Bring a **hostile** test player (different House, or houseless non-member) into range → turret **engages** them. Reload the mission → the rehydrated turret must **still spare you**.
16. `#estate build turret` a 4th time → "Turret limit reached (3)."

### Phase 4 — ops & QoL
17. `#estate where` (from off-plot and on-plot), `#estate help`.
18. As an **admin (level ≥4)**: `#estate list` shows all estates + object budget; `#estate reclaim <name>` removes another player's estate. As a non-admin → "for admins."

### Regression (gpu-off equivalent: prove no collateral damage)
19. Confirm **legacy** deployables still work: deploy a normal base with `DepBasePack`, and confirm non-estate force fields still gate on owner/grouplist only (Phase 0 hooks are backward-compatible).
20. Confirm **existing turrets** (mission/objective turrets) still target normally (the `verifyTarget` estate block no-ops when `$EstateOf[%turret]` is empty).
21. **REVERT the §16 accelerator config**, reload, and confirm normal cadence.

### What to watch in logs
- `[ESTATE] Rehydrated N structure(s) across M estate slot(s).` on every mission load.
- `[ESTATE] Saved registry to temp\<mission>_estates_.cs` after edits (debounced ~5s).
- `[TURRET DEBUG] verifyTarget()` lines confirm targeting decisions (already present in Turret.cs).
