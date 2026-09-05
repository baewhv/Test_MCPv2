using System;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Enemy;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 플레이어의 단일 발사체 탄환 컴포넌트입니다.
    /// Rigidbody2D 물리 이동을 기반으로 수직 상향 이동하며 화면 최상단(PlayAreaManager.MaxY 또는 10.5u) 이탈 또는 적 충돌 시 오브젝트 풀로 회수됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerBullet : MonoBehaviour
    {
        [Header("Bullet Movement Settings")]
        [Tooltip("탄환 수직 상향 이동 속도 (400 px/sec 기준 월드 환산치 약 27.7 units/sec)")]
        [SerializeField] private float _speed = 27.7f;

        [Header("Damage Settings")]
        [Tooltip("탄환 기본 데미지")]
        [SerializeField] private int _damage = 1;

        [Header("Physics & Collider Settings")]
        [Tooltip("물리 이동을 제어하는 Rigidbody2D")]
        [SerializeField] private Rigidbody2D _rigidbody2D;

        [Tooltip("충돌 감지용 BoxCollider2D")]
        [SerializeField] private BoxCollider2D _boxCollider2D;

        [Header("References")]
        [Tooltip("화면 상단 이탈 감지를 위한 PlayAreaManager")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        private Action<PlayerBullet> _onDeactivatedCallback;

        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                SetupComponents();
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.velocity = Vector2.up * _speed;
                }
            }
        }

        public int Damage => _damage;
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
        /// 오브젝트 풀 반환 콜백 및 플레이 영역 관리자를 초기화합니다.
        /// </summary>
        public void Initialize(Action<PlayerBullet> onDeactivatedCallback, PlayAreaManager playAreaManager = null)
        {
            _onDeactivatedCallback = onDeactivatedCallback;
            if (playAreaManager != null)
            {
                _playAreaManager = playAreaManager;
            }
            SetupComponents();
            if (_rigidbody2D != null)
            {
                _rigidbody2D.velocity = Vector2.up * _speed;
            }
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
                _rigidbody2D.velocity = Vector2.up * _speed;
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
                _rigidbody2D.velocity = Vector2.up * _speed;
            }
            CheckBoundary();
        }

        private void Update()
        {
            CheckBoundary();
        }

        /// <summary>
        /// 수직 상향 이동을 수행하고 화면 상단 경계 이탈을 검사합니다 (단위 테스트 및 수동 이동 호환).
        /// </summary>
        public void Move(float deltaTime)
        {
            Vector3 pos = transform.position;
            pos.y += _speed * deltaTime;
            transform.position = pos;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.position = pos;
                _rigidbody2D.velocity = Vector2.up * _speed;
            }

            CheckBoundary();
        }

        /// <summary>
        /// 화면 상단 경계(PlayAreaManager.MaxY 또는 10.5u)를 초과했는지 검사하여 풀에 반환합니다.
        /// </summary>
        private void CheckBoundary()
        {
            float maxY = (_playAreaManager != null) ? _playAreaManager.MaxY : 10.5f;
            if (transform.position.y > maxY || (_rigidbody2D != null && _rigidbody2D.position.y > maxY))
            {
                ReturnToPool();
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
                _rigidbody2D.velocity = Vector2.zero;
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

            // 상단 경계 충돌체와 접촉 시 풀 반환
            if (collision.CompareTag("Boundary") || collision.gameObject.name == "TopBorder" || collision.name.Contains("Border"))
            {
                ReturnToPool();
                return;
            }

            // 적 충돌 판정 시 데미지 부여 및 풀 반환
            if (collision.CompareTag("Enemy") || collision.name.Contains("Enemy"))
            {
                IDamageable damageable = collision.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage);
                }
                else
                {
                    EnemyBase enemy = collision.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(_damage);
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

            if (other.CompareTag("Boundary") || other.gameObject.name == "TopBorder" || other.name.Contains("Border"))
            {
                ReturnToPool();
                return;
            }

            if (other.CompareTag("Enemy") || other.name.Contains("Enemy"))
            {
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage);
                }
                else
                {
                    EnemyBase enemy = other.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(_damage);
                    }
                }
                ReturnToPool();
            }
        }
    }
}
