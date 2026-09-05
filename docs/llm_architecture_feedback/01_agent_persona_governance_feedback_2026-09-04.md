# 에이전트 거버넌스 및 규칙 개선 피드백 보고서 (2026-09-04 세션)

이 문서는 2026년 9월 4일 작업 세션 중 발생한 에이전트 간 역할 중첩, 충돌 및 워크플로우 병목 현상을 분석하고, 이를 해결하기 위해 에이전트 규칙(`.agents/`)과 프로젝트 헌법(`GEMINI.md`)을 수정한 내역 및 개선 시사점을 총괄 정리한 아키텍처 피드백 보고서입니다.

---

## 1. 개요 및 배경

프로젝트 진행 과정에서 멀티에이전트 협업 체계의 성숙도를 높이기 위해 실제 기능 개발(`PlayAreaManager` 리팩토링 및 `IDamageable` 피격 파이프라인 디커플링)을 수행하면서 아래와 같은 5대 운영 이슈가 식별되었으며, 이에 대한 즉각적인 규칙 개정이 이루어졌습니다.

---

## 2. 5대 핵심 개선 이슈 및 해결 내역

### Issue 1. Developer와 QA 간의 중복 테스트 및 페르소나 모호성 해소
- **문제 상황**:
  - Developer가 C# 기능 구현 시 NUnit 단위 테스트(`*Tests.cs`)를 직접 작성하고 실행한 후, QA 에이전트가 이를 다시 NUnit으로 재검증하는 **테스트 단계의 중복 및 역할 중첩**이 발생함.
  - 화이트박스 구현자가 본인의 코드를 자체 검증하는 단위 테스트를 작성함으로써, 객관적인 블랙박스 검증의 취지가 약화됨.
- **해결 방안 및 규칙 개정**:
  - **Developer**: 오직 순수 C# 구현, Zero-Override 프리팹 조립, 사전 컴파일 검증(`unity_cli.js compile`), 개별 구현 기술문서 작성에만 집중하도록 역할을 엄격히 한정. **NUnit 테스트 코드 작성 및 NUnit 테스트 실행 일체 금지**.
  - **QA**: Developer의 기술문서와 아키텍처 관계도를 바탕으로 **블랙박스 NUnit 단위/통합 테스트 코드(`Assets/Tests/`)를 직접 작성 및 보강**하고, 4대 필수 검수를 독점 전담.
- **반영 파일**: `.agents/agents/developer.md`, `.agents/agents/qa.md`, `.agents/skills/unity-coding-rule/SKILL.md`

---

### Issue 2. 기술 문서화 체계의 이원화 (숲과 나무의 분리)
- **문제 상황**:
  - `docs/ARCHITECTURE.md` 문서 내에 거시적 관계도뿐만 아니라 각 컴포넌트의 상세 구현 설명(긴 줄글 형태)이 혼재되어 문서가 비대해지고, 컴포넌트 내부의 세부 구현 명세를 확인할 독립 문서가 부재함.
- **해결 방안 및 규칙 개정**:
  - **중앙 지형도 (`docs/ARCHITECTURE.md`)**: 상세한 줄글 설명을 걷어내고, 시스템 간 **참조 기반 순수 관계도(상호작용 매트릭스, 2D Layer/Tag 충돌 매트릭스, 이벤트 발행-구독 흐름, Data SO 바인딩, Mermaid 다이어그램)**만 깔끔하게 색인화.
  - **개별 상세 설계도 (`docs/implementations/`)**: 신규 폴더를 신설하고, Developer가 기능 구현 완료 시 `[태스크명]_impl.md` 문서를 작성하여 클래스 내부 구조, 프로퍼티, 메서드 시그니처, 직렬화 바인딩 규격, 핵심 알고리즘 및 Rationale을 기록하도록 의무화.
- **반영 파일**: `docs/ARCHITECTURE.md`, `GEMINI.md`, `.agents/agents/developer.md`

---

### Issue 3. GitManager의 R&R 침범 방지 및 단방향 파이프라인 정립
- **문제 상황**:
  - 단순 규칙 문서 수정 커밋(Type 1)과 구현 기술문서 작성을 하나의 작업으로 묶어 GitManager에게 지시하는 과정에서, 버전 관리 전담인 GitManager가 코드 구현 문서를 작성하는 **에이전트 역할과 책임(R&R) 침범 오류**가 발생함.
- **해결 방안 및 규칙 개정**:
  - **GitManager**: 브랜치 격리, 커밋, 푸시, PR 관리, Issue 관리 등 **순수 버전 관리만 독점 전담**하며, 소스 코드 및 기술문서의 직접 작성/창작은 일체 금지.
  - **명확한 단방향 파이프라인 확립**:
    $$\text{Developer (구현 + 구현기술문서 + 관계도 1줄 + 컴파일 0건)} \longrightarrow \text{GitManager (PR 생성)} \longrightarrow \text{QA (NUnit 테스트 작성 + 4대 검수)}$$
