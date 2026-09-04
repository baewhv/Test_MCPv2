using System;
using UnityEngine;

// [사용자 C# 코드 스타일 템플릿 / Reference Code Style Template]
// 사용자의 고유 코딩 컨벤션 및 스타일을 이곳에 주입하여 에이전트가 완벽히 모방하도록 합니다.

/// <summary>
/// 컴포넌트 역할 및 기능 요약
/// </summary>
public class SamplePlayerController : MonoBehaviour
{
    // -------------------------------------------------------------
    // 1. 인스펙터 직렬화 필드 (Serialized Fields: [SerializeField] private)
    // -------------------------------------------------------------
    [Header("Movement Configuration")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Rigidbody2D rb;

    // -------------------------------------------------------------
    // 2. 프로퍼티 (Properties: 캡슐화된 읽기 전용/자동 프로퍼티)
    // -------------------------------------------------------------
    public float MoveSpeed => moveSpeed;
    public bool IsMoving { get; private set; }

    // -------------------------------------------------------------
    // 3. C# 액션 및 이벤트 (Events / Actions)
    // -------------------------------------------------------------
    public event Action<Vector2> OnMoved;

    // -------------------------------------------------------------
    // 4. 유니티 생명주기 메서드 (Unity Lifecycle Methods)
    // -------------------------------------------------------------
    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void OnDisable()
    {
        // 이벤트 리스너 해제 및 null 초기화 (메모리 누수 방지)
        OnMoved = null;
    }

    private void Update()
    {
        HandleMovementInput();
    }

    // -------------------------------------------------------------
    // 5. 공개 메서드 (Public API Contract)
    // -------------------------------------------------------------
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    // -------------------------------------------------------------
    // 6. 내부 구현 로직 (Private Helper Methods)
    // -------------------------------------------------------------
    private void HandleMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        IsMoving = !Mathf.Approximately(x, 0f);

        if (IsMoving)
        {
            OnMoved?.Invoke(new Vector2(x, 0f));
        }
    }
}
