# New RPG Ideas for Kingdom of Kronos
## Based on Review of Tribes RPG Mods Collection

---

## 1. COMBAT & DAMAGE SYSTEM ENHANCEMENTS

### 1.1 Advanced Hit/Miss System
**Source:** `playerdamage.cs` (NWVII)
- **Stamina-based forced misses**: When STA <= 2, force miss. When STA <= 20 (level 20+), 25% miss chance. When STA <= 50 (level 50+), 20% miss chance.
- **DEX-based hit calculation**: Complex tiered system based on DEX difference between attacker and defender (1d3 to 1d20 rolls depending on gap).
- **Lucky Ring mechanics**: Special item that affects critical hit chances (both players having ring = different crit thresholds).
- **Implementation**: Add `ForcedMiss()` function, enhance `Player::onDamage()` with STA checks, implement DEX-based hit tiers.

### 1.2 Critical Hit System
**Source:** `playerdamage.cs` (NWVII)
- **Roll-based criticals**: 1d20 roll system with modifiers from items (Lucky Ring).
- **Critical damage multiplier**: 2x damage on critical hits.
- **Critical stamina drain**: Different stamina drain on critical vs normal vs miss.
- **Implementation**: Add critical detection in damage calculation, visual feedback ("Critical hit!"), stamina drain scaling.

### 1.3 Backstab System
**Source:** `playerdamage.cs` (NWVII)
- **Position-based backstab**: Check if attacker is behind defender (rotation comparison).
- **Piercing weapon requirement**: Only works with piercing damage type.
- **Level-based multiplier**: `round(getFinalLVL(%sClient)/4)+1` damage multiplier.
- **Invisibility reveal**: Backstab reveals invisible attacker.
- **Implementation**: Add rotation check in `Player::onDamage()`, verify weapon type, apply multiplier.

### 1.4 Bash System
**Source:** `playerdamage.cs` (NWVII)
- **Charge-up bash**: `$NextHitBash[%sClient]` flag for next hit.
- **Bludgeoning weapon requirement**: Only works with bludgeoning damage type.
- **Level-based multiplier**: `Cap(round(getFinalLVL(%sClient)/10), -1, 2)` additional damage.
- **Knockback**: Bash applies momentum based on bash power.
- **Cooldown**: Block bash for `Cap(150-getFinalLVL(%sClient), 5, 50)` frames.
- **Implementation**: Add bash charge system, momentum calculation, cooldown tracking.

### 1.5 Attack Bonus System
**Source:** `playerdamage.cs` (NWVII)
- **Weapon-based bonuses**: `$AttackBonus[%type, %clientId]` tracks bonus count.
- **Exponential scaling**: `%ATKBonus += (%ATKBonus * %BonusCnt) / 5` for multiple bonuses.
- **Type-specific**: Different bonuses for different damage types.
- **Implementation**: Track attack bonuses per damage type, apply in damage calculation.

### 1.6 Damage Display System
**Source:** `playerdamage.cs` (NWVII)
- **Client-side damage text**: `remoteEval(%clientId, "ATKText", ...)` for damage display.
- **Color coding**: Different colors for attacker vs defender.
- **Special attack indicators**: "Backstabbed!", "Bashed!", "Critical hit!" prefixes.
- **Miss messages**: "MISS!", "MISS! (STA)", "MISS! (LCK)" with reasons.
- **Implementation**: Add client-side damage display system, color coding, special attack indicators.

### 1.7 Degradable Weapons/Armor
**Source:** `playerdamage.cs` (NWVII)
- **Random degradation chance**: `floor(getRandom() * 1234) == 50` (very rare).
- **Life Stream blessing**: Chance to repair/upgrade instead of degrade.
- **Armor damage tracking**: Track which armor piece takes damage.
- **Implementation**: Add degradation chance on hit, repair/upgrade system, armor tracking.

---

## 2. SKILL SYSTEM ENHANCEMENTS

### 2.1 Trait-Based Skill Multipliers
**Source:** `skills.cs` (NWVII)
- **Trait influence**: Skills affected by STR, INT, DEX, MEM, END traits.
- **Formula-based multipliers**: `(0.25 + $playerTrait[%client, "str"] * 0.3 + ...) * 4) / 10`
- **Per-skill calculations**: Each skill has different trait weightings.
- **Implementation**: Add `SetMultipliers()` function, trait system, skill multiplier calculations.

