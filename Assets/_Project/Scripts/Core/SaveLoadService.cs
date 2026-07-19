using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Voidovia
{
    [System.Serializable]
    public class SettlementSaveEntry
    {
        public string nodeId;
        public string displayName;
        public int culture;
        public float prosperity = 50f;
        public int notableRelation = GameConstants.NotableRelationBaseline;
        public BuildingState[] buildings = System.Array.Empty<BuildingState>();
    }

    [System.Serializable]
    public class SaveBlob
    {
        /// <summary>0 = written before versioning existed. Bump SaveLoadService.CurrentSaveVersion
        /// and add a migration branch in TryLoad when the format changes in a breaking way.</summary>
        public int saveVersion;
        public string heroName;
        public string originId;
        public int combat, leadership, tactics, trade, scouting;
        public int heroLevel = 1;
        public int heroXp;
        public int unspentSkillPoints;
        public int warbandExperienceRank;
        public int warbandExperience;
        public string nodeId;
        public int gold;
        public int day;
        public float hours;
        public bool merc;
        public bool vassal;
        public bool ownsLand;
        public string weapon;
        public string armour;
        public int reputation;
        public int bounty;
        public int questBeat;
        public string correctCityId;
        public string invJson;
        public string foodJson;
        public string troopsJson;
        public string woundedJson;
        public string cardsJson;
        public string relationsJson;
        public string prisonersJson;
        public string companionsJson;
        public string banditCampCooldownsJson;
        public string trainingJobsJson;
        public int prisonerSequence;
        public SettlementSaveEntry[] settlements = System.Array.Empty<SettlementSaveEntry>();
        public string questBoardJson;
        public bool firstBountyHuntDone;
        public bool firstTroopLevyDone;
        public string warsJson;
        public string ownershipJson;
    }

    public static class SaveLoadService
    {
        const string FileName = "voidovia_save.json";

        /// <summary>v1 → v2: added questBoardJson (offered + active QuestInstance list) and the two
        /// first-quest-completion companion-reward flags. Both default cleanly on an old save — an
        /// empty questBoardJson just means the board is empty until the next daily tick regenerates
        /// offers, and the flags default false (matching a save from before the quest system existed).
        /// v2 → v3: added woundedJson (the wounded pool). Empty on an old save means no wounded, which
        /// is exactly correct for a save from before the wounded system existed.
        /// v3 → v4: added warsJson (faction war matrix). Empty on an old save falls back to the seeded
        /// starting wars, matching a fresh campaign.
        /// v4 → v5: added ownershipJson (settlement control after captures). Empty on an old save leaves
        /// the map's authored owners untouched.
        /// v5 → v6: added bounty (Wanted-in-Voidovia status). Defaults to 0 (not wanted) on an old save.
        /// v6 → v7: added settlement notableRelation. Defaults to the baseline on an old save.</summary>
        public const int CurrentSaveVersion = 7;

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool SaveExists() => File.Exists(Path);

        public static void Save(GameState g)
        {
            var s = new SaveBlob
            {
                saveVersion = CurrentSaveVersion,
                heroName = g.Hero.name,
                originId = g.Hero.originId,
                combat = g.Hero.combat,
                leadership = g.Hero.leadership,
                tactics = g.Hero.tactics,
                trade = g.Hero.trade,
                scouting = g.Hero.scouting,
                heroLevel = g.Hero.level,
                heroXp = g.Hero.xp,
                unspentSkillPoints = g.Hero.unspentSkillPoints,
                warbandExperienceRank = g.Hero.warbandExperienceRank,
                warbandExperience = g.Party.warbandExperience,
                nodeId = g.Party.currentNodeId,
                gold = g.Party.gold,
                day = g.Party.day,
                hours = g.Party.hours,
                merc = g.Party.isVoidoviaMercenary,
                vassal = g.Party.isVoidoviaVassal,
                ownsLand = g.Party.ownsLand,
                weapon = g.Party.equippedWeaponId,
                armour = g.Party.equippedArmourId,
                reputation = (int)g.Party.reputation,
                bounty = g.Party.bounty,
                questBeat = (int)g.Act1Quest.Beat,
                correctCityId = g.Act1Quest.CorrectCityId
            };
            s.invJson = EncodeStacks(g.Party.inventory);
            s.foodJson = EncodeStacks(g.Party.food);
            s.troopsJson = EncodeTroops(g.Party.troops);
            s.woundedJson = EncodeTroops(g.Party.wounded);
            s.cardsJson = EncodeStacks(g.Party.powerCards);
            s.relationsJson = EncodeRelations(g.Party.relations);
            s.prisonersJson = EncodePrisoners(g.Party.prisoners);
            s.companionsJson = EncodeStrings(g.Party.companionIds);
            s.banditCampCooldownsJson = EncodeCampCooldowns(g.BanditCampLastRaidDay);
            s.trainingJobsJson = EncodeTrainingJobs(g.Party.trainingJobs);
            s.prisonerSequence = g.PrisonerSequence;
            s.settlements = EncodeSettlements(g.Settlements);
            s.questBoardJson = EncodeQuestInstances(g.QuestBoard.SnapshotAll());
            s.firstBountyHuntDone = g.Party.firstBountyHuntDone;
            s.firstTroopLevyDone = g.Party.firstTroopLevyDone;
            s.warsJson = EncodeWars(g.Diplomacy.AllWars());
            s.ownershipJson = EncodeOwnership(g.Map);
            File.WriteAllText(Path, JsonUtility.ToJson(s, true));
            Debug.Log($"[Save] Wrote {Path}");
        }

        public static bool TryLoad(GameState g)
        {
            if (!File.Exists(Path)) return false;
            var s = JsonUtility.FromJson<SaveBlob>(File.ReadAllText(Path));
            if (s == null) return false;
            if (s.saveVersion < CurrentSaveVersion)
                Debug.Log($"[Save] Loading save v{s.saveVersion} (current v{CurrentSaveVersion}) — migrating: quest board and companion-reward flags default empty/false on pre-v2 saves.");
            g.Hero.name = s.heroName;
            g.Hero.originId = s.originId;
            g.Hero.combat = s.combat;
            g.Hero.leadership = s.leadership;
            g.Hero.tactics = s.tactics;
            g.Hero.trade = s.trade;
            g.Hero.scouting = s.scouting;
            g.Hero.level = Mathf.Max(1, s.heroLevel);
            g.Hero.xp = s.heroXp;
            g.Hero.unspentSkillPoints = s.unspentSkillPoints;
            g.Hero.warbandExperienceRank = s.warbandExperienceRank;
            g.Party.warbandExperience = s.warbandExperience;
            g.Party.currentNodeId = s.nodeId;
            g.Party.gold = s.gold;
            g.Party.day = s.day;
            g.Party.hours = s.hours;
            g.Party.isVoidoviaMercenary = s.merc;
            g.Party.isVoidoviaVassal = s.vassal;
            g.Party.ownsLand = s.ownsLand;
            g.Party.equippedWeaponId = s.weapon;
            g.Party.equippedArmourId = s.armour;
            g.Party.reputation = (ReputationFlag)s.reputation;
            g.Party.SetBounty(s.bounty); // keeps the WantedInVoidovia flag consistent with the amount
            g.Party.inventory.Clear();
            g.Party.food.Clear();
            g.Party.troops.Clear();
            g.Party.wounded.Clear();
            g.Party.powerCards.Clear();
            g.Party.relations.Clear();
            g.Party.prisoners.Clear();
            g.Party.companionIds.Clear();
            DecodeStacks(s.invJson, g.Party.inventory);
            DecodeStacks(s.foodJson, g.Party.food);
            DecodeTroops(s.troopsJson, g.Party.troops);
            DecodeTroops(s.woundedJson, g.Party.wounded);
            DecodeStacks(s.cardsJson, g.Party.powerCards);
            DecodeRelations(s.relationsJson, g.Party.relations);
            DecodePrisoners(s.prisonersJson, g.Party.prisoners);
            DecodeStrings(s.companionsJson, g.Party.companionIds);
            g.BanditCampLastRaidDay.Clear();
            DecodeCampCooldowns(s.banditCampCooldownsJson, g.BanditCampLastRaidDay);
            g.Party.trainingJobs.Clear();
            DecodeTrainingJobs(s.trainingJobsJson, g.Party.trainingJobs);
            g.PrisonerSequence = s.prisonerSequence;
            DecodeSettlements(s.settlements, g.Settlements);
            g.Act1Quest.ForceBeatForSave((StolenItemQuestBeat)s.questBeat, s.correctCityId);
            if (g.Party.companionIds.Count == 0)
                g.Party.companionIds.Add("bangkok_kuo");
            g.Party.firstBountyHuntDone = s.firstBountyHuntDone;
            g.Party.firstTroopLevyDone = s.firstTroopLevyDone;
            g.QuestBoard.RestoreAll(DecodeQuestInstances(s.questBoardJson), g.Map);
            // Empty on a pre-v4 save — leave the seeded starting wars in place rather than wiping them.
            if (!string.IsNullOrEmpty(s.warsJson))
                g.Diplomacy.RestoreWars(DecodeWars(s.warsJson));
            ApplyOwnership(s.ownershipJson, g);
            Debug.Log("[Save] Loaded ok");
            return true;
        }

        static string EncodeStacks(List<InventoryStack> list)
        {
            var parts = new List<string>();
            foreach (var s in list) parts.Add($"{s.itemId}:{s.count}");
            return string.Join(";", parts);
        }

        static string EncodeTroops(List<TroopStack> list)
        {
            var parts = new List<string>();
            foreach (var s in list) parts.Add($"{s.troopId}:{s.count}");
            return string.Join(";", parts);
        }

        static string EncodeRelations(Dictionary<FactionId, int> relations)
        {
            var parts = new List<string>();
            foreach (var kv in relations) parts.Add($"{(int)kv.Key}:{kv.Value}");
            return string.Join(";", parts);
        }

        static string EncodeStrings(List<string> list) => string.Join(";", list);

        static string EncodePrisoners(List<PrisonerRecord> list)
        {
            var parts = new List<string>();
            foreach (var p in list)
            {
                var name = (p.displayName ?? "Prisoner").Replace(';', ' ').Replace('|', ' ');
                parts.Add($"{p.id}|{name}|{p.recruitTroopId ?? ""}|{p.capturedDay}|{(p.isLord ? 1 : 0)}|{(int)p.sourceFaction}");
            }

            return string.Join(";", parts);
        }

        static void DecodePrisoners(string raw, List<PrisonerRecord> list)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                var kv = part.Split('|');
                if (kv.Length != 5 && kv.Length != 6) continue;
                if (!int.TryParse(kv[3], out var capturedDay)) continue;
                var sourceFaction = FactionId.Bandits;
                if (kv.Length == 6 && int.TryParse(kv[5], out var factionInt))
                    sourceFaction = (FactionId)factionInt;
                list.Add(new PrisonerRecord
                {
                    id = kv[0],
                    displayName = kv[1],
                    recruitTroopId = string.IsNullOrEmpty(kv[2]) ? null : kv[2],
                    capturedDay = capturedDay,
                    isLord = kv[4] == "1",
                    sourceFaction = sourceFaction
                });
            }
        }

        static bool IsSettlement(MapNodeData node) =>
            !node.isTemporary && node.type is NodeType.Capital or NodeType.Town or NodeType.Castle or NodeType.Village;

        static string EncodeOwnership(WorldGraph map)
        {
            var parts = new List<string>();
            foreach (var node in map.Nodes.Values)
                if (IsSettlement(node))
                    parts.Add($"{node.id}:{(int)node.controllingFaction}");
            return string.Join(";", parts);
        }

        static void ApplyOwnership(string raw, GameState g)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                var kv = part.Split(':');
                if (kv.Length != 2 || !int.TryParse(kv[1], out var factionInt)) continue;
                if (!g.Map.TryGetNode(kv[0], out var node)) continue;
                node.controllingFaction = (FactionId)factionInt;
                g.Market?.OnOwnerChanged(node);
            }
        }

        static string EncodeWars(List<(FactionId a, FactionId b)> wars)
        {
            var parts = new List<string>();
            foreach (var (a, b) in wars)
                parts.Add($"{(int)a}:{(int)b}");
            return string.Join(";", parts);
        }

        static List<(FactionId, FactionId)> DecodeWars(string raw)
        {
            var list = new List<(FactionId, FactionId)>();
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var part in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                if (!int.TryParse(kv[0], out var a) || !int.TryParse(kv[1], out var b)) continue;
                list.Add(((FactionId)a, (FactionId)b));
            }

            return list;
        }

        static string EncodeCampCooldowns(Dictionary<string, int> cooldowns)
        {
            var parts = new List<string>();
            foreach (var kv in cooldowns) parts.Add($"{kv.Key}:{kv.Value}");
            return string.Join(";", parts);
        }

        static void DecodeCampCooldowns(string raw, Dictionary<string, int> cooldowns)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                if (!int.TryParse(kv[1], out var day)) continue;
                cooldowns[kv[0]] = day;
            }
        }

        static string EncodeTrainingJobs(List<TrainingJob> jobs)
        {
            var parts = new List<string>();
            foreach (var j in jobs)
                parts.Add($"{j.sourceTroopId}|{j.targetTroopId}|{j.count}|{j.completesOnDay}");
            return string.Join(";", parts);
        }

        static void DecodeTrainingJobs(string raw, List<TrainingJob> jobs)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                var kv = part.Split('|');
                if (kv.Length != 4) continue;
                if (!int.TryParse(kv[2], out var count)) continue;
                if (!int.TryParse(kv[3], out var completesOnDay)) continue;
                jobs.Add(new TrainingJob
                {
                    sourceTroopId = kv[0],
                    targetTroopId = kv[1],
                    count = count,
                    completesOnDay = completesOnDay
                });
            }
        }

        static string CleanField(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace(';', ' ').Replace('|', ' ');

        static string EncodeQuestInstances(List<QuestInstance> instances)
        {
            var parts = new List<string>();
            foreach (var q in instances)
            {
                parts.Add(string.Join("|",
                    q.instanceId, q.templateId, (int)q.type, (int)q.state, q.giverNodeId,
                    CleanField(q.title), CleanField(q.description), q.rewardGold, q.deadlineDay,
                    q.targetNodeId ?? "", q.targetRoadId ?? "", CleanField(q.bountyTargetName),
                    q.escortDestinationNodeId ?? "", q.deliveryDestinationNodeId ?? "",
                    q.levyTroopId ?? "", q.levyCount, (int)q.levyArc));
            }

            return string.Join(";", parts);
        }

        static List<QuestInstance> DecodeQuestInstances(string raw)
        {
            var list = new List<QuestInstance>();
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var part in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                var kv = part.Split('|');
                if (kv.Length != 17) continue;
                if (!int.TryParse(kv[7], out var reward)) continue;
                if (!int.TryParse(kv[8], out var deadline)) continue;
                if (!int.TryParse(kv[15], out var levyCount)) continue;
                list.Add(new QuestInstance
                {
                    instanceId = kv[0],
                    templateId = kv[1],
                    type = (QuestTemplateType)int.Parse(kv[2]),
                    state = (QuestState)int.Parse(kv[3]),
                    giverNodeId = kv[4],
                    title = kv[5],
                    description = kv[6],
                    rewardGold = reward,
                    deadlineDay = deadline,
                    targetNodeId = string.IsNullOrEmpty(kv[9]) ? null : kv[9],
                    targetRoadId = string.IsNullOrEmpty(kv[10]) ? null : kv[10],
                    bountyTargetName = kv[11],
                    escortDestinationNodeId = string.IsNullOrEmpty(kv[12]) ? null : kv[12],
                    deliveryDestinationNodeId = string.IsNullOrEmpty(kv[13]) ? null : kv[13],
                    levyTroopId = string.IsNullOrEmpty(kv[14]) ? null : kv[14],
                    levyCount = levyCount,
                    levyArc = (TroopLevyArc)int.Parse(kv[16])
                });
            }

            return list;
        }

        static SettlementSaveEntry[] EncodeSettlements(Dictionary<string, SettlementState> settlements)
        {
            var entries = new SettlementSaveEntry[settlements.Count];
            var i = 0;
            foreach (var kv in settlements)
            {
                var buildings = new BuildingState[kv.Value.buildings.Count];
                var j = 0;
                foreach (var b in kv.Value.buildings.Values)
                    buildings[j++] = b;

                entries[i++] = new SettlementSaveEntry
                {
                    nodeId = kv.Value.nodeId,
                    displayName = kv.Value.displayName,
                    culture = (int)kv.Value.culture,
                    prosperity = kv.Value.prosperity,
                    notableRelation = kv.Value.notableRelation,
                    buildings = buildings
                };
            }

            return entries;
        }

        static void DecodeStacks(string raw, List<InventoryStack> list)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                if (!int.TryParse(kv[1], out var n)) continue;
                list.Add(new InventoryStack { itemId = kv[0], count = n });
            }
        }

        static void DecodeTroops(string raw, List<TroopStack> list)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                if (!int.TryParse(kv[1], out var n)) continue;
                list.Add(new TroopStack { troopId = kv[0], count = n });
            }
        }

        static void DecodeRelations(string raw, Dictionary<FactionId, int> relations)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                if (!int.TryParse(kv[0], out var factionInt)) continue;
                if (!int.TryParse(kv[1], out var value)) continue;
                relations[(FactionId)factionInt] = value;
            }
        }

        static void DecodeStrings(string raw, List<string> list)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
                if (!string.IsNullOrEmpty(part))
                    list.Add(part);
        }

        static void DecodeSettlements(SettlementSaveEntry[] entries, Dictionary<string, SettlementState> settlements)
        {
            settlements.Clear();
            if (entries == null) return;
            foreach (var entry in entries)
            {
                var settlement = new SettlementState(entry.nodeId, entry.displayName)
                {
                    culture = (FactionId)entry.culture,
                    prosperity = entry.prosperity <= 0f ? 50f : entry.prosperity,
                    notableRelation = entry.notableRelation <= 0 ? GameConstants.NotableRelationBaseline : entry.notableRelation
                };
                foreach (var b in entry.buildings)
                    settlement.RestoreBuilding(b.type, b.tier, b.isBuilt);
                settlements[entry.nodeId] = settlement;
            }
        }
    }
}
