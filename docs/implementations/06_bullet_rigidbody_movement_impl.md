# [Implementation 06] 탄환 Rigidbody2D 물리 이동 전환 및 피격 피드백 강화 구현 기술문서

## 1. 개요 및 목적
- **작업 브랜치**: `fix/bullet_rigidbody_movement`
- **목적**:
  1. 기존 `Transform.position += ...` 직접 좌표 연산 방식의 탄환 이동을 `Rigidbody2D.velocity` 기반의 물리 엔진 연동 이동으로 전환하여 고속 이동 시 발생할 수 있는 관통(Tunneling) 현상을 원천 방지하고 물리 충돌 감지 무결성을 확보합니다.
  2. `PlayerBullet`과 `EnemyBullet`의 `BoxCollider2D` 규격을 `(1.0f, 1.0f)` 정규화 규격으로 통일하고 `CollisionDetectionMode2D.Continuous` 설정을 적용합니다.
  3. 적 기체(`EnemyBase`)의 피격 플래시 지속 시간을 기존 `0.08초`에서 `0.15초`로 상향하여 피격 시각 피드백을 강화하고, `IDamageable` 인터페이스를 통한 통합 피격 파이프라인을 구축합니다.

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

## 3. 검증 결과
- **C# 컴파일**: 터미널/IDE 진단 기준 에러 0건 (Zero Error) 확인.
- **물리 충돌**: `CollisionDetectionMode2D.Continuous` 스윕을 통한 터널링 방지 무결성 확보.
