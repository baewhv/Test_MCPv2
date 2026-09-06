using System;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 적 기체가 플레이어를 향해 발사하는 단일 탄환 컴포넌트입니다.
    /// Rigidbody2D 물리 이동을 기반으로 지정된 방향과 속도로 이동하며 플레이어 충돌 또는 화면 외곽 이탈 시 풀로 회수됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyBullet : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("탄환 기본 속도 (약 16 units/sec)")]
        [SerializeField] private float _speed = 16f;

        [Tooltip("탄환 이동 방향 단위 벡터")]
        [SerializeField] private Vector2 _direction = Vector2.down;

        [Header("Damage Settings")]
        [Tooltip("탄환 기본 데미지")]
        [SerializeField] private int _damage = 1;

        [Header("Physics & Collider Settings")]
        [Tooltip("물리 이동을 제어하는 Rigidbody2D")]
        [SerializeField] private Rigidbody2D _rigidbody2D;

        [Tooltip("충돌 감지용 BoxCollider2D")]
        [SerializeField] private BoxCollider2D _boxCollider2D;

        [Header("References")]
        [Tooltip("화면 이탈 감지를 위한 PlayAreaManager")]
        [SerializeField] private PlayAreaManager _playAreaManager;

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

        private void SetupComponents()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }
            if (_boxCollider2D == null)
            {
                _boxCollider2D = GetComponent<BoxCollider2D>();
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody2D.gravityScale = 0f;
                _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _rigidbody2D.freezeRotation = true;
                _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (_boxCollider2D != null)
            {
                _boxCollider2D.isTrigger = true;
                _boxCollider2D.size = new Vector2(1f, 1f);
            }
        }

        private void Update()
        {
            CheckBoundary();
        }

        private void FixedUpdate()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _direction * _speed;
            }
        }

        /// <summary>
        /// 델타 타임을 기반으로 탄환의 이동 및 경계 검사를 수행합니다 (단위 테스트 및 비물리 환경 지원).
        /// </summary>
        /// <param name="deltaTime">경과 시간 (초)</param>
        public void Move(float deltaTime)
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.MovePosition(_rigidbody2D.position + _direction * (_speed * deltaTime));
            }
            else
            {
                transform.position += (Vector3)(_direction * (_speed * deltaTime));
            }
            CheckBoundary();
        }

        private void CheckBoundary()
        {
            float minX = -8f;
            float maxX = 8f;
            float minY = -9.5f;
            float maxY = 10.5f;

            if (_playAreaManager != null)
            {
                minX = _playAreaManager.MinX - 1f;
                maxX = _playAreaManager.MaxX + 1f;
                minY = _playAreaManager.MinY - 1f;
                maxY = _playAreaManager.MaxY + 1f;
            }

            Vector3 pos = transform.position;
            if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
            {
                ReturnToPool();
            }
        }

        /// <summary>
        /// 탄환을 비활성화하고 오브젝트 풀로 반환합니다.
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

            if (_onDeactivatedCallback != null)
            {
                Action<EnemyBullet> callback = _onDeactivatedCallback;
                _onDeactivatedCallback = null;
                callback.Invoke(this);
            }
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

            // 피격 대상(IDamageable) 또는 플레이어 충돌 판정 시 데미지 부여 및 풀 반환
            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                ReturnToPool();
                return;
            }

            if (collision.CompareTag("Player") || collision.name.Contains("Player"))
            {
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

            // 피격 대상(IDamageable) 또는 플레이어 충돌 판정 시 데미지 부여 및 풀 반환
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                ReturnToPool();
                return;
            }

            if (other.CompareTag("Player") || other.name.Contains("Player"))
            {
                ReturnToPool();
            }
        }
    }
}
