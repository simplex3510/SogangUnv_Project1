using UnityEngine;

namespace Game.Systems
{
    /// <summary>하루 운영의 영업 시각과 시간 배속 설정.</summary>
    [CreateAssetMenu(menuName = "Game/DailyOperationConfig", fileName = "DailyOperationConfig")]
    public class DailyOperationConfig : ScriptableObject
    {
        [Header("영업 시간")]
        [SerializeField, Range(0, 23)] private int _businessStartHour = 9;
        [SerializeField, Range(0, 23)] private int _businessEndHour = 18;

        [Header("시간 배속")]
        [Tooltip("현실 1초당 진행되는 게임 시간(초)")]
        [SerializeField, Min(0f)] private float _gameSpeed = 60f; // TODO(tuning)

        public int BusinessStartSeconds => _businessStartHour * 3600;
        public int BusinessEndSeconds => _businessEndHour * 3600;
        public float GameSpeed => _gameSpeed;
    }
}
