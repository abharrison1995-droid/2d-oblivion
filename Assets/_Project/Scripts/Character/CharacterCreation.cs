using System;
using System.Text;

namespace Voidovia
{
    public enum FamilyOrigin
    {
        Diplomats,
        Traders,
        Nomads,
        Healers
    }

    public enum ChildhoodLean
    {
        Horses,
        TradingToys,
        OrganizingTeams,
        StealingFighting
    }

    public enum MooseChoice
    {
        NurseToRelease,
        HealToSell,
        KillForMeat,
        LeaveAlone
    }

    [Serializable]
    public class CharacterCreationResult
    {
        public string heroName = "Wanderer";
        public FamilyOrigin family;
        public ChildhoodLean childhood;
        public MooseChoice moose;

        public int combat = 5;
        public int leadership = 5;
        public int tactics = 5;
        public int trade = 5;
        public int scouting = 5;
        public int startingGold = 80;
        public string spawnNodeId = "millwright";
        public string[] startingTroopIds = Array.Empty<string>();
        public string stolenItemFlavour = "A family keepsake the Butter rogues snatched.";
        public string originSummary;
        public bool horseAffinity;
        public bool healerAffinity;
        public bool wantedLean;
    }

    /// <summary>
    /// Three interview beats → Voidovia start kit + stats.
    /// </summary>
    public static class CharacterCreation
    {
        public static readonly string[] FamilyLabels =
        {
            "Diplomats",
            "Traders",
            "Nomads",
            "Healers"
        };

        public static readonly string[] FamilyBlurbs =
        {
            "Courts, seals, and careful words. You grew up watching people negotiate power.",
            "Ledgers, caravans, and sharp eyes for a deal. Coin was the family language.",
            "Tents, herds, and long roads. Home was wherever the next pasture waited.",
            "Salves, patience, and debts of gratitude. Your kin mended what the world broke."
        };

        public static readonly string[] ChildhoodLabels =
        {
            "Horses",
            "Trading toys at school",
            "Organizing teams",
            "Stealing / fighting"
        };

        public static readonly string[] ChildhoodBlurbs =
        {
            "You haunted the pens and trails. Saddle leather felt like honesty.",
            "Marbles, knives, favours — you always left recess richer.",
            "You decided who stood where, and somehow they listened.",
            "Scraps behind the sheds. You learned to take a hit and return one."
        };

        public static readonly string[] MooseLabels =
        {
            "Nurse it, then one day release it",
            "Heal it so you can sell it",
            "Kill it for meat",
            "Leave it alone — ew, dirty moose"
        };

        public static readonly string[] MooseBlurbs =
        {
            "Mercy first. The village will talk — not all of it kindly.",
            "Compassion with a price tag. Practical, and a little cold.",
            "The animal ends. Your stores don't. Winter logic.",
            "You walk on. Some call it disgust. Some call it sense."
        };

        public static CharacterCreationResult Build(string name, FamilyOrigin family, ChildhoodLean childhood, MooseChoice moose)
        {
            var r = new CharacterCreationResult
            {
                heroName = string.IsNullOrWhiteSpace(name) ? "Wanderer" : name.Trim(),
                family = family,
                childhood = childhood,
                moose = moose,
                combat = 5,
                leadership = 5,
                tactics = 5,
                trade = 5,
                scouting = 5,
                startingGold = 80
            };

            ApplyFamily(r);
            ApplyChildhood(r);
            ApplyMoose(r);
            ClampStats(r);
            BuildSummary(r);
            return r;
        }

