# kronos_memremap boot test (manual)

Goal: prove tribes53f.exe boots **with mem.dll** (instead of the corruption crash),
and that the shim relocated mem's hooks to our addresses.

## Steps
1. Copy the built shim into the plugin folder mem loads from:
   `Plugins\Source\kronos_memremap\kronos_memremap.dll`  ->  `C:\Dynamix\Tribes\Plugins\`
2. Delete any old log: `C:\Dynamix\Tribes\kronos_memremap.log`
3. Confirm `mem.dll` + `ploader.dll` are present in `C:\Dynamix\Tribes\` (they are).
4. Launch **`C:\Dynamix\Tribes\tribes53f.exe`** (NOT Tribes.exe). It auto-loads mem.dll.
5. Read `C:\Dynamix\Tribes\kronos_memremap.log`.

## What the log should say (success)
```
--- kronos_memremap loaded ---
memremap: restored=62 relocated(verbatim=11 jmp=2) deferred=6 of 62 sites
```
Plus: the game boots past the instant crash it used to hit with mem present.

## How to read the result
- **Log with counts + boots** = shim WORKS (proof of concept). 13 hooks now live at
  our addresses; the other 49 are restored/inert (so ~25 ABSENT-function Kronos
  features won't work yet — that's the expected follow-up: byte-match those funcs).
- **`GUARD FAIL: host is not tribes53f`** = you launched the wrong exe (stock
  Tribes.exe) — the guard correctly no-ops. Relaunch tribes53f.exe.
- **Log present but crashes shortly after** = the shim ran fine; a later crash is a
  restored-but-inert hook's feature or a deferred site. Note roughly WHERE it crashes
  (main menu? on connect? in-game?) — that tells us which hook to tackle next.
- **NO log at all + crash** = it crashed BEFORE plugins loaded (a corrupted site ran
  during early engine init, before the shim). That means the plugin-timing is too late
  and we need an earlier injection point. Note this — it's the key thing to learn.

## Safety / cleanup
- Safe for stock: if stock Tribes.exe later loads this plugin, the guard no-ops it.
- To keep the live install pristine, delete `C:\Dynamix\Tribes\Plugins\kronos_memremap.dll`
  after testing.
- This does NOT touch mem.dll, the exes, or any running process.

Report back the log contents + where (if anywhere) it crashes, and I'll take it from there.
