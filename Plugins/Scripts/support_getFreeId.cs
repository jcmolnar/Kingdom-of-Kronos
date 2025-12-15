// =============================================================================
// PlayerManager Script Support
// -----------------------------------------------------------------------------
// Implements PlayerManager::getFreeId and PlayerManager::isIdFree in Script
// because native DLL injection is blocked by the specific T1Vista executable.
// =============================================================================

// Constants for Client ID range
$PlayerManager::MinClientId = 2049;
$PlayerManager::MaxClientId = 4096; // 65535 is theoretical max but 2048-4096 is standard

// -----------------------------------------------------------------------------
// PlayerManager::getFreeId()
// Returns the first available Client ID starting from 2049.
// Returns -1 if no ID is available.
// -----------------------------------------------------------------------------
function PlayerManager::getFreeId()
{
	for (%id = $PlayerManager::MinClientId; %id <= $PlayerManager::MaxClientId; %id++)
	{
		// isObject(%id) returns true if ANY object (Client, Item, Static, etc.) uses this ID.
		// If false, the ID is free to use.
		if (!isObject(%id))
		{
			return %id;
		}
	}
	
	// No free IDs found
	return -1;
}

// -----------------------------------------------------------------------------
// PlayerManager::isIdFree(%id)
// Checks if a specific ID is free.
// -----------------------------------------------------------------------------
function PlayerManager::isIdFree(%id)
{
	return !isObject(%id);
}

echo("PlayerManager Support Script Loaded.");
