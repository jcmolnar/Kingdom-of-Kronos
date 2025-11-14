-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
BattleHUD - ver 1.12
By: Deus_ex_Machina
E-Mail: khris142@yahoo.com

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
Instructions
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

	Unzip to C:\Dynamix\TRIBES\config
		**NOTE** Only if Tribes is installed in	C:\Dynamix\TRIBES\config.
		If not put the files in the TRIBES\config where ever you installed it.

	Should create the following directories:
		C:\Dynamix\TRIBES\config\DeusRPGPack\BattleHUD

	Open your TRIBES\config\autoexec.cs and type the following it it:

	exec("presto\\Install.cs");
	Include("DeusRPGPack\\DeusInstall.cs");
	Include("DeusRPGPack\\BattleHUD\\Install.cs");

Read the BattleHUDKeyBinds.cs to view / change key binds! 

Note my huds work best in 1024x768 res! (thats the res I made my huds in)

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
To Run Tribes Mod RPG, make a Shortcut on your Windows Desktop. Do this by:
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

		Right-Click on desktop
		Select New->Shortcut
		Click Browse and locate your Tribes.exe in your Tribes directory.
		Click OK after you find it.
		At the end of the Command Line box enter the following:
			 -mod RPG
		Should look something like this
		C:\Dynamix\TRIBES\Tribes.exe -mod RPG

		Make sure the is a space between the existing command line and
			what you typed in.
		Click Next.
		Type the name of the Shortcut as Tribes RPG and click Finish.

	Use this shortcut to run Tribes RPG.

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
How to use it.
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

Simple. It'll load on its own when you connect to a server. By default the hud is not 
displayed right away when you join. Press Q (default key) to toggle your hud.

This hud will track:
Bot kills
PKs
Deaths
Suicides
Spells casted (how many times you casted spells)
Play time
Total play time
Damage taken
Damage done

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
To view some stats that are not shown on the hud type these cmds
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

#say !mystats
Will #say your stats
#say !mystats #global
Will say your stats #global
#say !mystats #tell JoeBob
Will #tell your stats to JoeBob

Same with the rest of the chat cmds.

#say !mydmgstats
#say !mytime

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*



