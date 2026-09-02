using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;

namespace Galaga.Tests
{
    [TestFixture]
    public class BulletPoolTests
    {
        private GameObject _cameraObject;
        private PlayAreaManager _playAreaManager;
        private GameObject _playerObject;
        private PlayerShooting _playerShooting;
        private GameObject _bulletPrefab;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            Camera cam = _cameraObject.AddComponent<Camera>();
            _playAreaManager = _cameraObject.AddComponent<PlayAreaManager>();
            _playAreaManager.RecalculateBounds();

            _bulletPrefab = new GameObject("TestBulletPrefab");
            _bulletPrefab.AddComponent<PlayerBullet>();
            _bulletPrefab.SetActive(false);

            _playerObject = new GameObject("TestPlayer");
            _playerShooting = _playerObject.AddComponent<PlayerShooting>();
            _playerShooting.BulletPrefab = _bulletPrefab;
            _playerShooting.PlayAreaManager = _playAreaManager;
            _playerShooting.FireCooldown = 0f;
            _playerShooting.InitializePool();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }
            if (_bulletPrefab != null)
            {
                Object.DestroyImmediate(_bulletPrefab);
            }
            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
            }

            GameObject poolRoot = GameObject.Find("PlayerBulletPool");
            if (poolRoot != null)
            {
                Object.DestroyImmediate(poolRoot);
            }
        }

        [Test]
        public void SingleFighter_CanShoot_MaxTwoBulletsSimultaneously()
        {
            Assert.AreEqual(0, _playerShooting.ActiveBulletCount);

            bool firstShot = _playerShooting.TryFire();
            Assert.IsTrue(firstShot, "1번째 탄환 발사는 성공해야 합니다.");
            Assert.AreEqual(1, _playerShooting.ActiveBulletCount, "1번째 발사 후 활성 탄환 수는 1이어야 합니다.");

            bool secondShot = _playerShooting.TryFire();
            Assert.IsTrue(secondShot, "2번째 탄환 발사는 성공해야 합니다.");
            Assert.AreEqual(2, _playerShooting.ActiveBulletCount, "2번째 발사 후 활성 탄환 수는 2여야 합니다.");

            bool thirdShot = _playerShooting.TryFire();
            Assert.IsFalse(thirdShot, "화면 내 최대 2발 제한으로 3번째 발사는 실패해야 합니다.");
            Assert.AreEqual(2, _playerShooting.ActiveBulletCount, "활성 탄환 수는 2로 유지되어야 합니다.");
        }

        [Test]
        public void Bullet_ReturnsToPool_WhenExceedingMaxY()
        {
            _playerShooting.TryFire();
            _playerShooting.TryFire();
            Assert.AreEqual(2, _playerShooting.ActiveBulletCount);

            // 풀에 생성된 탄환들을 찾아서 화면 상단 밖으로 이동 후 Move 시뮬레이션
            PlayerBullet[] bullets = Object.FindObjectsByType<PlayerBullet>(FindObjectsSortMode.None);
            foreach (var bullet in bullets)
            {
                if (bullet.gameObject.activeSelf)
                {
                    bullet.transform.position = new Vector3(0f, _playAreaManager.MaxY + 1f, 0f);
                    bullet.Move(0.1f);
                }
            }

            Assert.AreEqual(0, _playerShooting.ActiveBulletCount, "상단 경계를 벗어난 탄환은 풀로 회수되어 활성 수가 0이어야 합니다.");
        }

        [Test]
        public void Bullet_CanShootAgain_AfterReturnedToPool()
        {
            _playerShooting.TryFire();
            _playerShooting.TryFire();
            Assert.AreEqual(2, _playerShooting.ActiveBulletCount);

            // 1발을 수동으로 풀에 반환
            PlayerBullet[] bullets = Object.FindObjectsByType<PlayerBullet>(FindObjectsSortMode.None);
            PlayerBullet activeBullet = null;
            foreach (var bullet in bullets)
            {
                if (bullet.gameObject.activeSelf)
                {
                    activeBullet = bullet;
                    break;
                }
            }

            Assert.IsNotNull(activeBullet, "활성화된 탄환이 존재해야 합니다.");
            activeBullet.ReturnToPool();

            Assert.AreEqual(1, _playerShooting.ActiveBulletCount, "1발 반환 후 활성 탄환 수는 1이어야 합니다.");

            // 다시 1발 발사 가능해야 함
            bool shotAgain = _playerShooting.TryFire();
            Assert.IsTrue(shotAgain, "탄환 회수 후 다시 발사가 가능해야 합니다.");
            Assert.AreEqual(2, _playerShooting.ActiveBulletCount, "재발사 후 활성 탄환 수는 2여야 합니다.");
        }

        [Test]
        public void Bullet_MovesUpward_WithCorrectVelocity()
        {
            GameObject singleBulletObj = new GameObject("VelocityTestBullet");
            PlayerBullet bullet = singleBulletObj.AddComponent<PlayerBullet>();
            bullet.Speed = 27.7f;
            bullet.transform.position = Vector3.zero;

            bullet.Move(1.0f);

            Assert.AreEqual(0f, bullet.transform.position.x, 0.001f);
            Assert.AreEqual(27.7f, bullet.transform.position.y, 0.01f);

            Object.DestroyImmediate(singleBulletObj);
        }

        [Test]
        public void DualFighter_CanShoot_MaxFourBulletsSimultaneously()
        {
            _playerShooting.IsDualFighter = true;
            Assert.AreEqual(4, _playerShooting.MaxSimultaneousBullets);

            // 듀얼 파이터는 1회 사격 시 2발 발사
            bool firstShot = _playerShooting.TryFire();
            Assert.IsTrue(firstShot, "듀얼 파이터 1차 사격 성공");
            Assert.AreEqual(2, _playerShooting.ActiveBulletCount, "1차 사격 후 탄환 수 2");

            bool secondShot = _playerShooting.TryFire();
            Assert.IsTrue(secondShot, "듀얼 파이터 2차 사격 성공");
            Assert.AreEqual(4, _playerShooting.ActiveBulletCount, "2차 사격 후 탄환 수 4");

            bool thirdShot = _playerShooting.TryFire();
            Assert.IsFalse(thirdShot, "최대 4발 도달 시 추가 사격 불가");
            Assert.AreEqual(4, _playerShooting.ActiveBulletCount);
        }
    }
}
