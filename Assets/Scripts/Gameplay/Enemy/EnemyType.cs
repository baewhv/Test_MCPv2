using System;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 갤러그 적 기체의 기본 유형을 정의하는 열거형입니다.
    /// </summary>
    public enum EnemyType
    {
        Zako = 0,       // 자코 (드론 / 청색 곤충, 기본 HP 1)
        Goei = 1,       // 고에이 (가드 / 적색 나비, 기본 HP 1)
        BossGalaga = 2, // 보스 갤러그 (커맨더 / 녹-청 대형기, 기본 HP 2)
        Boss = 2        // BossGalaga alias
    }

    /// <summary>
    /// 적 기체의 라이프사이클 및 행동 상태 머신 열거형입니다.
    /// </summary>
    public enum EnemyState
    {
        Spawning,   // 스폰 대기 / 초기화
        Entering,   // 편대 진입 궤적 비행 중
        Formation,  // 편대 슬롯 안착 및 호흡(Hovering) 중
        Diving,     // 플레이어를 향한 급강하 공격 비행 중
        Returning,  // 화면 하단 이탈 후 상단 재진입 복귀 중
        Dead        // 격파/사망 상태
    }
}
