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
            base.InitOutfit();

            AddItem(new FancyShirt(0x486));
            AddItem(new LongPants(0x1BB));
            AddItem(new ThighBoots(0x486));
            AddItem(new Cloak(0x486));
            AddItem(new BodySash(0x8A5));
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
            list.Add(new VendorTypeDefinition("Blacksmith", VendorCategory.Trades, "Ingots, tongs, hammers, anvils, forges", new SBBlacksmith()));
            list.Add(new VendorTypeDefinition("Armorer", VendorCategory.Trades, "Plate, chainmail, ringmail, helmets, shields, leather armor",
                new SBPlateArmor(), new SBChainmailArmor(), new SBRingmailArmor(), new SBHelmetArmor(), new SBMetalShields(), new SBWoodenShields(), new SBStuddedArmor(), new SBLeatherArmor()));
            list.Add(new VendorTypeDefinition("Weaponsmith", VendorCategory.Trades, "Swords, maces, daggers, axes, spears, polearms", new SBWeaponSmith()));
            list.Add(new VendorTypeDefinition("Carpenter", VendorCategory.Trades, "Furniture, woodcrafting tools, boards", new SBCarpenter()));
            list.Add(new VendorTypeDefinition("Tinker", VendorCategory.Trades, "Tinker tools, clockwork, gears, lockpicks, bottles", new SBTinker(this)));
            list.Add(new VendorTypeDefinition("Tailor & Weaver", VendorCategory.Trades, "Cloth, bolts, dyes, dye tubs, sewing kits, clothing", new SBTailor(), new SBWeaver()));
            list.Add(new VendorTypeDefinition("Bowyer & Fletcher", VendorCategory.Trades, "Bows, crossbows, arrows, bolts, shafts, feathers", new SBBowyer()));
            list.Add(new VendorTypeDefinition("Leather Worker", VendorCategory.Trades, "Hides, leather, sewing kits, tanner tools", new SBLeatherWorker(), new SBTanner(), new SBFurtrader()));
            list.Add(new VendorTypeDefinition("Cobbler", VendorCategory.Trades, "Boots, shoes, sandals, thigh boots", new SBCobbler()));
            list.Add(new VendorTypeDefinition("Stone Crafter", VendorCategory.Trades, "Mallets, chisels, granite, stone furniture", new SBStoneCrafter()));
            list.Add(new VendorTypeDefinition("Glassblower", VendorCategory.Trades, "Blowpipes, sand, glass items", new SBGlassblower()));

            // Category 1: Magic & Scholarly
            list.Add(new VendorTypeDefinition("Mage & Mystic", VendorCategory.Magic, "Reagents, blank scrolls, spellbooks, mystic reagents", new SBMage(), new SBMystic(), new SBNecromancer(), new SBHolyMage()));
            list.Add(new VendorTypeDefinition("Alchemist", VendorCategory.Magic, "Potions, mortar & pestle, reagents, heating stand", new SBAlchemist(this)));
            list.Add(new VendorTypeDefinition("Scribe", VendorCategory.Magic, "Blank scrolls, scribe pens, ink, spellbooks", new SBScribe()));
            list.Add(new VendorTypeDefinition("Mapmaker", VendorCategory.Magic, "Blank maps, world maps, city maps, sea charts", new SBMapmaker()));
            list.Add(new VendorTypeDefinition("Healer", VendorCategory.Magic, "Bandages, heal & cure potions, garlic, ginseng", new SBHealer()));

            // Category 2: Provisions & Food
            list.Add(new VendorTypeDefinition("Provisioner", VendorCategory.Provisions, "Backpacks, bags, bedrolls, torches, lanterns, rope", new SBProvisioner()));
            list.Add(new VendorTypeDefinition("Innkeeper", VendorCategory.Provisions, "Food, drinks, bedrolls, candles, torches", new SBInnKeeper()));
            list.Add(new VendorTypeDefinition("Baker & Cook", VendorCategory.Provisions, "Bread, pies, cookies, flour, cooking utensils", new SBBaker(), new SBCook()));
            list.Add(new VendorTypeDefinition("Butcher", VendorCategory.Provisions, "Meat, ribs, bacon, cleavers, knives", new SBButcher()));
            list.Add(new VendorTypeDefinition("Tavern Keeper", VendorCategory.Provisions, "Liquor, beer, wine, glasses, tavern fare", new SBBarkeeper(), new SBTavernKeeper()));
            list.Add(new VendorTypeDefinition("Fisherman", VendorCategory.Provisions, "Fishing poles, fresh fish, oysters, bait", new SBFisherman()));
            list.Add(new VendorTypeDefinition("Farmer & Beekeeper", VendorCategory.Provisions, "Pitchforks, seeds, beeswax, honey", new SBFarmer(), new SBBeekeeper()));
            list.Add(new VendorTypeDefinition("Miner", VendorCategory.Provisions, "Pickaxes, shovels, lanterns, torches", new SBMiner()));
            list.Add(new VendorTypeDefinition("Gardener & Herbalist", VendorCategory.Provisions, "Potted plants, dirt, seeds, herbs", new SBGardener(), new SBHerbalist()));

            // Category 3: Services & Specialty
            list.Add(new VendorTypeDefinition("Animal Trainer", VendorCategory.Services, "Horses, pack animals, dogs, cats", new SBAnimalTrainer()));
            list.Add(new VendorTypeDefinition("Architect & Deeds", VendorCategory.Services, "House deeds, house placement tools", new SBArchitect(), new SBRealEstateBroker(), new SBHouseDeed()));
            list.Add(new VendorTypeDefinition("Shipwright", VendorCategory.Services, "Small, medium, and large ship deeds", new SBShipwright(this)));
            list.Add(new VendorTypeDefinition("Jeweler", VendorCategory.Services, "Gold rings, necklaces, earrings, gems", new SBJewel()));
            list.Add(new VendorTypeDefinition("Bard", VendorCategory.Services, "Lutes, drums, harps, flutes, tamborines", new SBBard()));
            list.Add(new VendorTypeDefinition("Hair Stylist", VendorCategory.Services, "Hair dyes, special grooming tools", new SBHairStylist()));
            list.Add(new VendorTypeDefinition("Thief", VendorCategory.Services, "Lockpicks, disguise kits, cloaks", new SBThief()));
            list.Add(new VendorTypeDefinition("Variety Dealer", VendorCategory.Services, "Assorted general adventure supplies", new SBVarietyDealer()));

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
        public List<SBInfo> SBInfos { get; }

        public VendorTypeDefinition(string name, VendorCategory category, string description, params SBInfo[] sbInfos)
        {
            Name = name;
            Category = category;
            Description = description;
            SBInfos = new List<SBInfo>(sbInfos);
        }
    }

    public class UniversalVendorTypeGump : Gump
    {
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

            // Sleek dark stone window background
            AddBackground(0, 0, 560, 480, 0x2422);
            AddAlphaRegion(10, 10, 540, 460);

            // Title & instructions
            AddHtml(20, 20, 520, 25, "<center><basefont color=#F0D060 size=5>Universal Merchant Catalogue</basefont></center>", false, false);
            AddHtml(20, 45, 520, 25, "<center><basefont color=#CCCCCC>Select a merchant profession to browse their ware inventory:</basefont></center>", false, false);

            // Category Tab Buttons
            int tabY = 75;
            AddTab(20, tabY, "Trades & Crafts", VendorCategory.Trades);
            AddTab(155, tabY, "Magic & Lore", VendorCategory.Magic);
            AddTab(290, tabY, "Provisions & Food", VendorCategory.Provisions);
            AddTab(425, tabY, "Services & Misc", VendorCategory.Services);

            // Divider
            AddImageTiled(20, 108, 520, 2, 0x2711);

            // Render professions for current tab in 2 columns
            var allTypes = m_Vendor.VendorTypes;
            int col0X = 30;
            int col1X = 290;
            int row0Y = 122;
            int row1Y = 122;
            int indexInCat = 0;

            for (int i = 0; i < allTypes.Count; i++)
            {
                var vt = allTypes[i];
                if (vt.Category != category)
                    continue;

                bool isLeftCol = (indexInCat % 2 == 0);
                int x = isLeftCol ? col0X : col1X;
                int y = isLeftCol ? row0Y : row1Y;

                int buttonID = 100 + i;

                // Selection button
                AddButton(x, y + 2, 0x15E1, 0x15E5, buttonID, GumpButtonType.Reply, 0);

                // Profession title
                AddLabel(x + 28, y, 0xFF, vt.Name);

                // Wares brief description
                AddLabel(x + 28, y + 16, 0x35, vt.Description);

                if (isLeftCol)
                    row0Y += 40;
                else
                    row1Y += 40;

                indexInCat++;
            }

            // Footer note
            AddHtml(20, 445, 520, 25, "<center><basefont color=#999999 size=2>Tip: Select 'Sell' from the context menu or speak 'sell' to sell any backpack items.</basefont></center>", false, false);
        }

        private void AddTab(int x, int y, string label, VendorCategory cat)
        {
            bool isSelected = (m_CurrentCategory == cat);
            int buttonID = 10 + (int)cat;

            if (isSelected)
            {
                AddImageTiled(x - 2, y - 2, 120, 26, 0x0A3C);
                AddButton(x, y, 0x0FA5, 0x0FA7, buttonID, GumpButtonType.Reply, 0);
                AddLabel(x + 20, y + 2, 0x35, label);
            }
            else
            {
                AddButton(x, y, 0x0FA5, 0x0FA7, buttonID, GumpButtonType.Reply, 0);
                AddLabel(x + 20, y + 2, 0x7E1, label);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            PlayerMobile pm = sender.Mobile as PlayerMobile;
            if (pm == null || m_Vendor == null || m_Vendor.Deleted)
                return;

            int id = info.ButtonID;

            if (id == 0) // Closed / Cancelled
                return;

            // Tab navigation (10-13)
            if (id >= 10 && id < 20)
            {
                VendorCategory newCat = (VendorCategory)(id - 10);
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
