---
name: unity-qa-workflow
description: QA 에이전트가 기술 명세서를 바탕으로 Assets/Tests/에 NUnit 단위/통합 테스트 코드를 직접 작성하고, 직접 커밋, 4대 필수 런타임 및 Deprecated API 무결성 검수, 스크린샷 캡처 및 PR 승인/반려를 완결하는 표준 검수 워크플로우 스킬입니다.
---

# Unity QA 4대 검수 및 NUnit 테스트 작성 워크플로우

이 스킬은 QA 에이전트가 PR 수신 시 수행하는 4대 필수 런타임 검수, Zero-Override 프리팹 검증, Deprecated API 정적 검사, NUnit 테스트 코드 작성, 직접 커밋 및 PR 승인/반려 절차를 정의합니다.

---

## 1. QA 검수 4단계 표준 워크플로우

### [1단계: NUnit 테스트 코드 작성, 무인 실행 및 직접 커밋]
1. **NUnit 테스트 작성**: `Assets/Tests/Editor/[기능명]Tests.cs` 또는 `Assets/Tests/Runtime/[기능명]PlayTests.cs`를 작성합니다.
2. **무인 테스트 실행**:
   ```bash
   node .agents/skills/unity-cli-runner/scripts/unity_cli.js test
   ```
3. **테스트 코드 직접 커밋**:
   ```bash
   git add Assets/Tests/
   git commit -m "[test] : [기능명] NUnit 단위/통합 테스트 코드 작성 및 검증 완료"
   ```

### [2단계: 4대 필수 런타임, Zero-Override 및 Deprecated 검수 (Inspection)]
1. **컴파일 무결성 & Deprecated API 0건 검증**:
   - Console Error 0건 확인
   - **Deprecated/Obsolete API (경고 CS0618) 발생 여부 전수 검사**: 구식 API(예: `FindObjectOfType`, 레거시 Input 등) 사용 발견 시 즉시 **`QA 반려 (5-C)`** 처리하고 Developer에게 최신 API 교체 요청
2. **Zero-Override 프리팹 무결성 검증 (씬 오버라이드 0건)**:
   - 씬 파일(`*.unity`) 내 `PrefabInstance` 블록에서 `m_AddedComponents`, `m_RemovedComponents`, 프로퍼티 변경(`m_Modifications`) 존재 여부 전수 검사
   - **오버라이드 1건이라도 발견 시 즉시 `QA 반려 (5-C)`** 처리하고 Developer에게 프리팹 원본 에셋 반영(`Apply All`) 요구
3. **Missing Reference 검증**: 인스펙터 직렬화 누락 0건 확인
4. **시각적 렌더링 검증**: 스크린샷 캡처 및 정상 렌더링 확인

### [3단계: 검수 결과 처리 (승인 vs 반려)]
- **검수 통과 시**:
  1. `docs/work/worklist.md`의 해당 태스크를 `- [x] [태스크명] (PR #nn)`로 완료 체크합니다.
- **검수 반려 시 (오버라이드 / Deprecated API / 결함 발견)**:
  1. `docs/work/status.md`를 `[QA] [기능명] QA 검수 반려 (Prefab Override / Deprecated API / 결함 발견) ➔ developer에게 수정 요청 인계`로 갱신합니다.
  2. 소통 로거를 통해 Developer에게 구체적인 결함/오버라이드 내용과 개선 요청을 인계합니다.

### [4단계: PR 검수 승인(Approve) 및 GitManager 문서 동기화 인계]
1. 검수 통과 시 GitHub MCP `create_pull_request_review` (event: `APPROVE`, body: "QA 4대 검수 및 NUnit 테스트 100% 통과 승인")를 제출합니다.
   - **머지 권한 원칙 (No Auto-Merge)**: QA 에이전트는 절대로 PR을 직접 머지(`merge_pull_request`)하지 않습니다. PR 머지는 사용자가 GitHub에서 직접 검토 후 머지합니다.
2. `docs/work/status.md`를 갱신하고 `GitManager`에게 문서 동기화를 인계합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "QA" --to "GitManager" --type "문서 동기화 요청" --msg "[기능명] PR 승인(Approve) 완료, worklist/status 동기화 요청"
   ```
