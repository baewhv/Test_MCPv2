using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

[TestFixture]
public class IDamageablePipelineTests
{
    // Dummy / Mock IDamageable Component for pure decoupled pipeline testing
    public class MockDamageableTarget : MonoBehaviour, IDamageable
    {
        public bool IsAlive { get; set; } = true;
        public int LastReceivedDamage { get; private set; } = 0;
        public int DamageCallCount { get; private set; } = 0;
        public bool ReturnValue { get; set; } = true;

        public bool TakeDamage(int damage = 1)
        {
            LastReceivedDamage = damage;
            DamageCallCount++;
            if (!IsAlive) return false;
            return ReturnValue;
        }
    }

    private GameObject _playerObj;
    private PlayerHealth _playerHealth;
    private GameObject _enemyObj;
    private EnemyBase _enemyBase;
    private EnemyDataSO _testEnemyData;
    private GameObject _mockTargetObj;
    private MockDamageableTarget _mockTarget;
    private GameObject _playerBulletObj;
    private PlayerBullet _playerBullet;
    private GameObject _enemyBulletObj;
    private EnemyBullet _enemyBullet;

    [SetUp]
    public void SetUp()
    {
        // 1. Mock Target
        _mockTargetObj = new GameObject("MockDamageableTarget");
        _mockTargetObj.tag = "Untagged";
        _mockTargetObj.AddComponent<BoxCollider2D>().isTrigger = true;
        _mockTarget = _mockTargetObj.AddComponent<MockDamageableTarget>();

        // 2. Player Setup
        _playerObj = new GameObject("TestPlayer");
        _playerObj.tag = "Player";
        _playerObj.AddComponent<BoxCollider2D>().isTrigger = true;
        _playerHealth = _playerObj.AddComponent<PlayerHealth>();
        _playerHealth.Initialize(3);

        // 3. Enemy Setup
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

        // 4. Player Bullet Setup
        _playerBulletObj = new GameObject("TestPlayerBullet");
        _playerBulletObj.tag = "PlayerBullet";
        _playerBulletObj.AddComponent<BoxCollider2D>().isTrigger = true;
        _playerBullet = _playerBulletObj.AddComponent<PlayerBullet>();
        _playerBullet.Speed = 20f;

        // 5. Enemy Bullet Setup
        _enemyBulletObj = new GameObject("TestEnemyBullet");
        _enemyBulletObj.tag = "EnemyBullet";
        _enemyBulletObj.AddComponent<BoxCollider2D>().isTrigger = true;
        _enemyBullet = _enemyBulletObj.AddComponent<EnemyBullet>();
        _enemyBullet.Speed = 16f;
    }

    [TearDown]
    public void TearDown()
    {
        if (_mockTargetObj != null) UnityEngine.Object.DestroyImmediate(_mockTargetObj);
        if (_playerObj != null) UnityEngine.Object.DestroyImmediate(_playerObj);
        if (_enemyObj != null) UnityEngine.Object.DestroyImmediate(_enemyObj);
        if (_playerBulletObj != null) UnityEngine.Object.DestroyImmediate(_playerBulletObj);
        if (_enemyBulletObj != null) UnityEngine.Object.DestroyImmediate(_enemyBulletObj);
        if (_testEnemyData != null) UnityEngine.Object.DestroyImmediate(_testEnemyData);
    }

    #region IDamageable Polymorphism Tests

    [Test]
    public void PlayerBullet_HitsMockIDamageable_AppliesDamage_And_ReturnsToPool()
    {
        // Arrange
        bool returnedToPool = false;
        _playerBullet.Initialize((b) => { returnedToPool = true; });

        // Act - Simulate 2D Trigger collision with Mock Target
        var triggerMethod = typeof(PlayerBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockCollider = _mockTargetObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_playerBullet, new object[] { mockCollider });

        // Assert
        Assert.AreEqual(1, _mockTarget.DamageCallCount, "Mock IDamageable Target must receive TakeDamage call.");
        Assert.AreEqual(_playerBullet.Damage, _mockTarget.LastReceivedDamage, "Mock Target must receive the bullet damage.");
        Assert.IsTrue(returnedToPool, "PlayerBullet must return to pool upon hitting IDamageable target.");
        Assert.IsFalse(_playerBulletObj.activeSelf, "PlayerBullet GameObject must be deactivated upon returning to pool.");
    }

