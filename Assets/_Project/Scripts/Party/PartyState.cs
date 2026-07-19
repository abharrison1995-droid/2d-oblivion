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
    /// A captive held by the party: a named quest lord (delivered, not recruited/sold)
    /// or a generic captive (bandit, etc.) who can be recruited, sold, or released.
    /// </summary>
    [Serializable]
    public class PrisonerRecord
    {
        public string id;
        public string displayName;
        public string recruitTroopId; // null/empty when not recruitable (e.g. a named quest lord)
        public int capturedDay;
        public bool isLord;
        public FactionId sourceFaction = FactionId.Bandits;
    }

    /// <summary>
    /// Troops pulled from the fighting roster while they train up to the next tier.
    /// Not available in battle until CompletesOnDay.
    /// </summary>
    [Serializable]
    public class TrainingJob
    {
        public string sourceTroopId;
        public string targetTroopId;
        public int count;
        public int completesOnDay;
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
        public float morale = 50f;
        public float fractionalFoodFill = 0f;
        public int warbandExperience;
        public List<TroopStack> troops = new();
        /// <summary>Men wounded in battle: they occupy the warband but can't fight, and recover back
        /// into <see cref="troops"/> over time (faster resting in a settlement). See GameState recovery.</summary>
        public List<TroopStack> wounded = new();
        public List<TrainingJob> trainingJobs = new();
        public List<InventoryStack> inventory = new();
        public List<InventoryStack> food = new();
        public List<InventoryStack> powerCards = new();
        public string equippedWeaponId;
        public string equippedArmourId;
        public List<string> companionIds = new();
        public List<PrisonerRecord> prisoners = new();
        public Dictionary<FactionId, int> relations = new();
        public ReputationFlag reputation = ReputationFlag.Good;
        /// <summary>Gold owed to Voidovia for crimes. While &gt; 0 you're Wanted: patrols hunt you and you
        /// must clear it at a Voidovia settlement. Kept in sync with the WantedInVoidovia flag.</summary>
        public int bounty;
        public bool isVoidoviaMercenary;
        public bool isVoidoviaVassal;
        public bool ownsLand;
        public bool firstBountyHuntDone;
        public bool firstTroopLevyDone;

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

        public int TotalWounded
        {
            get
            {
                var n = 0;
                foreach (var w in wounded)
                    n += w.count;
                return n;
            }
        }

        /// <summary>Adds men to the wounded bucket (same stacking pattern as AddTroop).</summary>
        public void AddWounded(string troopId, int count)
        {
            if (count <= 0) return;
            foreach (var w in wounded)
            {
                if (w.troopId != troopId) continue;
                w.count += count;
                return;
            }

            wounded.Add(new TroopStack { troopId = troopId, count = count });
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

        /// <summary>Remove a specific troop type/count (used to apply exact battle casualties so the
        /// sim's weakest-first losses reach the real roster, unlike RemoveMen which strips biggest stacks).</summary>
        public void RemoveTroops(string troopId, int count)
        {
            if (count <= 0) return;
            for (var i = 0; i < troops.Count; i++)
            {
                if (troops[i].troopId != troopId) continue;
                troops[i].count -= count;
                if (troops[i].count <= 0) troops.RemoveAt(i);
                return;
            }
        }

        public void AddMorale(float delta)
        {
            morale += delta;
            if (morale < 10f) morale = 10f;
            if (morale > 100f) morale = 100f;
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
            if (slot == EquipSlot.Weapon && equippedWeaponId == itemId) return false;
            if (slot == EquipSlot.Armour && equippedArmourId == itemId) return false;

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

        public PrisonerRecord AddPrisoner(string id, string displayName, string recruitTroopId, bool isLord = false, FactionId sourceFaction = FactionId.Bandits)
        {
            var record = new PrisonerRecord
            {
                id = id,
                displayName = displayName,
                recruitTroopId = recruitTroopId,
                capturedDay = day,
                isLord = isLord,
                sourceFaction = sourceFaction
            };
            prisoners.Add(record);
            return record;
        }

        public bool RemovePrisoner(string id) => prisoners.RemoveAll(p => p.id == id) > 0;

        public bool TryFindPrisoner(string id, out PrisonerRecord record)
        {
            foreach (var p in prisoners)
            {
                if (p.id != id) continue;
                record = p;
                return true;
            }

            record = null;
            return false;
        }

        public bool IsWantedInVoidovia => bounty > 0;

        /// <summary>Set the Voidovia bounty and keep the WantedInVoidovia reputation flag in sync.</summary>
        public void SetBounty(int amount)
        {
            bounty = Math.Max(0, amount);
            if (bounty > 0)
                reputation |= ReputationFlag.WantedInVoidovia;
            else
                reputation &= ~ReputationFlag.WantedInVoidovia;
        }

        public void AddRelation(FactionId faction, int delta)
        {
            relations.TryGetValue(faction, out var value);
            value += delta;
            relations[faction] = value;
            if (value <= GameConstants.InfamyRelationThreshold)
                reputation |= ReputationFlag.Infamous;
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
