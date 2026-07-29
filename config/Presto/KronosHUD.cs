//==============================================================================
// KronosHUD.cs - Kingdom of Kronos client HUD bridge
//
// The RPG panels are a ModernHUD slot provider, while transient RPG information
// (casts, targets, weapon and examine popups) remains available with any pack.
// This file also owns the single ScriptGL post-draw dispatcher used by the
// menu/shop/chat/NPC scripts.
//==============================================================================

if($KronosHUD::ClientLoaded == "")
   $KronosHUD::ClientLoaded = true;

// Persisted defaults. Positions are percentages to preserve the original
// KronosMenu drag behavior at every resolution.
if($pref::Kronos::enabled == "")       $pref::Kronos::enabled = true;
if($pref::Kronos::vitals == "")        $pref::Kronos::vitals = true;
if($pref::Kronos::target == "")        $pref::Kronos::target = true;
if($pref::Kronos::cast == "")          $pref::Kronos::cast = true;
if($pref::Kronos::weaponPopup == "")   $pref::Kronos::weaponPopup = true;
if($pref::Kronos::examinePopup == "")  $pref::Kronos::examinePopup = true;
if($pref::Kronos::damageText == "")     $pref::Kronos::damageText = true;
if($pref::Kronos::opacity == "")       $pref::Kronos::opacity = 92;
if($pref::Kronos::UiScalePct == "")    $pref::Kronos::UiScalePct = 100;
if($pref::Kronos::ChatScale == "")     $pref::Kronos::ChatScale = 11;
if($pref::Kronos::vitalsPos == "")     $pref::Kronos::vitalsPos = "1.5 84";
if($pref::Kronos::infoHudPos == "")    $pref::Kronos::infoHudPos = "81.5 84";
if($pref::Kronos::wbarPos == "")       $pref::Kronos::wbarPos = "25 96.3";

function KronosHUD::applyUiScale()
{
   $pref::Kronos::UiScale = $pref::Kronos::UiScalePct / 100;
}

function KronosHUD::applyChatScale()
{
   $pref::Kronos::chatFontH = $pref::Kronos::ChatScale / 1000;
   $KC::lastFont = -1;
}

KronosHUD::applyUiScale();
KronosHUD::applyChatScale();

// These rows are re-registered whenever Framework.cs is exec'd. They are
// intentionally client-wide: the RPG overlays continue to work while a player
// borrows HUD modules from another ModernHUD pack.
function KronosHUD::registerSettings()
{
   for(%i = 0; %i < $ModernHUD::SettingCount; %i++)
   {
      if($ModernHUD::Setting[%i, "key"] == "pref::Kronos::enabled")
         return;
   }

   ModernHUD::setting("bool", "pref::Kronos::enabled",
      "Kronos RPG HUD", "1", "", "");
   ModernHUD::setting("bool", "pref::Kronos::vitals",
      "RPG vitals panels", "1", "", "");
   ModernHUD::setting("bool", "pref::Kronos::target",
      "RPG target frame", "1", "", "");
   ModernHUD::setting("bool", "pref::Kronos::cast",
      "Spell cast bar", "1", "", "");
   ModernHUD::setting("bool", "pref::Kronos::weaponPopup",
      "Weapon popup", "1", "", "");
   ModernHUD::setting("bool", "pref::Kronos::examinePopup",
      "Item examine popup", "1", "", "");
   ModernHUD::setting("bool", "pref::Kronos::damageText",
      "Floating damage text", "1", "", "");
   ModernHUD::setting("bool", "pref::Kronos::chatEnabled",
      "Kronos chat overlay", "1", "",
      "KronosChat::applyVisibility(); KronosChat::bindTalkKey();");
   ModernHUD::setting("int", "pref::Kronos::UiScalePct",
      "Kronos UI scale (%)", "100", "50|150|5",
      "KronosHUD::applyUiScale();");
   ModernHUD::setting("int", "pref::Kronos::ChatScale",
      "Chat text size", "11", "6|40|1",
      "KronosHUD::applyChatScale();");
   ModernHUD::setting("int", "pref::Kronos::opacity",
      "Kronos HUD opacity (%)", "92", "40|100|5", "");
}