    [Test]
    public void EnemyBullet_HitsMockIDamageable_AppliesDamage_And_ReturnsToPool()
    {
        // Arrange
        bool returnedToPool = false;
        _enemyBullet.Initialize(Vector2.down, 16f, (b) => { returnedToPool = true; });

        // Act - Simulate 2D Trigger collision with Mock Target
        var triggerMethod = typeof(EnemyBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockCollider = _mockTargetObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_enemyBullet, new object[] { mockCollider });

        // Assert
        Assert.AreEqual(1, _mockTarget.DamageCallCount, "Mock IDamageable Target must receive TakeDamage call.");
        Assert.AreEqual(_enemyBullet.Damage, _mockTarget.LastReceivedDamage, "Mock Target must receive bullet damage.");
        Assert.IsTrue(returnedToPool, "EnemyBullet must return to pool upon hitting IDamageable target.");
        Assert.IsFalse(_enemyBulletObj.activeSelf, "EnemyBullet GameObject must be deactivated upon returning to pool.");
    }

    #endregion

    #region PlayerHealth IDamageable Implementation Tests

    [Test]
    public void PlayerHealth_Implements_IDamageable_Interface()
    {
        Assert.IsTrue(_playerHealth is IDamageable, "PlayerHealth must implement IDamageable interface.");
        IDamageable damageable = _playerHealth as IDamageable;
        Assert.IsNotNull(damageable);
        Assert.IsTrue(damageable.IsAlive, "PlayerHealth.IsAlive must return true when player has lives.");
    }

    [Test]
    public void PlayerHealth_IsAlive_ReturnsFalse_WhenDead()
    {
        _playerHealth.TakeDamage(3);
        IDamageable damageable = _playerHealth as IDamageable;
        Assert.IsFalse(damageable.IsAlive, "PlayerHealth.IsAlive must return false when lives reach zero.");
        Assert.IsTrue(_playerHealth.IsDead);
    }

    [Test]
    public void EnemyBullet_HitsPlayerHealth_Through_IDamageable_Pipeline()
    {
        // Arrange
        bool returnedToPool = false;
        _enemyBullet.Initialize(Vector2.down, 16f, (b) => { returnedToPool = true; });

        // Act - Trigger collision with Player
        var triggerMethod = typeof(EnemyBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var playerCollider = _playerObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_enemyBullet, new object[] { playerCollider });

        // Assert
        Assert.AreEqual(2, _playerHealth.CurrentLives, "Player lives must decrease by 1.");
        Assert.IsTrue(returnedToPool, "EnemyBullet must return to pool upon hitting player.");
        Assert.IsFalse(_enemyBulletObj.activeSelf, "EnemyBullet must be deactivated.");
    }

    [Test]
    public void PlayerHealth_Ignores_Inactive_EnemyBullet_Collision()
    {
        // Arrange - Deactivate enemy bullet before collision
        _enemyBulletObj.SetActive(false);
        int initialLives = _playerHealth.CurrentLives;

        // Act - PlayerHealth receives collision from inactive bullet
        var triggerMethod = typeof(PlayerHealth).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var bulletCollider = _enemyBulletObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_playerHealth, new object[] { bulletCollider });

