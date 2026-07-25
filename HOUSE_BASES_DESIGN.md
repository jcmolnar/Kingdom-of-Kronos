# House Bases — Faction Compounds Design

Status: DRAFT (2026-07-18). Supersedes the *player-housing* framing of `HOUSING_SYSTEM_DESIGN.md`
for faction content; merges `rpg/scripts/analysis/HOUSE_TREASURY_SYSTEM_TRACKER.md` (coffer/
steward/governance) with the built Estate engine (`rpg/scripts/Estate.cs`) as the construction
backbone. Estates (per-player) remain a separate, still-gated feature; this system reuses its
machinery under new house-keyed state.

## 1. Vision

Replace the map-hardcoded flag-pedestal "bases" with **player-built House compounds**. Each of the
four Houses (HouseKronos, HouseYuliple, HouseArbal, HouseCurama) founds a compound in the world,
purchases buildings, walls, turrets, and **flag pedestals**, and defends them. Structures can be
**damaged but never destroyed** — once built, a House cannot lose a structure, but degraded
structures lose function until repaired. Repairs draw from a **system-owned House coffer** that is
funded only by house reward windows. Compounds are compass-trackable and present a named zone
("House Kronos Compound").

Deviation from the treasury tracker: bases are **ground compounds**, not floating bases. The
Steward NPC is retained for governance UX and as the invasion "reset" objective, but compounds are
directly assaultable — so anti-enclosure rules and the damage model are first-class, not optional.

## 2. Economy — two separate streams

### 2a. Building money (personal, spendable on construction)
- New per-player currency: **House Marks** (funkvar `HouseMarks`, storeData inc/dec like COINS).
- Earned ONLY during **house reward windows** (`HouseEarnings()`, gameevents.cs:335, 15-min tick),
  alongside the existing exp/coins/rp/gold payouts. AFK players count (existing behavior, by
  design — rewards presence).
- Scaled by the existing control-share multiplier (0.25 floor + 0.75 × objective share).
- Spent at the Steward (or `#house build`) to purchase structures for the compound. Non-tradeable,
  non-bankable, house-locked: on leaving a house, Marks zero out (removes join/leave arbitrage).
- Marks make construction a *collective grind*: any member can contribute Marks toward a structure
  (structures have a Mark price; partial funding accumulates per structure order until paid).

### 2b. House Coffer (system-owned, repairs only)
- `$House::Coffer[%h]` (h = 1..4). Players can NEVER deposit or withdraw.
- Funded only inside `HouseEarnings()`: `income = BasePerMember × membersOnline × controlMult`.
- Spent ONLY on repairs (see §5). No upgrades, no purchases — this deviates from the tracker's
  "all purchases from coffer" model per the new direction; the tracker's spend-cap/quorum
  machinery therefore only applies to repair authorization (simplified: Minor tier only).
- Persisted in `HouseData::Save/Load` (`[KoK-Houses].cs`) with safe defaults + clamping on load.

## 3. Compound construction (Estate engine, house-keyed)

Generalize `Estate.cs` registries rather than forking wholesale: add an owner-type dimension so a
"house estate" is an estate whose owner key is `#H1..#H4` instead of a character name. Concretely:

- `$Estate::Owner[%eid] = "#H" @ %house` — sentinel prefix `#H` marks house compounds; all
  member/permission checks route through `IsSameHouse` instead of the member list.
- **Founding**: officer-tier action (rank threshold via RankPoints, TBD) + Mark cost. One compound
  per house initially (`$HouseCfg::MaxCompounds=1`, raisable later). Same placement rules as
  estates: no PROTECTED zones, 100-unit town clearance, min distance from other estates AND other
  house compounds (larger: 300 units between rival compounds).
- **Building**: any house member with sufficient Marks uses the estate build flow
  (`Estate::Build` path: LOS ground-aim, slope check, plot radius, 6-unit min separation).
  Catalog = existing `$EstateCfg::DB` structures **plus** `FlagStand` (see §4). Plot radius larger
  than player estates (e.g. 80), global object budget per house (e.g. 150).
