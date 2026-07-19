using UnityEngine;

namespace Voidovia.Battle2D
{
    [RequireComponent(typeof(Rigidbody2D), typeof(HealthEntity))]
    public class HeroController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float attackRange = 1.5f;
        public float attackCooldown = 0.5f;
        public float attackDamage = 25f;

        Rigidbody2D _rb;
        HealthEntity _health;
        Camera _mainCamera;
        
        float _lastAttackTime;
        Vector2 _moveInput;
        Vector2 _lookDir = Vector2.right;

        // Visuals
        Transform _swordPivot;
        SpriteRenderer _swordSprite;
        float _swingTimer;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f; // Top-down
            _rb.freezeRotation = true;
            _health = GetComponent<HealthEntity>();
            _mainCamera = Camera.main;

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
            _swordSprite.color = Color.white;
            // Generate a simple white rectangle for the sword if no sprite is available
            _swordSprite.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            swordGo.transform.localScale = new Vector3(10f, 2f, 1f);
            
            _swordPivot.gameObject.SetActive(false);
        }

        void Update()
        {
            if (BattleManager2D.Instance != null && BattleManager2D.Instance.IsBattleOver)
            {
                _moveInput = Vector2.zero;
                return;
            }

            // Input
            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");

            // Look direction (mouse)
            if (_mainCamera != null)
            {
                Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                _lookDir = (mousePos - transform.position).normalized;
                
                // Flip sprite based on look direction if needed
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr != _swordSprite)
                {
                    sr.flipX = _lookDir.x < 0;
                }
            }

            // Block
            _health.isBlocking = Input.GetMouseButton(1); // Right click

            // Attack
            if (Input.GetMouseButtonDown(0) && Time.time >= _lastAttackTime + attackCooldown && !_health.isBlocking)
            {
                Attack();
            }

            // Swing Animation
            if (_swingTimer > 0)
            {
                _swingTimer -= Time.deltaTime;
                float t = 1f - (_swingTimer / 0.15f);
                // Rotate pivot from -45 to +45
                float angle = Mathf.Lerp(-45f, 45f, t);
                float baseAngle = Mathf.Atan2(_lookDir.y, _lookDir.x) * Mathf.Rad2Deg;
                _swordPivot.rotation = Quaternion.Euler(0, 0, baseAngle + angle);
                
                if (_swingTimer <= 0)
                {
                    _swordPivot.gameObject.SetActive(false);
                }
            }
        }

        void FixedUpdate()
        {
            if (_health.isBlocking)
            {
                _rb.velocity = _moveInput.normalized * (moveSpeed * 0.5f); // Move slower while blocking
            }
            else
            {
                _rb.velocity = _moveInput.normalized * moveSpeed;
            }
        }

        void Attack()
        {
            _lastAttackTime = Time.time;
            
            // Visuals
            _swordPivot.gameObject.SetActive(true);
            _swingTimer = 0.15f;

            // Hit detection (Cone or Circle overlap)
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                var targetHealth = hit.GetComponent<HealthEntity>();
                if (targetHealth != null && !targetHealth.isPlayerSide)
                {
                    // Check if within angle
                    Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
                    if (Vector2.Angle(_lookDir, dirToTarget) < 60f)
                    {
                        targetHealth.TakeDamage(attackDamage);
                        // Optional: Apply knockback
                        var targetRb = hit.GetComponent<Rigidbody2D>();
                        if (targetRb != null)
                        {
                            targetRb.AddForce(dirToTarget * 150f, ForceMode2D.Impulse);
                        }
                    }
                }
            }
        }
    }
}
