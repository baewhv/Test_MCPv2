using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 편대 안착 상태(Formation)의 적 기체를 주기적으로 선별하여 단독 또는 보스+호위기 동반 급강하(Diving Attack)를 트리거하고,
    /// 화면 하단 이탈 후 상단 재진입 복귀(Return to Formation) 궤적을 제어하는 AI 매니저 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyDiveController : MonoBehaviour
    {
        [Header("Manager References")]
        [Tooltip("편대 그리드 매니저 참조")]
        [SerializeField] private FormationGridManager _gridManager;

        [Tooltip("플레이어 위치 참조 (조준 및 예측 궤적 계산용)")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("플레이 영역 경계 매니저 참조")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        [Header("Dive Settings")]
        [Tooltip("급강하 공격 주기 간격(초)")]
        [SerializeField] private float _diveInterval = 2.5f;

        [Tooltip("급강하 비행 속도 (units/sec)")]
        [SerializeField] private float _diveSpeed = 11.0f;

        [Tooltip("복귀 비행 속도 (units/sec)")]
        [SerializeField] private float _returnSpeed = 10.0f;

        [Tooltip("동시 다이브 가능한 최대 적 기체 수")]
        [SerializeField] private int _maxConcurrentDives = 4;

        [Tooltip("플레이어 이동 예측 리드 타임 (초)")]
        [SerializeField] private float _playerLeadTime = 0.25f;

        [Tooltip("주기적 자동 급강하 활성화 여부")]
        [SerializeField] private bool _autoDiveEnabled = false;

        [Header("Boundary Y Coordinates")]
        [Tooltip("다이브 종점 화면 하단 Y좌표")]
        [SerializeField] private float _screenBottomY = -11.0f;

        [Tooltip("복귀 시작 화면 상단 재진입 Y좌표")]
        [SerializeField] private float _screenTopY = 11.0f;

        private readonly List<EnemyBase> _divingEnemies = new List<EnemyBase>();
        private Coroutine _diveLoopCoroutine;
        private Vector3 _lastPlayerPos;
        private Vector3 _playerVelocity;

        public event Action<EnemyBase> OnDiveStarted;
        public event Action<EnemyBase> OnDiveCompleted;
        public event Action<EnemyBase> OnReturnStarted;

        public FormationGridManager GridManager
        {
            get => _gridManager;
            set => _gridManager = value;
        }

        public Transform PlayerTransform
        {
            get => _playerTransform;
            set => _playerTransform = value;
        }

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        public float DiveInterval
        {
            get => _diveInterval;
            set => _diveInterval = Mathf.Max(0.5f, value);
        }

        public float DiveSpeed
        {
            get => _diveSpeed;
            set => _diveSpeed = Mathf.Max(1f, value);
        }

        public float ReturnSpeed
        {
            get => _returnSpeed;
            set => _returnSpeed = Mathf.Max(1f, value);
        }

        public int MaxConcurrentDives
        {
            get => _maxConcurrentDives;
            set => _maxConcurrentDives = Mathf.Max(1, value);
        }

        public bool AutoDiveEnabled
        {
            get => _autoDiveEnabled;
            set
            {
                _autoDiveEnabled = value;
                if (_autoDiveEnabled)
                {
                    StartAutoDive();
                }
                else
                {
                    StopAutoDive();
                }
            }
        }

        public IReadOnlyList<EnemyBase> DivingEnemies => _divingEnemies;
        public int ActiveDiveCount => _divingEnemies.Count;

        private void Awake()
        {
            if (_gridManager == null)
            {
                _gridManager = GetComponent<FormationGridManager>();
            }
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }

            if (_playAreaManager == null && Camera.main != null)
            {
                _playAreaManager = Camera.main.GetComponent<PlayAreaManager>();
            }

            if (_playerTransform != null)
            {
                _lastPlayerPos = _playerTransform.position;
            }

            if (_autoDiveEnabled)
            {
                StartAutoDive();
            }
        }

        private void Update()
        {
            UpdatePlayerVelocity();
        }

        private void OnDisable()
        {
            StopAutoDive();
        }

        private void UpdatePlayerVelocity()
        {
            if (_playerTransform != null)
            {
                Vector3 currentPos = _playerTransform.position;
                if (Time.deltaTime > 0.0001f)
                {
                    _playerVelocity = (currentPos - _lastPlayerPos) / Time.deltaTime;
                }
                _lastPlayerPos = currentPos;
            }
        }

        /// <summary>
        /// 자동 급강하 루프를 시작합니다.
        /// </summary>
        public void StartAutoDive()
        {
            _autoDiveEnabled = true;
            if (_diveLoopCoroutine != null)
            {
                StopCoroutine(_diveLoopCoroutine);
            }
            _diveLoopCoroutine = StartCoroutine(AutoDiveRoutine());
        }

        /// <summary>
        /// 자동 급강하 루프를 중단합니다.
        /// </summary>
        public void StopAutoDive()
        {
            _autoDiveEnabled = false;
            if (_diveLoopCoroutine != null)
            {
                StopCoroutine(_diveLoopCoroutine);
                _diveLoopCoroutine = null;
            }
        }

        private IEnumerator AutoDiveRoutine()
        {
            while (_autoDiveEnabled)
            {
                yield return new WaitForSeconds(_diveInterval);

                if (_divingEnemies.Count < _maxConcurrentDives)
                {
                    TriggerRandomDive();
                }
            }
        }

        /// <summary>
        /// 편대에 대기 중인 적 중에서 무작위로 1기(또는 보스+호위 편대)를 선택하여 급강하를 시작합니다.
        /// </summary>
        public bool TriggerRandomDive()
        {
            if (_gridManager == null || _gridManager.Slots == null)
            {
                return false;
            }

            // 편대 상태인 적 후보 수집
            List<FormationSlot> availableSlots = new List<FormationSlot>();
            for (int i = 0; i < _gridManager.Slots.Count; i++)
            {
                FormationSlot slot = _gridManager.Slots[i];
                if (slot != null && slot.IsOccupied && slot.Occupant != null)
                {
                    if (slot.Occupant.CurrentState == EnemyState.Formation && !_divingEnemies.Contains(slot.Occupant))
                    {
                        availableSlots.Add(slot);
                    }
                }
            }

            if (availableSlots.Count == 0)
            {
                return false;
            }

            FormationSlot chosenSlot = availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];
            EnemyBase leader = chosenSlot.Occupant;

            List<EnemyBase> escorts = null;
            if (leader.Type == EnemyType.BossGalaga)
            {
                escorts = FindAvailableEscortsForBoss(leader, UnityEngine.Random.Range(1, 3));
            }

            return TriggerDive(leader, escorts);
        }

        /// <summary>
        /// 특정 적 리더(및 선택적 호위기)의 급강하 공격을 트리거합니다.
        /// </summary>
        public bool TriggerDive(EnemyBase leader, List<EnemyBase> escorts = null)
        {
            if (leader == null || leader.IsDead || leader.CurrentState != EnemyState.Formation)
            {
                return false;
            }

            Vector2 playerTarget = GetPredictedPlayerPosition();
            Vector2 leaderStartPos = leader.transform.position;

            // 리더 다이브 시작
            StartSingleDive(leader, leaderStartPos, playerTarget);

            // 호위기 다이브 시작
            if (escorts != null && escorts.Count > 0)
            {
                for (int i = 0; i < escorts.Count; i++)
                {
                    EnemyBase escort = escorts[i];
                    if (escort != null && !escort.IsDead && escort.CurrentState == EnemyState.Formation)
                    {
                        float xOffset = (i == 0) ? -1.2f : 1.2f;
                        Vector2 offset = new Vector2(xOffset, 0.6f);
                        StartEscortDive(escort, escort.transform.position, leaderStartPos, playerTarget, offset);
                    }
                }
            }

            return true;
        }

        private void StartSingleDive(EnemyBase enemy, Vector2 startPos, Vector2 playerTarget)
        {
            _divingEnemies.Add(enemy);
            enemy.SetState(EnemyState.Diving);

            BezierSegment[] divePath = CreateSingleDiveTrajectory(startPos, playerTarget, _screenBottomY);
            BezierPathFollower follower = enemy.PathFollower;

            if (follower != null)
            {
                float speed = enemy.Data != null ? enemy.Data.MoveSpeed * 1.1f : _diveSpeed;
                follower.SetPath(divePath, speed, loop: false);
                follower.RotateAlongPath = true;
                follower.RotationOffset = -90f;

                Action onComplete = null;
                onComplete = () =>
                {
                    follower.OnPathCompleted -= onComplete;
                    HandleEnemyReachedBottom(enemy);
                };

                follower.OnPathCompleted += onComplete;
                follower.Play();
            }

            OnDiveStarted?.Invoke(enemy);
        }

        private void StartEscortDive(EnemyBase escort, Vector2 startPos, Vector2 bossStartPos, Vector2 playerTarget, Vector2 offset)
        {
            _divingEnemies.Add(escort);
            escort.SetState(EnemyState.Diving);

            BezierSegment[] escortPath = CreateEscortDiveTrajectory(startPos, bossStartPos, playerTarget, offset, _screenBottomY);
            BezierPathFollower follower = escort.PathFollower;

            if (follower != null)
            {
                float speed = escort.Data != null ? escort.Data.MoveSpeed * 1.1f : _diveSpeed;
                follower.SetPath(escortPath, speed, loop: false);
                follower.RotateAlongPath = true;
                follower.RotationOffset = -90f;

                Action onComplete = null;
                onComplete = () =>
                {
                    follower.OnPathCompleted -= onComplete;
                    HandleEnemyReachedBottom(escort);
                };

                follower.OnPathCompleted += onComplete;
                follower.Play();
            }

            OnDiveStarted?.Invoke(escort);
        }

        private void HandleEnemyReachedBottom(EnemyBase enemy)
        {
            if (enemy == null || enemy.IsDead)
            {
                _divingEnemies.Remove(enemy);
                return;
            }

            // 화면 상단으로 재진입하여 소속 슬롯으로 복귀
            enemy.SetState(EnemyState.Returning);
            OnReturnStarted?.Invoke(enemy);

            FormationSlot slot = FindSlotForEnemy(enemy);
            Vector2 targetSlotPos = slot != null ? slot.CurrentWorldPosition : new Vector2(0f, 6f);

            // 상단 재진입 시작 좌표 계산
            float entryX = Mathf.Clamp(enemy.transform.position.x, -4f, 4f);
            Vector2 entryPos = new Vector2(entryX, _screenTopY);

            // 위치를 화면 상단으로 텔레포트
            enemy.transform.position = new Vector3(entryPos.x, entryPos.y, enemy.transform.position.z);

            BezierSegment[] returnPath = CreateReturnTrajectory(entryPos, targetSlotPos);
            BezierPathFollower follower = enemy.PathFollower;

            if (follower != null)
            {
                follower.SetPath(returnPath, _returnSpeed, loop: false);
                follower.RotateAlongPath = true;
                follower.RotationOffset = -90f;

                Action onReturnComplete = null;
                onReturnComplete = () =>
                {
                    follower.OnPathCompleted -= onReturnComplete;
                    _divingEnemies.Remove(enemy);
                    enemy.EnterFormation();
                    OnDiveCompleted?.Invoke(enemy);
                };

                follower.OnPathCompleted += onReturnComplete;
                follower.Play();
            }
            else
            {
                _divingEnemies.Remove(enemy);
                enemy.EnterFormation();
                OnDiveCompleted?.Invoke(enemy);
            }
        }

        private FormationSlot FindSlotForEnemy(EnemyBase enemy)
        {
            if (_gridManager == null || _gridManager.Slots == null)
            {
                return null;
            }

            for (int i = 0; i < _gridManager.Slots.Count; i++)
            {
                FormationSlot slot = _gridManager.Slots[i];
                if (slot != null && slot.Occupant == enemy)
                {
                    return slot;
                }
            }
            return null;
        }

        /// <summary>
        /// 보스 기체와 인접한 행/열에서 대기 중인 고에이(Goei) 호위기를 탐색합니다.
        /// </summary>
        public List<EnemyBase> FindAvailableEscortsForBoss(EnemyBase boss, int maxCount = 2)
        {
            List<EnemyBase> escorts = new List<EnemyBase>();
            if (_gridManager == null || _gridManager.Slots == null || boss == null)
            {
                return escorts;
            }

            FormationSlot bossSlot = FindSlotForEnemy(boss);
            int bossCol = bossSlot != null ? bossSlot.ColumnIndex : 2;

            // 고에이 행(Row 1, Row 2)에서 보스 열과 가까운 고에이 우선 선택
            List<FormationSlot> goeiSlots = new List<FormationSlot>();
            for (int i = 0; i < _gridManager.Slots.Count; i++)
            {
                FormationSlot s = _gridManager.Slots[i];
                if (s != null && s.IsOccupied && s.Occupant != null && s.AssignedType == EnemyType.Goei)
                {
                    if (s.Occupant.CurrentState == EnemyState.Formation && !_divingEnemies.Contains(s.Occupant))
                    {
                        goeiSlots.Add(s);
                    }
                }
            }

            // 보스 열과의 거리를 기준으로 정렬
            goeiSlots.Sort((a, b) => Mathf.Abs(a.ColumnIndex - bossCol).CompareTo(Mathf.Abs(b.ColumnIndex - bossCol)));

            for (int i = 0; i < goeiSlots.Count && escorts.Count < maxCount; i++)
            {
                escorts.Add(goeiSlots[i].Occupant);
            }

            return escorts;
        }

        private Vector2 GetPredictedPlayerPosition()
        {
            if (_playerTransform == null)
            {
                return new Vector2(0f, -8f);
            }

            Vector2 playerPos = _playerTransform.position;
            float predictedX = playerPos.x + (_playerVelocity.x * _playerLeadTime);

            if (_playAreaManager != null)
            {
                predictedX = Mathf.Clamp(predictedX, _playAreaManager.MinX + 0.5f, _playAreaManager.MaxX - 0.5f);
            }
            else
            {
                predictedX = Mathf.Clamp(predictedX, -5.5f, 5.5f);
            }

            return new Vector2(predictedX, playerPos.y);
        }

        /// <summary>
        /// 단독 적 기체의 급강하 3차 베지어 궤적(2개 세그먼트)을 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateSingleDiveTrajectory(Vector2 startPos, Vector2 targetPlayerPos, float screenBottomY = -11.0f)
        {
            // 1단계: 편대에서 이탈하여 외곽으로 호를 그리며 중간 높이 도달
            float outwardDir = (startPos.x >= 0f) ? 1f : -1f;
            if (Mathf.Abs(startPos.x) < 0.5f)
            {
                outwardDir = (targetPlayerPos.x >= 0f) ? 1f : -1f;
            }

            Vector2 p0 = startPos;
            Vector2 p1 = startPos + new Vector2(outwardDir * 2.5f, 1.0f);
            Vector2 p2 = new Vector2(startPos.x + (outwardDir * 3.5f), 1.0f);
            Vector2 midPoint = new Vector2(startPos.x + (outwardDir * 1.5f), 0.0f);
            BezierSegment seg1 = new BezierSegment(p0, p1, p2, midPoint);

            // 2단계: 중간 지점에서 플레이어 예측 위치를 향해 급강하하여 화면 하단 통과
            Vector2 p3 = midPoint;
            Vector2 p4 = new Vector2(midPoint.x - (outwardDir * 1.5f), -3.0f);
            Vector2 p5 = new Vector2(targetPlayerPos.x, targetPlayerPos.y + 2.0f);
            Vector2 endPoint = new Vector2(targetPlayerPos.x, screenBottomY);
            BezierSegment seg2 = new BezierSegment(p3, p4, p5, endPoint);

            return new BezierSegment[] { seg1, seg2 };
        }

        /// <summary>
        /// 호위기의 오프셋 동반 급강하 궤적을 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateEscortDiveTrajectory(Vector2 startPos, Vector2 bossStartPos, Vector2 targetPlayerPos, Vector2 escortOffset, float screenBottomY = -11.0f)
        {
            BezierSegment[] bossTrajectory = CreateSingleDiveTrajectory(bossStartPos, targetPlayerPos, screenBottomY);

            BezierSegment[] escortTrajectory = new BezierSegment[bossTrajectory.Length];
            for (int i = 0; i < bossTrajectory.Length; i++)
            {
                BezierSegment bSeg = bossTrajectory[i];
                if (i == 0)
                {
                    escortTrajectory[i] = new BezierSegment(
                        startPos,
                        bSeg.p1 + escortOffset,
                        bSeg.p2 + escortOffset,
                        bSeg.p3 + escortOffset
                    );
                }
                else
                {
                    escortTrajectory[i] = new BezierSegment(
                        bSeg.p0 + escortOffset,
                        bSeg.p1 + escortOffset,
                        bSeg.p2 + escortOffset,
                        bSeg.p3 + escortOffset
                    );
                }
            }

            return escortTrajectory;
        }

        /// <summary>
        /// 화면 상단 재진입 후 슬롯으로 완만하게 복귀하는 궤적을 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateReturnTrajectory(Vector2 returnEntryPos, Vector2 slotTargetPos)
        {
            Vector2 p0 = returnEntryPos;
            Vector2 p1 = new Vector2(returnEntryPos.x, returnEntryPos.y - 2.5f);
            Vector2 p2 = new Vector2(slotTargetPos.x, slotTargetPos.y + 2.0f);
            Vector2 p3 = slotTargetPos;

            return new BezierSegment[] { new BezierSegment(p0, p1, p2, p3) };
        }
    }
}
