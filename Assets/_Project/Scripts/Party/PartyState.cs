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
    /// Player warband runtime state: men, food, gold, location, gear.
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
        public List<InventoryStack> powerCards = new();
        public string equippedWeaponId;
        public string equippedArmourId;
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

        public void RemoveMen(int count)
        {
            var remaining = count;
            troops.Sort((a, b) => b.count.CompareTo(a.count));
            for (var i = 0; i < troops.Count && remaining > 0; i++)
            {
                var take = Math.Min(troops[i].count, remaining);
                troops[i].count -= take;
                remaining -= take;
            }

            troops.RemoveAll(t => t.count <= 0);
        }

        public void AddInventory(string itemId, int count = 1)
        {
            foreach (var s in inventory)
            {
                if (s.itemId != itemId) continue;
                s.count += count;
                return;
            }

            inventory.Add(new InventoryStack { itemId = itemId, count = count });
        }

        public bool TryEquip(string itemId, EquipSlot slot, out string unequippedId)
        {
            unequippedId = null;
            InventoryStack found = null;
            foreach (var s in inventory)
            {
                if (s.itemId == itemId && s.count > 0)
                {
                    found = s;
                    break;
                }
            }

            if (found == null) return false;

            if (slot == EquipSlot.Weapon)
            {
                unequippedId = equippedWeaponId;
                equippedWeaponId = itemId;
            }
            else if (slot == EquipSlot.Armour)
            {
                unequippedId = equippedArmourId;
                equippedArmourId = itemId;
            }
            else return false;

            found.count--;
            if (found.count <= 0) inventory.Remove(found);
            if (!string.IsNullOrEmpty(unequippedId))
                AddInventory(unequippedId, 1);
            return true;
        }

        public bool HasPowerCard(string cardId)
        {
            foreach (var s in powerCards)
                if (s.itemId == cardId && s.count > 0)
                    return true;
            return false;
        }

        public void AddPowerCard(string cardId, int count = 1)
        {
            foreach (var s in powerCards)
            {
                if (s.itemId != cardId) continue;
                s.count += count;
                return;
            }

            powerCards.Add(new InventoryStack { itemId = cardId, count = count });
        }

        public bool TryBuyPowerCard(string cardId, int price)
        {
            if (gold < price) return false;
            gold -= price;
            AddPowerCard(cardId);
            return true;
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
