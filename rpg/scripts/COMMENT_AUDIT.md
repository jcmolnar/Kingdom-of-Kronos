# Script Comment Audit — outdated / incorrect comments

Purpose: find comments in `rpg/scripts/*.cs` that **contradict the current code** and would
mislead a future reader (human or AI). Started 2026-07-03.

Method: keyword sweep (`no longer / removed / deprecated / default / disabled / line N / used to`,
etc.) then verify each hit against the actual code it describes. Only comments proven to be
**wrong about current behavior** are listed. Accurate historical notes and accurate "disabled"
tombstones are intentionally NOT listed.

Status key: `[ ]` found, not yet fixed · `[x]` fixed · `[keep]` verified accurate (false alarm)

### RUNNING TOTALS (as of 2026-07-03)
- **16 stale comments fixed** (all comment-only, zero behavior change):
  itemevents MountWeaponOnTalk; ai telemetry line-ref; remortseal deprecated-header; 9 rotted
  line-number citations; ai 15s→30s + 5min→1min timing; skills RedFlag copy-paste; rpgfunk lootbag
  header (2min/25u → 30s/10u).
- **1 flagged for user decision** (not edited): economy.cs:68 buy-haggling 50%-vs-20%.
- **~28 files deep-read or thoroughly verified CLEAN.** Pattern: maintained files pristine; yield is
  isolated copy-paste/tuning drift in older or large files.
- Remaining long tail: full line-by-line prose of the 3 giant files (comchat/rpgfunk/Belt) beyond
  grep coverage, data files (armor/weapon tables), and UI files (GUI/Options/Admin/menu).

---

## CONFIRMED STALE (misleading)

### 1. itemevents.cs:67-80 — MountWeaponOnTalk default is wrong
- [x] FIXED 2026-07-03 (comment-only). Comment documented `MountWeaponOnTalk` default = **"true"** and says weapons mount when a
  player talks to a bot.
- Reality: runtime defaults it to **"false"** (`ai.cs:12438-12439`), and both `ai.cs:12434` and
  `comchat.cs:10620` state weapon mounting on talk was **removed** — weapons only mount on collision.
- Risk: high — this is the canonical flag documentation block; anyone configuring town bots would
  trust the wrong default and expect talk-mounting that no longer exists.
- Fix: change the two "(default: "true")" / "mounted when player talks" lines (itemevents.cs:68 and
  the `Default behavior` block ~line 80) to "false" + "weapons only mount on collision".

### 2. ai.cs:6510 & 6521 — stale line-number reference
- [x] FIXED 2026-07-03 (comment-only; replaced "line ~5971" with a name reference). Comment: "Telemetry will be recorded when max retries is reached (line ~5971)".
- Reality: the max-retries telemetry call is now at `ai.cs:6824`
  (`Telemetry_RecordSpawnFailed("other"); // Max retries reached`). "~5971" is off by ~850 lines.
- Risk: medium — sends a reader to the wrong place; line refs in comments rot on every edit.
- Fix: change "(line ~5971)" → "(see Telemetry_RecordSpawnFailed(\"other\") max-retries call below)"
  or update to ~6824. Prefer a non-numeric reference so it can't rot again.

### 3. remortseal.cs:302-303 — "no longer used" is false for GetRoundMultiplier
- [x] FIXED 2026-07-03 (comment-only; rescoped DEPRECATED header to only GetBaseStrengthMultiplier, labeled GetRoundMultiplier ACTIVE). Header "DEPRECATED: Old multiplier functions kept for backwards compatibility / These are no
  longer used by SetupBot" sits above TWO functions: `GetBaseStrengthMultiplier` (truly unused,
  returns 1.0) and `GetRoundMultiplier`.
- Reality: `GetRoundMultiplier` IS used by SetupBot — called at `remortseal.cs:2004`
  (`%mult = SealBattle::GetRoundMultiplier(%round);`) for round HP scaling. It is live code.
- Risk: high — "deprecated, no longer used" invites deletion, which would break seal round scaling.
- Fix: move/rescope the DEPRECATED header so it only covers `GetBaseStrengthMultiplier`; label
  `GetRoundMultiplier` as active (round HP lookup used by SetupBot).

---

## BATCH 2 — rotted in-comment line-number citations (all FIXED 2026-07-03, comment-only)
Category finding: the codebase had many comments citing absolute line numbers ("line 5619",
"lines 795-800", etc.). As files grew, nearly all drifted to point at unrelated code. Fixed by
replacing the numbers with durable name/anchor references so they can't rot again.

