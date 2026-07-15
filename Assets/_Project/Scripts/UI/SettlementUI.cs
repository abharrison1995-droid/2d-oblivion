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

        public void Open(System.Action onClose = null)
        {
            _onClose = onClose;
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
            _body = UiFactory.Label(root, "Body", "", 22, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.76f));
            Stretch(_body, 0.06f, 0.78f, 0.94f, 0.89f);
            _list = UiFactory.Panel(root, "List", new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.76f), new Color(0, 0, 0, 0.2f));
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

            g.Market.EnsureMarket(node);
            var market = g.Market.Get(node.id);
            var buyer = market.isPeasant ? "Peasants" : "Merchant";
            _title.text = $"{node.displayName} — Market & Recruits";
            _body.text = $"{buyer} gold: {market.buyerGold}g\nYour gold: {g.Party.gold}g\nRecruits: {market.recruitTroopId} @ {market.recruitPrice}g (stock {market.recruitStock})";

            var y = 0.88f;
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
    }
}
