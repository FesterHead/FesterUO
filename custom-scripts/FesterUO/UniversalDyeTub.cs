/*
 * Universal Highlander Dye Tub ("There can be only one!")
 * 
 * Provenance & Attribution:
 * - Visual Palette Gump & Swatch Logic: Feng / UO Wildlands Team
 *   Source: ServUO Community Archive - Customizable Rune Book and Spell Book Dye Tubs (Resource #2642)
 *   https://www.servuo.dev/archive/customizable-rune-book-and-spell-book-dye-tubs-16-preset-colors.2642/
 * - Universal Dyeing Concepts: MightyHythloth, Lord_GreyWolf, tangentzero, Triberius-Rex
 *   Source: Ultimate Hue Room Generation System / UltimateDyeTub
 *   https://github.com/Triberius-Rex/TheForging/blob/main/Scripts/Custom/UltimateDyeTub.cs
 * - Unified Implementation & Parameterization: FesterUO Private Shard
 * 
 * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 *
 * A single all-in-one universal dye tub featuring a 16-preset customizable visual palette gump
 * with multi-gradient swatch preview bars rendered from Ultima.Hues.
 * Dyes armor, weapons, clothing, leather, shields, jewelry, containers, runebooks, spellbooks,
 * ethereal mounts, and controlled pets for a configurable gold cost per use.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class UniversalDyeTub : DyeTub, IDyesTargetHandler, Engines.VeteranRewards.IRewardItem
    {
        public class Default
        {
            // Preset hues covering reds, oranges, yellows, greens, teal, blues, purples, leather/brown, black, and white
            public static readonly int[] Colors = new int[]
            {
                37, 38,   // Reds
                43, 48,   // Oranges
                53, 55,   // Yellows
                58, 67,   // Greens
                83,       // Teal
                3,  93,   // Blues
                13, 18,   // Purples
                1121,     // Runic Atlas Brown
                1,        // Black
                2050      // White
            };

            public static int Length => Colors.Length;
        }

        private bool m_IsRewardItem;
        public int[] m_CustomColors;
        private bool m_ChangeMode = false;
        private int? m_CustomCost;

        [Constructable]
        public UniversalDyeTub()
        {
            Weight = 1.0;
            LootType = LootType.Blessed;
            Name = "Universal Dye Tub";

            m_CustomColors = new int[Default.Length];
            for (int i = 0; i < Default.Length; i++)
            {
                m_CustomColors[i] = Default.Colors[i];
            }

            int hue = m_CustomColors[Utility.Random(Default.Length)];
            Hue = hue;
            DyedHue = hue;
        }

        public UniversalDyeTub(Serial serial) : base(serial)
        {
        }

        public override bool AllowDyables => true;
        public override bool AllowRunebooks => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CostPerUse
        {
            get => m_CustomCost ?? Config.Get("UniversalDyeTub.CostPerUse", 1000);
            set => m_CustomCost = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set => m_IsRewardItem = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int[] CustomColors
        {
            get => m_CustomColors;
            set => m_CustomColors = value;
        }

        public bool ChangeMode
        {
            get => m_ChangeMode;
            set => m_ChangeMode = value;
        }

        public void SetTubHue(int hue)
        {
            Hue = hue;
            DyedHue = hue;
            InvalidateProperties();
        }

        public void SetCustomColor(int index, int hue)
        {
            if (index < 0 || index >= Default.Length)
                return;

            m_CustomColors[index] = hue;
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && (IsChildOf(from.Backpack) || from.InRange(GetWorldLocation(), 3)))
            {
                list.Add(new UniversalDyeTubPaletteEntry(from, this));
            }
        }

        public class UniversalDyeTubPaletteEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly UniversalDyeTub m_Tub;

            public UniversalDyeTubPaletteEntry(Mobile from, UniversalDyeTub tub)
                : base(1151720, 10) // "Set Hue"
            {
                m_From = from;
                m_Tub = tub;
            }

            public override void OnClick()
            {
                if (m_From == null || m_Tub == null || m_Tub.Deleted)
                    return;

                m_Tub.ChangeMode = false;
                m_From.CloseGump(typeof(UniversalCustomHueGump));
                m_From.SendGump(new UniversalCustomHueGump(m_From, m_Tub));
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Set to hue {DyedHue}");
            list.Add($"Cost: {CostPerUse:#,0} gold per use");
        }

        // Called when standard Dyes are used on this tub via IDyesTargetHandler (patch 28)
        public void OnDyesUsed(Mobile from, DyeTub tub)
        {
            ChangeMode = false;
            from.CloseGump(typeof(UniversalCustomHueGump));
            from.SendGump(new UniversalCustomHueGump(from, this));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 3) || IsChildOf(from.Backpack))
            {
                from.SendMessage(68, $"Select an item, ethereal mount, or controlled pet to dye ({CostPerUse:#,0} gold).");
                from.Target = new UniversalDyeTubTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public class UniversalDyeTubTarget : Target
        {
            private readonly UniversalDyeTub m_Tub;

            public UniversalDyeTubTarget(UniversalDyeTub tub) : base(12, false, TargetFlags.None)
            {
                m_Tub = tub;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Tub == null || m_Tub.Deleted || from == null)
                    return;

                if (!from.InRange(m_Tub.GetWorldLocation(), 3) && !m_Tub.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                    return;
                }

                int cost = m_Tub.CostPerUse;
                int totalGold = 0;
                if (from.Backpack != null)
                    totalGold += from.Backpack.GetAmount(typeof(Gold));
                totalGold += Banker.GetBalance(from);

                if (cost > 0 && totalGold < cost)
                {
                    from.SendMessage(38, $"You cannot afford to dye that. It costs {cost:#,0} gold.");
                    return;
                }

                if (targeted is EtherealMount ethereal)
                {
                    if (ethereal.RootParent == from)
                    {
                        if (DeductCost(from, cost))
                        {
                            ethereal.TransparentMountedHue = m_Tub.DyedHue;
                            ethereal.Hue = m_Tub.DyedHue;
                            from.PlaySound(0x23E);
                            from.SendMessage(68, $"You dyed your ethereal mount for {cost:#,0} gold.");
                        }
                    }
                    else
                    {
                        from.SendMessage(38, "You can only dye ethereal mounts that are in your backpack or mounted!");
                    }
                }
                else if (targeted is Item item)
                {
                    if (item == m_Tub)
                    {
                        from.SendMessage(38, "You cannot dye the tub with itself. Use Dyes on it or right-click it to choose a color.");
                        return;
                    }

                    if (item.RootParent == from)
                    {
                        if (DeductCost(from, cost))
                        {
                            item.Hue = m_Tub.DyedHue;
                            from.PlaySound(0x23E);
                            string itemName = string.IsNullOrWhiteSpace(item.Name) ? "item" : item.Name;
                            from.SendMessage(68, $"You dyed the {itemName} for {cost:#,0} gold.");
                        }
                    }
                    else
                    {
                        from.SendMessage(38, "You can only dye objects that are in your backpack or on your person!");
                    }
                }
                else if (targeted is BaseCreature pet)
                {
                    if (!pet.Controlled || pet.ControlMaster != from)
                    {
                        from.SendMessage(38, "You can only dye animals whom you control!");
                        return;
                    }

                    if (!pet.InRange(from, 3))
                    {
                        from.SendMessage(38, "Your pet is too far away to dye.");
                        return;
                    }

                    if (DeductCost(from, cost))
                    {
                        pet.Hue = m_Tub.DyedHue;
                        from.PlaySound(0x23E);
                        from.SendMessage(68, $"You dyed {pet.Name} for {cost:#,0} gold.");
                    }
                }
                else
                {
                    from.SendMessage(38, "You cannot dye that.");
                }
            }

            private static bool DeductCost(Mobile from, int amount)
            {
                if (amount <= 0)
                    return true;

                int leftPrice = amount;
                if (from.Backpack != null)
                    leftPrice -= from.Backpack.ConsumeUpTo(typeof(Gold), leftPrice);

                if (leftPrice > 0)
                    Banker.Withdraw(from, leftPrice);

                return true;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write(m_IsRewardItem);
            writer.Write(m_CustomCost.HasValue);
            if (m_CustomCost.HasValue)
                writer.Write(m_CustomCost.Value);

            writer.Write(Default.Length);
            for (int i = 0; i < Default.Length; i++)
                writer.Write(m_CustomColors[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                {
                    m_IsRewardItem = reader.ReadBool();
                    bool hasCustomCost = reader.ReadBool();
                    if (hasCustomCost)
                        m_CustomCost = reader.ReadInt();
                    else
                        m_CustomCost = null;

                    int length = reader.ReadInt();
                    m_CustomColors = new int[length];
                    for (int i = 0; i < length; i++)
                        m_CustomColors[i] = reader.ReadInt();

                    break;
                }
            }
        }
    }

    public class UniversalCustomHueGump : Gump
    {
        private readonly Mobile m_From;
        private readonly UniversalDyeTub m_Tub;

        public UniversalCustomHueGump(Mobile from, UniversalDyeTub tub) : base(0, 0)
        {
            m_From = from;
            m_Tub = tub;
            int width = 625;
            int noHueButtonOffset = 380;
            int resetButtonOffset = 160;
            string customizeBtnText = tub.ChangeMode ? "Exit customization mode" : "Customize Colors";
            string gumpTitle = tub.ChangeMode ? "Select Palette Slot To Change" : "Universal Dye Tub Palette";

            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);

            AddBackground(0, 0, width, 260, 9270);
            AddImageTiled(10, 10, 605, 240, 2624); // Black inset background
            AddHtml(0, 16, width, 24, $"<BASEFONT COLOR=#F5D77F><CENTER><BIG><B>{gumpTitle}</B></BIG></CENTER></BASEFONT>", false, false);
            AddHtml(0, 36, width, 18, $"<BASEFONT COLOR=#CCCCCC><CENTER>Cost per use: {tub.CostPerUse:#,0} Gold | Active Hue: {tub.DyedHue}</CENTER></BASEFONT>", false, false);

            int index = 0;

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int hue = m_Tub.CustomColors[index];

                    int px = 25 + (x * 70);
                    int py = 58 + (y * 72);
                    int btnOffset = 5;
                    int stripeWidth = 6;
                    int pixelOffset = 32;

                    AddButton(px, py, 2328, 2329, 1000 + index, GumpButtonType.Reply, 0);

                    for (int color = 0; color < 32; color++)
                    {
                        string hueHex = GetHueHtml(hue, color);
                        AddHtml(px + btnOffset + color, py + btnOffset, 2, 50, $"<BODYBGCOLOR=#{hueHex}>", false, false);
                    }
                    string midHex = GetHueHtml(hue, 31);
                    AddHtml(px + btnOffset + pixelOffset, py + btnOffset, stripeWidth, 50, $"<BODYBGCOLOR=#{midHex}>", false, false);
                    for (int color = 0; color < 32; color++)
                    {
                        string hueHex = GetHueHtml(hue, 31 - color);
                        AddHtml(px + btnOffset + pixelOffset + stripeWidth + color, py + btnOffset, 2, 50, $"<BODYBGCOLOR=#{hueHex}>", false, false);
                    }

                    index++;
                }
            }

            AddButton(25, 210, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(60, 212, 200, 20, $"<BASEFONT COLOR=#FFFFFF>{customizeBtnText}</BASEFONT>", false, false);

            AddButton(width - noHueButtonOffset, 210, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtml(width - noHueButtonOffset + 35, 212, 160, 20, "<BASEFONT COLOR=#FFFFFF>Remove Dyed Hue</BASEFONT>", false, false);

            AddButton(width - resetButtonOffset, 210, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddHtml(width - resetButtonOffset + 35, 212, 140, 20, "<BASEFONT COLOR=#FFFFFF>Restore Default</BASEFONT>", false, false);
        }

        private static string GetHueHtml(int hue, int colorIndex = -1)
        {
            if (hue < 1)
                return "080808";

            hue -= 1; // Fix offset difference between Hues and in-game indices
            if (hue < 0 || hue >= Ultima.Hues.List.Length)
                return "080808";

            Ultima.Hue hd = Ultima.Hues.List[hue];
            if (hd == null || hd.Colors == null || hd.Colors.Length == 0)
                return "080808";

            int mid = colorIndex < 0 ? hd.Colors.Length / 2 : colorIndex;
            if (mid < 0 || mid >= hd.Colors.Length)
                return "080808";

            var c = hd.GetColor(mid);
            if (c.IsEmpty || c.A <= 0)
                return "080808";

            int rgb = c.ToArgb() & 0x00FFFFFF;
            return rgb.ToString("X6");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Tub == null || m_Tub.Deleted)
                return;

            if (info.ButtonID >= 1000 && info.ButtonID < 2000)
            {
                int index = info.ButtonID - 1000;

                if (m_Tub.ChangeMode)
                {
                    m_From.SendHuePicker(new InternalPicker(m_From, m_Tub, index));
                    return;
                }

                m_Tub.SetTubHue(m_Tub.CustomColors[index]);
                m_From.CloseGump(typeof(UniversalCustomHueGump));
                return;
            }

            switch (info.ButtonID)
            {
                case 0:
                    return;
                case 1:
                {
                    m_Tub.ChangeMode = !m_Tub.ChangeMode;
                    m_From.SendGump(new UniversalCustomHueGump(m_From, m_Tub));
                    break;
                }
                case 2:
                {
                    m_Tub.SetTubHue(0);
                    m_From.CloseGump(typeof(UniversalCustomHueGump));
                    break;
                }
                case 3:
                {
                    for (int i = 0; i < UniversalDyeTub.Default.Length; i++)
                        m_Tub.SetCustomColor(i, UniversalDyeTub.Default.Colors[i]);
                    m_From.SendGump(new UniversalCustomHueGump(m_From, m_Tub));
                    break;
                }
            }
        }

        private class InternalPicker : HuePicker
        {
            private readonly UniversalDyeTub m_Tub;
            private readonly int m_Index;
            private readonly Mobile m_From;

            public InternalPicker(Mobile from, UniversalDyeTub tub, int index)
                : base(tub.ItemID)
            {
                m_Tub = tub;
                m_Index = index;
                m_From = from;
            }

            public override void OnResponse(int hue)
            {
                if (m_Tub == null || m_Tub.Deleted)
                    return;

                m_Tub.SetCustomColor(m_Index, hue);
                m_From.SendGump(new UniversalCustomHueGump(m_From, m_Tub));
            }
        }
    }
}
