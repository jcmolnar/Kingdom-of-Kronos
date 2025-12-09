# Duplicate Functions Review

**Generated:** November 24, 2025  
**Status:** ⚠️ **TRUE DUPLICATES FOUND**

---

## Summary

Both duplicate functions are **true duplicates** - they have identical logic with only minor differences (debug statements). These should be consolidated to avoid maintenance issues and potential conflicts.

---

## 1. `round(%n)` Function

### Locations
- **rpgfunk.cs** (line 2496)
- **rpghud.cs** (line 12)

### Implementation Comparison

#### rpgfunk.cs:
```cs
function round(%n)
{
//	dbecho($dbechoMode, "round(" @ %n @ ")");

	if(%n < 0)
	{
		%t = -1;
		%n = -%n;
	}	
	else if(%n >= 0)
		%t = 1;

	%f = floor(%n);
	%a = %n - %f;
	if(%a < 0.5)
		%b = 0;
	else if(%a >= 0.5)
		%b = 1;

	return (%f + %b) * %t;
}
```

#### rpghud.cs:
```cs
function round(%n)
{
	if(%n < 0)
	{
		%t = -1;
		%n = -%n;
	}	
	else if(%n >= 0)
		%t = 1;

	%f = floor(%n);
	%a = %n - %f;
	if(%a < 0.5)
		%b = 0;
	else if(%a >= 0.5)
		%b = 1;

	return (%f + %b) * %t;
}
```

### Analysis
- ✅ **Logic:** IDENTICAL
- ⚠️ **Difference:** rpgfunk.cs has a commented-out debug statement
- **Purpose:** Rounds a number to the nearest integer (handles negative numbers correctly)

### Usage
The `round()` function is used extensively throughout the codebase:
- `playerdamage.cs` - Damage calculations
- `comchat.cs` - Distance calculations and UI elements
- `rpgstats.cs` - Stat calculations
- `Belt.cs` - Cost calculations
- `spells.cs` - Spell calculations
- `weapons.cs` - Item cost calculations
- `economy.cs` - Economy calculations
- And many more files...

**Note:** Since `rpghud.cs` is client-side only (has `if($dedicated) return;` at the top), and `rpgfunk.cs` is server-side, there may be a reason for the duplication. However, the server-side version should be the authoritative one.

---

## 2. `FixDecimals(%c)` Function

### Locations
- **rpgfunk.cs** (line 3622)
- **rpghud.cs** (line 32)

### Implementation Comparison

#### rpgfunk.cs:
```cs
function FixDecimals(%c)
{
	dbecho($dbechoMode, "FixDecimals(" @ %c @ ")");

	%d = round(%c * 10);
	%m = (%d / 10) * 1.000001;

	return %m;
}
```

#### rpghud.cs:
```cs
function FixDecimals(%c)
{
	%d = round(%c * 10);
	%m = (%d / 10) * 1.000001;

	return %m;
}
```

### Analysis
- ✅ **Logic:** IDENTICAL
- ⚠️ **Difference:** rpgfunk.cs has an active debug statement (`dbecho`)
- **Purpose:** Fixes floating-point precision issues by rounding to 1 decimal place and applying a small multiplier (1.000001) to avoid floating-point errors

### Usage
The `FixDecimals()` function is used in:
- `rpghud.cs` - For displaying weight: `$weight = FixDecimals($rpgdata["Weight"]);`
- `rpgstats.cs` - For stat calculations
- `skills.cs` - For skill-related calculations

**Note:** `rpghud.cs` calls `FixDecimals()` which in turn calls `round()`. Since `rpghud.cs` defines both functions locally, it's self-contained. However, this creates a maintenance issue if the logic needs to change.

---

## Recommendations

### Option 1: Keep Both (Current State)
**Pros:**
- `rpghud.cs` is client-side only (`if($dedicated) return;`)
- Client-side code may need these functions independently
- No risk of breaking client-side functionality

**Cons:**
- Code duplication
- Maintenance burden (changes must be made in two places)
- Risk of divergence over time

### Option 2: Remove from rpghud.cs (Recommended)
**Action:**
1. Remove `round(%n)` and `FixDecimals(%c)` from `rpghud.cs`
2. Ensure these functions are available globally when `rpghud.cs` executes
3. Since `rpgfunk.cs` is loaded before `rpghud.cs` in `Server.cs`, the functions should be available

**Pros:**
- Single source of truth
- Easier maintenance
- No code duplication

**Cons:**
- Need to verify client-side code can access server-side functions (may not work if `rpghud.cs` runs on client)

### Option 3: Create Shared Utility File
**Action:**
1. Create a new file `utilities.cs` or `mathutils.cs`
2. Move both functions there
3. Load it early in `Server.cs` (before both `rpgfunk.cs` and `rpghud.cs`)
4. Remove duplicates from both files

**Pros:**
- Best practice (shared utilities in dedicated file)
- Clear separation of concerns
- Easy to find and maintain

**Cons:**
- Requires creating a new file
- Need to ensure it loads in correct order

---

## Important Note About rpghud.cs

Looking at `rpghud.cs` line 4:
```cs
if($dedicated)
	return;
```

This file is **client-side only**. However, in Tribes, client-side scripts can still call server-side functions if they're defined globally. The question is whether `rpghud.cs` needs these functions to be defined locally or if it can rely on the server-side versions.

Since `rpghud.cs` is loaded on the server (it's in the `Server.cs` exec list), but has a check to exit early on dedicated servers, it appears to be a **hybrid** file that runs on both client and server (when not dedicated).

---

## Recommended Action

**I recommend Option 2: Remove from rpghud.cs**

**Reasoning:**
1. `rpgfunk.cs` is loaded before `rpghud.cs` in `Server.cs` (line 164 vs line 198)
2. The functions are identical, so there's no functional reason to keep both
3. `rpgfunk.cs` has the more complete version (with debug statements)
4. Reduces maintenance burden

**Steps:**
1. Remove lines 12-30 (`round` function) from `rpghud.cs`
2. Remove lines 32-38 (`FixDecimals` function) from `rpghud.cs`
3. Test that `rpghud.cs` still works correctly (it should call the functions from `rpgfunk.cs`)

**If Option 2 doesn't work** (e.g., client-side can't access server functions), then **Option 3** (shared utility file) is the best alternative.

---

## Verification

After making changes, verify:
1. ✅ HUD still displays correctly
2. ✅ Weight display works (`FixDecimals` is used for weight)
3. ✅ No script errors in console
4. ✅ Functions are accessible where needed

---

**End of Review**

