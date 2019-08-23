using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kultie.AutoTileSystem
{
    public class VXAutoTile
    {
        private static readonly Dictionary<int, int> VXQuarterTileLookup = new Dictionary<int, int>()
        {
            //Put a key for each tile by what quadrant A-D it is
            //and the "corner" score (see below)
                                      { 0xA5, 2 }, { 0xB5, 3 },
                                      { 0xC5, 6 }, { 0xD5, 7 },

            { 0xA0, 8 }, { 0xB4, 9 }, { 0xA1, 10}, { 0xB0, 11},
            { 0xC1, 12 },{ 0xD7, 13}, { 0xC7, 14 },{ 0xD4, 15},

            { 0xA4, 16}, { 0xB7, 17}, { 0xA7, 18}, { 0xB1, 19},
            { 0xC0, 20 },{ 0xD1, 21}, { 0xC4, 22 },{ 0xD0, 23}
        };

        private static int[] GetAtlasQuarterTilesForNeighborMask(int neighbors)
        {
            //Each tile in the atlas is composed of 4 quarter tiles,
            //call them by quadrant:
            //  A B
            //  C D
            //The VXQuarterTileLookup matches the requested quadrant and corner score
            //to the quarter tile index inside the VX AutoTile texture, numbering like this
            //  0  1  2  3
            //  4  5  6  7
            //  8  9 10 11
            // 12 13 14 15
            // 16 17 18 19
            // 20 21 22 23
            //Where 0,1,4,5 are not used, 2,3,6,7 are the "270 degree" tiles
            //and 8-23 are the main pattern
            var c = EightBitAutoTile.GetCornerScores(neighbors);
            return new int[]
            {
                VXQuarterTileLookup[0xA0 + c[0]],
                VXQuarterTileLookup[0xB0 + c[1]],
                VXQuarterTileLookup[0xC0 + c[2]],
                VXQuarterTileLookup[0xD0 + c[3]]
            };
        }

        public static int[] GetTile(int neighbors)
        {
            var key = EightBitAutoTile.FilterNeighbors((byte)neighbors);
            var vxQuarterTiles = GetAtlasQuarterTilesForNeighborMask(key);
            for (int i = 0; i < 4; i++)
            {
                int vxQIndex = vxQuarterTiles[i];
            }
            return vxQuarterTiles;
        }
    }
}
