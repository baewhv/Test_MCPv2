# Unity 5대 전문 에이전트 자율 협업 프레임워크 (Unity Multi-Agent Framework)

> **Antigravity AI 기반 5대 정예 에이전트 분업 시스템**
> 기획 분석부터 AI 리소스 제작, C# 개발, 무인 QA 검증, Git 버전 관리 및 Notion 일지 기록까지 완전 자동화된 사이클을 제공하는 유니티 프로젝트 표준 템플릿입니다.

---

## 1. 5대 에이전트 협업 및 라이프사이클 흐름도 (Overall Architecture)

```mermaid
graph TD
    %% 사용자 및 투입 영역
    User["사용자 (User)"]
    Specs["docs/specs/ (기획서)"]
    
    %% 5대 전문 에이전트
    Designer["1. Designer (기획 분석 / 태스크 세분화)"]
    Artist["2. Artist (AI 리소스 제작 / 온디맨드)"]
    Developer["3. Developer (C# 코딩 / 프리팹 조립 / 관계도 색인)"]
    GitManager["4. GitManager (Worktree 격리 / 커밋 / PR 전담)"]
    QA["5. QA (4대 무결성 검수 / PR 승인)"]

    %% 핵심 관리 및 산출물
    Worklist["docs/work/worklist.md (체크리스트)"]
    Status["docs/work/status.md (실시간 상태 제어)"]
    Imports["Assets/_Imports/ (Submodule 원본 격리)"]
    Prefabs["Assets/Prefabs/ (독립 완제품 프리팹)"]
    ArchMap["docs/ARCHITECTURE.md (객체 상호작용 색인)"]
    CommLog["docs/logs/agent_comm_YYYY-MM-DD.md (소통 감사 로그)"]
    PR["GitHub Pull Request (develop 대상)"]
    Devlog["Notion 학습일지 DB (일일 회고)"]

    %% 흐름 연결
    User -->|"기획서 등록"| Specs
    Specs -->|"탐색 및 코어루프 검토"| Designer
    Designer -->|"태스크 세분화"| Worklist
    Designer -->|"상태 갱신"| Status

    User -->|"고품질 에셋 요청 시"| Artist
    Artist -->|"2D/오디오/3D/파티클 생성"| Imports
    Artist -->|"에셋 연결 제안 등록"| Status

    User -->|"'작업 하나 진행해줘'"| Developer
    Developer -->|"작업 확인"| Worklist
    Developer -->|"C# 구현 및 에셋 바인딩"| Prefabs
    Developer -->|"관계도 갱신"| ArchMap
    Developer -->|"CLI 컴파일 자가검증"| Developer

    Developer -->|"커밋 / PR 요청"| GitManager
    GitManager -->|"Worktree 격리 및 .meta 검증"| GitManager
    GitManager -->|"PR 생성"| PR
    GitManager -->|"상태 갱신"| Status

    PR -->|"검수 요청"| QA
    QA -->|"4대 검수 (NUnit/에러/코어루프/스크린샷)"| QA
    QA -->|"검수 통과 코멘트"| PR
    QA -->|"- [x] 태스크 (PR #nn) 체크"| Worklist
    QA -->|"상태 갱신"| Status

    QA -->|"검수 완료 보고"| User
    User -->|"최종 Merge"| PR
    PR -->|"머지 완료 감지 및 Worktree 정리"| GitManager

    User -->|"'오늘 작업 마칠게'"| Devlog
    
    %% 실시간 소통 로깅 (모든 전환 시 1줄 누적)
    Designer -.->|"log_comm.js"| CommLog
    Artist -.->|"log_comm.js"| CommLog
    Developer -.->|"log_comm.js"| CommLog
    GitManager -.->|"log_comm.js"| CommLog
    QA -.->|"log_comm.js"| CommLog
```

---

## 2. 1개 작업 단위 완결 시퀀스 (Single Task Loop Sequence)

1개의 작업(Task)은 `Developer ➔ GitManager ➔ QA`의 완전한 검수 및 승인 사이클을 마쳤을 때 1회 완결됩니다:

