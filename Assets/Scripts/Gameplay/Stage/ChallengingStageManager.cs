using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Score;

namespace Galaga.Gameplay.Stage
{
    /// <summary>
    /// 챌린징 스테이지 결과 데이터 구조체입니다.
    /// </summary>
    [Serializable]
    public struct ChallengingStageResult
    {
        [Tooltip("스테이지 번호")]
        public int StageNumber;

        [Tooltip("총 출현 적 기체 수 (기본 40기)")]
        public int TotalEnemies;

        [Tooltip("플레이어가 격파한 적 기체 수")]
        public int HitCount;

        [Tooltip("산정된 보너스 점수 (40기 완파 시 10,000점 / 미만 시 격파수 * 100점)")]
        public int BonusScore;

        [Tooltip("40기 전멸(PERFECT) 달성 여부")]
        public bool IsPerfect;

        public ChallengingStageResult(int stageNumber, int totalEnemies, int hitCount, int bonusScore, bool isPerfect)
        {
            StageNumber = stageNumber;
            TotalEnemies = totalEnemies;
            HitCount = hitCount;
            BonusScore = bonusScore;
            IsPerfect = isPerfect;
        }
    }

    /// <summary>
    /// 4n - 1 주기(Stage 3, 7, 11, 15...)에 진행되는 챌린징 스테이지(보너스 라운드)를 총괄하는 매니저입니다.
    /// 5개 웨이브(각 8기 = 총 40기)의 노탄환 비행 궤적 생성, 적 격파 수 추적, PERFECT(10,000점) 및 스페셜 보너스 산정을 수행합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChallengingStageManager : MonoBehaviour
    {
        // -------------------------------------------------------------
        // 1. 인스펙터 직렬화 필드 (Serialized Fields: [SerializeField] private)
        // -------------------------------------------------------------
        [Header("Enemy Prefabs")]
        [Tooltip("자코 완제품 프리팹 (PF_Enemy_Zako)")]
        [SerializeField] private GameObject _zakoPrefab;

        [Tooltip("고에이 완제품 프리팹 (PF_Enemy_Goei)")]
        [SerializeField] private GameObject _goeiPrefab;

        [Tooltip("보스 갤러그 완제품 프리팹 (PF_Enemy_Boss)")]
        [SerializeField] private GameObject _bossPrefab;

        [Header("Challenging Configuration")]
        [Tooltip("총 진행 웨이브 수 (기본 5)")]
        [SerializeField] private int _totalWaves = 5;

        [Tooltip("웨이브 당 적 기체 수 (기본 8)")]
        [SerializeField] private int _enemiesPerWave = 8;

        [Tooltip("웨이브 내 기체 간 스폰 간격(초)")]
        [SerializeField] private float _spawnInterval = 0.18f;

        [Tooltip("웨이브 간 대기 시간 간격(초)")]
        [SerializeField] private float _waveInterval = 1.0f;

        [Tooltip("챌린징 비행 속도 (units/sec)")]
        [SerializeField] private float _flightSpeed = 12.0f;

        [Tooltip("챌린징 종료 후 결과 표시 및 다음 스테이지 전환 대기 시간(초)")]
        [SerializeField] private float _postStageDelay = 3.0f;

        [Header("References")]
        [Tooltip("스폰된 적들을 담을 부모 트랜스폼")]
        [SerializeField] private Transform _enemyContainer;

        [Tooltip("스테이지 매니저 참조")]
        [SerializeField] private StageManager _stageManager;

        [Tooltip("스코어 매니저 참조")]
        [SerializeField] private ScoreManager _scoreManager;

        // -------------------------------------------------------------
        // 2. 런타임 상태 필드 (Runtime State Fields)
        // -------------------------------------------------------------
        private int _currentStageNumber = 3;
        private int _currentWaveIndex = 0;
        private int _totalSpawnedCount = 0;
        private int _hitCount = 0;
        private int _escapedCount = 0;
        private bool _isChallengingInProgress = false;
        private bool _isChallengingClearing = false;

        private readonly List<EnemyBase> _activeChallengingEnemies = new List<EnemyBase>();
        private Coroutine _challengingCoroutine;
        private Coroutine _finishCoroutine;

        // -------------------------------------------------------------
        // 3. 프로퍼티 (Properties)
        // -------------------------------------------------------------
        public static ChallengingStageManager Instance { get; private set; }

        public int CurrentStageNumber => _currentStageNumber;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int TotalSpawnedCount => _totalSpawnedCount;
        public int HitCount => _hitCount;
        public int EscapedCount => _escapedCount;
        public int TotalChallengingEnemies => _totalWaves * _enemiesPerWave;
        public bool IsChallengingInProgress => _isChallengingInProgress;
        public bool IsChallengingClearing => _isChallengingClearing;
        public float FlightSpeed
        {
            get => _flightSpeed;
            set => _flightSpeed = Mathf.Max(1f, value);
        }
        public float PostStageDelay
        {
            get => _postStageDelay;
            set => _postStageDelay = Mathf.Max(0f, value);
        }

        public GameObject ZakoPrefab
        {
            get => _zakoPrefab;
            set => _zakoPrefab = value;
        }

        public GameObject GoeiPrefab
        {
            get => _goeiPrefab;
            set => _goeiPrefab = value;
        }

        public GameObject BossPrefab
        {
            get => _bossPrefab;
            set => _bossPrefab = value;
        }

        public StageManager StageManager
        {
            get => _stageManager;
            set => _stageManager = value;
        }

        public ScoreManager ScoreManager
        {
            get => _scoreManager;
            set => _scoreManager = value;
        }

        public IReadOnlyList<EnemyBase> ActiveChallengingEnemies => _activeChallengingEnemies;

        // -------------------------------------------------------------
        // 4. C# 이벤트 (Events / Actions)
        // -------------------------------------------------------------
        /// <summary>
        /// 챌린징 스테이지가 시작될 때 발행되는 이벤트 (스테이지 번호 전달)
        /// </summary>
        public event Action<int> OnChallengingStageStarted;

        /// <summary>
        /// 개별 챌린징 웨이브가 시작될 때 발행되는 이벤트 (웨이브 번호 1~5 전달)
        /// </summary>
        public event Action<int> OnChallengingWaveStarted;

        /// <summary>
        /// 개별 챌린징 웨이브 스폰이 완료되었을 때 발행되는 이벤트
        /// </summary>
        public event Action<int> OnChallengingWaveCompleted;

        /// <summary>
        /// 챌린징 스테이지 중 적 격파 수가 증가할 때마다 발행되는 이벤트 (현재 격파 수 전달)
        /// </summary>
        public event Action<int> OnChallengingHitCountChanged;

        /// <summary>
        /// 40기 전원 처리(격파 또는 이탈) 완료 후 보너스 정산 결과가 산출되었을 때 발행되는 이벤트
        /// </summary>
        public event Action<ChallengingStageResult> OnChallengingStageCompleted;

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
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            StopChallengingStage();

            OnChallengingStageStarted = null;
            OnChallengingWaveStarted = null;
            OnChallengingWaveCompleted = null;
            OnChallengingHitCountChanged = null;
            OnChallengingStageCompleted = null;
        }

