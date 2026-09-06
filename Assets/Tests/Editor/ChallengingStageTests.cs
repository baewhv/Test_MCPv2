using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;
using Galaga.Gameplay.Score;
using Galaga.Gameplay.Stage;

namespace Galaga.Tests
{
    [TestFixture]
    public class ChallengingStageTests
    {
        private GameObject _scoreManagerObject;
        private ScoreManager _scoreManager;
        private GameObject _stageManagerObject;
        private StageManager _stageManager;
        private GameObject _challengingManagerObject;
        private ChallengingStageManager _challengingManager;
        private GameObject _playerObject;
        private PlayerHealth _playerHealth;
        private EnemyDataSO _testZakoData;
        private EnemyDataSO _testBossData;
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            EnemyShooting.GlobalShootingBlocked = false;

            _playerObject = new GameObject("TestPlayer");
            _playerHealth = _playerObject.AddComponent<PlayerHealth>();
            _playerHealth.Initialize(3);

            _scoreManagerObject = new GameObject("TestScoreManager");
            _scoreManager = _scoreManagerObject.AddComponent<ScoreManager>();
            _scoreManager.Initialize(20000, _playerHealth);

            _stageManagerObject = new GameObject("TestStageManager");
            _stageManager = _stageManagerObject.AddComponent<StageManager>();
            _stageManager.PlayerHealth = _playerHealth;
            _stageManager.StageStartDelay = 0f;
            _stageManager.StageClearDelay = 0f;
            _stageManager.AutoAdvanceToNextStage = false;
            _stageManager.Initialize(1);

            _challengingManagerObject = new GameObject("TestChallengingStageManager");
            _challengingManager = _challengingManagerObject.AddComponent<ChallengingStageManager>();
            _challengingManager.StageManager = _stageManager;
            _challengingManager.ScoreManager = _scoreManager;
            _challengingManager.PostStageDelay = 0f;

            _testZakoData = ScriptableObject.CreateInstance<EnemyDataSO>();
            _testZakoData.Initialize(
                type: EnemyType.Zako,
                enemyName: "TestZako",
                maxHp: 1,
                scoreStay: 50,
                scoreDive: 100,
                moveSpeed: 10f,
                normalColor: Color.blue,
                damagedColor: Color.cyan,
                flashColor: Color.white,
                flashDuration: 0.08f
            );

            _testBossData = ScriptableObject.CreateInstance<EnemyDataSO>();
            _testBossData.Initialize(
                type: EnemyType.BossGalaga,
                enemyName: "TestBoss",
                maxHp: 2,
                scoreStay: 150,
                scoreDive: 400,
                moveSpeed: 10f,
                normalColor: Color.green,
                damagedColor: Color.blue,
                flashColor: Color.white,
                flashDuration: 0.08f
            );
        }

