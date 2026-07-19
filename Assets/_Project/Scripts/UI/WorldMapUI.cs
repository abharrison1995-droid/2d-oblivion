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
        const float MapScale = 135f;
        const float MinZoom = 0.25f; // widened for the 3x3-sized ground area — lets you see the whole map at once
        const float MaxZoom = 1.8f;
        float _mapZoom = 1f;

        const float BottomExpandedHeight = 0.3f;
        const float BottomCollapsedHeight = 0.05f;

        Canvas _canvas;
        RectTransform _mapContent;
        RectTransform _viewport;
        RectTransform _bottomPanel;
        RectTransform _bottomContent;
        Button _menuToggleBtn;
        bool _menuCollapsed;
        Text _hud;
        Text _inspectTitle;
        Text _inspectBody;
        Text _logText;
        ScrollRect _logScroll;
        Text _journeyHud;
        string _selectedNodeId;
        readonly Dictionary<string, RectTransform> _nodeViews = new();
        readonly List<GameObject> _roadViews = new();
        readonly List<GameObject> _routeHighlights = new();
        readonly Dictionary<string, RectTransform> _partyViews = new();
        RectTransform _playerMarker;
        GameObject _encounterPanel;
        Image _encounterPanelImage;
        RectTransform _encounterIconSlot;
        Text _encounterTitle;
        Text _encounterBody;
        Button _fightBtn;
        Button _fleeBtn;
        Button _talkBtn;
        Button _surrenderBtn;
        Button _booksBtn;
        Button _troopsBtn;
        Button _actionBtn;
        TravelEncounter _pendingEncounter;

        // Double-click / double-tap a node to open a travel confirmation — works even with the bottom
        // menu (and its Travel button) collapsed.
        const float DoubleClickSeconds = 0.4f;
        string _lastClickNodeId;
        float _lastClickTime = -1f;
        GameObject _travelConfirmPanel;
        Text _travelConfirmTitle;
        Text _travelConfirmBody;
        Button _travelConfirmGo;
        string _travelConfirmTarget;

        const float AutoTravelIntervalSeconds = 1f;
        float _autoTickTimer;
        bool _travelLocked;

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
            AppendLog("Drag map to pan. Tap a place to inspect it; double-tap it to travel (works with the menu hidden). Party HUD for inventory.");
        }

        public void AppendLog(string line)
        {
            if (_logText == null) return;
            _logText.text = string.IsNullOrEmpty(_logText.text) ? line : _logText.text + "\n" + line;
            if (_logText.text.Length > 2500)
                _logText.text = _logText.text.Substring(_logText.text.Length - 2500);

            // Force the just-appended line into view instead of leaving the newest entry
            // hidden until the player scrolls down to find it.
            if (_logScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                _logScroll.verticalNormalizedPosition = 0f;
            }
        }

        void BuildChrome()
        {
            _canvas = UiFactory.CreateCanvas("WorldMapCanvas", 10);
            var root = _canvas.GetComponent<RectTransform>();

            var viewport = UiFactory.Panel(root, "MapViewport", new Vector2(0f, BottomExpandedHeight), new Vector2(1f, 0.92f), new Color(0.14f, 0.18f, 0.16f, 1f));
            _viewport = viewport;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Unrestricted;
            scroll.inertia = true;
            scroll.decelerationRate = 0.1f;

            _mapContent = new GameObject("MapContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _mapContent.SetParent(viewport, false);
            _mapContent.anchorMin = _mapContent.anchorMax = _mapContent.pivot = new Vector2(0.5f, 0.5f);
            // 3x3 of the original 3200x2600 area — current content stays centered; the extra
            // space is empty but textured, ready for new regions later.
            _mapContent.sizeDelta = new Vector2(9600, 7800);
            scroll.content = _mapContent;
            var ground = UiFactory.Panel(_mapContent, "Ground", Vector2.zero, Vector2.one, Color.white);
            ProceduralGround.Apply(ground.GetComponent<Image>());

            UiFactory.Button(viewport, "ZoomInBtn", "+", new Vector2(0.91f, 0.86f), new Vector2(0.99f, 0.97f), ZoomIn);
            UiFactory.Button(viewport, "ZoomOutBtn", "-", new Vector2(0.91f, 0.74f), new Vector2(0.99f, 0.85f), ZoomOut);

            var hudPanel = UiFactory.Panel(root, "Hud", new Vector2(0f, 0.92f), new Vector2(1f, 1f), new Color(0.05f, 0.06f, 0.07f, 0.9f));
            _hud = UiFactory.Label(hudPanel, "HudText", "", 22, TextAnchor.MiddleLeft, UiFactory.Theme.TextDim);

            _journeyHud = UiFactory.Label(root, "JourneyHud", "", 20, TextAnchor.MiddleCenter, UiFactory.Theme.TextDim);
            var jrt = _journeyHud.GetComponent<RectTransform>();
            jrt.anchorMin = new Vector2(0.05f, 0.86f);
            jrt.anchorMax = new Vector2(0.95f, 0.91f);

            var bottom = UiFactory.Panel(root, "Bottom", new Vector2(0f, 0f), new Vector2(1f, BottomExpandedHeight), new Color(0.07f, 0.08f, 0.1f, 0.96f));
            _bottomPanel = bottom;

            var content = UiFactory.Panel(bottom, "BottomContent", Vector2.zero, new Vector2(1f, 0.87f), new Color(0, 0, 0, 0));
            _bottomContent = content;

            _inspectTitle = UiFactory.Label(content, "InspectTitle", "Select a place", 26, TextAnchor.UpperLeft, UiFactory.Theme.TextTitle);
            Stretch(_inspectTitle, 0f, 0.86f, 0.58f, 1f);
            _inspectBody = UiFactory.Label(content, "InspectBody", "", 19, TextAnchor.UpperLeft, UiFactory.Theme.TextDim);
            Stretch(_inspectBody, 0f, 0.64f, 0.58f, 0.86f);

            // Right column — compact 2×5 grid, half-size buttons with a leading symbol.
            const float colAx0 = 0.6f, colAx1 = 0.785f, colBx0 = 0.805f, colBx1 = 0.99f;
            const float rowH = 0.176f, rowGap = 0.018f, rowTop = 0.97f;
            float RowTop(int row) => rowTop - row * (rowH + rowGap);
            float RowBottom(int row) => RowTop(row) - rowH;

            UiFactory.Button(content, "TravelBtn", "→ Travel", new Vector2(colAx0, RowBottom(0)), new Vector2(colAx1, RowTop(0)), TravelToSelected);
            _troopsBtn = UiFactory.Button(content, "TroopsBtn", "◆ Troops", new Vector2(colBx0, RowBottom(0)), new Vector2(colBx1, RowTop(0)), OpenTroops);
            _actionBtn = UiFactory.Button(content, "ActionBtn", "■ Enter", new Vector2(colAx0, RowBottom(1)), new Vector2(colAx1, RowTop(1)), OpenSettlementMenu);
            UiFactory.Button(content, "PartyBtn", "◆ Party", new Vector2(colBx0, RowBottom(1)), new Vector2(colBx1, RowTop(1)), OpenParty);
            UiFactory.Button(content, "QuestBtn", "! Quest", new Vector2(colAx0, RowBottom(2)), new Vector2(colAx1, RowTop(2)), QuestAction);
            UiFactory.Button(content, "AdvisorBtn", "? Advisor", new Vector2(colBx0, RowBottom(2)), new Vector2(colBx1, RowTop(2)), AskAdvisor);
            _booksBtn = UiFactory.Button(content, "BooksBtn", "§ Books", new Vector2(colAx0, RowBottom(3)), new Vector2(colAx1, RowTop(3)), OpenBookStore);
            _booksBtn.gameObject.SetActive(false);
            UiFactory.Button(content, "SaveBtn", "● Save", new Vector2(colBx0, RowBottom(3)), new Vector2(colBx1, RowTop(3)), DoSave);
            UiFactory.Button(content, "LoadBtn", "○ Load", new Vector2(colAx0, RowBottom(4)), new Vector2(colAx1, RowTop(4)), DoLoad);

            var logPanel = UiFactory.Panel(content, "LogPanel", new Vector2(0.02f, 0.02f), new Vector2(0.58f, 0.6f), new Color(0.05f, 0.06f, 0.08f, 0.5f));
            logPanel.gameObject.AddComponent<RectMask2D>();
            _logScroll = logPanel.gameObject.AddComponent<ScrollRect>();
            _logScroll.horizontal = false;
            _logScroll.vertical = true;
            _logScroll.movementType = ScrollRect.MovementType.Elastic;
            _logScroll.scrollSensitivity = 20f;

            // Top-anchored, grows downward as lines are appended — newest line ends up at the
            // bottom, which AppendLog scrolls into view each time so it's never hidden above.
            _logText = UiFactory.Label(logPanel, "Log", "", 21, TextAnchor.UpperLeft, UiFactory.Theme.TextDim);
            var logRt = _logText.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0f, 1f);
            logRt.anchorMax = new Vector2(1f, 1f);
            logRt.pivot = new Vector2(0f, 1f);
            logRt.offsetMin = new Vector2(8, -30);
            logRt.offsetMax = new Vector2(-8, -8);

            var csf = _logText.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _logScroll.content = logRt;

            // Fixed-size tab pinned to the screen's bottom-right corner — same spot, same
            // size, whether the menu is expanded or collapsed, so it always works as both
            // the hide AND unhide control.
            _menuToggleBtn = UiFactory.Button(root, "MenuToggle", "▼ Hide", new Vector2(0.85f, 0f), new Vector2(1f, 0.05f), ToggleBottomMenu);
            var toggleRt = _menuToggleBtn.GetComponent<RectTransform>();
            toggleRt.anchorMin = toggleRt.anchorMax = new Vector2(1f, 0f);
            toggleRt.pivot = new Vector2(1f, 0f);
            toggleRt.sizeDelta = new Vector2(170f, 60f);
            toggleRt.anchoredPosition = new Vector2(-10f, 10f);

            BuildEncounterPanel(root);
        }

        void ToggleBottomMenu()
        {
            _menuCollapsed = !_menuCollapsed;
            _bottomContent.gameObject.SetActive(!_menuCollapsed);
            var height = _menuCollapsed ? BottomCollapsedHeight : BottomExpandedHeight;
            _bottomPanel.anchorMax = new Vector2(1f, height);
            _viewport.anchorMin = new Vector2(0f, height);
            var label = _menuToggleBtn.GetComponentInChildren<Text>();
            label.text = _menuCollapsed ? "▲ Menu" : "▼ Hide";
        }

        void BuildEncounterPanel(Transform root)
        {
            var panel = UiFactory.Panel(root, "Encounter", new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.78f), new Color(0.12f, 0.1f, 0.08f, 0.98f));
            _encounterPanel = panel.gameObject;
            _encounterPanelImage = panel.GetComponent<Image>();
            _encounterPanel.SetActive(false);
            _encounterIconSlot = UiFactory.Panel(panel, "IconSlot", new Vector2(0.71f, 0.74f), new Vector2(0.95f, 0.97f), new Color(0, 0, 0, 0.25f));
            _encounterTitle = UiFactory.Label(panel, "T", "", 26, TextAnchor.UpperLeft, UiFactory.Theme.TextBody);
            Stretch(_encounterTitle, 0.05f, 0.75f, 0.68f, 0.97f);
            _encounterBody = UiFactory.Label(panel, "B", "", 22, TextAnchor.UpperLeft, Color.white);
            Stretch(_encounterBody, 0.06f, 0.35f, 0.94f, 0.74f);
            _fightBtn = UiFactory.Button(panel, "Fight", "Fight", new Vector2(0.05f, 0.05f), new Vector2(0.32f, 0.28f), () => ResolveEncounter("fight"));
            _fleeBtn = UiFactory.Button(panel, "Flee", "Flee", new Vector2(0.36f, 0.05f), new Vector2(0.63f, 0.28f), () => ResolveEncounter("flee"));
            _talkBtn = UiFactory.Button(panel, "Talk", "Talk/Pay", new Vector2(0.67f, 0.05f), new Vector2(0.95f, 0.28f), () => ResolveEncounter("talk"));
            _surrenderBtn = UiFactory.Button(panel, "Surrender", "Demand Surrender", new Vector2(0.05f, 0.295f), new Vector2(0.95f, 0.345f), () => ResolveEncounter("surrender"));
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

            foreach (var edge in g.Map.OffPathEdges)
                DrawOffPathTrail(edge, false);

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
            // Index 0 is the opaque "Ground" panel — sit just above it, always below nodes added later.
            go.transform.SetSiblingIndex(1);
            var rt = go.GetComponent<RectTransform>();
            var p0 = MapToLocal(a.mapPosition);
            var p1 = MapToLocal(b.mapPosition);
            var dir = p1 - p0;
            rt.sizeDelta = new Vector2(dir.magnitude, highlight ? 9f : (edge.allowSevereRaids ? 7f : 5.5f));
            rt.anchoredPosition = (p0 + p1) * 0.5f;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            go.GetComponent<Image>().color = highlight
                ? new Color(0.98f, 0.82f, 0.3f, 1f)
                : edge.allowSevereRaids
                    ? new Color(0.72f, 0.3f, 0.22f, 0.95f)
                    : new Color(0.68f, 0.56f, 0.36f, 0.95f);
            if (highlight) _routeHighlights.Add(go);
            else _roadViews.Add(go);
        }

        /// <summary>
        /// Off-path connections draw as a dashed, faded trail instead of a solid road bar —
        /// signals "no maintained road here" at a glance, whether shown persistently or as
        /// the active route while bushwhacking.
        /// </summary>
        void DrawOffPathTrail(RoadEdgeData edge, bool highlight)
        {
            if (!GameState.Instance.Map.TryGetNode(edge.fromNodeId, out var a)) return;
            if (!GameState.Instance.Map.TryGetNode(edge.toNodeId, out var b)) return;

            var p0 = MapToLocal(a.mapPosition);
            var p1 = MapToLocal(b.mapPosition);
            var full = p1 - p0;
            var length = full.magnitude;
            if (length < 1f) return;
            var dir = full / length;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            const float dashLength = 26f;
            const float gapLength = 18f;
            var step = dashLength + gapLength;
            var count = Mathf.Max(1, Mathf.FloorToInt(length / step));

            for (var i = 0; i < count; i++)
            {
                var start = i * step;
                var center = p0 + dir * (start + dashLength * 0.5f);
                var go = new GameObject(edge.id + "_dash" + i + (highlight ? "_hl" : ""), typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_mapContent, false);
                go.transform.SetSiblingIndex(1);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(dashLength, highlight ? 6f : 4f);
                rt.anchoredPosition = center;
                rt.localRotation = Quaternion.Euler(0, 0, angle);
                go.GetComponent<Image>().color = highlight
                    ? new Color(0.85f, 0.55f, 0.25f, 0.9f)
                    : new Color(0.45f, 0.38f, 0.3f, 0.55f);

                if (highlight) _routeHighlights.Add(go);
                else _roadViews.Add(go);
            }
        }

        void CreateNodeView(MapNodeData node)
        {
            var size = node.type switch
            {
                NodeType.Capital => 78f,
                NodeType.Town => 64f,
                NodeType.Castle => 60f,
                NodeType.QuestLair => 56f,
                NodeType.BanditCamp => 50f,
                _ => 48f
            };
            var go = new GameObject(node.id, typeof(RectTransform), typeof(Button));
            go.transform.SetParent(_mapContent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = MapToLocal(node.mapPosition);

            // Shape is a separate child so bandit camps can render as a rotated diamond
            // without rotating the label text too.
            var shapeGo = new GameObject("Shape", typeof(RectTransform), typeof(Image));
            shapeGo.transform.SetParent(go.transform, false);
            var shapeRt = shapeGo.GetComponent<RectTransform>();
            if (node.type == NodeType.BanditCamp)
            {
                shapeRt.anchorMin = new Vector2(0.15f, 0.15f);
                shapeRt.anchorMax = new Vector2(0.85f, 0.85f);
                shapeRt.localRotation = Quaternion.Euler(0, 0, 45f);
            }
            else
            {
                shapeRt.anchorMin = Vector2.zero;
                shapeRt.anchorMax = Vector2.one;
            }

            shapeRt.offsetMin = Vector2.zero;
            shapeRt.offsetMax = Vector2.zero;
            var img = shapeGo.GetComponent<Image>();
            img.color = NodeColor(node);
            var id = node.id;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnNodeClicked(id));
            var label = UiFactory.Label(go.transform, "Name", node.displayName, 17, TextAnchor.LowerCenter, Color.white);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(-0.4f, -0.55f);
            lrt.anchorMax = new Vector2(1.4f, 0.15f);
            if (node.isSkeleton)
            {
                var tag = UiFactory.Label(go.transform, "Stub", "far realm", 14, TextAnchor.UpperCenter, UiFactory.Theme.TextDim);
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
            var visible = g.WorldParties.VisibleNear(g.PlayerMapPosition(),
                (g.Journey.IsActive ? 3.2f : 2.2f) * WorldPartyDirector.MapScaleFactor);
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
            if (node.type == NodeType.BanditCamp) return new Color(0.32f, 0.1f, 0.1f, 1f);
            if (node.isSkeleton) return new Color(0.25f, 0.28f, 0.4f, 0.9f);
            return node.type switch
            {
                NodeType.Capital => new Color(0.72f, 0.62f, 0.28f, 1f),
                NodeType.Town => new Color(0.35f, 0.48f, 0.55f, 1f),
                NodeType.Castle => new Color(0.4f, 0.36f, 0.42f, 1f),
                _ => new Color(0.32f, 0.42f, 0.3f, 1f)
            };
        }

        static bool IsSettlementNode(MapNodeData node) =>
            !node.isTemporary && node.type is NodeType.Capital or NodeType.Town or NodeType.Castle or NodeType.Village;

        static Vector2 MapToLocal(Vector2 mapPos) => mapPos * MapScale;

        void CenterOnParty()
        {
            _mapContent.anchoredPosition = -MapToLocal(GameState.Instance.PlayerMapPosition());
        }

        void ZoomIn() => SetZoom(_mapZoom + 0.2f);
        void ZoomOut() => SetZoom(_mapZoom - 0.2f);

        void SetZoom(float zoom)
        {
            _mapZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            _mapContent.localScale = Vector3.one * _mapZoom;
        }

        void SelectNode(string nodeId)
        {
            _selectedNodeId = nodeId;
            if (!GameState.Instance.Map.TryGetNode(nodeId, out var node)) return;
            var g = GameState.Instance;
            var here = g.Party.currentNodeId == nodeId && !g.Journey.IsActive;
            var route = g.Map.GetRoute(g.Party.currentNodeId, nodeId);
            RoadEdgeData offPathEdge = null;
            var hasOffPath = route.Count == 0 && g.Map.TryGetOffPathEdge(g.Party.currentNodeId, nodeId, out offPathEdge);
            var hours = 0f;
            foreach (var e in route) hours += e.travelHours;
            var services = "";
            if (node.hasStore || node.type == NodeType.Village) services += "Market ";
            if (node.hasRecruitment || node.type == NodeType.Village) services += "Recruit ";
            if (node.hasBookStore) services += "Books ";
            _inspectTitle.text = node.displayName;
            var campStatus = "";
            if (node.type == NodeType.BanditCamp)
            {
                campStatus = g.CanRaidCamp(node.id, out var daysLeft)
                    ? "\nBandit camp — ready to raid."
                    : $"\nBandit camp — regrouping, raidable again in {daysLeft}d.";
            }

            var travelInfo = here
                ? "You are here.\n"
                : route.Count > 0
                    ? $"Road: {route.Count} legs · road-time ~{hours:0}h (actual time depends on mounts/scouting/terrain)\n"
                    : hasOffPath
                        ? $"No road — off-path only (~{offPathEdge.travelHours:0}h bushwhacking, slower and more dangerous)\n"
                        : "No route.\n";

            var warLine = "";
            var prosperityLine = "";
            if (IsSettlementNode(node))
            {
                var prosperity = g.ProsperityOf(node.id);
                var mood = prosperity >= 65f ? "thriving" : prosperity >= 40f ? "steady" : prosperity >= 20f ? "struggling" : "sacked";
                prosperityLine = $"\nProsperity: {prosperity:0}% ({mood})";

                var regard = g.NotableRelationOf(node.id);
                var regardMood = regard >= GameConstants.NotableUpgradeThreshold ? "trusts you — offers better recruits"
                    : regard >= 35 ? "cordial" : regard >= 15 ? "wary" : "cold";
                prosperityLine += $"\nNotable regard: {regard}/100 ({regardMood})";

                var enemies = g.Diplomacy.EnemiesOf(node.controllingFaction);
                if (enemies.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var e in enemies) names.Add(GameState.FactionName(e));
                    warLine = $"\nAt war with: {string.Join(", ", names)}";
                }
            }

            _inspectBody.text =
                $"{node.type} · {node.controllingFaction}\n" +
                travelInfo +
                $"Services: {(services == "" ? "None" : services)}" +
                campStatus +
                prosperityLine +
                warLine +
                (node.isSkeleton ? "\nFar-realm stub." : "");
            foreach (var kv in _nodeViews)
            {
                if (!g.Map.TryGetNode(kv.Key, out var n)) continue;
                kv.Value.GetComponentInChildren<Image>().color = kv.Key == nodeId ? new Color(0.95f, 0.9f, 0.55f) : NodeColor(n);
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
            if (string.IsNullOrEmpty(_selectedNodeId))
            {
                AppendLog("Select a destination.");
                return;
            }

            StartJourneyTo(_selectedNodeId);
        }

        /// <summary>Shared journey launcher for both the Travel button and the double-click travel popup.</summary>
        void StartJourneyTo(string destId)
        {
            var g = GameState.Instance;
            if (g.Journey.IsActive)
            {
                AppendLog("Already travelling.");
                return;
            }

            if (destId == g.Party.currentNodeId)
            {
                AppendLog("Already here.");
                return;
            }

            var route = g.Map.GetRoute(g.Party.currentNodeId, destId);
            if (route.Count > 0)
            {
                if (!g.Journey.Begin(g.Map, g.Party.currentNodeId, destId, out var err))
                {
                    AppendLog(err);
                    return;
                }

                HighlightActiveRoute(route);
                EnsurePlayerMarker();
                UpdatePlayerMarker();
                _autoTickTimer = AutoTravelIntervalSeconds;
                _journeyHud.text = $"Journey → {destId} · {g.Journey.Steps.Count} inches · auto-travelling";
                AppendLog($"Route set ({g.Journey.Steps.Count} inches). Mounts & scouting change inch time and event chance.");
                RefreshWorldParties();
                return;
            }

            if (g.Map.TryGetOffPathEdge(g.Party.currentNodeId, destId, out _))
            {
                if (!g.Journey.BeginOffPath(g.Map, g.Party.currentNodeId, destId, out var offErr))
                {
                    AppendLog(offErr);
                    return;
                }

                ClearRouteHighlights();
                DrawOffPathTrail(new RoadEdgeData { id = "active_offpath", fromNodeId = g.Party.currentNodeId, toNodeId = destId }, true);
                EnsurePlayerMarker();
                UpdatePlayerMarker();
                _autoTickTimer = AutoTravelIntervalSeconds;
                _journeyHud.text = $"Off-path → {destId} · {g.Journey.Steps.Count} inches · rough going";
                AppendLog($"You leave the road and cut cross-country ({g.Journey.Steps.Count} inches) — slower, and trouble finds you more easily out here.");
                RefreshWorldParties();
                return;
            }

            AppendLog("No road route.");
        }

        /// <summary>Single click selects a node (shows its info); a quick second click on the same node
        /// opens a travel confirmation — so you can set off even with the bottom menu collapsed.</summary>
        void OnNodeClicked(string id)
        {
            var now = Time.unscaledTime;
            var isDouble = id == _lastClickNodeId && now - _lastClickTime <= DoubleClickSeconds;
            SelectNode(id);
            if (isDouble)
            {
                _lastClickTime = -1f; // consume, so a third tap doesn't immediately re-open the popup
                ShowTravelConfirm(id);
                return;
            }

            _lastClickNodeId = id;
            _lastClickTime = now;
        }

        void ShowTravelConfirm(string nodeId)
        {
            var g = GameState.Instance;
            if (!g.Map.TryGetNode(nodeId, out var node)) return;
            BuildTravelConfirmPanel();
            _travelConfirmTarget = nodeId;

            var route = g.Map.GetRoute(g.Party.currentNodeId, nodeId);
            RoadEdgeData offPathEdge = null;
            var hasOffPath = route.Count == 0 && g.Map.TryGetOffPathEdge(g.Party.currentNodeId, nodeId, out offPathEdge);
            var hours = 0f;
            foreach (var e in route) hours += e.travelHours;

            string body;
            bool canGo;
            if (g.Journey.IsActive) { body = "You're already on the road."; canGo = false; }
            else if (nodeId == g.Party.currentNodeId) { body = "You are already here."; canGo = false; }
            else if (route.Count > 0) { body = $"Road: {route.Count} legs · ~{hours:0}h\n(actual time varies with mounts, scouting, terrain)"; canGo = true; }
            else if (hasOffPath) { body = $"No road — off-path only\n(~{offPathEdge.travelHours:0}h bushwhacking, slower and more dangerous)"; canGo = true; }
            else { body = "No route from here."; canGo = false; }

            _travelConfirmTitle.text = $"Travel to {node.displayName}?";
            _travelConfirmBody.text = body;
            _travelConfirmGo.interactable = canGo;
            _travelConfirmPanel.SetActive(true);
            _travelConfirmPanel.transform.SetAsLastSibling();
        }

        void BuildTravelConfirmPanel()
        {
            if (_travelConfirmPanel != null) return;
            var panel = UiFactory.Panel(_canvas.transform, "TravelConfirm", new Vector2(0.31f, 0.39f), new Vector2(0.69f, 0.61f), new Color(0.1f, 0.11f, 0.13f, 0.98f));
            _travelConfirmPanel = panel.gameObject;

            _travelConfirmTitle = UiFactory.Label(panel, "Title", "", 27, TextAnchor.UpperCenter, UiFactory.Theme.TextTitle);
            Stretch(_travelConfirmTitle, 0.05f, 0.6f, 0.95f, 0.95f);
            _travelConfirmBody = UiFactory.Label(panel, "Body", "", 20, TextAnchor.UpperCenter, UiFactory.Theme.TextDim);
            Stretch(_travelConfirmBody, 0.06f, 0.32f, 0.94f, 0.6f);

            _travelConfirmGo = UiFactory.Button(panel, "Go", "Travel", new Vector2(0.08f, 0.06f), new Vector2(0.49f, 0.28f), () =>
            {
                var target = _travelConfirmTarget;
                HideTravelConfirm();
                if (!string.IsNullOrEmpty(target))
                    StartJourneyTo(target);
            });
            UiFactory.Button(panel, "Cancel", "Cancel", new Vector2(0.51f, 0.06f), new Vector2(0.92f, 0.28f), HideTravelConfirm);
            _travelConfirmPanel.SetActive(false);
        }

        void HideTravelConfirm()
        {
            if (_travelConfirmPanel != null)
                _travelConfirmPanel.SetActive(false);
        }

        void AdvanceJourney()
        {
            var g = GameState.Instance;
            if (!g.Journey.IsActive)
            {
                AppendLog("No active journey — select a town and tap Travel.");
                return;
            }

            if (_travelLocked) return;

            // No per-inch log line — only the journey's start (TravelToSelected) and end
            // (OnJourneyFinished) get logged, plus whatever an encounter itself reports.
            g.Journey.TryAdvance(g.Party, g.Hero, g.Travel, g.Economy, g.TroopRoster, g.Rng,
                out var encounter, out _, out var finished);

            // hours advanced ~use last step estimate for world party tick
            g.WorldParties.TickTowardTargets(g.Map, 1.2f);
            UpdatePlayerMarker();
            CenterOnParty();
            RefreshWorldParties();
            RefreshHud();

            if (encounter.kind != TravelEncounterKind.None)
            {
                if (encounter.kind == TravelEncounterKind.Weather)
                    ApplyWeatherPenalty(g);
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
            _travelLocked = true;
            _pendingEncounter = encounter;
            _encounterTitle.text = encounter.title;
            _encounterBody.text = encounter.body + "\n\nNearby bands may also be watching.";

            var accent = EncounterVisuals.AccentColor(encounter.kind);
            _encounterPanelImage.color = Color.Lerp(new Color(0.12f, 0.1f, 0.08f, 0.98f), accent, 0.22f);
            foreach (Transform child in _encounterIconSlot) Destroy(child.gameObject);
            UiFactory.IconBox(_encounterIconSlot, "Icon", Vector2.zero, Vector2.one,
                EncounterVisuals.ResourcePath(encounter.kind), accent, EncounterVisuals.Abbrev(encounter.kind));

            var isCaravan = encounter.kind == TravelEncounterKind.Trader;
            _fightBtn.gameObject.SetActive(encounter.canFight);
            _fightBtn.GetComponentInChildren<Text>().text = isCaravan ? "Raid" : "Fight";
            _fleeBtn.gameObject.SetActive(encounter.canFlee);
            var showTalk = encounter.canTalk || encounter.canPay;
            _talkBtn.gameObject.SetActive(showTalk);
            if (showTalk)
            {
                var cost = encounter.friendlyBandits ? 0 : EncounterCost(encounter.kind);
                var label = _talkBtn.GetComponentInChildren<Text>();
                label.text = isCaravan
                    ? (cost > 0 ? $"Trade {cost}g" : "Trade")
                    : (cost > 0 ? $"Pay {cost}g" : "Talk");
            }

            var enemyCount = BattleUI.ForceCount(encounter.cachedForce ?? BattleUI.EncounterForce(encounter.kind));
            var canSurrenderDemand = !encounter.friendlyBandits && encounter.kind == TravelEncounterKind.BanditAmbush && BanditSurrender.CanOffer(enemyCount);
            _surrenderBtn.gameObject.SetActive(canSurrenderDemand);
            if (canSurrenderDemand)
            {
                var chance = BanditSurrender.SuccessChance(enemyCount);
                _surrenderBtn.GetComponentInChildren<Text>().text = $"Demand Surrender (~{chance * 100f:0}%)";
            }

            _encounterPanel.SetActive(true);
        }

        static int EncounterCost(TravelEncounterKind kind) => kind switch
        {
            TravelEncounterKind.Trader => 25,
            TravelEncounterKind.Healers => 10,
            TravelEncounterKind.Refugees => 6,
            TravelEncounterKind.MinorThieves => 10,
            TravelEncounterKind.BanditAmbush => 25,
            TravelEncounterKind.ButterRaid => 60,
            TravelEncounterKind.VoidoviaPatrol => GameConstants.PatrolBribeCost,
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
                battleUi.BeginEncounterBattle(e, outcome => OnEncounterBattleResolved(outcome, e));
                return;
            }

            if (choice == "surrender" && e.kind == TravelEncounterKind.BanditAmbush)
            {
                _pendingEncounter = null;
                var g = GameState.Instance;
                var enemyCount = BattleUI.ForceCount(e.cachedForce ?? BattleUI.EncounterForce(e.kind));
                var chance = BanditSurrender.SuccessChance(enemyCount);
                if (g.Rng.NextDouble() < chance)
                {
                    AppendLog(BanditSurrender.Resolve(enemyCount, e.title));
                    RefreshHud();
                    AdvanceOrFinishAfterEncounter();
                }
                else
                {
                    AppendLog($"They refuse to yield: {e.title}");
                    var battleUi = FindObjectOfType<BattleUI>() ?? new GameObject("BattleUI").AddComponent<BattleUI>();
                    battleUi.BeginEncounterBattle(e, outcome => OnEncounterBattleResolved(outcome, e));
                }

                return;
            }

            if (choice == "flee" && e.canFlee && e.canFight)
            {
                var g = GameState.Instance;
                var chance = JourneyController.EscapeChance(e.kind, g.Party, g.Hero, g.TroopRoster);
                if (g.Rng.NextDouble() >= chance)
                {
                    AppendLog($"Your escape falters — they close before you can break away: {e.title}");
                    _pendingEncounter = null;
                    var battleUi = FindObjectOfType<BattleUI>() ?? new GameObject("BattleUI").AddComponent<BattleUI>();
                    battleUi.BeginEncounterBattle(e, outcome => OnEncounterBattleResolved(outcome, e));
                    return;
                }
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
            var cost = e.friendlyBandits ? 0 : EncounterCost(e.kind);
            var paidInFull = true;
            if (cost > 0)
            {
                var paid = Mathf.Min(cost, g.Party.gold);
                g.Party.gold -= paid;
                paidInFull = paid >= cost;
                AppendLog(paidInFull
                    ? $"You pay {paid}g: {e.title}"
                    : $"You scrape together {paid}g (short of the {cost}g asked): {e.title}");
            }
            else
            {
                AppendLog($"You talk your way through: {e.title}");
            }

            switch (e.kind)
            {
                case TravelEncounterKind.Trader:
                    if (paidInFull && !string.IsNullOrEmpty(e.caravanGoodId) && e.caravanGoodCount > 0)
                    {
                        g.Party.AddInventory(e.caravanGoodId, e.caravanGoodCount);
                        g.Party.AddRelation(FactionId.Traders, 1);
                        AppendLog($"You trade with the caravan — {e.caravanGoodCount}× trade goods for your saddlebags (resell them in a town).");
                    }
                    else
                    {
                        AppendLog("You hadn't the coin to trade in earnest.");
                    }
                    break;

                case TravelEncounterKind.Healers:
                    if (paidInFull)
                    {
                        g.Party.AddMorale(20f);
                        g.Party.AddRelation(FactionId.Healers, 1);
                        AppendLog("The healers tend your men — spirits lift.");
                    }
                    else
                    {
                        g.Party.AddMorale(5f);
                    }
                    break;

                case TravelEncounterKind.Refugees:
                    if (paidInFull)
                    {
                        g.Party.AddMorale(5f);
                        g.Party.AddRelation(FactionId.Independent, 2);
                        AppendLog("Word of your charity travels with them.");
                    }
                    else
                    {
                        g.Party.AddMorale(-5f);
                    }
                    break;

                case TravelEncounterKind.Rumour:
                    ResolveRumour();
                    break;

                case TravelEncounterKind.ButterRaid:
                    if (paidInFull)
                        g.Party.AddRelation(FactionId.ButterKlanBoys, 2);
                    break;

                case TravelEncounterKind.MinorThieves or TravelEncounterKind.BanditAmbush when e.friendlyBandits:
                    g.Party.AddRelation(FactionId.Bandits, 1);
                    g.Party.AddMorale(2f);
                    AppendLog("You trade news with the raiders — nothing owed either way.");
                    break;
            }
        }

        void ResolveRumour()
        {
            var g = GameState.Instance;
            var nearby = g.WorldParties.VisibleNear(g.Journey.CurrentMapPos, 6f * WorldPartyDirector.MapScaleFactor);
            if (nearby.Count == 0)
            {
                AppendLog("Just idle chatter — nothing worth acting on.");
                return;
            }

            WorldParty closest = null;
            var bestDist = float.MaxValue;
            foreach (var p in nearby)
            {
                var d = (p.mapPos - g.Journey.CurrentMapPos).sqrMagnitude;
                if (d >= bestDist) continue;
                bestDist = d;
                closest = p;
            }

            var dir = DirectionLabel(closest.mapPos - g.Journey.CurrentMapPos);
            AppendLog($"The drover points {dir}: \"{closest.displayName}\" was seen that way.");
            g.Party.AddMorale(2f);
        }

        static string DirectionLabel(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0001f) return "nearby";
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            return angle switch
            {
                < 22.5f or >= 337.5f => "east",
                < 67.5f => "northeast",
                < 112.5f => "north",
                < 157.5f => "northwest",
                < 202.5f => "west",
                < 247.5f => "southwest",
                < 292.5f => "south",
                _ => "southeast"
            };
        }

        void ApplyWeatherPenalty(GameState g)
        {
            const float extraHours = 2f;
            g.Party.hours += extraHours;
            while (g.Party.hours >= GameConstants.HoursPerDay)
            {
                g.Party.hours -= GameConstants.HoursPerDay;
                g.Party.day++;
                g.OnNewDay();
            }

            g.Economy.ConsumeFood(g.Party, extraHours / GameConstants.HoursPerDay, out var foodLog);
            AppendLog($"The storm costs you {extraHours:0}h more on the road. {foodLog}");
        }

        void OnEncounterBattleResolved(BattleOutcome outcome, TravelEncounter e)
        {
            AppendLog(outcome.playerVictory
                ? $"You best the {e.title}. {outcome.summary}"
                : $"You're driven off by the {e.title}. {outcome.summary}");
            if (e.kind == TravelEncounterKind.ButterRaid && outcome.playerVictory)
                GameState.Instance.Party.AddRelation(FactionId.ButterKlanBoys, -3);

            if (e.kind == TravelEncounterKind.Trader && outcome.playerVictory)
            {
                var p = GameState.Instance.Party;
                p.gold += e.caravanGold;
                if (!string.IsNullOrEmpty(e.caravanGoodId) && e.caravanGoodCount > 0)
                    p.AddInventory(e.caravanGoodId, e.caravanGoodCount);
                p.AddRelation(FactionId.Traders, -8);
                p.AddRelation(FactionId.Voidovia, -5); // banditry on the realm's roads darkens your name
                AppendLog($"You plunder the caravan — {e.caravanGold}g and {e.caravanGoodCount}× trade goods. Word of the robbery spreads; Traders and Voidovia sour on you.");
            }

            if (e.kind == TravelEncounterKind.VoidoviaPatrol)
            {
                var p = GameState.Instance.Party;
                if (outcome.playerVictory)
                {
                    p.AddRelation(FactionId.Voidovia, -5);
                    AppendLog("You cut down Lord Void's men. Your name only darkens — the bounty stands.");
                }
                else
                {
                    var fine = Mathf.Min(p.gold, p.bounty);
                    p.gold -= fine;
                    p.SetBounty(p.bounty - fine);
                    AppendLog(p.bounty > 0
                        ? $"The patrol overpowers you. A {fine}g fine is taken, but {p.bounty}g of bounty remains."
                        : $"The patrol overpowers you — but a {fine}g fine settles your debt. Your name is clear.");
                }
            }

            if (!outcome.playerVictory && e.kind == TravelEncounterKind.BanditAmbush)
            {
                var g = GameState.Instance;
                var nearestCamp = g.FindNearestBanditCamp(g.PlayerMapPosition());
                AppendLog(BanditCaptivity.Apply(g, nearestCamp?.parentSettlementId));
                CenterOnParty();
            }

            RefreshHud();
            AdvanceOrFinishAfterEncounter();
        }

        void AdvanceOrFinishAfterEncounter()
        {
            _travelLocked = false;
            if (!GameState.Instance.Journey.IsActive)
                OnJourneyFinished();
            else
            {
                _autoTickTimer = AutoTravelIntervalSeconds;
                _journeyHud.text =
                    $"Inch {GameState.Instance.Journey.StepIndex}/{GameState.Instance.Journey.Steps.Count}";
            }
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

            var escort = g.QuestBoard.FindActiveEscortAt(g.Party.currentNodeId);
            if (escort != null)
            {
                if (!g.QuestBoard.TryResolveEscortArrival(escort, g, g.Rng, out _, out var escortLog))
                {
                    AppendLog(escortLog);
                    var enemy = QuestBoardService.RollEnemyForce("Ambushers", g.Rng);
                    var player = new BattleForce { name = "Your warband", troops = new List<TroopStack>(g.Party.troops) };
                    FindObjectOfType<BattleUI>()?.Begin(player, enemy, false, null, outcome =>
                    {
                        g.QuestBoard.TryResolveEscortBattle(escort, outcome, g, out var battleLog);
                        AppendLog(battleLog);
                        RefreshHud();
                    });
                }
                else
                {
                    AppendLog(escortLog);
                }
            }

            AppendLog($"Arrived {g.Party.currentNodeId}.");
            RefreshHud();
            RefreshWorldParties();
        }

        void OpenSettlementMenu()
        {
            var g = GameState.Instance;
            if (g.Journey.IsActive)
            {
                AppendLog("Finish or wait — you're on the road.");
                return;
            }

            var ui = FindObjectOfType<SettlementMenuUI>() ?? new GameObject("SettlementMenuUI").AddComponent<SettlementMenuUI>();
            ui.Open(g.Party.currentNodeId, AppendLog, RefreshHud);
        }

        void OpenParty()
        {
            var ui = FindObjectOfType<PartyPanelUI>() ?? new GameObject("PartyPanelUI").AddComponent<PartyPanelUI>();
            ui.Open(RefreshHud);
        }

        void OpenTroops()
        {
            var ui = FindObjectOfType<PartyPanelUI>() ?? new GameObject("PartyPanelUI").AddComponent<PartyPanelUI>();
            ui.Open(RefreshHud, "troops");
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
                AppendLog("Advisor is in Lik-E-Leek.");
                return;
            }

            if (g.Map.TryGetNode(g.Party.currentNodeId, out var node) && node.hasBookStore &&
                g.Act1Quest.Beat is StolenItemQuestBeat.InvestigateCities or StolenItemQuestBeat.ExButterIntel
                    or StolenItemQuestBeat.LairSpawned or StolenItemQuestBeat.Completed)
            {
                // Secondary: books when advisor already done
            }

            g.Act1Quest.SpeakToAdvisor();
            AppendLog("Advisor: try Beef or Tollbar.");
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
                AppendLog("Advisor names Beef and Tollbar.");
                return;
            }

            if (g.Party.currentNodeId == g.Act1Quest.LairNodeId && g.Act1Quest.LairVisible &&
                g.Act1Quest.Beat == StolenItemQuestBeat.LairSpawned)
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
                    AppendLog("Bring the Chief to Lik-E-Leek, Here, or Al-Javvid for audience.");
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

            var questAtNode = g.QuestBoard.FindActiveByTargetNode(g.Party.currentNodeId);
            if (questAtNode != null)
            {
                var enemy = QuestBoardService.RollEnemyForce(questAtNode.title, g.Rng);
                var player = new BattleForce { name = "Your warband", troops = new List<TroopStack>(g.Party.troops) };
                var giverNodeId = questAtNode.giverNodeId;
                FindObjectOfType<BattleUI>()?.Begin(player, enemy, false, null, outcome =>
                {
                    if (g.QuestBoard.TryResolveCombatVictory(questAtNode, outcome, g, out var log))
                    {
                        g.Party.currentNodeId = giverNodeId;
                        RebuildDynamicNodes();
                        SelectNode(giverNodeId);
                        CenterOnParty();
                    }

                    AppendLog(log);
                    RefreshHud();
                });
                return;
            }

            AppendLog("No quest beat here. Advisor in Lik-E-Leek, lair raid, or Void audience after capture.");
        }

        void RefreshHud()
        {
            if (_hud == null || GameState.Instance == null) return;
            foreach (var note in GameState.Instance.PendingNotifications)
                AppendLog(note);
            GameState.Instance.PendingNotifications.Clear();

            var p = GameState.Instance.Party;
            var wages = GameState.Instance.Economy.WeeklyWageBill(p);
            var need = GameState.Instance.Economy.DailyFoodNeed(p);
            var fill = GameState.Instance.Economy.TotalFoodFill(p);
            var days = need > 0.01f ? fill / need : 99f;
            var woundedTag = p.TotalWounded > 0 ? $" ({p.TotalWounded} wounded)" : "";
            var cap = GameState.Instance.MaxPartySize;
            var wantedTag = p.IsWantedInVoidovia ? $" · WANTED {p.bounty}g" : "";
            _hud.text =
                $"{GameState.Instance.Hero.name} · Day {p.day} · {p.gold}g · Food~{days:0.0}d · Wages {wages}/wk · {p.TotalMen}/{cap} men{woundedTag}{wantedTag} · {p.currentNodeId}";
            UpdatePlayerMarker();
            if (_booksBtn != null)
                _booksBtn.gameObject.SetActive(GameState.Instance.Map.TryGetNode(p.currentNodeId, out var here) && here.hasBookStore);
            if (_actionBtn != null && GameState.Instance.Map.TryGetNode(p.currentNodeId, out var currentNode))
                _actionBtn.GetComponentInChildren<Text>().text = ActionButtonLabel(currentNode.type);
        }

        static string ActionButtonLabel(NodeType type) => type switch
        {
            NodeType.Capital => "■ Enter City",
            NodeType.Town => "■ Enter Town",
            NodeType.Castle => "■ Enter Castle",
            NodeType.Village => "■ Enter Village",
            NodeType.BanditCamp => "■ Enter Camp",
            NodeType.QuestLair => "■ Enter Lair",
            _ => "■ Enter"
        };

        void Update()
        {
            if (GameState.Instance == null || _playerMarker == null) return;
            var s = 1f + Mathf.Sin(Time.time * 3f) * 0.06f;
            _playerMarker.localScale = new Vector3(s, s, 1f);

            if (GameState.Instance.Journey.IsActive && !_travelLocked)
            {
                _autoTickTimer -= Time.deltaTime;
                if (_autoTickTimer <= 0f)
                {
                    _autoTickTimer = AutoTravelIntervalSeconds;
                    AdvanceJourney();
                }
            }
        }
    }
}
