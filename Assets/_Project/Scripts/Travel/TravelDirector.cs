using System;
using UnityEngine;

namespace Voidovia
{
    public enum TravelEncounterKind
    {
        None,
        Rumour,
        Trader,
        Healers,
        MinorThieves,
        Refugees,
        Weather,
        BanditAmbush,
        ButterRaid, // locked until later game
        VoidoviaPatrol // hunts the player while Wanted in Voidovia
    }

    [Serializable]
    public class TravelEncounter
    {
        public TravelEncounterKind kind;
        public string title;
        public string body;
        public bool canFight;
        public bool canFlee = true;
        public bool canTalk;
        public bool canPay;
        public bool friendlyBandits;

        /// <summary>Rolled once when the encounter is generated so the pre-fight surrender/count
        /// preview and the actual battle use the same force instead of rolling twice.</summary>
        public BattleForce cachedForce;

        /// <summary>Caravan cargo (Trader encounters): the coin and trade goods you'd loot by raiding, or
        /// the goods offered when you trade peacefully. Rolled once at generation.</summary>
        public int caravanGold;
        public string caravanGoodId;
        public int caravanGoodCount;
    }

    /// <summary>
    /// KCD-style journey: play route edges, roll interrupts, advance time.
    /// Severe Butter raids gated behind campaign flag.
    /// </summary>
    public class TravelDirector
    {
        public bool SevereRaidsUnlocked { get; set; }

        static readonly TravelEncounterKind[] LightTable =
        {
            TravelEncounterKind.None,
            TravelEncounterKind.None,
            TravelEncounterKind.Rumour,
            TravelEncounterKind.Trader,
            TravelEncounterKind.Weather,
            TravelEncounterKind.MinorThieves,
            TravelEncounterKind.Refugees,
            TravelEncounterKind.Healers,
            TravelEncounterKind.BanditAmbush
        };

        /// <summary>Off-path table: almost entirely hostile/negative kinds — no maintained
        /// road means no toll-paying traders or friendly rumour-mongers out here.</summary>
        static readonly TravelEncounterKind[] OffPathTable =
        {
            TravelEncounterKind.MinorThieves,
            TravelEncounterKind.MinorThieves,
            TravelEncounterKind.BanditAmbush,
            TravelEncounterKind.BanditAmbush,
            TravelEncounterKind.Weather,
            TravelEncounterKind.Weather,
            TravelEncounterKind.Rumour,
            TravelEncounterKind.Refugees
        };

        public TravelEncounter RollEncounter(RoadEdgeData edge, System.Random rng)
        {
            if (edge == null)
                return None();

            var roll = rng.NextDouble();
            if (roll > edge.danger)
                return None();

            if (SevereRaidsUnlocked && edge.allowSevereRaids && rng.NextDouble() < 0.35)
            {
                return new TravelEncounter
                {
                    kind = TravelEncounterKind.ButterRaid,
                    title = "Butter banners on the ridge",
                    body = "A proper Butter warband cuts across the road. This is no rogue scrap.",
                    canFight = true,
                    canFlee = true,
                    canTalk = false,
                    canPay = true
                };
            }

            var kind = LightTable[rng.Next(LightTable.Length)];
            var encounter = Build(kind);
            encounter.cachedForce = BattleForceTables.Encounter(kind, rng);
            ApplyCaravanCargo(encounter, rng);
            ApplyBanditFriendliness(encounter);
            return encounter;
        }

        /// <summary>Fills a caravan's cargo (coin + trade goods). Cargo scales up a little so a raid can be
        /// a real payday — at the cost of the infamy that comes with robbing the realm's roads.</summary>
        static void ApplyCaravanCargo(TravelEncounter e, System.Random rng)
        {
            if (e.kind != TravelEncounterKind.Trader) return;
            e.caravanGold = rng.Next(40, 121);
            e.caravanGoodId = "grain_trade";
            e.caravanGoodCount = rng.Next(2, 5);
        }