        // -------------------------------------------------------------
        // 6. 초기화 및 참조 바인딩 (Initialization & References)
        // -------------------------------------------------------------
        /// <summary>
        /// 필요한 매니저 참조가 누락된 경우 씬에서 자동 탐색하여 바인딩합니다.
        /// </summary>
        public void ResolveReferences()
        {
            if (_stageManager == null)
            {
                _stageManager = StageManager.Instance != null ? StageManager.Instance : FindAnyObjectByType<StageManager>();
            }

            if (_scoreManager == null)
            {
                _scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
            }
        }

        // -------------------------------------------------------------
        // 7. 챌린징 스테이지 진행 제어 (Stage Progression)
        // -------------------------------------------------------------
        /// <summary>
        /// 지정된 스테이지 번호로 챌린징 스테이지 시퀀스를 시작합니다.
        /// </summary>
        /// <param name="stageNumber">현재 스테이지 번호</param>
        public void StartChallengingStage(int stageNumber)
        {
            StopChallengingStage();
            ResolveReferences();

            _currentStageNumber = Mathf.Max(1, stageNumber);
            _currentWaveIndex = 0;
            _totalSpawnedCount = 0;
            _hitCount = 0;
            _escapedCount = 0;
            _isChallengingInProgress = true;
            _isChallengingClearing = false;

            // 전역 적 사격 차단 (노탄환 비행 모드)
            EnemyShooting.GlobalShootingBlocked = true;

            OnChallengingStageStarted?.Invoke(_currentStageNumber);
            OnChallengingHitCountChanged?.Invoke(_hitCount);

            _challengingCoroutine = StartCoroutine(ChallengingSequenceRoutine());
        }

