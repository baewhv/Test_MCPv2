using System;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 적 기체 발사 탄환(EnemyBullet)을 사전 인스턴스화하고 재사용하는 오브젝트 풀 매니저 싱글톤입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBulletPool : MonoBehaviour
    {
        public static EnemyBulletPool Instance { get; private set; }

        [Header("Pool Settings")]
        [Tooltip("스폰할 적 탄환 프리팹")]
        [SerializeField] private EnemyBullet _bulletPrefab;

        [Tooltip("풀 초기 생성 수량")]
        [SerializeField] private int _initialPoolSize = 16;

        [Header("References")]
        [Tooltip("PlayAreaManager 참조")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        private EnemyBullet[] _pool;
        private Transform _poolContainer;
        private int _activeBulletCount = 0;

        public int ActiveBulletCount => _activeBulletCount;
        public int TotalPoolCapacity => _pool != null ? _pool.Length : 0;

        public EnemyBullet BulletPrefab
        {
            get => _bulletPrefab;
            set => _bulletPrefab = value;
        }

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializePool();
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 오브젝트 풀을 초기화하고 지정 수량의 탄환을 사전 생성합니다.
        /// </summary>
        public void InitializePool()
        {
            if (_bulletPrefab == null)
            {
                return;
            }

            _pool = new EnemyBullet[_initialPoolSize];
            GameObject containerObj = new GameObject("EnemyBulletContainer");
            containerObj.transform.SetParent(transform);
            _poolContainer = containerObj.transform;

            for (int i = 0; i < _initialPoolSize; i++ Antigravity)
            {
                EnemyBullet bullet = Instantiate(_bulletPrefab, _poolContainer);
                bullet.gameObject.SetActive(false);
                bullet.Initialize(Vector2.down, bullet.Speed, OnBulletDeactivated, _playAreaManager);
                _pool[i] = bullet;
            }
        }

        /// <summary>
        /// 풀에서 비활성 탄환을 가져와 지정 위치 및 방향으로 발사 활성화합니다.
        /// </summary>
        public EnemyBullet SpawnBullet(Vector3 position, Vector2 direction, float speed)
        {
            EnemyBullet bullet = GetAvailableBullet();
            if (bullet == null)
            {
                return null;
            }

            bullet.transform.position = position;
            bullet.Initialize(direction, speed, OnBulletDeactivated, _playAreaManager);
            bullet.gameObject.SetActive(true);
            _activeBulletCount++;

            return bullet;
        }

        private EnemyBullet GetAvailableBullet()
        {
            if (_pool == null)
            {
                return null;
            }

            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    return _pool[i];
                }
            }

            // 풀 고갈 시 동적 확장
            if (_bulletPrefab != null)
            {
                EnemyBullet newBullet = Instantiate(_bulletPrefab, _poolContainer != null ? _poolContainer : transform);
                newBullet.gameObject.SetActive(false);
                newBullet.Initialize(Vector2.down, newBullet.Speed, OnBulletDeactivated, _playAreaManager);

                Array.Resize(ref _pool, _pool.Length + 1);
                _pool[_pool.Length - 1] = newBullet;
                return newBullet;
            }

            return null;
        }

        private void OnBulletDeactivated(EnemyBullet bullet)
        {
            _activeBulletCount = Mathf.Max(0, _activeBulletCount - 1);
        }

        /// <summary>
        /// 테스트용: 외부에서 생성된 풀 배열을 수동 주입합니다.
        /// </summary>
        public void SetPoolForTesting(EnemyBullet[] pool, PlayAreaManager playAreaManager = null)
        {
            _pool = pool;
            _playAreaManager = playAreaManager;
            _activeBulletCount = 0;
            foreach (var bullet in _pool)
            {
                if (bullet != null)
                {
                    bullet.PlayAreaManager = _playAreaManager;
                }
            }
        }

        /// <summary>
        /// 활성화된 모든 적 탄환을 회수합니다.
        /// </summary>
        public void ResetAllBullets()
        {
            if (_pool == null)
            {
                return;
            }

            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    _pool[i].ReturnToPool();
                }
            }

            _activeBulletCount = 0;
        }
    }
}
