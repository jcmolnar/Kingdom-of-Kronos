//============================================================================
// kronos_reprobe.cpp — READ-ONLY runtime confirmation probe for the T1Vista.exe
// full-binary static RE map (T1Vista_RE_Kit/RE/MASTER_RE_MAP.md).
//
// PURPOSE: upgrade the map's [static:unverified] NUMERIC offsets/addresses to
// [confirmed:runtime] by reading them on a LIVE (dev/local) T1Vista server and
// cross-checking each against a self-consistent truth. Every read is SEH-guarded:
// a wrong address returns "<fault>" and NEVER crashes the server. The single
// engine-function CALL (reFindCall) is also SEH-wrapped.
//
// This mutates NOTHING (no events, no writes). Safe to load on any server, but
// run it on a DEV/LOCAL one first per RE discipline. Registration ABI is
// identical to the proven kronos_virtualitems.dll / kronos_datetime.dll.
//
// Commands (type in the server console):
//   reGlobals()            sg-globals field map: dbm / SimManager / PlayerManager
//                          resolved two ways (sg-relative + direct), cross-checked.
//   reItem(idx)            ItemData wire fields for datablock item[idx]:
//                          name, imageId, price, hudIcon, typeString, showInventory.
//   reClient(managerId)    THE mega-validator. managerId = your script %clientId.
//                          Computes ClientRep via findClient's index math and asserts
//                          rep.id(+0xc)==managerId (PASS => layout + math confirmed),
//                          then dumps the ShapeBase combat block (energy/damage/data)
//                          from BOTH the +0x94 and +0x98 candidates to resolve which
//                          is controlObject.
//   reFindCall(managerId)  SEH-guarded CALL to findClient@0x0040de80; compares its
//                          returned pointer to the computed rep (confirms the ADDRESS).
//
// Expected PASS output (if the map is correct) is documented per-command below and
// in RE/MASTER_RE_MAP.md.
//============================================================================
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <cstdlib>

#define FUN_ADDCMD     0x005f4138   // addCommand (confirmed:runtime)

// ---- globals (sg field map; Domains A/D/E consensus) ----
#define SG_PP          0x006a8494   // ptr-to-sg-globals            [confirmed]
#define SG_DBM_OFF     0x10         // sg+0x10 = dbm                [confirmed]
#define SG_SIMMGR_OFF  0x14         // sg+0x14 = root SimManager    [static]
#define SG_PM_OFF      0x34         // sg+0x34 = PlayerManager      [static]
#define DBM_DIRECT     0x006a8428   // dbm direct global           [confirmed]
#define SIMMGR_DIRECT  0x006a842c   // SimManager direct global    [static]
#define PM_DIRECT      0x006a844c   // PlayerManager direct global [static]

// ---- DataBlockManager / ItemData ----
#define DBM_ITEMSIZE_OFF 0x10b4     // dataBlocks[Item=5].size      [confirmed]
#define DBM_ITEMARR_OFF  0x10c0     // dataBlocks[Item=5].array     [confirmed]
#define ITEM_NAME1     0x04         // GameBaseData nameKey (name)  [static]
#define ITEM_CLASSNM   0x20         // GameBaseData className       [static]
#define ITEM_DESC      0x14         // GameBaseData description (LOW confidence)
#define ITEM_IMAGEID   0x100        // ItemData imageId  int
#define ITEM_PRICE     0x104        // ItemData price    int         [static — belt work]
#define ITEM_HUDICON   0x11c        // ItemData hudIcon  char*
#define ITEM_TYPESTR   0x120        // ItemData typeString char*
#define ITEM_SHOWINV   0x124        // ItemData showInventory byte

// ---- PlayerManager / ClientRep (Domain D) ----
#define PM_CLIENTLIST  0x250ec      // clientList[128] base
#define PM_NUMCLIENTS  0x24ee0      // numClients int
#define CLIENTREP_SZ   0x1E0        // sizeof(ClientRep) = 480
#define REP_ID         0x0c         // id == managerId == %clientId
#define REP_NAME       0x10         // name char*
#define REP_TEAM       0x14         // team int
#define REP_CTRL_A     0x94         // controlObject candidate A (Domain D)
#define REP_CTRL_B     0x98         // controlObject candidate B (Domain C) / ownedObject
#define MGR_BASE       0x800        // index = managerId - 0x800 (2048)

