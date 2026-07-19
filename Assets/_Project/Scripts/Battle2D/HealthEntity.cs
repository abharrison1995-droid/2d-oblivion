using System;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia.Battle2D
{
    public class HealthEntity : MonoBehaviour
    {
        public float maxHp = 100f;
        public float currentHp;
        public Action onDeath;
        public Action<float> onDamageTaken;

        public bool isBlocking;
        public float blockDamageMultiplier = 0.25f;

        public int troopValue = 1; // For tracking casualties (e.g. 1 sprite = 5 troops)
        public string troopId;
        public bool isPlayerSide;

        SpriteRenderer _sprite;
        Color _originalColor;
        float _flashTimer;

        void Awake()
        {
            currentHp = maxHp;
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite != null)
                _originalColor = _sprite.color;
        }

        public void TakeDamage(float amount)
        {
            if (currentHp <= 0) return;

            if (isBlocking)
                amount *= blockDamageMultiplier;

            currentHp -= amount;
            onDamageTaken?.Invoke(amount);

            if (_sprite != null)
            {
                _sprite.color = Color.red;
                _flashTimer = 0.15f;
            }

            if (currentHp <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            if (onDeath != null)
                onDeath.Invoke();
            
            // Simple death visual (fade/shrink or just destroy)
            Destroy(gameObject);
        }

        void Update()
        {
            if (_flashTimer > 0)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0 && _sprite != null)
                {
                    _sprite.color = _originalColor;
                }
            }
        }
    }
}
