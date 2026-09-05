---
name: unity-design-workflow
description: Designer 에이전트가 docs/specs/ 원본 기획서를 분석하여 docs/tech_spec/ 기획 상세 명세서 작성, worklist.md 실무 태스크 등록, GitHub Issue 기획 제안 및 GitManager 문서 인계를 완결하는 표준 기획 워크플로우 스킬입니다.
---

# Unity 게임 기획 및 태스크 도출 워크플로우

이 스킬은 Designer 에이전트가 원본 기획서를 바탕으로 기술 상세 명세서를 작성하고, 4단계 아키텍처 우선 순서에 따라 실무 개발 태스크를 도출하며, 기획 보완 제안 및 문서 인계를 완결하는 표준 절차를 정의합니다.

---

## 1. 기획 분석 및 태스크 도출 4단계 표준 워크플로우

### [1단계: 기획 상세 명세서 작성 (`docs/tech_spec/[시스템명]_spec.md`)]
1. `docs/specs/` 내 원본 기획서와 `docs/PROJECT_SPEC.md`의 아키텍처 기준(2D/3D, ScriptableObject 패턴 등)을 정밀 분석합니다.
2. 기반 인프라 및 데이터 구조 확정 타이밍을 준수하여 필요한 **공용 인터페이스(`IDamageable`), 데이터 SO 스키마(`SO_*`), 코어 매니저 계층**을 명세서 작성 시점에 확정합니다.
3. `docs/tech_spec/[시스템명]_spec.md` 파일을 아래 표준 템플릿으로 작성합니다:
   ```markdown
   # [시스템명] 상세 기획 명세서 (Specification)

   ## 1. 시스템 개요 및 목적
   - (해당 기능의 핵심 플레이어 경험 및 코어 루프 내 역할)

   ## 2. 상세 메커니즘 및 게임 룰셋
   - (구체적 수치, 계산 공식, 상태 머신 FSM, 속도/딜레이 등 파라미터)

   ## 3. 데이터 구조 및 컴포넌트 설계 (Data-Driven SO)
   - (PROJECT_SPEC.md 기준에 맞춘 ScriptableObject 필드 구조 및 프리팹 조립 컴포넌트)

   ## 4. 예외 상황 및 엣지 케이스 (Edge Cases)
   - (화면 이탈, 동시 피격, 잔여 탄환 처리, 조작권 상실 등 예외 처리 수칙)

   ## 5. 시스템 간 상호작용 및 이벤트 흐름
   - (공유 인터페이스, 타 매니저/오브젝트와의 상호작용 및 이벤트 발행/구독 관계, 필요 시 mermaid 다이어그램 첨부)
   ```

### [2단계: 4단계 아키텍처 우선 실무 태스크 도출 및 `worklist.md` 등록]
1. 작성된 명세서를 바탕으로 **4단계 아키텍처 우선 순서(Architecture-First Order)**에 따라 태스크를 세분화하여 `docs/work/worklist.md`에 등록합니다:
   - **[1단계] 기반 인프라 및 데이터 계약**: 공유 인터페이스(`IDamageable`), 공용 매니저, 데이터 `SO` 정의
   - **[2단계] 핵심 수학/이동 유틸리티 및 베이스 클래스**: 궤적 계산 모듈, 추상 클래스(`EnemyBase`), 오브젝트 풀러
   - **[3단계] 액터 엔티티 및 Zero-Override 완제품 프리팹**: 플레이어 기체, 적 AI 기체, 2D 히트박스 충돌 연동
   - **[4단계] HUD/UI, 연출 및 코어루프 통합 검수**: 스코어보드 UI, 파티클 이펙트/사운드 연동, NUnit 통합 검수
2. **독립 완결성(Self-Contained Unit)**: 각 태스크는 Developer가 명세서를 참조하여 단일 1루프(Developer ➔ GitManager ➔ QA)로 완결 및 검증할 수 있는 단위여야 합니다.
3. 태스크 그룹 상단에 해당 상세 명세서 링크(`[기획 상세 명세서](docs/tech_spec/[시스템명]_spec.md)`)를 명시합니다.

### [3단계: GitHub Issue 추가 기획 제안 프로토콜]
기획서 상 부족하거나 모호한 부분 발견 시 임의 수정하지 않고 아래 절차로 이슈를 제안합니다:
1. **제안서 초안 작성 규격 (GitManager 위임)**:
   - **제안 제목**: `[AI_designer][제안] [기획 보완/추가 요약]`
   - **제안 본문 양식**:
     ```markdown
     ## 1. 기획 보완/추가 사유
     - (기획서 상 부족하거나 모호한 부분, 예외 상황 기술)

     ## 2. 제안하는 세부 기획 내용
     - (구체적인 규칙, 수치, 분기 로직, UI/UX 흐름 기술)
     - *(필요 시 mermaid 다이어그램 첨부)*

     ## 3. 예상되는 게임플레이 영향 및 고려사항
     - **예상 효과**: (플레이 경험, 코어루프 완성도 향상)
     - **고려사항**: (개발 난이도, 타 시스템과의 연계성)
     ```
2. **4단계 상태 전이 수칙**:
   - **`[제안]`**: Designer 초안 작성 ➔ GitManager 중복 검사 후 신규 이슈 등록 (`[AI_designer][제안] ...`)
   - **`[수락]`**: 사용자가 제안을 수락하면 `GitManager`가 제목을 `[AI_designer][수락] ...`으로 갱신 ➔ Designer가 승인된 기획 내용을 `docs/tech_spec/`에 반영하고 `worklist.md`에 세부 태스크로 등록
   - **`[완료]`**: Developer 개발 및 QA 검수 완료 후 PR 머지 시 `GitManager`가 `[AI_designer][완료] ...`로 변경 후 Issue Close
   - **`[반려]`**: 미채택 시 `GitManager`가 `[AI_designer][반려] ...`로 변경 후 Issue Close (재제안 시 타당성 보완 후 Reopen)

### [4단계: 상태 현황판 갱신 및 GitManager 문서 푸시 인계]
1. `docs/work/status.md`의 `[현재 상태]`를 갱신합니다:
   - 코어루프 충족 시: `[현재 상태] [Designer] 기획 분석 완료 및 코어루프 조건 달성 ➔ Developer 작업 진행 가능`
   - 코어루프 미달 시: `[현재 상태] [Designer] 코어루프 조건 미달성 (기획 보완 대기)`
2. 워킹 트리를 Clean하게 유지하기 위해 `GitManager`에게 Type 1 문서 커밋/푸시를 인계합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "Designer" --to "GitManager" --type "문서 커밋 요청" --msg "[기능명] tech_spec 및 worklist 갱신본 develop 직접 커밋/푸시 요청"
   ```
3. **사용자 통제 원칙**: Developer의 코딩은 에이전트가 임의 개시하지 않으며, 반드시 사용자의 명시적 작업 착수 명령 후에만 시작됩니다.
