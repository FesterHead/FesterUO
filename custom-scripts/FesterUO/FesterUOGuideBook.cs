using System;
using Server;
using Server.Commands;
using Server.Items;

namespace Server.Custom
{
    public class FesterUOGuideBook : BrownBook
    {
        public static readonly BookContent Content = new BookContent(
            "Adventurer's Guide", "Britannia Council",
            new BookPageInfo(
                "  -- FESTER UO --",
                " Adventurer's Guide",
                "===================",
                "Welcome to Britannia!",
                "This tome details your",
                "satchel, tools, housing,",
                "sanctuary, bestiary,",
                "loot filter, & commands."),
            new BookPageInfo(
                "RESOURCE SATCHEL",
                "-------------------",
                "Your blessed satchel",
                "automatically gathers",
                "all resources harvested",
                "by your special tools:",
                "ingots, boards, leather,",
                "cloth, wool, and fish."),
            new BookPageInfo(
                "SATCHEL DETAILS",
                "-------------------",
                "Holds 1,000 items with",
                "100% weight reduction.",
                "",
                "Accepts raw resources,",
                "maps, scrolls, & runes.",
                "",
                "Keep it in your pack!"),
            new BookPageInfo(
                "WITHOUT SATCHEL",
                "-------------------",
                "If your satchel is not",
                "in your backpack, all",
                "harvested resources go",
                "straight to your pack,",
                "where standard weight",
                "& item limits apply!"),
            new BookPageInfo(
                "ENCHANTED TOOLS",
                "-------------------",
                "All starter tools are",
                "blessed & unbreakable.",
                "",
                "* Pickaxe: Auto-smelts",
                "  mined ore into ingots.",
                "",
                "* Hatchet: Auto-saws",
                "  cut logs into boards."),
            new BookPageInfo(
                "* Skinning Knife:",
                "  Carves corpses into",
                "  cut leather, shears",
                "  sheep for wool, and",
                "  fillets raw fish.",
                "",
                "* Fishing Pole: Cleans",
                "  catches into steaks."),
            new BookPageInfo(
                "* Scythe: Reaps all",
                "  crops in a 3-tile",
                "  radius (wheat, cotton,",
                "  flax, and vegetables)."),
            new BookPageInfo(
                "SANCTUARY TALISMAN",
                "-------------------",
                "Every new character",
                "starts with a blessed",
                "Talisman of Sanctuary",
                "in their backpack.",
                "",
                "Carried or equipped,",
                "wild beasts will not",
                "attack you or pets!"),
            new BookPageInfo(
                "SANCTUARY DETAILS",
                "-------------------",
                "* Wilds (All Facets):",
                "  Hostile creatures",
                "  peacefully ignore you.",
                "",
                "* Dungeons & Bosses:",
                "  Protection ceases;",
                "  dungeon mobs attack!",
                "",
                "* Double-click talisman",
                "  to toggle On or Off."),
            new BookPageInfo(
                "HOUSING",
                "-------------------",
                "Player housing may be",
                "constructed anywhere",
                "land is open and valid",
                "across Britannia.",
                "",
                "Housing is allowed",
                "on Trammel, Felucca,",
                "Malas, Tokuno, & TerMur."),
            new BookPageInfo(
                "STARTER COTTAGE",
                "-------------------",
                "Your pack contains a",
                "Cottage Deed Voucher.",
                "",
                "Double-click it to pick",
                "from 6 classic 7x7",
                "styles whenever you are",
                "ready to build a home!"),
            new BookPageInfo(
                "HOME FIXTURES",
                "-------------------",
                "Your first character",
                "also receives two home",
                "fixture addon deeds:",
                "",
                "* Ankh of Sacrifice Deed",
                "* House Moongate Deed",
                "",
                "Place them in your home!"),
            new BookPageInfo(
                "ANKH OF SACRIFICE",
                "-------------------",
                "Double-click deed in",
                "your house to place.",
                "Choose East or South.",
                "",
                "* Automatically revives",
                "  ghosts within 1 tile.",
                "* Tithes & karma prayer.",
                "* Chop with axe to",
                "  re-deed anytime!"),
            new BookPageInfo(
                "HOUSE MOONGATE",
                "-------------------",
                "Double-click deed in",
                "your house to place.",
                "",
                "* Step on or double-",
                "  click to open the",
                "  Public Moongate menu.",
                "* Travel instantly to",
                "  any city or facet",
                "  right from home!"),
            new BookPageInfo(
                "RE-DEEDING GATES",
                "-------------------",
                "Unlike standard UO,",
                "your House Moongate",
                "can be easily re-deeded!",
                "",
                "Double-click an axe",
                "(e.g. your hatchet)",
                "and target the gate.",
                "",
                "It safely returns to a",
                "deed in your pack!"),
            new BookPageInfo(
                "MAGIC & RUNES",
                "-------------------",
                "Your starter kit has:",
                "* Blessed Spellbook with",
                "  all 64 magery spells.",
                "* Blessed Runebook with",
                "  20 recall charges.",
                "* 16 blank recall runes",
                "  inside your satchel!"),
            new BookPageInfo(
                "UNIVERSAL DYE TUB",
                "-------------------",
                "Every character starts",
                "with a blessed Universal",
                "Highlander Dye Tub!",
                "",
                "* Dyes clothes, armor,",
                "  weapons, containers,",
                "  books, mounts, & pets!"),
            new BookPageInfo(
                "DYE TUB DETAILS",
                "-------------------",
                "* Double-click tub to",
                "  dye equipment or pets",
                "  (1,000 gold per use).",
                "",
                "* Right-click -> Set Hue",
                "  (or use standard Dyes)",
                "  to open the 16-color",
                "  gradient palette!"),
            new BookPageInfo(
                "HUNTER'S BESTIARY",
                "-------------------",
                "Type [Bestiary to open",
                "your hunting grimoire.",
                "",
                "Slay mobs to discover",
                "treatises that grant a",
                "permanent +20% damage"),
            new BookPageInfo(
                "BESTIARY MASTERY",
                "-------------------",
                "bonus vs that monster!",
                "",
                "* 193 creature types.",
                "* Controlled pets also",
                "  gain your bonuses!",
                "* Dynamic tooltips show",
                "  if already learned."),
            new BookPageInfo(
                "PLAYER COMMANDS",
                "-------------------",
                "[Bestiary",
                "  Opens your Bestiary.",
                "",
                "[rbank",
                "  Opens remote bank.",
                "",
                "[c <message>",
                "  Global chat broadcast."),
            new BookPageInfo(
                "PLAYER COMMANDS 2",
                "-------------------",
                "[say",
                "  House & boat gump.",
                "",
                "[GetSanctuary",
                "  Claims your talisman.",
                "",
                "[LootFilter",
                "  ARPG-style ground &",
                "  corpse loot filter."),
            new BookPageInfo(
                "PLAYER COMMANDS 3",
                "-------------------",
                "[ApexHunt",
                "  PvM hunt standing.",
                "",
                "[SlowGrind",
                "  View 100+ skill pace.",
                "",
                "[ApexHuntToggle",
                "  Toggles hunt HUD.",
                "",
                "[ApexHuntTop",
                "  View hunt rankings."),
            new BookPageInfo(
                "PLAYER COMMANDS 4",
                "-------------------",
                "[guide",
                "  Opens or receives the",
                "  latest Adventurer's",
                "  Guide edition.",
                "",
                "[guidebook",
                "  Alias for [guide.",
                "",
                "[getguide",
                "  Alias for [guide."),
            new BookPageInfo(
                "LEGENDARY MASTER",
                "-------------------",
                "Seek the Master of",
                "Skills for combat trials",
                "to earn +5 PowerScrolls",
                "up to 120 (Legendary)!",
                "",
                "* Requires 100.0 (GM)",
                "  at current skill cap.",
                "* Slay assigned dungeon",
                "  bosses before time",
                "  expires to win scroll."),
            new BookPageInfo(
                "MASTER COMMANDS",
                "-------------------",
                "Stand near the Master",
                "(within 5 tiles) and",
                "speak aloud or",
                "double-click him:",
                "",
                "give task",
                "  Starts hunting trial.",
                "  Upon completion, choose",
                "  your skill via gump!",
                "",
                "remove task",
                "  Abandons active task."),
            new BookPageInfo(
                "STABLES & REPAIRS",
                "-------------------",
                "* Stables: All players",
                "  enjoy 12 base stalls",
                "  at any Animal Trainer.",
                "",
                "* Powder of Fortifying:",
                "  Sold by smiths and",
                "  tinkers to protect",
                "  gear max durability."),
            new BookPageInfo(
                "SEA & ESTATES",
                "-------------------",
                "Housing allowance is",
                "granted one per account",
                "(shared by characters).",
                "",
                "A Blessed Small Boat",
                "Deed is also included",
                "for your sea voyages.")
        );

