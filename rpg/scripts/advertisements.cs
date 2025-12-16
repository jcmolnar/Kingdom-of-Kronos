// ============================================================================
// ADVERTISEMENTS SYSTEM - Order-Based
// Ads play in the order they are defined below (top to bottom)
// To add a new ad: Just call AddAd("line1", "line2", ...) anywhere
// To insert between existing ads: Just add a new AddAd() call where you want it
// ============================================================================

// Reset ads when file loads (for hot-reload support)
$AdList = "";
$AdLineCount = "";
$AdTotalCount = 0;

// Helper function to register an ad (supports up to 5 lines per ad)
function AddAd(%line0, %line1, %line2, %line3, %line4)
{
	$AdTotalCount++;
	%idx = $AdTotalCount;
	
	// Store lines
	$AdLine[%idx, 0] = %line0;
	$AdLine[%idx, 1] = %line1;
	$AdLine[%idx, 2] = %line2;
	$AdLine[%idx, 3] = %line3;
	$AdLine[%idx, 4] = %line4;
}

// ============================================================================
// DEFINE YOUR ADS HERE - Just add/remove/reorder AddAd() calls as needed
// Ads will play in the order they appear (top to bottom)
// ============================================================================

AddAd("Welcome to Kingdom of Kronos - Hosted by Jobo",
      "       Active development by Jobo");

AddAd("NEW DUNGEON ALERT!",
      "The Void has been added as the newest high level dungeon",
      "This one might be a little tricky to find your way into...",
      "Hint 1: Think of an upside down Stone Henge entrance");

AddAd("Join the TRPG Discord Server!",
      "https://discord.gg/r3CGT9TY");

AddAd("For a list of Armor/Spells/Items/Zones",
      "Use the Help Commands in your tab menu");

AddAd("BACKPACK IMPLEMENTED",
      "Access it via option 2 in the tab menu",
      "Use #bugreport if it's not working");

AddAd("Credits:Asnabel,Superfat,Panda,Scorpion,Xan and DeraJ",
      "They all put their part into helping.");

AddAd("Critical Hit Skill Added",
      "Depending on your skill level, your chance",
      "at getting a 2X damage hit will increase.");

AddAd("This server has been doned hack proof by:",
      "Corona and Shorty.");

AddAd("Special thanks to Superfat for keeping the mod from dying on a hard drive.");

AddAd("Check out STANCES in the tab menu!");

AddAd("Remort seal implemented - every 20 remorts",
      "adventurers will need to venture in and break it!");

AddAd("House Artifacts and Bases NOW SAVE",
      "They will restore if the server crashes or restarts.",
      "Let the battles begin!");

AddAd("The server has been updated to v0.8.1",
      "Please report any bugs to the TRPG Discord Server",
      "In the KoK mod channel",
      "Or use the #bugreport command");

AddAd("Want to suggest a feature or idea?",
      "Use #featurerequest or #requestfeature",
      "Your ideas could be added to the server!");

AddAd("Vehicle Combat Skill Added",
      "Every 100 points in the skill, Scout damage increases by 1.",
      "The skill is capped at 1000 points.");

AddAd("LUCK toggle in tab menu now shows DEATH or MISS",
      "It also shows a description when you choose each one",
      "Please ensure you have the right one set");

// ============================================================================
// ADVERTISEMENT ENGINE - Don't modify below unless you know what you're doing
// ============================================================================

function Advertize(%stage)
{
	// Check if advertisements have been stopped (for manual re-execution)
	if($AdvertizeStopped == true)
	{
		echo("Advertize: Previous execution stopped, exiting scheduled call");
		return;
	}
	
	// Wrap around
	if(%stage < 1) %stage = 1;
	if(%stage > $AdTotalCount) %stage = 1;
	
	echo("Advertize stage " @ %stage @ " of " @ $AdTotalCount);
	
	// Display all lines for this ad
	for(%line = 0; %line < 5; %line++)
	{
		%text = $AdLine[%stage, %line];
		if(%text != "")
			Messageall(2, %text);
	}
	
	%stage++;
	schedule("Advertize(" @ %stage @ ");", 300);
}

// When file is manually executed, stop any existing advertisements and start fresh
if($AdvertizeActive == true)
{
	echo("Advertize: Stopping previous advertisement schedule...");
	$AdvertizeStopped = true;
	// Wait a moment for any scheduled calls to check the flag, then reset
	schedule("$AdvertizeStopped = false; $AdvertizeActive = true; Advertize(1);", 1);
}
else
{
	$AdvertizeActive = true;
	$AdvertizeStopped = false;
	Advertize(1);
}
