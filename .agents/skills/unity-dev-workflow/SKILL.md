---
name: unity-dev-workflow
description: Developer 에이전트가 docs/tech_spec/ 분석, 작업 브랜치 일치 검증(Safety Gate), 4단계 아키텍처 구현, CLI 컴파일 검증, 직접 커밋, docs/implementations/ 기술문서 작성 및 GitManager PR 인계를 완결하는 표준 개발 워크플로우 스킬입니다.
---

# Unity 클라이언트 개발 및 구현 기술문서 작성 워크플로우

이 스킬은 Developer 에이전트가 단일 개발 태스크를 수신했을 때 시작부터 인계까지 완결하는 5단계 표준 절차 및 기술 제안 프로토콜을 정의합니다.

---

## 1. 개발 5단계 표준 워크플로우

### [1단계: 사전 명세 분석 및 First-Tool-Call 브랜치 일치 검증 (Safety Gate)]
1. `docs/tech_spec/[시스템명]_tech_spec.md` 및 `docs/PROJECT_SPEC.md`의 아키텍처 기준을 확인합니다.
2. **First-Tool-Call 브랜치 검증 의무화 (절대 규칙)**:
   - Developer가 턴을 시작할 때 **가장 첫 번째 도구 호출(Tool Call #1)은 무조건 `run_command("git branch --show-current")`**여야 합니다.
   - *파일 수정 도구(`write_to_file`, `replace_file_content`, `unityMCP` 등)를 먼저 호출하는 행위는 엄격히 금지됩니다.*
3. **Safety Gate 판정**:
   - `git branch --show-current`의 터미널 출력이 `docs/work/status.md`의 `**작업 브랜치**`와 100% 일치하는지 대조합니다.
   - **불일치 또는 `develop` 브랜치인 경우 (Safety Trigger)**:
     - **어떠한 소스 코드나 에셋도 절대 수정하지 않습니다. (파일 쓰기 도구 호출 0건)**
     - 즉시 작업을 중단하고 PM에게 "현재 물리적 브랜치([출력값])가 status.md의 작업 브랜치([지정값])와 불일치합니다. 브랜치 전환을 요청합니다."라고 보고하고 대기합니다.

### [2단계: 4단계 아키텍처 우선(Architecture-First) 구현 & 더미 리소스 원칙]
1. **토큰 절약형 더미 리소스 우선 원칙 (Primitive First)**:
   - 기능 구현 시 AI 리소스 생성 대신 유니티 기본 도형(Primitive: Capsule, Cube, Sphere) 및 단색 머티리얼을 사용합니다.
   - 이펙트는 내장 `Particle System`(`PF_VFX_*`), 움직임은 `Animator Controller`(`AC_*`)를 바인딩합니다.
2. 아래 의존성 순서에 따라 C# 스크립트와 프리팹을 조립합니다:
   - **[1단계] 기반 인프라 & 데이터 계약**: 공유 인터페이스(`IDamageable`), Data SO 스키마, 코어 매니저
   - **[2단계] 수학/이동 유틸리티 & 베이스 클래스**: 궤적 계산 모듈, 추상 클래스(`EnemyBase`), 오브젝트 풀러
   - **[3단계] 액터 엔티티 & Zero-Override 완제품 프리팹**: 플레이어, 적 AI 기체, 2D 히트박스 바인딩
   - **[4단계] HUD/UI 및 연출**: 스코어보드, 파티클 이펙트/사운드 바인딩


### [3단계: 백그라운드 컴파일 검증 및 물리적 커밋 검증 (Proof-of-Commit)]
1. 코드 작성 후 아래 명령을 실행하여 컴파일 에러가 0건인지 자체 검증합니다:
   ```bash
   node .agents/skills/unity-cli-runner/scripts/unity_cli.js compile
   ```
2. 표준 터미널 Git 명령어로 작업 브랜치에서 **순수 작업물(`Assets/`)만** 직접 커밋하고 원격으로 즉시 푸시합니다 (`docs/` 문서는 커밋하지 않고 로컬에 보존):
   ```bash
   git add Assets/
   git commit -m "[feat] : [기능명] C# 구현 및 프리팹 조립 완료"
   git push origin HEAD
   ```
3. **물리적 커밋 및 푸시 증거 확인 (Proof-of-Commit)**:
   ```bash
   git log -1 --oneline
   ```
   - *커밋 해시와 메시지가 정상 출력되는지 확인한 후에만 인계 단계로 진행합니다.*

### [4단계: 구현 기술문서 작성 및 아키텍처 관계도 동기화 (로컬 디스크 작성)]
1. **개별 구현 기술문서 작성**: 네이티브 파일 도구로 `docs/implementations/[태스크명]_impl.md` 파일을 생성하고 기술 명세를 작성합니다.
2. **아키텍처 관계도 동기화**: `docs/ARCHITECTURE.md`에 관계도를 갱신합니다.
   - *참고: 작성된 `docs/` 문서는 PR 머지 후 PM이 `develop` 브랜치에 일괄 커밋/푸시합니다.*

### [5단계: 상태 현황판 갱신 및 GitManager PR 인계]
1. `docs/work/status.md`의 `**진행 상태**`를 `[Developer] [기능명] 구현 및 커밋 완료 ➔ git_manager에게 PR 발행 인계`로 갱신합니다.
2. 아래 소통 로거를 실행하고 턴을 종료합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "Developer" --to "GitManager" --type "PR 요청" --msg "[기능명] C# 구현 및 직접 커밋(Proof-of-Commit 확인 완료), Clean PR 발행 요청"
   ```

---

## 2. GitHub Issue 기술 제안 프로토콜 (개선/리팩토링 제안 시)
코드 개선점 발견 시 임의 수정하지 않고 `GitManager`에게 이슈 등록을 요청합니다 (`[AI_developer][제안] ...`).
