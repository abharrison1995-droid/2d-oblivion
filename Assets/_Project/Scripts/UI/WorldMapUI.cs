using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Scrollable map + inch-by-inch journey (route highlight, party marker, nearby bands).
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        const float MapScale = 90f;

        Canvas _canvas;
        RectTransform _mapContent;
        Text _hud;
        Text _inspectTitle;
        Text _inspectBody;
        Text _logText;
        Text _journeyHud;
        string _selectedNodeId;
        readonly Dictionary<string, RectTransform> _nodeViews = new();
        readonly List<GameObject> _roadViews = new();
        readonly List<GameObject> _routeHighlights = new();
        readonly Dictionary<string, RectTransform> _partyViews = new();
        RectTransform _playerMarker;
        GameObject _encounterPanel;
        Text _encounterTitle;
        Text _encounterBody;
        Button _fightBtn;
        Button _fleeBtn;
        Button _talkBtn;
        Button _booksBtn;
        TravelEncounter _pendingEncounter;

        public void Show()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
                RefreshHud();
                RebuildDynamicNodes();
                RefreshWorldParties();
                return;
            }

            BuildChrome();
            RebuildFullMap();
            EnsurePlayerMarker();
            RefreshWorldParties();
            RefreshHud();
            AppendLog("Drag map. Travel inch-by-inch. Party HUD for inventory. Settlements for recruit/sell.");
        }

        public void AppendLog(string line)
        {
            if (_logText == null) return;
            _logText.text = line + "\n" + _logText.text;
            if (_logText.text.Length > 2500)
                _logText.text = _logText.text.Substring(0, 2500);
        }

        void BuildChrome()
        {
            _canvas = UiFactory.CreateCanvas("WorldMapCanvas", 10);
            var root = _canvas.GetComponent<RectTransform>();

            var viewport = UiFactory.Panel(root, "MapViewport", new Vector2(0f, 0.36f), new Vector2(1f, 0.92f), new Color(0.14f, 0.18f, 0.16f, 1f));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Unrestricted;
            scroll.inertia = true;
            scroll.decelerationRate = 0.1f;

            _mapContent = new GameObject("MapContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _mapContent.SetParent(viewport, false);
            _mapContent.anchorMin = _mapContent.anchorMax = _mapContent.pivot = new Vector2(0.5f, 0.5f);
            _mapContent.sizeDelta = new Vector2(2400, 2000);
            scroll.content = _mapContent;
            UiFactory.Panel(_mapContent, "Ground", Vector2.zero, Vector2.one, new Color(0.18f, 0.24f, 0.2f, 1f));

            var hudPanel = UiFactory.Panel(root, "Hud", new Vector2(0f, 0.92f), new Vector2(1f, 1f), new Color(0.05f, 0.06f, 0.07f, 0.9f));
            _hud = UiFactory.Label(hudPanel, "HudText", "", 22, TextAnchor.MiddleLeft, new Color(0.9f, 0.88f, 0.8f));

            _journeyHud = UiFactory.Label(root, "JourneyHud", "", 20, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.7f));
            var jrt = _journeyHud.GetComponent<RectTransform>();
            jrt.anchorMin = new Vector2(0.05f, 0.86f);
            jrt.anchorMax = new Vector2(0.95f, 0.91f);

            var bottom = UiFactory.Panel(root, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0.36f), new Color(0.07f, 0.08f, 0.1f, 0.96f));
            _inspectTitle = UiFactory.Label(bottom, "InspectTitle", "Select a place", 28, TextAnchor.UpperLeft, new Color(0.93f, 0.86f, 0.7f));
            Stretch(_inspectTitle, 0f, 0.78f, 0.58f, 1f);
            _inspectBody = UiFactory.Label(bottom, "InspectBody", "", 20, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.78f));
            Stretch(_inspectBody, 0f, 0.32f, 0.58f, 0.78f);

            // Right column actions — larger touch targets
            UiFactory.Button(bottom, "TravelBtn", "Travel", new Vector2(0.6f, 0.80f), new Vector2(0.99f, 0.97f), TravelToSelected);
            UiFactory.Button(bottom, "AdvanceBtn", "Advance inch", new Vector2(0.6f, 0.62f), new Vector2(0.99f, 0.78f), AdvanceJourney);
            UiFactory.Button(bottom, "SettleBtn", "Market/Recruit", new Vector2(0.6f, 0.44f), new Vector2(0.99f, 0.60f), OpenSettlement);
            UiFactory.Button(bottom, "PartyBtn", "Party / Inv", new Vector2(0.6f, 0.26f), new Vector2(0.99f, 0.42f), OpenParty);
            UiFactory.Button(bottom, "QuestBtn", "Quest", new Vector2(0.6f, 0.14f), new Vector2(0.72f, 0.24f), QuestAction);
            UiFactory.Button(bottom, "AdvisorBtn", "Advisor", new Vector2(0.73f, 0.14f), new Vector2(0.85f, 0.24f), AskAdvisor);
            _booksBtn = UiFactory.Button(bottom, "BooksBtn", "Books", new Vector2(0.86f, 0.14f), new Vector2(0.99f, 0.24f), OpenBookStore);
            _booksBtn.gameObject.SetActive(false);
            UiFactory.Button(bottom, "SaveBtn", "Save", new Vector2(0.6f, 0.01f), new Vector2(0.79f, 0.12f), DoSave);
            UiFactory.Button(bottom, "LoadBtn", "Load", new Vector2(0.8f, 0.01f), new Vector2(0.99f, 0.12f), DoLoad);

            _logText = UiFactory.Label(bottom, "Log", "", 16, TextAnchor.LowerLeft, new Color(0.65f, 0.7f, 0.68f));
            Stretch(_logText, 0.02f, 0.02f, 0.58f, 0.3f);

            BuildEncounterPanel(root);
        }

        void BuildEncounterPanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "Encounter", new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.78f), new Color(0.12f, 0.1f, 0.08f, 0.98f));
            _encounterPanel = panel.gameObject;
            _encounterPanel.SetActive(false);
            _encounterTitle = UiFactory.Label(panel, "T", "", 28, TextAnchor.UpperCenter, new Color(0.95f, 0.85f, 0.6f));
            Stretch(_encounterTitle, 0.05f, 0.75f, 0.95f, 0.95f);
            _encounterBody = UiFactory.Label(panel, "B", "", 22, TextAnchor.UpperLeft, Color.white);
            Stretch(_encounterBody, 0.06f, 0.35f, 0.94f, 0.74f);
            _fightBtn = UiFactory.Button(panel, "Fight", "Fight", new Vector2(0.05f, 0.05f), new Vector2(0.32f, 0.28f), () => ResolveEncounter("fight"));
            _fleeBtn = UiFactory.Button(panel, "Flee", "Flee", new Vector2(0.36f, 0.05f), new Vector2(0.63f, 0.28f), () => ResolveEncounter("flee"));
            _talkBtn = UiFactory.Button(panel, "Talk", "Talk/Pay", new Vector2(0.67f, 0.05f), new Vector2(0.95f, 0.28f), () => ResolveEncounter("talk"));
        }

        static void Stretch(Text t, float x0, float y0, float x1, float y1)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void RebuildFullMap()
        {
            foreach (var road in _roadViews)
                if (road) Destroy(road);
            _roadViews.Clear();
            foreach (var kv in _nodeViews)
                if (kv.Value) Destroy(kv.Value.gameObject);
            _nodeViews.Clear();

            var g = GameState.Instance;
            var drawn = new HashSet<string>();
            foreach (var from in g.Map.Nodes.Keys)
            foreach (var to in g.Map.Nodes.Keys)
            {
                if (string.CompareOrdinal(from, to) >= 0) continue;
                var route = g.Map.GetRoute(from, to);
                if (route.Count != 1) continue;
                var edge = route[0];
                var key = edge.fromNodeId + "|" + edge.toNodeId;
                if (!drawn.Add(key)) continue;
                DrawRoad(edge, false);
            }

            foreach (var node in g.Map.Nodes.Values)
                CreateNodeView(node);
            CenterOnParty();
        }

        void RebuildDynamicNodes()
        {
            var g = GameState.Instance;
            foreach (var node in g.Map.Nodes.Values)
            {
                if (_nodeViews.ContainsKey(node.id)) continue;
                CreateNodeView(node);
                if (node.isTemporary && !string.IsNullOrEmpty(node.parentSettlementId))
                {
                    DrawRoad(new RoadEdgeData
                    {
                        id = "temp_" + node.id,
                        fromNodeId = node.parentSettlementId,
                        toNodeId = node.id,
                        travelHours = 3f,
                        danger = 0.15f,
                        terrain = TerrainType.Forest
                    }, false);
                }
            }
        }

        void DrawRoad(RoadEdgeData edge, bool highlight)
        {
            if (!GameState.Instance.Map.TryGetNode(edge.fromNodeId, out var a)) return;
            if (!GameState.Instance.Map.TryGetNode(edge.toNodeId, out var b)) return;
            var go = new GameObject(edge.id + (highlight ? "_hl" : ""), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapContent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            var p0 = MapToLocal(a.mapPosition);
            var p1 = MapToLocal(b.mapPosition);
            var dir = p1 - p0;
            rt.sizeDelta = new Vector2(dir.magnitude, highlight ? 8f : (edge.allowSevereRaids ? 6f : 3.5f));
            rt.anchoredPosition = (p0 + p1) * 0.5f;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            go.GetComponent<Image>().color = highlight
                ? new Color(0.95f, 0.8f, 0.25f, 0.95f)
                : edge.allowSevereRaids
                    ? new Color(0.55f, 0.25f, 0.2f, 0.85f)
                    : new Color(0.45f, 0.4f, 0.32f, 0.75f);
            if (highlight) _routeHighlights.Add(go);
            else _roadViews.Add(go);
        }

        void CreateNodeView(MapNodeData node)
        {
            var size = node.type switch
            {
                NodeType.Capital => 78f,
                NodeType.Town => 64f,
                NodeType.Castle => 60f,
                NodeType.QuestLair => 56f,
                _ => 48f
            };
            var go = new GameObject(node.id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_mapContent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = MapToLocal(node.mapPosition);
            var img = go.GetComponent<Image>();
            img.color = NodeColor(node);
            var id = node.id;
            go.GetComponent<Button>().onClick.AddListener(() => SelectNode(id));
            var label = UiFactory.Label(go.transform, "Name", node.displayName, 17, TextAnchor.LowerCenter, Color.white);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(-0.4f, -0.55f);
            lrt.anchorMax = new Vector2(1.4f, 0.15f);
            if (node.isSkeleton)
            {
                var tag = UiFactory.Label(go.transform, "Stub", "far realm", 14, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.55f));
                var trt = tag.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(-0.2f, 0.85f);
                trt.anchorMax = new Vector2(1.2f, 1.35f);
            }

            _nodeViews[node.id] = rt;
        }

        void EnsurePlayerMarker()
        {
            if (_playerMarker != null) return;
            var go = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapContent, false);
            _playerMarker = go.GetComponent<RectTransform>();
            _playerMarker.sizeDelta = new Vector2(36, 36);
            go.GetComponent<Image>().color = new Color(0.2f, 0.85f, 1f, 1f);
            UpdatePlayerMarker();
        }

        void UpdatePlayerMarker()
        {
            if (_playerMarker == null) return;
            _playerMarker.anchoredPosition = MapToLocal(GameState.Instance.PlayerMapPosition());
            _playerMarker.SetAsLastSibling();
        }

        void RefreshWorldParties()
        {
            var g = GameState.Instance;
            var visible = g.WorldParties.VisibleNear(g.PlayerMapPosition(), g.Journey.IsActive ? 3.2f : 2.2f);
            var want = new HashSet<string>();
            foreach (var p in visible) want.Add(p.id);

            foreach (var kv in new List<string>(_partyViews.Keys))
            {
                if (want.Contains(kv)) continue;
                if (_partyViews[kv]) Destroy(_partyViews[kv].gameObject);
                _partyViews.Remove(kv);
            }

            foreach (var p in visible)
            {
                if (!_partyViews.TryGetValue(p.id, out var rt) || rt == null)
                {
                    var go = new GameObject(p.id, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(_mapContent, false);
                    rt = go.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(28, 28);
                    var img = go.GetComponent<Image>();
                    img.color = p.faction == FactionId.ButterKlanBoys
                        ? new Color(0.85f, 0.55f, 0.15f)
                        : new Color(0.7f, 0.3f, 0.35f);
                    var lbl = UiFactory.Label(go.transform, "n", p.displayName, 12, TextAnchor.UpperCenter, Color.white);
                    var lrt = lbl.GetComponent<RectTransform>();
                    lrt.anchorMin = new Vector2(-1f, 0.9f);
                    lrt.anchorMax = new Vector2(2f, 1.6f);
                    _partyViews[p.id] = rt;
                }

                _partyViews[p.id].anchoredPosition = MapToLocal(p.mapPos);
            }
        }

        static Color NodeColor(MapNodeData node)
        {
            if (node.type == NodeType.QuestLair) return new Color(0.55f, 0.35f, 0.15f, 1f);
            if (node.isSkeleton) return new Color(0.25f, 0.28f, 0.4f, 0.9f);
            return node.type switch
            {
                NodeType.Capital => new Color(0.72f, 0.62f, 0.28f, 1f),
                NodeType.Town => new Color(0.35f, 0.48f, 0.55f, 1f),
                NodeType.Castle => new Color(0.4f, 0.36f, 0.42f, 1f),
                _ => new Color(0.32f, 0.42f, 0.3f, 1f)
            };
        }

        static Vector2 MapToLocal(Vector2 mapPos) => mapPos * MapScale;

        void CenterOnParty()
        {
            _mapContent.anchoredPosition = -MapToLocal(GameState.Instance.PlayerMapPosition());
        }

        void SelectNode(string nodeId)
        {
            _selectedNodeId = nodeId;
            if (!GameState.Instance.Map.TryGetNode(nodeId, out var node)) return;
            var g = GameState.Instance;
            var here = g.Party.currentNodeId == nodeId && !g.Journey.IsActive;
            var route = g.Map.GetRoute(g.Party.currentNodeId, nodeId);
            var hours = 0f;
            foreach (var e in route) hours += e.travelHours;
            var services = "";
            if (node.hasStore || node.type == NodeType.Village) services += "Market ";
            if (node.hasRecruitment || node.type == NodeType.Village) services += "Recruit ";
            if (node.hasBookStore) services += "Books ";
            _inspectTitle.text = node.displayName;
            _inspectBody.text =
                $"{node.type} · {node.controllingFaction}\n" +
                (here ? "You are here.\n" : $"Road: {route.Count} legs · road-time ~{hours:0}h (actual time depends on mounts/scouting/terrain)\n") +
                $"Services: {(services == "" ? "None" : services)}" +
                (node.isSkeleton ? "\nFar-realm stub." : "");
            foreach (var kv in _nodeViews)
            {
                if (!g.Map.TryGetNode(kv.Key, out var n)) continue;
                kv.Value.GetComponent<Image>().color = kv.Key == nodeId ? new Color(0.95f, 0.9f, 0.55f) : NodeColor(n);
            }
        }

        void ClearRouteHighlights()
        {
            foreach (var go in _routeHighlights)
                if (go) Destroy(go);
            _routeHighlights.Clear();
        }

        void HighlightActiveRoute(List<RoadEdgeData> route)
        {
            ClearRouteHighlights();
            foreach (var edge in route)
                DrawRoad(edge, true);
        }

        void TravelToSelected()
        {
            if (GameState.Instance.Journey.IsActive)
            {
                AppendLog("Already travelling — tap Advance inch.");
                return;
            }

            if (string.IsNullOrEmpty(_selectedNodeId))
            {
                AppendLog("Select a destination.");
                return;
            }

            var g = GameState.Instance;
            if (_selectedNodeId == g.Party.currentNodeId)
            {
                AppendLog("Already here.");
                return;
            }

            var route = g.Map.GetRoute(g.Party.currentNodeId, _selectedNodeId);
            if (route.Count == 0)
            {
                AppendLog("No road route.");
                return;
            }

            if (!g.Journey.Begin(g.Map, g.Party.currentNodeId, _selectedNodeId, out var err))
            {
                AppendLog(err);
                return;
            }

            HighlightActiveRoute(route);
            EnsurePlayerMarker();
            UpdatePlayerMarker();
            _journeyHud.text = $"Journey → {_selectedNodeId} · {g.Journey.Steps.Count} inches · Advance to move";
            AppendLog($"Route set ({g.Journey.Steps.Count} inches). Mounts & scouting change inch time and event chance.");
            RefreshWorldParties();
        }

        void AdvanceJourney()
        {
            var g = GameState.Instance;
            if (!g.Journey.IsActive)
            {
                AppendLog("No active journey — select a town and tap Travel.");
                return;
            }

            if (_encounterPanel.activeSelf) return;

            g.Journey.TryAdvance(g.Party, g.Hero, g.Travel, g.Economy, g.TroopRoster, g.Rng,
                out var encounter, out var log, out var finished);

            // hours advanced ~use last step estimate for world party tick
            g.WorldParties.TickTowardTargets(g.Map, 1.2f);
            UpdatePlayerMarker();
            CenterOnParty();
            RefreshWorldParties();
            AppendLog(log);
            RefreshHud();

            if (encounter.kind != TravelEncounterKind.None)
            {
                ShowEncounterPanel(encounter);
                return;
            }

            if (finished) OnJourneyFinished();
            else
                _journeyHud.text =
                    $"Inch {g.Journey.StepIndex}/{g.Journey.Steps.Count} → {g.Journey.DestinationId}";
        }

        void ShowEncounterPanel(TravelEncounter encounter)
        {
            _pendingEncounter = encounter;
            _encounterTitle.text = encounter.title;
            _encounterBody.text = encounter.body + "\n\nNearby bands may also be watching.";
            _fightBtn.gameObject.SetActive(encounter.canFight);
            _fleeBtn.gameObject.SetActive(encounter.canFlee);
            var showTalk = encounter.canTalk || encounter.canPay;
            _talkBtn.gameObject.SetActive(showTalk);
            if (showTalk)
            {
                var cost = EncounterCost(encounter.kind);
                var label = _talkBtn.GetComponentInChildren<Text>();
                label.text = cost > 0 ? $"Pay {cost}g" : "Talk";
            }

            _encounterPanel.SetActive(true);
        }

        static int EncounterCost(TravelEncounterKind kind) => kind switch
        {
            TravelEncounterKind.Trader => 10,
            TravelEncounterKind.Healers => 10,
            TravelEncounterKind.Refugees => 6,
            TravelEncounterKind.MinorThieves => 10,
            TravelEncounterKind.BanditAmbush => 25,
            TravelEncounterKind.ButterRaid => 60,
            _ => 0
        };

        void ResolveEncounter(string choice)
        {
            _encounterPanel.SetActive(false);
            var e = _pendingEncounter;
            if (e == null) return;

            if (choice == "fight" && e.canFight)
            {
                _pendingEncounter = null;
                var battleUi = FindObjectOfType<BattleUI>() ?? new GameObject("BattleUI").AddComponent<BattleUI>();
                battleUi.BeginEncounterBattle(e.kind, outcome => OnEncounterBattleResolved(outcome, e));
                return;
            }

            switch (choice)
            {
                case "flee" when e.canFlee:
                    AppendLog($"You slip away from: {e.title}");
                    break;
                case "talk" when e.canTalk || e.canPay:
                    ResolveTalkOrPay(e);
                    break;
                default:
                    AppendLog($"Nothing to be done here: {e.title}");
                    break;
            }

            _pendingEncounter = null;
            RefreshHud();
            AdvanceOrFinishAfterEncounter();
        }

        void ResolveTalkOrPay(TravelEncounter e)
        {
            var g = GameState.Instance;
            var cost = EncounterCost(e.kind);
            if (cost > 0)
            {
                var paid = Mathf.Min(cost, g.Party.gold);
                g.Party.gold -= paid;
                AppendLog(paid >= cost
                    ? $"You pay {paid}g: {e.title}"
                    : $"You scrape together {paid}g (short of the {cost}g asked): {e.title}");
            }
            else
            {
                AppendLog($"You talk your way through: {e.title}");
            }

            if (e.kind == TravelEncounterKind.Trader)
            {
                g.Party.food.Add(new InventoryStack { itemId = "bread", count = 2 });
                AppendLog("Bought 2 bread from the trader.");
            }
        }

        void OnEncounterBattleResolved(BattleOutcome outcome, TravelEncounter e)
        {
            AppendLog(outcome.playerVictory
                ? $"You best the {e.title}. {outcome.summary}"
                : $"You're driven off by the {e.title}. {outcome.summary}");
            RefreshHud();
            AdvanceOrFinishAfterEncounter();
        }

        void AdvanceOrFinishAfterEncounter()
        {
            if (!GameState.Instance.Journey.IsActive)
                OnJourneyFinished();
            else
                _journeyHud.text =
                    $"Inch {GameState.Instance.Journey.StepIndex}/{GameState.Instance.Journey.Steps.Count}";
        }

        void OnJourneyFinished()
        {
            ClearRouteHighlights();
            _journeyHud.text = "";
            var g = GameState.Instance;
            UpdatePlayerMarker();
            SelectNode(g.Party.currentNodeId);
            var hint = g.Act1Quest.OnArriveForInvestigation(g.Party.currentNodeId, true);
            if (!string.IsNullOrEmpty(hint))
            {
                AppendLog(hint);
                RebuildDynamicNodes();
            }

            AppendLog($"Arrived {g.Party.currentNodeId}.");
            RefreshHud();
            RefreshWorldParties();
        }

        void OpenSettlement()
        {
            var g = GameState.Instance;
            if (g.Journey.IsActive)
            {
                AppendLog("Finish or wait — you're on the road.");
                return;
            }

            if (!g.Map.TryGetNode(g.Party.currentNodeId, out var node)) return;
            if (!(node.hasStore || node.hasRecruitment || node.type == NodeType.Village || node.type == NodeType.Town || node.type == NodeType.Capital))
            {
                AppendLog("Nothing to trade or recruit here.");
                return;
            }

            var ui = FindObjectOfType<SettlementUI>() ?? new GameObject("SettlementUI").AddComponent<SettlementUI>();
            ui.Open(RefreshHud);
        }

        void OpenParty()
        {
            var ui = FindObjectOfType<PartyPanelUI>() ?? new GameObject("PartyPanelUI").AddComponent<PartyPanelUI>();
            ui.Open(RefreshHud);
        }

        void OpenBookStore()
        {
            var g = GameState.Instance;
            if (g.Journey.IsActive)
            {
                AppendLog("Finish or wait — you're on the road.");
                return;
            }

            if (!g.Map.TryGetNode(g.Party.currentNodeId, out var node) || !node.hasBookStore)
            {
                AppendLog("No book store here.");
                return;
            }

            var ui = FindObjectOfType<BookStoreUI>() ?? new GameObject("BookStoreUI").AddComponent<BookStoreUI>();
            ui.Open(RefreshHud);
        }

        void AskAdvisor()
        {
            var g = GameState.Instance;
            if (g.Journey.IsActive)
            {
                AppendLog("On the road.");
                return;
            }

            if (g.Party.currentNodeId != "greyledger")
            {
                AppendLog("Advisor is in Greyledger.");
                return;
            }

            if (g.Map.TryGetNode(g.Party.currentNodeId, out var node) && node.hasBookStore &&
                g.Act1Quest.Beat is StolenItemQuestBeat.InvestigateCities or StolenItemQuestBeat.ExButterIntel
                    or StolenItemQuestBeat.LairSpawned or StolenItemQuestBeat.Completed)
            {
                // Secondary: books when advisor already done
            }

            g.Act1Quest.SpeakToAdvisor();
            AppendLog("Advisor: try Ashpond or Tollbar.");
        }

        void DoSave()
        {
            SaveLoadService.Save(GameState.Instance);
            AppendLog($"Saved → {SaveLoadService.Path}");
        }

        void DoLoad()
        {
            if (!SaveLoadService.SaveExists())
            {
                AppendLog("No save file yet.");
                return;
            }

            if (GameState.Instance.Journey.IsActive)
                GameState.Instance.Journey.Cancel();

            if (!SaveLoadService.TryLoad(GameState.Instance))
            {
                AppendLog("Load failed — see Console.");
                return;
            }

            ClearRouteHighlights();
            RebuildDynamicNodes();
            UpdatePlayerMarker();
            CenterOnParty();
            SelectNode(GameState.Instance.Party.currentNodeId);
            RefreshWorldParties();
            RefreshHud();
            AppendLog($"Loaded {GameState.Instance.Hero.name}. Quest: {GameState.Instance.Act1Quest.Beat}");
        }

        void QuestAction()
        {
            var g = GameState.Instance;
            if (g.Journey.IsActive)
            {
                AppendLog("On the road — advance inches first.");
                return;
            }

            if (g.Party.currentNodeId == "greyledger" &&
                g.Act1Quest.Beat is StolenItemQuestBeat.QuestGiven or StolenItemQuestBeat.SeekAdvice)
            {
                g.Act1Quest.SpeakToAdvisor();
                AppendLog("Advisor names Ashpond and Tollbar.");
                return;
            }

            if (g.Party.currentNodeId == g.Act1Quest.LairNodeId && g.Act1Quest.LairVisible)
            {
                FindObjectOfType<BattleUI>()?.BeginLairBattle(outcome =>
                {
                    g.Act1Quest.TryCompleteLairRaid(outcome, g.Party);
                    foreach (var cardId in outcome.rewardPowerCardIds)
                        AppendLog($"Chief's treatise: {cardId}");
                    AppendLog(outcome.summary);
                    RefreshHud();
                });
                return;
            }

            if (g.Act1Quest.Beat == StolenItemQuestBeat.ChiefCaptured)
            {
                // Prefer Voidovia hub for audience
                if (g.Party.currentNodeId is not ("greyledger" or "bastion_holt" or "red_knoll"))
                {
                    AppendLog("Bring the Chief to Greyledger, Bastion Holt, or Red Knoll for audience.");
                    return;
                }

                var audience = FindObjectOfType<VoidAudienceUI>() ?? new GameObject("VoidAudienceUI").AddComponent<VoidAudienceUI>();
                audience.Open(() =>
                {
                    AppendLog(g.Party.isVoidoviaMercenary
                        ? "You ride for Lord Void's purse."
                        : "You remain free company — for now.");
                    RefreshHud();
                });
                return;
            }

            AppendLog("No quest beat here. Advisor in Greyledger, lair raid, or Void audience after capture.");
        }

        void RefreshHud()
        {
            if (_hud == null || GameState.Instance == null) return;
            var p = GameState.Instance.Party;
            var wages = GameState.Instance.Economy.WeeklyWageBill(p);
            var need = GameState.Instance.Economy.DailyFoodNeed(p);
            var fill = 0f;
            foreach (var f in p.food) fill += f.count;
            var days = need > 0.01f ? fill / need : 99f;
            _hud.text =
                $"{GameState.Instance.Hero.name} · Day {p.day} · {p.gold}g · Food~{days:0.0}d · Wages {wages}/wk · {p.TotalMen} men · {p.currentNodeId}";
            UpdatePlayerMarker();
            if (_booksBtn != null)
                _booksBtn.gameObject.SetActive(GameState.Instance.Map.TryGetNode(p.currentNodeId, out var here) && here.hasBookStore);
        }

        void Update()
        {
            if (GameState.Instance == null || _playerMarker == null) return;
            var s = 1f + Mathf.Sin(Time.time * 3f) * 0.06f;
            _playerMarker.localScale = new Vector3(s, s, 1f);
        }
    }
}
