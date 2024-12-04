using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using Cyotek.Drawing.BitmapFont;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Xml;

namespace ClassicUO.Game.Managers
{
    public class PaperdollItem
    {
        public uint Serial { get; set; }
        public Layer Layer { get; set; }
        public ushort Graphic { get; set; }
        public ushort Hue { get; set; }
        public ushort AnimID { get; set; }
        public bool IsPartialHue { get; set; }
    }

    internal class PaperdollSelectCharManager
    {
        public static PaperdollSelectCharManager Instance => instance ??= new PaperdollSelectCharManager();

        private Dictionary<string, PaperdollItem> items = new Dictionary<string, PaperdollItem>();

        private string savePath;

        private static PaperdollSelectCharManager instance;

        private PaperdollSelectCharManager()
        {
            Load();
        }

        public void AddItem(string key, Layer layer, ushort graphic, ushort hue, uint serial, ushort animID, bool isPartialHue)
        {

            if (items.ContainsKey(key))
            {
                items[key] = new PaperdollItem
                {
                    Layer = layer,
                    Graphic = graphic,
                    Hue = hue,
                    Serial = serial, 
                    AnimID = animID,
                    IsPartialHue = isPartialHue
                };
            }
            else
            {
                items.Add(key, new PaperdollItem
                {
                    Layer = layer,
                    Graphic = graphic,
                    Hue = hue,
                    Serial = serial,
                    AnimID = animID,
                    IsPartialHue = isPartialHue
                });
            }
        }

        public void Save()
        {
            try
            {
                Mobile mobile = World.Mobiles.Get(World.Player.Serial);

                if (mobile != null)
                {
                    foreach (Layer layer in Enum.GetValues(typeof(Layer)))
                    {
                        Item item = mobile.FindItemByLayer(layer);

                        if (item != null)
                        {
                            if (mobile.Serial == World.Player.Serial)
                            {
                                // Aqui você pode adicionar condições específicas para os layers, se necessário
                                if (layer != Layer.Bracelet && layer != Layer.Earrings && layer != Layer.Ring)
                                {
                                    AddItem(item.Serial.ToString(), item.Layer, item.Graphic, item.Hue, item.Serial, item.ItemData.AnimID, item.ItemData.IsPartialHue);
                                }
                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save marked tile data: {ex.Message}");
            }
        }

        public void SaveJson()
        {
            try
            {

                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", Settings.GlobalSettings.Username, World.ServerName, World.Player.Name, "paperdollSelectCharManager.json");
                File.WriteAllText(savePath, json);
                items.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save marked tile data: {ex.Message}");
            }
        }

        public void Load()
        {
            savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", Settings.GlobalSettings.Username, World.ServerName, World.Player.Name, "paperdollSelectCharManager.json");
            if (File.Exists(savePath))
            {
                try
                {
                    string json = File.ReadAllText(savePath);
                    items = JsonSerializer.Deserialize<Dictionary<string, PaperdollItem>>(json) ?? new Dictionary<string, PaperdollItem>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load marked tile data: {ex.Message}");
                }
            }
        }
    }
}
