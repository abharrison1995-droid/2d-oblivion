using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Demanding a bandit force surrender instead of fighting: needs a headcount advantage,
    /// and your currently-banked Warband Experience sweetens the odds further. A successful
    /// surrender plays out like a win without a fight — half loot, full XP/WE, a few captives.
    /// </summary>
    public static class BanditSurrender
    {
        public static bool CanOffer(int enemyCount)
        {
            var g = GameState.Instance;
            if (g?.Party == null || enemyCount <= 0) return false;
            return g.Party.TotalMen >= enemyCount * GameConstants.SurrenderOutnumberRatio;
        }

        public static float SuccessChance(int enemyCount)
        {
            var g = GameState.Instance;
            if (g?.Party == null || enemyCount <= 0) return 0f;

            var ratio = g.Party.TotalMen / (float)enemyCount;
            var outnumberBonus = Mathf.Max(0f, ratio - GameConstants.SurrenderOutnumberRatio) * GameConstants.SurrenderOutnumberBonusScale;
            var weBonus = Mathf.Clamp01(g.Party.warbandExperience / GameConstants.SurrenderWarbandExperienceSoftCap) *
                          GameConstants.SurrenderWarbandExperienceBonusCap;
            return Mathf.Clamp01(GameConstants.SurrenderBaseChance + outnumberBonus + weBonus);
        }

        /// <summary>Applies rewards for a successful surrender and returns a log line.</summary>
        public static string Resolve(int enemyCount, string enemyLabel)
        {
            var g = GameState.Instance;
            var strength = enemyCount * GameConstants.SurrenderStrengthPerHead;
            var xpGain = Mathf.RoundToInt(strength * GameConstants.BattleXpPerEnemyStrength);
            var weGain = Mathf.RoundToInt(strength * GameConstants.WarbandExperiencePerEnemyStrength);
            var goldGain = Mathf.RoundToInt(enemyCount * 8f);

            g.Party.gold += goldGain;
            g.Party.warbandExperience += weGain;
            g.Party.food.Add(new InventoryStack { itemId = "grain", count = Mathf.Max(1, enemyCount / 4) });
            var levelUps = g.Hero.AddXp(xpGain);

            var prisonerCount = Mathf.Min(GameConstants.SurrenderPrisonerCap, Mathf.Max(1, enemyCount / 4));
            for (var i = 0; i < prisonerCount; i++)
                g.Party.AddPrisoner(g.NextPrisonerId(), "Bandit Captive", GameConstants.RecruitedBanditTroopId);

            var log = $"{enemyLabel} lay down arms without a fight. +{goldGain}g, +{xpGain} XP, +{weGain} Warband Experience, {prisonerCount} captive(s) taken.";
            foreach (var l in levelUps)
                log += $"\n{l}";
            return log;
        }
    }
}
