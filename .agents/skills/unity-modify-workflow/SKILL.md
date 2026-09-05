---
name: unity-modify-workflow
description: Developer 에이전트가 docs/tech_spec/ 변경사항을 분석하고 docs/ARCHITECTURE.md 및 docs/implementations/를 역색인하여 기존 C# 코드를 핀포인트 수정, 컴파일 검증 및 문서 최신화를 완결하는 표준 코드 수정/리팩토링 워크플로우 스킬입니다.
---

# Unity 기존 코드 수정 및 리팩토링 워크플로우

이 스킬은 Developer 에이전트가 기획 변경, 버그 수정, 리팩토링 등 기존 구현물의 변경 작업을 수신했을 때 무분별한 전체 탐색을 방지하고 문서 역색인을 통해 최소 변경으로 완결하는 5단계 표준 절차를 정의합니다.

---

## 1. 코드 수정 5단계 표준 워크플로우

### [1단계: tech_spec 변경사항 파악 및 브랜치 준비]
1. `docs/tech_spec/[시스템명]_tech_spec.md` 또는 기획 변경 요청 사항을 분석하여 어떤 요구사항이 변경/추가/수정되었는지 파악합니다.
2. `GitManager`에게 `develop` 최신 패치 및 수정 전용 작업 브랜치 생성을 요청합니다.

### [2단계: ARCHITECTURE 및 implementations 역색인 타겟 특정]
임의로 전체 코드를 뒤지지 않고 기존 문서를 역색인하여 수정 대상을 핀포인트로 특정합니다:
1. `docs/ARCHITECTURE.md`에서 변경 대상 시스템의 상호작용 매트릭스, 이벤트 흐름, 의존성 관계를 확인합니다.
2. `docs/implementations/[태스크명]_impl.md`에서 해당 기능의 클래스 구조, 직렬화 필드, 공개 API(`Public API`), 알고리즘 Rationale을 확인합니다.
3. 수정이 필요한 대상 C# 스크립트(`.cs`) 및 프리팹(`.prefab`) 파일 목록을 명확히 도출합니다.

### [3단계: 핀포인트 C# 코드 수정 및 컴파일 검증]
1. `unity-coding-rule` 및 `unity-work-rule`을 준수하여 도출된 타겟 파일만 핀포인트로 수정합니다 (사이드 이펙트 최소화).
2. 아래 명령을 실행하여 C# 컴파일 에러 0건을 자체 검증합니다:
   ```bash
   node .agents/skills/unity-cli-runner/scripts/unity_cli.js compile
   ```

### [4단계: implementations 기술문서 및 ARCHITECTURE 최신화]
수정된 내용에 맞추어 기존 기술 문서를 즉시 갱신합니다:
1. `docs/implementations/[태스크명]_impl.md`:
   - 변경된 직렬화 필드, 공개 API, 메서드 시그니처 갱신
   - 변경 사유 및 Rationale 기록
2. `docs/ARCHITECTURE.md`:
   - 상호작용 매트릭스, 이벤트 흐름표, 의존 관계의 변경분 반영

### [5단계: 상태 현황판 갱신 및 GitManager 직접 인계]
1. `docs/work/status.md`의 `[현재 상태]`를 `[Developer] [기능명] 수정 및 기술문서 갱신 완료 ➔ git_manager에게 커밋/PR 인계`로 갱신합니다.
2. 아래 소통 로거를 실행하고 턴을 종료합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "Developer" --to "GitManager" --type "PR 요청" --msg "[기능명] C# 코드 수정 및 기술문서/아키텍처 갱신 완료, 커밋/PR 요청"
   ```
