//-----------------------------------------------------------------------------
// RemoteAdminHelpers.cs - stable entry points for the remote web admin console
//-----------------------------------------------------------------------------
// The web console reaches the server through the engine telnet rcon (see
// config/RemoteConsole.cs), which runs raw TorqueScript. These functions give
// the UI (a) parse-friendly listings for its pickers and (b) thin, named
// wrappers around existing admin actions so buttons call one clean function
// instead of reconstructing chat commands. No new game logic lives here - every
// wrapper delegates to code that already exists.
//
// All list output is fenced between  <TAG>  and  </TAG>  markers on their own
// lines so the Node app can slice the exact block out of the telnet stream.
//-----------------------------------------------------------------------------

// List every connected client as  id|name|adminLevel|isAi  (one per line).
// Uses GetEveryoneIdList() - the same enumerator NEWgetClientByName() uses.
function AdminListClients()
{
	echo("<CLIENTS>");
	%list = GetEveryoneIdList();
	for(%i = 0; GetWord(%list, %i) != -1; %i++)
	{
		%id = GetWord(%list, %i);
		%name = Client::getName(%id);
		%lvl = floor(%id.adminLevel);
		%isAi = 0;
		if(Player::isAiControlled(%id))
			%isAi = 1;
		echo(%id @ "|" @ %name @ "|" @ %lvl @ "|" @ %isAi);
	}
	echo("</CLIENTS>");
}

// List a player's stored economy/inventory values as  KEY|VALUE  (one per line).
// Reads the existing storeData/fetchData funkvar store (Banking.cs et al.). Extend
// %keys with any funkvar you want surfaced in the UI restore panel.
function AdminListItems(%clientId)
{
	echo("<ITEMS " @ %clientId @ ">");
	if(%clientId == -1 || %clientId == "")
	{
		echo("ERROR|invalid clientId");
		echo("</ITEMS>");
		return;
	}
	%keys = "COINS BANK EXP LEVEL";
	for(%i = 0; GetWord(%keys, %i) != -1; %i++)
	{
		%k = GetWord(%keys, %i);
		echo(%k @ "|" @ fetchData(%clientId, %k));
	}
	echo("</ITEMS>");
}

//-----------------------------------------------------------------------------
// Action wrappers (each delegates to existing code)
//-----------------------------------------------------------------------------

// Graceful restart: Down() warns players, saves characters + world, then exits;
// InfiniteSpawn relaunches the process. %minutes 0 = immediate countdown. See
// rpgfunk.cs Down()/FinalizeShutdownAndExit(). Pass a large value to abort? No -
// use AdminRestartCancel(). Returns a status string for the UI.
function AdminRestart(%minutes)
{
	if(%minutes == "")
		%minutes = 1;
	Down(%minutes);
	return "OK|restart scheduled in " @ %minutes @ " min";
}

// Immediate hard restart (no countdown/save beyond Down's own path). Uses the
// ForceExitPlugin forceExit(); InfiniteSpawn brings the server back up.
function AdminRestartNow()
{
	Down(0);
	return "OK|immediate restart";
}

// Give/restore items to a player. %stuff is the same payload #givethisstuff
// accepts. Delegates to GiveThisStuff() (comchat.cs #givethisstuff path).
function AdminGiveStuff(%clientId, %stuff)
{
	if(%clientId == -1 || %clientId == "")
		return "ERROR|invalid clientId";
	GiveThisStuff(%clientId, %stuff, True);
	if(Player::isAiControlled(%clientId))
		HardcodeAIskills(%clientId);
	return "OK|gave " @ %clientId @ ": " @ %stuff;
}

// Kick (%ban=0) or ban (%ban=1) a client. Admin::kick with admin=-1 acts as the
// server/super-admin; BanList::add persists to config/banlist.cs.
function AdminKick(%clientId, %ban)
{
	if(%clientId == -1 || %clientId == "")
		return "ERROR|invalid clientId";
	Admin::kick(-1, %clientId, %ban);
	%word = "kicked ";
	if(%ban)
		%word = "banned ";
	return "OK|" @ %word @ %clientId;
}

// Set a client's admin level (0-5). Sets the same field #setadmin does.
function AdminSetLevel(%clientId, %level)
{
	if(%clientId == -1 || %clientId == "")
		return "ERROR|invalid clientId";
	%clientId.adminLevel = %level;
	return "OK|" @ %clientId @ " adminLevel=" @ %level;
}

// Broadcast a server announcement to everyone (messageAll).
function AdminAnnounce(%text)
{
	messageAll(0, %text);
	return "OK|announced";
}

echo("[RCON] RemoteAdminHelpers loaded (AdminListClients/AdminListItems/AdminRestart/AdminGiveStuff/AdminKick/AdminSetLevel/AdminAnnounce).");
