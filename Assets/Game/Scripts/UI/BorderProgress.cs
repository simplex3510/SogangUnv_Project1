using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// 부모 테두리를 따라 위 가운데에서 시계 방향으로 차오르는 진행 표시. USS 테두리로는 일부만 그릴 수 없어 Painter2D로 그린다.
    /// 부모의 테두리 두께만큼 바깥으로 넓혀 부모 테두리 위에 겹쳐 그린다.
    /// </summary>
    public class BorderProgress : VisualElement
    {
        private float _value;

        /// <summary>채움 비율(0~1).</summary>
        public float Value
        {
            get => _value;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_value, value) == false)
                {
                    _value = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public float Radius { get; set; } = 10f;
        public float LineWidth { get; set; } = 3f;
        public Color Color { get; set; } = new(0.33f, 0.85f, 0.55f);

        public BorderProgress()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            generateVisualContent += OnGenerateVisualContent;
        }

        /// <summary>부모의 테두리 두께·모서리 반경에 맞춰 붙인다.</summary>
        public void AttachTo(VisualElement parent)
        {
            if (this.parent != parent)
            {
                parent.Add(this);
            }

            float border = parent.resolvedStyle.borderLeftWidth;
            style.left = style.top = style.right = style.bottom = -border;
            Radius = parent.resolvedStyle.borderTopLeftRadius;
            LineWidth = Mathf.Max(3f, border + 1f);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (_value <= 0.001f)
            {
                return;
            }

            // 선 굵기 절반만큼 안쪽 사각형을 따라 그려 선이 요소 밖으로 잘리지 않게 한다
            float inset = LineWidth * 0.5f;
            float left = inset;
            float top = inset;
            float right = layout.width - inset;
            float bottom = layout.height - inset;
            if (right - left < 1f || bottom - top < 1f)
            {
                return;
            }

            float r = Mathf.Max(0f, Mathf.Min(Radius - inset, (right - left) * 0.5f, (bottom - top) * 0.5f));
            float width = right - left - 2f * r;
            float height = bottom - top - 2f * r;
            float arc = Mathf.PI * 0.5f * r;
            float remaining = _value * (2f * (width + height) + 4f * arc);
            float centerX = (left + right) * 0.5f;

            Painter2D painter = ctx.painter2D;
            painter.lineWidth = LineWidth;
            painter.strokeColor = Color;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(centerX, top));

            // 위 가운데 → 오른쪽 위 → 오른쪽 아래 → 왼쪽 아래 → 왼쪽 위 → 위 가운데. 각도는 화면 기준 시계 방향(-90 = 위)
            if (Line(painter, new Vector2(centerX, top), new Vector2(right - r, top), ref remaining)
                && Corner(painter, new Vector2(right - r, top + r), r, -90f, arc, ref remaining)
                && Line(painter, new Vector2(right, top + r), new Vector2(right, bottom - r), ref remaining)
                && Corner(painter, new Vector2(right - r, bottom - r), r, 0f, arc, ref remaining)
                && Line(painter, new Vector2(right - r, bottom), new Vector2(left + r, bottom), ref remaining)
                && Corner(painter, new Vector2(left + r, bottom - r), r, 90f, arc, ref remaining)
                && Line(painter, new Vector2(left, bottom - r), new Vector2(left, top + r), ref remaining)
                && Corner(painter, new Vector2(left + r, top + r), r, 180f, arc, ref remaining))
            {
                Line(painter, new Vector2(left + r, top), new Vector2(centerX, top), ref remaining);
            }

            painter.Stroke();
        }

        /// <summary>남은 길이만큼 직선을 긋는다. 다 그렸으면 다음 구간으로 넘어가도록 true.</summary>
        private static bool Line(Painter2D painter, Vector2 from, Vector2 to, ref float remaining)
        {
            float length = Vector2.Distance(from, to);
            float t = length > 0f ? Mathf.Min(1f, remaining / length) : 1f;
            painter.LineTo(Vector2.Lerp(from, to, t));
            remaining -= length;
            return remaining > 0f;
        }

        private static bool Corner(Painter2D painter, Vector2 center, float radius, float startDegrees, float length, ref float remaining)
        {
            if (length > 0f)
            {
                float t = Mathf.Min(1f, remaining / length);
                painter.Arc(center, radius, Angle.Degrees(startDegrees), Angle.Degrees(startDegrees + 90f * t), ArcDirection.Clockwise);
            }

            remaining -= length;
            return remaining > 0f;
        }
    }
}
