// ============================================================================
// BulkOrderMaster.cs - Dedicated Bulk Order Master NPC & 8-in-1 Crafting Hub
//
// Origin: FesterUO Custom Scripts (custom-scripts/FesterUO/)
// Purpose: A central artisan guildmaster mobile placed in crafting squares or
//          player workshops that manages Bulk Order Deeds across all 8 craft trades:
//          Blacksmithing, Tailoring, Tinkering, Carpentry, Fletching,
//          Alchemy, Inscription, and Cooking.
//
// Features:
// - Placed via [add BulkOrderMaster; defaults to frozen, invulnerable, and blessed.
// - Interactive 8-in-1 Gump: Double-click or speech ("bod", "order", "craft", etc.)
//   displays an overview of all 8 craft trades with current skills, banked points,
//   real-time cooldown timers, and cached deed counts.
// - One-Stop Claiming: Claim ready BODs directly from the gump without running
//   across Britannia to 8 separate shopkeepers.
// - Universal Turn-In: Drag-and-drop any completed Small or Large BOD onto the Master
//   to automatically receive gold, fame, City Loyalty, and banked/pending points.
// - Rewards Catalog: Direct access to Publish 95 bulk order reward point stores
//   for all 8 professions.
// - Bribe Support: Right-click context menu supports bribing to upgrade BODs.
// - Point Banking Mode: Toggle between Automatic, Enabled, and Disabled point banking.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.ContextMenus;
using Server.Engines.BulkOrders;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Custom.FesterUO
{
    public class BulkOrderMaster : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        private BODType m_ActiveBODType = BODType.Smith;

        public override BODType BODType => m_ActiveBODType;
        public void SetActiveBODType(BODType type) => m_ActiveBODType = type;

        protected override List<SBInfo> SBInfos => m_SBInfos;
        public override void InitSBInfo() { }

        public override bool CanBuy => false;
        public override bool CanSell => false;
        public override bool CanTeach => false;
        public override bool ChangeRace => false;
        public override bool IsInvulnerable => true;
        public override bool ShowFameTitle => false;
        public override bool ClickTitle => true;
        public override bool SupportsBulkOrders(Mobile from) => false;

        [Constructable]
        public BulkOrderMaster() : base("the Bulk Order Master")
        {
            Name = Config.Get("BulkOrders.NPCName", "Artisan Guildmaster");
            Title = Config.Get("BulkOrders.NPCTitle", "the Bulk Order Master");

            Frozen = true;
            CantWalk = true;
            Blessed = true;
            Direction = Direction.South;

            SpeechHue = 53;
        }

        public BulkOrderMaster(Serial serial) : base(serial)
        {
        }

        public override void InitOutfit()
        {
            Female = Utility.RandomBool();
            Body = Female ? 0x191 : 0x190;
            Hue = Race.RandomSkinHue();

            HairItemID = Female ? Race.RandomHair(true) : Race.RandomHair(false);
            HairHue = Race.RandomHairHue();

            if (!Female)
            {
                FacialHairItemID = Race.RandomFacialHair();
                FacialHairHue = HairHue;
            }

            SetWearable(new FancyShirt(1150));
            SetWearable(new LongPants(1109));
            SetWearable(new FullApron(1175)); // Artisan dark navy apron
            SetWearable(new ThighBoots(1175));
            SetWearable(new GoldRing());
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            int range = Config.Get("BulkOrders.InteractionRange", 4);
            return InRange(from.Location, range);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            Mobile from = e.Mobile;
            int range = Config.Get("BulkOrders.InteractionRange", 4);

            if (from == null || !from.Alive || !InRange(from.Location, range))
                return;

            string speech = e.Speech.Trim().ToLower();
            if (speech.Contains("bod") || speech.Contains("order") || speech.Contains("bulk") ||
                speech.Contains("craft") || speech.Contains("reward") || speech.Contains("trade"))
            {
                e.Handled = true;
                OpenBODGump(from);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            int range = Config.Get("BulkOrders.InteractionRange", 4);

            if (from.InRange(Location, range))
            {
                OpenBODGump(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            int range = Config.Get("BulkOrders.InteractionRange", 4);
            if (from.Alive && from is PlayerMobile pm && pm.InRange(Location, range) && CheckVendorAccess(from))
            {
                list.Add(new SimpleContextMenuEntry(from, 6152, m => OpenBODGump(m))); // "Bulk Order Info"
                list.Add(new SimpleContextMenuEntry(from, 1155593, m => OpenBODGump(m))); // "Claim Rewards"
                list.Add(new SimpleContextMenuEntry(from, 1152295, m => TryBribeMaster(m))); // "Bribe"
            }
        }

        public void OpenBODGump(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive)
            {
                pm.CloseGump(typeof(BulkOrderMasterGump));
                pm.SendGump(new BulkOrderMasterGump(this, pm));
            }
        }

        public void ClaimBOD(PlayerMobile pm, BODType type)
        {
            int range = Config.Get("BulkOrders.InteractionRange", 4);
            if (pm == null || !pm.Alive || !InRange(pm.Location, range))
                return;

            SkillName skill = BulkOrderSystem.GetSkillForBOD(type);
            double skillVal = pm.Skills[skill].Base;
            double minSkill = Config.Get("BulkOrders.MinSkillRequired", 0.1);

            if (skillVal < minSkill && pm.AccessLevel == AccessLevel.Player)
            {
                pm.SendMessage(0x22, $"You need at least {minSkill:F1}% {skill} skill to claim bulk orders for this trade.");
                return;
            }

            if (!BulkOrderSystem.CanGetBulkOrder(pm, type) && pm.AccessLevel == AccessLevel.Player)
            {
                TimeSpan next = BulkOrderSystem.GetNextBulkOrder(type, pm);
                int totalMinutes = (int)Math.Max(1, Math.Ceiling(next.TotalMinutes));
                SayTo(pm, 1072058, totalMinutes.ToString(), 0x3B2); // An offer may be available in about ~1_minutes~ minutes.
                return;
            }

            Item bod = BulkOrderSystem.CreateBulkOrder(pm, type, true);
            if (bod != null)
            {
                pm.CloseGump(typeof(BulkOrderMasterGump));

                if (bod is LargeBOD large)
                {
                    pm.CloseGump(typeof(LargeBODAcceptGump));
                    pm.SendGump(new LargeBODAcceptGump(pm, large));
                }
                else if (bod is SmallBOD small)
                {
                    pm.CloseGump(typeof(SmallBODAcceptGump));
                    pm.SendGump(new SmallBODAcceptGump(pm, small));
                }

                SayTo(pm, $"Here is your {type} bulk order, {pm.Name}. Good luck with your crafting!");
            }
        }

        public void OpenRewards(PlayerMobile pm, BODType type)
        {
            int range = Config.Get("BulkOrders.InteractionRange", 4);
            if (pm == null || !pm.Alive || !InRange(pm.Location, range))
                return;

            m_ActiveBODType = type;

            var context = BulkOrderSystem.GetContext(pm);
            int pending = context != null ? context.GetPendingRewardFor(type) : 0;

            pm.CloseGump(typeof(BulkOrderMasterGump));

            if (pending > 0)
            {
                if (context.PointsMode == PointsMode.Enabled)
                {
                    pm.SendGump(new ConfirmBankPointsGump(pm, this, type, pending, (double)pending * 0.02));
                }
                else
                {
                    pm.SendGump(new RewardsGump(this, pm, type, pending));
                }
            }
            else if (!BulkOrderSystem.CanClaimRewards(pm))
            {
                SayTo(pm, 1157083, 0x3B2); // You must claim your last turn-in reward in order for us to continue doing business.
            }
            else
            {
                pm.SendGump(new RewardsGump(this, pm, type));
            }
        }

        public void TryBribeMaster(Mobile m)
        {
            if (UnderWatch)
            {
                if (WatchEnds < DateTime.UtcNow)
                {
                    WatchEnds = DateTime.MinValue;
                    RecentBribes = 0;
                }
                else
                {
                    SayTo(m, 1152293, 0x3B2); // My business is being watched by the Guild, so I can't be messing with bulk orders right now. Come back when there's less heat on me!
                    return;
                }
            }

            SayTo(m, 1152295, 0x3B2); // So you want to do a little business under the table?
            m.SendLocalizedMessage(1152296); // Target a bulk order deed to show to the shopkeeper.

            m.BeginTarget(-1, false, TargetFlags.None, (from, targeted) =>
            {
                IBOD bod = targeted as IBOD;

                if (bod is Item item && item.IsChildOf(from.Backpack))
                {
                    m_ActiveBODType = bod.BODType;

                    if (BulkOrderSystem.CanExchangeBOD(from, this, bod, -1))
                    {
                        int amount = BulkOrderSystem.GetBribe(bod);
                        amount *= BribeMultiplier;

                        if (Bribes == null)
                            Bribes = new Dictionary<Mobile, PendingBribe>();

                        if (!Bribes.ContainsKey(m))
                            Bribes[m] = new PendingBribe(bod, amount);
                        else
                        {
                            Bribes[m].BOD = bod;
                            Bribes[m].Amount = amount;
                        }

                        SayTo(from, 1152292, amount.ToString("N0", CultureInfo.GetCultureInfo("en-US")), 0x3B2);
                    }
                }
                else if (bod == null)
                {
                    SayTo(from, 1152297, 0x3B2); // That is not a bulk order deed.
                }
            });
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is SmallBOD || dropped is LargeBOD)
            {
                IBOD bod = dropped as IBOD;
                if (bod == null)
                    return false;

                m_ActiveBODType = bod.BODType;

                if (BulkOrderSystem.NewSystemEnabled && Bribes != null && Bribes.ContainsKey(from) && Bribes[from].BOD == bod)
                {
                    if (BulkOrderSystem.CanExchangeBOD(from, this, bod, Bribes[from].Amount))
                    {
                        DoBribe(from, bod);
                        return false;
                    }
                }

                PlayerMobile pm = from as PlayerMobile;
                if (Core.ML && pm != null && pm.NextBODTurnInTime > DateTime.UtcNow)
                {
                    SayTo(from, 1079976, 0x3B2); // You'll have to wait a few seconds while I inspect the last order.
                    return false;
                }

                if (!bod.Complete)
                {
                    SayTo(from, 1045131, 0x3B2); // You have not completed the order yet.
                    return false;
                }

                if (!BulkOrderSystem.CanClaimRewards(from))
                {
                    SayTo(from, 1157083, 0x3B2); // You must claim your last turn-in reward in order for us to continue doing business.
                    return false;
                }

                Item reward;
                int gold, fame;

                if (dropped is SmallBOD smallBOD)
                    smallBOD.GetRewards(out reward, out gold, out fame);
                else if (dropped is LargeBOD largeBOD)
                    largeBOD.GetRewards(out reward, out gold, out fame);
                else
                {
                    gold = 0;
                    fame = 0;
                    reward = null;
                }

                from.SendSound(0x3D);

                if (BulkOrderSystem.NewSystemEnabled && from is PlayerMobile)
                {
                    SayTo(from, 1157204, from.Name, 0x3B2); // Ho! Ho! Thank ye ~1_PLAYER~ for giving me a Bulk Order Deed!

                    BODContext context = BulkOrderSystem.GetContext(from);
                    int points = 0;
                    double banked = 0.0;

                    if (dropped is SmallBOD sBOD)
                        BulkOrderSystem.ComputePoints(sBOD, out points, out banked);
                    else if (dropped is LargeBOD lBOD)
                        BulkOrderSystem.ComputePoints(lBOD, out points, out banked);

                    switch (context.PointsMode)
                    {
                        case PointsMode.Enabled:
                            context.AddPending(bod.BODType, points);
                            from.SendGump(new ConfirmBankPointsGump((PlayerMobile)from, this, bod.BODType, points, banked));
                            break;
                        case PointsMode.Disabled:
                            context.AddPending(bod.BODType, points);
                            from.SendGump(new RewardsGump(this, (PlayerMobile)from, bod.BODType, points));
                            break;
                        case PointsMode.Automatic:
                            BulkOrderSystem.SetPoints(from, bod.BODType, banked);
                            from.SendGump(new RewardsGump(this, (PlayerMobile)from, bod.BODType));
                            break;
                    }

                    Banker.Deposit(from, gold, true);
                }
                else
                {
                    SayTo(from, 1045132, 0x3B2); // Thank you so much! Here is a reward for your effort.

                    if (reward != null)
                        from.AddToBackpack(reward);

                    Banker.Deposit(from, gold, true);
                }

                Titles.AwardFame(from, fame, true);
                Server.Engines.CityLoyalty.CityLoyaltySystem.OnBODTurnIn(from, gold);

                if (Core.ML && pm != null)
                {
                    pm.NextBODTurnInTime = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
                }

                dropped.Delete();
                return true;
            }

            return base.OnDragDrop(from, dropped);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class BulkOrderMasterGump : Gump
    {
        public class TradeEntry
        {
            public BODType Type { get; }
            public string Name { get; }
            public SkillName Skill { get; }
            public int Hue { get; }

            public TradeEntry(BODType type, string name, SkillName skill, int hue)
            {
                Type = type;
                Name = name;
                Skill = skill;
                Hue = hue;
            }
        }

        public static readonly TradeEntry[] Trades = new TradeEntry[]
        {
            new TradeEntry(BODType.Smith, "Blacksmithing", SkillName.Blacksmith, 0x44E),
            new TradeEntry(BODType.Tailor, "Tailoring", SkillName.Tailoring, 0x483),
            new TradeEntry(BODType.Tinkering, "Tinkering", SkillName.Tinkering, 1109),
            new TradeEntry(BODType.Carpentry, "Carpentry", SkillName.Carpentry, 1512),
            new TradeEntry(BODType.Fletching, "Bowcraft & Fletching", SkillName.Fletching, 1425),
            new TradeEntry(BODType.Alchemy, "Alchemy", SkillName.Alchemy, 2505),
            new TradeEntry(BODType.Inscription, "Inscription", SkillName.Inscribe, 2598),
            new TradeEntry(BODType.Cooking, "Cooking", SkillName.Cooking, 1169)
        };

        private readonly BulkOrderMaster m_Master;
        private readonly PlayerMobile m_Player;

        public BulkOrderMasterGump(BulkOrderMaster master, PlayerMobile player) : base(50, 50)
        {
            m_Master = master;
            m_Player = player;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // Outer Frame
            AddBackground(0, 0, 700, 560, 9270);
            AddAlphaRegion(15, 15, 670, 530);

            // Header Icon & Titles
            AddItem(25, 20, 0x2258, 1175);
            AddHtml(70, 20, 360, 24, "<BIG><BASEFONT COLOR=#FCCA03>Artisan Bulk Order Guild</BASEFONT></BIG>", false, false);
            AddHtml(70, 44, 380, 20, "<BASEFONT COLOR=#BBBBBB>Master Registrar: Claim deeds, review queues, and redeem rewards.</BASEFONT>", false, false);

            // Banking Mode Toggle Banner
            BODContext context = BulkOrderSystem.GetContext(m_Player);
            string modeStr = context != null ? context.PointsMode.ToString() : "Automatic";
            AddHtml(440, 24, 150, 20, $"<RIGHT><BASEFONT COLOR=#EEEEEE>Mode: <b>{modeStr}</b></BASEFONT></RIGHT>", false, false);
            AddButton(600, 22, 4005, 4007, 1, GumpButtonType.Reply, 0); // Toggle Mode
            AddHtml(635, 24, 50, 20, "<BASEFONT COLOR=#00FF99>Toggle</BASEFONT>", false, false);

            // Top Header Divider
            AddImageTiled(25, 68, 650, 2, 9277);

            // Column Headers
            AddHtml(35, 74, 160, 20, "<BASEFONT COLOR=#FCCA03><b>Trade / Profession</b></BASEFONT>", false, false);
            AddHtml(205, 74, 65, 20, "<BASEFONT COLOR=#FCCA03><b>Skill</b></BASEFONT>", false, false);
            AddHtml(275, 74, 100, 20, "<BASEFONT COLOR=#FCCA03><b>Banked Pts</b></BASEFONT>", false, false);
            AddHtml(385, 74, 140, 20, "<BASEFONT COLOR=#FCCA03><b>Queue Status</b></BASEFONT>", false, false);
            AddHtml(535, 74, 130, 20, "<BASEFONT COLOR=#FCCA03><b>Actions</b></BASEFONT>", false, false);

            // Second Divider
            AddImageTiled(25, 96, 650, 2, 9277);

            double minSkill = Config.Get("BulkOrders.MinSkillRequired", 0.1);

            // Rows
            for (int i = 0; i < Trades.Length; i++)
            {
                TradeEntry trade = Trades[i];
                int y = 104 + (i * 48);

                // Alternating row background
                AddImageTiled(25, y, 650, 44, 2624);
                AddAlphaRegion(25, y, 650, 44);

                // Colored Deed Icon
                AddItem(30, y + 8, 0x14F0, trade.Hue);

                // Profession Name
                AddHtml(65, y + 12, 135, 20, $"<BASEFONT COLOR=#FFFFFF><b>{trade.Name}</b></BASEFONT>", false, false);

                // Skill Value
                double skillVal = m_Player.Skills[trade.Skill].Base;
                string skillColor = skillVal >= 100.0 ? "#FCCA03" : (skillVal > 0 ? "#00FF99" : "#777777");
                AddHtml(205, y + 12, 65, 20, $"<BASEFONT COLOR={skillColor}>{skillVal:F1}%</BASEFONT>", false, false);

                // Banked Points
                double pts = BulkOrderSystem.GetPoints(m_Player, trade.Type);
                AddHtml(275, y + 12, 100, 20, $"<BASEFONT COLOR=#FCCA03>{pts:N1} pts</BASEFONT>", false, false);

                // Queue & Readiness Status
                bool canClaimSkill = skillVal >= minSkill || m_Player.AccessLevel > AccessLevel.Player;
                bool isReady = false;
                int cached = 0;
                TimeSpan next = TimeSpan.Zero;

                if (context != null && context.Entries.ContainsKey(trade.Type))
                {
                    var entry = context.Entries[trade.Type];
                    if (entry != null)
                    {
                        entry.CheckCache();
                        cached = entry.CachedDeeds;
                        if (entry.LastBulkOrder + TimeSpan.FromHours(BulkOrderSystem.Delay) < DateTime.UtcNow)
                        {
                            isReady = true;
                        }
                        else if (cached > 0)
                        {
                            isReady = true;
                        }
                        else
                        {
                            next = BulkOrderSystem.GetNextBulkOrder(trade.Type, m_Player);
                        }
                    }
                }
                else
                {
                    isReady = true;
                }

                string statusHtml;
                if (!canClaimSkill)
                {
                    statusHtml = $"<BASEFONT COLOR=#777777>Req. {minSkill:F1}%</BASEFONT>";
                }
                else if (isReady || m_Player.AccessLevel > AccessLevel.Player)
                {
                    string cacheStr = cached > 0 ? $" <BASEFONT COLOR=#FFDD55>({cached} saved)</BASEFONT>" : "";
                    statusHtml = $"<BASEFONT COLOR=#00FF66><b>READY</b></BASEFONT>{cacheStr}";
                }
                else
                {
                    int totalMinutes = (int)Math.Max(1, Math.Ceiling(next.TotalMinutes));
                    statusHtml = $"<BASEFONT COLOR=#FF9933>Ready in {totalMinutes}m</BASEFONT>";
                }
                AddHtml(385, y + 12, 140, 20, statusHtml, false, false);

                // Claim Button
                if (canClaimSkill && (isReady || m_Player.AccessLevel > AccessLevel.Player))
                {
                    AddButton(535, y + 10, 4005, 4007, 100 + i, GumpButtonType.Reply, 0);
                    AddHtml(570, y + 12, 45, 20, "<BASEFONT COLOR=#00FF99>Claim</BASEFONT>", false, false);
                }

                // Rewards Catalog Button
                AddButton(615, y + 10, 4011, 4013, 200 + i, GumpButtonType.Reply, 0);
                AddHtml(650, y + 12, 45, 20, "<BASEFONT COLOR=#FCCA03>Shop</BASEFONT>", false, false);
            }

            // Footer Divider
            AddImageTiled(25, 492, 650, 2, 9277);

            // Instructions Tip
            AddHtml(25, 498, 650, 20, "<CENTER><BASEFONT COLOR=#999999>Tip: Drop any completed Small or Large BOD directly onto the Master to turn it in. Right-click to Bribe.</BASEFONT></CENTER>", false, false);

            // Close Button
            AddButton(315, 524, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddLabel(350, 524, 1152, "Close");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!(sender.Mobile is PlayerMobile pm) || pm.Deleted || !pm.Alive)
                return;

            if (m_Master == null || m_Master.Deleted)
                return;

            int id = info.ButtonID;

            if (id == 0) // Close
            {
                return;
            }

            if (id == 1) // Toggle Points Mode
            {
                BODContext context = BulkOrderSystem.GetContext(pm);
                if (context != null)
                {
                    if (context.PointsMode == PointsMode.Automatic)
                        context.PointsMode = PointsMode.Enabled;
                    else if (context.PointsMode == PointsMode.Enabled)
                        context.PointsMode = PointsMode.Disabled;
                    else
                        context.PointsMode = PointsMode.Automatic;

                    pm.SendMessage(0x59, $"Bulk Order Banking Mode set to: {context.PointsMode}");
                    pm.CloseGump(typeof(BulkOrderMasterGump));
                    pm.SendGump(new BulkOrderMasterGump(m_Master, pm));
                }
                return;
            }

            if (id >= 100 && id < 100 + Trades.Length) // Claim BOD
            {
                int index = id - 100;
                TradeEntry trade = Trades[index];
                m_Master.ClaimBOD(pm, trade.Type);
                return;
            }

            if (id >= 200 && id < 200 + Trades.Length) // Open Rewards
            {
                int index = id - 200;
                TradeEntry trade = Trades[index];
                m_Master.OpenRewards(pm, trade.Type);
                return;
            }
        }
    }
}