// ---- ShapeBase combat (Domain C) ----
#define SB_DATA        0x240        // ShapeBase::data (ShapeBaseData*)
#define SB_SIMOBJ_ID   0x3c         // SimObject id
#define SB_ENERGY      0x724        // energy float
#define SB_DAMAGE      0x72c        // damageLevel float

#define FN_FINDCLIENT  0x0040de80   // findClient(pm, managerId) regcall EAX=pm,EDX=id

// ---------------------------------------------------------------- safe reads
static bool safeRead(unsigned va, unsigned* out) {
    __try { *out = *(volatile unsigned*)va; return true; }
    __except (EXCEPTION_EXECUTE_HANDLER) { return false; }
}
static bool safeReadByte(unsigned va, unsigned char* out) {
    __try { *out = *(volatile unsigned char*)va; return true; }
    __except (EXCEPTION_EXECUTE_HANDLER) { return false; }
}
static bool inImage(unsigned va)   { return va >= 0x00400000 && va < 0x007E1000; }
static bool looksHeap(unsigned va) { return va >= 0x00010000 && va < 0x7FFF0000; }

// read a C string (bounded) into buf; returns true if it looks like text.
static bool safeReadStr(unsigned va, char* buf, int cap) {
    int i = 0;
    for (; i < cap - 1; i++) {
        unsigned char c;
        if (!safeReadByte(va + i, &c)) { buf[i] = 0; return i > 0; }
        if (c == 0) break;
        if (c < 9 || c > 126) { buf[i] = 0; return false; } // non-text
        buf[i] = (char)c;
    }
    buf[i] = 0;
    return i > 0;
}
static void appf(char* out, int cap, const char* fmt, ...) {
    int n = (int)strlen(out); va_list ap; va_start(ap, fmt);
    _vsnprintf(out + n, cap - n - 1, fmt, ap); va_end(ap); out[cap-1] = 0;
}
static float asF(unsigned u) { float f; memcpy(&f, &u, 4); return f; }

static void flog(const char* s) { FILE* f = fopen("kronos_reprobe.log","a"); if(f){fprintf(f,"%s\n",s);fclose(f);} }

// ---------------------------------------------------------------- reGlobals
// PASS: dbm(sg+0x10) == dbm(direct 0x6a8428); all three ptrs look like heap.
extern "C" char* __cdecl c_reGlobals(int, char**) {
    static char o[512]; o[0]=0;
    unsigned sg=0,dbmR=0,simR=0,pmR=0,dbmD=0,simD=0,pmD=0;
    if (!safeRead(SG_PP,&sg)||!sg){ strcpy(o,"reGlobals: sg ptr <fault>"); return o; }
    safeRead(sg+SG_DBM_OFF,&dbmR); safeRead(sg+SG_SIMMGR_OFF,&simR); safeRead(sg+SG_PM_OFF,&pmR);
    safeRead(DBM_DIRECT,&dbmD); safeRead(SIMMGR_DIRECT,&simD); safeRead(PM_DIRECT,&pmD);
    appf(o,sizeof(o),"sg=0x%08x | dbm sg+0x10=0x%08x direct=0x%08x %s | ",
         sg,dbmR,dbmD,(dbmR==dbmD&&dbmR)?"MATCH":"DIFFER");
    appf(o,sizeof(o),"SimMgr sg+0x14=0x%08x direct=0x%08x %s | ",
         simR,simD,(simR==simD&&simR)?"MATCH":"DIFFER");
    appf(o,sizeof(o),"PM sg+0x34=0x%08x direct=0x%08x %s",
         pmR,pmD,(pmR==pmD&&pmR)?"MATCH":"DIFFER");
    flog(o); return o;
}

