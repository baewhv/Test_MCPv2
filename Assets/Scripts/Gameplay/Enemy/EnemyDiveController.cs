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
            EnemyBase bossCandidate = candidateEnemies.Find(e => e.EnemyType == EnemyType.Boss);
            if (bossCandidate != null && UnityEngine.Random.value < 0.4f)
            {
                TriggerBossEscortDive(bossCandidate, candidateEnemies);
                return;
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

            Vector3 startPos = enemy.transform.position;
            Vector3 targetPos = GetPredictedPlayerPosition();

            // 3차 베지어 급강하 경로 2개 생성 (강하 -> 하단 통과)
            BezierPath divePath = GenerateDiveBezierPath(startPos, targetPos);

            enemy.SetState(EnemyState.Diving);
            _activeDivingEnemies.Add(enemy);

            BezierPathFollower follower = enemy.GetComponent<BezierPathFollower>();
            if (follower != null)
            {
                follower.FollowPath(divePath, _diveSpeed, () => OnDivePathCompleted(enemy));
            }

            OnDiveStarted?.Invoke(enemy);
        }

        /// <summary>
        /// 보스 갤러그와 고에이 1~2기가 동반 급강하하는 호위 편대 다이브를 개시합니다.
        /// </summary>
        public void TriggerBossEscortDive(EnemyBase boss, List<EnemyBase> candidates)
        {
            if (boss == null || boss.CurrentState != EnemyState.Formation)
            {
                return;
            }

            List<EnemyBase> goeiList = candidates.FindAll(e => e.EnemyType == EnemyType.Goei && e.CurrentState == EnemyState.Formation);
            int escortCount = Mathf.Min(goeiList.Count, UnityEngine.Random.Range(1, 3));

            Vector3 targetPos = GetPredictedPlayerPosition();
            BezierPath bossPath = GenerateDiveBezierPath(boss.transform.position, targetPos);

            boss.SetState(EnemyState.Diving);
            _activeDivingEnemies.Add(boss);

            BezierPathFollower bossFollower = boss.GetComponent<BezierPathFollower>();
            if (bossFollower != null)
            {
                bossFollower.FollowPath(bossPath, _diveSpeed, () => OnDivePathCompleted(boss));
            }
            OnDiveStarted?.Invoke(boss);

            // 호위기 동반 발진 (보스 좌우 오프셋 궤적)
            for (int i = 0; i < escortCount; i++)\n            {
                EnemyBase escort = goeiList[i];
                float sideOffset = (i == 0) ? -1.2f : 1.2f;
                Vector3 escortTarget = targetPos + new Vector3(sideOffset, 0f, 0f);

                BezierPath escortPath = GenerateDiveBezierPath(escort.transform.position, escortTarget, sideOffset);

                escort.SetState(EnemyState.Diving);
                _activeDivingEnemies.Add(escort);

                BezierPathFollower escortFollower = escort.GetComponent<BezierPathFollower>();
                if (escortFollower != null)
                {
                    escortFollower.FollowPath(escortPath, _diveSpeed, () => OnDivePathCompleted(escort));
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

            // 화면 상단 재진입 위치 설정
            float reentryX = Mathf.Clamp(enemy.transform.position.x, -6.0f, 6.0f);
            enemy.transform.position = new Vector3(reentryX, _screenTopReentryY, 0f);

            Vector3 targetSlotPos = enemy.SlotAnchorPosition;
            if (_gridManager != null && enemy.AssignedSlot != null)
            {
                targetSlotPos = enemy.AssignedSlot.WorldPosition;
            }

            BezierPath returnPath = GenerateReturnBezierPath(enemy.transform.position, targetSlotPos);

            BezierPathFollower follower = enemy.GetComponent<BezierPathFollower>();
            if (follower != null)
            {
                follower.FollowPath(returnPath, _returnSpeed, () => OnReturnCompleted(enemy));
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
            predictedPos.y = -8.0f; // Y좌표는 항상 플레이어 라인 고정

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
        /// 시작점에서 목표 플레이어 위치를 향해 루프 비행하며 화면 하단으로 빠져나가는 3차 베지어 경로를 생성합니다.
        /// </summary>
        public BezierPath GenerateDiveBezierPath(Vector3 start, Vector3 target, float horizontalBias = 0f)
        {
            BezierPath path = new BezierPath();

            // 곡선 1: 그리드 이탈 및 급선회 루프 (Start -> Swoop)
            float sign = (start.x > target.x) ? -1f : 1f;
            Vector3 p0 = start;
            Vector3 p1 = start + new Vector3(sign * 2.0f + horizontalBias, 2.0f, 0f);
            Vector3 p2 = start + new Vector3(sign * 4.0f + horizontalBias, -3.0f, 0f);
            Vector3 p3 = new Vector3((start.x + target.x) * 0.5f + horizontalBias, 0f, 0f);
            path.AddSegment(new BezierSegment(p0, p1, p2, p3));

            // 곡선 2: 플레이어 조준 강하 및 화면 하단 통과 (Swoop -> Player Dive -> Bottom Exit)
            Vector3 q0 = p3;
            Vector3 q1 = target + new Vector3(-sign * 1.5f + horizontalBias, 4.0f, 0f);
            Vector3 q2 = target + new Vector3(0f, 1.0f, 0f);
            Vector3 q3 = new Vector3(target.x + sign * 2.5f + horizontalBias, _screenBottomY, 0f);
            path.AddSegment(new BezierSegment(q0, q1, q2, q3));

            return path;
        }

        /// <summary>
        /// 화면 상단 재진입점에서 소속 그리드 슬롯으로 복귀하는 3차 베지어 곡선 경로를 생성합니다.
        /// </summary>
        public BezierPath GenerateReturnBezierPath(Vector3 reentryPos, Vector3 targetSlot)
        {
            BezierPath path = new BezierPath();

            Vector3 p0 = reentryPos;
            Vector3 p1 = reentryPos + new Vector3(0f, -3.0f, 0f);
            Vector3 p2 = targetSlot + new Vector3((reentryPos.x > targetSlot.x ? 2.0f : -2.0f), 2.0f, 0f);
            Vector3 p3 = targetSlot;

            path.AddSegment(new BezierSegment(p0, p1, p2, p3));
            return path;
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
