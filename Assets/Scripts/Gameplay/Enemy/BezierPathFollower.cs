using System;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 3차 베지어 곡선 경로를 따라 오브젝트를 등속/가변 속도로 이동시키고 회전을 제어하는 경로 추적 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class BezierPathFollower : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("초당 이동 속도 (units/sec)")]
        [SerializeField] private float _moveSpeed = 10f;

        [Tooltip("이동 궤적의 진행 방향 접선에 맞춰 오브젝트를 2D 회전 정렬할지 여부")]
        [SerializeField] private bool _rotateAlongPath = true;

        [Tooltip("2D 회전 각도 오프셋 (기본 스프라이트가 상향(+Y) 기준일 때 접선 정렬용 -90도)")]
        [SerializeField] private float _rotationOffset = -90f;

        [Tooltip("경로 종점 도달 시 처음부터 반복 순환할지 여부")]
        [SerializeField] private bool _loop = false;

        [Tooltip("컴포넌트 활성화 시 자동으로 경로 이동을 시작할지 여부")]
        [SerializeField] private bool _autoPlayOnEnable = false;

        [Tooltip("경로 이동 완료 시 게임오브젝트를 비활성화할지 여부")]
        [SerializeField] private bool _disableOnComplete = false;

        [Header("Path Data")]
        [Tooltip("추적할 3차 베지어 곡선 세그먼트 배열")]
        [SerializeField] private BezierSegment[] _segments = Array.Empty<BezierSegment>();

        [Header("Gizmo Settings")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [SerializeField] private int _gizmoResolution = 20;

        private float _progress = 0f;
        private float _totalPathLength = 0f;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private int _currentSegmentIndex = 0;
        private Vector2 _currentTangent = Vector2.up;

        public event Action OnPathStarted;
        public event Action OnPathCompleted;
        public event Action<float> OnProgressChanged;
        public event Action<int> OnSegmentChanged;

        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = Mathf.Max(0f, value);
        }

        public bool RotateAlongPath
        {
            get => _rotateAlongPath;
            set => _rotateAlongPath = value;
        }

        public float RotationOffset
        {
            get => _rotationOffset;
            set => _rotationOffset = value;
        }

        public bool Loop
        {
            get => _loop;
            set => _loop = value;
        }

        public bool AutoPlayOnEnable
        {
            get => _autoPlayOnEnable;
            set => _autoPlayOnEnable = value;
        }

        public bool DisableOnComplete
        {
            get => _disableOnComplete;
            set => _disableOnComplete = value;
        }

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public float Progress => _progress;
        public int CurrentSegmentIndex => _currentSegmentIndex;
        public float TotalPathLength => _totalPathLength;
        public Vector2 CurrentTangent => _currentTangent;
        public BezierSegment[] Segments => _segments;

        private void Awake()
        {
            RecalculatePathLength();
        }

        private void OnEnable()
        {
            if (_autoPlayOnEnable && _segments != null && _segments.Length > 0)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            _isPlaying = false;
            _isPaused = false;
        }

        private void Update()
        {
            if (_isPlaying && !_isPaused)
            {
                UpdatePathFollow(Time.deltaTime);
            }
        }

        /// <summary>
        /// 다중 세그먼트 경로 및 속도를 설정합니다.
        /// </summary>
        public void SetPath(BezierSegment[] segments, float speed, bool loop = false)
        {
            _segments = segments ?? Array.Empty<BezierSegment>();
            _moveSpeed = Mathf.Max(0f, speed);
            _loop = loop;
            _progress = 0f;
            _currentSegmentIndex = 0;
            RecalculatePathLength();
        }

        /// <summary>
        /// 단일 3차 베지어 곡선 제어점 및 속도를 설정합니다.
        /// </summary>
        public void SetPath(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float speed, bool loop = false)
        {
            _segments = new BezierSegment[]
            {
                new BezierSegment(p0, p1, p2, p3)
            };
            _moveSpeed = Mathf.Max(0f, speed);
            _loop = loop;
            _progress = 0f;
            _currentSegmentIndex = 0;
            RecalculatePathLength();
        }

        /// <summary>
        /// 현재 경로 이동을 시작합니다.
        /// </summary>
        public void Play(float startProgress = 0f)
        {
            if (_segments == null || _segments.Length == 0)
            {
                return;
            }

            RecalculatePathLength();
            _progress = Mathf.Clamp01(startProgress);
            _isPlaying = true;
            _isPaused = false;
            _currentSegmentIndex = CalculateSegmentIndex(_progress);

            ApplyPositionAndRotation(_progress);
            OnPathStarted?.Invoke();
            OnProgressChanged?.Invoke(_progress);
        }

        /// <summary>
        /// 경로 이동을 일시 정지합니다.
        /// </summary>
        public void Pause()
        {
            if (_isPlaying)
            {
                _isPaused = true;
            }
        }

        /// <summary>
        /// 일시 정지된 경로 이동을 재개합니다.
        /// </summary>
        public void Resume()
        {
            if (_isPlaying && _isPaused)
            {
                _isPaused = false;
            }
        }

        /// <summary>
        /// 경로 이동을 중단하고 상태를 초기화합니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
        }

        /// <summary>
        /// 진행도를 특정 위치(0~1)로 수동 리셋합니다.
        /// </summary>
        public void ResetProgress(float progress = 0f)
        {
            _progress = Mathf.Clamp01(progress);
            _currentSegmentIndex = CalculateSegmentIndex(_progress);
            if (_segments != null && _segments.Length > 0)
            {
                ApplyPositionAndRotation(_progress);
            }
            OnProgressChanged?.Invoke(_progress);
        }

        /// <summary>
        /// DeltaTime 기반으로 경로 진행도를 갱신하고 위치/회전을 이동시킵니다. (테스트 및 수동 시뮬레이션용)
        /// </summary>
        public void UpdatePathFollow(float deltaTime)
        {
            if (_segments == null || _segments.Length == 0)
            {
                _isPlaying = false;
                return;
            }

            if (_totalPathLength <= 0.0001f)
            {
                _progress = 1f;
                ApplyPositionAndRotation(1f);
                CompletePath();
                return;
            }

            float distanceMoved = _moveSpeed * deltaTime;
            float progressDelta = distanceMoved / _totalPathLength;
            _progress += progressDelta;

            if (_progress >= 1f)
            {
                if (_loop)
                {
                    _progress = _progress % 1f;
                    int newIndex = CalculateSegmentIndex(_progress);
                    if (newIndex != _currentSegmentIndex)
                    {
                        _currentSegmentIndex = newIndex;
                        OnSegmentChanged?.Invoke(_currentSegmentIndex);
                    }
                    ApplyPositionAndRotation(_progress);
                    OnProgressChanged?.Invoke(_progress);
                }
                else
                {
                    _progress = 1f;
                    ApplyPositionAndRotation(1f);
                    OnProgressChanged?.Invoke(1f);
                    CompletePath();
                }
            }
            else
            {
                int newIndex = CalculateSegmentIndex(_progress);
                if (newIndex != _currentSegmentIndex)
                {
                    _currentSegmentIndex = newIndex;
                    OnSegmentChanged?.Invoke(_currentSegmentIndex);
                }
                ApplyPositionAndRotation(_progress);
                OnProgressChanged?.Invoke(_progress);
            }
        }

        private void CompletePath()
        {
            _isPlaying = false;
            _isPaused = false;
            OnPathCompleted?.Invoke();

            if (_disableOnComplete)
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyPositionAndRotation(float progress)
        {
            Vector2 position = BezierCurve.EvaluatePath(_segments, progress);
            transform.position = new Vector3(position.x, position.y, transform.position.z);

            _currentTangent = BezierCurve.GetPathTangent(_segments, progress);

            if (_rotateAlongPath && _currentTangent.sqrMagnitude > 0.0001f)
            {
                float angle = (Mathf.Atan2(_currentTangent.y, _currentTangent.x) * Mathf.Rad2Deg) + _rotationOffset;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private int CalculateSegmentIndex(float progress)
        {
            if (_segments == null || _segments.Length <= 1)
            {
                return 0;
            }

            float totalProgress = Mathf.Clamp01(progress) * _segments.Length;
            return Mathf.Min(Mathf.FloorToInt(totalProgress), _segments.Length - 1);
        }

        private void RecalculatePathLength()
        {
            _totalPathLength = BezierCurve.CalculatePathLength(_segments);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos || _segments == null || _segments.Length == 0)
            {
                return;
            }

            Gizmos.color = _gizmoColor;
            for (int s = 0; s < _segments.Length; s++)
            {
                BezierSegment seg = _segments[s];

                // 제어선 표시
                Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
                Gizmos.DrawLine(seg.p0, seg.p1);
                Gizmos.DrawLine(seg.p2, seg.p3);

                // 제어점 표시
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(seg.p0, 0.15f);
                Gizmos.DrawWireSphere(seg.p3, 0.15f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(seg.p1, 0.1f);
                Gizmos.DrawWireSphere(seg.p2, 0.1f);

                // 곡선 선분 렌더링
                Gizmos.color = _gizmoColor;
                Vector2 prevPoint = seg.p0;
                int resolution = Mathf.Max(4, _gizmoResolution);
                for (int i = 1; i <= resolution; i++)
                {
                    float t = (float)i / resolution;
                    Vector2 currentPoint = seg.Evaluate(t);
                    Gizmos.DrawLine(prevPoint, currentPoint);
                    prevPoint = currentPoint;
                }
            }
        }
    }
}