        static void ApplyFamily(CharacterCreationResult r)
        {
            switch (r.family)
            {
                case FamilyOrigin.Diplomats:
                    r.leadership += 3;
                    r.trade += 1;
                    r.tactics += 1;
                    r.combat -= 1;
                    r.startingGold = 140;
                    r.spawnNodeId = "greyledger";
                    r.startingTroopIds = new[] { "void_militia", "void_militia" };
                    r.stolenItemFlavour = "A diplomat's seal-ring — Voidovian courts still recognise the cut.";
                    break;
                case FamilyOrigin.Traders:
                    r.trade += 3;
                    r.scouting += 1;
                    r.leadership += 1;
                    r.startingGold = 160;
                    r.spawnNodeId = "saltmere";
                    r.startingTroopIds = new[] { "void_militia", "void_archer" };
                    r.stolenItemFlavour = "A merchant's tally-rod — worthless to bandits, everything to you.";
                    break;
                case FamilyOrigin.Nomads:
                    r.scouting += 3;
                    r.combat += 1;
                    r.tactics += 1;
                    r.trade -= 1;
                    r.startingGold = 70;
                    r.spawnNodeId = "marshend";
                    r.startingTroopIds = new[] { "voidovan_cattle_rustler", "void_militia" };
                    r.stolenItemFlavour = "A braided nomad clasp — marks safe passage among roaming kin.";
                    r.horseAffinity = true;
                    break;
                case FamilyOrigin.Healers:
                    r.leadership += 2;
                    r.trade += 1;
                    r.scouting += 1;
                    r.combat -= 1;
                    r.startingGold = 100;
                    r.spawnNodeId = "millwright";
                    r.startingTroopIds = new[] { "void_militia", "void_militia", "void_archer" };
                    r.stolenItemFlavour = "A healer's bone-charm — half relic, half promise.";
                    r.healerAffinity = true;
                    break;
            }
        }

        static void ApplyChildhood(CharacterCreationResult r)
        {
            switch (r.childhood)
            {
                case ChildhoodLean.Horses:
                    r.scouting += 2;
                    r.combat += 1;
                    r.horseAffinity = true;
                    // Nudge: free light horse seed if not already mounted-heavy
                    if (Array.IndexOf(r.startingTroopIds, "voidovan_cattle_rustler") < 0)
                        r.startingTroopIds = Append(r.startingTroopIds, "voidovan_cattle_rustler");
                    break;
                case ChildhoodLean.TradingToys:
                    r.trade += 2;
                    r.scouting += 1;
                    r.startingGold += 25;
                    break;
                case ChildhoodLean.OrganizingTeams:
                    r.leadership += 2;
                    r.tactics += 2;
                    r.startingTroopIds = Append(r.startingTroopIds, "void_militia");
                    break;
                case ChildhoodLean.StealingFighting:
                    r.combat += 2;
                    r.scouting += 1;
                    r.leadership -= 1;
                    r.wantedLean = true;
                    break;
            }
        }

        static void ApplyMoose(CharacterCreationResult r)
        {
            switch (r.moose)
            {
                case MooseChoice.NurseToRelease:
                    r.leadership += 2;
                    r.healerAffinity = true;
                    break;
                case MooseChoice.HealToSell:
                    r.trade += 2;
                    r.tactics += 1;
                    r.startingGold += 20;
                    break;
                case MooseChoice.KillForMeat:
                    r.combat += 1;
                    r.scouting += 1;
                    // meat handled when applying to party
                    break;
                case MooseChoice.LeaveAlone:
                    r.tactics += 1;
                    r.trade += 1;
                    // colder start: slightly less village warmth later via relation 0 stays
                    break;
            }
        }

        static void ClampStats(CharacterCreationResult r)
        {
            r.combat = Clamp(r.combat);
            r.leadership = Clamp(r.leadership);
            r.tactics = Clamp(r.tactics);
            r.trade = Clamp(r.trade);
            r.scouting = Clamp(r.scouting);
            if (r.startingGold < 30) r.startingGold = 30;
        }

        static int Clamp(int v) => v < 1 ? 1 : (v > 12 ? 12 : v);

        static string[] Append(string[] src, string id)
        {
            var next = new string[src.Length + 1];
            Array.Copy(src, next, src.Length);
            next[src.Length] = id;
            return next;
        }

        static void BuildSummary(CharacterCreationResult r)
        {
            var sb = new StringBuilder();
            sb.Append("Family of ").Append(FamilyLabels[(int)r.family]);
            sb.Append(". Child of ").Append(ChildhoodLabels[(int)r.childhood].ToLowerInvariant());
            sb.Append(". Moose: ").Append(MooseLabels[(int)r.moose]).Append('.');
            r.originSummary = sb.ToString();
        }
    }
}
