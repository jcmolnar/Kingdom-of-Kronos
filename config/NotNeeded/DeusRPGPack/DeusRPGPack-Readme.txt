-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
DeusRPGPack - ver 2.274
By: Deus_ex_Machina
E-Mail: khris142@yahoo.com

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
Instructions
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

	Unzip to C:\Dynamix\TRIBES\config
		**NOTE** Only if Tribes is installed in	C:\Dynamix\TRIBES\config.
		If not put the files in the TRIBES\config where ever you installed
		it.

	Should create the following directories:
		C:\Dynamix\TRIBES\config\DeusRPGPack
		C:\Dynamix\TRIBES\config\Presto

	Open your TRIBES\config\autoexec.cs and type the following it it:

	exec("presto\\Install.cs");
	Include("DeusRPGPack\\DeusInstall.cs");


Read the UserOptions and DeusRPGPack\DeusKeyBinds.cs to view or change key binds and options! 

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
If you have my DeusRPGHUDs delete them. This RPGPack has the huds with it :P
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

In game if you want to train your pickpocket or mug with a new thats
not on the menu.
You can set a var to use a different item.
Heres the Var
$Citem

In game press  ~  .
And type it like this
$Citem = "Blue Potion";
or any other item. (Note, $CItem needs to look like the name that
you see in your inv) 

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
	Read the DeusRPGPack\DeusKeyBinds.cs to view or change key binds! 
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

**Note control V is the new chat menu! This menu has all the training and other stuff =]
Very nice because it won't bind over all your keys for training!
And check out the smithing menu I made. Hope you like it!

**Auto mining
When you goto the menu you see

	tAuto Mining: Quit = TRUE
	fAuto Mining: Quit = FALSE

Well...If you start automining using Quit = TRUE
That means when you're over weight, it stops the mining and #recalls and quits Tribes =]

Quit = FALSE
Means it won't =P

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
	How to use DeusPW
-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

If you already have a char on a server. Just add this at the end somewhere of your autoexec.cs (look in the autoexec that cames with this if you don't know where to put it.., or if you don't have an autoexec...)

DeusPW("My_Char_Name_Here", "My_Password_Here", "Server_Name_Here");

Ex:
DeusPW("Deus_ex_Machina", "Blah blah blah...", "JI & PoG's RPG");

When you join a new server(Server with a different name)
DeusPW will make a new password for that character and saves it!


If you ever need to remove a password

ClearPW("My_Char_Name_Here", "Server_Name_Here");

Ex:
ClearPW("Deus_ex_Machina", "JI & PoG's RPG");

If you need to change your password join and press ctrl v, and look for Other Options (key 'o')

Now press 1 and you'll tell yourself to enter a new password (lol)
Type
#tell My_char_Name, MyOneWordNewPass

(Note the , after your name is needed)

Ex:
#tell Deus_ex_Machina, rpg_rules


This will update DeusPW and save the new password.

Also, if you join a server and the IP is new, it'll warn you saying it MAY be an imposter server! (Maybe i'll make something for this later)

One more thing, using this you do not need your password on the select char screen AFTER haveing used this.(Meaning check to see if you have a DeusPWPrefs.cs just in case =P)


***Check the autoexec.cs thats included for other stuff to use! Including auto-save on lvl up!

-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*