### 2.2 Skill Restrictions
**Source:** `skills.cs` (NWVII)
- **Item-based restrictions**: Items require minimum skill levels.
- **Multiple restriction types**: Level, Group, Class, Remort, Admin Level, House.
- **Formula-based restrictions**: `Cap((a / b) - 20), 0, "inf") * 10.0` for weapons/armor.
- **Implementation**: Add `$SkillRestriction[%item]` system, check restrictions on equip/use.

### 2.3 Skill Usage Tracking
**Source:** `playerdamage.cs` (NWVII)
- **Skill counter**: `SkillCounter(%sClient, %dClient, %type, 1)` tracks skill usage.
- **Type-specific tracking**: Different skills for different damage types.
- **Implementation**: Add skill usage tracking, experience gain from skill use.

### 2.4 Expanded Skill List
**Source:** `skills.cs` (NWVII, KoK)
- **New skills**: Wood Cutting, Weight Capacity, Bashing, Stealing, Hiding, Backstabbing, Farming, Mining, Traps, Sense Heading, Cooking, Haggling, Crafting, Archery, Armor, Dodging, Endurance, Criticals, Speech, Energy, Healing.
- **Skill descriptions**: `$SkillDesc[%skillId]` for display.
- **Implementation**: Add new skill definitions, skill gain functions, skill requirements.

---

## 3. ECONOMY & TRADING SYSTEM

### 3.1 Dynamic Merchant Economy
**Source:** `economy.cs` (NWVII)
- **Merchant inventory tracking**: `$merchantShop[%aiName, %item]` tracks stock.
- **Buy/sell counters**: `$MerchantCounterB[%aiName, %item]` and `$MerchantCounterS[%aiName, %item]` track transactions.
- **Stock depletion**: Merchants run out of items when sold.
- **Custom pricing**: `$NewItemBuyCost[%aiName, %item]` and `$NewItemSellCost[%aiName, %item]` for per-merchant pricing.
- **Implementation**: Add merchant inventory system, stock tracking, custom pricing.

### 3.2 Haggling System
**Source:** `economy.cs` (NWVII)
- **Skill-based discounts**: `$SkillHaggling` affects buy/sell prices.
- **Buy price reduction**: `round($PlayerSkill[%clientId, $SkillStealing] / 20) / 100` up to 50% discount.
- **Sell price increase**: `round($PlayerSkill[%clientId, $SkillStealing] / 10) / 125` up to 100% bonus.
- **Skill usage**: `UseSkill(%clientId, $SkillHaggling, False, False)` on transactions.
- **Implementation**: Add haggling skill, price modification functions, skill gain on trade.

### 3.3 Stealing/Pickpocketing System
**Source:** `economy.cs` (NWVII)
- **Pickpocket mode**: `%clientId.currentInvSteal` tracks target.
- **Distance check**: Must be within 2 units to steal.
- **Weight consideration**: Item weight affects steal success.
- **Skill requirement**: Uses `$SkillStealing`.
- **Implementation**: Add pickpocket command, distance checks, steal success calculation.

---

## 4. RESOURCE GATHERING & CRAFTING

### 4.1 Wood Cutting System
**Source:** `wood.cs`, `worldtree.cs` (NWVII)
- **Tree objects**: `StaticShapeData Tree` with HP system.
- **Chopping mechanics**: `tree::chop()` reduces tree HP, drops wood based on skill.
- **Skill-based drops**: Higher wood cutting skill = better wood types.
- **Tree respawn**: Trees sink and respawn after being chopped.
- **Wood types**: Splint, Twig, Stick, Rod, LongRod, Lumber, OakWood, PineWood, BireWood, WormWood, RedWood.
- **Implementation**: Add tree objects, chopping system, skill-based drops, respawn mechanics.

