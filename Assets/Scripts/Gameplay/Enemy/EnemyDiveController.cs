using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 상단 그리드에 대기 중인 적들의 주기적 급강하(Diving Attack)를 관리하는 컨트롤러입니다.
    /// 단독 다이브, 호위 편대 다이브(보스 1기 + 고에이 1~2기), 플레이어 예측 조준 및 화면 하단 루프 복귀를 제어합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyDiveController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("편대 그리드 매니저 참조")]
        [SerializeField] private FormationGridManager _gridManager;

        [Tooltip("플레이어 기체 Transform (예측 궤적 계산용)")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("플레이 영역 매니저 (화면 외곽 좌표 계산용)")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        [Header("Dive Timing Settings")]
        [Tooltip("다이브 발생 주기 (초)")]
        [SerializeField] private float _diveInterval = 3.0f;

        [Tooltip("다이브 비행 속도 (units/sec)")]
        [SerializeField] private float _diveSpeed = 11.0f;

        [Tooltip("하단 통과 후 상단 루프 복귀 비행 속도 (units/sec)")]
        [SerializeField] private float _returnSpeed = 10.0f;

        [Tooltip("동시 다이브 최대 적 기체 수")]
        [SerializeField] private int _maxConcurrentDives = 4;

        [Tooltip("플레이어 이동 예측 시간 가중치 (초)")]
        [SerializeField] private float _playerLeadTime = 0.25f;

        [Tooltip("게임 시작 시 자동 다이브 실행 여부")]
        [SerializeField] private bool _autoDiveEnabled = true;

        [Header("Loop Boundaries")]
        [Tooltip("화면 하단 이탈 Y좌표 (이 좌표 통과 시 상단 복귀 비행 시작)")]
        [SerializeField] private float _screenBottomY = -11.0f;

        [Tooltip("화면 상단 재진입 Y좌표")]
        [SerializeField] private float _screenTopReentryY = 11.0f;

        private List<EnemyBase> _activeDivingEnemies = new List<EnemyBase>();
        private Coroutine _diveLoopCoroutine;
        private Vector3 _lastPlayerPos = new Vector3(0f, -8f, 0f);

        public event Action<EnemyBase> OnDiveStarted;
        public event Action<EnemyBase> OnDiveCompleted;

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
            set => _diveInterval = value;
        }

        public float DiveSpeed
        {
            get => _diveSpeed;
            set => _diveSpeed = value;
        }

        public float ReturnSpeed
        {
            get => _returnSpeed;
            set => _returnSpeed = value;
        }

        public int MaxConcurrentDives
        {
            get => _maxConcurrentDives;
            set => _maxConcurrentDives = value;
        }

        public bool AutoDiveEnabled
        {
            get => _autoDiveEnabled;
            set => _autoDiveEnabled = value;
        }

        public int ActiveDivingCount => _activeDivingEnemies.Count;

        private void Start()
        {
            if (_gridManager == null)
            {
                _gridManager = GetComponent<FormationGridManager>();
                if (_gridManager == null)
                {
                    _gridManager = FindAnyObjectByType<FormationGridManager>();
                }
            }

            if (_playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }

            if (_playAreaManager == null)
            {
                _playAreaManager = PlayAreaManager.Instance;
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

        private void OnDisable()
        {
            StopAutoDive();
        }

        /// <summary>
        /// 주기적 자동 다이브 코루틴을 시작합니다.
        /// </summary>
        public void StartAutoDive()
        {
            if (_diveLoopCoroutine != null)
            {
                StopCoroutine(_diveLoopCoroutine);
            }
            _diveLoopCoroutine = StartCoroutine(DiveLoopRoutine());
        }

        /// <summary>
        /// 자동 다이브 코루틴을 정지합니다.
        /// </summary>
        public void StopAutoDive()
        {
            if (_diveLoopCoroutine != null)
            {
                StopCoroutine(_diveLoopCoroutine);
                _diveLoopCoroutine = null;
            }
        }

        private IEnumerator DiveLoopRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_diveInterval);

                if (_activeDivingEnemies.Count < _maxConcurrentDives)
                {
                    TriggerRandomDive();
                }
            }
        }

        /// <summary>
        /// 대기 중인 적 중에서 무작위로 단독 또는 호위 편대 다이브를 선별하여 발동합니다.
        /// </summary>
        public void TriggerRandomDive()
        {
            if (_gridManager == null)
            {
                return;
            }

            List<EnemyBase> candidateEnemies = GetEligibleFormationEnemies();
            if (candidateEnemies.Count == 0)
            {
                return;
            }

            // 보스 기체가 있고 호위기가 존재할 확률 검사 (호위 편대 다이브)
            EnemyBase bossCandidate = candidateEnemies.Find(e => e.Type == EnemyType.BossGalaga || e.Type == EnemyType.Boss);
            if (bossCandidate != null && UnityEngine.Random.value < 0.4f)
            {
                List<EnemyBase> escorts = FindAvailableEscortsForBoss(bossCandidate, 2);
                if (escorts.Count > 0)
                {
                    TriggerBossEscortDive(bossCandidate, escorts);
                    return;
                }
            }

            // 단독 다이브 발동
            EnemyBase selectedEnemy = candidateEnemies[UnityEngine.Random.Range(0, candidateEnemies.Count)];
            LaunchSingleDive(selectedEnemy);
        }

        /// <summary>
        /// 단일 적 기체의 급강하 궤적을 생성하고 비행을 시작합니다.
        /// </summary>
        public void LaunchSingleDive(EnemyBase enemy)
        {
            if (enemy == null || enemy.CurrentState != EnemyState.Formation)
            {
                return;
            }

            Vector2 startPos = enemy.transform.position;
            Vector2 targetPos = GetPredictedPlayerPosition();

            BezierSegment[] diveSegments = CreateSingleDiveTrajectory(startPos, targetPos, _screenBottomY);

            enemy.SetState(EnemyState.Diving);
            _activeDivingEnemies.Add(enemy);

            BezierPathFollower follower = enemy.GetComponent<BezierPathFollower>();
            if (follower != null)
            {
                follower.SetPath(diveSegments, _diveSpeed, false);
                Action onCompleted = null;
                onCompleted = () =>
                {
                    follower.OnPathCompleted -= onCompleted;
                    OnDivePathCompleted(enemy);
                };
                follower.OnPathCompleted += onCompleted;
                follower.Play();
            }

            OnDiveStarted?.Invoke(enemy);
        }

        /// <summary>
        /// 보스 갤러그와 고에이 1~2기가 동반 급강하하는 호위 편대 다이브를 개시합니다.
        /// </summary>
        public void TriggerBossEscortDive(EnemyBase boss, List<EnemyBase> escorts)
        {
            if (boss == null || boss.CurrentState != EnemyState.Formation)
            {
                return;
            }

            Vector2 bossStart = boss.transform.position;
            Vector2 targetPos = GetPredictedPlayerPosition();

            BezierSegment[] bossSegments = CreateSingleDiveTrajectory(bossStart, targetPos, _screenBottomY);

            boss.SetState(EnemyState.Diving);
            _activeDivingEnemies.Add(boss);

            BezierPathFollower bossFollower = boss.GetComponent<BezierPathFollower>();
            if (bossFollower != null)
            {
                bossFollower.SetPath(bossSegments, _diveSpeed, false);
                Action onCompleted = null;
                onCompleted = () =>
                {
                    bossFollower.OnPathCompleted -= onCompleted;
                    OnDivePathCompleted(boss);
                };
                bossFollower.OnPathCompleted += onCompleted;
                bossFollower.Play();
            }
            OnDiveStarted?.Invoke(boss);

            // 호위기 동반 발진
            for (int i = 0; i < escorts.Count; i++)
            {
                EnemyBase escort = escorts[i];
                if (escort == null || escort.CurrentState != EnemyState.Formation) continue;

                Vector2 escortStart = escort.transform.position;
                Vector2 escortOffset = (i == 0) ? new Vector2(-1.2f, 0.6f) : new Vector2(1.2f, 0.6f);

                BezierSegment[] escortSegments = CreateEscortDiveTrajectory(escortStart, bossStart, targetPos, escortOffset, _screenBottomY);

                escort.SetState(EnemyState.Diving);
                _activeDivingEnemies.Add(escort);

                BezierPathFollower escortFollower = escort.GetComponent<BezierPathFollower>();
                if (escortFollower != null)
                {
                    escortFollower.SetPath(escortSegments, _diveSpeed, false);
                    Action onCompleted = null;
                    onCompleted = () =>
                    {
                        escortFollower.OnPathCompleted -= onCompleted;
                        OnDivePathCompleted(escort);
                    };
                    escortFollower.OnPathCompleted += onCompleted;
                    escortFollower.Play();
                }
                OnDiveStarted?.Invoke(escort);
            }
        }

        private void OnDivePathCompleted(EnemyBase enemy)
        {
            if (enemy == null || enemy.CurrentState == EnemyState.Dead)
            {
                _activeDivingEnemies.Remove(enemy);
                return;
            }

            // 화면 하단 도달 시 상단 재진입 루프 복귀 경로 실행
            LaunchReturnToFormation(enemy);
        }

        /// <summary>
        /// 화면 하단을 통과한 적을 상단($Y=+11$)으로 재배치하고 원래 슬롯으로 복귀하는 궤적을 실행합니다.
        /// </summary>
        public void LaunchReturnToFormation(EnemyBase enemy)
        {
            if (enemy == null || enemy.CurrentState == EnemyState.Dead)
            {
                _activeDivingEnemies.Remove(enemy);
                return;
            }

            enemy.SetState(EnemyState.Returning);

            float reentryX = Mathf.Clamp(enemy.transform.position.x, -6.0f, 6.0f);
            Vector2 entryPos = new Vector2(reentryX, _screenTopReentryY);
            enemy.transform.position = new Vector3(entryPos.x, entryPos.y, 0f);

            Vector2 targetSlotPos = Vector2.zero;
            if (_gridManager != null)
            {
                FormationSlot slot = _gridManager.FindSlotByEnemy(enemy);
                if (slot != null)
                {
                    targetSlotPos = slot.CurrentWorldPosition;
                }
                else
                {
                    targetSlotPos = _gridManager.GridOrigin;
                }
            }

            BezierSegment[] returnSegments = CreateReturnTrajectory(entryPos, targetSlotPos);

            BezierPathFollower follower = enemy.GetComponent<BezierPathFollower>();
            if (follower != null)
            {
                follower.SetPath(returnSegments, _returnSpeed, false);
                Action onCompleted = null;
                onCompleted = () =>
                {
                    follower.OnPathCompleted -= onCompleted;
                    OnReturnCompleted(enemy);
                };
                follower.OnPathCompleted += onCompleted;
                follower.Play();
            }
        }

        private void OnReturnCompleted(EnemyBase enemy)
        {
            if (enemy == null || enemy.CurrentState == EnemyState.Dead)
            {
                _activeDivingEnemies.Remove(enemy);
                return;
            }

            _activeDivingEnemies.Remove(enemy);
            enemy.EnterFormation();
            OnDiveCompleted?.Invoke(enemy);
        }

        /// <summary>
        /// 대기 상태(Formation)이며 살아있는 적 기체 목록을 반환합니다.
        /// </summary>
        public List<EnemyBase> GetEligibleFormationEnemies()
        {
            List<EnemyBase> result = new List<EnemyBase>();
            if (_gridManager == null)
            {
                return result;
            }

            FormationSlot[] slots = _gridManager.GetAllSlots();
            if (slots == null)
            {
                return result;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].IsOccupied && slots[i].Occupant != null)
                {
                    EnemyBase enemy = slots[i].Occupant;
                    if (enemy.CurrentState == EnemyState.Formation && enemy.IsAlive)
                    {
                        result.Add(enemy);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 보스 기체와 같은 열 또는 가장 인접한 대기 중인 고에이 기체들을 선별합니다.
        /// </summary>
        public List<EnemyBase> FindAvailableEscortsForBoss(EnemyBase boss, int maxEscorts)
        {
            List<EnemyBase> escorts = new List<EnemyBase>();
            if (boss == null || _gridManager == null) return escorts;

            FormationSlot bossSlot = _gridManager.FindSlotByEnemy(boss);
            int bossCol = bossSlot != null ? bossSlot.ColumnIndex : 2;

            List<FormationSlot> goeiSlots = new List<FormationSlot>();
            FormationSlot[] allSlots = _gridManager.GetAllSlots();
            for (int i = 0; i < allSlots.Length; i++)
            {
                FormationSlot slot = allSlots[i];
                if (slot.IsOccupied && slot.Occupant != null && slot.Occupant.IsAlive &&
                    slot.Occupant.CurrentState == EnemyState.Formation &&
                    (slot.AssignedType == EnemyType.Goei || slot.Occupant.Type == EnemyType.Goei))
                {
                    goeiSlots.Add(slot);
                }
            }

            goeiSlots.Sort((a, b) => Mathf.Abs(a.ColumnIndex - bossCol).CompareTo(Mathf.Abs(b.ColumnIndex - bossCol)));

            for (int i = 0; i < Mathf.Min(maxEscorts, goeiSlots.Count); i++)
            {
                escorts.Add(goeiSlots[i].Occupant);
            }

            return escorts;
        }

        /// <summary>
        /// 플레이어 기체의 현재 위치 및 속도를 기반으로 예측된 조준 좌표를 계산합니다.
        /// </summary>
        public Vector3 GetPredictedPlayerPosition()
        {
            if (_playerTransform == null)
            {
                return new Vector3(0f, -8.0f, 0f);
            }

            Vector3 currentPos = _playerTransform.position;
            Vector3 velocity = (currentPos - _lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.001f);
            _lastPlayerPos = currentPos;

            Vector3 predictedPos = currentPos + velocity * _playerLeadTime;
            predictedPos.y = -8.0f;

            if (_playAreaManager != null)
            {
                predictedPos.x = Mathf.Clamp(predictedPos.x, _playAreaManager.MinX + 0.5f, _playAreaManager.MaxX - 0.5f);
            }
            else
            {
                predictedPos.x = Mathf.Clamp(predictedPos.x, -7.0f, 7.0f);
            }

            return predictedPos;
        }

        /// <summary>
        /// 단독 급강하 2구간 3차 베지어 세그먼트를 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateSingleDiveTrajectory(Vector2 startPos, Vector2 playerPos, float screenBottomY)
        {
            float sign = (startPos.x > playerPos.x) ? -1f : 1f;
            Vector2 midPoint = new Vector2((startPos.x + playerPos.x) * 0.5f, (startPos.y + playerPos.y) * 0.5f);

            BezierSegment seg1 = new BezierSegment(
                startPos,
                startPos + new Vector2(sign * 2.0f, 2.0f),
                startPos + new Vector2(sign * 4.0f, -2.0f),
                midPoint
            );

            BezierSegment seg2 = new BezierSegment(
                midPoint,
                midPoint + new Vector2(-sign * 1.5f, -3.0f),
                playerPos + new Vector2(0f, 2.0f),
                new Vector2(playerPos.x, screenBottomY)
            );

            return new BezierSegment[] { seg1, seg2 };
        }

        /// <summary>
        /// 호위기 급강하 2구간 3차 베지어 세그먼트를 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateEscortDiveTrajectory(Vector2 escortStartPos, Vector2 bossStartPos, Vector2 playerPos, Vector2 escortOffset, float screenBottomY)
        {
            float sign = (escortStartPos.x > playerPos.x) ? -1f : 1f;
            Vector2 midPoint = new Vector2((escortStartPos.x + playerPos.x) * 0.5f + escortOffset.x, (escortStartPos.y + playerPos.y) * 0.5f + escortOffset.y);

            BezierSegment seg1 = new BezierSegment(
                escortStartPos,
                escortStartPos + new Vector2(sign * 2.0f, 2.0f),
                escortStartPos + new Vector2(sign * 4.0f, -2.0f),
                midPoint
            );

            BezierSegment seg2 = new BezierSegment(
                midPoint,
                midPoint + new Vector2(-sign * 1.5f, -3.0f),
                playerPos + escortOffset + new Vector2(0f, 2.0f),
                new Vector2(playerPos.x + escortOffset.x, screenBottomY + escortOffset.y)
            );

            return new BezierSegment[] { seg1, seg2 };
        }

        /// <summary>
        /// 상단 재진입 후 지정 슬롯으로 복귀하는 단일 베지어 세그먼트를 생성합니다.
        /// </summary>
        public static BezierSegment[] CreateReturnTrajectory(Vector2 entryPos, Vector2 targetSlotPos)
        {
            BezierSegment seg = new BezierSegment(
                entryPos,
                entryPos + new Vector2(0f, -3.0f),
                targetSlotPos + new Vector2((entryPos.x > targetSlotPos.x ? 2.0f : -2.0f), 2.0f),
                targetSlotPos
            );

            return new BezierSegment[] { seg };
        }

        /// <summary>
        /// 테스트 또는 외부에서 특정 적의 다이브를 강제 발동합니다.
        /// </summary>
        public void ForceDive(EnemyBase enemy)
        {
            LaunchSingleDive(enemy);
        }
    }
}
