using System;
using System.Collections.Generic;

namespace Voidovia
{
    public enum StolenItemQuestBeat
    {
        NotStarted,
        QuestGiven,
        SeekAdvice,
        InvestigateCities,
        ExButterIntel,
        LairSpawned,
        LairRaided,
        ChiefCaptured,
        Completed
    }

    [Serializable]
    public class QuestDefinition
    {
        public string id;
        public string displayName;
        public string[] beatIds = Array.Empty<string>();
    }

    /// <summary>
    /// Act 1: What's Mine — stolen item across two cities → temporary Butter lair → capture chief.
    /// </summary>
    public class StolenItemQuestController
    {
        public const string QuestId = "act1_whats_mine";
        public const string StolenItemId = "starter_stolen_heirloom";
        public const string ButterChiefId = "buttery_chief";

        public StolenItemQuestBeat Beat { get; private set; } = StolenItemQuestBeat.NotStarted;
        public string AdviceNodeId { get; private set; } = "greyledger";
        public string CityAId { get; private set; } = "ashpond";
        public string CityBId { get; private set; } = "tollbar";
        public string CorrectCityId { get; private set; } = "tollbar";
        public string InvestigatedCityId { get; private set; }
        public bool ChoseWrongCity { get; private set; }
        public string LairNodeId { get; private set; } = "buttery_lair";
        public bool LairVisible { get; private set; }

        public event Action<string> Log;
        public event Action<MapNodeData, RoadEdgeData> LairSpawnRequested;

        public void StartQuest()
        {
            Beat = StolenItemQuestBeat.QuestGiven;
            Emit("A Buttery Chief's rogues stole your heirloom. Find someone in Greyledger who knows the road.");
        }

        public void SpeakToAdvisor()
        {
            if (Beat != StolenItemQuestBeat.QuestGiven && Beat != StolenItemQuestBeat.SeekAdvice)
                return;

            Beat = StolenItemQuestBeat.SeekAdvice;
            Emit($"Advisor: \"Try {Display(CityAId)} or {Display(CityBId)}. One of those dens feeds the Buttery Chief.\"");
            Beat = StolenItemQuestBeat.InvestigateCities;
        }

        public string OnArriveForInvestigation(string nodeId, bool wonFight)
        {
            if (Beat != StolenItemQuestBeat.InvestigateCities)
                return null;

            InvestigatedCityId = nodeId;
            if (nodeId != CityAId && nodeId != CityBId)
                return "Nothing useful here for the heirloom hunt.";

            if (nodeId == CorrectCityId)
            {
                ChoseWrongCity = false;
                Emit("Harder scrap — the Chief's men were staging here. A wounded ex-Butter talks.");
                return GiveIntel();
            }

            ChoseWrongCity = true;
            Emit("Wrong town, but you still had to fight frightened thugs. One kneels and redirects you.");
            CorrectCityId = nodeId == CityAId ? CityBId : CityAId;
            return GiveIntel();
        }

        string GiveIntel()
        {
            Beat = StolenItemQuestBeat.ExButterIntel;
            Emit($"Ex-Butter: \"The real hole is outside {Display(CorrectCityId)}. Newly dug. Fresh banners.\"");
            SpawnLair();
            return CorrectCityId;
        }

        void SpawnLair()
        {
            Beat = StolenItemQuestBeat.LairSpawned;
            LairVisible = true;

            var lair = new MapNodeData
            {
                id = LairNodeId,
                displayName = "Buttery Lair",
                type = NodeType.QuestLair,
                controllingFaction = FactionId.ButterKlanBoys,
                isTemporary = true,
                parentSettlementId = CorrectCityId,
                mapPosition = default // WorldBootstrap will offset from parent
            };

            var link = new RoadEdgeData
            {
                id = $"road_{CorrectCityId}_{LairNodeId}",
                fromNodeId = CorrectCityId,
                toNodeId = LairNodeId,
                travelHours = 3f,
                danger = 0.15f,
                terrain = TerrainType.Forest,
                allowSevereRaids = false
            };

            LairSpawnRequested?.Invoke(lair, link);
            Emit("Map updated: Buttery Lair is visible just outside the town.");
        }

        public bool TryCompleteLairRaid(BattleOutcome outcome, PartyState party)
        {
            if (Beat != StolenItemQuestBeat.LairSpawned || !LairVisible)
                return false;

            if (!outcome.playerVictory)
            {
                Emit("The lair holds. Regroup and try again.");
                return false;
            }

            Beat = StolenItemQuestBeat.LairRaided;

            if (!outcome.capturedLordIds.Contains(ButterChiefId))
            {
                Emit("You won the scrap but the Buttery Chief slipped the net. Mission requires capture.");
                return false;
            }

            party.prisoners.Add(ButterChiefId);
            party.inventory.Add(new InventoryStack { itemId = StolenItemId, count = 1 });
            foreach (var loot in outcome.loot)
                party.inventory.Add(loot);

            Beat = StolenItemQuestBeat.ChiefCaptured;
            Emit("Heirloom recovered. Buttery Chief taken alive — deliver him to Voidovia authority.");
            return true;
        }

        public void DeliverChiefToVoid(PartyState party)
        {
            if (Beat != StolenItemQuestBeat.ChiefCaptured)
                return;

            party.prisoners.Remove(ButterChiefId);
            party.AddRelation(FactionId.Voidovia, 15);
            Beat = StolenItemQuestBeat.Completed;
            Emit("Lord Void's men take the Chief. The Wide Eyed Beast will want a word.");
        }

        static string Display(string id) => id switch
        {
            "ashpond" => "Ashpond",
            "tollbar" => "Tollbar",
            "greyledger" => "Greyledger",
            _ => id
        };

        void Emit(string msg) => Log?.Invoke(msg);
    }
}
