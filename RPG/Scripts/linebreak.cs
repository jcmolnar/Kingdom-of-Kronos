// Filename: Linebreak.cs
// Type: Tribes Script
// Description: Ascii code blocking script.
// Author: Trooper
// Created: 23rd March 2004
// Modified: 24th March 2004
// Notes: Becuase this script is so strict, there have been a few
// reports where the script is catching certain ascii codes in voice packs.

function bytetohex(%decimal)
{
    %hex[ 0] = "0"; %hex[ 1] = "1"; %hex[ 2] = "2"; %hex[ 3] = "3";
    %hex[ 4] = "4"; %hex[ 5] = "5"; %hex[ 6] = "6"; %hex[ 7] = "7";
    %hex[ 8] = "8"; %hex[ 9] = "9"; %hex[10] = "A"; %hex[11] = "B";
    %hex[12] = "C"; %hex[13] = "D"; %hex[14] = "E"; %hex[15] = "F";
    
    %b = floor(%decimal / 16);
    %r = %decimal % 16;
    
    %value = %hex[%b] @ %hex[%r];
    
    return %value;
}

function LineBreak::Test(%client, %cropped)
{
    if(%cropped == "")
        return false;
        
    for(%i = 1; %i < 256; %i++)
    {
        if(%i < 32 || %i > 126)
        {
            %h = bytetohex(%i);

            if(String::findSubStr(%cropped, $hex[%h]) != -1)
            {
                $linebreaker = client::getname(%client) @ " hex = " @ %h @ " dec = " @ %i;
                echo("LineBreak::Check detected invalid characters: " @ $linebreaker);
                Client::SendMessage(%client, $MsgRed, "The server detected a malformed key code(0x" @ %h @ ") from you and has been logged. This is sometimes an indication of a linebreak. If this is a mistake, please notify an admin and explain the cause.");
                export("linebreaker", "temp\\linebreaks.cs", true);
                deleteVariables("linebreaker");
                return true;
            }
        }
    }
    
    return false;
}

$hex["00"] = "\x00"; $hex["01"] = "\x01"; $hex["02"] = "\x02"; $hex["03"] = "\x03";
$hex["04"] = "\x04"; $hex["05"] = "\x05"; $hex["06"] = "\x06"; $hex["07"] = "\x07";
$hex["08"] = "\x08"; $hex["09"] = "\x09"; $hex["0A"] = "\x0A"; $hex["0B"] = "\x0B";
$hex["0C"] = "\x0C"; $hex["0D"] = "\x0D"; $hex["0E"] = "\x0E"; $hex["0F"] = "\x0F";

$hex["10"] = "\x10"; $hex["11"] = "\x11"; $hex["12"] = "\x12"; $hex["13"] = "\x13";
$hex["14"] = "\x14"; $hex["15"] = "\x15"; $hex["16"] = "\x16"; $hex["17"] = "\x17";
$hex["18"] = "\x18"; $hex["19"] = "\x19"; $hex["1A"] = "\x1A"; $hex["1B"] = "\x1B";
$hex["1C"] = "\x1C"; $hex["1D"] = "\x1D"; $hex["1E"] = "\x1E"; $hex["1F"] = "\x1F";

$hex["20"] = "\x20"; $hex["21"] = "\x21"; $hex["22"] = "\x22"; $hex["23"] = "\x23";
$hex["24"] = "\x24"; $hex["25"] = "\x25"; $hex["26"] = "\x26"; $hex["27"] = "\x27";
$hex["28"] = "\x28"; $hex["29"] = "\x29"; $hex["2A"] = "\x2A"; $hex["2B"] = "\x2B";
$hex["2C"] = "\x2C"; $hex["2D"] = "\x2D"; $hex["2E"] = "\x2E"; $hex["2F"] = "\x2F";

