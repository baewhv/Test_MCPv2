---
name: unity-pm-orchestration
description: PM 에이전트가 사전 환경 검증, 사용자 작업 의도 라우팅, 브랜치명 지정, status.md FSM 상태 제어, N개 배치 루프 관리 및 1루프 최종 완료 보고를 완결하는 표준 오케스트레이션 스킬입니다.
---

# Unity 프로젝트 매니저 오케스트레이션 워크플로우

이 스킬은 PM(Project Manager) 에이전트가 사용자의 작업 명령을 수신했을 때 적정 전문 에이전트에게 위임하고, 작업 브랜치를 명시적으로 지정하여 직접 인계(Direct Handoff) 흐름을 감시하며, 최종 완료 보고를 총괄하는 표준 절차를 정의합니다.

---

## 1. 사전 환경 검증 및 MCP 안전 점검 (Pre-flight & Safety)

1. **시스템 환경 설정 플래그 확인 (0-Tool-Call)**:
   - `GEMINI.md`의 `프로젝트 환경 설정 상태`가 **`[SETUP_COMPLETED]`**라면 별도 파일 읽기 도구 호출 없이 0.1초 만에 즉시 실무 파이프라인으로 직행합니다.
   - 만약 `미완료` 상태라면 `docs/PROJECT_SPEC.md`를 확인하여 사용자에게 필수 정보 입력을 요청하고 작업을 대기합니다.
2. **MCP 미연결 차단 프로토콜 (`[MCP_NotConnected]`)**:
   - 서브에이전트가 작업을 수행하기 위해 필요한 필수 도구(GitHub MCP, Unity MCP 등)가 미연결된 경우 작업을 강행하지 않고 `docs/work/status.md`에 **`[MCP_NotConnected]`**를 명시하고 서브에이전트 호출을 즉시 차단합니다.

---

## 2. 사용자 작업 명령 라우팅 프로토콜 (Task Routing)

### [단일 작업 단위 루프의 정의 및 브랜치 지정 원칙]
1. **작업 브랜치 지정**:
   - PM은 작업을 할당할 때 태스크 성격에 맞는 브랜치명(`feat/[기능명]`, `fix/[버그명]`, `refactor/[대상]`)을 명확히 지정합니다.
   - `docs/work/status.md`의 `**작업 브랜치**` 필드에 해당 브랜치명을 기입합니다:
     ```markdown
     - **진행 상태**: [PM] [기능명] 작업 착수 ➔ git_manager에게 브랜치 분리 요청
     - **작업 브랜치**: `feat/[기능명]`
     ```
   - `GitManager`에게 지정된 브랜치명으로 분리 및 전환(`git-branch-setup`)을 지시합니다.

2. **1개 개발 사이클의 정의 (1사이클 = 브랜치 분리부터 QA 승인 후 PM 문서 동기화까지)**:
   - **실무 진행**: `[Step 1: GitManager] 브랜치 분리/발행 ➔ [Step 2: Developer] C# 구현 (Assets/ 커밋/푸시) ➔ [Step 3: GitManager] Clean PR 발행 ➔ [Step 4: QA] 4대 검수 & NUnit 커밋/푸시 & PR Approve`
   - **1사이클 최종 완결 (PM)**: `[Step 5] QA 승인 수신 즉시, PM이 develop 브랜치에 docs/ 작업 문서를 일괄 커밋/푸시(git-doc-sync)하여 1개 사이클을 공식 최종 완결`
   - **사용자 검토 및 머지**: `[Step 6] PM이 사용자에게 1사이클 완료 종합 보고 ➔ 사용자가 GitHub UI에서 PR을 확인 후 수동 머지`

