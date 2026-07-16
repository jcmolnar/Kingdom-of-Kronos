// PluginLoader Info:
// -----------------------------------------------------------------------------
// CommandLine Forced Load or No-Load < Over-rides Script-Based Load  >
// -----------------------------------------------------------------------------
// Forced Load Example: Tribes.exe +p DoSFix
// Forced No-Load Example: Tribes.exe -p DoSFix
// Multiple Plugin Example: Tribes.exe +p DoSFix,crcBypass,etc.
// -----------------------------------------------------------------------------
// Script-Based Load
// Example: $PluginLoader::DoSFix = true;
// -----------------------------------------------------------------------------
if($dedicated) {
	$PluginLoader::DoSFix = true;
	$PluginLoader::ClientSideAddonPlugin = false;
	$PluginLoader::kronosfix_server = true;
	$PluginLoader::kronos_playermanager = true;
	$PluginLoader::kronos_aiteardown = true;
	$PluginLoader::ForceExitPlugin = true;  // forceExit() hard exit - Down() uses it so hung mem.dll threads can't block the InfiniteSpawn restart
	$PluginLoader::kronos_datetime = true;  // getRealDate()/getRealTime()/getRealDayOfWeek() - daily quest rotation day stamp
	$PluginLoader::kronos_virtualitems = true;  // Phase D: per-client ItemData push (vslotPushItem) - belt weapons in the stock inventory for vanilla clients. Required by $pref::VSlotsEnabled (VirtualSlots.cs); read-only probes (vslotDbm/Peek) also available
	$PluginLoader::kronos_reprobe = true;  // READ-ONLY T1Vista RE confirmation probe: reGlobals()/reItem()/reClient()/reFindCall() - SEH-guarded reads, posts/writes/hooks nothing, cannot crash (DEV)
}
else {
	$PluginLoader::DoSFix = false; //Because dosfix doesn't play nice with special chats
	$PluginLoader::ClientSideAddonPlugin = true;
}

$PluginLoader::MathPlugin = true;
$PluginLoader::StringPlugin = true;
$PluginLoader::GraphicPlugin = true;
$PluginLoader::PatchesPlugin = true;
$PluginLoader::CommLinkPlugin = true;
$PluginLoader::BovExpansionPlugin = true;
$PluginLoader::ServerSidePlugin = true;
//$PluginLoader::TribesXT = true;  // Disabled - conflicts with existing plugins
//$PluginLoader::PlayerManagerPlugin = true;  // EXILED to Plugins\Exiled\ - used the blocked plugin_open convention, never loaded; kronos_playermanager.dll replaced it

// Load Script-based Implementation since Native DLL is blocked
// Execution from 'config' folder (which is in search path)
echo("Loading support_getFreeId.cs...");
exec("support_getFreeId.cs");