// A blank slot belongs to the active pack. An explicit Kronos::<part> value is
// ours even when another pack is active, which is how mixed HUDs avoid drawing
// both implementations on top of one another.
function KronosHUD::ownsSlot(%value)
{
   if(!$pref::Kronos::enabled || %value == "off")
      return false;
   if(String::findSubStr(%value, "Kronos::") == 0)
      return true;
   return %value == "" && $ModernHUD::PackId == "kronos";
}

function KronosHUD::alpha(%fraction)
{
   %a = floor(255 * ($pref::Kronos::opacity / 100) * %fraction);
   if(%a < 0) %a = 0;
   if(%a > 255) %a = 255;
   return %a;
}

function KronosHUD::scale(%screen)
{
   %s = getWord(%screen, 1) / 1080;
   if(%s < 0.70) %s = 0.70;
   if(%s > 1.25) %s = 1.25;
   return %s;
}

function KronosHUD::position(%pref, %screen, %w, %h)
{
   %x = floor(getWord(%pref, 0) * getWord(%screen, 0) / 100);
   %y = floor(getWord(%pref, 1) * getWord(%screen, 1) / 100);
   if(%x < 0) %x = 0;
   if(%y < 0) %y = 0;
   if(%x + %w > getWord(%screen, 0)) %x = getWord(%screen, 0) - %w;
   if(%y + %h > getWord(%screen, 1)) %y = getWord(%screen, 1) - %h;
   return %x @ " " @ %y;
}

function KronosHUD::publishRect(%name, %pos, %w, %h)
{
   // KronosMenu's existing drag framework reads this cache. Publishing it keeps
   // old saved positions and the TAB-menu drag handles fully compatible.
   $vhud[%name, render, pos] = %pos;
   $vhud[%name, render, size] = %w @ " " @ %h;
}

function KronosHUD::panel(%x, %y, %w, %h)
{
   glColor4ub(8, 13, 19, KronosHUD::alpha(0.82));
   glRectangle(%x, %y, %w, %h);
   glColor4ub(52, 154, 183, KronosHUD::alpha(0.85));
   glRectangle(%x, %y, %w, 2);
}

function KronosHUD::meter(%x, %y, %w, %h, %value, %max, %r, %g, %b)
{
   %frac = 0;
   if(%max > 0)
      %frac = %value / %max;
   if(%frac < 0) %frac = 0;
   if(%frac > 1) %frac = 1;

   glColor4ub(20, 25, 31, KronosHUD::alpha(0.92));
   glRectangle(%x, %y, %w, %h);
   %fill = floor(%w * %frac);
   if(%fill > 0)
   {
      glColor4ub(%r, %g, %b, KronosHUD::alpha(0.95));
      glRectangle(%x, %y, %fill, %h);
   }
}

function KronosHUD::text(%x, %y, %size, %text, %r, %g, %b)
{
   glSetFont("Verdana", %size, $GLEX_SMOOTH, 0);
   glColor4ub(%r, %g, %b, KronosHUD::alpha(1));
   glDrawString(%x, %y, %text);
}

function KronosHUD::centerText(%x, %y, %w, %size, %text, %r, %g, %b)
{
   %font = %size;
   glSetFont("Verdana", %font, $GLEX_SMOOTH, 0);
   %tw = getWord(glGetStringDimensions(%text), 0);
   while(%tw > %w - 8 && %font > 7)
   {
      %font--;
      glSetFont("Verdana", %font, $GLEX_SMOOTH, 0);
      %tw = getWord(glGetStringDimensions(%text), 0);
   }
   if(%tw > %w - 8)
   {
      %full = %text;
      while(%tw > %w - 8 && String::len(%text) > 4)
      {
         %text = String::getSubStr(%text, 0, String::len(%text) - 2);
         glSetFont("Verdana", %font, $GLEX_SMOOTH, 0);
         %tw = getWord(glGetStringDimensions(%text @ "..."), 0);
      }
      if(%text != %full)
         %text = %text @ "...";
   }
   glColor4ub(%r, %g, %b, KronosHUD::alpha(1));
   glDrawString(%x + floor((%w - %tw) / 2), %y, %text);
}

function KronosHUD::stripTags(%text)
{
   // Weapon descriptions use the stock <f0>..<f9> bottomprint palette.
   for(%i = 0; %i < 10; %i++)
   {
      %tag = "<f" @ %i @ ">";
      while((%at = String::findSubStr(%text, %tag)) != -1)
         %text = String::getSubStr(%text, 0, %at) @
            String::getSubStr(%text, %at + 4, 9999);
   }
   return %text;
}

