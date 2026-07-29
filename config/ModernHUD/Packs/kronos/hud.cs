//==============================================================================
// KRONOS - Kingdom of Kronos RPG HUD provider
//==============================================================================

exec("ModernHUD/Framework.cs");

$ModernHUD::Enabled = true;
$ModernHUD::Pack = "Kronos";
$ModernHUD::PackId = "kronos";

function ModernHUDPack::ownsSlot(%value)
{
   return KronosHUD::ownsSlot(%value);
}

function ModernHUDPack::prefs()
{
}

function ModernHUDPack::stockHuds()
{
   Control::SetVisible(crosshairHud, true);
   Control::SetVisible(Minimap, true);
   Control::SetVisible(clockHud, true);
   Control::SetVisible(compassHud, false);
   Control::SetVisible(sensorHUD, false);

   if(KronosHUD::ownsSlot($pref::HudSlot::healthenergy))
   {
      Control::SetVisible(healthHud, false);
      Control::SetVisible(jetPackHud, false);
   }

   if(KronosHUD::ownsSlot($pref::HudSlot::weapon))
      Control::SetVisible(weaponHud, false);

   KronosChat::applyVisibility();
}

function ModernHUDPack::detachRetained()
{
}

function ModernHUDPack::init()
{
}

function ModernHUDPack::draw(%screen)
{
   glPartScale(0, 0, 1);
   KronosHUD::drawPersistent(%screen);
}

function ModernHUDPack::onGuiOpen(%gui)
{
   if(%gui == "playGui")
      Schedule::Add("ModernHUDPack::stockHuds();", 0);
}

ModernHUD::attach("eventGuiOpen", "ModernHUDPack::onGuiOpen");
ModernHUDPack::prefs();
ModernHUDPack::stockHuds();
ModernHUDPack::init();
