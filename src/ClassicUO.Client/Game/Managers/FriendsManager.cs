using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal static class FriendManager
    {
        /// <summary>
        /// Set of Char names
        /// </summary>
        public static Dictionary<uint, string> FriendsList = new Dictionary<uint, string>();
        private const string XML_FILE_Path = "friend_list.xml";

        /// <summary>
        /// Initialize Ignore Manager
        /// - Load List from XML file
        /// </summary>
        public static void Initialize()
        {
            ReadFriendList();
        }

        /// <summary>
        /// Add Char to ignored list
        /// </summary>
        /// <param name="entity">Targeted Entity</param>
        public static void AddFriend(Entity entity)
        {
            if (entity is not Mobile m)
            {                
                GameActions.Print("Invalid Target. Mobiles Only");
                return;
            }
            if (m.NotorietyFlag == Data.NotorietyFlag.Invulnerable)
            {
                GameActions.Print("Vendors are not allowed to be your friend.");
                return;
            }

            AddFriend(m.Serial, m.Name);
        }
        public static void AddFriend(uint serial, string name)
        {
            if (serial == World.Player.Serial)
            {
                GameActions.Print("You cannot be friends with yourself.");
                return;
            }
            if (FriendsList.ContainsKey(serial))
            {
                GameActions.Print("This friend already exists.");
                return;
            }

            FriendsList.Add(serial, name);
            // Redraw list of chars
            UIManager.GetGump<FriendsManagerGump>()?.Redraw();

            GameActions.Print("You have now become friends.");
        }
        public static void AddFriend(uint serial)
        {
            AddFriend(serial, serial.ToString());
        }

        /// <summary>
        /// Remove Char from Ignored List
        /// </summary>
        /// <param name="charName">Char name</param>
        public static void RemoveFriend(uint serial)
        {
            if (FriendsList.ContainsKey(serial))
                FriendsList.Remove(serial);
        }

        /// <summary>
        /// Load Ignored List from XML file
        /// </summary>
        private static void ReadFriendList()
        {
            var list = new Dictionary<uint, string>();

            string ignoreXmlPath = Path.Combine(ProfileManager.ProfilePath, XML_FILE_Path); ;

            if (!File.Exists(ignoreXmlPath))
            {
                return;
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(ignoreXmlPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            XmlElement root = doc["friends"];

            if (root != null)
            {
                foreach (XmlElement xml in root.ChildNodes)
                {
                    if (xml.Name != "friend")
                    {
                        continue;
                    }
                    if (uint.TryParse(xml.GetAttribute("serial"), out var serial)){
                        var name = serial.ToString();
                        if (!string.IsNullOrWhiteSpace(xml.GetAttribute("name")))
                        {
                            name = xml.GetAttribute("name");
                        }
                        list.Add(serial, name);
                    }
                }
            }

            FriendsList = list;
        }

        /// <summary>
        /// Save List to XML File
        /// </summary>
        public static void SaveFriendList()
        {
            string ignoreXmlPath = Path.Combine(ProfileManager.ProfilePath, XML_FILE_Path);

            using (XmlTextWriter xml = new XmlTextWriter(ignoreXmlPath, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("friends");

                foreach (var ch in FriendsList)
                {
                    xml.WriteStartElement("friend");
                    xml.WriteAttributeString("serial", ch.Key.ToString());
                    xml.WriteAttributeString("name", ch.Value.ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }
    }
}
