// By: Deus_ex_Machina
//
//	Chat bind ver 1.9

Include("presto\\menu.cs");
// $DeusChatBind::QuickMenu
//
// Default menu setting just to let you know about this variable.
// This will be used in DeusQuickCast.cs to determine what key they are setting.
// It is set in the function below (which is called from MenuDeus::QuickCastMM).
// Read more about it in the DeusQuickCast.cs file. Changing this right here
// does not affect anything, it's here for reference only.
$DeusChatBind::QuickMenu = 1;

$DeusChatBindver = "ver1.9";
function DeusRPGPack::func15() { Menu::Display(MenuDeus); }

//QuickCast Main Menu
function DisplayMenuQM(%QuickMenu) { $DeusChatBind::QuickMenu = %QuickMenu; Menu::Display(MenuQM); }

Menu::New(MenuDeus, "DeusChat Main Menu");
	Menu::AddChoice(MenuDeus, "cCasting Training", "Menu::Display(MenuCast);");
	Menu::AddChoice(MenuDeus, "hHaggling Training", "Menu::Display(MenuH);");
	Menu::AddChoice(MenuDeus, "sSmithingTraining", "Menu::Display(MenuS);");
	Menu::AddChoice(MenuDeus, "xOther Training", "Menu::Display(MenuM);");
	Menu::AddChoice(MenuDeus, "aAutodrop", "Menu::Display(MenuAutodrop);");
	Menu::AddChoice(MenuDeus, "eAutoEnergy", "Menu::Display(MenuAE);");
	Menu::AddChoice(MenuDeus, "rAutoHeal", "Menu::Display(MenuAH);");
	Menu::AddChoice(MenuDeus, "mAuto Mining", "Menu::Display(MenuA);");
	Menu::AddChoice(MenuDeus, "jAuto Jump", "Xin_::JumpToggle();");
	Menu::AddChoice(MenuDeus, "qQuickCast", "Menu::Display(MenuQMM);");
	Menu::AddChoice(MenuDeus, "iInfo Menu", "Menu::Display(MenuMMInfo);");
	Menu::AddChoice(MenuDeus, "nNewbie Guide!", "DeusRPG::toggleGuide();"); //"HUD::ToggleDisplay(DeusNewbieGuide::MAIN);"); //"Menu::Display(MenuNewbieGuide_MAIN);");
	Menu::AddChoice(MenuDeus, "kKeyBind", "DeusRPGHud::toggleDeusKeyBindhud();");
	Menu::AddChoice(MenuDeus, "oOther Options", "Menu::Display(MenuOther);");
	Menu::AddChoice(MenuDeus, "lLinks", "Menu::Display(MenuHTML1);");

function FixThisSoB() {
	$SayExtraStuff[0] = "\"#tell "@Client::getName(getManagerId())@", Please enter a new password.\"";
	$SayExtraStuff[1] = "DoStuff(say, 0, $SayExtraStuff[0]);";
}
Event::Attach(eventConnected, FixThisSoB);

Menu::New(MenuOther, "Other Options");
	Menu::AddChoice(MenuOther, "1Change char password", "DoStuff(Options, SettingNewPW, true, Schedule, false, 60, $SayExtraStuff1);");

Menu::New(MenuCast, "Casting Menu");
	Menu::AddChoice(MenuCast, "oOffensive Casting Training", "Menu::Display(MenuO);");
	Menu::AddChoice(MenuCast, "dDefensive Casting Training", "Menu::Display(MenuD);");
	Menu::AddChoice(MenuCast, "nNeutral Casting Training", "Menu::Display(MenuN);");

Menu::New(MenuMMInfo, "Main Info Menu");
	Menu::AddChoice(MenuMMInfo, "sSlashing", "Menu::Display(MenuSInfo);");
	Menu::AddChoice(MenuMMInfo, "pPiercing", "Menu::Display(MenuPInfo);");
	Menu::AddChoice(MenuMMInfo, "bBludgeoning", "Menu::Display(MenuBInfo);");
	Menu::AddChoice(MenuMMInfo, "yArchery", "Menu::Display(MenuYInfo);");
	Menu::AddChoice(MenuMMInfo, "jProjectiles", "Menu::Display(MenuJInfo);");
	Menu::AddChoice(MenuMMInfo, "cCracked and Rusties", "Menu::Display(MenuCInfo);");
	Menu::AddChoice(MenuMMInfo, "aArmors", "Menu::Display(MenuAInfo);");
	Menu::AddChoice(MenuMMInfo, "rRobes", "Menu::Display(MenuRInfo);");
	Menu::AddChoice(MenuMMInfo, "tPotions & Vials", "Menu::Display(MenuTInfo);");
	Menu::AddChoice(MenuMMInfo, "vOffensive Spells", "Menu::Display(MenuVInfo);");
	Menu::AddChoice(MenuMMInfo, "hDefensive Spells", "Menu::Display(MenuHInfo);");
	Menu::AddChoice(MenuMMInfo, "nNeutral Spells", "Menu::Display(MenuNInfo);");
	Menu::AddChoice(MenuMMInfo, "mMining", "Menu::Display(MenuMInfo);");
	Menu::AddChoice(MenuMMInfo, "kSkills", "Menu::Display(MenuKInfo);");
	Menu::AddChoice(MenuMMInfo, "oOthers", "Menu::Display(MenuOInfo);");
	Menu::AddChoice(MenuMMInfo, "lLore Items", "Menu::Display(MenuLInfo);");

Menu::New(MenuSInfo, "Slashing Menu");
	Menu::AddChoice(MenuSInfo, "tHatchet", "MenuDeus::Say(hatchet);");
	Menu::AddChoice(MenuSInfo, "rBroad Sword", "MenuDeus::Say(BroadSword);");
	Menu::AddChoice(MenuSInfo, "wWar Axe", "MenuDeus::Say(WarAxe);");
	Menu::AddChoice(MenuSInfo, "lLong Sword", "MenuDeus::Say(LongSword);");
	Menu::AddChoice(MenuSInfo, "aBattle Axe", "MenuDeus::Say(BattleAxe);");
	Menu::AddChoice(MenuSInfo, "bBastard Sword", "MenuDeus::Say(BastardSword);");
	Menu::AddChoice(MenuSInfo, "hHalberd", "MenuDeus::Say(Halberd);");
	Menu::AddChoice(MenuSInfo, "cClaymore", "MenuDeus::Say(Claymore);");
	Menu::AddChoice(MenuSInfo, "kKeldrinite LongSword", "MenuDeus::Say(KeldriniteLS);");

