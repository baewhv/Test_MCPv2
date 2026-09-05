# 프로젝트 환경 명세서 (Project Environment Specification)

- **환경 설정 상태 (Setup Status)**: `[SETUP_COMPLETED]`

> [!NOTE]
> GitHub 토큰, Notion 토큰 등 민감한 인증 키(API Key/PAT)는 이곳에 입력하지 마시고, MCP 설정(`config/mcp_config.json`)을 통해 안전하게 관리됩니다.

---

## 1. 버전 관리 및 저장소 정보 (Git & GitHub)
- **GitHub Repository URL**: `https://github.com/baewhv/Test_MCPTest`
- **Default Integration Branch**: `develop`
- **Release Branch**: `main`
- **Worktree Parent Directory**: `../TestMCP_worktrees`

---

## 2. 외부 연동 명세 (Notion & External Services)
- **Notion Database Name**: `학습일지`
- **Notion Database ID**: `13cc49b1-3a07-814e-b7b5-cf14b64ca1ee`
- **Notion Page Title Format**: `[YYYY-MM-DD] 작업 기록`

---

## 3. Unity 프로젝트 및 에셋 환경 명세 (Unity Specification)
- **Unity Project Name**: `TestMCP`
- **Unity Editor Path**: `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe`
- **Target Platform**: `PC, Mac & Linux Standalone`
- **Asset Root**: `Assets/`
- **Raw Imports Root (Submodule Boundary)**: `Assets/_Imports/`
- **Default Screenshot Output**: `Assets/Screenshots`
- **Core Loop Test Scene**: `Assets/Scenes/SampleScene.unity`

---

## 4. 아키텍처 및 데이터 드리븐 인프라 기준 (Architecture & Data Baseline)
- **Physics Engine Mode**: `2D (Rigidbody2D / Collider2D)`
- **Data-Driven Architecture**: `ScriptableObject 기반 데이터 분리 (Assets/ScriptableObjects/Data/)`
- **Object Pooling Pattern**: `제네릭 풀링 시스템 (Generic Object Pool / IPoolable)`
- **Core State Machine**: `FSM 기반 상태 제어`
- **Input System**: `New Input System (com.unity.inputsystem)`

---
## 5. 필수 5대 도구 인프라 명세 (Essential Tools & MCPs)

| 도구 명칭 | 구분 | 주요 전담 역할 | 미연결 시 영향 (Blocker) |
| :--- | :--- | :--- | :--- |
| **GitHub MCP** | MCP Server | PR 생성, 커밋 푸시, 이슈/리뷰 코멘트 등록 | PR 생성 및 자동 머지 인계 불가 |
| **Unity MCP** | MCP Server | 에디터 플레이 제어, 콘솔 에러 읽기, 스크린샷 캡처 | QA 4대 검수 중 런타임/스크린샷 검증 불가 |
| **Unity CLI** | CLI Tool | 백그라운드 무인 컴파일 검증 및 NUnit 단위 테스트 | Developer/QA의 오프라인 사전 검증 불가 |
| **Notion MCP** | MCP Server | 일일 학습일지 자동 생성 및 접힌 토글 피드백 | 작업 종료 시 Notion 자동 일지 작성 불가 |
| **Rider MCP** | MCP Server | C# 네이밍 컨벤션 검사 및 IDE 진단 연동 | C# IDE 정적 분석 및 네이밍 실시간 검증 불가 |

