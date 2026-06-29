// DTS Shape File Viewer
// Allows players to view .dts shape files in-game
// Commands: #viewdts <filename>, #listdts [page], #cleardts

// Global list of all .dts files (loaded from DTS_FILE_LIST.txt)
$DTSFileList = "";

// Load DTS file list on server start
function DTSViewer::init()
{
	%file = "DTS_FILE_LIST.txt";
	// Try root directory first, then check if it's in a subdirectory
	if(!isFile(%file))
		%file = "..\\..\\DTS_FILE_LIST.txt";
	if(isFile(%file))
	{
		%handle = openFile(%file, "read");
		if(%handle != -1)
		{
			%count = 0;
			while(!fileEOF(%handle))
			{
				%line = readLine(%handle);
				%line = String::trim(%line);
				if(%line != "")
				{
					// Remove .dts extension for easier searching
					%name = String::getSubStr(%line, 0, String::len(%line) - 4);
					$DTSFileList = $DTSFileList @ %line @ "\t" @ %name @ "\n";
					$DTSFile[%count] = %line;
					$DTSFileName[%count] = %name;
					%count++;
				}
			}
			closeFile(%handle);
			$DTSFileCount = %count;
			echo("DTS Viewer: Loaded " @ %count @ " .dts files");
		}
		else
		{
			echo("DTS Viewer: Could not open " @ %file);
		}
	}
	else
	{
		echo("DTS Viewer: " @ %file @ " not found");
	}
}

// View a specific .dts file
function DTSViewer::viewShape(%clientId, %shapeName)
{
	%player = Client::getOwnedObject(%clientId);
	if(%player == "" || %player == -1)
	{
		Client::sendMessage(%clientId, $MsgRed, "You must be in-game to view shapes.");
		return;
	}

	// Clean up old viewer object for this player
	DTSViewer::clearViewer(%clientId);

	// Find the shape file
	%shapeFile = "";
	%found = false;
	
	// Try exact match first (with .dts extension)
	if(String::findSubStr(%shapeName, ".dts") != -1)
	{
		%shapeFile = %shapeName;
	}
	else
	{
		%shapeFile = %shapeName @ ".dts";
	}

	// Search through the list
	for(%i = 0; %i < $DTSFileCount; %i++)
	{
		%file = $DTSFile[%i];
		%name = $DTSFileName[%i];
		
		// Check if it matches (case-insensitive)
		if(String::ICompare(%file, %shapeFile) == 0 || String::ICompare(%name, %shapeName) == 0)
		{
			%shapeFile = %file;
			%found = true;
			break;
		}
	}

	if(!%found)
	{
		// Try partial match
		for(%i = 0; %i < $DTSFileCount; %i++)
		{
			%file = $DTSFile[%i];
			%name = $DTSFileName[%i];
			
			if(String::findSubStr(String::toLower(%name), String::toLower(%shapeName)) != -1)
			{
				%shapeFile = %file;
				%found = true;
				break;
			}
		}
	}

	if(!%found)
	{
		Client::sendMessage(%clientId, $MsgRed, "Shape file not found: " @ %shapeName);
		Client::sendMessage(%clientId, $MsgBeige, "Use #listdts to see available shapes.");
		return;
	}

	// Get player position and spawn shape in front of them
	%playerPos = GameBase::getPosition(%player);
	%playerRot = GameBase::getRotation(%player);
	
	%px = GetWord(%playerPos, 0);
	%py = GetWord(%playerPos, 1);
	%pz = GetWord(%playerPos, 2);
	
	// Calculate forward direction
	%rotX = GetWord(%playerRot, 0);
	%rotY = GetWord(%playerRot, 1);
	%rotZ = GetWord(%playerRot, 2);
	
	// Spawn 5 units in front of player
	%spawnX = %px + (5 * mSin(%rotZ * 3.14159 / 180));
	%spawnY = %py + (5 * mCos(%rotZ * 3.14159 / 180));
	%spawnZ = %pz + 1;
	%spawnPos = %spawnX @ " " @ %spawnY @ " " @ %spawnZ;
	
	// Create the StaticShape
	%viewerObj = newObject("", "StaticShape", %shapeFile, true);
	if(%viewerObj != "")
	{
		addToSet("MissionCleanup", %viewerObj);
		GameBase::setPosition(%viewerObj, %spawnPos);
		GameBase::setRotation(%viewerObj, %playerRot);
		
		// Store reference for cleanup
		$DTSViewerObject[%clientId] = %viewerObj;
		
		Client::sendMessage(%clientId, $MsgGreen, "Viewing shape: " @ %shapeFile);
		Client::sendMessage(%clientId, $MsgBeige, "Use #cleardts to remove the viewer object.");
	}
	else
	{
		Client::sendMessage(%clientId, $MsgRed, "Failed to load shape: " @ %shapeFile);
	}
}

