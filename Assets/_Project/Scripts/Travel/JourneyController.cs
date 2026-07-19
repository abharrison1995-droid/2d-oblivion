using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    [Serializable]
    public class JourneyStep
    {
        public string edgeId;
        public TerrainType terrain;
        public float danger;
        public bool allowSevereRaids;
        public Vector2 worldPos; // map space
        public string fromNodeId;
        public string toNodeId;
        public bool arrivesAtNode;
        public string arriveNodeId;
        public bool isOffPath;
        public float offPathSpeedMultiplier = 1f;
    }

    /// <summary>
    /// Inch-by-inch travel: route broken into steps; time/event chance from speed, mounts, terrain.
    /// </summary>
    public class JourneyController
    {
        public bool IsActive { get; private set; }
        public string DestinationId { get; private set; }
        public int StepIndex { get; private set; }
        public IReadOnlyList<JourneyStep> Steps => _steps;
        public Vector2 CurrentMapPos { get; private set; }

        readonly List<JourneyStep> _steps = new();
        readonly List<GameObject> _highlights = new(); // owned by UI; cleared via callback

        public float CalculateHoursPerStep(PartyState party, CharacterStats hero, JourneyStep step, TroopRosterData roster)
        {
            var mountRatio = MountRatio(party, roster);
            var scouting = (hero?.scouting ?? 5) + CompanionBonuses.ScoutingBonus();
            // Base inch time; mounts and scouting shorten; terrain lengthens; big parties drag.
            var hours = 1.15f;
            hours *= TerrainMult(step.terrain);
            hours *= 1f - Mathf.Clamp01(mountRatio) * 0.35f;
            hours *= 1f - Mathf.Clamp((scouting - 5) * 0.03f, -0.15f, 0.2f);
            var men = Mathf.Max(1, party.TotalMen);
            hours *= 1f + Mathf.Clamp01((men - 8) / 40f) * 0.35f;
            if (step.isOffPath)
                hours *= step.offPathSpeedMultiplier;
            return Mathf.Clamp(hours, 0.35f, step.isOffPath ? 6f : 3.5f);
        }

        public float EventChance(PartyState party, CharacterStats hero, JourneyStep step, TroopRosterData roster)
        {
            var chance = step.danger * 0.55f + 0.08f;
            var mountRatio = MountRatio(party, roster);
            chance *= 1f - mountRatio * 0.25f; // mounts evade some roadside mess
            var scouting = hero.scouting + CompanionBonuses.ScoutingBonus();
            chance *= 1f - Mathf.Clamp((scouting - 5) * 0.04f, -0.2f, 0.25f);
            if (step.isOffPath)
                chance += 0.18f; // no maintained road — remote ground draws more trouble
            if ((party.reputation & ReputationFlag.Infamous) != 0)
                chance += GameConstants.InfamousDangerBonus; // a notorious band attracts trouble
            return Mathf.Clamp01(chance);
        }

        public bool Begin(WorldGraph map, string fromId, string toId, out string error)
        {
            error = null;
            _steps.Clear();
            var route = map.GetRoute(fromId, toId);
            if (route.Count == 0)
            {
                error = "No route.";
                return false;
            }

            if (!map.TryGetNode(fromId, out var at))
            {
                error = "Bad start.";
                return false;
            }

            CurrentMapPos = at.mapPosition;
            DestinationId = toId;

            foreach (var edge in route)
            {
                if (!map.TryGetNode(edge.fromNodeId, out var a) || !map.TryGetNode(edge.toNodeId, out var b))
                    continue;

                // ~1 inch per ~2 map hours of road, minimum 2
                var inches = Mathf.Max(2, Mathf.RoundToInt(edge.travelHours / 2f));
                for (var i = 1; i <= inches; i++)
                {
                    var t = i / (float)inches;
                    var pos = Vector2.Lerp(a.mapPosition, b.mapPosition, t);
                    _steps.Add(new JourneyStep
                    {
                        edgeId = edge.id,
                        terrain = edge.terrain,
                        danger = edge.danger,
                        allowSevereRaids = edge.allowSevereRaids,
                        worldPos = pos,
                        fromNodeId = edge.fromNodeId,
                        toNodeId = edge.toNodeId,
                        arrivesAtNode = i == inches,
                        arriveNodeId = i == inches ? edge.toNodeId : null
                    });
                }
            }

            StepIndex = 0;
            IsActive = _steps.Count > 0;
            return IsActive;
        }

        /// <summary>
        /// Direct bushwhack to an off-path-only destination (bandit camps) — no Dijkstra route,
        /// just a straight line along the single off-path edge, flagged so speed/danger/encounter
        /// rolls apply the off-path penalties.
        /// </summary>
        public bool BeginOffPath(WorldGraph map, string fromId, string toId, out string error)
        {
            error = null;
            _steps.Clear();

            if (!map.TryGetOffPathEdge(fromId, toId, out var edge))
            {
                error = "No off-path route.";
                return false;
            }

            if (!map.TryGetNode(fromId, out var a) || !map.TryGetNode(toId, out var b))
            {
                error = "Bad start.";
                return false;
            }

            CurrentMapPos = a.mapPosition;
            DestinationId = toId;

            var inches = Mathf.Max(2, Mathf.RoundToInt(edge.travelHours / 2f));
            for (var i = 1; i <= inches; i++)
            {
                var t = i / (float)inches;
                var pos = Vector2.Lerp(a.mapPosition, b.mapPosition, t);
                _steps.Add(new JourneyStep
                {
                    edgeId = edge.id,
                    terrain = edge.terrain,
                    danger = edge.danger,
                    allowSevereRaids = false,
                    worldPos = pos,
                    fromNodeId = fromId,
                    toNodeId = toId,
                    arrivesAtNode = i == inches,
                    arriveNodeId = i == inches ? toId : null,
                    isOffPath = true,
                    offPathSpeedMultiplier = edge.offPathSpeedMultiplier
                });
            }

            StepIndex = 0;
            IsActive = _steps.Count > 0;
            return IsActive;
        }

        public bool TryAdvance(
            PartyState party,
            CharacterStats hero,
            TravelDirector travel,
            EconomyService economy,
            TroopRosterData roster,
            System.Random rng,
            out TravelEncounter encounter,
            out string log,
            out bool finished)
        {
            encounter = new TravelEncounter { kind = TravelEncounterKind.None };
            finished = false;
            log = "";

            if (!IsActive || StepIndex >= _steps.Count)
            {
                IsActive = false;
                finished = true;
                log = "Journey already finished.";
                return false;
            }

            var step = _steps[StepIndex];
            var hours = CalculateHoursPerStep(party, hero, step, roster);
            party.hours += hours;
            while (party.hours >= GameConstants.HoursPerDay)
            {
                party.hours -= GameConstants.HoursPerDay;
                party.day++;
                GameState.Instance?.OnNewDay();
            }

            CurrentMapPos = step.worldPos;
            economy.ConsumeFood(party, hours / GameConstants.HoursPerDay, out var foodLog);

            var chance = EventChance(party, hero, step, roster);
            if (rng.NextDouble() < chance)
            {
                var edgeForRoll = new RoadEdgeData
                {
                    danger = Mathf.Clamp01(step.danger + 0.15f),
                    terrain = step.terrain,
                    allowSevereRaids = step.allowSevereRaids
                };
                encounter = step.isOffPath
                    ? travel.RollOffPathEncounter(edgeForRoll, rng)
                    : travel.RollEncounter(edgeForRoll, rng);
            }

            // A wanted outlaw draws Voidovia patrols on maintained roads — odds climb with the bounty.
            if (encounter.kind == TravelEncounterKind.None && !step.isOffPath && party.IsWantedInVoidovia)
            {
                var patrolChance = Mathf.Clamp01(
                    GameConstants.WantedPatrolBaseChance + party.bounty * GameConstants.WantedPatrolChancePerBounty);
                if (rng.NextDouble() < patrolChance)
                    encounter = travel.RollWantedPatrol(rng);
            }

            StepIndex++;
            if (step.arrivesAtNode && !string.IsNullOrEmpty(step.arriveNodeId))
                party.currentNodeId = step.arriveNodeId;

            if (StepIndex >= _steps.Count)
            {
                IsActive = false;
                finished = true;
                party.currentNodeId = DestinationId;
                log = $"Arrived {DestinationId}. {foodLog}";
            }
            else
            {
                log =
                    $"Advance ({hours:0.0}h, {step.terrain}). Speeds set by mounts/scouting/terrain. {foodLog}";
            }

            return true;
        }

        public void Cancel()
        {
            IsActive = false;
            _steps.Clear();
            StepIndex = 0;
        }

        /// <summary>
        /// Odds of slipping away from a fight-capable encounter instead of it forcing battle.
        /// Mounts and scouting improve the odds; tougher encounters are harder to shake.
        /// </summary>
        public static float EscapeChance(TravelEncounterKind kind, PartyState party, CharacterStats hero, TroopRosterData roster)
        {
            var baseChance = kind switch
            {
                TravelEncounterKind.MinorThieves => 0.85f,
                TravelEncounterKind.BanditAmbush => 0.6f,
                TravelEncounterKind.ButterRaid => 0.4f,
                TravelEncounterKind.VoidoviaPatrol => 0.5f,
                _ => 1f
            };
            var mountRatio = MountRatio(party, roster);
            var scouting = (hero?.scouting ?? 5) + CompanionBonuses.ScoutingBonus();
            var scoutingBonus = Mathf.Clamp(scouting - 5, -5, 10) * 0.03f;
            return Mathf.Clamp01(baseChance + mountRatio * 0.3f + scoutingBonus);
        }

        static float MountRatio(PartyState party, TroopRosterData roster)
        {
            if (roster?.troops == null || party.TotalMen <= 0) return 0f;
            var mounts = 0;
            foreach (var stack in party.troops)
            {
                foreach (var def in roster.troops)
                {
                    if (def.id != stack.troopId) continue;
                    if (def.isMounted) mounts += stack.count;
                    break;
                }
            }

            return mounts / (float)party.TotalMen;
        }

        static float TerrainMult(TerrainType t) => t switch
        {
            TerrainType.Road => 1f,
            TerrainType.Hill => 1.2f,
            TerrainType.Forest => 1.25f,
            TerrainType.Marsh => 1.45f,
            TerrainType.Mountain => 1.55f,
            _ => 1.1f
        };
    }
}
