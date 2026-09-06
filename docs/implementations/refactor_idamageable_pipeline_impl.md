# [Implementation] IDamageable 인터페이스 도입 및 피격 파이프라인 디커플링 구현 기술문서

## 1. 개요 및 목적
- **작업 브랜치**: `refactor/idamageable-pipeline`
- **목적**:
  1. 기존 탄환(`PlayerBullet`, `EnemyBullet`)과 피격 대상(`EnemyBase`, `PlayerHealth`) 간의 구체 클래스 직접 참조/캐스팅 강결합을 해소하고, `IDamageable` 인터페이스 기반의 피격 파이프라인으로 완전 디커플링(Decoupling).
  2. `No-Namespace` 표준 원칙을 준수하여 `Assets/Scripts/Gameplay/Combat/IDamageable.cs`를 최상위 전역 스코프로 정의하고 `bool IsAlive { get; }` 프로퍼티 및 `bool TakeDamage(int damage = 1)` 시그니처를 일관되게 제공.
  3. `PlayerBullet`과 `EnemyBullet`에서 불필요한 도메인 구체 네임스페이스(`Galaga.Gameplay.Enemy`, `Galaga.Gameplay.Player`) 종속성을 완전히 제거하고, `TryGetComponent<IDamageable>(out var damageable)`을 통한 안전한 다형성 피격 처리 구현.
  4. Unity 2023+ 권장 컴파일 표준 및 C# 코딩 규칙을 준수하여 컴파일 에러 0건 및 .meta GUID 100% 무손실 보존.

---

## 2. 변경 파일 및 세부 구현 사항

### 2.1 `IDamageable.cs` (`Assets/Scripts/Gameplay/Combat/IDamageable.cs`)
- **No-Namespace 전역 인터페이스 선언**:
  - `Galaga.Gameplay.Combat` 네임스페이스 래핑을 제거하고 전역 스코프로 선언하여 모든 모듈에서 using 구문 없이 즉시 참조 가능하도록 표준화.
- **인터페이스 멤버**:
  - `bool IsAlive { get; }`: 대상의 생존 여부 프로퍼티.
  - `bool TakeDamage(int damage = 1)`: 데미지 적용 및 사망/피격 처리 메서드.

```csharp
/// <summary>
/// 피격 가능한 모든 엔티티(적 기체, 플레이어 등)가 공통으로 구현하는 표준 피격 인터페이스입니다.
/// 탄환(PlayerBullet, EnemyBullet) 및 충돌 판정 시스템과의 결합도를 제거(Decoupling)합니다.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 대상이 현재 생존해 있는지 여부를 반환합니다.
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// 대상에게 데미지를 입힙니다.
    /// </summary>
    /// <param name="damage">입힐 데미지 양 (기본값: 1)</param>
    /// <returns>피격 처리 성공 여부 또는 사망 여부</returns>
    bool TakeDamage(int damage = 1);
}
```

### 2.2 `PlayerBullet.cs` (`Assets/Scripts/Gameplay/Combat/PlayerBullet.cs`)
- **구체 클래스 의존성 제거**:
  - `using Galaga.Gameplay.Enemy;` 네임스페이스 의존성을 제거.
  - `EnemyBase` 구체 클래스 폴백 캐스팅 로직 전면 제거.
- **`TryGetComponent<IDamageable>` 다형성 피격 처리**:
  - `OnTriggerEnter2D` 및 `OnTriggerEnter`에서 충돌 대상이 `IDamageable`을 구현하는 경우 `damageable.TakeDamage(_damage)` 호출 후 탄환 풀 회수(`ReturnToPool()`).
  - 향후 추가될 보스, 장애물 등 `IDamageable` 구현 객체와도 코드 수정 없이 100% 연동 가능.

### 2.3 `EnemyBullet.cs` (`Assets/Scripts/Gameplay/Combat/EnemyBullet.cs`)
- **구체 클래스 의존성 제거**:
  - `using Galaga.Gameplay.Player;` 네임스페이스 의존성을 제거.
  - `PlayerHealth` 구체 클래스 폴백 캐스팅 로직 전면 제거.
- **`TryGetComponent<IDamageable>` 다형성 피격 처리**:
  - `OnTriggerEnter2D` 및 `OnTriggerEnter`에서 플레이어 또는 피격 대상이 `IDamageable`을 구현하는 경우 `damageable.TakeDamage(_damage)` 호출 후 탄환 풀 회수(`ReturnToPool()`).

### 2.4 `PlayerHealth.cs` (`Assets/Scripts/Gameplay/Player/PlayerHealth.cs`)
- **`IDamageable.IsAlive` 프로퍼티 완결**:
  - `public bool IsAlive => !_isDead;` 구현으로 `IDamageable` 인터페이스 규격 100% 충족.
- **피격 충돌 처리 무결성**:
  - `OnTriggerEnter2D` / `OnTriggerEnter`에서 `TryGetComponent<EnemyBullet>`와 `bullet.gameObject.activeSelf` 검사를 통해 비활성화된 탄환의 중복 피격 원천 차단.

### 2.5 `EnemyBase.cs` (`Assets/Scripts/Gameplay/Enemy/EnemyBase.cs`)
- **`IDamageable` 인터페이스 구현 유지 및 충돌 무결성 강화**:
  - `public bool IsAlive => !IsDead;` 및 `public bool TakeDamage(int damage = 1)` 구현 보존.
  - `OnTriggerEnter2D` / `OnTriggerEnter`에서 `TryGetComponent<PlayerBullet>` 및 `bullet.gameObject.activeSelf` 검사를 통해 중복 피격 방어 및 안전한 회수 연동.

---

## 3. 검증 결과

1. **Unity C# 컴파일 무결성 검증**:
   - Unity Editor Console 및 Domain Reload 검증 결과: **컴파일 에러 0건 (0 Errors)**.
2. **.meta GUID 무결성**:
   - 기존 파일 In-place 수정을 통해 모든 스크립트 고유 GUID 보존 완료.
3. **아키텍처 결합도 개선**:
   - Combat 도메인(탄환)이 Enemy / Player 구체 도메인을 직접 참조하지 않고 `IDamageable` 추상 계층을 통해 상호작용하도록 완벽히 디커플링 달성.
