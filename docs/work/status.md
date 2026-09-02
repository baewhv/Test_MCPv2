# 개발 및 기획 진행 상태 (Status)

## [현재 상태]
- [Developer] [Phase 3: Task 3-1 ~ 3-4 적 급강하 AI, 탄환 사격 및 충돌/격파 전투 시스템] C# 구현 및 프리팹/씬 조립 완료 ➔ git_manager에게 커밋/PR 인계

> **[현재 상태 표준 전이 규격 안내]**
> - **1. 기획 완료**: `[Designer] 기획 분석 완료 및 코어루프 조건 달성 ➔ Developer 작업 진행 가능` *(미달 시: `[Designer] 코어루프 조건 미달성 (기획 보완 대기)`)*
> - **2. 리소스 제작 완료 (병렬/선행)**: `[Artist] [기능명] 리소스 제작 및 세팅 완료 ➔ status.md 제안항목에 에셋 연결 기록`
> - **3. 개발 완료**: `[Developer] [기능명] C# 구현 및 프리팹/씬 조립 완료 ➔ git_manager에게 커밋/PR 인계`
> - **4. PR 생성 완료**: `[GitManager] [기능명] PR 생성 완료 (PR #nn) ➔ qa에게 검수 인계`
> - **5-A. QA 진행 중**: `[QA] [기능명] QA 4대 검수 진행 중 (NUnit, 콘솔, 코어루프, 스크린샷)`
> - **5-B. QA 통과**: `[QA] [기능명] QA 4대 검수 통과 및 worklist [x] 완료 ➔ 사용자 최종 Merge 대기`
> - **5-C. QA 반려**: `[QA] [기능명] QA 검수 반려 (결함 발견) ➔ developer에게 수정 요청 인계`
> - **6. 머지 정리 완료**: `[GitManager] PR 머지 확인 및 Worktree 정리 완료 ➔ 다음 작업 대기`
> - **※ 도구 차단/대기**: `[도구차단] [에이전트명] [기능명] 작업 중단 (필수 도구 [도구명] 미연결) ➔ 사용자 설정 대기`

## [기획 필요항목]
- **1. 2인 플레이 턴제(2UP) 모드 지원 여부**:
  - *기획서에는 2UP 스코어 영역이 명시되어 있으나, 1인 플레이 우선 구현 후 2인 턴제 모드를 확장할지 여부 결정 필요.*
  - *[추천]*: 1인 싱글 플레이어 코어루프 및 챌린징 스테이지를 완성한 후 2인 교대 모드는 선택적 확장 태스크로 처리.
- **2. 하이스코어 및 통계 영구 저장 방식**:
  - *[추천]*: Unity `PlayerPrefs` 또는 JSON 파일 기반 로컬 저장 방식으로 구현.
- **3. 사운드 및 스프라이트 리소스 확보 방식**:
  - *[추천]*: Artist 에이전트의 절차적/나노바나나 생성 리소스와 Unity 내장 기본 2D 스프라이트를 1차 활용하고, 사운드는 AudioSynth/Procedural 사운드 또는 에셋 번들 연동.

## [개발 요소 제안항목]
- [x] **Phase 3 구현 완료 (Task 3-1 ~ 3-4)**:
  - `EnemyDiveController`: 3차 베지어 단독/호위 다이브 궤적 및 화면 하단 통과 후 상단 루프 복귀 AI
  - `EnemyBullet` & `EnemyBulletPool`: 하향 탄속 16 units/sec 조준 탄환 풀링 시스템
  - `EnemyShooting`: 다이브 중간 구간($t=0.3\sim 0.6$) 조준 사격 알고리즘
  - `PlayerBullet` <-> `EnemyBase` <-> `PlayerHealth` 2D 히트박스 충돌 및 데미지 파이프라인
  - `ExplosionEffect` & `ExplosionManager`: 파티클 버스트 폭발 이펙트 풀링 및 격파 자동 연동
  - 완제품 프리팹 (`PF_EnemyBullet`, `PF_Explosion`, `PF_Enemy_*`, `PF_Player`, `PF_FormationGridManager`)
  - NUnit EditMode 단위 테스트 42건 100% 통과

- [ ] **Phase 4 대비 아키텍처 확장 제안**:
  - `ScoreManager`: 적 격파 시 대기/비행/보스호위별 차등 점수 가산 및 2만/7만점 익스텐드 지급
  - `StageDirector`: 40기 전멸 감지(Stage Clear) 및 다음 스테이지 전환 FSM
  - `ChallengingStageManager`: 4주기 보너스 라운드 비공격 비행 및 격파 집계
  - `DynamicRankManager`: 스테이지 진행도 및 생존 시간에 따른 탄속/다이브 빈도 가변 조정
