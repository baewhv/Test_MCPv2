using System;
using System.Collections;
using UnityEngine;
using Galaga.Gameplay.Player;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 보스 갤러그의 트랙터 빔 전개, 사다리꼴 콜라이더 영역 판정, 시각적 렌더링 및 빔 타이머 수명주기를 제어하는 컴포넌트입니다.
    /// 규격: 상단 너비 0.556u (8px), 하단 너비 3.333u (48px), 높이 8.333u (120px)
    /// </summary>
    [DisallowMultipleComponent]
    public class BossTractorBeam : MonoBehaviour
    {
        [Header("Collider Settings")]
        [Tooltip("트랙터 빔 영역 감지용 2D 폴리곤 콜라이더")]
        [SerializeField] private PolygonCollider2D _polygonCollider;

        [Tooltip("상단 너비 (보스 하단 배출구 폭, 기본 0.556 units)")]
        [SerializeField] private float _topWidth = 0.556f;

        [Tooltip("하단 너비 (플레이어 고도 도달 폭, 기본 3.333 units)")]
        [SerializeField] private float _bottomWidth = 3.333f;

        [Tooltip("트랙터 빔 높이 (보스 호버 위치에서 플레이어 라인까지, 기본 8.333 units)")]
        [SerializeField] private float _beamHeight = 8.333f;

        [Tooltip("콜라이더 및 메시 로컬 오프셋")]
        [SerializeField] private Vector2 _beamOffset = Vector2.zero;

        [Header("Visual & Animation")]
        [Tooltip("트랙터 빔 절차적 메시 필터")]
        [SerializeField] private MeshFilter _meshFilter;

        [Tooltip("트랙터 빔 렌더러")]
        [SerializeField] private MeshRenderer _meshRenderer;

        [Tooltip("추가 시각 라인 렌더러 (선택 사항)")]
        [SerializeField] private LineRenderer _lineRenderer;

        [Tooltip("빔 색상 순환 주기 속도 (Hz)")]
        [SerializeField] private float _colorCycleSpeed = 15.0f;

        [Tooltip("순환할 빔 색상 팔레트")]
        [SerializeField] private Color[] _cycleColors = new Color[]
        {
            new Color(0.2f, 0.85f, 1.0f, 0.75f), // Cyan
            new Color(0.1f, 0.45f, 1.0f, 0.70f), // Blue
            new Color(0.9f, 0.95f, 1.0f, 0.85f), // White
            new Color(0.3f, 0.70f, 1.0f, 0.75f)  // Sky Blue
        };

        [Header("Beam Timing & State")]
        [Tooltip("트랙터 빔 기본 전개 지속 시간 (초)")]
        [SerializeField] private float _beamDuration = 4.0f;

        [Tooltip("소유주 보스 기체 참조")]
        [SerializeField] private EnemyBase _ownerBoss;

        [Tooltip("런타임 빔 활성화 상태")]
        [SerializeField] private bool _isBeamActive = false;

        private Coroutine _beamTimerCoroutine;
        private float _currentTimer = 0f;
        private Mesh _proceduralMesh;
        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public event Action OnBeamActivated;
        public event Action OnBeamDeactivated;
        public event Action<Collider2D> OnTargetCaptured;
        public event Action OnBeamTimeout;

        public PolygonCollider2D PolygonCollider => _polygonCollider;
        public MeshFilter MeshFilterComponent => _meshFilter;
        public MeshRenderer MeshRendererComponent => _meshRenderer;
        public LineRenderer LineRendererComponent => _lineRenderer;
        public bool IsBeamActive => _isBeamActive;
        public float BeamDuration
        {
            get => _beamDuration;
            set => _beamDuration = Mathf.Max(0.1f, value);
        }
        public float RemainingTime => Mathf.Max(0f, _beamDuration - _currentTimer);
        public float TopWidth
        {
            get => _topWidth;
            set
            {
                _topWidth = value;
                UpdateColliderAndMesh();
            }
        }
        public float BottomWidth
        {
            get => _bottomWidth;
            set
            {
                _bottomWidth = value;
                UpdateColliderAndMesh();
            }
        }
        public float BeamHeight
        {
            get => _beamHeight;
            set
            {
                _beamHeight = value;
                UpdateColliderAndMesh();
            }
        }
        public Vector2 BeamOffset
        {
            get => _beamOffset;
            set
            {
                _beamOffset = value;
                UpdateColliderAndMesh();
            }
        }
        public EnemyBase OwnerBoss
        {
            get => _ownerBoss;
            set
            {
                if (_ownerBoss != null)
                {
                    _ownerBoss.OnDestroyed -= HandleOwnerDestroyed;
                }
                _ownerBoss = value;
                if (_ownerBoss != null)
                {
                    _ownerBoss.OnDestroyed += HandleOwnerDestroyed;
                }
            }
        }

        private void Awake()
        {
            if (_polygonCollider == null)
            {
                _polygonCollider = GetComponent<PolygonCollider2D>();
            }

            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }

            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }

            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }

            if (_ownerBoss == null)
            {
                _ownerBoss = GetComponentInParent<EnemyBase>();
            }

            _propBlock = new MaterialPropertyBlock();
            UpdateColliderAndMesh();
            SetVisualActive(false);
        }

        private void OnEnable()
        {
            if (_ownerBoss != null)
            {
                _ownerBoss.OnDestroyed += HandleOwnerDestroyed;
            }
        }

        private void OnDisable()
        {
            DeactivateBeam();

            if (_ownerBoss != null)
            {
                _ownerBoss.OnDestroyed -= HandleOwnerDestroyed;
            }

            OnBeamActivated = null;
            OnBeamDeactivated = null;
            OnTargetCaptured = null;
            OnBeamTimeout = null;
        }

        private void OnDestroy()
        {
            if (_proceduralMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_proceduralMesh);
                }
                else
                {
                    DestroyImmediate(_proceduralMesh);
                }
                _proceduralMesh = null;
            }
        }

        private void Update()
        {
            if (!_isBeamActive)
            {
                return;
            }

            AlignToWorldDown();
            UpdateVisualAnimation();
        }

        private void LateUpdate()
        {
            if (_isBeamActive)
            {
                AlignToWorldDown();
            }
        }

        /// <summary>
        /// 부모 보스 기체의 회전 상태와 관계없이 트랙터 빔이 월드 좌표계 기준 하향(-Y)을 향하도록 트랜스폼 회전을 정렬합니다.
        /// </summary>
        public void AlignToWorldDown()
        {
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 사다리꼴 꼭짓점 로컬 좌표 배열을 계산하여 반환합니다.
        /// 꼭짓점 순서: Top-Left (0), Top-Right (1), Bottom-Right (2), Bottom-Left (3)
        /// </summary>
        public Vector2[] GetLocalVertices()
        {
            float halfTop = _topWidth * 0.5f;
            float halfBottom = _bottomWidth * 0.5f;

            return new Vector2[]
            {
                new Vector2(-halfTop, 0f) + _beamOffset,
                new Vector2(halfTop, 0f) + _beamOffset,
                new Vector2(halfBottom, -_beamHeight) + _beamOffset,
                new Vector2(-halfBottom, -_beamHeight) + _beamOffset
            };
        }

        /// <summary>
        /// 월드 좌표계 기준의 사다리꼴 꼭짓점 배열을 반환합니다.
        /// </summary>
        public Vector2[] GetWorldVertices()
        {
            Vector2[] local = GetLocalVertices();
            Vector2[] world = new Vector2[local.Length];
            for (int i = 0; i < local.Length; i++)
            {
                world[i] = transform.TransformPoint(local[i]);
            }
            return world;
        }

        /// <summary>
        /// 인스펙터 설정 규격에 맞추어 PolygonCollider2D 및 절차적 2D 사다리꼴 메시를 갱신합니다.
        /// </summary>
        public void UpdateColliderAndMesh()
        {
            Vector2[] points = GetLocalVertices();

            if (_polygonCollider != null)
            {
                _polygonCollider.isTrigger = true;
                _polygonCollider.SetPath(0, points);
            }

            UpdateProceduralMesh(points);
            UpdateLineRenderer(points);
        }

        private void UpdateProceduralMesh(Vector2[] points)
        {
            if (_meshFilter == null)
            {
                return;
            }

            if (_proceduralMesh == null)
            {
                _proceduralMesh = new Mesh();
                _proceduralMesh.name = "TractorBeamMesh";
                _meshFilter.sharedMesh = _proceduralMesh;
            }

            Vector3[] vertices = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = new Vector3(points[i].x, points[i].y, 0f);
            }

            int[] triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f)
            };

            _proceduralMesh.Clear();
            _proceduralMesh.vertices = vertices;
            _proceduralMesh.triangles = triangles;
            _proceduralMesh.uv = uvs;
            _proceduralMesh.RecalculateNormals();
            _proceduralMesh.RecalculateBounds();
        }

        private void UpdateLineRenderer(Vector2[] points)
        {
            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.positionCount = 5;
            _lineRenderer.useWorldSpace = false;
            for (int i = 0; i < 4; i++)
            {
                _lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
            }
            _lineRenderer.SetPosition(4, new Vector3(points[0].x, points[0].y, 0f));
        }

        /// <summary>
        /// 지정된 지속 시간 동안 트랙터 빔을 전개합니다.
        /// </summary>
        /// <param name="duration">빔 유지 시간(초). 음수 전달 시 기본 _beamDuration 적용</param>
        public void ActivateBeam(float duration = -1f)
        {
            if (duration > 0f)
            {
                _beamDuration = duration;
            }

            _isBeamActive = true;
            _currentTimer = 0f;
            AlignToWorldDown();

            if (_polygonCollider != null)
            {
                _polygonCollider.enabled = true;
            }

            SetVisualActive(true);

            if (_beamTimerCoroutine != null)
            {
                StopCoroutine(_beamTimerCoroutine);
            }

            if (gameObject.activeInHierarchy)
            {
                _beamTimerCoroutine = StartCoroutine(BeamTimerRoutine());
            }

            OnBeamActivated?.Invoke();
        }

        /// <summary>
        /// 트랙터 빔을 즉시 회수하고 비활성화합니다.
        /// </summary>
        public void DeactivateBeam()
        {
            if (!_isBeamActive && _beamTimerCoroutine == null)
            {
                return;
            }

            _isBeamActive = false;
            _currentTimer = 0f;

            if (_beamTimerCoroutine != null)
            {
                StopCoroutine(_beamTimerCoroutine);
                _beamTimerCoroutine = null;
            }

            if (_polygonCollider != null)
            {
                _polygonCollider.enabled = false;
            }

            SetVisualActive(false);
            OnBeamDeactivated?.Invoke();
        }

        private IEnumerator BeamTimerRoutine()
        {
            while (_currentTimer < _beamDuration)
            {
                _currentTimer += Time.deltaTime;
                yield return null;
            }

            _beamTimerCoroutine = null;
            OnBeamTimeout?.Invoke();
            DeactivateBeam();
        }

        private void UpdateVisualAnimation()
        {
            if (_cycleColors == null || _cycleColors.Length == 0)
            {
                return;
            }

            int colorIndex = (int)(Time.time * _colorCycleSpeed) % _cycleColors.Length;
            Color currentColor = _cycleColors[colorIndex];

            if (_meshRenderer != null)
            {
                if (_propBlock == null)
                {
                    _propBlock = new MaterialPropertyBlock();
                }

                _meshRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, currentColor);
                _propBlock.SetColor(ColorId, currentColor);
                _meshRenderer.SetPropertyBlock(_propBlock);
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.startColor = currentColor;
                _lineRenderer.endColor = currentColor;
            }
        }

        private void SetVisualActive(bool active)
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = active;
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = active;
            }
        }

        private void HandleOwnerDestroyed(EnemyBase boss)
        {
            DeactivateBeam();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!_isBeamActive || collision == null)
            {
                return;
            }

            // 플레이어 충돌 감지
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player == null)
            {
                player = collision.GetComponentInParent<PlayerController>();
            }

            bool isPlayer = player != null ||
                            collision.CompareTag("Player") ||
                            collision.name.Contains("Player") ||
                            collision.GetComponent<PlayerHealth>() != null;

            if (isPlayer)
            {
                OnTargetCaptured?.Invoke(collision);

                if (player != null && FighterCaptureController.Instance != null && !FighterCaptureController.Instance.IsCapturing)
                {
                    EnemyBoss boss = _ownerBoss != null ? _ownerBoss.GetComponent<EnemyBoss>() : GetComponentInParent<EnemyBoss>();
                    FighterCaptureController.Instance.StartCaptureSequence(player, this, boss);
                }
            }
        }
    }
}
