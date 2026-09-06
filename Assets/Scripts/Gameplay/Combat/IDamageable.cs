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
