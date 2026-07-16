//-----------------------------------------------------------------------------
// RemoteConsole.cs - enable the engine's built-in Telnet remote console (rcon)
//-----------------------------------------------------------------------------
// The Sim engine ships a SimTelnetPlugin: setting $TelnetPort nonzero opens a
// TCP listener; a client authenticates against $TelnetPassword and every line
// it then sends is run through console->evaluate() - full TorqueScript, exactly
// like typing at the physical server console.
//
// This backs the remote web admin console. The Node admin app (same PC) is the
// ONLY intended telnet client and connects to 127.0.0.1:$TelnetPort.
//
// SECURITY: the plugin binds INADDR_ANY with a plaintext password, so the port
// must be firewalled to localhost (see docs/RemoteAdminConsole-Runbook.md). The
// Cloudflare tunnel never exposes it - cloudflared only routes hostnames listed
// in config.yml. The password lives in the gitignored RemoteConsole.local.cs so
// it is never committed. Exec'd from Server.cs's script-load chain.
//-----------------------------------------------------------------------------

$TelnetPort = 28001;

// Load the real password from the local (gitignored) secret file. If it is
// missing the port stays open with no valid password set - so we refuse to arm
// the listener unless the secret is present.
if(isFile("config\\RemoteConsole.local.cs"))
{
	// NOTE: exec() loads via the ResourceManager by BASENAME (console.cpp c_exec),
	// not by raw path - an explicit "config\..." subpath fails as "invalid script
	// file". config/ is already a scanned resource path (same as TaurikAdmins.cs),
	// so the basename resolves. isFile() above still needs the real relative path.
	exec("RemoteConsole.local.cs");
	echo("[RCON] Telnet remote console armed on 127.0.0.1:" @ $TelnetPort);
}
else
{
	$TelnetPort = 0;
	echo("[RCON] config/RemoteConsole.local.cs missing - telnet console DISABLED.");
	echo("[RCON] Copy RemoteConsole.local.cs.example to RemoteConsole.local.cs and set a password.");
}
