# Unity & Rider MCP 도구 레퍼런스 (MCP Tools Reference)

이 문서는 프로젝트에 연동된 **Unity MCP (`unityMCP` 48종)** 및 **JetBrains Rider MCP (`jetbrains-companion` 4종)** 도구의 전체 목록과 기능 명세를 체계적으로 정리한 참조 문서입니다.

---

## 1. JetBrains Rider MCP 도구 목록 (4종)

Rider IDE와의 실시간 정적 분석 및 에디터 뷰포트 연동을 제공하는 전용 도구입니다.

| 도구명 (Tool Name) | 주요 기능 및 설명 | 핵심 파라미터 |
| :--- | :--- | :--- |
| **`ide_get_active_editor`** | 현재 Rider에서 포커스된 활성 에디터의 파일 경로, 커서 위치(Line/Column), 선택 텍스트 영역을 조회합니다. | - |
| **`ide_get_open_files`** | 현재 Rider 에디터에 열려 있는 모든 파일 목록 및 탭 상태를 조회합니다. | - |
| **`ide_get_diagnostics`** | C# 소스코드의 정적 분석 진단 결과(컴파일 에러, 경고, 네이밍 컨벤션 위반, 리팩토링 제안)를 실시간 조회합니다. | `path`, `severity` |
| **`ide_open_file`** | 지정한 파일 경로의 특정 라인 및 컬럼 위치로 Rider 에디터 화면을 즉시 전환하여 엽니다. | `path`, `line`, `column` |

---

## 2. Unity MCP 도구 목록 (48종)

유니티 에디터의 씬, 프리팹, 컴포넌트, 물리, 애니메이션, 렌더링, 생성형 AI 및 테스트를 직접 제어하는 도구 모음입니다.

### 2.1 Prefab, Component & ScriptableObject (프리팹 및 직렬화 제어)
> [!TIP]
> 프리팹 수정 시 `.prefab` YAML을 텍스트로 직접 수정하지 않고 아래 전용 도구를 사용하면 토큰 소모를 90% 이상 절감하고 Missing Reference를 원천 차단합니다.

| 도구명 (Tool Name) | 주요 기능 및 설명 | 핵심 파라미터 / 예시 |
| :--- | :--- | :--- |
| **`manage_prefabs`** | 프리팹 생성, 계층구조 조회, 자식 오브젝트 생성/삭제, 컴포넌트 직렬화 프로퍼티(`component_properties`) 일괄 수정 | `action: "modify_contents"`, `prefab_path`, `component_properties`, `create_child` |
| **`manage_components`** | GameObject에 컴포넌트 추가/제거 및 직렬화 프로퍼티(`set_property`) 값 설정 | `action: "set_property"`, `target`, `component_type`, `properties` |
| **`manage_scriptable_object`** | Unity SerializedObject 프로퍼티 경로 기반으로 ScriptableObject 에셋 생성 및 수치 패치 | `action`, `target`, `type_name`, `patches` |

---

### 2.2 Scene & GameObject 제어 (씬 및 게임오브젝트 생명주기)

| 도구명 (Tool Name) | 주요 기능 및 설명 | 핵심 파라미터 |
| :--- | :--- | :--- |
| **`manage_scene`** | 씬 생성, 로드(Additive 지원), 저장, 닫기, 활성 씬 전환, 계층구조(Hierarchy) 조회 및 검증 | `action`, `scene_path`, `template`, `auto_repair` |
| **`manage_gameobject`** | 게임오브젝트 생성(기본도형/프리팹), 이름변경, 복제, 삭제, 위치/회전/스케일 변환, 부모 지정 | `action`, `name`, `position`, `rotation`, `scale`, `parent` |
| **`find_gameobjects`** | 이름, 태그, 레이어, 컴포넌트 타입, 경로를 기반으로 씬 내 오브젝트 검색 (Instance ID 반환) | `search_term`, `search_method`, `include_inactive` |

---

### 2.3 Physics, Animation & VFX (물리, 애니메이션, 시각효과)

| 도구명 (Tool Name) | 주요 기능 및 설명 | 핵심 파라미터 |
| :--- | :--- | :--- |
| **`manage_physics`** | 2D/3D 물리 설정, 충돌 매트릭스(Collision Matrix) 제어, 물리 머티리얼, 조인트, 레이캐스트 질의 | `action`, `layer_a`, `layer_b`, `material_path`, `force` |
| **`manage_animation`** | Animator 파라미터 제어, AnimatorController 상태 머신/전이(Transition) 생성, 클립 키프레임 생성 | `action: "animator_* / controller_* / clip_*"`, `controller_path` |
| **`manage_vfx`** | Visual Effect Graph 에셋 생성, 속성 파라미터 바인딩 및 이벤트 트리거 | `action`, `target`, `properties` |

---

### 2.4 Rendering, Materials & Camera (렌더링, 머티리얼, 카메라, 캡처)

| 도구명 (Tool Name) | 주요 기능 및 설명 | 핵심 파라미터 |
| :--- | :--- | :--- |
| **`manage_camera`** | 일반 Camera 및 Cinemachine 설정 (타겟 추적, 렌즈, 우선순위), 게임뷰/씬뷰 스크린샷 캡처 | `action: "screenshot"`, `camera`, `capture_source`, `view_target` |
| **`manage_material`** | 머티리얼 생성, 셰이더 프로퍼티/컬러 지정, 렌더러에 머티리얼 할당 | `action`, `material_path`, `shader`, `color`, `properties` |
| **`manage_graphics`** | URP/HDRP 볼륨, 포스트 프로세싱 효과, 라이트맵 베이킹, 렌더링 통계(Draw calls), 스카이박스 설정 | `action`, `effect`, `ambient_mode`, `settings` |
| **`manage_shader`** | 셰이더 스크립트 생성, 읽기, 수정, 삭제 | `action`, `path`, `contents` |
| **`manage_texture`** | 단색, 체커보드, 줄무늬, 그라디언트, 노이즈 기반 절차적 텍스처 생성 및 스프라이트 변환 | `action`, `fill_color`, `pattern`, `as_sprite` |
| **`manage_ui`** | uGUI 캔버스, 패널, 버튼, 텍스트(TMP), 슬라이더 UI 요소 생성 및 배치 | `action`, `ui_type`, `parent`, `properties` |

