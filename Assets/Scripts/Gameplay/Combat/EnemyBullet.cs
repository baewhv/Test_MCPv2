using System;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Player;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 적 기체가 발사하는 하향 조준 탄환 컴포넌트입니다.
    /// Rigidbody2D 물리 이동을 기반으로 지정된 각도로 이동하며 플레이어 기체와 충돌 시 데미지를 입히고 화면 외곽 경계 이탈 시 풀로 반환됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyBullet : MonoBehaviour
    {
        [Header("Bullet Movement Settings")]
        [Tooltip("적 탄환 이동 속도 (약 16.0 units/sec)")]
        [SerializeField] private float _speed = 16.0f;

        [Header("Damage Settings")]
        [Tooltip("탄환 기본 데미지")]
        [SerializeField] private int _damage = 1;

        [Header("Physics & Collider Settings")]
        [Tooltip("물리 이동을 제어하는 Rigidbody2D")]
        [SerializeField] private Rigidbody2D _rigidbody2D;

        [Tooltip("충돌 감지용 BoxCollider2D")]
        [SerializeField] private BoxCollider2D _boxCollider2D;

        [Header("References")]
        [Tooltip("화면 하단/외곽 이탈 감지용 PlayAreaManager")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        private Vector2 _direction = Vector2.down;
        private Action<EnemyBullet> _onDeactivatedCallback;

        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                SetupComponents();
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.linearVelocity = _direction * _speed;
                }
            }
        }

        public int Damage => _damage;
        public Vector2 Direction => _direction;
        public Rigidbody2D Rigidbody2D
        {
            get
            {
                SetupComponents();
                return _rigidbody2D;
            }
        }
        public BoxCollider2D BoxCollider2D
        {
            get
            {
                SetupComponents();
                return _boxCollider2D;
            }
        }

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        public bool IsActive => gameObject.activeSelf;

        /// <summary>
        /// 발사 방향, 속도 및 풀 반환 콜백을 초기화합니다.
        /// </summary>
        public void Initialize(Vector2 direction, float speed, Action<EnemyBullet> onDeactivatedCallback, PlayAreaManager playAreaManager = null)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            _speed = speed;
            _onDeactivatedCallback = onDeactivatedCallback;
            if (playAreaManager != null)
            {
                _playAreaManager = playAreaManager;
            }

            SetupComponents();

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _direction * _speed;
            }

            // 진행 방향으로 탄환 2D 회전 정렬
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Awake()
        {
            SetupComponents();
        }

        private void OnEnable()
        {
            SetupComponents();
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _direction * _speed;
            }
        }

        public void SetupComponents()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }
            if (_rigidbody2D != null)
            {
                _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody2D.gravityScale = 0f;
                _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _rigidbody2D.freezeRotation = true;
            }

            if (_boxCollider2D == null)
            {
                _boxCollider2D = GetComponent<BoxCollider2D>();
            }
            if (_boxCollider2D != null)
            {
                _boxCollider2D.isTrigger = true;
                _boxCollider2D.size = new Vector2(1.0f, 1.0f);
            }
        }

        private void Start()
        {
            if (_playAreaManager == null)
            {
                _playAreaManager = PlayAreaManager.Instance;
            }
            if (_playAreaManager == null && Camera.main != null)
            {
                _playAreaManager = Camera.main.GetComponent<PlayAreaManager>();
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _direction * _speed;
            }
            CheckBoundary();
        }

        private void Update()
        {
            CheckBoundary();
        }

        /// <summary>
        /// 지정된 방향과 속도로 이동하고 화면 하단/외곽 경계 이탈을 검사합니다 (단위 테스트 및 수동 이동 호환).
        /// </summary>
        public void Move(float deltaTime)
        {
            Vector3 pos = transform.position;
            pos.x += _direction.x * _speed * deltaTime;
            pos.y += _direction.y * _speed * deltaTime;
            transform.position = pos;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.position = pos;
                _rigidbody2D.linearVelocity = _direction * _speed;
            }

            CheckBoundary();
        }

        private void CheckBoundary()
        {
            if (_playAreaManager != null)
            {
                if (_playAreaManager.IsOutOfBounds(transform.position, 1.5f))
                {
                    ReturnToPool();
                }
            }
            else
            {
                // Fallback: 기본 외곽 범위 검사
                if (transform.position.y < -12.0f || transform.position.y > 12.0f ||
                    transform.position.x < -10.0f || transform.position.x > 10.0f)
                {
                    ReturnToPool();
                }
            }
        }

        /// <summary>
        /// 탄환을 비활성화하고 등록된 풀 콜백에 반환합니다.
        /// </summary>
        public void ReturnToPool()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            gameObject.SetActive(false);
            _onDeactivatedCallback?.Invoke(this);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null || !gameObject.activeSelf)
            {
                return;
            }

            // 외곽 경계 충돌 시 회수
            if (collision.CompareTag("Boundary") || collision.gameObject.name.Contains("Border") || collision.name.Contains("Boundary"))
            {
                ReturnToPool();
                return;
            }

            // 플레이어 피격 처리
            if (collision.CompareTag("Player") || collision.name.Contains("Player"))
            {
                IDamageable damageable = collision.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage);
                }
                else
                {
                    PlayerHealth health = collision.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(_damage);
                    }
                }
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !gameObject.activeSelf)
            {
                return;
            }

            if (other.CompareTag("Boundary") || other.gameObject.name.Contains("Border") || other.name.Contains("Boundary"))
            {
                ReturnToPool();
                return;
            }

            if (other.CompareTag("Player") || other.name.Contains("Player"))
            {
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage);
                }
                else
                {
                    PlayerHealth health = other.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(_damage);
                    }
                }
                ReturnToPool();
            }
        }
    }
}
