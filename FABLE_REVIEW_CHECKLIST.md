# Fable review checklist — recent work (Jul 2–4, 2026)

Two separate mods were worked on. **Everything is script-only, nothing built/deployed/live-tested** except where noted. Please review for logic bugs, regressions, and anything that looks wrong; flag don't fix unless asked.

---

## A. Red Moon RPG (RMRPG) — full load-order bug sweep  ← BIGGEST piece

- **Repo:** `C:\Dynamix\Tribes\RMRPG\scripts` (git, branch `main`, baseline `9e5adae`)
- **28 commits** since baseline. **Exhaustive per-fix log: `RMRPG_FIXES.md` in that folder — read it first.**
- **Deploy caveat:** loose scripts override `scripts.vol` ONLY if the vol is made unreachable. Not yet tested in-game.
- Separate mod from Kronos — different (rougher) conventions, its own repo/history.

**What was done:** every server-exec'd file (Server.cs load order) read end-to-end; dominant bug classes were undefined vars, `=` vs `==`, and duplicate/misattached functions.

**HIGHEST-PRIORITY to scrutinize (behavior changes + untested logic):**
- `68c5e01` **Chocobo jump-to-dismount** — NEW `Armor::jump` (guarded, falls through to `Player::jump` for non-chocobos). ⚠️ UNTESTED and rests on an assumption that the jump callback fires with `%this` = the controlled chocobo. Verify it can't break normal-player jumping.
- `711dbbd` / `bae013c` **flag-decision behavior changes:** Rogues now get the better `#steal` formula; status/cage durations `+=`→`=` (respect 10-min cap); **smith now consumes `count*%multiplier` → 99-arrow batches cost 99× materials (retune pricing)**; Drag_Sword/Punisher `onFire` model-name fix; admin cmd target/guard fixes.
- `b2871ca` Chocobo trade-flow duplicate-handler fix (`processMenuTradeChocobo`).
- `1eba76c` CurePotionStuff rewrite (was fully dead); `ebd049f` Mute-added-to-Blind-list fix.

**Rest of the fix commits (mostly clear undefined-var / typo fixes):**
`19f668e` `094209a` `1c4ff0a` `314d789` `c96df7a` `9f3d728` `ad9e702` `42aa83b` `b37f093` `1c75339` `10b9123` `823f2c3` `b0982e6` `40f7039` `fbbdaf5` `82b8092` `893ec34` (quarantine dead scripts) `f6b8a97` `9a397b1` `4328c01`

**Deliberately LEFT (documented in RMRPG_FIXES.md):** Carling PK-logging (broken, still cages on every lowbie PK); DeusKeys `Keys()` (dead code); assassin bounty-timeout key order; two cosmetics. Confirm these are OK to leave.

---

## B. Kingdom of Kronos (KoK) — branch `testing`

- **Repo:** this folder (current checkout = `testing`).

**This session (Jul 4):**
- `83e28a0` **Beautify player-viewed coins/exp** — floors fractional sources + `Number::Beautify` display (commas). Check no double-flooring / display-vs-stored drift.
- `67f2dd9` **Comment audit** — fixed outdated/incorrect script comments. Tracker: `rpg/scripts/COMMENT_AUDIT.md`. Comments only; verify none changed behavior.

**Daily quest system (Jul 2):**
- `e56bd97` rotating daily quests (Fetch/Cull) + `kronos_datetime` plugin; `b9840ef` Elite TTK-boss daily.
- ⚠️ **KNOWN latent bug (not introduced by us, but flag):** `DualWielding.cs` calls `GetRemort()` (lines 757, 783) which is **defined nowhere** → its remort checks are broken. Left out of scope.
- Elite theme not yet live-tested (herald spawn, `#daily summon`, TTK scaling).

