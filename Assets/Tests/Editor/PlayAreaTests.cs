using NUnit.Framework;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Tests
{
    [TestFixture]
    public class PlayAreaTests
    {
        private GameObject _cameraObject;
        private Camera _camera;
        private PlayAreaManager _playAreaManager;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            _camera = _cameraObject.AddComponent<Camera>();
            _playAreaManager = _cameraObject.AddComponent<PlayAreaManager>();
            _playAreaManager.RecalculateBounds();
        }

        [TearDown]
        public void TearDown()
        {
            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
            }
        }

        [Test]
        public void TargetAspectRatio_ShouldBeCalculatedCorrectly_For224x288()
        {
            // 224 / 288 = 7 / 9 approx 0.7777778
            float expectedAspect = 224f / 288f;
            Assert.AreEqual(expectedAspect, _playAreaManager.TargetAspectRatio, 0.001f);
        }

        [Test]
        public void ClampPosition_ShouldClampInsideBounds()
        {
            float minX = _playAreaManager.MinX;
            float maxX = _playAreaManager.MaxX;

            Vector2 farLeft = new Vector2(minX - 10f, 0f);
            Vector2 clamped = _playAreaManager.ClampPosition(farLeft);

            Assert.AreEqual(minX, clamped.x, 0.001f);
            Assert.AreEqual(0f, clamped.y, 0.001f);

            Vector2 farRight = new Vector2(maxX + 10f, 0f);
            clamped = _playAreaManager.ClampPosition(farRight);

            Assert.AreEqual(maxX, clamped.x, 0.001f);
        }

        [Test]
        public void IsOutOfBounds_ShouldReturnTrue_WhenOutsideMargin()
        {
            Vector2 outsidePos = new Vector2(_playAreaManager.MaxX + 5f, 0f);
            Assert.IsTrue(_playAreaManager.IsOutOfBounds(outsidePos, 0.5f));

            Vector2 insidePos = new Vector2(0f, 0f);
            Assert.IsFalse(_playAreaManager.IsOutOfBounds(insidePos, 0.5f));
        }
    }
}
