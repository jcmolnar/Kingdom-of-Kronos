// Double-execution detection
if($ServerPrefs::AlreadyLoaded)
{
	$ServerPrefs::DoubleExecuted = true;
	echo("===================================================");
	echo("WARNING: ServerPrefs.cs is being executed TWICE!");
	echo("Check your launcher for +exec ServerPrefs.cs");
	echo("===================================================");
}
else
{
	$ServerPrefs::AlreadyLoaded = true;
	echo("ServerPrefs.cs loaded successfully.");
}

// Persistent spam function using SimSet heartbeat
function SimSet::checkDouble(%this)
{
	echo("===================================================");
	echo("WARNING: ServerPrefs.cs was executed TWICE!");
	echo("Check your launcher for +exec ServerPrefs.cs");
	echo("===================================================");
	%this.schedule(500, "checkDouble");
}

$Server::AutoAssignTeams = "true";
$Server::Connect = "<jc><f2>\nWelcome To .:(Kingdom of Kronos v0.6.6 - Development):.\n\nPlease report all bugs to http://tribesrpg.proboards.com";
$Server::CurrentMaster = "0";
$Server::DuplicateIPs = "true";
$Server::FileURL = "http://tribesrpg.org";
$Server::FloodProtectionEnabled = "false";
$Server::HostName = ".:(Kingdom of Kronos v0.6.6 - Development):.";
$Server::HostPublicGame = "true";
$Server::Info = "<jc><f2>Kingdom of Kronos : Remastered v0.6.6\n\nCommunity Forums: http://tribesrpg.proboards.com";
$Server::JoinMOTD = "<jc><f1>Message of the Day:\nWelcome to the Kingdom of Kronos!\n\nEnjoy.";
$Server::CurrentMaster = "0";
$server::MasterAddressN0 = "t1m1.pu.net:28000 t1m2.pu.net:28000 t1m3.pu.net:28000 t1m1.tribes1.co:28000 t1m2.tribes1.co:28000 t1m3.tribes1.co:28000 159.65.169.146:28000";
$Server::MasterName0 = "unofficial";
$Server::XLMasterN0 = "IP:18.217.60.195:28000";
$Server::XLMasterN1 = "IP:144.202.54.147:28000";
$Server::XLMasterN2 = "IP:69.28.91.237:28000";
$Server::XLMasterN3 = "IP:15.204.204.222:28000";
$Server::XLMasterN4 = "IP:51.81.187.48:28000";
$Server::XLMasterN5 = "IP:76.255.198.131:28000";
$Server::XLMasterN6 = "IP:159.65.169.146:28000";
$Server::nummasters = "7" ;
$Server::MaxPlayers = "16";
$Server::MinVotes = "1";
$Server::MinVotesPct = "0.5";
$Server::MinVoteTime = "45";
$Server::numMasters = "7";
$Server::Password = "beta66";
$Server::Port = "28000";
$Server::respawnTime = "2";
$Server::Security = "true";
$Server::TeamDamageScale = "0";
$Server::teamName0 = "Citizen";
$Server::teamName1 = "Enemy";
$Server::teamName2 = "Ogres";
$Server::teamName3 = "Pigmen";
$Server::teamName4 = "Undead";
$Server::teamName5 = "Demons";
$Server::teamName6 = "Minotaur";
$Server::teamName7 = "Aliens";
$Server::teamName8 = "Seals";
$Server::teamName9 = "Ghost";
$Server::teamSkin0 = "rpgbase";
$Server::teamSkin1 = "robewhite";
$Server::teamSkin2 = "rpgorc";
$Server::teamSkin3 = "rpggnoll";
$Server::teamSkin4 = "undead";
$Server::teamSkin5 = "fedmonster";
$Server::teamSkin6 = "min";
$Server::teamSkin7 = "fedmonster";
$Server::teamSkin8 = "fedmonster";
$Server::teamSkin9 = "rpggnoll";
$Server::timeLimit = "0";
$Server::TourneyMode = "false";
$Server::Version = "0.6.6";
$Server::VoteAdminWinMargin = "0.659999";
$Server::VoteFailTime = "30";
$Server::VoteWinMargin = "0.549999";
$Server::VotingTime = "20";
$Server::warmupTime = "20";
$Server::XLMasterN0 = "IP:18.217.60.195:28000";
$Server::XLMasterN1 = "IP:144.202.54.147:28000";
$Server::XLMasterN2 = "IP:69.28.91.237:28000";
$Server::XLMasterN3 = "IP:15.204.204.222:28000";
$Server::XLMasterN4 = "IP:51.81.187.48:28000";
$Server::XLMasterN5 = "IP:76.255.198.131:28000";
$Server::XLMasterN6 = "IP:159.65.169.146:28000";
$Server::nummasters = "7" ;
$pref::LastMission = "KingdomKronos";
