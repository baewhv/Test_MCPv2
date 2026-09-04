using System;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Enemy;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 플레이어의 단일 발사체 탄환 컴포넌트입니다.
    /// 수직 상향 이동하며 화면 최상단(PlayAreaManager.MaxY) 이탈 또는 적 충돌 시 오브젝트 풀로 회수됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerBullet : MonoBehaviour
    {
        [Header("Bullet Movement Settings")]
        [Tooltip("탄환 수직 상향 이동 속도 (400 px/sec 기준 월드 환산치 약 27.7 units/sec)")]
        [SerializeField] private float _speed = 27.7f;

        [Header("Damage Settings")]
        [Tooltip("탄환 기본 데미지")]
        [SerializeField] private int _damage = 1;

        [Header("References")]
        [Tooltip("화면 상단 이탈 감지를 위한 PlayAreaManager")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        private Action<PlayerBullet> _onDeactivatedCallback;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public int Damage => _damage;

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

        private void Update()
        {
            Move(Time.deltaTime);
        }

        /// <summary>
        /// 수직 상향 이동을 수행하고 화면 상단 경계 이탈을 검사합니다.
        /// </summary>
        public void Move(float deltaTime)
        {
            Vector3 pos = transform.position;
            pos.y += _speed * deltaTime;
            transform.position = pos;

            CheckBoundary();
        }

        /// <summary>
        /// 화면 상단 경계(PlayAreaManager.MaxY)를 초과했는지 검사하여 풀에 반환합니다.
        /// </summary>
        private void CheckBoundary()
        {
            if (_playAreaManager != null)
            {
                if (transform.position.y > _playAreaManager.MaxY)
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
            if (collision.CompareTag("Boundary") || collision.gameObject.name == "TopBorder")
            {
                ReturnToPool();
                return;
            }

            // 적 충돌 판정 시 데미지 부여 및 풀 반환
            if (collision.CompareTag("Enemy") || collision.name.Contains("Enemy"))
            {
                EnemyBase enemy = collision.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
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

            if (other.CompareTag("Boundary") || other.gameObject.name == "TopBorder")
            {
                ReturnToPool();
                return;
            }

            if (other.CompareTag("Enemy") || other.name.Contains("Enemy"))
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
                }
                ReturnToPool();
            }
        }
    }
}
