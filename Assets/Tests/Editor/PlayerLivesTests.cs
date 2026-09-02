using NUnit.Framework;
using UnityEngine;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    [TestFixture]
    public class PlayerLivesTests
    {
        private GameObject _playerObject;
        private PlayerHealth _playerHealth;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("TestPlayer");
            _playerHealth = _playerObject.AddComponent<PlayerHealth>();
            _playerHealth.Initialize(3);
            _playerHealth.RespawnPosition = new Vector3(0f, -8f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }
        }

        [Test]
        public void PlayerHealth_InitializesWithStartingLives_Three()
        {
            Assert.AreEqual(3, _playerHealth.CurrentLives);
            Assert.IsFalse(_playerHealth.IsDead);
            Assert.IsFalse(_playerHealth.IsInvincible);
        }

        [Test]
        public void TakeDamage_DecreasesLifeByOne_AndRespawns()
        {
            _playerObject.transform.position = new Vector3(5f, -8f, 0f);

            bool damaged = _playerHealth.TakeDamage(1);

            Assert.IsTrue(damaged);
            Assert.AreEqual(2, _playerHealth.CurrentLives);
            Assert.AreEqual(new Vector3(0f, -8f, 0f), _playerObject.transform.position);
            Assert.IsTrue(_playerHealth.IsInvincible);
        }

        [Test]
        public void TakeDamage_IsIgnored_WhenInvincible()
        {
            _playerHealth.SetInvincibleDirectly(true);

            bool damaged = _playerHealth.TakeDamage(1);

            Assert.IsFalse(damaged);
            Assert.AreEqual(3, _playerHealth.CurrentLives);
        }

        [Test]
        public void TakeDamage_TriggersDeathEvent_WhenLivesReachZero()
        {
            bool diedEventTriggered = false;
            _playerHealth.OnPlayerDied += () => { diedEventTriggered = true; };

            _playerHealth.SetLives(1);
            _playerHealth.SetInvincibleDirectly(false);

            bool damaged = _playerHealth.TakeDamage(1);

            Assert.IsTrue(damaged);
            Assert.AreEqual(0, _playerHealth.CurrentLives);
            Assert.IsTrue(_playerHealth.IsDead);
            Assert.IsTrue(diedEventTriggered);
        }

        [Test]
        public void AddLife_IncreasesLife_UpToMax()
        {
            int receivedLives = 0;
            _playerHealth.OnLivesChanged += (lives) => { receivedLives = lives; };

            _playerHealth.AddLife(1);

            Assert.AreEqual(4, _playerHealth.CurrentLives);
            Assert.AreEqual(4, receivedLives);

            // 최대 6기 초과 방지 검증
            _playerHealth.AddLife(10);
            Assert.AreEqual(6, _playerHealth.CurrentLives);
        }
    }
}
