// Debug Initialization Script
// Automatically starts debug UDP broadcasting when server loads
// Set $Debug::Enabled = true; in ServerPrefs.cs to enable, false; to disable

// Default to disabled if not set in ServerPrefs.cs
if($Debug::Enabled == "")
	$Debug::Enabled = false; // Default to disabled for safety

// Debug filtering configuration
// These control what gets logged (filtering is done client-side in Python, but these help organize)
if($Debug::TrackUnassignedVars == "")
	$Debug::TrackUnassignedVars = true; // Track unassigned variable warnings
if($Debug::TrackFunctionCalls == "")
	$Debug::TrackFunctionCalls = false; // Track all function calls (VERY verbose, use sparingly)
if($Debug::TrackFunctionStats == "")
	$Debug::TrackFunctionStats = false; // Track function performance stats

function InitDebugBroadcast()
{
	dbecho($dbechoMode, "InitDebugBroadcast()");
	
	if(!$Debug::Enabled)
	{
		// Debug is disabled by default - inform user how to enable it
		echo("Debug broadcast is disabled (default).");
		echo("To enable: Type 'debugon' or 'Debug::On()' in console.");
		echo("Or set $Debug::Enabled = true; in ServerPrefs.cs to auto-enable on server start.");
		return;
	}
	
	if($Debug::Port == "")
		$Debug::Port = 1123; // Default port
	
	echo("===== Initializing Debug UDP Broadcast on port " @ $Debug::Port @ " =====");
	
	// Start broadcasting debug messages to Python listener
	broadcastDebugPort($Debug::Port);
	
	// Enable function tracking for stack analysis (tracks unassigned variables)
	if($Debug::TrackUnassignedVars)
	{
		Debug::functions();
		echo("Debug: Tracking unassigned variables enabled");
	}
	
	echo("===== Debug broadcast initialized =====");
	echo("Make sure Python debug listener is running on port " @ $Debug::Port @ " to capture messages.");
	echo("Use Debug:: functions to control what is tracked (see DebugInit.cs)");
}

// ====================
// Debug Control Functions
// ====================

// Enable/disable debug tracking at runtime
function Debug::Enable()
{
	if($Debug::Enabled)
	{
		echo("Debug tracking is already ENABLED");
		return;
	}
	
	$Debug::Enabled = true;
	
	if($Debug::Port == "")
		$Debug::Port = 1123;
	
	broadcastDebugPort($Debug::Port);
	if($Debug::TrackUnassignedVars)
		Debug::functions();
	
	echo("===== Debug tracking ENABLED =====");
	echo("Port: " @ $Debug::Port);
	echo("Make sure Python listener is running!");
	echo("Type 'Debug::Off()' or 'debugoff' to disable");
}

function Debug::Disable()
{
	if(!$Debug::Enabled)
	{
		echo("Debug tracking is already DISABLED");
		return;
	}
	
	$Debug::Enabled = false;
	// Note: Can't easily disable broadcastDebugPort without restart
	// But can disable function tracking
	echo("===== Debug tracking DISABLED =====");
	echo("Note: UDP broadcast may continue until server restart");
	echo("Type 'Debug::On()' or 'debugon' to enable again");
}

// Simple toggle function
function Debug::Toggle()
{
	if($Debug::Enabled)
		Debug::Disable();
	else
		Debug::Enable();
}

// Convenient console aliases for quick access
function debugon()
{
	Debug::Enable();
}

function debugoff()
{
	Debug::Disable();
}

function debugtoggle()
{
	Debug::Toggle();
}

// Control unassigned variable tracking
function Debug::EnableUnassignedVars()
{
	$Debug::TrackUnassignedVars = true;
	Debug::functions();
	echo("Debug: Unassigned variable tracking ENABLED");
}

function Debug::DisableUnassignedVars()
{
	$Debug::TrackUnassignedVars = false;
	// Note: Can't easily disable Debug::functions() without restart
	// But setting flag helps with configuration
	echo("Debug: Unassigned variable tracking DISABLED (restart required to fully disable)");
}