        /// <summary>Rolled for off-path steps instead of RollEncounter — same danger gate, hostile-biased table.</summary>
        public TravelEncounter RollOffPathEncounter(RoadEdgeData edge, System.Random rng)
        {
            if (edge == null)
                return None();

            var roll = rng.NextDouble();
            if (roll > edge.danger)
                return None();

            var kind = OffPathTable[rng.Next(OffPathTable.Length)];
            var encounter = Build(kind);
            encounter.cachedForce = BattleForceTables.Encounter(kind, rng);
            ApplyBanditFriendliness(encounter);
            return encounter;
        }

        /// <summary>A Voidovia patrol that runs down a wanted outlaw. Fight them (and darken your name
        /// further), bribe past, or run. Fielded with real Void troops, so a win captures Void soldiers.</summary>
        public TravelEncounter RollWantedPatrol(System.Random rng)
        {
            return new TravelEncounter
            {
                kind = TravelEncounterKind.VoidoviaPatrol,
                title = "Voidovia patrol",
                body = "Lord Void's riders wheel across the road — they have your description. \"Stand and answer for your crimes, outlaw.\"",
                canFight = true,
                canFlee = true,
                canTalk = false,
                canPay = true,
                cachedForce = BattleForceTables.VoidoviaPatrol(rng)
            };
        }

        static void ApplyBanditFriendliness(TravelEncounter e)
        {
            if (e.kind != TravelEncounterKind.MinorThieves && e.kind != TravelEncounterKind.BanditAmbush)
                return;

            var party = GameState.Instance?.Party;
            if (party == null || party.GetRelation(FactionId.Bandits) < GameConstants.BanditFriendlyRelationThreshold)
                return;

            e.friendlyBandits = true;
            e.title = e.kind == TravelEncounterKind.BanditAmbush ? "Bandit banners, raised in greeting" : "Familiar footpads";
            e.body = "These raiders know your name — and count you a friend. They wave you through, no toll asked.";
            e.canFight = false;
            e.canFlee = true;
            e.canTalk = true;
            e.canPay = false;
        }

        public void ApplyTravelTime(PartyState party, RoadEdgeData edge)
        {
            party.hours += edge.travelHours;
            while (party.hours >= GameConstants.HoursPerDay)
            {
                party.hours -= GameConstants.HoursPerDay;
                party.day++;
                GameState.Instance?.OnNewDay();
            }
        }

        static TravelEncounter None() => new()
        {
            kind = TravelEncounterKind.None,
            title = string.Empty,
            body = string.Empty
        };

        static TravelEncounter Build(TravelEncounterKind kind) => kind switch
        {
            TravelEncounterKind.Rumour => new TravelEncounter
            {
                kind = kind,
                title = "Road rumour",
                body = "A drover mutters about Butter boys near Tollbar.",
                canTalk = true
            },
            TravelEncounterKind.Trader => new TravelEncounter
            {
                kind = kind,
                title = "Merchant caravan",
                body = "Oxen, cloth, and coin bound between markets, a few hired guards riding alongside. Trade with them — or take it all.",
                canFight = true, // raid the caravan (fight its guards)
                canFlee = true,
                canTalk = true,
                canPay = true
            },
            TravelEncounterKind.Healers => new TravelEncounter
            {
                kind = kind,
                title = "Healer circle",
                body = "Traveling healers offer salves for coin or favour.",
                canTalk = true,
                canPay = true
            },
            TravelEncounterKind.MinorThieves => new TravelEncounter
            {
                kind = kind,
                title = "Footpads in the brush",
                body = "A handful of starving thieves try their luck.",
                canFight = true,
                canFlee = true,
                canPay = true
            },
            TravelEncounterKind.Refugees => new TravelEncounter
            {
                kind = kind,
                title = "Refugees on the verge",
                body = "Families flee a burnt steading. They ask for bread.",
                canTalk = true,
                canPay = true
            },
            TravelEncounterKind.Weather => new TravelEncounter
            {
                kind = kind,
                title = "Hard rain",
                body = "The road turns to soup. Travel slows.",
                canFlee = false,
                canTalk = true
            },
            TravelEncounterKind.BanditAmbush => new TravelEncounter
            {
                kind = kind,
                title = "Bandit ambush",
                body = "An organized band blocks the road, weapons ready, hands out for toll.",
                canFight = true,
                canFlee = true,
                canPay = true
            },
            _ => None()
        };
    }
}
