using System;

namespace Voidovia
{
    public enum CompanionBonusType
    {
        BattlePower,
        TradeSellBonus,
        WageDiscount,
        ScoutingBonus,
        CommandSwingBonus,
        LootBonus,
        RecruitDiscount,
        /// <summary>Surgeon role: more casualties are saved as wounded (vs dead), and the wounded
        /// mend faster. Same value feeds both the battle dead/wounded split and the daily recovery rate.</summary>
        FieldMedicine
    }

    [Serializable]
    public class CompanionDefinition
    {
        public string id;
        public string displayName;
        public string role;
        public string traitDescription;
        public CompanionBonusType bonusType;
        public float bonusValue;
        public int recruitCost;
        public string homeNodeId;
        public bool isQuestReward;
    }

    [Serializable]
    public class CompanionCatalog
    {
        public CompanionDefinition[] companions = Array.Empty<CompanionDefinition>();

        public bool TryGet(string id, out CompanionDefinition def)
        {
            foreach (var c in companions)
            {
                if (c.id != id) continue;
                def = c;
                return true;
            }

            def = null;
            return false;
        }
    }
}
