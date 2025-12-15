// =============================================================================
// PlayerManager Script Support
// -----------------------------------------------------------------------------
// Implements PlayerManager::getFreeId and PlayerManager::isIdFree in Script
// =============================================================================

$PlayerManager::MinClientId = 2049;
$PlayerManager::MaxClientId = 4096;

function PlayerManager::getFreeId()
{
	for (%id = $PlayerManager::MinClientId; %id <= $PlayerManager::MaxClientId; %id++)
	{
		if (!isObject(%id))
			return %id;
	}
	return -1;
}

function PlayerManager::isIdFree(%id)
{
	return !isObject(%id);
}

echo("PlayerManager Support Script Loaded.");
