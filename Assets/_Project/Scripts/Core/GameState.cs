using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Session singleton. Holds party, map, quest, and service directors.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public PartyState Party { get; private set; }
        public CharacterStats Hero { get; private set; }
        public WorldGraph Map { get; private set; }
        public TravelDirector Travel { get; private set; }
        public BattleDirector Battle { get; private set; }
        public EconomyService Economy { get; private set; }
        public StolenItemQuestController Act1Quest { get; private set; }
        public Dictionary<string, SettlementState> Settlements { get; } = new();

        public TroopRosterData TroopRoster { get; private set; }
        public EconomyCatalog Catalog { get; private set; }
        public System.Random Rng { get; private set; } = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Party = new PartyState();
            Hero = new CharacterStats();
            Map = new WorldGraph();
            Travel = new TravelDirector();
            Battle = new BattleDirector();
            Economy = new EconomyService();
            Act1Quest = new StolenItemQuestController();
        }

        public void BindData(WorldMapData map, TroopRosterData troops, EconomyCatalog catalog)
        {
            TroopRoster = troops;
            Catalog = catalog;
            Map.Load(map);
            Economy.LoadFood(catalog.foods);
            Economy.LoadTroops(troops.troops);

            Act1Quest.Log += msg => Debug.Log($"[Quest] {msg}");
            Act1Quest.LairSpawnRequested += (node, road) =>
            {
                if (Map.TryGetNode(node.parentSettlementId, out var parent))
                {
                    node.mapPosition = parent.mapPosition + new Vector2(0.6f, -0.4f);
                }

                Map.AddTemporaryNode(node, road);
            };
        }

        public void ApplyOrigin(OriginChoice origin)
        {
            Hero.ApplyOrigin(origin);
            Party.currentNodeId = origin.spawnNodeId;
            Party.gold = origin.startingGold;
            Party.troops.Clear();
            foreach (var troopId in origin.startingTroopIds)
                AddTroop(troopId, 1);

            Party.food.Add(new InventoryStack { itemId = "grain", count = 8 });
            Party.food.Add(new InventoryStack { itemId = "bread", count = 4 });
            Party.companionIds.Add("bangkok_kuo");
            Party.relations[FactionId.Voidovia] = 0;
            Party.reputation = ReputationFlag.Good;

            if (origin.id == "marsh_outlaw")
                Party.reputation |= ReputationFlag.WantedInVoidovia;
        }

        public void AddTroop(string troopId, int count)
        {
            foreach (var stack in Party.troops)
            {
                if (stack.troopId != troopId)
                    continue;
                stack.count += count;
                return;
            }

            Party.troops.Add(new TroopStack { troopId = troopId, count = count });
        }

        public bool TryOfferMercenaryContract(out string message)
        {
            if (Party.isVoidoviaMercenary)
            {
                message = "You already ride for Voidovia's purse.";
                return false;
            }

            Party.isVoidoviaMercenary = true;
            message =
                $"Lord Void offers a mercenary contract: {GameConstants.LordVoidMercenaryPurseMin}–{GameConstants.LordVoidMercenaryPurseMax} gold per week. Enough for some wages and food — not a deathball.";
            return true;
        }

        public bool TryOfferVassalage(out string message)
        {
            if (!Party.CanBecomeVassal())
            {
                message =
                    $"Need mercenary standing, relation ≥ {GameConstants.VassalRelationThreshold}, and good reputation.";
                return false;
            }

            Party.isVoidoviaVassal = true;
            message = "Lord Void names you vassal. Land talk comes later — but the dynasty door is open.";
            return true;
        }

        public int RollMercenaryPurse() =>
            Rng.Next(GameConstants.LordVoidMercenaryPurseMin, GameConstants.LordVoidMercenaryPurseMax + 1);
    }
}
