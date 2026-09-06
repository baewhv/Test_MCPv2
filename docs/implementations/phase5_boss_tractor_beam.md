# [Implementation 09] 보스 갤러그 트랙터 빔 발사 및 포획 영역 콜라이더 전개 구현 기술문서 (Task 5-1)

## 1. 개요 및 목적
- **작업 브랜치**: `feat_phase5_boss_tractor_beam`
- **목적**:
  1. 원작 Galaga의 핵심 기믹인 보스 갤러그(Boss Galaga)의 트랙터 빔(Tractor Beam) 전개 메커니즘을 Unity 2D 환경에 구현합니다.
  2. 기획 상세 명세서(`docs/tech_spec/03_boss_tractor_beam_dual_fighter_tech_spec.md`)의 콜라이더 규격(상단 너비 0.556u, 하단 너비 3.333u, 높이 8.333u)을 충족하는 사다리꼴 `PolygonCollider2D`를 생성하고 절차적 메시 및 색상 순환 애니메이션을 연결합니다.
  3. 보스 전용 다이브 AI 궤적을 구현하여, 화면 상중단($Y=1.0\text{u}$) 호버링 정지 ➔ 트랙터 빔 전개(4.0초) ➔ 빔 회수 후 하단 급강하 복귀의 완전한 사이클을 구축합니다.
  4. 독립 완제품 프리팹 `PF_BossTractorBeam.prefab` 및 조립 완제품 `PF_Enemy_Boss.prefab`을 구축하여 Zero-Override 무결성을 확보합니다.

---

## 2. 변경 파일 및 상세 구현 내용

### 2.1 `BossTractorBeam.cs` (`Assets/Scripts/Gameplay/Enemy/BossTractorBeam.cs`)
- **역할**: 트랙터 빔의 사다리꼴 콜라이더 생성/갱신, 절차적 2D 메시 생성, 실시간 색상 순환 애니메이션, 4.0초 지속 타이머 및 플레이어 포획 감지를 담당.
- **사다리꼴 규격 및 로컬 좌표**:
  - 상단 너비: $0.556\text{ units}$ ($8\text{ px} / 14.4$)
  - 하단 너비: $3.333\text{ units}$ ($48\text{ px} / 14.4$)
  - 높이: $8.333\text{ units}$ ($120\text{ px} / 14.4$)
  - 꼭짓점 4개:
    - Point 0 (Top-Left): `(-0.278f, 0.0f)`
    - Point 1 (Top-Right): `(0.278f, 0.0f)`
    - Point 2 (Bottom-Right): `(1.667f, -8.333f)`
    - Point 3 (Bottom-Left): `(-1.667f, -8.333f)`
- **시각 연출 및 애니메이션**:
  - `UpdateProceduralMesh()`를 통해 런타임에 4꼭짓점 Quad 메시 생성 및 `MeshFilter` 자동 바인딩.
  - Cyan, Blue, White, Sky Blue 4색 팔레트를 $15\text{Hz}$ 주기로 순환하는 `MaterialPropertyBlock` 기반 애니메이션 구현.
- **수명주기 및 이벤트**:
  - `public void ActivateBeam(float duration = -1f)`: 빔 콜라이더 및 렌더러 활성화, 타이머 코루틴 시작, `OnBeamActivated` 발행.
  - `public void DeactivateBeam()`: 빔 콜라이더/렌더러 비활성화, 코루틴 정지, `OnBeamDeactivated` 발행.
  - `OnBeamTimeout`: 4.0초 경과 시 타임아웃 이벤트 발행.
  - `OnTargetCaptured`: 플레이어 기체 진입 감지 시 `Collider2D`와 함께 포획 이벤트 발행.
  - `OnDisable()`: 빔 비활성화, 소유주 보스 파괴 이벤트 구독 해제, 모든 델리게이트 메모리 누수 방지 초기화.

### 2.2 `EnemyBoss.cs` (`Assets/Scripts/Gameplay/Enemy/EnemyBoss.cs`)
- **역할**: 보스 갤러그 전용 특수 상태 머신, 트랙터 빔 제어 및 상단 포획기 결속 슬롯(`CapturedFighterSlot`) 관리.
- **직렬화 필드**:
  - `_tractorBeam`: 하단 트랙터 빔 컴포넌트 참조.
  - `_capturedFighterSlot`: 포획된 기체가 결속되는 상단 로컬 슬롯 (`Vector3(0, 0.8f, 0)`).
  - `_hasCapturedFighter`: 결속 여부 상태 플래그.
  - `_capturedFighter`: 결속된 포획기 `EnemyBase` 참조.
  - `_hoverTargetY` (기본 1.0u): 트랙터 빔 발사를 위한 호버링 고도.
  - `_beamDuration` (기본 4.0초): 빔 지속 시간.
