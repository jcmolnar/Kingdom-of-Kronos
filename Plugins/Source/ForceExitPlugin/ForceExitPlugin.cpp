//============================================================================
// ForceExitPlugin.cpp — forceExit() hard process exit for the 1.3 Kronos
// server (T1Vista.exe, Borland, base 0x400000, no ASLR).
//
// WHY: the mem.dll/hudbot loader can leave a background thread alive after
// quit() (see Plugins\_newStuff.txt V0.12 note), so the process never exits
// and InfiniteSpawn never relaunches the server. TerminateProcess does not
// wait on any thread, so a stuck DLL thread cannot block it.
//
// Commands (pure registration, NO engine hooks — cannot destabilize anything):
//   forceExit([exitCode]) -> terminates the process immediately, never returns
//
// WARNING: skips ALL engine shutdown including script onExit(). Down() in
// rpgfunk.cs (FinalizeShutdownAndExit) saves/exports first — use down(), do
// not call forceExit() ad hoc.
//
// REGISTRATION: identical proven pattern to kronos_datetime.dll — the Kronos
// loader (mem.dll) does LoadLibrary -> getPlugin() -> calls
// descriptor->vtable[0] (our init) AFTER the console is ready; init registers
// via the engine StringCallback addCommand FUN_005f4138. Handler ABI:
// ECX=argc, [ESP+4]=argv (argv[0]=name), return char* in EAX, RET 4.
//
// Coexists with kronosfix_server / kronos_playermanager / kronos_aiteardown /
// kronos_datetime (no shared hook sites; this DLL patches no code at all).
// Build: build_ForceExitPlugin.bat (MSVC x86 — naked/inline-asm requires 32-bit).
//============================================================================
#include <windows.h>
#include <cstdio>
#include <cstdlib>

#define FUN_ADDCMD 0x005f4138   // engine StringCallback addCommand (T1Vista.exe)

static void flog(const char* msg)
{
    FILE* f = fopen("ForceExitPlugin.log", "a"); if (!f) return;
    fprintf(f, "%s\n", msg); fclose(f);
}

// ---- command implementation ----
extern "C" char* __cdecl c_forceExit(int argc, char** argv)
{
    UINT exitCode = 0;
    if (argc >= 2 && argv && argv[1])
        exitCode = (UINT)atoi(argv[1]);

    char buf[64];
    sprintf(buf, "forceExit: terminating process (exit code %u)", exitCode);
    flog(buf);

    TerminateProcess(GetCurrentProcess(), exitCode);
    return (char*)"True"; // unreachable
}

// engine ABI: argc in ECX, argv on stack ([ESP+4]); callee cleans argv (ret 4); result in EAX.
#define HANDLER(NAME, IMPL) __declspec(naked) void NAME(){ \
    __asm mov eax,[esp+4]   /*argv*/ \
    __asm push eax          /*arg2 argv*/ \
    __asm push ecx          /*arg1 argc*/ \
    __asm call IMPL \
    __asm add esp,8 \
    __asm ret 4 }
HANDLER(h_forceExit, c_forceExit)

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
    regCmd("forceExit", (void*)&h_forceExit);
    flog("registered forceExit");
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
    g_desc[4] = (unsigned)"ForceExit";      // +0x10 name
    g_desc[5] = (unsigned)"Hard process exit for reliable InfiniteSpawn restarts"; // +0x14 desc
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
        flog("--- ForceExitPlugin loaded ---");
    }
    return TRUE;
}