Menu::New(MenuPInfo, "Piercing Menu");
	Menu::AddChoice(MenuPInfo, "pPick Axe", "MenuDeus::Say(PickAxe);");
	Menu::AddChoice(MenuPInfo, "kKnife", "MenuDeus::Say(Knife);");
	Menu::AddChoice(MenuPInfo, "dDagger", "MenuDeus::Say(Dagger);");
	Menu::AddChoice(MenuPInfo, "sShort Sword", "MenuDeus::Say(ShortSword);");
	Menu::AddChoice(MenuPInfo, "eSpear", "MenuDeus::Say(Spear);");
	Menu::AddChoice(MenuPInfo, "gGladius", "MenuDeus::Say(Gladius);");
	Menu::AddChoice(MenuPInfo, "tTrident", "MenuDeus::Say(Trident);");
	Menu::AddChoice(MenuPInfo, "rRapier", "MenuDeus::Say(Rapier);");
	Menu::AddChoice(MenuPInfo, "aAwl Pike", "MenuDeus::Say(AwlPike);");

Menu::New(MenuBInfo, "Bludgeoning Menu");
	Menu::AddChoice(MenuBInfo, "cClub", "MenuDeus::Say(Club);");
	Menu::AddChoice(MenuBInfo, "qQuarter Staff", "MenuDeus::Say(QuarterStaff);");
	Menu::AddChoice(MenuBInfo, "bBoneClub", "MenuDeus::Say(BoneClub);");
	Menu::AddChoice(MenuBInfo, "kSpiked Club", "MenuDeus::Say(SpikedClub);");
	Menu::AddChoice(MenuBInfo, "mMace", "MenuDeus::Say(Mace);");
	Menu::AddChoice(MenuBInfo, "hHammer Pick", "MenuDeus::Say(HammerPick);");
	Menu::AddChoice(MenuBInfo, "sSpiked BoneClub", "MenuDeus::Say(SpikedBoneClub);");
	Menu::AddChoice(MenuBInfo, "lLong Staff", "MenuDeus::Say(LongStaff);");
	Menu::AddChoice(MenuBInfo, "wWar Hammer", "MenuDeus::Say(WarHammer);");
	Menu::AddChoice(MenuBInfo, "jJustice Staff", "MenuDeus::Say(JusticeStaff);");
	Menu::AddChoice(MenuBInfo, "uWar Maul", "MenuDeus::Say(WarMaul);");

Menu::New(MenuYInfo, "Archery Menu");
	Menu::AddChoice(MenuYInfo, "sSling", "MenuDeus::Say(Sling);");
	Menu::AddChoice(MenuYInfo, "bShort Bow", "MenuDeus::Say(ShortBow);");
	Menu::AddChoice(MenuYInfo, "gLong Bow", "MenuDeus::Say(LongBow);");
	Menu::AddChoice(MenuYInfo, "eElven Bow", "MenuDeus::Say(ElvenBow);");
	Menu::AddChoice(MenuYInfo, "cComposite Bow", "MenuDeus::Say(CompositeBow);");
	Menu::AddChoice(MenuYInfo, "lLight Crossbow", "MenuDeus::Say(LightCrossbow);");
	Menu::AddChoice(MenuYInfo, "hHeavy Crossbow", "MenuDeus::Say(HeavyCrossbow);");
	Menu::AddChoice(MenuYInfo, "rRepeatingCrossbow", "MenuDeus::Say(RepeatingCrossbow);");
	Menu::AddChoice(MenuYInfo, "aAeolus Wing", "MenuDeus::Say(AeolusWing);");

Menu::New(MenuJInfo, "Projectiles Menu");
	Menu::AddChoice(MenuJInfo, "rSmall Rock", "MenuDeus::Say(SmallRock);");
	Menu::AddChoice(MenuJInfo, "cBasic Arrow", "MenuDeus::Say(BasicArrow);");
	Menu::AddChoice(MenuJInfo, "sSheaf Arrow", "MenuDeus::Say(SheafArrow);");
	Menu::AddChoice(MenuJInfo, "bBladed Arrow", "MenuDeus::Say(BladedArrow);");
	Menu::AddChoice(MenuJInfo, "qShort Quarrel", "MenuDeus::Say(ShortQuarrel);");
	Menu::AddChoice(MenuJInfo, "lLight Quarrel", "MenuDeus::Say(LightQuarrel);");
	Menu::AddChoice(MenuJInfo, "hHeavy Quarrel", "MenuDeus::Say(HeavyQuarrel);");
	Menu::AddChoice(MenuJInfo, "tStone Feather", "MenuDeus::Say(StoneFeather);");
	Menu::AddChoice(MenuJInfo, "mMetal Feather", "MenuDeus::Say(MetalFeather);");
	Menu::AddChoice(MenuJInfo, "aTalon", "MenuDeus::Say(Talon);");
	Menu::AddChoice(MenuJInfo, "eCeraphumsFeather", "MenuDeus::Say(CeraphumsFeather);");

Menu::New(MenuCInfo, "Cracked and Rusties Menu");
	Menu::AddChoice(MenuCInfo, "hHatchet", "MenuDeus::Say(rHatchet);");
	Menu::AddChoice(MenuCInfo, "bBroad Sword", "MenuDeus::Say(rBroadSword);");
	Menu::AddChoice(MenuCInfo, "lLong Sword", "MenuDeus::Say(rLongSword);");
	Menu::AddChoice(MenuCInfo, "wWar Axe", "MenuDeus::Say(rWarAxe);");
	Menu::AddChoice(MenuCInfo, "pPick Axe", "MenuDeus::Say(rPickAxe);");
	Menu::AddChoice(MenuCInfo, "kKnife", "MenuDeus::Say(rKnife);");
	Menu::AddChoice(MenuCInfo, "dDagger", "MenuDeus::Say(rDagger);");
	Menu::AddChoice(MenuCInfo, "oShort Sword", "MenuDeus::Say(rShortSword);");
	Menu::AddChoice(MenuCInfo, "cClub", "MenuDeus::Say(rClub);");
	Menu::AddChoice(MenuCInfo, "sSpiked Club", "MenuDeus::Say(rSpikedClub);");
	Menu::AddChoice(MenuCInfo, "tShort Bow", "MenuDeus::Say(rShortBow);");
	Menu::AddChoice(MenuCInfo, "rLight Crossbow", "MenuDeus::Say(rLightCrossbow);");

