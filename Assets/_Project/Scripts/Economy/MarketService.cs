using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Per-settlement buyer wallet + recruit stock. Recruit price is derived from the
    /// recruited troop's actual hireFee (see MarketService.RecruitPrice), not stored here,
    /// so it can't drift out of sync with troops.json.
    /// </summary>
    public class SettlementMarket
    {
        public string nodeId;
        public int buyerGold;
        public bool isPeasant; // villages
        public string recruitTroopId = "void_militia";
        public int recruitStock = 12;

        public static SettlementMarket CreateFor(MapNodeData node, System.Random rng)
        {
            var peasant = node.type == NodeType.Village;
            // Towns/capitals sometimes offer their culture's ranged recruit instead of the melee line.
            var ranged = node.type is NodeType.Town or NodeType.Capital && rng.NextDouble() > 0.5;
            return new SettlementMarket
            {
                nodeId = node.id,
                isPeasant = peasant,
                buyerGold = peasant ? rng.Next(40, 91) : rng.Next(220, 481),
                recruitTroopId = RecruitTroopForCulture(node.controllingFaction, ranged),
                recruitStock = peasant ? rng.Next(4, 9) : rng.Next(8, 16)
            };
        }

        /// <summary>The T1 recruit a settlement offers, by its controlling faction's culture — so a
        /// Butter-held town hires out Butter Thugs, a Nomad town Nomad riders, and everyone else the
        /// Void line. Captured enemies already recruit into their own tree; this is the buy-side mirror.</summary>
        public static string RecruitTroopForCulture(FactionId faction, bool ranged) => faction switch
        {
            FactionId.ButterKlanBoys => ranged ? "butter_slinger" : "butter_thug",
            FactionId.Nomads => ranged ? "nomad_skirmisher" : "nomad_outrider",
            _ => ranged ? "void_archer" : "void_militia"
        };
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

        /// <summary>Re-point a settlement's recruit stock at its new owner's culture after a capture, so
        /// a Butter-taken town starts hiring out Butter troops, a Nomad-taken one Nomad riders, etc.</summary>
        public void OnOwnerChanged(MapNodeData node)
        {
            if (!_markets.TryGetValue(node.id, out var market))
            {
                EnsureMarket(node);
                return;
            }

            var ranged = node.type is NodeType.Town or NodeType.Capital && _rng.NextDouble() > 0.5;
            market.recruitTroopId = SettlementMarket.RecruitTroopForCulture(node.controllingFaction, ranged);
        }

        public void TickDay(System.Random rng)
        {
            foreach (var market in _markets.Values)
            {
                // Prosperity scales how deep a purse the buyers hold and how many recruits are willing —
                // so a raided (or thriving) town shows it at the market.
                var prosperity = GameState.Instance?.ProsperityOf(market.nodeId) ?? GameConstants.ProsperityBaseline;
                var factor = Mathf.Clamp(prosperity / GameConstants.ProsperityBaseline, 0.3f, 1.8f);

                var maxGold = Mathf.RoundToInt((market.isPeasant ? 90 : 480) * factor);
                if (market.buyerGold < maxGold)
                {
                    var regen = market.isPeasant ? rng.Next(10, 25) : rng.Next(30, 80);
                    market.buyerGold += regen;
                    if (market.buyerGold > maxGold) market.buyerGold = maxGold;
                }

                // A friendly notable brings more willing recruits; a wary one, few. (0.4× at zero regard → 1.4× at full.)
                var notable = GameState.Instance?.NotableRelationOf(market.nodeId) ?? GameConstants.NotableRelationBaseline;
                var notableFactor = GameConstants.NotableStockFactorMin + notable / 100f;
                var maxStock = Mathf.Max(1, Mathf.RoundToInt((market.isPeasant ? 8 : 15) * factor * notableFactor));
                if (market.recruitStock < maxStock)
                {
                    market.recruitStock += rng.Next(1, 3);
                    if (market.recruitStock > maxStock) market.recruitStock = maxStock;
                }
            }
        }

        public SettlementMarket Get(string nodeId) =>
            _markets.TryGetValue(nodeId, out var m) ? m : null;

        public bool TryGetItem(string id, out ItemDefinition item) => _items.TryGetValue(id, out item);

        public int OfferPrice(ItemDefinition item, SettlementMarket market)
        {
            if (item == null || item.unsellable) return 0;
            var mult = market.isPeasant ? 0.45f : 0.85f;
            mult *= HeroStatBonuses.TradeSellMultiplier();
            mult *= CompanionBonuses.TradeSellMultiplier();
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

        /// <summary>The troop this settlement actually offers: its base recruit, upgraded to the culture's
        /// next tier once the local notable's regard clears the threshold (trust unlocks better men).</summary>
        public string EffectiveRecruitTroopId(SettlementMarket market)
        {
            var baseId = market.recruitTroopId;
            var gs = GameState.Instance;
            if (gs?.TroopRoster == null) return baseId;
            if (gs.NotableRelationOf(market.nodeId) < GameConstants.NotableUpgradeThreshold) return baseId;
            return gs.TroopRoster.TryGet(baseId, out var def) && !string.IsNullOrEmpty(def.upgradesToId)
                ? def.upgradesToId
                : baseId;
        }

        static float NotableDiscount(string nodeId)
        {
            var rel = GameState.Instance?.NotableRelationOf(nodeId) ?? 0;
            return Mathf.Clamp01(rel / 100f) * GameConstants.NotablePriceDiscountMax;
        }

        /// <summary>The effective recruit's hireFee (from troops.json), adjusted by Trade and the notable's
        /// regard (a friendly notable hires men out cheaper).</summary>
        public int RecruitPrice(SettlementMarket market)
        {
            var troopId = EffectiveRecruitTroopId(market);
            var baseFee = 10;
            if (GameState.Instance?.TroopRoster != null && GameState.Instance.TroopRoster.TryGet(troopId, out var def))
                baseFee = def.hireFee;
            var mult = HeroStatBonuses.TradeBuyMultiplier() * CompanionBonuses.RecruitPriceMultiplier()
                       * (1f - NotableDiscount(market.nodeId));
            return Mathf.Max(1, Mathf.RoundToInt(baseFee * mult));
        }

        public bool TryRecruit(PartyState party, SettlementMarket market, out string log)
        {
            if (market.recruitStock <= 0)
            {
                log = "No willing recruits left here today.";
                return false;
            }

            if (GameState.Instance != null && !GameState.Instance.CanRecruit(1, out log))
                return false;

            var troopId = EffectiveRecruitTroopId(market);
            var price = RecruitPrice(market);
            if (party.gold < price)
            {
                log = $"Need {price}g to hire.";
                return false;
            }

            party.gold -= price;
            market.buyerGold += price;
            market.recruitStock--;
            AddTroop(party, troopId, 1);
            var name = GameState.Instance?.TroopRoster != null && GameState.Instance.TroopRoster.TryGet(troopId, out var def)
                ? def.displayName
                : troopId;
            log = $"Hired 1× {name} for {price}g. Stock {market.recruitStock}.";
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
