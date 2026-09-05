using System;
using System.Collections.Generic;
using UnityEngine;

namespace Galaga.Core
{
    /// <summary>
    /// 단일 3차 베지어 곡선 세그먼트 제어점을 표현하는 직렬화 가능 구조체입니다.
    /// </summary>
    [Serializable]
    public struct BezierSegment
    {
        [Tooltip("곡선 시작점 (P0)")]
        public Vector2 p0;

        [Tooltip("시작 제어점 (P1)")]
        public Vector2 p1;

        [Tooltip("도착 제어점 (P2)")]
        public Vector2 p2;

        [Tooltip("곡선 도착점 (P3)")]
        public Vector2 p3;

        public BezierSegment(Vector2 start, Vector2 control1, Vector2 control2, Vector2 end)
        {
            p0 = start;
            p1 = control1;
            p2 = control2;
            p3 = end;
        }

        /// <summary>
        /// 진행도 t(0~1)에서의 2D 좌표를 계산합니다.
        /// </summary>
        public Vector2 Evaluate(float t)
        {
            return BezierCurve.Evaluate(p0, p1, p2, p3, t);
        }

        /// <summary>
        /// 진행도 t(0~1)에서의 정규화된 진행 방향 접선 벡터를 계산합니다.
        /// </summary>
        public Vector2 GetTangent(float t)
        {
            return BezierCurve.GetTangent(p0, p1, p2, p3, t);
        }

        /// <summary>
        /// 곡선 세그먼트의 근사 호 길이(Arc Length)를 계산합니다.
        /// </summary>
        public float CalculateLength(int samples = 20)
        {
            return BezierCurve.CalculateSegmentLength(p0, p1, p2, p3, samples);
        }
    }

    /// <summary>
    /// 여러 개의 3차 베지어 곡선 세그먼트를 순차적으로 연결한 경로 데이터 클래스입니다.
    /// </summary>
    [Serializable]
    public class BezierPath
    {
        [SerializeField] private List<BezierSegment> _segments = new List<BezierSegment>();

        public List<BezierSegment> Segments => _segments;
        public int SegmentCount => _segments != null ? _segments.Count : 0;

        public BezierPath()
        {
            _segments = new List<BezierSegment>();
        }

        public BezierPath(IEnumerable<BezierSegment> segments)
        {
            _segments = segments != null ? new List<BezierSegment>(segments) : new List<BezierSegment>();
        }

        public void AddSegment(BezierSegment segment)
        {
            if (_segments == null)
            {
                _segments = new List<BezierSegment>();
            }
            _segments.Add(segment);
        }

        public void Clear()
        {
            _segments?.Clear();
        }

        public BezierSegment[] ToArray()
        {
            return _segments != null ? _segments.ToArray() : Array.Empty<BezierSegment>();
        }

        public static implicit operator BezierSegment[](BezierPath path)
        {
            return path?._segments != null ? path._segments.ToArray() : Array.Empty<BezierSegment>();
        }
    }

    /// <summary>
    /// 3차 베지어 곡선(Cubic Bézier Curve) 위치 및 접선 벡터 계산을 전담하는 순수 정적 수학 유틸리티 클래스입니다.
    /// 수식: B(t) = (1-t)^3*P0 + 3*(1-t)^2*t*P1 + 3*(1-t)*t^2*P2 + t^3*P3 (0 <= t <= 1)
    /// </summary>
    public static class BezierCurve
    {
        private const float Epsilon = 0.000001f;

        /// <summary>
        /// 4개의 제어점과 진행도 t(0~1)를 기반으로 3차 베지어 곡선 상의 2D 좌표를 계산합니다.
        /// </summary>
        public static Vector2 Evaluate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
        }

