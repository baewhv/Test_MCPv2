# 객체 상호작용 및 아키텍처 관계도 (Object Architecture & Interaction Map)

이 문서는 프로젝트 내 모든 게임 오브젝트 간의 충돌 상호작용, 생성 및 생명주기 관리(Spawner/Pool), 이벤트 구독 관계, ScriptableObject 데이터 바인딩을 총괄 색인화하는 마스터 아키텍처 문서입니다.
`Developer` 에이전트가 신규 기능을 구현하거나 프리팹을 조립할 때마다 실시간으로 갱신 관리합니다.

---

## 1. 객체 상호작용 및 충돌 매트릭스 (Interaction Matrix)

| 발신 객체 (Sender) | 수신 객체 (Receiver) | 감지 방식 (Trigger / Collision) | 상호작용 내용 및 호출 메서드 |
| :--- | :--- | :--- | :--- |
| `PlayerController` (`PF_Player`) | `PlayAreaManager` | 직접 참조 / 메서드 호출 | `ClampPosition()`을 호출하여 화면 좌우 경계 밖 이탈 방지 |
| `PlayerShooting` (`PF_Player`) | `PlayerBullet` (`PF_PlayerBullet`) | 오브젝트 풀 (Get / Release) | 탄환 풀링 활성화 및 화면 내 최대 발사 수(싱글 2발 / 듀얼 4발) 관리 |
| `PlayerBullet` (`PF_PlayerBullet`) | `PlayAreaManager` (TopBorder) | `OnTriggerEnter2D` / `MaxY` 초과 검사 | 화면 상단 경계 도달 시 `ReturnToPool()` 호출하여 풀로 자동 반환 |
| `PlayerBullet` (`PF_PlayerBullet`) | `Enemy` | `OnTriggerEnter2D` | 적 피격 판정 및 데미지 전달 후 `ReturnToPool()` 회수 |
| `PlayAreaManager` (BoundaryColliders) | `Player` / `Bullet` / `Enemy` | OnTriggerEnter2D / ClampPosition / IsOutOfBounds | 플레이어 화면 이탈 방지 클램프 및 화면 외곽 탄환 소멸/적 회수 판정 |

---

## 2. 객체 생성 및 생명주기 관리 (Spawn & Lifecycle Management)

- **플레이어 및 발사체 구조**:
  - `PlayerController`: 화면 최하단(Y: -8)에 위치하여 New Input System 또는 키보드 입력을 통해 1차원 수평 이동 및 경계 제한 수행
  - `PlayerShooting`: 공격 입력 트리거 시 풀에서 탄환을 꺼내 스폰 위치에 활성화하며, 화면 내 동시 탄환 제한(싱글 2발, 듀얼 4발)을 제어
  - `PlayerBullet`: 초당 27.7 units(400 px/sec) 속도로 수직 상향 이동하며, 화면 상단 경계 초과 시 풀로 반환
  - `PlayAreaManager`: Main Camera에 상주하며 3:4(224x288) 종횡비 뷰포트 자동 정렬 및 플레이 영역 경계 Bounds/콜라이더 생명주기 관리

---

## 3. 이벤트 구독 및 알림 흐름 (Event Flow)

| 이벤트 발행자 (Publisher) | 이벤트 명 (Event / Action) | 구독자 (Subscriber) | 반응 로직 (Handler Method) |
| :--- | :--- | :--- | :--- |
| `InputSystem (Player/Move)` | `ReadValue<Vector2>()` | `PlayerController` | `OnEnable()`/`OnDisable()`에서 액션 활성화/해제 및 실시간 X축 이동 반영 |
| `InputSystem (Player/Attack)` | `performed` | `PlayerShooting` | `OnEnable()`/`OnDisable()`에서 액션 활성화/해제 및 `TryFire()` 호출 |
| `PlayerBullet` | `_onDeactivatedCallback` | `PlayerShooting` | `ReturnToPool()` 시 활성 탄환 목록(`_activeBullets`)에서 제거하여 슬롯 반환 |
| `PlayAreaManager` | `RecalculateBounds()` | `Camera` / `BoundaryColliders` | 해상도 변경 시 카메라 Rect 및 외곽 충돌체 재배치 |

---

## 4. ScriptableObject 데이터 참조 구조 (Data Binding)

| 데이터 SO (Asset) | 참조 컴포넌트 (Consumer) | 전달 데이터 및 역할 |
| :--- | :--- | :--- |
| *(차기 태스크에서 적/플레이어 스탯 SO 추가 예정)* | - | - |

---

## 5. 아키텍처 및 호출 흐름 다이어그램 (Architecture Diagram)

```mermaid
graph TD
    InputMove["New Input System (Player/Move)"]
    InputAttack["New Input System (Player/Attack)"]
    Player["PlayerController (PF_Player)"]
    Shooting["PlayerShooting (PF_Player)"]
    BulletPool["PlayerBulletPool (Object Pool)"]
    Bullet["PlayerBullet (PF_PlayerBullet)"]
    PlayAreaManager["PlayAreaManager (Core)"]
    Boundaries["BoundaryColliders (TopBorder)"]

    InputMove --> Player
    Player --> PlayAreaManager
    PlayAreaManager --> Boundaries

    InputAttack --> Shooting
    Shooting -->|"1. TryFire() (Max 2 Bullets)"| BulletPool
    BulletPool -->|"2. Spawn / Activate"| Bullet
    Bullet -->|"3. Vertical Move (27.7 u/s)"| Boundaries
    Bullet -->|"4. Out of Bounds / Hit ➔ ReturnToPool()"| Shooting
```