// Display function performance statistics
// Shows which functions take the most time and are called most frequently
function Debug::ShowFunctionStats()
{
	if(!$Debug::Enabled)
	{
		echo("Debug is not enabled. Enable it first.");
		return;
	}
	
	if($Debug::TrackFunctionStats)
	{
		Debug::DisplayFunctionStats();
	}
	else
	{
		echo("Function stats tracking is disabled. Set $Debug::TrackFunctionStats = true; and restart to enable.");
	}
}

// Reset function statistics counters
function Debug::ResetStats()
{
	if($Debug::Enabled)
	{
		Debug::ResetFunctionStats();
		echo("Debug: Function statistics reset");
	}
	else
	{
		echo("Debug is not enabled.");
	}
}

// Quick debug session - enable everything for a short period
function Debug::QuickSession(%duration)
{
	if(%duration == "")
		%duration = 60; // Default 60 seconds
	
	echo("Starting quick debug session for " @ %duration @ " seconds...");
	$Debug::Enabled = true;
	broadcastDebugPort($Debug::Port);
	if($Debug::TrackUnassignedVars)
		Debug::functions();
	
	// Auto-disable after duration
	schedule("Debug::Disable(); echo(\"Quick debug session ended.\");", %duration);
	echo("Quick debug session will end in " @ %duration @ " seconds");
}

// ====================
// Debug Search Helpers
// ====================

// Helper to search for specific function patterns in logs
// Usage: Debug::SearchFor("Item::giveItem") - tells you what to look for in Python listener
function Debug::SearchFor(%pattern)
{
	if(%pattern == "")
	{
		echo("Usage: Debug::SearchFor(\"functionName\")");
		echo("Example: Debug::SearchFor(\"Item::giveItem\")");
		echo("Example: Debug::SearchFor(\"ERROR\")");
		return;
	}
	
	echo("=========================================");
	echo("Searching debug output for: " @ %pattern);
	echo("=========================================");
	echo("In your Python listener, look for lines containing: " @ %pattern);
	echo("Common patterns to search for:");
	echo("  - Function names: Debug::SearchFor(\"Item::giveItem\")");
	echo("  - Errors: Debug::SearchFor(\"ERROR\")");
	echo("  - Unassigned vars: Debug::SearchFor(\"Variable.*unassigned\")");
	echo("  - Specific files: Debug::SearchFor(\"itemevents\")");
	echo("=========================================");
}

// Helper to show current debug configuration
function Debug::Status()
{
	echo("=========================================");
	echo("Debug Configuration Status");
	echo("=========================================");
	%enabled = "NO";
	if($Debug::Enabled)
		%enabled = "YES";
	echo("Enabled: " @ %enabled);
	echo("Port: " @ $Debug::Port);
	
	%trackVars = "NO";
	if($Debug::TrackUnassignedVars)
		%trackVars = "YES";
	echo("Track Unassigned Variables: " @ %trackVars);
	
	%trackCalls = "NO";
	if($Debug::TrackFunctionCalls)
		%trackCalls = "YES";
	echo("Track Function Calls: " @ %trackCalls);
	
	%trackStats = "NO";
	if($Debug::TrackFunctionStats)
		%trackStats = "YES";
	echo("Track Function Stats: " @ %trackStats);
	echo("=========================================");
	echo("Available commands:");
	echo("  Debug::On() / debugon - Enable debug tracking");
	echo("  Debug::Off() / debugoff - Disable debug tracking");
	echo("  Debug::Toggle() / debugtoggle - Toggle debug on/off");
	echo("  Debug::Status() - Show current configuration");
	echo("  Debug::QuickSession(60) - Enable for 60 seconds");
	echo("  Debug::SearchFor(\"pattern\") - Help finding patterns");
	echo("  Debug::ShowFunctionStats() - Show performance stats");
	echo("  Debug::ResetStats() - Reset performance counters");
	echo("=========================================");
}

// Alias functions for easier console access
function Debug::On()
{
	Debug::Enable();
}

function Debug::Off()
{
	Debug::Disable();
}