        // Assert - No damage should be taken from inactive bullet
        Assert.AreEqual(initialLives, _playerHealth.CurrentLives, "PlayerHealth must ignore collisions from inactive bullets.");
    }

    #endregion

    #region EnemyBase IDamageable Implementation Tests

    [Test]
    public void EnemyBase_Implements_IDamageable_Interface()
    {
        Assert.IsTrue(_enemyBase is IDamageable, "EnemyBase must implement IDamageable interface.");
        IDamageable damageable = _enemyBase as IDamageable;
        Assert.IsNotNull(damageable);
        Assert.IsTrue(damageable.IsAlive, "EnemyBase.IsAlive must return true when enemy is alive.");
    }

    [Test]
    public void EnemyBase_IsAlive_ReturnsFalse_WhenDestroyed()
    {
        _enemyBase.TakeDamage(2);
        IDamageable damageable = _enemyBase as IDamageable;
        Assert.IsFalse(damageable.IsAlive, "EnemyBase.IsAlive must return false when HP reaches zero.");
        Assert.IsTrue(_enemyBase.IsDead);
    }

    [Test]
    public void PlayerBullet_HitsEnemyBase_Through_IDamageable_Pipeline()
    {
        // Arrange
        bool returnedToPool = false;
        _playerBullet.Initialize((b) => { returnedToPool = true; });

        // Act - Trigger collision with Enemy
        var triggerMethod = typeof(PlayerBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var enemyCollider = _enemyObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_playerBullet, new object[] { enemyCollider });

        // Assert
        Assert.AreEqual(1, _enemyBase.CurrentHP, "Enemy HP must decrease from 2 to 1.");
        Assert.IsTrue(returnedToPool, "PlayerBullet must return to pool upon hitting enemy.");
        Assert.IsFalse(_playerBulletObj.activeSelf, "PlayerBullet must be deactivated.");
    }

    [Test]
    public void EnemyBase_Ignores_Inactive_PlayerBullet_Collision()
    {
        // Arrange - Deactivate player bullet before collision
        _playerBulletObj.SetActive(false);
        int initialHp = _enemyBase.CurrentHP;

        // Act - EnemyBase receives collision from inactive bullet
        var triggerMethod = typeof(EnemyBase).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var bulletCollider = _playerBulletObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_enemyBase, new object[] { bulletCollider });

        // Assert - No damage should be taken from inactive bullet
        Assert.AreEqual(initialHp, _enemyBase.CurrentHP, "EnemyBase must ignore collisions from inactive bullets.");
    }

    [Test]
    public void PlayerBullet_Ignores_Player_Collision_NoSelfDamage()
    {
        // Arrange
        bool returnedToPool = false;
        _playerBullet.Initialize((b) => { returnedToPool = true; });
        int initialLives = _playerHealth.CurrentLives;

        // Act - PlayerBullet receives collision with Player
        var triggerMethod = typeof(PlayerBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var playerCollider = _playerObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_playerBullet, new object[] { playerCollider });

        // Assert
        Assert.AreEqual(initialLives, _playerHealth.CurrentLives, "Player lives must not decrease when colliding with own PlayerBullet.");
        Assert.IsFalse(returnedToPool, "PlayerBullet must not be returned to pool on Player collision.");
        Assert.IsTrue(_playerBulletObj.activeSelf, "PlayerBullet must remain active.");
    }

    [Test]
    public void EnemyBullet_Ignores_Enemy_Collision_NoSelfDamage()
    {
        // Arrange
        bool returnedToPool = false;
        _enemyBullet.Initialize(Vector2.down, 16f, (b) => { returnedToPool = true; });
        int initialHp = _enemyBase.CurrentHP;

        // Act - EnemyBullet receives collision with Enemy
        var triggerMethod = typeof(EnemyBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var enemyCollider = _enemyObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_enemyBullet, new object[] { enemyCollider });

        // Assert
        Assert.AreEqual(initialHp, _enemyBase.CurrentHP, "Enemy HP must not decrease when colliding with own EnemyBullet.");
        Assert.IsFalse(returnedToPool, "EnemyBullet must not be returned to pool on Enemy collision.");
        Assert.IsTrue(_enemyBulletObj.activeSelf, "EnemyBullet must remain active.");
    }

    [Test]
    public void PlayerHealth_Ignores_PlayerBullet_Collision()
    {
        // Arrange
        int initialLives = _playerHealth.CurrentLives;

        // Act - PlayerHealth receives collision from PlayerBullet
        var triggerMethod = typeof(PlayerHealth).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var bulletCollider = _playerBulletObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_playerHealth, new object[] { bulletCollider });

        // Assert
        Assert.AreEqual(initialLives, _playerHealth.CurrentLives, "PlayerHealth must ignore collisions from PlayerBullet.");
    }

    [Test]
    public void EnemyBase_Ignores_EnemyBullet_Collision()
    {
        // Arrange
        int initialHp = _enemyBase.CurrentHP;

        // Act - EnemyBase receives collision from EnemyBullet
        var triggerMethod = typeof(EnemyBase).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        var bulletCollider = _enemyBulletObj.GetComponent<BoxCollider2D>();
        triggerMethod.Invoke(_enemyBase, new object[] { bulletCollider });

        // Assert
        Assert.AreEqual(initialHp, _enemyBase.CurrentHP, "EnemyBase must ignore collisions from EnemyBullet.");
    }

    [Test]
    public void PlayerBullet_Ignores_NamedPlayerObject_Collision()
    {
        // Arrange
        var playerCustomObj = new GameObject("Player_Custom_Ship");
        playerCustomObj.tag = "Untagged"; // Test name matching even without tag
        var col = playerCustomObj.AddComponent<BoxCollider2D>();
        bool returnedToPool = false;
        _playerBullet.Initialize((b) => { returnedToPool = true; });

        // Act
        var triggerMethod = typeof(PlayerBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        triggerMethod.Invoke(_playerBullet, new object[] { col });

        // Assert
        Assert.IsFalse(returnedToPool, "PlayerBullet must ignore objects containing 'Player' in name.");
        Assert.IsTrue(_playerBulletObj.activeSelf, "PlayerBullet must stay active.");

        UnityEngine.Object.DestroyImmediate(playerCustomObj);
    }

    [Test]
    public void EnemyBullet_Ignores_NamedEnemyObject_Collision()
    {
        // Arrange
        var enemyCustomObj = new GameObject("Enemy_Custom_Ship");
        enemyCustomObj.tag = "Untagged"; // Test name matching even without tag
        var col = enemyCustomObj.AddComponent<BoxCollider2D>();
        bool returnedToPool = false;
        _enemyBullet.Initialize(Vector2.down, 16f, (b) => { returnedToPool = true; });

        // Act
        var triggerMethod = typeof(EnemyBullet).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        triggerMethod.Invoke(_enemyBullet, new object[] { col });

        // Assert
        Assert.IsFalse(returnedToPool, "EnemyBullet must ignore objects containing 'Enemy' in name.");
        Assert.IsTrue(_enemyBulletObj.activeSelf, "EnemyBullet must stay active.");

        UnityEngine.Object.DestroyImmediate(enemyCustomObj);
    }

    [Test]
    public void PlayerHealth_Ignores_NamedPlayerBullet_Collision()
    {
        // Arrange
        var bulletCustomObj = new GameObject("PlayerBullet_Clone");
        bulletCustomObj.tag = "Untagged";
        var col = bulletCustomObj.AddComponent<BoxCollider2D>();
        int initialLives = _playerHealth.CurrentLives;

        // Act
        var triggerMethod = typeof(PlayerHealth).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        triggerMethod.Invoke(_playerHealth, new object[] { col });

        // Assert
        Assert.AreEqual(initialLives, _playerHealth.CurrentLives, "PlayerHealth must ignore objects named PlayerBullet.");

        UnityEngine.Object.DestroyImmediate(bulletCustomObj);
    }

    [Test]
    public void EnemyBase_Ignores_NamedEnemyBullet_Collision()
    {
        // Arrange
        var bulletCustomObj = new GameObject("EnemyBullet_Clone");
        bulletCustomObj.tag = "Untagged";
        var col = bulletCustomObj.AddComponent<BoxCollider2D>();
        int initialHp = _enemyBase.CurrentHP;

        // Act
        var triggerMethod = typeof(EnemyBase).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        triggerMethod.Invoke(_enemyBase, new object[] { col });

        // Assert
        Assert.AreEqual(initialHp, _enemyBase.CurrentHP, "EnemyBase must ignore objects named EnemyBullet.");

        UnityEngine.Object.DestroyImmediate(bulletCustomObj);
    }

    #endregion
}
