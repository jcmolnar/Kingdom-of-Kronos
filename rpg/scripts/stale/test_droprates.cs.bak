// Test function for "1 in X" drop rate system (used for AdminBoots)
// This matches the actual drop logic in rpgfunk.cs and playerdamage.cs
function testDropRate1InX(%x, %count, %repeat)
{
	if(!%count) %count = 10000;
	if(!%repeat) %repeat = 10;
	
	echo("========================================");
	echo("Testing drop rate: 1 in " @ %x);
	echo("Kills per test: " @ %count);
	echo("Number of tests: " @ %repeat);
	echo("========================================");
	
	%totalDrops = 0;
	%totalKills = 0;
	
	for(%i = 0; %i < %repeat; %i++)
	{
		%drops = 0;
		for(%j = 0; %j < %count; %j++)
		{
			// This matches the actual drop logic: roll 1 to X, need exactly 1
			%roll = floor(getRandom() * %x) + 1;  // Roll 1 to %x
			
			if(%roll == 1)
			{
				%drops++;
			}
		}
		
		%totalDrops += %drops;
		%totalKills += %count;
		
		%actualRate = %drops / %count * 100;
		%expectedRate = (1 / %x) * 100;
		
		echo("Test " @ (%i + 1) @ ": " @ %drops @ " drops out of " @ %count @ " kills (" @ %actualRate @ "%, expected " @ %expectedRate @ "%)");
	}
	
	%averageDrops = %totalDrops / %repeat;
	%averageRate = (%totalDrops / %totalKills) * 100;
	%expectedRate = (1 / %x) * 100;
	%expectedDrops = %count / %x;
	
	echo("========================================");
	echo("RESULTS:");
	echo("Total drops: " @ %totalDrops @ " out of " @ %totalKills @ " kills");
	echo("Average drops per " @ %count @ " kills: " @ %averageDrops);
	echo("Average drop rate: " @ %averageRate @ "%");
	echo("Expected drop rate: " @ %expectedRate @ "%");
	echo("Expected average drops per " @ %count @ " kills: " @ %expectedDrops);
	
	%deviation = %averageRate - %expectedRate;
	%deviationPercent = (%deviation / %expectedRate) * 100;
	
	echo("Deviation: " @ %deviation @ "% (" @ %deviationPercent @ "% from expected)");
	echo("========================================");
	
	return %averageDrops;
}

// Test AdminBoots drop rates
function testAdminBootsDropRates()
{
	echo("");
	echo("========================================");
	echo("ADMINBOOTS DROP RATE VERIFICATION");
	echo("========================================");
	echo("");
	
	echo("Testing 1/5000 (Abolisher bot)...");
	testDropRate1InX(5000, 50000, 10);
	echo("");
	
	echo("Testing 1/10000 (Obliterator bot)...");
	testDropRate1InX(10000, 100000, 10);
	echo("");
	
	echo("Testing 1/15000 (Liquifier bot)...");
	testDropRate1InX(15000, 150000, 10);
	echo("");
	
	echo("========================================");
	echo("VERIFICATION COMPLETE");
	echo("========================================");
	echo("");
	echo("Expected results:");
	echo("  1/5000  = 0.02%  (1 drop per 5000 kills)");
	echo("  1/10000 = 0.01%  (1 drop per 10000 kills)");
	echo("  1/15000 = 0.0067% (1 drop per 15000 kills)");
	echo("");
	echo("Run 'testAdminBootsDropRates();' to test all rates");
	echo("Or run 'testDropRate1InX(5000, 10000, 1);' to test a specific rate");
	echo("========================================");
}

// Original dropTest function (for reference - uses different system)
function dropTest(%base,%roll,%count,%repeat)
{
    echo("dropTest result for base = "@%base@" and roll = "@%roll@":");
    %total = 0;
    if(!%count) %count = 1000;
    if(!%repeat) %repeat = 10;
    for(%i=0;%i<%repeat;%i++)
    {
        %drops = 0;
        for(%j=0;%j<%count;%j++)
        {
            %r = Cap(floor(getRandom() * (100-%roll))+%roll+1,0,100);
            if(%r > 100) %r = 100;
            %w2 = round(%base * (%r/100));
            if(%w2 < 0) %w2 = 0;
            if(%count <= 20 && %repeat < 1) echo("Test "@%j+1@": rolled "@%r@" > "@%w2);
            if(%w2 > 0)
            {
                %drops += %w2;
                %total += %w2;
            }
        }
        if(%repeat > 1 && %repeat < 11)
            echo("Test "@%i+1@": "@%count@" kills / "@%drops@" drops / "@%total@" total");
        else if(%repeat < 11)
            echo(%count@" kills / "@%drops@" drops");
    }
    echo("Average drop rate approx. "@%total/%repeat);
}

