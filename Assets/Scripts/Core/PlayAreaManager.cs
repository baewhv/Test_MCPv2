using UnityEngine;

namespace Galaga.Core
{
    /// <summary>
    /// 3:4 아케이드 비율(224x288) 기준 플레이 영역 경계 및 카메라 뷰포트를 관리하는 코어 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class PlayAreaManager : MonoBehaviour
    {
        [Header("Target Resolution & Aspect Ratio")]
        [Tooltip("기준 해상도 가로 픽셀")]
        [SerializeField] private float _targetWidth = 224f;

        [Tooltip("기준 해상도 세로 픽셀")]
        [SerializeField] private float _targetHeight = 288f;

        [Header("Camera & World Size Settings")]
        [Tooltip("월드 좌표계 기준 플레이 영역 세로 절반 크기 (Orthographic Size)")]
        [SerializeField] private float _orthographicSize = 10f;

        [Header("Boundary Colliders")]
        [Tooltip("좌우/상하 외곽 충돌체 자동 생성 여부")]
        [SerializeField] private bool _generateBoundaryColliders = true;

        [Tooltip("경계 충돌체 두께")]
        [SerializeField] private float _colliderThickness = 1f;

        [Tooltip("경계 충돌체 레이어")]
        [SerializeField] private string _boundaryLayerName = "Default";

        private Camera _targetCamera;
        private Rect _worldPlayBounds;

        public float TargetAspectRatio => _targetWidth / _targetHeight;
        public Rect WorldPlayBounds => _worldPlayBounds;
        public float MinX => _worldPlayBounds.xMin;
        public float MaxX => _worldPlayBounds.xMax;
        public float MinY => _worldPlayBounds.yMin;
        public float MaxY => _worldPlayBounds.yMax;
        public float PlayWidth => _worldPlayBounds.width;
        public float PlayHeight => _worldPlayBounds.height;

        private void Awake()
        {
            _targetCamera = GetComponent<Camera>();
            RecalculateBounds();

            if (_generateBoundaryColliders)
            {
                CreateBoundaryColliders();
            }
        }

        /// <summary>
        /// 3:4 타겟 종횡비에 맞춰 카메라 뷰포트 Rect 및 월드 플레이 영역 Rect를 재계산합니다.
        /// </summary>
        public void RecalculateBounds()
        {
            if (_targetCamera == null)
            {
                _targetCamera = GetComponent<Camera>();
            }

            if (_targetCamera == null)
            {
                return;
            }

            _targetCamera.orthographic = true;
            _targetCamera.orthographicSize = _orthographicSize;

            float targetAspect = TargetAspectRatio;
            float windowAspect = (float)Screen.width / Screen.height;
            float scaleHeight = windowAspect / targetAspect;

            if (scaleHeight < 1.0f)
            {
                // 화면이 타겟 비율보다 길쭉할 때 (레터박스)
                Rect rect = _targetCamera.rect;
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) * 0.5f;
                _targetCamera.rect = rect;
            }
            else
            {
                // 화면이 타겟 비율보다 넓을 때 (필러박스)
                float scaleWidth = 1.0f / scaleHeight;
                Rect rect = _targetCamera.rect;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) * 0.5f;
                rect.y = 0;
                _targetCamera.rect = rect;
            }

            float worldHeight = _orthographicSize * 2f;
            float worldWidth = worldHeight * targetAspect;
            Vector3 camPos = _targetCamera.transform.position;

            _worldPlayBounds = new Rect(
                camPos.x - (worldWidth * 0.5f),
                camPos.y - (worldHeight * 0.5f),
                worldWidth,
                worldHeight
            );
        }

        /// <summary>
        /// 주어진 좌표를 플레이 영역 좌우 경계 내로 제한합니다.
        /// </summary>
        public Vector2 ClampPosition(Vector2 position, float halfWidth = 0f, float halfHeight = 0f)
        {
            float clampedX = Mathf.Clamp(position.x, MinX + halfWidth, MaxX - halfWidth);
            float clampedY = Mathf.Clamp(position.y, MinY + halfHeight, MaxY - halfHeight);
            return new Vector2(clampedX, clampedY);
        }

        /// <summary>
        /// 주어진 좌표가 플레이 영역 화면 밖으로 벗어났는지 검사합니다.
        /// </summary>
        public bool IsOutOfBounds(Vector2 position, float margin = 0.5f)
        {
            return position.x < (MinX - margin) ||
                   position.x > (MaxX + margin) ||
                   position.y < (MinY - margin) ||
                   position.y > (MaxY + margin);
        }

        private void CreateBoundaryColliders()
        {
            int layer = LayerMask.NameToLayer(_boundaryLayerName);
            if (layer < 0)
            {
                layer = 0;
            }

            GameObject borderRoot = new GameObject("BoundaryColliders");
            borderRoot.transform.SetParent(transform);
            borderRoot.transform.localPosition = Vector3.zero;

            // 좌측 경계
            CreateBoxCollider(borderRoot, "LeftBorder",
                new Vector2(MinX - (_colliderThickness * 0.5f), _worldPlayBounds.center.y),
                new Vector2(_colliderThickness, PlayHeight + (_colliderThickness * 2f)),
                layer);

            // 우측 경계
            CreateBoxCollider(borderRoot, "RightBorder",
                new Vector2(MaxX + (_colliderThickness * 0.5f), _worldPlayBounds.center.y),
                new Vector2(_colliderThickness, PlayHeight + (_colliderThickness * 2f)),
                layer);

            // 상단 경계 (탄환/적 이탈 판정용)
            CreateBoxCollider(borderRoot, "TopBorder",
                new Vector2(_worldPlayBounds.center.x, MaxY + (_colliderThickness * 0.5f)),
                new Vector2(PlayWidth + (_colliderThickness * 2f), _colliderThickness),
                layer);

            // 하단 경계
            CreateBoxCollider(borderRoot, "BottomBorder",
                new Vector2(_worldPlayBounds.center.x, MinY - (_colliderThickness * 0.5f)),
                new Vector2(PlayWidth + (_colliderThickness * 2f), _colliderThickness),
                layer);
        }

        private void CreateBoxCollider(GameObject parent, string name, Vector2 worldPos, Vector2 size, int layer)
        {
            GameObject borderObj = new GameObject(name);
            borderObj.layer = layer;
            borderObj.transform.SetParent(parent.transform);
            borderObj.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            BoxCollider2D box = borderObj.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3(_worldPlayBounds.center.x, _worldPlayBounds.center.y, 0f);
            Vector3 size = new Vector3(_worldPlayBounds.width, _worldPlayBounds.height, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
