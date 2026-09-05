using System;
using System.Collections;
using UnityEngine;
using Galaga.Gameplay.Combat;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 모든 적 기체의 기본 생명주기, 체력, 피격 플래시, 상태 머신 및 경로 추적 연동을 담당하는 베이스 컴포넌트입니다.
    /// IDamageable 인터페이스를 구현하여 탄환 및 충돌 시스템과의 피격 파이프라인 무결성을 보장합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BezierPathFollower))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Data Configuration")]
        [Tooltip("적 기체 스펙 ScriptableObject")]
        [SerializeField] private EnemyDataSO _enemyData;

        [Header("Component References")]
        [Tooltip("3차 베지어 곡선 경로 추적 컴포넌트")]
        [SerializeField] private BezierPathFollower _pathFollower;

        [Tooltip("시각적 렌더러 (SpriteRenderer 또는 MeshRenderer)")]
        [SerializeField] private Renderer _renderer;

        [Tooltip("충돌 판정 콜라이더")]
        [SerializeField] private Collider2D _collider;

        [Header("Runtime State (Inspector View)")]
        [SerializeField] private EnemyState _currentState = EnemyState.Spawning;
        [SerializeField] private int _currentHP = 1;

        private Coroutine _flashCoroutine;
        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public event Action<EnemyBase> OnDestroyed;
        public event Action<EnemyBase, int> OnDamaged;
        public event Action<EnemyBase, EnemyState> OnStateChanged;

        public EnemyDataSO Data => _enemyData;
        public BezierPathFollower PathFollower => _pathFollower;
        public EnemyState CurrentState => _currentState;
        public int CurrentHP => _currentHP;
        public bool IsDead => _currentState == EnemyState.Dead || _currentHP <= 0;
        public bool IsAlive => !IsDead;
        public EnemyType Type => _enemyData != null ? _enemyData.Type : EnemyType.Zako;
        public EnemyType EnemyType => Type;

        private void Awake()
        {
            if (_pathFollower == null)
            {
                _pathFollower = GetComponent<BezierPathFollower>();
            }

            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            if (_enemyData != null)
            {
                Initialize(_enemyData);
            }
        }

        private void OnEnable()
        {
            if (_pathFollower != null)
            {
                _pathFollower.OnPathCompleted += HandlePathCompleted;
            }
        }

        private void OnDisable()
        {
            if (_pathFollower != null)
            {
                _pathFollower.OnPathCompleted -= HandlePathCompleted;
            }

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
        }

        /// <summary>
        /// 적 데이터를 기반으로 체력, 이동속도, 기본 색상 등을 초기화합니다.
        /// </summary>
        public void Initialize(EnemyDataSO data)
        {
            _enemyData = data;
            if (_enemyData != null)
            {
                _currentHP = _enemyData.MaxHP;
                if (_pathFollower != null)
                {
                    _pathFollower.MoveSpeed = _enemyData.MoveSpeed;
                }
                ApplyColor(_enemyData.NormalColor);
            }
            else
            {
                _currentHP = 1;
            }

            _currentState = EnemyState.Spawning;
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        /// <summary>
        /// 상태를 명시적으로 전환합니다.
        /// </summary>
        public void SetState(EnemyState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            _currentState = newState;
            OnStateChanged?.Invoke(this, _currentState);
        }

        /// <summary>
        /// 피격을 처리합니다 (IDamageable 인터페이스 구현).
        /// </summary>
        /// <param name="damage">입힐 데미지 양</param>
        /// <returns>사망 여부 (true: 사망/격파, false: 생존)</returns>
        public bool TakeDamage(int damage = 1)
        {
            if (IsDead)
            {
                return true;
            }

            _currentHP -= damage;
            OnDamaged?.Invoke(this, _currentHP);

            if (_currentHP <= 0)
            {
                _currentHP = 0;
                Die();
                return true;
            }
            else
            {
                // 피격 플래시 및 손상 색상 반영
                Color targetBaseColor = (_enemyData != null && _currentHP < _enemyData.MaxHP)
                    ? _enemyData.DamagedColor
                    : (_enemyData != null ? _enemyData.NormalColor : Color.white);

                TriggerFlash(targetBaseColor);
                return false;
            }
        }

        /// <summary>
        /// 현재 상태에 따른 격파 점수를 반환합니다.
        /// </summary>
        public int GetCurrentScoreValue()
        {
            if (_enemyData == null)
            {
                return 50;
            }

            return _currentState == EnemyState.Formation
                ? _enemyData.ScoreStay
                : _enemyData.ScoreDive;
        }

        /// <summary>
        /// 편대 진입 완료 시 호출되어 편대 대기 상태로 진입합니다.
        /// </summary>
        public void EnterFormation()
        {
            SetState(EnemyState.Formation);
            if (_pathFollower != null)
            {
                _pathFollower.Stop();
            }
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 적 사망/격파 처리를 수행합니다.
        /// </summary>
        public void Die()
        {
            SetState(EnemyState.Dead);

            if (_pathFollower != null)
            {
                _pathFollower.Stop();
            }

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (ExplosionManager.Instance != null)
            {
                ExplosionManager.Instance.HandleEnemyDestroyed(this);
            }

            OnDestroyed?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void HandlePathCompleted()
        {
            if (_currentState == EnemyState.Entering)
            {
                EnterFormation();
            }
            else if (_currentState == EnemyState.Diving)
            {
                SetState(EnemyState.Returning);
            }
            else if (_currentState == EnemyState.Returning)
            {
                EnterFormation();
            }
        }

        private void TriggerFlash(Color restoreColor)
        {
            if (!gameObject.activeInHierarchy)
            {
                ApplyColor(restoreColor);
                return;
            }

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(FlashRoutine(restoreColor));
        }

        private IEnumerator FlashRoutine(Color restoreColor)
        {
            Color flashColor = _enemyData != null ? _enemyData.FlashColor : Color.white;
            float duration = (_enemyData != null && _enemyData.FlashDuration > 0f) ? _enemyData.FlashDuration : 0.15f;

            ApplyColor(flashColor);
            yield return new WaitForSeconds(duration);
            ApplyColor(restoreColor);
            _flashCoroutine = null;
        }

        public void ApplyColor(Color color)
        {
            if (_renderer is SpriteRenderer spriteRenderer)
            {
                spriteRenderer.color = color;
                return;
            }

            if (_renderer != null)
            {
                if (_propBlock == null)
                {
                    _propBlock = new MaterialPropertyBlock();
                }

                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, color);
                _propBlock.SetColor(ColorId, color);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null || IsDead)
            {
                return;
            }

            if (collision.CompareTag("PlayerBullet") || collision.name.Contains("Bullet"))
            {
                PlayerBullet bullet = collision.GetComponent<PlayerBullet>();
                int dmg = bullet != null ? bullet.Damage : 1;
                TakeDamage(dmg);

                if (bullet != null)
                {
                    bullet.ReturnToPool();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || IsDead)
            {
                return;
            }

            if (other.CompareTag("PlayerBullet") || other.name.Contains("Bullet"))
            {
                PlayerBullet bullet = other.GetComponent<PlayerBullet>();
                int dmg = bullet != null ? bullet.Damage : 1;
                TakeDamage(dmg);

                if (bullet != null)
                {
                    bullet.ReturnToPool();
                }
            }
        }
    }
}
