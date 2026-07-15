using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// First screen on boot. Click through to the New Game / Continue menu.
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        Canvas _canvas;
        System.Action _onContinue;

        public void Show(System.Action onContinue)
        {
            _onContinue = onContinue;
            Ensure();
            _canvas.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        void Ensure()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("TitleScreenCanvas", 60);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.05f, 0.05f, 0.07f, 1f));

            var title = UiFactory.Label(root, "Title", "VOIDOVIA", 72, TextAnchor.MiddleCenter, new Color(0.93f, 0.86f, 0.7f));
            Stretch(title, 0.1f, 0.56f, 0.9f, 0.76f);

            var tagline = UiFactory.Label(root, "Tagline", "A gritty low-fantasy warband saga", 24, TextAnchor.MiddleCenter, new Color(0.68f, 0.68f, 0.66f));
            Stretch(tagline, 0.1f, 0.47f, 0.9f, 0.55f);

            UiFactory.Button(root, "Continue", "Click to begin", new Vector2(0.35f, 0.16f), new Vector2(0.65f, 0.28f),
                () => _onContinue?.Invoke());
        }

        static void Stretch(Text t, float x0, float y0, float x1, float y1)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
        }
    }
}
