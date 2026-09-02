using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 스테이지 시작 시 40기(Group 1~5)의 적을 순차적으로 스폰하고 베지어 진입 궤적 비행 후 편대 그리드 슬롯에 안착시키는 시퀀스 매니저입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class EntranceSequenceManager : MonoBehaviour
    {
        [Header("Manager References")]
        [Tooltip("편대 그리드 매니저 참조")]
        [SerializeField] private FormationGridManager _gridManager;

        [Tooltip("급강하 공격 컨트롤러 참조 (진입 완료 후 다이브 자동 시작용)")]
        [SerializeField] private EnemyDiveController _diveController;

        [Header("Enemy Prefabs")]
        [Tooltip("자코 완제품 프리팹 (PF_Enemy_Zako)")]
        [SerializeField] private GameObject _zakoPrefab;

        [Tooltip("고에이 완제품 프리팹 (PF_Enemy_Goei)")]
        [SerializeField] private GameObject _goeiPrefab;

        [Tooltip("보스 갤러그 완제품 프리팹 (PF_Enemy_Boss)")]
        [SerializeField] private GameObject _bossPrefab;

        [Header("Container & Spawn Settings")]
        [Tooltip("스폰된 적들을 담을 부모 트랜스폼")]
        [SerializeField] private Transform _enemyContainer;

        [Tooltip("웨이브 내 기체 간 스폰 시간 간격(초)")]
        [SerializeField] private float _spawnInterval = 0.2f;

        [Tooltip("웨이브(Group) 간 대기 시간 간격(초)")]
        [SerializeField] private float _waveInterval = 1.2f;

        [Tooltip("씬 시작 시 자동으로 진입 시퀀스를 시작할지 여부")]
        [SerializeField] private bool _autoStartOnPlay = true;

        private bool _isSequenceRunning = false;
        private int _currentWaveIndex = 0;
        private int _totalSpawnedCount = 0;
        private int _totalArrivedCount = 0;
        private Coroutine _sequenceCoroutine;
        private readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>();

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnSequenceCompleted;
        public event Action<EnemyBase> OnEnemySpawned;

        public FormationGridManager GridManager
        {
            get => _gridManager;
            set => _gridManager = value;
        }

        public EnemyDiveController DiveController
        {
            get => _diveController;
            set => _diveController = value;
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

        public float SpawnInterval
        {
            get => _spawnInterval;
            set => _spawnInterval = Mathf.Max(0.01f, value);
        }

        public float WaveInterval
        {
            get => _waveInterval;
            set => _waveInterval = Mathf.Max(0.1f, value);
        }

        public bool IsSequenceRunning => _isSequenceRunning;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int TotalSpawnedCount => _totalSpawnedCount;
        public int TotalArrivedCount => _totalArrivedCount;
        public IReadOnlyList<EnemyBase> ActiveEnemies => _activeEnemies;

        private void Start()
        {
            if (_diveController == null)
            {
                _diveController = GetComponent<EnemyDiveController>();
            }

            if (_autoStartOnPlay)
            {
                StartEntranceSequence();
            }
        }

        private void OnDisable()
        {
            StopEntranceSequence();
        }

        /// <summary>
        /// 전체 5개 웨이브 편대 진입 시퀀스를 시작합니다.
        /// </summary>
        public void StartEntranceSequence()
        {
            if (_isSequenceRunning)
            {
                return;
            }

            if (_gridManager == null)
            {
                _gridManager = GetComponent<FormationGridManager>();
            }

            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }

            _sequenceCoroutine = StartCoroutine(SequenceRoutine());
        }

        /// <summary>
        /// 실행 중인 진입 시퀀스를 중단합니다.
        /// </summary>
        public void StopEntranceSequence()
        {
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
                _sequenceCoroutine = null;
            }
            _isSequenceRunning = false;
        }

        private IEnumerator SequenceRoutine()
        {
            _isSequenceRunning = true;
            _totalSpawnedCount = 0;
            _totalArrivedCount = 0;
            _activeEnemies.Clear();

            for (int wave = 1; wave <= 5; wave++)
            {
                _currentWaveIndex = wave;
                OnWaveStarted?.Invoke(wave);

                yield return StartCoroutine(SpawnWaveRoutine(wave));

                OnWaveCompleted?.Invoke(wave);

                if (wave < 5)
                {
                    yield return new WaitForSeconds(_waveInterval);
                }
            }

            // 모든 적이 안착할 때까지 대기
            while (_totalArrivedCount < _totalSpawnedCount)
            {
                yield return null;
            }

            _isSequenceRunning = false;
            _sequenceCoroutine = null;
            OnSequenceCompleted?.Invoke();

            if (_diveController != null)
            {
                _diveController.StartAutoDive();
            }
        }

        private IEnumerator SpawnWaveRoutine(int waveIndex)
        {
            EnemyType[] groupTypes = GetWaveEnemyTypes(waveIndex);

            for (int i = 0; i < groupTypes.Length; i++)
            {
                EnemyType type = groupTypes[i];
                SpawnAndLaunchEnemy(waveIndex, type);
                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        /// <summary>
        /// 단일 기체를 스폰하고 베지어 경로 비행을 시작합니다 (테스트 및 수동 제어용).
        /// </summary>
        public EnemyBase SpawnAndLaunchEnemy(int waveIndex, EnemyType type)
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

            _activeEnemies.Add(enemy);
            _totalSpawnedCount++;

            // 그리드 슬롯 배정
            FormationSlot targetSlot = null;
            if (_gridManager != null)
            {
                targetSlot = _gridManager.AssignEnemyToNextAvailableSlot(type, enemy);
            }

            Vector2 targetPos = targetSlot != null ? targetSlot.CurrentWorldPosition : Vector2.zero;
            BezierSegment[] trajectory = CreateEntranceTrajectory(waveIndex, targetPos);

            enemy.SetState(EnemyState.Entering);
            BezierPathFollower follower = enemy.PathFollower;
            if (follower != null)
            {
                float speed = enemy.Data != null ? enemy.Data.MoveSpeed : 10f;
                follower.SetPath(trajectory, speed, loop: false);
                follower.RotateAlongPath = true;
                follower.RotationOffset = -90f;

                // 도착 콜백 바인딩
                follower.OnPathCompleted += () =>
                {
                    _totalArrivedCount++;
                    enemy.EnterFormation();
                };

                follower.Play();
            }

            OnEnemySpawned?.Invoke(enemy);
            return enemy;
        }

        /// <summary>
        /// 웨이브 번호(1~5)에 따른 8기 구성 타입을 반환합니다.
        /// </summary>
        public static EnemyType[] GetWaveEnemyTypes(int waveIndex)
        {
            switch (waveIndex)
            {
                case 1: // Group 1: 보스 4기 + 고에이 4기
                    return new EnemyType[]
                    {
                        EnemyType.BossGalaga, EnemyType.BossGalaga, EnemyType.BossGalaga, EnemyType.BossGalaga,
                        EnemyType.Goei, EnemyType.Goei, EnemyType.Goei, EnemyType.Goei
                    };
                case 2: // Group 2: 자코 8기
                    return new EnemyType[]
                    {
                        EnemyType.Zako, EnemyType.Zako, EnemyType.Zako, EnemyType.Zako,
                        EnemyType.Zako, EnemyType.Zako, EnemyType.Zako, EnemyType.Zako
                    };
                case 3: // Group 3: 자코 8기
                    return new EnemyType[]
                    {
                        EnemyType.Zako, EnemyType.Zako, EnemyType.Zako, EnemyType.Zako,
                        EnemyType.Zako, EnemyType.Zako, EnemyType.Zako, EnemyType.Zako
                    };
                case 4: // Group 4: 고에이 8기
                    return new EnemyType[]
                    {
                        EnemyType.Goei, EnemyType.Goei, EnemyType.Goei, EnemyType.Goei,
                        EnemyType.Goei, EnemyType.Goei, EnemyType.Goei, EnemyType.Goei
                    };
                case 5: // Group 5: 고에이 4기 + 자코 4기
                    return new EnemyType[]
                    {
                        EnemyType.Goei, EnemyType.Goei, EnemyType.Goei, EnemyType.Goei,
                        EnemyType.Zako, EnemyType.Zako, EnemyType.Zako, EnemyType.Zako
                    };
                default:
                    return Array.Empty<EnemyType>();
            }
        }

        /// <summary>
        /// 웨이브 번호와 목표 슬롯 위치에 맞춘 3차 베지어 진입 곡선 세그먼트 배열을 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateEntranceTrajectory(int waveIndex, Vector2 targetSlotPos)
        {
            switch (waveIndex)
            {
                case 1: // Group 1: 상단 중앙 진입 후 중앙 루프 및 상단 안착
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(0f, 11f),
                        new Vector2(0f, 5f),
                        new Vector2(3f, 1f),
                        new Vector2(0f, 0f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(0f, 0f),
                        new Vector2(-3f, -1f),
                        new Vector2(targetSlotPos.x, targetSlotPos.y - 3f),
                        targetSlotPos
                    );
                    return new BezierSegment[] { seg1, seg2 };
                }
                case 2: // Group 2: 좌측 하단 진입 루프
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(-7.5f, -6f),
                        new Vector2(-3f, -2f),
                        new Vector2(-6f, 3f),
                        new Vector2(-2f, 4f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(-2f, 4f),
                        new Vector2(1f, 5f),
                        new Vector2(targetSlotPos.x - 1f, targetSlotPos.y - 2f),
                        targetSlotPos
                    );
                    return new BezierSegment[] { seg1, seg2 };
                }
                case 3: // Group 3: 우측 하단 진입 루프
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(7.5f, -6f),
                        new Vector2(3f, -2f),
                        new Vector2(6f, 3f),
                        new Vector2(2f, 4f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(2f, 4f),
                        new Vector2(-1f, 5f),
                        new Vector2(targetSlotPos.x + 1f, targetSlotPos.y - 2f),
                        targetSlotPos
                    );
                    return new BezierSegment[] { seg1, seg2 };
                }
                case 4: // Group 4: 좌측 상단 진입 후 대각 하강 및 상승 안착
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(-7.5f, 9f),
                        new Vector2(-3f, 6f),
                        new Vector2(-5f, 0f),
                        new Vector2(0f, 1f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(0f, 1f),
                        new Vector2(3f, 2f),
                        new Vector2(targetSlotPos.x, targetSlotPos.y - 2f),
                        targetSlotPos
                    );
                    return new BezierSegment[] { seg1, seg2 };
                }
                case 5: // Group 5: 우측 상단 진입 후 대각 하강 및 상승 안착
                {
                    BezierSegment seg1 = new BezierSegment(
                        new Vector2(7.5f, 9f),
                        new Vector2(3f, 6f),
                        new Vector2(5f, 0f),
                        new Vector2(0f, 1f)
                    );
                    BezierSegment seg2 = new BezierSegment(
                        new Vector2(0f, 1f),
                        new Vector2(-3f, 2f),
                        new Vector2(targetSlotPos.x, targetSlotPos.y - 2f),
                        targetSlotPos
                    );
                    return new BezierSegment[] { seg1, seg2 };
                }
                default:
                {
                    BezierSegment seg = new BezierSegment(
                        new Vector2(0f, 11f),
                        new Vector2(0f, 6f),
                        new Vector2(targetSlotPos.x, targetSlotPos.y + 2f),
                        targetSlotPos
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
