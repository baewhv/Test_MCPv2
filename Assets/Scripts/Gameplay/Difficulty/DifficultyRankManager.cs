using System;
using UnityEngine;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;
using Galaga.Gameplay.Stage;

namespace Galaga.Gameplay.Difficulty
{
    /// <summary>
    /// 동적 난이도 랭크에 따라 결정되는 게임플레이 가변 수치 매개변수 구조체입니다.
    /// </summary>
    [System.Serializable]
    public struct DifficultyRankParameters : IEquatable<DifficultyRankParameters>
    {
        [Tooltip("현재 난이도 랭크 (1 ~ 32)")]
        public int Rank;

        [Tooltip("적 기체 급강하 비행 속도 (units/sec)")]
        public float DiveSpeed;

        [Tooltip("적 기체 조준 탄환 비행 속도 (units/sec)")]
        public float BulletSpeed;

        [Tooltip("동시 급강하 최대 적 기체 수")]
        public int MaxConcurrentDives;

        [Tooltip("급강하 공격 발동 주기/쿨타임 (초)")]
        public float DiveInterval;

        [Tooltip("보스 기체의 트랙터 빔 사용 확률 (0.0 ~ 1.0)")]
        public float TractorBeamProbability;

        public DifficultyRankParameters(int rank, float diveSpeed, float bulletSpeed, int maxConcurrentDives, float diveInterval, float tractorBeamProbability)
        {
            Rank = rank;
            DiveSpeed = diveSpeed;
            BulletSpeed = bulletSpeed;
            MaxConcurrentDives = maxConcurrentDives;
            DiveInterval = diveInterval;
            TractorBeamProbability = tractorBeamProbability;
        }

        public bool Equals(DifficultyRankParameters other)
        {
            return Rank == other.Rank &&
                   Mathf.Approximately(DiveSpeed, other.DiveSpeed) &&
                   Mathf.Approximately(BulletSpeed, other.BulletSpeed) &&
                   MaxConcurrentDives == other.MaxConcurrentDives &&
                   Mathf.Approximately(DiveInterval, other.DiveInterval) &&
                   Mathf.Approximately(TractorBeamProbability, other.TractorBeamProbability);
        }

        public override bool Equals(object obj)
        {
            return obj is DifficultyRankParameters other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rank, DiveSpeed, BulletSpeed, MaxConcurrentDives, DiveInterval, TractorBeamProbability);
        }

