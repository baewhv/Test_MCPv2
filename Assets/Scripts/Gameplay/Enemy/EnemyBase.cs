using System;
using System.Collections;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Score;

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
        [SerializeField] private int _escortCount = 0;

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

        public int EscortCount
        {
            get => _escortCount;
            set => _escortCount = value;
        }
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

            _propBlock = new MaterialPropertyBlock();
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

            OnDestroyed = null;
            OnDamaged = null;
            OnStateChanged = null;
        }

        /// <summary>
        /// ScriptableObject 데이터 기반으로 적 기체를 초기화합니다.
        /// </summary>
        public void Initialize(EnemyDataSO data)
        {
            _enemyData = data;
            if (_enemyData != null)
            {
                _currentHP = _enemyData.MaxHP;
                ApplyColor(_enemyData.NormalColor);
            }
            else
            {
                _currentHP = 1;
            }

            _currentState = EnemyState.Spawning;
        }

        /// <summary>
        /// 적 기체의 현재 상태를 변경하고 이벤트를 발생시킵니다.
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
        /// 편대 진입 또는 복귀 완료 후 상단 그리드 대기 상태(GridHovering)로 진입합니다.
        /// </summary>
        public void EnterFormation()
        {
            SetState(EnemyState.Formation);
        }

        /// <summary>
        /// 데미지를 적용하고 플래시 연출 및 파괴 처리를 수행합니다.
        /// </summary>
        /// <param name="damage">입힐 데미지 양</param>
        /// <returns>파괴(사망) 여부</returns>
        public bool TakeDamage(int damage = 1)
        {
            if (IsDead)
            {
                return false;
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
                // 보스 1타 피격 시 청색 변색 또는 플래시 연출
                Color nextColor = (_enemyData != null && _enemyData.Type == EnemyType.BossGalaga && _currentHP == 1)
                    ? _enemyData.DamagedColor
                    : (_enemyData != null ? _enemyData.NormalColor : Color.white);

                TriggerFlash(nextColor);
                return false;
            }
        }

        /// <summary>
        /// 적 기체 사망/파괴 시퀀스를 처리합니다.
        /// </summary>
        public void Die()
        {
            EnemyState prevState = _currentState;
            SetState(EnemyState.Dead);

            if (ScoreManager.Instance != null)
            {
                bool isDiving = (prevState == EnemyState.Diving || prevState == EnemyState.Entering || prevState == EnemyState.Returning);
                ScoreManager.Instance.AddEnemyScore(Type, isDiving, EscortCount);
            }

            if (ExplosionManager.Instance != null)
            {
                float size = (_enemyData != null && _enemyData.Type == EnemyType.BossGalaga) ? 2.0f : 1.2f;
                ExplosionManager.Instance.SpawnExplosion(transform.position, size, 0.4f);
            }

            OnDestroyed?.Invoke(this);
            gameObject.SetActive(false);
        }

/// <summary>
        /// 현재 상태(대기/비행)에 대응하는 점수 값을 반환합니다.
        /// </summary>
        public int GetCurrentScoreValue()
        {
            if (_enemyData == null)
            {
                return 0;
            }

            bool isDiving = (_currentState == EnemyState.Diving || _currentState == EnemyState.Entering || _currentState == EnemyState.Returning);
            return isDiving ? _enemyData.ScoreDive : _enemyData.ScoreStay;
        }


        /// <summary>
        /// 진입 또는 다이브 비행 경로를 할당하고 추적을 시작합니다.
        /// </summary>
public void StartPathFollow(BezierSegment[] segments, float speed, bool alignRotation = true)
        {
            if (_pathFollower == null)
            {
                _pathFollower = GetComponent<BezierPathFollower>();
            }

            if (_pathFollower != null)
            {
                _pathFollower.SetPath(segments, speed, false);
                _pathFollower.RotateAlongPath = alignRotation;
                _pathFollower.Play();
            }
        }

        private void HandlePathCompleted()
        {
            if (_currentState == EnemyState.Entering)
            {
                SetState(EnemyState.Formation);
            }
            else if (_currentState == EnemyState.Diving)
            {
                SetState(EnemyState.Returning);
            }
        }

        private void TriggerFlash(Color restoreColor)
        {
            if (!gameObject.activeSelf)
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
            float duration = _enemyData != null ? _enemyData.FlashDuration : 0.08f;

            ApplyColor(flashColor);
            yield return new WaitForSeconds(duration);
            ApplyColor(restoreColor);
            _flashCoroutine = null;
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
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

                if (_renderer is SpriteRenderer spriteRenderer)
                {
                    spriteRenderer.color = color;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null || IsDead)
            {
                return;
            }

            // 적 탄환 및 적 아군 충돌 무시 (자폭 및 오폭 방지)
            if (collision.CompareTag("EnemyBullet") || collision.CompareTag("Enemy") ||
                collision.GetComponent<EnemyBullet>() != null ||
                collision.name.Contains("EnemyBullet"))
            {
                return;
            }

            // 플레이어 탄환 피격 처리
            if (collision.CompareTag("PlayerBullet") || collision.GetComponent<PlayerBullet>() != null ||
                (collision.name.Contains("PlayerBullet") && !collision.name.Contains("EnemyBullet")))
            {
                if (collision.TryGetComponent<PlayerBullet>(out var bullet))
                {
                    if (bullet.gameObject.activeSelf)
                    {
                        TakeDamage(bullet.Damage);
                        bullet.ReturnToPool();
                    }
                }
                else
                {
                    TakeDamage(1);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || IsDead)
            {
                return;
            }

            // 적 탄환 및 적 아군 충돌 무시 (자폭 및 오폭 방지)
            if (other.CompareTag("EnemyBullet") || other.CompareTag("Enemy") ||
                other.GetComponent<EnemyBullet>() != null ||
                other.name.Contains("EnemyBullet"))
            {
                return;
            }

            // 플레이어 탄환 피격 처리
            if (other.CompareTag("PlayerBullet") || other.GetComponent<PlayerBullet>() != null ||
                (other.name.Contains("PlayerBullet") && !other.name.Contains("EnemyBullet")))
            {
                if (other.TryGetComponent<PlayerBullet>(out var bullet))
                {
                    if (bullet.gameObject.activeSelf)
                    {
                        TakeDamage(bullet.Damage);
                        bullet.ReturnToPool();
                    }
                }
                else
                {
                    TakeDamage(1);
                }
            }
        }
    }
}
