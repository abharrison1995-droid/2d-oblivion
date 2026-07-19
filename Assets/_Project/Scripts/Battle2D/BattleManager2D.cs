using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia.Battle2D
{
    public class BattleManager2D : MonoBehaviour
    {
        public static BattleManager2D Instance { get; private set; }

        public bool IsBattleOver { get; private set; }

        Action<BattleOutcome> _onFinished;
        BattleForce _playerForce;
        BattleForce _enemyForce;

        int _playerCasualties;
        int _enemyCasualties;
        float _scaleFactor = 1f;

        GameObject _environmentRoot;
        Camera _battleCamera;
        Text _uiLog;

        public void Begin(BattleForce player, BattleForce enemy, Action<BattleOutcome> onFinished)
        {
            Instance = this;
            _onFinished = onFinished;
            _playerForce = player;
            _enemyForce = enemy;
            IsBattleOver = false;
            _playerCasualties = 0;
            _enemyCasualties = 0;

            SetupEnvironment();
            SpawnArmies();
        }

        void SetupEnvironment()
        {
            _environmentRoot = new GameObject("BattleEnvironment2D");
            
            // Battle Camera
            var camGo = new GameObject("BattleCamera");
            camGo.transform.SetParent(_environmentRoot.transform);
            camGo.transform.position = new Vector3(0, 0, -10);
            _battleCamera = camGo.AddComponent<Camera>();
            _battleCamera.orthographic = true;
            _battleCamera.orthographicSize = 10f;
            _battleCamera.clearFlags = CameraClearFlags.SolidColor;
            _battleCamera.backgroundColor = new Color(0.2f, 0.3f, 0.2f);
            camGo.AddComponent<AudioListener>(); // Ensure we have one

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(_environmentRoot.transform);
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            var bgTex = Resources.Load<Texture2D>("Battle2D/battle_bg");
            if (bgTex != null)
            {
                bgSr.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
                // Scale it up massively to cover the screen
                bgGo.transform.localScale = new Vector3(10f, 10f, 1f);
            }

            // Simple UI
            var canvasGo = UiFactory.CreateCanvas("Battle2DUICanvas", 50);
            canvasGo.transform.SetParent(_environmentRoot.transform);
            var root = UiFactory.Panel(canvasGo.transform, "Root", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
            _uiLog = UiFactory.Label(root, "Log", "Battle Started!", 24, TextAnchor.UpperCenter, Color.white);
            var rt = _uiLog.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.9f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void SpawnArmies()
        {
            int totalPlayer = BattleUI.ForceCount(_playerForce);
            int totalEnemy = BattleUI.ForceCount(_enemyForce);
            int maxTroops = Mathf.Max(totalPlayer, totalEnemy);

            // Scale down if battles are too large
            if (maxTroops > 30)
            {
                _scaleFactor = maxTroops / 30f;
            }

            // Spawn Player Hero
            SpawnHero();

            // Spawn Allied Troops
            foreach (var t in _playerForce.troops)
            {
                int spawnCount = Mathf.Max(1, Mathf.RoundToInt(t.count / _scaleFactor));
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnTroop(t.troopId, true, new Vector2(UnityEngine.Random.Range(-8f, -2f), UnityEngine.Random.Range(-5f, 5f)));
                }
            }

            // Spawn Enemy Troops
            foreach (var t in _enemyForce.troops)
            {
                int spawnCount = Mathf.Max(1, Mathf.RoundToInt(t.count / _scaleFactor));
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnTroop(t.troopId, false, new Vector2(UnityEngine.Random.Range(2f, 8f), UnityEngine.Random.Range(-5f, 5f)));
                }
            }
        }

        void SpawnHero()
        {
            var go = new GameObject("Hero");
            go.transform.SetParent(_environmentRoot.transform);
            go.transform.position = new Vector3(-5, 0, 0);
            
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = Resources.Load<Texture2D>("Battle2D/hero");
            if (tex != null)
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            
            var health = go.AddComponent<HealthEntity>();
            health.maxHp = 200f;
            health.isPlayerSide = true;
            health.troopValue = 0; // Hero doesn't count towards troop casualties directly in the same way
            health.onDeath += () => OnEntityDeath(health);

            go.AddComponent<HeroController>();
        }

        void SpawnTroop(string troopId, bool isPlayerSide, Vector2 position)
        {
            var go = new GameObject(isPlayerSide ? $"Ally_{troopId}" : $"Enemy_{troopId}");
            go.transform.SetParent(_environmentRoot.transform);
            go.transform.position = position;
            
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = Resources.Load<Texture2D>(isPlayerSide ? "Battle2D/soldier" : "Battle2D/bandit");
            if (tex != null)
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            
            var health = go.AddComponent<HealthEntity>();
            health.maxHp = 50f;
            health.isPlayerSide = isPlayerSide;
            health.troopId = troopId;
            health.troopValue = Mathf.RoundToInt(1 * _scaleFactor); // The number of actual troops this sprite represents
            health.onDeath += () => OnEntityDeath(health);

            go.AddComponent<TroopAIController>();
        }

        void OnEntityDeath(HealthEntity entity)
        {
            if (IsBattleOver) return;

            if (entity.isPlayerSide)
            {
                _playerCasualties += entity.troopValue;
            }
            else
            {
                _enemyCasualties += entity.troopValue;
            }

            CheckWinCondition();
        }

        void CheckWinCondition()
        {
            bool hasPlayerAlive = false;
            bool hasEnemyAlive = false;

            var allEntities = FindObjectsOfType<HealthEntity>();
            foreach (var e in allEntities)
            {
                if (e.currentHp > 0)
                {
                    if (e.isPlayerSide) hasPlayerAlive = true;
                    else hasEnemyAlive = true;
                }
            }

            if (!hasPlayerAlive || !hasEnemyAlive)
            {
                IsBattleOver = true;
                EndBattle(hasPlayerAlive);
            }
        }

        void EndBattle(bool playerWon)
        {
            _uiLog.text = playerWon ? "VICTORY!" : "DEFEAT!";
            
            var outcome = new BattleOutcome
            {
                playerVictory = playerWon,
                summary = playerWon ? $"You won the skirmish! Lost {_playerCasualties} men." : $"Your force was routed. Lost {_playerCasualties} men."
            };

            // Deduct casualties from GameState Party
            if (_playerCasualties > 0)
            {
                GameState.Instance.Party.RemoveMen(_playerCasualties);
            }

            // Wait a few seconds then cleanup
            // In a C# coroutine or just Invoke we need to pass the outcome.
            // Since Invoke takes a string method name with no params, we'll store outcome.
            _pendingOutcome = outcome;
            Invoke(nameof(Cleanup), 3f);
        }

        BattleOutcome _pendingOutcome;

        void Cleanup()
        {
            Destroy(_environmentRoot);
            _onFinished?.Invoke(_pendingOutcome);
        }
    }
}
