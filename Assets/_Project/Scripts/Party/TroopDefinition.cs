using System;
using UnityEngine;

namespace Voidovia
{
    [Serializable]
    public class TroopDefinition
    {
        public string id;
        public string displayName;
        public TroopCategory category;
        public int tier = 1;
        public int hireFee = 15;
        public int weeklyWage = 2;
        public float foodDrain = 1f;
        public int melee = 10;
        public int ranged = 0;
        public int armour = 5;
        public int morale = 50;
        public bool isMounted;
        public bool isSpecialty;
        public string upgradesToId;
        public int upgradeGold = GameConstants.UpgradeT1ToT2;
        public bool requiresBuilding;
        public BuildingType requiredBuilding;
        public int requiredBuildingTier = 1;
    }

    [Serializable]
    public class TroopRosterData
    {
        public TroopDefinition[] troops = Array.Empty<TroopDefinition>();

        public bool TryGet(string id, out TroopDefinition def)
        {
            foreach (var t in troops)
            {
                if (t.id != id) continue;
                def = t;
                return true;
            }

            def = null;
            return false;
        }
    }
}