// Clear viewer object for a player
function DTSViewer::clearViewer(%clientId)
{
	if($DTSViewerObject[%clientId] != "")
	{
		%obj = $DTSViewerObject[%clientId];
		if(isObject(%obj))
		{
			GameBase::delete(%obj);
		}
		$DTSViewerObject[%clientId] = "";
	}
}

// List available .dts files (paginated)
function DTSViewer::listShapes(%clientId, %page)
{
	if($DTSFileCount == 0)
	{
		Client::sendMessage(%clientId, $MsgRed, "No .dts files loaded. Run DTSViewer::init() first.");
		return;
	}

	%pageSize = 20;
	%rawPages = $DTSFileCount / %pageSize;
	%totalPages = floor(%rawPages);
	if(%totalPages < %rawPages)
		%totalPages++;
	
	if(%page == "" || %page < 1)
		%page = 1;
	if(%page > %totalPages)
		%page = %totalPages;
	
	%start = (%page - 1) * %pageSize;
	%end = %start + %pageSize;
	if(%end > $DTSFileCount)
		%end = $DTSFileCount;
	
	Client::sendMessage(%clientId, $MsgGreen, "=== Available .dts Files (Page " @ %page @ " of " @ %totalPages @ ") ===");
	
	for(%i = %start; %i < %end; %i++)
	{
		%file = $DTSFile[%i];
		%name = $DTSFileName[%i];
		Client::sendMessage(%clientId, $MsgWhite, (%i + 1) @ ". " @ %name @ " (" @ %file @ ")");
	}
	
	if(%page < %totalPages)
		Client::sendMessage(%clientId, $MsgBeige, "Use #listdts " @ (%page + 1) @ " to see more.");
}

// Search for shapes by name
function DTSViewer::searchShapes(%clientId, %searchTerm)
{
	if($DTSFileCount == 0)
	{
		Client::sendMessage(%clientId, $MsgRed, "No .dts files loaded.");
		return;
	}

	%searchTerm = String::toLower(%searchTerm);
	%results = 0;
	%maxResults = 20;
	
	Client::sendMessage(%clientId, $MsgGreen, "=== Search Results for: " @ %searchTerm @ " ===");
	
	for(%i = 0; %i < $DTSFileCount && %results < %maxResults; %i++)
	{
		%file = $DTSFile[%i];
		%name = $DTSFileName[%i];
		
		if(String::findSubStr(String::toLower(%name), %searchTerm) != -1 || 
		   String::findSubStr(String::toLower(%file), %searchTerm) != -1)
		{
			Client::sendMessage(%clientId, $MsgWhite, (%results + 1) @ ". " @ %name @ " (" @ %file @ ")");
			%results++;
		}
	}
	
	if(%results == 0)
		Client::sendMessage(%clientId, $MsgRed, "No matches found.");
	else if(%results == %maxResults)
		Client::sendMessage(%clientId, $MsgBeige, "Showing first " @ %maxResults @ " results. Be more specific to narrow down.");
}

// Initialize on server start
schedule("DTSViewer::init();", 1);

