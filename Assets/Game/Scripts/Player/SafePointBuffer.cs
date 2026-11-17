using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Player.Recovery
{
    /// <summary>복구에 쓰는 안전 지점. 위치와 수평 진행 방향, 기록 시각.</summary>
    public readonly struct SafePoint
    {
        public readonly Vector3 Position;
        public readonly Vector3 Forward;
        public readonly float RecordedTime;

        public SafePoint(Vector3 position, Vector3 forward, float recordedTime)
        {
            Position = position;
            Forward = forward;
            RecordedTime = recordedTime;
        }
    }

    /// <summary>
    /// 최근 안전 지점을 고정 개수만큼 보관하고 복구 지점을 고른다(기획서 10.6). MonoBehaviour와 무관한 순수 로직이다.
    /// </summary>
    public class SafePointBuffer
    {
        private readonly List<SafePoint> _points = new();
        private readonly int _capacity;

        public SafePointBuffer(int capacity)
        {
            _capacity = capacity;
        }

        public int Count => _points.Count;

        /// <summary>안전 지점을 추가한다. 보관 개수를 넘으면 가장 오래된 지점을 버린다.</summary>
        public void Record(SafePoint point)
        {
            if (_points.Count >= _capacity)
            {
                _points.RemoveAt(0);
            }

            _points.Add(point);
        }

        /// <summary>
        /// 기록된 지 minAgeSeconds 이상 지난 지점 중 가장 최근 것을 고른다. 겹치면 그보다 이전 지점으로 내려간다.
        /// 쓸 수 있는 지점이 없으면 false(호출자가 물류센터 앞으로 대체한다).
        /// </summary>
        public bool TryPick(float now, float minAgeSeconds, Func<SafePoint, bool> overlaps, out SafePoint picked)
        {
            for (int i = _points.Count - 1; i >= 0; i--)
            {
                SafePoint candidate = _points[i];
                if (now - candidate.RecordedTime < minAgeSeconds || overlaps(candidate))
                {
                    continue;
                }

                picked = candidate;
                return true;
            }

            picked = default;
            return false;
        }
    }
}
