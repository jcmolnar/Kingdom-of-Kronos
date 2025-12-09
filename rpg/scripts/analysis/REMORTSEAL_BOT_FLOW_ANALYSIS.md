# RemortSeal Bot Spawn and Deletion Flow Analysis

## Overview
This document traces the complete flow of how enemy bots are spawned and deleted in the remortseal system.

---

## 1. BOT SPAWNING FLOW

### 1.1 Entry Point: `SealBattle::Loop()` or `SealBattle::SpawnRound2()` / `SealBattle::SpawnRound3()`

**Location:** `remortseal.cs` lines 305-342 (Round 1), 555-593 (Round 2), 595-633 (Round 3)

**Process:**
1. Determines which spawnIndex to use based on round:
   - Round 1: `%bot = 66` (Fighter), `%bot2 = 67` (Mage), `%bot3 = 68` (Guardian)
   - Round 2: `%bot = 69` (Fighter), `%bot2 = 70` (Mage), `%bot3 = 71` (Guardian)
   - Round 3: `%bot = 72` (Fighter), `%bot2 = 73` (Mage), `%bot3 = 74` (Guardian)

2. Validates spawn indexes exist:
   ```torquescript
   if($spawnIndex[%bot] == "" || $spawnIndex[%bot2] == "" || $spawnIndex[%bot3] == "")
       return;
   ```

3. Calls `AI::helper()` for each bot:
   ```torquescript
   AI::helper($spawnIndex[%bot], "SealFighter1", "TempSpawn -3608 -2365 354 1", default);
   AI::helper($spawnIndex[%bot2], "SealMage1", "TempSpawn -3608 -2365 354 1", default);
   AI::helper($spawnIndex[%bot3], "SealGuardian1", "TempSpawn -3608 -2365 354 1", default);
   ```

4. Schedules `SealBattle::SetupBot()` after 0.7 seconds:
   ```torquescript
   schedule("SealBattle::SetupBot(\"SealFighter1\", \"" @ %pos @ "\");", 0.7);
   ```

**Key Points:**
- Uses `TempSpawn` command issuer type (not `SpawnPoint`)
- Bot names: `SealFighter1`, `SealMage1`, `SealGuardian1` (Round 1), `SealFighter2/3`, etc. (Rounds 2/3)
- Spawn position: `-3608 -2365 354` (Colloseum spawn point)

---

### 1.2 `AI::helper()` Function

**Location:** `Ai.cs` lines 1215-1275

**Process:**
1. Parses command issuer:
   - For `TempSpawn`: Extracts position from command string (`-3608 -2365 354`)

2. Gets AI number:
   ```torquescript
   %n = getAInumber();
   %newName = %aiName @ %n;  // e.g., "SealFighter1" @ "1" = "SealFighter1"
   ```

3. **CRITICAL:** Does NOT increment spawn point counter (only does for `SpawnPoint` type):
   ```torquescript
   if(GetWord(%commandIssuer, 0) == "SpawnPoint")
   {
       // Only increments counter for SpawnPoint type
       $numAIperSpawnPoint[%spawnpoint]++;
   }
   ```
   - Seal battle bots use `TempSpawn`, so **NO spawn point counter is incremented**

4. Calls `SpawnAI()`:
   ```torquescript
   %spawnResult = SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout);
   ```

---

### 1.3 `SpawnAI()` Function

**Location:** `Ai.cs` lines 1288-1437

**Process:**
1. Checks if bot already exists:
   ```torquescript
   %existingId = AI::getClientIdFromName(%newName);
   if(%existingId != -1 && %existingId != "")
       return %newName;  // Already exists
   ```

2. Checks if spawn is already scheduled:
   ```torquescript
   if($SpawnAIScheduled[%newName] == "true")
       return %newName;  // Already scheduled
   ```

3. For first call (not internal retry):
   - Sets scheduled flag: `$SpawnAIScheduled[%newName] = "true"`
   - Schedules `SpawnAIInternal()` with 2.0s delay:
     ```torquescript
     schedule("SpawnAIInternal(...)", 2.0);
     ```

4. For internal call (after 2.0s delay):
   - Calls `createAI()` immediately:
     ```torquescript
     %result = createAI(%newName, %armor, %spawnPos, %rotation, %displayName);
     ```

5. Schedules client ID lookup:
   ```torquescript
   schedule("SpawnAIGetClientId(...)", 0.5);
   ```

---

