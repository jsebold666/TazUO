#region license

// Copyright (C) 2020 project dust765
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Dust765.Shared;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;

namespace ClassicUO.Dust765.Autos
{
    internal class AutoRangeDisplay
    {
        public static bool IsEnabled { get; set; }
        public static Thread a_AutoRangeThread;

        private static Item _tempItemInLeftHand; //temp var
        private static Item _tempItemInRightHand;

        //WEAPONS LIST
        public static readonly List<ushort> WeaponsList = new List<ushort>();

        //##AutoRangeDisplay Toggle##//
        public static void Toggle()
        {
            GameActions.Print(String.Format("Auto AutoRangeDisplay:{0}abled", (IsEnabled = !IsEnabled) == true ? "En" : "Dis"), 70);
        }
		
		//##Register Command and Perform Checks##//
        public static void Initialize()
        {
            CommandManager.Register("autorange", args => Toggle());

            LoadFile();

            a_AutoRangeThread = new Thread(new ThreadStart(DoAutoRange))
            {
                IsBackground = true
            };

            if (ProfileManager.CurrentProfile.AutoRangeDisplayAlways)
            {
                IsEnabled = true;
            }
        }
		
		//##Default AutoRangeDisplay Status on GameLoad##//
        static AutoRangeDisplay()
        {
            if (ProfileManager.CurrentProfile.AutoRangeDisplayAlways)
            {
                GameActions.Print(String.Format("Auto AutoRangeDisplay:Enabled"), 70);
                IsEnabled = true;
            }
            else
                IsEnabled = false;
        }
		
		//##Perform AutoRangeDisplay Update on Toggle/Player Death##//
        public static void Update()
        {
            if (!IsEnabled) // || World.Player.IsDead)            
                DisableAutoRangeDisplay();

            if (IsEnabled)
                EnableAutoRangeDisplay();            
        }

		//##Enable AutoRangeDisplay##//
        private static void EnableAutoRangeDisplay()
        {
            if (!a_AutoRangeThread.IsAlive)
            {
                a_AutoRangeThread = new Thread(new ThreadStart(DoAutoRange))
                {
                    IsBackground = true
                };
                a_AutoRangeThread.Start();
            }
        }

		//##Disable AutoRangeDisplay##//
        private static void DisableAutoRangeDisplay()
        {
            if (a_AutoRangeThread.IsAlive)
            {
                a_AutoRangeThread.Abort();
            }
        }
		
		//##Perform AutoRangeDisplay Checks##//
        private static void DoAutoRange()
        {
            DateTime dateTime = DateTime.Now;
            while (true)
            {

                //if (World.Player == null || World.Player.IsDead)
                //    DisableAutoRangeDisplay();

                if (World.Player == null || World.Player.IsDead || ProfileManager.CurrentProfile == null)
                    return;

                _tempItemInLeftHand = null;
                _tempItemInRightHand = null;
                _tempItemInLeftHand = World.Player.FindItemByLayer(Layer.OneHanded);
                _tempItemInRightHand = World.Player.FindItemByLayer(Layer.TwoHanded);

                int range = 0;
                int index = 0;

                if (_tempItemInRightHand != null)
                {
                    index = WeaponsList.IndexOf(_tempItemInRightHand.Graphic);

                    if (index != 0)
                    {
                        range = WeaponsList[index + 1];

                        ProfileManager.CurrentProfile.AutoRangeDisplayActive = true;
                        ProfileManager.CurrentProfile.AutoRangeDisplayActiveRange = range;
                    } else
                    {
                        ProfileManager.CurrentProfile.AutoRangeDisplayActive = false;
                    }
                }
                if (_tempItemInLeftHand != null)
                {
                    index = WeaponsList.IndexOf(_tempItemInLeftHand.Graphic);

                    if (index != 0)
                    {
                        range = WeaponsList[index + 1];

                        ProfileManager.CurrentProfile.AutoRangeDisplayActive = true;
                        ProfileManager.CurrentProfile.AutoRangeDisplayActiveRange = range;
                    }
                    else
                    {
                        ProfileManager.CurrentProfile.AutoRangeDisplayActive = false;
                    }
                }

                if (_tempItemInRightHand == null && _tempItemInLeftHand == null)
                    ProfileManager.CurrentProfile.AutoRangeDisplayActive = false;

                Thread.Sleep(50);
            }
        }
		
		//##Perform Ninja Trigger##//
        private static void NinjaItem()
        {
            
        }
		
		//##Perform Message##//
        public static void Print(string message, MessageColor color = MessageColor.Default)
        {
            GameActions.Print(message, (ushort) color, MessageType.System, 1);
        }

        //GET WEAPON SPEED FROM FILE OR CREATE IT
        public static void LoadFile()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string autorange = Path.Combine(path, "autorange.txt");

            if (!File.Exists(autorange))
            {
                using (StreamWriter writer = new StreamWriter(autorange))
                {
                    //0x13B1, 0x13B2 bow / 0x26C2, 0x26CC comp / 0x26C3, 0x26CD repeating / 0x27A5, 0x27F0 yumi / 0xF4F, 0xF50 xbow / 0x13FC, 0x13FD heavy xbow
                    //0x2D1E, 0x2D2A elven comp, 0x2D1F, 0x2D2B magical shortbow
                    ushort[] weapons = {

                        0x13B1, 0x13B2, 0x26C2, 0x26CC, 0x26C3, 0x26CD, 0x27A5, 0x27F0,
                        0xF4F, 0xF50, 0x13FC, 0x13FD, 0x2D1E, 0x2D2A, 0x2D1F, 0x2D2B
                    };

                    for (int i = 0; i < weapons.Length; i++)
                    {
                        ushort graphic = weapons[i];
                        ushort flag = 10;

                        switch (graphic)
                        {
                            case 0x13B1:
                            case 0x13B2:
                                flag = 10;
                                break;

                            case 0x26C2:
                            case 0x26CC:
                                flag = 10;
                                break;

                            case 0x26C3:
                            case 0x26CD:
                                flag = 7;
                                break;

                            case 0x27A5:
                            case 0x27F0:
                                flag = 10;
                                break;

                            case 0xF4F:
                            case 0xF50:
                                flag = 8;
                                break;

                            case 0x13FC:
                            case 0x13FD:
                                flag = 8;
                                break;

                            case 0x2D1E:
                            case 0x2D2A:
                                flag = 10;
                                break;

                            case 0x2D1F:
                            case 0x2D2B:
                                flag = 10;
                                break;
                        }

                        writer.WriteLine($"{graphic}={flag}");
                    }
                }
            }

            TextFileParser autorangeParser = new TextFileParser(File.ReadAllText(autorange), new[] { ' ', '\t', ',', '=' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!autorangeParser.IsEOF())
            {
                var ss = autorangeParser.ReadTokens();
                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        WeaponsList.Add(graphic);
                    }

                    if (ushort.TryParse(ss[1], out ushort f))
                    {
                        WeaponsList.Add(f);
                    }
                }
            }
        }
    }
}