# 객체 상호작용 및 아키텍처 관계도 (Object Architecture & Interaction Map)

이 문서는 프로젝트 내 모든 게임 오브젝트 간의 충돌 상호작용, 생성 및 생명주기 관리(Spawner/Pool), 이벤트 구독 관계, ScriptableObject 데이터 바인딩을 총괄 색인화하는 마스터 아키텍처 문서입니다.
`Developer` 에이전트가 신규 기능을 구현하거나 프리팹을 조립할 때마다 실시간으로 갱신 관리합니다.

---

## 1. 객체 상호작용 및 충돌 매트릭스 (Interaction Matrix)

| 발신 객체 (Sender) | 수신 객체 (Receiver) | 감지 방식 (Trigger / Collision) | 상호작용 내용 및 호출 메서드 |
| :--- | :--- | :--- | :--- |
| `PlayerController` (`PF_Player`) | `PlayAreaManager` | 직접 참조 / 메서드 호출 | `ClampPosition()`을 호출하여 화면 좌우 경계 밖 이탈 방지 |
| `PlayerShooting` (`PF_Player`) | `PlayerBullet` (`PF_PlayerBullet`) | 오브젝트 풀 관리 / `TryFire()` | 화면 내 최대 2발(싱글) / 4발(듀얼) 발사 제한 및 인스턴스 활성화 |
| `PlayerBullet` | `PlayAreaManager` / `EnemyBase` | OnTriggerEnter2D / `CheckBoundary()` | 화면 상단(`MaxY`) 이탈 또는 적 피격 시 `ReturnToPool()`을 호출하여 풀 회수 |
| `PlayerHealth` (`PF_Player`) | `EnemyBase` / `EnemyBullet` | OnTriggerEnter2D | 피격 감지 시 `TakeDamage(1)` 호출 (무적 시 무시, 잔기 차감 후 중앙 리스폰) |
| `EnemyBase` (`PF_Enemy_*`) | `PlayerBullet` | OnTriggerEnter2D | `TakeDamage(1)` 호출하여 HP 감소, 플래시 연출 및 0 이하 시 `Die()` |
| `BezierPathFollower` | `BezierCurve` | 정적 수학 메서드 호출 | `EvaluatePath()`, `GetPathTangent()`를 호출하여 진행도($t$)에 따른 2D 위치 및 회전각 계산 |
| `EntranceSequenceManager` | `FormationGridManager` | 직접 참조 / 메서드 호출 | `AssignEnemyToNextAvailableSlot()`을 통해 적에게 슬롯 배정 및 진입 궤적 생성 |
| `FormationGridManager` | `EnemyBase` | 위치 동기화 / 상태 제어 | 슬롯에 안착된 적(`EnemyState.Formation`)의 위치를 Sine wave 호흡 좌표로 실시간 동기화 |

---

## 2. 객체 생성 및 생명주기 관리 (Spawn & Lifecycle Management)

- **플레이어 완제품 프리팹 구조 (`Assets/Prefabs/PF_Player.prefab`)**:
  - `PlayerController`: 1차원 수평 이동 및 경계 클램핑 제어
  - `PlayerShooting`: 오브젝트 풀링 기반 탄환 발사 및 화면 내 2발 제한 관리
  - `PlayerHealth`: 기본 잔기 3기, 피격 시 잔기 차감, 중앙(-8Y) 리스폰 및 1.5초 무적 깜빡임 제어
- **적 3종 완제품 프리팹 구조 (`Assets/Prefabs/`)**:
  - `PF_Enemy_Zako.prefab`: 자코 (체력 1, 청색 곤충, 대기 50점 / 다이브 100점)
  - `PF_Enemy_Goei.prefab`: 고에이 (체력 1, 적색 나비, 대기 80점 / 다이브 160점)
  - `PF_Enemy_Boss.prefab`: 보스 갤러그 (체력 2, 녹색-청색 대형기, 1타 피격 시 청색 변색, 대기 150점 / 다이브 400점)
  - 각 프리팹은 `EnemyBase`, `BezierPathFollower`, `BoxCollider`, `MeshRenderer`, `EnemyDataSO`가 완벽 조립된 Zero-Override 프리팹
