using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Galaga.Core;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 플레이어 기체의 탄환 발사 및 오브젝트 풀을 관리하는 컴포넌트입니다.
    /// 화면 내 최대 탄환 수(싱글 2발 / 듀얼 4발)를 제한합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerShooting : MonoBehaviour
    {
        [Header("Shooting Settings")]
        [Tooltip("싱글 파이터 기준 화면 내 동시 존재 가능한 최대 탄환 수")]
        [SerializeField] private int _maxSimultaneousBullets = 2;

        [Tooltip("발사 최소 간격(초)")]
        [SerializeField] private float _fireCooldown = 0.05f;

        [Tooltip("듀얼 파이터 활성화 여부")]
        [SerializeField] private bool _isDualFighter = false;

        [Header("Spawn Points")]
        [Tooltip("단일 파이터 기본 발사 위치")]
        [SerializeField] private Transform _centerFirePoint;

        [Tooltip("듀얼 파이터 좌측 발사 위치")]
        [SerializeField] private Transform _leftFirePoint;

        [Tooltip("듀얼 파이터 우측 발사 위치")]
        [SerializeField] private Transform _rightFirePoint;

        [Header("Bullet Prefab & Pool")]
        [Tooltip("탄환 프리팹 (PlayerBullet 컴포넌트 포함 필수)")]
        [SerializeField] private GameObject _bulletPrefab;

        [Tooltip("오브젝트 풀 초기 생성 수량")]
        [SerializeField] private int _initialPoolSize = 6;

        [Header("References")]
        [Tooltip("PlayAreaManager 참조")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        [Tooltip("New Input System 공격 액션 참조")]
        [SerializeField] private InputActionReference _attackAction;

        private readonly List<PlayerBullet> _bulletPool = new List<PlayerBullet>();
        private readonly HashSet<PlayerBullet> _activeBullets = new HashSet<PlayerBullet>();
        private float _cooldownTimer = 0f;
        private Transform _bulletPoolRoot;

        public int MaxSimultaneousBullets => _isDualFighter ? _maxSimultaneousBullets * 2 : _maxSimultaneousBullets;
        public int ActiveBulletCount => _activeBullets.Count;

        public bool IsDualFighter
        {
            get => _isDualFighter;
            set => _isDualFighter = value;
        }

        public GameObject BulletPrefab
        {
            get => _bulletPrefab;
            set => _bulletPrefab = value;
        }

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        public Transform CenterFirePoint
        {
            get => _centerFirePoint;
            set => _centerFirePoint = value;
        }

        public Transform LeftFirePoint
        {
            get => _leftFirePoint;
            set => _leftFirePoint = value;
        }

        public Transform RightFirePoint
        {
            get => _rightFirePoint;
            set => _rightFirePoint = value;
        }

        public float FireCooldown
        {
            get => _fireCooldown;
            set => _fireCooldown = value;
        }

        private void Awake()
        {
            InitializePool();
        }

        private void Start()
        {
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
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            // Fallback Keyboard 직접 폴링 (Input Action 미연결 시)
            if (_attackAction == null)
            {
                CheckKeyboardInput();
            }
        }

        private void CheckKeyboardInput()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame)
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
        /// 오브젝트 풀을 초기화하고 지정된 수량만큼 탄환을 미리 생성합니다.
        /// </summary>
        public void InitializePool()
        {
            if (_bulletPoolRoot == null)
            {
                GameObject rootObj = new GameObject("PlayerBulletPool");
                _bulletPoolRoot = rootObj.transform;
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

        private PlayerBullet CreateNewBulletInstance()
        {
            GameObject bulletObj;
            if (_bulletPrefab != null)
            {
                bulletObj = Instantiate(_bulletPrefab, _bulletPoolRoot);
            }
            else
            {
                bulletObj = new GameObject("PlayerBullet");
                if (_bulletPoolRoot != null)
                {
                    bulletObj.transform.SetParent(_bulletPoolRoot);
                }
            }

            bulletObj.SetActive(false);

            PlayerBullet bullet = bulletObj.GetComponent<PlayerBullet>();
            if (bullet == null)
            {
                bullet = bulletObj.AddComponent<PlayerBullet>();
            }

            bullet.Initialize(OnBulletDeactivated, _playAreaManager);
            _bulletPool.Add(bullet);
            return bullet;
        }

        private PlayerBullet GetPooledBullet()
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

        /// <summary>
        /// 탄환 발사를 시도합니다. 화면 내 최대 탄환 수 또는 쿨다운 제한에 걸릴 경우 false를 반환합니다.
        /// </summary>
        /// <returns>발사 성공 여부</returns>
        public bool TryFire()
        {
            if (_cooldownTimer > 0f)
            {
                return false;
            }

            int requiredBullets = _isDualFighter ? 2 : 1;
            if (_activeBullets.Count + requiredBullets > MaxSimultaneousBullets)
            {
                return false;
            }

            if (_isDualFighter)
            {
                Vector3 leftPos = _leftFirePoint != null ? _leftFirePoint.position : transform.position + new Vector3(-0.3f, 0.5f, 0f);
                Vector3 rightPos = _rightFirePoint != null ? _rightFirePoint.position : transform.position + new Vector3(0.3f, 0.5f, 0f);

                SpawnBulletAt(leftPos);
                SpawnBulletAt(rightPos);
            }
            else
            {
                Vector3 centerPos = _centerFirePoint != null ? _centerFirePoint.position : transform.position + new Vector3(0f, 0.5f, 0f);
                SpawnBulletAt(centerPos);
            }

            _cooldownTimer = _fireCooldown;
            return true;
        }

        private PlayerBullet SpawnBulletAt(Vector3 spawnPosition)
        {
            PlayerBullet bullet = GetPooledBullet();
            bullet.transform.position = spawnPosition;
            bullet.PlayAreaManager = _playAreaManager;
            bullet.gameObject.SetActive(true);

            _activeBullets.Add(bullet);
            return bullet;
        }

        /// <summary>
        /// 탄환이 비활성화(화면 이탈 또는 적 충돌)될 때 호출되는 콜백입니다.
        /// </summary>
        private void OnBulletDeactivated(PlayerBullet bullet)
        {
            if (bullet != null)
            {
                _activeBullets.Remove(bullet);
            }
        }
    }
}
