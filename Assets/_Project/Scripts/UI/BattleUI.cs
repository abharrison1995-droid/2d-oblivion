using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Football Manager-style decision card UI for battles.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _title;
        Text _prompt;
        Text _kuo;
        Text _log;
        RectTransform _optionsRoot;
        Action<BattleOutcome> _onFinished;
        bool _active;

        public void BeginLairBattle(Action<BattleOutcome> onFinished)
        {
            var g = GameState.Instance;
            var player = new BattleForce { name = "Your warband", troops = new List<TroopStack>(g.Party.troops) };
            var enemy = new BattleForce
            {
                name = "Buttery Lair",
                troops = new List<TroopStack>
                {
                    new() { troopId = "void_militia", count = 8 },
                    new() { troopId = "voidovan_cattle_rustler", count = 3 }
                }
            };
            Begin(player, enemy, true, StolenItemQuestController.ButterChiefId, onFinished);
        }

        public void Begin(BattleForce player, BattleForce enemy, bool captureLord, string lordId, Action<BattleOutcome> onFinished)
        {
            _onFinished = onFinished;
            _active = true;
            EnsureUi();
            _canvas.gameObject.SetActive(true);
            GameState.Instance.Battle.Begin(player, enemy, captureLord, lordId);
            _title.text = $"{player.name} vs {enemy.name}";
            _log.text = "Battle joined. Bangkok Kuo steadies her breath.";
            ShowCurrentDecision();
        }

        void EnsureUi()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("BattleCanvas", 40);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.06f, 0.07f, 0.09f, 0.97f));

            _title = UiFactory.Label(root, "Title", "Battle", 36, TextAnchor.UpperCenter, new Color(0.93f, 0.86f, 0.7f));
            var tr = _title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.05f, 0.9f);
            tr.anchorMax = new Vector2(0.95f, 0.98f);

            _prompt = UiFactory.Label(root, "Prompt", "", 28, TextAnchor.UpperLeft, Color.white);
            var pr = _prompt.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.06f, 0.68f);
            pr.anchorMax = new Vector2(0.94f, 0.88f);

            _kuo = UiFactory.Label(root, "Kuo", "", 22, TextAnchor.UpperLeft, new Color(0.7f, 0.82f, 0.9f));
            var kr = _kuo.GetComponent<RectTransform>();
            kr.anchorMin = new Vector2(0.06f, 0.56f);
            kr.anchorMax = new Vector2(0.94f, 0.68f);

            _optionsRoot = UiFactory.Panel(root, "Options", new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.54f), new Color(0, 0, 0, 0.2f));

            _log = UiFactory.Label(root, "Log", "", 20, TextAnchor.LowerLeft, new Color(0.7f, 0.72f, 0.68f));
            var lr = _log.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.06f, 0.04f);
            lr.anchorMax = new Vector2(0.94f, 0.20f);
        }

        void ShowCurrentDecision()
        {
            foreach (Transform child in _optionsRoot)
                Destroy(child.gameObject);

            var battle = GameState.Instance.Battle;
            if (battle.Phase == BattlePhase.Resolve || battle.CurrentDecision() == null)
            {
                Finish();
                return;
            }

            var d = battle.CurrentDecision();
            _prompt.text = $"[{d.targetCategory}] {d.prompt}";
            _kuo.text = d.sunTzuAside ?? "";

            for (var i = 0; i < d.options.Length; i++)
            {
                var order = d.options[i];
                var top = 0.98f - i * 0.24f;
                var bottom = top - 0.2f;
                UiFactory.Button(_optionsRoot, "Opt" + i, order.ToString(), new Vector2(0f, bottom), new Vector2(1f, top), () =>
                {
                    battle.ApplyOrder(order, out var beat);
                    _log.text = beat + "\n" + _log.text;
                    ShowCurrentDecision();
                });
            }
        }

        void Finish()
        {
            if (!_active) return;
            _active = false;
            var outcome = GameState.Instance.Battle.Resolve(GameState.Instance.Rng);
            _prompt.text = outcome.playerVictory ? "Victory" : "Defeat";
            _kuo.text = outcome.summary;
            foreach (Transform child in _optionsRoot)
                Destroy(child.gameObject);

            UiFactory.Button(_optionsRoot, "Continue", "Continue", new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.65f), () =>
            {
                _canvas.gameObject.SetActive(false);
                _onFinished?.Invoke(outcome);
            });
        }
    }
}
