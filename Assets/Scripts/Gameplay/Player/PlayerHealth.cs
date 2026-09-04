using System;
using System.Collections;
using UnityEngine;
using Galaga.Gameplay.Combat;

namespace Galaga.Gameplay.Player
{
    /// <summary>
    /// 플레이어 기체의 잔기(Lives), 피격, 리스폰 및 무적(Invincibility) 깜빡임 시스템을 제어하는 컴포넌트입니다.
    /// IDamageable 인터페이스를 구현하여 적 발사체 및 충돌체와의 결합도를 디커플링합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Lives Settings")]
        [Tooltip("게임 시작 시 기본 잔기 수")]
        [SerializeField] private int _startingLives = 3;

        [Tooltip("최대 보유 가능한 잔기 수")]
        [SerializeField] private int _maxLives = 6;

        [Header("Respawn & Invincibility")]
        [Tooltip("부활 시 무적 지속 시간(초)")]
        [SerializeField] private float _invincibilityDuration = 1.5f;

        [Tooltip("무적 중 메쉬 깜빡임 주기(초)")]
        [SerializeField] private float _blinkInterval = 0.1f;

        [Tooltip("리스폰 고정 위치 (화면 중앙 최하단)")]
        [SerializeField] private Vector3 _respawnPosition = new Vector3(0f, -8f, 0f);

        [Header("References")]
        [Tooltip("깜빡임 연출용 Renderer (미지정 시 자동 탐색)")]
        [SerializeField] private Renderer _playerRenderer;

        private int _currentLives = 3;
        private bool _isInvincible = false;
        private bool _isDead = false;
        private Coroutine _invincibilityCoroutine;

        /// <summary>
        /// 잔기 변경 시 발행되는 이벤트 (현재 잔기 수 전달)
        /// </summary>
        public event Action<int> OnLivesChanged;

        /// <summary>
        /// 플레이어 리스폰 완료 시 발행되는 이벤트
        /// </summary>
        public event Action OnPlayerRespawned;

        /// <summary>
        /// 잔기 소진으로 최종 게임 오버 사망 시 발행되는 이벤트
        /// </summary>
        public event Action OnPlayerDied;

        public int CurrentHP => _currentLives;
        public int CurrentLives => _currentLives;
        public int MaxLives => _maxLives;
        public bool IsInvincible => _isInvincible;
        public bool IsDead => _isDead;
        public Vector3 RespawnPosition
        {
            get => _respawnPosition;
            set => _respawnPosition = value;
        }

        public float InvincibilityDuration
        {
            get => _invincibilityDuration;
            set => _invincibilityDuration = value;
        }

        /// <summary>
        /// 잔기 및 컴포넌트를 명시적으로 초기화합니다 (런타임 및 단위 테스트용).
        /// </summary>
        public void Initialize(int startingLives = 3)
        {
            _startingLives = startingLives;
            _currentLives = startingLives;
            _isDead = false;
            _isInvincible = false;

            if (_playerRenderer == null)
            {
                _playerRenderer = GetComponent<Renderer>();
                if (_playerRenderer == null)
                {
                    _playerRenderer = GetComponentInChildren<Renderer>();
                }
            }
        }

        private void Awake()
        {
            Initialize(_startingLives);
        }

        private void Start()
        {
            OnLivesChanged?.Invoke(_currentLives);
        }

        private void OnDisable()
        {
            if (_invincibilityCoroutine != null)
            {
                StopCoroutine(_invincibilityCoroutine);
                _invincibilityCoroutine = null;
            }

            if (_playerRenderer != null)
            {
                _playerRenderer.enabled = true;
            }
        }

