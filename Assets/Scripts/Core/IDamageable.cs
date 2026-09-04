/// <summary>
/// 피격 및 데미지 처리가 가능한 모든 게임 엔티티(적 기체, 플레이어 기체 등)를 위한 공용 인터페이스입니다.
/// 발사체 및 충돌 처리 시스템은 구체 클래스 대신 본 인터페이스를 참조하여 결합도를 낮추고 OCP(개방-폐쇄 원칙)를 만족합니다.
/// </summary>
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
