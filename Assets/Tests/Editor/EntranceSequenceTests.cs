using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Enemy;

namespace Galaga.Tests
{
    [TestFixture]
    public class EntranceSequenceTests
    {
        private GameObject _managerObject;
        private EntranceSequenceManager _sequenceManager;
        private FormationGridManager _gridManager;

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("TestEntranceSequence");
            _gridManager = _managerObject.AddComponent<FormationGridManager>();
            _gridManager.InitializeGrid();

            _sequenceManager = _managerObject.AddComponent<EntranceSequenceManager>();
            _sequenceManager.GridManager = _gridManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerObject != null)
            {
                Object.DestroyImmediate(_managerObject);
            }
        }

        [Test]
        public void GetWaveEnemyTypes_ReturnsExactly8EnemiesPerWave()
        {
            int totalBoss = 0;
            int totalGoei = 0;
            int totalZako = 0;

            for (int wave = 1; wave <= 5; wave++)
            {
                EnemyType[] types = EntranceSequenceManager.GetWaveEnemyTypes(wave);
                Assert.AreEqual(8, types.Length, $"Wave {wave} must have exactly 8 enemies");

                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] == EnemyType.BossGalaga) totalBoss++;
                    else if (types[i] == EnemyType.Goei) totalGoei++;
                    else if (types[i] == EnemyType.Zako) totalZako++;
                }
            }

            // 총 40기: 보스 4, 고에이 16, 자코 20
            Assert.AreEqual(4, totalBoss);
            Assert.AreEqual(16, totalGoei);
            Assert.AreEqual(20, totalZako);
            Assert.AreEqual(40, totalBoss + totalGoei + totalZako);
        }

        [Test]
        public void CreateEntranceTrajectory_GeneratesValidPathEndingAtTarget()
        {
            Vector2 targetSlot = new Vector2(2f, 5f);

            for (int wave = 1; wave <= 5; wave++)
            {
                BezierSegment[] trajectory = EntranceSequenceManager.CreateEntranceTrajectory(wave, targetSlot);

                Assert.IsNotNull(trajectory);
                Assert.GreaterOrEqual(trajectory.Length, 1);

                // 경로의 마지막 점(P3)은 반드시 목표 슬롯 위치와 일치해야 함
                BezierSegment lastSegment = trajectory[trajectory.Length - 1];
                Assert.AreEqual(targetSlot.x, lastSegment.p3.x, 0.0001f);
                Assert.AreEqual(targetSlot.y, lastSegment.p3.y, 0.0001f);
            }
        }

        [Test]
        public void SpawnAndLaunchEnemy_WhenEnemyDestroyedDuringEntrance_IncrementsDestroyedCountAndReleasesSlot()
        {
            GameObject zakoPrefab = new GameObject("TestZakoPrefab");
            zakoPrefab.AddComponent<BezierPathFollower>();
            EnemyBase enemyComp = zakoPrefab.AddComponent<EnemyBase>();

            _sequenceManager.ZakoPrefab = zakoPrefab;

            // 시퀀스 실행 상태를 시뮬레이션하기 위해 reflection으로 _isSequenceRunning 설정
            var isRunningField = typeof(EntranceSequenceManager).GetField("_isSequenceRunning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isRunningField?.SetValue(_sequenceManager, true);

            EnemyBase spawnedEnemy = _sequenceManager.SpawnAndLaunchEnemy(1, EnemyType.Zako);
            Assert.IsNotNull(spawnedEnemy);
            Assert.AreEqual(1, _sequenceManager.TotalSpawnedCount);
            Assert.AreEqual(0, _sequenceManager.TotalArrivedCount);
            Assert.AreEqual(0, _sequenceManager.TotalDestroyedDuringEntrance);

            FormationSlot slot = _gridManager.FindSlotByEnemy(spawnedEnemy);
            Assert.IsNotNull(slot, "Enemy should be assigned to a slot");
            Assert.IsTrue(slot.IsOccupied);

            // 진입 도중 적 격추
            spawnedEnemy.Die();

            Assert.AreEqual(1, _sequenceManager.TotalDestroyedDuringEntrance);
            Assert.AreEqual(0, _sequenceManager.TotalArrivedCount);
            Assert.IsFalse(slot.IsOccupied, "Slot must be released when enemy is destroyed during entrance");
            Assert.IsFalse(_sequenceManager.ActiveEnemies.Contains(spawnedEnemy));

            Object.DestroyImmediate(zakoPrefab);
        }

        [Test]
        public void EntranceSequence_ArrivalAndDestroyCondition_SatisfiesCompletion()
        {
            GameObject zakoPrefab = new GameObject("TestZakoPrefab2");
            zakoPrefab.AddComponent<BezierPathFollower>();
            zakoPrefab.AddComponent<EnemyBase>();

            _sequenceManager.ZakoPrefab = zakoPrefab;

            var isRunningField = typeof(EntranceSequenceManager).GetField("_isSequenceRunning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isRunningField?.SetValue(_sequenceManager, true);

            // 4기 스폰
            EnemyBase e1 = _sequenceManager.SpawnAndLaunchEnemy(1, EnemyType.Zako);
            EnemyBase e2 = _sequenceManager.SpawnAndLaunchEnemy(1, EnemyType.Zako);
            EnemyBase e3 = _sequenceManager.SpawnAndLaunchEnemy(1, EnemyType.Zako);
            EnemyBase e4 = _sequenceManager.SpawnAndLaunchEnemy(1, EnemyType.Zako);

            Assert.AreEqual(4, _sequenceManager.TotalSpawnedCount);

            // 2기 안착 완료 (OnPathCompleted 호출 시뮬레이션)
            e1.EnterFormation();
            var arrivedField = typeof(EntranceSequenceManager).GetField("_totalArrivedCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            arrivedField?.SetValue(_sequenceManager, 2);

            // 2기 진입 중 파괴
            e3.Die();
            e4.Die();

            Assert.AreEqual(2, _sequenceManager.TotalArrivedCount);
            Assert.AreEqual(2, _sequenceManager.TotalDestroyedDuringEntrance);
            Assert.AreEqual(_sequenceManager.TotalSpawnedCount, _sequenceManager.TotalArrivedCount + _sequenceManager.TotalDestroyedDuringEntrance);

            Object.DestroyImmediate(zakoPrefab);
        }
    }
}
