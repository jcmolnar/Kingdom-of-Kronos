# 🏰 KINGDOM OF KRONOS V0.8.5 UPDATE

---

## 🤖 AI SYSTEM OVERHAUL - COMPLETE!

**Townbots, enemy bots, and players are now properly co-existing!**

- Townbots can now wear cool armor and equip weapons without breaking player gameplay
- Townbots and enemy bots can now path and move around - Still to be implemented
- Fixed all ghost bot issues, spawn leaks, and server crashes
- Fixed `$numAI` counter properly tracking live bots (no more spawn blocking)
- Massive stability improvements - server no longer crashes and bots don't stop spawning

---

## 🎰 LAS VEGAS GAMBLING DISTRICT - RE-ADDED!

**EXP gambling removed, COINS only now - 11 new games across 4 NPCs:**

| NPC | Games |
|-----|-------|
| **Card Shark Charlie** | Blackjack, High-Low, Dice Duel, Streak Challenge |
| **Quick Quinn** | Coin Flip, Rock Paper Scissors, Number Guess |
| **Wheel Master Wendy** | Wheel of Fortune, Roulette, Kronos Slots (3x3, 5 pay lines!) |
| **Bag Picker** | Bag Pick (with fixes - bet deducted upfront, proper payouts) |

---

## ⚔️ DUAL WIELDING SYSTEM

**NOW FULLY FUNCTIONAL!** SOON TO BE IMPLEMENTED

- Off-hand weapons now deal damage
- Fixed weapon animations for dual wielding
- Improved weapon mount positioning
- Works with: Swords, Daggers, Axes, Maces, Hammers, Spears, Tridents, Picks

---

## 🌌 NEW CONTENT

### New Zone
- **The Void** - New endgame zone

### New Weapons
- **WhiteDiamondVoid Cutter**
- **WhiteDiamondVoid Impaler**
- **WhiteDiamondVoid Crusher**

### New Items
- **AdminBoots** - Fast as fuck boi!
- **Virus Fragments** - Added to Admin's Demise drops
- **Gian Echoes Quest Bot** - New quest NPC

### Balance Changes
- **Angels Enigma** - Nerfed to be more in line between zones
- **Remort Seal Bots** - Now attack based on % of player health and scale with remort level (much easier to balance across ALL seal remorts)

---

## 🛡️ GAMEPLAY IMPROVEMENTS

### Lootbag Aggregation
- Bots no longer pick up packs (reduces lost loot on server crashes)
- Packs within 10 units automatically merge into a single pack
- (About the height/distance of 2 players standing on each other)

### AFK Detection
- Over-leveled players hogging zones while AFK are now teleported to a safe spot
- Cast your hearts out into the void!

### Spell System
- Can't cast defensive spells on enemy bots anymore

### Belt Integration
- Belt system now stable
- Accessories to soon be added to free up slots for more weapons/armor

---

## 🔧 PERFORMANCE & STABILITY

### Server Stability
- Fixed all spawnpoints checking every 15 seconds → now only checks zones with players
- Fixed server ticking for initsoundpoints and haze every 5-10 seconds (thanks @basencrypt)
- Removed tons of bloat by adding helper functions
- Massive code refactoring (5000+ lines reduced)

### Technical Improvements
- 10-second zone spawn delay prevents pass-through spawns
- Ghost bot detection and cleanup system
- Watchdog system for infinite loop detection
- Full spawn telemetry tracking
- New admin commands: `#fullbotscan`, `#spawntelemetry`

---

## 📊 BY THE NUMBERS

- **72 commits** of work
- **5000+ lines** of code reduced through refactoring
- **15+ critical bugs** fixed
- **11 gambling games** added
- **4 new NPCs** (3 gambling + 1 quest)
- **3 new weapons** added
- **1 new zone** added

---

**This update was focused on making AI bots stable - and it finally works!**

*Probably a bunch of other shit I'm missing but the AI system was the main focus. If you find any bugs, let me know!*

---

**Version**: Kingdom of Kronos V0.8.5  
**Released**: December 2025
