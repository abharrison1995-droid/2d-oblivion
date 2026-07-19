using System;
using System.Collections.Generic;

namespace Voidovia
{
    public class CharacterStats
    {
        public string name = "Wanderer";
        public string originId;
        public int combat = 5;
        public int leadership = 5;
        public int tactics = 5;
        public int trade = 5;
        public int scouting = 5;

        public int level = 1;
        public int xp;
        public int unspentSkillPoints;

        /// <summary>
        /// Permanent investment in Warband Experience gain — spent skill points only ever
        /// raise this. Distinct from PartyState.warbandExperience, which is the spendable
        /// pool this rank makes accrue faster.
        /// </summary>
        public int warbandExperienceRank;

        public int XpForNextLevel() => (int)Math.Round(GameConstants.XpCurveBase * Math.Pow(level, GameConstants.XpCurveExponent));

        /// <summary>Adds XP, applying every level-up the amount crosses. Returns one log line per level gained.</summary>
        public List<string> AddXp(int amount)
        {
            var logs = new List<string>();
            if (amount <= 0) return logs;

            xp += amount;
            while (xp >= XpForNextLevel())
            {
                xp -= XpForNextLevel();
                level++;
                unspentSkillPoints += GameConstants.SkillPointsPerLevel;
                logs.Add($"Level up! You are now level {level} ({GameConstants.SkillPointsPerLevel} skill point{(GameConstants.SkillPointsPerLevel == 1 ? "" : "s")} to spend).");
            }

            return logs;
        }

        public int WarbandExperienceDailyGain() =>
            GameConstants.BaseWarbandExperiencePerDay + warbandExperienceRank * GameConstants.WarbandExperiencePerRankBonus;
    }
}