        /// <summary>
        /// 진행 중인 챌린징 스테이지를 즉시 중단하고 정리합니다.
        /// </summary>
        public void StopChallengingStage()
        {
            if (_challengingCoroutine != null)
            {
                StopCoroutine(_challengingCoroutine);
                _challengingCoroutine = null;
            }

            if (_finishCoroutine != null)
            {
                StopCoroutine(_finishCoroutine);
                _finishCoroutine = null;
            }

            EnemyShooting.GlobalShootingBlocked = false;
            _isChallengingInProgress = false;
            _isChallengingClearing = false;

            ClearActiveEnemies();
        }

        private IEnumerator ChallengingSequenceRoutine()
        {
            int totalEnemiesTarget = _totalWaves * _enemiesPerWave;

            for (int wave = 1; wave <= _totalWaves; wave++)
            {
                _currentWaveIndex = wave;
                OnChallengingWaveStarted?.Invoke(wave);

                yield return StartCoroutine(SpawnChallengingWaveRoutine(wave));

                OnChallengingWaveCompleted?.Invoke(wave);

                if (wave < _totalWaves)
                {
                    yield return new WaitForSeconds(_waveInterval);
                }
            }

            // 모든 적(40기)이 격파되거나 화면 밖으로 이탈할 때까지 대기
            while (_isChallengingInProgress && (_hitCount + _escapedCount < _totalSpawnedCount || _totalSpawnedCount < totalEnemiesTarget))
            {
                yield return null;
            }

            if (_isChallengingInProgress && !_isChallengingClearing)
            {
                FinishChallengingStage();
            }

            _challengingCoroutine = null;
        }

