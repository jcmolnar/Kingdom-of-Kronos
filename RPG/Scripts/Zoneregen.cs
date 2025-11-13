
function refreshHPREGEN(%clientId) 
{ 
dbecho($dbechoMode, "refreshHPREGEN(" @ %clientId @ ")"); 

%a = $PlayerSkill[%clientId, $SkillHealing] / 250000; 
if(%clientId.sleepMode == 1) 
%b = %a + 0.0200; 
else if(%clientId.sleepMode == 2) 
%b = %a; 
else 
%b = %a; 

%c = AddPoints(%clientId, 10) / 2000; 
if(Zone::getType(fetchData(%clientId, "zone")) == "PROTECTED")
{ 
%r = %b + %c + 0.06; 
} 
else 
{ 
%r = %b + %c; 
} 
GameBase::setAutoRepairRate(Client::getOwnedObject(%clientId), %r); 
} 
function ZoneisProtected(%clientId) 
{ 
%group = nameToId("MissionGroup\\Zones"); 
%object = Group::getObject(%group, %i); 
%system = Object::getName(%object); 
%type = GetWord(%system, 0); 
if(%type == "PROTECTED") 
{ 
return 1; 
} 
else 
{ 
return 0; 
} 
} 
