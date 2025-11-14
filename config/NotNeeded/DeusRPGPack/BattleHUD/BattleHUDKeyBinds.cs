// By: Deus_ex_Machina
//
//	KeyBinds 1.0


$BattleBind::KeyBind[0] = "l"; //HUD toggle



//===
EditActionMap("playMap.sae");
bindkey(play, $BattleBind::KeyBind[0], "BattleHUD::toggleHUD();");
