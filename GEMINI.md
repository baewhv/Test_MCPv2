# 프로젝트 에이전트 협업 및 운영 규칙 (Project Rules)

> [!NOTE]
> 언어/커뮤니케이션, 보안/마스킹, 코드 품질 및 문서화(.md) 아티팩트 생성 규칙은 전역 규칙(`Global Rules`)을 따릅니다.

---

## 0. 프로젝트 환경 설정 상태 (Setup Status)
- **상태**: `[SETUP_COMPLETED]`
<!-- 새 프로젝트 템플릿 복제 시 \"미완료\"로 시작하며, docs/PROJECT_SPEC.md 설정 완료 후 \"[SETUP_COMPLETED]\"로 갱신됩니다. -->

---

## 1. 사용자 작업 지시 및 직접 인계(Direct Handoff) 원칙
- 메인(Default) 에이전트는 사용자로부터 작업 실행 지시(\"기획서 분석해줘\", \"작업 하나 진행해줘\", \"N개 작업 진행해줘\", \"리팩토링해줘\", \"현재 상태\" 등)를 수신하면, 직접 코딩이나 검수를 수행하지 않고 **`invoke_subagent` 도구를 호출하여 `PM` 에이전트에게 지시를 위임**한다.
- **서브에이전트 간 직접 인계 및 비동기 병행 처리**:
  - 서브에이전트는 단계마다 PM을 거치는 동기식 병목을 없애기 위해, 작업 완료 시 다음 전담 에이전트(`Developer ➔ GitManager ➔ QA`)에게 일감을 **직접 위임(Direct Handoff)**하고 즉시 턴을 종료한다.
  - `PM`에게는 전체 협업 흐름을 추적할 수 있도록 행적 로그(`agent-communication-logger`)를 남기며, PM은 1루프 최종 완결 시 사용자 종합 보고를 총괄한다.


---

## 2. 읽기 전용 문서 위치 (Read-Only Specifications)
- 아래 경로의 문서는 사용자가 직접 작성한 원본 문서이므로, 모든 에이전트는 **수정 및 덮어쓰기가 절대 불가능하며 오직 읽기(Read-Only)**만 수행한다:

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/specs/` | 사용자가 등록한 게임 시스템/기능 기획서 원본 | **엄격한 읽기 전용 (Strict Read-Only)** |

---

## 3. 작업 문서 위치 (Working Documents)
- 아래 경로의 문서는 서브 에이전트가 개발/분석 과정에서 실시간으로 갱신하는 작업 파일입니다:

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/PROJECT_SPEC.md` | 프로젝트 환경 사양 기입 문서 | 초기 설정을 위해 읽기/쓰기 가능 |
| `docs/ARCHITECTURE.md` | 프로젝트 아키텍처 지도 및 관계도 | 읽기 / 쓰기 가능 |
| `docs/logs/` | 에이전트 간 실시간 소통 기록 폴더 | 읽기 / 쓰기 가능 |
| `docs/work/worklist.md` | 서브 에이전트 작업 태스크 체크리스트 | 읽기 / 쓰기 가능 |
| `docs/work/status.md` | 서브 에이전트 현재 실시간 작업 상태판 | 읽기 / 쓰기 가능 |
| `docs/tech_spec/` | 서브 에이전트(Designer)가 작성한 기획 기술 명세서 폴더 | 읽기 / 쓰기 가능 |
| `docs/implementations/` | 서브 에이전트(Developer)가 작성한 개별 구현 기술문서 폴더 | 읽기 / 쓰기 가능 |

---

## 4. 기타 문서 위치 (Miscellaneous)

| 경로 (Path) | 설명 (Description) | 에이전트 접근 권한 |
| :--- | :--- | :--- |
| `docs/llm_architecture_feedback/` | 에이전트 구조 및 협업에 대한 피드백 폴더 | 읽기 / 쓰기 가능 |
