/************************************************************************************
 * Script: SayGump.cs                                                               *
 * Origin: FesterUO Custom Scripts (servuo/custom-scripts/FesterUO/)                *
 * Purpose: Compact, universal quick-speech "Say" gump for boat navigation and      *
 *          house management commands.                                              *
 *                                                                                  *
 * Commands: [say, [tillerman, [boat, [house                                        *
 * Features:                                                                        *
 * - Universal access: Opens anywhere across all facets without vessel restrictions *
 * - House section: "Lock Down", "Secure", "Release", "Unsecure", "Trash Barrel",   *
 *   "Ban", and "Eject" speech commands with native targeting keyword integration   *
 * - Boat section: Continuous sail, one-tile nudges, turns, and emergency halt      *
 * - ~50% smaller footprint (155x185 vs 260x225) with intuitive tab switching       *
 * - Seamless re-display upon button press for uninterrupted navigation/decorating  *
 ************************************************************************************/

using System;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Custom
{
    public class SayGump : Gump
    {
        public enum SayTab
        {
            House,
            Boat
        }

        public enum Buttons
        {
            Close = 0,
            TabHouse = 1,
            TabBoat = 2,

            // House commands
            LockDown = 10,
            Secure = 11,
            Release = 12,
            Unsecure = 13,
            TrashBarrel = 14,
            Ban = 15,
            Eject = 16,

            // Boat commands
            Forward = 20,
            Back = 21,
            Right = 22,
            Left = 23,
            ForwardOne = 24,
            BackOne = 25,
            RightOne = 26,
            LeftOne = 27,
            Stop = 28,
            TurnAround = 29,
            TurnLeft = 30,
            TurnRight = 31
        }

        private readonly SayTab m_Tab;
        private readonly BaseBoat m_Boat;

        public static void Initialize()
        {
            CommandSystem.Register("say", AccessLevel.Player, new CommandEventHandler(Say_OnCommand));
            CommandSystem.Register("tillerman", AccessLevel.Player, new CommandEventHandler(Tillerman_OnCommand));
            CommandSystem.Register("boat", AccessLevel.Player, new CommandEventHandler(Tillerman_OnCommand));
            CommandSystem.Register("house", AccessLevel.Player, new CommandEventHandler(House_OnCommand));
        }

        [Usage("say")]
        [Description("Opens the Say quick-speech gump for house and vessel commands.")]
        public static void Say_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Deleted || !from.Alive)
                return;

            BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);
            SayTab defaultTab = (boat != null && boat.Contains(from)) ? SayTab.Boat : SayTab.House;

            from.SendGump(new SayGump(from, defaultTab, boat));
        }

        [Usage("tillerman")]
        [Aliases("boat")]
        [Description("Opens the boat navigation section of the Say gump.")]
        public static void Tillerman_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Deleted || !from.Alive)
                return;

            BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);
            from.SendGump(new SayGump(from, SayTab.Boat, boat));
        }

        [Usage("house")]
        [Description("Opens the house management section of the Say gump.")]
        public static void House_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Deleted || !from.Alive)
                return;

            BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);
            from.SendGump(new SayGump(from, SayTab.House, boat));
        }

        public SayGump(Mobile from, SayTab tab = SayTab.House, BaseBoat boat = null) : base(100, 100)
        {
            m_Tab = tab;
            m_Boat = boat ?? BaseBoat.FindBoatAt(from, from.Map);

            from.CloseGump(typeof(SayGump));
            from.CloseGump(typeof(TillermanGump));

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // Ultra-compact 155x185 dark aesthetic layout (51% smaller than original 260x225)
            AddBackground(0, 0, 155, 185, 9200);
            AddAlphaRegion(6, 6, 143, 173);

            // Tab navigation bar
            bool isHouse = (m_Tab == SayTab.House);

            AddButton(10, 9, isHouse ? 4006 : 4005, isHouse ? 4005 : 4007, (int)Buttons.TabHouse, GumpButtonType.Reply, 0);
            AddLabel(28, 7, isHouse ? 53 : 995, "House");

            AddButton(82, 9, !isHouse ? 4006 : 4005, !isHouse ? 4005 : 4007, (int)Buttons.TabBoat, GumpButtonType.Reply, 0);
            AddLabel(100, 7, !isHouse ? 53 : 995, "Boat");

            // Separator
            AddImageTiled(8, 27, 139, 1, 0x2711);

            if (isHouse)
            {
                // House commands section
                AddButton(12, 33, 4005, 4007, (int)Buttons.LockDown, GumpButtonType.Reply, 0);
                AddLabel(32, 33, 1152, "Lock Down");

                AddButton(12, 55, 4005, 4007, (int)Buttons.Secure, GumpButtonType.Reply, 0);
                AddLabel(32, 55, 1152, "Secure");

                AddButton(12, 77, 4005, 4007, (int)Buttons.Release, GumpButtonType.Reply, 0);
                AddLabel(32, 77, 1152, "Release");

                AddButton(12, 99, 4005, 4007, (int)Buttons.Unsecure, GumpButtonType.Reply, 0);
                AddLabel(32, 99, 1152, "Unsecure");

                AddButton(12, 121, 4005, 4007, (int)Buttons.TrashBarrel, GumpButtonType.Reply, 0);
                AddLabel(32, 121, 1152, "Trash Barrel");

                AddButton(12, 143, 4005, 4007, (int)Buttons.Ban, GumpButtonType.Reply, 0);
                AddLabel(32, 143, 1152, "Ban");

                AddButton(78, 143, 4005, 4007, (int)Buttons.Eject, GumpButtonType.Reply, 0);
                AddLabel(98, 143, 1152, "Eject");
            }
            else
            {
                // Boat navigation section (2-column layout)
                AddHtml(12, 29, 60, 16, "<basefont color=#87CEEB><center><small>SAIL</small></center></basefont>", false, false);
                AddHtml(82, 29, 60, 16, "<basefont color=#87CEEB><center><small>1-TILE</small></center></basefont>", false, false);

                // Row 1: Forward / Forward One
                AddButton(12, 47, 4005, 4007, (int)Buttons.Forward, GumpButtonType.Reply, 0);
                AddLabel(32, 47, 1152, "Fwd");
                AddButton(82, 47, 4005, 4007, (int)Buttons.ForwardOne, GumpButtonType.Reply, 0);
                AddLabel(102, 47, 1152, "Fwd 1");

                // Row 2: Left / Left One
                AddButton(12, 69, 4005, 4007, (int)Buttons.Left, GumpButtonType.Reply, 0);
                AddLabel(32, 69, 1152, "Left");
                AddButton(82, 69, 4005, 4007, (int)Buttons.LeftOne, GumpButtonType.Reply, 0);
                AddLabel(102, 69, 1152, "Left 1");

                // Row 3: Right / Right One
                AddButton(12, 91, 4005, 4007, (int)Buttons.Right, GumpButtonType.Reply, 0);
                AddLabel(32, 91, 1152, "Right");
                AddButton(82, 91, 4005, 4007, (int)Buttons.RightOne, GumpButtonType.Reply, 0);
                AddLabel(102, 91, 1152, "Right 1");

                // Row 4: Back / Back One
                AddButton(12, 113, 4005, 4007, (int)Buttons.Back, GumpButtonType.Reply, 0);
                AddLabel(32, 113, 1152, "Back");
                AddButton(82, 113, 4005, 4007, (int)Buttons.BackOne, GumpButtonType.Reply, 0);
                AddLabel(102, 113, 1152, "Back 1");

                // Row 5: Turn Left / Turn Right
                AddButton(12, 135, 4005, 4007, (int)Buttons.TurnLeft, GumpButtonType.Reply, 0);
                AddLabel(32, 135, 1152, "Turn L");
                AddButton(82, 135, 4005, 4007, (int)Buttons.TurnRight, GumpButtonType.Reply, 0);
                AddLabel(102, 135, 1152, "Turn R");

                // Row 6: Turn Around / Stop
                AddButton(12, 157, 4005, 4007, (int)Buttons.TurnAround, GumpButtonType.Reply, 0);
                AddLabel(32, 157, 1152, "About");
                AddButton(82, 157, 4005, 4007, (int)Buttons.Stop, GumpButtonType.Reply, 0);
                AddLabel(102, 157, 38, "STOP");
            }
        }

        private static void SpeakWithKeyword(Mobile from, string text, int keyword)
        {
            from.DoSpeech(text, new int[] { keyword }, MessageType.Regular, from.SpeechHue);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (from == null || from.Deleted || !from.Alive)
                return;

            if (info.ButtonID == (int)Buttons.Close || info.ButtonID == 0)
                return;

            // Tab switching
            if (info.ButtonID == (int)Buttons.TabHouse)
            {
                from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                return;
            }

            if (info.ButtonID == (int)Buttons.TabBoat)
            {
                from.SendGump(new SayGump(from, SayTab.Boat, m_Boat));
                return;
            }

            // House commands
            switch ((Buttons)info.ButtonID)
            {
                case Buttons.LockDown:
                    SpeakWithKeyword(from, "I wish to lock this down", 0x23);
                    from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                    return;

                case Buttons.Secure:
                    SpeakWithKeyword(from, "I wish to secure this", 0x25);
                    from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                    return;

                case Buttons.Release:
                    SpeakWithKeyword(from, "I wish to release this", 0x24);
                    from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                    return;

                case Buttons.Unsecure:
                    SpeakWithKeyword(from, "I wish to unsecure this", 0x26);
                    from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                    return;

                case Buttons.TrashBarrel:
                    SpeakWithKeyword(from, "I wish to place a trash barrel", 0x28);
                    from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                    return;

                case Buttons.Ban:
                    SpeakWithKeyword(from, "I ban thee", 0x34);
                    from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                    return;

                case Buttons.Eject:
                    SpeakWithKeyword(from, "Remove thyself", 0x33);
                    from.SendGump(new SayGump(from, SayTab.House, m_Boat));
                    return;
            }

            // Boat commands
            BaseBoat boat = m_Boat ?? BaseBoat.FindBoatAt(from, from.Map);
            bool canCommand = (boat != null && !boat.Deleted && boat.Contains(from) && boat.CanCommand(from) && !boat.Scuttled);

            switch ((Buttons)info.ButtonID)
            {
                case Buttons.Forward:
                    SpeakWithKeyword(from, "Forward", 0x45);
                    if (canCommand) boat.StartMove(Direction.North, true);
                    break;

                case Buttons.Back:
                    SpeakWithKeyword(from, "Backward", 0x46);
                    if (canCommand) boat.StartMove(Direction.South, true);
                    break;

                case Buttons.Left:
                    SpeakWithKeyword(from, "Left", 0x47);
                    if (canCommand) boat.StartMove(Direction.West, true);
                    break;

                case Buttons.Right:
                    SpeakWithKeyword(from, "Right", 0x48);
                    if (canCommand) boat.StartMove(Direction.East, true);
                    break;

                case Buttons.ForwardOne:
                    SpeakWithKeyword(from, "Forward one", 0x5A);
                    if (canCommand) boat.OneMove(Direction.North);
                    break;

                case Buttons.BackOne:
                    SpeakWithKeyword(from, "Backward one", 0x5B);
                    if (canCommand) boat.OneMove(Direction.South);
                    break;

                case Buttons.LeftOne:
                    SpeakWithKeyword(from, "Left one", 0x58);
                    if (canCommand) boat.OneMove(Direction.West);
                    break;

                case Buttons.RightOne:
                    SpeakWithKeyword(from, "Right one", 0x59);
                    if (canCommand) boat.OneMove(Direction.East);
                    break;

                case Buttons.TurnLeft:
                    SpeakWithKeyword(from, "Turn left", 0x60);
                    if (canCommand) boat.StartTurn(-2, true);
                    break;

                case Buttons.TurnRight:
                    SpeakWithKeyword(from, "Turn right", 0x61);
                    if (canCommand) boat.StartTurn(2, true);
                    break;

                case Buttons.TurnAround:
                    SpeakWithKeyword(from, "Come about", 0x62);
                    if (canCommand) boat.StartTurn(-4, true);
                    break;

                case Buttons.Stop:
                    SpeakWithKeyword(from, "Stop", 0x4F);
                    if (canCommand) boat.StopMove(true);
                    break;
            }

            // Keep the gump open on the Boat tab for uninterrupted navigation
            from.SendGump(new SayGump(from, SayTab.Boat, boat));
        }
    }

    // Backward-compatible alias
    public class TillermanGump : SayGump
    {
        public TillermanGump(Mobile from, SayTab tab = SayTab.Boat, BaseBoat boat = null) : base(from, tab, boat)
        {
        }
    }
}