        private IEnumerator SpawnChallengingWaveRoutine(int waveIndex)
        {
            EnemyType[] waveTypes = GetChallengingWaveEnemyTypes(waveIndex);

            for (int i = 0; i < waveTypes.Length; i++)
            {
                EnemyType type = waveTypes[i];
                SpawnAndLaunchChallengingEnemy(waveIndex, i, type);
                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        /// <summary>
        /// 단일 챌린징 적 기체를 스폰하고 궤적 비행을 시작합니다.
        /// </summary>
        public EnemyBase SpawnAndLaunchChallengingEnemy(int waveIndex, int subIndex, EnemyType type)
        {
            GameObject prefab = GetPrefabForType(type);
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(prefab, _enemyContainer);
            EnemyBase enemy = instance.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                enemy = instance.AddComponent<EnemyBase>();
            }

            // 사격 차단 확인
            EnemyShooting shooting = instance.GetComponent<EnemyShooting>();
            if (shooting != null)
            {
                shooting.IsShootingEnabled = false;
            }

            _activeChallengingEnemies.Add(enemy);
            _totalSpawnedCount++;

            // 챌린징 베지어 궤적 생성
            BezierSegment[] trajectory = CreateChallengingTrajectory(waveIndex, subIndex);

            enemy.SetState(EnemyState.Diving);
            enemy.OnDestroyed += HandleEnemyDefeated;

            BezierPathFollower follower = enemy.PathFollower;
            if (follower == null)
            {
                follower = enemy.GetComponent<BezierPathFollower>();
            }

            if (follower != null)
            {
                follower.SetPath(trajectory, _flightSpeed, loop: false);
                follower.RotateAlongPath = true;
                follower.RotationOffset = -90f;

                Action onCompleted = null;
                onCompleted = () =>
                {
                    follower.OnPathCompleted -= onCompleted;
                    HandleEnemyEscaped(enemy);
                };
                follower.OnPathCompleted += onCompleted;
                follower.Play();
            }

            return enemy;
        }

        private void HandleEnemyDefeated(EnemyBase enemy)
        {
            if (enemy != null)
            {
                enemy.OnDestroyed -= HandleEnemyDefeated;
                _activeChallengingEnemies.Remove(enemy);
            }

            if (!_isChallengingInProgress)
            {
                return;
            }

            _hitCount++;
            OnChallengingHitCountChanged?.Invoke(_hitCount);

            CheckCompletionCondition();
        }

        private void HandleEnemyEscaped(EnemyBase enemy)
        {
            if (enemy != null)
            {
                enemy.OnDestroyed -= HandleEnemyDefeated;
                _activeChallengingEnemies.Remove(enemy);
                enemy.gameObject.SetActive(false);
            }

            if (!_isChallengingInProgress)
            {
                return;
            }

            _escapedCount++;
            CheckCompletionCondition();
        }

        private void CheckCompletionCondition()
        {
            int totalEnemiesTarget = _totalWaves * _enemiesPerWave;
            if (_totalSpawnedCount >= totalEnemiesTarget && (_hitCount + _escapedCount >= totalEnemiesTarget))
            {
                if (_isChallengingInProgress && !_isChallengingClearing)
                {
                    FinishChallengingStage();
                }
            }
        }

        /// <summary>
        /// 40기 전원 처리 완료 후 보너스 점수를 정산하고 완료 이벤트를 발행합니다.
        /// </summary>
        public void FinishChallengingStage()
        {
            if (_isChallengingClearing)
            {
                return;
            }

            _isChallengingClearing = true;
            _isChallengingInProgress = false;
            EnemyShooting.GlobalShootingBlocked = false;

            int totalEnemiesTarget = _totalWaves * _enemiesPerWave;
            int bonusScore = ScoreManager.CalculateChallengingBonus(_hitCount, totalEnemiesTarget);
            bool isPerfect = (_hitCount >= totalEnemiesTarget);

            if (_scoreManager != null)
            {
                _scoreManager.AddScore(bonusScore);
            }
            else if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(bonusScore);
            }

            ChallengingStageResult result = new ChallengingStageResult(
                _currentStageNumber,
                totalEnemiesTarget,
                _hitCount,
                bonusScore,
                isPerfect
            );

            OnChallengingStageCompleted?.Invoke(result);

            if (_finishCoroutine != null)
            {
                StopCoroutine(_finishCoroutine);
            }
            _finishCoroutine = StartCoroutine(ChallengingEndRoutine());
        }

        private IEnumerator ChallengingEndRoutine()
        {
            if (_postStageDelay > 0f)
            {
                yield return new WaitForSeconds(_postStageDelay);
            }

            _finishCoroutine = null;

            if (_stageManager != null)
            {
                _stageManager.AdvanceToNextStage();
            }
            else if (StageManager.Instance != null)
            {
                StageManager.Instance.AdvanceToNextStage();
            }
        }

        private void ClearActiveEnemies()
        {
            for (int i = 0; i < _activeChallengingEnemies.Count; i++)
            {
                if (_activeChallengingEnemies[i] != null)
                {
                    _activeChallengingEnemies[i].OnDestroyed -= HandleEnemyDefeated;
                }
            }
            _activeChallengingEnemies.Clear();
        }

