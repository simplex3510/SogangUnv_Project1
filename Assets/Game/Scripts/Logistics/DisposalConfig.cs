using UnityEngine;

namespace Game.Logistics
{
    /// <summary>폐기 물류 처분 설정(기획서 11.6 처분 장소).</summary>
    [CreateAssetMenu(menuName = "Game/Logistics/DisposalConfig", fileName = "DisposalConfig")]
    public class DisposalConfig : ScriptableObject
    {
        [Tooltip("폐기 물류 1개를 처분하는 데 걸리는 시간(초)")]
        [SerializeField, Min(0f)] private float _secondsPerParcel = 1f; // TODO(tuning)

        public float SecondsPerParcel => _secondsPerParcel;
    }
}
