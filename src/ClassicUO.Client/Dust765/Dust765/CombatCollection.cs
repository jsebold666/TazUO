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

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using ClassicUO.Resources;

using ClassicUO.Network;
using static ClassicUO.Network.NetClient;

using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace ClassicUO.Dust765.Dust765
{
    internal static class CombatCollection
    {
        //USED CONSTANTS
        // ## BEGIN - END ## // ART / HUE CHANGES
        public const ushort GOLD1_REPLACE_GRAPHIC = 0x0E73; //new - cannonball, old: skillball: 0x5740
        public const ushort GOLD2_REPLACE_GRAPHIC = 0x4202;
        public const ushort TREE_REPLACE_GRAPHIC = 0x0E59;
        public const ushort TREE_REPLACE_GRAPHIC_TILE = 0x07BD;
        public const ushort BLOCKER_REPLACE_GRAPHIC_TILE = 0x07BD;
        public const ushort BLOCKER_REPLACE_GRAPHIC_STUMP = 0x0E56;
        public const ushort BLOCKER_REPLACE_GRAPHIC_ROCK = 0x1775;
        public const ushort BRIGHT_WHITE_COLOR = 0x080A;
        public const ushort BRIGHT_PINK_COLOR = 0x0503;
        public const ushort BRIGHT_ICE_COLOR = 0x0480;
        public const ushort BRIGHT_FIRE_COLOR = 0x0496;
        public const ushort BRIGHT_POISON_COLOR = 0x0A0B;
        public const ushort BRIGHT_PARALYZE_COLOR = 0x0A13;
        // ## BEGIN - END ## // ART / HUE CHANGES

        // ## BEGIN - END ## // ART / HUE CHANGES
        //GAME\SCENES\GAMESCENEDRAWINGSORTING.CS
        public static Static GSDSFilters(Static st)
        {
            if (StaticFilters.IsTree(st.OriginalGraphic, out int index)) //INDEX?
            {
                if (ProfileManager.CurrentProfile.ColorTreeTile)
                    st.Hue = ProfileManager.CurrentProfile.TreeTileHue;
                else
                    st.RestoreOriginalHue();
            }
            if (IsBlockerTreeArt(st.OriginalGraphic) || IsBlockerStoneArt(st.OriginalGraphic))
            {
                if (ProfileManager.CurrentProfile.ColorBlockerTile)
                    st.Hue = ProfileManager.CurrentProfile.BlockerTileHue;
                else
                    st.RestoreOriginalHue();
            }
            return st;
        }

        //GAMEOBJECT\VIEWS\STATICVIEW.CS
        public static ushort ArtloaderFilters(ushort graphic)
        {
            if (StaticFilters.IsTree(graphic, out _))
            {
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TreeType == 1)
                {
                    graphic = TREE_REPLACE_GRAPHIC;
                }
                else if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TreeType == 2)
                {
                    graphic = TREE_REPLACE_GRAPHIC_TILE;
                }
            }
            if (IsBlockerTreeArt(graphic))
            {
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 1)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_STUMP;
                }
                else if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 2)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_TILE;
                }
            }
            if (IsBlockerStoneArt(graphic))
            {
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 1)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_ROCK;
                }
                else if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 2)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_TILE;
                }
            }
            return graphic;
        }

        //GAME\GAMEOBJECTS\VIEWS\GAMEEFFECTVIEW.CS
        public static ushort EnergyBoltArt(ushort graphic)
        {
            if (ProfileManager.CurrentProfile.EnergyBoltArtType == 1)
                graphic = 0x36FE;
            else if (ProfileManager.CurrentProfile.EnergyBoltArtType == 2)
                graphic = 0x2256;

            return graphic;
        }
        public static bool IsChangedEnergyBoltArt(ushort graphic)
        {
            if (ProfileManager.CurrentProfile.EnergyBoltArtType == 1 & graphic == 0x36FE)
                return true;
            else if (ProfileManager.CurrentProfile.EnergyBoltArtType == 2 & graphic == 0x2256)
                return true;

            return false;
        }
        public static ushort EnergyBoltHue(ushort hue)
        {
            if (ProfileManager.CurrentProfile.ColorEnergyBolt)
                hue = ProfileManager.CurrentProfile.EnergyBoltHue;

            if (ProfileManager.CurrentProfile.EnergyBoltNeonType == 1)
                hue = BRIGHT_WHITE_COLOR;
            else if (ProfileManager.CurrentProfile.EnergyBoltNeonType == 2)
                hue = BRIGHT_PINK_COLOR;
            else if (ProfileManager.CurrentProfile.EnergyBoltNeonType == 3)
                hue = BRIGHT_ICE_COLOR;
            else if (ProfileManager.CurrentProfile.EnergyBoltNeonType == 4)
                hue = BRIGHT_FIRE_COLOR;

            return hue;
        }

        //GAME\GAMEOBJECTS\ITEM.CS
        public static ushort GoldArt(ushort graphic)
        {
            if (ProfileManager.CurrentProfile.GoldType == 1) // skillball
            {
                graphic = GOLD1_REPLACE_GRAPHIC;
            }
            else if (ProfileManager.CurrentProfile.GoldType == 2) // prevcoin)
            {
                graphic = GOLD2_REPLACE_GRAPHIC;
            }

            return graphic;
        }
        public static ushort GoldHue(ushort hue)
        {
            if (ProfileManager.CurrentProfile.ColorGold)
                hue = ProfileManager.CurrentProfile.GoldHue;

            return hue;
        }

        //GAME\GAMEOBJECTS\VIEWS\ITEMVIEW.CS
        public static ushort StealthtHue(ushort hue)
        {
            if (ProfileManager.CurrentProfile.ColorStealth)
                hue = ProfileManager.CurrentProfile.StealthHue;

            if (ProfileManager.CurrentProfile.StealthNeonType == 1)
                hue = BRIGHT_WHITE_COLOR;
            else if (ProfileManager.CurrentProfile.StealthNeonType == 2)
                hue = BRIGHT_PINK_COLOR;
            else if (ProfileManager.CurrentProfile.StealthNeonType == 3)
                hue = BRIGHT_ICE_COLOR;
            else if (ProfileManager.CurrentProfile.StealthNeonType == 4)
                hue = BRIGHT_FIRE_COLOR;

            return hue;
        }

        [MethodImpl(256)]
        public static bool IsBlockerTreeArt(ushort g)
        {
            switch (g)
            {
                case 0x1772:
                case 0x177A:
                case 0xC2D:
                case 0xC99:
                case 0xC9B:
                case 0xC9C:
                case 0xC9D:
                case 0xCA6:
                case 0xCC4:

                    return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public static bool IsBlockerStoneArt(ushort g)
        {
            switch (g)
            {
                case 0x1363:
                case 0x1364:
                case 0x1365:
                case 0x1366:
                case 0x1367:
                case 0x1368:
                case 0x1369:
                case 0x136A:
                case 0x136B:
                case 0x136C:
                case 0x136D:

                    return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public static bool IsStealthArt(ushort g)
        {
            switch (g)
            {
                case 0x1E03:
                case 0x1E04:
                case 0x1E05:
                case 0x1E06:

                    return true;
            }

            return false;
        }
        // ## BEGIN - END ## // ART / HUE CHANGES
        // ## BEGIN - END ## // VISUAL HELPERS
        //GAME\GAMEOBJECTS\VIEWS\MOBILEVIEW.CS
        public static ushort OwnAuraColorByHP()
        {
            ushort color = 0x0044;

            int ww = World.Player.HitsMax;

            if (ww > 0)
            {
                ww = World.Player.Hits * 100 / ww;

                if (ww > 100)
                    ww = 100;
                else if (ww < 1)
                    ww = 0;

                World.Player.UpdateHits((byte)ww);
            }

            if (World.Player.HitsPercentage < 20)
                color = 0x0021; //red
            else if (World.Player.HitsPercentage < 40)
                color = 0x002B; //red orange
            else if (World.Player.HitsPercentage < 60)
                color = 0x0030; //orange
            else if (World.Player.HitsPercentage < 80)
                color = 0x0035; //yellow (but shows green?)
            else if (World.Player.HitsPercentage == 100)
                color = (ushort)Notoriety.GetHue(World.Player.NotorietyFlag);
            // "original colors from overhead %, < 30 0x0021, < 50 0x0030, < 80 0x0058"

            return color;
        }
        public static ushort WeaponsHue(ushort hue)
        {
            if (ProfileManager.CurrentProfile.GlowingWeaponsType == 1)
                hue = BRIGHT_WHITE_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 2)
                hue = BRIGHT_PINK_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 3)
                hue = BRIGHT_ICE_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 4)
                hue = BRIGHT_FIRE_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 5)
                hue = ProfileManager.CurrentProfile.HighlightGlowingWeaponsTypeHue;

            return hue;
        }
        public static ushort LastTargetHue(Mobile mobile, ushort hue)
        {
            if (ProfileManager.CurrentProfile.HighlightLastTargetType == 1)
                hue = BRIGHT_WHITE_COLOR;
            else if (ProfileManager.CurrentProfile.HighlightLastTargetType == 2)
                hue = BRIGHT_PINK_COLOR;
            else if (ProfileManager.CurrentProfile.HighlightLastTargetType == 3)
                hue = BRIGHT_ICE_COLOR;
            else if (ProfileManager.CurrentProfile.HighlightLastTargetType == 4)
                hue = BRIGHT_FIRE_COLOR;
            else if (ProfileManager.CurrentProfile.HighlightLastTargetType == 5)
                hue = ProfileManager.CurrentProfile.HighlightLastTargetTypeHue;

            if (mobile.IsPoisoned)
            {
                if (ProfileManager.CurrentProfile.HighlightLastTargetTypePoison == 1)
                    hue = BRIGHT_WHITE_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePoison == 2)
                    hue = BRIGHT_PINK_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePoison == 3)
                    hue = BRIGHT_ICE_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePoison == 4)
                    hue = BRIGHT_FIRE_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePoison == 5)
                    hue = BRIGHT_POISON_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePoison == 6)
                    hue = ProfileManager.CurrentProfile.HighlightLastTargetTypePoisonHue;
            }

            if (mobile.IsParalyzed)
            {
                if (ProfileManager.CurrentProfile.HighlightLastTargetTypePara == 1)
                    hue = BRIGHT_WHITE_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePara == 2)
                    hue = BRIGHT_PINK_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePara == 3)
                    hue = BRIGHT_ICE_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePara == 4)
                    hue = BRIGHT_FIRE_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePara == 5)
                    hue = BRIGHT_PARALYZE_COLOR;
                else if (ProfileManager.CurrentProfile.HighlightLastTargetTypePara == 6)
                    hue = ProfileManager.CurrentProfile.HighlightLastTargetTypeParaHue;
            }

            return hue;
        }

        //GAME\GAMECURSOR.CS
        public static void UpdateSpelltime()
        {
            GameCursor._spellTime = 30 - ((Time.Ticks - GameCursor._startSpellTime) / 1000); // count down
        }
        public static void StartSpelltime()
        {
            GameCursor._startSpellTime = Time.Ticks;
        }

        //GAME\GAMEOBJECTS\VIEWS\ - MULTIVIEW.CS - STATICVIEW.CS - TILEVIEW.CS
        //24 - Wall of Stone
        //28 - Fire Field
        //39 - Poison Field
        //47 - Paralyze Field
        //50 - Energy Field
        //-
        //48 - Reveal
        //49 - Chain Lightning
        //55 - Meteor Swarm
        public static bool MultiFieldPreview(Multi obj)
        {
            if (GameCursor._spellTime >= 1)
            {
                if (GameActions.LastSpellIndexCursor == 24)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 28 || GameActions.LastSpellIndexCursor == 39 || GameActions.LastSpellIndexCursor == 47 || GameActions.LastSpellIndexCursor == 50)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 48 || GameActions.LastSpellIndexCursor == 49 || GameActions.LastSpellIndexCursor == 55)
                {
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + -22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool StaticFieldPreview(Static obj)
        {
            if (GameCursor._spellTime >= 1)
            {
                if (GameActions.LastSpellIndexCursor == 24)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {

                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 28 || GameActions.LastSpellIndexCursor == 39 || GameActions.LastSpellIndexCursor == 47 || GameActions.LastSpellIndexCursor == 50)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 48 || GameActions.LastSpellIndexCursor == 49 || GameActions.LastSpellIndexCursor == 55)
                {
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + -22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool LandFieldPreview(Land obj)
        {
            if (GameCursor._spellTime >= 1)
            {
                if (GameActions.LastSpellIndexCursor == 24)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {

                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {

                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 28 || GameActions.LastSpellIndexCursor == 39 || GameActions.LastSpellIndexCursor == 47 || GameActions.LastSpellIndexCursor == 50)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 48 || GameActions.LastSpellIndexCursor == 49 || GameActions.LastSpellIndexCursor == 55)
                {
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + -22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool MobileFieldPreview(Mobile obj)
        {
            if (GameCursor._spellTime >= 1)
            {
                if (GameActions.LastSpellIndexCursor == 24)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {

                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {

                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 28 || GameActions.LastSpellIndexCursor == 39 || GameActions.LastSpellIndexCursor == 47 || GameActions.LastSpellIndexCursor == 50)
                {
                    //Calc _fieldEastToWest
                    if (SelectedObject.Object == obj)
                    {
                        int PlayerX = World.Player.X;
                        int PlayerY = World.Player.Y;

                        int dx = PlayerX - obj.X;
                        int dy = PlayerY - obj.Y;

                        int rx = (dx - dy) * 44;
                        int ry = (dx + dy) * 44;

                        if (rx >= 0 && ry >= 0)
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                        else if (rx >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else if (ry >= 0)
                        {
                            GameCursor._fieldEastToWest = true;
                        }
                        else
                        {
                            GameCursor._fieldEastToWest = false;
                        }
                    }

                    if (GameCursor._fieldEastToWest is true)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                    else if (GameCursor._fieldEastToWest is false)
                    {
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                        if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                        {
                            return true;
                        }
                    }
                }
                if (GameActions.LastSpellIndexCursor == 48 || GameActions.LastSpellIndexCursor == 49 || GameActions.LastSpellIndexCursor == 55)
                {
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + -22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 0) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 88) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 66) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 88) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 0) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 66) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X - 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y + 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 22) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 22) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                    if (SelectedObject.Object != null && (SelectedObject.Object.RealScreenPosition.X + 44) == obj.RealScreenPosition.X && (SelectedObject.Object.RealScreenPosition.Y - 44) == obj.RealScreenPosition.Y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //NETWORK\PACKETHANDLERS.CS
        public static void SpellCastFromCliloc(string text)
        {
            if (SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell))
            {
                GameActions.LastSpellIndexCursor = spell.ID;
            }
            else
            {
                //THIS IS INCASE RAZOR OR ANOTHER ASSISTANT REWRITES THE STRING

                foreach (var key in SpellDefinition.WordToTargettype.Keys)
                {
                    if (text.Contains(key)) //SPELL FOUND
                    {
                        GameActions.LastSpellIndexCursor = SpellDefinition.WordToTargettype[key].ID;

                        //break; //DONT BREAK LOOP BECAUSE OF IN NOX / IN NOX GRAV
                    }
                }
            }
        }
        // ## BEGIN - END ## // VISUAL HELPERS

    }
}