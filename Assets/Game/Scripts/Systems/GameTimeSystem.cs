using System;

using UnityEngine;

namespace Game.Systems
{
    /// <summary>게임 내 시간과 배속의 단일 기준. Time.timeScale은 건드리지 않는다.</summary>
    public class GameTimeSystem : MonoBehaviour
    {
        public static GameTimeSystem Instance { get; private set; }

        [SerializeField] private DailyOperationConfig _config;

        private float _currentSeconds; // 자정 기준 게임 시간(초)

        public event Action BusinessEndReached;

        public bool IsBusinessEndReached { get; private set; }

        public float CurrentSeconds => _currentSeconds;

        // 다른 시스템이 영업 시각 기준으로 생성 마감 등을 계산할 때 사용한다
        public int BusinessStartSeconds => _config != null ? _config.BusinessStartSeconds : 0;
        public int BusinessEndSeconds => _config != null ? _config.BusinessEndSeconds : 0;

        /// <summary>직전 프레임에 진행된 게임 시간(초). 영업 종료 후에는 0.</summary>
        public float GameDeltaSeconds { get; private set; }

        public string CurrentTimeText => FormatTime(_currentSeconds);

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogError($"DailyOperationConfig is not assigned in {gameObject.name}");
                enabled = false;
                return;
            }

            _currentSeconds = _config.BusinessStartSeconds;
        }

        private void Update()
        {
            if (IsBusinessEndReached)
            {
                GameDeltaSeconds = 0f;
                return;
            }

            GameDeltaSeconds = Time.deltaTime * _config.GameSpeed;
            _currentSeconds += GameDeltaSeconds;

            if (_currentSeconds >= _config.BusinessEndSeconds)
            {
                _currentSeconds = _config.BusinessEndSeconds;
                IsBusinessEndReached = true;
                BusinessEndReached?.Invoke();
                Debug.Log($"영업 종료 시각 도달: {CurrentTimeText}");
            }
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            return $"{hours:00}:{minutes:00}";
        }
    }
}
