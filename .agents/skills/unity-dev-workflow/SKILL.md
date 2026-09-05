---
name: unity-dev-workflow
description: Developer 에이전트가 docs/tech_spec/ 분석, 작업 브랜치 일치 검증(Safety Gate), 4단계 아키텍처 구현, CLI 컴파일 검증, 직접 커밋, docs/implementations/ 기술문서 작성 및 GitManager PR 인계를 완결하는 표준 개발 워크플로우 스킬입니다.
---

# Unity 클라이언트 개발 및 구현 기술문서 작성 워크플로우

이 스킬은 Developer 에이전트가 단일 개발 태스크를 수신했을 때 시작부터 인계까지 완결하는 5단계 표준 절차 및 기술 제안 프로토콜을 정의합니다.

---

## 1. 개발 5단계 표준 워크플로우

### [1단계: 사전 명세 분석 및 작업 브랜치 일치 검증 (Safety Gate)]
1. `docs/tech_spec/[시스템명]_tech_spec.md` 및 `docs/PROJECT_SPEC.md`의 아키텍처 기준을 참조합니다.
2. **브랜치 일치 여부 자가 검증 (Safety Gate)**:
   - 터미널에서 현재 체크아웃된 브랜치를 확인합니다:
     ```bash
     git branch --show-current
     ```
   - `docs/work/status.md`에 명시된 `**작업 브랜치**`와 현재 브랜치가 100% 일치하는지 대조합니다.
   - **불일치 시 (Safety Trigger)**:
     - **어떠한 소스 코드나 에셋도 절대 수정하지 않습니다.**
     - 즉시 작업을 중단하고 PM에게 "현재 브랜치([현재])가 status.md의 작업 브랜치([지정])와 불일치합니다. 브랜치 전환을 요청합니다."라고 보고하고 대기합니다.

### [2단계: 4단계 아키텍처 우선(Architecture-First) 구현]
브랜치 일치가 검증되면 아래 의존성 순서에 따라 C# 스크립트와 프리팹을 조립합니다:
1. **[1단계] 기반 인프라 & 데이터 계약**: 공유 인터페이스(`IDamageable`), Data SO 스키마, 코어 매니저
2. **[2단계] 수학/이동 유틸리티 & 베이스 클래스**: 궤적 계산 모듈, 추상 클래스(`EnemyBase`), 오브젝트 풀러
3. **[3단계] 액터 엔티티 & Zero-Override 완제품 프리팹**: 플레이어, 적 AI 기체, 2D 히트박스 바인딩
4. **[4단계] HUD/UI 및 연출**: 스코어보드, 파티클 이펙트/사운드 바인딩

### [3단계: 백그라운드 사전 컴파일 검증 및 직접 커밋]
1. 코드 작성 후 아래 명령을 실행하여 컴파일 에러가 0건인지 자체 검증합니다:
   ```bash
   node .agents/skills/unity-cli-runner/scripts/unity_cli.js compile
   ```
2. 컴파일 검증 완료 후 작업 브랜치에서 본인의 작업물을 직접 커밋합니다:
   ```bash
   git add Assets/Scripts/ Assets/Prefabs/
   git commit -m "[feat] : [기능명] C# 구현 및 프리팹 조립 완료"
   ```

### [4단계: 구현 기술문서 작성 및 아키텍처 관계도 동기화]
1. **개별 구현 기술문서 작성**: `docs/implementations/[태스크명]_impl.md` 파일을 생성하고 기술 명세를 작성합니다.
2. **아키텍처 관계도 동기화**: `docs/ARCHITECTURE.md`에 관계도를 갱신합니다.

### [5단계: 상태 현황판 갱신 및 GitManager PR 인계]
1. `docs/work/status.md`의 `**진행 상태**`를 `[Developer] [기능명] 구현 및 커밋 완료 ➔ git_manager에게 PR 발행 인계`로 갱신합니다.
2. 아래 소통 로거를 실행하고 턴을 종료합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "Developer" --to "GitManager" --type "PR 요청" --msg "[기능명] C# 구현 및 직접 커밋 완료, PR 발행 요청"
   ```

---

## 2. GitHub Issue 기술 제안 프로토콜 (개선/리팩토링 제안 시)
코드 개선점 발견 시 임의 수정하지 않고 `GitManager`에게 이슈 등록을 요청합니다 (`[AI_developer][제안] ...`).
