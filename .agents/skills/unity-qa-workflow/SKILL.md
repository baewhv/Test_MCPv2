---
name: unity-qa-workflow
description: QA 에이전트가 PR 수신 시 작업 브랜치 변경 파일만 타겟팅하여 NUnit 단위/통합 테스트를 작성하고, 직접 커밋, 4대 필수 런타임 및 Deprecated/Zero-Override 검수, 무인 회귀 테스트 및 PR 승인/반려를 완결하는 PR 단위 타겟 부분 검수 워크플로우 스킬입니다.
---

# Unity PR 단위 타겟 부분 검수 및 NUnit 테스트 작성 워크플로우 (PR Scoped QA Inspection)


이 스킬은 QA 에이전트가 PR 수신 시 수행하는 2-Tier 하이브리드 검수(타겟 범위 검수 + 전체 무인 회귀 테스트), Zero-Override 프리팹 검증, Deprecated API 정적 검사, NUnit 테스트 코드 작성, 직접 커밋 및 PR 승인/반려 절차를 정의합니다.

---

## 0. QA 핵심 원칙 및 2-Tier 하이브리드 검수 전략 (Core Principles)

1. **2-Tier 하이브리드 검수 전략 (Targeted Scope & Full Regression)**:
   - **Tier 1 (타겟 신규 테스트 및 정적 검수)**:
     - 신규 NUnit 테스트 작성 및 4대 정적/씬 검수는 `git diff --name-only origin/develop`로 식별된 **이번 PR 변경/생성 파일 및 구현 기술문서([docs/implementations/](file:///C:/Users/KGA1/Desktop/TestMCP/docs/implementations)) 대상에만 엄격히 한정**합니다.
     - 이번 작업과 무관한 기존 파일이나 기존 테스트 코드를 불필요하게 열람(`view_file`)하거나 수정하지 않습니다.
   - **Tier 2 (전체 회귀 무인 자동 검증)**:
     - `unity-cli-runner`(`unity_cli.js test`) 1회 백그라운드 실행을 통해 **신규 테스트 통과 및 프로젝트 전체 기존 테스트 무결성(Regression 0건)을 단 1턴 만에 일괄 검증**합니다.
2. **비즈니스 로직 수정 절대 금지 & 즉시 반려 (Strict Fast-Fail Boundary)**:
   - QA 에이전트는 `Assets/Scripts/` 하위의 게임 비즈니스 로직 코드를 **단 한 줄도 직접 수정할 수 없습니다**.
   - 테스트 코드 작성 중 구현 누락, 컴파일 에러, 기능 결함, 단위 테스트 실패 발견 시 **절대로 직접 코드를 고치지 말고 즉시 `[3단계: QA 반려 (5-C)]`로 직행**하여 Developer에게 수정을 요청합니다.
3. **표준 네이티브 도구 의무화 & unityMCP 코드 I/O 전면 금지**:
   - 테스트 코드(`Assets/Tests/`) 작성/수정 및 `docs/` 문서 갱신은 반드시 표준 파일 도구(`write_to_file`, `replace_file_content`)를 사용합니다.
   - `unityMCP`의 `apply_text_edits`, `manage_script`, `create_script`, `get_sha`, `run_tests`, `get_test_job` 사용을 **전면 금지**합니다.
   - 무인 컴파일 검증 및 단위 테스트 실행은 오직 `unity-cli-runner`(`run_command`)를 통해서만 무인 백그라운드로 실행합니다.

---

## 1. QA 검수 4단계 표준 워크플로우

### [1단계: 변경 범위 파악 및 NUnit 테스트 코드 작성/커밋]
1. **작업 변경 파일 범위 확인 (Target Scoping)**:
   ```bash
   git diff --name-only origin/develop
   ```
   - 이번 PR에서 실제로 생성/수정된 파일 목록만 확인하여 검수 대상을 명확히 한정합니다.
2. **타겟 NUnit 테스트 작성 (표준 파일 도구 사용)**:
   - `docs/implementations/[태스크명]_impl.md`의 구현 명세 및 공개 API 계약에 대해서만 `write_to_file` 도구로 `Assets/Tests/Editor/[기능명]Tests.cs` (또는 `Runtime/`)를 작성합니다.
3. **전체 회귀 무인 테스트 실행 (CLI 러너 1회)**:
   ```bash
   node .agents/skills/unity-cli-runner/scripts/unity_cli.js test
   ```
   - 전체 테스트 100% Pass 여부 확인 (실패 시 원인 파악 후 즉시 Developer에게 반려 인계).
4. **테스트 코드 직접 커밋 및 원격 푸시**:
   ```bash
   git add Assets/Tests/
   git commit -m "[test] : [기능명] NUnit 단위/통합 테스트 코드 작성 및 검증 완료"
   git push origin HEAD
   ```

### [2단계: 4대 필수 런타임, Zero-Override 및 Deprecated 타겟 검수 (Inspection)]
*(이번 PR에서 변경/생성된 파일 목록을 대상으로만 검수 진행)*
1. **컴파일 무결성 & Deprecated API 0건 검증**:
   - Console Error 0건 확인
   - **Deprecated/Obsolete API (경고 CS0618) 검사**: 이번 변경 파일들 내에 구식 API(예: `FindObjectOfType`, 레거시 Input 등) 사용 발견 시 즉시 **`QA 반려 (5-C)`** 처리
2. **Zero-Override 프리팹 무결성 검증 (씬 오버라이드 0건)**:
   - 이번 작업 관련 프리팹(`Assets/Prefabs/PF_*`) 및 씬 파일(`*.unity`) 내 `PrefabInstance` 블록에서 `m_AddedComponents`, `m_RemovedComponents`, 프로퍼티 변경(`m_Modifications`) 존재 여부 검사
   - **오버라이드 1건이라도 발견 시 즉시 `QA 반려 (5-C)`** 처리하고 Developer에게 프리팹 원본 에셋 반영(`Apply All`) 요구
3. **Missing Reference 검증**: 이번 작업 대상 인스펙터 직렬화 누락 0건 확인
4. **시각적 렌더링 검증**: 필요 시 스크린샷 캡처 및 정상 렌더링 확인

### [3단계: 검수 결과 처리 (승인 vs 반려)]
- **검수 통과 시**:
  1. `replace_file_content`로 `docs/work/worklist.md`의 해당 태스크를 `- [x] [태스크명] (PR #nn)`로 완료 체크합니다.
- **검수 반려 시 (비즈니스 로직 결함 / 테스트 실패 / 오버라이드 / Deprecated API 발견)**:
  1. `replace_file_content`로 `docs/work/status.md`를 `[QA] [기능명] QA 검수 반려 (사유 기입) ➔ developer에게 수정 요청 인계`로 갱신합니다.
  2. 소통 로거를 통해 Developer에게 구체적인 결함/오버라이드/실패 테스트 내용과 개선 요청을 인계합니다:
     ```bash
     node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "QA" --to "Developer" --type "QA 반려/수정요청" --msg "[기능명] 결함 발견: [상세내용] 수정 요청"
     ```

### [4단계: PR 검수 승인(Approve) 및 GitManager 문서 동기화 인계]
1. 검수 통과 시 GitHub MCP `create_pull_request_review` (event: `APPROVE`, body: "QA 4대 검수 및 NUnit 테스트 100% 통과 승인")를 제출합니다.
   - **머지 권한 원칙 (No Auto-Merge)**: QA 에이전트는 절대로 PR을 직접 머지(`merge_pull_request`)하지 않습니다. PR 머지는 사용자가 GitHub에서 직접 검토 후 머지합니다.
2. `replace_file_content`로 `docs/work/status.md`를 갱신하고 `GitManager`에게 문서 동기화를 인계합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "QA" --to "GitManager" --type "문서 동기화 요청" --msg "[기능명] PR 승인(Approve) 완료, worklist/status 동기화 요청"
   ```



