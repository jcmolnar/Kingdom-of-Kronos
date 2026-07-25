//============================================================================
// kronos_virtualitems.cpp — Phase C go/no-go for belt-item native integration.
//
// GOAL (production): push per-client ItemData datablock rewrites so belt items
// appear in the VANILLA stock inventory/shop screen (see belt-weapons plan).
// The engine loophole is real (TribesSource dataBlockManager.cpp:304: client
// places datablocks BY INDEX any time; item pack carries description/price/
// hudIcon/showInventory). The blocker is pure reverse-engineering of the
// UNSYMBOLIZED Borland server exe (T1Vista.exe) to the send machinery.
//
// THIS FILE IS THE **PROBE** (v0, READ-ONLY). It does NOT post any event and
// NEVER writes engine memory. It only:
//   - registers the console command  vslotProbe()
//   - reads the 31 dataNames[] string pointers at the CONFIRMED table address
//     and resolves them, returning them to the server console.
// If the console prints "Sound Profile|...|Item|...|IRC Channel", then every
// confirmed T1Vista address (below) is proven correct AT RUNTIME and the
// plugin's engine-memory read path works — the safe foundation before any
// destructive push code is written. See T1VISTA_RE_MAP.md.
//
// Run this on a LOCAL/DEV server first. It cannot destabilize anything.
//
// Registration ABI is identical to the proven kronos_datetime.dll.
//============================================================================
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <cstdlib>

// ---- CONFIRMED T1Vista.exe addresses (see T1VISTA_RE_MAP.md) ----
#define FUN_ADDCMD    0x005f4138   // addCommand (StringCallback reg)  [known]
#define DATANAMES_TBL 0x0060ed44   // dataNames[31] string-ptr table   [confirmed]
#define ADDR_CONSOLE  0x006583c4   // Console global (obj ptr)         [confirmed]
#define ADDR_EXECUTEF 0x005f42bc   // Console->executef(argc,fn,...)   [confirmed]
#define NUM_DATATYPES 31

// sg.dbm access (from getNumItems/getItemData handlers @ 0x41e5cc/0x41e554):
//   sg globals ptr = *(SG_GLOBALS_PP); DataBlockManager = sgglobals + SG_DBM_OFF
#define SG_GLOBALS_PP 0x006a8494    // global holding the sg-globals object ptr
#define SG_DBM_OFF    0x10          // sg.dbm = *(*(SG_GLOBALS_PP) + 0x10)
#define DBM_ITEMSIZE_OFF 0x10b4     // dbm + 0x10b4 = dataBlocks[Item=5].size()
#define DBM_ITEMARR_OFF  0x10c0     // dbm + 0x10c0 = dataBlocks[Item=5] array ptr
#define ITEM_GROUP    5             // ItemDataType index in dataNames[]

// ---- PUSH mechanism (from sendDataToClient @ 0x402c48; see T1VISTA_RE_MAP.md) ----
#define DBM_GLOBAL    0x006a8428    // dbm = *(0x6a8428)  (one deref)
#define FN_SENDDATA   0x00402c48    // sendDataToClient  regcall(EAX=dbm, EDX=mgrId)
#define FN_OPNEW      0x004887c4    // operator new(size) cdecl -> EAX
#define FN_EVTCTOR    0x00402960    // DataBlockEvent::ctor  regcall(EAX=this)
#define EVT_SIZE      0x30          // sizeof(DataBlockEvent)
#define EVT_MGRID     0x18          // evt.address.managerId
#define EVT_GROUP     0x20
#define EVT_GSIZE     0x24
#define EVT_BLOCK     0x28
#define EVT_DATA      0x2c
#define DBM_POSTER_OFF 0x44         // poster = *(dbm+0x44); postCurrentEvent = poster.vtbl[0x54]
#define POST_VTBL_OFF 0x54

// SEH-guarded 4-byte read (returns false on access violation - never crashes).
static bool safeRead(unsigned va, unsigned* out)
{
    __try { *out = *(volatile unsigned*)va; return true; }
    __except (EXCEPTION_EXECUTE_HANDLER) { return false; }
}

