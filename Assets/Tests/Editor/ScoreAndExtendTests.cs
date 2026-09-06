using NUnit.Framework;
using UnityEngine;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;
using Galaga.Gameplay.Score;

namespace Galaga.Tests
{
    [TestFixture]
    public class ScoreAndExtendTests
    {
        private GameObject _scoreManagerObject;
        private ScoreManager _scoreManager;
        private GameObject _playerObject;
        private PlayerHealth _playerHealth;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("TestPlayer");
            _playerHealth = _playerObject.AddComponent<PlayerHealth>();
            _playerHealth.Initialize(3);

            _scoreManagerObject = new GameObject("TestScoreManager");
            _scoreManager = _scoreManagerObject.AddComponent<ScoreManager>();
            _scoreManager.Initialize(20000, _playerHealth);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scoreManagerObject != null)
            {
                Object.DestroyImmediate(_scoreManagerObject);
            }

            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }
        }

        [Test]
        public void ScoreCalculation_Zako_ReturnsCorrectScores()
        {
            int stayScore = _scoreManager.CalculateEnemyScore(EnemyType.Zako, false);
            int diveScore = _scoreManager.CalculateEnemyScore(EnemyType.Zako, true);

            Assert.AreEqual(50, stayScore, "자코 대기 점수는 50점이어야 합니다.");
            Assert.AreEqual(100, diveScore, "자코 다이브 점수는 100점이어야 합니다.");
        }

        [Test]
        public void ScoreCalculation_Goei_ReturnsCorrectScores()
        {
            int stayScore = _scoreManager.CalculateEnemyScore(EnemyType.Goei, false);
            int diveScore = _scoreManager.CalculateEnemyScore(EnemyType.Goei, true);

            Assert.AreEqual(80, stayScore, "고에이 대기 점수는 80점이어야 합니다.");
            Assert.AreEqual(160, diveScore, "고에이 다이브 점수는 160점이어야 합니다.");
        }

        [Test]
        public void ScoreCalculation_BossGalaga_ReturnsCorrectScoresByStateAndEscorts()
        {
            int stayScore = _scoreManager.CalculateEnemyScore(EnemyType.BossGalaga, false, 0);
            int soloDiveScore = _scoreManager.CalculateEnemyScore(EnemyType.BossGalaga, true, 0);
            int escort1DiveScore = _scoreManager.CalculateEnemyScore(EnemyType.BossGalaga, true, 1);
            int escort2DiveScore = _scoreManager.CalculateEnemyScore(EnemyType.BossGalaga, true, 2);

            Assert.AreEqual(150, stayScore, "보스 대기 점수는 150점이어야 합니다.");
            Assert.AreEqual(400, soloDiveScore, "보스 단독 다이브 점수는 400점이어야 합니다.");
            Assert.AreEqual(800, escort1DiveScore, "보스 호위 1기 다이브 점수는 800점이어야 합니다.");
            Assert.AreEqual(1600, escort2DiveScore, "보스 호위 2기 다이브 점수는 1600점이어야 합니다.");
        }

        [Test]
        public void AddScore_FiresOnScoreChangedEvent()
        {
            int recordedScore = 0;
            _scoreManager.OnScoreChanged += (score) => { recordedScore = score; };

            _scoreManager.AddScore(500);

            Assert.AreEqual(500, _scoreManager.CurrentScore);
            Assert.AreEqual(500, recordedScore);
        }

        [Test]
        public void AddScore_ExceedingHighScore_UpdatesHighScoreAndFiresEvent()
        {
            int recordedHighScore = 0;
            _scoreManager.OnHighScoreChanged += (highScore) => { recordedHighScore = highScore; };

            // 초기 최고 점수는 20,000점
            _scoreManager.AddScore(15000);
            Assert.AreEqual(20000, _scoreManager.HighScore);
            Assert.AreEqual(0, recordedHighScore);

            // 20,000점을 초과하여 25,000점이 되는 경우
            _scoreManager.AddScore(10000);
            Assert.AreEqual(25000, _scoreManager.HighScore);
            Assert.AreEqual(25000, recordedHighScore);
        }

        [Test]
        public void ExtendLife_TriggersAtFirst20000Points_AndGrantsLife()
        {
            int extendCount = 0;
            int extendScoreAtEvent = 0;
            _scoreManager.OnExtendLife += (score) =>
            {
                extendCount++;
                extendScoreAtEvent = score;
            };

            int initialLives = _playerHealth.CurrentLives; // 3

            // 19,900점 가산 시 익스텐드 미발동
            _scoreManager.AddScore(19900);
            Assert.AreEqual(0, extendCount);
            Assert.AreEqual(initialLives, _playerHealth.CurrentLives);
            Assert.IsFalse(_scoreManager.HasFirstExtend);

            // 추가 100점 가산으로 정확히 20,000점 도달 ➔ 1차 익스텐드 발동
            _scoreManager.AddScore(100);
            Assert.AreEqual(1, extendCount);
            Assert.AreEqual(20000, extendScoreAtEvent);
            Assert.AreEqual(initialLives + 1, _playerHealth.CurrentLives); // 4
            Assert.IsTrue(_scoreManager.HasFirstExtend);
            Assert.AreEqual(70000, _scoreManager.NextExtendScore);
        }

        [Test]
        public void ExtendLife_TriggersRepeatedlyAtEvery70000Points()
        {
            int extendCount = 0;
            _scoreManager.OnExtendLife += (score) => { extendCount++; };

            // 1차 20,000점 도달 (익스텐드 1회)
            _scoreManager.AddScore(20000);
            Assert.AreEqual(1, extendCount);
            Assert.AreEqual(4, _playerHealth.CurrentLives);
            Assert.AreEqual(70000, _scoreManager.NextExtendScore);

            // 70,000점 도달 (익스텐드 2회)
            _scoreManager.AddScore(50000);
            Assert.AreEqual(2, extendCount);
            Assert.AreEqual(5, _playerHealth.CurrentLives);
            Assert.AreEqual(140000, _scoreManager.NextExtendScore);

            // 140,000점 도달 (익스텐드 3회, 최대 잔기 6기 도달)
            _scoreManager.AddScore(70000);
            Assert.AreEqual(3, extendCount);
            Assert.AreEqual(6, _playerHealth.CurrentLives);
            Assert.AreEqual(210000, _scoreManager.NextExtendScore);
        }

        [Test]
        public void ExtendLife_BulkScoreAddition_TriggersMultipleExtendsCorrectly()
        {
            int extendCount = 0;
            _scoreManager.OnExtendLife += (score) => { extendCount++; };

            // 단번에 150,000점 가산 시 (20,000점 달성 + 70,000점 달성 + 140,000점 달성 = 총 3회 익스텐드)
            _scoreManager.AddScore(150000);

            Assert.AreEqual(3, extendCount);
            Assert.AreEqual(6, _playerHealth.CurrentLives);
            Assert.AreEqual(210000, _scoreManager.NextExtendScore);
        }

        [Test]
        public void EnemyBase_Die_AddsScoreToScoreManager()
        {
            // ScoreManager.Instance는 SetUp에서 등록됨
            GameObject enemyObj = new GameObject("TestEnemy");
            enemyObj.AddComponent<BezierPathFollower>();
            EnemyBase enemy = enemyObj.AddComponent<EnemyBase>();

            // ScriptableObject 없이 기본 Zako 동작 검증
            // 대기 상태 격파
            enemy.SetState(EnemyState.Formation);
            enemy.Die();

            Assert.AreEqual(50, _scoreManager.CurrentScore, "대기 자코 격파 시 50점 가산");

            // 새로운 다이빙 보스 기체 테스트 (EnemyType.BossGalaga 데이터 주입)
            GameObject bossObj = new GameObject("TestBoss");
            bossObj.AddComponent<BezierPathFollower>();
            EnemyBase boss = bossObj.AddComponent<EnemyBase>();
            EnemyDataSO bossData = ScriptableObject.CreateInstance<EnemyDataSO>();
            bossData.Initialize(EnemyType.BossGalaga, "BossGalaga", 2, 150, 400, 10f, Color.green, Color.blue, Color.white);
            boss.Initialize(bossData);

            boss.SetState(EnemyState.Diving);
            boss.EscortCount = 2;
            boss.Die();

            // 50점 + 1600점 = 1650점
            Assert.AreEqual(1650, _scoreManager.CurrentScore, "호위 2기 다이빙 보스 격파 시 1600점 가산");

            Object.DestroyImmediate(enemyObj);
            Object.DestroyImmediate(bossObj);
            Object.DestroyImmediate(bossData);
        }

        [Test]
        public void ResetScore_ResetsCurrentScoreAndExtendTargets()
        {
            _scoreManager.AddScore(25000);
            Assert.AreEqual(25000, _scoreManager.CurrentScore);
            Assert.IsTrue(_scoreManager.HasFirstExtend);

            _scoreManager.ResetScore();

            Assert.AreEqual(0, _scoreManager.CurrentScore);
            Assert.IsFalse(_scoreManager.HasFirstExtend);
            Assert.AreEqual(20000, _scoreManager.NextExtendScore);
        }
    }
}
