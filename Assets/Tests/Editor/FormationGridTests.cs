using NUnit.Framework;
using UnityEngine;
using Galaga.Gameplay.Enemy;

namespace Galaga.Tests
{
    [TestFixture]
    public class FormationGridTests
    {
        private GameObject _gridObject;
        private FormationGridManager _gridManager;

        [SetUp]
        public void SetUp()
        {
            _gridObject = new GameObject("TestGridManager");
            _gridManager = _gridObject.AddComponent<FormationGridManager>();
            _gridManager.GridOrigin = new Vector2(0f, 6.0f);
            _gridManager.RowSpacing = 1.0f;
            _gridManager.ColumnSpacing = 1.0f;
            _gridManager.InitializeGrid();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gridObject != null)
            {
                Object.DestroyImmediate(_gridObject);
            }
        }

        [Test]
        public void GridManager_CreatesExactly40Slots()
        {
            Assert.AreEqual(40, _gridManager.Slots.Count);
            Assert.AreEqual(0, _gridManager.OccupiedCount);
            Assert.IsFalse(_gridManager.IsFull);
        }

        [Test]
        public void GridManager_RowTypesAndCounts_MatchSpecification()
        {
            // Row 0: 4 Boss
            int bossCount = 0;
            // Row 1-2: 16 Goei
            int goeiCount = 0;
            // Row 3-4: 20 Zako
            int zakoCount = 0;

            for (int i = 0; i < _gridManager.Slots.Count; i++)
            {
                FormationSlot slot = _gridManager.Slots[i];
                if (slot.AssignedType == EnemyType.BossGalaga) bossCount++;
                else if (slot.AssignedType == EnemyType.Goei) goeiCount++;
                else if (slot.AssignedType == EnemyType.Zako) zakoCount++;
            }

            Assert.AreEqual(4, bossCount);
            Assert.AreEqual(16, goeiCount);
            Assert.AreEqual(20, zakoCount);
        }

        [Test]
        public void GridManager_SlotsAreSymmetricalAroundXZero()
        {
            for (int r = 0; r < FormationGridManager.RowCounts.Length; r++)
            {
                int count = FormationGridManager.RowCounts[r];
                for (int c = 0; c < count / 2; c++)
                {
                    FormationSlot leftSlot = _gridManager.GetSlot(r, c);
                    FormationSlot rightSlot = _gridManager.GetSlot(r, count - 1 - c);

                    Assert.IsNotNull(leftSlot);
                    Assert.IsNotNull(rightSlot);
                    Assert.AreEqual(-leftSlot.BaseLocalPosition.x, rightSlot.BaseLocalPosition.x, 0.001f);
                    Assert.AreEqual(leftSlot.BaseLocalPosition.y, rightSlot.BaseLocalPosition.y, 0.001f);
                }
            }
        }

        [Test]
        public void GridManager_AssignAndReleaseEnemy_UpdatesOccupancy()
        {
            GameObject enemyObj = new GameObject("DummyEnemy");
            EnemyBase enemy = enemyObj.AddComponent<EnemyBase>();

            FormationSlot assignedSlot = _gridManager.AssignEnemyToNextAvailableSlot(EnemyType.BossGalaga, enemy);

            Assert.IsNotNull(assignedSlot);
            Assert.IsTrue(assignedSlot.IsOccupied);
            Assert.AreEqual(enemy, assignedSlot.Occupant);
            Assert.AreEqual(1, _gridManager.OccupiedCount);

            _gridManager.ReleaseEnemy(enemy);

            Assert.IsFalse(assignedSlot.IsOccupied);
            Assert.IsNull(assignedSlot.Occupant);
            Assert.AreEqual(0, _gridManager.OccupiedCount);

            Object.DestroyImmediate(enemyObj);
        }

        [Test]
        public void GridManager_UpdateFormationHover_AppliesSineWaveOffset()
        {
            FormationSlot centerLeftSlot = _gridManager.GetSlot(0, 1); // Row 0
            Vector2 initialPos = centerLeftSlot.CurrentWorldPosition;

            _gridManager.SwayFrequency = 2.0f;
            _gridManager.SwayAmplitude = 1.0f;
            _gridManager.ExpandFrequency = 2.0f;
            _gridManager.ExpandAmplitude = 0.2f;

            // t = 0 -> sin(0) = 0
            _gridManager.UpdateFormationHover(0f);
            Vector2 posAtZero = centerLeftSlot.CurrentWorldPosition;

            // t = 0.5 * PI / 2.0 -> time = PI / 4 -> sin(2 * PI / 4) = sin(PI/2) = 1.0
            float peakTime = Mathf.PI / 4f;
            _gridManager.UpdateFormationHover(peakTime);
            Vector2 posAtPeak = centerLeftSlot.CurrentWorldPosition;

            Assert.AreNotEqual(posAtZero.x, posAtPeak.x);
        }
    }
}
