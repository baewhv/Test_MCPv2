using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Enemy;

namespace Galaga.Tests
{
    [TestFixture]
    public class EnemyDiveTests
    {
        private GameObject _gridObject;
        private FormationGridManager _gridManager;
        private EnemyDiveController _diveController;

        [SetUp]
        public void SetUp()
        {
            _gridObject = new GameObject("TestFormationGrid");
            _gridManager = _gridObject.AddComponent<FormationGridManager>();
            _gridManager.InitializeGrid();

            _diveController = _gridObject.AddComponent<EnemyDiveController>();
            _diveController.GridManager = _gridManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_gridObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_gridObject);
            }
        }

        [Test]
        public void CreateSingleDiveTrajectory_GeneratesValidSegmentsEndingAtScreenBottom()
        {
            Vector2 startPos = new Vector2(2f, 5f);
            Vector2 playerPos = new Vector2(-1f, -8f);
            float screenBottomY = -11f;

            BezierSegment[] trajectory = EnemyDiveController.CreateSingleDiveTrajectory(startPos, playerPos, screenBottomY);

            Assert.IsNotNull(trajectory);
            Assert.AreEqual(2, trajectory.Length, "Dive trajectory must have 2 segments");

            // Segment 1 시작점 검증
            Assert.AreEqual(startPos.x, trajectory[0].p0.x, 0.0001f);
            Assert.AreEqual(startPos.y, trajectory[0].p0.y, 0.0001f);

            // Segment 2 종점 검증 (플레이어 X좌표와 일치하며 화면 하단 도달)
            BezierSegment lastSeg = trajectory[1];
            Assert.AreEqual(playerPos.x, lastSeg.p3.x, 0.0001f);
            Assert.AreEqual(screenBottomY, lastSeg.p3.y, 0.0001f);
        }

        [Test]
        public void CreateEscortDiveTrajectory_GeneratesParallelTrajectoryWithOffset()
        {
            Vector2 bossStartPos = new Vector2(0f, 6f);
            Vector2 escortStartPos = new Vector2(-1f, 5f);
            Vector2 playerPos = new Vector2(0f, -8f);
            Vector2 escortOffset = new Vector2(-1.2f, 0.6f);

            BezierSegment[] escortPath = EnemyDiveController.CreateEscortDiveTrajectory(
                escortStartPos, bossStartPos, playerPos, escortOffset, -11f
            );

            Assert.IsNotNull(escortPath);
            Assert.AreEqual(2, escortPath.Length);

            // 호위기 시작점 검증
            Assert.AreEqual(escortStartPos.x, escortPath[0].p0.x, 0.0001f);
            Assert.AreEqual(escortStartPos.y, escortPath[0].p0.y, 0.0001f);

            // 종점 오프셋 검증
            Assert.AreEqual(playerPos.x + escortOffset.x, escortPath[1].p3.x, 0.0001f);
            Assert.AreEqual(-11f + escortOffset.y, escortPath[1].p3.y, 0.0001f);
        }

        [Test]
        public void CreateReturnTrajectory_GeneratesValidPathFromTopToSlot()
        {
            Vector2 entryPos = new Vector2(1f, 11f);
            Vector2 targetSlotPos = new Vector2(1.5f, 4.5f);

            BezierSegment[] returnPath = EnemyDiveController.CreateReturnTrajectory(entryPos, targetSlotPos);

            Assert.IsNotNull(returnPath);
            Assert.AreEqual(1, returnPath.Length);
            Assert.AreEqual(entryPos.x, returnPath[0].p0.x, 0.0001f);
            Assert.AreEqual(entryPos.y, returnPath[0].p0.y, 0.0001f);
            Assert.AreEqual(targetSlotPos.x, returnPath[0].p3.x, 0.0001f);
            Assert.AreEqual(targetSlotPos.y, returnPath[0].p3.y, 0.0001f);
        }

        [Test]
        public void EnemyShooting_CanShoot_EvaluatesProgressBounds()
        {
            // 진행도 0.1 (사격 범위 밖: false)
            Assert.IsFalse(EnemyShooting.CanShoot(0.1f, 0, 1, 0.3f, 0.6f));

            // 진행도 0.45 (사격 범위 내: true)
            Assert.IsTrue(EnemyShooting.CanShoot(0.45f, 0, 1, 0.3f, 0.6f));

            // 진행도 0.8 (사격 범위 초과: false)
            Assert.IsFalse(EnemyShooting.CanShoot(0.8f, 0, 1, 0.3f, 0.6f));

            // 이미 최대 발사 수를 쐈을 때 (false)
            Assert.IsFalse(EnemyShooting.CanShoot(0.45f, 1, 1, 0.3f, 0.6f));
        }

        [Test]
        public void EnemyDiveController_FindAvailableEscortsForBoss_SelectsClosestGoei()
        {
            // 보스 기체 생성 (Row 0, Col 1)
            GameObject bossObj = new GameObject("Boss");
            EnemyBase boss = bossObj.AddComponent<EnemyBase>();
            _gridManager.AssignEnemyToSlot(0, 1, boss);
            boss.EnterFormation();

            // 고에이 3기 생성 (Row 1, Col 1, Col 2, Col 7)
            GameObject goei1Obj = new GameObject("Goei1");
            EnemyBase goei1 = goei1Obj.AddComponent<EnemyBase>();
            _gridManager.AssignEnemyToSlot(1, 1, goei1);
            goei1.EnterFormation();

            GameObject goei2Obj = new GameObject("Goei2");
            EnemyBase goei2 = goei2Obj.AddComponent<EnemyBase>();
            _gridManager.AssignEnemyToSlot(1, 2, goei2);
            goei2.EnterFormation();

            GameObject goei3Obj = new GameObject("Goei3");
            EnemyBase goei3 = goei3Obj.AddComponent<EnemyBase>();
            _gridManager.AssignEnemyToSlot(1, 7, goei3);
            goei3.EnterFormation();

            List<EnemyBase> escorts = _diveController.FindAvailableEscortsForBoss(boss, 2);

            Assert.IsNotNull(escorts);
            Assert.AreEqual(2, escorts.Count);
            // Col 1과 Col 2의 고에이가 선택되어야 함
            Assert.Contains(goei1, escorts);
            Assert.Contains(goei2, escorts);
            Assert.IsFalse(escorts.Contains(goei3));

            UnityEngine.Object.DestroyImmediate(bossObj);
            UnityEngine.Object.DestroyImmediate(goei1Obj);
            UnityEngine.Object.DestroyImmediate(goei2Obj);
            UnityEngine.Object.DestroyImmediate(goei3Obj);
        }
    }
}