3. **서브 에이전트 실물 도구 호출 파이프라인 (invoke_subagent Pipeline)**:
   - PM은 직접 코드를 작성하거나 브랜치를 분리/검수하지 않고, **반드시 `invoke_subagent` 도구를 실제로 호출**하여 독립된 서브 에이전트에게 실행을 위임합니다:
     - **[Step 1: GitManager 호출 (브랜치 분리/발행)]**:
       `invoke_subagent(TypeName="git_manager", Role="Git 형상 관리자", Prompt="feat/[기능명] 브랜치를 develop 기준으로 분리하고 원격에 즉시 publish한 뒤 로컬 전환을 검증해주세요.")`
     - **[Step 2: Developer 호출 (기능 구현 및 커밋)]**:
       `invoke_subagent(TypeName="developer", Role="Unity 클라이언트 개발자", Prompt="docs/tech_spec/[기능명]_tech_spec.md 명세를 바탕으로 feat/[기능명] 브랜치에서 C# 구현, CLI 컴파일 검증, Assets/ 커밋 및 원격 푸시, docs/implementations/ 기술문서를 작성해주세요.")`
     - **[Step 3: GitManager 호출 (Clean PR 발행)]**:
       `invoke_subagent(TypeName="git_manager", Role="Git 형상 관리자", Prompt="feat/[기능명] 브랜치에 대해 develop 대상 Clean PR을 발행하고 QA에게 인계해주세요.")`
     - **[Step 4: QA 호출 (검수 및 Approve)]**:
       `invoke_subagent(TypeName="qa", Role="소프트웨어 품질 보증(QA)", Prompt="PR #[번호]에 대해 변경 파일 타겟 NUnit 테스트 작성, 4대 검수 및 무인 회귀 테스트를 수행하고 PR Approve 리뷰를 제출해주세요.")`
     - **[Step 5: PM 문서 동기화 완결]**:
       QA 승인 보고를 받은 즉시 PM이 `git-doc-sync`를 실행하여 `develop` 브랜치에 `docs/` 문서를 커밋/푸시하고 1사이클을 완결합니다.
   - *주의: 단순히 터미널 소통 로깅(`log_comm.js`)만 찍고 PM이 개발 스킬을 직접 읽어 코딩/수정을 혼자 수행하는 1인 다역(Roleplay) 행위를 엄격히 금지합니다.*



### [명령어별 라우팅 규격]
1. **단일 작업 착수 ("작업 하나 진행해줘", "다음 작업 진행해줘")**:
   - `docs/work/status.md`의 진행 중인 작업을 확인 후, `docs/work/worklist.md`의 미완료(`- [ ]`) 태스크를 **1순위: `## 사용자 최우선 지시사항`, 2순위: `## 작업 체크리스트`** 순서로 탐색하여 최상위 1개 작업을 선택한 뒤 브랜치를 지정하고 GitManager/Developer에게 위임합니다.
2. **다중/배치 작업 착수 ("N개의 작업 진행해줘", 예: "3개의 작업 진행해줘")**:
   - `worklist.md`의 미완료 항목들을 우선순위에 따라 최상위부터 N개의 작업을 순차적으로 1개 루프씩 완수하며 연계 실행합니다.
3. **특정 작업군 일괄 지정 착수 ("[작업명 키워드] 작업들 진행해줘")**:
   - `worklist.md`에서 해당 키워드와 일치하는 **모든 미완료 작업 목록 전체**를 탐색합니다.
   - 사용자에게 일치하는 작업 리스트를 확인 질문하고, 승인을 받은 후 순차적으로 1개 루프씩 모두 완수합니다.
4. **GitHub Issue 점검 및 동기화 ("이슈 체크해줘", "이슈 확인해줘")**:
   - `github-issue-sync` 스킬을 호출하여 [반려] Close, [수락]➔[착수] worklist 등록, [완료] Close 및 [제안] 대기건수를 확인하여 보고합니다:
     ```bash
     node .agents/skills/github-issue-sync/scripts/sync_issues.js
     ```
5. **프로젝트 종합 전체 전수 검수 ("전체 검수해줘", "전수 검사해줘", "릴리즈 검수해줘")**:
   - `QA` 에이전트에게 `unity-qa-full-inspect` 스킬 실행을 위임하고 5대 전수 점검(정적 분석, Zero-Override 전수, NUnit 전체 통계, 삼각 정합성) 종합 보고서를 수신하여 사용자에게 보고합니다.