        [Constructable]
        public FesterUOGuideBook() : base(false)
        {
            Name = "Adventurer's Guide";
            Hue = 0x482; // Antique gilded hue
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public static new void Initialize()
        {
            CommandSystem.Register("Guide", AccessLevel.Player, Guide_OnCommand);
            CommandSystem.Register("GuideBook", AccessLevel.Player, Guide_OnCommand);
            CommandSystem.Register("GetGuide", AccessLevel.Player, Guide_OnCommand);
        }

        [Usage("Guide")]
        [Aliases("GuideBook", "GetGuide")]
        [Description("Opens your Adventurer's Guide or provides a fresh copy updated to the latest edition.")]
        private static void Guide_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null)
                return;

            if (from.Backpack == null)
            {
                from.SendMessage(38, "You must have a backpack to receive a guide book.");
                return;
            }

            FesterUOGuideBook existing = from.Backpack.FindItemByType<FesterUOGuideBook>();

            if (existing != null)
            {
                existing.Delete();
                from.SendMessage(68, "Your Adventurer's Guide has been updated to the latest edition.");
            }
            else
            {
                from.SendMessage(68, "A blessed Adventurer's Guide has been placed in your backpack.");
            }

            FesterUOGuideBook guide = new FesterUOGuideBook();
            from.Backpack.DropItem(guide);
            from.PlaySound(0x249);
            guide.OnDoubleClick(from);
        }

        public FesterUOGuideBook(Serial serial) : base(serial)
        {
        }

        public override BookContent DefaultContent => Content;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadEncodedInt();
        }
    }
}