- **편대 관리 및 진입 시퀀서 프리팹 (`Assets/Prefabs/PF_FormationGridManager.prefab`)**:
  - `FormationGridManager`: 5행 40기(보스 4, 고에이 16, 자코 20) 슬롯 생성, 좌우 Sine wave 진동 및 수축/팽창 호흡 연출
  - `EntranceSequenceManager`: 5개 그룹(Group 1~5) 순차 스폰 및 3차 베지어 진입 비행 후 지정 슬롯 안착 총괄

---

## 3. 이벤트 구독 및 알림 흐름 (Event Flow)

| 이벤트 발행자 (Publisher) | 이벤트 명 (Event / Action) | 구독자 (Subscriber) | 반응 로직 (Handler Method) |
| :--- | :--- | :--- | :--- |
| `InputSystem (Player/Move)` | `ReadValue<Vector2>()` | `PlayerController` | `OnEnable()`/`OnDisable()`에서 액션 활성화/해제 및 실시간 X축 이동 반영 |
| `InputSystem (Player/Attack)` | `performed` | `PlayerShooting` | `OnAttackAction`에서 `TryFire()`를 호출하여 탄환 발사 |
| `PlayerHealth` | `OnLivesChanged(int)` | `HUD / UIManager` | 잔기 변경 시 하단 잔여 기수 아이콘 실시간 갱신 |
| `PlayerHealth` | `OnPlayerRespawned` | `PlayerController` / `Audio` | 리스폰 효과음 재생 및 기체 조작권 복구 |
| `PlayerHealth` | `OnPlayerDied` | `GameManager / StateDirector` | 최종 사망 시 게임 오버 결과 화면 시퀀스 트리거 |
| `BezierPathFollower` | `OnPathStarted` | `EnemyBase` | 진입/다이브 비행 시작 |
| `BezierPathFollower` | `OnPathCompleted` | `EnemyBase` / `EntranceSequenceManager` | 편대 진입 완료 시 `EnterFormation()` 호출 및 슬롯 안착 |
| `EnemyBase` | `OnDamaged(EnemyBase, int)` | `ScoreManager / SoundManager` | 피격 효과음 및 플래시 연출 |
| `EnemyBase` | `OnDestroyed(EnemyBase)` | `ScoreManager / FormationGridManager` | 점수 가산 및 편대 슬롯 점유 해제 |
| `EntranceSequenceManager` | `OnWaveStarted(int)` | `BGM / HUD` | 각 진입 웨이브(1~5) 개시 연출 |
| `EntranceSequenceManager` | `OnSequenceCompleted` | `StageDirector` | 40기 진입 완료 감지 및 다이브 공격 페이즈 개시 |

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
    Player["Player (PF_Player Prefab)"]
    PlayerCtrl["PlayerController"]
    PlayerShoot["PlayerShooting"]
    PlayerHealth["PlayerHealth"]
    BulletPool["PlayerBulletPool"]

    GridMgr["FormationGridManager (40 Slots, Sine Hover)"]
    SeqMgr["EntranceSequenceManager (5 Waves)"]
    Enemy["Enemy (PF_Enemy_Zako/Goei/Boss)"]
    PathFollower["BezierPathFollower"]
    EnemyData["EnemyDataSO (Stats / Scores)"]
    BezierMath["BezierCurve (Math Engine)"]

    InputSystem --> PlayerCtrl
    InputSystem --> PlayerShoot
    Player --> PlayerCtrl
    Player --> PlayerShoot
    Player --> PlayerHealth

    SeqMgr --> GridMgr
    SeqMgr -->|Spawn & Launch| Enemy
    Enemy --> EnemyData
    Enemy --> PathFollower
    PathFollower --> BezierMath
    PathFollower -->|OnPathCompleted| Enemy
    Enemy -->|EnterFormation| GridMgr
    GridMgr -->|Update Position (Sine Wave)| Enemy
```