6. **온디맨드 삼각 정합성 감사 ("기획/코드/문서 검수해줘", "감사해줘")**:
   - `QA` 에이전트에게 `unity-spec-audit` 스킬 실행을 위임하고 감사 보고서를 수신합니다.
7. **일일 작업 종료 및 개발일지 작성 ("오늘 작업 마칠게", "개발일지 작성해줘", "퇴근")**:
   - **1단계**: `github-issue-sync` 스킬로 당일 이슈 상태를 최종 정리합니다.
   - **2단계**: `unity-devlog-workflow` 스킬을 호출하여 Notion 학습일지 페이지를 생성하고 AI 회고 피드백을 접힌 토글로 부착합니다.


---

## 3. PM 물리적 교차 검증 게이트 (Physical Double-Check Gate)

PM은 서브에이전트의 텍스트 완료 보고("브랜치 분리 완료", "커밋 완료")를 맹신하지 않고, **에이전트 인계 전후로 로컬 터미널 셸 명령을 직접 1회 실행하여 물리적 상태를 교차 검증(Double-Check)**합니다:

1. **브랜치 분리 교차 검증 (GitManager ➔ Developer 인계 전)**:
   - PM이 직접 실행: `run_command("git branch --show-current")`
   - *검증*: 출력된 브랜치가 `feat/[기능명]`이 맞는지 확인. `develop`에 머물러 있을 경우 Developer에게 작업을 넘기지 않고 GitManager에게 브랜치 재전환 지시.
2. **커밋 완료 교차 검증 (Developer ➔ GitManager 인계 전)**:
   - PM이 직접 실행: `run_command("git status --porcelain")`
   - *검증*: `Assets/` 폴더 내에 Unstaged/Untracked 소스 코드가 남아있지 않고 깨끗하게 커밋되었는지 확인.
3. **QA 승인 후 최종 상태 검증 (QA ➔ PM 문서 동기화 전)**:
   - PM이 직접 실행: `run_command("git log -1 --oneline")` 및 `run_command("git status")`
   - *검증*: QA 테스트 커밋이 정상적으로 찍히고 워킹 트리에 충돌/결함이 없는지 확인 후 `git-doc-sync` 실행.

---

## 4. 실시간 작업 상태판 (`docs/work/status.md`) FSM 규격

단계 전환 시 `status.md`를 아래 표준 텍스트 규격에 맞춰 갱신합니다:
```markdown
## [현재 상태]
- **진행 상태**: [현재 진행 에이전트 및 상태]
- **작업 브랜치**: `feat/[기능명]` (또는 `develop`)
```

---

## 5. 1개 개발 사이클 최종 완결 보고 양식

QA 에이전트의 검수 승인(Approve) 수신 즉시 PM이 `git-doc-sync`를 실행하여 문서 동기화를 완결한 후, 사용자에게 아래 양식으로 1사이클 공식 완결 보고서를 출력하고 PR 머지를 안내합니다:

```markdown
### [기능명] 1개 개발 사이클 최종 완결 보고 (문서 동기화 완료)

- **완결 태스크**: [태스크명] (PR #[번호])
- **QA 검수 결과**: 4대 런타임/정적 검수 및 NUnit 100% 통과 (APPROVE 완료)
- **develop 문서 동기화**: `docs/` 작업 문서 일괄 커밋 및 `origin/develop` 푸시 완료 (`git-doc-sync`)
- **동기화된 산출물 목록**:
  - `docs/work/worklist.md` (완료 체크 반영)
  - `docs/work/status.md` (완료 상태 전환)
  - `docs/implementations/[태스크명]_impl.md` (구현 기술문서)
  - `docs/logs/agent_comm_YYYY-MM-DD.md` (협업 로그)
- **안내**: GitHub에서 PR #[번호]를 확인하시고 머지(Merge)해 주십시오. (에이전트의 1사이클 작업은 100% 완료되었습니다.)
```



