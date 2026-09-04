using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Galaga.Core;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 플레이어 탄환 발사 및 오브젝트 풀을 관리하는 컴포넌트입니다.
    /// 화면 내 최대 발사 탄환 수(싱글 2발, 듀얼 4발)를 제한하며 발사 간격을 제어합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerShooting : MonoBehaviour
    {
        [Header("Bullet Prefab & Pool Settings")]
        [Tooltip("발사할 플레이어 탄환 프리팹")]
        [SerializeField] private PlayerBullet _bulletPrefab;

        [Tooltip("오브젝트 풀 초기 생성 수량")]
        [SerializeField] private int _initialPoolSize = 10;

        [Header("Firing Constraints")]
        [Tooltip("싱글 파이터 상태에서 화면 내 최대 허용 탄환 수")]
        [SerializeField] private int _singleMaxBullets = 2;

        [Tooltip("듀얼 파이터 상태에서 화면 내 최대 허용 탄환 수")]
        [SerializeField] private int _dualMaxBullets = 4;

        [Tooltip("연사 쿨타임 (초)")]
        [SerializeField] private float _fireCooldown = 0.15f;

        [Tooltip("탄환 발사 Y 오프셋 (기체 중심 기준)")]
        [SerializeField] private float _bulletSpawnOffsetY = 0.5f;

        [Tooltip("듀얼 파이터 좌우 포대 X 오프셋")]
        [SerializeField] private float _dualGunOffsetX = 0.45f;

        [Header("References")]
        [Tooltip("PlayAreaManager 참조")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        [Tooltip("New Input System 발사 액션 참조")]
        [SerializeField] private InputActionReference _attackAction;

        private PlayerBullet[] _bulletPool;
        private int _activeBulletCount = 0;
        private float _lastFireTime = -999f;
        private bool _isDualFighter = false;
        private bool _canShoot = true;

        public int ActiveBulletCount => _activeBulletCount;
        public int MaxBulletsOnScreen => _isDualFighter ? _dualMaxBullets : _singleMaxBullets;
        public int MaxSimultaneousBullets => MaxBulletsOnScreen;
        public float FireCooldown { get => _fireCooldown; set => _fireCooldown = value; }
        public bool IsDualFighter
        {
            get => _isDualFighter;
            set => _isDualFighter = value;
        }

        public bool CanShoot
        {
            get => _canShoot;
            set => _canShoot = value;
        }

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        public PlayerBullet BulletPrefab
        {
            get => _bulletPrefab;
            set => _bulletPrefab = value;
        }

        private void Awake()
        {
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

        private void OnEnable()
        {
            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Enable();
                _attackAction.action.performed += OnAttackAction;
            }
        }

        private void OnDisable()
        {
            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.performed -= OnAttackAction;
                _attackAction.action.Disable();
            }
        }

        private void Update()
        {
            // 액션 바인딩이 없을 때 New Input System Keyboard 직접 폴링 fallback
            if (_attackAction == null || _attackAction.action == null)
            {
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    TryFire();
                }
            }
        }

        private void OnAttackAction(InputAction.CallbackContext context)
        {
            TryFire();
        }

        /// <summary>
        /// 오브젝트 풀을 초기화하고 지정된 수량의 탄환을 사전 인스턴스화합니다.
        /// </summary>
        public void InitializePool()
        {
            if (_bulletPrefab == null)
            {
                return;
            }

            _bulletPool = new PlayerBullet[_initialPoolSize];
            GameObject poolRoot = new GameObject("PlayerBulletPool");
            poolRoot.transform.SetParent(transform);

            for (int i = 0; i < _initialPoolSize; i++)
            {
                PlayerBullet bullet = Instantiate(_bulletPrefab, poolRoot.transform);
                bullet.gameObject.SetActive(false);
                bullet.Initialize(OnBulletDeactivated, _playAreaManager);
                _bulletPool[i] = bullet;
            }
        }

        /// <summary>
        /// 발사 제약 조건(화면 내 최대 탄환 수, 쿨타임, 조작권)을 검사한 후 탄환을 발사합니다.
        /// </summary>
        public bool TryFire()
        {
            if (!_canShoot)
            {
                return false;
            }

            if (Time.time < _lastFireTime + _fireCooldown)
            {
                return false;
            }

            int requiredBullets = _isDualFighter ? 2 : 1;
            if (_activeBulletCount + requiredBullets > MaxBulletsOnScreen)
            {
                return false;
            }

            if (_isDualFighter)
            {
                FireDual();
            }
            else
            {
                FireSingle();
            }

            _lastFireTime = Time.time;
            return true;
        }

        private void FireSingle()
        {
            PlayerBullet bullet = GetPooledBullet();
            if (bullet == null)
            {
                return;
            }

            Vector3 spawnPos = transform.position;
            spawnPos.y += _bulletSpawnOffsetY;

            ActivateBullet(bullet, spawnPos);
        }

        private void FireDual()
        {
            PlayerBullet leftBullet = GetPooledBullet();
            PlayerBullet rightBullet = GetPooledBullet();

            if (leftBullet == null || rightBullet == null)
            {
                if (leftBullet != null) leftBullet.gameObject.SetActive(false);
                if (rightBullet != null) rightBullet.gameObject.SetActive(false);
                return;
            }

            Vector3 leftPos = transform.position;
            leftPos.x -= _dualGunOffsetX;
            leftPos.y += _bulletSpawnOffsetY;

            Vector3 rightPos = transform.position;
            rightPos.x += _dualGunOffsetX;
            rightPos.y += _bulletSpawnOffsetY;

            ActivateBullet(leftBullet, leftPos);
            ActivateBullet(rightBullet, rightPos);
        }

        private void ActivateBullet(PlayerBullet bullet, Vector3 position)
        {
            bullet.transform.position = position;
            bullet.gameObject.SetActive(true);
            _activeBulletCount++;
        }

        private PlayerBullet GetPooledBullet()
        {
            if (_bulletPool == null)
            {
                return null;
            }

            for (int i = 0; i < _bulletPool.Length; i++)
            {
                if (_bulletPool[i] != null && !_bulletPool[i].gameObject.activeSelf)
                {
                    return _bulletPool[i];
                }
            }

            // 풀이 고갈되었을 때 동적 확장
            if (_bulletPrefab != null)
            {
                PlayerBullet newBullet = Instantiate(_bulletPrefab, transform);
                newBullet.gameObject.SetActive(false);
                newBullet.Initialize(OnBulletDeactivated, _playAreaManager);

                Array.Resize(ref _bulletPool, _bulletPool.Length + 1);
                _bulletPool[_bulletPool.Length - 1] = newBullet;
                return newBullet;
            }

            return null;
        }

        private void OnBulletDeactivated(PlayerBullet bullet)
        {
            _activeBulletCount = Mathf.Max(0, _activeBulletCount - 1);
        }

        /// <summary>
        /// 테스트 또는 수동 바인딩용: 외부에서 생성된 풀 배열을 주입합니다.
        /// </summary>
        public void SetBulletPoolForTesting(PlayerBullet[] pool)
        {
            _bulletPool = pool;
            _activeBulletCount = 0;
            foreach (var bullet in _bulletPool)
            {
                if (bullet != null)
                {
                    bullet.Initialize(OnBulletDeactivated, _playAreaManager);
                }
            }
        }

        /// <summary>
        /// 활성화된 모든 탄환을 회수하고 카운트를 초기화합니다.
        /// </summary>
        public void ResetAllBullets()
        {
            if (_bulletPool == null)
            {
                return;
            }

            for (int i = 0; i < _bulletPool.Length; i++)
            {
                if (_bulletPool[i] != null && _bulletPool[i].gameObject.activeSelf)
                {
                    _bulletPool[i].ReturnToPool();
                }
            }

            _activeBulletCount = 0;
        }
    }
}
