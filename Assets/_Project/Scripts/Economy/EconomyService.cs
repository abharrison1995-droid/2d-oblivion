using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// M&amp;B-style food consumption + weekly wages.
    /// </summary>
    public class EconomyService
    {
        readonly Dictionary<string, FoodDefinition> _foods = new();
        readonly Dictionary<string, TroopDefinition> _troops = new();

        public void LoadFood(IEnumerable<FoodDefinition> foods)
        {
            _foods.Clear();
            foreach (var f in foods)
                _foods[f.id] = f;
        }

        public void LoadTroops(IEnumerable<TroopDefinition> troops)
        {
            _troops.Clear();
            foreach (var t in troops)
                _troops[t.id] = t;
        }

        public int WeeklyWageBill(PartyState party)
        {
            var total = 0;
            foreach (var stack in party.troops)
            {
                if (!_troops.TryGetValue(stack.troopId, out var def))
                    continue;
                total += def.weeklyWage * stack.count;
            }

            return Mathf.RoundToInt(total * HeroStatBonuses.LeadershipWageMultiplier() * CompanionBonuses.WageMultiplier());
        }

        public float DailyFoodNeed(PartyState party)
        {
            var need = 0f;
            foreach (var stack in party.troops)
            {
                if (!_troops.TryGetValue(stack.troopId, out var def))
                    continue;
                need += def.foodDrain * stack.count;
            }

            // Companions / player rough overhead
            need += 1f + party.companionIds.Count;
            return need;
        }

        /// <summary>Total remaining food measured in the same "fill" units DailyFoodNeed/ConsumeFood use
        /// (count × fillPerUnit), so a food-days estimate matches what actually gets eaten. Summing raw
        /// unit counts understates stores whenever a food's fillPerUnit differs from 1.</summary>
        public float TotalFoodFill(PartyState party)
        {
            var fill = 0f;
            foreach (var stack in party.food)
            {
                if (_foods.TryGetValue(stack.itemId, out var food))
                    fill += stack.count * food.fillPerUnit;
                else
                    fill += stack.count;
            }

            return fill + party.fractionalFoodFill;
        }

        public bool ConsumeFood(PartyState party, float days, out string log)
        {
            var need = DailyFoodNeed(party) * days;
            
            var remaining = need - party.fractionalFoodFill;
            if (remaining < 0f)
            {
                party.fractionalFoodFill = -remaining;
                remaining = 0f;
            }
            else
            {
                party.fractionalFoodFill = 0f;
            }

            var typesEaten = 0;

            for (var i = party.food.Count - 1; i >= 0 && remaining > 0.01f; i--)
            {
                var stack = party.food[i];
                if (!_foods.TryGetValue(stack.itemId, out var food))
                    continue;

                var availableFill = stack.count * food.fillPerUnit;
                if (availableFill <= 0f)
                    continue;

                var usedFill = availableFill < remaining ? availableFill : remaining;
                var unitsUsed = (int)System.Math.Ceiling(usedFill / food.fillPerUnit);
                if (unitsUsed > stack.count)
                    unitsUsed = stack.count;

                var fillProvided = unitsUsed * food.fillPerUnit;
                if (fillProvided > remaining)
                {
                    party.fractionalFoodFill += (fillProvided - remaining);
                }

                stack.count -= unitsUsed;
                remaining -= fillProvided;
                typesEaten++;
                if (stack.count <= 0)
                    party.food.RemoveAt(i);
                else
                    party.food[i] = stack;
            }

            if (remaining > 0.01f)
            {
                log = $"Party is hungry ({remaining:0.0} fill short). Morale will suffer.";
                party.AddMorale(-15f);
                return false;
            }

            if (typesEaten >= 2)
            {
                log = "Men ate well — variety kept spirits up.";
                party.AddMorale(5f);
            }
            else
            {
                log = "Rations issued.";
            }
            return true;
        }

        public int SellPrice(ItemDefinition item, NodeType settlementType)
        {
            var mult = settlementType switch
            {
                NodeType.Capital => 1.0f,
                NodeType.Town => 0.85f,
                NodeType.Castle => 0.75f,
                _ => 0.55f
            };
            return (int)(item.baseValue * mult);
        }
    }
}