---

### 2.5 Asset & Generative AI (에셋 관리 및 AI 에셋 생성)

| 도구명 (Tool Name) | 주요 기능 및 설명 | 핵심 파라미터 |
| :--- | :--- | :--- |
| **`manage_asset`** | 프로젝트 내 에셋 검색, 임포트, 이동, 이름변경, 삭제 | `action: "search"`, `filter_type`, `path`, `destination` |
| **`generate_image`** | AI(fal.ai, OpenRouter) 기반 2D 텍스처 및 스프라이트 생성 | `prompt`, `width`, `height`, `transparent`, `output_folder` |
| **`generate_model`** | AI(Tripo, Meshy) 기반 3D 모델(GLB, FBX, OBJ) 생성 | `prompt`, `format`, `target_size`, `output_folder` |
| **`generate_audio`** | AI(fal.ai Stable Audio) 기반 배경음악(BGM) 및 효과음(SFX) 생성 | `prompt`, `duration`, `model`, `output_folder` |
| **`import_model`** | Sketchfab 마켓플레이스 3D 모델 검색, 프리뷰 및 프로젝트 임포트 | `action: "search / import"`, `query`, `uid` |
| **`import_model_file`** | 로컬 디스크의 3D 파일(FBX, OBJ, GLB)을 임포트 파이프라인으로 반입 (리깅/애니메이션 지원) | `source_path`, `target_size`, `animation_type` |
| **`manage_probuilder`** | ProBuilder 기반 인-에디터 3D 지오메트리 모델링 (Extrude, Bevel, Subdivide, UV 매핑) | `action: "create_shape / extrude_faces"`, `properties` |

---

### 2.6 Editor Lifecycle, Testing & Profiling (에디터 제어, 테스트, 프로파일러)

| 도구명 (Tool Name) | 주요 기능 및 설명 | 핵심 파라미터 |
| :--- | :--- | :--- |
| **`read_console`** | 유니티 에디터 콘솔의 실시간 로그, 경고, 컴파일 에러 메시지 조회 | `log_types`, `count`, `clear` |
| **`manage_editor`** | 에디터 Play/Pause/Stop 제어, 태그/레이어 추가/삭제, Undo/Redo 실행 | `action: "play / pause / stop / undo"`, `tag_name`, `layer_name` |
| **`run_tests`** | 유니티 Test Runner 단위/통합 테스트 비동기 실행 | `test_mode: "EditMode / PlayMode"`, `categories` |
| **`get_test_job`** | 비동기 실행된 테스트 작업 결과(Pass/Fail, 에러 상세) 폴링 | `job_id`, `include_failed_tests` |
| **`manage_profiler`** | 프레임 디버거 이벤트 조회, 메모리 스냅샷 캡처 및 비교, 드로우콜/메모리 카운터 측정 | `action`, `category`, `snapshot_path` |
| **`manage_build`** | 타겟 플랫폼 전환(Windows/Mac/Android 등) 및 플레이어 바이너리 빌드 실행 | `action: "build / platform"`, `target`, `output_path` |
| **`manage_packages`** | UPM(Package Manager) 패키지 검색, 설치, 삭제, Scoped Registry 설정 | `action: "add_package / list_packages"`, `package` |
| **`refresh_unity`** | Unity AssetDatabase 강제 리프레시 및 스크립트 컴파일 트리거 | - |
| **`execute_menu_item`** | 유니티 상단 툴바 메뉴 아이템 명령 경로 직접 실행 | `menu_path` |
| **`batch_execute`** | 여러 MCP 명령을 1개 배치로 묶어 대기시간 및 토큰 소모를 10~100배 단축 실행 | `commands`, `parallel`, `fail_fast` |
| **`unity_docs`** | 유니티 공식 C# API 및 매뉴얼 오프라인 문서 검색 | `query`, `category` |
| **`unity_reflect`** | 런타임 C# 타입, 메서드, 필드, 프로퍼티 리플렉션 검사 | `type_name`, `member_type` |

---

### 2.7 Script & Execution Tools (※ 거버넌스 제약 준수 대상)
> [!WARNING]
> C# 소스코드 수정 및 터미널 실행은 프로젝트 헌장(`GEMINI.md`)에 따라 표준 네이티브 도구(`write_to_file`, `replace_file_content`, `run_command`)를 사용하는 것이 원칙이며, 아래 도구를 악용한 임의 우회 실행은 엄격히 금지됩니다.

| 도구명 (Tool Name) | 설명 |
| :--- | :--- |
| `create_script` / `delete_script` | 유니티 프로젝트 내 C# 스크립트 생성/삭제 (표준: `write_to_file`) |
| `manage_script` / `apply_text_edits` / `script_apply_edits` | C# 스크립트 텍스트/구조적 수정 라우터 (표준: `replace_file_content`) |
| `execute_code` | 유니티 에디터 내 C# 메모리 즉시 실행 (터미널 우회 절대 금지) |
| `find_in_file` / `get_sha` / `validate_script` | 파일 내 정규식 검색, SHA256 검사, 스크립트 문법 유효성 검사 |
| `manage_tools` / `set_active_instance` / `debug_request_context` | 세션별 도구 그룹 활성화 제어 및 MCP 세션 디버깅 |
