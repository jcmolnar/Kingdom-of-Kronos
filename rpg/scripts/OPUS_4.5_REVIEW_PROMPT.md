# Opus 4.5 Review Prompt: Zone Mismatch Analysis

## Context
We are analyzing a "Zone Mismatch" log entry from `DespawnZoneBots` for Bot 2060 (`Incarnate6`).
The logs show:
`[DESPAWN DEBUG] Zone Mismatch for 2060: botZone=0, originZone=8617, targetZone=8611. Skipping.`

## Facts
1.  **Spawn Storage**: `SpawnAIGetClientId` explicitly stored `zone` and `SpawnOriginZoneID` as `8617` at spawn time.
2.  **Retrieval**: `DespawnZoneBots` retrieved `botZone=0` (from `fetchData("zone")`) and `originZone=8617` (from `fetchData("SpawnOriginZoneID")`).
3.  **Logic**: `DespawnZoneBots` compares the bot's zone to the target zone (`8611`).
    *   Primary Check: `botZone (0) == targetZone (8611)` -> False.
    *   Fallback Check: `originZone (8617) == targetZone (8611)` -> False.
    *   Result: "Skipping" (Correct behavior, as the bot belongs to 8617, not 8611).

## Analysis
The "Zone Mismatch" log is actually confirming **correct behavior**. The despawn logic correctly identified that the bot does *not* belong to the target zone (8611) and skipped it.

The confusion arises from `botZone=0`.
*   We suspect `fetchData("zone")` corresponds to the **dynamic physics zone** updated by `Zone::Update`.
*   If `Zone::Update` determined the bot is physically in Zone 0 (The Void) or hasn't updated yet, it would overwrite the logical zone (8617) text field with 0.
*   The fallback to `SpawnOriginZoneID` (which we verified is set in `SpawnAI` and retrieved correctly as 8617) saved the day by preserving the *logical* spawn association.

## Question for Opus
Does this analysis hold check out? specifically:
1.  Is `botZone=0` acceptable here given that `SpawnOriginZoneID` acted as the correct fallback?
2.  Does `botZone=0` indicate a potential issue with `Zone::Update` clobbering the zone data too aggressively, or is this expected behavior for a bot that might physically be briefly "out of bounds" or just spawned?
3.  Should we be concerned that the dynamic `zone` field is being used for logic that requires the static `SpawnOriginZoneID`? (It seems our Fallback implementation handles this, but is it efficient?)

Please validate if the fallback mechanism is robust enough to ignore `botZone` completely if it differs from `SpawnOriginZoneID`.

## Additional Questions for Review
1.  **SpawnPoint Logic**: We are currently using manual counters (`$SpawnBotCount[...]` aka `SpawnPoint`) which sometimes drift and require our `ReconcileSpawnCounters` function (which fixed 4 discrepancies in the logs). Is there a more robust, **engine-native** method to track the number of bots per spawn point? For example, using a `SimSet` for each spawn point and using `SimSet::getCount()`?
2.  **Server Crash & Debug Spam**: The server crashed shortly after the logs shown. We noticed `AI::Periodic` seems to be running for **dead bots** (`Ignoring corpse object Corpse219...`).
    *   Could this loop (dead bots continuing to execute periodic logic) be causing a resource leak or log flood that crashes the server?
    *   The log shows frequent `ENTRY` messages for `AI::Periodic` (multiple per second). Is the frequency too high?
    *   Should `AI::Periodic` explicitly remove itself (`cancel(schedule)`) if the bot is dead?

