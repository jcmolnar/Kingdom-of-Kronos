# Live Server Deploy Checklist (dev V0.8.1 -> live)

Live server: `C:\Users\Joe\Desktop\Kingdom of Kronos V0.8 BOV Plugins\rpg\scripts`

## 0. Diff results vs live (2026-07-12)
- 43 scripts differ, 5 exist only in dev (AutoRestart, BeltWeapons, Estate,
  RemoteAdminHelpers, VirtualSlots). Of these, ONLY RemoteAdminHelpers goes live as an
  active feature (2026-07-13 decision); the rest are untested/unfinished dev features:
  - **AutoRestart** — EXCLUDED from the package; its exec + InitAutoRestart() are
    commented in the sanitized Server.cs (it would schedule a live restart at ~60h).
  - **BeltWeapons / VirtualSlots / Estate** — the FILES still ship (many live scripts
    call their functions: BeltWeapon::OnDeath on every death, VSlot::Sync/Init,
    #estate in comchat, Estate::Init in gameevents; missing files = console-error
    spam), but they are inert on live:
    - sanitized Server.cs sets `$pref::VSlotsEnabled = false` and
      `$pref::EstatesEnabled = false` (dev Server.cs sets both true);
    - sanitized BeltWeapons.cs has ALL registrations commented (9 KOKBATCH shields +
      the 2 test items) — zero belt items exist, every hook no-ops;
    - Estate.cs got a master gate (default OFF): #estate commands answer "not
      available", no upkeep loop, no rehydration.
- Live hotfixes newer than dev were MERGED BACK into dev on 2026-07-12:
  Client.cs ($ServerShuttingDown/MainWindow guards in onConnectionError/onConnection)
  and Help.cs ("The Void" dungeon rename). No other live file was newer than dev.
- 8 live-only scripts (`Ai - Copy.cs`, Colloseum.cs, HumanArmors2.cs, comchat_clean.cs,
  housebonus-gameevents.cs, spells1.cs, testMagicMissile.cs, test_droprates.cs) are NOT
  exec'd by live Server.cs — dead files; leave or delete, they don't matter.
- Dev Server.cs deliberately disables exec(hackfix) (documented at Server.cs:~323);
  deploying dev Server.cs turns hackfix off on live — intended.
- Dev Server.cs execs RemoteConsole, which lives in dev `config\RemoteConsole.cs` —
  copy `config\RemoteConsole.cs` to live's config AND create its
  `RemoteConsole.local.cs` secret there (see the .example), or the exec logs a
  harmless missing-file error and rcon stays off.

The dev script tree is self-consistent and load-order is driven by `rpg/scripts/Server.cs`
(exec list at :254+). **Never cherry-pick individual .cs files** — 43 scripts differ
between dev and live and they call into each other (e.g. today's lootbag fix spans
item.cs + itemevents.cs + weapons.cs + rpgfunk.cs + playerdamage.cs). The deploy unit is
the whole scripts folder, replaced atomically. Live already has Ascension, DailyQuest,
WeeklyBoss, Banking, AutoSkill, KronosHUD_Server, etc. — only the 5 scripts in section 0
are new to live.

## 1. Back up live first
- Rename live `RPG\scripts` -> `RPG\scripts.bak-YYYYMMDD` (instant rollback path).
- Do NOT touch `temp\` under the Tribes root — ALL player character files
  (`temp\<name>.cs`), world saves (`temp\<mission>_worldsave_.cs`), and ServerTime live
  there. Replacing scripts cannot affect saves.

## 2. Scripts (the atomic unit)
- Copy the entire packaged `scripts\` folder (157 .cs files, top-level only — excludes
  stale/, analysis/, backup/, docs) over live `RPG\scripts`.
- Case-rename note: live has `Ai.cs`/`charfunk.cs` etc.; dev names may differ only by
  case. Windows FS is case-insensitive so a full replace handles this — but if you copy
  into the old folder instead of replacing it, delete leftovers that no dev file matches
  (e.g. old `bottalk.cs`, `compass.cs`, `CustomChatHUD.cs` if truly unused) ONLY after
  confirming nothing execs them.

## 3. Non-script dependencies of the new scripts
- **Plugins (Tribes root `Plugins\`)** — new features hard-require these on live:
  - `kronos_datetime.dll` — DailyQuest/WeeklyBoss day source
  - `kronosfix_server.dll` (updated)
  - playermanager / aiteardown / other kronos_*.dll currently loaded by dev
    `Plugins/Scripts/PluginLoader.cs` — copy PluginLoader.cs together with its DLL set
    (they are a matched pair; a loader referencing a missing DLL logs errors at boot).
- **Models/assets: DO NOT COPY.** The changed .dts/.vol/skins in dev (Axe, BattleAxe,
  broadsword, elfinblade, fedmonster, rpgmalehuman, zombie, test, wood.vol,
  Skins\zombie.bmp) are redone-model TEST assets — datablocks referencing them crash
  vanilla clients that lack the new models (vanilla-compat mandate). The deploy package
  instead uses the SANITIZED files (see `sanitized of new weapons\`, refreshed
  2026-07-13 from current dev):
  - `Accessory.cs`, `newstuff.cs`, `shopping.cs` — KOKBATCH new-weapon blocks stripped;
    regenerated so they now include the Jul 12 review fixes (#25 belt-item guard,
    #66 AddPoints perf) the old Jul 5 sanitized copies were missing.
  - `BeltWeapons.cs` — NEW sanitized variant: the 9 KOKBATCH belt-shield
    registrations (CathedralAegis...StarlightEye) are commented out because their
    visual ItemData only exists in the stripped KOKBATCH blocks. Test items
    (TestBeltBlade/TestBeltMail) use stock assets and stay enabled.
  - `MISSIONS\KingdomKronos.mis` — sanitized SHOP list (no indexes 246-291) and live
    hazeDistance 817.5 (dev's 1500 was a test value).
  When the new models ship to clients someday, deploy dev's unsanitized versions of
  these 4 scripts + the mission + the .dts set together as one unit.
- **Do NOT copy** `config\ServerPrefs.cs` / `banlist.cs` blindly — live has its own
  prefs/banlist; diff and merge intentionally.

## 4. Restart + verify
- Full server restart (exec-time state; hot-exec of 150 files is not safe).
- Watch `console.log` boot for: `Unable to find function`, `Unknown command`,
  `exec: file not found`, plugin load failures.
- Smoke test: connect, die with items -> #trackpack shows pack; pack survives >5 min and
  only owner can lift it (this deploy includes the popToken fix + permanent pack lock);
  kill a bot -> lootbag drops/merges; #daily works (proves kronos_datetime.dll present).
- Confirm a world save cycle runs clean (`[LOOTBAG AGGREGATE]` + worldsave lines).

## 5. Rollback
- Stop server, delete new `scripts`, rename `scripts.bak-YYYYMMDD` back, restore old
  plugin DLLs, restart.
