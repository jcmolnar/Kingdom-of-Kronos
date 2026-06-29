// =============================================================================
// PlayerManager Script Support
// -----------------------------------------------------------------------------
// getFreeId / isIdFree are now provided NATIVELY by kronos_playermanager.dll
// (reservation-aware; reads the engine's REAL client free-pool head). The script
// versions were removed so the native commands are authoritative with no
// shadowing. reserveId / releaseId / getFreeList are DLL-only. See re/playermanager.md.
// =============================================================================

$PlayerManager::MinClientId = 2049;
$PlayerManager::MaxClientId = 4096;

echo("PlayerManager Support: native DLL provides getFreeId/isIdFree/reserveId/releaseId/getFreeList.");
