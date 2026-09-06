# 프로젝트 에이전트 협업 및 운영 규칙 (Project Rules)

> [!NOTE]
> 언어/커뮤니케이션, 보안/마스킹, 코드 품질 및 문서화(.md) 아티팩트 생성 규칙은 전역 규칙(`Global Rules`)을 따릅니다.

---

## 0. 프로젝트 환경 설정 상태 (Setup Status)
- **상태**: `[SETUP_COMPLETED]`
<!-- 새 프로젝트 템플릿 복제 시 "미완료"로 시작하며, docs/PROJECT_SPEC.md 설정 완료 후 "[SETUP_COMPLETED]"로 갱신됩니다. -->

---

## 1. 표준 5단계 개발 라이프사이클 및 Clean PR 원칙
- **Clean PR 원칙 (코드 순수성 유지)**: 작업 브랜치(`feat/...`)에는 오직 실제 게임 소스 코드, 프리팹, 테스트 코드(`Assets/`)만 커밋하여 PR을 생성하며, `docs/` 폴더의 문서는 작업 브랜치에 커밋하지 않고 로컬에 보존합니다.
- **5단계 표준 협업 사이클 (1사이클 = 브랜치 분리부터 QA 승인 후 PM 문서 동기화 완료까지)**:
  1. `[Developer]` `feat/...` 브랜치에서 기능 구현 및 기술문서 작성 ➔ `git add Assets/` 작업물만 선별 커밋 및 원격 푸시 ➔ `GitManager` 인계
  2. `[GitManager]` 순수 작업물 Clean PR 발행 ➔ `QA` 인계
  3. `[QA]` `feat/...` 브랜치 4대 검수 & NUnit 테스트 커밋/푸시(`git add Assets/Tests/`) ➔ GitHub PR Approve 리뷰 등록 ➔ `PM` 인계
  4. `[PM 문서 동기화 및 1사이클 최종 완결]` QA 승인 수신 즉시, PM이 `develop` 브랜치로 작업 문서를 일괄 커밋/푸시(`git-doc-sync`: `git add docs/`, `git commit -m "[docs]..."`)하여 **1개 사이클(루프)을 최종 공식 완결**
  5. `[PM 보고 & 사용자 PR 머지]` PM이 사용자에게 "1사이클 개발 완결 및 문서 동기화 완료" 종합 보고 ➔ **사용자가 GitHub UI에서 PR을 확인 후 직접 수동 머지**


---

## 2. 브랜치 보호 및 머지 통제 절대 규칙 (Branch Protection)
- **develop 직접 푸시 엄격 금지**: 개발 및 QA 진행 중에는 어떠한 에이전트도 `develop` 브랜치에 직접 소스코드를 푸시할 수 없습니다.
- **PR 상태 임의 조작 금지**: QA 및 GitManager는 PR을 직접 머지(`merge_pull_request`)하거나 임의로 닫는(`update_issue` / close) 우회 행위를 일체 수행할 수 없으며, 모든 PR의 머지는 오직 **사용자**가 수행합니다.

---

## 3. 에이전트 4대 행동 제어 및 안전 헌장 (Agent Safety & Anti-Loop Policy)
*이 규칙은 모든 서브 에이전트가 생성되는 즉시 시스템 프롬프트에 Always-On으로 강제 주입되는 최상위 절대 헌장입니다.*

1. **도구 우회 사용 전면 금지 (Strict Tool Discipline)**:
   - 파일 수정/생성은 오직 네이티브 파일 도구(`write_to_file`, `replace_file_content`)만, 터미널 실행은 표준 셸 도구(`run_command`)만 사용해야 합니다.
   - 권한이 없거나 도구가 결핍되었다고 해서 `unityMCP`의 `apply_text_edits`, `manage_script`, `get_sha`, `execute_code` 등을 악용하여 코드를 임의 수정하거나 터미널 프로세스를 우회 실행하는 행위를 엄격히 금지합니다.
