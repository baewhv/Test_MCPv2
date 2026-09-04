# 객체 상호작용 및 아키텍처 관계도 (Object Architecture & Interaction Map)

이 문서는 프로젝트 내 모든 게임 오브젝트 간의 충돌 상호작용, 생성 및 생명주기 관리(Spawner/Pool), 이벤트 구독 관계, ScriptableObject 데이터 바인딩을 총괄 색인화하는 마스터 아키텍처 문서입니다.
`Developer` 에이전트가 신규 기능을 구현하거나 프리팹을 조립할 때마다 실시간으로 갱신 관리합니다.

---

## 1. 객체 상호작용 및 충돌 매트릭스 (Interaction Matrix)

| 발신 객체 (Sender) | 수신 객체 (Receiver) | 감지 방식 (Trigger / Collision) | 상호작용 내용 및 호출 메서드 |
| :--- | :--- | :--- | :--- |
| `PlayAreaManager` (독립 매니저) | `Camera` (`Main Camera`) | 직렬화 참조 (`_targetCamera`) | 3:4 타겟 해상도(224x288) 뷰포트 Rect 및 Orthographic Size(10) 동기화 |
| `PlayAreaManager` | BoundaryColliders (`Boundary` Tag) | 런타임 4방향 생성 | Left, Right, Top, Bottom 외곽 BoxCollider2D(isTrigger: true) 배치 |
| `PlayerController` (`PF_Player`) | `PlayAreaManager` | 직접 참조 / 싱글톤 인스턴스 | `ClampPosition()`을 호출하여 화면 좌우 경계 밖 이탈 방지 |
| `PlayerShooting` (`PF_Player`) | `PlayerBullet` (`PF_PlayerBullet`) | 오브젝트 풀 관리 / `TryFire()` | 화면 내 최대 2발(싱글) / 4발(듀얼) 발사 제한 및 인스턴스 활성화 |
| `PlayerBullet` (`PF_PlayerBullet`) | `PlayAreaManager` / `EnemyBase` | OnTriggerEnter2D / `CheckBoundary()` | 화면 상단(`MaxY`) 이탈 또는 적 피격 시 `ReturnToPool()`을 호출하여 풀 회수 |
| `PlayerHealth` (`PF_Player`) | `EnemyBase` / `EnemyBullet` | OnTriggerEnter2D | 피격 감지 시 `TakeDamage(1)` 호출 (무적 시 무시, 잔기 차감 후 중앙 리스폰) |
| `EnemyBase` (`PF_Enemy_*`) | `PlayerBullet` | OnTriggerEnter2D | `TakeDamage(1)` 호출하여 HP 감소, 피격 플래시 및 0 이하 시 `Die()` ➔ 폭발 이펙트 트리거 |
| `EnemyDiveController` | `FormationGridManager` / `EnemyBase` | 직접 참조 / 상태 변경 | 대기 중인 적(단독/호위)을 선별하여 3차 베지어 급강하(`EnemyState.Diving`) 궤적 생성 및 비행 개시 |
| `EnemyDiveController` | `EnemyBase` (복귀) | 경로 완료 콜백 / 텔레포트 | 화면 하단 통과 시 화면 상단($y=11$)으로 재진입하여 소속 슬롯으로 복귀(`EnemyState.Returning` ➔ `EnterFormation`) |
| `EnemyShooting` (`PF_Enemy_*`) | `EnemyBulletPool` / `PlayerTransform` | `OnProgressChanged` ($t=0.3\sim 0.6$) | 다이브 중간 구간에서 플레이어 현재 위치를 조준하여 `EnemyBullet` 스폰 및 사격 |
| `EnemyBullet` (`PF_EnemyBullet`) | `PlayerHealth` / `PlayAreaManager` | OnTriggerEnter2D / `CheckBoundary()` | 플레이어 충돌 시 `PlayerHealth.TakeDamage(1)` 호출 및 풀 회수, 화면 하단 이탈 시 자동 풀 반환 |
| `ExplosionManager` | `ExplosionEffect` (`PF_Explosion`) | 오브젝트 풀 관리 / 이벤트 리스너 | 적 격파(`OnDestroyed`) 및 플레이어 사망 시 `SpawnExplosion()`을 호출하여 파티클 버스트 재생 |
| `BezierPathFollower` | `BezierCurve` | 정적 수학 메서드 호출 | `EvaluatePath()`, `GetPathTangent()`를 호출하여 진행도($t$)에 따른 2D 위치 및 회전각 계산 |
| `EntranceSequenceManager` | `FormationGridManager` | 직접 참조 / 메서드 호출 | `AssignEnemyToNextAvailableSlot()`을 통해 적에게 슬롯 배정 및 진입 궤적 생성 |
| `FormationGridManager` | `EnemyBase` | 위치 동기화 / 상태 제어 | 슬롯에 안착된 적(`EnemyState.Formation`)의 위치를 Sine wave 호흡 좌표로 실시간 동기화 |

---

## 2. 객체 생성 및 생명주기 관리 (Spawn & Lifecycle Management)