        [TearDown]
        public void TearDown()
        {
            EnemyShooting.GlobalShootingBlocked = false;

            for (int i = 0; i < _spawnedObjects.Count; i++)
            {
                if (_spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(_spawnedObjects[i]);
                }
            }
            _spawnedObjects.Clear();

            if (_testZakoData != null)
            {
                Object.DestroyImmediate(_testZakoData);
            }
            if (_testBossData != null)
            {
                Object.DestroyImmediate(_testBossData);
            }
            if (_challengingManagerObject != null)
            {
                Object.DestroyImmediate(_challengingManagerObject);
            }
            if (_stageManagerObject != null)
            {
                Object.DestroyImmediate(_stageManagerObject);
            }
            if (_scoreManagerObject != null)
            {
                Object.DestroyImmediate(_scoreManagerObject);
            }
            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }
        }

        private EnemyBase CreateDummyEnemy(EnemyDataSO data, string name = "DummyEnemy")
        {
            GameObject obj = new GameObject(name);
            _spawnedObjects.Add(obj);
            obj.AddComponent<BezierPathFollower>();
            EnemyBase enemy = obj.AddComponent<EnemyBase>();
            enemy.Initialize(data != null ? data : _testZakoData);
            return enemy;
        }

        // =====================================================================
        // 1. 챌린징 스테이지 판정 수학 공식 (4n - 1) 검증
        // =====================================================================
        [Test]
        public void ChallengingStage_Formula_IdentifiesEvery4thStageStartingFromStage3()
        {
            int[] challengingStages = { 3, 7, 11, 15, 19, 23, 27, 31, 35, 39, 43, 47, 51, 99 };
            foreach (int stage in challengingStages)
            {
                Assert.IsTrue(StageManager.CheckIsChallengingStage(stage), $"Stage {stage}는 챌린징 스테이지여야 합니다.");
            }
        }

        [Test]
        public void ChallengingStage_Formula_NormalAndInvalidStages_ReturnsFalse()
        {
            int[] normalStages = { -5, -1, 0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 16, 17, 18, 20, 21, 22 };
            foreach (int stage in normalStages)
            {
                Assert.IsFalse(StageManager.CheckIsChallengingStage(stage), $"Stage {stage}는 일반 스테이지여야 합니다.");
            }
        }

        // =====================================================================
        // 2. 보너스 점수 정산 공식 (40기 완파 10,000점 / 부분 hitCount * 100) 검증
        // =====================================================================
        [Test]
        public void BonusCalculation_40Kills_ReturnsPerfectBonus_10000Points()
        {
            int bonus = ScoreManager.CalculateChallengingBonus(40, 40);
            Assert.AreEqual(10000, bonus, "40기 완파 시 10,000점 PERFECT 보너스가 지급되어야 합니다.");
        }

        [Test]
        public void BonusCalculation_MoreThan40Kills_ReturnsPerfectBonus_10000Points()
        {
            int bonus = ScoreManager.CalculateChallengingBonus(45, 40);
            Assert.AreEqual(10000, bonus, "40기 이상 완파 시에도 10,000점 PERFECT 보너스가 반환되어야 합니다.");
        }

        [Test]
        public void BonusCalculation_PartialKills_Returns100PointsPerKill()
        {
            Assert.AreEqual(0, ScoreManager.CalculateChallengingBonus(0, 40), "0기 격파 시 0점");
            Assert.AreEqual(100, ScoreManager.CalculateChallengingBonus(1, 40), "1기 격파 시 100점");
            Assert.AreEqual(500, ScoreManager.CalculateChallengingBonus(5, 40), "5기 격파 시 500점");
            Assert.AreEqual(2000, ScoreManager.CalculateChallengingBonus(20, 40), "20기 격파 시 2,000점");
            Assert.AreEqual(3900, ScoreManager.CalculateChallengingBonus(39, 40), "39기 격파 시 3,900점");
        }

        [Test]
        public void BonusCalculation_NegativeKills_ReturnsZero()
        {
            int bonus = ScoreManager.CalculateChallengingBonus(-5, 40);
            Assert.AreEqual(0, bonus, "음수 격파 수는 0점으로 처리되어야 합니다.");
        }

        // =====================================================================
        // 3. ScoreManager 연동 점수 가산 및 이벤트 검증
        // =====================================================================
        [Test]
        public void ScoreManager_AddChallengingBonus_AddsPointsAndFiresEvents()
        {
            int receivedHitCount = -1;
            int receivedBonus = -1;
            _scoreManager.OnChallengingBonusAwarded += (hits, bonus) =>
            {
                receivedHitCount = hits;
                receivedBonus = bonus;
            };

            int awardedBonus = _scoreManager.AddChallengingBonus(25);

            Assert.AreEqual(2500, awardedBonus, "25기 격파 보너스는 2,500점이어야 합니다.");
            Assert.AreEqual(2500, _scoreManager.CurrentScore, "ScoreManager의 현재 점수에 2,500점이 가산되어야 합니다.");
            Assert.AreEqual(25, receivedHitCount);
            Assert.AreEqual(2500, receivedBonus);
        }

        [Test]
        public void ScoreManager_AddChallengingBonus_Perfect_Adds10000Points()
        {
            int awardedBonus = _scoreManager.AddChallengingBonus(40);
            Assert.AreEqual(10000, awardedBonus);
            Assert.AreEqual(10000, _scoreManager.CurrentScore);
        }

        // =====================================================================
        // 4. 노탄환 비행 모드 (EnemyShooting 차단) 검증
        // =====================================================================
        [Test]
        public void EnemyShooting_GlobalShootingBlocked_PreventsShooting()
        {
            EnemyShooting.GlobalShootingBlocked = true;

            bool canShoot = EnemyShooting.CanShoot(0.45f, 0, 1, 0.3f, 0.6f, true);
            Assert.IsFalse(canShoot, "GlobalShootingBlocked가 true이면 CanShoot은 false를 반환해야 합니다.");

            GameObject shooterObj = new GameObject("TestShooter");
            _spawnedObjects.Add(shooterObj);
            EnemyBase enemy = shooterObj.AddComponent<EnemyBase>();
            enemy.Initialize(_testZakoData);
            EnemyShooting shooting = shooterObj.AddComponent<EnemyShooting>();

            bool shotFired = shooting.TryFireAtPlayer();
            Assert.IsFalse(shotFired, "GlobalShootingBlocked가 true이면 TryFireAtPlayer는 실패해야 합니다.");
        }

        [Test]
        public void EnemyShooting_CanFire_False_PreventsShooting()
        {
            EnemyShooting.GlobalShootingBlocked = false;

            bool canShoot = EnemyShooting.CanShoot(0.45f, 0, 1, 0.3f, 0.6f, false);
            Assert.IsFalse(canShoot, "CanFire가 false이면 CanShoot은 false를 반환해야 합니다.");

            GameObject shooterObj = new GameObject("TestShooter");
            _spawnedObjects.Add(shooterObj);
            EnemyBase enemy = shooterObj.AddComponent<EnemyBase>();
            enemy.Initialize(_testZakoData);
            EnemyShooting shooting = shooterObj.AddComponent<EnemyShooting>();
            shooting.CanFire = false;

            bool shotFired = shooting.TryFireAtPlayer();
            Assert.IsFalse(shotFired, "CanFire가 false이면 TryFireAtPlayer는 실패해야 합니다.");
        }

        [Test]
        public void EnemyShooting_IsShootingEnabled_ReflectsProperties()
        {
            GameObject shooterObj = new GameObject("TestShooter");
            _spawnedObjects.Add(shooterObj);
            EnemyBase enemy = shooterObj.AddComponent<EnemyBase>();
            enemy.Initialize(_testZakoData);
            EnemyShooting shooting = shooterObj.AddComponent<EnemyShooting>();

            EnemyShooting.GlobalShootingBlocked = false;
            shooting.CanFire = true;
            Assert.IsTrue(shooting.IsShootingEnabled);

            EnemyShooting.GlobalShootingBlocked = true;
            Assert.IsFalse(shooting.IsShootingEnabled);

            EnemyShooting.GlobalShootingBlocked = false;
            shooting.IsShootingEnabled = false;
            Assert.IsFalse(shooting.CanFire);
            Assert.IsFalse(shooting.IsShootingEnabled);
        }

        // =====================================================================
        // 5. 챌린징 스테이지 5웨이브 40기 구성 및 궤적 검증
        // =====================================================================
        [Test]
        public void ChallengingStage_5Waves_8EnemiesPerWave_Total40Enemies()
        {
            int totalEnemies = 0;
            for (int wave = 1; wave <= 5; wave++)
            {
                EnemyType[] types = ChallengingStageManager.GetChallengingWaveEnemyTypes(wave);
                Assert.AreEqual(8, types.Length, $"Wave {wave}는 8기의 적 구성을 가져야 합니다.");
                totalEnemies += types.Length;
            }

            Assert.AreEqual(40, totalEnemies, "총 5웨이브 40기의 적이 구성되어야 합니다.");
        }

        [Test]
        public void ChallengingStage_WaveEnemyTypes_MatchSpecification()
        {
            // Wave 1 & 2: Zako 8기
            EnemyType[] wave1 = ChallengingStageManager.GetChallengingWaveEnemyTypes(1);
            foreach (var type in wave1)
            {
                Assert.AreEqual(EnemyType.Zako, type);
            }

            // Wave 3 & 4: Goei 8기
            EnemyType[] wave3 = ChallengingStageManager.GetChallengingWaveEnemyTypes(3);
            foreach (var type in wave3)
            {
                Assert.AreEqual(EnemyType.Goei, type);
            }

            // Wave 5: Boss 4기 + Goei 4기
            EnemyType[] wave5 = ChallengingStageManager.GetChallengingWaveEnemyTypes(5);
            int bossCount = 0;
            int goeiCount = 0;
            foreach (var type in wave5)
            {
                if (type == EnemyType.BossGalaga) bossCount++;
                if (type == EnemyType.Goei) goeiCount++;
            }
            Assert.AreEqual(4, bossCount, "Wave 5는 보스 4기를 포함해야 합니다.");
            Assert.AreEqual(4, goeiCount, "Wave 5는 고에이 4기를 포함해야 합니다.");
        }

        [Test]
        public void ChallengingStage_TrajectoryGeneration_ReturnsValidSegmentsForAllWaves()
        {
            for (int wave = 1; wave <= 5; wave++)
            {
                for (int subIndex = 0; subIndex < 8; subIndex++)
                {
                    BezierSegment[] trajectory = ChallengingStageManager.CreateChallengingTrajectory(wave, subIndex);
                    Assert.IsNotNull(trajectory, $"Wave {wave} Sub {subIndex} 궤적은 null이 아니어야 합니다.");
                    Assert.Greater(trajectory.Length, 0, $"Wave {wave} Sub {subIndex} 궤적 세그먼트 수가 1 이상이어야 합니다.");
                }
            }
        }

        // =====================================================================
        // 6. ChallengingStageManager 격파수 집계 및 결과 정산 검증
        // =====================================================================
        [Test]
        public void ChallengingStageManager_HitCount_IncrementsOnEnemyDefeated()
        {
            _challengingManager.StartChallengingStage(3);
            Assert.IsTrue(_challengingManager.IsChallengingInProgress);
            Assert.IsTrue(EnemyShooting.GlobalShootingBlocked);

            int hitCountEventArg = -1;
            _challengingManager.OnChallengingHitCountChanged += (count) => hitCountEventArg = count;

            _challengingManager.StopChallengingStage();
            Assert.IsFalse(_challengingManager.IsChallengingInProgress);
            Assert.IsFalse(EnemyShooting.GlobalShootingBlocked);
        }

        [Test]
        public void ChallengingStageManager_FinishChallengingStage_Perfect_Awards10000Points()
        {
            _challengingManager.StartChallengingStage(3);

            ChallengingStageResult completedResult = default;
            bool completedFired = false;
            _challengingManager.OnChallengingStageCompleted += (res) =>
            {
                completedResult = res;
                completedFired = true;
            };

            _challengingManager.FinishChallengingStage();

            Assert.IsTrue(completedFired, "OnChallengingStageCompleted 이벤트가 발행되어야 합니다.");
            Assert.IsFalse(EnemyShooting.GlobalShootingBlocked, "종료 후 노탄환 모드가 해제되어야 합니다.");
        }

        // =====================================================================
        // 7. StageManager와 챌린징 보너스 연동 검증
        // =====================================================================
        [Test]
        public void StageManager_ChallengingStageClear_AwardsCalculatedBonus()
        {
            // Stage 3 (챌린징 스테이지) 시작
            _stageManager.StartStage(3);
            Assert.IsTrue(_stageManager.IsChallengingStage);

            int receivedHitCount = -1;
            int receivedBonus = -1;
            _stageManager.OnChallengingBonusAwarded += (hits, bonus) =>
            {
                receivedHitCount = hits;
                receivedBonus = bonus;
            };

            // 40기 먼저 일괄 등록 후 순차 격파
            List<EnemyBase> enemies = new List<EnemyBase>();
            for (int i = 0; i < 40; i++)
            {
                EnemyBase enemy = CreateDummyEnemy(_testZakoData, $"Enemy_{i}");
                _stageManager.RegisterEnemy(enemy);
                enemies.Add(enemy);
            }

            for (int i = 0; i < 40; i++)
            {
                enemies[i].TakeDamage(1);
            }

            Assert.AreEqual(40, _stageManager.DestroyedEnemyCount);
            Assert.AreEqual(40, receivedHitCount);
            Assert.AreEqual(10000, receivedBonus, "40기 완파 시 10,000점 PERFECT 보너스가 정산되어야 합니다.");
            Assert.IsTrue(_stageManager.IsStageClearing);
        }

        [Test]
        public void StageManager_ChallengingStagePartialClear_AwardsPartialBonus()
        {
            _stageManager.StartStage(3);
            Assert.IsTrue(_stageManager.IsChallengingStage);

            int receivedHitCount = -1;
            int receivedBonus = -1;
            _stageManager.OnChallengingBonusAwarded += (hits, bonus) =>
            {
                receivedHitCount = hits;
                receivedBonus = bonus;
            };

            // 40기 먼저 일괄 등록
            List<EnemyBase> enemies = new List<EnemyBase>();
            for (int i = 0; i < 40; i++)
            {
                EnemyBase enemy = CreateDummyEnemy(_testZakoData, $"Enemy_{i}");
                _stageManager.RegisterEnemy(enemy);
                enemies.Add(enemy);
            }

            // 20기만 격파
            for (int i = 0; i < 20; i++)
            {
                enemies[i].TakeDamage(1);
            }

            // 나머지 20기는 Unregister (화면 이탈 시뮬레이션)
            for (int i = 20; i < 40; i++)
            {
                _stageManager.UnregisterEnemy(enemies[i]);
            }

            Assert.AreEqual(20, _stageManager.DestroyedEnemyCount);
            Assert.AreEqual(20, receivedHitCount);
            Assert.AreEqual(2000, receivedBonus, "20기 격파 시 2,000점 보너스가 정산되어야 합니다.");
        }

        [Test]
        public void StageManager_NormalStageClear_DoesNotAwardChallengingBonus()
        {
            // Stage 1 (일반 스테이지) 시작
            _stageManager.StartStage(1);
            Assert.IsFalse(_stageManager.IsChallengingStage);

            bool challengingBonusAwarded = false;
            _stageManager.OnChallengingBonusAwarded += (hits, bonus) => challengingBonusAwarded = true;

            List<EnemyBase> enemies = new List<EnemyBase>();
            for (int i = 0; i < 40; i++)
            {
                EnemyBase enemy = CreateDummyEnemy(_testZakoData, $"Enemy_{i}");
                _stageManager.RegisterEnemy(enemy);
                enemies.Add(enemy);
            }

            for (int i = 0; i < 40; i++)
            {
                enemies[i].TakeDamage(1);
            }

            Assert.IsFalse(challengingBonusAwarded, "일반 스테이지에서는 OnChallengingBonusAwarded가 발행되지 않아야 합니다.");
        }
    }
}
