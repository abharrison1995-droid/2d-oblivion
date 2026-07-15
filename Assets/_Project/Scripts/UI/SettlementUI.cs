using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    public class SettlementUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _title;
        Text _body;
        RectTransform _list;
        System.Action _onClose;
        bool _showBuildings;

        public void Open(System.Action onClose = null)
        {
            _onClose = onClose;
            _showBuildings = false;
            Ensure();
            _canvas.gameObject.SetActive(true);
            Rebuild();
        }

        void Ensure()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("SettlementCanvas", 30);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.07f, 0.08f, 0.09f, 0.97f));
            _title = UiFactory.Label(root, "Title", "Settlement", 34, TextAnchor.UpperCenter, new Color(0.93f, 0.86f, 0.7f));
            Stretch(_title, 0.05f, 0.9f, 0.95f, 0.98f);

            UiFactory.Button(root, "TabMarket", "Market", new Vector2(0.05f, 0.80f), new Vector2(0.49f, 0.88f), () =>
            {
                _showBuildings = false;
                Rebuild();
            });
            UiFactory.Button(root, "TabBuildings", "Buildings", new Vector2(0.51f, 0.80f), new Vector2(0.95f, 0.88f), () =>
            {
                _showBuildings = true;
                Rebuild();
            });

            _body = UiFactory.Label(root, "Body", "", 20, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.76f));
            Stretch(_body, 0.06f, 0.71f, 0.94f, 0.79f);
            _list = UiFactory.Panel(root, "List", new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.70f), new Color(0, 0, 0, 0.2f));
            UiFactory.Button(root, "Close", "Leave", new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.1f), () =>
            {
                _canvas.gameObject.SetActive(false);
                _onClose?.Invoke();
            });
        }

        static void Stretch(Text t, float x0, float y0, float x1, float y1)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
        }

        void Rebuild()
        {
            foreach (Transform c in _list) Destroy(c.gameObject);
            var g = GameState.Instance;
            if (!g.Map.TryGetNode(g.Party.currentNodeId, out var node))
            {
                _body.text = "Not in a settlement.";
                return;
            }

            _title.text = node.displayName;
            if (_showBuildings)
                RebuildBuildings(g, node);
            else
                RebuildMarket(g, node);
        }

        void RebuildMarket(GameState g, MapNodeData node)
        {
            g.Market.EnsureMarket(node);
            var market = g.Market.Get(node.id);
            var buyer = market.isPeasant ? "Peasants" : "Merchant";
            _body.text = $"{buyer} gold: {market.buyerGold}g   Your gold: {g.Party.gold}g";

            var y = 0.96f;
            if (node.hasRecruitment || node.type == NodeType.Village)
            {
                UiFactory.Button(_list, "Recruit", $"Recruit 1× {market.recruitTroopId} ({market.recruitPrice}g)",
                    new Vector2(0f, y - 0.12f), new Vector2(1f, y), () =>
                    {
                        g.Market.TryRecruit(g.Party, market, out var log);
                        _body.text = log + $"\n{buyer} gold: {market.buyerGold}g";
                        Rebuild();
                    });
                y -= 0.14f;
            }

            var hdr = UiFactory.Label(_list, "SellHdr", $"Sell loot to {buyer.ToLowerInvariant()} (they pay {(market.isPeasant ? "less" : "more")})", 18, TextAnchor.MiddleLeft, new Color(0.85f, 0.8f, 0.7f));
            var hr = hdr.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0f, y - 0.08f);
            hr.anchorMax = new Vector2(1f, y);
            y -= 0.1f;

            if (g.Party.inventory.Count == 0)
            {
                var empty = UiFactory.Label(_list, "Empty", "Inventory empty.", 18, TextAnchor.MiddleLeft, Color.gray);
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0f, y - 0.08f);
                er.anchorMax = new Vector2(1f, y);
                return;
            }

            foreach (var stack in g.Party.inventory)
            {
                if (!g.Market.TryGetItem(stack.itemId, out var item)) continue;
                if (item.unsellable) continue;
                var price = g.Market.OfferPrice(item, market);
                var id = stack.itemId;
                var top = y;
                var bottom = y - 0.11f;
                y = bottom - 0.02f;
                UiFactory.Button(_list, id + top,
                    $"{item.displayName} x{stack.count} → {price}g  (buyer {market.buyerGold}g)",
                    new Vector2(0f, bottom), new Vector2(1f, top),
                    () =>
                    {
                        g.Market.TrySell(g.Party, market, id, out var log);
                        _body.text = log;
                        Rebuild();
                    });
            }
        }

        void RebuildBuildings(GameState g, MapNodeData node)
        {
            var settlement = g.GetOrCreateSettlement(node);
            _body.text = $"Governor's Grotto tier {settlement.GrottoTier}/4   Your gold: {g.Party.gold}g\n(Costs are early placeholders — tune in playtest.)";

            var y = 0.97f;

            var nextGrotto = settlement.GrottoTier + 1;
            if (settlement.CanUpgrade(BuildingType.GovernorsGrotto, nextGrotto))
            {
                var cost = BuildCost(BuildingType.GovernorsGrotto, nextGrotto);
                Row(ref y, $"Upgrade Governor's Grotto → tier {nextGrotto} ({cost}g)",
                    () => TryBuild(g, settlement, BuildingType.GovernorsGrotto, nextGrotto, cost));
            }
            else
            {
                Row(ref y, $"Governor's Grotto — tier {settlement.GrottoTier} (max)", null);
            }

            BuildingRow(g, settlement, BuildingType.Barracks, "Barracks", settlement.HighestBarracksRecruit(), ref y);
            BuildingRow(g, settlement, BuildingType.ArcheryRange, "Archery Range", settlement.HighestArcheryRecruit(), ref y);
            BuildingRow(g, settlement, BuildingType.MilitaryStables, "Military Stables", settlement.HighestStableRecruit(), ref y);

            if (settlement.GetTier(BuildingType.ChurchOfTheBlackFluffyTail) > 0)
            {
                Row(ref y, "Church of the Black Fluffy Tail — built", null);
            }
            else if (settlement.CanUpgrade(BuildingType.ChurchOfTheBlackFluffyTail, 1))
            {
                var cost = BuildCost(BuildingType.ChurchOfTheBlackFluffyTail, 1);
                Row(ref y, $"Build Church of the Black Fluffy Tail ({cost}g)",
                    () => TryBuild(g, settlement, BuildingType.ChurchOfTheBlackFluffyTail, 1, cost));
            }
            else
            {
                Row(ref y, "Church of the Black Fluffy Tail — requires Grotto tier 4", null);
            }

            if (settlement.CanRecruitVoidKnight())
            {
                RecruitRow(g, "void_knight_foot", ref y);
                RecruitRow(g, "void_knight_mount", ref y);
            }
        }

        void BuildingRow(GameState g, SettlementState settlement, BuildingType type, string label, string unlockedRecruitId, ref float y)
        {
            var tier = settlement.GetTier(type);
            var nextTier = tier + 1;
            if (settlement.CanUpgrade(type, nextTier))
            {
                var cost = BuildCost(type, nextTier);
                var verb = tier == 0 ? "Build" : "Upgrade";
                Row(ref y, $"{verb} {label} → tier {nextTier} ({cost}g)",
                    () => TryBuild(g, settlement, type, nextTier, cost));
            }
            else
            {
                var reason = tier >= 4 ? "max" : "requires higher Grotto tier";
                Row(ref y, $"{label} — tier {tier} ({reason})", null);
            }

            if (!string.IsNullOrEmpty(unlockedRecruitId))
                RecruitRow(g, unlockedRecruitId, ref y);
        }

        void RecruitRow(GameState g, string troopId, ref float y)
        {
            if (!g.TroopRoster.TryGet(troopId, out var def)) return;
            Row(ref y, $"Recruit 1× {def.displayName} ({def.hireFee}g)", () => TryRecruitTroop(g, def));
        }

        void Row(ref float y, string label, System.Action onClick)
        {
            var top = y;
            var bottom = y - 0.10f;
            y = bottom - 0.015f;
            var btn = UiFactory.Button(_list, "Row" + _list.childCount, label, new Vector2(0f, bottom), new Vector2(1f, top), onClick);
            btn.interactable = onClick != null;
        }

        void TryBuild(GameState g, SettlementState settlement, BuildingType type, int targetTier, int cost)
        {
            if (g.Party.gold < cost)
            {
                _body.text = $"Need {cost}g for that.";
                return;
            }

            if (!settlement.TryBuildOrUpgrade(type, targetTier, out var error))
            {
                _body.text = error;
                return;
            }

            g.Party.gold -= cost;
            _body.text = $"Built for {cost}g.";
            Rebuild();
        }

        void TryRecruitTroop(GameState g, TroopDefinition def)
        {
            if (g.Party.gold < def.hireFee)
            {
                _body.text = $"Need {def.hireFee}g to hire {def.displayName}.";
                return;
            }

            g.Party.gold -= def.hireFee;
            g.AddTroop(def.id, 1);
            _body.text = $"Hired 1× {def.displayName} for {def.hireFee}g.";
            Rebuild();
        }

        /// <summary>Placeholder build costs — not balanced, tune in playtest.</summary>
        static int BuildCost(BuildingType type, int targetTier) => type switch
        {
            BuildingType.GovernorsGrotto => targetTier * targetTier * 100,
            BuildingType.ChurchOfTheBlackFluffyTail => 400,
            _ => targetTier * 80
        };
    }
}
