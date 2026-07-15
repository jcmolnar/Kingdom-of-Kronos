//============================================================================
// kronos_datetime.cpp — real calendar date/time console commands for the 1.3
// Kronos server (T1Vista.exe, Borland, base 0x400000, no ASLR).
//
// WHY: TorqueScript has no calendar source. getIntegerTime() is the sim clock
// (resets on restart, and kronosfix_server.dll rebases it), timeStamp() is the
// BUILD date. The daily quest rotation needs a day stamp that survives server
// crashes/restarts, so this plugin exposes the OS clock to script.
//
// Commands (pure registration, NO engine hooks — cannot destabilize anything):
//   getRealDate()      -> "YYYYMMDD"        e.g. 20260702  (local time)
//   getRealTime()      -> "HHMMSS"          e.g. 143205    (24h, local)
//   getRealDayOfWeek() -> "0".."6"          0 = Sunday (SYSTEMTIME convention)
//
// REGISTRATION: identical proven pattern to kronos_playermanager.dll — the
// Kronos loader (mem.dll) does LoadLibrary -> getPlugin() -> calls
// descriptor->vtable[0] (our init) AFTER the console is ready; init registers
// via the engine StringCallback addCommand FUN_005f4138. Handler ABI:
// ECX=argc, [ESP+4]=argv (argv[0]=name), return char* in EAX, RET 4.
//
// Coexists with kronosfix_server / kronos_playermanager / kronos_aiteardown
// (no shared hook sites; this DLL patches no code at all).
// Build: build_kronos_datetime.bat (MSVC x86 — naked/inline-asm requires 32-bit).
//============================================================================
#include <windows.h>
#include <cstdio>

#define FUN_ADDCMD 0x005f4138   // engine StringCallback addCommand (T1Vista.exe)

static void flog(const char* msg)
{
    FILE* f = fopen("kronos_datetime.log", "a"); if (!f) return;
    fprintf(f, "%s\n", msg); fclose(f);
}

// ---- command implementations (argc/argv unused; zero-arg commands) ----
extern "C" char* __cdecl c_getRealDate(int, char**)
{
    static char buf[16];
    SYSTEMTIME st; GetLocalTime(&st);
    sprintf(buf, "%04d%02d%02d", st.wYear, st.wMonth, st.wDay);
    return buf;
}
extern "C" char* __cdecl c_getRealTime(int, char**)
{
    static char buf[16];
    SYSTEMTIME st; GetLocalTime(&st);
    sprintf(buf, "%02d%02d%02d", st.wHour, st.wMinute, st.wSecond);
    return buf;
}
extern "C" char* __cdecl c_getRealDayOfWeek(int, char**)
{
    static char buf[4];
    SYSTEMTIME st; GetLocalTime(&st);
    sprintf(buf, "%d", st.wDayOfWeek);
    return buf;
}
// getRealMillis() -> wall-clock milliseconds (GetTickCount). The sim clocks
// (getSimTime / getIntegerTime) FREEZE for the duration of a frame, so script
// can't time anything inside one frame with them - this can (perf probes).
// Wraps at 49.7 days; probes only ever subtract two nearby reads.
extern "C" char* __cdecl c_getRealMillis(int, char**)
{
    static char buf[16];
    sprintf(buf, "%lu", (unsigned long)GetTickCount());
    return buf;
}

// engine ABI: argc in ECX, argv on stack ([ESP+4]); callee cleans argv (ret 4); result in EAX.
#define HANDLER(NAME, IMPL) __declspec(naked) void NAME(){ \
    __asm mov eax,[esp+4]   /*argv*/ \
    __asm push eax          /*arg2 argv*/ \
    __asm push ecx          /*arg1 argc*/ \
    __asm call IMPL \
    __asm add esp,8 \
    __asm ret 4 }
HANDLER(h_getRealDate,      c_getRealDate)
HANDLER(h_getRealTime,      c_getRealTime)
HANDLER(h_getRealDayOfWeek, c_getRealDayOfWeek)
HANDLER(h_getRealMillis,    c_getRealMillis)

// regCmd(name, handler): the engine's getNumClients StringCallback registration, byte-for-byte.
//   PUSH handler ; PUSH 0 ; MOV ECX,name ; XOR EDX,EDX ; CALL 0x005f4138  (RET 8 self-cleans)
__declspec(naked) void regCmd(const char* /*name*/, void* /*handler*/)
{
    __asm {
        mov  eax, [esp+8]       // handler (arg2)
        push eax                // param_5 = handler
        push 0                  // param_4 = 0 (handler validates argc itself; none needed here)
        mov  ecx, [esp+0xc]     // name (arg1: orig [esp+4], +8 from the 2 pushes)
        xor  edx, edx           // param_2 = 0
        mov  eax, FUN_ADDCMD
        call eax                // FUN_005f4138 ; RET 8 cleans the 2 pushes
        ret                     // cdecl: caller cleans name+handler
    }
}

static bool g_registered = false;
extern "C" void __cdecl doRegister()
{
    if (g_registered) return;
    g_registered = true;
    regCmd("getRealDate",      (void*)&h_getRealDate);
    regCmd("getRealTime",      (void*)&h_getRealTime);
    regCmd("getRealDayOfWeek", (void*)&h_getRealDayOfWeek);
    regCmd("getRealMillis",    (void*)&h_getRealMillis);
    flog("registered getRealDate/getRealTime/getRealDayOfWeek/getRealMillis");
}

// Plugin descriptor vtable[0] = our init; loader calls it AFTER the console is ready.
__declspec(naked) void myInit()
{
    __asm {
        pushad
        call doRegister
        popad
        mov  eax, 1             // activation success
        ret
    }
}

// ----- Kronos plugin descriptor (40 bytes) returned by getPlugin -----
//  [+0]=vtable; [+4]=cookie; [+8..+0xf]=version DOUBLE 4.0; [+0x10]=name; [+0x14]=desc;
//  [+0x18]=cmd table(unused); [+0x1c]=flags(1=client,2=server,3=both); [+0x20..]=cookie.
static unsigned g_vtable[8];
static unsigned g_desc[10];

extern "C" __declspec(dllexport) void* getPlugin()
{
    g_vtable[0] = (unsigned)&myInit;
    g_vtable[1] = 0x005f40d8; g_vtable[2] = 0x005f450c; g_vtable[3] = 0x005f3ff8;
    g_vtable[4] = 0x005f3f10; g_vtable[5] = 0x005f4138; g_vtable[6] = 0x005f41a8;
    g_vtable[7] = 0x005f3ddc;
    g_desc[0] = (unsigned)g_vtable;         // +0   vtable
    g_desc[1] = 0xbaadf00d;                 // +4
    g_desc[2] = 0x00000000;                 // +8   version double lo
    g_desc[3] = 0x40100000;                 // +0xc version double hi => 4.0
    g_desc[4] = (unsigned)"DateTime";       // +0x10 name
    g_desc[5] = (unsigned)"Kronos real calendar date/time"; // +0x14 desc
    g_desc[6] = 0;                          // +0x18 cmd table (unused; init registers directly)
    g_desc[7] = 3;                          // +0x1c flags: client|server
    g_desc[8] = 0xbaadf00d;                 // +0x20
    g_desc[9] = 0xbaadf00d;                 // +0x24
    return g_desc;
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hinst);
        HMODULE self = NULL;   // self-pin so a stray FreeLibrary can't dangle registered handlers
        GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
                           (LPCWSTR)(void*)&myInit, &self);
        flog("--- kronos_datetime loaded ---");
    }
    return TRUE;
}
