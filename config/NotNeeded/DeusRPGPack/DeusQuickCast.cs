// By: Deus_ex_Machina
//
//	QuickCast 1.33

$DeusQuickCastver = "ver1.33";
// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
// This function sets the binding to what the user specifies from the ChatBind menu.
// It uses a global variable ($DeusChatBind::QuickMenu) to determine what key it is
// setting. This is to automate the process of making menus and having those menus
// set the right keybinding. I found that trying to use variables to much while building
// the menus made them crash (Presto Pack problem?). This also helps with adding whatever
// number of keys the user wants to specify in the DeusKeyBinds.cs file for Quick Casts.
// Read that file for more information on adding/removing key bindings.
// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
function QuickCast::Set(%NewSpell) {
	$DeusRPGPackPrefs::QuickCastSpell::Spell[$DeusChatBind::QuickMenu] = %NewSpell;

	QuickCast::Save();

	Client::centerPrint("<jc><f0>Setting <f1>" @ $DeusRPGPackPrefs::QuickCast::KeyBind[$DeusChatBind::QuickMenu] @ " <f0>-Quick Cast to<f1> " @ $DeusRPGPackPrefs::QuickCastSpell::Spell[$DeusChatBind::QuickMenu], 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}


// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
// This function is called from the keybinding. The key bindings are set according to what
// is in the DeusKeyBinds.cs file. Read that file for more information on adding/removing
// key bindings.
// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
function QuickCast::UseCast(%CastSet)
{
	say(0, "#cast " @ $DeusRPGPackPrefs::QuickCastSpell::Spell[%CastSet]);
}


// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
// This function prints the current settings to the users play screen.
// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
function QuickCast::Info() {
	for(%i = 1; %i <= $DeusRPGPackPrefs::QuickCast::NumBinds; %i++)
	{
		if(%PrintLine == "")
			%PrintLine = "<jc><f0>Key <f1>" @ $DeusRPGPackPrefs::QuickCast::KeyBind[%i] @ " <f0>is Spell <f1>" @ $DeusRPGPackPrefs::QuickCastSpell::Spell[%i];
		else
			%PrintLine = %PrintLine @ "\n<jc><f0>Key <f1>" @ $DeusRPGPackPrefs::QuickCast::KeyBind[%i] @ " <f0>is Spell <f1>" @ $DeusRPGPackPrefs::QuickCastSpell::Spell[%i];
	}

	Client::centerPrint(%PrintLine, 1);
	Schedule("Client::centerPrint(\"\", 1);", 10);
}


// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
// This function saves the settings that are made with the menus. Also, it saves the
// settings on exitting the game, in case of problems.
// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
function QuickCast::Save() {
	export("$DeusRPGPackPrefs::*", "config\\DeusRPGPackPrefs.cs");
}


// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
for(%i = 1; %i <= $DeusRPGPackPrefs::QuickCast::NumBinds; %i++)
	bindkey(play, $DeusRPGPackPrefs::QuickCast::KeyBind[%i], "QuickCast::UseCast(" @ %i @ ");");

$DeusRPG::ScriptCheck++;
