using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Enemy;

namespace Galaga.Tests
{
    [TestFixture]
    public class RigidbodyBulletMovementTests
    {
        private GameObject _playerBulletObj;
        private PlayerBullet _playerBullet;
        private GameObject _enemyBulletObj;
        private EnemyBullet _enemyBullet;
        private GameObject _enemyObj;
        private EnemyBase _enemyBase;
        private EnemyDataSO _enemyData;

        [SetUp]
        public void SetUp()
        {
            _playerBulletObj = new GameObject("TestPlayerBullet");
            _playerBullet = _playerBulletObj.AddComponent<PlayerBullet>();
            _playerBullet.SetupComponents();

            _enemyBulletObj = new GameObject("TestEnemyBullet");
            _enemyBullet = _enemyBulletObj.AddComponent<EnemyBullet>();
            _enemyBullet.SetupComponents();

            _enemyObj = new GameObject("TestEnemy");
            _enemyBase = _enemyObj.AddComponent<EnemyBase>();

            _enemyData = ScriptableObject.CreateInstance<EnemyDataSO>();
            _enemyData.Initialize(
                type: EnemyType.Zako,
                enemyName: "Zako",
                maxHp: 2,
                scoreStay: 50,
                scoreDive: 100,
                moveSpeed: 10f,
                normalColor: Color.blue,
                damagedColor: Color.cyan,
                flashColor: Color.white,
                flashDuration: 0.15f
            );
            _enemyBase.Initialize(_enemyData);
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerBulletObj != null) Object.DestroyImmediate(_playerBulletObj);
            if (_enemyBulletObj != null) Object.DestroyImmediate(_enemyBulletObj);
            if (_enemyObj != null) Object.DestroyImmediate(_enemyObj);
            if (_enemyData != null) Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void PlayerBullet_HasContinuousRigidbody2D_And_TriggerBoxCollider2D()
        {
            Rigidbody2D rb = _playerBullet.GetComponent<Rigidbody2D>();
            BoxCollider2D col = _playerBullet.GetComponent<BoxCollider2D>();

            Assert.IsNotNull(rb, "PlayerBullet에는 Rigidbody2D가 반드시 존재해야 합니다.");
            Assert.AreEqual(0f, rb.gravityScale, "Rigidbody2D의 gravityScale은 0이어야 합니다.");
            Assert.AreEqual(CollisionDetectionMode2D.Continuous, rb.collisionDetectionMode, "터널링 방지를 위해 collisionDetectionMode는 Continuous여야 합니다.");
            Assert.IsTrue(rb.freezeRotation, "Z축 회전 고정(freezeRotation)이 활성화되어야 합니다.");

            Assert.IsNotNull(col, "PlayerBullet에는 BoxCollider2D가 반드시 존재해야 합니다.");
            Assert.IsTrue(col.isTrigger, "BoxCollider2D는 트리거(isTrigger = true)여야 합니다.");
            Assert.AreEqual(new Vector2(1.0f, 1.0f), col.size, "BoxCollider2D 크기는 (1.0, 1.0) 정규화 규격이어야 합니다.");
        }

        [Test]
        public void PlayerBullet_Velocity_IsSetToUpwardSpeed()
        {
            _playerBullet.Speed = 27.7f;
            Rigidbody2D rb = _playerBullet.GetComponent<Rigidbody2D>();

            Assert.AreEqual(0f, rb.velocity.x, 0.001f);
            Assert.AreEqual(27.7f, rb.velocity.y, 0.01f);
        }

        [Test]
        public void EnemyBullet_HasContinuousRigidbody2D_And_TriggerBoxCollider2D()
        {
            Rigidbody2D rb = _enemyBullet.GetComponent<Rigidbody2D>();
            BoxCollider2D col = _enemyBullet.GetComponent<BoxCollider2D>();

            Assert.IsNotNull(rb, "EnemyBullet에는 Rigidbody2D가 반드시 존재해야 합니다.");
            Assert.AreEqual(0f, rb.gravityScale, "Rigidbody2D의 gravityScale은 0이어야 합니다.");
            Assert.AreEqual(CollisionDetectionMode2D.Continuous, rb.collisionDetectionMode, "터널링 방지를 위해 collisionDetectionMode는 Continuous여야 합니다.");
            Assert.IsTrue(rb.freezeRotation, "Z축 회전 고정(freezeRotation)이 활성화되어야 합니다.");

            Assert.IsNotNull(col, "EnemyBullet에는 BoxCollider2D가 반드시 존재해야 합니다.");
            Assert.IsTrue(col.isTrigger, "BoxCollider2D는 트리거(isTrigger = true)여야 합니다.");
            Assert.AreEqual(new Vector2(1.0f, 1.0f), col.size, "BoxCollider2D 크기는 (1.0, 1.0) 정규화 규격이어야 합니다.");
        }

        [Test]
        public void EnemyBullet_Initialize_SetsVelocityTowardsDirection()
        {
            Vector2 dir = new Vector2(1f, -1f).normalized;
            float speed = 16f;

            _enemyBullet.Initialize(dir, speed, null);
            Rigidbody2D rb = _enemyBullet.GetComponent<Rigidbody2D>();

            Vector2 expectedVelocity = dir * speed;
            Assert.AreEqual(expectedVelocity.x, rb.velocity.x, 0.01f);
            Assert.AreEqual(expectedVelocity.y, rb.velocity.y, 0.01f);
        }

        [Test]
        public void EnemyBase_ImplementsIDamageable_And_TakesDamageCorrectly()
        {
            Assert.IsTrue(_enemyBase is IDamageable, "EnemyBase는 IDamageable 인터페이스를 구현해야 합니다.");

            IDamageable damageable = _enemyBase as IDamageable;
            Assert.IsNotNull(damageable);

            bool isDead = damageable.TakeDamage(1);
            Assert.IsFalse(isDead, "체력 2인 적에게 1 데미지를 주면 생존해야 합니다.");
            Assert.AreEqual(1, _enemyBase.CurrentHP);

            isDead = damageable.TakeDamage(1);
            Assert.IsTrue(isDead, "체력 0에 도달하면 사망(isDead = true)해야 합니다.");
            Assert.AreEqual(0, _enemyBase.CurrentHP);
        }

        [Test]
        public void EnemyDataSO_FlashDuration_IsNormalizedToPointOneFiveSeconds()
        {
            EnemyDataSO defaultSo = ScriptableObject.CreateInstance<EnemyDataSO>();
            Assert.AreEqual(0.15f, defaultSo.FlashDuration, 0.001f, "EnemyDataSO 기본 피격 플래시 지속 시간은 0.15초여야 합니다.");
            Object.DestroyImmediate(defaultSo);
        }
    }
}
