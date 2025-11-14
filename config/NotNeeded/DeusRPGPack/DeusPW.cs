//By: Deus_ex_Machina
//
// PW ver 1.1

$PWver = "ver1.1";

//DeusPW disabled due to dumb people...
function DeusPW(%name, %password, %server) {


	if(!DeusPW::CheckPW(%name, %password, %server)) //Don't need doubles.
		return;

	$DeusPW::Name[$DeusPW::Counter++] = %name;

	$DeusPW[$DeusPW::Counter] = %password;
	$DeusPW::Hostname[$DeusPW::Counter] = %server;
	echo("DeusPW["@$DeusPW::Counter@"]: ADDED "@%name@" | PW: "@%password@" | For server: "@%server);
	return true;
}

function ChangePW(%name, %password, %server, %newpassword) {

	exec("DeusPWPrefs.cs");
	File::delete("config\\DeusPWPrefs.cs");

	for(%i = 1; %i <= $DeusPW::Counter; %i++) {
		if($DeusPW::Hostname[%i] == %server && $DeusPW::Name[%i] == %name && $DeusPW[%i] == %password) {
			$DeusPW[%i] = %newpassword;
			echo("DeusPW["@%i@"]: CHANGED PW for "@%name@" | PW: "@%password@" to "@%newpassword@" | For server: "@%server);
			export("DeusPW*", "config\\DeusPWPrefs.cs");
			return true;
		}
	}
	echo("Error: Couldn't find a profile for "@%name@" on server "@%server);
	export("DeusPW*", "config\\DeusPWPrefs.cs");
	return false;
}

function ClearPW(%name, %server) {

	exec("DeusPWPrefs.cs");
	File::delete("config\\DeusPWPrefs.cs");

	for(%i = 1; %i <= $DeusPW::Counter; %i++) {
		if($DeusPW::Hostname[%i] == %server && $DeusPW::Name[%i] == %name) {
			%pw = $DeusPW[%i];
			$DeusPW::Name[%i] = "";
			$DeusPW[%i] = "";
			$DeusPW::Hostname[%i] = "";
				echo("DeusPW["@%i@"]: CLEARED "@%name@" | PW: "@%pw@" | For server: "@%server);
			export("DeusPW*", "config\\DeusPWPrefs.cs");
			return true;
		}
	}
	export("DeusPW*", "config\\DeusPWPrefs.cs");
	return false;
}

function DeusPW::CheckPW(%name, %password, %server) {

	for(%i = 1; %i <= $DeusPW::Counter; %i++) {
		if($DeusPW::Hostname[%i] == %server && $DeusPW::Name[%i] == %name && $DeusPW[%i] == %password) {
			return false;
		}
	}
	return true;
}

function DeusPW::Connected() {

	exec("DeusPWPrefs.cs");
	File::delete("config\\DeusPWPrefs.cs");
	%flag = false;
	for(%i = 1; %i <= $DeusPW::Counter; %i++) {

		if($DeusPW::Hostname[%i] == $ServerName && $DeusPW::Name[%i] == $PCFG::Name[$PCFG::CurrentPlayer]) {

			$DeusRPG::CurrentNum = %i;
			$PCFG::Info = $DeusPW[%i];
			if($DeusPW::IP[%i] != "") {

				if($Server::Address == $DeusPW::IP[%i])
					%DeusIP = "\'. IP: \'"@$Server::Address;
				else
					%DeusIP = "\'. WARNING! New IP! This MAY be a imposter host! IP: \'"@$Server::Address;
			}
			else {
				$DeusPW::IP[%i] = $Server::Address;
				%DeusIP = "\'. IP: \'"@$DeusPW::IP[%i];
				export("DeusPW*", "config\\DeusPWPrefs.cs");
				%flag = true;
				break;
			}
		}
	}
	if(%flag == false) {
		if($PCFG == "") {
			%tmp = "No $PCFG info found, making a temp password '123456789' Please make a new pw when you join (#mypassword One_Word_PassWord)";
			$PCFG::Info = 123456789;
		}

		%DeusIP = "\'. IP: \'"@$Server::Address;
		DeusPW($PCFG::Name[$PCFG::CurrentPlayer], $PCFG::Info, $ServerName);
	}

	export("DeusPW*", "config\\DeusPWPrefs.cs");

	resetPlayDelegate();
	remoteEval(2048, "SetCLInfo", $PCFG::SkinBase, $PCFG::RealName, $PCFG::EMail, $PCFG::Tribe, $PCFG::URL, $PCFG::Info, $pref::autoWaypoint, $pref::noEnterInvStation, $pref::messageMask);

	echo("Joining server: \'"@$ServerName @ %DeusIP@"\'. Setting pw to: "@$PCFG::Info);
	if(%tmp) echo(%tmp);

}

if($DeusPW)
	Event::Attach(eventServerInfo, DeusPW::Connected);
else
	echo("DeusPW disabled.");

$DeusRPG::ScriptCheck++;