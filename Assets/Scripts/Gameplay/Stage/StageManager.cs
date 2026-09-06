using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Gameplay.Stage
{
    /// <summary>
    /// 스테이지 번호 관리, 40기 적 생존수 추적 및 전멸/섬멸(Stage Clear) 감지, 
    /// 챌린징 스테이지(4n-1) 분기, 클리어 딜레이 및 다음 스테이지 루프 진입을 총괄하는 매니저입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class StageManager : MonoBehaviour
    {
        // -------------------------------------------------------------
        // 1. 인스펙터 직렬화 필드 (Serialized Fields: [SerializeField] private)
        // -------------------------------------------------------------
        [Header("Stage Settings")]
        [Tooltip("최초 시작 스테이지 번호")]
        [SerializeField] private int _startingStage = 1;

        [Tooltip("스테이지 당 총 스폰 적 수 (기본 40기)")]
        [SerializeField] private int _totalStageEnemies = 40;

        [Tooltip("스테이지 시작 전 준비 딜레이 시간 (초)")]
        [SerializeField] private float _stageStartDelay = 1.0f;

        [Tooltip("적 전멸 후 다음 스테이지로 넘어가기 전 클리어 딜레이 시간 (초)")]
        [SerializeField] private float _stageClearDelay = 2.5f;

        [Tooltip("Awake 시점에 자동으로 1스테이지를 시작할지 여부")]
        [SerializeField] private bool _autoStartOnAwake = false;

        [Tooltip("클리어 시 자동으로 다음 스테이지로 진행할지 여부")]
        [SerializeField] private bool _autoAdvanceToNextStage = true;

        [Header("Manager References")]
        [Tooltip("편대 진입 시퀀스 매니저 참조")]
        [SerializeField] private EntranceSequenceManager _entranceSequenceManager;

        [Tooltip("편대 그리드 매니저 참조")]
        [SerializeField] private FormationGridManager _formationGridManager;

        [Tooltip("적 급강하 컨트롤러 참조")]
        [SerializeField] private EnemyDiveController _enemyDiveController;

        [Tooltip("플레이어 체력/사망 이벤트 참조")]
        [SerializeField] private PlayerHealth _playerHealth;

        // -------------------------------------------------------------
        // 2. 런타임 상태 필드 (Runtime State Fields)
        // -------------------------------------------------------------
        private int _currentStage = 1;
        private int _aliveEnemyCount = 0;
        private int _spawnedEnemyCount = 0;
        private bool _isStageInProgress = false;
        private bool _isStageClearing = false;
        private bool _isChallengingStage = false;
        private bool _isEntranceSequenceFinished = false;

        private readonly List<EnemyBase> _registeredEnemies = new List<EnemyBase>();
        private Coroutine _stageStartCoroutine;
        private Coroutine _stageClearCoroutine;

        // -------------------------------------------------------------
        // 3. 프로퍼티 (Properties)
        // -------------------------------------------------------------
        public static StageManager Instance { get; private set; }

        public int CurrentStage => _currentStage;
        public int AliveEnemyCount => _aliveEnemyCount;
        public int TotalStageEnemies => _totalStageEnemies;
        public int SpawnedEnemyCount => _spawnedEnemyCount;
        public bool IsStageInProgress => _isStageInProgress;
        public bool IsStageClearing => _isStageClearing;
        public bool IsChallengingStage => _isChallengingStage;
        public bool IsEntranceSequenceFinished => _isEntranceSequenceFinished;
        public IReadOnlyList<EnemyBase> RegisteredEnemies => _registeredEnemies;

        public bool AutoAdvanceToNextStage
        {
            get => _autoAdvanceToNextStage;
            set => _autoAdvanceToNextStage = value;
        }

        public float StageClearDelay
        {
            get => _stageClearDelay;
            set => _stageClearDelay = Mathf.Max(0f, value);
        }

        public float StageStartDelay
        {
            get => _stageStartDelay;
            set => _stageStartDelay = Mathf.Max(0f, value);
        }

        public EntranceSequenceManager EntranceSequenceManager
        {
            get => _entranceSequenceManager;
            set
            {
                UnbindEntranceSequenceManager();
                _entranceSequenceManager = value;
                BindEntranceSequenceManager();
            }
        }

        public FormationGridManager FormationGridManager
        {
            get => _formationGridManager;
            set => _formationGridManager = value;
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
        // 4. C# 이벤트 (Events / Actions)
        // -------------------------------------------------------------
        /// <summary>
        /// 스테이지 번호가 변경(설정)되었을 때 발행되는 이벤트 (HUD/Badge 렌더러 동기화용)
        /// </summary>
        public event Action<int> OnStageChanged;

        /// <summary>
        /// 스테이지 시작 루틴이 완료되고 본격적인 교전이 개시될 때 발행되는 이벤트
        /// </summary>
        public event Action<int> OnStageStarted;

        /// <summary>
        /// 40기 적 전멸로 스테이지가 클리어되었을 때 발행되는 이벤트
        /// </summary>
        public event Action<int> OnStageCleared;

        /// <summary>
        /// 생존 적 기체 수가 변경될 때마다 발행되는 이벤트 (현재 생존 기체 수 전달)
        /// </summary>
        public event Action<int> OnEnemyCountChanged;

        /// <summary>
        /// 챌린징 스테이지(4n-1) 여부가 결정될 때 발행되는 이벤트 (true: 챌린징 스테이지, false: 일반 스테이지)
        /// </summary>
        public event Action<bool> OnChallengingStageTriggered;

        /// <summary>
        /// 적 40기 전체 섬멸(0기) 감지 시 즉시 발행되는 이벤트
        /// </summary>
        public event Action OnAllEnemiesDefeated;

        /// <summary>
        /// 신규 적 기체가 스테이지 생존 카운트에 등록될 때 발행되는 이벤트
        /// </summary>
        public event Action<EnemyBase> OnEnemyRegistered;

        /// <summary>
        /// 적 기체가 격파 또는 등록 해제될 때 발행되는 이벤트
        /// </summary>
        public event Action<EnemyBase> OnEnemyUnregistered;

        /// <summary>
        /// 플레이어 잔기 소진으로 게임 오버 시 발행되는 이벤트
        /// </summary>
        public event Action OnGameOver;

        // -------------------------------------------------------------
        // 5. 유니티 생명주기 메서드 (Lifecycle Methods)
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

            if (_autoStartOnAwake)
            {
                Initialize(_startingStage);
                StartStage(_startingStage);
            }
        }

        private void OnEnable()
        {
            BindEntranceSequenceManager();
            BindPlayerHealth();
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            StopAllStageCoroutines();
            UnbindEntranceSequenceManager();
            UnbindPlayerHealth();
            ClearRegisteredEnemies();

            OnStageChanged = null;
            OnStageStarted = null;
            OnStageCleared = null;
            OnEnemyCountChanged = null;
            OnChallengingStageTriggered = null;
            OnAllEnemiesDefeated = null;
            OnEnemyRegistered = null;
            OnEnemyUnregistered = null;
            OnGameOver = null;
        }

        // -------------------------------------------------------------
        // 6. 초기화 및 참조 바인딩 (Initialization & References)
        // -------------------------------------------------------------
        /// <summary>
        /// 매니저 참조가 누락된 경우 씬에서 자동 탐색하여 바인딩합니다.
        /// </summary>
        public void ResolveReferences()
        {
            if (_entranceSequenceManager == null)
            {
                _entranceSequenceManager = FindAnyObjectByType<EntranceSequenceManager>();
            }

            if (_formationGridManager == null)
            {
                _formationGridManager = FindAnyObjectByType<FormationGridManager>();
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
        /// 스테이지 매니저의 상태를 지정된 스테이지 번호로 초기화합니다 (런타임 및 단위 테스트용).
        /// </summary>
        /// <param name="startingStage">초기 스테이지 번호</param>
        public void Initialize(int startingStage = 1)
        {
            Instance = this;
            _currentStage = Mathf.Max(1, startingStage);
            _aliveEnemyCount = 0;
            _spawnedEnemyCount = 0;
            _isStageInProgress = false;
            _isStageClearing = false;
            _isEntranceSequenceFinished = false;
            _isChallengingStage = CheckIsChallengingStage(_currentStage);

            ClearRegisteredEnemies();
            ResolveReferences();

            OnStageChanged?.Invoke(_currentStage);
            OnChallengingStageTriggered?.Invoke(_isChallengingStage);
            OnEnemyCountChanged?.Invoke(_aliveEnemyCount);
        }

        /// <summary>
        /// 게임을 완전히 초기화하고 1스테이지로 리셋합니다.
        /// </summary>
        public void ResetGame()
        {
            StopAllStageCoroutines();
            Initialize(1);
        }

        // -------------------------------------------------------------
        // 7. 스테이지 진행 및 루프 제어 (Stage Progression & Loop)
        // -------------------------------------------------------------
        /// <summary>
        /// 지정된 스테이지 번호로 스테이지를 시작합니다.
        /// </summary>
        /// <param name="stageNumber">시작할 스테이지 번호 (1 이상)</param>
        public void StartStage(int stageNumber)
        {
            StopAllStageCoroutines();
            ClearRegisteredEnemies();

            _currentStage = Mathf.Max(1, stageNumber);
            _aliveEnemyCount = 0;
            _spawnedEnemyCount = 0;
            _isStageInProgress = true;
            _isStageClearing = false;
            _isEntranceSequenceFinished = false;
            _isChallengingStage = CheckIsChallengingStage(_currentStage);

            OnStageChanged?.Invoke(_currentStage);
            OnChallengingStageTriggered?.Invoke(_isChallengingStage);
            OnEnemyCountChanged?.Invoke(_aliveEnemyCount);

            // 편대 그리드 초기화
            if (_formationGridManager != null)
            {
                _formationGridManager.InitializeGrid();
            }

            // 다이브 컨트롤러 초기 상태 정지 (진입 완료 전까지 다이브 대기)
            if (_enemyDiveController != null)
            {
                _enemyDiveController.StopAutoDive();
            }

            _stageStartCoroutine = StartCoroutine(StageStartRoutine());
        }

        /// <summary>
        /// 다음 스테이지 번호로 자동 진입합니다.
        /// </summary>
        public void AdvanceToNextStage()
        {
            StartStage(_currentStage + 1);
        }

        private IEnumerator StageStartRoutine()
        {
            if (_stageStartDelay > 0f)
            {
                yield return new WaitForSeconds(_stageStartDelay);
            }

            OnStageStarted?.Invoke(_currentStage);

            // 진입 시퀀스 시작
            if (_entranceSequenceManager != null)
            {
                _entranceSequenceManager.StartEntranceSequence();
            }
            else
            {
                // 진입 매니저가 없는 테스트/간이 환경에서는 진입 완료로 간주
                _isEntranceSequenceFinished = true;
            }

            _stageStartCoroutine = null;
        }

        // -------------------------------------------------------------
        // 8. 적 기체 등록 및 섬멸 판정 (Enemy Tracking & Annihilation)
        // -------------------------------------------------------------
        /// <summary>
        /// 신규 스폰된 적 기체를 스테이지 관리 목록에 등록하고 사망 이벤트를 구독합니다.
        /// </summary>
        /// <param name="enemy">등록할 적 기체 컴포넌트</param>
        public void RegisterEnemy(EnemyBase enemy)
        {
            if (enemy == null || _registeredEnemies.Contains(enemy))
            {
                return;
            }

            _registeredEnemies.Add(enemy);
            _aliveEnemyCount++;
            _spawnedEnemyCount++;

            enemy.OnDestroyed += HandleEnemyDestroyed;

            OnEnemyRegistered?.Invoke(enemy);
            OnEnemyCountChanged?.Invoke(_aliveEnemyCount);
        }

        /// <summary>
        /// 등록된 적 기체를 관리 목록에서 제외합니다.
        /// </summary>
        /// <param name="enemy">제외할 적 기체 컴포넌트</param>
        public void UnregisterEnemy(EnemyBase enemy)
        {
            if (enemy == null || !_registeredEnemies.Contains(enemy))
            {
                return;
            }

            enemy.OnDestroyed -= HandleEnemyDestroyed;
            _registeredEnemies.Remove(enemy);

            if (_aliveEnemyCount > 0)
            {
                _aliveEnemyCount--;
            }

            OnEnemyUnregistered?.Invoke(enemy);
            OnEnemyCountChanged?.Invoke(_aliveEnemyCount);

            CheckStageClearCondition();
        }

        /// <summary>
        /// 적 기체 격파(사망) 시 호출되어 생존 카운트를 차감하고 섬멸 여부를 판정합니다.
        /// </summary>
        /// <param name="enemy">격파된 적 기체 컴포넌트</param>
        public void HandleEnemyDestroyed(EnemyBase enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.OnDestroyed -= HandleEnemyDestroyed;

            if (_registeredEnemies.Contains(enemy))
            {
                _registeredEnemies.Remove(enemy);
            }

            _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);

            OnEnemyCountChanged?.Invoke(_aliveEnemyCount);

            CheckStageClearCondition();
        }

        /// <summary>
        /// 5개 웨이브 진입 시퀀스가 완료되었음을 알립니다.
        /// </summary>
        public void HandleEntranceSequenceCompleted()
        {
            _isEntranceSequenceFinished = true;
            CheckStageClearCondition();
        }

        /// <summary>
        /// 스테이지 클리어(적 전멸) 조건을 검사하고 만족 시 클리어 시퀀스를 트리거합니다.
        /// </summary>
        public void CheckStageClearCondition()
        {
            if (!_isStageInProgress || _isStageClearing)
            {
                return;
            }

            // 조건: 진입 시퀀스가 끝났거나 정해진 적 수만큼 스폰되었고, 생존 적 수가 0일 때
            bool allSpawned = _isEntranceSequenceFinished || (_spawnedEnemyCount >= _totalStageEnemies);
            if (allSpawned && _aliveEnemyCount <= 0)
            {
                TriggerStageClear();
            }
        }

        /// <summary>
        /// 스테이지 클리어 연출 및 시퀀스를 실행합니다.
        /// </summary>
        private void TriggerStageClear()
        {
            _isStageClearing = true;
            _isStageInProgress = false;

            // 다이브 공격 정지
            if (_enemyDiveController != null)
            {
                _enemyDiveController.StopAutoDive();
            }

            OnAllEnemiesDefeated?.Invoke();
            OnStageCleared?.Invoke(_currentStage);

            if (_stageClearCoroutine != null)
            {
                StopCoroutine(_stageClearCoroutine);
            }

            _stageClearCoroutine = StartCoroutine(StageClearRoutine());
        }

        private IEnumerator StageClearRoutine()
        {
            if (_stageClearDelay > 0f)
            {
                yield return new WaitForSeconds(_stageClearDelay);
            }

            _stageClearCoroutine = null;

            if (_autoAdvanceToNextStage)
            {
                AdvanceToNextStage();
            }
        }

        /// <summary>
        /// 테스트 또는 디버깅용 강제 스테이지 클리어 메서드입니다.
        /// </summary>
        public void ForceStageClear()
        {
            _aliveEnemyCount = 0;
            _isEntranceSequenceFinished = true;
            TriggerStageClear();
        }

        // -------------------------------------------------------------
        // 9. 챌린징 스테이지 판정 수학 공식 (Challenging Stage Formula)
        // -------------------------------------------------------------
        /// <summary>
        /// 스테이지 번호가 챌린징 스테이지(4n - 1 주기: Stage 3, 7, 11, 15, ...)인지 판정합니다.
        /// </summary>
        /// <param name="stageNumber">검사할 스테이지 번호</param>
        /// <returns>챌린징 스테이지 여부</returns>
        public static bool CheckIsChallengingStage(int stageNumber)
        {
            if (stageNumber < 3)
            {
                return false;
            }

            // 4n - 1 공식: (stage + 1) % 4 == 0 (3, 7, 11, 15, 19...)
            return (stageNumber + 1) % 4 == 0;
        }

        // -------------------------------------------------------------
        // 10. 내부 이벤트 바인딩 헬퍼 (Internal Event Helpers)
        // -------------------------------------------------------------
        private void BindEntranceSequenceManager()
        {
            if (_entranceSequenceManager != null)
            {
                _entranceSequenceManager.OnEnemySpawned += RegisterEnemy;
                _entranceSequenceManager.OnSequenceCompleted += HandleEntranceSequenceCompleted;
            }
        }

        private void UnbindEntranceSequenceManager()
        {
            if (_entranceSequenceManager != null)
            {
                _entranceSequenceManager.OnEnemySpawned -= RegisterEnemy;
                _entranceSequenceManager.OnSequenceCompleted -= HandleEntranceSequenceCompleted;
            }
        }

        private void BindPlayerHealth()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDied += HandlePlayerDied;
            }
        }

        private void UnbindPlayerHealth()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDied -= HandlePlayerDied;
            }
        }

        private void HandlePlayerDied()
        {
            _isStageInProgress = false;
            StopAllStageCoroutines();

            if (_enemyDiveController != null)
            {
                _enemyDiveController.StopAutoDive();
            }

            OnGameOver?.Invoke();
        }

        private void ClearRegisteredEnemies()
        {
            for (int i = 0; i < _registeredEnemies.Count; i++)
            {
                if (_registeredEnemies[i] != null)
                {
                    _registeredEnemies[i].OnDestroyed -= HandleEnemyDestroyed;
                }
            }
            _registeredEnemies.Clear();
        }

        private void StopAllStageCoroutines()
        {
            if (_stageStartCoroutine != null)
            {
                StopCoroutine(_stageStartCoroutine);
                _stageStartCoroutine = null;
            }

            if (_stageClearCoroutine != null)
            {
                StopCoroutine(_stageClearCoroutine);
                _stageClearCoroutine = null;
            }
        }
    }
}
