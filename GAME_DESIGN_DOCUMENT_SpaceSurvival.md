# Apocalypse to Orbit: Game Design Document

> **Working Title**: *Apocalypse to Orbit* (Placeholder)  
> **Genre**: Survival / Crafting / Base Building / Space Simulation  
> **Art Style**: Low-poly vector style (inspired by Valheim)  
> **Engine**: Unity (Recommended)  
> **Document Created**: December 19, 2025

---

## 📖 Game Concept

### High-Level Pitch

A **two-phase survival crafting game** where the apocalypse forces players through a dramatic transition:

1. **Phase 1 (Earth)**: Survive the apocalypse—build, craft, gather resources, and fight to stay alive
2. **Phase 2 (Space)**: Escape to a damaged orbital space sanctuary, repair it using asteroid resources, and optionally return to Earth for resupply missions

The core hook is the **shuttle transition**—packing everything you can carry into a shuttle and making a desperate escape to orbit, where an entirely new survival challenge awaits.

---

## 🎮 Core Gameplay Loop

```
┌─────────────────────────────────────────────────────────────────┐
│                         PHASE 1: EARTH                          │
├─────────────────────────────────────────────────────────────────┤
│  Survive → Gather → Craft → Build Base → Prepare for Launch    │
│                              │                                   │
│                              ▼                                   │
│                    Find & Repair Shuttle                         │
│                              │                                   │
│                              ▼                                   │
│              Pack Cargo (Limited Capacity!)                      │
│                              │                                   │
│                              ▼                                   │
│                    🚀 LAUNCH TO ORBIT 🚀                         │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        PHASE 2: SPACE                            │
├─────────────────────────────────────────────────────────────────┤
│  Repair Station → Mine Asteroids → Expand & Upgrade             │
│                              │                                   │
│                              ▼                                   │
│              Produce Fuel → Return to Earth (Optional)          │
│                              │                                   │
│                              ▼                                   │
│                 Resupply → Return to Orbit                       │
│                              │                                   │
│                              ▼                                   │
│                    ??? ENDGAME ???                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎨 Art Direction

### Style Reference: Valheim

- **Low-poly 3D** geometry with stylized textures
- **Vector-inspired** clean shapes and bold silhouettes
- **Atmospheric** lighting and fog for mood
- **Distinct biomes** with clear visual identity

### Key Visual Goals

| Element | Earth | Space |
|---------|-------|-------|
| **Palette** | Muted apocalyptic (grays, browns, sickly greens) | Cold blues, warm amber lights, stark black |
| **Atmosphere** | Dust, smoke, radiation haze | Void of space, lens flares, star fields |
| **Architecture** | Ruined buildings, makeshift shelters | Industrial sci-fi, modular station design |
| **Lighting** | Harsh sunlight, firelight | Artificial station lights, Earthglow |

---

## 🛠️ Engine Recommendation

### Why Unity?

1. **Valheim was built in Unity** — the exact engine that achieved your target aesthetic
2. **C# Scripting** — familiar language if coming from other C# projects
3. **Asset Store** — massive library of tools, shaders, and starter kits
4. **Community** — extensive tutorials for low-poly/stylized 3D
5. **Cross-Platform** — easy deployment to PC, consoles, and beyond
6. **Proven for Survival Games** — Rust, Subnautica, The Forest all use Unity

### Alternative Considerations

| Engine | Pros | Cons |
|--------|------|------|
| **Unreal Engine 5** | Powerful, great tools | Pushes toward realism, steeper curve |
| **Godot 4** | Free, lightweight, indie-friendly | 3D not as mature for large projects |

---

## 🌍 Phase 1: Earth Survival Systems

### Core Survival

| System | Description | Priority |
|--------|-------------|----------|
| **Health/Damage** | HP, damage types (radiation, physical, hunger), death/respawn | 🔴 Critical |
| **Hunger/Thirst** | Metabolic needs, food spoilage, water purification | 🔴 Critical |
| **Stamina** | Sprinting, heavy actions, regeneration | 🟡 High |
| **Temperature** | Hypothermia/hyperthermia, clothing insulation, shelter bonuses | 🟡 High |
| **Status Effects** | Poison, bleeding, radiation sickness, buffs/debuffs | 🟢 Medium |

### Crafting & Building

| System | Description | Priority |
|--------|-------------|----------|
| **Recipe System** | Crafting recipes, unlockable blueprints, workbench tiers | 🔴 Critical |
| **Resource Gathering** | Mining, logging, scavenging, tool durability | 🔴 Critical |
| **Building/Construction** | Snap-based or freeform building, structural integrity, decay | 🔴 Critical |
| **Storage** | Containers, inventory weight/slots, item stacking | 🔴 Critical |
| **Workstations** | Forge, workbench, chemistry table, cooking station | 🟡 High |

### World & Environment

| System | Description | Priority |
|--------|-------------|----------|
| **Day/Night Cycle** | Lighting, enemy behavior changes, visibility | 🔴 Critical |
| **Weather** | Rain, storms, radiation clouds, seasonal changes | 🟢 Medium |
| **Procedural/World Gen** | Biomes, POI placement, resource distribution | 🟡 High |
| **Threat Zones** | Radiation areas, enemy territories, loot scaling | 🟢 Medium |

### Combat & Enemies

| System | Description | Priority |
|--------|-------------|----------|
| **Melee Combat** | Swing timing, blocking, stamina cost | 🔴 Critical |
| **Ranged Combat** | Ammo types, projectile physics, accuracy | 🟡 High |
| **Enemy AI** | Patrol, aggro, attack patterns, fleeing | 🔴 Critical |
| **Spawning System** | Wave systems, density control, horde events | 🟡 High |

---

## 🚀 Phase 1.5: Shuttle Transition

### Shuttle Systems

| System | Description | Priority |
|--------|-------------|----------|
| **Shuttle Discovery** | Finding/unlocking shuttle location on map | 🔴 Critical |
| **Shuttle Repair** | Multi-stage repair requiring specific resources | 🔴 Critical |
| **Cargo Capacity** | Weight/slot limits for what you bring to space | 🔴 Critical |
| **Launch Sequence** | Fuel requirements, launch timing, point of no return? | 🔴 Critical |

### Design Considerations

- **The shuttle moment is your game's HOOK** — make it feel impactful
- Should launching be a point of no return, or can players return freely?
- Cargo limits force meaningful decisions: What do you bring? What do you leave?

---

## 🛸 Phase 2: Space Station Systems

### Station Management

| System | Description | Priority |
|--------|-------------|----------|
| **Power Grid** | Solar panels, generators, power distribution, brownouts | 🔴 Critical |
| **Oxygen/Life Support** | O2 generation, CO2 scrubbing, breach events | 🔴 Critical |
| **Hull Integrity** | Damage, repairs, compartmentalization | 🟡 High |
| **Module System** | Unlocking/building new station sections | 🟡 High |
| **Crew/NPCs** *(optional)* | Survivors to rescue, assign tasks, morale | 🟢 Medium |

### Space Resource Gathering

| System | Description | Priority |
|--------|-------------|----------|
| **Asteroid Mining** | EVA or drones, resource types, hazards | 🔴 Critical |
| **Orbital Mechanics** *(light)* | Asteroid approach windows, fuel cost to intercept | 🟢 Medium |
| **Salvage** | Derelict ships/satellites for parts | 🟢 Medium |
| **Processing** | Refinery, smelting, component fabrication | 🟡 High |

### Earth Return Missions

| System | Description | Priority |
|--------|-------------|----------|
| **Fuel Production** | Synthesizing fuel from space resources | 🔴 Critical |
| **Re-entry Shuttle** | Cargo limits, landing zone selection | 🟡 High |
| **Time Pressure** *(optional)* | Earth base decay while you're gone | 🟢 Medium |
| **Resupply Runs** | Strategic planning for what to bring back | 🟡 High |

---

## 🔧 Shared/Cross-Phase Systems

### Player Progression

| System | Description | Priority |
|--------|-------------|----------|
| **Skill Tree/XP** | Leveling, unlockable abilities, specializations | 🟡 High |
| **Research/Tech Tree** | Unlocking new recipes, station modules | 🟡 High |
| **Persistent Upgrades** | Things that carry over (if roguelike elements) | 🟢 Medium |

### Inventory & Items

| System | Description | Priority |
|--------|-------------|----------|
| **Inventory Management** | Grid or slot-based, weight, quick slots | 🔴 Critical |
| **Item Quality/Durability** | Wear, repair, quality tiers | 🟡 High |
| **Equipment Slots** | Armor, tools, accessories | 🟡 High |

### UI/UX Systems

| System | Description | Priority |
|--------|-------------|----------|
| **HUD** | Health/stamina bars, minimap, status icons | 🔴 Critical |
| **Menus** | Inventory, crafting, map, settings | 🔴 Critical |
| **Notifications** | Quest updates, warnings, tutorials | 🟡 High |
| **Map System** | Fog of war, markers, waypoints | 🟡 High |

### Save/Load & Persistence

| System | Description | Priority |
|--------|-------------|----------|
| **World State Serialization** | Buildings, container contents, entity positions | 🔴 Critical |
| **Player Data** | Stats, inventory, unlocks | 🔴 Critical |
| **Autosave/Manual Save** | Crash protection, save slots | 🔴 Critical |

### Audio

| System | Description | Priority |
|--------|-------------|----------|
| **Ambient Audio** | Environmental sounds, space silence vs Earth chaos | 🟡 High |
| **Music System** | Dynamic music, phase-specific themes | 🟢 Medium |
| **SFX** | Footsteps, crafting, combat, UI feedback | 🟡 High |

---

## 🏁 Endgame Ideas (TBD)

The endgame hasn't been determined yet. Here are potential directions:

### Option 1: Escape / Colonization
> Fully repair the station, gather enough resources to launch to a new planet or moon. The game ends with establishing humanity's new home.

### Option 2: Earth Reclamation
> Use the station to manufacture something that reverses the apocalypse. The game ends with returning to a healed Earth.

### Option 3: Cycle / Roguelike
> The station eventually fails, you crash back to Earth with some persistent upgrades. The loop begins again with new challenges. Each cycle pushes further.

### Option 4: Build a Fleet
> Expand from one station to multiple. Eventually build a self-sustaining space civilization. The game becomes increasingly about management at scale.

---

## 📊 Development Roadmap

### Phase 1: Foundation (Months 1-3)
```
[■■■■■■■■■■] Player Controller
[■■■■■■■■■■] Basic Inventory System
[■■■■■■■■■■] Resource Gathering (Basic)
[■■■■■■■■■■] Crafting System (Basic)
```

### Phase 2: Core Loop (Months 4-6)
```
[■■■■■■■■■■] Building System
[■■■■■■■■■■] Survival Needs (Hunger/Thirst/Health)
[■■■■■■■■■■] Day/Night Cycle
[■■■■■■■■■■] Basic Combat
```

### Phase 3: World & Enemies (Months 7-9)
```
[■■■■■■■■■■] Enemy AI
[■■■■■■■■■■] World Generation / Biomes
[■■■■■■■■■■] Workstations & Advanced Crafting
[■■■■■■■■■■] Shuttle Discovery & Repair
```

### Phase 4: Space Systems (Months 10-14)
```
[■■■■■■■■■■] Launch Sequence & Transition
[■■■■■■■■■■] Station Core Systems (Power/O2)
[■■■■■■■■■■] Asteroid Mining
[■■■■■■■■■■] Station Building/Modules
```

### Phase 5: Earth Return & Polish (Months 15-18)
```
[■■■■■■■■■■] Fuel Production & Return Missions
[■■■■■■■■■■] Tech Tree / Progression
[■■■■■■■■■■] Audio & VFX Polish
[■■■■■■■■■■] Save/Load System
```

### Phase 6: Endgame & Launch (Months 19-24)
```
[■■■■■■■■■■] Endgame Content
[■■■■■■■■■■] Balancing & Tuning
[■■■■■■■■■■] Bug Fixes & Optimization
[■■■■■■■■■■] Early Access / Release
```

> ⚠️ **Scope Warning**: This is easily 2-3 years of solo development. Prioritize ruthlessly and consider starting with a vertical slice!

---

## 💡 Design Principles

1. **Modular Architecture** — Design systems that work in both Earth and Space contexts
2. **The Shuttle is the Hook** — Make the transition feel momentous and impactful
3. **Meaningful Decisions** — Cargo limits, resource scarcity, and time pressure create tension
4. **Two Games in One** — Each phase should feel distinct but connected
5. **Prototype Early** — Test the phase transition before building out full content

---

## 📝 Open Questions

- [ ] Should the shuttle launch be a point of no return?
- [ ] How does time pass on Earth while in space? Does the base decay?
- [ ] Single-player only, or multiplayer support?
- [ ] What causes the apocalypse? (Narrative context)
- [ ] What's the final endgame goal?
- [ ] Permadeath / roguelike elements?

---

## 📚 Reference Games

| Game | What to Study |
|------|---------------|
| **Valheim** | Art style, building system, progression |
| **Subnautica** | Phase transition (surface → depths), base building |
| **The Forest** | Survival balance, crafting, enemy pressure |
| **Raft** | Resource scarcity, expansion mechanics |
| **Oxygen Not Included** | Station life support, resource loops |
| **Astroneer** | Space exploration, resource processing |
| **RimWorld** | Colony management, systems interaction |

---

*Document Version 1.0 — Initial Concept Capture*
