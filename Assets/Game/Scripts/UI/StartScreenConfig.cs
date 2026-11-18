using Unity.Cinemachine;
using UnityEngine;

namespace Game.UI
{
    /// <summary>시작 화면의 카메라 전환과 메뉴 배치 설정(design/quick-specs/start-screen-2026-11-18.md).</summary>
    [CreateAssetMenu(menuName = "Game/StartScreenConfig", fileName = "StartScreenConfig")]
    public class StartScreenConfig : ScriptableObject
    {
        [Header("카메라 전환")]
        [Tooltip("메뉴 구도에서 인게임 구도로 돌아가는 시간(초)")]
        [SerializeField, Range(0.5f, 3f)] private float _blendTime = 1.5f; // TODO(tuning)
        [SerializeField] private CinemachineBlendDefinition.Styles _blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

        [Header("메뉴 배치")]
        [Tooltip("아래 행일수록 오른쪽으로 미는 간격(px). 0이면 수직 정렬")]
        [SerializeField, Range(0f, 120f)] private float _rowOffset = 66f; // TODO(tuning)
        [Tooltip("메뉴 영역이 차지하는 화면 오른쪽 폭(%)")]
        [SerializeField, Range(35f, 55f)] private float _menuWidthPercent = 45f; // TODO(tuning)

        public float BlendTime => _blendTime;
        public CinemachineBlendDefinition.Styles BlendStyle => _blendStyle;
        public float RowOffset => _rowOffset;
        public float MenuWidthPercent => _menuWidthPercent;
    }
}
