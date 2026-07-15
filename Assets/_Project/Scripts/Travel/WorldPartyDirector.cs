using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    public class WorldParty
    {
        public string id;
        public string displayName;
        public FactionId faction;
        public Vector2 mapPos;
        public int strength;
        public string homeNodeId;
        public string targetNodeId;
        public bool isMajor = true;
    }

    /// <summary>
    /// Other major bands on the strategic map. Visible when near the player or while travelling.
    /// </summary>
    public class WorldPartyDirector
    {
        public readonly List<WorldParty> Parties = new();

        public void SeedVoidovia(WorldGraph map, System.Random rng)
        {
            Parties.Clear();
            Add(map, "patrol_void", "Voidovia Border Patrol", FactionId.Voidovia, "bastion_holt", "ironcauseway", 28);
            Add(map, "butter_rogues", "Butter Rogue Column", FactionId.ButterKlanBoys, "tollbar", "ashpond", 18);
            Add(map, "trader_column", "Saltmere Traders", FactionId.Traders, "saltmere", "greyledger", 10);
            Add(map, "orthodox_scout", "Orthodoxy Scouts", FactionId.Orthodoxy, "ironcauseway", "orthodoxy_bastion", 22);

            foreach (var p in Parties)
            {
                if (!map.TryGetNode(p.homeNodeId, out var n)) continue;
                p.mapPos = n.mapPosition + new Vector2(
                    (float)(rng.NextDouble() * 0.6 - 0.3),
                    (float)(rng.NextDouble() * 0.6 - 0.3));
            }
        }

        public void Tick(float hours)
        {
            foreach (var p in Parties)
            {
                // home/target are nodes; move toward target in map space using stored positions refreshed by UI if needed
                // Directional drift — UI passes target positions via RefreshTargets when graph known
            }
        }

        public void TickTowardTargets(WorldGraph map, float hours)
        {
            foreach (var p in Parties)
            {
                if (!map.TryGetNode(p.targetNodeId, out var target)) continue;
                var dir = target.mapPosition - p.mapPos;
                if (dir.magnitude < 0.2f)
                {
                    var swap = p.homeNodeId;
                    p.homeNodeId = p.targetNodeId;
                    p.targetNodeId = swap;
                    continue;
                }

                var step = Mathf.Clamp01(hours * 0.045f);
                p.mapPos += dir.normalized * step;
            }
        }

        public List<WorldParty> VisibleNear(Vector2 playerMapPos, float radius = 2.2f)
        {
            var list = new List<WorldParty>();
            foreach (var p in Parties)
            {
                if ((p.mapPos - playerMapPos).sqrMagnitude <= radius * radius)
                    list.Add(p);
            }

            return list;
        }

        void Add(WorldGraph map, string id, string name, FactionId fac, string home, string target, int str)
        {
            if (!map.TryGetNode(home, out var n)) return;
            if (!map.TryGetNode(target, out _)) return;
            Parties.Add(new WorldParty
            {
                id = id,
                displayName = name,
                faction = fac,
                homeNodeId = home,
                targetNodeId = target,
                mapPos = n.mapPosition,
                strength = str
            });
        }
    }
}
