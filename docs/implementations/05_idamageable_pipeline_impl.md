# [Implementation 05] IDamageable 인터페이스 도입 및 피격 파이프라인 디커플링 기술문서

## 1. 모듈 개요 및 도입 배경

### 1.1 개요
본 문서는 Galaga 프로젝트의 전투 및 피격 시스템에서 구체 클래스 간의 직접적인 강결합을 해소하고, 객체지향 5대 원칙 중 **개방-폐쇄 원칙(Open-Closed Principle, OCP)** 및 **의존 역전 원칙(Dependency Inversion Principle, DIP)**을 달성하기 위해 `IDamageable` 공용 인터페이스를 도입하고 탄환/피격 파이프라인을 디커플링한 구현 상세를 기록합니다.

### 1.2 도입 배경 및 문제점
- **구체 클래스 의존성(Concrete Coupling)**: 기존 `PlayerBullet`은 `EnemyBase`를, `EnemyBullet`은 `PlayerHealth`를 직접 `GetComponent<T>()`로 조회하여 호출했습니다.
- **확장 한계**: 추후 보스 갤러그의 트랙터 빔에 의해 포획된 아군 파이터(`CapturedFighter`), 장애물, 듀얼 파이터 편측 피격 등 새로운 피격 대상 엔티티가 추가될 때마다 발사체 클래스의 충돌 분기문(`if-else`)을 계속 수정해야 하는 구조적 한계가 존재했습니다.
- **해결책**: 모든 피격 가능 개체가 구현하는 표준 `IDamageable` 인터페이스를 전역 스코프에 정의하고, 발사체는 인터페이스 단일 통로로만 데미지를 전달하도록 파이프라인을 전면 리팩토링했습니다.

---

## 2. IDamageable 인터페이스 정의 및 멤버 명세

### 2.1 인터페이스 정의 (`Assets/Scripts/Core/IDamageable.cs`)
```csharp
public interface IDamageable
{
    /// <summary>
    /// 현재 잔여 체력 (적 기체의 경우 HP, 플레이어의 경우 잔기 수)
    /// </summary>
    int CurrentHP { get; }

    /// <summary>
    /// 사망 또는 격파 여부
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// 지정된 데미지를 적용합니다.
    /// </summary>
    /// <param name="damage">적용할 데미지 수치</param>
    void TakeDamage(int damage);
}
```

### 2.2 프로퍼티 및 메서드 시그니처 명세
| 멤버 명칭 | 유형 | 접근자 | 설명 |
| :--- | :--- | :--- | :--- |
| `CurrentHP` | `int` | `get` | 엔티티의 잔여 체력 반환 (적: HP 수치, 플레이어: 잔기 수) |
| `IsDead` | `bool` | `get` | 엔티티가 파괴/사망 상태인지 여부 반환 |
| `TakeDamage(int damage)` | `void` | `Method` | 전달받은 데미지를 엔티티 체력에 차감하고 피격 연출/사망 시퀀스 트리거 |

---

## 3. EnemyBase 및 PlayerHealth 구현 세부사항

### 3.1 `EnemyBase` (`Assets/Scripts/Gameplay/Enemy/EnemyBase.cs`)
- **인터페이스 상속**: `public class EnemyBase : MonoBehaviour, IDamageable`
- **프로퍼티 매핑**:
  - `public int CurrentHP => _currentHP;`
  - `public bool IsDead => _currentState == EnemyState.Dead || _currentHP <= 0;`
- **메서드 구현**:
  - 기존 `public bool TakeDamage(int damage = 1)` 로직을 그대로 유지하여 기존 단위 테스트 및 레거시 호출 호환성 보장
  - `void IDamageable.TakeDamage(int damage) => TakeDamage(damage);` 명시적 인터페이스 구현으로 `IDamageable` 계약 충족

### 3.2 `PlayerHealth` (`Assets/Scripts/Gameplay/Player/PlayerHealth.cs`)
- **인터페이스 상속**: `public class PlayerHealth : MonoBehaviour, IDamageable`
- **프로퍼티 매핑**:
  - `public int CurrentHP => _currentLives;` (IDamageable 규격)
  - `public int CurrentLives => _currentLives;` (기존 프로퍼티 유지)
  - `public bool IsDead => _isDead;`
- **메서드 구현**:
  - 기존 `public bool TakeDamage(int damage = 1)` 로직(무적 판정, 잔기 차감, 리스폰/사망)을 온전히 유지
  - `void IDamageable.TakeDamage(int damage) => TakeDamage(damage);` 명시적 인터페이스 구현을 통해 `IDamageable` 표준 계약 충족

---

## 4. PlayerBullet 및 EnemyBullet 충돌 처리 디커플링 로직

### 4.1 `PlayerBullet` 충돌 디커플링 (`Assets/Scripts/Gameplay/Combat/PlayerBullet.cs`)
- **Before**: `EnemyBase enemy = collision.GetComponent<EnemyBase>(); enemy.TakeDamage(_damage);`
- **After**:
```csharp
private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision == null || !gameObject.activeSelf) return;

    if (collision.CompareTag("Boundary") || collision.gameObject.name == "TopBorder")
    {
        ReturnToPool();
        return;
    }

    if (collision.CompareTag("Enemy") || collision.name.Contains("Enemy"))
    {
        IDamageable damageable = collision.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }
        ReturnToPool();
    }
}
```

### 4.2 `EnemyBullet` 충돌 디커플링 (`Assets/Scripts/Gameplay/Combat/EnemyBullet.cs`)
- **Before**: `PlayerHealth player = collision.GetComponent<PlayerHealth>(); player.TakeDamage(_damage);`
- **After**:
```csharp
private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision == null || !gameObject.activeSelf) return;

    if (collision.CompareTag("Player") || collision.name.Contains("Player"))
    {
        IDamageable damageable = collision.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }
        ReturnToPool();
        return;
    }

    if (collision.CompareTag("Boundary") || collision.gameObject.name == "BottomBorder")
    {
        ReturnToPool();
    }
}
```

---

## 5. 설계 결정 사유(Rationale) 및 기대 효과

1. **완전한 상호 디커플링 (Decoupling)**:
   - 발사체(`PlayerBullet`, `EnemyBullet`)는 피격 대상의 내부 구현(적 AI 상태머신, 플레이어 잔기/무적 코루틴 등)을 전혀 알 필요 없이 `IDamageable` 계약만을 신뢰합니다.
2. **Phase 5 포획기 및 특수 기체 확장 용이성**:
   - 향후 Phase 5에서 추가될 `CapturedFighter`나 보너스 라운드 특수 타깃이 생겨도 발사체 수정 없이 `IDamageable`만 구현하면 즉시 피격 파이프라인에 편입됩니다.
3. **기존 NUnit 단위 테스트 및 하위 호환성 100% 보존**:
   - `bool TakeDamage(int)` 기존 시그니처와 `void IDamageable.TakeDamage(int)` 명시적 구현을 병행하여 기존 NUnit 테스트(`CombatTests.cs`, `PlayerLivesTests.cs` 등)와의 호환성을 완벽하게 유지했습니다.