function KronosHUD::drawVitals(%screen)
{
   if(!$pref::Kronos::vitals ||
      !KronosHUD::ownsSlot($pref::HudSlot::healthenergy))
      return;

   %s = KronosHUD::scale(%screen);
   %w = floor(350 * %s);
   %h = floor(78 * %s);
   %p = KronosHUD::position($pref::Kronos::vitalsPos, %screen, %w, %h);
   %x = getWord(%p, 0);
   %y = getWord(%p, 1);
   %pad = floor(8 * %s);
   %bh = floor(18 * %s);
   %font = floor(11 * %s);

   KronosHUD::publishRect("kh_vitals", %p, %w, %h);
   KronosHUD::panel(%x, %y, %w, %h);

   KronosHUD::meter(%x + %pad, %y + %pad, %w - (%pad * 2), %bh,
      $KH::hp, $KH::maxHp, 188, 50, 63);
   KronosHUD::centerText(%x + %pad, %y + %pad + 2, %w - (%pad * 2), %font,
      "HP " @ $KH::hp @ " / " @ $KH::maxHp, 248, 248, 248);

   KronosHUD::meter(%x + %pad, %y + %pad + %bh + 3, %w - (%pad * 2), %bh,
      $KH::mana, $KH::maxMana, 47, 118, 201);
   KronosHUD::centerText(%x + %pad, %y + %pad + %bh + 5, %w - (%pad * 2), %font,
      "MP " @ $KH::mana @ " / " @ $KH::maxMana, 248, 248, 248);

   %xpNeed = $KH::xpNext - $KH::xpCur;
   %xpHave = $KH::exp - $KH::xpCur;
   if(%xpNeed <= 0)
   {
      %xpNeed = 1;
      %xpHave = 0;
   }
   KronosHUD::meter(%x + %pad, %y + %pad + (%bh * 2) + 6,
      %w - (%pad * 2), %bh, %xpHave, %xpNeed, 209, 169, 47);
   KronosHUD::centerText(%x + %pad, %y + %pad + (%bh * 2) + 8,
      %w - (%pad * 2), %font, "XP " @ %xpHave @ " / " @ %xpNeed, 18, 18, 18);
}

function KronosHUD::drawInfo(%screen)
{
   if(!$pref::Kronos::vitals ||
      !KronosHUD::ownsSlot($pref::HudSlot::items))
      return;

   %s = KronosHUD::scale(%screen);
   %w = floor(300 * %s);
   %h = floor(78 * %s);
   %p = KronosHUD::position($pref::Kronos::infoHudPos, %screen, %w, %h);
   %x = getWord(%p, 0);
   %y = getWord(%p, 1);
   %pad = floor(9 * %s);
   %font = floor(12 * %s);

   KronosHUD::publishRect("kh_info", %p, %w, %h);
   KronosHUD::panel(%x, %y, %w, %h);
   KronosHUD::text(%x + %pad, %y + %pad,
      %font, "Level " @ $KH::lvl @ "  Remort " @ $KH::remort, 238, 205, 107);
   KronosHUD::text(%x + %pad, %y + %pad + floor(20 * %s),
      %font, $KH::class @ "  |  " @ $KH::zone, 224, 235, 240);
   KronosHUD::text(%x + %pad, %y + %pad + floor(40 * %s),
      %font, "Gold " @ $KH::gold, 238, 205, 107);
}

function KronosHUD::drawWeaponBar(%screen)
{
   if(!KronosHUD::ownsSlot($pref::HudSlot::weapon))
      return;

   %name = $KH::weapon;
   if(%name == "")
      %name = GetItemDesc(GetMountedItem(0));
   if(%name == "")
      return;

   %s = KronosHUD::scale(%screen);
   %w = floor(420 * %s);
   %h = floor(34 * %s);
   %p = KronosHUD::position($pref::Kronos::wbarPos, %screen, %w, %h);
   %x = getWord(%p, 0);
   %y = getWord(%p, 1);

   KronosHUD::publishRect("kh_wbar", %p, %w, %h);
   KronosHUD::panel(%x, %y, %w, %h);
   KronosHUD::centerText(%x, %y + floor(8 * %s), %w, floor(12 * %s),
      %name, 229, 238, 241);
}