**V0.9 review sweep (Jul 2, commits `7229460..7e1856b`) — earlier sweep, already has its own live-test checklist:**
`7e1856b` economy/Server (smith dupe window, sell token, cost loop) · `cada62f` admin-gate debug cmds · `7e36174` Spawn/playerspawn (visibility loop, SpawnLoop watchdog) · `ff6b72c` Belt.cs · `f99108f` shout dist back to 5 · `3b239a6` rpgstats+zone · `f431ac5` connectivity+playerdamage · `7229460` ai/remortseal/DualWielding/Ascension/rpgfunk. Lower priority for re-review (already swept) but fair game.

---

## Not code — FYI only (no review needed)
- Memory hygiene: synced stale MEMORY.md index hooks, flagged 2 superseded DTS entries, added an RMRPG memory. (`~/.claude/.../memory/`)

## FABLE REVIEW VERDICT (Jul 4, 2026) — review completed
All 28 RMRPG commits + KoK beautify/comment-audit/daily-quest reviewed against live code.
**Verified correct:** all undefined-var/typo/dup-function fixes; AccessoryData model names
(MakeItem `%fixshape = shape@delay` minus dot, Eval-vs-static load order safe in both cases);
Spells.cs %w2/returnFlag changes match the Spells2.cs:556 dispatcher exactly; trade-menu
handler wired to buildMenu "TradeChocobo"; Status/carling `= Cap` fixes; comment audit is
comments-only; beautify is display-only (no Beautify value feeds storeData/logic), floors
placed after all multipliers; daily-quest hooks clean, PluginLoader change is only the
datetime flag (do-not-commit constraint NOT violated); SummonElite correctly allows
resummon after a creditless elite death.

**FINDINGS (flag, not fixed):**
1. ⚠️ **Chocobo jump-dismount (68c5e01) — likely never fires, and has a wrong-owner edge.**
   Mount transfers CONTROL to the chocobo; the engine's jump script callback only fires for
   a control Player that is itself mounted to something — the chocobo isn't, so jump while
   riding probably just engine-jumps the bird (Armor::jump never called). If the callback
   instead fires on the RIDER (mounted player, className also "Armor"), %this = rider →
   falls through to stock Player::jump = raw unmount WITHOUT control restore/weapon/flags →
   broken half-dismount. Also `Chocobo::Delete(%cl)` keys off the RIDER — riding someone
   else's bird deletes the rider's OWN chocobo record and leaves the ridden one "isUsed"
   forever; guard with `if($ChocoboSpawn[%cl] == %this)`. And delete-on-dismount is a design
   change (menu-Return semantics applied to every hop-off). LIVE-TEST FIRST, expect rework.
2. **Smith (711dbbd): the "99× material cost" worry is MOOT — but two pre-existing bugs.**
   %tempsmith is "item count item count…" (tmpSmithList), so the batch check
   `getword(%tempsmith,1)=="Shape:_Arrow"` compares a COUNT and can never match (should be
   word 0). Net: %multiplier is always overwritten to 1 inside CompleteSmith → (a) arrow
   99-batching is dead, (b) the player's requested %amt is discarded — they pay cost×N coins
   and receive 1 item. Our material fix itself is safe (count×1).
3. **Beautify commit carries unadvertised scope:** remoteKShopTip (new item-tooltip remote
   handler in KronosHUD_Server.cs) is in 83e28a0 but not its message. HUD-gated + read-only
   (vanilla-safe), but it's an untested feature inside a "formatting" commit.
4. Minor: Number::Beautify depends on MathPlugin — if that DLL is missing, every converted
   message renders a blank where the number was; consider a boot-time sanity echo. RMRPG
   #setpl still lacks a `%id != -1` guard (bad name writes $PL[-1], pre-existing);
   getRandomName retry recursion has no depth bound (practically fine).

## Suggested review order
1. RMRPG flag-decision commits + the untested Chocobo dismount (`711dbbd`, `bae013c`, `68c5e01`) — highest regression risk.
2. RMRPG CurePotionStuff / Mute / trade-handler rewrites (`1eba76c`, `ebd049f`, `b2871ca`).
3. KoK beautify (`83e28a0`) — verify floor/display correctness.
4. Skim the rest against RMRPG_FIXES.md / COMMENT_AUDIT.md.
