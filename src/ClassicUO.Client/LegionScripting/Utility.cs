using System.Collections.Generic;
using System.ComponentModel;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting
{
    internal static class Utility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gfx">Graphic to match</param>
        /// <param name="parentContainer">Matches *only* the parent container, not root **Don't use different continer params together**</param>
        /// <param name="rootContainer"></param>
        /// <param name="parOrRootContainer"></param>
        /// <param name="hue">Hue to match</param>
        /// <param name="groundRange">Distance from player</param>
        /// <returns></returns>
        public static List<Item> FindItems(
            uint gfx = uint.MaxValue,
            uint parentContainer = uint.MaxValue,
            uint rootContainer = uint.MaxValue,
            uint parOrRootContainer = uint.MaxValue,
            ushort hue = ushort.MaxValue,
            int groundRange = int.MaxValue
            )
        {
            List<Item> list = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (gfx != uint.MaxValue && item.Graphic != gfx)
                    continue;

                if (parentContainer != uint.MaxValue && item.Container != parentContainer)
                    continue;

                if (rootContainer != uint.MaxValue && item.RootContainer != rootContainer)
                    continue;

                if (parOrRootContainer != uint.MaxValue && (item.Container != parOrRootContainer && item.RootContainer != parOrRootContainer))
                    continue;

                if (hue != ushort.MaxValue && item.Hue != hue)
                    continue;

                if (groundRange != int.MaxValue && item.Distance > groundRange)
                    continue;

                list.Add(item);
            }

            return list;
        }

        public static uint ContentsCount(Item container)
        {
            if (container == null) return 0;

            uint c = 0;
            for (LinkedObject i = container.Items; i != null; i = i.Next)
                c++;

            return c;
        }
    }
}
