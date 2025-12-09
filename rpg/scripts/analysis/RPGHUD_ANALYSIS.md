# rpghud.cs Analysis

**Purpose:** Client-side HUD (Heads-Up Display) script for displaying RPG stats  
**Type:** **CLIENT-SIDE ONLY** script  
**Requires:** PrestoPack (client-side mod)

---

## What is rpghud.cs?

`rpghud.cs` is a **client-side script** that creates an on-screen HUD displaying player RPG statistics. It is **NOT** a server-side script, despite being loaded by `Server.cs`.

### Key Indicators It's Client-Side:

1. **Line 4-5:** `if($dedicated) return;`
   - This exits immediately on dedicated servers
   - Only executes when running a client (non-dedicated server)

2. **Line 7:** `EditActionMap("playMap.sae");`
   - Client-side function for editing action maps

3. **Line 8:** `bindCommand(keyboard0, make, "2", TO, "toggleRPGhud();");`
   - Client-side keyboard binding (binds "2" key to toggle HUD)

4. **Lines 128-129:** `HUD::New()` and `HUD::Display()`
   - These are PrestoPack functions (client-side mod)
   - From the changelog: "Prevents 'Unknown command' errors on dedicated servers where PrestoPack HUD functions aren't available"

---

## How It Works

### Data Flow:

```
Client (rpghud.cs)
  ↓ calls rpgfetchdata("MANA")
  ↓ (client-side function, likely in PrestoPack or base game)
  ↓ sends request to server
  ↓
Server (rpgstats.cs)
  ↓ remotefetchData(%clientId, "MANA")
  ↓ fetches data: fetchData(%clientId, "MANA")
  ↓ sends back via: remoteEval(%clientId, SetRPGdata, %data, "MANA")
  ↓
Client
  ↓ receives data in $rpgdata["MANA"]
  ↓ displays on HUD
```

### Functions:

1. **`getrpgdata()`** (lines 40-89)
   - Fetches RPG data from server using `rpgfetchdata()`
   - Stores data in global `$rpgdata` array
   - Updates weight less frequently (every 6 calls / delay)

2. **`UpdateRPGHUD(%hud)`** (lines 91-121)
   - Main HUD update function
   - Called by PrestoPack HUD system
   - Displays: Level, Remort, Race, Class, Zone, HP, Mana, EXP, Weight, Coins, Bank, ATK, DEF, MDEF, LCK

3. **`toggleRPGhud()`** (lines 123-126)
   - Toggles HUD visibility
   - Bound to "2" key

4. **`round(%n)` and `FixDecimals(%c)`** (lines 12-38)
   - Utility functions for number formatting
   - Used for weight display: `$weight = FixDecimals($rpgdata["Weight"]);`

---

## Why It's Loaded by Server.cs

In Tribes, scripts can be loaded on both server and client:
- **Server.cs** loads scripts that are sent to clients
- The `if($dedicated) return;` check ensures it only **executes** on clients
- This allows the script to be distributed to all clients automatically

---

## Server Host Perspective

### From a Server Host's Perspective:

**Does it serve a purpose?** **YES, but indirectly:**

1. **Distribution:** Server.cs loads it, so it gets sent to all clients automatically
2. **No Server Processing:** The script does NOT run on dedicated servers (exits immediately)
3. **Client Experience:** It enhances the client experience by showing stats in a HUD
4. **No Server Resources:** Since it exits on dedicated servers, it uses zero server resources

### What Happens on a Dedicated Server:

```cs
if($dedicated)
    return;  // ← Script exits here, nothing else executes
```

**Result:** The entire script is skipped. No functions are defined, no HUD is created, no processing happens.

### What Happens on a Client:

- Script executes fully
- HUD is created and displayed
- Keyboard bindings are set up
- HUD updates periodically showing player stats

---

## About the Duplicate Functions

### Why `round()` and `FixDecimals()` are duplicated:

**Current Situation:**
- Both functions exist in `rpgfunk.cs` (server-side)
- Both functions exist in `rpghud.cs` (client-side)

**Why the duplication might exist:**

1. **Client Independence:** Client-side code may need these functions to work independently
2. **Scope Issues:** Client-side scripts might not reliably access server-side functions
3. **Safety:** Having them locally ensures the HUD works even if server-side functions aren't accessible
4. **Legacy:** May have been copied before the server-side versions existed

### Should They Be Removed?

**Recommendation: TEST FIRST**

Since `rpghud.cs` is client-side and `rpgfunk.cs` is server-side:
- The client may not have direct access to server-side functions
- However, since both scripts are loaded on the client (Server.cs sends them), the functions from `rpgfunk.cs` should be available

**To determine if removal is safe:**
1. Remove the duplicate functions from `rpghud.cs`
2. Test that the HUD still works
3. Check that weight display works (uses `FixDecimals`)
4. Verify no "Unknown function" errors

**If it doesn't work:**
- Keep the duplicates (they're needed for client-side independence)
- Or create a shared client-side utility file

---

## Summary

| Aspect | Details |
|--------|---------|
| **Type** | Client-side script |
| **Purpose** | Display RPG stats in on-screen HUD |
| **Server Impact** | None (exits immediately on dedicated servers) |
| **Client Impact** | Creates HUD, binds keys, displays stats |
| **Requires** | PrestoPack mod (client-side) |
| **Loaded By** | Server.cs (for distribution to clients) |
| **Executes On** | Clients only (not dedicated servers) |
| **Duplicate Functions** | `round()` and `FixDecimals()` - may be needed for client independence |

---

**Conclusion:** `rpghud.cs` is a **client-side enhancement script** that has **zero impact on server hosting**. It's loaded by the server only to distribute it to clients. On dedicated servers, it does nothing. On clients, it provides a nice HUD display of RPG stats.

---

**End of Analysis**

