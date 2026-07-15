using System;

namespace Voidovia
{
    [Serializable]
    public class OriginChoice
    {
        public string id;
        public string displayName;
        public string blurb;
        public int combat;
        public int leadership;
        public int tactics;
        public int trade;
        public int scouting;
        public int startingGold;
        public string spawnNodeId;
        public string[] startingTroopIds = Array.Empty<string>();
        public string stolenItemFlavour;
    }

    public class CharacterStats
    {
        public string name = "Wanderer";
        public string originId;
        public int combat = 5;
        public int leadership = 5;
        public int tactics = 5;
        public int trade = 5;
        public int scouting = 5;

        public void ApplyOrigin(OriginChoice origin)
        {
            originId = origin.id;
            combat = origin.combat;
            leadership = origin.leadership;
            tactics = origin.tactics;
            trade = origin.trade;
            scouting = origin.scouting;
        }
    }

    public static class OriginCatalog
    {
        public static readonly OriginChoice[] VoidoviaStarts =
        {
            new()
            {
                id = "toll_clerk",
                displayName = "Toll Clerk's Son",
                blurb = "Raised on ledgers and Greyledger gossip. Soft hands, sharp numbers.",
                combat = 3, leadership = 6, tactics = 4, trade = 8, scouting = 4,
                startingGold = 180,
                spawnNodeId = "greyledger",
                startingTroopIds = new[] { "void_militia", "void_militia" },
                stolenItemFlavour = "Your father's seal-ring — proof in any toll house."
            },
            new()
            {
                id = "mill_levy",
                displayName = "Mill Levy",
                blurb = "Drafted from Millwright's fields. Tough, trusted in villages.",
                combat = 6, leadership = 5, tactics = 3, trade = 3, scouting = 5,
                startingGold = 60,
                spawnNodeId = "millwright",
                startingTroopIds = new[] { "void_militia", "void_militia", "void_militia", "void_archer" },
                stolenItemFlavour = "A battered family seax — not fine, but yours."
            },
            new()
            {
                id = "red_knoll_sellsword",
                displayName = "Red Knoll Sellsword",
                blurb = "Muster-yard muscle. Better kit, colder welcome.",
                combat = 8, leadership = 4, tactics = 6, trade = 3, scouting = 4,
                startingGold = 90,
                spawnNodeId = "red_knoll",
                startingTroopIds = new[] { "trained_void_militia", "void_archer" },
                stolenItemFlavour = "A paid-for blade with a Red Knoll stamp."
            },
            new()
            {
                id = "marsh_outlaw",
                displayName = "Marsh Outlaw",
                blurb = "Saltmere reeds and bad decisions. Fast on the road, watched in town.",
                combat = 5, leadership = 3, tactics = 5, trade = 4, scouting = 8,
                startingGold = 40,
                spawnNodeId = "saltmere",
                startingTroopIds = new[] { "void_militia", "voidovan_cattle_rustler" },
                stolenItemFlavour = "A smugglers' clasp — opens doors, and warrants."
            }
        };
    }
}
