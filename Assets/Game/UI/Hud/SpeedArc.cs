using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI.Hud
{
    /// <summary>속도계 아크. USS로는 호를 그릴 수 없어 Painter2D로 그린다.</summary>
    [UxmlElement]
    public partial class SpeedArc : VisualElement
    {
        // 레퍼런스처럼 아래가 트인 240° 아크. 0°=오른쪽, 시계 방향 증가
        private const float StartDegrees = 150f;
        private const float SweepDegrees = 240f;

        private float _value;

        /// <summary>채움 비율(0~1).</summary>
        [UxmlAttribute]
        public float Value
        {
            get => _value;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_value, value))
                {
                    return;
                }

                _value = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute] public float Thickness { get; set; } = 18f;
        [UxmlAttribute] public float OutlineWidth { get; set; } = 4f;
        [UxmlAttribute] public Color OutlineColor { get; set; } = new Color(0.17f, 0.2f, 0.25f, 0.95f);
        [UxmlAttribute] public Color TrackColor { get; set; } = new Color(0.32f, 0.36f, 0.42f, 0.9f);
        [UxmlAttribute] public Color FillColor { get; set; } = new Color(0.25f, 0.85f, 0.55f);

        public SpeedArc()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect rect = contentRect;
            if (rect.width < 1f || rect.height < 1f)
            {
                return;
            }

            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - Thickness * 0.5f - OutlineWidth;
            Painter2D painter = ctx.painter2D;
            painter.lineCap = LineCap.Round;

            // 외곽선 → 트랙 → 채움 순서로 겹쳐 그려 굵은 테두리 아크를 만든다
            Stroke(painter, center, radius, SweepDegrees, Thickness + OutlineWidth * 2f, OutlineColor);
            Stroke(painter, center, radius, SweepDegrees, Thickness, TrackColor);
            if (_value > 0.001f)
            {
                Stroke(painter, center, radius, SweepDegrees * _value, Thickness, FillColor);
            }
        }

        private static void Stroke(Painter2D painter, Vector2 center, float radius, float sweep, float width, Color color)
        {
            painter.lineWidth = width;
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(StartDegrees), Angle.Degrees(StartDegrees + sweep),
                        ArcDirection.Clockwise);
            painter.Stroke();
        }
    }
}