function KronosHUD::drawPersistent(%screen)
{
   if(!$pref::Kronos::enabled)
      return;
   KronosHUD::drawVitals(%screen);
   KronosHUD::drawInfo(%screen);
   KronosHUD::drawWeaponBar(%screen);
}

function KronosHUD::drawCast(%screen)
{
   if(!$pref::Kronos::cast || $KH::castStart == "")
      return;
   %elapsed = getSimTime() - $KH::castStart;
   if(%elapsed < 0 || %elapsed > $KH::castRecovery)
   {
      $KH::castStart = "";
      return;
   }

   %s = KronosHUD::scale(%screen);
   %w = floor(360 * %s);
   %h = floor(34 * %s);
   %x = floor((getWord(%screen, 0) - %w) / 2);
   %y = floor(getWord(%screen, 1) * 0.72);
   KronosHUD::panel(%x, %y, %w, %h);
   if(%elapsed <= $KH::castTime)
   {
      KronosHUD::meter(%x + 6, %y + %h - 9, %w - 12, 5,
         %elapsed, $KH::castTime, 113, 83, 196);
      %label = $KH::castName;
   }
   else
   {
      %recovery = $KH::castRecovery - $KH::castTime;
      if(%recovery <= 0) %recovery = 1;
      KronosHUD::meter(%x + 6, %y + %h - 9, %w - 12, 5,
         %elapsed - $KH::castTime, %recovery, 52, 154, 183);
      %label = "Recovering: " @ $KH::castName;
   }
   KronosHUD::centerText(%x, %y + floor(8 * %s), %w, floor(12 * %s),
      %label, 242, 239, 255);
}

function KronosHUD::drawTarget(%screen)
{
   if(!$pref::Kronos::target || $KH::targetTime == "" ||
      getSimTime() - $KH::targetTime > 1.2)
      return;

   %s = KronosHUD::scale(%screen);
   %w = floor(340 * %s);
   %h = floor(46 * %s);
   %x = floor((getWord(%screen, 0) - %w) / 2);
   %y = floor(68 * %s);
   KronosHUD::panel(%x, %y, %w, %h);
   KronosHUD::centerText(%x, %y + floor(7 * %s), %w, floor(13 * %s),
      $KH::targetName, 242, 242, 242);
   if($KH::targetHp != "NPC")
      KronosHUD::meter(%x + 8, %y + %h - 12, %w - 16, 6,
         $KH::targetHp, 100, 189, 51, 61);
   if($KH::targetDamage != "")
      KronosHUD::text(%x + %w + 8, %y + floor(8 * %s), floor(13 * %s),
         "-" @ $KH::targetDamage, 255, 205, 85);
}

function KronosHUD::drawPopup(%screen, %text, %age, %duration, %side)
{
   if(%text == "" || %age < 0 || %age > %duration)
      return;
   %s = KronosHUD::scale(%screen);
   %w = floor(500 * %s);
   %h = floor(46 * %s);
   if(%side == "right")
      %x = getWord(%screen, 0) - %w - floor(22 * %s);
   else
      %x = floor((getWord(%screen, 0) - %w) / 2);
   %y = getWord(%screen, 1) - %h - floor(105 * %s);
   KronosHUD::panel(%x, %y, %w, %h);
   KronosHUD::centerText(%x + 8, %y + floor(13 * %s), %w - 16,
      floor(11 * %s), %text, 234, 239, 241);
}

function kronos::examine_render(%sw, %sh)
{
   if(!$pref::Kronos::examinePopup || $KH::exTime == "")
      return;
   KronosHUD::drawPopup(%sw @ " " @ %sh, $KH::examine,
      getSimTime() - $KH::exTime, 10, "right");
}

function KronosHUD::drawTransient(%screen)
{
   KronosHUD::drawCast(%screen);
   KronosHUD::drawTarget(%screen);
   KronosHUD::drawDamageText(%screen);
   if($pref::Kronos::weaponPopup && $KH::weaponTime != "")
      KronosHUD::drawPopup(%screen, $KH::weaponPopupText,
         getSimTime() - $KH::weaponTime, 3, "center");
   kronos::examine_render(getWord(%screen, 0), getWord(%screen, 1));
}

