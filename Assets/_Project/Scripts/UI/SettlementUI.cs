using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    public class SettlementUI : MonoBehaviour
    {
        enum Tab { Market, Buildings, Quests }

        Canvas _canvas;
        Text _title;
        Text _body;
        RectTransform _list;
        System.Action _onClose;
        Tab _tab;

        public void Open(System.Action onClose = null)
        {
            _onClose = onClose;
            _tab = Tab.Market;
            Ensure();
            _canvas.gameObject.SetActive(true);
            Rebuild();
        }

        void Ensure()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("SettlementCanvas", 30);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, UiFactory.Theme.PanelBackground);
            _title = UiFactory.Label(root, "Title", "Settlement", 34, TextAnchor.UpperCenter, UiFactory.Theme.TextTitle);
            Stretch(_title, 0.05f, 0.9f, 0.95f, 0.98f);

            UiFactory.Button(root, "TabMarket", "Market", new Vector2(0.05f, 0.80f), new Vector2(0.3367f, 0.88f), () =>
            {
                _tab = Tab.Market;
                Rebuild();
            });
            UiFactory.Button(root, "TabBuildings", "Buildings", new Vector2(0.3567f, 0.80f), new Vector2(0.6433f, 0.88f), () =>
            {
                _tab = Tab.Buildings;
                Rebuild();
            });
            UiFactory.Button(root, "TabQuests", "Quests", new Vector2(0.6633f, 0.80f), new Vector2(0.95f, 0.88f), () =>
            {
                _tab = Tab.Quests;
                Rebuild();
            });

            _body = UiFactory.Label(root, "Body", "", 20, TextAnchor.UpperLeft, UiFactory.Theme.TextDim);
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
            switch (_tab)
            {
                case Tab.Buildings:
                    RebuildBuildings(g, node);
                    break;
                case Tab.Quests:
                    RebuildQuests(g, node);
                    break;
                default:
                    RebuildMarket(g, node);
                    break;
            }
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
                var effId = g.Market.EffectiveRecruitTroopId(market);
                var effName = g.TroopRoster != null && g.TroopRoster.TryGet(effId, out var edef) ? edef.displayName : effId;
                var regard = g.NotableRelationOf(node.id);
                var (rTop, rBottom) = UiFactory.NextRow(ref y, 0.12f, 0.02f);
                UiFactory.Button(_list, "Recruit",
                    $"Recruit 1× {effName} ({g.Market.RecruitPrice(market)}g) · {market.recruitStock} left · notable regard {regard}/100",
                    new Vector2(0f, rBottom), new Vector2(1f, rTop), () =>
                    {
                        g.Market.TryRecruit(g.Party, market, out var log);
                        _body.text = log + $"\n{buyer} gold: {market.buyerGold}g";
                        Rebuild();
                    }, UiFactory.ButtonStyle.Primary);
            }

            var hdr = UiFactory.Label(_list, "SellHdr", $"Sell loot to {buyer.ToLowerInvariant()} (they pay {(market.isPeasant ? "less" : "more")})", 18, TextAnchor.MiddleLeft, UiFactory.Theme.TextDim);
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
                var (top, bottom) = UiFactory.NextRow(ref y, 0.11f, 0.02f);
                UiFactory.Button(_list, id + top,
                    $"{item.displayName} x{stack.count} → {price}g  (buyer {market.buyerGold}g)",
                    new Vector2(0f, bottom), new Vector2(1f, top),
                    () =>
                    {
                        g.Market.TrySell(g.Party, market, id, out var log);
                        _body.text = log;
                        Rebuild();
                    }, UiFactory.ButtonStyle.Primary);
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

            SpecialtyRow(g, settlement, BuildingType.ChurchOfTheBlackFluffyTail, "Church of the Black Fluffy Tail", ref y);
            if (settlement.CanRecruitVoidKnight())
            {
                RecruitRow(g, "void_knight_foot", ref y);
                RecruitRow(g, "void_knight_mount", ref y);
            }

            SpecialtyRow(g, settlement, BuildingType.MooseCavalryYard, "Moose Cavalry Yard", ref y);
            if (settlement.CanRecruitMooseLancer())
                RecruitRow(g, "void_moose_lancer", ref y);

            SpecialtyRow(g, settlement, BuildingType.CinderFoundry, "Cinder Foundry", ref y);
            if (settlement.CanRecruitCinderGuard())
                RecruitRow(g, "void_cinder_guard", ref y);
        }

        /// <summary>Shared built / can-build / needs-Grotto-tier-4 row for a Grotto-tier-4-gated,
        /// single-build specialty building (Church + the newer specialties).</summary>
        void SpecialtyRow(GameState g, SettlementState settlement, BuildingType type, string label, ref float y)
        {
            if (settlement.GetTier(type) > 0)
            {
                Row(ref y, $"{label} — built", null);
            }
            else if (settlement.CanUpgrade(type, 1))
            {
                var cost = BuildCost(type, 1);
                Row(ref y, $"Build {label} ({cost}g)", () => TryBuild(g, settlement, type, 1, cost));
            }
            else
            {
                Row(ref y, $"{label} — requires Grotto tier 4", null);
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
            var price = Mathf.Max(1, Mathf.RoundToInt(def.hireFee * HeroStatBonuses.TradeBuyMultiplier()));
            Row(ref y, $"Recruit 1× {def.displayName} ({price}g)", () => TryRecruitTroop(g, def, price));
        }

        void RebuildQuests(GameState g, MapNodeData node)
        {
            _body.text = $"Your gold: {g.Party.gold}g   Day {g.Party.day}";
            var y = 0.97f;

            var offers = g.QuestBoard.OffersAt(node.id);
            if (offers.Count == 0)
                Row(ref y, "No quests posted here today.", null);

            foreach (var offer in offers)
            {
                var inst = offer;
                Row(ref y, $"{inst.title} — {inst.rewardGold}g (due in {Mathf.Max(0, inst.deadlineDay - g.Party.day)}d)", null);
                Row(ref y, inst.description, null);
                Row(ref y, "Accept", () =>
                {
                    g.QuestBoard.TryAccept(node.id, inst.instanceId, g, out var log);
                    _body.text = log;
                    Rebuild();
                });
            }

            var active = new List<QuestInstance>(g.QuestBoard.ActiveAt(node.id));
            if (active.Count > 0)
                Row(ref y, "— In progress —", null);

            foreach (var inst in active)
            {
                var toTurnIn = inst;
                Row(ref y, $"{inst.title} — {StatusFor(inst, g)}", null);
                if (g.QuestBoard.IsReadyToTurnIn(inst, g.Party, g.Party.day))
                {
                    Row(ref y, $"Turn in ({inst.rewardGold}g)", () =>
                    {
                        g.QuestBoard.TryTurnIn(toTurnIn, g, out var log);
                        _body.text = log;
                        Rebuild();
                    });
                }
            }
        }

        static string StatusFor(QuestInstance inst, GameState g) => inst.type switch
        {
            QuestTemplateType.BanditHideoutClear or QuestTemplateType.BountyHunt =>
                "travel there and use \"! Quest\" to fight",
            QuestTemplateType.EscortCaravan => $"travel to {DisplayNodeName(g, inst.escortDestinationNodeId)}",
            QuestTemplateType.DeliveryFetch =>
                $"deliver at {DisplayNodeName(g, inst.deliveryDestinationNodeId)} by day {inst.deadlineDay}",
            QuestTemplateType.TroopLevy => $"bring {inst.levyCount}× {DisplayTroopName(g, inst.levyTroopId)} here",
            _ => "in progress"
        };

        static string DisplayNodeName(GameState g, string nodeId) =>
            g.Map.TryGetNode(nodeId, out var n) ? n.displayName : nodeId;

        static string DisplayTroopName(GameState g, string troopId) =>
            g.TroopRoster != null && g.TroopRoster.TryGet(troopId, out var def) ? def.displayName : troopId;

        void Row(ref float y, string label, System.Action onClick, UiFactory.ButtonStyle style = UiFactory.ButtonStyle.Primary)
        {
            var (top, bottom) = UiFactory.NextRow(ref y, 0.10f, 0.015f);
            var btn = UiFactory.Button(_list, "Row" + _list.childCount, label, new Vector2(0f, bottom), new Vector2(1f, top), onClick,
                onClick != null ? style : UiFactory.ButtonStyle.Secondary);
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
            if (g.Map.TryGetNode(g.Party.currentNodeId, out var node))
            {
                g.Market.EnsureMarket(node);
                var market = g.Market.Get(node.id);
                if (market != null) market.buyerGold += cost;
            }
            _body.text = $"Built for {cost}g.";
            Rebuild();
        }

        void TryRecruitTroop(GameState g, TroopDefinition def, int price)
        {
            if (!g.CanRecruit(1, out var capReason))
            {
                _body.text = capReason;
                return;
            }

            if (g.Party.gold < price)
            {
                _body.text = $"Need {price}g to hire {def.displayName}.";
                return;
            }

            g.Party.gold -= price;
            g.AddTroop(def.id, 1);
            if (g.Map.TryGetNode(g.Party.currentNodeId, out var node))
            {
                g.Market.EnsureMarket(node);
                var market = g.Market.Get(node.id);
                if (market != null) market.buyerGold += price;
            }
            _body.text = $"Hired 1× {def.displayName} for {price}g.";
            Rebuild();
        }

        /// <summary>Placeholder build costs — not balanced, tune in playtest.</summary>
        static int BuildCost(BuildingType type, int targetTier) => type switch
        {
            BuildingType.GovernorsGrotto => targetTier * targetTier * 100,
            BuildingType.ChurchOfTheBlackFluffyTail => 400,
            BuildingType.MooseCavalryYard => 380,
            BuildingType.CinderFoundry => 360,
            _ => targetTier * 80
        };
    }
}
