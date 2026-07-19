using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Act 1 climax beat: meet Lord Void after delivering the Buttery Chief.
    /// </summary>
    public class VoidAudienceUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _body;
        System.Action _onDone;

        public void Open(System.Action onDone)
        {
            _onDone = onDone;
            Ensure();
            _canvas.gameObject.SetActive(true);
            _body.text =
                "Lord Void, The Wide Eyed Beast, receives you in a quiet solar — not a throne of bones.\n\n" +
                "\"They say I eat children,\" he smiles, soft as milk. \"I eat worries. Butter banners worry me.\"\n\n" +
                "He takes the captured Buttery Chief from your escort.\n\n" +
                "\"Ride for Voidovia's purse if you will. Mercenary pay — a few hundred a week. " +
                "Enough for men and bread. Not enough for an empire. Prove true, and later we speak of vassalage.\"";
        }

        void Ensure()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("VoidAudienceCanvas", 45);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, UiFactory.Theme.PanelBackground);
            var title = UiFactory.Label(root, "Title", "Audience — Lord Void", 36, TextAnchor.UpperCenter, UiFactory.Theme.TextTitle);
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.05f, 0.88f);
            tr.anchorMax = new Vector2(0.95f, 0.97f);

            UiFactory.Portrait(root, "Portrait", new Vector2(0.08f, 0.62f), new Vector2(0.30f, 0.86f), "lord_void");

            _body = UiFactory.Label(root, "Body", "", 24, TextAnchor.UpperLeft, UiFactory.Theme.TextBody);
            var br = _body.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.34f, 0.28f);
            br.anchorMax = new Vector2(0.92f, 0.86f);

            UiFactory.Button(root, "Accept", "Accept mercenary contract", new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.24f), () =>
            {
                GameState.Instance.Act1Quest.DeliverChiefToVoid(GameState.Instance.Party);
                GameState.Instance.TryOfferMercenaryContract(out _);
                Close();
            });
            UiFactory.Button(root, "Refuse", "Remain a free company (for now)", new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.11f), () =>
            {
                GameState.Instance.Act1Quest.DeliverChiefToVoid(GameState.Instance.Party);
                Close();
            });
        }

        void Close()
        {
            _canvas.gameObject.SetActive(false);
            _onDone?.Invoke();
        }
    }
}
