// By: Deus_ex_Machina
//
//	Install 1.0

//Start Install
if($RPGPackver >= 2.2) {
	exec("DeusRPGPack\\BattleHUD\\BattleHUDKeyBinds.cs");
	exec("DeusRPGPack\\BattleHUD\\BattleHUD.cs");
	//exec("DeusRPGPack\\BattleHUD\\BattleHUDCustom.cs");
}
else
	echo("BattleHUD needs DeusRPGPack 2.2 or greater to work!");
//
//End Install
$BattleHUDver = "1.12";
Presto::AddScriptBanner(BattleHUDBanner, "<f2><jc>BattleHUD <f1>version <f2>"@$BattleHUDver@"\n<f0>Done by:\n<f1>Deus_ex_Machina<jl>\n\n <f0>Stats for <f1>"@$BattleHUD::stats::PlayerName@"\n <f0>Bots <f1>"@$BattleHUD::stats::Bots[$BattleHUD::stats::PlayerName]@"\n <f0>PKs <f1>"@$BattleHUD::stats::PKs[$BattleHUD::stats::PlayerName]@"\n <f0>Suicide <f1>"@$BattleHUD::stats::Self[$BattleHUD::stats::PlayerName]@"\n <f0>Spells casted <f1>"@$BattleHUD::stats::Spells[$BattleHUD::stats::PlayerName]);