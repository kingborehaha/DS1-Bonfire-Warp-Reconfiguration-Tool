using System.Diagnostics;

using SoulsFormats;

namespace BoreSoulsResource
{
    public static class MsbCommon
    {

        public static List<int> CorpseFlatAnimIds = new()
        {
            1,
            12,
            20,
            21,
            70,
            71,
            81,
            82,
            83,
        };

        public static void HandleDupeEntityIds(MSB1 msb)
        {
            HashSet<int> ids = new();
            foreach (var x in msb.Regions.GetEntries())
            {
                if (x.EntityID <= 0)
                    continue;

                if (!ids.Add(x.EntityID))
                {
                    Debug.WriteLine($"{x.Name}: Dupe entity ID: {x.EntityID}");
                    x.EntityID = -1;
                }
            }
            foreach (var x in msb.Parts.GetEntries())
            {
                if (x.EntityID <= 0)
                    continue;

                if (!ids.Add(x.EntityID))
                {
                    Debug.WriteLine($"{x.Name}: Dupe entity ID: {x.EntityID}");
                    x.EntityID = -1;
                }
            }
            foreach (var x in msb.Events.GetEntries())
            {
                if (x.EntityID <= 0)
                    continue;

                if (!ids.Add(x.EntityID))
                {
                    Debug.WriteLine($"{x.Name}: Dupe entity ID: {x.EntityID}");
                    x.EntityID = -1;
                }
            }
        }

    }
}
