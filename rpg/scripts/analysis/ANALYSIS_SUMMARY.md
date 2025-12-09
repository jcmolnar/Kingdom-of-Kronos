# Analysis Summary - Functions and Variables

**Generated:** November 24, 2025  
**Analysis Type:** Comprehensive function and variable extraction with duplicate detection

---

## Summary Statistics

### Functions
- **Total Unique Functions:** 928
- **Duplicate Function Signatures:** 2 (requires investigation)
- **Functions with Same Name, Different Signatures:** Multiple (overloaded functions - this is normal)

### Variables
- **Total Unique Variables:** 551 (after deduplication)
- **Original Count (with duplicates):** 2,038
- **Variables Used in Multiple Scripts:** 101 (global variables - this is normal)

---

## Key Findings

### ✅ Variables - Successfully Deduplicated

The original analysis was counting the same variable multiple times within the same file. After deduplication:
- **Before:** 2,038 variable entries
- **After:** 551 unique variables
- **Reduction:** ~73% reduction by removing duplicates

**Example of duplicates found:**
- `console::logmode` appeared twice in globals.cs
- `dbechoMode` appeared twice in globals.cs
- `arenaOn` appeared twice in globals.cs
- Many variables were being counted multiple times due to multiple assignments or array access patterns

### ⚠️ Duplicate Functions Found

**2 functions have identical signatures in multiple files:**

1. **`FixDecimals(%c)`**
   - Found in: `rpgfunk.cs`, `rpghud.cs`
   - **Action Required:** Review both implementations. If they're identical, consider:
     - Moving to a common utility file
     - Removing one duplicate
     - Or keeping both if they serve different purposes (unlikely with same signature)

2. **`round(%n)`**
   - Found in: `rpgfunk.cs`, `rpghud.cs`
   - **Action Required:** Same as above - review both implementations

### 📊 Functions with Same Name, Different Signatures

These are **NOT duplicates** - they are overloaded functions or functions in different namespaces:

- `Game::onPlayerConnected` - Different signatures in `game.cs` and `connectivity.cs` (likely different purposes)
- `messageAll` - Different signatures in `game.cs` and `comchat.cs` (overloaded function)
- `Vehicle::findEmptySeat` - Multiple signatures in `Vehicle.cs` (overloaded function)

**This is normal and expected behavior.**

---

## Files Generated

1. **`CLEANED_ANALYSIS.txt`**
   - Complete list of all unique functions and variables by script
   - Deduplicated variables (each variable listed once per script)
   - Summary statistics

2. **`DUPLICATE_FUNCTIONS_LIST.txt`**
   - Detailed list of duplicate function signatures
   - Action items for each duplicate

3. **`DUPLICATE_ANALYSIS.txt`**
   - Comprehensive duplicate analysis
   - Functions with same name but different signatures
   - Variables used across multiple scripts

4. **`ANALYSIS_RESULTS.txt`**
   - Original raw analysis (includes duplicates for reference)

---

## Recommendations

### Immediate Actions

1. **Review the 2 duplicate functions:**
   - Check `FixDecimals(%c)` in both `rpgfunk.cs` and `rpghud.cs`
   - Check `round(%n)` in both `rpgfunk.cs` and `rpghud.cs`
   - Determine if they're truly duplicates or serve different purposes
   - If duplicates, remove one or move to a common utility file

### Long-term Improvements

1. **Create a common utilities file** for shared functions like `FixDecimals` and `round`
2. **Document the 101 global variables** that are used across multiple scripts
3. **Review function naming** to ensure consistency across the codebase

---

## Scripts Analyzed

64 scripts total, loaded in order by `Server.cs`:
- globals.cs, Ai.cs, rpgfunk.cs, skills.cs, house.cs, rpgarena.cs
- sleep.cs, game.cs, Admin.cs, marker.cs, trigger.cs, zone.cs
- spells.cs, classes.cs, party.cs, jail.cs, nsound.cs, Help.cs
- baseExpData.cs, baseDebrisData.cs, baseProjData.cs, armordata.cs
- mission.cs, item.cs, Accessory.cs, weapons.cs, armors.cs
- Crystal.cs, Spawn.cs, connectivity.cs, gameevents.cs, shopping.cs
- weight.cs, mana.cs, hp.cs, rpgstats.cs, rpghud.cs, playerdamage.cs
- playerspawn.cs, itemevents.cs, Belt.cs, economy.cs, remote.cs
- weaponHandling.cs, BonusState.cs, ferry.cs, Player.cs, Vehicle.cs
- Turret.cs, beacon.cs, staticshape.cs, Station.cs, moveable.cs
- sensor.cs, Mine.cs, interiorLight.cs, comchat.cs, plugs.cs
- version.cs, hackfix.cs, newstuff.cs, advertisements.cs
- remortseal.cs, DebugInit.cs

---

## Notes

- All variables are now properly deduplicated
- Function extraction is accurate and complete
- Only 2 true duplicate function signatures found (both utility functions)
- The codebase is generally well-organized with minimal duplication

---

**End of Summary**