// ---------------------------------------------------------------- reItem
// PASS: name is readable text; price a small int; hudIcon/typeString readable; showInv 0/1.
extern "C" char* __cdecl c_reItem(int argc, char** argv) {
    static char o[640]; o[0]=0;
    unsigned idx = (argc>=2)?(unsigned)strtoul(argv[1],0,10):0;
    unsigned dbm=0,cnt=0,arr=0,it=0;
    if(!safeRead(DBM_DIRECT,&dbm)||!dbm){strcpy(o,"reItem: dbm <fault>");return o;}
    safeRead(dbm+DBM_ITEMSIZE_OFF,&cnt);
    if(idx>=cnt){ appf(o,sizeof(o),"reItem: idx %u out of range (count=%u)",idx,cnt); return o; }
    safeRead(dbm+DBM_ITEMARR_OFF,&arr);
    if(!arr||!safeRead(arr+idx*4,&it)||!it){strcpy(o,"reItem: item ptr <fault>");return o;}
    unsigned nameP=0,hudP=0,typeP=0,imageId=0,price=0; unsigned char showInv=0xff;
    safeRead(it+ITEM_NAME1,&nameP); safeRead(it+ITEM_IMAGEID,&imageId);
    safeRead(it+ITEM_PRICE,&price); safeRead(it+ITEM_HUDICON,&hudP);
    safeRead(it+ITEM_TYPESTR,&typeP); safeReadByte(it+ITEM_SHOWINV,&showInv);
    char nm[64]="<bad>",hud[64]="<bad>",typ[64]="<bad>";
    safeReadStr(nameP,nm,sizeof(nm)); safeReadStr(hudP,hud,sizeof(hud)); safeReadStr(typeP,typ,sizeof(typ));
    appf(o,sizeof(o),"item[%u]@0x%08x name=\"%s\" imageId=%d price=%u(+0x104) hudIcon=\"%s\"(+0x11c) typeString=\"%s\"(+0x120) showInventory=%u(+0x124)",
         idx,it,nm,(int)imageId,price,hud,typ,showInv);
    flog(o); return o;
}

// dump a ShapeBase-combat candidate; returns short label of plausibility.
static void dumpShape(char* o,int cap,const char* tag,unsigned ctrl){
    if(!looksHeap(ctrl)){ appf(o,cap," | %s=0x%08x <not-ptr>",tag,ctrl); return; }
    unsigned sid=0,data=0,eU=0,dU=0;
    safeRead(ctrl+SB_SIMOBJ_ID,&sid); safeRead(ctrl+SB_DATA,&data);
    safeRead(ctrl+SB_ENERGY,&eU); safeRead(ctrl+SB_DAMAGE,&dU);
    float e=asF(eU),d=asF(dU);
    bool plausible = looksHeap(data) && e>=-1.0f && e<10000.0f && d>=-1.0f && d<100000.0f;
    appf(o,cap," | %s=0x%08x[simId=%d data=0x%08x energy=%.1f dmg=%.1f]%s",
         tag,ctrl,(int)sid,data,e,d, plausible?" <PLAUSIBLE ShapeBase>":"");
}

// ---------------------------------------------------------------- reClient
// PASS: rep.id(+0xc) == managerId  => findClient index math + ClientRep layout confirmed.
// Then one of the two ctrl candidates dumps as <PLAUSIBLE ShapeBase> => that offset is
// controlObject and energy(+0x724)/damage(+0x72c)/data(+0x240) are confirmed.
extern "C" char* __cdecl c_reClient(int argc, char** argv){
    static char o[768]; o[0]=0;
    if(argc<2){strcpy(o,"usage: reClient(managerId)  [managerId = your script %clientId, 2049-2175]");return o;}
    unsigned mgr=(unsigned)strtoul(argv[1],0,10);
    unsigned pm=0; if(!safeRead(PM_DIRECT,&pm)||!pm){strcpy(o,"reClient: PM <fault>");return o;}
    if(mgr<MGR_BASE||mgr>=MGR_BASE+128){appf(o,sizeof(o),"reClient: managerId %u outside [2048,2175]",mgr);return o;}
    unsigned idx=mgr-MGR_BASE;
    unsigned rep=pm+PM_CLIENTLIST+idx*CLIENTREP_SZ;
    unsigned id=0,nameP=0,team=0,ctrlA=0,ctrlB=0;
    safeRead(rep+REP_ID,&id); safeRead(rep+REP_NAME,&nameP); safeRead(rep+REP_TEAM,&team);
    safeRead(rep+REP_CTRL_A,&ctrlA); safeRead(rep+REP_CTRL_B,&ctrlB);
    char nm[64]="<bad>"; safeReadStr(nameP,nm,sizeof(nm));
    appf(o,sizeof(o),"rep[idx=%u]@0x%08x id(+0xc)=%u %s name=\"%s\" team=%d",
         idx,rep,id,(id==mgr)?"== managerId PASS":"!= managerId FAIL",nm,(int)team);
    dumpShape(o,sizeof(o),"ctrl+0x94",ctrlA);
    dumpShape(o,sizeof(o),"ctrl+0x98",ctrlB);
    flog(o); return o;
}