static void flog(const char* msg)
{
    FILE* f = fopen("kronos_virtualitems.log", "a"); if (!f) return;
    fprintf(f, "%s\n", msg); fclose(f);
}

// Safe-ish readable check: address is inside the mapped module image span.
// (No SEH here to keep the naked-ABI build simple; the probe only reads the
// static dataNames table + the strings it points at, all in-image.)
static bool inImage(unsigned va)
{
    return va >= 0x00400000 && va < 0x007E1000;
}

// ---- vslotProbe(): READ-ONLY. Resolve the dataNames[] table and return it. ----
extern "C" char* __cdecl c_vslotProbe(int, char**)
{
    static char out[1024];
    out[0] = 0;
    flog("--- vslotProbe: resolving dataNames[] at 0x0060ed44 ---");

    const unsigned* tbl = (const unsigned*)DATANAMES_TBL;
    int ok = 0;
    for (int i = 0; i < NUM_DATATYPES; i++)
    {
        unsigned p = tbl[i];
        const char* name = "<bad>";
        if (inImage(p))
            name = (const char*)p;
        else
        {
            char b[64]; sprintf(b, "  [%d] bad ptr 0x%08x", i, p); flog(b);
            strncat(out, "|<bad>", sizeof(out)-strlen(out)-1);
            continue;
        }
        char b[128]; sprintf(b, "  [%2d] 0x%08x -> \"%s\"", i, p, name); flog(b);
        if (i) strncat(out, "|", sizeof(out)-strlen(out)-1);
        strncat(out, name, sizeof(out)-strlen(out)-1);
        ok++;
    }
    char tail[96];
    sprintf(tail, "vslotProbe: resolved %d/%d datablock names", ok, NUM_DATATYPES);
    flog(tail);
    // Expected first/last: "Sound Profile" ... "IRC Channel".
    return out;
}

// ---- vslotDbm(): READ-ONLY. Resolve sg.dbm and report the Item datablock
//      count. Compare the returned count to getNumItems() in script: if they
//      match, the sg.dbm resolution path is CONFIRMED at runtime. ----
extern "C" char* __cdecl c_vslotDbm(int, char**)
{
    static char out[256];
    unsigned sgp = 0, dbm = 0, itemCount = 0;
    if (!safeRead(SG_GLOBALS_PP, &sgp) || !sgp) { sprintf(out, "dbm: bad sg globals ptr @ 0x%08x", SG_GLOBALS_PP); flog(out); return out; }
    if (!safeRead(sgp + SG_DBM_OFF, &dbm) || !dbm) { sprintf(out, "dbm: bad dbm ptr (sg=0x%08x)", sgp); flog(out); return out; }
    if (!safeRead(dbm + DBM_ITEMSIZE_OFF, &itemCount)) { sprintf(out, "dbm: bad item-count read (dbm=0x%08x)", dbm); flog(out); return out; }
    unsigned arr = 0, item0 = 0;
    safeRead(dbm + DBM_ITEMARR_OFF, &arr);
    if (arr) safeRead(arr, &item0);
    sprintf(out, "sg=0x%08x dbm=0x%08x items=%u itemArr=0x%08x item[0]=0x%08x  (items should == getNumItems())",
            sgp, dbm, itemCount, arr, item0);
    flog(out);
    return out;
}

// ---- vslotPeek(hexAddr [,count]): READ-ONLY dword dump. General RE tool -
//      SEH-guarded, so a bad address returns "<fault>" instead of crashing. ----
extern "C" char* __cdecl c_vslotPeek(int argc, char** argv)
{
    static char out[512];
    out[0] = 0;
    if (argc < 2) { strcpy(out, "usage: vslotPeek(hexAddr [,count])"); return out; }
    unsigned addr = (unsigned)strtoul(argv[1], 0, 16);
    int count = (argc >= 3) ? atoi(argv[2]) : 4;
    if (count < 1) count = 1;
    if (count > 32) count = 32;
    char b[32];
    sprintf(b, "0x%08x:", addr); strcat(out, b);
    for (int i = 0; i < count; i++)
    {
        unsigned v;
        if (safeRead(addr + i*4, &v)) sprintf(b, " %08x", v);
        else                          strcpy(b, " <fault>");
        strncat(out, b, sizeof(out)-strlen(out)-1);
    }
    flog(out);
    return out;
}

