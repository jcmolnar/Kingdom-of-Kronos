# Kingdom of Kronos — New Feature Design

Status: design / planning. Scope constraint: **server-side only** — every feature must work
for unmodified stock Tribes 1.30 clients (pure server TorqueScript or a native `getPlugin()`
DLL, zero client changes). Communication channels available to vanilla clients: chat (with
`~category` tags stripped by the engine), `bottomprint`, stock menus (gui mode 4), the
server-driven scoreboard (`Client::setScore`), and sounds.

All file references are `rpg/scripts/*.cs` unless noted.

---

## Two hard ceilings (they do not overlap — convenient)

| Ceiling | Value | Who draws from it | Enforcement |
|---|---|---|---|
| **BaseRep id pool** | 2049–2175 = **127 slots** | players **+** companions **+** roaming merchants **+** all town/enemy bots | `PlayerManager::getFreeId` (ai.cs:5053) fails gracefully; needs a budget policy |
| **General object cap** | ~**1024** objects | housing/building deployables (StaticShapes, turrets, force fields) | `patchServerNetcode()` (would raise to ~4094) is **deliberately disabled** (Server.cs:121) for pre-1.31 client compat → per-player caps + decay are mandatory, not optional |

The usual TorqueScript taxes apply throughout: no `schedule cancel()` (use the
generation-token pattern), 255-char `remoteEval`/chat cap (chunk), ~256-char funkvar field
cap (split like `SplitAndSaveBankStorage`).

---

## Feature catalog

### 1. New weapon procs — `small`
Per-weapon on-hit callbacks already exist (`SkyRender::Impale` playerdamage.cs:5322,
`EchoFang::Echo` :5357), fired from `Player::onDamage` (:2925).
1. Data registry `$WeaponProc[weaponName] = "type param..."` in weapons.cs.
2. In `onDamage`, after final damage, dispatch attacker's weapon to a proc handler.
3. Handlers: Lifesteal, Chain-lightning / Cleave (radius search + `Bomb1..26` FX), Armor-shred
   (temp DEF debuff on victim, token-scheduled decay).
- **Gotcha:** proc damage must set a re-entrancy flag so it can't recurse into `onDamage`.

### 2. Backstab damage — `medium`
Hook `Player::onDamage` (:2925). Compute victim forward vector vs `normalize(attackerPos −
victimPos)`; dot over threshold ⇒ behind ⇒ damage multiplier, gated + trained by a new
Backstab skill (`$SkillDesc`/`$SkillRestriction` + `UseSkill` skills.cs:941).
- **Risk:** deriving victim facing from the transform (MathPlugin vector ops); tuning is the work.

### 3. Enchanting gear — `medium`
Base accessory stats fold in via `Belt::ApplyAccessoryStats` (Belt.cs:3488) / `AddPoints`
(Accessory.cs:351); recompute via `RefreshAll` (rpgfunk.cs:5664).
1. Enchants are **per-character, per-slot** (belt items are shared string templates) — store in
   a funkvar `Enchant_<slot> = "statchar val ..."`.
2. `#enchant` command/NPC (router comchat.cs:43) consuming materials + gold, RNG success.
3. Fold enchant bonus into `ApplyAccessoryStats`; `RefreshAll` on change; persists via `SaveCharacter`.

### 4. Estates — player housing / building system — `large` (see `HOUSING_SYSTEM_DESIGN.md`)
Full deployable-structure system with per-estate **Coffers** to fund upkeep. Infrastructure
largely exists (`SaveWorldDeployables`/`LoadWorld` persistence, deploy datablocks, deploy
pipeline). Dedicated design doc; summary of the shape:
- Buy a **Deed** → claim a plot → deploy structures (walls, force fields, turrets, stations,
  storage) that **persist** across mission reload and restart.
- Each estate has a **Coffer** (treasury) that pays automatic upkeep on the `RecursiveWorld`
  tick; empty coffer ⇒ structures decay ⇒ eventually reclaimed (keeps the object cap safe).
- **Live bug fixed as step 0:** `LoadWorld` calls `DeployForceField`/`DeployTree` which are
  **undefined** — saved force fields/trees never rehydrate today.

### 5. New quest lines — `medium-large`
Framework modeled on `DailyQuest.cs`.
1. `$Quest[id, step, TYPE/TARGET/COUNT/REWARD/NEXT]` + per-character `QuestProg_<id>` funkvar.
2. Step types reuse hooks: kill (`Daily::OnKill` pattern in `onKilled`), collect (belt check),
   talk (botType branch comchat.cs:10676), reach (proximity scan on `RecursiveWorld`), deliver.
3. Quest-giver botType + `#quest` tracker; rewards via `GiveThisStuff` (rpgfunk.cs:6282), exp,
   gold, items, **titles** (feature 6).
4. Start from `Mission::init` (gameevents.cs:1); progress persists via `SaveCharacter`.

### 6. Titles & achievements — `medium`
Vanilla-safe display channels: scoreboard column via `Client::setScore` (rpgstats.cs:1046 —
free-form tab columns) + a prefix on the server-composed chat line (comchat.cs:3019). **No
`Client::setName` exists** — the over-head nameplate can't carry a title.
1. `$Achieve[id, NAME/DESC/COND/TITLE/REWARD]` registry; track via `onKilled`,
   `refreshClientScore` (:999), `DoRemort` (:1056), quest/boss completions.
2. Per-character progress funkvars; unlock ⇒ `~`-tagged announce + title/reward grant.
3. `#title set` equips a title (funkvar) → injected into scoreboard column + chat prefix.
- **Build early:** quests/bosses pay out titles; shared reward sink.

### 7. Companions — `small-medium` (≈80% already built)
The "botmaker" pet NPC already implements it (comchat.cs:11672–11823): spawn via `AI::helper`,
owner↔pet link (`petowner`/`PersonalPetList`/`$PetList`), follow via `botAttackMode=2` →
`AI::newDirectiveFollow` (ai.cs:9272), death/disconnect cleanup (ai.cs:8144, connectivity.cs:215),
per-owner cap `$maxPetsPerPlayer`.
1. Drop the 1-hr `Pet::TurnEvil` timer (comchat.cs:11814) for owned companions.
2. **Persist** companion (type/name/level) to owner funkvar; re-summon on spawn/relog.
3. Buy tiers from a beastmaster NPC; `#companion summon/dismiss`; add guard behavior.
- **Hard cap:** each active companion = 1 of the 127 id-pool slots. Cap 1/player + global budget.

### 8. Roaming merchants — `small-medium`
Merchant botType + shop already exist (`SetupShop` shopping.cs:1 via `$BotInfo[bot,SHOP]`).
1. Spawn a merchant bot; token-scheduled route loop advancing a position array via
   `AI::newDirectiveWaypoint` (`WaypointToWorld` ai.cs:7721); dwell at stops.
2. Register in a caravan table started from `Mission::init` so they respawn on reload
   (they're bots, not deployables).
- **Constraints:** 1 id-pool slot each; exempt from `DespawnZoneBots` (connectivity.cs:187)
  since they cross zones. Engine-directive pathing only (no script A*).

---

## Suggested build order
1. **Fix `DeployForceField`/`DeployTree`** (real latent bug; ships alone; de-risks Estates).
2. **Titles & achievements framework** (reward sink the rest feed).
3. **Weapon procs + backstab** (self-contained combat; validates the `onDamage` hook).
4. **Companions + roaming merchants** (productize existing bot systems; set the id-budget policy).
5. **Enchanting**, then **quest lines**, then the full **Estates** system.
