# [Implementation 08] 스테이지 진행 및 섬멸 판정 매니저(StageManager) 구현 기술문서

## 1. 개요 및 목적
- **작업 브랜치**: `feat_phase4_stage_progression_manager`
- **목적**:
  1. 원작 Galaga의 스테이지 순환 루프, 40기 적 생존 수 추적 및 전멸(Stage Clear) 자동 감지 시스템을 구축합니다.
  2. 스테이지 번호(`CurrentStage`) 관리, $4n-1$ 주기(Stage 3, 7, 11, 15...) 챌린징 스테이지 분기 플래그를 수학적 공식으로 산출합니다.
  3. 적 전멸 감지 시 다이브 공격 정지, 클리어 이벤트 발행, 클리어 딜레이(2.5초) 후 다음 스테이지로 자동 전환되는 루프 파이프라인을 확립합니다.
  4. `EntranceSequenceManager`, `FormationGridManager`, `EnemyDiveController`, `PlayerHealth`와의 유기적 이벤트 결합 및 메모리 누수 없는 해제(`OnDisable`)를 구현합니다.
  5. 독립 완제품 프리팹 `PF_StageManager.prefab`을 신규 조립하여 씬 독립성과 Zero-Override 무결성을 보장합니다.

---

## 2. 변경 파일 및 클래스별 상세 구현

### 2.1 `StageManager.cs` (`Assets/Scripts/Gameplay/Stage/StageManager.cs`)
- **네임스페이스**: `Galaga.Gameplay.Stage`
- **싱글톤 아키텍처**:
  - `public static StageManager Instance { get; private set; }`
  - `Awake()` 시 싱글톤 인스턴스 할당 및 중복 인스턴스 파괴, `OnDisable()` 시 `Instance = null` 초기화.
- **직렬화 필드 ([SerializeField] private)**:
  - `_startingStage` (기본 1): 최초 시작 스테이지 번호.
  - `_totalStageEnemies` (기본 40): 스테이지 당 스폰 총 적 수.
  - `_stageStartDelay` (기본 1.0초): 스테이지 시작 전 준비 딜레이.
  - `_stageClearDelay` (기본 2.5초): 적 전멸 후 다음 스테이지 전환 전 클리어 딜레이.
  - `_autoStartOnAwake` (기본 false): Awake 시점 자동 시작 여부.
  - `_autoAdvanceToNextStage` (기본 true): 클리어 후 다음 스테이지 자동 진입 여부.
  - `_entranceSequenceManager`: 편대 진입 시퀀스 매니저 참조.
  - `_formationGridManager`: 편대 그리드 매니저 참조.
  - `_enemyDiveController`: 적 급강하 컨트롤러 참조.
  - `_playerHealth`: 플레이어 사망 이벤트 참조.
- **런타임 상태 관리**:
  - `_currentStage`: 현재 진행 중인 스테이지 번호.
  - `_aliveEnemyCount`: 현재 생존 중인 적 기체 수.
  - `_spawnedEnemyCount`: 현재 스폰 완료된 적 기체 수.
  - `_isStageInProgress`: 스테이지 교전 진행 플래그.
  - `_isStageClearing`: 스테이지 클리어 시퀀스 진행 중 플래그.
  - `_isChallengingStage`: 챌린징 스테이지 여부 플래그 ($4n-1$).
  - `_isEntranceSequenceFinished`: 5개 웨이브 진입 시퀀스 완료 여부.
  - `_registeredEnemies`: 현재 등록 추적 중인 적 기체 리스트.