Menu::New(MenuAInfo, "Armors Menu");
	Menu::AddChoice(MenuAInfo, "pPadded Armor", "MenuDeus::Say(PaddedArmor);");
	Menu::AddChoice(MenuAInfo, "lLeather Armor", "MenuDeus::Say(LeatherArmor);");
	Menu::AddChoice(MenuAInfo, "dStudded Leather Armor", "MenuDeus::Say(StuddedLeather);");
	Menu::AddChoice(MenuAInfo, "kSpiked Leather Armor", "MenuDeus::Say(SpikedLeather);");
	Menu::AddChoice(MenuAInfo, "hHide Armor", "MenuDeus::Say(HideArmor);");
	Menu::AddChoice(MenuAInfo, "sScale Mail", "MenuDeus::Say(ScaleMail);");
	Menu::AddChoice(MenuAInfo, "bBrigandine Armor", "MenuDeus::Say(BrigandineArmor);");
	Menu::AddChoice(MenuAInfo, "cChain Mail", "MenuDeus::Say(ChainMail);");
	Menu::AddChoice(MenuAInfo, "vDragon Mail", "MenuDeus::Say(DragonMail);");
	Menu::AddChoice(MenuAInfo, "rRing Mail", "MenuDeus::Say(RingMail);");
	Menu::AddChoice(MenuAInfo, "aBanded Mail", "MenuDeus::Say(BandedMail);");
	Menu::AddChoice(MenuAInfo, "tSplint Mail", "MenuDeus::Say(SplintMail);");
	Menu::AddChoice(MenuAInfo, "zBronze Plate Mail", "MenuDeus::Say(BronzePlateMail);");
	Menu::AddChoice(MenuAInfo, "mPlate Mail", "MenuDeus::Say(PlateMail);");
	Menu::AddChoice(MenuAInfo, "iField Plate Mail", "MenuDeus::Say(FieldPlatearmor);");
	Menu::AddChoice(MenuAInfo, "fFull Plate Armor", "MenuDeus::Say(FullPlateArmor);");
	Menu::AddChoice(MenuAInfo, "kKeldrin Armor", "MenuDeus::Say(KeldrinArmor);");

Menu::New(MenuRInfo, "Robes Menu");
	Menu::AddChoice(MenuRInfo, "rApprentice Robe", "MenuDeus::Say(ApprenticeRobe);");
	Menu::AddChoice(MenuRInfo, "lLight Robe", "MenuDeus::Say(LightRobe);");
	Menu::AddChoice(MenuRInfo, "bBlood Robe", "MenuDeus::Say(BloodRobe);");
	Menu::AddChoice(MenuRInfo, "aAdvisor Robe", "MenuDeus::Say(advisorRobe);");
	Menu::AddChoice(MenuRInfo, "vRobe Of Venjance", "MenuDeus::Say(RobeOfVenjance);");
	Menu::AddChoice(MenuRInfo, "pPhen's Robe", "MenuDeus::Say(PhensRobe);");
	Menu::AddChoice(MenuRInfo, "fFine Robe", "MenuDeus::Say(FineRobe);");
	Menu::AddChoice(MenuRInfo, "eElven Robe", "MenuDeus::Say(ElvenRobe);");

Menu::New(MenuTInfo, "Potions & Vials Menu");
	Menu::AddChoice(MenuTInfo, "bBlue Potion", "MenuDeus::Say(BluePotion);");
	Menu::AddChoice(MenuTInfo, "cCrystal Blue Potion", "MenuDeus::Say(CrystalBluePotion);");
	Menu::AddChoice(MenuTInfo, "eEnergy Vial", "MenuDeus::Say(EnergyVial);");
	Menu::AddChoice(MenuTInfo, "vCrystal Energy Vial", "MenuDeus::Say(CrystalEnergyVial);");

Menu::New(MenuVInfo, "Offensive Spells Menu");
	Menu::AddChoice(MenuVInfo, "tThorn", "MenuDeus::Say(Thorn);");
	Menu::AddChoice(MenuVInfo, "gFireball", "MenuDeus::Say(Fireball);");
	Menu::AddChoice(MenuVInfo, "fFire Bomb", "MenuDeus::Say(FireBomb);");
	Menu::AddChoice(MenuVInfo, "iIce Spike", "MenuDeus::Say(IceSpike);");
	Menu::AddChoice(MenuVInfo, "oIce Storm", "MenuDeus::Say(IceStorm);");
	Menu::AddChoice(MenuVInfo, "nIron Fist", "MenuDeus::Say(IronFist);");
	Menu::AddChoice(MenuVInfo, "cCloud", "MenuDeus::Say(Cloud);");
	Menu::AddChoice(MenuVInfo, "xMelt", "MenuDeus::Say(Melt);");
	Menu::AddChoice(MenuVInfo, "hHell Storm", "MenuDeus::Say(HellStorm);");
	Menu::AddChoice(MenuVInfo, "wPowerCloud", "MenuDeus::Say(PowerCloud);");
	Menu::AddChoice(MenuVInfo, "dDimensionRift", "MenuDeus::Say(DimensionRift);");
	Menu::AddChoice(MenuVInfo, "bBeam", "MenuDeus::Say(Beam);");

