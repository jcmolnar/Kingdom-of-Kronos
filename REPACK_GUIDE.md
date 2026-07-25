# Tribes RPG Repack — release guide

How to cut a new release of the all-in-one client repack ("Jobo's Tribes RPG
Repack", lineage: phantom's community Repack 41 → r42+). Companion script:
`tools\repack\build-repack.ps1`.

## The three trees (never confuse them)

| Tree | Role |
|---|---|
| `C:\Dynamix\Tribes` | **Play/dev install.** Dirty: logs, saves, experiments, build artifacts. Content is created/tested here first. |
| `C:\Dynamix\Tribes - Repack` | **Curated master.** What ships, byte-for-byte. Content flows in from the play install BY HAND (copy only what a release needs). |
| `C:\Dynamix\Jobo's Tribes RPG Repack r<N>.zip` | **The release artifact**, produced by the script. Upload to KingdomofKronos.com/downloads. |

Canonical binary sources:
- `NativeTribes.exe` — dev repo root (`Kingdom of Kronos V0.8.1 Development\NativeTribes.exe`, itself deployed from the Tribes Native Build tree). The script hash-syncs this automatically.
- `Tribes.exe` — Lasthope 1.30, frozen since 2020. Never changes.
- Client plugins (`Plugins\*.dll`) — copied by hand when a client plugin actually changes; the script prints their dates so a stale one is visible. Client and server plugin sets differ — never bulk-copy `Plugins\` from the dev repo (those are server plugins).

## Release procedure

1. **Land content** into `C:\Dynamix\Tribes - Repack`: copy the new/changed
   assets (models, skins, vols, scripts, HUD files) from the play install.
   Copy files individually — never `robocopy /MIR` the play install over the
   master (that's how cruft and personal state get back in).
2. **Run the script** from the dev repo:
   `pwsh tools\repack\build-repack.ps1 -Version <N>`
   It refreshes NativeTribes.exe, deletes cruft (pdb/bak/logs/test artifacts,
   empties `Temp\`/`Recordings\`, removes `config\play.gui.old`), audits
   configs for personal data, checks the manifest, and zips.
3. **Fix any `[FAIL]` audit findings by hand** (the script never edits configs)
   and re-run. Typical finding: you test-played from the repack folder and the
   game wrote your player name into `config\Players.cs` or re-saved
   `ClientPrefs.cs` with favorites/banned-IP entries. Re-sanitize (see below).
4. **Bump version strings by hand**: the `(release N)` line at the top of
   `README - Tribes RPG Repack.txt`. The zip name comes from `-Version`.
5. **Smoke test** (checklist below), then upload the zip.

## Standing sanitization rules (what "clean" means)

- **Never ship**: `*.pdb`, `TribesNativeDbg.exe`, debug launchers, `cap1.bin`
  captures, `WS_FTP.LOG`/`INSTALL.LOG`, `*.bak`/`*.stockbak`, root `*.log`,
  `native_crash.txt`, non-empty `Temp\`/`Recordings\`.
- **`config\banlist.cs`, `badwords.cs` ship EMPTY** — they regenerate per
  player. A real IP in banlist.cs would auto-ban that person on every
  downstream host. **`config\Players.cs`** ships empty or with the generic
  `"test"` identity (RMRPG-repack convention); any real player name fails the
  audit.
- **`config\ClientPrefs.cs`** ships with tuned defaults but WITHOUT:
  `bannedserveriplist`, `favoriteList*`, `buddyList*`, `MSMOTD`/`MSprevMOTD`,
  debug prefs (`carDebug`, `frameProfile`). Sound volumes at 0.75 (don't ship
  your muted setup). NOTE: the game REWRITES this file on every exit — always
  sanitize ClientPrefs.cs LAST, after the game is closed, and let the script
  audit confirm it. GPU driver string + resolution are fine to ship (engine falls back;
  autoexec re-applies OpenGL ~3s after boot — the known 1.30 driver-pref fix).
- **`config\ServerPrefs.cs`**: no `$Server::Address`, blank `$Server::Password`,
  generic `HostName`.
- **`config\rmrpgserv.cs`**: `$AdminPassword[5] = "changeme"` (documented in
  the README as the default), `$extrainfo` empty.
- **`config\rpgserv.cs`**: all five `$AdminPassword[]` blank (RPG mod requires
  all five filled for admin login, so blank = disabled = safe default).
- **`config\GameServerList.cs`**: keeping the public "Kingdom of Kronos" entry
  is a feature; the TESTING entry must not ship.
- **`config\sex.cs` exemption**: its plaintext passwords are the third-party
  SEX-mod author's own upstream config — ships as-is (audit skips it).
- **Launchers**: `modlist.txt` = `-mod rpg` (NativeTribes zero-arg launch;
  args inject AHEAD of the real command line, so `-mod rmrpg` on top yields
  the rpg+rmrpg stack). Dedicated hosting always uses NativeTribes.exe — the
  Borland `Tribes.exe` loads the mem.dll plugin chain (GraphicPlugin), which
  has no GL context when `-dedicated` and crashes.
- **Keep**: phantom's original `Repack readme.txt` (history/credits),
  `EULA.txt`, `readme.txt`, `dpifix.reg`, `tools\`+`mappers\` (community
  expectation), all mod folders, empty `Temp\`+`Recordings\` (engine needs
  them — phantom r8 note).

## Smoke-test checklist (before upload)

1. `NativeTribes.exe` double-click from the repack folder → boots into RPG
   join screen, no exec errors in `console.log`, Kronos server listed.
2. `Play RPG (Stock Tribes).bat` → stock client boots into rpg mod.
3. `Play RMRPG (Native).bat` → RMRPG loads (rpg+rmrpg stack).
4. Extract the zip to a **fresh path** and repeat check 1 from there — proves
   no absolute-path assumptions (the "Dynamix - 64 Bit" install crashed on
   join for exactly this class of reason).
5. `Host RMRPG Server.bat` → dedicated server reaches "mission RMR loaded" in
   console.log without the GL-plugin crash. **Careful on the dev machine:
   live servers run here — kill test servers only by the exact PID you
   spawned, never by process name.**
6. Join the live Kingdom of Kronos server and play for a minute (HUD, chat,
   a fight). Delete the `console.log`/`Players.cs` this writes into the
   master, then re-run the script (it re-audits) before final zip.

## Versioning

Continue phantom's release numbering (41 → 42 → …). `$pref::lastRepack` inside
`ClientPrefs.cs`/scripts is engine-managed — don't hand-edit it; the README
line + zip name are the version of record.