function KronosHUD::claimDefaultSlots()
{
   // Blank slots belong to the active ModernHUD pack. Only migrate them when
   // Kronos is standalone or is itself the selected master pack.
   if($ModernHUD::PackId != "" && $ModernHUD::PackId != "kronos")
      return;

   if($pref::HudSlot::healthenergy == "")
      $pref::HudSlot::healthenergy = "Kronos::vitals";
   if($pref::HudSlot::items == "")
      $pref::HudSlot::items = "Kronos::info";
   if($pref::HudSlot::weapon == "")
      $pref::HudSlot::weapon = "Kronos::weapon";
   if($pref::HudSlot::chat == "")
      $pref::HudSlot::chat = "Kronos::chat";
}

function KronosHUD::applySlotVisibility()
{
   if(KronosHUD::ownsSlot($pref::HudSlot::healthenergy))
   {
      Control::SetVisible(healthHud, false);
      Control::SetVisible(jetPackHud, false);
   }
   if(KronosHUD::ownsSlot($pref::HudSlot::weapon))
      Control::SetVisible(weaponHud, false);
   if(isFunction("KronosChat::applyVisibility"))
      KronosChat::applyVisibility();
}

// Server channels.
function remoteKronosHUD(%server, %hp, %maxHp, %mana, %maxMana,
   %exp, %xpCur, %xpNext, %gold, %lvl, %remort)
{
   if(%server != 2048)
      return;

   $KH::hp = %hp;
   $KH::maxHp = %maxHp;
   $KH::mana = %mana;
   $KH::maxMana = %maxMana;
   $KH::exp = %exp;
   $KH::xpCur = %xpCur;
   $KH::xpNext = %xpNext;
   $KH::gold = %gold;
   $KH::lvl = %lvl;
   $KH::remort = %remort;

   KronosHUD::claimDefaultSlots();
   KronosHUD::applySlotVisibility();

   %now = getSimTime();
   if($KH::handshakeAt == "" || %now < $KH::handshakeAt ||
      %now - $KH::handshakeAt > 25)
   {
      $KH::handshake = true;
      $KH::handshakeAt = %now;
      remoteEval(2048, KHudOn);
      KronosHUD::startHeartbeat();
   }
}

function remoteKronosHUD2(%server, %class, %zone)
{
   if(%server != 2048)
      return;

   $KH::class = KronosHUD::stripTags(%class);
   $KH::zone = KronosHUD::stripTags(%zone);
}

function remoteKronosTarget(%server, %name, %hp, %damage)
{
   if(%server != 2048)
      return;

   $KH::targetName = KronosHUD::stripTags(%name);
   $KH::targetHp = %hp;
   $KH::targetDamage = %damage;
   $KH::targetTime = getSimTime();
}

function remoteKronosCast(%server, %name, %castTime, %recovery)
{
   if(%server != 2048)
      return;

   $KH::castName = KronosHUD::stripTags(%name);
   $KH::castTime = %castTime;
   $KH::castRecovery = %recovery;
   $KH::castStart = getSimTime();
}

function remoteKronosCastStop(%server)
{
   if(%server != 2048)
      return;

   $KH::castStart = "";
}

function remoteKronosWeapon(%server, %text)
{
   if(%server != 2048)
      return;

   $KH::weaponPopupText = KronosHUD::stripTags(%text);
   $KH::weapon = $KH::weaponPopupText;
   %colon = String::findSubStr($KH::weapon, ":");
   if(%colon > 0)
      $KH::weapon = String::getSubStr($KH::weapon, 0, %colon);
   $KH::weaponTime = getSimTime();
}

function remoteKronosExamine(%server, %text)
{
   if(%server != 2048)
      return;

   $KH::examine = KronosHUD::stripTags(%text);
   $KH::exTime = getSimTime();
}

function remoteATKText(%server, %text, %style, %view)
{
   if(%server != 2048)
      return;

   if(!$pref::Kronos::damageText)
      return;

   %text = String::replace(%text, "<jl>", "");
   %text = String::replace(%text, "<jr>", "");
   %text = String::replace(%text, "<jc>", "");
   %text = KronosHUD::stripTags(%text);
   if(%text == "")
      return;

   %i = $KH::damageNext;
   if(%i == "") %i = 0;
   $KH::damageText[%i] = %text;
   $KH::damageStyle[%i] = %style;
   $KH::damageView[%i] = %view;
   $KH::damageTime[%i] = getSimTime();
   $KH::damageNext = (%i + 1) % 8;
}

