# Remote Web Admin Console — Runbook

A website you host from home that gives you full server-console control from anywhere:
restore items, restart the server, run any command you could type at the physical console.

```
Internet
  → Cloudflare (TLS) + Cloudflare Access (email OTP + MFA)        [outer gate]
    → cloudflared tunnel  (existing "tribes" tunnel)
      → admin Node app  @ 127.0.0.1:8100   (app login + Access-JWT check)
        → TCP telnet     @ 127.0.0.1:28001  ($TelnetPassword)
          → console->evaluate()  = full TorqueScript on the live server
```

> Status: deployed and working. Telnet rcon on **28001** (shares the number with the
> game's UDP port — different protocol, no conflict). Cloudflare Access is live on
> `spring-snow-9e4f.cloudflareaccess.com`; all four boot tasks are registered and running.

Two repos, same PC:
- **Game server** — `Kingdom of Kronos V0.8.1 Development` (this folder): enables the telnet rcon.
- **Web layer** — `Tribes Browser Based`: the Node admin app + cloudflared tunnel.

---

## What was added

**Game server (this repo)**
- `config/RemoteConsole.cs` — arms the engine telnet rcon on port 28001 (exec'd from `rpg/scripts/Server.cs`).
  Loads the secret with `exec("RemoteConsole.local.cs")` — a **basename**, because the engine's `exec`
  resolves scripts through the ResourceManager, not by raw path (an explicit `config\…` subpath fails as
  "invalid script file", which silently leaves the password unset → an open, empty-password console).
- `config/RemoteConsole.local.cs` — the telnet password (**gitignored**). Template: `.local.cs.example`.
- `rpg/scripts/RemoteAdminHelpers.cs` — `AdminListClients`, `AdminListItems`, and button wrappers
  (`AdminRestart`, `AdminGiveStuff`, `AdminKick`, `AdminSetLevel`, `AdminAnnounce`).

**Web layer (`Tribes Browser Based`)**
- `test/admin/server.js` — the admin app (localhost:8100, login + Access check + telnet bridge, audit log).
- `test/admin/config.local.js` — secrets (**gitignored**). Template: `.local.js.example`. Helper: `set-password.js`.
- `relay/cloudflared/config.yml` — `admin.kingdomofkronos.com` → `http://localhost:8100`.
- `relay/cloudflared/setup-boot-persistence.ps1` — adds the `TribesAdmin` boot task (alongside
  `TribesRelay` / `TribesStatic` / `TribesTunnel`).

Login: user `admin`, password already changed from the generated default (stored hashed in
`config.local.js`). Re-set it any time with `set-password.js` (below).

---

## One-time setup

### 1. Set matching telnet password + port
Confirm both files agree (a mismatch on either is the usual cause of failures):
- `config/RemoteConsole.local.cs` → `$TelnetPassword`  ==  `config.local.js` → `TELNET_PASSWORD`
- `config/RemoteConsole.cs` → `$TelnetPort` (28001)     ==  `config.local.js` → `TELNET_PORT` (28001)

### 2. Change the admin login password
```
cd "C:\Users\Joe\Desktop\Tribes Browser Based"
node test\admin\set-password.js "your-new-password"
```
Paste the printed `PASS_SALT` / `PASS_HASH` into `test/admin/config.local.js`.

### 3. Firewall the telnet port to localhost (run once, elevated)
The engine binds the telnet listener to all interfaces, so block it from the LAN. The tunnel
never exposes it (cloudflared only routes hostnames in config.yml); this closes the LAN path.
```powershell
New-NetFirewallRule -DisplayName "Kronos rcon localhost only (block LAN)" -Direction Inbound `
  -Protocol TCP -LocalPort 28001 -RemoteAddress LocalSubnet -Action Block
```
(Loopback 127.0.0.1 is exempt from the LocalSubnet match, so the local admin app still connects.
This blocks only TCP 28001 — the game's UDP 28001 player traffic is untouched.) **[done]**

### 4. Create the DNS route + Cloudflare Access application  **[done]**
```
"C:\Users\Joe\Downloads\cloudflared.exe" tunnel route dns tribes admin.kingdomofkronos.com
```
Then in the Cloudflare **Zero Trust** dashboard:
1. **Access → Applications → Add → Self-hosted.** Application domain: `admin.kingdomofkronos.com`.
2. **Policy:** Action *Allow*, Include *Emails → your email* (add a second identity method / MFA as desired).
   If you hit "That account does not have access," the policy's email doesn't match the one you sign in with.
3. Save. Open the application's **Overview** and copy its **Application Audience (AUD) tag**.
4. Find your **team domain** under Zero Trust → Settings (this deployment: `spring-snow-9e4f.cloudflareaccess.com`).

Both are set in `test/admin/config.local.js`, with the outer gate on:
```js
REQUIRE_CF_ACCESS: true,
CF_TEAM_DOMAIN: 'spring-snow-9e4f.cloudflareaccess.com',
CF_ACCESS_AUD:  '<the AUD tag — in config.local.js>',
```

### 5. Register + start the boot tasks (run once, elevated)  **[done]**
`setup-boot-persistence.ps1` registers **four** SYSTEM at-boot tasks (`TribesRelay`, `TribesStatic`,
`TribesAdmin`, `TribesTunnel`), each auto-restarting. It only registers — it does not start them.
```
powershell -ExecutionPolicy Bypass -File "C:\Users\Joe\Desktop\Tribes Browser Based\relay\cloudflared\setup-boot-persistence.ps1"
```
To activate without a reboot, first free the ports held by any manually-started copies, then start the
tasks (this briefly blips the live site while the tunnel restarts):
```powershell
Get-Process cloudflared -EA SilentlyContinue | Stop-Process -Force
foreach($p in 8099,9000,9001,8100){ (Get-NetTCPConnection -LocalPort $p -State Listen -EA SilentlyContinue).OwningProcess | Select -Unique | %{ Stop-Process -Id $_ -Force -EA SilentlyContinue } }
Get-ScheduledTask Tribes* | Start-ScheduledTask
```
`Get-ScheduledTask Tribes* | Get-ScheduledTaskInfo | Select TaskName,LastTaskResult` should show
`267009` (0x41301 = "currently running") for all four. Otherwise just **reboot** — the tasks start clean.

---

## Running it

- **Game server:** start as usual (`server.bat` / watchdog). On boot it prints
  `[RCON] Telnet remote console armed on 127.0.0.1:28001`. If instead you see
  `telnet console DISABLED`, the `.local.cs` secret file is missing.
- **Admin app:** runs as the `TribesAdmin` boot task. To restart it after a config/code change,
  elevated: `schtasks /end /tn TribesAdmin` then `schtasks /run /tn TribesAdmin` (no PID hunting).
  Manual run for local testing: `node "C:\Users\Joe\Desktop\Tribes Browser Based\test\admin\server.js"`.
  Then browse `https://admin.kingdomofkronos.com`.

> Note: `config.local.js` is read once at startup (Node `require` caches it), so any edit —
> telnet port/password, password hash, Access fields — needs an admin-app restart to take effect.

---

## Security model

- **Two independent walls:** Cloudflare Access (pre-origin, MFA) + app session login. The app
  verifies the `Cf-Access-Jwt-Assertion` header against your team's JWKS on every request/WS,
  so anything that skipped Access is rejected at the origin too.
- **Nothing extra on the internet:** admin app and telnet both bind `127.0.0.1`; only the
  authenticated tunnel reaches the app; telnet is also firewalled off the LAN.
- **Secrets never committed:** telnet password and app password/hash live in gitignored files.
- **Separate process** from the public `serve.js`, on a separate hostname.
- **Audit trail:** every command is logged to `test/admin/audit.log` (timestamp, identity, command).
- **Login lockout:** 5 failed logins → 15-minute lockout.

Rotate the telnet password by editing both `.local` files and restarting the server + admin app.

---

## Migrating to the live server folder

Copy to the live game-server folder:
- `config/RemoteConsole.cs`, `config/RemoteConsole.local.cs`
- `rpg/scripts/RemoteAdminHelpers.cs`
- the two `exec(...)` lines added to `rpg/scripts/Server.cs`
- add `config/RemoteConsole.local.cs` to that folder's `.gitignore`

Then on the live host:
- apply the localhost-only firewall rule (step 3) for the live server's telnet port
- if the live server uses a different `$TelnetPort`, update `TELNET_PORT` in `config.local.js` to match
  and restart the admin app

The web app + tunnel already run on this PC and are folder-independent.

---

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| App logs `telnet console DISABLED` | `config/RemoteConsole.local.cs` missing on the game server. |
| Console shows `[bridge] telnet error: ECONNREFUSED` on the wrong port | Admin app running with a stale `config.local.js` (Node caches it) — restart `TribesAdmin`. Or `$TelnetPort` not armed / port mismatch. |
| Telnet accepts an **empty** password (and rejects the real one) | The secret file didn't load, so `$TelnetPassword` is unset. Check console.log for `exec: invalid script file` — the `exec` must use the **basename** (`RemoteConsole.local.cs`), not a `config\…` path. Restart the game server after fixing. |
| Helper functions return `Unknown command` | `RemoteAdminHelpers.cs` hit a compile error (check console.log for `Syntax error` + line). Note the 1998 console has **no `?:` ternary** — use `if`/`else`. |
| `admin.kingdomofkronos.com` shows 404 / "page can't be found" after a config.yml edit | cloudflared doesn't hot-reload; restart the tunnel (or `TribesTunnel` task). Use a **quoted** config path: `Start-Process cloudflared -ArgumentList 'tunnel --config "…\config.yml" run tribes'` — an array split the spaced path and takes the site down. |
| "That account does not have access" at the Access login | The Access policy's allowed email doesn't match the identity you signed in with. Clear the Access session at `https://<team>.cloudflareaccess.com/cdn-cgi/access/logout`, then retry. |
| App exits: `REQUIRE_CF_ACCESS is on but CF_TEAM_DOMAIN…unset` | Fill in the Access fields, or set `REQUIRE_CF_ACCESS:false` for local-only testing. |
| Works locally, 403 through the tunnel | Access app AUD/team-domain wrong, or the Access policy doesn't include your email. |
