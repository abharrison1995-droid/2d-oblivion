using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Full-screen 3-step origin interview before the campaign map.
    /// </summary>
    public class CharacterCreationUI : MonoBehaviour
    {
        public System.Action<CharacterCreationResult> Completed;

        int _step; // 0 name/family, 1 childhood, 2 moose, 3 confirm
        string _name = "";
        FamilyOrigin _family = FamilyOrigin.Traders;
        ChildhoodLean _childhood = ChildhoodLean.OrganizingTeams;
        MooseChoice _moose = MooseChoice.NurseToRelease;

        Canvas _canvas;
        Text _title;
        Text _body;
        Text _statsPreview;
        RectTransform _choicesRoot;
        RectTransform _portraitSlot;
        Button _backBtn;
        readonly System.Collections.Generic.List<Button> _familyChoiceButtons = new();

        public void Show()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
                Rebuild();
                return;
            }

            _canvas = UiFactory.CreateCanvas("CharacterCreationCanvas", 50);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.08f, 0.09f, 0.11f, 0.98f));

            _title = UiFactory.Label(root, "Title", "Who are you?", 42, TextAnchor.UpperCenter, new Color(0.93f, 0.88f, 0.74f));
            var titleRt = _title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.88f);
            titleRt.anchorMax = new Vector2(0.95f, 0.98f);

            _portraitSlot = UiFactory.Panel(root, "PortraitSlot", new Vector2(0.78f, 0.62f), new Vector2(0.94f, 0.87f), Color.clear);

            _body = UiFactory.Label(root, "Body", "", 26, TextAnchor.UpperLeft, new Color(0.85f, 0.84f, 0.8f));
            var bodyRt = _body.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.06f, 0.62f);
            bodyRt.anchorMax = new Vector2(0.76f, 0.87f);

            _choicesRoot = UiFactory.Panel(root, "Choices", new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.60f), new Color(0, 0, 0, 0.15f));

            // Dedicated strip below the choices panel — never overlaps the last choice card.
            _backBtn = UiFactory.Button(root, "Back", "Back", new Vector2(0.06f, 0.21f), new Vector2(0.30f, 0.28f), null);
            _backBtn.GetComponent<Image>().color = new Color(0.32f, 0.16f, 0.15f, 0.95f);

            _statsPreview = UiFactory.Label(root, "Stats", "", 24, TextAnchor.LowerLeft, new Color(0.7f, 0.78f, 0.72f));
            var statsRt = _statsPreview.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.06f, 0.02f);
            statsRt.anchorMax = new Vector2(0.94f, 0.20f);

            Rebuild();
        }

        public void Hide()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        void Rebuild()
        {
            foreach (Transform child in _choicesRoot)
                Destroy(child.gameObject);

            foreach (Transform child in _portraitSlot)
                Destroy(child.gameObject);
            UiFactory.Portrait(_portraitSlot, "Portrait", Vector2.zero, Vector2.one, "hero_" + _family.ToString().ToLowerInvariant());

            var preview = CharacterCreation.Build(_name, _family, _childhood, _moose);
            _statsPreview.text =
                $"Preview — Combat {preview.combat}  Leadership {preview.leadership}  Tactics {preview.tactics}\n" +
                $"Trade {preview.trade}  Scouting {preview.scouting}  Gold {preview.startingGold}  Spawn {preview.spawnNodeId}";

            switch (_step)
            {
                case 0:
                    _title.text = "Family";
                    AddNameField();
                    _familyChoiceButtons.Clear();
                    for (var i = 0; i < 4; i++)
                    {
                        var idx = i;
                        var btn = AddChoice(CharacterCreation.FamilyLabels[i], CharacterCreation.FamilyBlurbs[i], i, () =>
                        {
                            if (string.IsNullOrWhiteSpace(_name)) return;
                            _family = (FamilyOrigin)idx;
                            _step = 1;
                            Rebuild();
                        });
                        _familyChoiceButtons.Add(btn);
                    }

                    UpdateNameGate();
                    break;
                case 1:
                    _title.text = "As a child";
                    _body.text = "As a child you gravitated toward…\nThis pushes how you lead, fight, ride, or deal.";
                    for (var i = 0; i < 4; i++)
                    {
                        var idx = i;
                        AddChoice(CharacterCreation.ChildhoodLabels[i], CharacterCreation.ChildhoodBlurbs[i], i, () =>
                        {
                            _childhood = (ChildhoodLean)idx;
                            _step = 2;
                            Rebuild();
                        });
                    }
                    break;
                case 2:
                    _title.text = "The moose";
                    _body.text = "You're faced with an injured moose right outside the village. You…";
                    for (var i = 0; i < 4; i++)
                    {
                        var idx = i;
                        AddChoice(CharacterCreation.MooseLabels[i], CharacterCreation.MooseBlurbs[i], i, () =>
                        {
                            _moose = (MooseChoice)idx;
                            _step = 3;
                            Rebuild();
                        });
                    }
                    break;
                case 3:
                    _title.text = "Begin";
                    _body.text = $"{preview.originSummary}\n\nStolen soon after: {preview.stolenItemFlavour}";
                    UiFactory.Button(_choicesRoot, "Confirm", "Enter Voidovia", new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.55f), () =>
                    {
                        var result = CharacterCreation.Build(_name, _family, _childhood, _moose);
                        _canvas.gameObject.SetActive(false);
                        Completed?.Invoke(result);
                    });
                    break;
            }

            _backBtn.gameObject.SetActive(_step > 0);
            _backBtn.onClick.RemoveAllListeners();
            if (_step > 0)
            {
                var previousStep = _step - 1;
                _backBtn.onClick.AddListener(() =>
                {
                    _step = previousStep;
                    Rebuild();
                });
            }
        }

        void AddNameField()
        {
            var field = UiFactory.Input(_choicesRoot, "Name", "Your name…", new Vector2(0f, 0.82f), new Vector2(1f, 0.98f));
            field.text = _name;
            field.onValueChanged.AddListener(v =>
            {
                _name = v;
                UpdateNameGate();
            });
        }

        void UpdateNameGate()
        {
            var hasName = !string.IsNullOrWhiteSpace(_name);
            foreach (var btn in _familyChoiceButtons)
                if (btn != null) btn.interactable = hasName;

            var nameLine = hasName ? $"Name: {_name}" : "Name: (not set yet)";
            _body.text = hasName
                ? $"{nameLine}\nYou are from a family of…\nThis sets your Voidovia starting place, coin, and base lean."
                : $"{nameLine}\nYou are from a family of…\nEnter a name above before choosing — Voidovia needs to know what to call you.";
        }

        Button AddChoice(string title, string blurb, int index, System.Action onClick)
        {
            // Top starts at 0.78 to leave room for the name field above on step 0; 4 choices stack.
            var top = 0.78f - index * 0.18f;
            var bottom = top - 0.16f;
            var btn = UiFactory.Button(_choicesRoot, "Choice" + index, $"{title}\n{blurb}", new Vector2(0f, bottom), new Vector2(1f, top), onClick);
            var label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 22;
                label.alignment = TextAnchor.MiddleLeft;
            }

            return btn;
        }
    }
}
