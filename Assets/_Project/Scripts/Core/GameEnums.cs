using System;

namespace Voidovia
{
    public enum NodeType
    {
        Capital,
        Town,
        Castle,
        Village,
        QuestLair,
        Wilderness,
        BanditCamp
    }

    public enum TerrainType
    {
        Road,
        Forest,
        Hill,
        Marsh,
        Mountain
    }

    public enum TroopCategory
    {
        Infantry,
        Spear,
        Archer,
        Crossbow,
        Mounted,
        Specialty
    }

    public enum ItemType
    {
        Weapon,
        Armour,
        TradeGood,
        Trophy,
        Consumable,
        Quest
    }

    public enum EquipSlot
    {
        None,
        Weapon,
        Armour
    }

    public enum BuildingType
    {
        GovernorsGrotto,
        Barracks,
        ArcheryRange,
        MilitaryStables,
        Armory,
        Walls,
        Dungeon,
        Granary,
        Sewers,
        Aqueduct,
        WheatFields,
        Mill,
        Merchants,
        Tavern,
        ChurchOfTheBlackFluffyTail,
        MooseCavalryYard,
        CinderFoundry
    }

    public enum FactionId
    {
        Independent,
        Voidovia,
        ButterKlanBoys,
        RaXaelDynasty,
        SmallSpine,
        LongSpines,
        Orthodoxy,
        Nomads,
        Healers,
        Traders,
        Bandits
    }

    public enum QuestState
    {
        Offered,
        Active,
        Completed,
        Failed,
        Expired
    }

    public enum BattlePhase
    {
        Prep,
        Opening,
        Mid,
        Crisis,
        Resolve
    }

    public enum UnitOrder
    {
        Hold,
        Push,
        FallBack,
        Shieldwall,
        Brace,
        LooseVolley,
        FocusFire,
        ConserveAmmo,
        Flank,
        Charge,
        Screen,
        Pursue,
        CommitSpecial
    }

    [Flags]
    public enum ReputationFlag
    {
        None = 0,
        Good = 1 << 0,
        Infamous = 1 << 1,
        WantedInVoidovia = 1 << 2
    }
}
