using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Enemy;

namespace Galaga.Tests
{
    [TestFixture]
    public class BezierCurveTests
    {
        private GameObject _followerObject;
        private BezierPathFollower _follower;

        [SetUp]
        public void SetUp()
        {
            _followerObject = new GameObject("TestFollower");
            _follower = _followerObject.AddComponent<BezierPathFollower>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_followerObject != null)
            {
                Object.DestroyImmediate(_followerObject);
            }
        }

        #region BezierCurve Pure Math Tests

        [Test]
        public void Evaluate_AtStart_ReturnsP0()
        {
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(2f, 5f);
            Vector2 p2 = new Vector2(8f, 5f);
            Vector2 p3 = new Vector2(10f, 0f);

            Vector2 result = BezierCurve.Evaluate(p0, p1, p2, p3, 0f);

            Assert.AreEqual(p0.x, result.x, 0.0001f);
            Assert.AreEqual(p0.y, result.y, 0.0001f);
        }

        [Test]
        public void Evaluate_AtEnd_ReturnsP3()
        {
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(2f, 5f);
            Vector2 p2 = new Vector2(8f, 5f);
            Vector2 p3 = new Vector2(10f, 0f);

            Vector2 result = BezierCurve.Evaluate(p0, p1, p2, p3, 1f);

            Assert.AreEqual(p3.x, result.x, 0.0001f);
            Assert.AreEqual(p3.y, result.y, 0.0001f);
        }

        [Test]
        public void Evaluate_AtMidPoint_CalculatesCorrectCubicFormula()
        {
            // B(0.5) = (1/8)*P0 + (3/8)*P1 + (3/8)*P2 + (1/8)*P3
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 10f);
            Vector2 p2 = new Vector2(10f, 10f);
            Vector2 p3 = new Vector2(10f, 0f);

            // B_x(0.5) = 0.125*0 + 0.375*0 + 0.375*10 + 0.125*10 = 3.75 + 1.25 = 5.0
            // B_y(0.5) = 0.125*0 + 0.375*10 + 0.375*10 + 0.125*0 = 3.75 + 3.75 = 7.5
            Vector2 expected = new Vector2(5.0f, 7.5f);
            Vector2 result = BezierCurve.Evaluate(p0, p1, p2, p3, 0.5f);

