# Master List of clipTrailingNumbers() Usage in AllScripts.txt

## Summary
This document categorizes all instances of `clipTrailingNumbers()` found in AllScripts.txt by what they affect: **Players**, **Townbots**, or **Enemy Bots**.

**Total Instances Found:** 13 (excluding function definition)
- **Already Fixed (using backwards digit removal):** 6 instances
- **Still Using clipTrailingNumbers():** 5 instances
- **Function Definition:** 1 instance

---

## Category 1: ENEMY BOTS

### Already Fixed (Using Backwards Digit Removal)

1. **Line 4508** - `createAI()` function (`scripts/Ai.cs`)
   - **Context:** Extracts guardtype from AI name for armor/race lookup
   - **Affects:** Enemy bots spawned via `createAI()`
   - **Status:** ✅ FIXED - Uses backwards digit removal logic
   - **Code Location:** `scripts/Ai.cs` line ~1025

2. **Line 5052** - `AI::setWeapons()` function (`scripts/Ai.cs`)
   - **Context:** Extracts guardtype from AI name for `$BotEquipment` lookup
   - **Affects:** Enemy bots when setting weapons/equipment
   - **Status:** ✅ FIXED - Uses backwards digit removal logic
   - **Code Location:** `scripts/Ai.cs` line ~1569

3. **Line 87925** - `SealBattle::SetupBot()` function (`scripts/remortseal.cs`)
   - **Context:** Extracts guardtype from bot name (internal name like "RoundTwo497")
   - **Affects:** Seal Battle enemy bots
   - **Status:** ✅ FIXED - Uses backwards digit removal logic
   - **Code Location:** `scripts/remortseal.cs` line ~1354

4. **Line 87956** - `SealBattle::SetupBot()` function (`scripts/remortseal.cs`)
   - **Context:** Extracts guardtype from AI name (fallback if bot name fails)
   - **Affects:** Seal Battle enemy bots
   - **Status:** ✅ FIXED - Uses backwards digit removal logic
   - **Code Location:** `scripts/remortseal.cs` line ~1361

5. **Line 90663** - `Colloseum::SetupBot()` function (`scripts/rpgarena.cs`)
   - **Context:** Extracts guardtype from BotInfoAiName for equipment lookup
   - **Affects:** Colloseum enemy bots
   - **Status:** ✅ FIXED - Uses backwards digit removal logic
   - **Code Location:** `scripts/rpgarena.cs` line ~1149

### Still Using clipTrailingNumbers() - NEEDS FIX

6. **Line 6247** - `UpdateAppearance()` function (`scripts/Ai.cs`)
   - **Context:** Extracts guardtype from bot name to get race for armor type lookup
   - **Affects:** All bots (enemy bots and townbots) when updating appearance
   - **Status:** ❌ NEEDS FIX - Still using `clipTrailingNumbers(%botName)`
   - **Code Location:** `scripts/Ai.cs` line ~2764
   - **Impact:** If bot name is "Obliterator0", returns empty string instead of "Obliterator", causing race lookup to fail

7. **Line 7936** - `SpawnAI()` function (`scripts/Ai.cs`)
   - **Context:** Extracts guardtype from bot name to set RACE before UpdateTeam()
   - **Affects:** Enemy bots spawned from spawn points (SpawnPoint type)
   - **Status:** ❌ NEEDS FIX - Still using `clipTrailingNumbers(%newName)`
   - **Code Location:** `scripts/Ai.cs` line ~4453
   - **Impact:** If bot name is "Abolisher1", returns empty string instead of "Abolisher", causing race lookup to fail

8. **Line 8030** - `SpawnAI()` function (`scripts/Ai.cs`)
   - **Context:** Extracts guardtype from bot name to set RACE before UpdateTeam() (fallback logic)
   - **Affects:** Enemy bots spawned from spawn points (SpawnPoint type)
   - **Status:** ❌ NEEDS FIX - Still using `clipTrailingNumbers(%newName)`
   - **Code Location:** `scripts/Ai.cs` line ~4547
   - **Impact:** If bot name is "Liquifier2", returns empty string instead of "Liquifier", causing race lookup to fail

---

## Category 2: TOWNBOTS

### Already Fixed (Using Backwards Digit Removal)

