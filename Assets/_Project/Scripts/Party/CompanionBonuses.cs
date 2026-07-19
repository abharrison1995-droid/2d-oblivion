using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Sums recruited companions' bonuses into the same mechanical hooks HeroStatBonuses uses,
    /// so each companion's trait actually does something instead of being flavor text.
    /// </summary>
    public static class CompanionBonuses
    {
        static float Sum(CompanionBonusType type)
        {
            var gs = GameState.Instance;
            if (gs?.Party == null || gs.Companions == null) return 0f;
            var total = 0f;
            foreach (var id in gs.Party.companionIds)
            {
                if (gs.Companions.TryGet(id, out var def) && def.bonusType == type)
                    total += def.bonusValue;
            }
            return total;
        }

        public static float BattlePowerMultiplier() => 1f + Sum(CompanionBonusType.BattlePower);
        public static float TradeSellMultiplier() => 1f + Sum(CompanionBonusType.TradeSellBonus);
        public static float WageMultiplier() => Mathf.Clamp01(1f - Sum(CompanionBonusType.WageDiscount));
        public static float ScoutingBonus() => Sum(CompanionBonusType.ScoutingBonus);
        public static float CommandSwingBonus() => Sum(CompanionBonusType.CommandSwingBonus);
        public static float LootMultiplier() => 1f + Sum(CompanionBonusType.LootBonus);
        public static float RecruitPriceMultiplier() => Mathf.Clamp01(1f - Sum(CompanionBonusType.RecruitDiscount));

        /// <summary>Additive bonus from Surgeon companions, applied both to the battle wounded-vs-dead
        /// split and to the daily wounded recovery rate. See GameState.RecoverWoundedDaily / BattleUI.</summary>
        public static float FieldMedicineBonus() => Sum(CompanionBonusType.FieldMedicine);
    }
}
