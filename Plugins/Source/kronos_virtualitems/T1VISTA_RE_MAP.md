# T1Vista.exe RE map — datablock subsystem (for kronos_virtualitems.dll)

Target binary: **T1Vista.exe** (the LIVE server exe). 32-bit PE, image base
`0x400000`, no ASLR. `.text` 0x401000–0x60E000, `.data` 0x60E000+.
NOTE: NativeTribes.exe / NativeTribes.pdb are a DIFFERENT binary (modern MSVC
rebuild) — their symbols do NOT map to T1Vista addresses. T1Vista is
Borland-compiled and unsymbolized; every address below was recovered by static
analysis (pefile + capstone) anchored on string literals.

## CONFIRMED (verified by disassembly of DataBlockManager::processEvent @ ~0x403290)

| Symbol | Address | How confirmed |
|---|---|---|
| `Console` global (obj ptr) | `0x006583c4` | `mov eax,[0x6583c4]; push eax; call executef` in processEvent |
| `Console->executef(argc,fn,...)` vararg | `0x005f42bc` | called for `dataGotBlock`/`dataFinished` in processEvent |
| `dataNames[31]` string-ptr table | `0x0060ed44` | `mov ecx,[edx*4 + 0x60ed44]` (edx = evt->group); 31 valid ptrs |
| `DataBlockManager::processEvent` | ~`0x00403290`..`0x00403325` | matches source 1:1 (dataGotBlock/dataFinished/group==30) |
| `DataBlockManager::lookupDataBlock` | `0x00403328` | returns dataBlocks[group][block] |
| str "dataGotBlock" | `0x0060eede` | |
| str "dataFinished" | `0x0060eeeb` | |
| str "DataBlockEvent" (x2) | `0x004034b0`,`0x00403658` | class-name literals in the DBM code cluster |

### DataBlockEvent field offsets (from processEvent reads)
- `+0x20` = group (compared to 0x1e = NumDataTypes-1)
- `+0x24` = gsize
- `+0x28` = block   (source order: group, gsize, block; see pack())
- `+?`    = data ptr (GameBaseData*) — not yet located; unpack sets it

### DataBlockManager instance layout (from lookupDataBlock @ 0x403328)
- `this + 0x1064 + group*0x10` = dataBlocks[group].size (Vector count)
- `this + 0x1070 + group*0x10` = dataBlocks[group] element array ptr
- so each `Vector<GameBaseData*>` slot is 0x10 bytes, base `this+0x1060`

### Already known (from kronos_datetime.dll, same binary)
- `addCommand` (StringCallback reg) = `0x005f4138`
- plugin console vtable region: 0x5f3ddc / 0x5f3f10 / 0x5f3ff8 / 0x5f40d8 /
  0x5f4138 / 0x5f41a8 / 0x5f450c

## CONFIRMED round 2 (from getNumItems/getItemData handlers)

Handler addresses (from addCommand reg block @ 0x423660+):
- `getItemData` handler = `0x0041e554`
- `getNumItems` handler = `0x0041e5cc`

**sg.dbm access path (THE KEY):**
```
mov edx, [0x006a8494]     ; edx = *(sg-globals-ptr global)
mov ecx, [edx + 0x10]     ; ecx = sg.dbm  (DataBlockManager instance)
mov eax, [ecx + 0x10b4]   ; dataBlocks[Item=5].size()  == getNumItems()
```
- `SG_GLOBALS_PP = 0x006a8494` (global holding the sg-globals object ptr)
- `sg.dbm = *(*(0x006a8494) + 0x10)`
- item count @ dbm + 0x10b4  (== dataBlocks[5].size; 5 = ItemDataType)
- **`DataBlockManager::lookupDataBlock` = `0x00403350`** — Borland regcall:
  EAX=this(dbm), ECX=group, EDX=blockId → returns GameBaseData* (ItemData*) in EAX.
  getItemData does: `eax=dbm; ecx=5; edx=idx; call 0x403350`.
- `intToStr` = `0x0041be04`
- a Console print/usage helper = `0x005f3ddc`

Probe now has (all READ-ONLY, SEH-guarded): `vslotProbe()` (dataNames),
`vslotDbm()` (resolve+report sg.dbm+item count), `vslotPeek(hexAddr[,count])`
(general dword dump). Use these to confirm addresses empirically on the server.

## CONFIRMED round 3 — THE FULL PUSH MECHANISM (from sendDataToClient @ 0x402c48)

