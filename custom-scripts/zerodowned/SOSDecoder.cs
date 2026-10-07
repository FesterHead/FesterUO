/************************************************************************************
* Community Script: SOS Decoder (Instant Transporter)                               *
* Author: zerodowned                                                                *
* Source: https://github.com/zerodowned/Custom-Scripts-for-ServUO/blob/master/SOS%20Decoder/SOSDecoder.cs *
*                                                                                   *
* Quality of Life (QoL) seafaring utility:                                          *
* - Instantly transports a player's boat to the open water coordinates of an SOS    *
* - Checks for combat, criminal status, overloading, and jail escape prevention     *
* - Unlimited use / no charges                                                      *
************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class SOSDecoder : Item
    {
        [Constructable]
        public SOSDecoder() : base(0x577E) // 22398 - scroll/astrolabe graphic
        {
            Movable = true;
            Hue = 1366;
            Weight = 0.0;
            Name = "SOS Instant Transporter";
            LootType = LootType.Blessed;
        }

        public SOSDecoder(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Creates a gateway directly to the<br>location of a SOS");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Player)
                return;

            bool hasWaterRing = from.Ring != null && from.Ring.GetType().Name == "WaterRing";

            if (hasWaterRing)
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    UseBook(from, false);
                    return;
                }
                else
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                }
            }
            else
            {
                BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);

                if (boat == null)
                {
                    from.SendMessage("You must be on a boat before using this.");
                    return;
                }
                else if (boat.IsMoving)
                {
                    from.SendMessage("Stop the boat before using this.");
                    return;
                }

                if (from.InRange(GetWorldLocation(), 1))
                    UseBook(from, true);
                else
                    from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public bool UseBook(Mobile m, bool hasBoat)
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
                m.Target = new SOSTarget(this, hasBoat);
                m.SendMessage("Target an SOS to calculate coordinates.");
                return true;
            }
        }

        public static int UpgradeCostPerLevel => Config.Get("TreasureMaps.SOSUpgradeCostPerLevel", Config.Get("SOS.UpgradeCostPerLevel", 10000));
        public static int MaxUpgradeLevel => Math.Min(3, Config.Get("TreasureMaps.SOSMaxUpgradeLevel", Config.Get("SOS.MaxUpgradeLevel", 3)));

        public static void Initialize()
        {
            EventSink.ContextMenu += OnSOSContextMenu;
        }

        private static void OnSOSContextMenu(ContextMenuEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || !from.Alive)
                return;

            if (e.Target is SOS sos && sos.IsChildOf(from.Backpack) && !sos.IsAncient)
            {
                if (sos.Level < MaxUpgradeLevel)
                {
                    e.Entries.Add(new UpgradeSOSContextEntry(from, sos, false));

                    if (MaxUpgradeLevel - sos.Level > 1)
                    {
                        e.Entries.Add(new UpgradeSOSContextEntry(from, sos, true));
                    }
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.CheckAlive() && IsChildOf(from.Backpack))
            {
                list.Add(new UpgradeSOSDecoderTargetEntry(from, this, false));
                list.Add(new UpgradeSOSDecoderTargetEntry(from, this, true));
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

        public static bool UpgradeSOS(Mobile from, SOS sos, bool maxUpgrade)
        {
            if (from == null || !from.Alive || sos == null || sos.Deleted)
                return false;

            if (!sos.IsChildOf(from.Backpack))
            {
                from.SendMessage(0x22, "The SOS must be in your backpack to upgrade it.");
                return false;
            }

            if (sos.IsAncient)
            {
                from.SendMessage(0x22, "Ancient SOS messages cannot be upgraded.");
                return false;
            }

            if (sos.Level >= MaxUpgradeLevel)
            {
                from.SendMessage(0x35, "This SOS is already at the maximum upgrade level (Level {0}). Ancient SOS messages can only be found naturally.", MaxUpgradeLevel);
                return false;
            }

            int targetLevel = maxUpgrade ? MaxUpgradeLevel : sos.Level + 1;
            if (targetLevel > MaxUpgradeLevel)
                targetLevel = MaxUpgradeLevel;

            int levelsToGain = targetLevel - sos.Level;
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

            sos.Level = targetLevel;

            from.PlaySound(0x2E6); // Gold coins
            from.PlaySound(0x25);  // Water sound

            from.SendMessage(0x35, "You have upgraded the SOS to Level {0} for {1:N0} gold.", targetLevel, cost);
            from.LocalOverheadMessage(MessageType.Regular, 0x35, false, $"Upgraded SOS to Level {targetLevel}!");

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

    public class UpgradeSOSContextEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly SOS m_SOS;
        private readonly bool m_MaxUpgrade;

        public UpgradeSOSContextEntry(Mobile from, SOS sos, bool maxUpgrade)
            : base(maxUpgrade ? 1062483 : 1072238) // 1072238 = Upgrade, 1062483 = Promote
        {
            m_From = from;
            m_SOS = sos;
            m_MaxUpgrade = maxUpgrade;
        }

        public override void OnClick()
        {
            SOSDecoder.UpgradeSOS(m_From, m_SOS, m_MaxUpgrade);
        }
    }

    public class UpgradeSOSDecoderTargetEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly SOSDecoder m_Decoder;
        private readonly bool m_MaxUpgrade;

        public UpgradeSOSDecoderTargetEntry(Mobile from, SOSDecoder decoder, bool maxUpgrade)
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

            m_From.Target = new UpgradeSOSTarget(m_MaxUpgrade);
            if (m_MaxUpgrade)
            {
                m_From.SendMessage("Target an SOS to upgrade to maximum level (Level 3) for 10,000 gp per level.");
            }
            else
            {
                m_From.SendMessage("Target an SOS to upgrade +1 level (10,000 gp).");
            }
        }
    }

    public class UpgradeSOSTarget : Target
    {
        private readonly bool m_MaxUpgrade;

        public UpgradeSOSTarget(bool maxUpgrade) : base(1, false, TargetFlags.None)
        {
            m_MaxUpgrade = maxUpgrade;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is SOS sos)
            {
                SOSDecoder.UpgradeSOS(from, sos, m_MaxUpgrade);
            }
            else
            {
                from.SendMessage(0x22, "You can only upgrade SOS messages!");
            }
        }
    }

    public class SOSTarget : Target
    {
        private readonly SOSDecoder _sos;
        private readonly bool HasBoat;

        public SOSTarget(SOSDecoder sos, bool hasBoat) : base(1, false, TargetFlags.None)
        {
            _sos = sos;
            HasBoat = hasBoat;
        }

        protected override void OnTarget(Mobile from, object target)
        {
            if (target is SOS sos)
            {
                if (sos.Deleted || sos.RootParent != from)
                {
                    from.SendMessage("The SOS must be in your possession.");
                    return;
                }

                Map map = sos.TargetMap;
                if (map == null || map == Map.Internal)
                {
                    from.SendMessage("The SOS does not have a valid destination.");
                    return;
                }

                if (from.Map != map)
                {
                    from.SendMessage(string.Format("That SOS destination is in another facet ({0}).", map.Name));
                    return;
                }

                bool ring = from.Ring != null && from.Ring.GetType().Name == "WaterRing";
                BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);

                if (boat == null && !ring)
                {
                    from.SendMessage("You must be on a boat before using this.");
                    return;
                }
                else if (!ring && boat != null && boat.IsMoving)
                {
                    from.SendMessage("Stop the boat before using this.");
                    return;
                }

                if (!ring && boat != null)
                {
                    bool teleported = false;
                    for (int i = 0; i < 5; i++) // Try 5 times
                    {
                        int x = Utility.Random(sos.TargetLocation.X, 20);
                        int y = Utility.Random(sos.TargetLocation.Y, 20);
                        int z = map.GetAverageZ(x, y);

                        Point3D dest = new Point3D(x, y, z);

                        if (boat.CanFit(dest, map, boat.ItemID))
                        {
                            int xOffset = x - boat.X;
                            int yOffset = y - boat.Y;
                            int zOffset = z - boat.Z;

                            boat.Teleport(xOffset, yOffset, zOffset);

                            if (boat.Facing == Direction.North || boat.Facing == Direction.South)
                            {
                                Point3D pLeft = new Point3D(boat.X - 3, boat.Y, 1);
                                Effects.SendLocationEffect(pLeft, boat.Map, 8104, 20, 10);

                                Point3D pRight = new Point3D(boat.X + 3, boat.Y, 1);
                                Effects.SendLocationEffect(pRight, boat.Map, 8109, 20, 10);
                            }

                            if (boat.Facing == Direction.East || boat.Facing == Direction.West)
                            {
                                Point3D pLeft = new Point3D(boat.X, boat.Y - 3, 1);
                                Effects.SendLocationEffect(pLeft, boat.Map, 8099, 20, 10);

                                Point3D pRight = new Point3D(boat.X, boat.Y + 3, 1);
                                Effects.SendLocationEffect(pRight, boat.Map, 8114, 20, 10);
                            }

                            from.SendMessage(68, "Your boat surges through the tides directly to the SOS waters!");
                            teleported = true;
                            break;
                        }
                    }

                    if (!teleported)
                    {
                        from.SendMessage(38, "Could not find a clear water location near the SOS. Please try again.");
                        return;
                    }
                }
                else
                {
                    from.MoveToWorld(new Point3D(sos.TargetLocation.X, sos.TargetLocation.Y, map.GetAverageZ(sos.TargetLocation.X, sos.TargetLocation.Y)), map);
                }
            }
            else
            {
                from.SendMessage("You can only use this on an SOS!");
            }
        }
    }
}
