# Issue #19 탄환 발사자 오폭 및 셀프 데미지 버그 수정 기술문서

## 1. 개요 및 배경
- **이슈 번호**: Issue #19
- **작업 브랜치**: `fix/issue-19-bullet-self-damage`
- **문제 현상**: 탄환 발사 시 발사자 본인의 콜라이더와 즉시 충돌하여 발사자가 자폭 피해를 입거나, 적 탄환이 아군 적 기체에 피해를 입히는 현상 발생 (`PlayerBullet` -> `PlayerHealth`, `EnemyBullet` -> `EnemyBase`).

---

## 2. 근본 원인 분석 (Root Cause Analysis)
1. **탄환-피격 판정 구조 결함**:
   - `PlayerBullet` 및 `EnemyBullet`의 `OnTriggerEnter2D` / `OnTriggerEnter` 내부에서 `collision.TryGetComponent<IDamageable>(out var damageable)`로 모든 `IDamageable` 대상에 무조건 데미지를 부여함.
   - `PlayerHealth`와 `EnemyBase` 모두 `IDamageable`을 구현하고 있어, 탄환 스폰 직후 발사자 콜라이더 접촉 시 발사자 본인에게 데미지가 적용됨.
2. **EnemyBase 피격 판정 문자열 필터링 결함**:
   - `EnemyBase.cs`의 `OnTriggerEnter2D`에서 `collision.name.Contains("Bullet")`로 판정하여 `EnemyBullet` 접촉 시에도 `TakeDamage(1)`이 발동되는 버그 존재.

---

## 3. 수정 내역 및 아키텍처 개선

### 3.1 PlayerBullet.cs
- 플레이어 본인 및 아군 탄환/기체 충돌 무시 필터링 추가:
  - `collision.CompareTag("Player")`
  - `collision.CompareTag("PlayerBullet")`
  - `collision.GetComponent<PlayerHealth>() != null`
  - `collision.GetComponent<PlayerController>() != null`
  - `collision.name.Contains("Player")`
- 플레이어 대상 충돌 시 데미지 부여 및 풀 회수를 수행하지 않고 안전하게 통과 처리.
- 적(`EnemyBase` 등) 및 외부 `IDamageable` 대상에 대해서만 피격 데미지 부여 및 풀 반환.

### 3.2 EnemyBullet.cs
- 적 기체 본인 및 아군 적 탄환/기체 충돌 무시 필터링 추가:
  - `collision.CompareTag("Enemy")`
  - `collision.CompareTag("EnemyBullet")`
  - `collision.GetComponent<EnemyBase>() != null`
  - `collision.name.Contains("Enemy")`
- 적 대상 충돌 시 데미지 부여 및 풀 회수를 수행하지 않고 통과 처리.
- 플레이어(`PlayerHealth`) 및 외부 `IDamageable` 대상에 대해서만 피격 데미지 부여 및 풀 반환.

### 3.3 EnemyBase.cs & PlayerHealth.cs 2중 방어 필터링
- `EnemyBase.cs`: `EnemyBullet` 접촉 시 무시하도록 방어 로직을 명확히 하고, 오직 `PlayerBullet`에 대해서만 피격 처리.
- `PlayerHealth.cs`: `PlayerBullet` 접촉 시 무시하도록 방어 로직을 보강하여 발사자 셀프 데미지 원천 차단.

---

## 4. 무결성 및 단위 테스트 검증

### 4.1 신규 추가 테스트 케이스
1. **CombatTests.cs**:
   - `PlayerBullet_DoesNotDamage_Player_OnTriggerEnter`: PlayerBullet이 플레이어 콜라이더 접촉 시 데미지 미부여 및 풀 미회수 검증.
   - `EnemyBullet_DoesNotDamage_Enemy_OnTriggerEnter`: EnemyBullet이 적 기체 콜라이더 접촉 시 데미지 미부여 및 풀 미회수 검증.
2. **IDamageablePipelineTests.cs**:
   - `PlayerBullet_Ignores_Player_Collision_NoSelfDamage`: IDamageable 파이프라인에서 플레이어 자폭 방지 검증.
   - `EnemyBullet_Ignores_Enemy_Collision_NoSelfDamage`: IDamageable 파이프라인에서 적 자폭 및 팀킬 방지 검증.
   - `PlayerHealth_Ignores_PlayerBullet_Collision`: PlayerHealth 컴포넌트 단위에서 플레이어 탄환 충돌 무시 검증.
   - `EnemyBase_Ignores_EnemyBullet_Collision`: EnemyBase 컴포넌트 단위에서 적 탄환 충돌 무시 검증.

### 4.2 컴파일 및 정적 분석 결과
- `dotnet build`: 오류 0개 (무결성 검증 완료)
- JetBrains Rider IDE Diagnostics: 오류 0건, 경고 0건

---

## 5. 커밋 및 브랜치 정보
- **브랜치**: `fix/issue-19-bullet-self-damage`
- **커밋 해시**: `6a37b76`
- **커밋 메시지**: `[fix] : Issue #19 탄환 발사 시 발사자 셀프 충돌 및 자폭 버그 수정`
