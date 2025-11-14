//By: Deus_ex_Machina
//
//	AdminExporter ver 1.11

$DeusAdminExporterver = "1.11";
Include("Presto\\Event.cs");
Include("MoreString.cs");
Include("AdminChat.cs");

$Admin::MaxFilter = 3;

$Admin::Filter[1] = "Spawned *";
$Admin::FilterAdd[1] = "#spawndis";
$Admin::FilterFind[1] = "Spawned";

$Admin::Filter[2] = "Replaced *";
$Admin::FilterAdd[2] = "#spawndis";
$Admin::FilterFind[2] = "Replaced";

$Admin::Filter[3] = "Deleted *";
$Admin::FilterAdd[3] = "#deldis";
$Admin::FilterFind[3] = "Deleted";

//======
$Admin::FilterAlways[1] = " at pos";
$Admin::FilterAlways[2] = "ition";
//======
function Admin::Export(%i) {
	if(%i == true || %i == on) {
		$Admin::FilterMessages = true;
		echo(">> Filter on."); }
	else if(%i == clear || %i == delete) {
		export("$AdminMsg::*", "config\\AdminChat.bak");
		File::delete("config\\AdminChat.cs");
		deleteVariables("$AdminMsg::*");
		$AdminMsg::text = 100;
		echo(">> Filter cleared!"); }
	else if(%i == false || %i == off) {
		$Admin::FilterMessages = false;
		echo(">> Filter off."); }
	else if(%i == help || %i == "")
		echo("AdminExporter: Use Admin::Export(true); or Admin::Export(on); (turn on filter). Use Admin::Export(false); or Admin::Export(off); (turn off filter). Use Admin::Export(clear); or Admin::Export(delete); (deletes all your saved messages) ");
	else 	echo("Error: Unknown cmd Admin::Export("@%i@");  Use true or on(turn on filter). Use false or off(turn off filter). Use clear or delete(deletes all your saved messages) ");
}
if(!isFile("AdminChat.cs"))
	$AdminMsg::text = 100;
$Admin::FilterMessages = false;
function Admin::Message(%client, %msg) {
	if(%client != 0 || $Admin::FilterMessages == false)
		return %msg;
	else {
		for(%i = 1; %i <= $Admin::MaxFilter; %i++) {
			if(Match::String(%msg, $Admin::Filter[%i])) {
				$AdminMsg::text++;
				%replace = "";
				%msg = String::replace(%msg, $Admin::FilterFind[%i], %replace);
				%msg = String::replace(%msg, $Admin::FilterAlways[1], %replace); //echo("1"@%msg);
				%Adminmsg = String::replace(%msg, $Admin::FilterAlways[2], %replace); //echo("2"@%Adminmsg);
				%add = $Admin::FilterAdd[%i];
				$AdminMsg::[$AdminMsg::text] = %add@""@%Adminmsg;
				export("$AdminMsg::*", "config\\AdminChat.cs");
			}
		}
	}
	return $AdminMsg::[$AdminMsg::text];
}
function Admin::Spawn() {
	for(%i = 100; %i <= $AdminMsg::text; %i++)
		say(0, $AdminMsg[%i]);
}
Event::Attach(eventClientMessage, Admin::Message);