### 4.2 Farming System
**Source:** `FruitTree.cs`, `wood.cs` (NWVII)
- **Fruit trees**: Trees that drop fruit items (Apple, Pear, Orange, Coconut, Banana, Pomegranate).
- **Harvester tool**: Special weapon `Harvest` for harvesting fruit.
- **Tree health system**: Trees have `cuts` counter, destroyed after enough harvests.
- **Fruit consumption**: Fruits restore MANA (3-25 points depending on type).
- **Tree sinking/respawning**: Trees sink when destroyed, respawn after random time.
- **Implementation**: Add fruit tree system, harvester tool, fruit items, consumption effects.

### 4.3 Cooking System
**Source:** `wood.cs` (NWVII)
- **Cooking ingredients**: Grain, ValdueBerry, RedBerry, BlueBerry, StrawBerry, TwigFruit, GreenFruit, Grape, WhiteGrape.
- **Skill requirements**: Each ingredient requires minimum cooking skill (0.1 to 750).
- **Ingredient costs**: Hardcoded costs for each ingredient.
- **Implementation**: Add cooking ingredients, skill requirements, cooking recipes.

### 4.4 Mining System
**Source:** `skills.cs` (NWVII)
- **Mining skill**: `$SkillMining` for resource gathering.
- **Trait-based**: Mining affected by STR, INT, END traits.
- **Implementation**: Add mining locations, skill-based resource drops, mining tools.

---

## 5. SPELL SYSTEM ENHANCEMENTS

### 5.1 Spell Types
**Source:** `spells.cs` (NWVII)
- **Spell categories**: Self, LOS, SelfRadius, LOSRadius, SelfRadiusLOS, LOSRadiusSelf, SelfRadiusLOSRadius.
- **Spell definitions**: Keyword, name, description, delay, recovery time, damage value, LOS range, mana cost, sounds.
- **Skill types**: Offensive, Defensive, Neutral casting skills.
- **Implementation**: Add spell type system, spell definition structure, skill-based casting.

### 5.2 Spell Casting System
**Source:** `spells.cs` (NWVII)
- **Casting steps**: `$SpellCastStep[%clientId]` tracks casting progress.
- **Mana cost split**: Half mana cost upfront, half on completion.
- **Recovery time calculation**: Based on skill level, `(1000 - %sk) / 1000` affects recovery.
- **LOS requirement**: Spells can require line of sight.
- **Implementation**: Add spell casting state machine, mana cost system, recovery time calculation.

### 5.3 Spell Damage System
**Source:** `playerdamage.cs` (NWVII)
- **INT-based damage**: Spell damage based on caster's INT vs target's INT.
- **WIS modifier**: `round(getFinalWIS(%sClient) * 1.4)` affects damage.
- **MDEF resistance**: Target's MDEF reduces spell damage.
- **Level difference**: Damage affected by level difference between caster and target.
- **Hit chance**: Complex INT-based hit calculation (1d10 to 1d40 rolls).
- **Implementation**: Add spell damage calculation, INT-based hit system, MDEF resistance.

---

## 6. DEATH & RESPAWN SYSTEM

### 6.1 Death EXP Loss
**Source:** `playerdamage.cs` (NWVII)
- **Killed by player**: `floor(%ge + pow(%lvl, 2))` EXP loss.
- **Self-kill**: `floor(%ge + pow(%lvl, 2.5))` EXP loss (more severe).
- **Ctrl+K penalty**: Additional `500 * %lvl` EXP loss for suicide.
- **Level protection**: No EXP loss for levels <= 10.
- **Implementation**: Add death EXP loss calculation, level protection, suicide penalty.

### 6.2 PKer System
**Source:** `playerdamage.cs` (NWVII)
- **PKer flag**: `UpdateBonusState(%KillerId, "PKer", 30 * 15, "add")` for 15 minutes.
- **Legal kills**: Kills in self-defense don't mark as PKer.
- **Zone-based**: Different rules for FREEFORALL zones.
- **Bounty system**: `$bounty[%killerId]` tracks bounties.
- **Implementation**: Add PKer tracking, time-based flags, bounty system.

### 6.3 Lootbag System Enhancements
**Source:** `playerdamage.cs` (NWVII)
- **LCK-based protection**: Items protected based on LCK remaining.
- **Equipped item priority**: Currently equipped weapon always drops if LCK < 0.
- **Lore item protection**: `$LoreItem[%a]` items always drop.
- **NoDrop items**: `$ItemData[%a, NoDrop]` items never drop.
- **Timeout system**: Different timeouts based on LCK (300 seconds with LCK, 5 seconds without).
- **Implementation**: Enhance lootbag system, LCK-based protection, item flags.

