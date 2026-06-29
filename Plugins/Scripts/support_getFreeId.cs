// =============================================================================
// PlayerManager Script Support
// -----------------------------------------------------------------------------
// getFreeId / isIdFree are now provided NATIVELY by kronos_playermanager.dll
// (reservation-aware; reads the engine's REAL client free-pool head, not an
// isObject scan). The script versions were removed so the native commands are
// authoritative with no shadowing. reserveId / releaseId / getFreeList are
// DLL-only. Original kept as support_getFreeId.cs.bak-playermgr. See re/playermanager.md.
// =============================================================================

// Kept in case any caller still references these constants.
$PlayerManager::MinClientId = 2049;
$PlayerManager::MaxClientId = 4096;

echo("PlayerManager Support: native DLL provides getFreeId/isIdFree/reserveId/releaseId/getFreeList.");