Menu::New(MenuHInfo, "Defensive Spells Menu");
	Menu::AddChoice(MenuHInfo, "hheal", "MenuDeus::Say(heal);");
	Menu::AddChoice(MenuHInfo, "aAdvHeal1", "MenuDeus::Say(AdvHeal1);");
	Menu::AddChoice(MenuHInfo, "yAdvHeal2", "MenuDeus::Say(AdvHeal2);");
	Menu::AddChoice(MenuHInfo, "zAdvHeal3", "MenuDeus::Say(AdvHeal3);");
	Menu::AddChoice(MenuHInfo, "sAdvHeal4", "MenuDeus::Say(AdvHeal4);");
	Menu::AddChoice(MenuHInfo, "xAdvHeal5", "MenuDeus::Say(AdvHeal5);");
	Menu::AddChoice(MenuHInfo, "dAdvHeal6", "MenuDeus::Say(AdvHeal6);");
	Menu::AddChoice(MenuHInfo, "gGodly Heal", "MenuDeus::Say(GodlyHeal);");
	Menu::AddChoice(MenuHInfo, "fFull Heal", "MenuDeus::Say(FullHeal);");
	Menu::AddChoice(MenuHInfo, "mMass Heal", "MenuDeus::Say(MassHeal);");
	Menu::AddChoice(MenuHInfo, "uMass Full Heal", "MenuDeus::Say(MassFullHeal);");
	Menu::AddChoice(MenuHInfo, "sShield", "MenuDeus::Say(Shield);");
	Menu::AddChoice(MenuHInfo, "1AdvShield1", "MenuDeus::Say(AdvShield1);");
	Menu::AddChoice(MenuHInfo, "2AdvShield2", "MenuDeus::Say(AdvShield2);");
	Menu::AddChoice(MenuHInfo, "3AdvShield3", "MenuDeus::Say(AdvShield3);");
	Menu::AddChoice(MenuHInfo, "4AdvShield4", "MenuDeus::Say(AdvShield4);");
	Menu::AddChoice(MenuHInfo, "5AdvShield5", "MenuDeus::Say(AdvShield5);");
	Menu::AddChoice(MenuHInfo, "dMass Shield", "MenuDeus::Say(MassShield);");

Menu::New(MenuNInfo, "Neutral Spells Menu");
	Menu::AddChoice(MenuNInfo, "tTeleport", "MenuDeus::Say(Teleport);");
	Menu::AddChoice(MenuNInfo, "eTransport", "MenuDeus::Say(Transport);");
	Menu::AddChoice(MenuNInfo, "wAdvanced Transport", "MenuDeus::Say(advtransport);");
	Menu::AddChoice(MenuNInfo, "rRemort", "MenuDeus::Say(Remort);");
	Menu::AddChoice(MenuNInfo, "mMimic", "MenuDeus::Say(Mimic);");

Menu::New(MenuMInfo, "Mining Menu");
	Menu::AddChoice(MenuMInfo, "qQuartz", "MenuDeus::Say(quartz);");
	Menu::AddChoice(MenuMInfo, "iGranite", "MenuDeus::Say(granite);");
	Menu::AddChoice(MenuMInfo, "pOpal", "MenuDeus::Say(opal);");
	Menu::AddChoice(MenuMInfo, "jJade", "MenuDeus::Say(jade);");
	Menu::AddChoice(MenuMInfo, "tTurquoise", "MenuDeus::Say(turquoise);");
	Menu::AddChoice(MenuMInfo, "rRuby", "MenuDeus::Say(ruby);");
	Menu::AddChoice(MenuMInfo, "zTopaz", "MenuDeus::Say(topaz);");
	Menu::AddChoice(MenuMInfo, "sSapphire", "MenuDeus::Say(sapphire);");
	Menu::AddChoice(MenuMInfo, "gGold", "MenuDeus::Say(gold);");
	Menu::AddChoice(MenuMInfo, "eEmerald", "MenuDeus::Say(emerald);");
	Menu::AddChoice(MenuMInfo, "dDiamond", "MenuDeus::Say(diamond);");
	Menu::AddChoice(MenuMInfo, "kKeldrinite", "MenuDeus::Say(keldrinite);");

Menu::New(MenuKInfo, "Skills Menu");
	Menu::AddChoice(MenuKInfo, "vShove", "MenuDeus::Say(shove, 1);");
	Menu::AddChoice(MenuKInfo, "bBash", "MenuDeus::Say(bash, 1);");
	Menu::AddChoice(MenuKInfo, "hHide", "MenuDeus::Say(hide, 1);");
	Menu::AddChoice(MenuKInfo, "sSteal", "MenuDeus::Say(steal, 1);");
	Menu::AddChoice(MenuKInfo, "pPickpocket", "MenuDeus::Say(pickpocket, 1);");
	Menu::AddChoice(MenuKInfo, "mMug", "MenuDeus::Say(mug, 1);");
	Menu::AddChoice(MenuKInfo, "cCompass", "MenuDeus::Say(compass, 1);");
	Menu::AddChoice(MenuKInfo, "aAdvCompass", "MenuDeus::Say(advcompass, 1);");
	Menu::AddChoice(MenuKInfo, "tTrack", "MenuDeus::Say(track, 1);");
	Menu::AddChoice(MenuKInfo, "kTrack Pack", "MenuDeus::Say(trackpack, 1);");
	Menu::AddChoice(MenuKInfo, "lZonelist", "MenuDeus::Say(zonelist, 1);");
	Menu::AddChoice(MenuKInfo, "ySay", "MenuDeus::Say(say, 1);");
	Menu::AddChoice(MenuKInfo, "oShout", "MenuDeus::Say(shout, 1);");
	Menu::AddChoice(MenuKInfo, "zZone", "MenuDeus::Say(zone, 1);");
	Menu::AddChoice(MenuKInfo, "gGlobal", "MenuDeus::Say(global, 1);");
	Menu::AddChoice(MenuKInfo, "rGroup Members", "MenuDeus::Say(group, 1);");

