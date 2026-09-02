# 워크플로우 멈춤/지연 현상 분석 및 개선 보고서 (Workflow Blocking Analysis)

이 문서는 2026-09-02 작업 진행 중 발생했던 워크플로우 멈춤(지연) 현상의 근본 원인을 분석하고, 이를 해결하기 위해 에이전트 시스템에 적용한 개선 사항을 정리한 내부 기술 보고서입니다. (로컬 보관 전용)

---

## 1. 발생 상황 개요 (Incident Overview)

* **발생 시점**: Designer의 기획서 분석 완료 후, `develop` 브랜치에 마크다운 문서([status.md](docs/work/status.md), [worklist.md](docs/work/worklist.md))를 동기화하는 단계
* **현상**: 터미널 명령(`git push origin develop`) 실행 직후 명령어가 백그라운드 태스크(Task-71)로 전환되었으며, 약 3분 이상 응답 없이 대기 상태(Hang)에 머무름
* **결과**: 사용자가 "작업 상황은 어떤가요?"라고 재질의할 때까지 후속 개발 작업(Developer 착수)으로 연결되지 못하고 지연 발생

---

## 2. 근본 원인 분석 (Root Cause Analysis)

### 2.1 Windows 터미널 환경의 Git CLI 원격 통신 I/O 블로킹
* **터미널 인증/핸드셰이크 락**: Windows 환경에서 `git push` 실행 시 Git Credential Manager의 인증 확인 또는 원격 저장소 네트워크 I/O 동기화 과정에서 터미널 세션이 프롬프트 대기 상태로 진입.
* **비동기 대기 전이**: AI 에이전트의 터미널 실행 도구(`run_command`)가 일정 시간 응답이 없자 백그라운드 비동기 태스크로 넘겨버렸고, 프로세스가 무한 대기하면서 전체 체인이 중단됨.

### 2.2 작업 유형별 분기 프로토콜 부재
* 당시 작업은 C# 코드나 프리팹 조립이 수반되는 "기능 개발"이 아니라 단순 기획 문서 갱신이었음.
* 단순 문서 변경(`Type 4: Lightweight Docs Commit`)은 신속하게 동기화 후 즉시 다음 에이전트로 넘겨야 함에도, 무거운 터미널 푸시 루틴에 묶여 지연이 발생함.

### 2.3 턴 내 원스톱 인계 연계 부재
* 푸시 명령어를 백그라운드로 넘긴 후, 동일 턴 내에서 즉시 Developer ➔ GitManager ➔ QA로 이어지는 연속 실행을 완수하지 않고 사용자 응답을 대기하는 턴 종료가 발생함.

---

## 3. 해결 방안 및 시스템 개선 사항 (Solutions Applied)

### 3.1 원격 저장소 통신 표준: GitHub MCP 1순위 전담 (MCP-First Remote Policy)
* **로컬 파일 제어**: `git worktree add`, `git add`, `git commit`, `git status` 등 로컬 파일 시스템 작업만 Git CLI로 수행.
* **원격 통신 제어**: 원격 브랜치 생성(`create_branch`), 파일 푸시(`push_files`), PR 발행(`create_pull_request`), 승인 코멘트(`add_issue_comment`)는 **GitHub MCP API를 1순위로 호출**하여 터미널 I/O 락 및 인증 프리징을 원천 차단.

### 3.2 Git Manager 5대 작업 유형별 분기 프로토콜 정립 ([git_manager.md](.agents/agents/git_manager.md))

| 작업 유형 (Trigger) | 실행 절차 | 후속 인계 대상 |
| :--- | :--- | :---: |
| **[Type 1] 신규 기능 착수** | `git worktree add`로 격리 워크트리 및 브랜치 생성 ➔ 경로 전달 | `Developer` |
| **[Type 2] 기능 개발 완료** | `.meta` 1:1 검증 ➔ 로컬 커밋 ➔ GitHub MCP 푸시 ➔ 신규 PR 생성 | `QA` (검수 요청) |
| **[Type 3] QA 반려 수정** | 워크트리 수정 코드 `[fix]` 커밋 ➔ GitHub MCP 추가 푸시 (기존 PR 자동 갱신) | `QA` (재검수 요청) |
| **[Type 4] 순수 문서 변경** | 메인 저장소 `develop` 직접 커밋 & GitHub MCP 푸시 ➔ 즉시 다음 작업 인계 | `Developer` / `System` |
| **[Type 5] PR 머지 후 정리** | Worktree 삭제(`git worktree remove`) ➔ 로컬/원격 브랜치 삭제 ➔ `develop` 최신화 | `System` (대기 상태) |

### 3.3 단일 턴 내 원스톱 완결 원칙 (Continuous Single Loop)
* 인계가 필요한 단계에서 대기하지 않고, `Developer ➔ GitManager ➔ QA`의 1개 작업 단위를 동일 턴 내에서 완주하여 작업 흐름의 단절을 방지.

---

## 4. 검증 결과 (Verification Results)

* 개선된 GitHub MCP 원격 푸시 및 5대 분기 프로토콜을 적용하여 **`Task 1-1: 프로젝트 기본 씬 및 카메라/플레이 영역 경계(Border) 세팅`** 작업을 실행:
  1. `Developer`: C# 구현, 씬 저장, 단위 테스트 3건 통과, 아키텍처 문서 갱신
  2. `GitManager`: `.meta` 무결성 검증, GitHub MCP `push_files` 및 `create_pull_request`로 **PR #1 즉시 생성** (터미널 지연 0초)
  3. `QA`: NUnit 통과, 콘솔 에러 0건, PlayMode 검증, 스크린샷 캡처, PR #1 승인 리뷰 작성 및 [worklist.md](docs/work/worklist.md) `[x]` 체크 완료
* **결과**: 일체의 멈춤이나 백그라운드 프리징 없이 1개 완전 루프가 성공적으로 완주됨.
