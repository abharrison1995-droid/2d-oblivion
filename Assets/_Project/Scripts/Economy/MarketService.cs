using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Per-settlement buyer wallet + recruit stock (mostly T1 @ ~15g).
    /// </summary>
    public class SettlementMarket
    {
        public string nodeId;
        public int buyerGold;
        public bool isPeasant; // villages
        public string recruitTroopId = "void_militia";
        public int recruitPrice = 15;
        public int recruitStock = 12;

        public static SettlementMarket CreateFor(MapNodeData node, System.Random rng)
        {
            var peasant = node.type == NodeType.Village;
            return new SettlementMarket
            {
                nodeId = node.id,
                isPeasant = peasant,
                buyerGold = peasant ? rng.Next(40, 91) : rng.Next(220, 481),
                recruitTroopId = node.type is NodeType.Town or NodeType.Capital
                    ? (rng.NextDouble() > 0.5 ? "void_militia" : "void_archer")
                    : "void_militia",
                recruitPrice = 15,
                recruitStock = peasant ? rng.Next(4, 9) : rng.Next(8, 16)
            };
        }
    }

    public class MarketService
    {
        readonly Dictionary<string, SettlementMarket> _markets = new();
        readonly Dictionary<string, ItemDefinition> _items = new();
        System.Random _rng = new();

        public void LoadItems(IEnumerable<ItemDefinition> items)
        {
            _items.Clear();
            foreach (var i in items)
                _items[i.id] = i;
        }

        public void SetRng(System.Random rng) => _rng = rng ?? _rng;

        public void EnsureMarket(MapNodeData node)
        {
            if (!_markets.ContainsKey(node.id))
                _markets[node.id] = SettlementMarket.CreateFor(node, _rng);
        }

        public SettlementMarket Get(string nodeId) =>
            _markets.TryGetValue(nodeId, out var m) ? m : null;

        public bool TryGetItem(string id, out ItemDefinition item) => _items.TryGetValue(id, out item);

        public int OfferPrice(ItemDefinition item, SettlementMarket market)
        {
            if (item == null || item.unsellable) return 0;
            var mult = market.isPeasant ? 0.45f : 0.85f;
            return Math.Max(1, (int)(item.baseValue * mult));
        }

        public bool TrySell(PartyState party, SettlementMarket market, string itemId, out string log)
        {
            if (!_items.TryGetValue(itemId, out var item) || item.unsellable)
            {
                log = "Cannot sell that.";
                return false;
            }

            var stack = FindInv(party, itemId);
            if (stack == null || stack.count <= 0)
            {
                log = "You don't have that.";
                return false;
            }

            var price = OfferPrice(item, market);
            if (market.buyerGold < price)
            {
                log = market.isPeasant
                    ? $"Peasants only have {market.buyerGold}g left — too poor for {item.displayName} ({price}g)."
                    : $"Merchant purse is thin ({market.buyerGold}g). Needs {price}g for {item.displayName}.";
                return false;
            }

            stack.count--;
            if (stack.count <= 0) party.inventory.Remove(stack);
            market.buyerGold -= price;
            party.gold += price;
            log = $"Sold {item.displayName} for {price}g. Buyer gold now {market.buyerGold}g.";
            return true;
        }

        public bool TryRecruit(PartyState party, SettlementMarket market, out string log)
        {
            if (market.recruitStock <= 0)
            {
                log = "No willing recruits left here today.";
                return false;
            }

            if (party.gold < market.recruitPrice)
            {
                log = $"Need {market.recruitPrice}g to hire.";
                return false;
            }

            party.gold -= market.recruitPrice;
            market.recruitStock--;
            AddTroop(party, market.recruitTroopId, 1);
            log = $"Hired 1× {market.recruitTroopId} for {market.recruitPrice}g. Stock {market.recruitStock}.";
            return true;
        }

        static InventoryStack FindInv(PartyState party, string itemId)
        {
            foreach (var s in party.inventory)
                if (s.itemId == itemId) return s;
            return null;
        }

        static void AddTroop(PartyState party, string troopId, int count)
        {
            foreach (var t in party.troops)
            {
                if (t.troopId != troopId) continue;
                t.count += count;
                return;
            }

            party.troops.Add(new TroopStack { troopId = troopId, count = count });
        }
    }
}
