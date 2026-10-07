/************************************************************************************
* Community Script: Treasure Map Decoder (Instant Transporter)                      *
* Author: zerodowned                                                                *
* Source: https://github.com/zerodowned/Custom-Scripts-for-ServUO/tree/master/Treasure%20Map%20Decoder *
*                                                                                   *
* Quality of Life (QoL) exploration utility:                                        *
* - Opens a timed moongate directly to the chest coordinates of a Treasure Map      *
* - Checks for combat, criminal status, overloading, and jail escape prevention     *
* - Decodes undeciphered maps on use and prevents completed map travel              *
* - Unlimited use / no charges                                                      *
************************************************************************************/

using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class TreasureMapDecoder : Item
    {
        [Constructable]
        public TreasureMapDecoder() : base(0x577E) // 22398 - scroll/astrolabe graphic
        {
            Movable = true;
            Hue = 1266;
            Weight = 0.0;
            Name = "Treasure Map Instant Transporter";
            LootType = LootType.Blessed;
        }

        public TreasureMapDecoder(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Creates a gateway directly to the<br>Chest location of a treasure map");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Player)
                return;

            if (IsChildOf(from.Backpack))
            {
                UseBook(from);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public bool UseBook(Mobile m)
        {
            if (m.Criminal)
            {
                m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }
            else if (Server.Spells.SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }
            else if (Server.Misc.WeightOverloading.IsOverloaded(m))
            {
                m.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
                return false;
            }
            else if (m.Region is Server.Regions.Jail)
            {
                m.SendLocalizedMessage(1041530, "", 0x35); // You'll need a better jailbreak plan then that!
                return false;
            }
            else if (m.Spell != null)
            {
                m.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                return false;
            }
            else
            {
                m.Target = new TmapTarget(this);
                m.SendMessage("Target a Treasure Map");
                return true;
            }
        }

        public static int UpgradeCostPerLevel => Config.Get("TreasureMaps.UpgradeCostPerLevel", 10000);
        public static int MaxUpgradeLevel => Config.Get("TreasureMaps.MaxUpgradeLevel", 6);

        public static void Initialize()
        {
            EventSink.ContextMenu += OnTreasureMapContextMenu;
        }

        private static void OnTreasureMapContextMenu(ContextMenuEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || !from.Alive)
                return;

            if (e.Target is TreasureMap map && map.IsChildOf(from.Backpack) && !map.Completed)
            {
                if (map.Level < MaxUpgradeLevel)
                {
                    e.Entries.Add(new UpgradeMapContextEntry(from, map, false));

                    if (MaxUpgradeLevel - map.Level > 1)
                    {
                        e.Entries.Add(new UpgradeMapContextEntry(from, map, true));
                    }
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.CheckAlive() && IsChildOf(from.Backpack))
            {
                list.Add(new UpgradeMapDecoderTargetEntry(from, this, false));
                list.Add(new UpgradeMapDecoderTargetEntry(from, this, true));
            }
        }

        public static int GetBankGold(Mobile from)
        {
            if (from == null)
                return 0;

            return Banker.GetBalance(from);
        }

        public static int GetPackGold(Mobile from)
        {
            if (from == null || from.Backpack == null)
                return 0;

            Item[] goldItems = from.Backpack.FindItemsByType(typeof(Gold), true);
            long total = 0;

            for (int i = 0; i < goldItems.Length; ++i)
            {
                total += goldItems[i].Amount;
            }

            return (int)Math.Min(int.MaxValue, total);
        }

        public static int GetTotalGold(Mobile from)
        {
            if (from == null)
                return 0;

            long total = (long)GetBankGold(from) + (long)GetPackGold(from);
            return (int)Math.Min(int.MaxValue, total);
        }

        public static bool DeductGold(Mobile from, int amount)
        {
            if (from == null || amount <= 0)
                return false;

            int bankBal = GetBankGold(from);
            int packBal = GetPackGold(from);

            if ((long)bankBal + (long)packBal < amount)
                return false;

            int fromBank = Math.Min(amount, bankBal);
            int fromPack = amount - fromBank;

            if (fromBank > 0)
            {
                if (!Banker.Withdraw(from, fromBank, false))
                    return false;
            }

            if (fromPack > 0 && from.Backpack != null)
            {
                from.Backpack.ConsumeTotal(typeof(Gold), fromPack, true);
            }

            return true;
        }

        public static bool UpgradeMap(Mobile from, TreasureMap map, bool maxUpgrade)
        {
            if (from == null || !from.Alive || map == null || map.Deleted)
                return false;

            if (!map.IsChildOf(from.Backpack))
            {
                from.SendMessage(0x22, "The treasure map must be in your backpack to upgrade it.");
                return false;
            }

            if (map.Completed)
            {
                from.SendMessage(0x22, "That treasure map has already been completed.");
                return false;
            }

            if (map.Level >= MaxUpgradeLevel)
            {
                from.SendMessage(0x35, "This treasure map is already at the maximum upgrade level (Level {0}).", MaxUpgradeLevel);
                return false;
            }

            int targetLevel = maxUpgrade ? MaxUpgradeLevel : map.Level + 1;
            int levelsToGain = targetLevel - map.Level;
            if (levelsToGain <= 0)
                return false;

            int cost = levelsToGain * UpgradeCostPerLevel;
            int totalGold = GetTotalGold(from);

            if (totalGold < cost)
            {
                from.SendMessage(0x22, "You do not have enough gold. Upgrading to Level {0} requires {1:N0} gold (Available: {2:N0} gp).", targetLevel, cost, totalGold);
                from.LocalOverheadMessage(MessageType.Regular, 0x22, false, "Insufficient gold!");
                return false;
            }

            if (!DeductGold(from, cost))
            {
                from.SendMessage(0x22, "Could not withdraw gold for the upgrade.");
                return false;
            }

            map.Level = targetLevel;

            from.PlaySound(0x2E6); // Gold coins
            from.PlaySound(0x249); // Map/scroll

            from.SendMessage(0x35, "You have upgraded the treasure map to Level {0} for {1:N0} gold.", targetLevel, cost);
            from.LocalOverheadMessage(MessageType.Regular, 0x35, false, $"Upgraded map to Level {targetLevel}!");

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class UpgradeMapContextEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly TreasureMap m_Map;
        private readonly bool m_MaxUpgrade;

        public UpgradeMapContextEntry(Mobile from, TreasureMap map, bool maxUpgrade)
            : base(maxUpgrade ? 1062483 : 1072238) // 1072238 = Upgrade, 1062483 = Promote
        {
            m_From = from;
            m_Map = map;
            m_MaxUpgrade = maxUpgrade;
        }

        public override void OnClick()
        {
            TreasureMapDecoder.UpgradeMap(m_From, m_Map, m_MaxUpgrade);
        }
    }

    public class UpgradeMapDecoderTargetEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly TreasureMapDecoder m_Decoder;
        private readonly bool m_MaxUpgrade;

        public UpgradeMapDecoderTargetEntry(Mobile from, TreasureMapDecoder decoder, bool maxUpgrade)
            : base(maxUpgrade ? 1062483 : 1072238) // 1072238 = Upgrade, 1062483 = Promote
        {
            m_From = from;
            m_Decoder = decoder;
            m_MaxUpgrade = maxUpgrade;
        }

        public override void OnClick()
        {
            if (m_From == null || !m_From.Alive || m_Decoder == null || m_Decoder.Deleted)
                return;

            m_From.Target = new UpgradeMapTarget(m_MaxUpgrade);
            if (m_MaxUpgrade)
            {
                m_From.SendMessage("Target a Treasure Map to upgrade to maximum level (10,000 gp per level).");
            }
            else
            {
                m_From.SendMessage("Target a Treasure Map to upgrade +1 level (10,000 gp).");
            }
        }
    }

    public class UpgradeMapTarget : Target
    {
        private readonly bool m_MaxUpgrade;

        public UpgradeMapTarget(bool maxUpgrade) : base(1, false, TargetFlags.None)
        {
            m_MaxUpgrade = maxUpgrade;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is TreasureMap map)
            {
                TreasureMapDecoder.UpgradeMap(from, map, m_MaxUpgrade);
            }
            else
            {
                from.SendMessage(0x22, "You can only upgrade Treasure Maps!");
            }
        }
    }

    public class TmapTarget : Target
    {
        private readonly TreasureMapDecoder _mapDecoder;

        public TmapTarget(TreasureMapDecoder mapDecoder) : base(1, false, TargetFlags.None)
        {
            _mapDecoder = mapDecoder;
        }

        protected override void OnTarget(Mobile from, object target)
        {
            if (target is TreasureMap ts)
            {
                if (ts.Deleted)
                    return;

                if (ts.RootParent != from)
                {
                    from.SendMessage("The treasure map must be in your possession.");
                    return;
                }

                if (ts.Decoder != null && ts.Decoder != from && !Config.Get("TreasureMaps.DecodedMapsOpenToAll", true))
                {
                    from.SendMessage("Someone else has already deciphered this map!");
                    return;
                }

                if (ts.Completed)
                {
                    from.SendMessage("That map has already been completed.");
                    return;
                }

                if (ts.Facet == null || ts.Facet == Map.Internal || from.Map == null || from.Map == Map.Internal)
                {
                    from.SendMessage("That map cannot be deciphered here.");
                    return;
                }

                TmapBookMoongate gate = new TmapBookMoongate();
                gate.TargetMap = ts.Facet;

                if (ts.Decoder == null)
                {
                    ts.Decoder = from;
                }

                int z = gate.TargetMap.GetAverageZ(ts.ChestLocation.X, ts.ChestLocation.Y);
                Point3D p = new Point3D(ts.ChestLocation.X, ts.ChestLocation.Y, z);

                gate.Target = p;
                gate.MoveToWorld(new Point3D(from.Location), from.Map);
            }
            else
            {
                from.SendMessage("You can only use this on Treasure Maps!");
            }
        }
    }

    public class TmapBookMoongate : Moongate
    {
        public override bool ShowFeluccaWarning => false;

        [Constructable]
        public TmapBookMoongate() : base()
        {
            InternalTimer t = new InternalTimer(this);
            t.Start();
        }

        private class InternalTimer : Timer
        {
            private readonly Item m_Item;

            public InternalTimer(Item item)
                : base(TimeSpan.FromSeconds(30.0))
            {
                Priority = TimerPriority.OneSecond;
                m_Item = item;
            }

            protected override void OnTick()
            {
                m_Item.Delete();
            }
        }

        public override void OnGateUsed(Mobile m)
        {
            base.OnGateUsed(m);

            Delete();
        }

        public TmapBookMoongate(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            Delete();
        }
    }
}
