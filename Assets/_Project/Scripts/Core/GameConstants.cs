namespace Voidovia
{
    /// <summary>
    /// Shared balance knobs. Tune in playtest; M&amp;B-like denar scale.
    /// </summary>
    public static class GameConstants
    {
        public const int HoursPerDay = 24;
        public const float BaseTravelHoursPerEdge = 8f;

        public const int LordVoidMercenaryPurseMin = 200;
        public const int LordVoidMercenaryPurseMax = 400;

        public const int VassalRelationThreshold = 30;

        public const int OriginBackgroundCount = 4;

        // Upgrade gold bands (Warband-ish)
        public const int UpgradeT1ToT2 = 30;
        public const int UpgradeT2ToT3 = 80;
        public const int UpgradeT3ToT4 = 160;
        public const int UpgradeIntoVoidKnight = 320;
        public const int VoidKnightMountPremium = 100;
    }
}
