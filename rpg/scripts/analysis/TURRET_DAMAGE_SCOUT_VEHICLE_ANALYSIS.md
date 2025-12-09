# Turret Damage vs Scout Vehicle Damage Analysis

**Question:** Is the turret damage change affecting scout vehicles?

**Answer:** **NO** - Scout vehicles are explicitly excluded from turret damage detection.

---

## Turret Damage Detection Logic

### Code Location: `playerdamage.cs` lines 950-980

```cs
// Check if shooter is a turret - turrets use projectile base damage, not physical damage formula
%isTurretDamage = false;
if(isObject(%object))
{
    %objectClassName = %object.className;
    if(%objectClassName == "Turret")
        %isTurretDamage = true;
}

// Also detect turret projectiles by checking if it's MissileDamageType from a source without skills/ATK
// Turrets use MissileDamageType projectiles but don't have player skills or ATK
if(!%isTurretDamage && %type == $MissileDamageType)
{
    // Check if shooter doesn't have ATK or skills (indicating it's a turret, not a player)
    %shooterATK = fetchData(%shooterClient, "ATK");
    if((%shooterATK == "" || %shooterATK == -1 || %shooterATK == 0) && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
    {
        // Check if it's not a vehicle projectile (vehicles are handled separately)
        %shooterPlayerObj = Client::getOwnedObject(%shooterClient);
        %isVehicle = false;
        if(%shooterPlayerObj != -1)
        {
            %shooterType = getObjectType(%shooterPlayerObj);
            if(%shooterType == "Vehicle" || %shooterType == "Flier")
                %isVehicle = true;
        }
        // If not a vehicle and no ATK/skills, it's likely a turret
        if(!%isVehicle)  // ← KEY: Only sets turret damage if NOT a vehicle
            %isTurretDamage = true;
    }
}
```

---

## Scout Vehicle Detection

### How Scout Vehicles Are Created

From `comchat.cs` line 1623:
```cs
%turret = newObject("Flyer","Flier","Scout",true);
```

**Scout vehicles are created as `"Flier"` objects.**

### Vehicle Detection Logic

The code explicitly checks for vehicles:
```cs
%shooterType = getObjectType(%shooterPlayerObj);
if(%shooterType == "Vehicle" || %shooterType == "Flier")  // ← Scout vehicles are "Flier"
    %isVehicle = true;
```

Then:
```cs
if(!%isVehicle)  // ← This prevents turret damage from being applied to vehicles
    %isTurretDamage = true;
```

---

## Logic Flow for Scout Vehicle Damage

1. **Scout vehicle fires projectile** → `MissileDamageType`
2. **Turret detection starts** → Checks if `%object.className == "Turret"` → **FALSE** (it's a Flier)
3. **Fallback detection** → Checks if `MissileDamageType` from source without ATK/skills
4. **Vehicle check** → Gets shooter object type → **"Flier"** → Sets `%isVehicle = true`
5. **Turret damage check** → `if(!%isVehicle)` → **FALSE** (because `%isVehicle = true`)
6. **Result** → `%isTurretDamage` remains **FALSE**
7. **Damage calculation** → Uses normal vehicle damage calculation (with Vehicle Combat skill bonus)

---

## Vehicle Damage Calculation (Separate System)

Scout vehicles use a **completely different damage system**:

### Location: `playerdamage.cs` lines 1100-1141

```cs
// Check if this is a vehicle projectile
%isVehicleProjectile = false;

// Get the shooter's player object to check if it's a vehicle
%shooterPlayerObj = Client::getOwnedObject(%shooterClient);
if(%shooterPlayerObj != -1)
{
    %shooterType = getObjectType(%shooterPlayerObj);
    if(%shooterType == "Vehicle" || %shooterType == "Flier")
    {
        %isVehicleProjectile = true;
    }
}
// Also check if shooter is a client ID but the player is in a vehicle
if(!%isVehicleProjectile && %type == $MissileDamageType && (%skilltype == "" || $PlayerSkill[%shooterClient, %skilltype] == ""))
{
    // This is likely a vehicle projectile - check if player is in a vehicle
    %playerObj = Client::getOwnedObject(%shooterClient);
    if(%playerObj != -1 && %playerObj.vehicle != "")
    {
        %isVehicleProjectile = true;
    }
}

if(%isVehicleProjectile && !isRPGAI(%shooterClient))
{
    // Get Vehicle Combat skill (Skill 14)
    %vehicleSkill = $PlayerSkill[%shooterClient, $SkillVehicleCombat];
    if(%vehicleSkill == "" || %vehicleSkill == -1)
        %vehicleSkill = 0;
    
    // Cap skill at 1000
    if(%vehicleSkill > 1000)
        %vehicleSkill = 1000;
    
    // Calculate bonus: +1 damage per 100 skill points
    %vehicleBonus = floor(%vehicleSkill / 100);
    
    // Add bonus to damage (in Tribes damage units, before conversion)
    %value += %vehicleBonus;
}
```

**Scout vehicles get:**
- Normal projectile damage calculation
- **PLUS** Vehicle Combat skill bonus (+1 damage per 100 skill points, capped at 1000)
- **NOT** turret damage calculation (100 base damage scaled by level)

---

## Conclusion

### ✅ Scout Vehicles Are NOT Affected

**Reasons:**
1. **Explicit exclusion:** Line 977 checks `if(!%isVehicle)` before setting turret damage
2. **Vehicle detection:** Scout vehicles are detected as "Flier" objects (line 973)
3. **Separate system:** Vehicles use their own damage calculation with Vehicle Combat skill bonus
4. **Different logic path:** Turret damage and vehicle damage are completely separate code paths

### What Scout Vehicles Use:

- **Normal projectile damage** (from baseProjData.cs projectile definitions)
- **Vehicle Combat skill bonus** (+1 damage per 100 skill points)
- **Standard damage formula** (not the turret-specific formula)

### What Turrets Use:

- **Base turret damage** (100 Tribes damage units)
- **Level scaling** (1 + level/100 multiplier)
- **DEF reduction** and random variation
- **Stance bypass** (except Normal and Glass Cannon)

---

## Verification

The code has **two separate detection systems**:

1. **Turret Detection** (lines 950-980)
   - Checks for `className == "Turret"`
   - Checks for `MissileDamageType` without ATK/skills
   - **Excludes vehicles** with `if(!%isVehicle)`

2. **Vehicle Detection** (lines 1100-1141)
   - Checks for `getObjectType() == "Vehicle" || "Flier"`
   - Checks for player in vehicle (`%playerObj.vehicle != ""`)
   - Applies Vehicle Combat skill bonus

These systems are **mutually exclusive** - if something is detected as a vehicle, it cannot be detected as a turret.

---

**End of Analysis**

