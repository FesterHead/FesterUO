/*
 * High-Capacity Tabletop Holding Box & Deed
 * 
 * Provenance & Attribution:
 * - Design Concept: FesterUO Private Shard
 * - Storage & Decorator Mechanics: ServUO Container / IChopable / Addon architecture
 * 
 * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 *
 * A specialized non-movable tabletop storage box designed to hold large volumes
 * of unsorted loot (e.g. SOS and Treasure Map chests) without consuming house
 * lockdowns or secure storage quotas. Features tabletop auto-elevation, stacking
 * on top of existing containers, built-in height adjustment (Raise/Lower/Turn),
 * and furniture / universal dye tub compatibility.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Custom
{
    [Furniture]
    public class HoldingBox : Container, IChopable, IFlipable
    {
        public static readonly int[] ValidHues = new int[] { 0xE7D, 0x9A8, 0xE80 };

        public override int DefaultMaxItems => Config.Get("HoldingBox.MaxItems", 1000);
        public override int DefaultMaxWeight => Config.Get("HoldingBox.MaxWeight", 0);
        public override bool IsDecoContainer => false;

        [Constructable]
        public HoldingBox() : this(Config.Get("HoldingBox.DefaultHue", 1175))
        {
        }

        [Constructable]
        public HoldingBox(int hue) : base(0xE7D)
        {
            Name = "a holding box";
            Hue = hue;
            Weight = 4.0;
            Movable = false;
        }

        public HoldingBox(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && !house.IsFriend(from) && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage(38, "You do not have permission to open this holding box.");
                return;
            }

            base.OnDoubleClick(from);
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (m == null || item == null)
                return false;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && !house.IsFriend(m) && m.AccessLevel < AccessLevel.GameMaster)
            {
                if (message)
                    m.SendMessage(38, "You do not have permission to place items in this holding box.");
                return false;
            }

            int maxItems = MaxItems;
            if (checkItems && maxItems != 0 && (TotalItems + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems)
            {
                if (message)
                    SendFullItemsMessage(m, item);
                return false;
            }

            int maxWeight = MaxWeight;
            if (maxWeight != 0 && (TotalWeight + plusWeight + item.TotalWeight + item.PileWeight) > maxWeight)
            {
                if (message)
                    SendFullWeightMessage(m, item);
                return false;
            }

            return true;
        }

        public virtual bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted || from == null || sender == null)
                return false;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && !house.IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage(38, "You must be the house owner or co-owner to dye this holding box.");
                return false;
            }

            Hue = sender.DyedHue;
            from.PlaySound(0x23E);
            from.SendMessage(68, "You dyed the holding box.");
            return true;
        }

        public virtual void OnFlip(Mobile from)
        {
            if (ItemID == 0xE7D)
                ItemID = 0x9A8;
            else if (ItemID == 0x9A8)
                ItemID = 0xE80;
            else
                ItemID = 0xE7D;

            from?.PlaySound(0x3E4);
        }

        public void OnChop(Mobile from)
        {
            if (from == null)
                return;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house == null || (!house.IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster))
            {
                from.SendMessage(38, "You must be the house owner or co-owner to dismantle this holding box.");
                return;
            }

            if (Items.Count > 0)
            {
                from.SendMessage(38, "You cannot dismantle the holding box while items are inside!");
                return;
            }

            Dismantle(from);
        }

        public void Dismantle(Mobile from)
        {
            HoldingBoxDeed deed = new HoldingBoxDeed(Hue);

            if (from.Backpack == null || !from.Backpack.TryDropItem(from, deed, false))
                deed.MoveToWorld(Location, Map);

            from.PlaySound(0x3B3);
            from.SendMessage(68, "You have dismantled the holding box into a deed.");
            Delete();
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (from != null && from.Alive && house != null && (house.IsCoOwner(from) || from.AccessLevel >= AccessLevel.GameMaster))
            {
                list.Add(new RaiseEntry(this, from));
                list.Add(new LowerEntry(this, from));
                list.Add(new TurnEntry(this, from));
                list.Add(new PackUpEntry(this, from));
            }
        }

        private class RaiseEntry : ContextMenuEntry
        {
            private readonly HoldingBox m_Box;
            private readonly Mobile m_From;

            public RaiseEntry(HoldingBox box, Mobile from) : base(1018324, 2) // Up
            {
                m_Box = box;
                m_From = from;
            }

            public override void OnClick()
            {
                if (m_Box == null || m_Box.Deleted || m_From == null)
                    return;

                BaseHouse house = BaseHouse.FindHouseAt(m_Box);
                if (house == null || (!house.IsCoOwner(m_From) && m_From.AccessLevel < AccessLevel.GameMaster))
                    return;

                m_Box.Z += 1;
                m_From.PlaySound(0x3E4);
                m_From.SendMessage(68, "You raise the holding box.");
            }
        }

        private class LowerEntry : ContextMenuEntry
        {
            private readonly HoldingBox m_Box;
            private readonly Mobile m_From;

            public LowerEntry(HoldingBox box, Mobile from) : base(1018325, 2) // Down
            {
                m_Box = box;
                m_From = from;
            }

            public override void OnClick()
            {
                if (m_Box == null || m_Box.Deleted || m_From == null)
                    return;

                BaseHouse house = BaseHouse.FindHouseAt(m_Box);
                if (house == null || (!house.IsCoOwner(m_From) && m_From.AccessLevel < AccessLevel.GameMaster))
                    return;

                if (m_Box.Z > house.Z)
                {
                    m_Box.Z -= 1;
                    m_From.PlaySound(0x3E4);
                    m_From.SendMessage(68, "You lower the holding box.");
                }
                else
                {
                    m_From.SendMessage(38, "The holding box cannot be lowered any further.");
                }
            }
        }

        private class TurnEntry : ContextMenuEntry
        {
            private readonly HoldingBox m_Box;
            private readonly Mobile m_From;

            public TurnEntry(HoldingBox box, Mobile from) : base(1018323, 2) // Turn
            {
                m_Box = box;
                m_From = from;
            }

            public override void OnClick()
            {
                if (m_Box == null || m_Box.Deleted || m_From == null)
                    return;

                BaseHouse house = BaseHouse.FindHouseAt(m_Box);
                if (house == null || (!house.IsCoOwner(m_From) && m_From.AccessLevel < AccessLevel.GameMaster))
                    return;

                m_Box.OnFlip(m_From);
            }
        }

        private class PackUpEntry : ContextMenuEntry
        {
            private readonly HoldingBox m_Box;
            private readonly Mobile m_From;

            public PackUpEntry(HoldingBox box, Mobile from) : base(1151601, 2) // Re-deed
            {
                m_Box = box;
                m_From = from;
            }

            public override void OnClick()
            {
                if (m_Box == null || m_Box.Deleted || m_From == null)
                    return;

                m_Box.OnChop(m_From);
            }
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

    public class HoldingBoxDeed : Item
    {
        [Constructable]
        public HoldingBoxDeed() : this(Config.Get("HoldingBox.DefaultHue", 1175))
        {
        }

        [Constructable]
        public HoldingBoxDeed(int hue) : base(0x14F0)
        {
            Name = "a holding box deed";
            Hue = hue;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public HoldingBoxDeed(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !IsChildOf(from.Backpack))
            {
                from?.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            BaseHouse house = BaseHouse.FindHouseAt(from);

            if (house == null || (!house.IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster))
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
                return;
            }

            from.SendMessage(68, "Target a floor tile, table, or existing holding box to place it.");
            from.Target = new HoldingBoxPlacementTarget(this);
        }

        private class HoldingBoxPlacementTarget : Target
        {
            private readonly HoldingBoxDeed m_Deed;

            public HoldingBoxPlacementTarget(HoldingBoxDeed deed) : base(3, true, TargetFlags.None)
            {
                m_Deed = deed;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Deed == null || m_Deed.Deleted || from == null)
                    return;

                if (!m_Deed.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    return;
                }

                IPoint3D p = targeted as IPoint3D;
                if (p == null)
                    return;

                Point3D loc;
                Map map = from.Map;

                if (targeted is Item item)
                {
                    int topZ = item.Z + item.ItemData.Height;
                    loc = new Point3D(item.X, item.Y, topZ);
                }
                else
                {
                    loc = new Point3D(p.X, p.Y, p.Z);
                }

                BaseHouse house = BaseHouse.FindHouseAt(loc, map, 16);

                if (house == null || (!house.IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster))
                {
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                    return;
                }

                HoldingBox box = new HoldingBox(m_Deed.Hue);
                box.MoveToWorld(loc, map);

                m_Deed.Delete();

                from.PlaySound(0x42);
                from.SendMessage(68, "You have placed the holding box.");
            }
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
}
