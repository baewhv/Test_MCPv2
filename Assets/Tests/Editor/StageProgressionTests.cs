using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;
using Galaga.Gameplay.Stage;

namespace Galaga.Tests
{
    [TestFixture]
    public class StageProgressionTests
    {
        private GameObject _stageManagerObject;
        private StageManager _stageManager;
        private GameObject _playerObject;
        private PlayerHealth _playerHealth;
        private GameObject _entranceSeqObject;
        private EntranceSequenceManager _entranceSequenceManager;
        private EnemyDataSO _testEnemyData;
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("TestPlayer");
            _playerHealth = _playerObject.AddComponent<PlayerHealth>();
            _playerHealth.Initialize(3);

            _entranceSeqObject = new GameObject("TestEntranceSequenceManager");
            _entranceSequenceManager = _entranceSeqObject.AddComponent<EntranceSequenceManager>();

            _testEnemyData = ScriptableObject.CreateInstance<EnemyDataSO>();
            _testEnemyData.Initialize(
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

            _stageManagerObject = new GameObject("TestStageManager");
            _stageManager = _stageManagerObject.AddComponent<StageManager>();
            _stageManager.PlayerHealth = _playerHealth;
            _stageManager.EntranceSequenceManager = _entranceSequenceManager;
            _stageManager.StageStartDelay = 0f;
            _stageManager.StageClearDelay = 0f;
            _stageManager.AutoAdvanceToNextStage = false;
            _stageManager.Initialize(1);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawnedObjects.Count; i++)
            {
                if (_spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(_spawnedObjects[i]);
                }
            }
            _spawnedObjects.Clear();

            if (_testEnemyData != null)
            {
                Object.DestroyImmediate(_testEnemyData);
            }

            if (_stageManagerObject != null)
            {
                Object.DestroyImmediate(_stageManagerObject);
            }