$hex["30"] = "\x30"; $hex["31"] = "\x31"; $hex["32"] = "\x32"; $hex["33"] = "\x33";
$hex["34"] = "\x34"; $hex["35"] = "\x35"; $hex["36"] = "\x36"; $hex["37"] = "\x37";
$hex["38"] = "\x38"; $hex["39"] = "\x39"; $hex["3A"] = "\x3A"; $hex["3B"] = "\x3B";
$hex["3C"] = "\x3C"; $hex["3D"] = "\x3D"; $hex["3E"] = "\x3E"; $hex["3F"] = "\x3F";

$hex["40"] = "\x40"; $hex["41"] = "\x41"; $hex["42"] = "\x42"; $hex["43"] = "\x43";
$hex["44"] = "\x44"; $hex["45"] = "\x45"; $hex["46"] = "\x46"; $hex["47"] = "\x47";
$hex["48"] = "\x48"; $hex["49"] = "\x49"; $hex["4A"] = "\x4A"; $hex["4B"] = "\x4B";
$hex["4C"] = "\x4C"; $hex["4D"] = "\x4D"; $hex["4E"] = "\x4E"; $hex["4F"] = "\x4F";

$hex["50"] = "\x50"; $hex["51"] = "\x51"; $hex["52"] = "\x52"; $hex["53"] = "\x53";
$hex["54"] = "\x54"; $hex["55"] = "\x55"; $hex["56"] = "\x56"; $hex["57"] = "\x57";
$hex["58"] = "\x58"; $hex["59"] = "\x59"; $hex["5A"] = "\x5A"; $hex["5B"] = "\x5B";
$hex["5C"] = "\x5C"; $hex["5D"] = "\x5D"; $hex["5E"] = "\x5E"; $hex["5F"] = "\x5F";

$hex["60"] = "\x60"; $hex["61"] = "\x61"; $hex["62"] = "\x62"; $hex["63"] = "\x63";
$hex["64"] = "\x64"; $hex["65"] = "\x65"; $hex["66"] = "\x66"; $hex["67"] = "\x67";
$hex["68"] = "\x68"; $hex["69"] = "\x69"; $hex["6A"] = "\x6A"; $hex["6B"] = "\x6B";
$hex["6C"] = "\x6C"; $hex["6D"] = "\x6D"; $hex["6E"] = "\x6E"; $hex["6F"] = "\x6F";

$hex["70"] = "\x70"; $hex["71"] = "\x71"; $hex["72"] = "\x72"; $hex["73"] = "\x73";
$hex["74"] = "\x74"; $hex["75"] = "\x75"; $hex["76"] = "\x76"; $hex["77"] = "\x77";
$hex["78"] = "\x78"; $hex["79"] = "\x79"; $hex["7A"] = "\x7A"; $hex["7B"] = "\x7B";
$hex["7C"] = "\x7C"; $hex["7D"] = "\x7D"; $hex["7E"] = "\x7E"; $hex["7F"] = "\x7F";

$hex["80"] = "\x80"; $hex["81"] = "\x81"; $hex["82"] = "\x82"; $hex["83"] = "\x83";
$hex["84"] = "\x84"; $hex["85"] = "\x85"; $hex["86"] = "\x86"; $hex["87"] = "\x87";
$hex["88"] = "\x88"; $hex["89"] = "\x89"; $hex["8A"] = "\x8A"; $hex["8B"] = "\x8B";
$hex["8C"] = "\x8C"; $hex["8D"] = "\x8D"; $hex["8E"] = "\x8E"; $hex["8F"] = "\x8F";

$hex["90"] = "\x90"; $hex["91"] = "\x91"; $hex["92"] = "\x92"; $hex["93"] = "\x93";
$hex["94"] = "\x94"; $hex["95"] = "\x95"; $hex["96"] = "\x96"; $hex["97"] = "\x97";
$hex["98"] = "\x98"; $hex["99"] = "\x99"; $hex["9A"] = "\x9A"; $hex["9B"] = "\x9B";
$hex["9C"] = "\x9C"; $hex["9D"] = "\x9D"; $hex["9E"] = "\x9E"; $hex["9F"] = "\x9F";

