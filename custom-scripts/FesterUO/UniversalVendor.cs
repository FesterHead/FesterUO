// ============================================================================
// UniversalVendor.cs - Universal Vendor NPC Mobile
//
// Origin: FesterUO Custom Scripts (servuo/custom-scripts/FesterUO/)
// Purpose: A master vendor NPC placed and frozen by GMs providing one-stop access
//          to all standard NPC vendor wares and purchasing capabilities.
//
// Features:
// - Placed via [add UniversalVendor; defaults to frozen, invulnerable, and blessed.
// - Selecting "Buy" (via click, speech, or double-click) displays an interactive
//   gump to select from all valid vendor professions across 4 categories.
// - Selecting a vendor profession opens the native UO Buy window with items and
//   stocking matching individual vendors.
// - Restocking and stock economics follow Config/ServUO/Vendors.cfg directly via
//   BaseVendor (RestockDelay, EconomyStockAmount, ReagentStockAmount, RestockDecay).
// - Selecting "Sell" opens the native UO Sell window allowing players to sell
//   any items in their backpack that any vendor in the realm normally purchases.
// ============================================================================

using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.FesterUO
{
    public class UniversalVendor : BaseVendor
    {
        private const int InteractionRange = 4;

        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        private List<VendorTypeDefinition> m_VendorTypes;

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public List<VendorTypeDefinition> VendorTypes => m_VendorTypes;

        public override bool CanTeach => false;
        public override bool ChangeRace => false;
        public override bool IsInvulnerable => true;

        [Constructable]
        public UniversalVendor() : base("the Universal Vendor")
        {
            Name = "Universal Merchant";
            Title = "the Universal Vendor";

            Frozen = true;
            CantWalk = true;
            Blessed = true;
            Direction = Direction.South;

            SpeechHue = 53;
        }

        public UniversalVendor(Serial serial) : base(serial)
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Clear();
            m_VendorTypes = BuildVendorTypes();

            foreach (var vt in m_VendorTypes)
            {
                foreach (var sb in vt.SBInfos)
                {
                    m_SBInfos.Add(sb);
                }
            }
        }

        public override void InitOutfit()
        {
            SetWearable(new FancyShirt(0x486));
            SetWearable(new LongPants(0x1BB));
            SetWearable(new ThighBoots(0x486));
            SetWearable(new Cloak(0x486));
            SetWearable(new BodySash(0x8A5));

            int hairHue = GetHairHue();
            Utility.AssignRandomHair(this, hairHue);
            if (!Female)
            {
                Utility.AssignRandomFacialHair(this, hairHue);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(Location, InteractionRange))
            {
                VendorBuy(from);
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override void VendorBuy(Mobile from)
        {
            if (!IsActiveSeller || !from.CheckAlive() || !CheckVendorAccess(from))
                return;

            if (!from.InRange(Location, InteractionRange))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            from.CloseGump(typeof(UniversalVendorTypeGump));
            from.SendGump(new UniversalVendorTypeGump(from, this));
        }

        public override void VendorSell(Mobile from)
        {
            if (!from.InRange(Location, InteractionRange))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            base.VendorSell(from);
        }

        public void OpenCategoryBuy(Mobile from, VendorTypeDefinition vendorType)
        {
            if (!IsActiveSeller || !from.CheckAlive() || !CheckVendorAccess(from))
                return;

            if (!from.InRange(Location, InteractionRange))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (DateTime.UtcNow - LastRestock > RestockDelay)
            {
                Restock();
            }

            UpdateBuyInfo();

            var list = new List<BuyItemState>();
            Container cont = BuyPack;
            List<ObjectPropertyList> opls = null;

            foreach (var sb in vendorType.SBInfos)
            {
                var buyList = sb.BuyInfo;
                if (buyList == null)
                    continue;

                for (int i = 0; i < buyList.Count && list.Count < 250; i++)
                {
                    GenericBuyInfo gbi = buyList[i];
                    if (gbi.Amount <= 0)
                        continue;

                    if (gbi.Type == typeof(BlankScroll) && gbi.Amount < BaseVendor.EconomyStockAmount)
                    {
                        gbi.Amount = gbi.MaxAmount = BaseVendor.EconomyStockAmount;
                    }

                    IEntity disp = gbi.GetDisplayEntity();

                    list.Add(new BuyItemState(
                        gbi.Name,
                        cont.Serial,
                        disp == null ? (Serial)0x7FC0FFEE : disp.Serial,
                        gbi.Price,
                        gbi.Amount,
                        gbi.ItemID,
                        gbi.Hue));

                    if (opls == null)
                        opls = new List<ObjectPropertyList>();

                    if (disp is Item it)
                        opls.Add(it.PropertyList);
                    else if (disp is Mobile m)
                        opls.Add(m.PropertyList);
                }
            }

            if (list.Count > 0)
            {
                list.Sort(new BuyItemStateComparer());
                SendPacksTo(from);

                NetState ns = from.NetState;
                if (ns == null)
                    return;

                if (ns.ContainerGridLines)
                    from.Send(new VendorBuyContent6017(list));
                else
                    from.Send(new VendorBuyContent(list));

                from.Send(new VendorBuyList(this, list));

                if (ns.HighSeas)
                    from.Send(new DisplayBuyListHS(this));
                else
                    from.Send(new DisplayBuyList(this));

                from.Send(new MobileStatusExtended(from));

                if (opls != null)
                {
                    for (int i = 0; i < opls.Count; ++i)
                        from.Send(opls[i]);
                }

                SayTo(from, $"Browsing {vendorType.Name} merchandise. Have a look around!");
            }
            else
            {
                SayTo(from, $"I am currently out of stock for {vendorType.Name} goods.");
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e.Mobile.InRange(Location, InteractionRange))
            {
                string speech = e.Speech.Trim().ToLower();

                if (speech == "buy" || speech == "vendor buy")
                {
                    e.Handled = true;
                    VendorBuy(e.Mobile);
                    return;
                }
                else if (speech == "sell" || speech == "vendor sell")
                {
                    e.Handled = true;
                    VendorSell(e.Mobile);
                    return;
                }
            }

            base.OnSpeech(e);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
        }

        private List<VendorTypeDefinition> BuildVendorTypes()
        {
            var list = new List<VendorTypeDefinition>();

            // Category 0: Trades & Crafts
            list.Add(new VendorTypeDefinition("Blacksmith", VendorCategory.Trades, "Ingots, tongs, hammers, anvils, forges", 0x13E3, new SBBlacksmith()));
            list.Add(new VendorTypeDefinition("Armorer", VendorCategory.Trades, "Plate, chainmail, ringmail, helmets, shields", 0x1415,
                new SBPlateArmor(), new SBChainmailArmor(), new SBRingmailArmor(), new SBHelmetArmor(), new SBMetalShields(), new SBWoodenShields(), new SBStuddedArmor(), new SBLeatherArmor()));
            list.Add(new VendorTypeDefinition("Weaponsmith", VendorCategory.Trades, "Swords, maces, daggers, axes, spears, polearms", 0x13B9, new SBWeaponSmith()));
            list.Add(new VendorTypeDefinition("Carpenter", VendorCategory.Trades, "Furniture, woodcrafting tools, boards", 0x1034, new SBCarpenter()));
            list.Add(new VendorTypeDefinition("Tinker", VendorCategory.Trades, "Tinker tools, clockwork, gears, lockpicks, bottles", 0x1EB8, new SBTinker(this)));
            list.Add(new VendorTypeDefinition("Tailor & Weaver", VendorCategory.Trades, "Cloth, bolts, dyes, dye tubs, sewing kits", 0xF9D, new SBTailor(), new SBWeaver()));
            list.Add(new VendorTypeDefinition("Bowyer & Fletcher", VendorCategory.Trades, "Bows, crossbows, arrows, bolts, shafts, feathers", 0x13B2, new SBBowyer()));
            list.Add(new VendorTypeDefinition("Leather Worker", VendorCategory.Trades, "Hides, leather, sewing kits, tanner tools", 0x1078, new SBLeatherWorker(), new SBTanner(), new SBFurtrader()));
            list.Add(new VendorTypeDefinition("Cobbler", VendorCategory.Trades, "Boots, shoes, sandals, thigh boots", 0x170B, new SBCobbler()));
            list.Add(new VendorTypeDefinition("Stone Crafter", VendorCategory.Trades, "Mallets, chisels, granite, stone furniture", 0x12B3, new SBStoneCrafter()));
            list.Add(new VendorTypeDefinition("Glassblower", VendorCategory.Trades, "Blowpipes, sand, glass items", 0x1830, new SBGlassblower()));

            // Category 1: Magic & Scholarly
            list.Add(new VendorTypeDefinition("Mage & Mystic", VendorCategory.Magic, "Reagents, blank scrolls, spellbooks", 0xEFA, new SBMage(), new SBMystic(), new SBNecromancer(), new SBHolyMage()));
            list.Add(new VendorTypeDefinition("Alchemist", VendorCategory.Magic, "Potions, mortar & pestle, reagents, heating stand", 0xE9B, new SBAlchemist(this)));
            list.Add(new VendorTypeDefinition("Scribe", VendorCategory.Magic, "Blank scrolls, scribe pens, ink, spellbooks", 0xEF3, new SBScribe(this)));
            list.Add(new VendorTypeDefinition("Mapmaker", VendorCategory.Magic, "Blank maps, world maps, city maps, sea charts", 0x14EB, new SBMapmaker()));
            list.Add(new VendorTypeDefinition("Healer", VendorCategory.Magic, "Bandages, heal & cure potions, garlic, ginseng", 0xE21, new SBHealer()));

            // Category 2: Provisions & Food
            list.Add(new VendorTypeDefinition("Provisioner", VendorCategory.Provisions, "Backpacks, bags, bedrolls, torches, lanterns", 0x9B2, new SBProvisioner()));
            list.Add(new VendorTypeDefinition("Innkeeper", VendorCategory.Provisions, "Food, drinks, bedrolls, candles, torches", 0x1F95, new SBInnKeeper()));
            list.Add(new VendorTypeDefinition("Baker & Cook", VendorCategory.Provisions, "Bread, pies, cookies, flour, cooking utensils", 0x103B, new SBBaker(), new SBCook()));
            list.Add(new VendorTypeDefinition("Butcher", VendorCategory.Provisions, "Meat, ribs, bacon, cleavers, knives", 0xEC3, new SBButcher()));
            list.Add(new VendorTypeDefinition("Tavern Keeper", VendorCategory.Provisions, "Liquor, beer, wine, glasses, tavern fare", 0x9C7, new SBBarkeeper(), new SBTavernKeeper()));
            list.Add(new VendorTypeDefinition("Fisherman", VendorCategory.Provisions, "Fishing poles, fresh fish, oysters, bait", 0xDC0, new SBFisherman()));
            list.Add(new VendorTypeDefinition("Farmer & Beekeeper", VendorCategory.Provisions, "Pitchforks, seeds, beeswax, honey", 0xE87, new SBFarmer(), new SBBeekeeper()));
            list.Add(new VendorTypeDefinition("Miner", VendorCategory.Provisions, "Pickaxes, shovels, lanterns, torches", 0xE86, new SBMiner()));
            list.Add(new VendorTypeDefinition("Gardener & Herbalist", VendorCategory.Provisions, "Potted plants, dirt, seeds, herbs", 0x11C8, new SBGardener(), new SBHerbalist()));

            // Category 3: Services & Specialty
            list.Add(new VendorTypeDefinition("Animal Trainer", VendorCategory.Services, "Horses, pack animals, dogs, cats", 0xE81, new SBAnimalTrainer()));
            list.Add(new VendorTypeDefinition("Architect & Deeds", VendorCategory.Services, "House deeds, house placement tools", 0x14F0, new SBArchitect(), new SBRealEstateBroker(), new SBHouseDeed()));
            list.Add(new VendorTypeDefinition("Shipwright", VendorCategory.Services, "Small, medium, and large ship deeds", 0x14F1, new SBShipwright(this)));
            list.Add(new VendorTypeDefinition("Jeweler", VendorCategory.Services, "Gold rings, necklaces, earrings, gems", 0x108A, new SBJewel()));
            list.Add(new VendorTypeDefinition("Bard", VendorCategory.Services, "Lutes, drums, harps, flutes, tamborines", 0xEB3, new SBBard()));
            list.Add(new VendorTypeDefinition("Hair Stylist", VendorCategory.Services, "Hair dyes, special grooming tools", 0xF9F, new SBHairStylist()));
            list.Add(new VendorTypeDefinition("Thief", VendorCategory.Services, "Lockpicks, disguise kits, cloaks", 0x14FB, new SBThief()));
            list.Add(new VendorTypeDefinition("Variety Dealer", VendorCategory.Services, "Assorted general adventure supplies", 0xE41, new SBVarietyDealer()));

            return list;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            _ = reader.ReadInt();
        }
    }

    public enum VendorCategory
    {
        Trades = 0,
        Magic = 1,
        Provisions = 2,
        Services = 3
    }

    public class VendorTypeDefinition
    {
        public string Name { get; }
        public VendorCategory Category { get; }
        public string Description { get; }
        public int ItemID { get; }
        public List<SBInfo> SBInfos { get; }

        public VendorTypeDefinition(string name, VendorCategory category, string description, int itemID, params SBInfo[] sbInfos)
        {
            Name = name;
            Category = category;
            Description = description;
            ItemID = itemID;
            SBInfos = new List<SBInfo>(sbInfos);
        }
    }

    public class UniversalVendorTypeGump : Gump
    {
        private const int GumpWidth = 720;
        private const int GumpHeight = 485;

        private readonly Mobile m_Player;
        private readonly UniversalVendor m_Vendor;
        private readonly VendorCategory m_CurrentCategory;

        public UniversalVendorTypeGump(Mobile player, UniversalVendor vendor, VendorCategory category = VendorCategory.Trades) : base(60, 50)
        {
            m_Player = player;
            m_Vendor = vendor;
            m_CurrentCategory = category;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // Textured dark slate window background
            AddBackground(0, 0, GumpWidth, GumpHeight, 9200);
            AddImageTiled(10, 10, GumpWidth - 20, GumpHeight - 20, 2624);
            AddAlphaRegion(10, 10, GumpWidth - 20, GumpHeight - 20);

            // Header Section
            AddItem(22, 14, 0x0E76); // Coin pouch icon
            AddHtml(64, 14, 500, 24, "<BASEFONT COLOR=#FCCA03 SIZE=5><B>Universal Merchant Catalogue</B></BASEFONT>", false, false);
            AddHtml(64, 38, 550, 20, "<BASEFONT COLOR=#B0BEC5>Select a merchant profession below to browse their goods and wares:</BASEFONT>", false, false);

            // Close button in header (top right)
            AddButton(GumpWidth - 42, 16, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);

            // Header horizontal divider
            AddImageTiled(18, 62, GumpWidth - 36, 2, 9354);

            // Category Navigation Tabs (Row Y=70, H=32)
            int tabY = 70;
            int tabW = 165;
            AddCategoryTab(18, tabY, tabW, "Trades & Crafts", VendorCategory.Trades);
            AddCategoryTab(191, tabY, tabW, "Magic & Lore", VendorCategory.Magic);
            AddCategoryTab(364, tabY, tabW, "Provisions & Food", VendorCategory.Provisions);
            AddCategoryTab(537, tabY, tabW, "Services & Misc", VendorCategory.Services);

            // Sub-tab horizontal divider
            AddImageTiled(18, 108, GumpWidth - 36, 2, 9354);

            // Content Area - 2 Column Grid of Profession Cards
            var allTypes = m_Vendor.VendorTypes;
            int col0X = 18;
            int col1X = 366;
            int cardW = 336;
            int cardH = 46;
            int indexInCat = 0;

            for (int i = 0; i < allTypes.Count; i++)
            {
                var vt = allTypes[i];
                if (vt.Category != category)
                    continue;

                bool isLeftCol = (indexInCat % 2 == 0);
                int x = isLeftCol ? col0X : col1X;
                int row = indexInCat / 2;
                int y = 116 + (row * 50);

                int buttonID = 100 + i;

                // Card panel background
                AddBackground(x, y, cardW, cardH, 9200);
                AddImageTiled(x + 2, y + 2, cardW - 4, cardH - 4, 2624);
                AddAlphaRegion(x + 2, y + 2, cardW - 4, cardH - 4);

                // Browse / select button
                AddButton(x + 6, y + 12, 4005, 4007, buttonID, GumpButtonType.Reply, 0);

                // Profession item graphic inset
                AddImageTiled(x + 32, y + 6, 36, 34, 2624);
                AddAlphaRegion(x + 32, y + 6, 36, 34);
                AddItem(x + 34, y + 7, vt.ItemID);

                // Profession title
                AddHtml(x + 74, y + 4, cardW - 80, 20, $"<BASEFONT COLOR=#FFE082><B>{vt.Name}</B></BASEFONT>", false, false);

                // Wares description strictly bounded so text never bleeds into adjacent column
                AddHtml(x + 74, y + 23, cardW - 80, 20, $"<BASEFONT COLOR=#90A4AE SIZE=1>{vt.Description}</BASEFONT>", false, false);

                indexInCat++;
            }

            // Footer Area
            AddImageTiled(18, 422, GumpWidth - 36, 2, 9354);

            // Selling helper info
            AddHtml(22, 432, 370, 42, "<BASEFONT COLOR=#B0BEC5><B>💡 Selling Items:</B><BR>Click <BASEFONT COLOR=#69F0AE>Sell Backpack</BASEFONT> or speak <BASEFONT COLOR=#00FFCC>\"vendor sell\"</BASEFONT> to sell goods.</BASEFONT>", false, false);

            // Direct Sell Backpack button
            AddButton(410, 436, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtml(442, 438, 140, 20, "<BASEFONT COLOR=#69F0AE><B>Sell Backpack Items</B></BASEFONT>", false, false);

            // Close button
            AddButton(590, 436, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
            AddHtml(624, 438, 60, 20, "<BASEFONT COLOR=#ECEFF1>Close</BASEFONT>", false, false);
        }

        private void AddCategoryTab(int x, int y, int width, string label, VendorCategory cat)
        {
            bool isSelected = (m_CurrentCategory == cat);
            int buttonID = 10 + (int)cat;
            int count = GetCategoryCount(cat);

            AddBackground(x, y, width, 32, isSelected ? 9200 : 9300);
            if (isSelected)
            {
                AddImageTiled(x + 2, y + 2, width - 4, 28, 2624);
                AddAlphaRegion(x + 2, y + 2, width - 4, 28);
            }

            AddButton(x + 6, y + 6, isSelected ? 4006 : 4005, isSelected ? 4007 : 4006, buttonID, GumpButtonType.Reply, 0);

            string tabText = isSelected
                ? $"<BASEFONT COLOR=#00FFCC><B>{label}</B></BASEFONT> <BASEFONT COLOR=#80CBC4>({count})</BASEFONT>"
                : $"<BASEFONT COLOR=#ECEFF1>{label}</BASEFONT> <BASEFONT COLOR=#90A4AE>({count})</BASEFONT>";

            AddHtml(x + 32, y + 7, width - 36, 20, tabText, false, false);
        }

        private int GetCategoryCount(VendorCategory cat)
        {
            if (m_Vendor?.VendorTypes == null)
                return 0;

            int count = 0;
            for (int i = 0; i < m_Vendor.VendorTypes.Count; i++)
            {
                if (m_Vendor.VendorTypes[i].Category == cat)
                    count++;
            }
            return count;
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            PlayerMobile pm = sender.Mobile as PlayerMobile;
            if (pm == null || m_Vendor == null || m_Vendor.Deleted)
                return;

            int id = info.ButtonID;

            if (id == 0) // Closed / Cancelled
                return;

            // Direct Sell backpack wares
            if (id == 2)
            {
                m_Vendor.VendorSell(pm);
                return;
            }

            // Tab navigation (10-13)
            if (id >= 10 && id < 20)
            {
                VendorCategory newCat = (VendorCategory)(id - 10);
                pm.CloseGump(typeof(UniversalVendorTypeGump));
                pm.SendGump(new UniversalVendorTypeGump(pm, m_Vendor, newCat));
                return;
            }

            // Profession selected (100+)
            if (id >= 100)
            {
                int typeIdx = id - 100;
                var allTypes = m_Vendor.VendorTypes;

                if (typeIdx >= 0 && typeIdx < allTypes.Count)
                {
                    m_Vendor.OpenCategoryBuy(pm, allTypes[typeIdx]);
                }
            }
        }
    }
}
