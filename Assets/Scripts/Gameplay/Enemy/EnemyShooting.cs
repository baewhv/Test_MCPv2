using System;
using UnityEngine;
using Galaga.Gameplay.Combat;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 적 기체가 급강하(Diving) 중 플레이어의 현재 위치를 조준하여 탄환을 발사하는 조준 사격 컴포넌트입니다.
    /// 베지어 궤적의 중간 구간(t = 0.3 ~ 0.6)에서 지정된 발수만큼 탄환을 사격합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyShooting : MonoBehaviour
    {
        [Header("Shooting Settings")]
        [Tooltip("급강하 중 발사할 최대 탄환 수")]
        [SerializeField] private int _maxShotsPerDive = 1;

        [Tooltip("사격 개시 베지어 진행도 (0.0 ~ 1.0)")]
        [SerializeField] private float _fireProgressMin = 0.3f;

        [Tooltip("사격 종료 베지어 진행도 (0.0 ~ 1.0)")]
        [SerializeField] private float _fireProgressMax = 0.6f;

        [Tooltip("탄환 비행 속도 (units/sec)")]
        [SerializeField] private float _bulletSpeed = 16.0f;

        [Tooltip("사격 쿨다운 간격(초)")]
        [SerializeField] private float _shotCooldown = 0.2f;

        [Header("References")]
        [Tooltip("적 기체 베이스 컴포넌트")]
        [SerializeField] private EnemyBase _enemyBase;

        [Tooltip("플레이어 위치 Transform")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("탄환 풀 매니저 참조 (미할당 시 EnemyBulletPool.Instance 사용)")]
        [SerializeField] private EnemyBulletPool _bulletPool;

        private int _shotsFiredThisDive = 0;
        private float _lastShotTime = -10f;

        public event Action<Vector3, Vector2> OnShotFired;

        public int MaxShotsPerDive
        {
            get => _maxShotsPerDive;
            set => _maxShotsPerDive = Mathf.Max(0, value);
        }

        public float FireProgressMin
        {
            get => _fireProgressMin;
            set => _fireProgressMin = Mathf.Clamp01(value);
        }

        public float FireProgressMax
        {
            get => _fireProgressMax;
            set => _fireProgressMax = Mathf.Clamp01(value);
        }

        public float BulletSpeed
        {
            get => _bulletSpeed;
            set => _bulletSpeed = Mathf.Max(1f, value);
        }

        public Transform PlayerTransform
        {
            get => _playerTransform;
            set => _playerTransform = value;
        }

        public EnemyBulletPool BulletPool
        {
            get => _bulletPool;
            set => _bulletPool = value;
        }

        public int ShotsFiredThisDive => _shotsFiredThisDive;

        private void Awake()
        {
            if (_enemyBase == null)
            {
                _enemyBase = GetComponent<EnemyBase>();
            }
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }

            if (_bulletPool == null)
            {
                _bulletPool = EnemyBulletPool.Instance;
            }
        }

        private void OnEnable()
        {
            if (_enemyBase != null)
            {
                _enemyBase.OnStateChanged += HandleStateChanged;
                if (_enemyBase.PathFollower != null)
                {
                    _enemyBase.PathFollower.OnProgressChanged += HandleProgressChanged;
                }
            }
        }

        private void OnDisable()
        {
            if (_enemyBase != null)
            {
                _enemyBase.OnStateChanged -= HandleStateChanged;
                if (_enemyBase.PathFollower != null)
                {
                    _enemyBase.PathFollower.OnProgressChanged -= HandleProgressChanged;
                }
            }
        }

        private void HandleStateChanged(EnemyBase enemy, EnemyState newState)
        {
            if (newState == EnemyState.Diving)
            {
                _shotsFiredThisDive = 0;
                _lastShotTime = -10f;
            }
        }

        private void HandleProgressChanged(float progress)
        {
            if (_enemyBase == null || _enemyBase.CurrentState != EnemyState.Diving || _enemyBase.IsDead)
            {
                return;
            }

            if (CanShoot(progress, _shotsFiredThisDive, _maxShotsPerDive, _fireProgressMin, _fireProgressMax))
            {
                if (Time.time - _lastShotTime >= _shotCooldown)
                {
                    TryFireAtPlayer();
                }
            }
        }

        /// <summary>
        /// 플레이어를 향해 조준 탄환을 1발 발사합니다.
        /// </summary>
        public bool TryFireAtPlayer()
        {
            if (_bulletPool == null)
            {
                _bulletPool = EnemyBulletPool.Instance;
                if (_bulletPool == null)
                {
                    return false;
                }
            }

            Vector3 spawnPos = transform.position;
            Vector2 aimDirection = Vector2.down;

            if (_playerTransform != null)
            {
                Vector3 playerPos = _playerTransform.position;
                Vector2 diff = (playerPos - spawnPos);
                if (diff.sqrMagnitude > 0.0001f)
                {
                    aimDirection = diff.normalized;
                }
            }

            EnemyBullet bullet = _bulletPool.SpawnBullet(spawnPos, aimDirection, _bulletSpeed);
            _shotsFiredThisDive++;
            _lastShotTime = Time.time;

            OnShotFired?.Invoke(spawnPos, aimDirection);
            return bullet != null;
        }

        /// <summary>
        /// 현재 진행도와 발사 횟수에 따라 다이브 중 사격이 가능한지 판정하는 순수 판정 로직입니다 (단위 테스트용).
        /// </summary>
        public static bool CanShoot(float progress, int currentShots, int maxShots, float minT = 0.3f, float maxT = 0.6f)
        {
            if (currentShots >= maxShots)
            {
                return false;
            }

            return progress >= minT && progress <= maxT;
        }
    }
}