**dbm global (direct, one deref):** `dbm = *(0x006a8428)`  (== sg+0x10; the
engine reads it here at the sendDataToClient call site). Runtime value seen:
0x032ca328. (vslotDbm's 2-deref via 0x6a8494 gives the same.)

**`DataBlockManager::sendDataToClient` = `0x00402c48`** (virtual in the vtable,
but the impl address is directly callable). Borland regcall:
- **EAX = dbm (this)** , **EDX = clientManagerId**
- Re-sends ALL 31 datablock groups to that one client (the exact connect path).
- Body per block: `new DataBlockEvent` → set fields → postCurrentEvent.

**DataBlockEvent:** size = **0x30**, ctor = **`0x00402960`** (EAX=this). Fields:
- `+0x18` address.managerId
- `+0x20` group      `+0x24` gsize      `+0x28` block (255/0xff = empty sentinel)
- `+0x2c` data (GameBaseData* / ItemData* — or a clone to customize per client)

**postCurrentEvent (virtual):** poster = `*(dbm + 0x44)`; call
`(*poster)[+0x54]` regcall EAX=poster, EDX=evt. (i.e. `[[dbm+0x44]] + 0x54`.)

**operator new** = `0x004887c4` (push size, cdecl, returns ptr in EAX).

**managerId source:** at the call site, `managerId = *(connectionObj + 0xc)`.
Per source, sendDataToClient is called with `ClientRep.id` (BaseRep.id, the
FIRST logical id field). This is the SAME id space as the Kronos script
`%clientId` (2049–2175 — see memory ai-clientid-three-actor-architecture /
baserep-id-2048-is-server-slot). STRONG hypothesis: **push managerId == script
%clientId**. MUST confirm at runtime before trusting for a targeted push
(wrong id = event dropped, low risk, but verify).

**ItemData layout (item[0] @ 0x0400ee88):** vtable `+0x00` = 0x0061af48;
`+0x04` and `+0x20` = name StringTableEntry (same ptr, 0x032a6a44). Precise
description/price/hudIcon/showInventory offsets still to be pinned from the peek
+ source pack order — BUT for v0 the description can be changed in SCRIPT
(getItemData(idx).description="X") before the push, so field offsets are a
Phase-D (per-client clone) concern, not needed for the go/no-go.

## Two viable pushes (both fully RE'd)
- **A (simplest, heaviest):** call sendDataToClient(dbm, mgrId) — re-sends ALL
  blocks (connect path). Safest/most-proven; may cause a brief client reload.
- **B (surgical):** craft ONE DataBlockEvent for the Item group at [idx] and
  postCurrentEvent — updates just that slot, one dataGotBlock callback. Less
  disruptive; the production per-client path.

## Go/no-go test (do on a LOCAL/DEV server first)
Script: `getItemData(<idx>).description = "TESTXYZ";`
Then DLL push to your own %clientId. Open inventory → row shows "TESTXYZ" live,
no disconnect = GO. Wrong managerId → event dropped (harmless). A wrong ADDRESS
(not managerId) is the only crash risk — all addresses above are cross-checked,
but validate on dev.

## Address confidence — RESOLVED by full-binary decomp (2026-07-11)

The earlier "this can't be finished blind" caveat is **retired**. The whole
binary is now decompiled at `T1Vista_RE_Kit/RE/`, and every address/offset the
push path uses was cross-checked against the raw Ghidra decomp of
`DataBlockManager_sendDataToClient @0x00402c48`
(`RE/ghidra/decomp/page_00400000.c`). The DLL's `vslotPushItem` asm is a
BYTE-FOR-BYTE replica of one iteration of that function's inner loop:

| DLL constant | value | decomp evidence (sendDataToClient) |
|---|---|---|
| `FN_SENDDATA` | 0x00402c48 | function entry |
| `FN_EVTCTOR`  | 0x00402960 | `DataBlockEvent_ctor(iVar1)` |
| `FN_OPNEW`    | 0x004887c4 | `operator_new()` |
| `EVT_DATA`  0x2c | `*(iVar1+0x2c)=*(local_14[3]+iVar2*4)` (array[block]) |
| `EVT_GROUP` 0x20 | `*(iVar1+0x20)=iVar3` (group idx) |
| `EVT_GSIZE` 0x24 | `*(iVar1+0x24)=*local_14` (dataBlocks[g].size) |
| `EVT_BLOCK` 0x28 | `*(iVar1+0x28)=iVar2` (block idx) |
| `EVT_MGRID` 0x18 | `*(iVar1+0x18)=param_2` (managerId) |
| poster/vtbl 0x44/0x54 | `(**(code**)(**(int**)(param_1+0x44)+0x54))(*(int**)(param_1+0x44),iVar1)` |
| `DBM_ITEMARR_OFF` 0x10c0 | `local_14[3]` = dbm+0x1064+5*0x10+0xc = dbm+0x10c0 |
| `DBM_ITEMSIZE_OFF` 0x10b4 | `*local_14` = dbm+0x1064+5*0x10 = dbm+0x10b4 |

`DBM_GLOBAL 0x006a8428` (dbm, one deref) is `[confirmed:runtime]` in the map
base; the registration ABI (`addCommand 0x005f4138`, the plugin vtable region)
is battle-tested — kronos_datetime.dll uses the identical bytes and runs live.

**Net:** a wrong-managerId push is dropped harmlessly; there is no longer a
wrong-ADDRESS crash risk to fear — the addresses are decomp-verified. The only
remaining unknown is EMPIRICAL client behavior (does a re-pushed ItemData's
renamed row render in the stock inventory, and does the client survive the
`data->preload` on receipt) — that is exactly what the go/no-go run answers, and
it cannot crash the SERVER regardless of the outcome.

Reference (read-only, same source, different addresses): NativeTribes.pdb.
Precedent for per-client event posting in this exe: PlayerManager.cpp
(PlayerSkinEvent/MissionResetEvent — sg.playerManager + postCurrentEvent).

---

## ADDENDUM 2026-07-10 — full-binary RE pass (see T1Vista_RE_Kit/RE/MASTER_RE_MAP.md)

A 5-agent static RE pass over the whole binary (Ghidra: 11,161 fns exported to
T1Vista_RE_Kit/RE/ghidra/; per-subsystem maps in RE/domains/) extended and
CORRECTED parts of the map above. Decomp-adjudicated corrections relevant here:

- **lookupDataBlock disambiguation** (this file previously conflated them):
  - `0x00403328` returns the **object** `dataBlocks[g][b]` = *(*(dbm+0x1070+g*0x10)+b*4).
    THIS is the one to use to fetch an ItemData* by index.
  - `0x00403350` returns the **name** (object+4). getItemData() calls THIS and
    returns a name string — which is why 0x403350 "worked" as a char* return.
  Borland regcall EAX=dbm, ECX=group, EDX=block for both.
- `processEvent` real entry = **0x004031ac** (0x403290 is mid-body).
- **ItemData wire offsets now pinned** (ctor 0x4509a8 / pack 0x450aa8 / unpack
  0x450c20): imageId+0x100, price+0x104, hudIcon+0x11c, typeString+0x120,
  showInventory+0x124 (byte), showWeaponBar+0x125, hiliteOnActive+0x126. These are
  the per-client-clone fields the belt-item push (Phase D) needs — [static:unverified].
- createDataBlock type→ctor table @ 0x00402d1c (ItemData=type5→0x4509a8).
- findClient(managerId→ClientRep) = 0x0040de80: index = managerId − 0x800;
  ClientRep sizeof 0x1E0, id@+0xc (== managerId == %clientId). Confirms the
  managerId==%clientId hypothesis structurally.

All addenda are [static:unverified] (binary not executed this pass). Confirm with
vslotPeek on DEV before writing. Master index: T1Vista_RE_Kit/RE/MASTER_RE_MAP.md.

---

## PHASE E — per-weapon swing speed (hook site RE'd, NOT auto-installed)

Goal: two belt weapons sharing one shell shape can swing at DIFFERENT speeds.
Today a belt weapon inherits its shell's `fireTime` (correct per-SHAPE, so this
is a nicety, not a correctness gap). Per-weapon speed needs a server hook.

