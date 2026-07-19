using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Procedural side-quest board: 1-2 offers per settlement, refreshed daily (mirrors
    /// MarketService's per-settlement stock/regen pattern). Five templates share one
    /// QuestInstance shape; resolution differs per QuestTemplateType.
    /// </summary>
    public class QuestBoardService
    {
        static readonly List<QuestInstance> Empty = new();
        static readonly string[] BountyNames = { "Redcap Sil", "One-Eyed Corla", "Ashgutter Rudd", "Marsh Fen", "Tull Ironhand" };

        readonly Dictionary<string, List<QuestInstance>> _offers = new();
        readonly List<QuestInstance> _active = new();
        QuestTemplateCatalog _catalog;
        int _seq;

        public event Action<QuestInstance> QuestCompleted;

        public IReadOnlyList<QuestInstance> Active => _active;

        public void LoadTemplates(QuestTemplateCatalog catalog) => _catalog = catalog;

        public IReadOnlyList<QuestInstance> OffersAt(string nodeId) =>
            _offers.TryGetValue(nodeId, out var list) ? list : Empty;

        public IEnumerable<QuestInstance> ActiveAt(string nodeId)
        {
            foreach (var q in _active)
                if (RelevantNode(q) == nodeId)
                    yield return q;
        }

        static string RelevantNode(QuestInstance q) => q.type switch
        {
            QuestTemplateType.DeliveryFetch => q.deliveryDestinationNodeId,
            _ => q.giverNodeId
        };

        public static bool IsQuestGiverNode(MapNodeData node) =>
            !node.isTemporary && (node.hasRecruitment || node.type == NodeType.Village);

        public void TickDay(WorldGraph map, System.Random rng, int today)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var q = _active[i];
                if (today <= q.deadlineDay) continue;
                q.state = QuestState.Expired;
                // A broken promise sours the notable who trusted you with it.
                if (map.TryGetNode(q.giverNodeId, out var giver))
                    GameState.Instance?.AddNotableRelation(giver, -GameConstants.NotableDispleasureHit);
                CleanupSpawnedNode(map, q);
                _active.RemoveAt(i);
            }

            foreach (var node in map.Nodes.Values)
            {
                if (!IsQuestGiverNode(node)) continue;
                if (!_offers.TryGetValue(node.id, out var list))
                {
                    list = new List<QuestInstance>();
                    _offers[node.id] = list;
                }

                while (list.Count < GameConstants.QuestBoardOffersPerSettlement)
                {
                    if (rng.NextDouble() > GameConstants.QuestBoardDailyOfferChance) break;
                    var inst = Generate(node, map, rng, today);
                    if (inst == null) break;
                    list.Add(inst);
                }
            }
        }

        QuestInstance Generate(MapNodeData giver, WorldGraph map, System.Random rng, int today)
        {
            if (_catalog?.templates == null || _catalog.templates.Length == 0) return null;
            var type = (QuestTemplateType)rng.Next(5);
            if (!_catalog.TryGet(type, out var tpl)) return null;

            var inst = new QuestInstance
            {
                instanceId = $"quest_{today}_{_seq++}",
                templateId = tpl.id,
                type = type,
                state = QuestState.Offered,
                giverNodeId = giver.id,
                rewardGold = rng.Next(tpl.minReward, tpl.maxReward + 1)
            };

            switch (type)
            {
                case QuestTemplateType.BanditHideoutClear:
                    inst.title = string.Format(tpl.titlePattern, giver.displayName);
                    inst.description = $"A den of raiders has been spotted near {giver.displayName}. Clear it out.";
                    break;

                case QuestTemplateType.BountyHunt:
                    var bountyName = BountyNames[rng.Next(BountyNames.Length)];
                    inst.bountyTargetName = bountyName;
                    inst.title = string.Format(tpl.titlePattern, bountyName);
                    inst.description = $"{bountyName} has been raiding the roads near {giver.displayName}. Hunt them down.";
                    break;

                case QuestTemplateType.EscortCaravan:
                {
                    var dest = PickOtherSettlement(giver, map, rng);
                    if (dest == null) return null;
                    inst.escortDestinationNodeId = dest.id;
                    inst.title = string.Format(tpl.titlePattern, dest.displayName);
                    inst.description = $"See a caravan safely from {giver.displayName} to {dest.displayName}.";
                    break;
                }

                case QuestTemplateType.DeliveryFetch:
                {
                    var dest = PickOtherSettlement(giver, map, rng);
                    if (dest == null) return null;
                    inst.deliveryDestinationNodeId = dest.id;
                    inst.title = string.Format(tpl.titlePattern, dest.displayName);
                    inst.description = $"Carry word from {giver.displayName} to {dest.displayName} before it goes stale.";
                    break;
                }

                case QuestTemplateType.TroopLevy:
                    inst.levyArc = ArcFor(giver.type);
                    inst.levyTroopId = LevyTroopFor(inst.levyArc, rng);
                    inst.levyCount = rng.Next(GameConstants.QuestLevyCountMin, GameConstants.QuestLevyCountMax + 1);
                    inst.title = string.Format(tpl.titlePattern, giver.displayName);
                    inst.description = LevyFlavor(inst.levyArc, giver.displayName);
                    break;
            }

            var days = tpl.minDeadlineDays + (float)rng.NextDouble() * (tpl.maxDeadlineDays - tpl.minDeadlineDays);
            inst.deadlineDay = today + Mathf.CeilToInt(days);
            return inst;
        }

        static TroopLevyArc ArcFor(NodeType type) => type switch
        {
            NodeType.Village => TroopLevyArc.VillageBanditProtection,
            NodeType.Capital or NodeType.Castle => TroopLevyArc.NobleAtWar,
            _ => TroopLevyArc.SettlementManpowerLoss
        };

        static string LevyTroopFor(TroopLevyArc arc, System.Random rng) => arc switch
        {
            TroopLevyArc.NobleAtWar => rng.NextDouble() < 0.5 ? "void_archer" : "voidovan_cattle_rustler",
            _ => "void_militia"
        };

        static string LevyFlavor(TroopLevyArc arc, string giverName) => arc switch
        {
            TroopLevyArc.VillageBanditProtection => $"{giverName} pleads for men to stand watch against raiders.",
            TroopLevyArc.NobleAtWar => $"A noble levies troops from {giverName} for a coming campaign.",
            _ => $"{giverName} has lost hands to illness, desertion, and age — they need replacements."
        };

        static MapNodeData PickOtherSettlement(MapNodeData exclude, WorldGraph map, System.Random rng)
        {
            var candidates = new List<MapNodeData>();
            foreach (var n in map.Nodes.Values)
                if (n.id != exclude.id && IsQuestGiverNode(n))
                    candidates.Add(n);
            return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
        }

        public bool TryAccept(string nodeId, string instanceId, GameState g, out string log)
        {
            log = "";
            if (!_offers.TryGetValue(nodeId, out var list))
            {
                log = "No quests here.";
                return false;
            }

            var inst = list.Find(q => q.instanceId == instanceId);
            if (inst == null)
            {
                log = "That quest is gone.";
                return false;
            }

            list.Remove(inst);
            inst.state = QuestState.Active;

            if (inst.type is QuestTemplateType.BanditHideoutClear or QuestTemplateType.BountyHunt &&
                !TrySpawnTarget(inst, g.Map, g.Rng))
            {
                log = "Couldn't find a place to send you — quest cancelled.";
                return false;
            }

            _active.Add(inst);
            log = $"Accepted: {inst.title}";
            return true;
        }

        static bool TrySpawnTarget(QuestInstance inst, WorldGraph map, System.Random rng)
        {
            if (!map.TryGetNode(inst.giverNodeId, out var giver)) return false;

            var nodeId = $"{inst.instanceId}_site";
            var node = new MapNodeData
            {
                id = nodeId,
                displayName = inst.type == QuestTemplateType.BountyHunt ? $"{inst.bountyTargetName}'s Camp" : "Raider's Den",
                type = NodeType.QuestLair,
                controllingFaction = FactionId.Bandits,
                isTemporary = true,
                parentSettlementId = giver.id,
                mapPosition = giver.mapPosition + new Vector2(0.5f + (float)rng.NextDouble() * 0.3f, -0.4f - (float)rng.NextDouble() * 0.3f)
            };
            var link = new RoadEdgeData
            {
                id = $"road_{giver.id}_{nodeId}",
                fromNodeId = giver.id,
                toNodeId = nodeId,
                travelHours = 3f,
                danger = 0.2f,
                terrain = TerrainType.Forest,
                allowSevereRaids = false
            };

            map.AddTemporaryNode(node, link);
            inst.targetNodeId = nodeId;
            inst.targetRoadId = link.id;
            return true;
        }

        public QuestInstance FindActiveByTargetNode(string nodeId) =>
            _active.Find(q => q.type is QuestTemplateType.BanditHideoutClear or QuestTemplateType.BountyHunt && q.targetNodeId == nodeId);

        public static BattleForce RollEnemyForce(string displayName, System.Random rng)
        {
            var troops = rng.Next(3) switch
            {
                0 => new List<TroopStack> { new() { troopId = "butter_thug", count = 5 } },
                1 => new List<TroopStack> { new() { troopId = "butter_thug", count = 6 }, new() { troopId = "butter_raider", count = 2 } },
                _ => new List<TroopStack> { new() { troopId = "butter_thug", count = 4 }, new() { troopId = "butter_slinger", count = 3 } }
            };
            return new BattleForce { name = displayName, faction = FactionId.Bandits, troops = troops };
        }

        public bool TryResolveCombatVictory(QuestInstance inst, BattleOutcome outcome, GameState g, out string log)
        {
            if (!outcome.playerVictory)
            {
                log = "The fight went badly — the quest remains open. Try again before the deadline.";
                return false;
            }

            Complete(inst, g);
            log = $"{inst.title} — complete. +{inst.rewardGold}g.";
            CleanupSpawnedNode(g.Map, inst);
            _active.Remove(inst);
            return true;
        }

        public QuestInstance FindActiveEscortAt(string nodeId) =>
            _active.Find(q => q.type == QuestTemplateType.EscortCaravan && q.escortDestinationNodeId == nodeId);

        /// <summary>Rolled once on arrival at the escort destination. False + not-ambushed means a fight is
        /// needed (caller opens battle and finishes via TryResolveEscortBattle); false + ambushed likewise.</summary>
        public bool TryResolveEscortArrival(QuestInstance inst, GameState g, System.Random rng, out bool ambushed, out string log)
        {
            ambushed = rng.NextDouble() < GameConstants.QuestEscortAmbushChance;
            if (ambushed)
            {
                log = "Riders break from the treeline — the caravan is under attack!";
                return false;
            }

            Complete(inst, g);
            _active.Remove(inst);
            log = $"{inst.title} — the caravan arrives safely. +{inst.rewardGold}g.";
            return true;
        }

        public bool TryResolveEscortBattle(QuestInstance inst, BattleOutcome outcome, GameState g, out string log)
        {
            if (!outcome.playerVictory)
            {
                inst.state = QuestState.Failed;
                _active.Remove(inst);
                log = "The caravan is lost. Quest failed.";
                return false;
            }

            Complete(inst, g);
            _active.Remove(inst);
            log = $"{inst.title} — caravan defended. +{inst.rewardGold}g.";
            return true;
        }

        public bool IsReadyToTurnIn(QuestInstance inst, PartyState party, int today) => inst.type switch
        {
            QuestTemplateType.DeliveryFetch => party.currentNodeId == inst.deliveryDestinationNodeId && today <= inst.deadlineDay,
            QuestTemplateType.TroopLevy => party.currentNodeId == inst.giverNodeId && HasTroops(party, inst.levyTroopId, inst.levyCount),
            _ => false
        };

        static bool HasTroops(PartyState party, string troopId, int count)
        {
            foreach (var s in party.troops)
                if (s.troopId == troopId)
                    return s.count >= count;
            return false;
        }

        public bool TryTurnIn(QuestInstance inst, GameState g, out string log)
        {
            if (!IsReadyToTurnIn(inst, g.Party, g.Party.day))
            {
                log = "Not ready yet.";
                return false;
            }

            if (inst.type == QuestTemplateType.TroopLevy)
            {
                var stack = g.Party.troops.Find(s => s.troopId == inst.levyTroopId);
                if (stack == null || stack.count < inst.levyCount)
                {
                    log = "Don't have enough men.";
                    return false;
                }

                stack.count -= inst.levyCount;
                if (stack.count <= 0) g.Party.troops.Remove(stack);
            }

            Complete(inst, g);
            _active.Remove(inst);
            log = $"{inst.title} — delivered. +{inst.rewardGold}g.";
            return true;
        }

        void Complete(QuestInstance inst, GameState g)
        {
            inst.state = QuestState.Completed;
            g.Party.gold += inst.rewardGold;
            if (g.Map.TryGetNode(inst.giverNodeId, out var giver))
            {
                g.Party.AddRelation(giver.controllingFaction, GameConstants.QuestRelationReward);
                g.AddNotableRelation(giver, GameConstants.QuestNotableReward); // the notable remembers a favour done
            }
            QuestCompleted?.Invoke(inst);
        }

        static void CleanupSpawnedNode(WorldGraph map, QuestInstance inst)
        {
            if (!string.IsNullOrEmpty(inst.targetNodeId))
                map.RemoveNode(inst.targetNodeId);
        }

        public List<QuestInstance> SnapshotAll()
        {
            var all = new List<QuestInstance>(_active);
            foreach (var list in _offers.Values)
                all.AddRange(list);
            return all;
        }

        public void RestoreAll(IEnumerable<QuestInstance> instances, WorldGraph map = null)
        {
            _offers.Clear();
            _active.Clear();
            foreach (var inst in instances)
            {
                if (inst.state == QuestState.Offered)
                {
                    if (!_offers.TryGetValue(inst.giverNodeId, out var list))
                    {
                        list = new List<QuestInstance>();
                        _offers[inst.giverNodeId] = list;
                    }

                    list.Add(inst);
                }
                else if (inst.state == QuestState.Active)
                {
                    _active.Add(inst);
                    RespawnTargetNode(inst, map);
                }
            }
        }

        /// <summary>Combat quests (BanditHideoutClear / BountyHunt) spawn a temporary map node + road
        /// when accepted; the map topology isn't part of the save, so on load we must re-create the
        /// target site from the saved ids — otherwise the quest is active but its destination no longer
        /// exists on the map and can never be reached or completed.</summary>
        static void RespawnTargetNode(QuestInstance inst, WorldGraph map)
        {
            if (map == null) return;
            if (inst.type is not (QuestTemplateType.BanditHideoutClear or QuestTemplateType.BountyHunt)) return;
            if (string.IsNullOrEmpty(inst.targetNodeId)) return;
            if (map.TryGetNode(inst.targetNodeId, out _)) return; // already present
            if (!map.TryGetNode(inst.giverNodeId, out var giver)) return;

            var node = new MapNodeData
            {
                id = inst.targetNodeId,
                displayName = inst.type == QuestTemplateType.BountyHunt ? $"{inst.bountyTargetName}'s Camp" : "Raider's Den",
                type = NodeType.QuestLair,
                controllingFaction = FactionId.Bandits,
                isTemporary = true,
                parentSettlementId = giver.id,
                mapPosition = giver.mapPosition + new Vector2(0.55f, -0.5f)
            };
            var link = new RoadEdgeData
            {
                id = string.IsNullOrEmpty(inst.targetRoadId) ? $"road_{giver.id}_{inst.targetNodeId}" : inst.targetRoadId,
                fromNodeId = giver.id,
                toNodeId = inst.targetNodeId,
                travelHours = 3f,
                danger = 0.2f,
                terrain = TerrainType.Forest,
                allowSevereRaids = false
            };
            map.AddTemporaryNode(node, link);
        }
    }
}