- **코어 매니저 계층 구조 (`MainGameScene.unity`)**:
  - `Main Camera`: 순수 렌더링 및 AudioListener 전담
  - `PlayAreaManager`: 독립 GameObject로 배치되어 Target Camera 직렬화 바인딩 및 3:4 뷰포트/월드 경계 계산, Boundary BoxCollider2D 관리
- **플레이어 완제품 프리팹 구조 (`Assets/Prefabs/PF_Player.prefab`)**:
  - `PlayerController`: 1차원 수평 이동 및 경계 클램핑 제어
  - `PlayerShooting`: 오브젝트 풀링 기반 탄환 발사 및 화면 내 2발 제한 관리
  - `PlayerHealth`: 기본 잔기 3기, 피격 시 잔기 차감, 중앙(-8Y) 리스폰 및 1.5초 무적 깜빡임 제어
  - `BoxCollider2D` (isTrigger: true): 적 기체 및 적 탄환과의 2D 피격 판정
- **적 3종 완제품 프리팹 구조 (`Assets/Prefabs/`)**:
  - `PF_Enemy_Zako.prefab`: 자코 (체력 1, 청색 곤충, 대기 50점 / 다이브 100점)
  - `PF_Enemy_Goei.prefab`: 고에이 (체력 1, 적색 나비, 대기 80점 / 다이브 160점)
  - `PF_Enemy_Boss.prefab`: 보스 갤러그 (체력 2, 녹색-청색 대형기, 1타 피격 시 청색 변색, 대기 150점 / 다이브 400점)
  - 각 프리팹은 `EnemyBase`, `BezierPathFollower`, `EnemyShooting`, `BoxCollider2D`, `MeshRenderer`, `EnemyDataSO`가 완벽 조립된 Zero-Override 프리팹
- **탄환 및 전투 발사체 프리팹 (`Assets/Prefabs/`)**:
  - `PF_PlayerBullet.prefab`: 플레이어 수직 상향 탄환 (속도 27.7 units/sec, BoxCollider2D)
  - `PF_EnemyBullet.prefab`: 적 조준 하향 탄환 (속도 16.0 units/sec, BoxCollider2D, 적색 발광)
  - `PF_Explosion.prefab`: 격파/피격 폭발 시각 효과 (ParticleSystem 버스트 + ExplosionEffect)
- **편대 관리 및 진입/다이브 시퀀서 프리팹 (`Assets/Prefabs/PF_FormationGridManager.prefab`)**:
  - `FormationGridManager`: 5행 40기(보스 4, 고에이 16, 자코 20) 슬롯 생성, 좌우 Sine wave 진동 및 수축/팽창 호흡 연출
  - `EntranceSequenceManager`: 5개 그룹(Group 1~5) 순차 스폰 및 3차 베지어 진입 비행 후 지정 슬롯 안착 총괄
  - `EnemyDiveController`: 주기적 단독/보스+호위기 동반 급강하 궤적 계산, 화면 하단 통과 후 상단 루프 복귀 제어
  - `EnemyBulletPool`: 적 탄환 16기 사전 풀링 및 발사체 관리
  - `ExplosionManager`: 폭발 이펙트 10기 사전 풀링 및 격파 시 자동 스폰 연동

---

## 3. 이벤트 구독 및 알림 흐름 (Event Flow)

| 이벤트 발행자 (Publisher) | 이벤트 명 (Event / Action) | 구독자 (Subscriber) | 반응 로직 (Handler Method) |
| :--- | :--- | :--- | :--- |
| `InputSystem (Player/Move)` | `ReadValue<Vector2>()` | `PlayerController` | `OnEnable()`/`OnDisable()`에서 액션 활성화/해제 및 실시간 X축 이동 반영 |
| `InputSystem (Player/Attack)` | `performed` | `PlayerShooting` | `OnAttackAction`에서 `TryFire()`를 호출하여 탄환 발사 |
| `PlayerHealth` | `OnLivesChanged(int)` | `HUD / UIManager` | 잔기 변경 시 하단 잔여 기수 아이콘 실시간 갱신 |
| `PlayerHealth` | `OnPlayerRespawned` | `PlayerController` / `Audio` | 리스폰 효과음 재생 및 기체 조작권 복구 |
| `PlayerHealth` | `OnPlayerDied` | `GameManager / ExplosionManager` | 최종 사망 시 폭발 이펙트 재생 및 게임 오버 시퀀스 트리거 |
| `BezierPathFollower` | `OnPathStarted` | `EnemyBase` | 진입/다이브 비행 시작 |
| `BezierPathFollower` | `OnProgressChanged(float)` | `EnemyShooting` | 진행도 $t=0.3\sim 0.6$ 구간 도달 시 플레이어 조준 탄환 1발 발사 |
| `BezierPathFollower` | `OnPathCompleted` | `EnemyBase` / `EnemyDiveController` | 경로 완료 시 진입 안착 또는 하단 루프 복귀 핸들러 실행 |
| `EnemyDiveController` | `OnDiveStarted(EnemyBase)` | `Audio / HUD` | 급강하 효과음 재생 및 적 비행 상태 전환 |
| `EnemyDiveController` | `OnDiveCompleted(EnemyBase)` | `FormationGridManager` | 복귀 완료 후 슬롯 안착 및 대기 상태 복구 |
| `EnemyBase` | `OnDamaged(EnemyBase, int)` | `ScoreManager / SoundManager` | 피격 효과음 및 플래시 연출 |
| `EnemyBase` | `OnDestroyed(EnemyBase)` | `ScoreManager / FormationGridManager / ExplosionManager` | 점수 가산, 편대 슬롯 점유 자동 해제 및 폭발 이펙트 스폰 |
| `EntranceSequenceManager` | `OnWaveStarted(int)` | `BGM / HUD` | 각 진입 웨이브(1~5) 개시 연출 |
| `EntranceSequenceManager` | `OnSequenceCompleted` | `EnemyDiveController` | 40기 진입 완료 시 자동 다이브 루프(`StartAutoDive()`) 활성화 |

