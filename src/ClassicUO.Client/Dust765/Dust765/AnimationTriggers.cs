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
    public class AnimationTriggers
    {
        public void OnOwnCharacterAnimation(ushort action)
        {
            if (action >= 9 && action <= 15 || action == 18 || action == 19 || action >= 26 && action <= 29 || action == 31)
            {
                //26 horse_attack1h_slashright_01 / 27 horse_attackbow_01 / 28 horse_attackcrossbow_01 / 29 horse_attack2h_slashright_01 / 31_punch_punc_jab 01
                //9 attacklslash1h_01 / 10 attackpierce1h_01 / 11 attackbash1h_01 / 12 attackbash2h_01 / 13 attackslash2h_01 / 14 attackpierce2h_01 / 15 combatadvanced_1h_01 / 18 attackbow_01 / 19 attackcrossbow_01

                // ## BEGIN - END ## // MACROS
                CombatCollection._HarmOnSwingTrigger = true;
                // ## BEGIN - END ## // MACROS
                // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
                UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();
                UOClassicCombatBuffbar?.ClilocTriggerSwing();
                // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            }
            return;
        }
        public void OnOwnCharacterAnimationNew(ushort action, ushort type)
        {
            // ## BEGIN - END ## // MACROS
            CombatCollection._HarmOnSwingTrigger = true;
            // ## BEGIN - END ## // MACROS
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();
            UOClassicCombatBuffbar?.ClilocTriggerSwing();
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS

            return;
        }
    }
}