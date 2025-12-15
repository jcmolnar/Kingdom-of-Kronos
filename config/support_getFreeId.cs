// =============================================================================
// PlayerManager Script Support
// -----------------------------------------------------------------------------
// Implements PlayerManager::getFreeId and PlayerManager::isIdFree in Script
// =============================================================================

$PlayerManager::MinClientId = 2049;
$PlayerManager::MaxClientId = 4096;

// Check if a client ID slot is free (no client connected)
// NOTE: isObject() does NOT work for client IDs - must use Client:: functions
function PlayerManager::isIdFree(%id)
{
	// A slot is free if there's no name AND no owned object
	%name = Client::getName(%id);
	if(%name != "" && %name != -1)
		return false;  // Name exists = occupied
	
	%obj = Client::getOwnedObject(%id);
	if(%obj != "" && %obj != -1)
		return false;  // Has player object = occupied
	
	return true;  // Slot is free
}

// Find the first free client ID
function PlayerManager::getFreeId()
{
	for (%id = $PlayerManager::MinClientId; %id <= $PlayerManager::MaxClientId; %id++)
	{
		if (PlayerManager::isIdFree(%id))
			return %id;
	}
	return -1;  // Server is full
}

echo("PlayerManager Support Script Loaded (FIXED - uses Client::getName).");

