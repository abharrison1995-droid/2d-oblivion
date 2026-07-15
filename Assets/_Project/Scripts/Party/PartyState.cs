using System;
using System.Collections.Generic;

namespace Voidovia
{
    [Serializable]
    public class TroopStack
    {
        public string troopId;
        public int count;
    }

    [Serializable]
    public class InventoryStack
    {
        public string itemId;
        public int count;
    }

    /// <summary>
    /// Player warband runtime state: men, food, gold, location.
    /// </summary>
    public class PartyState
    {
        public string currentNodeId = "greyledger";
        public int gold = 120;
        public float hours = 8f;
        public int day = 1;
        public List<TroopStack> troops = new();
        public List<InventoryStack> inventory = new();
        public List<InventoryStack> food = new();
        public List<string> companionIds = new();
        public List<string> prisoners = new();
        public Dictionary<FactionId, int> relations = new();
        public ReputationFlag reputation = ReputationFlag.Good;
        public bool isVoidoviaMercenary;
        public bool isVoidoviaVassal;
        public bool ownsLand;

        public int TotalMen
        {
            get
            {
                var n = 0;
                foreach (var t in troops)
                    n += t.count;
                return n;
            }
        }

        public void AddRelation(FactionId faction, int delta)
        {
            relations.TryGetValue(faction, out var value);
            relations[faction] = value + delta;
        }

        public int GetRelation(FactionId faction)
        {
            relations.TryGetValue(faction, out var value);
            return value;
        }

        public bool CanBecomeVassal()
        {
            return isVoidoviaMercenary
                   && GetRelation(FactionId.Voidovia) >= GameConstants.VassalRelationThreshold
                   && (reputation & ReputationFlag.Good) != 0
                   && (reputation & ReputationFlag.Infamous) == 0;
        }
    }
}