- [x] ai.cs:4775 — "IsEnemyBot/IsTownBot defined ... around lines 2495, 2546" → real defs are
  `isTownBot()` @2625 and `isEnemyBot()` @2676 (lowercase; order was also reversed). De-numbered.
  (Side note: these helpers are real and heavily used across damage/team/zone logic — NOT a missing
  function, despite a case-sensitive grep initially missing them.)
- [x] ai.cs:4973 — "balances increment at line 4392" → 4392 is unrelated event-command code. De-numbered.
- [x] rpgfunk.cs:4219 — "%cw extracted ... at line 3819" → 3819 is blank/GiveThisStuff item parsing. De-numbered.
- [x] DualWielding.cs:867 — "unequipped above (lines 795-800)" → 795-800 is the EquipOffHand header. De-numbered.
- [x] itemevents.cs:232 & :265 — "same pattern as rpgfunk.cs line 1850/1852" → those lines are
  storeData GROUP/CLASS character-load code, not an $AccessoryVar pattern. De-numbered.
- [x] playerspawn.cs:214 — "previously called on line 164" → de-numbered (historical bugfix note).
- [x] playerspawn.cs:384 — "GiveThisStuff() (line 5619 in rpgfunk.cs)" → GiveThisStuff is @6181; 5619
  is inside RefreshAll's own body. De-numbered to just "in rpgfunk.cs".
- [x] DualWielding.cs:67 — "DoRemort() (line ~1015)" → actual @1048. Updated the number.
- [keep] DualWielding.cs:63 — "Weapon::onUse() (line ~149)" → actually @149. Accurate, left as-is.

---

## BATCH 3 — ai.cs timing/interval claims (FIXED 2026-07-03, comment-only)
Swept every "runs every N seconds/minutes" comment in ai.cs against the actual schedule() interval.

- [x] ai.cs:10128 — "This runs every 15 seconds (increased frequency...)" for PeriodicBotTeamCheck →
  actually reschedules at 30s (ai.cs:12332), and ai.cs:12329 documents the 15s→30s change. Fixed to
  "every 30 seconds", dropped the misleading "increased frequency".
- [x] ai.cs:210 — "Runs every 5 minutes" for PeriodicAINumberReconciliation → actually 60s (schedule
  @218 and @274 both 60, inline "// 1 minute", echo "every 60 seconds"). Fixed to "every 60 seconds".
- [keep] ai.cs:138/146 watchdog "every 5 seconds" — accurate (reschedules @164 at 5; the 10 @203 is
  just the initial kickoff delay).
- [keep] ai.cs:279 ghost-bot scan "every 30 seconds" — accurate (PeriodicGhostBotScan @288/305/409 = 30).
- [keep] ai.cs:501/705 graveyard cleanup "every 30 seconds" — accurate (@506/707 = 30).
- [keep] ai.cs:9761 ghost-clientId cleanup "every 30 seconds" — accurate (@9757 = 30; @9762 kickoff 60).
- [keep] ai.cs:344 "flagged for more than 25 seconds" — accurate (`%elapsed > 25`).

### remortseal.cs numeric/timing sweep — ALL ACCURATE (no fixes)
Checked cooldown 180s=3min (@16), bot spawn delay 15 (@20, "increased from 12" matches), round-1
check 10s / round-2+ 5s (@1000-1004), Guardian spawn 1.0s / Mage 0.5s (@1132/1135), mage spell
default 0.06=6% (@2034), $SealBotHealingReduction default 0.1=10% (@115/2195). remortseal.cs's
numeric comments are trustworthy.

---

## BATCH 4 — playerdamage.cs DEEP PROSE READ (verdict: CLEAN, no fixes)
Read the head (client-id resolution, onKilled, damage-message helpers) line-by-line and verified
every formula/proc/value comment in the combat-math sections against the code:
- Stances: Glass Cannon 2.5x (@4129), MageBane/BladeBane null+double (@4135-4148), Defensive /2 (@4122). Accurate.
- Ascension: Berserker's Rage +30% at <=25% HP (@4160-4161), Iron Skin 0.85 (@4166), Dodge Mastery <10% (@4174). Accurate.
- Robe procs: Retribution 25% reflect 10% (@4272-4281, $ArmorEffectChance[JudgementRobe]=25),
  Static Discharge 25% zap 500 (@4295-4301, $ArmorEffectChance[StormRobe]=25). Accurate.
- Bash: mult cap +1.5x (@3211), shove sqrt(skill)/2 cap 10 (@3222-3224); Vehicle Combat +1/100 cap
  +10 (@3540-3544). Accurate.
