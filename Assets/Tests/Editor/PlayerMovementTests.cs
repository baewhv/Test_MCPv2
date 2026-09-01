using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    [TestFixture]
    public class PlayerMovementTests
    {
        private GameObject _cameraObject;
        private PlayAreaManager _playAreaManager;
        private GameObject _playerObject;
        private PlayerController _playerController;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            Camera cam = _cameraObject.AddComponent<Camera>();
            _playAreaManager = _cameraObject.AddComponent<PlayAreaManager>();
            _playAreaManager.RecalculateBounds();

            _playerObject = new GameObject("TestPlayer");
            _playerController = _playerObject.AddComponent<PlayerController>();
            _playerController.PlayAreaManager = _playAreaManager;
            _playerController.MoveSpeed = 10f;
            _playerController.HalfWidth = 0.5f;
            _playerController.FixedYPosition = -8f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }
            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
            }
        }

        [Test]
        public void Player_MovesRight_WhenInputPositive()
        {
            _playerObject.transform.position = new Vector3(0f, -8f, 0f);
            _playerController.SetInputDirectly(1.0f);

            _playerController.Move(0.1f); // 1.0 * 10 * 0.1 = 1.0

            Assert.AreEqual(1.0f, _playerObject.transform.position.x, 0.001f);
            Assert.AreEqual(-8.0f, _playerObject.transform.position.y, 0.001f);
        }

        [Test]
        public void Player_MovesLeft_WhenInputNegative()
        {
            _playerObject.transform.position = new Vector3(0f, -8f, 0f);
            _playerController.SetInputDirectly(-1.0f);

            _playerController.Move(0.1f); // -1.0 * 10 * 0.1 = -1.0

            Assert.AreEqual(-1.0f, _playerObject.transform.position.x, 0.001f);
            Assert.AreEqual(-8.0f, _playerObject.transform.position.y, 0.001f);
        }

        [Test]
        public void Player_DoesNotExceed_LeftBoundary()
        {
            _playerObject.transform.position = new Vector3(0f, -8f, 0f);
            _playerController.SetInputDirectly(-1.0f);

            // 10초간 좌측으로 전진 이동 (100 units 이동 시도)
            _playerController.Move(10f);

            float expectedMinX = _playAreaManager.MinX + _playerController.HalfWidth;
            Assert.AreEqual(expectedMinX, _playerObject.transform.position.x, 0.001f);
        }

        [Test]
        public void Player_DoesNotExceed_RightBoundary()
        {
            _playerObject.transform.position = new Vector3(0f, -8f, 0f);
            _playerController.SetInputDirectly(1.0f);

            // 10초간 우측으로 전진 이동 (100 units 이동 시도)
            _playerController.Move(10f);

            float expectedMaxX = _playAreaManager.MaxX - _playerController.HalfWidth;
            Assert.AreEqual(expectedMaxX, _playerObject.transform.position.x, 0.001f);
        }
    }
}
