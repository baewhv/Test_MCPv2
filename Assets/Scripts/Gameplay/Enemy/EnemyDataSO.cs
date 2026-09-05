using UnityEngine;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 적 기체의 스펙 및 데이터(체력, 점수, 이동속도, 색상 등)를 정의하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Enemy_Data", menuName = "Galaga/Enemy Data", order = 1)]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("적 기체 유형")]
        [SerializeField] private EnemyType _enemyType = EnemyType.Zako;

        [Tooltip("적 기체 명칭")]
        [SerializeField] private string _enemyName = "Zako";

        [Header("Stats")]
        [Tooltip("최대 체력 (HP)")]
        [SerializeField] private int _maxHP = 1;

        [Tooltip("편대 대기 상태 격파 점수")]
        [SerializeField] private int _scoreStay = 50;

        [Tooltip("비행/급강하 상태 격파 점수")]
        [SerializeField] private int _scoreDive = 100;

        [Tooltip("기본 비행 속도 (units/sec)")]
        [SerializeField] private float _moveSpeed = 10f;

        [Header("Visual & FX")]
        [Tooltip("기본 외형 색상")]
        [SerializeField] private Color _normalColor = Color.blue;

        [Tooltip("피격 손상 상태 색상 (보스 갤러그 1타 피격 시 등)")]
        [SerializeField] private Color _damagedColor = Color.cyan;

        [Tooltip("피격 순간 플래시 반전 색상")]
        [SerializeField] private Color _flashColor = Color.white;

        [Tooltip("피격 플래시 지속 시간(초) - 시인성 강화를 위해 0.15초로 상향")]
        [SerializeField] private float _flashDuration = 0.15f;

        public EnemyType Type => _enemyType;
        public string EnemyName => _enemyName;
        public int MaxHP => _maxHP;
        public int ScoreStay => _scoreStay;
        public int ScoreDive => _scoreDive;
        public float MoveSpeed => _moveSpeed;
        public Color NormalColor => _normalColor;
        public Color DamagedColor => _damagedColor;
        public Color FlashColor => _flashColor;
        public float FlashDuration => _flashDuration;

        /// <summary>
        /// 런타임 또는 단위 테스트에서 스펙을 동적으로 설정합니다.
        /// </summary>
        public void Initialize(
            EnemyType type,
            string enemyName,
            int maxHp,
            int scoreStay,
            int scoreDive,
            float moveSpeed,
            Color normalColor,
            Color damagedColor,
            Color flashColor,
            float flashDuration = 0.15f)
        {
            _enemyType = type;
            _enemyName = enemyName;
            _maxHP = Mathf.Max(1, maxHp);
            _scoreStay = scoreStay;
            _scoreDive = scoreDive;
            _moveSpeed = Mathf.Max(0f, moveSpeed);
            _normalColor = normalColor;
            _damagedColor = damagedColor;
            _flashColor = flashColor;
            _flashDuration = Mathf.Max(0.01f, flashDuration);
        }
    }
}
