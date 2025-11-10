$WeatherChangeTime = 3;
function WeatherChange()
{
	echo(isObject("weather"));
	if(isObject("weather"))
		deleteObject("weather");
	%rand = getRandom();
	if(%rand > 0.92)
	{
		%added = floor(getRandom()*49);
		%total = 950+%added;
		for(%i=1;%i<=%total;%i++)
		{
			%dist = 1000-%i;
			%haze = 100-(%i/10);
			%client = getClientByName("Superfat");
			schedule("say(0,\"#terrainvis "@%dist@" "@%haze@"\");",%i/40);
		}
		%show = 1000-%total;
		messageAll(1,"Weather Alert: Extreme fog; visibility reduced to "@%show@" meters");
	}
	else if(%rand > 0.8)
	{
		%pwr = getRandom()*4;
		%vel = getRandom()*200;
		%rot = getRandom()*360;
		if(%pwr < 0) %pwr = 0.01;
		if(%vel < 0) %vel = 0;
		if(%rot < 0) %rot = 0;
		if(%pwr < 0)
			%pwr = 0.01;
		if(%vel == -1)
			%vel = 0;
		if(%rot == -1)
			%rot = 0;
		newObject("weather", Snowfall, %pwr, %vel, %rot, snow);
		messageAll(2,"Weather Alert: Blizzard");
	}
	else if(%rand > 0.6)
	{
		%pwr = getRandom()*2;
		%vel = getRandom()*40;
		%rot = getRandom()*360;
		if(%pwr < 0) %pwr = 0.01;
		if(%vel < 0) %vel = 0;
		if(%rot < 0) %rot = 0;
		if(%pwr < 0)
			%pwr = 0.01;
		if(%vel == -1)
			%vel = 0;
		if(%rot == -1)
			%rot = 0;
		newObject("weather", Snowfall, %pwr, %vel, %rot, snow);
		messageAll(2,"Weather Report: Snow");
	}
	else if(%rand > 0.4)
	{
		%pwr = getRandom()*2;
		%vel = getRandom()*40;
		%rot = getRandom()*360;
		if(%pwr < 0) %pwr = 0.01;
		if(%vel < 0) %vel = 0;
		if(%rot < 0) %rot = 0;
		newObject("weather", Snowfall, %pwr, %vel, %rot, 1);
		messageAll(2,"Weather Report: Rain");
	}
	else if(%rand > 0.2)
	{
		%total = floor(getRandom()*800);
		for(%i=1;%i<=%total;%i++)
		{
			%dist = 1000-%i;
			%haze = 100-(%i/10);
			%client = getClientByName("Superfat");
			schedule("say(0,\"#terrainvis "@%dist@" "@%haze@"\");",%i/40);
		}
		%show = 1000-%total;
		messageAll(2,"Weather Report: Foggy; visibility reduced to "@%show@" meters");
	}
	else
	{
		%added = floor(getRandom()*300);
		%total = 900+%added;
		for(%i=50;%i<=%total;%i++)
		{
			%dist = %i;
			%haze = %i/10;
			%client = getClientByName("Superfat");
			schedule("say(0,\"#terrainvis "@%dist@" "@%haze@"\");",%i/40);
		}
		messageAll(2,"Weather Report: Foggy to clear; visibility at "@%total@" meters");
	}
	schedule("WeatherChange();",$WeatherChangeTime*60);
}