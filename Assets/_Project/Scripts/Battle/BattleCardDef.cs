using System;

namespace Voidovia
{
    public enum BattleCardKind
    {
        Command,
        Power
    }

    public enum PowerEffectId
    {
        None,
        EnemyCategoryAttackDown,
        EnemyCategoryAccuracyDown,
        PlayerCategoryAttackUp,
        PlayerMoraleUp,
        EnemyMoraleDown,
        RevealWeakness // flat swing bonus
    }

    [Serializable]
    public class BattleCardDef
    {
        public string id;
        public string displayName;
        public string description;
        public BattleCardKind kind;
        public TroopCategory forCategory; // commands
        public UnitOrder order; // commands
        public PowerEffectId effect = PowerEffectId.None;
        public TroopCategory targetCategory;
        public float magnitude = 0.2f;
        public int bookstorePrice; // 0 = not sold
        public bool rare;
        public bool bossDrop;
    }

    [Serializable]
    public class BattleCardCatalogData
    {
        public BattleCardDef[] cards = Array.Empty<BattleCardDef>();
    }
}
