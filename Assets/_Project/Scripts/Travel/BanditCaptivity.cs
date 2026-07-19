using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Consequence of losing a fight to bandits: they lift a cut of your gold, hold the
    /// party for a stretch, and turn you loose near the nearest bandit camp.
    /// </summary>
    public static class BanditCaptivity
    {
        public static string Apply(GameState g, string preferredReleaseNodeId)
        {
            var stolen = Mathf.Min(GameConstants.BanditGoldTheftCap,
                Mathf.RoundToInt(g.Party.gold * GameConstants.BanditGoldTheftFraction));
            g.Party.gold -= stolen;
            g.Party.AddMorale(-15f);

            g.Party.hours += GameConstants.BanditCaptivityHours;
            while (g.Party.hours >= GameConstants.HoursPerDay)
            {
                g.Party.hours -= GameConstants.HoursPerDay;
                g.Party.day++;
                g.OnNewDay();
            }

            if (g.Journey.IsActive)
                g.Journey.Cancel();

            var releaseNodeId = preferredReleaseNodeId;
            if (string.IsNullOrEmpty(releaseNodeId) || !g.Map.TryGetNode(releaseNodeId, out _))
                releaseNodeId = g.Party.currentNodeId;

            var releaseName = releaseNodeId;
            if (g.Map.TryGetNode(releaseNodeId, out var releaseNode))
            {
                g.Party.currentNodeId = releaseNodeId;
                releaseName = releaseNode.displayName;
            }

            return stolen > 0
                ? $"They drag you off to their camp and lift {stolen}g before turning you loose near {releaseName}."
                : $"They drag you off to their camp — you had nothing worth taking — and turn you loose near {releaseName}.";
        }
    }
}
