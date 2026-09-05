# [Implementation 06] 탄환 Rigidbody2D 물리 이동 전환 및 피격 피드백 강화, Zero-Override 프리팹 무결성 구현 기술문서

## 1. 개요 및 목적
- **작업 브랜치**: `fix/bullet_rigidbody_movement`
- **목적**:
  1. 기존 `Transform.position += ...` 직접 좌표 연산 방식의 탄환 이동을 `Rigidbody2D.velocity` 기반의 물리 엔진 연동 이동으로 전환하여 고속 이동 시 발생할 수 있는 관통(Tunneling) 현상을 원천 방지하고 물리 충돌 감지 무결성을 확보합니다.
  2. `PlayerBullet`과 `EnemyBullet`의 `BoxCollider2D` 규격을 `(1.0f, 1.0f)` 정규화 규격으로 통일하고 `CollisionDetectionMode2D.Continuous` 설정을 적용합니다.
  3. 적 기체(`EnemyBase`)의 피격 플래시 지속 시간을 기존 `0.08초`에서 `0.15초`로 상향하여 피격 시각 피드백을 강화하고, `IDamageable` 인터페이스를 통한 통합 피격 파이프라인을 구축합니다.
  4. **Zero-Override 무결성 달성 및 직렬화 참조 타입 불일치 경고 해소**:
     - `PF_FormationGridManager.prefab`에 `EnemyBulletPool` 및 `ExplosionManager`를 프리팹 원본 컴포넌트로 통합.
     - `PlayerShooting._bulletPrefab`과 `EnemyBulletPool._bulletPrefab`의 직렬화 필드 참조를 GameObject가 아닌 각각의 대응 MonoBehaviour 컴포넌트(`PlayerBullet`, `EnemyBullet`)로 직렬화 바인딩하여 Type Mismatch 경고를 원천 해소.
     - `MainGameScene.unity`에서 프리팹 오버라이드(`m_AddedComponents`, `m_RemovedComponents`) 및 중복 컴포넌트를 완전 제거하여 **Zero-Override Clean Instance (Overrides 0건)** 달성.

---

## 2. 변경 파일 및 클래스별 상세 구현

### 2.1 `PlayerBullet.cs` (`Assets/Scripts/Gameplay/Combat/PlayerBullet.cs`)
- **주요 변경 사항**:
  - `[RequireComponent(typeof(Rigidbody2D))]`, `[RequireComponent(typeof(BoxCollider2D))]` 컴포넌트 강제 종속성 추가.
  - `Rigidbody2D` 및 `BoxCollider2D` 캐싱 및 초기화 메서드(`SetupComponents()`) 구현:
    - `gravityScale = 0f`
    - `collisionDetectionMode = CollisionDetectionMode2D.Continuous`
    - `freezeRotation = true`
    - `_boxCollider2D.size = Vector2.one (1.0f, 1.0f)` 및 `isTrigger = true`
  - 물리 이동 파이프라인:
    - `OnEnable()` 및 `FixedUpdate()`에서 `_rigidbody2D.velocity = Vector2.up * _speed` 적용.
    - 기존 단위 테스트 및 수동 시뮬레이션 지원을 위해 `Move(float deltaTime)` 메서드 하위 호환성 유지.
  - 경계 검사:
    - `transform.position.y > MaxY (10.5u)` 도달 시 `ReturnToPool()` 호출로 오브젝트 풀 자동 회수.
  - 피격 처리:
    - `IDamageable` 인터페이스 대상 `TakeDamage()` 호출 후 풀 반환.