2. **직무 영역 준수 및 권한 부재 시 즉시 반려 (Strict Role Boundaries & Fast-Fail)**:
   - 모든 서브 에이전트는 본인의 화이트리스트 직무 영역(`Core Scope`)만 수행합니다.
   - **Fast-Fail 절대 원칙**: 지시받은 작업을 수행할 정규 도구(`run_command`, `write_to_file` 등)가 없거나, 직무 범위를 벗어난 요청을 받았을 경우 **절대로 타 도구로 우회하거나 강행하지 말고, 그 즉시 작업을 전면 중단(0-Tool-Call Trigger)한 뒤 PM에게 반려 사유(필요 도구/적정 에이전트)를 보고**하십시오.
   - **QA 비즈니스 로직 수정 절대 금지**: QA는 `Assets/Scripts/` 코드를 단 한 줄도 직접 수정할 수 없으며, 결함 발견 시 즉시 `QA 반려 (5-C)` 처리하여 Developer에게 인계합니다.
3. **단일 작업 100-Step 초과 방지 회로 차단 (Loop Circuit Breaker)**:
   - 단일 태스크 턴에서 도구 호출/스텝 수가 100회를 초과할 경우, 무한 수정 루프를 즉시 중단(Circuit Break)하고 진행 상황, 장애 원인, 차단 사유를 명시하여 보고 후 추가 지시를 대기합니다.
4. **서브 에이전트 실물 도구 호출 위임 의무화 (Mandatory Subagent Invocation)**:
   - PM 및 메인 에이전트는 직접 코딩/브랜치 조작/검수를 1인 다역(Roleplay)으로 수행하지 않고, 반드시 `invoke_subagent` 도구를 실제로 호출하여 독립된 전문 에이전트에게 실행을 위임해야 합니다.

---

## 4. 읽기 전용 문서 위치 (Read-Only Specifications)

- 아래 경로의 문서는 사용자가 직접 작성한 원본 문서이므로, 모든 에이전트는 **수정 및 덮어쓰기가 절대 불가능하며 오직 읽기(Read-Only)**만 수행한다:

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/specs/` | 사용자가 등록한 게임 시스템/기능 기획서 원본 | **엄격한 읽기 전용 (Strict Read-Only)** |

---

## 5. 작업 문서 위치 (Working Documents)
- 아래 경로의 문서는 서브 에이전트가 개발/분석 과정에서 실시간으로 갱신하는 작업 파일입니다 (사용자 PR 머지 후 PM이 일괄 커밋):

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/PROJECT_SPEC.md` | 프로젝트 환경 사양 기입 문서 | 초기 설정을 위해 읽기/쓰기 가능 |
| `docs/FOLDER_STRUCTURE.md` | 유니티 표준 폴더 구조 및 에셋/프리팹 네이밍 색인 | 읽기 / 쓰기 가능 |
| `docs/ARCHITECTURE.md` | 프로젝트 아키텍처 지도 및 관계도 | 읽기 / 쓰기 가능 |
| `docs/logs/` | 에이전트 간 실시간 소통 기록 폴더 | 읽기 / 쓰기 가능 |
| `docs/work/worklist.md` | 서브 에이전트 작업 태스크 체크리스트 | 읽기 / 쓰기 가능 |
| `docs/work/status.md` | 서브 에이전트 현재 실시간 작업 상태판 | 읽기 / 쓰기 가능 |
| `docs/tech_spec/` | 서브 에이전트(Designer)가 작성한 기획 기술 명세서 폴더 | 읽기 / 쓰기 가능 |
| `docs/implementations/` | 서브 에이전트(Developer)가 작성한 개별 구현 기술문서 폴더 | 읽기 / 쓰기 가능 |

---

## 6. 기타 문서 위치 (Miscellaneous)

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/llm_architecture_feedback/` | 에이전트 구조 및 협업에 대한 피드백 폴더 | 읽기 / 쓰기 가능 |
