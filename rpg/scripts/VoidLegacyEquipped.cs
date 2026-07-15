//==============================================================================
// VoidLegacyEquipped.cs - VOID Phase 1b (2026-07-14)
//
// The 23 "Equipped"-class X0 armor datablocks, MOVED here from armors.cs and
// newstuff.cs so they register LAST (exec'd from the end of BeltWeapons.cs,
// the final exec). Purpose: the 40 new VSlot rows push total registrations to
// ~221, over the 200 per-player itemTypeList cap (retail 1.40) - but these X0
// blocks never receive COUNTS anymore (armor equip is the belt path since
// Phase 1a; the GiveThisStuff migration branch converts saved X0 tokens
// without touching engine counts), and count READS above the cap are bounds-
// checked. Parking exactly these at the top indices (~198-220) keeps every
// count-bearing item at <= ~197.
//
// They still exist at all only for the TRANSITION (mounted-armor visuals on
// not-yet-migrated players mid-session, and itemevents' Equipped-class
// branch). Phase 2 deletes this file outright.
//==============================================================================

ItemData RatSkinShirt0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Rat Skin Shirt";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData StuddedLeatherSuit0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Studded Leather Suit";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData ToughHideSuit0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Tough Hide Suit";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData IronScaleMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Iron Scale Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData SteelScaleMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Steel Scale Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData SteelBrigandineMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Steel Brigandine Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData GoldenBrigandineMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Golden Brigandine Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData GoldenChainMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Golden Chain Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData CrystalChainMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Crystal Chain Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData CrystalRingMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Crystal Ring Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData CrystalBandedMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Crystal Banded Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData CrystalSplintMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Crystal Splint Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData TungstenSplintMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Tungsten Splint Mail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData TungstenPlateMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Tungsten Platemail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData DiamondPlateMail0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Diamond Platemail";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData DiamondFieldPlate0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Diamond Field Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData DiamondFullPlate0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Diamond Full Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData BlackDiamondFullPlate0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Black Diamond Full Plate";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

ItemData RedDiamondPlate0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Red Diamond Plate";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = RedDiamondPlateImage;

	heading = "aArmor";
};

ItemData WhiteDiamondPlate0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "White Diamond Plate";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = WhiteDiamondPlateImage;

	heading = "aArmor";
};

ItemData JudgementRobe0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Judgement Robe";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = JudgementRobeImage;

	heading = "aArmor";
};

ItemData StormRobe0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Storm Robe";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = StormRobeImage;

	heading = "aArmor";
};

ItemData VoidRobe0
{
	showInventory = false;	// VOID 2026-07-15: parked above the 128-bit shopping bitfield (indexes 198-220); isItemShoppingOn() reads OOB garbage up there, phantom-rendering these in every merchant BUY pane. Nothing carries X0 counts post-migration, so hide them from all item GUIs.
	description = "Void Robe";
	className = "Equipped";
	shapeFile = "discammo";
	imageType = VoidRobeImage;

	heading = "aArmor";
};

