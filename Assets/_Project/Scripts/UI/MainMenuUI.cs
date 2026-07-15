using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// New Game / Continue menu shown after the title screen, before character creation.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        Canvas _canvas;
        Button _continueBtn;

        public void Show(System.Action onNewGame, System.Action onContinue)
        {
            Ensure(onNewGame, onContinue);
            RefreshContinueAvailability();
            _canvas.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        void Ensure(System.Action onNewGame, System.Action onContinue)
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("MainMenuCanvas", 55);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.06f, 0.06f, 0.08f, 1f));

            var title = UiFactory.Label(root, "Title", "VOIDOVIA", 44, TextAnchor.MiddleCenter, new Color(0.93f, 0.86f, 0.7f));
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.1f, 0.72f);
            tr.anchorMax = new Vector2(0.9f, 0.86f);

            UiFactory.Button(root, "NewGame", "New Game", new Vector2(0.3f, 0.5f), new Vector2(0.7f, 0.6f),
                () => onNewGame?.Invoke());
            _continueBtn = UiFactory.Button(root, "Continue", "Continue", new Vector2(0.3f, 0.36f), new Vector2(0.7f, 0.46f),
                () => onContinue?.Invoke());
        }

        void RefreshContinueAvailability()
        {
            var hasSave = SaveLoadService.SaveExists();
            _continueBtn.interactable = hasSave;
            var label = _continueBtn.GetComponentInChildren<Text>();
            if (label != null)
                label.text = hasSave ? "Continue" : "Continue (no save yet)";
        }
    }
}
