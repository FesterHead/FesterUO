/************************************************************************************
* Community Script: Treasure Map and SOS Storage Book - Gumps                      *
* Author: 4737Carlin (January 12, 2025)                                             *
* Source: https://www.servuo.dev/archive/treasure-map-and-sos-storage-book.2546/   *
*                                                                                  *
* Gump interfaces for TMapBook:                                                    *
* - TMapGump: Book page browsing, map inspection, dropping, pricing                *
* - TMapBuyGump: Player vendor purchasing interface                                *
* - TMapFilterGump: Multi-criteria filtering (Type, Level, Facet, Decoded)         *
************************************************************************************/

using System;
using System.Collections;
using Server.Gumps;
using Server.Mobiles;
using Server.Prompts;
using System.Collections.Generic;

namespace Server.Items
{
    public class TMapGump : Gump
    {
        private const int LabelColor = 0x7FFF;
        private readonly PlayerMobile m_From;
        private readonly TMapBook m_Book;
        private readonly ArrayList m_List;
        private int m_Page;

        private string ColorAndCenter(string color, string str)
        {
            return String.Format("<basefont color=#{0:X6}><center>{1}</center>", color, str);
        }

        public TMapGump(PlayerMobile from, TMapBook book) : this(from, book, 0, null)
        {
        }

