---
name: unity-coding-rule
description: 유니티 C# 스크립트 작성 시 [SerializeField] private 직렬화 캡슐화, OnDisable 이벤트 해제, Fake Null 검사, Animator.StringToHash 캐싱, 부하 유발 Search API 제한, 네임스페이스(namespace) 사용 일체 금지, .meta GUID 보존, Deprecated API 금지 및 TestCode 표준을 준수하는 C# 코딩 표준 스킬
---

# 유니티 C# 코딩 표준 스킬 (Unity C# Coding Skill)

이 스킬은 프로젝트의 모든 유니티 C# 스크립트 작성 시 반드시 준수해야 하는 C# 코딩 컨벤션, 네임스페이스 제한, 메모리 안전성, .meta GUID 보존, Deprecated API 배제, Fake Null 검사 및 컴파일 검증 표준 지침입니다.

---

## 1. 직렬화 필드 캡슐화 및 명명 규칙 (Serialization & Naming)
1. **인스펙터 노출 필드**:
   - `public` 변수 선언을 엄격히 금지하며, 반드시 **`[SerializeField] private`** 속성을 사용하여 캡슐화합니다.
2. **카멜 표기법 (camelCase)**:
   - 인스펙터 노출 변수는 `camelCase`로 작성합니다. (예: `[SerializeField] private float moveSpeed;`)
3. **가독성 Header 그룹화**:
   - 관련된 인스펙터 필드는 `[Header("그룹명")]`을 사용하여 인스펙터 상에서 명확하게 구분합니다.
4. **외부 읽기 접근**:
   - 외부 클래스에서 접근해야 하는 변수는 람다 프로퍼티(Expression-bodied property) 또는 `{ get; private set; }`을 사용합니다.
   - 예시: `public float MoveSpeed => moveSpeed;`

---

## 2. 생명주기 및 메모리 누수 방지 (Lifecycle & Event Cleanup)
1. **이벤트 구독 해제 의무**:
   - C# `event Action` 또는 델리게이트 구독 시, **반드시 `OnDisable()` 또는 `OnDestroy()`에서 `-=`로 구독을 해제**하거나 `null`로 초기화하여 메모리 누수를 원천 방지합니다.
2. **Fake Null 안전 검사 (Unity Object)**:
   - `UnityEngine.Object`를 상속받은 객체(MonoBehaviour, GameObject, Component 등)에 대해 C#의 널 조건부 연산자(`?.`, `??`) 사용을 지양하고, **명시적 `if (obj != null)` 검사**를 수행합니다 (유니티 C++ 언더레이어 파괴 객체의 Fake Null 문제 방지).
3. **애니메이터 파라미터 해시 캐싱**:
   - `animator.SetTrigger("Attack")`과 같은 문자열 기반 호출을 금지하고, **`private static readonly int AnimAttack = Animator.StringToHash("Attack");`** 형태로 정적 정수 해시를 사전 캐싱하여 사용합니다.

---

## 3. 부하 유발 Search API 전면 금지 및 보류 원칙 (No Expensive Search APIs)
1. **절대 금지 API (Update / 프레임 루프 내)**:
   - `Update()`, `FixedUpdate()`, `LateUpdate()` 및 코루틴 반복 루프 내에서 아래의 씬 전수 탐색 API 호출을 전면 금지합니다:
     - `FindObjectOfType`, `FindObjectsOfType`, `GameObject.Find`, `GameObject.FindWithTag`
     - `GetComponentsInChildren`, `GetComponentInChildren`, `GetComponentsInParent`
2. **표준 해결 원칙**:
   - 인스펙터 드래그 앤 드롭 바인딩 (`[SerializeField] private`) 또는 `Awake()`/`Start()` 1회 캐싱을 기본으로 사용합니다.

---

## 4. C# 스크립트 수정 및 .meta GUID 영구 보존 원칙 (Meta Integrity)
1. **`delete_script` 호출 엄격 금지 (Missing Mono Script 방지)**:
   - 기존 C# 스크립트 수정 시 편의를 위해 스크립트를 삭제(`delete_script`) 후 재생성하는 행위를 엄격히 금지합니다.
   - 스크립트 삭제 시 고유 식별자인 `.meta` 파일(GUID)이 함께 삭제되어, 해당 스크립트를 바인딩하고 있던 **모든 프리팹 및 씬에서 Missing (Mono Script) 결함이 발생**합니다.
2. **In-place 파일 수정 필수**:
   - 파일 수정 시 반드시 로컬 파일 시스템 도구(In-place Overwrite 또는 `replace_file_content`)를 사용하여 `.meta` 파일의 GUID를 100% 보존해야 합니다.

---

## 5. Deprecated (Obsolete) API 사용 전면 금지 및 최신 권장 API 대체 원칙
Unity 엔진 버전 업데이트에 따라 사용 중단(`[Obsolete]`)되었거나 경고(`CS0618`)를 유발하는 구식 코드는 사용을 엄격히 금지하며, 반드시 아래의 최신 권장 API로 대체합니다:

| 구식 Deprecated API (사용 금지) | 최신 표준 권장 API (대체 필수) | 설명 |
| :--- | :--- | :--- |
| `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` 또는 `FindAnyObjectByType<T>()` | Unity 2023+ 권장 (정렬 오버헤드 제거) |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.None)` | Unity 2023+ 권장 (명시적 정렬 모드 지정) |
| `Input.GetKey / Input.GetAxis` | `UnityEngine.InputSystem` (New Input System) | 레거시 인풋 매니저 배제 |
| `Application.LoadLevel` | `SceneManager.LoadScene` | 씬 매니저 최신 API 사용 |
| `WWW` | `UnityWebRequest` | 레거시 네트워크 객체 배제 |

---

## 6. 네임스페이스(namespace) 사용 일체 금지 원칙 (No-Namespace Rule)
- **원칙**: 유니티 C# 스크립트 작성 시 **`namespace` 키워드를 일체 사용하지 않고 최상위 전역 스코프에 클래스를 정의**합니다.
- **이유**: 불필요한 인덴트 깊이 증가, `using` 구문 남발, 어셈블리 정의 파일(`.asmdef`)과의 불필요한 결합 복잡도를 방지하고 코드를 가장 직관적이고 단순하게 유지합니다.

---

## 7. 테스트 코드(TestCode) 작성 및 폴더 분류 표준 (Test Code Standards)
1. **폴더 분류 기준**:
   - **`Assets/Tests/Editor/` (EditMode Tests)**: 유니티 엔진 런타임 없이 C# 순수 로직, 수학 공식, ScriptableObject 데이터 정합성, 상태 머신 전이를 초고속 검증하는 단위 테스트.
   - **`Assets/Tests/Runtime/` (PlayMode Tests)**: 씬 로드, 물리 충돌(`Collider2D`), 스포너 생성 및 풀링 수명주기를 검증하는 통합 테스트.
2. **테스트 파일 명명 컨벤션**:
   - 반드시 **`[대상클래스명]Tests.cs`** 형식으로 작성합니다. (예: `PlayerShootingTests.cs`, `EnemyBaseTests.cs`)

---

## 8. 사용자 맞춤 코드 스타일 참조 템플릿
- 구체적인 클래스 필드 배치, 메서드 구조, 주석 스타일은 **`.agents/skills/unity-coding-rule/references/code_style_sample.cs`** 템플릿을 기본 참조 모델로 삼아 일관되게 코딩합니다.
