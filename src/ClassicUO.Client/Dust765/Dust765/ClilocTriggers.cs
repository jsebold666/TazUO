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
using ClassicUO.Game.Managers;
using ClassicUO.Assets;
using System.Text.RegularExpressions; //REGEX

namespace ClassicUO.Dust765.Dust765
{
    public class ClilocTriggers
    {
        private static int[] _startBandiesAtClilocs = new int[]
        {
            500956,
            500957,
            500958,
            500959,
            500960
        };
        private static int[] _stopBandiesAtClilocs = new int[]
        {
            500955,
            500962,
            500963,
            500964,
            500965,
            500966,
            500967,
            500968,
            500969,
            503252,
            503253,
            503254,
            503255,
            503256,
            503257,
            503258,
            503259,
            503260,
            503261,
            1010058,
            1010648,
            1010650,
            1060088,
            1060167
        };
        private static int[] _disarmedAtClilocs = new int[]
        {
            501708, //I have been disarmed.
            1004007 //You have been disarmed!
        };
        public void OnMessage(string text, ushort hue, string name, bool isunicode = true)
        {
            UOClassicCombatSelf UOClassicCombatSelf = UIManager.GetGump<UOClassicCombatSelf>(); // ## BEGIN - END ## // SELF
            UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();

            //SYS MESSAGES ONLY
            if (name != "System" && text.Length <= 0)
                return;

            // ## BEGIN - END ## // SELF
            //STOP BANDIES TIMER
            for (int i = 0; i < _stopBandiesAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_stopBandiesAtClilocs[i]) == null)
                    continue;

                if (text.StartsWith(ClilocLoader.Instance.GetString(_stopBandiesAtClilocs[i])))
                {
                    UOClassicCombatSelf?.ClilocTriggerStopBandies();
                    return;
                }
            }

            //START BANDIES TIMER
            for (int i = 0; i < _startBandiesAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_startBandiesAtClilocs[i]) == null)
                    continue;

                if (text.StartsWith(ClilocLoader.Instance.GetString(_startBandiesAtClilocs[i])))
                {
                    UOClassicCombatSelf?.ClilocTriggerStartBandies();
                    return;
                }
            }
            // ## BEGIN - END ## // SELF

            //GOT DISARMED
            for (int i = 0; i < _disarmedAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_disarmedAtClilocs[i]) == null)
                    continue;

                if (text.StartsWith(ClilocLoader.Instance.GetString(_disarmedAtClilocs[i])))
                {
                    UOClassicCombatSelf?.ClilocTriggerGotDisarmed();// ## BEGIN - END ## // SELF
                    UOClassicCombatBuffbar?.ClilocTriggerGotDisarmed();

                    return;
                }
            }
            if (text.StartsWith("Their attack disarms you!"))
            {
                UOClassicCombatSelf?.ClilocTriggerGotDisarmed();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerGotDisarmed();
                return;
            }

            //DISARM
            if (text.StartsWith("You will now attempt to disarm your opponents."))
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmON();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerDisarmON();
                return;
            }
            if (text.StartsWith("You refrain from making disarm attempts."))
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmOFF();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerDisarmOFF();
                return;
            }
            //SUCCESSFUL DISARM MSGES
            if (text.StartsWith("You disarm their weapon!"))
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmStriked();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerDisarmStriked();
                return;
            }
            if (text.StartsWith("Your strike disarms your target!"))
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmStriked();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerDisarmStriked();
                return;
            }
            if (text.StartsWith("You successfully disarm your opponent!"))
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmStriked();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerDisarmStriked();
                return;
            }
            //FAILED DISARM MSGES
            if (text.StartsWith("You fail to disarm your opponent."))
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmFailed();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerDisarmFailed();
                return;
            }
            if (text.StartsWith("You failed in your attempt do disarm."))
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmFailed();// ## BEGIN - END ## // SELF
                UOClassicCombatBuffbar?.ClilocTriggerDisarmFailed();
                return;
            }
            // ## BEGIN - END ## // SELF
            //UCCSELF CLILOC TRIGGERS
            if (text.Contains("You must have a free hand"))
            {
                UOClassicCombatSelf?.ClilocTriggerFSFreeHands();
                return;
            }
            if (text.Contains("You must wait") && text.Contains("second before using another") && text.Contains("potion"))
            {

                string seconds = Regex.Match(text, @"\d+").Value; //first number
                int secondsS = Int32.Parse(seconds);

                ushort potion = 0;

                if (text.Contains("health"))
                {
                    potion = 0x0F0C;
                }
                else if (text.Contains("cure"))
                {
                    potion = 0x0F07;
                }

                UOClassicCombatSelf?.ClilocTriggerFSWaitX(secondsS, potion);
                return;
            }
            if (text.StartsWith("You are already at full health."))
            {
                UOClassicCombatSelf?.ClilocTriggerFSFullHP();
                return;
            }
            if (text.StartsWith("You are not poisoned."))
            {
                UOClassicCombatSelf?.ClilocTriggerFSNoPoison();
                return;
            }
            if (text.StartsWith("You decide against drinking this potion, as you are already at full stamina."))
            {
                UOClassicCombatSelf?.ClilocTriggerFSFullStamina();
                return;
            }
            if (text.StartsWith("A tasty bite of the enchanted apple lifts all curses from your soul."))
            {
                UOClassicCombatSelf?.ClilocTriggerEApple();
                return;
            }
            // ## BEGIN - END ## // SELF
        }
        public void OnCliloc(uint cliloc)
        {
            UOClassicCombatSelf UOClassicCombatSelf = UIManager.GetGump<UOClassicCombatSelf>();// ## BEGIN - END ## // SELF
            UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();

            // ## BEGIN - END ## // SELF
            //STOP BANDIES TIMER
            for (int i = 0; i < _stopBandiesAtClilocs.Length; i++)
            {
                if (_stopBandiesAtClilocs[i] == cliloc)
                {
                    UOClassicCombatSelf?.ClilocTriggerStopBandies();
                    return;
                }
            }

            //START BANDIES TIMER
            for (int i = 0; i < _startBandiesAtClilocs.Length; i++)
            {
                if (_startBandiesAtClilocs[i] == cliloc)
                {
                    UOClassicCombatSelf?.ClilocTriggerStartBandies();
                    return;
                }
            }
            // ## BEGIN - END ## // SELF

            //GOT DISARMED
            for (int i = 0; i < _disarmedAtClilocs.Length; i++)
            {
                if (_disarmedAtClilocs[i] == cliloc)
                {
                    UOClassicCombatSelf?.ClilocTriggerGotDisarmed();// ## BEGIN - END ## // SELF
                    UOClassicCombatBuffbar?.ClilocTriggerGotDisarmed();
                    return;
                }
            }
        }
    }
}