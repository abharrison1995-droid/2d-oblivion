using System.Collections.Generic;

namespace Voidovia
{
    /// <summary>
    /// Small weighted pools of enemy troop compositions, so repeat fights against the same
    /// kind of enemy (camp raid, lair, road encounter) don't feel identical every time.
    /// </summary>
    public static class BattleForceTables
    {
        public static BattleForce BanditCamp(string displayName, System.Random rng)
        {
            var troops = rng.Next(3) switch
            {
                0 => new List<TroopStack> { new() { troopId = "butter_thug", count = 8 }, new() { troopId = "butter_slinger", count = 3 } },
                1 => new List<TroopStack> { new() { troopId = "butter_thug", count = 10 }, new() { troopId = "butter_raider", count = 2 } },
                _ => new List<TroopStack> { new() { troopId = "butter_thug", count = 9 }, new() { troopId = "butter_slinger", count = 2 }, new() { troopId = "butter_raider", count = 2 } }
            };
            return new BattleForce { name = displayName, faction = FactionId.ButterKlanBoys, troops = troops };
        }

        /// <summary>Modest variance — Act 1's lair is a scripted chief-capture beat, so this stays close to
        /// the tuned difficulty rather than swinging wide. Butter Klan troops: hits hard, breaks early.</summary>
        public static BattleForce Lair(System.Random rng)
        {
            var thugs = rng.Next(6, 9);
            var raiders = rng.Next(2, 5);
            return new BattleForce
            {
                name = "Buttery Lair",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack> { new() { troopId = "butter_thug", count = thugs }, new() { troopId = "butter_raider", count = raiders } }
            };
        }

        public static BattleForce Encounter(TravelEncounterKind kind, System.Random rng) => kind switch
        {
            TravelEncounterKind.MinorThieves => new BattleForce
            {
                name = "Footpads",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack> { new() { troopId = "butter_thug", count = rng.Next(2, 5) } }
            },
            TravelEncounterKind.BanditAmbush => RollAmbush(rng),
            TravelEncounterKind.ButterRaid => new BattleForce
            {
                name = "Butter warband",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack> { new() { troopId = "butter_raider", count = rng.Next(4, 7) }, new() { troopId = "butter_potthrower", count = rng.Next(2, 4) } }
            },
            _ => new BattleForce
            {
                name = "Hostiles",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack> { new() { troopId = "butter_thug", count = rng.Next(1, 3) } }
            }
        };

        static BattleForce RollAmbush(System.Random rng) => rng.Next(3) switch
        {
            0 => new BattleForce { name = "Bandit ambush", faction = FactionId.ButterKlanBoys, troops = new List<TroopStack> { new() { troopId = "butter_thug", count = 5 } } },
            1 => new BattleForce { name = "Bandit ambush", faction = FactionId.ButterKlanBoys, troops = new List<TroopStack> { new() { troopId = "butter_thug", count = 4 }, new() { troopId = "butter_slinger", count = 3 } } },
            _ => new BattleForce { name = "Bandit ambush", faction = FactionId.ButterKlanBoys, troops = new List<TroopStack> { new() { troopId = "butter_thug", count = 6 }, new() { troopId = "butter_raider", count = 1 } } }
        };
    }
}
