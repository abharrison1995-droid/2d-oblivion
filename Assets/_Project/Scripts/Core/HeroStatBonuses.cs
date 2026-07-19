using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Small, focused mechanical hooks for the hero stats — Combat/Leadership/Tactics/Trade
    /// were spendable via skill points but had no gameplay effect anywhere. Warband Experience
    /// (the currently banked amount) also feeds combat/surrender so hoarding it isn't purely
    /// a training-speed decision.
    /// </summary>
    public static class HeroStatBonuses
    {
        public static float CombatBattlePowerMultiplier()
        {
            var hero = GameState.Instance?.Hero;
            return hero == null ? 1f : 1f + (hero.combat - 5) * GameConstants.CombatBattlePowerBonusPerPoint;
        }

        public static float WarbandExperienceBattlePowerMultiplier()
        {
            var party = GameState.Instance?.Party;
            if (party == null) return 1f;
            var frac = Mathf.Clamp01(party.warbandExperience / GameConstants.SurrenderWarbandExperienceSoftCap);
            return 1f + frac * GameConstants.WarbandExperienceBattlePowerBonusCap;
        }

        public static float TacticsCasualtyMultiplier()
        {
            var hero = GameState.Instance?.Hero;
            if (hero == null) return 1f;
            var reduction = Mathf.Clamp((hero.tactics - 5) * GameConstants.TacticsCasualtyReductionPerPoint,
                -GameConstants.TacticsCasualtyReductionCap, GameConstants.TacticsCasualtyReductionCap);
            return 1f - reduction;
        }

        public static float LeadershipWageMultiplier()
        {
            var hero = GameState.Instance?.Hero;
            if (hero == null) return 1f;
            var discount = Mathf.Clamp((hero.leadership - 5) * GameConstants.LeadershipWageDiscountPerPoint,
                -GameConstants.LeadershipWageDiscountCap, GameConstants.LeadershipWageDiscountCap);
            return 1f - discount;
        }

        public static float TradeSellMultiplier()
        {
            var hero = GameState.Instance?.Hero;
            if (hero == null) return 1f;
            var bonus = Mathf.Clamp((hero.trade - 5) * GameConstants.TradePriceBonusPerPoint,
                -GameConstants.TradePriceBonusCap, GameConstants.TradePriceBonusCap);
            return 1f + bonus;
        }

        public static float TradeBuyMultiplier()
        {
            var hero = GameState.Instance?.Hero;
            if (hero == null) return 1f;
            var bonus = Mathf.Clamp((hero.trade - 5) * GameConstants.TradePriceBonusPerPoint,
                -GameConstants.TradePriceBonusCap, GameConstants.TradePriceBonusCap);
            return 1f - bonus;
        }
    }
}