```mermaid
sequenceDiagram
    autonumber
    actor User as 사용자
    participant Dev as Developer
    participant GM as GitManager
    participant QA as QA
    participant Git as GitHub (develop)
    participant Log as agent_comm_YYYY-MM-DD.md

    User->>Dev: "작업 하나 진행해줘"
    Note over Dev: worklist.md 최상위 미완료 태스크 선택
    Dev->>Dev: C# 코드 작성 & 프리미티브/에셋 결합 프리팹 조립
    Dev->>Dev: ARCHITECTURE.md 객체 관계도 갱신
    Dev->>Dev: unity-cli-runner 백그라운드 사전 컴파일 검증
    Dev->>Log: log_comm.js ("Developer -> GitManager: PR 요청")
    Dev->>GM: C# 및 프리팹 조립 완료, PR 요청

    GM->>GM: Worktree 격리 생성 & .meta 파일 무결성 검증
    GM->>Git: 작업 브랜치 푸시 및 develop 대상 PR 생성
    GM->>Log: log_comm.js ("GitManager -> QA: PR #nn 검수 요청")
    GM->>QA: PR #nn 생성 완료, 검수 인계

    Note over QA: QA 4대 필수 검수 실행
    QA->>QA: 1. NUnit 단위 테스트 (Dual Mode)
    QA->>QA: 2. 콘솔 에러 0건 & Search API / 컨벤션 검증
    QA->>QA: 3. 코어 루프 런타임 구동 검증
    QA->>QA: 4. 기능 추가 증빙 스크린샷 캡처
    
    QA->>Git: PR에 4대 검수 통과 승인 코멘트 등록
    QA->>QA: worklist.md에 "- [x] [태스크명] (PR #nn)" 체크
    QA->>Log: log_comm.js ("QA -> GitManager: QA 승인 완료")
    QA->>User: QA 4대 검수 통과 보고 (사용자 머지 대기)

    User->>Git: PR 최종 Merge
    Git->>GM: 머지 완료 확인
    GM->>GM: Worktree 및 로컬 작업 브랜치 삭제 정리
```

---

## 3. 5대 전문 에이전트 R&R 매트릭스

| 에이전트 | 단일 전담 역할 (Single Responsibility) | 주요 도구 및 전담 규칙 | 핵심 산출물 |
| :--- | :--- | :--- | :--- |
| **`Designer`** | 기획서 정밀 분석, 코어루프 검토, 태스크 세분화 | `docs/specs/`, `GEMINI.md` | `worklist.md`, `status.md` |
| **`Artist`** | AI 2D/3D/오디오 리소스 생성, Particle System, Animator Controller | 나노바나나, UnityMCP, `asset_generation_rule.md` | `Assets/_Imports/`, `status.md` 에셋 제안 |
| **`Developer`** | C# 코딩, 프리미티브 더미 조립, Search API 금지/보류, CLI 사전검수 | `csharp_coding_rule.md`, `unity-cli-runner` | `PF_*.prefab`, `docs/ARCHITECTURE.md` |
| **`QA`** | NUnit 테스트, 콘솔/Search API 검증, 코어루프 검증, 스크린샷 캡처 | UnityMCP, `unity-cli-runner` | PR 승인 코멘트, `worklist.md [x] (PR #nn)` |
| **`GitManager`** | Git Worktree 격리, .meta 검증, 커밋/푸시, PR 생성 및 머지 정리 | `git_rule.md`, GitHub MCP | GitHub PR, 클린 저장소 |

---

## 4. 이원화 관리 체계 (Dual-Tracking System)

```mermaid
graph LR
    subgraph StatusTracking["1. 실시간 상태 제어판 (docs/work/status.md)"]
        S1["최신 1줄 상태 실시간 덮어쓰기"]
        S2["AI 에이전트 간 FSM 진행 가능 여부 판단"]
        S3["기획 필요항목 & 개발 요소 제안항목(- [ ] 양식) 관리"]
    end

    subgraph AuditLogging["2. 실시간 소통 감사 로그 (docs/logs/agent_comm_*.md)"]
        L1["에이전트 간 인계 시마다 1줄씩 타임라인 누적"]
        L2["발신자 / 수신자 / 소통유형 / 핵심메시지 기록"]
        L3["사용자의 실시간 협업 과정 감사(Audit) 및 검증"]
    end
```

---

## 5. 사용자 작업 실행 명령어 빠른 참조 (Command Reference)

