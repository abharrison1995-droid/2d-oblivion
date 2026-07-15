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
        ButterRaid // locked until later game
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
            return Build(kind);
        }

        public void ApplyTravelTime(PartyState party, RoadEdgeData edge)
        {
            party.hours += edge.travelHours;
            while (party.hours >= GameConstants.HoursPerDay)
            {
                party.hours -= GameConstants.HoursPerDay;
                party.day++;
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
                title = "Merchant train",
                body = "Oxen, cloth, and gossip from Saltmere.",
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
