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
        public BuildingState[] buildings = System.Array.Empty<BuildingState>();
    }

    [System.Serializable]
    public class SaveBlob
    {
        public string heroName;
        public string originId;
        public int combat, leadership, tactics, trade, scouting;
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
        public int questBeat;
        public string correctCityId;
        public string invJson;
        public string foodJson;
        public string troopsJson;
        public string cardsJson;
        public string relationsJson;
        public string prisonersJson;
        public string companionsJson;
        public SettlementSaveEntry[] settlements = System.Array.Empty<SettlementSaveEntry>();
    }

    public static class SaveLoadService
    {
        const string FileName = "voidovia_save.json";

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool SaveExists() => File.Exists(Path);

        public static void Save(GameState g)
        {
            var s = new SaveBlob
            {
                heroName = g.Hero.name,
                originId = g.Hero.originId,
                combat = g.Hero.combat,
                leadership = g.Hero.leadership,
                tactics = g.Hero.tactics,
                trade = g.Hero.trade,
                scouting = g.Hero.scouting,
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
                questBeat = (int)g.Act1Quest.Beat,
                correctCityId = g.Act1Quest.CorrectCityId
            };
            s.invJson = EncodeStacks(g.Party.inventory);
            s.foodJson = EncodeStacks(g.Party.food);
            s.troopsJson = EncodeTroops(g.Party.troops);
            s.cardsJson = EncodeStacks(g.Party.powerCards);
            s.relationsJson = EncodeRelations(g.Party.relations);
            s.prisonersJson = EncodeStrings(g.Party.prisoners);
            s.companionsJson = EncodeStrings(g.Party.companionIds);
            s.settlements = EncodeSettlements(g.Settlements);
            File.WriteAllText(Path, JsonUtility.ToJson(s, true));
            Debug.Log($"[Save] Wrote {Path}");
        }

        public static bool TryLoad(GameState g)
        {
            if (!File.Exists(Path)) return false;
            var s = JsonUtility.FromJson<SaveBlob>(File.ReadAllText(Path));
            if (s == null) return false;
            g.Hero.name = s.heroName;
            g.Hero.originId = s.originId;
            g.Hero.combat = s.combat;
            g.Hero.leadership = s.leadership;
            g.Hero.tactics = s.tactics;
            g.Hero.trade = s.trade;
            g.Hero.scouting = s.scouting;
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
            g.Party.inventory.Clear();
            g.Party.food.Clear();
            g.Party.troops.Clear();
            g.Party.powerCards.Clear();
            g.Party.relations.Clear();
            g.Party.prisoners.Clear();
            g.Party.companionIds.Clear();
            DecodeStacks(s.invJson, g.Party.inventory);
            DecodeStacks(s.foodJson, g.Party.food);
            DecodeTroops(s.troopsJson, g.Party.troops);
            DecodeStacks(s.cardsJson, g.Party.powerCards);
            DecodeRelations(s.relationsJson, g.Party.relations);
            DecodeStrings(s.prisonersJson, g.Party.prisoners);
            DecodeStrings(s.companionsJson, g.Party.companionIds);
            DecodeSettlements(s.settlements, g.Settlements);
            g.Act1Quest.ForceBeatForSave((StolenItemQuestBeat)s.questBeat, s.correctCityId);
            if (g.Party.companionIds.Count == 0)
                g.Party.companionIds.Add("bangkok_kuo");
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
                var settlement = new SettlementState(entry.nodeId, entry.displayName) { culture = (FactionId)entry.culture };
                foreach (var b in entry.buildings)
                    settlement.RestoreBuilding(b.type, b.tier, b.isBuilt);
                settlements[entry.nodeId] = settlement;
            }
        }
    }
}