        public static bool operator ==(DifficultyRankParameters left, DifficultyRankParameters right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DifficultyRankParameters left, DifficultyRankParameters right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 스테이지 번호, 생존 시간, 사망 횟수를 종합하여 실시간 동적 난이도 랭크(Rank 1~32)를 산출하고
    /// 적 비행속도, 탄속, 다이브 쿨타임, 동시 다이브 수, 트랙터 빔 확률을 가변 제어하는 시스템 매니저입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class DifficultyRankManager : MonoBehaviour
    {
        // -------------------------------------------------------------
        // 1. 상수 정의 (Constants)
        // -------------------------------------------------------------
        public const int MinRank = 1;
        public const int MaxRank = 32;
        public const float SurvivalSecondsPerRank = 30.0f;
        public const int StageRankMultiplier = 2;
        public const int DeathPenaltyMultiplier = 3;

        // -------------------------------------------------------------
        // 2. 인스펙터 직렬화 필드 (Serialized Fields: [SerializeField] private)
        // -------------------------------------------------------------
        [Header("Rank Calculation Settings")]
        [Tooltip("매 프레임 Update에서 생존 시간을 누적하고 랭크를 자동 갱신할지 여부")]
        [SerializeField] private bool _autoUpdateInGame = true;

        [Header("Manager References")]
        [Tooltip("스테이지 매니저 참조")]
        [SerializeField] private StageManager _stageManager;

        [Tooltip("적 급강하 컨트롤러 참조")]
        [SerializeField] private EnemyDiveController _enemyDiveController;

        [Tooltip("플레이어 체력/사망 컴포넌트 참조")]
        [SerializeField] private PlayerHealth _playerHealth;

        // -------------------------------------------------------------
        // 3. 런타임 상태 필드 (Runtime State Fields)
        // -------------------------------------------------------------
        private int _currentStage = 1;
        private float _survivalSeconds = 0f;
        private int _deathCount = 0;
        private int _currentRank = 2;
        private DifficultyRankParameters _currentParameters;
        private bool _isManualRankOverride = false;
        private int _manualRank = 1;

        // -------------------------------------------------------------
        // 4. 프로퍼티 (Properties)
        // -------------------------------------------------------------
        public static DifficultyRankManager Instance { get; private set; }

        public int CurrentRank => _currentRank;
        public int CurrentStage => _currentStage;
        public float SurvivalSeconds => _survivalSeconds;
        public int DeathCount => _deathCount;
        public DifficultyRankParameters CurrentParameters => _currentParameters;
        public bool IsManualRankOverride => _isManualRankOverride;
        public bool AutoUpdateInGame
        {
            get => _autoUpdateInGame;
            set => _autoUpdateInGame = value;
        }

        public StageManager StageManager
        {
            get => _stageManager;
            set
            {
                UnbindStageManager();
                _stageManager = value;
                BindStageManager();
            }
        }

        public EnemyDiveController EnemyDiveController
        {
            get => _enemyDiveController;
            set => _enemyDiveController = value;
        }

        public PlayerHealth PlayerHealth
        {
            get => _playerHealth;
            set
            {
                UnbindPlayerHealth();
                _playerHealth = value;
                BindPlayerHealth();
            }
        }

        // -------------------------------------------------------------
        // 5. C# 이벤트 (Events / Actions)
        // -------------------------------------------------------------
        /// <summary>
        /// 난이도 랭크 수치가 변경되었을 때 발행되는 이벤트 (변경된 랭크 전달)
        /// </summary>
        public event Action<int> OnRankChanged;

        /// <summary>
        /// 난이도 가변 파라미터가 갱신되었을 때 발행되는 이벤트
        /// </summary>
        public event Action<DifficultyRankParameters> OnParametersChanged;

        // -------------------------------------------------------------
        // 6. 유니티 생명주기 메서드 (Lifecycle Methods)
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

            ResolveReferences();
            Initialize(_currentStage);
        }

        private void OnEnable()
        {
            BindStageManager();
            BindPlayerHealth();
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindStageManager();
            UnbindPlayerHealth();

            OnRankChanged = null;
            OnParametersChanged = null;
        }

        private void Update()
        {
            if (!_autoUpdateInGame || _isManualRankOverride)
            {
                return;
            }

            // 플레이어가 살아있고 스테이지가 진행 중일 때만 생존 시간 누적
            bool isPlaying = true;
            if (_stageManager != null && !_stageManager.IsStageInProgress)
            {
                isPlaying = false;
            }
            if (_playerHealth != null && !_playerHealth.IsAlive)
            {
                isPlaying = false;
            }

            if (isPlaying)
            {
                UpdateSurvivalTime(Time.deltaTime);
            }
        }

        // -------------------------------------------------------------
        // 7. 참조 자동 바인딩 및 초기화 (Initialization & References)
        // -------------------------------------------------------------
        /// <summary>
        /// 누락된 매니저 및 컴포넌트 참조를 씬에서 탐색하여 바인딩합니다.
        /// </summary>
        public void ResolveReferences()
        {
            if (_stageManager == null)
            {
                _stageManager = StageManager.Instance != null ? StageManager.Instance : FindAnyObjectByType<StageManager>();
            }

            if (_enemyDiveController == null)
            {
                _enemyDiveController = FindAnyObjectByType<EnemyDiveController>();
            }

            if (_playerHealth == null)
            {
                _playerHealth = FindAnyObjectByType<PlayerHealth>();
            }
        }

        /// <summary>
        /// 동적 난이도 랭크 매니저의 상태를 초기화합니다 (런타임 및 단위 테스트용).
        /// </summary>
        /// <param name="startingStage">시작 스테이지 번호</param>
        public void Initialize(int startingStage = 1)
        {
            Instance = this;
            _currentStage = Mathf.Max(1, startingStage);
            _survivalSeconds = 0f;
            _deathCount = 0;
            _isManualRankOverride = false;

            RecalculateRank(true);
        }

        /// <summary>
        /// 랭크 시스템을 완전히 리셋합니다.
        /// </summary>
        public void ResetRank()
        {
            Initialize(1);
        }

        // -------------------------------------------------------------
        // 8. 랭크 계산 및 상태 갱신 (Rank Calculation Logic)
        // -------------------------------------------------------------
        /// <summary>
        /// 스테이지 번호, 생존 시간(초), 사망 횟수를 기반으로 총 난이도 랭크를 계산하는 순수 수학 공식입니다.
        /// 공식: Clamp(CurrentStage * 2 + Floor(SurvivalSeconds / 30) - DeathCount * 3, 1, 32)
        /// </summary>
        public static int CalculateRank(int stage, float survivalSeconds, int deathCount)
        {
            int validStage = Mathf.Max(1, stage);
            float validSurvival = Mathf.Max(0f, survivalSeconds);
            int validDeaths = Mathf.Max(0, deathCount);

            int baseStageRank = validStage * StageRankMultiplier;
            int survivalRank = Mathf.FloorToInt(validSurvival / SurvivalSecondsPerRank);
            int deathPenalty = validDeaths * DeathPenaltyMultiplier;

            int totalRank = baseStageRank + survivalRank - deathPenalty;
            return Mathf.Clamp(totalRank, MinRank, MaxRank);
        }

        /// <summary>
        /// 난이도 랭크에 대응하는 5대 게임플레이 수치(비행속도, 탄속, 동시 다이브 수, 쿨타임, 트랙터 빔 확률)를 반환합니다.
        /// </summary>
        public static DifficultyRankParameters GetParametersForRank(int rank)
        {
            int clampedRank = Mathf.Clamp(rank, MinRank, MaxRank);

            if (clampedRank <= 5)
            {
                // Rank 1 ~ 5
                return new DifficultyRankParameters(clampedRank, 8.33f, 11.11f, 2, 3.0f, 0.20f);
            }
            else if (clampedRank <= 15)
            {
                // Rank 6 ~ 15
                return new DifficultyRankParameters(clampedRank, 12.50f, 15.28f, 3, 1.8f, 0.50f);
            }
            else if (clampedRank <= 25)
            {
                // Rank 16 ~ 25
                return new DifficultyRankParameters(clampedRank, 15.50f, 18.00f, 4, 1.2f, 0.75f);
            }
            else
            {
                // Rank 26 ~ 32
                return new DifficultyRankParameters(clampedRank, 17.36f, 20.83f, 5, 0.8f, 0.90f);
            }
        }

        /// <summary>
        /// 생존 시간을 델타 시간만큼 누적하고 필요 시 랭크를 갱신합니다.
        /// </summary>
        public void UpdateSurvivalTime(float deltaTime)
        {
            if (deltaTime <= 0f || _isManualRankOverride)
            {
                return;
            }

            _survivalSeconds += deltaTime;
            RecalculateRank(false);
        }

        /// <summary>
        /// 플레이어 피격/사망 시 호출되어 사망 횟수를 1 증가시키고 데스 페널티(-3 랭크)를 적용합니다.
        /// </summary>
        public void RecordDeath()
        {
            _deathCount++;
            RecalculateRank(false);
        }

        /// <summary>
        /// 스테이지 번호를 갱신하고 기본 스테이지 랭크를 재산출합니다.
        /// </summary>
        public void SetStage(int stage)
        {
            _currentStage = Mathf.Max(1, stage);
            RecalculateRank(false);
        }

        /// <summary>
        /// 디버그 또는 테스트용으로 동적 산출 공식을 우회하고 특정 랭크로 강제 고정합니다.
        /// </summary>
        public void SetManualRank(int rank)
        {
            _isManualRankOverride = true;
            _manualRank = Mathf.Clamp(rank, MinRank, MaxRank);
            ApplyRank(_manualRank, true);
        }

        /// <summary>
        /// 수동 오버라이드를 해제하고 정상 동적 산출 공식 모드로 복귀합니다.
        /// </summary>
        public void ClearManualRankOverride()
        {
            _isManualRankOverride = false;
            RecalculateRank(true);
        }

        /// <summary>
        /// 현재 상태를 바탕으로 랭크 및 파라미터를 재계산하고 게임플레이에 적용합니다.
        /// </summary>
        public void RecalculateRank(bool forceNotify = false)
        {
            int newRank = _isManualRankOverride
                ? _manualRank
                : CalculateRank(_currentStage, _survivalSeconds, _deathCount);

            ApplyRank(newRank, forceNotify);
        }

        private void ApplyRank(int newRank, bool forceNotify)
        {
            bool rankChanged = (_currentRank != newRank) || forceNotify;
            _currentRank = newRank;

            DifficultyRankParameters newParams = GetParametersForRank(_currentRank);
            bool paramsChanged = !_currentParameters.Equals(newParams) || forceNotify;
            _currentParameters = newParams;

            ApplyParametersToGame();

            if (rankChanged)
            {
                OnRankChanged?.Invoke(_currentRank);
            }

            if (paramsChanged)
            {
                OnParametersChanged?.Invoke(_currentParameters);
            }
        }

        /// <summary>
        /// 현재 산출된 동적 난이도 파라미터를 관련 게임플레이 컴포넌트에 주입합니다.
        /// </summary>
        public void ApplyParametersToGame()
        {
            if (_enemyDiveController != null)
            {
                _enemyDiveController.DiveSpeed = _currentParameters.DiveSpeed;
                _enemyDiveController.DiveInterval = _currentParameters.DiveInterval;
                _enemyDiveController.MaxConcurrentDives = _currentParameters.MaxConcurrentDives;
            }
        }

        // -------------------------------------------------------------
        // 9. 내부 이벤트 핸들러 (Internal Event Handlers)
        // -------------------------------------------------------------
        private void BindStageManager()
        {
            if (_stageManager != null)
            {
                _stageManager.OnStageChanged += HandleStageChanged;
            }
        }

        private void UnbindStageManager()
        {
            if (_stageManager != null)
            {
                _stageManager.OnStageChanged -= HandleStageChanged;
            }
        }

        private void BindPlayerHealth()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnLivesChanged += HandlePlayerLivesChanged;
            }
        }

        private void UnbindPlayerHealth()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnLivesChanged -= HandlePlayerLivesChanged;
            }
        }

        private void HandleStageChanged(int newStage)
        {
            SetStage(newStage);
        }

        private int _lastTrackedLives = -1;
        private void HandlePlayerLivesChanged(int currentLives)
        {
            if (_lastTrackedLives != -1 && currentLives < _lastTrackedLives)
            {
                RecordDeath();
            }
            _lastTrackedLives = currentLives;
        }
    }
}
