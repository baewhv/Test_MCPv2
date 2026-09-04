using System;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    /// <summary>
    /// IDamageable 공용 인터페이스 도입 및 발사체 피격 파이프라인 디커플링 무결성을 검증하는 단위 테스트입니다.
    /// </summary>
    [TestFixture]
    public class IDamageableTests
    {
        private GameObject _enemyObj;
        private EnemyBase _enemyBase;
        private EnemyDataSO _testEnemyData;

        private GameObject _playerObj;
        private PlayerHealth _playerHealth;

        private GameObject _playerBulletObj;
        private PlayerBullet _playerBullet;

        private GameObject _enemyBulletObj;
        private EnemyBullet _enemyBullet;

        [SetUp]
        public void SetUp()
        {
            // 1. EnemyBase 세팅
            _testEnemyData = ScriptableObject.CreateInstance<EnemyDataSO>();
            _testEnemyData.Initialize(
                type: EnemyType.Zako,
                enemyName: "TestZako",
                maxHp: 2,
                scoreStay: 50,
                scoreDive: 100,
                moveSpeed: 10f,
                normalColor: Color.blue,
                damagedColor: Color.cyan,
                flashColor: Color.white,
                flashDuration: 0.08f
            );

            _enemyObj = new GameObject("TestEnemy");
            _enemyObj.tag = "Enemy";
            _enemyObj.AddComponent<BoxCollider2D>().isTrigger = true;
            _enemyBase = _enemyObj.AddComponent<EnemyBase>();
            _enemyBase.Initialize(_testEnemyData);

            // 2. PlayerHealth 세팅
            _playerObj = new GameObject("TestPlayer");
            _playerObj.tag = "Player";
            _playerObj.AddComponent<BoxCollider2D>().isTrigger = true;
            _playerHealth = _playerObj.AddComponent<PlayerHealth>();
            _playerHealth.Initialize(3);

            // 3. PlayerBullet 세팅
            _playerBulletObj = new GameObject("TestPlayerBullet");
            _playerBulletObj.tag = "PlayerBullet";
            _playerBulletObj.AddComponent<BoxCollider2D>().isTrigger = true;
            _playerBullet = _playerBulletObj.AddComponent<PlayerBullet>();

            // 4. EnemyBullet 세팅
            _enemyBulletObj = new GameObject("TestEnemyBullet");
            _enemyBulletObj.tag = "EnemyBullet";
            _enemyBulletObj.AddComponent<BoxCollider2D>().isTrigger = true;
            _enemyBullet = _enemyBulletObj.AddComponent<EnemyBullet>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_enemyObj != null) UnityEngine.Object.DestroyImmediate(_enemyObj);
            if (_playerObj != null) UnityEngine.Object.DestroyImmediate(_playerObj);
            if (_playerBulletObj != null) UnityEngine.Object.DestroyImmediate(_playerBulletObj);
            if (_enemyBulletObj != null) UnityEngine.Object.DestroyImmediate(_enemyBulletObj);
            if (_testEnemyData != null) UnityEngine.Object.DestroyImmediate(_testEnemyData);
        }

        #region EnemyBase IDamageable Tests

        [Test]
        public void EnemyBase_Implements_IDamageable_Interface()
        {
            Assert.IsInstanceOf<IDamageable>(_enemyBase);
            IDamageable damageable = _enemyBase as IDamageable;
            Assert.IsNotNull(damageable);
            Assert.AreEqual(2, damageable.CurrentHP);
            Assert.IsFalse(damageable.IsDead);
        }

        [Test]
        public void EnemyBase_IDamageable_TakeDamage_ReducesHP_And_TransitionsToIsDead()
        {
            IDamageable damageable = _enemyBase;

            // 1차 데미지: HP 2 -> 1, IsDead false
            damageable.TakeDamage(1);
            Assert.AreEqual(1, damageable.CurrentHP);
            Assert.AreEqual(1, _enemyBase.CurrentHP);
            Assert.IsFalse(damageable.IsDead);
            Assert.IsFalse(_enemyBase.IsDead);

            // 2차 데미지: HP 1 -> 0, IsDead true
            damageable.TakeDamage(1);
            Assert.AreEqual(0, damageable.CurrentHP);
            Assert.AreEqual(0, _enemyBase.CurrentHP);
            Assert.IsTrue(damageable.IsDead);
            Assert.IsTrue(_enemyBase.IsDead);
            Assert.AreEqual(EnemyState.Dead, _enemyBase.CurrentState);
        }

        #endregion

        #region PlayerHealth IDamageable Tests

        [Test]
        public void PlayerHealth_Implements_IDamageable_Interface()
        {
            Assert.IsInstanceOf<IDamageable>(_playerHealth);
            IDamageable damageable = _playerHealth as IDamageable;
            Assert.IsNotNull(damageable);
            Assert.AreEqual(3, damageable.CurrentHP);
            Assert.AreEqual(3, _playerHealth.CurrentLives);
            Assert.IsFalse(damageable.IsDead);
        }

        [Test]
        public void PlayerHealth_IDamageable_TakeDamage_DecreasesLives_And_SetsInvincibility()
        {
            IDamageable damageable = _playerHealth;

            damageable.TakeDamage(1);

            Assert.AreEqual(2, damageable.CurrentHP);
            Assert.AreEqual(2, _playerHealth.CurrentLives);
            Assert.IsTrue(_playerHealth.IsInvincible);
            Assert.IsFalse(damageable.IsDead);
        }

        [Test]
        public void PlayerHealth_IDamageable_TakeDamage_WhenInvincible_DoesNotTakeDamage()
        {
            IDamageable damageable = _playerHealth;
            _playerHealth.SetInvincibleDirectly(true);

            damageable.TakeDamage(1);

            Assert.AreEqual(3, damageable.CurrentHP);
            Assert.AreEqual(3, _playerHealth.CurrentLives);
            Assert.IsFalse(damageable.IsDead);
        }

        [Test]
        public void PlayerHealth_IDamageable_ZeroLives_TransitionsToIsDead()
        {
            IDamageable damageable = _playerHealth;

            damageable.TakeDamage(3);

            Assert.AreEqual(0, damageable.CurrentHP);
            Assert.AreEqual(0, _playerHealth.CurrentLives);
            Assert.IsTrue(damageable.IsDead);
            Assert.IsTrue(_playerHealth.IsDead);
        }

        #endregion

        #region Decoupled Polymorphism & Mock Target Tests

        /// <summary>
        /// IDamageable 인터페이스를 구현한 모의 피격 엔티티 (예: 보스 트랙터빔 포획기, 장애물 등)
        /// </summary>
        private class MockDamageableTarget : MonoBehaviour, IDamageable
        {
            public int CurrentHP { get; private set; } = 5;
            public bool IsDead => CurrentHP <= 0;
            public int LastDamageReceived { get; private set; } = 0;
            public int TakeDamageCallCount { get; private set; } = 0;

            public void SetHP(int hp)
            {
                CurrentHP = hp;
            }

            public void TakeDamage(int damage)
            {
                CurrentHP -= damage;
                LastDamageReceived = damage;
                TakeDamageCallCount++;
            }
        }

        [Test]
        public void MockTarget_IDamageable_Polymorphism_WorksDirectly()
        {
            GameObject mockObj = new GameObject("MockTarget");
            MockDamageableTarget mockTarget = mockObj.AddComponent<MockDamageableTarget>();

            IDamageable damageable = mockObj.GetComponent<IDamageable>();
            Assert.IsNotNull(damageable);

            damageable.TakeDamage(2);

            Assert.AreEqual(3, damageable.CurrentHP);
            Assert.AreEqual(2, mockTarget.LastDamageReceived);
            Assert.AreEqual(1, mockTarget.TakeDamageCallCount);
            Assert.IsFalse(damageable.IsDead);

            damageable.TakeDamage(3);
            Assert.AreEqual(0, damageable.CurrentHP);
            Assert.IsTrue(damageable.IsDead);

            UnityEngine.Object.DestroyImmediate(mockObj);
        }

        [Test]
        public void PlayerBullet_Damages_Any_IDamageable_Entity_Without_ConcreteEnemyBase()
        {
            // EnemyBase 없이 순수 IDamageable만 가진 Mock 객체 생성
            GameObject mockObj = new GameObject("EnemyMockTarget");
            mockObj.tag = "Enemy";
            mockObj.AddComponent<BoxCollider2D>().isTrigger = true;
            MockDamageableTarget mockTarget = mockObj.AddComponent<MockDamageableTarget>();
            mockTarget.SetHP(3);

            bool bulletDeactivated = false;
            _playerBullet.Initialize((b) => { bulletDeactivated = true; });

            // IDamageable 기반 데미지 인계 검증
            IDamageable damageable = mockObj.GetComponent<IDamageable>();
            Assert.IsNotNull(damageable);
            damageable.TakeDamage(_playerBullet.Damage);

            Assert.AreEqual(2, mockTarget.CurrentHP);
            Assert.AreEqual(1, mockTarget.LastDamageReceived);
            Assert.AreEqual(1, mockTarget.TakeDamageCallCount);

            UnityEngine.Object.DestroyImmediate(mockObj);
        }

        [Test]
        public void EnemyBullet_Damages_Any_IDamageable_Entity_Without_ConcretePlayerHealth()
        {
            // PlayerHealth 없이 순수 IDamageable만 가진 Mock 플레이어 객체 생성
            GameObject mockObj = new GameObject("PlayerMockTarget");
            mockObj.tag = "Player";
            mockObj.AddComponent<BoxCollider2D>().isTrigger = true;
            MockDamageableTarget mockTarget = mockObj.AddComponent<MockDamageableTarget>();
            mockTarget.SetHP(2);

            bool bulletDeactivated = false;
            _enemyBullet.Initialize(Vector2.down, 16f, (b) => { bulletDeactivated = true; });

            // IDamageable 기반 데미지 인계 검증
            IDamageable damageable = mockObj.GetComponent<IDamageable>();
            Assert.IsNotNull(damageable);
            damageable.TakeDamage(_enemyBullet.Damage);

            Assert.AreEqual(1, mockTarget.CurrentHP);
            Assert.AreEqual(1, mockTarget.LastDamageReceived);
            Assert.AreEqual(1, mockTarget.TakeDamageCallCount);

            UnityEngine.Object.DestroyImmediate(mockObj);
        }

        #endregion
    }
}
