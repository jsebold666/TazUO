using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.Managers
{
    internal class PartyManager
    {
        private const int PARTY_SIZE = 10;

        public uint Leader { get; set; }
        public uint Inviter { get; set; }
        public bool CanLoot { get; set; }

        public PartyMember[] Members { get; } = new PartyMember[PARTY_SIZE];


        public long PartyHealTimer { get; set; }
        public uint PartyHealTarget { get; set; }

        public void ParsePacket(ref PacketBufferReader p)
        {
            byte code = p.ReadByte();

            bool add = false;

            switch (code)
            {
                case 1:
                    add = true;
                    goto case 2;

                case 2:
                    byte count = p.ReadByte();

                    if (count <= 1)
                    {
                        Leader = 0;
                        Inviter = 0;

                        for (int i = 0; i < PARTY_SIZE; i++)
                        {
                            if (Members[i] == null || Members[i].Serial == 0)
                            {
                                break;
                            }

                            BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(Members[i].Serial);


                            if (gump != null)
                            {
                                if (code == 2)
                                {
                                    Members[i].Serial = 0;
                                }

                                gump.RequestUpdateContents();
                            }
                        }

                        Clear();

                        UIManager.GetGump<PartyGump>()?.RequestUpdateContents();

                        break;
                    }

                    Clear();

                    uint to_remove = 0xFFFF_FFFF;

                    if (!add)
                    {
                        to_remove = p.ReadUInt();

                        UIManager.GetGump<BaseHealthBarGump>(to_remove)?.RequestUpdateContents();
                    }

                    bool remove_all = !add && to_remove == World.Player;
                    int done = 0;

                    for (int i = 0; i < count; i++)
                    {
                        uint serial = p.ReadUInt();
                        bool remove = !add && serial == to_remove;

                        if (remove && serial == to_remove && i == 0)
                        {
                            remove_all = true;
                        }

                        if (!remove && !remove_all)
                        {
                            if (!Contains(serial))
                            {
                                Members[i] = new PartyMember(serial);
                            }

                            done++;
                        }

                        if (i == 0 && !remove && !remove_all)
                        {
                            Leader = serial;
                        }

                        BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(serial);

                        if (gump != null)
                        {
                            gump.RequestUpdateContents();
                        }
                        else
                        {
                            if (serial == World.Player)
                            {
                            }
                        }
                    }

                    if (done <= 1 && !add)
                    {
                        for (int i = 0; i < PARTY_SIZE; i++)
                        {
                            if (Members[i] != null && SerialHelper.IsValid(Members[i].Serial))
                            {
                                uint serial = Members[i].Serial;

                                Members[i] = null;

                                UIManager.GetGump<BaseHealthBarGump>(serial)?.RequestUpdateContents();
                            }
                        }

                        Clear();
                    }


                    UIManager.GetGump<PartyGump>()?.RequestUpdateContents();

                    break;

                case 3:
                case 4:
                    uint ser = p.ReadUInt();
                    string name = p.ReadUnicode();

                    for (int i = 0; i < PARTY_SIZE; i++)
                    {
                        if (Members[i] != null && Members[i].Serial == ser)
                        {
                            MessageManager.HandleMessage
                            (
                                null, name, Members[i].Name, ProfileManager.CurrentProfile.PartyMessageHue, MessageType.Party,
                                3, TextType.GUILD_ALLY
                            );

                            break;
                        }
                    }

                    break;

                case 7:
                    Inviter = p.ReadUInt();

                    if (ProfileManager.CurrentProfile.PartyInviteGump)
                    {
                        UIManager.Add(new PartyInviteGump(Inviter));
                    }

                    break;
            }
        }

        public bool Contains(uint serial)
        {
            for (int i = 0; i < PARTY_SIZE; i++)
            {
                PartyMember mem = Members[i];

                if (mem != null && mem.Serial == serial)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            Leader = 0;
            Inviter = 0;

            for (int i = 0; i < PARTY_SIZE; i++)
            {
                Members[i] = null;
            }
        }
    }

    internal class PartyMember : IEquatable<PartyMember>
    {
        private string _name;

        public PartyMember(uint serial)
        {
            Serial = serial;
            _name = Name;
        }

        public string Name
        {
            get
            {
                Mobile mobile = World.Mobiles.Get(Serial);

                if (mobile != null)
                {
                    _name = mobile.Name;

                    if (string.IsNullOrEmpty(_name))
                    {
                        _name = ResGeneral.NotSeeing;
                    }
                }

                return _name;
            }
        }

        public bool Equals(PartyMember other)
        {
            if (other == null)
            {
                return false;
            }

            return other.Serial == Serial;
        }

        public uint Serial;
    }
}