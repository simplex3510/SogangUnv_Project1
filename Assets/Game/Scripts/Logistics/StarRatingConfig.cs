using UnityEngine;

namespace Game.Logistics
{
    /// <summary>일일 별점 경계값 설정(기획서 12.4). 조건부 성공 비율 R이 이 값 이상이면 해당 별 개수를 받는다.</summary>
    [CreateAssetMenu(menuName = "Game/Logistics/StarRatingConfig", fileName = "StarRatingConfig")]
    public class StarRatingConfig : ScriptableObject
    {
        [Tooltip("별 2개 경계값 T2. R이 이 값 이상이면 별 2개(R = 1이면 3개)")]
        [SerializeField, Range(0f, 1f)] private float _threshold2 = 0.7f; // TODO(tuning): 조정은 Story 003
        [Tooltip("별 1개 경계값 T1. R이 이 값 이상이면 별 1개")]
        [SerializeField, Range(0f, 1f)] private float _threshold1 = 0.4f; // TODO(tuning): 조정은 Story 003

        public float Threshold2 => _threshold2;
        public float Threshold1 => _threshold1;

        private void OnValidate()
        {
            if (_threshold1 >= _threshold2)
            {
                Debug.LogWarning($"StarRatingConfig: T1({_threshold1})은 T2({_threshold2})보다 작아야 한다(기획서 12.4)", this);
            }
        }
    }
}