        // -------------------------------------------------------------
        // 8. 웨이브 구성 및 3차 베지어 궤적 수학 (Wave Configurations & Trajectories)
        // -------------------------------------------------------------
        /// <summary>
        /// 챌린징 스테이지 각 웨이브 번호(1~5)에 대응하는 8기 적 기체 구성을 반환합니다.
        /// Wave 1: 자코 8기
        /// Wave 2: 자코 8기
        /// Wave 3: 고에이 8기
        /// Wave 4: 고에이 8기
        /// Wave 5: 보스 4기 + 고에이 4기
        /// </summary>
        public static EnemyType[] GetChallengingWaveEnemyTypes(int waveIndex)
        {
            switch (waveIndex)
            {
                case 1:
                case 2:
                    return new EnemyType[]
                    {
                        EnemyType.Zako, EnemyType.Zako, EnemyType.Zako, EnemyType.Zako,
                        EnemyType.Zako, EnemyType.Zako, EnemyType.Zako, EnemyType.Zako
                    };

                case 3:
                case 4:
                    return new EnemyType[]
                    {
                        EnemyType.Goei, EnemyType.Goei, EnemyType.Goei, EnemyType.Goei,
                        EnemyType.Goei, EnemyType.Goei, EnemyType.Goei, EnemyType.Goei
                    };

                case 5:
                    return new EnemyType[]
                    {
                        EnemyType.BossGalaga, EnemyType.BossGalaga, EnemyType.BossGalaga, EnemyType.BossGalaga,
                        EnemyType.Goei, EnemyType.Goei, EnemyType.Goei, EnemyType.Goei
                    };

                default:
                    return Array.Empty<EnemyType>();
            }
        }

        /// <summary>
        /// 챌린징 스테이지 웨이브별 특수 베지어 비행 궤적을 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateChallengingTrajectory(int waveIndex, int subIndex)
        {
            float xOffset = (subIndex - 3.5f) * 0.15f;

            switch (waveIndex)
            {
                case 1: // Wave 1: 좌측 상단 진입 후 중앙 8자 선회 및 우하단 이탈
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(-6f + xOffset, 11f),
                        new Vector2(-4f + xOffset, 5f),
                        new Vector2(-3f, 1f),
                        new Vector2(0f, 0f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(0f, 0f),
                        new Vector2(3f, -1f),
                        new Vector2(4f, -4f),
                        new Vector2(0f, -4.5f)
                    );
                    BezierSegment seg3 = new BezierSegment(
                        new Vector2(0f, -4.5f),
                        new Vector2(-4f, -4f),
                        new Vector2(-2f, -1f),
                        new Vector2(0f, 1f)
                    );
                    BezierSegment seg4 = new BezierSegment(
                        new Vector2(0f, 1f),
                        new Vector2(3f, 3f),
                        new Vector2(6f + xOffset, -4f),
                        new Vector2(9f + xOffset, -11f)
                    );
                    return new BezierSegment[] { seg1, seg2, seg3, seg4 };
                }

                case 2: // Wave 2: 우측 상단 진입 후 중앙 8자 선회 및 좌하단 이탈 (Wave 1 대칭)
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(6f - xOffset, 11f),
                        new Vector2(4f - xOffset, 5f),
                        new Vector2(3f, 1f),
                        new Vector2(0f, 0f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(0f, 0f),
                        new Vector2(-3f, -1f),
                        new Vector2(-4f, -4f),
                        new Vector2(0f, -4.5f)
                    );
                    BezierSegment seg3 = new BezierSegment(
                        new Vector2(0f, -4.5f),
                        new Vector2(4f, -4f),
                        new Vector2(2f, -1f),
                        new Vector2(0f, 1f)
                    );
                    BezierSegment seg4 = new BezierSegment(
                        new Vector2(0f, 1f),
                        new Vector2(-3f, 3f),
                        new Vector2(-6f - xOffset, -4f),
                        new Vector2(-9f - xOffset, -11f)
                    );
                    return new BezierSegment[] { seg1, seg2, seg3, seg4 };
                }