function KronosHUD::drawDamageText(%screen)
{
   if(!$pref::Kronos::damageText)
      return;

   %now = getSimTime();
   %sw = getWord(%screen, 0);
   %sh = getWord(%screen, 1);
   %s = KronosHUD::scale(%screen);
   for(%i = 0; %i < 8; %i++)
   {
      %at = $KH::damageTime[%i];
      if(%at == "")
         continue;
      %age = %now - %at;
      if(%age < 0 || %age > 1.25)
      {
         $KH::damageTime[%i] = "";
         continue;
      }

      %view = $KH::damageView[%i];
      %style = $KH::damageStyle[%i];
      %x = floor(%sw / 2) + ((%i % 3) - 1) * floor(36 * %s);
      %y = floor(%sh * 0.47);
      %font = floor(16 * %s);
      %r = 238; %g = 238; %b = 238;

      if(%view == "attacker")
      {
         %y = floor(%sh * 0.34);
         %r = 255; %g = 103; %b = 88;
      }
      else if(%view == "defender")
      {
         %y = floor(%sh * 0.58);
         %r = 91; %g = 194; %b = 235;
      }

      if(%style == "pop")
      {
         %y -= floor(26 * %s * %age);
         if(%age < 0.18)
            %font += floor(8 * %s);
      }
      else if(%style == "nameplate")
         %y += floor(34 * %s * %age);
      else
         %y -= floor(48 * %s * %age);

      glSetFont("Verdana", %font, $GLEX_SMOOTH, 3);
      %tw = getWord(glGetStringDimensions($KH::damageText[%i]), 0);
      glColor4ub(%r, %g, %b, KronosHUD::alpha(1 - (%age / 1.25)));
      glDrawString(%x - floor(%tw / 2), %y, $KH::damageText[%i]);
   }
}

function KronosHUD::heartbeat(%generation)
{
   if(%generation != $KH::heartbeatGeneration)
      return;
   if($KH::handshake)
   {
      remoteEval(2048, KHudOn);
      $KH::handshakeAt = getSimTime();
   }
   schedule("KronosHUD::heartbeat(" @ %generation @ ");", 30);
}

function KronosHUD::startHeartbeat()
{
   $KH::heartbeatGeneration++;
   schedule("KronosHUD::heartbeat(" @ $KH::heartbeatGeneration @ ");", 30);
}

// One callback, one deterministic layer order. Companion files only define
// their renderers; they never replace this function.
function ScriptGL::playGui::onPostDraw(%dimensions)
{
   if(isFunction("KronosMenu::screenDim"))
      %screen = KronosMenu::screenDim(%dimensions);
   else
      %screen = %dimensions;

   glPartScale(0, 0, 1);

   // When Kronos is the active pack, ModernHUDPack::draw already rendered these.
   // With another active pack, explicit Kronos slot borrowing renders them here.
   if($ModernHUD::PackId != "kronos")
      KronosHUD::drawPersistent(%screen);
   KronosHUD::applySlotVisibility();
   KronosHUD::drawTransient(%screen);

   if(isFunction("KronosMenu::render")) KronosMenu::render(%screen);
   if(isFunction("KronosShop::render")) KronosShop::render(%screen);
   if(isFunction("KronosChat::render"))
      KronosChat::render(getWord(%screen, 0), getWord(%screen, 1));
   if(isFunction("KronosNPC::render"))
      KronosNPC::render(getWord(%screen, 0), getWord(%screen, 1));
   if(isFunction("KronosMenu::renderSlider"))
      KronosMenu::renderSlider(getWord(%screen, 0), getWord(%screen, 1));
   if(isFunction("KronosMenu::renderChatGrip"))
      KronosMenu::renderChatGrip(getWord(%screen, 0), getWord(%screen, 1));
}

// Load the companion client scripts once, in their dependency order.
if($KronosHUD::CompanionsLoaded == "")
{
   $KronosHUD::CompanionsLoaded = true;
   exec("Presto/KronosInput.cs");
   exec("Presto/KronosMenu.cs");
   exec("Presto/KronosShop.cs");
   exec("Presto/KronosChat.cs");
   exec("Presto/KronosNPC.cs");
}

echo("KronosHUD: ModernHUD-compatible RPG client bridge loaded");