- **반영 파일**: `.agents/agents/git_manager.md`, `.agents/agents/developer.md`, `.agents/agents/pm.md`

---

### Issue 4. Unity MCP 도구 호출에 따른 에디터 프리징 방지
- **문제 상황**:
  - QA 에이전트가 테스트 코드를 생성하기 위해 `unityMCP create_script` 도구를 호출했으나, Unity 에디터의 스크립트 컴파일 및 도메인 리로드(Domain Reload) 과정에서 MCP 소켓 세션 타임아웃이 발생하여 에디터 응답이 일시적으로 멈추는 현상이 발생함.
- **해결 방안 및 규칙 개정**:
  - 테스트 코드 파일 생성 시 에디터 소켓을 점유하는 `create_script` 대신 표준 로컬 파일 생성 도구(`write_to_file`)를 사용.
  - 컴파일 및 NUnit 테스트 검증 시 에디터 프리징 위험이 없는 **백그라운드 CLI 러너(`unity-cli-runner`)를 우선 활용**하도록 안전 수칙 정립.
- **반영 파일**: `.agents/agents/qa.md`, `.agents/skills/unity-cli-runner/SKILL.md`

---

### Issue 5. 신규 브랜치 분기 전 `develop` 최신 패치(Fetch & Pull) 의무화
- **문제 상황**:
  - PR #9가 `develop`에 머지되었으나, 후속 작업인 PR #10이 PR #9 머지 이전의 구버전 `develop`에서 분기되어 있어 **동일 파일들에 대한 3-Way 병합 충돌(Conflict)**이 발생함.
- **해결 방안 및 규칙 개정**:
  - GitManager가 신규 작업 브랜치 또는 Worktree를 생성하기 전에 **반드시 먼저 `git checkout develop && git fetch origin develop && git pull origin develop`을 실행하여 로컬 develop을 100% 최신 패치/동기화한 뒤 분기**하도록 의무화.
- **반영 파일**: `.agents/agents/git_manager.md`, `.agents/agents/developer.md`

---

## 3. 에이전트 규칙 파일별 수정 내역 종합

| 파일 경로 | 주요 수정 내용 |
| :--- | :--- |
| [`.agents/agents/developer.md`](file:///C:/Users/KGA1/Desktop/Test_MCPv2/.agents/agents/developer.md) | - Unity C# 코딩 및 기술문서 작성만 전담 (테스트 코드 작성/실행 일체 금지)<br>- `docs/implementations/[태스크명]_impl.md` 작성 및 `ARCHITECTURE.md` 관계도 1줄 동기화 의무화<br>- 신규 기능 개발 시작 시 GitManager에게 develop 최신 패치 요청 명시 |
| [`.agents/agents/git_manager.md`](file:///C:/Users/KGA1/Desktop/Test_MCPv2/.agents/agents/git_manager.md) | - Git 관련 업무(버전 관리/PR/Issue)만 독점 전담 (코드/문서 직접 작성 금지)<br>- **신규 작업 브랜치/Worktree 생성 전 develop fetch & pull 필수 선행 규칙 추가** |
| [`.agents/agents/qa.md`](file:///C:/Users/KGA1/Desktop/Test_MCPv2/.agents/agents/qa.md) | - `docs/implementations/` 명세를 기반으로 **NUnit 단위/통합 테스트 코드 직접 작성 및 4대 필수 검수 전담**<br>- `create_script` 호출 지양 및 `write_to_file` + `unity-cli-runner` 안전 실행 수칙 반영 |
| [`docs/ARCHITECTURE.md`](file:///C:/Users/KGA1/Desktop/Test_MCPv2/docs/ARCHITECTURE.md) | - 줄글 설명 제거 후 순수 참조 기반 관계도(상호작용, 충돌, 이벤트, 바인딩, Mermaid 다이어그램)로 경량화 |
| [`GEMINI.md`](file:///C:/Users/KGA1/Desktop/Test_MCPv2/GEMINI.md) | - 작업 문서 목록에 `docs/implementations/` (Developer 개별 구현 기술문서 폴더) 추가 |

---

## 4. 향후 운영 제언 및 시사점

1. **단방향 파이프라인의 엄격한 준수**:
   - 에이전트 간 핑퐁이나 불필요한 중복 작업을 방지하기 위해 `Developer ➔ GitManager ➔ QA` 단방향 흐름을 상시 유지합니다.
2. **문서 계층화의 엄격한 유지**:
   - 기획은 `docs/tech_spec/`, 구현 상세는 `docs/implementations/`, 전체 연결 관계는 `docs/ARCHITECTURE.md`에 분리 기록하여 문서 간 역할 충돌을 방지합니다.
3. **선행 동기화(Pre-Branch Sync) 습관화**:
   - 모든 신규 작업 시작 시 항상 원격 `develop`을 패치/풀한 후 브랜치를 생성하여 GitHub 상의 머지 충돌을 사전에 원천 차단합니다.
