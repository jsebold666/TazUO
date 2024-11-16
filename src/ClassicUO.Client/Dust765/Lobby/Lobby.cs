using System;
using System.Net;
using ClassicUO.Configuration;
using ClassicUO.Dust765.Managers;
using ClassicUO.Dust765.Lobby.Networking;
using ClassicUO.Dust765.Shared;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;

namespace ClassicUO.Dust765.Lobby
{
    public static class Lobby
    {
        public static int Port = Convert.ToInt32(ProfileManager.CurrentProfile.LobbyPort);
        static IPEndPoint _ipep;
        public static NetState _netState;

        public static void LoadCommands()
        {
            CommandManager.Register("lobby", new Action<string[]>(OnCommand_Client));
        }

        private static void OnCommand_Client(string[] s)
        {
            if (_netState == null || !_netState.IsOpen)
            {
                if (s.Length == 0 || s.Length == 1 || s.Length >= 4)
                {
                    Print("Invalid command.", MessageColor.Red);
                    Print("Use \"-lobby help\" for a list of commands.", MessageColor.Green);
                    return;
                }

                switch (s[1])
                {
                    default:
                        Print("Failed to recognize network command.", MessageColor.Orange);
                        break;

                    case "help":
                        Print("Usage: lobby status | connect", MessageColor.Green);
                        break;

                    case "status":
                        Print("You are currently offline.", MessageColor.Red);
                        break;

                    case "connect":
                        if (s.Length == 3)
                        {
                            string ipString = s[2];
                            if (ipString == "help")
                            {
                                Print("Usage: lobby connect 127.0.0.1");
                                break;
                            }
                            else if (NetState.ValidateIPv4(ipString))
                            {
                                IPAddress ipAddress = IPAddress.None;
                                IPAddress.TryParse(ipString, out ipAddress);
                                Connect(ipAddress);
                            }
                            else
                            {
                                Print("You provided an invalid IP address format.", MessageColor.Orange);
                                Print("Usage: lobby connect 127.0.0.1", MessageColor.Orange);
                            }
                            break;
                        }
                        else
                        {
                            Print("You provided an invalid IP address format.", MessageColor.Orange);
                            Print("Usage: lobby connect 127.0.0.1", MessageColor.Orange);
                        }
                        break;

                    case "cast":
                        Print("You must be connected to perform that action.", MessageColor.Orange);
                        break;

                    case "target":
                        Print("You must be connected to perform that action.", MessageColor.Orange);
                        break;

                    case "drop":
                        Print("You must be connected to perform that action.", MessageColor.Orange);
                        break;

                    case "attack":
                        Print("You must be connected to perform that action.", MessageColor.Orange);
                        break;
                }
                return;
            }

            if (_netState.IsOpen)
            {
                Mobile mob;

                if (s.Length == 0 || s.Length == 1)// || s.Length >= 4)
                {
                    Print("Invalid command.", MessageColor.Red);
                    Print("Use \"-lobby help\" for a list of commands.", MessageColor.Green);
                    return;
                }

                switch (s[1])
                {
                    default:
                        Print("Network command not recognized.", MessageColor.Orange);
                        break;

                    case "help":
                        Print("Usage: lobby status | disconnect | cast | target | drop | hid", MessageColor.Green);
                        break;

                    case "status":
                        Print($"You are connected to {_ipep.Address}.", MessageColor.Green);
                        break;

                    case "connect":
                        Print($"You are already connected to {_ipep.Address}.", MessageColor.Orange);
                        break;

                    case "disconnect":
                        Disconnect();
                        break;

                    case "drop" when s.Length == 3 && s[2] == "help":
                        Print("Usage: Drops any spell you are currently holding onto the last target sent to the group.");
                        break;

                    case "drop" when TargetManager.LastTargetInfo.Serial != 0:
                        mob = World.Mobiles.Get(TargetManager.LastTargetInfo.Serial);

                        if (mob != null)
                            _netState.Send(new PTarget(mob));
                        else
                            Print("Target was out of range.", MessageColor.Red);
                        break;

                    case "target" when s.Length == 3 && s[2] == "help":
                        Print("Usage: Update the last target for everyone in the group.");
                        break;

                    case "target" when TargetManager.LastTargetInfo.Serial == 0:
                        Print("No last target is set.", MessageColor.Red);
                        break;

                    case "target" when TargetManager.LastTargetInfo.Serial != 0:
                        mob = World.Mobiles.Get(TargetManager.LastTargetInfo.Serial);
                        if (mob != null)
                            _netState.Send(new PSetTarget(mob));
                        else
                            Print("Target was out of range.", MessageColor.Red);
                        break;

                    case "cast" when s.Length >= 3:
                        string value = s[2];
                        if (value == "help")
                        {
                            Print("Usage: lobby cast {spell}");
                            break;
                        }

                        if (s.Length > 3)
                        {
                            int i = 0;
                            foreach (var arg in s)
                            {
                                if (i >= 3)
                                {
                                    value = value + s[i];
                                }
                                i++;
                            }
                        }

                        try
                        {
                            var spell = Enum.Parse(typeof(SpellAction), value, true);
                            _netState.Send(new PSpellCast((SpellAction) spell));
                        }
                        catch
                        {
                            Print($"Spell = '{value}' is invalid, failed to parse.", MessageColor.Orange);
                        }
                        break;

                    case "attack" when s.Length == 3 && s[2] == "help":
                        Print("Usage: Everyone in the group attacks last target.");
                        break;

                    case "attack" when TargetManager.LastTargetInfo.Serial == 0:
                        Print("No last target is set.", MessageColor.Red);
                        break;

                    case "attack" when TargetManager.LastTargetInfo.Serial != 0:
                        mob = World.Mobiles.Get(TargetManager.LastTargetInfo.Serial);
                        if (mob != null)
                            _netState.Send(new PAttack(mob));
                        else
                            Print("Target was out of range.", MessageColor.Red);
                        break;
                }
                return;
            }
        }

        public static void Connect(IPAddress addr)
        {
            bool rejoin = _netState != null ? CompareIPv4(_netState.Address, addr) : false;
            if (rejoin && _netState.IsOpen)
            {
                Disconnect();
            }
            _ipep = new IPEndPoint(addr, Lobby.Port);
            _netState = NetState.Connect(_ipep);
            if (_netState == null || !_netState.IsOpen)
            {
                Print("You failed to establish a connection.", MessageColor.Orange);
            }
            else
            {
                _netState.Send(new PConnected(World.Player, rejoin));
            }
        }

        public static bool CompareIPv4(IPAddress current, IPAddress other)
        {
            byte[] from = current.GetAddressBytes();
            byte[] to = other.GetAddressBytes();

            for (int i = 0; i < 4; i++)
            {
                if (from[i] != to[i])
                    return false;
            }

            return true;
        }

        public static void Disconnect()
        {
            if (_netState == null)
                return;

            try
            {
                _netState.Disconnect();
                _netState = null;
                Print("Disconnected from the network.", MessageColor.Orange);
            }
            catch
            {
                Print("Disconnected from the network failed.", MessageColor.Red);
            }
        }

        public static void Update()
        {
            if (_netState == null)
                return;

            _netState.Slice();
        }

        public static void Print(string message, MessageColor color = MessageColor.Default)
        {
            GameActions.Print(message, (ushort) color, MessageType.System, 1);
        }
    }
}