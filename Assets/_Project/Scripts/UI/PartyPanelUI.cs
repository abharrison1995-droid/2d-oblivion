using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Gold / food / wages strip opener + dual inventory/equipment + power-card pouch submenu.
    /// </summary>
    public class PartyPanelUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _header;
        Text _body;
        RectTransform _list;
        bool _pouchMode;
        System.Action _onClose;

        public void Open(System.Action onClose = null)
        {
            _onClose = onClose;
            _pouchMode = false;
            Ensure();
            _canvas.gameObject.SetActive(true);
            Rebuild();
        }

        void Ensure()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("PartyPanelCanvas", 32);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.06f, 0.07f, 0.09f, 0.98f));
            _header = UiFactory.Label(root, "Header", "", 28, TextAnchor.UpperLeft, new Color(0.93f, 0.86f, 0.7f));
            Stretch(_header, 0.05f, 0.88f, 0.95f, 0.98f);
            _body = UiFactory.Label(root, "Body", "", 22, TextAnchor.UpperLeft, new Color(0.8f, 0.82f, 0.78f));
            Stretch(_body, 0.05f, 0.74f, 0.95f, 0.87f);
            _list = UiFactory.Panel(root, "List", new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.72f), new Color(0, 0, 0, 0.18f));

            UiFactory.Button(root, "Pouch", "Power-card pouch", new Vector2(0.05f, 0.02f), new Vector2(0.48f, 0.11f), () =>
            {
                _pouchMode = !_pouchMode;
                Rebuild();
            });
            UiFactory.Button(root, "Close", "Close", new Vector2(0.52f, 0.02f), new Vector2(0.95f, 0.11f), () =>
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
            var p = g.Party;
            var wages = g.Economy.WeeklyWageBill(p);
            var foodNeed = g.Economy.DailyFoodNeed(p);
            var foodFill = 0f;
            foreach (var f in p.food)
            {
                // rough fill
                foodFill += f.count;
            }

            var daysLeft = foodNeed > 0.01f ? foodFill / foodNeed : 99f;
            _header.text = $"{g.Hero.name} · {p.gold}g · Food ~{daysLeft:0.0}d · Wages {wages}g/wk · {p.TotalMen} men";
            _body.text =
                $"Equipped weapon: {p.equippedWeaponId ?? "(none)"}\n" +
                $"Equipped armour: {p.equippedArmourId ?? "(none)"}\n" +
                (_pouchMode ? "POWER-CARD POUCH" : "INVENTORY — tap gear to equip");

            var y = 0.95f;
            if (_pouchMode)
            {
                if (p.powerCards.Count == 0)
                {
                    PlaceLabel("No treatises yet. Book stores, quests, and chiefs.", ref y);
                    return;
                }

                foreach (var c in p.powerCards)
                {
                    var name = c.itemId;
                    if (g.Battle.TryGetCard(c.itemId, out var def))
                        name = $"{def.displayName} — {def.description}";
                    PlaceLabel($"{name}  ×{c.count}", ref y);
                }

                return;
            }

            foreach (var stack in p.inventory)
            {
                var id = stack.itemId;
                var label = $"{id} ×{stack.count}";
                EquipSlot slot = EquipSlot.None;
                if (g.Market.TryGetItem(id, out var item))
                {
                    label = $"{item.displayName} ×{stack.count} [{item.quality}]";
                    slot = item.equipSlot;
                }

                var top = y;
                var bottom = y - 0.12f;
                y = bottom - 0.02f;
                if (slot != EquipSlot.None)
                {
                    var s = slot;
                    UiFactory.Button(_list, id, $"Equip: {label}", new Vector2(0f, bottom), new Vector2(1f, top), () =>
                    {
                        if (p.TryEquip(id, s, out _))
                            _body.text = $"Equipped {id}.";
                        Rebuild();
                    });
                }
                else
                {
                    PlaceLabel(label, ref y, top, bottom);
                }
            }

            if (p.inventory.Count == 0)
                PlaceLabel("Bags empty aside from rations (food is separate).", ref y);
        }

        void PlaceLabel(string text, ref float y, float top = -1, float bottom = -1)
        {
            if (top < 0)
            {
                top = y;
                bottom = y - 0.1f;
                y = bottom - 0.02f;
            }

            var lbl = UiFactory.Label(_list, "L" + y, text, 18, TextAnchor.MiddleLeft, Color.white);
            var rt = lbl.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, bottom);
            rt.anchorMax = new Vector2(1f, top);
        }
    }
}