                case 3: // Wave 3: 좌측 하단 진입 후 상단 루프 및 우상단 이탈
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(-8f + xOffset, -6f),
                        new Vector2(-4f + xOffset, -2f),
                        new Vector2(-2f, 3f),
                        new Vector2(0f, 6f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(0f, 6f),
                        new Vector2(2.5f, 8f),
                        new Vector2(4.5f, 6f),
                        new Vector2(2.5f, 4f)
                    );
                    BezierSegment seg3 = new BezierSegment(
                        new Vector2(2.5f, 4f),
                        new Vector2(0f, 2f),
                        new Vector2(5f + xOffset, 7f),
                        new Vector2(9f + xOffset, 11f)
                    );
                    return new BezierSegment[] { seg1, seg2, seg3 };
                }

                case 4: // Wave 4: 우측 하단 진입 후 상단 루프 및 좌상단 이탈 (Wave 3 대칭)
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(8f - xOffset, -6f),
                        new Vector2(4f - xOffset, -2f),
                        new Vector2(2f, 3f),
                        new Vector2(0f, 6f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(0f, 6f),
                        new Vector2(-2.5f, 8f),
                        new Vector2(-4.5f, 6f),
                        new Vector2(-2.5f, 4f)
                    );
                    BezierSegment seg3 = new BezierSegment(
                        new Vector2(-2.5f, 4f),
                        new Vector2(0f, 2f),
                        new Vector2(-5f - xOffset, 7f),
                        new Vector2(-9f - xOffset, 11f)
                    );
                    return new BezierSegment[] { seg1, seg2, seg3 };
                }

                case 5: // Wave 5: 중앙 좌우 동시 교차 분할 진입
                {
                    if (subIndex % 2 == 0)
                    {
                        // 좌측 상단 진입 ➔ 우측 대각 관통 후 우하단 이탈
                        BezierSegment seg1 = new BezierSegment(
                            new Vector2(-3.5f + xOffset, 11f),
                            new Vector2(-2f, 6f),
                            new Vector2(1f, 3f),
                            new Vector2(3f, 0f)
                        );
                        BezierSegment seg2 = new BezierSegment(
                            new Vector2(3f, 0f),
                            new Vector2(5f, -3f),
                            new Vector2(6f + xOffset, -7f),
                            new Vector2(8.5f + xOffset, -11f)
                        );
                        return new BezierSegment[] { seg1, seg2 };
                    }
                    else
                    {
                        // 우측 상단 진입 ➔ 좌측 대각 관통 후 좌하단 이탈
                        BezierSegment seg1 = new BezierSegment(
                            new Vector2(3.5f - xOffset, 11f),
                            new Vector2(2f, 6f),
                            new Vector2(-1f, 3f),
                            new Vector2(-3f, 0f)
                        );
                        BezierSegment seg2 = new BezierSegment(
                            new Vector2(-3f, 0f),
                            new Vector2(-5f, -3f),
                            new Vector2(-6f - xOffset, -7f),
                            new Vector2(-8.5f - xOffset, -11f)
                        );
                        return new BezierSegment[] { seg1, seg2 };
                    }
                }

                default:
                {
                    BezierSegment seg = new BezierSegment(
                        new Vector2(0f, 11f),
                        new Vector2(0f, 5f),
                        new Vector2(0f, -5f),
                        new Vector2(0f, -11f)
                    );
                    return new BezierSegment[] { seg };
                }
            }
        }

        private GameObject GetPrefabForType(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Zako:
                    return _zakoPrefab;
                case EnemyType.Goei:
                    return _goeiPrefab;
                case EnemyType.BossGalaga:
                    return _bossPrefab;
                default:
                    return _zakoPrefab;
            }
        }
    }
}
