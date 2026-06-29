// AutoRestart.cs - Automated Server Restart System
// Schedules a server restart every 60 hours with staggered player warnings.

function InitAutoRestart()
{
	echo("===== Automated Server Restart System Initializing =====");
	
	// Total cycle: 60 hours
	// 60 hours = 216,000 seconds
	
	// 24 Hour Warning
	schedule("messageAll(1, \"[SERVER] Automated restart scheduled in 24 hours.\");", 129600);
	
	// 12 Hour Warning
	schedule("messageAll(1, \"[SERVER] Automated restart scheduled in 12 hours.\");", 172800);
	
	// 1 Hour Warning
	schedule("messageAll(1, \"[SERVER] Automated restart scheduled in 1 hour.\");", 212400);
	
	// 30 Minute Warning
	schedule("messageAll(1, \"[SERVER] Automated restart scheduled in 30 minutes.\");", 214200);
	
	// 10 Minute Warning
	schedule("messageAll(1, \"[SERVER] Automated restart scheduled in 10 minutes.\");", 215400);
	
	// Final 5 Minute Warning and Shutdown sequence
	// This triggers the existing Down(%minutes) function in rpgfunk.cs
	// 59 hours, 55 minutes = 215,700 seconds
	schedule("Down(5);", 215700);
	
	echo("===== Automated Server Restart scheduled for 60 hours from now =====");
}
