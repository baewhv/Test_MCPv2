using System;
using UnityEngine;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 보스 갤러그(Boss Galaga) 전용 특수 제어 컴포넌트입니다.
    /// 트랙터 빔 전개, 상단 포획기 결속 슬롯 관리, 포획/구출 상태 추적 및 호버링 동작을 제어합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyBoss : MonoBehaviour
    {
        [Header("Tractor Beam References")]
        [Tooltip("보스 하단 트랙터 빔 전개 컴포넌트")]
        [SerializeField] private BossTractorBeam _tractorBeam;

        [Header("Captured Fighter Slot")]
        [Tooltip("포획된 아군 기체가 결속되는 상단 슬롯 Transform")]
        [SerializeField] private Transform _capturedFighterSlot;

        [Tooltip("현재 포획기가 결속되어 있는지 여부")]
        [SerializeField] private bool _hasCapturedFighter = false;

        [Tooltip("결속된 포획기 적 객체 참조")]
        [SerializeField] private EnemyBase _capturedFighter;

        [Header("Tractor Beam Dive Settings")]
        [Tooltip("트랙터 빔 전개를 위한 화면 호버링 정지 Y좌표 (기본 1.0u)")]
        [SerializeField] private float _hoverTargetY = 1.0f;

        [Tooltip("트랙터 빔 발사 지속 시간 (초, 기본 4.0초)")]
        [SerializeField] private float _beamDuration = 4.0f;

        private EnemyBase _enemyBase;

        public event Action<EnemyBoss> OnTractorBeamDiveStarted;
        public event Action<EnemyBoss> OnTractorBeamHoverStarted;
        public event Action<EnemyBoss> OnTractorBeamEnded;
        public event Action<EnemyBoss, EnemyBase> OnFighterCaptured;

        public EnemyBase BaseEnemy => _enemyBase;
        public BossTractorBeam TractorBeam
        {
            get => _tractorBeam;
            set => _tractorBeam = value;
        }
        public Transform CapturedFighterSlot => _capturedFighterSlot;
        public bool HasCapturedFighter => _hasCapturedFighter;
        public EnemyBase CapturedFighter => _capturedFighter;
        public float HoverTargetY
        {
            get => _hoverTargetY;
            set => _hoverTargetY = value;
        }
        public float BeamDuration
        {
            get => _beamDuration;
            set => _beamDuration = Mathf.Max(0.5f, value);
        }

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();

            if (_tractorBeam == null)
            {
                _tractorBeam = GetComponentInChildren<BossTractorBeam>(true);
            }

            if (_capturedFighterSlot == null)
            {
                Transform foundSlot = transform.Find("CapturedFighterSlot");
                if (foundSlot != null)
                {
                    _capturedFighterSlot = foundSlot;
                }
                else
                {
                    GameObject slotObj = new GameObject("CapturedFighterSlot");
                    slotObj.transform.SetParent(transform);
                    slotObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                    slotObj.transform.localRotation = Quaternion.identity;
                    _capturedFighterSlot = slotObj.transform;
                }
            }
        }

        private void OnEnable()
        {
            if (_tractorBeam != null)
            {
                _tractorBeam.OwnerBoss = _enemyBase;
                _tractorBeam.OnTargetCaptured += HandleTargetCaptured;
                _tractorBeam.OnBeamTimeout += HandleBeamTimeout;
            }

            if (_enemyBase != null)
            {
                _enemyBase.OnDestroyed += HandleBossDestroyed;
            }
        }

        private void OnDisable()
        {
            if (_tractorBeam != null)
            {
                _tractorBeam.OnTargetCaptured -= HandleTargetCaptured;
                _tractorBeam.OnBeamTimeout -= HandleBeamTimeout;
                _tractorBeam.DeactivateBeam();
            }

            if (_enemyBase != null)
            {
                _enemyBase.OnDestroyed -= HandleBossDestroyed;
            }

            OnTractorBeamDiveStarted = null;
            OnTractorBeamHoverStarted = null;
            OnTractorBeamEnded = null;
            OnFighterCaptured = null;
        }

        /// <summary>
        /// 트랙터 빔 호버링 고도에 도달했을 때 빔 전개를 시작합니다.
        /// </summary>
        public void StartTractorBeam()
        {
            if (_enemyBase != null)
            {
                _enemyBase.SetState(EnemyState.TractorBeam);
            }

            if (_tractorBeam != null)
            {
                _tractorBeam.ActivateBeam(_beamDuration);
            }

            OnTractorBeamHoverStarted?.Invoke(this);
        }

        /// <summary>
        /// 트랙터 빔을 회수하고 일반 급강하 상태로 복귀합니다.
        /// </summary>
        public void StopTractorBeam()
        {
            if (_tractorBeam != null && _tractorBeam.IsBeamActive)
            {
                _tractorBeam.DeactivateBeam();
            }

            OnTractorBeamEnded?.Invoke(this);
        }

        /// <summary>
        /// 포획된 기체를 보스 상단 결속 슬롯에 부착합니다.
        /// </summary>
        public void AttachCapturedFighter(EnemyBase fighter)
        {
            if (fighter == null)
            {
                return;
            }

            _capturedFighter = fighter;
            _hasCapturedFighter = true;

            if (_capturedFighterSlot != null)
            {
                fighter.transform.SetParent(_capturedFighterSlot);
                fighter.transform.localPosition = Vector3.zero;
                fighter.transform.localRotation = Quaternion.identity;
            }

            OnFighterCaptured?.Invoke(this, fighter);
        }

        /// <summary>
        /// 결속된 포획기를 해제/분리합니다.
        /// </summary>
        public EnemyBase DetachCapturedFighter()
        {
            EnemyBase detached = _capturedFighter;
            if (_capturedFighter != null)
            {
                _capturedFighter.transform.SetParent(null);
                _capturedFighter = null;
            }

            _hasCapturedFighter = false;
            return detached;
        }

        private void HandleTargetCaptured(Collider2D playerCollider)
        {
            // 포획 이벤트 발생 시
            DeactivateTractorBeamInternal();
        }

        private void HandleBeamTimeout()
        {
            DeactivateTractorBeamInternal();
        }

        private void DeactivateTractorBeamInternal()
        {
            StopTractorBeam();
        }

        private void HandleBossDestroyed(EnemyBase boss)
        {
            StopTractorBeam();
        }
    }
}