        public TMapGump(PlayerMobile from, TMapBook book, int page, ArrayList list) : base(12, 24)
        {
            from.CloseGump(typeof(TMapGump));
            from.CloseGump(typeof(TMapFilterGump));

            m_From = from;
            m_Book = book;
            m_Page = page;

            if (list == null)
            {
                list = new ArrayList(book.Entries.Count);

                for (int i = 0; i < book.Entries.Count; ++i)
                {
                    object obj = book.Entries[i];

                    if (CheckFilter(obj))
                    {
                        list.Add(obj);
                    }
                }
            }

            m_List = list;

            int index = GetIndexForPage(page);
            int count = GetCountForIndex(index);

            int tableIndex = 0;

            PlayerVendor pv = book.RootParent as PlayerVendor;

            bool canDrop = book.IsChildOf(from.Backpack);
            bool canBuy = (pv != null);
            bool canPrice = (canDrop || canBuy);

            if (canBuy)
            {
                VendorItem vi = pv.GetVendorItem(book);

                canBuy = (vi != null && !vi.IsForSale);
            }

            int width = 600;

            if (!canPrice)
            {
                width = 516;
            }

            X = (624 - width) / 2;
            AddPage(0);
            AddBackground(10, 10, width, 439, 5054);
            AddImageTiled(18, 20, width - 17, 420, 2624);

            if (canPrice)
            {
                AddImageTiled(493, 64, 52, 352, 1416);
                AddImageTiled(547, 64, 50, 352, 200);
            }

            if (canDrop)
            {
                AddImageTiled(20, 64, 32, 352, 1416);
            }

            AddImageTiled(58, 64, 66, 352, 200);
            AddImageTiled(126, 64, 103, 352, 1416);
            AddImageTiled(231, 64, 80, 352, 200);
            AddImageTiled(313, 64, 100, 352, 1416);
            AddImageTiled(415, 64, 76, 352, 200);

            for (int i = index; i < (index + count) && i >= 0 && i < list.Count; ++i)
            {
                object obj = list[i];

                if (!CheckFilter(obj))
                {
                    continue;
                }

                AddImageTiled(24, 94 + (tableIndex * 32), canPrice ? 573 : 489, 2, 2624);

                ++tableIndex;
            }

            AddAlphaRegion(18, 20, width - 17, 420);
            AddImage(5, 5, 10460);
            AddImage(width - 15, 5, 10460);
            AddImage(5, 424, 10460);
            AddImage(width - 15, 424, 10460);
            AddHtml(10, 32, 590, 32, ColorAndCenter("FFFFFF", "Treasure Map and SOS Book"), false, false); // Type
            AddHtml(58, 64, 66, 32, ColorAndCenter("FFFFFF", "Type"), false, false); // Type
            AddHtml(126, 64, 103, 32, ColorAndCenter("FFFFFF", "Facet"), false, false);
            AddHtml(231, 64, 80, 32, ColorAndCenter("FFFFFF", "Level"), false, false);
            AddHtml(313, 64, 100, 32, ColorAndCenter("FFFFFF", "Decoded"), false, false);
            AddHtml(415, 64, 76, 32, ColorAndCenter("FFFFFF", "Status"), false, false);
            AddButton(35, 32, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(70, 32, 200, 32, 1062476, LabelColor, false, false); // Set Filter

            TMapFilter f = book.Filter;

            AddHtml(canPrice ? 470 : 386, 32, 120, 32, ColorAndCenter("0096FF", f.IsDefault ? "Unfiltered" : "Using Filter"), false, false);
            AddButton(canPrice ? 465 : 430, 416, 4017, 4018, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(canPrice ? 500 : 465, 416, 120, 20, 1011441, LabelColor, false, false); // EXIT

            if (canDrop)
            {
                AddHtmlLocalized(26, 64, 50, 32, 1062212, LabelColor, false, false); // Drop
            }

            if (canPrice)
            {
                if (canBuy)
                {
                    AddHtmlLocalized(516, 64, 200, 32, 1062218, LabelColor, false, false); // Price
                    AddHtmlLocalized(576, 64, 200, 32, 1062219, LabelColor, false, false); // Buy
                }
                else
                {
                    AddHtml(493, 64, 52, 32, ColorAndCenter("FFFFFF", "+1"), false, false);
                    AddHtml(547, 64, 50, 32, ColorAndCenter("FFFFFF", "Max"), false, false);

                    if (canDrop)
                    {
                        AddButton(70, 416, 4005, 4007, 4, GumpButtonType.Reply, 0);
                        AddHtml(95, 416, 120, 20, ColorAndCenter("FFFFFF", "Upgrade All (+1)"), false, false);
                    }
                }
            }

            tableIndex = 0;

            if (page > 0)
            {
                AddButton(canPrice ? 215 : 165, 416, 4014, 4016, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(canPrice ? 245 : 200, 416, 150, 20, 1011067, LabelColor, false, false); // Previous page
            }

            if (GetIndexForPage(page + 1) < list.Count)
            {
                AddButton(canPrice ? 340 : 300, 416, 4005, 4007, 3, GumpButtonType.Reply, 0);
                AddHtmlLocalized(canPrice ? 370 : 330, 416, 150, 20, 1011066, LabelColor, false, false); // Next page
            }

            for (int i = index; i < (index + count) && i >= 0 && i < list.Count; ++i)
            {
                object obj = list[i];

                if (!CheckFilter(obj))
                {
                    continue;
                }

                if (obj is TMapEntry)
                {
                    TMapEntry e = (TMapEntry)obj;

                    int y = 96 + (tableIndex++ * 32);

                    if (canDrop)
                    {
                        AddButton(30, y + 2, 5602, 5606, 5 + (i * 3), GumpButtonType.Reply, 0);
                    }

                    if (canBuy && e.Price > 0)
                    {
                        AddButton(579, y + 2, 2117, 2118, 6 + (i * 3), GumpButtonType.Reply, 0);
                        AddHtml(493, y, 78, 32, ColorAndCenter("FFFFFF", e.Price.ToString()), false, false); // Price
                    }
                    else if (canDrop)
                    {
                        bool canUp = !e.Completed && e.Level < TMapBook.MaxUpgradeLevel;
                        bool canMax = !e.Completed && (TMapBook.MaxUpgradeLevel - e.Level > 1);

                        if (canUp)
                        {
                            AddButton(509, y + 2, 2117, 2118, 6 + (i * 3), GumpButtonType.Reply, 0);
                        }
                        else
                        {
                            AddHtml(493, y, 52, 32, ColorAndCenter("777777", "-"), false, false);
                        }

                        if (canMax)
                        {
                            AddButton(562, y + 2, 2117, 2118, 7 + (i * 3), GumpButtonType.Reply, 0);
                        }
                        else
                        {
                            AddHtml(547, y, 50, 32, ColorAndCenter("777777", "-"), false, false);
                        }
                    }

                    AddHtml(58, y, 66, 32, ColorAndCenter("FFFFFF", "T-Map"), false, false); // Type
                    AddHtml(126, y, 103, 32, ColorAndCenter("FFFFFF", e.Facet.ToString()), false, false); //Facet
                    AddHtml(231, y, 80, 32, ColorAndCenter("FFFFFF", e.Level.ToString()), false, false); //Level
                    AddHtml(313, y, 100, 32, ColorAndCenter("FFFFFF", e.Decoder == null ? "False" : "True"), false, false); //IsDecoded
                    AddHtml(415, y, 76, 32, (e.Completed == false ? ColorAndCenter("FFFFFF", e.Decoder == null ? "-" : "Incomplete") : ColorAndCenter("FFEA00", "Complete")), false, false); //Completed
                }
                else if (obj is SOSEntry)
                {
                    SOSEntry e = (SOSEntry)obj;

                    int y = 96 + (tableIndex++ * 32);

                    if (canDrop)
                    {
                        AddButton(30, y + 2, 5602, 5606, 5 + (i * 3), GumpButtonType.Reply, 0);
                    }

                    if (canBuy && e.Price > 0)
                    {
                        AddButton(579, y + 2, 2117, 2118, 6 + (i * 3), GumpButtonType.Reply, 0);
                        AddHtml(493, y, 78, 32, ColorAndCenter("FFFFFF", e.Price.ToString()), false, false); // Price
                    }
                    else if (canDrop)
                    {
                        bool canUp = !e.IsAncient && e.Level < TMapBook.SOSMaxUpgradeLevel;
                        bool canMax = !e.IsAncient && (TMapBook.SOSMaxUpgradeLevel - e.Level > 1);

                        if (canUp)
                        {
                            AddButton(509, y + 2, 2117, 2118, 6 + (i * 3), GumpButtonType.Reply, 0);
                        }
                        else
                        {
                            AddHtml(493, y, 52, 32, ColorAndCenter("777777", "-"), false, false);
                        }

                        if (canMax)
                        {
                            AddButton(562, y + 2, 2117, 2118, 7 + (i * 3), GumpButtonType.Reply, 0);
                        }
                        else
                        {
                            AddHtml(547, y, 50, 32, ColorAndCenter("777777", "-"), false, false);
                        }
                    }

                    AddHtml(58, y, 66, 32, ColorAndCenter("FFFFFF", "SOS"), false, false); // Type
                    AddHtml(126, y, 103, 32, ColorAndCenter("FFFFFF", e.TargetMap.ToString()), false, false); //Facet
                    AddHtml(231, y, 80, 32, ColorAndCenter("FFFFFF", e.Level.ToString()), false, false); //Level
                    AddHtml(313, y, 100, 32, ColorAndCenter("FFFFFF", "-"), false, false); //Decoded
                    AddHtml(415, y, 76, 32, (e.IsAncient == false ? ColorAndCenter("FFFFFF", "Regular") : ColorAndCenter("FFEA00", "Ancient")), false, false); //Ancient
                }
            }
        }

        public Item Reconstruct(object obj)
        {
            Item item = null;

            if (obj is TMapEntry)
            {
                item = ((TMapEntry)obj).Reconstruct();
            }
            else if (obj is SOSEntry)
            {
                item = ((SOSEntry)obj).Reconstruct();
            }

            return item;
        }

        private int MapValue(Map map)
        {
            if (map == Map.Trammel)
            {
                return 1;
            }
            else if (map == Map.Ilshenar)
            {
                return 2;
            }
            else if (map == Map.Malas)
            {
                return 3;
            }
            else if (map == Map.Tokuno)
            {
                return 4;
            }
            else if (map == Map.TerMur)
            {
                return 5;
            }

            return 0;
        }

        public bool CheckFilter(object obj)
        {
            if (obj is TMapEntry tmap)
            {
                return CheckFilter(0, tmap.Level, MapValue(tmap.Facet), tmap.Decoded);
            }
            else if (obj is SOSEntry sos)
            {
                return CheckFilter(1, sos.Level, MapValue(sos.TargetMap), 1);
            }

            return false;
        }

        public bool CheckFilter(int type, int level, int facet, int decoded)
        {
            TMapFilter f = m_Book.Filter;

            if (f.Type > 0 && type != f.Type - 1)
            {
                return false;
            }

            if (f.Level > 0 && level != f.Level)
            {
                return false;
            }

            if (f.Facet > 0 && facet != f.Facet - 1)
            {
                return false;
            }

            if (f.Decoded > 0 && decoded != f.Decoded - 1)
            {
                return false;
            }

            return true;
        }

        public int GetIndexForPage(int page)
        {
            int index = 0;

            while (page-- > 0)
            {
                index += GetCountForIndex(index);
            }

            return index;
        }

        public int GetCountForIndex(int index)
        {
            int slots = 0;
            int count = 0;

            ArrayList list = m_List;

            for (int i = index; i >= 0 && i < list.Count; ++i)
            {
                object obj = list[i];

                if (CheckFilter(obj))
                {
                    if ((slots + 1) > 10)
                    {
                        break;
                    }

                    slots += 1;
                }

                ++count;
            }

            return count;
        }

        public int GetPageForIndex(int index, int sizeDropped)
        {
            if (index <= 0)
            {
                return 0;
            }

            int count = 0;
            int page = 0;

            ArrayList list = m_List;

            int i;
            object obj;

            for (i = 0; (i < index) && (i < list.Count); i++)
            {
                obj = list[i];

                if (CheckFilter(obj))
                {
                    if ((count + 1) > 10)
                    {
                        page++;

                        count = 1;
                    }
                }
            }

            i++;

            if (count + sizeDropped > 10)
            {
                while ((i < list.Count) && (count <= 10))
                {
                    obj = list[i];

                    if (CheckFilter(obj))
                    {
                        count += 1;
                    }

                    i++;
                }

                if (count > 10)
                {
                    page++;
                }
            }

            return page;
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            int index = info.ButtonID;

            switch (index)
            {
                case 0: // EXIT
                    {
                        break;
                    }
                case 1: // Set Filter
                    {
                        m_From.SendGump(new TMapFilterGump(m_From, m_Book));

                        break;
                    }
                case 2: // Previous page
                    {
                        if (m_Page > 0)
                        {
                            m_From.SendGump(new TMapGump(m_From, m_Book, m_Page - 1, m_List));
                        }

                        break;
                    }
                case 3: // Next page
                    {
                        if (GetIndexForPage(m_Page + 1) < m_List.Count)
                        {
                            m_From.SendGump(new TMapGump(m_From, m_Book, m_Page + 1, m_List));
                        }

                        break;
                    }
                case 4: // Upgrade All (+1)
                    {
                        if (m_Book.IsChildOf(m_From.Backpack))
                        {
                            UpgradeAll(m_From);
                            m_From.SendGump(new TMapGump(m_From, m_Book, m_Page, m_List));
                        }

                        break;
                    }
                default:
                    {
                        bool canDrop = m_Book.IsChildOf(m_From.Backpack);
                        bool canPrice = canDrop || (m_Book.RootParent is PlayerVendor);

                        index -= 5;

                        int action = index % 3;
                        index /= 3;

                        if (index < 0 || index >= m_List.Count)
                        {
                            break;
                        }

                        object obj = m_List[index];

                        if (!m_Book.Entries.Contains(obj))
                        {
                            m_From.SendMessage("The map selected is not available"); // The map selected is not available.

                            break;
                        }

                        if (action == 0) // Drop
                        {
                            if (m_Book.IsChildOf(m_From.Backpack))
                            {
                                Item item = Reconstruct(obj);

                                if (item != null)
                                {
                                    Container pack = m_From.Backpack;

                                    if ((pack == null) || ((pack != null) && (!pack.CheckHold(m_From, item, true, true, 0, item.PileWeight + item.TotalWeight))))
                                    {
                                        m_From.SendLocalizedMessage(503204); // You do not have room in your backpack for this

                                        m_From.SendGump(new TMapGump(m_From, m_Book, m_Page, null));
                                    }
                                    else
                                    {
                                        if (m_Book.IsChildOf(m_From.Backpack))
                                        {
                                            m_From.AddToBackpack(item);

                                            m_From.SendMessage("The map has been placed in your backpack"); // The map has been placed in your backpack.

                                            m_Book.Entries.Remove(obj);

                                            m_Book.InvalidateProperties();

                                            if (m_Book.Entries.Count / 5 < m_Book.ItemCount)
                                            {
                                                m_Book.ItemCount--;

                                                m_Book.InvalidateItems();
                                            }

                                            if (m_Book.Entries.Count > 0)
                                            {
                                                m_Page = GetPageForIndex(index, 1);

                                                m_From.SendGump(new TMapGump(m_From, m_Book, m_Page, null));
                                            }
                                            else
                                            {
                                                m_From.SendLocalizedMessage(1062381); // The book is empty.
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    m_From.SendMessage("Internal error. The map could not be reconstructed.");
                                }
                            }
                        }
                        else if (action == 1) // +1 Upgrade (or Buy if on vendor)
                        {
                            if (m_Book.IsChildOf(m_From.Backpack))
                            {
                                UpgradeEntry(m_From, obj, false);
                                m_From.SendGump(new TMapGump(m_From, m_Book, m_Page, m_List));
                            }
                            else if (m_Book.RootParent is PlayerVendor)
                            {
                                PlayerVendor pv = (PlayerVendor)m_Book.RootParent;

                                VendorItem vi = pv.GetVendorItem(m_Book);

                                if (vi != null && !vi.IsForSale)
                                {
                                    int price = 0;

                                    if (obj is TMapEntry)
                                    {
                                        price = ((TMapEntry)obj).Price;
                                    }
                                    else
                                    {
                                        price = ((SOSEntry)obj).Price;
                                    }

                                    if (price == 0)
                                    {
                                        m_From.SendMessage("The map selected is not available"); // The map selected is not available.
                                    }
                                    else
                                    {
                                        if (m_Book.Entries.Count > 0)
                                        {
                                            m_Page = GetPageForIndex(index, 1);

                                            m_From.SendGump(new TMapBuyGump(m_From, m_Book, obj, m_Page, price));
                                        }
                                        else
                                        {
                                            m_From.SendLocalizedMessage(1062381); // The book is empty.
                                        }
                                    }
                                }
                            }
                        }
                        else if (action == 2) // Max Upgrade
                        {
                            if (m_Book.IsChildOf(m_From.Backpack))
                            {
                                UpgradeEntry(m_From, obj, true);
                                m_From.SendGump(new TMapGump(m_From, m_Book, m_Page, m_List));
                            }
                        }

                        break;
                    }
            }
        }

        private void UpgradeEntry(PlayerMobile from, object obj, bool maxUpgrade)
        {
            if (from == null || !from.Alive || obj == null || !m_Book.IsChildOf(from.Backpack))
                return;

            if (obj is TMapEntry mapEntry)
            {
                if (mapEntry.Completed)
                {
                    from.SendMessage(0x22, "That treasure map has already been completed.");
                    return;
                }

                int maxLevel = TMapBook.MaxUpgradeLevel;
                if (mapEntry.Level >= maxLevel)
                {
                    from.SendMessage(0x35, "This treasure map is already at the maximum upgrade level (Level {0}).", maxLevel);
                    return;
                }

                int targetLevel = maxUpgrade ? maxLevel : mapEntry.Level + 1;
                int levelsToGain = targetLevel - mapEntry.Level;
                if (levelsToGain <= 0)
                    return;

                int cost = levelsToGain * TMapBook.UpgradeCostPerLevel;
                int totalGold = TMapBook.GetTotalGold(from);

                if (totalGold < cost)
                {
                    from.SendMessage(0x22, "You do not have enough gold. Upgrading to Level {0} requires {1:N0} gold (Available: {2:N0} gp).", targetLevel, cost, totalGold);
                    from.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x22, false, "Insufficient gold!");
                    return;
                }

                if (!TMapBook.DeductGold(from, cost))
                {
                    from.SendMessage(0x22, "Could not withdraw gold for the upgrade.");
                    return;
                }

                mapEntry.Level = targetLevel;
                if (Enum.IsDefined(typeof(TreasureLevel), targetLevel))
                {
                    mapEntry.TreasureLevel = (TreasureLevel)targetLevel;
                }

                from.PlaySound(0x2E6); // Gold coins
                from.PlaySound(0x249); // Map/scroll

                from.SendMessage(0x35, "You have upgraded the treasure map to Level {0} for {1:N0} gold.", targetLevel, cost);
                from.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x35, false, $"Upgraded map to Level {targetLevel}!");
            }
            else if (obj is SOSEntry sosEntry)
            {
                if (sosEntry.IsAncient)
                {
                    from.SendMessage(0x22, "Ancient SOS messages cannot be upgraded.");
                    return;
                }

                int maxLevel = TMapBook.SOSMaxUpgradeLevel;
                if (sosEntry.Level >= maxLevel)
                {
                    from.SendMessage(0x35, "This SOS is already at the maximum upgrade level (Level {0}). Ancient SOS messages can only be found naturally.", maxLevel);
                    return;
                }

                int targetLevel = maxUpgrade ? maxLevel : sosEntry.Level + 1;
                if (targetLevel > maxLevel)
                    targetLevel = maxLevel;

                int levelsToGain = targetLevel - sosEntry.Level;
                if (levelsToGain <= 0)
                    return;

                int cost = levelsToGain * TMapBook.SOSUpgradeCostPerLevel;
                int totalGold = TMapBook.GetTotalGold(from);

                if (totalGold < cost)
                {
                    from.SendMessage(0x22, "You do not have enough gold. Upgrading to Level {0} requires {1:N0} gold (Available: {2:N0} gp).", targetLevel, cost, totalGold);
                    from.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x22, false, "Insufficient gold!");
                    return;
                }

                if (!TMapBook.DeductGold(from, cost))
                {
                    from.SendMessage(0x22, "Could not withdraw gold for the upgrade.");
                    return;
                }

                sosEntry.Level = targetLevel;

                from.PlaySound(0x2E6); // Gold coins
                from.PlaySound(0x25);  // Water splash

                from.SendMessage(0x35, "You have upgraded the SOS to Level {0} for {1:N0} gold.", targetLevel, cost);
                from.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x35, false, $"Upgraded SOS to Level {targetLevel}!");
            }
        }

        private void UpgradeAll(PlayerMobile from)
        {
            if (from == null || !from.Alive || !m_Book.IsChildOf(from.Backpack))
                return;

            int upgradedCount = 0;
            int totalCost = 0;

            for (int i = 0; i < m_Book.Entries.Count; i++)
            {
                object obj = m_Book.Entries[i];
                if (obj is TMapEntry mapEntry && !mapEntry.Completed && mapEntry.Level < TMapBook.MaxUpgradeLevel)
                {
                    int cost = TMapBook.UpgradeCostPerLevel;
                    if (TMapBook.GetTotalGold(from) >= cost && TMapBook.DeductGold(from, cost))
                    {
                        mapEntry.Level++;
                        if (Enum.IsDefined(typeof(TreasureLevel), mapEntry.Level))
                        {
                            mapEntry.TreasureLevel = (TreasureLevel)mapEntry.Level;
                        }
                        totalCost += cost;
                        upgradedCount++;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (obj is SOSEntry sosEntry && !sosEntry.IsAncient && sosEntry.Level < TMapBook.SOSMaxUpgradeLevel)
                {
                    int cost = TMapBook.SOSUpgradeCostPerLevel;
                    if (TMapBook.GetTotalGold(from) >= cost && TMapBook.DeductGold(from, cost))
                    {
                        sosEntry.Level++;
                        totalCost += cost;
                        upgradedCount++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (upgradedCount > 0)
            {
                from.PlaySound(0x2E6);
                from.SendMessage(0x35, "Upgraded {0} item(s) in book by +1 level for {1:N0} gold.", upgradedCount, totalCost);
                from.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x35, false, $"Upgraded {upgradedCount} items (+1)!");
            }
            else
            {
                from.SendMessage(0x22, "No items in the book could be upgraded (either already max level or insufficient gold).");
            }
        }
            }
        }

        private class SetPricePrompt : Prompt
        {
            public override int MessageCliloc { get { return 1062383; } }
            private readonly TMapBook m_Book;
            private readonly object m_Object;
            private readonly int m_Page;
            private readonly ArrayList m_List;
            public SetPricePrompt(TMapBook book, object obj, int page, ArrayList list)
            {
                m_Book = book;
                m_Object = obj;
                m_Page = page;
                m_List = list;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Object != null && !m_Book.Entries.Contains(m_Object))
                {
                    from.SendMessage("The map selected is not available"); // The map selected is not available.

                    return;
                }

                int price = Utility.ToInt32(text);

                if (price < 0 || price > 250000000)
                {
                    from.SendLocalizedMessage(1062390); // The price you requested is outrageous!
                }
                else if (m_Object == null)
                {
                    for (int i = 0; i < m_List.Count; ++i)
                    {
                        object obj = m_List[i];

                        if (!m_Book.Entries.Contains(obj))
                        {
                            continue;
                        }

                        if (obj is TMapEntry)
                        {
                            ((TMapEntry)obj).Price = price;
                        }
                        else if (obj is SOSEntry)
                        {
                            ((SOSEntry)obj).Price = price;
                        }
                    }

                    from.SendMessage("Map prices set.");

                    if (from is PlayerMobile)
                    {
                        from.SendGump(new TMapGump((PlayerMobile)from, m_Book, m_Page, m_List));
                    }
                }
                else if (m_Object is TMapEntry)
                {
                    ((TMapEntry)m_Object).Price = price;

                    from.SendMessage("Map price set"); // Map price set.

                    if (from is PlayerMobile)
                    {
                        from.SendGump(new TMapGump((PlayerMobile)from, m_Book, m_Page, m_List));
                    }
                }
                else if (m_Object is SOSEntry)
                {
                    ((SOSEntry)m_Object).Price = price;

                    from.SendMessage("Map price set"); // Map price set.

                    if (from is PlayerMobile)
                    {
                        from.SendGump(new TMapGump((PlayerMobile)from, m_Book, m_Page, m_List));
                    }
                }
            }
        }
    }

    public class TMapBuyGump : Gump
    {
        private readonly PlayerMobile m_From;
        private readonly TMapBook m_Book;
        private readonly object m_Object;
        private readonly int m_Price;
        private readonly int m_Page;

        public TMapBuyGump(PlayerMobile from, TMapBook book, object obj, int page, int price)
            : base(100, 200)
        {
            this.m_From = from;
            this.m_Book = book;
            this.m_Object = obj;
            this.m_Price = price;
            this.m_Page = page;

            this.AddPage(0);

            this.AddBackground(100, 10, 300, 150, 5054);

            this.AddHtmlLocalized(125, 20, 250, 24, 1019070, false, false); // You have agreed to purchase:

            if (this.m_Object is TMapEntry tmap)
            {
                int val = 0;

                if (tmap.Decoder != null)
                {
                    if (tmap.Level == 6)
                    {
                        val = 1063453;
                    }
                    else if (tmap.Level == 7)
                    {
                        val = 1116773;
                    }
                    else
                    {
                        val = 1041516 + tmap.Level;
                    }
                }
                else if (tmap.Level == 6)
                {
                    val = 1063452;
                }
                else if (tmap.Level == 7)
                {
                    val = 1116790;
                }
                else
                {
                    val = 1041510 + tmap.Level;
                }

                this.AddHtmlLocalized(125, 45, 250, 24, val, false, false); // a treasure map

                price = ((TMapEntry)this.m_Object).Price;
            }
            else if (this.m_Object is SOSEntry sos)
            {
                if (sos.IsAncient)
                {
                    this.AddHtmlLocalized(125, 45, 250, 24, 1063450, false, false); // an ancient SOS
                }
                else
                {
                    this.AddHtmlLocalized(125, 45, 250, 24, 1041081, false, false); // a waterstained SOS
                }

                price = ((SOSEntry)this.m_Object).Price;
            }

            this.AddHtmlLocalized(125, 70, 250, 24, 1019071, false, false); // for the amount of:
            this.AddLabel(125, 95, 0, price.ToString());

            this.AddButton(250, 130, 4005, 4007, 1, GumpButtonType.Reply, 0);
            this.AddHtmlLocalized(282, 130, 100, 24, 1011012, false, false); // CANCEL

            this.AddButton(120, 130, 4005, 4007, 2, GumpButtonType.Reply, 0);
            this.AddHtmlLocalized(152, 130, 100, 24, 1011036, false, false); // OKAY
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 2)
            {
                PlayerVendor pv = this.m_Book.RootParent as PlayerVendor;

                if (this.m_Book.Entries.Contains(this.m_Object) && pv != null)
                {
                    int price = 0;

                    VendorItem vi = pv.GetVendorItem(this.m_Book);

                    if (vi != null && !vi.IsForSale)
                    {
                        if (this.m_Object is TMapEntry)
                        {
                            price = ((TMapEntry)this.m_Object).Price;
                        }
                        else if (this.m_Object is SOSEntry)
                        {
                            price = ((SOSEntry)this.m_Object).Price;
                        }
                    }

                    if (price != this.m_Price)
                    {
                        pv.SayTo(this.m_From, "The price has been been changed. If you like, you may offer to purchase the item again.");
                    }
                    else if (price == 0)
                    {
                        pv.SayTo(this.m_From, "The map selected is not available"); // The map selected is not available.
                    }
                    else
                    {
                        Item item = null;

                        if (this.m_Object is TMapEntry)
                        {
                            item = ((TMapEntry)this.m_Object).Reconstruct();
                        }
                        else if (this.m_Object is SOSEntry)
                        {
                            item = ((SOSEntry)this.m_Object).Reconstruct();
                        }

                        if (item == null)
                        {
                            this.m_From.SendMessage("Internal error. The map could not be reconstructed.");
                        }
                        else
                        {
                            pv.Say(this.m_From.Name);

                            Container pack = this.m_From.Backpack;

                            if ((pack == null) || ((pack != null) && (!pack.CheckHold(this.m_From, item, true, true, 0, item.PileWeight + item.TotalWeight))))
                            {
                                pv.SayTo(this.m_From, 503204); // You do not have room in your backpack for this

                                this.m_From.SendGump(new TMapGump(this.m_From, this.m_Book, this.m_Page, null));
                            }
                            else
                            {
                                if ((pack != null && pack.ConsumeTotal(typeof(Gold), price)) || Banker.Withdraw(this.m_From, price))
                                {
                                    this.m_Book.Entries.Remove(this.m_Object);

                                    this.m_Book.InvalidateProperties();

                                    pv.HoldGold += price;

                                    this.m_From.AddToBackpack(item);

                                    this.m_From.SendLocalizedMessage(1156843); // Amn item has been placed in your backpack.

                                    if (this.m_Book.Entries.Count / 5 < this.m_Book.ItemCount)
                                    {
                                        this.m_Book.ItemCount--;

                                        this.m_Book.InvalidateItems();
                                    }

                                    if (this.m_Book.Entries.Count > 0)
                                    {
                                        this.m_From.SendGump(new TMapGump(this.m_From, this.m_Book, this.m_Page, null));
                                    }
                                    else
                                    {
                                        this.m_From.SendLocalizedMessage(1062381); // The book is empty.
                                    }
                                }
                                else
                                {
                                    pv.SayTo(this.m_From, 503205); // You cannot afford this item.

                                    item.Delete();
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (pv == null)
                    {
                        this.m_From.SendMessage("The map selected is not available"); // The map selected is not available.
                    }
                    else
                    {
                        pv.SayTo(this.m_From, "The map selected is not available"); // The map selected is not available.
                    }
                }
            }
            else
            {
                this.m_From.SendLocalizedMessage(503207); // Cancelled purchase.
            }
        }
    }

    public class TMapFilterGump : Gump
    {
        private static readonly List<string> m_Materials = new List<string>
        {
            "--Blank--", //0

            "All", //1
            "False", //2
            "True", //3
            "--ExpansionSpare--", //4
            "--ExpansionSpare--", //5

            "Types", //6
            "Levels", //7
            "Facets", //8
            "Decoded", //9
            "--ExpansionSpare--", //10

            "T-Map", //11
            "SOS", //12
            "--ExpansionSpare--", //13
            "--ExpansionSpare--", //14
            "--ExpansionSpare--", //15

            "Level 1", //16
            "Level 2", //17
            "Level 3", //18
            "Level 4", //19
            "Level 5", //20
            "--ExpansionSpare--", //21
            "--ExpansionSpare--", //22
            "--ExpansionSpare--", //23
            "--ExpansionSpare--", //24
            "--ExpansionSpare--", //25

            "Felucca", //26
            "Trammel", //27
            "Ilshenar", //28
            "Malas", //29
            "Tokuno", //20
            "Termur", //31
        };

        private static readonly int[,] m_TypeFilters = new int[,] //to allow for expansion
        {
            {  1,  0 }, // All
            { 11,  1 }, // T-Map
            { 12,  2 }, // SOS
        };

        private static readonly int[,] m_LevelFilters = new int[,] //to allow for expansion
        {
            {  1,  0 }, // All
            { 16,  1 }, // Level 1
            { 17,  2 }, // Level 2
            { 18,  3 }, // Level 3
            { 19,  4 }, // Level 4
            { 20,  5 }, // Level 5
        };

        private static readonly int[,] m_FacetFilters = new int[,]
        {
            {  1,  0 }, // All
            { 26,  1 }, // Felucca
            { 27,  2 }, // Trammel
            { 28,  3 }, // Ilshenar
            { 29,  4 }, // Malas
            { 30,  5 }, // Tokuno

            {  0,  0 }, // --Blank--
            { 31,  6 }, // TerMur
        };

        private static readonly int[,] m_DecodedFilters = new int[,]
        {
            {   1,  0 }, // All
            {   2,  1 }, // False
            {   3,  2 }, // True
        };

        private static readonly int[][,] m_Filters = new int[][,]
        {
            m_TypeFilters,
            m_LevelFilters,
            m_FacetFilters,
            m_DecodedFilters,
        };

        private static int FacetNumber(Map map)
        {
            if (map == Map.Trammel)
                return 1;
            else if (map == Map.Ilshenar)
                return 2;
            else if (map == Map.Malas)
                return 3;
            else if (map == Map.Tokuno)
                return 4;
            else if (map == Map.TerMur)
                return 5;
            return 0;
        }

        private static readonly int[] m_XOffsets_Types = new int[] { 0, 100, 200 };
        private static readonly int[] m_XOffsets_Levels = new int[] { 0, 100, 200, 300, 400, 500 };
        private static readonly int[] m_XOffsets_Facets = new int[] { 0, 100, 200, 300, 400, 500 };
        private static readonly int[] m_XOffsets_Decoded = new int[] { 0, 100, 200 };

        private static readonly int[] m_XWidths_Small = new int[] { 75, 75, 75 };
        private static readonly int[] m_XWidths_Large = new int[] { 60, 60, 60, 60, 60, 60 };

        private const int LabelColor = 0x7FFF;
        private readonly PlayerMobile m_From;
        private readonly TMapBook m_Book;

        private string ColorAndCenter(string color, string str)
        {
            return String.Format("<basefont color=#{0:X6}><center>{1}</center>", color, str);
        }

        public TMapFilterGump(PlayerMobile from, TMapBook book) : base(12, 24)
        {
            from.CloseGump(typeof(TMapGump));
            from.CloseGump(typeof(TMapFilterGump));

            m_From = from;
            m_Book = book;

            TMapFilter f = book.Filter;

            AddPage(0);
            AddBackground(10, 10, 620, 525, 5054);
            AddImageTiled(12, 14, 614, 519, 2624);
            AddAlphaRegion(12, 14, 614, 519);
            AddImage(5, 5, 10460);
            AddImage(605, 5, 10460);
            AddImage(5, 515, 10460);
            AddImage(605, 515, 10460);
            AddHtmlLocalized(270, 32, 200, 32, 1062223, LabelColor, false, false); // Filter Preference
            AddHtml(26, 64, 50, 32, ColorAndCenter("FFFFFF", "Types"), false, false);
            AddFilterList(26, 96, m_XOffsets_Types, 40, m_TypeFilters, m_XWidths_Small, f.Type, 0);

            AddHtml(26, 160, 50, 32, ColorAndCenter("FFFFFF", "Levels"), false, false);
            AddFilterList(26, 192, m_XOffsets_Levels, 40, m_LevelFilters, m_XWidths_Large, f.Level, 1);

            AddHtml(26, 256, 50, 32, ColorAndCenter("FFFFFF", "Facets"), false, false);
            AddFilterList(26, 288, m_XOffsets_Facets, 40, m_FacetFilters, m_XWidths_Large, f.Facet, 2);

            AddHtml(26, 384, 50, 32, ColorAndCenter("FFFFFF", "Decoded"), false, false);
            AddFilterList(26, 416, m_XOffsets_Decoded, 40, m_DecodedFilters, m_XWidths_Small, f.Decoded, 3);

            AddHtmlLocalized(85, 480, 120, 32, 1062231, LabelColor, false, false); // Clear Filter
            AddButton(50, 480, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(540, 480, 50, 32, 1011046, LabelColor, false, false); // APPLY
            AddButton(505, 480, 4017, 4018, 0, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            TMapFilter f = m_Book.Filter;

            int index = info.ButtonID;

            switch (index)
            {
                case 0: // Apply
                    {
                        m_From.SendGump(new TMapGump(m_From, m_Book));

                        return;
                    }
                case 1: // Clear Filter
                    {
                        f.Clear(); break;
                    }
                default:
                    {
                        index -= 4;

                        int type = index % 4;
                        index /= 4;

                        int[][,] filter = m_Filters;

                        if (type >= 0 && type < filter.Length)
                        {
                            int[,] filters = filter[type];

                            if (index >= 0 && index < filters.GetLength(0))
                            {
                                if (filters[index, 0] == 0)
                                    break;
                                switch (type)
                                {
                                    case 0:
                                        f.Type = filters[index, 1]; break;
                                    case 1:
                                        f.Level = filters[index, 1]; break;
                                    case 2:
                                        f.Facet = filters[index, 1]; break;
                                    case 3:
                                        f.Decoded = filters[index, 1]; break;
                                }
                            }
                        }

                        break;
                    }
            }

            m_From.SendGump(new TMapFilterGump(m_From, m_Book));
        }

        private void AddFilterList(int x, int y, int[] xOffsets, int yOffset, int[,] filters, int[] xWidths, int filterValue, int filterIndex)
        {
            for (int i = 0; i < filters.GetLength(0); ++i)
            {
                int number = filters[i, 0];

                bool isSelected = (filters[i, 1] == filterValue);

                if (!isSelected && (i % xOffsets.Length) == 0)
                {
                    isSelected = (filterValue == 0);
                }

                if (number == 0 && filters[i, 1] == 0)
                {
                    continue;
                }

                if (number > 1000) //handle clilocs (there are not any but for compatability ... )
                {
                    AddHtmlLocalized(x + 35 + xOffsets[i % xOffsets.Length], y + ((i / xOffsets.Length) * yOffset), xWidths[i % xOffsets.Length], 32, number, isSelected ? 16927 : LabelColor, false, false);
                }
                else //handle list<string>
                {
                    AddHtml(x + 35 + xOffsets[i % xOffsets.Length], y + ((i / xOffsets.Length) * yOffset), xWidths[i % xOffsets.Length], 32, String.Format("<basefont color={0}>{1}</basefont>", isSelected ? "#8484FF" : "#FFFFFF", m_Materials[number]), false, false);
                }

                AddButton(x + xOffsets[i % xOffsets.Length], y + ((i / xOffsets.Length) * yOffset), 4005, 4007, 4 + filterIndex + (i * 4), GumpButtonType.Reply, 0);
            }
        }
    }
}