- **Persistence**: rides the existing estate registry save (`temp\<mission>_estates_.cs`,
  `RehydrateAll` in finishMissionLoad). New per-structure fields: `$Estate::S_HP[%k]` (see §5).
- **Zone**: default compound name = `"House <name> Compound"` via the existing display-only
  `Estate::ZoneLoop` tab-column override. Renameable by officers (32-char cap, same as estates).
- **Turrets**: existing `Turret::verifyTarget` house logic already fires on different-House
  players when `.Team` is a house — set spawned turret `.Team` to the owning house.

## 4. Purchasable flag pedestals

- Remove (or leave dormant) the 16 `instant StaticShape FlagStand` objects in
  `KingdomKronos.mis:305+` once the system ships; during transition both can coexist.
- `FlagStand` becomes a buildable catalog entry (high Mark cost, Critical tier: requires the
  tracker's quorum vote — max(1, ceil(Active72h × 0.5)) capped at 3, solo-house 10-min delay).
- On spawn, script performs what `FlagStand::objectiveInit` did at mission load: link into
  `$teamFlagStand[%team]`, register with the objectives refresh (`RecalcHouseObjectives`).
- Per-house pedestal cap: **4** (own-house stands only — a house buys stands for ITS flags;
  capturing rivals' flags still happens by carrying to your own stand, unchanged
  `FlagStand::onCollision` logic).
- Flag home position = the purchased stand's position; `SaveHouseObjectives` already persists
  flag positions/holders and needs no format change.
- Pedestals are structures: damageable-never-destroyed like everything else; a heavily damaged
  stand (< 25% HP) cannot **receive** a capture until repaired (defensive counterplay), but its
  own flag remains functional.

## 5. Damage model — damaged, never destroyed

New for the mod (current "invulnerability" is just maxDamage=10000; no repair mechanic exists).

- Per-structure HP: `$Estate::S_HP[%k]` (0..100), persisted. Structures spawn at 100.
- Hook `StaticShape::onDamage` / `Turret::onDamage`: if `$EstateOf[%obj]` resolves to a house
  compound, convert incoming damage to HP loss and **clamp so engine damageLevel never reaches
  maxDamage** — `onDestroyed` must never fire for house structures. Visual: use engine damage
  level up to ~95% for smoke/debris states where datablocks support it.
- **Degradation thresholds** (function loss, object persists):
  - ≤ 50%: turrets fire rate halved; force-field flicker (periodic passable window).
  - ≤ 25%: turrets offline (`onDisabled` path), force fields down (passable), doors stop gating,
    flag stands can't receive captures.
  - 0%: fully inert but standing. Never removed.
- **Only rival-house players can damage house structures** (pre-filter like townbot immunity);
  own-house and houseless players do 0. PROTECTED-zone rules don't apply (compounds can't be
  founded there anyway).
- **Repair**: `#house repair` at the structure (LOS aim, like build) or "Repair All" at Steward.
  Cost = `CostPerHP × HP missing × structure tier`, drawn from `$House::Coffer` only. Any member
  can trigger (Minor tier — no vote, short per-player cooldown to stop spam-drain griefing from
  a hostile infiltrator; optional: repairs disabled for members < 72h in house).
- **Combat lockout**: structure repairs blocked for N minutes after last taking rival damage
  (prevents heal-tanking during a raid; N ≈ 5, tuning knob).

## 6. Compass tracking

- Stock vanilla-safe primitive: `issueCommand(%client, %client, 0, "text~wcapobj", x, y)` +
  `setCommandStatus` clear (same as `Flag::setWaypoint`, objectives.cs:918-954).
- `#house track [kronos|yuliple|arbal|curama|off]` sets a waypoint on that house's compound
  center. One waypoint at a time (stock compass limit) — a toggle/target-select, not
  all-four-at-once. Respect `%client.autoWaypoint` interaction: manual `#house track` overrides;
  flag pickup re-claims the waypoint as today.
