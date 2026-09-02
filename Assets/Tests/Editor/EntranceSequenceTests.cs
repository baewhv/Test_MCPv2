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
    }
}
