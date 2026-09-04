using System;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Player;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 적 기체가 발사하는 탄환 컴포넌트입니다.
    /// 플레이어를 향해 직선 비행(기본 15~20 units/sec)하며, 화면 하단 이탈 또는 플레이어 충돌 시 풀로 회수됩니다.
    /// IDamageable 인터페이스를 통해 플레이어와의 결합도를 디커플링합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBullet : MonoBehaviour
    {
        [Header("Bullet Movement Settings")]
        [Tooltip("적 탄환 비행 속도 (units/sec)")]
        [SerializeField] private float _speed = 16.0f;

        [Tooltip("탄환 기본 데미지")]
        [SerializeField] private int _damage = 1;

        [Header("References")]
        [Tooltip("화면 하단 이탈 감지용 PlayAreaManager")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        private Vector2 _direction = Vector2.down;
        private Action<EnemyBullet> _onDeactivatedCallback;

        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0.1f, value);
        }

        public int Damage => _damage;
        public Vector2 Direction => _direction;
        public bool IsActive => gameObject.activeSelf;

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        /// <summary>
        /// 탄환 비행 방향, 속도, 풀 회수 콜백을 초기화합니다.
        /// </summary>
        public void Initialize(Vector2 direction, float speed, Action<EnemyBullet> onDeactivatedCallback, PlayAreaManager playAreaManager = null)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            _speed = speed > 0f ? speed : 16.0f;
            _onDeactivatedCallback = onDeactivatedCallback;
            if (playAreaManager != null)
            {
                _playAreaManager = playAreaManager;
            }

            // 진행 방향으로 탄환 2D 회전 정렬
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Start()
        {
            if (_playAreaManager == null && Camera.main != null)
            {
                _playAreaManager = Camera.main.GetComponent<PlayAreaManager>();
            }
        }

        private void Update()
        {
            Move(Time.deltaTime);
        }

        /// <summary>
        /// 진행 방향으로 이동하고 화면 하단/외곽 경계를 검사합니다.
        /// </summary>
        public void Move(float deltaTime)
        {
            Vector3 delta = (Vector3)(_direction * _speed * deltaTime);
            transform.position += delta;

            CheckBoundary();
        }

        private void CheckBoundary()
        {
            float minY = _playAreaManager != null ? _playAreaManager.MinY - 1.0f : -10.5f;
            float minX = _playAreaManager != null ? _playAreaManager.MinX - 2.0f : -8.0f;
            float maxX = _playAreaManager != null ? _playAreaManager.MaxX + 2.0f : 8.0f;
            float maxY = _playAreaManager != null ? _playAreaManager.MaxY + 2.0f : 12.0f;

            Vector3 pos = transform.position;
            if (pos.y < minY || pos.y > maxY || pos.x < minX || pos.x > maxX)
            {
                ReturnToPool();
            }
        }

        /// <summary>
        /// 탄환을 비활성화하고 풀 콜백에 반환합니다.
        /// </summary>
        public void ReturnToPool()
        {
            if (!gameObject.activeSelf)
            {
                return;
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

            // 플레이어와 충돌 시 IDamageable 인터페이스를 통한 피격 처리 및 풀 회수
            if (collision.CompareTag("Player") || collision.name.Contains("Player"))
            {
                IDamageable damageable = collision.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage);
                }
                ReturnToPool();
                return;
            }

            // 하단/외곽 경계 충돌 시 풀 회수
            if (collision.CompareTag("Boundary") || collision.gameObject.name == "BottomBorder")
            {
                ReturnToPool();
            }
        }
    }
}
