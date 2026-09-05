using NUnit.Framework;
using UnityEngine;
using Galaga.Core;

namespace Galaga.Tests
{
    [TestFixture]
    public class PlayAreaManagerTests
    {
        private GameObject _managerObject;
        private GameObject _cameraObject;
        private Camera _camera;
        private PlayAreaManager _playAreaManager;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("MainCamera");
            _cameraObject.tag = "MainCamera";
            _camera = _cameraObject.AddComponent<Camera>();
            _camera.transform.position = new Vector3(0f, 0f, -10f);

            _managerObject = new GameObject("PlayAreaManager");
            _playAreaManager = _managerObject.AddComponent<PlayAreaManager>();
            _playAreaManager.TargetCamera = _camera;
            _playAreaManager.RecalculateBounds();
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerObject != null)
            {
                Object.DestroyImmediate(_managerObject);
            }

            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
            }
        }

        [Test]
        public void Instantiate_AsStandaloneObject_WithoutCameraComponent_Succeeds()
        {
            GameObject standaloneObj = new GameObject("StandaloneManager");
            PlayAreaManager manager = standaloneObj.AddComponent<PlayAreaManager>();

            Assert.IsNotNull(manager);
            Assert.IsNull(standaloneObj.GetComponent<Camera>(), "PlayAreaManager should not require Camera on the same GameObject.");

            Object.DestroyImmediate(standaloneObj);
        }

        [Test]
        public void TargetAspectRatio_ShouldBeCalculatedCorrectly_For224x288()
        {
            // 224 / 288 = 7 / 9 approx 0.7777778
            float expectedAspect = 224f / 288f;
            Assert.AreEqual(expectedAspect, _playAreaManager.TargetAspectRatio, 0.001f);
        }

        [Test]
        public void TargetCamera_FallbackToMainCamera_WhenUnassigned()
        {
            GameObject standaloneObj = new GameObject("FallbackManager");
            PlayAreaManager manager = standaloneObj.AddComponent<PlayAreaManager>();
            manager.RecalculateBounds();

            Assert.IsNotNull(manager.TargetCamera);
            Assert.AreEqual(Camera.main, manager.TargetCamera);

            Object.DestroyImmediate(standaloneObj);
        }

        [Test]
        public void RecalculateBounds_CalculatesAccurateWorldBounds_WithOrthographicCamera()
        {
            _playAreaManager.RecalculateBounds();

            float expectedHeight = 20f; // 10 * 2
            float expectedWidth = 20f * (224f / 288f); // approx 15.55556f
            float expectedMinX = -(expectedWidth * 0.5f); // approx -7.77778f
            float expectedMaxX = (expectedWidth * 0.5f); // approx 7.77778f

            Assert.AreEqual(expectedHeight, _playAreaManager.PlayHeight, 0.01f);
            Assert.AreEqual(expectedWidth, _playAreaManager.PlayWidth, 0.01f);
            Assert.AreEqual(expectedMinX, _playAreaManager.MinX, 0.01f);
            Assert.AreEqual(expectedMaxX, _playAreaManager.MaxX, 0.01f);
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
        public void ClampPosition_WithHalfWidth_ClampsCorrectly()
        {
            float halfWidth = 0.5f;
            float expectedMinX = _playAreaManager.MinX + halfWidth;
            float expectedMaxX = _playAreaManager.MaxX - halfWidth;

            Vector2 farLeft = new Vector2(-100f, 0f);
            Vector2 clamped = _playAreaManager.ClampPosition(farLeft, halfWidth, 0f);
            Assert.AreEqual(expectedMinX, clamped.x, 0.001f);

            Vector2 farRight = new Vector2(100f, 0f);
            clamped = _playAreaManager.ClampPosition(farRight, halfWidth, 0f);
            Assert.AreEqual(expectedMaxX, clamped.x, 0.001f);
        }

        [Test]
        public void IsOutOfBounds_ShouldReturnTrue_WhenOutsideMargin()
        {
            Vector2 outsidePos = new Vector2(_playAreaManager.MaxX + 5f, 0f);
            Assert.IsTrue(_playAreaManager.IsOutOfBounds(outsidePos, 0.5f));

            Vector2 insidePos = new Vector2(0f, 0f);
            Assert.IsFalse(_playAreaManager.IsOutOfBounds(insidePos, 0.5f));
        }

        [Test]
        public void CreateBoundaryColliders_CreatesColliders_WithBoundaryTag()
        {
            _playAreaManager.CreateBoundaryColliders();

            Transform borderRoot = _managerObject.transform.Find("BoundaryColliders");
            Assert.IsNotNull(borderRoot, "BoundaryColliders root transform should be created under PlayAreaManager.");

            string[] borderNames = { "LeftBorder", "RightBorder", "TopBorder", "BottomBorder" };
            foreach (string borderName in borderNames)
            {
                Transform border = borderRoot.Find(borderName);
                Assert.IsNotNull(border, $"Border {borderName} should exist.");
                Assert.AreEqual("Boundary", border.tag, $"Border {borderName} must have 'Boundary' tag.");

                BoxCollider2D box = border.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(box, $"Border {borderName} must have BoxCollider2D.");
                Assert.IsTrue(box.isTrigger, $"Border {borderName} BoxCollider2D must be trigger.");
            }
        }
    }
}