        /// <summary>
        /// 4개의 제어점과 진행도 t(0~1)를 기반으로 3차 베지어 곡선 상의 3D 좌표를 계산합니다.
        /// </summary>
        public static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
        }

        /// <summary>
        /// 3차 베지어 곡선의 1차 도함수(B'(t)) 벡터를 계산합니다.
        /// 수식: B'(t) = 3*(1-t)^2*(P1-P0) + 6*(1-t)*t*(P2-P1) + 3*t^2*(P3-P2)
        /// </summary>
        public static Vector2 GetDerivative(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;

            Vector2 d0 = 3f * uu * (p1 - p0);
            Vector2 d1 = 6f * u * t * (p2 - p1);
            Vector2 d2 = 3f * tt * (p3 - p2);

            return d0 + d1 + d2;
        }

        /// <summary>
        /// 3차 베지어 곡선의 1차 도함수(B'(t)) 3D 벡터를 계산합니다.
        /// </summary>
        public static Vector3 GetDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;

            Vector3 d0 = 3f * uu * (p1 - p0);
            Vector3 d1 = 6f * u * t * (p2 - p1);
            Vector3 d2 = 3f * tt * (p3 - p2);

            return d0 + d1 + d2;
        }

        /// <summary>
        /// 진행도 t(0~1)에서의 정규화된 진행 방향 접선(Tangent) 단위 벡터를 계산합니다.
        /// </summary>
        public static Vector2 GetTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            Vector2 derivative = GetDerivative(p0, p1, p2, p3, t);

            if (derivative.sqrMagnitude > Epsilon)
            {
                return derivative.normalized;
            }

            // 제어점 간 거리가 0이거나 미분값이 0인 경우 방어 폴백
            Vector2 fallback = p3 - p0;
            if (fallback.sqrMagnitude > Epsilon)
            {
                return fallback.normalized;
            }

            return Vector2.up;
        }

        /// <summary>
        /// 진행도 t(0~1)에서의 정규화된 진행 방향 접선(Tangent) 3D 단위 벡터를 계산합니다.
        /// </summary>
        public static Vector3 GetTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 derivative = GetDerivative(p0, p1, p2, p3, t);

            if (derivative.sqrMagnitude > Epsilon)
            {
                return derivative.normalized;
            }

            Vector3 fallback = p3 - p0;
            if (fallback.sqrMagnitude > Epsilon)
            {
                return fallback.normalized;
            }

            return Vector3.up;
        }

        /// <summary>
        /// 단일 세그먼트의 호 길이(Arc Length)를 선형 분할 샘플링을 통해 근사 계산합니다.
        /// </summary>
        public static float CalculateSegmentLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int samples = 20)
        {
            if (samples < 1)
            {
                samples = 1;
            }

            float length = 0f;
            Vector2 prevPoint = p0;
            float step = 1f / samples;

            for (int i = 1; i <= samples; i++)
            {
                float t = i * step;
                Vector2 currentPoint = Evaluate(p0, p1, p2, p3, t);
                length += Vector2.Distance(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }

            return length;
        }

        /// <summary>
        /// 여러 세그먼트로 구성된 경로 전체의 정규화 진행도 progress(0~1)에 대응하는 2D 좌표를 계산합니다.
        /// </summary>
        public static Vector2 EvaluatePath(BezierSegment[] segments, float progress)
        {
            if (segments == null || segments.Length == 0)
            {
                return Vector2.zero;
            }

            progress = Mathf.Clamp01(progress);
            int segmentCount = segments.Length;

            if (segmentCount == 1)
            {
                return segments[0].Evaluate(progress);
            }

            if (progress >= 1f)
            {
                return segments[segmentCount - 1].Evaluate(1f);
            }

            float totalProgress = progress * segmentCount;
            int segIndex = Mathf.Min(Mathf.FloorToInt(totalProgress), segmentCount - 1);
            float localT = totalProgress - segIndex;

            return segments[segIndex].Evaluate(localT);
        }

        /// <summary>
        /// 여러 세그먼트로 구성된 경로 전체의 정규화 진행도 progress(0~1)에 대응하는 접선 단위 벡터를 계산합니다.
        /// </summary>
        public static Vector2 GetPathTangent(BezierSegment[] segments, float progress)
        {
            if (segments == null || segments.Length == 0)
            {
                return Vector2.up;
            }

            progress = Mathf.Clamp01(progress);
            int segmentCount = segments.Length;

            if (segmentCount == 1)
            {
                return segments[0].GetTangent(progress);
            }

            if (progress >= 1f)
            {
                return segments[segmentCount - 1].GetTangent(1f);
            }

            float totalProgress = progress * segmentCount;
            int segIndex = Mathf.Min(Mathf.FloorToInt(totalProgress), segmentCount - 1);
            float localT = totalProgress - segIndex;

            return segments[segIndex].GetTangent(localT);
        }

        /// <summary>
        /// 여러 세그먼트로 구성된 경로 전체의 총 길이를 계산합니다.
        /// </summary>
        public static float CalculatePathLength(BezierSegment[] segments, int samplesPerSegment = 20)
        {
            if (segments == null || segments.Length == 0)
            {
                return 0f;
            }

            float totalLength = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                totalLength += segments[i].CalculateLength(samplesPerSegment);
            }

            return totalLength;
        }
    }
}