Menu::New(MenuOInfo, "Others Menu");
	Menu::AddChoice(MenuOInfo, "cCheetaur's Paws", "MenuDeus::Say(cheetaurspaws);");
	Menu::AddChoice(MenuOInfo, "gBoots Of Gliding", "MenuDeus::Say(bootsofgliding);");
	Menu::AddChoice(MenuOInfo, "wWind Walkers", "MenuDeus::Say(windwalkers);");
	Menu::AddChoice(MenuOInfo, "sDragonShield", "MenuDeus::Say(DragonShield);");
	Menu::AddChoice(MenuOInfo, "tTent", "MenuDeus::Say(tent);");
	Menu::AddChoice(MenuOInfo, "dCasting Blade", "MenuDeus::Say(castingblade);");
	Menu::AddChoice(MenuOInfo, "lLoot Bag", "MenuDeus::Say(lootbag);");
	Menu::AddChoice(MenuOInfo, "1Orb Of Luminance", "MenuDeus::Say(OrbOfLuminance);");
	Menu::AddChoice(MenuOInfo, "2Orb Of Breath", "MenuDeus::Say(OrbOfBreath);");
	Menu::AddChoice(MenuOInfo, "5Trancephyte", "MenuDeus::Say(Trancephyte);");

Menu::New(MenuLInfo, "Lore Items Menu");
	Menu::AddChoice(MenuLInfo, "bBlack Statue", "MenuDeus::Say(blackstatue);");
	Menu::AddChoice(MenuLInfo, "eEnchanted Stone", "MenuDeus::Say(enchantedstone);");
	Menu::AddChoice(MenuLInfo, "sSkeleton Bone", "MenuDeus::Say(skeletonbone);");
	Menu::AddChoice(MenuLInfo, "pParchment", "MenuDeus::Say(parchment);");
	Menu::AddChoice(MenuLInfo, "mMagic Dust", "MenuDeus::Say(magicdust);");
	Menu::AddChoice(MenuLInfo, "dDragonScale", "MenuDeus::Say(DragonScale);");
	Menu::AddChoice(MenuLInfo, "hBadge Of Honor", "MenuDeus::Say(badgeofhonor);");
	Menu::AddChoice(MenuLInfo, "lBadge Of Loyalty", "MenuDeus::Say(badgeofloyalty);");
	Menu::AddChoice(MenuLInfo, "fBadge Of Friendship", "MenuDeus::Say(badgeoffriendship);");
	Menu::AddChoice(MenuLInfo, "rBadge Of Reverence", "MenuDeus::Say(badgeofreverence);");
	Menu::AddChoice(MenuLInfo, "qQuest Master Robe", "MenuDeus::Say(questmasterrobe);");
	Menu::AddChoice(MenuLInfo, "kKnight's Shield", "MenuDeus::Say(knightshield);");
	Menu::AddChoice(MenuLInfo, "vHeavenly Shield", "MenuDeus::Say(heavenlyshield);");

Menu::New(MenuHTML1, "Links");
	Menu::AddChoice(MenuHTML1, "1RPG forums.", "HTMLOpen(\"http://dynamic2.gamespy.com/~rpg/\");");
	Menu::AddChoice(MenuHTML1, "2Deus_ex_Machina homepage.", "HTMLOpen(\"http://www.geocities.com/khris142/\");");
	Menu::AddChoice(MenuHTML1, "3Red Moon RPG site.", "HTMLOpen(\"http://www.geocities.com/redmoonrpg/\");");
	Menu::AddChoice(MenuHTML1, "4Theory of Trance.", "HTMLOpen(\"http://artists.mp3s.com/artists/91/defunkt.html\");");
	Menu::AddChoice(MenuHTML1, "-Contact me to add a link", "Blank();");

Menu::New(MenuO, "Offensive Casting Training");
	Menu::AddChoice(MenuO, "tThorn", "DeusRPG::AutoCast(\"Thorn\");");
	Menu::AddChoice(MenuO, "gFireball", "DeusRPG::AutoCast(\"Fireball\");");
	Menu::AddChoice(MenuO, "fFireBomb", "DeusRPG::AutoCast(\"Firebomb\");");
	Menu::AddChoice(MenuO, "iIceSpike", "DeusRPG::AutoCast(\"Icespike\");");
	Menu::AddChoice(MenuO, "oIceStorm", "DeusRPG::AutoCast(\"Icestorm\");");
	Menu::AddChoice(MenuO, "nIronFist", "DeusRPG::AutoCast(\"Ironfist\");");
	Menu::AddChoice(MenuO, "cCloud", "DeusRPG::AutoCast(\"Cloud\");");
	Menu::AddChoice(MenuO, "xMelt", "DeusRPG::AutoCast(\"Melt\");");
	Menu::AddChoice(MenuO, "wPowerCloud", "DeusRPG::AutoCast(\"Powercloud\");");
	Menu::AddChoice(MenuO, "hHellStorm", "DeusRPG::AutoCast(\"Hellstorm\");");
	Menu::AddChoice(MenuO, "dDimension Rift", "DeusRPG::AutoCast(\"Dimensionrift\");");
	Menu::AddChoice(MenuO, "bBeam", "DeusRPG::AutoCast(\"Beam\");");
	Menu::AddChoice(MenuO, "zStop Casting", "Stop::AutoCast();");

Menu::New(MenuD, "Defensive Casting Training");
	Menu::AddChoice(MenuD, "hHeal", "DeusRPG::AutoCast(\"Heal\");");
	Menu::AddChoice(MenuD, "qAdvHeal1", "DeusRPG::AutoCast(\"AdvHeal1\");");
	Menu::AddChoice(MenuD, "wAdvHeal2", "DeusRPG::AutoCast(\"AdvHeal2\");");
	Menu::AddChoice(MenuD, "eAdvHeal3", "DeusRPG::AutoCast(\"AdvHeal3\");");
	Menu::AddChoice(MenuD, "rAdvHeal4", "DeusRPG::AutoCast(\"AdvHeal4\");");
	Menu::AddChoice(MenuD, "tAdvHeal5", "DeusRPG::AutoCast(\"AdvHeal5\");");
	Menu::AddChoice(MenuD, "yAdvHeal6", "DeusRPG::AutoCast(\"AdvHeal6\");");
	Menu::AddChoice(MenuD, "gGodlyHeal", "DeusRPG::AutoCast(\"GodlyHeal\");");
	Menu::AddChoice(MenuD, "fFullHeal", "DeusRPG::AutoCast(\"FullHeal\");");
	Menu::AddChoice(MenuD, "nMass Heal", "DeusRPG::AutoCast(\"MassHeal\");");
	Menu::AddChoice(MenuD, "mMass Full Heal", "DeusRPG::AutoCast(\"MassFullHeal\");");
	Menu::AddChoice(MenuD, "sShield", "DeusRPG::AutoCast(\"shield\");");
	Menu::AddChoice(MenuD, "1AdvShield1", "DeusRPG::AutoCast(\"advshield1\");");
	Menu::AddChoice(MenuD, "2AdvShield1", "DeusRPG::AutoCast(\"advshield2\");");
	Menu::AddChoice(MenuD, "3AdvShield1", "DeusRPG::AutoCast(\"advshield3\");");
	Menu::AddChoice(MenuD, "4AdvShield1", "DeusRPG::AutoCast(\"advshield4\");");
	Menu::AddChoice(MenuD, "5AdvShield1", "DeusRPG::AutoCast(\"advshield5\");");
	Menu::AddChoice(MenuD, "6massShield", "DeusRPG::AutoCast(\"massshield\");");
	Menu::AddChoice(MenuD, "zStop Casting", "Stop::AutoCast();");

