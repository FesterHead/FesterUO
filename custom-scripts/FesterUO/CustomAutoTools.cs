using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Gumps;
using Server.Network;
using Server.Commands;
using Server.ContextMenus;
using Server.Engines.Quests;

namespace Server.Custom
{
    // =========================================================================
    // GLOBAL HARVEST EVENT HOOK
    // Automatically intercepts harvest success when using custom tools
    // =========================================================================
    public static class CustomAutoToolsHandler
    {
        public static void Initialize()
        {
            EventSink.ResourceHarvestSuccess += OnResourceHarvestSuccess;
            CommandSystem.Register("FishFilter", AccessLevel.Player, OnFishFilterCommand);
            CommandSystem.Register("FilletFilter", AccessLevel.Player, OnFishFilterCommand);
        }

        private static void OnFishFilterCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null)
                return;

            FestersFishingPole pole = FestersFishingPole.FindPole(from);
            if (pole == null)
            {
                from.SendMessage(38, "You must have a Master Angler's Rod equipped or in your backpack to configure the fillet filter.");
                return;
            }

            from.CloseGump(typeof(FestersFishFilterGump));
            from.SendGump(new FestersFishFilterGump(from, pole));
        }

        private static void OnResourceHarvestSuccess(ResourceHarvestSuccessEventArgs e)
        {
            if (e.Harvester == null || e.Tool == null)
                return;

            if (e.Tool is FestersPickaxe)
            {
                FestersPickaxe.ProcessOre(e.Harvester);
            }
            else if (e.Tool is FestersHatchet)
            {
                FestersHatchet.ProcessLogs(e.Harvester);
            }
            else if (e.Tool is FestersFishingPole pole)
            {
                pole.ProcessFish(e.Harvester);
            }
        }
    }

    // =========================================================================
    // 1. FESTER'S PICKAXE (Auto-Smelt Ore -> Ingots to Satchel)
    // =========================================================================
    public class FestersPickaxe : Pickaxe
    {
        public override HarvestSystem HarvestSystem => Mining.System;

        [Constructable]
        public FestersPickaxe() : base()
        {
            Name = "Smelter's Pickaxe";
            Hue = 1161; // Burnished gold
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Auto-Smelt]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            ProcessOre(from);
            base.OnDoubleClick(from);
        }

        public static void ProcessOre(Mobile from)
        {
            if (from?.Backpack == null)
                return;

            // Find any newly mined ore in the backpack and convert directly to ingots
            List<Item> ores = new List<Item>(from.Backpack.FindItemsByType(typeof(BaseOre)));

            foreach (Item oreItem in ores)
            {
                if (oreItem is BaseOre ore)
                {
                    int ingotAmount = ore.Amount * 2; // Standard 1 ore -> 2 ingots ratio
                    CraftResource resourceType = ore.Resource;
                    ore.Delete();

                    Item ingots;

                    switch (resourceType)
                    {
                        case CraftResource.DullCopper: ingots = new DullCopperIngot(ingotAmount); break;
                        case CraftResource.ShadowIron:  ingots = new ShadowIronIngot(ingotAmount); break;
                        case CraftResource.Copper:      ingots = new CopperIngot(ingotAmount); break;
                        case CraftResource.Bronze:      ingots = new BronzeIngot(ingotAmount); break;
                        case CraftResource.Gold:        ingots = new GoldIngot(ingotAmount); break;
                        case CraftResource.Agapite:     ingots = new AgapiteIngot(ingotAmount); break;
                        case CraftResource.Verite:      ingots = new VeriteIngot(ingotAmount); break;
                        case CraftResource.Valorite:    ingots = new ValoriteIngot(ingotAmount); break;
                        default:                        ingots = new IronIngot(ingotAmount); break;
                    }

                    string oreType = CraftResources.GetName(resourceType);
                    int hue = CraftResources.GetHue(resourceType);
                    if (hue == 0)
                        hue = 68;

                    FestersResourceSatchel.Deposit(from, ingots);
                    from.SendMessage(hue, $"You smelt the vein into {ingotAmount} {oreType} ingots.");
                }
            }

            // Also tuck any mined granite straight into the satchel
            List<Item> granites = new List<Item>(from.Backpack.FindItemsByType(typeof(BaseGranite)));
            foreach (Item granite in granites)
            {
                FestersResourceSatchel.Deposit(from, granite);
            }
        }

        public FestersPickaxe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // 2. FESTER'S HATCHET (Auto-Saw Logs -> Boards to Satchel)
    // =========================================================================
    public class FestersHatchet : Hatchet
    {
        public override HarvestSystem HarvestSystem => Lumberjacking.System;

        [Constructable]
        public FestersHatchet() : base()
        {
            Name = "Forester's Hatchet";
            Hue = 1436; // Deep evergreen
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Auto-Saw]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            ProcessLogs(from);
            base.OnDoubleClick(from);
        }

        public static void ProcessLogs(Mobile from)
        {
            if (from?.Backpack == null)
                return;

            List<Item> logs = new List<Item>(from.Backpack.FindItemsByType(typeof(BaseLog)));

            foreach (Item logItem in logs)
            {
                if (logItem is BaseLog log)
                {
                    int boardAmount = log.Amount;
                    CraftResource resourceType = log.Resource;
                    log.Delete();

                    Item boards;

                    switch (resourceType)
                    {
                        case CraftResource.OakWood:      boards = new OakBoard(boardAmount); break;
                        case CraftResource.AshWood:      boards = new AshBoard(boardAmount); break;
                        case CraftResource.YewWood:      boards = new YewBoard(boardAmount); break;
                        case CraftResource.Heartwood:    boards = new HeartwoodBoard(boardAmount); break;
                        case CraftResource.Bloodwood:    boards = new BloodwoodBoard(boardAmount); break;
                        case CraftResource.Frostwood:    boards = new FrostwoodBoard(boardAmount); break;
                        default:                         boards = new Board(boardAmount); break;
                    }

                    string woodType = CraftResources.IsStandard(resourceType) ? "boards" : $"{CraftResources.GetName(resourceType)} boards";
                    int hue = CraftResources.GetHue(resourceType);
                    if (hue == 0)
                        hue = 68;

                    FestersResourceSatchel.Deposit(from, boards);
                    from.SendMessage(hue, $"You mill the timber into {boardAmount} {woodType}.");
                }
            }
        }

        public FestersHatchet(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // 3. FESTER'S FISHING POLE (Auto-Fillet Steaks & Trash Purge to Satchel)
    // =========================================================================
    public class FestersFishingPole : FishingPole
    {
        private bool m_AutoProtectQuestFish = true;
        private HashSet<string> m_ProtectedFish = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoProtectQuestFish
        {
            get => m_AutoProtectQuestFish;
            set => m_AutoProtectQuestFish = value;
        }

        public HashSet<string> ProtectedFish
        {
            get => m_ProtectedFish;
            set => m_ProtectedFish = value ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        [Constructable]
        public FestersFishingPole() : base()
        {
            Name = "Master Angler's Rod";
            Hue = 1153; // Ocean cerulean
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            int count = m_ProtectedFish != null ? m_ProtectedFish.Count : 0;
            if (m_AutoProtectQuestFish && count > 0)
                LabelTo(from, $"[Auto-Fillet: Quest Protected + {count} Exempt]", 68);
            else if (m_AutoProtectQuestFish)
                LabelTo(from, "[Auto-Fillet: Quest Protected]", 68);
            else if (count > 0)
                LabelTo(from, $"[Auto-Fillet: {count} Exempt]", 68);
            else
                LabelTo(from, "[Indestructible & Auto-Fillet]", 68);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && (IsChildOf(from.Backpack) || Parent == from))
            {
                list.Add(new ConfigureFishFilterEntry(this, from));
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            ProcessFish(from, this);
            base.OnDoubleClick(from);
        }

        public void ProcessFish(Mobile from)
        {
            ProcessFish(from, this);
        }

        public static void ProcessFish(Mobile from, FestersFishingPole pole)
        {
            if (from?.Backpack == null)
                return;

            List<Item> toFillet = new List<Item>();
            int preservedCount = 0;

            foreach (Item item in from.Backpack.Items)
            {
                if (item is Fish || item is BaseFish || item is BigFish ||
                    (item is BaseHighseasFish && !(item is RareFish) && !(item is BaseCrabAndLobster)))
                {
                    if (pole != null && pole.IsProtected(from, item))
                    {
                        preservedCount += item.Amount;
                        continue;
                    }

                    toFillet.Add(item);
                }
            }

            foreach (Item found in toFillet)
            {
                int steaksCount = found.Amount * 4;
                found.Delete();

                RawFishSteak steaks = new RawFishSteak(steaksCount);
                FestersResourceSatchel.Deposit(from, steaks);
                from.SendMessage(68, $"You reel in a catch and stow {steaksCount} clean raw fish steaks.");
            }

            if (preservedCount > 0)
            {
                from.SendMessage(53, $"Preserved {preservedCount} whole fish according to your fillet filter.");
            }

            // Clean up soggy shoes or junk boots caught on the line
            List<Item> shoes = new List<Item>();
            foreach (Item item in from.Backpack.Items)
            {
                if (item is BaseShoes shoe && shoe.Hue == 0 && shoe.LootType != LootType.Blessed)
                {
                    shoes.Add(shoe);
                }
            }
            foreach (Item shoe in shoes)
            {
                shoe.Delete();
                from.SendMessage(38, "You toss waterlogged junk back into the depths.");
            }
        }

        public bool IsProtected(Mobile from, Item item)
        {
            if (item == null)
                return false;

            Type t = item.GetType();

            if (m_AutoProtectQuestFish && IsQuestFish(from, t))
                return true;

            if (m_ProtectedFish != null && (m_ProtectedFish.Contains(t.Name) || m_ProtectedFish.Contains(t.FullName)))
                return true;

            return false;
        }

        public static bool IsQuestFish(Mobile from, Type fishType)
        {
            if (fishType == null)
                return false;

            if (from is PlayerMobile pm && pm.Quests != null)
            {
                for (int i = 0; i < pm.Quests.Count; i++)
                {
                    if (pm.Quests[i] is ProfessionalFisherQuest quest && quest.Objectives != null && quest.Objectives.Count > 0)
                    {
                        if (quest.Objectives[0] is FishQuestObjective obj && obj.Line != null)
                        {
                            if (obj.Line.TryGetValue(fishType, out int[] counts))
                            {
                                if (counts != null && counts.Length >= 2 && counts[0] < counts[1])
                                    return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static FestersFishingPole FindPole(Mobile from)
        {
            if (from == null)
                return null;

            if (from.FindItemOnLayer(Layer.OneHanded) is FestersFishingPole equipped1)
                return equipped1;

            if (from.FindItemOnLayer(Layer.TwoHanded) is FestersFishingPole equipped2)
                return equipped2;

            if (from.Backpack != null)
                return from.Backpack.FindItemByType<FestersFishingPole>();

            return null;
        }

        public FestersFishingPole(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version

            writer.Write(m_AutoProtectQuestFish);
            writer.Write(m_ProtectedFish != null ? m_ProtectedFish.Count : 0);
            if (m_ProtectedFish != null)
            {
                foreach (string fish in m_ProtectedFish)
                {
                    writer.Write(fish);
                }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            m_ProtectedFish = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            m_AutoProtectQuestFish = true;

            if (reader.End())
                return; // Legacy version 0

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                {
                    m_AutoProtectQuestFish = reader.ReadBool();
                    int count = reader.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        string s = reader.ReadString();
                        if (!string.IsNullOrEmpty(s))
                            m_ProtectedFish.Add(s);
                    }
                    break;
                }
            }
        }
    }

    // =========================================================================
    // 4. FESTER'S SKINNING KNIFE (Auto Cut-Leather & Auto-Shear to Satchel)
    // =========================================================================
    public class FestersSkinningKnife : SkinningKnife
    {
        [Constructable]
        public FestersSkinningKnife() : base()
        {
            Name = "Tanner's Skinning Knife";
            Hue = 1170; // Saddle leather brown
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Auto-Dressing]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage(68, "Target an animal corpse to skin or a living sheep to shear:");
            from.Target = new InternalTannerTarget(this);
        }

        public static void ProcessCorpseHarvest(Mobile from, Corpse corpse)
        {
            if (from == null || corpse == null)
                return;

            List<Item> harvestables = new List<Item>();
            foreach (Item subItem in corpse.Items)
            {
                if (subItem is BaseHides || subItem is BaseScales || subItem is Feather || subItem is Wool || subItem is CookableFood)
                {
                    harvestables.Add(subItem);
                }
            }

            int meatsHarvested = 0;
            int feathersHarvested = 0;
            int scalesHarvested = 0;

            foreach (Item item in harvestables)
            {
                if (item is BaseHides hide)
                {
                    int amount = hide.Amount;
                    CraftResource res = hide.Resource;
                    hide.Delete();

                    Item leather;
                    switch (res)
                    {
                        case CraftResource.SpinedLeather: leather = new SpinedLeather(amount); break;
                        case CraftResource.HornedLeather: leather = new HornedLeather(amount); break;
                        case CraftResource.BarbedLeather: leather = new BarbedLeather(amount); break;
                        default:                          leather = new Leather(amount); break;
                    }

                    FestersResourceSatchel.Deposit(from, leather);
                    from.SendMessage(68, $"You skin the beast and stow {amount} cut leather into your satchel.");
                }
                else if (item is BaseScales)
                {
                    scalesHarvested += item.Amount;
                    FestersResourceSatchel.Deposit(from, item);
                }
                else if (item is Feather)
                {
                    feathersHarvested += item.Amount;
                    FestersResourceSatchel.Deposit(from, item);
                }
                else if (item is Wool)
                {
                    FestersResourceSatchel.Deposit(from, item);
                    from.SendMessage(68, "You harvest fleece and stow it into your satchel.");
                }
                else if (item is CookableFood)
                {
                    meatsHarvested += item.Amount;
                    FestersResourceSatchel.Deposit(from, item);
                }
            }

            if (feathersHarvested > 0)
                from.SendMessage(68, $"You pluck {feathersHarvested} feathers and stow them into your satchel.");

            if (scalesHarvested > 0)
                from.SendMessage(68, $"You harvest {scalesHarvested} dragon scales and stow them into your satchel.");

            if (meatsHarvested > 0)
                from.SendMessage(68, $"You butcher {meatsHarvested} fresh meat and stow it into your satchel.");
        }

        private class InternalTannerTarget : Target
        {
            private readonly FestersSkinningKnife m_Knife;

            public InternalTannerTarget(FestersSkinningKnife knife) : base(3, false, TargetFlags.None)
            {
                m_Knife = knife;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Corpse corpse)
                {
                    if (!corpse.Carved)
                    {
                        corpse.Carve(from, m_Knife);
                        ProcessCorpseHarvest(from, corpse);
                    }
                    else
                    {
                        from.SendMessage(38, "That corpse has already been carved.");
                    }
                }
                else if (targeted is Sheep sheep)
                {
                    if (sheep.Alive && !sheep.Controlled)
                    {
                        sheep.PlaySound(0xD6);
                        sheep.BodyValue = 0xDF; // Sheared graphic

                        BoltOfCloth cloth = new BoltOfCloth(2);
                        FestersResourceSatchel.Deposit(from, cloth);
                        from.SendMessage(68, "You shear the fleece and pack 2 bolts of cloth directly into your satchel.");
                    }
                }
                else
                {
                    from.SendMessage(38, "You can only use this on animal corpses or sheep.");
                }
            }
        }

        public FestersSkinningKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // 5. FESTER'S SCYTHE (Radius Crop Sweeper & Auto-Loom to Satchel)
    // =========================================================================
    public class FestersScythe : Scythe
    {
        [Constructable]
        public FestersScythe() : base()
        {
            Name = "Harvester's Scythe";
            Hue = 1365; // Amber grain
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Field Sweeper]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            Map map = from.Map;
            if (map == null) return;

            // 3-tile area sweep around player
            IPooledEnumerable eable = map.GetItemsInRange(from.Location, 3);
            int clothHarvested = 0;
            int threadHarvested = 0;

            foreach (Item item in eable)
            {
                // Cotton plants -> Bolts of Cloth
                if (item.ItemID >= 0x0C51 && item.ItemID <= 0x0C54)
                {
                    item.Delete();
                    BoltOfCloth bolt = new BoltOfCloth(1);
                    FestersResourceSatchel.Deposit(from, bolt);
                    clothHarvested++;
                }
                // Flax plants -> Spools of Thread
                else if (item.ItemID >= 0x1A99 && item.ItemID <= 0x1A9B)
                {
                    item.Delete();
                    SpoolOfThread thread = new SpoolOfThread(3);
                    FestersResourceSatchel.Deposit(from, thread);
                    threadHarvested++;
                }
            }
            eable.Free();

            if (clothHarvested > 0 || threadHarvested > 0)
            {
                from.PlaySound(0x248);
                from.SendMessage(68, $"You scythe the field: {clothHarvested} cloth bolts and {threadHarvested} thread bundles stowed.");
            }
            else
            {
                from.SendMessage(38, "No mature cotton or flax plants found within 3 tiles.");
            }
        }

        public FestersScythe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // CONTEXT MENU: CONFIGURE FISH FILTER
    // =========================================================================
    public class ConfigureFishFilterEntry : ContextMenuEntry
    {
        private readonly FestersFishingPole m_Pole;
        private readonly Mobile m_From;

        public ConfigureFishFilterEntry(FestersFishingPole pole, Mobile from) : base(6103)
        {
            m_Pole = pole;
            m_From = from;
        }

        public override void OnClick()
        {
            if (m_From == null || m_Pole == null || m_Pole.Deleted)
                return;

            m_From.CloseGump(typeof(FestersFishFilterGump));
            m_From.SendGump(new FestersFishFilterGump(m_From, m_Pole));
        }
    }

    // =========================================================================
    // FISH FILLET FILTER GUMP
    // Interactive gump for exempting specific fish and quest fish from auto-fillet
    // =========================================================================
    public class FestersFishFilterGump : Gump
    {
        public enum ActionButton
        {
            Cancel = 0,
            Save = 1,
            TabDeepWater = 10,
            TabShore = 11,
            TabDungeon = 12,
            TabCommon = 13,
            SelectAllTab = 20,
            DeselectAllTab = 21,
            ProtectCurrentQuest = 22,
            ClearAll = 23
        }

        private static readonly Type[] DeepWaterFishList = BaseHighseasFish.DeepWaterFish;
        private static readonly Type[] ShoreFishList = BaseHighseasFish.ShoreFish;
        private static readonly Type[] DungeonFishList = BaseHighseasFish.DungeonFish;
        private static readonly Type[] CommonFishList = new Type[] { typeof(Fish), typeof(BigFish) };

        private readonly Mobile m_From;
        private readonly FestersFishingPole m_Pole;
        private readonly int m_Tab;
        private readonly bool m_AutoProtect;
        private readonly HashSet<string> m_Selected;

        public FestersFishFilterGump(Mobile from, FestersFishingPole pole)
            : this(from, pole, 0, pole?.AutoProtectQuestFish ?? true, pole?.ProtectedFish != null ? new HashSet<string>(pole.ProtectedFish, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public FestersFishFilterGump(Mobile from, FestersFishingPole pole, int tab, bool autoProtect, HashSet<string> selected)
            : base(100, 80)
        {
            m_From = from;
            m_Pole = pole;
            m_Tab = Math.Max(0, Math.Min(tab, 3));
            m_AutoProtect = autoProtect;
            m_Selected = selected ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Closable = true;
            Draggable = true;
            Resizable = false;

            BuildGump();
        }

        private void BuildGump()
        {
            AddPage(0);

            // Outer Frame (Stone / Parchment)
            AddBackground(0, 0, 560, 510, 9270);
            AddImageTiled(11, 10, 538, 490, 2624);
            AddAlphaRegion(11, 10, 538, 490);

            // Title Header
            AddHtml(20, 15, 520, 22, "<center><basefont color=#E6C229 size=5><b>Master Angler's Fillet Filter</b></basefont></center>", false, false);
            AddHtml(20, 38, 520, 18, "<center><basefont color=#BBBBBB>Select fish species to preserve whole in your backpack (exempt from auto-filleting).</basefont></center>", false, false);

            // Active Fishmonger Quest Summary Box
            AddImageTiled(20, 58, 520, 36, 9274);
            AddAlphaRegion(20, 58, 520, 36);

            Dictionary<Type, int[]> questTargets = GetActiveQuestTargets(m_From);
            if (questTargets.Count > 0)
            {
                List<string> entries = new List<string>();
                foreach (KeyValuePair<Type, int[]> kvp in questTargets)
                {
                    string name = FormatFishName(kvp.Key);
                    entries.Add($"{name} ({kvp.Value[0]}/{kvp.Value[1]})");
                }
                string questSummary = string.Join(", ", entries);
                AddHtml(25, 62, 510, 28, $"<basefont color=#66FF66><b>Active Fishmonger Order:</b></basefont> <basefont color=#FFFFCC>{questSummary}</basefont>", false, false);
            }
            else
            {
                AddHtml(25, 67, 510, 20, "<basefont color=#888888><i>No active Fishmonger quest found in quest log.</i></basefont>", false, false);
            }

            // Auto-Protect Quest Fish Checkbox
            AddCheck(22, 100, 0xD2, 0xD3, m_AutoProtect, 1);
            AddLabel(46, 98, 1152, "Auto-Protect Active Fishmonger Quest Targets (Always Keep Whole)");

            // Tab Navigation Bar
            string[] tabLabels = new string[] { $"Deep Water ({DeepWaterFishList.Length})", $"Shore ({ShoreFishList.Length})", $"Dungeon ({DungeonFishList.Length})", $"Common ({CommonFishList.Length})" };
            int[] tabX = new int[] { 22, 160, 285, 420 };

            for (int t = 0; t < tabLabels.Length; t++)
            {
                int x = tabX[t];
                int y = 125;
                if (m_Tab == t)
                {
                    AddImage(x, y, 4006); // pressed state
                    AddLabel(x + 35, y, 68, tabLabels[t]); // cyan highlight
                }
                else
                {
                    AddButton(x, y, 4005, 4007, (int)ActionButton.TabDeepWater + t, GumpButtonType.Reply, 0);
                    AddLabel(x + 35, y, 1152, tabLabels[t]);
                }
            }

            // Fish Checkbox Grid (2 Columns, up to 9 rows)
            Type[] currentList = GetCurrentFishList(m_Tab);
            int startY = 155;
            for (int i = 0; i < currentList.Length; i++)
            {
                Type fishType = currentList[i];
                int col = i % 2;
                int row = i / 2;
                int itemX = col == 0 ? 30 : 290;
                int itemY = startY + (row * 24);
                int switchID = 100 + i;

                bool isChecked = m_Selected.Contains(fishType.Name);
                AddCheck(itemX, itemY, 0xD2, 0xD3, isChecked, switchID);

                bool isQuest = questTargets.ContainsKey(fishType);
                string displayName = FormatFishName(fishType);
                int hue;

                if (isQuest)
                {
                    int[] counts = questTargets[fishType];
                    displayName += $" [Quest {counts[0]}/{counts[1]}]";
                    hue = 53; // Gold
                }
                else if (isChecked)
                {
                    hue = 68; // Cyan
                }
                else
                {
                    hue = 1152; // White
                }

                AddLabel(itemX + 26, itemY, hue, displayName);
            }

            // Quick Action Buttons
            AddButton(25, 385, 4005, 4007, (int)ActionButton.SelectAllTab, GumpButtonType.Reply, 0);
            AddLabel(60, 385, 1152, "Check Tab");

            AddButton(155, 385, 4005, 4007, (int)ActionButton.DeselectAllTab, GumpButtonType.Reply, 0);
            AddLabel(190, 385, 1152, "Uncheck Tab");

            AddButton(290, 385, 4005, 4007, (int)ActionButton.ProtectCurrentQuest, GumpButtonType.Reply, 0);
            AddLabel(325, 385, 53, "Protect Active Quest");

            AddButton(460, 385, 4005, 4007, (int)ActionButton.ClearAll, GumpButtonType.Reply, 0);
            AddLabel(495, 385, 38, "Clear All");

            // Separator Line
            AddImageTiled(20, 418, 520, 2, 9274);

            // Summary Status
            AddHtml(25, 426, 510, 20, $"<basefont color=#AAAAAA>Currently preserving <b><basefont color=#66FF66>{m_Selected.Count}</basefont></b> manual exemptions. Checked fish will not be filleted.</basefont>", false, false);

            // Bottom Action Buttons
            AddButton(150, 455, 4005, 4007, (int)ActionButton.Save, GumpButtonType.Reply, 0);
            AddLabel(185, 455, 68, "Save & Apply");

            AddButton(330, 455, 4005, 4007, (int)ActionButton.Cancel, GumpButtonType.Reply, 0);
            AddLabel(365, 455, 38, "Cancel");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_From == null || m_Pole == null || m_Pole.Deleted)
                return;

            int buttonID = info.ButtonID;
            if (buttonID == (int)ActionButton.Cancel)
                return;

            // Harvest the current page state
            bool autoProtect = info.IsSwitched(1);
            Type[] currentList = GetCurrentFishList(m_Tab);
            for (int i = 0; i < currentList.Length; i++)
            {
                Type type = currentList[i];
                if (info.IsSwitched(100 + i))
                {
                    m_Selected.Add(type.Name);
                }
                else
                {
                    m_Selected.Remove(type.Name);
                }
            }

            switch ((ActionButton)buttonID)
            {
                case ActionButton.Save:
                {
                    m_Pole.AutoProtectQuestFish = autoProtect;
                    m_Pole.ProtectedFish = new HashSet<string>(m_Selected, StringComparer.OrdinalIgnoreCase);
                    m_From.SendMessage(68, $"Fillet filter saved: {m_Selected.Count} fish species exempted from auto-filleting. Auto-protect quest targets: {(autoProtect ? "ENABLED" : "DISABLED")}.");
                    break;
                }
                case ActionButton.TabDeepWater:
                case ActionButton.TabShore:
                case ActionButton.TabDungeon:
                case ActionButton.TabCommon:
                {
                    int newTab = buttonID - (int)ActionButton.TabDeepWater;
                    m_From.SendGump(new FestersFishFilterGump(m_From, m_Pole, newTab, autoProtect, m_Selected));
                    break;
                }
                case ActionButton.SelectAllTab:
                {
                    foreach (Type t in currentList)
                    {
                        m_Selected.Add(t.Name);
                    }
                    m_From.SendGump(new FestersFishFilterGump(m_From, m_Pole, m_Tab, autoProtect, m_Selected));
                    break;
                }
                case ActionButton.DeselectAllTab:
                {
                    foreach (Type t in currentList)
                    {
                        m_Selected.Remove(t.Name);
                    }
                    m_From.SendGump(new FestersFishFilterGump(m_From, m_Pole, m_Tab, autoProtect, m_Selected));
                    break;
                }
                case ActionButton.ProtectCurrentQuest:
                {
                    Dictionary<Type, int[]> targets = GetActiveQuestTargets(m_From);
                    int added = 0;
                    foreach (Type t in targets.Keys)
                    {
                        if (m_Selected.Add(t.Name))
                            added++;
                    }
                    m_From.SendMessage(53, added > 0 ? $"Added {added} active quest fish to your exemption filter." : "Active quest fish are already in your filter.");
                    m_From.SendGump(new FestersFishFilterGump(m_From, m_Pole, m_Tab, autoProtect, m_Selected));
                    break;
                }
                case ActionButton.ClearAll:
                {
                    m_Selected.Clear();
                    m_From.SendMessage(38, "Cleared all manual fish exemptions.");
                    m_From.SendGump(new FestersFishFilterGump(m_From, m_Pole, m_Tab, autoProtect, m_Selected));
                    break;
                }
            }
        }

        private static Type[] GetCurrentFishList(int tab)
        {
            switch (tab)
            {
                case 0: return DeepWaterFishList;
                case 1: return ShoreFishList;
                case 2: return DungeonFishList;
                case 3: return CommonFishList;
                default: return DeepWaterFishList;
            }
        }

        public static Dictionary<Type, int[]> GetActiveQuestTargets(Mobile from)
        {
            Dictionary<Type, int[]> dict = new Dictionary<Type, int[]>();

            if (from is PlayerMobile pm && pm.Quests != null)
            {
                for (int i = 0; i < pm.Quests.Count; i++)
                {
                    if (pm.Quests[i] is ProfessionalFisherQuest quest && quest.Objectives != null && quest.Objectives.Count > 0)
                    {
                        if (quest.Objectives[0] is FishQuestObjective obj && obj.Line != null)
                        {
                            foreach (KeyValuePair<Type, int[]> kvp in obj.Line)
                            {
                                if (!dict.ContainsKey(kvp.Key))
                                    dict[kvp.Key] = new int[] { kvp.Value[0], kvp.Value[1] };
                            }
                        }
                    }
                }
            }

            return dict;
        }

        public static string FormatFishName(Type type)
        {
            if (type == null)
                return "Unknown";

            if (type == typeof(Fish))
                return "Ordinary Fish";

            if (type == typeof(BigFish))
                return "Big Fish";

            string name = type.Name;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && !char.IsUpper(name[i + 1]))))
                {
                    sb.Append(' ');
                }
                sb.Append(name[i]);
            }
            return sb.ToString();
        }
    }
}
