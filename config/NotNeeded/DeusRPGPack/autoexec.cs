//If you can't get my stuff working.. try this autoexec.cs
//Just copy it to your TRIBES\config\
//And over-write/back-up your old one.
echo(">> Loading Autoexec.cs");
$console::logmode = 0;	//Set this to 1 to log all your msg that go in thru your console..(This can slow you down bad when it gets big... not good to have one with a slow machine)
editActionMap("playmap.sae");
exec("presto\\Install.cs");
Include("Autoload.cs");
exec("DeusRPGPack\\DeusInstall.cs");
Include("DeusRPGPack\\BattleHUD\\Install.cs");
Include("DeusRPGPack\\DeusAdminExporter.cs");
exec("presto\\Install.cs");
Include("DeusRPGPack\\DeusInstall.cs");


bindCommand(mouse0, zaxis0, TO, "nextWeapon();"); //Wheel forward	May or may not work for you..
bindCommand(mouse0, zaxis1, TO, "prevWeapon();"); //Wheel backward



// bindCommand(keyboard0, make, "space", TO, "use(\"Crystal Blue Potion\");");
// bindCommand(keyboard0, make, "alt", TO, "use(\"Blue Potion\");");
// bindCommand(keyboard0, make, "z", TO, "remoteeval(2048, say(0, \"#steal\"));");


// DeusPW("Deus_ex_Machina", "My_Password_Here", "JI & PoG's RPG");
// ClearPW("Deus_ex_Machina", "JI & PoG's RPG");





if(!$error || $DeusInstallLoaded)
	echo(">> Loaded Autoexec.cs No errors found.");
else
	echo(">> Loaded Autoexec.cs But with errors.");