            if (_entranceSeqObject != null)
            {
                Object.DestroyImmediate(_entranceSeqObject);
            }

            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }
        }

        private EnemyBase CreateDummyEnemy(string name = "DummyEnemy")
        {
            GameObject enemyObj = new GameObject(name);
            _spawnedObjects.Add(enemyObj);
            EnemyBase enemy = enemyObj.AddComponent<EnemyBase>();
            enemy.Initialize(_testEnemyData);
            return enemy;
        }

        [Test]
        public void Singleton_InstanceAssignmentAndAccess()
        {
            Assert.IsNotNull(StageManager.Instance, "StageManager.Instance는 유효한 인스턴스를 가리켜야 합니다.");
            Assert.AreSame(_stageManager, StageManager.Instance, "Instance는 현재 활성화된 StageManager여야 합니다.");
        }

        [Test]
        public void ChallengingStage_Formula_CorrectlyIdentifiesStages()
        {
            // 4n - 1 주기: Stage 3, 7, 11, 15, 19, 23...
            int[] challengingStages = { 3, 7, 11, 15, 19, 23, 27, 31 };
            int[] normalStages = { 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 16, 20 };

            foreach (int stage in challengingStages)
            {
                Assert.IsTrue(StageManager.CheckIsChallengingStage(stage), $"Stage {stage}는 챌린징 스테이지여야 합니다.");
            }

            foreach (int stage in normalStages)
            {
                Assert.IsFalse(StageManager.CheckIsChallengingStage(stage), $"Stage {stage}는 일반 스테이지여야 합니다.");
            }
        }

        [Test]
        public void Initialize_SetsStartingStageAndFiresEvents()
        {
            int receivedStage = 0;
            bool receivedChallenging = false;
            int receivedEnemyCount = -1;

            _stageManager.OnStageChanged += (s) => receivedStage = s;
            _stageManager.OnChallengingStageTriggered += (c) => receivedChallenging = c;
            _stageManager.OnEnemyCountChanged += (count) => receivedEnemyCount = count;

            _stageManager.Initialize(3);

            Assert.AreEqual(3, _stageManager.CurrentStage);
            Assert.AreEqual(0, _stageManager.AliveEnemyCount);
            Assert.IsFalse(_stageManager.IsStageInProgress);
            Assert.IsFalse(_stageManager.IsStageClearing);
            Assert.IsTrue(_stageManager.IsChallengingStage);

            Assert.AreEqual(3, receivedStage);
            Assert.IsTrue(receivedChallenging);
            Assert.AreEqual(0, receivedEnemyCount);
        }

        [Test]
        public void StartStage_UpdatesStageAndFlags()
        {
            int changedStage = 0;
            _stageManager.OnStageChanged += (s) => changedStage = s;

            _stageManager.StartStage(2);

            Assert.AreEqual(2, _stageManager.CurrentStage);
            Assert.IsTrue(_stageManager.IsStageInProgress);
            Assert.IsFalse(_stageManager.IsStageClearing);
            Assert.IsFalse(_stageManager.IsChallengingStage);
            Assert.AreEqual(2, changedStage);
        }

        [Test]
        public void RegisterEnemy_IncreasesCountsAndFiresEvents()
        {
            EnemyBase enemy = CreateDummyEnemy();
            EnemyBase registeredEnemyEventArg = null;
            int enemyCountEventArg = 0;

            _stageManager.OnEnemyRegistered += (e) => registeredEnemyEventArg = e;
            _stageManager.OnEnemyCountChanged += (c) => enemyCountEventArg = c;

            _stageManager.RegisterEnemy(enemy);

            Assert.AreEqual(1, _stageManager.AliveEnemyCount);
            Assert.AreEqual(1, _stageManager.SpawnedEnemyCount);
            Assert.AreSame(enemy, registeredEnemyEventArg);
            Assert.AreEqual(1, enemyCountEventArg);
            Assert.AreEqual(1, _stageManager.RegisteredEnemies.Count);
        }

        [Test]
        public void RegisterEnemy_Duplicate_IsIgnored()
        {
            EnemyBase enemy = CreateDummyEnemy();

            _stageManager.RegisterEnemy(enemy);
            _stageManager.RegisterEnemy(enemy);

            Assert.AreEqual(1, _stageManager.AliveEnemyCount);
            Assert.AreEqual(1, _stageManager.SpawnedEnemyCount);
            Assert.AreEqual(1, _stageManager.RegisteredEnemies.Count);
        }

        [Test]
        public void UnregisterEnemy_DecreasesCountAndFiresEvents()
        {
            EnemyBase enemy = CreateDummyEnemy();
            _stageManager.RegisterEnemy(enemy);

            EnemyBase unregisteredEnemyEventArg = null;
            int enemyCountEventArg = -1;

            _stageManager.OnEnemyUnregistered += (e) => unregisteredEnemyEventArg = e;
            _stageManager.OnEnemyCountChanged += (c) => enemyCountEventArg = c;

            _stageManager.UnregisterEnemy(enemy);

            Assert.AreEqual(0, _stageManager.AliveEnemyCount);
            Assert.AreSame(enemy, unregisteredEnemyEventArg);
            Assert.AreEqual(0, enemyCountEventArg);
            Assert.AreEqual(0, _stageManager.RegisteredEnemies.Count);
        }

        [Test]
        public void EnemyDestroyed_ReducesAliveCountAndTriggersClearWhenZero()
        {
            _stageManager.StartStage(1);

            EnemyBase enemy1 = CreateDummyEnemy("Enemy1");
            EnemyBase enemy2 = CreateDummyEnemy("Enemy2");

            _stageManager.RegisterEnemy(enemy1);
            _stageManager.RegisterEnemy(enemy2);
            _stageManager.HandleEntranceSequenceCompleted();

            Assert.AreEqual(2, _stageManager.AliveEnemyCount);

            bool allDefeatedFired = false;
            int clearedStage = 0;
            _stageManager.OnAllEnemiesDefeated += () => allDefeatedFired = true;
            _stageManager.OnStageCleared += (stage) => clearedStage = stage;

            // enemy1 처치
            enemy1.TakeDamage(1);
            Assert.AreEqual(1, _stageManager.AliveEnemyCount);
            Assert.IsFalse(allDefeatedFired);

            // enemy2 처치 -> 0기 도달 및 스테이지 클리어
            enemy2.TakeDamage(1);
            Assert.AreEqual(0, _stageManager.AliveEnemyCount);
            Assert.IsTrue(allDefeatedFired, "모든 적 처치 시 OnAllEnemiesDefeated가 발행되어야 합니다.");
            Assert.AreEqual(1, clearedStage, "OnStageCleared 이벤트에 클리어된 스테이지 번호가 전달되어야 합니다.");
            Assert.IsTrue(_stageManager.IsStageClearing);
            Assert.IsFalse(_stageManager.IsStageInProgress);
        }

        [Test]
        public void StageClearCondition_NotTriggered_IfEntranceNotFinishedAndSpawnCountBelowTotal()
        {
            _stageManager.StartStage(1);
            // 진입 시퀀스 완료되지 않음 (_isEntranceSequenceFinished = false)

            EnemyBase enemy = CreateDummyEnemy("Enemy1");
            _stageManager.RegisterEnemy(enemy);

            bool clearedFired = false;
            _stageManager.OnStageCleared += (s) => clearedFired = true;

            // 적 격파
            enemy.TakeDamage(1);

            Assert.AreEqual(0, _stageManager.AliveEnemyCount);
            Assert.IsFalse(clearedFired, "진입 시퀀스가 완료되지 않았고 총 스폰수(40기) 미만이면 클리어가 발동되지 않아야 합니다.");
            Assert.IsFalse(_stageManager.IsStageClearing);
        }

        [Test]
        public void AdvanceToNextStage_IncrementsCurrentStage()
        {
            _stageManager.StartStage(1);
            Assert.AreEqual(1, _stageManager.CurrentStage);

            _stageManager.AdvanceToNextStage();
            Assert.AreEqual(2, _stageManager.CurrentStage);

            _stageManager.AdvanceToNextStage();
            Assert.AreEqual(3, _stageManager.CurrentStage);
            Assert.IsTrue(_stageManager.IsChallengingStage);
        }

        [Test]
        public void ForceStageClear_TriggersClearSequenceImmediately()
        {
            _stageManager.StartStage(1);
            EnemyBase enemy = CreateDummyEnemy();
            _stageManager.RegisterEnemy(enemy);

            bool cleared = false;
            _stageManager.OnStageCleared += (s) => cleared = true;

            _stageManager.ForceStageClear();

            Assert.IsTrue(cleared);
            Assert.IsTrue(_stageManager.IsStageClearing);
            Assert.AreEqual(0, _stageManager.AliveEnemyCount);
        }

        [Test]
        public void PlayerDied_StopsStageAndFiresGameOver()
        {
            _stageManager.StartStage(1);
            Assert.IsTrue(_stageManager.IsStageInProgress);

            bool gameOverFired = false;
            _stageManager.OnGameOver += () => gameOverFired = true;

            // 플레이어 잔기 일괄 소진 (3 데미지)
            _playerHealth.TakeDamage(3);

            Assert.IsTrue(_playerHealth.IsDead);
            Assert.IsFalse(_stageManager.IsStageInProgress);
            Assert.IsTrue(gameOverFired, "플레이어 사망 시 OnGameOver 이벤트가 발행되어야 합니다.");
        }

        [Test]
        public void ResetGame_ResetsToStageOne()
        {
            _stageManager.StartStage(5);
            Assert.AreEqual(5, _stageManager.CurrentStage);

            _stageManager.ResetGame();

            Assert.AreEqual(1, _stageManager.CurrentStage);
            Assert.IsFalse(_stageManager.IsStageInProgress);
            Assert.AreEqual(0, _stageManager.AliveEnemyCount);
        }
    }
}