### 1.4 `createAI()` Function

**Location:** `Ai.cs` lines 1440-1470

**Process:**
1. Calls `AI::spawn()`:
   ```torquescript
   %result = AI::spawn(%aiName, %armor, %spawnPos, %rotation, %displayName);
   ```

2. If successful, schedules `createAIPostSpawn()`:
   ```torquescript
   schedule("createAIPostSpawn(\"" @ %aiName @ "\", \"" @ %armor @ "\", " @ %group @ ");", 0.1);
   ```

**Key Points:**
- `AI::spawn()` is the engine function that creates the Player object
- The Player object may not be immediately available in the client list

---

### 1.5 `SpawnAIGetClientId()` Function

**Location:** `Ai.cs` lines 1450-1700

**Process:**
1. Searches for client ID by display name (brute force search 2049-2200):
   ```torquescript
   for(%checkId = 2049; %checkId <= 2200; %checkId++)
   {
       %checkName = Client::getName(%checkId);
       if(%checkName == %displayName)
       {
           %aiId = %checkId;
           break;
       }
   }
   ```

2. Validates player object exists:
   ```torquescript
   %playerObj = Client::getOwnedObject(%aiId);
   if(%playerObj == -1 || %playerObj == "")
       // Invalid - retry or fail
   ```

3. Sets bot data:
   ```torquescript
   $BotInfoAiName[%aiId] = %newName;
   storeData(%aiId, "BotInfoAiName", %newName);
   ```

4. Sets team (for enemy bots):
   ```torquescript
   %botTeam = $BotInfo[%newName, botTeam];
   if(%botTeam == "" || %botTeam == 0)
       %botTeam = $TeamForRace[$BotInfo[%newName, RACE]];
   Player::setTeam(%aiId, %botTeam);
   ```

5. Schedules weapon mounting:
   ```torquescript
   schedule("AI::setWeapons(" @ %aiId @ ");", 0.15);
   ```

**Key Points:**
- Seal battle bots go through the same spawn flow as enemy bots
- They get `BotInfoAiName` set, but **NO `SpawnBotInfo`** (because they use `TempSpawn`, not `SpawnPoint`)
- They are treated as enemy bots (not town bots)

---

### 1.6 `SealBattle::SetupBot()` Function

**Location:** `remortseal.cs` lines 992-1385

**Process:**
1. Gets bot client ID:
   ```torquescript
   %aiId = NEWgetClientByName(%botName);
   if(%aiId == -1 || %aiId == "")
       return;  // Bot not found
   ```

2. Clears zone data:
   ```torquescript
   storeData(%aiId, "zone", "");
   storeData(%aiId, "tmpzone", "");
   ```

3. Sets seal battle bot flags:
   ```torquescript
   storeData(%aiId, "botAttackMode", 1);
   storeData(%aiId, "tmpbotdata", %pos);
   storeData(%aiId, "noDropLootbagFlag", True);
   storeData(%aiId, "AImoveChance", 1);
   storeData(%aiId, "SealBattleBot", true);  // CRITICAL FLAG
   ```

4. Sets race and armor:
   ```torquescript
   storeData(%aiId, "RACE", "Seals");
   Player::setArmor(%aiId, "SealsArmor");
   ```

5. Gives equipment:
   ```torquescript
   %equipString = $BotInfo[%botName, ITEMS];
   GiveThisStuff(%aiId, %equipString, False);
   ```

6. Scales bot stats based on seal value multiplier:
   ```torquescript
   %mult = SealBattle::GetEnemyStrengthMultiplier();
   storeData(%aiId, "SealBattleMultiplier", %mult);
   // Scales STR, DEX, INT, LVL, VIT, Endurance skill, Energy skill, HP, MANA, DEF, ATK, MDEF, DMG
   ```

7. Calls `RefreshAll()` multiple times to recalculate stats

**Key Points:**
- `SealBattleBot` flag is set to `"true"` - this identifies the bot as a seal battle bot
- `noDropLootbagFlag` is set to `True` - prevents lootbag drops
- Bot stats are scaled based on `TotalSealValue` (multiplier increases with seal value)

---

## 2. BOT DELETION FLOW

### 2.1 When Players Die: `SealBattle::Loop()` Manual Kill

**Location:** `remortseal.cs` lines 260-302

**Trigger:** `$SealFighterDied == true` (all players are dead or left Colloseum)

