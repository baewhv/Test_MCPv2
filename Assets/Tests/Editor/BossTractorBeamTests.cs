using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    [TestFixture]
    public class BossTractorBeamTests
    {
        private GameObject _bossObject;
        private EnemyBase _enemyBase;
        private EnemyBoss _enemyBoss;
        private BossTractorBeam _tractorBeam;

        [SetUp]
        public void SetUp()
        {
            _bossObject = new GameObject("TestBoss");
            _enemyBase = _bossObject.AddComponent<EnemyBase>();
            _enemyBoss = _bossObject.AddComponent<EnemyBoss>();

            GameObject beamObject = new GameObject("TractorBeam");
            beamObject.transform.SetParent(_bossObject.transform);
            _tractorBeam = beamObject.AddComponent<BossTractorBeam>();

            _enemyBoss.TractorBeam = _tractorBeam;
            _tractorBeam.OwnerBoss = _enemyBase;
        }

        [TearDown]
        public void TearDown()
        {
            if (_bossObject != null)
            {
                Object.DestroyImmediate(_bossObject);
            }
        }

        [Test]
        public void BossTractorBeam_DefaultDimensions_MatchesTechSpec()
        {
            // 상단 0.556u (8px), 하단 3.333u (48px), 높이 8.333u (120px)
            Assert.AreEqual(0.556f, _tractorBeam.TopWidth, 0.001f);
            Assert.AreEqual(3.333f, _tractorBeam.BottomWidth, 0.001f);
            Assert.AreEqual(8.333f, _tractorBeam.BeamHeight, 0.001f);
            Assert.AreEqual(4.0f, _tractorBeam.BeamDuration, 0.001f);
        }

        [Test]
        public void BossTractorBeam_GetLocalVertices_ReturnsAccurateTrapezoidPoints()
        {
            Vector2[] vertices = _tractorBeam.GetLocalVertices();

            Assert.IsNotNull(vertices);
            Assert.AreEqual(4, vertices.Length, "Must have 4 vertices for trapezoid");

            // Point 0: Top-Left (-0.278f, 0f)
            Assert.AreEqual(-0.278f, vertices[0].x, 0.001f);
            Assert.AreEqual(0f, vertices[0].y, 0.001f);

            // Point 1: Top-Right (0.278f, 0f)
            Assert.AreEqual(0.278f, vertices[1].x, 0.001f);
            Assert.AreEqual(0f, vertices[1].y, 0.001f);

            // Point 2: Bottom-Right (1.667f, -8.333f)
            Assert.AreEqual(1.667f, vertices[2].x, 0.001f);
            Assert.AreEqual(-8.333f, vertices[2].y, 0.001f);

            // Point 3: Bottom-Left (-1.667f, -8.333f)
            Assert.AreEqual(-1.667f, vertices[3].x, 0.001f);
            Assert.AreEqual(-8.333f, vertices[3].y, 0.001f);
        }

        [Test]
        public void BossTractorBeam_ActivateAndDeactivate_UpdatesStateAndEvents()
        {
            bool activatedCalled = false;
            bool deactivatedCalled = false;

            _tractorBeam.OnBeamActivated += () => activatedCalled = true;
            _tractorBeam.OnBeamDeactivated += () => deactivatedCalled = true;

            Assert.IsFalse(_tractorBeam.IsBeamActive);

            _tractorBeam.ActivateBeam(4.0f);
            Assert.IsTrue(_tractorBeam.IsBeamActive);
            Assert.IsTrue(activatedCalled);

            _tractorBeam.DeactivateBeam();
            Assert.IsFalse(_tractorBeam.IsBeamActive);
            Assert.IsTrue(deactivatedCalled);
        }

        [Test]
        public void EnemyBoss_StartTractorBeam_SetsEnemyStateToTractorBeam()
        {
            _enemyBoss.StartTractorBeam();

            Assert.AreEqual(EnemyState.TractorBeam, _enemyBase.CurrentState);
            Assert.IsTrue(_tractorBeam.IsBeamActive);
        }

        [Test]
        public void EnemyBoss_AttachAndDetachCapturedFighter_ManagesSlotAndState()
        {
            GameObject fighterObj = new GameObject("CapturedFighter");
            EnemyBase fighterEnemy = fighterObj.AddComponent<EnemyBase>();

            Assert.IsFalse(_enemyBoss.HasCapturedFighter);
            Assert.IsNull(_enemyBoss.CapturedFighter);

            _enemyBoss.AttachCapturedFighter(fighterEnemy);

            Assert.IsTrue(_enemyBoss.HasCapturedFighter);
            Assert.AreEqual(fighterEnemy, _enemyBoss.CapturedFighter);
            Assert.AreEqual(_enemyBoss.CapturedFighterSlot, fighterEnemy.transform.parent);

            EnemyBase detached = _enemyBoss.DetachCapturedFighter();

            Assert.IsFalse(_enemyBoss.HasCapturedFighter);
            Assert.IsNull(_enemyBoss.CapturedFighter);
            Assert.AreEqual(fighterEnemy, detached);
            Assert.IsNull(fighterEnemy.transform.parent);

            Object.DestroyImmediate(fighterObj);
        }

        [Test]
        public void CreateBossTractorHoverTrajectory_GeneratesValidPathToHoverY()
        {
            Vector2 startPos = new Vector2(0f, 6.0f);
            Vector2 hoverPos = new Vector2(1.5f, 1.0f);

            BezierSegment[] segments = EnemyDiveController.CreateBossTractorHoverTrajectory(startPos, hoverPos);

            Assert.IsNotNull(segments);
            Assert.AreEqual(2, segments.Length, "Hover trajectory must have 2 segments");

            // Segment 1 시작점 검증
            Assert.AreEqual(startPos.x, segments[0].p0.x, 0.001f);
            Assert.AreEqual(startPos.y, segments[0].p0.y, 0.001f);

            // Segment 2 종점 검증 (호버링 타겟 도달)
            Assert.AreEqual(hoverPos.x, segments[1].p3.x, 0.001f);
            Assert.AreEqual(hoverPos.y, segments[1].p3.y, 0.001f);
        }

        [Test]
        public void CreateBossPostBeamDiveTrajectory_GeneratesDownwardSwoopToScreenBottom()
        {
            Vector2 hoverPos = new Vector2(1.5f, 1.0f);
            Vector2 playerPos = new Vector2(2.0f, -8.0f);
            float screenBottomY = -11.0f;

            BezierSegment[] segments = EnemyDiveController.CreateBossPostBeamDiveTrajectory(hoverPos, playerPos, screenBottomY);

            Assert.IsNotNull(segments);
            Assert.AreEqual(1, segments.Length);

            // 시작점 (호버 위치)
            Assert.AreEqual(hoverPos.x, segments[0].p0.x, 0.001f);
            Assert.AreEqual(hoverPos.y, segments[0].p0.y, 0.001f);

            // 종점 (플레이어 X 좌표 및 화면 하단)
            Assert.AreEqual(playerPos.x, segments[0].p3.x, 0.001f);
            Assert.AreEqual(screenBottomY, segments[0].p3.y, 0.001f);
        }
    }
}
