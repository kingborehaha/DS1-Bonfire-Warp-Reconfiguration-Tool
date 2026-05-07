using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoreSoulsResource
{
    public static class DS1Common
    {
        public static Dictionary<ushort, int> WeaponRecoveryTimeByMotionCategory = new()
        {
            {20, 14},
            {23, 21},
            {25, 31},
            {26, 43},
            {27, 20},
            {28, 18},
            {56, 18},
            {29, 21},
            {30, 30},
            {32, 46},
            {33, 31},
            {35, 53},
            {36, 22},
            {38, 32},
            {42, 20},
            {43, 37},
            {47, 45},
            {48, 29},

            // ranged weapons. not accurate because of draw times and shit
            {44, 22},
            {46, 20},
            {0, 20}, // bolts and arrows

            // spell tools. not accurate, completely made up
            {41, 10},
        };
    }
}