**Process:**
1. Gets client IDs for all seal battle bots:
   ```torquescript
   %g1 = NEWgetClientByName("SealGuardian1");
   %m1 = NEWgetClientByName("SealMage1");
   %f1 = NEWgetClientByName("SealFighter1");
   // ... Round 2 and 3 bots
   ```

2. Calls `Player::Kill()` on each bot:
   ```torquescript
   if(%g1 != -1) Player::Kill(%g1);
   if(%m1 != -1) Player::Kill(%m1);
   if(%f1 != -1) Player::Kill(%f1);
   ```

3. Calls `SealBattle::Conclude()` to end the battle

**Key Points:**
- This is a **manual kill** triggered when players die
- All bots are killed immediately via `Player::Kill()`
- No lootbags are dropped (bots have `noDropLootbagFlag = True`)

---

### 2.2 When Bots Are Killed Normally: `Player::onKilled()`

**Location:** `playerdamage.cs` lines 156-1800

**Trigger:** Bot dies from player damage (normal combat)

**Process:**
1. Gets bot info:
   ```torquescript
   %spawnBotInfo = fetchData(%clientId, "SpawnBotInfo");
   %botInfoAiName = fetchData(%clientId, "BotInfoAiName");
   ```

2. **CRITICAL:** Seal battle bots have `SpawnBotInfo == ""` (they use `TempSpawn`, not `SpawnPoint`)

3. Checks if this is an enemy bot:
   ```torquescript
   if(%spawnBotInfo != "" && %spawnBotInfo != "0" && %spawnBotInfo != -1)
   {
       // Enemy bot with SpawnBotInfo - decrement spawn point counter
       // ... (seal battle bots skip this)
   }
   ```

