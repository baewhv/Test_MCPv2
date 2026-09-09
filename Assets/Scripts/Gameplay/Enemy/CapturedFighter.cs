using System;
using UnityEngine;
using Galaga.Gameplay.Combat;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 보스 갤러그 상단 슬롯에 결속된 포획기(Captured Fighter)를 제어하는 컴포넌트입니다.
    /// 적군(EnemyBase)으로 기능하며 포획 상태 렌더링, 보스 결속/해제 및 구출 분기 상태를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public class CapturedFighter : MonoBehaviour
    {
        [Header("Visual & Color")]
        [Tooltip("포획 상태 적군 틴트 색상 (기본 적색/청색 틴트)")]
        [SerializeField] private Color _capturedColor = new Color(1.0f, 0.35f, 0.35f, 1.0f);

        [Tooltip("구출 시 복구할 아군 기본 색상")]
        [SerializeField] private Color _rescuedColor = Color.white;

        [Tooltip("시각적 렌더러")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Boss Attachment State")]
        [Tooltip("현재 결속되어 있는 보스 갤러그")]
        [SerializeField] private EnemyBoss _ownerBoss;

        [Tooltip("보스 결속 상태 여부")]
        [SerializeField] private bool _isAttachedToBoss = false;

        private EnemyBase _enemyBase;

        public event Action<CapturedFighter> OnFighterFreed;
        public event Action<CapturedFighter> OnFighterDestroyed;

        public EnemyBase EnemyBaseComponent => _enemyBase;
        public EnemyBoss OwnerBoss => _ownerBoss;
        public bool IsAttachedToBoss => _isAttachedToBoss;
        public Color CapturedColor => _capturedColor;
        public Color RescuedColor => _rescuedColor;
        public SpriteRenderer SpriteRendererComponent => _spriteRenderer;

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }
        }

        private void OnEnable()
        {
            if (_enemyBase != null)
            {
                _enemyBase.OnDestroyed += HandleEnemyDestroyed;
            }
        }

        private void OnDisable()
        {
            if (_enemyBase != null)
            {
                _enemyBase.OnDestroyed -= HandleEnemyDestroyed;
            }

            OnFighterFreed = null;
            OnFighterDestroyed = null;
        }

        /// <summary>
        /// 포획기를 초기화하고 적군 데이터 및 소유주 보스를 설정합니다.
        /// </summary>
        public void Initialize(EnemyBoss boss, EnemyDataSO data = null)
        {
            _ownerBoss = boss;

            if (_enemyBase == null)
            {
                _enemyBase = GetComponent<EnemyBase>();
            }

            if (_enemyBase != null && data != null)
            {
                _enemyBase.Initialize(data);
            }

            ApplyCapturedVisual();

            if (_ownerBoss != null && _ownerBoss.CapturedFighterSlot != null)
            {
                AttachToBossSlot(_ownerBoss.CapturedFighterSlot);
            }
        }

        /// <summary>
        /// 보스 상단 결속 슬롯에 위치 및 부모 트랜스폼을 고정합니다.
        /// </summary>
        public void AttachToBossSlot(Transform slot)
        {
            if (slot == null)
            {
                return;
            }

            transform.SetParent(slot);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _isAttachedToBoss = true;

            if (_enemyBase != null)
            {
                _enemyBase.SetState(EnemyState.Formation);
            }
        }

        /// <summary>
        /// 보스 결속에서 해제합니다.
        /// </summary>
        public void DetachFromBoss()
        {
            transform.SetParent(null);
            _isAttachedToBoss = false;
            OnFighterFreed?.Invoke(this);
        }

        /// <summary>
        /// 포획 적군 시각적 색상을 적용합니다.
        /// </summary>
        public void ApplyCapturedVisual()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _capturedColor;
            }
            if (_enemyBase != null)
            {
                _enemyBase.ApplyColor(_capturedColor);
            }
        }

        /// <summary>
        /// 구출 완료 아군 시각적 색상으로 복구합니다.
        /// </summary>
        public void ApplyRescuedVisual()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _rescuedColor;
            }
            if (_enemyBase != null)
            {
                _enemyBase.ApplyColor(_rescuedColor);
            }
        }

        /// <summary>
        /// 소유주 보스를 명시적으로 설정합니다.
        /// </summary>
        public void SetOwnerBoss(EnemyBoss boss)
        {
            _ownerBoss = boss;
        }

        private void HandleEnemyDestroyed(EnemyBase enemy)
        {
            OnFighterDestroyed?.Invoke(this);
        }
    }
}
