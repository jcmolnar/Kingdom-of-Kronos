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
//$PluginLoader::PlayerManagerPlugin = true;

// Load Script-based Implementation since Native DLL is blocked
// Execution from 'config' folder (which is in search path)
echo("Loading support_getFreeId.cs...");
exec("support_getFreeId.cs");