        /// <summary>
        /// 피격을 처리합니다. 무적 상태이거나 이미 사망한 경우 무시됩니다.
        /// </summary>
        /// <returns>실제 피격 발생 여부</returns>
        public bool TakeDamage(int damage = 1)
        {
            if (_isInvincible || _isDead)
            {
                return false;
            }

            _currentLives -= damage;
            OnLivesChanged?.Invoke(_currentLives);

            if (ExplosionManager.Instance != null)
            {
                ExplosionManager.Instance.SpawnExplosion(transform.position, 1.5f, 0.6f);
            }

            if (_currentLives <= 0)
            {
                _currentLives = 0;
                _isDead = true;
                HandleDeath();
            }
            else
            {
                Respawn();
            }

            return true;
        }

        /// <summary>
        /// IDamageable 인터페이스 명시적 구현
        /// </summary>
        void IDamageable.TakeDamage(int damage)
        {
            TakeDamage(damage);
        }

        /// <summary>
        /// 익스텐드(보너스 점수 도달 등)로 추가 잔기를 지급합니다.
        /// </summary>
        public void AddLife(int amount = 1)
        {
            if (_isDead)
            {
                return;
            }

            _currentLives = Mathf.Min(_currentLives + amount, _maxLives);
            OnLivesChanged?.Invoke(_currentLives);
        }

        /// <summary>
        /// 잔기를 지정된 수치로 설정합니다 (테스트 및 초기화용).
        /// </summary>
        public void SetLives(int lives)
        {
            _currentLives = Mathf.Clamp(lives, 0, _maxLives);
            _isDead = _currentLives <= 0;
            OnLivesChanged?.Invoke(_currentLives);
        }

        /// <summary>
        /// 플레이어를 중앙 최하단 리스폰 위치로 재배치하고 무적 시퀀스를 시작합니다.
        /// </summary>
        public void Respawn()
        {
            transform.position = _respawnPosition;
            StartInvincibility(_invincibilityDuration);
            OnPlayerRespawned?.Invoke();
        }

        /// <summary>
        /// 지정된 시간 동안 무적 상태를 활성화하고 깜빡임 효과를 적용합니다.
        /// </summary>
        public void StartInvincibility(float duration)
        {
            if (_invincibilityCoroutine != null)
            {
                StopCoroutine(_invincibilityCoroutine);
            }

            _invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));
        }

        private IEnumerator InvincibilityRoutine(float duration)
        {
            _isInvincible = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_playerRenderer != null)
                {
                    _playerRenderer.enabled = !_playerRenderer.enabled;
                }

                yield return new WaitForSeconds(_blinkInterval);
                elapsed += _blinkInterval;
            }

            if (_playerRenderer != null)
            {
                _playerRenderer.enabled = true;
            }

            _isInvincible = false;
            _invincibilityCoroutine = null;
        }

        /// <summary>
        /// 테스트 또는 수동으로 무적 상태를 즉시 설정합니다.
        /// </summary>
        public void SetInvincibleDirectly(bool isInvincible)
        {
            _isInvincible = isInvincible;
        }

        private void HandleDeath()
        {
            if (_playerRenderer != null)
            {
                _playerRenderer.enabled = false;
            }

            OnPlayerDied?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null || _isInvincible || _isDead)
            {
                return;
            }

            if (collision.CompareTag("EnemyBullet") || collision.name.Contains("EnemyBullet"))
            {
                EnemyBullet bullet = collision.GetComponent<EnemyBullet>();
                if (bullet != null)
                {
                    bullet.ReturnToPool();
                }
                TakeDamage(1);
                return;
            }

            if (collision.CompareTag("Enemy") || collision.name.Contains("Enemy"))
            {
                TakeDamage(1);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || _isInvincible || _isDead)
            {
                return;
            }

            if (other.CompareTag("EnemyBullet") || other.name.Contains("EnemyBullet"))
            {
                EnemyBullet bullet = other.GetComponent<EnemyBullet>();
                if (bullet != null)
                {
                    bullet.ReturnToPool();
                }
                TakeDamage(1);
                return;
            }

            if (other.CompareTag("Enemy") || other.name.Contains("Enemy"))
            {
                TakeDamage(1);
            }
        }
    }
}
