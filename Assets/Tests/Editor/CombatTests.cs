using System;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    [TestFixture]
    public class CombatTests
    {
        private GameObject _playerObj;
        private PlayerHealth _playerHealth;
        private GameObject _enemyObj;
        private EnemyBase _enemyBase;
        private EnemyDataSO _testEnemyData;
        private GameObject _bulletObj;
        private PlayerBullet _playerBullet;
        private GameObject _enemyBulletObj;
        private EnemyBullet _enemyBullet;

        [SetUp]
        public void SetUp()
        {
            // Player 설정
            _playerObj = new GameObject("TestPlayer");
            _playerObj.tag = "Player";
            _playerObj.AddComponent<BoxCollider2D>().isTrigger = true;
            _playerHealth = _playerObj.AddComponent<PlayerHealth>();
            _playerHealth.Initialize(3);

            // Enemy 설정
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

            // Player Bullet 설정
            _bulletObj = new GameObject("TestPlayerBullet");
            _bulletObj.tag = "PlayerBullet";
            _bulletObj.AddComponent<BoxCollider2D>().isTrigger = true;
            _playerBullet = _bulletObj.AddComponent<PlayerBullet>();
            _playerBullet.Speed = 20f;

            // Enemy Bullet 설정
            _enemyBulletObj = new GameObject("TestEnemyBullet");
            _enemyBulletObj.tag = "EnemyBullet";
            _enemyBulletObj.AddComponent<BoxCollider2D>().isTrigger = true;
            _enemyBullet = _enemyBulletObj.AddComponent<EnemyBullet>();
            _enemyBullet.Speed = 16f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObj != null) UnityEngine.Object.DestroyImmediate(_playerObj);
            if (_enemyObj != null) UnityEngine.Object.DestroyImmediate(_enemyObj);
            if (_bulletObj != null) UnityEngine.Object.DestroyImmediate(_bulletObj);
            if (_enemyBulletObj != null) UnityEngine.Object.DestroyImmediate(_enemyBulletObj);
            if (_testEnemyData != null) UnityEngine.Object.DestroyImmediate(_testEnemyData);
        }

        [Test]
        public void EnemyBase_TakeDamage_ReducesHP_And_TriggersDamagedEvent()
        {
            int damagedHp = -1;
            _enemyBase.OnDamaged += (enemy, hp) => { damagedHp = hp; };

            bool isDead = _enemyBase.TakeDamage(1);

            Assert.IsFalse(isDead);
            Assert.AreEqual(1, _enemyBase.CurrentHP);
            Assert.AreEqual(1, damagedHp);
            Assert.IsFalse(_enemyBase.IsDead);
        }

        [Test]
        public void EnemyBase_HPZero_CallsDie_And_TriggersOnDestroyedEvent()
        {
            bool destroyedEventFired = false;
            _enemyBase.OnDestroyed += (enemy) => { destroyedEventFired = true; };

            bool isDead = _enemyBase.TakeDamage(2);

            Assert.IsTrue(isDead);
            Assert.AreEqual(0, _enemyBase.CurrentHP);
            Assert.IsTrue(_enemyBase.IsDead);
            Assert.AreEqual(EnemyState.Dead, _enemyBase.CurrentState);
            Assert.IsTrue(destroyedEventFired);
            Assert.IsFalse(_enemyObj.activeSelf);
        }

        [Test]
        public void EnemyBullet_Move_MovesTowardsDirection_And_DeactivatesOnBoundary()
        {
            bool deactivated = false;
            _enemyBullet.Initialize(Vector2.down, 10f, (b) => { deactivated = true; });
            _enemyBullet.transform.position = new Vector3(0f, 0f, 0f);

            // 1초 이동 후 위치 검사
            _enemyBullet.Move(1.0f);
            Assert.AreEqual(-10f, _enemyBullet.transform.position.y, 0.001f);

            // 경계 밖 이동 시 회수 검사
            _enemyBullet.Move(0.5f);
            Assert.IsTrue(deactivated);
            Assert.IsFalse(_enemyBullet.gameObject.activeSelf);
        }

        [Test]
        public void PlayerHealth_TakeDamage_DecreasesLives_And_TriggersEvents()
        {
            int livesChangedCount = 0;
            _playerHealth.OnLivesChanged += (lives) => { livesChangedCount++; };

            bool damaged = _playerHealth.TakeDamage(1);

            Assert.IsTrue(damaged);
            Assert.AreEqual(2, _playerHealth.CurrentLives);
            Assert.GreaterOrEqual(livesChangedCount, 1);
            Assert.IsTrue(_playerHealth.IsInvincible);
        }

        [Test]
        public void PlayerHealth_Invincible_DoesNotTakeDamage()
        {
            _playerHealth.SetInvincibleDirectly(true);

            bool damaged = _playerHealth.TakeDamage(1);

            Assert.IsFalse(damaged);
            Assert.AreEqual(3, _playerHealth.CurrentLives);
        }

        [Test]
        public void PlayerHealth_ZeroLives_TriggersDeath()
        {
            bool diedEventFired = false;
            _playerHealth.OnPlayerDied += () => { diedEventFired = true; };

            _playerHealth.TakeDamage(3);

            Assert.AreEqual(0, _playerHealth.CurrentLives);
            Assert.IsTrue(_playerHealth.IsDead);
            Assert.IsTrue(diedEventFired);
        }

        [Test]
        public void ExplosionEffect_Play_SetsDuration_And_Completes()
        {
            GameObject expObj = new GameObject("TestExp");
            ExplosionEffect exp = expObj.AddComponent<ExplosionEffect>();

            bool completed = false;
            exp.Play(Vector3.zero, 0.2f, 1.0f, (e) => { completed = true; });

            Assert.AreEqual(0.2f, exp.Duration);

            exp.Complete();
            Assert.IsTrue(completed);
            Assert.IsFalse(expObj.activeSelf);

            UnityEngine.Object.DestroyImmediate(expObj);
        }
    }
}
