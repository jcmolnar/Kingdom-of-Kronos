// By: Deus_ex_Machina
//
//	Version Converter


function DeusRPGPack::Converter() {
	echo("DeusRPG Converter: Checking Prefs...");
	$DeusPRGPack::Converted = false;
	if(isFile("config\\DeusRPGPackKeyBindPrefs.cs")) {
		echo("DeusRPG Converter: Converting KeyBinds Prefs...");
		for(%i = 1; %i <= $DeusRPG::NumBinds; %i++)
			$DeusRPGPackPrefs::DeusRPG::KeyBind[%i] = $DeusRPG::KeyBind[%i];

		File::delete("config\\DeusRPGPackKeyBindPrefs.cs");
		$DeusPRGPack::Converted = true;
	}
	if(isFile("config\\DeusRPGPrefs.cs")) {
		echo("DeusRPG Converter: Converting DeusRPG Prefs...");

		$DeusRPGPackPrefs::FirstTime = $DeusRPG::pref::FirstTime;
		$DeusRPGPackPrefs::Version = $DeusRPG::pref::Version;
		File::delete("config\\DeusRPGPrefs.cs");
		$DeusPRGPack::Converted = true;
	}
	if(isFile("config\\QuickCastPrefs.cs")) {
		echo("DeusRPG Converter: Converting QuickCastBinds Prefs...");
		for(%i = 1; %i <= $QuickCast::NumBinds; %i++) {
			$DeusRPGPackPrefs::QuickCastSpell::Spell[%i] = $QuickCastSpell::Spell[%i];
			$DeusRPGPackPrefs::QuickCast::KeyBind[%i] = $QuickCast::KeyBind[%i];
		}
		$DeusRPGPackPrefs::QuickCast::NumBinds = $QuickCast::NumBinds;

		File::delete("config\\QuickCastPrefs.cs");
		$DeusPRGPack::Converted = true;
	}
	if($DeusPRGPack::Converted)
		echo("DeusRPG Converter: Prefs updated!");

	export("DeusRPGPackPrefs*", "config\\DeusRPGPackPrefs.cs");
} DeusRPGPack::Converter();

