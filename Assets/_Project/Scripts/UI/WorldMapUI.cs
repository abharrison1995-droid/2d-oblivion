using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Scrollable/pannable strategic map: nodes, roads, settlement inspect, travel.
    /// Built at runtime for mobile-friendly dragging.
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
        GameObject _inspectPanel;
        string _selectedNodeId;
        readonly Dictionary<string, RectTransform> _nodeViews = new();
        readonly List<GameObject> _roadViews = new();

        public void Show()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
                RefreshHud();
                RebuildDynamicNodes();
                return;
            }

            BuildChrome();
            RebuildFullMap();
            RefreshHud();
            AppendLog("Drag the map to explore. Tap a settlement to inspect routes.");
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

            // Map viewport
            var viewport = UiFactory.Panel(root, "MapViewport", new Vector2(0f, 0.34f), new Vector2(1f, 1f), new Color(0.14f, 0.18f, 0.16f, 1f));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Unrestricted;
            scroll.inertia = true;
            scroll.decelerationRate = 0.1f;
            scroll.scrollSensitivity = 40f;

            _mapContent = new GameObject("MapContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _mapContent.SetParent(viewport, false);
            _mapContent.anchorMin = new Vector2(0.5f, 0.5f);
            _mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            _mapContent.pivot = new Vector2(0.5f, 0.5f);
            _mapContent.sizeDelta = new Vector2(2200, 1800);
            scroll.content = _mapContent;

            // soft ground plate
            var ground = UiFactory.Panel(_mapContent, "Ground", Vector2.zero, Vector2.one, new Color(0.18f, 0.24f, 0.2f, 1f));
            ground.offsetMin = Vector2.zero;
            ground.offsetMax = Vector2.zero;

            // Top HUD
            var hudPanel = UiFactory.Panel(root, "Hud", new Vector2(0f, 0.9f), new Vector2(1f, 1f), new Color(0.05f, 0.06f, 0.07f, 0.88f));
            _hud = UiFactory.Label(hudPanel, "HudText", "", 24, TextAnchor.MiddleLeft, new Color(0.9f, 0.88f, 0.8f));

            // Bottom inspect + log
            var bottom = UiFactory.Panel(root, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0.34f), new Color(0.07f, 0.08f, 0.1f, 0.96f));
            _inspectPanel = bottom.gameObject;
            _inspectTitle = UiFactory.Label(bottom, "InspectTitle", "Select a place", 30, TextAnchor.UpperLeft, new Color(0.93f, 0.86f, 0.7f));
            var titleRt = _inspectTitle.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.72f);
            titleRt.anchorMax = new Vector2(0.62f, 1f);

            _inspectBody = UiFactory.Label(bottom, "InspectBody", "Pan and zoom-feel via drag. Roads show travel links.", 22, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.78f));
            var bodyRt = _inspectBody.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0.28f);
            bodyRt.anchorMax = new Vector2(0.62f, 0.72f);

            UiFactory.Button(bottom, "TravelBtn", "Travel here", new Vector2(0.64f, 0.70f), new Vector2(0.98f, 0.92f), TravelToSelected);
            UiFactory.Button(bottom, "AdvisorBtn", "Ask advisor", new Vector2(0.64f, 0.48f), new Vector2(0.98f, 0.68f), AskAdvisor);
            UiFactory.Button(bottom, "BooksBtn", "Book store", new Vector2(0.64f, 0.26f), new Vector2(0.98f, 0.46f), OpenBooks);
            UiFactory.Button(bottom, "QuestBtn", "Quest action", new Vector2(0.64f, 0.04f), new Vector2(0.98f, 0.24f), QuestAction);

            _logText = UiFactory.Label(bottom, "Log", "", 18, TextAnchor.LowerLeft, new Color(0.65f, 0.7f, 0.68f));
            var logRt = _logText.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0.02f, 0.02f);
            logRt.anchorMax = new Vector2(0.62f, 0.28f);
        }

        void RebuildFullMap()
        {
            foreach (var road in _roadViews)
                if (road != null) Destroy(road);
            _roadViews.Clear();
            foreach (var kv in _nodeViews)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _nodeViews.Clear();

            var g = GameState.Instance;
            // Draw roads first (under nodes)
            foreach (var node in g.Map.Nodes.Values)
            {
                // We'll draw each undirected edge once via id without _rev
            }

            // Reconstruct unique roads by probing routes between known pairs is messy;
            // instead redraw from loaded adjacency using display names on nodes only,
            // and draw lines for every non-_rev edge we discover via GetRoute length-1 neighbours.
            var drawn = new HashSet<string>();
            foreach (var from in g.Map.Nodes.Keys)
            {
                foreach (var to in g.Map.Nodes.Keys)
                {
                    if (string.CompareOrdinal(from, to) >= 0) continue;
                    var route = g.Map.GetRoute(from, to);
                    if (route.Count != 1) continue;
                    var edge = route[0];
                    var key = edge.fromNodeId + "|" + edge.toNodeId;
                    var key2 = edge.toNodeId + "|" + edge.fromNodeId;
                    if (drawn.Contains(key) || drawn.Contains(key2)) continue;
                    drawn.Add(key);
                    DrawRoad(edge);
                }
            }

            foreach (var node in g.Map.Nodes.Values)
                CreateNodeView(node);

            CenterOn(g.Party.currentNodeId);
        }

        void RebuildDynamicNodes()
        {
            var g = GameState.Instance;
            foreach (var node in g.Map.Nodes.Values)
            {
                if (_nodeViews.ContainsKey(node.id)) continue;
                CreateNodeView(node);
                // link road to parent if temporary
                if (node.isTemporary && !string.IsNullOrEmpty(node.parentSettlementId))
                {
                    var edge = new RoadEdgeData
                    {
                        id = "temp_" + node.id,
                        fromNodeId = node.parentSettlementId,
                        toNodeId = node.id,
                        travelHours = 3f,
                        danger = 0.15f
                    };
                    DrawRoad(edge);
                }
            }
            RefreshHud();
        }

        void DrawRoad(RoadEdgeData edge)
        {
            if (!GameState.Instance.Map.TryGetNode(edge.fromNodeId, out var a)) return;
            if (!GameState.Instance.Map.TryGetNode(edge.toNodeId, out var b)) return;

            var go = new GameObject(edge.id, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapContent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            var p0 = MapToLocal(a.mapPosition);
            var p1 = MapToLocal(b.mapPosition);
            var mid = (p0 + p1) * 0.5f;
            var dir = p1 - p0;
            var dist = dir.magnitude;
            rt.sizeDelta = new Vector2(dist, edge.allowSevereRaids ? 6f : 3.5f);
            rt.anchoredPosition = mid;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            var img = go.GetComponent<Image>();
            img.color = edge.allowSevereRaids
                ? new Color(0.55f, 0.25f, 0.2f, 0.85f)
                : new Color(0.45f, 0.4f, 0.32f, 0.75f);
            _roadViews.Add(go);
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
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var captured = node.id;
            btn.onClick.AddListener(() => SelectNode(captured));

            var label = UiFactory.Label(go.transform, "Name", node.displayName, node.isSkeleton ? 16 : 18, TextAnchor.LowerCenter, Color.white);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(-0.4f, -0.55f);
            lrt.anchorMax = new Vector2(1.4f, 0.15f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            if (node.isSkeleton)
            {
                var tag = UiFactory.Label(go.transform, "Stub", "far realm", 14, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.55f, 0.9f));
                var trt = tag.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(-0.2f, 0.85f);
                trt.anchorMax = new Vector2(1.2f, 1.35f);
            }

            _nodeViews[node.id] = rt;
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

        void CenterOn(string nodeId)
        {
            if (!_nodeViews.TryGetValue(nodeId, out var rt)) return;
            _mapContent.anchoredPosition = -rt.anchoredPosition;
        }

        void SelectNode(string nodeId)
        {
            _selectedNodeId = nodeId;
            if (!GameState.Instance.Map.TryGetNode(nodeId, out var node)) return;

            _inspectTitle.text = node.displayName;
            var g = GameState.Instance;
            var here = g.Party.currentNodeId == nodeId;
            var route = g.Map.GetRoute(g.Party.currentNodeId, nodeId);
            var hours = 0f;
            foreach (var e in route) hours += e.travelHours;

            var faction = node.controllingFaction.ToString();
            var skeleton = node.isSkeleton
                ? "\nSkeleton realm — visible on the world map, deep quests later."
                : "";
            var services = "";
            if (node.hasStore) services += "Store ";
            if (node.hasTavern) services += "Tavern ";
            if (node.hasRecruitment) services += "Recruit ";
            if (node.hasBookStore) services += "BookStore ";

            _inspectBody.text =
                $"{node.type} · {faction}\n" +
                (here ? "You are here.\n" : $"Route: {route.Count} legs · ~{hours:0} hours\n") +
                $"Services: {(string.IsNullOrWhiteSpace(services) ? "None" : services)}" +
                skeleton;

            HighlightSelection(nodeId);
        }

        void HighlightSelection(string nodeId)
        {
            foreach (var kv in _nodeViews)
            {
                if (!GameState.Instance.Map.TryGetNode(kv.Key, out var n)) continue;
                var img = kv.Value.GetComponent<Image>();
                if (img == null) continue;
                img.color = kv.Key == nodeId
                    ? new Color(0.95f, 0.9f, 0.55f, 1f)
                    : NodeColor(n);
            }
        }

        void TravelToSelected()
        {
            if (string.IsNullOrEmpty(_selectedNodeId))
            {
                AppendLog("Select a destination first.");
                return;
            }

            var g = GameState.Instance;
            if (_selectedNodeId == g.Party.currentNodeId)
            {
                AppendLog("Already here.");
                return;
            }

            if (g.Map.TryGetNode(_selectedNodeId, out var dest) && dest.isSkeleton)
            {
                AppendLog($"{dest.displayName} is a far-realm stub. Reachable later when those kingdoms open.");
                // Still allow travel for exploration feel? User said skeleton - I'll allow travel so map feels real, but flag content empty.
            }

            var route = g.Map.GetRoute(g.Party.currentNodeId, _selectedNodeId);
            if (route.Count == 0)
            {
                AppendLog("No road route.");
                return;
            }

            foreach (var edge in route)
            {
                g.Travel.ApplyTravelTime(g.Party, edge);
                var encounter = g.Travel.RollEncounter(edge, g.Rng);
                if (encounter.kind != TravelEncounterKind.None)
                    AppendLog($"{encounter.title}: {encounter.body}");
            }

            g.Party.currentNodeId = _selectedNodeId;
            g.Economy.ConsumeFood(g.Party, route.Count * 0.35f, out var foodLog);
            AppendLog($"Arrived {_selectedNodeId}. {foodLog}");
            CenterOn(_selectedNodeId);
            SelectNode(_selectedNodeId);
            RefreshHud();

            // Auto investigation if on quest cities
            var hint = g.Act1Quest.OnArriveForInvestigation(_selectedNodeId, true);
            if (!string.IsNullOrEmpty(hint))
            {
                AppendLog(hint);
                RebuildDynamicNodes();
            }
        }

        void OpenBooks()
        {
            var g = GameState.Instance;
            if (!g.Map.TryGetNode(g.Party.currentNodeId, out var node) || !node.hasBookStore)
            {
                AppendLog("No Book Store here. Greyledger keeps the expensive treatises.");
                return;
            }

            var books = FindObjectOfType<BookStoreUI>();
            if (books == null)
            {
                var go = new GameObject("BookStoreUI");
                books = go.AddComponent<BookStoreUI>();
                DontDestroyOnLoad(go);
            }

            books.Open(() => RefreshHud());
        }

        void AskAdvisor()
        {
            var g = GameState.Instance;
            if (g.Party.currentNodeId != "greyledger")
            {
                AppendLog("The useful gossip is in Greyledger — travel there first.");
                return;
            }

            g.Act1Quest.SpeakToAdvisor();
            AppendLog("Advisor points you at Ashpond and Tollbar.");
        }

        void QuestAction()
        {
            var g = GameState.Instance;
            if (g.Party.currentNodeId == g.Act1Quest.LairNodeId && g.Act1Quest.LairVisible)
            {
                FindObjectOfType<BattleUI>()?.BeginLairBattle(outcome =>
                {
                    g.Act1Quest.TryCompleteLairRaid(outcome, g.Party);
                    foreach (var cardId in outcome.rewardPowerCardIds)
                        AppendLog($"Chief's treatise claimed: {cardId}");
                    AppendLog(outcome.summary);
                    RefreshHud();
                });
                return;
            }

            if (g.Act1Quest.Beat == StolenItemQuestBeat.ChiefCaptured)
            {
                g.Act1Quest.DeliverChiefToVoid(g.Party);
                if (g.TryOfferMercenaryContract(out var msg))
                    AppendLog(msg);
                else
                    AppendLog(msg);
                RefreshHud();
                return;
            }

            AppendLog("No special quest action here. Investigate quest towns or the lair.");
        }

        void RefreshHud()
        {
            if (_hud == null || GameState.Instance == null) return;
            var p = GameState.Instance.Party;
            var h = GameState.Instance.Hero;
            _hud.text =
                $"{h.name} · Day {p.day} · {p.gold}g · {p.TotalMen} men · At {p.currentNodeId} · Quest: {GameState.Instance.Act1Quest.Beat}";
        }

        void Update()
        {
            // Keep party marker feel: pulse current node scale slightly
            if (GameState.Instance == null) return;
            if (_nodeViews.TryGetValue(GameState.Instance.Party.currentNodeId, out var here))
            {
                var s = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
                here.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
