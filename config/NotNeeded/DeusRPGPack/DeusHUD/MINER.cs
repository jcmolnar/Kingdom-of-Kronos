//By : Deus_ex_Machina
//
//	RPGHUDs ver 2.561

//===============================
//GemHUD by Aleman
//Re-edited by Deus_ex_Machina
//===============================

// Gem data is broken up into:
// $DeusRPGHud::Gem[-Number-, Desc]
// $DeusRPGHud::Gem[-Number-, Count]
// $DeusRPGHud::Gem[-Number-, PerTotal]
//
// e.g.
// $DeusRPGHud::Gem[1, Desc] would = "SmallRock"
// $DeusRPGHud::Gem[1, Count] would = how many they have on them
// $DeusRPGHud::Gem[1, PerTotal] would = how much of the total worth of all
//                                       gems does this particular category of gem cover

$DeusRPGHud::Gem[1, Desc] = "SmallRock";
$DeusRPGHud::Gem[2, Desc] = "Quartz";
$DeusRPGHud::Gem[3, Desc] = "Granite";
$DeusRPGHud::Gem[4, Desc] = "Opal";
$DeusRPGHud::Gem[5, Desc] = "Jade";
$DeusRPGHud::Gem[6, Desc] = "Turquoise";
$DeusRPGHud::Gem[7, Desc] = "Ruby";
$DeusRPGHud::Gem[8, Desc] = "Topaz";
$DeusRPGHud::Gem[9, Desc] = "Sapphire";
$DeusRPGHud::Gem[10, Desc] = "Gold";
$DeusRPGHud::Gem[11, Desc] = "Emerald";
$DeusRPGHud::Gem[12, Desc] = "Diamond";
$DeusRPGHud::Gem[13, Desc] = "Keldrinite";

$DeusRPGHud::GemCount = 13;

function DeusRPGHud::UpdateDeusMINERhud(%hud) {
	%DeusRPGHud::TotalGems = 0;
	$DeusRPGHud::AmountMined = 0;

	for(%i = 1; %i <= $DeusRPGHud::GemCount; %i++)
	{
		if($DeusRPGHud::Gem[%i, Desc] == "SmallRock") //special case for small rocks
			$DeusRPGHud::Gem[%i, Count] = getItemCount("Small rock");
		else
			$DeusRPGHud::Gem[%i, Count] = getItemCount($DeusRPGHud::Gem[%i, Desc]);

		%DeusRPGHud::TotalGems = %DeusRPGHud::TotalGems + $DeusRPGHud::Gem[%i, Count];

		if($DeusRPGHud::Gem[%i, Desc] == "SmallRock")
			$DeusRPGHud::Gem[%i, Amount] = 1 * $DeusRPGHud::Gem[%i, Count];
		else
			$DeusRPGHud::Gem[%i, Amount] = DeusRPG::FetchData("getsellcost " @ $DeusRPGHud::Gem[%i, Desc]) * $DeusRPGHud::Gem[%i, Count];

		$DeusRPGHud::AmountMined+= $DeusRPGHud::Gem[%i, Amount];
	}

	HUD::AddTextLine(%hud, "<f0>Mining stuff: ");

	for(%i = 1; %i <= $DeusRPGHud::GemCount; %i++)
	{
		if($DeusRPGHud::Gem[%i, Count] == 0)
			HUD::AddTextLine(DeusMINERhud, "<f1>" @ $DeusRPGHud::Gem[%i, Desc] @ ": <f2>None");
		else
		{
//			$DeusRPGHud::Gem[%i, PerTotal] = FixDecimals(100*($DeusRPGHud::Gem[%i, Count]/%DeusRPGHud::TotalGems));
			HUD::AddTextLine(DeusMINERhud, "<f1>" @ $DeusRPGHud::Gem[%i, Desc] @ ": <f2>" @ $DeusRPGHud::Gem[%i, Count] @ "<f0> - <f2>$" @ $DeusRPGHud::Gem[%i, Amount] @ "<f0>");
		}
	}

	HUD::AddTextLine(DeusMINERhud, "<f0>Total <f1>$ <f2>" @ $DeusRPGHud::AmountMined);

	return $DeusRPGHud::CBdelay;
}

HUD::New(DeusMINERhud, DeusRPGHud::UpdateDeusMINERhud, $Hud::prefs::last::DeusMINERhud);

$DeusRPGHUD::ScriptCheck++;