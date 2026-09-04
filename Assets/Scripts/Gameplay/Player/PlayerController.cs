using UnityEngine;
using UnityEngine.InputSystem;
using Galaga.Core;

namespace Galaga.Gameplay.Player
{
    /// <summary>
    /// 플레이어 단일 기체(Single Fighter)의 1차원 수평 이동 및 화면 경계 클램핑을 제어하는 컨트롤러입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("월드 기준 수평 이동 속도 (기본 150 px/sec 환산치 약 10.4 units/sec)")]
        [SerializeField] private float _moveSpeed = 10.4f;

        [Tooltip("기체 좌우 히트박스/외형 절반 너비")]
        [SerializeField] private float _halfWidth = 0.45f;

        [Tooltip("기체 고정 Y좌표 (화면 최하단 고정)")]
        [SerializeField] private float _fixedYPosition = -8.0f;

        [Header("References")]
        [Tooltip("화면 경계 클램프 계산을 위한 PlayAreaManager")]
        [SerializeField] private PlayAreaManager _playAreaManager;

        [Tooltip("New Input System 이동 액션 참조")]
        [SerializeField] private InputActionReference _moveAction;

        private float _currentInputX;
        private bool _isExternalInput = false;

        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value;
        }

        public float HalfWidth
        {
            get => _halfWidth;
            set => _halfWidth = value;
        }

        public float FixedYPosition
        {
            get => _fixedYPosition;
            set => _fixedYPosition = value;
        }

        public PlayAreaManager PlayAreaManager
        {
            get => _playAreaManager;
            set => _playAreaManager = value;
        }

        public float CurrentInputX => _currentInputX;

        private void Awake()
        {
            // Y좌표 화면 최하단 고정 초기화
            Vector3 pos = transform.position;
            pos.y = _fixedYPosition;
            transform.position = pos;
        }

        private void Start()
        {
            if (_playAreaManager == null)
            {
                _playAreaManager = PlayAreaManager.Instance;
            }
            if (_playAreaManager == null && Camera.main != null)
            {
                _playAreaManager = Camera.main.GetComponent<PlayAreaManager>();
            }
        }

        private void OnEnable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Disable();
            }
        }

        private void Update()
        {
            ReadInput();
            Move(Time.deltaTime);
        }

        private void ReadInput()
        {
            if (_isExternalInput)
            {
                return;
            }

            float input = 0f;

            if (_moveAction != null && _moveAction.action != null)
            {
                Vector2 moveValue = _moveAction.action.ReadValue<Vector2>();
                input = moveValue.x;
            }
            else
            {
                // Fallback: New Input System Keyboard 직접 폴링
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
                    {
                        input -= 1f;
                    }
                    if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
                    {
                        input += 1f;
                    }
                }
            }

            _currentInputX = Mathf.Clamp(input, -1f, 1f);
        }

        /// <summary>
        /// 테스트 또는 외부 시스템(AI/도킹 시퀀스 등)에서 직접 이동 입력을 주입합니다.
        /// </summary>
        public void SetInputDirectly(float inputX)
        {
            _isExternalInput = true;
            _currentInputX = Mathf.Clamp(inputX, -1f, 1f);
        }

        /// <summary>
        /// 외부 직접 입력 모드를 해제하고 일반 입력 폴링으로 복귀합니다.
        /// </summary>
        public void ReleaseDirectInput()
        {
            _isExternalInput = false;
        }

        /// <summary>
        /// 입력과 deltaTime에 기반하여 수평 이동을 수행하고 화면 경계 내로 클램프합니다.
        /// </summary>
        public void Move(float deltaTime)
        {
            Vector3 pos = transform.position;
            pos.x += _currentInputX * _moveSpeed * deltaTime;
            pos.y = _fixedYPosition;

            if (_playAreaManager != null)
            {
                Vector2 clamped = _playAreaManager.ClampPosition(new Vector2(pos.x, pos.y), _halfWidth, 0f);
                pos.x = clamped.x;
            }

            transform.position = pos;
        }
    }
}