**Hook target (from RE/domains/C_player_combat.md §1):**
- `Player::setImageState` = **0x0041aa40** (the weapon/image state machine core).
- It stores the fire delay into the player's mounted-image slot:
  `slot+0x18 (delayTime, float) = *(ItemImageData + 0x60) (fireTime)`.
- Image slot layout: `itemImageList[8] @ player+0x1678`, stride `0x394`;
  `delayTime` at `+0x18` within a slot; `imageId` at `+0x08`; `state` at `+0x00`.
- `getItemImageData(imageId)` = **0x00419754** → ItemImageData* (`+0x60`=fireTime,
  `+0x54`=fireMode 1=Spinning/2=Sustained, `+0xa0`=fire sfx/seq).

**Safe implementation path (do this ONLY with a live DEV server to test):**
1. Store a per-player swing scale in the DLL (`vslotSwingScale(clientId, pct)`),
   keyed by `managerId − 0x800` (the ClientRep index, findClient=0x0040de80).
2. Trampoline-hook the `slot+0x18 = imgData+0x60` store in setImageState: after
   the engine writes delayTime, multiply it by the wielder's scale. Recover the
   wielder from the player `this` (setImageState's EAX). A 5-byte relative-JMP
   detour with a byte-accurate trampoline is required — pull the exact prologue/
   store-site bytes from `RE/func_batches/` for 0x0041aa40 before patching.
3. `BeltWeapon::Equip` sets the scale = `$WeaponDelay[belt] / GetDelay(shell)`;
   clears (→1.0) on unequip/death. Observers are unaffected (server-authoritative
   fire timing; first-person prediction optionally synced by also pushing the
   shell's ItemImageData to the wielder — group 6 — with the logical fireTime).

**Why not installed at load:** a mid-function code patch on the Borland binary
must be byte-verified against a running server (a wrong trampoline = crash). The
push-datablock path (Phases C/D) needs NO code patching — it only posts events —
so it ships first. Gate any Phase E patch behind an explicit console command
(e.g. `vslotSwingHookInstall`) that is never called until the DEV test passes;
DLL load must remain non-mutating.
