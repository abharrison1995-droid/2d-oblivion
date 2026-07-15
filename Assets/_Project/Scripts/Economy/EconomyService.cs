using System.Collections.Generic;

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

            return total;
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

        public bool ConsumeFood(PartyState party, float days, out string log)
        {
            var need = DailyFoodNeed(party) * days;
            var remaining = need;
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

                stack.count -= unitsUsed;
                remaining -= unitsUsed * food.fillPerUnit;
                typesEaten++;
                if (stack.count <= 0)
                    party.food.RemoveAt(i);
                else
                    party.food[i] = stack;
            }

            if (remaining > 0.01f)
            {
                log = $"Party is hungry ({remaining:0.0} fill short). Morale will suffer.";
                return false;
            }

            log = typesEaten >= 2
                ? "Men ate well — variety kept spirits up."
                : "Rations issued.";
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
