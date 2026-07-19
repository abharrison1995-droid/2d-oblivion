using System;
using System.Collections.Generic;

namespace Voidovia
{
    /// <summary>
    /// Faction-vs-faction war/peace state — distinct from the player's own relations. Wars shift on a
    /// slow daily timer (plus a seeded starting conflict) so the strategic map has its own life: AI
    /// warbands march on the enemies their faction is at war with (see WorldPartyDirector), and
    /// settlements can change hands (later Phase 2 work) as those wars play out.
    /// </summary>
    public class FactionDiplomacy
    {
        // The factions that actually wage war. Independent/Healers/Traders/Bandits stay out of it.
        public static readonly FactionId[] Belligerents =
        {
            FactionId.Voidovia, FactionId.ButterKlanBoys, FactionId.RaXaelDynasty,
            FactionId.SmallSpine, FactionId.LongSpines, FactionId.Orthodoxy, FactionId.Nomads
        };

        readonly HashSet<long> _wars = new();

        /// <summary>(a, b, nowAtWar) — fired whenever a pair's war state actually changes.</summary>
        public event Action<FactionId, FactionId, bool> WarStateChanged;

        static long Key(FactionId a, FactionId b)
        {
            var x = (int)a;
            var y = (int)b;
            var lo = x < y ? x : y;
            var hi = x < y ? y : x;
            return ((long)lo << 32) | (uint)hi;
        }

        public bool AreAtWar(FactionId a, FactionId b) => a != b && _wars.Contains(Key(a, b));

        public void SetWar(FactionId a, FactionId b, bool atWar)
        {
            if (a == b) return;
            var key = Key(a, b);
            var changed = atWar ? _wars.Add(key) : _wars.Remove(key);
            if (changed)
                WarStateChanged?.Invoke(a, b, atWar);
        }

        public List<FactionId> EnemiesOf(FactionId faction)
        {
            var list = new List<FactionId>();
            foreach (var other in Belligerents)
                if (AreAtWar(faction, other))
                    list.Add(other);
            return list;
        }

        public bool HasAnyEnemy(FactionId faction)
        {
            foreach (var other in Belligerents)
                if (AreAtWar(faction, other))
                    return true;
            return false;
        }

        /// <summary>Starting conflicts. Voidovia and the Butter Klan are already at each other's throats —
        /// the border war the whole campaign opens on.</summary>
        public void Seed()
        {
            SetWar(FactionId.Voidovia, FactionId.ButterKlanBoys, true);
        }

        /// <summary>Once per day: maybe flip one random belligerent pair between war and peace, so the
        /// diplomatic map keeps moving without descending into everyone-vs-everyone.</summary>
        public void TickDay(System.Random rng)
        {
            if (rng.NextDouble() >= GameConstants.WarStateChangeChancePerDay)
                return;

            var a = Belligerents[rng.Next(Belligerents.Length)];
            var b = Belligerents[rng.Next(Belligerents.Length)];
            if (a == b) return;

            if (AreAtWar(a, b))
            {
                if (rng.NextDouble() < GameConstants.MakePeaceChance)
                    SetWar(a, b, false);
            }
            else if (rng.NextDouble() < GameConstants.DeclareWarChance)
            {
                SetWar(a, b, true);
            }
        }

        // --- Save/load ---

        public List<(FactionId a, FactionId b)> AllWars()
        {
            var list = new List<(FactionId, FactionId)>();
            foreach (var key in _wars)
                list.Add(((FactionId)(int)(key >> 32), (FactionId)(int)(key & 0xffffffff)));
            return list;
        }

        public void RestoreWars(IEnumerable<(FactionId a, FactionId b)> wars)
        {
            _wars.Clear();
            foreach (var (a, b) in wars)
                if (a != b)
                    _wars.Add(Key(a, b));
        }
    }
}
