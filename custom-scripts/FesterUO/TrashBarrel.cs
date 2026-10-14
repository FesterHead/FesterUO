/*
 * Multi-Floor Trash Barrel & Craftable Deed System
 * 
 * Provenance & Attribution:
 * - Design Concept: FesterUO Private Shard
 * - Core Container & CUB Mechanics: ServUO BaseTrash / TrashBarrel architecture
 * 
 * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 *
 * Provides a craftable deed enabling placement of multiple functional trash barrels
 * across different house floors, bypassing the legacy single-barrel restriction in
 * BaseHouse. Supports full Clean Up Britannia (CUB) points, 3-minute auto-emptying,
 * pack-up/chopping deed recovery, and furniture/universal dye tub customization.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Custom
{
    [Furniture]
    public class HouseTrashBarrel : TrashBarrel, IChopable
    {
        [Constructable]
        public HouseTrashBarrel() : base()
        {
            Movable = false;
        }

        public HouseTrashBarrel(Serial serial) : base(serial)
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
                from.SendMessage(38, "You do not have permission to access this trash barrel.");
                return;
            }

            base.OnDoubleClick(from);
        }

        public new void OnChop(Mobile from)
        {
            if (from == null)
                return;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house == null || (!house.IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster))
            {
                from.SendMessage(38, "You must be the house owner or co-owner to dismantle this trash barrel.");
                return;
            }

            if (Items.Count > 0)
            {
                from.SendMessage(38, "You cannot dismantle the trash barrel while items are inside!");
                return;
            }

            Dismantle(from);
        }

        void IChopable.OnChop(Mobile from)
        {
            OnChop(from);
        }

        public void Dismantle(Mobile from)
        {
            if (Config.Get("TrashBarrel.ChopReturnsDeed", true))
            {
                TrashBarrelDeed deed = new TrashBarrelDeed();

                if (from.Backpack == null || !from.Backpack.TryDropItem(from, deed, false))
                    deed.MoveToWorld(Location, Map);

                from.PlaySound(0x3B3);
                from.SendMessage(68, "You have dismantled the trash barrel into a deed.");
            }
            else
            {
                Effects.PlaySound(Location, Map, 0x3B3);
                from.SendLocalizedMessage(500461); // You destroy the item.
            }

            Destroy();
        }

        public virtual bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted || from == null || sender == null)
                return false;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && !house.IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage(38, "You must be the house owner or co-owner to dye this trash barrel.");
                return false;
            }

            Hue = sender.DyedHue;
            from.PlaySound(0x23E);
            from.SendMessage(68, "You dyed the trash barrel.");
            return true;
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (from != null && from.Alive && house != null && (house.IsCoOwner(from) || from.AccessLevel >= AccessLevel.GameMaster))
            {
                list.Add(new PackUpEntry(this, from));
            }
        }

        private class PackUpEntry : ContextMenuEntry
        {
            private readonly HouseTrashBarrel m_Barrel;
            private readonly Mobile m_From;

            public PackUpEntry(HouseTrashBarrel barrel, Mobile from) : base(6173, 2) // Pack Up
            {
                m_Barrel = barrel;
                m_From = from;
            }

            public override void OnClick()
            {
                if (m_Barrel == null || m_Barrel.Deleted || m_From == null)
                    return;

                m_Barrel.OnChop(m_From);
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
            reader.ReadInt();
        }
    }

    public class TrashBarrelDeed : Item
    {
        [Constructable]
        public TrashBarrelDeed() : base(0x14F0)
        {
            Name = "a trash barrel deed";
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public TrashBarrelDeed(Serial serial) : base(serial)
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

            from.SendMessage(68, "Target where you wish to place the trash barrel.");
            from.Target = new TrashBarrelPlacementTarget(this);
        }

        private class TrashBarrelPlacementTarget : Target
        {
            private readonly TrashBarrelDeed m_Deed;

            public TrashBarrelPlacementTarget(TrashBarrelDeed deed) : base(3, true, TargetFlags.None)
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

                Point3D loc = new Point3D(p.X, p.Y, p.Z);
                Map map = from.Map;

                BaseHouse house = BaseHouse.FindHouseAt(loc, map, 16);

                if (house == null || (!house.IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster))
                {
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                    return;
                }

                for (int i = 0; house.Doors != null && i < house.Doors.Count; ++i)
                {
                    if (house.Doors[i] is BaseDoor door)
                    {
                        Point3D dp = door.Location;

                        if (door.Open)
                            dp = new Point3D(dp.X - door.Offset.X, dp.Y - door.Offset.Y, dp.Z - door.Offset.Z);

                        if ((loc.Z + 16) >= dp.Z && (dp.Z + 16) >= loc.Z && Utility.InRange(loc, dp, 1))
                        {
                            from.SendLocalizedMessage(502120); // You cannot place a trash barrel near a door or near steps.
                            return;
                        }
                    }
                }

                HouseTrashBarrel barrel = new HouseTrashBarrel();
                barrel.MoveToWorld(loc, map);

                m_Deed.Delete();

                from.PlaySound(0x42);
                from.SendLocalizedMessage(502121); /* You have a new trash barrel.
                * Three minutes after you put something in the barrel, the trash will be emptied.
                * Be forewarned, this is permanent! */
            }
        }

        [CallPriority(100)]
        public static void Initialize()
        {
            if (!Config.Get("TrashBarrel.CraftingEnabled", true))
                return;

            CraftSystem craft = DefCarpentry.CraftSystem;
            if (craft == null)
                return;

            double minSkill = Config.Get("TrashBarrel.MinSkill", 65.0);
            double maxSkill = Config.Get("TrashBarrel.MaxSkill", 90.0);
            int oakBoards = Config.Get("TrashBarrel.OakBoards", 30);
            int shadowIronIngots = Config.Get("TrashBarrel.ShadowIronIngots", 15);
            int sulfurousAsh = Config.Get("TrashBarrel.SulfurousAsh", 5);

            int index = craft.AddCraft(
                typeof(TrashBarrelDeed),
                1044292, // Containers category
                "trash barrel deed",
                minSkill,
                maxSkill,
                typeof(OakBoard),
                1072644, // Oak Board
                oakBoards,
                1072652  // You do not have enough oak wood.
            );

            craft.AddRes(index, typeof(ShadowIronIngot), 1044024, shadowIronIngots, 1044268); // Shadow Iron Ingot
            craft.AddRes(index, typeof(SulfurousAsh), 1044355, sulfurousAsh, 1044363);         // Sulfurous Ash

            craft.ForceNonExceptional(index);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
