---
name: unity-qa-workflow
description: QA 에이전트가 기술 명세서를 바탕으로 Assets/Tests/에 NUnit 단위/통합 테스트 코드를 직접 작성하고, 직접 커밋, 4대 필수 런타임 검수, 스크린샷 캡처 및 PR 승인을 완결하는 표준 검수 워크플로우 스킬입니다.
---

# Unity QA 4대 검수 및 NUnit 테스트 작성 워크플로우

이 스킬은 QA 에이전트가 PR 수신 시 수행하는 4대 필수 런타임 검수, NUnit 테스트 코드 작성, 직접 커밋 및 PR 승인 절차를 정의합니다.

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

### [2단계: 4대 필수 런타임 검수 (Runtime Inspection)]
1. **컴파일 무결성 검증**: Console Error 0건 확인
2. **Zero-Override 프리팹 검증**: 씬 내 오버라이드 0건 확인
3. **Missing Reference 검증**: 인스펙터 직렬화 누락 0건 확인
4. **시각적 렌더링 검증**: 스크린샷 캡처 및 정상 렌더링 확인

### [3단계: 검수 보고서 작성 및 worklist 체크 완료]
1. `docs/work/worklist.md`의 해당 태스크를 `- [x] [태스크명] (PR #nn)`로 완료 체크합니다.

### [4단계: PR 승인/머지 및 GitManager 문서 동기화 인계]
1. GitHub MCP `create_pull_request_review` (event: APPROVE) 또는 `merge_pull_request`를 실행합니다.
2. `docs/work/status.md`를 갱신하고 `GitManager`에게 문서 동기화를 인계합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "QA" --to "GitManager" --type "문서 동기화 요청" --msg "[기능명] PR 승인 및 검수 완료, worklist/status 동기화 요청"
   ```