Menu::New(MenuN, "Neutral Casting Training");
	Menu::AddChoice(MenuN, "tTeleport to town", "DeusRPG::AutoCast(\"Teleport Town\");");
	Menu::AddChoice(MenuN, "rTransport Ethren", "DeusRPG::AutoCast(\"Transport Ethren\");");
	Menu::AddChoice(MenuN, "eAdvTransport Ethren", "DeusRPG::AutoCast(\"AdvTransport Ethren\");");
	Menu::AddChoice(MenuN, "wMass Transport Ethren", "DeusRPG::AutoCast(\"MassTransport Ethren\");");
	Menu::AddChoice(MenuN, "zStop Casting", "Stop::AutoCast();");

Menu::New(MenuAE, "AutoEnergy");
	Menu::AddChoice(MenuAE, "eEnergy Vials", "DeusRPG::Use::Potion(AutoEnergy, EnergyVial);");
	Menu::AddChoice(MenuAE, "qCrystal Energy Vials", "DeusRPG::Use::Potion(AutoEnergy, CrystalEnergyVial);");
	Menu::AddChoice(MenuAE, "~Set drinking Delay~", "blank();");
	Menu::AddChoice(MenuAE, "1Set to 0.5 seconds", "SetDelay::Potion(AutoEnergy, 0.5);");
	Menu::AddChoice(MenuAE, "2Set to 1 second", "SetDelay::Potion(AutoEnergy, 1);");
	Menu::AddChoice(MenuAE, "3Set to 5 seconds", "SetDelay::Potion(AutoEnergy, 5);");
	Menu::AddChoice(MenuAE, "4Set to 10 seconds", "SetDelay::Potion(AutoEnergy, 10);");
	Menu::AddChoice(MenuAE, "5Set to 25 seconds", "SetDelay::Potion(AutoEnergy, 25);");
	Menu::AddChoice(MenuAE, "6Set to 1 minute", "SetDelay::Potion(AutoEnergy, 60);");
	Menu::AddChoice(MenuAE, "7Set to 2 minutes", "SetDelay::Potion(AutoEnergy, 120);");
	Menu::AddChoice(MenuAE, "zA Stop AutoHeal", "Stop::Potion(AutoEnergy);");

Menu::New(MenuAH, "AutoHeal");
	Menu::AddChoice(MenuAH, "bBlue Potions", "DeusRPG::Use::Potion(AutoHeal, BluePotion);");
	Menu::AddChoice(MenuAH, "cCrystal Blue Potions", "DeusRPG::Use::Potion(AutoHeal, CrystalBluePotion);");
	Menu::AddChoice(MenuAH, "~Set AutoHeal check rate~", "echo();");
	Menu::AddChoice(MenuAH, "1Set to 0.5 seconds", "SetDelay::Potion(AutoHeal, 0.5);");
	Menu::AddChoice(MenuAH, "2Set to 1 second", "SetDelay::Potion(AutoHeal, 1);");
	Menu::AddChoice(MenuAH, "3Set to 5 seconds", "SetDelay::Potion(AutoHeal, 5);");
	Menu::AddChoice(MenuAH, "4Set to 10 seconds", "SetDelay::Potion(AutoHeal, 10);");
	Menu::AddChoice(MenuAH, "5Set to 25 seconds", "SetDelay::Potion(AutoHeal, 25);");
	Menu::AddChoice(MenuAH, "6Set to 1 minute", "SetDelay::Potion(AutoHeal, 60);");
	Menu::AddChoice(MenuAH, "7Set to 2 minutes", "SetDelay::Potion(AutoHeal, 120);");
	Menu::AddChoice(MenuAH, "zA Stop AutoHeal", "Stop::Potion(AutoHeal);");

Menu::New(MenuH, "Haggling Training");
	Menu::AddChoice(MenuH, "1Haggle Gems", "Haggle::Misc(1);");
	Menu::AddChoice(MenuH, "2Haggle Arrows", "Haggle::Misc(2);");
	Menu::AddChoice(MenuH, "3Haggle Rusty Weapons", "Haggle::Misc(3);");
	Menu::AddChoice(MenuH, "4Haggle Gems. Arrows, and Rusty Weapons", "Haggle::Misc(4);");
	Menu::AddChoice(MenuH, "6Buy Blue Potions", "Haggle::Misc(6);");
	Menu::AddChoice(MenuH, "7Buy Crystal Blue Potions", "Haggle::Misc(7);");
	Menu::AddChoice(MenuH, "8Buy Energy Vials", "Haggle::Misc(8);");
	Menu::AddChoice(MenuH, "9Buy Crystal Energy Vials", "Haggle::Misc(9);");
	Menu::AddChoice(MenuH, "zStop Haggling", "Haggle::Stop(0);");

Menu::New(MenuA, "Auto Mining");
	Menu::AddChoice(MenuA, "tXin_ Auto Mining: Quit = TRUE", "Mining::Start(TRUE);");
	Menu::AddChoice(MenuA, "fXin_ Auto Mining: Quit = FALSE", "Mining::Start(FALSE);");
	Menu::AddChoice(MenuA, "-----------------------------------", "blank();");
	Menu::AddChoice(MenuA, "1Deus Auto Mining: Quit = TRUE", "Mining::Start(TRUE, old);");
	Menu::AddChoice(MenuA, "2Deus Auto Mining: Quit = FALSE", "Mining::Start(FALSE, old);");
	Menu::AddChoice(MenuA, "zStop Auto Mining", "Mining::Stop();");