| 명령어 구분 | 사용자 입력 예시 | 에이전트 동작 및 처리 결과 |
| :--- | :--- | :--- |
| **단일 작업** | *"작업 하나 진행해줘"*, *"다음 작업 진행해줘"* | `worklist.md`의 미완료 최상위 1개 태스크 루프 완결 |
| **배치 작업** | *"3개의 작업 진행해줘"*, *"N개의 작업 진행해줘"* | 최상위부터 N개 태스크를 순차적으로 1개 루프씩 연계 완수 |
| **일괄 지정** | *"[키워드] 작업들 진행해줘"* | 일치하는 태스크 목록 확인 질문 ➔ 승인 후 전체 완결 |
| **상태 질의** | *"현재 작업상태는?"*, *"어디까지 됨?"* | 진행 중 / 반려 대기 / 착수 가능 3대 분기 보고 |
| **기획 수정** | *"기획서 [기능명] 수정했으니 반영해줘"* | `Designer` 재분석 ➔ `worklist.md` [수정] 태스크 등록 ➔ 개발 1루프 수행 |
| **안전 리팩토링** | *"[기능명] 코드 직관적으로 리팩토링해줘"* | `Developer` 구조 해설 ➔ `refactor` 격리 브랜치 개발 ➔ `QA` 회귀 검수 |
| **작업 종료** | *"오늘 작업 마칠게"*, *"개발일지 작성해줘"* | Notion `학습일지` DB에 일지 자동 생성 ➔ 접힌 토글 AI 피드백 부착 |

---

## 6. 프로젝트 디렉토리 구조 (Directory Layout)

```
TestMCP/
├── .agents/
│   ├── agents/             # 5대 전문 에이전트 지침서 (designer, artist, developer, qa, git_manager)
│   ├── rules/              # 4대 표준 규칙 (csharp, git, unity_folder, asset_generation)
│   └── skills/             # 3대 전용 스킬 (unity-cli-runner, logger, unity-devlog-workflow)
├── Assets/
│   ├── _Imports/           # [Submodule 대상] 외부 원본 리소스 (Audio, Fonts, Models, Textures)
│   ├── Animations/         # 애니메이션 클립(.anim), 컨트롤러(.controller)
│   ├── Materials/          # 머티리얼(.mat)
│   ├── Prefabs/            # 독립 완제품 프리팹 (PF_*.prefab)
│   ├── Scenes/             # 씬 파일 (*Scene.unity, StageX-Y.unity)
│   ├── ScriptableObjects/  # 데이터 SO 에셋 (SO_*.asset)
│   ├── Scripts/            # C# 소스 코드
├── docs/
│   ├── screenshots/        # QA 검수 증빙 캡처 이미지
│   ├── specs/              # 기획서 투입 디렉토리 (Drop-in Directory)
│   ├── work/               # 실시간 상태판 (status.md) 및 태스크 체크리스트 (worklist.md)
│   ├── logs/               # 일일 에이전트 실시간 소통 감사 로그 (agent_comm_*.md)
│   ├── PROJECT_SPEC.md     # GitHub / Notion / Unity 환경 설정 명세서
│   ├── ARCHITECTURE.md     # 객체 상호작용, 스포너, 이벤트 흐름 총괄 관계도
│   └── INDEX.md            # 프로젝트 마스터 색인
├── GEMINI.md               # 전역 에이전트 공통 규칙 및 인텐트 라우팅
└── README.md               # 프레임워크 메인 안내 및 아키텍처 다이어그램
```

---

## 7. 빠른 시작 가이드 (Quick Start Guide)

1. **환경 설정**:
   - [`docs/PROJECT_SPEC.md`](file:///C:/Users/KGA1/Desktop/TestMCP/docs/PROJECT_SPEC.md)를 열고 본인의 GitHub 저장소 정보, Notion Database ID, Unity 설치 경로를 입력합니다.
2. **기획서 등록**:
   - [`docs/specs/`](file:///C:/Users/KGA1/Desktop/TestMCP/docs/specs/) 폴더에 기획서(예: `01_player_movement.md`)를 작성하여 넣고 *"기획서 분석해줘"*를 호출합니다.
3. **개발 가동**:
   - `Designer`가 기획 분석을 완료하면 *"작업 하나 진행해줘"* 또는 *"3개의 작업 진행해줘"*를 입력하여 자율 개발 사이클을 가동합니다.
4. **일일 종료**:
   - 개발을 마치고 퇴근할 때 *"오늘 작업 마칠게"*라고 말하면 Notion 학습일지가 자동으로 생성됩니다.
