# Bot Attributes (STR, DEX, INT, VIT) Analysis

## Where Do STR, DEX, INT, VIT Come From?

### For Regular Bots (SpawnPoint, TempSpawn, MarkerSpawn)

**STR, DEX, INT, VIT are NOT directly stored or used for regular bots.**

These attributes are **only used for Seal Battle bot scaling** in `SealBattle::SetupBot()`. For regular bots:

1. **MaxHP calculation** (rpgstats.cs line 183-189):
   - Uses: `$MinHP[RACE]`, `Endurance skill * 0.6`, `AddPoints(clientId, 4)`, `RemortStep`, `LVL`, `AddBonusStatePoints("MaxHP")`
   - **Does NOT use STR, DEX, INT, VIT directly**

2. **MaxMANA calculation** (rpgstats.cs line 208-215):
   - Uses: `8`, `Energy skill * (1/3)`, `RemortStep * 2`, `AddPoints(clientId, 5)`, `AddBonusStatePoints("MaxMANA")`
   - **Does NOT use STR, DEX, INT, VIT directly**

3. **AddPoints() function** (Accessory.cs line 303-367):
   - Checks equipment for `$SpecialVar` values
   - `AddPoints(clientId, 4)` checks equipment for special variables that affect HP
   - `AddPoints(clientId, 5)` checks equipment for special variables that affect MANA
   - **Does NOT use STR, DEX, INT, VIT**

### For Seal Battle Bots (TempSpawn in remortseal.cs)

**STR, DEX, INT, VIT are fetched and scaled** in `SealBattle::SetupBot()` (remortseal.cs lines 1187-1229):

1. **Fetched from storeData()**:
   ```torquescript
   %baseSTR = fetchData(%aiId, "STR");
   %baseDEX = fetchData(%aiId, "DEX");
   %baseINT = fetchData(%aiId, "INT");
   %baseVIT = fetchData(%aiId, "VIT");
   ```

2. **Scaled by multiplier**:
   ```torquescript
   %scaledSTR = floor(%baseSTR * %mult);
   %scaledDEX = floor(%baseDEX * %mult);
   %scaledINT = floor(%baseINT * %mult);
   %scaledVIT = floor(%baseVIT * %mult);
   ```

3. **Stored back**:
   ```torquescript
   storeData(%aiId, "STR", %scaledSTR);
   storeData(%aiId, "DEX", %scaledDEX);
   storeData(%aiId, "INT", %scaledINT);
   storeData(%aiId, "VIT", %scaledVIT);
   ```

## The Problem

**STR, DEX, INT, VIT are being fetched but may not exist for regular bots!**

When `SealBattle::SetupBot()` calls `fetchData(%aiId, "STR")`, it will return:
- `""` (empty) if the value was never set
- `-1` if the value doesn't exist
- `0` if it was explicitly set to 0

This means:
- If STR/DEX/INT/VIT are empty, scaling them will result in `floor("" * mult)` = `0` or `floor(-1 * mult)` = `-mult`
- The scaled values will be `0` or negative, which is incorrect

## Where Should STR, DEX, INT, VIT Come From?

### Option 1: From Equipment Strings (Not Currently Implemented)

Equipment strings could include:
```
"CLASS Fighter LVL 1000 STR 50 DEX 30 INT 20 VIT 40 ..."
```

But `GiveThisStuff()` does NOT parse STR, DEX, INT, VIT from equipment strings. It only parses:
- CLASS, LVL, COINS, REMORT, LCK, TOURNYRANK, RankPoints, EXP, AI, CNT, CNTAFFECTS, LVLG, LVLS, LVLE

### Option 2: Calculated from Equipment (Not Currently Implemented)

STR, DEX, INT, VIT could be calculated from equipped items via `AddPoints()`, but:
- `AddPoints(clientId, 4)` returns HP bonuses from equipment, not STR
- `AddPoints(clientId, 5)` returns MANA bonuses from equipment, not INT
- There's no `AddPoints(clientId, "STR")` or similar

### Option 3: Not Used for Regular Bots (Current State)

**For regular bots, STR, DEX, INT, VIT are NOT used at all.** They're only:
- Fetched in `SealBattle::SetupBot()` (but may be empty)
- Scaled (but scaling empty values results in 0 or negative)
- Stored back (but may not affect anything)

## Recommendation

**For Seal Battle bots, we should:**

1. **Check if STR/DEX/INT/VIT exist before scaling**:
   ```torquescript
   %baseSTR = fetchData(%aiId, "STR");
   if(%baseSTR != "" && %baseSTR != -1 && %baseSTR != 0)
   {
       %scaledSTR = floor(%baseSTR * %mult);
       storeData(%aiId, "STR", %scaledSTR);
   }
   ```

2. **OR: Set default values if they don't exist**:
   ```torquescript
   %baseSTR = fetchData(%aiId, "STR");
   if(%baseSTR == "" || %baseSTR == -1 || %baseSTR == 0)
       %baseSTR = 0;  // Default to 0 if not set
   %scaledSTR = floor(%baseSTR * %mult);
   storeData(%aiId, "STR", %scaledSTR);
   ```

3. **OR: Remove STR/DEX/INT/VIT scaling entirely** if they're not used:
   - These attributes don't appear to be used in MaxHP/MaxMANA calculations
   - Scaling them may not have any effect
   - Only LVL, RemortStep, Endurance, Energy, and base HP/MANA values matter

## Conclusion

**STR, DEX, INT, VIT are NOT used for regular bots.** They're only being scaled in `SealBattle::SetupBot()`, but:
- They may not exist (will be empty/0/-1)
- They don't appear to be used in MaxHP/MaxMANA calculations
- Scaling them may have no effect

**The actual scaling that matters is:**
- LVL (directly adds to MaxHP)
- RemortStep (affects HP and MANA)
- Endurance skill (affects HP)
- Energy skill (affects MANA)
- Base HP/MANA values from spawn config

**Recommendation:** Either remove STR/DEX/INT/VIT scaling (if not used) or add validation to ensure they exist before scaling.


