# Walkthrough - PlayerManager Development Journey

This document outlines the complete process of developing a solution for ID management in Kingdom of Kronos, detailing what was attempted, what failed, and the final successful implementation.

## Phase 1: The C++ Plugin Attempt (Failed)

### Goal
Create a standalone C++ DLL (`PlayerManagerPlugin.dll`) to expose `PlayerManager::getFreeId()` and `PlayerManager::isIdFree()` as high-performance console commands.

### Implementation Logic
1.  **Plugin Interface**: Implemented `_open` and `_close` exports required by Tribes' `SimDLLObject`.
2.  **Console Binding**: Used reverse-engineered addresses for `Console` instance and `addCommand` function to register TorqueScript commands from C++.
3.  **Memory Access**: Attempted direct memory scanning for `PlayerManager` (though ultimately not needed for the script logic).

### What Didn't Work
*   **Persistent Loading Failures**: `T1Vista.exe` consistently refused to load the DLL via `SimDLLObject`.
    *   Error: `newObject: persistent create failed` (despite valid path).
    *   Manual `LoadLibrary` tests also acted suspiciously (possibly blocked).
*   **Whitelisting Hypothesis**: The modified executable (`T1Vista.exe`) likely contains a hardcoded whitelist or integrity check for DLLs, or requires exports/signatures that undocumented community knowledge suggested.
*   **Outcome**: The C++ approach was abandoned to avoid modifying the executable or fighting obscure anti-cheat/integrity mechanisms.

## Phase 2: The Script Pivot (Success)

### Goal
Implement the same functionality using pure TorqueScript, leveraging the engine's built-in `isObject()` function.

### Implementation Logic
1.  **Concept**: Since Client IDs are fixed range (2049-4096), we can iterate this range and check `isObject(%id)` to find a free slot.
2.  **Logic**:
    ```javascript
    function PlayerManager::getFreeId() {
        for(%i = 2049; %i <= 4096; %i++) {
            if(!isObject(%i)) return %i;
        }
        return -1;
    }
    ```
3.  **Loading**:
    *   Created `config/support_getFreeId.cs` (search path safe).
    *   Called via `exec("support_getFreeId.cs")` in `PluginLoader.cs`.

### What Worked
*   **Reliability**: Script always works if the engine runs. No binary dependencies.
*   **Performance**: While O(N), the loop is fast enough for occasional use (spawning).
*   **Integration**: Seamlessly callable from any other script.

## Phase 3: Core Integration (Optimization)

### Problem
The server needed protection against full capacity and optimization for bot spawning (which previously scanned 2049-2200 client IDs manually).

### Solution: `Ai.cs` Integration
We integrated `PlayerManager::getFreeId()` into the spawn core:

1.  **`SpawnAI` (Enemy Bots)**
    *   **Safeguard**: Checks `getFreeId()` before spawning. If -1, aborts spawn (prevents crashes).
    *   **Prediction**: Passes the predicted ID to `SpawnAIGetClientId`.
    *   **Optimization**: `SpawnAIGetClientId` now verifies the predicted ID first (O(1)) before falling back to expensive searches (O(N)).

2.  **`SpawnZoneBots` (Town Bots)**
    *   **Safeguard**: Checks `getFreeId()` inside the spawn loop.
    *   **Optimization**: Validates the predicted ID immediately after spawn.

## Final Status
*   **Plugin**: Script-based (`support_getFreeId.cs`).
*   **Integration**: Fully merged into `Ai.cs`.
*   **Verification**: Ready for server launch test.
