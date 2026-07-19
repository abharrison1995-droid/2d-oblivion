using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Special Book stores sell extremely expensive power cards / treatises.
    /// </summary>
    public class BookStoreUI : MonoBehaviour
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
            _canvas = UiFactory.CreateCanvas("BookStoreCanvas", 35);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, UiFactory.Theme.PanelBackground);
            _title = UiFactory.Label(root, "Title", "Book Store — Treatises of War", 34, TextAnchor.UpperCenter, UiFactory.Theme.TextTitle);
            var tr = _title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.05f, 0.9f);
            tr.anchorMax = new Vector2(0.95f, 0.98f);

            _body = UiFactory.Label(root, "Body",
                "Power cards are ruinously expensive. Save for bosses, quests… or bankrupt yourself for an edge.",
                22, TextAnchor.UpperLeft, new Color(0.8f, 0.78f, 0.72f));
            var br = _body.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.06f, 0.78f);
            br.anchorMax = new Vector2(0.94f, 0.89f);

            _list = UiFactory.Panel(root, "List", new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.76f), new Color(0, 0, 0, 0.2f));
            UiFactory.Button(root, "Close", "Leave", new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.1f), () =>
            {
                _canvas.gameObject.SetActive(false);
                _onClose?.Invoke();
            });
        }

        void Rebuild()
        {
            foreach (Transform c in _list) Destroy(c.gameObject);
            var g = GameState.Instance;
            var gold = g.Party.gold;
            UiFactory.Label(_list, "Gold", $"Your gold: {gold}", 22, TextAnchor.UpperLeft, UiFactory.Theme.TextDim);
            var gr = _list.Find("Gold") as RectTransform;
            if (gr != null)
            {
                gr.anchorMin = new Vector2(0f, 0.9f);
                gr.anchorMax = new Vector2(1f, 1f);
            }

            var stock = new List<BattleCardDef>();
            foreach (var card in g.Battle.AllCards)
            {
                if (card.kind != BattleCardKind.Power) continue;
                if (card.bookstorePrice <= 0) continue;
                if (card.bossDrop) continue;
                stock.Add(card);
            }

            var y = 0.88f;
            foreach (var card in stock)
            {
                var c = card;
                var top = y;
                var bottom = y - 0.18f;
                y = bottom - 0.02f;
                var owned = g.Party.HasPowerCard(c.id) ? " (owned)" : "";
                UiFactory.Button(_list, c.id,
                    $"{c.displayName} — {c.bookstorePrice}g{owned}\n{c.description}",
                    new Vector2(0f, bottom), new Vector2(1f, top),
                    () => Buy(c));
            }
        }

        void Buy(BattleCardDef card)
        {
            var g = GameState.Instance;
            if (g.Party.HasPowerCard(card.id))
            {
                _body.text = "You already carry that treatise.";
                return;
            }

            if (!g.Party.TryBuyPowerCard(card.id, card.bookstorePrice))
            {
                _body.text = $"Need {card.bookstorePrice}g. You have {g.Party.gold}g. These are meant to hurt.";
                return;
            }

            if (g.Map.TryGetNode(g.Party.currentNodeId, out var node))
            {
                g.Market.EnsureMarket(node);
                var market = g.Market.Get(node.id);
                if (market != null) market.buyerGold += card.bookstorePrice;
            }

            _body.text = $"Purchased {card.displayName}. It waits in your pouch for battle.";
            Rebuild();
        }
    }
}