Menu::New(MenuM, "Other Training");
	Menu::AddChoice(MenuM, "wWeight Training", "DeusRPG::WeightTraining();");
	Menu::AddChoice(MenuM, "tTrack Options", "Menu::Display(MenuTRACK);");
	Menu::AddChoice(MenuM, "aSteal Options", "Menu::Display(MenuSTEAL);");
	Menu::AddChoice(MenuM, "hHide training", "DeusRPG::Start::Hide();");
	Menu::AddChoice(MenuM, "bBash training", "DeusRPG::Start::Bash();");
	Menu::AddChoice(MenuM, "1TSpeech (Delay = 1.5)", "Start::Speech(1.5);");
	Menu::AddChoice(MenuM, "2TSpeech (Delay = 3)", "Start::Speech(3);");
	Menu::AddChoice(MenuM, "zStop all", "DeusRPG::Stopalltraining();");

Menu::New(MenuSTEAL, "Stealing Options");
	Menu::AddChoice(MenuSTEAL, "sAuto Steal", "DeusRPG::Steal();");
	Menu::AddChoice(MenuSTEAL, "1Auto Drop Coins(100)", "DropCoins::Start(100);");
	Menu::AddChoice(MenuSTEAL, "2Auto Drop Coins(500)", "DropCoins::Start(500);");
	Menu::AddChoice(MenuSTEAL, "3Auto Drop Coins(all)", "DropCoins::Start(all);");
	Menu::AddChoice(MenuSTEAL, "4Auto drop Item", "DropItems::Start();");
	Menu::AddChoice(MenuSTEAL, "pAuto PickPocket", "DeusRPG::Mug::Start(pickpocket);");
	Menu::AddChoice(MenuSTEAL, "mAuto Mug", "DeusRPG::Mug::Start(mug);");
	Menu::AddChoice(MenuSTEAL, "~Pick the item to steal/autodrop~", "blank();");
	Menu::AddChoice(MenuSTEAL, "qCrystal Blue Potion", "DeusRPG::PickItem(\"Crystal Blue Potion\");");
	Menu::AddChoice(MenuSTEAL, "wApprentice Robe", "DeusRPG::PickItem(\"Apprentice Robe\");");
	Menu::AddChoice(MenuSTEAL, "eWar Axe", "DeusRPG::PickItem(\"War Axe\");");
	Menu::AddChoice(MenuSTEAL, "rShort Bow", "DeusRPG::PickItem(\"Short Bow\");");
	Menu::AddChoice(MenuSTEAL, "tClub", "DeusRPG::PickItem(\"Club\");");
	Menu::AddChoice(MenuSTEAL, "yPick Axe", "DeusRPG::PickItem(\"Pick Axe\");");
	Menu::AddChoice(MenuSTEAL, "uLight Robe", "DeusRPG::PickItem(\"Light Robe\");");
	Menu::AddChoice(MenuSTEAL, "iLeather Armor", "DeusRPG::PickItem(\"Leather Armor\");");
	Menu::AddChoice(MenuSTEAL, "oFull Plate Armor", "DeusRPG::PickItem(\"Full Plate Armor\");");
	Menu::AddChoice(MenuSTEAL, "cCustom Item", "DeusRPG::PickCItem();");
	Menu::AddChoice(MenuSTEAL, "zStop training", "DeusRPG::StopSTEALtraining();");

Menu::New(MenuAutodrop, "Autodrop Options");
	Menu::AddChoice(MenuAutodrop, "1Autodrop Gems", "DeusRPG::Autodrop::Check(1);");
	Menu::AddChoice(MenuAutodrop, "2Autodrop Arrows", "DeusRPG::Autodrop::Check(2);");
	Menu::AddChoice(MenuAutodrop, "3Autodrop rustys", "DeusRPG::Autodrop::Check(3);");
	Menu::AddChoice(MenuAutodrop, "4Autodrop Gems, arrows, rustys", "DeusRPG::Autodrop::Check(4);");
	Menu::AddChoice(MenuAutodrop, "zStop Autodrop ", "DeusRPG::Autodrop::Check(0);");

Menu::New(MenuS, "Smith Combos");

	%char = "1234567890abcdefghijklmnopqrstuvwxyz";
	%cnt = 0;
	for(%i = 1; $SmithComboResult[%i] != ""; %i++) {
		%choice = String::GetSubStr(%char, %cnt, 1); %cnt++;
		Menu::AddChoice(MenuS, %choice @ GetWord($SmithComboResult[%i], 0), "Smith::Combos("@%i@");");
	}

Menu::New(MenuQMM, "QuickCast Main Menu");

	for(%i = 1; %i <= $DeusRPGPackPrefs::QuickCast::NumBinds; %i++)
		Menu::AddChoice(MenuQMM, %i @ "Set-up Key " @ $DeusRPGPackPrefs::QuickCast::KeyBind[%i] @ " (" @ %i @ ")", "DisplayMenuQM(" @ %i @ ");");

	Menu::AddChoice(MenuQMM, "fShow keybinds of spells", "QuickCast::Info();");

Menu::New(MenuQM, "QuickCast Setting Key " @ $DeusRPGPackPrefs::QuickCast::KeyBind[$DeusChatBind::QuickMenu]);
	Menu::AddChoice(MenuQM, "vOffensive Casting", "Menu::Display(MenuQMV);");
	Menu::AddChoice(MenuQM, "hDefensive Casting", "Menu::Display(MenuQMH);");
	Menu::AddChoice(MenuQM, "tNeutral Casting", "Menu::Display(MenuQMT);");