- **C# 이벤트 시스템**:
  - `public event Action<int> OnStageChanged`: 스테이지 번호 설정 시 발행 (HUD/Badge 동기화).
  - `public event Action<int> OnStageStarted`: 스테이지 시작 딜레이 후 실 교전 개시 시 발행.
  - `public event Action<int> OnStageCleared`: 적 전멸로 스테이지 클리어 시 발행.
  - `public event Action<int> OnEnemyCountChanged`: 생존 적 수 변경 시 실시간 발행.
  - `public event Action<bool> OnChallengingStageTriggered`: 챌린징 스테이지 진입 여부 발행.
  - `public event Action OnAllEnemiesDefeated`: 40기 전멸 판정 즉시 발행.
  - `public event Action<EnemyBase> OnEnemyRegistered`: 적 기체 스폰 및 등록 시 발행.
  - `public event Action<EnemyBase> OnEnemyUnregistered`: 적 기체 해제 시 발행.
  - `public event Action OnGameOver`: 플레이어 사망으로 게임 오버 시 발행.
- **핵심 메서드**:
  - `CheckIsChallengingStage(int stageNumber)`:
    - 수학 공식: `stageNumber >= 3 && (stageNumber + 1) % 4 == 0`
    - Stage 3, 7, 11, 15, 19... ➔ `true` 반환, 그 외 ➔ `false` 반환.
  - `Initialize(int startingStage = 1)`: 스테이지 상태 및 적 카운트를 명시적으로 초기화.
  - `StartStage(int stageNumber)`:
    - 스테이지 번호 갱신, 챌린징 플래그 계산, 그리드 초기화(`InitializeGrid()`), 다이브 컨트롤러 일시 정지 후 시작 코루틴 실행.
  - `AdvanceToNextStage()`:
    - `StartStage(_currentStage + 1)`을 호출하여 다음 스테이지 루프로 진입.
  - `RegisterEnemy(EnemyBase enemy)`:
    - 스폰된 적을 등록하고 `_aliveEnemyCount` 증가, `enemy.OnDestroyed += HandleEnemyDestroyed` 바인딩.
  - `HandleEnemyDestroyed(EnemyBase enemy)`:
    - 적 사망 시 `_aliveEnemyCount` 차감, 이벤트 발행 후 `CheckStageClearCondition()` 검사.
  - `CheckStageClearCondition()`:
    - 진입 시퀀스 완료 또는 40기 스폰 완료 상태에서 `_aliveEnemyCount <= 0` 달성 시 `TriggerStageClear()` 호출.
  - `TriggerStageClear()`:
    - 다이브 공격 정지(`StopAutoDive()`), 클리어 이벤트 발행, `StageClearRoutine` 코루틴(2.5초 대기 후 `AdvanceToNextStage`) 실행.
  - `HandlePlayerDied()`:
    - 게임 오버 처리 및 다이브 정지, `OnGameOver` 이벤트 발행.

---

## 3. 프리팹 및 씬 직렬화 무결성 (Zero-Override Integrity)

### 3.1 `PF_StageManager.prefab` 신규 조립
- 독립 완제품 프리팹 `Assets/Prefabs/PF_StageManager.prefab` 신규 생성.
- `StageManager` 컴포넌트 부착:
  - `_startingStage: 1`
  - `_totalStageEnemies: 40`
  - `_stageStartDelay: 1.0`
  - `_stageClearDelay: 2.5`
  - `_autoStartOnAwake: false`
  - `_autoAdvanceToNextStage: true`
- 씬 배치 시 별도의 인스펙터 오버라이드 없이 즉시 사용 가능한 Zero-Override 구조 확립.

---

## 4. 검증 결과
- **C# 컴파일 무결성**: Assembly-CSharp 컴파일 에러 0건.
- **챌린징 스테이지 공식 검증**:
  - Stage 1, 2, 4, 5, 6 ➔ `False`
  - Stage 3, 7, 11, 15 ➔ `True` ($4n-1$ 주기 완벽 일치)
- **생명주기 및 섬멸 판정 검증**:
  - 기체 등록 시 `AliveEnemyCount` 증가 ➔ 격파 시 차감 ➔ 0기 도달 및 진입 완료 시 `IsStageClearing = true`, `OnStageCleared` 발행 정상 확인.