---

## 4. ScriptableObject 데이터 참조 구조 (Data Binding)

| 데이터 SO (Asset) | 참조 컴포넌트 (Consumer) | 전달 데이터 및 역할 |
| :--- | :--- | :--- |
| `SO_Enemy_Zako.asset` | `PF_Enemy_Zako` (`EnemyBase`) | 자코 스펙 (MaxHP: 1, ScoreStay: 50, ScoreDive: 100, Speed: 10) |
| `SO_Enemy_Goei.asset` | `PF_Enemy_Goei` (`EnemyBase`) | 고에이 스펙 (MaxHP: 1, ScoreStay: 80, ScoreDive: 160, Speed: 11) |
| `SO_Enemy_Boss.asset` | `PF_Enemy_Boss` (`EnemyBase`) | 보스 스펙 (MaxHP: 2, ScoreStay: 150, ScoreDive: 400, Speed: 9, 2타 피격 색상) |

---

## 5. 아키텍처 및 호출 흐름 다이어그램 (Architecture Diagram)

```mermaid
graph TD
    InputSystem["New Input System (Move / Attack)"]
    Camera["Main Camera (Rendering Only)"]
    PlayAreaMgr["PlayAreaManager (Standalone Manager)"]
    Boundary["Boundary BoxColliders (Tag: Boundary)"]

    Player["Player (PF_Player Prefab)"]
    PlayerCtrl["PlayerController"]
    PlayerShoot["PlayerShooting"]
    PlayerHealth["PlayerHealth"]
    PlayerBullet["PlayerBullet (PF_PlayerBullet)"]

    GridMgr["FormationGridManager (40 Slots, Sine Hover)"]
    SeqMgr["EntranceSequenceManager (5 Waves)"]
    DiveCtrl["EnemyDiveController (Dive & Return AI)"]
    EnemyBulletPool["EnemyBulletPool (PF_EnemyBullet)"]
    ExplosionMgr["ExplosionManager (PF_Explosion)"]

    Enemy["Enemy (PF_Enemy_Zako/Goei/Boss)"]
    PathFollower["BezierPathFollower"]
    Shooting["EnemyShooting"]
    EnemyData["EnemyDataSO (Stats / Scores)"]
    BezierMath["BezierCurve (Math Engine)"]

    PlayAreaMgr -->|Sync Viewport & Ortho| Camera
    PlayAreaMgr -->|Generate| Boundary
    InputSystem --> PlayerCtrl
    InputSystem --> PlayerShoot
    Player --> PlayerCtrl
    Player --> PlayerShoot
    Player --> PlayerHealth
    PlayerCtrl -->|Clamp Position| PlayAreaMgr
    PlayerShoot -->|Fire| PlayerBullet

    SeqMgr --> GridMgr
    SeqMgr -->|Spawn & Launch| Enemy
    SeqMgr -->|OnSequenceCompleted| DiveCtrl
    DiveCtrl -->|Trigger Dive / Escort| Enemy
    Enemy --> EnemyData
    Enemy --> PathFollower
    Enemy --> Shooting
    Shooting -->|Fire (t=0.3~0.6)| EnemyBulletPool
    PathFollower --> BezierMath
    PathFollower -->|OnPathCompleted| Enemy
    Enemy -->|EnterFormation| GridMgr
    GridMgr -->|Update Position (Sine Wave)| Enemy

    PlayerBullet -->|OnTriggerEnter2D: Damage| Enemy
    PlayerBullet -->|OnTriggerEnter2D: Despawn| Boundary
    EnemyBulletPool -->|OnTriggerEnter2D: Damage| PlayerHealth
    EnemyBulletPool -->|OnTriggerEnter2D: Despawn| Boundary
    Enemy -->|OnTriggerEnter2D: Collision| PlayerHealth
    Enemy -->|OnDestroyed| ExplosionMgr
    PlayerHealth -->|OnPlayerDied| ExplosionMgr
```
