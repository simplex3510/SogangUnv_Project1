using UnityEngine;

namespace Game.Player.Movement
{
    /// <summary>
    /// 고속 모드 사용 시간 게이지와 모드 전환 쿨다운.
    /// 규칙: design/quick-specs/fast-mode-gauge-speedometer-2026-11-17.md
    /// </summary>
    public class FastModeGauge
    {
        private readonly VehicleConfig _config;
        private float _sinceModeChange = float.PositiveInfinity;

        public FastModeGauge(VehicleConfig config)
        {
            _config = config;
        }

        /// <summary>게이지 비율(0~1).</summary>
        public float Value { get; private set; } = 1f;

        /// <summary>게이지가 0에 닿아 강제 전환된 뒤 재진입 기준까지 차지 않은 상태.</summary>
        public bool IsExhausted { get; private set; }

        public bool IsOnCooldown => _sinceModeChange < _config.ModeChangeCooldownSeconds;

        /// <summary>시간을 진행한다. 고속 중 게이지가 0에 닿으면 true(일반 모드로 강제 전환해야 함).</summary>
        public bool Tick(float deltaTime, bool isFast)
        {
            _sinceModeChange += deltaTime;

            if (isFast)
            {
                Value = Mathf.Max(0f, Value - deltaTime / _config.FastGaugeDrainSeconds);
                if (Value > 0f)
                {
                    return false;
                }

                IsExhausted = true;
                return true;
            }

            if (_sinceModeChange >= _config.FastGaugeRechargeDelaySeconds)
            {
                Value = Mathf.Min(1f, Value + deltaTime / _config.FastGaugeRechargeSeconds);
            }

            if (IsExhausted && Value >= _config.FastGaugeReentryRatio)
            {
                IsExhausted = false;
            }

            return false;
        }

        /// <summary>수동 전환 요청이 게이지·쿨다운 조건을 만족하는지.</summary>
        public bool CanToggle(bool isFast)
        {
            if (IsOnCooldown)
            {
                return false;
            }

            return isFast || (Value > 0f && IsExhausted == false);
        }

        /// <summary>모드가 바뀐 순간(수동·강제) 호출해 쿨다운과 충전 지연을 시작한다.</summary>
        public void NotifyModeChanged()
        {
            _sinceModeChange = 0f;
        }
    }
}