// ============================================================================
// WRITE commands. These POST datablock events to a client. Test on a DEV/LOCAL
// server first. Wrong managerId => event dropped (harmless). The addresses are
// cross-checked, but this is the first path that mutates engine/network state.
// ============================================================================

static unsigned resolveDbm()
{
    unsigned dbm = 0;
    if (!safeRead(DBM_GLOBAL, &dbm)) return 0;
    return dbm;
}

// ---- vslotResend(managerId): re-send ALL datablocks to one client (the exact
//      connect-time path). Combine with a script-side ItemData.description
//      change to see a live rename. Heaviest but most-proven push. ----
extern "C" char* __cdecl c_vslotResend(int argc, char** argv)
{
    static char out[160];
    if (argc < 2) { strcpy(out, "usage: vslotResend(managerId)  [managerId = the client's %clientId]"); return out; }
    unsigned mgrId = (unsigned)strtoul(argv[1], 0, 10);
    unsigned dbm = resolveDbm();
    if (!dbm) { strcpy(out, "vslotResend: dbm not resolved"); flog(out); return out; }
    sprintf(out, "vslotResend: dbm=0x%08x mgrId=%u -> sendDataToClient()", dbm, mgrId);
    flog(out);
    __asm {
        mov eax, dbm
        mov edx, mgrId
        mov ecx, FN_SENDDATA
        call ecx
    }
    flog("vslotResend: returned OK");
    strcat(out, " done");
    return out;
}

// ---- vslotPushItem(managerId, itemIdx): surgical single-block push. Re-sends
//      the CURRENT server ItemData at itemIdx to one client (whatever
//      description script last set on it). One dataGotBlock callback, no reload.
extern "C" char* __cdecl c_vslotPushItem(int argc, char** argv)
{
    static char out[224];
    if (argc < 3) { strcpy(out, "usage: vslotPushItem(managerId, itemIndex)"); return out; }
    unsigned mgrId  = (unsigned)strtoul(argv[1], 0, 10);
    unsigned idx    = (unsigned)strtoul(argv[2], 0, 10);
    unsigned dbm = resolveDbm();
    if (!dbm) { strcpy(out, "vslotPushItem: dbm not resolved"); flog(out); return out; }
    unsigned itemCount = 0, itemArr = 0, itemPtr = 0, poster = 0;
    if (!safeRead(dbm + DBM_ITEMSIZE_OFF, &itemCount) || idx >= itemCount)
    { sprintf(out, "vslotPushItem: idx %u out of range (count=%u)", idx, itemCount); flog(out); return out; }
    safeRead(dbm + DBM_ITEMARR_OFF, &itemArr);
    if (!itemArr || !safeRead(itemArr + idx*4, &itemPtr) || !itemPtr)
    { strcpy(out, "vslotPushItem: bad item ptr"); flog(out); return out; }
    if (!safeRead(dbm + DBM_POSTER_OFF, &poster) || !poster)
    { strcpy(out, "vslotPushItem: bad poster"); flog(out); return out; }

    sprintf(out, "vslotPushItem: dbm=0x%08x mgr=%u idx=%u data=0x%08x poster=0x%08x",
            dbm, mgrId, idx, itemPtr, poster);
    flog(out);

    unsigned evt = 0;
    __asm {
        // evt = operator new(0x30)
        push EVT_SIZE
        mov  ecx, FN_OPNEW
        call ecx
        add  esp, 4
        mov  evt, eax
        test eax, eax
        je   done
        // DataBlockEvent::ctor(this=eax)
        mov  ecx, FN_EVTCTOR
        call ecx
        // fill fields on evt
        mov  ebx, evt
        mov  eax, mgrId
        mov  [ebx + EVT_MGRID], eax
        mov  dword ptr [ebx + EVT_GROUP], ITEM_GROUP
        mov  eax, itemCount
        mov  [ebx + EVT_GSIZE], eax
        mov  eax, idx
        mov  [ebx + EVT_BLOCK], eax
        mov  eax, itemPtr
        mov  [ebx + EVT_DATA], eax
        // postCurrentEvent: eax=poster(this), edx=evt ; call [poster.vtbl + 0x54]
        mov  eax, poster
        mov  edx, evt
        mov  ecx, [eax]
        call dword ptr [ecx + POST_VTBL_OFF]
    done:
    }
    if (!evt) { strcat(out, "  (new failed)"); flog("vslotPushItem: operator new failed"); return out; }
    flog("vslotPushItem: event posted OK");
    strcat(out, "  posted");
    return out;
}

