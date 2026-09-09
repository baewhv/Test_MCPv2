using System;
using System.Collections;
using UnityEngine;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Enemy;

namespace Galaga.Gameplay.Player
{
    /// <summary>
    /// 플레이어 기체 포획 상태 머신 단계 열거형입니다.
    /// </summary>
    public enum CapturePhase
    {
        None = 0,
        Phase1_ControlLoss = 1,   // 0.0s ~ 0.2s: 조작권 즉시 상실 및 빔 중심축 보간 시작
        Phase2_SpinAlign = 2,     // 0.2s ~ 1.2s: Z축 360도 스핀 회전 및 중심 정렬, 상향 견인
        Phase3_TractorPull = 3,   // 1.2s ~ 2.2s: 보스 상단 슬롯 견인 및 색상 반전 (포획 틴트)
        Phase4_FormationBind = 4  // 2.2s ~ 3.0s: 보스 상단 결속, 잔기 차감 및 차기 기체 출격/게임오버
    }

    /// <summary>
    /// 플레이어 기체가 보스 트랙터 빔에 피격되었을 때 4단계 포획 시퀀스를 전담 제어하는 컨트롤러입니다.
    /// 조작 차단, 스핀 회전, 상단 견인, 색상 반전, 보스 결속 및 잔기 차감/리스폰을 유기적으로 총괄합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class FighterCaptureController : MonoBehaviour
    {
        // -------------------------------------------------------------
        // 1. 인스펙터 직렬화 필드 (Serialized Fields)
        // -------------------------------------------------------------
        [Header("Phase Duration Settings")]
        [Tooltip("Phase 1: 조작 상실 지속 시간 (초, 기본 0.2초)")]
        [SerializeField] private float _phase1Duration = 0.2f;

        [Tooltip("Phase 2: 회전 스핀 및 중심 정렬 지속 시간 (초, 기본 1.0초)")]
        [SerializeField] private float _phase2Duration = 1.0f;

        [Tooltip("Phase 3: 상단 견인 및 색상 반전 지속 시간 (초, 기본 1.0초)")]
        [SerializeField] private float _phase3Duration = 1.0f;

        [Tooltip("Phase 4: 편대 결속 및 리스폰 지속 시간 (초, 기본 0.8초)")]
        [SerializeField] private float _phase4Duration = 0.8f;

        [Header("Motion & Visual Settings")]
        [Tooltip("Phase 2 스핀 회전 각속도 (deg/sec, 기본 720도/초 = 2초당 4바퀴)")]
        [SerializeField] private float _spinSpeed = 720.0f;

        [Tooltip("포획 상태 적군 틴트 색상 (적색/청색 반전 틴트)")]
        [SerializeField] private Color _capturedColor = new Color(1.0f, 0.35f, 0.35f, 1.0f);

        [Tooltip("기본 아군 색상")]
        [SerializeField] private Color _normalColor = Color.white;

        [Tooltip("보스 상단에 결속될 포획기 프리팹 (미지정 시 동적 생성)")]
        [SerializeField] private CapturedFighter _capturedFighterPrefab;

        // -------------------------------------------------------------
        // 2. 런타임 상태 필드 (Runtime State Fields)
        // -------------------------------------------------------------
        private bool _isCapturing = false;
        private CapturePhase _currentPhase = CapturePhase.None;
        private float _currentPhaseTimer = 0f;
        private float _totalCaptureElapsedTime = 0f;

        private PlayerController _capturedPlayer;
        private PlayerShooting _playerShooting;
        private PlayerHealth _playerHealth;
        private Renderer _playerRenderer;
        private SpriteRenderer _playerSpriteRenderer;

        private BossTractorBeam _capturingBeam;
        private EnemyBoss _capturingBoss;
        private CapturedFighter _lastCapturedFighter;

        private Coroutine _captureSequenceCoroutine;
        private Vector3 _phaseStartPos;
        private Vector3 _phaseTargetPos;
        private MaterialPropertyBlock _propBlock;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // -------------------------------------------------------------
        // 3. 프로퍼티 및 싱글톤 (Properties & Singleton)
        // -------------------------------------------------------------
        public static FighterCaptureController Instance { get; private set; }

        public bool IsCapturing => _isCapturing;
        public CapturePhase CurrentPhase => _currentPhase;
        public float CurrentPhaseTimer => _currentPhaseTimer;
        public float TotalCaptureElapsedTime => _totalCaptureElapsedTime;
        public float Phase1Duration => _phase1Duration;
        public float Phase2Duration => _phase2Duration;
        public float Phase3Duration => _phase3Duration;
        public float Phase4Duration => _phase4Duration;
        public float TotalDuration => _phase1Duration + _phase2Duration + _phase3Duration + _phase4Duration;

        public PlayerController CapturedPlayer => _capturedPlayer;
        public BossTractorBeam CapturingBeam => _capturingBeam;
        public EnemyBoss CapturingBoss => _capturingBoss;
        public CapturedFighter LastCapturedFighter => _lastCapturedFighter;

        public CapturedFighter CapturedFighterPrefab
        {
            get => _capturedFighterPrefab;
            set => _capturedFighterPrefab = value;
        }

        // -------------------------------------------------------------
        // 4. C# 이벤트 (Events)
        // -------------------------------------------------------------
        public event Action<PlayerController, EnemyBoss> OnCaptureStarted;
        public event Action<CapturePhase> OnPhaseChanged;
        public event Action<CapturedFighter> OnCaptureCompleted;
        public event Action OnCaptureCancelled;

        // -------------------------------------------------------------
        // 5. 생명주기 메서드 (Lifecycle Methods)
        // -------------------------------------------------------------
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

            _propBlock = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDisable()
        {
            if (_isCapturing)
            {
                CancelCaptureSequence();
            }

            OnCaptureStarted = null;
            OnPhaseChanged = null;
            OnCaptureCompleted = null;
            OnCaptureCancelled = null;
        }

        // -------------------------------------------------------------
        // 6. 포획 시퀀스 개시 및 상태 제어
        // -------------------------------------------------------------
        /// <summary>
        /// 트랙터 빔에 피격된 플레이어 기체의 4단계 포획 시퀀스를 시작합니다.
        /// </summary>
        /// <param name="player">포획 대상 플레이어 컨트롤러</param>
        /// <param name="beam">플레이어를 포획한 트랙터 빔</param>
        /// <param name="boss">소유주 보스 갤러그 (생략 시 빔 소유주로 자동 추적)</param>
        /// <returns>시퀀스 시작 성공 여부</returns>
        public bool StartCaptureSequence(PlayerController player, BossTractorBeam beam, EnemyBoss boss = null)
        {
            if (_isCapturing || player == null || beam == null)
            {
                return false;
            }

            _capturedPlayer = player;
            _playerShooting = player.GetComponent<PlayerShooting>();
            _playerHealth = player.GetComponent<PlayerHealth>();
            _playerRenderer = player.GetComponent<Renderer>();
            _playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

            if (_playerSpriteRenderer == null && _playerRenderer is SpriteRenderer sr)
            {
                _playerSpriteRenderer = sr;
            }

            // 플레이어가 이미 무적이거나 사망 상태인 경우 포획 불가
            if (_playerHealth != null && (_playerHealth.IsDead || _playerHealth.IsInvincible))
            {
                return false;
            }

            _capturingBeam = beam;
            _capturingBoss = boss != null ? boss : (beam.OwnerBoss != null ? beam.OwnerBoss.GetComponent<EnemyBoss>() : null);

            if (_capturingBoss == null)
            {
                _capturingBoss = beam.GetComponentInParent<EnemyBoss>();
            }

            _isCapturing = true;
            _totalCaptureElapsedTime = 0f;
            _currentPhaseTimer = 0f;

            // Phase 1 즉시 진입: 조작 차단 및 무적 설정
            EnterPhase(CapturePhase.Phase1_ControlLoss);

            if (_captureSequenceCoroutine != null)
            {
                StopCoroutine(_captureSequenceCoroutine);
            }

            if (gameObject.activeInHierarchy)
            {
                _captureSequenceCoroutine = StartCoroutine(CaptureSequenceRoutine());
            }

            OnCaptureStarted?.Invoke(_capturedPlayer, _capturingBoss);
            return true;
        }

        /// <summary>
        /// 진행 중인 포획 시퀀스를 즉시 취소하고 플레이어 조작권을 원복합니다 (보스 격파 등 예외 상황).
        /// </summary>
        public void CancelCaptureSequence()
        {
            if (!_isCapturing)
            {
                return;
            }

            if (_captureSequenceCoroutine != null)
            {
                StopCoroutine(_captureSequenceCoroutine);
                _captureSequenceCoroutine = null;
            }

            RestorePlayerState();

            _isCapturing = false;
            _currentPhase = CapturePhase.None;
            _currentPhaseTimer = 0f;
            _totalCaptureElapsedTime = 0f;

            OnCaptureCancelled?.Invoke();
        }

        private void EnterPhase(CapturePhase phase)
        {
            _currentPhase = phase;
            _currentPhaseTimer = 0f;

            if (_capturedPlayer != null)
            {
                _phaseStartPos = _capturedPlayer.transform.position;
            }

            switch (_currentPhase)
            {
                case CapturePhase.Phase1_ControlLoss:
                    // Phase 1: 조작권 즉시 상실 (CanMove=false, CanShoot=false), 무적 활성화
                    if (_capturedPlayer != null)
                    {
                        _capturedPlayer.CanMove = false;
                    }
                    if (_playerShooting != null)
                    {
                        _playerShooting.CanShoot = false;
                    }
                    if (_playerHealth != null)
                    {
                        _playerHealth.SetInvincibleDirectly(true);
                    }
                    break;

                case CapturePhase.Phase2_SpinAlign:
                    // Phase 2: X좌표 빔 중심 정렬 및 서서히 상승
                    break;

                case CapturePhase.Phase3_TractorPull:
                    // Phase 3: 회전 리셋 및 보스 상단 슬롯으로 견인, 색상 반전
                    if (_capturedPlayer != null)
                    {
                        _capturedPlayer.transform.rotation = Quaternion.identity;
                    }
                    break;

                case CapturePhase.Phase4_FormationBind:
                    // Phase 4: 보스 상단 결속, 트랙터 빔 회수, 잔기 차감 및 차기 기체 출격
                    ExecutePhase4BindingAndRespawn();
                    break;
            }

            OnPhaseChanged?.Invoke(_currentPhase);
        }

        // -------------------------------------------------------------
        // 7. 시퀀스 갱신 및 보간 로직 (Coroutine & Manual Ticking)
        // -------------------------------------------------------------
        private IEnumerator CaptureSequenceRoutine()
        {
            while (_isCapturing && _currentPhase != CapturePhase.None)
            {
                float dt = Time.deltaTime;
                TickSequence(dt);
                yield return null;
            }

            _captureSequenceCoroutine = null;
        }

        /// <summary>
        /// 외부 수동 틱 및 단위 테스트를 위한 시퀀스 갱신 메서드입니다.
        /// </summary>
        public void UpdateSequence(float deltaTime)
        {
            if (!_isCapturing)
            {
                return;
            }

            TickSequence(deltaTime);
        }

        private void TickSequence(float dt)
        {
            if (!_isCapturing || _capturedPlayer == null)
            {
                return;
            }

            _currentPhaseTimer += dt;
            _totalCaptureElapsedTime += dt;

            switch (_currentPhase)
            {
                case CapturePhase.Phase1_ControlLoss:
                    UpdatePhase1(dt);
                    if (_currentPhaseTimer >= _phase1Duration)
                    {
                        EnterPhase(CapturePhase.Phase2_SpinAlign);
                    }
                    break;

                case CapturePhase.Phase2_SpinAlign:
                    UpdatePhase2(dt);
                    if (_currentPhaseTimer >= _phase2Duration)
                    {
                        EnterPhase(CapturePhase.Phase3_TractorPull);
                    }
                    break;

                case CapturePhase.Phase3_TractorPull:
                    UpdatePhase3(dt);
                    if (_currentPhaseTimer >= _phase3Duration)
                    {
                        EnterPhase(CapturePhase.Phase4_FormationBind);
                    }
                    break;

                case CapturePhase.Phase4_FormationBind:
                    UpdatePhase4(dt);
                    if (_currentPhaseTimer >= _phase4Duration)
                    {
                        CompleteCaptureSequence();
                    }
                    break;
            }
        }

        private void UpdatePhase1(float dt)
        {
            // 빔 중심 X좌표로 부드럽게 보간
            if (_capturingBeam != null)
            {
                Vector3 pos = _capturedPlayer.transform.position;
                float targetX = _capturingBeam.transform.position.x;
                pos.x = Mathf.MoveTowards(pos.x, targetX, 5.0f * dt);
                _capturedPlayer.transform.position = pos;
            }
        }

        private void UpdatePhase2(float dt)
        {
            // Z축 360도 연속 회전
            _capturedPlayer.transform.Rotate(0f, 0f, _spinSpeed * dt);

            // X좌표 완전 정렬 및 Y좌표 서서히 상향 견인
            Vector3 pos = _capturedPlayer.transform.position;
            if (_capturingBeam != null)
            {
                float targetX = _capturingBeam.transform.position.x;
                pos.x = Mathf.MoveTowards(pos.x, targetX, 8.0f * dt);
            }

            // 호버링 고도 또는 보스 하단 방향으로 서서히 상승
            pos.y += 1.5f * dt;
            _capturedPlayer.transform.position = pos;
        }

        private void UpdatePhase3(float dt)
        {
            // 보스 상단 슬롯 또는 보스 위치(+0.8u)를 향해 상승 및 정렬
            Vector3 targetPos = Vector3.zero;
            if (_capturingBoss != null)
            {
                if (_capturingBoss.CapturedFighterSlot != null)
                {
                    targetPos = _capturingBoss.CapturedFighterSlot.position;
                }
                else
                {
                    targetPos = _capturingBoss.transform.position + new Vector3(0f, 0.8f, 0f);
                }
            }
            else if (_capturingBeam != null)
            {
                targetPos = _capturingBeam.transform.position + new Vector3(0f, 0.8f, 0f);
            }

            float t = Mathf.Clamp01(_currentPhaseTimer / Mathf.Max(0.01f, _phase3Duration));
            _capturedPlayer.transform.position = Vector3.Lerp(_phaseStartPos, targetPos, t);

            // 색상 반전 (기본 색상 -> 포획 적군 틴트 색상)
            Color blendedColor = Color.Lerp(_normalColor, _capturedColor, t);
            ApplyPlayerColor(blendedColor);
        }

        private void UpdatePhase4(float dt)
        {
            // Phase 4 대기 및 차기 기체 출격 시점 대기
        }

        // -------------------------------------------------------------
        // 8. Phase 4 결속 및 완료 처리
        // -------------------------------------------------------------
        private void ExecutePhase4BindingAndRespawn()
        {
            // 트랙터 빔 비활성화
            if (_capturingBeam != null)
            {
                _capturingBeam.DeactivateBeam();
            }
            if (_capturingBoss != null)
            {
                _capturingBoss.StopTractorBeam();
            }

            // 보스 상단 슬롯에 CapturedFighter 인스턴스 생성 및 결속
            CapturedFighter fighterInstance = CreateOrSpawnCapturedFighter();
            _lastCapturedFighter = fighterInstance;

            if (_capturingBoss != null && fighterInstance != null)
            {
                _capturingBoss.AttachCapturedFighter(fighterInstance.EnemyBaseComponent);
                fighterInstance.AttachToBossSlot(_capturingBoss.CapturedFighterSlot);
            }

            // 플레이어 잔기 1 차감
            if (_playerHealth != null)
            {
                _playerHealth.DeductLifeOnCapture();
            }
        }

        private void CompleteCaptureSequence()
        {
            _isCapturing = false;
            _currentPhase = CapturePhase.None;

            if (_playerHealth != null && _playerHealth.CurrentLives > 0)
            {
                // 잔기가 남아있는 경우 차기 기체 출격 (하단 중앙 리스폰, 조작권 회복, 무적 부여)
                RespawnNextFighter();
            }
            else
            {
                // 잔기 소진 시 기체 숨김 및 게임 오버 확정
                if (_capturedPlayer != null)
                {
                    _capturedPlayer.gameObject.SetActive(false);
                }
            }

            OnCaptureCompleted?.Invoke(_lastCapturedFighter);
        }

        private CapturedFighter CreateOrSpawnCapturedFighter()
        {
            GameObject fighterObj = null;

            if (_capturedFighterPrefab != null)
            {
                fighterObj = Instantiate(_capturedFighterPrefab.gameObject);
            }
            else
            {
                // 프리팹 미지정 시 절차적 생성
                fighterObj = new GameObject("CapturedFighter");
                fighterObj.tag = "Enemy";

                SpriteRenderer sr = fighterObj.AddComponent<SpriteRenderer>();
                if (_playerSpriteRenderer != null)
                {
                    sr.sprite = _playerSpriteRenderer.sprite;
                }

                CircleCollider2D col = fighterObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;

                EnemyBase enemyBase = fighterObj.AddComponent<EnemyBase>();
                fighterObj.AddComponent<CapturedFighter>();
            }

            CapturedFighter captured = fighterObj.GetComponent<CapturedFighter>();
            if (captured != null)
            {
                captured.Initialize(_capturingBoss);
            }

            return captured;
        }

        /// <summary>
        /// 잔기가 남아있을 때 차기 기체를 출격시키고 조작권을 완전히 회복합니다.
        /// </summary>
        public void RespawnNextFighter()
        {
            if (_capturedPlayer == null)
            {
                return;
            }

            _capturedPlayer.gameObject.SetActive(true);
            _capturedPlayer.transform.rotation = Quaternion.identity;
            ApplyPlayerColor(_normalColor);

            if (_playerHealth != null)
            {
                _capturedPlayer.transform.position = _playerHealth.RespawnPosition;
                _playerHealth.StartInvincibility(_playerHealth.InvincibilityDuration);
            }
            else
            {
                _capturedPlayer.transform.position = new Vector3(0f, -8.0f, 0f);
            }

            if (_capturedPlayer != null)
            {
                _capturedPlayer.CanMove = true;
            }

            if (_playerShooting != null)
            {
                _playerShooting.CanShoot = true;
            }
        }

        private void RestorePlayerState()
        {
            if (_capturedPlayer == null)
            {
                return;
            }

            _capturedPlayer.transform.rotation = Quaternion.identity;
            _capturedPlayer.CanMove = true;
            ApplyPlayerColor(_normalColor);

            if (_playerShooting != null)
            {
                _playerShooting.CanShoot = true;
            }

            if (_playerHealth != null)
            {
                _playerHealth.SetInvincibleDirectly(false);
            }
        }

        private void ApplyPlayerColor(Color color)
        {
            if (_playerSpriteRenderer != null)
            {
                _playerSpriteRenderer.color = color;
            }
            else if (_playerRenderer != null)
            {
                if (_propBlock == null)
                {
                    _propBlock = new MaterialPropertyBlock();
                }

                _playerRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, color);
                _propBlock.SetColor(ColorId, color);
                _playerRenderer.SetPropertyBlock(_propBlock);
            }
        }
    }
}
