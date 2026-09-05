using System;
using UnityEngine;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Gameplay.Score
{
    /// <summary>
    /// 플레이어 점수 계산, 하이스코어 갱신, 적 기체 유형별 차등 점수 부여 및 보너스 잔기(Extend Life) 시스템을 총괄하는 매니저입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScoreManager : MonoBehaviour
    {
        // -------------------------------------------------------------
        // 1. 인스펙터 직렬화 필드 (Serialized Fields: [SerializeField] private)
        // -------------------------------------------------------------
        [Header("Extend Score Settings")]
        [Tooltip("1차 익스텐드 달성 점수 (기본 20,000점)")]
        [SerializeField] private int _firstExtendScore = 20000;

        [Tooltip("2차 및 이후 반복 익스텐드 간격 점수 (기본 70,000점)")]
        [SerializeField] private int _repeatExtendInterval = 70000;

        [Tooltip("초기 하이스코어 (기본 20,000점)")]
        [SerializeField] private int _initialHighScore = 20000;

        [Header("References")]
        [Tooltip("잔기 추가를 위한 PlayerHealth 컴포넌트 참조")]
        [SerializeField] private PlayerHealth _playerHealth;

        // -------------------------------------------------------------
        // 2. 런타임 상태 필드 (Runtime State Fields)
        // -------------------------------------------------------------
        private int _currentScore = 0;
        private int _highScore = 20000;
        private int _nextExtendScore = 20000;
        private bool _hasFirstExtend = false;

        // -------------------------------------------------------------
        // 3. 프로퍼티 (Properties)
        // -------------------------------------------------------------
        public static ScoreManager Instance { get; private set; }

        public int CurrentScore => _currentScore;
        public int HighScore => _highScore;
        public int NextExtendScore => _nextExtendScore;
        public int FirstExtendScore => _firstExtendScore;
        public int RepeatExtendInterval => _repeatExtendInterval;
        public bool HasFirstExtend => _hasFirstExtend;

        public PlayerHealth PlayerHealth
        {
            get => _playerHealth;
            set => _playerHealth = value;
        }

        // -------------------------------------------------------------
        // 4. C# 이벤트 (Events / Actions)
        // -------------------------------------------------------------
        /// <summary>
        /// 현재 점수 변경 시 발행되는 이벤트 (현재 점수 전달)
        /// </summary>
        public event Action<int> OnScoreChanged;

        /// <summary>
        /// 최고 점수 변경 시 발행되는 이벤트 (최고 점수 전달)
        /// </summary>
        public event Action<int> OnHighScoreChanged;

        /// <summary>
        /// 익스텐드(보너스 잔기 획득) 조건 도달 시 발행되는 이벤트 (획득 시점의 현재 점수 전달)
        /// </summary>
        public event Action<int> OnExtendLife;

        // -------------------------------------------------------------
        // 5. 유니티 생명주기 메서드 (Lifecycle Methods)
        // -------------------------------------------------------------
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Initialize(_initialHighScore, _playerHealth);
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            OnScoreChanged = null;
            OnHighScoreChanged = null;
            OnExtendLife = null;
        }

        // -------------------------------------------------------------
        // 6. 초기화 및 리셋 (Initialization & Reset)
        // -------------------------------------------------------------
        /// <summary>
        /// 스코어 매니저의 상태를 명시적으로 초기화합니다 (런타임 및 단위 테스트용).
        /// </summary>
        /// <param name="initialHighScore">초기 하이스코어</param>
        /// <param name="playerHealth">연동할 PlayerHealth 참조</param>
        public void Initialize(int initialHighScore = 20000, PlayerHealth playerHealth = null)
        {
            Instance = this;
            _initialHighScore = initialHighScore;
            _highScore = initialHighScore;
            _currentScore = 0;
            _nextExtendScore = _firstExtendScore;
            _hasFirstExtend = false;

            if (playerHealth != null)
            {
                _playerHealth = playerHealth;
            }
            else if (_playerHealth == null)
            {
                _playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            OnScoreChanged?.Invoke(_currentScore);
            OnHighScoreChanged?.Invoke(_highScore);
        }

        /// <summary>
        /// 게임 재시작 시 현재 점수를 0으로 리셋하고 익스텐드 목표를 초기화합니다.
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            _nextExtendScore = _firstExtendScore;
            _hasFirstExtend = false;
            OnScoreChanged?.Invoke(_currentScore);
        }

        // -------------------------------------------------------------
        // 7. 점수 산정 및 가산 공개 메서드 (Scoring Public APIs)
        // -------------------------------------------------------------
        /// <summary>
        /// 적 기체 유형과 비행/호위 상태에 따른 격파 점수를 계산합니다.
        /// </summary>
        /// <param name="type">적 기체 유형 (Zako, Goei, BossGalaga)</param>
        /// <param name="isDiving">급강하/비행 여부</param>
        /// <param name="escortCount">보스 동반 호위기 수 (0: 단독, 1: 1기 호위, 2: 2기 호위)</param>
        /// <returns>산정된 점수</returns>
        public int CalculateEnemyScore(EnemyType type, bool isDiving, int escortCount = 0)
        {
            switch (type)
            {
                case EnemyType.Zako:
                    return isDiving ? 100 : 50;

                case EnemyType.Goei:
                    return isDiving ? 160 : 80;

                case EnemyType.BossGalaga:
                    if (!isDiving)
                    {
                        return 150;
                    }
                    else
                    {
                        if (escortCount >= 2)
                        {
                            return 1600;
                        }
                        else if (escortCount == 1)
                        {
                            return 800;
                        }
                        else
                        {
                            return 400;
                        }
                    }

                default:
                    return isDiving ? 100 : 50;
            }
        }

        /// <summary>
        /// 지정된 점수를 현재 점수에 가산하고 하이스코어 갱신 및 익스텐드를 검사합니다.
        /// </summary>
        /// <param name="points">가산할 점수</param>
        public void AddScore(int points)
        {
            if (points <= 0)
            {
                return;
            }

            _currentScore += points;
            OnScoreChanged?.Invoke(_currentScore);

            if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                OnHighScoreChanged?.Invoke(_highScore);
            }

            CheckExtend();
        }

        /// <summary>
        /// 적 기체 격파 정보를 기반으로 점수를 계산하여 즉시 가산합니다.
        /// </summary>
        /// <param name="type">적 기체 유형</param>
        /// <param name="isDiving">급강하/비행 여부</param>
        /// <param name="escortCount">보스 동반 호위기 수</param>
        public void AddEnemyScore(EnemyType type, bool isDiving, int escortCount = 0)
        {
            int points = CalculateEnemyScore(type, isDiving, escortCount);
            AddScore(points);
        }

        // -------------------------------------------------------------
        // 8. 익스텐드 내부 처리 로직 (Extend Life Logic)
        // -------------------------------------------------------------
        private void CheckExtend()
        {
            if (!_hasFirstExtend)
            {
                if (_currentScore >= _firstExtendScore)
                {
                    _hasFirstExtend = true;
                    _nextExtendScore = _repeatExtendInterval;
                    TriggerExtend();

                    // 1차 달성 후 즉시 2차 이상 구간에 도달한 경우 (단일 점수 대량 가산 대비)
                    while (_currentScore >= _nextExtendScore)
                    {
                        _nextExtendScore += _repeatExtendInterval;
                        TriggerExtend();
                    }
                }
            }
            else
            {
                while (_currentScore >= _nextExtendScore)
                {
                    _nextExtendScore += _repeatExtendInterval;
                    TriggerExtend();
                }
            }
        }

        private void TriggerExtend()
        {
            OnExtendLife?.Invoke(_currentScore);

            if (_playerHealth != null)
            {
                _playerHealth.AddLife(1);
            }
        }
    }
}