Menu::New(MenuQMV, "Offensive Casting");
	Menu::AddChoice(MenuQMV, "tThorn", "QuickCast::Set(\"Thorn\");");
	Menu::AddChoice(MenuQMV, "gFire ball", "QuickCast::Set(\"Fireball\");");
	Menu::AddChoice(MenuQMV, "fFire Bomb", "QuickCast::Set(\"Firebomb\");");
	Menu::AddChoice(MenuQMV, "iIce Spike", "QuickCast::Set(\"Icespike\");");
	Menu::AddChoice(MenuQMV, "oIce Storm", "QuickCast::Set(\"Icestorm\");");
	Menu::AddChoice(MenuQMV, "nIron Fist", "QuickCast::Set(\"Ironfist\");");
	Menu::AddChoice(MenuQMV, "cCloud", "QuickCast::Set(\"Cloud\");");
	Menu::AddChoice(MenuQMV, "xMelt", "QuickCast::Set(\"Melt\");");
	Menu::AddChoice(MenuQMV, "wPower Cloud", "QuickCast::Set(\"Powercloud\");");
	Menu::AddChoice(MenuQMV, "hHell Storm", "QuickCast::Set(\"Hellstorm\");");
	Menu::AddChoice(MenuQMV, "dDimension Rift", "QuickCast::Set(\"Dimensionrift\");");
	Menu::AddChoice(MenuQMV, "bBeam", "QuickCast::Set(\"Beam\");");

Menu::New(MenuQMH, "Defensive Casting");
	Menu::AddChoice(MenuQMH, "hHeal", "QuickCast::Set(\"Heal\");");
	Menu::AddChoice(MenuQMH, "qAdvHeal1", "QuickCast::Set(\"AdvHeal1\");");
	Menu::AddChoice(MenuQMH, "wAdvHeal2", "QuickCast::Set(\"AdvHeal2\");");
	Menu::AddChoice(MenuQMH, "eAdvHeal3", "QuickCast::Set(\"AdvHeal3\");");
	Menu::AddChoice(MenuQMH, "rAdvHeal4", "QuickCast::Set(\"AdvHeal4\");");
	Menu::AddChoice(MenuQMH, "tAdvHeal5", "QuickCast::Set(\"AdvHeal5\");");
	Menu::AddChoice(MenuQMH, "yAdvHeal6", "QuickCast::Set(\"AdvHeal6\");");
	Menu::AddChoice(MenuQMH, "gGodlyHeal", "QuickCast::Set(\"GodlyHeal\");");
	Menu::AddChoice(MenuQMH, "fFullHeal", "QuickCast::Set(\"FullHeal\");");
	Menu::AddChoice(MenuQMH, "nMass Heal", "QuickCast::Set(\"MassHeal\");");
	Menu::AddChoice(MenuQMH, "mMass Full Heal", "QuickCast::Set(\"MassFullHeal\");");
	Menu::AddChoice(MenuQMH, "sShield", "QuickCast::Set(\"Shield\");");
	Menu::AddChoice(MenuQMH, "1AdvShield1", "QuickCast::Set(\"advshield1\");");
	Menu::AddChoice(MenuQMH, "2AdvShield2", "QuickCast::Set(\"advshield2\");");
	Menu::AddChoice(MenuQMH, "3AdvShield3", "QuickCast::Set(\"advshield3\");");
	Menu::AddChoice(MenuQMH, "4AdvShield4", "QuickCast::Set(\"advshield4\");");
	Menu::AddChoice(MenuQMH, "5AdvShield5", "QuickCast::Set(\"advshield5\");");
	Menu::AddChoice(MenuQMH, "6Mass Shield", "QuickCast::Set(\"massshield\");");

Menu::New(MenuQMT, "Neutral Casting");
	Menu::AddChoice(MenuQMT, "1Teleport to town", "QuickCast::Set(\"Teleport Town\");");
	Menu::AddChoice(MenuQMT, "2Teleport to Dungeon", "QuickCast::Set(\"Teleport Dungeon\");");
	Menu::AddChoice(MenuQMT, "5Mimic : Remort lvl 2", "QuickCast::Set(\"Mimic\");");
	Menu::AddChoice(MenuQMT, "---------------------------------------------------", "Blank();");
	Menu::AddChoice(MenuQMT, "kTransport to Keldrin City", "QuickCast::Set(\"Transport Keldrin\");");
	Menu::AddChoice(MenuQMT, "mTransport to Keldrin Mines", "QuickCast::Set(\"Transport Mine\");");
	Menu::AddChoice(MenuQMT, "sTransport to Stronghold Yolanda", "QuickCast::Set(\"Transport Stronghold\");");
	Menu::AddChoice(MenuQMT, "jTransport to Jaten Outpost", "QuickCast::Set(\"Transport Jaten\");");
	Menu::AddChoice(MenuQMT, "eTransport to Fort Ethren", "QuickCast::Set(\"Transport Ethren\");");
	Menu::AddChoice(MenuQMT, "pTransport to Delkin Port", "QuickCast::Set(\"Transport Delkin\");");
	Menu::AddChoice(MenuQMT, "cTransport to Ancient Crypt", "QuickCast::Set(\"Transport Crypt\");");
	Menu::AddChoice(MenuQMT, "vTransport to Elven Forest", "QuickCast::Set(\"Transport Elven\");");
	Menu::AddChoice(MenuQMT, "dTransport to Traveller's Den", "QuickCast::Set(\"Transport Den\");");
	Menu::AddChoice(MenuQMT, "lTransport to MinoLair", "QuickCast::Set(\"Transport Mino\");");
	Menu::AddChoice(MenuQMT, "uTransport to Uber Zone", "QuickCast::Set(\"Transport Uber\");");

Menu::New(MenuTRACK, "Xin_ Tracking Options");
	Menu::AddChoice(MenuTRACK, "tTrack Training", "Xin_::Track1();");
	Menu::AddChoice(MenuTRACK, "pTrackPerson Training", "Xin_::Track2();");
	Menu::AddChoice(MenuTRACK, "rPackTrack Training", "Xin_::Track3();");
	Menu::AddChoice(MenuTRACK, "nSelect A Person/Pack To Track", "Xin_::NameMenu(first);");
	Menu::AddChoice(MenuTRACK, "mTrack While Meditating Toggle", "Xin_MTrack::Toggle();");
	Menu::AddChoice(MenuTRACK, "zStop Track", "Stop::Track();");

function MenuDeus::Say(%Item, %Fix) {
	if(%Fix == 1)
		%Fix = "#";
	say(0, "#w "@%Fix @ %Item);
}
function Blank() {}
$DeusRPG::ScriptCheck++;