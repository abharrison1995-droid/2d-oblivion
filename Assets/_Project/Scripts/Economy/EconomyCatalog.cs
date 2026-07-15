using System;
using UnityEngine;

namespace Voidovia
{
    [Serializable]
    public class FoodDefinition
    {
        public string id;
        public string displayName;
        public int basePrice = 10;
        public float fillPerUnit = 1f;
        public int moraleBonus;
    }

    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string displayName;
        public ItemType type;
        public int baseValue = 25;
        public float weight = 1f;
        public bool unsellable;
        public string quality = "serviceable";
        public EquipSlot equipSlot = EquipSlot.None;
        public int combatBonus;
        public int armourBonus;
    }

    [Serializable]
    public class EconomyCatalog
    {
        public FoodDefinition[] foods = Array.Empty<FoodDefinition>();
        public ItemDefinition[] items = Array.Empty<ItemDefinition>();
    }
}
