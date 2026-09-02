using NUnit.Framework;
using UnityEngine;
using Galaga.Gameplay.Enemy;

namespace Galaga.Tests
{
    [TestFixture]
    public class EnemyDataTests
    {
        private GameObject _enemyObject;
        private EnemyBase _enemy;
        private EnemyDataSO _testData;

        [SetUp]
        public void SetUp()
        {
            _enemyObject = new GameObject("TestEnemy");
            _enemy = _enemyObject.AddComponent<EnemyBase>();

            _testData = ScriptableObject.CreateInstance<EnemyDataSO>();
            _testData.Initialize(
                type: EnemyType.Zako,
                enemyName: "TestZako",
                maxHp: 1,
                scoreStay: 50,
                scoreDive: 100,
                moveSpeed: 10f,
                normalColor: Color.blue,
                damagedColor: Color.cyan,
                flashColor: Color.white,
                flashDuration: 0.05f
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (_enemyObject != null)
            {
                Object.DestroyImmediate(_enemyObject);
            }

            if (_testData != null)
            {
                Object.DestroyImmediate(_testData);
            }
        }

        [Test]
        public void EnemyDataSO_Properties_InitializedCorrectly()
        {
            Assert.AreEqual(EnemyType.Zako, _testData.Type);
            Assert.AreEqual("TestZako", _testData.EnemyName);
            Assert.AreEqual(1, _testData.MaxHP);
            Assert.AreEqual(50, _testData.ScoreStay);
            Assert.AreEqual(100, _testData.ScoreDive);
            Assert.AreEqual(10f, _testData.MoveSpeed);
            Assert.AreEqual(Color.blue, _testData.NormalColor);
        }

        [Test]
        public void EnemyBase_Initialize_SetsHPAndSpeed()
        {
            _enemy.Initialize(_testData);

            Assert.AreEqual(1, _enemy.CurrentHP);
            Assert.AreEqual(EnemyState.Spawning, _enemy.CurrentState);
            Assert.IsFalse(_enemy.IsDead);
            Assert.AreEqual(EnemyType.Zako, _enemy.Type);
        }

        [Test]
        public void EnemyBase_TakeDamage_KillsSingleHpEnemy()
        {
            _enemy.Initialize(_testData);

            bool destroyedFired = false;
            _enemy.OnDestroyed += (e) => destroyedFired = true;

            bool isDead = _enemy.TakeDamage(1);

            Assert.IsTrue(isDead);
            Assert.IsTrue(_enemy.IsDead);
            Assert.AreEqual(0, _enemy.CurrentHP);
            Assert.AreEqual(EnemyState.Dead, _enemy.CurrentState);
            Assert.IsTrue(destroyedFired);
        }

        [Test]
        public void EnemyBase_BossGalaga_SurvivesFirstHit_AndChangesColor()
        {
            EnemyDataSO bossData = ScriptableObject.CreateInstance<EnemyDataSO>();
            bossData.Initialize(
                type: EnemyType.BossGalaga,
                enemyName: "BossGalaga",
                maxHp: 2,
                scoreStay: 150,
                scoreDive: 400,
                moveSpeed: 9f,
                normalColor: Color.green,
                damagedColor: Color.blue,
                flashColor: Color.white
            );

            _enemy.Initialize(bossData);

            bool damagedFired = false;
            int hpOnDamage = 0;
            _enemy.OnDamaged += (e, hp) =>
            {
                damagedFired = true;
                hpOnDamage = hp;
            };

            // 1타 피격
            bool isDead = _enemy.TakeDamage(1);

            Assert.IsFalse(isDead);
            Assert.IsFalse(_enemy.IsDead);
            Assert.AreEqual(1, _enemy.CurrentHP);
            Assert.IsTrue(damagedFired);
            Assert.AreEqual(1, hpOnDamage);

            // 2타 피격 -> 사망
            isDead = _enemy.TakeDamage(1);
            Assert.IsTrue(isDead);
            Assert.IsTrue(_enemy.IsDead);
            Assert.AreEqual(0, _enemy.CurrentHP);

            Object.DestroyImmediate(bossData);
        }

        [Test]
        public void EnemyBase_GetCurrentScoreValue_ReflectsState()
        {
            _enemy.Initialize(_testData);

            _enemy.SetState(EnemyState.Formation);
            Assert.AreEqual(50, _enemy.GetCurrentScoreValue());

            _enemy.SetState(EnemyState.Diving);
            Assert.AreEqual(100, _enemy.GetCurrentScoreValue());

            _enemy.SetState(EnemyState.Entering);
            Assert.AreEqual(100, _enemy.GetCurrentScoreValue());
        }
    }
}