- **주요 기능**:
  - `StartTractorBeam()`: `EnemyState.TractorBeam` 상태로 전이 후 트랙터 빔 전개.
  - `StopTractorBeam()`: 트랙터 빔 회수.
  - `AttachCapturedFighter(EnemyBase fighter)`: 포획기를 상단 슬롯의 자식으로 계층 이동 및 위치 고정.
  - `DetachCapturedFighter()`: 포획기 결속 해제 및 반환.

### 2.3 `EnemyType.cs` (`Assets/Scripts/Gameplay/Enemy/EnemyType.cs`)
- `EnemyState` 열거형에 `TractorBeam` (트랙터 빔 호버링 및 발사 중) 상태 추가.

### 2.4 `EnemyDiveController.cs` (`Assets/Scripts/Gameplay/Enemy/EnemyDiveController.cs`)
- **트랙터 빔 다이브 파이프라인 확장**:
  - `LaunchBossTractorBeamDive(EnemyBase boss, float hoverY = 1.0f, float beamDuration = 4.0f)`:
    1. 보스가 편대에서 이탈하여 플레이어 X좌표 및 호버링 고도($Y=1.0\text{u}$)로 곡선 진입 (`CreateBossTractorHoverTrajectory`).
    2. 고도 도달 시 `EnemyState.TractorBeam` 전이 및 하향 회전 정렬.
    3. `EnemyBoss` 또는 `BossTractorBeam`을 통해 4.0초 빔 발사.
    4. 빔 종료 또는 타임아웃 시 하단 급강하 궤적(`CreateBossPostBeamDiveTrajectory`)으로 전환.
    5. 화면 하단($Y=-11.0\text{u}$) 도달 시 상단($Y=+11.0\text{u}$) 재진입 후 원래 편대 슬롯으로 복귀 (`LaunchReturnToFormation`).
  - `TriggerRandomDive()` 다이브 선택 알고리즘 업데이트:
    - 보스 기체 선택 시 결속된 포획기가 없으면 35% 확률로 트랙터 빔 다이브 발동.

---

## 3. 프리팹 조립 및 Zero-Override 구성

### 3.1 `PF_BossTractorBeam.prefab` (신규 완제품 프리팹)
- **계층 구조**:
  - `PF_BossTractorBeam` (Root):
    - `Transform`
    - `MeshFilter`
    - `MeshRenderer`
    - `PolygonCollider2D` (`isTrigger = true`, 4개 꼭짓점 기본 세팅)
    - `BossTractorBeam` (`_topWidth: 0.556`, `_bottomWidth: 3.333`, `_beamHeight: 8.333`, `_beamDuration: 4.0`)

### 3.2 `PF_Enemy_Boss.prefab` (완제품 보스 프리팹 업데이트)
- **계층 구조**:
  - `PF_Enemy_Boss` (Root):
    - `Transform`, `MeshFilter`, `MeshRenderer`, `BezierPathFollower`, `EnemyBase`, `BoxCollider2D`, `EnemyShooting`
    - `EnemyBoss` (`_hoverTargetY: 1.0`, `_beamDuration: 4.0`, 슬롯 및 빔 직렬화 바인딩)
    - 자식 1: `TractorBeam` (`PF_BossTractorBeam` 컴포넌트 구성, 기본 비활성화)
    - 자식 2: `CapturedFighterSlot` (`localPosition: (0, 0.8, 0)`)

---

## 4. 단위 테스트 및 검증 결과 (`BossTractorBeamTests.cs`)

- **테스트 케이스 구성**:
  1. `BossTractorBeam_DefaultDimensions_MatchesTechSpec`: 상단 0.556u, 하단 3.333u, 높이 8.333u, 시간 4.0초 스펙 일치 검증.
  2. `BossTractorBeam_GetLocalVertices_ReturnsAccurateTrapezoidPoints`: 4개 꼭짓점 좌표 정밀도 검증 (`(-0.278, 0)`, `(0.278, 0)`, `(1.667, -8.333)`, `(-1.667, -8.333)`).
  3. `BossTractorBeam_ActivateAndDeactivate_UpdatesStateAndEvents`: 빔 전개/회수 상태 및 `OnBeamActivated`/`OnBeamDeactivated` 이벤트 검증.
  4. `EnemyBoss_StartTractorBeam_SetsEnemyStateToTractorBeam`: 보스 빔 발사 시 `EnemyState.TractorBeam` 전이 검증.
  5. `EnemyBoss_AttachAndDetachCapturedFighter_ManagesSlotAndState`: 상단 결속 슬롯 계층 및 상태 관리 검증.
  6. `CreateBossTractorHoverTrajectory_GeneratesValidPathToHoverY`: 호버링 베지어 진입 궤적 검증.
  7. `CreateBossPostBeamDiveTrajectory_GeneratesDownwardSwoopToScreenBottom`: 빔 종료 후 하단 급강하 궤적 검증.
- **결과**: 모든 테스트 100% 정상 통과 및 C# 무인 컴파일 무결성 확보.