---

## 7. CLASS & GROUP SYSTEM

### 7.1 Class Multipliers
**Source:** `classes.cs`, `skills.cs` (KoK, NWVII)
- **Per-class skill multipliers**: Each class has different multipliers for each skill (0.1 to 2.0).
- **Primary skills**: 2.0 multiplier (class specialty).
- **Secondary skills**: 1.5 multiplier.
- **Normal skills**: ~1.0 multiplier.
- **Weak skills**: ~0.5 multiplier.
- **Very weak skills**: 0.2 multiplier.
- **Unsuitable skills**: 0.1 multiplier.
- **Implementation**: Add class multiplier system, skill gain modification.

### 7.2 Remort Class Names
**Source:** `classes.cs` (KoK)
- **Progressive class names**: Class names change based on remort level.
- **Tiered progression**: Different names for remort ranges (1-4, 5-14, 15-29, 30-49, 50-74, 75-99, 100-124).
- **Class-specific names**: Each class has unique progression names.
- **Implementation**: Add remort-based class name lookup, name progression system.

### 7.3 World Rank System
**Source:** `classes.cs` (KoK)
- **Rank progression**: Newbie → Adventurer → Soldier → Gladiator → Star → SuperStar → Titan → Demi-God → God.
- **Rank-based benefits**: Potential for rank-based bonuses or restrictions.
- **Implementation**: Add world rank system, rank calculation, rank display.

---

## 8. ITEM SYSTEM ENHANCEMENTS

### 8.1 Accessory Type System
**Source:** `Accessory.cs` (NWVII)
- **Accessory categories**: Ring, Body, Boots, Back, Shield, Talisman, Sword, Axe, Polearm, Bludgeon, Ranged, Projectile.
- **Max accessories**: Different max counts per type (Rings = 2, others = 1).
- **Special variables**: MDEF, HP, Mana, ATK, DEF, Weight Capacity, HP regen, Mana regen.
- **Implementation**: Add accessory type system, max counts, special variable tracking.

### 8.2 Item Special Variables
**Source:** `Accessory.cs` (NWVII)
- **SpecialVar system**: Items can have multiple special effects.
- **Variable types**: MDEF, HP, Mana, ATK, DEF, Weight Capacity, HP regen, Mana regen.
- **Status effects**: Items can apply status effects on hit (`$ItemData[%weapon, svar]`).
- **Implementation**: Add special variable system, status effect application, item effect tracking.

### 8.3 Item Cost System
**Source:** `Accessory.cs` (NWVII)
- **Hardcoded costs**: `$HardcodedItemCost[%item]` for base prices.
- **Generated costs**: `GenerateItemCost(%item)` for calculated prices.
- **Cost lookup**: `GetItemCost(%item)` checks hardcoded first, then generated.
- **Implementation**: Add item cost system, cost generation, price lookup.

---

## 9. MENU SYSTEM

### 9.1 Dynamic Menu System
**Source:** `menu.cs` (NWVII)
- **Menu building**: `Client::buildMenu(%clientId, %menuTitle, %menuCode, %cancellable)`.
- **Menu items**: `Client::addMenuItem(%clientId, %option, %code)`.
- **Menu processing**: `processMenu%menuCode(%clientId, %code)` dynamic function calls.
- **Menu locking**: `%clientId.menuLock` prevents cancellation.
- **Implementation**: Add menu system, dynamic menu processing, menu state tracking.

---

## 10. STATS & ATTRIBUTES

### 10.1 Trait System
**Source:** `rpgstats.cs` (NWVII)
- **Five traits**: STR, INT, DEX, MEM, END.
- **Trait storage**: `$playerTrait[%clientId, %trait]`.
- **Trait-based calculations**: Traits affect skills, stats, and abilities.
- **Implementation**: Add trait system, trait allocation, trait-based modifiers.

