using System.Collections.Generic;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 적 탄환의 인스턴스를 관리하고 재사용하는 오브젝트 풀 매니저 컴포넌트입니다.
    /// GC 생성을 최소화하고 화면 내 적 발사체를 안전하게 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBulletPool : MonoBehaviour
    {
        public static EnemyBulletPool Instance { get; private set; }

        [Header("Pool Configuration")]
        [Tooltip("적 탄환 완제품 프리팹 (PF_EnemyBullet)")]
        [SerializeField] private GameObject _bulletPrefab;

        [Tooltip("초기 생성 탄환 수량")]
        [SerializeField] private int _initialPoolSize = 16;

        [Header("References")]
        [Tooltip("PlayAreaManager 참조")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        private readonly List<EnemyBullet> _bulletPool = new List<EnemyBullet>();
        private readonly HashSet<EnemyBullet> _activeBullets = new HashSet<EnemyBullet>();
        private Transform _poolRoot;

        public GameObject BulletPrefab
        {
            get => _bulletPrefab;
            set => _bulletPrefab = value;
        }

        public int InitialPoolSize
        {
            get => _initialPoolSize;
            set => _initialPoolSize = Mathf.Max(1, value);
        }

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        public int ActiveBulletCount => _activeBullets.Count;
        public int TotalPoolCount => _bulletPool.Count;

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
        /// 오브젝트 풀을 초기화하고 사전 인스턴스를 생성합니다.
        /// </summary>
        public void InitializePool()
        {
            if (_poolRoot == null)
            {
                _poolRoot = transform;
            }

            if (_bulletPrefab == null)
            {
                return;
            }

            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewBulletInstance();
            }
        }

        private EnemyBullet CreateNewBulletInstance()
        {
            GameObject bulletObj;
            if (_bulletPrefab != null)
            {
                bulletObj = Instantiate(_bulletPrefab, _poolRoot);
            }
            else
            {
                bulletObj = new GameObject("EnemyBullet");
                bulletObj.transform.SetParent(_poolRoot);
            }

            bulletObj.SetActive(false);

            EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
            if (bullet == null)
            {
                bullet = bulletObj.AddComponent<EnemyBullet>();
            }

            bullet.Initialize(Vector2.down, 16f, OnBulletDeactivated, _playAreaManager);
            _bulletPool.Add(bullet);
            return bullet;
        }

        /// <summary>
        /// 풀에서 비활성화된 탄환을 가져오거나 새로 생성하여 반환합니다.
        /// </summary>
        public EnemyBullet SpawnBullet(Vector3 position, Vector2 direction, float speed = 16.0f)
        {
            EnemyBullet bullet = GetPooledBullet();
            bullet.transform.position = position;
            bullet.PlayAreaManager = _playAreaManager;
            bullet.Initialize(direction, speed, OnBulletDeactivated, _playAreaManager);
            bullet.gameObject.SetActive(true);

            _activeBullets.Add(bullet);
            return bullet;
        }

        private EnemyBullet GetPooledBullet()
        {
            for (int i = 0; i < _bulletPool.Count; i++)
            {
                if (_bulletPool[i] != null && !_bulletPool[i].gameObject.activeSelf)
                {
                    return _bulletPool[i];
                }
            }

            return CreateNewBulletInstance();
        }

        private void OnBulletDeactivated(EnemyBullet bullet)
        {
            if (bullet != null)
            {
                _activeBullets.Remove(bullet);
            }
        }

        /// <summary>
        /// 모든 활성 탄환을 강제 회수합니다.
        /// </summary>
        public void ClearAllActiveBullets()
        {
            List<EnemyBullet> activeList = new List<EnemyBullet>(_activeBullets);
            for (int i = 0; i < activeList.Count; i++)
            {
                if (activeList[i] != null)
                {
                    activeList[i].ReturnToPool();
                }
            }
            _activeBullets.Clear();
        }
    }
}