- Own compound trackable always; rival compounds trackable always (they're territorial
  objectives, not hideouts — founding one is a public act, announced server-wide).

## 7. Griefing & anti-enclosure

The estate engine has NO anti-enclosure logic (players can fully box objects in with RForceField
walls). For contested objectives this must be closed:

1. **Pedestal clearance rule (hard, cheap)**: no structure may be placed within `PedClear` units
   (10) of any flag stand, and no stand may be placed within `PedClear` of existing structures.
   Enforced in the `Estate::Build` validation path both directions. Kills the layered-onion
   burial exploit without pathfinding.
2. **RForceField excluded** from the house catalog entirely; only `StaticDoorForceField` (which
   goes passable at ≤25% HP) is available. Every barrier therefore has a damage-based bypass.
3. **Steward reset valve** (from tracker): each house has a Steward NPC in its home town. Killing
   a rival Steward returns all flags that house has captured to their owners and locks that
   house's upgrades for 24h (`StewardIsDown`/`StewardRespawnAt`, persisted). Guaranteed
   counterplay against a runaway house even if its compound is impractical to raid.
4. **Budgets**: per-house object budget (150) + min separation + plot radius bound total wall
   mass. Existing global estate budget (400) counts house structures too.
5. **Flag starvation**: a captured rival flag sitting on a stand uncontested for 12h (tuning
   knob) auto-returns home. Combined with the 25% reward floor, total lockout is bounded.
6. **Hostile-infiltrator drain**: repair/build actions log to the house ledger (tracker's
   `RecentLedger`); per-player repair cooldown; officers can boot members (existing
   `BootFromCurrentHouse`).

## 8. Persistence summary

| State | Where |
|---|---|
| Compound + structures + S_HP | estate registry (`temp\<mission>_estates_.cs`) |
| Coffer, ledger, Steward lifecycle, pedestal unlock counts | `HouseData::Save/Load` → `[KoK-Houses].cs` |
| Flag positions/holders, BaseControl/FlagCommand | `temp\HouseObjectives.cs` (unchanged) |
| House Marks | per-player funkvar (existing storeData path) |

All loads clamp negatives/invalids (int32-safe per the economy-fix conventions, 5118b1b) and
default missing keys (old saves must load clean). Version marker `$House::SchemaVer`.

## 9. Build order (stages, each live-testable)

1. **Marks + coffer income** — add both streams to `HouseEarnings()`; persistence; `#house
   status` readout. No world objects yet. (Small, ships value: visible accrual.)
2. **House compounds** — estate-engine generalization (`#H` owner keys), founding, building from
   the existing catalog with Marks, zone naming, persistence, turret teams. Pedestal clearance
   rule + RForceField exclusion from day one.
3. **Damage/repair** — S_HP, clamp-never-destroy, degradation thresholds, coffer-funded repair,
   combat lockout, rival-only damage filter.
4. **Purchasable pedestals** — FlagStand catalog entry, quorum gate, runtime objectiveInit
   wiring, mission-file stands retired, capture-block at low HP, 12h auto-return.
5. **Compass + Steward** — `#house track`; Steward NPC (comchat branch) with status/repair-all/
   ledger menu and the death→flag-return→24h-lock invasion valve.

Stage order front-loads the economy (needed by everything) and defers the two riskiest pieces
(objective rewiring, NPC state machine) until the compound loop is proven.

## 10. Open tuning knobs

Mark prices per structure, coffer income rate, CostPerHP, degradation thresholds, PedClear,
combat-lockout minutes, auto-return hours, rival-compound min distance, pedestal quorum sizes,
officer rank threshold, repair cooldowns. All `$HouseCfg::*` so live-tunable without re-exec.

## 11. Out of scope

Estates (per-player) UX changes; House Oaths questline; invasion *scheduler* (PvE waves) from the
tracker — only the Steward death consequences are in scope here; per-lixel visual damage states.
