# Kingdom of Kronos V0.7.2 - Major Changes Since V0.7.1

## TURRET SYSTEM COMPLETE REWORK
- **Turret Team Assignment Fix**: Changed all turrets from team -1 (neutral) to team 1 (enemy) to activate Tribes' native targeting system
- **Generator Power System**: Implemented proper generator-to-turret power connections, ensuring all turrets are powered and enabled
- **Turret Finding Improvements**: Enhanced recursive search to find all 25 turrets including temple turrets (previously only found 12)
- **House-Based Targeting**: TempleIndoorTurret turrets now only target opposing houses (not their own house members)
- **Turret Damage System**: Complete rework of turret damage calculation
  - Turrets now use dedicated damage formula (bypasses physical damage formula that requires ATK/skills)
  - Base damage: 100 Tribes damage units with DEF reduction and ±15% random variation
  - Level-based scaling: `baseDamage * (1 + (playerLevel / 100))` to maintain threat at all levels
  - Stance bypass: Turret damage bypasses all stances except Normal and Glass Cannon
  - Fixed turret damage being capped at 1 damage

## TAB MENU SYSTEM REDESIGN
- **Complete Tab Menu Overhaul**: All players (including admins) now use RPG menu instead of base Tribes menu
- **Command Interception**: Base Tribes commands (changeteams, observe, etc.) are intercepted and redirected to RPG menu
- **Menu System Fixes**: Fixed tab menu loading base Tribes menu options - now always opens RPG menu
- **Base Menu Disabling**: Base Tribes menu (PlayChatMenu) is permanently disabled for all players

## VEHICLE COMBAT SKILL SYSTEM
- **Replaced Archery Skill**: Renamed unused Archery skill (Skill 14) to Vehicle Combat
- **Vehicle Damage Scaling**: Scout Vehicle damage now scales with Vehicle Combat skill
  - +1 damage per 100 skill points
  - Maximum +10 damage at 1000 skill
  - Only applies to players (not AI bots)
- **Equal Skill Gain**: All classes now have 1.0 multiplier for Vehicle Combat skill (balanced progression)

## BELT STORAGE SYSTEM IMPROVEMENTS
- **Bank Storage Synchronization**: Fixed BeltStorage and StoredQuestItems/StoredKeyItems sync issues
- **Deposit/Withdraw Bug Fix**: Fixed critical bug where only 1 item was deposited/withdrawn instead of full count
- **Numeric Input Support**: Players can now type numbers (1-100) in chat to deposit/withdraw specific amounts
- **Empty Storage Fix**: Fixed bug where empty storage wasn't being saved properly (export() skips empty strings)
- **Flag Save Prevention**: Flags are never saved to character data (prevented item loss on disconnect/crash)

## DAMAGE MESSAGE SYSTEM REDESIGN
- **Bottomprint with Alignment**: All damage messages now use bottomprint with left/right alignment
  - Damage dealt: Left-aligned (`<jl>` tag)
  - Damage taken: Right-aligned (`<jr>` tag)
- **Combat Clarity**: Easier to track combat in real-time without cluttering chat window
- **Spell Damage Messages**: Spell damage messages also use bottomprint for consistency

## HOUSE SYSTEM IMPROVEMENTS
- **House Member Counts**: Objectives screen now displays real-time member counts for each house
- **Member Count Updates**: Member counts update automatically when players join/leave houses or connect/disconnect
- **House0 Prevention**: Complete fix for "House0" bug that prevented players from joining houses
  - Save function prevents "House0" from being saved
  - Load function clears "House0" on character load
  - Display functions validate and clear invalid house values
  - GuildMaster properly detects players with no house
- **House Joining Fix**: Fixed GuildMaster to correctly identify players with no house (treats "0", "House0" as empty)

## FLAG SYSTEM IMPROVEMENTS
- **Flag Drop on Disconnect**: Flags now properly drop with full logic when players disconnect/crash
- **Map Flag Reset System**: Map flags (team -1) reset to original position after 1 minute when dropped
- **Flag Drop Broadcast**: Global broadcast message when player drops a map flag that was held by a house
- **Flag Save Prevention**: Flags are never saved to character data (prevented restoration on reload)
- **House Objective Persistence**: SaveHouseObjectives() and UpdateHouseObjectivesDisplay() called after all flag events (pickup, drop, reset, placement)