9. **Line 34874** - Townbot interaction code (`scripts/comchat.cs`)
   - **Context:** Extracts bot type from AI name for bot type detection (merchant, banker, quest, etc.)
   - **Affects:** Townbots when players interact with them
   - **Status:** ✅ FIXED - Uses backwards digit removal logic
   - **Code Location:** `scripts/comchat.cs` line ~9276

### Still Using clipTrailingNumbers() - NEEDS FIX

10. **Line 68455** - `LasVegas.cs` gambler bot interaction
    - **Context:** Checks if bot type is "gambler" for gambler bot interaction
    - **Affects:** Townbots (gambler bots)
    - **Status:** ❌ NEEDS FIX - Still using `clipTrailingNumbers(%aiName) == "gambler"`
    - **Code Location:** `scripts/LasVegas.cs` line 1
    - **Impact:** If bot name is "gambler1", returns "gamb" instead of "gambler", preventing gambler bot interaction

---

## Category 3: ALL BOTS (General Bot Info Parsing)

### Still Using clipTrailingNumbers() - NEEDS FIX

11. **Line 13798** - BotInfo parsing from SimGroup (`scripts/Ai.cs`)
    - **Context:** Parses bot info from SimGroup names (e.g., "NAME1", "LVL2", "RACE3")
    - **Affects:** All bots (enemy bots and townbots) when loading bot info from mission groups
    - **Status:** ❌ NEEDS FIX - Still using `clipTrailingNumbers(%type)`
    - **Code Location:** `scripts/Ai.cs` line ~10315
    - **Impact:** If group name is "NAME10", returns "NAME1" instead of "NAME", causing parsing to fail
    - **Note:** This is parsing SimGroup names, not bot names, so the impact may be different

---

## Category 4: FUNCTION DEFINITION

12. **Line 94447** - Function definition (`scripts/rpgfunk.cs`)
    - **Context:** The actual `clipTrailingNumbers()` function definition
    - **Affects:** N/A (function definition)
    - **Status:** ⚠️ KEEP - Function definition should remain for backwards compatibility (even though it's buggy)
    - **Code Location:** `scripts/rpgfunk.cs` line ~3146
    - **Note:** The function should remain defined but all usages should be replaced with backwards digit removal logic

---

## Priority Fix List

### High Priority (Affects Core Functionality)

1. **Line 6247** - `UpdateAppearance()` - Affects ALL bots (enemy + townbots)
2. **Line 7936** - `SpawnAI()` - Affects enemy bot spawning
3. **Line 8030** - `SpawnAI()` - Affects enemy bot spawning
4. **Line 68455** - `LasVegas.cs` gambler bot - Affects townbot interaction

### Medium Priority (May Have Edge Cases)

5. **Line 13798** - BotInfo parsing - Affects all bots but may have different impact since it's parsing SimGroup names

---

## Recommended Fix Pattern

Replace all instances with backwards digit removal logic:

```torquescript
// Extract guardtype/botType by removing trailing digits (more reliable than clipTrailingNumbers)
// This works backwards from the end to find and remove only trailing digits
%guardtype = %botName;  // or %aiName, %newName, %type, etc.
%len = String::len(%botName);
%numStr = "";

// Find trailing digits (working backwards)
for(%i = %len - 1; %i >= 0; %i--)
{
    %char = String::getSubStr(%botName, %i, 1);
    if(%char >= "0" && %char <= "9")
    {
        %numStr = %char @ %numStr;
    }
    else
    {
        break;
    }
}

// If we found trailing digits, remove them
if(%numStr != "")
{
    %guardtype = String::getSubStr(%botName, 0, %len - String::len(%numStr));
}
```

---

## Notes

- The function `clipTrailingNumbers()` works forwards from the start of the string, which causes it to fail for names like "quest1" (returns "qu" instead of "quest") or "Liquifier1" (returns empty string instead of "Liquifier").
- The backwards digit removal logic works correctly for all cases:
  - "quest1" → "quest" ✅
  - "merchant2" → "merchant" ✅
  - "Obliterator0" → "Obliterator" ✅
  - "Liquifier1" → "Liquifier" ✅
  - "RoundTwo497" → "RoundTwo" ✅