// ---------------------------------------------------------------- reFindCall
// SEH-guarded CALL to findClient@0x40de80; compares to computed rep => confirms ADDRESS.
extern "C" char* __cdecl c_reFindCall(int argc, char** argv){
    static char o[320]; o[0]=0;
    if(argc<2){strcpy(o,"usage: reFindCall(managerId)");return o;}
    unsigned mgr=(unsigned)strtoul(argv[1],0,10);
    unsigned pm=0; if(!safeRead(PM_DIRECT,&pm)||!pm){strcpy(o,"reFindCall: PM <fault>");return o;}
    unsigned computed = (mgr>=MGR_BASE&&mgr<MGR_BASE+128)? pm+PM_CLIENTLIST+(mgr-MGR_BASE)*CLIENTREP_SZ : 0;
    unsigned ret=0; bool crashed=false;
    __try {
        __asm {
            mov eax, pm
            mov edx, mgr
            mov ecx, FN_FINDCLIENT
            call ecx
            mov ret, eax
        }
    } __except(EXCEPTION_EXECUTE_HANDLER) { crashed=true; }
    if(crashed){ appf(o,sizeof(o),"reFindCall: CALL faulted (address 0x%08x wrong?)",FN_FINDCLIENT); flog(o); return o; }
    appf(o,sizeof(o),"findClient(0x%08x)=0x%08x  computed=0x%08x  %s",
         mgr,ret,computed,(ret==computed&&ret)?"MATCH -> address+math CONFIRMED":
                          (ret==0?"NULL (slot empty/invalid id)":"DIFFER"));
    flog(o); return o;
}

// ------------------------------------------------- registration (proven ABI)
#define HANDLER(NAME, IMPL) __declspec(naked) void NAME(){ \
    __asm mov eax,[esp+4] \
    __asm push eax \
    __asm push ecx \
    __asm call IMPL \
    __asm add esp,8 \
    __asm ret 4 }
HANDLER(h_reGlobals,  c_reGlobals)
HANDLER(h_reItem,     c_reItem)
HANDLER(h_reClient,   c_reClient)
HANDLER(h_reFindCall, c_reFindCall)

__declspec(naked) void regCmd(const char* /*name*/, void* /*handler*/){
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
static bool g_reg=false;
extern "C" void __cdecl doRegister(){ if(g_reg)return; g_reg=true;
    regCmd("reGlobals",  (void*)&h_reGlobals);
    regCmd("reItem",     (void*)&h_reItem);
    regCmd("reClient",   (void*)&h_reClient);
    regCmd("reFindCall", (void*)&h_reFindCall);
    flog("kronos_reprobe: registered reGlobals/reItem/reClient/reFindCall (READ-ONLY)");
}
__declspec(naked) void myInit(){
    __asm {
        pushad
        call doRegister
        popad
        mov  eax, 1
        ret
    }
}

static unsigned g_vt[8], g_desc[10];
extern "C" __declspec(dllexport) void* getPlugin(){
    g_vt[0]=(unsigned)&myInit;
    g_vt[1]=0x005f40d8; g_vt[2]=0x005f450c; g_vt[3]=0x005f3ff8; g_vt[4]=0x005f3f10;
    g_vt[5]=0x005f4138; g_vt[6]=0x005f41a8; g_vt[7]=0x005f3ddc;
    g_desc[0]=(unsigned)g_vt; g_desc[1]=0xbaadf00d; g_desc[2]=0; g_desc[3]=0x40100000;
    g_desc[4]=(unsigned)"ReProbe"; g_desc[5]=(unsigned)"T1Vista RE runtime confirmation (read-only)";
    g_desc[6]=0; g_desc[7]=3;   // flags MUST be 3 (client|server) or init never runs
    g_desc[8]=0xbaadf00d; g_desc[9]=0xbaadf00d; return g_desc;
}
BOOL WINAPI DllMain(HINSTANCE h,DWORD r,LPVOID){ if(r==DLL_PROCESS_ATTACH) DisableThreadLibraryCalls(h); return TRUE; }