## PERSISTENT CENTERPRINT SYSTEM
- **New Persistent Display Function**: Created persistentCenterprint() function that re-displays messages periodically
- **Prevents Overwriting**: Messages refresh every 1.5 seconds to prevent being overwritten by bottomprint
- **Centered Text**: All stats/info displays now use `<jc>` tag for centered text
- **Stats Display Improvements**: #getinfo, #getstats, "View your stats" menu, and #w command now use persistent centerprint
- **5-Second Duration**: Stats displays show for 5 seconds (reduced from 30 for better UX)

## REMORT SEAL IMPROVEMENTS
- **AI Bot Damage Prevention**: Seal battle bots no longer damage each other (only damage players)
- **Self-Damage Allowed**: Bots can still hurt themselves with spells (self-damage from AoE spells)
- **Outside Damage Prevention**: Players outside Colloseum cannot damage bots inside Colloseum (prevents camping/sniping)
- **Colloseum Arena Bot Protection**: Arena bots (Round1, Round2, Round3) also protected from outside damage
- **Zone Update Fix**: #helpseal command now forces immediate zone update so damage protection works immediately
- **Death Message Enhancement**: When all players die during seal attempt, shows names of all participants

## NEW WEAPONS
- **Aeco Seorei**: New spear weapon with equivalent damage (600) and speed (0.50s) of Terminus Est
  - Skill requirement: Piercing 2030, Remort 50
  - Price: $700,000,000
- **Morning Star**: New mace weapon with equivalent damage (600) of Terminus Est
  - Skill requirement: Bludgeoning 2030, Remort 50
  - Price: $700,000,000

## AI BOT IMPROVEMENTS
- **Per-Bot Movement Control**: Seal/Colloseum bots use AImoveChance = 1 (don't run away) while other bots maintain normal behavior
- **Bot-to-Bot Damage Prevention**: AI bots in seal battles don't damage each other
- **Scout Vehicle Damage Fix**: Fixed bug where scout vehicles were incorrectly dealing turret damage

## CODE OPTIMIZATION & CLEANUP
- **RemoteEval Validation**: Added client validation before all remoteEval() calls to prevent "bad managerId" errors
- **Debug Output Removal**: Removed excessive debug echo statements from production code
- **Field Conflict Resolution**: Fixed funk::var field conflicts between old bank system and belt storage
- **String Function Optimization**: Removed String::trim() calls (doesn't exist in Tribes) that caused errors

## ADMIN COMMANDS
- **#getstats Command**: New command to view player/bot combat stats (DEF, MDEF, ATK, HP, MANA, etc.)
- **#addbelt Command**: Admin command to add belt items to players (Admin Level 5)
- **#getbelt Command**: Admin command to view player's belt items (Admin Level 1)
- **#getbeltstorage Command**: Admin command to view player's belt storage at banker (Admin Level 1)
- **#getinventory Command**: Admin command to view complete inventory (equipped, belt, storage) (Admin Level 1)

## GAME BALANCE
- **BlackDiamondDreamSword Drop Rate**: Changed from 100% to 1% drop rate (more rare and valuable)
- **Bane Stance Mana Prevention**: Potions and mana restoration items no longer work in MageBane/BladeBane stances
- **Glass Cannon Stance**: Renamed from "Casting" stance, increased spell damage to 2.5x, increased penalties to 50% HP/MANA reduction

## BUG FIXES
- **Quest Item Drop Logic**: Fixed issue where quest items were only stacking to 5 in enemy lootbags
- **Belt Item Drop Logic**: Fixed bug where equipped items disappeared on death if same item existed in storage
- **StoredKeyItems Cleanup**: Fixed bug where StoredKeyItems wasn't being cleaned before modification during withdrawal
- **House System NULL/0 Fixes**: Multiple fixes to prevent "House0" bug and ensure proper house joining
- **Turret Damage Fix**: Fixed turrets dealing only 1 damage (now use proper damage formula)
- **Scout Vehicle Damage Fix**: Fixed scout vehicles incorrectly dealing turret damage
- **Flag Save Prevention**: Fixed flags being saved to character data (now prevented)

