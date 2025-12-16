
// =============================================================================
// ENGINE BUG FIX: Dedicated Server Crash on Exit
// =============================================================================
// The Tribes engine (FearMain::onExit) unconditionally calls GuiLoadContentCtrl(MainWindow, "quit.gui")
// on shutdown. On dedicated servers, MainWindow does not exist, causing a crash/freeze.
//
// By defining this function ONLY on dedicated servers, we shadow the engine command
// and safely ignore the call, preventing the crash.
// -----------------------------------------------------------------------------
if($dedicated)
{
	function GuiLoadContentCtrl(%window, %content)
	{
		// Shadowing engine command to prevent crash on dedicated server exit.
		// Do nothing.
		if($debugThis) echo("Blocked engine call to GuiLoadContentCtrl(" @ %window @ ", " @ %content @ ") on dedicated server.");
	}
	echo("Engine Fix: GuiLoadContentCtrl override enabled for dedicated server.");
}