// ---- engine handler ABI: argc in ECX, argv on stack ([esp+4]); ret char* in
//      EAX; callee cleans argv (ret 4). Identical to kronos_datetime. ----
#define HANDLER(NAME, IMPL) __declspec(naked) void NAME(){ \
    __asm mov eax,[esp+4] \
    __asm push eax \
    __asm push ecx \
    __asm call IMPL \
    __asm add esp,8 \
    __asm ret 4 }
HANDLER(h_vslotProbe,    c_vslotProbe)
HANDLER(h_vslotDbm,      c_vslotDbm)
HANDLER(h_vslotPeek,     c_vslotPeek)
HANDLER(h_vslotResend,   c_vslotResend)
HANDLER(h_vslotPushItem, c_vslotPushItem)

// regCmd(name, handler): byte-for-byte the kronos_datetime registration.
__declspec(naked) void regCmd(const char* /*name*/, void* /*handler*/)
{
    __asm {
        mov  eax, [esp+8]
        push eax
        push 0
        mov  ecx, [esp+0xc]
        xor  edx, edx
        mov  eax, FUN_ADDCMD
        call eax
        ret
    }
}

static bool g_registered = false;
extern "C" void __cdecl doRegister()
{
    if (g_registered) return;
    g_registered = true;
    regCmd("vslotProbe",    (void*)&h_vslotProbe);
    regCmd("vslotDbm",      (void*)&h_vslotDbm);
    regCmd("vslotPeek",     (void*)&h_vslotPeek);
    regCmd("vslotResend",   (void*)&h_vslotResend);
    regCmd("vslotPushItem", (void*)&h_vslotPushItem);
    flog("registered vslotProbe/vslotDbm/vslotPeek (RO) + vslotResend/vslotPushItem (WRITE)");
}

__declspec(naked) void myInit()
{
    __asm {
        pushad
        call doRegister
        popad
        mov  eax, 1
        ret
    }
}

// ----- Kronos plugin descriptor (40 bytes) — identical layout to datetime ---
static unsigned g_vtable[8];
static unsigned g_desc[10];

extern "C" __declspec(dllexport) void* getPlugin()
{
    flog("getPlugin() called by loader");
    g_vtable[0] = (unsigned)&myInit;
    g_vtable[1] = 0x005f40d8; g_vtable[2] = 0x005f450c; g_vtable[3] = 0x005f3ff8;
    g_vtable[4] = 0x005f3f10; g_vtable[5] = 0x005f4138; g_vtable[6] = 0x005f41a8;
    g_vtable[7] = 0x005f3ddc;
    g_desc[0] = (unsigned)g_vtable;
    g_desc[1] = 0xbaadf00d;
    g_desc[2] = 0x00000000;
    g_desc[3] = 0x40100000;                 // version double 4.0
    g_desc[4] = (unsigned)"VirtualItems";
    g_desc[5] = (unsigned)"Kronos belt-item native slots (PROBE, read-only)";
    g_desc[6] = 0;
    g_desc[7] = 3;                          // flags: client|server (mem.dll only
                                            // calls vtable[0] init when the client
                                            // bit is set; datetime uses 3 too)
    g_desc[8] = 0xbaadf00d;
    g_desc[9] = 0xbaadf00d;
    return g_desc;
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hinst);
        HMODULE self = NULL;
        GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
                           (LPCWSTR)(void*)&myInit, &self);
        flog("--- kronos_virtualitems (PROBE) loaded ---");
    }
    return TRUE;
}
