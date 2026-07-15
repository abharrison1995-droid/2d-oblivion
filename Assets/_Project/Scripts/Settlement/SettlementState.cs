using System;
using System.Collections.Generic;

namespace Voidovia
{
    [Serializable]
    public class BuildingState
    {
        public BuildingType type;
        public int tier; // 0 = not built (for upgradeables), 1+ built
        public bool isBuilt;
    }

    /// <summary>
    /// Owned settlement. Governor's Grotto gates other building tiers.
    /// Church of the Black Fluffy Tail unlocks at Grotto T4.
    /// </summary>
    public class SettlementState
    {
        public string nodeId;
        public string displayName;
        public FactionId culture = FactionId.Voidovia;
        public readonly Dictionary<BuildingType, BuildingState> buildings = new();

        public int GrottoTier => GetTier(BuildingType.GovernorsGrotto);

        public SettlementState(string nodeId, string displayName)
        {
            this.nodeId = nodeId;
            this.displayName = displayName;
            Ensure(BuildingType.Tavern, 1, true);
            Ensure(BuildingType.GovernorsGrotto, 1, true);
        }

        public int GetTier(BuildingType type) =>
            buildings.TryGetValue(type, out var b) && b.isBuilt ? b.tier : 0;

        public bool CanUpgrade(BuildingType type, int targetTier)
        {
            if (type == BuildingType.GovernorsGrotto)
                return targetTier <= 4 && targetTier == GrottoTier + 1;

            if (type == BuildingType.ChurchOfTheBlackFluffyTail)
                return GrottoTier >= 4 && GetTier(type) == 0;

            if (type is BuildingType.Sewers or BuildingType.Aqueduct or BuildingType.WheatFields
                or BuildingType.Mill or BuildingType.Merchants or BuildingType.Dungeon
                or BuildingType.Granary or BuildingType.Tavern)
            {
                return !buildings.TryGetValue(type, out var existing) || !existing.isBuilt;
            }

            // Tiered military / walls / armory
            var maxTier = type == BuildingType.Walls ? 3 : 4;
            if (targetTier < 1 || targetTier > maxTier)
                return false;
            if (targetTier > GrottoTier)
                return false;

            var current = GetTier(type);
            return targetTier == current + 1 || (current == 0 && targetTier == 1);
        }

        public bool TryBuildOrUpgrade(BuildingType type, int targetTier, out string error)
        {
            if (!CanUpgrade(type, targetTier))
            {
                error = type == BuildingType.ChurchOfTheBlackFluffyTail
                    ? "Requires Governor's Grotto tier 4."
                    : "Governor's Grotto must match or exceed this tier first.";
                return false;
            }

            Ensure(type, targetTier, true);
            error = null;
            return true;
        }

        /// <summary>Directly restores a building's state from a save file, bypassing CanUpgrade gating.</summary>
        public void RestoreBuilding(BuildingType type, int tier, bool isBuilt) => Ensure(type, tier, isBuilt);

        public bool CanRecruitVoidKnight() =>
            GetTier(BuildingType.ChurchOfTheBlackFluffyTail) >= 1 && GrottoTier >= 4;

        public string HighestBarracksRecruit() => GetTier(BuildingType.Barracks) switch
        {
            >= 4 => "void_peacekeeper",
            3 => "void_sergeant",
            2 => "trained_void_militia",
            1 => "void_militia",
            _ => null
        };

        public string HighestArcheryRecruit() => GetTier(BuildingType.ArcheryRange) switch
        {
            >= 4 => "void_bow_cohort",
            3 => "void_longbowman",
            2 => "trained_void_archer",
            1 => "void_archer",
            _ => null
        };

        public string HighestStableRecruit() => GetTier(BuildingType.MilitaryStables) switch
        {
            >= 4 => "dark_knight",
            3 => "trained_void_rider",
            2 => "void_rider",
            1 => "voidovan_cattle_rustler",
            _ => null
        };

        void Ensure(BuildingType type, int tier, bool built)
        {
            buildings[type] = new BuildingState
            {
                type = type,
                tier = tier,
                isBuilt = built
            };
        }
    }
}
