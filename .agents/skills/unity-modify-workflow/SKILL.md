---
name: unity-modify-workflow
description: Developer 에이전트가 docs/tech_spec/ 변경사항을 분석하고 작업 브랜치 일치 검증(Safety Gate), docs/ARCHITECTURE.md 및 docs/implementations/를 역색인하여 기존 C# 코드를 핀포인트 수정, 컴파일 검증, 직접 커밋 및 문서 최신화를 완결하는 표준 코드 수정/리팩토링 워크플로우 스킬입니다.
---

# Unity 기존 코드 수정 및 리팩토링 워크플로우

이 스킬은 Developer 에이전트가 기획 변경, 버그 수정, 리팩토링 등 기존 구현물의 변경 작업을 수신했을 때 무분별한 전체 탐색을 방지하고 문서 역색인을 통해 최소 변경으로 완결하는 5단계 표준 절차를 정의합니다.

---

## 1. 코드 수정 5단계 표준 워크플로우

### [1단계: tech_spec 변경사항 파악 및 작업 브랜치 일치 검증 (Safety Gate)]
1. `docs/tech_spec/[시스템명]_tech_spec.md` 또는 기획 변경 요청 사항을 분석하여 변경 요구사항을 파악합니다.
2. **브랜치 일치 여부 자가 검증 (Safety Gate)**:
   - 터미널에서 현재 체크아웃된 브랜치를 확인합니다:
     ```bash
     git branch --show-current
     ```
   - `docs/work/status.md`에 명시된 `**작업 브랜치**`와 현재 브랜치가 100% 일치하는지 대조합니다.
   - **불일치 시 (Safety Trigger)**:
     - **어떠한 소스 코드도 절대 수정하지 않습니다.**
     - 즉시 작업을 중단하고 PM에게 "현재 브랜치([현재])가 status.md의 작업 브랜치([지정])와 불일치합니다. 브랜치 전환을 요청합니다."라고 보고하고 대기합니다.

### [2단계: ARCHITECTURE 및 implementations 역색인 타겟 특정]
1. `docs/ARCHITECTURE.md` 및 `docs/implementations/[태스크명]_impl.md`를 역색인하여 수정이 필요한 C# 스크립트(`.cs`) 및 프리팹을 핀포인트로 특정합니다.

### [3단계: 핀포인트 C# 코드 수정, 컴파일 검증 및 직접 커밋]
1. 타겟 파일만 핀포인트로 수정 후 컴파일 에러 0건을 검증합니다:
   ```bash
   node .agents/skills/unity-cli-runner/scripts/unity_cli.js compile
   ```
2. 작업 브랜치에서 본인의 수정 내역을 직접 커밋합니다:
   ```bash
   git add Assets/
   git commit -m "[refactor] : [기능명] 코드 수정 및 리팩토링 완료"
   ```

### [4단계: implementations 기술문서 및 ARCHITECTURE 최신화]
1. `docs/implementations/[태스크명]_impl.md` 및 `docs/ARCHITECTURE.md`의 변경사항을 갱신합니다.

### [5단계: 상태 현황판 갱신 및 GitManager PR 인계]
1. `docs/work/status.md`의 `**진행 상태**`를 `[Developer] [기능명] 수정 및 커밋 완료 ➔ git_manager에게 PR 발행 인계`로 갱신합니다.
2. 소통 로거를 실행하고 턴을 종료합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "Developer" --to "GitManager" --type "PR 요청" --msg "[기능명] C# 코드 수정 및 직접 커밋 완료, PR 발행 요청"
   ```