- "Legacy validation removed" / "check environment deaths FIRST" tombstones (@226/289/332) — accurate.
Conclusion: playerdamage.cs comments are trustworthy; it's a heavily-maintained file. The greppable
categories (batches 1-3) had already caught its only stale comments.

---

## BATCH 5 — skills.cs FULL DEEP READ (1 fix)
- [x] FIXED skills.cs:341-342 — the "Red Flag" class block (the hacking-punishment class, all 0.1
  multipliers) had a copy-pasted CLERIC description ("Clerics are good with Bludgeoning weapons but
  VERY good at healing spells...") sitting under its header. Removed it; the accurate "For hacking
  set a player to this..." note remains. (Not catchable by any keyword/line grep — pure prose drift.)
- [keep] Everything else verified accurate: skill-ID table + "no longer used" 7/17 labels; class
  multiplier summary (primary 2.0/secondary 1.5); restriction-number derivation formula (@374-387);
  AddSkillPoint caps (bashing/vehicle/crit/haggle 1000, speech 100); SkillCanUse L/R/A/G/C/H engine
  matches its comments incl. "check class/group/house first" (@922); UseSkill cap formula (@948);
  RefreshAll 2s throttle (@970-979).

---

## BATCH 6 — shopping.cs (clean) + economy.cs (1 FLAGGED, not edited)
- [keep] shopping.cs — FULL read, clean. Merchant-definitions note (@392-397: all shop lists now in
  KingdomKronos.mis TownBots SimGroup) matches current architecture; belt-category loop comment ok.
- [FLAG — needs your call] economy.cs:68 — in getBuyCost, `%maxHagglingPercent = 0.5; // 50% max from
  1000 haggling skill`. But the BUY discount formula (@75) is `round(skill/50)/100`, which at the 1000
  haggling cap yields only 20% (you'd need skill 2500 to reach 50%). getSellCost uses a DIFFERENT
  divisor (@105, `round(skill/14)/100`) which does reach the 50% cap (~skill 700). So the "50% max
  from 1000 haggling skill" comment is true for SELLING but wrong for BUYING.
  Two possibilities — your call which:
    (a) intended: buy haggling is deliberately weaker than sell haggling → fix the COMMENT to say the
        0.5 is the Cap ceiling, and note buy only reaches ~20% at the skill cap; OR
    (b) bug: the buy divisor should be /20 (to reach 50% at 1000 skill) → a CODE change, balance impact.
  Left untouched pending your decision (harmless today: the 0.5 only feeds the minCost floor, which
  never binds since real buy discounts stay <=20%).
- [keep] economy.cs rest — clean (BuySell, buyItem/sellItem, robe-talent gates, "TEMPORARILY DISABLED"
  steal block, cropped-vs-raw token BUGFIX note @502-505, bank "50 types" limit).

---

## BATCH 7 — remaining medium/large files (2 fixes so far, ongoing)
- [x] FIXED skills.cs:341-342 (RedFlag copy-paste — see Batch 5 above; grouped here too).
- [x] FIXED rpgfunk.cs:3331 — Lootbag Aggregation section header said "runs every 2 minutes and
  combines lootbags within 25 units", but $LootbagAggregateInterval=30 (30s, used @3542/3671) and
  $LootbagAggregateRadius=10 (used @3628). Both numbers stale (likely tuned down without updating the
  header). Rewrote header to reference the variables + current values (30s / 10 units).
- [keep] rpgfunk.cs:7796 objectives refresh "every 60 seconds" — accurate (reschedule @7838 = 60).
- [keep] TransmogSystem.cs, Banking.cs, Mine.cs, SigilForge.cs — no drift-marker comments; clean.
- [keep] rpgstats.cs — MaxHP formula (@296-302) matches documented MinHP+End*0.6+AddPoints(4)+
  R*End/8+LVL+bonus; MaxMANA/MaxWeight/Glass-Cannon 0.5 all match; MaxLevel cap 125+R*8 (@939). Clean.
- [keep] newstuff.cs weapon/robe effect descriptions — VERIFIED incl. the intricate Echo Fang comment
  (40% echo/2s, Momentum +10%/stack max +50%, 1.5s window) against playerdamage.cs:4399-4441. All
  robe procs (Judgement/Storm/Void) match too. Trustworthy.
- [keep] comchat.cs + Belt.cs inline timing/distance comments (1.5s AI re-enable @1817, >15s stuck
  reservation @7514, 3s deployable save, etc.) — spot-verified accurate.
- [keep] Belt.cs — header/architecture read: BankStorageConversion field mapping (@29: fields
  38/39/42/43/45/46) VERIFIED against rpgfunk.cs:1452; "skip GetNS during early load" note accurate;
  FixCorruptedList "CORRUPTION FIXES DISABLED - returns list unchanged" (@2157-2171) accurate; MUG
  section genuinely emptied (@2189). Well-maintained.

- [keep] DualWielding.cs — AllowForBots=false default (@120) matches @25; "OLD SKILL REQUIREMENT
  replaced by Ascension talent" (@122) accurate (DualWielding is an Ascension talent); BUGFIX
  tombstones accurate. (Known separate latent bug: undefined GetRemort() — code issue, not comment.)
- [keep] weapons.cs — Void weapon comment (3 random bombs Bomb21/25/28, every 50th → Bomb27, @528-529)
  matches code @531-545 exactly.

## PERIPHERAL CODE OBSERVATION (NOT a comment issue — for user, uncertain)
- `%`-modulo is used in only 2 places: weapons.cs:531 `(%attackCount % 50)` and playerdamage.cs:3440
  `(%hitCount % 2)`. Daily::Mod was written with floor() on the belief this TorqueScript lacks a
  modulus operator. If that belief is correct these two are latent bugs; but their use in core combat
  (playerdamage) suggests `%` modulo may actually work here (MathPlugin?). UNVERIFIED — worth a glance,
  outside the comment audit's scope. The comments themselves are accurate.

---

## VERIFIED ACCURATE (checked, NOT stale — do not touch)
- [keep] Server.cs:394 "ValidateSpawnPointCounters removed - replaced by ReconcileSpawnCounters()" —
  correct; function only survives in `stale/` backups, ReconcileSpawnCounters is live (ai.cs:2079).
- [keep] gameevents.cs:318 "ScheduleSave deprecated - SaveCharacter called directly" — correct; it's
  a real passthrough to SaveCharacter.
- [keep] ai.cs:12434 / comchat.cs:10620 MountWeaponOnTalk removed — correct (these are the source of
  truth that itemevents.cs contradicts).
- [keep] Belt/rpgfunk "CORRUPTION FIXES DISABLED - only logging" family — accurate current state.
- [keep] economy/comchat/rpgfunk "TEMPORARILY DISABLED - steal/mug/pickpocket" family — accurate.
- [keep] AutoSkill/rpgstats/skills "no longer used skills (stealing 7, mining 17)" — accurate.
- [keep] ai.cs "removed - no callers" tombstones (activelyFollow, moveToAttackMarker,
  Bot_MatchesEnemyPattern, AI::moveAhead, VerifyEnemyBotTeam) — verified: each name appears ONLY in
  its own tombstone comment, zero live callers. Accurate.

---

## FILES SWEPT
Keyword+numeric pass across all *.cs (batches 1-3). Also verified CLEAN in depth: backpack.cs
(no behavior-claim comments; possibly-legacy Gems/Keys/Scrolls system but no comment lies about it),
Ascension.cs (talent costs/effects/dynamic-robe-cost 25/10/5 all match code + playerdamage procs).
DEEP PROSE READ done on:
- playerdamage.cs (combat-math/proc sections) — CLEAN
- skills.cs (full) — 1 fix (RedFlag copy-paste)
- shopping.cs (full) — CLEAN
- economy.cs (full) — 1 FLAGGED (haggling buy 20% vs 50% comment)
- classes.cs (full) — CLEAN (team 0-12 cross-file claim verified vs EnemyArmors.cs)
- house.cs (full) — CLEAN
- Player.cs (full) — CLEAN (stock turret hack comment still accurate)
- remortseal.cs (numeric/timing + batch-1 fix) — CLEAN otherwise
- ai.cs (keyword + timing sweep) — 3 fixes; prose not yet fully read

## ALSO VERIFIED CLEAN (depth): Vehicle.cs, Station.cs, Turret.cs (stock files; comments accurate/
self-consistent — transmog-moved-to-onCollision, turret house-targeting, getHeatFactor hack xref).

## REMAINING for deep prose read
TransmogSystem.cs, SigilForge.cs, Banking.cs, Mine.cs, LasVegas.cs, rpgstats.cs, rpgarena.cs,
connectivity.cs, objectives.cs, zone.cs, newstuff.cs, weapons.cs (data), spells.cs (full),
comchat.cs (full), rpgfunk.cs (full), Belt.cs (full), Accessory.cs, DualWielding.cs, Admin.cs,
GUI.CS, Options.cs, and the armor DATA files (low comment risk). ai.cs full prose read still pending.