            Assert.AreEqual(expected.x, result.x, 0.0001f);
            Assert.AreEqual(expected.y, result.y, 0.0001f);
        }

        [Test]
        public void Evaluate_ClampsTBetweenZeroAndOne()
        {
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(2f, 4f);
            Vector2 p2 = new Vector2(6f, 4f);
            Vector2 p3 = new Vector2(8f, 0f);

            Vector2 negativeTResult = BezierCurve.Evaluate(p0, p1, p2, p3, -0.5f);
            Vector2 overTResult = BezierCurve.Evaluate(p0, p1, p2, p3, 1.5f);

            Assert.AreEqual(p0.x, negativeTResult.x, 0.0001f);
            Assert.AreEqual(p0.y, negativeTResult.y, 0.0001f);
            Assert.AreEqual(p3.x, overTResult.x, 0.0001f);
            Assert.AreEqual(p3.y, overTResult.y, 0.0001f);
        }

        [Test]
        public void GetTangent_AtStart_MatchesNormalizedDerivative()
        {
            // B'(0) = 3 * (P1 - P0)
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 5f);
            Vector2 p2 = new Vector2(5f, 10f);
            Vector2 p3 = new Vector2(10f, 10f);

            Vector2 tangent = BezierCurve.GetTangent(p0, p1, p2, p3, 0f);
            Vector2 expected = (p1 - p0).normalized;

            Assert.AreEqual(expected.x, tangent.x, 0.0001f);
            Assert.AreEqual(expected.y, tangent.y, 0.0001f);
            Assert.AreEqual(1.0f, tangent.magnitude, 0.0001f);
        }

        [Test]
        public void GetTangent_AtEnd_MatchesNormalizedDerivative()
        {
            // B'(1) = 3 * (P3 - P2)
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 5f);
            Vector2 p2 = new Vector2(5f, 10f);
            Vector2 p3 = new Vector2(10f, 10f);

            Vector2 tangent = BezierCurve.GetTangent(p0, p1, p2, p3, 1f);
            Vector2 expected = (p3 - p2).normalized;

            Assert.AreEqual(expected.x, tangent.x, 0.0001f);
            Assert.AreEqual(expected.y, tangent.y, 0.0001f);
            Assert.AreEqual(1.0f, tangent.magnitude, 0.0001f);
        }

        [Test]
        public void GetTangent_ZeroDerivative_FallsBackGracefully()
        {
            // 모든 제어점이 일치할 때
            Vector2 p = new Vector2(5f, 5f);
            Vector2 tangent = BezierCurve.GetTangent(p, p, p, p, 0.5f);

            // 기본 방향 Vector2.up으로 폴백
            Assert.AreEqual(Vector2.up.x, tangent.x, 0.0001f);
            Assert.AreEqual(Vector2.up.y, tangent.y, 0.0001f);
        }

        [Test]
        public void CalculateLength_StraightLine_MatchesEuclideanDistance()
        {
            // 균등 분할된 직선: P0(0,0), P1(0, 3.333), P2(0, 6.666), P3(0, 10)
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 10f / 3f);
            Vector2 p2 = new Vector2(0f, 20f / 3f);
            Vector2 p3 = new Vector2(0f, 10f);

            float length = BezierCurve.CalculateSegmentLength(p0, p1, p2, p3, 50);

            Assert.AreEqual(10f, length, 0.01f);
        }

        [Test]
        public void EvaluatePath_MultiSegment_CalculatesSmoothTransition()
        {
            BezierSegment seg1 = new BezierSegment(
                new Vector2(0f, 0f),
                new Vector2(0f, 5f),
                new Vector2(5f, 5f),
                new Vector2(5f, 0f)
            );

            BezierSegment seg2 = new BezierSegment(
                new Vector2(5f, 0f),
                new Vector2(5f, -5f),
                new Vector2(10f, -5f),
                new Vector2(10f, 0f)
            );

            BezierSegment[] segments = new BezierSegment[] { seg1, seg2 };

            // progress = 0 -> seg1 시작점
            Vector2 start = BezierCurve.EvaluatePath(segments, 0f);
            Assert.AreEqual(0f, start.x, 0.0001f);
            Assert.AreEqual(0f, start.y, 0.0001f);

            // progress = 0.5 -> seg1 종점이자 seg2 시작점 (5, 0)
            Vector2 mid = BezierCurve.EvaluatePath(segments, 0.5f);
            Assert.AreEqual(5f, mid.x, 0.0001f);
            Assert.AreEqual(0f, mid.y, 0.0001f);

            // progress = 1.0 -> seg2 종점 (10, 0)
            Vector2 end = BezierCurve.EvaluatePath(segments, 1.0f);
            Assert.AreEqual(10f, end.x, 0.0001f);
            Assert.AreEqual(0f, end.y, 0.0001f);
        }

        #endregion

        #region BezierPathFollower Component Tests

        [Test]
        public void Follower_MovesAlongPath_WithDeltaTime()
        {
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 10f / 3f);
            Vector2 p2 = new Vector2(0f, 20f / 3f);
            Vector2 p3 = new Vector2(0f, 10f);

            _follower.SetPath(p0, p1, p2, p3, speed: 5f);
            _follower.Play();

            Assert.IsTrue(_follower.IsPlaying);
            Assert.AreEqual(0f, _follower.Progress, 0.0001f);
            Assert.AreEqual(0f, _followerObject.transform.position.y, 0.0001f);

            // 1초 후 이동 거리 = 5f (총 길이 10f 중 절반 = 0.5 progress)
            _follower.UpdatePathFollow(1.0f);

            Assert.AreEqual(0.5f, _follower.Progress, 0.01f);
            Assert.AreEqual(5.0f, _followerObject.transform.position.y, 0.1f);
        }

        [Test]
        public void Follower_FiresCompletionEvent_WhenReachingEnd()
        {
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 10f / 3f);
            Vector2 p2 = new Vector2(0f, 20f / 3f);
            Vector2 p3 = new Vector2(0f, 10f);

            bool completedFired = false;
            _follower.OnPathCompleted += () => completedFired = true;

            _follower.SetPath(p0, p1, p2, p3, speed: 10f);
            _follower.Play();

            // 1초 이동 (총 거리 10f 도달)
            _follower.UpdatePathFollow(1.1f);

            Assert.IsTrue(completedFired);
            Assert.IsFalse(_follower.IsPlaying);
            Assert.AreEqual(1.0f, _follower.Progress, 0.0001f);
            Assert.AreEqual(10f, _followerObject.transform.position.y, 0.0001f);
        }

        [Test]
        public void Follower_Loops_WhenLoopEnabled()
        {
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 10f / 3f);
            Vector2 p2 = new Vector2(0f, 20f / 3f);
            Vector2 p3 = new Vector2(0f, 10f);

            _follower.SetPath(p0, p1, p2, p3, speed: 10f, loop: true);
            _follower.Play();

            // 1.2초 이동 -> 1바퀴 돌고 0.2초 분량 (progress 0.2)
            _follower.UpdatePathFollow(1.2f);

            Assert.IsTrue(_follower.IsPlaying);
            Assert.AreEqual(0.2f, _follower.Progress, 0.02f);
        }

        [Test]
        public void Follower_RotatesAlongTangent_WhenRotateAlongPathEnabled()
        {
            // 상향(+Y) 직선 이동
            Vector2 p0 = new Vector2(0f, 0f);
            Vector2 p1 = new Vector2(0f, 2f);
            Vector2 p2 = new Vector2(0f, 4f);
            Vector2 p3 = new Vector2(0f, 6f);

            _follower.RotateAlongPath = true;
            _follower.RotationOffset = -90f;
            _follower.SetPath(p0, p1, p2, p3, speed: 5f);
            _follower.Play();

            // 접선은 (0, 1) -> Atan2(1, 0) = 90도 -> 90 + (-90) = 0도
            Assert.AreEqual(0f, _followerObject.transform.eulerAngles.z, 0.01f);
        }

        #endregion
    }
}
