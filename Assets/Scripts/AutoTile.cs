using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
namespace Kultie.AutoTileSystem{
    public static class EightBitAutoTile
    {
        public static readonly Dictionary<int, int> NeighborsToAtlasIndex = CreateNeighborsToAtlasIndex();

        public static Dictionary<int, int> CreateNeighborsToAtlasIndex()
        {
            //Very simple - just go through all 256 neighbor combos and find the valid
            //ones, incrementing as we go.
            //By convention, we will say the LSB -> MSB represents clockwise rotation
            //with the LSB being the left spoke "west"
            var result = new Dictionary<int, int>();
            int tileIndex = 0;
            for (int i = 0; i < 256; i++)
            {
                var cornerScores = GetCornerScores(i);
                bool isValid = true;
                for (int j = 0; j < 4 && isValid; j++)
                    isValid = IsValidCornerScore(cornerScores[j]);
                if (isValid)
                {
                    result.Add(i, tileIndex);
                    tileIndex++;
                }
            }
            return result; //Should be 47
        }

        public static int[] GetCornerScores(int neighborBits)
        {
            //Returns an array of "corner" scores, that is the the 3-bit
            //flags for the directions determining that quad in
            //clockwise order.
            //So bit runs 0-2, 2-4, 4-6, 6-"8" clockwise (A,B,D,C)
            //However, the return is in A,B,C,D to make things easier upstream
            int n = (neighborBits << 8) | neighborBits; //extend one copy to wrap easier
            //int n = neighborBits << 8;//extend one copy to wrap easier
            return new int[]
            {
                (n & 0x7),          //A
                ( (n>>2) & 0x7),    //B
                ( (n>>6) & 0x7),    //C
                ( (n>>4) & 0x7)     //D
            };
        }

        private static bool IsValidCornerScore(int score)
        {
            //Bit 0 is a cardinal direction, 1 is the corner, 2 is the next cardinal direction
            //So the corner cannot be on unless both cardinals are on. While this could be
            //done bit-masky, we just statically rule out 110, 011, 010
            return (score != 6 && score != 3 && score != 2);
        }


        private static byte NorthAndWest = 0x05; //-----N-W
        private static byte EastAndNorth = 0x14; //---E-N--
        private static byte SouthAndEast = 0x50; //-S-E----
        private static byte WestAndSouth = 0x41; //-S-----W

        private static byte CornerNWMask = 0xFD; //Not ------C-
        private static byte CornerNEMask = 0xF7; //Not ----C---
        private static byte CornerSEMask = 0xDF; //Not --C-----
        private static byte CornerSWMask = 0x7F; //Not C-------

        public static int GetTileIndex(int neighbors)
        {
            //First, filter the neighbors of invalidly set corners
            //byte filtered = FilterNeighbors((byte)neighbors);
            //Then get the index into the 47-tile set
            return NeighborsToAtlasIndex[neighbors];
        }

        public static byte FilterNeighbors(byte neighbors)
        {
            //One way this could be done is to get each corner score and ensure that if the
            //middle bit is set, then the other two are as well or don't count it
            //Then reassemble the corner scores.
            //However, that involves allocating an array at runtime, so we'll do a similar trick
            //with bit masking
            //So, we have West = 0, NW = 1, etc. clockwise around.
            //We basically want to check that e.g. NW is masked out if W/N aren't set.
            //So we can simply mask out the four corners if the relevant cardinals aren't set unconditionally.
            if ((neighbors & NorthAndWest) != NorthAndWest)
                neighbors &= CornerNWMask;
            if ((neighbors & EastAndNorth) != EastAndNorth)
                neighbors &= CornerNEMask;
            if ((neighbors & SouthAndEast) != SouthAndEast)
                neighbors &= CornerSEMask;
            if ((neighbors & WestAndSouth) != WestAndSouth)
                neighbors &= CornerSWMask;
            return neighbors;
        }
    }

}
