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
        /// <summary>Fixed muster point this band retreats to when battered — never reassigned like homeNodeId.</summary>
        public string spawnNodeId;
        public bool isMajor = true;
    }

    /// <summary>
    /// Other major bands on the strategic map. Visible when near the player or while travelling.
    /// </summary>
    public class WorldPartyDirector
    {
        /// <summary>Map distances were rescaled ~2.2x from their original tuning; visibility
        /// radii and patrol speed below scale the same way to keep the same relative feel.</summary>
        public const float MapScaleFactor = 2.2f;

        public readonly List<WorldParty> Parties = new();

        public void SeedVoidovia(WorldGraph map, System.Random rng)
        {
            Parties.Clear();
            Add(map, "patrol_void", "Voidovia Border Patrol", FactionId.Voidovia, "bastion_holt", "ironcauseway", 28);
            Add(map, "butter_rogues", "Butter Rogue Column", FactionId.ButterKlanBoys, "tollbar", "ashpond", 18);
            Add(map, "trader_column", "Voi-D-Nee Traders", FactionId.Traders, "saltmere", "greyledger", 10);
            Add(map, "orthodox_scout", "Orthodoxy Scouts", FactionId.Orthodoxy, "ironcauseway", "orthodoxy_bastion", 22);
            Add(map, "smallspine_host", "Small Spine Host", FactionId.SmallSpine, "smallspine_hub", "smallspine_watch", 24);
            Add(map, "longspine_host", "Long Spines Host", FactionId.LongSpines, "longspine_hub", "longspine_reach", 24);

            foreach (var p in Parties)
            {
                if (!map.TryGetNode(p.homeNodeId, out var n)) continue;
                p.mapPos = n.mapPosition + new Vector2(
                    (float)(rng.NextDouble() * MapScaleFactor * 0.6 - MapScaleFactor * 0.3),
                    (float)(rng.NextDouble() * MapScaleFactor * 0.6 - MapScaleFactor * 0.3));
            }
        }

        public void TickTowardTargets(WorldGraph map, float hours)
        {
            var diplo = GameState.Instance?.Diplomacy;

            foreach (var p in Parties)
            {
                // Warring bands march on an enemy settlement; if the current target isn't one, re-pick.
                if (diplo != null && diplo.HasAnyEnemy(p.faction) && !IsEnemyControlled(map, diplo, p, p.targetNodeId))
                    RetargetToNearestEnemy(map, diplo, p, null);

                if (!map.TryGetNode(p.targetNodeId, out var target)) continue;

                var dir = target.mapPosition - p.mapPos;
                if (dir.magnitude < 0.2f * MapScaleFactor)
                {
                    OnReachTarget(map, diplo, p, target);
                    continue;
                }

                var step = Mathf.Clamp(hours * 0.045f * MapScaleFactor, 0f, MapScaleFactor);
                p.mapPos = Vector2.MoveTowards(p.mapPos, target.mapPosition, step);
            }

            ResolveSkirmishes(diplo);
        }

        /// <summary>Slow strength recovery so battered bands rebuild between clashes instead of dwindling
        /// to nothing. Call once per in-game day.</summary>
        public void OnNewDay()
        {
            foreach (var p in Parties)
                if (p.strength < GameConstants.WorldPartyMaxStrength)
                    p.strength = Math.Min(GameConstants.WorldPartyMaxStrength,
                        p.strength + GameConstants.WorldPartyDailyStrengthRegen);
        }

        void OnReachTarget(WorldGraph map, FactionDiplomacy diplo, WorldParty p, MapNodeData target)
        {
            if (diplo != null && IsEnemyControlled(map, diplo, p, p.targetNodeId))
            {
                if (p.strength >= SettlementDefense(target))
                    CaptureSettlement(p, target);
                else
                {
                    NotifyIfNearPlayer(target.mapPosition, $"{p.displayName} raids {target.displayName} but can't hold it.");
                    p.strength = Math.Max(GameConstants.WorldPartyRetreatStrength, p.strength - 3);
                    GameState.Instance?.GetOrCreateSettlement(target).AddProsperity(-GameConstants.RaidProsperityHit);
                }

                p.homeNodeId = p.targetNodeId;
                RetargetToNearestEnemy(map, diplo, p, p.targetNodeId);
                return;
            }

            // Peacetime patrol: ping-pong between home and target as before.
            (p.homeNodeId, p.targetNodeId) = (p.targetNodeId, p.homeNodeId);
        }

        /// <summary>Flip a settlement to its conqueror: ownership changes, its market starts recruiting the
        /// new owner's culture, and the band spends strength garrisoning it. Always announced — it reshapes
        /// the strategic map, not just the player's neighbourhood.</summary>
        void CaptureSettlement(WorldParty p, MapNodeData node)
        {
            var previous = node.controllingFaction;
            if (previous == p.faction) return;

            node.controllingFaction = p.faction;
            p.strength = Math.Max(GameConstants.WorldPartyRetreatStrength,
                p.strength - GameConstants.SettlementCaptureStrengthCost);

            var g = GameState.Instance;
            if (g == null) return;
            g.Market?.OnOwnerChanged(node);
            g.GetOrCreateSettlement(node).AddProsperity(-GameConstants.SackProsperityHit); // a storming is a sacking
            g.PendingNotifications.Add(
                $"{GameState.FactionName(p.faction)} have taken {node.displayName} from {GameState.FactionName(previous)}!");
        }

        static int SettlementDefense(MapNodeData node)
        {
            var baseDef = node.type switch
            {
                NodeType.Capital => GameConstants.GarrisonCapital,
                NodeType.Castle => GameConstants.GarrisonCastle,
                NodeType.Town => GameConstants.GarrisonTown,
                _ => GameConstants.GarrisonVillage
            };

            var g = GameState.Instance;
            if (g != null && g.Settlements.TryGetValue(node.id, out var s))
                baseDef += s.GetTier(BuildingType.Walls) * GameConstants.GarrisonWallsPerTier;
            return baseDef;
        }

        static bool IsEnemyControlled(WorldGraph map, FactionDiplomacy diplo, WorldParty p, string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || !map.TryGetNode(nodeId, out var node)) return false;
            if (!IsSettlement(node) || node.controllingFaction == p.faction) return false;
            return diplo.AreAtWar(p.faction, node.controllingFaction);
        }

        static bool IsSettlement(MapNodeData node) =>
            !node.isTemporary && node.type is NodeType.Capital or NodeType.Town or NodeType.Castle or NodeType.Village;

        void RetargetToNearestEnemy(WorldGraph map, FactionDiplomacy diplo, WorldParty p, string exclude)
        {
            MapNodeData best = null;
            var bestDist = float.MaxValue;
            foreach (var node in map.Nodes.Values)
            {
                if (node.id == exclude) continue;
                if (!IsSettlement(node) || node.controllingFaction == p.faction) continue;
                if (!diplo.AreAtWar(p.faction, node.controllingFaction)) continue;
                var d = (node.mapPosition - p.mapPos).sqrMagnitude;
                if (d >= bestDist) continue;
                bestDist = d;
                best = node;
            }

            if (best != null)
                p.targetNodeId = best.id;
            else
                p.targetNodeId = p.spawnNodeId; // no enemy holding in reach — fall back home
        }

        void ResolveSkirmishes(FactionDiplomacy diplo)
        {
            if (diplo == null) return;
            var range = GameConstants.WorldPartyContactRange * MapScaleFactor;
            var rangeSq = range * range;

            for (var i = 0; i < Parties.Count; i++)
            for (var j = i + 1; j < Parties.Count; j++)
            {
                var a = Parties[i];
                var b = Parties[j];
                if (!diplo.AreAtWar(a.faction, b.faction)) continue;
                if ((a.mapPos - b.mapPos).sqrMagnitude > rangeSq) continue;

                var winner = a.strength >= b.strength ? a : b;
                var loser = a.strength >= b.strength ? b : a;
                loser.strength -= GameConstants.WorldPartySkirmishLoserDamage;
                winner.strength -= GameConstants.WorldPartySkirmishWinnerDamage;

                // Loser breaks off toward its muster point, which separates the two so they don't grind
                // to dust in a single spot.
                loser.targetNodeId = loser.spawnNodeId;
                NotifyIfNearPlayer(winner.mapPos, $"Warbands clash: {winner.displayName} bloodies {loser.displayName}.");
            }
        }

        void NotifyIfNearPlayer(Vector2 pos, string message)
        {
            var g = GameState.Instance;
            if (g == null) return;
            var r = 3f * MapScaleFactor;
            if ((pos - g.PlayerMapPosition()).sqrMagnitude > r * r) return;
            if (g.Rng.NextDouble() < 0.5) // don't narrate every single exchange
                g.PendingNotifications.Add(message);
        }

        public List<WorldParty> VisibleNear(Vector2 playerMapPos, float radius = 2.2f * MapScaleFactor)
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
                spawnNodeId = home,
                mapPos = n.mapPosition,
                strength = str
            });
        }
    }
}
