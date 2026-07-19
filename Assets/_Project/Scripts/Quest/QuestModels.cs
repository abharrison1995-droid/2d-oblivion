using System;

namespace Voidovia
{
    public enum QuestTemplateType
    {
        BanditHideoutClear,
        EscortCaravan,
        DeliveryFetch,
        BountyHunt,
        TroopLevy
    }

    public enum TroopLevyArc
    {
        VillageBanditProtection,
        NobleAtWar,
        SettlementManpowerLoss
    }

    [Serializable]
    public class QuestTemplate
    {
        public string id;
        public QuestTemplateType type;
        public string titlePattern; // {0} = target/flavor name
        public int minReward;
        public int maxReward;
        public float minDeadlineDays;
        public float maxDeadlineDays;
    }

    [Serializable]
    public class QuestTemplateCatalog
    {
        public QuestTemplate[] templates = Array.Empty<QuestTemplate>();

        public bool TryGet(QuestTemplateType type, out QuestTemplate template)
        {
            foreach (var t in templates)
            {
                if (t.type != type) continue;
                template = t;
                return true;
            }

            template = null;
            return false;
        }
    }

    /// <summary>
    /// A generated quest, offered by a settlement or accepted into the player's active list.
    /// One shape covers all five templates; unused fields for a given type stay blank/zero.
    /// </summary>
    [Serializable]
    public class QuestInstance
    {
        public string instanceId;
        public string templateId;
        public QuestTemplateType type;
        public QuestState state = QuestState.Offered;
        public string giverNodeId;
        public string title;
        public string description;
        public int rewardGold;
        public int deadlineDay;

        // Bandit hideout clear / bounty hunt — spawned temporary node + link, cleaned up on resolve.
        public string targetNodeId;
        public string targetRoadId;
        public string bountyTargetName;

        // Escort caravan
        public string escortDestinationNodeId;

        // Delivery / fetch
        public string deliveryDestinationNodeId;

        // Troop levy
        public string levyTroopId;
        public int levyCount;
        public TroopLevyArc levyArc;
    }
}