4. **Seal battle bots skip spawn point counter decrement** (they don't have `SpawnBotInfo`)

5. Marks client ID as recently freed:
   ```torquescript
   $ClientIdRecentlyFreed[%clientId] = getSimTime();
   ```

6. Schedules `AI::delete()`:
   ```torquescript
   schedule("AI::delete(\"" @ %escapedName @ "\");", 1.0);
   ```

7. Cleans up bot data:
   - Clears `storeData()` fields (QuestItems, KeyItems, etc.)
   - Clears `$aidirectiveTable` entries
   - Clears `$Belt::CachedList` entries
   - Removes from pet/bot groups

**Key Points:**
- Seal battle bots **do NOT have `SpawnBotInfo`**, so they skip spawn point counter logic
- They still go through normal cleanup (data clearing, `AI::delete()`)
- `noDropLootbagFlag = True` prevents lootbag drops

---

### 2.3 `AI::onDroneKilled()` Function (For Drones Only)

**Location:** `Ai.cs` lines 2700-3100

**Note:** Seal battle bots are **Player objects**, not Drones, so this function is **NOT called** for them.

**Process:**
- Only called for Drones (old bot system)
- Seal battle bots use the new Player object system

---

### 2.4 `AI::delete()` Function

**Location:** Engine function (not in scripts)

**Process:**
1. Deletes the AI name from the engine
2. Frees the AI number for reuse
3. The Player object is deleted by the engine after `Player::onKilled()` completes

**Key Points:**
- This is called 1.0 second after `Player::onKilled()` (scheduled delay)
- The Player object may already be deleted by the engine by this point

---

## 3. SPECIAL HANDLING FOR SEAL BATTLE BOTS

### 3.1 Damage Prevention

**Location:** `playerdamage.cs` lines 2353-2382

**Process:**
1. Prevents seal battle bots from damaging each other:
   ```torquescript
   if(fetchData(%shooterClient, "SealBattleBot") == "true" && 
      fetchData(%damagedClient, "SealBattleBot") == "true" && 
      %shooterClient != %damagedClient)
   {
       %value = 0;  // No damage
   }
   ```

2. Prevents players outside Colloseum from damaging bots inside:
   ```torquescript
   if($SealBattleActive == true && !isRPGAI(%shooterClient) && 
      fetchData(%damagedClient, "SealBattleBot") == "true")
   {
       if(%shooterZone != "Colloseum" && %damagedZone == "Colloseum")
           %value = 0;  // No damage
   }
   ```

---

### 3.2 Zone System Exclusion

**Location:** `zone.cs` lines 1027-1044

**Process:**
1. Zone system checks for `SealBattleBot` flag:
   ```torquescript
   %sealBattleBot = fetchData(%id, "SealBattleBot");
   if(%sealBattleBot == "true")
       continue;  // Skip seal battle bots
   ```

2. Also checks bot name pattern:
   ```torquescript
   if(String::findSubStr(%botName, "SealFighter") == 0 || 
      String::findSubStr(%botName, "SealMage") == 0 || 
      String::findSubStr(%botName, "SealGuardian") == 0)
       continue;  // Skip seal battle bots
   ```

**Key Points:**
- Seal battle bots are excluded from zone-based bot management
- They are managed entirely by `remortseal.cs`

---

### 3.3 No Lootbag Drops

**Location:** `remortseal.cs` lines 1020, 248-250

**Process:**
1. `noDropLootbagFlag` is set to `True` during setup:
   ```torquescript
   storeData(%aiId, "noDropLootbagFlag", True);
   ```

2. Also set in `SealBattle::StartBattle()`:
   ```torquescript
   storeData(Sealbot, "noDropLootbagFlag", True);
   storeData(Sealbot2, "noDropLootbagFlag", True);
   storeData(Sealbot3, "noDropLootbagFlag", True);
   ```

**Key Points:**
- Seal battle bots never drop lootbags
- This prevents item farming from seal battles

---

## 4. DATA SET DURING SPAWN

### 4.1 Set by `SpawnAI()` / `SpawnAIGetClientId()`:
- `$BotInfoAiName[%clientId]` = bot name (e.g., "SealFighter1")
- `storeData(%clientId, "BotInfoAiName", %botName)`
- `Player::setTeam(%clientId, %botTeam)` - sets bot team

### 4.2 Set by `SealBattle::SetupBot()`:
- `storeData(%clientId, "botAttackMode", 1)`
- `storeData(%clientId, "tmpbotdata", %pos)`
- `storeData(%clientId, "noDropLootbagFlag", True)` - **CRITICAL**
- `storeData(%clientId, "AImoveChance", 1)`
- `storeData(%clientId, "SealBattleBot", true)` - **CRITICAL FLAG**
- `storeData(%clientId, "RACE", "Seals")`
- `storeData(%clientId, "SealBattleMultiplier", %mult)`
- `storeData(%clientId, "zone", "")` - cleared
- `storeData(%clientId, "tmpzone", "")` - cleared
- Equipment via `GiveThisStuff()`
- Scaled stats (STR, DEX, INT, LVL, VIT, HP, MANA, DEF, ATK, MDEF, DMG)

### 4.3 **NOT SET** (Important):
- `storeData(%clientId, "SpawnBotInfo", ...)` - **NOT SET** (uses `TempSpawn`, not `SpawnPoint`)
- `storeData(%clientId, "SpawnTime", ...)` - **NOT SET**
- No spawn point counter increment (uses `TempSpawn`)

---

## 5. DATA CLEARED DURING DELETION

### 5.1 Cleared by `Player::onKilled()`:
- `$ClientIdRecentlyFreed[%clientId]` = `getSimTime()` - marks as recently freed
- `storeData(%clientId, "RemortStep", "")`
- `storeData(%clientId, "QuestItems", "")`
- `storeData(%clientId, "KeyItems", "")`
- `storeData(%clientId, "Consumables", "")`
- `storeData(%clientId, "Armor", "")`
- `storeData(%clientId, "Accessories", "")`
- `storeData(%clientId, "Other", "")`
- `storeData(%clientId, "noExperienceFlag", "")`
- `storeData(%clientId, "noDropLootbagFlag", "")`
- `storeData(%clientId, "dumbAIflag", "")`
- `storeData(%clientId, "frozen", "")`
- `storeData(%clientId, "noBotSniff", "")`
- `storeData(%clientId, "SpellCastStep", "")`
- `storeData(%clientId, "LCKconsequence", "")`
- `storeData(%clientId, "AIattackMarker", "")`
- `storeData(%clientId, "ExpDistributed", "")`
- `$aidirectiveTable[%clientId, 0-99]` = "" (all directives cleared)
- `$Belt::CachedList[%clientId, ...]` = "" (all belt cached lists cleared)
- `storeData(%clientId, "AITarget", "")`
- `storeData(%clientId, "AILastDestination", "")`
- `storeData(%clientId, "AILastLoggedDist", "")`
- `storeData(%clientId, "AIMovementLoopRunning", "")`
- `storeData(%clientId, "botTeam", "")`
- Pet/bot group cleanup

### 5.2 Scheduled Cleanup:
- `AI::delete(%botName)` - scheduled 1.0 second after death
- This deletes the AI name from the engine and frees the AI number

### 5.3 **NOW CLEARED** (Fixed):
- `$BotInfoAiName[%clientId]` = "" - **NOW CLEARED** for bots without `SpawnBotInfo`
- `storeData(%clientId, "BotInfoAiName", "")` - **NOW CLEARED** for bots without `SpawnBotInfo`
- `$EnemyBotData[%clientId, "BotInfoAiName"]` = "" - **NOW CLEARED**
- `$ClientData[%clientId, "BotInfoAiName"]` = "" - **NOW CLEARED`

**FIX APPLIED:** Added cleanup in `Player::onKilled()` for bots without `SpawnBotInfo` (seal battle bots) since they don't go through `AI::onDroneKilled()`.

---

## 6. POTENTIAL ISSUES AND RECOMMENDATIONS

### 6.1 Issue: `BotInfoAiName` Not Cleared - **FIXED**

**Problem:**
- Seal battle bots are Player objects, not Drones
- `AI::onDroneKilled()` is NOT called for them
- `BotInfoAiName` was not being cleared, leaving stale data

**Fix Applied:**
- Added cleanup of `BotInfoAiName` in `Player::onKilled()` for bots without `SpawnBotInfo`:
  ```torquescript
  if(%spawnBotInfo == "" && %botInfoAiName != "")
  {
      // This is a seal battle bot or other non-spawn-point bot
      $BotInfoAiName[%clientId] = "";
      storeData(%clientId, "BotInfoAiName", "");
      $EnemyBotData[%clientId, "BotInfoAiName"] = "";
      $ClientData[%clientId, "BotInfoAiName"] = "";
  }
  ```
- **Location:** `playerdamage.cs` lines ~1631-1642

### 6.2 Issue: No Spawn Point Counter

**Current Behavior:**
- Seal battle bots use `TempSpawn`, so no spawn point counter is incremented
- This is **correct** - they're not managed by spawn points

**No Action Needed:** This is intentional and correct.

### 6.3 Issue: Manual Kill vs Normal Kill

**Current Behavior:**
- When players die: `Player::Kill()` is called manually
- When bots die normally: `Player::onKilled()` is called automatically
- Both paths should clean up the same way

**Verification Needed:**
- Ensure `Player::Kill()` triggers `Player::onKilled()` properly
- Verify cleanup is consistent in both cases

---

## 7. SUMMARY FLOW DIAGRAM

```
SPAWN FLOW:
SealBattle::Loop() / SpawnRound2() / SpawnRound3()
  └─> AI::helper(spawnIndex, botName, "TempSpawn ...", default)
      └─> SpawnAI()
          └─> createAI()
              └─> AI::spawn() [Engine]
                  └─> SpawnAIGetClientId()
                      └─> Sets BotInfoAiName, team, etc.
                          └─> schedule(SealBattle::SetupBot(), 0.7s)
                              └─> Sets SealBattleBot flag, scales stats, gives equipment

DELETION FLOW (Normal Kill):
Bot dies from player damage
  └─> Player::onKilled()
      └─> Marks client ID as recently freed
      └─> Clears bot data (storeData, arrays, etc.)
      └─> schedule(AI::delete(), 1.0s)
          └─> AI::delete() [Engine] - deletes AI name

DELETION FLOW (Manual Kill):
All players die
  └─> SealBattle::Loop() detects $SealFighterDied == true
      └─> Player::Kill() for each bot
          └─> Player::onKilled() [same as normal kill]
              └─> (same cleanup as above)
      └─> SealBattle::Conclude()
```

---

## 8. KEY DIFFERENCES FROM ENEMY BOTS

| Feature | Enemy Bots | Seal Battle Bots |
|---------|------------|------------------|
| Spawn Type | `SpawnPoint` | `TempSpawn` |
| SpawnBotInfo | Set | **NOT SET** |
| Spawn Point Counter | Incremented | **NOT Incremented** |
| BotInfoAiName | Set | Set |
| SealBattleBot Flag | Not set | **Set to "true"** |
| noDropLootbagFlag | May be set | **Always True** |
| Zone Management | Managed by zone system | **Excluded from zone system** |
| AI::onDroneKilled() | Called (if Drone) | **NOT Called (Player object)** |
| Cleanup | Full cleanup | **May miss BotInfoAiName cleanup** |

---

## END OF ANALYSIS