### 2.2 `EnemyBullet.cs` (`Assets/Scripts/Gameplay/Combat/EnemyBullet.cs`)
- **주요 변경 사항**:
  - `[RequireComponent(typeof(Rigidbody2D))]`, `[RequireComponent(typeof(BoxCollider2D))]` 컴포넌트 강제 종속성 추가.
  - `Rigidbody2D` 및 `BoxCollider2D` 초기화:
    - `gravityScale = 0f`
    - `collisionDetectionMode = CollisionDetectionMode2D.Continuous`
    - `freezeRotation = true`
    - `_boxCollider2D.size = Vector2.one (1.0f, 1.0f)` 및 `isTrigger = true`
  - 물리 이동 파이프라인:
    - `Initialize()` 시 전달받은 방향 벡터(`_direction`)와 속도(`_speed`)에 맞춰 `_rigidbody2D.velocity = _direction * _speed` 적용.
    - `FixedUpdate()`에서 물리 속도 지속 유지 및 경계 이탈 검사 수행.
  - 경계 검사:
    - `PlayAreaManager.IsOutOfBounds(pos, 1.5f)` 또는 기본 Y/X 경계 이탈 시 `ReturnToPool()`.
  - 피격 처리:
    - `IDamageable` 인터페이스 대상 `TakeDamage()` 호출 후 풀 반환.

### 2.3 `EnemyBase.cs` (`Assets/Scripts/Gameplay/Enemy/EnemyBase.cs`)
- **주요 변경 사항**:
  - `IDamageable` 인터페이스 상속 및 구현 (`bool TakeDamage(int damage = 1)`).
  - 피격 플래시 지속 시간 상향:
    - `_flashDuration` 기본값을 0.15초로 상향 적용 (`EnemyDataSO` 연동 및 fallback 0.15s).
  - `TakeDamage()` 호출 시 잔여 체력 차감, `OnDamaged` 이벤트 발행, `FlashRoutine` 실행 및 체력 0 도달 시 `Die()` 상태 전이.

### 2.4 `IDamageable.cs` (`Assets/Scripts/Gameplay/Combat/IDamageable.cs`)
- **신규 인터페이스**:
  - `public interface IDamageable { bool TakeDamage(int damage = 1); }`
  - `EnemyBase` 및 `PlayerHealth`에 통합 구현되어 탄환 컴포넌트와의 결합도를 최소화(Decoupled).

---

## 3. 프리팹 및 씬 직렬화 무결성 (Zero-Override & Type Integrity)

### 3.1 프리팹 원본 통합 (`PF_FormationGridManager.prefab`)
- `EnemyBulletPool` 및 `ExplosionManager` 컴포넌트를 `PF_FormationGridManager.prefab` 원본 에셋에 추가 및 정식 통합.
- `EnemyBulletPool._bulletPrefab` 직렬화 필드에 `PF_EnemyBullet.prefab`의 `EnemyBullet` 컴포넌트 PPtr 바인딩.
- `ExplosionManager._explosionPrefab` 직렬화 필드에 `PF_Explosion.prefab` 바인딩.

### 3.2 탄환 컴포넌트 직렬화 참조 타입 불일치 해소 (`PF_Player.prefab`)
- `PlayerShooting._bulletPrefab` 필드에 GameObject가 아닌 `PF_PlayerBullet.prefab` 내부의 `PlayerBullet` 컴포넌트 PPtr 직렬화 매핑.
- Unity 엔진 직렬화 역직렬화 시 `Serialized reference type mismatch` 경고 0건 달성.

### 3.3 씬 내 프리팹 오버라이드 0건 달성 (`MainGameScene.unity`)
- `PF_FormationGridManager` 및 `PF_Player`의 씬 내 임의 추가/제거 컴포넌트(`m_AddedComponents`, `m_RemovedComponents`) 완전 정리.
- 씬 내 모든 프리팹 인스턴스가 독립 완제품 프리팹 원본과 100% 일치하는 **Zero-Override Clean Instance** 상태 확립.

---

## 4. 검증 결과
- **C# 컴파일 및 콘솔 로그**: 터미널/IDE/Unity 에디터 진단 기준 에러 0건 (Zero Error), 경고 0건 (Zero Warning) 확인.
- **물리 충돌**: `CollisionDetectionMode2D.Continuous` 스윕을 통한 터널링 방지 무결성 확보.
- **Zero-Override 무결성**: `MainGameScene.unity` 내 모든 PrefabInstance 오버라이드 0건 달성 (`ObjectOverrides: 0, AddedComponents: 0, RemovedComponents: 0`).
