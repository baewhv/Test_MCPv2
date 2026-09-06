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
- **5단계 표준 협업 사이클**:
  1. `[Developer]` `feat/...` 브랜치에서 기능 구현 및 기술문서 작성 ➔ `git add Assets/` 작업물만 선별 커밋 ➔ `GitManager` 인계
  2. `[GitManager]` `feat/...` 브랜치 원격 푸시 ➔ 순수 작업물 Clean PR 발행 ➔ `QA` 인계
  3. `[QA]` `feat/...` 브랜치 4대 검수 & NUnit 테스트 커밋(`git add Assets/Tests/`) ➔ GitHub PR Approve 리뷰 등록 ➔ `PM` 보고
  4. `[PM & 사용자]` PM이 "QA 승인 완료, PR 머지 대기" 간결 알림 보고 ➔ **사용자가 GitHub UI에서 PR을 직접 수동 머지**
  5. `[PM 문서 최종 정리 (Post-Merge)]` 사용자가 PR 머지 후 다음 지시 시, PM이 `develop` 브랜치를 pull 최신화하고 `docs/` 문서를 일괄 커밋/푸시(`git add docs/`, `git commit -m "[docs]..."`)하여 1루프 최종 완결

---

## 2. 브랜치 보호 및 머지 통제 절대 규칙 (Branch Protection)
- **develop 직접 푸시 엄격 금지**: 개발 및 QA 진행 중에는 어떠한 에이전트도 `develop` 브랜치에 직접 소스코드를 푸시할 수 없습니다.
- **PR 상태 임의 조작 금지**: QA 및 GitManager는 PR을 직접 머지(`merge_pull_request`)하거나 임의로 닫는(`update_issue` / close) 우회 행위를 일체 수행할 수 없으며, 모든 PR의 머지는 오직 **사용자**가 수행합니다.

---

## 3. 읽기 전용 문서 위치 (Read-Only Specifications)

- 아래 경로의 문서는 사용자가 직접 작성한 원본 문서이므로, 모든 에이전트는 **수정 및 덮어쓰기가 절대 불가능하며 오직 읽기(Read-Only)**만 수행한다:

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/specs/` | 사용자가 등록한 게임 시스템/기능 기획서 원본 | **엄격한 읽기 전용 (Strict Read-Only)** |

---

## 4. 작업 문서 위치 (Working Documents)
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

## 5. 기타 문서 위치 (Miscellaneous)

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/llm_architecture_feedback/` | 에이전트 구조 및 협업에 대한 피드백 폴더 | 읽기 / 쓰기 가능 |