$hex["A0"] = "\xA0"; $hex["A1"] = "\xA1"; $hex["A2"] = "\xA2"; $hex["A3"] = "\xA3";
$hex["A4"] = "\xA4"; $hex["A5"] = "\xA5"; $hex["A6"] = "\xA6"; $hex["A7"] = "\xA7";
$hex["A8"] = "\xA8"; $hex["A9"] = "\xA9"; $hex["AA"] = "\xAA"; $hex["AB"] = "\xAB";
$hex["AC"] = "\xAC"; $hex["AD"] = "\xAD"; $hex["AE"] = "\xAE"; $hex["AF"] = "\xAF";

$hex["B0"] = "\xB0"; $hex["B1"] = "\xB1"; $hex["B2"] = "\xB2"; $hex["B3"] = "\xB3";
$hex["B4"] = "\xB4"; $hex["B5"] = "\xB5"; $hex["B6"] = "\xB6"; $hex["B7"] = "\xB7";
$hex["B8"] = "\xB8"; $hex["B9"] = "\xB9"; $hex["BA"] = "\xBA"; $hex["BB"] = "\xBB";
$hex["BC"] = "\xBC"; $hex["BD"] = "\xBD"; $hex["BE"] = "\xBE"; $hex["BF"] = "\xBF";

$hex["C0"] = "\xC0"; $hex["C1"] = "\xC1"; $hex["C2"] = "\xC2"; $hex["C3"] = "\xC3";
$hex["C4"] = "\xC4"; $hex["C5"] = "\xC5"; $hex["C6"] = "\xC6"; $hex["C7"] = "\xC7";
$hex["C8"] = "\xC8"; $hex["C9"] = "\xC9"; $hex["CA"] = "\xCA"; $hex["CB"] = "\xCB";
$hex["CC"] = "\xCC"; $hex["CD"] = "\xCD"; $hex["CE"] = "\xCE"; $hex["CF"] = "\xCF";

$hex["D0"] = "\xD0"; $hex["D1"] = "\xD1"; $hex["D2"] = "\xD2"; $hex["D3"] = "\xD3";
$hex["D4"] = "\xD4"; $hex["D5"] = "\xD5"; $hex["D6"] = "\xD6"; $hex["D7"] = "\xD7";
$hex["D8"] = "\xD8"; $hex["D9"] = "\xD9"; $hex["DA"] = "\xDA"; $hex["DB"] = "\xDB";
$hex["DC"] = "\xDC"; $hex["DD"] = "\xDD"; $hex["DE"] = "\xDE"; $hex["DF"] = "\xDF";

$hex["E0"] = "\xE0"; $hex["E1"] = "\xE1"; $hex["E2"] = "\xE2"; $hex["E3"] = "\xE3";
$hex["E4"] = "\xE4"; $hex["E5"] = "\xE5"; $hex["E6"] = "\xE6"; $hex["E7"] = "\xE7";
$hex["E8"] = "\xE8"; $hex["E9"] = "\xE9"; $hex["EA"] = "\xEA"; $hex["EB"] = "\xEB";
$hex["EC"] = "\xEC"; $hex["ED"] = "\xED"; $hex["EE"] = "\xEE"; $hex["EF"] = "\xEF";

$hex["F0"] = "\xF0"; $hex["F1"] = "\xF1"; $hex["F2"] = "\xF2"; $hex["F3"] = "\xF3";
$hex["F4"] = "\xF4"; $hex["F5"] = "\xF5"; $hex["F6"] = "\xF6"; $hex["F7"] = "\xF7";
$hex["F8"] = "\xF8"; $hex["F9"] = "\xF9"; $hex["FA"] = "\xFA"; $hex["FB"] = "\xFB";
$hex["FC"] = "\xFC"; $hex["FD"] = "\xFD"; $hex["FE"] = "\xFE"; $hex["FF"] = "\xFF";