### 10.2 Dynamic MaxHP System
**Source:** `rpgstats.cs` (NWVII)
- **Race-based HP**: Different races have different base HP.
- **Team balance**: HP adjusted based on team composition (Orcs vs Humans).
- **Dynamic calculation**: `(%d/2)/%h` for humans, `(%d/2)/%o` for orcs.
- **Minimum HP**: 200 minimum, 1000 default.
- **Implementation**: Add dynamic HP calculation, team balance system, race-based modifiers.

### 10.3 Overweight System
**Source:** `rpgstats.cs` (NWVII)
- **Overweight tracking**: `$ClientData[%clientId, "OverweightStep"]`.
- **MDEF penalty**: `(fetchData(%clientId, "OverweightStep") * 7.0) / 100` reduces MDEF.
- **Weight capacity**: `50 + $PlayerSkill[%clientId, $SkillWeightCapacity]` base capacity.
- **Implementation**: Add overweight tracking, stat penalties, weight capacity system.

---

## 11. ZONE & WORLD SYSTEMS

### 11.1 Zone Types
**Source:** `playerdamage.cs` (NWVII)
- **FREEFORALL zones**: PvP always enabled.
- **PROTECTED zones**: No PvP damage.
- **Zone-based rules**: Different death/EXP rules per zone type.
- **Implementation**: Add zone type system, zone-based rule enforcement.

### 11.2 Water Damage
**Source:** `playerdamage.cs` (NWVII)
- **Water detection**: LOS check for water objects.
- **Water damage amplification**: `%value *= $waterDamageAmp` for landing in water.
- **Splash sound**: Play sound when landing in water.
- **Implementation**: Add water detection, damage amplification, sound effects.

---

## 12. STATUS EFFECTS

### 12.1 Status Effect System
**Source:** `playerdamage.cs` (NWVII)
- **Status lookup list**: `$StatusLookUpList[%i]` for status types.
- **Status application**: `Status::%statusName(%dClient, %sClient, %pow)` dynamic calls.
- **Status clearing**: Statuses cleared on death (`-666` flag).
- **Weapon-based statuses**: Weapons can apply statuses via `$ItemData[%weapon, svar]`.
- **Implementation**: Add status effect system, status application, status tracking.

---

## 13. AI & BOT ENHANCEMENTS

### 13.1 Bot Death Sayings
**Source:** `playerdamage.cs` (NWVII)
- **Death sayings**: `$MasterBotDieSay[%aiName]` for bot death messages.
- **Zone chat**: Death sayings broadcast to zone.
- **Implementation**: Add bot death saying system, zone broadcasting.

---

## 14. CRAFTING SYSTEM

### 14.1 Crafting Skills
**Source:** `skills.cs` (NWVII)
- **Crafting skill**: `$SkillCrafting` for item creation.
- **Trait-based**: Affected by INT, END, MEM traits.
- **Implementation**: Add crafting system, recipe requirements, skill-based success.

---

## 15. IMPLEMENTATION PRIORITY

### High Priority (Core Gameplay)
1. **Combat Enhancements**: Critical hits, backstab, bash, hit/miss system
2. **Skill System**: Trait-based multipliers, skill restrictions
3. **Death System**: EXP loss, PKer system, enhanced lootbags
4. **Economy**: Merchant inventory, haggling, stealing

### Medium Priority (Content Expansion)
5. **Resource Gathering**: Wood cutting, farming, mining
6. **Crafting**: Cooking, item crafting
7. **Spell System**: Enhanced casting, spell types
8. **Class System**: Class multipliers, remort names

### Low Priority (Polish & Features)
9. **Menu System**: Dynamic menus
10. **Status Effects**: Status system
11. **Accessory System**: Enhanced item types
12. **Zone Enhancements**: Water damage, zone types

---

## NOTES

- All code patterns are from reviewed Tribes RPG mods (NWVII, KoK variants)
- Systems should be adapted to fit Kingdom of Kronos's existing architecture
- Some systems may conflict with existing implementations - review before integration
- Test thoroughly before deploying to production
- Consider performance impact of complex calculations

---

**Last Updated**: Based on comprehensive review of Tribes RPG mods collection
**Files Reviewed**: playerdamage.cs, skills.cs, spells.cs, economy.cs, FruitTree.cs, wood.cs, worldtree.cs, rpgstats.cs, Accessory.cs, objectives.cs, menu.cs, classes.cs



