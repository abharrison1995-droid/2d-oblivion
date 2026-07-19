using UnityEngine;

namespace Voidovia.Battle2D
{
    [RequireComponent(typeof(Rigidbody2D), typeof(HealthEntity))]
    public class TroopAIController : MonoBehaviour
    {
        public float moveSpeed = 3f;
        public float attackRange = 1.2f;
        public float attackCooldown = 1.5f;
        public float attackDamage = 15f;

        Rigidbody2D _rb;
        HealthEntity _health;
        
        float _lastAttackTime;
        Transform _target;

        // Visuals
        Transform _swordPivot;
        SpriteRenderer _swordSprite;
        float _swingTimer;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _health = GetComponent<HealthEntity>();

            // Add some randomness to speed and cooldown so they don't sync up perfectly
            moveSpeed += Random.Range(-0.5f, 0.5f);
            attackCooldown += Random.Range(-0.2f, 0.2f);

            CreateSwordVisual();
        }

        void CreateSwordVisual()
        {
            var pivotGo = new GameObject("SwordPivot");
            pivotGo.transform.SetParent(transform, false);
            _swordPivot = pivotGo.transform;

            var swordGo = new GameObject("SwordSprite");
            swordGo.transform.SetParent(_swordPivot, false);
            swordGo.transform.localPosition = new Vector3(0.8f, 0, 0);
            _swordSprite = swordGo.AddComponent<SpriteRenderer>();
            _swordSprite.color = Color.gray;
            _swordSprite.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            swordGo.transform.localScale = new Vector3(8f, 1.5f, 1f);
            
            _swordPivot.gameObject.SetActive(false);
        }

        void Update()
        {
            if (BattleManager2D.Instance != null && BattleManager2D.Instance.IsBattleOver)
            {
                _rb.velocity = Vector2.zero;
                return;
            }

            FindTarget();

            if (_target != null)
            {
                float dist = Vector2.Distance(transform.position, _target.position);
                Vector2 dir = (_target.position - transform.position).normalized;

                // Flip sprite
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr != _swordSprite)
                {
                    sr.flipX = dir.x < 0;
                }

                if (dist > attackRange * 0.8f)
                {
                    _rb.velocity = dir * moveSpeed;
                }
                else
                {
                    _rb.velocity = Vector2.zero;
                    if (Time.time >= _lastAttackTime + attackCooldown)
                    {
                        Attack(dir);
                    }
                }
            }
            else
            {
                _rb.velocity = Vector2.zero;
            }

            // Swing Animation
            if (_swingTimer > 0)
            {
                _swingTimer -= Time.deltaTime;
                float t = 1f - (_swingTimer / 0.15f);
                float angle = Mathf.Lerp(-45f, 45f, t);
                
                Vector2 lookDir = _target != null ? (_target.position - transform.position).normalized : Vector2.right;
                float baseAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                _swordPivot.rotation = Quaternion.Euler(0, 0, baseAngle + angle);
                
                if (_swingTimer <= 0)
                {
                    _swordPivot.gameObject.SetActive(false);
                }
            }
        }

        void FindTarget()
        {
            if (_target != null && _target.gameObject.activeInHierarchy) return;

            // Simple O(N) closest target search
            float closestDist = float.MaxValue;
            Transform closest = null;

            var allEntities = FindObjectsOfType<HealthEntity>();
            foreach (var e in allEntities)
            {
                if (e == _health || e.isPlayerSide == _health.isPlayerSide) continue;

                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closest = e.transform;
                }
            }

            _target = closest;
        }

        void Attack(Vector2 dir)
        {
            _lastAttackTime = Time.time;
            
            // Visuals
            _swordPivot.gameObject.SetActive(true);
            _swingTimer = 0.15f;

            if (_target != null)
            {
                var targetHealth = _target.GetComponent<HealthEntity>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(attackDamage);
                    
                    var targetRb = _target.GetComponent<Rigidbody2D>();
                    if (targetRb != null)
                    {
                        targetRb.AddForce(dir * 50f, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }
}
